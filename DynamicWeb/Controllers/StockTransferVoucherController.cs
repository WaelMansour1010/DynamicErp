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
using System.Data.Entity.Core.Objects;

namespace MyERP.Controllers
{
    public class StockTransferVoucherController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: StockTransferVoucher
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة تحويل المخازن",
                EnAction = "Index",
                ControllerName = "StockTransferVoucher",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("StockTransferVoucher", "View", "Index", null, null, "تحويل المخازن");
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            var depIds = db.Departments.Where(d => (userId == 1 || db.UserDepartments.Where(u => u.UserId == userId && u.Privilege == true).Select(u => u.DepartmentId).Contains(d.Id)) && d.IsDeleted == false && d.IsActive == true).Select(d => (int?)d.Id);
            ///////////-----------------------------------------------------------------------
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            /////////////////////////// Search ////////////////////

            IQueryable<StockTransferVoucher> stockTransferVouchers;

            if (string.IsNullOrEmpty(searchWord))
            {
                stockTransferVouchers = db.StockTransferVouchers.Where(s => s.IsDeleted == false && (depIds.Contains(s.DepartmentId) || (depIds.Contains(s.DestinationDepartmentId)))).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.StockTransferVouchers.Where(s => s.IsDeleted == false && (depIds.Contains(s.DepartmentId) || (depIds.Contains(s.DestinationDepartmentId)))).Count();
            }
            else
            {
                stockTransferVouchers = db.StockTransferVouchers.Where(s => s.IsDeleted == false && (depIds.Contains(s.DepartmentId) || (depIds.Contains(s.DestinationDepartmentId))) && (s.DocumentNumber.Contains(searchWord) || s.Branch.ArName.Contains(searchWord) || s.Warehouse.ArName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Currency.ArName.Contains(searchWord) || s.SystemPage.ArName.Contains(searchWord) || s.Notes.Contains(searchWord))).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);

