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
using System.Data.SqlClient;
using System.Globalization;
using Amazon.S3;
using Amazon.S3.Model;
using System.IO;
using Amazon.Runtime;
using Amazon;
using System.Threading.Tasks;
using MyERP.Utils;
using MyERP.Repository;
using System.Web.Script.Serialization;

namespace MyERP.Controllers
{
    public class ItemController : Controller
    {
        private readonly MySoftERPEntity db = new MySoftERPEntity();
        // GET: Item
        public ActionResult Index(int? itemGroupId, int? departmentId, int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();

            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            ViewBag.UseExpiryDateForItems = systemSetting.UseExpiryDateForItems;

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة الاصناف",
                EnAction = "Index",
                ControllerName = "Item",
                UserName = User.Identity.Name,
                UserId = userId,
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("Item", "View", "Index", null, null, "الاصناف");
            ///////-----------------------------------------------------------------------

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            ViewBag.ItemGroupId = new SelectList(db.ItemGroups.Where(x => /*x.TypeId == 3&&*/x.IsDeleted == false && x.IsActive == true).Select(x => new
            {
                x.Id,
                ArName = session != null && session.ToString() == "en" && x.EnName != null ? x.Code + " - " + x.EnName : x.Code + " - " + x.ArName
            }), "Id", "ArName", itemGroupId);
            ViewBag.DepartmentId = new SelectList(db.Departments.Where(x => !x.IsDeleted && x.IsActive).Select(x => new
            {
                x.Id,
                ArName = session != null && session.ToString() == "en" && x.EnName != null ? x.Code + " - " + x.EnName : x.Code + " - " + x.ArName
            }), "Id", "ArName", departmentId);

            IQueryable<Item> itemsList;
            List<int> departmentItemsIdsList = new List<int>();


            if (string.IsNullOrEmpty(searchWord))
            {
                if (departmentId.HasValue)
                {
                    departmentItemsIdsList = db.DepartmentItems.Where(d => d.DepartmentId == departmentId).Select(d => d.ItemId).ToList();

                    itemsList = db.Items.Where(c => c.IsDeleted == false && departmentItemsIdsList.Contains(c.Id) && (itemGroupId == null || c.ItemGroupId == itemGroupId)).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.Items.Where(c => c.IsDeleted == false && departmentItemsIdsList.Contains(c.Id) && (itemGroupId == null || c.ItemGroupId == itemGroupId)).Count();
                }
                else
                {
                    itemsList = db.Items.Where(c => c.IsDeleted == false && (itemGroupId == null || c.ItemGroupId == itemGroupId)).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.Items.Where(c => c.IsDeleted == false && (itemGroupId == null || c.ItemGroupId == itemGroupId)).Count();
                }

            }
            else if (departmentId.HasValue)
            {
                departmentItemsIdsList = db.DepartmentItems.Where(d => d.DepartmentId == departmentId).Select(d => d.ItemId).ToList();

                itemsList = db.Items.Where(s => s.IsDeleted == false && departmentItemsIdsList.Contains(s.Id) && (itemGroupId == null || s.ItemGroupId == itemGroupId) && (s.Code.Contains(searchWord) || s.Code == searchWord || s.ArName.Contains(searchWord) || s.ProductNo.Contains(searchWord) || s.EnName.Contains(searchWord) || s.ItemGroup.ArName.Contains(searchWord) || s.ItemType.ArName.Contains(searchWord) || s.Notes.Contains(searchWord))).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Items.Where(s => s.IsDeleted == false && departmentItemsIdsList.Contains(s.Id) && (itemGroupId == null || s.ItemGroupId == itemGroupId) && (s.Code.Contains(searchWord) || s.Code == searchWord || s.ArName.Contains(searchWord) || s.ProductNo.Contains(searchWord) || s.EnName.Contains(searchWord) || s.ItemGroup.ArName.Contains(searchWord) || s.ItemType.ArName.Contains(searchWord) || s.Notes.Contains(searchWord))).Count();
            }
            else
            {
                itemsList = db.Items.Where(s => s.IsDeleted == false && (itemGroupId == null || s.ItemGroupId == itemGroupId) && (s.Code.Contains(searchWord) || s.Code == searchWord || s.ArName.Contains(searchWord) || s.ProductNo.Contains(searchWord) || s.EnName.Contains(searchWord) || s.ItemGroup.ArName.Contains(searchWord) || s.ItemType.ArName.Contains(searchWord) || s.Notes.Contains(searchWord))).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Items.Where(s => s.IsDeleted == false && (itemGroupId == null || s.ItemGroupId == itemGroupId) && (s.Code.Contains(searchWord) || s.Code == searchWord || s.ArName.Contains(searchWord) || s.ProductNo.Contains(searchWord) || s.EnName.Contains(searchWord) || s.ItemGroup.ArName.Contains(searchWord) || s.ItemType.ArName.Contains(searchWord) || s.Notes.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(itemsList.ToList());
        }

        [SkipERPAuthorize]
        public JsonResult ShowLastPurchasePrice()
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            if (userId == 1)
            {
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(db.UserPrivileges.FirstOrDefault(u => u.PageAction.EnName == "ShowLastPurchasePrice" && u.PageAction.Action == "ShowLastPurchasePrice" && u.UserId == userId).Privileged, JsonRequestBehavior.AllowGet);

            }

        }
        [SkipERPAuthorize]
        public JsonResult ShowAvgCostPrice()
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            if (userId == 1)
            {
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(db.UserPrivileges.FirstOrDefault(u => u.PageAction.EnName == "ShowAvgCostPrice" && u.PageAction.Action == "ShowAvgCostPrice" && u.UserId == userId).Privileged, JsonRequestBehavior.AllowGet);
            }

        }

        AmazonS3Client client = new AmazonS3Client(new BasicAWSCredentials("AKIA6NGZYTRRXPJJC2GM", "2FhxdVeZxH4ao50MShsLIOqoc6cnih5TO/j2jPvx"), RegionEndpoint.USEast2);

        string BucketName = "mysoftecom";

        // GET: Item/Edit/5
        public ActionResult AddEdit(int? id, bool? repeat)
        {

            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            int? roleId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("RoleId").Value);
            SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
            ViewBag.AddItemBarcodeBasedOnItemCode = systemSetting.AddItemBarcodeBasedOnItemCode;
            ViewBag.PricingDependOnCustGroups = systemSetting.PricingDependOnCustGroups;
            ViewBag.EnterItemCodeManually = systemSetting.EnterItemCodeManually;
            //user privilege for factory department
            ViewBag.FactoryPrivilege = userId == 1 || db.UserDepartments.Where(x => x.UserId == userId && x.DepartmentId == 28).Select(x => x.Privilege).FirstOrDefault() == true;
            var DisplayItemPrices = userId == 1 ? true : db.UserPrivileges.Where(u => u.PageAction.PageId == 56 && u.PageAction.EnName == "DisplayItemPrices" && u.PageAction.Action == "DisplayItemPrices" && u.UserId == userId).Select(x => x.Privileged).FirstOrDefault();
            DisplayItemPrices = DisplayItemPrices ?? db.RolePrivileges.Where(u => u.PageAction.PageId == 56 && u.PageAction.EnName == "DisplayItemPrices" && u.PageAction.Action == "DisplayItemPrices" && u.RoleId == roleId).Select(x => x.Privileged).FirstOrDefault();

            ViewBag.DisplayItemPrices = DisplayItemPrices ?? false;

            var CanChangeItemName = userId == 1 ? true : db.UserPrivileges.Where(u => u.PageAction.PageId == 56 && u.PageAction.EnName == "CanChangeItemName" && u.PageAction.Action == "CanChangeItemName" && u.UserId == userId).Select(x => x.Privileged).FirstOrDefault();
            CanChangeItemName = CanChangeItemName ?? db.RolePrivileges.Where(u => u.PageAction.PageId == 56 && u.PageAction.EnName == "CanChangeItemName" && u.PageAction.Action == "CanChangeItemName" && u.RoleId == roleId).Select(x => x.Privileged).FirstOrDefault();
            ViewBag.CanChangeItemName = CanChangeItemName ?? false;

            var CanChangeProductNumber = userId == 1 ? true : db.UserPrivileges.Where(u => u.PageAction.PageId == 56 && u.PageAction.EnName == "CanChangeProductNumber" && u.PageAction.Action == "CanChangeProductNumber" && u.UserId == userId).Select(x => x.Privileged).FirstOrDefault();
            CanChangeProductNumber = CanChangeProductNumber ?? db.RolePrivileges.Where(u => u.PageAction.PageId == 56 && u.PageAction.EnName == "CanChangeProductNumber" && u.PageAction.Action == "CanChangeProductNumber" && u.RoleId == roleId).Select(x => x.Privileged).FirstOrDefault();
            ViewBag.CanChangeProductNumber = CanChangeProductNumber ?? false;

            var CanChangeBarcode = userId == 1 ? true : db.UserPrivileges.Where(u => u.PageAction.PageId == 56 && u.PageAction.EnName == "CanChangeBarcode" && u.PageAction.Action == "CanChangeBarcode" && u.UserId == userId).Select(x => x.Privileged).FirstOrDefault();
            CanChangeBarcode = CanChangeBarcode ?? db.RolePrivileges.Where(u => u.PageAction.PageId == 56 && u.PageAction.EnName == "CanChangeBarcode" && u.PageAction.Action == "CanChangeBarcode" && u.RoleId == roleId).Select(x => x.Privileged).FirstOrDefault();
            ViewBag.CanChangeBarcode = CanChangeBarcode ?? false;

            var SystemPageId = db.SystemPages.Where(a => a.IsActive == true && a.IsDeleted == false && a.ControllerName == "Item").FirstOrDefault().Id;
            ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
            {
                b.Id,
                ArName = b.FixedPart + " - " + b.ArName
            }), "Id", "ArName");
            ViewBag.AlternativeItemId = new SelectList(db.Items.Where(i => i.IsDeleted == false && i.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = session != null && session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
            }), "Id", "ArName");
            if (id == null)
            {
                ViewBag.ItemGroupId = new SelectList(db.ItemGroups.Where(i => i.IsDeleted == false && i.IsActive == true/*&&i.TypeId==3*/).Select(b => new
                {
                    b.Id,
                    ArName = session != null && session != null && session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.ItemTypeId = new SelectList(db.ItemTypes.Where(i => i.IsDeleted == false && i.IsActive == true).Select(b => new
                {
                    b.Id,
                    ArName = session != null && session != null && session.ToString() == "en" && b.EnName != null ? b.EnName : b.ArName
                }), "Id", "ArName");
                ViewBag.ItemCategoryId = new SelectList(db.ItemCategories.Where(i => i.IsDeleted == false && i.IsActive == true).Select(b => new
                {
                    b.Id,
                    ArName = session != null && session != null && session.ToString() == "en" && b.EnName != null ? b.EnName : b.ArName
                }), "Id", "ArName");

                return View();
            }
            Item item = db.Items.Find(id);
            if (item == null)
            {
                return HttpNotFound();
            }
            var AlternativeItem = db.GetAlternativeItemsByMainItemId(id).ToList();
            ViewBag.AlternativeItem = AlternativeItem;
            ViewBag.ItemGroupId = new SelectList(db.ItemGroups.Where(i => i.IsDeleted == false && i.IsActive == true /*&& i.TypeId == 3*/).Select(b => new
            {
                b.Id,
                ArName = session != null && session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
            }), "Id", "ArName", item.ItemGroupId);
            ViewBag.ItemTypeId = new SelectList(db.ItemTypes.Where(i => i.IsDeleted == false && i.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = session != null && session.ToString() == "en" && b.EnName != null ? b.EnName : b.ArName
            }), "Id", "ArName", item.ItemTypeId);
            ViewBag.ItemCategoryId = new SelectList(db.ItemCategories.Where(i => i.IsDeleted == false && i.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = session != null && session.ToString() == "en" && b.EnName != null ? b.EnName : b.ArName
            }), "Id", "ArName", item.ItemCategoryId);
            ViewBag.AvgCost = db.Database.SqlQuery<decimal>("select dbo.Item_AvgCost(@Id,@DepartmentId)", new SqlParameter("@Id", id), new SqlParameter("@DepartmentId", 28)).FirstOrDefault();
            ViewBag.Next = QueryHelper.Next((int)id, "Item");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Item");
            ViewBag.Last = QueryHelper.GetLast("Item");
            ViewBag.First = QueryHelper.GetFirst("Item");

            ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
            {
                b.Id,
                ArName = b.FixedPart + " - " + b.ArName
            }), "Id", "ArName", item.FieldsCodingId);

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل الصنف",
                EnAction = "AddEdit",
                ControllerName = "Item",
                UserName = User.Identity.Name,
                UserId = userId,
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = item.Id,
                ArItemName = item.ArName,
                EnItemName = item.EnName,
                CodeOrDocNo = item.Code
            });
            ViewBag.IsRepeat = repeat == true;

            return View(item);
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult AddEdit(Item item)
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            bool isECommerce = System.Web.Configuration.WebConfigurationManager.AppSettings["ECommerce"] == "true" ? true : false;
            SystemSetting systemSetting = db.SystemSettings.FirstOrDefault();
            var EnterItemCodeManually = systemSetting.EnterItemCodeManually;

            if (ModelState.IsValid)
            {
                if (EnterItemCodeManually == true && item.Id == 0)
                {
                    var _item = db.Items.Where(a => a.Code == item.Code).FirstOrDefault();
                    if (_item != null)
                    {
                        return Json(new { success = "Item Code Is Used Before" });
                    }
                }

                var barcodes = new List<string>();
                foreach (var itemPrice in item.ItemPrices)
                {
                    if (!string.IsNullOrEmpty(itemPrice.Barcode) && db.ItemPrices.Where(x => x.Barcode == itemPrice.Barcode && x.ItemId != item.Id).Any())
                    {
                        barcodes.Add(itemPrice.Barcode);
                    }
                }
                if (barcodes.Count > 0)
                {
                    return Json(new { success = false, barcodes });
                }
                var id = item.Id;
                item.IsDeleted = false;
                if (item.Id > 0)
                {
                    if (EnterItemCodeManually == true)
                    {
                        var _item = db.Items.AsNoTracking().Where(a => a.Code == item.Code).FirstOrDefault();
                        if (_item != null)
                        {
                            if (_item.Id != item.Id)
                            {
                                return Json(new { success = "Item Code Is Used Before" });
                            }
                        }
                    }

                    MyXML.xPathName = "ItemPrices";
                    var ItemPrices = MyXML.GetXML(item.ItemPrices.Select(i => new { i.Id, i.Barcode, i.CustomerGroupId, i.Equivalent, i.ItemUnitId, i.Price, i.IsDefault, i.IsActive, i.IsDeleted, i.ToleranceQty, i.FactoryPrice, i.VendorId, i.PriceBeforeDiscount }));
                    MyXML.xPathName = "ItemVendors";
                    var ItemVendors = MyXML.GetXML(item.ItemVendors);
                    MyXML.xPathName = "ItemComponents";
                    var ItemComponents = MyXML.GetXML(item.ItemComponents);
                    MyXML.xPathName = "DepartmentItems";
                    var DepartmentItemsXml = MyXML.GetXML(item.DepartmentItems);
                    MyXML.xPathName = "AlternativeItems";
                    var AlternativeItems = MyXML.GetXML(item.AlternativeItems);

                    if (isECommerce == false)
                    {
                        if (item.Image != null && item.Image.Contains("base64"))
                        {
                            string fileName = "/images/Items/Item_" + item.Id.ToString() + "_" + DateTime.Now.Ticks + ".jpeg";
                            var bytes = new byte[7000];
                            if (item.Image.Contains("jpeg"))
                            {
                                bytes = Convert.FromBase64String(item.Image.Replace("data:image/jpeg;base64,", ""));
                            }
                            else
                            {
                                bytes = Convert.FromBase64String(item.Image.Replace("data:image/png;base64,", ""));
                            }
                            using (var imageFile = new FileStream(Server.MapPath(fileName), FileMode.Create))
                            {
                                imageFile.Write(bytes, 0, bytes.Length);
                                imageFile.Flush();
                            }
                            item.Image = domainName + fileName;
                        }
                        else if (item.Image == null)
                        {
                            var old = db.Items.Find(item.Id);
                            item.Image = old.Image;
                        }
                    }
                    else
                    {
                        List<ItemImage> imageList = db.ItemImages.Where(i => i.ItemId == item.Id).ToList();

                        if (item.ItemURLImages != null)
                        {
                            foreach (string img in item.ItemURLImages)
                            {
                                if (img.Contains("base64"))
                                {
                                    string fileName = "Item_" + item.Id.ToString() + "_" + DateTime.Now.Ticks + ".jpeg";


                                    if (new AmazonHelper().WritingAnObject("items/" + fileName, img))
                                    {
                                        db.ItemImages.Add(new ItemImage()
                                        {
                                            Image = fileName,
                                            ItemId = item.Id
                                        });
                                        db.SaveChanges();
                                    }
                                }
                            }
                        }


                        // For Deleting .
                        foreach (ItemImage deletedImage in imageList.Where(i => i.ItemId == item.Id && (item.ItemURLImages == null ? true : !item.ItemURLImages.Contains(i.Id.ToString()))).ToList())
                        {
                            db.ItemImages.Remove(deletedImage);
                            db.SaveChanges();

                            if (isECommerce == true)
                            {
                                new AmazonHelper().DeleteAnObject("items/" + deletedImage.Image);
                            }
                        }
                    }



                    int result = db.Item_Update(ItemPrices, ItemVendors, ItemComponents, DepartmentItemsXml, AlternativeItems, item.Id, item.Code, item.ArName, item.EnName, item.OrderLimit, item.ItemGroupId, item.ItemTypeId, item.HasWarranty, item.HasExpiry, item.WarrantyDays, item.WarrantyMonths, item.WarrantyYears, item.IsActive, item.IsDeleted, item.IsPosted, item.IsLinked, item.UserId, item.Notes, item.Image, item.ProductNo, item.VirtualQuantity, item.ArNotes, item.KrNotes, item.EnDetails, item.ArDetails, item.KrDetails, item.KrName, item.HasAccessory, item.IsAccessory, item.ItemCategoryId, item.FieldsCodingId, item.NotIncludeValueAddedTax, item.TaxPercentage);

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("Item", "Edit", "AddEdit", id, null, "الاصناف");

                    // Add DB Change
                    QueryHelper.AddDBChange(new DBChange()
                    {
                        TableName = "Item",
                        SelectedId = item.Id,
                        IsMasterChange = true,
                        IsNew = false,
                        IsTransaction = false
                    });
                }
                else
                {
                    item.IsActive = true;
                    MyXML.xPathName = "ItemPrices";
                    var ItemPrices = MyXML.GetXML(item.ItemPrices.Select(i => new { i.Barcode, i.CustomerGroupId, i.Equivalent, i.ItemUnitId, i.Price, i.IsDefault, i.IsActive, i.IsDeleted, i.ToleranceQty, i.FactoryPrice, i.VendorId, i.PriceBeforeDiscount }));
                    MyXML.xPathName = "ItemVendors";
                    var ItemVendors = MyXML.GetXML(item.ItemVendors);
                    MyXML.xPathName = "ItemComponents";
                    var ItemComponents = MyXML.GetXML(item.ItemComponents);
                    MyXML.xPathName = "DepartmentItems";
                    var DepartmentItemsXml = MyXML.GetXML(item.DepartmentItems);
                    MyXML.xPathName = "AlternativeItems";
                    var AlternativeItems = MyXML.GetXML(item.AlternativeItems);
                    if (System.Web.Configuration.WebConfigurationManager.AppSettings["ProjectName"] != "Baraka")
                    {
                        //item.Code = (QueryHelper.CodeLastNum("Item") + 1).ToString();
                        if (EnterItemCodeManually != true)
                        {
                            item.Code = new JavaScriptSerializer().Serialize(SetCodeNum(item.FieldsCodingId).Data).ToString().Trim('"');
                        }
                    }
                    if (isECommerce == false)
                    {
                        if (item.Image != null && item.Image.Contains("base64"))
                        {
                            string fileName = "/images/Items/Item_" + item.Id.ToString() + "_" + DateTime.Now.Ticks + ".jpeg";
                            var bytes = new byte[7000];
                            if (item.Image.Contains("jpeg"))
                            {
                                bytes = Convert.FromBase64String(item.Image.Replace("data:image/jpeg;base64,", ""));
                            }
                            else
                            {
                                bytes = Convert.FromBase64String(item.Image.Replace("data:image/png;base64,", ""));
                            }
                            using (var imageFile = new FileStream(Server.MapPath(fileName), FileMode.Create))
                            {
                                imageFile.Write(bytes, 0, bytes.Length);
                                imageFile.Flush();
                            }
                            item.Image = domainName + fileName;
                        }
                    }



                    db.Item_Insert(ItemPrices, ItemVendors, ItemComponents, DepartmentItemsXml, AlternativeItems, item.Code, item.ArName, item.EnName, item.OrderLimit, item.ItemGroupId, item.ItemTypeId, item.HasWarranty, item.HasExpiry, item.WarrantyDays, item.WarrantyMonths, item.WarrantyYears, item.IsActive, item.IsDeleted, item.IsPosted, item.IsLinked, item.UserId, item.Notes, item.Image, item.ProductNo, item.VirtualQuantity, item.ArNotes, item.KrNotes, item.EnDetails, item.ArDetails, item.KrDetails, item.KrName, item.HasAccessory, item.IsAccessory, item.ItemCategoryId, item.FieldsCodingId, item.NotIncludeValueAddedTax, item.TaxPercentage);

                    // Add DB Change
                    var SelectedId = db.Items.OrderByDescending(r => r.Id).FirstOrDefault().Id;
                    QueryHelper.AddDBChange(new DBChange()
                    {
                        TableName = "Item",
                        SelectedId = SelectedId,
                        IsMasterChange = true,
                        IsNew = true,
                        IsTransaction = false
                    });


                    int LastItemID = QueryHelper.GetLast("Item").HasValue ? QueryHelper.GetLast("Item").Value : 0;

                    if (isECommerce == true)
                    {
                        if (item.ItemURLImages != null)
                        {
                            foreach (string img in item.ItemURLImages)
                            {
                                string fileName = "Item_" + LastItemID.ToString() + "_" + DateTime.Now.Ticks + ".jpeg";


                                if (new AmazonHelper().WritingAnObject("items/" + fileName, img))
                                {
                                    db.ItemImages.Add(new ItemImage()
                                    {
                                        Image = fileName,
                                        ItemId = LastItemID
                                    });
                                    db.SaveChanges();
                                }
                            }
                        }
                    }


                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("Item", "Add", "AddEdit", item.Id, null, "الاصناف");

                    /////////-----------------------------------------------------------------------

                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل الصنف" : "اضافة الصنف",
                    EnAction = "AddEdit",
                    ControllerName = "Item",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = item.Id,
                    ArItemName = item.ArName,
                    EnItemName = item.EnName,
                    CodeOrDocNo = item.Code
                });

                return Json(new { success = true });
            }
            var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();

            return Json(new
            {
                success = false,
                errors
            });
        }

        [SkipERPAuthorize]
        public ActionResult ItemDepartments(int? itemId)
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            var departments = db.DepartmentItems.Where(x => x.ItemId == itemId && x.Department.IsActive == true && x.Department.IsDeleted == false).Select(x => new
            {
                x.DepartmentId,
                Included = true,
                DepartmentArName = session != null && session.ToString() == "en" && x.Department.EnName != null ? x.Department.Code + " - " + x.Department.EnName : x.Department.Code + " - " + x.Department.ArName
            }).Union(db.Departments.Where(x => !db.DepartmentItems.Where(d => d.ItemId == itemId).Select(d => d.DepartmentId).Contains(x.Id) && x.IsDeleted == false && x.IsActive == true).Select(x => new { DepartmentId = x.Id, Included = false, DepartmentArName = session.ToString() == "en" && x.EnName != null ? x.Code + " - " + x.EnName : x.Code + " - " + x.ArName })).ToList();
            return PartialView(departments.Select(x => new DepartmentItem { DepartmentId = x.DepartmentId, Included = x.Included, DepartmentArName = x.DepartmentArName }));
        }

        // POST: Item/Delete/5
        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            //-- Check if this item is used in other Transactions 
            var check = db.CheckItemExistanceInOtherTransactions(id).FirstOrDefault();
            if (check > 0)
            {
                return Content("False");
            }
            else
            {
                Item item = db.Items.Find(id);
                item.IsDeleted = true;
                item.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                Random random = new Random();
                const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
                item.Code = Code;
                item.FieldsCodingId = null;
                foreach (var i in item.ItemPrices)
                {
                    i.IsDeleted = true;
                    i.Barcode = null;
                }
                db.Entry(item).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف الصنف",
                    EnAction = "AddEdit",
                    ControllerName = "Item",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = item.EnName,
                    ArItemName = item.ArName,
                    CodeOrDocNo = item.Code
                });

                ////-------------------- Notification-------------------------////
                Notification.GetNotification("Item", "Delete", "Delete", id, null, "الاصناف");

                // Add DB Change
                QueryHelper.AddDBChange(new DBChange()
                {
                    TableName = "Item",
                    SelectedId = item.Id,
                    IsMasterChange = true,
                    IsNew = false,
                    IsTransaction = false
                });

                return Content("true");
            }
        }


        [HttpPost]
        public ActionResult ActivateDeactivate(int id)
        {
            try
            {
                Item item = db.Items.Find(id);
                if (item.IsActive == true)
                {
                    item.IsActive = false;
                }
                else
                {
                    item.IsActive = true;
                }
                item.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(item).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)item.IsActive ? "تنشيط الصنف" : "إلغاء الصنف",
                    EnAction = "AddEdit",
                    ControllerName = "Item",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = item.Id,
                    EnItemName = item.EnName,
                    ArItemName = item.ArName,
                    CodeOrDocNo = item.Code
                });
                ////-------------------- Notification-------------------------////
                if (item.IsActive == true)
                {
                    Notification.GetNotification("Item", "Activate/Deactivate", "ActivateDeactivate", id, true, "الاصناف");
                }
                else
                {

                    Notification.GetNotification("Item", "Activate/Deactivate", "ActivateDeactivate", id, false, "الاصناف");
                }
                /////-----------------------------------------------------------------------

                // Add DB Change
                QueryHelper.AddDBChange(new DBChange()
                {
                    TableName = "Item",
                    SelectedId = item.Id,
                    IsMasterChange = true,
                    IsNew = false,
                    IsTransaction = false
                });

                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }
        [SkipERPAuthorize]
        public JsonResult SetCodeNum(int? FieldsCodingId)
        {
            if (FieldsCodingId > 0)
            {
                double result = 0;
                var fieldsCoding = db.FieldsCodings.Where(a => a.Id == FieldsCodingId).FirstOrDefault();
                var fixedPart = fieldsCoding.FixedPart;
                var noOfDigits = fieldsCoding.DigitsNo;
                var IsAutomaticSequence = fieldsCoding.IsAutomaticSequence;
                var IsZerosFills = fieldsCoding.IsZerosFills;
                if (string.IsNullOrEmpty(fixedPart))
                {
                    var code = db.Items.Where(a => a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
                    if (code.FirstOrDefault() == null)
                    {
                        result = 0 + 1;
                    }
                    else
                    {
                        result = (double.Parse(code.FirstOrDefault().ToString())) + 1;
                    }
                    var CodeNo = "";
                    if (IsZerosFills == true)
                    {
                        if (result.ToString().Length < noOfDigits)
                        {
                            CodeNo = QueryHelper.FillsWithZeros(noOfDigits, result.ToString());
                        }
                        else
                        {
                            CodeNo = result.ToString();
                        }
                    }
                    else
                    {
                        CodeNo = result.ToString();
                    }
                    return Json(CodeNo, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    var FullNewCode = "";
                    var CodeNo = "";
                    var code = db.Items.Where(a => a.Code.Contains(fixedPart) && a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
                    if (code.FirstOrDefault() == null)
                    {
                        result = 0 + 1;
                        if (IsZerosFills == true)
                        {
                            if (result.ToString().Length < noOfDigits)
                            {
                                CodeNo = QueryHelper.FillsWithZeros(noOfDigits /*- fixedPart.Length*/, result.ToString());
                            }
                            else
                            {
                                CodeNo = result.ToString();
                            }
                        }
                        else
                        {
                            CodeNo = result.ToString();
                        }
                        FullNewCode = fixedPart + CodeNo;
                    }
                    else
                    {
                        var LastCode = code.FirstOrDefault().ToString();
                        result = double.Parse(LastCode.Substring(LastCode.LastIndexOf(fixedPart) + fixedPart.Length)) + 1;
                        if (IsZerosFills == true)
                        {
                            if (result.ToString().Length < noOfDigits)
                            {
                                CodeNo = QueryHelper.FillsWithZeros(noOfDigits /*- fixedPart.Length*/, result.ToString());
                            }
                            else
                            {
                                CodeNo = result.ToString();
                            }
                        }
                        else
                        {
                            CodeNo = result.ToString();
                        }
                        FullNewCode = fixedPart + CodeNo;
                    }
                    return Json(FullNewCode, JsonRequestBehavior.AllowGet);
                }
            }
            else
            {
                //var code = QueryHelper.CodeLastNum("Item");
                //return Json(code + 1, JsonRequestBehavior.AllowGet);
                double result = 0;
                var code = db.Items.Where(a => a.FieldsCodingId == null && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
                if (code.FirstOrDefault() == null)
                {
                    result = 0 + 1;
                }
                else
                {
                    result = double.Parse(code.FirstOrDefault().ToString()) + 1;
                }
                return Json(result, JsonRequestBehavior.AllowGet);
            }
        }

        [SkipERPAuthorize]
        public JsonResult GetComponentItems()
        {
            return Json(db.Items.Where(i => i.IsDeleted == false && i.IsActive == true).Select(i => new { ArName = i.ArName + " - " + i.ItemPrices.Where(x => x.IsDefault == true).Select(x => x.ItemUnit.ArName).FirstOrDefault(), i.Id }), JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public async Task<ActionResult> GetItemQuantityInEachWarehouse(int itemId, int depId)
        {
            ViewBag.LastPrice = db.PurchaseInvoiceDetails.Where(p => p.ItemId == itemId && p.PurchaseInvoice.DepartmentId == depId && p.IsDeleted == false).OrderByDescending(p => p.Id).Select(p => new itemData { Price = p.Price, VoucherDate = p.PurchaseInvoice.VoucherDate, VendorName = p.PurchaseInvoice.Vendor.ArName, qty = p.Qty }).ToList();

            //var LastPrice = db.PurchaseInvoiceDetails.Where(p => p.ItemId == itemId && p.PurchaseInvoice.DepartmentId == depId && p.IsDeleted == false).OrderByDescending(p => p.Id).Select(p => p.Price).FirstOrDefault();
            var UnitEquivalent = db.PurchaseInvoiceDetails.Where(p => p.ItemId == itemId && p.PurchaseInvoice.DepartmentId == depId && p.IsDeleted == false).OrderByDescending(p => p.Id).Select(p => p.UnitEquivalent).FirstOrDefault();
            //ViewBag.LastPrice = LastPrice / decimal.Parse(UnitEquivalent.ToString());//get price for default unit
            ItemRepository itemRepository = new ItemRepository(db);
            ViewBag.AVGCost = await itemRepository.GetItemAvgPrice(itemId, depId);

            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            int roleId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("RoleId").Value);
            var unit = db.ItemPrices.Where(i => i.ItemId == itemId && i.IsDeleted == false && i.IsDefault == true)
                .FirstOrDefault();
            ViewBag.DefaultUnit = unit?.ItemUnit?.ArName??"";

            var ShowCost = userId == 1 ? true : db.UserPrivileges.Where(u => u.PageAction.PageId == 56 && u.PageAction.EnName == "ShowAvgCostPrice" && u.PageAction.Action == "ShowAvgCostPrice" && u.UserId == userId).Select(x => x.Privileged).FirstOrDefault();
            ShowCost = ShowCost ?? db.RolePrivileges.Where(u => u.PageAction.PageId == 56 && u.PageAction.EnName == "ShowAvgCostPrice" && u.PageAction.Action == "ShowAvgCostPrice" && u.RoleId == roleId).Select(x => x.Privileged).FirstOrDefault();
            if (ShowCost != true)
                ShowCost = false;
            ViewBag.ShowCost = ShowCost;

            var ShowPurchasePrice = userId == 1 ? true : db.UserPrivileges.Where(u => u.PageAction.PageId == 56 && u.PageAction.EnName == "ShowLastPurchasePrice" && u.PageAction.Action == "ShowLastPurchasePrice" && u.UserId == userId).Select(x => x.Privileged).FirstOrDefault();
            ShowPurchasePrice = ShowPurchasePrice ?? db.RolePrivileges.Where(u => u.PageAction.PageId == 56 && u.PageAction.EnName == "ShowLastPurchasePrice" && u.PageAction.Action == "ShowLastPurchasePrice" && u.RoleId == roleId).Select(x => x.Privileged).FirstOrDefault();
            if (ShowPurchasePrice != true)
                ShowPurchasePrice = false;
            ViewBag.ShowPurchasePrice = ShowPurchasePrice;

            var ShowItemDetails = userId == 1 ? true : db.UserPrivileges.Where(u => u.PageAction.PageId == 56 && u.PageAction.EnName == "ShowItemDetils" && u.PageAction.Action == "ShowItemDetils" && u.UserId == userId).Select(x => x.Privileged).FirstOrDefault();
            ShowItemDetails = ShowItemDetails ?? db.RolePrivileges.Where(u => u.PageAction.PageId == 56 && u.PageAction.EnName == "ShowItemDetils" && u.PageAction.Action == "ShowItemDetils" && u.RoleId == roleId).Select(x => x.Privileged).FirstOrDefault();
            if (ShowItemDetails != true)
                ShowItemDetails = false;
            ViewBag.ShowItemDetails = ShowItemDetails;
            ViewBag.ItemId = itemId;
            return PartialView(db.GetItemQuantityInEachWarehouse(itemId, depId));
        }

        public ActionResult AdjustPrice()
        {
            ViewBag.ItemGroupId = new SelectList(db.ItemGroups.Where(i => i.IsDeleted == false && i.IsActive == true /*&& i.TypeId == 3*/), "Id", "ArName");
            return View();
        }

        //[HttpPost]
        //public ActionResult AdjustPrice(int id, decimal? value, decimal? perc)
        //{
        //    return Content(db.ItemGroup_AdjustPrice(id, value, perc) == -1 ? "true" : "false");
        //}

        public JsonResult ItemSearch(string searchWord)
        {
            var items = db.Items.Where(x => x.ArName.Contains(searchWord) && x.IsActive == true && x.IsDeleted == false).Select(x => x.ArName);
            return Json(items, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public ActionResult ImportItems()
        {
            ViewBag.DepartmentId = new SelectList(db.Departments.Where(i => i.IsDeleted == false && i.IsActive == true), "Id", "ArName");
            return View();
        }
        [SkipERPAuthorize]
        [HttpPost]
        [AllowAnonymous]

        public ActionResult ImportItems(int depId)
        {

            string json = "";
            var idsOnWebsite = db.Departments.Where(d => d.Id == depId).Select(d => new { d.IdOnWebSite, d.IdOnWebsite2, d.IdOnWebsite3 }).FirstOrDefault();
            using (WebClient wc = new WebClient())
            {
                //ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                //wc.UseDefaultCredentials = true;

                //  wc.Credentials = CredentialCache.DefaultCredentials;
                wc.Headers.Add("Content-Type:application/json; charset=utf-8"); //Content-Type  
                wc.Headers.Add("Accept:application/json");
                wc.Headers.Add("x-app-token", "kimostore");
                wc.Headers.Add("x-app-erp", "kimostore_erp");
                if (idsOnWebsite.IdOnWebsite3 != null)
                    json = wc.DownloadString("http://165.227.194.105/api/erp/products?category_id[]=" + idsOnWebsite.IdOnWebSite + "&category_id[]=" + idsOnWebsite.IdOnWebsite2 + "&category_id[]=" + idsOnWebsite.IdOnWebsite3);
                else if (idsOnWebsite.IdOnWebsite2 != null)
                    json = wc.DownloadString("http://165.227.194.105/api/erp/products?category_id[]=" + idsOnWebsite.IdOnWebSite + "&category_id[]=" + idsOnWebsite.IdOnWebsite2);
                else
                    json = wc.DownloadString("http://165.227.194.105/api/erp/products?category_id[]=" + idsOnWebsite.IdOnWebSite);
            }
            dynamic products = new JavaScriptSerializer().Deserialize<dynamic>(json);
            List<Item> items = new List<Item>();
            //  string last = db.Items.Where(i => i.DepartmentId == depId).OrderByDescending(i => i.Id).Select(i => i.Code).FirstOrDefault();
            string last = db.DepartmentItems.Where(i => i.DepartmentId == depId).OrderByDescending(i => i.ItemId).Select(i => i.Item.Code).FirstOrDefault();
            double lastCode = last != null ? double.Parse(last) + 1 : 1;
            db.Database.ExecuteSqlCommand($"update Item set IsDeleted=1 where Id in (select ItemId from DepartmentItem where DepartmentId={depId}" + ")");
            foreach (dynamic product in products["final"])
            {
                string idOnWebsite = product["_id"];
                Item item = db.Items.FirstOrDefault(i => i.IdOnWebSite == idOnWebsite);
                if (item != null)
                {//use EnName instead of ArName
                    item.EnName = product["name"]["ar"];
                    item.ArName = product["name"]["en"];
                    // item.Image = product["image"];
                    item.IsDeleted = false;
                    item.IsActive = true;
                    //  db.ItemPrices.RemoveRange(item.ItemPrices);
                    if (product["price"].Count > 0)
                    {
                        foreach (var itemPrice in item.ItemPrices)
                        {
                            if (itemPrice.CustomerGroupId == 1)
                            {
                                itemPrice.Price = product["price"]["default"];
                            }
                            else if (itemPrice.CustomerGroupId == 2)
                            {
                                itemPrice.Price = product["price"]["dealer"];
                            }
                            else if (itemPrice.CustomerGroupId == 3)
                            {
                                itemPrice.Price = product["price"]["top"];
                            }

                        }
                        //    item.ItemPrices = new List<ItemPrice>() {new ItemPrice {
                        //        Price = product["price"]["default"],
                        //        ItemUnitId=1,
                        //        Equivalent=1,
                        //        CustomerGroupId =1,
                        //        IsActive =true,
                        //        IsDeleted =false,
                        //IsDefault=true},
                        //    new ItemPrice{Price= product["price"]["dealer"] ,
                        //        ItemUnitId=1,
                        //        Equivalent=1,
                        //        CustomerGroupId=2,
                        //        IsActive =true,
                        //        IsDeleted =false,
                        //     IsDefault=false},
                        //    new ItemPrice{Price= product["price"]["top"],
                        //        ItemUnitId=1,
                        //        Equivalent=1,
                        //        CustomerGroupId=3,
                        //        IsActive =true,
                        //        IsDeleted =false,
                        //     IsDefault=false} };
                    }
                    db.Entry(item).State = EntityState.Modified;

                }
                else
                {

                    items.Add(new Item
                    {
                        Code = (lastCode++).ToString(),
                        EnName = product["name"]["ar"],
                        ArName = product["name"]["en"],
                        // Image = product["image"],
                        IsDeleted = false,
                        IsActive = true,
                        ItemGroupId = 1,

                        // DepartmentId = depId,
                        IdOnWebSite = idOnWebsite,
                        ItemTypeId = 1,
                        ItemPrices = new List<ItemPrice>() {
                        new ItemPrice {
                            Price = product["price"]["default"],
                            ItemUnitId=1,
                            Equivalent=1,
                            CustomerGroupId =1,
                            IsActive =true,
                            IsDeleted =false,
                        IsDefault=true},
                        new ItemPrice{Price= product["price"]["dealer"] ,
                            ItemUnitId=1,
                            Equivalent=1,
                            CustomerGroupId=2,

                            IsActive =true,
                            IsDeleted =false,

                         IsDefault=false},
                        new ItemPrice{Price=product["price"]["top"],
                            ItemUnitId=1,
                            Equivalent=1,
                            CustomerGroupId=3,
                            IsActive =true,
                            IsDeleted =false,

                         IsDefault=false}
                    }
                    });

                }
            }
            try
            {
                if (items.Count > 0)
                {
                    db.Items.AddRange(items);

                    var dpp = db.DepartmentItems.Select(x => x.ItemId).ToList();
                    var xx = db.Items.Where(p => !dpp.Contains(p.Id)).ToList();

                }

                db.SaveChanges();
                List<DepartmentItem> departmentItems = new List<DepartmentItem>();
                foreach (var item in items)
                {
                    departmentItems.Add(new DepartmentItem
                    {
                        ItemId = item.Id,
                        DepartmentId = depId
                    });

                }
                db.DepartmentItems.AddRange(departmentItems);
                db.SaveChanges();//to save departmentItems
                return Content("true");
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [SkipERPAuthorize]
        public ActionResult UpdateQuantities(int depId)
        {
            try
            {
                // QueryHelper.UpdateAllItemsQuantitiesOnWebsite(depId);
                return Content("true");
            }
            catch (Exception)
            {
                return Content("false");
            }

        }
        [SkipERPAuthorize]
        private string IdonWebsiteByDepartmentId(int id)
        {
            return db.Departments.Find(id).IdOnWebSite;
        }
        [SkipERPAuthorize]
        [AllowAnonymous]
        public JsonResult ImportDepartments()
        {
            string json;
            using (WebClient wc = new WebClient())
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                wc.UseDefaultCredentials = true;

                wc.Credentials = CredentialCache.DefaultCredentials;
                wc.Headers.Add("Authorization", "Bearer JSW1PSXPQEADS51867UTF2EA2MASDCVVFA90852SFK1490665476KALBQDCEFSDS7");
                json = wc.DownloadString("https://kimostore.net/api/erp/categories");
            }
            dynamic categories = new JavaScriptSerializer().Deserialize<dynamic>(json);
            List<Department> departments = new List<Department>();

            double lastCode = QueryHelper.CodeLastNum("Department") + 1;
            foreach (dynamic product in categories["categories"])
            {
                departments.Add(new Department
                {
                    Code = (lastCode++).ToString(),
                    ArName = product["name"]["ar"],
                    EnName = product["name"]["en"],
                    IsActive = true,
                    IsDeleted = false,
                    IdOnWebSite = int.Parse(product["id"])
                });
            }
            try
            {
                db.Departments.AddRange(departments);
                db.SaveChanges();
                return Json("true", JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public JsonResult GetItemsWithItemGroup()
        {
            //var x = db.ItemGroups.Where(a=>a.IsActive==true&&a.IsDeleted==false&&a.TypeId>0).Select(a=>new
            //{
            //    a.Id ,
            //    a.Code ,
            //    a.ArName ,
            //    a.EnName ,
            //    a.TypeId,
            //    a.ParentItemGroupId
            //}).ToList();
            //return Json(x, JsonRequestBehavior.AllowGet);
            //var x = db.GetItemsWithItemGroup().ToList();
            return Json(db.GetItemsWithItemGroup().ToList(), JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult GetLastTaxPercentage()
        {
            var TaxPercentage = db.TaxesPercentages.Where(a => a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).FirstOrDefault().TaxPercentage;
            return Json(TaxPercentage, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public ActionResult ChangeItemType()
        {
            //مادة خام
            var itemType = db.ItemTypes.Find(1);
            itemType.ArName = "مادة خام - سلعة";
            db.Entry(itemType).State = EntityState.Modified;
            db.SaveChanges();
            return View();
        }

        [SkipERPAuthorize]
        public ActionResult InsertIntoItemsFromItemInsertion()
        {
            try
            {
                db.InsertIntoItemsFromItemInsertion();
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException.Message;
                return Json(msg, JsonRequestBehavior.AllowGet);
            }

            return Json("success", JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public ActionResult ChangeFinancialPeriodCloseStatus()
        {
            try
            {
                db.Database.ExecuteSqlCommand($"update FinancialPeriods set Closed=null where IsActive=1");
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException.Message;
                return Json(msg, JsonRequestBehavior.AllowGet);
            }

            return Json("success", JsonRequestBehavior.AllowGet);
        }


        [SkipERPAuthorize]
        public ActionResult Barcode(int? ItemId, string barcode, string name)
        {
            var itemPrices = db.ItemPrices.Where(a => a.IsActive == true && a.IsDeleted == false && a.ItemId == ItemId).ToList();
            ViewBag.BarCodes = itemPrices;
            ViewBag.Name = name;
            ViewBag.Barcode = barcode;
            return View();
        }

        [SkipERPAuthorize]
        public ActionResult ItemsPricingBasedOnCustomerGroups(int? CustomerGroupId, int pageIndex = 1, int wantedRowsNo = 20)
        {
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            ViewBag.CustomerGroupId = new SelectList(db.CustomersGroups.Where(i => i.IsDeleted == false && i.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");

            var Details = db.ItemPrices.Where(a => a.IsDeleted == false && a.IsActive == true && a.CustomerGroupId == CustomerGroupId)
                .OrderBy(a => a.ItemId).Skip(skipRowsNo).Take(wantedRowsNo).ToList();

            ViewBag.Count = db.ItemPrices.Where(a => a.IsDeleted == false && a.IsActive == true && a.CustomerGroupId == CustomerGroupId).Select(a => new
            {
                ItemPriceId = a.Id,
                a.ItemId,
                ItemName = a.Barcode + " - " + a.Item.Code + " - " + a.Item.ArName,
                Price = a.Price != null ? a.Price : 0,
                a.ItemUnitId,
                ItemUnitName = a.ItemUnit.Code + " - " + a.ItemUnit.ArName,
                a.Equivalent,
                a.IsDefault,
            }).OrderBy(a => a.ItemId).Count();

            return View(Details);
        }
        [SkipERPAuthorize]
        [HttpPost]
        public ActionResult ItemsPricingBasedOnCustomerGroups(List<ItemPrice> ItemPrices)
        {
            if (ModelState.IsValid)
            {
                foreach (var item in ItemPrices)
                {
                    var itemprice = db.ItemPrices.Find(item.Id);
                    itemprice.Price = item.Price;
                    db.Entry(itemprice).State = EntityState.Modified;
                    try
                    {
                        db.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        var e = ex.InnerException.Message;
                    }
                }

                return Json(new { success = true });
            }
            var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();

            return Json(new
            {
                success = false,
                errors
            });
        }



        [SkipERPAuthorize]
        public ActionResult PrintItemsBarcode()
        {
            ViewBag.ItemId = new SelectList(db.Items.Where(i => i.IsDeleted == false && i.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");
            ViewBag.ItemPriceId = new SelectList(db.ItemPrices.Where(i => i.IsDeleted == false && i.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Barcode
            }), "Id", "ArName");

            ViewBag.DocumentTypeId = new SelectList(new List<dynamic> {
                new { id=1,name="فاتورة بيع"},
                new { id=2,name="فاتورة شراء"},
                new { id=3,name="سند صرف مخزني"},
                new { id=4,name="نقطة بيع"}}, "id", "name");
            return View();
        }

        [SkipERPAuthorize]
        [HttpPost]
        [AllowAnonymous]
        public ActionResult PrintItemsBarcode(List<ItemsPrintBarcodeQueue> itemsPrintBarcodeQueues)
        {
            if (ModelState.IsValid)
            {
                db.ItemsPrintBarcodeQueues.AddRange(itemsPrintBarcodeQueues);
                db.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [AllowAnonymous]
        [SkipERPAuthorize]
        public JsonResult GetDocumentNumberByDocumentTypeId(int? DocumentTypeId)
        {
            var Invoice = new List<dynamic>();
            if (DocumentTypeId == 1) // فاتورة بيع
            {
                Invoice.Add(db.SalesInvoices.Where(a => a.IsActive == true && a.IsDeleted == false && a.PosId == null).Select(a => new { a.Id, a.DocumentNumber }).ToList());
            }
            else if (DocumentTypeId == 2) // فاتورة شراء
            {
                Invoice.Add(db.PurchaseInvoices.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => new { a.Id, a.DocumentNumber }).ToList());
            }
            else if (DocumentTypeId == 3) // سند صرف مخزنى
            {
                Invoice.Add(db.StockIssueVouchers.Where(a => a.IsActive == true && a.IsDeleted == false && a.TypeId == 1).Select(a => new { a.Id, a.DocumentNumber }).ToList());
            }
            else if (DocumentTypeId == 4) // نقطة بيع
            {
                Invoice.Add(db.SalesInvoices.Where(a => a.IsActive == true && a.IsDeleted == false && a.PosId != null).Select(a => new { a.Id, a.DocumentNumber }).ToList());
            }

            return Json(Invoice, JsonRequestBehavior.AllowGet);
        }

        [AllowAnonymous]
        [SkipERPAuthorize]
        public JsonResult GetDocumentDetailsByDocumentTypeIdAndDocumentId(int? DocumentTypeId, int? DocumentId)
        {
            var InvoiceDetails = new List<dynamic>();
            if (DocumentTypeId == 1) // فاتورة بيع
            {
                InvoiceDetails.Add(db.SalesInvoiceDetails.Where(a => a.IsDeleted == false && a.MainDocId == DocumentId).Select(a => new { a.ItemId, a.ItemPriceId, ItemName = a.Item.Code + " - " + a.Item.ArName, Barcode = a.ItemPrice.Barcode, a.Qty }).ToList());
            }
            else if (DocumentTypeId == 2) // فاتورة شراء
            {
                InvoiceDetails.Add(db.PurchaseInvoiceDetails.Where(a => a.IsDeleted == false && a.MainDocId == DocumentId).Select(a => new { a.ItemId, a.ItemPriceId, ItemName = a.Item.Code + " - " + a.Item.ArName, Barcode = a.ItemPrice.Barcode, a.Qty }).ToList());
            }
            else if (DocumentTypeId == 3) // سند صرف مخزنى
            {
                InvoiceDetails.Add(db.StockIssueVoucherDetails.Where(a => a.IsDeleted == false && a.MainDocId == DocumentId).Select(a => new { a.ItemId, a.ItemPriceId, ItemName = a.Item.Code + " - " + a.Item.ArName, Barcode = a.ItemPrice.Barcode, a.Qty }).ToList());
            }
            else if (DocumentTypeId == 4) // نقطة بيع
            {
                InvoiceDetails.Add(db.SalesInvoiceDetails.Where(a => a.IsDeleted == false && a.MainDocId == DocumentId).Select(a => new { a.ItemId, a.ItemPriceId, ItemName = a.Item.Code + " - " + a.Item.ArName, Barcode = a.ItemPrice.Barcode, a.Qty }).ToList());
            }

            return Json(InvoiceDetails, JsonRequestBehavior.AllowGet);
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
    public class itemData
    {
        public decimal? Price { get; set; }
        public DateTime? VoucherDate { get; set; }
        public string VendorName { get; set; }
        public double? qty { get; set; }
    }
}
