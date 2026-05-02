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
    public class VisitsRequirementsController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: VisitsRequirements
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة تعريف متطلبات الزيارات",
                EnAction = "Index",
                ControllerName = "VisitsRequirements",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });

            Notification.GetNotification("VisitsRequirements", "View", "Index", null, null, "تعريف متطلبات الزيارات");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }

            IQueryable<CRM_VisitsRequirements> cRM_VisitsRequirements;
            if (string.IsNullOrEmpty(searchWord))
            {
                cRM_VisitsRequirements = db.CRM_VisitsRequirements.Where(a => a.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.CRM_VisitsRequirements.Where(a => a.IsDeleted == false).Count();
            }
            else
            {
                cRM_VisitsRequirements = db.CRM_VisitsRequirements.Where(a => a.IsDeleted == false &&
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.CRM_VisitsRequirements.Where(a => a.IsDeleted == false && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;

            return View(cRM_VisitsRequirements.ToList());
        }


        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {
                var Code = SetCodeNum();
                ViewBag.Code = int.Parse(Code.Data.ToString());
                return View();
            }
            CRM_VisitsRequirements cRM_VisitsRequirements = db.CRM_VisitsRequirements.Find(id);
            if (cRM_VisitsRequirements == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل تعريف متطلبات الزيارات ",
                EnAction = "AddEdit",
                ControllerName = "VisitsRequirements",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });

            ViewBag.Next = QueryHelper.Next((int)id, "CRM_VisitsRequirements");
            ViewBag.Previous = QueryHelper.Previous((int)id, "CRM_VisitsRequirements");
            ViewBag.Last = QueryHelper.GetLast("CRM_VisitsRequirements");
            ViewBag.First = QueryHelper.GetFirst("CRM_VisitsRequirements");
            return View(cRM_VisitsRequirements);
        }

        [HttpPost]
        public ActionResult AddEdit(CRM_VisitsRequirements cRM_VisitsRequirements, string newBtn)
        {
            if (ModelState.IsValid)
            {
                var id = cRM_VisitsRequirements.Id;
                cRM_VisitsRequirements.IsDeleted = false;
                if (cRM_VisitsRequirements.Id > 0)
                {
                    cRM_VisitsRequirements.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    db.Entry(cRM_VisitsRequirements).State = EntityState.Modified;
                    Notification.GetNotification("VisitsRequirements", "Edit", "AddEdit", cRM_VisitsRequirements.Id, null, "تعريف متطلبات الزيارات");
                }
                else
                {
                    cRM_VisitsRequirements.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    cRM_VisitsRequirements.Code = (QueryHelper.CodeLastNum("CRM_VisitsRequirements") + 1).ToString();
                    cRM_VisitsRequirements.IsActive = true;
                    db.CRM_VisitsRequirements.Add(cRM_VisitsRequirements);
                    Notification.GetNotification("VisitsRequirements", "Add", "AddEdit", cRM_VisitsRequirements.Id, null, "تعريف متطلبات الزيارات");
                }

                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    var mod = ModelState.First(c => c.Key == "Code");  // this
                    mod.Value.Errors.Add("هذا الكود موجود من قبل");
                    return View(cRM_VisitsRequirements);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل تعريف متطلبات الزيارات" : "اضافة تعريف متطلبات الزيارات",
                    EnAction = "AddEdit",
                    ControllerName = "VisitsRequirements",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = cRM_VisitsRequirements.Code
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
            
            return View(cRM_VisitsRequirements);
        }

        [SkipERPAuthorize]
        public JsonResult SetCodeNum()
        {
            var code = QueryHelper.CodeLastNum("CRM_VisitsRequirements");
            return Json(code + 1, JsonRequestBehavior.AllowGet);
        }



        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                CRM_VisitsRequirements cRM_VisitsRequirements = db.CRM_VisitsRequirements.Find(id);
                cRM_VisitsRequirements.IsDeleted = true;
                cRM_VisitsRequirements.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(cRM_VisitsRequirements).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف تعريف متطلبات الزيارة",
                    EnAction = "AddEdit",
                    ControllerName = "VisitsRequirements",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = cRM_VisitsRequirements.EnName

                });
                Notification.GetNotification("VisitsRequirements", "Delete", "Delete", id, null, "تعريف متطلبات الزيارات");


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
                CRM_VisitsRequirements cRM_VisitsRequirements = db.CRM_VisitsRequirements.Find(id);
                if (cRM_VisitsRequirements.IsActive == true)
                {
                    cRM_VisitsRequirements.IsActive = false;
                }
                else
                {
                    cRM_VisitsRequirements.IsActive = true;
                }
                cRM_VisitsRequirements.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(cRM_VisitsRequirements).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)cRM_VisitsRequirements.IsActive ? "تنشيط تعريف متطلبات الزيارات" : "إلغاء تنشيط تعريف متطلبات الزيارات",
                    EnAction = "AddEdit",
                    ControllerName = "VisitsRequirements",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = cRM_VisitsRequirements.Id,
                    EnItemName = cRM_VisitsRequirements.EnName,
                    ArItemName = cRM_VisitsRequirements.ArName,
                    CodeOrDocNo = cRM_VisitsRequirements.Code
                });
                if (cRM_VisitsRequirements.IsActive == true)
                {
                    Notification.GetNotification("VisitsRequirements", "Activate/Deactivate", "ActivateDeactivate", id, true, "تعريف متطلبات الزيارات");
                }
                else
                {

                    Notification.GetNotification("VisitsRequirements", "Activate/Deactivate", "ActivateDeactivate", id, false, "تعريف متطلبات الزيارات");
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