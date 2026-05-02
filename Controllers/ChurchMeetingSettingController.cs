using MyERP.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Controllers
{
    public class ChurchMeetingSettingController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: ChurchMeetingSetting
        public ActionResult Index()
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            ChurchMeetingSetting churchMeetingSetting = db.ChurchMeetingSettings.Any() ? db.ChurchMeetingSettings.FirstOrDefault() : new ChurchMeetingSetting();
            return View(churchMeetingSetting);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(ChurchMeetingSetting churchMeetingSetting)
        {
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            var count = db.ChurchMeetingSettings.Count();
            if (count > 0)
            {
                db.Entry(churchMeetingSetting).State = EntityState.Modified;
                // Add DB Change
                QueryHelper.AddDBChange(new DBChange()
                {
                    TableName = "ChurchMeetingSetting",
                    SelectedId = churchMeetingSetting.Id,
                    IsMasterChange = true,
                    IsNew = false,
                    IsTransaction = false
                });
            }
            else
            {
                db.ChurchMeetingSettings.Add(churchMeetingSetting);
                db.SaveChanges();
                // Add DB Change
                var SelectedId = db.ChurchMeetingSettings.OrderByDescending(r => r.Id).FirstOrDefault().Id;
                QueryHelper.AddDBChange(new DBChange()
                {
                    TableName = "ChurchMeetingSetting",
                    SelectedId = SelectedId,
                    IsMasterChange = true,
                    IsNew = true,
                    IsTransaction = false
                });
            }
            return RedirectToAction("Index");
        }
    }
}