using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using System.Web.Script.Serialization;
using MyERP.Repository;
using System.Data.Entity.Core.Objects;

namespace MyERP.Controllers.HotelManagement
{
    public class MealReservationController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: MealReservation
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة حجز الوجبات",
                EnAction = "Index",
                ControllerName = "MealReservation",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("MealReservation", "View", "Index", null, null, "حجز الوجبات");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<MealReservation> mealReservations;

            if (string.IsNullOrEmpty(searchWord))
            {
                mealReservations = db.MealReservations.Where(s => s.IsDeleted == false ).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.MealReservations.Where(s => s.IsDeleted == false ).Count();
            }
            else
            {
                mealReservations = db.MealReservations.Where(s => s.IsDeleted == false  && (s.DocumentNumber.Contains(searchWord) || s.Customer.ArName.Contains(searchWord))).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.MealReservations.Where(s => s.IsDeleted == false && (s.DocumentNumber.Contains(searchWord) || s.Customer.ArName.Contains(searchWord))).Count();
            }

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة حجز الوجبات",
                EnAction = "Index",
                ControllerName = "MealReservation",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            return View(mealReservations.ToList());
        }
        public ActionResult AddEdit(int? id)
        {
            ViewBag.ItemId = new SelectList(db.Items.Where(x => x.IsActive==true && x.IsDeleted==false).Select(x => new {
                Id = x.Id,
                ArName =x.Code+" - "+ x.ArName
            }), "Id", "ArName");
            ViewBag.MealId = new SelectList(new List<dynamic> {
                    new { Id=1, ArName="فطار"},
                    new { Id=2, ArName="غداء"},
                    new { Id=3, ArName="عشاء"} }
                    , "Id", "ArName");
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            var departments = departmentRepository.UserDepartments(1).ToList();//show all deprartments so that user can select factory department
            CashboxReposistory cashboxReposistory = new CashboxReposistory(db);
            SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            if (id == null)
            {
                ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", systemSetting.DefaultDepartmentId);
                ViewBag.CashBoxId = new SelectList(cashboxReposistory.UserCashboxes(userId, systemSetting.DefaultDepartmentId).ToList(), "Id", "ArName", systemSetting.DefaultCashBoxId);

                ViewBag.CustomerId = new SelectList(db.Customers.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
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

            //--------- JournalEntry --------------//
            int sysPageId = QueryHelper.SourcePageId("MealReservation");
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
            MealReservation mealReservation = db.MealReservations.Find(id);
            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", mealReservation.DepartmentId);
            ViewBag.CashBoxId = new SelectList(cashboxReposistory.UserCashboxes(userId, mealReservation.DepartmentId).ToList(), "Id", "ArName", mealReservation.CashBoxId);

            ViewBag.CustomerId = new SelectList(db.Customers.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", mealReservation.CustomerId);

            if (mealReservation == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل حجز الوجبات ",
                EnAction = "AddEdit",
                ControllerName = "MealReservation",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });

            ViewBag.VoucherDate = mealReservation.VoucherDate != null ? mealReservation.VoucherDate.Value.ToString("yyyy-MM-ddTHH:mm") : null;

            ViewBag.Next = QueryHelper.Next((int)id, "MealReservation");
            ViewBag.Previous = QueryHelper.Previous((int)id, "MealReservation");
            ViewBag.Last = QueryHelper.GetLast("MealReservation");
            ViewBag.First = QueryHelper.GetFirst("MealReservation");
            return View(mealReservation);
        }

        [HttpPost]
        public ActionResult AddEdit(MealReservation mealReservation)
        {
            if (ModelState.IsValid)
            {
                var id = mealReservation.Id;
                mealReservation.IsDeleted = false;
                mealReservation.IsActive = true;
                mealReservation.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                if (mealReservation.Id > 0)
                {
                    //db.MealReservationDetails.RemoveRange(db.MealReservationDetails.Where(x => x.MainDocId == mealReservation.Id));
                    //var mealReservationDetails = mealReservation.MealReservationDetails.ToList();
                    //mealReservationDetails.ForEach((x) => x.MainDocId = mealReservation.Id);
                    //mealReservation.MealReservationDetails = null;
                    //db.Entry(mealReservation).State = EntityState.Modified;
                    //db.MealReservationDetails.AddRange(mealReservationDetails);
                    MyXML.xPathName = "Details";
                    var MealReservationDetails = MyXML.GetXML(mealReservation.MealReservationDetails);
                    db.MealReservation_Update(mealReservation.Id, mealReservation.DocumentNumber,mealReservation.CustomerId,mealReservation.VoucherDate,mealReservation.Notes,mealReservation.Image,mealReservation.UserId,mealReservation.IsDeleted,mealReservation.Total,mealReservation.Discount,mealReservation.TotalAfterDiscount,mealReservation.CurrencyId,mealReservation.CurrencyEquivalent,mealReservation.DepartmentId,mealReservation.CashBoxId,mealReservation.Paid,mealReservation.Remain,MealReservationDetails);
                    Notification.GetNotification("MealReservation", "Edit", "AddEdit", mealReservation.Id, null, "حجز الوجبات");
                }
                else
                {
                    mealReservation.DocumentNumber = new JavaScriptSerializer().Serialize(SetDocNum(mealReservation.DepartmentId, mealReservation.VoucherDate).Data).ToString().Trim('"');
                    //db.MealReservations.Add(mealReservation);
                    MyXML.xPathName = "Details";
                    var MealReservationDetails = MyXML.GetXML(mealReservation.MealReservationDetails);
                    var idResult = new ObjectParameter("Id", typeof(Int32));
                    db.MealReservation_Insert(idResult,mealReservation.CustomerId,mealReservation.VoucherDate,mealReservation.Notes,mealReservation.Image,mealReservation.UserId,mealReservation.IsDeleted,mealReservation.Total,mealReservation.Discount,mealReservation.TotalAfterDiscount,mealReservation.CurrencyId,mealReservation.CurrencyEquivalent,mealReservation.DepartmentId,mealReservation.CashBoxId,mealReservation.Paid,mealReservation.Remain,MealReservationDetails);
                    id = (int)idResult.Value;
                    Notification.GetNotification("MealReservation", "Add", "AddEdit", mealReservation.Id, null, "حجز الوجبات");
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

                //    return View(mealReservation);
                //}
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل حجز الوجبات" : "اضافة حجز الوجبات",
                    EnAction = "AddEdit",
                    ControllerName = "MealReservation",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = mealReservation.DocumentNumber
                });
                return Json(new { success = true, id });

            }
            return Json(new { success = false });
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult Delete(int id)
        {
            try
            {
                //MealReservation mealReservation = db.MealReservations.Find(id);
                //mealReservation.IsDeleted = true;
                //mealReservation.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                //db.Entry(mealReservation).State = EntityState.Modified;
                //db.SaveChanges();

                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.MealReservation_Delete(id, userId);

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف حجز الوجبات",
                    EnAction = "AddEdit",
                    ControllerName = "MealReservation",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("MealReservation", "Delete", "Delete", id, null, "حجز الوجبات");


                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

        }

        [SkipERPAuthorize]
        public JsonResult SetDocNum(int? id, DateTime? VoucherDate)
        {
            bool IsExistInDocumentsCoding = false;
            var noOfDigits = (int?)0;
            var YearFormat = (int?)0;
            var CodingTypeId = (int?)0;
            var IsZerosFills = (bool?)null;
            var newDocNo = "";
            var GeneratedDocNo = "";
            var lastObj = db.MealReservations.Where(a => a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.MealReservations.Where(a => a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Value.Month == VoucherDate.Value.Month && a.VoucherDate.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.MealReservations.Where(a => a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

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
            //double DocNo = 0;
            //var code = db.Database.SqlQuery<string>($"select top(1)[DocumentNumber] from [MealReservation] order by [Id] desc");
            //if (code.FirstOrDefault() == null)
            //{
            //    DocNo = 0;
            //}
            //else
            //{
            //    DocNo = double.Parse(code.FirstOrDefault().ToString());
            //}
            //return Json(DocNo + 1, JsonRequestBehavior.AllowGet);
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