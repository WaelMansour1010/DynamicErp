using System.Globalization;
using System.Resources;

namespace MyERP.Areas.MainErp.Infrastructure.Localization
{
    public static class MainErpLocalizationService
    {
        private static readonly ResourceManager ResourceManager =
            new ResourceManager("MyERP.Areas.MainErp.Resources.MainErp", typeof(MainErpLocalizationService).Assembly);

        public static string T(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            var culture = new CultureInfo(MainErpCultureManager.CurrentLanguage);
            var value = ResourceManager.GetString(key, culture);
            return string.IsNullOrWhiteSpace(value) ? key : value;
        }

        public static string Localize(string arabic, string english)
        {
            return MainErpEntityLocalization.Localize(arabic, english);
        }
    }
}
