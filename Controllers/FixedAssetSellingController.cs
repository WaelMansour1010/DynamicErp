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
using System.Security.Claims;
using MyERP.Repository;
using System.Data.Entity.Core.Objects;

namespace MyERP.Controllers
{
    public class FixedAssetSellingController : Controller
    {
        private readonly MySoftERPEntity db = new MySoftERPEntity();

        // GET: FixedAssetSelling
        public async Task<ActionResult> Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            Repository<FixedAssetSelling> repository = new Repository<FixedAssetSelling>(db);
            var depIds = await departmentRepository.UserDepartmentsIds(userId);
            Notification.GetNotification("FixedAssetSelling", "View", "Index", null, null, "مستند بيع اصول");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<FixedAssetSelling> fixedAssetSellings;
            if (string.IsNullOrEmpty(searchWord))
            {
                fixedAssetSellings = repository.GetAll().Where(s => s.IsDeleted == false && (userId == 1 || depIds.Contains(s.DepartmentId))).Include(s => s.Department).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = await repository.GetAll().Where(s => s.IsDeleted == false && (userId == 1 || depIds.Contains(s.DepartmentId))).CountAsync();
            }
            else
            {
                fixedAssetSellings = repository.GetAll().Where(s => s.IsDeleted == false && (userId == 1 || depIds.Contains(s.DepartmentId)) && s.DocumentNumber.Contains(searchWord)).Include(s => s.Department).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = await repository.GetAll().Where(s => s.IsDeleted == false && (userId == 1 || depIds.Contains(s.DepartmentId)) && s.DocumentNumber.Contains(searchWord)).CountAsync();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة بيع اصول",
                EnAction = "Index",
                ControllerName = "FixedAssetSelling",
                UserName = User.Identity.Name,
                UserId = userId,
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            return View(await fixedAssetSellings.ToListAsync());
        }

        [SkipERPAuthorize]
        public ActionResult _FixedAssetSellingValues(int fixedAssetId, int departmentId, DateTime date)
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

