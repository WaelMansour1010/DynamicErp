using MyERP.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Controllers.HR
{
    public class OfficialVacationController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: OfficialVacation
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "قائمة الإجازات الرسمية",
                EnAction = "Index",
                ControllerName = "OfficialVacation",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });

            //--------------------- Notification --------------------//
            Notification.GetNotification("OfficialVacation", "View", "Index", null, null, "الإجازات الرسمية");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<OfficialVacation> officialVacations;
            if (string.IsNullOrEmpty(searchWord))
            {
                officialVacations = db.OfficialVacations.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.OfficialVacations.Where(c => c.IsDeleted == false).Count();
            }
            else
            {
                officialVacations = db.OfficialVacations.Where(s => s.IsDeleted == false && (s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.Code.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.OfficialVacations.Where(s => s.IsDeleted == false && (s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.Code.Contains(searchWord))).OrderBy(s => s.Id).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(officialVacations.ToList());
        }

        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {
                var Code = SetCodeNum();
                ViewBag.Code = int.Parse(Code.Data.ToString());
                return View();
            }
            OfficialVacation officialVacation = db.OfficialVacations.Find(id);
            if (officialVacation == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل إجازة رسمية ",
                EnAction = "AddEdit",
                ControllerName = "OfficialVacation",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });
            ViewBag.Next = QueryHelper.Next((int)id, "OfficialVacation");
            ViewBag.Previous = QueryHelper.Previous((int)id, "OfficialVacation");
            ViewBag.Last = QueryHelper.GetLast("OfficialVacation");
            ViewBag.First = QueryHelper.GetFirst("OfficialVacation");
            //StartDate
            ViewBag.StartDate = officialVacation.StartDate.Value.ToString("yyyy-MM-ddTHH:mm");
            //EndDate
            ViewBag.EndDate = officialVacation.EndDate.Value.ToString("yyyy-MM-ddTHH:mm");
            return View(officialVacation);
        }

        [HttpPost]
        public ActionResult AddEdit(OfficialVacation officialVacation, string newBtn)
        {
            if (ModelState.IsValid)
            {
                //--------------------------- Edit if there is id ------------------------//
                var id = officialVacation.Id;
                officialVacation.IsDeleted = false;
                if (officialVacation.Id > 0)
                {
                    officialVacation.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    //----- To Edit Shifts Within Official Vacations -------//
                    var old = db.ShiftOfficialVacations.Where(a => a.OfficialVacationId == id).ToList();
                    foreach (var i in old)
                    {
                        i.DateFrom = officialVacation.StartDate;
                        i.DateTo = officialVacation.EndDate;
                        i.OfficialVacationId = officialVacation.Id;
                        i.ShiftId = i.ShiftId;
                       // i.Id = i.Id;
                    }
                    //-------------------------------------------------------------------//
                    db.Entry(officialVacation).State = EntityState.Modified;
                    ////-------------------- Notification for any edit -------------------------////
                    Notification.GetNotification("OfficialVacation", "Edit", "AddEdit", id, null, "الإجازات الرسمية");
                }
                else
                {
                    officialVacation.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    officialVacation.Code = (QueryHelper.CodeLastNum("OfficialVacation") + 1).ToString();
                    officialVacation.IsActive = true;
                    //----- To Add Shifts Within Official Vacations -------//
                    var shiftIds = db.Shifts.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => a.Id).ToList();
                    var shiftOfficialVacations = new List<ShiftOfficialVacation>();
                    foreach (var i in shiftIds)
                    {
                        var shiftOfficialVacation = new ShiftOfficialVacation();
                        shiftOfficialVacation.ShiftId = i;
                        shiftOfficialVacation.OfficialVacationId = officialVacation.Id;
                        shiftOfficialVacation.DateFrom = officialVacation.StartDate;
                        shiftOfficialVacation.DateTo = officialVacation.EndDate;
                        shiftOfficialVacations.Add(shiftOfficialVacation);
                    }
                    officialVacation.ShiftOfficialVacations = shiftOfficialVacations;
                    //-------------------------------------------------------------------//
                    db.OfficialVacations.Add(officialVacation);
                    ////-------------------- Notification for adding new shift -------------------------////
                    Notification.GetNotification("OfficialVacation", "Add", "AddEdit", officialVacation.Id, null, "الإجازات الرسمية");
                }
                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    var mod = ModelState.First(c => c.Key == "Code");  // this
                    mod.Value.Errors.Add("هذا الكود موجود من قبل");
                    return View(officialVacation);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل إجازة رسمية" : "اضافة إجازة رسمية",
                    EnAction = "AddEdit",
                    ControllerName = "OfficialVacation",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = officialVacation.Code
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

            return View(officialVacation);
        }

        [SkipERPAuthorize]
        public JsonResult SetCodeNum()
        {
            var code = QueryHelper.CodeLastNum("OfficialVacation");
            return Json(code + 1, JsonRequestBehavior.AllowGet);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                OfficialVacation officialVacation = db.OfficialVacations.Find(id);
                officialVacation.IsDeleted = true;
                officialVacation.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(officialVacation).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف الإجازة الرسمية",
                    EnAction = "AddEdit",
                    ControllerName = "OfficialVacation",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("OfficialVacation", "Delete", "Delete", id, null, "الإجازات الرسمية");
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
                OfficialVacation officialVacation = db.OfficialVacations.Find(id);
                if (officialVacation.IsActive == true)
                {
                    officialVacation.IsActive = false;
                }
                else
                {
                    officialVacation.IsActive = true;
                }
                officialVacation.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(officialVacation).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)officialVacation.IsActive ? "تعديل إجازة رسمية" : "اضافة إجازة رسمية",
                    EnAction = "AddEdit",
                    ControllerName = "OfficialVacation",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = officialVacation.Id,
                });
                ////-------------------- Notification-------------------------////
                if (officialVacation.IsActive == true)
                {

                    Notification.GetNotification("OfficialVacation", "Activate/Deactivate", "ActivateDeactivate", id, true, "الإجازات الرسمية");
                }
                else
                {

                    Notification.GetNotification("OfficialVacation", "Activate/Deactivate", "ActivateDeactivate", id, false, "الإجازات الرسمية");
                }

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

    }
}