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

namespace MyERP.Controllers
{
    public class StockReceiptVoucherController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: StockReceiptVoucher
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
            ViewBag.UseExpiryDateForItems = systemSetting.UseExpiryDateForItems;

            ViewBag.ShowSerialNumbers = systemSetting.ShowSerialNumbers == true;
            var depIds = db.Departments.Where(d => (userId == 1 || db.UserDepartments.Where(u => u.UserId == userId && u.Privilege == true).Select(u => u.DepartmentId).Contains(d.Id)) && d.IsDeleted == false && d.IsActive == true).Select(d => (int?)d.Id);

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة سندات صرف المخازن",
                EnAction = "Index",
                ControllerName = "StockReceiptVoucher",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("StockReceiptVoucher", "View", "Index", null, null, "سندات صرف المخازن");
            /////////-----------------------------------------------------------------------

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            /////////////////////////// Search ////////////////////

            IQueryable<StockReceiptVoucher> stockReceiptVouchers;

            if (string.IsNullOrEmpty(searchWord))
            {
                stockReceiptVouchers = db.StockReceiptVouchers.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId)).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.StockReceiptVouchers.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId)).Count();

            }
            else
            {
                stockReceiptVouchers = db.StockReceiptVouchers.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (s.DocumentNumber.Contains(searchWord) || s.Branch.ArName.Contains(searchWord) || s.Warehouse.ArName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Currency.ArName.Contains(searchWord) || s.SystemPage.ArName.Contains(searchWord) || s.Notes.Contains(searchWord))).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.StockReceiptVouchers.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (s.DocumentNumber.Contains(searchWord) || s.Branch.ArName.Contains(searchWord) || s.Warehouse.ArName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Currency.ArName.Contains(searchWord) || s.SystemPage.ArName.Contains(searchWord) || s.Notes.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(stockReceiptVouchers.ToList());
        }

        // GET: StockReceiptVoucher/AddEdit/5
        public ActionResult AddEdit(int? id)
        {
            var session = Session["lang"];
            SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();

            ViewBag.UseExpiryDateForItems = systemSetting.UseExpiryDateForItems;


            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            int sysPageId = QueryHelper.SourcePageId("StockReceiptVoucher");

            if (id == null)
            {
                if (userId == 1)
                {
                    ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                    {
                        Id = b.Id,
                        ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
                    }), "Id", "ArName", systemSetting.DefaultDepartmentId);
                    ViewBag.WarehouseId = new SelectList(db.Warehouses.Where(b => b.IsActive == true && b.IsDeleted == false && b.DepartmentId == systemSetting.DefaultDepartmentId).Select(b => new
                    {
                        Id = b.Id,
                        ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
                    }), "Id", "ArName", systemSetting.DefaultWarehouseId);
                }
                else
                {
                    ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId && d.Privilege == true && d.Department.IsActive == true && d.Department.IsDeleted == false).Select(b => new
                    {
                        Id = b.DepartmentId,
                        ArName = session.ToString() == "en" && b.Department.EnName != null ? b.Department.Code + " - " + b.Department.EnName : b.Department.Code + " - " + b.Department.ArName
                    }), "Id", "ArName", systemSetting.DefaultDepartmentId);
                    ViewBag.WarehouseId = new SelectList(db.UserWareHouses.Where(b => b.UserId == userId && b.Privilege == true && b.Warehouse.IsActive == true && b.Warehouse.IsDeleted == false && b.Warehouse.DepartmentId == systemSetting.DefaultDepartmentId).Select(b => new
                    {
                        Id = b.WareHouseId,
                        ArName = session.ToString() == "en" && b.Warehouse.EnName != null ? b.Warehouse.Code + " - " + b.Warehouse.EnName : b.Warehouse.Code + " - " + b.Warehouse.ArName
                    }), "Id", "ArName", systemSetting.DefaultWarehouseId);
                }

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
                return View();
            }
            StockReceiptVoucher stockReceiptVoucher = db.StockReceiptVouchers.Find(id);

            if (stockReceiptVoucher == null)
            {
                return HttpNotFound();
            }
            if (stockReceiptVoucher.AutoCreated == true)
            {
                var sysPage = db.SystemPages.Where(a => a.Id == stockReceiptVoucher.SystemPageId).FirstOrDefault();
                var table = sysPage.TableName;
                stockReceiptVoucher.SelectedId = int.Parse(db.Database.SqlQuery<string>($"select top(1)([DocumentNumber]) from[{table}] where [Id] = " + stockReceiptVoucher.SelectedId).FirstOrDefault());
            }
            ViewBag.JE = db.GetJournalEntryBySourceIdAndSourcePageId(id, sysPageId).FirstOrDefault();
            if (ViewBag.JE != null)
            {
                var JEDocNo = ViewBag.JE;
                ViewBag.JE.DocumentNumber = JEDocNo != null ? JEDocNo.DocumentNumber.Replace("-", "") : "";
                int JEId = ViewBag.JE.Id;
                int sourcePageId = ViewBag.JE.SourcePageId;
                ViewBag.JEDetails = db.JournalEntryDetails.Where(a => a.SourceId == id && a.JournalEntryId == JEId && a.SourcePageId == sourcePageId).OrderBy(a => a.Id).ToList();

            }

            ViewBag.SystemPage = stockReceiptVoucher.AutoCreated == true && stockReceiptVoucher.SystemPageId > 0 ? db.SystemPages.Find(stockReceiptVoucher.SystemPageId).ArName : "";

            if (userId == 1)
            {
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
                }), "Id", "ArName", stockReceiptVoucher.DepartmentId);
                ViewBag.WarehouseId = new SelectList(db.Warehouses.Where(b => b.IsActive == true && b.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
                }), "Id", "ArName", stockReceiptVoucher.WarehouseId);
            }
            else
            {
                ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId && d.Privilege == true && d.Department.IsActive == true && d.Department.IsDeleted == false).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = session.ToString() == "en" && b.Department.EnName != null ? b.Department.Code + " - " + b.Department.EnName : b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", stockReceiptVoucher.DepartmentId);
                ViewBag.WarehouseId = new SelectList(db.UserWareHouses.Where(b => b.UserId == userId && b.Privilege == true && b.Warehouse.IsActive == true && b.Warehouse.IsDeleted == false && b.Warehouse.DepartmentId == stockReceiptVoucher.DepartmentId).Select(b => new
                {
                    Id = b.WareHouseId,
                    ArName = session.ToString() == "en" && b.Warehouse.EnName != null ? b.Warehouse.Code + " - " + b.Warehouse.EnName : b.Warehouse.Code + " - " + b.Warehouse.ArName
                }), "Id", "ArName", stockReceiptVoucher.WarehouseId);
            }

            ViewBag.VoucherDate = stockReceiptVoucher.VoucherDate.ToString("yyyy-MM-ddTHH:mm");

            ViewBag.Next = QueryHelper.Next((int)id, "StockReceiptVoucher");
            ViewBag.Previous = QueryHelper.Previous((int)id, "StockReceiptVoucher");
            ViewBag.Last = QueryHelper.GetLast("StockReceiptVoucher");
            ViewBag.First = QueryHelper.GetFirst("StockReceiptVoucher");

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل سند صرف مخزني",
                EnAction = "AddEdit",
                ControllerName = "StockReceiptVoucher",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = stockReceiptVoucher.Id,
                CodeOrDocNo = stockReceiptVoucher.DocumentNumber
            });
            return View(stockReceiptVoucher);
        }

        // POST: StockReceiptVoucher/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult AddEdit([Bind(Include = "Id,DocumentNumber,BranchId,WarehouseId,DepartmentId,VoucherDate,VendorOrCustomerId,CurrencyId,CurrencyEquivalent,Total,TotalItemsDiscount,SalesTaxes,TotalAfterTaxes,VoucherDiscountValue,VoucherDiscountPercentage,NetTotal,Paid,ValidityPeriod,DeliveryPeriod,CostPriceId,CurrentQuantity,DestinationWarehouseId,SystemPageId,SelectedId,TotalCostPrice,TotalItemDirectExpenses,IsDelivered,IsAccepted,IsLinked,IsCompleted,IsPosted,UserId,IsActive,IsDeleted,AutoCreated,Notes,Image,UpdatedId,StockReceiptVoucherDetails")] StockReceiptVoucher stockReceiptVoucher)
        {
            if (ModelState.IsValid)
            {
                var id = stockReceiptVoucher.Id;
                stockReceiptVoucher.IsDeleted = false;

                // Patch details
                DataTable patches = new DataTable("PatchDetails");
                DataColumn ItemId = new DataColumn("ItemId", typeof(int));
                DataColumn ExpireDate = new DataColumn("ExpireDate", typeof(DateTime));

                patches.Columns.Add(ItemId);
                patches.Columns.Add(ExpireDate);
                foreach (var detail in stockReceiptVoucher.StockReceiptVoucherDetails)
                {
                    if (detail.ExpireDate != null)
                    {
                        detail.ExpireDate = detail.ExpireDate.Value.AddHours(6);
                        var patch = db.Patches.Where(e => e.ExpireDate.Value.Day == detail.ExpireDate.Value.Day &&
                        e.ExpireDate.Value.Month == detail.ExpireDate.Value.Month &&
                        e.ExpireDate.Value.Year == detail.ExpireDate.Value.Year &&
                        e.ItemId == detail.ItemId).Any();
                        if (patch != true && detail.SystemPageId == null && detail.SelectedId == null)
                        {
                            DataRow row = patches.NewRow();
                            row["ItemId"] = detail.ItemId;
                            row["ExpireDate"] = detail.ExpireDate.Value.AddHours(6);
                            patches.Rows.Add(row);
                        }
                    }
                }

                MyXML.xPathName = "PatchDetails";
                var stockReceiptVoucherPatchDetails = MyXML.GetXML(patches);

                if (stockReceiptVoucher.Id > 0)
                {
                    if (db.StockReceiptVouchers.Find(stockReceiptVoucher.Id).IsPosted == true)
                    {
                        return Content("false");
                    }
                    MyXML.xPathName = "Details";
                    var stockReceiptVoucherDetails = MyXML.GetXML(stockReceiptVoucher.StockReceiptVoucherDetails);
                    stockReceiptVoucher.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                    db.StockReceiptVoucher_Update(stockReceiptVoucher.Id, stockReceiptVoucher.DocumentNumber, stockReceiptVoucher.BranchId, stockReceiptVoucher.WarehouseId, stockReceiptVoucher.DepartmentId, stockReceiptVoucher.VoucherDate, stockReceiptVoucher.VendorOrCustomerId, stockReceiptVoucher.CurrencyId, stockReceiptVoucher.CurrencyEquivalent, stockReceiptVoucher.Total, stockReceiptVoucher.TotalItemsDiscount, stockReceiptVoucher.SalesTaxes, stockReceiptVoucher.TotalAfterTaxes, stockReceiptVoucher.VoucherDiscountValue, stockReceiptVoucher.VoucherDiscountPercentage, stockReceiptVoucher.NetTotal, stockReceiptVoucher.Paid, stockReceiptVoucher.ValidityPeriod, stockReceiptVoucher.DeliveryPeriod, stockReceiptVoucher.CostPriceId, stockReceiptVoucher.CurrentQuantity, stockReceiptVoucher.DestinationWarehouseId, stockReceiptVoucher.SystemPageId, stockReceiptVoucher.SelectedId, stockReceiptVoucher.TotalCostPrice, stockReceiptVoucher.TotalItemDirectExpenses, stockReceiptVoucher.IsDelivered, stockReceiptVoucher.IsAccepted, stockReceiptVoucher.IsLinked, stockReceiptVoucher.IsCompleted, stockReceiptVoucher.IsPosted, stockReceiptVoucher.UserId, stockReceiptVoucher.IsActive, stockReceiptVoucher.IsDeleted, stockReceiptVoucher.AutoCreated, stockReceiptVoucher.Notes, stockReceiptVoucher.Image, stockReceiptVoucher.UpdatedId, stockReceiptVoucherDetails, stockReceiptVoucherPatchDetails);

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("StockReceiptVoucher", "Edit", "AddEdit", id, null, "سندات صرف المخازن");
                    QueryHelper.AddLog(new MyLog()
                    {
                        ArAction = "تعديل سند صرف مخزني",
                        EnAction = "AddEdit",
                        ControllerName = "StockReceiptVoucher",
                        UserName = User.Identity.Name,
                        UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                        LogDate = DateTime.Now,
                        RequestMethod = "POST",
                        SelectedItem = stockReceiptVoucher.Id,
                        CodeOrDocNo = stockReceiptVoucher.DocumentNumber
                    });
                    /////////-----------------------------------------------------------------------
                    return Json(new { success = "true", id = stockReceiptVoucher.Id });

                }
                else
                {
                    stockReceiptVoucher.IsActive = true;
                    MyXML.xPathName = "Details";
                    var stockReceiptVoucherDetails = MyXML.GetXML(stockReceiptVoucher.StockReceiptVoucherDetails);
                    stockReceiptVoucher.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    var resultId = new System.Data.Entity.Core.Objects.ObjectParameter("Id", typeof(Int32));
                    db.StockReceiptVoucher_Insert(resultId, stockReceiptVoucher.BranchId, stockReceiptVoucher.WarehouseId, stockReceiptVoucher.DepartmentId, stockReceiptVoucher.VoucherDate, stockReceiptVoucher.VendorOrCustomerId, stockReceiptVoucher.CurrencyId, stockReceiptVoucher.CurrencyEquivalent, stockReceiptVoucher.Total, stockReceiptVoucher.TotalItemsDiscount, stockReceiptVoucher.SalesTaxes, stockReceiptVoucher.TotalAfterTaxes, stockReceiptVoucher.VoucherDiscountValue, stockReceiptVoucher.VoucherDiscountPercentage, stockReceiptVoucher.NetTotal, stockReceiptVoucher.Paid, stockReceiptVoucher.ValidityPeriod, stockReceiptVoucher.DeliveryPeriod, stockReceiptVoucher.CostPriceId, stockReceiptVoucher.CurrentQuantity, stockReceiptVoucher.DestinationWarehouseId, stockReceiptVoucher.SystemPageId, stockReceiptVoucher.SelectedId, stockReceiptVoucher.TotalCostPrice, stockReceiptVoucher.TotalItemDirectExpenses, stockReceiptVoucher.IsDelivered, stockReceiptVoucher.IsAccepted, stockReceiptVoucher.IsLinked, stockReceiptVoucher.IsCompleted, false, stockReceiptVoucher.UserId, stockReceiptVoucher.IsActive, stockReceiptVoucher.IsDeleted, stockReceiptVoucher.AutoCreated, stockReceiptVoucher.Notes, stockReceiptVoucher.Image, stockReceiptVoucher.UpdatedId, stockReceiptVoucherDetails, false, stockReceiptVoucherPatchDetails);


                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("StockReceiptVoucher", "Add", "AddEdit", stockReceiptVoucher.Id, null, "سندات صرف المخازن");

                    ////////-----------------------------------------------------------------------

                    QueryHelper.AddLog(new MyLog()
                    {
                        ArAction = "اضافة سند صرف مخزني",
                        EnAction = "AddEdit",
                        ControllerName = "StockReceiptVoucher",
                        UserName = User.Identity.Name,
                        UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                        LogDate = DateTime.Now,
                        RequestMethod = "POST",
                        SelectedItem = stockReceiptVoucher.Id,
                        CodeOrDocNo = stockReceiptVoucher.DocumentNumber
                    });
                    return Json(new { success = "true", id = resultId.Value });
                }
            }
            var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();

            return Content("false");
        }



        [SkipERPAuthorize]
        public JsonResult GetSerialNumber(int? invoiceId, int? itemId)
        {
            var stockReceiptVoucer = db.StockReceiptVouchers.Find(invoiceId);
            var SerialNumbers = JsonConvert.SerializeObject(db.PurchaseSaleSerialNumbers.Where(s => s.SelectedId == stockReceiptVoucer.SelectedId && s.ItemId == itemId && s.PageSourceId == stockReceiptVoucer.SystemPageId).Select(d => new { d.ItemId, d.SerialNumber }));


            return Json(SerialNumbers, JsonRequestBehavior.AllowGet);
        }

        // GET
        public ActionResult AddEditSerialNumber(int? id, int itemId)
        {
            var session = Session["lang"];
            SystemSetting sysObj = db.SystemSettings.FirstOrDefault();
            ViewData["ShowSerialNumbers"] = true;
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            StockReceiptVoucher stockReceiptVoucer = db.StockReceiptVouchers.Find(id);
            if (stockReceiptVoucer == null)
            {
                return HttpNotFound();
            }
            if (itemId == 0)
            {
                var x = db.StockReceiptVoucherDetails.Where(p => p.MainDocId == id).FirstOrDefault().ItemId;
                itemId = int.Parse(x.ToString());
            }
            ViewBag.GeneralItemId = itemId;


            ViewBag.BranchId = new SelectList(db.Branches.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new {
                Id = b.Id,
                ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
            }), "Id", "ArName", stockReceiptVoucer.BranchId);
            if (userId == 1)
            {
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new {
                    Id = b.Id,
                    ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
                }), "Id", "ArName", stockReceiptVoucer.DepartmentId);
                ViewBag.WarehouseId = new SelectList(db.Warehouses.Where(b => b.IsActive == true && b.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
                }), "Id", "ArName", stockReceiptVoucer.WarehouseId);

            }
            else
            {
                ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new {
                    Id = b.DepartmentId,
                    ArName = session.ToString() == "en" && b.Department.EnName != null ? b.Department.Code + " - " + b.Department.EnName : b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", stockReceiptVoucer.DepartmentId);
                ViewBag.WarehouseId = new SelectList(db.UserWareHouses.Where(b => b.UserId == userId).Select(b => new
                {
                    Id = b.WareHouseId,
                    ArName = session.ToString() == "en" && b.Warehouse.EnName != null ? b.Warehouse.Code + " - " + b.Warehouse.EnName : b.Warehouse.Code + " - " + b.Warehouse.ArName
                }), "Id", "ArName", stockReceiptVoucer.WarehouseId);

            }
            ViewBag.SerialNumbers = JsonConvert.SerializeObject(db.PurchaseSaleSerialNumbers.Where(s => s.SelectedId == stockReceiptVoucer.SelectedId && s.ItemId == itemId && s.PageSourceId == stockReceiptVoucer.SystemPageId).Select(d => new { d.ItemId, d.SerialNumber }));

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح سيريال سند التوريد ",
                EnAction = "AddEdit",
                ControllerName = "StockReceiptVoucher",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = stockReceiptVoucer.Id,
                CodeOrDocNo = stockReceiptVoucer.DocumentNumber
            });

            return View(stockReceiptVoucer);
        }


        // POST: StockReceiptVoucher/Delete/5
        [HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                StockReceiptVoucher stockReceiptVoucher = db.StockReceiptVouchers.Find(id);
                if (stockReceiptVoucher.IsPosted == true)
                {
                    return Content("false");
                }
                if (stockReceiptVoucher.AutoCreated == true)
                {
                    return Content("autoCreated");
                }
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.StockReceiptVoucher_Delete(id, userId);
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف سند توريد مخزني",
                    EnAction = "AddEdit",
                    ControllerName = "StockReceiptVoucher",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = stockReceiptVoucher.DocumentNumber
                });

                ////-------------------- Notification-------------------------////
                Notification.GetNotification("StockReceiptVoucher", "Delete", "Delete", id, null, "سندات صرف المخازن");

                ///////////-----------------------------------------------------------------------

                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }
        [SkipERPAuthorize]
        public JsonResult SetDocNum(int id, DateTime? VoucherDate)
        {
                bool IsExistInDocumentsCoding = false;
                var noOfDigits = (int?)0;
                var YearFormat = (int?)0;
                var CodingTypeId = (int?)0;
                var IsZerosFills = (bool?)null;
                var newDocNo = "";
                var GeneratedDocNo = "";
                var lastObj = db.StockReceiptVouchers.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
                var lastDocNo = lastObj != null ? lastObj.DocumentNumber : "0";
                var DepartmentCode = db.Departments.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == id).FirstOrDefault().Code;
                DepartmentCode = double.Parse(DepartmentCode) < 10 ? "0" + DepartmentCode : DepartmentCode;
                var DepartmentDoc = db.DocumentsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id).FirstOrDefault();
                var MonthFormat = VoucherDate.Value.Month < 10 ? "0" + VoucherDate.Value.Month.ToString() : VoucherDate.Value.Month.ToString();
                if (DepartmentDoc == null)
                {
                    DepartmentDoc = db.DocumentsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.AllDepartments == true).FirstOrDefault();
                    if (DepartmentDoc == null)
                    {
                        IsExistInDocumentsCoding = false;
                    }
                    else
                    {
                        IsExistInDocumentsCoding = true;
                    }
                }
                else
                {
                    IsExistInDocumentsCoding = true;
                }
                if (IsExistInDocumentsCoding == true)
                {
                    noOfDigits = DepartmentDoc.DigitsNo;
                    YearFormat = DepartmentDoc.YearFormat;
                    CodingTypeId = DepartmentDoc.CodingTypeId;
                    IsZerosFills = DepartmentDoc.IsZerosFills;
                    YearFormat = YearFormat == 2 ? int.Parse(VoucherDate.Value.Year.ToString().Substring(2, 2)) : int.Parse(VoucherDate.Value.Year.ToString());

                    if (CodingTypeId == 1)//آلي
                    {
                        if (lastDocNo.Contains("-"))
                        {
                            var ar = lastDocNo.Split('-');
                            if (IsZerosFills == true)
                            {
                                newDocNo = QueryHelper.FillsWithZeros(noOfDigits, (double.Parse(ar[3]) + 1).ToString());
                            }
                            else
                            {
                                newDocNo = (double.Parse(ar[3]) + 1).ToString();
                            }
                        }
                        else
                        {
                            if (IsZerosFills == true)
                            {
                                newDocNo = QueryHelper.FillsWithZeros(noOfDigits, (double.Parse(lastDocNo) + 1).ToString());
                            }
                            else
                            {
                                newDocNo = (double.Parse(lastDocNo) + 1).ToString();
                            }
                        }
                        GeneratedDocNo = DepartmentCode + "-" + YearFormat + "-" + MonthFormat + "-" + newDocNo;
                    }
                    else if (CodingTypeId == 2)//متصل شهري
                    {
                        lastObj = db.StockReceiptVouchers.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Month == VoucherDate.Value.Month && a.VoucherDate.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
                        if (lastObj != null)
                        {
                            if (lastObj.DocumentNumber.Contains("-"))
                            {
                                var ar = lastObj.DocumentNumber.Split('-');
                                if (double.Parse(ar[2]) == VoucherDate.Value.Month)
                                {
                                    if (IsZerosFills == true)
                                    {
                                        newDocNo = QueryHelper.FillsWithZeros(noOfDigits, (double.Parse(ar[3]) + 1).ToString());
                                    }
                                    else
                                    {
                                        newDocNo = (double.Parse(ar[3]) + 1).ToString();
                                    }
                                }
                                else
                                {
                                    if (IsZerosFills == true)
                                    {
                                        newDocNo = QueryHelper.FillsWithZeros(noOfDigits, "1").ToString();
                                    }
                                    else
                                    {
                                        newDocNo = "1";
                                    }
                                }
                            }
                            else
                            {
                                if (IsZerosFills == true)
                                {
                                    newDocNo = QueryHelper.FillsWithZeros(noOfDigits, (double.Parse(lastObj.DocumentNumber) + 1).ToString()).ToString();
                                }
                                else
                                {
                                    newDocNo = (double.Parse(lastObj.DocumentNumber) + 1).ToString();
                                }
                            }
                        }
                        else
                        {
                            if (IsZerosFills == true)
                            {
                                newDocNo = QueryHelper.FillsWithZeros(noOfDigits, "1").ToString();
                            }
                            else
                            {
                                newDocNo = "1";
                            }
                        }
                        GeneratedDocNo = DepartmentCode + "-" + YearFormat + "-" + MonthFormat + "-" + newDocNo;
                    }
                    else if (CodingTypeId == 3)//متصل سنوي
                    {
                        lastObj = db.StockReceiptVouchers.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

                        if (lastObj != null)
                        {
                            if (lastObj.DocumentNumber.Contains("-"))
                            {
                                var ar = lastObj.DocumentNumber.Split('-');
                                var VoucherDateFormate = int.Parse(ar[1]).ToString().Length == 2 ? int.Parse((VoucherDate.Value.Year.ToString()).Substring(2, 2)) : VoucherDate.Value.Year;
                                if (double.Parse(ar[1]) == VoucherDateFormate)
                                {
                                    if (IsZerosFills == true)
                                    {
                                        newDocNo = QueryHelper.FillsWithZeros(noOfDigits, (double.Parse(ar[3]) + 1).ToString());
                                    }
                                    else
                                    {
                                        newDocNo = (double.Parse(ar[3]) + 1).ToString();
                                    }
                                }
                                else
                                {
                                    if (IsZerosFills == true)
                                    {
                                        newDocNo = QueryHelper.FillsWithZeros(noOfDigits, "1").ToString();
                                    }
                                    else
                                    {
                                        newDocNo = "1";
                                    }
                                }
                            }
                            else
                            {
                                if (IsZerosFills == true)
                                {
                                    newDocNo = QueryHelper.FillsWithZeros(noOfDigits, (double.Parse(lastObj.DocumentNumber) + 1).ToString()).ToString();
                                }
                                else
                                {
                                    newDocNo = (double.Parse(lastObj.DocumentNumber) + 1).ToString();
                                }
                            }

                        }
                        else
                        {
                            if (IsZerosFills == true)
                            {
                                newDocNo = QueryHelper.FillsWithZeros(noOfDigits, "1").ToString();
                            }
                            else
                            {
                                newDocNo = "1";
                            }
                        }
                        GeneratedDocNo = DepartmentCode + "-" + YearFormat + "-" + MonthFormat + "-" + newDocNo;
                    }
                }
                else
                {
                    if (lastDocNo.Contains("-"))
                    {
                        var ar = lastDocNo.Split('-');
                        newDocNo = (double.Parse(ar[3]) + 1).ToString();
                    }
                    else
                    {
                        newDocNo = (double.Parse(lastDocNo) + 1).ToString();
                    }
                    GeneratedDocNo = newDocNo;
                }
                return Json(GeneratedDocNo, JsonRequestBehavior.AllowGet);
                //var docNo = QueryHelper.DocLastNum(id, "StockReceiptVoucher");
                //double i = (docNo) + 1;
                //return Json(i, JsonRequestBehavior.AllowGet);
            }

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

        // GET
        [SkipERPAuthorize]
        public ActionResult StockReceiptVoucherBarcode(int? id, int itemId)
        {
            var session = Session["lang"];
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            StockReceiptVoucher stockReceiptVoucher = db.StockReceiptVouchers.Find(id);
            if (stockReceiptVoucher == null)
            {
                return HttpNotFound();
            }

            var warehouseOBItems = db.WarehouseOBDetails.Where(p => p.WarehouseOBId == id).ToList();

            ViewBag.WarehouseOBItems = warehouseOBItems;

            ViewBag.GeneralItemId = itemId;

            ViewBag.Date = stockReceiptVoucher.VoucherDate.ToString() != "" ? stockReceiptVoucher.VoucherDate.ToString("yyyy-MM-ddTHH:mm") : "";

            ViewBag.BranchId = new SelectList(db.Branches.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                Id = b.Id,
                ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
            }), "Id", "ArName", stockReceiptVoucher.BranchId);
            if (userId == 1)
            {
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
                }), "Id", "ArName", stockReceiptVoucher.DepartmentId);
                ViewBag.WarehouseId = new SelectList(db.Warehouses.Where(b => b.IsActive == true && b.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
                }), "Id", "ArName", stockReceiptVoucher.WarehouseId);

            }
            else
            {
                ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = session.ToString() == "en" && b.Department.EnName != null ? b.Department.Code + " - " + b.Department.EnName : b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", stockReceiptVoucher.DepartmentId);
                ViewBag.WarehouseId = new SelectList(db.UserWareHouses.Where(b => b.UserId == userId).Select(b => new
                {
                    Id = b.WareHouseId,
                    ArName = session.ToString() == "en" && b.Warehouse.EnName != null ? b.Warehouse.Code + " - " + b.Warehouse.EnName : b.Warehouse.Code + " - " + b.Warehouse.ArName
                }), "Id", "ArName", stockReceiptVoucher.WarehouseId);

            }

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح باركود الارصدة الافتتاحية",
                EnAction = "AddEdit",
                ControllerName = "WarehouseOBBarcode",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = stockReceiptVoucher.Id,
                CodeOrDocNo = stockReceiptVoucher.DocumentNumber
            });

            return View(stockReceiptVoucher);
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
