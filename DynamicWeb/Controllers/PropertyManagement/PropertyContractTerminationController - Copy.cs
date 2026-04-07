using MyERP.Models;
using MyERP.Repository;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Web.Mvc;
using MyERP.Utils;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

namespace MyERP.Controllers.PropertyManagement
{
    public class PropertyContractTerminationController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: PropertyContractTermination
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة تصفية العقد",
                EnAction = "Index",
                ControllerName = "PropertyContractTermination",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("PropertyContractTermination", "View", "Index", null, null, "تصفية العقد");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<PropertyContractTermination> propertyContractTermination;

            if (string.IsNullOrEmpty(searchWord))
            {
                propertyContractTermination = db.PropertyContractTerminations.Where(s => s.IsDeleted == false).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.PropertyContractTerminations.Where(s => s.IsDeleted == false).Count();
            }
            else
            {
                propertyContractTermination = db.PropertyContractTerminations.Where(s => s.IsDeleted == false && (s.DocumentNumber.Contains(searchWord))).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.PropertyContractTerminations.Where(s => s.IsDeleted == false && (s.DocumentNumber.Contains(searchWord))).Count();
            }

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة تصفية العقد",
                EnAction = "Index",
                ControllerName = "PropertyContractTermination",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            return View(propertyContractTermination.ToList());
        }
        public ActionResult AddEdit(int? id)
        {
            SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
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
            DepartmentRepository departmentRepository = new DepartmentRepository(db);

            ViewBag.PropertyComponentId = new SelectList(db.PropertyComponents.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");

            if (id == null)
            {
                ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", systemSetting.DefaultDepartmentId);
                ViewBag.VoucherDate = cTime.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.TerminationDate = cTime.ToString("yyyy-MM-dd");
                ViewBag.LastBatchDate = cTime.ToString("yyyy-MM-dd");
                return View();
            }
            PropertyContractTermination contractTermination = db.PropertyContractTerminations.Find(id);

            if (contractTermination == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل تصفية العقد ",
                EnAction = "AddEdit",
                ControllerName = "PropertyContractTermination",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });

            ViewBag.Next = QueryHelper.Next((int)id, "PropertyContractTermination");
            ViewBag.Previous = QueryHelper.Previous((int)id, "PropertyContractTermination");
            ViewBag.Last = QueryHelper.GetLast("PropertyContractTermination");
            ViewBag.First = QueryHelper.GetFirst("PropertyContractTermination");

            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", contractTermination.DepartmentId);
            ViewBag.VoucherDate = contractTermination.VoucherDate != null ? contractTermination.VoucherDate.ToString("yyyy-MM-ddTHH:mm") : null;
            ViewBag.TerminationDate = contractTermination.TerminationDate != null ? contractTermination.TerminationDate.Value.ToString("yyyy-MM-dd") : null;
            ViewBag.LastBatchDate = contractTermination.LastBatchDate != null ? contractTermination.LastBatchDate.Value.ToString("yyyy-MM-dd") : null;

            ViewBag.PropertyRenterId = new SelectList(db.PropertyRenters.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", contractTermination.PropertyRenterId);

            ViewBag.PropertyContractId = new SelectList(db.PropertyContracts.Where(a => a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.DocumentNumber
            }), "Id", "ArName", contractTermination.PropertyContractId);

            ViewBag.PropertyId = new SelectList(db.Properties.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", contractTermination.PropertyId);
            ViewBag.PropertyUnitTypeId = new SelectList(db.PropertyUnitTypes.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", contractTermination.PropertyUnitTypeId);
            
            //-------------------- journal Entry --------------------//
            int sysPageId = QueryHelper.SourcePageId("PropertyContractTermination");
            JournalEntry journal = db.JournalEntries.FirstOrDefault(j => j.SourceId == id && j.SourcePageId == sysPageId);
            ViewBag.Journal = journal;
            ViewBag.JournalDocumentNumber = journal != null && journal.DocumentNumber != null ? journal.DocumentNumber.Replace("-", "") : "";
            //----------------------------------------------------------------------------//
            var PropUnit = db.PropertyDetails.Where(a => a.Id == contractTermination.PropertyContract.PropertyUnitId).FirstOrDefault();

            ViewBag.UnitCode = PropUnit != null ? PropUnit.PropertyUnitNo : string.Empty; // تعيين قيمة فارغة إذا كانت PropUnit null

            var mergedUnitIds = contractTermination.PropertyContract.PropertyContractMergedUnit.Select(mu => mu.PropertyUnitId).ToList();

            var selectedMergedUnits = new MultiSelectList(db.PropertyDetails
               .Where(pd => mergedUnitIds.Contains(pd.Id))
               .Select(b => new
               {
                   Id = b.Id,
                   ArName = b.PropertyUnitNo
               }).ToList(), "Id", "ArName");
            ViewBag.selectedMergedUnits = (MultiSelectList)selectedMergedUnits.AsEnumerable();

            return View(contractTermination);
        }

        [HttpPost]
        public ActionResult AddEdit(PropertyContractTermination contractTermination)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            if (ModelState.IsValid)
            {
                var id = contractTermination.Id;
                contractTermination.IsDeleted = false;
                contractTermination.UserId = userId;
                //set unit status and merged units status to available
                var contract =
                    db.PropertyContracts.FirstOrDefault(t => t.Id == contractTermination.PropertyContractId);
                var unit = db.PropertyDetails.Where(t => t.MainDocId == contractTermination.PropertyId &&
                                                         t.Id ==
                                                         contract.PropertyUnitId).FirstOrDefault();
                unit.StatusId = PropertyDetailsStatus.Available;
                foreach (var item in contract.PropertyContractMergedUnit)
                {
                    var mergedunit = db.PropertyDetails.Where(t => t.MainDocId == contractTermination.PropertyId &&
                                                             t.Id ==
                                                             item.PropertyUnitId).FirstOrDefault();
                    mergedunit.StatusId = PropertyDetailsStatus.Available;
                }

                if (contractTermination.Id > 0)
                {
                    // ===== XML =====
                    MyXML.xPathName = "Details";
                    var detailsXml = MyXML.GetXML(contractTermination.PropertyContractTerminationDetails);

                    MyXML.xPathName = "Damages";
                    var damagesXml = MyXML.GetXML(contractTermination.PropertyContractTerminationDamages);

                    // ===== Update =====
                    db.PropertyContractTerminate_Update(
                        contractTermination.Id,
                        contractTermination.DocumentNumber,
                        contractTermination.DepartmentId,
                        contractTermination.VoucherDate,
                        contractTermination.PropertyContractId,
                        contractTermination.PropertyId,
                        contractTermination.PropertyUnitTypeId,
                        contractTermination.PropertyRenterId,
                        contractTermination.TerminationDate,
                        contractTermination.LastBatchDate,
                        contractTermination.IsLastBatchCalculation,
                        contractTermination.IsDocumented,
                        contractTermination.IncreaseDayValue,
                        contractTermination.IncreaseDaysNo,
                        contractTermination.IncreaseDaysTotalValue,
                        contractTermination.DecreaseDayValue,
                        contractTermination.DecreaseDaysNo,
                        contractTermination.DecreaseDaysTotalValue,
                        contractTermination.IsDeleted,
                        contractTermination.UserId,
                        contractTermination.Notes,
                        contractTermination.Image,

                        // ✅ الحقول الجديدة بالترتيب الجديد
                        contractTermination.TotalUnpaidAmount ?? 0m,
                        contractTermination.InsuranceAmount ?? 0m,
                        contractTermination.RenterBalance ?? 0m,
                        contractTermination.CalculationMethod ?? 0,

                        detailsXml,
                        damagesXml
                    );

                    db.SaveChanges();

                    Notification.GetNotification("PropertyContractTermination", "Edit", "AddEdit",
                        contractTermination.Id, null, "تصفية العقد");
                }
                else
                {
                    // ===== XML =====
                    MyXML.xPathName = "Details";
                    var detailsXml = MyXML.GetXML(contractTermination.PropertyContractTerminationDetails);

                    MyXML.xPathName = "Damages";
                    var damagesXml = MyXML.GetXML(contractTermination.PropertyContractTerminationDamages);

                    var idResult = new ObjectParameter("Id", typeof(int));

                    // ===== Insert =====
                    db.PropertyContractTerminate_Insert(
                        idResult,
                        contractTermination.DepartmentId,
                        contractTermination.VoucherDate,
                        contractTermination.PropertyContractId,
                        contractTermination.PropertyId,
                        contractTermination.PropertyUnitTypeId,
                        contractTermination.PropertyRenterId,
                        contractTermination.TerminationDate,
                        contractTermination.LastBatchDate,
                        contractTermination.IsLastBatchCalculation,
                        contractTermination.IsDocumented,
                        contractTermination.IncreaseDayValue,
                        contractTermination.IncreaseDaysNo,
                        contractTermination.IncreaseDaysTotalValue,
                        contractTermination.DecreaseDayValue,
                        contractTermination.DecreaseDaysNo,
                        contractTermination.DecreaseDaysTotalValue,
                        contractTermination.IsDeleted,
                        contractTermination.UserId,
                        contractTermination.Notes,
                        contractTermination.Image,

                        // ✅ الحقول الجديدة
                        contractTermination.TotalUnpaidAmount ?? 0m,
                        contractTermination.InsuranceAmount ?? 0m,
                        contractTermination.RenterBalance ?? 0m,
                        contractTermination.CalculationMethod ?? 0,

                        detailsXml,
                        damagesXml
                    );

                    db.SaveChanges();

                    contractTermination.Id = (int)idResult.Value;

                    Notification.GetNotification("PropertyContractTermination", "Add", "AddEdit",
                        contractTermination.Id, null, "تصفية العقد");
                }

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = contractTermination.Id > 0 ? "تعديل تصفية العقد" : "اضافة تصفية العقد",
                    EnAction = "AddEdit",
                    ControllerName = "PropertyContractTermination",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "Post",
                    SelectedItem = id,
                });
                return Json(new { success = true });
            }
            var errors = ModelState
                   .Where(x => x.Value.Errors.Count > 0)
                   .Select(x => new { x.Key, x.Value.Errors })
                   .ToArray();
            return Json(new { success = false });
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                PropertyContractTermination contractTermination = db.PropertyContractTerminations.Find(id);
                contractTermination.IsDeleted = true;
                contractTermination.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                foreach (var item in contractTermination.PropertyContractTerminationDetails)
                {
                    item.IsDeleted = true;
                }
                foreach (var item in contractTermination.PropertyContractTerminationDamages)
                {
                    item.IsDeleted = true;
                }
                db.Entry(contractTermination).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف تصفية العقد",
                    EnAction = "AddEdit",
                    ControllerName = "PropertyContractTermination",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                });
                Notification.GetNotification("PropertyContractTermination", "Delete", "Delete", id, null, "تصفية العقد");
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
            var lastObj = db.PropertyContractTerminations.Where(a => a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.PropertyContractTerminations.Where(a => a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Month == VoucherDate.Value.Month && a.VoucherDate.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.PropertyContractTerminations.Where(a => a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

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
        }
        [SkipERPAuthorize]

        public JsonResult GetPropertyContractTerminationDetails(string searchText, DateTime? terminationDate = null, bool useContractEndDate = false)
        {
            if (string.IsNullOrWhiteSpace(searchText) || searchText.Trim().Length < 2)
                return Json(new object[0], JsonRequestBehavior.AllowGet);

            var q = searchText.Trim();
            DateTime referenceDate = terminationDate ?? DateTime.Now;

            var contracts = db.PropertyContracts
                .AsNoTracking()
                .Where(a => a.IsDeleted == false && (
                       a.DocumentNumber.Contains(q)
                    || a.PropertyRenter.Mobile.Contains(q)
                    || a.PropertyRenter.ArName.Contains(q)
                    || a.PropertyRenter.EnName.Contains(q)
                    || a.Property.Code.Contains(q)
                    || a.Property.ArName.Contains(q)
                    || a.Property.EnName.Contains(q)
                    || a.PropertyUnitType.Code.Contains(q)
                    || a.PropertyUnitType.ArName.Contains(q)
                    || a.PropertyUnitType.EnName.Contains(q)
                    || a.Property.PropertyDetails.Any(pd => pd.IsDeleted == false
                                                            && pd.PropertyUnitNo.Contains(q))
                ))
                .OrderByDescending(a => a.Id)
                .Take(30)
                .ToList();

            var details = contracts.Select(a =>
            {
                DateTime calculationEndDate;
                List<PropertyContractBatch> allBatches;

                if (useContractEndDate)
                {
                    allBatches = a.PropertyContractBatches
                        .Where(b => b.BatchDate.HasValue)
                        .OrderBy(b => b.BatchDate)
                        .ToList();

                    var lastBatchDate = allBatches.LastOrDefault()?.BatchDate;
                    calculationEndDate = lastBatchDate.HasValue ? lastBatchDate.Value :
                                       (a.ContractEndDate.HasValue ? a.ContractEndDate.Value : referenceDate);
                }
                else
                {
                    calculationEndDate = referenceDate;
                    allBatches = a.PropertyContractBatches
                        .Where(b => b.BatchDate.HasValue)
                        .OrderBy(b => b.BatchDate)
                        .ToList();
                }

                // نجيب الدفعات المستحقة (اللي استحقاقها قبل أو في تاريخ التصفية)
                var relevantBatches = allBatches
                    .Where(b => b.BatchDate.HasValue && b.BatchDate.Value.AddDays(30) <= calculationEndDate)
                    .OrderBy(b => b.BatchDate)
                    .ToList();

                // جلب المدفوعات
                var batchIds = allBatches.Select(b => b.Id).ToList();

                var payments = db.CashReceiptVoucherPropertyContractBatches
                    .AsNoTracking()
                    .Where(p => p.PropertyContractBatchId.HasValue
                             && batchIds.Contains(p.PropertyContractBatchId.Value)
                             && p.Paid != 0)
                    .GroupBy(p => p.PropertyContractBatchId.Value)
                    .Select(g => new
                    {
                        PropertyContractBatchId = g.Key,
                        TotalPaid = g.Sum(x => x.Paid ?? 0),
                        IsFullyDelivered = g.Any(x => x.IsDelivered == true)
                    })
                    .ToList();

                // المتغيرات للحساب
                decimal dailyRate = 0m;
                DateTime? lastCompletedBatchDueDate = null;
                int increaseDays = 0;
                decimal increaseAmount = 0m;

                // نجيب آخر دفعة مستحقة كاملة
                var lastCompletedBatch = relevantBatches.LastOrDefault();

                if (lastCompletedBatch != null && lastCompletedBatch.BatchDate.HasValue)
                {
                    // تاريخ استحقاق آخر دفعة كاملة
                    lastCompletedBatchDueDate = lastCompletedBatch.BatchDate.Value.AddDays(30);

                    // حساب سعر اليوم
                    if (lastCompletedBatch.BatchTotal.HasValue && lastCompletedBatch.BatchTotal.Value > 0)
                    {
                        dailyRate = lastCompletedBatch.BatchTotal.Value / 30m;
                    }

                    // حساب أيام الزيادة
                    increaseDays = (calculationEndDate.Date - lastCompletedBatchDueDate.Value.Date).Days;
                    increaseAmount = dailyRate * increaseDays;
                }

                // تفاصيل الدفعات
                // تفاصيل الدفعات
                var detailsList = relevantBatches.Select(b =>
                {
                    var payment = payments.FirstOrDefault(p => p.PropertyContractBatchId == b.Id);
                    decimal totalPaid = payment?.TotalPaid ?? 0;
                    bool isFullyPaid = payment?.IsFullyDelivered == true;

                    decimal batchRentOriginal = (b.BatchRentValue ?? 0) + (b.BatchRentValueTaxes ?? 0);
                    decimal batchWaterOriginal = (b.BatchWaterValue ?? 0) + (b.BatchWaterValueTaxes ?? 0);
                    decimal batchElectricityOriginal = (b.BatchElectricityValue ?? 0) + (b.BatchElectricityValueTaxes ?? 0);
                    decimal batchCommissionOriginal = (b.BatchCommissionValue ?? 0) + (b.BatchCommissionValueTaxes ?? 0);
                    decimal batchServicesOriginal = (b.BatchServicesValue ?? 0) + (b.BatchServicesValueTaxes ?? 0);
                    decimal batchInsuranceOriginal = (b.BatchInsuranceValue ?? 0) + (b.BatchInsuranceValueTaxes ?? 0);
                    decimal batchTotalOriginal = b.BatchTotal ?? 0;

                    decimal remain = batchTotalOriginal - totalPaid;
                    remain = remain < 0 ? 0 : remain;

                    return new
                    {
                        BatchNo = b.BatchNo,  // خليها زي ما هي (int?)
                        b.BatchDate,
                        PropertyContractBatchId = (int?)b.Id,
                        PropertyContractId = b.MainDocId,
                        IsPartialBatch = false,

                        BatchRentValue = isFullyPaid ? 0m :
                                       (remain > 0 && batchTotalOriginal > 0 ? (batchRentOriginal * remain / batchTotalOriginal) : 0m),

                        BatchWaterValue = isFullyPaid ? 0m :
                                        (remain > 0 && batchTotalOriginal > 0 ? (batchWaterOriginal * remain / batchTotalOriginal) : 0m),

                        BatchElectricityValue = isFullyPaid ? 0m :
                                              (remain > 0 && batchTotalOriginal > 0 ? (batchElectricityOriginal * remain / batchTotalOriginal) : 0m),

                        BatchCommissionValue = isFullyPaid ? 0m :
                                             (remain > 0 && batchTotalOriginal > 0 ? (batchCommissionOriginal * remain / batchTotalOriginal) : 0m),

                        BatchServicesValue = isFullyPaid ? 0m :
                                           (remain > 0 && batchTotalOriginal > 0 ? (batchServicesOriginal * remain / batchTotalOriginal) : 0m),

                        BatchInsuranceValue = isFullyPaid ? 0m :
                                            (remain > 0 && batchTotalOriginal > 0 ? (batchInsuranceOriginal * remain / batchTotalOriginal) : 0m),

                        Remain = remain,
                        OriginalTotal = batchTotalOriginal,
                        TotalPaid = totalPaid
                    };
                }).ToList();

                // إضافة الدفعة الجزئية إذا كان هناك أيام زيادة
                // إضافة الدفعة الجزئية إذا كان هناك أيام زيادة
                if (increaseDays > 0 && lastCompletedBatch != null && lastCompletedBatchDueDate.HasValue)
                {
                    decimal lastBatchTotal = lastCompletedBatch.BatchTotal ?? 0;

                    detailsList.Add(new
                    {
                        BatchNo = (int?)((lastCompletedBatch.BatchNo ?? 0) + 1),
                        BatchDate = lastCompletedBatchDueDate,
                        PropertyContractBatchId = (int?)0,  // بدل null استخدم 0
                        PropertyContractId = lastCompletedBatch.MainDocId,
                        IsPartialBatch = true,

                        BatchRentValue = lastBatchTotal > 0 ?
                                       increaseAmount * ((lastCompletedBatch.BatchRentValue ?? 0) + (lastCompletedBatch.BatchRentValueTaxes ?? 0)) / lastBatchTotal : 0m,

                        BatchWaterValue = lastBatchTotal > 0 ?
                                        increaseAmount * ((lastCompletedBatch.BatchWaterValue ?? 0) + (lastCompletedBatch.BatchWaterValueTaxes ?? 0)) / lastBatchTotal : 0m,

                        BatchElectricityValue = lastBatchTotal > 0 ?
                                              increaseAmount * ((lastCompletedBatch.BatchElectricityValue ?? 0) + (lastCompletedBatch.BatchElectricityValueTaxes ?? 0)) / lastBatchTotal : 0m,

                        BatchCommissionValue = lastBatchTotal > 0 ?
                                             increaseAmount * ((lastCompletedBatch.BatchCommissionValue ?? 0) + (lastCompletedBatch.BatchCommissionValueTaxes ?? 0)) / lastBatchTotal : 0m,

                        BatchServicesValue = lastBatchTotal > 0 ?
                                           increaseAmount * ((lastCompletedBatch.BatchServicesValue ?? 0) + (lastCompletedBatch.BatchServicesValueTaxes ?? 0)) / lastBatchTotal : 0m,

                        BatchInsuranceValue = lastBatchTotal > 0 ?
                                            increaseAmount * ((lastCompletedBatch.BatchInsuranceValue ?? 0) + (lastCompletedBatch.BatchInsuranceValueTaxes ?? 0)) / lastBatchTotal : 0m,

                        Remain = increaseAmount,
                        OriginalTotal = increaseAmount,
                        TotalPaid = 0m
                    });
                }

                return new
                {
                    ContractId = a.Id,
                    a.UnifiedContractNumber,
                    a.ContractStartDate,
                    a.ContractEndDate,
                    CalculationEndDate = calculationEndDate,
                    UseContractEndDate = useContractEndDate,

                    ContractNo = a.DocumentNumber,

                    a.PropertyId,
                    Property = a.Property.ArName,

                    RenterId = a.PropertyRenterId,
                    Renter = a.PropertyRenter.ArName,
                    RenterMobile = a.PropertyRenter.Mobile,

                    UnitId = a.PropertyUnitId,
                    Unit = a.PropertyUnit != null ? a.PropertyUnit.ArName : "",

                    UnitCode = a.Property.PropertyDetails
                                        .Where(c => c.Id == a.PropertyUnitId)
                                        .Select(c => c.PropertyUnitNo)
                                        .FirstOrDefault(),

                    UnitTypeId = a.PropertyUnitTypeId,
                    UnitType = a.PropertyUnitType.ArName,

                    MergedUnits = a.Property.PropertyDetails
                        .Where(pd => a.PropertyContractMergedUnit
                                        .Select(mu => mu.PropertyUnitId)
                                        .Contains(pd.Id))
                        .Select(pd => new { pd.Id, pd.PropertyUnitNo })
                        .ToList(),

                    // معلومات مرجعية
                    LastCompletedBatchDueDate = lastCompletedBatchDueDate,

                    DailyRate = dailyRate,
                    DaysDifference = increaseDays,
                    IncreaseDays = increaseDays,
                    DecreaseDays = 0,

                    IncreaseAmount = increaseAmount,
                    DecreaseAmount = 0m,

                    Details = detailsList
                };
            }).ToList();

            return Json(details, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetPropertyContractTerminationDetailsTest4(string searchText, DateTime? terminationDate = null, bool useContractEndDate = false)
        {
            if (string.IsNullOrWhiteSpace(searchText) || searchText.Trim().Length < 2)
                return Json(new object[0], JsonRequestBehavior.AllowGet);

            var q = searchText.Trim();
            DateTime referenceDate = terminationDate ?? DateTime.Now;

            var contracts = db.PropertyContracts
                .AsNoTracking()
                .Where(a => a.IsDeleted == false && (
                       a.DocumentNumber.Contains(q)
                    || a.PropertyRenter.Mobile.Contains(q)
                    || a.PropertyRenter.ArName.Contains(q)
                    || a.PropertyRenter.EnName.Contains(q)
                    || a.Property.Code.Contains(q)
                    || a.Property.ArName.Contains(q)
                    || a.Property.EnName.Contains(q)
                    || a.PropertyUnitType.Code.Contains(q)
                    || a.PropertyUnitType.ArName.Contains(q)
                    || a.PropertyUnitType.EnName.Contains(q)
                    || a.Property.PropertyDetails.Any(pd => pd.IsDeleted == false
                                                            && pd.PropertyUnitNo.Contains(q))
                ))
                .OrderByDescending(a => a.Id)
                .Take(30)
                .ToList();

            var details = contracts.Select(a =>
            {
                DateTime calculationEndDate;
                List<PropertyContractBatch> relevantBatches;

                if (useContractEndDate)
                {
                    relevantBatches = a.PropertyContractBatches
                        .Where(b => b.BatchDate.HasValue)
                        .OrderBy(b => b.BatchDate)
                        .ToList();

                    var lastBatchDate = relevantBatches.LastOrDefault()?.BatchDate;
                    calculationEndDate = lastBatchDate.HasValue ? lastBatchDate.Value :
                                       (a.ContractEndDate.HasValue ? a.ContractEndDate.Value : referenceDate);
                }
                else
                {
                    calculationEndDate = referenceDate;
                    relevantBatches = a.PropertyContractBatches
                        .Where(b => b.BatchDate.HasValue && b.BatchDate.Value <= calculationEndDate)
                        .OrderBy(b => b.BatchDate)
                        .ToList();
                }

                // جلب المدفوعات
                var batchIds = relevantBatches.Select(b => b.Id).ToList();

                var payments = db.CashReceiptVoucherPropertyContractBatches
                    .AsNoTracking()
                    .Where(p => p.PropertyContractBatchId.HasValue
                             && batchIds.Contains(p.PropertyContractBatchId.Value)
                             && p.Paid != 0)
                    .GroupBy(p => p.PropertyContractBatchId.Value)
                    .Select(g => new
                    {
                        PropertyContractBatchId = g.Key,
                        TotalPaid = g.Sum(x => x.Paid ?? 0),
                        IsFullyDelivered = g.Any(x => x.IsDelivered == true)
                    })
                    .ToList();

                // تحديد آخر دفعة
                var lastBatch = relevantBatches
                    .OrderByDescending(b => b.BatchDate)
                    .FirstOrDefault();

                // المتغيرات للحساب
                decimal dailyRate = 0m;
                DateTime? lastBatchStartDate = null;
                DateTime? lastBatchDueDate = null;
                int actualDaysInLastBatch = 0;
                int increaseDays = 0;
                int decreaseDays = 0;
                decimal increaseAmount = 0m;
                decimal decreaseAmount = 0m;
                bool isPartialLastBatch = false;

                if (lastBatch != null && lastBatch.BatchDate.HasValue)
                {
                    lastBatchStartDate = lastBatch.BatchDate.Value;
                    lastBatchDueDate = lastBatchStartDate.Value.AddDays(30);

                    // حساب الأيام الفعلية من بداية آخر دفعة لحد تاريخ التصفية
                    actualDaysInLastBatch = (calculationEndDate.Date - lastBatchStartDate.Value.Date).Days;

                    // حساب سعر اليوم
                    if (lastBatch.BatchTotal.HasValue && lastBatch.BatchTotal.Value > 0)
                    {
                        dailyRate = lastBatch.BatchTotal.Value / 30m;
                    }

                    // الحساب الصحيح: هل الدفعة مستحقة كاملة ولا لأ؟
                    if (lastBatchDueDate.Value <= calculationEndDate)
                    {
                        // الدفعة خلصت ال30 يوم → يبقى عنده أيام زيادة
                        increaseDays = (calculationEndDate.Date - lastBatchDueDate.Value.Date).Days;
                        increaseAmount = dailyRate * increaseDays;
                        isPartialLastBatch = false;
                    }
                    else
                    {
                        // الدفعة لسه ما خلصتش ال30 يوم → يبقى دفعة جزئية
                        if (actualDaysInLastBatch < 30)
                        {
                            decreaseDays = 30 - actualDaysInLastBatch;
                            decreaseAmount = dailyRate * decreaseDays;
                            isPartialLastBatch = true;
                        }
                    }
                }

                return new
                {
                    ContractId = a.Id,
                    a.UnifiedContractNumber,
                    a.ContractStartDate,
                    a.ContractEndDate,
                    CalculationEndDate = calculationEndDate,
                    UseContractEndDate = useContractEndDate,

                    ContractNo = a.DocumentNumber,

                    a.PropertyId,
                    Property = a.Property.ArName,

                    RenterId = a.PropertyRenterId,
                    Renter = a.PropertyRenter.ArName,
                    RenterMobile = a.PropertyRenter.Mobile,

                    UnitId = a.PropertyUnitId,
                    Unit = a.PropertyUnit != null ? a.PropertyUnit.ArName : "",

                    UnitCode = a.Property.PropertyDetails
                                        .Where(c => c.Id == a.PropertyUnitId)
                                        .Select(c => c.PropertyUnitNo)
                                        .FirstOrDefault(),

                    UnitTypeId = a.PropertyUnitTypeId,
                    UnitType = a.PropertyUnitType.ArName,

                    MergedUnits = a.Property.PropertyDetails
                        .Where(pd => a.PropertyContractMergedUnit
                                        .Select(mu => mu.PropertyUnitId)
                                        .Contains(pd.Id))
                        .Select(pd => new { pd.Id, pd.PropertyUnitNo })
                        .ToList(),

                    // معلومات مرجعية واضحة
                    LastBatchStartDate = lastBatchStartDate,      // بداية آخر دفعة (مثلاً 16/10)
                    LastBatchDueDate = lastBatchDueDate,          // استحقاق آخر دفعة (مثلاً 15/11)
                    ActualDaysInLastBatch = actualDaysInLastBatch, // الأيام الفعلية من البداية

                    DailyRate = dailyRate,

                    IncreaseDays = increaseDays,                  // أيام زيادة (لو الدفعة خلصت)
                    DecreaseDays = decreaseDays,                  // أيام نقص (لو الدفعة جزئية)

                    IncreaseAmount = increaseAmount,
                    DecreaseAmount = decreaseAmount,

                    // تفاصيل الدفعات
                    Details = relevantBatches.Select((b, index) =>
                    {
                        var payment = payments.FirstOrDefault(p => p.PropertyContractBatchId == b.Id);

                        decimal totalPaid = payment?.TotalPaid ?? 0;
                        bool isFullyPaid = payment?.IsFullyDelivered == true;

                        // حساب القيم الأصلية
                        decimal batchRentOriginal = (b.BatchRentValue ?? 0) + (b.BatchRentValueTaxes ?? 0);
                        decimal batchWaterOriginal = (b.BatchWaterValue ?? 0) + (b.BatchWaterValueTaxes ?? 0);
                        decimal batchElectricityOriginal = (b.BatchElectricityValue ?? 0) + (b.BatchElectricityValueTaxes ?? 0);
                        decimal batchCommissionOriginal = (b.BatchCommissionValue ?? 0) + (b.BatchCommissionValueTaxes ?? 0);
                        decimal batchServicesOriginal = (b.BatchServicesValue ?? 0) + (b.BatchServicesValueTaxes ?? 0);
                        decimal batchInsuranceOriginal = (b.BatchInsuranceValue ?? 0) + (b.BatchInsuranceValueTaxes ?? 0);

                        decimal batchTotalOriginal = b.BatchTotal ?? 0;

                        // التحقق: هل دي آخر دفعة؟
                        bool isLastBatch = (index == relevantBatches.Count - 1);

                        // لو آخر دفعة وجزئية، نحسب بس الأيام الفعلية
                        if (isLastBatch && isPartialLastBatch)
                        {
                            // حساب القيمة بناءً على الأيام الفعلية
                            decimal partialAmount = dailyRate * actualDaysInLastBatch;

                            // طرح المدفوع
                            decimal remain = partialAmount - totalPaid;
                            remain = remain < 0 ? 0 : remain;

                            // توزيع المبلغ المتبقي على البنود بنفس النسبة
                            decimal distributionRatio = batchTotalOriginal > 0 ? (partialAmount / batchTotalOriginal) : 0;
                            decimal remainRatio = partialAmount > 0 ? (remain / partialAmount) : 0;

                            return new
                            {
                                b.BatchNo,
                                b.BatchDate,
                                PropertyContractBatchId = b.Id,
                                PropertyContractId = b.MainDocId,

                                BatchRentValue = isFullyPaid ? 0 : (batchRentOriginal * distributionRatio * remainRatio),
                                BatchWaterValue = isFullyPaid ? 0 : (batchWaterOriginal * distributionRatio * remainRatio),
                                BatchElectricityValue = isFullyPaid ? 0 : (batchElectricityOriginal * distributionRatio * remainRatio),
                                BatchCommissionValue = isFullyPaid ? 0 : (batchCommissionOriginal * distributionRatio * remainRatio),
                                BatchServicesValue = isFullyPaid ? 0 : (batchServicesOriginal * distributionRatio * remainRatio),
                                BatchInsuranceValue = isFullyPaid ? 0 : (batchInsuranceOriginal * distributionRatio * remainRatio),

                                Remain = remain,
                                OriginalTotal = batchTotalOriginal,
                                PartialTotal = partialAmount,
                                TotalPaid = totalPaid,
                                IsPartial = true
                            };
                        }
                        else
                        {
                            // دفعة عادية (كاملة)
                            decimal remain = batchTotalOriginal - totalPaid;
                            remain = remain < 0 ? 0 : remain;

                            return new
                            {
                                b.BatchNo,
                                b.BatchDate,
                                PropertyContractBatchId = b.Id,
                                PropertyContractId = b.MainDocId,

                                BatchRentValue = isFullyPaid ? 0 :
                                               (remain > 0 && batchTotalOriginal > 0 ? (batchRentOriginal * remain / batchTotalOriginal) : 0),

                                BatchWaterValue = isFullyPaid ? 0 :
                                                (remain > 0 && batchTotalOriginal > 0 ? (batchWaterOriginal * remain / batchTotalOriginal) : 0),

                                BatchElectricityValue = isFullyPaid ? 0 :
                                                      (remain > 0 && batchTotalOriginal > 0 ? (batchElectricityOriginal * remain / batchTotalOriginal) : 0),

                                BatchCommissionValue = isFullyPaid ? 0 :
                                                     (remain > 0 && batchTotalOriginal > 0 ? (batchCommissionOriginal * remain / batchTotalOriginal) : 0),

                                BatchServicesValue = isFullyPaid ? 0 :
                                                   (remain > 0 && batchTotalOriginal > 0 ? (batchServicesOriginal * remain / batchTotalOriginal) : 0),

                                BatchInsuranceValue = isFullyPaid ? 0 :
                                                    (remain > 0 && batchTotalOriginal > 0 ? (batchInsuranceOriginal * remain / batchTotalOriginal) : 0),

                                Remain = remain,
                                OriginalTotal = batchTotalOriginal,
                                PartialTotal = batchTotalOriginal,
                                TotalPaid = totalPaid,
                                IsPartial = false
                            };
                        }
                    }).ToList()
                };
            }).ToList();

            return Json(details, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetPropertyContractTerminationDetailsTested2(string searchText, DateTime? terminationDate = null, bool useContractEndDate = false)
        {
            if (string.IsNullOrWhiteSpace(searchText) || searchText.Trim().Length < 2)
                return Json(new object[0], JsonRequestBehavior.AllowGet);

            var q = searchText.Trim();
            DateTime referenceDate = terminationDate ?? DateTime.Now;

            var contracts = db.PropertyContracts
                .AsNoTracking()
                .Where(a => a.IsDeleted == false && (
                       a.DocumentNumber.Contains(q)
                    || a.PropertyRenter.Mobile.Contains(q)
                    || a.PropertyRenter.ArName.Contains(q)
                    || a.PropertyRenter.EnName.Contains(q)
                    || a.Property.Code.Contains(q)
                    || a.Property.ArName.Contains(q)
                    || a.Property.EnName.Contains(q)
                    || a.PropertyUnitType.Code.Contains(q)
                    || a.PropertyUnitType.ArName.Contains(q)
                    || a.PropertyUnitType.EnName.Contains(q)
                    || a.Property.PropertyDetails.Any(pd => pd.IsDeleted == false
                                                            && pd.PropertyUnitNo.Contains(q))
                ))
                .OrderByDescending(a => a.Id)
                .Take(30)
                .ToList();

            var details = contracts.Select(a =>
            {
                DateTime calculationEndDate;
                List<PropertyContractBatch> relevantBatches;

                if (useContractEndDate)
                {
                    relevantBatches = a.PropertyContractBatches
                        .Where(b => b.BatchDate.HasValue)
                        .OrderBy(b => b.BatchDate)
                        .ToList();

                    var lastBatchDate = relevantBatches.LastOrDefault()?.BatchDate;
                    calculationEndDate = lastBatchDate.HasValue ? lastBatchDate.Value :
                                       (a.ContractEndDate.HasValue ? a.ContractEndDate.Value : referenceDate);
                }
                else
                {
                    calculationEndDate = referenceDate;
                    relevantBatches = a.PropertyContractBatches
                        .Where(b => b.BatchDate.HasValue && b.BatchDate.Value <= calculationEndDate)
                        .OrderBy(b => b.BatchDate)
                        .ToList();
                }

                // جلب المدفوعات
                var batchIds = relevantBatches.Select(b => b.Id).ToList();

                var payments = db.CashReceiptVoucherPropertyContractBatches
                    .AsNoTracking()
                    .Where(p => p.PropertyContractBatchId.HasValue
                             && batchIds.Contains(p.PropertyContractBatchId.Value)
                             && p.Paid != 0)
                    .GroupBy(p => p.PropertyContractBatchId.Value)
                    .Select(g => new
                    {
                        PropertyContractBatchId = g.Key,
                        TotalPaid = g.Sum(x => x.Paid ?? 0),
                        IsFullyDelivered = g.Any(x => x.IsDelivered == true)
                    })
                    .ToList();

                // تحديد آخر دفعة
                var lastBatch = relevantBatches
                    .OrderByDescending(b => b.BatchDate)
                    .FirstOrDefault();

                // المتغيرات للحساب
                decimal dailyRate = 0m;
                DateTime? lastBatchStartDate = null;
                DateTime? lastBatchDueDate = null;
                int actualDaysInLastBatch = 0;
                int increaseDays = 0;
                int decreaseDays = 0;
                decimal increaseAmount = 0m;
                decimal decreaseAmount = 0m;

                if (lastBatch != null && lastBatch.BatchDate.HasValue)
                {
                    lastBatchStartDate = lastBatch.BatchDate.Value;
                    lastBatchDueDate = lastBatchStartDate.Value.AddDays(30);

                    // حساب الأيام الفعلية من بداية آخر دفعة لحد تاريخ التصفية
                    actualDaysInLastBatch = (calculationEndDate.Date - lastBatchStartDate.Value.Date).Days;

                    // حساب سعر اليوم
                    if (lastBatch.BatchTotal.HasValue && lastBatch.BatchTotal.Value > 0)
                    {
                        dailyRate = lastBatch.BatchTotal.Value / 30m;
                    }

                    // حساب الزيادة أو النقص
                    if (actualDaysInLastBatch > 30)
                    {
                        // أيام زيادة
                        increaseDays = actualDaysInLastBatch - 30;
                        increaseAmount = dailyRate * increaseDays;
                    }
                    else if (actualDaysInLastBatch < 30)
                    {
                        // أيام نقص
                        decreaseDays = 30 - actualDaysInLastBatch;
                        decreaseAmount = dailyRate * decreaseDays;
                    }
                    // لو actualDaysInLastBatch = 30 تماماً، يبقى لا زيادة ولا نقص
                }

                return new
                {
                    ContractId = a.Id,
                    a.UnifiedContractNumber,
                    a.ContractStartDate,
                    a.ContractEndDate,
                    CalculationEndDate = calculationEndDate,
                    UseContractEndDate = useContractEndDate,

                    ContractNo = a.DocumentNumber,

                    a.PropertyId,
                    Property = a.Property.ArName,

                    RenterId = a.PropertyRenterId,
                    Renter = a.PropertyRenter.ArName,
                    RenterMobile = a.PropertyRenter.Mobile,

                    UnitId = a.PropertyUnitId,
                    Unit = a.PropertyUnit != null ? a.PropertyUnit.ArName : "",

                    UnitCode = a.Property.PropertyDetails
                                        .Where(c => c.Id == a.PropertyUnitId)
                                        .Select(c => c.PropertyUnitNo)
                                        .FirstOrDefault(),

                    UnitTypeId = a.PropertyUnitTypeId,
                    UnitType = a.PropertyUnitType.ArName,

                    MergedUnits = a.Property.PropertyDetails
                        .Where(pd => a.PropertyContractMergedUnit
                                        .Select(mu => mu.PropertyUnitId)
                                        .Contains(pd.Id))
                        .Select(pd => new { pd.Id, pd.PropertyUnitNo })
                        .ToList(),

                    // معلومات مرجعية واضحة
                    LastBatchStartDate = lastBatchStartDate,      // تاريخ بداية آخر دفعة
                    LastBatchDueDate = lastBatchDueDate,          // تاريخ استحقاق آخر دفعة (بداية + 30 يوم)
                    ActualDaysInLastBatch = actualDaysInLastBatch, // الأيام الفعلية من البداية لحد التصفية

                    DailyRate = dailyRate,                        // سعر اليوم

                    IncreaseDays = increaseDays,                  // عدد أيام الزيادة
                    DecreaseDays = decreaseDays,                  // عدد أيام النقص

                    IncreaseAmount = increaseAmount,              // قيمة الزيادة
                    DecreaseAmount = decreaseAmount,              // قيمة النقص



                    // تفاصيل الدفعات
                    Details = relevantBatches.Select((b, index) =>
                    {
                        var payment = payments.FirstOrDefault(p => p.PropertyContractBatchId == b.Id);

                        decimal totalPaid = payment?.TotalPaid ?? 0;
                        bool isFullyPaid = payment?.IsFullyDelivered == true;

                        // حساب القيم الأصلية
                        decimal batchRentOriginal = (b.BatchRentValue ?? 0) + (b.BatchRentValueTaxes ?? 0);
                        decimal batchWaterOriginal = (b.BatchWaterValue ?? 0) + (b.BatchWaterValueTaxes ?? 0);
                        decimal batchElectricityOriginal = (b.BatchElectricityValue ?? 0) + (b.BatchElectricityValueTaxes ?? 0);
                        decimal batchCommissionOriginal = (b.BatchCommissionValue ?? 0) + (b.BatchCommissionValueTaxes ?? 0);
                        decimal batchServicesOriginal = (b.BatchServicesValue ?? 0) + (b.BatchServicesValueTaxes ?? 0);
                        decimal batchInsuranceOriginal = (b.BatchInsuranceValue ?? 0) + (b.BatchInsuranceValueTaxes ?? 0);

                        decimal batchTotalOriginal = b.BatchTotal ?? 0;

                        // التحقق: هل دي آخر دفعة؟
                        bool isLastBatch = (index == relevantBatches.Count - 1);

                        // لو آخر دفعة والمستأجر قعد أقل من 30 يوم، نحسب بس الأيام الفعلية
                        if (isLastBatch && actualDaysInLastBatch < 30 && actualDaysInLastBatch >= 0)
                        {
                            // حساب القيمة بناءً على الأيام الفعلية
                            decimal partialAmount = dailyRate * actualDaysInLastBatch;

                            // طرح المدفوع
                            decimal remain = partialAmount - totalPaid;
                            remain = remain < 0 ? 0 : remain;

                            // توزيع المبلغ المتبقي على البنود بنفس النسبة
                            decimal distributionRatio = batchTotalOriginal > 0 ? (partialAmount / batchTotalOriginal) : 0;
                            decimal remainRatio = partialAmount > 0 ? (remain / partialAmount) : 0;

                            return new
                            {
                                b.BatchNo,
                                b.BatchDate,
                                PropertyContractBatchId = b.Id,
                                PropertyContractId = b.MainDocId,

                                BatchRentValue = isFullyPaid ? 0 : (batchRentOriginal * distributionRatio * remainRatio),
                                BatchWaterValue = isFullyPaid ? 0 : (batchWaterOriginal * distributionRatio * remainRatio),
                                BatchElectricityValue = isFullyPaid ? 0 : (batchElectricityOriginal * distributionRatio * remainRatio),
                                BatchCommissionValue = isFullyPaid ? 0 : (batchCommissionOriginal * distributionRatio * remainRatio),
                                BatchServicesValue = isFullyPaid ? 0 : (batchServicesOriginal * distributionRatio * remainRatio),
                                BatchInsuranceValue = isFullyPaid ? 0 : (batchInsuranceOriginal * distributionRatio * remainRatio),

                                Remain = remain,
                                OriginalTotal = batchTotalOriginal,
                                PartialTotal = partialAmount,
                                TotalPaid = totalPaid,
                                IsPartial = true
                            };
                        }
                        else
                        {
                            // دفعة عادية (كاملة أو أكتر من 30 يوم)
                            decimal remain = batchTotalOriginal - totalPaid;
                            remain = remain < 0 ? 0 : remain;

                            return new
                            {
                                b.BatchNo,
                                b.BatchDate,
                                PropertyContractBatchId = b.Id,
                                PropertyContractId = b.MainDocId,

                                BatchRentValue = isFullyPaid ? 0 :
                                               (remain > 0 && batchTotalOriginal > 0 ? (batchRentOriginal * remain / batchTotalOriginal) : 0),

                                BatchWaterValue = isFullyPaid ? 0 :
                                                (remain > 0 && batchTotalOriginal > 0 ? (batchWaterOriginal * remain / batchTotalOriginal) : 0),

                                BatchElectricityValue = isFullyPaid ? 0 :
                                                      (remain > 0 && batchTotalOriginal > 0 ? (batchElectricityOriginal * remain / batchTotalOriginal) : 0),

                                BatchCommissionValue = isFullyPaid ? 0 :
                                                     (remain > 0 && batchTotalOriginal > 0 ? (batchCommissionOriginal * remain / batchTotalOriginal) : 0),

                                BatchServicesValue = isFullyPaid ? 0 :
                                                   (remain > 0 && batchTotalOriginal > 0 ? (batchServicesOriginal * remain / batchTotalOriginal) : 0),

                                BatchInsuranceValue = isFullyPaid ? 0 :
                                                    (remain > 0 && batchTotalOriginal > 0 ? (batchInsuranceOriginal * remain / batchTotalOriginal) : 0),

                                Remain = remain,
                                OriginalTotal = batchTotalOriginal,
                                PartialTotal = batchTotalOriginal,
                                TotalPaid = totalPaid,
                                IsPartial = false
                            };
                        }
                    }).ToList()
                };
            }).ToList();

            return Json(details, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetPropertyContractTerminationDetailsTestd(string searchText, DateTime? terminationDate = null, bool useContractEndDate = false)
        {
            if (string.IsNullOrWhiteSpace(searchText) || searchText.Trim().Length < 2)
                return Json(new object[0], JsonRequestBehavior.AllowGet);

            var q = searchText.Trim();
            DateTime referenceDate = terminationDate ?? DateTime.Now;

            var contracts = db.PropertyContracts
                .AsNoTracking()
                .Where(a => a.IsDeleted == false && (
                       a.DocumentNumber.Contains(q)
                    || a.PropertyRenter.Mobile.Contains(q)
                    || a.PropertyRenter.ArName.Contains(q)
                    || a.PropertyRenter.EnName.Contains(q)
                    || a.Property.Code.Contains(q)
                    || a.Property.ArName.Contains(q)
                    || a.Property.EnName.Contains(q)
                    || a.PropertyUnitType.Code.Contains(q)
                    || a.PropertyUnitType.ArName.Contains(q)
                    || a.PropertyUnitType.EnName.Contains(q)
                    || a.Property.PropertyDetails.Any(pd => pd.IsDeleted == false
                                                            && pd.PropertyUnitNo.Contains(q))
                ))
                .OrderByDescending(a => a.Id)
                .Take(30)
                .ToList();

            var details = contracts.Select(a =>
            {
                DateTime calculationEndDate;
                List<PropertyContractBatch> relevantBatches;

                if (useContractEndDate)
                {
                    relevantBatches = a.PropertyContractBatches
                        .Where(b => b.BatchDate.HasValue)
                        .OrderBy(b => b.BatchDate)
                        .ToList();

                    var lastBatchDate = relevantBatches.LastOrDefault()?.BatchDate;
                    calculationEndDate = lastBatchDate.HasValue ? lastBatchDate.Value :
                                       (a.ContractEndDate.HasValue ? a.ContractEndDate.Value : referenceDate);
                }
                else
                {
                    calculationEndDate = referenceDate;
                    relevantBatches = a.PropertyContractBatches
                        .Where(b => b.BatchDate.HasValue && b.BatchDate.Value <= calculationEndDate)
                        .OrderBy(b => b.BatchDate)
                        .ToList();
                }

                // جلب المدفوعات
                var batchIds = relevantBatches.Select(b => b.Id).ToList();

                var payments = db.CashReceiptVoucherPropertyContractBatches
                    .AsNoTracking()
                    .Where(p => p.PropertyContractBatchId.HasValue
                             && batchIds.Contains(p.PropertyContractBatchId.Value)
                             && p.Paid != 0)
                    .GroupBy(p => p.PropertyContractBatchId.Value)
                    .Select(g => new
                    {
                        PropertyContractBatchId = g.Key,
                        TotalPaid = g.Sum(x => x.Paid ?? 0),
                        IsFullyDelivered = g.Any(x => x.IsDelivered == true)
                    })
                    .ToList();

                // تحديد آخر دفعة
                var lastBatch = relevantBatches
                    .OrderByDescending(b => b.BatchDate)
                    .FirstOrDefault();

                int daysDifference = 0;
                decimal dailyRate = 0m;
                bool isPartialLastBatch = false;
                int actualDaysInLastBatch = 0;

                if (lastBatch != null && lastBatch.BatchDate.HasValue)
                {
                    // حساب عدد الأيام من بداية آخر دفعة لحد تاريخ التصفية
                    actualDaysInLastBatch = (calculationEndDate.Date - lastBatch.BatchDate.Value.Date).Days;

                    // لو الأيام أقل من 30 يوم، يبقى دي دفعة جزئية
                    if (actualDaysInLastBatch < 30 && actualDaysInLastBatch >= 0)
                    {
                        isPartialLastBatch = true;
                        daysDifference = actualDaysInLastBatch;
                    }
                    else if (actualDaysInLastBatch >= 30)
                    {
                        // لو أكتر من 30 يوم، يبقى في أيام زيادة
                        daysDifference = actualDaysInLastBatch - 30;
                    }
                    else
                    {
                        // لو سالب (تاريخ التصفية قبل آخر دفعة)
                        daysDifference = actualDaysInLastBatch;
                    }

                    if (lastBatch.BatchTotal.HasValue && lastBatch.BatchTotal.Value > 0)
                    {
                        dailyRate = lastBatch.BatchTotal.Value / 30m;
                    }
                }

                return new
                {
                    ContractId = a.Id,
                    a.UnifiedContractNumber,
                    a.ContractStartDate,
                    a.ContractEndDate,
                    CalculationEndDate = calculationEndDate,
                    UseContractEndDate = useContractEndDate,

                    ContractNo = a.DocumentNumber,

                    a.PropertyId,
                    Property = a.Property.ArName,

                    RenterId = a.PropertyRenterId,
                    Renter = a.PropertyRenter.ArName,
                    RenterMobile = a.PropertyRenter.Mobile,

                    UnitId = a.PropertyUnitId,
                    Unit = a.PropertyUnit != null ? a.PropertyUnit.ArName : "",

                    UnitCode = a.Property.PropertyDetails
                                        .Where(c => c.Id == a.PropertyUnitId)
                                        .Select(c => c.PropertyUnitNo)
                                        .FirstOrDefault(),

                    UnitTypeId = a.PropertyUnitTypeId,
                    UnitType = a.PropertyUnitType.ArName,

                    MergedUnits = a.Property.PropertyDetails
                        .Where(pd => a.PropertyContractMergedUnit
                                        .Select(mu => mu.PropertyUnitId)
                                        .Contains(pd.Id))
                        .Select(pd => new { pd.Id, pd.PropertyUnitNo })
                        .ToList(),

                    DaysDifference = daysDifference,
                    DailyRate = dailyRate,
                    IncreaseDays = daysDifference > 0 && !isPartialLastBatch ? daysDifference : 0,
                    DecreaseDays = daysDifference < 0 ? Math.Abs(daysDifference) : 0,
                    IncreaseAmount = (daysDifference > 0 && !isPartialLastBatch) ? (dailyRate * daysDifference) : 0,
                    DecreaseAmount = daysDifference < 0 ? (dailyRate * Math.Abs(daysDifference)) : 0,

                    // معلومات آخر دفعة الجزئية
                    IsPartialLastBatch = isPartialLastBatch,
                    ActualDaysInLastBatch = actualDaysInLastBatch,

                    // تفاصيل الدفعات
                    Details = relevantBatches.Select((b, index) =>
                    {
                        var payment = payments.FirstOrDefault(p => p.PropertyContractBatchId == b.Id);

                        decimal totalPaid = payment?.TotalPaid ?? 0;
                        bool isFullyPaid = payment?.IsFullyDelivered == true;

                        // حساب القيم الأصلية
                        decimal batchRentOriginal = (b.BatchRentValue ?? 0) + (b.BatchRentValueTaxes ?? 0);
                        decimal batchWaterOriginal = (b.BatchWaterValue ?? 0) + (b.BatchWaterValueTaxes ?? 0);
                        decimal batchElectricityOriginal = (b.BatchElectricityValue ?? 0) + (b.BatchElectricityValueTaxes ?? 0);
                        decimal batchCommissionOriginal = (b.BatchCommissionValue ?? 0) + (b.BatchCommissionValueTaxes ?? 0);
                        decimal batchServicesOriginal = (b.BatchServicesValue ?? 0) + (b.BatchServicesValueTaxes ?? 0);
                        decimal batchInsuranceOriginal = (b.BatchInsuranceValue ?? 0) + (b.BatchInsuranceValueTaxes ?? 0);

                        decimal batchTotalOriginal = b.BatchTotal ?? 0;

                        // التحقق: هل دي آخر دفعة؟
                        bool isLastBatch = (index == relevantBatches.Count - 1);

                        // لو آخر دفعة وجزئية، نحسب بس الأيام الفعلية
                        if (isLastBatch && isPartialLastBatch)
                        {
                            // حساب القيمة بناءً على الأيام الفعلية
                            decimal partialAmount = dailyRate * actualDaysInLastBatch;

                            // طرح المدفوع
                            decimal remain = partialAmount - totalPaid;
                            remain = remain < 0 ? 0 : remain;

                            // توزيع المبلغ المتبقي على البنود بنفس النسبة
                            decimal distributionRatio = batchTotalOriginal > 0 ? (partialAmount / batchTotalOriginal) : 0;

                            return new
                            {
                                b.BatchNo,
                                b.BatchDate,
                                PropertyContractBatchId = b.Id,
                                PropertyContractId = b.MainDocId,

                                BatchRentValue = isFullyPaid ? 0 : (batchRentOriginal * distributionRatio * (remain / partialAmount)),
                                BatchWaterValue = isFullyPaid ? 0 : (batchWaterOriginal * distributionRatio * (remain / partialAmount)),
                                BatchElectricityValue = isFullyPaid ? 0 : (batchElectricityOriginal * distributionRatio * (remain / partialAmount)),
                                BatchCommissionValue = isFullyPaid ? 0 : (batchCommissionOriginal * distributionRatio * (remain / partialAmount)),
                                BatchServicesValue = isFullyPaid ? 0 : (batchServicesOriginal * distributionRatio * (remain / partialAmount)),
                                BatchInsuranceValue = isFullyPaid ? 0 : (batchInsuranceOriginal * distributionRatio * (remain / partialAmount)),

                                Remain = remain,
                                OriginalTotal = batchTotalOriginal,
                                PartialTotal = partialAmount,
                                TotalPaid = totalPaid,
                                IsPartial = true
                            };
                        }
                        else
                        {
                            // دفعة عادية (كاملة)
                            decimal remain = batchTotalOriginal - totalPaid;
                            remain = remain < 0 ? 0 : remain;

                            return new
                            {
                                b.BatchNo,
                                b.BatchDate,
                                PropertyContractBatchId = b.Id,
                                PropertyContractId = b.MainDocId,

                                BatchRentValue = isFullyPaid ? 0 :
                                               (remain > 0 && batchTotalOriginal > 0 ? (batchRentOriginal * remain / batchTotalOriginal) : 0),

                                BatchWaterValue = isFullyPaid ? 0 :
                                                (remain > 0 && batchTotalOriginal > 0 ? (batchWaterOriginal * remain / batchTotalOriginal) : 0),

                                BatchElectricityValue = isFullyPaid ? 0 :
                                                      (remain > 0 && batchTotalOriginal > 0 ? (batchElectricityOriginal * remain / batchTotalOriginal) : 0),

                                BatchCommissionValue = isFullyPaid ? 0 :
                                                     (remain > 0 && batchTotalOriginal > 0 ? (batchCommissionOriginal * remain / batchTotalOriginal) : 0),

                                BatchServicesValue = isFullyPaid ? 0 :
                                                   (remain > 0 && batchTotalOriginal > 0 ? (batchServicesOriginal * remain / batchTotalOriginal) : 0),

                                BatchInsuranceValue = isFullyPaid ? 0 :
                                                    (remain > 0 && batchTotalOriginal > 0 ? (batchInsuranceOriginal * remain / batchTotalOriginal) : 0),

                                Remain = remain,
                                OriginalTotal = batchTotalOriginal,
                                PartialTotal = batchTotalOriginal,
                                TotalPaid = totalPaid,
                                IsPartial = false
                            };
                        }
                    }).ToList()
                };
            }).ToList();

            return Json(details, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetPropertyContractTerminationDetailsOld2(string searchText, DateTime? terminationDate = null, bool useContractEndDate = false)
        {
            // 1) حماية: لو مافيش نص أو أقل من حرفين نرجّع فاضي
            if (string.IsNullOrWhiteSpace(searchText) || searchText.Trim().Length < 2)
                return Json(new object[0], JsonRequestBehavior.AllowGet);

            var q = searchText.Trim();

            // تحديد التاريخ المرجعي للحساب
            DateTime referenceDate = terminationDate ?? DateTime.Now;

            // 2) الاستعلام مع AsNoTracking + ترتيب + حد أقصى للنتائج
            var contracts = db.PropertyContracts
                .AsNoTracking()
                .Where(a => a.IsDeleted == false && (
                       a.DocumentNumber.Contains(q)
                    || a.PropertyRenter.Mobile.Contains(q)
                    || a.PropertyRenter.ArName.Contains(q)
                    || a.PropertyRenter.EnName.Contains(q)
                    || a.Property.Code.Contains(q)
                    || a.Property.ArName.Contains(q)
                    || a.Property.EnName.Contains(q)
                    || a.PropertyUnitType.Code.Contains(q)
                    || a.PropertyUnitType.ArName.Contains(q)
                    || a.PropertyUnitType.EnName.Contains(q)
                    || a.Property.PropertyDetails.Any(pd => pd.IsDeleted == false
                                                            && pd.PropertyUnitNo.Contains(q))
                ))
                .OrderByDescending(a => a.Id)
                .Take(30)
                .ToList();

            var details = contracts.Select(a =>
            {
                DateTime calculationEndDate;
                List<PropertyContractBatch> relevantBatches;

                if (useContractEndDate)
                {
                    // "بنهاية العقد" → كل الدفعات
                    relevantBatches = a.PropertyContractBatches
                        .Where(b => b.BatchDate.HasValue)
                        .OrderBy(b => b.BatchDate)
                        .ToList();

                    var lastBatchDate = relevantBatches.LastOrDefault()?.BatchDate;
                    calculationEndDate = lastBatchDate.HasValue ? lastBatchDate.Value :
                                       (a.ContractEndDate.HasValue ? a.ContractEndDate.Value : referenceDate);
                }
                else
                {
                    // "بتاريخ التصفية" → فلترة
                    calculationEndDate = referenceDate;
                    relevantBatches = a.PropertyContractBatches
                        .Where(b => b.BatchDate.HasValue && b.BatchDate.Value <= calculationEndDate)
                        .OrderBy(b => b.BatchDate)
                        .ToList();
                }

                // جلب كل المدفوعات الخاصة بالدفعات دي من جدول السداد
                

                var batchIds = relevantBatches.Select(b => b.Id).ToList();

                var payments = db.CashReceiptVoucherPropertyContractBatches
                    .AsNoTracking()
                    .Where(p => p.PropertyContractBatchId.HasValue
                             && batchIds.Contains(p.PropertyContractBatchId.Value)
                             && p.Paid != 0)
                    .GroupBy(p => p.PropertyContractBatchId.Value)
                    .Select(g => new
                    {
                        PropertyContractBatchId = g.Key,
                        TotalPaid = g.Sum(x => x.Paid ?? 0),
                        IsFullyDelivered = g.Any(x => x.IsDelivered == true)
                    })
                    .ToList();
                // حساب آخر دفعة مستحقة
                var lastBatch = relevantBatches
                    .OrderByDescending(b => b.BatchDate)
                    .FirstOrDefault();

                int daysDifference = 0;
                decimal dailyRate = 0m;

                if (lastBatch != null && lastBatch.BatchDate.HasValue)
                {
                    daysDifference = (calculationEndDate.Date - lastBatch.BatchDate.Value.Date).Days;

                    if (lastBatch.BatchTotal.HasValue && lastBatch.BatchTotal.Value > 0)
                    {
                        dailyRate = lastBatch.BatchTotal.Value / 30m;
                    }
                }

                return new
                {
                    ContractId = a.Id,
                    a.UnifiedContractNumber,
                    a.ContractStartDate,
                    a.ContractEndDate,
                    CalculationEndDate = calculationEndDate,
                    UseContractEndDate = useContractEndDate,

                    ContractNo = a.DocumentNumber,

                    a.PropertyId,
                    Property = a.Property.ArName,

                    RenterId = a.PropertyRenterId,
                    Renter = a.PropertyRenter.ArName,
                    RenterMobile = a.PropertyRenter.Mobile,

                    UnitId = a.PropertyUnitId,
                    Unit = a.PropertyUnit != null ? a.PropertyUnit.ArName : "",

                    UnitCode = a.Property.PropertyDetails
                                        .Where(c => c.Id == a.PropertyUnitId)
                                        .Select(c => c.PropertyUnitNo)
                                        .FirstOrDefault(),

                    UnitTypeId = a.PropertyUnitTypeId,
                    UnitType = a.PropertyUnitType.ArName,

                    MergedUnits = a.Property.PropertyDetails
                        .Where(pd => a.PropertyContractMergedUnit
                                        .Select(mu => mu.PropertyUnitId)
                                        .Contains(pd.Id))
                        .Select(pd => new { pd.Id, pd.PropertyUnitNo })
                        .ToList(),

                    DaysDifference = daysDifference,
                    DailyRate = dailyRate,
                    IncreaseDays = daysDifference > 0 ? daysDifference : 0,
                    DecreaseDays = daysDifference < 0 ? Math.Abs(daysDifference) : 0,
                    IncreaseAmount = daysDifference > 0 ? (dailyRate * daysDifference) : 0,
                    DecreaseAmount = daysDifference < 0 ? (dailyRate * Math.Abs(daysDifference)) : 0,

                    // تفاصيل الدفعات مع حساب المتبقي الصحيح
                    Details = relevantBatches.Select(b =>
                    {
                        // جلب بيانات الدفع من القائمة اللي جبناها
                        var payment = payments.FirstOrDefault(p => p.PropertyContractBatchId == b.Id);

                        decimal totalPaid = payment?.TotalPaid ?? 0;
                        bool isFullyPaid = payment?.IsFullyDelivered == true;

                        // حساب القيم الأصلية
                        decimal batchRentOriginal = (b.BatchRentValue ?? 0) + (b.BatchRentValueTaxes ?? 0);
                        decimal batchWaterOriginal = (b.BatchWaterValue ?? 0) + (b.BatchWaterValueTaxes ?? 0);
                        decimal batchElectricityOriginal = (b.BatchElectricityValue ?? 0) + (b.BatchElectricityValueTaxes ?? 0);
                        decimal batchCommissionOriginal = (b.BatchCommissionValue ?? 0) + (b.BatchCommissionValueTaxes ?? 0);
                        decimal batchServicesOriginal = (b.BatchServicesValue ?? 0) + (b.BatchServicesValueTaxes ?? 0);
                        decimal batchInsuranceOriginal = (b.BatchInsuranceValue ?? 0) + (b.BatchInsuranceValueTaxes ?? 0);

                        decimal batchTotalOriginal = b.BatchTotal ?? 0;

                        // حساب المتبقي
                        decimal remain = batchTotalOriginal - totalPaid;
                        remain = remain < 0 ? 0 : remain; // لو في دفع زيادة نخليه صفر

                        return new
                        {
                            b.BatchNo,
                            b.BatchDate,
                            PropertyContractBatchId = b.Id,
                            PropertyContractId = b.MainDocId,

                            // القيم المتبقية (لو مسددة بالكامل = 0، لو جزئية = النسبة المتبقية)
                            BatchRentValue = isFullyPaid ? 0 :
                                           (remain > 0 ? (batchRentOriginal * remain / batchTotalOriginal) : 0),

                            BatchWaterValue = isFullyPaid ? 0 :
                                            (remain > 0 ? (batchWaterOriginal * remain / batchTotalOriginal) : 0),

                            BatchElectricityValue = isFullyPaid ? 0 :
                                                  (remain > 0 ? (batchElectricityOriginal * remain / batchTotalOriginal) : 0),

                            BatchCommissionValue = isFullyPaid ? 0 :
                                                 (remain > 0 ? (batchCommissionOriginal * remain / batchTotalOriginal) : 0),

                            BatchServicesValue = isFullyPaid ? 0 :
                                               (remain > 0 ? (batchServicesOriginal * remain / batchTotalOriginal) : 0),

                            BatchInsuranceValue = isFullyPaid ? 0 :
                                                (remain > 0 ? (batchInsuranceOriginal * remain / batchTotalOriginal) : 0),

                            // المتبقي الفعلي
                            Remain = remain,

                            // معلومات إضافية للتوضيح
                            OriginalTotal = batchTotalOriginal,
                            TotalPaid = totalPaid
                        };
                    }).ToList()
                };
            }).ToList();

            return Json(details, JsonRequestBehavior.AllowGet);
        }

        //public JsonResult GetPropertyContractTerminationDetails(string searchText)
        //{
        //    // 1) حماية: لو مافيش نص أو أقل من حرفين نرجّع فاضي
        //    if (string.IsNullOrWhiteSpace(searchText) || searchText.Trim().Length < 2)
        //        return Json(new object[0], JsonRequestBehavior.AllowGet);

        //    var q = searchText.Trim();

        //    // 2) الاستعلام مع AsNoTracking + ترتيب + حد أقصى للنتائج (يمكن تغييره)
        //    var details = db.PropertyContracts
        //        .AsNoTracking()
        //        .Where(a => a.IsDeleted == false && (
        //               a.DocumentNumber.Contains(q)
        //            || a.PropertyRenter.Mobile.Contains(q)
        //            || a.PropertyRenter.ArName.Contains(q)
        //            || a.PropertyRenter.EnName.Contains(q)
        //            || a.Property.Code.Contains(q)
        //            || a.Property.ArName.Contains(q)
        //            || a.Property.EnName.Contains(q)
        //            || a.PropertyUnitType.Code.Contains(q)
        //            || a.PropertyUnitType.ArName.Contains(q)
        //            || a.PropertyUnitType.EnName.Contains(q)
        //            // بحث برقم/كود الوحدة بدون ToString (بافتراض أن PropertyUnitNo نصّي)
        //            || a.Property.PropertyDetails.Any(pd => pd.IsDeleted == false
        //                                                    && pd.PropertyUnitNo.Contains(q))
        //        ))
        //        .OrderByDescending(a => a.Id)
        //        .Select(a => new
        //        {
        //            ContractId = a.Id,
        //            a.UnifiedContractNumber,
        //            a.ContractStartDate,
        //            a.ContractEndDate,

        //            ContractNo = a.DocumentNumber,

        //            a.PropertyId,
        //            Property = a.Property.ArName,

        //            RenterId = a.PropertyRenterId,
        //            Renter = a.PropertyRenter.ArName,
        //            RenterMobile = a.PropertyRenter.Mobile,

        //            UnitId = a.PropertyUnitId,
        //            Unit = a.PropertyUnit.ArName,

        //            // تجنب NullReference: اختَر النص ثم FirstOrDefault
        //            UnitCode = a.Property.PropertyDetails
        //                                .Where(c => c.Id == a.PropertyUnitId)
        //                                .Select(c => c.PropertyUnitNo)
        //                                .FirstOrDefault(),

        //            UnitTypeId = a.PropertyUnitTypeId,
        //            UnitType = a.PropertyUnitType.ArName,

        //            // الوحدات المدمجة
        //            MergedUnits = a.Property.PropertyDetails
        //                .Where(pd => a.PropertyContractMergedUnit
        //                                .Select(mu => mu.PropertyUnitId)
        //                                .Contains(pd.Id))
        //                .Select(pd => new { pd.Id, pd.PropertyUnitNo })
        //                .ToList(),

        //            // تفاصيل الدُفعات مع احترام IsDelivered
        //            Details = a.PropertyContractBatches.Select(b => new
        //            {
        //                b.BatchNo,
        //                b.BatchDate,
        //                PropertyContractBatchId = b.Id,
        //                PropertyContractId = b.MainDocId,

        //                BatchRentValue = b.CashReceiptVoucherPropertyContractBatches
        //                                            .Where(c => c.PropertyContractBatchId == b.Id)
        //                                            .Select(c => c.IsDelivered).FirstOrDefault() == true
        //                                            ? 0 : (b.BatchRentValue + b.BatchRentValueTaxes),

        //                BatchWaterValue = b.CashReceiptVoucherPropertyContractBatches
        //                                            .Where(c => c.PropertyContractBatchId == b.Id)
        //                                            .Select(c => c.IsDelivered).FirstOrDefault() == true
        //                                            ? 0 : (b.BatchWaterValue + b.BatchWaterValueTaxes),

        //                BatchElectricityValue = b.CashReceiptVoucherPropertyContractBatches
        //                                            .Where(c => c.PropertyContractBatchId == b.Id)
        //                                            .Select(c => c.IsDelivered).FirstOrDefault() == true
        //                                            ? 0 : (b.BatchElectricityValue + b.BatchElectricityValueTaxes),

        //                BatchCommissionValue = b.CashReceiptVoucherPropertyContractBatches
        //                                            .Where(c => c.PropertyContractBatchId == b.Id)
        //                                            .Select(c => c.IsDelivered).FirstOrDefault() == true
        //                                            ? 0 : (b.BatchCommissionValue + b.BatchCommissionValueTaxes),

        //                BatchServicesValue = b.CashReceiptVoucherPropertyContractBatches
        //                                            .Where(c => c.PropertyContractBatchId == b.Id)
        //                                            .Select(c => c.IsDelivered).FirstOrDefault() == true
        //                                            ? 0 : (b.BatchServicesValue + b.BatchServicesValueTaxes),

        //                BatchInsuranceValue = b.CashReceiptVoucherPropertyContractBatches
        //                                            .Where(c => c.PropertyContractBatchId == b.Id)
        //                                            .Select(c => c.IsDelivered).FirstOrDefault() == true
        //                                            ? 0 : (b.BatchInsuranceValue + b.BatchInsuranceValueTaxes),

        //                Remain = b.CashReceiptVoucherPropertyContractBatches
        //                            .Where(c => c.PropertyContractBatchId == b.Id)
        //                            .Select(c => c.Remain)
        //                            .FirstOrDefault() ?? b.BatchTotal
        //            }).ToList()
        //        })
        //        .Take(30)   // ⚠️ عدّلها حسب ما تحب، أو فعّل Paging
        //        .ToList();

        //    return Json(details, JsonRequestBehavior.AllowGet);
        //}

        [SkipERPAuthorize]
        public JsonResult GetPropertyComponentDetails(int? PropertyComponentId)
        {
            var Details = db.PropertyComponentDetails.Where(a => a.IsDeleted == false && a.PropertyComponentId == PropertyComponentId)
                .Select(a => new
                {
                    a.PropertyComponentId,
                    PropertyComponentDetailId = a.Id,
                    a.Price,
                    a.ArName,
                    a.SequenceNo,
                    a.Notes
                }).ToList();
            return Json(Details, JsonRequestBehavior.AllowGet);
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