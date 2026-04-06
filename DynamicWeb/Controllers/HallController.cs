using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using MyERP.Models;
using MyERP.Repository;

namespace MyERP.Controllers
{
    public class HallController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        // GET: Hall
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة القاعات",
                EnAction = "Index",
                ControllerName = "Hall",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            Notification.GetNotification("Hall", "View", "Index", null, null, "القاعات");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }
            IQueryable<Hall> halls;
            if (string.IsNullOrEmpty(searchWord))
            {
                halls = db.Halls.Where(a => a.IsDeleted == false&&a.IsActive==true).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Halls.Where(a => a.IsDeleted == false && a.IsActive == true).Count();
            }
            else
            {
                halls = db.Halls.Where(a => a.IsDeleted == false && a.IsActive == true &&
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Halls.Where(a => a.IsDeleted == false && a.IsActive == true && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(halls.ToList());
        }
        public ActionResult AddEdit(int? id)
        {
            var SystemPageId = db.SystemPages.Where(a => a.IsActive == true && a.IsDeleted == false && a.ControllerName == "Hall").FirstOrDefault().Id;
            SystemSetting systemSetting =  db.SystemSettings.Any() ?  db.SystemSettings.FirstOrDefault() : new SystemSetting();
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            if (id == null)
            {
                //var Code = SetCodeNum();
                //ViewBag.Code = int.Parse(Code.Data.ToString());
                ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
                {
                    b.Id,
                    ArName = b.FixedPart + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", systemSetting.DefaultDepartmentId);
                return View();
            }
            Hall hall = db.Halls.Find(id);
            if (hall == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل القاعات ",
                EnAction = "AddEdit",
                ControllerName = "Hall",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });

            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", hall.DepartmentId);
            ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
            {
                b.Id,
                ArName = b.FixedPart + " - " + b.ArName
            }), "Id", "ArName", hall.FieldsCodingId);
            ViewBag.Next = QueryHelper.Next((int)id, "Hall");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Hall");
            ViewBag.Last = QueryHelper.GetLast("Hall");
            ViewBag.First = QueryHelper.GetFirst("Hall");
            return View(hall);
        }
        [HttpPost]
        public ActionResult AddEdit(Hall hall, string newBtn)
        {
            var SystemPageId = db.SystemPages.Where(a => a.IsActive == true && a.IsDeleted == false && a.ControllerName == "Hall").FirstOrDefault().Id;
            if (ModelState.IsValid)
            {
                var id = hall.Id;
                hall.IsDeleted = false;
                if (hall.Id > 0)
                {
                    hall.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    db.Entry(hall).State = EntityState.Modified;
                    Notification.GetNotification("Hall", "Edit", "AddEdit", hall.Id, null, "القاعات");
                }
                else
                {
                    hall.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    //hall.Code = (QueryHelper.CodeLastNum("Hall") + 1).ToString();
                    hall.Code = new JavaScriptSerializer().Serialize(SetCodeNum(hall.FieldsCodingId).Data).ToString().Trim('"');
                    hall.IsActive = true;
                    db.Halls.Add(hall);
                    Notification.GetNotification("Hall", "Add", "AddEdit", hall.Id, null, "القاعات");
                }
                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    var mod = ModelState.First(c => c.Key == "Code");  // this
                    mod.Value.Errors.Add("هذا الكود موجود من قبل");
                    return View(hall);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل قاعة" : "اضافة قاعة",
                    EnAction = "AddEdit",
                    ControllerName = "Hall",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = hall.Code
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
            ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
            {
                b.Id,
                ArName = b.FixedPart + " - " + b.ArName
            }), "Id", "ArName", hall.FieldsCodingId);
            return View(hall);
        }
        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                Hall hall = db.Halls.Find(id);
                hall.IsDeleted = true;
                hall.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                Random random = new Random();
                const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
                hall.Code = Code;
                hall.FieldsCodingId = null;
                db.Entry(hall).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف قاعة",
                    EnAction = "AddEdit",
                    ControllerName = "Hall",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = hall.EnName
                });
                Notification.GetNotification("Hall", "Delete", "Delete", id, null, "القاعات");
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
                Hall hall = db.Halls.Find(id);
                if (hall.IsActive == true)
                {
                    hall.IsActive = false;
                }
                else
                {
                    hall.IsActive = true;
                }
                hall.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(hall).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)hall.IsActive ? "تنشيط القاعات" : "إلغاء تنشيط القاعات",
                    EnAction = "AddEdit",
                    ControllerName = "Hall",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = hall.Id,
                    EnItemName = hall.EnName,
                    ArItemName = hall.ArName,
                    CodeOrDocNo = hall.Code
                });
                if (hall.IsActive == true)
                {
                    Notification.GetNotification("Hall", "Activate/Deactivate", "ActivateDeactivate", id, true, "القاعات");
                }
                else
                {
                    Notification.GetNotification("Hall", "Activate/Deactivate", "ActivateDeactivate", id, false, " القاعات");
                }
                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }
        [SkipERPAuthorize]
        public JsonResult SetCodeNum(int? FieldsCodingId)
        {
            if (FieldsCodingId > 0)
            {
                double result = 0;
                var fieldsCoding = db.FieldsCodings.Where(a => a.Id == FieldsCodingId).FirstOrDefault();
                var fixedPart = fieldsCoding.FixedPart;
                var noOfDigits = fieldsCoding.DigitsNo;
                var IsAutomaticSequence = fieldsCoding.IsAutomaticSequence;
                var IsZerosFills = fieldsCoding.IsZerosFills;
                if (string.IsNullOrEmpty(fixedPart))
                {
                    var code = db.Halls.Where(a => a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
                    if (code.FirstOrDefault() == null)
                    {
                        result = 0 + 1;
                    }
                    else
                    {
                        result = (double.Parse(code.FirstOrDefault().ToString())) + 1;
                    }
                    var CodeNo = "";
                    if (IsZerosFills == true)
                    {
                        if (result.ToString().Length < noOfDigits)
                        {
                            CodeNo = QueryHelper.FillsWithZeros(noOfDigits, result.ToString());
                        }
                        else
                        {
                            CodeNo = result.ToString();
                        }
                    }
                    else
                    {
                        CodeNo = result.ToString();
                    }
                    return Json(CodeNo, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    var FullNewCode = "";
                    var CodeNo = "";
                    var code = db.Halls.Where(a => a.Code.Contains(fixedPart) && a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
                    if (code.FirstOrDefault() == null)
                    {
                        result = 0 + 1;
                        if (IsZerosFills == true)
                        {
                            if (result.ToString().Length < noOfDigits)
                            {
                                CodeNo = QueryHelper.FillsWithZeros(noOfDigits /*- fixedPart.Length*/, result.ToString());
                            }
                            else
                            {
                                CodeNo = result.ToString();
                            }
                        }
                        else
                        {
                            CodeNo = result.ToString();
                        }
                        FullNewCode = fixedPart + CodeNo;
                    }
                    else
                    {
                        var LastCode = code.FirstOrDefault().ToString();
                        result = double.Parse(LastCode.Substring(LastCode.LastIndexOf(fixedPart) + fixedPart.Length)) + 1;
                        if (IsZerosFills == true)
                        {
                            if (result.ToString().Length < noOfDigits)
                            {
                                CodeNo = QueryHelper.FillsWithZeros(noOfDigits /*- fixedPart.Length*/, result.ToString());
                            }
                            else
                            {
                                CodeNo = result.ToString();
                            }
                        }
                        else
                        {
                            CodeNo = result.ToString();
                        }
                        FullNewCode = fixedPart + CodeNo;
                    }
                    return Json(FullNewCode, JsonRequestBehavior.AllowGet);
                }
            }
            else
            {
                double result = 0;
                var code = db.Halls.Where(a => a.FieldsCodingId == null && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
                if (code.FirstOrDefault() == null)
                {
                    result = 0 + 1;
                }
                else
                {
                    result = double.Parse(code.FirstOrDefault().ToString()) + 1;
                }
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            //var code = QueryHelper.CodeLastNum("Hall");
            //return Json(code + 1, JsonRequestBehavior.AllowGet);
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