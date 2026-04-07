using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using MyERP.Repository;

namespace MyERP.Controllers.HotelManagement
{
    public class RoomController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: Room
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة الغرف",
                EnAction = "Index",
                ControllerName = "Room",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            Notification.GetNotification("Room", "View", "Index", null, null, "الغرف");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }
            IQueryable<Room> rooms;
            if (string.IsNullOrEmpty(searchWord))
            {
                rooms = db.Rooms.Where(a => a.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Rooms.Where(a => a.IsDeleted == false).Count();
            }
            else
            {
                rooms = db.Rooms.Where(a => a.IsDeleted == false &&
                (a.RoomNumber.Contains(searchWord) || a.Building.ArName.Contains(searchWord) || a.Building.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Rooms.Where(a => a.IsDeleted == false && (a.RoomNumber.Contains(searchWord) || a.Building.ArName.Contains(searchWord) || a.Building.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(rooms.ToList());
        }

        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {
                ViewBag.BuildingId = new SelectList(db.Buildings.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.FloorId = new SelectList(new List<dynamic> {
                    new { Id=1, ArName="أرضي"},
                    new { Id=2, ArName="أول علوي"},
                    new { Id=3, ArName="ثانى علوي"},
                    new { Id=4, ArName="ثالث علوي"}}, "Id", "ArName");
                ViewBag.RoomTypeId = new SelectList(db.RoomTypes.Where(a => a.IsActive == true && a.IsDeleted == false && a.ParentId != null && a.TypeId != 1).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                return View();
            }
            Room room = db.Rooms.Find(id);
            ViewBag.BuildingId = new SelectList(db.Buildings.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", room.BuildingId);

            ViewBag.FloorId = new SelectList(new List<dynamic> {
                    new { Id=1, ArName="أرضي"},
                    new { Id=2, ArName="أول علوي"},
                    new { Id=3, ArName="ثانى علوي"},
                    new { Id=4, ArName="ثالث علوي"}}, "Id", "ArName", room.FloorId);
            ViewBag.RoomTypeId = new SelectList(db.RoomTypes.Where(a => a.IsActive == true && a.IsDeleted == false&&a.ParentId!=null &&a.TypeId!=1).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName",room.RoomTypeId);
            if (room == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل الغرف ",
                EnAction = "AddEdit",
                ControllerName = "Room",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });
            ViewBag.Next = QueryHelper.Next((int)id, "Room");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Room");
            ViewBag.Last = QueryHelper.GetLast("Room");
            ViewBag.First = QueryHelper.GetFirst("Room");
            return View(room);
        }
        [HttpPost]
        public ActionResult AddEdit(Room room, string newBtn)
        {
            if (ModelState.IsValid)
            {
                var id = room.Id;
                room.IsDeleted = false;
                room.IsActive = true;
                if (room.Id > 0)
                {
                    room.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    db.Entry(room).State = EntityState.Modified;
                    Notification.GetNotification("Room", "Edit", "AddEdit", room.Id, null, "الغرف");
                }
                else
                {
                    room.RoomStatusId = 1;// Empty
                    room.IsReserved = false;
                    room.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    room.Code = (QueryHelper.CodeLastNum("Room") + 1).ToString();
                    db.Rooms.Add(room);
                    Notification.GetNotification("Room", "Add", "AddEdit", room.Id, null, "الغرف");
                }
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل الغرف" : "اضافة الغرف",
                    EnAction = "AddEdit",
                    ControllerName = "Room",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = room.Code
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
            var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();
            return View(room);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                Room room = db.Rooms.Find(id);
                room.IsDeleted = true;
                room.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(room).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف الغرف",
                    EnAction = "AddEdit",
                    ControllerName = "Room",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id
                });
                Notification.GetNotification("Room", "Delete", "Delete", id, null, "الغرف");
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
                Room room = db.Rooms.Find(id);
                if (room.IsActive == true)
                {
                    room.IsActive = false;
                }
                else
                {
                    room.IsActive = true;
                }
                room.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(room).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)room.IsActive ? "تنشيط الغرف" : "إلغاء تنشيط الغرف",
                    EnAction = "AddEdit",
                    ControllerName = "Room",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = room.Id,
                    CodeOrDocNo = room.Code
                });
                if (room.IsActive == true)
                {
                    Notification.GetNotification("Room", "Activate/Deactivate", "ActivateDeactivate", id, true, "الغرف");
                }
                else
                {
                    Notification.GetNotification("Room", "Activate/Deactivate", "ActivateDeactivate", id, false, " الغرف");
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
            var code = QueryHelper.CodeLastNum("Room");
            return Json(code + 1, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult GetRoomTypePrice(int? RoomTypeId)
        {
            var Price = db.RoomTypes.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == RoomTypeId).FirstOrDefault().Price;
            return Json(Price, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult ChangeRoomStatus(DateTime? RegistrationDate)
        {
           // RegistrationDate = DateTime.Parse("1/20/2022");
            var roomBooking = db.RoomBookings.Where(a => a.IsDeleted == false && (a.BookingEndDate.Value.Year == RegistrationDate.Value.Year && a.BookingEndDate.Value.Month == RegistrationDate.Value.Month && a.BookingEndDate.Value.Day <= RegistrationDate.Value.Day)).ToList();
            foreach (var item in roomBooking)
            {
                if (item.Room.RoomStatusId != 1)
                {
                    var room = db.Rooms.Find(item.RoomId);
                    if (room.IsReserved == true)
                    {
                        room.RoomStatusId = 1; // Empty
                        room.IsReserved = false;
                        db.Entry(room).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                }
            }
            return Json("Success", JsonRequestBehavior.AllowGet);
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