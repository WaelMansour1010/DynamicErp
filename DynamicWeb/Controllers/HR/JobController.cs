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
    public class JobController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        // GET: Job
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "قائمة الوظائف",
                EnAction = "Index",
                ControllerName = "Job",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });

            //--------------------- Notification --------------------//
            Notification.GetNotification("Job", "View", "Index", null, null, "الوظائف");

            //////////////////////////////////////////////////////////////////////////////////////
            //-------------------------------paging--------------------------------//
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            //////////////////////////////////////////////////////////////////////////////////////
            //------------------------------- Search ------------------------------------------------//
            IQueryable<Job> jobs;
            if (string.IsNullOrEmpty(searchWord))
            {
                jobs = db.Jobs.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Jobs.Where(c => c.IsDeleted == false).Count();
            }
            else
            {
                jobs = db.Jobs.Where(s => s.IsDeleted == false && (s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.Code.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Jobs.Where(s => s.IsDeleted == false && (s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.Code.Contains(searchWord))).OrderBy(s => s.Id).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(jobs.ToList());
        }

        //////////////////////////////////////////////////////////////////////////////////////
        //--------------------------- Authorization --------------------------------//
        [SkipERPAuthorize]
        public JsonResult SetCodeNum()
        {
            var code = QueryHelper.CodeLastNum("Job");
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
            Job job = db.Jobs.Find(id);

            if (job == null)
            {
                return HttpNotFound();
            }

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل وظيفة ",
                EnAction = "AddEdit",
                ControllerName = "Job",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });



            ViewBag.Next = QueryHelper.Next((int)id, "Job");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Job");
            ViewBag.Last = QueryHelper.GetLast("Job");
            ViewBag.First = QueryHelper.GetFirst("Job");


            return View(job);
        }

        [HttpPost]
        public ActionResult AddEdit(Job job , string newBtn)
        {
            if (ModelState.IsValid)
            {
                //--------------------------- Edit if there is id ------------------------//
                var id = job.Id;
                job.IsDeleted = false;
                if (job.Id > 0)
                {
                    job.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                    db.Entry(job).State = EntityState.Modified;

                    ////-------------------- Notification for any edit -------------------------////
                    Notification.GetNotification("Job", "Edit", "AddEdit", id, null, "الوظائف");
                }
                else
                {
                    job.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    job.Code = (QueryHelper.CodeLastNum("Job") + 1).ToString();
                    job.IsActive = true;
                    db.Jobs.Add(job);


                    ////-------------------- Notification for adding new shift -------------------------////
                    Notification.GetNotification("Job", "Add", "AddEdit", job.Id, null, "الوظائف");
                }

                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {

                    var mod = ModelState.First(c => c.Key == "Code");  // this
                    mod.Value.Errors.Add("هذا الكود موجود من قبل");
                    return View(job);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل وظيفة" : "اضافة وظيفة",
                    EnAction = "AddEdit",
                    ControllerName = "Job",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = job.Code

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

            return View(job);
        }

        [HttpPost, ActionName("Delete")]

        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                Job job = db.Jobs.Find(id);
                job.IsDeleted = true;
                job.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(job).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف الوظيفة",
                    EnAction = "AddEdit",
                    ControllerName = "Job",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("Job", "Delete", "Delete", id, null, "الوظائف");
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
                Job job = db.Jobs.Find(id);
                if (job.IsActive == true)
                {
                    job.IsActive = false;
                }
                else
                {
                    job.IsActive = true;
                }
                job.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(job).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)job.IsActive ? "تعديل وظيفة" : "اضافة وظيفة",
                    EnAction = "AddEdit",
                    ControllerName = "Job",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = job.Id,
                });
                ////-------------------- Notification-------------------------////
                if (job.IsActive == true)
                {

                    Notification.GetNotification("Job", "Activate/Deactivate", "ActivateDeactivate", id, true, "الوظائف");
                }
                else
                {

                    Notification.GetNotification("Job", "Activate/Deactivate", "ActivateDeactivate", id, false, "الوظائف");
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