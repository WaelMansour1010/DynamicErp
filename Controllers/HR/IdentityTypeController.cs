using DevExpress.Data.WcfLinq.Helpers;
using MyERP.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web.Mvc;

namespace MyERP.Controllers
{
    public class IdentityTypeController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: IdentityType

        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة نوع الهوية",
                EnAction = "Index",
                ControllerName = "IdentityType",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });

            Notification.GetNotification("IdentityType", "View", "Index", null, null, "نوع الهوية");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }

            IQueryable<IdentityType> identityTypes;
            if (string.IsNullOrEmpty(searchWord))
            {
                identityTypes = db.IdentityTypes.Where(a => a.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.IdentityTypes.Where(a => a.IsDeleted == false).Count();
            }
            else
            {
                identityTypes = db.IdentityTypes.Where(a => a.IsDeleted == false &&
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.IdentityTypes.Where(a => a.IsDeleted == false && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;

            return View(identityTypes.ToList());
        }

        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {
                var Code = SetCodeNum();
                ViewBag.Code = int.Parse(Code.Data.ToString());
                return View();
            }
            IdentityType identityType = db.IdentityTypes.Find(id);
            if (identityType == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل نوع الهوية ",
                EnAction = "AddEdit",
                ControllerName = "IdentityType",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });

            ViewBag.Next = QueryHelper.Next((int)id, "IdentityType");
            ViewBag.Previous = QueryHelper.Previous((int)id, "IdentityType");
            ViewBag.Last = QueryHelper.GetLast("IdentityType");
            ViewBag.First = QueryHelper.GetFirst("IdentityType");
            return View(identityType);
        }

        [HttpPost]
        public ActionResult AddEdit(IdentityType identityType, string newBtn)
        {
            if (ModelState.IsValid)
            {
                var id = identityType.Id;
                identityType.IsDeleted = false;
                if (identityType.Id > 0)
                {
                    identityType.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    db.Entry(identityType).State = EntityState.Modified;
                    Notification.GetNotification("IdentityType", "Edit", "AddEdit", identityType.Id, null, "نوع الهوية");
                }
                else
                {
                    identityType.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    identityType.Code = (QueryHelper.CodeLastNum("IdentityType") + 1).ToString();
                    identityType.IsActive = true;
                    db.IdentityTypes.Add(identityType);

                    Notification.GetNotification("IdentityType", "Add", "AddEdit", identityType.Id, null, "نوع الهوية");
                }

                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    var mod = ModelState.First(c => c.Key == "Code");  // this
                    mod.Value.Errors.Add("هذا الكود موجود من قبل");
                    return View(identityType);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل هوية" : "اضافة هوية",
                    EnAction = "AddEdit",
                    ControllerName = "IdentityType",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = identityType.Code
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

            return View(identityType);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                IdentityType identity = db.IdentityTypes.Find(id);
                identity.IsDeleted = true;
                identity.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(identity).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف هوية",
                    EnAction = "AddEdit",
                    ControllerName = "IdentityType",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = identity.EnName

                });
                Notification.GetNotification("IdentityType", "Delete", "Delete", id, null, "نوع الهوية");


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
                IdentityType identity = db.IdentityTypes.Find(id);
                if (identity.IsActive == true)
                {
                    identity.IsActive = false;
                }
                else
                {
                    identity.IsActive = true;
                }
                identity.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(identity).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)identity.IsActive ? "تنشيط نوع الهوية" : "إلغاء تنشيط نوع الهوية",
                    EnAction = "AddEdit",
                    ControllerName = "IdentityType",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = identity.Id,
                    EnItemName = identity.EnName,
                    ArItemName = identity.ArName,
                    CodeOrDocNo = identity.Code
                });
                if (identity.IsActive == true)
                {
                    Notification.GetNotification("IdentityType", "Activate/Deactivate", "ActivateDeactivate", id, true, "نوع الهوية");
                }
                else
                {

                    Notification.GetNotification("IdentityType", "Activate/Deactivate", "ActivateDeactivate", id, false, "نوع الهوية");
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
            var code = QueryHelper.CodeLastNum("IdentityType");
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