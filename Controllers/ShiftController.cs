using MyERP.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace MyERP.Controllers
{
    public class ShiftController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        // GET: Shift
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "مواعيد الشيفتات",
                EnAction = "Index",
                ControllerName = "Shift",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });

            //--------------------- Notification --------------------//
            Notification.GetNotification("Shift", "View", "Index", null, null, "الشيفتات");

            //////////////////////////////////////////////////////////////////////////////////////
            //-------------------------------paging--------------------------------//
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            //////////////////////////////////////////////////////////////////////////////////////
            //------------------------------- Search ------------------------------------------------//
            IQueryable<Shift> shifts;
            if (string.IsNullOrEmpty(searchWord))
            {
                shifts = db.Shifts.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Shifts.Where(c => c.IsDeleted == false).Count();
            }
            else
            {
                shifts = db.Shifts.Where(s => s.IsDeleted == false && (s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.Code.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Shifts.Where(s => s.IsDeleted == false && (s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.Code.Contains(searchWord))).OrderBy(s => s.Id).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(shifts.ToList());
        }

        //////////////////////////////////////////////////////////////////////////////////////
        //--------------------------- Authorization --------------------------------//
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
                    var code = db.Shifts.Where(a => a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
                    var code = db.Shifts.Where(a => a.Code.Contains(fixedPart) && a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
                var code = db.Shifts.Where(a => a.FieldsCodingId == null && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
            //var code = QueryHelper.CodeLastNum("Shift");
            //return Json(code + 1, JsonRequestBehavior.AllowGet);
        }

        //////////////////////////////////////////////////////////////////////////////////////
        //--------------------------------- Add Or Edit -----------------------------------//
        public ActionResult AddEdit(int? id)
        {
            var SystemPageId = db.SystemPages.Where(a => a.IsActive == true && a.IsDeleted == false && a.ControllerName == "Shift").FirstOrDefault().Id;

            ViewBag.EmployeeId = new SelectList(db.Employees.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");
            if (id == null)
            {
                ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
                {
                    b.Id,
                    ArName = b.FixedPart + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                List<Shift_Detail> Detail = new List<Shift_Detail>()
                    {
                         new Shift_Detail { EnDay="Saturday" , ArDay="السبت"  },
                         new Shift_Detail { EnDay="Sunday" , ArDay="الأحد"   },
                         new Shift_Detail { EnDay="Monday" ,   ArDay="الإثنين" },
                         new Shift_Detail { EnDay="Tuesday" ,ArDay="الثلاثاء"   },
                         new Shift_Detail { EnDay="Wednesday" , ArDay="الأربعاء"  },
                         new Shift_Detail { EnDay="Thursday" ,ArDay="الخميس"  },
                         new Shift_Detail { EnDay="Friday" , ArDay="الجمعة"  }
                    };
                ViewBag.ShiftDetail = Detail;
                ViewBag.OfficialVacation = db.OfficialVacations.Where(a => a.IsActive == true && a.IsDeleted == false).ToList();
                return View();
            }
            Shift shift = db.Shifts.Find(id);

            if (shift == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل المواعيد ",
                EnAction = "AddEdit",
                ControllerName = "Shift",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });
            ViewBag.DepartmentId = new SelectList(db.Departments.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", shift.DepartmentId);
            ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
            {
                b.Id,
                ArName = b.FixedPart + " - " + b.ArName
            }), "Id", "ArName",shift.FieldsCodingId);
            ViewBag.Next = QueryHelper.Next((int)id, "Shift");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Shift");
            ViewBag.Last = QueryHelper.GetLast("Shift");
            ViewBag.First = QueryHelper.GetFirst("Shift");
            ViewBag.ShiftOfficialVacations = db.ShiftOfficialVacations.Where(a => a.ShiftId == id).ToList();
            return View(shift);
        }

        [HttpPost]
        public ActionResult AddEdit(Shift shift)
        {
            var SystemPageId = db.SystemPages.Where(a => a.IsActive == true && a.IsDeleted == false && a.ControllerName == "Shift").FirstOrDefault().Id;

            if (ModelState.IsValid)
            {
                //--------------------------- Edit if there is id ------------------------//
                var id = shift.Id;
                shift.IsDeleted = false;
                ViewBag.ShiftOfficialVacations = db.ShiftOfficialVacations.Where(a => a.ShiftId==id).ToList();
                foreach (var shiftOfficialVacation in shift.ShiftOfficialVacations)
                {
                    var old = db.ShiftOfficialVacations.Where(a => a.OfficialVacationId == shiftOfficialVacation.OfficialVacationId && a.ShiftId == shiftOfficialVacation.ShiftId).FirstOrDefault();
                    if (old != null)
                    {
                        old.DateFrom = shiftOfficialVacation.DateFrom;
                        old.DateTo = shiftOfficialVacation.DateTo;

                        // Add DB Change
                        QueryHelper.AddDBChange(new DBChange()
                        {
                            TableName = "ShiftOfficialVacation",
                            SelectedId = old.Id,
                            IsMasterChange = true,
                            IsNew = false,
                            IsTransaction = false
                        });
                    }
                    else
                    {
                        db.ShiftOfficialVacations.Add(shiftOfficialVacation);
                        db.SaveChanges();

                        // Add DB Change
                        var SelectedId = db.ShiftOfficialVacations.OrderByDescending(r => r.Id).FirstOrDefault().Id;
                        QueryHelper.AddDBChange(new DBChange()
                        {
                            TableName = "ShiftOfficialVacation",
                            SelectedId = SelectedId,
                            IsMasterChange = true,
                            IsNew = true,
                            IsTransaction = false
                        });
                    }
                }
                if (shift.Id > 0)
                {
                    int UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    MyXML.xPathName = "ShiftDetails";
                    var ShiftDetailsXML = MyXML.GetXML(shift.ShiftDetails);
                    MyXML.xPathName = "ShiftEmployees";
                    var ShiftEmployeesXML = MyXML.GetXML(shift.ShiftEmployees);
                    // use another object to prevent entity error
                    var old = db.Shifts.Find(id);
                    db.ShiftDetails.RemoveRange(db.ShiftDetails.Where(p => p.ShiftId == old.Id).ToList());
                    db.ShiftEmployees.RemoveRange(db.ShiftEmployees.Where(p => p.ShiftId == old.Id).ToList());
                    old.ArName = shift.ArName;
                    old.EnName = shift.EnName;
                    old.DepartmentId = shift.DepartmentId;
                    foreach (var item in shift.ShiftDetails)
                    {
                        old.ShiftDetails.Add(item);
                    }
                    foreach (var item in shift.ShiftEmployees)
                    {
                        old.ShiftEmployees.Add(item);
                    }
                    db.Entry(old).State = EntityState.Modified;
                    db.SaveChanges();
                    ////-------------------- Notification for any edit -------------------------////
                    Notification.GetNotification("Shift", "Edit", "AddEdit", id, null, "الشيفتات");

                    // Add DB Change
                    QueryHelper.AddDBChange(new DBChange()
                    {
                        TableName = "Shift",
                        SelectedId = old.Id,
                        IsMasterChange = true,
                        IsNew = false,
                        IsTransaction = false
                    });
                }
                else
                {
                    shift.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    //shift.Code = (QueryHelper.CodeLastNum("Shift") + 1).ToString();
                    shift.Code = new JavaScriptSerializer().Serialize(SetCodeNum(shift.FieldsCodingId).Data).ToString().Trim('"');
                    shift.IsActive = true;
                    shift.IsDeleted = false;
                    db.Shifts.Add(shift);
                    ////-------------------- Notification for adding new shift -------------------------////
                    Notification.GetNotification("Shift", "Add", "AddEdit", shift.Id, null, "الشيفتات");
                }
                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    var mod = ModelState.First(c => c.Key == "Code");  // this
                    mod.Value.Errors.Add("هذا الكود موجود من قبل");
                    return View(shift);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل شيفت" : "اضافة شيفت",
                    EnAction = "AddEdit",
                    ControllerName = "Shift",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = shift.Code
                });
                return Json(new { success = "true" });
            }
            ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
            {
                b.Id,
                ArName = b.FixedPart + " - " + b.ArName
            }), "Id", "ArName", shift.FieldsCodingId);
            return View(shift);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                Shift shift = db.Shifts.Find(id);
                shift.IsDeleted = true;
                shift.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                Random random = new Random();
                const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
                shift.Code = Code;
                shift.FieldsCodingId = null;
                foreach (var detail in shift.ShiftDetails)
                {
                    detail.IsDeleted = true;
                }

                db.Entry(shift).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف الشيفت",
                    EnAction = "AddEdit",
                    ControllerName = "Shift",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("Shift", "Delete", "Delete", id, null, "الشيفتات");

                // Add DB Change
                QueryHelper.AddDBChange(new DBChange()
                {
                    TableName = "Shift",
                    SelectedId = shift.Id,
                    IsMasterChange = true,
                    IsNew = false,
                    IsTransaction = false
                });

                return Content("true");
            }
            catch (Exception)
            {

                throw;
            }
        }

        [HttpPost]
        public ActionResult ActivateDeactivate(int id)
        {
            try
            {
                Shift shift = db.Shifts.Find(id);
                if (shift.IsActive == true)
                {
                    shift.IsActive = false;
                }
                else
                {
                    shift.IsActive = true;
                }
                shift.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(shift).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)shift.IsActive ? "تعديل شيفت" : "اضافة شيفت",
                    EnAction = "AddEdit",
                    ControllerName = "Shift",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = shift.Id,
                });
                ////-------------------- Notification-------------------------////
                if (shift.IsActive == true)
                {

                    Notification.GetNotification("Shift", "Activate/Deactivate", "ActivateDeactivate", id, true, "الشيفتات");
                }
                else
                {

                    Notification.GetNotification("Shift", "Activate/Deactivate", "ActivateDeactivate", id, false, "الشيفتات");
                }

                // Add DB Change
                QueryHelper.AddDBChange(new DBChange()
                {
                    TableName = "Shift",
                    SelectedId = shift.Id,
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
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
    public class Shift_Detail
    {
        public string EnDay;
        public string ArDay;
    }
}