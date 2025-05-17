using Microsoft.AspNet.Identity;
using MyERP.Models;
using MyERP.Repository;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.SqlServer;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using FirebaseAdmin.Messaging;

namespace MyERP.Controllers
{
    public class HelperController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        [SkipERPAuthorize]
        public JsonResult ItemsByDepId(int? departmentId, bool? isDefault)
        {
            bool calculateQty = false;
            if (System.Web.Configuration.WebConfigurationManager.AppSettings["ProjectName"] == "Kimo")
            {
                isDefault = true;
                calculateQty = true;
            }
            if (System.Web.Configuration.WebConfigurationManager.AppSettings["ProjectName"] == "Genoise")
            {
                departmentId = null;
            }
            return Json(db.Item_AllByDepIdAndName_PriceAndQuantity(departmentId, null, "", isDefault, calculateQty), JsonRequestBehavior.AllowGet);

            //return Json(await db.ItemPrices.Where(x => x.Item.IsDeleted == false && x.Item.IsActive == true && (isDefault == null || x.IsDefault == isDefault) && (x.Item.DepartmentItems.Select(d => (int?)d.DepartmentId).Contains(departmentId))).Select(x => new
            //{
            //    x.Item.Id,
            //    ItemPriceId = x.Id,
            //    //  ArName =/* x.Barcode + " - " +*/ x.Item.ArName + " - " + x.ItemUnit.ArName,
            //    ArName = x.Item.Code + " - " + x.Item.ArName + " - " + x.ItemUnit.ArName,
            //    x.Price,
            //    x.Barcode,
            //    x.ItemUnitId,
            //    Quantity = db.GetItemQuantityInWarehouse(null, x.ItemId),
            //    x.Equivalent,
            //    ItemUnitArName = x.ItemUnit.ArName,
            //    x.FactoryPrice,
            //    ItemHasExpiry = x.Item.HasExpiry
            //}).ToListAsync(), JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult InventoryVoucherItemsByDepId(int? departmentId, bool? isDefault, int? warehouseId, int? groupId)
        {
            bool calculateQty = false;
            isDefault = true;
            if (System.Web.Configuration.WebConfigurationManager.AppSettings["ProjectName"] == "Kimo")
            {
                // isDefault = true;
                calculateQty = true;
            }
            if (System.Web.Configuration.WebConfigurationManager.AppSettings["ProjectName"] == "Genoise")
            {
                departmentId = null;
            }
            return Json(db.InventoryVoucher_Item_AllByDepIdAndName_PriceAndQuantity(departmentId, "", isDefault, calculateQty, warehouseId, groupId), JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public async Task<JsonResult> ItemsByDepIdAndGroupId(int? departmentId, int? groupId, bool? isDefault)
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            return Json(await db.ItemPrices.Where(x => x.Item.IsDeleted == false && x.Item.IsActive == true && (groupId == null || x.Item.ItemGroupId == groupId) && (isDefault == null || x.IsDefault == isDefault) && (departmentId == null || x.Item.DepartmentItems.Select(d => (int?)d.DepartmentId).Contains(departmentId))).Select(x => new
            {
                x.Item.Id,
                ItemPriceId = x.Id,
                ArName = x.Barcode + " - " + x.Item.ArName + " - " + x.ItemUnit.ArName,
                x.Price,
                x.Barcode,
                x.ItemUnitId,
                x.Equivalent,
                ItemUnitArName = session.ToString() == "en" && x.ItemUnit.EnName != null ? x.ItemUnit.EnName : x.ItemUnit.ArName,
                x.FactoryPrice,
                x.Item.ItemGroupId,
                Group = x.Item.ItemGroup.ArName,
                ItemHasExpiry = x.Item.HasExpiry

            }).ToListAsync(), JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult ItemsByWarehouseId(int? warehouseId)
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            var ob = db.WarehouseOBDetails.Where(w => w.WarehouseOBId == warehouseId && w.IsDeleted == false).Select(i => i.ItemPriceId);
            var stockReceipt = db.StockReceiptVoucherDetails.Where(s => s.WareHouseId == warehouseId && s.IsDeleted == false).Select(i => i.ItemPriceId);
            return Json(db.ItemPrices.Where(x => x.Item.IsDeleted == false && x.Item.IsActive == true && (ob.Contains(x.Id) || stockReceipt.Contains(x.Id))).Select(x => new
            {
                x.Item.Id,
                ItemPriceId = x.Id,
                ArName = session.ToString() == "en" && x.Item.EnName != null ? x.Barcode + " - " + x.Item.EnName : x.Barcode + " - " + x.Item.ArName,
                x.Price,
                x.Barcode,
                x.ItemUnitId,
                x.Equivalent,
                ItemUnitArName = session.ToString() == "en" && x.ItemUnit.EnName != null ? x.ItemUnit.EnName : x.ItemUnit.ArName,
                x.FactoryPrice
            }).ToListAsync(), JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult DefaultBarcodeByItemId(int id)
        {
            return Json(db.ItemPrices.FirstOrDefault(i => i.IsDefault == true && i.ItemId == id).Barcode, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult GetSystemSetting()
        {
            //  db.Configuration.ProxyCreationEnabled = false;
            return Json(db.SystemSettings.Select(s => new
            {
                Id = s.Id,
                UseItemUnit = s.UseItemUnit,
                AllowSalesOrderInPos = s.AllowSalesOrderInPos,
                AllowToAddSameItemMultipleTimes = s.AllowToAddSameItemMultipleTimes,
                ApplyTaxesOnSalesIvoiceAuto = s.ApplyTaxesOnSalesIvoiceAuto,
                PrintTransactionsAfterAdd = s.PrintTransactionsAfterAdd,
                PrintTransactionsAfterEdit = s.PrintTransactionsAfterEdit,
                ShowDirectExpense = s.ShowDirectExpense,
                ShowCashBoxBalance = s.ShowCashBoxBalance,
                ShowDashBoard = s.ShowDashBoard,
                ShowItemImageInsteadOfItemName = s.ShowItemImageInsteadOfItemName,
                ShowSalesPrices = s.ShowSalesPrices,
                ShowSerialNumbers = s.ShowSerialNumbers,
                ShowUserTasks = s.ShowUserTasks,
                Logo = s.Logo,
                WidthOfImage = s.WidthOfImage,
                HeightOfImage = s.HeightOfImage,
                DefaultBankId = s.DefaultBankId,
                DefaultCashBoxId = s.DefaultCashBoxId,
                DefaultDepartmentId = s.DefaultDepartmentId,
                DefaultQuantityInPos = s.DefaultQuantityInPos,
                DefaultWarehouseId = s.DefaultWarehouseId,
                WarehouseItemsDistribution = s.WarehouseItemsDistribution,
                UseMultiWarehousesInSalesInvoice = s.UseMultiWarehousesInSalesInvoice,
                PayViaCarryOver = s.PayViaCarryOver,
                PayViaCash = s.PayViaCash,
                PayViaCheque = s.PayViaCheque,
                PayViaVisa = s.PayViaVisa,
                MostSalesLastUpdate = s.MostSalesLastUpdate,
                ServiceFeesPercentage = s.ServiceFeesPercentage,
                VisaCommissionPercentage = s.VisaCommissionPercentage,
                UseCostCenter = s.UseCostCenter,
                NoOfItemGroupsInPOS = s.NoOfItemGroupsInPOS,
                NoOfItemsInPOS = s.NoOfItemsInPOS,
                EnableAdditionalItemsOnPos = s.EnableAdditionalItemsOnPos,
                IssueComponentsOfItemInStockIssue = s.IssueComponentsOfItemInStockIssue,
                ShowUserIcons = s.ShowUserIcons,
                PrintReceiptInsteadOfSalesInvoice = s.PrintReceiptInsteadOfSalesInvoice,
                s.WorkingWithTheEgyptianVacationSystem

            }).FirstOrDefault(), JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public async Task<JsonResult> WarehousesByDepartmentId(int id)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            WarehouseRepository warehouseRepository = new WarehouseRepository(db);
            var warehouses = await warehouseRepository.UserWarehouses(userId, id).ToListAsync();
            return Json(warehouses, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public static void UpdateQuantitiesOnWebsite(dynamic items)
        {
            MySoftERPEntity db = new MySoftERPEntity();
            try
            {
                MyXML.xPathName = "Details";
                string itemsXML = MyXML.GetXML(items);
                var json = new JavaScriptSerializer().Serialize(
                    new
                    {
                        type = "quantity",
                        items = db.GetItemsQuantitiesByIds(itemsXML).Select(i => new
                        {
                            _id = i.IdOnWebsite,
                            quantity = i.Qty
                        })
                    });
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                ServicePointManager.Expect100Continue = true;

                var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://165.227.194.105/api/erp/products/update");
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Headers.Add("Accept:application/json");
                httpWebRequest.Headers.Add("x-app-token", "kimostore");
                httpWebRequest.Headers.Add("x-app-erp", "kimostore_erp");
                httpWebRequest.Method = "PUT";

                httpWebRequest.UseDefaultCredentials = true;
                httpWebRequest.Credentials = CredentialCache.DefaultCredentials;

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(json);
                    streamWriter.Flush();
                    streamWriter.Close();
                }

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                }
            }
            catch (Exception ex) { }

        }
        [SkipERPAuthorize]
        public static void UpdatePricesOnWebsite(dynamic items)
        {
            MySoftERPEntity db = new MySoftERPEntity();
            try
            {
                MyXML.xPathName = "Details";
                string itemsXML = MyXML.GetXML(items);
                var json = new JavaScriptSerializer().Serialize(
                    new
                    {
                        products = db.GetItemsQuantitiesByIds(itemsXML).Select(i => new
                        {
                            _id = i.IdOnWebsite,
                            price = new
                            {
                                g1 = db.ItemPrices.Where(p => p.ItemId == i.Id && p.CustomerGroupId == 1).Select(p => p.Price).FirstOrDefault(),
                                g3 = db.ItemPrices.Where(p => p.ItemId == i.Id && p.CustomerGroupId == 2).Select(p => p.Price).FirstOrDefault(),
                                g5 = db.ItemPrices.Where(p => p.ItemId == i.Id && p.CustomerGroupId == 3).Select(p => p.Price).FirstOrDefault()
                            }
                        })
                    });
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                ServicePointManager.Expect100Continue = true;

                var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://165.227.194.105/api/erp/products/update/prices");
                httpWebRequest.Headers.Add("Content-Type:application/json; charset=utf-8"); //Content-Type  
                httpWebRequest.Headers.Add("Accept:application/json");
                httpWebRequest.Headers.Add("x-app-token", "kimostore");
                httpWebRequest.Headers.Add("x-app-erp", "kimostore_erp");
                httpWebRequest.Method = "PUT";

                httpWebRequest.UseDefaultCredentials = true;
                httpWebRequest.Credentials = CredentialCache.DefaultCredentials;

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(json);
                    streamWriter.Flush();
                    streamWriter.Close();
                }

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                }
            }
            catch (Exception ex) { }
        }
        [SkipERPAuthorize]
        public static void UpdateAllItemsQuantitiesOnWebsite(int depId)
        {
            MySoftERPEntity db = new MySoftERPEntity();
            UpdateQuantitiesOnWebsite(db.DepartmentItems.Where(i => i.DepartmentId == depId).Select(i => new { Id = i.ItemId }));
        }

        [SkipERPAuthorize]
        public async Task<JsonResult> CustomerRepByDepartmentId(int id)
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            return Json(await db.Employees.Where(a => a.DepartmentRepId == id && a.IsActive == true&&a.IsDeleted==false).Select(a => new
            {
                Id = a.Id,
                Name = session.ToString() == "en" && a.EnName != null ? a.EnName : a.ArName,
                Code = a.Code
            }).ToListAsync(), JsonRequestBehavior.AllowGet);
        }
         [SkipERPAuthorize]
        public async Task<JsonResult> CustomerRepByCustomerId(int id)
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            return Json(await db.CustomerReps.Where(a => a.CustomerId == id && a.IsActive == true).OrderByDescending(a => a.IsDefault).Select(a => new
            {
                Id = a.Employee.Id,
                Name = session.ToString() == "en" && a.Employee.EnName != null ? a.Employee.EnName : a.Employee.ArName,
                Code = a.Employee.Code
            }).ToListAsync(), JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult ItemQuantityOnSystem(int warehouseId, int itemId)
        {
            var item = db.Items.Find(itemId);
            if (item != null)
            {
                if (item.ItemTypeId == 4)
                    return Json("100", JsonRequestBehavior.AllowGet);
            }
            return Json(db.GetItemQuantityInWarehouse(warehouseId, itemId), JsonRequestBehavior.AllowGet);
        }


        [SkipERPAuthorize]
        public JsonResult ItemQuantityInEachWarehouse(int deptId, int itemId)
        {
            return Json(db.GetItemQuantityInEachWarehouse(itemId, deptId), JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult IsValidSerialNumber(int itemId, string serialNumber)
        {
            return Json(db.IsValidSerialNumber(itemId, serialNumber), JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult GetOpenPeriodBeginningEnding()
        {
            var openend = db.GetOpenPeriodBeginningEnding().FirstOrDefault();
            if (openend != null)
            {
                return Json(new
                {
                    PeriodEnd = openend.PeriodEnd.HasValue ? openend.PeriodEnd.Value.ToString("yyyy-MM-ddTHH:mm") : "",
                    PeriodStart = openend.PeriodStart.HasValue ? openend.PeriodStart.Value.ToString("yyyy-MM-ddTHH:mm") : ""
                }, JsonRequestBehavior.AllowGet);
            }
            return Json(new
            {
                PeriodEnd = "",
                PeriodStart = ""
            }, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public StartAndEndPeriod GetOpenPeriodBeginningEndingForReports()
        {
            var openend = db.GetOpenPeriodBeginningEnding().FirstOrDefault();

            StartAndEndPeriod strtAndEndPeriod = new StartAndEndPeriod();

            strtAndEndPeriod.end = openend.PeriodEnd.HasValue ? openend.PeriodEnd.Value.ToString("yyyy-MM-ddTHH:mm") : "";
            strtAndEndPeriod.start = openend.PeriodStart.HasValue ? openend.PeriodStart.Value.ToString("yyyy-MM-ddTHH:mm") : "";
            return strtAndEndPeriod;
        }

        [SkipERPAuthorize]
        [System.Web.Mvc.HttpPost]
        public async Task UpdateItemQuantities(ICollection<PurchaseSaleSerialNumber> itemIds, DateTime date)
        {
            //DateTime utcNow = DateTime.UtcNow;
            //TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            //DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
            //await Task.Run(() =>
            //{
            //    MyXML.xPathName = "ItemId";
            //    var itemIdsXML = MyXML.GetXML(itemIds.Select(x => new { ItemId = x.ItemId }));
            //    db.ItemQuantity_Insert(cTime, itemIdsXML);
            //});
        }

        [SkipERPAuthorize]
        public async Task<JsonResult> CashBoxByDepartment(int id)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            CashboxReposistory cashboxReposistory = new CashboxReposistory(db);
            return Json(await cashboxReposistory.UserCashboxes(userId, id).ToListAsync(), JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public async Task<JsonResult> ItemUnits(int? itemId)
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            if (itemId != null)
                return Json(await db.ItemPrices.Where(x => x.ItemId == itemId).Select(x => new
                {
                    Id = x.ItemUnitId,
                    ArName = session.ToString() == "en" && x.ItemUnit.EnName != null ? x.ItemUnit.Code + " - " + x.ItemUnit.EnName : x.ItemUnit.Code + " - " + x.ItemUnit.ArName,
                    x.Equivalent,
                    x.Barcode,
                    ItemPriceId = x.Id,
                    Price = x.Item.IsAccessory != true ? x.Price : 0,
                    x.FactoryPrice,
                    AccessoryPrice = x.Item.IsAccessory == true ? x.Price : 0
                }).ToListAsync(), JsonRequestBehavior.AllowGet);
            return Json(await db.ItemUnits.Where(x => x.IsActive == true && x.IsDeleted == false).Select(x => new
            {
                x.Id,
                ArName = session.ToString() == "en" && x.EnName != null ? x.Code + " - " + x.EnName : x.Code + " - " + x.ArName
                ,
                x.Equivalent
            }).ToListAsync(), JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public async Task<JsonResult> Vendors()
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            return Json(await db.Vendors.Where(x => x.IsActive == true && x.IsDeleted == false).Select(x => new
            {
                x.Id,
                ArName = session.ToString() == "en" && x.EnName != null ? x.Code + " - " + x.EnName : x.Code + " - " + x.ArName
            }).ToListAsync(), JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public async Task<JsonResult> CustomersGroups()
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            return Json(await db.CustomersGroups.Where(x => x.IsActive == true && x.IsDeleted == false).Select(x => new
            {
                x.Id,
                ArName = session.ToString() == "en" && x.EnName != null ? x.Code + " - " + x.EnName : x.Code + " - " + x.ArName
            }).ToListAsync(), JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public async Task<JsonResult> GetItemAvgPrice(int itemId, int departmentId)
        {
            ItemRepository itemRepository = new ItemRepository(db);
            var avgPrice = await itemRepository.GetItemAvgPrice(itemId, departmentId);
            //var avgPrice = db.ItemCostPrices.Where(x => x.ItemId == itemId && x.IsDeleted == false && x.DepartmentId==departmentId).OrderByDescending(x => x.Id).Select(x => x.CostAfter).FirstOrDefault();
            return Json(avgPrice, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        [HttpPost]
        public async Task<JsonResult> GetItemAvgPriceBulk(List<int> itemIds, int departmentId)
        {
            ItemRepository itemRepository = new ItemRepository(db);
            var avgCosts = await itemRepository.GetItemAvgPriceBulk(itemIds, departmentId);
            return Json(avgCosts);
        }

        [SkipERPAuthorize]
        public async Task<JsonResult> IsServiceFeesIncluded(int customerId)
        {
            return Json((await db.Customers.Where(x => x.Id == customerId).Select(x => x.IncludeFees).FirstOrDefaultAsync()) == true, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult GetCustomerDebitBalance(int id)
        {
            return Json(db.Customer_DebitBalance(id), JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public ActionResult CurrentTime()
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
            return Content(cTime.ToString("yyyy-MM-ddTHH:mm"));
        }
        [SkipERPAuthorize]
        public JsonResult CashBoxBalance(int id)
        {
            return Json(db.CashBox_Balances(id).Select(x => x.Balance).FirstOrDefault(), JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult GetUserImage()
        {
            int id = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            int EmpId = db.ERPUsers.Where(a => a.Id == id).FirstOrDefault().EmployeeId;
            return Json(db.Employees.Where(a => a.Id == EmpId).Select(a => a.Image).FirstOrDefault(), JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult ChangeLanguage()
        {
            if (Session["lang"].ToString() == "ar")
            {
                Session["lang"] = "en";
            }
            else
            {
                Session["lang"] = "ar";
            }
            int id = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            var user = db.ERPUsers.Find(id);
            user.Language = Session["lang"].ToString();
            db.Entry(user).State = EntityState.Modified;
            db.SaveChanges();
            return Json(new { lang = Session["lang"] }, JsonRequestBehavior.AllowGet);
        }
        //[SkipERPAuthorize]
        //public async Task<ActionResult> ItemGroups()
        //{
        //    return PartialView(await db.ItemGroups.Where(x => x.IsDeleted == false && x.IsActive == true && x.Id != 10).ToListAsync());//to hide raw materials in pos and sales order
        //}
        [SkipERPAuthorize]
        public async Task<ActionResult> ItemGroups(int pageIndex, int departmentId = 0)
        {
            int skipRowsNo = 0;
            var projectName = System.Web.Configuration.WebConfigurationManager.AppSettings["ProjectName"];
            var systemSetting = db.SystemSettings.FirstOrDefault();
            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * systemSetting.NoOfItemGroupsInPOS;
            ViewBag.NoOfGroups = systemSetting.NoOfItemGroupsInPOS;
            ViewBag.NoOfItems = systemSetting.NoOfItemsInPOS;
            ViewBag.ShowItemImageInsteadOfItemName = systemSetting.ShowItemImageInsteadOfItemName;
            ViewBag.WidthOfImage = systemSetting.WidthOfImage;
            ViewBag.HeightOfImage = systemSetting.HeightOfImage;
            ViewBag.MiniPos = System.Web.Configuration.WebConfigurationManager.AppSettings["MiniPos"] == "true" ? true : false;
            if (projectName == "Genoise" && departmentId == 28)
            {
                ViewBag.ItemGroupCount = db.ItemGroups.Where(x => x.IsDeleted == false && x.IsActive == true).Count();
                return PartialView(await db.ItemGroups.Where(x => x.IsDeleted == false && x.IsActive == true).OrderBy(x => x.Id).Skip(skipRowsNo).Take(systemSetting.NoOfItemGroupsInPOS).ToListAsync());
            }
            else
            {
                ViewBag.ItemGroupCount = db.ItemGroups.Where(x => x.IsDeleted == false && x.IsActive == true && x.IsInPos == true && (x.IsInAllDepartments == true ||
                (db.ItemGroupDepartments.Where(d => d.ItemGroupId == x.Id).Select(s => s.DepartmentId).ToList().Contains(departmentId)))).Count();
                var xy = await db.ItemGroups.Where(x => x.IsDeleted == false && x.IsActive == true && x.IsInPos == true && (x.IsInAllDepartments == true ||
                 db.ItemGroupDepartments.Where(d => d.ItemGroupId == x.Id).Select(s => s.DepartmentId).ToList().Contains(departmentId))).OrderBy(x => x.Id).ToListAsync();
                return PartialView(await db.ItemGroups.Where(x => x.IsDeleted == false && x.IsActive == true && x.IsInPos == true && (x.IsInAllDepartments == true ||
                db.ItemGroupDepartments.Where(d => d.ItemGroupId == x.Id).Select(s => s.DepartmentId).ToList().Contains(departmentId))).OrderBy(x => x.Id).Skip(skipRowsNo).Take(systemSetting.NoOfItemGroupsInPOS).ToListAsync());
            }
        }
        [SkipERPAuthorize]
        public async Task<JsonResult> GetItemsByGroupAndPaging(int groupId, int index)
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            int skipRowsNo = 0;
            bool isECommerce = System.Web.Configuration.WebConfigurationManager.AppSettings["ECommerce"] == "true" ? true : false;
            var systemSetting = db.SystemSettings.FirstOrDefault();
            if (index > 1)
                skipRowsNo = (index - 1) * systemSetting.NoOfItemsInPOS;
            db.Configuration.ProxyCreationEnabled = false;
            var items = await db.Items.Where(x => x.IsDeleted == false && x.IsActive == true && x.ItemGroupId == groupId).OrderBy(x => x.Id).Skip(skipRowsNo).Take(systemSetting.NoOfItemsInPOS).Select(x => new
            {
                x.Id,
                x.Code,
                ArName = session.ToString() == "en" && x.EnName != null ? /*x.Code + " - " +*/ x.EnName : /*x.Code + " - " + */x.ArName,
                x.EnName,
                Image = x.Image,
                x.HasAccessory,
                x.IsAccessory
            }).ToListAsync(); ;
            if (isECommerce == true)
            {
                items = await db.Items.Where(x => x.IsDeleted == false && x.IsActive == true && x.ItemGroupId == groupId).OrderBy(x => x.Id).Skip(skipRowsNo).Take(systemSetting.NoOfItemsInPOS).Select(x => new
                {
                    x.Id,
                    x.Code,
                    x.ArName,
                    x.EnName,
                    Image = db.ItemImages.Where(i => i.ItemId == x.Id).Select(i => i.Image).FirstOrDefault().ToString(),
                    x.HasAccessory,
                    x.IsAccessory
                }).ToListAsync();
            }
            var itemsCount = db.Items.Where(x => x.IsDeleted == false && x.IsActive == true && x.ItemGroupId == groupId).Count();
            return Json(new { items = items, itemsCount = itemsCount }, JsonRequestBehavior.AllowGet);
        }



        [HttpGet]
        public async Task UpdatePlayerCurrentUser(string playerId)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            ERPUser user = await db.ERPUsers.FindAsync(userId);
            user.PlayerId = playerId;
            db.Entry(user).State = EntityState.Modified;
            await db.SaveChangesAsync();
        }
        [SkipERPAuthorize] //
        [AllowAnonymous]
        public async Task<JsonResult> GetCustomers()
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            var customers = await db.Customers.Where(x => !x.IsDeleted && x.IsActive).Select(x => new
            {
                x.Id,
                ArName = /*session != null && session.ToString() == "en" && x.EnName != null ? x.EnName :*/ x.ArName,
                x.Code,
                Mobile = x.Mobile ?? "",
                Email = x.Email ?? "",
                Address = x.Address ?? "",
                x.IsBranch,
                x.IncludeFees,
                x.NationalId,
                x.IsBlocked,
                x.CustomersGroupId,
                CustomerGroupArName = x.CustomersGroup.Code + " - " + x.CustomersGroup.ArName,
                x.CityId,
                CityArName = x.CityId > 0 ? x.City.Code + " - " + x.City.ArName : null,
                x.CountryId,
                CountryArName = x.CountryId > 0 ? x.Country.Code + " - " + x.Country.ArName : null,
            }).ToListAsync();
            return Json(customers, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize] //

        public async Task<JsonResult> GetEmployee(int? id)
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            db.Configuration.ProxyCreationEnabled = false;
            var EmployeeContractJobId = db.EmployeeContracts.Where(a => a.IsActive == true && a.IsDeleted == false && a.EmployeeId == id).FirstOrDefault().ContractJobId;
            var job = db.Jobs.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == EmployeeContractJobId).FirstOrDefault();
            var Employee = await db.Employees.Where(x => !x.IsDeleted && x.IsActive && x.Id == id).Select(x => new
            {
                x.Id,
                ArName = session.ToString() == "en" && x.EnName != null ? x.EnName : x.ArName,
                x.Code,
                x.AdministrativeDepartmentId,
                x.DepartmentId,
                //EmpJob = session.ToString() == "en" && x.Job.EnName != null ? x.Job.EnName : x.Job.ArName,
                EmpJob = session.ToString() == "en" && job.EnName != null ? job.EnName : job.ArName,
                JobLocation = session.ToString() == "en" && x.Location.EnName != null ? x.Location.EnName : x.Location.ArName,
                HrDept = session.ToString() == "en" && x.HrDepartment.EnName != null ? x.HrDepartment.EnName : x.HrDepartment.ArName,
                EmpManager = db.Employees.Where(a => a.Id == x.DirectManagerId).Select(a => new { ArName = session.ToString() == "en" && a.EnName != null ? a.EnName : a.ArName, a.Code }).FirstOrDefault(),
                StartDate = x.HireDate,
                LastAttendDate = db.EmployeesAttendAndLeaveDetails.Where(a => a.EmployeeId == x.Id && a.IsDeleted == false).ToList(),
                EmployeeVacation = x.EmployeeVacationOpeningBalanceDetails.Where(a => a.EmployeeId == x.Id && a.IsDeleted == false).Select(a => new { a.VacationWithoutSalary, a.VacationBalance }),
                VacationWithoutSalaryRecord = x.VacationRequests.Where(a => a.EmployeeId == x.Id && a.IsDeleted == false && a.IsPaid == false && a.IsAcceptedByManager == true).Select(a => a.NumberOfVacationDays).Sum(),
                VacationWithSalaryRecord = x.VacationRequests.Where(a => a.EmployeeId == x.Id && a.IsDeleted == false && a.IsPaid == true && a.IsAcceptedByManager == true).Select(a => a.NumberOfVacationDays).Sum(),
                AbsenceDaysRecord = x.EmployeeAbsences.Where(a => a.EmployeeId == x.Id && a.IsDeleted == false && a.IsActive == true).Count(),
                AbsenceDaysBalance = x.EmployeeVacationOpeningBalanceDetails.Where(a => a.IsDeleted == false && a.EmployeeId == x.Id).FirstOrDefault().Absence,
                EmployeeContractVacationDetails = x.EmployeeContracts.Where(a => a.EmployeeId == x.Id && a.IsDeleted == false && a.IsActive == true).Select(a => new { a.VacationDuePeriodNum, VacationDuePeriod = db.PeriodTypes.Where(v => v.Id == a.VacationDuePeriodTypeId).FirstOrDefault().EquivalentDaysNum, a.VacationPeriodNum, VacationPeriod = db.PeriodTypes.Where(v => v.Id == a.VacationPeriodTypeId).FirstOrDefault().EquivalentDaysNum })
            }).ToListAsync();
            return Json(Employee, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public async Task<ActionResult> PurchaseRequestsAll()
        {
            IEnumerable<PurchaseRequest> purchaseRequests = await db.PurchaseRequests
                .Where(x => x.IsDeleted == false && x.IsApproved == true && !x.ManufacturingPurchaseRequests.Any()).Include(x => x.PurchaseRequestStatu).Include(x => x.Department).Include(x => x.ItemGroup).OrderByDescending(x => x.Id).ToListAsync();
            return PartialView(purchaseRequests);
        }

        [SkipERPAuthorize]
        public JsonResult GetAllItems()
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            return Json(db.ItemPrices.Where(x => x.Item.IsDeleted == false && x.Item.IsActive == true).Select(x => new
            {
                x.Item.Id,
                ItemPriceId = x.Id,
                ArName = session.ToString() == "en" && x.Item.EnName != null ? x.Barcode + " - " + x.Item.EnName : x.Barcode + " - " + x.Item.ArName,
                x.Price,
                x.Barcode,
                x.ItemUnitId,
                x.Equivalent,
                ItemUnitArName = session.ToString() == "en" && x.ItemUnit.EnName != null ? x.ItemUnit.EnName : x.ItemUnit.ArName,
                x.FactoryPrice
            }).ToList(), JsonRequestBehavior.AllowGet);
        }


        [SkipERPAuthorize]
        public JsonResult GetAllEmployees()
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            db.Configuration.ProxyCreationEnabled = false;
            return Json(db.Employees.Where(e => e.IsDeleted == false && e.IsActive == true).Select(e => new
            {
                Id = e.Id,
                e.Code,
                ArName = session.ToString() == "en" && e.EnName != null ? e.Code + " - " + e.EnName : e.Code + " - " + e.ArName,
                e.DepartmentId,
                e.AdministrativeDepartmentId,
                e.NationalId
            }).ToList(), JsonRequestBehavior.AllowGet);
        }


        [SkipERPAuthorize]
        public JsonResult GetAllDirectExpenses()
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            db.Configuration.ProxyCreationEnabled = false;
            return Json(db.DirectExpenses.Where(e => e.IsDeleted == false && e.IsActive == true).Select(e => new
            {
                Id = e.Id,
                ArName = session.ToString() == "en" && e.EnName != null ? e.Code + " - " + e.EnName : e.Code + " - " + e.ArName,
            }).ToList(), JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult GetUserTasks()
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            db.Configuration.ProxyCreationEnabled = false;
            var UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            //var CreatedByUser = db.UserTasks.Where(a => a.CreatedBy == UserId).Include(a=>a.ERPUser).Include(a=>a.TaskStatu).ToList(); // الموجهة من 
            //var AssignedToUser = db.UserTasks.Where(a => a.AssignedTo == UserId).Include(a=>a.ERPUser).Include(a=>a.TaskStatu).ToList(); //الموجهة ل 
            var CreatedByUser = db.UserTasks.Where(a => a.CreatedBy == UserId).Select(a => new
            {
                Id = a.Id,
                ArName = session.ToString() == "en" && a.EnName != null ? a.Code + " - " + a.EnName : a.Code + " - " + a.ArName,
                Code = a.Code,
                CreatedBy = a.CreatedBy,
                CreatedDate = a.CreatedDate,
                FinishedDate = a.FinishedDate,
                Notes = a.Notes,
                ERPId = a.ERPUser.Id,
                ERPName = a.ERPUser.Name,
                StatuId = a.TaskStatu.Id,
                StatuName = session.ToString() == "en" && a.TaskStatu.EnName != null ? a.TaskStatu.EnName : a.TaskStatu.ArName,
                EmployeeName = session.ToString() == "en" && a.ERPUser.Employee.EnName != null ? a.ERPUser.Employee.EnName : a.ERPUser.Employee.ArName,
                EmployeeId = a.ERPUser.Employee.Id
            }).ToList(); // الموجهة من 
            var AssignedToUser = db.UserTasks.Where(a => a.AssignedTo == UserId).Select(a => new
            {
                Id = a.Id,
                ArName = session.ToString() == "en" && a.EnName != null ? a.Code + " - " + a.EnName : a.Code + " - " + a.ArName,
                Code = a.Code,
                AssignedTo = a.AssignedTo,
                CreatedDate = a.CreatedDate,
                FinishedDate = a.FinishedDate,
                Notes = a.Notes,
                ERPId = a.ERPUser.Id,
                ERPName = a.ERPUser.Name,
                StatuId = a.TaskStatu.Id,
                StatuName = session.ToString() == "en" && a.TaskStatu.EnName != null ? a.TaskStatu.EnName : a.TaskStatu.ArName,
                EmployeeName = session.ToString() == "en" && a.ERPUser.Employee.EnName != null ? a.ERPUser.Employee.EnName : a.ERPUser.Employee.ArName,
                EmployeeId = a.ERPUser.Employee.Id
            }).ToList(); //الموجهة ل 
            var TaskStatus = db.TaskStatus.Select(a => new
            {
                Id = a.Id,
                ArName = session.ToString() == "en" && a.EnName != null ? a.EnName : a.ArName,
            }).ToList();

            return Json(new { AssignedToUser = AssignedToUser, CreatedByUser = CreatedByUser, TaskStatus = TaskStatus }, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult GetERPUsers()
        {
            //             var session = Session["lang"]!=null ? Session["lang"].ToString():"ar";
            db.Configuration.ProxyCreationEnabled = false;
            return Json(db.ERPUsers.Where(e => e.IsDeleted == false && e.IsActive == true).Select(e => new
            {
                Id = e.Id,
                ArName = e.Name,
                UserName = e.UserName
            }).ToList(), JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult GetNotActiveModules()
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            db.Configuration.ProxyCreationEnabled = false;
            //var systemPages = db.SystemPages.Where(a => a.IsModule == true && a.ParentId == null && a.Id != 2116 && !a.IsActive).OrderBy(s => s.Id)
            //           .Union(db.SystemPages.Where(a => a.ParentId == 2116 && a.IsModule == true && !a.IsActive).OrderBy(s => s.Id)).Select(s => new
            //           {
            //               Id = s.Id,
            //               ArName = session.ToString() == "en" && s.EnName != null ? s.EnName : s.ArName,
            //           });
            var systemPages = db.AllowedModules.Select(a => new
            {
                Id = a.SystemPageId,
                ArName = a.SystemPage.ArName
            }).ToList();
            return Json(systemPages, JsonRequestBehavior.AllowGet);
            ////return Json(db.SystemPages.Where(a => a.IsModule == true && a.ParentId == null && !a.IsActive).Select(a=>new {
            ////    Id=a.Id,
            ////    IsActive=a.IsActive,
            ////    ArName=a.ArName,

            ////}).ToList(), JsonRequestBehavior.AllowGet);
        }


        [SkipERPAuthorize]
        public JsonResult GetCustomerGroupsByItemId(int itemId)
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            return Json(db.ItemPrices.Where(e => e.IsDeleted == false && e.IsActive == true && e.ItemId == itemId).Select(e => new
            {
                GroupId = e.CustomerGroupId,
                GroupName = session.ToString() == "en" && e.CustomersGroup.EnName != null ? e.CustomersGroup.EnName : e.CustomersGroup.ArName,
                GroupPrice = e.Price,
                ItemPriceId = e.Id
            }).ToList(), JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult PatchesByItemId(int itemId, bool getZeroQunatity)
        {
            return Json(db.GetItemPatchQuantity(null, itemId, null, null, null, getZeroQunatity).ToList(), JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult PatchesPerVoucher(int systemPageId, int itemId, int selectedId)
        {
            return Json(db.GetItemPatchPerVoucher(systemPageId, itemId, selectedId).ToList(), JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult PatchesPerDoc(int systemPageId, int selectedId)
        {
            return Json(db.GetItemPatchPerDoc(systemPageId, selectedId).ToList(), JsonRequestBehavior.AllowGet);
        }


        [SkipERPAuthorize]
        public JsonResult GetDepartmentByWareHouseId(int id)
        {
            var Department = db.Warehouses.Where(b => b.IsActive == true && b.IsDeleted == false && b.Id == id).Select(b => new
            {
                DepartmentId = b.DepartmentId
            });
            return Json(Department, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult GetJournalEntryBySourceId(int InvoiceId)
        {
            int sysPageId = QueryHelper.SourcePageId("SalesInvoice");
            var journal = db.JournalEntries.Where(a => a.SourcePageId == sysPageId && a.SourceId == InvoiceId).Select(a => a.Id);
            return Json(journal, JsonRequestBehavior.AllowGet);
        } 
        [SkipERPAuthorize]
        public JsonResult GetJournalEntryByTableName(string TableName, int Id)
        {
            int sysPageId = QueryHelper.SourcePageId(TableName);
            var journal = db.JournalEntries.Where(a => a.SourcePageId == sysPageId && a.SourceId == Id).Select(a => a.Id);
            return Json(journal, JsonRequestBehavior.AllowGet);
        }


        [SkipERPAuthorize]
        public JsonResult GetPageActionsBySystemPageId(int id)
        {
            var PageActions = db.PageActions.Where(a => (a.IsActive == true && a.PageId == id)).Select(pa => new
            {
                pa.ArName,
                Name = pa.ArName
            });
            return Json(PageActions, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult GetCustomerBalanceInEachDept(int? id)
        {
            List<GetCustomerBalanceInEachDept_Result> balance = db.GetCustomerBalanceInEachDept(id).ToList();
            return Json(balance, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult GetVendorBalanceInEachDept(int? id)
        {
            List<GetVendorBalanceInEachDept_Result> balance = db.GetVendorBalanceInEachDept(id).ToList();
            return Json(balance, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult GetCustomerSalesInvoices(int? id, int? cashReceiptVoucherId, int? DepartmentId)
        {
            var customers = db.GetSalesInvoiceActualPayment(id, cashReceiptVoucherId, DepartmentId);
            return Json(customers.ToList(), JsonRequestBehavior.AllowGet);
            //return Json(db.SalesInvoices.Where(s => s.VendorOrCustomerId == id && s.IsActive == true && s.IsDeleted == false).Select(s => new
            //{
            //    s.Id,
            //    s.DocumentNumber,
            //    DepartmentName = s.Department.ArName,
            //    s.TotalAfterTaxes,
            //    s.Paid,
            //    s.VoucherDate
            //}).Take(5).ToList(), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetPurchaseInvoiceActualPayment(int? VendorId, int? CashIssueVoucherId, int? DepartmentId)
        {
            var vendorActualPayments = db.GetPurchaseInvoiceActualPayment(VendorId, CashIssueVoucherId, DepartmentId).ToList();
            //return Json(vendor.ToList(), JsonRequestBehavior.AllowGet);            
            return Json(vendorActualPayments, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult GetDepartmentTables(int departmentId)
        {
            db.Configuration.ProxyCreationEnabled = false;

            var hallsIds = db.Halls.Where(h => h.DepartmentId == departmentId && h.IsActive == true && h.IsDeleted == false).Select(h => h.Id).ToList();
            //var tables = db.Tables.Where(t => hallsIds.Contains(t.HallId.Value) && t.IsActive == true && t.IsDeleted == false).ToList();
            var tabless = db.SalesInvoices.Where(s => s.DepartmentId == departmentId && s.TableId != null && s.IsCollectedByCashier == false && hallsIds.Contains(s.Table.HallId.Value) && s.IsActive == true && s.IsDeleted == false).Select(t => new { Id = t.TableId, ArName = t.Table.ArName, EnName = t.Table.EnName, Total = t.TotalAfterTaxes, ReservationDateTime = t.Table.ReservationDateTime, IsReserved = t.Table.IsReserved, SalesInvoiceId = t.Id }).ToList();
            var tables = db.Tables.Where(t => hallsIds.Contains(t.HallId.Value) && t.IsActive == true && t.IsDeleted == false).Select(t => new { Id = t.Id, ArName = t.ArName, Code = t.Code, EnName = t.EnName, LastSalesInvoice = db.SalesInvoices.Where(s => s.TableId == t.Id && s.IsCollectedByCashier == false && s.IsActive == true && s.IsDeleted == false).Count() > 0 ? db.SalesInvoices.Where(s => s.TableId == t.Id && s.IsCollectedByCashier == false && s.IsActive == true && s.IsDeleted == false).OrderByDescending(s => s.Id).FirstOrDefault() : null, ReservationDateTime = t.ReservationDateTime, IsReserved = t.IsReserved }).ToList();
            return Json(tables, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult GetHallsByDepartmentId(int departmentId)
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            db.Configuration.ProxyCreationEnabled = false;
            var halls = db.Halls.Where(h => h.DepartmentId == departmentId && h.IsActive == true && h.IsDeleted == false).Select(h => new
            {
                Id = h.Id,
                ArName = session.ToString() == "en" && h.EnName != null ? h.Code + " - " + h.EnName : h.Code + " - " + h.ArName
            });
            return Json(halls, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult GetTableByHallId(int hallId, bool? IsChange)
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            db.Configuration.ProxyCreationEnabled = false;
            if (IsChange == true)
            {
                var UnReservedTables = db.Tables.Where(t => t.HallId == hallId && t.IsActive == true && t.IsDeleted == false && t.IsReserved != true).Select(t => new
                {
                    Id = t.Id,
                    ArName = session.ToString() == "en" && t.EnName != null ? /*t.Code + " - " +*/ t.EnName : /*t.Code + " - " +*/ t.ArName,
                    Code = t.Code,
                    EnName = t.EnName,
                    LastSalesInvoice = db.SalesInvoices.Where(s => s.TableId == t.Id && s.IsCollectedByCashier == false && s.IsActive == true && s.IsDeleted == false).Count() > 0 ? db.SalesInvoices.Where(s => s.TableId == t.Id && s.IsCollectedByCashier == false && s.IsActive == true && s.IsDeleted == false).OrderByDescending(s => s.Id).FirstOrDefault() : null,
                    ReservationDateTime = t.ReservationDateTime,
                    IsReserved = t.IsReserved,
                    IsWait = t.IsWait,
                    HallIncreaseValue = t.Hall.IncreaseValue,
                    HallIncreasePercentage = t.Hall.IncreasePercentage,
                    WaitersName = t.WaitersName != null ? t.WaitersName : ""

                }).ToList();
                return Json(UnReservedTables, JsonRequestBehavior.AllowGet);
            }
            var tables = db.Tables.Where(t => t.HallId == hallId && t.IsActive == true && t.IsDeleted == false).Select(t => new
            {
                Id = t.Id,
                ArName = session.ToString() == "en" && t.EnName != null ? /*t.Code + " - " +*/ t.EnName : /*t.Code + " - " +*/ t.ArName,
                Code = t.Code,
                EnName = t.EnName,
                LastSalesInvoice = db.SalesInvoices.Where(s => s.TableId == t.Id && s.IsCollectedByCashier == false && s.IsActive == true && s.IsDeleted == false).Count() > 0 ? db.SalesInvoices.Where(s => s.TableId == t.Id && s.IsCollectedByCashier == false && s.IsActive == true && s.IsDeleted == false).OrderByDescending(s => s.Id).FirstOrDefault() : null,
                ReservationDateTime = t.ReservationDateTime,
                IsReserved = t.IsReserved,
                IsWait = t.IsWait,
                HallIncreaseValue = t.Hall.IncreaseValue,
                HallIncreasePercentage = t.Hall.IncreasePercentage,
                WaitersName = t.WaitersName != null ? t.WaitersName : ""

            }).ToList();
            return Json(tables, JsonRequestBehavior.AllowGet);
        }


        [SkipERPAuthorize]
        public JsonResult GetCustomerOrVendorCreditLimitPeriod(int? CustomerId, int? VendorId)
        {
            var CustomerOrVendorCreditLimitPeriod = 0.00;
            if (CustomerId != null)
            {
                var customer = db.Customers.Find(CustomerId);
                if (customer.CreditLimitPeriod != null)
                {
                    CustomerOrVendorCreditLimitPeriod = (double)customer.CreditLimitPeriod;
                }
                else
                {
                    CustomerOrVendorCreditLimitPeriod = 0.00;
                }
            }
            else if (VendorId != null)
            {
                var vendor = db.Vendors.Find(VendorId);
                if (vendor.CreditLimitPeriod != null)
                {
                    CustomerOrVendorCreditLimitPeriod = (double)vendor.CreditLimitPeriod;
                }
                else
                {
                    CustomerOrVendorCreditLimitPeriod = 0.00;
                }
            }
            return Json(CustomerOrVendorCreditLimitPeriod, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        [HttpPost]
        public JsonResult ReserveTable(int Id, int? OldTableId, int? OldInvoiceId)
        {
            var table = db.Tables.Find(Id);
            if (table != null)
            {
                table.IsReserved = true;
                table.ReservationDateTime = DateTime.Now;
                if (OldTableId != null)
                {
                    var oldTable = db.Tables.Find(OldTableId);
                    if (oldTable != null)
                    {
                        oldTable.IsReserved = null;
                        oldTable.ReservationDateTime = null;
                        table.WaitersName = oldTable.WaitersName;
                        oldTable.WaitersName = null;
                        var invoice = db.SalesInvoices.Find(OldInvoiceId);
                        if (invoice != null)
                        {
                            invoice.TableId = Id;
                        }
                    }
                }
                db.SaveChanges();
                return Json(table.IsReserved, JsonRequestBehavior.AllowGet);
            }
            return Json(false, JsonRequestBehavior.AllowGet);
        }

        //[SkipERPAuthorize]
        //[HttpPost]
        //public ActionResult GetLastInvoiceOfTable(int Id)
        //{
        //    var table = db.Tables.Find(Id);
        //    var result = new object();

        //    if (table != null)
        //    {
        //        var invoice = db.SalesInvoices.Where(s => s.TableId == Id && s.IsActive == true && s.IsDeleted == false).OrderByDescending(s => s.Id).FirstOrDefault();

        //        if (invoice != null)
        //        {
        //            //table.IsReserved = false;
        //            ////table.ReservationDateTime = DateTime.Now;
        //            //db.SaveChanges();
        //            //result = new {IsTableClosed = true, InvoiceId = invoice.Id };
        //            //return Json(result, JsonRequestBehavior.AllowGet);

        //            result = new {InvoiceId = invoice.Id };

        //            return Json(result, JsonRequestBehavior.AllowGet);

        //        }
        //    }
        //    result = new { InvoiceId = 0 };
        //    return Json(result, JsonRequestBehavior.AllowGet);
        //}

        [SkipERPAuthorize]
        public async Task<JsonResult> GetPosCustomers()
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            return Json(await db.PosCustomers.Where(x => !x.IsDeleted && x.IsActive).Select(x => new
            {
                x.Id,
                ArName = session.ToString() == "en" && x.EnName != null ? x.Code + " - " + x.EnName : x.Code + " - " + x.ArName,
                x.Code,
                Mobile1 = x.Mobile1 ?? "",
                Address = x.Address ?? "",
                CustomerId = x.CustomerId
            }).ToListAsync(), JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult GetDriverCar(int driverId)
        {
            var carId = db.Employees.Where(e => e.IsDeleted == false && e.IsActive == true && e.Id == driverId).Select(e => e.FixedAssetId).FirstOrDefault();
            return Json(carId, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public async Task<JsonResult> GetGovernorates()
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            var governorates = await db.Countries.Where(e => e.IsDeleted == false && e.IsActive == true).Select(e => new
            {
                Id = e.Id,
                ArName = session.ToString() == "en" && e.EnName != null ? e.Code + " - " + e.EnName : e.Code + " - " + e.ArName
            }).ToListAsync();
            return Json(governorates, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public async Task<JsonResult> GetCitiesInGovernorate(int governorateId)
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            var cities = await db.Cities.Where(e => e.IsDeleted == false && e.IsActive == true && e.CountryId == governorateId).Select(e => new
            {
                Id = e.Id,
                ArName = session.ToString() == "en" && e.EnName != null ? e.Code + " - " + e.EnName : e.Code + " - " + e.ArName
            }).ToListAsync();
            return Json(cities, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public async Task<JsonResult> GetCities()
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            var cities = await db.Cities.Where(e => e.IsDeleted == false && e.IsActive == true).Select(e => new
            {
                Id = e.Id,
                ArName = session.ToString() == "en" && e.EnName != null ? e.Code + " - " + e.EnName : e.Code + " - " + e.ArName
            }).ToListAsync();
            return Json(cities, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public async Task<JsonResult> GetAreasInCity(int cityId)
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            var areas = await db.Areas.Where(e => e.IsDeleted == false && e.IsActive == true && e.CityId == cityId).Select(e => new
            {
                Id = e.Id,
                ArName = session.ToString() == "en" && e.EnName != null ? e.Code + " - " + e.EnName : e.Code + " - " + e.ArName
            }).ToListAsync();
            return Json(areas, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public async Task<JsonResult> GetAreas()
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            var areas = await db.Areas.Where(e => e.IsDeleted == false && e.IsActive == true).Select(e => new
            {
                Id = e.Id,
                ArName = session.ToString() == "en" && e.EnName != null ? e.Code + " - " + e.EnName : e.Code + " - " + e.ArName
            }).ToListAsync();
            return Json(areas, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult SearchPosCustomers(string mobileNo)
        {
            //db = new MySoftERPEntity();
            var posCustomerInDb = db.PosCustomers.Where(e => e.IsDeleted == false && e.IsActive == true && e.Mobile1 == mobileNo).Select(e => new
            {
                PosCustomerId = e.Id,
                PosCustomerCode = e.Code,
                ArName = e.ArName
                ,
                EnName = e.EnName,
                CityId = e.CityId,
                GovernorateId = e.GovernorateId,
                AreaId = e.AreaId,
                Address = e.Address,
                e.Mobile1,
                e.Mobile2,
                e.Notes,
                CustomerId = e.CustomerId
            }).AsNoTracking().FirstOrDefault();
            var posCustomer = new { posCustomer = posCustomerInDb };

            return Json(posCustomer, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult GetCustomerByIdentityNumberOrMobileNumber(string identityNo, string mobileNo)
        {
            var posCustomerInDb = new object();
            if (identityNo != "null")
            {
                posCustomerInDb = db.Customers.Where(e => e.IsDeleted == false && e.IsActive == true && e.NationalId == identityNo).Select(e => new
                {
                    PosCustomerId = e.Id,
                    PosCustomerCode = e.Code,
                    ArName = e.ArName,
                    EnName = e.EnName,
                    CityId = e.CityId,
                    GovernorateId = e.CountryId,
                    Address = e.Address,
                    e.Mobile,
                    e.IsBlocked,
                    e.Notes,
                    e.CustomersGroupId,
                    e.NationalId
                }).AsNoTracking().FirstOrDefault();
            }
            else if (mobileNo != "null")
            {
                posCustomerInDb = db.Customers.Where(e => e.IsDeleted == false && e.IsActive == true && e.Mobile == mobileNo).Select(e => new
                {
                    PosCustomerId = e.Id,
                    PosCustomerCode = e.Code,
                    ArName = e.ArName,
                    EnName = e.EnName,
                    CityId = e.CityId,
                    GovernorateId = e.CountryId,
                    Address = e.Address,
                    e.Mobile,
                    e.IsBlocked,
                    e.Notes,
                    e.CustomersGroupId,
                    e.NationalId
                }).AsNoTracking().FirstOrDefault();
            }

            var posCustomer = new { posCustomer = posCustomerInDb };
            return Json(posCustomer, JsonRequestBehavior.AllowGet);
        }


        [SkipERPAuthorize]
        public async Task<JsonResult> GetDriversByDep(int depId)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var drivers = await db.Employees.Where(e => e.IsDeleted == false && e.IsActive == true && e.IsDriver == true && e.DepartmentId == depId).ToListAsync();
            return Json(drivers, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        [HttpPost]
        public ActionResult SaveDeliveryData(int invoiceId, DateTime? DeliveryDate, DateTime? DeliveryStartDate, DateTime? DeliveryEndDate, int? CarId, int? DriverId)
        {
            //db.Configuration.ProxyCreationEnabled = false;
            var invoice = db.SalesInvoices.Where(s => s.Id == invoiceId && s.IsDeleted == false && s.IsActive == true).FirstOrDefault();

            if (invoice != null)
            {
                invoice.DeliveryDate = DeliveryDate;
                invoice.DeliveryStartDate = DeliveryStartDate;
                invoice.DeliveryEndDate = DeliveryEndDate;

                invoice.CarId = CarId;
                invoice.DriverId = DriverId;

                db.SaveChanges();

                return Json(new { Result = true }, JsonRequestBehavior.AllowGet);

            }
            return Json(new { Result = false }, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult GetVendorOB(int? VendorId, int? DepartmentId, DateTime? DateFrom, DateTime? DateTo, int pageIndex = 1, int wantedRowsNo = 100)
        {
            db.Database.CommandTimeout = 300;
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            ViewBag.wantedRowsNo = wantedRowsNo;
            var Vendorbalance = db.VendorOB_Get(VendorId, DepartmentId, DateFrom, DateTo).ToList().Skip(skipRowsNo).Take(wantedRowsNo);
            ViewBag.Count = db.VendorOB_Get(VendorId, DepartmentId, DateFrom, DateTo).Count();
            return Json(Vendorbalance, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult GetAssemblyPartsOfItem(int ItemId)
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            var ChildItem = db.GetAssemblyPartsOfItem(ItemId).ToList();
            var ParentUnit = db.ItemPrices.Where(a => a.ItemId == ItemId && a.IsActive == true && a.IsDeleted == false && a.IsDefault == true).Select(a => new
            {
                Id = a.ItemUnitId,
                ArName = session.ToString() == "en" && a.ItemUnit.EnName != null ? a.ItemUnit.Code + " - " + a.ItemUnit.EnName : a.ItemUnit.Code + " - " + a.ItemUnit.ArName
            });
            var ParentQty = db.ItemQuantities.Where(a => a.ItemId == ItemId).Select(a => a.Qty);
            return Json(new { ChildItem = ChildItem, ParentUnit = ParentUnit, ParentQty = ParentQty }, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult GetAssemblyItemsByDepartmentId(int DepartmentId)
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            var Items = db.DepartmentItems.Where(a => a.Item.IsActive == true && a.Item.IsDeleted == false && a.DepartmentId == DepartmentId && (a.Item.ItemTypeId == 2 || a.Item.ItemTypeId == 3)).Select(b => new
            {
                Id = b.Item.Id,
                ArName = session.ToString() == "en" && b.Item.EnName != null ? b.Item.Code + " - " + b.Item.EnName : b.Item.Code + " - " + b.Item.ArName
            }).ToList();
            return Json(Items, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult GetAccountOfFixedAssetsGroup(int FixedAssetsGroupId)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var Accounts = db.FixedAssetsGroups.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == FixedAssetsGroupId)
                .Select(a => new
                {
                    a.CapitalGainsAccounts,
                    CapitalGainsAccountsName = (db.ChartOfAccounts.Where(s => s.Id == a.CapitalGainsAccounts && s.IsActive == true && s.IsDeleted == false).Select(s => new { ArName = s.Code + " - " + s.ArName })),
                    a.CapitalLossesAccounts,
                    CapitalLossesAccountsName = (db.ChartOfAccounts.Where(s => s.Id == a.CapitalLossesAccounts && s.IsActive == true && s.IsDeleted == false).Select(s => new { ArName = s.Code + " - " + s.ArName })),
                    a.ChartOfAccountsIdDepracition,
                    ChartOfAccountsIdDepracitionName = (db.ChartOfAccounts.Where(s => s.Id == a.ChartOfAccountsIdDepracition && s.IsActive == true && s.IsDeleted == false).Select(s => new { ArName = s.Code + " - " + s.ArName })),
                    a.ChartOfAccountsIdTotalDepracition,
                    ChartOfAccountsIdTotalDepracitionName = (db.ChartOfAccounts.Where(s => s.Id == a.ChartOfAccountsIdTotalDepracition && s.IsActive == true && s.IsDeleted == false).Select(s => new { ArName = s.Code + " - " + s.ArName })),
                    a.ChartOfAccountsIdOriginalAccounts,
                    ChartOfAccountsIdOriginalAccountsName = (db.ChartOfAccounts.Where(s => s.Id == a.ChartOfAccountsIdOriginalAccounts && s.IsActive == true && s.IsDeleted == false).Select(s => new { ArName = s.Code + " - " + s.ArName })),
                    a.DepreciationMethod,
                    DepreciationMethodName = a.DepreciationMethod == 0 ? "قسط ثابت" : a.DepreciationMethod == 1 ? "قسط متناقص" : "",
                    a.DepraciationRate,
                    a.IsHasNoDepreciation
                })
                .ToList();
            return Json(Accounts, JsonRequestBehavior.AllowGet);
        }



        [SkipERPAuthorize]
        public JsonResult GetTotalsForEachInvoiceType(int? DepartmentId, DateTime? DateFrom, DateTime? DateTo, int? ShiftId, int? CashierUserId)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var Totals = db.GetTotalsForEachInvoiceType(DepartmentId, DateFrom, DateTo, ShiftId, CashierUserId);
            return Json(Totals.ToList(), JsonRequestBehavior.AllowGet);
        }


        [SkipERPAuthorize]
        public JsonResult GetDepartmentEmployees(int? DepartmentId)
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            db.Configuration.ProxyCreationEnabled = false;
            var Employees = db.Employees.Where(a => a.DepartmentId == DepartmentId && a.IsActive && !a.IsDeleted).Select(a => new
            {
                Id = a.Id,
                ArName = session.ToString() == "en" && a.EnName != null ? a.Code + " - " + a.EnName : a.Code + " - " + a.ArName
            }).ToList();
            return Json(Employees, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public static string EncodeTLV(string Seller, string TaxNumber, string InvoiceDate, string InvoiceTotalAmount, string InvoiceTaxAmount)
        {
            //db.Configuration.ProxyCreationEnabled = false;

            /* string Seller = "Bobs Records";
             string TaxNumber = "310122393500003";
             string InvoiceDate = "2022-04-25T15:30:00Z";
             string InvoiceTotalAmount = "1000.00";
             string InvoiceTaxAmount = "150.00";*/

            InvoiceDate = Convert.ToDateTime(InvoiceDate).ToString("s");

            // Seller 
            byte[] Sellerbytes = Encoding.UTF8.GetBytes(Seller);
            string SellerhexString = BitConverter.ToString(Sellerbytes);
            SellerhexString = (String.Format("0{0:X}", 1) + (Seller.Length < 16 ? String.Format("0{0:X}", Seller.Length) : String.Format("{0:X}", Seller.Length)) + SellerhexString).Replace("-", "");
            //var SellerToBase64 = Convert.ToBase64String(Sellerbytes);

            // TaxNumber 
            byte[] TaxNumberbytes = Encoding.UTF8.GetBytes(TaxNumber);
            string TaxNumberhexString = BitConverter.ToString(TaxNumberbytes);
            TaxNumberhexString = (String.Format("0{0:X}", 2) + (TaxNumber.Length < 16 ? String.Format("0{0:X}", TaxNumber.Length) : String.Format("{0:X}", TaxNumber.Length)) + TaxNumberhexString).Replace("-", "");
            //var TaxNumberToBase64 = Convert.ToBase64String(TaxNumberbytes);

            // InvoiceDate 
            byte[] InvoiceDatebytes = Encoding.UTF8.GetBytes(InvoiceDate);
            string InvoiceDatehexString = BitConverter.ToString(InvoiceDatebytes);
            InvoiceDatehexString = (String.Format("0{0:X}", 3) + (InvoiceDate.Length < 16 ? String.Format("0{0:X}", InvoiceDate.Length) : String.Format("{0:X}", InvoiceDate.Length)) + InvoiceDatehexString).Replace("-", "");
            //var InvoiceDateToBase64 = Convert.ToBase64String(InvoiceDatebytes);

            // InvoiceTotalAmount 
            byte[] InvoiceTotalAmountbytes = Encoding.UTF8.GetBytes(InvoiceTotalAmount);
            string InvoiceTotalAmounthexString = BitConverter.ToString(InvoiceTotalAmountbytes);
            InvoiceTotalAmounthexString = (String.Format("0{0:X}", 4) + (InvoiceTotalAmount.Length < 16 ? String.Format("0{0:X}", InvoiceTotalAmount.Length) : String.Format("{0:X}", InvoiceTotalAmount.Length)) + InvoiceTotalAmounthexString).Replace("-", "");
            //var InvoiceTotalAmountToBase64 = Convert.ToBase64String(InvoiceTotalAmountbytes);

            // InvoiceTaxAmount 
            byte[] InvoiceTaxAmountbytes = Encoding.UTF8.GetBytes(InvoiceTaxAmount);
            string InvoiceTaxAmounthexString = BitConverter.ToString(InvoiceTaxAmountbytes);
            InvoiceTaxAmounthexString = (String.Format("0{0:X}", 5) + (InvoiceTaxAmount.Length < 16 ? String.Format("0{0:X}", InvoiceTaxAmount.Length) : String.Format("{0:X}", InvoiceTaxAmount.Length)) + InvoiceTaxAmounthexString).Replace("-", "");
            //var InvoiceTaxAmountToBase64 = Convert.ToBase64String(InvoiceTaxAmountbytes);


            var TotalString = SellerhexString + TaxNumberhexString + InvoiceDatehexString + InvoiceTotalAmounthexString + InvoiceTaxAmounthexString;

            byte[] raw = new byte[TotalString.Length / 2];
            for (int i = 0; i < raw.Length; i++)
            {
                raw[i] = Convert.ToByte(TotalString.Substring(i * 2, 2), 16);
            }
            var ToBase64 = Convert.ToBase64String(raw);

            return ToBase64;
        }

        [SkipERPAuthorize]
        public ActionResult GetItemTransaction(int? itemId, int? departmentId, int? warehouseId, DateTime? dateFrom, DateTime? dateTo, int? itemCategoryId, int? itemGroupId, string docType, int? ActivityId, int? CompanyId, int pageIndex = 1, int wantedRowsNo = 100)
        {
            //db.Database.CommandTimeout = 300;
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            ViewBag.wantedRowsNo = wantedRowsNo;
            var ItemTransaction = db.GetItemTransactions(itemId, dateFrom, dateTo, departmentId, docType, warehouseId, itemGroupId, itemCategoryId, ActivityId, CompanyId).ToList().Skip(skipRowsNo).Take(wantedRowsNo);
            ViewBag.Count = db.GetItemTransactions(itemId, dateFrom, dateTo, departmentId, docType, warehouseId, itemGroupId, itemCategoryId,ActivityId,CompanyId).Count();
            return Json(ItemTransaction, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public ActionResult GetCashBoxBalanceSheet(int? cashBoxId, DateTime? dateFrom, DateTime? dateTo, int pageIndex = 1, int wantedRowsNo = 100)
        {
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            ViewBag.wantedRowsNo = wantedRowsNo;
            var cashBoxBalanceSheet = db.CashBox_BalanceSheet(cashBoxId, dateFrom, dateTo).ToList().Skip(skipRowsNo).Take(wantedRowsNo);
            ViewBag.Count = db.CashBox_BalanceSheet(cashBoxId, dateFrom, dateTo).Count();
            return Json(cashBoxBalanceSheet, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public ActionResult GetVendorCurrentBalance(int? departmentId, int? ActivityId, int? CompanyId, int pageIndex = 1, int wantedRowsNo = 100)
        {
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            ViewBag.wantedRowsNo = wantedRowsNo;
            var VendorCurrentBalance = db.VendorOB_GetAll(departmentId, ActivityId, CompanyId).ToList().Skip(skipRowsNo).Take(wantedRowsNo);
            ViewBag.Count = db.VendorOB_GetAll(departmentId, ActivityId, CompanyId).Count();
            return Json(VendorCurrentBalance, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public ActionResult SendDailyReportToAdmin(bool? sendReport)
        {
            if (sendReport == true)
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


                // -- Cheque -- //
                var chequeIssue = db.ChequeIssues.Where(a => a.IsActive == true && a.IsDeleted == false && a.DueDate.Value.Year == cTime.Year && (a.DueDate.Value.Month == cTime.Month || a.DueDate.Value.Month <= cTime.Month) && (a.DueDate.Value.Day == cTime.Day || cTime.Day - a.DueDate.Value.Day <= 7)).ToList();
                var chequeReceipt = db.ChequeReceipts.Where(a => a.IsActive == true && a.IsDeleted == false && a.DueDate.Value.Year == cTime.Year && (a.DueDate.Value.Month == cTime.Month || a.DueDate.Value.Month <= cTime.Month) && (a.DueDate.Value.Day == cTime.Day || cTime.Day - a.DueDate.Value.Day <= 7)).ToList();
                //-----------------------------------------------------------------------------------------//
                // -- CashBox -- //
                decimal? cashboxBalance = 0;
                var cashBoxsids = db.CashBoxes.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => a.Id).ToList();
                foreach (var id in cashBoxsids)
                {
                    var Cash_balance = db.CashBox_Balances(id).FirstOrDefault();
                    var balance = Cash_balance.Balance;
                    cashboxBalance += balance;
                }
                //-----------------------------------------------------------------------------------------//
                // -- DailySales -- //
                var dailysales = db.SalesInvoices.Where(a => a.IsActive == true && a.IsDeleted == false
                && a.VoucherDate.Year == cTime.Year && a.VoucherDate.Month == cTime.Month && a.VoucherDate.Day == cTime.Day
                && a.SalesInvoicePaymentMethods.Where(b => b.SalesInvoiceId == a.Id)
                .FirstOrDefault().PaymentMethodId == db.PaymentMethods.Where(p => p.IsActive == true && p.IsDeleted == false)
                .FirstOrDefault().Id).ToList();
                decimal? CashDailySales = 0;
                foreach (var sale in dailysales)
                {
                    CashDailySales += sale.SalesInvoicePaymentMethods.Where(a => a.SalesInvoiceId == sale.Id
                    && a.PaymentMethodId == db.PaymentMethods.Where(p => p.IsActive == true && p.IsDeleted == false)
                    .FirstOrDefault().Id).FirstOrDefault().Amount;
                }
                // -- DailyReturns -- //
                var dailyReturns = db.SalesReturns.Where(a => a.IsActive == true && a.IsDeleted == false
                && a.VoucherDate.Year == cTime.Year && a.VoucherDate.Month == cTime.Month && a.VoucherDate.Day == cTime.Day
                && a.SalesReturnPaymentMethods.Where(b => b.SalesReturnsId == a.Id)
                .FirstOrDefault().PaymentMethodId == db.PaymentMethods.Where(p => p.IsActive == true && p.IsDeleted == false)
                .FirstOrDefault().Id).ToList();
                decimal? CashdailyReturns = 0;
                foreach (var returns in dailyReturns)
                {
                    CashdailyReturns += returns.SalesReturnPaymentMethods.Where(a => a.SalesReturnsId == returns.Id
                    && a.PaymentMethodId == db.PaymentMethods.Where(p => p.IsActive == true && p.IsDeleted == false)
                    .FirstOrDefault().Id).FirstOrDefault().Amount;
                }
                // -- AllDailySales -- //
                var AllDailySales = db.SalesInvoices.Where(a => a.IsActive == true && a.IsDeleted == false
                && a.VoucherDate.Year == cTime.Year && a.VoucherDate.Month == cTime.Month && a.VoucherDate.Day == cTime.Day
                && a.SalesInvoicePaymentMethods.Where(b => b.SalesInvoiceId == a.Id).Any(b => b.SalesInvoiceId == a.Id)).ToList();
                decimal? AllDailySalesMethods = 0;
                foreach (var sale in AllDailySales)
                {
                    AllDailySalesMethods += sale.TotalAfterTaxes;
                }
                // -- AllDailyReturns -- //
                var AllDailySalesReturn = db.SalesReturns.Where(a => a.IsActive == true && a.IsDeleted == false
                && a.VoucherDate.Year == cTime.Year && a.VoucherDate.Month == cTime.Month && a.VoucherDate.Day == cTime.Day
                 && a.SalesReturnPaymentMethods.Where(b => b.SalesReturnsId == a.Id).Any(b => b.SalesReturnsId == a.Id)).ToList();
                decimal? AllDailySalesReturnMethods = 0;
                foreach (var returns in AllDailySalesReturn)
                {
                    AllDailySalesReturnMethods += returns.TotalAfterTaxes;
                }
                var ActualDailyCashSales = CashDailySales - CashdailyReturns;
                var AllActualDailySales = AllDailySalesMethods - AllDailySalesReturnMethods;
                //-----------------------------------------------------------------------------------------//
                var Msg = "<< استحقاق الشيكات >>\r\n" +
                    "الشيكات المطلوب تحصيلها : " + chequeReceipt.Count() + "\r\n" +
                    "الشيكات المطلوب دفعها : " + chequeIssue.Count() + "\r\n" +
                    "--------------------------------------------------- " + "\r\n" +
                    "إجمالى أرصدة الصناديق : " + cashboxBalance + "\r\n" +
                    "--------------------------------------------------- " + "\r\n" +
                    "<< إجمالى المبيعات اليومية >>\r\n" +
                    "جمالى المبيعات النقدية : " + ActualDailyCashSales + "\r\n" +
                    "إجمالى المبيعات  : " + AllActualDailySales + "\r\n";
                //-----------------------------------------------------------------------------------------//

                var UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                var userEmail = db.ERPUsers.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == UserId).FirstOrDefault().Email;
                var Admin = db.ERPUsers.FirstOrDefault();
                var AdminEmail = Admin.Email != null ? Admin.Email : "mysoft2022.eg@gmail.com";
                var senderEmail = new MailAddress(AdminEmail, "MySoft");
                var receiverEmail = new MailAddress(userEmail, "Receiver");
                //var Emailpassword = "Mysoft@123";
                var Emailpassword = Admin.AppPassword != null ? Admin.AppPassword : "bpnpqfhpeckovckl";
                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(senderEmail.Address, Emailpassword)
                };
                using (var mess = new MailMessage(senderEmail, receiverEmail)
                {
                    Subject = "Daily Report",
                    Body = Msg
                })
                {
                    smtp.Send(mess);
                }
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            return View();
        }

        [SkipERPAuthorize]
        public ActionResult GetDepartmentEmployeeSalary(int? DepartmentId, int? Month, int? Year)
        {
            var Employees = db.GetDepartmentEmployeeSalary(DepartmentId, Month, Year).ToList();
            return Json(Employees, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public ActionResult GetCustomerCurrentBalance(int? DepartmentId, bool? ShowDepartmentCustomer, int? ActivityId, int? CompanyId)
        {
            var customers = db.CustomerOB_GetAll(DepartmentId, ShowDepartmentCustomer, ActivityId, CompanyId).ToList();
            return Json(customers, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public ActionResult GetCustomerOB(int? customerId, int? departmentId, DateTime? dateFrom, DateTime? dateTo, int? ActivityId, int? CompanyId)
        {
            var customers = db.CustomerOB_Get(customerId, departmentId, ActivityId, CompanyId).Where(a => (a.Date >= dateFrom || dateFrom == null) && (dateTo == null || a.Date <= dateTo)).ToList();
            return Json(customers, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public ActionResult GetDebitAgesTotal(int? DepartmentId, int? CustomerId, int? ActivityId, int? CompanyId)
        {
            var TotalDebit = db.DebitAgesTotal_Get(DepartmentId, CustomerId, ActivityId, CompanyId).ToList();
            return Json(TotalDebit, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public ActionResult GetDebitAgesDetails(int? DepartmentId, int? CustomerId, int? RepId, int? ActivityId, int? CompanyId)
        {
            var DebitDetails = db.DebitAgesDetails_Get(DepartmentId, CustomerId, RepId, ActivityId, CompanyId).ToList();
            return Json(DebitDetails, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public ActionResult GetVendorDebitAgesTotal(int? DepartmentId, int? VendorId, int? ActivityId, int? CompanyId)
        {
            var VendorTotalDebit = db.VendorDebitAgesTotal_Get(DepartmentId, VendorId, ActivityId, CompanyId).ToList();
            return Json(VendorTotalDebit, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public ActionResult GetVendorDebitAgesDetails(int? DepartmentId, int? VendorId, int? ActivityId, int? CompanyId)
        {
            var VendorDebitDetails = db.VendorDebitAgesDetails_Get(DepartmentId, VendorId, ActivityId, CompanyId).ToList();
            return Json(VendorDebitDetails, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public ActionResult GetEmployeeDues(int? DepartmentId, int? Year, int? Month, int? ActivityId, int? CompanyId)
        {
            var EmployeeDues = db.GetEmployeeDues(DepartmentId, Year, Month, ActivityId, CompanyId).ToList();
            return Json(EmployeeDues, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public ActionResult GetEmployeeMonthlyAllocation(int? DepartmentId, int? AllocationType, int? Year, int? Month, int? ActivityId, int? CompanyId)
        {
            var EmployeeMonthlyAllocation = db.GetEmployeeMonthlyAllocationReport(DepartmentId, AllocationType, Year, Month, ActivityId, CompanyId).ToList();
            return Json(EmployeeMonthlyAllocation, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public ActionResult GetEmployeesNotInPayrollIssue(int? DepartmentId, int? Year, int? Month, int? ActivityId, int? CompanyId)
        {
            var EmployeeMonthlyAllocation = db.GetEmployeesNotInPayrollIssue(DepartmentId, Year, Month, ActivityId, CompanyId).ToList();
            return Json(EmployeeMonthlyAllocation, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public ActionResult GetCashIssueAndReceipt(int? departmentId, DateTime? dateFrom, DateTime? dateTo, int? cashBoxId, int? ActivityId, int? CompanyId)
        {
            var Receipt = db.CashIssueAndReceipt_Get(departmentId, dateFrom, dateTo, cashBoxId, ActivityId, CompanyId).ToList();
            return Json(Receipt, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public ActionResult GetChartOfAccount(int? TypeId, int? ClassificationId, int? CategoryId, int? AccountId)
        {
            var ChartOfAccount = db.ChartOfAccount_Get(TypeId, ClassificationId, CategoryId, AccountId).ToList();
            return Json(ChartOfAccount, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public ActionResult GetAccountStatement(DateTime? From, DateTime? To, int? AccountId, int? DepartmentId, int? ActivityId, int? CompanyId)
        {
            var AccountStatement = db.GetAccountStatement(From, To, AccountId, DepartmentId,ActivityId,CompanyId).ToList();
            return Json(AccountStatement, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public ActionResult GetAccountStatementDetails(DateTime? From, DateTime? To, int? AccountId, int? DepartmentId, int? ActivityId, int? CompanyId)
        {
            var AccountStatementDetails = db.GetAccountStatementDetails(From, To, AccountId, DepartmentId, ActivityId, CompanyId).ToList();
            return Json(AccountStatementDetails, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public ActionResult GetItems(int? DepartmentId, int? GroupId, int? CategoryId, int? ActivityId, int? CompanyId)
        {
            var items = db.Items_Get(DepartmentId, GroupId, CategoryId,ActivityId,CompanyId).ToList();
            return Json(items, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public ActionResult GetItemQuantities(int? DepartmentId, int? WarehouseId, int? GroupId, int? CategoryId, int? CustomerGroupId, int? ActivityId, int? CompanyId)
        {
            var items = db.GetItemQuantities(DepartmentId, WarehouseId, GroupId, CategoryId, CustomerGroupId,ActivityId,CompanyId).ToList();
            return Json(items, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public ActionResult GetWarehouseTransactionsInPeriod(int? WarehouseId, DateTime? From, DateTime? To, string TransactionType)
        {
            var Transactions = db.GetWarehouseTransactionsInPeriod(WarehouseId, From, To, TransactionType).ToList();
            return Json(Transactions, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public ActionResult GetIncomeList(int? DepartmentId, DateTime? From, DateTime? To, int? ActivityId, int? CompanyId)
        {
            var List = db.GetIncomeList(DepartmentId, From, To,ActivityId,CompanyId).ToList();
            return Json(List, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public ActionResult GetFinancialStatement(int? DepartmentId, DateTime? From, DateTime? To, int? ActivityId, int? CompanyId)
        {
            var FinancialStatement = db.GetFinancialStatement(DepartmentId, From, To, ActivityId, CompanyId).ToList();
            return Json(FinancialStatement, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public ActionResult GetBalanceReview(int? DepartmentId, DateTime? From, DateTime? To, int? ActivityId, int? CompanyId, int? AccountId, string ReportType = "General")
        {
            var BalanceReview = db.GetBalanceReview(From, To, DepartmentId, ActivityId, CompanyId, AccountId, ReportType).ToList();
            return Json(BalanceReview, JsonRequestBehavior.AllowGet);
        }
        
        public ActionResult GetBalanceReview2(int? DepartmentId, DateTime? From, DateTime? To, int? ActivityId, int? CompanyId, int? AccountId, string ReportType = "General")
        {
            var BalanceReviewDetails = db.GetBalanceReview(From, To, DepartmentId, ActivityId, CompanyId, AccountId, ReportType).ToList();
            return Json(BalanceReviewDetails, JsonRequestBehavior.AllowGet);
        }



        [SkipERPAuthorize]
        public ActionResult GetCostCenterTrialBalance(int? DepartmentId, DateTime? From, DateTime? To, int? ActivityId, int? CompanyId)
        {
            var Balance = db.CostCenter_TrialBalance(From, To, DepartmentId, ActivityId, CompanyId).ToList();
            return Json(Balance, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public ActionResult GetStockTransferDetailsInPeriod(int? WarehouseId, int? DestinationWarehouseId, DateTime? From, DateTime? To)
        {
            var Details = db.GetStockTransferDetailsInPeriod(WarehouseId, DestinationWarehouseId, From, To).ToList();
            return Json(Details, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public ActionResult GetItemPrice(int? DepartmentId, int? GroupId, int? CategoryId, int? CustomerGroupId, int? ActivityId, int? CompanyId)
        {
            var ItemPrice = db.GetItemPrice(DepartmentId, GroupId, CategoryId, CustomerGroupId, ActivityId, CompanyId).ToList();
            return Json(ItemPrice, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public ActionResult GetSlackItems(int? DepartmentId, DateTime? From, DateTime? To, int? GroupId, int? CategoryId, int? ActivityId, int? CompanyId)
        {
            var SlackItems = db.GetSlackItems(DepartmentId, From, To, GroupId, CategoryId, ActivityId, CompanyId).ToList();
            return Json(SlackItems, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public ActionResult GetItemShortage(int? DepartmentId, int? WarehouseId, int? GroupId, int? CategoryId, int? ActivityId, int? CompanyId)
        {
            var ItemShortage = db.ItemShortage(DepartmentId, WarehouseId, GroupId, CategoryId, ActivityId, CompanyId).ToList();
            return Json(ItemShortage, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public ActionResult GetInventoryEvaluation(int? DepartmentId, int? WarehouseId, int? GroupId, int? ActivityId, int? CompanyId)
        {
            var InventoryEvaluation = db.GetInventoryEvaluation(DepartmentId, WarehouseId, GroupId, ActivityId, CompanyId).ToList();
            return Json(InventoryEvaluation, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public ActionResult GetStockIssueVoucher(int? id, int? deptId, string docNo, DateTime? From, DateTime? To, int? ActivityId, int? CompanyId)
        {
            var StockIssueVoucher = db.StockIssueVoucher_Get(docNo, deptId, From, To, id,ActivityId,CompanyId).ToList();
            if (docNo.Length > 0 && StockIssueVoucher != null)
            {
                var stock = db.StockIssueVoucherDetails_Get(StockIssueVoucher.FirstOrDefault().Id, deptId);
                return Json(stock, JsonRequestBehavior.AllowGet);
            }
            else
            {
                var stock = db.StockIssueVoucherDetails_Get(null, deptId);
                return Json(stock, JsonRequestBehavior.AllowGet);
            }
        }

        [SkipERPAuthorize]
        public ActionResult GetNearlyExpiredPatches(int? DaysNo, int? WarehouseId, DateTime? From, DateTime? To, int? DepartmentId, int? ActivityId, int? CompanyId)
        {
            var NearlyExpiredPatches = db.GetNearlyExpiredPatches(DaysNo, WarehouseId, From, To, DepartmentId, ActivityId, CompanyId).ToList();
            return Json(NearlyExpiredPatches, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public ActionResult GetVendorCurrency(int? VendorId)
        {
            var Vendor = db.Vendors.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == VendorId).FirstOrDefault();
            var CurrencyId = Vendor != null ? Vendor.CurrencyId : null;
            var CurrencyName = Vendor.Currency != null ? Vendor.Currency.ArName : null;
            var CurrencyEquivalent = Vendor.Currency != null ? Vendor.Currency.Equivalent : null;
            return Json(new { CurrencyId = CurrencyId, CurrencyName = CurrencyName, CurrencyEquivalent = CurrencyEquivalent }, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public ActionResult GetReservedRooms(int? BuildingId, int? RoomId, DateTime? DateFrom, DateTime? DateTo, int pageIndex = 1, int wantedRowsNo = 100)
        {
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            ViewBag.wantedRowsNo = wantedRowsNo;
            var ReservedRooms = db.GetReservedRooms(BuildingId, RoomId, DateFrom, DateTo).ToList().Skip(skipRowsNo).Take(wantedRowsNo);
            ViewBag.Count = db.GetReservedRooms(BuildingId, RoomId, DateFrom, DateTo).Count();
            return Json(ReservedRooms, JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetRoomLeavePermissionInDate(DateTime? Date/*, int pageIndex = 1, int wantedRowsNo = 100*/)
        {
            //ViewBag.PageIndex = pageIndex;
            //int skipRowsNo = 0;

            //if (pageIndex > 1)
            //    skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            //ViewBag.wantedRowsNo = wantedRowsNo;
            var RoomLeavePermissionInDate = db.GetRoomLeavePermissionInDate(Date).ToList()/*.Skip(skipRowsNo).Take(wantedRowsNo)*/;
            //ViewBag.Count = db.GetRoomLeavePermissionInDate(Date).Count();
            return Json(RoomLeavePermissionInDate, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public ActionResult GetReservedMeals(DateTime? DateFrom, DateTime? DateTo, int pageIndex = 1, int wantedRowsNo = 100)
        {
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            ViewBag.wantedRowsNo = wantedRowsNo;
            var ReservedMeals = db.GetReservedMeals(DateFrom, DateTo).ToList().Skip(skipRowsNo).Take(wantedRowsNo);
            ViewBag.Count = db.GetReservedMeals(DateFrom, DateTo).Count();
            return Json(ReservedMeals, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public ActionResult GetDayUseReservation(DateTime? DateFrom, DateTime? DateTo, int pageIndex = 1, int wantedRowsNo = 100)
        {
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            ViewBag.wantedRowsNo = wantedRowsNo;
            var DayUseReservation = db.GetDayUseReservation(DateFrom, DateTo).ToList().Skip(skipRowsNo).Take(wantedRowsNo);
            ViewBag.Count = db.GetDayUseReservation(DateFrom, DateTo).Count();
            return Json(DayUseReservation, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public ActionResult GetHotelManagementCarEntry(DateTime? DateFrom, DateTime? DateTo, int pageIndex = 1, int wantedRowsNo = 100)
        {
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            ViewBag.wantedRowsNo = wantedRowsNo;
            var HotelManagementCarEntry = db.GetHotelManagementCarEntry(DateFrom, DateTo).ToList().Skip(skipRowsNo).Take(wantedRowsNo);
            ViewBag.Count = db.GetHotelManagementCarEntry(DateFrom, DateTo).Count();
            return Json(HotelManagementCarEntry, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        [AllowAnonymous]
        public ActionResult EncodeId(int id)
        {
            try
            {
                byte[] inputByteArray = Encoding.UTF8.GetBytes(id.ToString());
                byte[] rgbIV = { 0x21, 0x43, 0x56, 0x87, 0x10, 0xfd, 0xea, 0x1c };
                byte[] key = { };
                key = Encoding.UTF8.GetBytes("Z4a2rX3T");
                DESCryptoServiceProvider des = new DESCryptoServiceProvider();
                MemoryStream ms = new MemoryStream();
                CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(key, rgbIV), CryptoStreamMode.Write);
                cs.Write(inputByteArray, 0, inputByteArray.Length);
                cs.FlushFinalBlock();
                var str = Convert.ToBase64String(ms.ToArray()).Replace("+", "_pl_");//.Replace("=", "_eq_").Replace("/", "_sl_").Replace(@"\", "_bsl_");
                return Json(str, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return null;
            }
        }
        [SkipERPAuthorize]
        public ActionResult GetDoctorBalanceSheet(int? DoctorId, DateTime? dateFrom, DateTime? dateTo)
        {
            var Doctors = db.GetDoctorBalanceSheet(DoctorId, dateFrom, dateTo).ToList();
            return Json(Doctors, JsonRequestBehavior.AllowGet);
        }
        [AllowAnonymous]
        [SkipERPAuthorize]
        public JsonResult GetSalesInvoiceDetails()
        {
            //select first id from salesInvoicePrintQuee
            //select salesInvoiceId by salesInvoicePrintQueeId
            //delete salesInvoicePrintQueeId from salesInvoicePrintQuee 

            var salesInvoicePrintQueue = db.SalesInvoicePrintQueues.FirstOrDefault();
            if (salesInvoicePrintQueue != null)
            {
                var salesInvoicePrintQueueId = salesInvoicePrintQueue.Id;
                var salesInvoiceId = salesInvoicePrintQueue.SalesInvoiceId;
                var printOnKitchen = salesInvoicePrintQueue.PrintOnKitchen;
                var PrintInvoice = salesInvoicePrintQueue.PrintInvoice;
                var Invoice = db.SalesInvoice_Get(null, null, null, null, null, salesInvoiceId).FirstOrDefault();
                var Items = db.SalesInvoiceDetails.Where(a => a.IsDeleted == false && a.MainDocId == salesInvoiceId).Select(a => new
                {
                    ItemName = a.Item.ArName,
                    ItemCode = a.Item.Code,
                    a.UnitEquivalent,
                    ItemUnitArName = a.ItemUnit.ArName,
                    a.Qty,
                    Price = a.Price - (a.DiscountValue != null ? a.DiscountValue : 0),
                    a.Item.ItemGroupId,
                    ItemGroup = a.Item.ItemGroup.ArName,
                    PrinterName = a.Item.ItemGroup.PrinterName,
                    ItemNote = a.Notes,
                    a.IsAdded
                });
                var PaymentMethod = db.SalesInvoicePaymentMethods.Where(a => a.SalesInvoiceId == salesInvoiceId && a.Amount > 0).Select(a => new
                {
                    a.Amount,
                    PaymentMethodName = a.PaymentMethod.ArName,
                }).ToList();
                var logo = db.SystemSettings.FirstOrDefault().Logo;
                var PosId = db.SalesInvoices.Find(Invoice.Id).PosId != null ? db.SalesInvoices.Find(Invoice.Id).PosId : 0;
                var CashierPrinter = db.Pos.Where(a => a.Id == PosId).FirstOrDefault() != null ? db.Pos.Where(a => a.Id == PosId).FirstOrDefault().Printer : null;
                ArrayList printers = new ArrayList();
                foreach (var item in Items)
                {
                    if (salesInvoicePrintQueue.IsUpdated == true && item.IsAdded == true || salesInvoicePrintQueue.IsUpdated != true)
                    {
                        if (!printers.Contains(item.PrinterName) && item.PrinterName != null)//and not equal null
                            printers.Add(item.PrinterName);
                    }
                }
                List<KitchenItem> kitchenItemsArray = new List<KitchenItem>();
                foreach (var printer in printers)
                {
                    KitchenItem kitchenItem = new KitchenItem()
                    {
                        PrinterName = printer.ToString(),
                        DocumentNumber = Invoice.DocumentNumber,
                        Logo = logo,
                        VoucherDate = Invoice.VoucherDate.ToString()
                    };
                    List<ItemsToBePrintedAtKitchen> kitchenItemsList = new List<ItemsToBePrintedAtKitchen>();
                    foreach (var item in Items)
                    {
                        if (salesInvoicePrintQueue.IsUpdated == true && item.IsAdded == true || salesInvoicePrintQueue.IsUpdated != true)
                        {
                            if (item.PrinterName == kitchenItem.PrinterName)
                            {
                                ItemsToBePrintedAtKitchen itemsToBePrintedAtKitchen = new ItemsToBePrintedAtKitchen()
                                {
                                    Item = item.ItemName,
                                    Qty = (float)item.Qty,
                                    ItemNote = item.ItemNote
                                };
                                kitchenItemsList.Add(itemsToBePrintedAtKitchen);
                            }
                        }
                    }
                    kitchenItem.KitchenItems = kitchenItemsList;
                    kitchenItemsArray.Add(kitchenItem);
                }
                db.SalesInvoicePrintQueues.Remove(salesInvoicePrintQueue);
                db.SaveChanges();
                return Json(new { Invoice, Items, PaymentMethod, salesInvoiceId, logo, kitchenItemsArray, PrintInvoice, printOnKitchen, CashierPrinter, VoucherDate = Invoice.VoucherDate.ToString() }, JsonRequestBehavior.AllowGet);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }
        public class KitchenItem
        {
            public string DocumentNumber { get; set; }
            public string VoucherDate { get; set; }
            public string Logo { get; set; }
            public string PrinterName { get; set; }
            public List<ItemsToBePrintedAtKitchen> KitchenItems { get; set; }
        }
        public class ItemsToBePrintedAtKitchen
        {
            public string Item { get; set; }
            public string ItemCode { get; set; }
            public double UnitEquivalent { get; set; }
            public string ItemUnitArName { get; set; }
            public float Qty { get; set; }
            public decimal Price { get; set; }
            public string ItemGroup { get; set; }
            public int ItemGroupId { get; set; }
            public string PrinterName { get; set; }
            public string ItemNote { get; set; }
            public bool IsAdded { get; set; }

        }
        [AllowAnonymous]
        [SkipERPAuthorize]
        public JsonResult CountCurrency()
        {
            var Count = db.Currencies.Count();
            return Json(Count, JsonRequestBehavior.AllowGet);
        }
        [AllowAnonymous]
        [SkipERPAuthorize]
        public JsonResult GetItemBarcodesByItemId(int? ItemId)
        {
            var Barcodes = db.ItemPrices.Where(a => a.IsActive == true && a.IsDeleted == false && a.ItemId == ItemId).Select(a => new
            {
                a.Id,
                a.Barcode
            }).ToList();
            return Json(Barcodes, JsonRequestBehavior.AllowGet);
        }

        [AllowAnonymous]
        [SkipERPAuthorize]
        public JsonResult GetItemsPrintBarcodeQueue()
        {
            //select first id from ItemsPrintBarcodeQueue
            //select ItemId by ItemsPrintBarcodeQueueId
            //delete ItemsPrintBarcodeQueueId from ItemsPrintBarcodeQueue 
            var itemsBarcodePrinterName = db.SystemSettings.FirstOrDefault().ItemsBarcodePrinterName;
            var itemsPrintBarcodeQueue = db.ItemsPrintBarcodeQueues.FirstOrDefault();
            if (itemsPrintBarcodeQueue != null)
            {
                var ItemsPrintBarcodeQueue = db.ItemsPrintBarcodeQueues.Select(a => new
                {
                    ItemArName = a.Item.Code + " - " + a.Item.ArName,
                    a.ItemPrice.Barcode,
                    a.NumberOfPrintedCopies,
                    a.ItemPrice.Price,
                }).ToList();
                db.ItemsPrintBarcodeQueues.Remove(itemsPrintBarcodeQueue);
                db.SaveChanges();
                return Json(new { ItemsPrintBarcodeQueue, PrinterName = itemsBarcodePrinterName }, JsonRequestBehavior.AllowGet);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }
        [AllowAnonymous]
        [SkipERPAuthorize]
        public JsonResult GetLogo()
        {
            var logo = db.SystemSettings.FirstOrDefault().Logo;
            return Json(logo, JsonRequestBehavior.AllowGet);
        }
        [AllowAnonymous]
        [SkipERPAuthorize]
        public JsonResult CheckCurrentUserPassWord(string Password)
        {
            //var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            //var CurrentUser = db.ERPUsers.Where(u => u.Id == userId).FirstOrDefault().UserName;
            //ObjectParameter HashPW = new ObjectParameter("HashPW", typeof(string));
            //db.ERPUser_GetHashPw(CurrentUser, HashPW);
            //string strHashPw = HashPW.Value.ToString();
            //bool authenticated = PasswordEncrypt.VerifyHashPwd(Password, strHashPw);
            if (Password != "MySoftPassword@01220779491")
            {
                return Json(new { success = "PasswordIncorrect" }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { success = "success" }, JsonRequestBehavior.AllowGet);

        }
        [SkipERPAuthorize]
        public ActionResult ChurchMembershipVisitInPeriod(DateTime? From, DateTime? To)
        {
            var Visits = db.GetChurchMembershipVisitInPeriod(From, To).ToList();
            return Json(Visits, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public ActionResult GetAlternativeItems(int? MainItemId)
        {
            //var AlternativeItems = db.AlternativeItems.Where(a => a.ItemId == MainItemId && a.IsDeleted == false).Select(a => new {
            //    MainItemId = a.MainItemId,
            //    ItemId = a.ItemId,
            //    ItemName=a.Item1.ArName,
            //    ItemPrice = a.Item1.ItemPrices.Where(s=>s.IsDefault==true).FirstOrDefault().Price,
            //    ItemPriceId = a.Item1.ItemPrices.Where(s=>s.IsDefault==true).FirstOrDefault().Id,
            //}).ToList();
            var AlternativeItems = db.GetAlternativeItemsByMainItemId(MainItemId).ToList();
            return Json(AlternativeItems, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// -------------------------  ChurchMeeting Api ----------------------------------
        [AllowAnonymous]
        [SkipERPAuthorize]
        public JsonResult SearchFamilyMemberByMobileNo(string Mobile)
        {
            var member = db.ChurchMemberships.Where(a => a.IsActive == true && a.IsDeleted == false && a.Mobile == Mobile).Select(a => new
            {
                a.Id,
                a.Code,
                a.ArName,
                BirthDate = a.BirthDate.Value.ToString().Replace(@"\", ""),
                a.Mobile,
                a.Notes,
                a.Image,
                a.Address,
                a.TopicsForDiscussion,
                a.Email,
                a.PlayerId,
                a.Password,
                //a.ConfessionFather,
                ConfessionFather = a.ChurchFather.ArName,
                Gender = a.Gender.ArName,
                NoOfDays = SqlFunctions.DateDiff("day", a.ChurchMeetings.Where(m => m.ChurchMembershipId == a.Id && !m.IsDeleted).OrderByDescending(m => m.Date).FirstOrDefault().Date, DateTime.Now)
            }).ToList();
            return Json(member, JsonRequestBehavior.AllowGet);
        }
        [AllowAnonymous]
        [SkipERPAuthorize]
        public JsonResult GetPreviousChurchMeeting(int? ChurchMembershipId)
        {
            var PrevMeetings = db.ChurchMeetings.Where(a => a.ChurchMembershipId == ChurchMembershipId && !a.IsDeleted).Select(a => new
            {
                Date = a.Date.Value.ToString().Replace(@"\", ""),
                //a.ConfessionFather,
                ConfessionFather = a.ChurchMembership.ChurchFather.ArName,
                a.Notes
            }).ToList().OrderByDescending(s => s.Date);
            return Json(PrevMeetings, JsonRequestBehavior.AllowGet);
        }
        [AllowAnonymous]
        [SkipERPAuthorize]
        public JsonResult ChangeMemberTopicsForDiscussion(int? ChurchMembershipId, string TopicsForDiscussion)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var member = db.ChurchMemberships.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == ChurchMembershipId).FirstOrDefault();
            if (member != null)
            {
                member.TopicsForDiscussion = TopicsForDiscussion;
                db.Entry(member).State = EntityState.Modified;
                db.SaveChanges();
            }
            return Json(member, JsonRequestBehavior.AllowGet);
        }
        [AllowAnonymous]
        [SkipERPAuthorize]
        public JsonResult GetSpiritualNotes()
        {
            var spiritualNotes = db.SpiritualNotes.Select(a => new { a.Code, a.ArName, a.EnName, a.Id }).ToList();
            return Json(spiritualNotes, JsonRequestBehavior.AllowGet);
        }
        [AllowAnonymous]
        [SkipERPAuthorize]
        public JsonResult AddMemberSpiritualNotes(int memberId, DateTime date, string memberSpiritualNotes, string comment)
        {
            // db.Configuration.ProxyCreationEnabled = false;
            List<MemberSpiritualNote> spiritualNotes = new List<MemberSpiritualNote>();
            var ss = JsonConvert.DeserializeObject<List<MemberSpiritualNote>>(memberSpiritualNotes);
            foreach (var item in ss)
            {

                MemberSpiritualNote spiritualNote = new MemberSpiritualNote();
                spiritualNote.Date = item.Date;
                spiritualNote.ChurchMembershipId = item.ChurchMembershipId;
                spiritualNote.SpiritualNoteId = item.SpiritualNoteId;
                spiritualNote.Comment = comment;
                spiritualNotes.Add(spiritualNote);
            }
            db.MemberSpiritualNotes.RemoveRange(db.MemberSpiritualNotes.Where(s => s.ChurchMembershipId == memberId && s.Date.Value.Day == date.Day
            && s.Date.Value.Month == date.Month
            && s.Date.Value.Year == date.Year));
            db.SaveChanges();
            db.MemberSpiritualNotes.AddRange(spiritualNotes);
            db.SaveChanges();
            return Json(spiritualNotes, JsonRequestBehavior.AllowGet);
        }
        [AllowAnonymous]
        [SkipERPAuthorize]
        public JsonResult GetNoteIdsByMemberIdAndDate(int Month, int Year, int? ChurchMembershipId)
        {
            List<Object> result = new List<object>();
            var todayDate = DateTime.Now;
            var monthDaysCount = DateTime.DaysInMonth(Year, Month);
            int[] monthDays = Enumerable.Range(1, monthDaysCount).ToArray();
            foreach (var day in monthDays)
            {
                var Value = new object();
                var x = db.MemberSpiritualNotes.Where(a => a.ChurchMembershipId == ChurchMembershipId
              && a.Date.Value.Year == Year
              && a.Date.Value.Month == Month
              && a.Date.Value.Day == day
            ).ToList().Count;
                Value = new
                {
                    Day = day,
                    Value = x
                };
                result.Add(Value);


            }
            return Json(result.ToList(), JsonRequestBehavior.AllowGet);
        }
        [AllowAnonymous]
        [SkipERPAuthorize]
        public JsonResult GetNoteIdsByDate(DateTime Date, int? ChurchMembershipId)
        {
            var NoteIDs = db.MemberSpiritualNotes.Where(a => a.ChurchMembershipId == ChurchMembershipId
            && a.Date.Value.Year == Date.Year
            && a.Date.Value.Month == Date.Month
            && a.Date.Value.Day == Date.Day
            ).Select(s => new
            {
                Id = s.SpiritualNoteId,
                ArName = s.SpiritualNote.ArName,
                Comment = s.Comment,
                IsChecked = true
            }).ToList();
            List<dynamic> result = new List<object>();
            var Notes = db.SpiritualNotes.Select(s => new
            {
                Id = s.Id,
                ArName = s.ArName,
                IsChecked = false
            }
                );
            foreach (var note in NoteIDs)
            {
                result.Add(note);
            }
            foreach (var note in Notes)
            {
                if (!NoteIDs.Select(s => s.Id).Contains(note.Id))
                {
                    result.Add(note);
                }

            }

            return Json(result.ToList().OrderBy(o => o.Id), JsonRequestBehavior.AllowGet);
        }
        [AllowAnonymous]
        [SkipERPAuthorize]
        public JsonResult UpdatePlayerId(int ChurchMembershipId, string PlayerId)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var member = db.ChurchMemberships.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == ChurchMembershipId).FirstOrDefault();
            var oldPlayerId = member.PlayerId;
            if (member != null)
            {
                member.PlayerId = PlayerId;
                db.Entry(member).State = EntityState.Modified;
                db.SaveChanges();
            }
            if (PlayerId != oldPlayerId)
                SendToFirebaseUsers(ChurchMembershipId, "سلام", "أهلا بك فى التطبيق");
            return Json(member, JsonRequestBehavior.AllowGet);
        }
        [AllowAnonymous]
        [SkipERPAuthorize]
        public JsonResult GetChurchMeetingSetting()
        {
            var settings = db.ChurchMeetingSettings.FirstOrDefault();
            return Json(settings, JsonRequestBehavior.AllowGet);
        }

        [AllowAnonymous]
        [SkipERPAuthorize]
        public JsonResult checkFamilyMemberPasswordByMobileNo(string Mobile, string Password)
        {
            var member = db.ChurchMemberships.Where(a => a.IsActive == true && a.IsDeleted == false && a.Mobile == Mobile && a.Password == Password).Select(a => new
            {
                a.Id,
                a.Code,
                a.ArName,
                BirthDate = a.BirthDate.Value.ToString().Replace(@"\", ""),
                a.Mobile,
                a.Notes,
                a.Image,
                a.Address,
                a.TopicsForDiscussion,
                a.Email,
                a.Password,
                a.PlayerId,
                //a.ConfessionFather,
                ConfessionFather = a.ChurchFather.ArName,
                Gender = a.Gender.ArName,
                NoOfDays = SqlFunctions.DateDiff("day", a.ChurchMeetings.Where(m => m.ChurchMembershipId == a.Id && !m.IsDeleted).OrderByDescending(m => m.Date).FirstOrDefault().Date, DateTime.Now)
            }).ToList();
            return Json(member, JsonRequestBehavior.AllowGet);
        }
        [AllowAnonymous]
        [SkipERPAuthorize]
        public JsonResult UpdateMemberPassword(int? ChurchMembershipId, string Password)
        {
            string oldPassword = null;
            db.Configuration.ProxyCreationEnabled = false;
            var member = db.ChurchMemberships.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == ChurchMembershipId).FirstOrDefault();
            if (member != null)
            {
                oldPassword = member.Password;
                member.Password = Password;
                db.Entry(member).State = EntityState.Modified;
                db.SaveChanges();
                if (oldPassword == null || oldPassword == "")
                    SendToFirebaseUsers(member.Id, "سلام", "أهلا بك فى التطبيق");
            }
            var newObj = new ChurchMembership();
            newObj.Id = member.Id;
            newObj.Code = member.Code;
            newObj.ArName = member.ArName;
            newObj.BirthDate = member.BirthDate;//member.BirthDate.Value.ToString().Replace(@"\", "");
            newObj.Mobile = member.Mobile;
            newObj.Notes = member.Notes;
            newObj.Image = member.Image;
            newObj.Address = member.Address;
            newObj.TopicsForDiscussion = member.TopicsForDiscussion;
            newObj.Email = member.Email;
            newObj.PlayerId = member.PlayerId;
            newObj.Password = member.Password;
            newObj.ConfessionFather = member.ConfessionFather;
            newObj.ChurchFatherId = member.ChurchFatherId;
            return Json(newObj/*member*/, JsonRequestBehavior.AllowGet);
        }

        [AllowAnonymous]
        [SkipERPAuthorize]
        public JsonResult TestFirebase(int MemberId)
        {
            SendToFirebaseUsers(MemberId, "سلام", "أهلا بك فى التطبيق");
            return Json(MemberId/*member*/, JsonRequestBehavior.AllowGet);
        }

        public JsonResult SendToFirebaseUsers(int memberId, string title, string msg)
        {
            try
            {
                FirebaseApp.Create(new AppOptions()
                {
                    Credential = GoogleCredential.FromFile(Server.MapPath("/Content/myguide-4341f-firebase-adminsdk-fayrb-5a6e5b0ba1.json")),
                });
            }
            catch { }
            try
            {
                //for (int i = 0; i < playerIds.Length; i++)
                //{
                var playerId = db.ChurchMemberships.Where(a => a.Id == memberId).FirstOrDefault().PlayerId;
                var message = new Message()
                {
                    Notification = new FirebaseAdmin.Messaging.Notification
                    {
                        Title = title,
                        Body = msg
                    },
                    Token = playerId //"fPb3P01jPBs:APA91bHG-WVfDqIx0j6CvM6Y-oWLEF-N7ESJu_NMHFSECKP7huuG61WGJ9ow8Kp-AAGuiBA4rEmKtcfBnjTmyqhsu4d6qY04sj-s81onJOvCsCggwTMm9coek-cGTh6-Rm01ViCTj-U8"
                };
            

            // Send the message
            var response = FirebaseMessaging.DefaultInstance.SendAsync(message);
            return Json(response, JsonRequestBehavior.AllowGet);
            }
            catch(Exception ex) {
                return Json(ex, JsonRequestBehavior.AllowGet);
            }
            
            //}
            //  return Json(0, JsonRequestBehavior.AllowGet);
        }
        /// </summary>
        /// 
        /// <param name="disposing"></param>
        /// 
        [SkipERPAuthorize]
        public static string GetActivityLogo(int? DepartmentId)
        {
            MySoftERPEntity _db = new MySoftERPEntity();
            var systemSetting = _db.SystemSettings.FirstOrDefault();
            var UseActivityLogo = systemSetting.UseActivityLogo;
            var logo = "";
            if (DepartmentId != null)
            {
                var ActivityId = _db.Departments.Where(a => a.IsDeleted == false && a.IsActive == true && a.Id == DepartmentId).FirstOrDefault().ActivityId;
                if (ActivityId != null && UseActivityLogo == true)
                {
                    logo = _db.Activities.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == ActivityId).FirstOrDefault().Image;
                }
                else
                {
                    logo = systemSetting.Logo.ToString();
                }
            }
            else
            {
                logo = systemSetting.Logo.ToString();
            }
            return logo;
        }

        [SkipERPAuthorize]
        public ActionResult GetFixedAssets(int? DepartmentId, DateTime? DateFrom, DateTime? DateTo, int? ActivityId, int? CompanyId, int pageIndex = 1, int wantedRowsNo = 100)
        {
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            ViewBag.wantedRowsNo = wantedRowsNo;
            var Assets = db.FixedAsset_Get(DepartmentId, DateFrom, DateTo,ActivityId,CompanyId).ToList().Skip(skipRowsNo).Take(wantedRowsNo);
            ViewBag.Count = db.FixedAsset_Get(DepartmentId, DateFrom, DateTo,ActivityId,CompanyId).Count();
            return Json(Assets, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public ActionResult GetExpensesInPeriod(DateTime? From, DateTime? To, int? ExpenseId, int? DepartmentId, int? ActivityId, int? CompanyId, int pageIndex = 1, int wantedRowsNo = 100)
        {
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            ViewBag.wantedRowsNo = wantedRowsNo;
            var Expenses = db.GetExpensesInPeriod(From, To, DepartmentId, ExpenseId,ActivityId,CompanyId).ToList().Skip(skipRowsNo).Take(wantedRowsNo);
            ViewBag.Count = db.GetExpensesInPeriod(From, To, DepartmentId, ExpenseId, ActivityId, CompanyId).Count();
            return Json(Expenses, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public ActionResult GetEmployeesStatement(int? DepartmentId, int? WorkStatusId, int? HrDepartmentId, int? JobId, int? ContractsTypeId, int? NationalityId, int? ActivityId, int? CompanyId, int pageIndex = 1, int wantedRowsNo = 100)
        {
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            ViewBag.wantedRowsNo = wantedRowsNo;
            var EmployeesStatement = db.GetEmployeesStatement(DepartmentId, WorkStatusId, HrDepartmentId, JobId, ContractsTypeId, NationalityId, ActivityId, CompanyId).ToList().Skip(skipRowsNo).Take(wantedRowsNo);
            ViewBag.Count = db.GetEmployeesStatement(DepartmentId, WorkStatusId, HrDepartmentId, JobId, ContractsTypeId, NationalityId, ActivityId, CompanyId).Count();
            return Json(EmployeesStatement, JsonRequestBehavior.AllowGet);
        } 
        [SkipERPAuthorize]
        public ActionResult GetSalariesStatement(int? DepartmentId, int? Month, int? Year, int? HrDepartmentId, int? ActivityId, int? CompanyId, int pageIndex = 1, int wantedRowsNo = 100)
        {
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            ViewBag.wantedRowsNo = wantedRowsNo;
            var SalariesStatement = db.GetSalariesStatement(DepartmentId, Month,Year, HrDepartmentId, ActivityId, CompanyId).ToList().Skip(skipRowsNo).Take(wantedRowsNo);
            ViewBag.Count = db.GetSalariesStatement(DepartmentId, Month, Year, HrDepartmentId, ActivityId, CompanyId).Count();
            return Json(SalariesStatement, JsonRequestBehavior.AllowGet);
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
    public class StartAndEndPeriod
    {
        public string start;
        public string end;
    }
}