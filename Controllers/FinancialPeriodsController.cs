using MyERP.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Controllers
{

    public class FinancialPeriodsController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: FinancialPeriods
        public ActionResult Index(DateTime? date, int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة الفترات المحاسبية",
                EnAction = "Index",
                ControllerName = "FinancialPeriods",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("FinancialPeriods", "View", "Index", null, null, "الفترات المحاسبية");

            //int pageid = db.Get_PageId("FinancialPeriods").SingleOrDefault().Value;
            //var actionId = db.PageActions.FirstOrDefault(c => c.Action == "Index" && c.EnName == "View" && c.PageId == pageid).Id;
            //int userid = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            //var UserName = User.Identity.Name;
            //db.Sp_OccuredNotification(actionId, $"بفتح شاشة الفترات المحاسبية  {UserName}قام المستخدم  ");
            ////////////////-----------------------------------------------------------------------
            ViewBag.ClosedTransactions = (object)db.SystemPages.Where(s => s.IsActive == true && s.IsDeleted == false && s.IsTransaction == true).Select(s => s.ArName);
            ViewBag.PageIndex = pageIndex;

            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<FinancialPeriodMaster> financialPeriodMasters;
            if (date == null)
            {
                DateTime utcNow = DateTime.UtcNow;
                TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
                DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
                ViewBag.Date = cTime.ToString("yyyy-MM-dd");
                financialPeriodMasters = db.FinancialPeriodMasters.OrderBy(r => r.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.FinancialPeriodMasters.ToList().Count;
            }
            else
            {
                DateTime utcNow = new DateTime(date.Value.Year, date.Value.Month, date.Value.Day);
                TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
                DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
                ViewBag.Date = DateTime.Parse(date.ToString()).ToString("yyyy-MM-dd");
                financialPeriodMasters = db.FinancialPeriodMasters.Where(r => (r.PeriodStart.Value.Year == cTime.Year && r.PeriodStart.Value.Month == cTime.Month) || (r.PeriodEnd.Value.Year == cTime.Year && r.PeriodEnd.Value.Month == cTime.Month)|| (r.FinancialPeriods.Any(a=>(a.PeriodStart.Value.Year == cTime.Year && a.PeriodStart.Value.Month == cTime.Month)|| (a.PeriodEnd.Value.Year == cTime.Year && a.PeriodEnd.Value.Month == cTime.Month)))).OrderBy(r => r.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.FinancialPeriodMasters.Where(r => (r.PeriodStart.Value.Year == cTime.Year && r.PeriodEnd.Value.Month == cTime.Month) || (r.PeriodEnd.Value.Year == cTime.Year && r.PeriodEnd.Value.Month == cTime.Month || (r.FinancialPeriods.Any(a => (a.PeriodStart.Value.Year == cTime.Year && a.PeriodStart.Value.Month == cTime.Month) || (a.PeriodEnd.Value.Year == cTime.Year && a.PeriodEnd.Value.Month == cTime.Month))))).ToList().Count;
            }
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(financialPeriodMasters.ToList());

        }

        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {

                return View();
            }
            FinancialPeriodMaster financialPeriodMaster = db.FinancialPeriodMasters.Find(id);

            if (financialPeriodMaster == null)
            {
                return HttpNotFound();
            }

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل الفترات المحاسبية ",
                EnAction = "AddEdit",
                ControllerName = "FinancialPeriods",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });
            ViewBag.PeriodStart = financialPeriodMaster.PeriodStart.Value.ToString("yyyy-MM-ddTHH:mm");
            ViewBag.PeriodEnd = financialPeriodMaster.PeriodEnd.Value.ToString("yyyy-MM-ddTHH:mm");
            ViewBag.Next = QueryHelper.Next((int)id, "FinancialPeriodMaster");
            ViewBag.Previous = QueryHelper.Previous((int)id, "FinancialPeriodMaster");
            ViewBag.Last = QueryHelper.GetLast("FinancialPeriodMaster");
            ViewBag.First = QueryHelper.GetFirst("FinancialPeriodMaster");
            return View(financialPeriodMaster);
        }

        [HttpPost]
        public ActionResult AddEdit(FinancialPeriodMaster financialPeriodMaster)
        {
            if (ModelState.IsValid)
            {
                var id = financialPeriodMaster.Id;
                financialPeriodMaster.IsDeleted = false;
                financialPeriodMaster.IsActive = true;
                if (financialPeriodMaster.Id > 0)
                {
                    financialPeriodMaster.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                    db.FinancialPeriods.RemoveRange(db.FinancialPeriods.Where(x => x.FinancialPeriodMasterId == financialPeriodMaster.Id));
                    var Detials = financialPeriodMaster.FinancialPeriods.ToList();
                    Detials.ForEach((x) => x.FinancialPeriodMasterId = financialPeriodMaster.Id);
                    financialPeriodMaster.FinancialPeriods = null;
                    db.Entry(financialPeriodMaster).State = EntityState.Modified;
                    db.FinancialPeriods.AddRange(Detials);
                    ////-------------------- Notification for any edit -------------------------////
                    Notification.GetNotification("FinancialPeriodMaster", "Edit", "AddEdit", id, null, "الفترات المحاسبية");

                }
                else
                {
                    financialPeriodMaster.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    db.FinancialPeriodMasters.Add(financialPeriodMaster);
                    ////-------------------- Notification for adding new shift -------------------------////
                    Notification.GetNotification("FinancialPeriodMaster", "Add", "AddEdit", financialPeriodMaster.Id, null, "الفترات المحاسبية");

                }
                try
                {
                    db.SaveChanges();

                }
                catch (Exception ex)
                {
                    var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل الفترات المحاسبية" : "اضافة الفترات المحاسبية",
                    EnAction = "AddEdit",
                    ControllerName = "FinancialPeriods",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,

                });
                return Json(new { success = "true" });
            }
            var error = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .Select(x => new { x.Key, x.Value.Errors })
                        .ToArray();
            return Json(new { success = "false" });
        }

        [HttpPost]
        public bool ReOpen(int? DetailsId, int MainId)
        {
            var nextPeriod = db.FinancialPeriods_GetNextPeriod(DetailsId, MainId).FirstOrDefault();
            if (nextPeriod == null)
            {
                if (DetailsId != null)
                {
                    var period = db.FinancialPeriods.Where(a => a.Id == DetailsId).FirstOrDefault();
                    period.Opened = true;
                    period.IsActive = true;
                    period.LockStatus = false;
                    period.Status = "Active";
                    db.Entry(period).State = EntityState.Modified;
                    db.SaveChanges();
                    db.OpenTransactions(period.PeriodStart, period.PeriodEnd);

                    QueryHelper.AddLog(new MyLog()
                    {
                        ArAction = "فتح  فترة محاسبية ",
                        EnAction = "Index",
                        ControllerName = "FinancialPeriods",
                        UserName = User.Identity.Name,
                        UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                        LogDate = DateTime.Now,
                        RequestMethod = "GET",
                        SelectedItem = period.Id,
                        ArItemName = period.ArName,
                        EnItemName = period.EnName,
                        CodeOrDocNo = period.PeriodNo.ToString()
                    });

                    ////-------------------- Notification-------------------------////

                    //int pageid = db.Get_PageId("FinancialPeriods").SingleOrDefault().Value;
                    //var actionId = db.PageActions.FirstOrDefault(c => c.Action == "ReOpen" && c.EnName == "Re Open Period" && c.PageId == pageid).Id;
                    //int userid = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    //var UserName = User.Identity.Name;
                    //db.Sp_OccuredNotification(actionId, $"بفتح بيانات في شاشة فترة محاسبية  {UserName}قام المستخدم ");
                    ////////////////-----------------------------------------------------------------------

                    return true;
                }
                else
                {
                    var period = db.FinancialPeriodMasters.Where(a => a.Id == MainId).FirstOrDefault();
                    period.Opened = true;
                    period.IsActive = true;
                    period.LockStatus = false;
                    period.Status = "Active";
                    foreach (var item in period.FinancialPeriods)
                    {
                        item.Opened = true;
                        item.IsActive = true;
                        item.LockStatus = false;
                        item.Status = "Active";
                    }
                    db.Entry(period).State = EntityState.Modified;
                    db.SaveChanges();
                    db.OpenTransactions(period.PeriodStart, period.PeriodEnd);

                    QueryHelper.AddLog(new MyLog()
                    {
                        ArAction = "فتح  فترة محاسبية ",
                        EnAction = "Index",
                        ControllerName = "FinancialPeriods",
                        UserName = User.Identity.Name,
                        UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                        LogDate = DateTime.Now,
                        RequestMethod = "GET",
                        SelectedItem = period.Id,
                        ArItemName = period.ArName,
                        EnItemName = period.EnName,
                        // CodeOrDocNo = period.PeriodNo.ToString()
                    });

                    return true;
                }
            }
            else
            {
                if (nextPeriod.Status == "Closed")
                {
                    return false;
                }
                else
                {
                    if (DetailsId != null)
                    {
                        var period = db.FinancialPeriods.Where(a => a.Id == DetailsId).FirstOrDefault();
                        period.Opened = true;
                        period.IsActive = true;
                        period.LockStatus = false;
                        period.Status = "Active";
                        db.Entry(period).State = EntityState.Modified;
                        db.SaveChanges();
                        db.OpenTransactions(period.PeriodStart, period.PeriodEnd);

                        QueryHelper.AddLog(new MyLog()
                        {
                            ArAction = "فتح  فترة محاسبية ",
                            EnAction = "Index",
                            ControllerName = "FinancialPeriods",
                            UserName = User.Identity.Name,
                            UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                            LogDate = DateTime.Now,
                            RequestMethod = "GET",
                            SelectedItem = period.Id,
                            ArItemName = period.ArName,
                            EnItemName = period.EnName,
                            CodeOrDocNo = period.PeriodNo.ToString()
                        });
                        ////-------------------- Notification-------------------------////

                        //int pageid = db.Get_PageId("FinancialPeriods").SingleOrDefault().Value;
                        //var actionId = db.PageActions.FirstOrDefault(c => c.Action == "ReOpen" && c.EnName == "Re Open Period" && c.PageId == pageid).Id;
                        //int userid = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                        //var UserName = User.Identity.Name;
                        //db.Sp_OccuredNotification(actionId, $"بفتح بيانات في شاشة فترة محاسبية  {UserName}قام المستخدم ");
                        ////////////////-----------------------------------------------------------------------

                        return true;
                    }
                    else
                    {
                        var period = db.FinancialPeriodMasters.Where(a => a.Id == MainId).FirstOrDefault();
                        period.Opened = true;
                        period.IsActive = true;
                        period.LockStatus = false;
                        period.Status = "Active";
                        foreach (var item in period.FinancialPeriods)
                        {
                            item.Opened = true;
                            item.IsActive = true;
                            item.LockStatus = false;
                            item.Status = "Active";
                        }
                        db.Entry(period).State = EntityState.Modified;
                        db.SaveChanges();
                        db.OpenTransactions(period.PeriodStart, period.PeriodEnd);

                        QueryHelper.AddLog(new MyLog()
                        {
                            ArAction = "فتح  فترة محاسبية ",
                            EnAction = "Index",
                            ControllerName = "FinancialPeriods",
                            UserName = User.Identity.Name,
                            UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                            LogDate = DateTime.Now,
                            RequestMethod = "GET",
                            SelectedItem = period.Id,
                            ArItemName = period.ArName,
                            EnItemName = period.EnName,
                            //CodeOrDocNo = period.PeriodNo.ToString()
                        });
                        return true;
                    }
                }
            }
        }
        //open Period
        [HttpPost]
        public bool OpenPeriod(int? DetailsId, int MainId)
        {
            var PreviousPeriod = db.FinancialPeriods_GetPreviousPeriod(DetailsId, MainId).FirstOrDefault();
            if (PreviousPeriod == null)
            {
                if (DetailsId != null)
                {
                    var period = db.FinancialPeriods.Where(a => a.Id == DetailsId).FirstOrDefault();
                    period.Opened = true;
                    period.IsActive = true;
                    period.LockStatus = false;
                    period.Status = "Active";
                    db.Entry(period).State = EntityState.Modified;
                    db.SaveChanges();
                    db.OpenTransactions(period.PeriodStart, period.PeriodEnd);
                    QueryHelper.AddLog(new MyLog()
                    {
                        ArAction = "فتح  فترة محاسبية ",
                        EnAction = "Index",
                        ControllerName = "FinancialPeriods",
                        UserName = User.Identity.Name,
                        UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                        LogDate = DateTime.Now,
                        RequestMethod = "GET",
                        SelectedItem = period.Id,
                        ArItemName = period.ArName,
                        EnItemName = period.EnName,
                        CodeOrDocNo = period.PeriodNo.ToString()

                    });

                    ////-------------------- Notification-------------------------////

                    //int pageid = db.Get_PageId("FinancialPeriods").SingleOrDefault().Value;
                    //var actionId = db.PageActions.FirstOrDefault(c => c.Action == "OpenPeriod" && c.EnName == "Open Period" && c.PageId == pageid).Id;
                    //int userid = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    //var UserName = User.Identity.Name;
                    //db.Sp_OccuredNotification(actionId, $"بفتح بيانات في شاشة فترة محاسبية  {UserName}قام المستخدم  ");

                    ////////////////-----------------------------------------------------------------------

                    return true;
                }
                else
                {
                    var period = db.FinancialPeriodMasters.Where(a => a.Id == MainId).FirstOrDefault();
                    period.Opened = true;
                    period.IsActive = true;
                    period.LockStatus = false;
                    period.Status = "Active";
                    foreach (var item in period.FinancialPeriods)
                    {
                        item.Opened = true;
                        item.IsActive = true;
                        item.LockStatus = false;
                        item.Status = "Active";
                    }
                    db.Entry(period).State = EntityState.Modified;
                    db.SaveChanges();
                    db.OpenTransactions(period.PeriodStart, period.PeriodEnd);
                    QueryHelper.AddLog(new MyLog()
                    {
                        ArAction = "فتح  فترة محاسبية ",
                        EnAction = "Index",
                        ControllerName = "FinancialPeriods",
                        UserName = User.Identity.Name,
                        UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                        LogDate = DateTime.Now,
                        RequestMethod = "GET",
                        SelectedItem = period.Id,
                        ArItemName = period.ArName,
                        EnItemName = period.EnName,
                        // CodeOrDocNo = period.PeriodNo.ToString()
                    });
                    return true;
                }
            }
            else
            {
                if (PreviousPeriod.Status == "NotActive")
                {
                    return false;
                }
                else
                {
                    if (DetailsId != null)
                    {
                        var period = db.FinancialPeriods.Where(a => a.Id == DetailsId).FirstOrDefault();
                        period.Opened = true;
                        period.IsActive = true;
                        period.LockStatus = false;
                        period.Status = "Active";
                        db.Entry(period).State = EntityState.Modified;

                        db.SaveChanges();
                        db.OpenTransactions(period.PeriodStart, period.PeriodEnd);
                        QueryHelper.AddLog(new MyLog()
                        {
                            ArAction = "فتح  فترة محاسبية ",
                            EnAction = "Index",
                            ControllerName = "FinancialPeriods",
                            UserName = User.Identity.Name,
                            UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                            LogDate = DateTime.Now,
                            RequestMethod = "GET",
                            SelectedItem = period.Id,
                            ArItemName = period.ArName,
                            EnItemName = period.EnName,
                            CodeOrDocNo = period.PeriodNo.ToString()
                        });
                        ////-------------------- Notification-------------------------////

                        //int pageid = db.Get_PageId("FinancialPeriods").SingleOrDefault().Value;
                        //var actionId = db.PageActions.FirstOrDefault(c => c.Action == "OpenPeriod" && c.EnName == "Open Period" && c.PageId == pageid).Id;
                        //int userid = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                        //var UserName = User.Identity.Name;
                        //db.Sp_OccuredNotification(actionId, $"بفتح بيانات في شاشة فترة محاسبية  {UserName}قام المستخدم  ");

                        ////////////////-----------------------------------------------------------------------

                        return true;
                    }
                    else
                    {
                        var period = db.FinancialPeriodMasters.Where(a => a.Id == MainId).FirstOrDefault();
                        period.Opened = true;
                        period.IsActive = true;
                        period.LockStatus = false;
                        period.Status = "Active";
                        foreach (var item in period.FinancialPeriods)
                        {
                            item.Opened = true;
                            item.IsActive = true;
                            item.LockStatus = false;
                            item.Status = "Active";
                        }
                        db.Entry(period).State = EntityState.Modified;
                        db.SaveChanges();
                        db.OpenTransactions(period.PeriodStart, period.PeriodEnd);
                        QueryHelper.AddLog(new MyLog()
                        {
                            ArAction = "فتح  فترة محاسبية ",
                            EnAction = "Index",
                            ControllerName = "FinancialPeriods",
                            UserName = User.Identity.Name,
                            UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                            LogDate = DateTime.Now,
                            RequestMethod = "GET",
                            SelectedItem = period.Id,
                            ArItemName = period.ArName,
                            EnItemName = period.EnName,
                            // CodeOrDocNo = period.PeriodNo.ToString()
                        });
                        return true;
                    }
                }
            }
        }

        //close Period
        [HttpPost]
        public bool ClosePeriod(int? DetailsId, int MainId)
        {
            var PreviousPeriod = db.FinancialPeriods_GetPreviousPeriod(DetailsId, MainId).FirstOrDefault();

            if (PreviousPeriod == null)
            {
                if (DetailsId != null)
                {
                    var period = db.FinancialPeriods.Where(a => a.Id == DetailsId).FirstOrDefault();
                    period.Closed = true;
                    period.IsActive = false;
                    period.LockStatus = true;
                    period.Opened = true;
                    period.Status = "Closed";
                    db.Entry(period).State = EntityState.Modified;
                    db.SaveChanges();
                    db.CloseTransactions(period.PeriodStart, period.PeriodEnd);
                    QueryHelper.AddLog(new MyLog()
                    {
                        ArAction = "ترحيل فترة محاسبية ",
                        EnAction = "Index",
                        ControllerName = "FinancialPeriods",
                        UserName = User.Identity.Name,
                        UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                        LogDate = DateTime.Now,
                        RequestMethod = "GET",
                        SelectedItem = period.Id,
                        ArItemName = period.ArName,
                        EnItemName = period.EnName,
                        CodeOrDocNo = period.PeriodNo.ToString()
                    });
                    ////-------------------- Notification-------------------------////

                    //int pageid = db.Get_PageId("FinancialPeriods").SingleOrDefault().Value;
                    //var actionId = db.PageActions.FirstOrDefault(c => c.Action == "ClosePeriod" && c.EnName == "Close Period" && c.PageId == pageid).Id;
                    //int userid = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    //var UserName = User.Identity.Name;
                    //db.Sp_OccuredNotification(actionId, $"بترحيل بيانات في شاشة فترة محاسبية  {UserName}قام المستخدم  ");

                    ////////////////-----------------------------------------------------------------------

                    return true;
                }
                else
                {
                    var period = db.FinancialPeriodMasters.Where(a => a.Id == MainId).FirstOrDefault();
                    period.Closed = true;
                    period.IsActive = false;
                    period.LockStatus = true;
                    period.Opened = true;
                    period.Status = "Closed";
                    foreach (var item in period.FinancialPeriods)
                    {
                        item.Closed = true;
                        item.IsActive = false;
                        item.LockStatus = true;
                        item.Opened = true;
                        item.Status = "Closed";
                    }
                    db.Entry(period).State = EntityState.Modified;
                    db.SaveChanges();
                    db.CloseTransactions(period.PeriodStart, period.PeriodEnd);
                    QueryHelper.AddLog(new MyLog()
                    {
                        ArAction = "ترحيل فترة محاسبية ",
                        EnAction = "Index",
                        ControllerName = "FinancialPeriods",
                        UserName = User.Identity.Name,
                        UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                        LogDate = DateTime.Now,
                        RequestMethod = "GET",
                        SelectedItem = period.Id,
                        ArItemName = period.ArName,
                        EnItemName = period.EnName,
                        // CodeOrDocNo = period.PeriodNo.ToString()
                    });
                    return true;
                }
            }
            else
            {

                if (PreviousPeriod.Status != "Closed")
                {
                    return false;
                }
                else
                {
                    if (DetailsId != null)
                    {
                        var period = db.FinancialPeriods.Where(a => a.Id == DetailsId).FirstOrDefault();
                        period.Closed = true;
                        period.IsActive = false;
                        period.LockStatus = true;
                        period.Opened = true;
                        period.Status = "Closed";
                        db.Entry(period).State = EntityState.Modified;
                        db.SaveChanges();
                        db.CloseTransactions(period.PeriodStart, period.PeriodEnd);
                        QueryHelper.AddLog(new MyLog()
                        {
                            ArAction = "إغلاق فترة محاسبية ",
                            EnAction = "Index",
                            ControllerName = "FinancialPeriods",
                            UserName = User.Identity.Name,
                            UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                            LogDate = DateTime.Now,
                            RequestMethod = "GET",
                            SelectedItem = period.Id,
                            ArItemName = period.ArName,
                            EnItemName = period.EnName,
                            CodeOrDocNo = period.PeriodNo.ToString()
                        });
                        ////-------------------- Notification-------------------------////

                        //int pageid = db.Get_PageId("FinancialPeriods").SingleOrDefault().Value;
                        //var actionId = db.PageActions.FirstOrDefault(c => c.Action == "ClosePeriod" && c.EnName == "Close Period" && c.PageId == pageid).Id;
                        //int userid = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                        //var UserName = User.Identity.Name;
                        //db.Sp_OccuredNotification(actionId, $"بإغلاق بيانات في شاشة فترة محاسبية  {UserName}قام المستخدم  ");

                        ////////////////-----------------------------------------------------------------------

                        return true;
                    }
                    else
                    {
                        var period = db.FinancialPeriodMasters.Where(a => a.Id == MainId).FirstOrDefault();
                        period.Closed = true;
                        period.IsActive = false;
                        period.LockStatus = true;
                        period.Opened = true;
                        period.Status = "Closed";
                        foreach (var item in period.FinancialPeriods)
                        {
                            item.Closed = true;
                            item.IsActive = false;
                            item.LockStatus = true;
                            item.Opened = true;
                            item.Status = "Closed";
                        }
                        db.Entry(period).State = EntityState.Modified;
                        db.SaveChanges();
                        db.CloseTransactions(period.PeriodStart, period.PeriodEnd);
                        QueryHelper.AddLog(new MyLog()
                        {
                            ArAction = "إغلاق فترة محاسبية ",
                            EnAction = "Index",
                            ControllerName = "FinancialPeriods",
                            UserName = User.Identity.Name,
                            UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                            LogDate = DateTime.Now,
                            RequestMethod = "GET",
                            SelectedItem = period.Id,
                            ArItemName = period.ArName,
                            EnItemName = period.EnName,
                            // CodeOrDocNo = period.PeriodNo.ToString()
                        });
                        return true;
                    }
                }
            }
        }

    }
}