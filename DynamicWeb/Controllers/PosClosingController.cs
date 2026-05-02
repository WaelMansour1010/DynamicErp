using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using MyERP.Repository;
using System.Security.Claims;
using System.Data.Entity.Core.Objects;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MyERP.Controllers
{
    public class PosClosingController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        public async Task<ActionResult> Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "", int departmentId = 0)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            var depIds = await departmentRepository.UserDepartmentsIds(userId);
            SystemSetting systemSetting = await db.SystemSettings.AnyAsync() ? await db.SystemSettings.FirstOrDefaultAsync() : new SystemSetting();

            ////-------------------- Notification-------------------------////
            Notification.GetNotification("PosClosing", "View", "Index", null, null, "إغلاق نقاط البيع");

            if (userId == 1)
            {
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", departmentId);

            }
            else
            {
                ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", departmentId);

            }
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            Repository<PosClosing> repository = new Repository<PosClosing>(db);
            IQueryable<PosClosing> posClosing;

            if (string.IsNullOrEmpty(searchWord))
            {
                posClosing = repository.GetAll().Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (departmentId == 0 || s.DepartmentId == departmentId)).Include(s => s.Department).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = await repository.GetAll().Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (departmentId == 0 || s.DepartmentId == departmentId)).CountAsync();
            }
            else
            {
                posClosing = repository.GetAll().Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (s.DocumentNumber.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Date.ToString().Contains(searchWord)) && (departmentId == 0 || s.DepartmentId == departmentId)).Include(s => s.Department).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = await repository.GetAll().Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (s.DocumentNumber.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Date.ToString().Contains(searchWord)) && (departmentId == 0 || s.DepartmentId == departmentId)).CountAsync();
            }

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة إغلاق نفاط البيع",
                EnAction = "Index",
                ControllerName = "PosClosing",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            return View(await posClosing.ToListAsync());
        }

        public async Task<ActionResult> AddEdit(int? id)
        {
            SystemSetting systemSetting = await db.SystemSettings.AnyAsync() ? await db.SystemSettings.FirstOrDefaultAsync() : new SystemSetting();
            UserRepository userRepository = new UserRepository(db);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            var custodyBoxId = db.ERPUsers.Where(u => u.Id == userId).FirstOrDefault().CustodyBoxId;
            var UsercashBox = db.UserCashBoxes.Where(c => c.UserId == userId).FirstOrDefault();
            var cashBoxId = UsercashBox != null ? UsercashBox.CashBoxId : null;
            bool? IsCashier = db.ERPUsers.Where(e => e.Id == userId).FirstOrDefault().IsCashier;
            ViewBag.IsCashier = IsCashier;

            if (id == null)
            {
                int posId = 0;
                int departmnetId = 0;
                int? posManagerId = 0;
                if (IsCashier == true)
                {
                    Session["IsCashier"] = true;
                    var pos = db.Pos.Where(p => p.CurrentCashierUserId == userId && p.PosStatusId == 2).FirstOrDefault();
                    if (pos == null)
                        return RedirectToAction("PosLogin", "PointOfSale");
                    else
                    {
                        posId = pos.Id;
                        Session["PosId"] = pos.Id;
                        departmnetId = pos.DepartmentId;
                        posManagerId = pos.PosManagerId;
                    }
                }
                ViewBag.CashierUserId = new SelectList(db.ERPUsers.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.UserName
                }), "Id", "ArName", userId);

                ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", departmnetId);

                ViewBag.PosId = new SelectList(db.Pos.Where(p => p.IsActive == true && p.IsDeleted == false && p.PosStatusId == 2).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", posId);
                ViewBag.PosManagerId = new SelectList(db.Employees.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", posManagerId);

                var totalSales = db.SalesInvoices.Where(s => s.PosId == posId && s.CashierUserId == userId && s.IsCollected == false && s.IsDeleted == false && s.IsClosed == false).Sum(s => s.TotalAfterTaxes);
                var totalDiscount = db.SalesInvoices.Where(s => s.PosId == posId && s.CashierUserId == userId && s.IsCollected == false && s.IsDeleted == false && s.IsClosed == false).Sum(s => s.VoucherDiscountValue);
                var totalReturn = db.SalesReturns.Where(s => s.PosId == posId && s.CashierUserId == userId && s.IsCollected == false && s.IsDeleted == false && s.IsClosed == false).Sum(s => s.TotalAfterTaxes);
                var cashIssue = db.CashIssueVouchers.Where(s => s.PosId == posId && s.CashierUserId == userId && s.IsCollected == false && s.IsDeleted == false && s.IsActive == true && s.IsClosed == false).Sum(s => (decimal?)s.MoneyAmount);
                //var totalReservations = db.SalesOrders.Where(s => s.PosId == posId && s.CashierUserId == userId && s.IsCollected == false && s.IsDeleted == false && s.IsClosed == false).Sum(s => s.TotalAfterTaxes);
                var totalReservations = db.SalesOrderPaymentMethods.Where(s => s.SalesOrder.PosId == posId && s.SalesOrder.CashierUserId == userId && s.SalesOrder.IsCollected == false && s.SalesOrder.IsDeleted == false && s.SalesOrder.IsClosed == false).Sum(s => s.Amount);

                ViewBag.CustodyAmount = 0;
                if (custodyBoxId != null)
                    ViewBag.CustodyAmount = db.CashBox_Balances(custodyBoxId).FirstOrDefault().Balance;

                if (custodyBoxId == cashBoxId)
                    ViewBag.SameCashBox = true;
                totalSales = totalSales != null ? totalSales : 0;
                totalDiscount = totalDiscount != null ? totalDiscount : 0;
                totalReturn = totalReturn != null ? totalReturn : 0;
                cashIssue = cashIssue != null ? cashIssue : 0;
                totalReservations = totalReservations != null ? totalReservations : 0;
                var netSales = totalSales - totalReturn - cashIssue;
                netSales = netSales != null ? netSales : 0;
                ViewBag.TotalSales = totalSales;
                ViewBag.TotalDiscount = totalDiscount;
                ViewBag.TotalReturn = totalReturn;
                ViewBag.TotalCashIssue = cashIssue;
                ViewBag.NetSales = netSales;
                ViewBag.TotalReservations = totalReservations;

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

                ViewBag.Date = cTime.ToString("yyyy-MM-ddTHH:mm");
                return View();
            }
            PosClosing posClosing = null;

            {
                posClosing = await db.PosClosings.FindAsync(id);
                if (posClosing == null)
                    return HttpNotFound();


                ViewBag.CustodyAmount = posClosing.CustodyAmount;

                ViewBag.TotalSales = posClosing.TotalSales;
                ViewBag.TotalCashIssue = posClosing.TotalCashIssueVoucher;
                ViewBag.TotalDiscount = posClosing.TotalDiscount;
                ViewBag.TotalReturn = posClosing.TotalReturn;
                ViewBag.NetSales = posClosing.NetSales;
                ViewBag.TotalReservations = posClosing.TotalReservations;
                ViewBag.Next = QueryHelper.Next((int)id, "PosClosing");
                ViewBag.Previous = QueryHelper.Previous((int)id, "PosClosing");
                ViewBag.Last = QueryHelper.GetLast("PosClosing");
                ViewBag.First = QueryHelper.GetFirst("PosClosing");

                int sysPageId = QueryHelper.SourcePageId("PosClosing");
            }


            ViewBag.PosId = new SelectList(db.Pos.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", posClosing.PosId);
            ViewBag.PosManagerId = new SelectList(db.Employees.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", posClosing.PosManagerId);
            ViewBag.CashierUserId = new SelectList(db.ERPUsers.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.UserName
            }), "Id", "ArName", posClosing.CashierUserId);
            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", posClosing.DepartmentId);

            ViewBag.Date = posClosing.Date.ToString("yyyy-MM-ddTHH:mm");

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل إغلاق نقاط البيع",
                EnAction = "AddEdit",
                ControllerName = "PosClosing",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = posClosing.Id,
                CodeOrDocNo = posClosing.DocumentNumber
            });
            return View(posClosing);
        }


        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public JsonResult AddEdit(PosClosing posClosing, string posManagerPassword)
        {
            int userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            posClosing.UserId = userId;
            var user = db.ERPUsers.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == userId).FirstOrDefault();

            if (ModelState.IsValid)
            {
                var managerUserName = db.ERPUsers.Where(u => u.EmployeeId == posClosing.PosManagerId).FirstOrDefault().UserName;
                var custodyBox = db.ERPUsers.Where(u => u.Id == userId).FirstOrDefault().CustodyBoxId;

                ObjectParameter HashPW = new ObjectParameter("HashPW", typeof(string));
                db.ERPUser_GetHashPw(managerUserName, HashPW);
                string strHashPw = HashPW.Value.ToString();
                bool authenticated = PasswordEncrypt.VerifyHashPwd(posManagerPassword, strHashPw);
                if (!authenticated)
                {
                    return Json(new { success = "managerPasswordIncorrect" });
                }
                else
                {
                    var id = posClosing.Id;
                    posClosing.IsDeleted = false;
                    if (posClosing.Id > 0)
                    {
                        if (db.PosClosings.Find(posClosing.Id).IsPosted == true)
                        {
                            return Json(new { success = "false" });
                        }
                        MyXML.xPathName = "Details";
                        var PosClosingDetails = MyXML.GetXML(posClosing.PosClosingDetails);
                        MyXML.xPathName = "CurrencyCategory";
                        var PosClosingCurrencyCategories = MyXML.GetXML(posClosing.PosClosingCurrencyCategories);

                        db.PosClosing_Update(posClosing.Id, posClosing.BranchId, posClosing.DepartmentId, posClosing.Date, posClosing.ShiftId, posClosing.PosId, posClosing.CashierUserId
                            , posClosing.PosManagerId, posClosing.TotalSales, posClosing.TotalCashIssueVoucher, posClosing.TotalDiscount, posClosing.TotalReturn, posClosing.NetSales, posClosing.CustodyAmount, posClosing.CustodyAmountDelivered, posClosing.NetCashAmount, posClosing.NetCashAmountDelivered, posClosing.IsApproved, posClosing.ApprovalDate, posClosing.IsLinked, posClosing.IsPosted, posClosing.UserId, posClosing.IsActive, posClosing.IsDeleted, posClosing.Notes, posClosing.Image, PosClosingDetails, PosClosingCurrencyCategories, posClosing.TotalReservations, posClosing.ReservationCashAmount, posClosing.ReservationCashAmountDelivered);
                        Notification.GetNotification("PosClosing", "Edit", "AddEdit", id, null, "إغلاق نقاط البيع");
                    }
                    else
                    {
                        posClosing.IsActive = true;

                        MyXML.xPathName = "Details";
                        var PosClosingDetails = MyXML.GetXML(posClosing.PosClosingDetails);
                        MyXML.xPathName = "CurrencyCategory";
                        var PosClosingCurrencyCategories = MyXML.GetXML(posClosing.PosClosingCurrencyCategories);

                        var idResult = new ObjectParameter("Id", typeof(Int32));
                        db.PosClosing_Insert(idResult, posClosing.BranchId, posClosing.DepartmentId, posClosing.Date, posClosing.ShiftId, posClosing.PosId, userId
                            , posClosing.PosManagerId, posClosing.TotalSales, posClosing.TotalCashIssueVoucher, posClosing.TotalDiscount, posClosing.TotalReturn, posClosing.NetSales, posClosing.CustodyAmount, posClosing.CustodyAmountDelivered, posClosing.NetCashAmount, posClosing.NetCashAmountDelivered, posClosing.IsApproved, posClosing.ApprovalDate, posClosing.IsLinked, posClosing.IsPosted, posClosing.UserId, posClosing.IsActive, posClosing.IsDeleted, posClosing.Notes, posClosing.Image, PosClosingDetails, PosClosingCurrencyCategories, posClosing.TotalReservations, posClosing.ReservationCashAmount, posClosing.ReservationCashAmountDelivered);
                        id = (int)idResult.Value;

                        ////-------------------- Notification-------------------------////
                        Notification.GetNotification("PosClosing", "Add", "AddEdit", id, null, "إغلاق نقاط البيع");
                    }
                    QueryHelper.AddLog(new MyLog()
                    {
                        ArAction = posClosing.Id > 0 ? "تعديل  إغلاق نقطة بيع " : " إغلاق نقطة بيع",
                        EnAction = "AddEdit",
                        ControllerName = "PosClosing",
                        UserName = User.Identity.Name,
                        UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                        LogDate = DateTime.Now,
                        RequestMethod = "POST",
                        SelectedItem = posClosing.Id > 0 ? posClosing.Id : db.PosClosings.Max(i => i.Id),
                        CodeOrDocNo = posClosing.DocumentNumber
                    });

                    return Json(new { success = "true", id });
                }
            }
            var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();

            return Json(new { success = "false", errors });
        }

        [SkipERPAuthorize]
        public ActionResult PosSalesAnalysis()
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            var PosSession = Session["PosId"] != null ? Session["PosId"].ToString() : null;
            int posId = PosSession != null ? int.Parse(PosSession) : 0;
            return PartialView(db.Pos_GetTotalSalesAndReturn(posId, userId, null, null, false, false, null, null));//pos manager receive money from cashier not depend on certain department
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
            var lastObj = db.PosClosings.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.PosClosings.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.Date.Month == VoucherDate.Value.Month && a.Date.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.PosClosings.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.Date.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

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
            //var docNo = QueryHelper.DocLastNum(id, "PosClosing");
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
