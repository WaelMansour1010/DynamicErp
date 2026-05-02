using System.Data;
using System.Linq.Expressions;
using DevExpress.CodeParser;
using DevExpress.Pdf.Native.BouncyCastle.Ocsp;
using DevExpress.XtraRichEdit.Model;
using EazyCash.Auth;
using HROnlineModel;
using HRServices.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using DevExpress.XtraRichEdit.Import.Doc;
using EazyCash;
using EazyCash.Data;
using EazyCash.Models;
using Newtonsoft.Json;
using System.Diagnostics.Contracts;


namespace EmployeeService.Controllers
{
    [Route("api/empdata")]
    [ApiController]
    public class EmployeeDataController : ControllerBase
    {
        private readonly AutoMapper.IMapper mapper;
        private readonly HROnlineModel.HROnlineModel db;
        private readonly Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> _userManager;
        public EmployeeDataController(AutoMapper.IMapper _mapper, HROnlineModel.HROnlineModel _db, Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager)
        {
            db = _db;
            _userManager = userManager;
            mapper = _mapper;
        }
        [HttpGet("getdata")]
        public async Task<IActionResult> GetSavedData()
        {
            try
            {

                var stauts = (int)StatusEnum.Closed;

                var q = from vac in db.EmpVacations
                    join emp in db.TblEmployees on vac.EmployeeId equals emp.Emp_ID
                    where (vac.LevelOne == 1 || (emp.mangerid ?? 0) == 0) &&
                          (vac.LevelTow == 1 || (emp.swapedempid ?? 0) == 0) &&
                          (vac.LevelThree == 1 || (emp.swapedempid2 ?? 0) == 0)
                          && !(vac.IsSickleave??false)
                    select new  
                    {
                        
                            Id = vac.Id,
                            EmployeeCode = vac.EmployeeCode,
                            EmployeeName = vac.EmployeeName,
                            StartDate = vac.StartDate,
                            EndDate = vac.EndDate,
                            ChkSallary = vac.ChkSallary,
                            Notes = vac.Notes,
                            OrderNo = vac.OrderNo,
                            rowId = vac.RowId.ToString("D"),
                            AlterEmployee = vac.AlterEmp??0
                        };

                //var tbl = await db.EmpVacations.AsNoTracking().Where(t => t.LevelOne == 1 && 
                //                                                          t.LevelTow == 1 && t.LevelThree == 1 ).Select(t => new
                //{
                //    Id = t.Id,
                //    EmployeeCode = t.EmployeeCode,
                //    EmployeeName = t.EmployeeName,
                //    StartDate = t.StartDate,
                //    EndDate = t.EndDate,
                //    ChkSallary = t.ChkSallary,
                //    Notes = t.Notes,
                //    OrderNo = t.OrderNo,
                //    rowId = t.RowId.ToString("D")
                //}).ToListAsync();
                var tbl = await q.AsNoTracking().Distinct().ToListAsync();
                return Ok(tbl);


            }
            catch (Exception ex)
            {
                return Ok(ex.Message);
            }


        }

        [HttpGet("getsickleave")]
        public async Task<IActionResult> GetSickleave()
        {
            try
            {

                var stauts = (int)StatusEnum.Closed;

                var q = from vac in db.EmpVacations
                        join emp in db.TblEmployees on vac.EmployeeId equals emp.Emp_ID
                        where (vac.LevelOne == 1 || (emp.mangerid ?? 0) == 0) &&
                              (vac.LevelTow == 1 || (emp.swapedempid ?? 0) == 0) &&
                              (vac.LevelThree == 1 || (emp.swapedempid2 ?? 0) == 0)
                              && (vac.IsSickleave ?? false)
                        select new
                        {
                            Id = vac.Id,
                            EmployeeCode = vac.EmployeeCode,
                            EmployeeName = vac.EmployeeName,
                            StartDate = vac.StartDate,
                            EndDate = vac.EndDate,
                            ChkSallary = vac.ChkSallary,
                            Notes = vac.Notes,
                            OrderNo = vac.OrderNo,
                            AlterEmployee = vac.AlterEmp??0,
                            rowId = vac.RowId.ToString("D")
                        };

                //var tbl = await db.EmpVacations.AsNoTracking().Where(t => t.LevelOne == 1 && 
                //                                                          t.LevelTow == 1 && t.LevelThree == 1 ).Select(t => new
                //{
                //    Id = t.Id,
                //    EmployeeCode = t.EmployeeCode,
                //    EmployeeName = t.EmployeeName,
                //    StartDate = t.StartDate,
                //    EndDate = t.EndDate,
                //    ChkSallary = t.ChkSallary,
                //    Notes = t.Notes,
                //    OrderNo = t.OrderNo,
                //    rowId = t.RowId.ToString("D")
                //}).ToListAsync();
                var tbl = await q.AsNoTracking().Distinct().ToListAsync();
                return Ok(tbl);


            }
            catch (Exception ex)
            {
                return Ok(ex.Message);
            }


        }

