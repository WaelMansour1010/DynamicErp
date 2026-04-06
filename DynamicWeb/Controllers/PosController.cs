using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using MyERP.Models;
namespace MyERP.Controllers
{
    public class PosController : Controller
    {

        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: Pos
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة نقاط البيع",
                EnAction = "Index",
                ControllerName = "Pos",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });

            ////-------------------- Notification-------------------------////
            Notification.GetNotification("Pos", "View", "Index", null, null, "نقاط البيع");
            //////////////-----------------------------------------------------------------------
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            /////////////////////////// Search ////////////////////

            IQueryable<Pos> Pos;
            if (string.IsNullOrEmpty(searchWord))
            {
                Pos = db.Pos.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Pos.Where(s => s.IsDeleted == false).Count();


            }
            else
            {
                Pos = db.Pos.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Employee.ArName.Contains(searchWord) || s.Notes.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Pos.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Employee.ArName.Contains(searchWord) || s.Notes.Contains(searchWord))).Count();
            }

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            ///////////////////////////////////////////////////////////////////////////////
            return View(Pos.ToList());
        }

        public ActionResult AddEdit(int? id)
        {
            var SystemPageId = db.SystemPages.Where(a => a.IsActive == true && a.IsDeleted == false && a.ControllerName == "Pos").FirstOrDefault().Id;

            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            //add
            if (id == null)
            {
                ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
                {
                    b.Id,
                    ArName = b.FixedPart + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.PosManagerId = new SelectList(db.Employees.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.DepartmentId = new SelectList(db.Department_ReportUserDepartments(userId), "Id", "ArName");
                return View();
            }
            //edit and details
            Pos Pos = db.Pos.Find(id);
            if (Pos == null)
            {
                return HttpNotFound();
            }
            ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
            {
                b.Id,
                ArName = b.FixedPart + " - " + b.ArName
            }), "Id", "ArName", Pos.FieldsCodingId);
            ViewBag.PosManagerId = new SelectList(db.Employees.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", Pos.PosManagerId);
            ViewBag.DepartmentId = new SelectList(db.Department_ReportUserDepartments(userId), "Id", "ArName", Pos.DepartmentId);

            ViewBag.Next = QueryHelper.Next((int)id, "Pos");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Pos");
            ViewBag.Last = QueryHelper.GetLast("Pos");
            ViewBag.First = QueryHelper.GetFirst("Pos");

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل نقطة بيع",
                EnAction = "AddEdit",
                ControllerName = "Pos",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = Pos.Id,
                ArItemName = Pos.ArName,
                EnItemName = Pos.EnName,
                CodeOrDocNo = Pos.Code
            });

            return View(Pos);
        }


        [HttpPost]
        public ActionResult AddEdit(Pos Pos, string newBtn)
        {
            var SystemPageId = db.SystemPages.Where(a => a.IsActive == true && a.IsDeleted == false && a.ControllerName == "Pos").FirstOrDefault().Id;

            if (ModelState.IsValid)
            {

                var id = Pos.Id;

                Pos.IsDeleted = false;
                if (Pos.Id > 0)
                {
                    Pos.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                    db.Entry(Pos).State = EntityState.Modified;

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("Pos", "Edit", "AddEdit", id, null, "نقاط البيع");

                    //////////-----------------------------------------------------------------------

                    // Add DB Change
                    QueryHelper.AddDBChange(new DBChange()
                    {
                        TableName = "Pos",
                        SelectedId = Pos.Id,
                        IsMasterChange = true,
                        IsNew = false,
                        IsTransaction = false
                    });
                }
                else
                {
                    Pos.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    Pos.IsActive = true;
                    Pos.PosStatusId = 1;//closed by default
                    //Pos.Code = (QueryHelper.CodeLastNum("Pos") + 1).ToString();
                    Pos.Code = new JavaScriptSerializer().Serialize(SetCodeNum(Pos.FieldsCodingId).Data).ToString().Trim('"');

                    db.Pos.Add(Pos);
                    db.SaveChanges();
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("Pos", "Add", "AddEdit", Pos.Id, null, "نقاط البيع");
                    /////////////-----------------------------------------------------------------------

                    // Add DB Change
                    var SelectedId = db.Pos.OrderByDescending(r => r.Id).FirstOrDefault().Id;
                    QueryHelper.AddDBChange(new DBChange()
                    {
                        TableName = "Pos",
                        SelectedId = SelectedId,
                        IsMasterChange = true,
                        IsNew = true,
                        IsTransaction = false
                    });
                }
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل نقطة بيع" : "اضافة نقطة بيع",
                    EnAction = "AddEdit",
                    ControllerName = "Pos",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = Pos.Id,
                    ArItemName = Pos.ArName,
                    EnItemName = Pos.EnName,
                    CodeOrDocNo = Pos.Code
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


            ViewBag.Next = QueryHelper.Next(Pos.Id, "Pos");
            ViewBag.Previous = QueryHelper.Previous(Pos.Id, "Pos");
            ViewBag.Last = QueryHelper.GetLast("Pos");
            ViewBag.First = QueryHelper.GetFirst("Pos");
            ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
            {
                b.Id,
                ArName = b.FixedPart + " - " + b.ArName
            }), "Id", "ArName", Pos.FieldsCodingId);
            return View(Pos);
        }

        // POST: Pos/Delete/5
        [HttpPost, ActionName("Delete")]

        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                Pos Pos = db.Pos.Find(id);
                Pos.IsDeleted = true;
                Pos.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                Random random = new Random();
                const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
                Pos.Code = Code;
                Pos.FieldsCodingId = null;
                db.Entry(Pos).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف نقطة بيع",
                    EnAction = "AddEdit",
                    ControllerName = "Pos",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = Pos.EnName,
                    ArItemName = Pos.ArName,
                    CodeOrDocNo = Pos.Code
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("Pos", "Delete", "Delete", id, null, "نقاط البيع");

                ////////////////-----------------------------------------------------------------------

                // Add DB Change
                QueryHelper.AddDBChange(new DBChange()
                {
                    TableName = "Pos",
                    SelectedId = Pos.Id,
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

        [HttpPost]
        public ActionResult ActivateDeactivate(int id)
        {
            try
            {
                Pos Pos = db.Pos.Find(id);
                if (Pos.IsActive == true)
                {
                    Pos.IsActive = false;
                }
                else
                {
                    Pos.IsActive = true;
                }
                Pos.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(Pos).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)Pos.IsActive ? "تنشيط نقطة بيع" : "إلغاء نقطة بيع",
                    EnAction = "AddEdit",
                    ControllerName = "Pos",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = Pos.Id,
                    EnItemName = Pos.EnName,
                    ArItemName = Pos.ArName,
                    CodeOrDocNo = Pos.Code
                });
                if (Pos.IsActive == true)
                {
                    Notification.GetNotification("Pos", "Activate/Deactivate", "ActivateDeactivate", id, true, "نقاط البيع");
                }
                else
                {

                    Notification.GetNotification("Pos", "Activate/Deactivate", "ActivateDeactivate", id, false, "نقاط البيع");
                }

                // Add DB Change
                QueryHelper.AddDBChange(new DBChange()
                {
                    TableName = "Pos",
                    SelectedId = Pos.Id,
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
        public JsonResult GetPos(int id)
        {
            var pos = db.Pos.Find(id).PosManagerId;
            return Json(pos, JsonRequestBehavior.AllowGet);
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
                    var code = db.Pos.Where(a => a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
                    var code = db.Pos.Where(a => a.Code.Contains(fixedPart) && a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
                var code = db.Pos.Where(a => a.FieldsCodingId == null && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
            //var code = QueryHelper.CodeLastNum("Pos");
            //return Json(code + 1, JsonRequestBehavior.AllowGet);
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
