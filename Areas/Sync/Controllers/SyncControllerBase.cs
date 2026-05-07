using System;
using System.Configuration;
using System.Web.Mvc;

namespace MyERP.Areas.Sync.Controllers
{
    [SkipERPAuthorize]
    public abstract class SyncControllerBase : Controller
    {
        protected override void OnException(ExceptionContext filterContext)
        {
            if (filterContext == null || filterContext.ExceptionHandled)
            {
                base.OnException(filterContext);
                return;
            }

            if (IsFriendlySyncException(filterContext.Exception))
            {
                filterContext.ExceptionHandled = true;
                filterContext.HttpContext.Response.StatusCode = 503;
                filterContext.HttpContext.Response.TrySkipIisCustomErrors = true;
                filterContext.Result = View("~/Areas/Sync/Views/Shared/FriendlyError.cshtml");
                ViewBag.SyncErrorTitle = "تعذر تحميل بيانات المزامنة";
                ViewBag.SyncErrorMessage = "يرجى مراجعة إعدادات الاتصال والصلاحيات الخاصة بقاعدة بيانات المزامنة. لم يتم عرض أي بيانات اتصال أو كلمات مرور.";
                return;
            }

            base.OnException(filterContext);
        }

        private static bool IsFriendlySyncException(Exception exception)
        {
            while (exception != null)
            {
                if (exception is ConfigurationException || exception is System.Data.SqlClient.SqlException || exception is InvalidOperationException)
                {
                    return true;
                }

                exception = exception.InnerException;
            }

            return false;
        }
    }
}
