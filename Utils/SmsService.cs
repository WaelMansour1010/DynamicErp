using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace MyERP
{
    /// <summary>
    /// خدمة إرسال الرسائل النصية عبر Oursms API
    /// </summary>
    public static class OurSmsService
    {
        private static readonly string BaseUrl = "https://api.oursms.com/msgs/sms";
        private static readonly string CreditsUrl = "https://api.oursms.com/billing/credits";

        /// <summary>
        /// الحصول على API Token من Web.config
        /// </summary>
        private static string ApiToken => ConfigurationManager.AppSettings["OursmsApiToken"];

        /// <summary>
        /// الحصول على اسم المرسل من Web.config
        /// </summary>
        private static string SenderId => ConfigurationManager.AppSettings["OursmsSenderId"];

        /// <summary>
        /// الحد الأدنى للرصيد قبل التنبيه
        /// </summary>
        private static int MinimumCredits => int.TryParse(ConfigurationManager.AppSettings["OursmsMinCredits"], out int result) ? result : 10;

        /// <summary>
        /// نتيجة إرسال الرسالة
        /// </summary>
        public class SmsResult
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public string ResponseData { get; set; }
        }

        /// <summary>
        /// نتيجة فحص الرصيد
        /// </summary>
        public class CreditsResult
        {
            public bool Success { get; set; }
            public int Credits { get; set; }
            public bool IsLowBalance { get; set; }
            public string Message { get; set; }
        }

        /// <summary>
        /// إرسال رسالة SMS لرقم واحد
        /// </summary>
        /// <param name="phone">رقم الهاتف</param>
        /// <param name="message">نص الرسالة</param>
        /// <returns>نتيجة الإرسال</returns>
        public static async Task<SmsResult> SendSms(string phone, string message)
        {
            return await SendSms(new string[] { phone }, message);
        }

        /// <summary>
        /// إرسال رسالة SMS لعدة أرقام
        /// </summary>
        /// <param name="phones">أرقام الهواتف</param>
        /// <param name="message">نص الرسالة</param>
        /// <returns>نتيجة الإرسال</returns>
        public static async Task<SmsResult> SendSms(string[] phones, string message)
        {
            var result = new SmsResult();

            try
            {
                // التحقق من الإعدادات
                if (string.IsNullOrEmpty(ApiToken))
                {
                    result.Success = false;
                    result.Message = "لم يتم تكوين API Token في الإعدادات";
                    return result;
                }

                if (string.IsNullOrEmpty(SenderId))
                {
                    result.Success = false;
                    result.Message = "لم يتم تكوين اسم المرسل في الإعدادات";
                    return result;
                }

                // تنسيق أرقام الهواتف
                var formattedPhones = new string[phones.Length];
                for (int i = 0; i < phones.Length; i++)
                {
                    formattedPhones[i] = FormatPhoneNumber(phones[i]);
                }

                var request = WebRequest.Create(BaseUrl) as HttpWebRequest;
                request.Method = "POST";
                request.ContentType = "application/json; charset=utf-8";
                request.Headers.Add("Authorization", "Bearer " + ApiToken);

                var serializer = new JavaScriptSerializer();
                var requestBody = new
                {
                    src = SenderId,
                    dests = formattedPhones,
                    body = message,
                    msgClass = "transactional"
                };

                var jsonBody = serializer.Serialize(requestBody);
                byte[] byteArray = Encoding.UTF8.GetBytes(jsonBody);

                using (var writer = await request.GetRequestStreamAsync())
                {
                    await writer.WriteAsync(byteArray, 0, byteArray.Length);
                }

                using (var response = await request.GetResponseAsync() as HttpWebResponse)
                {
                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        result.ResponseData = await reader.ReadToEndAsync();
                    }

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        result.Success = true;
                        result.Message = "تم إرسال الرسالة بنجاح";
                    }
                    else
                    {
                        result.Success = false;
                        result.Message = $"فشل إرسال الرسالة: {response.StatusDescription}";
                    }
                }
            }
            catch (WebException ex)
            {
                result.Success = false;
                if (ex.Response != null)
                {
                    using (var reader = new StreamReader(ex.Response.GetResponseStream()))
                    {
                        result.ResponseData = reader.ReadToEnd();
                    }
                }
                result.Message = $"خطأ في إرسال الرسالة: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"SMS Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"خطأ غير متوقع: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"SMS Error: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// فحص الرصيد المتبقي
        /// </summary>
        /// <returns>نتيجة فحص الرصيد</returns>
        public static async Task<CreditsResult> CheckCredits()
        {
            var result = new CreditsResult();

            try
            {
                if (string.IsNullOrEmpty(ApiToken))
                {
                    result.Success = false;
                    result.Message = "لم يتم تكوين API Token في الإعدادات";
                    return result;
                }

                var request = WebRequest.Create(CreditsUrl) as HttpWebRequest;
                request.Method = "GET";
                request.Headers.Add("Authorization", "Bearer " + ApiToken);

                using (var response = await request.GetResponseAsync() as HttpWebResponse)
                {
                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        var responseData = await reader.ReadToEndAsync();
                        var serializer = new JavaScriptSerializer();
                        dynamic jsonResponse = serializer.DeserializeObject(responseData);

                        // محاولة استخراج الرصيد من الاستجابة
                        if (jsonResponse != null && jsonResponse.ContainsKey("credits"))
                        {
                            result.Credits = Convert.ToInt32(jsonResponse["credits"]);
                        }
                        else if (jsonResponse != null && jsonResponse.ContainsKey("balance"))
                        {
                            result.Credits = Convert.ToInt32(jsonResponse["balance"]);
                        }

                        result.Success = true;
                        result.IsLowBalance = result.Credits < MinimumCredits;
                        result.Message = result.IsLowBalance
                            ? $"تحذير: الرصيد المتبقي ({result.Credits}) أقل من الحد الأدنى ({MinimumCredits})"
                            : $"الرصيد المتبقي: {result.Credits} رسالة";
                    }
                }
            }
            catch (WebException ex)
            {
                result.Success = false;
                result.Message = $"خطأ في فحص الرصيد: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Credits Check Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"خطأ غير متوقع: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Credits Check Error: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// تنسيق رقم الهاتف ليتوافق مع صيغة API
        /// يدعم الأرقام السعودية والمصرية
        /// </summary>
        /// <param name="phone">رقم الهاتف الأصلي</param>
        /// <returns>رقم الهاتف المنسق</returns>
        public static string FormatPhoneNumber(string phone)
        {
            if (string.IsNullOrEmpty(phone))
                return phone;

            // إزالة المسافات والشرطات والأقواس
            phone = phone.Replace(" ", "")
                        .Replace("-", "")
                        .Replace("(", "")
                        .Replace(")", "")
                        .Replace("+", "");

            // إذا كان الرقم يبدأ بـ 00، نزيل 00
            if (phone.StartsWith("00"))
            {
                phone = phone.Substring(2);
            }

            // إذا كان الرقم يبدأ بـ 0 ويتكون من 10 أرقام (سعودي) أو 11 رقم (مصري)
            if (phone.StartsWith("0"))
            {
                // رقم سعودي: 05xxxxxxxx -> 966xxxxxxxx
                if (phone.Length == 10 && phone.StartsWith("05"))
                {
                    phone = "966" + phone.Substring(1);
                }
                // رقم مصري: 01xxxxxxxx -> 20xxxxxxxx
                else if (phone.Length == 11 && phone.StartsWith("01"))
                {
                    phone = "20" + phone.Substring(1);
                }
            }

            return phone;
        }

        /// <summary>
        /// إنشاء رسالة تذكير بالدفعة المستحقة
        /// </summary>
        /// <param name="renterName">اسم المستأجر</param>
        /// <param name="amount">قيمة القسط</param>
        /// <param name="dueDate">تاريخ الاستحقاق</param>
        /// <returns>نص الرسالة</returns>
        public static string CreateDueBatchMessage(string renterName, decimal amount, DateTime dueDate)
        {
            return $"عزيزي {renterName}، نود إخطارك بصدور قسط بقيمة {amount:N2} يستحق بتاريخ {dueDate:yyyy/MM/dd}. شكراً لتعاونكم.";
        }

        /// <summary>
        /// إرسال تنبيه للمدير عند انخفاض الرصيد
        /// </summary>
        /// <param name="adminPhone">رقم هاتف المدير</param>
        /// <param name="currentCredits">الرصيد الحالي</param>
        /// <returns>نتيجة الإرسال</returns>
        public static async Task<SmsResult> SendLowBalanceAlert(string adminPhone, int currentCredits)
        {
            var message = $"تنبيه: رصيد الرسائل النصية منخفض. الرصيد الحالي: {currentCredits} رسالة. يرجى شحن الرصيد لضمان استمرار الخدمة.";
            return await SendSms(adminPhone, message);
        }
    }
}
