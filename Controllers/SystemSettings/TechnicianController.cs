using DevExpress.Web.Mvc;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;

namespace MyERP.Controllers.SystemSettings
{
    public class TechnicianController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: Technician
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة الفنيين",
                EnAction = "Index",
                ControllerName = "Technician",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("Technician", "View", "Index", null, null, "الفنيين");

            ////////////////-----------------------------------------------------------------------
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            /////////////////////////// Search ////////////////////

            IQueryable<Techanician> techanicians;

            if (string.IsNullOrEmpty(searchWord))
            {
                techanicians = db.Techanicians.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);

                ViewBag.Count = db.Techanicians.Where(c => c.IsDeleted == false).Count();
            }
            else
            {
                techanicians = db.Techanicians.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) ||
                                                                          s.EnName.ToString().Contains(searchWord) || s.Phone1.Contains(searchWord) || s.Phone2.Contains(searchWord) || s.Address.Contains(searchWord) || s.AvgRate.ToString().Contains(searchWord) || s.Latitude.Contains(searchWord) || s.Longitude.Contains(searchWord) || s.VerificationCode.Contains(searchWord) || s.Notes.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = techanicians.Count();

                ///////////////////////////////////////////////////////////////////////////////

            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(techanicians.ToList());
        }
        [SkipERPAuthorize]
        public JsonResult SetCodeNum()
        {
            var code = QueryHelper.CodeLastNum("Techanician");
            return Json(code + 1, JsonRequestBehavior.AllowGet);
        }

        // GET: Technician/Edit/5
        public ActionResult AddEdit(int? id)
        {

            if (id == null)
            {
                Techanician NewObj = new Techanician();

                return View(NewObj);
            }
            Techanician Techanicians = db.Techanicians.Find(id);


            if (Techanicians == null)
            {
                return HttpNotFound();
            }


            ViewBag.Next = QueryHelper.Next((int)id, "Techanician");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Techanician");
            ViewBag.Last = QueryHelper.GetLast("Techanician");
            ViewBag.First = QueryHelper.GetFirst("Techanician");


            QueryHelper.AddLog(new MyLog()
            {

                ArAction = "فتح تفاصيل الفنيين",
                ControllerName = "Technician",

                EnAction = "AddEdit",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = Techanicians.Id,
                ArItemName = Techanicians.ArName,
                EnItemName = Techanicians.EnName,
                CodeOrDocNo = Techanicians.Code
            });

            return View(Techanicians);


        }

        // POST: Technician/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult AddEdit(Techanician techanician, string newBtn, HttpPostedFileBase[] upload)
        {
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            if (ModelState.IsValid)
            {
                var id = techanician.Id;
                techanician.IsDeleted = false;

                techanician.PlayerId = techanician.PlayerId;
                if (techanician.Id > 0)
                {
                    if (upload[0] != null)
                    {
                        upload[0].SaveAs(Server.MapPath("/images/TechnicianImages/") + upload[0].FileName);
                        techanician.Image = domainName + ("/images/TechnicianImages/") + upload[0].FileName;
                    }

                    if (upload[1] != null)

                    {
                        upload[1].SaveAs(Server.MapPath("/images/TechnicianImages/") + upload[1].FileName);
                        techanician.IdentityImage = domainName + ("/images/TechnicianImages/") + upload[1].FileName;
                    }

                    if (upload[2] != null)
                    {
                        upload[2].SaveAs(Server.MapPath("/images/TechnicianImages/") + upload[2].FileName);

                        techanician.CriminalChipsImage = domainName + ("/images/TechnicianImages/") + upload[2].FileName;

                    }

                    techanician.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                    db.Entry(techanician).State = EntityState.Modified;

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("technician", "Edit", "AddEdit", id, null, "الفنيين");

                    ///////////////-----------------------------------------------------------------------

                }
                else
                {
                    if (upload[0] != null)
                    {
                        upload[0].SaveAs(Server.MapPath("/images/TechnicianImages/") + upload[0].FileName);
                        techanician.Image = domainName + ("/images/TechnicianImages/") + upload[0].FileName;
                    }

                    if (upload[1] != null)

                    {
                        upload[1].SaveAs(Server.MapPath("/images/TechnicianImages/") + upload[1].FileName);
                        techanician.IdentityImage = domainName + ("/images/TechnicianImages/") + upload[1].FileName;
                    }

                    if (upload[2] != null)
                    {
                        upload[2].SaveAs(Server.MapPath("/images/TechnicianImages/") + upload[2].FileName);

                        techanician.CriminalChipsImage = domainName + ("/images/TechnicianImages/") + upload[2].FileName;

                    }
                    techanician.IsActive = true;

                    techanician.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);


                    db.Techanicians.Add(techanician);

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("technician", "Add", "AddEdit", techanician.Id, null, "الفنيين");

                    ///////////////-----------------------------------------------------------------------
                }
                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {

                    var mod = ModelState.First(c => c.Key == "Code");  // this

                    mod.Value.Errors.Add("هذا الكود موجود من قبل");


                    return View(techanician);
                }

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل الفنيين" : "اضافة الفنيين",
                    EnAction = "AddEdit",
                    ControllerName = "technician",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = techanician.Id,
                    ArItemName = techanician.ArName,
                    EnItemName = techanician.EnName,
                    CodeOrDocNo = techanician.Code
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

            else
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();
                return View(techanician);
            }
        }



        [SkipERPAuthorize]
        public ActionResult Panel(int? TechanicianId, DateTime? DateFrom, DateTime? DateTo, bool Search=false)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            DateTime utcNow = DateTime.UtcNow;
            TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
            ViewBag.DateFrom = cTime.ToString("yyyy-MM-ddTHH:mm");
            ViewBag.DateTo = cTime.ToString("yyyy-MM-ddTHH:mm");

            ViewBag.TechanicianId = new SelectList(db.Employees.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", TechanicianId);

            
            if (Search == true)
            {
                ViewBag.DateFrom = DateFrom != null ? DateFrom.Value.ToString("yyyy-MM-ddTHH:mm") : null;
                ViewBag.DateTo = DateTo != null ? DateTo.Value.ToString("yyyy-MM-ddTHH:mm") : null;

                var TechnicianOrders = db.GetTechnicianOrders(TechanicianId,DateFrom,DateTo);
                    return View(TechnicianOrders);
            }
            else
            {
                return View();
            }
        }

        [SkipERPAuthorize]
        public JsonResult GetTechnicianServices(int id)
        {
            var servParentIds = db.TechanicianServices.Where(x => x.TechanicianId == id && x.ServiceId != null && x.ServicesCategory.ParentId != null).Select(x => x.ServicesCategory.ParentId);
            var servParentParentIds = servParentIds.Union(db.ServicesCategories.Where(x => servParentIds.Contains(x.Id)).Select(x => x.ParentId)).Where(x => x != null);
            var services = db.ServicesCategories.Where(x => servParentParentIds.Distinct().Contains(x.Id)).Select(x => new
            {
                x.Id,
                x.ParentId,
                x.ArName,
                x.Code
            }).Distinct()
            .Union(db.TechanicianServices.Where(x => x.TechanicianId == id && x.ServiceId != null && x.ServicesCategory.ParentId != null).Select(x => new
            {
                Id = (int)x.ServiceId,
                x.ServicesCategory.ParentId,
                ArName = x.ServicesCategory.ArName,
                Code = x.ServicesCategory.Code
            })).Distinct();

            return Json(services, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult GetOrderDetails(int? TechanicianId, DateTime? DateFrom, DateTime? DateTo)
        {
            var OrderDetails = db.GetTechnicianOrderDetails(TechanicianId,DateFrom,DateTo);
            return Json(OrderDetails, JsonRequestBehavior.AllowGet);
        }


        // POST: Techanician/Delete/5
        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                Techanician techanician = db.Techanicians.Find(id);
                techanician.IsDeleted = true;
                db.Entry(techanician).State = EntityState.Modified;

                db.SaveChanges();


                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف الفنيين",
                    EnAction = "AddEdit",
                    ControllerName = "technician",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = techanician.Id,
                    EnItemName = techanician.EnName,
                    ArItemName = techanician.ArName,
                    CodeOrDocNo = techanician.Code
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("technician", "Delete", "Delete", id, null, "الفنيين");

                /////////////-----------------------------------------------------------------------

                return Content("true");
            }
            catch (Exception)
            {

                throw;
            }
        }

      
        [HttpPost]
        public ActionResult ActivateDeactivate(int id)
        {
            try
            {
                Techanician Techanicians = db.Techanicians.Find(id);
                if (Techanicians.IsActive == true)
                {
                    Techanicians.IsActive = false;
                }
                else
                {
                    Techanicians.IsActive = true;
                }

                db.Entry(Techanicians).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)Techanicians.IsActive ? "تنشيط الفنيين" : "إلغاء الفنيين",
                    EnAction = "AddEdit",
                    ControllerName = "Techanician",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = Techanicians.Id,
                    EnItemName = Techanicians.EnName,
                    ArItemName = Techanicians.ArName,
                    CodeOrDocNo = Techanicians.Code
                });
                ////-------------------- Notification-------------------------////
                if (Techanicians.IsActive == true)
                {
                    Notification.GetNotification("Technician", "Activate/Deactivate", "ActivateDeactivate", id, true, "الفنيين");
                }
                else
                {

                    Notification.GetNotification("Technician", "Activate/Deactivate", "ActivateDeactivate", id, false, "الفنيين");
                }
                //////////////-----------------------------------------------------------------------

                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        public ActionResult PrintTechnicianServices(int id)
        {
            ViewBag.id = id;
            ViewBag.TechName = db.Techanicians.Where(x => x.Id == id).Select(x => x.ArName).FirstOrDefault();
            return View();
        }


        [ValidateInput(false)]
        public ActionResult TreeListPartial(int id)
        {
            ViewBag.id = id;
            var servParentIds = db.TechanicianServices.Where(x => x.TechanicianId == id && x.ServiceId != null && x.ServicesCategory.ParentId != null).Select(x => x.ServicesCategory.ParentId);
            var servParentParentIds = servParentIds.Union(db.ServicesCategories.Where(x => servParentIds.Contains(x.Id)).Select(x => x.ParentId)).Where(x => x != null);
            var services = db.ServicesCategories.Where(x => servParentParentIds.Distinct().Contains(x.Id)).Select(x => new
            {
                x.Id,
                x.ParentId,
                x.ArName,
                x.Code
            }).Distinct()
            .Union(db.TechanicianServices.Where(x => x.TechanicianId == id && x.ServiceId != null && x.ServicesCategory.ParentId != null).Select(x => new
            {
                Id = (int)x.ServiceId,
                x.ServicesCategory.ParentId,
                ArName = x.ServicesCategory.ArName,
                Code = x.ServicesCategory.Code
            })).Distinct();
            //var model = db.ServicesCategories;
            return PartialView("~/Views/Technician/_TreeListPartial.cshtml", services.ToList());
        }
    }
}
