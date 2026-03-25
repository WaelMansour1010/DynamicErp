using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;

namespace MyERP.Controllers
{
    public class OrderRequestController : Controller
    {
        // GET: OrderRequest
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: UserPrivilege/Edit/5
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمةالطلبات المحتملة ",
                EnAction = "Index",
                ControllerName = "OrderRequest",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("OrderRequest", "View", "Index", null, null, "الطلبات المحتملة");
            ViewBag.EmployeeId = new SelectList(db.Employees.Where(e => e.IsActive == true && e.IsDeleted == false), "Id", "ArName");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<OrderRequest> orderRequests;

            if (string.IsNullOrEmpty(searchWord))
            {
                orderRequests = db.OrderRequests.Where(s => s.IsDeleted == false&&s.IsActive==true).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.OrderRequests.Where(s => s.IsDeleted == false && s.IsActive == true).Count();
            }
            else
            {
                orderRequests = db.OrderRequests.Where(s => s.IsDeleted == false && s.IsActive == true && (s.OrderNumber.Contains(searchWord) || s.CustomerName.Contains(searchWord)|| s.Status.ToString().Contains(searchWord) || s.CustomerAddress.Contains(searchWord) || s.CustomerPhone.Contains(searchWord) || s.OrderNumber.Contains(searchWord) || s.Service.Contains(searchWord) || s.Notes.Contains(searchWord))).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = orderRequests.Count();
            }
            ViewBag.searchWord = searchWord;

            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(orderRequests.ToList());
        }
  

        [SkipERPAuthorize]
        public JsonResult SetOrderNum()
        {
            var orderNo = QueryHelper.OrderLastNum("OrderRequest");
            return Json(orderNo + 1, JsonRequestBehavior.AllowGet);
        }

        //AddEdit
        public ActionResult AddEdit(int? id)
        {
            ViewBag.Id = id;
            if (id == null)
            {
                OrderRequest Newobj = new OrderRequest();
                ViewBag.EmployeeId = new SelectList(db.Employees.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
             
                return View(Newobj);
            }
            OrderRequest OrderRequest = db.OrderRequests.Find(id);
            if (OrderRequest == null)
            {
                return HttpNotFound();
            }
            else
            {
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "فتح تفاصيل العميل ",
                    EnAction = "AddEdit",
                    ControllerName = "OrderRequest",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "GET",
                    SelectedItem = OrderRequest.Id,
                  
                    CodeOrDocNo = OrderRequest.OrderNumber
                });
                ViewBag.EmployeeId = new SelectList(db.Employees.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName",OrderRequest.EmployeeId);
                ViewBag.Next = QueryHelper.Next((int)id, "OrderRequest");
                ViewBag.Previous = QueryHelper.Previous((int)id, "OrderRequest");
                ViewBag.Last = QueryHelper.GetLast("OrderRequest");
                ViewBag.First = QueryHelper.GetFirst("OrderRequest");

            
                return View(OrderRequest);
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddEdit(OrderRequest OrderRequest, string newBtn)
        {
           
            
            if (ModelState.IsValid)
            {
                var id = OrderRequest.Id;
                OrderRequest.IsDeleted = false;
                OrderRequest.IsActive = true;
                if (OrderRequest.Status==null)
                {
                    OrderRequest.Status = false;

                }
                if (OrderRequest.Id > 0)
                {

               

                    db.Entry(OrderRequest).State = EntityState.Modified;

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("OrderRequest", "Edit", "AddEdit", id, null, "الطلبات المحتملة");

                }
                else
                {

                    //Checking file is available to save.  

                    OrderRequest.OrderNumber = (QueryHelper.OrderLastNum("OrderRequest") + 1).ToString();
                    db.OrderRequests.Add(OrderRequest);
                    //-------------------- Notification-------------------------////
                    Notification.GetNotification("OrderRequest", "Add", "AddEdit", id, null, "الطلبات المحتملة");

                }
                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {

                    return View(OrderRequest);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل بيانات الطلبات المحتملة" : "اضافة الطلبات المحتملة",
                    EnAction = "AddEdit",
                    ControllerName = "OrderRequest",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = OrderRequest.Id > 0 ? OrderRequest.Id : db.OrderRequests.Max(i => i.Id),
                   
                    CodeOrDocNo = OrderRequest.OrderNumber
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


           
                //ViewBag.IdentityTypeId = new SelectList(db.IdentityTypes.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                //{
                //    Id = b.Id,
                //    ArName = b.Code + " - " + b.ArName
                //}), "Id", "ArName", customer.IdentityTypeId);

                return View(OrderRequest);
            }
        }

        [HttpPost]
        public ActionResult AssignEmployee(int[] ids, int empId)
        {
            var orderRequests= db.OrderRequests.Where(o => ids.Contains(o.Id));
            foreach (var item in orderRequests)
            {
                item.EmployeeId = empId;
                db.Entry(item).State = EntityState.Modified;
            }
            db.SaveChanges();
            return Content("true");
        }
        [HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
            
                OrderRequest OrderRequest = db.OrderRequests.Find(id);

                OrderRequest.IsDeleted = true;
                OrderRequest.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(OrderRequest).State = EntityState.Modified;


                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف الطلبات المحتملة",
                    EnAction = "AddEdit",
                    ControllerName = "OrderRequest",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                  
                    CodeOrDocNo = OrderRequest.OrderNumber
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("OrderRequest", "Delete", "Delete", id, null, "الطلبات المحتملة");
                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

        }

        //[HttpPost]
        //public ActionResult ActivateDeactivate(int id)
        //{
        //    try
        //    {
        //        OrderRequest OrderRequest = db.OrderRequests.Find(id);
        //        if (OrderRequest.IsActive == true)
        //        {
        //            OrderRequest.IsActive = false;
        //        }
        //        else
        //        {
        //            OrderRequest.IsActive = true;
        //        }

        //        db.Entry(OrderRequest).State = EntityState.Modified;

        //        db.SaveChanges();
        //        QueryHelper.AddLog(new MyLog()
        //        {
        //            ArAction = (bool)OrderRequest.IsActive ? "تنشيط الطلبات المحتملة" : "إلغاء تنشيط الطلبات المحتملة",
        //            EnAction = "AddEdit",
        //            ControllerName = "OrderRequest",
        //            UserName = User.Identity.Name,
        //            UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
        //            LogDate = DateTime.Now,
        //            RequestMethod = "POST",
        //            SelectedItem = OrderRequest.Id,
                  
        //            CodeOrDocNo = OrderRequest.OrderNumber
        //        });

        //        ////-------------------- Notification-------------------------////
        //        if (OrderRequest.IsActive == true)
        //        {
        //            Notification.GetNotification("OrderRequest", "Activate/Deactivate", "ActivateDeactivate", id, true, "الطلبات المحتملة");
        //        }
        //        else
        //        {

        //            Notification.GetNotification("OrderRequest", "Activate/Deactivate", "ActivateDeactivate", id, false, "الطلبات المحتملة");
        //        }

        //        return Content("true");
        //    }
        //    catch (Exception ex)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }
        //}

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