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

namespace MyERP.Controllers.PartyManagement
{
    [AllowAnonymous]
    [SkipERPAuthorize]
    public class PartyHomeController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        // GET: PartyHome
        public ActionResult Index()
        {
            DateTime utcNow = DateTime.UtcNow;
            TimeZone curTimeZone = TimeZone.CurrentTimeZone;
            // TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById(curTimeZone.StandardName);
            DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
            ViewBag.Date = cTime;
            var singers = db.Singers.Where(a => a.IsDeleted == false && a.IsActive == true && a.Parties.Any(p => p.Date.Value.Year == cTime.Year && (p.Date.Value.Month > cTime.Month || (p.Date.Value.Month == cTime.Month && p.Date.Value.Day >= cTime.Day)))).Include(a => a.Parties.Select(b => b.PartyBookings)).ToList();
            return View(singers);
        }
        public ActionResult PartyDetails(int PartyId)
        {
            DateTime utcNow = DateTime.UtcNow;
            TimeZone curTimeZone = TimeZone.CurrentTimeZone;
            // TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById(curTimeZone.StandardName);
            DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
            ViewBag.Date = cTime;
            ViewBag.PartyId = PartyId;
            var AvailableChairs = db.Chairs.Where(c => c.IsActive == true && c.IsDeleted == false && c.PartyId == PartyId && c.IsReserved != true).ToList();
            ViewBag.IsAvailableChairs = AvailableChairs.Count;
            //ViewBag.ChairsPrices = new SelectList(db.Chairs.Where(c => c.IsActive == true && c.IsDeleted == false && c.PartyId == PartyId && c.IsReserved != true).Select(b => new
            //{
            //    Id = Math.Round((double)b.Price, 2), //b.Id,
            //    Price = Math.Round((double)b.Price, 2)
            //}).Distinct().ToList(), "Id", "Price");
            ViewBag.GroupId = new SelectList(db.CustomersGroups.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");
            var Party = db.Parties.Where(a => a.IsDeleted == false && a.IsActive == true && a.Id == PartyId).FirstOrDefault();
            return View(Party);
        }
        public JsonResult GetAvailableChairs(int PartyId, decimal Price)
        {
            ViewBag.PartyId = PartyId;
            ViewBag.Price = Price;
            var AvailableChairs = db.Chairs.Where(c => c.IsActive == true && c.IsDeleted == false && c.PartyId == PartyId && c.IsReserved != true && c.Price == Price).Count();
            return Json(AvailableChairs, JsonRequestBehavior.AllowGet);
        }

        public ActionResult BookNow(int PartyId, decimal Price, int ChairsNo/*PartyBooking partyBooking,List<Chair>Chairs*/)
        {
            //PartyBooking partyBooking = (PartyBooking)TempData["PartyBooking"] ;

            //ViewBag.PartyId = partyBooking.PartyId;
            //ViewBag.Price = partyBooking.Total;
            //ViewBag.ChairsNo = partyBooking.ChairsNo;
            //partyBooking.Chairs = partyBooking.Chairs;
            //return View(partyBooking);
            ViewBag.PartyId = PartyId;
            ViewBag.Price = Price;
            ViewBag.ChairsNo = ChairsNo;
            return View();
        }
        [HttpPost]
        public ActionResult BookNow(int PartyId, decimal Price, int ChairsNo, List<Chair> Chairs)
        {
            DateTime utcNow = DateTime.UtcNow;
            TimeZone curTimeZone = TimeZone.CurrentTimeZone;
            // TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById(curTimeZone.StandardName);
            DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);

            double? perc = 0.14;
            var taxPercentage = db.TaxesPercentages.Where(t => t.IsActive == true && t.IsDeleted == false && t.DateFrom <= cTime && t.DateTo >= cTime).FirstOrDefault();
            if (taxPercentage != null)
                perc = (taxPercentage.TaxPercentage / 100);

            ViewBag.PartyId = PartyId;
            ViewBag.Price = Price;
            ViewBag.ChairsNo = ChairsNo;
            ViewBag.Chairs = Chairs;
            var PartyBooking = new PartyBooking();
            PartyBooking.BookingDate = cTime;
            PartyBooking.CustomerId = db.Customers.Where(a => a.IsActive == true && a.IsDeleted == false).FirstOrDefault().Id;
            PartyBooking.DocumentNumber = "1";
            PartyBooking.Total = Price;
            PartyBooking.Taxes = Price * (decimal?)perc;
            PartyBooking.TotalAfterTaxes = Price + (Price * (decimal?)perc);
            PartyBooking.IsDeleted = false;
            PartyBooking.PartyId = PartyId;
            PartyBooking.ChairsNo = ChairsNo;
            //PartyBooking.PaymentMethodId;
            //PartyBooking.OnlinePaymentNo;
            PartyBooking.Chairs = Chairs.ToList();

            TempData["PartyBooking"] = PartyBooking;

            //return RedirectToAction("BookNow",new {  PartyId,  Price,  ChairsNo });
            return Json(new{PartyId, Price, ChairsNo},JsonRequestBehavior.AllowGet);
            // return View(Chairs.ToList());
        }


