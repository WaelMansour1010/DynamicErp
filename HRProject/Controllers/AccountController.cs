//using BarakaBot;

using EazyCash.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
//using Microsoft.AspNet.Identity;
using IdentityResult = Microsoft.AspNetCore.Identity.IdentityResult;

namespace EazyCash.Controllers
{


    [Auth()]
    public class AccountController : Controller
    {
        private readonly Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly Microsoft.AspNetCore.Identity.RoleManager<IdentityRole> _roleManager;
        private readonly ILogger _logger;
        readonly HROnlineModel.HROnlineModel db;
        public AccountController(Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager,
                   SignInManager<ApplicationUser> signInManager, Microsoft.AspNetCore.Identity.RoleManager<IdentityRole> roleManager,
                   ILogger<AccountController> logger,
                   HROnlineModel.HROnlineModel _db)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _roleManager = roleManager;
            db = _db;
        }

        [HttpGet]

        [Auth(AllowAnonymous = true)]
        public IActionResult Register()
        {

            return View();
        }

        [HttpPost]

        [Auth(AllowAnonymous = true)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {

            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email, IsAdmin = false, IsActive = false };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Users");
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction(nameof(HomeController.Index), "Home");
                }
                AddErrors(result);
            }


            return View(model);
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        [Auth(AllowAnonymous = true)]
        [HttpGet]
        public async Task<IActionResult> getname(string code)
        {
            var empname = await db.TblEmployees.Where(t => t.Emp_Code == code).Select(t =>new  
                {
t.Emp_Namee,t.Emp_Name
                } ).AsNoTracking()
                .FirstOrDefaultAsync();
            if (empname == null)
            {
                return Json(new { msg = " not found"});
            }
            else
            {
                return Json(new { isok = true,name=string.IsNullOrEmpty(empname.Emp_Namee)?empname.Emp_Name:empname.Emp_Namee });
            }
            
        }

        [HttpGet]
        public async Task<IActionResult> Changeps(string txt1, string txt2, string txt3)
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
                return Json(new { msg = "Not Loged user" });
            }
            if (txt2 != txt3)
            {
                return Json(new { msg = "new password not match" });
            }
            var result = await _userManager.ChangePasswordAsync(user, txt1, txt2);
            if (result.Succeeded)
            {
                var empId = 0; 
                
                var Id = user.UserName.Split("@")?[0]; //$"{employee?.Emp_ID}@page.com";
                int.TryParse(Id, out empId);
                var employee = await db.TblEmployees.FirstOrDefaultAsync(t => t.Emp_ID == empId);
                if (employee != null)
                {
                    employee.PasswordChanged = true;
                    employee.PasswordChangedDate = DateTime.Now;
                    await  db.SaveChangesAsync();
                }
                return Json(new { isok = true });

            }
            else
            {
                await _signInManager.SignOutAsync();
                var errors = result.Errors.Select(t => t.Description).ToArray();
                return Json(new { msg = "  .. system wil logout " + string.Join(",", errors), reload = 1 });
            }
        }


        [HttpGet]

        [Auth(AllowAnonymous = true)]
        public async Task<IActionResult> Login()
        {
            var vm = new LoginViewModel();
            var users = await db.TblEmployees.Select(t => new SelectListItem { Text = t.Emp_Namee ??t.Emp_Name , Value = t.Emp_Code }).ToListAsync();
            vm.Users = users;

            return View("login", vm);
        }

        [HttpPost]

        [Auth(AllowAnonymous = true)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {

            // if (ModelState.IsValid)
            //   {
           
            var users = await db.TblEmployees.Select(t => new SelectListItem { Text = t.Emp_Namee , Value = t.Emp_Code   , }).ToListAsync();
            model.Users = users;
            var employee = await db.TblEmployees.FirstOrDefaultAsync(t => t.Emp_Code == model.EmployeeCode);
            var username = $"{employee?.Emp_ID}@page.com";
            var user = await _signInManager.UserManager.FindByNameAsync(username);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt. App User");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "user not active");
                return View(model);
            }

            // var check =  await _signInManager.UserManager.CheckPasswordAsync(user, model.Password);

            var result = await _signInManager.PasswordSignInAsync(username, model.Password, false, false);

            if (result.Succeeded)
            {
                return RedirectToAction("index", "Home");
            }

            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }
            //}

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Auth(AllowAnonymous = true)]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }




        // [HttpGet]
        // public async Task<IActionResult> FacebookLogin()
        // {
        //     ViewBag.appkey = CurrentSession.AppId;
        //    return View();
        //}

    }
}