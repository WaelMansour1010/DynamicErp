using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using MyERP.Repository;

namespace MyERP.Controllers.PropertyManagement
{
    public class PropertyController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: Property
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمةالعقار",
                EnAction = "Index",
                ControllerName = "Property",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            Notification.GetNotification("Property", "View", "Index", null, null, "العقار");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }
            IQueryable<Property> properties;
            if (string.IsNullOrEmpty(searchWord))
            {
                properties = db.Properties.Where(a => a.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Properties.Where(a => a.IsDeleted == false).Count();
            }
            else
            {
                properties = db.Properties.Where(a => a.IsDeleted == false &&
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Properties.Where(a => a.IsDeleted == false && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(properties.ToList());
        }

        public ActionResult AddEdit(int? id)
        {
            var systemSetting = db.SystemSettings.FirstOrDefault();
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

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
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            if (id == null)
            {
                ViewBag.PropertyTypeId = new SelectList(db.PropertyTypes.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", systemSetting.DefaultDepartmentId);
                ViewBag.CollectorId = new SelectList(db.Employees.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.FixedAssetId = new SelectList(db.FixedAssets.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.CountryId = new SelectList(db.Countries.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.CityId = new SelectList(db.Cities.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.MaintenanceTypeId = new SelectList(new List<dynamic> {
                new { Id=1,ArName="داخلية"},
                new { Id=2,ArName="خارجية"}}, "Id", "ArName");

                ViewBag.PropertyStatusId = new SelectList(db.PropertyStatus.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.FrontispieceId = new SelectList(new List<dynamic> {
               new { Id=1,ArName="بحري"},
                new { Id=2,ArName="قبلى"}}, "Id", "ArName");
                ViewBag.PropertyOwnerId = new SelectList(db.PropertyOwners.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.PeriodBetweenBatchesTypeId = new SelectList(new List<dynamic> {
                new { Id=1,ArName="يوم"},
                 new { Id=2,ArName="اسبوع"},
                new { Id=3,ArName="شهر"},
                new { Id=4,ArName="سنة"}}, "Id", "ArName");
                ViewBag.PropertyUnitTypeId = new SelectList(db.PropertyUnitTypes.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.TypeId = new SelectList(new List<dynamic> {
               new { Id=1,ArName="سكني"},
                new { Id=2,ArName="إداري"}}, "Id", "ArName");
                //ViewBag.StatusId = new SelectList(new List<dynamic> {
                //new { Id=1,ArName="مؤجر"},
                // new { Id=2,ArName="متاح"}}, "Id", "ArName");
                ModelState.Remove("StatusId");
                ViewBag.StatusId = new SelectList(
    new[]
    {
        new { Id = 1, ArName = "مؤجر" },
        new { Id = 2, ArName = "متاح" }
    },
    "Id", "ArName",
    2 // الافتراضي = متاح
);



                ViewBag.ContractDate = cTime.ToString("yyyy-MM-dd");
                ViewBag.ContractStartDate = cTime.ToString("yyyy-MM-dd");
                ViewBag.ContractEndDate = cTime.ToString("yyyy-MM-dd");
                ViewBag.FirstBatchDate = cTime.ToString("yyyy-MM-dd");

                return View();
            }
            Property property = db.Properties.Find(id);

            if (property == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل العقار ",
                EnAction = "AddEdit",
                ControllerName = "Property",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });

            ViewBag.PropertyTypeId = new SelectList(db.PropertyTypes.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", property.PropertyTypeId);
            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", property.DepartmentId);
            ViewBag.CollectorId = new SelectList(db.Employees.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", property.CollectorId);
            ViewBag.FixedAssetId = new SelectList(db.FixedAssets.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", property.FixedAssetId);
            ViewBag.CountryId = new SelectList(db.Countries.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", property.CountryId);
            ViewBag.CityId = new SelectList(db.Cities.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", property.CityId);
            ViewBag.MaintenanceTypeId = new SelectList(new List<dynamic> {
               new { id=1,name="داخلية"},
                new { id=2,name="خارجية"}}, "id", "name", property.MaintenanceTypeId);
            ViewBag.PropertyStatusId = new SelectList(db.PropertyStatus.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", property.PropertyStatusId);
            ViewBag.PropertyUnitTypeId = new SelectList(db.PropertyUnitTypes.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");
            ViewBag.FrontispieceId = new SelectList(new List<dynamic> {
               new { Id=1,ArName="بحري"},
                new { Id=2,ArName="قبلى"}}, "Id", "ArName", property.FrontispieceId);
            ViewBag.PropertyOwnerId = new SelectList(db.PropertyOwners.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", property.PropertyOwnerId);

            ViewBag.PeriodBetweenBatchesTypeId = new SelectList(new List<dynamic> {
                new { Id=1,ArName="يوم"},
                 new { Id=2,ArName="اسبوع"},
                new { Id=3,ArName="شهر"},
                new { Id=4,ArName="سنة"}}, "Id", "ArName", property.PeriodBetweenBatchesTypeId);
            ViewBag.TypeId = new SelectList(new List<dynamic> {
               new { Id=1,ArName="سكني"},
                new { Id=2,ArName="إداري"}}, "Id", "ArName");


            //ViewBag.StatusId = new SelectList(new List<dynamic> {
            //   new { Id=1,ArName="مؤجر"},
            //    new { Id=2,ArName="متاح"}}, "Id", "ArName");
            ModelState.Remove("StatusId");
            ViewBag.StatusId = new SelectList(
               new[]
               {
        new { Id = 1, ArName = "مؤجر" },
        new { Id = 2, ArName = "متاح" }
               },
               "Id", "ArName",
               2 // خليه برضه متاح افتراضياً عند إضافة سطر تفصيلة جديد
           );


            ViewBag.ContractDate = property.ContractDate != null ? property.ContractDate.Value.ToString("yyyy-MM-dd") : null;
            ViewBag.ContractStartDate = property.ContractStartDate != null ? property.ContractStartDate.Value.ToString("yyyy-MM-dd") : null;
            ViewBag.ContractEndDate = property.ContractEndDate != null ? property.ContractEndDate.Value.ToString("yyyy-MM-dd") : null;

            ViewBag.FirstBatchDate = property.FirstBatchDate != null ? property.FirstBatchDate.Value.ToString("yyyy-MM-dd") : null;

            ViewBag.Next = QueryHelper.Next((int)id, "Property");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Property");
            ViewBag.Last = QueryHelper.GetLast("Property");
            ViewBag.First = QueryHelper.GetFirst("Property");
           
            return View(property);
        }
        [HttpPost]
        public ActionResult AddEdit(Property property)
        {
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);

            if (ModelState.IsValid)
            {
                var id = property.Id;
                property.IsDeleted = false;
                property.IsActive = true;
                property.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                if (property.Id > 0)
                {
                    //----------------PropertyDetails---------------------------
                    // Get existing PropertyDetails from DB
                    var existingDetails = db.PropertyDetails.Where(x => x.MainDocId == property.Id).ToList();

                    // Update existing records
                    foreach (var propDetail in property.PropertyDetails)
                    {
                        var existingDetail = existingDetails.FirstOrDefault(x => x.Id == propDetail.Id);
                        if (existingDetail != null)
                        {
                            // Update the existing record
                            existingDetail.MainDocId = propDetail.MainDocId; // Replace with actual properties
                            existingDetail.PropertyUnitNo = propDetail.PropertyUnitNo;
                            existingDetail.PropertyUnitTypeId = propDetail.PropertyUnitTypeId;
                            existingDetail.Floor = propDetail.Floor;
                            existingDetail.RoomsNo = propDetail.RoomsNo;
                            existingDetail.HallsNo = propDetail.HallsNo;
                            existingDetail.IsFurnishing = propDetail.IsFurnishing;
                            existingDetail.IsApplyTax = propDetail.IsApplyTax;
                            existingDetail.StatusId = propDetail.StatusId;
                            existingDetail.TypeId = propDetail.TypeId;
                            existingDetail.RentMethod = propDetail.RentMethod;
                            existingDetail.Area = propDetail.Area;
                            existingDetail.MeterPrice = propDetail.MeterPrice;
                            existingDetail.RentalValue = propDetail.RentalValue;
                            existingDetail.LowestRentalValue = propDetail.LowestRentalValue; 
                            existingDetail.IsDeleted = propDetail.IsDeleted;
                            existingDetail.UserId = propDetail.UserId;
                            existingDetail.Notes = propDetail.Notes;
                            existingDetail.Image = propDetail.Image;
                        }
                        else
                        {
                            // Add new record
                            propDetail.MainDocId = property.Id;
                            db.PropertyDetails.Add(propDetail);
                        }
                    }

                    // Remove deleted records
                    var detailsToRemove = existingDetails
                        .Where(x => !property.PropertyDetails.Any(y => y.Id == x.Id))
                        .ToList();
                    db.PropertyDetails.RemoveRange(detailsToRemove);

                    //
                    //----------------PropertyBatches---------------------------
                    // Fetch existing PropertyBatches
                    var existingBatches = db.PropertyBatches.Where(x => x.MainDocId == property.Id).ToList();

                    // Update existing records
                    foreach (var propBatch in property.PropertyBatches)
                    {
                        var existingBatch = existingBatches.FirstOrDefault(x => x.Id == propBatch.Id);
                        if (existingBatch != null)
                        {
                            // Update the existing record
                            existingBatch.MainDocId = propBatch.MainDocId; // Replace with actual properties
                            existingBatch.BatchNo = propBatch.BatchNo;
                            existingBatch.BatchDate = propBatch.BatchDate;
                            existingBatch.BatchValueBeforeDiscountAddtionAndTax = propBatch.BatchValueBeforeDiscountAddtionAndTax;
                            //existingBatch.BatchValueBeforeTax = propBatch.BatchValueBeforeTax;
                            existingBatch.Discount = propBatch.Discount;
                            existingBatch.AddValue = propBatch.AddValue;
                            //existingBatch.TotalBatchValue = propBatch.TotalBatchValue;
                            //existingBatch.BatchTaxValue = propBatch.BatchTaxValue;
                            existingBatch.BatchTaxPercentage = propBatch.BatchTaxPercentage;
                            existingBatch.IsDeleted = propBatch.IsDeleted;
                            existingBatch.UserId = propBatch.UserId;
                            existingBatch.Notes = propBatch.Notes;
                            existingBatch.Image = propBatch.Image;
                            existingBatch.NumberOfBatches = propBatch.NumberOfBatches;
                            existingBatch.FirstBatchDate = propBatch.FirstBatchDate;
                            existingBatch.PeriodBetweenBatchesNum = propBatch.PeriodBetweenBatchesNum;
                            existingBatch.PeriodBetweenBatchesTypeId = propBatch.PeriodBetweenBatchesTypeId;
                        }
                        else
                        {
                            // Add new record
                            propBatch.MainDocId = property.Id;
                            db.PropertyBatches.Add(propBatch);
                        }
                    }

                    // Remove deleted records
                    var batchesToRemove = existingBatches
                        .Where(x => !property.PropertyBatches.Any(y => y.Id == x.Id))
                        .ToList();
                    db.PropertyBatches.RemoveRange(batchesToRemove);

                    Notification.GetNotification("Property", "Edit", "AddEdit", property.Id, null, "العقار");
                }
                else
                {
                    property.Code = (QueryHelper.CodeLastNum("Property") + 1).ToString();
                    db.Properties.Add(property);
                    Notification.GetNotification("Property", "Add", "AddEdit", property.Id, null, "العقار");
                }
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل العقار" : "اضافة العقار",
                    EnAction = "AddEdit",
                    ControllerName = "Property",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = property.Code
                });
                return Json(new { success = true });
            }
            var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();
            return Json(new { success = false });
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                Property property = db.Properties.Find(id);
                property.IsDeleted = true;
                property.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                foreach (var item in property.PropertyBatches)
                {
                    item.IsDeleted = true;
                }
                foreach (var item in property.PropertyDetails)
                {
                    item.IsDeleted = true;
                }
                db.Entry(property).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف العقار",
                    EnAction = "AddEdit",
                    ControllerName = "Property",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id
                });
                Notification.GetNotification("Property", "Delete", "Delete", id, null, "العقار");
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
                Property property = db.Properties.Find(id);
                if (property.IsActive == true)
                {
                    property.IsActive = false;
                }
                else
                {
                    property.IsActive = true;
                }
                property.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(property).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)property.IsActive ? "تنشيط العقار" : "إلغاء تنشيط العقار",
                    EnAction = "AddEdit",
                    ControllerName = "Property",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = property.Id,
                    CodeOrDocNo = property.Code
                });
                if (property.IsActive == true)
                {
                    Notification.GetNotification("Property", "Activate/Deactivate", "ActivateDeactivate", id, true, "العقار");
                }
                else
                {
                    Notification.GetNotification("Property", "Activate/Deactivate", "ActivateDeactivate", id, false, "العقار");
                }
                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }

        [SkipERPAuthorize]
        public JsonResult SetCodeNum()
        {
            var code = QueryHelper.CodeLastNum("Property");
            return Json(code + 1, JsonRequestBehavior.AllowGet);
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