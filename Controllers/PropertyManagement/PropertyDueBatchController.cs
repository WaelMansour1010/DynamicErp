using MyERP.Models;
using MyERP.Repository;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace MyERP.Controllers.PropertyManagement
{
    public class PropertyDueBatchController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: PropertyDueBatch
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة الدفعات المستحقة",
                EnAction = "Index",
                ControllerName = "PropertyDueBatch",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("PropertyDueBatch", "View", "Index", null, null, "الدفعات المستحقة");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<PropertyDueBatch> propertyDueBatches;

            if (string.IsNullOrEmpty(searchWord))
            {
                propertyDueBatches = db.PropertyDueBatches.Where(s => s.IsDeleted == false).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.PropertyDueBatches.Where(s => s.IsDeleted == false).Count();
            }
            else
            {
                propertyDueBatches = db.PropertyDueBatches.Where(s => s.IsDeleted == false && (s.DocumentNumber.Contains(searchWord))).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.PropertyDueBatches.Where(s => s.IsDeleted == false && (s.DocumentNumber.Contains(searchWord))).Count();
            }

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة الدفعات المستحقة",
                EnAction = "Index",
                ControllerName = "PropertyDueBatch",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            return View(propertyDueBatches.ToList());
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

            if (id == null)
            {
                ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", systemSetting.DefaultDepartmentId);
                ViewBag.VoucherDate = cTime.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.FromDate = cTime.ToString("yyyy-MM-dd");
                ViewBag.ToDate = cTime.ToString("yyyy-MM-dd");
                return View();
            }
            PropertyDueBatch dueBatch = db.PropertyDueBatches.Find(id);

            if (dueBatch == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل الدفعات المستحقة ",
                EnAction = "AddEdit",
                ControllerName = "PropertyDueBatch",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });

            ViewBag.Next = QueryHelper.Next((int)id, "PropertyDueBatch");
            ViewBag.Previous = QueryHelper.Previous((int)id, "PropertyDueBatch");
            ViewBag.Last = QueryHelper.GetLast("PropertyDueBatch");
            ViewBag.First = QueryHelper.GetFirst("PropertyDueBatch");

            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", dueBatch.DepartmentId);
            ViewBag.VoucherDate = dueBatch.VoucherDate != null ? dueBatch.VoucherDate.ToString("yyyy-MM-ddTHH:mm") : null;
            ViewBag.FromDate = dueBatch.FromDate != null ? dueBatch.FromDate.Value.ToString("yyyy-MM-dd") : null;
            ViewBag.ToDate = dueBatch.ToDate != null ? dueBatch.ToDate.Value.ToString("yyyy-MM-dd") : null;

            //-------------------- journal Entry --------------------//
            int sysPageId = QueryHelper.SourcePageId("PropertyDueBatch");
            JournalEntry journal = db.JournalEntries.FirstOrDefault(j => j.SourceId == id && j.SourcePageId == sysPageId);
            ViewBag.Journal = journal;
            ViewBag.JournalDocumentNumber = journal != null && journal.DocumentNumber != null ? journal.DocumentNumber.Replace("-", "") : "";
            //----------------------------------------------------------------------------//

            return View(dueBatch);
        }

        [HttpPost]
        public ActionResult AddEdit(PropertyDueBatch dueBatch)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            if (ModelState.IsValid)
            {
                var id = dueBatch.Id;
                dueBatch.IsDeleted = false;
                dueBatch.UserId = userId;
                if (dueBatch.Id > 0)
                {
                    //----------------------------------- **************************** ------------------------------------------//

                    MyXML.xPathName = "Details";
                    var PropertyDueBatchDetails = MyXML.GetXML(dueBatch.PropertyDueBatchDetails);
                    db.PropertyDueBatch_Update(dueBatch.Id, dueBatch.DocumentNumber, dueBatch.DepartmentId, dueBatch.VoucherDate, dueBatch.FromDate, dueBatch.ToDate, dueBatch.IsDeleted, dueBatch.UserId, dueBatch.Notes, dueBatch.Image, PropertyDueBatchDetails);
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("PropertyDueBatch", "Edit", "AddEdit", dueBatch.Id, null, "الدفعات المستحقة");
                }
                else
                {
                    MyXML.xPathName = "Details";
                    var PropertyDueBatchDetails = MyXML.GetXML(dueBatch.PropertyDueBatchDetails);
                    var idResult = new ObjectParameter("Id", typeof(Int32));
                    db.PropertyDueBatch_Insert(idResult, dueBatch.DepartmentId, dueBatch.VoucherDate, dueBatch.FromDate, dueBatch.ToDate, dueBatch.IsDeleted, dueBatch.UserId, dueBatch.Notes, dueBatch.Image, PropertyDueBatchDetails);
                    id = (int)idResult.Value;
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("PropertyDueBatch", "Add", "AddEdit", id, null, "الدفعات المستحقة");
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = dueBatch.Id > 0 ? "تعديل الدفعات المستحقة" : "اضافة الدفعات المستحقة",
                    EnAction = "AddEdit",
                    ControllerName = "PropertyDueBatch",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "Post",
                    SelectedItem = id,
                });

                // إرسال رسائل SMS للمستأجرين عند إنشاء دفعات جديدة
                if (dueBatch.Id == 0)
                {
                    Task.Run(async () => await SendDueBatchSmsNotifications(id));
                }

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
                PropertyDueBatch dueBatch = db.PropertyDueBatches.Find(id);
                dueBatch.IsDeleted = true;
                dueBatch.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                foreach (var item in dueBatch.PropertyDueBatchDetails)
                {
                    item.IsDeleted = true; 
                    item.IsSelected = false;
                    var ContractBatch = db.PropertyContractBatches.Where(a => a.Id == item.PropertyContractBatchId).FirstOrDefault();
                  //  ContractBatch.IsDeleted = true;
                    ContractBatch.IsRegisteredAsDue = false;
                }
               


                db.Entry(dueBatch).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف الدفعات المستحقة",
                    EnAction = "AddEdit",
                    ControllerName = "PropertyDueBatch",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                });
                Notification.GetNotification("PropertyDueBatch", "Delete", "Delete", id, null, "الدفعات المستحقة");
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
            var lastObj = db.PropertyDueBatches.Where(a => a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.PropertyDueBatches.Where(a => a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Month == VoucherDate.Value.Month && a.VoucherDate.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.PropertyDueBatches.Where(a => a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

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
        public JsonResult GetPropertyDueBatchDetails(DateTime? FromDate, DateTime? ToDate)
        {
            var Details = db.GetPropertyDueBatchDetails(FromDate, ToDate).ToList();
            return Json(Details, JsonRequestBehavior.AllowGet);
        }
        /// <summary>
        /// إرسال رسائل SMS للمستأجرين عند إنشاء دفعات مستحقة جديدة
        /// </summary>
        /// <param name="dueBatchId">معرف الدفعة المستحقة</param>
        private async Task SendDueBatchSmsNotifications(int dueBatchId)
        {
            try
            {
                using (var dbContext = new MySoftERPEntity())
                {
                    // جلب تفاصيل الدفعات المستحقة مع بيانات المستأجرين
                    var dueBatchDetails = dbContext.PropertyDueBatchDetails
                        .Where(d => d.MainDocId == dueBatchId && d.IsDeleted == false)
                        .Include(d => d.PropertyContractBatch)
                        .Include(d => d.PropertyContract)
                        .Include(d => d.PropertyContract.PropertyRenter)
                        .ToList();

                    // فحص الرصيد قبل الإرسال
                    var creditsResult = await SmsService.CheckCredits();
                    if (creditsResult.Success && creditsResult.IsLowBalance)
                    {
                        // إرسال تنبيه للمدير عند انخفاض الرصيد
                        var adminPhone = ConfigurationManager.AppSettings["OursmsAdminPhone"];
                        if (!string.IsNullOrEmpty(adminPhone))
                        {
                            await SmsService.SendLowBalanceAlert(adminPhone, creditsResult.Credits);
                        }
                    }

                    foreach (var detail in dueBatchDetails)
                    {
                        if (detail.PropertyContract?.PropertyRenter != null)
                        {
                            var renter = detail.PropertyContract.PropertyRenter;
                            var batch = detail.PropertyContractBatch;

                            // الحصول على رقم الهاتف (نفضل Mobile على Phone)
                            var phone = !string.IsNullOrEmpty(renter.Mobile) ? renter.Mobile : renter.Phone;

                            if (!string.IsNullOrEmpty(phone) && batch != null)
                            {
                                var renterName = !string.IsNullOrEmpty(renter.ArName) ? renter.ArName : renter.EnName;
                                var amount = batch.BatchTotal ?? 0;
                                var dueDate = batch.BatchDate ?? DateTime.Now;

                                // إنشاء الرسالة
                                var message = SmsService.CreateDueBatchMessage(renterName, amount, dueDate);

                                // إرسال الرسالة
                                var result = await SmsService.SendSms(phone, message);

                                // تسجيل نتيجة الإرسال
                                System.Diagnostics.Debug.WriteLine($"SMS to {phone}: {(result.Success ? "Success" : "Failed")} - {result.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending SMS notifications: {ex.Message}");
            }
        }

        /// <summary>
        /// فحص رصيد الرسائل النصية (API للواجهة الأمامية)
        /// </summary>
        [HttpGet]
        [SkipERPAuthorize]
        public async Task<JsonResult> CheckSmsCredits()
        {
            var result = await SmsService.CheckCredits();
            return Json(new
            {
                success = result.Success,
                credits = result.Credits,
                isLowBalance = result.IsLowBalance,
                message = result.Message
            }, JsonRequestBehavior.AllowGet);
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