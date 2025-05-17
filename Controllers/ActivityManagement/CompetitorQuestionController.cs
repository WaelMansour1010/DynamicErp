using MyERP.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Controllers.ActivityManagement
{
    [SkipERPAuthorize]
    [AllowAnonymous]
    public class CompetitorQuestionController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: CompetitorQuestion
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            ViewBag.IsActive = db.CompetitorQuestions.Where(a => a.IsActive == true).Count() > 0 ? true : false;
                       //-------------------------------paging--------------------------------//
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            //////////////////////////////////////////////////////////////////////////////////////
            //------------------------------- Search ------------------------------------------------//
            IQueryable<CompetitorQuestion> competitorQuestions;
            if (string.IsNullOrEmpty(searchWord))
            {
                competitorQuestions = db.CompetitorQuestions.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.CompetitorQuestions.Where(s => s.IsDeleted == false).Count();
            }
            else
            {
                competitorQuestions = db.CompetitorQuestions.Where(s =>s.IsDeleted==false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.CompetitorQuestions.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(competitorQuestions.ToList());
        }
        public ActionResult AddEdit(int? id)
        {
            ViewBag.logo = db.SystemSettings.FirstOrDefault().Logo;
            var CompetitorId = Session["CompitetorId"];
            if (id == null)
            {
                var Code = SetCodeNum();
                ViewBag.Code = int.Parse(Code.Data.ToString());
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

                ViewBag.Date = cTime.ToString("yyyy-MM-ddTHH:mm");
                return View();
            }
            CompetitorQuestion competitorQuestion = db.CompetitorQuestions.Find(id);

            var QuestionIds= db.CompetitorAnswers.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => a.QuestionId.ToString()).ToList();
            var DetailsIds = db.CompetitorQuestionDetails.Where(a => a.CompetitorQuestionId == competitorQuestion.Id).Select(a => a.Id.ToString()).ToList();
            var Compare = QuestionIds.Intersect(DetailsIds).ToList();
            if(Compare.Count()>0)
            {
                ViewBag.ThereIsAnswersForThoseQuestions = "يوجد إجابات لهذة الاسئلة , إذا قمت بتعديلها سيتم حذف الإجابات ";
            }

            if (competitorQuestion == null)
            {
                return HttpNotFound();
            }

           
            ViewBag.Next = QueryHelper.Next((int)id, "CompetitorQuestion");
            ViewBag.Previous = QueryHelper.Previous((int)id, "CompetitorQuestion");
            ViewBag.Last = QueryHelper.GetLast("CompetitorQuestion");
            ViewBag.First = QueryHelper.GetFirst("CompetitorQuestion");

            ViewBag.Date = competitorQuestion.Date.Value.ToString("yyyy-MM-ddTHH:mm");

            return View(competitorQuestion);
        }

        [HttpPost]
        public ActionResult AddEdit(CompetitorQuestion competitorQuestion)
        {
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            competitorQuestion.IsDeleted = false;
            competitorQuestion.IsActive = true;
          
            if (ModelState.IsValid)
            {
                var id = competitorQuestion.Id;
               
                if (competitorQuestion.Id > 0)
                {
                    var QuestionIds = db.CompetitorAnswers.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => a.QuestionId.ToString()).ToList();
                    var DetailsIds = db.CompetitorQuestionDetails.Where(a => a.CompetitorQuestionId == competitorQuestion.Id).Select(a => a.Id.ToString()).ToList();
                    var Compare = QuestionIds.Intersect(DetailsIds).ToList();
                    if(Compare.Count()>0)
                    {
                        db.CompetitorAnswers.RemoveRange(db.CompetitorAnswers.Where(a=> a.Date.Value.Year == competitorQuestion.Date.Value.Year
              && a.Date.Value.Month == competitorQuestion.Date.Value.Month && a.Date.Value.Day == competitorQuestion.Date.Value.Day));
                    }
                    db.CompetitorQuestionDetails.RemoveRange(db.CompetitorQuestionDetails.Where(x => x.CompetitorQuestionId == competitorQuestion.Id));
                    var competitorQuestionDetials = competitorQuestion.CompetitorQuestionDetails.ToList();
                    competitorQuestionDetials.ForEach((x) => x.CompetitorQuestionId = competitorQuestion.Id);
                    competitorQuestion.CompetitorQuestionDetails = null;
                    db.Entry(competitorQuestion).State = EntityState.Modified;
                    db.CompetitorQuestionDetails.AddRange(competitorQuestionDetials);

                    
                }
                else
                {
                    var Questions = db.CompetitorQuestions.Where(a => a.IsActive == true && a.IsDeleted == false && a.Date.Value.Year == competitorQuestion.Date.Value.Year
              && a.Date.Value.Month == competitorQuestion.Date.Value.Month && a.Date.Value.Day == competitorQuestion.Date.Value.Day).Select(a => a.CompetitorQuestionDetails).ToList();
                    if (Questions.Count() > 0)
                    {
                        return Json(new { success = "Exist Before" });
                    }
                   
                    competitorQuestion.Code = (QueryHelper.CodeLastNum("CompetitorQuestion") + 1).ToString();

                    db.CompetitorQuestions.Add(competitorQuestion);
                
                }

                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    var errors0 =ex.InnerException.InnerException.Message;
                    return Json(new { success = false, errors0 });

                }

                return Json(new { success = true });
            }
            var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();

            return Json(new { success = false,errors });
        }

        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                CompetitorQuestion competitorQuestion = db.CompetitorQuestions.Find(id);
                competitorQuestion.IsDeleted = true;
                db.Entry(competitorQuestion).State = EntityState.Modified;
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
                CompetitorQuestion competitorQuestion = db.CompetitorQuestions.Find(id);
                if (competitorQuestion.IsActive == true)
                {
                    competitorQuestion.IsActive = false;
                }
                else
                {
                    competitorQuestion.IsActive = true;
                }

                db.Entry(competitorQuestion).State = EntityState.Modified;

                db.SaveChanges();
               
               
                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        } 
        [HttpPost]
        public ActionResult StopAcceptingAnswers(bool?IsActive)
        {
            var status = 0;
            if(IsActive==true)
            {
                status = 1; 
            }
            try
            {
                db.Database.ExecuteSqlCommand($"update CompetitorQuestion set IsActive ={status}");
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
            var code = QueryHelper.CodeLastNum("CompetitorQuestion");
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