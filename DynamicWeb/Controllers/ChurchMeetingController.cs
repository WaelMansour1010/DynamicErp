using System;
using System.Collections.Generic;
 
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using MyERP.Repository;

namespace MyERP.Controllers
{
    public class ChurchMeetingController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: ChurchMeeting
        public ActionResult Index(DateTime? DateFrom, DateTime? DateTo, bool? IsSearch, int? FatherId, int pageIndex = 1, int wantedRowsNo = 10/*, string searchWord = ""*/)
        {
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            ViewBag.domainName = domainName;
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة المقابلات",
                EnAction = "Index",
                ControllerName = "ChurchMeeting",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            Notification.GetNotification("ChurchMeeting", "View", "Index", null, null, "المقابلات");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }
            IQueryable<ChurchMeeting> meetings;
            if (/*string.IsNullOrEmpty(searchWord)*/ IsSearch != true)
            {
                meetings = db.ChurchMeetings.Where(a => a.IsDeleted == false && (FatherId == 0 || a.ChurchMembership.ChurchFatherId == FatherId)).OrderByDescending(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.ChurchMeetings.Where(a => a.IsDeleted == false && (FatherId == 0 || a.ChurchMembership.ChurchFatherId == FatherId)).Count();
            }
            else
            {
                //meetings = db.ChurchMeetings.Where(a => a.IsDeleted == false &&
                //(a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                //    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                //ViewBag.Count = db.ChurchMeetings.Where(a => a.IsDeleted == false
                //&& (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
                var PrevMeetings = db.GetChurchMeetingInPeriod(DateFrom, DateTo, FatherId).OrderByDescending(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo).ToList();
                var Count = db.GetChurchMeetingInPeriod(DateFrom, DateTo, FatherId).Count();

                return Json(new { PrevMeetings, DateFrom, DateTo, wantedRowsNo, Count, pageIndex, domainName }, JsonRequestBehavior.AllowGet);

            }
            //ViewBag.searchWord = searchWord;
            ViewBag.DateFrom = DateFrom;
            ViewBag.DateTo = DateTo;
            ViewBag.wantedRowsNo = wantedRowsNo;
            ViewBag.FatherId = new SelectList(db.ChurchFathers.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", FatherId);
            return View(meetings.ToList());
        }

        public ActionResult AddEdit(int? id)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            if (id == null)
            {
                ViewBag.AreaId = new SelectList(db.Areas.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                var CurrentFatherId = db.ChurchFathers.Where(a => a.IsActive == true && a.IsDeleted == false && a.ERPUserId == userId).FirstOrDefault()!=null? db.ChurchFathers.Where(a => a.IsActive == true && a.IsDeleted == false && a.ERPUserId == userId).FirstOrDefault().Id:0;
                ViewBag.ChurchFatherId = new SelectList(db.ChurchFathers.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", CurrentFatherId);
                //------ Time Zone Depends On Currency --------//
                var Currency = db.Currencies.Where(a => a.IsActive == true && a.IsDeleted == false && a.IsDefault == true).FirstOrDefault();
                var CurrencyCode = Currency != null ? Currency.Code : "";
                TimeZoneInfo info;
                if (CurrencyCode == "SAR")
                {
                    //info = TimeZoneInfo.FindSystemTimeZoneById("Arabian Standard Time");//+2H from Egypt Standard Time
                    info = TimeZoneInfo.FindSystemTimeZoneById("Arab Standard Time");// +1H from Egypt Standard Time
                }
                else
                {
                    info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
                }
                DateTime utcNow = DateTime.UtcNow;
                DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
                //----------------- End of Time Zone Depends On Currency --------------------//
                ViewBag.ChurchMembershipId = new SelectList(db.ChurchMemberships.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.Date = cTime.ToString("yyyy-MM-ddTHH:mm");
                return View();
            }
            ChurchMeeting meeting = db.ChurchMeetings.Find(id);
            if (meeting == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل المقابلات ",
                EnAction = "AddEdit",
                ControllerName = "ChurchMeeting",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });
            ViewBag.ChurchMembershipId = new SelectList(db.ChurchMemberships.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", meeting.ChurchMembershipId);
            ViewBag.AreaId = new SelectList(db.Areas.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", meeting.ChurchMembership.AreaId);
            ViewBag.ChurchFatherId = new SelectList(db.ChurchFathers.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", meeting.ChurchFatherId);
            ViewBag.Date = meeting.Date != null ? meeting.Date.Value.ToString("yyyy-MM-ddTHH:mm") : null;
            ViewBag.Next = QueryHelper.Next((int)id, "ChurchMeeting");
            ViewBag.Previous = QueryHelper.Previous((int)id, "ChurchMeeting");
            ViewBag.Last = QueryHelper.GetLast("ChurchMeeting");
            ViewBag.First = QueryHelper.GetFirst("ChurchMeeting");
            return View(meeting);
        }
        [HttpPost]
        public ActionResult AddEdit(ChurchMeeting meeting, string TopicsForDiscussion)
        {
            if (ModelState.IsValid)
            {
                var send = false;
                var id = meeting.Id;
                meeting.IsDeleted = false;
                meeting.IsActive = true;
                meeting.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                if (meeting.Id > 0)
                {
                    db.Entry(meeting).State = EntityState.Modified;
                    Notification.GetNotification("ChurchMeeting", "Edit", "AddEdit", meeting.Id, null, "المقابلات");
                }
                else
                {

                    //-----------------------------***************--------------------------//
                    meeting.Code = (QueryHelper.CodeLastNum("ChurchMeeting") + 1).ToString();
                    db.ChurchMeetings.Add(meeting);
                    Notification.GetNotification("ChurchMeeting", "Add", "AddEdit", meeting.Id, null, "المقابلات");
                    send = true;
                }
                try
                {
                    db.SaveChanges();
                    if (send == true)
                    {
                        HelperController hh = new HelperController();
                        hh.SendToFirebaseUsers(int.Parse(meeting.ChurchMembershipId.ToString()), "شكرا", "اتمنى ليك وقت مفرح ومنتظر اشوفك قريب");
                    }
                    db.Database.ExecuteSqlCommand($"update ChurchMembership set TopicsForDiscussion=N'{TopicsForDiscussion}' where Id= {meeting.ChurchMembershipId}");
                }
                catch (Exception ex)
                {
                    var errors = ex.Message;

                    return Json(new { success = false });
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل المقابلات" : "اضافة المقابلات",
                    EnAction = "AddEdit",
                    ControllerName = "ChurchMeeting",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = meeting.Code
                });
                return Json(new { success = true });

            }

            var Validationerrors = ModelState
                                .Where(x => x.Value.Errors.Count > 0)
                                .Select(x => new { x.Key, x.Value.Errors })
                                .ToArray();

            return Json(new { success = false });
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                ChurchMeeting meeting = db.ChurchMeetings.Find(id);
                meeting.IsDeleted = true;
                meeting.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(meeting).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف المقابلات",
                    EnAction = "Delete",
                    ControllerName = "ChurchMeeting",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    // EnItemName = meeting.EnName
                });
                Notification.GetNotification("ChurchMeeting", "Delete", "Delete", id, null, "المقابلات");
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
                ChurchMeeting meeting = db.ChurchMeetings.Find(id);
                if (meeting.IsActive == true)
                {
                    meeting.IsActive = false;
                }
                else
                {
                    meeting.IsActive = true;
                }
                meeting.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(meeting).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)meeting.IsActive ? "تنشيط المقابلات" : "إلغاء تنشيط المقابلات",
                    EnAction = "Activate/Deactivate",
                    ControllerName = "ChurchMeeting",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = meeting.Id,
                    // EnItemName = meeting.EnName,
                    // ArItemName = meeting.ArName,
                    CodeOrDocNo = meeting.Code
                });
                if (meeting.IsActive == true)
                {
                    Notification.GetNotification("ChurchMeeting", "Activate/Deactivate", "ActivateDeactivate", id, true, "المقابلات");
                }
                else
                {
                    Notification.GetNotification("ChurchMeeting", "Activate/Deactivate", "ActivateDeactivate", id, false, " المقابلات");
                }
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
            var code = QueryHelper.CodeLastNum("ChurchMeeting");
            return Json(code + 1, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult GetFamilyMemberById(int? ChurchMembershipId)
        {
            var member = db.ChurchMemberships.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == ChurchMembershipId).Select(a => new
            { a.Id, a.Code, a.ArName, a.BirthDate, a.Relative, a.ParentId, a.Mobile, a.Image, a.Notes, a.IsAlive, a.Phone, a.Address, a.AreaId, a.IsUseApplication, a.TopicsForDiscussion, a.Email }).FirstOrDefault();
            return Json(member, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult GetMembers()
        {
            var members = db.ChurchMemberships.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => new
            { a.Id, a.Code, a.ArName, a.BirthDate, a.Relative, a.ParentId, a.Mobile, a.Notes, a.IsAlive, a.Email }).ToList();
            return Json(members, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult GetPreviousChurchMeeting(int? NoOfDays, int? ChurchMembershipId)
        {
            var PrevMeetings = db.GetPreviousChurchMeeting(NoOfDays, ChurchMembershipId).ToList();
            return Json(PrevMeetings, JsonRequestBehavior.AllowGet);
        }
        public ActionResult MembersNotInterviewedRecently()
        {
            return View();
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