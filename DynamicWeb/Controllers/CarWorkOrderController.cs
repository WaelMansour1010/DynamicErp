using MyERP.Models;
using MyERP.Repository;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Controllers
{
    public class CarWorkOrderController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: CarWorkOrder
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "خدمات السيارات - فتح قائمة أوامر الشغل",
                EnAction = "Index",
                ControllerName = "CarWorkOrder",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("CarWorkOrder", "View", "Index", null, null, "خدمات السيارات - أوامر الشغل");
            //////////-----------------------------------------------------------------------
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<CarWorkOrder> carWorkOrder;

            if (string.IsNullOrEmpty(searchWord))
            {
                carWorkOrder = db.CarWorkOrders.Where(s => s.IsDeleted == false).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.CarWorkOrders.Where(s => s.IsDeleted == false).Count();

            }
            else
            {
                carWorkOrder = db.CarWorkOrders.Where(s => s.IsDeleted == false && (s.DocumentNumber.Contains(searchWord) || s.Customer.ArName.Contains(searchWord) || s.CarModel.ArName.Contains(searchWord) || s.VoucherDate.ToString().Contains(searchWord) || s.Notes.Contains(searchWord))).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.CarWorkOrders.Where(s => s.IsDeleted == false && (s.DocumentNumber.Contains(searchWord) || s.Customer.ArName.Contains(searchWord) || s.CarModel.ArName.Contains(searchWord) || s.VoucherDate.ToString().Contains(searchWord) || s.Notes.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(carWorkOrder.ToList());
        }

        // GET: CarWorkOrder/Edit/5
        public async Task<ActionResult> AddEdit(int? id)
        {
            SystemSetting systemSetting = await db.SystemSettings.AnyAsync() ? await db.SystemSettings.FirstOrDefaultAsync() : new SystemSetting();
            ViewBag.ApplyTaxes = systemSetting.ApplyTaxesOnSalesIvoiceAuto;
            UserRepository userRepository = new UserRepository(db);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            if (id == null)
            {
                ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", systemSetting.DefaultDepartmentId);

                ViewBag.CustomerId = new SelectList(db.Customers.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.CarTypeId = new SelectList(db.CarTypes.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.CarModelId = new SelectList(db.CarModels.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.CarColorId = new SelectList(db.CarColors.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.CarServiceCategoryId = new SelectList(db.CarServiceCategories.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");


                //------ Time Zone Depends On Currency --------//
                var Currency = db.Currencies.Where(a => a.IsActive == true && a.IsDeleted == false && a.IsDefault == true).FirstOrDefault();
                var CurrencyCode = Currency != null ? Currency.Code : "";
                TimeZoneInfo info;
                if (CurrencyCode == "SAR")
                {
                    //info = TimeZoneInfo.FindSystemTimeZoneById("Arabian Standard Time");//+2H from Egypt Standard Time
                    info = TimeZoneInfo.FindSystemTimeZoneById("Arab Standard Time");// +1H from Egypt Standard Time
                }
                else
                {
                    info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
                }
                DateTime utcNow = DateTime.UtcNow;
                DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
                //----------------- End of Time Zone Depends On Currency --------------------//
                ViewBag.VoucherDate = cTime.ToString("yyyy-MM-ddTHH:mm");

                return View();
            }

            CarWorkOrder carWorkOrder = await db.CarWorkOrders.FindAsync(id);
            if (carWorkOrder == null)
                return HttpNotFound();

            ViewBag.CarEntranceId = carWorkOrder.SelectedId;
            ViewBag.HasCarSalesInvoice = db.CarSalesInvoices.Where(a => a.SelectedId == carWorkOrder.SelectedId && a.IsActive == true && a.IsDeleted == false).Count();

            ViewBag.Next = QueryHelper.Next((int)id, "CarWorkOrder");
            ViewBag.Previous = QueryHelper.Previous((int)id, "CarWorkOrder");
            ViewBag.Last = QueryHelper.GetLast("CarWorkOrder");
            ViewBag.First = QueryHelper.GetFirst("CarWorkOrder");

            int sysPageId = QueryHelper.SourcePageId("CarWorkOrder");
            //JournalEntry journal = db.JournalEntries.FirstOrDefault(j => j.SourceId == id && j.SourcePageId == sysPageId);
            //ViewBag.Journal = journal;
            var SourceDocumentNumber = db.CarEntrances.Where(a => a.Id == carWorkOrder.SelectedId).Select(a => a.DocumentNumber).FirstOrDefault();
            ViewBag.SourceDocumentNumber = SourceDocumentNumber != null ? SourceDocumentNumber.Replace("-", "") : "";

            ViewBag.CustomerId = new SelectList(db.Customers.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", carWorkOrder.CustomerId);

            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", carWorkOrder.DepartmentId);

            ViewBag.CarTypeId = new SelectList(db.CarTypes.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", carWorkOrder.CarTypeId);

            ViewBag.CarModelId = new SelectList(db.CarModels.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", carWorkOrder.CarModelId);

            ViewBag.CarColorId = new SelectList(db.CarColors.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", carWorkOrder.CarColorId);

            ViewBag.CarServiceCategoryId = new SelectList(db.CarServiceCategories.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", carWorkOrder.CarServiceCategoryId);

            ViewBag.CarServiceId = new SelectList(db.CarServices.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");

            

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "خدمات السيارات - فتح تفاصيل أمر شغل",
                EnAction = "AddEdit",
                ControllerName = "CarWorkOrder",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = carWorkOrder.Id,
                CodeOrDocNo = carWorkOrder.DocumentNumber
            });


            try
            {
                ViewBag.VoucherDate = carWorkOrder.VoucherDate.Value.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.OutDate = carWorkOrder.OutDate.Value.ToString("yyyy-MM-ddTHH:mm");
            }
            catch (Exception)
            {
            }

            return View(carWorkOrder);
        }


        [HttpPost]
        public JsonResult Add(int? CarEntranceId)
        {
            CarEntrance carEntrance = db.CarEntrances.Find(CarEntranceId);
            int userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            if (ModelState.IsValid)
            {

                int SystemPageId = db.SystemPages.Where(s => s.TableName == "CarEntrance").Select(s => s.Id).FirstOrDefault();
                //if (carSalesInvoice.SelectedId != null)
                //{
                //    DateTime utcNow = DateTime.UtcNow;
                //    TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
                //    DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
                //    carSalesInvoice.VoucherDate = cTime;
                //}

                MyXML.xPathName = "Details";
                //var CarSalesInvoiceDetails = MyXML.GetXML(carSalesInvoice.CarSalesInvoiceDetails);
                var CarWorkOrderDetails = MyXML.GetXML(carEntrance.CarEntranceDetails.Select(x => new { x.CarServiceId, x.ServiceDiscountPercentage, x.ServiceDiscountValue, x.Price, x.MainDocId, x.ServiceNetTotal, SelectedId = CarEntranceId, x.ServiceTypeId, SystemPageId, x.CurrencyId,x.CurrencyEquivalent, x.IsDeleted }));

                var idResult = new ObjectParameter("Id", typeof(Int32));
                db.CarWorkOrder_Insert(idResult,
                    carEntrance.DepartmentId,
                    carEntrance.VoucherDate,
                    carEntrance.CarPlateNumber,
                    carEntrance.CarSerialNumber,
                    carEntrance.CarTypeId,
                    carEntrance.CarColorId,
                    carEntrance.CarModelId,
                    carEntrance.CarServiceCategoryId,
                    carEntrance.CustomerId,
                    userId,
                    true,
                    false,
                    carEntrance.Notes,
                    carEntrance.Image,
                    carEntrance.Total,
                    carEntrance.TotalAfterTaxes,
                    carEntrance.VoucherDiscountValue,
                    carEntrance.VoucherDiscountPercentage,
                    carEntrance.NetTotal,
                    carEntrance.SalesTaxes,
                    carEntrance.OutDate,
                    carEntrance.CurrencyId,
                    carEntrance.CurrencyEquivalent,
                    true,
                    false,
                    false,
                    false,
                    true,
                    SystemPageId,
                    CarEntranceId,
                    carEntrance.CarInOdometer,
                    carEntrance.CarOutOdometer,
                    CarWorkOrderDetails);

                int id = (int)idResult.Value;

                ////-------------------- Notification-------------------------////
                Notification.GetNotification("CarWorkOrder", "Add", "AddEdit", id, null, "خدمات السيارات - أوامر الشغل");

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "خدمات السيارات -إضافة أمر شغل",
                    EnAction = "Add",
                    ControllerName = "CarWorkOrder",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    //SelectedItem = carSalesInvoice.Id > 0 ? carSalesInvoice.Id : db.SalesInvoices.Max(i => i.Id),
                    //CodeOrDocNo = carSalesInvoice.DocumentNumber
                });


                carEntrance.IsLinked = true;
                db.Entry(carEntrance).State = EntityState.Modified;
                db.SaveChanges();

                return Json(new { success = "true", id });
            }
            var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();

            return Json(new { success = "false", errors });
        }


        // POST: CarSalesInvoice/Delete/5
        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            int userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            CarWorkOrder carWorkOrder = db.CarWorkOrders.Find(id);
            List<CarWorkOrderDetail> WorkOrderDetails = db.CarWorkOrderDetails.Where(a => a.MainDocId == id).ToList();
            carWorkOrder.IsDeleted = true;
            carWorkOrder.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            Random random = new Random();
            const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            for (int i = 0; i < WorkOrderDetails.Count(); i++)
            {
                WorkOrderDetails[i].IsDeleted = true;
            }
           
            CarEntrance carEntrance = db.CarEntrances.Where(a => a.IsDeleted == false && a.IsActive == true && a.Id == carWorkOrder.SelectedId).FirstOrDefault();
            CarSalesInvoice carSalesInvoice = db.CarSalesInvoices.Where(a => a.IsDeleted == false && a.IsActive == true && a.SelectedId == carEntrance.Id).FirstOrDefault();
            if (carSalesInvoice == null)
            {
                carEntrance.IsLinked = false;                
                db.Entry(carEntrance).State = EntityState.Modified;
            }
            var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
            carWorkOrder.DocumentNumber = Code;
            db.Entry(carWorkOrder).State = EntityState.Modified;

            db.SaveChanges();


            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "خدمات السيارات - حذف أمر شغل",
                EnAction = "Delete",
                ControllerName = "CarWorkOrder",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "POST",
                SelectedItem = id,
                CodeOrDocNo = carWorkOrder.DocumentNumber
            });

            ////-------------------- Notification-------------------------////
            Notification.GetNotification("CarWorkOrder", "Delete", "Delete", id, null, "خدمات السيارات - أوامر الشغل");

            return Content("true");

        }


        public JsonResult CheckSalesInvoiceExist(int id)
        {
            db.Configuration.ProxyCreationEnabled = false;
            return Json(db.CarSalesInvoices.Where(a=>a.SelectedId == id && a.IsActive == true && a.IsDeleted == false).ToList(), JsonRequestBehavior.AllowGet);
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