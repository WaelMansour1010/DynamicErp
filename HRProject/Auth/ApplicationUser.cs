using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
//using BarakaBot;

namespace EazyCash.Auth
{
    public class ApplicationUser : IdentityUser
    {
        [StringLength(20)]
        public string? BranchId { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsActive { get; set; }

    }
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

        }
    }


    public enum PermissionAction
    {
        Add = 1,
        Update = 2,
        Delete = 4

    }
    //Extenting from AuthorizeAttribute or Attribute is upto user choice.
    //You can consider using AuthorizeAttribute if you want to use the predefined properties and functions from Authorize Attribute.
    //public class AuthorizePermission : AuthorizeAttribute, IAuthorizationFilter
    //{
    //    public bool allowall { get; set; } = false;
    //    public string Permissions { get; set; } //Permission string to get from controller

    //    public void OnAuthorization(AuthorizationFilterContext context)
    //    {

    //        return;

            
    //        //List<string> orderaction = new List<string> { "GetItemFromServer", "PostItemToServer", "FillDeleveryData",
    //        //    "Get","SaveOrderData" ,"AddMoreToOrder","CancelOrder","VIPCustomer","SaveRat","RestRating","GetaddByItemSize"};
    //        //var controller = context.ActionDescriptor.RouteValues["controller"];
    //        //var action = context.ActionDescriptor.RouteValues["action"];
    //        //if (controller == "Order") return;
    //        //if (controller == "Item" && orderaction.Contains(action)) return;

    //        //if (allowall)
    //        //{
    //        //    return;
    //        //}


    //        //var db = context.HttpContext.RequestServices.GetRequiredService<ChefBotSystem.Data.BarakadbModel>();
    //        //var usermanger = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
    //        ////   var usermanger = context.HttpContext.RequestServices.GetService(typeof(UserManager<ApplicationUser>)) as UserManager<ApplicationUser>;

    //        //var user = usermanger.GetUserAsync(context.HttpContext.User).Result;

    //        //if (user == null)
    //        //{

    //        //    context.Result = new RedirectResult("/Account/Login");
    //        //    return;
    //        //}
    //        //if (!user.IsActive)
    //        //{


    //        //    context.HttpContext.Items["actionmsg"] = "active";
    //        //    context.Result = new RedirectResult("/Account/Login");
    //        //    return;
    //        //}

    //        //if (user.IsAdmin)
    //        //{

    //        //    return;
    //        //}

            
    //        //var currentAction = db.Menus.Where(t => t.ControllerName == controller && t.ActionName == action).FirstOrDefault();
    //        //int menuId = -1;

    //        //if (currentAction != null)
    //        //{
    //        //    menuId = currentAction.Id;
    //        //}
    //        //else
    //        //{
    //        //    currentAction = db.Menus.Where(t => t.ControllerName == controller && t.ActionName == Permissions).FirstOrDefault();
    //        //    if (currentAction != null)
    //        //    {
    //        //        menuId = currentAction.Id;
    //        //    }
    //        //    else
    //        //    {
    //        //        context.HttpContext.Items["actionmsg"] = "per";

    //        //        context.Result = new RedirectResult("/Account/Login");

    //        //        return;
    //        //    }
    //        //}


    //        //var useraction = db.UserMenus.FirstOrDefault(t => t.MenuId == menuId && t.UserId == user.Id);
    //        //if (useraction == null)
    //        //{
    //        //    context.HttpContext.Items["actionmsg"] = "per";
    //        //    context.Result = new RedirectResult("/Account/Login");
    //        //    return;
    //        //}





            
    //    }
    //}
}