                ViewBag.Count = db.StockTransferVouchers.Where(s => s.IsDeleted == false && (depIds.Contains(s.DepartmentId) || (depIds.Contains(s.DestinationDepartmentId))) && (s.DocumentNumber.Contains(searchWord) || s.Branch.ArName.Contains(searchWord) || s.Warehouse.ArName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Currency.ArName.Contains(searchWord) || s.SystemPage.ArName.Contains(searchWord) || s.Notes.Contains(searchWord))).Count();
            }
            int roleId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("RoleId").Value);

            var ShowCost = userId == 1 ? true : db.UserPrivileges.Where(u => u.PageAction.PageId == 56 && u.PageAction.EnName == "ShowAvgCostPrice" && u.PageAction.Action == "ShowAvgCostPrice" && u.UserId == userId).Select(x => x.Privileged).FirstOrDefault();
            ShowCost = ShowCost ?? db.RolePrivileges.Where(u => u.PageAction.PageId == 56 && u.PageAction.EnName == "ShowAvgCostPrice" && u.PageAction.Action == "ShowAvgCostPrice" && u.RoleId == roleId).Select(x => x.Privileged).FirstOrDefault();
            if (ShowCost != true)
                ShowCost = false;
            ViewBag.ShowCost = ShowCost;
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(stockTransferVouchers.ToList());
        }

        // GET: StockTransferVoucher/AddEdit/5
        public ActionResult AddEdit(int? id)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
            ViewBag.UseExpiryDateForItems = systemSetting.UseExpiryDateForItems;
            int roleId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("RoleId").Value);
            var ShowCost = userId == 1 ? true : db.UserPrivileges.Where(u => u.PageAction.PageId == 56 && u.PageAction.EnName == "ShowAvgCostPrice" && u.PageAction.Action == "ShowAvgCostPrice" && u.UserId == userId).Select(x => x.Privileged).FirstOrDefault();
            ShowCost = ShowCost ?? db.RolePrivileges.Where(u => u.PageAction.PageId == 56 && u.PageAction.EnName == "ShowAvgCostPrice" && u.PageAction.Action == "ShowAvgCostPrice" && u.RoleId == roleId).Select(x => x.Privileged).FirstOrDefault();
            if (ShowCost != true)
                ShowCost = false;
            ViewBag.ShowCost = ShowCost;
            if (id == null)
            {
                var destinationWarehouses = db.Warehouses.Where(b => b.IsActive == true && b.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                });
                ViewBag.DestinationWarehouseId = new SelectList(destinationWarehouses, "Id", "ArName");
                if (userId == 1)
                {
                    ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                    {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", systemSetting.DefaultDepartmentId);
                    var warehouses = db.Warehouses.Where(b => b.IsActive == true && b.IsDeleted == false && b.DepartmentId == systemSetting.DefaultDepartmentId).Select(b => new
                    {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    });
                    ViewBag.WarehouseId = new SelectList(warehouses, "Id", "ArName", systemSetting.DefaultWarehouseId);
                    //var destinationWarehouses = db.Warehouses.Where(b => b.IsActive == true && b.IsDeleted == false).Select(b => new
                    //{
                    //    Id = b.Id,
                    //    ArName = b.Code + " - " + b.ArName
                    //});
                    //ViewBag.DestinationWarehouseId = new SelectList(destinationWarehouses, "Id", "ArName");

                }
                else
                {
                    ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId && d.Privilege == true && d.Department.IsActive == true && d.Department.IsDeleted == false).Select(b => new
                    {
                        Id = b.DepartmentId,
                        ArName = b.Department.Code + " - " + b.Department.ArName
                    }), "Id", "ArName", systemSetting.DefaultDepartmentId);
                    var warehouses = db.UserWareHouses.Where(b => b.UserId == userId && b.Privilege == true && b.Warehouse.IsActive == true && b.Warehouse.IsDeleted == false && b.Warehouse.DepartmentId == systemSetting.DefaultDepartmentId).Select(b => new
                    {
                        Id = b.WareHouseId,
                        ArName = b.Warehouse.Code + " - " + b.Warehouse.ArName
                    });
                    ViewBag.WarehouseId = new SelectList(warehouses, "Id", "ArName", systemSetting.DefaultWarehouseId);

                    //var destinationWarehouses = db.UserWareHouses.Where(b => b.UserId == userId && b.Privilege == true && b.Warehouse.IsActive == true && b.Warehouse.IsDeleted == false ).Select(b => new
                    //{
                    //    Id = b.WareHouseId,
                    //    ArName = b.Warehouse.Code + " - " + b.Warehouse.ArName
                    //});
                    //ViewBag.DestinationWarehouseId = new SelectList(destinationWarehouses, "Id", "ArName");
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
            StockTransferVoucher stockTransferVoucher = db.StockTransferVouchers.Find(id);
            if (stockTransferVoucher == null)
            {
                return HttpNotFound();
            }
            if (userId == 1)
            {
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", stockTransferVoucher.DepartmentId);
                var warehouses = db.Warehouses.Where(b => b.IsActive == true && b.IsDeleted == false && b.DepartmentId == stockTransferVoucher.DepartmentId).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                });

                ViewBag.WarehouseId = new SelectList(warehouses, "Id", "ArName", stockTransferVoucher.WarehouseId);
                // all warehouses of all departments
                var destinationWarehouses = db.Warehouses.Where(b => b.IsActive == true && b.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                });
                ViewBag.DestinationWarehouseId = new SelectList(destinationWarehouses, "Id", "ArName", stockTransferVoucher.DestinationWarehouseId);
            }
            else
            {
                ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId && d.Privilege == true && d.Department.IsActive == true && d.Department.IsDeleted == false).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", stockTransferVoucher.DepartmentId);
                var warehouses = db.UserWareHouses.Where(b => b.UserId == userId && b.Warehouse.IsDeleted == false && b.Warehouse.IsActive == true && b.Warehouse.DepartmentId == stockTransferVoucher.DepartmentId).Select(b => new
                {
                    Id = b.WareHouseId,
                    ArName = b.Warehouse.Code + " - " + b.Warehouse.ArName
                });
                ViewBag.WarehouseId = new SelectList(warehouses, "Id", "ArName", stockTransferVoucher.WarehouseId);
                // all warehouses of all departments
                var destinationWarehouses = db.UserWareHouses.Where(b => b.UserId == userId && b.Warehouse.IsDeleted == false && b.Warehouse.IsActive == true).Select(b => new
                {
                    Id = b.WareHouseId,
                    ArName = b.Warehouse.Code + " - " + b.Warehouse.ArName
                });
                ViewBag.DestinationWarehouseId = new SelectList(destinationWarehouses, "Id", "ArName", stockTransferVoucher.DestinationWarehouseId);
            }
            // make sure that transfer voucher has one receipt and one issue and one journal
            List<StockIssueVoucher> stockIssueVouchers = db.StockIssueVouchers.Where(p => p.IsActive == true && p.IsDeleted == false && p.SystemPageId == 53 && p.SelectedId == stockTransferVoucher.Id).OrderBy(p => p.DocumentNumber).ToList();
            List<StockReceiptVoucher> stockReceiptVouchers = db.StockReceiptVouchers.Where(p => p.IsActive == true && p.IsDeleted == false && p.SystemPageId == 53 && p.SelectedId == stockTransferVoucher.Id).OrderBy(p => p.DocumentNumber).ToList();
            List<JournalEntry> JournalEntries = db.JournalEntries.Where(p => p.IsActive == true && p.IsDeleted == false && p.SourcePageId == 53 && p.SourceId == stockTransferVoucher.Id).OrderBy(p => p.DocumentNumber).ToList();
            ViewBag.StockReceiptVouchers = stockReceiptVouchers;
            ViewBag.stockIssueVouchers = stockIssueVouchers;
            ViewBag.JournalEntries = JournalEntries;
            ViewBag.VoucherDate = stockTransferVoucher.VoucherDate.ToString("yyyy-MM-ddTHH:mm");

            ViewBag.Next = QueryHelper.Next((int)id, "StockTransferVoucher");
            ViewBag.Previous = QueryHelper.Previous((int)id, "StockTransferVoucher");
            ViewBag.Last = QueryHelper.GetLast("StockTransferVoucher");
            ViewBag.First = QueryHelper.GetFirst("StockTransferVoucher");

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل سند تحويل مخزني",
                EnAction = "AddEdit",
                ControllerName = "StockTransferVoucher",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = stockTransferVoucher.Id,
                CodeOrDocNo = stockTransferVoucher.DocumentNumber
            });
            var depIds = db.Departments.Where(d => (userId == 1 || db.UserDepartments.Where(u => u.UserId == userId && u.Privilege == true)
            .Select(u => u.DepartmentId).Contains(d.Id)) && d.IsDeleted == false && d.IsActive == true).Select(d => (int?)d.Id).ToList();
            // المستخدم مش هو الAdmin ولا الفروع بتاعته بتحتوي على الفرع المحول منه
            if (depIds.Contains(stockTransferVoucher.DepartmentId) == true || userId == 1)
            {
                ViewBag.Msg = "Allow";
            }
            else
            {
                ViewBag.Msg = "NotAllow";
            }
            if (depIds.Contains(stockTransferVoucher.DestinationDepartmentId) == true || userId == 1)
            {
                ViewBag.Accept = "Accept";
            }
            return View(stockTransferVoucher);
        }

        // POST: StockTransferVoucher/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult AddEdit(/*[Bind(Include = "Id,DocumentNumber,BranchId,WarehouseId,DepartmentId,VoucherDate,VendorOrCustomerId,CurrencyId,CurrencyEquivalent,Total,TotalItemsDiscount,SalesTaxes,TotalAfterTaxes,VoucherDiscountValue,VoucherDiscountPercentage,NetTotal,Paid,ValidityPeriod,DeliveryPeriod,CostPriceId,CurrentQuantity,DestinationWarehouseId,SystemPageId,SelectedId,TotalCostPrice,TotalItemDirectExpenses,IsDelivered,IsAccepted,IsLinked,IsCompleted,IsPosted,UserId,IsActive,IsDeleted,AutoCreated,Notes,Image,UpdatedId,StockTransferVoucherDetails")]*/ StockTransferVoucher stockTransferVoucher)
        {
            if (ModelState.IsValid)
            {
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                stockTransferVoucher.UserId = userId;
                var id = stockTransferVoucher.Id;
                stockTransferVoucher.IsDeleted = false;
                if (stockTransferVoucher.Id > 0)
                {
                    if (db.StockTransferVouchers.Find(stockTransferVoucher.Id).IsPosted == true)
                    {
                        return Content("false");
                    }
                    MyXML.xPathName = "Details";
                    var stockTransferVoucherDetails = MyXML.GetXML(stockTransferVoucher.StockTransferVoucherDetails);
                    stockTransferVoucher.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                    db.StockTransferVoucher_Update(stockTransferVoucher.Id, stockTransferVoucher.DocumentNumber, stockTransferVoucher.BranchId, stockTransferVoucher.WarehouseId, stockTransferVoucher.DepartmentId, stockTransferVoucher.VoucherDate, stockTransferVoucher.VendorOrCustomerId, stockTransferVoucher.CurrencyId, stockTransferVoucher.CurrencyEquivalent, stockTransferVoucher.Total, stockTransferVoucher.TotalItemsDiscount, stockTransferVoucher.SalesTaxes, stockTransferVoucher.TotalAfterTaxes, stockTransferVoucher.VoucherDiscountValue, stockTransferVoucher.VoucherDiscountPercentage, stockTransferVoucher.NetTotal, stockTransferVoucher.Paid, stockTransferVoucher.ValidityPeriod, stockTransferVoucher.DeliveryPeriod, stockTransferVoucher.CostPriceId, stockTransferVoucher.CurrentQuantity, stockTransferVoucher.DestinationWarehouseId, stockTransferVoucher.SystemPageId, stockTransferVoucher.SelectedId, stockTransferVoucher.TotalCostPrice, stockTransferVoucher.TotalItemDirectExpenses, stockTransferVoucher.IsDelivered, stockTransferVoucher.IsAccepted, stockTransferVoucher.IsLinked, stockTransferVoucher.IsCompleted, stockTransferVoucher.IsPosted, stockTransferVoucher.UserId, stockTransferVoucher.IsActive, stockTransferVoucher.IsDeleted, stockTransferVoucher.AutoCreated, stockTransferVoucher.Notes, stockTransferVoucher.Image, stockTransferVoucher.UpdatedId, stockTransferVoucherDetails, stockTransferVoucher.CostPriceIncreasePercentage, stockTransferVoucher.DestinationDepartmentId);

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("StockTransferVoucher", "Edit", "AddEdit", id, null, "تحويل المخازن");
                    QueryHelper.AddLog(new MyLog()
                    {
                        ArAction = "تعديل سند تحويل مخزني",
                        EnAction = "AddEdit",
                        ControllerName = "StockTransferVoucher",
                        UserName = User.Identity.Name,
                        UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                        LogDate = DateTime.Now,
                        RequestMethod = "POST",
                        SelectedItem = stockTransferVoucher.Id,
                        CodeOrDocNo = stockTransferVoucher.DocumentNumber
                    });
                    return Json(new { success = "true", id = stockTransferVoucher.Id });
                    /////////////-----------------------------------------------------------------------
                }
                else
                {
                    stockTransferVoucher.IsActive = true;
                    MyXML.xPathName = "Details";
                    var stockTransferVoucherDetails = MyXML.GetXML(stockTransferVoucher.StockTransferVoucherDetails);

                    var ResultId = new ObjectParameter("Id", typeof(Int32));
                    db.StockTransferVoucher_Insert(ResultId, stockTransferVoucher.BranchId, stockTransferVoucher.WarehouseId, stockTransferVoucher.DepartmentId, stockTransferVoucher.VoucherDate, stockTransferVoucher.VendorOrCustomerId, stockTransferVoucher.CurrencyId, stockTransferVoucher.CurrencyEquivalent, stockTransferVoucher.Total, stockTransferVoucher.TotalItemsDiscount, stockTransferVoucher.SalesTaxes, stockTransferVoucher.TotalAfterTaxes, stockTransferVoucher.VoucherDiscountValue, stockTransferVoucher.VoucherDiscountPercentage, stockTransferVoucher.NetTotal, stockTransferVoucher.Paid, stockTransferVoucher.ValidityPeriod, stockTransferVoucher.DeliveryPeriod, stockTransferVoucher.CostPriceId, stockTransferVoucher.CurrentQuantity, stockTransferVoucher.DestinationWarehouseId, stockTransferVoucher.SystemPageId, stockTransferVoucher.SelectedId, stockTransferVoucher.TotalCostPrice, stockTransferVoucher.TotalItemDirectExpenses, stockTransferVoucher.IsDelivered, stockTransferVoucher.IsAccepted, stockTransferVoucher.IsLinked, stockTransferVoucher.IsCompleted, false, stockTransferVoucher.UserId, stockTransferVoucher.IsActive, stockTransferVoucher.IsDeleted, stockTransferVoucher.AutoCreated, stockTransferVoucher.Notes, stockTransferVoucher.Image, stockTransferVoucher.UpdatedId, stockTransferVoucherDetails, stockTransferVoucher.CostPriceIncreasePercentage, stockTransferVoucher.DestinationDepartmentId);

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("StockTransferVoucher", "Add", "AddEdit", stockTransferVoucher.Id, null, "تحويل المخازن");

                    //////////////-----------------------------------------------------------------------

                    QueryHelper.AddLog(new MyLog()
                    {
                        ArAction = "اضافة سند تحويل مخزني",
                        EnAction = "AddEdit",
                        ControllerName = "StockTransferVoucher",
                        UserName = User.Identity.Name,
                        UserId = userId,
                        LogDate = DateTime.Now,
                        RequestMethod = "POST",
                        SelectedItem = stockTransferVoucher.Id,
                        CodeOrDocNo = stockTransferVoucher.DocumentNumber
                    });
                    return Json(new { success = "true", id = ResultId.Value });
                }
            }
            var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();

            return Content("false");
        }

        [HttpPost]
        [SkipERPAuthorize]
        public JsonResult AcceptTransferVoucher(int id)
        {
            int? roleId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("RoleId").Value);
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            var destinationWarehouseId = db.StockTransferVouchers.Where(s => s.Id == id).Select(x => x.DestinationWarehouseId).FirstOrDefault();
            var empId = db.Warehouses.Where(x => x.Id == destinationWarehouseId).Select(x => x.ResponsibleEmpId).FirstOrDefault();
            var destinationWarhouseUserId = db.Employees.Where(x => x.Id == empId).Select(x => x.UserId).FirstOrDefault();

            var canAccept = db.UserPrivileges.Where(u => u.PageAction.PageId == 53 && u.PageAction.EnName == "AcceptTransferVoucher" && u.PageAction.Action == "AcceptTransferVoucher" && u.UserId == userId).Select(x => x.Privileged).FirstOrDefault();
            canAccept = canAccept ?? db.RolePrivileges.Where(u => u.PageAction.PageId == 53 && u.PageAction.EnName == "AcceptTransferVoucher" && u.PageAction.Action == "AcceptTransferVoucher" && u.RoleId == roleId).Select(x => x.Privileged).FirstOrDefault();
            if (userId == 1 || canAccept == true)
            {
                db.StockTransferVoucher_Accept(id);
                return Json(new { success = "true" });
            }
            return Json(new { success = "false", cause = "unauthorized" });
        }

        // POST: StockTransferVoucher/Delete/5
        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            StockTransferVoucher stockTransferVoucher = db.StockTransferVouchers.Find(id);
            if (stockTransferVoucher.IsPosted == true)
            {
                return Content("false");
            }
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            db.StockTransferVoucher_Delete(id, userId);

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "حذف سند تحويل مخزني",
                EnAction = "AddEdit",
                ControllerName = "StockTransferVoucher",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "POST",
                SelectedItem = id,
                CodeOrDocNo = stockTransferVoucher.DocumentNumber
            });

            ////-------------------- Notification-------------------------////
            Notification.GetNotification("StockTransferVoucher", "Delete", "Delete", id, null, "تحويل  المخازن");

            return Content("true");
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
            var lastObj = db.StockTransferVouchers.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.StockTransferVouchers.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Month == VoucherDate.Value.Month && a.VoucherDate.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.StockTransferVouchers.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

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
            //var docNo = QueryHelper.DocLastNum(id, "StockTransferVoucher");
            //double i = (docNo) + 1;
            //return Json(i, JsonRequestBehavior.AllowGet);
        }

        public JsonResult WarehousesByDepartmentId(int id)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            if (userId == 1)
            {
                var warehouses = db.Warehouses.Where(w => w.IsActive == true && w.IsDeleted == false && w.DepartmentId == id).Select(w => new { w.Id, ArName = w.Code + " - " + w.ArName });
                return Json(warehouses, JsonRequestBehavior.AllowGet);
            }
            else
            {
                var warehouses = db.UserWareHouses.Where(w => w.UserId == userId && w.Warehouse.DepartmentId == id).Select(w => new { w.Warehouse.Id, ArName = w.Warehouse.Code + " - " + w.Warehouse.ArName });
                return Json(warehouses, JsonRequestBehavior.AllowGet);
            }
        }

        [SkipERPAuthorize]
        public JsonResult GetDetails(int id)
        {
            var details = db.StockTransferVoucherDetails.Where(a => a.IsDeleted == false && a.MainDocId == id).Select(a => new
            {
                a.StockTransferVoucher.IsAccepted,
                a.StockTransferVoucher.DocumentNumber,
                From = a.StockTransferVoucher.Warehouse.ArName,
                To = a.StockTransferVoucher.Warehouse1.ArName,
                Item = a.Item.ArName,
                a.Qty,
                Unit = a.ItemUnit.ArName
            }).ToList();

            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            var depIds = db.Departments.Where(d => (userId == 1 || db.UserDepartments.Where(u => u.UserId == userId && u.Privilege == true).Select(u => u.DepartmentId).Contains(d.Id)) && d.IsDeleted == false && d.IsActive == true).Select(d => (int?)d.Id).ToList();
            var AdminId = db.ERPUsers.Where(a => a.IsActive == true && a.IsDeleted == false).FirstOrDefault().Id;
            var stockTransferVoucher = db.StockTransferVouchers.Find(id);
            var Accept = "False";
            if (stockTransferVoucher != null)
            {
                if (depIds.Contains(stockTransferVoucher.DestinationDepartmentId) == true || userId == 1)
                {
                    Accept = "True";
                }
            }

            return Json(new { details, Accept }, JsonRequestBehavior.AllowGet);
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
