using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using System.Net;
using System.Data.Entity.Core.Objects;
using MyERP.Repository;

namespace MyERP.Controllers
{
    public class PurchaseRequestController : ViewToStringController
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: PurchaseRequest
        public async Task<ActionResult> Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            var depIds = await departmentRepository.UserDepartmentsIds(userId);
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح طلب شراء",
                EnAction = "Index",
                ControllerName = "PurchaseRequest",
                UserName = User.Identity.Name,
                UserId = userId,
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("PurchaseRequest", "View", "Index", null, null, "الاصناف");
            ///////-----------------------------------------------------------------------

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<PurchaseRequest> PurchaseRequestsList;
            if (string.IsNullOrEmpty(searchWord))
            {
                PurchaseRequestsList = db.PurchaseRequests.Where(c => c.IsDeleted == false && depIds.Contains(c.DepartmentId)).OrderByDescending(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.PurchaseRequests.Where(c => c.IsDeleted == false && depIds.Contains(c.DepartmentId)).Count();
            }
            else
            {
                PurchaseRequestsList = db.PurchaseRequests.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (s.DocumentNumber.Contains(searchWord) || s.Branch.ArName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.PurchaseRequestStatu.ArName.Contains(searchWord) || s.ItemGroup.ArName.Contains(searchWord) || s.Warehouse.ArName.Contains(searchWord) || s.VoucherDate.ToString().Contains(searchWord) || s.ApprovalDate.ToString().Contains(searchWord))).OrderByDescending(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.PurchaseRequests.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (s.DocumentNumber.Contains(searchWord) || s.Branch.ArName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.PurchaseRequestStatu.ArName.Contains(searchWord) || s.ItemGroup.ArName.Contains(searchWord) || s.Warehouse.ArName.Contains(searchWord) || s.VoucherDate.ToString().Contains(searchWord) || s.ApprovalDate.ToString().Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(PurchaseRequestsList.ToList());
        }

        // GET: PurchaseRequest/Edit/5
        public async Task<ActionResult> AddEdit(int? id)
        {
            var projectName = System.Web.Configuration.WebConfigurationManager.AppSettings["ProjectName"];
            if (projectName != "Genoise")
             return  RedirectToAction("AddEdit2/"+id);
            SystemSetting systemSetting = await db.SystemSettings.AnyAsync() ? await db.SystemSettings.FirstOrDefaultAsync() : new SystemSetting();
            ViewBag.ShowSerialNumbers = systemSetting.ShowSerialNumbers == true;
            ViewBag.PayViaCarryOver = systemSetting.PayViaCarryOver == true;
            ViewBag.PayViaCash = systemSetting.PayViaCash == true;
            ViewBag.PayViaCheque = systemSetting.PayViaCheque == true;
            ViewBag.PayViaVisa = systemSetting.PayViaVisa == true;
            //ViewBag.ServiceFeesPercentage = systemSetting.ServiceFeesPercentage.HasValue ? systemSetting.ServiceFeesPercentage : 0;
            ViewBag.PurchaseRequestId = id.HasValue ? id.Value : 0;
            ViewBag.HasManufacturingPurchase = false;
            
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            WarehouseRepository warehouseRepository = new WarehouseRepository(db);

            if (id == null)
            {
                ViewBag.StatusId = 1;
                ViewBag.IsApproved = false;

                ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", systemSetting.DefaultDepartmentId);
                ViewBag.WarehouseId = new SelectList(warehouseRepository.UserWarehouses(userId, systemSetting.DefaultDepartmentId), "Id", "ArName", systemSetting.DefaultWarehouseId);

                ViewBag.VendorOrCustomerId = new SelectList(db.Vendors.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
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
                return View(await db.ItemGroups.Where(x => x.IsActive == true && x.IsDeleted == false).Include(x => x.Items).ToListAsync());
            }
            PurchaseRequest purchaseRequest = await db.PurchaseRequests.FindAsync(id);
            if (purchaseRequest == null)
                return HttpNotFound();

            ViewBag.Next = QueryHelper.Next((int)id, "PurchaseRequest");
            ViewBag.Previous = QueryHelper.Previous((int)id, "PurchaseRequest");
            ViewBag.Last = QueryHelper.GetLast("PurchaseRequest");
            ViewBag.First = QueryHelper.GetFirst("PurchaseRequest");
            ViewBag.HasManufacturingPurchase = purchaseRequest.ManufacturingPurchaseRequests.Any();
            ViewBag.StatusId = purchaseRequest.StatusId;
            ViewBag.IsApproved = purchaseRequest.IsApproved == true;
            ViewBag.ApprovalDate = purchaseRequest.ApprovalDate;

            ViewBag.VendorOrCustomerId = new SelectList(db.Vendors.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", purchaseRequest.VendorOrCustomerId);

            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", purchaseRequest.DepartmentId);
            ViewBag.WarehouseId = new SelectList(warehouseRepository.UserWarehouses(userId, purchaseRequest.DepartmentId), "Id", "ArName", purchaseRequest.WarehouseId);
            ViewBag.ItemGroupId = purchaseRequest.ItemGroupId;
            ViewBag.VoucherDate = purchaseRequest.VoucherDate.ToString("yyyy-MM-ddTHH:mm");
            ViewBag.DocumentNumber = purchaseRequest.DocumentNumber;

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل طلب الشراء ",
                EnAction = "AddEdit",
                ControllerName = "PurchaseRequest",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = purchaseRequest.Id,
                CodeOrDocNo = purchaseRequest.DocumentNumber
            });
            return View(await db.ItemGroups.Where(x => x.IsActive == true && x.IsDeleted == false).Include(x => x.Items).ToListAsync());
        }

        // POST: PurchaseRequest/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public JsonResult AddEdit(PurchaseRequest purchaseRequest)
        {
            int userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            purchaseRequest.UserId = userId;
            if (ModelState.IsValid)
            {
                var id = purchaseRequest.Id;
                purchaseRequest.IsDeleted = false;
                if (purchaseRequest.IsApproved == true)
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

                    purchaseRequest.ApprovalDate = cTime;
                    purchaseRequest.ApprovalUserId = userId;
                }
                if (purchaseRequest.Id > 0)
                {
                    if (db.PurchaseRequests.Find(purchaseRequest.Id).IsPosted == true)
                    {
                        return Json(new { success = "false" });
                    }
                    MyXML.xPathName = "Details";
                    var PurchaseRequestDetails = MyXML.GetXML(purchaseRequest.PurchaseRequestDetails);

                    db.PurchaseRequest_Update(purchaseRequest.Id, purchaseRequest.BranchId, purchaseRequest.WarehouseId, purchaseRequest.DepartmentId, purchaseRequest.VoucherDate, purchaseRequest.VendorOrCustomerId, purchaseRequest.CurrencyId, purchaseRequest.CurrencyEquivalent, purchaseRequest.Total, purchaseRequest.ValidityPeriod, purchaseRequest.DeliveryPeriod, purchaseRequest.SystemPageId, purchaseRequest.SelectedId, purchaseRequest.IsDelivered, purchaseRequest.IsAccepted, purchaseRequest.IsLinked, purchaseRequest.IsCompleted, false, userId, purchaseRequest.IsActive, purchaseRequest.IsDeleted, purchaseRequest.AutoCreated, purchaseRequest.Notes, purchaseRequest.Image, 1, purchaseRequest.IsApproved, purchaseRequest.ApprovalDate, purchaseRequest.ApprovalUserId, purchaseRequest.ItemGroupId, PurchaseRequestDetails);

                    Notification.GetNotification("PurchaseRequest", "Edit", "AddEdit", id, null, "طلب شراء");
                }
                else
                {
                    purchaseRequest.IsActive = true;

                    MyXML.xPathName = "Details";
                    var PurchaseRequestDetails = MyXML.GetXML(purchaseRequest.PurchaseRequestDetails);

                    var idResult = new ObjectParameter("Id", typeof(Int32));
                    db.PurchaseRequest_Insert(idResult, purchaseRequest.BranchId, purchaseRequest.WarehouseId, purchaseRequest.DepartmentId, purchaseRequest.VoucherDate, purchaseRequest.VendorOrCustomerId, purchaseRequest.CurrencyId, purchaseRequest.CurrencyEquivalent, purchaseRequest.Total, purchaseRequest.ValidityPeriod, purchaseRequest.DeliveryPeriod, purchaseRequest.SystemPageId, purchaseRequest.SelectedId, purchaseRequest.IsDelivered, purchaseRequest.IsAccepted, purchaseRequest.IsLinked, purchaseRequest.IsCompleted, false, userId, purchaseRequest.IsActive, purchaseRequest.IsDeleted, purchaseRequest.AutoCreated, purchaseRequest.Notes, purchaseRequest.Image, 1, purchaseRequest.IsApproved, purchaseRequest.ApprovalDate, purchaseRequest.ApprovalUserId, purchaseRequest.ItemGroupId, PurchaseRequestDetails);
                    id = (int)idResult.Value;

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("PurchaseRequest", "Add", "AddEdit", id, null, "طلب شراء");
                }

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = purchaseRequest.Id > 0 ? "تعديل طلب شراء " : "اضافة طلب شراء",
                    EnAction = "AddEdit",
                    ControllerName = "PurchaseRequest",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = purchaseRequest.Id > 0 ? purchaseRequest.Id : db.PurchaseRequests.Max(i => i.Id),
                    CodeOrDocNo = purchaseRequest.DocumentNumber
                });

                return Json(new { success = "true", id });
            }
            var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();