        [HttpGet("getvisit")]
        public async Task<IActionResult> GetvisitData()
        {
            try
            {
                var stauts = (int)StatusEnum.Closed;

                var q = from vac in db.Visits
                    join emp in db.TblEmployees on vac.EmployeeId equals emp.Emp_ID
                    join  Cust in db.Customers on vac.CustomerId equals Cust.CusID
                    
                    select new  
                    {
                        visit = vac,
                        emp,Cust
                    };

                return Ok(q.AsNoTracking().ToList().Select(t => new
                {
                    customercode = t.visit.CustomerCode,
                    customerid = t.visit.CustomerId,
                    employeeid = t.visit.EmployeeId,
                    customername = t.Cust.CusNamee,
                    employeename = t.emp.Emp_Namee,
                    date = t.visit.OrderDate.ToString("dd/MM/yyyy"),
                    singintime = t.visit.SignInDate.HasValue ? t.visit.SignInDate.Value.ToString("dd/MM/yyyy HH:mm") : "",
                    signouttime = t.visit.SignOutDate.HasValue ? t.visit.SignOutDate.Value.ToString("dd/MM/yyyy HH:mm") : "",
                    notes = t.visit.Notes,
                    signinlocation = t.visit.SignInLocation??"",
                    signoutlocation = t.visit.SignOutLocation??"",
                    rowId = t.visit.OrderId.ToString("D"),
                    name  = t.visit.Name,
                    visitdate = t.visit.VisitDate,
                    time1=t.visit.VisitFromTime,
                    time2=t.visit.VisitToTime,
                    canceled =  t.visit.Canceled??false,
                    visitDone = t.visit.VisitDone??false ,
                    contracted =  t.visit.Contracted??false,
                    type=t.visit.VType??0
                }));


            }
            catch (Exception ex)
            {
                return Ok(ex.Message);
            }


        }

        [HttpGet("getperdata")]
        public async Task<IActionResult> GetperData()
        {
            try
            {
                var stauts = (int)StatusEnum.Closed;

                var q = from vac in db.tblPermissions
                    join emp in db.TblEmployees on vac.EmployeeId equals emp.Emp_ID
                    where (vac.LevelOne == 1 || (emp.mangerid ?? 0) == 0) &&
                          (vac.LevelTow == 1 || (emp.swapedempid ?? 0) == 0) &&
                          (vac.LevelThree == 1 || (emp.swapedempid2 ?? 0) == 0)
                    select vac;

                return Ok(q.AsNoTracking().ToList().Select(t => new
                {
                    Code = t.EmployeeCode,
                    Id = t.EmployeeId,
                    Name = t.EmployeeName,
                    Date = t.TransDate.ToString("dd/MM/yyyy"),
                    FromTime = t.FromTime.HasValue ? t.FromTime.Value.ToString("dd/MM/yyyy HH:mm") : "",
                    ToTime = t.ToTime.HasValue ? t.ToTime.Value.ToString("dd/MM/yyyy HH:mm") : "",
                    Notes = t.Notes,
                    OrderNo = t.Id,
                    rowId = t.RowId.ToString("D")
                }));


            }
            catch (Exception ex)
            {
                return Ok(ex.Message);
            }


        }


