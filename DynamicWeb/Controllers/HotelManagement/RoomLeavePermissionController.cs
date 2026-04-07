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
    public class RoomLeavePermissionController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: RoomLeavePermission
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة إذن مغادرة غرفة",
                EnAction = "Index",
                ControllerName = "RoomLeavePermission",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("RoomLeavePermission", "View", "Index", null, null, "إذن مغادرة غرفة");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<RoomLeavePermission> roomLeavePermissions;

            if (string.IsNullOrEmpty(searchWord))
            {
                roomLeavePermissions = db.RoomLeavePermissions.Where(s => s.IsDeleted == false).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.RoomLeavePermissions.Where(s => s.IsDeleted == false).Count();
            }
            else
            {
                roomLeavePermissions = db.RoomLeavePermissions.Where(s => s.IsDeleted == false && (s.DocumentNumber.Contains(searchWord) || s.Customer.ArName.Contains(searchWord))).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.RoomLeavePermissions.Where(s => s.IsDeleted == false && (s.DocumentNumber.Contains(searchWord) || s.Customer.ArName.Contains(searchWord))).Count();
            }

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة إذن مغادرة غرفة",
                EnAction = "Index",
                ControllerName = "RoomLeavePermission",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            return View(roomLeavePermissions.ToList());
        }
        public ActionResult AddEdit(int? id)
        {
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            var departments = departmentRepository.UserDepartments(1).ToList();//show all deprartments so that user can select factory department
            CashboxReposistory cashboxReposistory = new CashboxReposistory(db);
            SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            if (id == null)
            {
                ViewBag.RoomBookingId = new SelectList(db.RoomBookings.Where(a => a.IsDeleted == false && a.BookingStatusId == 2 && a.IsReserved == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.DocumentNumber
                }), "Id", "ArName");
                ViewBag.CustomerIdPopUp = new SelectList(db.Customers.Where(a => a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName =b.Code+" - "+ b.ArName
                }), "Id", "ArName");
                ViewBag.ResponsibleEmployeeId = new SelectList(db.Employees.Where(a => a.IsDeleted == false).Select(b => new
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
                ViewBag.VoucherDate = cTime.ToString("yyyy-MM-ddTHH:mm");
                return View();
            }
            //--------- JournalEntry --------------//
            int sysPageId = QueryHelper.SourcePageId("RoomLeavePermission");
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
            RoomLeavePermission roomLeavePermission = db.RoomLeavePermissions.Find(id);
            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", roomLeavePermission.DepartmentId);
            ViewBag.CashBoxId = new SelectList(cashboxReposistory.UserCashboxes(userId, roomLeavePermission.DepartmentId).ToList(), "Id", "ArName", roomLeavePermission.CashBoxId);
            ViewBag.RoomBookingId = new SelectList(db.RoomBookings.Where(a => a.IsDeleted == false /*&&a.BookingStatusId==2&&a.IsReserved==true*/).Select(b => new
            {
                Id = b.Id,
                ArName = b.DocumentNumber
            }), "Id", "ArName", roomLeavePermission.RoomBookingId);
            ViewBag.CustomerIdPopUp = new SelectList(db.Customers.Where(a => a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName",roomLeavePermission.CustomerId); 
            ViewBag.ResponsibleEmployeeId = new SelectList(db.Employees.Where(a => a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName",roomLeavePermission.ResponsibleEmployeeId);
            if (roomLeavePermission == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل إذن مغادرة غرفة ",
                EnAction = "AddEdit",
                ControllerName = "RoomLeavePermission",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });

            ViewBag.VoucherDate = roomLeavePermission.VoucherDate != null ? roomLeavePermission.VoucherDate.Value.ToString("yyyy-MM-ddTHH:mm") : null;
            ViewBag.Next = QueryHelper.Next((int)id, "RoomLeavePermission");
            ViewBag.Previous = QueryHelper.Previous((int)id, "RoomLeavePermission");
            ViewBag.Last = QueryHelper.GetLast("RoomLeavePermission");
            ViewBag.First = QueryHelper.GetFirst("RoomLeavePermission");
            return View(roomLeavePermission);
        }

        [HttpPost]
        public ActionResult AddEdit(RoomLeavePermission roomLeavePermission)
        {
            if (ModelState.IsValid)
            {
                var id = roomLeavePermission.Id;
                roomLeavePermission.IsDeleted = false;
                roomLeavePermission.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                if (roomLeavePermission.Id > 0)
                {
                    //db.Entry(roomLeavePermission).State = EntityState.Modified;
                    db.RoomLeavePermission_Update(roomLeavePermission.Id, roomLeavePermission.DocumentNumber, roomLeavePermission.VoucherDate, roomLeavePermission.DepartmentId, roomLeavePermission.RoomBookingId, roomLeavePermission.CashBoxId, roomLeavePermission.CustomerId, roomLeavePermission.BuildingId, roomLeavePermission.RoomId, roomLeavePermission.Insurance, roomLeavePermission.InsuranceDeduction, roomLeavePermission.Remain, roomLeavePermission.UserId, roomLeavePermission.IsDeleted, roomLeavePermission.Notes, roomLeavePermission.Image, roomLeavePermission.CurrencyId, roomLeavePermission.CurrencyEquivalent, roomLeavePermission.ResponsibleEmployeeId);
                    Notification.GetNotification("RoomLeavePermission", "Edit", "AddEdit", roomLeavePermission.Id, null, "إذن مغادرة غرفة");
                }
                else
                {
                    roomLeavePermission.DocumentNumber = new JavaScriptSerializer().Serialize(SetDocNum(roomLeavePermission.DepartmentId,roomLeavePermission.VoucherDate).Data).ToString().Trim('"');
                    var room = db.Rooms.Find(roomLeavePermission.RoomId);
                    room.IsReserved = false;
                    room.RoomStatusId = 1; //Empty
                    db.Entry(room).State = EntityState.Modified;

                    var roomBooking = db.RoomBookings.Find(roomLeavePermission.RoomBookingId);
                    roomBooking.IsReserved = false;
                    roomBooking.BookingStatusId = 3; //تم التسليم
                     db.Entry(roomBooking).State = EntityState.Modified;

                    // db.RoomLeavePermissions.Add(roomLeavePermission);
                    var idResult = new ObjectParameter("Id", typeof(Int32));
                    db.RoomLeavePermission_Insert(idResult,roomLeavePermission.VoucherDate,roomLeavePermission.DepartmentId,roomLeavePermission.RoomBookingId,roomLeavePermission.CashBoxId,roomLeavePermission.CustomerId,roomLeavePermission.BuildingId,roomLeavePermission.RoomId,roomLeavePermission.Insurance,roomLeavePermission.InsuranceDeduction,roomLeavePermission.Remain,roomLeavePermission.UserId,roomLeavePermission.IsDeleted,roomLeavePermission.Notes,roomLeavePermission.Image, roomLeavePermission.CurrencyId, roomLeavePermission.CurrencyEquivalent, roomLeavePermission.ResponsibleEmployeeId);
                    id = (int)idResult.Value;
                    Notification.GetNotification("RoomLeavePermission", "Add", "AddEdit", roomLeavePermission.Id, null, "إذن مغادرة غرفة");
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
                    return View(roomLeavePermission);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل إذن مغادرة غرفة" : "اضافة إذن مغادرة غرفة",
                    EnAction = "AddEdit",
                    ControllerName = "RoomLeavePermission",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = roomLeavePermission.DocumentNumber
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
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.RoomLeavePermission_Delete(id, userId);

                //RoomLeavePermission roomLeavePermission = db.RoomLeavePermissions.Find(id);
                //roomLeavePermission.IsDeleted = true;
                //roomLeavePermission.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                //Random random = new Random();
                //const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                //var docNo = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
                //roomLeavePermission.DocumentNumber = docNo;
                //db.Entry(roomLeavePermission).State = EntityState.Modified;

                //var systemPageid = db.SystemPages.Where(a => a.IsActive == true && a.IsDeleted == false && a.ControllerName == "RoomLeavePermission").FirstOrDefault().Id;
                //JournalEntry journalEntry = db.JournalEntries.Where(a => a.IsActive == true && a.IsDeleted == false && a.SourcePageId == systemPageid && a.SourceId == id).FirstOrDefault();
                //journalEntry.IsDeleted = true;
                //foreach (var detail in journalEntry.JournalEntryDetails)
                //{
                //    detail.IsDeleted = true;
                //}
                //db.Entry(journalEntry).State = EntityState.Modified;
                //db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف إذن مغادرة غرفة",
                    EnAction = "AddEdit",
                    ControllerName = "RoomLeavePermission",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("RoomLeavePermission", "Delete", "Delete", id, null, "إذن مغادرة غرفة");
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
            var lastObj = db.RoomLeavePermissions.Where(a => a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.RoomLeavePermissions.Where(a => a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Value.Month == VoucherDate.Value.Month && a.VoucherDate.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.RoomLeavePermissions.Where(a => a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

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
            //var docNo = QueryHelper.DocLastNum(id, "RoomLeavePermission");
            //double i = (docNo) + 1;
            //return Json(i, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult GetRoomBookingDetails(int? RoomBookingId)
        {
            var Details = db.RoomBookings.Where(a => a.IsDeleted == false && a.Id == RoomBookingId /*&& a.BookingStatusId == 2 && a.IsReserved == true*/).Select(a => new
            {
                a.CustomerId,
                CustomerArName = a.Customer.Code + " - " + a.Customer.ArName,
                CustomerMobile = a.Customer.Mobile,
                a.BuildingId,
                BuildingArName = a.Building.Code + " - " + a.Building.ArName,
                a.RoomId,
                RoomNumber = a.Room.Code + " - " + a.Room.RoomNumber,
                a.Insurance
            }).FirstOrDefault();
            return Json(Details, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult GetCustomerDetails(int? CustomerId, string MobileNo)
        {
            var CustomerReservation = new object();
            if (CustomerId >0)
            {
                CustomerReservation = db.RoomBookings.Where(e => e.IsDeleted == false && e.CustomerId == CustomerId&&e.BookingStatusId==2&&e.IsReserved==true).Select(e => new
                {
                    RoomBookingId = e.Id,
                    RoomBookingDocumentNumber = e.DocumentNumber,
                    e.CustomerId,
                    CustomerArName = e.Customer.Code+" - "+ e.Customer.ArName,
                    CustomerMobile = e.Customer.Mobile,
                    e.BookingStartDate,
                    e.BookingEndDate,
                    e.BuildingId,
                    BuildingArName = e.Building.Code + " - " + e.Building.ArName,
                    e.RoomId,
                    RoomNumber = e.Room.Code + " - " + e.Room.RoomNumber,
                    e.Insurance
                }).AsNoTracking().ToList();
            }
            else if (MobileNo != "null")
            {
                var _Customer = db.Customers.Where(e => e.IsDeleted == false && e.IsActive == true && e.Mobile == MobileNo).FirstOrDefault();
                var _customerId = _Customer != null ? _Customer.Id : 0;
                CustomerReservation = db.RoomBookings.Where(e => e.IsDeleted == false && e.CustomerId == _customerId && e.BookingStatusId == 2 && e.IsReserved == true).Select(e => new
                {
                    RoomBookingId = e.Id,
                    RoomBookingDocumentNumber = e.DocumentNumber,
                    e.CustomerId,
                    CustomerArName = e.Customer.Code + " - " + e.Customer.ArName,
                    CustomerMobile = e.Customer.Mobile,
                    e.BookingStartDate,
                    e.BookingEndDate,
                    e.BuildingId,
                    BuildingArName = e.Building.Code + " - " + e.Building.ArName,
                    e.RoomId,
                    RoomNumber = e.Room.Code + " - " + e.Room.RoomNumber,
                    e.Insurance
                }).AsNoTracking().ToList();
            }

           // var posCustomer = new { posCustomer = CustomerReservation };
            return Json(CustomerReservation, JsonRequestBehavior.AllowGet);
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