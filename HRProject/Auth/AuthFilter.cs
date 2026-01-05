using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace EazyCash.Auth
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class AuthAttribute : ActionFilterAttribute // AuthorizeAttribute, IAuthorizationFilter
    {
        //  private readonly string _someFilterParameter;
        //private readonly UserManager<ApplicationUser> _userManager;
        //private readonly SignInManager<ApplicationUser> _signInManager;
        //private readonly RoleManager<IdentityRole> _roleManager;
        public bool allowall { get; set; } = false;
        public bool AllowAnonymous { get; set; } = false;
        //public AuthAttribute()
        //{
        //    //_userManager = userManager;
        //    //_roleManager = roleManager;
        //}

        public   override void OnActionExecuting(ActionExecutingContext context)
        {

            //if (burgerExists)
            //{
            //    filterContext.Result = new RedirectToRouteResult(
            //        new System.Web.Routing.RouteValueDictionary {
            //            {"controller", "Inspection"}, {"action", "Index"}
            //        }
            //    );
            //}
            var descriptor = context.ActionDescriptor as ControllerActionDescriptor;
            var aname = descriptor?.ActionName;
            var cname = descriptor?.ControllerName;
                   
            if (AllowAnonymous ||
                //   (aname?.ToLower() == "index" && cname?.ToLower() == "home") ||aname?.ToLower() == "getname" ||
                (aname?.ToLower() == "login" && cname?.ToLower() == "account")|| (aname?.ToLower() == "getname" && cname?.ToLower() == "account"))
            {
                base.OnActionExecuting(context);
                return;
            }
               
            var user = context.HttpContext.User;
            var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
            var myuser =   userManager.GetUserAsync(user).Result;
            if (myuser == null || !myuser.IsActive)
            {
                context.Result = new RedirectToRouteResult(new RouteValueDictionary(new { action = "login", controller = "account" }));


            }

          //  if (!allowall && !(myuser?.IsAdmin??false))
            {
             //   context.Result = new RedirectToRouteResult(new RouteValueDictionary(new { action = "login", controller = "account" }));
            }
            //ontext.Result = new UnauthorizedResult();  //new StatusCodeResult((int)System.Net.HttpStatusCode.Forbidden);
            base.OnActionExecuting(context);
            return;

            //if (!user.Identity.IsAuthenticated)
            //{

            //    // it isn't needed to set unauthorized result 
            //    // as the base class already requires the user to be authenticated
            //    // this also makes redirect to a login page work properly
            //    context.Result = new UnauthorizedResult();
            //    return;
            //}

        }
    }
}
//    public async void OnAuthorization(AuthorizationFilterContext context)
        //    {
        //        var user = context.HttpContext.User;
        //        var userManager = context.HttpContext.RequestServices.GetService<UserManager<ApplicationUser>>();
        //        var myuser = await userManager.GetUserAsync(user);

        //        context.Result = new StatusCodeResult((int)System.Net.HttpStatusCode.Forbidden);

        //        return;

        //        //if (!user.Identity.IsAuthenticated)
        //        //{

        //        //    // it isn't needed to set unauthorized result 
        //        //    // as the base class already requires the user to be authenticated
        //        //    // this also makes redirect to a login page work properly
        //        //    context.Result = new UnauthorizedResult();
        //        //    return;
        //        //}

        //        // you can also use registered services
        //        //  

        //        //var isAuthorized = someService.IsUserAuthorized(user.Identity.Name, _someFilterParameter);
        //        //if (!isAuthorized)
        //        //{
        //        //    context.Result = new StatusCodeResult((int)System.Net.HttpStatusCode.Forbidden);
        //        //    return;
        //        //}
        //    }
        //}
 

//public void OnAuthentication(AuthenticationContext filterContext)
//      {
//          if (string.IsNullOrEmpty(Convert.ToString(filterContext.HttpContext.Session["UserName"])))
//          {
//              filterContext.Result = new HttpUnauthorizedResult();
//          }
//      }
//      public void OnAuthenticationChallenge(AuthenticationChallengeContext filterContext)
//      {
//          if (filterContext.Result == null || filterContext.Result is HttpUnauthorizedResult)
//          {
//              //Redirecting the user to the Login View of Account Controller  
//              filterContext.Result = new RedirectToRouteResult(
//                  new RouteValueDictionary
//                  {
//                      { "controller", "Account" },
//                      { "action", "Login" }
//                  });
//          }
//      }
//  }