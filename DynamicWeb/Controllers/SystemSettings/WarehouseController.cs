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
using System.Web.Script.Serialization;

namespace MyERP.Controllers.SystemSettings
{
    public class WarehouseController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: Warehouse
        public ActionResult Index(bool? IsSearch,int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "", int departmentId = 0)
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة المخازن",
                EnAction = "Index",
                ControllerName = "Warehouse",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("Warehouse", "View", "Index", null, null, "المخازن");

            ///////////-----------------------------------------------------------------------

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            /////////////////////////// Search ////////////////////

            IQueryable<Warehouse> warehouses;
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            var DeptId = 0;
            if (userId == 1)
            {
                var depts = db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).ToList();
                if (depts.Count == 1)
                {
                    DeptId = depts.FirstOrDefault().Id;
                    ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                    {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", DeptId);
                    if(IsSearch != true)
                    {
                        departmentId = DeptId;
                    }
                }
                else
                {
                    ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                    {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", departmentId);
                }                
            }
            else
            {
                var depts = db.UserDepartments.Where(d => d.UserId == userId).ToList();
                if (depts.Count == 1)
                {
                    DeptId = depts.FirstOrDefault().Id;
                    ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                    {
                        Id = b.DepartmentId,
                        ArName = b.Department.Code + " - " + b.Department.ArName
                    }), "Id", "ArName", DeptId);
                    if (IsSearch != true)
                    {
                        departmentId = DeptId;
                    }
                }
                else
                {
                    ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                    {
                        Id = b.DepartmentId,
                        ArName = b.Department.Code + " - " + b.Department.ArName
                    }), "Id", "ArName", departmentId);
                }
            }
            
            if (string.IsNullOrEmpty(searchWord))
            {
                if (userId == 1)
                {
                    warehouses = db.Warehouses.Where(s => s.IsDeleted == false && (departmentId == 0||s.DepartmentId==departmentId)).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.Warehouses.Where(c => c.IsDeleted == false && (departmentId == 0 || c.DepartmentId == departmentId)).Count();
                }
                else
                {
                    warehouses = db.Warehouses.Where(s => s.IsDeleted == false && (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (departmentId == 0|| s.DepartmentId == departmentId)).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.Warehouses.Where(c => c.IsDeleted == false && (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(c.DepartmentId)) && ( departmentId == 0 || c.DepartmentId == departmentId)).Count();
                }
            }
            else
            {
                if (userId == 1)
                {
                    warehouses = db.Warehouses.Where(s => s.IsDeleted == false && (departmentId == 0 || s.DepartmentId == departmentId) && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.ToString().Contains(searchWord) || s.Employee.ArName.Contains(searchWord) || s.Phone.Contains(searchWord) || s.Location.Contains(searchWord) || s.Notes.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.Warehouses.Where(s => s.IsDeleted == false && (departmentId == 0|| s.DepartmentId == departmentId) && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.ToString().Contains(searchWord) || s.Employee.ArName.Contains(searchWord) || s.Phone.Contains(searchWord) || s.Location.Contains(searchWord) || s.Notes.Contains(searchWord))).Count();
                }
                else
                {
                    warehouses = db.Warehouses.Where(s => s.IsDeleted == false && (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (departmentId == 0  || s.DepartmentId == departmentId) && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.ToString().Contains(searchWord) || s.Employee.ArName.Contains(searchWord) || s.Phone.Contains(searchWord) || s.Location.Contains(searchWord) || s.Notes.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.Warehouses.Where(s => s.IsDeleted == false && (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && ( departmentId == 0 || s.DepartmentId == departmentId) && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.ToString().Contains(searchWord) || s.Employee.ArName.Contains(searchWord) || s.Phone.Contains(searchWord) || s.Location.Contains(searchWord) || s.Notes.Contains(searchWord))).Count();
                }
                ///////////////////////////////////////////////////////////////////////////////
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(warehouses.ToList());

        }

        // GET: Warehouse/Edit/5
        public ActionResult AddEdit(int? id)
        {
            var SystemPageId = db.SystemPages.Where(a => a.IsActive == true && a.IsDeleted == false && a.ControllerName == "Warehouse").FirstOrDefault().Id;

            int userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            var systemsetting = db.SystemSettings.FirstOrDefault();
            ViewBag.UseSeparatedAccountsForWarehouse = systemsetting.UseSeparatedAccountsForWarehouse;
            var subAccounts = db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && (c.ClassificationId == 3)).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }).ToList();

            if (id == null)
            {
                ViewBag.ResponsibleEmpId = new SelectList(db.Employees.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                if (userId == 1)
                {
                    ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                    {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName");
                }
                else
                {
                    ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                    {
                        Id = b.DepartmentId,
                        ArName = b.Department.Code + " - " + b.Department.ArName
                    }), "Id", "ArName");
                }
                ViewBag.StockAccountId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.SalesCostAccountId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.InventoryVarianceAccountId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.LostAndDamagedAccountId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.GiftsAndSamplesAccountId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
                {
                    b.Id,
                    ArName = b.FixedPart + " - " + b.ArName
                }), "Id", "ArName");

                return View();
            }
            Warehouse warehouse = db.Warehouses.Find(id);
            if (warehouse == null)
            {
                return HttpNotFound();
            }
            ViewBag.StockAccountId = new SelectList(subAccounts, "Id", "ArName", warehouse.StockAccountId);
            ViewBag.SalesCostAccountId = new SelectList(subAccounts, "Id", "ArName", warehouse.SalesCostAccountId);
            ViewBag.InventoryVarianceAccountId = new SelectList(subAccounts, "Id", "ArName", warehouse.InventoryVarianceAccountId);
            ViewBag.LostAndDamagedAccountId = new SelectList(subAccounts, "Id", "ArName", warehouse.LostAndDamagedAccountId);
            ViewBag.GiftsAndSamplesAccountId = new SelectList(subAccounts, "Id", "ArName", warehouse.GiftsAndSamplesAccountId);

            ViewBag.ResponsibleEmpId = new SelectList(db.Employees.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", warehouse.ResponsibleEmpId);
            if (userId == 1)
            {
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", warehouse.DepartmentId);
            }
            else
            {
                ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", warehouse.DepartmentId);
            }
            ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
            {
                b.Id,
                ArName = b.FixedPart + " - " + b.ArName
            }), "Id", "ArName", warehouse.FieldsCodingId);

            ViewBag.Next = QueryHelper.Next((int)id, "Warehouse");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Warehouse");
            ViewBag.Last = QueryHelper.GetLast("Warehouse");
            ViewBag.First = QueryHelper.GetFirst("Warehouse");

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل المخازن",
                EnAction = "AddEdit",
                ControllerName = "Warehouse",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = warehouse.Id,
                ArItemName = warehouse.ArName,
                EnItemName = warehouse.EnName,
                CodeOrDocNo = warehouse.Code
            });
            return View(warehouse);
        }

        // POST: Warehouse/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult AddEdit(Warehouse warehouse, string newBtn)
        {
            int userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            if (ModelState.IsValid)
            {
                var id = warehouse.Id;
                warehouse.IsDeleted = false;
                if (warehouse.Id > 0)
                {
                    warehouse.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                    db.Entry(warehouse).State = EntityState.Modified;

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("Warehouse", "Edit", "AddEdit", id, null, "المخازن");

                    ///////////////-----------------------------------------------------------------------

                    // Add DB Change
                    QueryHelper.AddDBChange(new DBChange()
                    {
                        TableName = "Warehouse",
                        SelectedId = warehouse.Id,
                        IsMasterChange = true,
                        IsNew = false,
                        IsTransaction = false
                    });
                }
                else
                {
                    Notification.GetNotification("Warehouse", "Add", "AddEdit", warehouse.Id, null, "المخازن");

                    warehouse.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    //warehouse.Code= (QueryHelper.CodeLastNum("Warehouse") + 1).ToString();
                    warehouse.Code = new JavaScriptSerializer().Serialize(SetCodeNum(warehouse.FieldsCodingId).Data).ToString().Trim('"');

                    warehouse.IsActive = true;
                    db.Warehouses.Add(warehouse);
                    db.SaveChanges();
                    /////////////-----------------------------------------------------------------------

                    // Add DB Change
                    var SelectedId = db.Warehouses.OrderByDescending(r => r.Id).FirstOrDefault().Id;
                    QueryHelper.AddDBChange(new DBChange()
                    {
                        TableName = "Warehouse",
                        SelectedId = SelectedId,
                        IsMasterChange = true,
                        IsNew = true,
                        IsTransaction = false
                    });
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل المخازن" : "اضافة المخازن",
                    EnAction = "AddEdit",
                    ControllerName = "Warehouse",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = warehouse.Id,
                    ArItemName = warehouse.ArName,
                    EnItemName = warehouse.EnName,
                    CodeOrDocNo = warehouse.Code
                });
                db.SaveChanges();
                if (newBtn == "saveAndNew")
                {
                    return RedirectToAction("AddEdit");

                }
                else
                {
                    return RedirectToAction("Index");
                }
            }
            var subAccounts = db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && (c.ClassificationId == 3)).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }).ToList();
            ViewBag.StockAccountId = new SelectList(subAccounts, "Id", "ArName", warehouse.StockAccountId);
            ViewBag.SalesCostAccountId = new SelectList(subAccounts, "Id", "ArName", warehouse.SalesCostAccountId);
            ViewBag.InventoryVarianceAccountId = new SelectList(subAccounts, "Id", "ArName", warehouse.InventoryVarianceAccountId);
            ViewBag.LostAndDamagedAccountId = new SelectList(subAccounts, "Id", "ArName", warehouse.LostAndDamagedAccountId);
            ViewBag.GiftsAndSamplesAccountId = new SelectList(subAccounts, "Id", "ArName", warehouse.GiftsAndSamplesAccountId);

            ViewBag.ResponsibleEmpId = new SelectList(db.Employees.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", warehouse.ResponsibleEmpId);
            if (userId == 1)
            {
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", warehouse.DepartmentId);
            }
            else
            {
                ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", warehouse.DepartmentId);
            }

            return View(warehouse);
        }

        // POST: Warehouse/Delete/5
        [HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                Warehouse warehouse = db.Warehouses.Find(id);
                warehouse.IsDeleted = true;
                warehouse.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                Random random = new Random();
                const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
                warehouse.Code = Code;
                warehouse.FieldsCodingId = null;
                db.Entry(warehouse).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف المخازن",
                    EnAction = "AddEdit",
                    ControllerName = "Warehouse",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = warehouse.EnName,
                    ArItemName = warehouse.ArName,
                    CodeOrDocNo = warehouse.Code
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("Warehouse", "Delete", "Delete", id, null, "المخازن");

                ////////////-----------------------------------------------------------------------

                // Add DB Change
                QueryHelper.AddDBChange(new DBChange()
                {
                    TableName = "Warehouse",
                    SelectedId = id,
                    IsMasterChange = true,
                    IsNew = false,
                    IsTransaction = false
                });

                return Content("true");

            }
            catch (Exception)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

        }

        [HttpPost]
        public ActionResult ActivateDeactivate(int id)
        {
            try
            {
                Warehouse warehouse = db.Warehouses.Find(id);
                if (warehouse.IsActive == true)
                {
                    warehouse.IsActive = false;
                }
                else
                {
                    warehouse.IsActive = true;
                }
                warehouse.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(warehouse).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)warehouse.IsActive ? "تنشيط المخازن" : "إلغاء المخازن",
                    EnAction = "AddEdit",
                    ControllerName = "Warehouse",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = warehouse.Id,
                    EnItemName = warehouse.EnName,
                    ArItemName = warehouse.ArName,
                    CodeOrDocNo = warehouse.Code
                });
                ////-------------------- Notification-------------------------////
                if (warehouse.IsActive == true)
                {
                    Notification.GetNotification("Warehouse", "Activate/Deactivate", "ActivateDeactivate", id, true, "المخازن");
                }
                else
                {

                    Notification.GetNotification("Warehouse", "Activate/Deactivate", "ActivateDeactivate", id, false, "المخازن");
                }
                ///////////////-----------------------------------------------------------------------

                // Add DB Change
                QueryHelper.AddDBChange(new DBChange()
                {
                    TableName = "Warehouse",
                    SelectedId = id,
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
            //var code = QueryHelper.CodeLastNum("Warehouse");
            //return Json(code + 1, JsonRequestBehavior.AllowGet);
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
                    var code = db.Warehouses.Where(a => a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
                    var code = db.Warehouses.Where(a => a.Code.Contains(fixedPart) && a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
                var code = db.Warehouses.Where(a => a.FieldsCodingId == null && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
