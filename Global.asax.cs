using DevExpress.Security.Resources;
using DevExpress.XtraReports.Web.WebDocumentViewer;
using DevExpress.XtraReports.Configuration;
using DevExpress.XtraReports.Security;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;



namespace MyERP
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            DevExpress.XtraReports.Web.QueryBuilder.Native.QueryBuilderBootstrapper.SessionState = System.Web.SessionState.SessionStateBehavior.Disabled;
            DevExpress.XtraReports.Web.ReportDesigner.Native.ReportDesignerBootstrapper.SessionState = System.Web.SessionState.SessionStateBehavior.Disabled;
            DevExpress.XtraReports.Web.WebDocumentViewer.Native.WebDocumentViewerBootstrapper.SessionState = System.Web.SessionState.SessionStateBehavior.Disabled;
            
            DefaultWebDocumentViewerContainer.UseCachedReportSourceBuilder();
            //WebDocumentViewer.DefaultSettings.ScriptPermissionState = DevExpress.XtraReports.Security.ScriptPermissionManager.;
            
            AreaRegistration.RegisterAllAreas();

            DevExpress.XtraReports.Security.ScriptPermissionManager.GlobalInstance = new DevExpress.XtraReports.Security.ScriptPermissionManager(DevExpress.XtraReports.Security.ExecutionMode.Unrestricted);
            ScriptPermissionManager.GlobalInstance = new ScriptPermissionManager(ExecutionMode.Unrestricted); //
            // #############################################

            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            DevExpress.Web.Mvc.MVCxWebDocumentViewer.StaticInitialize();
            AntiForgeryConfig.UniqueClaimTypeIdentifier = ClaimTypes.Name;

            DevExpress.Web.Mvc.MVCxReportDesigner.StaticInitialize();
        }
    }
}
