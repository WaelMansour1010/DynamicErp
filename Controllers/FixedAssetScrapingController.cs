using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using MyERP.Repository;
using System.Security.Claims;
using System.Data.Entity.Core.Objects;

namespace MyERP.Controllers
{
    public class FixedAssetScrapingController : Controller
    {
        private readonly MySoftERPEntity db = new MySoftERPEntity();

        // GET: FixedAssetScraping
        public async Task<ActionResult> Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            Repository<FixedAssetScraping> repository = new Repository<FixedAssetScraping>(db);
            var depIds = await departmentRepository.UserDepartmentsIds(userId);
            Notification.GetNotification("FixedAssetScraping", "View", "Index", null, null, "مستند استبعاد اصول");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<FixedAssetScraping> fixedAssetScrapings;
            if (string.IsNullOrEmpty(searchWord))
            {
                fixedAssetScrapings = repository.GetAll().Where(s => s.IsDeleted == false && (userId == 1 || depIds.Contains(s.DepartmentId))).Include(s => s.Department).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = await repository.GetAll().Where(s => s.IsDeleted == false && (userId == 1 || depIds.Contains(s.DepartmentId))).CountAsync();
            }
            else
            {
                fixedAssetScrapings = repository.GetAll().Where(s => s.IsDeleted == false && (userId == 1 || depIds.Contains(s.DepartmentId)) && s.DocumentNumber.Contains(searchWord)).Include(s => s.Department).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = await repository.GetAll().Where(s => s.IsDeleted == false && (userId == 1 || depIds.Contains(s.DepartmentId)) && s.DocumentNumber.Contains(searchWord)).CountAsync();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة استبعاد اصول",
                EnAction = "Index",
                ControllerName = "FixedAssetScraping",
                UserName = User.Identity.Name,
                UserId = userId,
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            return View(await fixedAssetScrapings.ToListAsync());
        }

        [SkipERPAuthorize]
        public ActionResult _FixedAssetScrapingValues(int fixedAssetId, int departmentId, DateTime date)
        {
            try
            {
                return PartialView(db.FixedAsset_DepreciationValues(date, fixedAssetId, null, departmentId).FirstOrDefault());
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                return Content(ex.Message);
            }
        }

