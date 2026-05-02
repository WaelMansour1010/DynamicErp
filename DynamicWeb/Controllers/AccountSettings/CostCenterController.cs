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
using System.Web.Script.Serialization;

namespace MyERP.Controllers.AccountSettings
{
    public class CostCenterController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: CostCenter
        public async Task<ActionResult> Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            IQueryable<CostCenter> costCenters;

            if (string.IsNullOrEmpty(searchWord))
            {
                costCenters = db.CostCenters.Where(x => !x.IsDeleted).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = await db.CostCenters.Where(x => !x.IsDeleted).CountAsync();
            }
            else
            {
                costCenters = db.CostCenters.Where(x => !x.IsDeleted && (x.Code.Contains(searchWord) || x.ArName.Contains(searchWord) || x.EnName.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = await db.CostCenters.Where(x => !x.IsDeleted && (x.Code.Contains(searchWord) || x.ArName.Contains(searchWord) || x.EnName.Contains(searchWord))).CountAsync();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة مراكز التكلفة",
                EnAction = "Index",
                ControllerName = "CostCenter",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("CostCenter", "View", "Index", null, null, "مراكز التكلفة");
            return View(await costCenters.ToListAsync());
        }

        public async Task<ActionResult> AddEdit(int? id)
        {
            var SystemPageId = db.SystemPages.Where(a => a.IsActive == true && a.IsDeleted == false && a.ControllerName == "CostCenter").FirstOrDefault().Id;

            if (id == null)
            {
                ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
                {
                    b.Id,
                    ArName = b.FixedPart + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.TypeId = new SelectList(new List<dynamic> { new { Id=0, ArName="رئيسي"},
            new { Id=1, ArName="وسيط"},
            new { Id=2, ArName="فرعي"}}, "Id", "ArName");
                return View();
            }
            CostCenter costCenter = await db.CostCenters.FindAsync(id);
            if (costCenter == null)
            {
                return HttpNotFound();
            }
            
            ViewBag.TypeId = new SelectList(new List<dynamic> { new { Id=0, ArName="رئيسي"},
            new { Id=1, ArName="وسيط"},
            new { Id=2, ArName="فرعي"}}, "Id", "ArName", costCenter.TypeId);
            ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
            {
                b.Id,
                ArName = b.FixedPart + " - " + b.ArName
            }), "Id", "ArName",costCenter.FieldsCodingId);
            return View(costCenter);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddEdit(CostCenter costCenter, string newBtn)
        {
            var SystemPageId = db.SystemPages.Where(a => a.IsActive == true && a.IsDeleted == false && a.ControllerName == "CostCenter").FirstOrDefault().Id;

            if (ModelState.IsValid)
            {
                var id = costCenter.Id;
                costCenter.IsDeleted = false;
                if (costCenter.Id > 0)
                {
                    db.Entry(costCenter).State = EntityState.Modified;
                }
                else
                {
                    costCenter.IsActive = true;
                    costCenter.Code = new JavaScriptSerializer().Serialize(SetCodeNum(costCenter.FieldsCodingId).Data).ToString().Trim('"');
                    db.CostCenters.Add(costCenter);
                }
                await db.SaveChangesAsync();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل مراكز التكلفة" : "اضافة مراكز التكلفة",
                    EnAction = "AddEdit",
                    ControllerName = "CostCenter",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = costCenter.Id,
                    ArItemName = costCenter.ArName,
                    EnItemName = costCenter.EnName,
                    CodeOrDocNo = costCenter.Code
                });
                Notification.GetNotification("CostCenter",id>0? "Add":"Edit", "AddEdit", costCenter.Id, null, "مراكز التكلفة");
                if (newBtn == "saveAndNew")
                    return RedirectToAction("AddEdit");
                else
                    return RedirectToAction("Index");
            }
            ViewBag.TypeId = new SelectList(new List<dynamic> { new { Id=0, ArName="رئيسي"},
            new { Id=1, ArName="وسيط"},
            new { Id=2, ArName="فرعي"}}, "Id", "ArName", costCenter.TypeId);
            ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
            {
                b.Id,
                ArName = b.FixedPart + " - " + b.ArName
            }), "Id", "ArName", costCenter.FieldsCodingId);
            return View(costCenter);
        }

        // POST: CostCenter/Delete/5
        [HttpPost, ActionName("Delete")]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            CostCenter costCenter = await db.CostCenters.FindAsync(id);
            costCenter.IsDeleted = true;
            costCenter.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            Random random = new Random();
            const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
            costCenter.Code = Code;
            costCenter.FieldsCodingId = null;
            db.Entry(costCenter).State = EntityState.Modified;
            await db.SaveChangesAsync();
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "حذف مراكز التكلفة",
                EnAction = "AddEdit",
                ControllerName = "CostCenter",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "POST",
                SelectedItem = id,
                EnItemName = costCenter.EnName,
                ArItemName = costCenter.ArName,
                CodeOrDocNo = costCenter.Code
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("CostCenter", "Delete", "Delete", id, null, "مراكز التكلفة");
            return Content("true");
        }

        [HttpPost]
        public ActionResult ActivateDeactivate(int id)
        {
            CostCenter costCenter = db.CostCenters.Find(id);
            if (costCenter.IsActive == true)
            {
                costCenter.IsActive = false;
            }
            else
            {
                costCenter.IsActive = true;
            }
            costCenter.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            db.Entry(costCenter).State = EntityState.Modified;

            db.SaveChanges();
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = costCenter.IsActive ? "تنشيط مراكز التكلفة" : "إلغاء مراكز التكلفة",
                EnAction = "AddEdit",
                ControllerName = "CostCenter",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "POST",
                SelectedItem = costCenter.Id,
                EnItemName = costCenter.EnName,
                ArItemName = costCenter.ArName,
                CodeOrDocNo = costCenter.Code
            });
            if (costCenter.IsActive == true)
            {
                Notification.GetNotification("CostCenter", "Activate/Deactivate", "ActivateDeactivate", id, true, "مراكز التكلفة");
            }
            else
            {

                Notification.GetNotification("CostCenter", "Activate/Deactivate", "ActivateDeactivate", id, false, "مراكز التكلفة");
            }

            return Content("true");
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
                    var code = db.CostCenters.Where(a => a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
                    var code = db.CostCenters.Where(a => a.Code.Contains(fixedPart) && a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
                var code = db.CostCenters.Where(a => a.FieldsCodingId == null && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
            //var code = QueryHelper.CodeLastNum("CostCenter");
            //return Json(code + 1, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public async Task<JsonResult> CostCentersByTypeId(int id)
        {
            if (id == 2)
                return Json(await db.CostCenters.Where(x => x.IsActive && !x.IsDeleted && x.TypeId == 1).Select(x => new { x.Id, ArName = x.Code + " - " + x.ArName }).ToListAsync(), JsonRequestBehavior.AllowGet);
            else if (id == 1)
                return Json(await db.CostCenters.Where(x => x.IsActive && !x.IsDeleted && (x.TypeId == 1 || x.TypeId == 0)).Select(x => new { x.Id, ArName = x.Code + " - " + x.ArName }).ToListAsync(), JsonRequestBehavior.AllowGet);
            else
                return Json("", JsonRequestBehavior.AllowGet);
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
