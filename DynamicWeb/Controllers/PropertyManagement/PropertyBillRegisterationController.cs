using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using MyERP.Models;
using MyERP.ViewModels;
using System.Security.Claims;
using Microsoft.Ajax.Utilities;
using DevExpress.DataProcessing;
using System.Data.Entity.Core.Objects;

namespace MyERP.Controllers.PropertyManagement
{
    public class PropertyBillRegisterationController : Controller
    {
        private readonly MySoftERPEntity db = new MySoftERPEntity();

        //
        // GET: Index
        public async Task<ActionResult> Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            ViewBag.PageIndex = pageIndex;
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة تسجيل فواتير ",
                EnAction = "Index",
                ControllerName = "PropertyBillRegisteration",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("PropertyBillRegisteration", "View", "Index", null, null, " تسجيل فواتير");

            //////////////-----------------------------------------------------------------------
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;


            IQueryable<PropertyBillRegisteration> billRegistrations;
            if (string.IsNullOrEmpty(searchWord))
            {
                billRegistrations = db.PropertyBillRegisterations.OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = await db.PropertyBillRegisterations.CountAsync();
            }
            else
            {
                billRegistrations = db.PropertyBillRegisterations.Where(s =>  s.PropertyContract.DocumentNumber.Contains(searchWord) || s.TransactionNumber.ToString().Contains(searchWord) /*|| s.Renters.ArName.Contains(searchWord)*/ ).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = await db.PropertyBillRegisterations.Where(s => s.PropertyContract.DocumentNumber.Contains(searchWord) || s.TransactionNumber.ToString().Contains(searchWord) /*|| s.Renters.ArName.Contains(searchWord)*/ ).CountAsync();
            }

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            //billRegistrations = await db.PropertyBillRegisterations.ToListAsync();

            ////// Map to ViewModel
            //var viewModelList = await billRegistrations.Select(b => new PropertyBillRegisterationViewModel
            //{
            //    Id = b.Id,
            //    TransactionNumber = b.TransactionNumber,
            //    BillRegDate = b.BillRegDate,
            //    ContractId = b.ContractId,
            //    RenterId = b.RenterId,
            //    PropertyDetailId = b.PropertyDetailId,
            //    RenterArName = db.PropertyRenters.Where(m => m.Id == b.RenterId).Select(pr => new
            //    {
            //        ArName = pr.Code + " - " + pr.ArName
            //    }).ToString(),
            //    PropertyArName = db.Properties.Where(m => m.Id == db.PropertyDetails.FirstOrDefault(pd => pd.Id == b.PropertyDetailId).MainDocId).Select(p=>new
            //    {
            //        ArName = p.Code + " - " + p.ArName
            //    }).ToString(),
            //    //PropertyArName = db.Properties.Include(oo => oo.PropertyDetails).Where(mm => mm.PropertyDetails.Any(p => p.Id == b.PropertyDetailId)).Select(p =>
            //    //new {
            //    //    ArName = p.Code + " - " + p.ArName
            //    //}).FirstOrDefault().ToString(), 
            //    PropertyUnitNo = db.PropertyDetails.Where(m => m.Id == b.PropertyDetailId).Select(pd=> new
            //    {
            //        UnitNo = pd.PropertyUnitNo
            //    }).ToString(),
            //    ContractDocumentNumber = db.PropertyContracts.Where(c => c.Id == b.ContractId).Select(pc => new 
            //    {
            //        DocNumber = pc.DocumentNumber
            //    }).ToString(),
            //    GasBillValue = b.GasBillValue,
            //    ElectricityBillValue = b.ElectricityBillValue,
            //    ViolationBillValue = b.ViolationBillValue
            //}).ToListAsync();
            var viewModelList = await billRegistrations.Select(b => new PropertyBillRegisterationViewModel
            {
                Id = b.Id,
                TransactionNumber = b.Id,
                BillRegDate = b.BillRegDate,
                ContractId = b.ContractId,
                RenterId = b.RenterId,
                PropertyDetailId = b.PropertyDetailId,
                RenterArName = db.PropertyRenters
                    .Where(m => m.Id == b.RenterId)
                    .Select(pr => pr.Code + " - " + pr.ArName)
                    .FirstOrDefault(),
                PropertyArName = db.Properties
                    .Where(m => m.Id == db.PropertyDetails
                        .Where(pd => pd.Id == b.PropertyDetailId)
                        .Select(pd => pd.MainDocId)
                        .FirstOrDefault())
                    .Select(p => p.Code + " - " + p.ArName)
                    .FirstOrDefault(),
                PropertyUnitNo = db.PropertyDetails
                    .Where(m => m.Id == b.PropertyDetailId)
                    .Select(pd => pd.PropertyUnitNo)
                    .FirstOrDefault().ToString(),
                ContractDocumentNumber = db.PropertyContracts
                    .Where(c => c.Id == b.ContractId)
                    .Select(pc => pc.DocumentNumber)
                    .FirstOrDefault(),
                GasBillValue = b.GasBillValue,
                ElectricityBillValue = b.ElectricityBillValue,
                ViolationBillValue = b.ViolationBillValue
            }).ToListAsync();


