using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using MyERP.Repository;
using System.Security.Claims;
using System.Data.Entity.Core.Objects;
using System.Threading.Tasks;
using Newtonsoft.Json;
using DevExpress.XtraPrinting;

namespace MyERP.Controllers
{
    public class CompanyJobRequestController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: CompanyJobRequest
        public ActionResult Index(int? jobStatusId,int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة طلبات الشركات",
                EnAction = "Index",
                ControllerName = "CompanyJobRequest",
                UserName = User.Identity.Name,
                UserId = userId,
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("CompanyJobRequest", "View", "Index", null, null, "طلبات الشركات");
            ///////-----------------------------------------------------------------------

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<CompanyJobRequest> companyJobRequests;
            if (string.IsNullOrEmpty(searchWord))
            {
                companyJobRequests = db.CompanyJobRequests.Where(c => c.IsDeleted == false &&(jobStatusId ==null ||c.JobStatusId==jobStatusId)).OrderByDescending(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.CompanyJobRequests.Where(c => c.IsDeleted == false && (jobStatusId == null || c.JobStatusId == jobStatusId)).Count();
            }
            else
            {
                companyJobRequests = db.CompanyJobRequests.Where(s => s.IsDeleted == false && (jobStatusId == null || s.JobStatusId == jobStatusId) && (s.DocumentNumber.Contains(searchWord)||s.Company.ArName.Contains(searchWord) || s.Company.EnName.Contains(searchWord) || s.Job.ArName.Contains(searchWord) || s.Job.EnName.Contains(searchWord) || s.JobStatu.ArName.Contains(searchWord) || s.JobStatu.EnName.Contains(searchWord))).OrderByDescending(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.CompanyJobRequests.Where(s => s.IsDeleted == false && (jobStatusId == null || s.JobStatusId == jobStatusId) && ((s.DocumentNumber.Contains(searchWord) || s.Company.ArName.Contains(searchWord) || s.Company.EnName.Contains(searchWord) || s.Job.ArName.Contains(searchWord) || s.Job.EnName.Contains(searchWord) || s.JobStatu.ArName.Contains(searchWord) || s.JobStatu.EnName.Contains(searchWord)))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            ViewBag.JobStatusId = new SelectList(db.JobStatus.Select(b => new
            {
                Id = b.Id,
                ArName = b.ArName
            }), "Id", "ArName");
            return View(companyJobRequests.ToList());
        }
        public ActionResult AddEdit(int? id)
        {
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            ViewBag.domainName = domainName;
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            if (id == null)
            {
                ViewBag.CompanyId = new SelectList(db.Companies.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.JobId = new SelectList(db.Jobs.Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.JobStatusId = new SelectList(db.JobStatus.Select(b => new
                {
                    Id = b.Id,
                    ArName = b.ArName
                }), "Id", "ArName");
                return View();
            }
            CompanyJobRequest companyJobRequest = db.CompanyJobRequests.Find(id);
            if (companyJobRequest == null)
            {
                return HttpNotFound();
            }
            ViewBag.Next = QueryHelper.Next((int)id, "CompanyJobRequest");
            ViewBag.Previous = QueryHelper.Previous((int)id, "CompanyJobRequest");
            ViewBag.Last = QueryHelper.GetLast("CompanyJobRequest");
            ViewBag.First = QueryHelper.GetFirst("CompanyJobRequest");
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل  طلبات الشركات",
                EnAction = "AddEdit",
                ControllerName = "CompanyJobRequest",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = companyJobRequest.Id,
                //ArItemName = companyJobRequest.Company.ArName,
                //EnItemName = companyJobRequest.Company.EnName,
                CodeOrDocNo = companyJobRequest.DocumentNumber
            });
            ViewBag.CompanyId = new SelectList(db.Companies.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", companyJobRequest.CompanyId);

            ViewBag.JobId = new SelectList(db.Jobs.Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", companyJobRequest.JobId);

            ViewBag.JobStatusId = new SelectList(db.JobStatus.Select(b => new
            {
                Id = b.Id,
                ArName = b.ArName
            }), "Id", "ArName", companyJobRequest.JobStatusId);
            try
            {
                ViewBag.JobRequestDate = companyJobRequest.JobRequestDate.Value.ToString("yyyy-MM-dd");
            }
            catch (Exception)
            {
            }
            return View(companyJobRequest);
        }

        [HttpPost]
        public ActionResult AddEdit(CompanyJobRequest companyJobRequest/*, string newBtn*/)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            if (ModelState.IsValid)
            {
                companyJobRequest.IsDeleted = false;
                if (companyJobRequest.Id > 0)
                {
                    companyJobRequest.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    companyJobRequest.IsActive = true;
                    //use another object to prevent entity error
                    var old = db.CompanyJobRequests.Find(companyJobRequest.Id);
                    old.DocumentNumber = companyJobRequest.DocumentNumber;
                    old.CompanyId = companyJobRequest.CompanyId;
                    old.JobId = companyJobRequest.JobId;
                    old.Requirements = companyJobRequest.Requirements;
                    old.JobStatusId = companyJobRequest.JobStatusId;
                    old.JobRequestDate = companyJobRequest.JobRequestDate;
                    old.Notes = companyJobRequest.Notes;
                    old.IsActive = companyJobRequest.IsActive;
                    old.IsDeleted = companyJobRequest.IsDeleted;
                    old.RequiredNo = companyJobRequest.RequiredNo;
                    old.CompanyLocation = companyJobRequest.CompanyLocation;
                    old.Governorate = companyJobRequest.Governorate;
                    old.AdSource = companyJobRequest.AdSource;
                    old.ContactMethod = companyJobRequest.ContactMethod;
                    old.ResponsibleServant = companyJobRequest.ResponsibleServant;
                    old.Salary = companyJobRequest.Salary;
                    old.Insurances = companyJobRequest.Insurances;
                    old.Transport = companyJobRequest.Transport;
                    old.Commission = companyJobRequest.Commission;

                    db.JobCandidates.RemoveRange(db.JobCandidates.Where(p => p.CompanyJobRequestId == old.Id).ToList());
                    foreach (var item in companyJobRequest.JobCandidates)
                    {
                        old.JobCandidates.Add(item);
                    }
                    db.Entry(old).State = EntityState.Modified;


                    //db.Entry(companyJobRequest).State = EntityState.Modified;
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("CompanyJobRequest", "Edit", "AddEdit", companyJobRequest.Id, null, "طلبات الشركات");
                }
                else
                {
                    companyJobRequest.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    companyJobRequest.IsActive = true;
                    db.CompanyJobRequests.Add(companyJobRequest);
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("CompanyJobRequest", "Add", "AddEdit", companyJobRequest.Id, null, "طلبات الشركات");
                }
                db.SaveChanges();

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = companyJobRequest.Id > 0 ? "تعديل طلبات الشركات" : "اضافة طلبات الشركات",
                    EnAction = "AddEdit",
                    ControllerName = "CompanyJobRequest",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "Post",
                    SelectedItem = companyJobRequest.Id,
                    //ArItemName = companyJobRequest.Company.ArName,
                    //EnItemName = companyJobRequest.Company.EnName,
                    CodeOrDocNo = companyJobRequest.DocumentNumber
                });
                return Json(new { success = "true" });
            }
            var errors = ModelState
                     .Where(x => x.Value.Errors.Count > 0)
                     .Select(x => new { x.Key, x.Value.Errors })
                     .ToArray();
            return Json(new { success = "false", errors });
        }


        [SkipERPAuthorize]
        public JsonResult GetCandidates(int? jobId,DateTime?DateFrom,DateTime?DateTo)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var candidates = db.EmployeeJobs.Where(a => a.JobId == jobId&&(DateFrom == null ||a.Employee.ApplicationDate>=DateFrom) && (DateTo == null || a.Employee.ApplicationDate <=DateTo)).Select(a => new
            {
                Id = a.Employee.Id,
                ArName = a.Employee.ArName
            }).ToList();
            return Json(candidates, JsonRequestBehavior.AllowGet);
        }

        public ActionResult CandidateInterviewFollowUp(int? id, int? companyId, int? companyJobRequestId)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            if (id == null && companyId == null && companyJobRequestId == null)
            {
                return View();
            }
            else if (companyId != null && companyJobRequestId != null)
            {               
                var candidateFollowUp = db.JobCandidates.Where(a => a.CompanyJobRequestId == companyJobRequestId && a.CompanyJobRequest.CompanyId == companyId).ToList();
                return View(candidateFollowUp);
            }
            JobCandidate jobCandidate = db.JobCandidates.Find(id);
            if (jobCandidate == null)
            {
                return HttpNotFound();
            }
            ViewBag.Next = QueryHelper.Next((int)id, "JobCandidate");
            ViewBag.Previous = QueryHelper.Previous((int)id, "JobCandidate");
            ViewBag.Last = QueryHelper.GetLast("JobCandidate");
            ViewBag.First = QueryHelper.GetFirst("JobCandidate");
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل طلبات الشركات",
                EnAction = "AddEdit",
                ControllerName = "JobCandidate",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = jobCandidate.Id,
                //ArItemName = companyJobRequest.Company.ArName,
                //EnItemName = companyJobRequest.Company.EnName,
                // CodeOrDocNo = jobCandidate.DocumentNumber
            });
            ViewBag.CompanyId = new SelectList(db.Companies.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");
            return View(jobCandidate);
        }

