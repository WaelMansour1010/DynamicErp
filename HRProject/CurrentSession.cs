using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace EazyCash
{
    public class CurrentSession
    {
        public static IConfiguration Configuration { get; set; }

        public static string ConnectionString
        {
            get
            {
                // التعديل الجديد: فحص اسم الجهاز
                // لو اسم جهازك الحالي PC2 هيستخدم اللوكال، غير كده هيستخدم السيرفر
                if (Environment.MachineName.Equals("PC2", StringComparison.OrdinalIgnoreCase))
                {
                    return Configuration.GetConnectionString("Local");
                }

                return Configuration.GetConnectionString("myconnection");
            }
        }

        public static int TimeZone
        {
            get
            {
                try
                {
                    var time = Configuration["Pub:time"];
                    var hour = 0;
                    int.TryParse(time, out hour);
                    return hour;
                }
                catch
                {
                    return 0;
                }
            }
        }

        public static bool location
        {
            // "Pub": {
            //    "loc": "1"
            // }
            get
            {
                try
                {
                    return Configuration["Pub:loc"] == "1";
                }
                catch
                {
                    return false;
                }
            }
        }
    }

    public static class ext
    {
        public static DateTime ToSiteTime(this DateTime dt)
        {
            return dt.ToUniversalTime().AddHours(CurrentSession.TimeZone);
        }

        public static async Task<string> GetBodyStringAsync(this HttpRequest request, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;

            using (StreamReader reader = new StreamReader(request.Body, encoding))
                return await reader.ReadToEndAsync();
        }

        public static async Task<string> GetRawBodyStringAsync(this HttpRequest request, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;

            using (StreamReader reader = new StreamReader(request.Body, encoding))
                return await reader.ReadToEndAsync();
        }

        public static string PrepareQuery(string Word)
        {
            if (string.IsNullOrEmpty(Word))
            {
                return Word;
            }
            string alph = "أاآإ";
            string haa = "ةه";
            string yaa = "يى";

            string S = "", mychar;

            for (int i = 0; i < Word.Length; i++)
            {
                mychar = Word[i].ToString();
                if (alph.Contains(mychar))
                {
                    S += "[" + alph + "]";
                }
                else if (haa.Contains(mychar))
                {
                    S += "[" + haa + "]";
                }
                else if (yaa.Contains(mychar))
                {
                    S += "[" + yaa + "]";
                }
                else
                {
                    S += mychar;
                }
            }

            return $"%{S}%";
        }
    }
}