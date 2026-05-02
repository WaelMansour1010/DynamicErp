using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using MyERP.Repository;

namespace MyERP.Controllers
{
    public class ChurchMembershipController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: ChurchMembership
        public ActionResult Index(bool? FamiliesOnly, bool? IsSearch, int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "", int AreaId = 0, int FatherId = 0)
        {
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            ViewBag.domainName = domainName;
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة العضوية الكنسية",
                EnAction = "Index",
                ControllerName = "ChurchMembership",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            Notification.GetNotification("ChurchMembership", "View", "Index", null, null, "العضوية الكنسية");
            ViewBag.AreaId = new SelectList(db.Areas.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", AreaId);

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }
            IQueryable<ChurchMembership> churchMemberships;
            if (string.IsNullOrEmpty(searchWord))
            {
                if (IsSearch == true)
                {
                    if (FamiliesOnly == true)
                    {
                        churchMemberships = db.ChurchMemberships.Where(a => a.IsDeleted == false && a.ParentId == null && (AreaId == 0 || a.AreaId == AreaId) && (FatherId == 0 || a.ChurchFatherId == FatherId)).OrderByDescending(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                        ViewBag.Count = db.ChurchMemberships.Where(a => a.IsDeleted == false && a.ParentId == null && (AreaId == 0 || a.AreaId == AreaId) && (FatherId == 0 || a.ChurchFatherId == FatherId)).Count();
                    }
                    else
                    {
                        churchMemberships = db.ChurchMemberships.Where(a => a.IsDeleted == false /*&& a.ParentId == null*/ && (AreaId == 0 || a.AreaId == AreaId) && (FatherId == 0 || a.ChurchFatherId == FatherId)).OrderByDescending(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                        ViewBag.Count = db.ChurchMemberships.Where(a => a.IsDeleted == false /*&& a.ParentId == null*/ && (AreaId == 0 || a.AreaId == AreaId) && (FatherId == 0 || a.ChurchFatherId == FatherId)).Count();
                    }
                    ViewBag.FamiliesOnly = FamiliesOnly;
                }
                else
                {
                    churchMemberships = db.ChurchMemberships.Where(a => a.IsDeleted == false && a.ParentId == null && (AreaId == 0 || a.AreaId == AreaId) && (FatherId == 0 || a.ChurchFatherId == FatherId)).OrderByDescending(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.ChurchMemberships.Where(a => a.IsDeleted == false && a.ParentId == null && (AreaId == 0 || a.AreaId == AreaId) && (FatherId == 0 || a.ChurchFatherId == FatherId)).Count();
                    ViewBag.FamiliesOnly = true;
                }
            }
            else
            {
                if (FamiliesOnly == true)
                {
                    churchMemberships = db.ChurchMemberships.Where(a => a.IsDeleted == false && a.ParentId == null && (AreaId == 0 || a.AreaId == AreaId) && (FatherId == 0 || a.ChurchFatherId == FatherId) &&
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)
                || a.ChurchFather.ArName.Contains(searchWord) || a.ChurchFather.EnName.Contains(searchWord)
                ))
                    .OrderByDescending(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.ChurchMemberships.Where(a => a.IsDeleted == false && a.ParentId == null && (AreaId == 0 || a.AreaId == AreaId) && (FatherId == 0 || a.ChurchFatherId == FatherId) && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord) || a.ChurchFather.ArName.Contains(searchWord) || a.ChurchFather.EnName.Contains(searchWord))).Count();
                }
                else
                {
                    churchMemberships = db.ChurchMemberships.Where(a => a.IsDeleted == false /*&& a.ParentId == null*/ && (AreaId == 0 || a.AreaId == AreaId) && (FatherId == 0 || a.ChurchFatherId == FatherId) &&
               (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)
               || a.ChurchMembership2.ArName.Contains(searchWord) || a.ChurchMembership2.EnName.Contains(searchWord) || a.ChurchMembership2.Code.Contains(searchWord)
               || a.ChurchFather.ArName.Contains(searchWord) || a.ChurchFather.EnName.Contains(searchWord)
               )).OrderByDescending(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);

                    ViewBag.Count = db.ChurchMemberships.Where(a => a.IsDeleted == false /*&& a.ParentId == null*/ && (AreaId == 0 || a.AreaId == AreaId) && (FatherId == 0 || a.ChurchFatherId == FatherId)
                    && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)
                                   || a.ChurchMembership2.ArName.Contains(searchWord) || a.ChurchMembership2.EnName.Contains(searchWord) || a.ChurchMembership2.Code.Contains(searchWord)
                    || a.ChurchFather.ArName.Contains(searchWord) || a.ChurchFather.EnName.Contains(searchWord))).Count();
                    ViewBag.FamiliesOnly = true;

                }
                ViewBag.FamiliesOnly = FamiliesOnly;
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;

            ViewBag.FamilyId = new SelectList(db.ChurchMemberships.Where(a => a.IsActive == true && a.IsDeleted == false && a.ParentId == null).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");
            ViewBag.FatherId = new SelectList(db.ChurchFathers.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", FatherId);

            return View(churchMemberships.ToList());
        }

        public ActionResult AddEdit(int? id)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
          //  ViewBag.MemberCodeRes = (QueryHelper.CodeLastNum("ChurchMembership") + 2).ToString();
            ViewBag.MemberCodeRes = (CodeLastNum("ChurchMembership") + 2).ToString();
            ViewBag.PopUpMemberChurchFatherId = new SelectList(db.ChurchFathers.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");
            if (id == null)
            {
                ViewBag.AreaId = new SelectList(db.Areas.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.ChurchFatherId = new SelectList(db.ChurchFathers.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
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

                ViewBag.BirthDate = cTime.ToString("yyyy-MM-ddTHH:mm");
                return View();
            }
            ChurchMembership churchMembership = db.ChurchMemberships.Find(id);
            if (churchMembership == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل العضوية الكنسية ",
                EnAction = "AddEdit",
                ControllerName = "ChurchMembership",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });
            ViewBag.AreaId = new SelectList(db.Areas.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", churchMembership.AreaId);
            ViewBag.ChurchFatherId = new SelectList(db.ChurchFathers.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", churchMembership.ChurchFatherId);

            ViewBag.BirthDate = churchMembership.BirthDate != null ? churchMembership.BirthDate.Value.ToString("yyyy-MM-ddTHH:mm") : null;
            ViewBag.Next = QueryHelper.Next((int)id, "ChurchMembership");
            ViewBag.Previous = QueryHelper.Previous((int)id, "ChurchMembership");
            ViewBag.Last = QueryHelper.GetLast("ChurchMembership");
            ViewBag.First = QueryHelper.GetFirst("ChurchMembership");
            return View(churchMembership);
        }
        [HttpPost]
        public ActionResult AddEdit(ChurchMembership churchMembership)
        {
            if (ModelState.IsValid)
            {
                //var churchMembership = churchMemberships.Where(a => a.Relative == null).FirstOrDefault();
                var id = churchMembership.Id;
                churchMembership.IsDeleted = false;
                churchMembership.IsActive = true;
                churchMembership.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
                var FamilyMember = churchMembership.ChurchMembership1.OrderBy(a =>int.Parse(a.Code));
                if (churchMembership.Id > 0)
                {
                    //--------------- Image -------------//

                    if (churchMembership.Image != null && churchMembership.Image.Contains("base64"))
                    {
                        string fileName = "";
                        fileName = "/images/ChurchMembership/Images/" + churchMembership.Code + "-" + churchMembership.ArName + ".jpeg";
                        //to Check If Image Name Exist before
                        var file = Server.MapPath("/images/ChurchMembership/Images/" + churchMembership.Code + "-" + churchMembership.ArName + ".jpeg").Replace('\\', '/');
                        //if (System.IO.File.Exists(file))
                        if (System.IO.File.Exists(file))
                        {
                            System.IO.File.Delete(file);
                        }
                        var bytes = new byte[7000];
                        if (churchMembership.Image.Contains("jpeg"))
                        {
                            bytes = Convert.FromBase64String(churchMembership.Image.Replace("data:image/jpeg;base64,", ""));
                        }
                        else
                        {
                            bytes = Convert.FromBase64String(churchMembership.Image.Replace("data:image/png;base64,", ""));
                        }
                        using (var imageFile = new FileStream(Server.MapPath(fileName), FileMode.Create))
                        {
                            imageFile.Write(bytes, 0, bytes.Length);
                            imageFile.Flush();
                        }
                        churchMembership.Image = domainName + fileName;
                    }
                    else
                    {
                        churchMembership.Image = churchMembership.Image;
                    }

                    if (FamilyMember != null)
                    {
                        foreach (var item in FamilyMember)
                        {

                            if (item.Image != null && item.Image.Contains("base64"))
                            {
                                string fileName = "";
                                fileName = "/images/ChurchMembership/Images/" + item.Code + "-" + item.ArName + ".jpeg";
                                //to Check If Image Name Exist before
                                var file = Server.MapPath("/images/ChurchMembership/Images/" + item.Code + "-" + item.ArName + ".jpeg").Replace('\\', '/');
                                if (System.IO.File.Exists(file))
                                {
                                    System.IO.File.Delete(file);
                                }
                                var bytes = new byte[7000];
                                if (item.Image.Contains("jpeg"))
                                {
                                    bytes = Convert.FromBase64String(item.Image.Replace("data:image/jpeg;base64,", ""));
                                }
                                else
                                {
                                    bytes = Convert.FromBase64String(item.Image.Replace("data:image/png;base64,", ""));
                                }
                                using (var imageFile = new FileStream(Server.MapPath(fileName), FileMode.Create))
                                {
                                    imageFile.Write(bytes, 0, bytes.Length);
                                    imageFile.Flush();
                                }
                                item.Image = domainName + fileName;
                            }
                            else
                            {
                                item.Image = item.Image;
                            }
                        }
                    }

                    //------------******************---------------//
                    var Olds = new List<ChurchMembership>();
                    foreach (var item in FamilyMember)
                    {
                        var old = db.ChurchMemberships.Find(item.Id);
                        if (old != null)
                        {
                            old.Code = item.Code;
                            old.ParentId = churchMembership.Id;
                            old.ArName = item.ArName;
                            old.BirthDate = item.BirthDate;
                            old.Relative = item.Relative;
                            old.ConfessionFather = item.ConfessionFather;
                            old.Mobile = item.Mobile;
                            old.IsAlive = item.IsAlive;
                            old.FollowUp = item.FollowUp;
                            old.Notes = item.Notes;
                            old.IsDeleted = false;
                            old.IsActive = true;
                            old.UserId = churchMembership.UserId;
                            old.Image = item.Image;
                            old.Email = item.Email;
                            old.Password = item.Password;
                            old.PlayerId = item.PlayerId;
                            Olds.Add(old);
                        }
                        else
                        {
                            var _new = new ChurchMembership();
                            _new.Code = item.Code;
                            _new.ParentId = churchMembership.Id;
                            _new.ArName = item.ArName;
                            _new.BirthDate = item.BirthDate;
                            _new.Relative = item.Relative;
                            _new.ConfessionFather = item.ConfessionFather;
                            _new.Mobile = item.Mobile;
                            _new.IsAlive = item.IsAlive;
                            _new.FollowUp = item.FollowUp;
                            _new.Notes = item.Notes;
                            _new.IsDeleted = false;
                            _new.IsActive = true;
                            _new.UserId = churchMembership.UserId;
                            _new.Image = item.Image;
                            _new.Email = item.Email;
                            _new.Password = item.Password;
                            _new.PlayerId = item.PlayerId;
                            db.ChurchMemberships.Add(_new);
                          //  Olds.Add(_new);
                        }

                    }

                    // Never Delete Child 
                    //db.ChurchMemberships.RemoveRange(db.ChurchMemberships.Where(x => x.ParentId == churchMembership.Id));
                    //var churchMembershipFamily = FamilyMember.ToList();
                    //churchMembershipFamily.ForEach((x) => x.ParentId = churchMembership.Id);
                    churchMembership.ChurchMembership1 = null;
                    churchMembership.ChurchMembership1 = Olds;

                    db.ChurchMembershipVisits.RemoveRange(db.ChurchMembershipVisits.Where(x => x.MainDocId == churchMembership.Id));
                    var churchMembershipVisit = churchMembership.ChurchMembershipVisits.ToList();
                    churchMembershipVisit.ForEach((x) => x.MainDocId = churchMembership.Id);
                    churchMembership.ChurchMembershipVisits = null;
                    db.Entry(churchMembership).State = EntityState.Modified;
                   // db.ChurchMemberships.AddRange(FamilyMember);
                    db.ChurchMembershipVisits.AddRange(churchMembershipVisit);
                    Notification.GetNotification("ChurchMembership", "Edit", "AddEdit", churchMembership.Id, null, "العضوية الكنسية");
                }
                else
                {
                    //---------------- Image -----------------//
                    if (churchMembership.Image != null && churchMembership.Image.Contains("base64"))
                    {
                        string fileName = "";
                        fileName = "/images/ChurchMembership/Images/" + churchMembership.Code + "-" + churchMembership.ArName + ".jpeg";
                        var bytes = new byte[7000];
                        if (churchMembership.Image.Contains("jpeg"))
                        {
                            bytes = Convert.FromBase64String(churchMembership.Image.Replace("data:image/jpeg;base64,", ""));
                        }
                        else
                        {
                            bytes = Convert.FromBase64String(churchMembership.Image.Replace("data:image/png;base64,", ""));
                        }
                        using (var imageFile = new FileStream(Server.MapPath(fileName), FileMode.Create))
                        {
                            imageFile.Write(bytes, 0, bytes.Length);
                            imageFile.Flush();
                        }
                        churchMembership.Image = domainName + fileName;
                    }
                    if (FamilyMember != null)
                    {
                        foreach (var item in FamilyMember)
                        {
                            if (item.Image != null)
                            {
                                string fileName = "";
                                fileName = "/images/ChurchMembership/Images/" + item.Code + "-" + item.ArName + ".jpeg";
                                var bytes = new byte[7000];
                                if (item.Image.Contains("jpeg"))
                                {
                                    bytes = Convert.FromBase64String(item.Image.Replace("data:image/jpeg;base64,", ""));
                                }
                                else
                                {
                                    bytes = Convert.FromBase64String(item.Image.Replace("data:image/png;base64,", ""));
                                }
                                using (var imageFile = new FileStream(Server.MapPath(fileName), FileMode.Create))
                                {
                                    imageFile.Write(bytes, 0, bytes.Length);
                                    imageFile.Flush();
                                }
                                item.Image = domainName + fileName;
                            }
                        }
                    }
                    //-----------------------------***************--------------------------//
                   // churchMembership.Code = (QueryHelper.CodeLastNum("ChurchMembership") + 1).ToString();
                    churchMembership.Code = (CodeLastNum("ChurchMembership") + 1).ToString();
                    churchMembership.ParentId = null;
                    churchMembership.Relative = null;
                    db.ChurchMemberships.Add(churchMembership);
                    Notification.GetNotification("ChurchMembership", "Add", "AddEdit", churchMembership.Id, null, "العضوية الكنسية");
                }
                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    var errors = ex.InnerException.InnerException.Message;

                    return Json(new { success = false,errors });
                }
                id = churchMembership.Id;
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل العضوية الكنسية" : "اضافة العضوية الكنسية",
                    EnAction = "AddEdit",
                    ControllerName = "ChurchMembership",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = churchMembership.Code
                });
                return Json(new { success = true, Id = id });

            }
            var Validationerrors = ModelState
                                .Where(x => x.Value.Errors.Count > 0)
                                .Select(x => new { x.Key, x.Value.Errors })
                                .ToArray();

            return Json(new { success = false });
        }
        [SkipERPAuthorize]
        public ActionResult Gallary(int? Id, string Text)
        {
            if (Id != null)
            {
                db.Configuration.ProxyCreationEnabled = false;
                if (Text == "Next")
                {
                    var NextMember = db.ChurchMemberships.Where(a => a.IsActive == true && a.IsDeleted == false && a.ParentId == null && a.Id > Id).Select(a => new { a.Id, a.ArName, a.Relative, a.Image, a.BirthDate, a.Mobile, a.Address, Details = a.ChurchMembership1.Select(s => new { s.Id, s.ArName, s.Relative }).ToList(), PrevVisits = a.ChurchMembershipVisits.Select(x => new { x.Date, x.ConfessionFather }).ToList() }).FirstOrDefault();
                    //var Detail = db.ChurchMemberships.Where(a => a.IsActive == true && a.IsDeleted == false && a.ParentId==NextMember.Id).Select(a=>new {a.Id,a.ArName,a.Relative,a.Image }).ToList();
                    return Json(new { Member = NextMember }, JsonRequestBehavior.AllowGet);
                }
                else if (Text == "Prev")
                {
                    var PrevMember = db.ChurchMemberships.Where(a => a.IsActive == true && a.IsDeleted == false && a.ParentId == null && a.Id < Id).Select(a => new { a.Id, a.ArName, a.Relative, a.Image, a.BirthDate, a.Mobile, a.Address, Details = a.ChurchMembership1.Select(s => new { s.Id, s.ArName, s.Relative }).ToList(), PrevVisits = a.ChurchMembershipVisits.Select(x => new { x.Date, x.ConfessionFather }).ToList() }).OrderByDescending(a=>a.Id).FirstOrDefault();
                    //var Detail = db.ChurchMemberships.Where(a => a.IsActive == true && a.IsDeleted == false && a.ParentId == PrevMember.Id).Select(a => new { a.Id, a.ArName, a.Relative,a.Image }).ToList();
                    return Json(new { Member = PrevMember }, JsonRequestBehavior.AllowGet);
                }
            }
            var Member = db.ChurchMemberships.Where(a => a.IsActive == true && a.IsDeleted == false && a.ParentId == null).FirstOrDefault();
            //ViewBag.Id = Members.OrderBy(a => a.Id).FirstOrDefault().Id;
            return View(Member);
        }
        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                ChurchMembership churchMembership = db.ChurchMemberships.Find(id);
                churchMembership.IsDeleted = true;
                churchMembership.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                foreach (var item in churchMembership.ChurchMembership1)
                {
                    item.IsDeleted = true;
                }
                foreach (var item in churchMembership.ChurchMembershipVisits)
                {
                    item.IsDeleted = true;
                }
                db.Entry(churchMembership).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف العضوية الكنسية",
                    EnAction = "Delete",
                    ControllerName = "ChurchMembership",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = churchMembership.EnName
                });
                Notification.GetNotification("ChurchMembership", "Delete", "Delete", id, null, "العضوية الكنسية");
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
                ChurchMembership churchMembership = db.ChurchMemberships.Find(id);
                if (churchMembership.IsActive == true)
                {
                    churchMembership.IsActive = false;
                }
                else
                {
                    churchMembership.IsActive = true;
                }
                churchMembership.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(churchMembership).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)churchMembership.IsActive ? "تنشيط العضوية الكنسية" : "إلغاء تنشيط العضوية الكنسية",
                    EnAction = "Activate/Deactivate",
                    ControllerName = "ChurchMembership",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = churchMembership.Id,
                    EnItemName = churchMembership.EnName,
                    ArItemName = churchMembership.ArName,
                    CodeOrDocNo = churchMembership.Code
                });
                if (churchMembership.IsActive == true)
                {
                    Notification.GetNotification("ChurchMembership", "Activate/Deactivate", "ActivateDeactivate", id, true, "العضوية الكنسية");
                }
                else
                {
                    Notification.GetNotification("ChurchMembership", "Activate/Deactivate", "ActivateDeactivate", id, false, " العضوية الكنسية");
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
            //var code = QueryHelper.CodeLastNum("ChurchMembership");
            var code = CodeLastNum("ChurchMembership");
            return Json(code + 1, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult SearchByMobileNo(string Mobile)
        {
            var member = db.ChurchMemberships.Where(a => a.IsActive == true && a.IsDeleted == false && a.ParentId == null && a.Mobile == Mobile).FirstOrDefault();
            var id = member != null ? member.Id : 0;
            return Json(id, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult SearchFamilyMemberByMobileNo(string Mobile)
        {
            var member = db.ChurchMemberships.Where(a => a.IsActive == true && a.IsDeleted == false && a.Mobile == Mobile).Select(a => new
            { a.Id, a.Code, a.ArName, a.BirthDate, a.Relative, a.ParentId, a.Mobile, a.Image, a.Notes, a.IsAlive, a.FollowUp, a.Phone, a.Address, a.AreaId, a.IsUseApplication, a.TopicsForDiscussion, a.Email }).ToList();
            return Json(member, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult LinkMember(int id, int FamilyId, string Relative)
        {
            try
            {
                ChurchMembership churchMembership = db.ChurchMemberships.Find(id);
                churchMembership.ParentId = FamilyId;
                churchMembership.Relative = Relative;
                db.Entry(churchMembership).State = EntityState.Modified;
                db.SaveChanges();
                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }

        [SkipERPAuthorize]
        public ActionResult MembersBirthDay(DateTime? Date)
        {

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

            ViewBag.Date = cTime.ToString("yyyy-MM-dd");
            if (Date != null)
            {
                var Member = db.GetMembersBirthDay(Date).ToList();
                return Json(Member, JsonRequestBehavior.AllowGet);
            }
            return View();
        }
        [SkipERPAuthorize]
        public ActionResult MemberSpiritualNote(DateTime? Date, int? MemberId)
        {

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

            ViewBag.Date = cTime.ToString("yyyy-MM-dd");
            ViewBag.MemberId = new SelectList(db.ChurchMemberships.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");
            if (Date != null)
            {
                var Member = db.GetMemberSpiritualNote(Date, MemberId).ToList();
                return Json(Member, JsonRequestBehavior.AllowGet);
            }
            return View();
        }

        [SkipERPAuthorize]
        public ActionResult MembersFollowUp(int pageIndex = 1, int wantedRowsNo = 10)
        {
            ViewBag.PageIndex = pageIndex;
            ViewBag.wantedRowsNo = wantedRowsNo;

            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }
            var members = db.ChurchMemberships.Where(a => a.IsActive == true && a.IsDeleted == false && a.FollowUp == true).OrderBy(a=>a.Id).Skip(skipRowsNo).Take(wantedRowsNo).ToList();
            ViewBag.Count = db.ChurchMemberships.Where(a => a.IsActive == true && a.IsDeleted == false && a.FollowUp == true).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo).Count();
            return View(members);
        }

        public  double CodeLastNum(string table)
        {
            var code = db.Database.SqlQuery<string>($"SELECT CAST((select max(CAST([Code] AS int)) from [{ table}] where IsDeleted=0 and IsActive=1) AS NVARCHAR(max))");
            if (code.FirstOrDefault() == null)
            {
                return 0;
            }
            return double.Parse(code.FirstOrDefault().ToString());
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