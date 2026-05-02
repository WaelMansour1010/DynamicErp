using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using MyERP.Repository;
using Newtonsoft.Json;
using System.IO;

namespace MyERP.Controllers
{
    [AllowAnonymous]
    [SkipERPAuthorize]
    public class HotelBookingController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: HotelBooking
        public ActionResult Index()
        {

            ViewBag.CustomerId = new SelectList(db.Customers.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");
            ViewBag.RoomTypeId = new SelectList(db.RoomTypes.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");

            DateTime utcNow = DateTime.UtcNow;
            TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
            ViewBag.CheckInDate = cTime.ToString("yyyy-MM-ddTHH:mm");
            ViewBag.CheckOutDate = cTime.ToString("yyyy-MM-ddTHH:mm");
            var roomType = db.RoomTypes.Where(a => a.IsActive == true && a.IsDeleted == false && a.ParentId == null && a.TypeId == 1).ToList();
            return View(roomType);
        }
        public ActionResult Index3(int? ParentId)
        {
         
            //DateTime utcNow = DateTime.UtcNow;
            //TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            //DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
            //ViewBag.CheckInDate = cTime.ToString("yyyy-MM-ddTHH:mm");
            //ViewBag.CheckOutDate = cTime.ToString("yyyy-MM-ddTHH:mm");
            var roomType = db.RoomTypes.Where(a => a.IsActive == true && a.IsDeleted == false &&  a.ParentId == ParentId && a.TypeId == 2).ToList();
            return View(roomType);
        }
        public ActionResult Index2(int? ParentId)
        {
            ViewBag.CustomerId = new SelectList(db.Customers.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");
            ViewBag.RoomTypeId = new SelectList(db.RoomTypes.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");

            DateTime utcNow = DateTime.UtcNow;
            TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
            ViewBag.CheckInDate = cTime.ToString("yyyy-MM-ddTHH:mm");
            ViewBag.CheckOutDate = cTime.ToString("yyyy-MM-ddTHH:mm");
            var roomType = db.RoomTypes.Where(a => a.IsActive == true && a.IsDeleted == false && a.ParentId == ParentId && a.TypeId ==3).ToList();
            ViewBag.MainChalet = roomType.FirstOrDefault() != null ? roomType.FirstOrDefault().RoomType2.ArName : null;
            return View(roomType);
        }

        public ActionResult Registration(int? RoomTypeId, DateTime? From, DateTime? To)
        {
            ViewBag.RoomTypeId = RoomTypeId;
            ViewBag.From = From;
            ViewBag.To = To;
           // ViewBag.Currency = "SR"; //الريال السعودي
            ViewBag.Currency = "USD";
            return View();
        }

        public JsonResult CompleteRegistration(int? CustomerId, int? RoomTypeId, DateTime? From, DateTime? To,int ?PaymentMethodId, string Email,string OnlinePaymentId)
        {
            if(CustomerId!=null&&Email!=null)
            {
                var customer = db.Customers.Find(CustomerId);
                customer.Email = Email;
                db.Entry(customer).State = EntityState.Modified;
                db.SaveChanges();
            }
            ViewBag.RoomTypeId = RoomTypeId;
            ViewBag.From = From;
            ViewBag.To = To;
            DateTime utcNow = DateTime.UtcNow;
            TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);

            
            double? perc = 0.14;
            var taxPercentage = db.TaxesPercentages.Where(t => t.IsActive == true && t.IsDeleted == false && t.DateFrom <= cTime && t.DateTo >= cTime).FirstOrDefault();
            if (taxPercentage != null)
                perc = (taxPercentage.TaxPercentage / 100);

            RoomBooking roomBooking = new RoomBooking();
            roomBooking.RegistrationDate = cTime; // Temp 
            roomBooking.PaymentMethodId = PaymentMethodId;
            roomBooking.OnlinePaymentId = OnlinePaymentId;
            roomBooking.DepartmentId = db.Departments.Where(a=>a.IsActive==true&&a.IsDeleted==false).FirstOrDefault().Id;// Temp
            roomBooking.CashBoxId = db.CashBoxes.Where(a=>a.IsActive==true&&a.IsDeleted==false&&a.DepartmentId== roomBooking.DepartmentId).FirstOrDefault().Id;// Temp
            roomBooking.CustomerId = CustomerId;
            roomBooking.BookingStartDate = From;
            roomBooking.BookingEndDate = To;
            roomBooking.RoomTypeId = RoomTypeId;
            roomBooking.NightsNumber = (int?)(roomBooking.BookingEndDate - roomBooking.BookingStartDate).Value.TotalDays;
            roomBooking.TaxPercentage = perc;
            if (System.Web.Configuration.WebConfigurationManager.AppSettings["ProjectName"] == "FunTime")
            {
                var RoomType = db.RoomTypes.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == RoomTypeId).FirstOrDefault();
                var RoomTypePrice = RoomType != null ? RoomType.Price : 1;
                var total= (roomBooking.NightsNumber) * RoomTypePrice;
                var totalAfterTaxes = (total * (decimal?)roomBooking.TaxPercentage) + total;
                roomBooking.Tax = total * (decimal?)roomBooking.TaxPercentage;
                roomBooking.Remain = totalAfterTaxes; //(((roomBooking.NightsNumber) * RoomTypePrice)*roomBooking.Tax)+((roomBooking.NightsNumber) * RoomTypePrice);
                roomBooking.RequiredAmount = totalAfterTaxes; //roomBooking.Remain;
                roomBooking.RoomPrice = RoomTypePrice;
                roomBooking.Total = total; //(roomBooking.NightsNumber) * RoomTypePrice;
                roomBooking.TotalAfterTaxes = totalAfterTaxes;// (roomBooking.Total * roomBooking.Tax) + roomBooking.Total;
            }
            else
            {
                var Room = db.Rooms.Where(a => a.IsActive == true && a.IsDeleted == false && a.RoomTypeId == RoomTypeId).FirstOrDefault();
                var RoomId = Room != null ? Room.Id : 0;
                var BuildingId = Room != null ? Room.BuildingId : 0;
                roomBooking.RoomId = RoomId; // Temp
                roomBooking.BuildingId = BuildingId; // Temp
                var total = (roomBooking.NightsNumber) * Room.RoomPrice;
                var totalAfterTaxes = (total * roomBooking.Tax) + total;
                roomBooking.Tax = total * (decimal?)roomBooking.TaxPercentage;
                roomBooking.Remain = totalAfterTaxes; ;//(((roomBooking.NightsNumber) * Room.RoomPrice)*roomBooking.Tax) +(roomBooking.NightsNumber) * Room.RoomPrice;
                roomBooking.RequiredAmount = totalAfterTaxes; // roomBooking.Remain;
                roomBooking.RoomPrice = Room.RoomPrice;
                roomBooking.Total = total; //(roomBooking.NightsNumber) * Room.RoomPrice;
                roomBooking.TotalAfterTaxes = totalAfterTaxes;// (roomBooking.Total * roomBooking.Tax) + roomBooking.Total;
            }
            roomBooking.Paid = 0;
            //roomBooking.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            roomBooking.IsDeleted = false;
            /*,roomBooking.Total,roomBooking.Discount,roomBooking.TotalAfterDiscount,,roomBooking.Paid,roomBooking.Remain,roomBooking.Insurance,roomBooking.BookingStatusId,roomBooking.IsReserved,roomBooking.CurrencyId,roomBooking.CurrencyEquivalent, roomBooking.RequiredAmount, roomBooking.RoomPrice, roomBooking.NightsNumber*/

            return Json(roomBooking, JsonRequestBehavior.AllowGet);
        }

        public ActionResult AboutUs()
        {
            return View();
        }

        public JsonResult showBookingDetails( int? RoomTypeId, DateTime? From, DateTime? To)
        {
            int NoOfNights = (int)(To - From).Value.TotalDays;
            DateTime utcNow = DateTime.UtcNow;
            TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
                 double? perc = 0.14;
            var taxPercentage = db.TaxesPercentages.Where(t => t.IsActive == true && t.IsDeleted == false && t.DateFrom <= cTime && t.DateTo >= cTime).FirstOrDefault();
            if (taxPercentage != null)
                perc = (taxPercentage.TaxPercentage/100);

            var Details = db.RoomTypes.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == RoomTypeId).Select(a => new
            {
                a.Id,
                RoomTypeArName = a.Code + " - " + a.ArName,
                a.Price,
                NightsNumber = NoOfNights,
                BookingStartDate=From,
                BookingEndDate=To,
                RequiredAmount= ((decimal?)perc * (NoOfNights * a.Price)) + ((NoOfNights * a.Price)),
                Remain= ((decimal?)perc * (NoOfNights * a.Price)) + ((NoOfNights * a.Price)),
                Total = (NoOfNights * a.Price),
                Paid=0,
                Tax= perc,
                Notes=a.Notes
            }).FirstOrDefault();

            return Json(Details, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult StoreOnlinePaymentFile(string id,string jsonObj)
        {
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);           
            var DeserializedObj = JsonConvert.DeserializeObject( jsonObj);

            //to Check If CV Name Exist before
            var file = Server.MapPath("/assets/images/Hotel/PayPalFile/" + id + ".txt").Replace('\\', '/');
            if (System.IO.File.Exists(file))
            {
                System.IO.File.Delete(file);
            }
            var jsonString = JsonConvert.SerializeObject(DeserializedObj);
            System.IO.File.WriteAllText(file, jsonString);

            return Json(id, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Test()
        {
            return View();
        }
    }
}