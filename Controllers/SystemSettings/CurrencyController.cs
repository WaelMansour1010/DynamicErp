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

namespace MyERP.Controllers.SystemSettings
{
    
    [ERPAuthorize]
    public class CurrencyController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            //////////////// LOG
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمةالعملات",
                EnAction = "Index",
                ControllerName = "Currency",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("Currency", "View", "Index",null, null, "العملات");
            //////////////////-----------------------------------------------------------------------
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            
            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            /////////////////////////// Search ////////////////////

            IQueryable<Currency> currency;
            if (string.IsNullOrEmpty(searchWord))
            {
                currency = db.Currencies.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Currencies.Where(s => s.IsDeleted == false).Count();


            }
            else
            {
                currency = db.Currencies.Where(s => s.IsDeleted == false &&( s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) ||
                                           s.EnName.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = currency.Count();

                }

                ViewBag.searchWord = searchWord;
                ViewBag.wantedRowsNo = wantedRowsNo;
            ///////////////////////////////////////////////////////////////////////////////
                return View(currency.ToList());
            }



        public ActionResult AddEdit(int? id)
        {

            if (id == null)
            {
                Currency NewObj = new Currency();
               
                return View(NewObj);
            }
            Currency currency = db.Currencies.Find(id);
            if (currency == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل العملة ",
                EnAction = "AddEdit",
                ControllerName = "Currency",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = currency.Id,
                ArItemName = currency.ArName,
                EnItemName = currency.EnName,
                CodeOrDocNo = currency.Code
            });
            ViewBag.Next = QueryHelper.Next((int)id, "Currency");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Currency");
            ViewBag.Last = QueryHelper.GetLast("Currency");
            ViewBag.First = QueryHelper.GetFirst("Currency");
            return View(currency);
        }


        [HttpPost]
        
        public ActionResult AddEdit(Currency currency, string newBtn)
        {
            if (ModelState.IsValid)
            {
                var id = currency.Id;
                if (currency.IsDefault == null)
                {
                    currency.IsDefault = false;
                }
                else
                {
                    QueryHelper.RemoveDefaultCurrency("Currency");
                }
                currency.IsDeleted = false;
                if (currency.Id > 0)
                {
                    currency.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    db.Entry(currency).State = EntityState.Modified;

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("Currency", "Edit", "AddEdit", id, null, "العملات");
                    //////////////////-----------------------------------------------------------------------
                }
                else
                {
                    currency.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    currency.IsActive = true;
                    //currency.Code= (QueryHelper.CodeLastNum("Currency") + 1).ToString();
                    db.Currencies.Add(currency);

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("Currency", "Add", "AddEdit", currency.Id, null, "العملات");

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
                    return View(currency);
                }
                    QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل العملة" : "اضافة العملة",
                    EnAction = "AddEdit",
                    ControllerName = "Currency",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = currency.Id,
                    ArItemName = currency.ArName,
                    EnItemName = currency.EnName,
                    CodeOrDocNo = currency.Code
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
            

            return View(currency);
        }


        [HttpPost, ActionName("Delete")]
        
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                Currency currency = db.Currencies.Find(id);
                currency.IsDeleted = true;
                currency.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(currency).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف العملة",
                    EnAction = "AddEdit",
                    ControllerName = "Currency",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = currency.EnName,
                    ArItemName = currency.ArName,
                    CodeOrDocNo = currency.Code
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("Currency", "Delete", "Delete", id, null, "العملات");

                //////////////////-----------------------------------------------------------------------


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
                Currency currency = db.Currencies.Find(id);
                if (currency.IsActive == true)
                {
                    currency.IsActive = false;
                }
                else
                {
                    currency.IsActive = true;
                }
                currency.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(currency).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)currency.IsActive ? "تنشيط العملة" : "إلغاء تنشيط العملة",
                    EnAction = "AddEdit",
                    ControllerName = "Currency",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = currency.Id,
                    EnItemName = currency.EnName,
                    ArItemName = currency.ArName,
                    CodeOrDocNo = currency.Code
                });

                if (currency.IsActive == true)
                {
                    Notification.GetNotification("Currency", "Activate/Deactivate", "ActivateDeactivate", id, true, "العملات");
                }
                else
                {

                    Notification.GetNotification("Currency", "Activate/Deactivate", "ActivateDeactivate", id, false, "العملات");
                }
                
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
