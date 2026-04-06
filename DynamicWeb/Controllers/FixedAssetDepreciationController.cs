using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using MyERP.Repository;

namespace MyERP.Controllers
{
    public class FixedAssetDepreciationController : Controller
    {
        private readonly MySoftERPEntity db = new MySoftERPEntity();

        // GET: FixedAssetDepreciation
        public async Task<ActionResult> Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            Repository<FixedAssetDepreciation> repository = new Repository<FixedAssetDepreciation>(db);
            var depIds = await departmentRepository.UserDepartmentsIds(userId);
            Notification.GetNotification("FixedAssetDepreciation", "View", "Index", null, null, "مستند اهلاك اصول");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            IQueryable<FixedAssetDepreciation> fixedAssetDepreciations;

            if (string.IsNullOrEmpty(searchWord))
            {
                fixedAssetDepreciations = repository.GetAll().Where(s => s.IsDeleted == false && (userId == 1 || depIds.Contains(s.DepartmentId))).Include(s => s.Department).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = await repository.GetAll().Where(s => s.IsDeleted == false && (userId == 1 || depIds.Contains(s.DepartmentId))).CountAsync();

            }
            else
            {
                fixedAssetDepreciations = repository.GetAll().Where(s => s.IsDeleted == false && (userId == 1 || depIds.Contains(s.DepartmentId)) && s.DocumentNumber.Contains(searchWord)).Include(s => s.Department).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = await repository.GetAll().Where(s => s.IsDeleted == false && (userId == 1 || depIds.Contains(s.DepartmentId)) && s.DocumentNumber.Contains(searchWord)).CountAsync();
            }

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة مستند اهلاك اصول",
                EnAction = "Index",
                ControllerName = "FixedAssetDepreciation",
                UserName = User.Identity.Name,
                UserId = userId,
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            return View(await fixedAssetDepreciations.ToListAsync());
        }