        [HttpGet("getadvdata")]
        public async Task<IActionResult> GetadvData()
        {
            try
            {
              

                var q = from vac in db.tblAdvances
                    join emp in db.TblEmployees on vac.EmpId equals emp.Emp_ID
                    where (vac.LevelOne == 1 || (emp.mangerid ?? 0) == 0) &&
                          (vac.LevelTow == 1 || (emp.swapedempid ?? 0) == 0) &&
                          (vac.LevelThree == 1 || (emp.swapedempid2 ?? 0) == 0)
                    select vac;

                return Ok(q.AsNoTracking().ToList().Select(t => new
                {
                    EmployeeCode = t.Code,
                    EmployeeId = t.EmpId,
                    Emp1 = t.Emp1,
                    Emp2 = t.Emp2,
                    Amount = t.Amount,
                    OrderDate = t.OrderDate.ToString("dd/MM/yyyy"),
                    Notes = t.Notes,
                    OrderNo = t.Id,
                    rowId = t.OrderId.ToString("D"),

                }));


            }
            catch (Exception ex)
            {
                return Ok(ex.Message);
            }


        }
        [HttpGet("approve")]
        public async Task<IActionResult> ApproveTrans(string orderNo, string transType)
        {
            var id = 0;
            int.TryParse(orderNo, out id);
            try
            {



                if (transType == "vac")
                {
                    var vacTrans = await db.EmpVacations.FirstOrDefaultAsync(t => t.Id == id);
                    if (vacTrans != null)
                    {
                        if (vacTrans.Status == (int)StatusEnum.Approved)
                        {
                            return Ok($"Error trans No: {vacTrans.Id} allready  approved ");
                        }
                        else if ((vacTrans.LevelOne != 1 && vacTrans.LevelTow != 1 && vacTrans.LevelThree != 1 ))
                        {
                            return Ok($"Error trans No: {vacTrans.Id} not accepted  can't approved ");
                        }
                        //else if ((vacTrans.Status ?? 0) == (int)StatusEnum.Rejected)
                        //{
                        //    return Ok($"Error  trans No: {vacTrans.Id}   Rejected can't approved ");
                        //}

                        //else if ((vacTrans.Status ?? 0) == (int)StatusEnum.Closed)
                        //{
                        //    return Ok($"Error  trans No: {vacTrans.Id}   allready  closed  ");
                        //}
                        vacTrans.Status = (int)StatusEnum.Approved;
                        await db.SaveChangesAsync();
                        return Ok($"Vacation No: {vacTrans.Id} Employee Name: {vacTrans.EmployeeName} approved successfully");
                    }
                    else
                    {
                        return Ok("Error vacation not found");
                    }

                }
                else if (transType == "per")
                {
                    var perTrans = await db.tblPermissions.FirstOrDefaultAsync(t => t.Id == id);
                    if (perTrans != null)
                    {
                        if (perTrans.Status == (int)StatusEnum.Approved)
                        {
                            return Ok($"Error trans No: {perTrans.Id} allready  approved ");
                        }
                        else if ((perTrans.LevelOne != 1 && perTrans.LevelTow != 1 && perTrans.LevelThree != 1))
                        {
                            return Ok($"Error trans No: {perTrans.Id} not accepted  can't approved ");
                        }
                        //else if ((perTrans.Status ?? 0) == (int)StatusEnum.Pinding)
                        //{
                        //    return Ok($"Error trans No: {perTrans.Id} still pinding can't approved ");
                        //}
                        //else if ((perTrans.Status ?? 0) == (int)StatusEnum.Rejected)
                        //{
                        //    return Ok($"Error  trans No: {perTrans.Id}   Rejected can't approved ");
                        //}
                        //else if (( perTrans.Status ?? 0) == (int)StatusEnum.Closed)
                        //{
                        //    return Ok($"Error  trans No: { perTrans.Id}   allready  closed  ");
                        //}

                        perTrans.Status = (int)StatusEnum.Approved;
                        await db.SaveChangesAsync();
                        return Ok($"Permission No: {perTrans.Id} Employee Name: {perTrans.EmployeeName} approved successfully");
                    }
                    else
                    {
                        return Ok("Error Permission not found");
                    }
                }
                else if (transType == "itemReq")
                {
                    var itemReq = await db.tblItemRequests.Where(t => t.OrderNo == id).ToListAsync();
                    if (itemReq.Any())
                    {
                        if (itemReq.First().Status == (int)StatusEnum.Approved)
                        {
                            return Ok($"Error trans No: {itemReq.First().OrderNo} allready  approved ");
                        }
                        else if ((itemReq.First().LevelOne != 1 && itemReq.First().LevelTow != 1 && itemReq.First().LevelThree != 1))
                        {
                            return Ok($"Error trans No: {itemReq.First().OrderNo} not accepted  can't approved ");
                        }
                        //else if ((itemReq.First().Status ?? 0) == (int)StatusEnum.Pinding)
                        //{
                        //    return Ok($"Error trans No: {itemReq.First().OrderNo} still pinding can't approved ");
                        //}

                        //else if ((itemReq.First().Status ?? 0) == (int)StatusEnum.Rejected)
                        //{
                        //    return Ok($"Error  trans No: {itemReq.First().OrderNo}   Rejected can't approved ");
                        //}

                        //else  if ((itemReq.First().Status ?? 0) == (int)StatusEnum.Closed)
                        //  {
                        //      return Ok($"Error  trans No: {itemReq.First().OrderNo}   allready  closed ");
                        //  }


                        foreach (var item in itemReq)
                        {
                            item.Status = (int)StatusEnum.Approved;
                        }

                        await db.SaveChangesAsync();
                        return Ok($"Materials Requisition No: {itemReq.First().OrderNo} Employee Name: {itemReq.First().EmployeeName} approved successfully");
                    }
                    else
                    {
                        return Ok("Error Materials Requisition not found");
                    }
                }
            }
            catch (Exception ex)
            {
                return Ok($"Error :{ex.Message}");
            }
            return Ok($"Error:No thing Approved");
        }

