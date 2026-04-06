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
    public class ERPAuthorizeAttribute : System.Web.Mvc.ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext filterContext)
        
        {

        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {

            int userId = 0;
            if (HttpContext.Current.Request.IsAuthenticated)
            {
                userId = int.Parse(((ClaimsIdentity)HttpContext.Current.User.Identity).FindFirst("Id").Value);
                using (MySoftERPEntity db = new MySoftERPEntity())
                {
                    if (!db.ERPUsers.Where(x => x.Id == userId && x.IsDeleted == false && x.IsActive == true && x.IsPasswordReset == false).Any())
                    {
                       
                        HttpContext.Current.Request.GetOwinContext().Authentication.SignOut();
                        return;
                    }
                }
            }
            if (filterContext.ActionDescriptor.ControllerDescriptor.IsDefined(typeof(SkipERPAuthorize), true) || filterContext.ActionDescriptor.IsDefined(typeof(SkipERPAuthorize), true) || filterContext.ActionDescriptor.GetCustomAttributes(typeof(SkipERPAuthorize), false).Any())
            {
                return;
            }
            bool? acces = Log(filterContext.HttpContext.Request.HttpMethod, filterContext.RouteData, filterContext.ActionParameters, userId);
            if (acces != true)
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

        private bool? Log(string methodType, RouteData routeData, IDictionary<string, object> parms, int userId)
        {
            var prm1 = 0;
            try
            {
                var t = parms.FirstOrDefault().Value ;
                int.TryParse((t??"").ToString(), out prm1);
            }
            catch
            {

            }

           
               


            using (MySoftERPEntity db = new MySoftERPEntity())
            {
                int roleId = int.Parse(((ClaimsIdentity)HttpContext.Current.User.Identity).FindFirst("RoleId").Value);
                string controllerName = (string)routeData.Values["controller"];
                string actionName = (string)routeData.Values["action"];
                if (controllerName == "PointOfSale" )
                    controllerName = "SalesInvoice";//inherit salesInvoice privileges
                else if (controllerName == "PosSalesReturn")
                    controllerName = "SalesReturn";//inherit salesReturn privileges
                if (userId == 1 || controllerName == "Report" ||
                    actionName == "Unauthorized" ||
                    controllerName == "LogIn" ||
                    controllerName == "LogOut" ||
                    actionName == "SetCodeNum" ||
                    actionName == "SetDocNum" || 
                    actionName == "GetItemQuantityInEachWarehouse" ||
                    actionName == "ImportDepartments" || 
                    actionName == "IdonWebsiteByDepartmentId" || 
                    controllerName == "Helper")
                {
                    return true;
                }
                //  else if (( (parms.FirstOrDefault().Value != null )  ) && actionName == "AddEdit" && methodType == "GET")
                else if (parms.Any()
                         && parms.FirstOrDefault().Value != null
                          && prm1 != 0
                         && actionName == "AddEdit"
                         && methodType == "GET")
                {
                    var tableName = db.SystemPages.Where(s => s.ControllerName == controllerName && s.IsTransaction == true).Select(s => s.TableName).FirstOrDefault();
                    var up = db.UserPrivileges.Where(u => u.UserId == userId && u.SystemPage.ControllerName == controllerName && u.PageAction.Action == (actionName) && u.PageAction.EnName == "Edit").Select(x => x.Privileged).FirstOrDefault();
                    var privilege = up ?? db.RolePrivileges.Where(u => u.RoleId == roleId && u.SystemPage.ControllerName == controllerName && u.PageAction.Action == (actionName) && u.PageAction.EnName == "Edit").Select(x => x.Privileged).FirstOrDefault();

                    if (tableName != null)
                    {
                        try
                        {
                            //var id = parms["id"];
                            //if (controllerName.Equals("CashReceiptVoucher", StringComparison.InvariantCultureIgnoreCase) && 
                            //  actionName.Equals("AddEdit" , StringComparison.InvariantCultureIgnoreCase) && parms.ContainsKey("cid"))
                            //{
                            //    id = parms["cid"];
                            //}
                            var depId = db.Database
                                .SqlQuery<int?>($"select DepartmentId from [{tableName}] where Id={ parms["id"]}")
                                .FirstOrDefault();
                            var userDepIds = db.Departments.Where(d =>
                                (userId == 1 || db.UserDepartments.Where(u => u.UserId == userId && u.Privilege == true)
                                    .Select(u => u.DepartmentId).Contains(d.Id)) && d.IsDeleted == false &&
                                d.IsActive == true).Select(d => (int?)d.Id);
                            return privilege == true && userDepIds.Contains(depId);
                        }
                        catch (Exception ex)
                        {
                            var r = ex;
                        }
                    }

                    return privilege == true;

                }
                // else if (parms.FirstOrDefault().Value != null && actionName == "AddEdit" && methodType == "POST")
                else if (parms.Any()
                         && parms.FirstOrDefault().Value != null
                         && prm1 != 0
                         && actionName == "AddEdit"
                         && methodType == "POST")
                {
                    var up = db.UserPrivileges.Where(u => u.UserId == userId && u.SystemPage.ControllerName == controllerName && u.PageAction.Action == (actionName) && u.PageAction.EnName == ("Add")).Select(x => x.Privileged).FirstOrDefault();
                    var privilege = up ?? db.RolePrivileges.Where(u => u.RoleId == roleId && u.SystemPage.ControllerName == controllerName && u.PageAction.Action == (actionName) && u.PageAction.EnName == ("Add")).Select(x => x.Privileged).FirstOrDefault();
                    return privilege == true;
                }
                else if (actionName == "AddEdit" && methodType == "GET")
                {
                    var up = db.UserPrivileges.Where(u => u.UserId == userId && u.SystemPage.ControllerName == controllerName && u.PageAction.Action == (actionName) && u.PageAction.EnName == ("Add")).Select(x => x.Privileged).FirstOrDefault();
                    var privilege = up ?? db.RolePrivileges.Where(u => u.RoleId == roleId && u.SystemPage.ControllerName == controllerName && u.PageAction.Action == (actionName) && u.PageAction.EnName == ("Add")).Select(x => x.Privileged).FirstOrDefault();
                    return privilege == true;
                }
                else
                {
                    var up = db.UserPrivileges.Where(u => u.UserId == userId && u.SystemPage.ControllerName == controllerName && u.PageAction.Action == (actionName)).Select(x => x.Privileged).FirstOrDefault();
                    var privilege = up ?? db.RolePrivileges.Where(u => u.RoleId == roleId && u.SystemPage.ControllerName == controllerName && u.PageAction.Action == (actionName)).Select(x => x.Privileged).FirstOrDefault();
                    return privilege == true;
                }
            }
        }

    }

    public class SkipERPAuthorize : System.Web.Mvc.ActionFilterAttribute
    {

    }

   
}
