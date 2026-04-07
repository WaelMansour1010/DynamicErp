using MyERP.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Controllers.HotelManagement
{
    public class BookingInstructionController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: BookingInstruction
        public ActionResult Index()
        {
            BookingInstruction bookingInstruction = db.BookingInstructions.Any() ? db.BookingInstructions.FirstOrDefault() : new BookingInstruction();
            return View(bookingInstruction);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(BookingInstruction bookingInstruction)
        {
            var count = db.BookingInstructions.Count();
            if (count > 0)
            {
                db.Entry(bookingInstruction).State = EntityState.Modified;
            }
            else
            {
                db.BookingInstructions.Add(bookingInstruction);
            }
            db.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}