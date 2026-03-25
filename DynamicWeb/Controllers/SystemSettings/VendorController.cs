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
using System.Threading;
using System.Globalization;
using System.Web.Script.Serialization;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace MyERP.Controllers
{
    public class VendorController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: Vendor
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمةالموردين",
                EnAction = "Index",
                ControllerName = "Vendor",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("Vendor", "View", "Index", null, null, "الموردين");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<Vendor> vendors;

            if (string.IsNullOrEmpty(searchWord))
            {
                vendors = db.Vendors.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Vendors.Where(s => s.IsDeleted == false).Count();
            }
            else
            {
                vendors = db.Vendors.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.Code.ToString().Contains(searchWord) || s.Company.ArName.Contains(searchWord) || s.ERPUser.Name.Contains(searchWord) || s.Notes.Contains(searchWord) || s.VendorsGroup.ArName.Contains(searchWord) || s.DealingDate.ToString().Contains(searchWord) || s.Address.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Vendors.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.Code.ToString().Contains(searchWord) || s.Company.ArName.Contains(searchWord) || s.ERPUser.Name.Contains(searchWord) || s.Notes.Contains(searchWord) || s.VendorsGroup.ArName.Contains(searchWord) || s.DealingDate.ToString().Contains(searchWord) || s.Address.Contains(searchWord))).Count();

            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(vendors.ToList());
        }

        //AddEdit
        public ActionResult AddEdit(int? id)
        {
            var SystemPageId = db.SystemPages.Where(a => a.IsActive == true && a.IsDeleted == false && a.ControllerName == "Vendor").FirstOrDefault().Id;
            if (id == null)
            {
                ViewBag.VendorsGroupId = new SelectList(db.VendorsGroups.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
                {
                    b.Id,
                    ArName = b.FixedPart + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.CurrencyId = new SelectList(db.Currencies.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                DateTime utcNow = DateTime.UtcNow;
                TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
                DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
                ViewBag.DealingDate = cTime.ToString("yyyy-MM-ddTHH:mm");
                return View();
            }
            Vendor vendor = db.Vendors.Find(id);
            if (vendor == null)
            {
                return HttpNotFound();
            }
            else
            {
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "فتح تفاصيل المورد",
                    EnAction = "AddEdit",
                    ControllerName = "Vendor",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "GET",
                    SelectedItem = vendor.Id,
                    ArItemName = vendor.ArName,
                    EnItemName = vendor.EnName,
                    CodeOrDocNo = vendor.Code
                });

                ViewBag.VendorsGroupId = new SelectList(db.VendorsGroups.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", vendor.VendorsGroupId);
                ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
                {
                    b.Id,
                    ArName = b.FixedPart + " - " + b.ArName
                }), "Id", "ArName",vendor.FieldsCodingId);
                ViewBag.CurrencyId = new SelectList(db.Currencies.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", vendor.CurrencyId);
                ViewBag.Next = QueryHelper.Next((int)id, "Vendor");
                ViewBag.Previous = QueryHelper.Previous((int)id, "Vendor");
                ViewBag.Last = QueryHelper.GetLast("Vendor");
                ViewBag.First = QueryHelper.GetFirst("Vendor");

                try
                {
                    ViewBag.DealingDate = vendor.DealingDate.Value.ToString("yyyy-MM-ddTHH:mm");
                    ViewBag.IdentityExpireDate = vendor.IdentityExpireDate.Value.ToString("yyyy-MM-ddTHH:mm");
                    ViewBag.IdentityIssueDate = vendor.IdentityIssueDate.Value.ToString("yyyy-MM-ddTHH:mm");

                }
                catch (Exception)
                {
                }
                return View(vendor);
            }

        }
       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddEdit(Vendor vendor, string newBtn)
        {
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);

            if (ModelState.IsValid)
            {
                vendor.IsDeleted = false;
                vendor.ObCredit = 0;
                vendor.ObDebit = 0;
                var id = vendor.Id;
                if (vendor.Id > 0)
                {
                    vendor.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    db.Entry(vendor).State = EntityState.Modified;

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("Vendor", "Edit", "AddEdit", id, null, "الموردين");
                }
                else
                {
                    vendor.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    vendor.IsActive = true;
                    //vendor.Code = (QueryHelper.CodeLastNum("Vendor") + 1).ToString();
                    vendor.Code = new JavaScriptSerializer().Serialize(SetCodeNum(vendor.FieldsCodingId).Data).ToString().Trim('"');

                    db.Vendors.Add(vendor);

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("Vendor", "Add", "AddEdit", vendor.Id, null, "الموردين");
                }
                db.SaveChanges();

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل المورد" : "اضافة مورد",
                    EnAction = "AddEdit",
                    ControllerName = "Vendor",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = vendor.Id > 0 ? vendor.Id : db.Vendors.Max(i => i.Id),
                    ArItemName = vendor.ArName,
                    EnItemName = vendor.EnName,
                    CodeOrDocNo = vendor.Code
                });
                if (newBtn == "saveAndNew")
                {
                    return RedirectToAction("AddEdit");

                }
                else
                {
                    return RedirectToAction("Index");
                }
            }
            else
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();
                ViewBag.AccountId = new SelectList(db.ChartOfAccounts.Where(a => (a.IsActive == true && a.IsDeleted == false && a.ClassificationId == 3)).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", vendor.AccountId);

                ViewBag.VendorsGroupId = new SelectList(db.VendorsGroups.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", vendor.VendorsGroupId);
                ViewBag.IdentityTypeId = new SelectList(db.IdentityTypes.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", vendor.IdentityTypeId);
                ViewBag.CurrencyId = new SelectList(db.Currencies.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", vendor.CurrencyId);
                return View(vendor);
            }
        }
        [HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                //-- Check if this Vendor is used in other Transactions 
                var check = db.CheckVendorExistanceInOtherTransactions(id).FirstOrDefault();
                if (check > 0)
                {
                    return Content("False");
                }
                else
                {
                    var balance = db.VendorOpenningBalances.Where(c => c.VendorId == id).Sum(c => c.OBDebit - c.OBCredit);
                    if (balance != null && Math.Abs((decimal)balance) > 0)
                    {
                        return Content("hasBalance");
                    }
                    Vendor vendor = db.Vendors.Find(id);
                    vendor.IsDeleted = true;

                    vendor.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    Random random = new Random();
                    const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                    var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
                    vendor.Code = Code;
                    vendor.FieldsCodingId = null;
                    db.Entry(vendor).State = EntityState.Modified;


                    db.SaveChanges();
                    QueryHelper.AddLog(new MyLog()
                    {
                        ArAction = "حذف المورد",
                        EnAction = "AddEdit",
                        ControllerName = "Vendor",
                        UserName = User.Identity.Name,
                        UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                        LogDate = DateTime.Now,
                        RequestMethod = "POST",
                        SelectedItem = id,
                        EnItemName = vendor.EnName,
                        ArItemName = vendor.ArName,
                        CodeOrDocNo = vendor.Code
                    });
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("Vendor", "Delete", "Delete", id, null, "الموردين");

                    ////////////-----------------------------------------------------------------------

                    return Content("true");
                }
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
                Vendor vendor = db.Vendors.Find(id);
                if (vendor.IsActive == true)
                {
                    vendor.IsActive = false;
                }
                else
                {
                    vendor.IsActive = true;
                }
                vendor.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(vendor).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)vendor.IsActive ? "تنشيط المورد" : "إلغاء تنشيط المورد",
                    EnAction = "AddEdit",
                    ControllerName = "Vendor",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = vendor.Id,
                    EnItemName = vendor.EnName,
                    ArItemName = vendor.ArName,
                    CodeOrDocNo = vendor.Code
                });
                ////-------------------- Notification-------------------------////
                if (vendor.IsActive == true)
                {
                    Notification.GetNotification("Vendor", "Activate/Deactivate", "ActivateDeactivate", id, true, "الموردين");
                }
                else
                {

                    Notification.GetNotification("Vendor", "Activate/Deactivate", "ActivateDeactivate", id, false, "الموردين");
                }
                //int pageid = db.Get_PageId("Vendor").SingleOrDefault().Value;
                //var actionId = db.PageActions.FirstOrDefault(c => c.Action == "ActivateDeactivate" && c.EnName == "Activate/Deactivate" && c.PageId == pageid).Id;
                //int userid = db.UserNotifications.FirstOrDefault(c => c.ActionId == actionId && c.PageId == pageid).UserId.Value;
                //var UserName = User.Identity.Name;
                //db.Sp_OccuredNotification(actionId, (bool)vendor.IsActive ? $" تنشيط  في شاشة الموردين{UserName}قام المستخدم  " : $"إلغاء تنشيط  في شاشة الموردين{UserName}قام المستخدم  ");
                //////////////////-----------------------------------------------------------------------

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
                    var code = db.Vendors.Where(a => a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
                    var code = db.Vendors.Where(a => a.Code.Contains(fixedPart) && a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
                double result = 0;
                var code = db.Vendors.Where(a => a.FieldsCodingId == null && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
                var x = code.FirstOrDefault();
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
            //var code = QueryHelper.CodeLastNum("Vendor");
            //return Json(code + 1, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult SetCodeNumExcel(int? FieldsCodingId,int row)
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
                    var code = db.Vendors.Where(a => a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
                    if (code.FirstOrDefault() == null)
                    {
                        result = 0 + 1+row;
                    }
                    else
                    {
                        result = (double.Parse(code.FirstOrDefault().ToString())) + 1+row;
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
                    var code = db.Vendors.Where(a => a.Code.Contains(fixedPart) && a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
                    if (code.FirstOrDefault() == null)
                    {
                        result = 0 + 1 + row;
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
                        result = double.Parse(LastCode.Substring(LastCode.LastIndexOf(fixedPart) + fixedPart.Length)) + 1 + row;
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
                double result = 0;
                var code = db.Vendors.Where(a => a.FieldsCodingId == null && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
                var x = code.FirstOrDefault();
                if (code.FirstOrDefault() == null)
                {
                    result = 0 + 1 + row;
                }
                else
                {
                    result = double.Parse(code.FirstOrDefault().ToString()) + 1 + row;
                }
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            //var code = QueryHelper.CodeLastNum("Vendor");
            //return Json(code + 1, JsonRequestBehavior.AllowGet);
        }



        public ActionResult ImportExcelFile()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ImportExcelFile(HttpPostedFileBase excelfile)
        {
            string Error;
            if (excelfile == null || excelfile.ContentLength == 0)
            {
                Error = "من فضلك اختر ملف";
                return Json(Error, JsonRequestBehavior.AllowGet);
            }
            else
            {
                if (excelfile.FileName.EndsWith("xls") || excelfile.FileName.EndsWith("xlsx"))
                {
                    string path = Server.MapPath("~/Content/" + excelfile.FileName);
                    if (System.IO.File.Exists(path))
                    {
                        System.IO.File.Delete(path);
                    }
                    excelfile.SaveAs(path);
                    List<Vendor> vendors = new List<Vendor>();

                    //------------------------- Work With Excel Without Download On Server -------------------------------------------//
                    SpreadsheetDocument spreadsheet = SpreadsheetDocument.Open(path, false);
                    WorkbookPart workbookPart = spreadsheet.WorkbookPart;
                    WorksheetPart worksheetPart = workbookPart.WorksheetParts.Last();
                    SheetData sheetData = worksheetPart.Worksheet.Elements<SheetData>().Last();

                    // ----------- Insert Into Data Table To Read Blank Cells From Excel ------- //
                    // Solve Ignoring Empty Cells  Problemm 

                    DataTable dt = new DataTable();
                    IEnumerable<Row> rowss = sheetData.Descendants<Row>();
                    foreach (Cell cell in rowss.ElementAt(0))
                    {
                        dt.Columns.Add(GetCellValue(spreadsheet, cell));
                    }
                    foreach (Row row in rowss) //this will also include your header row...
                    {
                        DataRow tempRow = dt.NewRow();
                        for (int i = 0; i < row.Descendants<Cell>().Count(); i++)
                        {
                            Cell cell = row.Descendants<Cell>().ElementAt(i);
                            int actualCellIndex = CellReferenceToIndex(cell);
                            tempRow[actualCellIndex] = GetCellValue(spreadsheet, cell);
                        }
                        dt.Rows.Add(tempRow);
                    }
                    var DataTableRows = dt.Rows;
                   // var VendorCode = new JavaScriptSerializer().Serialize(SetCodeNumExcel(28,0).Data).ToString().Trim('"');
                  
                        for (int row = 1; row < DataTableRows.Count; row++)
                    {
                        var RowData = DataTableRows[row];
                        Vendor record = new Vendor();
                        if (row == 1)
                        {
                            record.Code = new JavaScriptSerializer().Serialize(SetCodeNumExcel(28, 0).Data).ToString().Trim('"'); //(double.Parse(VendorCode)).ToString();
                        }
                        else
                        {
                            record.Code = new JavaScriptSerializer().Serialize(SetCodeNumExcel(28, row-1).Data).ToString().Trim('"');// (int.Parse(VendorCode) + row-1).ToString();
                        }
                        record.ArName = RowData[0] != null ? (RowData[0]).ToString() : null;
                        record.EnName = RowData[1] != null ? (RowData[1]).ToString() : null;
                        record.DealingDate = DateTime.Parse((RowData[2]).ToString());
                        var GroupCode= RowData[3] != null ?(RowData[3]).ToString() : null;
                        var Group = db.VendorsGroups.Where(a => a.IsActive == true && a.IsDeleted == false && a.Code == GroupCode).FirstOrDefault();
                        record.VendorsGroupId = Group!=null? Group.Id:0;
                        record.VendorsGroupId = record.VendorsGroupId == 0 ? null : record.VendorsGroupId;
                        record.Email = RowData[4] != null ? (RowData[4]).ToString() : null;
                        record.Mobile = RowData[5] != null ? (RowData[5]).ToString() : null;
                        record.Address = RowData[6] != null ? (RowData[6]).ToString() : null;
                        record.IdentityNumber = RowData[7] != null ? (RowData[7]).ToString() : null;
                        record.VATNumber = RowData[8] != null ? (RowData[8]).ToString() : null;
                        record.TaxNumber = RowData[9] != null ? (RowData[9]).ToString() : null;
                        record.IsActive = true;
                        record.IsDeleted = false;
                        record.ObCredit = 0;
                        record.ObDebit = 0;
                        record.FieldsCodingId = 28;

                        vendors.Add(record);
                    }
                    db.Vendors.AddRange(vendors);
                    try
                    {
                        db.SaveChanges();
                        //spreadsheet.Close();
                        return Json("success", JsonRequestBehavior.AllowGet);
                    }
                    catch (Exception ex)
                    {
                        var errors = ex.InnerException.InnerException.Message;
                    }
                    //spreadsheet.Close();
                    return Json("Error", JsonRequestBehavior.AllowGet);

                    // ----------- End Insert Into Data Table To Read Blank Cells From Excel ------- //
                }
                else
                {
                    Error = "نوع الملف غير صحيح";
                    return Json(Error, JsonRequestBehavior.AllowGet);
                }
            }
        }

        public static string GetCellValue(SpreadsheetDocument document, Cell cell)
        {
            SharedStringTablePart stringTablePart = document.WorkbookPart.SharedStringTablePart;
            string value = cell.CellValue != null ? cell.CellValue.InnerXml : null;

            if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
            {
                return stringTablePart.SharedStringTable.ChildElements[Int32.Parse(value)].InnerText;
            }
            else if (cell.CellReference.ToString().Contains("C") || cell.CellReference.ToString().Contains("F")) //Attend/LeaveDate
            {
                // Read Date
                var date = DateTime.FromOADate(double.Parse(cell.CellValue.Text)).ToString("yyyy-MM-ddTHH:mm");//ToString("dd/MM/yyyy");
                return date;
            }
            else
            {
                return value;
            }
        }
        private static int CellReferenceToIndex(Cell cell)
        {
            int index = 0;
            string reference = cell.CellReference.ToString().ToUpper();
            foreach (char ch in reference)
            {
                if (Char.IsLetter(ch))
                {
                    int value = (int)ch - (int)'A';
                    index = (index == 0) ? value : ((index + 1) * 26) + value;
                }
                else
                {
                    return index;
                }
            }
            return index;
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