        [HttpGet("getitemreqdata")]
        public async Task<IActionResult> GetItemreData()
        {
            try
            {
                // var status = (int)StatusEnum.Closed;
                //var itemReq = await db.tblItemRequests.Where(t => t.LevelOne == 1 && t.LevelTow == 1 && t.LevelThree == 1 ).AsNoTracking().GroupBy(t => t.OrderId)
                //  .ToListAsync();
                var q =await  (from vac in db.tblItemRequests
                        join emp in db.TblEmployees on vac.EmployeeId equals emp.Emp_ID
                    where (vac.LevelOne == 1 || (emp.mangerid ?? 0) == 0) &&
                          (vac.LevelTow == 1 || (emp.swapedempid ?? 0) == 0) &&
                          (vac.LevelThree == 1 || (emp.swapedempid2 ?? 0) == 0)
                    select new
                    {
                        vac,emp.Emp_Namee
                    }).ToListAsync();


                var retdata = (from itm in q
                    group itm by itm.vac.OrderId
                    into itmgrp
                    let myitem = itmgrp.First().vac
                    let empName = itmgrp.First().Emp_Namee
                    select new
                    {
                        Code = myitem.EmployeeCode,
                        Id = myitem.EmployeeId,
                        Name = empName,
                        Date = (myitem.OrderDate ?? DateTime.Now).ToString("dd/MM/yyyy"),
                        OrderNo = myitem.OrderNo,
                        OrderId = myitem.OrderId.ToString("D"),
                        Items = string.Join(",", itmgrp.Select(item => $"{item.vac.ItemId}|{item.vac.Qty ?? 0}|{item.vac.UnitId}")),
                        rowId = myitem.OrderId.ToString("D"),
                        projectId = myitem.Project_id
                    }).ToList();
                return Ok(retdata);
                //return Ok(q.ToList().GroupBy(t => t.OrderId).Select(t => new
                //{
                //    Code = t.First().EmployeeCode,
                //    Id = t.First().EmployeeId,
                //    Name = t.First().EmployeeName,
                //    Date = (t.First().OrderDate ?? DateTime.Now).ToString("dd/MM/yyyy"),
                //    OrderNo = t.First().OrderNo,
                //    OrderId = t.First().OrderId.ToString("D"),
                //    Items = string.Join(",", t.Select(item => $"{item.ItemId}|{item.Qty ?? 0}|{item.UnitId}")),
                //    rowId = t.First().OrderId.ToString("D"),



                //}).ToList());


            }
            catch (Exception ex)
            {
                return Ok(ex.Message);
            }


        }


        [HttpPost("uploaditems")]
        public async Task<IActionResult> UploadItems(List<ItemModel> model)
        {
            try
            {
                foreach (var item in model)
                {
                    var name = item.ItemName + "";
                    if (name.Length > 100)
                    {
                        name = (item.ItemName + "").Substring(0, 100);
                    }
                    var namee = item.ItemNamee + "";
                    if (namee.Length > 100)
                    {
                        namee = (item.ItemNamee + "").Substring(0, 100);
                    }


                    var dbitem = await db.TblItems.FirstOrDefaultAsync(t => t.ItemID == item.ItemId);
                    if (dbitem == null)
                    {
                        dbitem = new TblItem()
                        {
                            ItemID = item.ItemId,
                            ItemCode = item.ItemCode,
                            ItemName = name,
                            ItemNamee = namee
                        };
                        await db.TblItems.AddAsync(dbitem);
                        await db.SaveChangesAsync();

                    }
                    else
                    {
                        dbitem.ItemCode = item.ItemCode;
                        dbitem.ItemName = name;
                        dbitem.ItemNamee = namee;
                        await db.SaveChangesAsync();


                    }
                }

                return Ok(new
                {
                    isok = true
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    isok = false,
                    msg = GetExceptionMessages(ex)
                });
            }




        }

