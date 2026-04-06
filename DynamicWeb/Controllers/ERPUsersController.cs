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
using MyERP.Models.CustomModels;
using System.Threading.Tasks;

namespace MyERP.Controllers
{
    public class ERPUsersController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة المستخدمين  ",
                EnAction = "Index",
                ControllerName = "ERPUsers",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("ERPUsers", "View", "Index", null, null, "المستخدمين");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<ERPUser> erpUsers;

            if (string.IsNullOrEmpty(searchWord))
            {
                erpUsers = db.ERPUsers.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.ERPUsers.Where(s => s.IsDeleted == false).Count();
            }
            else
            {
                erpUsers = db.ERPUsers.Where(s => s.IsDeleted == false && (s.Name.Contains(searchWord) || s.UserName.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.ERPUsers.Where(s => s.IsDeleted == false && (s.Name.Contains(searchWord) || s.UserName.Contains(searchWord))).Count();

            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(erpUsers.ToList());

        }

        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {
                ERPUser erpUserObj = new ERPUser();
                ERPUsersModel erpUserVM = new ERPUsersModel()
                {
                    Id = erpUserObj.Id,
                    Name = erpUserObj.Name,
                    UserName = erpUserObj.UserName,
                    Password = erpUserObj.Password,
                    Email = erpUserObj.Email,
                    EmployeeId = erpUserObj.EmployeeId,
                    RoleId = erpUserObj.RoleId,
                    IsDeleted = erpUserObj.IsDeleted,
                    IsActive = erpUserObj.IsActive,
                    AllCashBoxes = db.GetAllUserCashBox().ToList(),
                    AllDepartments = db.GetAllUserDepartment().ToList(),
                    AllWareHouses = db.GetAllUserWareHouse().ToList(),
                    AllPos = db.GetAllUserPos().ToList(),
                    IsCashier=erpUserObj.IsCashier,
                    IsWaiter = erpUserObj.IsWaiter,
                    CustodyBoxId=erpUserObj.CustodyBoxId,
                    ShowDashBoardForUser=erpUserObj.ShowDashBoardForUser,
                    EnableTwoFactorAuthentication=erpUserObj.EnableTwoFactorAuthentication,
                    AppPassword = erpUserObj.AppPassword,
                    EnableAppPassword = erpUserObj.EnableAppPassword,
                    VerificationCode = erpUserObj.VerificationCode
                };
                ViewBag.CustodyBoxId = new SelectList(db.CashBoxes.Where(c => c.TypeId == 2).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.EmployeeId = new SelectList(db.Employees.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.RoleId = new SelectList(db.ERPRoles.Where(b=>b.IsActive==true&&b.IsDeleted==false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                return View(erpUserVM);
            }
            ERPUser erpUser = db.ERPUsers.Find(id);
            if (erpUser == null)
            {
                return HttpNotFound();
            }
            else
            {
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "فتح تفاصيل المستخدمين",
                    EnAction = "AddEdit",
                    ControllerName = "ERPUsers",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "GET",
                    SelectedItem = erpUser.Id,
                    ArItemName = erpUser.Name

                });
                ERPUsersModel erpUserVM = new ERPUsersModel()
                {
                    Id = erpUser.Id,
                    Name = erpUser.Name,
                    UserName = erpUser.UserName,
                    Password = erpUser.Password,
                    Email = erpUser.Email,
                    IsDeleted = erpUser.IsDeleted,
                    IsActive = erpUser.IsActive,
                    EmployeeId = erpUser.EmployeeId,
                    RoleId = erpUser.RoleId,
                    UserCashboxes = db.GetUserCashBoxes(id).ToList(),
                    UserDepartments = db.GetUserDepartments(id).ToList(),
                    UserWareHouses = db.GetUserWarehouses(id).ToList(),
                    UserPos = db.GetUserPos(id).ToList(),
                    IsCashier=erpUser.IsCashier,
                    IsWaiter = erpUser.IsWaiter,
                    CustodyBoxId=erpUser.CustodyBoxId,
                    ShowDashBoardForUser = erpUser.ShowDashBoardForUser,
                    EnableTwoFactorAuthentication = erpUser.EnableTwoFactorAuthentication,
                    AppPassword = erpUser.AppPassword,
                    EnableAppPassword = erpUser.EnableAppPassword,
                    VerificationCode = erpUser.VerificationCode
                };
                ViewBag.CustodyBoxId = new SelectList(db.CashBoxes.Where(c=>c.TypeId==2).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", erpUser.CustodyBoxId);

                ViewBag.EmployeeId = new SelectList(db.Employees.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", erpUser.EmployeeId);
                ViewBag.RoleId = new SelectList(db.ERPRoles.Where(b => b.IsActive == true && b.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", erpUser.RoleId);
                ViewBag.Next = QueryHelper.Next((int)id, "ERPUser");
                ViewBag.Previous = QueryHelper.Previous((int)id, "ERPUser");
                ViewBag.Last = QueryHelper.GetLast("ERPUser");
                ViewBag.First = QueryHelper.GetFirst("ERPUser");
                return View(erpUserVM);
            }
        }

        [HttpPost]
        public async Task<ActionResult> AddEdit(ERPUser erpuser)
        {
            if (ModelState.IsValid)
            {
                var id = erpuser.Id;

                //*********************************** EDIT ******************************//
                if (erpuser.Id > 0)
                {
                    if (await db.ERPUsers.Where(x => x.UserName == erpuser.UserName && x.Id!=erpuser.Id).AnyAsync())
                        return Json(new { success = "false", cause = "username taken" });

                    ////---------------------------------update user departments-----------------------
                    MyXML.xPathName = "UserDepartments";
                    var userDepartments = MyXML.GetXML(erpuser.UserDepartments.Where(a => a.Privilege == true));

                    ////---------------------------------update UserWareHouses-----------------------
                    MyXML.xPathName = "UserWareHouses";
                    var userWareHouses = MyXML.GetXML(erpuser.UserWareHouses.Where(a => a.Privilege == true));
                    ////---------------------------------update UserCashBoxes-----------------------
                    MyXML.xPathName = "UserCashBoxes";
                    var userCashBoxes = MyXML.GetXML(erpuser.UserCashBoxes.Where(a => a.Privilege == true));

                    ////---------------------------------update UserPos-----------------------
                    MyXML.xPathName = "UserPos";
                    var userPos = MyXML.GetXML(erpuser.UserPos.Where(a => a.Privilege == true));
                    erpuser.Password = string.IsNullOrEmpty(erpuser.Password) ? null :PasswordEncrypt.ComputeHashPwd(erpuser.Password);

                    //___________________check if Emoloyee With AnyUser _______________//
                    // NoChange
                    //if ( )
                    //{ }
                    ////  return Json(new { success = "true"/*, EmpExist = "Employee Exist" */});
                    ////change
                    //else
                    //{
                        if (db.ERPUsers.Where(x => x.EmployeeId == erpuser.EmployeeId && x.Id == erpuser.Id).Count() ==0 && db.ERPUsers.Where(x => x.EmployeeId == erpuser.EmployeeId).Count()>0)
                        {
                            return Json(new { success = "false", EmpExist = "Employee Exist" });
                        }
                 //   }
                    //______________________________________________________________________________________________________________//

                    db.ERPUser_Update(erpuser.Id, erpuser.UserId, erpuser.Name, erpuser.UserName,erpuser.Password, erpuser.Email, erpuser.EmployeeId, erpuser.RoleId, false, erpuser.IsActive, userDepartments, userWareHouses, userCashBoxes,erpuser.IsCashier,userPos,erpuser.CustodyBoxId,erpuser.ShowDashBoardForUser,erpuser.EnableTwoFactorAuthentication, erpuser.VerificationCode, erpuser.AppPassword, erpuser.EnableAppPassword,erpuser.IsWaiter);

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("ERPUsers", "Edit", "AddEdit", id, null, "المستخدمين");
                    bool isOnline = System.Web.Configuration.WebConfigurationManager.AppSettings["Online"] == "true" ? true : false;

                    // Add DB Change
                    QueryHelper.AddDBChange(new DBChange() {
                        TableName = "ERPUser",
                        SelectedId = erpuser.Id,
                        IsMasterChange = isOnline,
                        IsNew = false,
                        IsTransaction = false
                    });

                }

                //********************************* ADD ********************************//
                else
                {
                    if (await db.ERPUsers.Where(x=>x.UserName==erpuser.UserName).AnyAsync())
                        return Json(new { success = "false", cause="username taken" });

                    ////---------------------------------insert UserCashBoxes-----------------------
                    MyXML.xPathName = "UserCashBoxes";
                    var userCashBoxes = MyXML.GetXML(erpuser.UserCashBoxes.Where(a => a.Privilege == true));
                    ////---------------------------------insert UserPos-----------------------
                    MyXML.xPathName = "UserPos";
                    var userPos = MyXML.GetXML(erpuser.UserPos.Where(a => a.Privilege == true));
                    ////---------------------------------insert user departments-----------------------

                    MyXML.xPathName = "UserDepartments";
                    var userDepartments = MyXML.GetXML(erpuser.UserDepartments.Where(a => a.Privilege == true));
                    ////---------------------------------insert UserWareHouses-----------------------

                    MyXML.xPathName = "UserWareHouses";
                    var userWareHouses = MyXML.GetXML(erpuser.UserWareHouses.Where(a => a.Privilege == true));
                    var password = PasswordEncrypt.ComputeHashPwd(erpuser.Password);


                    //___________________check if Emoloyee With AnyUser _______________//

                    if (await db.ERPUsers.Where(x => x.EmployeeId == erpuser.EmployeeId).AnyAsync())
                        return Json(new { success = "false", EmpExist = "Employee Exist" });
         //______________________________________________________________________________________________________________//

                    db.ERPUser_Insert(erpuser.UserId, erpuser.Name, erpuser.UserName, password, erpuser.Email, erpuser.EmployeeId, erpuser.RoleId, false, true, userDepartments, userWareHouses, userCashBoxes,erpuser.IsCashier,userPos,erpuser.CustodyBoxId,erpuser.ShowDashBoardForUser,erpuser.EnableTwoFactorAuthentication, erpuser.VerificationCode, erpuser.AppPassword, erpuser.EnableAppPassword,erpuser.IsWaiter);
                    db.UserHomePageDefault();
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("ERPUsers", "Add", "AddEdit", erpuser.Id, null, "المستخدمين");
                    bool isOnline = System.Web.Configuration.WebConfigurationManager.AppSettings["Online"] == "true" ? true : false;

                    // Add DB Change
                    var SelectedId = db.ERPUsers.OrderByDescending(r => r.Id).FirstOrDefault().Id;
                    QueryHelper.AddDBChange(new DBChange()
                    {
                        TableName = "ERPUser",
                        SelectedId = SelectedId,
                        IsMasterChange = isOnline,
                        IsNew = true,
                        IsTransaction = false
                    });

                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل بيانات مستخدم" : "اضافة مستخدم",
                    EnAction = "AddEdit",
                    ControllerName = "ERPUsers",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = erpuser.Id > 0 ? erpuser.Id : db.ERPUsers.Max(i => i.Id),
                    ArItemName = erpuser.Name,

                });
                return Json(new { success = "true", id });
            }
            var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();

            return Json(new { success = "false",errors });
        }

        [HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                ERPUser erpUser = db.ERPUsers.Find(id);
                erpUser.IsDeleted = true;
                erpUser.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(erpUser).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف مستخدم",
                    EnAction = "AddEdit",
                    ControllerName = "ERPUsers",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = erpUser.Name,

                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("ERPUsers", "Delete", "Delete", id, null, "المستخدمين");
                bool isOnline = System.Web.Configuration.WebConfigurationManager.AppSettings["Online"] == "true" ? true : false;

                // Add DB Change
                QueryHelper.AddDBChange(new DBChange()
                {
                    TableName = "ERPUser",
                    SelectedId = id,
                    IsMasterChange = isOnline,
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
                ERPUser erpUser = db.ERPUsers.Find(id);
                if (erpUser.IsActive == true)
                {
                    erpUser.IsActive = false;
                }
                else
                {
                    erpUser.IsActive = true;
                }
                erpUser.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(erpUser).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)erpUser.IsActive ? "تنشيط مستخدم" : "إلغاء تنشيط مستخدم",
                    EnAction = "AddEdit",
                    ControllerName = "ERPUsers",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = erpUser.Id,
                    EnItemName = erpUser.Name,

                });
                ////-------------------- Notification-------------------------////
                if (erpUser.IsActive == true)
                {
                    Notification.GetNotification("ERPUsers", "Activate/Deactivate", "ActivateDeactivate", id, true, "المستخدمين");
                }
                else
                {

                    Notification.GetNotification("ERPUsers", "Activate/Deactivate", "ActivateDeactivate", id, false, "المستخدمين");
                }
                bool isOnline = System.Web.Configuration.WebConfigurationManager.AppSettings["Online"] == "true" ? true : false;

                // Add DB Change
                QueryHelper.AddDBChange(new DBChange()
                {
                    TableName = "ERPUser",
                    SelectedId = id,
                    IsMasterChange = isOnline,
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
}
