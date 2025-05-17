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

namespace MyERP.Controllers
{
    public class PosReceiptVoucherController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();


        public async Task<ActionResult> Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "", int departmentId = 0)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            var depIds = await departmentRepository.UserDepartmentsIds(userId);
            SystemSetting systemSetting = await db.SystemSettings.AnyAsync() ? await db.SystemSettings.FirstOrDefaultAsync() : new SystemSetting();

            ////-------------------- Notification-------------------------////
            Notification.GetNotification("PosReceiptVoucher", "View", "Index", null, null, "سند قبض نقاط البيع");

            if (userId == 1)
            {
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", departmentId);

            }
            else
            {
                ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new {
                    Id = b.DepartmentId,
                    ArName = b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", departmentId);

            }
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            Repository<PosReceiptVoucher> repository = new Repository<PosReceiptVoucher>(db);
            IQueryable<PosReceiptVoucher> PosReceiptVoucher;

            if (string.IsNullOrEmpty(searchWord))
            {
                PosReceiptVoucher = repository.GetAll().Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (departmentId == 0 || s.DepartmentId == departmentId)).Include(s => s.Department).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = await repository.GetAll().Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (departmentId == 0 || s.DepartmentId == departmentId)).CountAsync();
            }
            else
            {
                PosReceiptVoucher = repository.GetAll().Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (s.DocumentNumber.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Date.ToString().Contains(searchWord)) && (departmentId == 0 || s.DepartmentId == departmentId)).Include(s => s.Department).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = await repository.GetAll().Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (s.DocumentNumber.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Date.ToString().Contains(searchWord)) && (departmentId == 0 || s.DepartmentId == departmentId)).CountAsync();
            }

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة سند قبض نفاط البيع",
                EnAction = "Index",
                ControllerName = "PosReceiptVoucher",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            return View(await PosReceiptVoucher.ToListAsync());
        }


        public async Task<ActionResult> AddEdit(int? id)
        {
            SystemSetting systemSetting = await db.SystemSettings.AnyAsync() ? await db.SystemSettings.FirstOrDefaultAsync() : new SystemSetting();

            UserRepository userRepository = new UserRepository(db);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            CashboxReposistory cashboxReposistory = new CashboxReposistory(db);
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            if (id == null)
            {
                
                ViewBag.CashierUserId = new SelectList(db.ERPUsers.Where(c => c.IsDeleted == false && c.IsActive == true && c.IsCashier == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.UserName
                }), "Id", "ArName");

                ViewBag.DepartmentId = new SelectList(await departmentRepository.UserDepartments(userId).ToListAsync(), "Id", "ArName", systemSetting.DefaultDepartmentId);
                ViewBag.ShiftId = new SelectList(db.Shifts.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.ArName
                }), "Id", "ArName");

                ViewBag.AccountantCashBoxId = new SelectList(await cashboxReposistory.UserCashboxes(userId, systemSetting.DefaultDepartmentId).ToListAsync(), "Id", "ArName", systemSetting.DefaultCashBoxId);
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
                ViewBag.DateFrom = cTime.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.DateTo = cTime.ToString("yyyy-MM-ddTHH:mm");
                return View();
            }
            PosReceiptVoucher PosReceiptVoucher = null;

            
                PosReceiptVoucher = await db.PosReceiptVouchers.FindAsync(id);
                if (PosReceiptVoucher == null)
                    return HttpNotFound();

                ViewBag.Next = QueryHelper.Next((int)id, "PosReceiptVoucher");
                ViewBag.Previous = QueryHelper.Previous((int)id, "PosReceiptVoucher");
                ViewBag.Last = QueryHelper.GetLast("PosReceiptVoucher");
                ViewBag.First = QueryHelper.GetFirst("PosReceiptVoucher");

                int sysPageId = QueryHelper.SourcePageId("PosReceiptVoucher");
                int cashTransferPageId = QueryHelper.SourcePageId("CashTransfer");
            JournalEntry journal = db.JournalEntries.FirstOrDefault(j => j.SourceId == id && j.SourcePageId == sysPageId);
                ViewBag.Journal = journal;
            ViewBag.Journal.DocumentNumber = journal != null ? journal.DocumentNumber.Replace("-", "") : "";

            var cashTransferIds = QueryHelper.CashTransferJournalIds(id, cashTransferPageId);
            var CashJournal = db.JournalEntryDetails.Where(j => j.SourcePageId == cashTransferPageId && cashTransferIds.Contains(j.SourceId));
            ViewBag.CashTransferJournal=CashJournal;

            ViewBag.CashierUserId = new SelectList(db.ERPUsers.Where(c => c.IsDeleted == false && c.IsActive == true && c.IsCashier == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.UserName
            }), "Id", "ArName", PosReceiptVoucher.CashierUserId);
            ViewBag.AccountantCashBoxId = new SelectList(await cashboxReposistory.UserCashboxes(userId, PosReceiptVoucher.DepartmentId).ToListAsync(), "Id", "ArName", PosReceiptVoucher.AccountantCashBoxId);

            ViewBag.DepartmentId = new SelectList(await departmentRepository.UserDepartments(userId).ToListAsync(), "Id", "ArName", PosReceiptVoucher.DepartmentId);
            ViewBag.ShiftId = new SelectList(db.Shifts.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.ArName
            }), "Id", "ArName",PosReceiptVoucher.ShiftId);

            ViewBag.Date = PosReceiptVoucher.Date.ToString("yyyy-MM-ddTHH:mm");
            ViewBag.DateFrom = PosReceiptVoucher.DateFrom.ToString("yyyy-MM-ddTHH:mm");
            ViewBag.DateTo = PosReceiptVoucher.DateTo.ToString("yyyy-MM-ddTHH:mm");

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل سند قبض نقاط البيع",
                EnAction = "AddEdit",
                ControllerName = "PosReceiptVoucher",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = PosReceiptVoucher.Id,
                CodeOrDocNo = PosReceiptVoucher.DocumentNumber
            });
            return View(PosReceiptVoucher);
        }


        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public JsonResult AddEdit(PosReceiptVoucher PosReceiptVoucher, string posManagerPassword)
        {
            int userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            PosReceiptVoucher.UserId = userId;
            if (ModelState.IsValid)
            {
                    var id = PosReceiptVoucher.Id;
                    PosReceiptVoucher.IsDeleted = false;
                    if (PosReceiptVoucher.Id > 0)
                    {
                        if (db.PosReceiptVouchers.Find(PosReceiptVoucher.Id).IsPosted == true)
                        {
                            return Json(new { success = "false" });
                        }
                        MyXML.xPathName = "Details";
                        var PosReceiptVoucherDetails = MyXML.GetXML(PosReceiptVoucher.PosReceiptVoucherDetails);
                    MyXML.xPathName = "CustomerTypes";
                    var PosReceiptVoucherCustomerTypes = MyXML.GetXML(PosReceiptVoucher.PosReceiptVoucherCustomerTypes);
                    db.PosReceiptVoucher_Update(PosReceiptVoucher.Id, PosReceiptVoucher.BranchId, PosReceiptVoucher.DepartmentId, PosReceiptVoucher.Date, PosReceiptVoucher.ShiftId, PosReceiptVoucher.CashierUserId
                           ,PosReceiptVoucher.DateFrom,PosReceiptVoucher.DateTo ,PosReceiptVoucher.AccountantCashBoxId, PosReceiptVoucher.IsLinked, PosReceiptVoucher.IsPosted, PosReceiptVoucher.UserId, PosReceiptVoucher.IsActive, PosReceiptVoucher.IsDeleted, PosReceiptVoucher.Notes, PosReceiptVoucher.Image, PosReceiptVoucherDetails, PosReceiptVoucherCustomerTypes);
                        Notification.GetNotification("PosReceiptVoucher", "Edit", "AddEdit", id, null, "سند قبض نقاط البيع");
                    }
                    else
                    {
                        PosReceiptVoucher.IsActive = true;
                        MyXML.xPathName = "Details";
                        var PosReceiptVoucherDetails = MyXML.GetXML(PosReceiptVoucher.PosReceiptVoucherDetails);
                    MyXML.xPathName = "CustomerTypes";
                    var PosReceiptVoucherCustomerTypes = MyXML.GetXML(PosReceiptVoucher.PosReceiptVoucherCustomerTypes);
                    var idResult = new ObjectParameter("Id", typeof(Int32));
                        db.PosReceiptVoucher_Insert(idResult, PosReceiptVoucher.BranchId, PosReceiptVoucher.DepartmentId, PosReceiptVoucher.Date, PosReceiptVoucher.ShiftId, PosReceiptVoucher.CashierUserId
                           , PosReceiptVoucher.DateFrom, PosReceiptVoucher.DateTo, PosReceiptVoucher.AccountantCashBoxId, PosReceiptVoucher.IsLinked, PosReceiptVoucher.IsPosted, PosReceiptVoucher.UserId, PosReceiptVoucher.IsActive, PosReceiptVoucher.IsDeleted, PosReceiptVoucher.Notes, PosReceiptVoucher.Image, PosReceiptVoucherDetails, PosReceiptVoucherCustomerTypes);
                        id = (int)idResult.Value;
                        ////-------------------- Notification-------------------------////
                        Notification.GetNotification("PosReceiptVoucher", "Add", "AddEdit", id, null, "سند قبض نقاط البيع");
                    }
                    QueryHelper.AddLog(new MyLog()
                    {
                        ArAction = PosReceiptVoucher.Id > 0 ? "تعديل  سند قبض نقطة بيع " : " سند قبض نقطة بيع",
                        EnAction = "AddEdit",
                        ControllerName = "PosReceiptVoucher",
                        UserName = User.Identity.Name,
                        UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                        LogDate = DateTime.Now,
                        RequestMethod = "POST",
                        SelectedItem = PosReceiptVoucher.Id > 0 ? PosReceiptVoucher.Id : db.PosReceiptVouchers.Max(i => i.Id),
                        CodeOrDocNo = PosReceiptVoucher.DocumentNumber
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
        public ActionResult PosSalesAnalysis()
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            int posId = int.Parse(Session["PosId"].ToString());
            return PartialView(db.Pos_GetTotalSalesAndReturn(posId, userId, null, null, false,false,null,null));//pos manager receive money from cashier not depend on certain department
        }


        public JsonResult GetDetails(DateTime DateFrom, DateTime DateTo, int? cashierUserId,int?DepartmentId,int?ShiftId)
        {
            return Json(db.Pos_GetTotalSalesAndReturn(null, cashierUserId,DateFrom, DateTo, true,false,DepartmentId,ShiftId), JsonRequestBehavior.AllowGet);
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
            var lastObj = db.PosReceiptVouchers.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.PosReceiptVouchers.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.Date.Month == VoucherDate.Value.Month && a.Date.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.PosReceiptVouchers.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.Date.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

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
            //var docNo = QueryHelper.DocLastNum(id, "PosReceiptVoucher");
            //double i = (docNo) + 1;
            //return Json(i, JsonRequestBehavior.AllowGet);
        }
    }
}