using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using System;
using System.Security.Claims;
using System.Threading;
using System.Globalization;
using System.Web.Script.Serialization;

namespace MyERP.Controllers.SystemSettings.GroupCoding
{
    public class VendorsGroupController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: vendorGroups
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة مجموعات الموردين",
                EnAction = "Index",
                ControllerName = "VendorsGroup",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });

            ////////////////-----------------------------------------------------------------------
            Notification.GetNotification("VendorsGroup", "View", "Index", null, null, "مجموعات الموردين");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<VendorsGroup> vendorsGroups;

            if (string.IsNullOrEmpty(searchWord))
            {
                vendorsGroups = db.VendorsGroups.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.VendorsGroups.Where(s => s.IsDeleted == false).Count();
            }
            else
            {
                vendorsGroups = db.VendorsGroups.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.Code.ToString().Contains(searchWord) || s.Company.ArName.Contains(searchWord) || s.ERPUser.Name.Contains(searchWord) || s.Notes.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = vendorsGroups.Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(vendorsGroups.ToList());
        }
        
        public ActionResult AddEdit(int? id)
        {
            var SystemPageId = db.SystemPages.Where(a => a.IsActive == true && a.IsDeleted == false && a.ControllerName == "VendorsGroup").FirstOrDefault().Id;
            if (id == null)
            {
                ViewBag.CompanyId = new SelectList(db.Companies.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
                {
                    b.Id,
                    ArName = b.FixedPart + " - " + b.ArName
                }), "Id", "ArName");
                return View();
            }
            VendorsGroup vendorGroup = db.VendorsGroups.Find(id);
            if (vendorGroup == null)
            {
                return HttpNotFound();
            }
            else
            {
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "فتح تفاصيل مجموعة الموردين",
                    EnAction = "AddEdit",
                    ControllerName = "VendorsGroup",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "GET",
                    SelectedItem = vendorGroup.Id,
                    ArItemName = vendorGroup.ArName,
                    EnItemName = vendorGroup.EnName,
                    CodeOrDocNo = vendorGroup.Code
                });
                ViewBag.CompanyId = new SelectList(db.Companies.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", vendorGroup.CompanyId);
                ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
                {
                    b.Id,
                    ArName = b.FixedPart + " - " + b.ArName
                }), "Id", "ArName",vendorGroup.FieldsCodingId);
                ViewBag.Next = QueryHelper.Next((int)id, "VendorsGroup");
                ViewBag.Previous = QueryHelper.Previous((int)id, "VendorsGroup");
                ViewBag.Last = QueryHelper.GetLast("VendorsGroup");
                ViewBag.First = QueryHelper.GetFirst("VendorsGroup");
                return View(vendorGroup);
            }
        }

        [HttpPost]
        public ActionResult AddEdit(VendorsGroup vendorGroup, string newBtn)
        {
            var SystemPageId = db.SystemPages.Where(a => a.IsActive == true && a.IsDeleted == false && a.ControllerName == "VendorsGroup").FirstOrDefault().Id;
            if (ModelState.IsValid)
            {
                vendorGroup.IsDeleted = false;
                if (vendorGroup.Id > 0)
                {
                    vendorGroup.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    db.Entry(vendorGroup).State = EntityState.Modified;


                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("VendorsGroup", "Edit", "AddEdit", vendorGroup.Id, null, " مجموعات الموردين");

                    //int pageid = db.Get_PageId("VendorsGroup").SingleOrDefault().Value;
                    //var actionId = db.PageActions.FirstOrDefault(c => c.Action == "AddEdit" && c.EnName == "Edit" && c.PageId == pageid).Id;
                    //int userid = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    //var UserName = User.Identity.Name;
                    //db.Sp_OccuredNotification(actionId, $"بتعديل بيانات في شاشة مجموعات الموردين  {UserName}قام المستخدم ");
                    ////////////////-----------------------------------------------------------------------


                }
                else
                {
                    vendorGroup.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    //vendorGroup.Code = (QueryHelper.CodeLastNum("VendorsGroup") + 1).ToString();
                    vendorGroup.Code = new JavaScriptSerializer().Serialize(SetCodeNum(vendorGroup.FieldsCodingId).Data).ToString().Trim('"');

                    vendorGroup.IsActive = true;
                    db.VendorsGroups.Add(vendorGroup);

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("VendorsGroup", "Add", "AddEdit", vendorGroup.Id, null, "مجموعات الموردين");

                    //int pageid = db.Get_PageId("VendorsGroup").SingleOrDefault().Value;
                    //var actionId = db.PageActions.FirstOrDefault(c => c.Action == "AddEdit" && c.EnName == "Add" && c.PageId == pageid).Id;
                    //int userid = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    //var UserName = User.Identity.Name;
                    //db.Sp_OccuredNotification(actionId, $"باضافة بيانات في شاشة مجموعات الموردين  {UserName}قام المستخدم  ");

                    ////////////////-----------------------------------------------------------------------


                }
                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {

                    var mod = ModelState.First(c => c.Key == "Code");  // this

                    mod.Value.Errors.Add("هذا الكود موجود من قبل");
                    ViewBag.CompanyId = new SelectList(db.Companies.Where(a => (a.IsActive == true && a.IsDeleted == false)), "Id", "ArName");
                    return View(vendorGroup);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = vendorGroup.Id > 0 ? "تعديل مجموعة موردين" : "اضافة مجموعة موردين",
                    EnAction = "AddEdit",
                    ControllerName = "VendorsGroup",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = vendorGroup.Id > 0 ? vendorGroup.Id : db.VendorsGroups.Max(i => i.Id),
                    ArItemName = vendorGroup.ArName,
                    EnItemName = vendorGroup.EnName,
                    CodeOrDocNo = vendorGroup.Code
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
            else
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();
                ViewBag.CompanyId = new SelectList(db.Companies.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
                {
                    b.Id,
                    ArName = b.FixedPart + " - " + b.ArName
                }), "Id", "ArName", vendorGroup.FieldsCodingId);
                return View(vendorGroup);
            }
        }
        [HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                VendorsGroup vendorGroup = db.VendorsGroups.Find(id);
                vendorGroup.IsDeleted = true;
                vendorGroup.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                Random random = new Random();
                const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
                vendorGroup.Code = Code;
                vendorGroup.FieldsCodingId = null;
                db.Entry(vendorGroup).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف مجموعة موردين",
                    EnAction = "AddEdit",
                    ControllerName = "VendorsGroup",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = vendorGroup.EnName,
                    ArItemName = vendorGroup.ArName,
                    CodeOrDocNo = vendorGroup.Code
                });

                ////-------------------- Notification-------------------------////
                Notification.GetNotification("VendorsGroup", "Delete", "Delete", id, null, "مجموعات الموردين");

                //int pageid = db.Get_PageId("VendorsGroup").SingleOrDefault().Value;
                //var actionId = db.PageActions.FirstOrDefault(c => c.Action == "Delete" && c.EnName == "Delete" && c.PageId == pageid).Id;
                //int userid = db.UserNotifications.FirstOrDefault(c => c.ActionId == actionId && c.PageId == pageid).UserId.Value;
                //var UserName = User.Identity.Name;
                //db.Sp_OccuredNotification(actionId, $"بحذف بيانات في شاشة مجموعات الموردين  {UserName}قام المستخدم  ");
                ////////////////-----------------------------------------------------------------------

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
                VendorsGroup vendorGroup = db.VendorsGroups.Find(id);
                if (vendorGroup.IsActive == true)
                {
                    vendorGroup.IsActive = false;
                }
                else
                {
                    vendorGroup.IsActive = true;
                }
                vendorGroup.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(vendorGroup).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)vendorGroup.IsActive ? "تنشيط مجموعة الموردين" : "إلغاء تنشيط مجموعة الموردين",
                    EnAction = "AddEdit",
                    ControllerName = "VendorsGroup",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = vendorGroup.Id,
                    EnItemName = vendorGroup.EnName,
                    ArItemName = vendorGroup.ArName,
                    CodeOrDocNo = vendorGroup.Code
                });
                ////-------------------- Notification-------------------------////
                if (vendorGroup.IsActive == true)
                {
                    Notification.GetNotification("VendorsGroup", "Activate/Deactivate", "ActivateDeactivate", id, true, "مجموعة الموردين");
                }
                else
                {

                    Notification.GetNotification("VendorsGroup", "Activate/Deactivate", "ActivateDeactivate", id, false, "مجموعة الموردين");
                }
                //int pageid = db.Get_PageId("VendorsGroup").SingleOrDefault().Value;
                //var actionId = db.PageActions.FirstOrDefault(c => c.Action == "ActivateDeactivate" && c.EnName == "Activate/Deactivate" && c.PageId == pageid).Id;
                //int userid = db.UserNotifications.FirstOrDefault(c => c.ActionId == actionId && c.PageId == pageid).UserId.Value;
                //var UserName = User.Identity.Name;
                //db.Sp_OccuredNotification(actionId, (bool)vendorGroup.IsActive ? $" تنشيط  في شاشة مجموعة الموردين{UserName}قام المستخدم  " : $"إلغاء تنشيط  في شاشة مجموعة الموردين{UserName}قام المستخدم  ");
                ////////////////-----------------------------------------------------------------------

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
                    var code = db.VendorsGroups.Where(a => a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
                    var code = db.VendorsGroups.Where(a => a.Code.Contains(fixedPart) && a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
                var code = db.VendorsGroups.Where(a => a.FieldsCodingId == null && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
            //var code = QueryHelper.CodeLastNum("VendorsGroup");
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