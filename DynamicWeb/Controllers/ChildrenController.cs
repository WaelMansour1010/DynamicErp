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

namespace MyERP.Controllers
{
    public class ChildrenController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        // GET: Children
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "", int ChurchServiceId = 0)
        {
            ViewBag.ChurchServiceId = new SelectList(db.ChurchServices.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", ChurchServiceId);

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة الأطفال",
                EnAction = "Index",
                ControllerName = "Children",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            Notification.GetNotification("Children", "View", "Index", null, null, "الأطفال");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }
            IQueryable<MyERP.Models.Child> children;
            if (string.IsNullOrEmpty(searchWord))
            {
                children = db.Children.Where(a => a.IsDeleted == false && (ChurchServiceId == 0 || a.ChurchServiceId == ChurchServiceId)).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Children.Where(a => a.IsDeleted == false && (ChurchServiceId == 0 || a.ChurchServiceId == ChurchServiceId)).Count();
            }
            else
            {
                children = db.Children.Where(a => a.IsDeleted == false && (ChurchServiceId == 0 || a.ChurchServiceId == ChurchServiceId) &&
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)
                || a.ChurchService.ArName.Contains(searchWord)|| a.ChurchService.EnName.Contains(searchWord)
                ))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Children.Where(a => a.IsDeleted == false && (ChurchServiceId == 0 || a.ChurchServiceId == ChurchServiceId) && 
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)
                || a.ChurchService.ArName.Contains(searchWord) || a.ChurchService.EnName.Contains(searchWord)
                )).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(children.ToList());
        }

        public ActionResult AddEdit(int? id)
        {
            DateTime utcNow = DateTime.UtcNow;
            TimeZone curTimeZone = TimeZone.CurrentTimeZone;
            // TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById(curTimeZone.StandardName);
            DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);

            if (id == null)
            {
                ViewBag.BirthDate = cTime.ToString("yyyy-MM-dd");
                ViewBag.SubscriptionStartDate = cTime.ToString("yyyy-MM-dd");
                ViewBag.ChurchServiceId = new SelectList(db.ChurchServices.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.GenderId = new SelectList(db.Genders.Select(b => new
                {
                    Id = b.Id,
                    ArName = b.ArName
                }), "Id", "ArName");
               
                return View();
            }
            Child child = db.Children.Find(id);

            if (child == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل الأطفال ",
                EnAction = "AddEdit",
                ControllerName = "Children",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });
            ViewBag.Next = QueryHelper.Next((int)id, "Children");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Children");
            ViewBag.Last = QueryHelper.GetLast("Children");
            ViewBag.First = QueryHelper.GetFirst("Children");
            ViewBag.BirthDate = child.BirthDate != null ? child.BirthDate.Value.ToString("yyyy-MM-dd") : null;
            ViewBag.SubscriptionStartDate = child.SubscriptionStartDate != null ? child.SubscriptionStartDate.Value.ToString("yyyy-MM-dd") : null;

            ViewBag.ChurchServiceId = new SelectList(db.ChurchServices.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", child.ChurchServiceId);
            ViewBag.GenderId = new SelectList(db.Genders.Select(b => new
            {
                Id = b.Id,
                ArName = b.ArName
            }), "Id", "ArName", child.GenderId);
           
            return View(child);
        }
        [HttpPost]
        public ActionResult AddEdit(Child child, string newBtn)
        {
            if (ModelState.IsValid)
            {
                var id = child.Id;
                child.IsDeleted = false;
                child.IsActive = true;
                child.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                if (child.Id > 0)
                {

                    db.Entry(child).State = EntityState.Modified;
                    Notification.GetNotification("Children", "Edit", "AddEdit", child.Id, null, "الأطفال");
                }
                else
                {

                    child.Code = (QueryHelper.CodeLastNum("Children") + 1).ToString();
                    db.Children.Add(child);
                    Notification.GetNotification("Children", "Add", "AddEdit", child.Id, null, "الأطفال");
                }
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل الأطفال" : "اضافة الأطفال",
                    EnAction = "AddEdit",
                    ControllerName = "Children",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = child.Code
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
            var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();
            return View(child);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                Child child = db.Children.Find(id);
                child.IsDeleted = true;
                child.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(child).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف الأطفال",
                    EnAction = "AddEdit",
                    ControllerName = "Children",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id
                });
                Notification.GetNotification("Children", "Delete", "Delete", id, null, "الأطفال");
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
                Child child = db.Children.Find(id);
                if (child.IsActive == true)
                {
                    child.IsActive = false;
                }
                else
                {
                    child.IsActive = true;
                }
                child.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(child).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)child.IsActive ? "تنشيط الأطفال" : "إلغاء تنشيط الأطفال",
                    EnAction = "AddEdit",
                    ControllerName = "Children",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = child.Id,
                    CodeOrDocNo = child.Code
                });
                if (child.IsActive == true)
                {
                    Notification.GetNotification("Children", "Activate/Deactivate", "ActivateDeactivate", id, true, "الأطفال");
                }
                else
                {
                    Notification.GetNotification("Children", "Activate/Deactivate", "ActivateDeactivate", id, false, " الأطفال");
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
            var code = QueryHelper.CodeLastNum("Children");
            return Json(code + 1, JsonRequestBehavior.AllowGet);
        }

        public ActionResult StudentsAccountStatement(int? Year, int? Month)
        {
            List<int> year = new List<int>();
            //Month
            ViewBag.Month = new SelectList(new List<dynamic> {
                new { id=1,name="يناير"},
                new { id=2,name="فبراير"},
                new { id=3,name="مارس"},
                new { id=4,name="إبريل"},
                new { id=5,name="مايو"},
                new { id=6,name="يونيه"},
                new { id=7,name="يوليو"},
                new { id=8,name="اغسطس"},
                new { id=9,name="سبتمبر"},
                new { id=10,name="اكتوبر"},
                new { id=11,name="نوفمبر"},
                new { id=12,name="ديسمبر"}}, "id", "name");
            //year
            for (var i = 2019; i <= 2030; i++)
            {
                year.Add(i);
                ViewBag.Year = new SelectList(year);
            }

            if (Year != null || Month != null)
            {
                var childAccountStatement = db.GetChildrenAccountStatement(Year, Month).ToList();
                return View(childAccountStatement);
            }
            return View();
        }
        [SkipERPAuthorize]
        public ActionResult StudentsArrears(int? Year, int? Month)
        {
            List<int> year = new List<int>();
            //Month
            ViewBag.Month = new SelectList(new List<dynamic> {
                new { id=1,name="يناير"},
                new { id=2,name="فبراير"},
                new { id=3,name="مارس"},
                new { id=4,name="إبريل"},
                new { id=5,name="مايو"},
                new { id=6,name="يونيه"},
                new { id=7,name="يوليو"},
                new { id=8,name="اغسطس"},
                new { id=9,name="سبتمبر"},
                new { id=10,name="اكتوبر"},
                new { id=11,name="نوفمبر"},
                new { id=12,name="ديسمبر"}}, "id", "name");
            //year
            for (var i = 2019; i <= 2030; i++)
            {
                year.Add(i);
                ViewBag.Year = new SelectList(year);
            }
            DateTime utcNow = DateTime.UtcNow;
            TimeZone curTimeZone = TimeZone.CurrentTimeZone;
            TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById(curTimeZone.StandardName);
            DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
            List<MyERP.Models.CustomModels.CashReceiptArrear> cashReceiptArrears = new List<Models.CustomModels.CashReceiptArrear>();

            if (Year != null || Month != null)
            {
                var studentsArrears = db.GetChildrenArrears(Year, Month).ToList();
                foreach (var item in studentsArrears)
                {
                    var cashReceiptArrear = new MyERP.Models.CustomModels.CashReceiptArrear();
                    cashReceiptArrear.Id = item.Id;
                    cashReceiptArrear.ArName = item.ArName;
                    cashReceiptArrear.DocumentNumber = item.DocumentNumber;
                    cashReceiptArrear.MoneyAmount = item.MoneyAmount;
                    cashReceiptArrear.MonthlySubscription = item.MonthlySubscription;
                    cashReceiptArrear.SubscriptionStartDate = item.SubscriptionStartDate;
                    List<MyERP.Models.CustomModels.CashReceiptArrearDetail> details = new List<Models.CustomModels.CashReceiptArrearDetail>();
                    var count = 0;
                    if (item.Year == cTime.Year)
                    {
                        if (item.CashReceiptVoucherId != null)
                        {
                            for (int? i = item.Month; i < cTime.Month; i++)
                            {
                                var cashReceiptArrearDetail = new MyERP.Models.CustomModels.CashReceiptArrearDetail();
                                cashReceiptArrearDetail.CashReceiptVoucherId = 0;
                                cashReceiptArrearDetail.Date = null;
                                cashReceiptArrearDetail.Year = cTime.Year;
                                cashReceiptArrearDetail.Month = i + 1;
                                details.Add(cashReceiptArrearDetail);
                            }
                            count = (int)(cTime.Month - item.Month);
                            cashReceiptArrear.NoOfMonthsOverdue = count;
                            cashReceiptArrear.TotalOverdue = count * (item.MonthlySubscription);
                            cashReceiptArrear.cashReceiptArrearDetails = details;
                            cashReceiptArrears.Add(cashReceiptArrear);
                        }
                        else
                        {
                            for (int? i = item.SubscriptionStartDate.Value.Month; i <= cTime.Month; i++)
                            {
                                var cashReceiptArrearDetail = new MyERP.Models.CustomModels.CashReceiptArrearDetail();
                                cashReceiptArrearDetail.CashReceiptVoucherId = 0;
                                cashReceiptArrearDetail.Date = null;
                                cashReceiptArrearDetail.Year = cTime.Year;
                                cashReceiptArrearDetail.Month = i;
                                details.Add(cashReceiptArrearDetail);
                            }
                            count = (int)(cTime.Month - item.SubscriptionStartDate.Value.Month);
                            cashReceiptArrear.cashReceiptArrearDetails = details;
                            cashReceiptArrear.NoOfMonthsOverdue = count;
                            cashReceiptArrear.TotalOverdue = count * (item.MonthlySubscription);
                            cashReceiptArrears.Add(cashReceiptArrear);
                        }
                    }
                    else
                    {
                        for (int? i = item.SubscriptionStartDate.Value.Month; i <= cTime.Month; i++)
                        {
                            var cashReceiptArrearDetail = new MyERP.Models.CustomModels.CashReceiptArrearDetail();
                            cashReceiptArrearDetail.CashReceiptVoucherId = 0;
                            cashReceiptArrearDetail.Date = null;
                            cashReceiptArrearDetail.Year = cTime.Year;
                            cashReceiptArrearDetail.Month = i;
                            details.Add(cashReceiptArrearDetail);
                        }
                        count = (int)(cTime.Month - item.SubscriptionStartDate.Value.Month);
                        cashReceiptArrear.cashReceiptArrearDetails = details;
                        cashReceiptArrear.NoOfMonthsOverdue = count;
                        cashReceiptArrear.TotalOverdue = count * (item.MonthlySubscription);
                        cashReceiptArrears.Add(cashReceiptArrear);
                    }

                }
                return View(cashReceiptArrears.ToList());
            }
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