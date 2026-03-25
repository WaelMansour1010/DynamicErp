using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using System.Security.Claims;
using System.Globalization;
using System.Threading;
using System.Web.Script.Serialization;

namespace MyERP.Controllers
{
    public class CustomersGroupsController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: CustomersGroups
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة مجموعات العملاء",
                EnAction = "Index",
                ControllerName = "CustomersGroups",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("CustomersGroups", "View", "Index", null, null, "مجموعات العملاء");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<CustomersGroup> customersGroups;

            if (string.IsNullOrEmpty(searchWord))
            {
                customersGroups = db.CustomersGroups.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = customersGroups.Count();
            }
            else
            {
                customersGroups = db.CustomersGroups.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.Code.ToString().Contains(searchWord) || s.Company.ArName.Contains(searchWord) || s.ERPUser.Name.Contains(searchWord) || s.Notes.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = customersGroups.Count();

            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(customersGroups.ToList());
        }

        public ActionResult AddEdit(int? id )
        {
            var SystemPageId = db.SystemPages.Where(a => a.IsActive == true && a.IsDeleted == false && a.ControllerName == "CustomersGroups").FirstOrDefault().Id;
            ViewBag.Id = id;
            if (id == null)
            {
                ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
                {
                    b.Id,
                    ArName = b.FixedPart + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.CompanyId = new SelectList(db.Companies.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                return View();
            }
            CustomersGroup customergroup = db.CustomersGroups.Find(id);
            if (customergroup == null)
            {
                return HttpNotFound();
            }
            else
            {
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "فتح تفاصيل مجموعة العملاء ",
                    EnAction = "AddEdit",
                    ControllerName = "CustomersGroups",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "GET",
                    SelectedItem = customergroup.Id,
                    ArItemName = customergroup.ArName,
                    EnItemName = customergroup.EnName,
                    CodeOrDocNo = customergroup.Code
                });
                ViewBag.CompanyId = new SelectList(db.Companies.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", customergroup.CompanyId);
                ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
                {
                    b.Id,
                    ArName = b.FixedPart + " - " + b.ArName
                }), "Id", "ArName", customergroup.FieldsCodingId);
                ViewBag.Next = QueryHelper.Next((int)id, "CustomersGroup");
                ViewBag.Previous = QueryHelper.Previous((int)id, "CustomersGroup");
                ViewBag.Last = QueryHelper.GetLast("CustomersGroup");
                ViewBag.First = QueryHelper.GetFirst("CustomersGroup");
                return View(customergroup);
            }

        }

        [HttpPost]
        public ActionResult AddEdit(CustomersGroup customerGroup, string newBtn)
        {
            var SystemPageId = db.SystemPages.Where(a => a.IsActive == true && a.IsDeleted == false && a.ControllerName == "CustomersGroups").FirstOrDefault().Id;
            if (ModelState.IsValid)
            {
                var id = customerGroup.Id;
                customerGroup.IsDeleted = false;
                if (customerGroup.Id > 0)
                {
                    customerGroup.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                    db.Entry(customerGroup).State = EntityState.Modified;

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("CustomersGroups", "Edit", "AddEdit", id, null, "مجموعات العملاء");
                }
                else
                {
                    customerGroup.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    //customerGroup.Code = (QueryHelper.CodeLastNum("CustomersGroup") + 1).ToString();
                    customerGroup.Code = new JavaScriptSerializer().Serialize(SetCodeNum(customerGroup.FieldsCodingId).Data).ToString().Trim('"');
                    customerGroup.IsActive = true;
                    db.CustomersGroups.Add(customerGroup);

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("CustomersGroups", "Add", "AddEdit", id, null, "مجموعات العملاء");
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
                    return View(customerGroup);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل مجموعة عملاء" : "اضافة محموعة عملاء",
                    EnAction = "AddEdit",
                    ControllerName = "CustomersGroups",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = customerGroup.Id > 0 ? customerGroup.Id : db.CustomersGroups.Max(i => i.Id),
                    ArItemName = customerGroup.ArName,
                    EnItemName = customerGroup.EnName,
                    CodeOrDocNo = customerGroup.Code
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
                }), "Id", "ArName", customerGroup.FieldsCodingId);
                return View(customerGroup);
            }
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                CustomersGroup customerGroup = db.CustomersGroups.Find(id);
                customerGroup.IsDeleted = true;
                customerGroup.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                Random random = new Random();
                const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
                customerGroup.Code = Code;
                customerGroup.FieldsCodingId = null;
                db.Entry(customerGroup).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف مجموعة عملاء",
                    EnAction = "AddEdit",
                    ControllerName = "CustomersGroups",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = customerGroup.EnName,
                    ArItemName = customerGroup.ArName,
                    CodeOrDocNo = customerGroup.Code
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("CustomersGroups", "Delete", "Delete", id, null, "مجموعات العملاء");

                //int pageid = db.Get_PageId("CustomersGroups").SingleOrDefault().Value;
                //var actionId = db.PageActions.FirstOrDefault(c => c.Action == "Delete" && c.EnName == "Delete" && c.PageId == pageid).Id;
                //int userid = db.UserNotifications.FirstOrDefault(c => c.ActionId == actionId && c.PageId == pageid).UserId.Value;
                //var UserName = User.Identity.Name;
                //db.Sp_OccuredNotification(actionId, $"بحذف بيانات في شاشة مجموعات العملاء  {UserName}قام المستخدم  ");
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
                CustomersGroup customerGroup = db.CustomersGroups.Find(id);
                if (customerGroup.IsActive == true)
                {
                    customerGroup.IsActive = false;
                }
                else
                {
                    customerGroup.IsActive = true;
                }
                customerGroup.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(customerGroup).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)customerGroup.IsActive ? "تنشيط مجموعة عملاء" : "إلغاء تنشيط مجموعة عملاء",
                    EnAction = "AddEdit",
                    ControllerName = "CustomersGroups",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = customerGroup.Id,
                    EnItemName = customerGroup.EnName,
                    ArItemName = customerGroup.ArName,
                    CodeOrDocNo = customerGroup.Code
                });
                ////-------------------- Notification-------------------------////
                if (customerGroup.IsActive == true)
                {
                    Notification.GetNotification("CustomersGroups", "Activate/Deactivate", "ActivateDeactivate", id, true, "مجموعة عملاء");
                }
                else
                {

                    Notification.GetNotification("CustomersGroups", "Activate/Deactivate", "ActivateDeactivate", id, false, "مجموعة عملاء");
                }
                //int pageid = db.Get_PageId("CustomersGroups").SingleOrDefault().Value;
                //var actionId = db.PageActions.FirstOrDefault(c => c.Action == "ActivateDeactivate" && c.EnName == "Activate/Deactivate" && c.PageId == pageid).Id;
                //int userid = db.UserNotifications.FirstOrDefault(c => c.ActionId == actionId && c.PageId == pageid).UserId.Value;
                //var UserName = User.Identity.Name;
                //db.Sp_OccuredNotification(actionId, (bool)customerGroup.IsActive ? $" تنشيط  في شاشة مجموعة عملاء{UserName}قام المستخدم  " : $"إلغاء تنشيط  في شاشة مجموعة عملاء{UserName}قام المستخدم  ");
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
                    var code = db.CustomersGroups.Where(a => a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
                    var code = db.CustomersGroups.Where(a => a.Code.Contains(fixedPart) && a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
                    if (code.FirstOrDefault() == null)
                    {
                        result = 0 + 1;
                        if (IsZerosFills == true)
                        {
                            if (result.ToString().Length < noOfDigits)
                            {
                                CodeNo = QueryHelper.FillsWithZeros(noOfDigits/* - fixedPart.Length*/, result.ToString());
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
                                CodeNo = QueryHelper.FillsWithZeros(noOfDigits/* - fixedPart.Length*/, result.ToString());
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
                var code = db.CustomersGroups.Where(a => a.FieldsCodingId == null && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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

            //var code = QueryHelper.CodeLastNum("CustomersGroup");
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
