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
    public class ElderController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: Elder
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة المسنين",
                EnAction = "Index",
                ControllerName = "Elder",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            Notification.GetNotification("Elder", "View", "Index", null, null, "المسنين");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }
            IQueryable<MyERP.Models.Elder> elders;
            if (string.IsNullOrEmpty(searchWord))
            {
                elders = db.Elders.Where(a => a.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Elders.Where(a => a.IsDeleted == false).Count();
            }
            else
            {
                elders = db.Elders.Where(a => a.IsDeleted == false &&
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Elders.Where(a => a.IsDeleted == false && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(elders.ToList());
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
                return View();
            }
            Elder elder = db.Elders.Find(id);

            if (elder == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل المسنين ",
                EnAction = "AddEdit",
                ControllerName = "Elder",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });
            ViewBag.Next = QueryHelper.Next((int)id, "Elder");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Elder");
            ViewBag.Last = QueryHelper.GetLast("Elder");
            ViewBag.First = QueryHelper.GetFirst("Elder");
            ViewBag.BirthDate = elder.BirthDate != null ? elder.BirthDate.Value.ToString("yyyy-MM-dd") : null;
            ViewBag.SubscriptionStartDate = elder.SubscriptionStartDate != null ? elder.SubscriptionStartDate.Value.ToString("yyyy-MM-dd") : null;

            return View(elder);
        }
        [HttpPost]
        public ActionResult AddEdit(Elder elder, string newBtn)
        {
            if (ModelState.IsValid)
            {
                var id = elder.Id;
                elder.IsDeleted = false;
                elder.IsActive = true;
                elder.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                if (elder.Id > 0)
                {

                    db.Entry(elder).State = EntityState.Modified;
                    Notification.GetNotification("Elder", "Edit", "AddEdit", elder.Id, null, "المسنين");
                }
                else
                {

                    elder.Code = (QueryHelper.CodeLastNum("Elder") + 1).ToString();
                    db.Elders.Add(elder);
                    Notification.GetNotification("Elder", "Add", "AddEdit", elder.Id, null, "المسنين");
                }
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل المسنين" : "اضافة المسنين",
                    EnAction = "AddEdit",
                    ControllerName = "Elder",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = elder.Code
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
            return View(elder);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                Elder elder = db.Elders.Find(id);
                elder.IsDeleted = true;
                elder.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(elder).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف المسنين",
                    EnAction = "AddEdit",
                    ControllerName = "Elder",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id
                });
                Notification.GetNotification("Elder", "Delete", "Delete", id, null, "المسنين");
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
                Elder elder = db.Elders.Find(id);
                if (elder.IsActive == true)
                {
                    elder.IsActive = false;
                }
                else
                {
                    elder.IsActive = true;
                }
                elder.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(elder).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)elder.IsActive ? "تنشيط المسنين" : "إلغاء تنشيط المسنين",
                    EnAction = "AddEdit",
                    ControllerName = "Elder",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = elder.Id,
                    CodeOrDocNo = elder.Code
                });
                if (elder.IsActive == true)
                {
                    Notification.GetNotification("Elder", "Activate/Deactivate", "ActivateDeactivate", id, true, "المسنين");
                }
                else
                {
                    Notification.GetNotification("Elder", "Activate/Deactivate", "ActivateDeactivate", id, false, " المسنين");
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
            var code = QueryHelper.CodeLastNum("Elder");
            return Json(code + 1, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public ActionResult ElderAccountStatement(int? Year, int? Month)
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
                var elderAccountStatements = db.GetElderAccountStatement(Year, Month).ToList();
                return View(elderAccountStatements);
            }
            return View();
        }

        [SkipERPAuthorize]
        public ActionResult ElderlyArrears(int? Year, int? Month)
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
                var elderlyArrears = db.GetElderlyArrears(Year, Month).ToList();
                foreach (var item in elderlyArrears)
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
                                count = (int)i;
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