        [HttpPost("uploadunits")]
        public async Task<IActionResult> UploadUnites(DataUnitesModel model)
        {
            try
            {

              
                foreach (var item in model.Unites)
                {


                    var dbitem = await db.TblUnites.FirstOrDefaultAsync(t => t.UnitID == item.UnitID  );
                    if (dbitem == null)
                    {
                       
                        dbitem = new TblUnite()
                        {
                            UnitID    = item.UnitID,
                            UnitName     = item.UnitName ,
                            UnitNamee     = item.UnitNamee,
                            SessionCode   = item.SessionCode,
                            QRCODE    = item.QRCODE,
                            HaveWeight   = item.HaveWeight 
                        };
                        await db.TblUnites.AddAsync(dbitem);
                        await db.SaveChangesAsync();

                    }
                    else
                    {
                      // dbitem. UnitID = item.UnitID,
                      dbitem.UnitName = item.UnitName;
                      dbitem.UnitNamee = item.UnitNamee;
                      dbitem.SessionCode = item.SessionCode;
                      dbitem.QRCODE = item.QRCODE;
                      dbitem.HaveWeight = item.HaveWeight;
                        await db.SaveChangesAsync();


                    }
                }

                foreach (var item in model.ItemUnites )
                {
                     
 
                    var dbitem = await db.TblItemsUnits.FirstOrDefaultAsync(t => t.ItemID == item.ItemID && t.UnitID == item.UnitID);
                    if (dbitem == null)
                    {
                        if ((item.ItemID ?? 0) == 0 || (item.UnitID ?? 0) == 0)
                        {
                            continue;
                        }
                        dbitem = new TblItemsUnit()
                        {
                            JunckID      = item.JunckID,
                            ItemID    = item.ItemID??0,
                            UnitID  = item.UnitID??0,
                            UnitFactor    = item.UnitFactor,
                            SecOrder      = item.SecOrder,
                            DefaultUnit   = item.DefaultUnit,
                            UnitSalesPrice    = item.UnitSalesPrice,
                            UnitPurPrice     = item.UnitPurPrice,
                            FactorByDefaultUnit   = item.FactorByDefaultUnit,
                            FactorBySmallUnit   = item.FactorBySmallUnit,
                            MinSelingPrice    = item.MinSelingPrice,
                            ForUnit   = item.ForUnit ,
                            MethodCalc   = item.MethodCalc,
                            PartItemQty =  item.PartItemQty, 
                            SessionCode   = item.SessionCode,
                            barCodeNo2  = item.barCodeNo2,  
                            UnitWholeSalePrice   = item.UnitWholeSalePrice,
                            OldUnitSalesPrice    = item.OldUnitSalesPrice,
                            OldUnitWholeSalePrice     = item.OldUnitWholeSalePrice,
                            MaxSelingPrice    = item.MaxSelingPrice,
                            SelingPriceDestr   =   item.SelingPriceDestr
                        };
                        await db.TblItemsUnits.AddAsync(dbitem);
                        await db.SaveChangesAsync();

                    }
                    else
                    {
                        dbitem.JunckID = item.JunckID;
                  //dbitem.ItemID = item.ItemID ?? 0,
                 //dbitem.   UnitID = item.UnitID ?? 0,
                 dbitem.UnitFactor = item.UnitFactor;
                 dbitem.SecOrder = item.SecOrder;
                 dbitem.DefaultUnit = item.DefaultUnit;
                 dbitem.UnitSalesPrice = item.UnitSalesPrice;
                 dbitem.UnitPurPrice = item.UnitPurPrice;
                 dbitem.FactorByDefaultUnit = item.FactorByDefaultUnit;
                 dbitem.FactorBySmallUnit = item.FactorBySmallUnit;
                 dbitem.MinSelingPrice = item.MinSelingPrice;
                 dbitem.ForUnit = item.ForUnit;
                 dbitem.MethodCalc = item.MethodCalc;
                 dbitem.PartItemQty = item.PartItemQty;
                 dbitem.SessionCode = item.SessionCode;
                 dbitem.barCodeNo2 = item.barCodeNo2;
                 dbitem.UnitWholeSalePrice = item.UnitWholeSalePrice;
                 dbitem.OldUnitSalesPrice = item.OldUnitSalesPrice;
                 dbitem.OldUnitWholeSalePrice = item.OldUnitWholeSalePrice;
                 dbitem.MaxSelingPrice = item.MaxSelingPrice;
                 dbitem.SelingPriceDestr = item.SelingPriceDestr;
                 await db.SaveChangesAsync();


                    }
                }

           

                return Ok(new
                {
                    isok = true
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    isok = false,
                    msg = GetExceptionMessages(ex)
                });
            }




        }


       


