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
using System.Web.Script.Serialization;

namespace MyERP.Controllers.HotelManagement
{
    [AllowAnonymous]
    [SkipERPAuthorize]
    public class RoomTypeController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: RoomType
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة انواع الشاليهات",
                EnAction = "Index",
                ControllerName = "RoomType",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });

            Notification.GetNotification("RoomType", "View", "Index", null, null, "انواع الشاليهات");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }

            IQueryable<RoomType> roomTypes;
            if (string.IsNullOrEmpty(searchWord))
            {
                roomTypes = db.RoomTypes.Where(a => a.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.RoomTypes.Where(a => a.IsDeleted == false).Count();
            }
            else
            {
                roomTypes = db.RoomTypes.Where(a => a.IsDeleted == false &&
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.RoomTypes.Where(a => a.IsDeleted == false && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;

            return View(roomTypes.ToList());
        }

        // GET: RoomType/Edit/5
        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {
                ViewBag.TypeId = new SelectList(new List<dynamic> {
                    new { Id=1, ArName="رئيسى"},
                    new { Id=2, ArName="وسيط"} ,
                    new { Id=3, ArName="فرعي"}
                }, "Id", "ArName");
                ViewBag.ParentId = new SelectList(db.RoomTypes.Where(c => c.IsActive == true && c.IsDeleted == false).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }).ToList(), "Id", "ArName");
                return View();
            }

            RoomType roomType = db.RoomTypes.Find(id);
            if (roomType == null)
                return HttpNotFound();

            ViewBag.Next = QueryHelper.Next((int)id, "RoomType");
            ViewBag.Previous = QueryHelper.Previous((int)id, "RoomType");
            ViewBag.Last = QueryHelper.GetLast("RoomType");
            ViewBag.First = QueryHelper.GetFirst("RoomType");

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل انواع الشاليهات",
                EnAction = "AddEdit",
                ControllerName = "RoomType",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = roomType.Id,
                CodeOrDocNo = roomType.Code
            });
            ViewBag.TypeId = new SelectList(new List<dynamic> {
                   new { Id=1, ArName="رئيسى"},
                    new { Id=2, ArName="وسيط"} ,
                    new { Id=3, ArName="فرعي"}}, "Id", "ArName", roomType.TypeId);
            //ViewBag.ParentId = new SelectList(db.RoomTypes.Where(c => c.IsActive == true && c.IsDeleted == false).Select(b => new
            //{
            //    b.Id,
            //    ArName = b.Code + " - " + b.ArName
            //}).ToList(), "Id", "ArName", roomType.ParentId);

            if (roomType.TypeId == 3) // فرعى
            {
                ViewBag.ParentId = new SelectList(db.RoomTypes.Where(c => c.IsActive == true && c.IsDeleted == false && c.TypeId == 2).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }).ToList(), "Id", "ArName", roomType.ParentId);
            }
            else if (roomType.TypeId == 2)
            {
                ViewBag.ParentId = new SelectList(db.RoomTypes.Where(c => c.IsActive == true && c.IsDeleted == false && c.TypeId == 1).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }).ToList(), "Id", "ArName", roomType.ParentId);
            }
            else
            {
                ViewBag.ParentId = new SelectList(db.RoomTypes.Where(c => c.IsActive == true && c.IsDeleted == false && c.ParentId == null && c.TypeId == 1).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }).ToList(), "Id", "ArName", roomType.ParentId);
            }

            return View(roomType);
        }

        [HttpPost]
        public ActionResult AddEdit(RoomType roomType)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            var bytes = new byte[7000];
            string fileName = "";

            if (ModelState.IsValid)
            {
                var id = roomType.Id;
                roomType.IsDeleted = false;
                roomType.IsActive = true;
                roomType.UserId = userId;
                var RoomTypeImageList = new List<RoomTypeImage>();
                if (roomType.Id > 0)
                {

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("RoomType", "Edit", "AddEdit", roomType.Id, null, "انواع الشاليهات");

                    List<RoomTypeImage> imageList = db.RoomTypeImages.Where(i => i.MainDocId == roomType.Id).ToList();

                    if (roomType.RoomTypeImages != null)
                    {
                        //// For Deleting .
                        //foreach (RoomTypeImage deletedImage in imageList.Where(i => i.MainDocId == roomType.Id).ToList())
                        //{
                        //    db.RoomTypeImages.Remove(deletedImage);
                        //    db.SaveChanges();

                        //}

                        var lastRoomTypeImage = db.RoomTypeImages.OrderByDescending(a => a.Id).FirstOrDefault();
                        int LastItemID = (lastRoomTypeImage != null ? lastRoomTypeImage.Id : 0) + 1;

                        foreach (var img in roomType.RoomTypeImages)
                        {

                            if (img != null && img.Image.Contains("base64"))
                            {
                                fileName = "/RoomType" + LastItemID.ToString() /*+ "_" + roomType.ArName*/ + ".jpeg";
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
                                RoomTypeImageList.Add(new RoomTypeImage()
                                {
                                    Image = domainName + fileName,
                                    MainDocId = roomType.Id
                                });
                                LastItemID++;
                            }
                            else //previous images
                            {
                                RoomTypeImageList.Add(new RoomTypeImage()
                                {
                                    Image = img.Image,
                                    MainDocId = roomType.Id
                                });
                            }
                        }

                        db.RoomTypeImages.RemoveRange(db.RoomTypeImages.Where(x => x.MainDocId == roomType.Id));
                        var RoomTypeImages = roomType.RoomTypeImages.ToList();
                        RoomTypeImages.ForEach((x) => x.MainDocId = roomType.Id);
                        roomType.RoomTypeImages = null;
                        db.Entry(roomType).State = EntityState.Modified;
                        db.RoomTypeImages.AddRange(RoomTypeImageList);
                    }
                }
                else
                {
                    var lastRoomTypeImage = db.RoomTypeImages.OrderByDescending(a => a.Id).FirstOrDefault();
                    int LastItemID = (lastRoomTypeImage != null ? lastRoomTypeImage.Id : 0) + 1;

                    foreach (var img in roomType.RoomTypeImages)
                    {
                        if (img != null && img.Image.Contains("base64"))
                        {

                            fileName = "/RoomType" + LastItemID.ToString() /*+ "_" + roomType.ArName */+ ".jpeg";
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
                            RoomTypeImageList.Add(new RoomTypeImage()
                            {
                                Image = domainName + fileName,
                                MainDocId = LastItemID
                            });
                        }
                        LastItemID++;
                    }
                    //db.RoomTypeImages.RemoveRange(db.RoomTypeImages.Where(x => x.MainDocId == roomType.Id));
                    //var RoomTypeImages = roomType.RoomTypeImages.ToList();
                    //RoomTypeImages.ForEach((x) => x.MainDocId = roomType.Id);
                    //roomType.RoomTypeImages = null;
                    //db.Entry(roomType).State = EntityState.Modified;
                    //db.RoomTypeImages.AddRange(RoomTypeImageList);

                    roomType.RoomTypeImages = RoomTypeImageList;
                    db.RoomTypes.Add(roomType);


                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("RoomType", "Add", "AddEdit", roomType.Id, null, "انواع الشاليهات");
                }
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = roomType.Id > 0 ? "تعديل انواع الشاليهات" : "اضافة انواع الشاليهات",
                    EnAction = "AddEdit",
                    ControllerName = "RoomType",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "Post",
                    SelectedItem = roomType.Id,
                    CodeOrDocNo = roomType.Code
                });

                return Json(new { success = "true" });

            }
            var errors = ModelState
                   .Where(x => x.Value.Errors.Count > 0)
                   .Select(x => new { x.Key, x.Value.Errors })
                   .ToArray();
            return View(roomType);
        }


        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                RoomType roomType = db.RoomTypes.Find(id);
                roomType.IsDeleted = true;
                roomType.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(roomType).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف نوع سيارة",
                    EnAction = "AddEdit",
                    ControllerName = "RoomType",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = roomType.EnName

                });
                Notification.GetNotification("RoomType", "Delete", "Delete", id, null, "انواع الشاليهات");


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
                RoomType roomType = db.RoomTypes.Find(id);
                if (roomType.IsActive == true)
                {
                    roomType.IsActive = false;
                }
                else
                {
                    roomType.IsActive = true;
                }
                roomType.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(roomType).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)roomType.IsActive ? "تنشيط انواع الشاليهات" : "إلغاء تنشيط انواع الشاليهات",
                    EnAction = "AddEdit",
                    ControllerName = "RoomType",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = roomType.Id,
                    EnItemName = roomType.EnName,
                    ArItemName = roomType.ArName,
                    CodeOrDocNo = roomType.Code
                });
                if (roomType.IsActive == true)
                {
                    Notification.GetNotification("RoomType", "Activate/Deactivate", "ActivateDeactivate", id, true, "انواع الشاليهات");
                }
                else
                {

                    Notification.GetNotification("RoomType", "Activate/Deactivate", "ActivateDeactivate", id, false, " انواع الشاليهات");
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
            var code = QueryHelper.CodeLastNum("RoomType");
            return Json(code + 1, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public ActionResult RoomDetails(int Id)
        {
            DateTime utcNow = DateTime.UtcNow;
            TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
            ViewBag.CheckInDate = cTime.ToString("yyyy-MM-ddTHH:mm");
            ViewBag.CheckOutDate = cTime.ToString("yyyy-MM-ddTHH:mm");

            var RoomType = db.RoomTypes.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == Id).FirstOrDefault();
            return View(RoomType);
        }
        [SkipERPAuthorize]
        public ActionResult GetRoomByType(int? TypeId)
        {
            var parent = db.RoomTypes.Where(a => a.IsActive == true && a.IsDeleted == false && a.ParentId == null && a.TypeId == 1)
                               .Select(a => new
                               {
                                   a.Id,
                                   ArName = a.Code + " - " + a.ArName
                               }).ToList();
            if (TypeId == 3)
            {
                parent = db.RoomTypes.Where(a => a.IsActive == true && a.IsDeleted == false && a.TypeId == 2)
                              .Select(a => new
                              {
                                  a.Id,
                                  ArName = a.Code + " - " + a.ArName
                              }).ToList();
            }

            return Json(parent, JsonRequestBehavior.AllowGet);
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