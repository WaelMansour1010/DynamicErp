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
    public class CarSalesInvoiceController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: CarSalesInvoice
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "خدمات السيارات - فتح قائمة أوامر الشغل",
                EnAction = "Index",
                ControllerName = "CarSalesInvoice",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("CarSalesInvoice", "View", "Index", null, null, "خدمات السيارات - فواتير البيع");
            //////////-----------------------------------------------------------------------
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<CarSalesInvoice> carSalesInvoice;

            if (string.IsNullOrEmpty(searchWord))
            {
                carSalesInvoice = db.CarSalesInvoices.Where(s => s.IsDeleted == false && s.IsActive == true).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.CarSalesInvoices.Where(s => s.IsDeleted == false && s.IsActive == true).Count();
            }
            else
            {
                carSalesInvoice = db.CarSalesInvoices.Where(s => s.IsDeleted == false && s.IsActive == true && (s.DocumentNumber.Contains(searchWord) || s.Customer.ArName.Contains(searchWord) || s.CarModel.ArName.Contains(searchWord) || s.VoucherDate.ToString().Contains(searchWord) || s.Notes.Contains(searchWord))).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.CarSalesInvoices.Where(s => s.IsDeleted == false && s.IsActive == true && (s.DocumentNumber.Contains(searchWord) || s.Customer.ArName.Contains(searchWord) || s.CarModel.ArName.Contains(searchWord) || s.VoucherDate.ToString().Contains(searchWord) || s.Notes.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(carSalesInvoice.ToList());
        }

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

            CarSalesInvoice carSalesInvoice = await db.CarSalesInvoices.FindAsync(id);
            if (carSalesInvoice == null)
                return HttpNotFound();

            ViewBag.CarEntranceId = carSalesInvoice.SelectedId;

            ViewBag.Next = QueryHelper.Next((int)id, "CarSalesInvoice");
            ViewBag.Previous = QueryHelper.Previous((int)id, "CarSalesInvoice");
            ViewBag.Last = QueryHelper.GetLast("CarSalesInvoice");
            ViewBag.First = QueryHelper.GetFirst("CarSalesInvoice");

            int sysPageId = QueryHelper.SourcePageId("CarSalesInvoice");
            JournalEntry journal = db.JournalEntries.FirstOrDefault(j => j.SourceId == id && j.SourcePageId == sysPageId);
            ViewBag.Journal = journal;
            ViewBag.Journal.DocumentNumber = journal != null ? journal.DocumentNumber.Replace("-", "") : "";
            var SourceDocumentNumber = db.CarEntrances.Where(a => a.Id == carSalesInvoice.SelectedId).Select(a => a.DocumentNumber).FirstOrDefault();
            ViewBag.SourceDocumentNumber = SourceDocumentNumber != null ? SourceDocumentNumber.Replace("-", "") : "";

            ViewBag.CustomerId = new SelectList(db.Customers.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", carSalesInvoice.CustomerId);

            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", carSalesInvoice.DepartmentId);

            ViewBag.CarTypeId = new SelectList(db.CarTypes.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", carSalesInvoice.CarTypeId);

            ViewBag.CarModelId = new SelectList(db.CarModels.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", carSalesInvoice.CarModelId);

            ViewBag.CarColorId = new SelectList(db.CarColors.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", carSalesInvoice.CarColorId);

            ViewBag.CarServiceCategoryId = new SelectList(db.CarServiceCategories.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", carSalesInvoice.CarServiceCategoryId);

            ViewBag.CarServiceId = new SelectList(db.CarServices.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "خدمات السيارات - فتح تفاصيل فاتورة بيع",
                EnAction = "AddEdit",
                ControllerName = "CarSalesInvoice",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = carSalesInvoice.Id,
                CodeOrDocNo = carSalesInvoice.DocumentNumber
            });


            try
            {
                ViewBag.VoucherDate = carSalesInvoice.VoucherDate.Value.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.OutDate = carSalesInvoice.OutDate.Value.ToString("yyyy-MM-ddTHH:mm");
            }
            catch (Exception)
            {
            }

            return View(carSalesInvoice);
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
                var CarSalesInvoiceDetails = MyXML.GetXML(carEntrance.CarEntranceDetails.Select(x => new { x.CarServiceId, x.ServiceDiscountPercentage, x.ServiceDiscountValue, x.Price, x.MainDocId, x.ServiceNetTotal, SelectedId = CarEntranceId, x.ServiceTypeId, SystemPageId, x.CurrencyId, x.CurrencyEquivalent, x.IsDeleted }));




                var idResult = new ObjectParameter("Id", typeof(Int32));
                db.CarSalesInvoice_Insert(idResult,
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
                    CarSalesInvoiceDetails);

                int id = (int)idResult.Value;

                ////-------------------- Notification-------------------------////
                Notification.GetNotification("CarSalesInvoice", "Add", "AddEdit", id, null, "خدمات السيارات - فواتير البيع");

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "خدمات السيارات -إضافة فاتورة بيع",
                    EnAction = "Add",
                    ControllerName = "CarSalesInvoice",
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
            CarSalesInvoice carSalesInvoice = db.CarSalesInvoices.Find(id);
            List<CarSalesInvoiceDetail> SalesInvoiceDetails = db.CarSalesInvoiceDetails.Where(a => a.MainDocId == id).ToList();
            carSalesInvoice.IsDeleted = true;
            carSalesInvoice.UserId = userId;
            for (int i = 0; i < SalesInvoiceDetails.Count(); i++)
            {
                SalesInvoiceDetails[i].IsDeleted = true;
            }
            CarEntrance carEntrance = db.CarEntrances.Where(a => a.IsDeleted == false && a.IsActive == true && a.Id == carSalesInvoice.SelectedId).FirstOrDefault();
            CarWorkOrder carWorkOrder = db.CarWorkOrders.Where(a => a.IsDeleted == false && a.IsActive == true && a.SelectedId == carEntrance.Id).FirstOrDefault();
            Random random = new Random();
            const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            if (carWorkOrder == null)
            {
                carEntrance.IsLinked = false;
                var CarEntranceCode = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
                carEntrance.DocumentNumber = CarEntranceCode;
                db.Entry(carEntrance).State = EntityState.Modified;
            }
            
            var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
            carSalesInvoice.DocumentNumber = Code;

            db.Entry(carSalesInvoice).State = EntityState.Modified;
            var systemPageId = db.SystemPages.Where(a => a.ControllerName == "CarSalesInvoice").FirstOrDefault().Id;
            var journalEntry = db.JournalEntries.Where(a => a.SourcePageId == systemPageId && a.SourceId == id).FirstOrDefault();
            journalEntry.IsDeleted = true;
            foreach (var item in journalEntry.JournalEntryDetails)
            {
                item.IsDeleted = true;
            }

            var JournalCode = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
            journalEntry.DocumentNumber = JournalCode;
            db.Entry(journalEntry).State = EntityState.Modified;
            db.SaveChanges();


            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "خدمات السيارات - حذف فاتورة بيع",
                EnAction = "Delete",
                ControllerName = "CarSalesInvoice",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "POST",
                SelectedItem = id,
                CodeOrDocNo = carSalesInvoice.DocumentNumber
            });

            ////-------------------- Notification-------------------------////
            Notification.GetNotification("CarSalesInvoice", "Delete", "Delete", id, null, "خدمات السيارات - فواتير البيع");

            return Content("true");

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