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
using System.Data.Entity.Core.Objects;
using System.Threading.Tasks;
using MyERP.Repository;

namespace MyERP.Controllers
{
    public class ManufacturingOrderController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        public async Task<ActionResult> Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "",int statusId=0)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            var emplpyee = await db.ERPUsers.Where(x => x.Id == userId).Select(x => new { x.Employee.IsChef, x.EmployeeId }).FirstOrDefaultAsync();

            Notification.GetNotification("ManufacturingOrder", "View", "Index", null, null, "أوامر التصنيع");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            Repository<ManufacturingOrder> repository = new Repository<ManufacturingOrder>(db);
            IQueryable<ManufacturingOrder> manufacturingOrders;

            ViewBag.StatusId = new SelectList( new[] {
                 new {Id="1", ArName="لم يتم الإنهاء"},
                 new {Id="2", ArName="تم الإنهاء"},
                 new {Id="3", ArName="تم الإعتماد"},
            }, "Id", "ArName", statusId);
            bool isAccepted = false, isCompleted = false;
            if (statusId == 2)
            {
                isCompleted = true;
            }
            else if (statusId == 3)
            {
                isCompleted = true;
                isAccepted = true;
            }
            if (string.IsNullOrEmpty(searchWord))
            {
                manufacturingOrders = repository.GetAll().Where(s => s.IsDeleted == false && (emplpyee.IsChef ? s.ChefId == emplpyee.EmployeeId : true)&&(statusId==0||(s.IsCompleted==isCompleted&&s.IsAccepted==isAccepted))).Include(s => s.Warehouse).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = await repository.GetAll().Where(s => s.IsDeleted == false && (emplpyee.IsChef ? s.ChefId == emplpyee.EmployeeId : true) && (statusId == 0 || (s.IsCompleted == isCompleted && s.IsAccepted == isAccepted))).CountAsync();
            }
            else
            {
                manufacturingOrders = repository.GetAll().Where(s => s.IsDeleted == false && (s.DocumentNumber.Contains(searchWord) || s.ManufacturingRequest.ItemGroup.ArName.Contains(searchWord) || s.Employee.ArName.Contains(searchWord) || s.OrderDate.ToString().Contains(searchWord)||s.ManufacturingRequest.DocumentNumber.Contains(searchWord)) && (emplpyee.IsChef ? s.ChefId == emplpyee.EmployeeId : true) && (statusId == 0 || (s.IsCompleted == isCompleted && s.IsAccepted == isAccepted))).Include(s => s.Warehouse).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = await repository.GetAll().Where(s => s.IsDeleted == false && s.DocumentNumber.Contains(searchWord) && (emplpyee.IsChef ? s.ChefId == emplpyee.EmployeeId : true) && (statusId == 0 || (s.IsCompleted == isCompleted && s.IsAccepted == isAccepted))).CountAsync();
            }

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة أوامر التصنيع",
                EnAction = "Index",
                ControllerName = "ManufacturingOrder",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });

            return View(await manufacturingOrders.ToListAsync());
        }

        public async Task<ActionResult> AddEdit(int id)
        {
            SystemSetting systemSetting = await db.SystemSettings.AnyAsync() ? await db.SystemSettings.FirstOrDefaultAsync() : new SystemSetting();
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            var departments = await departmentRepository.UserDepartments(1).ToListAsync();//show all deprartments so that user can select factory department
            ViewBag.LinkWithDocModalDepartmentId = new SelectList(departments, "Id", "ArName", systemSetting.DefaultDepartmentId);

            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            ViewBag.ShowItemCost = userId == 1 ? true : await db.UserPrivileges.Where(x => x.PageAction.EnName == "ShowItemCost" && x.PageAction.PageId == 6199).Select(x => x.Privileged == true).FirstOrDefaultAsync();
            ManufacturingOrder manufacturingOrder = await db.ManufacturingOrders.FindAsync(id);
            if (manufacturingOrder == null)
            {
                return HttpNotFound();
            }

            ViewBag.OrderDate = manufacturingOrder.OrderDate.ToString("yyyy-MM-ddTHH:mm");

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل اوامر البيع",
                EnAction = "AddEdit",
                ControllerName = "ManufacturingOrder",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = manufacturingOrder.Id,
                CodeOrDocNo = manufacturingOrder.DocumentNumber
            });
            return View(manufacturingOrder);
        }

        [HttpPost]
        public async Task<JsonResult> SetItemIsDelivered(int id, List<ManufacturingRequestItem> manufacturingRequestItems, string notes)
        {
            using (DbContextTransaction transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    foreach (var item in manufacturingRequestItems)
                    {
                        db.ManufacturingRequestItems.Attach(item);
                        item.IsDelivered = true;
                        db.Entry(item).Property(x => x.ReceivedQty).IsModified = true;
                        db.Entry(item).Property(x => x.IsDelivered).IsModified = true;
                    }
                    ManufacturingOrder manufacturingOrder = new ManufacturingOrder { Id = id, DocumentNumber = "" };
                    db.ManufacturingOrders.Attach(manufacturingOrder);
                    manufacturingOrder.Notes = notes;
                    db.Entry(manufacturingOrder).Property(x => x.Notes).IsModified = true;
                    await db.SaveChangesAsync();
                    transaction.Commit();
                    var manufacturingRequestItemIds = manufacturingRequestItems.Select(x => x.Id);
                    var manufacturingRequest = await db.ManufacturingOrders.Where(x => x.Id == id).Select(x => new
                    {
                        x.ManufacturingRequest.DepartmentId,
                        Items = x.ManufacturingRequest.ManufacturingRequestItems.Where(i => manufacturingRequestItemIds.Contains(i.Id)).Select(r => new
                        {
                            r.ItemId,
                            ItemPriceId = r.Item.ItemPrices.Where(i => i.IsDefault == true).Select(i => i.Id).FirstOrDefault(),
                            r.ItemUnitId,
                            Price = r.Cost,
                            CostPrice = r.Cost,
                            Qty = r.ReceivedQty,
                            UnitEquivalent = 1,
                            r.WarehouseId
                        })
                    }).FirstOrDefaultAsync();
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
                    var resultId = new ObjectParameter("Id", typeof(Int32));
                    foreach (var warehouseId in manufacturingRequest.Items.Select(x => x.WarehouseId).Distinct())
                    {
                        MyXML.xPathName = "Details";
                        string stockIssueDetils = MyXML.GetXML(manufacturingRequest.Items.Where(x => x.WarehouseId == warehouseId));
                        db.StockIssueVoucher_Insert(resultId, null, warehouseId, manufacturingRequest.DepartmentId, cTime, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, 6200, id, null, null, null, true, true, true, false, userId, true, false, true, null, null, null, stockIssueDetils, 1);
                    }
                    int chefWarehouseId = await db.ManufacturingOrders.Where(x => x.Id == id).Select(x => x.WarehouseId).FirstOrDefaultAsync();
                    MyXML.xPathName = "Details";
                    string stockReceiptDetails = MyXML.GetXML(manufacturingRequest.Items);
                    db.StockReceiptVoucher_Insert(resultId, null, chefWarehouseId, manufacturingRequest.DepartmentId, cTime, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, 6200, id, null, null, null, true, true, true, false, userId, true, false, true, null, null, null, stockReceiptDetails, false,null);
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
            return Json(true);
        }

        [HttpPost]
        public async Task<JsonResult> UpdateProducedQty(int id, List<ManufacturingRequestItem> manufacturingRequestItems)
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
                    foreach (var item in manufacturingRequestItems)
                    {
                        db.ManufacturingRequestItems.Attach(item);
                        db.Entry(item).Property(x => x.ProducedQty).IsModified = true;
                    }
                    ManufacturingOrder manufacturingOrder = new ManufacturingOrder() { Id = id, IsCompleted = true, CompletionDate = cTime, DocumentNumber = "" };
                    db.ManufacturingOrders.Attach(manufacturingOrder);
                    db.Entry(manufacturingOrder).Property(x => x.IsCompleted).IsModified = true;
                    db.Entry(manufacturingOrder).Property(x => x.CompletionDate).IsModified = true;
                    await db.SaveChangesAsync();
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
            return Json(true);
        }

        [HttpPost]
        public async Task<JsonResult> AcceptOrder(int id, int manufacturingRequestId, int warehouseId, bool isProcess1)
        {
            using (DbContextTransaction transaction = db.Database.BeginTransaction())
            {
                try
                {
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
                    var manufacturingRequest = await db.ManufacturingRequests.Where(x => x.Id == manufacturingRequestId).Select(x => new
                    {
                        x.DepartmentId,
                        WarehouseId = x.ManufacturingOrders.Select(m => m.WarehouseId).FirstOrDefault(),
                        Items = x.ManufacturingRequestItems.Select(r => new
                        {
                            r.ItemId,
                            ItemPriceId = r.Item.ItemPrices.Where(i => i.IsDefault == true).Select(i => i.Id).FirstOrDefault(),
                            r.ItemUnitId,
                            Price = r.Cost,
                            CostPrice = r.Cost,
                            Qty = r.QTY,
                            UnitEquivalent = 1,
                            IsProcess =r.IsProcessed, 
                            r.IsProcessComponent
                        })
                    }).FirstOrDefaultAsync();
                    MyXML.xPathName = "Details";
                    string stockIssueDetils = MyXML.GetXML(manufacturingRequest.Items.Where(x => x.IsProcessComponent));
                    var resultId = new ObjectParameter("Id", typeof(Int32));
                    db.StockIssueVoucher_Insert(resultId, null, manufacturingRequest.WarehouseId, manufacturingRequest.DepartmentId, cTime, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, 6200, id, null, null, null, true, true, true, false, userId, true, false, true, null, null, null, stockIssueDetils, 1);
                    MyXML.xPathName = "Details";
                    string stockReceiptDetails = MyXML.GetXML(manufacturingRequest.Items.Where(x => x.IsProcess));
                    db.StockReceiptVoucher_Insert(resultId, null, warehouseId, manufacturingRequest.DepartmentId, cTime, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, 6200, id, null, null, null, true, true, true, false, userId, true, false, true, null, null, null, stockReceiptDetails, true,null);
                    ManufacturingOrder manufacturingOrder = new ManufacturingOrder() { Id = id, DocumentNumber = "", IsAccepted = true, AcceptanceDate = cTime };
                    db.ManufacturingOrders.Attach(manufacturingOrder);
                    db.Entry(manufacturingOrder).Property(x => x.IsAccepted).IsModified = true;
                    db.Entry(manufacturingOrder).Property(x => x.AcceptanceDate).IsModified = true;
                    await db.SaveChangesAsync();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw;
                }
            }

            return Json(true);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            var manufacturingOrder = await db.ManufacturingOrders.Where(x => x.Id == id).Select(x => new { x.IsPosted, x.DocumentNumber }).FirstOrDefaultAsync();
            if (manufacturingOrder.IsPosted == true)
            {
                return Content("false");
            }
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            db.ManufacturingOrder_Delete(id);
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = " حذف اوامر الشراء",
                EnAction = "AddEdit",
                ControllerName = "PurchaseOrder",
                UserName = User.Identity.Name,
                UserId = userId,
                LogDate = DateTime.Now,
                RequestMethod = "POST",
                SelectedItem = id,
                CodeOrDocNo = manufacturingOrder.DocumentNumber
            });

            ////-------------------- Notification-------------------------////
            Notification.GetNotification("ManufacturingOrder", "Delete", "Delete", id, null, "اوامر التصنيع");

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