using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Configuration; // لقراءة المفتاح من Web.config
using Newtonsoft.Json;      // المكتبة الموجودة في مشروعك حالياً
using System.Collections.Generic;

public class ZhipuAiService
{
    // قراءة المفتاح الذي أضفته أنت في Web.config
    private static readonly string ApiKey = ConfigurationManager.AppSettings["ZhipuAi_ApiKey"];
    private const string ApiUrl = "https://open.bigmodel.cn/api/paas/v4/chat/completions";

    public async Task<string> CallAiAsync(string userMessage)
    {
        using (var client = new HttpClient())
        {
            // 1. إعدادات الحماية (Authorization)
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");

            // 2. تجهيز البيانات المرسلة (الـ Payload)
            var payload = new
            {
                model = "glm-4.7", // الموديل الذي اخترناه
                messages = new[]
                {
                    new { role = "user", content = userMessage }
                },
                temperature = 0.7 // درجة الإبداع في الإجابة
            };

            var jsonContent = JsonConvert.SerializeObject(payload);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            try
            {
                // 3. إرسال الطلب للسيرفر
                var response = await client.PostAsync(ApiUrl, httpContent);
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // 4. استخراج نص الإجابة فقط من الـ JSON المعقد
                    dynamic result = JsonConvert.DeserializeObject(responseString);
                    return result.choices[0].message.content;
                }
                else
                {
                    return $"خطأ من السيرفر: {response.StatusCode} - {responseString}";
                }
            }
            catch (Exception ex)
            {
                return $"حدث خطأ في الاتصال: {ex.Message}";
            }
        }
    }
}