            return View(viewModelList);
        }

        // GET: AddEdit
        public async Task<ActionResult> AddEdit(int? id)
        {
            //------ Time Zone Depends On Currency --------//
            var Currency = db.Currencies.FirstOrDefault(a => a.IsActive == true && a.IsDeleted == false && a.IsDefault == true);
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
            //cTime = cTime.ToString("yyyy-MM-ddTHH:mm");
                
            var viewModel = new PropertyBillRegisterationViewModel
            {
                Contracts = new SelectList(await db.PropertyContracts
                .Where(c => c.IsDeleted == false).Select(b => new
                {
                    b.Id,
                    ArName = b.DocumentNumber + " - " + b.UnifiedContractNumber
                }).ToListAsync(), "Id", "ArName") ,
                BillRegDate = cTime
            };

            if (id == null) // Add operation
            {
                return View(viewModel);
            }

            // Edit operation
            var billRegisteration = await db.PropertyBillRegisterations.FindAsync(id);
            if (billRegisteration == null)
            {
                return HttpNotFound();
            }

            viewModel.Id = viewModel.TransactionNumber = billRegisteration.Id;
            viewModel.BillRegDate = billRegisteration.BillRegDate;
            viewModel.ContractId = billRegisteration.ContractId;
            viewModel.RenterId = billRegisteration.RenterId;
            var propId = viewModel.PropertyId = db.PropertyDetails.FirstOrDefault(pd => pd.Id == billRegisteration.PropertyDetailId).MainDocId;
            viewModel.PropertyDetailId = billRegisteration.PropertyDetailId;
            viewModel.ContractDocumentNumber = db.PropertyContracts.Where(c => c.Id == billRegisteration.ContractId).Select(pc => new
            {
                DocNumber = pc.DocumentNumber
            }).ToString();
            
            viewModel.PropertyArName = db.Properties.Where(m => m.Id == propId).Select(p => new
                                            {
                                                ArName = p.Code + " - " + p.ArName
                                            }).FirstOrDefault().ArName.ToString();

            viewModel.PropertyUnitNo = db.PropertyDetails.FirstOrDefault(pd => pd.Id == billRegisteration.PropertyDetailId).PropertyUnitNo.ToString();
            viewModel.RenterArName = db.PropertyRenters.Where(m => m.Id == billRegisteration.RenterId).Select(pr => new
            {
                ArName = pr.Code + " - " + pr.ArName
            }).FirstOrDefault().ArName.ToString();
            viewModel.GasBillValue = billRegisteration.GasBillValue;
            viewModel.ElectricityBillValue = billRegisteration.ElectricityBillValue;
            viewModel.ViolationBillValue = billRegisteration.ViolationBillValue;
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل تسجيل المصاريف",
                EnAction = "AddEdit",
                ControllerName = "PropertyBillRegisteration",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = billRegisteration.Id > 0 ? billRegisteration.Id : db.PropertyBillRegisterations.Max(i => i.Id),
                CodeOrDocNo = billRegisteration.Id.ToString()
            });

            return View(viewModel);
        }

        // POST: AddEdit
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<ActionResult> AddEdit(PropertyBillRegisteration propertyBillRegisteration)
        {
            /*--- Document Coding ---*/
            var DocumentCoding = "";
            SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
            if (systemSetting.DocumentCoding == true)
            {
                DocumentCoding = db.PropertyContracts
                .Where(c => c.IsDeleted == false && c.Id == propertyBillRegisteration.ContractId).FirstOrDefault().DocumentNumber;
            }
            DocumentCoding = DocumentCoding.Length > 0 ? DocumentCoding : null;
            /*-------**************** End Of Document Coding *****************--------*/
            if (ModelState.IsValid)
            {
                var id = propertyBillRegisteration.Id;

                var billRegisteration = new PropertyBillRegisteration
                {
                    Id = propertyBillRegisteration.Id,
                    TransactionNumber = propertyBillRegisteration.TransactionNumber = propertyBillRegisteration.Id,
                    BillRegDate = propertyBillRegisteration.BillRegDate,
                    ContractId = propertyBillRegisteration.ContractId,
                    RenterId = propertyBillRegisteration.RenterId,
                    PropertyDetailId = propertyBillRegisteration.PropertyDetailId,
                    GasBillValue = propertyBillRegisteration.GasBillValue,
                    ElectricityBillValue = propertyBillRegisteration.ElectricityBillValue,
                    ViolationBillValue = propertyBillRegisteration.ViolationBillValue
                };

                int userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                decimal RentersAccount =
                    (billRegisteration.ElectricityBillValue ?? 0m) +
                    (billRegisteration.GasBillValue ?? 0m) +
                    (billRegisteration.ViolationBillValue ?? 0m);
                var DepartmentId = db.PropertyContracts.Where(pc => pc.Id == propertyBillRegisteration.ContractId)
                            .FirstOrDefault().DepartmentId;
                if (billRegisteration.Id == 0) // Add operation
                {
                    var idResult = new ObjectParameter("Id", typeof(Int32));
                    try
                    {
                        db.PropertBillRegisteration_Insert(
                            idResult,
                            billRegisteration.BillRegDate,
                            billRegisteration.TransactionNumber,
                            billRegisteration.ContractId,
                            billRegisteration.RenterId,
                            billRegisteration.PropertyDetailId,
                            billRegisteration.ElectricityBillValue,
                            billRegisteration.GasBillValue,
                            billRegisteration.ViolationBillValue,
                            RentersAccount,
                            userId,
                            DepartmentId);

                        id = (int)idResult.Value;
                    }
                    catch (Exception ex)
                    {
                    }
                    Notification.GetNotification("PropertyBillRegisteration", "Add", "AddEdit", propertyBillRegisteration.Id, null, "تسجيل المصاريف");

                }
                else // Edit operation
                {
                    try
                    {
                        db.PropertBillRegisteration_Update(
                            billRegisteration.Id,
                            billRegisteration.BillRegDate,
                            billRegisteration.TransactionNumber,
                            billRegisteration.ContractId,
                            billRegisteration.RenterId,
                            billRegisteration.PropertyDetailId,
                            billRegisteration.ElectricityBillValue,
                            billRegisteration.GasBillValue,
                            billRegisteration.ViolationBillValue,
                            RentersAccount,
                            userId,
                            DepartmentId);
                    }
                    catch (Exception ex)
                    {

                    }
                    Notification.GetNotification("PropertyBillRegisteration", "Edit", "AddEdit", propertyBillRegisteration.Id, null, "تسجيل المصاريف");

                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = propertyBillRegisteration.Id > 0 ? "تعديل تسجيل المصاريف" : "اضافة تسجيل المصاريف",
                    EnAction = "AddEdit",
                    ControllerName = "PropertyBillRegisteration",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id
                });
                return Json(new { success = "true", id });
            }
            else
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();

                //------ Time Zone Depends On Currency --------//
                var Currency = db.Currencies.FirstOrDefault(a => a.IsActive == true && a.IsDeleted == false && a.IsDefault == true);
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
                //cTime = cTime.ToString("yyyy-MM-ddTHH:mm");

                var viewModel = new PropertyBillRegisterationViewModel
                {
                    Contracts = new SelectList(await db.PropertyContracts
                    .Where(c => c.IsDeleted == false).Select(b => new
                    {
                        b.Id,
                        ArName = b.DocumentNumber + " - " + b.UnifiedContractNumber
                    }).ToListAsync(), "Id", "ArName"),
                    BillRegDate = cTime
                };
                return View(viewModel);
            }
        }

        
        // POST: Delete
        [HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            var billRegisteration = await db.PropertyBillRegisterations.FindAsync(id);
            if (billRegisteration == null)
            {
                return HttpNotFound();
            }

            db.PropertyBillRegisterations.Remove(billRegisteration);
            await db.SaveChangesAsync();
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "حذف تسجيل المصاريف",
                EnAction = "AddEdit",
                ControllerName = "PropertyBillRegisteration",
                UserName = User.Identity.Name,
                UserId = userId,
                LogDate = DateTime.Now,
                RequestMethod = "POST",
                SelectedItem = billRegisteration.Id
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("PropertyBillRegisteration", "Delete", "Delete", id, null, "تسجيل المصاريف");
            ///////////////-----------------------------------------------------------------------
            return Content("true");
        }
        

        

        [SkipERPAuthorize]
        public JsonResult GetRenterPerContract(int contractId)
        {
            var PropertyRenterId = db.PropertyContracts.Where(pc=>pc.Id== contractId).FirstOrDefault().PropertyRenterId;
            var PropertyRenterName = db.PropertyContracts.Where(pc => pc.Id == contractId).FirstOrDefault().PropertyRenter.ArName;

            return Json(new { id = PropertyRenterId, value = PropertyRenterName }, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult GetPropertyPerContract(int contractId)
        {
            var PropertyId = db.PropertyContracts.Where(pc => pc.Id == contractId).FirstOrDefault().PropertyId;
            var PropertyName = db.PropertyContracts.Where(pc => pc.Id == contractId).FirstOrDefault().Property.ArName;

            return Json(new { id= PropertyId, value = PropertyName }, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult GetPropertyUnitPerContract(int contractId)
        {
            var PropertyDetailId = db.PropertyContracts.Where(pc => pc.Id == contractId && pc.IsDeleted == false).FirstOrDefault()?.PropertyUnitId;
            var PropertyUnitNo = db.PropertyDetails.Where(pd => pd.Id == PropertyDetailId).FirstOrDefault()?.PropertyUnitNo;

            return Json(new {id= PropertyDetailId, value = PropertyUnitNo }, JsonRequestBehavior.AllowGet);
        }
        
        [SkipERPAuthorize]
        public JsonResult getJournalEntry(string billRegisterationId)
        {
            int id = int.Parse(billRegisterationId);
            int sysPageId = QueryHelper.SourcePageId("PropertyBillRegisteration");
            var JE = db.GetJournalEntryBySourceIdAndSourcePageId(int.Parse(billRegisterationId), sysPageId).FirstOrDefault();
            var JEDepartment = db.Departments.Where(d => d.Id == JE.DepartmentId).FirstOrDefault().ArName;
            if (JE != null)
            {
                var JEDocNo = (JE.DocumentNumber.IsNullOrWhiteSpace() == false) ? JE.DocumentNumber.Replace("-", "") : "";
                //JE.DocumentNumber = JE.DocumentNumber.Replace("-", "");
                int JEId = JE.Id;
                int JESourcePageId = JE.SourcePageId ?? 0;                
                DateTime JEDate = JE.Date.GetValueOrDefault();
                var JENotes = JE.Notes;
                var JEDetails = db.JournalEntryDetails.Where(a => a.SourceId == id && a.JournalEntryId == JEId && a.SourcePageId == JESourcePageId)
                    .OrderBy(a => a.Id)
                    .Select(a => new
                    {
                        a.Id,
                        a.Debit,
                        a.Credit,
                        a.ChartOfAccount.ArName
                    }).ToList();
                
                return Json(
                    new{ 
                        success = "true" ,
                        JEDocNo,
                        JEDate,
                        JEId,
                        JESourcePageId,
                        JEDepartment,
                        JENotes,
                        JEDetails
                    }, JsonRequestBehavior.AllowGet);
            }
            else 
            {
                return Json(
                    new
                    {
                        success = "false"
                    }, JsonRequestBehavior.AllowGet);
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
