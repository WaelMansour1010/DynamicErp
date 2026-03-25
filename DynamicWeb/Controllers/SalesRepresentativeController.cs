using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using MyERP.Models;
using MyERP.Repository;


namespace MyERP.Controllers
{
    public class SalesRepresentativeController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        // GET: SalesRepresentative
        public ActionResult Index(int? RepresentativeGroupId, int? DepartmentRepId, int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            var depIds = departmentRepository.UserDepartmentsIds(userId);
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "المناديب",
                EnAction = "Index",
                ControllerName = "SalesRepresentative",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            Notification.GetNotification("SalesRepresentative", "View", "Index", null, null, "المناديب");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }
            IQueryable<Employee> SalesRepresentatives;
            if (string.IsNullOrEmpty(searchWord))
            {
                SalesRepresentatives = db.Employees.Where(a => a.IsDeleted == false && a.IsSalesRepresentative == true
                && (RepresentativeGroupId == null || a.RepresentativeGroupId == RepresentativeGroupId) && (DepartmentRepId == null || a.DepartmentRepId == DepartmentRepId)
                ).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Employees.Where(a => a.IsDeleted == false && a.IsSalesRepresentative == true
                && (RepresentativeGroupId == null || a.RepresentativeGroupId == RepresentativeGroupId) && (DepartmentRepId == null || a.DepartmentRepId == DepartmentRepId)).Count();
            }
            else
            {
                SalesRepresentatives = db.Employees.Where(a => a.IsDeleted == false && a.IsSalesRepresentative == true
                && (RepresentativeGroupId == null || a.RepresentativeGroupId == RepresentativeGroupId) && (DepartmentRepId == null || a.DepartmentRepId == DepartmentRepId)
                && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))
                ).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Employees.Where(a => a.IsDeleted == false && a.IsSalesRepresentative == true
                && (RepresentativeGroupId == null || a.RepresentativeGroupId == RepresentativeGroupId) && (DepartmentRepId == null || a.DepartmentRepId == DepartmentRepId)
                && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)
                )).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            ViewBag.RepresentativeGroupId = new SelectList(db.SalesRepresentativesGroups.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", RepresentativeGroupId);
            if (userId == 1)
            {
                ViewBag.DepartmentRepId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", DepartmentRepId);
            }
            else
            {
                ViewBag.DepartmentRepId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", DepartmentRepId);
            }
            return View(SalesRepresentatives.ToList());
        }

        public async Task<ActionResult> AddEdit(int? id)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            var depIds = await departmentRepository.UserDepartmentsIds(userId);
            if (id == null)
            {
                ViewBag.RepresentativeGroupId = new SelectList(db.SalesRepresentativesGroups.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.RepId = new SelectList(db.Employees.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                if (userId == 1)
                {
                    ViewBag.DepartmentRepId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                    {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName");
                }
                else
                {
                    ViewBag.DepartmentRepId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                    {
                        Id = b.DepartmentId,
                        ArName = b.Department.Code + " - " + b.Department.ArName
                    }), "Id", "ArName");
                }
                return View();
            }
            Employee SalesRepresentative = await db.Employees.FindAsync(id);
            if (SalesRepresentative == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل المناديب ",
                EnAction = "AddEdit",
                ControllerName = "SalesRepresentative",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });
            ViewBag.Next = QueryHelper.Next((int)id, "Employee");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Employee");
            ViewBag.Last = QueryHelper.GetLast("Employee");
            ViewBag.First = QueryHelper.GetFirst("Employee");

            ViewBag.RepresentativeGroupId = new SelectList(db.SalesRepresentativesGroups.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", SalesRepresentative.RepresentativeGroupId);
            ViewBag.RepId = new SelectList(db.Employees.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", SalesRepresentative.Id);
            if (userId == 1)
            {
                ViewBag.DepartmentRepId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", SalesRepresentative.DepartmentRepId);
            }
            else
            {
                ViewBag.DepartmentRepId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", SalesRepresentative.DepartmentRepId);
            }

            return View( SalesRepresentative);
        }
        [HttpPost]
        public async Task<ActionResult> AddEdit(/*Employee SalesRepresentative*/int?RepId,int? DepartmentRepId,int? RepresentativeGroupId,double? DiscountPercentage,bool? IsSalesRepresentative, int? Id )
        {

            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            var depIds = await departmentRepository.UserDepartmentsIds(userId);
            var id = Id;
            Employee OldSalesRepresentative =await db.Employees.FindAsync(id);
            bool IsExist = false;
            //if (ModelState.IsValid)
            //{
           // var PrevEmployeeLinkedWithRep0 = await db.Employees.AnyAsync(a => a.RepId == id);
           
            if (id > 0)
                {
                var PrevEmployeeLinkedWithRep = await db.Employees.Where(a => a.RepId == RepId && a.Id != id && a.IsDeleted == false && a.IsActive == true).FirstOrDefaultAsync();
                IsExist = PrevEmployeeLinkedWithRep != null ? true : false;
                              }
                else
                {
                var PrevEmployeeLinkedWithRep = await db.Employees.Where(a => a.RepId == RepId && a.IsDeleted == false && a.IsActive == true).FirstOrDefaultAsync();
                IsExist = PrevEmployeeLinkedWithRep != null ? true : false;
            }
            if (IsExist == true)
            {
                return Json(new { success = "Exist Before" }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                OldSalesRepresentative.RepId = id;
                OldSalesRepresentative.DepartmentRepId = DepartmentRepId;
                OldSalesRepresentative.RepresentativeGroupId = RepresentativeGroupId;
                OldSalesRepresentative.IsSalesRepresentative = true;
                OldSalesRepresentative.DiscountPercentage = DiscountPercentage;
                db.Entry(OldSalesRepresentative).State = EntityState.Modified;
                Notification.GetNotification("SalesRepresentative", "Edit", "AddEdit", id, null, "المناديب");
            }
            try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    var errorsEx =ex.Message;
                    return Json(new { success = "false", errorsEx  });

                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل المناديب" : "اضافة المناديب",
                    EnAction = "AddEdit",
                    ControllerName = "SalesRepresentative",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = OldSalesRepresentative.Code
                });
                return Json(new { success = "true" });
            //}

            var errors = ModelState
                              .Where(x => x.Value.Errors.Count > 0)
                              .Select(x => new { x.Key, x.Value.Errors })
                              .ToArray();
            return Json(new { success = "false", errors });
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