using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using System.Security.Claims;
using System.Data.Entity.Core.Objects;

namespace MyERP.Controllers
{
    public class InventoryVoucherController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: InventoryVoucher
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            var depIds = db.Departments.Where(d => (userId == 1 || db.UserDepartments.Where(u => u.UserId == userId && u.Privilege == true).Select(u => u.DepartmentId).Contains(d.Id)) && d.IsDeleted == false && d.IsActive == true).Select(d => (int?)d.Id);
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة الجرد",
                EnAction = "Index",
                ControllerName = "InventoryVoucher",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("InventoryVoucher", "View", "Index", null, null, "الجرد");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<InventoryVoucher> inventoryVouchers;

            if (string.IsNullOrEmpty(searchWord))
            {
                inventoryVouchers = db.InventoryVouchers.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId)).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.InventoryVouchers.Where(s => s.IsDeleted == false && s.IsActive == true && depIds.Contains(s.DepartmentId)).Count();
            }
            else
            {
                inventoryVouchers = db.InventoryVouchers.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (s.DocumentNumber.Contains(searchWord) || s.Branch.ArName.Contains(searchWord) || s.Warehouse.ArName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Currency.ArName.Contains(searchWord) || s.SystemPage.ArName.Contains(searchWord) || s.Notes.Contains(searchWord))).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.InventoryVouchers.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (s.DocumentNumber.Contains(searchWord) || s.Branch.ArName.Contains(searchWord) || s.Warehouse.ArName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Currency.ArName.Contains(searchWord) || s.SystemPage.ArName.Contains(searchWord) || s.Notes.Contains(searchWord))).Count();

            }

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(inventoryVouchers.ToList());
        }

        // GET: InventoryVoucher/Edit/5
        public ActionResult AddEdit(int? id)
        {
            SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();

            ViewBag.UseExpiryDateForItems = systemSetting.UseExpiryDateForItems;
            ViewBag.ShowItemsInsteadOfItemGroupsInInventoryVoucher = systemSetting.ShowItemsInsteadOfItemGroupsInInventoryVoucher;
            ViewBag.ShowDepartmentItemsInInventoryVoucher = systemSetting.ShowDepartmentItemsInInventoryVoucher;

            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            if (id == null)
            {
                if (userId == 1)
                {
                    ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                    {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", systemSetting.DefaultDepartmentId);
                    ViewBag.WarehouseId = new SelectList(db.Warehouses.Where(b => b.IsActive == true && b.IsDeleted == false && b.DepartmentId == systemSetting.DefaultDepartmentId).Select(b => new
                    {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", systemSetting.DefaultWarehouseId);
                }
                else
                {
                    ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId && d.Privilege == true && d.Department.IsActive == true && d.Department.IsDeleted == false).Select(b => new
                    {
                        Id = b.DepartmentId,
                        ArName = b.Department.Code + " - " + b.Department.ArName
                    }), "Id", "ArName", systemSetting.DefaultDepartmentId);
                    ViewBag.WarehouseId = new SelectList(db.UserWareHouses.Where(b => b.UserId == userId && b.Warehouse.IsDeleted == false && b.Warehouse.IsActive == true && b.Warehouse.DepartmentId == systemSetting.DefaultDepartmentId).Select(b => new
                    {
                        Id = b.WareHouseId,
                        ArName = b.Warehouse.Code + " - " + b.Warehouse.ArName
                    }), "Id", "ArName", systemSetting.DefaultWarehouseId);
                }
                ViewBag.ItemGroupId = new SelectList(db.ItemGroups.Where(b => b.IsActive == true && b.IsDeleted == false).Select(b => new
                {
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
                return View();
            }
            InventoryVoucher inventoryVoucher = db.InventoryVouchers.Find(id);
            if (inventoryVoucher == null)
            {
                return HttpNotFound();
            }

            if (userId == 1)
            {
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", inventoryVoucher.DepartmentId);
                ViewBag.WarehouseId = new SelectList(db.Warehouses.Where(b => b.IsDeleted == false && b.IsActive == true && b.DepartmentId == inventoryVoucher.DepartmentId).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", inventoryVoucher.WarehouseId);
            }
            else
            {
                ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId && d.Privilege == true && d.Department.IsActive == true && d.Department.IsDeleted == false).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", inventoryVoucher.DepartmentId);
                ViewBag.WarehouseId = new SelectList(db.UserWareHouses.Where(b => b.UserId == userId && b.Privilege == true && b.Warehouse.IsActive == true && b.Warehouse.IsDeleted == false && b.Warehouse.DepartmentId == inventoryVoucher.DepartmentId).Select(b => new
                {
                    Id = b.WareHouseId,
                    ArName = b.Warehouse.Code + " - " + b.Warehouse.ArName
                }), "Id", "ArName", inventoryVoucher.WarehouseId);
            }

            ViewBag.ItemGroupId = new SelectList(db.ItemGroups.Where(b => b.IsActive == true && b.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", inventoryVoucher.ItemGroupId);
            ViewBag.VoucherDate = inventoryVoucher.VoucherDate.ToString("yyyy-MM-ddTHH:mm");

            //------------------------------------------------------------------------------------------------//
            // make sure that transfer voucher has one receipt and one issue and one journal
            List<JournalEntry> JournalEntries = new List<JournalEntry>();
            var SystemPageId = db.SystemPages.Where(a => a.IsActive == true && a.IsDeleted == false && a.TableName == "InventoryVoucher").FirstOrDefault().Id;
            List<StockIssueVoucher> stockIssueVouchers = db.StockIssueVouchers.Where(p => p.IsActive == true && p.IsDeleted == false && p.SystemPageId == SystemPageId && p.SelectedId == inventoryVoucher.Id).OrderBy(p => p.DocumentNumber).ToList();
            List<StockReceiptVoucher> stockReceiptVouchers = db.StockReceiptVouchers.Where(p => p.IsActive == true && p.IsDeleted == false && p.SystemPageId == SystemPageId && p.SelectedId == inventoryVoucher.Id).OrderBy(p => p.DocumentNumber).ToList();
            foreach (var item in stockIssueVouchers)
            {
                var Entry = db.JournalEntries.Where(p => p.IsActive == true && p.IsDeleted == false && p.SourcePageId == 50 && p.SourceId == item.Id).FirstOrDefault();
                JournalEntries.Add(Entry);
            }
            foreach (var item in stockReceiptVouchers)
            {
                var Entry = db.JournalEntries.Where(p => p.IsActive == true && p.IsDeleted == false && p.SourcePageId == 55 && p.SourceId == item.Id).FirstOrDefault();
                JournalEntries.Add(Entry);
            }
            ViewBag.StockReceiptVouchers = stockReceiptVouchers;
            ViewBag.stockIssueVouchers = stockIssueVouchers;
            ViewBag.JournalEntries = JournalEntries.OrderBy(p => p.DocumentNumber).ToList();
            //------------------------------------------------------------------------------------------------//
            ViewBag.Next = QueryHelper.Next((int)id, "InventoryVoucher");
            ViewBag.Previous = QueryHelper.Previous((int)id, "InventoryVoucher");
            ViewBag.Last = QueryHelper.GetLast("InventoryVoucher");
            ViewBag.First = QueryHelper.GetFirst("InventoryVoucher");

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل الجرد",
                EnAction = "AddEdit",
                ControllerName = "InventoryVoucher",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = inventoryVoucher.Id,
                CodeOrDocNo = inventoryVoucher.DocumentNumber
            });
            return View(inventoryVoucher);
        }

        // POST: InventoryVoucher/Edit/5
        [HttpPost]
        public JsonResult AddEdit(InventoryVoucher inventoryVoucher)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            if (ModelState.IsValid)
            {
                var id = inventoryVoucher.Id;
                inventoryVoucher.IsDeleted = false;
                if (inventoryVoucher.Id > 0)
                {
                    MyXML.xPathName = "Details";
                    var InventoryVoucherDetails = MyXML.GetXML(inventoryVoucher.InventoryVoucherDetails);
                    inventoryVoucher.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                    db.InventoryVoucher_Update(inventoryVoucher.Id, inventoryVoucher.DocumentNumber, inventoryVoucher.BranchId, inventoryVoucher.WarehouseId, inventoryVoucher.DepartmentId, inventoryVoucher.VoucherDate, inventoryVoucher.VendorOrCustomerId, inventoryVoucher.CurrencyId, inventoryVoucher.CurrencyEquivalent, inventoryVoucher.Total, inventoryVoucher.TotalItemsDiscount, inventoryVoucher.SalesTaxes, inventoryVoucher.TotalAfterTaxes, inventoryVoucher.VoucherDiscountValue, inventoryVoucher.VoucherDiscountPercentage, inventoryVoucher.NetTotal, inventoryVoucher.Paid, inventoryVoucher.ValidityPeriod, inventoryVoucher.DeliveryPeriod, inventoryVoucher.CostPriceId, inventoryVoucher.CurrentQuantity, inventoryVoucher.DestinationWarehouseId, inventoryVoucher.SystemPageId, inventoryVoucher.SelectedId, inventoryVoucher.TotalCostPrice, inventoryVoucher.TotalItemDirectExpenses, inventoryVoucher.IsDelivered, inventoryVoucher.IsAccepted, inventoryVoucher.IsLinked, inventoryVoucher.IsCompleted, inventoryVoucher.IsPosted, userId, inventoryVoucher.IsActive, inventoryVoucher.IsDeleted, inventoryVoucher.AutoCreated, inventoryVoucher.Notes, inventoryVoucher.Image, inventoryVoucher.UpdatedId, InventoryVoucherDetails, inventoryVoucher.ItemGroupId, inventoryVoucher.AdjustmentonEmployeeReceivable, inventoryVoucher.InventoryTypeId);

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("InventoryVoucher", "Edit", "AddEdit", id, null, "الجرد");

                }
                else
                {
                    inventoryVoucher.IsActive = true;
                    MyXML.xPathName = "Details";
                    var InventoryVoucherDetails = MyXML.GetXML(inventoryVoucher.InventoryVoucherDetails);
                    inventoryVoucher.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    var idResult = new ObjectParameter("Id", typeof(int));
                    db.InventoryVoucher_Insert(idResult, inventoryVoucher.BranchId, inventoryVoucher.WarehouseId, inventoryVoucher.DepartmentId, inventoryVoucher.VoucherDate, inventoryVoucher.VendorOrCustomerId, inventoryVoucher.CurrencyId, inventoryVoucher.CurrencyEquivalent, inventoryVoucher.Total, inventoryVoucher.TotalItemsDiscount, inventoryVoucher.SalesTaxes, inventoryVoucher.TotalAfterTaxes, inventoryVoucher.VoucherDiscountValue, inventoryVoucher.VoucherDiscountPercentage, inventoryVoucher.NetTotal, inventoryVoucher.Paid, inventoryVoucher.ValidityPeriod, inventoryVoucher.DeliveryPeriod, inventoryVoucher.CostPriceId, inventoryVoucher.CurrentQuantity, inventoryVoucher.DestinationWarehouseId, inventoryVoucher.SystemPageId, inventoryVoucher.SelectedId, inventoryVoucher.TotalCostPrice, inventoryVoucher.TotalItemDirectExpenses, inventoryVoucher.IsDelivered, inventoryVoucher.IsAccepted, inventoryVoucher.IsLinked, inventoryVoucher.IsCompleted, inventoryVoucher.IsPosted, userId, inventoryVoucher.IsActive, inventoryVoucher.IsDeleted, inventoryVoucher.AutoCreated, inventoryVoucher.Notes, inventoryVoucher.Image, inventoryVoucher.UpdatedId, InventoryVoucherDetails, inventoryVoucher.ItemGroupId, inventoryVoucher.AdjustmentonEmployeeReceivable, inventoryVoucher.InventoryTypeId);
                    id = (int)idResult.Value;

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("InventoryVoucher", "Add", "AddEdit", inventoryVoucher.Id, null, "الجرد");

                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = inventoryVoucher.Id > 0 ? "تعديل جرد" : "اضافة جرد",
                    EnAction = "AddEdit",
                    ControllerName = "InventoryVoucher",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = inventoryVoucher.Id,
                    CodeOrDocNo = inventoryVoucher.DocumentNumber
                });

                return Json(new { success = "true", id });
            }
            var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();
            return Json(new { success = "false", errors });
        }

        [HttpPost]
        public JsonResult Adjustment(int id,bool? AdjustmentonEmployeeReceivable)
        {
            if(AdjustmentonEmployeeReceivable!=null)
            {
                var inventoryVoucher = db.InventoryVouchers.Find(id);
                inventoryVoucher.AdjustmentonEmployeeReceivable = AdjustmentonEmployeeReceivable;
                db.Entry(inventoryVoucher).State = EntityState.Modified;
                db.SaveChanges();
            }
            db.InventoryVoucher_Adjustment(id);
            return Json(true);
        }

        // POST: InventoryVoucher/Delete/5
        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            db.InventoryVoucher_Delete(id, userId);
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("InventoryVoucher", "Delete", "Delete", id, null, "الجرد");

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
            var lastObj = db.InventoryVouchers.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.InventoryVouchers.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Month == VoucherDate.Value.Month && a.VoucherDate.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.InventoryVouchers.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

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
            //var docNo = QueryHelper.DocLastNum(id, "InventoryVoucher");
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
