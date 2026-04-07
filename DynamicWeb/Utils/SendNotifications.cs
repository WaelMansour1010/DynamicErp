using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Web.Script.Serialization;
using System.Web;

namespace MyERP
{
    public static class SendNotifications
    {
        private const string restId = "MWI5YTZlMTYtNWJmMy00Mjc4LWI1OWItZWY1NzhkZDgxNTk5";
        private const string appId = "cbf18993-898a-4b6b-87ff-e51ec31735d5";

        public static async Task SendToUsers(string[] playerIds, string title, string msg, string url)
        {
            string domainName = HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority);
            url = domainName + "/" + url;
            var request = WebRequest.Create("https://onesignal.com/api/v1/notifications") as HttpWebRequest;

            request.KeepAlive = true;
            request.Method = "POST";
            request.ContentType = "application/json; charset=utf-8";

            request.Headers.Add("authorization", "Basic " + restId);

            var serializer = new JavaScriptSerializer();
            var obj = new
            {
                app_id = appId,
                contents = new { en = msg },
                headings = new { en = title },
                url,
                include_player_ids = playerIds
            };

            var param = serializer.Serialize(obj);
            byte[] byteArray = Encoding.UTF8.GetBytes(param);

            string responseContent = null;

            try
            {
                using (var writer = await request.GetRequestStreamAsync())
                {
                    writer.Write(byteArray, 0, byteArray.Length);
                }

                using (var response = await request.GetResponseAsync() as HttpWebResponse)
                {
                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        responseContent = reader.ReadToEnd();
                    }
                }
            }
            catch (WebException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                System.Diagnostics.Debug.WriteLine(new StreamReader(ex.Response.GetResponseStream()).ReadToEnd());
            }

            System.Diagnostics.Debug.WriteLine(responseContent);

        }

        public static async Task SendToAll(string title, string msg)
        {
            var request = WebRequest.Create("https://onesignal.com/api/v1/notifications") as HttpWebRequest;

            request.KeepAlive = true;
            request.Method = "POST";
            request.ContentType = "application/json; charset=utf-8";

            request.Headers.Add("authorization", "Basic " + restId);

            var serializer = new JavaScriptSerializer();
            var obj = new
            {
                app_id = appId,
                contents = new { en = msg },
                headings = new { en = title },
                included_segments = new string[] { "All" }
            };
            var param = serializer.Serialize(obj);
            byte[] byteArray = Encoding.UTF8.GetBytes(param);

            string responseContent = null;

            try
            {
                using (var writer = await request.GetRequestStreamAsync())
                {
                    writer.Write(byteArray, 0, byteArray.Length);
                }

                using (var response = await request.GetResponseAsync() as HttpWebResponse)
                {
                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        responseContent = reader.ReadToEnd();
                    }
                }
            }
            catch (WebException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                System.Diagnostics.Debug.WriteLine(new StreamReader(ex.Response.GetResponseStream()).ReadToEnd());
            }

            System.Diagnostics.Debug.WriteLine(responseContent);
        }
    }
}