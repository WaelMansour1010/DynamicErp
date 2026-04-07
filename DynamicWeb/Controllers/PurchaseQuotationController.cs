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
using Newtonsoft.Json;
using System.Data.Entity.Core.Objects;
using System.Web.Script.Serialization;

namespace MyERP.Controllers
    {
        
        public class PurchaseQuotationController : Controller
        {
            private MySoftERPEntity db = new MySoftERPEntity();

            // GET: PurchaseQuotation
            public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
            {
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "فتح قائمة أسعار الشراء",
                    EnAction = "Index",
                    ControllerName = "PurchaseQuotation",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "GET"
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("PurchaseQuotation", "View", "Index", null, null, "فواتير المشتريات");
                ViewBag.PageIndex = pageIndex;
                int skipRowsNo = 0;

                if (pageIndex > 1)
                    skipRowsNo = (pageIndex - 1) * wantedRowsNo;
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                var depIds = db.Departments.Where(d => (userId == 1 || db.UserDepartments.Where(u => u.UserId == userId && u.Privilege == true).Select(u => u.DepartmentId).Contains(d.Id)) && d.IsDeleted == false && d.IsActive == true).Select(d => (int?)d.Id);
                IQueryable<PurchaseQuotation> PurchaseQuotations;

                if (string.IsNullOrEmpty(searchWord))
                {
                    PurchaseQuotations = db.PurchaseQuotations.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId)).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.PurchaseQuotations.Where(s => s.IsDeleted == false && s.IsActive == true && depIds.Contains(s.DepartmentId)).Count();

            }
            else
                {
                    PurchaseQuotations = db.PurchaseQuotations.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (s.DocumentNumber.Contains(searchWord) || s.Branch.ArName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.VoucherDate.ToString().Contains(searchWord) || s.Warehouse.ArName.Contains(searchWord)  || s.Notes.Contains(searchWord) || s.Vendor.ArName.Contains(searchWord))).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                  
  ViewBag.Count = PurchaseQuotations.Count();
                }
              
                ViewBag.searchWord = searchWord;
                ViewBag.wantedRowsNo = wantedRowsNo;
                return View(PurchaseQuotations.ToList());

            }
           
            // GET: PurchaseQuotation/Edit/5
            public ActionResult AddEdit(int? id)
            {
                SystemSetting sysObj = db.SystemSettings.FirstOrDefault();
                ViewData["AfterAdd"] = sysObj.PrintTransactionsAfterAdd;
                ViewData["AfterEdit"] = sysObj.PrintTransactionsAfterEdit; 
                ViewData["ShowSalesPrices"] = sysObj.ShowSalesPrices;
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                ViewBag.SystemPageId = db.Database.SqlQuery<int>($"select Id from [SystemPage]  where Code='PurchaseQuotation'").FirstOrDefault();

                if (id == null)
                {
                   
                    if (userId == 1)
                    {
                        ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new {
                            Id = b.Id,
                            ArName = b.Code + " - " + b.ArName
                        }), "Id", "ArName", sysObj.DefaultDepartmentId);
                     
                    }
                    else
                    {
                        ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new {
                            Id = b.DepartmentId,
                            ArName = b.Department.Code + " - " + b.Department.ArName
                        }), "Id", "ArName", sysObj.DefaultDepartmentId);
                        
                    }

                    ViewBag.ItemId = JsonConvert.SerializeObject(db.Items.Where(i => i.IsDeleted == false && i.IsActive == true).Select(i => new { i.ArName, i.Id }));
                    ViewBag.ItemPriceId = JsonConvert.SerializeObject(db.ItemPrices.Where(i => i.IsDeleted == false && i.IsActive == true).Select(i => new { i.Barcode, i.Id }));

                    ViewBag.VendorId = new SelectList(db.Vendors.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName");
                //------ Time Zone Depends On Currency --------//
                var Currency = db.Currencies.Where(a => a.IsActive == true && a.IsDeleted == false && a.IsDefault == true).FirstOrDefault();
                var CurrencyCode = Currency != null ? Currency.Code : "";
                TimeZoneInfo info;
                if (CurrencyCode == "SAR")
                {
                    //info = TimeZoneInfo.FindSystemTimeZoneById("Arabian Standard Time");//+2H from Egypt Standard Time
                    info = TimeZoneInfo.FindSystemTimeZoneById("Arab Standard Time");// +1H from Egypt Standard Time
                }
                else
                {
                    info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
                }
                DateTime utcNow = DateTime.UtcNow;
                DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
                //----------------- End of Time Zone Depends On Currency --------------------//

                ViewBag.VoucherDate = cTime.ToString("yyyy-MM-ddTHH:mm");
                PurchaseQuotation newobj = new PurchaseQuotation();
                    return View(newobj);
                }
                PurchaseQuotation PurchaseQuotation = db.PurchaseQuotations.Find(id);
                if (PurchaseQuotation == null)
                {
                    return HttpNotFound();
                }
                int sysPageId = QueryHelper.SourcePageId("PurchaseQuotation");

               
                ViewBag.Accounts = new SelectList(db.ChartOfAccounts.Where(a => a.ClassificationId == 3 && a.IsActive == true && a.IsDeleted == false), "Id", "ArName");

               

                ViewBag.VendorId = new SelectList(db.Vendors.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", PurchaseQuotation.VendorOrCustomerId);
               
                if (userId == 1)
                {
                    ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", PurchaseQuotation.DepartmentId);
                  

                }
                else
                {
                    ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new {
                        Id = b.DepartmentId,
                        ArName = b.Department.Code + " - " + b.Department.ArName
                    }), "Id", "ArName", PurchaseQuotation.DepartmentId);
                   

                }
                ViewBag.ItemId = JsonConvert.SerializeObject(db.Items.Where(i => i.IsDeleted == false && i.IsActive == true).Select(i => new { i.ArName, i.Id }));
               

                ViewBag.ItemPriceId = JsonConvert.SerializeObject(db.ItemPrices.Where(i => i.IsDeleted == false && i.IsActive == true).Select(i => new { i.Barcode, i.Id }));
                ViewBag.SerialNumbers = JsonConvert.SerializeObject(db.PurchaseSaleSerialNumbers.Where(s => s.SelectedId == id && s.PageSourceId == db.SystemPages.FirstOrDefault(d => d.TableName == "PurchaseQuotation").Id).Select(d => new { d.ItemId, d.SerialNumber }));
                try
                {
                    ViewBag.VoucherDate = PurchaseQuotation.VoucherDate.Value.ToString("yyyy-MM-ddTHH:mm");
                }
                catch (Exception)
                {
                }

                ViewBag.Next = QueryHelper.Next((int)id, "PurchaseQuotation");
                ViewBag.Previous = QueryHelper.Previous((int)id, "PurchaseQuotation");
                ViewBag.Last = QueryHelper.GetLast("PurchaseQuotation");
                ViewBag.First = QueryHelper.GetFirst("PurchaseQuotation");

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "فتح تفاصيل عرض سعر شراء",
                    EnAction = "AddEdit",
                    ControllerName = "PurchaseQuotation",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "GET",
                    SelectedItem = PurchaseQuotation.Id,
                    CodeOrDocNo = PurchaseQuotation.DocumentNumber
                });
                return View(PurchaseQuotation);
            }

            // POST: PurchaseQuotation/Edit/5
            // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
            // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
            [HttpPost]
            //[ValidateAntiForgeryToken]
            public ActionResult AddEdit(PurchaseQuotation PurchaseQuotation)
            {
                if (ModelState.IsValid)
                {
                    var id = PurchaseQuotation.Id;
                    PurchaseQuotation.IsDeleted = false;
                    PurchaseQuotation.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    if (PurchaseQuotation.Id > 0)
                    {
                        if (db.PurchaseQuotations.Find(PurchaseQuotation.Id).IsPosted == true)
                        {
                            return Content("false");
                        }
                        MyXML.xPathName = "Details";
                        var PurchaseQuotationDetails = MyXML.GetXML(PurchaseQuotation.PurchaseQuotationDetails);

                        db.PurchaseQuotation_Update(PurchaseQuotation.Id, PurchaseQuotation.DocumentNumber, PurchaseQuotation.BranchId, PurchaseQuotation.WarehouseId, PurchaseQuotation.DepartmentId, PurchaseQuotation.VoucherDate, PurchaseQuotation.VendorOrCustomerId, PurchaseQuotation.CurrencyId, PurchaseQuotation.CurrencyEquivalent, PurchaseQuotation.Total, PurchaseQuotation.TotalItemsDiscount, PurchaseQuotation.SalesTaxes, PurchaseQuotation.TotalAfterTaxes, PurchaseQuotation.VoucherDiscountValue, PurchaseQuotation.VoucherDiscountPercentage, PurchaseQuotation.NetTotal, PurchaseQuotation.Paid, PurchaseQuotation.ValidityPeriod, PurchaseQuotation.DeliveryPeriod, PurchaseQuotation.CostPriceId, PurchaseQuotation.CurrentQuantity, PurchaseQuotation.DestinationWarehouseId, PurchaseQuotation.SystemPageId, PurchaseQuotation.SelectedId, PurchaseQuotation.TotalCostPrice, PurchaseQuotation.TotalItemDirectExpenses, PurchaseQuotation.IsDelivered, PurchaseQuotation.IsAccepted, PurchaseQuotation.IsLinked, PurchaseQuotation.IsCompleted, PurchaseQuotation.IsPosted, PurchaseQuotation.UserId, PurchaseQuotation.IsActive, PurchaseQuotation.IsDeleted, PurchaseQuotation.AutoCreated, PurchaseQuotation.Notes, PurchaseQuotation.Image, PurchaseQuotation.UpdatedId, PurchaseQuotationDetails);

                        Notification.GetNotification("PurchaseQuotation", "Edit", "AddEdit", id, null, "عروض أسعار الشراء");



                    }
                    else
                    {
                        PurchaseQuotation.IsActive = true;
                        MyXML.xPathName = "Details";
                        var PurchaseQuotationDetails = MyXML.GetXML(PurchaseQuotation.PurchaseQuotationDetails);

                       

                        db.PurchaseQuotation_Insert(PurchaseQuotation.BranchId, PurchaseQuotation.WarehouseId, PurchaseQuotation.DepartmentId, PurchaseQuotation.VoucherDate, PurchaseQuotation.VendorOrCustomerId, PurchaseQuotation.CurrencyId, PurchaseQuotation.CurrencyEquivalent, PurchaseQuotation.Total, PurchaseQuotation.TotalItemsDiscount, PurchaseQuotation.SalesTaxes, PurchaseQuotation.TotalAfterTaxes, PurchaseQuotation.VoucherDiscountValue, PurchaseQuotation.VoucherDiscountPercentage, PurchaseQuotation.NetTotal, PurchaseQuotation.Paid, PurchaseQuotation.ValidityPeriod, PurchaseQuotation.DeliveryPeriod, PurchaseQuotation.CostPriceId, PurchaseQuotation.CurrentQuantity, PurchaseQuotation.DestinationWarehouseId, PurchaseQuotation.SystemPageId, PurchaseQuotation.SelectedId, PurchaseQuotation.TotalCostPrice, PurchaseQuotation.TotalItemDirectExpenses, PurchaseQuotation.IsDelivered, PurchaseQuotation.IsAccepted, PurchaseQuotation.IsLinked, PurchaseQuotation.IsCompleted, false, PurchaseQuotation.UserId, PurchaseQuotation.IsActive, PurchaseQuotation.IsDeleted, PurchaseQuotation.AutoCreated, PurchaseQuotation.Notes, PurchaseQuotation.Image, PurchaseQuotation.UpdatedId, PurchaseQuotationDetails);

                        ////-------------------- Notification-------------------------////
                        Notification.GetNotification("PurchaseQuotation", "Add", "AddEdit", id, null, " فواتير المشتريات");

                        //int pageid = db.Get_PageId("PurchaseQuotation").SingleOrDefault().Value;
                        //var actionId = db.PageActions.FirstOrDefault(c => c.Action == "AddEdit" && c.EnName == "Add" && c.PageId == pageid).Id;
                        //int userid = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                        //var UserName = User.Identity.Name;
                        //db.Sp_OccuredNotification(actionId, $"باضافة بيانات في شاشة فواتير المشتريات  {UserName}قام المستخدم  ");

                        ////////////////-----------------------------------------------------------------------

                    }
                    QueryHelper.AddLog(new MyLog()
                    {
                        ArAction = id > 0 ? "تعديل عرض سعر شراء " : "اضافة عرض سعر شراء",
                        EnAction = "AddEdit",
                        ControllerName = "PurchaseQuotation",
                        UserName = User.Identity.Name,
                        UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                        LogDate = DateTime.Now,
                        RequestMethod = "POST",
                        SelectedItem = PurchaseQuotation.Id > 0 ? PurchaseQuotation.Id : db.PurchaseQuotations.Max(i => i.Id),
                        CodeOrDocNo = PurchaseQuotation.DocumentNumber
                    });
                    return Content("true");
                }
                var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .Select(x => new { x.Key, x.Value.Errors })
                        .ToArray();

                return Content("false");
            }

            // POST: PurchaseQuotation/Delete/5
            [HttpPost, ActionName("Delete")]
            //[ValidateAntiForgeryToken]
            public ActionResult DeleteConfirmed(int id)
            {
                try
                {
                    PurchaseQuotation PurchaseQuotation = db.PurchaseQuotations.Find(id);
                    if (PurchaseQuotation.IsPosted == true)
                    {
                        return Content("false");
                    }
                    var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                    db.PurchaseQuotation_Delete(id, userId);
                    db.SaveChanges();
                    QueryHelper.AddLog(new MyLog()
                    {
                        ArAction = " حذف عرض سعر شراء",
                        EnAction = "AddEdit",
                        ControllerName = "PurchaseQuotation",
                        UserName = User.Identity.Name,
                        UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                        LogDate = DateTime.Now,
                        RequestMethod = "POST",
                        SelectedItem = id,
                        CodeOrDocNo = PurchaseQuotation.DocumentNumber
                    });

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("PurchaseQuotation", "Delete", "Delete", id, null, "فواتير المشتريات");

                   

                    return Content("true");
                }
                catch (Exception ex)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }
            }
            // get last price of item occur in invoices
            [SkipERPAuthorize]
            public JsonResult SetLastPrice(int? itemId)
            {
                var lastPrice = db.GetLastItemPrice(itemId).FirstOrDefault();
                return Json(lastPrice, JsonRequestBehavior.AllowGet);
            }
           
            [SkipERPAuthorize]
            public JsonResult SetDocNum(int id)
            {

                var docNo = QueryHelper.DocLastNum(id, "PurchaseQuotation");
                double i = (docNo) + 1;
                return Json(i, JsonRequestBehavior.AllowGet);
            }
           
            [SkipERPAuthorize]
            public JsonResult SetBankAccounts(int bankId)
            {
                var BankAccountsList = db.GetBankAccountByBankId(bankId).ToList();
                return Json(BankAccountsList, JsonRequestBehavior.AllowGet);
            }
            [SkipERPAuthorize]
            public JsonResult WarehousesByDepartmentId(int id)
            {
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                if (userId == 1)
                {
                    var warehouses = db.Warehouses.Where(w => w.IsActive == true && w.IsDeleted == false && w.DepartmentId == id).Select(w => new { w.Id, w.ArName });
                    return Json(warehouses, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    var warehouses = db.UserWareHouses.Where(w => w.UserId == userId && w.Warehouse.DepartmentId == id).Select(w => new { w.Warehouse.Id, w.Warehouse.ArName });
                    return Json(warehouses, JsonRequestBehavior.AllowGet);
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
