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
    
 
    public class BranchController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة الفروع",
                EnAction = "Index",
                ControllerName = "Branch",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("Branch", "View", "Index",null, null, "الفروع");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<Branch> branches;

            if (string.IsNullOrEmpty(searchWord))
            {
                branches = db.Branches.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Branches.Where(s => s.IsDeleted == false).Count();
            }
            else
            {
                branches = db.Branches.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.CreationDate.ToString().Contains(searchWord) || s.Mobile.Contains(searchWord) || s.ERPUser.Name.Contains(searchWord) || s.Notes.Contains(searchWord) || s.Address.Contains(searchWord) || s.Address.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = branches.Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(branches.ToList());


        }
        [SkipERPAuthorize]
        public JsonResult SetCodeNum()
        {
            var code = QueryHelper.CodeLastNum("Branch");
            return Json(code + 1, JsonRequestBehavior.AllowGet);
        }


        public ActionResult AddEdit(int? id)
      {

            if (id == null)
            {
                Branch NewObj = new Branch();
                ViewBag.CompanyId = new SelectList(db.Companies.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
               
                return View(NewObj);
            }
            Branch branch = db.Branches.Find(id);
            if (branch == null)
            {
                return HttpNotFound();
            }

        
            ViewBag.CompanyId = new SelectList(db.Companies.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName",branch.CompanyId);
            ViewBag.Next = QueryHelper.Next((int)id, "Branch");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Branch");
            ViewBag.Last = QueryHelper.GetLast("Branch");
            ViewBag.First = QueryHelper.GetFirst("Branch");
            try
            {
                ViewBag.CreationDate = branch.CreationDate.Value.ToString("yyyy-MM-ddTHH:mm");


            }
            catch (Exception)
            {
            }

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل الفرع",
                EnAction = "AddEdit",
                ControllerName = "Branch",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = branch.Id,
                ArItemName = branch.ArName,
                EnItemName = branch.EnName,
                CodeOrDocNo = branch.Code
            });
            return View(branch);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddEdit(Branch branch, string newBtn)
        {
            if (ModelState.IsValid)
            {
                var id = branch.Id;
                branch.IsDeleted = false;
                if (branch.Id > 0)
                {
                    branch.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                    db.Entry(branch).State = EntityState.Modified;

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("Branch", "Edit", "AddEdit", id, null, "الفروع");

                    
                }
                else
                {
                    branch.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    branch.Code= (QueryHelper.CodeLastNum("Branch") + 1).ToString();
                    branch.IsActive = true;
                    db.Branches.Add(branch);

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("Branch", "Add", "AddEdit", branch.Id, null, "الفروع");

                }
                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {

                    var mod = ModelState.First(c => c.Key == "Code");  // this

                    mod.Value.Errors.Add("هذا الكود موجود من قبل");
                    ViewBag.CompanyId = new SelectList(db.Companies.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "ArName");

                    return View(branch);
                }
                    QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id> 0 ? "تعديل الفرع" : "اضافة الفرع",
                    EnAction = "AddEdit",
                    ControllerName = "Branch",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = branch.Id ,
                    ArItemName = branch.ArName,
                    EnItemName = branch.EnName,
                    CodeOrDocNo = branch.Code
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
            ViewBag.CompanyId = new SelectList(db.Companies.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");

            return View(branch);
        }


        [HttpPost, ActionName("Delete")]
  
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                Branch branch = db.Branches.Find(id);
                branch.IsDeleted = true;
                branch.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(branch).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف الفرع",
                    EnAction = "AddEdit",
                    ControllerName = "Branch",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = branch.EnName,
                    ArItemName = branch.ArName,
                    CodeOrDocNo = branch.Code
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("Branch", "Delete", "Delete", id, null, "الفروع");

           

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
                Branch branch = db.Branches.Find(id);
                if (branch.IsActive == true)
                {
                    branch.IsActive = false;
                }
                else
                {
                    branch.IsActive = true;
                }
                branch.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(branch).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)branch.IsActive ? "تنشيط الفرع" : "إلغاء الفرع",
                    EnAction = "AddEdit",
                    ControllerName = "Branch",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = branch.Id,
                    EnItemName = branch.EnName,
                    ArItemName = branch.ArName,
                    CodeOrDocNo = branch.Code
                });
                ////-------------------- Notification-------------------------////
                if (branch.IsActive == true)
                {
                    Notification.GetNotification("Branch", "Activate/Deactivate", "ActivateDeactivate", id, true, "الفروع");
                }
                else
                {

                    Notification.GetNotification("Branch", "Activate/Deactivate", "ActivateDeactivate", id, false, "الفروع");
                }
               

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
