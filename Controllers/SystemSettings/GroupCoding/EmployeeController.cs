using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using MyERP.Models;
using MyERP.Repository;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace MyERP.Controllers.SystemSettings.GroupCoding
{
    public class EmployeeController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: Employee
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "", int departmentId = 0)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            if (userId == 1)
            {
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", departmentId);
            }
            else
            {
                ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", departmentId);
            }

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة الموظفين",
                EnAction = "Index",
                ControllerName = "Employee",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("Employee", "View", "Index", null, null, "الموظفين");
            //////////-----------------------------------------------------------------------
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<Employee> employees;

            if (string.IsNullOrEmpty(searchWord))
            {
                if (userId == 1)
                {
                    employees = db.Employees.Where(s => s.IsDeleted == false  && (departmentId == 0 || s.DepartmentId == departmentId)).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.Employees.Where(s => s.IsDeleted == false  && (departmentId == 0 || s.DepartmentId == departmentId)).Count();
                }
                else
                {
                    employees = db.Employees.Where(s => s.IsDeleted == false && (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (departmentId == 0 || s.DepartmentId == departmentId)).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.Employees.Where(s => s.IsDeleted == false && (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (departmentId == 0 || s.DepartmentId == departmentId)).Count();
                }
            }
            else
            {
                if (userId == 1)
                {
                    employees = db.Employees.Where(s => s.IsDeleted == false  && (departmentId == 0 || s.DepartmentId == departmentId) && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.ChartOfAccount.ArName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.NationalId.Contains(searchWord) || s.Birthdate.ToString().Contains(searchWord) || s.HireDate.ToString().Contains(searchWord) || s.Email.Contains(searchWord) || s.Notes.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.Employees.Where(s => s.IsDeleted == false && (departmentId == 0 || s.DepartmentId == departmentId) && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.ChartOfAccount.ArName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.NationalId.Contains(searchWord) || s.Birthdate.ToString().Contains(searchWord) || s.HireDate.ToString().Contains(searchWord) || s.Email.Contains(searchWord) || s.Notes.Contains(searchWord))).Count();
                }
                else
                {
                    employees = db.Employees.Where(s => s.IsDeleted == false && s.IsActive == true && (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (departmentId == 0 || s.DepartmentId == departmentId) && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.ChartOfAccount.ArName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.NationalId.Contains(searchWord) || s.Birthdate.ToString().Contains(searchWord) || s.HireDate.ToString().Contains(searchWord) || s.Email.Contains(searchWord) || s.Notes.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.Employees.Where(s => s.IsDeleted == false && s.IsActive == true && (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (departmentId == 0 || s.DepartmentId == departmentId) && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.ChartOfAccount.ArName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.NationalId.Contains(searchWord) || s.Birthdate.ToString().Contains(searchWord) || s.HireDate.ToString().Contains(searchWord) || s.Email.Contains(searchWord) || s.Notes.Contains(searchWord))).Count();
                }
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(employees.ToList());
        }

        // GET: Employee/Edit/5
        public ActionResult AddEdit(int? id)
        {
            var SystemPageId = db.SystemPages.Where(a => a.IsActive == true && a.IsDeleted == false && a.ControllerName == "Employee").FirstOrDefault().Id;
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            SystemSetting systemSetting = db.SystemSettings.FirstOrDefault();
            ViewBag.EnterEmployeeCodeManually = systemSetting.EnterEmployeeCodeManually;
            if (id == null)
            {
                ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
                {
                    b.Id,
                    ArName = b.FixedPart + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.AccountId = new SelectList(db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && c.ClassificationId == 3).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                // drop down list of Job Grades
                ViewBag.JobGradeId = new SelectList(db.JobGrades.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                // drop down list of Gender
                ViewBag.GenderId = new SelectList(db.Genders.Select(b => new
                {
                    Id = b.Id,
                    ArName = b.ArName
                }), "Id", "ArName");
                // drop down list of Marital Status
                ViewBag.MaritalStatusId = new SelectList(db.MaritalStatus.Select(b => new
                {
                    Id = b.Id,
                    ArName = b.ArName
                }), "Id", "ArName");
                // drop down list of Contracts Type
                ViewBag.ContractsTypeId = new SelectList(db.ContractsTypes.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                // drop down list of Work Status
                ViewBag.WorkStatusId = new SelectList(db.WorkStatus.Select(b => new
                {
                    Id = b.Id,
                    ArName = b.ArName
                }), "Id", "ArName");
                // drop down list of Direct Manager
                ViewBag.DirectManagerId = new SelectList(db.Employees.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                // drop down list of Employee Type
                ViewBag.EmployeeTypeId = new SelectList(db.EmployeeTypes.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                // drop down list of Alternative Employee
                ViewBag.AlternativeEmployeeId = new SelectList(db.Employees.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                // drop down list of Department
                ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName");
                // drop down list of Nationality
                ViewBag.NationalityId = new SelectList(db.Nationalities.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                // drop down list of Religion
                ViewBag.ReligionId = new SelectList(db.Religions.Select(b => new
                {
                    Id = b.Id,
                    ArName = b.ArName
                }), "Id", "ArName");
                // drop down list of Location
                ViewBag.LocationId = new SelectList(db.Locations.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                // drop down list of Work Team
                ViewBag.WorkTeamId = new SelectList(db.WorkTeams.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                // drop down list of National Id Job
                ViewBag.NationalJobId = new SelectList(db.Jobs.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                // drop down list of passport Area 
                ViewBag.PassportReleaseAddressId = new SelectList(db.Countries.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                //ViewBag.PassportReleaseAddressId = new SelectList(db.Areas.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                //{
                //    Id = b.Id,
                //    ArName = b.Code + " - " + b.ArName
                //}), "Id", "ArName");

                // drop down list of Passport Job
                ViewBag.PassportJobId = new SelectList(db.Jobs.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                // drop down list of Area
                ViewBag.AreaId = new SelectList(db.Areas.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                // drop down list of Hr Department
                ViewBag.HrDepartmentId = new SelectList(db.HrDepartments.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                // drop down list of Administrative Departments
                ViewBag.AdministrativeDepartmentId = new SelectList(db.AdministrativeDepartments.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                // drop down list of Job
                ViewBag.JobId = new SelectList(db.Jobs.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");


                return View();
            }
            Employee employee = db.Employees.Find(id);
            if (employee == null)
            {
                return HttpNotFound();
            }
            ViewBag.Next = QueryHelper.Next((int)id, "Employee");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Employee");
            ViewBag.Last = QueryHelper.GetLast("Employee");
            ViewBag.First = QueryHelper.GetFirst("Employee");

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل  الموظف",
                EnAction = "AddEdit",
                ControllerName = "Employee",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = employee.Id,
                ArItemName = employee.ArName,
                EnItemName = employee.EnName,
                CodeOrDocNo = employee.Code
            });

            ViewBag.AccountId = new SelectList(db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && c.ClassificationId == 3).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", employee.AccountId);


            // drop down list of Job Grades
            ViewBag.JobGradeId = new SelectList(db.JobGrades.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", employee.JobGradeId);
            // drop down list of Gender
            ViewBag.GenderId = new SelectList(db.Genders.Select(b => new
            {
                Id = b.Id,
                ArName = b.ArName
            }), "Id", "ArName", employee.GenderId);
            // drop down list of Marital Status
            ViewBag.MaritalStatusId = new SelectList(db.MaritalStatus.Select(b => new
            {
                Id = b.Id,
                ArName = b.ArName
            }), "Id", "ArName", employee.MaritalStatusId);
            // drop down list of Contracts Type
            ViewBag.ContractsTypeId = new SelectList(db.ContractsTypes.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", employee.ContractsTypeId);
            // drop down list of Work Status
            ViewBag.WorkStatusId = new SelectList(db.WorkStatus.Select(b => new
            {
                Id = b.Id,
                ArName = b.ArName
            }), "Id", "ArName", employee.WorkStatusId);
            // drop down list of Direct Manager
            ViewBag.DirectManagerId = new SelectList(db.Employees.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", employee.DirectManagerId);
            // drop down list of Employee Type
            ViewBag.EmployeeTypeId = new SelectList(db.EmployeeTypes.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", employee.EmployeeTypeId);
            // drop down list of Alternative Employee
            ViewBag.AlternativeEmployeeId = new SelectList(db.Employees.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", employee.AlternativeEmployeeId);
            // drop down list of Department
            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", employee.DepartmentId);
            // drop down list of Nationality
            ViewBag.NationalityId = new SelectList(db.Nationalities.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", employee.NationalityId);
            // drop down list of Religion
            ViewBag.ReligionId = new SelectList(db.Religions.Select(b => new
            {
                Id = b.Id,
                ArName = b.ArName
            }), "Id", "ArName", employee.ReligionId);
            // drop down list of Location
            ViewBag.LocationId = new SelectList(db.Locations.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", employee.LocationId);
            // drop down list of Work Team
            ViewBag.WorkTeamId = new SelectList(db.WorkTeams.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", employee.WorkTeamId);
            // drop down list of National Id Job
            ViewBag.NationalJobId = new SelectList(db.Jobs.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", employee.NationalJobId);
            // drop down list of passport Area 
            ViewBag.PassportReleaseAddressId = new SelectList(db.Countries.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", employee.PassportReleaseAddressId);
            //ViewBag.PassportReleaseAddressId = new SelectList(db.Areas.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            //{
            //    Id = b.Id,
            //    ArName = b.Code + " - " + b.ArName
            //}), "Id", "ArName", employee.PassportReleaseAddressId);

            // drop down list of Passport Job
            ViewBag.PassportJobId = new SelectList(db.Jobs.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", employee.PassportJobId);
            // drop down list of Area
            ViewBag.AreaId = new SelectList(db.Areas.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", employee.AreaId);
            // drop down list of Hr Department
            ViewBag.HrDepartmentId = new SelectList(db.HrDepartments.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", employee.HrDepartmentId);
            // drop down list of Administrative Departments
            ViewBag.AdministrativeDepartmentId = new SelectList(db.AdministrativeDepartments.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", employee.AdministrativeDepartmentId);
            // drop down list of Job
            ViewBag.JobId = new SelectList(db.Jobs.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", employee.JobId);
            ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
            {
                b.Id,
                ArName = b.FixedPart + " - " + b.ArName
            }), "Id", "ArName", employee.FieldsCodingId);
            try
            {
                ViewBag.Birthdate = employee.Birthdate.Value.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.HireDate = employee.HireDate.Value.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.DrivingLicenseExpiryDate = employee.DrivingLicenseExpiryDate.Value.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.DrivingLicenseReleaseDate = employee.DrivingLicenseReleaseDate.Value.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.WorkLicenseExpiryDate = employee.WorkLicenseExpiryDate.Value.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.WorkLicenseReleaseDate = employee.WorkLicenseReleaseDate.Value.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.ResidenceExpiryDate = employee.ResidenceExpiryDate.Value.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.ResidenceReleaseDate = employee.ResidenceReleaseDate.Value.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.IndustrialSafetyExpiryDate = employee.IndustrialSafetyExpiryDate.Value.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.PassportExpiryDate = employee.PassportExpiryDate.Value.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.PassportReleaseDate = employee.PassportReleaseDate.Value.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.NationalExpiryDate = employee.NationalExpiryDate.Value.ToString("yyyy-MM-ddTHH:mm");

            }
            catch (Exception)
            {
            }
            return View(employee);
        }

        // POST: Employee/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult AddEdit(Employee employee, string newBtn, HttpPostedFileBase upload)
        {
            var SystemPageId = db.SystemPages.Where(a => a.IsActive == true && a.IsDeleted == false && a.ControllerName == "Employee").FirstOrDefault().Id;
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            SystemSetting systemSetting = db.SystemSettings.FirstOrDefault();
            var EnterEmployeeCodeManually = systemSetting.EnterEmployeeCodeManually;

            if (ModelState.IsValid)
            {
                if (EnterEmployeeCodeManually == true && employee.Id == 0)
                {
                    var emp = db.Employees.Where(a => a.Code == employee.Code).FirstOrDefault();
                    if (emp != null)
                    {
                        return Content("Employee Code Is Used Before");
                    }
                }
                employee.IsDeleted = false;
                if (employee.Id > 0)
                {
                    if (EnterEmployeeCodeManually == true)
                    {
                        var emp = db.Employees.AsNoTracking().Where(a => a.Code == employee.Code).FirstOrDefault();
                        if (emp != null)
                        {
                            if (emp.Id != employee.Id)
                            {
                                return Content("Employee Code Is Used Before");
                            }
                        }
                    }
                    employee.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    employee.IsActive = true;
                    if (upload != null)
                    {
                        upload.SaveAs(Server.MapPath("/images/EmployeesImages/") + upload.FileName);

                        employee.Image = domainName + ("/images/EmployeesImages/") + upload.FileName;

                    }
                    db.Entry(employee).State = EntityState.Modified;
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("Employee", "Edit", "AddEdit", employee.Id, null, "الموظفين");
                }
                else
                {
                    employee.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    //employee.Code= (QueryHelper.CodeLastNum("Employee") + 1).ToString();
                    if (EnterEmployeeCodeManually != true)
                    {
                        employee.Code = new JavaScriptSerializer().Serialize(SetCodeNum(employee.FieldsCodingId).Data).ToString().Trim('"');
                    }
                    employee.IsActive = true;
                    if (upload != null)
                    {
                        upload.SaveAs(Server.MapPath("/images/EmployeesImages/") + upload.FileName);

                        employee.Image = domainName + ("/images/EmployeesImages/") + upload.FileName;
                    }
                    db.Employees.Add(employee);


                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("Employee", "Add", "AddEdit", employee.Id, null, "الموظفين");
                }
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = employee.Id > 0 ? "تعديل موظف" : "اضافة  موظف",
                    EnAction = "AddEdit",
                    ControllerName = "Employee",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "Post",
                    SelectedItem = employee.Id,
                    ArItemName = employee.ArName,
                    EnItemName = employee.EnName,
                    CodeOrDocNo = employee.Code
                });
                if (newBtn == "saveAndNew")
                    return RedirectToAction("AddEdit");
                else
                    return RedirectToAction("Index");
            }
            ViewBag.AccountId = new SelectList(db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && c.ClassificationId == 3), "Id", "ArName", employee.AccountId);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", employee.DepartmentId);

            ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
            {
                b.Id,
                ArName = b.FixedPart + " - " + b.ArName
            }), "Id", "ArName", employee.FieldsCodingId);
            //Gender
            ViewBag.GenderId = new SelectList(db.Genders, "Id", "ArName", employee.GenderId);
            //MaritalStatus
            ViewBag.MaritalStatusId = new SelectList(db.MaritalStatus, "Id", "ArName", employee.MaritalStatusId);
            //Religion
            ViewBag.ReligionId = new SelectList(db.Religions, "Id", "ArName", employee.ReligionId);
            //Workstatus
            ViewBag.WorkStatusId = new SelectList(db.WorkStatus, "Id", "ArName", employee.WorkStatusId);
            //JobGrade
            ViewBag.JobGradeId = new SelectList(db.JobGrades.Where(a => a.IsDeleted == false && a.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", employee.JobGradeId);
            //EmployeeType
            ViewBag.EmployeeTypeId = new SelectList(db.EmployeeTypes.Where(a => a.IsDeleted == false && a.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", employee.EmployeeTypeId);
            //ContractsType
            ViewBag.ContractsTypeId = new SelectList(db.ContractsTypes.Where(a => a.IsDeleted == false && a.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", employee.ContractsTypeId);
            //Nationality
            ViewBag.NationalityId = new SelectList(db.Nationalities.Where(a => a.IsDeleted == false && a.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", employee.NationalityId);
            //Location
            ViewBag.LocationId = new SelectList(db.Locations.Where(a => a.IsDeleted == false && a.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", employee.LocationId);
            //WorkTeam
            ViewBag.WorkTeamId = new SelectList(db.WorkTeams.Where(a => a.IsDeleted == false && a.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", employee.WorkTeamId);
            //Area
            ViewBag.AreaId = new SelectList(db.Areas.Where(a => a.IsDeleted == false && a.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", employee.AreaId);
            //HrDepartment
            ViewBag.HrDepartmentId = new SelectList(db.HrDepartments.Where(a => a.IsDeleted == false && a.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", employee.HrDepartmentId);
            //AdminstraiveDepartment
            ViewBag.AdministrativeDepartmentId = new SelectList(db.AdministrativeDepartments.Where(a => a.IsDeleted == false && a.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", employee.AdministrativeDepartmentId);
            //Job
            ViewBag.JobId = new SelectList(db.Jobs.Where(a => a.IsDeleted == false && a.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", employee.JobId);
            //DirectManager -- SelfJoin
            ViewBag.DirectManagerId = new SelectList(db.Employees.Where(a => a.IsDeleted == false && a.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", employee.DirectManagerId);


            //<<Alternative Employee>>
            ViewBag.AlternativeEmployeeId = new SelectList(db.Employees.Where(a => a.IsDeleted == false && a.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", employee.AlternativeEmployeeId);
            //<<National Job>>
            ViewBag.NationalJobId = new SelectList(db.Jobs.Where(a => a.IsDeleted == false && a.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", employee.NationalJobId);
            //<<Passport Job>>
            ViewBag.PassportJobId = new SelectList(db.Jobs.Where(a => a.IsDeleted == false && a.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", employee.PassportJobId);
            //<<Passport Address Releas>>
            ViewBag.PassportReleaseAddressId = new SelectList(db.Countries.Where(a => a.IsDeleted == false && a.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", employee.PassportReleaseAddressId);
            //ViewBag.PassportReleaseAddressId = new SelectList(db.Areas.Where(a => a.IsDeleted == false && a.IsActive == true).Select(b => new
            //{
            //    Id = b.Id,
            //    ArName = b.Code + " - " + b.ArName
            //}), "Id", "ArName", employee.PassportReleaseAddressId);


            return View(employee);
        }
        public ActionResult AddEdit2(int? id)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            if (id == null)
            {
                // drop down list of Gender
                ViewBag.GenderId = new SelectList(db.Genders.Select(b => new
                {
                    Id = b.Id,
                    ArName = b.ArName
                }), "Id", "ArName");
                // drop down list of Marital Status
                ViewBag.MaritalStatusId = new SelectList(db.MaritalStatus.Select(b => new
                {
                    Id = b.Id,
                    ArName = b.ArName
                }), "Id", "ArName");
                // drop down list of ApplicantWorkStatus
                ViewBag.ApplicantWorkStatusId = new SelectList(db.ApplicantWorkStatus.Select(b => new
                {
                    Id = b.Id,
                    ArName = b.ArName
                }), "Id", "ArName");
                // drop down list of Job 
                ViewBag.JobId = new SelectList(db.Jobs.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                // drop down list of Church 
                ViewBag.ChurchId = new SelectList(db.Churches.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                // drop down list of Qualification 
                ViewBag.QualificationId = new SelectList(db.Qualifications.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.AreaId = new SelectList(db.Areas.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                return View();
            }
            Employee employee = db.Employees.Find(id);
            if (employee == null)
            {
                return HttpNotFound();
            }
            ViewBag.Next = QueryHelper.Next((int)id, "Employee");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Employee");
            ViewBag.Last = QueryHelper.GetLast("Employee");
            ViewBag.First = QueryHelper.GetFirst("Employee");

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل  الموظف",
                EnAction = "AddEdit",
                ControllerName = "Employee",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = employee.Id,
                ArItemName = employee.ArName,
                EnItemName = employee.EnName,
                CodeOrDocNo = employee.Code
            });
            // drop down list of Qualification 
            ViewBag.QualificationId = new SelectList(db.Qualifications.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");
            // drop down list of Church 
            ViewBag.ChurchId = new SelectList(db.Churches.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", employee.ChurchId);

            // drop down list of Gender
            ViewBag.GenderId = new SelectList(db.Genders.Select(b => new
            {
                Id = b.Id,
                ArName = b.ArName
            }), "Id", "ArName", employee.GenderId);
            // drop down list of Marital Status
            ViewBag.MaritalStatusId = new SelectList(db.MaritalStatus.Select(b => new
            {
                Id = b.Id,
                ArName = b.ArName
            }), "Id", "ArName", employee.MaritalStatusId);

            // drop down list of ApplicantWorkStatus
            ViewBag.ApplicantWorkStatusId = new SelectList(db.ApplicantWorkStatus.Select(b => new
            {
                Id = b.Id,
                ArName = b.ArName
            }), "Id", "ArName", employee.ApplicantWorkStatusId);

            // drop down list of Job
            ViewBag.JobId = new SelectList(db.Jobs.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName"/*, employee.JobId*/);
            ViewBag.AreaId = new SelectList(db.Areas.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", employee.AreaId);
            try
            {
                ViewBag.Birthdate = employee.Birthdate.Value.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.ApplicationDate = employee.ApplicationDate.Value.ToString("yyyy-MM-ddTHH:mm");
            }
            catch (Exception)
            {
            }

            return View(employee);
        }

        [HttpPost]
        public ActionResult AddEdit2(Employee employee)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            if (ModelState.IsValid)
            {
                employee.IsDeleted = false;

                if (employee.Id > 0)
                {
                    employee.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    employee.IsActive = true;

                    // use another object to prevent entity error
                    var old = db.Employees.Find(employee.Id);
                    old.Code = employee.Code;
                    old.ArName = employee.ArName;
                    old.Qualification = employee.Qualification;
                    old.GraduationYear = employee.GraduationYear;
                    old.MobileNumber = employee.MobileNumber;
                    old.PhoneNumber = employee.PhoneNumber;
                    old.EmployeeAddress = employee.EmployeeAddress;
                    old.ApplicantWorkStatusId = employee.ApplicantWorkStatusId;
                    old.ApplicationDate = employee.ApplicationDate;
                    old.Birthdate = employee.Birthdate;
                    old.Email = employee.Email;
                    old.ExpectedSalary = employee.ExpectedSalary;
                    old.GenderId = employee.GenderId;
                    old.ChurchId = employee.ChurchId;
                    old.MaritalStatusId = employee.MaritalStatusId;
                    old.NationalId = employee.NationalId;
                    old.IsActive = employee.IsActive;
                    old.IsDeleted = employee.IsDeleted;
                    old.AreaId = employee.AreaId;
                    old.PassportId = employee.PassportId;
                    old.MobileNumber2 = employee.MobileNumber2;
                    //old.Image = employee.Image;
                    db.EmployeeJobs.RemoveRange(db.EmployeeJobs.Where(p => p.EmployeeId == old.Id).ToList());
                    db.EmployeeExperiences.RemoveRange(db.EmployeeExperiences.Where(p => p.EmployeeId == old.Id).ToList());
                    foreach (var item in employee.EmployeeJobs)
                    {
                        old.EmployeeJobs.Add(item);
                    }
                    foreach (var item in employee.EmployeeExperiences)
                    {
                        old.EmployeeExperiences.Add(item);
                    }
                    db.EmployeeQualifications.RemoveRange(db.EmployeeQualifications.Where(p => p.EmployeeId == old.Id).ToList());
                    foreach (var item in employee.EmployeeQualifications)
                    {
                        old.EmployeeQualifications.Add(item);
                    }

                    if (employee.Image != null && employee.Image.Contains("base64"))
                    {
                        string fileName = "";
                        fileName = "/images/JobApp/EmployeeImg/" + employee.Code + "-" + employee.ArName + ".jpeg";
                        //to Check If Image Name Exist before
                        var file = Server.MapPath("/images/JobApp/EmployeeImg/" + employee.Code + "-" + employee.ArName + ".jpeg").Replace('\\', '/');
                        if (System.IO.File.Exists(file))
                        {
                            System.IO.File.Delete(file);
                        }
                        var bytes = new byte[7000];
                        if (employee.Image.Contains("jpeg"))
                        {
                            bytes = Convert.FromBase64String(employee.Image.Replace("data:image/jpeg;base64,", ""));
                        }
                        else
                        {
                            bytes = Convert.FromBase64String(employee.Image.Replace("data:image/png;base64,", ""));
                        }
                        using (var imageFile = new FileStream(Server.MapPath(fileName), FileMode.Create))
                        {
                            imageFile.Write(bytes, 0, bytes.Length);
                            imageFile.Flush();
                        }
                        old.Image = domainName + fileName;
                    }
                    else if (employee.Image == null && old.Image != null)
                    {
                        old.Image = old.Image;
                    }
                    if (employee.CV != null && employee.CV.Contains("base64"))
                    {
                        string fileName = "";
                        fileName = "/images/JobApp/EmployeeCv/" + employee.Code + "-" + employee.ArName + ".pdf";
                        //to Check If CV Name Exist before
                        var file = Server.MapPath("/images/JobApp/EmployeeCv/" + employee.Code + "-" + employee.ArName + ".jpeg").Replace('\\', '/');
                        if (System.IO.File.Exists(file))
                        {
                            System.IO.File.Delete(file);
                        }
                        var bytes = new byte[7000];
                        if (employee.CV.Contains("pdf"))
                        {
                            bytes = Convert.FromBase64String(employee.CV.Replace("data:application/pdf;base64,", ""));
                        }
                        else
                        {
                            bytes = Convert.FromBase64String(employee.CV.Replace("data:application/pdf;base64,", ""));
                        }
                        using (var CvFile = new FileStream(Server.MapPath(fileName), FileMode.Create))
                        {
                            CvFile.Write(bytes, 0, bytes.Length);
                            CvFile.Flush();
                        }
                        employee.CV = domainName + fileName;
                    }
                    else if (employee.CV == null && old.CV != null)
                    {
                        old.CV = old.CV;
                    }

                    db.Entry(old).State = EntityState.Modified;
                    // db.Entry(employee).State = EntityState.Modified;
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("Employee", "Edit", "AddEdit", employee.Id, null, "الموظفين");
                }
                else
                {
                    employee.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    //employee.Code= (QueryHelper.CodeLastNum("Employee") + 1).ToString();
                    employee.IsActive = true;

                    if (employee.Image != null && employee.Image.Contains("base64"))
                    {
                        string fileName = "";
                        fileName = "/images/JobApp/EmployeeImg/" + employee.Code + "-" + employee.ArName + ".jpeg";
                        var bytes = new byte[7000];
                        if (employee.Image.Contains("jpeg"))
                        {
                            bytes = Convert.FromBase64String(employee.Image.Replace("data:image/jpeg;base64,", ""));
                        }
                        else
                        {
                            bytes = Convert.FromBase64String(employee.Image.Replace("data:image/png;base64,", ""));
                        }
                        using (var imageFile = new FileStream(Server.MapPath(fileName), FileMode.Create))
                        {
                            imageFile.Write(bytes, 0, bytes.Length);
                            imageFile.Flush();
                        }
                        employee.Image = domainName + fileName;
                    }

                    if (employee.CV != null && employee.CV.Contains("base64"))
                    {
                        string fileName = "";
                        fileName = "/images/JobApp/EmployeeCv/" + employee.Code + "-" + employee.ArName + ".pdf";
                        var bytes = new byte[7000];
                        if (employee.CV.Contains("pdf"))
                        {
                            bytes = Convert.FromBase64String(employee.CV.Replace("data:application/pdf;base64,", ""));
                        }
                        else
                        {
                            bytes = Convert.FromBase64String(employee.CV.Replace("data:application/pdf;base64,", ""));
                        }
                        using (var CvFile = new FileStream(Server.MapPath(fileName), FileMode.Create))
                        {
                            CvFile.Write(bytes, 0, bytes.Length);
                            CvFile.Flush();
                        }
                        employee.CV = domainName + fileName;
                    }

                    db.Employees.Add(employee);


                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("Employee", "Add", "AddEdit", employee.Id, null, "الموظفين");
                }
                db.SaveChanges();

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = employee.Id > 0 ? "تعديل موظف" : "اضافة  موظف",
                    EnAction = "AddEdit",
                    ControllerName = "Employee",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "Post",
                    SelectedItem = employee.Id,
                    ArItemName = employee.ArName,
                    EnItemName = employee.EnName,
                    CodeOrDocNo = employee.Code
                });
                return Json(new { success = "true" });
            }

            //// drop down list of Church 
            //ViewBag.ChurchId = new SelectList(db.Churches.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            //{
            //    Id = b.Id,
            //    ArName = b.Code + " - " + b.ArName
            //}), "Id", "ArName", employee.ChurchId);

            //// drop down list of Gender
            //ViewBag.GenderId = new SelectList(db.Genders.Select(b => new
            //{
            //    Id = b.Id,
            //    ArName = b.ArName
            //}), "Id", "ArName", employee.GenderId);
            //// drop down list of Marital Status
            //ViewBag.MaritalStatusId = new SelectList(db.MaritalStatus.Select(b => new
            //{
            //    Id = b.Id,
            //    ArName = b.ArName
            //}), "Id", "ArName", employee.MaritalStatusId);

            //// drop down list of ApplicantWorkStatus
            //ViewBag.ApplicantWorkStatusId = new SelectList(db.ApplicantWorkStatus.Select(b => new
            //{
            //    Id = b.Id,
            //    ArName = b.ArName
            //}), "Id", "ArName", employee.ApplicantWorkStatusId);

            //// drop down list of Job
            //ViewBag.JobId = new SelectList(db.Jobs.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            //{
            //    Id = b.Id,
            //    ArName = b.Code + " - " + b.ArName
            //}), "Id", "ArName", employee.JobId);
            // return View(employee);
            var errors = ModelState
                     .Where(x => x.Value.Errors.Count > 0)
                     .Select(x => new { x.Key, x.Value.Errors })
                     .ToArray();

            return Json(new { success = "false", errors });
        }

        [SkipERPAuthorize]
        public JsonResult GetJobRequestDetailsByEmployeeId(int? id)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var Details = db.JobCandidates.Where(a => a.EmployeeId == id).Select(a => new
            {
                Id = a.Id,
                CompanyJobRequestId = a.CompanyJobRequestId,
                CompanyJobRequestNo = a.CompanyJobRequest.DocumentNumber,
                JobId = a.JobId,
                JobName = a.Job.ArName,
                JobStatusId = a.CompanyJobRequest.JobStatusId,
                JobStatusName = a.CompanyJobRequest.JobStatu.ArName,
                CompanyId = a.CompanyJobRequest.CompanyId,
                CompanyName = a.CompanyJobRequest.Company.ArName,
                JobRequestDate = a.CompanyJobRequest.JobRequestDate,
                EmployeeMobile = a.Employee.MobileNumber,
                CompanyNumber = a.CompanyJobRequest.Company.Mobile,
                Interviewer = a.CompanyJobRequest.Company.Interviewer


            }).ToList();
            return Json(Details, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult ActivateDeactivate(int id)
        {
            try
            {
                Employee employee = db.Employees.Find(id);
                if (employee.IsActive == true)
                {
                    employee.IsActive = false;
                }
                else
                {
                    employee.IsActive = true;
                }
                employee.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(employee).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = employee.Id > 0 ? "تنشيط موظف" : "إلغاء تنشيط موظف",
                    EnAction = "AddEdit",
                    ControllerName = "Employee",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "Post",
                    SelectedItem = employee.Id,
                    ArItemName = employee.ArName,
                    EnItemName = employee.EnName,
                    CodeOrDocNo = employee.Code
                });
                ////-------------------- Notification-------------------------////
                if (employee.IsActive == true)
                {
                    Notification.GetNotification("Employee", "Activate/Deactivate", "ActivateDeactivate", id, true, "الموظفين");
                }
                else
                {

                    Notification.GetNotification("Employee", "Activate/Deactivate", "ActivateDeactivate", id, false, "الموظفين");
                }
                ///////-----------------------------------------------------------------------


                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }
        // POST: Employee/Delete/5
        [HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                Employee employee = db.Employees.Find(id);
                employee.IsDeleted = true;
                employee.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                //foreach (var detail in employee.EmployeeExperiences)
                //{
                //    db.Entry(detail).State = EntityState.Deleted;
                //}
                //foreach (var detail in employee.EmployeeJobs)
                //{
                //    db.Entry(detail).State = EntityState.Deleted;
                //}
                Random random = new Random();
                const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
                employee.Code = Code;
                employee.FieldsCodingId = null;
                db.Entry(employee).State = EntityState.Modified;

                db.SaveChanges();

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف موظف",
                    EnAction = "AddEdit",
                    ControllerName = "Employee",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = employee.EnName,
                    ArItemName = employee.ArName,
                    CodeOrDocNo = employee.Code
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("Employee", "Delete", "Delete", id, null, "الموظفين");

                ///////-----------------------------------------------------------------------

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
                    var code = db.Employees.Where(a => a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
                    var code = db.Employees.Where(a => a.Code.Contains(fixedPart) && a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
                var code = db.Employees.Where(a => a.FieldsCodingId == null && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
            //var code = QueryHelper.CodeLastNum("Employee");
            //return Json(code + 1, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ImportExcelFile()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ImportExcelFile(HttpPostedFileBase excelfile)
        {
            string Error;
            if (excelfile == null || excelfile.ContentLength == 0)
            {
                Error = "من فضلك اختر ملف";
                return Json(Error, JsonRequestBehavior.AllowGet);
            }
            else
            {
                if (excelfile.FileName.EndsWith("xls") || excelfile.FileName.EndsWith("xlsx"))
                {
                    string path = Server.MapPath("~/Content/" + excelfile.FileName);
                    if (System.IO.File.Exists(path))
                    {
                        System.IO.File.Delete(path);
                    }
                    excelfile.SaveAs(path);
                    List<Employee> employees = new List<Employee>();

                    //------------------------- Work With Excel Without Download On Server -------------------------------------------//
                    SpreadsheetDocument spreadsheet = SpreadsheetDocument.Open(path, false);
                    WorkbookPart workbookPart = spreadsheet.WorkbookPart;
                    WorksheetPart worksheetPart = workbookPart.WorksheetParts.Last();
                    SheetData sheetData = worksheetPart.Worksheet.Elements<SheetData>().Last();

                    // ----------- Insert Into Data Table To Read Blank Cells From Excel ------- //
                    // Solve Ignoring Empty Cells  Problemm 

                    DataTable dt = new DataTable();
                    IEnumerable<Row> rowss = sheetData.Descendants<Row>();
                    foreach (Cell cell in rowss.ElementAt(0))
                    {
                        dt.Columns.Add(GetCellValue(spreadsheet, cell));
                    }

                    foreach (Row row in rowss) //this will also include your header row...
                    {
                        DataRow tempRow = dt.NewRow();
                        sum = 0;
                        for (int i = 0; i < row.Descendants<Cell>().Count(); i++)
                        {
                            Cell cell = row.Descendants<Cell>().ElementAt(i);
                            var xa = cell.CellReference;
                            int actualCellIndex = CellReferenceToIndex(cell);
                            tempRow[actualCellIndex] = GetCellValue(spreadsheet, cell);
                        }
                        var x = tempRow.ItemArray;
                        dt.Rows.Add(tempRow);
                    }
                    var DataTableRows = dt.Rows;
                    var EmployeeCode = new JavaScriptSerializer().Serialize(SetCodeNum(null).Data).ToString().Trim('"');
                    for (int row = 1; row < DataTableRows.Count; row++)
                    {
                        var RowData = DataTableRows[row];
                        Employee record = new Employee();

                        //------------------------  New Excel ---------------------------//
                        if (row == 1)
                        {
                            record.Code = (double.Parse(EmployeeCode)).ToString();
                        }
                        else
                        {
                            record.Code = (double.Parse(EmployeeCode) + row - 1).ToString();
                        }
                        //record.Code = RowData[0].ToString().Length > 0 /*!= null*/ ? (RowData[0]).ToString() : null;
                        record.EmpCodeOnFingerPrint = RowData[1].ToString().Length > 0 /*!= null*/ ? (RowData[1]).ToString() : null;
                        record.ArName = RowData[2].ToString().Length > 0 /*!= null*/ ? (RowData[2]).ToString() : null;
                        record.EnName = RowData[3].ToString().Length > 0 /*!= null*/ ? (RowData[3]).ToString() : null;
                        var nationalityCode = RowData[4].ToString().Length > 0 /*!= null*/ ? (RowData[4]).ToString() : null;
                        var nationality = db.Nationalities.Where(a => a.IsDeleted == false && a.IsActive == true && a.Code == nationalityCode).FirstOrDefault();
                        record.NationalityId = nationality.ToString().Length > 0 /*!= null*/ ? nationality.Id : 0;
                        record.NationalityId = record.NationalityId == 0 ? null : record.NationalityId;
                        record.GenderId = RowData[5].ToString().Length > 0 /*!= null*/ ? int.Parse(RowData[5].ToString()) : 0;
                        record.GenderId = record.GenderId == 0 ? null : record.GenderId;
                        record.ReligionId = RowData[6].ToString().Length > 0 /*!= null*/ ? int.Parse(RowData[6].ToString()) : 0;
                        record.ReligionId = record.ReligionId == 0 ? null : record.ReligionId;
                        record.MaritalStatusId = RowData[7].ToString().Length > 0 /*!= null*/ ? int.Parse(RowData[7].ToString()) : 0;
                        record.MaritalStatusId = record.MaritalStatusId == 0 ? null : record.MaritalStatusId;
                        if (RowData[8].ToString().Length > 0 /*!= null*/)
                        {
                            record.Birthdate = DateTime.Parse((RowData[8]).ToString());
                        }
                        if (RowData[9].ToString().Length > 0 /*!= null*/)
                        {
                            record.BirthdateHijri = DateTime.Parse((RowData[9]).ToString());
                        }
                        record.MobileNumber = RowData[10].ToString().Length > 0 /*!= null*/ ? (RowData[10]).ToString() : null;
                        var DeptCode = RowData[11].ToString().Length > 0 /*!= null*/ ? (RowData[11]).ToString() : null;
                        var Department = db.Departments.Where(a => a.IsActive == true && a.IsDeleted == false && a.Code == DeptCode).FirstOrDefault();
                        var DeptId = Department != null ? Department.Id : 0;
                        record.DepartmentId = DeptId;
                        if (record.DepartmentId == 0)
                        {
                            record.DepartmentId = null;
                        }
                        var hrDepartmentCode = RowData[12].ToString().Length > 0 /*!= null*/ ? (RowData[12]).ToString() : null;
                        var hrDepartment = db.HrDepartments.Where(a => a.IsActive == true && a.IsDeleted == false && a.Code == hrDepartmentCode).FirstOrDefault();
                        record.HrDepartmentId = hrDepartment != null ? hrDepartment.Id : 0;
                        record.HrDepartmentId = record.HrDepartmentId == 0 ? null : record.HrDepartmentId;
                        var CompanyjobCode = RowData[13] != null ? (RowData[13]).ToString() : null;
                        var job = db.Jobs.Where(a => a.IsActive == true && a.IsDeleted == false && a.Code == CompanyjobCode).FirstOrDefault();
                        record.JobId = job != null ? job.Id : 0;
                        record.JobId = record.JobId == 0 ? null : record.JobId;
                        record.WorkStatusId = RowData[14].ToString().Length > 0 /*!= null*/ ? int.Parse(RowData[14].ToString()) : 0;
                        record.WorkStatusId = record.WorkStatusId == 0 ? null : record.WorkStatusId;
                        record.NationalId = RowData[15].ToString().Length > 0 /*!= null*/ ? RowData[15].ToString() : null;
                        if (RowData[16].ToString().Length > 0 /*!= null*/)
                        {
                            record.ResidenceReleaseDate = DateTime.Parse((RowData[16]).ToString());
                        }
                        if (RowData[17].ToString().Length > 0 /*!= null*/)
                        {
                            record.ResidenceReleaseDateHijri = DateTime.Parse((RowData[17]).ToString());
                        }
                        if (RowData[18].ToString().Length > 0 /*!= null*/)
                        {
                            record.ResidenceExpiryDate = DateTime.Parse((RowData[18]).ToString());
                        }
                        if (RowData[19].ToString().Length > 0 /*!= null*/)
                        {
                            record.ResidenceExpiryDateHijri = DateTime.Parse((RowData[19]).ToString());
                        }
                        var ResidencejobCode = RowData[20].ToString().Length > 0 /*!= null*/ ? (RowData[20]).ToString() : null;
                        var Residencejob = db.Jobs.Where(a => a.IsActive == true && a.IsDeleted == false && a.Code == ResidencejobCode).FirstOrDefault();
                        record.NationalJobId = Residencejob != null ? Residencejob.Id : 0;
                        record.NationalJobId = record.NationalJobId == 0 ? null : record.NationalJobId;
                        record.PassportId = RowData[21].ToString().Length > 0 /*!= null*/ ? RowData[21].ToString() : null;
                        if (RowData[22].ToString().Length > 0 /*!= null*/)
                        {
                            record.PassportReleaseDate = DateTime.Parse((RowData[22]).ToString());
                        }
                        if (RowData[23].ToString().Length > 0 /*!= null*/)
                        {
                            record.PassportReleaseDateHijri = DateTime.Parse((RowData[23]).ToString());
                        }
                        if (RowData[24].ToString().Length > 0 /*!= null*/)
                        {
                            record.PassportExpiryDate = DateTime.Parse((RowData[24]).ToString());
                        }
                        if (RowData[25].ToString().Length > 0 /*!= null*/)
                        {
                            record.PassportExpiryDateHijri = DateTime.Parse((RowData[25]).ToString());
                        }
                        var PassportjobCode = RowData[26].ToString().Length > 0 /*!= null*/ ? (RowData[26]).ToString() : null;
                        var Passportjob = db.Jobs.Where(a => a.IsActive == true && a.IsDeleted == false && a.Code == PassportjobCode).FirstOrDefault();
                        record.PassportJobId = Passportjob != null ? Passportjob.Id : 0;
                        record.PassportJobId = record.PassportJobId == 0 ? null : record.PassportJobId;
                        if (RowData[27].ToString().Length > 0 /*!= null*/)
                        {
                            record.HireDate = DateTime.Parse((RowData[27]).ToString());
                        }
                        var ContractsTypeCode = RowData[28].ToString().Length > 0 /*!= null*/ ? (RowData[28]).ToString() : null;
                        var ContractsType = db.ContractsTypes.Where(a => a.IsActive == true && a.IsDeleted == false && a.Code == ContractsTypeCode).FirstOrDefault();
                        record.ContractsTypeId = ContractsType != null ? ContractsType.Id : 0;
                        record.ContractsTypeId = record.ContractsTypeId == 0 ? null : record.ContractsTypeId;
                        record.Email = (RowData[29]).ToString().Length > 0 /*!= null*/ ? (RowData[29]).ToString() : null;
                        record.IsActive = true;
                        record.IsDeleted = false;
                        //----------------------- End of New Excel --------------------------//

                        //if (row == 1)
                        //{
                        //    record.Code = (double.Parse(EmployeeCode)).ToString();
                        //}
                        //else
                        //{
                        //    record.Code = (double.Parse(EmployeeCode) + row).ToString();
                        //}
                        //record.ArName = RowData[0] != null ? (RowData[0]).ToString() : null;
                        //record.EnName = RowData[1] != null ? (RowData[1]).ToString() : null;
                        //record.Birthdate = DateTime.Parse((RowData[2]).ToString());
                        //record.ContractsTypeId = RowData[3] != null ? int.Parse((RowData[3]).ToString()) : 0;
                        //record.WorkStatusId = RowData[4] != null ? int.Parse((RowData[4]).ToString()) : 0;
                        //record.HireDate = DateTime.Parse((RowData[5]).ToString());
                        //record.Email = RowData[6] != null ? (RowData[6]).ToString() : null;
                        //record.MobileNumber = RowData[7] != null ? (RowData[7]).ToString() : null;
                        //record.EmployeeAddress = RowData[8] != null ? (RowData[8]).ToString() : null;
                        //record.NationalId = RowData[9] != null ? (RowData[9]).ToString() : null;
                        //record.IsActive = true;
                        //record.IsDeleted = false;
                        //var Dept = RowData[10] != null ? (RowData[10]).ToString() : null;
                        //var DepartmentId = db.Departments.Where(a => a.IsActive == true && a.IsDeleted == false && a.Code == Dept).FirstOrDefault();
                        //record.DepartmentId = DepartmentId != null ? DepartmentId.Id : 0;
                        //if (record.DepartmentId == 0)
                        //{
                        //    record.DepartmentId = null;
                        //}

                        employees.Add(record);
                    }
                    db.Employees.AddRange(employees);
                    try
                    {
                        db.SaveChanges();
                       // spreadsheet.Close();
                        return Json("success", JsonRequestBehavior.AllowGet);
                    }
                    catch (Exception ex)
                    {
                        var errors = ex.InnerException.InnerException.Message;
                    }
                    return Json("Error", JsonRequestBehavior.AllowGet);


                    // ----------- End Insert Into Data Table To Read Blank Cells From Excel ------- //

                    //ArrayList data = new ArrayList();
                    //// Get Cell Data To Fill data "ArrayList"
                    //foreach (Row r in sheetData.Elements<Row>())
                    //{
                    //    var sheetElm = sheetData.Elements<Row>();
                    //    var rows = new ArrayList();
                    //    foreach (Cell c in r.Elements<Cell>())
                    //    {
                    //        var Elem = r.Elements<Cell>();
                    //        if (c.DataType != null && c.DataType == CellValues.SharedString)
                    //        {
                    //            // Read String
                    //            var stringId = Convert.ToInt32(c.InnerText);
                    //            var stringValue = workbookPart.SharedStringTablePart.SharedStringTable.Elements<SharedStringItem>().ElementAt(stringId).InnerText;
                    //            rows.Add(stringValue);
                    //        }
                    //        else
                    //        {
                    //            if (c.CellReference.ToString().Contains("D") || c.CellReference.ToString().Contains("G")) //Attend/LeaveDate
                    //            {
                    //                // Read Date
                    //                var date = DateTime.FromOADate(double.Parse(c.CellValue.Text)).ToString("yyyy-MM-ddTHH:mm");//ToString("dd/MM/yyyy");
                    //                rows.Add(date);
                    //            }

                    //            //else if (c.CellReference.ToString().Contains("G")) //Attend/LeaveTime
                    //            //{
                    //            //    // Read Time
                    //            //    TimeSpan time = DateTime.FromOADate(double.Parse(c.CellValue.Text)).TimeOfDay;
                    //            //    rows.Add(time);
                    //            //}
                    //            else
                    //            {
                    //                // if DataType Null >> By Defult It Read Int 
                    //                rows.Add(c.CellValue.Text);
                    //            }
                    //        }
                    //    }
                    //    data.Add(rows);
                    //}
                    //// Loop on data "ArrayList" To Fill Employee Obj
                    //for (int row = 1; row < data.Count; row++)
                    //{
                    //    var RowData = (ArrayList)data[row];

                    //    Employee record = new Employee();
                    //    record.Code = RowData[0] != null ? (RowData[0]).ToString() : null;
                    //    record.ArName = RowData[1] != null ? (RowData[1]).ToString() : null;
                    //    record.EnName = RowData[2] != null ? (RowData[2]).ToString() : null;

                    //    record.Birthdate = DateTime.Parse((RowData[3]).ToString());
                    //    record.ContractsTypeId = RowData[4] != null ? int.Parse((RowData[4]).ToString()) : 0;
                    //    record.WorkStatusId = RowData[5] != null ? int.Parse((RowData[5]).ToString()) : 0;
                    //    record.HireDate = DateTime.Parse((RowData[6]).ToString());
                    //    record.Email = RowData[7] != null ? (RowData[7]).ToString() : null;
                    //    record.MobileNumber = RowData[8] != null ? (RowData[8]).ToString() : null;
                    //    record.EmployeeAddress = RowData[9] != null ? (RowData[9]).ToString() : null;
                    //    record.NationalId = RowData[10] != null ? (RowData[10]).ToString() : null;
                    //    var Dept = RowData[11] != null ? (RowData[11]).ToString() : null;
                    //    var DepartmentId = db.Departments.Where(a => a.IsActive == true && a.IsDeleted == false && a.Code == Dept).FirstOrDefault().Id;
                    //    record.DepartmentId = DepartmentId;

                    //    employees.Add(record);


                    //}
                    ////----------------------- End of Work With Excel Without Download On Server -----------------------//

                    //db.Employees.AddRange(employees);
                    //db.SaveChanges();

                    //spreadsheet.Close();
                    //return Json("success", JsonRequestBehavior.AllowGet);
                }
                else
                {
                    Error = "نوع الملف غير صحيح";
                    return Json(Error, JsonRequestBehavior.AllowGet);
                }
            }
        }


        public static string GetCellValue(SpreadsheetDocument document, Cell cell)
        {
            SharedStringTablePart stringTablePart = document.WorkbookPart.SharedStringTablePart;
            //string value = cell.CellValue.InnerXml;
            string value = cell.CellValue != null ? cell.CellValue.InnerXml : null;


            if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
            {
                return stringTablePart.SharedStringTable.ChildElements[Int32.Parse(value)].InnerText;
            }
            else if (cell.CellReference.ToString().Contains("I") || cell.CellReference.ToString().Contains("J") //BIRTHDATE
                || cell.CellReference.ToString().Contains("Q") || cell.CellReference.ToString().Contains("R") // RESIDENCE_RELEASE
                || cell.CellReference.ToString().Contains("S") || cell.CellReference.ToString().Contains("T") //RESIDENCE_EXPITR
                || cell.CellReference.ToString().Contains("W") || cell.CellReference.ToString().Contains("X") //PASSPORT_RELEASE
                || cell.CellReference.ToString().Contains("Y") || cell.CellReference.ToString().Contains("Z") //PASSPORT_EXPIRE
                || cell.CellReference.ToString().Contains("AB")) //HireDate
            {
                // Read Date
                var date = DateTime.FromOADate(double.Parse(cell.CellValue.Text)).ToString("yyyy-MM-ddTHH:mm");//ToString("dd/MM/yyyy");
                return date;
            }

            else
            {
                return value;
            }
        }
        private static int CellReferenceToIndex(Cell cell)
        {
            int index = 0;
            string reference = cell.CellReference.ToString().ToUpper();
            if (reference.Length == 2)
            {
                foreach (char ch in reference)
                {
                    if (Char.IsLetter(ch))
                    {
                        int value = (int)ch - (int)'A';
                        index = (index == 0) ? value : ((index + 1) * 26) + value;
                    }
                    else
                    {
                        return index;
                    }
                }
                return index;
            }

            foreach (char ch in reference)
            {
                if (Char.IsLetter(ch))
                {
                    int value = (int)ch - (int)'A';
                    index = value + 25 + 1  /*(index == 0) ? value : ((index + 1) * 26) + value*/;
                }
                else
                {
                    return index;
                }
            }
            return index;
            //sum++;
            //index = 25 + sum;
            //return index;
        }
        static int sum = 0;

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
