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
using Newtonsoft.Json;

namespace MyERP.Controllers.SystemSettings
{
    public class WarehouseOBController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: WarehouseOB
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            SystemSetting sysObj = db.SystemSettings.FirstOrDefault();
            ViewBag.UseExpiryDateForItems = sysObj.UseExpiryDateForItems;

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة الارصدة الافتتاحية للمخازن",
                EnAction = "Index",
                ControllerName = "WarehouseOB",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("WarehouseOB", "View", "Index", null, null, "الارصدة الافتتاحية للمخازن");

            //////////-----------------------------------------------------------------------

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            /////////////////////////// Search ////////////////////
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            var depIds = db.Departments.Where(d => (userId == 1 || db.UserDepartments.Where(u => u.UserId == userId && u.Privilege == true).Select(u => u.DepartmentId).Contains(d.Id)) && d.IsDeleted == false && d.IsActive == true).Select(d => (int?)d.Id);
            IQueryable<WarehouseOB> warehouseObs;

            if (string.IsNullOrEmpty(searchWord))
            {
                warehouseObs = db.WarehouseOBs.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId)).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.WarehouseOBs.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId)).Count();
            }
            else
            {
                warehouseObs = db.WarehouseOBs.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (s.DocumentNumber.Contains(searchWord) || s.Branch.ArName.Contains(searchWord) ||
                                                                               s.Warehouse.ArName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Date.ToString().Contains(searchWord) || s.Currency.ArName.Contains(searchWord) || s.CurrencyEquivalent.ToString().Contains(searchWord) || s.MoneyTotal.ToString().Contains(searchWord) || s.Notes.Contains(searchWord))).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.WarehouseOBs.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (s.DocumentNumber.Contains(searchWord) || s.Branch.ArName.Contains(searchWord) ||
                                                                               s.Warehouse.ArName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Date.ToString().Contains(searchWord) || s.Currency.ArName.Contains(searchWord) || s.CurrencyEquivalent.ToString().Contains(searchWord) || s.MoneyTotal.ToString().Contains(searchWord) || s.Notes.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(warehouseObs.ToList());
        }

        // GET: WarehouseOB/Edit/5
        public ActionResult AddEdit(int? id)
        {
            SystemSetting sysObj = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
            ViewData["ShowSerialNumbers"] = sysObj.ShowSerialNumbers == true;
            ViewBag.UseExpiryDateForItems = sysObj.UseExpiryDateForItems;
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            if (id == null)
            {
                if (userId == 1)
                {
                    ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                    {
                        b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", sysObj.DefaultDepartmentId);
                    ViewBag.WarehouseId = new SelectList(db.Warehouses.Where(b => b.IsActive == true && b.IsDeleted == false && b.DepartmentId == sysObj.DefaultDepartmentId).Select(b => new
                    {
                        b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", sysObj.DefaultWarehouseId);
                }
                else
                {
                    ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId && d.Department.IsActive == true && d.Department.IsDeleted == false).Select(b => new
                    {
                        Id = b.DepartmentId,
                        ArName = b.Department.Code + " - " + b.Department.ArName
                    }), "Id", "ArName", sysObj.DefaultDepartmentId);
                    ViewBag.WarehouseId = new SelectList(db.UserWareHouses.Where(b => b.UserId == userId && b.Warehouse.IsActive == true && b.Warehouse.IsDeleted == false && b.Warehouse.DepartmentId == sysObj.DefaultDepartmentId).Select(b => new
                    {
                        Id = b.WareHouseId,
                        ArName = b.Warehouse.Code + " - " + b.Warehouse.ArName
                    }), "Id", "ArName", sysObj.DefaultWarehouseId);
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

                ViewBag.Date = cTime.ToString("yyyy-MM-ddTHH:mm");
                return View();
            }
            WarehouseOB warehouseOB = db.WarehouseOBs.Find(id);
            if (warehouseOB == null)
            {
                return HttpNotFound();
            }
            if (userId == 1)
            {
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", warehouseOB.DepartmentId);
                ViewBag.WarehouseId = new SelectList(db.Warehouses.Where(b => b.IsActive == true && b.IsDeleted == false && b.DepartmentId == warehouseOB.DepartmentId).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", warehouseOB.WarehouseId);
            }
            else
            {
                ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", warehouseOB.DepartmentId);
                ViewBag.WarehouseId = new SelectList(db.UserWareHouses.Where(b => b.UserId == userId && b.Warehouse.DepartmentId == warehouseOB.DepartmentId).Select(b => new
                {
                    Id = b.WareHouseId,
                    ArName = b.Warehouse.Code + " - " + b.Warehouse.ArName
                }), "Id", "ArName", warehouseOB.WarehouseId);
            }

            ViewBag.SerialNumbers = JsonConvert.SerializeObject(db.PurchaseSaleSerialNumbers.Where(s => s.SelectedId == id && s.PageSourceId == db.SystemPages.FirstOrDefault(d => d.TableName == "WarehouseOB").Id).Select(d => new { d.ItemId, d.SerialNumber }));

            ViewBag.Date = warehouseOB.Date.HasValue ? warehouseOB.Date.Value.ToString("yyyy-MM-ddTHH:mm") : "";

            ViewBag.Next = QueryHelper.Next((int)id, "WarehouseOB");
            ViewBag.Previous = QueryHelper.Previous((int)id, "WarehouseOB");
            ViewBag.Last = QueryHelper.GetLast("WarehouseOB");
            ViewBag.First = QueryHelper.GetFirst("WarehouseOB");

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل الارصدة الافتتاحية للمخازن",
                EnAction = "AddEdit",
                ControllerName = "WarehouseOB",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = warehouseOB.Id,
                CodeOrDocNo = warehouseOB.DocumentNumber
            });
            return View(warehouseOB);
        }

        // POST: WarehouseOB/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public JsonResult AddEdit(WarehouseOB warehouseOB, ICollection<PurchaseSaleSerialNumber> serialNumbers)
        {
            if (ModelState.IsValid)
            {
                warehouseOB.IsDeleted = false;
                var id = warehouseOB.Id;
                // Patch details
                DataTable patches = new DataTable("PatchDetails");
                DataColumn ItemId = new DataColumn("ItemId", typeof(int));
                DataColumn ExpireDate = new DataColumn("ExpireDate", typeof(DateTime));
                DataColumn PatchCode = new DataColumn("PatchCode", typeof(string));
                patches.Columns.Add(ItemId);
                patches.Columns.Add(ExpireDate);
                patches.Columns.Add(PatchCode);
                foreach (var detail in warehouseOB.WarehouseOBDetails)
                {
                    if (detail.ExpireDate != null)
                    {
                        var patch = db.Patches.Where(e => e.ExpireDate.Value.Day == detail.ExpireDate.Value.Day &&
                        e.ExpireDate.Value.Month == detail.ExpireDate.Value.Month &&
                        e.ExpireDate.Value.Year == detail.ExpireDate.Value.Year &&
                        e.ItemId == detail.ItemId).Any();
                        if (patch != true)
                        {
                            DataRow row = patches.NewRow();
                            row["ItemId"] = detail.ItemId;
                            row["ExpireDate"] = detail.ExpireDate;
                            row["PatchCode"] = detail.PatchCode;
                            patches.Rows.Add(row);
                        }
                    }
                }

                MyXML.xPathName = "PatchDetails";
                var warehouseOBPatchDetails = MyXML.GetXML(patches);



                if (warehouseOB.Id > 0)
                {
                    if (db.WarehouseOBs.Find(warehouseOB.Id).IsPosted == true)
                    {
                        return Json("false");
                    }
                    MyXML.xPathName = "WarehouseOBDetails";
                    var WarehouseOBDetails = MyXML.GetXML(warehouseOB.WarehouseOBDetails);
                    MyXML.xPathName = "SerialNumbers";
                    var SerialNumbersXML = MyXML.GetXML(serialNumbers);
                    db.WarehouseOB_Update(warehouseOB.Id, warehouseOB.DocumentNumber, warehouseOB.BranchId, warehouseOB.WarehouseId, warehouseOB.DepartmentId, warehouseOB.Date, warehouseOB.CurrencyId, warehouseOB.CurrencyEquivalent, warehouseOB.MoneyTotal, warehouseOB.IsActive, warehouseOB.IsDeleted, warehouseOB.IsLinked, warehouseOB.IsPosted, warehouseOB.IsCompleted, warehouseOB.IsAccepted, warehouseOB.UserId, warehouseOB.Notes, warehouseOB.Image, WarehouseOBDetails, SerialNumbersXML, warehouseOBPatchDetails);

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("WarehouseOB", "Edit", "AddEdit", id, null, "الارصدة الافتتاحية للمخازن");

                    ////////////////-----------------------------------------------------------------------

                }
                else
                {
                    warehouseOB.IsActive = true;
                    MyXML.xPathName = "WarehouseOBDetails";
                    var WarehouseOBDetails = MyXML.GetXML(warehouseOB.WarehouseOBDetails);
                    MyXML.xPathName = "SerialNumbers";
                    var SerialNumbersXML = MyXML.GetXML(serialNumbers);

                    db.WarehouseOB_Insert(warehouseOB.BranchId, warehouseOB.WarehouseId, warehouseOB.DepartmentId, warehouseOB.Date, warehouseOB.CurrencyId, warehouseOB.CurrencyEquivalent, warehouseOB.MoneyTotal, warehouseOB.IsActive, warehouseOB.IsDeleted, warehouseOB.IsLinked, false, warehouseOB.IsCompleted, warehouseOB.IsAccepted, warehouseOB.UserId, warehouseOB.Notes, warehouseOB.Image, WarehouseOBDetails, SerialNumbersXML, warehouseOBPatchDetails);

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("WarehouseOB", "Add", "AddEdit", warehouseOB.Id, null, "الارصدة الافتتاحية للمخازن");

                    //////-----------------------------------------------------------------------
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل الارصدة الافتتاحية للمخازن" : "اضافة الارصدة الافتتاحية للمخازن",
                    EnAction = "AddEdit",
                    ControllerName = "WarehouseOB",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = warehouseOB.Id > 0 ? warehouseOB.Id : db.WarehouseOBs.Max(i => i.Id),
                    CodeOrDocNo = warehouseOB.DocumentNumber
                });
                return Json("true");
            }
            var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();

            return Json(errors);
        }

        // POST: WarehouseOB/Delete/5
        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                WarehouseOB warehouseOB = db.WarehouseOBs.Find(id);
                if (warehouseOB.IsPosted == true)
                {
                    return Content("false");
                }
                db.WarehouseOB_Delete(id);

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف الارصدة الافتتاحية للمخازن",
                    EnAction = "AddEdit",
                    ControllerName = "WarehouseOB",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = warehouseOB.DocumentNumber
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("WarehouseOB", "Delete", "Delete", id, null, "الارصدة الافتتاحية للمخازن");

                ////////////-----------------------------------------------------------------------

                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
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
            var lastObj = db.WarehouseOBs.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.WarehouseOBs.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.Date.Value.Month == VoucherDate.Value.Month && a.Date.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.WarehouseOBs.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.Date.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

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
            //var docNo = QueryHelper.DocLastNum(id, "WarehouseOB");
            //double i = (docNo) + 1;
            //return Json(i, JsonRequestBehavior.AllowGet);
        }

        // GET
        [SkipERPAuthorize]
        public ActionResult WarehouseOBBarcode(int? id, int itemId)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            WarehouseOB warehouseOB = db.WarehouseOBs.Find(id);
            if (warehouseOB == null)
            {
                return HttpNotFound();
            }

            var warehouseOBItems = db.WarehouseOBDetails.Where(p => p.WarehouseOBId == id).ToList();

            ViewBag.WarehouseOBItems = warehouseOBItems;

            ViewBag.GeneralItemId = itemId;

            ViewBag.Date = warehouseOB.Date.HasValue ? warehouseOB.Date.Value.ToString("yyyy-MM-ddTHH:mm") : "";

            ViewBag.BranchId = new SelectList(db.Branches.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", warehouseOB.BranchId);
            if (userId == 1)
            {
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", warehouseOB.DepartmentId);
                ViewBag.WarehouseId = new SelectList(db.Warehouses.Where(b => b.IsActive == true && b.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", warehouseOB.WarehouseId);

            }
            else
            {
                ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", warehouseOB.DepartmentId);
                ViewBag.WarehouseId = new SelectList(db.UserWareHouses.Where(b => b.UserId == userId).Select(b => new
                {
                    Id = b.WareHouseId,
                    ArName = b.Warehouse.Code + " - " + b.Warehouse.ArName
                }), "Id", "ArName", warehouseOB.WarehouseId);

            }

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح باركود الارصدة الافتتاحية",
                EnAction = "AddEdit",
                ControllerName = "WarehouseOBBarcode",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = warehouseOB.Id,
                CodeOrDocNo = warehouseOB.DocumentNumber
            });

            return View(warehouseOB);
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