        // GET: FixedAssetScraping/Edit/5
        public async Task<ActionResult> AddEdit(int? id)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            if (id == null)
            {
                SystemSetting systemSetting =await db.SystemSettings.AnyAsync()? await db.SystemSettings.FirstOrDefaultAsync():new SystemSetting();

                ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", systemSetting.DefaultDepartmentId);

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
            FixedAssetScraping fixedAssetScraping = await db.FixedAssetScrapings.FindAsync(id);
            if (fixedAssetScraping == null)
            {
                return HttpNotFound();
            }
            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", fixedAssetScraping.DepartmentId);

            ViewBag.Date = fixedAssetScraping.Date.ToString("yyyy-MM-ddTHH:mm");
            ViewBag.Editable = db.FixedAssetScrapings.Where(x => x.IsDeleted == false).Max(x => x.Id) == id;
            int sysPageId = QueryHelper.SourcePageId("FixedAssetScraping");
            JournalEntry journal = db.JournalEntries.FirstOrDefault(j => j.SourceId == id && j.SourcePageId == sysPageId);
            ViewBag.Journal = journal;
            ViewBag.Next = QueryHelper.Next((int)id, "FixedAssetScraping");
            ViewBag.Previous = QueryHelper.Previous((int)id, "FixedAssetScraping");
            ViewBag.Last = QueryHelper.GetLast("FixedAssetScraping");
            ViewBag.First = QueryHelper.GetFirst("FixedAssetScraping");

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل استبعاد اصول",
                EnAction = "AddEdit",
                ControllerName = "FixedAssetScraping",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = fixedAssetScraping.Id,
                CodeOrDocNo = fixedAssetScraping.DocumentNumber
            });
            return View(fixedAssetScraping);
        }

        [HttpPost]
        public ActionResult AddEdit([Bind(Include = "Id,DocumentNumber,BranchId,Total,Date,DepartmentId,CurrencyId,CurrencyEquivalent,IsDeleted,IsActive,IsPosted,IsLinked,AutoCreated,UserId,Notes,FixedAssetScrapingDetails")] FixedAssetScraping fixedAssetScraping)
        {
            if (ModelState.IsValid)
            {
                var id = fixedAssetScraping.Id;
                fixedAssetScraping.IsDeleted = false;
                if (id > 0)
                {
                    MyXML.xPathName = "FixedAssetScrapingDetails";
                    var FixedAssetScrapingDetailsXML = MyXML.GetXML(fixedAssetScraping.FixedAssetScrapingDetails);
                    db.FixedAssetScraping_Update(fixedAssetScraping.Id, fixedAssetScraping.DocumentNumber, fixedAssetScraping.BranchId, fixedAssetScraping.Total, fixedAssetScraping.Date, fixedAssetScraping.DepartmentId, fixedAssetScraping.CurrencyId, fixedAssetScraping.CurrencyEquivalent, fixedAssetScraping.IsDeleted, fixedAssetScraping.IsActive, fixedAssetScraping.IsPosted, fixedAssetScraping.IsLinked, fixedAssetScraping.AutoCreated, fixedAssetScraping.UserId, fixedAssetScraping.Notes, FixedAssetScrapingDetailsXML);
                }
                else
                {
                    MyXML.xPathName = "FixedAssetScrapingDetails";
                    var FixedAssetScrapingDetailsXML = MyXML.GetXML(fixedAssetScraping.FixedAssetScrapingDetails);
                    var idResult = new ObjectParameter("Id", typeof(Int32));
                    db.FixedAssetScraping_Insert(idResult, fixedAssetScraping.DocumentNumber, fixedAssetScraping.BranchId, fixedAssetScraping.Total, fixedAssetScraping.Date, fixedAssetScraping.DepartmentId, fixedAssetScraping.CurrencyId, fixedAssetScraping.CurrencyEquivalent, fixedAssetScraping.IsDeleted, fixedAssetScraping.IsActive, fixedAssetScraping.IsPosted, fixedAssetScraping.IsLinked, fixedAssetScraping.AutoCreated, fixedAssetScraping.UserId, fixedAssetScraping.Notes, FixedAssetScrapingDetailsXML);
                    id = (int)idResult.Value;
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = fixedAssetScraping.Id > 0 ? "تعديل  مستند استبعاد اصول " : "اضافة مستند استبعاد اصول",
                    EnAction = "AddEdit",
                    ControllerName = "FixedAssetScraping",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = fixedAssetScraping.DocumentNumber
                });
                Notification.GetNotification("FixedAssetScraping", fixedAssetScraping.Id > 0 ? "Edit" : "Add", "AddEdit", id, null, "مستند استبعاد اصول");

                return Json(new { success = "true" });
            }
            return Json(new { success = "false" });
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            int maxId = db.FixedAssetScrapings.Where(x => x.IsDeleted == false).Max(x => x.Id);
            if (maxId == id)
            {
                db.FixedAssetScraping_Delete(id);
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = " حذف مستند استبعاد اصول",
                    EnAction = "AddEdit",
                    ControllerName = "FixedAssetScraping",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = db.FixedAssetScrapings.Where(x => x.Id == id).Select(x => x.DocumentNumber).FirstOrDefault()
                });
                Notification.GetNotification("FixedAssetScraping", "Delete", "Delete", id, null, "مستند اهلاك اصول");

                return Content("true");
            }
            else
                return Content("false");
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
            var lastObj = db.FixedAssetScrapings.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.FixedAssetScrapings.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.Date.Month == VoucherDate.Value.Month && a.Date.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.FixedAssetScrapings.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.Date.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

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
            //var docNo = QueryHelper.DocLastNum(id, "FixedAssetScraping");
            //double i = (docNo) + 1;
            //return Json(i, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult FixedAssetsByDepartment(int id, int? fixedAssetScrapingId)
        {
            var fixedAssets = db.FixedAssetScrapingDetails.Where(x => x.MainDocId == fixedAssetScrapingId).Select(x => x.FixedAssetId);
            return Json(db.FixedAssets.Where(x => x.IsActive == true && x.IsDeleted == false && x.DepartmentId == id && ( fixedAssets.Contains(x.Id) || x.FixedAssetStatusId == 1 || x.FixedAssetStatusId == 2)).Select(x => new { x.Id, ArName = x.Code + " - " + x.ArName }), JsonRequestBehavior.AllowGet);
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
