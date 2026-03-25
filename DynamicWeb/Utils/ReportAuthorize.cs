using MyERP.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace MyERP
{
    public class ReportAuthorize : System.Web.Mvc.ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {

        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            bool acces = Log(filterContext.RouteData);
            if (!acces)
            {
                filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Home", action = "Unauthorized" }));
            }
        }

        public override void OnResultExecuted(ResultExecutedContext filterContext)
        {

        }

        public override void OnResultExecuting(ResultExecutingContext filterContext)
        {

        }

        private bool Log(RouteData routeData)
        {
            string controllerName = (string)routeData.Values["controller"];
            string actionName = (string)routeData.Values["action"];
            if (controllerName == "Report" && actionName == "E_Invoice")
                return true;
            int userId = int.Parse(((ClaimsIdentity)HttpContext.Current.User.Identity).FindFirst("Id").Value);
           
            if (userId == 1 || (controllerName=="Report" && actionName == "Index") || actionName == "CashierInvoice" || actionName == "CashierInvoice2" || actionName== "SalesOrder") 
            {
                return true;
            }

            using (MySoftERPEntity context = new MySoftERPEntity())
            {
                int roleId = int.Parse(((ClaimsIdentity)HttpContext.Current.User.Identity).FindFirst("RoleId").Value);
                string name = (string)routeData.Values["action"];
                int reportId = (context.SystemPages.Where(a => a.EnName == name && a.IsReport == true)).Select(a => a.Id).FirstOrDefault();

                var up = context.UserReports.FirstOrDefault(u => u.UserId == userId && u.ReportId == reportId);

                if (up != null)
                {
                    return true;
                }
                else
                {
                    var rp = context.ERPRoleReports.FirstOrDefault(u => u.UserRoleId == roleId && u.ReportId == reportId);

                    if (rp != null)
                    {
                        return true;

                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }

    }

}
