using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using System.Security.Claims;

namespace MyERP.Controllers.SystemSettings
{
    
    public class UserController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة المستخدمين",
                EnAction = "Index",
                ControllerName = "User",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("User", "View", "Index",null, null, "المستخدمين");

            //int pageid = db.Get_PageId("User").SingleOrDefault().Value;
            //var actionId = db.PageActions.FirstOrDefault(c => c.Action == "Index" && c.EnName == "View" && c.PageId == pageid).Id;
            //int userid = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            //var UserName = User.Identity.Name;
            //db.Sp_OccuredNotification(actionId, $"بفتح شاشة المستخدمين  {UserName}قام المستخدم  ");
            //////////////////-----------------------------------------------------------------------

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            ViewBag.Count = db.Users.Where(c => c.IsDeleted == false).Count();

            if (string.IsNullOrEmpty(searchWord))
            {
                var users = db.Users.Where(d => d.IsDeleted == false).OrderBy(d => d.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                return View(users.ToList());
            }
            else
            {
                var users = db.Users.Where(d => d.IsDeleted == false && (d.Name.Contains(searchWord) || d.UserName.Contains(searchWord))).OrderBy(d => d.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                return View(users.ToList());
            }

        }


    
        public ActionResult AddEdit(int? id)
        {

            if (id == null)
            {
                User NewObj = new User();
                return View(NewObj);
            }
            User user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل المستخدم ",
                EnAction = "AddEdit",
                ControllerName = "User",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = user.Id,
                ArItemName = user.Name,
        
            });
            ViewBag.Next = QueryHelper.Next((int)id, "Users");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Users");
            ViewBag.Last = QueryHelper.GetLast("Users");
            ViewBag.First = QueryHelper.GetFirst("Users");
            return View(user);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddEdit(User user, string newBtn)
        {
            if (ModelState.IsValid)
            {
                var id = user.Id;
                user.IsDeleted = false;
                if (user.Id > 0)
                {
                    db.Entry(user).State = EntityState.Modified;

                    //////-------------------- Notification-------------------------////
                    Notification.GetNotification("User", "Edit", "AddEdit", id, null, "المستخدم");

                    //int pageid = db.Get_PageId("User").SingleOrDefault().Value;
                    //var actionId = db.PageActions.FirstOrDefault(c => c.Action == "AddEdit" && c.EnName == "Edit" && c.PageId == pageid).Id;
                    //int userid = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    //var UserName = User.Identity.Name;
                    //db.Sp_OccuredNotification(actionId, $"بتعديل بيانات في شاشة المستخدم  {UserName}قام المستخدم ");
                    //////////////////-----------------------------------------------------------------------
                }
                else
                {
                    user.IsActive = true;
                    db.Users.Add(user);
                    //-------------------- Notification-------------------------////
                    Notification.GetNotification("User", "Add", "AddEdit", id, null, "المستخدم");

                    //int pageid = db.Get_PageId("User").SingleOrDefault().Value;
                    //var actionId = db.PageActions.FirstOrDefault(c => c.Action == "AddEdit" && c.EnName == "Add" && c.PageId == pageid).Id;
                    //int userid = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    //var UserName = User.Identity.Name;
                    //db.Sp_OccuredNotification(actionId, $"باضافة بيانات في شاشة المستخدم  {UserName}قام المستخدم  ");

                    ////////////////-----------------------------------------------------------------------

                }

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id> 0 ? "تعديل بيانات مستخدم" : "اضافة مستخدم",
                    EnAction = "AddEdit",
                    ControllerName = "User",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = user.Id > 0 ? user.Id : db.Users.Max(i => i.Id),
                    ArItemName = user.Name,
                  
                });
                if (newBtn == "saveAndNew")
                {
                    return RedirectToAction("AddEdit");

                }
                else
                {
                    return RedirectToAction("Index");
                }
            }

            return View(user);
        }


        [HttpPost, ActionName("Delete")]
       
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                User user = db.Users.Find(id);
                user.IsDeleted = true;
                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف مستخدم",
                    EnAction = "AddEdit",
                    ControllerName = "User",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    ArItemName = user.Name,
             
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("User", "Delete", "Delete", id, null, "المستخدم");

                //int pageid = db.Get_PageId("User").SingleOrDefault().Value;
                //var actionId = db.PageActions.FirstOrDefault(c => c.Action == "Delete" && c.EnName == "Delete" && c.PageId == pageid).Id;
                //int userid = db.UserNotifications.FirstOrDefault(c => c.ActionId == actionId && c.PageId == pageid).UserId.Value;
                //var UserName = User.Identity.Name;
                //db.Sp_OccuredNotification(actionId, $"بحذف بيانات في شاشة المستخدم  {UserName}قام المستخدم  ");
                //////////////////-----------------------------------------------------------------------


                return Content("true");
            }
            catch (Exception)
            {

                throw;
            }
        }

        [HttpPost]
        public ActionResult ActivateDeactivate(int id)
        {
            try
            {
                User user = db.Users.Find(id);
                if (user.IsActive == true)
                {
                    user.IsActive = false;
                }
                else
                {
                    user.IsActive = true;
                }

                db.Entry(user).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)user.IsActive ? "تنشيط بيانات المستخدم" : "إلغاء تنشيط المستخدم",
                    EnAction = "AddEdit",
                    ControllerName = "User",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = user.Id,  
                    ArItemName = user.Name,
                
                });
                ////-------------------- Notification-------------------------////
                if (user.IsActive == true)
                {
                    Notification.GetNotification("User", "Activate/Deactivate", "ActivateDeactivate", id, true, "المستخدمين");
                }
                else
                {

                    Notification.GetNotification("User", "Activate/Deactivate", "ActivateDeactivate", id, false, "المستخدمين");
                }
                //int pageid = db.Get_PageId("User").SingleOrDefault().Value;
                //var actionId = db.PageActions.FirstOrDefault(c => c.Action == "ActivateDeactivate" && c.EnName == "Activate/Deactivate" && c.PageId == pageid).Id;
                //int userid = db.UserNotifications.FirstOrDefault(c => c.ActionId == actionId && c.PageId == pageid).UserId.Value;
                //var UserName = User.Identity.Name;
                //db.Sp_OccuredNotification(actionId, (bool)user.IsActive ? $" تنشيط  في شاشة المستخدمين{UserName}قام المستخدم  " : $"إلغاء تنشيط  في شاشة المستخدمين{UserName}قام المستخدم  ");
                //////////////////-----------------------------------------------------------------------

                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }


    }
}
