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

namespace MyERP.Controllers.PartyManagement
{
    public class PartyBookingController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: PartyBooking
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة حجز الحفلات",
                EnAction = "Index",
                ControllerName = "PartyBooking",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("PartyBooking", "View", "Index", null, null, "حجز الحفلات");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<PartyBooking> partyBookings;

            if (string.IsNullOrEmpty(searchWord))
            {
                partyBookings = db.PartyBookings.Where(s => s.IsDeleted == false).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.PartyBookings.Where(s => s.IsDeleted == false).Count();
            }
            else
            {
                partyBookings = db.PartyBookings.Where(s => s.IsDeleted == false && (s.DocumentNumber.Contains(searchWord) || s.Party.Singer.ArName.Contains(searchWord))).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.PartyBookings.Where(s => s.IsDeleted == false && (s.DocumentNumber.Contains(searchWord) || s.Party.Singer.ArName.Contains(searchWord))).Count();
            }

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة حجز الحفلات",
                EnAction = "Index",
                ControllerName = "PartyBooking",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            return View(partyBookings.ToList());
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

            if (id == null)
            {
                ViewBag.PartyId = new SelectList(db.Parties.Where(a => a.IsActive == true && a.IsDeleted == false && a.Date.Value.Year == cTime.Year && (a.Date.Value.Month > cTime.Month || (a.Date.Value.Month == cTime.Month && a.Date.Value.Day >= cTime.Day))).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code
                }), "Id", "ArName");
                ViewBag.CustomerId = new SelectList(db.Customers.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code+" - "+b.ArName
                }), "Id", "ArName");
               
                ViewBag.BookingDate = cTime.ToString("yyyy-MM-ddTHH:mm");
                return View();
            }
            PartyBooking partyBooking = db.PartyBookings.Find(id);

            if (partyBooking == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل حجز الحفلات ",
                EnAction = "AddEdit",
                ControllerName = "PartyBooking",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });


            ViewBag.Next = QueryHelper.Next((int)id, "PartyBooking");
            ViewBag.Previous = QueryHelper.Previous((int)id, "PartyBooking");
            ViewBag.Last = QueryHelper.GetLast("PartyBooking");
            ViewBag.First = QueryHelper.GetFirst("PartyBooking");
            ViewBag.PartyId = new SelectList(db.Parties.Where(a => a.IsActive == true && a.IsDeleted == false && a.Date.Value.Year == cTime.Year && (a.Date.Value.Month > cTime.Month || (a.Date.Value.Month == cTime.Month && a.Date.Value.Day >= cTime.Day))).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code
            }), "Id", "ArName", partyBooking.PartyId);
            ViewBag.CustomerId = new SelectList(db.Customers.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", partyBooking.CustomerId);
            ViewBag.BookingDate = partyBooking.BookingDate != null ? partyBooking.BookingDate.Value.ToString("yyyy-MM-ddTHH:mm") : null;

            return View(partyBooking);
        }

        [AllowAnonymous]
        [SkipERPAuthorize]
        [HttpPost]
        public ActionResult AddEdit(PartyBooking partyBooking, bool? IsWeb)
        {
            //var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            if (ModelState.IsValid)
            {
                var id = partyBooking.Id;
                partyBooking.IsDeleted = false;
                //partyBooking.UserId = userId;
                if (partyBooking.Id > 0)
                {
                    MyXML.xPathName = "Chairs";
                    var Chairs = MyXML.GetXML(partyBooking.Chairs);
                    db.PartyBooking_Update(partyBooking.Id, partyBooking.DocumentNumber, partyBooking.BookingDate, partyBooking.PartyId, partyBooking.ChairsNo, partyBooking.UserId, partyBooking.IsDeleted, partyBooking.Notes, partyBooking.Image, partyBooking.Total, partyBooking.Taxes, partyBooking.TotalAfterTaxes, partyBooking.CustomerId, partyBooking.PaymentMethodId, partyBooking.OnlinePaymentNo, partyBooking.ChairPrice,Chairs);
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("PartyBooking", "Edit", "AddEdit", partyBooking.Id, null, "حجز الحفلات");
                }
                else
                {
                    MyXML.xPathName = "Chairs";
                    var Chairs = MyXML.GetXML(partyBooking.Chairs);

                    var idResult = new ObjectParameter("Id", typeof(Int32));
                    db.PartyBooking_Insert(idResult, partyBooking.BookingDate, partyBooking.PartyId, partyBooking.ChairsNo, partyBooking.UserId, partyBooking.IsDeleted, partyBooking.Notes, partyBooking.Image, partyBooking.Total, partyBooking.Taxes, partyBooking.TotalAfterTaxes, partyBooking.CustomerId, partyBooking.PaymentMethodId, partyBooking.OnlinePaymentNo, partyBooking.ChairPrice, Chairs);
                    id = (int)idResult.Value;
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("PartyBooking", "Add", "AddEdit", id, null, "حجز الحفلات");
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = partyBooking.Id > 0 ? "تعديل حجز الحفلات" : "اضافة حجز الحفلات",
                    EnAction = "AddEdit",
                    ControllerName = "PartyBooking",
                    UserName = User.Identity.Name,
                    //UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "Post",
                    SelectedItem = id,
                });

                //---------------------- Send Email ------------------------------//
                if (IsWeb == true)
                {
                    var party = db.Parties.Find(partyBooking.PartyId);
                    var singer = db.Singers.Find(party.SingerId);
                    var Admin = db.ERPUsers.FirstOrDefault();
                    var PartyBookingDocNo = db.PartyBookings.Find(id).DocumentNumber;
                    var Customer = db.Customers.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == partyBooking.CustomerId).FirstOrDefault();
                    var CustomerEmail = Customer != null ? Customer.Email : null;
                    MailAddress receiverEmail = new MailAddress(CustomerEmail);
                    var AdminEmail = Admin.Email != null ? Admin.Email : "mysoft2022.eg@gmail.com";
                    var senderEmail = new MailAddress(AdminEmail, "MySoft");
                    //var Emailpassword = "Mysoft@123";
                    var Emailpassword = Admin.AppPassword != null ? Admin.AppPassword : "bpnpqfhpeckovckl";
                    MailMessage message = new MailMessage(senderEmail, receiverEmail);
                    message.Subject = "تأكيد حجز الحفلة ";
                    message.Body = "تم حجز حفلة  : " + singer.ArName + "\n" +
                        " بتاريخ : " + party.Date + "\n" +
                        " من  : " + party.FromTime + "\n" +
                        " إلى  : " + party.ToTime + "\n" +
                        "برقم حجز : " + PartyBookingDocNo + "\n" +
                        //" عدد المقاعد : " + partyBooking.ChairsNo + "\n" +
                        //" سعر المقعد : " + partyBooking.ChairPrice + "\n" +
                        "الإجمالي : " + partyBooking.Total + "\n" +
                        " الضريبة : " + partyBooking.Taxes + "\n" +
                        " المبلغ المطلوب : " + partyBooking.TotalAfterTaxes;
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
                //---------------------- End Of Send Email ------------------------------//

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
                PartyBooking partyBooking = db.PartyBookings.Find(id);
                partyBooking.IsDeleted = true;
                partyBooking.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                foreach (var item in partyBooking.Chairs)
                {
                    item.IsReserved = null;
                    item.PartyBookingId = null;
                }

                db.Entry(partyBooking).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف حجز الحفلات",
                    EnAction = "AddEdit",
                    ControllerName = "PartyBooking",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,

                });
                Notification.GetNotification("PartyBooking", "Delete", "Delete", id, null, "حجز الحفلات");


                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

        }

        [SkipERPAuthorize]
        public JsonResult SetDocNum()
        {
            double DocNo = 0;
            var code = db.Database.SqlQuery<string>($"select top(1)[DocumentNumber] from [PartyBooking] order by [Id] desc");
            if (code.FirstOrDefault() == null)
            {
                DocNo = 0;
            }
            else
            {
                DocNo = double.Parse(code.FirstOrDefault().ToString());
            }
            return Json(DocNo + 1, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult GetPartyDetails(int? PartyId,int? PartyBookingId)
        {
            var Details = db.Parties.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == PartyId).Select(a => new
            {
                SingerName = a.Singer.ArName,
                a.SingerId,
                a.Date,
                a.FromTime,
                a.ToTime,
                RemainingChairs = (a.Chairs.Count()) - (a.Chairs.Where(c => c.IsReserved == true).Count())
            }).FirstOrDefault();
            var Chairs = db.Chairs.Where(a => a.IsActive == true && a.IsDeleted == false && a.PartyId == PartyId&&(a.PartyBookingId==null||a.PartyBookingId== PartyBookingId)).Select(a=>new { 
                a.Id,
                a.ChairNo,
                a.Price,
                a.IsReserved,
                a.PartyId,
                a.PartyBookingId,
                a.Notes,

            }).OrderBy(a=>a.Price).ToList();
            return Json(new { Details , Chairs}, JsonRequestBehavior.AllowGet);
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