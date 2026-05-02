using MyERP.Models;
using MyERP.Repository;
using System;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace MyERP.Controllers.HotelManagement
{
    public class DayUseReservationController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: DayUseReservation
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة حجز داي يوز",
                EnAction = "Index",
                ControllerName = "DayUseReservation",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("DayUseReservation", "View", "Index", null, null, "حجز داي يوز");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<DayUseReservation> dayUseReservations;

            if (string.IsNullOrEmpty(searchWord))
            {
                dayUseReservations = db.DayUseReservations.Where(s => s.IsDeleted == false).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.DayUseReservations.Where(s => s.IsDeleted == false).Count();
            }
            else
            {
                dayUseReservations = db.DayUseReservations.Where(s => s.IsDeleted == false && (s.DocumentNumber.Contains(searchWord) || s.Customer.ArName.Contains(searchWord))).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.DayUseReservations.Where(s => s.IsDeleted == false && (s.DocumentNumber.Contains(searchWord) || s.Customer.ArName.Contains(searchWord))).Count();
            }

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة حجز داي يوز",
                EnAction = "Index",
                ControllerName = "DayUseReservation",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            return View(dayUseReservations.ToList());
        }
        public ActionResult AddEdit(int? id)
        {
            ViewBag.GroupId = new SelectList(db.CustomersGroups.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            var departments =  departmentRepository.UserDepartments(1).ToList();//show all deprartments so that user can select factory department
            CashboxReposistory cashboxReposistory = new CashboxReposistory(db);
            SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            if (id == null)
            {
                ViewBag.CustomerId = new SelectList(db.Customers.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", systemSetting.DefaultDepartmentId);
                ViewBag.CashBoxId = new SelectList(cashboxReposistory.UserCashboxes(userId, systemSetting.DefaultDepartmentId).ToList(), "Id", "ArName", systemSetting.DefaultCashBoxId);

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
                ViewBag.RegistrationDate = cTime.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.VoucherDate = cTime.ToString("yyyy-MM-ddTHH:mm");
                return View();
            }
            //--------- JournalEntry --------------//
            int sysPageId = QueryHelper.SourcePageId("DayUseReservation");
            ViewBag.JE = db.GetJournalEntryBySourceIdAndSourcePageId(id, sysPageId).FirstOrDefault();
            if (ViewBag.JE != null)
            {
                var JEDocNo = ViewBag.JE;
                ViewBag.JE.DocumentNumber = JEDocNo != null ? JEDocNo.DocumentNumber.Replace("-", "") : "";
                int JEId = ViewBag.JE.Id;
                int sourcePageId = ViewBag.JE.SourcePageId;
                ViewBag.JEDetails = db.JournalEntryDetails.Where(a => a.SourceId == id && a.JournalEntryId == JEId && a.SourcePageId == sourcePageId).OrderBy(a => a.Id).ToList();
            }
            //--------------------------------------------------------------------------------------------//

            DayUseReservation dayUseReservation = db.DayUseReservations.Find(id);
            ViewBag.CustomerId = new SelectList(db.Customers.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", dayUseReservation.CustomerId);
            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", dayUseReservation.DepartmentId);
            ViewBag.CashBoxId = new SelectList(cashboxReposistory.UserCashboxes(userId, dayUseReservation.DepartmentId).ToList(), "Id", "ArName", dayUseReservation.CashBoxId);

            if (dayUseReservation == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل حجز داي يوز ",
                EnAction = "AddEdit",
                ControllerName = "DayUseReservation",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });

            ViewBag.RegistrationDate = dayUseReservation.RegistrationDate != null ? dayUseReservation.RegistrationDate.Value.ToString("yyyy-MM-ddTHH:mm") : null;
            ViewBag.VoucherDate = dayUseReservation.VoucherDate != null ? dayUseReservation.VoucherDate.ToString("yyyy-MM-ddTHH:mm") : null;

            ViewBag.Next = QueryHelper.Next((int)id, "DayUseReservation");
            ViewBag.Previous = QueryHelper.Previous((int)id, "DayUseReservation");
            ViewBag.Last = QueryHelper.GetLast("DayUseReservation");
            ViewBag.First = QueryHelper.GetFirst("DayUseReservation");
            return View(dayUseReservation);
        }

        [HttpPost]
        public ActionResult AddEdit(DayUseReservation dayUseReservation)
        {
            if (ModelState.IsValid)
            {
                var id = dayUseReservation.Id;
                dayUseReservation.IsDeleted = false;
                dayUseReservation.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                if (dayUseReservation.Id > 0)
                {
                    // db.Entry(dayUseReservation).State = EntityState.Modified;
                    db.DayUseReservation_Update(dayUseReservation.Id, dayUseReservation.DocumentNumber,dayUseReservation.VoucherDate,dayUseReservation.RegistrationDate,dayUseReservation.CustomerId,dayUseReservation.PeopleNumber,dayUseReservation.Amount,dayUseReservation.Total,dayUseReservation.Discount,dayUseReservation.TotalAfterDiscount,dayUseReservation.UserId,dayUseReservation.IsDeleted,dayUseReservation.Notes,dayUseReservation.Image,dayUseReservation.DepartmentId,dayUseReservation.CashBoxId,dayUseReservation.Paid,dayUseReservation.Remain,dayUseReservation.CurrencyId,dayUseReservation.CurrencyEquivalent);


                    Notification.GetNotification("DayUseReservation", "Edit", "AddEdit", dayUseReservation.Id, null, "حجز داي يوز");
                }
                else
                {
                    dayUseReservation.DocumentNumber = new JavaScriptSerializer().Serialize(SetDocNum(dayUseReservation.DepartmentId,dayUseReservation.VoucherDate).Data).ToString().Trim('"');
                    var idResult = new ObjectParameter("Id", typeof(Int32));
                    db.DayUseReservation_Insert(idResult,dayUseReservation.VoucherDate,dayUseReservation.RegistrationDate,dayUseReservation.CustomerId,dayUseReservation.PeopleNumber,dayUseReservation.Amount,dayUseReservation.Total,dayUseReservation.Discount,dayUseReservation.TotalAfterDiscount,dayUseReservation.UserId,dayUseReservation.IsDeleted,dayUseReservation.Notes,dayUseReservation.Image,dayUseReservation.DepartmentId,dayUseReservation.CashBoxId,dayUseReservation.Paid,dayUseReservation.Remain,dayUseReservation.CurrencyId,dayUseReservation.CurrencyEquivalent
                        );
                    id = (int)idResult.Value;
                    //db.DayUseReservations.Add(dayUseReservation);
                    Notification.GetNotification("DayUseReservation", "Add", "AddEdit", dayUseReservation.Id, null, "حجز داي يوز");
                }
                //try
                //{
                //    db.SaveChanges();
                //}
                //catch (Exception ex)
                //{
                //    var errors = ModelState
                //    .Where(x => x.Value.Errors.Count > 0)
                //    .Select(x => new { x.Key, x.Value.Errors })
                //    .ToArray();

                //    return View(dayUseReservation);
                //}

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل حجز داي يوز" : "اضافة حجز داي يوز",
                    EnAction = "AddEdit",
                    ControllerName = "DayUseReservation",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = dayUseReservation.DocumentNumber
                });
                return Json(new { success = true, id });

            }
            var ValidationErrors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();
            return Json(new { success = false });
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult Delete(int id)
        {
            try
            {
                //DayUseReservation dayUseReservation = db.DayUseReservations.Find(id);
                //dayUseReservation.IsDeleted = true;
                //dayUseReservation.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                //db.Entry(dayUseReservation).State = EntityState.Modified;
                //db.SaveChanges();

                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.DayUseReservation_Delete(id, userId);

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف حجز داي يوز",
                    EnAction = "AddEdit",
                    ControllerName = "DayUseReservation",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("DayUseReservation", "Delete", "Delete", id, null, "حجز داي يوز");
                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

        }

        [SkipERPAuthorize]
        public JsonResult SetDocNum(int?id, DateTime? VoucherDate)
        {
            bool IsExistInDocumentsCoding = false;
            var noOfDigits = (int?)0;
            var YearFormat = (int?)0;
            var CodingTypeId = (int?)0;
            var IsZerosFills = (bool?)null;
            var newDocNo = "";
            var GeneratedDocNo = "";
            var lastObj = db.DayUseReservations.Where(a => a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.DayUseReservations.Where(a => a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Month == VoucherDate.Value.Month && a.VoucherDate.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.DayUseReservations.Where(a => a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

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
            //var docNo = QueryHelper.DocLastNum(id, "DayUseReservation");
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