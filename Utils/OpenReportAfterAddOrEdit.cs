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
        public class OpenReportAfterAddOrEdit : System.Web.Mvc.ActionFilterAttribute
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
                string controllerName = (string)routeData.Values["controller"];
                string actionName = (string)routeData.Values["action"];


                if (parms.FirstOrDefault().Value != null && actionName == "AddEdit" && methodType == "GET")
                {
                    var up = context.UserPrivileges.FirstOrDefault(u => u.UserId == userId && u.PageAction.Action == (actionName) && u.PageAction.EnName == "Edit").Privileged;

                    return up != null ? (bool)up : (bool)context.RolePrivileges.FirstOrDefault(u => u.RoleId == roleId && u.PageAction.Action == (actionName) && u.PageAction.EnName == "Edit").Privileged;

                }
                else if (parms.FirstOrDefault().Value != null && actionName == "AddEdit" && methodType == "POST")
                {
                    var up = context.UserPrivileges.FirstOrDefault(u => u.UserId == userId && u.PageAction.Action == (actionName) && u.PageAction.EnName == ("Add")).Privileged;

                    return up != null ? (bool)up : (bool)context.RolePrivileges.FirstOrDefault(u => u.RoleId == roleId && u.PageAction.Action == (actionName) && u.PageAction.EnName == ("Add")).Privileged;
                }
                else if (actionName == "AddEdit" && methodType == "GET")
                {
                    var up = context.UserPrivileges.FirstOrDefault(u => u.UserId == userId && u.PageAction.Action == (actionName) && u.PageAction.EnName == ("Add")).Privileged;

                    return up != null ? (bool)up : (bool)context.RolePrivileges.FirstOrDefault(u => u.RoleId == roleId && u.PageAction.Action == (actionName) && u.PageAction.EnName == ("Add")).Privileged;
                }
                else
                {
                    var up = context.UserPrivileges.FirstOrDefault(u => u.UserId == userId && u.PageAction.Action == (actionName)).Privileged;

                    return up != null ? (bool)up : (bool)context.RolePrivileges.FirstOrDefault(u => u.RoleId == roleId && u.PageAction.Action == (actionName)).Privileged;
                }

            }
        }
    }
