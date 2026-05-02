using DevExpress.Data.WcfLinq.Helpers;
using MyERP.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web.Mvc;
namespace MyERP.Controllers
{
    public class ContractsTypeController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: ContractsType
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة أنواع التعاقدات",
                EnAction = "Index",
                ControllerName = "ContractsType",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });

            Notification.GetNotification("ContractsType", "View", "Index", null, null, "أنواع التعاقدات");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }

            IQueryable<ContractsType> contractsTypes;
            if (string.IsNullOrEmpty(searchWord))
            {
                contractsTypes = db.ContractsTypes.Where(a => a.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.ContractsTypes.Where(a => a.IsDeleted == false).Count();
            }
            else
            {
                contractsTypes = db.ContractsTypes.Where(a => a.IsDeleted == false &&
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.ContractsTypes.Where(a => a.IsDeleted == false && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;

            return View(contractsTypes.ToList());
        }

        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {
                var Code = SetCodeNum();
                ViewBag.Code = int.Parse(Code.Data.ToString());
                return View();
            }
            ContractsType contractsType = db.ContractsTypes.Find(id);
            if (contractsType == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل أنواع التعاقدات ",
                EnAction = "AddEdit",
                ControllerName = "ContractsType",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });

            ViewBag.Next = QueryHelper.Next((int)id, "ContractsType");
            ViewBag.Previous = QueryHelper.Previous((int)id, "ContractsType");
            ViewBag.Last = QueryHelper.GetLast("ContractsType");
            ViewBag.First = QueryHelper.GetFirst("ContractsType");
            return View(contractsType);
        }

        [HttpPost]
        public ActionResult AddEdit(ContractsType contracts, string newBtn)
        {
            if (ModelState.IsValid)
            {
                var id = contracts.Id;
                contracts.IsDeleted = false;
                if (contracts.Id > 0)
                {
                    contracts.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    db.Entry(contracts).State = EntityState.Modified;
                    Notification.GetNotification("ContractsType", "Edit", "AddEdit", contracts.Id, null, "أنواع التعاقدات");
                }
                else
                {
                    contracts.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    contracts.Code = (QueryHelper.CodeLastNum("ContractsType") + 1).ToString();
                    contracts.IsActive = true;
                    db.ContractsTypes.Add(contracts);

                    Notification.GetNotification("ContractsType", "Add", "AddEdit", contracts.Id, null, "أنواع التعاقدات");
                }

                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    var mod = ModelState.First(c => c.Key == "Code");  // this
                    mod.Value.Errors.Add("هذا الكود موجود من قبل");
                    return View(contracts);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل نوع تعاقد" : "اضافة نوع تعاقد",
                    EnAction = "AddEdit",
                    ControllerName = "ContractsType",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = contracts.Code
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

            return View(contracts);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                ContractsType contracts = db.ContractsTypes.Find(id);
                contracts.IsDeleted = true;
                contracts.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(contracts).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف نوع تعاقد",
                    EnAction = "AddEdit",
                    ControllerName = "ContractsType",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = contracts.EnName

                });
                Notification.GetNotification("ContractsType", "Delete", "Delete", id, null, "أنواع التعاقدات");


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
                ContractsType contractsType = db.ContractsTypes.Find(id);
                if (contractsType.IsActive == true)
                {
                    contractsType.IsActive = false;
                }
                else
                {
                    contractsType.IsActive = true;
                }
                contractsType.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(contractsType).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)contractsType.IsActive ? "تنشيط أنواع التعاقدات" : "إلغاء تنشيط أنواع التعاقدات",
                    EnAction = "AddEdit",
                    ControllerName = "ContractsType",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = contractsType.Id,
                    EnItemName = contractsType.EnName,
                    ArItemName = contractsType.ArName,
                    CodeOrDocNo = contractsType.Code
                });
                if (contractsType.IsActive == true)
                {
                    Notification.GetNotification("ContractsType", "Activate/Deactivate", "ActivateDeactivate", id, true, "أنواع التعاقدات");
                }
                else
                {

                    Notification.GetNotification("ContractsType", "Activate/Deactivate", "ActivateDeactivate", id, false, "أنواع التعاقدات");
                }

                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }


        [SkipERPAuthorize]
        public JsonResult SetCodeNum()
        {
            var code = QueryHelper.CodeLastNum("ContractsType");
            return Json(code + 1, JsonRequestBehavior.AllowGet);
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