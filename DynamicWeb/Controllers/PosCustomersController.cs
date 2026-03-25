using MyERP.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace MyERP.Controllers
{
    public class PosCustomersController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: PosCustomers
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح عملاء نقاط البيع",
                EnAction = "Index",
                ControllerName = "PosCustomers",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });

            Notification.GetNotification("PosCustomers", "View", "Index", null, null, "عملاء نقاط البيع");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }

            IQueryable<PosCustomer> posCustomers;
            if (string.IsNullOrEmpty(searchWord))
            {
                posCustomers = db.PosCustomers.Where(a => a.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.PosCustomers.Where(a => a.IsDeleted == false).Count();
            }
            else
            {
                posCustomers = db.PosCustomers.Where(a => a.IsDeleted == false &&
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.PosCustomers.Where(a => a.IsDeleted == false && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;

            return View(posCustomers.ToList());
        }

        [HttpGet]
        public ActionResult AddEdit(int? id)
        {
            var SystemPageId = db.SystemPages.Where(a => a.IsActive == true && a.IsDeleted == false && a.ControllerName == "PosCustomers").FirstOrDefault().Id;

            if (id == null)
            {
                ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
                {
                    b.Id,
                    ArName = b.FixedPart + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.CustomerId = new SelectList(db.Customers.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                return View();
            }

            PosCustomer posCustomer = db.PosCustomers.Find(id);
            if (posCustomer == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل عملاء نقاط البيع ",
                EnAction = "AddEdit",
                ControllerName = "PosCustomers",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });

            ViewBag.Next = QueryHelper.Next((int)id, "PosCustomer");
            ViewBag.Previous = QueryHelper.Previous((int)id, "PosCustomer");
            ViewBag.Last = QueryHelper.GetLast("PosCustomer");
            ViewBag.First = QueryHelper.GetFirst("PosCustomer");
            ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
            {
                b.Id,
                ArName = b.FixedPart + " - " + b.ArName
            }), "Id", "ArName",posCustomer.FieldsCodingId);
            ViewBag.GovernorateId = posCustomer.GovernorateId;
            ViewBag.CityId = posCustomer.CityId;
            ViewBag.AreaId = posCustomer.AreaId;
            ViewBag.CustomerId = new SelectList(db.Customers.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", posCustomer.CustomerId);
            /*//CityId
            ViewBag.CityId = new SelectList(db.Cities.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", area.CityId);*/
            return View(posCustomer);
        }

        [SkipERPAuthorize]
        [HttpPost]
        public ActionResult AddEdit(PosCustomer posCustomer, string newBtn)
        {
            var SystemPageId = db.SystemPages.Where(a => a.IsActive == true && a.IsDeleted == false && a.ControllerName == "PosCustomers").FirstOrDefault().Id;
            if (ModelState.IsValid)
            {
                var id = posCustomer.Id;
                posCustomer.IsDeleted = false;
                if (posCustomer.Id > 0)
                {
                    //posCustomer.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    db.Entry(posCustomer).State = EntityState.Modified;
                    Notification.GetNotification("PosCustomers", "Edit", "AddEdit", posCustomer.Id, null, "عملاء نقاط البيع");
                }
                else
                {
                    //cRM_TimeSetting.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    posCustomer.Code = new JavaScriptSerializer().Serialize(SetCodeNum(posCustomer.FieldsCodingId).Data).ToString().Trim('"');
                    //posCustomer.Code = (QueryHelper.CodeLastNum("PosCustomer") + 1).ToString();
                    posCustomer.IsActive = true;
                    db.PosCustomers.Add(posCustomer);

                    Notification.GetNotification("PosCustomers", "Add", "AddEdit", posCustomer.Id, null, "عملاء نقاط البيع");
                }

                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    var mod = ModelState.First(c => c.Key == "Code");  // this
                    mod.Value.Errors.Add("هذا الكود موجود من قبل");
                    return View(posCustomer);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل عملاء نقاط البيع" : "اضافة عملاء نقاط البيع",
                    EnAction = "AddEdit",
                    ControllerName = "PosCustomers",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = posCustomer.Code
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
            }), "Id", "ArName", posCustomer.FieldsCodingId);
            ViewBag.CustomerId = new SelectList(db.Customers.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", posCustomer.CustomerId);
            return View(posCustomer);
        }

        [SkipERPAuthorize]
        [HttpPost]
        public ActionResult AddUpdate(PosCustomer posCustomer)
        {
           // if (ModelState.IsValid)
            {
                var id = posCustomer.Id;
                posCustomer.IsDeleted = false;
                if (posCustomer.Id > 0)
                {
                    //posCustomer.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    posCustomer.IsActive = true;
                    posCustomer.IsDeleted = false;

                    db.Entry(posCustomer).State = EntityState.Modified;
                    Notification.GetNotification("PosCustomers", "Edit", "AddEdit", posCustomer.Id, null, "عملاء نقاط البيع");
                }
                else
                {
                    //cRM_TimeSetting.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    //posCustomer.Code = (QueryHelper.CodeLastNum("PosCustomer") + 1).ToString();
                    posCustomer.Code = new JavaScriptSerializer().Serialize(SetCodeNum(posCustomer.FieldsCodingId).Data).ToString().Trim('"');
                    posCustomer.IsActive = true;
                    db.PosCustomers.Add(posCustomer);

                    Notification.GetNotification("PosCustomers", "Add", "AddEdit", posCustomer.Id, null, "عملاء نقاط البيع");
                }

                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل عملاء نقاط البيع" : "اضافة عملاء نقاط البيع",
                    EnAction = "AddEdit",
                    ControllerName = "PosCustomers",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = posCustomer.Code
                });
                
            }

            return Json(posCustomer,JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                PosCustomer posCustomer = db.PosCustomers.Find(id);
                posCustomer.IsDeleted = true;
                //posCustomer.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                Random random = new Random();
                const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
                posCustomer.Code = Code;
                posCustomer.FieldsCodingId = null;
                db.Entry(posCustomer).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف عميل نقطة بيع",
                    EnAction = "AddEdit",
                    ControllerName = "PosCustomers",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = posCustomer.EnName

                });
                Notification.GetNotification("PosCustomers", "Delete", "Delete", id, null, "عملاء نقاط البيع");


                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

        }

        [SkipERPAuthorize]
        [HttpPost]
        public ActionResult ActivateDeactivate(int id)
        {
            try
            {
                PosCustomer posCustomer = db.PosCustomers.Find(id);
                if (posCustomer.IsActive == true)
                {
                    posCustomer.IsActive = false;
                }
                else
                {
                    posCustomer.IsActive = true;
                }
                //cRM_TimeSetting.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(posCustomer).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)posCustomer.IsActive ? "تنشيط عملاء نقاط البيع" : "إلغاء تنشيط عملاء نقاط البيع",
                    EnAction = "AddEdit",
                    ControllerName = "PosCustomers",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = posCustomer.Id,
                    EnItemName = posCustomer.EnName,
                    ArItemName = posCustomer.ArName,
                    CodeOrDocNo = posCustomer.Code
                });
                if (posCustomer.IsActive == true)
                {
                    Notification.GetNotification("PosCustomers", "Activate/Deactivate", "ActivateDeactivate", id, true, "عملاء نقاط البيع");
                }
                else
                {

                    Notification.GetNotification("PosCustomers", "Activate/Deactivate", "ActivateDeactivate", id, false, " عملاء نقاط البيع");
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
                    var code = db.PosCustomers.Where(a => a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
                    var code = db.PosCustomers.Where(a => a.Code.Contains(fixedPart) && a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
                var code = db.PosCustomers.Where(a => a.FieldsCodingId == null && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
            //var code = QueryHelper.CodeLastNum("PosCustomer");
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