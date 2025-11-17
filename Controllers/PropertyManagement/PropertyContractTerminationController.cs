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
                    ////----------------------------------- **************************** ------------------------------------------//
                    MyXML.xPathName = "Details";
                    var PropertyContractTerminationDetails = MyXML.GetXML(contractTermination.PropertyContractTerminationDetails);
                    MyXML.xPathName = "Damages";
                    var PropertyContractTerminationDamages = MyXML.GetXML(contractTermination.PropertyContractTerminationDamages);
                    db.PropertyContractTerminate_Update(contractTermination.Id, contractTermination.DocumentNumber,contractTermination.DepartmentId, contractTermination.VoucherDate, contractTermination.PropertyContractId, contractTermination.PropertyId, contractTermination.PropertyUnitTypeId, contractTermination.PropertyRenterId, contractTermination.TerminationDate, contractTermination.LastBatchDate, contractTermination.IsLastBatchCalculation, contractTermination.IsDocumented, contractTermination.IncreaseDayValue, contractTermination.IncreaseDaysNo, contractTermination.IncreaseDaysTotalValue, contractTermination.DecreaseDayValue, contractTermination.DecreaseDaysNo, contractTermination.DecreaseDaysTotalValue, contractTermination.IsDeleted, contractTermination.UserId, contractTermination.Notes, contractTermination.Image, PropertyContractTerminationDetails, PropertyContractTerminationDamages);
                    
                    db.SaveChanges();
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("PropertyContractTermination", "Edit", "AddEdit", contractTermination.Id, null, "تصفية العقد");
                }
                else
                {
                    MyXML.xPathName = "Details";
                    var PropertyContractTerminationDetails = MyXML.GetXML(contractTermination.PropertyContractTerminationDetails);
                    MyXML.xPathName = "Damages";
                    var PropertyContractTerminationDamages = MyXML.GetXML(contractTermination.PropertyContractTerminationDamages);
                    var idResult = new ObjectParameter("Id", typeof(Int32));
                    db.PropertyContractTerminate_Insert(idResult, contractTermination.DepartmentId, contractTermination.VoucherDate, contractTermination.PropertyContractId, contractTermination.PropertyId, contractTermination.PropertyUnitTypeId, contractTermination.PropertyRenterId, contractTermination.TerminationDate, contractTermination.LastBatchDate, contractTermination.IsLastBatchCalculation, contractTermination.IsDocumented, contractTermination.IncreaseDayValue, contractTermination.IncreaseDaysNo, contractTermination.IncreaseDaysTotalValue, contractTermination.DecreaseDayValue, contractTermination.DecreaseDaysNo, contractTermination.DecreaseDaysTotalValue, contractTermination.IsDeleted, contractTermination.UserId, contractTermination.Notes, contractTermination.Image, PropertyContractTerminationDetails, PropertyContractTerminationDamages);
                    
                    db.SaveChanges();
                    //////////
                    id = (int)idResult.Value;
                    //////-------------------- Notification-------------------------////
                    Notification.GetNotification("PropertyContractTermination", "Add", "AddEdit", id, null, "تصفية العقد");
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
        
        public JsonResult GetPropertyContractTerminationDetails(string searchText)
        {
            // 1) حماية: لو مافيش نص أو أقل من حرفين نرجّع فاضي
            if (string.IsNullOrWhiteSpace(searchText) || searchText.Trim().Length < 2)
                return Json(new object[0], JsonRequestBehavior.AllowGet);

            var q = searchText.Trim();

            // 2) الاستعلام مع AsNoTracking + ترتيب + حد أقصى للنتائج (يمكن تغييره)
            var details = db.PropertyContracts
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
                    // بحث برقم/كود الوحدة بدون ToString (بافتراض أن PropertyUnitNo نصّي)
                    || a.Property.PropertyDetails.Any(pd => pd.IsDeleted == false
                                                            && pd.PropertyUnitNo.Contains(q))
                ))
                .OrderByDescending(a => a.Id)
                .Select(a => new
                {
                    ContractId = a.Id,
                    a.UnifiedContractNumber,
                    a.ContractStartDate,
                    a.ContractEndDate,

                    ContractNo = a.DocumentNumber,

                    a.PropertyId,
                    Property = a.Property.ArName,

                    RenterId = a.PropertyRenterId,
                    Renter = a.PropertyRenter.ArName,
                    RenterMobile = a.PropertyRenter.Mobile,

                    UnitId = a.PropertyUnitId,
                    Unit = a.PropertyUnit.ArName,

                    // تجنب NullReference: اختَر النص ثم FirstOrDefault
                    UnitCode = a.Property.PropertyDetails
                                        .Where(c => c.Id == a.PropertyUnitId)
                                        .Select(c => c.PropertyUnitNo)
                                        .FirstOrDefault(),

                    UnitTypeId = a.PropertyUnitTypeId,
                    UnitType = a.PropertyUnitType.ArName,

                    // الوحدات المدمجة
                    MergedUnits = a.Property.PropertyDetails
                        .Where(pd => a.PropertyContractMergedUnit
                                        .Select(mu => mu.PropertyUnitId)
                                        .Contains(pd.Id))
                        .Select(pd => new { pd.Id, pd.PropertyUnitNo })
                        .ToList(),

                    // تفاصيل الدُفعات مع احترام IsDelivered
                    Details = a.PropertyContractBatches.Select(b => new
                    {
                        b.BatchNo,
                        b.BatchDate,
                        PropertyContractBatchId = b.Id,
                        PropertyContractId = b.MainDocId,

                        BatchRentValue = b.CashReceiptVoucherPropertyContractBatches
                                                    .Where(c => c.PropertyContractBatchId == b.Id)
                                                    .Select(c => c.IsDelivered).FirstOrDefault() == true
                                                    ? 0 : (b.BatchRentValue + b.BatchRentValueTaxes),

                        BatchWaterValue = b.CashReceiptVoucherPropertyContractBatches
                                                    .Where(c => c.PropertyContractBatchId == b.Id)
                                                    .Select(c => c.IsDelivered).FirstOrDefault() == true
                                                    ? 0 : (b.BatchWaterValue + b.BatchWaterValueTaxes),

                        BatchElectricityValue = b.CashReceiptVoucherPropertyContractBatches
                                                    .Where(c => c.PropertyContractBatchId == b.Id)
                                                    .Select(c => c.IsDelivered).FirstOrDefault() == true
                                                    ? 0 : (b.BatchElectricityValue + b.BatchElectricityValueTaxes),

                        BatchCommissionValue = b.CashReceiptVoucherPropertyContractBatches
                                                    .Where(c => c.PropertyContractBatchId == b.Id)
                                                    .Select(c => c.IsDelivered).FirstOrDefault() == true
                                                    ? 0 : (b.BatchCommissionValue + b.BatchCommissionValueTaxes),

                        BatchServicesValue = b.CashReceiptVoucherPropertyContractBatches
                                                    .Where(c => c.PropertyContractBatchId == b.Id)
                                                    .Select(c => c.IsDelivered).FirstOrDefault() == true
                                                    ? 0 : (b.BatchServicesValue + b.BatchServicesValueTaxes),

                        BatchInsuranceValue = b.CashReceiptVoucherPropertyContractBatches
                                                    .Where(c => c.PropertyContractBatchId == b.Id)
                                                    .Select(c => c.IsDelivered).FirstOrDefault() == true
                                                    ? 0 : (b.BatchInsuranceValue + b.BatchInsuranceValueTaxes),

                        Remain = b.CashReceiptVoucherPropertyContractBatches
                                    .Where(c => c.PropertyContractBatchId == b.Id)
                                    .Select(c => c.Remain)
                                    .FirstOrDefault() ?? b.BatchTotal
                    }).ToList()
                })
                .Take(30)   // ⚠️ عدّلها حسب ما تحب، أو فعّل Paging
                .ToList();

            return Json(details, JsonRequestBehavior.AllowGet);
        }

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