        // GET: FixedAssetSelling/Edit/5
        public async Task<ActionResult> AddEdit(int? id)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            CashboxReposistory cashboxReposistory = new CashboxReposistory(db);
            if (id == null)
            {
                SystemSetting systemSetting = await db.SystemSettings.AnyAsync() ? await db.SystemSettings.FirstOrDefaultAsync() : new SystemSetting();

                ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", systemSetting.DefaultDepartmentId);
                ViewBag.CashboxId= new SelectList(await cashboxReposistory.UserCashboxes(userId, systemSetting.DefaultDepartmentId).ToListAsync(), "Id", "ArName", systemSetting.DefaultCashBoxId);
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
            FixedAssetSelling fixedAssetSelling = await db.FixedAssetSellings.FindAsync(id);
            if (fixedAssetSelling == null)
            {
                return HttpNotFound();
            }
            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", fixedAssetSelling.DepartmentId);
            ViewBag.CashboxId = new SelectList(await cashboxReposistory.UserCashboxes(userId, fixedAssetSelling.DepartmentId).ToListAsync(), "Id", "ArName", fixedAssetSelling.CashboxId);

            ViewBag.Date = fixedAssetSelling.Date.ToString("yyyy-MM-ddTHH:mm");
            ViewBag.Editable = db.FixedAssetSellings.Where(x => x.IsDeleted == false).Max(x => x.Id) == id;
            int sysPageId = QueryHelper.SourcePageId("FixedAssetSelling");
            JournalEntry journal = db.JournalEntries.FirstOrDefault(j => j.SourceId == id && j.SourcePageId == sysPageId);
            ViewBag.Journal = journal;
            ViewBag.Next = QueryHelper.Next((int)id, "FixedAssetSelling");
            ViewBag.Previous = QueryHelper.Previous((int)id, "FixedAssetSelling");
            ViewBag.Last = QueryHelper.GetLast("FixedAssetSelling");
            ViewBag.First = QueryHelper.GetFirst("FixedAssetSelling");

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل بيع اصول",
                EnAction = "AddEdit",
                ControllerName = "FixedAssetSelling",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = fixedAssetSelling.Id,
                CodeOrDocNo = fixedAssetSelling.DocumentNumber
            });
            return View(fixedAssetSelling);
        }

        [HttpPost]
        public ActionResult AddEdit([Bind(Include = "Id,DocumentNumber,BranchId,Total,Date,DepartmentId,CurrencyId,CurrencyEquivalent,IsDeleted,IsActive,IsPosted,IsLinked,AutoCreated,UserId,Notes,CashboxId,FixedAssetSellingDetails")] FixedAssetSelling fixedAssetSelling)
        {
            if (ModelState.IsValid)
            {
                var id = fixedAssetSelling.Id;
                fixedAssetSelling.IsDeleted = false;
                if (id > 0)
                {
                    MyXML.xPathName = "FixedAssetSellingDetails";
                    var FixedAssetSellingDetailsXML = MyXML.GetXML(fixedAssetSelling.FixedAssetSellingDetails);
                    db.FixedAssetSelling_Update(fixedAssetSelling.Id, fixedAssetSelling.DocumentNumber, fixedAssetSelling.BranchId, fixedAssetSelling.Total, fixedAssetSelling.Date, fixedAssetSelling.DepartmentId, fixedAssetSelling.CurrencyId, fixedAssetSelling.CurrencyEquivalent, fixedAssetSelling.IsDeleted, fixedAssetSelling.IsActive, fixedAssetSelling.IsPosted, fixedAssetSelling.IsLinked, fixedAssetSelling.AutoCreated, fixedAssetSelling.UserId, fixedAssetSelling.Notes, fixedAssetSelling.CashboxId, FixedAssetSellingDetailsXML);
                }
                else
                {
                    MyXML.xPathName = "FixedAssetSellingDetails";
                    var FixedAssetSellingDetailsXML = MyXML.GetXML(fixedAssetSelling.FixedAssetSellingDetails);
                    var idResult = new ObjectParameter("Id", typeof(Int32));
                    db.FixedAssetSelling_Insert(idResult, fixedAssetSelling.DocumentNumber, fixedAssetSelling.BranchId, fixedAssetSelling.Total, fixedAssetSelling.Date, fixedAssetSelling.DepartmentId, fixedAssetSelling.CurrencyId, fixedAssetSelling.CurrencyEquivalent, fixedAssetSelling.IsDeleted, fixedAssetSelling.IsActive, fixedAssetSelling.IsPosted, fixedAssetSelling.IsLinked, fixedAssetSelling.AutoCreated, fixedAssetSelling.UserId, fixedAssetSelling.Notes, fixedAssetSelling.CashboxId, FixedAssetSellingDetailsXML);
                    id = (int)idResult.Value;
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = fixedAssetSelling.Id > 0 ? "تعديل بيع اصول " : "اضافة بيع اصول",
                    EnAction = "AddEdit",
                    ControllerName = "FixedAssetSelling",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = fixedAssetSelling.DocumentNumber
                });
                Notification.GetNotification("FixedAssetSelling", fixedAssetSelling.Id > 0 ? "Edit" : "Add", "AddEdit", id, null, "مستند بيع اصول");

                return Json(new { success = "true" });
            }
            return Json(new { success = "false" });
        }

        // POST: FixedAssetSelling/Delete/5
        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            int maxId = db.FixedAssetSellings.Where(x => x.IsDeleted == false).Max(x => x.Id);
            if (maxId == id)
            {
                db.FixedAssetSelling_Delete(id);
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = " حذف مستند بيع اصول",
                    EnAction = "AddEdit",
                    ControllerName = "FixedAssetSelling",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = db.FixedAssetSellings.Where(x => x.Id == id).Select(x => x.DocumentNumber).FirstOrDefault()
                });
                Notification.GetNotification("FixedAssetSelling", "Delete", "Delete", id, null, "مستند بيع اصول");

                return Content("true");
            }
            else
                return Content("false");
        }
        [SkipERPAuthorize]
        public JsonResult FixedAssetsByDepartment(int id, int? fixedAssetSellingId)
        {
            var fixedAssets = db.FixedAssetSellingDetails.Where(x => x.MainDocId == fixedAssetSellingId).Select(x => x.FixedAssetId);
            return Json(db.FixedAssets.Where(x => x.IsActive == true && x.IsDeleted == false && x.DepartmentId == id && (fixedAssets.Contains(x.Id) || x.FixedAssetStatusId == 1 || x.FixedAssetStatusId == 2)).Select(x => new { x.Id, ArName = x.Code + " - " + x.ArName }), JsonRequestBehavior.AllowGet);
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
            var lastObj = db.FixedAssetSellings.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.FixedAssetSellings.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.Date.Month == VoucherDate.Value.Month && a.Date.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.FixedAssetSellings.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.Date.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

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
            //var docNo = QueryHelper.DocLastNum(id, "FixedAssetSelling");
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