            return Json(new { success = "false", errors });
        }

        public async Task<JsonResult> PurchaseRequestDetails(int id, bool ForView = false)
        {
            var purchaseRequest = await db.PurchaseRequests.Where(x => x.Id == id).OrderByDescending(x => x.Id).Select(x => new { x.Id, x.Total, x.WarehouseId, x.DepartmentId, x.VoucherDate, x.DocumentNumber, x.IsLinked, x.SelectedId, x.SystemPageId }).FirstOrDefaultAsync();

            var view = RenderRazorViewToString(ForView ? "PurchaseRequestView" : "PurchaseRequestAvailability", await db.PurchaseRequestDetails.Where(x => x.MainDocId == id).ToListAsync());
            return Json(new { view, purchaseRequest, VoucherDate = purchaseRequest.VoucherDate.ToString("yyyy-MM-ddTHH:mm") }, JsonRequestBehavior.AllowGet);
        }



        //---------------------- AddEdit Normal ---------------------//
        public ActionResult AddEdit2(int? id)
        {
            SystemSetting systemSetting =  db.SystemSettings.Any() ?  db.SystemSettings.FirstOrDefault() : new SystemSetting();
            ViewBag.UseExpiryDateForItems = systemSetting.UseExpiryDateForItems;
            ViewBag.ShowSerialNumbers = systemSetting.ShowSerialNumbers == true;
            ViewBag.PayViaCarryOver = systemSetting.PayViaCarryOver == true;
            ViewBag.PayViaCash = systemSetting.PayViaCash == true;
            ViewBag.PayViaCheque = systemSetting.PayViaCheque == true;
            ViewBag.PayViaVisa = systemSetting.PayViaVisa == true;
            //ViewBag.ServiceFeesPercentage = systemSetting.ServiceFeesPercentage.HasValue ? systemSetting.ServiceFeesPercentage : 0;
            ViewBag.PurchaseRequestId = id.HasValue ? id.Value : 0;
            ViewBag.HasManufacturingPurchase = false;
            ViewBag.ShowItemWidthAndHeightAndAreaInTransactions = systemSetting.ShowItemWidthAndHeightAndAreaInTransactions;
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            WarehouseRepository warehouseRepository = new WarehouseRepository(db);

            UserRepository userRepository = new UserRepository(db);
            int roleId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("RoleId").Value);
            var CanChangeItemPrice = userId == 1 ? true : db.UserPrivileges.Where(u => u.PageAction.PageId == 58 && u.PageAction.EnName == "ChangeItemPrice" && u.PageAction.Action == "ChangeItemPrice" && u.UserId == userId).Select(x => x.Privileged).FirstOrDefault();
            CanChangeItemPrice = CanChangeItemPrice ?? db.RolePrivileges.Where(u => u.PageAction.PageId == 58 && u.PageAction.EnName == "ChangeItemPrice" && u.PageAction.Action == "ChangeItemPrice" && u.RoleId == roleId).Select(x => x.Privileged).FirstOrDefault();
            ViewBag.CanChangeItemPrice = CanChangeItemPrice;
            // ViewBag.CanChangeItemPrice =  userRepository.HasActionPrivilege(userId, "ChangeItemPrice", "ChangeItemPrice");

            if (id == null)
            {
                ViewBag.StatusId = 1;
                ViewBag.IsApproved = false;

                ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", systemSetting.DefaultDepartmentId);
                ViewBag.WarehouseId = new SelectList(warehouseRepository.UserWarehouses(userId, systemSetting.DefaultDepartmentId), "Id", "ArName", systemSetting.DefaultWarehouseId);

                ViewBag.VendorOrCustomerId = new SelectList(db.Vendors.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.ItemGroupId = new SelectList(db.ItemGroups.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
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
                // return View( db.ItemGroups.Where(x => x.IsActive == true && x.IsDeleted == false).Include(x => x.Items).ToListAsync());
                return View();
            }
            PurchaseRequest purchaseRequest =  db.PurchaseRequests.Find(id);
            if (purchaseRequest == null)
                return HttpNotFound();

            ViewBag.Next = QueryHelper.Next((int)id, "PurchaseRequest");
            ViewBag.Previous = QueryHelper.Previous((int)id, "PurchaseRequest");
            ViewBag.Last = QueryHelper.GetLast("PurchaseRequest");
            ViewBag.First = QueryHelper.GetFirst("PurchaseRequest");
            ViewBag.HasManufacturingPurchase = purchaseRequest.ManufacturingPurchaseRequests.Any();
            ViewBag.StatusId = purchaseRequest.StatusId;
            ViewBag.IsApproved = purchaseRequest.IsApproved == true;
            ViewBag.ApprovalDate = purchaseRequest.ApprovalDate;

            ViewBag.VendorOrCustomerId = new SelectList(db.Vendors.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", purchaseRequest.VendorOrCustomerId);

            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", purchaseRequest.DepartmentId);
            ViewBag.WarehouseId = new SelectList(warehouseRepository.UserWarehouses(userId, purchaseRequest.DepartmentId), "Id", "ArName", purchaseRequest.WarehouseId);
            ViewBag.ItemGroupId = new SelectList(db.ItemGroups.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", purchaseRequest.ItemGroupId);
           // ViewBag.ItemGroupId = purchaseRequest.ItemGroupId;
            ViewBag.VoucherDate = purchaseRequest.VoucherDate.ToString("yyyy-MM-ddTHH:mm");
            ViewBag.DocumentNumber = purchaseRequest.DocumentNumber;

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل فاتورة البيع",
                EnAction = "AddEdit",
                ControllerName = "PurchaseRequest",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = purchaseRequest.Id,
                CodeOrDocNo = purchaseRequest.DocumentNumber
            });
            //  return View( db.ItemGroups.Where(x => x.IsActive == true && x.IsDeleted == false).Include(x => x.Items).ToList());
            return View(purchaseRequest);
        }

       
        [HttpPost]
        public JsonResult AddEdit2(PurchaseRequest purchaseRequest)
        {
            int userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            purchaseRequest.UserId = userId;
            if (ModelState.IsValid)
            {
                var id = purchaseRequest.Id;
                purchaseRequest.IsDeleted = false;
                if (purchaseRequest.IsApproved == true)
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

                    purchaseRequest.ApprovalDate = cTime;
                    purchaseRequest.ApprovalUserId = userId;
                }
                if (purchaseRequest.Id > 0)
                {
                    if (db.PurchaseRequests.Find(purchaseRequest.Id).IsPosted == true)
                    {
                        return Json(new { success = "false" });
                    }
                    MyXML.xPathName = "Details";
                    var PurchaseRequestDetails = MyXML.GetXML(purchaseRequest.PurchaseRequestDetails);

                    db.PurchaseRequest_Update(purchaseRequest.Id, purchaseRequest.BranchId, purchaseRequest.WarehouseId, purchaseRequest.DepartmentId, purchaseRequest.VoucherDate, purchaseRequest.VendorOrCustomerId, purchaseRequest.CurrencyId, purchaseRequest.CurrencyEquivalent, purchaseRequest.Total, purchaseRequest.ValidityPeriod, purchaseRequest.DeliveryPeriod, purchaseRequest.SystemPageId, purchaseRequest.SelectedId, purchaseRequest.IsDelivered, purchaseRequest.IsAccepted, purchaseRequest.IsLinked, purchaseRequest.IsCompleted, false, userId, purchaseRequest.IsActive, purchaseRequest.IsDeleted, purchaseRequest.AutoCreated, purchaseRequest.Notes, purchaseRequest.Image, 1, purchaseRequest.IsApproved, purchaseRequest.ApprovalDate, purchaseRequest.ApprovalUserId, purchaseRequest.ItemGroupId, PurchaseRequestDetails);

                    Notification.GetNotification("PurchaseRequest", "Edit", "AddEdit", id, null, "طلب شراء");
                }
                else
                {
                    purchaseRequest.IsActive = true;

                    MyXML.xPathName = "Details";
                    var PurchaseRequestDetails = MyXML.GetXML(purchaseRequest.PurchaseRequestDetails);

                    var idResult = new ObjectParameter("Id", typeof(Int32));
                    db.PurchaseRequest_Insert(idResult, purchaseRequest.BranchId, purchaseRequest.WarehouseId, purchaseRequest.DepartmentId, purchaseRequest.VoucherDate, purchaseRequest.VendorOrCustomerId, purchaseRequest.CurrencyId, purchaseRequest.CurrencyEquivalent, purchaseRequest.Total, purchaseRequest.ValidityPeriod, purchaseRequest.DeliveryPeriod, purchaseRequest.SystemPageId, purchaseRequest.SelectedId, purchaseRequest.IsDelivered, purchaseRequest.IsAccepted, purchaseRequest.IsLinked, purchaseRequest.IsCompleted, false, userId, purchaseRequest.IsActive, purchaseRequest.IsDeleted, purchaseRequest.AutoCreated, purchaseRequest.Notes, purchaseRequest.Image, 1, purchaseRequest.IsApproved, purchaseRequest.ApprovalDate, purchaseRequest.ApprovalUserId, purchaseRequest.ItemGroupId, PurchaseRequestDetails);
                    id = (int)idResult.Value;

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("PurchaseRequest", "Add", "AddEdit", id, null, "طلب شراء");
                }

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = purchaseRequest.Id > 0 ? "تعديل طلب شراء " : "اضافة طلب شراء",
                    EnAction = "AddEdit",
                    ControllerName = "PurchaseRequest",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = purchaseRequest.Id > 0 ? purchaseRequest.Id : db.PurchaseRequests.Max(i => i.Id),
                    CodeOrDocNo = purchaseRequest.DocumentNumber
                });

                return Json(new { success = "true", id });
            }
            var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();

            return Json(new { success = "false", errors });
        }
