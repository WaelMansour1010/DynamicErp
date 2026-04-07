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
using MyERP.Repository;

namespace MyERP.Controllers
{
    [SkipERPAuthorize]
    [StockIssueAuthorize]
    public class StockIssueVoucherController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: StockIssueVoucher
        public ActionResult Index(int type, int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
            ViewBag.ShowSerialNumbers = systemSetting.ShowSerialNumbers == true;
            ViewBag.Type = type;
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة سندات صرف المخازن",
                EnAction = "Index",
                ControllerName = "StockIssueVoucher",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("StockIssueVoucher", "View", "Index", null, null, "سندات صرف المخازن");

            /////////-----------------------------------------------------------------------

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            /////////////////////////// Search ////////////////////

            var depIds = db.Departments.Where(d => (userId == 1 || db.UserDepartments.Where(u => u.UserId == userId && u.Privilege == true).Select(u => u.DepartmentId).Contains(d.Id)) && d.IsDeleted == false && d.IsActive == true).Select(d => (int?)d.Id);

            IQueryable<StockIssueVoucher> stockIssueVouchers;

            if (string.IsNullOrEmpty(searchWord))
            {
                stockIssueVouchers = db.StockIssueVouchers.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && s.TypeId == type).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.StockIssueVouchers.Where(s => s.TypeId == type && s.IsDeleted == false && depIds.Contains(s.DepartmentId)).Count();
            }
            else
            {
                stockIssueVouchers = db.StockIssueVouchers.Where(s => depIds.Contains(s.DepartmentId) && s.IsDeleted == false && s.TypeId == type && (s.DocumentNumber.Contains(searchWord) || s.Branch.ArName.Contains(searchWord) || s.Warehouse.ArName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.VendorOrCustomerId.ToString().Contains(searchWord) || s.Currency.ArName.Contains(searchWord) || s.SystemPage.ArName.Contains(searchWord) || s.Notes.Contains(searchWord))).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.StockIssueVouchers.Where(s => depIds.Contains(s.DepartmentId) && s.IsDeleted == false && s.TypeId == type && (s.DocumentNumber.Contains(searchWord) || s.Branch.ArName.Contains(searchWord) || s.Warehouse.ArName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.VendorOrCustomerId.ToString().Contains(searchWord) || s.Currency.ArName.Contains(searchWord) || s.SystemPage.ArName.Contains(searchWord) || s.Notes.Contains(searchWord))).Count();
            }
            int roleId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("RoleId").Value);
            var ShowCost = userId == 1 ? true : db.UserPrivileges.Where(u => u.PageAction.PageId == 56 && u.PageAction.EnName == "ShowAvgCostPrice" && u.PageAction.Action == "ShowAvgCostPrice" && u.UserId == userId).Select(x => x.Privileged).FirstOrDefault();
            ShowCost = ShowCost ?? db.RolePrivileges.Where(u => u.PageAction.PageId == 56 && u.PageAction.EnName == "ShowAvgCostPrice" && u.PageAction.Action == "ShowAvgCostPrice" && u.RoleId == roleId).Select(x => x.Privileged).FirstOrDefault();
            if (ShowCost != true)
                ShowCost = false;
            ViewBag.ShowCost = ShowCost;
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(stockIssueVouchers.ToList());

        }

        // GET: StockIssueVoucher/AddEdit/5
        public ActionResult AddEdit(int? id, int? type)
        {
            var session = Session["lang"];
            ViewBag.Type = type == null ? 1 : type;
            SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
            ViewBag.UseExpiryDateForItems = systemSetting.UseExpiryDateForItems;

            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            int sysPageId = QueryHelper.SourcePageId("StockIssueVoucher");
            ViewBag.SystemPageId = sysPageId;
            int roleId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("RoleId").Value);
            var ShowCost = userId == 1 ? true : db.UserPrivileges.Where(u => u.PageAction.PageId == 56 && u.PageAction.EnName == "ShowAvgCostPrice" && u.PageAction.Action == "ShowAvgCostPrice" && u.UserId == userId).Select(x => x.Privileged).FirstOrDefault();
            ShowCost = ShowCost ?? db.RolePrivileges.Where(u => u.PageAction.PageId == 56 && u.PageAction.EnName == "ShowAvgCostPrice" && u.PageAction.Action == "ShowAvgCostPrice" && u.RoleId == roleId).Select(x => x.Privileged).FirstOrDefault();
            if (ShowCost != true)
                ShowCost = false;
            ViewBag.ShowCost = ShowCost;

            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            var departments = departmentRepository.UserDepartments(1).ToList();//show all deprartments so that user can select factory department
            ViewBag.LinkWithDocModalDepartmentId = new SelectList(departments, "Id", "ArName", systemSetting.DefaultDepartmentId);

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
            StockIssueVoucher stockIssueVoucher = db.StockIssueVouchers.Find(id);
            if (stockIssueVoucher == null)
            {
                return HttpNotFound();
            }
            if (stockIssueVoucher.AutoCreated == true)
            {
                var sysPage = db.SystemPages.Where(a => a.Id == stockIssueVoucher.SystemPageId).Select(x => new { x.ArName, x.TableName }).FirstOrDefault();
                ViewBag.SystemPage = sysPage.ArName;
                var table = sysPage.TableName;
                stockIssueVoucher.SelectedId = int.Parse(db.Database.SqlQuery<string>($"select top(1)([DocumentNumber]) from[{table}] where [Id] = " + stockIssueVoucher.SelectedId).FirstOrDefault());
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
            if (userId == 1)
            {
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
                }), "Id", "ArName", stockIssueVoucher.DepartmentId);
                ViewBag.WarehouseId = new SelectList(db.Warehouses.Where(b => b.IsActive == true && b.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
                }), "Id", "ArName", stockIssueVoucher.WarehouseId);
            }
            else
            {
                ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId && d.Privilege == true && d.Department.IsActive == true && d.Department.IsDeleted == false).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = session.ToString() == "en" && b.Department.EnName != null ? b.Department.Code + " - " + b.Department.EnName : b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", stockIssueVoucher.DepartmentId);
                ViewBag.WarehouseId = new SelectList(db.UserWareHouses.Where(b => b.UserId == userId && b.Privilege == true && b.Warehouse.IsActive == true && b.Warehouse.IsDeleted == false && b.Warehouse.DepartmentId == stockIssueVoucher.DepartmentId).Select(b => new
                {
                    Id = b.WareHouseId,
                    ArName = session.ToString() == "en" && b.Warehouse.EnName != null ? b.Warehouse.Code + " - " + b.Warehouse.EnName : b.Warehouse.Code + " - " + b.Warehouse.ArName
                }), "Id", "ArName", stockIssueVoucher.WarehouseId);
            }

            ViewBag.VoucherDate = stockIssueVoucher.VoucherDate.ToString("yyyy-MM-ddTHH:mm");

            ViewBag.Next = QueryHelper.Next((int)id, "StockIssueVoucher");
            ViewBag.Previous = QueryHelper.Previous((int)id, "StockIssueVoucher");
            ViewBag.Last = QueryHelper.GetLast("StockIssueVoucher");
            ViewBag.First = QueryHelper.GetFirst("StockIssueVoucher");

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل سند صرف مخزني",
                EnAction = "AddEdit",
                ControllerName = "StockIssueVoucher",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = stockIssueVoucher.Id,
                CodeOrDocNo = stockIssueVoucher.DocumentNumber
            });
            return View(stockIssueVoucher);
        }

        // POST: StockIssueVoucher/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult AddEdit([Bind(Include = "Id,DocumentNumber,BranchId,WarehouseId,DepartmentId,VoucherDate,VendorOrCustomerId,CurrencyId,CurrencyEquivalent,Total,TotalItemsDiscount,SalesTaxes,TotalAfterTaxes,VoucherDiscountValue,VoucherDiscountPercentage,NetTotal,Paid,ValidityPeriod,DeliveryPeriod,CostPriceId,CurrentQuantity,DestinationWarehouseId,SystemPageId,SelectedId,TotalCostPrice,TotalItemDirectExpenses,IsDelivered,IsAccepted,IsLinked,IsCompleted,IsPosted,UserId,IsActive,IsDeleted,AutoCreated,Notes,Image,UpdatedId,StockIssueVoucherDetails,TypeId")] StockIssueVoucher stockIssueVoucher)
        {
            if (ModelState.IsValid)
            {
                var id = stockIssueVoucher.Id;
                stockIssueVoucher.IsDeleted = false;
                if (stockIssueVoucher.Id > 0)
                {
                    if (db.StockIssueVouchers.Find(stockIssueVoucher.Id).IsPosted == true)
                    {
                        return Content("false");
                    }
                    MyXML.xPathName = "Details";
                    var StockIssueVoucherDetails = MyXML.GetXML(stockIssueVoucher.StockIssueVoucherDetails);
                    stockIssueVoucher.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                    db.StockIssueVoucher_Update(stockIssueVoucher.Id, stockIssueVoucher.DocumentNumber, stockIssueVoucher.BranchId, stockIssueVoucher.WarehouseId, stockIssueVoucher.DepartmentId, stockIssueVoucher.VoucherDate, stockIssueVoucher.VendorOrCustomerId, stockIssueVoucher.CurrencyId, stockIssueVoucher.CurrencyEquivalent, stockIssueVoucher.Total, stockIssueVoucher.TotalItemsDiscount, stockIssueVoucher.SalesTaxes, stockIssueVoucher.TotalAfterTaxes, stockIssueVoucher.VoucherDiscountValue, stockIssueVoucher.VoucherDiscountPercentage, stockIssueVoucher.NetTotal, stockIssueVoucher.Paid, stockIssueVoucher.ValidityPeriod, stockIssueVoucher.DeliveryPeriod, stockIssueVoucher.CostPriceId, stockIssueVoucher.CurrentQuantity, stockIssueVoucher.DestinationWarehouseId, stockIssueVoucher.SystemPageId, stockIssueVoucher.SelectedId, stockIssueVoucher.TotalCostPrice, stockIssueVoucher.TotalItemDirectExpenses, stockIssueVoucher.IsDelivered, stockIssueVoucher.IsAccepted, stockIssueVoucher.IsLinked, stockIssueVoucher.IsCompleted, stockIssueVoucher.IsPosted, stockIssueVoucher.UserId, stockIssueVoucher.IsActive, stockIssueVoucher.IsDeleted, stockIssueVoucher.AutoCreated, stockIssueVoucher.Notes, stockIssueVoucher.Image, stockIssueVoucher.UpdatedId, StockIssueVoucherDetails, stockIssueVoucher.TypeId);

                    ////-------------------- Notification-------------------------////

                    ////////////-----------------------------------------------------------------------
                    Notification.GetNotification("StockIssueVoucher", "Edit", "AddEdit", id, null, "سندات صرف المخازن");
                    QueryHelper.AddLog(new MyLog()
                    {
                        ArAction = "تعديل سند صرف مخزني",
                        EnAction = "AddEdit",
                        ControllerName = "StockIssueVoucher",
                        UserName = User.Identity.Name,
                        UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                        LogDate = DateTime.Now,
                        RequestMethod = "POST",
                        SelectedItem = stockIssueVoucher.Id,
                        CodeOrDocNo = stockIssueVoucher.DocumentNumber
                    });
                    return Json(new { success = "true", id = stockIssueVoucher.Id });
                }
                else
                {
                    stockIssueVoucher.IsActive = true;
                    MyXML.xPathName = "Details";
                    var StockIssueVoucherDetails = MyXML.GetXML(stockIssueVoucher.StockIssueVoucherDetails);
                    stockIssueVoucher.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    var resultId = new System.Data.Entity.Core.Objects.ObjectParameter("Id", typeof(Int32));
                    db.StockIssueVoucher_Insert(resultId, stockIssueVoucher.BranchId, stockIssueVoucher.WarehouseId, stockIssueVoucher.DepartmentId, stockIssueVoucher.VoucherDate, stockIssueVoucher.VendorOrCustomerId, stockIssueVoucher.CurrencyId, stockIssueVoucher.CurrencyEquivalent, stockIssueVoucher.Total, stockIssueVoucher.TotalItemsDiscount, stockIssueVoucher.SalesTaxes, stockIssueVoucher.TotalAfterTaxes, stockIssueVoucher.VoucherDiscountValue, stockIssueVoucher.VoucherDiscountPercentage, stockIssueVoucher.NetTotal, stockIssueVoucher.Paid, stockIssueVoucher.ValidityPeriod, stockIssueVoucher.DeliveryPeriod, stockIssueVoucher.CostPriceId, stockIssueVoucher.CurrentQuantity, stockIssueVoucher.DestinationWarehouseId, stockIssueVoucher.SystemPageId, stockIssueVoucher.SelectedId, stockIssueVoucher.TotalCostPrice, stockIssueVoucher.TotalItemDirectExpenses, stockIssueVoucher.IsDelivered, stockIssueVoucher.IsAccepted, stockIssueVoucher.IsLinked, stockIssueVoucher.IsCompleted, false, stockIssueVoucher.UserId, stockIssueVoucher.IsActive, stockIssueVoucher.IsDeleted, stockIssueVoucher.AutoCreated, stockIssueVoucher.Notes, stockIssueVoucher.Image, stockIssueVoucher.UpdatedId, StockIssueVoucherDetails, stockIssueVoucher.TypeId);

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("StockIssueVoucher", "Add", "AddEdit", stockIssueVoucher.Id, null, "سندات صرف المخازن");

                    /////////-----------------------------------------------------------------------

                    QueryHelper.AddLog(new MyLog()
                    {
                        ArAction = "اضافة سند صرف مخزني",
                        EnAction = "AddEdit",
                        ControllerName = "StockIssueVoucher",
                        UserName = User.Identity.Name,
                        UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                        LogDate = DateTime.Now,
                        RequestMethod = "POST",
                        SelectedItem = stockIssueVoucher.Id,
                        CodeOrDocNo = stockIssueVoucher.DocumentNumber
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
            var stockIssueVoucer = db.StockIssueVouchers.Find(invoiceId);
            var SerialNumbers = JsonConvert.SerializeObject(db.PurchaseSaleSerialNumbers.Where(s => s.SelectedId == stockIssueVoucer.SelectedId && s.ItemId == itemId && s.PageSourceId == stockIssueVoucer.SystemPageId).Select(d => new { d.ItemId, d.SerialNumber }));


            return Json(SerialNumbers, JsonRequestBehavior.AllowGet);
        }

        // GET
        public ActionResult AddEditSerialNumber(int? id, int itemId)
        {
            var session = Session["lang"];
            SystemSetting sysObj = db.SystemSettings.FirstOrDefault();
            ViewData["ShowSerialNumbers"] = true;


            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            StockIssueVoucher stockIssueVoucer = db.StockIssueVouchers.Find(id);
            if (stockIssueVoucer == null)
            {
                return HttpNotFound();
            }
            if (itemId == 0)
            {
                var x = db.StockIssueVoucherDetails.Where(p => p.MainDocId == id).FirstOrDefault().ItemId;
                itemId = int.Parse(x.ToString());
            }
            ViewBag.GeneralItemId = itemId;
            ViewBag.BranchId = new SelectList(db.Branches.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                Id = b.Id,
                ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
            }), "Id", "ArName", stockIssueVoucer.BranchId);
            if (userId == 1)
            {
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
                }), "Id", "ArName", stockIssueVoucer.DepartmentId);
                ViewBag.WarehouseId = new SelectList(db.Warehouses.Where(b => b.IsActive == true && b.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
                }), "Id", "ArName", stockIssueVoucer.WarehouseId);
            }
            else
            {
                ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = session.ToString() == "en" && b.Department.EnName != null ? b.Department.Code + " - " + b.Department.EnName : b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", stockIssueVoucer.DepartmentId);
                ViewBag.WarehouseId = new SelectList(db.UserWareHouses.Where(b => b.UserId == userId).Select(b => new
                {
                    Id = b.WareHouseId,
                    ArName = session.ToString() == "en" && b.Warehouse.EnName != null ? b.Warehouse.Code + " - " + b.Warehouse.EnName : b.Warehouse.Code + " - " + b.Warehouse.ArName
                }), "Id", "ArName", stockIssueVoucer.WarehouseId);

            }
            ViewBag.SerialNumbers = JsonConvert.SerializeObject(db.PurchaseSaleSerialNumbers.Where(s => s.SelectedId == stockIssueVoucer.SelectedId && s.ItemId == itemId && s.PageSourceId == stockIssueVoucer.SystemPageId).Select(d => new { d.ItemId, d.SerialNumber }));



            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح سيريال سند الصرف ",
                EnAction = "AddEdit",
                ControllerName = "StockIssueVoucher",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = stockIssueVoucer.Id,
                CodeOrDocNo = stockIssueVoucer.DocumentNumber
            });

            return View(stockIssueVoucer);
        }


        // POST: StockIssueVoucher/Delete/5
        [HttpPost, ActionName("Delete")]

        public ActionResult DeleteConfirmed(int id)
        {
            StockIssueVoucher stockIssueVoucher = db.StockIssueVouchers.Find(id);
            if (stockIssueVoucher.IsPosted == true)
            {
                return Content("false");
            }
            if (stockIssueVoucher.AutoCreated == true)
            {
                return Content("autoCreated");
            }
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            db.StockIssueVoucher_Delete(id, userId);

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "حذف سند صرف مخزني",
                EnAction = "AddEdit",
                ControllerName = "StockIssueVoucher",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "POST",
                SelectedItem = id,
                CodeOrDocNo = stockIssueVoucher.DocumentNumber
            });
            //////////////-----------------------------------------------------------------------
            Notification.GetNotification("StockIssueVoucher", "Delete", "Delete", id, null, "سندات صرف المخازن");

            return Content("true");
        }

        [HttpPost]
        public JsonResult AcceptVoucher(int id)
        {
            db.StockIssueVoucher_Accept(id);
            return Json(true);
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
            var lastObj = db.StockIssueVouchers.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.StockIssueVouchers.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Month == VoucherDate.Value.Month && a.VoucherDate.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.StockIssueVouchers.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

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
            //var docNo = QueryHelper.DocLastNum(id, "StockIssueVoucher");
            //double i = (docNo) + 1;
            //return Json(i, JsonRequestBehavior.AllowGet);
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
