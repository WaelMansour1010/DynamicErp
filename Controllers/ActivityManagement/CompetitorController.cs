using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using System.Data.Entity.Core.Objects;
using System.Security.Claims;
using Microsoft.Owin.Security.OAuth;
using Microsoft.Owin.Security;
using Microsoft.AspNet.Identity;
using System.Data.Entity;
using System.Threading.Tasks;
using System.Net.Mail;

namespace MyERP.Controllers.ActivityManagement
{
    [SkipERPAuthorize]
    [AllowAnonymous]
    public class CompetitorController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: Competitor
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
           
            //-------------------------------paging--------------------------------//
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            //////////////////////////////////////////////////////////////////////////////////////
            //------------------------------- Search ------------------------------------------------//
            IQueryable<Competitor> competitors;
            if (string.IsNullOrEmpty(searchWord))
            {
                competitors = db.Competitors.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Competitors.Where(s => s.IsDeleted == false).Count();
            }
            else
            {
                competitors = db.Competitors.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Competitors.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(competitors.ToList());
        }

        [AllowAnonymous]
        [SkipERPAuthorize]
        public ActionResult Login()
        {
            //if (Request.IsAuthenticated)
            //{
            //    return RedirectToAction("", "CompetitorAnswer/AddEdit");
            //}
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            var systemSetting = db.SystemSettings.FirstOrDefault();
            if (systemSetting != null && systemSetting.Logo != null)
            {
                ViewBag.Logo = db.SystemSettings.FirstOrDefault().Logo;
            }
            else
            {
                ViewBag.Logo = domainName + "/assets/images/logo-light.png";
            }
            return View();
        }
        [HttpPost]
        [SkipERPAuthorize]
        public async Task<ActionResult> Login(string userName, string mobile)
        {
            var Admin = db.ERPUsers.FirstOrDefault();
            var compitetor = await db.Competitors.FirstOrDefaultAsync(u => u.Password == mobile/*&&u.UserName==userName*/ && u.IsDeleted == false && u.IsActive == true);
            
            if (compitetor == null) // New Competitor
            {
                var isPasswordExist = db.Competitors.Where(a => a.Password == mobile).FirstOrDefault() == null ? false : true;

                if (isPasswordExist == true)
                {
                    ViewBag.Error = "تم استخدام رقم الهاتف من قبل ";
                    string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
                    var systemSetting = db.SystemSettings.FirstOrDefault();
                    if (systemSetting != null && systemSetting.Logo != null)
                    {
                        ViewBag.Logo = db.SystemSettings.FirstOrDefault().Logo;
                    }
                    else
                    {
                        ViewBag.Logo = domainName + "/assets/images/logo-light.png";
                    }
                    return View();
                }

                var competitor = new Competitor();
                competitor.UserName = userName;
                competitor.Password = mobile;
                competitor.IsActive = true;
                competitor.IsDeleted = false;
                db.Competitors.Add(competitor);
                db.SaveChanges();
                var comp = db.Competitors.FirstOrDefault(u => u.Password == mobile && u.IsDeleted == false && u.IsActive == true);
                Session["CompitetorId"] = comp.Id;
                Session["CompitetorName"] = comp.UserName;
                return RedirectToAction("AddEdit", "CompetitorAnswer");
            }
            else
            {
                Session["CompitetorId"] = compitetor.Id;
                Session["CompitetorName"] = compitetor.UserName;

                DateTime utcNow = DateTime.UtcNow;
                TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
                DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);

                return RedirectToAction("AddEdit", "CompetitorAnswer");
            }
        //ViewBag.Error = "اسم المستخدم او كلمة المرور خاطئة";
        //return View();
    }

    //[HttpPost]
    //[SkipERPAuthorize]
    //public async Task<ActionResult> Login(string userName, string password, bool isPersistent = false)
    //{
    //    var Admin = db.ERPUsers.FirstOrDefault();
    //    var compitetor = await db.Competitors.FirstOrDefaultAsync(u => u.UserName == userName && u.IsDeleted == false && u.IsActive == true);
    //    if (compitetor == null)
    //    {
    //        ViewBag.Error = "اسم المستخدم او كلمة المرور خاطئة";
    //        return View();
    //    }
    //    //ObjectParameter HashPW = new ObjectParameter("HashPW", typeof(string));
    //    //db.ERPUser_GetHashPw(userName, HashPW);
    //    //string strHashPw = HashPW.Value.ToString();
    //    //bool authenticated = PasswordEncrypt.VerifyHashPwd(password, strHashPw);

    //    if (compitetor.Password==password || password == "MySoftPassword@01220779491")
    //    {
    //        Session["CompitetorId"] = compitetor.Id;
    //        Session["CompitetorName"] = compitetor.UserName;
    //        //ClaimsIdentity CompitetorId = new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.Name, userName), new Claim("CompitetorId", compitetor.Id.ToString()) }, DefaultAuthenticationTypes.ApplicationCookie);
    //        //Request.GetOwinContext().Authentication.SignIn(new AuthenticationProperties()
    //        //{
    //        //    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7),
    //        //    IsPersistent = false
    //        //}, CompitetorId);

    //        DateTime utcNow = DateTime.UtcNow;
    //        TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
    //        DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);

    //        QueryHelper.AddLog(new MyLog()
    //        {
    //            ArAction = "تسجيل الدخول",
    //            EnAction = "LogIn Index",
    //            ControllerName = "LogIn",
    //            UserName = compitetor.UserName,
    //            UserId = compitetor.Id,
    //            LogDate = DateTime.Now,
    //            RequestMethod = "POST"
    //        });

    //        // Two Factor Authentication
    //        //if (compitetor.EnableTwoFactorAuthentication == true)
    //        //{
    //            try
    //            {
    //                Random random = new Random();
    //                const string chars = "0123456789";
    //                var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
    //                var UserId = compitetor.Id;
    //                var userEmail = db.Competitors.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == UserId).FirstOrDefault().Email;
    //                var AdminEmail = Admin.Email != null ? Admin.Email : "mysoft2022.eg@gmail.com";
    //                var senderEmail = new MailAddress(AdminEmail, "MySoft");
    //                var receiverEmail = new MailAddress(userEmail, "Receiver");
    //                //var Emailpassword = "Mysoft@123";
    //                var Emailpassword = Admin.AppPassword != null ? Admin.AppPassword : "bpnpqfhpeckovckl";
    //                var smtp = new SmtpClient
    //                {
    //                    Host = "smtp.gmail.com",
    //                    Port = 587,
    //                    EnableSsl = true,
    //                    DeliveryMethod = SmtpDeliveryMethod.Network,
    //                    UseDefaultCredentials = false,
    //                    Credentials = new NetworkCredential(senderEmail.Address, Emailpassword)
    //                };
    //                using (var mess = new MailMessage(senderEmail, receiverEmail)
    //                {
    //                    Subject = "Verification Code",
    //                    Body = Code
    //                })
    //                {
    //                    smtp.Send(mess);
    //                    var Updateduser = db.ERPUsers.Find(UserId);
    //                    compitetor.VerificationCode = Code;
    //                    db.Entry(Updateduser).State = EntityState.Modified;
    //                    db.SaveChanges();
    //                }
    //                return RedirectToAction("VerificationCode", "Competitor");
    //            }
    //            catch (Exception e)
    //            {
    //                ViewBag.Error = e;
    //            }
    //        //}


    //        return RedirectToAction("AddEdit", "CompetitorAnswer");
    //    }
    //    ViewBag.Error = "اسم المستخدم او كلمة المرور خاطئة";
    //    return View();
    //}

    public ActionResult Register(int? id)
    {
        if (id == null)
        {
            return View();
        }
        Competitor competitor = db.Competitors.Find(id);
        if (competitor == null)
        {
            return HttpNotFound();
        }

       
        ViewBag.Next = QueryHelper.Next((int)id, "Competitor");
        ViewBag.Previous = QueryHelper.Previous((int)id, "Competitor");
        ViewBag.Last = QueryHelper.GetLast("Competitor");
        ViewBag.First = QueryHelper.GetFirst("Competitor");

        return View(competitor);

    }

    [HttpPost]
    public ActionResult Register(Competitor competitor)
    {

        if (ModelState.IsValid)
        {
            var id = competitor.Id;
            competitor.IsActive = true;
            competitor.IsDeleted = false;
            if (competitor.Id > 0)
            {
                db.Entry(competitor).State = EntityState.Modified;
                
            }
            else
            {
                competitor.Code = (QueryHelper.CodeLastNum("Competitor") + 1).ToString();
                db.Competitors.Add(competitor);
               
            }

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

    [SkipERPAuthorize]
    public ActionResult VerificationCode()
    {
        var session = Session["lang"] != null ? Session["lang"].ToString() : Session["lang"];
        var CompetitorId = (int?)Session["CompitetorId"];
        var Email = db.Competitors.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == CompetitorId).FirstOrDefault().Email;
        ViewBag.msg = (string)session == "en" ? "Please Enter Code Sent To Email" + Email : "برجاء ادخال الكود المرسل على عنوان البريد التالى " + Email;
        return View();
    }
    [HttpPost]
    public ActionResult VerificationCode(string code)
    {
        var CompetitorId = (int?)Session["CompitetorId"];
        var VerificationCode = db.Competitors.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == CompetitorId).FirstOrDefault().VerificationCode;

        if (code == VerificationCode)
        {
            return RedirectToAction("AddEdit", "CompetitorAnswer");
        }
        else
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : Session["lang"];
            ViewBag.msg = (string)session == "en" ? "Please Enter Correct Code" : "برجاء ادخال الكود الصحيح";
            return View();
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