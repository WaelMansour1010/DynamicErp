using System.Data;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using DevExpress.DataProcessing;
using DevExpress.Office.NumberConverters;
using DevExpress.PivotGrid.OLAP;
using DevExpress.XtraPrinting;
using DevExpress.XtraPrinting.Native;
using DevExpress.XtraReports.Parameters;
using DevExpress.XtraRichEdit.Layout;
using EazyCash.Auth;
using EazyCash.Data;
using EazyCash.Models;
using HROnlineModel;
using HRServices.Models;
using Library.DRpts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
//using Microsoft.AspNet.Identity;
using IdentityResult = Microsoft.AspNetCore.Identity.IdentityResult;

namespace EazyCash.Controllers
{
    [Auth()]
    public class HomeController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        readonly HROnlineModel.HROnlineModel db;


        public HomeController(IHttpClientFactory _httpfactory,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager, RoleManager<IdentityRole> roleManager,
            IWebHostEnvironment _env, HROnlineModel.HROnlineModel _db
        )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            db = _db;
        }

        public async Task<IActionResult> Index()
        {

            var user = await _userManager.GetUserAsync(HttpContext.User);
            var userid = int.Parse(user.UserName.Split("@")[0]);
            var homemodel = new HomeModel();
            var lst = new List<ManagerTransModel>();
            lst.AddRange(await GetManagerTrans());
            lst.AddRange(await GetSwapTrans());
            lst.AddRange(await GetSwapTransLevel3());
            homemodel.ManagerTrans = lst.OrderBy(t => (int)t.EmployeeLevelType).ToList();
            homemodel.MyTransModel = new List<ManagerTransModel>(); //await GetSwapTrans();
            homemodel.MyTransModelLevel3 = new List<ManagerTransModel>(); //await GetSwapTransLevel3();

            var screens = await db.TblEmployeeScreens.Where(t => t.Emp_ID == userid && t.ScreenName == "FrmPO6").Select(
                t => new
                    EmployeeScreenModel()
                    {
                        Emp_ID = t.Emp_ID,
                        CanAdd = t.CanAdd,
                        CanEdit = t.CanEdit,
                        CanShow = t.CanShow,
                        RowID = t.RowID,
                        ScreenName = t.ScreenName

                    }).AsNoTracking().FirstOrDefaultAsync();
            homemodel.ItemRequestShow = screens?.CanShow ?? false;
            return View(homemodel);
        }

        [NonAction]
        async Task<List<ManagerTransModel>> GetEmployeeTrans()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            var userid = int.Parse(user.UserName.Split("@")[0]);
            var employee = await db.TblEmployees.FirstOrDefaultAsync(t => t.Emp_ID == userid);
            // var status = (int)StatusEnum.Approved;
            var VacData = await (from vac in db.EmpVacations
                join empo in db.TblEmployees on vac.EmployeeId equals empo.Emp_ID into grp
                from emp in grp.DefaultIfEmpty()
                join aempo in db.TblEmployees on vac.AlterEmp equals aempo.Emp_ID into agrp
                from aemp in agrp.DefaultIfEmpty()
                join proj in db.projects on vac.project_id equals proj.id into gpprog
                from prjcts in gpprog.DefaultIfEmpty()
                join jp in db.TblEmpJobsTypes on vac.JobTypeID equals jp.JobTypeID into gpjobs
                from job in gpjobs.DefaultIfEmpty()
                where vac.EmployeeId == employee.Emp_ID
                //  && vac.Status != status
                select new
                {
                    Date = vac.StartDate,
                    EmployeeName = emp.Emp_Namee,
                    ProjectId = vac.project_id,
                    ProjectName = prjcts == null ? "" : prjcts.Project_nameE,
                    JobID = vac.JobTypeID,
                    JobName = job == null ? "" : job.JobTypeNamee,
                    EmployeeCode = emp.Emp_Code,
                    OrderId = vac.Id,
                    Type = (int)TransTypeEnum.Vaction,
                    Status = vac.Status,
                    FromDate = vac.StartDate,
                    ToDate = vac.EndDate,
                    Paid = vac.ChkSallary,
                    Notes = vac.Notes,
                    OrderDate = vac.OrderDate,
                    LevelOne = vac.LevelOne,
                    LevelTow = vac.LevelTow,
                    LevelThree = vac.LevelThree,
                    IsSickleave = vac.IsSickleave,
                    EmployeeLevelType = LevelApproveType.LevelOne,
                    alteremp = aemp == null ? "" : $"{aemp.Emp_Code}-{aemp.Emp_Namee}"
                }).AsNoTracking().ToListAsync();

            var perData = await (from vac in db.tblPermissions
                join empo in db.TblEmployees on vac.EmployeeId equals empo.Emp_ID into grp
                from emp in grp.DefaultIfEmpty()
                join proj in db.projects on vac.project_id equals proj.id into gpprog
                from prjcts in gpprog.DefaultIfEmpty()
                join jp in db.TblEmpJobsTypes on vac.JobTypeID equals jp.JobTypeID into gpjobs
                from job in gpjobs.DefaultIfEmpty()
                where vac.EmployeeId == employee.Emp_ID
                //  && vac.Status != status
                select new
                {
                    Date = vac.TransDate,
                    EmployeeName = emp.Emp_Name,
                    ProjectId = vac.project_id,
                    ProjectName = prjcts == null ? "" : prjcts.Project_nameE,
                    JobID = vac.JobTypeID,
                    JobName = job == null ? "" : job.JobTypeNamee,
                    EmployeeCode = emp.Emp_Code,
                    OrderId = vac.Id,
                    Type = (int)TransTypeEnum.Permssion,
                    Status = vac.Status,
                    FromTime = vac.FromTime,
                    ToTime = vac.ToTime,
                    Notes = vac.Notes,
                    OrderDate = vac.OrderDate,
                    LevelOne = vac.LevelOne,
                    LevelTow = vac.LevelTow,
                    LevelThree = vac.LevelThree,
                    EmployeeLevelType = LevelApproveType.LevelOne
                }).AsNoTracking().ToListAsync();

            var advnceData = await (from vac in db.tblAdvances
                join empo in db.TblEmployees on vac.EmpId equals empo.Emp_ID  
                 

                join empo1 in db.TblEmployees on vac.Emp1 equals empo1.Emp_ID  
                

                join empo2 in db.TblEmployees on vac.Emp2 equals empo2.Emp_ID  
               

                join proj in db.projects on vac.project_id equals proj.id into gpprog
                from prjcts in gpprog.DefaultIfEmpty()
                join jp in db.TblEmpJobsTypes on vac.JobTypeID equals jp.JobTypeID into gpjobs
                from job in gpjobs.DefaultIfEmpty()
                where vac.EmpId == employee.Emp_ID
                                    //  && vac.Status != status
                                    select new
                {
                    Date = vac.OrderDate,
                    EmployeeName = empo.Emp_Name,
                    ProjectId = vac.project_id,
                    ProjectName = prjcts == null ? "" : prjcts.Project_nameE,
                    JobID = vac.JobTypeID,
                    JobName = job == null ? "" : job.JobTypeNamee,
                    EmployeeCode = empo.Emp_Code,
                    OrderId = vac.Id,
                    Type = (int)TransTypeEnum.Advance,
                    Notes = vac.Notes,
                    OrderDate = vac.OrderDate,
                    LevelOne = vac.LevelOne,
                    LevelTow = vac.LevelTow,
                    LevelThree = vac.LevelThree,
                    EmployeeLevelType = LevelApproveType.LevelOne,
                    Emp1 = $"{empo1.Emp_Code}-{empo1.Emp_Namee}",
                    Emp2 = $"{empo2.Emp_Code}-{empo2.Emp_Namee}",
                    Amount = vac.Amount
                }).AsNoTracking().ToListAsync();
            //var reqData = await from  db.tblItemRequests.Where(t => t.EmployeeId == employee.Emp_ID).GroupBy(t => t.OrderId)
            //    .AsNoTracking().ToListAsync();

            var items = await (from itm in db.tblItemRequests
                join empo in db.TblEmployees on itm.EmployeeId equals empo.Emp_ID into grp
                from emp in grp.DefaultIfEmpty()
                join proj in db.projects on itm.Project_id equals proj.id into gpprog
                from prjcts in gpprog.DefaultIfEmpty()
                join jp in db.TblEmpJobsTypes on itm.JobTypeID equals jp.JobTypeID into gpjobs
                from job in gpjobs.DefaultIfEmpty()
                where itm.EmployeeId == employee.Emp_ID
                select new
                {
                    itm,
                    emp,
                    prjcts,
                    job
                }).AsNoTracking().ToListAsync();
            var reqData = items.GroupBy(t => t.itm.OrderId).ToList();
            var reqItems = reqData.Select(t => new
            {
                Id = t.Key,
                Date = t.First().itm.OrderDate,
                EmployeeName = t.First()?.emp?.Emp_Namee,
                ProjectId = t.First().itm.Project_id,
                ProjectName = t.First().prjcts?.Project_nameE,
                JobID = t.First().itm.JobTypeID,
                JobName = t.First().job?.JobTypeNamee,
                EmployeeCode = t.First().emp?.Emp_Code,
                OrderId = t.First().itm.OrderNo,
                Type = (int)TransTypeEnum.Materials,
                Status = t.First().itm.Status,
                Items = t.Select(item => new
                {
                    item.itm.ItemCode,
                    ItemName = item.itm.ItemNamee ?? item.itm.ItemName,
                    Qty = item.itm.Qty,
                    Notes = item.itm.Notes,
                    UnitId = item.itm.UnitId,
                    UnitName = item.itm.UnitName
                }),

                LevelOne = t.First().itm.LevelOne,
                LevelTow = t.First().itm.LevelTow,
                LevelThree = t.First().itm.LevelThree,
                EmployeeLevelType = LevelApproveType.LevelOne

            });



            var model = new List<ManagerTransModel>();
            model.AddRange(VacData.Select(t => new ManagerTransModel
            {
                Date = t.OrderDate.ToString("dd/MM/yyyy"),
                EmployeeName = t.EmployeeName ?? "",
                JobName = t.JobName ?? "",
                ProjectName = t.ProjectName ?? "",
                EmployeeCode = t.EmployeeCode ?? "",
                OrderId = t.OrderId,
                TransType = (TransTypeEnum)t.Type,
                Status = (StatusEnum)(t.Status ?? 0),
                LevelOne = t.LevelOne,
                LevelTow = t.LevelTow,
                LevelThree = t.LevelThree,
                EmployeeLevelType = t.EmployeeLevelType,
                IsSickleave = t.IsSickleave,
                AlterEmp = t.alteremp,
                Data =
                    $"From Date¢To Date¢Paid Type¢Notes¢Alternative Employee¢Sick leave¥{t.Date:dd/MM/yyyy}¢{t.ToDate:dd/MM/yyyy}¢{((t.Paid ?? false) ? "Paind" : "Unpaid")}¢{t.Notes}¢{t.alteremp}¢{((t.IsSickleave ?? false) ? "Sick leave" : "None")}"


            }).ToList());

            model.AddRange(perData.Select(t => new ManagerTransModel
            {
                Date = t.OrderDate.ToString("dd/MM/yyyy"),
                EmployeeName = t.EmployeeName ?? "",
                JobName = t.JobName ?? "",
                ProjectName = t.ProjectName ?? "",
                EmployeeCode = t.EmployeeCode ?? "",
                OrderId = (t.OrderId),
                TransType = (TransTypeEnum)t.Type,
                Status = (StatusEnum)(t.Status ?? 0),
                LevelOne = t.LevelOne,
                LevelTow = t.LevelTow,
                LevelThree = t.LevelThree,
                EmployeeLevelType = t.EmployeeLevelType,

                Data =
                    $"Permission Date¢From Time¢To Time¢Notes¥{t.Date:dd/MM/yyyy}¢{t.FromTime:hh:mm tt}¢{t.ToTime:hh:mm tt}¢{t.Notes}"
            }).ToList());

            model.AddRange(advnceData.Select(t => new ManagerTransModel
            {
                Date = t.OrderDate.ToString("dd/MM/yyyy"),
                EmployeeName = t.EmployeeName ?? "",
                JobName = t.JobName ?? "",
                ProjectName = t.ProjectName ?? "",
                EmployeeCode = t.EmployeeCode ?? "",
                OrderId = (t.OrderId),
                TransType = (TransTypeEnum)t.Type,
                LevelOne = t.LevelOne,
                LevelTow = t.LevelTow,
                LevelThree = t.LevelThree,
                EmployeeLevelType = t.EmployeeLevelType,
                Data =
                    $"Order Date¢Amount¢Guarantee 1¢Guarantee 2¢Notes¥{t.Date:dd/MM/yyyy}¢{t.Amount}¢{t.Emp1}¢{t.Emp2}¢{t.Notes}"
            }).ToList());
            model.AddRange(reqItems.Select(t => new ManagerTransModel
            {
                RowId = t.Id,
                Date = t.Date.HasValue ? t.Date.Value.ToString("dd/MM/yyyy") : "",
                EmployeeName = t.EmployeeName ?? "",
                JobName = t.JobName ?? "",
                ProjectName = t.ProjectName ?? "",
                EmployeeCode = t.EmployeeCode ?? "",
                OrderId = (t.OrderId),
                TransType = (TransTypeEnum)t.Type,
                Status = (StatusEnum)(t.Status ?? 0),
                LevelOne = t.LevelOne,
                LevelTow = t.LevelTow,
                LevelThree = t.LevelThree,
                EmployeeLevelType = t.EmployeeLevelType,
                Data = "Item Code ¢ Item Name ¢ Unit ¢ Qty ¢ Notes ¥" + string.Join("¥",
                    t.Items.Select(item => $"{item.ItemCode}¢{item.ItemName}¢{item.UnitName}¢{item.Qty}¢{item.Notes}")
                        .ToArray())
            }).ToList());

            return model;
        }


        [NonAction]
        async Task<List<ManagerTransModel>> GetManagerTrans()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            var userid = int.Parse(user.UserName.Split("@")[0]);
            var employee = await db.TblEmployees.FirstOrDefaultAsync(t => t.Emp_ID == userid);
            // var status = (int)StatusEnum.Approved;
            var VacData = await (from vac in db.EmpVacations
                join empo in db.TblEmployees on vac.EmployeeId equals empo.Emp_ID into grp
                from emp in grp.DefaultIfEmpty()
                join aempo in db.TblEmployees on vac.AlterEmp equals aempo.Emp_ID into agrp
                from aemp in agrp.DefaultIfEmpty()
                join proj in db.projects on vac.project_id equals proj.id into gpprog
                from prjcts in gpprog.DefaultIfEmpty()
                join jp in db.TblEmpJobsTypes on vac.JobTypeID equals jp.JobTypeID into gpjobs
                from job in gpjobs.DefaultIfEmpty()
                where (vac.ManagerId == employee.Emp_ID)
                //  && vac.Status != status
                select new
                {
                    Date = vac.StartDate,
                    EmployeeName = emp.Emp_Namee,
                    ProjectId = vac.project_id,
                    ProjectName = prjcts == null ? "" : prjcts.Project_nameE,
                    JobID = vac.JobTypeID,
                    JobName = job == null ? "" : job.JobTypeNamee,
                    EmployeeCode = emp.Emp_Code,
                    OrderId = vac.Id,
                    Type = (int)TransTypeEnum.Vaction,
                    Status = vac.Status,
                    FromDate = vac.StartDate,
                    ToDate = vac.EndDate,
                    Paid = vac.ChkSallary,
                    Notes = vac.Notes,
                    OrderDate = vac.OrderDate,
                    LevelOne = vac.LevelOne,
                    LevelTow = vac.LevelTow,
                    LevelThree = vac.LevelThree,
                    EmployeeLevelType = LevelApproveType.LevelOne,
                    IsSickleave = vac.IsSickleave,
                    alteremp = aemp == null ? "" : $"{aemp.Emp_Code}-{aemp.Emp_Namee}"
                }).AsNoTracking().ToListAsync();

            var perData = await (from vac in db.tblPermissions
                join empo in db.TblEmployees on vac.EmployeeId equals empo.Emp_ID into grp
                from emp in grp.DefaultIfEmpty()
                join proj in db.projects on vac.project_id equals proj.id into gpprog
                from prjcts in gpprog.DefaultIfEmpty()
                join jp in db.TblEmpJobsTypes on vac.JobTypeID equals jp.JobTypeID into gpjobs
                from job in gpjobs.DefaultIfEmpty()
                where vac.ManagerId == employee.Emp_ID
                //  && vac.Status != status
                select new
                {
                    Date = vac.TransDate,
                    EmployeeName = emp.Emp_Name,
                    ProjectId = vac.project_id,
                    ProjectName = prjcts == null ? "" : prjcts.Project_nameE,
                    JobID = vac.JobTypeID,
                    JobName = job == null ? "" : job.JobTypeNamee,
                    EmployeeCode = emp.Emp_Code,
                    OrderId = vac.Id,
                    Type = (int)TransTypeEnum.Permssion,
                    Status = vac.Status,
                    FromTime = vac.FromTime,
                    ToTime = vac.ToTime,
                    Notes = vac.Notes,
                    OrderDate = vac.OrderDate,
                    LevelOne = vac.LevelOne,
                    LevelTow = vac.LevelTow,
                    LevelThree = vac.LevelThree,
                    EmployeeLevelType = LevelApproveType.LevelOne
                }).AsNoTracking().ToListAsync();


            var advnceData = await (from vac in db.tblAdvances
                join empo in db.TblEmployees on vac.EmpId equals empo.Emp_ID  
              

                join empo1 in db.TblEmployees on vac.Emp1 equals empo1.Emp_ID  
              

                join empo2 in db.TblEmployees on vac.Emp2 equals empo2.Emp_ID  
              

                                    join proj in db.projects on vac.project_id equals proj.id into gpprog
                from prjcts in gpprog.DefaultIfEmpty()
                join jp in db.TblEmpJobsTypes on vac.JobTypeID equals jp.JobTypeID into gpjobs
                from job in gpjobs.DefaultIfEmpty()
                where vac.ManagerId == employee.Emp_ID
                //  && vac.Status != status
                select new
                {
                    Date = vac.OrderDate,
                    EmployeeName = empo.Emp_Name,
                    ProjectId = vac.project_id,
                    ProjectName = prjcts == null ? "" : prjcts.Project_nameE,
                    JobID = vac.JobTypeID,
                    JobName = job == null ? "" : job.JobTypeNamee,
                    EmployeeCode = empo.Emp_Code,
                    OrderId = vac.Id,
                    Type = (int)TransTypeEnum.Advance,
                    Notes = vac.Notes,
                    OrderDate = vac.OrderDate,
                    LevelOne = vac.LevelOne,
                    LevelTow = vac.LevelTow,
                    LevelThree = vac.LevelThree,
                    EmployeeLevelType = LevelApproveType.LevelOne,
                    Emp1 = $"{empo1.Emp_Code}-{empo1.Emp_Namee}",
                    Emp2 = $"{empo2.Emp_Code}-{empo2.Emp_Namee}",
                    Amount = vac.Amount
                }).AsNoTracking().ToListAsync();

            //var reqData = await db.tblItemRequests.Where(t => t.ManagerId == employee.Emp_ID).GroupBy(t => t.OrderId)
            //    .AsNoTracking().ToListAsync();

            var items = await (from itm in db.tblItemRequests
                join empo in db.TblEmployees on itm.EmployeeId equals empo.Emp_ID into grp
                from emp in grp.DefaultIfEmpty()
                join proj in db.projects on itm.Project_id equals proj.id into gpprog
                from prjcts in gpprog.DefaultIfEmpty()
                join jp in db.TblEmpJobsTypes on itm.JobTypeID equals jp.JobTypeID into gpjobs
                from job in gpjobs.DefaultIfEmpty()
                where itm.ManagerId == employee.Emp_ID
                select new
                {
                    itm,
                    emp,
                    prjcts,
                    job
                }).AsNoTracking().ToListAsync();
            var reqData = items.GroupBy(t => t.itm.OrderId).ToList();
            //    var reqData = items.GroupBy(t => t.itm.OrderId).ToList();
            var reqItems = reqData.Select(t => new
            {
                Id = t.Key,
                Date = t.First().itm.OrderDate,
                EmployeeName = t.First()?.emp?.Emp_Namee,
                ProjectId = t.First().itm.Project_id,
                ProjectName = t.First().prjcts?.Project_nameE,
                JobID = t.First().itm.JobTypeID,
                JobName = t.First().job?.JobTypeNamee,
                EmployeeCode = t.First().emp?.Emp_Code,
                OrderId = t.First().itm.OrderNo,
                Type = (int)TransTypeEnum.Materials,
                Status = t.First().itm.Status,
                Items = t.Select(item => new
                {
                    item.itm.ItemCode,
                    ItemName = item.itm.ItemNamee ?? item.itm.ItemName,
                    Qty = item.itm.Qty,
                    Notes = item.itm.Notes,
                    UnitId = item.itm.UnitId,
                    UnitName = item.itm.UnitName
                }),

                LevelOne = t.First().itm.LevelOne,
                LevelTow = t.First().itm.LevelTow,
                LevelThree = t.First().itm.LevelThree,
                EmployeeLevelType = LevelApproveType.LevelOne
            });


            var model = new List<ManagerTransModel>();
            model.AddRange(VacData.Select(t => new ManagerTransModel
            {
                Date = t.OrderDate.ToString("dd/MM/yyyy"),
                EmployeeName = t.EmployeeName ?? "",
                JobName = t.JobName ?? "",
                ProjectName = t.ProjectName ?? "",
                EmployeeCode = t.EmployeeCode ?? "",
                OrderId = t.OrderId,
                TransType = (TransTypeEnum)t.Type,
                Status = (StatusEnum)(t.Status ?? 0),
                LevelOne = t.LevelOne,
                LevelTow = t.LevelTow,
                LevelThree = t.LevelThree,
                EmployeeLevelType = t.EmployeeLevelType,
                IsSickleave = t.IsSickleave,
                AlterEmp = t.alteremp,
                Data =
                    $"From Date¢To Date¢Paid Type¢Notes¢Alternative Employee¢Sick leave¥{t.Date:dd/MM/yyyy}¢{t.ToDate:dd/MM/yyyy}¢{((t.Paid ?? false) ? "Paind" : "Unpaid")}¢{t.Notes}¢{t.alteremp}¢{((t.IsSickleave ?? false) ? "Sick leave" : "None")}"

            }).ToList());

            model.AddRange(perData.Select(t => new ManagerTransModel
            {
                Date = t.OrderDate.ToString("dd/MM/yyyy"),
                EmployeeName = t.EmployeeName ?? "",
                JobName = t.JobName ?? "",
                ProjectName = t.ProjectName ?? "",
                EmployeeCode = t.EmployeeCode ?? "",
                OrderId = (t.OrderId),
                TransType = (TransTypeEnum)t.Type,
                Status = (StatusEnum)(t.Status ?? 0),
                LevelOne = t.LevelOne,
                LevelTow = t.LevelTow,
                LevelThree = t.LevelThree,
                EmployeeLevelType = t.EmployeeLevelType,
                Data =
                    $"Permission Date¢From Time¢To Time¢Notes¥{t.Date:dd/MM/yyyy}¢{t.FromTime:hh:mm tt}¢{t.ToTime:hh:mm tt}¢{t.Notes}"
            }).ToList());

       model.AddRange(advnceData.Select(t => new ManagerTransModel
            {
                Date = t.OrderDate.ToString("dd/MM/yyyy"),
                EmployeeName = t.EmployeeName ?? "",
                JobName = t.JobName ?? "",
                ProjectName = t.ProjectName ?? "",
                EmployeeCode = t.EmployeeCode ?? "",
                OrderId = (t.OrderId),
                TransType = (TransTypeEnum)t.Type,
                LevelOne = t.LevelOne,
                LevelTow = t.LevelTow,
                LevelThree = t.LevelThree,
                EmployeeLevelType = t.EmployeeLevelType,
                Data =
                    $"Order Date¢Amount¢Guarantee 1¢Guarantee 2¢Notes¥{t.Date:dd/MM/yyyy}¢{t.Amount}¢{t.Emp1}¢{t.Emp2}¢{t.Notes}"
            }).ToList());


            model.AddRange(reqItems.Select(t => new ManagerTransModel
            {
                RowId = t.Id,
                Date = t.Date.HasValue ? t.Date.Value.ToString("dd/MM/yyyy") : "",
                EmployeeName = t.EmployeeName ?? "",
                JobName = t.JobName ?? "",
                ProjectName = t.ProjectName ?? "",
                EmployeeCode = t.EmployeeCode ?? "",
                OrderId = (t.OrderId),
                TransType = (TransTypeEnum)t.Type,
                Status = (StatusEnum)(t.Status ?? 0),
                LevelOne = t.LevelOne,
                LevelTow = t.LevelTow,
                LevelThree = t.LevelThree,
                EmployeeLevelType = t.EmployeeLevelType,
                Data = "Item Code ¢ Item Name ¢ Unit ¢ Qty ¢ Notes ¥" + string.Join("¥",
                    t.Items.Select(item => $"{item.ItemCode}¢{item.ItemName}¢{item.UnitName}¢{item.Qty}¢{item.Notes}")
                        .ToArray())
            }).ToList());

            return model;
        }

        [NonAction]
        async Task<List<ManagerTransModel>> GetSwapTrans()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            var userid = int.Parse(user.UserName.Split("@")[0]);
            //  var dbuser = await db.TblUsers.FirstOrDefaultAsync(t => t.UserID == userid);
            var employee = await db.TblEmployees.FirstOrDefaultAsync(t => t.Emp_ID == userid);
            //  var status = (int)StatusEnum.Approved;
            var VacData = await (from vac in db.EmpVacations
                join empo in db.TblEmployees on vac.EmployeeId equals empo.Emp_ID into grp
                from emp in grp.DefaultIfEmpty()
                join aempo in db.TblEmployees on vac.AlterEmp equals aempo.Emp_ID into agrp
                from aemp in agrp.DefaultIfEmpty()
                join proj in db.projects on vac.project_id equals proj.id into gpprog
                from prjcts in gpprog.DefaultIfEmpty()
                join jp in db.TblEmpJobsTypes on vac.JobTypeID equals jp.JobTypeID into gpjobs
                from job in gpjobs.DefaultIfEmpty()
                where emp.swapedempid == employee.Emp_ID
                //  && vac.Status != status
                select new
                {
                    Date = vac.StartDate,
                    EmployeeName = emp.Emp_Namee,
                    ProjectId = vac.project_id,
                    ProjectName = prjcts == null ? "" : prjcts.Project_nameE,
                    JobID = vac.JobTypeID,
                    JobName = job == null ? "" : job.JobTypeNamee,
                    EmployeeCode = emp.Emp_Code,
                    OrderId = vac.Id,
                    Type = (int)TransTypeEnum.Vaction,
                    Status = vac.Status,
                    FromDate = vac.StartDate,
                    ToDate = vac.EndDate,
                    Paid = vac.ChkSallary,
                    Notes = vac.Notes,
                    OrderDate = vac.OrderDate,
                    LevelOne = vac.LevelOne,
                    LevelTow = vac.LevelTow,
                    LevelThree = vac.LevelThree,
                    EmployeeLevelType = LevelApproveType.LevelTow,
                    IsSickleave = vac.IsSickleave,
                    alteremp = aemp == null ? "" : $"{aemp.Emp_Code}-{aemp.Emp_Namee}"
                }).AsNoTracking().ToListAsync();
            var perData = await (from vac in db.tblPermissions
                join empo in db.TblEmployees on vac.EmployeeId equals empo.Emp_ID into grp
                from emp in grp.DefaultIfEmpty()
                join proj in db.projects on vac.project_id equals proj.id into gpprog
                from prjcts in gpprog.DefaultIfEmpty()
                join jp in db.TblEmpJobsTypes on vac.JobTypeID equals jp.JobTypeID into gpjobs
                from job in gpjobs.DefaultIfEmpty()
                where emp.swapedempid == employee.Emp_ID
                //  && vac.Status != status
                select new
                {
                    Date = vac.TransDate,
                    EmployeeName = emp.Emp_Name,
                    ProjectId = vac.project_id,
                    ProjectName = prjcts == null ? "" : prjcts.Project_nameE,
                    JobID = vac.JobTypeID,
                    JobName = job == null ? "" : job.JobTypeNamee,
                    EmployeeCode = emp.Emp_Code,
                    OrderId = vac.Id,
                    Type = (int)TransTypeEnum.Permssion,
                    Status = vac.Status,
                    FromTime = vac.FromTime,
                    ToTime = vac.ToTime,
                    Notes = vac.Notes,
                    OrderDate = vac.OrderDate,
                    LevelOne = vac.LevelOne,
                    LevelTow = vac.LevelTow,
                    LevelThree = vac.LevelThree,
                    EmployeeLevelType = LevelApproveType.LevelTow
                }).AsNoTracking().ToListAsync();

            var advnceData = await (from vac in db.tblAdvances
                join empo in db.TblEmployees on vac.EmpId equals empo.Emp_ID  
               

                join empo1 in db.TblEmployees on vac.Emp1 equals empo1.Emp_ID  
                

                join empo2 in db.TblEmployees on vac.Emp2 equals empo2.Emp_ID  
                

                join proj in db.projects on vac.project_id equals proj.id into gpprog
                from prjcts in gpprog.DefaultIfEmpty()
                join jp in db.TblEmpJobsTypes on vac.JobTypeID equals jp.JobTypeID into gpjobs
                from job in gpjobs.DefaultIfEmpty()
                where empo.swapedempid == employee.Emp_ID
                                    //  && vac.Status != status
                                    select new
                {
                    Date = vac.OrderDate,
                    EmployeeName = empo.Emp_Name,
                    ProjectId = vac.project_id,
                    ProjectName = prjcts == null ? "" : prjcts.Project_nameE,
                    JobID = vac.JobTypeID,
                    JobName = job == null ? "" : job.JobTypeNamee,
                    EmployeeCode = empo.Emp_Code,
                    OrderId = vac.Id,
                    Type = (int)TransTypeEnum.Advance,
                    Notes = vac.Notes,
                    OrderDate = vac.OrderDate,
                    LevelOne = vac.LevelOne,
                    LevelTow = vac.LevelTow,
                    LevelThree = vac.LevelThree,
                    EmployeeLevelType = LevelApproveType.LevelOne,
                    Emp1 = $"{empo1.Emp_Code}-{empo1.Emp_Namee}",
                    Emp2 = $"{empo2.Emp_Code}-{empo2.Emp_Namee}",
                    Amount = vac.Amount
                }).AsNoTracking().ToListAsync();
            //var reqData = await (from trans in db.tblItemRequests
            //                         //   where trans.Status != status
            //                     join Emp in db.TblEmployees on trans.EmployeeId equals Emp.Emp_ID
            //                     where Emp.swapedempid == employee.Emp_ID
            //                     group trans by trans.OrderId)
            //    .AsNoTracking().ToListAsync();

            var items = await (from itm in db.tblItemRequests
                join empo in db.TblEmployees on itm.EmployeeId equals empo.Emp_ID into grp
                from emp in grp.DefaultIfEmpty()
                join proj in db.projects on itm.Project_id equals proj.id into gpprog
                from prjcts in gpprog.DefaultIfEmpty()
                join jp in db.TblEmpJobsTypes on itm.JobTypeID equals jp.JobTypeID into gpjobs
                from job in gpjobs.DefaultIfEmpty()
                where emp.swapedempid == employee.Emp_ID
                select new
                {
                    itm,
                    emp,
                    prjcts,
                    job
                }).AsNoTracking().ToListAsync();
            var reqData = items.GroupBy(t => t.itm.OrderId).ToList();
            var reqItems = reqData.Select(t => new
            {
                Id = t.Key,
                Date = t.First().itm.OrderDate,
                EmployeeName = t.First()?.emp?.Emp_Namee,
                ProjectId = t.First().itm.Project_id,
                ProjectName = t.First().prjcts?.Project_nameE,
                JobID = t.First().itm.JobTypeID,
                JobName = t.First().job?.JobTypeNamee,
                EmployeeCode = t.First().emp?.Emp_Code,
                OrderId = t.First().itm.OrderNo,
                Type = (int)TransTypeEnum.Materials,
                Status = t.First().itm.Status,
                Items = t.Select(item => new
                {
                    item.itm.ItemCode,
                    ItemName = item.itm.ItemNamee ?? item.itm.ItemName,
                    Qty = item.itm.Qty,
                    Notes = item.itm.Notes,
                    UnitId = item.itm.UnitId,
                    UnitName = item.itm.UnitName
                }),

                LevelOne = t.First().itm.LevelOne,
                LevelTow = t.First().itm.LevelTow,
                LevelThree = t.First().itm.LevelThree,
                EmployeeLevelType = LevelApproveType.LevelOne
            });

            //var reqItems = reqData.Select(t => new
            //{
            //    Date = t.First().OrderDate,
            //    Name = t.First().EmployeeName,
            //    OrderId = t.First().OrderNo,
            //    Type = (int)TransTypeEnum.Materials,
            //    Status = t.First().Status,
            //    Items = t.Select(item => new
            //    {
            //        item.ItemCode,
            //        ItemName = item.ItemNamee ?? item.ItemName,
            //        Qty = item.Qty,
            //        Notes = item.Notes
            //    }),
            //    LevelOne = t.First().LevelOne,
            //    LevelTow = t.First().LevelTow,
            //    LevelThree = t.First().LevelThree,
            //    EmployeeLevelType = LevelApproveType.LevelTow
            //});



            var model = new List<ManagerTransModel>();
            model.AddRange(VacData.Select(t => new ManagerTransModel
            {
                Date = t.OrderDate.ToString("dd/MM/yyyy"),
                EmployeeName = t.EmployeeName ?? "",
                JobName = t.JobName ?? "",
                ProjectName = t.ProjectName ?? "",
                EmployeeCode = t.EmployeeCode ?? "",
                OrderId = t.OrderId,
                TransType = (TransTypeEnum)t.Type,
                Status = (StatusEnum)(t.Status ?? 0),
                LevelOne = t.LevelOne,
                LevelTow = t.LevelTow,
                LevelThree = t.LevelThree,
                EmployeeLevelType = LevelApproveType.LevelTow,
                IsSickleave = t.IsSickleave,
                AlterEmp = t.alteremp,
                Data =
                    $"From Date¢To Date¢Paid Type¢Notes¢Alternative Employee¢Sick leave¥{t.Date:dd/MM/yyyy}¢{t.ToDate:dd/MM/yyyy}¢{((t.Paid ?? false) ? "Paind" : "Unpaid")}¢{t.Notes}¢{t.alteremp}¢{((t.IsSickleave ?? false) ? "Sick leave" : "None")}"


            }).ToList());

            model.AddRange(perData.Select(t => new ManagerTransModel
            {
                Date = t.OrderDate.ToString("dd/MM/yyyy"),
                EmployeeName = t.EmployeeName ?? "",
                JobName = t.JobName ?? "",
                ProjectName = t.ProjectName ?? "",
                EmployeeCode = t.EmployeeCode ?? "",
                OrderId = (t.OrderId),
                TransType = (TransTypeEnum)t.Type,
                Status = (StatusEnum)(t.Status ?? 0),
                LevelOne = t.LevelOne,
                LevelTow = t.LevelTow,
                LevelThree = t.LevelThree,
                EmployeeLevelType = LevelApproveType.LevelTow,
                Data =
                    $"Permission Date¢From Time¢To Time¢Notes¥{t.Date:dd/MM/yyyy}¢{t.FromTime:hh:mm tt}¢{t.ToTime:hh:mm tt}¢{t.Notes}"
            }).ToList());

            model.AddRange(advnceData.Select(t => new ManagerTransModel
            {
                Date = t.OrderDate.ToString("dd/MM/yyyy"),
                EmployeeName = t.EmployeeName ?? "",
                JobName = t.JobName ?? "",
                ProjectName = t.ProjectName ?? "",
                EmployeeCode = t.EmployeeCode ?? "",
                OrderId = (t.OrderId),
                TransType = (TransTypeEnum)t.Type,
                LevelOne = t.LevelOne,
                LevelTow = t.LevelTow,
                LevelThree = t.LevelThree,
                EmployeeLevelType = t.EmployeeLevelType,
                Data =
                    $"Order Date¢Amount¢Guarantee 1¢Guarantee 2¢Notes¥{t.Date:dd/MM/yyyy}¢{t.Amount}¢{t.Emp1}¢{t.Emp2}¢{t.Notes}"
            }).ToList());
            model.AddRange(reqItems.Select(t => new ManagerTransModel
            {
                RowId = t.Id,
                Date = t.Date.HasValue ? t.Date.Value.ToString("dd/MM/yyyy") : "",
                EmployeeName = t.EmployeeName ?? "",
                JobName = t.JobName ?? "",
                ProjectName = t.ProjectName ?? "",
                EmployeeCode = t.EmployeeCode ?? "",
                OrderId = (t.OrderId),
                TransType = (TransTypeEnum)t.Type,
                Status = (StatusEnum)(t.Status ?? 0),
                LevelOne = t.LevelOne,
                LevelTow = t.LevelTow,
                LevelThree = t.LevelThree,
                EmployeeLevelType = LevelApproveType.LevelTow,
                Data = "Item Code ¢ Item Name ¢ Unit ¢ Qty ¢ Notes ¥" + string.Join("¥",
                    t.Items.Select(item => $"{item.ItemCode}¢{item.ItemName}¢{item.UnitName}¢{item.Qty}¢{item.Notes}")
                        .ToArray())
            }).ToList());

            return model;
        }

        [HttpGet]
        public async Task<IActionResult> getitemReportfile(Guid id)
        {
            var rpt = from itm in db.tblItemRequests
                join empo in db.TblEmployees on itm.EmployeeId equals empo.Emp_ID into empGrp
                from Emp in empGrp.DefaultIfEmpty()
                join projo in db.projects on itm.Project_id equals projo.id into progrp
                from Proj in progrp.DefaultIfEmpty()
                join unito in db.TblUnites on itm.UnitId equals unito.UnitID into unitoGrp
                from Unit in unitoGrp.DefaultIfEmpty()
                join jopo in db.TblEmpJobsTypes on itm.JobTypeID equals jopo.JobTypeID into jopoGrp
                from jop in jopoGrp.DefaultIfEmpty()
                join itmo in db.TblItems on itm.ItemId equals itmo.ItemID into itmogrp
                from item in itmogrp.DefaultIfEmpty()
                where itm.OrderId == id
                select new
                {
                    EmployeeCode = itm.EmployeeCode,
                    EmployeeName = Emp == null ? "" : Emp.Emp_Namee,
                    Job = jop == null ? "" : jop.JobTypeNamee,
                    Project = Proj == null ? "" : Proj.Project_nameE,
                    OrderNo = itm.OrderNo,
                    OrderDate = itm.OrderDate,
                    ItemCode = itm.ItemCode,
                    ItemName = item == null ? "" : item.ItemNamee,
                    Unit = Unit == null ? "" : Unit.UnitNamee,
                    Qty = itm.Qty,
                    Notes = itm.Notes


                };



            var report = new MaterialsRequisitionRpt();
            report.DataSource = await rpt.ToListAsync();

            var stream = new MemoryStream();
            var fileDownload = "";
            var contentType = "";
            using (MemoryStream ms = new MemoryStream())
            {
                contentType = "application/pdf";
                report.ExportToPdf(ms);
                fileDownload = $"MaterialsRequisitionRpt{rpt?.FirstOrDefault()?.OrderNo}.pdf";
                return File(ms.ToArray(), contentType, fileDownload);
            }

        }

        [NonAction]
        async Task<List<ManagerTransModel>> GetSwapTransLevel3()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            var userid = int.Parse(user.UserName.Split("@")[0]);
            //  var dbuser = await db.TblUsers.FirstOrDefaultAsync(t => t.UserID == userid);
            var employee = await db.TblEmployees.FirstOrDefaultAsync(t => t.Emp_ID == userid);
            // var status = (int)StatusEnum.Approved;
            var VacData = await (from vac in db.EmpVacations
                join empo in db.TblEmployees on vac.EmployeeId equals empo.Emp_ID into grp
                from emp in grp.DefaultIfEmpty()

                join aempo in db.TblEmployees on vac.AlterEmp equals aempo.Emp_ID into agrp
                from aemp in agrp.DefaultIfEmpty()
                join proj in db.projects on vac.project_id equals proj.id into gpprog
                from prjcts in gpprog.DefaultIfEmpty()
                join jp in db.TblEmpJobsTypes on vac.JobTypeID equals jp.JobTypeID into gpjobs
                from job in gpjobs.DefaultIfEmpty()
                where emp.swapedempid2 == employee.Emp_ID
                // && vac.Status != status
                select new
                {
                    Date = vac.StartDate,
                    EmployeeName = emp.Emp_Namee,
                    ProjectId = vac.project_id,
                    ProjectName = prjcts == null ? "" : prjcts.Project_nameE,
                    JobID = vac.JobTypeID,
                    JobName = job == null ? "" : job.JobTypeNamee,
                    EmployeeCode = emp.Emp_Code,
                    OrderId = vac.Id,
                    Type = (int)TransTypeEnum.Vaction,
                    Status = vac.Status,
                    FromDate = vac.StartDate,
                    ToDate = vac.EndDate,
                    Paid = vac.ChkSallary,
                    Notes = vac.Notes,
                    OrderDate = vac.OrderDate,
                    LevelOne = vac.LevelOne,
                    LevelTow = vac.LevelTow,
                    LevelThree = vac.LevelThree,
                    EmployeeLevelType = LevelApproveType.LevelThree,
                    IsSickleave = vac.IsSickleave,
                    alteremp = aemp == null ? "" : $"{aemp.Emp_Code}-{aemp.Emp_Namee}"
                }).AsNoTracking().ToListAsync();

            var perData = await (from vac in db.tblPermissions
                join empo in db.TblEmployees on vac.EmployeeId equals empo.Emp_ID into grp
                from emp in grp.DefaultIfEmpty()
                join proj in db.projects on vac.project_id equals proj.id into gpprog
                from prjcts in gpprog.DefaultIfEmpty()
                join jp in db.TblEmpJobsTypes on vac.JobTypeID equals jp.JobTypeID into gpjobs
                from job in gpjobs.DefaultIfEmpty()
                where emp.swapedempid2 == employee.Emp_ID
                //  && vac.Status != status
                select new
                {
                    Date = vac.TransDate,
                    EmployeeName = emp.Emp_Name,
                    ProjectId = vac.project_id,
                    ProjectName = prjcts.Project_nameE,
                    JobID = vac.JobTypeID,
                    JobName = job.JobTypeNamee,
                    EmployeeCode = emp.Emp_Code,
                    OrderId = vac.Id,
                    Type = (int)TransTypeEnum.Permssion,
                    Status = vac.Status,
                    FromTime = vac.FromTime,
                    ToTime = vac.ToTime,
                    Notes = vac.Notes,
                    OrderDate = vac.OrderDate,
                    LevelOne = vac.LevelOne,
                    LevelTow = vac.LevelTow,
                    LevelThree = vac.LevelThree,
                    EmployeeLevelType = LevelApproveType.LevelThree
                }).AsNoTracking().ToListAsync();


            var advnceData = await (from vac in db.tblAdvances
                join empo in db.TblEmployees on vac.EmpId equals empo.Emp_ID 
                 

                join empo1 in db.TblEmployees on vac.Emp1 equals empo1.Emp_ID  
             

                join empo2 in db.TblEmployees on vac.Emp2 equals empo2.Emp_ID  
               

                join proj in db.projects on vac.project_id equals proj.id into gpprog
                from prjcts in gpprog.DefaultIfEmpty()
                join jp in db.TblEmpJobsTypes on vac.JobTypeID equals jp.JobTypeID into gpjobs
                from job in gpjobs.DefaultIfEmpty()
                where empo.swapedempid2 == employee.Emp_ID
                                    //  && vac.Status != status
                                    select new
                {
                    Date = vac.OrderDate,
                    EmployeeName = empo.Emp_Name,
                    ProjectId = vac.project_id,
                    ProjectName = prjcts == null ? "" : prjcts.Project_nameE,
                    JobID = vac.JobTypeID,
                    JobName = job == null ? "" : job.JobTypeNamee,
                    EmployeeCode = empo.Emp_Code,
                    OrderId = vac.Id,
                    Type = (int)TransTypeEnum.Advance,
                    Notes = vac.Notes,
                    OrderDate = vac.OrderDate,
                    LevelOne = vac.LevelOne,
                    LevelTow = vac.LevelTow,
                    LevelThree = vac.LevelThree,
                    EmployeeLevelType = LevelApproveType.LevelOne,
                    Emp1 = $"{empo1.Emp_Code}-{empo1.Emp_Namee}",
                    Emp2 = $"{empo2.Emp_Code}-{empo2.Emp_Namee}",
                    Amount = vac.Amount
                }).AsNoTracking().ToListAsync();
            //var reqData = await (from trans in db.tblItemRequests
            //                         // where trans.Status != status
            //                     join Emp in db.TblEmployees on trans.EmployeeId equals Emp.Emp_ID
            //                     where Emp.swapedempid2 == employee.Emp_ID
            //                     group trans by trans.OrderId)
            //    .AsNoTracking().ToListAsync();

            //var reqItems = reqData.Select(t => new
            //{
            //    Date = t.First().OrderDate,
            //    Name = t.First().EmployeeName,
            //    OrderId = t.First().OrderNo,
            //    Type = (int)TransTypeEnum.Materials,
            //    Status = t.First().Status,
            //    Items = t.Select(item => new
            //    {
            //        item.ItemCode,
            //        ItemName = item.ItemNamee ?? item.ItemName,
            //        Qty = item.Qty,
            //        Notes = item.Notes
            //    }),
            //    LevelOne = t.First().LevelOne,
            //    LevelTow = t.First().LevelTow,
            //    LevelThree = t.First().LevelThree,
            //    EmployeeLevelType = LevelApproveType.LevelThree
            //});


            var items = await (from itm in db.tblItemRequests
                join empo in db.TblEmployees on itm.EmployeeId equals empo.Emp_ID into grp
                from emp in grp.DefaultIfEmpty()
                join proj in db.projects on itm.Project_id equals proj.id into gpprog
                from prjcts in gpprog.DefaultIfEmpty()
                join jp in db.TblEmpJobsTypes on itm.JobTypeID equals jp.JobTypeID into gpjobs
                from job in gpjobs.DefaultIfEmpty()
                where emp.swapedempid2 == employee.Emp_ID
                select new
                {
                    itm,
                    emp,
                    prjcts,
                    job
                }).AsNoTracking().ToListAsync();
            var reqData = items.GroupBy(t => t.itm.OrderId).ToList();
            var reqItems = reqData.Select(t => new
            {
                Id = t.Key,
                Date = t.First().itm.OrderDate,
                EmployeeName = t.First()?.emp?.Emp_Namee,
                ProjectId = t.First().itm.Project_id,
                ProjectName = t.First().prjcts?.Project_nameE,
                JobID = t.First().itm.JobTypeID,
                JobName = t.First().job?.JobTypeNamee,
                EmployeeCode = t.First().emp?.Emp_Code,
                OrderId = t.First().itm.OrderNo,
                Type = (int)TransTypeEnum.Materials,
                Status = t.First().itm.Status,
                Items = t.Select(item => new
                {
                    item.itm.ItemCode,
                    ItemName = item.itm.ItemNamee ?? item.itm.ItemName,
                    Qty = item.itm.Qty,
                    Notes = item.itm.Notes,
                    UnitId = item.itm.UnitId,
                    UnitName = item.itm.UnitName
                }),

                LevelOne = t.First().itm.LevelOne,
                LevelTow = t.First().itm.LevelTow,
                LevelThree = t.First().itm.LevelThree,
                EmployeeLevelType = LevelApproveType.LevelOne
            });


            var model = new List<ManagerTransModel>();
            model.AddRange(VacData.Select(t => new ManagerTransModel
            {
                Date = t.OrderDate.ToString("dd/MM/yyyy"),
                EmployeeName = t.EmployeeName ?? "",
                JobName = t.JobName ?? "",
                ProjectName = t.ProjectName ?? "",
                EmployeeCode = t.EmployeeCode ?? "",
                OrderId = t.OrderId,
                TransType = (TransTypeEnum)t.Type,
                Status = (StatusEnum)(t.Status ?? 0),
                LevelOne = t.LevelOne,
                LevelTow = t.LevelTow,
                LevelThree = t.LevelThree,
                EmployeeLevelType = LevelApproveType.LevelThree,
                IsSickleave = t.IsSickleave,
                AlterEmp = t.alteremp,
                Data =
                    $"From Date¢To Date¢Paid Type¢Notes¢Alternative Employee¢Sick leave¥{t.Date:dd/MM/yyyy}¢{t.ToDate:dd/MM/yyyy}¢{((t.Paid ?? false) ? "Paind" : "Unpaid")}¢{t.Notes}¢{t.alteremp}¢{((t.IsSickleave ?? false) ? "Sick leave" : "None")}"


            }).ToList());

            model.AddRange(perData.Select(t => new ManagerTransModel
            {
                Date = t.OrderDate.ToString("dd/MM/yyyy"),
                EmployeeName = t.EmployeeName ?? "",
                JobName = t.JobName ?? "",
                ProjectName = t.ProjectName ?? "",
                EmployeeCode = t.EmployeeCode ?? "",
                OrderId = (t.OrderId),
                TransType = (TransTypeEnum)t.Type,
                Status = (StatusEnum)(t.Status ?? 0),
                LevelOne = t.LevelOne,
                LevelTow = t.LevelTow,
                LevelThree = t.LevelThree,
                EmployeeLevelType = LevelApproveType.LevelThree,
                Data =
                    $"Permission Date¢From Time¢To Time¢Notes¥{t.Date:dd/MM/yyyy}¢{t.FromTime:hh:mm tt}¢{t.ToTime:hh:mm tt}¢{t.Notes}"
            }).ToList());

            model.AddRange(advnceData.Select(t => new ManagerTransModel
            {
                Date = t.OrderDate.ToString("dd/MM/yyyy"),
                EmployeeName = t.EmployeeName ?? "",
                JobName = t.JobName ?? "",
                ProjectName = t.ProjectName ?? "",
                EmployeeCode = t.EmployeeCode ?? "",
                OrderId = (t.OrderId),
                TransType = (TransTypeEnum)t.Type,
                LevelOne = t.LevelOne,
                LevelTow = t.LevelTow,
                LevelThree = t.LevelThree,
                EmployeeLevelType = t.EmployeeLevelType,
                Data =
                    $"Order Date¢Amount¢Guarantee 1¢Guarantee 2¢Notes¥{t.Date:dd/MM/yyyy}¢{t.Amount}¢{t.Emp1}¢{t.Emp2}¢{t.Notes}"
            }).ToList());


            model.AddRange(reqItems.Select(t => new ManagerTransModel
            {
                RowId = t.Id,
                Date = t.Date.HasValue ? t.Date.Value.ToString("dd/MM/yyyy") : "",
                EmployeeName = t.EmployeeName ?? "",
                JobName = t.JobName ?? "",
                ProjectName = t.ProjectName ?? "",
                EmployeeCode = t.EmployeeCode ?? "",
                OrderId = (t.OrderId),
                TransType = (TransTypeEnum)t.Type,
                Status = (StatusEnum)(t.Status ?? 0),
                LevelOne = t.LevelOne,
                LevelTow = t.LevelTow,
                LevelThree = t.LevelThree,
                EmployeeLevelType = LevelApproveType.LevelThree,
                Data = "Item Code ¢ Item Name ¢ Unit ¢ Qty ¢ Notes ¥" + string.Join("¥",
                    t.Items.Select(item => $"{item.ItemCode}¢{item.ItemName}¢{item.UnitName}¢{item.Qty}¢{item.Notes}")
                        .ToArray())
            }).ToList());

            return model;
        }

        //[NonAction]
        //async Task<List<ManagerTransModel>> GetFinshedManagerTrans()
        //{
        //    var user = await _userManager.GetUserAsync(HttpContext.User);
        //    var userid = int.Parse(user.UserName.Split("@")[0]);
        //    //  var dbuser = await db.TblUsers.FirstOrDefaultAsync(t => t.UserID == userid);
        //    var employee = await db.TblEmployees.FirstOrDefaultAsync(t => t.Emp_ID == userid);
        //    var status = (int)StatusEnum.Approved;
        //    var VacData = await (from vac in db.EmpVacations
        //                         join empo in db.TblEmployees on vac.EmployeeId equals empo.Emp_ID into grp
        //                         from emp in grp.DefaultIfEmpty()
        //                         where vac.ManagerId == employee.Emp_ID
        //                         && vac.Status == status
        //                         select new
        //                         {
        //                             Date = vac.StartDate,
        //                             Name = emp.Emp_Name,
        //                             OrderId = vac.Id,
        //                             Type = (int)TransTypeEnum.Vaction,
        //                             Status = vac.Status,
        //                             FromDate = vac.StartDate,
        //                             ToDate = vac.EndDate,
        //                             Paid = vac.ChkSallary,
        //                             Notes = vac.Notes,
        //                             OrderDate = vac.OrderDate 



        //                         }).AsNoTracking().ToListAsync();
        //    var perData = await (from vac in db.tblPermissions
        //                         join empo in db.TblEmployees on vac.EmployeeId equals empo.Emp_ID into grp
        //                         from emp in grp.DefaultIfEmpty()
        //                         where vac.ManagerId == employee.Emp_ID
        //                               && vac.Status == status
        //                         select new
        //                         {
        //                             Date = vac.TransDate,
        //                             Name = emp.Emp_Name,
        //                             OrderId = vac.Id,
        //                             Type = (int)TransTypeEnum.Permssion,
        //                             Status = vac.Status,
        //                             FromTime = vac.FromTime,
        //                             ToTime = vac.ToTime,
        //                             Notes = vac.Notes,
        //                             OrderDate = vac.OrderDate
        //                         }).AsNoTracking().ToListAsync();

        //    var reqData = await db.tblItemRequests.Where(t => t.Status == status).Where(t => t.ManagerId == employee.Emp_ID).GroupBy(t => t.OrderId)
        //        .AsNoTracking().ToListAsync();

        //    var reqItems = reqData.Select(t => new
        //    {
        //        Date = t.First().OrderDate,
        //        Name = t.First().EmployeeName,
        //        OrderId = t.First().OrderNo,
        //        Type = (int)TransTypeEnum.Materials,
        //        Status = t.First().Status,
        //        Items = t.Select(item => new
        //        {
        //            item.ItemCode,
        //            ItemName = item.ItemNamee ?? item.ItemName,
        //            Qty = item.Qty,
        //            Notes = item.Notes
        //        })
        //    });



        //    var model = new List<ManagerTransModel>();
        //    model.AddRange(VacData.Select(t => new ManagerTransModel
        //    {
        //        Date = t.OrderDate.ToString("dd/MM/yyyy"),
        //        Name = t.Name,
        //        OrderId = t.OrderId,
        //        TransType = (TransTypeEnum)t.Type,
        //        Status = (StatusEnum)(t.Status ?? 0),
        //        Data = $"From Date¢To Date¢Paid Type¢Notes¥{t.Date:dd/MM/yyyy}¢{t.ToDate:dd/MM/yyyy}¢{((t.Paid ?? false) ? "Paind" : "Unpaid")}¢{t.Notes}"


        //    }).ToList());

        //    model.AddRange(perData.Select(t => new ManagerTransModel
        //    {
        //        Date = t.OrderDate.ToString("dd/MM/yyyy"),
        //        Name = t.Name,
        //        OrderId = (t.OrderId),
        //        TransType = (TransTypeEnum)t.Type,
        //        Status = (StatusEnum)(t.Status ?? 0),
        //        Data = $"Permission Date¢From Time¢To Time¢Notes¥{t.Date:dd/MM/yyyy}¢{t.FromTime:hh:mm tt}¢{t.ToTime:hh:mm tt}¢{t.Notes}"
        //    }).ToList());

        //    model.AddRange(reqItems.Select(t => new ManagerTransModel
        //    {
        //        Date = t.Date.HasValue ? t.Date.Value.ToString("dd/MM/yyyy") : "",
        //        Name = t.Name,
        //        OrderId = (t.OrderId),
        //        TransType = (TransTypeEnum)t.Type,
        //        Status = (StatusEnum)(t.Status ?? 0),
        //        Data = "Item Code ¢ Item Name ¢ Qty ¢ Notes ¥" + string.Join("¥", t.Items.Select(item => $"{item.ItemCode}¢{item.ItemName}¢{item.Qty}¢{item.Notes}").ToArray())
        //    }).ToList());

        //    return model;
        //}
        //[NonAction]
        //async Task<List<ManagerTransModel>> GetFinshedSwapTrans()
        //{
        //    var user = await _userManager.GetUserAsync(HttpContext.User);
        //    var userid = int.Parse(user.UserName.Split("@")[0]);
        //    //  var dbuser = await db.TblUsers.FirstOrDefaultAsync(t => t.UserID == userid);
        //    var employee = await db.TblEmployees.FirstOrDefaultAsync(t => t.Emp_ID == userid);
        //    var status = (int)StatusEnum.Approved;
        //    var VacData = await (from vac in db.EmpVacations
        //                         join empo in db.TblEmployees on vac.EmployeeId equals empo.Emp_ID into grp
        //                         from emp in grp.DefaultIfEmpty()
        //                         where emp.swapedempid == employee.Emp_ID
        //                         && vac.Status == status
        //                         select new
        //                         {
        //                             Date = vac.StartDate,
        //                             Name = emp.Emp_Name,
        //                             OrderId = vac.Id,
        //                             Type = (int)TransTypeEnum.Vaction,
        //                             Status = vac.Status,
        //                             FromDate = vac.StartDate,
        //                             ToDate = vac.EndDate,
        //                             Paid = vac.ChkSallary,
        //                             Notes = vac.Notes,
        //                             OrderDate = vac.OrderDate
        //                         }).AsNoTracking().ToListAsync();
        //    var perData = await (from vac in db.tblPermissions
        //                         join empo in db.TblEmployees on vac.EmployeeId equals empo.Emp_ID into grp
        //                         from emp in grp.DefaultIfEmpty()
        //                         where emp.swapedempid == employee.Emp_ID
        //                               && vac.Status == status
        //                         select new
        //                         {
        //                             Date = vac.TransDate,
        //                             Name = emp.Emp_Name,
        //                             OrderId = vac.Id,
        //                             Type = (int)TransTypeEnum.Permssion,
        //                             Status = vac.Status,
        //                             FromTime = vac.FromTime,
        //                             ToTime = vac.ToTime,
        //                             Notes = vac.Notes,
        //                             OrderDate = vac.OrderDate
        //                         }).AsNoTracking().ToListAsync();

        //    var reqData = await (from trans in db.tblItemRequests
        //                         where trans.Status == status
        //                         join Emp in db.TblEmployees on trans.EmployeeId equals Emp.Emp_ID
        //                         where Emp.swapedempid == employee.Emp_ID
        //                         group trans by trans.OrderId)
        //        .AsNoTracking().ToListAsync();

        //    var reqItems = reqData.Select(t => new
        //    {
        //        Date = t.First().OrderDate,
        //        Name = t.First().EmployeeName,
        //        OrderId = t.First().OrderNo,
        //        Type = (int)TransTypeEnum.Materials,
        //        Status = t.First().Status,
        //        Items = t.Select(item => new
        //        {
        //            item.ItemCode,
        //            ItemName = item.ItemNamee ?? item.ItemName,
        //            Qty = item.Qty,
        //            Notes = item.Notes
        //        })
        //    });



        //    var model = new List<ManagerTransModel>();
        //    model.AddRange(VacData.Select(t => new ManagerTransModel
        //    {
        //        Date = t.OrderDate.ToString("dd/MM/yyyy"),
        //        Name = t.Name,
        //        OrderId = t.OrderId,
        //        TransType = (TransTypeEnum)t.Type,
        //        Status = (StatusEnum)(t.Status ?? 0),
        //        Data = $"From Date¢To Date¢Paid Type¢Notes¥{t.Date:dd/MM/yyyy}¢{t.ToDate:dd/MM/yyyy}¢{((t.Paid ?? false) ? "Paind" : "Unpaid")}¢{t.Notes}"


        //    }).ToList());

        //    model.AddRange(perData.Select(t => new ManagerTransModel
        //    {
        //        Date = t.OrderDate.ToString("dd/MM/yyyy"),
        //        Name = t.Name,
        //        OrderId = (t.OrderId),
        //        TransType = (TransTypeEnum)t.Type,
        //        Status = (StatusEnum)(t.Status ?? 0),
        //        Data = $"Permission Date¢From Time¢To Time¢Notes¥{t.Date:dd/MM/yyyy}¢{t.FromTime:hh:mm tt}¢{t.ToTime:hh:mm tt}¢{t.Notes}"
        //    }).ToList());

        //    model.AddRange(reqItems.Select(t => new ManagerTransModel
        //    {
        //        Date = t.Date.HasValue ? t.Date.Value.ToString("dd/MM/yyyy") : "",
        //        Name = t.Name,
        //        OrderId = (t.OrderId),
        //        TransType = (TransTypeEnum)t.Type,
        //        Status = (StatusEnum)(t.Status ?? 0),
        //        Data = "Item Code ¢ Item Name ¢ Qty ¢ Notes ¥" + string.Join("¥", t.Items.Select(item => $"{item.ItemCode}¢{item.ItemName}¢{item.Qty}¢{item.Notes}").ToArray())
        //    }).ToList());

        //    return model;
        //}

        [HttpGet]
        public async Task<ViewResult> Vacation()
        {
            var messges = new List<string>();
            var user = await _userManager.GetUserAsync(HttpContext.User);
            var userid = int.Parse(user.UserName.Split("@")[0]);
            //  var dbuser = await db.TblUsers.FirstOrDefaultAsync(t => t.UserID == userid);
            var employee = await db.TblEmployees.FirstOrDefaultAsync(t => t.Emp_ID == userid);
            var model = new VactionModel();
            model.EmployeeCode = employee.Emp_Code;
            model.EmployeeName = employee.Emp_Namee ?? employee.Emp_Name;
            model.OrderDate = DateTime.Now.ToSiteTime();
            model.Balance = employee.balanceH3;
            return View(model);
        }

        [HttpPost]
        public async Task<ViewResult> Vacation(VactionModel Model)
        {
            //if (!ModelState.IsValid)
            //{
            //    return View(Model);
            //}
            var user = await _userManager.GetUserAsync(HttpContext.User);
            var userid = int.Parse(user.UserName.Split("@")[0]);
            // var dbuser = await db.TblUsers.FirstOrDefaultAsync(t => t.UserID == userid);

            var employee = await db.TblEmployees.FirstOrDefaultAsync(t => t.Emp_ID == userid);
            DateTime fromDate = DateTime.Now; //= new DateTime(da.Parse(Model.FromDate)  );
            DateTime ToDate = DateTime.Now; //= new DateTime(int.Parse(Model.ToDate) );
            DateTime.TryParseExact(Model.FromDate, "dd/MM/yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out fromDate);
            DateTime.TryParseExact(Model.ToDate, "dd/MM/yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out ToDate);

            //Declare @EmpId int
            //set @EmpId = 7
            //Select projects.empid from projects where id  in (Select Project_id from opr_employee_details where getdate() BETWEEN FromDate and ToDate  and Emp_id = @EmpId )

            var EmployeeMaster = (await (db.GetEmployeeProjectDataAsync(employee.Emp_ID, DateTime.Now.ToSiteTime())))
                .FirstOrDefault();

            var empData = new EmpVacation
            {
                EmployeeName = employee.Emp_Namee ?? employee.Emp_Name,
                EmployeeCode = employee.Emp_Code.ToString(),
                EmployeeId = employee.Emp_ID,
                ChkSallary = Model.withSal == 1,
                EndDate = ToDate,
                StartDate = fromDate,
                Notes = Model.Details,
                Status = null,
                OrderDate = DateTime.Now.ToSiteTime(),
                ManagerId = EmployeeMaster?.manager,
                JobTypeID = EmployeeMaster?.JobId,
                JobTypeName = EmployeeMaster?.JobName,
                project_id = EmployeeMaster?.project,
                ProjectName = EmployeeMaster?.ProjectName,
                IsSickleave = Model.IsSickleave,
                AlterEmp = Model.AlterEmp
            };
            await db.EmpVacations.AddAsync(empData);
            await db.SaveChangesAsync();
            //   var model = await GetManagerTrans();
            var homemodel = new HomeModel();

            homemodel.ManagerTrans = await GetManagerTrans();
            homemodel.MyTransModel = await GetSwapTrans();
            //*********************
            //byte[] attachmentData = File.ReadAllBytes(@"C:\Baraka\1.jpg");
            //string attachmentFileName = "file.pdf";

            
            var swemployee = db.TblEmployees.FirstOrDefault(t => t.Emp_ID == employee.swapedempid);
            var swemployee2 = db.TblEmployees.FirstOrDefault(t => t.Emp_ID == employee.swapedempid2);
            var swemployee2mnger = db.TblEmployees.FirstOrDefault(t => t.Emp_ID == employee.mangerid);
             
            //string toEmail = "samyebid@gmail.com";
            string subject = $"Employee Vacation  {empData.EmployeeName}";
            string textBody = $@"Employee Code {empData.EmployeeCode} EmployeeName:{empData.EmployeeName} Start Date {empData.StartDate:dd-MM-yyyy} End Date {empData.EndDate:dd-MM-yyyy}";
            var lst = new List<string>();
             
           
                if (!string.IsNullOrEmpty(swemployee?.Emp_Mail))
                {
                    await SendEmailWithAttachmentAsyncRakka(swemployee.Emp_Mail, subject, textBody);
                }
                if (!string.IsNullOrEmpty(swemployee2?.Emp_Mail))
                {
                    await SendEmailWithAttachmentAsyncRakka(swemployee2.Emp_Mail, subject, textBody);
                }
                if (!string.IsNullOrEmpty(swemployee2mnger?.Emp_Mail))
                {
                    await SendEmailWithAttachmentAsyncRakka(swemployee2mnger.Emp_Mail, subject, textBody);
                }
            
         
            //*********************
            return View(nameof(Index), homemodel);
        }


        [HttpGet]
        public async Task<ViewResult> Permission()
        {
            var messges = new List<string>();
            var user = await _userManager.GetUserAsync(HttpContext.User);
            var userid = int.Parse(user.UserName.Split("@")[0]);
            //  var dbuser = await db.TblUsers.FirstOrDefaultAsync(t => t.UserID == userid);

            var employee = await db.TblEmployees.FirstOrDefaultAsync(t => t.Emp_ID == userid);

            var model = new PermissionModel();
            model.EmployeeCode = employee.Emp_Code;
            model.EmployeeName = employee?.Emp_Namee ?? employee.Emp_Name;
            model.OrderDate = DateTime.Now.ToSiteTime();
            return View(model);
        }

        [HttpPost]
        public async Task<ViewResult> Permission(PermissionModel Model)
        {
            
             
            var user = await _userManager.GetUserAsync(HttpContext.User);
            var userid = int.Parse(user.UserName.Split("@")[0]);
            // var dbuser = await db.TblUsers.FirstOrDefaultAsync(t => t.UserID == userid);

            var employee = await db.TblEmployees.FirstOrDefaultAsync(t => t.Emp_ID == userid);
            var orderDate = DateTime.Now;
            var startTime = DateTime.Now;
            var endTime = DateTime.Now;




            DateTime.TryParseExact(Model.Date, "dd/MM/yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out orderDate);

            DateTime.TryParseExact(Model.Date + " " + Model.FromTime, "dd/MM/yyyy HH:mm",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out startTime);

            DateTime.TryParseExact(Model.Date + " " + Model.ToTime, "dd/MM/yyyy HH:mm",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out endTime);
            var empMasterDate = (await db.GetEmployeeProjectDataAsync(employee.Emp_ID, DateTime.Now.ToSiteTime()))
                .FirstOrDefault();
            var empData = new tblPermission()
            {
                EmployeeName = employee.Emp_Namee ?? employee.Emp_Name,
                EmployeeCode = employee.Emp_Code.ToString(),
                FromTime = startTime,
                ToTime = endTime,
                EmployeeId = employee.Emp_ID,
                Notes = Model.Notes,
                TransDate = orderDate,
                Status = null,
                OrderDate = DateTime.Now.ToSiteTime(),
                ManagerId = empMasterDate?.manager,
                JobTypeID = empMasterDate?.JobId,
                JobTypeName = empMasterDate?.JobName,
                project_id = empMasterDate?.project,
                ProjectName = empMasterDate?.ProjectName


            };
            await db.tblPermissions.AddAsync(empData);
            await db.SaveChangesAsync();
            var homemodel = new HomeModel();

            homemodel.ManagerTrans = await GetManagerTrans();
            homemodel.MyTransModel = await GetSwapTrans();
            return View(nameof(Index), homemodel);
        }

        //"React(1,@emp.TransType,@emp.OrderId)
        // React(s, t, i) {
        [HttpGet]
        public async Task<IActionResult> React(int s, int t, int i, int l)
        {
            try
            {
                var trans = (TransTypeEnum)t;
                if (trans == TransTypeEnum.Permssion)
                {
                    var per = await db.tblPermissions.FirstOrDefaultAsync(t => t.Id == i);
                    if (per != null)
                    {
                        // per.Status = s;
                        if (l == 1)
                        {
                            per.LevelOne = s;
                        }

                        else if (l == 2)
                        {
                            per.LevelTow = s;
                        }
                        else if (l == 3)
                        {
                            per.LevelThree = s;
                        }

                        await db.SaveChangesAsync();
                    }
                }
                else if (trans == TransTypeEnum.Vaction)
                {
                    var per = await db.EmpVacations.FirstOrDefaultAsync(h => h.Id == i);
                    if (per != null)
                    {
                        // per.Status = s;
                        if (l == 1)
                        {
                            per.LevelOne = s;
                        }

                        else if (l == 2)
                        {
                            per.LevelTow = s;
                        }
                        else if (l == 3)
                        {
                            per.LevelThree = s;
                        }

                        await db.SaveChangesAsync();
                    }
                }
                else if (trans == TransTypeEnum.Materials)
                {
                    var per = await db.tblItemRequests.Where(n => n.OrderNo == i).ToListAsync();
                    foreach (var request in per)
                    {
                        // request.Status = s;
                        if (l == 1)
                        {
                            request.LevelOne = s;
                        }

                        else if (l == 2)
                        {
                            request.LevelTow = s;
                        }
                        else if (l == 3)
                        {
                            request.LevelThree = s;
                        }

                    }

                    await db.SaveChangesAsync();

                }
                else if (trans == TransTypeEnum.Advance)
                {
                    var per = await db.tblAdvances.FirstOrDefaultAsync(h => h.Id == i);
                    if (per != null)
                    {
                        // per.Status = s;
                        if (l == 1)
                        {
                            per.LevelOne = s;
                        }

                        else if (l == 2)
                        {
                            per.LevelTow = s;
                        }
                        else if (l == 3)
                        {
                            per.LevelThree = s;
                        }

                        await db.SaveChangesAsync();
                    }
                }

            }
            catch
            {

            }


            return Ok();
        }


        [HttpGet]
        public async Task<IActionResult> Reactold(int s, int t, int i, int l)
        {
            try
            {
                var trans = (TransTypeEnum)t;
                if (trans == TransTypeEnum.Permssion)
                {
                    var per = await db.tblPermissions.FirstOrDefaultAsync(t => t.Id == i);
                    if (per != null)
                    {
                        per.Status = s;
                        await db.SaveChangesAsync();
                    }
                }
                else if (trans == TransTypeEnum.Vaction)
                {
                    var per = await db.EmpVacations.FirstOrDefaultAsync(h => h.Id == i);
                    if (per != null)
                    {
                        per.Status = s;
                        await db.SaveChangesAsync();
                    }
                }
                else if (trans == TransTypeEnum.Materials)
                {
                    var per = await db.tblItemRequests.Where(n => n.OrderNo == i).ToListAsync();
                    foreach (var request in per)
                    {
                        request.Status = s;

                    }

                    await db.SaveChangesAsync();

                }

            }
            catch
            {

            }


            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> close(int t, int i)
        {
            try
            {
                var trans = (TransTypeEnum)t;
                if (trans == TransTypeEnum.Permssion)
                {
                    var per = await db.tblPermissions.FirstOrDefaultAsync(t => t.Id == i);
                    if (per != null)
                    {
                        per.Status = (int)StatusEnum.Closed;
                        await db.SaveChangesAsync();
                    }
                }
                else if (trans == TransTypeEnum.Vaction)
                {
                    var per = await db.EmpVacations.FirstOrDefaultAsync(h => h.Id == i);
                    if (per != null)
                    {
                        per.Status = (int)StatusEnum.Closed;
                        await db.SaveChangesAsync();
                    }
                }
                else if (trans == TransTypeEnum.Materials)
                {
                    var per = await db.tblItemRequests.Where(n => n.OrderNo == i).ToListAsync();
                    foreach (var request in per)
                    {
                        request.Status = (int)StatusEnum.Closed;

                    }

                    await db.SaveChangesAsync();

                }

            }
            catch
            {

            }


            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> EmployeeTrans()
        {
            var model = new HomeModel();
            // model.MyTransModel = await GetSwapTrans();
            model.EmployeeTrans = await GetEmployeeTrans();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> FinshedTrans()
        {
            var model = new HomeModel();
            //model.MyTransModel = await GetFinshedSwapTrans();
            //model.ManagerTrans = await GetFinshedManagerTrans();

            return View(model);
        }


        [HttpPost]
        public async Task<IActionResult> SaveItemRequest()
        {

            try
            {
                var user = await _userManager.GetUserAsync(HttpContext.User);
                var userid = int.Parse(user.UserName.Split("@")[0]);
                var screens = await db.TblEmployeeScreens.Where(t => t.Emp_ID == userid && t.ScreenName == "FrmPO6")
                    .Select(t => new
                        EmployeeScreenModel()
                        {
                            Emp_ID = t.Emp_ID,
                            CanAdd = t.CanAdd,
                            CanEdit = t.CanEdit,
                            CanShow = t.CanShow,
                            RowID = t.RowID,
                            ScreenName = t.ScreenName

                        }).AsNoTracking().FirstOrDefaultAsync();

                if (!(screens?.CanShow ?? false))
                {
                    return Json("Not Auth");
                }


                var body = await HttpContext.Request.GetRawBodyStringAsync();
                var model = JsonConvert.DeserializeObject<List<OrderLineModel>>(body);

                var OrderNo = 0;
                if (db.tblItemRequests.AsNoTracking().Any())
                {
                    OrderNo = await db.tblItemRequests.AsNoTracking().Select(t => t.OrderNo).MaxAsync(t => t);
                }

                ;
                OrderNo = OrderNo + 1;


                //var dbuser = await db.TblUsers.FirstOrDefaultAsync(t => t.UserID == userid);
                var employee = await db.TblEmployees.FirstOrDefaultAsync(t => t.Emp_ID == userid);
                var orderId = Guid.NewGuid();
                var empMasterData = (await db.GetEmployeeProjectDataAsync(employee.Emp_ID, DateTime.Now.ToSiteTime()))
                    .FirstOrDefault();


                await db.Database.BeginTransactionAsync();
                foreach (var item in model)
                {
                    var unit = await db.TblUnites.Where(t => t.UnitID == item.unitid).Select(t => new
                    {
                        t.UnitNamee
                    }).FirstOrDefaultAsync();
                    var myitem = await db.TblItems.Where(t => t.ItemID == item.ItemId).AsNoTracking().Select(t => new
                    {
                        t.ItemName,
                        t.ItemNamee,
                        t.ItemCode
                    }).FirstOrDefaultAsync();
                    var dbitem = new tblItemRequest()
                    {
                        OrderId = orderId,
                        EmployeeId = employee.Emp_ID,
                        OrderNo = OrderNo,
                        EmployeeCode = employee.Emp_Code,
                        OrderDate = DateTime.Now.ToSiteTime(),
                        Notes = item.Notes,
                        EmployeeName = employee.Emp_Namee,
                        ItemId = item.ItemId,
                        ItemName = myitem.ItemName,
                        ItemCode = myitem.ItemCode,
                        ItemNamee = myitem.ItemNamee,
                        Qty = item.Qty,
                        RowId = Guid.NewGuid(),
                        Status = null,
                        ManagerId = empMasterData?.manager,
                        JobTypeID = empMasterData?.JobId,
                        JobTypeName = empMasterData?.JobName,
                        Project_id = empMasterData?.project,
                        ProjectName = empMasterData?.ProjectName,
                        UnitId = item.unitid,
                        UnitName = unit?.UnitNamee ?? ""
                    };
                    await db.tblItemRequests.AddAsync(dbitem);
                    await db.SaveChangesAsync();
                }

                await db.Database.CommitTransactionAsync();

            }
            catch (Exception ex)
            {
                await db.Database.RollbackTransactionAsync();
                return Json(ex.Message);
            }

            return Json(new { isok = true });
        }

        public async Task<IActionResult> MaterialsRequisition()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            var userid = int.Parse(user.UserName.Split("@")[0]);
            var screens = await db.TblEmployeeScreens.Where(t => t.Emp_ID == userid && t.ScreenName == "FrmPO6").Select(
                t => new
                    EmployeeScreenModel()
                    {
                        Emp_ID = t.Emp_ID,
                        CanAdd = t.CanAdd,
                        CanEdit = t.CanEdit,
                        CanShow = t.CanShow,
                        RowID = t.RowID,
                        ScreenName = t.ScreenName

                    }).AsNoTracking().FirstOrDefaultAsync();
            if (!(screens?.CanShow ?? false))
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }

            var OrderNo = 0;
            if (db.tblItemRequests.AsNoTracking().Any())
            {
                OrderNo = db.tblItemRequests.AsNoTracking().Select(t => t.OrderNo).Max(t => t);
            }

            ;


            var inv = new OrderModel()
            {
                Status = "New",

                OrderDate = DateTime.Now.ToSiteTime().ToString("dd/MM/yyyy"),
                OrderNo = OrderNo + 1

            };
            return View(inv);
        }

        [NonAction]
        private async Task<List<InvoiceViewModel>> getinvoicemodel(Guid orderId)
        {


            var inv = await db.tblItemRequests.Where(t => t.OrderId == orderId).AsNoTracking().Select(t =>
                new InvoiceViewModel
                {
                    EmployeeCode = t.EmployeeCode,
                    EmployeeId = t.EmployeeId ?? 0,
                    EmployeeName = t.EmployeeName,
                    RowId = t.RowId,
                    ItemCode = t.ItemCode,
                    ItemId = t.ItemId,
                    ItemName = t.ItemName,
                    ItemNamee = t.ItemNamee,
                    ManagerId = t.ManagerId ?? 0,
                    Notes = t.Notes,
                    OrderDate = t.OrderDate,
                    OrderId = orderId,
                    OrderNo = t.OrderNo,
                    Qty = t.Qty ?? 1

                }).ToListAsync();
            return inv;





        }

        [HttpPost]
        public async Task<IActionResult> ItemsList(PostModel model)
        {
            try
            {


                int filteredResultsCount;
                int totalResultsCount;



                var searchBy = model.search;
                var take = model.length;
                var skip = model.start;

                string sortBy = "";
                bool sortDir = true;



                model.search ??= new Search
                {
                    value = ""
                };
                if (string.IsNullOrEmpty(model.search.value))
                {
                    model.search.value = "";
                }

                var srch = model.search.value;
                List<int> notwanted = new List<int>() { 2, 3 };
                List<ItemModel> result = new List<ItemModel>();

                var searchafteraedit = ext.PrepareQuery(srch);
                var sortcol = "name";
                var sortorder = "asc";
                if (model.order != null)
                {
                    sortcol = model.columns[model.order[0].column].data;
                    sortorder = model.order[0].dir.ToLower();
                }

                if (take == -1)
                {
                    take = int.MaxValue;
                    skip = 0;

                }

                //   var dbmanger = new dbManager(db.Database.GetDbConnection());

                var res = await db.SpGetitemAsync(searchafteraedit, take, skip, sortcol, sortorder);



                var count = await db.SpGetitemsCountAsync(searchafteraedit);


                filteredResultsCount = count?.FirstOrDefault()?.Column0 ?? 0;

                var querycount = await db.TblItems.CountAsync();

                totalResultsCount = querycount;
                //                var stru = $@"SELECT TblItemsUnits.UnitID,TblItemsUnits.ItemID,
                //       TblUnites.UnitNamee UnitName
                //FROM TblItemsUnits
                //    JOIN TblUnites
                //        ON TblItemsUnits.UnitID = TblUnites.UnitID
                //WHERE ItemID IN ( {string.Join(",", res.Select(t=> t.ItemID) )} );";

                List<int> items = res.Select(t => t.ItemID).ToList();


                var unitesData = await (from un in db.TblItemsUnits
                    join unit in db.TblUnites on un.UnitID equals unit.UnitID into untgrp
                    where items.Contains(un.ItemID)
                    from MyUnit in untgrp.DefaultIfEmpty()
                    select new MyItemUnitModel()
                    {
                        ItemID = un.ItemID,
                        UnitId = un.UnitID,
                        UnitName = MyUnit == null ? "" : MyUnit.UnitNamee,
                        DefaultUnit = un.DefaultUnit == 1,
                        UnitFactor = un.UnitFactor ?? 0m

                    }).ToListAsync();


                result = res.Select(t => new ItemModel()
                {
                    ItemCode = t.ItemCode,
                    ItemId = t.ItemID,
                    ItemName = t.ItemName,
                    ItemNamee = t.ItemNamee,
                    ItemUnites = unitesData.Where(n => n.ItemID == t.ItemID).ToList(),
                    //unitId = unitesData.FirstOrDefault(n => n.ItemID == t.ItemID && n.DefaultUnit)?.UnitId ??
                    //         unitesData.FirstOrDefault()?.UnitId,
                    unitId = unitesData.Where(unit => unit.ItemID == t.ItemID).MaxBy(uni => uni.UnitFactor)?.UnitId
                }).ToList();






                return Json(new
                {

                    draw = model.draw,
                    recordsTotal = totalResultsCount,
                    recordsFiltered = filteredResultsCount,

                    data = result
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {

                    draw = model.draw,
                    recordsTotal = 0,
                    recordsFiltered = 0,


                });
            }

        }
        [HttpGet]
        public async Task<IActionResult> visitrpt()
        {
            if (!CurrentSession.location)
            {
                return NotFound();
            }

            var res = await (from v in db.Visits
                join c in db.Customers on v.CustomerId equals c.CusID
                join tp in db.TblTypeVisits on v.VType equals tp.Id into grp
                from tpl in grp.DefaultIfEmpty()
                select new
                {
                    CustomerCode = c.Fullcode,
                    OrderId = v.OrderId,
                    CusNamee = c.CusNamee,
                    Notes = v.Notes,
                    OrderDate = v.OrderDate,
                    SignInDate = v.SignInDate,
                    SignOutDate = v.SignOutDate,
                    SignInLocation = v.SignInLocation,
                    SignOutLocation = v.SignOutLocation,
                    VisitDate = v.VisitDate,
                    Name = v.Name,
                    VisitDone = v.VisitDone,
                    Contracted = v.Contracted,
                    Canceled = v.Canceled,
                    TypeVisitName = tpl == null ? "" : tpl.namee
                }).ToListAsync();

                 var  result =   res.ToList().Select(t => new VisitDataModel()
            {
                code = t.CustomerCode,
                id = t.OrderId,
                name = t.CusNamee,
                notes = t.Notes,
                orderdate = t.OrderDate.ToString("dd/MM/yyyy"),
                signindate = (t.SignInDate?.ToString("dd/MM/yyyy hh:mm tt")) ?? "",
                signoutdate = (t.SignOutDate?.ToString("dd/MM/yyyy hh:mm tt")) ?? "",
                signinlocation = t.SignInLocation,
                signoutlocation = t.SignOutLocation,
                visitdate = t.VisitDate,
                visitname = t.Name,
                notes2 =
                    $"{((t.VisitDone ?? false) ? "Visit Done" : "")}{((t.Contracted ?? false) ? "-Contracted" : "")}{((t.Canceled ?? false ? "-Canceled" : ""))}",
                type = t.TypeVisitName

            }).ToList();
           return View(result);
        }

        [HttpPost]
        public async Task<IActionResult> VisitList(PostModel model)
        {
            if (!CurrentSession.location)
            {
                return Json(new
                {

                    draw = model.draw,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                });
            }

            try
            {


                int filteredResultsCount;
                int totalResultsCount;



                var searchBy = model.search;
                var take = model.length;
                var skip = model.start;

                string sortBy = "";
                bool sortDir = true;

                model.search ??= new Search
                {
                    value = ""
                };
                if (string.IsNullOrEmpty(model.search.value))
                {
                    model.search.value = "";
                }

                var srch = model.search.value;
                List<int> notwanted = new List<int>() { 2, 3 };
                List<VisitDataModel> result = new List<VisitDataModel>();

                var searchafteraedit = ext.PrepareQuery(srch);
                var sortcol = "name";
                var sortorder = "asc";
                if (model.order != null)
                {
                    sortcol = model.columns[model.order[0].column].data;
                    sortorder = model.order[0].dir.ToLower();
                }

                if (take == -1)
                {
                    take = int.MaxValue;
                    skip = 0;

                }

                var user = await _userManager.GetUserAsync(HttpContext.User);
                var userid = int.Parse(user.UserName.Split("@")[0]);

                var res = await db.sp_getVisiteAsync(searchafteraedit, take, skip, sortcol, sortorder, userid);



                var count = await db.sp_getvisit_CountAsync(searchafteraedit, userid);


                filteredResultsCount = count?.FirstOrDefault()?.column0 ?? 0;

                var querycount = await db.Visits.Where(t => t.EmployeeId == userid).CountAsync();

                totalResultsCount = querycount;

                result = res.ToList().Select(t => new VisitDataModel()
                {
                    code = t.CustomerCode,
                    id = t.OrderId,
                    name = t.CusNamee,
                    notes = t.Notes,
                    orderdate = t.OrderDate.ToString("dd/MM/yyyy"),
                    signindate = (t.SignInDate?.ToString("dd/MM/yyyy hh:mm tt")) ?? "",
                    signoutdate = (t.SignOutDate?.ToString("dd/MM/yyyy hh:mm tt")) ?? "",
                    signinlocation = t.SignInLocation,
                    signoutlocation = t.SignOutLocation,
                    visitdate = t.VisitDate,
                    visitname = t.Name,
                    notes2 =
                        $"{((t.VisitDone ?? false) ? "Visit Done" : "")}{((t.Contracted ?? false) ? "-Contracted" : "")}{((t.Canceled ?? false ? "-Canceled" : ""))}",
                    type = t.TypeVisitName

                }).ToList();






                return Json(new
                {

                    draw = model.draw,
                    recordsTotal = totalResultsCount,
                    recordsFiltered = filteredResultsCount,

                    data = result
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {

                    draw = model.draw,
                    recordsTotal = 0,
                    recordsFiltered = 0,


                });
            }

        }

        //{ id: id, loc: loc, time: time, accuracy: accuracy },
        [HttpGet]
        public async Task<IActionResult> LocLogin(string id, string loc, int time, string accuracy)
        {
            try
            {
                Guid orderid = Guid.Empty;
                Guid.TryParse(id, out orderid);
                var vist = await db.Visits.Where(t => t.OrderId == orderid).FirstOrDefaultAsync();
                vist.SignInLocation = loc;
                vist.SignInDate = DateTime.Now.ToSiteTime();
                vist.accuracy = accuracy;
                vist.timestamp = time;
                await db.SaveChangesAsync();
                return Json(new
                {
                    isok = true
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    isok = false,
                    msg = ex.Message
                });
            }



        }

        [HttpGet]
        public async Task<IActionResult> LocLogout(string id, string loc, int time, string accuracy)
        {
            try
            {
                Guid orderid = Guid.Empty;
                Guid.TryParse(id, out orderid);
                var vist = await db.Visits.Where(t => t.OrderId == orderid).FirstOrDefaultAsync();
                vist.SignOutLocation = loc;
                vist.SignOutDate = DateTime.Now.ToSiteTime();
                vist.accuracy = accuracy;
                vist.timestamp = time;
                await db.SaveChangesAsync();
                return Json(new
                {
                    isok = true
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    isok = false,
                    msg = ex.Message
                });
            }

        }

        [HttpGet]
        public async Task<IActionResult> newvisit(int id, int custid,
            string notes, string name, string date,
            string fromtime, string totime,
            bool visitdone,
            bool canceled,
            bool contracted, int? type)
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            var userid = int.Parse(user.UserName.Split("@")[0]);
            var customer = await db.Customers.Where(t => t.CusID == custid && t.Type == 1).AsNoTracking()
                .FirstOrDefaultAsync();



            var model = new Visit
            {
                EmployeeId = userid,
                CustomerCode = customer.Fullcode,
                CustomerId = custid,
                Notes = notes,
                OrderDate = DateTime.Now.ToSiteTime(),
                OrderId = Guid.NewGuid(),
                Name = name,
                VisitDate = date,
                VisitFromTime = fromtime,
                VisitToTime = totime,
                VisitDone = visitdone,
                Canceled = canceled,
                Contracted = contracted,
                VType = type
            };
            await db.Visits.AddAsync(model);
            await db.SaveChangesAsync();
            return Ok();
        }



        [HttpGet]
        public async Task<IActionResult> editvisit(Guid id, int custid,
            string notes, string name, string date,
            string fromtime, string totime,
            bool visitdone,
            bool canceled,
            bool contracted, int? type)
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            var userid = int.Parse(user.UserName.Split("@")[0]);
            var customer = await db.Customers.Where(t => t.CusID == custid && t.Type == 1).AsNoTracking()
                .FirstOrDefaultAsync();

            var model = await db.Visits.FirstOrDefaultAsync(t => t.OrderId == id);
            if (model == null)
            {
                return Json(new
                {
                    isok = false,
                    msg = "Vist not found"
                });
            }

            if (customer == null)
            {
                return Json(new
                {
                    isok = false,
                    msg = "Customer not found"
                });
            }

            model.EmployeeId = userid;
            model.CustomerCode = customer.Fullcode;
            model.CustomerId = custid;
            model.Notes = notes;
            // model.OrderDate = DateTime.Now.ToSiteTime();

            model.Name = name;
            model.VisitDate = date;
            model.VisitFromTime = fromtime;
            model.VisitToTime = totime;
            model.VisitDone = visitdone;
            model.Canceled = canceled;
            model.Contracted = contracted;
            model.VType = type;
            await db.SaveChangesAsync();
            return Ok();
        }

        public async Task<IActionResult> SearchAutoCompleteCustomer(string word)
        {



            var newword = ext.PrepareQuery(word);

            var mdata = await db.procCustomerAutoCompleateSearchAsync($"%{newword}%");





            return Json(mdata.Select(t => new
            {
                name = t.CusName,
                namee = t.CusNamee,
                code = t.code,
                fullcode = t.Fullcode,
                id = t.CusID
            }));

        }

        public async Task<IActionResult> SearchAutoCompleteEmployee(string word)
        {


            var newword = ext.PrepareQuery(word);
            var mdata = await db.procEmployeeAutoCompleateSearchAsync($"%{newword}%");

            //   List<int> items = mdata.Select(t => t.Emp_ID).ToList();


            //var unitesData = await (from un in db.TblItemsUnits
            //    join unit in db.TblUnites on un.UnitID equals unit.UnitID into untgrp
            //    where items.Contains(un.ItemID)
            //    from MyUnit in untgrp.DefaultIfEmpty()
            //    select new MyItemUnitModel()
            //    {
            //        ItemID = un.ItemID,
            //        UnitId = un.UnitID,
            //        UnitName = MyUnit == null ? "" : MyUnit.UnitNamee,
            //        DefaultUnit = un.DefaultUnit == 1

            //    }).ToListAsync();
            return Json(mdata.Select(t => new
            {
                name = t.Emp_Name,
                namee = t.Emp_Namee,
                id = t.Emp_ID,
                code = t.Emp_Code

            }));

        }

        public async Task<IActionResult> SearchAutoCompleteNew(string word)
        {


            var newword = ext.PrepareQuery(word);
            var mdata = await db.ProcMianAutoCompleateSearchAsync($"%{newword}%");

            List<int> items = mdata.Select(t => t.ItemID).ToList();


            var unitesData = await (from un in db.TblItemsUnits
                join unit in db.TblUnites on un.UnitID equals unit.UnitID into untgrp
                where items.Contains(un.ItemID)
                from MyUnit in untgrp.DefaultIfEmpty()
                select new MyItemUnitModel()
                {
                    ItemID = un.ItemID,
                    UnitId = un.UnitID,
                    UnitName = MyUnit == null ? "" : MyUnit.UnitNamee,
                    DefaultUnit = un.DefaultUnit == 1,
                    UnitFactor = un.UnitFactor??0m

                }).ToListAsync();

            return Json(mdata.Select(t => new ItemModel
            {
                ItemName = t.ItemName,
                ItemCode = t.ItemCode,
                ItemNamee = t.ItemNamee,
                ItemId = t.ItemID,
                ItemUnites = unitesData.Where(n => n.ItemID == t.ItemID).ToList(),
                //unitId = unitesData.FirstOrDefault(n => n.ItemID == t.ItemID && n.DefaultUnit)?.UnitId ??
                //         unitesData.FirstOrDefault()?.UnitId,
                 unitId = unitesData.Where(unit=> unit.ItemID == t.ItemID).MaxBy(uni=>uni.UnitFactor)?.UnitId

            }));

        }

        public async Task<IActionResult> getvisit(Guid id)
        {
            var vist = await (from vis in db.Visits
                join cu in db.Customers on vis.CustomerId equals cu.CusID
                join tbl in db.TblTypeVisits on vis.VType equals tbl.Id into gp
                from type in gp.DefaultIfEmpty()
                where vis.OrderId == id
                select new
                {
                    customername = $"{cu.Fullcode}-{cu.CusNamee}",
                    customerid = cu.CusID,
                    visitname = vis.Name,
                    date = vis.VisitDate,
                    time0 = vis.VisitFromTime,
                    time1 = vis.VisitToTime,
                    notes = vis.Notes,
                    visitdone = vis.VisitDone ?? false,
                    canceled = vis.Canceled ?? false,
                    contracted = vis.Contracted ?? false,
                    id = vis.OrderId,
                    type = vis.VType

                }).FirstOrDefaultAsync();
            if (vist == null)
            {
                return Json(new
                {
                    isok = false,
                    msg = "not found"
                });
            }

            return Json(new
            {
                isok = true,
                msg = "",
                data = vist
            });
        }


        public async Task<IActionResult> Visit(Guid? orderId = null)
        {
            if (!CurrentSession.location)
            {
                return NotFound();
            }

            var types = await db.TblTypeVisits.AsNoTracking().ToListAsync();
            ViewBag.VType = types;
            return View();
        }

        public async Task<IActionResult> advance(Guid? orderId = null)
        {
            //if (!CurrentSession.location)
            //{
            //    return NotFound();
            //}
            var user = await _userManager.GetUserAsync(HttpContext.User);
            var userid = int.Parse(user.UserName.Split("@")[0]);

            var employee = await db.TblEmployees.FirstOrDefaultAsync(t => t.Emp_ID == userid);
            var empMasterDate = (await db.GetEmployeeProjectDataAsync(employee.Emp_ID, DateTime.Now.ToSiteTime()))
                .FirstOrDefault();
            var mnngerid = empMasterDate?.manager;
            var mnger = await db.TblEmployees.FirstOrDefaultAsync(t => mnngerid == t.Emp_ID );
            var model = new AdvanceModel()
            {
                EmployeeName = $"{employee.Emp_Code}-{employee.Emp_Namee}",
                MangerName = $"{mnger?.Emp_Code}-{mnger?.Emp_Namee}"
            };

            return View(model);
        }



        [HttpPost]
        public async Task<ViewResult> advance(AdvanceModel Model)
        {
          
            var user = await _userManager.GetUserAsync(HttpContext.User);
            var userid = int.Parse(user.UserName.Split("@")[0]);
 
            var employee = await db.TblEmployees.FirstOrDefaultAsync(t => t.Emp_ID == userid);
            var empMasterDate = (await db.GetEmployeeProjectDataAsync(employee.Emp_ID, DateTime.Now.ToSiteTime()))
                .FirstOrDefault();
            var mnngerid = empMasterDate?.manager;
            var mnger = await db.TblEmployees.FirstOrDefaultAsync(t => mnngerid == t.Emp_ID);


            var empData = new tblAdvance()
            {
                OrderId  =  Guid.NewGuid(),
                EmpId = employee.Emp_ID,
                Code  = employee.Emp_Code,
                Emp1 = Model.Emp1,
                Emp2 = Model.Emp1,
                Amount = Model.Amount,
                OrderDate = DateTime.Now.ToSiteTime(),
                ManagerId = empMasterDate?.manager,
                JobTypeID = empMasterDate?.JobId,
                JobTypeName = empMasterDate?.JobName,
                project_id = empMasterDate?.project,
                ProjectName = empMasterDate?.ProjectName,
                Notes = Model.Notes

            };
            await db.tblAdvances.AddAsync(empData);
            await db.SaveChangesAsync();
            //   var model = await GetManagerTrans();
            var homemodel = new HomeModel();

            homemodel.ManagerTrans = await GetManagerTrans();
            homemodel.MyTransModel = await GetSwapTrans();
            return View(nameof(Index), homemodel);
        }






        public async Task SendEmailWithAttachmentAsyncRakka(string  toEmail, string subject, string textBody   )
        {
            string MailgunDomain = "rakaagroup.com";
            string MailgunApiKey = "82f059fa95e58d4ef8c5ad0138f4fa29-623e10c8-94a9b7a2";
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri($"https://api.mailgun.net/v3/{MailgunDomain}");
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"api:{MailgunApiKey}")));
                //foreach (var tostr in toEmail)
                //{
                    var formData = new MultipartFormDataContent();
                    formData.Add(new StringContent("Rakaa Group  <Admin@rakaagroup.com>"), "from");
                    formData.Add(new StringContent(toEmail), "to");
                    formData.Add(new StringContent(subject), "subject");
                    formData.Add(new StringContent(textBody), "text");
                    //if (attachmentData != null)
                    //{
                    //    formData.Add(new ByteArrayContent(attachmentData), "attachment", attachmentFileName);

                    //}
                    var response = await httpClient.PostAsync($"/v3/{MailgunDomain}/messages", formData);
               // }
               
 

                //if (!response.IsSuccessStatusCode)
                //{
                //    var responseContent = await response.Content.ReadAsStringAsync();
                //    throw new HttpRequestException($"Mailgun request failed: {response.StatusCode} - {responseContent}");
                //}
            }
        }
    }
}


