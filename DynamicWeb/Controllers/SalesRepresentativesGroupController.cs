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
    public class SalesRepresentativesGroupController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        // GET: SalesRepresentativesGroup
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة مجموعات مندوبى المبيعات",
                EnAction = "Index",
                ControllerName = "SalesRepresentativesGroup",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            Notification.GetNotification("SalesRepresentativesGroup", "View", "Index", null, null, "مجموعات مندوبى المبيعات");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }
            IQueryable<SalesRepresentativesGroup> salesRepresentativesGroups;
            if (string.IsNullOrEmpty(searchWord))
            {
                salesRepresentativesGroups = db.SalesRepresentativesGroups.Where(a => a.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.SalesRepresentativesGroups.Where(a => a.IsDeleted == false).Count();
            }
            else
            {
                salesRepresentativesGroups = db.SalesRepresentativesGroups.Where(a => a.IsDeleted == false &&
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.SalesRepresentativesGroups.Where(a => a.IsDeleted == false && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(salesRepresentativesGroups.ToList());
        }
        public ActionResult AddEdit(int? id)
        {
            var SystemPageId = db.SystemPages.Where(a => a.IsActive == true && a.IsDeleted == false && a.ControllerName == "SalesRepresentativesGroup").FirstOrDefault().Id;
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            if (id == null)
            {
                ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
                {
                    b.Id,
                    ArName = b.FixedPart + " - " + b.ArName
                }), "Id", "ArName");
                return View();
            }
            SalesRepresentativesGroup salesRepresentativesGroup = db.SalesRepresentativesGroups.Find(id);
            if (salesRepresentativesGroup == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل مجموعات مندوبى المبيعات ",
                EnAction = "AddEdit",
                ControllerName = "SalesRepresentativesGroup",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });
            ViewBag.Next = QueryHelper.Next((int)id, "SalesRepresentativesGroup");
            ViewBag.Previous = QueryHelper.Previous((int)id, "SalesRepresentativesGroup");
            ViewBag.Last = QueryHelper.GetLast("SalesRepresentativesGroup");
            ViewBag.First = QueryHelper.GetFirst("SalesRepresentativesGroup");
            ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
            {
                b.Id,
                ArName = b.FixedPart + " - " + b.ArName
            }), "Id", "ArName",salesRepresentativesGroup.FieldsCodingId);
            return View(salesRepresentativesGroup);
        }
        [HttpPost]
        public ActionResult AddEdit(SalesRepresentativesGroup salesRepresentativesGroup, string newBtn)
        {
            var SystemPageId = db.SystemPages.Where(a => a.IsActive == true && a.IsDeleted == false && a.ControllerName == "SalesRepresentativesGroup").FirstOrDefault().Id;
            if (ModelState.IsValid)
            {
                var id = salesRepresentativesGroup.Id;
                salesRepresentativesGroup.IsDeleted = false;
                if (salesRepresentativesGroup.Id > 0)
                {
                    salesRepresentativesGroup.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    db.Entry(salesRepresentativesGroup).State = EntityState.Modified;
                    Notification.GetNotification("SalesRepresentativesGroup", "Edit", "AddEdit", salesRepresentativesGroup.Id, null, "مجموعات مندوبى المبيعات");
                }
                else
                {
                    salesRepresentativesGroup.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    //salesRepresentativesGroup.Code = (QueryHelper.CodeLastNum("SalesRepresentativesGroup") + 1).ToString();
                    salesRepresentativesGroup.Code = new JavaScriptSerializer().Serialize(SetCodeNum(salesRepresentativesGroup.FieldsCodingId).Data).ToString().Trim('"');
                    salesRepresentativesGroup.IsActive = true;
                    db.SalesRepresentativesGroups.Add(salesRepresentativesGroup);
                    Notification.GetNotification("SalesRepresentativesGroup", "Add", "AddEdit", salesRepresentativesGroup.Id, null, "مجموعات مندوبى المبيعات");
                }
                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    var mod = ModelState.First(c => c.Key == "Code");  // this
                    mod.Value.Errors.Add("هذا الكود موجود من قبل");
                    return View(salesRepresentativesGroup);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل مجموعات مندوبى المبيعات" : "اضافة مجموعات مندوبى المبيعات",
                    EnAction = "AddEdit",
                    ControllerName = "SalesRepresentativesGroup",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = salesRepresentativesGroup.Code
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
            }), "Id", "ArName",salesRepresentativesGroup.FieldsCodingId);
            return View(salesRepresentativesGroup);
        }
        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                SalesRepresentativesGroup salesRepresentativesGroup = db.SalesRepresentativesGroups.Find(id);
                salesRepresentativesGroup.IsDeleted = true;
                salesRepresentativesGroup.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                Random random = new Random();
                const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
                salesRepresentativesGroup.Code = Code;
                salesRepresentativesGroup.FieldsCodingId = null;
                db.Entry(salesRepresentativesGroup).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف مجموعات مندوبى المبيعات",
                    EnAction = "AddEdit",
                    ControllerName = "SalesRepresentativesGroup",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = salesRepresentativesGroup.EnName
                });
                Notification.GetNotification("SalesRepresentativesGroup", "Delete", "Delete", id, null, "مجموعات مندوبى المبيعات");
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
                SalesRepresentativesGroup salesRepresentativesGroup = db.SalesRepresentativesGroups.Find(id);
                if (salesRepresentativesGroup.IsActive == true)
                {
                    salesRepresentativesGroup.IsActive = false;
                }
                else
                {
                    salesRepresentativesGroup.IsActive = true;
                }
                salesRepresentativesGroup.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(salesRepresentativesGroup).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)salesRepresentativesGroup.IsActive ? "تنشيط مجموعات مندوبى المبيعات" : "إلغاء تنشيط مجموعات مندوبى المبيعات",
                    EnAction = "AddEdit",
                    ControllerName = "SalesRepresentativesGroup",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = salesRepresentativesGroup.Id,
                    EnItemName = salesRepresentativesGroup.EnName,
                    ArItemName = salesRepresentativesGroup.ArName,
                    CodeOrDocNo = salesRepresentativesGroup.Code
                });
                if (salesRepresentativesGroup.IsActive == true)
                {
                    Notification.GetNotification("SalesRepresentativesGroup", "Activate/Deactivate", "ActivateDeactivate", id, true, "مجموعات مندوبى المبيعات");
                }
                else
                {
                    Notification.GetNotification("SalesRepresentativesGroup", "Activate/Deactivate", "ActivateDeactivate", id, false, " مجموعات مندوبى المبيعات");
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
                    var code = db.SalesRepresentativesGroups.Where(a => a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
                    var code = db.SalesRepresentativesGroups.Where(a => a.Code.Contains(fixedPart) && a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
                var code = db.SalesRepresentativesGroups.Where(a => a.FieldsCodingId == null && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
            //var code = QueryHelper.CodeLastNum("SalesRepresentativesGroup");
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