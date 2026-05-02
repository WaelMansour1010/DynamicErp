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
using System.Threading.Tasks;
using MyERP.Repository;
using System.Web.Script.Serialization;

namespace MyERP.Controllers
{
    public class ManufacturingRequestController : ViewToStringController
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        public async Task<ActionResult> Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            var depIds = await departmentRepository.UserDepartmentsIds(userId);
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("ManufacturingRequest", "View", "Index", null, null, "اوامر البيع");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<ManufacturingRequest> manufacturingRequests;

            if (string.IsNullOrEmpty(searchWord))
            {
                manufacturingRequests = db.ManufacturingRequests.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId)).Include(s => s.Department).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = await db.ManufacturingRequests.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId)).CountAsync();
            }
            else
            {
                manufacturingRequests = db.ManufacturingRequests.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (s.DocumentNumber.Contains(searchWord) ||  s.Department.ArName.Contains(searchWord) || s.Employee.ArName.Contains(searchWord) || s.RequestDate.ToString().Contains(searchWord))).Include(s => s.Department).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = await db.ManufacturingRequests.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (s.DocumentNumber.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Employee.ArName.Contains(searchWord) || s.RequestDate.ToString().Contains(searchWord)||s.ItemGroup.ArName.Contains(searchWord))).CountAsync();
            }

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة اوامر البيع",
                EnAction = "Index",
                ControllerName = "ManufacturingRequest",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            return View(await manufacturingRequests.ToListAsync());
        }

        public async Task<ActionResult> AddEdit(int? id)
        {
            SystemSetting systemSetting = await db.SystemSettings.AnyAsync() ? await db.SystemSettings.FirstOrDefaultAsync() : new SystemSetting();
            ViewBag.ShowItemWidthAndHeightAndAreaInTransactions = systemSetting.ShowItemWidthAndHeightAndAreaInTransactions;

            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            ViewBag.ShowItemCost = userId == 1 ? true : await db.UserPrivileges.Where(x => x.PageAction.EnName == "ShowItemCost" && x.PageAction.PageId == 6199).Select(x => x.Privileged == true).FirstOrDefaultAsync();

            var chefs = await db.Employees.Where(d => d.IsActive == true && d.IsDeleted == false && d.IsChef).Select(b => new
            {
                b.Id,
                b.ArName
            }).ToListAsync();

            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            var departments = await departmentRepository.UserDepartments(1).ToListAsync();//show all deprartments so that user can select factory department
            ViewBag.LinkWithDocModalDepartmentId = new SelectList(departments, "Id", "ArName", systemSetting.DefaultDepartmentId);

            if (id == null)
            {
                ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", systemSetting.DefaultDepartmentId);
                ViewBag.ItemGroupId = new SelectList(db.ItemGroups.Where(x => x.IsActive == true && x.IsDeleted == false).Select(x => new { x.Id, x.ArName }), "Id", "ArName");
                ViewBag.EmployeeChefId = new SelectList(chefs, "Id", "ArName");

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
                ViewBag.RequestDate = cTime.ToString("yyyy-MM-ddTHH:mm");
                return View();
            }
            ManufacturingRequest manufacturingRequest = await db.ManufacturingRequests.FindAsync(id);
            if (manufacturingRequest == null)
            {
                return HttpNotFound();
            }
            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", manufacturingRequest.DepartmentId);

            ViewBag.EmployeeChefId = new SelectList(chefs, "Id", "ArName", manufacturingRequest.EmployeeChefId);

            ViewBag.ItemGroupId = new SelectList(db.ItemGroups.Where(x => x.IsActive == true && x.IsDeleted == false).Select(x => new { x.Id, x.ArName }), "Id", "ArName", manufacturingRequest.ItemGroupId);
            ViewBag.RequestDate = manufacturingRequest.RequestDate.ToString("yyyy-MM-ddTHH:mm");

            ViewBag.Next = QueryHelper.Next((int)id, "ManufacturingRequest");
            ViewBag.Previous = QueryHelper.Previous((int)id, "ManufacturingRequest");
            ViewBag.Last = QueryHelper.GetLast("ManufacturingRequest");
            ViewBag.First = QueryHelper.GetFirst("ManufacturingRequest");

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل اوامر البيع",
                EnAction = "AddEdit",
                ControllerName = "ManufacturingRequest",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = manufacturingRequest.Id,
                CodeOrDocNo = manufacturingRequest.DocumentNumber
            });
            return View(manufacturingRequest);
        }

        [HttpPost]
        public async Task<JsonResult> AddEdit(ManufacturingRequest manufacturingRequest)
        {
            int userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            manufacturingRequest.UserId = userId;
            if (ModelState.IsValid)
            {
                using (DbContextTransaction trans = db.Database.BeginTransaction())
                {
                    try
                    {
                        manufacturingRequest.IsDeleted = false;
                        manufacturingRequest.IsActive = true;
                        //List<ManufacturingRequestItem> manufacturingRequestItems = new List<ManufacturingRequestItem>();
                        //manufacturingRequestItems.AddRange(manufacturingRequest.ManufacturingRequestItems.Where(x => x.IsProcess1Component || x.IsProcess2).GroupBy(x => new { x.ItemId, x.ItemUnitId, x.IsProcess1Component, x.IsProcess2, x.WarehouseId }).Select(x => new ManufacturingRequestItem { ItemId = x.Key.ItemId, ItemUnitId = x.Key.ItemUnitId, IsProcess2 = x.Key.IsProcess2, IsProcess1Component = x.Key.IsProcess1Component, QTY = x.Sum(i => i.QTY), WarehouseId=x.Key.WarehouseId }));

                        //List<int> process1Items = manufacturingRequest.ManufacturingRequestItems.Where(x => x.IsProcess1).Select(x => x.ItemId).ToList();

                        //List<int> process2ComponentItems = manufacturingRequest.ManufacturingRequestItems.Where(x => x.IsProcess2Component).Select(x => x.ItemId).ToList();

                        //List<int> commonItems = process1Items.Intersect(process2ComponentItems).ToList();

                        //manufacturingRequestItems.AddRange(manufacturingRequest.ManufacturingRequestItems.Where(x => commonItems.Contains(x.ItemId)).GroupBy(x => new { x.ItemId, x.ItemUnitId, x.WarehouseId }).Select(x => new ManufacturingRequestItem { ItemId = x.Key.ItemId, ItemUnitId = x.Key.ItemUnitId, IsProcess1 = true, IsProcess2Component = true, QTY = x.Sum(i => i.QTY), WarehouseId=x.Key.WarehouseId }));

                        //manufacturingRequestItems.AddRange(manufacturingRequest.ManufacturingRequestItems.Where(x => !commonItems.Contains(x.ItemId) && process1Items.Contains(x.ItemId)).GroupBy(x => new { x.ItemId, x.ItemUnitId, x.WarehouseId }).Select(x => new ManufacturingRequestItem { ItemId = x.Key.ItemId, ItemUnitId = x.Key.ItemUnitId, IsProcess1 = true, QTY = x.Sum(i => i.QTY), WarehouseId=x.Key.WarehouseId }));

                        //manufacturingRequestItems.AddRange(manufacturingRequest.ManufacturingRequestItems.Where(x => !commonItems.Contains(x.ItemId) && process2ComponentItems.Contains(x.ItemId)).GroupBy(x => new { x.ItemId, x.ItemUnitId, x.WarehouseId }).Select(x => new ManufacturingRequestItem { ItemId = x.Key.ItemId, ItemUnitId = x.Key.ItemUnitId, IsProcess2Component = true, QTY = x.Sum(i => i.QTY), WarehouseId=x.Key.WarehouseId }));
                        //manufacturingRequestItems.ForEach(x =>
                        //{
                        //    x.ManufacturingRequestId = manufacturingRequest.Id;
                        //    x.IsDelivered = false;
                        //});
                        //manufacturingRequest.ManufacturingRequestItems = manufacturingRequestItems;
                        List<ManufacturingRequestItem> manufacturingRequestItems = manufacturingRequest.ManufacturingRequestItems.ToList();
                        manufacturingRequestItems.ForEach(x =>
                        {
                            x.ManufacturingRequestId = manufacturingRequest.Id;
                            x.IsDelivered = false;
                        });
                        if (manufacturingRequest.Id == 0)
                        {
                           // var docNo = QueryHelper.DocLastNum(manufacturingRequest.DepartmentId, "ManufacturingRequest");
                           // manufacturingRequest.DocumentNumber = ((docNo) + 1).ToString();
                            manufacturingRequest.DocumentNumber = new JavaScriptSerializer().Serialize(SetDocNum(manufacturingRequest.DepartmentId, manufacturingRequest.RequestDate).Data).ToString().Trim('"');

                            foreach (var manufacturingPurchaseRequest in manufacturingRequest.ManufacturingPurchaseRequests)
                            {
                                PurchaseRequest purchaseRequest = new PurchaseRequest { Id = manufacturingPurchaseRequest.PurchaseRequestId, DocumentNumber = "" };
                                db.PurchaseRequests.Attach(purchaseRequest);
                                purchaseRequest.StatusId = 3;
                                db.Entry(purchaseRequest).Property(x => x.StatusId).IsModified = true;
                            }
                            db.ManufacturingRequests.Add(manufacturingRequest);
                        }
                        else
                        {
                            db.ManufacturingRequestItems.RemoveRange(db.ManufacturingRequestItems.Where(x => x.ManufacturingRequestId == manufacturingRequest.Id));

                            db.ManufacturingRequestItems.AddRange(manufacturingRequest.ManufacturingRequestItems);
                            manufacturingRequest.ManufacturingRequestItems = null;
                            foreach (var manufacturingPurchaseRequest in manufacturingRequest.ManufacturingPurchaseRequests)
                            {
                                PurchaseRequest purchaseRequest = new PurchaseRequest { Id = manufacturingPurchaseRequest.PurchaseRequestId };
                                db.PurchaseRequests.Attach(purchaseRequest);
                                purchaseRequest.StatusId = 2;
                                db.Entry(purchaseRequest).Property(x => x.StatusId).IsModified = true;
                            }

                            db.ManufacturingPurchaseRequests.RemoveRange(db.ManufacturingPurchaseRequests.Where(x => x.ManufacturingRequestId == manufacturingRequest.Id));

                            List<ManufacturingPurchaseRequest> manufacturingPurchases = manufacturingRequest.ManufacturingPurchaseRequests.ToList();

                            foreach (var manufacturingPurchaseRequest in manufacturingPurchases)
                            {
                                PurchaseRequest purchaseRequest = new PurchaseRequest { Id = manufacturingPurchaseRequest.PurchaseRequestId, DocumentNumber = "" };
                                db.PurchaseRequests.Attach(purchaseRequest);
                                purchaseRequest.StatusId = 3;
                                db.Entry(purchaseRequest).Property(x => x.StatusId).IsModified = true;
                            }

                            manufacturingPurchases.ForEach(x => x.ManufacturingRequestId = manufacturingRequest.Id);
                            db.ManufacturingPurchaseRequests.AddRange(manufacturingPurchases);
                            manufacturingRequest.ManufacturingPurchaseRequests = null;
                            db.Entry(manufacturingRequest).State = EntityState.Modified;
                        }
                        await db.SaveChangesAsync();
                        trans.Commit();
                    }
                    catch (Exception)
                    {
                        trans.Rollback();
                        throw;
                    }
                }

                var id = manufacturingRequest.Id;

                Notification.GetNotification("ManufacturingRequest", "Add", "AddEdit", id, null, "طلبات التصنيع");

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = manufacturingRequest.Id > 0 ? "تعديل طلبات التصنيع " : "اضافة طلبات التصنيع",
                    EnAction = "AddEdit",
                    ControllerName = "ManufacturingRequest",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = manufacturingRequest.Id > 0 ? manufacturingRequest.Id : db.ManufacturingRequests.Max(i => i.Id),
                    CodeOrDocNo = manufacturingRequest.DocumentNumber
                });

                return Json(new { success = "true", id });
            }
            var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();

            return Json(new { success = "false", errors });
        }

        [SkipERPAuthorize]
        public JsonResult SetDocNum(int id, DateTime? VoucherDate)
        {
            bool IsExistInDocumentsCoding = false;
            var noOfDigits = (int?)0;
            var YearFormat = (int?)0;
            var CodingTypeId = (int?)0;
            var IsZerosFills = (bool?)null;
            var newDocNo = "";
            var GeneratedDocNo = "";
            var lastObj = db.ManufacturingRequests.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.ManufacturingRequests.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.RequestDate.Month == VoucherDate.Value.Month && a.RequestDate.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.ManufacturingRequests.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.RequestDate.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

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
            //var docNo = QueryHelper.DocLastNum(id, "ManufacturingRequest");
            //double i = (docNo) + 1;
            //return Json(i, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public async Task<ActionResult> PurchaseRequests(int? id, int itemGroupId)
        {
            var Prs = (await db.ManufacturingPurchaseRequests.Where(x => x.ManufacturingRequestId == id)
                .Select(x => new { x.PurchaseRequest.DocumentNumber, x.PurchaseRequest.Department.ArName, x.PurchaseRequest.Id, x.PurchaseRequest.VoucherDate, Checked = true })
                .Union(db.PurchaseRequests.Where(x => x.IsDeleted == false && x.IsApproved == true && x.ItemGroupId == itemGroupId && !x.ManufacturingPurchaseRequests.Any())
                .Select(x => new { x.DocumentNumber, x.Department.ArName, x.Id, x.VoucherDate, Checked = false })).OrderByDescending(x => x.Id)
                .ToListAsync()).Select(x => new PurchaseRequest { DocumentNumber = x.DocumentNumber, DepartmentArName = x.ArName, Id = x.Id, VoucherDate = x.VoucherDate, Checked = x.Checked }).Distinct();
            return PartialView(Prs);
        }

        [HttpPost]
        [SkipERPAuthorize]
        public async Task<JsonResult> GetPRItems(List<int> Ids, int departmentId)
        {
            MyXML.xPathName = "PRIds";
            string PRIdsXml = MyXML.GetXML(Ids.Select(x => new { Id = x }));
            List<PurchaseRequest_GetItems_Result> get_PRItems = await Task.Run(() => db.PurchaseRequest_GetItems(PRIdsXml, departmentId).ToList());
            string processItems = RenderRazorViewToString("GetPRItems", get_PRItems.Where(x => x.Component == 0));
            string componentItems = RenderRazorViewToString("GetPRItems", get_PRItems.Where(x => x.Component == 1));

            return Json(new { processItems, componentItems });
        }

        [HttpPost]
        public async Task<JsonResult> MakeManufacturingOrder(int manuRequestId)
        {
            var manufacturingRequest = await db.ManufacturingRequests.Where(x => x.Id == manuRequestId).Select(x => new { PurchaseRequestIds = x.ManufacturingPurchaseRequests.Select(p => p.PurchaseRequestId), ManufacturingOrdersCount = x.ManufacturingOrders.Count }).FirstOrDefaultAsync();
            if (manufacturingRequest.ManufacturingOrdersCount == 0)
            {
                using (DbContextTransaction transaction = db.Database.BeginTransaction())
                {
                    try
                    {
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
                        var employeeChefId = await db.ManufacturingRequests.Where(x => x.Id == manuRequestId).Select(x => x.EmployeeChefId).FirstOrDefaultAsync();
                        string lastDocNum = await db.ManufacturingOrders.Where(x => x.IsDeleted == false).OrderByDescending(x => x.Id).Select(x => x.DocumentNumber).FirstOrDefaultAsync();
                        string docNum = string.IsNullOrEmpty(lastDocNum) ? "1" : (double.Parse(lastDocNum) + 1).ToString();
                        ManufacturingOrder manufacturingOrder = new ManufacturingOrder()
                        {
                            OrderDate = cTime,
                            ManufacturingRequestId = manuRequestId,
                            ChefId = employeeChefId,
                            DocumentNumber = docNum,
                            IsDeleted = false,
                            IsActive = true,
                            IsAccepted=false,
                            IsCompleted=false,
                            UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value)
                        };
                        //var empid = await db.ERPUsers.Where(x => x.Id == manufacturingOrder.ChefUserId).Select(x => x.EmployeeId).FirstOrDefaultAsync();
                        manufacturingOrder.WarehouseId = await db.Warehouses.Where(x => x.ResponsibleEmpId == manufacturingOrder.ChefId).Select(x => x.Id).FirstOrDefaultAsync();
                        if (manufacturingOrder.WarehouseId == 0)
                            return Json("noWarehouse");
                        db.ManufacturingOrders.Add(manufacturingOrder);
                        foreach (var item in manufacturingRequest.PurchaseRequestIds)
                        {
                            PurchaseRequest purchaseRequest = new PurchaseRequest { Id = item, DocumentNumber = "" };
                            db.PurchaseRequests.Attach(purchaseRequest);
                            purchaseRequest.StatusId = 4;
                            db.Entry(purchaseRequest).Property(x => x.StatusId).IsModified = true;
                        }
                        await db.SaveChangesAsync();
                        transaction.Commit();
                        return Json(manufacturingOrder);
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }

            }
            else
                return Json("exists");
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            ManufacturingRequest ManufacturingRequest = db.ManufacturingRequests.Find(id);
            if (ManufacturingRequest.IsPosted == true)
            {
                return Content("false");
            }
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            ManufacturingRequest.IsDeleted = true;
            Random random = new Random();
            const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
            ManufacturingRequest.DocumentNumber = Code;
            db.Entry(ManufacturingRequest).State = EntityState.Modified;
            db.SaveChanges();
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = " حذف طلبات التصنيع",
                EnAction = "AddEdit",
                ControllerName = "ManufacturingRequest",
                UserName = User.Identity.Name,
                UserId = userId,
                LogDate = DateTime.Now,
                RequestMethod = "POST",
                SelectedItem = id,
                CodeOrDocNo = ManufacturingRequest.DocumentNumber
            });

            ////-------------------- Notification-------------------------////
            Notification.GetNotification("ManufacturingRequest", "Delete", "Delete", id, null, "طلبات التصنيع");

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