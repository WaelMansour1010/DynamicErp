namespace MyERP.Areas.MainErp.Infrastructure.Localization
{
    public static class MainErpEntityLocalization
    {
        public static string Localize(string arabic, string english)
        {
            if (MainErpCultureManager.IsArabic)
            {
                return !string.IsNullOrWhiteSpace(arabic) ? arabic : english;
            }

            return !string.IsNullOrWhiteSpace(english) ? english : arabic;
        }

        public static string AccountDisplay(string accountSerial, string accountNameArabic, string accountNameEnglish)
        {
            var name = Localize(accountNameArabic, accountNameEnglish);
            if (string.IsNullOrWhiteSpace(name))
            {
                return MainErpLocalizationService.T("AccountNotFound");
            }

            return string.IsNullOrWhiteSpace(accountSerial)
                ? name
                : accountSerial + " - " + name;
        }
    }
}
