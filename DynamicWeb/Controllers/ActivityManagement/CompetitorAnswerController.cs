using MyERP.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace MyERP.Controllers.ActivityManagement
{
    [SkipERPAuthorize]
    [AllowAnonymous]
    public class CompetitorAnswerController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: CompetitorAnswer
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            //-------------------------------paging--------------------------------//
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            //////////////////////////////////////////////////////////////////////////////////////
            //------------------------------- Search ------------------------------------------------//
            IQueryable<CompetitorAnswer> competitorAnswers;
            if (string.IsNullOrEmpty(searchWord))
            {
                competitorAnswers = db.CompetitorAnswers.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.CompetitorAnswers.Where(s => s.IsDeleted == false).Count();
            }
            else
            {
                competitorAnswers = db.CompetitorAnswers.Where(s => s.IsDeleted == false && (s.Competitor.UserName.Contains(searchWord) || s.Competitor.Code.Contains(searchWord) || s.Answer.Contains(searchWord)
                )).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.CompetitorAnswers.Where(s => s.IsDeleted == false && (s.Competitor.UserName.Contains(searchWord) || s.Competitor.Code.Contains(searchWord) || s.Answer.Contains(searchWord)
                )).OrderBy(s => s.Id).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(competitorAnswers.ToList());
        }
        public ActionResult AddEdit(int? id)
        {
            ViewBag.AcceptingsAnswers = db.CompetitorQuestions.Where(a => a.IsActive == true).Count() > 0 ? true : false;

            var CompetitorId = Session["CompitetorId"];
            if (CompetitorId == null)
            {
                return RedirectToAction("Login", "Competitor");
            }

            ViewBag.logo = db.SystemSettings.FirstOrDefault().Logo;
            if (id == null)
            {
                //var Code = SetCodeNum();
                //ViewBag.Code = int.Parse(Code.Data.ToString());

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
                ViewBag.CompetitorId = (int?)CompetitorId;
                ViewBag.Date = db.CompetitorQuestions.OrderByDescending(a => a.Date).FirstOrDefault().Date.Value.ToString("yyyy-MM-ddTHH:mm");
                return View();
            }
            CompetitorAnswer competitorAnswer = db.CompetitorAnswers.Find(id);

            if (competitorAnswer == null)
            {
                return HttpNotFound();
            }

           
            ViewBag.Next = QueryHelper.Next((int)id, "CompetitorAnswer");
            ViewBag.Previous = QueryHelper.Previous((int)id, "CompetitorAnswer");
            ViewBag.Last = QueryHelper.GetLast("CompetitorAnswer");
            ViewBag.First = QueryHelper.GetFirst("CompetitorAnswer");


            return View(competitorAnswer);
        }

        [HttpPost]
        public ActionResult AddEdit(List<CompetitorAnswer> competitorAnswers)
        {
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            var Date = competitorAnswers[0].Date.Value;
            var CompitetorId = (int?)Session["CompitetorId"];
            if (ModelState.IsValid)
            {

                var previousAnswers = db.CompetitorAnswers.Where(a => a.Date.Value.Year == Date.Year && a.Date.Value.Month == Date.Month && a.Date.Value.Day == Date.Day&&a.CompetitorId== CompitetorId).ToList();
                if (previousAnswers.Count > 0)
                {
                    db.CompetitorAnswers.RemoveRange(previousAnswers);
                }
                db.CompetitorAnswers.AddRange(competitorAnswers);
                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    var errors0 = ex.InnerException.InnerException.Message;
                    return Json(new { success = false, errors0 });
                }

                return Json(new { success = true });
            }
            var errors = ModelState
                               .Where(x => x.Value.Errors.Count > 0)
                               .Select(x => new { x.Key, x.Value.Errors })
                               .ToArray();

            return Json(new { success = false, errors });
        }
        public ActionResult Correction()
        {
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
            return View();
        }

        [HttpPost]
        public ActionResult Correction(List<CompetitorAnswer> competitorAnswers)
        {
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            var Date = competitorAnswers[0].Date.Value;
            if (ModelState.IsValid)
            {

                var previousAnswers = db.CompetitorAnswers.Where(a => a.Date.Value.Year == Date.Year && a.Date.Value.Month == Date.Month && a.Date.Value.Day == Date.Day).ToList();
                if (previousAnswers.Count > 0)
                {
                    db.CompetitorAnswers.RemoveRange(previousAnswers);
                }
                db.CompetitorAnswers.AddRange(competitorAnswers);
                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    var errors0 = ex.InnerException.InnerException.Message;
                    return Json(new { success = false, errors0 });
                }

                return Json(new { success = true });
            }
            var errors = ModelState
                               .Where(x => x.Value.Errors.Count > 0)
                               .Select(x => new { x.Key, x.Value.Errors })
                               .ToArray();

            return Json(new { success = false, errors });
        }

        public ActionResult Submission()
        {
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
            ViewBag.logo = db.SystemSettings.FirstOrDefault().Logo;
            return View();
        }

        public ActionResult CompetitionResult(int? NoOfCorrectAnswers,int? NoOfCompetitors)
        {
            ViewBag.logo = db.SystemSettings.FirstOrDefault().Logo;
            //------ Time Zone Depends On Currency --------//
            var Currency = db.Currencies.Where(a => a.IsActive == true && a.IsDeleted == false && a.IsDefault == true).FirstOrDefault();
            var CurrencyCode = Currency != null ? Currency.Code : "";
            TimeZoneInfo info;
            if (CurrencyCode == "SAR")
            {
                info = TimeZoneInfo.FindSystemTimeZoneById("Arab Standard Time");// +1H from Egypt Standard Time
            }
            else
            {
                info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            }
            DateTime utcNow = DateTime.UtcNow;
            DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
            //----------------- End of Time Zone Depends On Currency --------------------//

            if (NoOfCorrectAnswers != null)
            {
                var result = db.GetCompetitionResult(NoOfCorrectAnswers, NoOfCompetitors).ToList();
                return Json(result,JsonRequestBehavior.AllowGet);
            }
            return View();
        } 

        [HttpPost]
        public ActionResult CompetitionResult(List<CompetitionResult> competitionResults)
        {
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            
            if (ModelState.IsValid)
            {

                var previousResults = db.CompetitionResults.ToList();
                if (previousResults.Count > 0)
                {
                    db.CompetitionResults.RemoveRange(previousResults);
                }
                db.CompetitionResults.AddRange(competitionResults);
                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    var errors0 = ex.InnerException.InnerException.Message;
                    return Json(new { success = false, errors0 });
                }

                return Json(new { success = true });
            }
            var errors = ModelState
                               .Where(x => x.Value.Errors.Count > 0)
                               .Select(x => new { x.Key, x.Value.Errors })
                               .ToArray();

            return Json(new { success = false, errors });
        }

        public ActionResult WinnersAnnouncement()
        {
            ViewBag.logo = db.SystemSettings.FirstOrDefault().Logo;
            //------ Time Zone Depends On Currency --------//
            var Currency = db.Currencies.Where(a => a.IsActive == true && a.IsDeleted == false && a.IsDefault == true).FirstOrDefault();
            var CurrencyCode = Currency != null ? Currency.Code : "";
            TimeZoneInfo info;
            if (CurrencyCode == "SAR")
            {
                info = TimeZoneInfo.FindSystemTimeZoneById("Arab Standard Time");// +1H from Egypt Standard Time
            }
            else
            {
                info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            }
            DateTime utcNow = DateTime.UtcNow;
            DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
            //----------------- End of Time Zone Depends On Currency --------------------//

            var winners = db.CompetitionResults.Where(a=>a.IsSelected==true).ToList();
            return View(winners);
        }


        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                CompetitorAnswer competitorAnswer = db.CompetitorAnswers.Find(id);
                competitorAnswer.IsDeleted = true;
                db.Entry(competitorAnswer).State = EntityState.Modified;
                db.SaveChanges();
              
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
                CompetitorAnswer competitorAnswer = db.CompetitorAnswers.Find(id);
                if (competitorAnswer.IsActive == true)
                {
                    competitorAnswer.IsActive = false;
                }
                else
                {
                    competitorAnswer.IsActive = true;
                }

                db.Entry(competitorAnswer).State = EntityState.Modified;

                db.SaveChanges();
              
                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }
        //[SkipERPAuthorize]
        //public JsonResult SetCodeNum()
        //{
        //    var code = QueryHelper.CodeLastNum("CompetitorAnswer");
        //    return Json(code + 1, JsonRequestBehavior.AllowGet);
        //} 
        [SkipERPAuthorize]
        public JsonResult GetQuestionByDate(DateTime Date)
        {
            var CompitetorId =(int?) Session["CompitetorId"];
            var Questions = db.CompetitorQuestionDetails.Where(a => a.CompetitorQuestion.Date.Value.Year == Date.Year && a.CompetitorQuestion.Date.Value.Month == Date.Month && a.CompetitorQuestion.Date.Value.Day == Date.Day
            )
                .Select(a => new
                {
                    a.Id,
                    a.Question,
                    IsTrue = a.CompetitorAnswers.Where(x => x.QuestionId == a.Id && x.CompetitorId == CompitetorId).Select(x => x.IsTrue).FirstOrDefault(),
                    Answer = a.CompetitorAnswers.Where(x => x.QuestionId == a.Id && x.CompetitorId == CompitetorId).Select(x => x.Answer).ToList()
                }).ToList();
            
            return Json(Questions, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult GetAnswersToCorrect(DateTime Date)
        {
            //var Questions = db.CompetitorQuestionDetails.Where(a => a.CompetitorQuestion.Date.Value.Year == Date.Year && a.CompetitorQuestion.Date.Value.Month == Date.Month && a.CompetitorQuestion.Date.Value.Day == Date.Day)
            //    .Select(a => new
            //    {
            //        a.Id,
            //        a.CompetitorQuestion.Date,
            //        a.Question,
            //        UserName = a.CompetitorAnswers.Where(x => x.QuestionId == a.Id).Select(x => x.Competitor.UserName).FirstOrDefault(),
            //        CompetitorId = a.CompetitorAnswers.Where(x => x.QuestionId == a.Id).Select(x => x.CompetitorId).FirstOrDefault(),
            //        IsTrue = a.CompetitorAnswers.Where(x => x.QuestionId == a.Id).Select(x => x.IsTrue).FirstOrDefault(),
            //        Answer = a.CompetitorAnswers.Where(x => x.QuestionId == a.Id).Select(x => x.Answer).ToList()
            //    }).OrderBy(a => a.Date).ToList();

            var Questions = db.CompetitorAnswers.Where(a => a.Date.Value.Year == Date.Year && a.Date.Value.Month == Date.Month && a.Date.Value.Day == Date.Day
            )
                .Select(a => new
                {
                    a.CompetitorQuestionDetail.Id,
                    a.Date,
                    a.CompetitorQuestionDetail.Question,
                    UserName = a.Competitor.UserName,
                    CompetitorId = a.CompetitorId,
                    IsTrue = a.IsTrue,
                    Answer = a.Answer
                }).ToList();
            return Json(Questions, JsonRequestBehavior.AllowGet);
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