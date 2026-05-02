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
    public class CustomerEvaluationController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        // GET: Area
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تقييم العملاء",
                EnAction = "Index",
                ControllerName = "CustomerEvaluation",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });

            Notification.GetNotification("CustomerEvaluation", "View", "Index", null, null, "تقييم العملاء");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }

            IQueryable<CRM_CustomerEvaluation> customerEvaluations;
            if (string.IsNullOrEmpty(searchWord))
            {
                customerEvaluations = db.CRM_CustomerEvaluation.Where(a => a.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.CRM_CustomerEvaluation.Where(a => a.IsDeleted == false).Count();
            }
            else
            {
                customerEvaluations = db.CRM_CustomerEvaluation.Where(a => a.IsDeleted == false &&
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.CRM_CustomerEvaluation.Where(a => a.IsDeleted == false && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;

            return View(customerEvaluations.ToList());
        }

        [HttpGet]
        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {

                return View();
            }

            CRM_CustomerEvaluation cRM_CustomerEvaluation = db.CRM_CustomerEvaluation.Find(id);
            if (cRM_CustomerEvaluation == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل تقييم العملاء ",
                EnAction = "AddEdit",
                ControllerName = "CustomerEvaluation",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });

            ViewBag.Next = QueryHelper.Next((int)id, "CRM_CustomerEvaluation");
            ViewBag.Previous = QueryHelper.Previous((int)id, "CRM_CustomerEvaluation");
            ViewBag.Last = QueryHelper.GetLast("CRM_CustomerEvaluation");
            ViewBag.First = QueryHelper.GetFirst("CRM_CustomerEvaluation");

            return View(cRM_CustomerEvaluation);
        }

        [HttpPost]
        public ActionResult AddEdit(CRM_CustomerEvaluation cRM_CustomerEvaluation, string newBtn)
        {
            if (ModelState.IsValid)
            {
                var id = cRM_CustomerEvaluation.Id;
                cRM_CustomerEvaluation.IsDeleted = false;
                if (cRM_CustomerEvaluation.Id > 0)
                {
                    cRM_CustomerEvaluation.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    db.Entry(cRM_CustomerEvaluation).State = EntityState.Modified;
                    Notification.GetNotification("CustomerEvaluation", "Edit", "AddEdit", cRM_CustomerEvaluation.Id, null, "تقييم العملاء");
                }
                else
                {
                    cRM_CustomerEvaluation.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    cRM_CustomerEvaluation.Code = (QueryHelper.CodeLastNum("CRM_CustomerEvaluation") + 1).ToString();
                    cRM_CustomerEvaluation.IsActive = true;
                    db.CRM_CustomerEvaluation.Add(cRM_CustomerEvaluation);

                    Notification.GetNotification("CustomerEvaluation", "Add", "AddEdit", cRM_CustomerEvaluation.Id, null, "تقييم العملاء");
                }

                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    var mod = ModelState.First(c => c.Key == "Code");  // this
                    mod.Value.Errors.Add("هذا الكود موجود من قبل");
                    return View(cRM_CustomerEvaluation);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل تقييم العملاء" : "اضافة تقييم العملاء",
                    EnAction = "AddEdit",
                    ControllerName = "CustomerEvaluation",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = cRM_CustomerEvaluation.Code
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

            return View(cRM_CustomerEvaluation);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                Area area = db.Areas.Find(id);
                area.IsDeleted = true;
                area.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(area).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف منطقة",
                    EnAction = "AddEdit",
                    ControllerName = "Area",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = area.EnName

                });
                Notification.GetNotification("Area", "Delete", "Delete", id, null, "المناطق و القطاعات");


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
                CRM_CustomerEvaluation cRM_CustomerEvaluation = db.CRM_CustomerEvaluation.Find(id);
                if (cRM_CustomerEvaluation.IsActive == true)
                {
                    cRM_CustomerEvaluation.IsActive = false;
                }
                else
                {
                    cRM_CustomerEvaluation.IsActive = true;
                }
                cRM_CustomerEvaluation.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(cRM_CustomerEvaluation).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)cRM_CustomerEvaluation.IsActive ? "تنشيط  تقييم العملاء" : "إلغاء تنشيط  تقييم العملاء",
                    EnAction = "AddEdit",
                    ControllerName = "CustomerEvaluation",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = cRM_CustomerEvaluation.Id,
                    EnItemName = cRM_CustomerEvaluation.EnName,
                    ArItemName = cRM_CustomerEvaluation.ArName,
                    CodeOrDocNo = cRM_CustomerEvaluation.Code
                });
                if (cRM_CustomerEvaluation.IsActive == true)
                {
                    Notification.GetNotification("CustomerEvaluation", "Activate/Deactivate", "ActivateDeactivate", id, true, "تقييم العملاء");
                }
                else
                {

                    Notification.GetNotification("CustomerEvaluation", "Activate/Deactivate", "ActivateDeactivate", id, false, "تقييم العملاء");
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
            var code = QueryHelper.CodeLastNum("CRM_CustomerEvaluation");
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