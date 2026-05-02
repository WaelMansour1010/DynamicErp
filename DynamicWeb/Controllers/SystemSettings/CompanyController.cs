using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using System.IO;
using System.Security.Claims;

namespace MyERP.Controllers.SystemSettings
{

    [ERPAuthorize]
    public class CompanyController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة الشركات",
                EnAction = "Index",
                ControllerName = "Company",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("Company", "View", "Index", null, null, "الشركات");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<Company> companies;

            if (string.IsNullOrEmpty(searchWord))
            {
                companies = db.Companies.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Companies.Where(s => s.IsDeleted == false).Count();
            }
            else
            {
                companies = db.Companies.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.FoundationDate.ToString().Contains(searchWord) || s.Mobile.Contains(searchWord) || s.ERPUser.Name.Contains(searchWord) || s.Notes.Contains(searchWord) || s.Address.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = companies.Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(companies.ToList());

        }

        [SkipERPAuthorize]
        public JsonResult SetCodeNum()
        {
            var code = QueryHelper.CodeLastNum("Company");
            return Json(code + 1, JsonRequestBehavior.AllowGet);
        }

        public ActionResult AddEdit(int? id)
        {

            if (id == null)
            {
                Company NewObj = new Company();

                return View(NewObj);
            }
            Company company = db.Companies.Find(id);
            if (company == null)
            {
                return HttpNotFound();
            }

            try
            {
                ViewBag.FoundationDate = company.FoundationDate.Value.ToString("yyyy-MM-ddTHH:mm");
            }
            catch (Exception)
            {
            }
            ViewBag.Next = QueryHelper.Next((int)id, "Company");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Company");
            ViewBag.Last = QueryHelper.GetLast("Company");
            ViewBag.First = QueryHelper.GetFirst("Company");
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل شركة",
                EnAction = "AddEdit",
                ControllerName = "Company",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = company.Id,
                ArItemName = company.ArName,
                EnItemName = company.EnName,
                CodeOrDocNo = company.Code
            });
            return View(company);
        }

        [HttpPost]
        public ActionResult AddEdit(Company company, string newBtn, HttpPostedFileBase upload)
        {
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            if (ModelState.IsValid)
            {
                var id = company.Id;
                company.IsDeleted = false;
                if (company.Id > 0)
                {
                    //var old = db.Companies.Find(company.Id);
                    //var OldLogo = old != null && old.Logo != null ? old.Logo : null;
                    company.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    if (upload != null)
                    {
                        var folder = Server.MapPath("~/images/CompanyImages/");
                        if (!Directory.Exists(folder))
                        {
                            Directory.CreateDirectory(folder);
                        }
                        upload.SaveAs(Server.MapPath("/images/CompanyImages/") + company.ArName+ ".jpg");

                        company.Logo = domainName + ("/images/CompanyImages/") + company.ArName + ".jpg";

                    }
                    else
                    {
                        company.Logo = company.Logo!=null?company.Logo:null;
                    }
                    db.Entry(company).State = EntityState.Modified;

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("Company", "Edit", "AddEdit", id, null, "الشركات");


                }
                else
                {
                    company.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    company.Code = (QueryHelper.CodeLastNum("Company") + 1).ToString();
                    company.IsActive = true;
                    if (upload != null)
                    {
                        var folder = Server.MapPath("~/images/CompanyImages/");
                        if (!Directory.Exists(folder))
                        {
                            Directory.CreateDirectory(folder);
                        }
                        //upload.SaveAs(Server.MapPath("/images/CompanyImages/") + upload.FileName);

                        //company.Logo = domainName + ("/images/CompanyImages/") + upload.FileName;
                        upload.SaveAs(Server.MapPath("/images/CompanyImages/") + company.ArName + ".jpg");

                        company.Logo = domainName + ("/images/CompanyImages/") + company.ArName + ".jpg";
                    }
                    db.Companies.Add(company);

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("Company", "Add", "AddEdit", company.Id, null, "الشركات");

                }
                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {

                    var mod = ModelState.First(c => c.Key == "Code");  // this

                    mod.Value.Errors.Add("هذا الكود موجود من قبل");
                    return View(company);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل شركة" : "اضافة شركة",
                    EnAction = "AddEdit",
                    ControllerName = "Company",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = company.Id,
                    ArItemName = company.ArName,
                    EnItemName = company.EnName,
                    CodeOrDocNo = company.Code
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

            return View(company);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                Company company = db.Companies.Find(id);
                company.IsDeleted = true;
                company.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(company).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف شركة",
                    EnAction = "AddEdit",
                    ControllerName = "Company",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = company.EnName,
                    ArItemName = company.ArName,
                    CodeOrDocNo = company.Code
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("Company", "Delete", "Delete", id, null, "الشركات");



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
                Company company = db.Companies.Find(id);
                if (company.IsActive == true)
                {
                    company.IsActive = false;
                }
                else
                {
                    company.IsActive = true;
                }
                company.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(company).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)company.IsActive ? "تنشيط شركة" : "إلغاء شركة",
                    EnAction = "AddEdit",
                    ControllerName = "Company",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = company.Id,
                    EnItemName = company.EnName,
                    ArItemName = company.ArName,
                    CodeOrDocNo = company.Code
                });
                ////-------------------- Notification-------------------------////
                if (company.IsActive == true)
                {
                    Notification.GetNotification("Company", "Activate/Deactivate", "ActivateDeactivate", id, true, "الشركات");
                }
                else
                {

                    Notification.GetNotification("Company", "Activate/Deactivate", "ActivateDeactivate", id, false, "الشركات");
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
