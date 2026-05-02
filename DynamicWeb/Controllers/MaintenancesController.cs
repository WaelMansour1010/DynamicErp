using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using MyERP.Models;

namespace MyERP.Controllers
{
    
   [ERPAuthorize]
    public class MaintenancesController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: Maintenances
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة اٍستلام الصيانات",
                EnAction = "Index",
                ControllerName = "Maintenances",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("Maintenances", "View", "Index", null, null, "الصيانات");

            ////////////////-----------------------------------------------------------------------

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            /////////////////////////// Search ////////////////////

            IQueryable<Maintenance> maintenances;

            if (string.IsNullOrEmpty(searchWord))
            {
                maintenances = db.Maintenances.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);

                ViewBag.Count = db.Maintenances.Where(c => c.IsDeleted == false).Count();
            }
            else
           {
                maintenances = db.Maintenances.Where(s => s.IsDeleted == false && (s.Item.ArName.Contains(searchWord) || s.ServicelNumber.Contains(searchWord) || s.Customer.ArName.Contains(searchWord) || s.Date.ToString().Contains(searchWord) || s.Barcode.Contains(searchWord) || s.ClientProblem.Contains(searchWord) || s.Appearance.Contains(searchWord) || s.SalesInvoiceDate.ToString().Contains(searchWord) || s.PurchaseDate.ToString().Contains(searchWord) || s.PurchaseInvoice.DocumentNumber.Contains(searchWord) || s.SalesInvoice.DocumentNumber.Contains(searchWord) || s.SalesPrice.ToString().Contains(searchWord) || s.Vendor.ArName.Contains(searchWord) || s.SerialNumber.Contains(searchWord) || s.InvoiceNumberOfVendor.ToString().Contains(searchWord) || s.Notes.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
               ViewBag.Count = maintenances.Count();
               ///////////////////////////////////////////////////////////////////////////////

           }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(maintenances.ToList());

        }
        
        // GET: Maintenances/Edit/5
        public ActionResult AddEdit(int? id)
        {

            if (id == null)
            {
                Maintenance Newobj = new Maintenance();
             
                //db.Maintenances.Where(d => d.IsDeleted == false && d.IsActive == true).Select(b => new {
                //    Id = b.Id,
                //    ArName = ""
                //}), "Id", "ArName");
               
                ViewBag.CustomerId = new SelectList(db.Customers.Where(d => d.IsDeleted == false && d.IsActive == true).Select(b => new {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.DamagedItemsId = new SelectList(db.Items.Where(d => d.IsDeleted == false && d.IsActive == true).Select(b => new {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.VendorId = new SelectList(db.Vendors.Where(d => d.IsDeleted == false && d.IsActive == true).Select(b => new {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                return View(Newobj);
            }
            Maintenance maintenance = db.Maintenances.Find(id);
            if (maintenance == null)
            {
                return HttpNotFound();
            }


            ViewBag.CustomerId = new SelectList(db.Customers.Where(d => d.IsDeleted == false && d.IsActive == true).Select(b => new {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", maintenance.CustomerID);
            ViewBag.DamagedItemsId = new SelectList(db.Items.Where(d => d.IsDeleted == false && d.IsActive == true).Select(b => new {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", maintenance.DamagedItemsId);
            ViewBag.VendorId = new SelectList(db.Vendors.Where(d => d.IsDeleted == false && d.IsActive == true).Select(b => new {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", maintenance.VendorId);


            ViewBag.Next = QueryHelper.Next((int)id, "Maintenance");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Maintenance");
            ViewBag.Last = QueryHelper.GetLast("Maintenance");
            ViewBag.First = QueryHelper.GetFirst("Maintenance");

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل الصيانات",
                EnAction = "AddEdit",
                ControllerName = "Maintenance",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = maintenance.Id
               
            });
         
            try
            {
                ViewBag.Date = maintenance.Date.Value.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.PurchaseDate = maintenance.PurchaseDate.Value.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.SalesInvoiceDate = maintenance.SalesInvoiceDate.Value.ToString("yyyy-MM-ddTHH:mm");

            }
            catch (Exception)
            {
            }
            return View(maintenance);
        }

        // POST: Maintenances/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]

        public ActionResult AddEdit(Maintenance maintenance, string newBtn, HttpPostedFileBase upload)
        {
            if (ModelState.IsValid)
            {
                var id = maintenance.Id;
                string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
                if (upload != null)
                {
                    upload.SaveAs(Server.MapPath("/Attachments/") + upload.FileName);

                    maintenance.AttachedFiles = domainName + ("/Attachments/") + upload.FileName;

                }

                maintenance.IsDeleted = false;
                if (maintenance.Id > 0)
                {

                    db.Entry(maintenance).State = EntityState.Modified;
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("Maintenances", "Edit", "AddEdit", id, null, "الصيانات");

                    //////////////-----------------------------------------------------------------------

                }
                else
                {
                  
                    maintenance.IsActive = true;
                    db.Maintenances.Add(maintenance);

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("Maintenances", "Add", "AddEdit", maintenance.Id, null, "الصيانات");

                    ///////////-----------------------------------------------------------------------


                }
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id> 0 ? "تعديل الصيانة" : "اضافة الصيانة",
                    EnAction = "AddEdit",
                    ControllerName = "maintenance",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = maintenance.Id ,
                  
                });

                if (newBtn == "saveAndNew")
                {
                    return RedirectToAction("AddEdit");

                }
                else
                {
                    return RedirectToAction("Index");
                }
            }
           

            ViewBag.CustomerId = new SelectList(db.Customers.Where(d => d.IsDeleted == false && d.IsActive == true).Select(b => new {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", maintenance.CustomerID);
            ViewBag.DamagedItemsId = new SelectList(db.Items.Where(d => d.IsDeleted == false && d.IsActive == true).Select(b => new {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", maintenance.DamagedItemsId);
            ViewBag.VendorId = new SelectList(db.Vendors.Where(d => d.IsDeleted == false && d.IsActive == true).Select(b => new {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", maintenance.VendorId);

            ViewBag.Next = QueryHelper.Next(maintenance.Id, "maintenance");
            ViewBag.Previous = QueryHelper.Previous(maintenance.Id, "maintenance");
            ViewBag.Last = QueryHelper.GetLast("maintenance");
            ViewBag.First = QueryHelper.GetFirst("maintenance");
            return View(maintenance);

        }
        public FileContentResult MyAction(string path)
        {
            path = Path.Combine(Server.MapPath("~/Attachments/") + path);
            byte[] attachmentBytes = System.IO.File.ReadAllBytes(path);
            return new FileContentResult(attachmentBytes, "txt");
        }
        // GET: Maintenances/Delete/5
        [HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                Maintenance maintenance = db.Maintenances.Find(id);
                maintenance.IsDeleted = true;
                db.Entry(maintenance).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف اٍستلام الصيانات",
                    EnAction = "AddEdit",
                    ControllerName = "Maintenances",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id
                   
                });

                ////-------------------- Notification-------------------------////
                Notification.GetNotification("Maintenances", "Delete", "Delete", id, null, "الصيانات");

                ///////////-----------------------------------------------------------------------

                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }


        [HttpPost]
        public ActionResult ActivateDeactivate(int id)
        {
            try
            {
                Maintenance maintenance = db.Maintenances.Find(id);
                if (maintenance.IsActive == true)
                {
                    maintenance.IsActive = false;
                }
                else
                {
                    maintenance.IsActive = true;
                }

                db.Entry(maintenance).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)maintenance.IsActive ? "تنشيط اٍستلام الصيانة" : "إلغاء اٍستلام الصيانة",
                    EnAction = "AddEdit",
                    ControllerName = "Maintenances",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = maintenance.Id
                    
                });
                ////-------------------- Notification-------------------------////
                if (maintenance.IsActive == true)
                {
                    Notification.GetNotification("Maintenances", "Activate/Deactivate", "ActivateDeactivate", id, true, "الصيانات");
                }
                else
                {

                    Notification.GetNotification("Maintenances", "Activate/Deactivate", "ActivateDeactivate", id, false, "الصيانات");
                }
                ///////////-----------------------------------------------------------------------

                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }
        public JsonResult Serial(string Num )
        {

            try
            {
             var PurchaseSaleSerialNumber   = db.PurchaseSaleSerialNumbers.Where(s => s.SerialNumber == Num && s.PageSourceId == 57).FirstOrDefault();
            if (PurchaseSaleSerialNumber != null)
            {
                var selectedidpurId = PurchaseSaleSerialNumber.SelectedId;
                int? selectedidSalId = db.PurchaseSaleSerialNumbers.FirstOrDefault(s => s.SerialNumber == Num && s.PageSourceId == 58).SelectedId;
                int? Vendor = db.PurchaseInvoices.FirstOrDefault(c => c.Id == selectedidpurId).VendorOrCustomerId;
                var DocumentNumberPur = db.PurchaseInvoices.FirstOrDefault(c => c.Id == selectedidpurId).DocumentNumber;

                var DocumentNumberSal = db.SalesInvoices.FirstOrDefault(c => c.Id == selectedidSalId).DocumentNumber;
                var Customer = db.SalesInvoices.FirstOrDefault(c => c.Id == selectedidSalId).VendorOrCustomerId;
                return Json(new { Vendor = Vendor, selectedidSalId = selectedidSalId, selectedidpurId = selectedidpurId, DocumentNumberPur = DocumentNumberPur, Customer = Customer, DocumentNumberSal = DocumentNumberSal},
                    JsonRequestBehavior.AllowGet);

            }
            }
            catch 
            {
                return Json("error",
                JsonRequestBehavior.AllowGet);
              
            }

            return Json("error",
                JsonRequestBehavior.AllowGet);


        }
        public JsonResult item(int? Num )
        {
            var itemprices = db.ItemPrices.Where(c => c.ItemId == Num).FirstOrDefault();
            if (itemprices!=null)
            {
var Price = db.ItemPrices.FirstOrDefault(c => c.ItemId == Num).Price;
            var Barcode = db.ItemPrices.FirstOrDefault(c => c.ItemId == Num).Barcode;
            return Json(new { Price = Price, Barcode= Barcode },
                JsonRequestBehavior.AllowGet);
            }
            return Json("error",
                JsonRequestBehavior.AllowGet);
           
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
