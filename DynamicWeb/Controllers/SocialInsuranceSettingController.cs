using MyERP.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Controllers
{
    public class SocialInsuranceSettingController : Controller
    {
        MySoftERPEntity db = new MySoftERPEntity();
        // GET: SocialInsuranceSetting
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult AddEdit()
        {

            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            var Setting = db.SocialInsuranceSettings.FirstOrDefault();

            if (Setting == null)
            {
                //DebitAcc
                ViewBag.DebitAccId = new SelectList(db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && c.ClassificationId == 3).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                //CompanyCreditAcc
                ViewBag.CompanyCreditAccId = new SelectList(db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && c.ClassificationId == 3).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                //EmployeeCreditAcc
                ViewBag.EmployeeCreditAccId = new SelectList(db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && c.ClassificationId == 3).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                //SalaryItem
                ViewBag.SalaryItemId = new SelectList(db.SalaryItems.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                return View();
            }

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل إعدادات التأمينات الإجتماعية",
                EnAction = "AddEdit",
                ControllerName = "SocialInsuranceSetting",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = Setting.Id,
            });

            //DebitAcc
            ViewBag.DebitAccId = new SelectList(db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && c.ClassificationId == 3).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", Setting.DebitAccId);
            //CompanyCreditAcc
            ViewBag.CompanyCreditAccId = new SelectList(db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && c.ClassificationId == 3).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", Setting.CompanyCreditAccId);
            //EmployeeCreditAcc
            ViewBag.EmployeeCreditAccId = new SelectList(db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && c.ClassificationId == 3).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", Setting.EmployeeCreditAccId);
            //SalaryItem
            ViewBag.SalaryItemId = new SelectList(db.SalaryItems.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");

            return View(Setting);
        }

        [HttpPost]
        public ActionResult AddEdit(SocialInsuranceSetting socialInsuranceSetting)
        {
            if (ModelState.IsValid)
            {
                var id = socialInsuranceSetting.Id;

                if (socialInsuranceSetting.Id > 0)
                {
                    // use another object to prevent entity error
                    var old = db.SocialInsuranceSettings.Find(id);
                    db.SocialInsuranceSettingDetails.RemoveRange(db.SocialInsuranceSettingDetails.Where(p => p.SocialInsuranceSettingId == old.Id).ToList());
                    old.CitizinCompanyPercentage = socialInsuranceSetting.CitizinCompanyPercentage;
                    old.CitizinEmployeePercentage = socialInsuranceSetting.CitizinEmployeePercentage;
                    old.ForeignCompanyPercentage = socialInsuranceSetting.ForeignCompanyPercentage;
                    old.ForeignEmployeePercentage = socialInsuranceSetting.ForeignEmployeePercentage;
                    old.DebitAccId = socialInsuranceSetting.DebitAccId;
                    old.CompanyCreditAccId = socialInsuranceSetting.CompanyCreditAccId;
                    old.EmployeeCreditAccId = socialInsuranceSetting.EmployeeCreditAccId;

                    foreach (var item in socialInsuranceSetting.SocialInsuranceSettingDetails)
                    {
                        old.SocialInsuranceSettingDetails.Add(item);
                    }
                    
                    db.Entry(old).State = EntityState.Modified;
                }
                    
                else
                {
                    db.SocialInsuranceSettings.Add(socialInsuranceSetting);
                }
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل إعدادات التأمينات الإجتماعية" : "اضافة إعدادات التأمينات الإجتماعية",
                    EnAction = "AddEdit",
                    ControllerName = "SocialInsuranceSetting",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = socialInsuranceSetting.Id
                });
                Notification.GetNotification("SocialInsuranceSetting", id > 0 ? "Edit" : "Add", "AddEdit", socialInsuranceSetting.Id, null, "إعدادات التأمينات الإجتماعية");
                return Json(new { success = "true" });
            }
            //DebitAcc
            ViewBag.DebitAccId = new SelectList(db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && c.ClassificationId == 3).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", socialInsuranceSetting.DebitAccId);
            //CompanyCreditAcc
            ViewBag.CompanyCreditAccId = new SelectList(db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && c.ClassificationId == 3).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", socialInsuranceSetting.CompanyCreditAccId);
            //EmployeeCreditAcc
            ViewBag.EmployeeCreditAccId = new SelectList(db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && c.ClassificationId == 3).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", socialInsuranceSetting.EmployeeCreditAccId);


            return View(socialInsuranceSetting);
        }

    }
}