        // GET: FixedAssetDepreciation/Edit/5
        public async Task<ActionResult> AddEdit(int? id)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            ViewBag.FixedAssetGroupId = new SelectList(db.FixedAssetsGroups.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");
            if (id == null)
            {
                SystemSetting systemSetting = db.SystemSettings.FirstOrDefault();
                ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", systemSetting.DefaultDepartmentId);
                ViewBag.MethodOfInput = new SelectList(new List<dynamic> { new { Id = 0, ArName = "أصل" }, new { Id = 1, ArName = "مجموعة أصول" } }, "Id", "ArName");
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
            FixedAssetDepreciation fixedAssetDepreciation = await db.FixedAssetDepreciations.FindAsync(id);
            if (fixedAssetDepreciation == null)
            {
                return HttpNotFound();
            }
            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", fixedAssetDepreciation.DepartmentId);

            ViewBag.MethodOfInput = new SelectList(new List<dynamic> { new { Id = 0, ArName = "أصل" }, new { Id = 1, ArName = "مجموعة أصول" } }, "Id", "ArName", fixedAssetDepreciation.MethodOfInput);
            ViewBag.Date = fixedAssetDepreciation.Date.ToString("yyyy-MM-ddTHH:mm");
            ViewBag.Editable = db.FixedAssetDepreciations.Where(x => x.IsDeleted == false).Max(x => x.Id) == id;
            int sysPageId = QueryHelper.SourcePageId("FixedAssetDepreciation");
            JournalEntry journal = db.JournalEntries.FirstOrDefault(j => j.SourceId == id && j.SourcePageId == sysPageId);
            ViewBag.Journal = journal;
            ViewBag.Next = QueryHelper.Next((int)id, "FixedAssetDepreciation");
            ViewBag.Previous = QueryHelper.Previous((int)id, "FixedAssetDepreciation");
            ViewBag.Last = QueryHelper.GetLast("FixedAssetDepreciation");
            ViewBag.First = QueryHelper.GetFirst("FixedAssetDepreciation");

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل مستند اهلاك اصول",
                EnAction = "AddEdit",
                ControllerName = "FixedAssetDepreciation",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = fixedAssetDepreciation.Id,
                CodeOrDocNo = fixedAssetDepreciation.DocumentNumber
            });
            return View(fixedAssetDepreciation);
        }

        // POST: FixedAssetDepreciation/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public ActionResult AddEdit([Bind(Include = "Id,DocumentNumber,BranchId,Total,Date,CurrencyId,CurrencyEquivalent,MethodOfInput,IsDeleted,IsActive,IsPosted,IsLinked,AutoCreated,UserId,Notes,FixedAssetDepreciationDetails,DepartmentId")] FixedAssetDepreciation fixedAssetDepreciation)
        {
            if (ModelState.IsValid)
            {
                var id = fixedAssetDepreciation.Id;
                fixedAssetDepreciation.IsDeleted = false;
                if (id > 0)
                {
                    MyXML.xPathName = "FixedAssetDepreciationDetails";
                    var FixedAssetDepreciationDetailsXML = MyXML.GetXML(fixedAssetDepreciation.FixedAssetDepreciationDetails);
                    db.FixedAssetDepreciation_Update(fixedAssetDepreciation.Id, fixedAssetDepreciation.DocumentNumber, fixedAssetDepreciation.BranchId, fixedAssetDepreciation.Total, fixedAssetDepreciation.Date, fixedAssetDepreciation.DepartmentId, fixedAssetDepreciation.CurrencyId, fixedAssetDepreciation.CurrencyEquivalent, fixedAssetDepreciation.MethodOfInput, fixedAssetDepreciation.IsDeleted, fixedAssetDepreciation.IsActive, fixedAssetDepreciation.IsPosted, fixedAssetDepreciation.IsLinked, fixedAssetDepreciation.AutoCreated, fixedAssetDepreciation.UserId, fixedAssetDepreciation.Notes, FixedAssetDepreciationDetailsXML);
                }
                else
                {
                    MyXML.xPathName = "FixedAssetDepreciationDetails";
                    var FixedAssetDepreciationDetailsXML = MyXML.GetXML(fixedAssetDepreciation.FixedAssetDepreciationDetails);
                    var idResult = new ObjectParameter("Id", typeof(Int32));
                    db.FixedAssetDepreciation_Insert(idResult, fixedAssetDepreciation.DocumentNumber, fixedAssetDepreciation.BranchId, fixedAssetDepreciation.Total, fixedAssetDepreciation.Date, fixedAssetDepreciation.DepartmentId, fixedAssetDepreciation.CurrencyId, fixedAssetDepreciation.CurrencyEquivalent, fixedAssetDepreciation.MethodOfInput, fixedAssetDepreciation.IsDeleted, fixedAssetDepreciation.IsActive, fixedAssetDepreciation.IsPosted, fixedAssetDepreciation.IsLinked, fixedAssetDepreciation.AutoCreated, fixedAssetDepreciation.UserId, fixedAssetDepreciation.Notes, FixedAssetDepreciationDetailsXML);
                    id = (int)idResult.Value;
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = fixedAssetDepreciation.Id > 0 ? "تعديل  مستند اهلاك اصول " : "اضافة مستند اهلاك اصول",
                    EnAction = "AddEdit",
                    ControllerName = "FixedAssetDepreciation",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = fixedAssetDepreciation.DocumentNumber
                });
                Notification.GetNotification("FixedAssetDepreciation", fixedAssetDepreciation.Id > 0 ? "Edit" : "Add", "AddEdit", id, null, "مستند اهلاك اصول");

                return Json(new { success = "true" });
            }
            return Json(new { success = "false" });
        }

        [SkipERPAuthorize]
        public ActionResult _FixedAssetDepreciationValues(int? fixedAssetId, int? fixedAssetGrpId, int? departmentId, DateTime date)
        {
            try
            {
                return PartialView(db.FixedAsset_DepreciationValues(date, fixedAssetId, fixedAssetGrpId, departmentId));
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                return Content(ex.Message);
            }
        }

        [SkipERPAuthorize]
        public JsonResult FixedAssetsByDepartment(int id)
        {
            return Json(db.FixedAssets.Where(x => x.IsActive == true && x.IsDeleted == false && x.DepartmentId == id && ( x.FixedAssetStatusId == 1 || x.FixedAssetStatusId == 2)).Select(x => new { x.Id, ArName = x.Code + " - " + x.ArName }), JsonRequestBehavior.AllowGet);
        }

        // POST: FixedAssetDepreciation/Delete/5
        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            int maxId = db.FixedAssetDepreciations.Where(x => x.IsDeleted == false).Max(x => x.Id);
            if (maxId == id)
            {
                db.FixedAssetDepreciation_Delete(id);
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = " حذف مستند اهلاك اصول",
                    EnAction = "AddEdit",
                    ControllerName = "FixedAssetDepreciation",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = db.FixedAssetDepreciations.Where(x => x.Id == id).Select(x => x.DocumentNumber).FirstOrDefault()
                });
                Notification.GetNotification("FixedAssetDepreciation", "Delete", "Delete", id, null, "مستند اهلاك اصول");

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
            var lastObj = db.FixedAssetDepreciations.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.FixedAssetDepreciations.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.Date.Month == VoucherDate.Value.Month && a.Date.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.FixedAssetDepreciations.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.Date.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

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
            //var docNo = QueryHelper.DocLastNum(id, "FixedAssetDepreciation");
            //double i = (docNo) + 1;
            //return Json(i, JsonRequestBehavior.AllowGet);
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
