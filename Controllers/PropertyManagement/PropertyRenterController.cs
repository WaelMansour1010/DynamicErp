using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using MyERP.Models;
using MyERP.Repository;

namespace MyERP.Controllers.PropertyManagement
{
    public class PropertyRenterController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: PropertyRenter
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة المستأجرين",
                EnAction = "Index",
                ControllerName = "PropertyRenter",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            Notification.GetNotification("PropertyRenter", "View", "Index", null, null, "المستأجرين");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }
            IQueryable<PropertyRenter> renters;
            if (string.IsNullOrEmpty(searchWord))
            {
                renters = db.PropertyRenters.Where(a => a.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.PropertyRenters.Where(a => a.IsDeleted == false).Count();
            }
            else
            {
                renters = db.PropertyRenters.Where(a => a.IsDeleted == false &&
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.PropertyRenters.Where(a => a.IsDeleted == false && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(renters.ToList());
        }
        public ActionResult AddEdit(int? id)
        {
            SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            var departments = departmentRepository.UserDepartments(1).ToList();//show all deprartments so that user can select factory department

            if (id == null)
            {

                ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", systemSetting.DefaultDepartmentId);
                ViewBag.CustomerId = new SelectList(db.Customers.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.CustomerRepId = new SelectList(db.Employees.Where(c => c.IsActive == true && c.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName") ;
                ViewBag.JobId = new SelectList(db.Jobs.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.RenterTypeId = new SelectList(new List<dynamic> {
                    new { Id=1, ArName="أفراد"},
                    new { Id=2, ArName="شركات"}}, "Id", "ArName");

                DateTime utcNow = DateTime.UtcNow;
                TimeZone curTimeZone = TimeZone.CurrentTimeZone;
                // TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
                TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById(curTimeZone.StandardName);
                DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);

                ViewBag.RegistrationDate = cTime.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.NationalExpiryDate = cTime.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.Birthdate = cTime.ToString("yyyy-MM-ddTHH:mm");

                return View();
            }
            PropertyRenter renter = db.PropertyRenters.Find(id);
            if (renter == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل المستأجرين ",
                EnAction = "AddEdit",
                ControllerName = "PropertyRenter",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });
            ViewBag.Next = QueryHelper.Next((int)id, "PropertyRenter");
            ViewBag.Previous = QueryHelper.Previous((int)id, "PropertyRenter");
            ViewBag.Last = QueryHelper.GetLast("PropertyRenter");
            ViewBag.First = QueryHelper.GetFirst("PropertyRenter");

            ViewBag.CustomerId = new SelectList(db.Customers.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", renter.CustomerId);
            ViewBag.CustomerRepId = new SelectList(db.Employees.Where(c => c.IsActive == true && c.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", renter.CustomerRepId);
            ViewBag.JobId = new SelectList(db.Jobs.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", renter.JobId);
            ViewBag.RenterTypeId = new SelectList(new List<dynamic> {
                    new { Id=1, ArName="أفراد"},
                    new { Id=2, ArName="شركات"}}, "Id", "ArName", renter.RenterTypeId);
            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName",renter.DepartmentId);

            ViewBag.RegistrationDate = renter.RegistrationDate != null ? renter.RegistrationDate.Value.ToString("yyyy-MM-ddTHH:mm") : null;
            ViewBag.NationalExpiryDate = renter.NationalExpiryDate != null ? renter.NationalExpiryDate.Value.ToString("yyyy-MM-ddTHH:mm") : null;
            ViewBag.Birthdate = renter.Birthdate != null ? renter.Birthdate.Value.ToString("yyyy-MM-ddTHH:mm") : null;

            return View(renter);
        }
        [HttpPost]
        public ActionResult AddEdit(PropertyRenter renter)
        {
            if (ModelState.IsValid)
            {
                var id = renter.Id;
                renter.IsDeleted = false;
                renter.IsActive = true;
                renter.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
                var bytes = new byte[7000];
                string fileName = "";
                List<PropertyRenterImage> PropertyRenterimageList = db.PropertyRenterImages.Where(i => i.MainDocId == renter.Id).ToList();

                if (renter.Id > 0)
                {
                    var lastPropertyRenterImage = db.PropertyRenterImages.OrderByDescending(a => a.Id).FirstOrDefault();
                    int LastItemID = (lastPropertyRenterImage != null ? lastPropertyRenterImage.Id : 0) + 1;
                    List<PropertyRenterImage> imageList = db.PropertyRenterImages.Where(i => i.MainDocId == renter.Id).ToList();

                    foreach (var img in renter.PropertyRenterImages)
                    {

                        if (img != null && img.Image.Contains("base64"))
                        {
                            fileName = "/images/PropertyManagement/PropertyRenter/PropertyRenter" + LastItemID.ToString() + ".jpeg";
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
                            PropertyRenterimageList.Add(new PropertyRenterImage()
                            {
                                Image = domainName + fileName,
                                MainDocId = renter.Id
                            });
                            LastItemID++;
                        }
                        else //previous images
                        {
                            PropertyRenterimageList.Add(new PropertyRenterImage()
                            {
                                Image = img.Image,
                                MainDocId = renter.Id
                            });
                        }
                    }
                    db.PropertyRenterImages.RemoveRange(db.PropertyRenterImages.Where(x => x.MainDocId == renter.Id));
                    var PropertyRenterImages = renter.PropertyRenterImages.ToList();
                    PropertyRenterImages.ForEach((x) => x.MainDocId = renter.Id);
                    renter.PropertyRenterImages = null;
                    db.Entry(renter).State = EntityState.Modified;
                    db.PropertyRenterImages.AddRange(PropertyRenterImages);

                    Notification.GetNotification("PropertyRenter", "Edit", "AddEdit", renter.Id, null, "المستأجرين");
                }
                else
                {
                    renter.Code = new JavaScriptSerializer().Serialize(SetCodeNum(renter.DepartmentId).Data).ToString().Trim('"');
                    var lastPropertyRenterImage = db.PropertyRenterImages.OrderByDescending(a => a.Id).FirstOrDefault();
                    int LastItemID = (lastPropertyRenterImage != null ? lastPropertyRenterImage.Id : 0) + 1;
                    foreach (var img in renter.PropertyRenterImages)
                    {
                        if (img != null && img.Image.Contains("base64"))
                        {

                            fileName = "/images/PropertyManagement/PropertyRenter/PropertyRenter" + LastItemID.ToString() + ".jpeg";
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
                            PropertyRenterimageList.Add(new PropertyRenterImage()
                            {
                                Image = domainName + fileName,
                                MainDocId = LastItemID
                            });
                        }
                        LastItemID++;
                    }
                    renter.PropertyRenterImages = PropertyRenterimageList;
                    db.PropertyRenters.Add(renter);
                    Notification.GetNotification("PropertyRenter", "Add", "AddEdit", renter.Id, null, "المستأجرين");
                }
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل المستأجرين" : "اضافة المستأجرين",
                    EnAction = "AddEdit",
                    ControllerName = "PropertyRenter",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = renter.Code
                });
                return Json(new { success = true });

            }
            var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();
            return Json(new { success = false });
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                PropertyRenter renter = db.PropertyRenters.Find(id);
                renter.IsDeleted = true;
                renter.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(renter).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف المستأجرين",
                    EnAction = "AddEdit",
                    ControllerName = "PropertyRenter",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = renter.EnName
                });
                Notification.GetNotification("PropertyRenter", "Delete", "Delete", id, null, "المستأجرين");
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
                PropertyRenter renter = db.PropertyRenters.Find(id);
                if (renter.IsActive == true)
                {
                    renter.IsActive = false;
                }
                else
                {
                    renter.IsActive = true;
                }
                renter.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(renter).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)renter.IsActive ? "تنشيط المستأجرين" : "إلغاء تنشيط المستأجرين",
                    EnAction = "AddEdit",
                    ControllerName = "PropertyRenter",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = renter.Id,
                    EnItemName = renter.EnName,
                    ArItemName = renter.ArName,
                    CodeOrDocNo = renter.Code
                });
                if (renter.IsActive == true)
                {
                    Notification.GetNotification("PropertyRenter", "Activate/Deactivate", "ActivateDeactivate", id, true, "المستأجرين");
                }
                else
                {
                    Notification.GetNotification("PropertyRenter", "Activate/Deactivate", "ActivateDeactivate", id, false, " المستأجرين");
                }
                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }

        [SkipERPAuthorize]
        public JsonResult SetCodeNum(int? id)
        {
            var LastCode = db.Database.SqlQuery<string>($"select isnull((select top(1) Code from PropertyRenter where [DepartmentId] = " + id + "order by  [Id] desc),0)");
            var _Code = double.Parse(LastCode.FirstOrDefault().ToString());
            double i = (_Code) + 1;
            return Json(i, JsonRequestBehavior.AllowGet);
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