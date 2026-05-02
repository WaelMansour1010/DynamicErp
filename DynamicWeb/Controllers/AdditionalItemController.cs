using MyERP.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Controllers
{
    public class AdditionalItemController : Controller
    {
        
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: AdditionalItem
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة الأصناف الإضافية",
                EnAction = "Index",
                ControllerName = "AdditionalItem",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("AdditionalItem", "View", "Index", null, null, "الأصناف الإضافية");
            //////////-----------------------------------------------------------------------
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<AdditionalItem> additionalItems;

            if (string.IsNullOrEmpty(searchWord))
            {
                additionalItems = db.AdditionalItems.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.AdditionalItems.Where(s => s.IsDeleted == false).Count();

            }
            else
            {
                additionalItems = db.AdditionalItems.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.Notes.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.AdditionalItems.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.Notes.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(additionalItems.ToList());
        }

        // GET: AdditionalItem/Edit/5
        public ActionResult AddEdit(int? id)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);


            if (id == null)
            {
                // drop down list of  items
                ViewBag.ItemId = new SelectList(db.Items.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.ArName
                }), "Id", "ArName");

                // drop down list of  item unit
                ViewBag.ItemPriceId = new SelectList(db.ItemUnits.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.ArName
                }), "Id", "ArName");

                // drop down list of item group
                ViewBag.ItemGroupId = new SelectList(db.ItemGroups.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                
                return View();
            }
            AdditionalItem additionalItem = db.AdditionalItems.Find(id);
            if (additionalItem == null)
            {
                return HttpNotFound();
            }
            ViewBag.Next = QueryHelper.Next((int)id, "AdditionalItem");
            ViewBag.Previous = QueryHelper.Previous((int)id, "AdditionalItem");
            ViewBag.Last = QueryHelper.GetLast("AdditionalItem");
            ViewBag.First = QueryHelper.GetFirst("AdditionalItem");

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل الصنف الإضافى",
                EnAction = "AddEdit",
                ControllerName = "AdditionalItem",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = additionalItem.Id,
                ArItemName = additionalItem.ArName,
                EnItemName = additionalItem.EnName,
                CodeOrDocNo = additionalItem.Code
            });


            // drop down list of  items
            ViewBag.ItemId = new SelectList(db.Items.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.ArName
            }), "Id", "ArName" , additionalItem.ItemId);

            // drop down list of  item unit
            ViewBag.ItemPriceId = new SelectList(db.ItemPrices.Where(c => c.ItemUnit.IsDeleted == false && c.ItemUnit.IsActive == true &&c.ItemId==additionalItem.ItemId).Select(b => new
            {
                Id = b.Id,
                ArName = b.ItemUnit.ArName
            }), "Id", "ArName" , additionalItem.ItemPriceId);

            // drop down list of item group
            ViewBag.ItemGroupId = new SelectList(db.ItemGroups.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");


            return View(additionalItem);
        }

        // POST: AdditionalItem/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult AddEdit(AdditionalItem additionalItem)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            if (ModelState.IsValid)
            {
                var id = additionalItem.Id;
                additionalItem.IsDeleted = false;
                if (additionalItem.Id > 0)
                {
                    additionalItem.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                    // use another object to prevent entity error
                    var old = db.AdditionalItems.Find(id);
                    db.AdditionalItemsGroups.RemoveRange(db.AdditionalItemsGroups.Where(p => p.AdditionalItemId == old.Id).ToList());
                    old.ArName = additionalItem.ArName;
                    //old.EnName = additionalItem.EnName;

                    old.ItemId = additionalItem.ItemId;
                    old.ItemUnitId = additionalItem.ItemUnitId;
                    old.ItemPriceId = additionalItem.ItemPriceId;
                    old.Equivalent = additionalItem.Equivalent;
                    old.DoublePrice = additionalItem.DoublePrice;
                    old.ExtraPrice = additionalItem.ExtraPrice;
                    old.ShortagePrice = additionalItem.ShortagePrice;
                    old.DoubleQuantity = additionalItem.DoubleQuantity;
                    old.ExtraQuantity = additionalItem.ExtraQuantity;
                    old.ShortageQuantity = additionalItem.ShortageQuantity;

                    
                    

                    foreach (var item in additionalItem.AdditionalItemsGroups)
                    {
                        old.AdditionalItemsGroups.Add(item);
                    }
                    
                    db.Entry(old).State = EntityState.Modified;

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("AdditionalItem", "Edit", "AddEdit", additionalItem.Id, null, "الأصناف الإضافية");

                    // Add DB Change
                    QueryHelper.AddDBChange(new DBChange()
                    {
                        TableName = "AdditionalItem",
                        SelectedId = old.Id,
                        IsMasterChange = true,
                        IsNew = false,
                        IsTransaction = false
                    });
                }
                else
                {
                    additionalItem.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    //additionalItem.Code= (QueryHelper.CodeLastNum("AdditionalItem") + 1).ToString();
                    additionalItem.IsActive = true;

                    db.AdditionalItems.Add(additionalItem);
                    db.SaveChanges();

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("AdditionalItem", "Add", "AddEdit", additionalItem.Id, null, "الأصناف الإضافية");

                    // Add DB Change
                    var SelectedId = db.AdditionalItems.OrderByDescending(r => r.Id).FirstOrDefault().Id;
                    QueryHelper.AddDBChange(new DBChange()
                    {
                        TableName = "AdditionalItem",
                        SelectedId = SelectedId,
                        IsMasterChange = true,
                        IsNew = true,
                        IsTransaction = false
                    });
                }
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = additionalItem.Id > 0 ? "تعديل صنف إضافى" : "اضافة صنف إضافى",
                    EnAction = "AddEdit",
                    ControllerName = "EmployeeContract",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "Post",
                    SelectedItem = additionalItem.Id,
                    ArItemName = additionalItem.ArName,
                    EnItemName = additionalItem.EnName,
                    CodeOrDocNo = additionalItem.Code
                });

                return Json(new { success = "true" });

            }


            // drop down list of  items
            ViewBag.ItemId = new SelectList(db.Items.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "ArName", additionalItem.ItemId);

            // drop down list of  item unit
            ViewBag.ItemPriceId = new SelectList(db.ItemUnits.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "ArName", additionalItem.ItemPriceId);

            // drop down list of item group
            ViewBag.ItemGroupId = new SelectList(db.ItemGroups.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "ArName");

            return View(additionalItem);
        }

        // POST: AdditionalItem/Delete/5
        [HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                AdditionalItem additionalItem = db.AdditionalItems.Find(id);
                additionalItem.IsDeleted = true;
                additionalItem.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(additionalItem).State = EntityState.Modified;

                db.SaveChanges();

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف صنف إضافى",
                    EnAction = "AddEdit",
                    ControllerName = "AdditionalItem",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = additionalItem.EnName,
                    ArItemName = additionalItem.ArName,
                    CodeOrDocNo = additionalItem.Code
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("AdditionalItem", "Delete", "Delete", id, null, "الأصناف الإضافية");

                ///////-----------------------------------------------------------------------

                // Add DB Change
               
                QueryHelper.AddDBChange(new DBChange()
                {
                    TableName = "AdditionalItem",
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
        [SkipERPAuthorize]
        public JsonResult SetCodeNum()
        {
            var code = QueryHelper.CodeLastNum("AdditionalItem");
            return Json(code + 1, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult GetDefaultUnits(int id)
        {
            var units = db.ItemPrices.Where(a=>a.ItemId == id && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a=>a.IsDefault).Select(a=> new { Id=a.Id , ArName=a.ItemUnit.ArName , ItemUnitId = a.ItemUnitId });
            return Json(units, JsonRequestBehavior.AllowGet);
        }


        [SkipERPAuthorize]
        public JsonResult GetAdditionalItems(int id)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var item = db.Items.Find(id);
            if(db.AdditionalItems.Where(a => a.IsActive == true && a.IsDeleted == false).Count() > 0)
            {
                var additionalItems = db.AdditionalItemsGroups.Include(b=>b.AdditionalItem.ItemPrice).Include(a=>a.AdditionalItem.ItemUnit).Where(a =>a.AdditionalItem.IsActive == true && a.AdditionalItem.IsDeleted == false&& a.ItemGroupId == item.ItemGroupId).Select(a=> new { ItemId= a.AdditionalItem.ItemId, ArName = a.AdditionalItem.ArName , ItemUnit = a.AdditionalItem.ItemPrice.ItemUnit.ArName , ExtraPrice = a.AdditionalItem.ExtraPrice , DoublePrice = a.AdditionalItem.DoublePrice , DoubleQuantity = a.AdditionalItem.DoubleQuantity , ExtraQuantity = a.AdditionalItem.ExtraQuantity, ItemPriceId =a.AdditionalItem.ItemPriceId});
                return Json(additionalItems, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(0, JsonRequestBehavior.AllowGet);
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

        [HttpPost]
        public ActionResult ActivateDeactivate(int id)
        {
            try
            {
                AdditionalItem additionalItem = db.AdditionalItems.Find(id);
                if (additionalItem.IsActive == true)
                {
                    additionalItem.IsActive = false;
                }
                else
                {
                    additionalItem.IsActive = true;
                }
                additionalItem.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(additionalItem).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = additionalItem.Id > 0 ? "تنشيط صنف إضافى" : "إلغاء تنشيط صنف إضافى",
                    EnAction = "AddEdit",
                    ControllerName = "AdditionalItem",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "Post",
                    SelectedItem = additionalItem.Id,
                    ArItemName = additionalItem.ArName,
                    EnItemName = additionalItem.EnName,
                    CodeOrDocNo = additionalItem.Code
                });
                ////-------------------- Notification-------------------------////
                if (additionalItem.IsActive == true)
                {
                    Notification.GetNotification("AdditionalItem", "Activate/Deactivate", "ActivateDeactivate", id, true, "الأصناف الإضافية");
                }
                else
                {

                    Notification.GetNotification("AdditionalItem", "Activate/Deactivate", "ActivateDeactivate", id, false, "الأصناف الإضافية");
                }
                ///////-----------------------------------------------------------------------

                // Add DB Change
                QueryHelper.AddDBChange(new DBChange()
                {
                    TableName = "AdditionalItem",
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
    }
}