using MyERP.Models;
using MyERP.Repository;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace MyERP.Controllers.HotelManagement
{
    public class RoomBookingController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: RoomBooking
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة حجز الغرف",
                EnAction = "Index",
                ControllerName = "RoomBooking",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("RoomBooking", "View", "Index", null, null, "حجز الغرف");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<RoomBooking> roomBookings;

            if (string.IsNullOrEmpty(searchWord))
            {
                roomBookings = db.RoomBookings.Where(s => s.IsDeleted == false).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.RoomBookings.Where(s => s.IsDeleted == false).Count();
            }
            else
            {
                roomBookings = db.RoomBookings.Where(s => s.IsDeleted == false && (s.DocumentNumber.Contains(searchWord) || s.Customer.ArName.Contains(searchWord))).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.RoomBookings.Where(s => s.IsDeleted == false && (s.DocumentNumber.Contains(searchWord) || s.Customer.ArName.Contains(searchWord))).Count();
            }

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة حجز الغرف",
                EnAction = "Index",
                ControllerName = "RoomBooking",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            return View(roomBookings.ToList());
        }
        public ActionResult AddEdit(int? id)
        {
            ViewBag.GroupId = new SelectList(db.CustomersGroups.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            var departments = departmentRepository.UserDepartments(1).ToList();//show all deprartments so that user can select factory department
            CashboxReposistory cashboxReposistory = new CashboxReposistory(db);
            SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            if (id == null)
            {
                ViewBag.RoomId = new SelectList(db.Rooms.Where(a => a.IsActive == true && a.IsDeleted == false && (a.RoomStatusId == 1 || a.RoomStatusId == 3)).Select(b => new
                {
                    Id = b.Id,
                    RoomNumber = b.Code + " - " + b.RoomNumber
                }), "Id", "RoomNumber");
                ViewBag.CustomerId = new SelectList(db.Customers.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.BuildingId = new SelectList(db.Buildings.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                // الفرعية فقط
                ViewBag.RoomTypeId = new SelectList(db.RoomTypes.Where(a => a.IsActive == true && a.IsDeleted == false && a.TypeId == 3).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                //كاش و فيزا فقط
                ViewBag.PaymentMethodId = new SelectList(db.PaymentMethods.Where(a => a.IsActive == true && a.IsDeleted == false && (a.Id == 1 || a.Id == 4)).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.ArName
                }), "Id", "ArName");
                ViewBag.BookingStatusId = new SelectList(new List<dynamic> {
                    new { Id=1, ArName="فى انتظار التأكيد "},
                    new { Id=2, ArName="تم دفع عربون"},
                    new { Id=3, ArName="تم التسكين"},
                    new { Id=4, ArName="تم التأجيل"},
                    new { Id=4, ArName="تمت المغادرة"}}, "Id", "ArName");
                ViewBag.BookingEmployeeId = new SelectList(db.Employees.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.ArName
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
                ViewBag.BookingStartDate = cTime.ToString("yyyy-MM-dd");
                ViewBag.BookingEndDate = cTime.ToString("yyyy-MM-dd");
                return View();
            }
            //--------- JournalEntry --------------//
            int sysPageId = QueryHelper.SourcePageId("RoomBooking");
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
            RoomBooking roomBooking = db.RoomBookings.Find(id);

            ViewBag.BookingStatusId = new SelectList(new List<dynamic> {
                    new { Id=1, ArName="فى انتظار التأكيد "},
                    new { Id=2, ArName="تم دفع عربون"},
                    new { Id=3, ArName="تم التسكين"},
                    new { Id=4, ArName="تم التأجيل"},
                    new { Id=4, ArName="تمت المغادرة"}}, "Id", "ArName", roomBooking.BookingStatusId);

            ViewBag.RoomId = new SelectList(db.Rooms.Where(a => a.IsActive == true && a.IsDeleted == false && a.BuildingId == roomBooking.BuildingId/* && (a.RoomStatusId == 1 || a.RoomStatusId == 3)*/).Select(b => new
            {
                Id = b.Id,
                RoomNumber = b.Code + " - " + b.RoomNumber
            }), "Id", "RoomNumber", roomBooking.RoomId);

            ViewBag.CustomerId = new SelectList(db.Customers.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", roomBooking.CustomerId);
            ViewBag.BuildingId = new SelectList(db.Buildings.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", roomBooking.BuildingId);
            // الفرعية فقط
            ViewBag.RoomTypeId = new SelectList(db.RoomTypes.Where(a => a.IsActive == true && a.IsDeleted == false && a.TypeId == 3).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", roomBooking.RoomTypeId);
            //كاش و فيزا فقط
            ViewBag.PaymentMethodId = new SelectList(db.PaymentMethods.Where(a => a.IsActive == true && a.IsDeleted == false && (a.Id == 1 || a.Id == 4)).Select(b => new
            {
                Id = b.Id,
                ArName = b.ArName
            }), "Id", "ArName");
            ViewBag.BookingEmployeeId = new SelectList(db.Employees.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.ArName
            }), "Id", "ArName", roomBooking.BookingEmployeeId);
            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", roomBooking.DepartmentId);
            ViewBag.CashBoxId = new SelectList(cashboxReposistory.UserCashboxes(userId, roomBooking.DepartmentId).ToList(), "Id", "ArName", roomBooking.CashBoxId);

            if (roomBooking == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل حجز الغرف ",
                EnAction = "AddEdit",
                ControllerName = "RoomBooking",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });

            ViewBag.RegistrationDate = roomBooking.RegistrationDate != null ? roomBooking.RegistrationDate.Value.ToString("yyyy-MM-ddTHH:mm") : null;
            ViewBag.BookingStartDate = roomBooking.BookingStartDate != null ? roomBooking.BookingStartDate.Value.ToString("yyyy-MM-dd") : null;
            ViewBag.BookingEndDate = roomBooking.BookingEndDate != null ? roomBooking.BookingEndDate.Value.ToString("yyyy-MM-dd") : null;

            ViewBag.Next = QueryHelper.Next((int)id, "RoomBooking");
            ViewBag.Previous = QueryHelper.Previous((int)id, "RoomBooking");
            ViewBag.Last = QueryHelper.GetLast("RoomBooking");
            ViewBag.First = QueryHelper.GetFirst("RoomBooking");
            return View(roomBooking);
        }

        [AllowAnonymous]
        [SkipERPAuthorize]
        [HttpPost]
        public ActionResult AddEdit(RoomBooking roomBooking)
        {
            if (ModelState.IsValid)
            {
                var id = roomBooking.Id;
                var Admin = db.ERPUsers.FirstOrDefault();
                roomBooking.IsDeleted = false;
                if (System.Web.Configuration.WebConfigurationManager.AppSettings["ProjectName"] != "FunTime")
                {
                    roomBooking.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                }

                if (roomBooking.Id > 0)
                {
                    //db.Entry(roomBooking).State = EntityState.Modified;
                    db.RoomBooking_Update(roomBooking.Id, roomBooking.DocumentNumber, roomBooking.RegistrationDate, roomBooking.RoomId, roomBooking.CustomerId, roomBooking.BookingStartDate, roomBooking.BookingEndDate, roomBooking.UserId, roomBooking.IsDeleted, roomBooking.Notes, roomBooking.Image, roomBooking.BuildingId, roomBooking.Total, roomBooking.Discount, roomBooking.TotalAfterDiscount, roomBooking.DepartmentId, roomBooking.CashBoxId, roomBooking.Paid, roomBooking.Remain, roomBooking.Insurance, roomBooking.BookingStatusId, roomBooking.IsReserved, roomBooking.CurrencyId, roomBooking.CurrencyEquivalent, roomBooking.RequiredAmount, roomBooking.RoomPrice, roomBooking.NightsNumber, roomBooking.RoomTypeId, roomBooking.Tax, roomBooking.PaymentMethodId, roomBooking.TotalAfterTaxes, roomBooking.OnlinePaymentId, roomBooking.TaxPercentage, roomBooking.BookingEmployeeId);
                    Notification.GetNotification("RoomBooking", "Edit", "AddEdit", roomBooking.Id, null, "حجز الغرف");
                }
                else
                {
                    roomBooking.IsReserved = true;
                    // roomBooking.BookingStatusId = 2;//تم التسكين
                    roomBooking.DocumentNumber = new JavaScriptSerializer().Serialize(SetDocNum(roomBooking.DepartmentId, roomBooking.RegistrationDate).Data).ToString().Trim('"');
                    if (System.Web.Configuration.WebConfigurationManager.AppSettings["ProjectName"] != "FunTime")
                    {
                        var room = db.Rooms.Find(roomBooking.RoomId);
                        room.IsReserved = true;
                        room.RoomStatusId = 2; //Busy
                                               // db.RoomBookings.Add(roomBooking);
                    }
                    var idResult = new ObjectParameter("Id", typeof(Int32));
                    db.RoomBooking_Insert(idResult, roomBooking.RegistrationDate, roomBooking.RoomId, roomBooking.CustomerId, roomBooking.BookingStartDate, roomBooking.BookingEndDate, roomBooking.UserId, roomBooking.IsDeleted, roomBooking.Notes, roomBooking.Image, roomBooking.BuildingId, roomBooking.Total, roomBooking.Discount, roomBooking.TotalAfterDiscount, roomBooking.DepartmentId, roomBooking.CashBoxId, roomBooking.Paid, roomBooking.Remain, roomBooking.Insurance, roomBooking.BookingStatusId, roomBooking.IsReserved, roomBooking.CurrencyId, roomBooking.CurrencyEquivalent, roomBooking.RequiredAmount, roomBooking.RoomPrice, roomBooking.NightsNumber, roomBooking.RoomTypeId, roomBooking.Tax, roomBooking.PaymentMethodId, roomBooking.TotalAfterTaxes, roomBooking.OnlinePaymentId, roomBooking.TaxPercentage, roomBooking.BookingEmployeeId);
                    id = (int)idResult.Value;
                    Notification.GetNotification("RoomBooking", "Add", "AddEdit", roomBooking.Id, null, "حجز الغرف");
                    // Send Email with Booking Data To Customer
                    if (System.Web.Configuration.WebConfigurationManager.AppSettings["ProjectName"] == "FunTime")
                    {
                        //var RoomNumber = db.Rooms.Where(a => a.IsDeleted == false && a.IsActive == true && a.Id == roomBooking.RoomId).FirstOrDefault().RoomNumber;
                        var RoomType = db.RoomTypes.Where(a => a.IsDeleted == false && a.IsActive == true && a.Id == roomBooking.RoomTypeId).FirstOrDefault().ArName;

                        var BookingNumber = roomBooking.DocumentNumber.ToString().Replace("-", "");
                        var Customer = db.Customers.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == roomBooking.CustomerId).FirstOrDefault();
                        var CustomerEmail = Customer != null ? Customer.Email : null;

                        MailAddress receiverEmail = new MailAddress(CustomerEmail);
                        var AdminEmail = Admin.Email != null ? Admin.Email : "mysoft2022.eg@gmail.com";
                        var senderEmail = new MailAddress(AdminEmail, "MySoft");
                        //var Emailpassword = "Mysoft@123";
                        var Emailpassword = Admin.AppPassword != null ? Admin.AppPassword : "bpnpqfhpeckovckl";
                        MailMessage message = new MailMessage(senderEmail, receiverEmail);
                        message.Subject = "تأكيد حجز الشاليه ";
                        message.Body = "تم حجز شاليه  : " + RoomType + "\n" +
                            "من تاريخ : " + roomBooking.BookingStartDate + "\n" +
                            " إلى تاريخ : " + roomBooking.BookingEndDate + "\n" +
                            "برقم حجز : " + BookingNumber + "\n" +
                            " المبلغ المدفوع : " + roomBooking.Paid + "\n" +
                            " المبلغ المطلوب : " + roomBooking.Remain;
                        SmtpClient smtp = new SmtpClient
                        {
                            Host = "smtp.gmail.com",
                            Port = 587,
                            EnableSsl = true,
                            DeliveryMethod = SmtpDeliveryMethod.Network,
                            UseDefaultCredentials = false,
                            Credentials = new NetworkCredential(senderEmail.Address, Emailpassword)
                        };
                        try
                        {
                            smtp.Send(message);
                        }
                        catch (SmtpException ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }
                }
                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();

                    return View(roomBooking);
                }
                if (System.Web.Configuration.WebConfigurationManager.AppSettings["ProjectName"] != "FunTime")
                {
                    QueryHelper.AddLog(new MyLog()
                    {
                        ArAction = id > 0 ? "تعديل حجز الغرف" : "اضافة حجز الغرف",
                        EnAction = "AddEdit",
                        ControllerName = "RoomBooking",
                        UserName = User.Identity.Name,
                        UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                        LogDate = DateTime.Now,
                        RequestMethod = "POST",
                        SelectedItem = id,
                        CodeOrDocNo = roomBooking.DocumentNumber
                    });
                }
                return Json(new { success = true, id });

            }
            var Validationerrors = ModelState
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
                RoomBooking roomBooking = db.RoomBookings.Find(id);
                if (roomBooking.Room.IsReserved == true)
                {
                    roomBooking.Room.IsReserved = false;
                    // return Content("false");
                }
                //roomBooking.IsDeleted = true;
                //db.Entry(roomBooking).State = EntityState.Modified;
                //db.SaveChanges();
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.RoomBooking_Delete(id, userId);
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف حجز الغرف",
                    EnAction = "AddEdit",
                    ControllerName = "RoomBooking",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("RoomBooking", "Delete", "Delete", id, null, "حجز الغرف");


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
            var lastObj = db.RoomBookings.Where(a => a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.RoomBookings.Where(a => a.IsDeleted == false && a.DepartmentId == id && a.RegistrationDate.Value.Month == VoucherDate.Value.Month && a.RegistrationDate.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.RoomBookings.Where(a => a.IsDeleted == false && a.DepartmentId == id && a.RegistrationDate.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

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
            //var docNo = QueryHelper.DocLastNum(id, "RoomBooking");
            //double i = (docNo) + 1;
            //return Json(i, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult CheckAvailableRooms(DateTime? RegistrationDate)
        {
            var roomBooking = db.RoomBookings.Where(a => a.IsDeleted == false && a.BookingEndDate.Value.Year == RegistrationDate.Value.Year && a.BookingEndDate.Value.Month == RegistrationDate.Value.Month).ToList();
            foreach (var item in roomBooking)
            {
                var x = item.BookingEndDate.Value.Hour;
                var y = RegistrationDate.Value.Hour;
                DateTime NewBookingEndDate;
                if (item.BookingEndDate.Value.Day == 1)
                {
                    var MonthDays = DateTime.DaysInMonth(item.BookingEndDate.Value.Year, item.BookingEndDate.Value.Month - 1);
                    NewBookingEndDate = new DateTime(item.BookingEndDate.Value.Year, item.BookingEndDate.Value.Month - 1, MonthDays, item.BookingEndDate.Value.Hour, item.BookingEndDate.Value.Minute, item.BookingEndDate.Value.Second);
                    var NewDays = NewBookingEndDate.AddDays(1).Day;
                    if (NewDays == RegistrationDate.Value.Day && (NewBookingEndDate.Hour >= 12 && RegistrationDate.Value.Hour <= 10))
                    {
                        var room = db.Rooms.Find(item.RoomId);
                        if (room.IsReserved == true)
                        {
                            room.RoomStatusId = 3; // about to leave 
                            db.Entry(room).State = EntityState.Modified;
                            db.SaveChanges();
                        }
                    }
                }
                else
                {
                    NewBookingEndDate = new DateTime(item.BookingEndDate.Value.Year, item.BookingEndDate.Value.Month, item.BookingEndDate.Value.Day - 1, item.BookingEndDate.Value.Hour, item.BookingEndDate.Value.Minute, item.BookingEndDate.Value.Second);
                    if (NewBookingEndDate.Day == RegistrationDate.Value.Day && (NewBookingEndDate.Hour >= 12 && RegistrationDate.Value.Hour <= 10))
                    {
                        var room = db.Rooms.Find(item.RoomId);
                        if (room.IsReserved == true)
                        {
                            room.RoomStatusId = 3; // about to leave 
                            db.Entry(room).State = EntityState.Modified;
                            db.SaveChanges();
                        }
                    }
                }
            }
            return Json("Success", JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public ActionResult Panel(DateTime? Date/*DateTime? BookingStartDate, DateTime? BookingEndDate*/, int? BuildingId, int? FloorId, Boolean Search = false)
        {
            // Booking Status -->> (انتظار - تم التسكين - تم التسليم - تم الالغاء)

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
            ViewBag.RegistrationDate = cTime.ToString("yyyy-MM-ddTHH:mm");
            ViewBag.BookingStartDate = cTime.ToString("yyyy-MM-ddTHH:mm");
            ViewBag.BookingEndDate = cTime.ToString("yyyy-MM-ddTHH:mm");
            ViewBag.Date = cTime.ToString("yyyy-MM-dd");
            ViewBag.BuildingId = new SelectList(db.Buildings.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", BuildingId);
            ViewBag.FloorId = new SelectList(new List<dynamic> {
                    new { Id=1, ArName="أرضي"},
                    new { Id=2, ArName="أول علوي"},
                    new { Id=3, ArName="ثانى علوي"},
                    new { Id=4, ArName="ثالث علوي"}}, "Id", "ArName", FloorId);
            var buildings = new List<Building>();
            if (BuildingId > 0)
            {
                buildings = db.Buildings.Where(a => a.IsDeleted == false && a.IsActive == true && a.Id == BuildingId).ToList();
                // ViewBag.Buildings = db.Buildings.Where(a => a.IsDeleted == false && a.IsActive == true && a.Id == BuildingId).ToList();
            }
            else
            {
                buildings = db.Buildings.Where(a => a.IsDeleted == false && a.IsActive == true).ToList();
                //ViewBag.Buildings = db.Buildings.Where(a => a.IsDeleted == false && a.IsActive == true).ToList();
            }

            if (Search == true)
            {
                //ViewBag.BookingStartDate = BookingStartDate != null ? BookingStartDate.Value.ToString("yyyy-MM-ddTHH:mm") : null;
                //ViewBag.BookingEndDate = BookingEndDate != null ? BookingEndDate.Value.ToString("yyyy-MM-ddTHH:mm") : null;
                ViewBag.Date = Date != null ? Date.Value.ToString("yyyy-MM-ddTHH:mm") : null;
                var BookedRooms = db.GetBuildingRooms_Panel(BuildingId, FloorId, Date/* BookingStartDate, BookingEndDate*/).ToList();
                //var BookedRoomsBuilding = buildings.Where(a => BookedRooms.Any(a1 => a1.BuildingId == a.Id)).ToList();
                //ViewBag.Buildings = BookedRoomsBuilding;
                ViewBag.Buildings = buildings;
                return View(BookedRooms);
            }
            else
            {
                return View();
            }
        }
        [AllowAnonymous]
        [SkipERPAuthorize]
        public JsonResult GetBuildingRooms(int? BuildingId, DateTime? BookingStartDate, DateTime? BookingEndDate, int? RoomTypeId, int? RoomId)
        {
            var rooms = db.Rooms.Where(a => a.IsActive == true && a.IsDeleted == false && a.BuildingId == BuildingId).Select(a => new { RoomId = a.Id, RoomCode = a.Code + " - " + a.RoomNumber }).ToList();
            return Json(rooms, JsonRequestBehavior.AllowGet);
            //var rooms = db.GetBuildingRooms(BuildingId, null, BookingStartDate, BookingEndDate, RoomTypeId).ToList(); 
            //return Json(rooms, JsonRequestBehavior.AllowGet); 
        }

        [SkipERPAuthorize]
        public JsonResult GetRoomPrice(int? RoomId, int? RoomTypeId)
        {
            if (System.Web.Configuration.WebConfigurationManager.AppSettings["ProjectName"] == "FunTime") //RoomTypeId
            {
                var RoomTypePrice = db.RoomTypes.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == RoomTypeId).FirstOrDefault().Price;
                return Json(RoomTypePrice, JsonRequestBehavior.AllowGet);
            }
            var RoomPrice = db.Rooms.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == RoomId).FirstOrDefault().RoomPrice;
            return Json(RoomPrice, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult CheckReservedRoomByRoomId_RoomTypeId(int? RoomId, int? RoomTypeId, DateTime? BookingStartDate, DateTime? BookingEndDate)
        {
            DateTime?[] ReservedDate;
            var Dates = new List<DateTime?>();

            if (System.Web.Configuration.WebConfigurationManager.AppSettings["ProjectName"] == "FunTime") //RoomTypeId
            {
                ReservedDate = db.RoomBookingDays.Where(a => a.RoomTypeId == RoomTypeId).Select(a => a.ReservationDate).ToArray();
            }
            else //RoomId
            {
                ReservedDate = db.RoomBookingDays.Where(a => a.RoomId == RoomId).Select(a => a.ReservationDate).ToArray();
            }

            if (ReservedDate.Length > 0)
            {
                for (var dt = BookingStartDate; dt <= BookingEndDate; dt = dt.Value.AddDays(1))
                {
                    var Exist = ReservedDate.Where(a => a.Value.ToString("yyyy-MM-dd") == dt.Value.ToString("yyyy-MM-dd")).Any();
                    if (Exist == true)
                    {
                        Dates.Add(dt);
                    }
                }
                if (Dates.Count() > 0)
                {
                    //return Json(ReservedDate, JsonRequestBehavior.AllowGet);
                    return Json(Dates, JsonRequestBehavior.AllowGet);

                }
            }
            return Json(Dates, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult ShowReservationDetails(int? RoomId, int? RoomTypeId)
        {
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
            if (RoomTypeId != null)
            {
                // يظهر بناء على الحالة مش التاريخ "اى حجز ماتقفلش هو اللى يظهر يعنى كله يظهر ماعدا الى تم التسكين و المغادرة و التأجيل
                // 1="فى انتظار التأكيد "= 2 , "تم دفع عربون" 
                var Details = db.RoomBookingDays.Where(a => a.RoomTypeId == RoomTypeId && (a.RoomBooking.BookingStatusId == 1 || a.RoomBooking.BookingStatusId == 2) /*a.ReservationDate.Value.Year == cTime.Year && a.ReservationDate.Value.Month== cTime.Month && a.ReservationDate.Value.Day== cTime.Day*/).Select(a => new
                {
                    BookingStatusId = a.RoomBooking.BookingStatusId,
                    BookingNo = a.RoomBooking.DocumentNumber,
                    ReservationDate = a.ReservationDate,
                    BookingStartDate = a.RoomBooking.BookingStartDate,
                    BookingEndDate = a.RoomBooking.BookingEndDate,
                    CustomerName = a.RoomBooking.Customer.ArName,
                    CustomerTel = a.RoomBooking.Customer.Mobile
                }).ToList();
                return Json(Details, JsonRequestBehavior.AllowGet);

            }
            var Detail = db.RoomBookingDays.Where(a => a.RoomId == RoomId && (a.RoomBooking.BookingStatusId == 1 || a.RoomBooking.BookingStatusId == 2) /*a.ReservationDate.Value.Year == cTime.Year && a.ReservationDate.Value.Month >= cTime.Month && a.ReservationDate.Value.Day >= cTime.Day*/).Select(a => new
            {
                BookingStatusId = a.RoomBooking.BookingStatusId,
                BookingNo = a.RoomBooking.DocumentNumber,
                ReservationDate = a.ReservationDate,
                BookingStartDate = a.RoomBooking.BookingStartDate,
                BookingEndDate = a.RoomBooking.BookingEndDate,
                CustomerName = a.RoomBooking.Customer.ArName,
                CustomerTel = a.RoomBooking.Customer.Mobile
            }).ToList();
            return Json(Detail, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        [AllowAnonymous]
        public JsonResult SendBookingDetails(int? id)
        {
            var roomBooking = db.RoomBookings.Where(a => a.Id == id).FirstOrDefault();
            var Room = roomBooking.RoomType != null ? roomBooking.RoomType.ArName : roomBooking.Room.RoomNumber;
            var instructions = db.BookingInstructions.FirstOrDefault() != null ? "\n" + "  تعليمات الحجز : " + db.BookingInstructions.FirstOrDefault().Instructions : "";
            var Detail = "تم حجز شاليه  : " + Room + "\n" +
                            "من تاريخ : " + roomBooking.BookingStartDate + "\n" +
                            " إلى تاريخ : " + roomBooking.BookingEndDate + "\n" +
                            "برقم حجز : " + roomBooking.DocumentNumber + "\n" +
                            " المبلغ المدفوع : " + roomBooking.Paid + "\n" +
                            " المبلغ المطلوب : " + roomBooking.Remain + instructions;
            var Mobile = roomBooking.Customer.Mobile;
            return Json(new { Detail, Mobile }, JsonRequestBehavior.AllowGet);
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