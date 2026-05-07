using System;
using System.Globalization;
using System.Threading;
using System.Web;

namespace MyERP.Areas.MainErp.Infrastructure.Localization
{
    public static class MainErpCultureManager
    {
        public const string SessionKey = "MainErp.Culture";
        public const string CookieName = "MainErp.Culture";
        public const string Arabic = "ar";
        public const string English = "en";

        public static string CurrentLanguage
        {
            get
            {
                var context = HttpContext.Current;
                var sessionValue = context != null && context.Session != null ? Convert.ToString(context.Session[SessionKey]) : null;
                if (IsSupported(sessionValue))
                {
                    return Normalize(sessionValue);
                }

                var cookieValue = context != null && context.Request != null && context.Request.Cookies[CookieName] != null
                    ? context.Request.Cookies[CookieName].Value
                    : null;
                if (IsSupported(cookieValue))
                {
                    return Normalize(cookieValue);
                }

                return Arabic;
            }
        }

        public static bool IsArabic
        {
            get { return CurrentLanguage == Arabic; }
        }

        public static string Direction
        {
            get { return IsArabic ? "rtl" : "ltr"; }
        }

        public static string DirectionCssClass
        {
            get { return IsArabic ? "main-erp-rtl" : "main-erp-ltr"; }
        }

        public static void ApplyCurrentCulture()
        {
            ApplyCulture(CurrentLanguage);
        }

        public static void SetCulture(string culture)
        {
            culture = Normalize(culture);
            var context = HttpContext.Current;
            if (context != null && context.Session != null)
            {
                context.Session[SessionKey] = culture;
            }

            if (context != null && context.Response != null)
            {
                var cookie = new HttpCookie(CookieName, culture)
                {
                    HttpOnly = true,
                    Expires = DateTime.Now.AddYears(1)
                };
                context.Response.Cookies.Set(cookie);
            }

            ApplyCulture(culture);
        }

        public static string Normalize(string culture)
        {
            if (string.IsNullOrWhiteSpace(culture))
            {
                return Arabic;
            }

            culture = culture.Trim().ToLowerInvariant();
            if (culture.StartsWith("en"))
            {
                return English;
            }

            return Arabic;
        }

        public static bool IsSupported(string culture)
        {
            if (string.IsNullOrWhiteSpace(culture))
            {
                return false;
            }

            culture = culture.Trim().ToLowerInvariant();
            return culture.StartsWith("ar") || culture.StartsWith("en");
        }

        private static void ApplyCulture(string culture)
        {
            var cultureInfo = new CultureInfo(culture == English ? "en-US" : "ar-SA");
            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;
        }
    }
}
