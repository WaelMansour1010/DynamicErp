using MyERP.Models;
using MyERP.Repository;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using MyERP.Models.MyModels;
using MyERP.Utils;

namespace MyERP.Controllers.PropertyManagement
{
    public class PropertyContractController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: PropertyContract
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة العقود",
                EnAction = "Index",
                ControllerName = "PropertyContract",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("PropertyContract", "View", "Index", null, null, "العقود");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<PropertyContract> propertyContracts;

            if (string.IsNullOrEmpty(searchWord))
            {
                propertyContracts = db.PropertyContracts.Where(s => s.IsDeleted == false).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.PropertyContracts.Where(s => s.IsDeleted == false).Count();
            }
            else
            {
                propertyContracts = db.PropertyContracts.Where(s => s.IsDeleted == false && (s.PropertyRenter.ArName.Contains(searchWord) ||  (s.DocumentNumber.Contains(searchWord) ||  
                    s.Property.ArName.Contains(searchWord)))).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.PropertyContracts.Where(s => s.IsDeleted == false && (s.PropertyRenter.ArName.Contains(searchWord) || (s.DocumentNumber.Contains(searchWord) ||
                    s.Property.ArName.Contains(searchWord)))).Count();
            }

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة العقود",
                EnAction = "Index",
                ControllerName = "PropertyContract",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            return View(propertyContracts.ToList());
        }
        public ActionResult AddEdit(int? id)
        {
            SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
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

            if (id == null)
            {
                ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId).ToList(), "Id", "ArName", systemSetting.DefaultDepartmentId);
                ViewBag.PropertyId = new SelectList(db.Properties.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }).ToList(), "Id", "ArName");
                ViewBag.PropertyUnitTypeId = new SelectList(db.PropertyUnitTypes.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }).ToList(), "Id", "ArName");
                ViewBag.PropertyUnitId = new SelectList(db.PropertyUnits.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }).ToList(), "Id", "ArName");

                ViewBag.PropertyContractMergedUnit = new MultiSelectList(db.PropertyUnits.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }).ToList(), "Id", "ArName");
                
                ViewBag.PropertyOwnerId = new SelectList(db.PropertyOwners.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }).ToList(), "Id", "ArName");
                ViewBag.PropertyRenterId = new SelectList(db.PropertyRenters.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }).ToList(), "Id", "ArName");
                ViewBag.RepId = new SelectList(db.Employees.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }).ToList(), "Id", "ArName");
                // for PropertyContractReps
                ViewBag.repId = new SelectList(db.Employees.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }).ToList(), "Id", "ArName");
                ViewBag.ContractPeriodTypeId = new SelectList(new List<dynamic> {
                 new { id=1,name="يوم"},
                 new { id=2,name="اسبوع"},
                new { id=3,name="شهر"},
                new { id=4,name="سنة"}}, "id", "name");
                ViewBag.PeriodBetweenBatchesTypeId = new SelectList(new List<dynamic> {
                new { id=1,name="يوم"},
                 new { id=2,name="اسبوع"},
                new { id=3,name="شهر"},
                new { id=4,name="سنة"}}, "id", "name");
                ViewBag.ContractTypeId = new SelectList(new List<dynamic> {
                new { id=1,name="جديد"},
                new { id=2,name="افتتاحى"}}, "id", "name");
                ViewBag.RentTypeId = new SelectList(new List<dynamic> {
               new { id=1,name="يومي"},
                new { id=2,name="شهري"},
                new { id=3,name="سنوي"}}, "id", "name");

                ViewBag.VoucherDate = cTime.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.ContractStartDate = cTime.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.ContractEndDate = cTime.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.FirstBatchDate = cTime.ToString("yyyy-MM-ddTHH:mm");
                return View();
            }
            PropertyContract contract = db.PropertyContracts.Find(id);
            ViewBag.IsLinkedWithCashReceiptVoucher = (contract.PropertyContractBatches.Where(a => a.IsDelivered == true).FirstOrDefault() != null ? contract.PropertyContractBatches.Where(a => a.IsDelivered == true).FirstOrDefault().CashReceiptVoucherPropertyContractBatches.Count() : 0) > 0 ? true : false;

            if (contract == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل العقود ",
                EnAction = "AddEdit",
                ControllerName = "PropertyContract",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });


            ViewBag.Next = QueryHelper.Next((int)id, "PropertyContract");
            ViewBag.Previous = QueryHelper.Previous((int)id, "PropertyContract");
            ViewBag.Last = QueryHelper.GetLast("PropertyContract");
            ViewBag.First = QueryHelper.GetFirst("PropertyContract");


            // journal Entry ----
            int sysPageId = QueryHelper.SourcePageId("PropertyContract");

            ViewBag.CashRecipt = db.CashReceiptVouchers.Where(s => s.PropertyContractId == id).AsNoTracking().ToList();

            JournalEntry journal = db.JournalEntries.FirstOrDefault(j => j.SourceId == id && j.SourcePageId == sysPageId);

            ViewBag.Journal = journal;
            ViewBag.JournalDocumentNumber = journal != null && journal.DocumentNumber != null ? journal.DocumentNumber.Replace("-", "") : "";
            ViewBag.IsPrevious_JE = journal != null ? true : false;
            //-----------------------------------------------------//

            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", contract.DepartmentId);

            ViewBag.PropertyId = new SelectList(db.Properties.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", contract.PropertyId);
            var UnitType = db.PropertyDetails.Where(a => a.IsDeleted == false && a.MainDocId == contract.PropertyId).Select(a => new { Id = a.PropertyUnitType.Id, ArName = a.PropertyUnitType.Code + " - " + a.PropertyUnitType.ArName }).Distinct();
            //ViewBag.PropertyUnitTypeId = new SelectList(db.PropertyUnitTypes.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            //{
            //    Id = b.Id,
            //    ArName = b.Code + " - " + b.ArName
            //}), "Id", "ArName", contract.PropertyUnitTypeId);
            ViewBag.PropertyUnitTypeId = new SelectList(UnitType, "Id", "ArName", contract.PropertyUnitTypeId);
            
            var allUnits =( from d in db.PropertyDetails
                where d.Id == contract.PropertyUnitId
                      && d.MainDocId == contract.PropertyId
                select d).ToList();

            ViewBag.PropertyUnitId = new SelectList(allUnits, "Id", "PropertyUnitNo", contract.PropertyUnitId);

            var mergedUnitIds = contract.PropertyContractMergedUnit.Select(mu => mu.PropertyUnitId).ToList();

            var selectedMergedUnits = new MultiSelectList(db.PropertyDetails
                .Where(pd => mergedUnitIds.Contains(pd.Id))
                .Select(b => new
                {
                    Id = b.Id,
                    ArName = b.PropertyUnitNo
                }).ToList(), "Id", "ArName");
            
            // Pass as MultiSelectList
            ViewBag.PropertyContractMergedUnit = new MultiSelectList(db.PropertyDetails.Where(a => a.MainDocId == contract.PropertyId && a.StatusId == PropertyDetailsStatus.Available).Select(b => new
            {
                Id = b.Id,
                ArName = b.PropertyUnitNo
            }).ToList(), "Id", "ArName", selectedMergedUnits);

            ViewBag.selectedMergedUnits = (MultiSelectList)selectedMergedUnits;

            ViewBag.PropertyOwnerId = new SelectList(db.PropertyOwners.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", contract.PropertyOwnerId);
            ViewBag.PropertyRenterId = new SelectList(db.PropertyRenters.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", contract.PropertyRenterId);
            ViewBag.RepId = new SelectList(db.Employees.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", contract.RepId);
            // for PropertyContractReps
            ViewBag.repId = new SelectList(db.Employees.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");
            ViewBag.ContractPeriodTypeId = new SelectList(new List<dynamic> {
                new { id=1,name="يوم"},
                 new { id=2,name="اسبوع"},
                new { id=3,name="شهر"},
                new { id=4,name="سنة"}}, "id", "name", contract.ContractPeriodTypeId);
            ViewBag.PeriodBetweenBatchesTypeId = new SelectList(new List<dynamic> {
                new { id=1,name="يوم"},
                 new { id=2,name="اسبوع"},
                new { id=3,name="شهر"},
                new { id=4,name="سنة"}}, "id", "name", contract.PeriodBetweenBatchesTypeId);
            ViewBag.ContractTypeId = new SelectList(new List<dynamic> {
                new { id=1,name="جديد"},
                new { id=2,name="افتتاحى"}}, "id", "name", contract.ContractTypeId);
            ViewBag.RentTypeId = new SelectList(new List<dynamic> {
               new { id=1,name="يومي"},
                new { id=2,name="اسبوعى"},
                new { id=3,name="شهري"},
                new { id=4,name="سنوي"}}, "id", "name", contract.RentTypeId);

            ViewBag.VoucherDate = contract.VoucherDate != null ? contract.VoucherDate.ToString("yyyy-MM-ddTHH:mm") : null;
            ViewBag.ContractStartDate = contract.ContractStartDate != null ? contract.ContractStartDate.Value.ToString("yyyy-MM-ddTHH:mm") : null;
            ViewBag.ContractEndDate = contract.ContractEndDate != null ? contract.ContractEndDate.Value.ToString("yyyy-MM-ddTHH:mm") : null;
            ViewBag.FirstBatchDate = contract.FirstBatchDate != null ? contract.FirstBatchDate.Value.ToString("yyyy-MM-ddTHH:mm") : null;

            var voIds = contract.PropertyContractBatches
                .Select(t => t.Id).ToList();

            var bathces = (from bt in db.CashReceiptVoucherPropertyContractBatches
                where voIds.Contains(bt.PropertyContractBatchId??0)
                select new { bt.PropertyContractBatchId, bt.Paid    }).GroupBy(t => t.PropertyContractBatchId).Select(t => new BatchPaidModel
            {
                Id = t.Key ?? 0,
                Paid = t.Sum(n => n.Paid ?? 0)
            }).ToList();
            //var bathces2 = (from bt in db.CashReceiptVoucherPropertyContractBatches
            //    where voIds.Contains(bt.PropertyContractBatchId ?? 0)
            //    select bt).ToList();
            //    select new { bt.PropertyContractBatchId, bt.Paid }).GroupBy(t => t.PropertyContractBatchId).Select(t => new BatchPaidModel
            //{
            //    Id = t.Key ?? 0,
            //    Paid = t.Sum(n => n.Paid ?? 0)
            //}).ToList();
            ViewBag.BathcesPaid = bathces;

            return View(contract);
        }

        [HttpPost]
        public ActionResult AddEdit(PropertyContract contract)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            var bytes = new byte[7000];
            string fileName = "";
            if (ModelState.IsValid)
            {
                var id = contract.Id;
                contract.IsDeleted = false;
                contract.UserId = userId;
                var PropertyContractImageList = new List<PropertyContractImage>();
                if (contract.Id > 0)
                {
                    List<PropertyContractImage> imageList = db.PropertyContractImages.Where(i => i.MainDocId == contract.Id).ToList();

                    var lastPropertyContractImage = db.PropertyContractImages.OrderByDescending(a => a.Id).FirstOrDefault();
                    int LastItemID = (lastPropertyContractImage != null ? lastPropertyContractImage.Id : 0) + 1;

                    foreach (var img in contract.PropertyContractImages)
                    {

                        if (img != null && img.Image.Contains("base64"))
                        {
                            fileName = "/images/PropertyManagement/PropertyContract/PropertyContract" + LastItemID.ToString() + ".jpeg";
                            if (img.Image.Contains("jpeg"))
                            {
                                bytes = Convert.FromBase64String(img.Image.Replace("data:image/jpeg;base64,", ""));
                            }
                            else
                            {
                                bytes = Convert.FromBase64String(img.Image.Replace("data:image/png;base64,", ""));
                            }
                            using (var imageFile = new FileStream(Server.MapPath(fileName), FileMode.Create))
                            {
                                imageFile.Write(bytes, 0, bytes.Length);
                                imageFile.Flush();
                            }
                            PropertyContractImageList.Add(new PropertyContractImage()
                            {
                                Image = domainName + fileName,
                                MainDocId = contract.Id
                            });
                            LastItemID++;
                        }
                        else //previous images
                        {
                            PropertyContractImageList.Add(new PropertyContractImage()
                            {
                                Image = img.Image,
                                MainDocId = contract.Id
                            });
                        }
                    }
                    MyXML.xPathName = "Reps";
                    var PropertyContractReps = MyXML.GetXML(contract.PropertyContractReps);
                    MyXML.xPathName = "Images";
                    var PropertyContractImages = MyXML.GetXML(PropertyContractImageList);

                    //----------------------------------- حولتها ل data table عشان كان جواها  list of CashReceiptVoucherPropertyContractBatches فمكانش عارف يحولها ل xml   ------------------------------------------//
                    DataTable Batches = new DataTable("Batches");
                    //DataColumn _Id = new DataColumn("Id", typeof(int));
                    //DataColumn MainDocId = new DataColumn("MainDocId", typeof(int));
                    DataColumn BatchNo = new DataColumn("BatchNo", typeof(int));
                    DataColumn BatchDate = new DataColumn("BatchDate", typeof(DateTime));
                    DataColumn BatchRentValue = new DataColumn("BatchRentValue", typeof(decimal));
                    DataColumn BatchRentValueTaxes = new DataColumn("BatchRentValueTaxes", typeof(decimal));
                    DataColumn BatchWaterValue = new DataColumn("BatchWaterValue", typeof(decimal));
                    DataColumn BatchWaterValueTaxes = new DataColumn("BatchWaterValueTaxes", typeof(decimal));
                    DataColumn BatchElectricityValue = new DataColumn("BatchElectricityValue", typeof(decimal));
                    DataColumn BatchElectricityValueTaxes = new DataColumn("BatchElectricityValueTaxes", typeof(decimal));
                    DataColumn BatchCommissionValue = new DataColumn("BatchCommissionValue", typeof(decimal));
                    DataColumn BatchCommissionValueTaxes = new DataColumn("BatchCommissionValueTaxes", typeof(decimal));
                    DataColumn BatchGasValue = new DataColumn("BatchGasValue", typeof(decimal));
                    DataColumn BatchGasValueTaxes = new DataColumn("BatchGasValueTaxes", typeof(decimal));
                    DataColumn BatchServicesValue = new DataColumn("BatchServicesValue", typeof(decimal));
                    DataColumn BatchServicesValueTaxes = new DataColumn("BatchServicesValueTaxes", typeof(decimal));
                    DataColumn BatchInsuranceValue = new DataColumn("BatchInsuranceValue", typeof(decimal));
                    DataColumn BatchInsuranceValueTaxes = new DataColumn("BatchInsuranceValueTaxes", typeof(decimal));
                    DataColumn BatchTotal = new DataColumn("BatchTotal", typeof(decimal));
                    DataColumn IsDelivered = new DataColumn("IsDelivered", typeof(bool));
                    DataColumn JournalEntryId = new DataColumn("JournalEntryId", typeof(int));
                    DataColumn IsDeleted = new DataColumn("IsDeleted", typeof(bool));
                    DataColumn _UserId = new DataColumn("UserId", typeof(int));
                    DataColumn Notes = new DataColumn("Notes", typeof(string));
                    DataColumn Image = new DataColumn("Image", typeof(string));

                    //BatchesXml.Columns.Add(_Id);
                    //BatchesXml.Columns.Add(MainDocId);
                    Batches.Columns.Add(BatchNo);
                    Batches.Columns.Add(BatchDate);
                    Batches.Columns.Add(BatchRentValue);
                    Batches.Columns.Add(BatchRentValueTaxes);
                    Batches.Columns.Add(BatchWaterValue);
                    Batches.Columns.Add(BatchWaterValueTaxes);
                    Batches.Columns.Add(BatchElectricityValue);
                    Batches.Columns.Add(BatchElectricityValueTaxes);
                    Batches.Columns.Add(BatchCommissionValue);
                    Batches.Columns.Add(BatchCommissionValueTaxes);
                    Batches.Columns.Add(BatchGasValue);
                    Batches.Columns.Add(BatchGasValueTaxes);
                    Batches.Columns.Add(BatchServicesValue);
                    Batches.Columns.Add(BatchServicesValueTaxes);
                    Batches.Columns.Add(BatchInsuranceValue);
                    Batches.Columns.Add(BatchInsuranceValueTaxes);
                    Batches.Columns.Add(BatchTotal);
                    Batches.Columns.Add(IsDelivered);
                    Batches.Columns.Add(IsDeleted);
                    Batches.Columns.Add(JournalEntryId);
                    Batches.Columns.Add(_UserId);
                    Batches.Columns.Add(Notes);
                    Batches.Columns.Add(Image);

                    foreach (var item in contract.PropertyContractBatches)
                    {
                        DataRow row = Batches.NewRow();
                        //row["Id"] = item.Id;
                        //row["MainDocId"] = item.MainDocId;
                        row["BatchNo"] = item.BatchNo;
                        row["BatchDate"] = item.BatchDate;
                        row["BatchRentValue"] = item.BatchRentValue!=null? item.BatchRentValue:0;
                        row["BatchRentValueTaxes"] = item.BatchRentValueTaxes!=null? item.BatchRentValueTaxes:0;
                        row["BatchWaterValue"] = item.BatchWaterValue!=null? item.BatchWaterValue:0;
                        row["BatchWaterValueTaxes"] = item.BatchWaterValueTaxes!=null? item.BatchWaterValueTaxes:0;
                        row["BatchElectricityValue"] = item.BatchElectricityValue!=null? item.BatchElectricityValue:0;
                        row["BatchElectricityValueTaxes"] = item.BatchElectricityValueTaxes!=null? item.BatchElectricityValueTaxes:0;
                        row["BatchCommissionValue"] = item.BatchCommissionValue!=null? item.BatchCommissionValue:0;
                        row["BatchCommissionValueTaxes"] = item.BatchCommissionValueTaxes!=null? item.BatchCommissionValueTaxes:0;
                        row["BatchGasValue"] = item.BatchGasValue!=null? item.BatchGasValue:0;
                        row["BatchGasValueTaxes"] = item.BatchGasValueTaxes!=null? item.BatchGasValueTaxes:0;
                        row["BatchServicesValue"] = item.BatchServicesValue!=null ? item.BatchServicesValue:0;
                        row["BatchServicesValueTaxes"] = item.BatchServicesValueTaxes!=null? item.BatchServicesValueTaxes:0;
                        row["BatchInsuranceValue"] = item.BatchInsuranceValue!=null? item.BatchInsuranceValue:0;
                        row["BatchInsuranceValueTaxes"] = item.BatchInsuranceValueTaxes!=null? item.BatchInsuranceValueTaxes:0;
                        row["BatchTotal"] = item.BatchTotal;
                        row["UserId"] = item.UserId;
                        if (item.JournalEntryId != null || contract.JournalEntryId != null)
                        {
                            row["JournalEntryId"] = item.JournalEntryId != null ? item.JournalEntryId : contract.JournalEntryId != null ? contract.JournalEntryId : 0;
                        }
                        row["IsDeleted"] = item.IsDeleted;
                        row["Notes"] = item.Notes;
                        row["Image"] = item.Image;
                        row["IsDelivered"] = item.IsDelivered;

                        Batches.Rows.Add(row);
                    }

                    MyXML.xPathName = "Batches";
                    var PropertyContractBatches = MyXML.GetXML(Batches);

                    //----------------------------------- **************************** ------------------------------------------//


                    //MyXML.xPathName = "Batches";
                    //var PropertyContractBatches = MyXML.GetXML(contract.PropertyContractBatches);
                    //-------------------------------MergedUnit-----------------------------------//
                    var MergedUnits = contract.PropertyContractMergedUnit;
                    MyXML.xPathName = "MergedUnits";
                    var PropertyContractMergedUnits = MyXML.GetXML(MergedUnits);
                    //---------------------------------------------------------------------------//

                    db.PropertyContract_Update(contract.Id, contract.DocumentNumber, contract.VoucherDate, contract.ContractTypeId, contract.PropertyId, contract.RentTypeId, contract.PropertyOwnerId, contract.PropertyRenterId, contract.RepId, contract.RentValue, contract.CommissionValue, contract.ServicesValue, contract.WaterValue, contract.NetTotal, contract.VATPercentage, contract.VATValue, contract.TotalAfterTaxes, contract.ContractStartDate, contract.ContractEndDate, contract.ContractPeriodNum, contract.ContractPeriodTypeId, contract.IsDeleted, contract.UserId, contract.Notes, contract.Image, contract.PropertyUnitTypeId, contract.PropertyUnitId, contract.IncludeRentValueInVAT, contract.IncludeWaterValueInVAT, contract.ElectricityValue, contract.IncludeElectricityValueInVAT, contract.GasValue, contract.GracePeriodDay, contract.GracePeriodMonth, contract.NumberOfBatches, contract.FirstBatchDate, contract.PeriodBetweenBatchesNum, contract.PeriodBetweenBatchesTypeId, contract.IsDivideWaterIntoBatches, contract.IsDivideElectricityIntoBatches, contract.UnifiedContractNumber, contract.IsAddedValue, contract.ContractSpecialTerms, contract.JournalEntryId, contract.DepartmentId, contract.InsuranceValue, contract.IncludeInsuranceValueInVAT, contract.IncludeGasValueInVAT, contract.IncludeCommissionValueInVAT, contract.IncludeServicesValueInVAT, contract.GovernmentalOrPrivateContract, PropertyContractBatches, PropertyContractReps, PropertyContractImages, PropertyContractMergedUnits);
                    ////-------------------- Notification-------------------------////
                   
                    Notification.GetNotification("PropertyContract", "Edit", "AddEdit", contract.Id, null, "العقود");
                }
                else
                {
                    var lastPropertyContractImage = db.PropertyContractImages.OrderByDescending(a => a.Id).FirstOrDefault();
                    int LastItemID = (lastPropertyContractImage != null ? lastPropertyContractImage.Id : 0) + 1;
                    foreach (var img in contract.PropertyContractImages)
                    {
                        if (img != null && img.Image.Contains("base64"))
                        {

                            fileName = "/images/PropertyManagement/PropertyContract/PropertyContract" + LastItemID.ToString() + ".jpeg";
                            if (img.Image.Contains("jpeg"))
                            {
                                bytes = Convert.FromBase64String(img.Image.Replace("data:image/jpeg;base64,", ""));
                            }
                            else
                            {
                                bytes = Convert.FromBase64String(img.Image.Replace("data:image/png;base64,", ""));
                            }
                            using (var imageFile = new FileStream(Server.MapPath(fileName), FileMode.Create))
                            {
                                imageFile.Write(bytes, 0, bytes.Length);
                                imageFile.Flush();
                            }
                            PropertyContractImageList.Add(new PropertyContractImage()
                            {
                                Image = domainName + fileName,
                                MainDocId = LastItemID
                            });
                        }
                        LastItemID++;
                    }
                    contract.PropertyContractImages = PropertyContractImageList;

                    MyXML.xPathName = "Images";
                    var PropertyContractImages = MyXML.GetXML(PropertyContractImageList);

                    MyXML.xPathName = "Reps";
                    var PropertyContractReps = MyXML.GetXML(contract.PropertyContractReps);
                    //----------------------------------- حولتها ل data table عشان كان جواها  list of CashReceiptVoucherPropertyContractBatches فمكانش عارف يحولها ل xml   ------------------------------------------//
                    DataTable Batches = new DataTable("Batches");
                    //DataColumn _Id = new DataColumn("Id", typeof(int));
                    //DataColumn MainDocId = new DataColumn("MainDocId", typeof(int));
                    DataColumn BatchNo = new DataColumn("BatchNo", typeof(int));
                    DataColumn BatchDate = new DataColumn("BatchDate", typeof(DateTime));
                    DataColumn BatchRentValue = new DataColumn("BatchRentValue", typeof(decimal));
                    DataColumn BatchRentValueTaxes = new DataColumn("BatchRentValueTaxes", typeof(decimal));
                    DataColumn BatchWaterValue = new DataColumn("BatchWaterValue", typeof(decimal));
                    DataColumn BatchWaterValueTaxes = new DataColumn("BatchWaterValueTaxes", typeof(decimal));
                    DataColumn BatchElectricityValue = new DataColumn("BatchElectricityValue", typeof(decimal));
                    DataColumn BatchElectricityValueTaxes = new DataColumn("BatchElectricityValueTaxes", typeof(decimal));
                    DataColumn BatchCommissionValue = new DataColumn("BatchCommissionValue", typeof(decimal));
                    DataColumn BatchCommissionValueTaxes = new DataColumn("BatchCommissionValueTaxes", typeof(decimal));
                    DataColumn BatchGasValue = new DataColumn("BatchGasValue", typeof(decimal));
                    DataColumn BatchGasValueTaxes = new DataColumn("BatchGasValueTaxes", typeof(decimal));
                    DataColumn BatchServicesValue = new DataColumn("BatchServicesValue", typeof(decimal));
                    DataColumn BatchServicesValueTaxes = new DataColumn("BatchServicesValueTaxes", typeof(decimal));
                    DataColumn BatchInsuranceValue = new DataColumn("BatchInsuranceValue", typeof(decimal));
                    DataColumn BatchInsuranceValueTaxes = new DataColumn("BatchInsuranceValueTaxes", typeof(decimal));
                    DataColumn BatchTotal = new DataColumn("BatchTotal", typeof(decimal));
                    DataColumn IsDelivered = new DataColumn("IsDelivered", typeof(bool));
                    DataColumn JournalEntryId = new DataColumn("JournalEntryId", typeof(int));
                    DataColumn IsDeleted = new DataColumn("IsDeleted", typeof(bool));
                    DataColumn _UserId = new DataColumn("UserId", typeof(int));
                    DataColumn Notes = new DataColumn("Notes", typeof(string));
                    DataColumn Image = new DataColumn("Image", typeof(string));

                    //BatchesXml.Columns.Add(_Id);
                    //BatchesXml.Columns.Add(MainDocId);
                    Batches.Columns.Add(BatchNo);
                    Batches.Columns.Add(BatchDate);
                    Batches.Columns.Add(BatchRentValue);
                    Batches.Columns.Add(BatchRentValueTaxes);
                    Batches.Columns.Add(BatchWaterValue);
                    Batches.Columns.Add(BatchWaterValueTaxes);
                    Batches.Columns.Add(BatchElectricityValue);
                    Batches.Columns.Add(BatchElectricityValueTaxes);
                    Batches.Columns.Add(BatchCommissionValue);
                    Batches.Columns.Add(BatchCommissionValueTaxes);
                    Batches.Columns.Add(BatchGasValue);
                    Batches.Columns.Add(BatchGasValueTaxes);
                    Batches.Columns.Add(BatchServicesValue);
                    Batches.Columns.Add(BatchServicesValueTaxes);
                    Batches.Columns.Add(BatchInsuranceValue);
                    Batches.Columns.Add(BatchInsuranceValueTaxes);
                    Batches.Columns.Add(BatchTotal);
                    Batches.Columns.Add(IsDelivered);
                    Batches.Columns.Add(IsDeleted);
                    Batches.Columns.Add(JournalEntryId);
                    Batches.Columns.Add(_UserId);
                    Batches.Columns.Add(Notes);
                    Batches.Columns.Add(Image);

                    foreach (var item in contract.PropertyContractBatches)
                    {
                        DataRow row = Batches.NewRow();
                        //row["Id"] = item.Id;
                        //row["MainDocId"] = item.MainDocId;
                        row["BatchNo"] = item.BatchNo;
                        row["BatchDate"] = item.BatchDate;
                        row["BatchRentValue"] = item.BatchRentValue;
                        row["BatchRentValueTaxes"] = item.BatchRentValueTaxes;
                        row["BatchWaterValue"] = item.BatchWaterValue;
                        row["BatchWaterValueTaxes"] = item.BatchWaterValueTaxes;
                        row["BatchElectricityValue"] = item.BatchElectricityValue;
                        row["BatchElectricityValueTaxes"] = item.BatchElectricityValueTaxes;
                        row["BatchCommissionValue"] = item.BatchCommissionValue;
                        row["BatchCommissionValueTaxes"] = item.BatchCommissionValueTaxes;
                        row["BatchGasValue"] = item.BatchGasValue;
                        row["BatchGasValueTaxes"] = item.BatchGasValueTaxes;
                        row["BatchServicesValue"] = item.BatchServicesValue;
                        row["BatchServicesValueTaxes"] = item.BatchServicesValueTaxes;
                        row["BatchInsuranceValue"] = item.BatchInsuranceValue;
                        row["BatchInsuranceValueTaxes"] = item.BatchInsuranceValueTaxes;
                        row["BatchTotal"] = item.BatchTotal;
                        row["UserId"] = contract.UserId;
                        //  row["JournalEntryId"] = item.JournalEntryId>0?item.JournalEntryId:null;
                        row["IsDeleted"] = item.IsDeleted;
                        row["Notes"] = item.Notes;
                        row["Image"] = item.Image;
                        row["IsDelivered"] = item.IsDelivered;
                        Batches.Rows.Add(row);
                    }

                    MyXML.xPathName = "Batches";
                    var PropertyContractBatches = MyXML.GetXML(Batches);
                    //----------------------------------- **************************** ------------------------------------------//

                    //MyXML.xPathName = "Batches";
                    //var PropertyContractBatches = MyXML.GetXML(contract.PropertyContractBatches);
                    //-------------------------------MergedUnit-----------------------------------//
                    var MergedUnits = contract.PropertyContractMergedUnit;
                    MyXML.xPathName = "MergedUnits";
                    var PropertyContractMergedUnits = MyXML.GetXML(MergedUnits);
                    //----------------------------------------------------------------------------//
                    var idResult = new ObjectParameter("Id", typeof(Int32));
                    db.PropertyContract_Insert(idResult, contract.VoucherDate, contract.ContractTypeId, contract.PropertyId, contract.RentTypeId, contract.PropertyOwnerId, contract.PropertyRenterId, contract.RepId, contract.RentValue, contract.CommissionValue, contract.ServicesValue, contract.WaterValue, contract.NetTotal, contract.VATPercentage, contract.VATValue, contract.TotalAfterTaxes, contract.ContractStartDate, contract.ContractEndDate, contract.ContractPeriodNum, contract.ContractPeriodTypeId, contract.IsDeleted, contract.UserId, contract.Notes, contract.Image, contract.PropertyUnitTypeId, contract.PropertyUnitId, contract.IncludeRentValueInVAT, contract.IncludeWaterValueInVAT, contract.ElectricityValue, contract.IncludeElectricityValueInVAT, contract.GasValue, contract.GracePeriodDay, contract.GracePeriodMonth, contract.NumberOfBatches, contract.FirstBatchDate, contract.PeriodBetweenBatchesNum, contract.PeriodBetweenBatchesTypeId, contract.IsDivideWaterIntoBatches, contract.IsDivideElectricityIntoBatches, contract.UnifiedContractNumber, contract.IsAddedValue, contract.ContractSpecialTerms, contract.JournalEntryId, contract.DepartmentId, contract.InsuranceValue, contract.IncludeInsuranceValueInVAT, contract.IncludeGasValueInVAT, contract.IncludeCommissionValueInVAT, contract.IncludeServicesValueInVAT, contract.GovernmentalOrPrivateContract, PropertyContractBatches, PropertyContractReps, PropertyContractImages, PropertyContractMergedUnits);
                   //set sttus to not avalable
                    var unit = db.PropertyDetails.Where(t => t.MainDocId == contract.PropertyId &&
                                                             t.Id ==
                                                             contract.PropertyUnitId).FirstOrDefault();
                    unit.StatusId = PropertyDetailsStatus.NotAvailableed;
                    db.SaveChanges();
                    //////////
                    id = (int)idResult.Value;
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("PropertyContract", "Add", "AddEdit", id, null, "العقود");
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = contract.Id > 0 ? "تعديل العقود" : "اضافة العقود",
                    EnAction = "AddEdit",
                    ControllerName = "PropertyContract",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "Post",
                    SelectedItem = id,
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
                PropertyContract contract = db.PropertyContracts.Find(id);
                contract.IsDeleted = true;
                contract.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                var systemPage = db.SystemPages.Where(a => a.IsActive == true && a.IsDeleted == false && a.TableName == "PropertyContract").FirstOrDefault();
                if(systemPage!=null)
                {
                    var systemPageId = systemPage.Id;
                    var journalEntry = db.JournalEntries.Where(a => a.SourcePageId == systemPageId && a.SourceId == id).FirstOrDefault();
                    if(journalEntry!=null)
                    {
                        journalEntry.IsDeleted = true;
                        db.Entry(journalEntry).State = EntityState.Modified;
                    }
                    
                }
                foreach (var item in contract.PropertyContractBatches)
                {
                    item.IsDeleted = true;
                }
                foreach (var item in contract.PropertyContractReps)
                {
                    item.IsDeleted = true;
                }

                db.Entry(contract).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف العقود",
                    EnAction = "AddEdit",
                    ControllerName = "PropertyContract",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,

                });
                Notification.GetNotification("PropertyContract", "Delete", "Delete", id, null, "العقود");
                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

        }

        [SkipERPAuthorize]
        //public JsonResult SetDocNum()
        //{
        //    double DocNo = 0;
        //    var code = db.Database.SqlQuery<string>($"select top(1)[DocumentNumber] from [PropertyContract] order by [Id] desc");
        //    if (code.FirstOrDefault() == null)
        //    {
        //        DocNo = 0;
        //    }
        //    else
        //    {
        //        DocNo = double.Parse(code.FirstOrDefault().ToString());
        //    }
        //    return Json(DocNo + 1, JsonRequestBehavior.AllowGet);
        //}
        public JsonResult SetDocNum(int id, DateTime? VoucherDate)
        {
            bool IsExistInDocumentsCoding = false;
            var noOfDigits = (int?)0;
            var YearFormat = (int?)0;
            var CodingTypeId = (int?)0;
            var IsZerosFills = (bool?)null;
            var newDocNo = "";
            var GeneratedDocNo = "";
            var lastObj = db.PropertyContracts.Where(a => a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
            var lastDocNo = lastObj != null ? lastObj.DocumentNumber : "0";
            var DepartmentCode = db.Departments.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == id).FirstOrDefault().Code;
            DepartmentCode = double.Parse(DepartmentCode) < 10 ? "0" + DepartmentCode : DepartmentCode;
            var DepartmentDoc = db.DocumentsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id).FirstOrDefault();
            var MonthFormat = VoucherDate.Value.Month < 10 ? "0" + VoucherDate.Value.Month.ToString() : VoucherDate.Value.Month.ToString();
            if (DepartmentDoc == null)
            {
                DepartmentDoc = db.DocumentsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.AllDepartments == true).FirstOrDefault();
                if (DepartmentDoc == null)
                {
                    IsExistInDocumentsCoding = false;
                }
                else
                {
                    IsExistInDocumentsCoding = true;
                }
            }
            else
            {
                IsExistInDocumentsCoding = true;
            }
            if (IsExistInDocumentsCoding == true)
            {
                noOfDigits = DepartmentDoc.DigitsNo;
                YearFormat = DepartmentDoc.YearFormat;
                CodingTypeId = DepartmentDoc.CodingTypeId;
                IsZerosFills = DepartmentDoc.IsZerosFills;
                YearFormat = YearFormat == 2 ? int.Parse(VoucherDate.Value.Year.ToString().Substring(2, 2)) : int.Parse(VoucherDate.Value.Year.ToString());

                if (CodingTypeId == 1)//آلي
                {
                    if (lastDocNo.Contains("-"))
                    {
                        var ar = lastDocNo.Split('-');
                        if (IsZerosFills == true)
                        {
                            newDocNo = QueryHelper.FillsWithZeros(noOfDigits, (double.Parse(ar[3]) + 1).ToString());
                        }
                        else
                        {
                            newDocNo = (double.Parse(ar[3]) + 1).ToString();
                        }
                    }
                    else
                    {
                        if (IsZerosFills == true)
                        {
                            newDocNo = QueryHelper.FillsWithZeros(noOfDigits, (double.Parse(lastDocNo) + 1).ToString());
                        }
                        else
                        {
                            newDocNo = (double.Parse(lastDocNo) + 1).ToString();
                        }
                    }
                    GeneratedDocNo = DepartmentCode + "-" + YearFormat + "-" + MonthFormat + "-" + newDocNo;
                }
                else if (CodingTypeId == 2)//متصل شهري
                {
                    lastObj = db.PropertyContracts.Where(a => a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Month == VoucherDate.Value.Month && a.VoucherDate.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
                    if (lastObj != null)
                    {
                        if (lastObj.DocumentNumber.Contains("-"))
                        {
                            var ar = lastObj.DocumentNumber.Split('-');
                            if (double.Parse(ar[2]) == VoucherDate.Value.Month)
                            {
                                if (IsZerosFills == true)
                                {
                                    newDocNo = QueryHelper.FillsWithZeros(noOfDigits, (double.Parse(ar[3]) + 1).ToString());
                                }
                                else
                                {
                                    newDocNo = (double.Parse(ar[3]) + 1).ToString();
                                }
                            }
                            else
                            {
                                if (IsZerosFills == true)
                                {
                                    newDocNo = QueryHelper.FillsWithZeros(noOfDigits, "1").ToString();
                                }
                                else
                                {
                                    newDocNo = "1";
                                }
                            }
                        }
                        else
                        {
                            if (IsZerosFills == true)
                            {
                                newDocNo = QueryHelper.FillsWithZeros(noOfDigits, (double.Parse(lastObj.DocumentNumber) + 1).ToString()).ToString();
                            }
                            else
                            {
                                newDocNo = (double.Parse(lastObj.DocumentNumber) + 1).ToString();
                            }
                        }
                    }
                    else
                    {
                        if (IsZerosFills == true)
                        {
                            newDocNo = QueryHelper.FillsWithZeros(noOfDigits, "1").ToString();
                        }
                        else
                        {
                            newDocNo = "1";
                        }
                    }
                    GeneratedDocNo = DepartmentCode + "-" + YearFormat + "-" + MonthFormat + "-" + newDocNo;
                }
                else if (CodingTypeId == 3)//متصل سنوي
                {
                    lastObj = db.PropertyContracts.Where(a => a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

                    if (lastObj != null)
                    {
                        if (lastObj.DocumentNumber.Contains("-"))
                        {
                            var ar = lastObj.DocumentNumber.Split('-');
                            var VoucherDateFormate = int.Parse(ar[1]).ToString().Length == 2 ? int.Parse((VoucherDate.Value.Year.ToString()).Substring(2, 2)) : VoucherDate.Value.Year;
                            if (double.Parse(ar[1]) == VoucherDateFormate)
                            {
                                if (IsZerosFills == true)
                                {
                                    newDocNo = QueryHelper.FillsWithZeros(noOfDigits, (double.Parse(ar[3]) + 1).ToString());
                                }
                                else
                                {
                                    newDocNo = (double.Parse(ar[3]) + 1).ToString();
                                }
                            }
                            else
                            {
                                if (IsZerosFills == true)
                                {
                                    newDocNo = QueryHelper.FillsWithZeros(noOfDigits, "1").ToString();
                                }
                                else
                                {
                                    newDocNo = "1";
                                }
                            }
                        }
                        else
                        {
                            if (IsZerosFills == true)
                            {
                                newDocNo = QueryHelper.FillsWithZeros(noOfDigits, (double.Parse(lastObj.DocumentNumber) + 1).ToString()).ToString();
                            }
                            else
                            {
                                newDocNo = (double.Parse(lastObj.DocumentNumber) + 1).ToString();
                            }
                        }

                    }
                    else
                    {
                        if (IsZerosFills == true)
                        {
                            newDocNo = QueryHelper.FillsWithZeros(noOfDigits, "1").ToString();
                        }
                        else
                        {
                            newDocNo = "1";
                        }
                    }
                    GeneratedDocNo = DepartmentCode + "-" + YearFormat + "-" + MonthFormat + "-" + newDocNo;
                }
            }
            else
            {
                if (lastDocNo.Contains("-"))
                {
                    var ar = lastDocNo.Split('-');
                    newDocNo = (double.Parse(ar[3]) + 1).ToString();
                }
                else
                {
                    newDocNo = (double.Parse(lastDocNo) + 1).ToString();
                }
                GeneratedDocNo = newDocNo;
            }
            return Json(GeneratedDocNo, JsonRequestBehavior.AllowGet);

        }

        [SkipERPAuthorize]
        public JsonResult GetPropertyOwner(int? PropertyId)
        {
            var Owners = db.Properties.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == PropertyId).Select(a => new { Id = a.PropertyOwner.Id, ArName = a.PropertyOwner.Code + " - " + a.PropertyOwner.ArName }).ToList();
            var UnitType = db.PropertyDetails.Where(a => a.IsDeleted == false && a.MainDocId == PropertyId).Select(a => new { Id = a.PropertyUnitType.Id, ArName = a.PropertyUnitType.Code + " - " + a.PropertyUnitType.ArName }).Distinct();


            return Json(new { Owners, UnitType }, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult GetUnitByUnitTypeId(int? UnitTypeId, int? PropertyId , int? contractId)
        {
            var Unit = db.GetPropertyUnitsByPropertyAndUnitTypeId(PropertyId, UnitTypeId , contractId).ToList();
            return Json(Unit, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult CreateJournalEntry(int Id)
        {
            try
            {
                db.PropertyContractJE_Insert(Id);
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(false, JsonRequestBehavior.AllowGet);

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