        [HttpPost("uploadprojects")]
        public async Task<IActionResult> UploadProjects()
        {
            try
            {


                var body = await HttpContext.Request.GetBodyStringAsync();

                EmployeeProjectModelData model = JsonConvert.DeserializeObject<EmployeeProjectModelData>(body);
                if (model?.ProjectData != null)
                {
                    if (model?.ProjectData?.Any() ?? false)
                    {
                        foreach (var item in model.ProjectData)
                        {
                            var dbitem = await db.projects.FirstOrDefaultAsync(t => t.id == item.id);
                            if (dbitem == null)
                            {
                                dbitem = new project()
                                {
                                    id = item.id
                                };
                                await db.projects.AddAsync(dbitem);
                            }
                            mapper.Map(item, dbitem);

                            await db.SaveChangesAsync();
                        }


                    }

                    if (model?.oprData?.Any() ?? false)
                    {
                        foreach (var item in model.oprData)
                        {
                            var dbitem = await db.opr_employee_details.FirstOrDefaultAsync(t => t.id == item.id);
                            if (dbitem == null)
                            {
                                dbitem = new opr_employee_detail()
                                {
                                    id = item.id
                                };

                                await db.opr_employee_details.AddAsync(dbitem);
                                await db.SaveChangesAsync();

                            }
                            mapper.Map(item, dbitem);
                            await db.SaveChangesAsync();
                        }


                    }
                }




                return Ok(new
                {
                    isok = true
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    isok = false,
                    msg = GetExceptionMessages(ex)
                });
            }




        }
        [NonAction]
        private string GetExceptionMessages(Exception e, string msgs = "")
        {
            if (e == null) return string.Empty;
            if (msgs == "") msgs = e.Message;
            if (e.InnerException != null)
                msgs += "\r\nInnerException: " + GetExceptionMessages(e.InnerException);
            return msgs;
        }
        //[HttpPost("uploadusers")]
        //public async Task<IActionResult> UploadUsers(List<VBUserModel> model)
        //{
        //    try
        //    {
        //        foreach (var user in model)
        //        {
        //            string userName = $"{user.UserID}@page.com";
        //            var dbUser = await db.TblUsers.FirstOrDefaultAsync(t => t.UserID == user.UserID);
        //            if (dbUser == null)
        //            {
        //                dbUser = new TblUser
        //                {
        //                    UserID = user.UserID,
        //                    UserName = user.UserName,
        //                    PassWord = user.PassWord,
        //                    Empid = user.Empid,
        //                    IsActive = user.IsActive
        //                };
        //                db.TblUsers.Add(dbUser);
        //                await db.SaveChangesAsync();
        //                var muser = new ApplicationUser { UserName = userName, Email = userName, IsAdmin = false, IsActive = user.IsActive == 1 };

        //                var result = await _userManager.CreateAsync(muser, user.PassWord);
        //            }
        //            else
        //            {



        //                dbUser.UserName = user.UserName;
        //                dbUser.PassWord = user.PassWord;
        //                dbUser.Empid = user.Empid;
        //                dbUser.IsActive = user.IsActive;
        //                await db.SaveChangesAsync();

        //                var muser = await _userManager.FindByNameAsync(userName);
        //                if (muser != null)
        //                {
        //                    await _userManager.RemovePasswordAsync(muser);
        //                    await _userManager.AddPasswordAsync(muser, user.PassWord);
        //                }
        //                else
        //                {
        //                    var nuser = new ApplicationUser { UserName = userName, Email = userName, IsAdmin = false, IsActive = user.IsActive == 1 };
        //                    var result = await _userManager.CreateAsync(nuser, user.PassWord);
        //                }
        //            }
        //        }

        //        return Ok("Users Updated");
        //    }
        //    catch (Exception ex)
        //    {
        //        return Ok("Error " + ex.Message);
        //    }




        //}

     [HttpPost("uploadjobs")]
        public async Task<IActionResult> UploadJobs(List<JobModel> model)
        {
            try
            {
                foreach (var emp in model)
                {

                   
                    var dbEmp = await db.TblEmpJobsTypes.FirstOrDefaultAsync(t => t.JobTypeID == emp.JobTypeID);
               

                 

                   
                    if (dbEmp == null)
                    {
                        dbEmp = new TblEmpJobsType()
                        {
                           JobTypeID = emp.JobTypeID,
                           JobTypeNamee = emp.JobTypeNamee,
                           JobTypeName = emp.JobTypeName,
                           VisaCode = emp.VisaCode 
                            
                        };
                        db.TblEmpJobsTypes.Add(dbEmp);
                        await db.SaveChangesAsync();
                    }
                    else
                    {

                        dbEmp.JobTypeNamee = emp.JobTypeNamee;
                        dbEmp.JobTypeName = emp.JobTypeName;
                        dbEmp.VisaCode = emp.VisaCode;
                        await db.SaveChangesAsync();

                    }
                }

                return Ok(new
                {
                    isok = true

                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    isok = false,
                    msg = GetExceptionMessages(ex)
                });
            }




        }

