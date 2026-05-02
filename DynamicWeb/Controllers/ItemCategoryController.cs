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
using MyERP.Utils;
using System.IO;

namespace MyERP.Controllers
{
    public class ItemCategoryController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        // GET: ItemCategory
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة فئات الأصناف",
                EnAction = "Index",
                ControllerName = "ItemCategory",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("ItemCategory", "View", "Index", null, null, "فئات الأصناف");
            //////-----------------------------------------------------------------------

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            /////////////////////////// Search ////////////////////

            IQueryable<ItemCategory> itemCategories;

            if (string.IsNullOrEmpty(searchWord))
            {
                itemCategories = db.ItemCategories.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);

                ViewBag.Count = db.ItemCategories.Where(c => c.IsDeleted == false).Count();
            }
            else
            {
                itemCategories = db.ItemCategories.Where(s => s.IsDeleted == false && (s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.Code.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.ItemCategories.Where(s => s.IsDeleted == false && (s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.Code.Contains(searchWord))).OrderBy(s => s.Id).Count();

                ///////////////////////////////////////////////////////////////////////////////

            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(itemCategories.ToList());


        }
        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {
                return View();
            }
            ItemCategory itemCategory = db.ItemCategories.Find(id);
            if (itemCategory == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل فئات الأصناف ",
                EnAction = "AddEdit",
                ControllerName = "ItemCategory",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = itemCategory.Id,
                ArItemName = itemCategory.ArName,
                EnItemName = itemCategory.EnName,
                CodeOrDocNo = itemCategory.Code
            });
            ViewBag.Next = QueryHelper.Next((int)id, "ItemCategory");
            ViewBag.Previous = QueryHelper.Previous((int)id, "ItemCategory");
            ViewBag.Last = QueryHelper.GetLast("ItemCategory");
            ViewBag.First = QueryHelper.GetFirst("ItemCategory");

            return View(itemCategory);
        }
        [HttpPost]
        public ActionResult AddEdit(ItemCategory itemCategory)
        {
            if (ModelState.IsValid)
            {
                var id = itemCategory.Id;
                itemCategory.IsDeleted = false;

                if (itemCategory.Id > 0)
                {
                    itemCategory.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    db.Entry(itemCategory).State = EntityState.Modified;

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("ItemCategory", "Edit", "AddEdit", id, null, "فئات الأصناف");
                    /////////-----------------------------------------------------------------------
                }
                else
                {
                    itemCategory.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    itemCategory.Code = (QueryHelper.CodeLastNum("ItemCategory") + 1).ToString();
                    itemCategory.IsActive = true;
                    db.ItemCategories.Add(itemCategory);
                    db.SaveChanges();

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("ItemCategory", "Add", "AddEdit", itemCategory.Id, null, "فئات الأصناف");
                    ///////////-----------------------------------------------------------------------
                }
                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {                    
                    var errors = ModelState
                   .Where(x => x.Value.Errors.Count > 0)
                   .Select(x => new { x.Key, x.Value.Errors })
                   .ToArray();
                   // return Json(new { success = false, errors });
                    return View(itemCategory);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل فئات الأصناف" : "اضافة فئات الأصناف",
                    EnAction = "AddEdit",
                    ControllerName = "ItemCategory",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = itemCategory.Id,
                    ArItemName = itemCategory.ArName,
                    EnItemName = itemCategory.EnName,
                    CodeOrDocNo = itemCategory.Code
                });
                return Json(new { success = "true" });
            }
            return View(itemCategory);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                ItemCategory itemCategory = db.ItemCategories.Find(id);
                itemCategory.IsDeleted = true;
                itemCategory.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(itemCategory).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف فئات الأصناف",
                    EnAction = "AddEdit",
                    ControllerName = "ItemCategory",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = itemCategory.EnName,
                    ArItemName = itemCategory.ArName,
                    CodeOrDocNo = itemCategory.Code
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("ItemCategory", "Delete", "Delete", id, null, "فئات الأصناف");
                 ////////////////-----------------------------------------------------------------------
                // Add DB Change
                QueryHelper.AddDBChange(new DBChange()
                {
                    TableName = "ItemCategory",
                    SelectedId = itemCategory.Id,
                    IsMasterChange = true,
                    IsNew = false,
                    IsTransaction = false
                });
                return Content("true");
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        [HttpPost]
        public ActionResult ActivateDeactivate(int id)
        {
            try
            {
                ItemCategory itemCategory = db.ItemCategories.Find(id);
                if (itemCategory.IsActive == true)
                {
                    itemCategory.IsActive = false;
                }
                else
                {
                    itemCategory.IsActive = true;
                }
                itemCategory.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(itemCategory).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)itemCategory.IsActive ? "تنشيط فئات الأصناف" : "إلغاء تنشيط فئات الأصناف",
                    EnAction = "AddEdit",
                    ControllerName = "ItemCategory",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = itemCategory.Id,
                    EnItemName = itemCategory.EnName,
                    ArItemName = itemCategory.ArName,
                    CodeOrDocNo = itemCategory.Code
                });
                ////-------------------- Notification-------------------------////
                if (itemCategory.IsActive == true)
                {
                    Notification.GetNotification("ItemCategory", "Activate/Deactivate", "ActivateDeactivate", id, true, "فئات الأصناف");
                }
                else
                {
                    Notification.GetNotification("ItemCategory", "Activate/Deactivate", "ActivateDeactivate", id, false, "فئات الأصناف");
                }
                 ////////////////-----------------------------------------------------------------------
                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }

        [SkipERPAuthorize]
        public JsonResult SetCodeNum()
        {
            var code = QueryHelper.CodeLastNum("ItemCategory");
            return Json(code + 1, JsonRequestBehavior.AllowGet);
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