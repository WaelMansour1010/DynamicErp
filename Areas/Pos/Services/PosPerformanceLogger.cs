using MyERP.Areas.Pos.Controllers;
using MyERP.Areas.Pos.Models;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;

namespace MyERP.Areas.Pos.Services
{
    public static class PosPerformanceLogger
    {
        private const int SlowThresholdMilliseconds = 300;

        public static void LogAction(string actionName, long elapsedMilliseconds, PosUserContext context, string path, string status)
        {
            if (elapsedMilliseconds < SlowThresholdMilliseconds && string.IsNullOrWhiteSpace(status))
            {
                return;
            }

            WriteLine("ACTION", actionName, elapsedMilliseconds, null, context, path, status);
        }

        public static void LogQuery(string actionName, string queryName, long elapsedMilliseconds, int? rowsReturned, PosUserContext context)
        {
            if (elapsedMilliseconds < SlowThresholdMilliseconds)
            {
                return;
            }

            WriteLine("QUERY", actionName, elapsedMilliseconds, rowsReturned, context, queryName, null);
        }

        private static void WriteLine(string type, string name, long elapsedMilliseconds, int? rowsReturned, PosUserContext context, string source, string status)
        {
            try
            {
                var root = HostingEnvironment.MapPath("~/App_Data/Logs");
                if (string.IsNullOrWhiteSpace(root))
                {
                    root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "Logs");
                }

                Directory.CreateDirectory(root);
                var fileName = "pos-performance-" + DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + ".log";
                var path = Path.Combine(root, fileName);

                var line = string.Join(" | ", new[]
                {
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
                    type,
                    "Name=" + Safe(name),
                    "ElapsedMs=" + elapsedMilliseconds.ToString(CultureInfo.InvariantCulture),
                    "Rows=" + (rowsReturned.HasValue ? rowsReturned.Value.ToString(CultureInfo.InvariantCulture) : string.Empty),
                    "UserId=" + (context != null ? context.UserId.ToString(CultureInfo.InvariantCulture) : string.Empty),
                    "BranchId=" + (context != null && context.BranchId.HasValue ? context.BranchId.Value.ToString(CultureInfo.InvariantCulture) : string.Empty),
                    "Source=" + Safe(source),
                    "Status=" + Safe(status)
                });

                File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);
            }
            catch
            {
                // Performance logging must never affect the POS workflow.
            }
        }

        private static string Safe(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Replace("\r", " ").Replace("\n", " ").Replace("|", "/").Trim();
        }
    }

    public sealed class PosPerformanceLogAttribute : ActionFilterAttribute
    {
        private const string StopwatchKey = "DynamicErp.Pos.Performance.Stopwatch";

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (IsPosArea(filterContext))
            {
                filterContext.HttpContext.Items[StopwatchKey] = Stopwatch.StartNew();
            }

            base.OnActionExecuting(filterContext);
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            try
            {
                if (!IsPosArea(filterContext))
                {
                    base.OnActionExecuted(filterContext);
                    return;
                }

                var stopwatch = filterContext.HttpContext.Items[StopwatchKey] as Stopwatch;
                if (stopwatch == null)
                {
                    base.OnActionExecuted(filterContext);
                    return;
                }

                stopwatch.Stop();
                var route = filterContext.RouteData;
                var controller = Convert.ToString(route.Values["controller"], CultureInfo.InvariantCulture);
                var action = Convert.ToString(route.Values["action"], CultureInfo.InvariantCulture);
                var actionName = controller + "." + action;
                var status = filterContext.Exception != null ? filterContext.Exception.GetType().Name : null;
                var request = filterContext.HttpContext != null ? filterContext.HttpContext.Request : null;
                var path = request != null ? request.Path : string.Empty;

                PosPerformanceLogger.LogAction(actionName, stopwatch.ElapsedMilliseconds, GetPosContext(filterContext.HttpContext), path, status);
            }
            catch
            {
                // Performance logging must never affect MVC actions.
            }

            base.OnActionExecuted(filterContext);
        }

        private static bool IsPosArea(ControllerContext context)
        {
            var area = context != null && context.RouteData != null ? context.RouteData.DataTokens["area"] as string : null;
            return string.Equals(area, "Pos", StringComparison.OrdinalIgnoreCase);
        }

        private static PosUserContext GetPosContext(HttpContextBase httpContext)
        {
            try
            {
                return httpContext != null && httpContext.Session != null
                    ? httpContext.Session[PosLoginController.PosContextSessionKey] as PosUserContext
                    : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
