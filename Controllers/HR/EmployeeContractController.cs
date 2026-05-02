using MyERP.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Controllers.HR
{
    public class EmployeeContractController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: EmployeeContract
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة عقود الموظفين",
                EnAction = "Index",
                ControllerName = "EmployeeContract",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("EmployeeContract", "View", "Index", null, null, " عقود الموظفين");
            //////////-----------------------------------------------------------------------
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<EmployeeContract> employeeContracts;

            if (string.IsNullOrEmpty(searchWord))
            {
                employeeContracts = db.EmployeeContracts.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.EmployeeContracts.Where(s => s.IsDeleted == false).Count();

            }
            else
            {
                employeeContracts = db.EmployeeContracts.Where(s => s.IsDeleted == false
                && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.HireDate.ToString().Contains(searchWord) || s.Notes.Contains(searchWord)
                ||s.Employee.ArName.Contains(searchWord) || s.Employee.EnName.Contains(searchWord)
                ||s.ContractType.ArName.Contains(searchWord) || s.ContractType.EnName.Contains(searchWord)
                ||s.ContractsStatu.ArName.Contains(searchWord)|| s.ContractsStatu.EnName.Contains(searchWord)

                )).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.EmployeeContracts.Where(s => s.IsDeleted == false 
                && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.HireDate.ToString().Contains(searchWord)|| s.Notes.Contains(searchWord) 
                || s.Employee.ArName.Contains(searchWord) || s.Employee.EnName.Contains(searchWord)
                || s.ContractType.ArName.Contains(searchWord) || s.ContractType.EnName.Contains(searchWord)
                || s.ContractsStatu.ArName.Contains(searchWord) || s.ContractsStatu.EnName.Contains(searchWord)

                )).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(employeeContracts.ToList());
        }

        // GET: EmployeeContract/Edit/5
        public ActionResult AddEdit(int? id)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);


            if (id == null)
            {
                // drop down list of  Contract Period Type
                ViewBag.ContractPeriodTypeId = new SelectList(db.PeriodTypes.Select(b => new
                {
                    Id = b.Id,
                    ArName = b.ArName
                }), "Id", "ArName");

                // drop down list of  Test Period Type
                ViewBag.TestPeriodTypeId = new SelectList(db.PeriodTypes.Select(b => new
                {
                    Id = b.Id,
                    ArName = b.ArName
                }), "Id", "ArName");

                // drop down list of Employee
                ViewBag.EmployeeId = new SelectList(db.Employees.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                // drop down list of Job
                ViewBag.ContractJobId = new SelectList(db.Jobs.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                // drop down list of Contracts Type
                ViewBag.ContractsTypeId = new SelectList(db.ContractsTypes.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");


                // drop down list of Contract status
                ViewBag.ContractsStatusId = new SelectList(db.ContractsStatus.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                // drop down list of Contract Type
                ViewBag.ContractTypeId = new SelectList(db.ContractTypes.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
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

                // drop down list of Employee salary item
                ViewBag.EmployeeSalaryItemId = new SelectList(db.SalaryItems.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                
                // drop down list of Employee Insurance category
                ViewBag.EmployeeInsuranceCategoryId = new SelectList(db.InsuranceCategories.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                      
                // drop down list of Wife Insurance category
                ViewBag.WifeInsuranceCategoryId = new SelectList(db.InsuranceCategories.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                // drop down list of Vacation Due Period Type
                ViewBag.VacationDuePeriodTypeId = new SelectList(db.PeriodTypes.Select(b => new
                {
                    Id = b.Id,
                    ArName = b.ArName
                }), "Id", "ArName");

                // drop down list of Vacation equivalent work Period Type
                ViewBag.VacationEquivalentWorkPeriodId = new SelectList(db.PeriodTypes.Select(b => new
                {
                    Id = b.Id,
                    ArName = b.ArName
                }), "Id", "ArName");

                // drop down list of Vacation Period Type
                ViewBag.VacationPeriodTypeId = new SelectList(db.PeriodTypes.Select(b => new
                {
                    Id = b.Id,
                    ArName = b.ArName
                }), "Id", "ArName");


                // drop down list of Vacation salary item
                ViewBag.VacationsSalaryItemId = new SelectList(db.SalaryItems.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                // drop down list of Annual Increase salary item
                ViewBag.AnnualIncreasesSalaryItemId = new SelectList(db.SalaryItems.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");


                // drop down list of End of service salary item
                ViewBag.EndOfServicesSalaryItemId = new SelectList(db.SalaryItems.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                return View();
            }
            EmployeeContract employeeContract = db.EmployeeContracts.Find(id);
            if (employeeContract == null)
            {
                return HttpNotFound();
            }
            ViewBag.Next = QueryHelper.Next((int)id, "EmployeeContract");
            ViewBag.Previous = QueryHelper.Previous((int)id, "EmployeeContract");
            ViewBag.Last = QueryHelper.GetLast("EmployeeContract");
            ViewBag.First = QueryHelper.GetFirst("EmployeeContract");

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل عقد الموظف",
                EnAction = "AddEdit",
                ControllerName = "EmployeeContract",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = employeeContract.Id,
                ArItemName = employeeContract.ArName,
                EnItemName = employeeContract.EnName,
                CodeOrDocNo = employeeContract.Code
            });

            // drop down list of  Contract Period Type
            ViewBag.ContractPeriodTypeId = new SelectList(db.PeriodTypes.Select(b => new
            {
                Id = b.Id,
                ArName = b.ArName
            }), "Id", "ArName", employeeContract.ContractPeriodTypeId);

            // drop down list of  Test Period Type
            ViewBag.TestPeriodTypeId = new SelectList(db.PeriodTypes.Select(b => new
            {
                Id = b.Id,
                ArName = b.ArName
            }), "Id", "ArName", employeeContract.TestPeriodTypeId);


            // drop down list of Employee
            ViewBag.EmployeeId = new SelectList(db.Employees.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", employeeContract.EmployeeId);

            // drop down list of Job
            ViewBag.ContractJobId = new SelectList(db.Jobs.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", employeeContract.ContractJobId);

            // drop down list of Contracts Type
            ViewBag.ContractsTypeId = new SelectList(db.ContractsTypes.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", employeeContract.ContractsTypeId);


            // drop down list of Contract status
            ViewBag.ContractsStatusId = new SelectList(db.ContractsStatus.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", employeeContract.ContractsStatusId);

            // drop down list of Contract Type
            ViewBag.ContractTypeId = new SelectList(db.ContractTypes.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", employeeContract.ContractTypeId);

            // drop down list of Administrative Departments
            ViewBag.AdministrativeDepartmentId = new SelectList(db.AdministrativeDepartments.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", employeeContract.AdministrativeDepartmentId);

            // drop down list of Employee salary item
            ViewBag.EmployeeSalaryItemId = new SelectList(db.SalaryItems.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");

            // drop down list of Employee category
            ViewBag.EmployeeInsuranceCategoryId = new SelectList(db.InsuranceCategories.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", employeeContract.EmployeeInsuranceCategoryId);

            // drop down list of Wife Insurance category
            ViewBag.WifeInsuranceCategoryId = new SelectList(db.InsuranceCategories.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", employeeContract.WifeInsuranceCategoryId);

            // drop down list of Vacation Due Period Type
            ViewBag.VacationDuePeriodTypeId = new SelectList(db.PeriodTypes.Select(b => new
            {
                Id = b.Id,
                ArName = b.ArName
            }), "Id", "ArName", employeeContract.VacationDuePeriodTypeId);

            // drop down list of Vacation equivalent work Period Type
            ViewBag.VacationEquivalentWorkPeriodId = new SelectList(db.PeriodTypes.Select(b => new
            {
                Id = b.Id,
                ArName = b.ArName
            }), "Id", "ArName" , employeeContract.VacationEquivalentWorkPeriodId);

            // drop down list of Vacation Period Type
            ViewBag.VacationPeriodTypeId = new SelectList(db.PeriodTypes.Select(b => new
            {
                Id = b.Id,
                ArName = b.ArName
            }), "Id", "ArName" , employeeContract.VacationPeriodTypeId);

            // drop down list of Vacation salary item
            ViewBag.VacationsSalaryItemId = new SelectList(db.SalaryItems.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");

            // drop down list of Annual Increase salary item
            ViewBag.AnnualIncreasesSalaryItemId = new SelectList(db.SalaryItems.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");


            // drop down list of End of service salary item
            ViewBag.EndOfServicesSalaryItemId = new SelectList(db.SalaryItems.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");


            try
            {
                ViewBag.ContractStartTime = employeeContract.ContractStartTime.Value.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.ContractEndTime = employeeContract.ContractEndTime.Value.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.HireDate = employeeContract.HireDate.Value.ToString("yyyy-MM-ddTHH:mm");
            }
            catch (Exception)
            {
            }
            return View(employeeContract);
        }

        // POST: Employee/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult AddEdit(EmployeeContract employeeContract)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            if (ModelState.IsValid)
            {
                var id = employeeContract.Id;
                employeeContract.IsDeleted = false;
                if (employeeContract.Id > 0)
                {
                    employeeContract.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                    // use another object to prevent entity error
                    var old = db.EmployeeContracts.Find(id);
                    db.EmployeeContractSalaryItems.RemoveRange(db.EmployeeContractSalaryItems.Where(p => p.EmployeeContractId == old.Id).ToList());
                    db.EmployeeContractVacations.RemoveRange(db.EmployeeContractVacations.Where(p => p.EmployeeContractId == old.Id).ToList());
                    db.EmployeeContractAnnualIncreases.RemoveRange(db.EmployeeContractAnnualIncreases.Where(p => p.EmployeeContractId == old.Id).ToList());
                    db.EmployeeContractEndOfServices.RemoveRange(db.EmployeeContractEndOfServices.Where(p => p.EmployeeContractId == old.Id).ToList());
                    //old.ArName = employeeContract.ArName;
                    //old.EnName = employeeContract.EnName;
                    old.ContractStartTime = employeeContract.ContractStartTime;
                    old.ContractEndTime = employeeContract.ContractEndTime;
                    old.EmployeeId = employeeContract.EmployeeId;
                    old.ContractPeriodNum = employeeContract.ContractPeriodNum;
                    old.ContractPeriodTypeId = employeeContract.ContractPeriodTypeId;
                    old.TestPeriodNum = employeeContract.TestPeriodNum;
                    old.TestPeriodTypeId = employeeContract.TestPeriodTypeId;
                    old.ContractJobId = employeeContract.ContractJobId;
                    old.ContractsTypeId = employeeContract.ContractsTypeId;
                    old.ContractsStatusId = employeeContract.ContractsStatusId;
                    old.ContractTypeId = employeeContract.ContractTypeId;
                    old.AdministrativeDepartmentId = employeeContract.AdministrativeDepartmentId;
                    old.HireDate = employeeContract.HireDate;
                    old.EmployeeHasTicket = employeeContract.EmployeeHasTicket;
                    old.WifeHasTicket = employeeContract.WifeHasTicket;
                    old.RelativesHasTicket = employeeContract.RelativesHasTicket;
                    old.TicketsNum = employeeContract.TicketsNum;
                    old.TicketsTotalValue = employeeContract.TicketsTotalValue;
                    old.InsuranceNum = employeeContract.InsuranceNum;
                    old.EmployeeHasInsurance = employeeContract.EmployeeHasInsurance;
                    old.EmployeeInsuranceCategoryId = employeeContract.EmployeeInsuranceCategoryId;
                    old.WifeHasInsurance = employeeContract.WifeHasInsurance;
                    old.WifeInsuranceCategoryId = employeeContract.WifeInsuranceCategoryId;
                    old.VacationDuePeriodNum = employeeContract.VacationDuePeriodNum;
                    old.VacationDuePeriodTypeId = employeeContract.VacationDuePeriodTypeId;
                    old.VacationEquivalentWorkPeriodNum = employeeContract.VacationEquivalentWorkPeriodNum;
                    old.VacationEquivalentWorkPeriodId = employeeContract.VacationEquivalentWorkPeriodId;
                    old.VacationSalaryIncreaseValue = employeeContract.VacationSalaryIncreaseValue;
                    old.VacationPeriodNum = employeeContract.VacationPeriodNum;
                    old.VacationPeriodTypeId = employeeContract.VacationPeriodTypeId;
                    old.SalaryFixedIncreaseValue = employeeContract.SalaryFixedIncreaseValue;
                    old.SalaryIncreaseRatio = employeeContract.SalaryIncreaseRatio;
                    old.EmployeeTotalSalary = employeeContract.EmployeeTotalSalary;
                    old.Sequence = employeeContract.Sequence;

                    foreach (var item in employeeContract.EmployeeContractSalaryItems)
                    {
                        old.EmployeeContractSalaryItems.Add(item);
                    }
                    foreach (var item in employeeContract.EmployeeContractVacations)
                    {
                        old.EmployeeContractVacations.Add(item);
                    }
                    foreach (var item in employeeContract.EmployeeContractAnnualIncreases)
                    {
                        old.EmployeeContractAnnualIncreases.Add(item);
                    }
                    foreach (var item in employeeContract.EmployeeContractEndOfServices)
                    {
                        old.EmployeeContractEndOfServices.Add(item);
                    }
                    db.Entry(old).State = EntityState.Modified;
                    
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("EmployeeContract", "Edit", "AddEdit", employeeContract.Id, null, "عقود الموظفين");
                }
                else
                {
                    employeeContract.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    //employee.Code= (QueryHelper.CodeLastNum("Employee") + 1).ToString();
                    employeeContract.IsActive = true;
                   
                    db.EmployeeContracts.Add(employeeContract);


                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("EmployeeContract", "Add", "AddEdit", employeeContract.Id, null, "عقود الموظفين");
                }
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = employeeContract.Id > 0 ? "تعديل عقد موظف" : "اضافة عقد موظف",
                    EnAction = "AddEdit",
                    ControllerName = "EmployeeContract",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "Post",
                    SelectedItem = employeeContract.Id,
                    ArItemName = employeeContract.ArName,
                    EnItemName = employeeContract.EnName,
                    CodeOrDocNo = employeeContract.Code
                });

                return Json(new { success = "true" });
                
            }


            // drop down list of  Contract Period Type
            ViewBag.ContractPeriodTypeId = new SelectList(db.PeriodTypes,"Id", "ArName", employeeContract.ContractPeriodTypeId);

            // drop down list of  Test Period Type
            ViewBag.TestPeriodTypeId = new SelectList(db.PeriodTypes,"Id", "ArName", employeeContract.TestPeriodTypeId);


            // drop down list of Employee
            ViewBag.EmployeeId = new SelectList(db.Employees.Where(c => c.IsDeleted == false && c.IsActive == true),"Id", "ArName", employeeContract.EmployeeId);

            // drop down list of Job
            ViewBag.ContractJobId = new SelectList(db.Jobs.Where(c => c.IsDeleted == false && c.IsActive == true),"Id", "ArName", employeeContract.ContractJobId);

            // drop down list of Contracts Type
            ViewBag.ContractsTypeId = new SelectList(db.ContractsTypes.Where(c => c.IsDeleted == false && c.IsActive == true),"Id", "ArName", employeeContract.ContractsTypeId);


            // drop down list of Contract status
            ViewBag.ContractsStatusId = new SelectList(db.ContractsStatus.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "ArName", employeeContract.ContractsStatusId);

            // drop down list of Contract Type
            ViewBag.ContractTypeId = new SelectList(db.ContractTypes.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "ArName", employeeContract.ContractTypeId);

            // drop down list of Administrative Departments
            ViewBag.AdministrativeDepartmentId = new SelectList(db.AdministrativeDepartments.Where(c => c.IsDeleted == false && c.IsActive == true),"Id", "ArName", employeeContract.AdministrativeDepartmentId);

            // drop down list of Employee salary item
            ViewBag.EmployeeSalaryItemId = new SelectList(db.SalaryItems.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "ArName");

            // drop down list of Employee category
            ViewBag.EmployeeInsuranceCategoryId = new SelectList(db.InsuranceCategories.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "ArName", employeeContract.EmployeeInsuranceCategoryId);

            // drop down list of Wife Insurance category
            ViewBag.WifeInsuranceCategoryId = new SelectList(db.InsuranceCategories.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "ArName", employeeContract.WifeInsuranceCategoryId);

            // drop down list of Vacation Due Period Type
            ViewBag.VacationDuePeriodTypeId = new SelectList(db.PeriodTypes, "Id", "ArName", employeeContract.VacationDuePeriodTypeId);

            // drop down list of Vacation equivalent work Period Type
            ViewBag.VacationEquivalentWorkPeriodId = new SelectList(db.PeriodTypes, "Id", "ArName", employeeContract.VacationEquivalentWorkPeriodId);

            // drop down list of Vacation Period Type
            ViewBag.VacationPeriodTypeId = new SelectList(db.PeriodTypes, "Id", "ArName", employeeContract.VacationPeriodTypeId);

            // drop down list of Vacation salary item
            ViewBag.VacationsSalaryItemId = new SelectList(db.SalaryItems.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "ArName");

            // drop down list of Annual Increase salary item
            ViewBag.AnnualIncreasesSalaryItemId = new SelectList(db.SalaryItems.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "ArName");

            // drop down list of End of service salary item
            ViewBag.EndOfServicesSalaryItemId = new SelectList(db.SalaryItems.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "ArName");

            return View(employeeContract);
        }

        [HttpPost]
        public ActionResult ActivateDeactivate(int id)
        {
            try
            {
                EmployeeContract employeeContract = db.EmployeeContracts.Find(id);
                if (employeeContract.IsActive == true)
                {
                    employeeContract.IsActive = false;
                }
                else
                {
                    employeeContract.IsActive = true;
                }
                employeeContract.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(employeeContract).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = employeeContract.Id > 0 ? "تنشيط عقد موظف" : "إلغاء تنشيط عقد موظف",
                    EnAction = "AddEdit",
                    ControllerName = "EmployeeContract",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "Post",
                    SelectedItem = employeeContract.Id,
                    ArItemName = employeeContract.ArName,
                    EnItemName = employeeContract.EnName,
                    CodeOrDocNo = employeeContract.Code
                });
                ////-------------------- Notification-------------------------////
                if (employeeContract.IsActive == true)
                {
                    Notification.GetNotification("EmployeeContract", "Activate/Deactivate", "ActivateDeactivate", id, true, "عقودالموظفين");
                }
                else
                {

                    Notification.GetNotification("EmployeeContract", "Activate/Deactivate", "ActivateDeactivate", id, false, "عقودالموظفين");
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
                EmployeeContract employeeContract = db.EmployeeContracts.Find(id);
                employeeContract.IsDeleted = true;
                employeeContract.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(employeeContract).State = EntityState.Modified;

                db.SaveChanges();

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف عقد موظف",
                    EnAction = "AddEdit",
                    ControllerName = "EmployeeContract",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = employeeContract.EnName,
                    ArItemName = employeeContract.ArName,
                    CodeOrDocNo = employeeContract.Code
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("EmployeeContract", "Delete", "Delete", id, null, " عقود الموظفين");

                ///////-----------------------------------------------------------------------

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
            var code = QueryHelper.CodeLastNum("EmployeeContract");
            return Json(code + 1, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult GetEmployeeContract(int? EmpId)
        {
            var contract = db.EmployeeContracts.Where(a=>a.IsDeleted==false&&a.IsActive==true&&a.EmployeeId==EmpId).FirstOrDefault();
            if(contract!=null)
            {
                return Json(new { success = "true" ,id=contract.Id},JsonRequestBehavior.AllowGet);
            }
            else
            return Json("false", JsonRequestBehavior.AllowGet);
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