        [HttpPost("uploademps")]
        public async Task<IActionResult> UploadEmployees(List<EmployeeModel> model)
        {
            try
            {
                foreach (var emp in model)
                {

                    var EmpCode = emp.Emp_Code + "";
                    if (EmpCode.Length > 50)
                    {
                        EmpCode = (EmpCode + "").Substring(0, 50);
                    }
                    var Fullcode = emp.FullCode + "";
                    if (Fullcode.Length > 255)
                    {
                        Fullcode = (Fullcode + "").Substring(0, 255);
                    }
                    var EmpName = emp.Emp_Name + "";
                    if (EmpName.Length > 255)
                    {
                        EmpName = (EmpName + "").Substring(0, 255);
                    }
                    var EmpNamee = emp.EmpNamee;
                    if (EmpNamee.Length > 255)
                    {
                        EmpNamee = (EmpNamee + "").Substring(0, 255);
                    }
                    var dbEmp = await db.TblEmployees.FirstOrDefaultAsync(t => t.Emp_ID == emp.Emp_ID);
                    var password = "";

                    string userName = $"{emp.Emp_ID}@page.com";

                    if (string.IsNullOrEmpty(emp.Password))
                    {
                        var t = emp.FullCode ?? EmpCode;

                        if (t.Length > 4)
                        {
                            password = t.Substring(t.Length - 4);
                        }
                        else
                        {
                            password = t;
                        }
                    }
                    else
                    {
                        password = emp.Password;
                    }
                    if (dbEmp == null)
                    {
                        dbEmp = new TblEmployee()
                        {
                            Emp_ID = emp.Emp_ID,
                            Emp_Code = EmpCode,
                            Fullcode = Fullcode,
                            Emp_Name = EmpName,
                            Emp_Namee = EmpNamee,
                            Sex = emp.Sex,
                            PassWord = password,
                            mangerid = emp.MangerId,
                            swapedempid = emp.swapedempid,
                            swapedempid2 = emp.swapedempid2,
                            project_id  = emp.project_id,
                            JobTypeID = emp.JobTypeID,
                            balanceH3 = emp.balanceH3,
                            balanceH1 = emp.balanceH1,
                            balanceH2 = emp.balanceH2,
                            balanceH4 = emp.balanceH4,
    Emp_Mail = emp.EmpMail,
                            Emp_Phone = emp.Emp_Phone,
                            Emp_mobile = emp.Emp_mobile

                        };
                        db.TblEmployees.Add(dbEmp);
                        await db.SaveChangesAsync();
                        var muser = new ApplicationUser { UserName = userName, Email = userName, IsAdmin = false, IsActive = true };

                        var result = await _userManager.CreateAsync(muser, password);
                    }
                    else
                    {

                        dbEmp.Emp_Code = EmpCode;
                        dbEmp.Emp_Namee = EmpName;
                        dbEmp.Emp_Namee = EmpNamee;
                        dbEmp.Sex = emp.Sex;
                        dbEmp.mangerid = emp.MangerId;
                        dbEmp.PassWord = password;
                        dbEmp.Fullcode = Fullcode;
                        dbEmp.swapedempid = emp.swapedempid;
                        dbEmp.swapedempid2 = emp.swapedempid2;
                        dbEmp.project_id = emp.project_id;
                        dbEmp.JobTypeID = emp.JobTypeID;
                        dbEmp.balanceH3 = emp.balanceH3;
                        dbEmp.balanceH1 = emp.balanceH1;
                        dbEmp.balanceH2 = emp.balanceH2;
                        dbEmp.balanceH4 = emp.balanceH4;
                        dbEmp.Emp_Mail = emp.EmpMail;
                        dbEmp.Emp_Phone = emp.Emp_Phone;
                        dbEmp.Emp_mobile = emp.Emp_mobile;
                        await db.SaveChangesAsync();
                        var muser = await _userManager.FindByNameAsync(userName);
                        if (muser != null)
                        {
                            if (!(dbEmp.PasswordChanged ?? false))
                            {
                                string token = await _userManager.GeneratePasswordResetTokenAsync(muser);
                                var result = await _userManager.ResetPasswordAsync(muser, token, password);
                            }

                        }
                        else
                        {
                            var nuser = new ApplicationUser { UserName = userName, Email = userName, IsAdmin = false, IsActive = true };
                            var result = await _userManager.CreateAsync(nuser, password);
                        }

                    }
                }

                return Ok(new
                {
                    isok = true

                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    isok = false,
                    msg = GetExceptionMessages(ex)
                });
            }




        }



