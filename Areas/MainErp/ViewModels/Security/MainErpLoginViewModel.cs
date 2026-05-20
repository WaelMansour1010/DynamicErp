namespace MyERP.Areas.MainErp.ViewModels.Security
{
    public class MainErpLoginViewModel
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string ReturnUrl { get; set; }
        public string ErrorMessage { get; set; }
        public string CurrentDatabaseName { get; set; }
        public bool IsDebugDatabaseOverrideEnabled { get; set; }
        public MyERP.Models.Branding.LoginBrandingContext Branding { get; set; }
    }
}