        [HttpPost]
        public ActionResult CandidateInterviewFollowUp(IEnumerable<JobCandidate> jobCandidates)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            if (ModelState.IsValid)
            {
                foreach (var item in jobCandidates)
                {
                    if (item.Id > 0)
                    {
                        db.Entry(item).State = EntityState.Modified;
                        ////-------------------- Notification-------------------------////
                        Notification.GetNotification("JobCandidate", "Edit", "AddEdit", item.Id, null, "طلبات الشركات");
                    }
                    else
                    {
                        db.JobCandidates.Add(item);
                        ////-------------------- Notification-------------------------////
                        Notification.GetNotification("JobCandidate", "Add", "AddEdit", item.Id, null, "طلبات الشركات");
                    }
                    db.SaveChanges();
                    QueryHelper.AddLog(new MyLog()
                    {
                        ArAction = item.Id > 0 ? "تعديل طلبات الشركات" : "اضافة طلبات الشركات",
                        EnAction = "AddEdit",
                        ControllerName = "JobCandidate",
                        UserName = User.Identity.Name,
                        UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                        LogDate = DateTime.Now,
                        RequestMethod = "Post",
                        SelectedItem = item.Id,
                        //ArItemName = jobCandidate.Company.ArName,
                        //EnItemName = jobCandidate.Company.EnName,
                        //CodeOrDocNo = jobCandidate.DocumentNumber
                    });
                }
                return Json(new { success = "true" });
            }
            var errors = ModelState
                     .Where(x => x.Value.Errors.Count > 0)
                     .Select(x => new { x.Key, x.Value.Errors })
                     .ToArray();
            return Json(new { success = "false", errors });
        }

        [SkipERPAuthorize]
        public JsonResult GetJobRequestByCompanyId(int? companyId)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var Request = db.CompanyJobRequests.Where(a => a.CompanyId == companyId).Select(a => new
            {
                Id = a.Id,
                DocNo = a.DocumentNumber
            }).ToList();
            return Json(Request, JsonRequestBehavior.AllowGet);
        }




        //[HttpPost]
        //public ActionResult ActivateDeactivate(int id)
        //{
        //    try
        //    {
        //        CompanyJobRequest companyJobRequest = db.CompanyJobRequests.Find(id);
        //        if (companyJobRequest.IsActive == true)
        //        {
        //            companyJobRequest.IsActive = false;
        //        }
        //        else
        //        {
        //            companyJobRequest.IsActive = true;
        //        }
        //        companyJobRequest.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

        //        db.Entry(companyJobRequest).State = EntityState.Modified;

        //        db.SaveChanges();
        //        QueryHelper.AddLog(new MyLog()
        //        {
        //            ArAction = companyJobRequest.Id > 0 ? "تنشيط طلبات الشركات" : "إلغاء تنشيط طلبات الشركات",
        //            EnAction = "AddEdit",
        //            ControllerName = "CompanyJobRequest",
        //            UserName = User.Identity.Name,
        //            UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
        //            LogDate = DateTime.Now,
        //            RequestMethod = "Post",
        //            SelectedItem = companyJobRequest.Id,
        //            //ArItemName = companyJobRequest.Company.ArName,
        //            //EnItemName = companyJobRequest.Company.EnName,
        //            CodeOrDocNo = companyJobRequest.DocumentNumber
        //        });
        //        ////-------------------- Notification-------------------------////
        //        if (companyJobRequest.IsActive == true)
        //        {
        //            Notification.GetNotification("CompanyJobRequest", "Activate/Deactivate", "ActivateDeactivate", id, true, "طلبات الشركات");
        //        }
        //        else
        //        {

        //            Notification.GetNotification("CompanyJobRequest", "Activate/Deactivate", "ActivateDeactivate", id, false, "طلبات الشركات");
        //        }
        //        ///////-----------------------------------------------------------------------


        //        return Content("true");
        //    }
        //    catch (Exception ex)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }
        //}

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                CompanyJobRequest companyJobRequest = db.CompanyJobRequests.Find(id);
                companyJobRequest.IsDeleted = true;
                companyJobRequest.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(companyJobRequest).State = EntityState.Modified;

                db.SaveChanges();

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف طلبات الشركات",
                    EnAction = "AddEdit",
                    ControllerName = "CompanyJobRequest",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = companyJobRequest.Company.EnName,
                    ArItemName = companyJobRequest.Company.ArName,
                    CodeOrDocNo = companyJobRequest.DocumentNumber
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("CompanyJobRequest", "Delete", "Delete", id, null, "طلبات الشركات");

                ///////-----------------------------------------------------------------------

                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }

        // -- Set Doc Num Only For This Transaction as it Required Company Id And Job Id 
        [SkipERPAuthorize]
        public JsonResult SetDocNum()
        {
            //var docNo = QueryHelper.DocLastNum(id, "CompanyJobRequest");
            //double i = (docNo) + 1;
            //return Json(i, JsonRequestBehavior.AllowGet);

            //var docNo = db.Database.SqlQuery<decimal>($"select ISNULL( max(convert(decimal, [DocumentNumber])),0) from CompanyJobRequest where (" + companyId + " is null or [CompanyId] = " + companyId + ") and (" + jobId + " is null or [JobId]=" + jobId + ")");
            var docNo = db.Database.SqlQuery<decimal>($"select ISNULL( max(convert(decimal, [DocumentNumber])),0) from CompanyJobRequest");
            double x = double.Parse(docNo.FirstOrDefault().ToString());
            var i = (x) + 1;
            return Json(i, JsonRequestBehavior.AllowGet);
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