        public JsonResult CompleteRegistration(int? CustomerId, int PartyId, decimal Price, int ChairsNo, int? PaymentMethodId, string Email, string OnlinePaymentId)
        {
            PartyBooking partyBooking = (PartyBooking)TempData["PartyBooking"];

            OnlinePaymentId = OnlinePaymentId == "undefined" ? null : OnlinePaymentId;
            if (CustomerId != null && Email != null)
            {
                var customer = db.Customers.Find(CustomerId);
                customer.Email = Email;
                db.Entry(customer).State = EntityState.Modified;
                db.SaveChanges();
            }
            ViewBag.PartyId = PartyId;
            ViewBag.Price = Price;
            ViewBag.ChairsNo = Price;

            DateTime utcNow = DateTime.UtcNow;
            TimeZone curTimeZone = TimeZone.CurrentTimeZone;
            // TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById(curTimeZone.StandardName);
            DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
            partyBooking.CustomerId = CustomerId;
            partyBooking.PaymentMethodId = PaymentMethodId;
            partyBooking.OnlinePaymentNo = OnlinePaymentId;

            //double? perc = 0.14;
            //var taxPercentage = db.TaxesPercentages.Where(t => t.IsActive == true && t.IsDeleted == false && t.DateFrom <= cTime && t.DateTo >= cTime).FirstOrDefault();
            //if (taxPercentage != null)
            //{
            //    perc = (taxPercentage.TaxPercentage / 100);
            //}
            //var Total = Price * ChairsNo;
            //var Taxes = Total * (decimal?)perc;
            //var TotalAfterTaxes = Total + (Taxes);
            //PartyBooking partyBooking = new PartyBooking();
            //partyBooking.DocumentNumber = "1";
            //partyBooking.PartyId = PartyId;
            //partyBooking.BookingDate = cTime;
            //partyBooking.PaymentMethodId = PaymentMethodId;
            //partyBooking.OnlinePaymentNo = OnlinePaymentId;
            //partyBooking.ChairPrice = Price;
            //partyBooking.ChairsNo = ChairsNo;
            //partyBooking.CustomerId = CustomerId;
            //partyBooking.Total = Total;
            //partyBooking.Taxes = Taxes;
            //partyBooking.TotalAfterTaxes = TotalAfterTaxes;
            //partyBooking.IsDeleted = false;
            return Json(partyBooking, JsonRequestBehavior.AllowGet);
        }


        public JsonResult showBookingDetails(int PartyId, decimal Price, int ChairsNo)
        {
            DateTime utcNow = DateTime.UtcNow;
            TimeZone curTimeZone = TimeZone.CurrentTimeZone;
            // TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById(curTimeZone.StandardName);
            DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);

            double? perc = 0.14;
            var taxPercentage = db.TaxesPercentages.Where(t => t.IsActive == true && t.IsDeleted == false && t.DateFrom <= cTime && t.DateTo >= cTime).FirstOrDefault();
            if (taxPercentage != null)
            { perc = (taxPercentage.TaxPercentage / 100); }
            //else
            //{
            //    perc = 0.14;
            //}
            var Total = Price /** ChairsNo*/;
            var Taxes = Total * (decimal?)perc;
            var TotalAfterTaxes = Total + (Taxes);
            var Details = db.Parties.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == PartyId).Select(a => new
            {
                a.Id,
                a.Date,
                a.FromTime,
                a.ToTime,
                Singer = a.Singer.ArName,
                ChairPrice = Price,
                ChairsNo = ChairsNo,
                Total = Total,
                Taxes = Taxes,
                TotalAfterTaxes = TotalAfterTaxes,
            }).FirstOrDefault();

            return Json(Details, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult StoreOnlinePaymentFile(string id, string jsonObj)
        {
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            var DeserializedObj = JsonConvert.DeserializeObject(jsonObj);

            //to Check If CV Name Exist before
            var file = Server.MapPath("/assets/images/Party/PayPalFile/" + id + ".txt").Replace('\\', '/');
            if (System.IO.File.Exists(file))
            {
                System.IO.File.Delete(file);
            }
            var jsonString = JsonConvert.SerializeObject(DeserializedObj);
            System.IO.File.WriteAllText(file, jsonString);

            return Json(id, JsonRequestBehavior.AllowGet);
        }

    }
}