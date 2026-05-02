using MyERP.Models;
using MyERP.Repository;
using MyERP.Utils;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Controllers.PartyManagement
{
    public class PartyController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: Party
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة الحفلات",
                EnAction = "Index",
                ControllerName = "Party",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });

            Notification.GetNotification("Party", "View", "Index", null, null, "الحفلات");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }

            IQueryable<Party> parties;
            if (string.IsNullOrEmpty(searchWord))
            {
                parties = db.Parties.Where(a => a.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Parties.Where(a => a.IsDeleted == false).Count();
            }
            else
            {
                parties = db.Parties.Where(a => a.IsDeleted == false &&
                (a.Singer.ArName.Contains(searchWord) || a.Singer.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Parties.Where(a => a.IsDeleted == false && (a.Singer.ArName.Contains(searchWord) || a.Singer.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;

            return View(parties.ToList());
        }

        // GET: Party/Edit/5
        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {
                ViewBag.SingerId = new SelectList(db.Singers.Where(c => c.IsActive == true && c.IsDeleted == false).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }).ToList(), "Id", "ArName");
                DateTime utcNow = DateTime.UtcNow;
                TimeZone curTimeZone = TimeZone.CurrentTimeZone;
                // TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
                TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById(curTimeZone.StandardName);
                DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
                ViewBag.Date = cTime.ToString("yyyy-MM-dd");
                return View();
            }

            Party party = db.Parties.Find(id);
            if (party == null)
                return HttpNotFound();

            ViewBag.Next = QueryHelper.Next((int)id, "Party");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Party");
            ViewBag.Last = QueryHelper.GetLast("Party");
            ViewBag.First = QueryHelper.GetFirst("Party");

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل الحفلات",
                EnAction = "AddEdit",
                ControllerName = "Party",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = party.Id,
                CodeOrDocNo = party.Code
            });

            ViewBag.SingerId = new SelectList(db.Singers.Where(c => c.IsActive == true && c.IsDeleted == false).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }).ToList(), "Id", "ArName", party.SingerId);
           
            ViewBag.Date = party.Date != null ? party.Date.Value.ToString("yyyy-MM-dd") : null;
            ViewBag.ReservedChairsCount = party.Chairs.Where(a => a.IsReserved == true && a.IsActive == true && a.IsDeleted == false).Count();
            return View(party);
        }

        [HttpPost]
        public ActionResult AddEdit(Party party)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            var bytes = new byte[7000];
            string fileName = "";

            if (ModelState.IsValid)
            {
                var id = party.Id;
                party.IsDeleted = false;
                party.IsActive = true;
                party.UserId = userId;
                var PartyImageList = new List<PartyImage>();
                if (party.Id > 0)
                {

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("Party", "Edit", "AddEdit", party.Id, null, "الحفلات");

                    List<PartyImage> imageList = db.PartyImages.Where(i => i.MainDocId == party.Id).ToList();

                   // if (party.PartyImages != null)
                   // {
                        var lastPartyImage = db.PartyImages.OrderByDescending(a => a.Id).FirstOrDefault();
                        int LastItemID = (lastPartyImage != null ? lastPartyImage.Id : 0) + 1;

                        foreach (var img in party.PartyImages)
                        {

                            if (img != null && img.Image.Contains("base64"))
                            {
                                fileName = "/images/PartyManagement/Party/Party" + LastItemID.ToString() + ".jpeg";
                                if (img.Image.Contains("jpeg"))
                                {
                                    bytes = Convert.FromBase64String(img.Image.Replace("data:image/jpeg;base64,", ""));
                                }
                                else
                                {
                                    bytes = Convert.FromBase64String(img.Image.Replace("data:image/png;base64,", ""));
                                }
                                using (var imageFile = new FileStream(Server.MapPath(fileName), FileMode.Create))
                                {
                                    imageFile.Write(bytes, 0, bytes.Length);
                                    imageFile.Flush();
                                }
                                PartyImageList.Add(new PartyImage()
                                {
                                    Image = domainName + fileName,
                                    MainDocId = party.Id
                                });
                                LastItemID++;
                            }
                            else //previous images
                            {
                                PartyImageList.Add(new PartyImage()
                                {
                                    Image = img.Image,
                                    MainDocId = party.Id
                                });
                            }
                        }
                        db.PartyImages.RemoveRange(db.PartyImages.Where(x => x.MainDocId == party.Id));
                        var PartyImages = party.PartyImages.ToList();
                        PartyImages.ForEach((x) => x.MainDocId = party.Id);
                        party.PartyImages = null;
                    db.Chairs.RemoveRange(db.Chairs.Where(x => x.PartyId == party.Id));
                    var chairs = party.Chairs.ToList();
                    chairs.ForEach((x) => x.PartyId = party.Id);
                    party.Chairs = null;
                    db.Entry(party).State = EntityState.Modified;
                    db.PartyImages.AddRange(PartyImageList);
                    db.Chairs.AddRange(chairs);

                    //}

                }
                else
                {
                    var lastPartyImage = db.PartyImages.OrderByDescending(a => a.Id).FirstOrDefault();
                    int LastItemID = (lastPartyImage != null ? lastPartyImage.Id : 0) + 1;
                    foreach (var img in party.PartyImages)
                    {
                        if (img != null && img.Image.Contains("base64"))
                        {

                            fileName = "/images/PartyManagement/Party/Party" + LastItemID.ToString() + ".jpeg";
                            if (img.Image.Contains("jpeg"))
                            {
                                bytes = Convert.FromBase64String(img.Image.Replace("data:image/jpeg;base64,", ""));
                            }
                            else
                            {
                                bytes = Convert.FromBase64String(img.Image.Replace("data:image/png;base64,", ""));
                            }
                            using (var imageFile = new FileStream(Server.MapPath(fileName), FileMode.Create))
                            {
                                imageFile.Write(bytes, 0, bytes.Length);
                                imageFile.Flush();
                            }
                            PartyImageList.Add(new PartyImage()
                            {
                                Image = domainName + fileName,
                                MainDocId = LastItemID
                            });
                        }
                        LastItemID++;
                    }
                    party.PartyImages = PartyImageList;
                    db.Parties.Add(party);

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("Party", "Add", "AddEdit", party.Id, null, "الحفلات");
                }
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = party.Id > 0 ? "تعديل الحفلات" : "اضافة الحفلات",
                    EnAction = "AddEdit",
                    ControllerName = "Party",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "Post",
                    SelectedItem = party.Id,
                    CodeOrDocNo = party.Code
                });

                return Json(new { success = "true" });

            }
            var errors = ModelState
                   .Where(x => x.Value.Errors.Count > 0)
                   .Select(x => new { x.Key, x.Value.Errors })
                   .ToArray();
            return Json(new { success = "false" });
        }


        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                Party party = db.Parties.Find(id);
                party.IsDeleted = true;
                party.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Chairs.RemoveRange(db.Chairs.Where(x => x.PartyId == id));
                db.PartyBookings.RemoveRange(db.PartyBookings.Where(x => x.PartyId == id));

                db.Entry(party).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف الحفلات",
                    EnAction = "AddEdit",
                    ControllerName = "Party",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,

                });
                Notification.GetNotification("Party", "Delete", "Delete", id, null, "الحفلات");


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
                Party party = db.Parties.Find(id);
                if (party.IsActive == true)
                {
                    party.IsActive = false;
                }
                else
                {
                    party.IsActive = true;
                }
                party.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(party).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)party.IsActive ? "تنشيط الحفلات" : "إلغاء تنشيط الحفلات",
                    EnAction = "AddEdit",
                    ControllerName = "Party",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = party.Id,
                    CodeOrDocNo = party.Code
                });
                if (party.IsActive == true)
                {
                    Notification.GetNotification("Party", "Activate/Deactivate", "ActivateDeactivate", id, true, "الحفلات");
                }
                else
                {

                    Notification.GetNotification("Party", "Activate/Deactivate", "ActivateDeactivate", id, false, " الحفلات");
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
            var code = QueryHelper.CodeLastNum("Party");
            return Json(code + 1, JsonRequestBehavior.AllowGet);
        } 
        
        [SkipERPAuthorize]
        public JsonResult GetChairsPrices(int PartyId)
        {
            var ChairsPrices = db.Chairs.Where(c => c.IsActive == true && c.IsDeleted == false && c.PartyId == PartyId).Select(b => new
            {
                FromNo = db.Chairs.Where(a => a.PartyId == PartyId && a.Price == b.Price).FirstOrDefault().ChairNo,
                ToNo = db.Chairs.Where(a => a.PartyId == PartyId && a.Price == b.Price).OrderByDescending(a => a.Id).FirstOrDefault().ChairNo,
                b.Price
            }).Distinct().ToList();
            var Chairs= db.Chairs.Where(c => c.IsActive == true && c.IsDeleted == false && c.PartyId == PartyId).Select(b => new
            {
                b.Id,
                b.IsReserved,
                b.ChairNo,
                b.Price
            }).ToList();
            return Json(new { ChairsPrices, Chairs }, JsonRequestBehavior.AllowGet);
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