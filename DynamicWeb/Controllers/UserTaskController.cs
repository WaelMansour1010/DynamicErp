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
    public class UserTaskController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: Task
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة المهام",
                EnAction = "Index",
                ControllerName = "UserTask",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });

           // Notification.GetNotification("UserTask", "View", "Index", null, null, "المهام");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }

            IQueryable<UserTask> tasks;
            if (string.IsNullOrEmpty(searchWord))
            {
                tasks = db.UserTasks.Where(a => a.IsDeleted == false).OrderByDescending(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.UserTasks.Where(a => a.IsDeleted == false).Count();
            }
            else
            {
                tasks = db.UserTasks.Where(a => a.IsDeleted == false &&
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.ERPUser.Name.Contains(searchWord) || a.ERPUser1.Name.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.UserTasks.Where(a => a.IsDeleted == false && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord) || a.ERPUser.Name.Contains(searchWord) || a.ERPUser1.Name.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;

            return View(tasks.ToList());
        }

        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {
                var Code = SetCodeNum();
                ViewBag.Code = int.Parse(Code.Data.ToString());

                ViewBag.AssignedTo = new SelectList(db.ERPUsers.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    b.Id,
                    Name = b.UserName
                }), "Id", "Name");



                ViewBag.StatusId = new SelectList(db.TaskStatus.Where(a => a.Id < 5).Select(b => new
                {
                    b.Id,
                    ArName = b.ArName
                }), "Id", "ArName");



                return View();
            }
            UserTask task = db.UserTasks.Find(id);
            if (task == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل المهمة",
                EnAction = "AddEdit",
                ControllerName = "UserTask",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });

            ViewBag.AssignedTo = new SelectList(db.ERPUsers.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                b.Id,
                Name = b.UserName
            }), "Id", "Name", task.AssignedTo);

            ViewBag.StatusId = new SelectList(db.TaskStatus.Select(b => new
            {
                b.Id,
                ArName = b.ArName
            }), "Id", "ArName", task.StatusId);

            ViewBag.TaskLogs = db.TaskLogs.Where(a => a.TaskId == task.Id).ToList();

            ViewBag.Next = QueryHelper.Next((int)id, "UserTask");
            ViewBag.Previous = QueryHelper.Previous((int)id, "UserTask");
            ViewBag.Last = QueryHelper.GetLast("UserTask");
            ViewBag.First = QueryHelper.GetFirst("UserTask");





            return View(task);
        }

        [SkipERPAuthorize]
        [HttpPost]
        public ActionResult AddEdit(UserTask task, bool? changeStatus = false)
        {

            DateTime utcNow = DateTime.UtcNow;
            TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);

            if (ModelState.IsValid)
            {
                var id = task.Id;
                task.IsDeleted = false;

                if (task.Id > 0)
                {
                    if (changeStatus == true) // we passed it from ajax request to change Only StatusId 
                    {
                        var StatusId = task.StatusId;
                        task = db.UserTasks.Find(id);
                        task.StatusId = StatusId;
                    }
                    task.CreatedBy = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    db.Entry(task).State = EntityState.Modified;
                  //  Notification.GetNotification("UserTask", "Edit", "AddEdit", task.Id, null, "المهام");

                    if (task.StatusId == 4)
                    {
                        var FinishedDate = db.UserTasks.Where(a => a.Id == task.Id && a.IsDeleted == false).FirstOrDefault().FinishedDate;
                        if (FinishedDate == null)
                        {
                            task.FinishedDate = cTime;
                        }
                    }
                    else
                    {
                        task.FinishedDate = null;
                    }



                    var LastStatus = db.TaskLogs.Where(a => a.TaskId == task.Id).OrderByDescending(a => a.Id).FirstOrDefault().StatusId;
                    if (LastStatus != null)
                    {
                        if (LastStatus != task.StatusId)
                        {
                            TaskLog TL = new TaskLog();
                            TL.TaskId = task.Id;
                            TL.StatusId = task.StatusId;
                            TL.UserId = task.CreatedBy;
                            TL.Date = cTime;
                            db.TaskLogs.Add(TL);
                        }
                    }
                }
                else
                {

                    task.CreatedDate = cTime;
                    task.CreatedBy = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    task.Code = (QueryHelper.CodeLastNum("UserTask") + 1).ToString();
                    db.UserTasks.Add(task);
                    TaskLog TL = new TaskLog();
                    TL.TaskId = task.Id;
                    TL.StatusId = task.StatusId;
                    TL.UserId = task.CreatedBy;
                    TL.Date = cTime;
                    db.TaskLogs.Add(TL);
                  //  Notification.GetNotification("UserTask", "Add", "AddEdit", id, null, "المهام");

                }

                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    var mod = ModelState.First(c => c.Key == "Code");  // this
                    mod.Value.Errors.Add("هذا الكود موجود من قبل");
                    return View(task);
                }
                id = task.Id;
                db.Sp_OccuredNotification(11517, task.ArName, cTime, id, task.AssignedTo);

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل مهمة" : "اضافة مهمة",
                    EnAction = "AddEdit",
                    ControllerName = "UserTask",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = task.Code
                });

                return Json(new { success = "true" });
            }


            ViewBag.AssignedTo = new SelectList(db.ERPUsers.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                b.Id,
                Name = b.UserName
            }), "Id", "Name", task.AssignedTo);

            ViewBag.StatusId = new SelectList(db.TaskStatus.Select(b => new
            {
                b.Id,
                ArName = b.ArName
            }), "Id", "ArName", task.StatusId);

            return View(task);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                UserTask task = db.UserTasks.Find(id);
                task.IsDeleted = true;
                task.CreatedBy = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(task).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف مهمة",
                    EnAction = "AddEdit",
                    ControllerName = "UserTask",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = task.EnName

                });
               // Notification.GetNotification("UserTask", "Delete", "Delete", id, null, "المهام");


                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

        }


        [SkipERPAuthorize]
        public JsonResult SetCodeNum()
        {
            var code = QueryHelper.CodeLastNum("UserTask");
            return Json(code + 1, JsonRequestBehavior.AllowGet);
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