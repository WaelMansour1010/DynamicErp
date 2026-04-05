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
using System.Threading.Tasks;
using MyERP.Repository;
using System.Text.RegularExpressions;

namespace MyERP.Controllers
{
    
    public class SalesQuotationController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: SalesQuotation
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            ViewBag.domainName = domainName;
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            var depIds = db.Departments.Where(d => (userId == 1 || db.UserDepartments.Where(u => u.UserId == userId && u.Privilege == true).Select(u => u.DepartmentId).Contains(d.Id)) && d.IsDeleted == false && d.IsActive == true).Select(d => (int?)d.Id);
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("SalesQuotation", "View", "Index", null, null, "عرض سعر البيع");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<SalesQuotation> salesQuotations;

            if (string.IsNullOrEmpty(searchWord))
            {
                salesQuotations = db.SalesQuotations.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId)).Include(s => s.Branch).Include(s => s.Currency).Include(s => s.Department).Include(s => s.Warehouse).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.SalesQuotations.Where(s => s.IsDeleted == false && s.IsActive == true && depIds.Contains(s.DepartmentId)).Count();

            }
            else
            {
                salesQuotations = db.SalesQuotations.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && s.DocumentNumber.Contains(searchWord)).Include(s => s.Branch).Include(s => s.Currency).Include(s => s.Department).Include(s => s.Warehouse).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = salesQuotations.Count();
            }

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة عرض سعر البيع",
                EnAction = "Index",
                ControllerName = "SalesQuotation",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            return View(salesQuotations.ToList());
        }

        // GET: SalesQuotation/Edit/5
        public async Task<ActionResult> AddEdit(int? id)
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            FillVehicleLookupData(session);
            SystemSetting systemSetting = await db.SystemSettings.AnyAsync() ? await db.SystemSettings.FirstOrDefaultAsync() : new SystemSetting();
            ViewBag.ServiceFeesPercentage = systemSetting.ServiceFeesPercentage.HasValue ? systemSetting.ServiceFeesPercentage : 0;
            ViewBag.ShowItemWidthAndHeightAndAreaInTransactions = systemSetting.ShowItemWidthAndHeightAndAreaInTransactions;

            UserRepository userRepository = new UserRepository(db);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            WarehouseRepository warehouseRepository = new WarehouseRepository(db);
            CashboxReposistory cashboxReposistory = new CashboxReposistory(db);

            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            int roleId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("RoleId").Value);
            var CanChangeItemPrice = userId == 1 ? true : db.UserPrivileges.Where(u => u.PageAction.PageId == 58 && u.PageAction.EnName == "ChangeItemPrice" && u.PageAction.Action == "ChangeItemPrice" && u.UserId == userId).Select(x => x.Privileged).FirstOrDefault();
            CanChangeItemPrice = CanChangeItemPrice ?? db.RolePrivileges.Where(u => u.PageAction.PageId == 58 && u.PageAction.EnName == "ChangeItemPrice" && u.PageAction.Action == "ChangeItemPrice" && u.RoleId == roleId).Select(x => x.Privileged).FirstOrDefault();
            ViewBag.CanChangeItemPrice = CanChangeItemPrice;
            //ViewBag.CanChangeItemPrice = await userRepository.HasActionPrivilege(userId, "ChangeItemPrice", "ChangeItemPrice");
          
            ViewBag.SystemPageId = db.Database.SqlQuery<int>($"select Id from [SystemPage]  where Code='SalesQuotation'").FirstOrDefault();
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            ViewBag.domainName = domainName;
            if (id == null)
            {

                ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", systemSetting.DefaultDepartmentId);

                ViewBag.WarehouseId = new SelectList(warehouseRepository.UserWarehouses(userId, systemSetting.DefaultDepartmentId), "Id", "ArName", systemSetting.DefaultWarehouseId);

                ViewBag.VendorOrCustomerId = new SelectList(db.Customers.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.CustomerRepId = new SelectList(db.CustomerReps.Where(a => (a.IsActive == true)).OrderBy(a => a.IsDefault).Select(b => new
                {
                    b.Employee.Id,
                    ArName = b.Employee.Code + " - " + b.Employee.ArName
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
            SalesQuotation SalesQuotation = db.SalesQuotations.Find(id);

            int sysPageId = QueryHelper.SourcePageId("SalesQuotation");

            if (SalesQuotation == null)
            {
                return HttpNotFound();
            }
            ViewBag.VendorOrCustomerId = new SelectList(db.Customers.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", SalesQuotation.VendorOrCustomerId);

            ViewBag.CustomerRepId = new SelectList(db.CustomerReps.Where(a => (a.IsActive == true)).OrderBy(a => a.IsDefault).Select(b => new
            {
                b.Employee.Id,
                ArName = b.Employee.Code + " - " + b.Employee.ArName
            }), "Id", "ArName", SalesQuotation.CustomerRepId);

            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", SalesQuotation.DepartmentId);
            ViewBag.WarehouseId = new SelectList(warehouseRepository.UserWarehouses(userId, SalesQuotation.DepartmentId), "Id", "ArName", SalesQuotation.WarehouseId);
           

            ViewBag.ItemId = JsonConvert.SerializeObject(db.Items.Where(i => i.IsDeleted == false && i.IsActive == true).Select(i => new { i.ArName, i.Id }));

            ViewBag.ItemPriceId = JsonConvert.SerializeObject(db.ItemPrices.Where(i => i.IsDeleted == false && i.IsActive == true).Select(i => new { i.Barcode, i.Id }));
            try
            {
               // ViewBag.VoucherDate = SalesQuotation.VoucherDate.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.VoucherDate = SalesQuotation.VoucherDate.Value.ToString("yyyy-MM-ddTHH:mm");
            }
            catch (Exception)
            {
            }

            ViewBag.Next = QueryHelper.Next((int)id, "SalesQuotation");
            ViewBag.Previous = QueryHelper.Previous((int)id, "SalesQuotation");
            ViewBag.Last = QueryHelper.GetLast("SalesQuotation");
            ViewBag.First = QueryHelper.GetFirst("SalesQuotation");

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل عرض سعر بيع",
                EnAction = "AddEdit",
                ControllerName = "SalesQuotation",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = SalesQuotation.Id,
                CodeOrDocNo = SalesQuotation.DocumentNumber
            });
            return View(SalesQuotation);
        }

        // POST: SalesQuotation/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult AddEdit(SalesQuotation SalesQuotation)
        {
            try
            {
                var chassisValidationMessage = ValidateVehicleChassisNumbers(SalesQuotation.SalesQuotationDetails, SalesQuotation.Id);
                if (!string.IsNullOrEmpty(chassisValidationMessage))
                    return Json(new { success = "false", message = chassisValidationMessage });
                int userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                SalesQuotation.UserId = userId;
                if (ModelState.IsValid)
                {
                    var id = SalesQuotation.Id;
                    SalesQuotation.IsDeleted = false;
                    var systemSetting = db.SystemSettings.FirstOrDefault();
                    var AutomaticallyApprovingSalesQuotation = systemSetting.AutomaticallyApprovingSalesQuotation;
                    if (SalesQuotation.Id > 0)
                    {
                        if (db.SalesQuotations.Find(SalesQuotation.Id).IsPosted == true)
                        {
                            return Content("false");
                        }
                        MyXML.xPathName = "Details";
                        var SalesQuotationDetails = MyXML.GetXML(SalesQuotation.SalesQuotationDetails);



                        db.SalesQuotation_Update(SalesQuotation.Id, SalesQuotation.DocumentNumber, SalesQuotation.BranchId, SalesQuotation.WarehouseId, SalesQuotation.DepartmentId, SalesQuotation.VoucherDate, SalesQuotation.VendorOrCustomerId , SalesQuotation.CurrencyId, SalesQuotation.CurrencyEquivalent, SalesQuotation.Total, SalesQuotation.TotalItemsDiscount, SalesQuotation.SalesTaxes, SalesQuotation.TotalAfterTaxes, SalesQuotation.VoucherDiscountValue, SalesQuotation.VoucherDiscountPercentage, SalesQuotation.NetTotal, SalesQuotation.Paid, SalesQuotation.ValidityPeriod, SalesQuotation.DeliveryPeriod, SalesQuotation.CostPriceId, SalesQuotation.CurrentQuantity, SalesQuotation.DestinationWarehouseId, SalesQuotation.SystemPageId, SalesQuotation.SelectedId, SalesQuotation.TotalCostPrice, SalesQuotation.TotalItemDirectExpenses, SalesQuotation.IsDelivered, SalesQuotation.IsAccepted, SalesQuotation.IsLinked, SalesQuotation.IsCompleted, SalesQuotation.IsPosted, SalesQuotation.UserId, SalesQuotation.IsActive, SalesQuotation.IsDeleted, SalesQuotation.AutoCreated,SalesQuotation.CostPriceChecked, SalesQuotation.Notes, SalesQuotation.Image, SalesQuotation.UpdatedId, SalesQuotationDetails, SalesQuotation.CustomerRepId);

                        Notification.GetNotification("SalesQuotation", "Edit", "AddEdit", id, null, "عرض سعر بيع");

                        ////-------------------- Notification-------------------------////

                        //int pageid = db.Get_PageId("SalesQuotation").SingleOrDefault().Value;
                        //var actionId = db.PageActions.FirstOrDefault(c => c.Action == "AddEdit" && c.EnName == "Edit" && c.PageId == pageid).Id;
                        //int userid = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                        //var UserName = User.Identity.Name;
                        //db.Sp_OccuredNotification(actionId, $"بتعديل بيانات في شاشة عرض سعر بيع  {UserName}قام المستخدم ");
                        ////////////////-----------------------------------------------------------------------

                    }
                    else
                    {
                        if(AutomaticallyApprovingSalesQuotation==true)
                        {
                            SalesQuotation.IsActive = true;
                        }
                        else
                        {
                            SalesQuotation.IsActive = false;
                        }
                        MyXML.xPathName = "Details";
                        var SalesQuotationDetails = MyXML.GetXML(SalesQuotation.SalesQuotationDetails);


                        db.SalesQuotation_Insert(SalesQuotation.BranchId, SalesQuotation.WarehouseId, SalesQuotation.DepartmentId, SalesQuotation.VoucherDate, SalesQuotation.VendorOrCustomerId , SalesQuotation.CurrencyId, SalesQuotation.CurrencyEquivalent, SalesQuotation.Total, SalesQuotation.TotalItemsDiscount, SalesQuotation.SalesTaxes, SalesQuotation.TotalAfterTaxes, SalesQuotation.VoucherDiscountValue, SalesQuotation.VoucherDiscountPercentage, SalesQuotation.NetTotal, SalesQuotation.Paid, SalesQuotation.ValidityPeriod, SalesQuotation.DeliveryPeriod, SalesQuotation.CostPriceId, SalesQuotation.CurrentQuantity, SalesQuotation.DestinationWarehouseId, SalesQuotation.SystemPageId, SalesQuotation.SelectedId, SalesQuotation.TotalCostPrice, SalesQuotation.TotalItemDirectExpenses, SalesQuotation.IsDelivered, SalesQuotation.IsAccepted, SalesQuotation.IsLinked, SalesQuotation.IsCompleted, false, SalesQuotation.UserId, SalesQuotation.IsActive, SalesQuotation.IsDeleted, SalesQuotation.AutoCreated, SalesQuotation.CostPriceChecked, SalesQuotation.Notes, SalesQuotation.Image, SalesQuotation.UpdatedId, SalesQuotationDetails , SalesQuotation.CustomerRepId);

                        ////-------------------- Notification-------------------------////
                        Notification.GetNotification("SalesQuotation", "Add", "AddEdit", id, null, "عرض سعر بيع");

                        //int pageid = db.Get_PageId("SalesQuotation").SingleOrDefault().Value;
                        //var actionId = db.PageActions.FirstOrDefault(c => c.Action == "AddEdit" && c.EnName == "Add" && c.PageId == pageid).Id;
                        //int userid = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                        //var UserName = User.Identity.Name;
                        //db.Sp_OccuredNotification(actionId, $"باضافة بيانات في شاشة عرض سعر بيع  {UserName}قام المستخدم  ");

                        ////////////////-----------------------------------------------------------------------
                    }
                    QueryHelper.AddLog(new MyLog()
                    {
                        ArAction = id > 0 ? "تعديل   عرض سعر بيع " : "اضافة   عرض سعر بيع",
                        EnAction = "AddEdit",
                        ControllerName = "SalesQuotation",
                        UserName = User.Identity.Name,
                        UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                        LogDate = DateTime.Now,
                        RequestMethod = "POST",
                        SelectedItem = SalesQuotation.Id > 0 ? SalesQuotation.Id : db.SalesQuotations.Max(i => i.Id),
                        CodeOrDocNo = SalesQuotation.DocumentNumber
                    });
                    return Json(new { success = "true", id });
                }
                var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .Select(x => new { x.Key, x.Value.Errors })
                        .ToArray();

                return Json(new { success = "false" });

            }
            catch (Exception ex)
            {

                return Json(new { success = "false" });
            }

        }

        private void FillVehicleLookupData(string session)
        {
            ViewBag.CarTypesJson = JsonConvert.SerializeObject(db.CarTypes.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
            {
                b.Id,
                ArName = session == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
            }).ToList());
            ViewBag.CarModelsJson = JsonConvert.SerializeObject(db.CarModels.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
            {
                b.Id,
                b.CarTypeId,
                ArName = session == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
            }).ToList());
            ViewBag.CarColorsJson = JsonConvert.SerializeObject(db.CarColors.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
            {
                b.Id,
                ArName = session == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
            }).ToList());
        }
        private string ValidateVehicleChassisNumbers(ICollection<SalesQuotationDetail> details, int quotationId)
        {
            if (details == null) return null;
            var normalizedInQuotation = new HashSet<string>();
            foreach (var detail in details)
            {
                var hasVehicleData = detail.CarTypeId.HasValue || detail.CarModelId.HasValue || detail.CarColorId.HasValue || !string.IsNullOrWhiteSpace(detail.EngineNo) || detail.ManufacturingYear.HasValue || !string.IsNullOrWhiteSpace(detail.PlateNo);
                var chassis = (detail.ChassisNo ?? "").Trim();
                if (hasVehicleData && string.IsNullOrWhiteSpace(chassis))
                    return "رقم الشاسيه مطلوب عند إدخال بيانات سيارة.";
                if (!string.IsNullOrWhiteSpace(chassis))
                {
                    var normalized = Regex.Replace(chassis.ToLower(), "\\s+", "");
                    if (normalizedInQuotation.Contains(normalized))
                        return "رقم الشاسيه مكرر داخل نفس عرض السعر.";
                    normalizedInQuotation.Add(normalized);
                    var existsInQuotation = db.SalesQuotationDetails.Where(x => !x.IsDeleted && x.MainDocId != quotationId && x.ChassisNo != null)
                        .Select(x => x.ChassisNo).ToList().Any(x => Regex.Replace(x.ToLower(), "\\s+", "") == normalized);
                    if (existsInQuotation)
                        return "رقم الشاسيه مستخدم مسبقاً في عرض سعر آخر.";
                }
            }
            return null;
        }

        // POST: SalesQuotation/Delete/5
        [HttpPost, ActionName("Delete")]

        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                SalesQuotation SalesQuotation = db.SalesQuotations.Find(id);
                if (SalesQuotation.IsPosted == true)
                {
                    return Content("false");
                }
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.SalesQuotation_Delete(id, userId);
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = " حذف عرض سعر بيع",
                    EnAction = "AddEdit",
                    ControllerName = "SalesQuotation",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = SalesQuotation.DocumentNumber
                });

                ////-------------------- Notification-------------------------////
                Notification.GetNotification("SalesQuotation", "Delete", "Delete", id, null, "عرض سعر بيع");

                //int pageid = db.Get_PageId("SalesQuotation").SingleOrDefault().Value;
                //var actionId = db.PageActions.FirstOrDefault(c => c.Action == "Delete" && c.EnName == "Delete" && c.PageId == pageid).Id;
                //int userid = db.UserNotifications.FirstOrDefault(c => c.ActionId == actionId && c.PageId == pageid).UserId.Value;
                //var UserName = User.Identity.Name;
                //db.Sp_OccuredNotification(actionId, $"بحذف بيانات في شاشة عرض سعر بيع  {UserName}قام المستخدم  ");
                ////////////////-----------------------------------------------------------------------

                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }
        // get last price of item occur in invoices
        [SkipERPAuthorize]
        public JsonResult SetLastPrice(int itemId)
        {
            var lastPrice = db.GetLastItemPrice(itemId).FirstOrDefault();
            return Json(lastPrice, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult SetBankAccounts(int bankId)
        {
            var BankAccountsList = db.GetBankAccountByBankId(bankId).ToList();
            return Json(BankAccountsList, JsonRequestBehavior.AllowGet);
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
            var lastObj = db.SalesQuotations.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.SalesQuotations.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Value.Month == VoucherDate.Value.Month && a.VoucherDate.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.SalesQuotations.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

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
            //var docNo = QueryHelper.DocLastNum(id, "SalesQuotation");
            //double i = (docNo) + 1;
            //return Json(i, JsonRequestBehavior.AllowGet);
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

        [SkipERPAuthorize]
        public JsonResult GetChangeItemPricePrivilege()
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            return Json(db.UserPrivileges.FirstOrDefault(u => u.PageAction.EnName == "ChangeItemPrice" && u.PageAction.Action == "ChangeItemPrice" && u.UserId == userId).Privileged, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult GetCustomerDebitBalance(int id)
        {
            return Json(db.Customer_DebitBalance(id), JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult ActivateDeactivate(int id)
        {
            SalesQuotation salesQuotation = db.SalesQuotations.Find(id);
            
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

            if (salesQuotation.IsActive != true)
            {
                salesQuotation.IsActive= true;
            }
            else
            {
                salesQuotation.IsActive = false;
            }

            db.Entry(salesQuotation).State = EntityState.Modified;

            db.SaveChanges();
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = (bool)salesQuotation.IsActive ? "اعتماد عرض سعر بيع" : "إلغاء اعتماد عرض سعر بيع",
                EnAction = "AddEdit",
                ControllerName = "SalesQuotation",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "POST",
                SelectedItem = salesQuotation.Id,
                CodeOrDocNo = salesQuotation.DocumentNumber
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("SalesQuotation", "Activate/Deactivate", "ActivateDeactivate", id, true, "عرض سعر بيع");
            return Json(salesQuotation.IsActive == true ? "approved" : "notApproved");
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
