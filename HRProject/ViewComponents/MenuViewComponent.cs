
using EazyCash.Auth;
using EazyCash.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EazyCash.ViewComponents
{
    public class MenuViewComponent : ViewComponent
    {

        
        public UserManager<ApplicationUser> UserManager { get; }
        private readonly HROnlineModel.HROnlineModel db;
        public MenuViewComponent( UserManager<ApplicationUser> userManager, HROnlineModel.HROnlineModel    _db)
        {
          
            UserManager = userManager;
            db = _db;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var user = await UserManager.GetUserAsync(UserClaimsPrincipal);
            var id =int.Parse( user.UserName.Split("@").First());
            var dbuser = await db.TblEmployees.FirstOrDefaultAsync(t => t.Emp_ID  == id);
//            var model = await db.GetFirstOrDefaultAsync<UserViewModel>(@"SELECT AspNetUsers.Id,
//       AspNetUsers.UserName,
//       AspNetUsers.IsAdmin,
//       AspNetUsers.IsActive,
//       AspNetUsers.BranchId,
//       Branch.Name BranchName
//FROM AspNetUsers
//    LEFT OUTER JOIN Branch
//        ON AspNetUsers.BranchId = Branch.Code
//WHERE AspNetUsers.Id = @id;


//" , new
//            {
//                id =  user.Id 
//            });

           
            
            return View("_Navigation", new UserViewModel()
            {
                UserName = dbuser.Emp_Namee  ?? dbuser.Emp_Name  , 
                Id = dbuser.Emp_ID .ToString() 
                 
            });
        }
    }
}