using MyERP.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Controllers
{
    public class JobGradeController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        // GET: JobGrade
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "قائمة الدرجات الوظيفية",
                EnAction = "Index",
                ControllerName = "JobGrade",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });

            //--------------------- Notification --------------------//
            Notification.GetNotification("JobGrade", "View", "Index", null, null, "الدرجات الوظيفية");

            //////////////////////////////////////////////////////////////////////////////////////
            //-------------------------------paging--------------------------------//
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            //////////////////////////////////////////////////////////////////////////////////////
            //------------------------------- Search ------------------------------------------------//
            IQueryable<JobGrade> jobGrades;
            if (string.IsNullOrEmpty(searchWord))
            {
                jobGrades = db.JobGrades.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.JobGrades.Where(c => c.IsDeleted == false).Count();
            }
            else
            {
                jobGrades = db.JobGrades.Where(s => s.IsDeleted == false && (s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.Code.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.JobGrades.Where(s => s.IsDeleted == false && (s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.Code.Contains(searchWord))).OrderBy(s => s.Id).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(jobGrades.ToList());
        }

        //////////////////////////////////////////////////////////////////////////////////////
        //--------------------------- Authorization --------------------------------//
        [SkipERPAuthorize]
        public JsonResult SetCodeNum()
        {
            var code = QueryHelper.CodeLastNum("JobGrade");
            return Json(code + 1, JsonRequestBehavior.AllowGet);
        }

        //////////////////////////////////////////////////////////////////////////////////////
        //--------------------------------- Add Or Edit -----------------------------------//
        public ActionResult AddEdit(int? id)
        {

            if (id == null)
            {
                var Code = SetCodeNum();
                ViewBag.Code = int.Parse(Code.Data.ToString());
                return View();
            }
            JobGrade jobGrade = db.JobGrades.Find(id);

            if (jobGrade == null)
            {
                return HttpNotFound();
            }

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل درجة وظيفية ",
                EnAction = "AddEdit",
                ControllerName = "JobGrade",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });



            ViewBag.Next = QueryHelper.Next((int)id, "JobGrade");
            ViewBag.Previous = QueryHelper.Previous((int)id, "JobGrade");
            ViewBag.Last = QueryHelper.GetLast("JobGrade");
            ViewBag.First = QueryHelper.GetFirst("JobGrade");


            return View(jobGrade);
        }

        [HttpPost]
        public ActionResult AddEdit(JobGrade jobGrade, string newBtn)
        {
            if (ModelState.IsValid)
            {
                //--------------------------- Edit if there is id ------------------------//
                var id = jobGrade.Id;
                jobGrade.IsDeleted = false;
                if (jobGrade.Id > 0)
                {
                    jobGrade.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                    db.Entry(jobGrade).State = EntityState.Modified;

                    ////-------------------- Notification for any edit -------------------------////
                    Notification.GetNotification("JobGrade", "Edit", "AddEdit", id, null, "الدرجات الوظيفية");
                }
                else
                {
                    jobGrade.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    jobGrade.Code = (QueryHelper.CodeLastNum("JobGrade") + 1).ToString();
                    jobGrade.IsActive = true;
                    db.JobGrades.Add(jobGrade);


                    ////-------------------- Notification for adding new shift -------------------------////
                    Notification.GetNotification("JobGrade", "Add", "AddEdit", jobGrade.Id, null, "الدرجات الوظيفية");
                }

                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {

                    var mod = ModelState.First(c => c.Key == "Code");  // this
                    mod.Value.Errors.Add("هذا الكود موجود من قبل");
                    return View(jobGrade);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل درجة وظيفية" : "اضافة درجة وظيفية",
                    EnAction = "AddEdit",
                    ControllerName = "JobGrade",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = jobGrade.Code

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

            return View(jobGrade);
        }

        [HttpPost, ActionName("Delete")]

        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                JobGrade jobGrade = db.JobGrades.Find(id);
                jobGrade.IsDeleted = true;
                jobGrade.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(jobGrade).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف الدرجة الوظيفية",
                    EnAction = "AddEdit",
                    ControllerName = "JobGrade",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("JobGrade", "Delete", "Delete", id, null, "الدرجات الوظيفية");
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
                JobGrade jobGrade = db.JobGrades.Find(id);
                if (jobGrade.IsActive == true)
                {
                    jobGrade.IsActive = false;
                }
                else
                {
                    jobGrade.IsActive = true;
                }
                jobGrade.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(jobGrade).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)jobGrade.IsActive ? "تعديل درجة وظيفية" : "اضافة درجة وظيفية",
                    EnAction = "AddEdit",
                    ControllerName = "JobGrade",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = jobGrade.Id,
                });
                ////-------------------- Notification-------------------------////
                if (jobGrade.IsActive == true)
                {

                    Notification.GetNotification("JobGrade", "Activate/Deactivate", "ActivateDeactivate", id, true, "الدرجات الوظيفية");
                }
                else
                {

                    Notification.GetNotification("JobGrade", "Activate/Deactivate", "ActivateDeactivate", id, false, "الدرجات الوظيفية");
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