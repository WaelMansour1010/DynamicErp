using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EazyCash.Auth;
using EazyCash.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using EazyCash.Models;
using Branch = EazyCash.Data.Branch;
using Microsoft.AspNetCore.Identity;
using System.Net.Mail;

namespace EazyCash.Controllers
{
    [Auth()]
    public class UsersController : Controller
    {
       
        private readonly dbManager db;
        private readonly Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> _userManager;
        public UsersController(  dbManager _db, Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager )
        {
            _userManager = userManager;

            db = _db;
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {

            var data = await db.QueryAsync<UserViewModel>(@"SELECT AspNetUsers.Id,
       AspNetUsers.UserName,
       AspNetUsers.IsAdmin,
       AspNetUsers.IsActive,
       AspNetUsers.BranchId, Branch.Name BranchName
FROM AspNetUsers
LEFT OUTER JOIN Branch ON  AspNetUsers.BranchId  = Branch.Code 
;
");
             
            
            return View(data);


        }

        private static bool IsValid(string email)
        {
            var valid = true;

            try
            {
                var emailAddress = new MailAddress(email);
            }
            catch
            {
                valid = false;
            }

            return valid;
        }
        [HttpPost]
              public async Task<IActionResult> Create( UserViewModel model)
        {
            ModelState.Remove("IsAdmin");
            ModelState.Remove("IsActive");
            ModelState.Remove("BranchName");
  ModelState.Remove("Branches");
 ModelState.Remove("Id");
 
            if (ModelState.IsValid)
            {
                //if (model.UserName.Contains("@"))
                //{
                //    ModelState.AddModelError("UserName", "UserName is invalid remove @ sign");
                //    return View(model);
                //}
            

                if (string.IsNullOrEmpty(model.UserName))
                {
                    ModelState.AddModelError("UserName", "UserName is empty  ");
                }
               
                if (IsValid(model.UserName))
                {
                    ModelState.AddModelError("UserName", "UserName is invalid ");
                    return View(model);
                }

                var mail = $"{model.UserName}@Caishny.com";
                var user = new ApplicationUser { UserName = mail, 
                    Email = mail, IsAdmin = model.IsAdmin,
                    IsActive = model.IsActive , BranchId = model.BranchId};

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Users");
                  
                    return RedirectToAction(nameof(HomeController.Index), "Users");
                }
             //   AddErrors(result);
            }

            var brnch = await db.QueryAsync<Branch>("SELECT * FROM Branch");
         
            model.Branches = (from b in brnch
                select

                    new SelectListItem
                    {
                        Text = b.Name,
                        Value = b.Code
                    }).ToList();
            return View(model);
           // return View(model);
        }
       
        public async Task<IActionResult> Create()
        {
            var brnch = await db.QueryAsync<Branch>("SELECT * FROM Branch");
            var model = new UserViewModel();
            model.Branches = (from b in brnch
                              select

                                  new SelectListItem
                                  {
                                      Text = b.Name,
                                      Value = b.Code
                                  }).ToList();
            model.IsActive = true;
            model.IsAdmin = false;
            return View(model);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (id == null  )
            {
                return NotFound();
            }

            var user = await db.GetFirstOrDefaultAsync<UserViewModel>(@"SELECT AspNetUsers.Id,
       AspNetUsers.UserName,
       AspNetUsers.IsAdmin,
       AspNetUsers.IsActive,
       AspNetUsers.BranchId, Branch.Name BranchName
FROM AspNetUsers
LEFT OUTER JOIN Branch ON  AspNetUsers.BranchId  = Branch.Code 
WHERE Id = @id;
", new
            {
                id = id
            });
            var brnch = await db.QueryAsync<Branch>("SELECT * FROM Branch");
            user.Branches = (from b in brnch
                select

                    new SelectListItem
                    {
                        Text = b.Name,
                        Value = b.Code
                    }).ToList();

          
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id,  UserViewModel user)
        {
            if (id != user.Id)
            {
                return NotFound();
            }

        //   ModelState.Remove("BranchName");

        //  if (ModelState.IsValid)
            {
                try
                {
                    if (IsValid(user.UserName))
                    {
                        ModelState.AddModelError("UserName", "UserName is invalid ");
                        return View(user);
                    }
                    var dbuser = await db.GetFirstOrDefaultAsync<AspNetUser>(@"SELECT *
FROM AspNetUsers
WHERE Id = @id;
", new
                    {
                        id = id
                    });



                     
                //    dbuser.UserName = user.UserName;
                    dbuser.IsAdmin = user.IsAdmin;
                    dbuser.BranchId = user.BranchId; 
                    dbuser.IsActive = user.IsActive;
                  await   db.UpdateObjAsync(dbuser);
                    
                }
                catch (DbUpdateConcurrencyException)
                {
                    if ( !( await UserExists(user.Id)))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }


            var model = new UserViewModel()
            {
                UserName = user.UserName,
                IsAdmin = user.IsAdmin,
                BranchId = user.BranchId,
                Id = user.Id,
                IsActive = user.IsActive
            };
            var brnch = await db.QueryAsync<Branch>("SELECT * FROM Branch");
            model.Branches = (from b in brnch
                select

                    new SelectListItem
                    {
                        Text = b.Name,
                        Value = b.Code
                    }).ToList();

            return View(model);
        }

       

        private async Task<bool> UserExists(string id)
        {
            var user =   db.GetFirstOrDefaultAsync<UserViewModel>(@"SELECT Id,
       UserName,
       IsAdmin,
       IsActive,
       BranchId
FROM AspNetUsers
WHERE Id = @id;
", new
            {
                id = id
            });





            return user != null;
        }
    }
}
