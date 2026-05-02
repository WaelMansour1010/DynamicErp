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
    public class ItemUnitsController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة وحدات الأصناف",
                EnAction = "Index",
                ControllerName = "ItemUnits",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("ItemUnits", "View", "Index", null, null, "وحدات الأصناف");

            /////////////////-----------------------------------------------------------------------

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            /////////////////////////// Search ////////////////////

            IQueryable<ItemUnit> itemUnits;

            if (string.IsNullOrEmpty(searchWord))
            {
                itemUnits = db.ItemUnits.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);

                ViewBag.Count = db.ItemUnits.Where(c => c.IsDeleted == false).Count();
            }
            else
            {
                itemUnits = db.ItemUnits.Where(s => s.IsDeleted == false && (s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.Equivalent.ToString().Contains(searchWord) || s.Code.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = itemUnits.Count();

                ///////////////////////////////////////////////////////////////////////////////

            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(itemUnits.ToList());

        }

        [SkipERPAuthorize]
        public JsonResult SetCodeNum()
        {
            var code = QueryHelper.CodeLastNum("ItemUnit");
            return Json(code + 1, JsonRequestBehavior.AllowGet);
        }


        public ActionResult AddEdit(int? id)
        {

            if (id == null)
            {
                ItemUnit NewObj = new ItemUnit();
                ViewBag.ParentId = new SelectList(db.ItemUnits.Where(c => c.IsDeleted == false && c.IsActive == true && !c.ParentId.HasValue).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                return View(NewObj);
            }
            ItemUnit itemUnit = db.ItemUnits.Find(id);
            if (itemUnit == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل وحدة الأصناف ",
                EnAction = "AddEdit",
                ControllerName = "ItemUnits",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = itemUnit.Id,
                ArItemName = itemUnit.ArName,
                EnItemName = itemUnit.EnName,
                CodeOrDocNo = itemUnit.Code
            });

            ViewBag.ParentId = new SelectList(db.ItemUnits.Where(c => c.IsDeleted == false && c.IsActive == true && !c.ParentId.HasValue && c.Id != id).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", itemUnit.ParentId);

            ViewBag.Next = QueryHelper.Next((int)id, "ItemUnit");
            ViewBag.Previous = QueryHelper.Previous((int)id, "ItemUnit");
            ViewBag.Last = QueryHelper.GetLast("ItemUnit");
            ViewBag.First = QueryHelper.GetFirst("ItemUnit");
            return View(itemUnit);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddEdit(ItemUnit itemUnit, string newBtn)
        {
            if (ModelState.IsValid)
            {
                var id = itemUnit.Id;
                itemUnit.IsDeleted = false;
                if (itemUnit.ParentId == null)
                    itemUnit.Equivalent = 1;
                if (itemUnit.Id > 0)
                {
                    itemUnit.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                    db.Entry(itemUnit).State = EntityState.Modified;

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("ItemUnits", "Edit", "AddEdit", id, null, "وحدات الأصناف");

                    // Add DB Change
                    QueryHelper.AddDBChange(new DBChange()
                    {
                        TableName = "ItemUnit",
                        SelectedId = itemUnit.Id,
                        IsMasterChange = true,
                        IsNew = false,
                        IsTransaction = false
                    });

                }
                else
                {
                    itemUnit.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    itemUnit.Code = (QueryHelper.CodeLastNum("ItemUnit") + 1).ToString();
                    itemUnit.IsActive = true;
                    db.ItemUnits.Add(itemUnit);
                    db.SaveChanges();
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("ItemUnits", "Add", "AddEdit", itemUnit.Id, null, "وحدات الأصناف");

                    // Add DB Change
                    var SelectedId = db.ItemUnits.OrderByDescending(r => r.Id).FirstOrDefault().Id;
                    QueryHelper.AddDBChange(new DBChange()
                    {
                        TableName = "ItemUnit",
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
                    ViewBag.ParentId = new SelectList(db.ItemUnits.Where(c => c.IsDeleted == false && c.IsActive == true && !c.ParentId.HasValue && c.Id != itemUnit.Id), "Id", "ArName", itemUnit.ParentId);

                    return View(itemUnit);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل وحدة الأصناف" : "اضافة وحدة أصناف",
                    EnAction = "AddEdit",
                    ControllerName = "ItemUnits",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = itemUnit.Id,
                    ArItemName = itemUnit.ArName,
                    EnItemName = itemUnit.EnName,
                    CodeOrDocNo = itemUnit.Code
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

            ViewBag.ParentId = new SelectList(db.ItemUnits.Where(c => c.IsDeleted == false && c.IsActive == true && !c.ParentId.HasValue && c.Id != itemUnit.Id), "Id", "ArName", itemUnit.ParentId);

            return View(itemUnit);
        }


        [HttpPost, ActionName("Delete")]

        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                ItemUnit itemUnit = db.ItemUnits.Find(id);
                itemUnit.IsDeleted = true;
                itemUnit.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(itemUnit).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف وحدة الأصناف",
                    EnAction = "AddEdit",
                    ControllerName = "ItemUnits",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = itemUnit.EnName,
                    ArItemName = itemUnit.ArName,
                    CodeOrDocNo = itemUnit.Code
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("ItemUnits", "Delete", "Delete", id, null, "وحدات الأصناف");

                //int pageid = db.Get_PageId("ItemUnits").SingleOrDefault().Value;
                //var actionId = db.PageActions.FirstOrDefault(c => c.Action == "Delete" && c.EnName == "Delete" && c.PageId == pageid).Id;
                //int userid = db.UserNotifications.FirstOrDefault(c => c.ActionId == actionId && c.PageId == pageid).UserId.Value;
                //var UserName = User.Identity.Name;
                //db.Sp_OccuredNotification(actionId, $"بحذف بيانات في شاشة وحدات الأصناف  {UserName}قام المستخدم  ", DateTime.Parse(TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time").ToString()),id);
                //////////////////-----------------------------------------------------------------------

                // Add DB Change
                QueryHelper.AddDBChange(new DBChange()
                {
                    TableName = "ItemUnit",
                    SelectedId = itemUnit.Id,
                    IsMasterChange = true,
                    IsNew = false,
                    IsTransaction = false
                });

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
            ItemUnit itemUnit = db.ItemUnits.Find(id);
            if (itemUnit.IsActive == true)
            {
                itemUnit.IsActive = false;
            }
            else
            {
                itemUnit.IsActive = true;
            }
            itemUnit.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            db.Entry(itemUnit).State = EntityState.Modified;

            db.SaveChanges();
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = (bool)itemUnit.IsActive ? "تنشيط وحدة الأصناف" : "إلغاء تنشيط وحدة الأصناف",
                EnAction = "AddEdit",
                ControllerName = "ItemUnits",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "POST",
                SelectedItem = itemUnit.Id,
                EnItemName = itemUnit.EnName,
                ArItemName = itemUnit.ArName,
                CodeOrDocNo = itemUnit.Code
            });
            ////-------------------- Notification-------------------------////
            if (itemUnit.IsActive == true)
            {
                Notification.GetNotification("ItemUnits", "Activate/Deactivate", "ActivateDeactivate", id, true, " وحدات الاصناف");
            }
            else
            {

                Notification.GetNotification("ItemUnits", "Activate/Deactivate", "ActivateDeactivate", id, false, " وحدات الاصناف");
            }

            // Add DB Change
            QueryHelper.AddDBChange(new DBChange()
            {
                TableName = "ItemUnit",
                SelectedId = itemUnit.Id,
                IsMasterChange = true,
                IsNew = false,
                IsTransaction = false
            });

            return Content("true");
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