//-------------------- **************************** ----------------------------------//

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
            var lastObj = db.PurchaseRequests.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.PurchaseRequests.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Month == VoucherDate.Value.Month && a.VoucherDate.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.PurchaseRequests.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

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

            //var docNo = QueryHelper.DocLastNum(id, "PurchaseRequest");
            //double i = (docNo) + 1;
            //return Json(i, JsonRequestBehavior.AllowGet);
        }

        // POST: PurchaseRequest/Delete/5
        [HttpPost, ActionName("Delete")]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            if (await db.ManufacturingPurchaseRequests.Where(x => x.PurchaseRequestId == id).AnyAsync())
                return Content("false");

            PurchaseRequest PurchaseRequest = db.PurchaseRequests.Find(id);
            PurchaseRequest.IsDeleted = true;
            PurchaseRequest.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            Random random = new Random();
            const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
            PurchaseRequest.DocumentNumber = Code;
            db.Entry(PurchaseRequest).State = EntityState.Modified;

            db.SaveChanges();
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "حذف طلب شراء",
                EnAction = "AddEdit",
                ControllerName = "PurchaseRequest",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "POST",
                SelectedItem = id,
                CodeOrDocNo = PurchaseRequest.DocumentNumber
            });

            ////-------------------- Notification-------------------------////
            Notification.GetNotification("PurchaseRequest", "Delete", "Delete", id, null, "الاصناف");

            ///////////-----------------------------------------------------------------------

            return Content("true");
        }

        [HttpPost]
        public JsonResult ActivateDeactivate(int id)
        {
            PurchaseRequest purchaseRequest = db.PurchaseRequests.Find(id);
            if (purchaseRequest.ManufacturingPurchaseRequests.Any())
            {
                return Json("hasManufacturingRequests");
            }
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

            if (purchaseRequest.IsApproved != true)
            {
                purchaseRequest.IsApproved = true;
                purchaseRequest.ApprovalUserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                purchaseRequest.ApprovalDate = cTime;
                purchaseRequest.StatusId = 2;
            }
            else
            {
                purchaseRequest.IsApproved = false;
                purchaseRequest.ApprovalUserId = null;
                purchaseRequest.ApprovalDate = null;
                purchaseRequest.StatusId = 1;
            }

            db.Entry(purchaseRequest).State = EntityState.Modified;

            db.SaveChanges();
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = (bool)purchaseRequest.IsApproved ? "اعتماد طلب شراء" : "إلغاء اعتماد طلب شراء",
                EnAction = "AddEdit",
                ControllerName = "PurchaseRequest",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "POST",
                SelectedItem = purchaseRequest.Id,
                CodeOrDocNo = purchaseRequest.DocumentNumber
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("PurchaseRequest", "Activate/Deactivate", "ActivateDeactivate", id, true, "طلب شراء");
            return Json(purchaseRequest.IsApproved == true ? "approved" : "notApproved");
        }

        public async Task<ActionResult> History()
        {
            var purchaseRequests = db.PurchaseRequests.Where(x => x.IsDeleted == false).Select(x => new
            {
                x.Id,
                x.ApprovalDate,
                ApprovalUserName = db.ERPUsers.Where(u => u.Id == x.ApprovalUserId).Select(u => u.Name).FirstOrDefault(),
                x.ApprovalUserId,
                x.DocumentNumber,
                x.VoucherDate,
                x.ERPUser.Name,
                x.IsApproved,
                ManufacturingRequests = x.ManufacturingPurchaseRequests.Select(m => new
                {
                    m.ManufacturingRequest.DocumentNumber,
                    ManufacturingOrders = m.ManufacturingRequest.ManufacturingOrders.Select(o => new
                    {
                        o.CompletionDate,
                        o.AcceptanceDate,
                        o.IsCompleted,
                        o.IsAccepted,
                        o.DocumentNumber,
                        o.OrderDate,
                        CreatedByUser = db.ERPUsers.Where(e => e.Id == o.UserId).Select(e => e.Name).FirstOrDefault(),
                        StockIssueDocNum = db.StockIssueVouchers.Where(s => s.SelectedId == o.Id && s.SystemPageId == 6200 && s.IsDeleted == false).Select(s => s.DocumentNumber),
                        StockReceiptDocNum = db.StockReceiptVouchers.Where(s => s.SelectedId == o.Id && s.SystemPageId == 6200 && s.IsDeleted == false).Select(s => s.DocumentNumber)
                    }),
                    m.ManufacturingRequest.RequestDate,
                })
            }).OrderByDescending(x => x.Id);
            return View((await purchaseRequests.ToListAsync()).Select(x => new PurchaseRequestHistory
            {
                ApprovalDate = x.ApprovalDate,
                ApprovalUserName = x.ApprovalUserName,
                ApprovalUserId = x.ApprovalUserId,
                DocumentNumber = x.DocumentNumber,
                VoucherDate = x.VoucherDate,
                User = x.Name,
                IsApproved = x.IsApproved == true,
                ManufacturingRequests = x.ManufacturingRequests.Select(r => new ManufacturingRequest
                {
                    DocumentNumber = r.DocumentNumber,
                    RequestDate = r.RequestDate,
                    ManufacturingOrders = r.ManufacturingOrders.Select(o => new ManufacturingOrder
                    {
                        DocumentNumber = o.DocumentNumber,
                        CompletionDate = o.CompletionDate,
                        AcceptanceDate = o.AcceptanceDate,
                        IsCompleted = o.IsCompleted,
                        IsAccepted = o.IsAccepted,
                        OrderDate = o.OrderDate,
                        CreatedByUser = o.CreatedByUser,
                        StockIssueDocNum = o.StockIssueDocNum,
                        StockReceiptDocNum = o.StockReceiptDocNum
                    }).ToList()
                })
            }));
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
