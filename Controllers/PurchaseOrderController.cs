using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;

namespace MyERP.Controllers
{
    public class PurchaseOrderController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: PurchaseOrder
        public async Task<ActionResult> Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            var depIds = db.Departments.Where(d => (userId == 1 || db.UserDepartments.Where(u => u.UserId == userId && u.Privilege == true).Select(u => u.DepartmentId).Contains(d.Id)) && d.IsDeleted == false && d.IsActive == true).Select(d => (int?)d.Id);
            Notification.GetNotification("PurchaseOrder", "View", "Index", null, null, "اوامر الشراء");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<PurchaseOrder> purchaseOrders;

            if (string.IsNullOrEmpty(searchWord))
            {
                purchaseOrders = db.PurchaseOrders.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId)).Include(s => s.Branch).Include(s => s.Currency).Include(s => s.Department).Include(s => s.Warehouse).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = await db.PurchaseOrders.Where(s => s.IsDeleted == false && s.IsActive == true && depIds.Contains(s.DepartmentId)).CountAsync();
            }
            else
            {
                purchaseOrders = db.PurchaseOrders.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (s.DocumentNumber.Contains(searchWord)||s.Department.ArName.Contains(searchWord)||s.Department.EnName.Contains(searchWord)||s.Warehouse.ArName.Contains(searchWord)||s.Warehouse.EnName.Contains(searchWord)||s.Vendor.ArName.Contains(searchWord)||s.Vendor.EnName.Contains(searchWord)||s.TotalAfterTaxes.ToString().Contains(searchWord))).Include(s => s.Branch).Include(s => s.Currency).Include(s => s.Department).Include(s => s.Warehouse).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = await db.PurchaseOrders.Where(s => s.IsDeleted == false && s.IsActive == true && depIds.Contains(s.DepartmentId) && (s.DocumentNumber.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Department.EnName.Contains(searchWord) || s.Warehouse.ArName.Contains(searchWord) || s.Warehouse.EnName.Contains(searchWord) || s.Vendor.ArName.Contains(searchWord) || s.Vendor.EnName.Contains(searchWord) || s.TotalAfterTaxes.ToString().Contains(searchWord))).CountAsync();
            }

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة اوامر الشراء",
                EnAction = "Index",
                ControllerName = "PurchaseOrder",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            return View(purchaseOrders.ToList());
        }

        // GET: PurchaseOrder/Edit/5
        public async Task<ActionResult> AddEdit(int? id)
        {
            SystemSetting systemSetting = await db.SystemSettings.AnyAsync() ? await db.SystemSettings.FirstOrDefaultAsync() : new SystemSetting();
            ViewBag.ShowSerialNumbers = systemSetting.ShowSerialNumbers == true;

            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            var canChangeItemPrice = userId == 1 ? true : await db.UserPrivileges.Where(u => u.PageAction.EnName == "ChangeItemPrice" && u.PageAction.Action == "ChangeItemPrice" && u.UserId == userId).Select(x => x.Privileged).FirstOrDefaultAsync();

            if (id == null)
            {
                if (userId == 1)
                {
                    ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                    {
                        b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", systemSetting.DefaultDepartmentId);
                    ViewBag.WarehouseId = new SelectList(db.Warehouses.Where(b => b.IsActive == true && b.IsDeleted == false && b.DepartmentId == systemSetting.DefaultDepartmentId).Select(b => new
                    {
                        b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", systemSetting.DefaultWarehouseId);
                }
                else
                {
                    ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId && d.Department.IsDeleted == false && d.Department.IsActive == true && d.Privilege == true).Select(b => new
                    {
                        Id = b.DepartmentId,
                        ArName = b.Department.Code + " - " + b.Department.ArName
                    }), "Id", "ArName", systemSetting.DefaultDepartmentId);
                    ViewBag.WarehouseId = new SelectList(db.UserWareHouses.Where(b => b.UserId == userId && b.Warehouse.DepartmentId == systemSetting.DefaultDepartmentId).Select(b => new
                    {
                        Id = b.WareHouseId,
                        ArName = b.Warehouse.Code + " - " + b.Warehouse.ArName
                    }), "Id", "ArName", systemSetting.DefaultWarehouseId);
                }

                ViewBag.VendorOrCustomerId = new SelectList(db.Vendors.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    b.Id,
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
            PurchaseOrder purchaseOrder = db.PurchaseOrders.Find(id);
            if (purchaseOrder == null)
            {
                return HttpNotFound();
            }
            ViewBag.VendorOrCustomerId = new SelectList(db.Vendors.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", purchaseOrder.VendorOrCustomerId);

            if (userId == 1)
            {
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", purchaseOrder.DepartmentId);
                ViewBag.WarehouseId = new SelectList(db.Warehouses.Where(b => b.IsActive == true && b.IsDeleted == false && b.DepartmentId == purchaseOrder.DepartmentId).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", purchaseOrder.WarehouseId);

            }
            else
            {
                ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId && d.Privilege == true && d.Department.IsActive == true && d.Department.IsDeleted == false).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", purchaseOrder.DepartmentId);
                ViewBag.WarehouseId = new SelectList(db.UserWareHouses.Where(b => b.UserId == userId && b.Privilege == true && b.Warehouse.IsActive == true && b.Warehouse.IsDeleted == false && b.Warehouse.DepartmentId == purchaseOrder.DepartmentId).Select(b => new
                {
                    Id = b.WareHouseId,
                    ArName = b.Warehouse.Code + " - " + b.Warehouse.ArName
                }), "Id", "ArName", purchaseOrder.WarehouseId);

            }

            ViewBag.VoucherDate = purchaseOrder.VoucherDate.ToString("yyyy-MM-ddTHH:mm");

            ViewBag.Next = QueryHelper.Next((int)id, "PurchaseOrder");
            ViewBag.Previous = QueryHelper.Previous((int)id, "PurchaseOrder");
            ViewBag.Last = QueryHelper.GetLast("PurchaseOrder");
            ViewBag.First = QueryHelper.GetFirst("PurchaseOrder");

            return View(purchaseOrder);
        }

        // POST: PurchaseOrder/Edit/5
        [HttpPost]
        public async Task<JsonResult> AddEdit(PurchaseOrder purchaseOrder)
        {
            int userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            purchaseOrder.UserId = userId;
            if (ModelState.IsValid)
            {
                var id = purchaseOrder.Id;
                purchaseOrder.IsDeleted = false;
                if (purchaseOrder.Id > 0)
                {
                    if ((await db.PurchaseOrders.Where(x => x.Id == purchaseOrder.Id).Select(x => x.IsPosted).FirstOrDefaultAsync()) == true)
                    {
                        return Json(new { success = "false" });
                    }
                    MyXML.xPathName = "Details";
                    var purchaseOrderDetails = MyXML.GetXML(purchaseOrder.PurchaseOrderDetails);

                    await Task.Run(() => db.PurchaseOrder_Update(purchaseOrder.Id, purchaseOrder.DocumentNumber, purchaseOrder.BranchId, purchaseOrder.WarehouseId, purchaseOrder.DepartmentId, purchaseOrder.VoucherDate, purchaseOrder.VendorOrCustomerId, purchaseOrder.CurrencyId, purchaseOrder.CurrencyEquivalent, purchaseOrder.Total, purchaseOrder.TotalItemsDiscount, purchaseOrder.SalesTaxes, purchaseOrder.TotalAfterTaxes, purchaseOrder.VoucherDiscountValue, purchaseOrder.VoucherDiscountPercentage, purchaseOrder.NetTotal, purchaseOrder.Paid, purchaseOrder.ValidityPeriod, purchaseOrder.DeliveryPeriod, purchaseOrder.CostPriceId, purchaseOrder.CurrentQuantity, purchaseOrder.DestinationWarehouseId, purchaseOrder.SystemPageId, purchaseOrder.SelectedId, purchaseOrder.TotalCostPrice, purchaseOrder.TotalItemDirectExpenses, purchaseOrder.IsDelivered, purchaseOrder.IsAccepted, purchaseOrder.IsLinked, purchaseOrder.IsCompleted, purchaseOrder.IsPosted, purchaseOrder.UserId, purchaseOrder.IsActive, purchaseOrder.IsDeleted, purchaseOrder.AutoCreated, purchaseOrder.Notes, purchaseOrder.Image, purchaseOrder.UpdatedId, purchaseOrder.CommercialRevenueTaxAmount, purchaseOrderDetails));

                    Notification.GetNotification("PurchaseOrder", "Edit", "AddEdit", id, null, "اوامر شراء");
                }
                else
                {
                    purchaseOrder.IsActive = true;
                    MyXML.xPathName = "Details";
                    var purchaseOrderDetails = MyXML.GetXML(purchaseOrder.PurchaseOrderDetails);
                    var idResult = new ObjectParameter("Id", typeof(Int32));
                    await Task.Run(() => db.PurchaseOrder_Insert(idResult, purchaseOrder.BranchId, purchaseOrder.WarehouseId, purchaseOrder.DepartmentId, purchaseOrder.VoucherDate, purchaseOrder.VendorOrCustomerId, purchaseOrder.CurrencyId, purchaseOrder.CurrencyEquivalent, purchaseOrder.Total, purchaseOrder.TotalItemsDiscount, purchaseOrder.SalesTaxes, purchaseOrder.TotalAfterTaxes, purchaseOrder.VoucherDiscountValue, purchaseOrder.VoucherDiscountPercentage, purchaseOrder.NetTotal, purchaseOrder.Paid, purchaseOrder.ValidityPeriod, purchaseOrder.DeliveryPeriod, purchaseOrder.CostPriceId, purchaseOrder.CurrentQuantity, purchaseOrder.DestinationWarehouseId, purchaseOrder.SystemPageId, purchaseOrder.SelectedId, purchaseOrder.TotalCostPrice, purchaseOrder.TotalItemDirectExpenses, purchaseOrder.IsDelivered, purchaseOrder.IsAccepted, purchaseOrder.IsLinked, purchaseOrder.IsCompleted, false, purchaseOrder.UserId, purchaseOrder.IsActive, purchaseOrder.IsDeleted, purchaseOrder.AutoCreated, purchaseOrder.Notes, purchaseOrder.Image, purchaseOrder.UpdatedId, purchaseOrder.CommercialRevenueTaxAmount, purchaseOrderDetails));
                    id = (int)idResult.Value;
                    purchaseOrder.Id = id;
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("PurchaseOrder", "Add", "AddEdit", id, null, "اوامر شراء");
                }

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = purchaseOrder.Id > 0 ? "تعديل اوامر شراء " : "اضافة اوامر شراء",
                    EnAction = "AddEdit",
                    ControllerName = "PurchaseOrder",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = purchaseOrder.Id > 0 ? purchaseOrder.Id : db.SalesOrders.Max(i => i.Id),
                    CodeOrDocNo = purchaseOrder.DocumentNumber
                });

                return Json(new { success = "true", id });
            }
            var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();

            return Json(new { success = "false", errors });
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
            var lastObj = db.PurchaseOrders.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.PurchaseOrders.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Month == VoucherDate.Value.Month && a.VoucherDate.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.PurchaseOrders.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

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

            //var docNo = QueryHelper.DocLastNum(id, "PurchaseOrder");
            //double i = (docNo) + 1;
            //return Json(i, JsonRequestBehavior.AllowGet);
        }
        [HttpPost, ActionName("Delete")]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            if (await db.ManufacturingPurchaseRequests.Where(x => x.PurchaseRequestId == id).AnyAsync())
                return Content("false");

            PurchaseOrder purchaseOrder = db.PurchaseOrders.Find(id);
            purchaseOrder.IsDeleted = true;
            purchaseOrder.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            foreach(var item in purchaseOrder.PurchaseOrderDetails)
            {
                item.IsDeleted = true;
            }
            Random random = new Random();
            const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
            purchaseOrder.DocumentNumber = Code;
            db.Entry(purchaseOrder).State = EntityState.Modified;

            db.SaveChanges();
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "حذف اوامر الشراء",
                EnAction = "AddEdit",
                ControllerName = "PurchaseOrder",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "POST",
                SelectedItem = id,
                CodeOrDocNo = purchaseOrder.DocumentNumber
            });

            ////-------------------- Notification-------------------------////
            Notification.GetNotification("PurchaseOrder", "Delete", "Delete", id, null, "اوامر الشراء");

            ///////////-----------------------------------------------------------------------

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
