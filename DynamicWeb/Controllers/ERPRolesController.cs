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

namespace MyERP.Controllers
{
    
    public class ERPRolesController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();


        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة مجموعات المستخدمين",
                EnAction = "Index",
                ControllerName = "ERPRoles",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            //////-------------------- Notification-------------------------////
            
            Notification.GetNotification("ERPRoles", "View", "Index", null, null, "مجموعات المستخدمين");


            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<ERPRole> erpRoles;

            if (string.IsNullOrEmpty(searchWord))
            {
                erpRoles = db.ERPRoles.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.ERPRoles.Where(s => s.IsDeleted == false).Count();
            }
            else
            {
                erpRoles = db.ERPRoles.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.Notes.Contains(searchWord) )).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.ERPRoles.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord)|| s.Notes.Contains(searchWord) )).Count();

            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(erpRoles.ToList());

        }
        [SkipERPAuthorize]
        public JsonResult SetCodeNum()
        {
            var code = QueryHelper.CodeLastNum("ERPRole");
            return Json(code + 1, JsonRequestBehavior.AllowGet);
        }



        public ActionResult AddEdit(int? id)
        {
            ViewBag.Id = id;
            if (id == null)
            {
                ERPRole Newobj = new ERPRole();

           
                return View(Newobj);
            }
            ERPRole erpRole = db.ERPRoles.Find(id);
            if (erpRole == null)
            {
                return HttpNotFound();
            }
            else
            {
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "فتح تفاصيل مجموعةالمستخدمين",
                    EnAction = "AddEdit",
                    ControllerName = "ERPRoles",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "GET",
                    SelectedItem = erpRole.Id,
                    ArItemName = erpRole.ArName,
                    EnItemName = erpRole.EnName,
                    CodeOrDocNo = erpRole.Code
                });
            
                ViewBag.Next = QueryHelper.Next((int)id, "ERPRole");
                ViewBag.Previous = QueryHelper.Previous((int)id, "ERPRole");
                ViewBag.Last = QueryHelper.GetLast("ERPRole");
                ViewBag.First = QueryHelper.GetFirst("ERPRole");
                return View(erpRole);
            }

        }
      
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public ActionResult AddEdit(ERPRole erpRole, string newBtn)
        {
            if (ModelState.IsValid)
            {
                erpRole.IsDeleted = false;
                var id = erpRole.Id;
                if (erpRole.Id > 0)
                {
                    erpRole.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    db.Entry(erpRole).State = EntityState.Modified;


                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("ERPRoles", "Edit", "AddEdit", id, null, "مجموعات المستخدمين");

                    //////////////////-----------------------------------------------------------------------

                    // Add DB Change
                    QueryHelper.AddDBChange(new DBChange()
                    {
                        TableName = "ERPRole",
                        SelectedId = erpRole.Id,
                        IsMasterChange = true,
                        IsNew = false,
                        IsTransaction = false
                    });
                }
                else
                {
                    erpRole.IsActive = true;
                    erpRole.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    erpRole.Code= (QueryHelper.CodeLastNum("ERPRole") + 1).ToString();
                    db.ERPRoles.Add(erpRole);
                    db.SaveChanges();

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("ERPRoles", "Add", "AddEdit", id, null, "مجموعات المستخدمين");
                    ////////////////-----------------------------------------------------------------------

                    // Add DB Change
                    var SelectedId = db.ERPRoles.OrderByDescending(r => r.Id).FirstOrDefault().Id;
                    QueryHelper.AddDBChange(new DBChange()
                    {
                        TableName = "ERPRole",
                        SelectedId = SelectedId,
                        IsMasterChange = true,
                        IsNew = true,
                        IsTransaction = false
                    });
                }
                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {

                    var mod = ModelState.First(c => c.Key == "Code");  // this

                    mod.Value.Errors.Add("هذا الكود موجود من قبل");

                    return View(erpRole);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل مجموعة المستخدمين" : "اضافة مجموعة مستخدمين",
                    EnAction = "AddEdit",
                    ControllerName = "ERPRoles",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = erpRole.Id > 0 ? erpRole.Id : db.ERPRoles.Max(i => i.Id),
                    ArItemName = erpRole.ArName,
                    EnItemName = erpRole.EnName,
                    CodeOrDocNo = erpRole.Code
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
            else
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();


                return View(erpRole);
            }



        }



        // POST: ReceiptAndPaymentVoucher/Delete/5
        [HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                ERPRole erpRole = db.ERPRoles.Find(id);
                erpRole.IsDeleted = true;
                erpRole.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(erpRole).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف مجموعةمستخدمين",
                    EnAction = "AddEdit",
                    ControllerName = "ERPRoles",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = erpRole.EnName,
                    ArItemName = erpRole.ArName,
                    CodeOrDocNo = erpRole.Code
                });
                Notification.GetNotification("ERPRoles", "Delete", "Delete", id, null, "مجموعة المستخدمين");

                // Add DB Change
                QueryHelper.AddDBChange(new DBChange()
                {
                    TableName = "ERPRole",
                    SelectedId = id,
                    IsMasterChange = true,
                    IsNew = false,
                    IsTransaction = false
                });
                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

        }

        [HttpPost]
        public ActionResult ActivateDeactivate(int id)
        {
            try
            {
                ERPRole erpRole = db.ERPRoles.Find(id);
                if (erpRole.IsActive == true)
                {
                    erpRole.IsActive = false;
                }
                else
                {
                    erpRole.IsActive = true;
                }
                erpRole.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(erpRole).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)erpRole.IsActive ? "تنشيط مجموعة مستخدمين" : "إلغاء تنشيط مجموعة مستخدمين ",
                    EnAction = "AddEdit",
                    ControllerName = "ERPRoles",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = erpRole.Id,
                    EnItemName = erpRole.EnName,
                    ArItemName = erpRole.ArName,
                    CodeOrDocNo = erpRole.Code
                });

                ////-------------------- Notification-------------------------////
                if (erpRole.IsActive == true)
                {
                    Notification.GetNotification("ERPRoles", "Activate/Deactivate", "ActivateDeactivate", id, true, "مجموعات المستخدمين");
                }
                else
                {

                    Notification.GetNotification("ERPRoles", "Activate/Deactivate", "ActivateDeactivate", id, false, "مجموعات المستخدمين");
                }
                //int pageid = db.Get_PageId("ERPRoles").SingleOrDefault().Value;
                //var actionId = db.PageActions.FirstOrDefault(c => c.Action == "Delete" && c.EnName == "Delete" && c.PageId == pageid).Id;
                //int userid = db.UserNotifications.FirstOrDefault(c => c.ActionId == actionId && c.PageId == pageid).UserId.Value;
                //var UserName = User.Identity.Name;
                //db.Sp_OccuredNotification(actionId, $"بحذف بيانات في شاشة مجموعات المستخدمين  {UserName}قام المستخدم  ");
                //////////////////-----------------------------------------------------------------------

                // Add DB Change
                QueryHelper.AddDBChange(new DBChange()
                {
                    TableName = "ERPRole",
                    SelectedId = id,
                    IsMasterChange = true,
                    IsNew = false,
                    IsTransaction = false
                });

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
