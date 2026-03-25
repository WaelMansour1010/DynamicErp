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
    public class PaymentMethodsController : Controller
    {
        // GET: PaymentMethods
        private MySoftERPEntity db = new MySoftERPEntity();

        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة طرق الدفع ",
                EnAction = "Index",
                ControllerName = "PaymentMethods",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("PaymentMethods", "View", "Index", null, null, "طرق الدفع");
            //////-----------------------------------------------------------------------

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            /////////////////////////// Search ////////////////////

            IQueryable<PaymentMethod> paymentMethods;

            if (string.IsNullOrEmpty(searchWord))
            {
                paymentMethods = db.PaymentMethods.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);

                ViewBag.Count = db.PaymentMethods.Where(c => c.IsDeleted == false).Count();
            }
            else
            {
                paymentMethods = db.PaymentMethods.Where(s => s.IsDeleted == false && (s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = paymentMethods.Count();

                ///////////////////////////////////////////////////////////////////////////////

            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(paymentMethods.ToList());


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
                    var code = db.PaymentMethods.Where(a => a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
                    var code = db.PaymentMethods.Where(a => a.Code.Contains(fixedPart) && a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
                var code = db.PaymentMethods.Where(a => a.FieldsCodingId == null && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
            //var code = QueryHelper.CodeLastNum("PaymentMethod");
            //return Json(code + 1, JsonRequestBehavior.AllowGet);
        }


        public ActionResult AddEdit(int? id)
        {
            var SystemPageId = db.SystemPages.Where(a => a.IsActive == true && a.IsDeleted == false && a.ControllerName == "PaymentMethods").FirstOrDefault().Id;
            if (id == null)
            {
                ViewBag.AccountId = new SelectList(db.ChartOfAccounts.Where(c => c.ChartOfAccountsClassification.Id == 3 && c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.CommissionAccountId = new SelectList(db.ChartOfAccounts.Where(c => c.ChartOfAccountsClassification.Id == 3 && c.IsDeleted == false && c.IsActive == true).Select(b => new
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
            PaymentMethod paymentMethod = db.PaymentMethods.Find(id);
            if (paymentMethod == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل طريقة الدفع ",
                EnAction = "AddEdit",
                ControllerName = "PaymentMethods",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = paymentMethod.Id,
                ArItemName = paymentMethod.ArName,
                EnItemName = paymentMethod.EnName,
 
            });
            ViewBag.AccountId = new SelectList(db.ChartOfAccounts.Where(c => c.ChartOfAccountsClassification.Id == 3 && c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName",paymentMethod.AccountId);
            ViewBag.CommissionAccountId = new SelectList(db.ChartOfAccounts.Where(c => c.ChartOfAccountsClassification.Id == 3 && c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", paymentMethod.CommissionAccountId);
            ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
            {
                b.Id,
                ArName = b.FixedPart + " - " + b.ArName
            }), "Id", "ArName", paymentMethod.FieldsCodingId);
            ViewBag.Next = QueryHelper.Next((int)id, "PaymentMethod");
            ViewBag.Previous = QueryHelper.Previous((int)id, "PaymentMethod");
            ViewBag.Last = QueryHelper.GetLast("PaymentMethod");
            ViewBag.First = QueryHelper.GetFirst("PaymentMethod");
            return View(paymentMethod);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddEdit(PaymentMethod paymentMethod, string newBtn)
        {
            var SystemPageId = db.SystemPages.Where(a => a.IsActive == true && a.IsDeleted == false && a.ControllerName == "PaymentMethods").FirstOrDefault().Id;
            if (ModelState.IsValid)
            {
                var id = paymentMethod.Id;
                paymentMethod.IsDeleted = false;
                
                if (paymentMethod.Id > 0)
                {
                    //paymentMethod.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                    db.Entry(paymentMethod).State = EntityState.Modified;
                    db.SaveChanges();

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("PaymentMethods", "Edit", "AddEdit", id, null, "طرق الدفع");

                    /////////-----------------------------------------------------------------------

                    // Add DB Change
                    QueryHelper.AddDBChange(new DBChange()
                    {
                        TableName = "PaymentMethod",
                        SelectedId = paymentMethod.Id,
                        IsMasterChange = true,
                        IsNew = false,
                        IsTransaction = false
                    });

                }
                else
                {
                   // paymentMethod.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    paymentMethod.IsActive = true;
                    paymentMethod.ForPos = true;
                    paymentMethod.Code = new JavaScriptSerializer().Serialize(SetCodeNum(paymentMethod.FieldsCodingId).Data).ToString().Trim('"');
                    db.PaymentMethods.Add(paymentMethod);
                    db.SaveChanges();
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("PaymentMethods", "Add", "AddEdit", paymentMethod.Id, null, "طرق الدفع");

                    ///////////-----------------------------------------------------------------------

                    // Add DB Change
                    var SelectedId = db.PaymentMethods.OrderByDescending(r => r.Id).FirstOrDefault().Id;
                    QueryHelper.AddDBChange(new DBChange()
                    {
                        TableName = "PaymentMethod",
                        SelectedId = SelectedId,
                        IsMasterChange = true,
                        IsNew = true,
                        IsTransaction = false
                    });

                }

                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {

                    var mod = ModelState.First(c => c.Key == "Code");  // this

                    mod.Value.Errors.Add("هذا الكود موجود من قبل");
                    return View(paymentMethod);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل طريقة دفع" : "اضافة طريقة دفع",
                    EnAction = "AddEdit",
                    ControllerName = "ItemGroups",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = paymentMethod.Id,
                    ArItemName = paymentMethod.ArName,
                    EnItemName = paymentMethod.EnName,
                    
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



            ViewBag.AccountId = new SelectList(db.ChartOfAccounts.Where(c => c.ChartOfAccountsClassification.Id == 3 && c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", paymentMethod.AccountId);
            ViewBag.CommissionAccountId = new SelectList(db.ChartOfAccounts.Where(c => c.ChartOfAccountsClassification.Id == 3 && c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", paymentMethod.CommissionAccountId);
            ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
            {
                b.Id,
                ArName = b.FixedPart + " - " + b.ArName
            }), "Id", "ArName", paymentMethod.FieldsCodingId);
            return View(paymentMethod);
        }


        [HttpPost, ActionName("Delete")]

        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                PaymentMethod paymentMethod  = db.PaymentMethods.Find(id);
                paymentMethod.IsDeleted = true;
                //paymentMethod.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                Random random = new Random();
                const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
                paymentMethod.Code = Code;
                paymentMethod.FieldsCodingId = null;
                db.Entry(paymentMethod).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف طريقة الدفع",
                    EnAction = "AddEdit",
                    ControllerName = "PaymentMethods",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = paymentMethod.EnName,
                    ArItemName = paymentMethod.ArName,
                    
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("PaymentMethods", "Delete", "Delete", id, null, "طرق دفع نقاط البيع");

                // Add DB Change
                QueryHelper.AddDBChange(new DBChange()
                {
                    TableName = "PaymentMethod",
                    SelectedId = paymentMethod.Id,
                    IsMasterChange = true,
                    IsNew = false,
                    IsTransaction = false
                });

                return Content("true");
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        public ActionResult ActivateDeactivate(int id)
        {
            try
            {
                PaymentMethod paymentMethod = db.PaymentMethods.Find(id);
                if (paymentMethod.IsActive == true)
                {
                    paymentMethod.IsActive = false;
                }
                else
                {
                    paymentMethod.IsActive = true;
                }
                //itemGroup.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(paymentMethod).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)paymentMethod.IsActive ? "تنشيط طريقة الدفع" : "إلغاء تنشيط طريقة الدفع",
                    EnAction = "AddEdit",
                    ControllerName = "PaymentMethods",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = paymentMethod.Id,
                    EnItemName = paymentMethod.EnName,
                    ArItemName = paymentMethod.ArName,
                    
                });
                ////-------------------- Notification-------------------------////
                if (paymentMethod.IsActive == true)
                {
                    Notification.GetNotification("PaymentMethods", "Activate/Deactivate", "ActivateDeactivate", id, true, "طرق الدفع");
                }
                else
                {

                    Notification.GetNotification("PaymentMethods", "Activate/Deactivate", "ActivateDeactivate", id, false, "طرق الدفع");
                }

                // Add DB Change
                QueryHelper.AddDBChange(new DBChange()
                {
                    TableName = "PaymentMethod",
                    SelectedId = paymentMethod.Id,
                    IsMasterChange = true,
                    IsNew = false,
                    IsTransaction = false
                });
                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
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