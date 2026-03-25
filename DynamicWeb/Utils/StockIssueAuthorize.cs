using MyERP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace MyERP
{
    public class StockIssueAuthorize : ActionFilterAttribute
    {
        private MySoftERPEntity context = new MySoftERPEntity();
        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {

        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            bool acces = Log(filterContext.HttpContext.Request.HttpMethod, filterContext.RouteData, filterContext.ActionParameters);
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

        private bool Log(string methodType, RouteData routeData, IDictionary<string, object> parms)
        {
            int userId = int.Parse(((ClaimsIdentity)HttpContext.Current.User.Identity).FindFirst("Id").Value);
            int roleId = int.Parse(((ClaimsIdentity)HttpContext.Current.User.Identity).FindFirst("RoleId").Value);
            string actionName = (string)routeData.Values["action"];
            if (actionName == "SetDocNum")
                return true;
            int type = 1;
            try
            {
                type = (int)parms["type"];
            }
            catch { }//if there is no "type" parameter consider it "1"
            string[] pageTypes = { "StockIssueVoucher", "StockDamageVoucher", "StockLossVoucher" };
            string pageCode =  pageTypes[type-1];

            if (userId == 1)
            {
                return true;
            }
            else if (parms.FirstOrDefault().Value != null && actionName == "AddEdit" && methodType == "GET")
            {
                var up = context.UserPrivileges.Where(u => u.UserId == userId && u.SystemPage.Code == pageCode && u.PageAction.Action == (actionName) && u.PageAction.EnName == "Edit").Select(x => x.Privileged).FirstOrDefault();
                var priv = up ?? context.RolePrivileges.Where(u => u.RoleId == roleId && u.SystemPage.Code == pageCode && u.PageAction.Action == (actionName) && u.PageAction.EnName == "Edit").Select(x => x.Privileged).FirstOrDefault();

                var depId = context.Database.SqlQuery<int?>($"select DepartmentId from [StockIssueVoucher] where Id={parms["id"]}").FirstOrDefault();
                var userDepIds = context.Departments.Where(d => (userId == 1 || context.UserDepartments.Where(u => u.UserId == userId && u.Privilege == true).Select(u => u.DepartmentId).Contains(d.Id)) && d.IsDeleted == false && d.IsActive == true).Select(d => (int?)d.Id);
                return priv == true && userDepIds.Contains(depId);
            }
            else if (parms.FirstOrDefault().Value != null && actionName == "AddEdit" && methodType == "POST")
            {
                var up = context.UserPrivileges.Where(u => u.UserId == userId && u.SystemPage.Code == pageCode && u.PageAction.Action == (actionName) && u.PageAction.EnName == "Add").Select(x => x.Privileged).FirstOrDefault();
                var priv = up ?? context.RolePrivileges.Where(u => u.RoleId == roleId && u.SystemPage.Code == pageCode && u.PageAction.Action == (actionName) && u.PageAction.EnName == "Add").Select(x => x.Privileged).FirstOrDefault();
                return priv == true;
            }
            else if (actionName == "AddEdit" && methodType == "GET")
            {
                var up = context.UserPrivileges.Where(u => u.UserId == userId && u.SystemPage.Code == pageCode && u.PageAction.Action == (actionName) && u.PageAction.EnName == "Add").Select(x => x.Privileged).FirstOrDefault();
                var priv = up ?? context.RolePrivileges.Where(u => u.RoleId == roleId && u.SystemPage.Code == pageCode && u.PageAction.Action == (actionName) && u.PageAction.EnName == "Add").Select(x => x.Privileged).FirstOrDefault();
                return priv == true;
            }
            else
            {
                var up = context.UserPrivileges.Where(u => u.UserId == userId && u.SystemPage.Code == pageCode && u.PageAction.Action == actionName).Select(x => x.Privileged).FirstOrDefault();
                var priv = up ?? context.RolePrivileges.Where(u => u.RoleId == roleId && u.SystemPage.Code == pageCode && u.PageAction.Action == actionName).Select(x => x.Privileged).FirstOrDefault();
                return priv == true;
            }

        }
    }
}