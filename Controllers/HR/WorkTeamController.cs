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
    public class WorkTeamController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        // GET: WorkTeam
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "قائمة فرق العمل",
                EnAction = "Index",
                ControllerName = "WorkTeam",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });

            //--------------------- Notification --------------------//
            Notification.GetNotification("WorkTeam", "View", "Index", null, null, "فرق العمل");

            //////////////////////////////////////////////////////////////////////////////////////
            //-------------------------------paging--------------------------------//
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            //////////////////////////////////////////////////////////////////////////////////////
            //------------------------------- Search ------------------------------------------------//
            IQueryable<WorkTeam> workTeams;
            if (string.IsNullOrEmpty(searchWord))
            {
                workTeams = db.WorkTeams.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.WorkTeams.Where(c => c.IsDeleted == false).Count();
            }
            else
            {
                workTeams = db.WorkTeams.Where(s => s.IsDeleted == false && (s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.Code.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.WorkTeams.Where(s => s.IsDeleted == false && (s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.Code.Contains(searchWord))).OrderBy(s => s.Id).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(workTeams.ToList());
        }

        //////////////////////////////////////////////////////////////////////////////////////
        //--------------------------- Authorization --------------------------------//
        [SkipERPAuthorize]
        public JsonResult SetCodeNum()
        {
            var code = QueryHelper.CodeLastNum("WorkTeam");
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
            WorkTeam workTeam = db.WorkTeams.Find(id);

            if (workTeam == null)
            {
                return HttpNotFound();
            }

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل فريق عمل ",
                EnAction = "AddEdit",
                ControllerName = "WorkTeam",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });



            ViewBag.Next = QueryHelper.Next((int)id, "WorkTeam");
            ViewBag.Previous = QueryHelper.Previous((int)id, "WorkTeam");
            ViewBag.Last = QueryHelper.GetLast("WorkTeam");
            ViewBag.First = QueryHelper.GetFirst("WorkTeam");


            return View(workTeam);
        }

        [HttpPost]
        public ActionResult AddEdit(WorkTeam workTeam, string newBtn)
        {
            if (ModelState.IsValid)
            {
                //--------------------------- Edit if there is id ------------------------//
                var id = workTeam.Id;
                workTeam.IsDeleted = false;
                if (workTeam.Id > 0)
                {
                    workTeam.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                    db.Entry(workTeam).State = EntityState.Modified;

                    ////-------------------- Notification for any edit -------------------------////
                    Notification.GetNotification("WorkTeam", "Edit", "AddEdit", id, null, "فرق العمل");
                }
                else
                {
                    workTeam.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    workTeam.Code = (QueryHelper.CodeLastNum("WorkTeam") + 1).ToString();
                    workTeam.IsActive = true;
                    db.WorkTeams.Add(workTeam);


                    ////-------------------- Notification for adding new shift -------------------------////
                    Notification.GetNotification("WorkTeam", "Add", "AddEdit", workTeam.Id, null, "فرق العمل");
                }

                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {

                    var mod = ModelState.First(c => c.Key == "Code");  // this
                    mod.Value.Errors.Add("هذا الكود موجود من قبل");
                    return View(workTeam);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل فريق عمل" : "اضافة فريق عمل",
                    EnAction = "AddEdit",
                    ControllerName = "WorkTeam",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = workTeam.Code

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

            return View(workTeam);
        }

        [HttpPost, ActionName("Delete")]

        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                WorkTeam workTeam = db.WorkTeams.Find(id);
                workTeam.IsDeleted = true;
                workTeam.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(workTeam).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف الدرجة الوظيفية",
                    EnAction = "AddEdit",
                    ControllerName = "WorkTeam",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("WorkTeam", "Delete", "Delete", id, null, "فرق العمل");
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
                WorkTeam workTeam = db.WorkTeams.Find(id);
                if (workTeam.IsActive == true)
                {
                    workTeam.IsActive = false;
                }
                else
                {
                    workTeam.IsActive = true;
                }
                workTeam.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(workTeam).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)workTeam.IsActive ? "تعديل فريق عمل" : "اضافة فريق عمل",
                    EnAction = "AddEdit",
                    ControllerName = "WorkTeam",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = workTeam.Id,
                });
                ////-------------------- Notification-------------------------////
                if (workTeam.IsActive == true)
                {

                    Notification.GetNotification("WorkTeam", "Activate/Deactivate", "ActivateDeactivate", id, true, "فرق العمل");
                }
                else
                {

                    Notification.GetNotification("WorkTeam", "Activate/Deactivate", "ActivateDeactivate", id, false, "فرق العمل");
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