        [HttpPost("uploadcust")]
        public async Task<IActionResult> UploadCustomers(List<CustomerModel> model)
        {
            try
            {
                foreach (var cust in model)
                {

                     
                    var dbcust = await db.Customers.FirstOrDefaultAsync(t => t.CusID == cust.CusID);
                    

                   

                   
                    if (dbcust == null)
                    {
                        dbcust = new Customer()
                        {
                             code = cust.code,
                             Address = cust.Address,
                             BranchId = cust.BranchId,
                             CityID = cust.CityID,
                             Company = cust.Company,
                             CountryID = cust.CountryID,
                             CusID = cust.CusID,
                             CusName = cust.CusName,
                             CusNamee = cust.CusNamee,
                             Cus_Phone = cust.Cus_Phone,
                             Cus_mobile = cust.Cus_mobile,
                             E_mail = cust.E_mail,
                             EmpId = cust.EmpId,
                             FaxNumber = cust.FaxNumber,
                             Fullcode  = cust.Fullcode,
                             GovernmentID = cust.GovernmentID,
                             HomeTel = cust.HomeTel,
                             JobTitle = cust.JobTitle,
                             Mail2 = cust.Mail2,
                             Mobile1 = cust.Mobile1,
                             Mobile2 = cust.Mobile2,
                             SaleType  = cust.SaleType,
                             Sex = cust.Sex,
                             Type = cust.Type,
                             ZipCode = cust.ZipCode
                             
                        };
                        db.Customers.Add(dbcust);
                        await db.SaveChangesAsync();
                        
                    }
                    else
                    {

                            dbcust.code = cust.code;
                            dbcust.Address = cust.Address;
                            dbcust.BranchId = cust.BranchId;
                            dbcust.CityID = cust.CityID;
                            dbcust.Company = cust.Company;
                            dbcust.CountryID = cust.CountryID;

                            dbcust.CusName = cust.CusName;
                            dbcust.CusNamee = cust.CusNamee;
                            dbcust.Cus_Phone = cust.Cus_Phone;
                            dbcust.Cus_mobile = cust.Cus_mobile;
                            dbcust.E_mail = cust.E_mail;
                            dbcust.EmpId = cust.EmpId;
                            dbcust.FaxNumber = cust.FaxNumber;
                            dbcust.Fullcode = cust.Fullcode;
                            dbcust. GovernmentID = cust.GovernmentID;
                            dbcust.HomeTel = cust.HomeTel;
                            dbcust.JobTitle = cust.JobTitle;
                            dbcust.Mail2 = cust.Mail2;
                            dbcust.Mobile1 = cust.Mobile1;
                            dbcust.Mobile2 = cust.Mobile2;
                            dbcust.SaleType = cust.SaleType;
                            dbcust.Sex = cust.Sex;
                            dbcust.Type = cust.Type;
                            dbcust.ZipCode = cust.ZipCode;
                        await db.SaveChangesAsync();
                      
                       

                    }
                }

                return Ok(new
                {
                    isok = true

                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    isok = false,
                    msg = GetExceptionMessages(ex)
                });
            }




        }

        [HttpPost("uploadscreen")]
        public async Task<IActionResult> UploadScreen(List<EmployeeScreenModel> model)
        {
            try
            {
                await db.Database.ExecuteSqlRawAsync("DELETE FROM TblEmployeeScreen;");

                foreach (var emp in model)
                {

                    var dbitem = new TblEmployeeScreen()
                    {
                        Emp_ID = emp.Emp_ID,
                        ScreenName = emp.ScreenName,
                        CanShow = emp.CanShow,
                        CanAdd = emp.CanAdd,
                        CanEdit = emp.CanEdit,
                        RowID = Guid.NewGuid()
                    };
                    await db.TblEmployeeScreens.AddAsync(dbitem);
                    await db.SaveChangesAsync();

                }

                return Ok(new
                {
                    isok = true

                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    isok = false,
                    msg = GetExceptionMessages(ex)
                });
            }




        }
    }
}
