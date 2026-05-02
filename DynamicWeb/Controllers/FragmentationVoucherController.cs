using DevExpress.Data.WcfLinq.Helpers;
using MyERP.Models;
using MyERP.Repository;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace MyERP.Controllers
{
    public class FragmentationVoucherController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: FragmentationVoucher
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة مستند التجزئة",
                EnAction = "Index",
                ControllerName = "FragmentationVoucher",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });

            Notification.GetNotification("FragmentationVoucher", "View", "Index", null, null, " مستند التجزئة");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }

            IQueryable<FragmentationVoucher> fragmentationVouchers;
            if (string.IsNullOrEmpty(searchWord))
            {
                fragmentationVouchers = db.FragmentationVouchers.Where(a => a.IsDeleted == false).OrderByDescending(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.FragmentationVouchers.Where(a => a.IsDeleted == false).Count();
            }
            else
            {
                fragmentationVouchers = db.FragmentationVouchers.Where(a => a.IsDeleted == false &&
                (a.Item.ArName.Contains(searchWord) || a.Item.EnName.Contains(searchWord) || a.Item.Code.Contains(searchWord)))
                    .OrderByDescending(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.FragmentationVouchers.Where(a => a.IsDeleted == false && (a.Item.ArName.Contains(searchWord) || a.Item.EnName.Contains(searchWord) || a.Item.Code.Contains(searchWord))).Count();
            }
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            int roleId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("RoleId").Value);
            var ShowCost = userId == 1 ? true : db.UserPrivileges.Where(u => u.PageAction.PageId == 56 && u.PageAction.EnName == "ShowAvgCostPrice" && u.PageAction.Action == "ShowAvgCostPrice" && u.UserId == userId).Select(x => x.Privileged).FirstOrDefault();
            ShowCost = ShowCost ?? db.RolePrivileges.Where(u => u.PageAction.PageId == 56 && u.PageAction.EnName == "ShowAvgCostPrice" && u.PageAction.Action == "ShowAvgCostPrice" && u.RoleId == roleId).Select(x => x.Privileged).FirstOrDefault();
            if (ShowCost != true)
                ShowCost = false;
            ViewBag.ShowCost = ShowCost;

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;

            return View(fragmentationVouchers.ToList());
        }

        public ActionResult AddEdit(int? id)
        {
            int userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            WarehouseRepository warehouseRepository = new WarehouseRepository(db);
            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", systemSetting.DefaultDepartmentId);
            ViewBag.WarehouseId = new SelectList(warehouseRepository.UserWarehouses(userId, systemSetting.DefaultDepartmentId), "Id", "ArName", systemSetting.DefaultWarehouseId);
            int sysPageId = QueryHelper.SourcePageId("FragmentationVoucher");
            ViewBag.SystemPageId = sysPageId;
            int roleId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("RoleId").Value);
            var ShowCost = userId == 1 ? true : db.UserPrivileges.Where(u => u.PageAction.PageId == 56 && u.PageAction.EnName == "ShowAvgCostPrice" && u.PageAction.Action == "ShowAvgCostPrice" && u.UserId == userId).Select(x => x.Privileged).FirstOrDefault();
            ShowCost = ShowCost ?? db.RolePrivileges.Where(u => u.PageAction.PageId == 56 && u.PageAction.EnName == "ShowAvgCostPrice" && u.PageAction.Action == "ShowAvgCostPrice" && u.RoleId == roleId).Select(x => x.Privileged).FirstOrDefault();
            if (ShowCost != true)
                ShowCost = false;
            ViewBag.ShowCost = ShowCost;
            if (id == null)
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
                ViewBag.VoucherDate = cTime.ToString("yyyy-MM-ddTHH:mm");

                ViewBag.ItemId = new SelectList(db.Items.Where(a => a.IsActive == true && a.IsDeleted == false && (a.ItemTypeId == 2 || a.ItemTypeId == 3)).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                return View();
            }
            FragmentationVoucher fragmentationVoucher = db.FragmentationVouchers.Find(id);

            if (fragmentationVoucher == null)
            {
                return HttpNotFound();
            }

            if (fragmentationVoucher.AutoCreated == true)
            {
                var sysPage = db.SystemPages.Where(a => a.Id == fragmentationVoucher.SystemPageId).Select(x => new { x.ArName, x.TableName }).FirstOrDefault();
                ViewBag.SystemPage = sysPage.ArName;
                var table = sysPage.TableName;
                fragmentationVoucher.SelectedId = int.Parse(db.Database.SqlQuery<string>($"select top(1)([DocumentNumber]) from[{table}] where [Id] = " + fragmentationVoucher.SelectedId).FirstOrDefault());
            }
            // ---------------------------JournalEntry -----------------------------------//
            List<JournalEntry> JournalEntries = new List<JournalEntry>();
            List<StockIssueVoucher> stockIssueVouchers = db.StockIssueVouchers.Where(p => p.IsActive == true && p.IsDeleted == false && p.SystemPageId == sysPageId && p.SelectedId == fragmentationVoucher.Id).OrderBy(p => p.DocumentNumber).ToList();
            List<StockReceiptVoucher> stockReceiptVouchers = db.StockReceiptVouchers.Where(p => p.IsActive == true && p.IsDeleted == false && p.SystemPageId == sysPageId && p.SelectedId == fragmentationVoucher.Id).OrderBy(p => p.DocumentNumber).ToList();
            ViewBag.StockReceiptVouchers = stockReceiptVouchers;
            ViewBag.stockIssueVouchers = stockIssueVouchers;
            foreach (var item in stockIssueVouchers)
            {
                var Entry = db.JournalEntries.Where(p => p.IsActive == true && p.IsDeleted == false && p.SourcePageId == 50 && p.SourceId == item.Id).FirstOrDefault();
                JournalEntries.Add(Entry);
            }
            foreach (var item in stockReceiptVouchers)
            {
                var Entry = db.JournalEntries.Where(p => p.IsActive == true && p.IsDeleted == false && p.SourcePageId == 55 && p.SourceId == item.Id).FirstOrDefault();
                JournalEntries.Add(Entry);
            }
            ViewBag.JournalEntries = JournalEntries.OrderBy(p => p.DocumentNumber).ToList();
            //-----------------------------------------------------------------------------------------------//

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل مستند التجزئة ",
                EnAction = "AddEdit",
                ControllerName = "FragmentationVoucher",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });
            ViewBag.Next = QueryHelper.Next((int)id, "FragmentationVoucher");
            ViewBag.Previous = QueryHelper.Previous((int)id, "FragmentationVoucher");
            ViewBag.Last = QueryHelper.GetLast("FragmentationVoucher");
            ViewBag.First = QueryHelper.GetFirst("FragmentationVoucher");

            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", fragmentationVoucher.DepartmentId);
            ViewBag.WarehouseId = new SelectList(warehouseRepository.UserWarehouses(userId, fragmentationVoucher.DepartmentId), "Id", "ArName", fragmentationVoucher.WarehouseId);
            ViewBag.VoucherDate = fragmentationVoucher.VoucherDate.ToString("yyyy-MM-ddTHH:mm");
            ViewBag.ItemId = new SelectList(db.Items.Where(a => a.IsActive == true && a.IsDeleted == false && (a.ItemTypeId == 2 || a.ItemTypeId == 3)).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", fragmentationVoucher.ItemId);

            return View(fragmentationVoucher);
        }

        [HttpPost]
        public ActionResult AddEdit(FragmentationVoucher fragmentationVoucher)
        {
            int userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            fragmentationVoucher.UserId = userId;
            if (ModelState.IsValid)
            {
                var id = fragmentationVoucher.Id;
                fragmentationVoucher.IsDeleted = false;
                if (fragmentationVoucher.Id > 0)
                {
                    MyXML.xPathName = "Details";
                    var FragmentationVoucherDetails = MyXML.GetXML(fragmentationVoucher.FragmentationVoucherDetails);

                    List<FragmentationVoucherDetail> details = new List<FragmentationVoucherDetail>();
                    var detailObj = new FragmentationVoucherDetail();
                    detailObj.ItemId = (int)fragmentationVoucher.ItemId;
                    detailObj.Qty = fragmentationVoucher.ItemQty;
                    detailObj.ItemUnitId = fragmentationVoucher.ItemUnitId;
                    detailObj.ItemPriceId = db.ItemPrices.Where(a => a.IsActive == true && a.IsDeleted == false && a.ItemId == fragmentationVoucher.ItemId).FirstOrDefault().Id;
                    detailObj.Price = 0;
                    detailObj.UnitEquivalent = 1;
                    detailObj.SystemPageId = db.SystemPages.Where(a => a.TableName == "FragmentationVoucher" && a.IsActive == true && a.IsDeleted == false).FirstOrDefault().Id;
                    detailObj.IsDeleted = false;
                    detailObj.CostPrice = 0;
                    details.Add(detailObj);
                    MyXML.xPathName = "Details";
                    var ObjectAsDetails = MyXML.GetXML(details);


                    db.FragmentationVoucher_Update(fragmentationVoucher.Id, fragmentationVoucher.DocumentNumber, fragmentationVoucher.VoucherDate, fragmentationVoucher.DepartmentId, fragmentationVoucher.WarehouseId, fragmentationVoucher.ItemId, fragmentationVoucher.ItemUnitId, fragmentationVoucher.ItemQty, fragmentationVoucher.UserId, fragmentationVoucher.IsActive, fragmentationVoucher.IsDeleted, fragmentationVoucher.Notes, fragmentationVoucher.Image
                        , fragmentationVoucher.CurrencyId, fragmentationVoucher.CurrencyEquivalent, fragmentationVoucher.SystemPageId, fragmentationVoucher.SelectedId, fragmentationVoucher.IsDelivered, fragmentationVoucher.IsAccepted, fragmentationVoucher.IsLinked, fragmentationVoucher.IsCompleted, fragmentationVoucher.IsPosted, fragmentationVoucher.AutoCreated, fragmentationVoucher.UpdatedId, fragmentationVoucher.IsClosed
                        , FragmentationVoucherDetails, ObjectAsDetails);
                    Notification.GetNotification("FragmentationVoucher", "Edit", "AddEdit", id, null, "مستند التجزئة");
                }
                else
                {
                    fragmentationVoucher.IsActive = true;
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
                    fragmentationVoucher.VoucherDate = cTime;
                    MyXML.xPathName = "Details";
                    var FragmentationVoucherDetails = MyXML.GetXML(fragmentationVoucher.FragmentationVoucherDetails);
                    var idResult = new ObjectParameter("Id", typeof(Int32));

                    List<FragmentationVoucherDetail> details = new List<FragmentationVoucherDetail>();
                    var detailObj = new FragmentationVoucherDetail();
                    detailObj.ItemId = (int)fragmentationVoucher.ItemId;
                    detailObj.Qty = fragmentationVoucher.ItemQty;
                    detailObj.ItemUnitId = fragmentationVoucher.ItemUnitId;
                    detailObj.ItemPriceId = db.ItemPrices.Where(a => a.IsActive == true && a.IsDeleted == false && a.ItemId == fragmentationVoucher.ItemId).FirstOrDefault().Id;
                    detailObj.Price = 0;
                    detailObj.UnitEquivalent = 1;
                    detailObj.SystemPageId = db.SystemPages.Where(a => a.TableName == "FragmentationVoucher" && a.IsActive == true && a.IsDeleted == false).FirstOrDefault().Id;
                    detailObj.IsDeleted = false;
                    detailObj.CostPrice = 0;
                    details.Add(detailObj);
                    MyXML.xPathName = "Details";
                    var ObjectAsDetails = MyXML.GetXML(details);

                    db.FragmentationVoucher_Insert(idResult, fragmentationVoucher.VoucherDate, fragmentationVoucher.DepartmentId, fragmentationVoucher.WarehouseId, fragmentationVoucher.ItemId, fragmentationVoucher.ItemUnitId, fragmentationVoucher.ItemQty, fragmentationVoucher.UserId, fragmentationVoucher.IsActive, fragmentationVoucher.IsDeleted, fragmentationVoucher.Notes, fragmentationVoucher.Image, fragmentationVoucher.CurrencyId, fragmentationVoucher.CurrencyEquivalent, fragmentationVoucher.SystemPageId, fragmentationVoucher.SelectedId, fragmentationVoucher.IsDelivered, fragmentationVoucher.IsAccepted, fragmentationVoucher.IsLinked, fragmentationVoucher.IsCompleted, fragmentationVoucher.IsPosted, fragmentationVoucher.AutoCreated, fragmentationVoucher.UpdatedId, fragmentationVoucher.IsClosed, FragmentationVoucherDetails, ObjectAsDetails);
                    id = (int)idResult.Value;
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("FragmentationVoucher", "Add", "AddEdit", id, null, "مستند التجزئة");
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = fragmentationVoucher.Id > 0 ? "تعديل  مستند التجزئة " : "اضافة  مستند التجزئة",
                    EnAction = "AddEdit",
                    ControllerName = "FragmentationVoucher",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = fragmentationVoucher.Id > 0 ? fragmentationVoucher.Id : db.FragmentationVouchers.Max(i => i.Id),
                    CodeOrDocNo = fragmentationVoucher.DocumentNumber
                });
                return Json(new { success = "true", id });
            }
            var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();
            return Json(new { success = "false", errors });
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            db.FragmentationVoucher_Delete(id, userId);
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "حذف مستند التجزئة",
                EnAction = "AddEdit",
                ControllerName = "FragmentationVoucher",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "POST",
                SelectedItem = id,
            });
            Notification.GetNotification("FragmentationVoucher", "Delete", "Delete", id, null, "مستند التجزئة");
            return Content("true");
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
            var lastObj = db.FragmentationVouchers.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.FragmentationVouchers.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Month == VoucherDate.Value.Month && a.VoucherDate.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.FragmentationVouchers.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

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
            //var docNo = QueryHelper.DocLastNum(id, "FragmentationVoucher");
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