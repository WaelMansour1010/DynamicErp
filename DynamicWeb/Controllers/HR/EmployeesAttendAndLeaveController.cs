using MyERP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using MyERP.Repository;
using System.Net;
using System.Data.Entity.Core.Objects;
using System.Data;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Cell = DocumentFormat.OpenXml.Spreadsheet.Cell;
using Row = DocumentFormat.OpenXml.Spreadsheet.Row;
using System.Text.RegularExpressions;
using System.Collections;

namespace MyERP.Controllers.HR
{
    public class EmployeesAttendAndLeaveController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: EmployeesAttendAndLeave
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
                ArAction = "فتح قائمة سجلات الحضور والإنصراف",
                EnAction = "Index",
                ControllerName = "EmployeeAttendAndLeave",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////--------------------Notification------------------------ -////
            Notification.GetNotification("EmployeeAttendAndLeave", "View", "Index", null, null, " سجلات الحضور والإنصراف");
            ////-----------------------------------------------------------------------
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<EmployeesAttendAndLeave> employeesAttendAndLeave;

            if (string.IsNullOrEmpty(searchWord))
            {
                if (userId == 1)
                {
                    employeesAttendAndLeave = db.EmployeesAttendAndLeaves.Where(s => s.IsDeleted == false && s.IsActive == true && (departmentId == 0 || s.DepartmentId == departmentId)).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.EmployeesAttendAndLeaves.Where(s => s.IsDeleted == false && s.IsActive == true && (departmentId == 0 || s.DepartmentId == departmentId)).Count();
                }
                else
                {
                    employeesAttendAndLeave = db.EmployeesAttendAndLeaves.Where(s => s.IsDeleted == false && s.IsActive == true && (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (departmentId == 0 || s.DepartmentId == departmentId)).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.EmployeesAttendAndLeaves.Where(s => s.IsDeleted == false && s.IsActive == true && (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (departmentId == 0 || s.DepartmentId == departmentId)).Count();
                }

            }
            else
            {
                if (userId == 1)
                {
                    employeesAttendAndLeave = db.EmployeesAttendAndLeaves.Where(s => s.IsDeleted == false && s.IsActive == true && (departmentId == 0 || s.DepartmentId == departmentId) && (s.DocumentNumber.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Date.ToString().Contains(searchWord) || s.Notes.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.EmployeesAttendAndLeaves.Where(s => s.IsDeleted == false && s.IsActive == true && (departmentId == 0 || s.DepartmentId == departmentId) && (s.DocumentNumber.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Department.EnName.Contains(searchWord) || s.Date.ToString().Contains(searchWord) || s.Notes.Contains(searchWord))).Count();
                }
                else
                {//EmployeeContracts
                    employeesAttendAndLeave = db.EmployeesAttendAndLeaves.Where(s => s.IsDeleted == false && s.IsActive == true && (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (departmentId == 0 || s.DepartmentId == departmentId) && (s.DocumentNumber.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Date.ToString().Contains(searchWord) || s.Notes.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.EmployeesAttendAndLeaves.Where(s => s.IsDeleted == false && s.IsActive == true && (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (departmentId == 0 || s.DepartmentId == departmentId) && (s.DocumentNumber.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Department.EnName.Contains(searchWord) || s.Date.ToString().Contains(searchWord) || s.Notes.Contains(searchWord))).Count();
                }
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(employeesAttendAndLeave.ToList());
        }

        //[HttpPost]
        //public JsonResult ImportExcelFile(HttpPostedFileBase excelfile)
        //{

        //    string Error;
        //    if (excelfile == null || excelfile.ContentLength == 0)
        //    {
        //        Error = "من فضلك اختر ملف";
        //        return Json(Error, JsonRequestBehavior.AllowGet);
        //    }
        //    else
        //    {
        //        if (excelfile.FileName.EndsWith("xls") || excelfile.FileName.EndsWith("xlsx"))
        //        {
        //            string path = Server.MapPath("~/Content/" + excelfile.FileName);
        //            if (System.IO.File.Exists(path))
        //            {
        //                System.IO.File.Delete(path);
        //            }
        //            excelfile.SaveAs(path);
        //            // read data from excel file

        //            Excel.Application application = new Excel.Application();
        //            Excel.Workbook workbook = application.Workbooks.Open(path);
        //            Excel.Worksheet worksheet = workbook.ActiveSheet;
        //            Excel.Range range = worksheet.UsedRange;
        //            List<EmployeesAttendAndLeaveDetail> AttendList = new List<EmployeesAttendAndLeaveDetail>();
        //            List<EmployeesAttendAndLeaveDetail> UnmatchedAttendList = new List<EmployeesAttendAndLeaveDetail>();
        //            List<List<EmployeesAttendAndLeaveDetail>> Result = new List<List<EmployeesAttendAndLeaveDetail>>();

        //            for (int row = 2; row <= range.Rows.Count; row++)
        //            {
        //                EmployeesAttendAndLeaveDetail record = new EmployeesAttendAndLeaveDetail();

        //                record.EmpCodeOnFingerPrint = ((Excel.Range)range.Cells[row, 1]).Text;
        //                // DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss")
        //                //   record.Attend_LeaveDateTime = DateTime.Parse(((Excel.Range)range.Cells[row, 4]).Text);
        //                var date = ((Excel.Range)range.Cells[row, 4]).Text;
        //                var time = ((Excel.Range)range.Cells[row, 5]).Text;
        //                var total = DateTime.Parse(date + " " + time);
        //                //record.Attend_LeaveDateTime = DateTime.Parse(total.ToString("MM/dd/yyyy HH:mm:ss"));

        //                var timeString = DateTime.Parse(total.ToString("MM/dd/yyyy HH:mm:ss"));
        //                var dateTimeToLocal = timeString.ToLocalTime();
        //                record.Attend_LeaveDateTime = dateTimeToLocal;
        //                record.Attend_LeaveDateTime = record.Attend_LeaveDateTime.Value.AddHours(-2);
        //                //  var x =   record.Attend_LeaveDateTime.Value.ToString("MM/dd/yyyy " + record.Attend_LeaveDateTime.Value.ToString("HH:mm:ss"));
        //                //record.Attend_LeaveTime = TimeSpan.Parse(((Excel.Range)range.Cells[row, 5]).Text);
        //                record.In_Out = int.Parse(((Excel.Range)range.Cells[row, 6]).Text);

        //                var emp = db.Employees.Where(a => a.EmpCodeOnFingerPrint == record.EmpCodeOnFingerPrint).FirstOrDefault();
        //                if (emp != null)
        //                {
        //                    record.EmployeeId = emp.Id;
        //                    record.EmployeeCode = emp.Code;
        //                    record.EmployeeArName = emp.ArName;
        //                    AttendList.Add(record);
        //                }
        //                else
        //                {
        //                    record.EmployeeCode = ((Excel.Range)range.Cells[row, 2]).Text;
        //                    record.EmployeeArName = ((Excel.Range)range.Cells[row, 3]).Text;
        //                    UnmatchedAttendList.Add(record);
        //                }


        //            }
        //            Result.Add(AttendList);
        //            Result.Add(UnmatchedAttendList);
        //            workbook.Close(0);
        //            application.Quit();
        //            return Json(Result, JsonRequestBehavior.AllowGet);
        //        }
        //        else
        //        {
        //            Error = "نوع الملف غير صحيح";
        //            return Json(Error, JsonRequestBehavior.AllowGet);
        //        }

        //    }
        //}

        [HttpPost]
        public JsonResult ImportExcelFile(HttpPostedFileBase excelfile)
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
                    List<EmployeesAttendAndLeaveDetail> AttendList = new List<EmployeesAttendAndLeaveDetail>();
                    List<EmployeesAttendAndLeaveDetail> UnmatchedAttendList = new List<EmployeesAttendAndLeaveDetail>();
                    List<EmployeesAttendAndLeaveDetail> EmployeesWithNoContractAttendLeaveDetails = new List<EmployeesAttendAndLeaveDetail>();
                    List<List<EmployeesAttendAndLeaveDetail>> Result = new List<List<EmployeesAttendAndLeaveDetail>>();
                    List<EmployeesAttendAndLeaveDetail> alreadyExistInDBAttendLeaveDetails = new List<EmployeesAttendAndLeaveDetail>();
                    List<EmployeesAttendAndLeaveDetail> tempAttendLeaveList = new List<EmployeesAttendAndLeaveDetail>();

                    //------------------------- Work With Excel Without Download On Server -------------------------------------------//
                    SpreadsheetDocument spreadsheet = SpreadsheetDocument.Open(path, false);
                    WorkbookPart workbookPart = spreadsheet.WorkbookPart;
                    WorksheetPart worksheetPart = workbookPart.WorksheetParts.First();
                    SheetData sheetData = worksheetPart.Worksheet.Elements<SheetData>().First();
                    ArrayList data = new ArrayList();
                    // Get Cell Data To Fill data "ArrayList"
                    foreach (Row r in sheetData.Elements<Row>())
                    {
                        var rows = new ArrayList();
                        foreach (Cell c in r.Elements<Cell>())
                        {
                            if (c.DataType != null && c.DataType == CellValues.SharedString)
                            {
                                // Read String
                                var stringId = Convert.ToInt32(c.InnerText);
                                var stringValue = workbookPart.SharedStringTablePart.SharedStringTable.Elements<SharedStringItem>().ElementAt(stringId).InnerText;
                                rows.Add(stringValue);
                            }
                            else
                            {
                                if (c.CellReference.ToString().Contains("F")) //Attend/LeaveDate
                                {
                                    // Read Date
                                    var date = DateTime.FromOADate(double.Parse(c.CellValue.Text)).ToString("dd/MM/yyyy");
                                    rows.Add(date);
                                }
                                else if (c.CellReference.ToString().Contains("G")) //Attend/LeaveTime
                                {
                                    // Read Time
                                    TimeSpan time = DateTime.FromOADate(double.Parse(c.CellValue.Text)).TimeOfDay;
                                    rows.Add(time);
                                }
                                else
                                {
                                    // if DataType Null >> By Defult It Read Int 
                                    rows.Add(c.CellValue.Text);
                                }
                            }
                        }
                        data.Add(rows);
                    }
                    // Loop on data "ArrayList" To Fill EmployeesAttendAndLeaveDetail Obj
                    for (int row = 1; row < data.Count; row++)
                    {
                        var RowData = (ArrayList)data[row];

                        EmployeesAttendAndLeaveDetail record = new EmployeesAttendAndLeaveDetail();
                        record.EmpCodeOnFingerPrint = (RowData[0]).ToString();
                        var date = (RowData[3]).ToString();
                        var time = (RowData[4]).ToString();
                        var total = Convert.ToDateTime(date + " " + time);
                        record.Attend_LeaveDateTime = total;
                        record.In_Out = Convert.ToInt32(RowData[5]);
                        var emp = db.Employees.Where(a => a.EmpCodeOnFingerPrint == record.EmpCodeOnFingerPrint).FirstOrDefault();
                        if (emp != null)
                        {
                            record.EmployeeId = emp.Id;
                            record.EmployeeCode = emp.Code;
                            record.EmployeeArName = emp.ArName;

                            // Check if employee has contract or not
                            var empContract = db.EmployeeContracts.Where(a => a.EmployeeId == emp.Id && a.IsActive == true && a.IsDeleted == false).FirstOrDefault();
                            if (empContract != null)
                            {
                                // Employee has contract
                                var EmpInAttendList = AttendList.Where(e => e.EmpCodeOnFingerPrint == record.EmpCodeOnFingerPrint && e.In_Out == record.In_Out).FirstOrDefault();

                                if (record.In_Out == 0)
                                {
                                    // Take First finger print in case of attend
                                    if (EmpInAttendList == null)
                                    {
                                        AttendList.Add(record);
                                        tempAttendLeaveList.Add(record);
                                    }
                                }
                                else
                                {
                                    // Take Last finger print in case of leave
                                    AttendList.Add(record);
                                    tempAttendLeaveList.Add(record);

                                    if (EmpInAttendList != null)
                                    {
                                        AttendList.Remove(EmpInAttendList);
                                        tempAttendLeaveList.Remove(record);
                                    }
                                }
                            }
                            else
                            {
                                // Employee does not have contract
                                EmployeesWithNoContractAttendLeaveDetails.Add(record);
                            }
                        }
                        else
                        {
                            record.EmployeeCode = (RowData[1]).ToString();
                            record.EmployeeArName = (RowData[2]).ToString();

                            var EmpInUnmatchedAttendList = UnmatchedAttendList.Where(e => e.EmpCodeOnFingerPrint == record.EmpCodeOnFingerPrint && e.In_Out == record.In_Out).FirstOrDefault();
                            if (record.In_Out == 0)
                            {
                                // Take First finger print in case of attend
                                if (EmpInUnmatchedAttendList == null)
                                {
                                    UnmatchedAttendList.Add(record);
                                }
                            }
                            else
                            {
                                // Take Last finger print in case of leave
                                UnmatchedAttendList.Add(record);
                                if (EmpInUnmatchedAttendList != null)
                                {
                                    UnmatchedAttendList.Remove(EmpInUnmatchedAttendList);
                                }
                            }
                        }
                    }
                    //----------------------- End of Work With Excel Without Download On Server -----------------------//

                    // //-------------- if Excel Downloaded on Server -------------------//
                    //// read data from excel file
                    //Excel.Application application = new Excel.Application();
                    //Excel.Workbook workbook = application.Workbooks.Open(path);
                    //Excel.Worksheet worksheet = workbook.ActiveSheet;
                    //Excel.Range range = worksheet.UsedRange;
                    //for (int row = 2; row <= range.Rows.Count; row++)
                    //{
                    //    EmployeesAttendAndLeaveDetail record = new EmployeesAttendAndLeaveDetail();
                    //    record.EmpCodeOnFingerPrint = ((Excel.Range)range.Cells[row, 1]).Text;
                    //    // DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss")
                    //    //   record.Attend_LeaveDateTime = DateTime.Parse(((Excel.Range)range.Cells[row, 4]).Text);
                    //    var date = ((Excel.Range)range.Cells[row, 4]).Text;
                    //    var time = ((Excel.Range)range.Cells[row, 5]).Text;
                    //    var total = Convert.ToDateTime(date + " " + time);
                    //    //  var total =Convert.ToString(Convert.ToString(date), {dd/MM/yyyy)+" "+Convert.ToString(time);
                    //    //    var test=DateTime.ParseExact(date + " " + time, "dd/MM/yy h:mm:ss tt", CultureInfo.InvariantCulture);
                    //    //record.Attend_LeaveDateTime = DateTime.Parse(total.ToString("MM/dd/yyyy HH:mm:ss"));
                    //    // var timeString = DateTime.Parse(total.ToString("MM/dd/yyyy HH:mm:ss"));
                    //    //var dateTimeToLocal = timeString.ToLocalTime();
                    //    record.Attend_LeaveDateTime = total;
                    //    //  var x =   record.Attend_LeaveDateTime.Value.ToString("MM/dd/yyyy " + record.Attend_LeaveDateTime.Value.ToString("HH:mm:ss"));
                    //    //record.Attend_LeaveTime = TimeSpan.Parse(((Excel.Range)range.Cells[row, 5]).Text);
                    //    record.In_Out = int.Parse(((Excel.Range)range.Cells[row, 6]).Text);
                    //    var emp = db.Employees.Where(a => a.EmpCodeOnFingerPrint == record.EmpCodeOnFingerPrint).FirstOrDefault();
                    //    if (emp != null)
                    //    {
                    //        record.EmployeeId = emp.Id;
                    //        record.EmployeeCode = emp.Code;
                    //        record.EmployeeArName = emp.ArName;
                    //        // Check if employee has contract or not
                    //        var empContract = db.EmployeeContracts.Where(a => a.EmployeeId == emp.Id && a.IsActive == true && a.IsDeleted == false).FirstOrDefault();
                    //        if (empContract != null)
                    //        {
                    //            // Employee has contract
                    //            var EmpInAttendList = AttendList.Where(e => e.EmpCodeOnFingerPrint == record.EmpCodeOnFingerPrint && e.In_Out == record.In_Out).FirstOrDefault();
                    //            if (record.In_Out == 0)
                    //            {
                    //                // Take First finger print in case of attend
                    //                if (EmpInAttendList == null)
                    //                {
                    //                    AttendList.Add(record);
                    //                    tempAttendLeaveList.Add(record);
                    //                }
                    //            }
                    //            else
                    //            {
                    //                // Take Last finger print in case of leave
                    //                AttendList.Add(record);
                    //                tempAttendLeaveList.Add(record);
                    //                if (EmpInAttendList != null)
                    //                {
                    //                    AttendList.Remove(EmpInAttendList);
                    //                    tempAttendLeaveList.Remove(record);
                    //                }
                    //            }
                    //        }
                    //        else
                    //        {
                    //            // Employee does not have contract
                    //            EmployeesWithNoContractAttendLeaveDetails.Add(record);
                    //        }
                    //    }
                    //    else
                    //    {
                    //        record.EmployeeCode = ((Excel.Range)range.Cells[row, 2]).Text;
                    //        record.EmployeeArName = ((Excel.Range)range.Cells[row, 3]).Text;
                    //        var EmpInUnmatchedAttendList = UnmatchedAttendList.Where(e => e.EmpCodeOnFingerPrint == record.EmpCodeOnFingerPrint && e.In_Out == record.In_Out).FirstOrDefault();
                    //        if (record.In_Out == 0)
                    //        {
                    //            // Take First finger print in case of attend
                    //            if (EmpInUnmatchedAttendList == null)
                    //            {
                    //                UnmatchedAttendList.Add(record);
                    //                //tempAttendLeaveList.Add(record);
                    //            }
                    //        }
                    //        else
                    //        {
                    //            // Take Last finger print in case of leave
                    //            UnmatchedAttendList.Add(record);
                    //            //tempAttendLeaveList.Add(record);
                    //            if (EmpInUnmatchedAttendList != null)
                    //            {
                    //                UnmatchedAttendList.Remove(EmpInUnmatchedAttendList);
                    //                //tempAttendLeaveList.Remove(record);
                    //            }
                    //        }
                    //        //UnmatchedAttendList.Add(record);
                    //    }
                    //}

                    // Check for attend and leave records that already exist in DB to remove from Excel records and not save them twice
                    foreach (var attendLeaveItem in tempAttendLeaveList)
                    {
                        var recordInDB = db.EmployeesAttendAndLeaveDetails.Where(e => e.EmpCodeOnFingerPrint == attendLeaveItem.EmpCodeOnFingerPrint && e.In_Out == attendLeaveItem.In_Out && e.Attend_LeaveDateTime.Value.Year == attendLeaveItem.Attend_LeaveDateTime.Value.Year && e.Attend_LeaveDateTime.Value.Month == attendLeaveItem.Attend_LeaveDateTime.Value.Month && e.Attend_LeaveDateTime.Value.Day == attendLeaveItem.Attend_LeaveDateTime.Value.Day).FirstOrDefault();

                        if (recordInDB != null)
                        {
                            alreadyExistInDBAttendLeaveDetails.Add(attendLeaveItem);
                            AttendList.Remove(attendLeaveItem);
                        }
                    }
                    Result.Add(AttendList);
                    Result.Add(UnmatchedAttendList);
                    Result.Add(alreadyExistInDBAttendLeaveDetails);
                    Result.Add(EmployeesWithNoContractAttendLeaveDetails);
                    //workbook.Close(0);
                    // application.Quit();
                 //   spreadsheet.Close();
                    return Json(Result, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    Error = "نوع الملف غير صحيح";
                    return Json(Error, JsonRequestBehavior.AllowGet);
                }
            }
        }


        //    GET: EmployeesAttendAndLeave/Edit/5
        public ActionResult AddEdit(int? id)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);

            if (id == null)
            {
                // drop down list of Department
                ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName");
                return View();
            }
            EmployeesAttendAndLeave e = db.EmployeesAttendAndLeaves.Find(id);
            if (e == null)
            {
                return HttpNotFound();
            }
            ViewBag.Next = QueryHelper.Next((int)id, "EmployeesAttendAndLeave");
            ViewBag.Previous = QueryHelper.Previous((int)id, "EmployeesAttendAndLeave");
            ViewBag.Last = QueryHelper.GetLast("EmployeesAttendAndLeave");
            ViewBag.First = QueryHelper.GetFirst("EmployeesAttendAndLeave");

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل سجل الحضور والإنصراف",
                EnAction = "AddEdit",
                ControllerName = "EmployeesAttendAndLeave",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = e.Id,
                CodeOrDocNo = e.DocumentNumber
            });

            // drop down list of Department
            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", e.DepartmentId);

            try
            {
                ViewBag.Date = e.Date.Value.ToString("yyyy-MM-ddTHH:mm");
            }
            catch (Exception)
            {
            }
            return View(e);
        }

        //    POST: Employee/Edit/5
        //     To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        //     more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        ////[ValidateAntiForgeryToken]
        //public ActionResult AddEdit(EmployeesAttendAndLeave e)
        //{
        //    var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
        //    string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
        //    if (ModelState.IsValid)
        //    {
        //        var id = e.Id;
        //        e.IsDeleted = false;
        //        if (e.Id > 0)
        //        {
        //            if (db.EmployeesAttendAndLeaves.Find(e.Id).IsPosted == true)
        //            {
        //                return Content("false");
        //            }
        //            e.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

        //            // update procedure 
        //            MyXML.xPathName = "Details";
        //            var E_Details = MyXML.GetXML(e.EmployeesAttendAndLeaveDetails);
        //            db.EmployeesAttendAndLeave_Update(e.Id, null, null, e.UserId, e.IsDeleted, e.IsActive, e.Date, e.DepartmentId, E_Details, e.IsPosted);

        //            /* //use another object to prevent entity error
        //             var old = db.EmployeesAttendAndLeaves.Find(id);
        //             db.EmployeesAttendAndLeaveDetails.RemoveRange(db.EmployeesAttendAndLeaveDetails.Where(p => p.EmployeesAttendAndLeaveId == old.Id).ToList());
        //             old.DocumentNumber = e.DocumentNumber;
        //             old.DepartmentId = e.DepartmentId;
        //             old.Date = e.Date;

        //             foreach (var item in e.EmployeesAttendAndLeaveDetails)
        //             {
        //                 old.EmployeesAttendAndLeaveDetails.Add(item);
        //             }

        //             db.Entry(old).State = EntityState.Modified;*/

        //            ///--------------------Notification------------------------ -////
        //            Notification.GetNotification("EmployeesAttendAndLeave", "Edit", "AddEdit", e.Id, null, "سجلات الحضور والانصراف");
        //        }
        //        else
        //        {
        //            e.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
        //            //employee.Code = (QueryHelper.CodeLastNum("Employee") + 1).ToString();
        //            e.IsActive = true;


        //            List<EmployeeLateness> Latenesses = new List<EmployeeLateness>();
        //            //List<EmployeeAbsence> Absences = new List<EmployeeAbsence>();
        //            List<OvertimeIssue> NewOvertimes = new List<OvertimeIssue>();
        //            List<OvertimeIssueDetial> NewOvertimesDetails = new List<OvertimeIssueDetial>();

        //            List<OvertimeIssue> OldOvertimes = new List<OvertimeIssue>();
        //            List<OvertimeIssueDetial> OldOvertimesDetails = new List<OvertimeIssueDetial>();




        //            // Details 
        //            MyXML.xPathName = "Details";
        //            foreach (var i in e.EmployeesAttendAndLeaveDetails)
        //            {
        //                // var x = DateTime.Parse(i.Attend_LeaveDateTime.ToString());
        //                //  i.Attend_LeaveDateTime = x.ToLocalTime();


        //                var Employee_ShiftId = db.ShiftEmployees.Where(a => a.EmployeeId == i.EmployeeId).Select(a => a.ShiftId).FirstOrDefault();
        //                var Shift = db.Shifts.Where(a => a.Id == Employee_ShiftId && a.IsActive == true && a.IsDeleted == false).FirstOrDefault();

        //                if (i.In_Out == 0)
        //                {

        //                    var ShiftStartTime = Shift.ShiftDetails.Where(a => a.IsDeleted == false && a.IsVacation == false && a.EnDay.StartsWith(i.Attend_LeaveDateTime.Value.ToString("ddd")) == true).FirstOrDefault() != null ? Shift.ShiftDetails.Where(a => a.IsDeleted == false && a.IsVacation == false && a.EnDay.StartsWith(i.Attend_LeaveDateTime.Value.ToString("ddd")) == true).FirstOrDefault().StartTime : null;

        //                    if (ShiftStartTime != null)
        //                    {

        //                        //var T1 = ShiftStartTime.Value.TotalMinutes;
        //                        //var T2 = (i.Attend_LeaveDateTime.Value.TimeOfDay).TotalMinutes - 120;
        //                        var T1 = (DateTime.Parse("4/15/2020 12:45:00 PM").TimeOfDay).TotalMinutes;
        //                        var T2 = ((DateTime.Parse("4/15/2020 13:45:00 PM")).TimeOfDay).TotalMinutes;


        //                        var Subtraction_Value = T1 - T2;
        //                        if (Subtraction_Value < 0)
        //                        {
        //                            EmployeeLateness EL = new EmployeeLateness();
        //                            EL.EmployeeId = i.EmployeeId;
        //                            EL.Date = i.Attend_LeaveDateTime;
        //                            EL.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
        //                            EL.LatenessHours = Math.Abs(Subtraction_Value)/60;
        //                            EL.IsDeleted = false;
        //                            EL.IsActive = true;

        //                            Latenesses.Add(EL);
        //                        }
        //                    }

        //                }
        //                else
        //                {
        //                    if (i.In_Out == 1)
        //                    {
        //                        var ShiftEndTime = Shift.ShiftDetails.Where(a => a.IsDeleted == false && a.IsVacation == false && a.EnDay.StartsWith(i.Attend_LeaveDateTime.Value.ToString("ddd")) == true).FirstOrDefault() != null? Shift.ShiftDetails.Where(a => a.IsDeleted == false && a.IsVacation == false && a.EnDay.StartsWith(i.Attend_LeaveDateTime.Value.ToString("ddd")) == true).FirstOrDefault().EndTime : null;

        //                        if (ShiftEndTime != null)
        //                        {
        //                            var T1 = ShiftEndTime.Value.TotalMinutes;
        //                            var T2 = (i.Attend_LeaveDateTime.Value.TimeOfDay).TotalMinutes - 120;

        //                            var Subtraction_Value = T1 - T2;
        //                            if (Subtraction_Value < 0)
        //                            {
        //                                OvertimeIssue OvertimeIssueRecord = db.OvertimeIssues.Where(a => a.EmployeeId == i.EmployeeId && a.IsDeleted == false && a.Month == i.Attend_LeaveDateTime.Value.Month && a.Year == i.Attend_LeaveDateTime.Value.Year).FirstOrDefault();

        //                                OvertimeIssueDetial issueDetial = new OvertimeIssueDetial();

        //                                if (OvertimeIssueRecord == null)
        //                                {
        //                                    OvertimeIssue OI = new OvertimeIssue();
        //                                    OI.EmployeeId = (int)i.EmployeeId;
        //                                    OI.DocumentNumber = (int.Parse(db.OvertimeIssues.OrderByDescending(q => q.Id).FirstOrDefault().DocumentNumber) + 1).ToString();
        //                                    OI.Month = i.Attend_LeaveDateTime.Value.Month;
        //                                    OI.Year = i.Attend_LeaveDateTime.Value.Year;
        //                                    OI.IsUsed = true;


        //                                    issueDetial.MainDocId = OI.Id;
        //                                    issueDetial.Date = (DateTime)i.Attend_LeaveDateTime;
        //                                    issueDetial.OvertimeTypeId = db.OvertimeTypes.Where(a => a.IsActive == true && a.IsDeleted == false).First().Id;
        //                                    issueDetial.Equivalent = Math.Abs(Subtraction_Value);
        //                                    issueDetial.Total = (db.EmployeeSalaryItems.Where(a => a.EmployeeId == i.EmployeeId).FirstOrDefault().Amount) / 240 *(decimal)Math.Abs(Subtraction_Value);
        //                                    OI.OvertimeIssueDetials.Add(issueDetial);
        //                                    NewOvertimes.Add(OI);
        //                                    NewOvertimesDetails.Add(issueDetial);
        //                                }
        //                                else
        //                                {

        //                                    issueDetial.MainDocId = OvertimeIssueRecord.Id;
        //                                    issueDetial.Date = (DateTime)i.Attend_LeaveDateTime;
        //                                    issueDetial.OvertimeTypeId = db.OvertimeTypes.Where(a => a.IsActive == true && a.IsDeleted == false).First().Id;
        //                                    issueDetial.Equivalent = Math.Abs(Subtraction_Value);
        //                                    issueDetial.Total = (db.EmployeeSalaryItems.Where(a => a.EmployeeId == i.EmployeeId).FirstOrDefault().Amount) / 240 * (decimal)Math.Abs(Subtraction_Value);
        //                                    OvertimeIssueRecord.OvertimeIssueDetials.Add(issueDetial);
        //                                    OldOvertimes.Add(OvertimeIssueRecord);
        //                                    OldOvertimesDetails.Add(issueDetial);

        //                                }
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //            var E_Details = MyXML.GetXML(e.EmployeesAttendAndLeaveDetails);
        //            MyXML.xPathName = "LatenessDetails";
        //            var Lateness_Details = MyXML.GetXML(Latenesses);

        //            MyXML.xPathName = "NewOvertimes";
        //            var NewOvertime = MyXML.GetXML(NewOvertimes);

        //            MyXML.xPathName = "NewOvertimesDetails";
        //            var NewOvertimes_Details = MyXML.GetXML(NewOvertimesDetails);

        //            MyXML.xPathName = "OldOvertimes";
        //            var OldOvertime = MyXML.GetXML(OldOvertimes);

        //            MyXML.xPathName = "OldOvertimesDetails";
        //            var OldOvertimes_Details = MyXML.GetXML(OldOvertimesDetails);

        //            //var Lateness_Details = MyXML.GetXML(Latenesses);
        //            //var Overtime_Details = MyXML.GetXML(NewOvertimes);
        //            //var Absence_Details = MyXML.GetXML(Absences);
        //            //Out Id
        //            var idOut = new ObjectParameter("Id", typeof(Int32));
        //            db.EmployeesAttendAndLeave_Insert(idOut, null, null, e.UserId, e.IsDeleted, e.IsActive, e.Date, e.DepartmentId, E_Details, e.IsPosted, Lateness_Details, NewOvertime, OldOvertime, NewOvertimes_Details, OldOvertimes_Details);

        //            // db.EmployeesAttendAndLeaves.Add(e);


        //            ///--------------------Notification------------------------ -////
        //            Notification.GetNotification("EmployeesAttendAndLeave", "Add", "AddEdit", e.Id, null, "سجلات الحضور والانصراف");
        //        }
        //        // db.SaveChanges();
        //        QueryHelper.AddLog(new MyLog()
        //        {
        //            ArAction = e.Id > 0 ? "تعديل سجل حضور وانصراف" : "اضافة سجل حضور وانصراف",
        //            EnAction = "AddEdit",
        //            ControllerName = "EmployeesAttendAndLeave",
        //            UserName = User.Identity.Name,
        //            UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
        //            LogDate = DateTime.Now,
        //            RequestMethod = "Post",
        //            SelectedItem = e.Id,
        //            CodeOrDocNo = e.DocumentNumber
        //        });

        //        return Json(new { success = "true" });

        //    }

        //    return View(e);
        //}

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult AddEdit(EmployeesAttendAndLeave e)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);

            if (ModelState.IsValid)
            {
                var id = e.Id;
                e.IsDeleted = false;
                if (e.Id > 0)
                {
                    e.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                    // update procedure 
                    MyXML.xPathName = "Details";
                    var E_Details = MyXML.GetXML(e.EmployeesAttendAndLeaveDetails);
                    //db.EmployeesAttendAndLeave_Update(e.Id, null, null, e.UserId, e.IsDeleted, e.IsActive, e.Date, e.DepartmentId, E_Details);

                    HandleAttendAndLeave(e);



                    /* //use another object to prevent entity error
                     var old = db.EmployeesAttendAndLeaves.Find(id);
                     db.EmployeesAttendAndLeaveDetails.RemoveRange(db.EmployeesAttendAndLeaveDetails.Where(p => p.EmployeesAttendAndLeaveId == old.Id).ToList());
                     old.DocumentNumber = e.DocumentNumber;
                     old.DepartmentId = e.DepartmentId;
                     old.Date = e.Date;

                     foreach (var item in e.EmployeesAttendAndLeaveDetails)
                     {
                         old.EmployeesAttendAndLeaveDetails.Add(item);
                     }

                     db.Entry(old).State = EntityState.Modified;*/

                    ///--------------------Notification------------------------ -////
                    Notification.GetNotification("EmployeesAttendAndLeave", "Edit", "AddEdit", e.Id, null, "سجلات الحضور والانصراف");
                }
                else
                {
                    e.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    //employee.Code = (QueryHelper.CodeLastNum("Employee") + 1).ToString();
                    e.IsActive = true;


                    HandleAttendAndLeave(e);

                    // db.EmployeesAttendAndLeaves.Add(e);


                    ///--------------------Notification------------------------ -////
                    Notification.GetNotification("EmployeesAttendAndLeave", "Add", "AddEdit", e.Id, null, "سجلات الحضور والانصراف");
                }
                // db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = e.Id > 0 ? "تعديل سجل حضور وانصراف" : "اضافة سجل حضور وانصراف",
                    EnAction = "AddEdit",
                    ControllerName = "EmployeesAttendAndLeave",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "Post",
                    SelectedItem = e.Id,
                    CodeOrDocNo = e.DocumentNumber
                });

                return Json(new { success = "true" });

            }

            return View(e);
        }

        public void HandleAttendAndLeave(EmployeesAttendAndLeave e)
        {
            //List<EmployeeLateness> Latenesses = new List<EmployeeLateness>();
            //List<EmployeeAbsence> Absences = new List<EmployeeAbsence>();
            ////List<OvertimeIssue> NewOvertimeIssue = new List<OvertimeIssue>();
            ////List<OvertimeIssueDetial> NewOvertimesDetails = new List<OvertimeIssueDetial>();
            ////List<OvertimeIssueDetial> OldOvertimesDetails = new List<OvertimeIssueDetial>();

            //List<object> NewOvertimesDetails = new List<object>();
            //List<object> OldOvertimesDetails = new List<object>();

            //List<OvertimeIssue> OldOvertimes = new List<OvertimeIssue>();
            //List<OvertimeIssue> NewOvertimes = new List<OvertimeIssue>();


            DataTable EDetailsDT = new DataTable("EDetailsDT");
            DataTable LatenessDetailsDT = new DataTable("LatenessDetails");
            DataTable NewOvertimeDT = new DataTable("NewOvertimes");
            DataTable NewOvertimesDetailsDT = new DataTable("NewOvertimesDetails");
            DataTable OldOvertimeDT = new DataTable("OldOvertimes");
            DataTable OldOvertimesDetailsDT = new DataTable("OldOvertimesDetails");
            DataTable NewPenalitiesDT = new DataTable("NewPenalties");
            DataTable NewPenalitiesDetailsDT = new DataTable("NewPenaltiesDetails");
            DataTable OldPenalitiesDT = new DataTable("OldPenalties");
            DataTable OldPenalitiesDetailsDT = new DataTable("OldPenaltiesDetails");

            DataTable NewAbsentPenalitiesDT = new DataTable("NewAbsentPenalties");
            DataTable NewAbsentPenalitiesDetailsDT = new DataTable("NewAbsentPenaltiesDetails");

            DataColumn EDetailId = new DataColumn("Id", typeof(int));
            DataColumn EmpCodeOnFingerPrint = new DataColumn("EmpCodeOnFingerPrint", typeof(string));
            DataColumn EmployeeArName = new DataColumn("EmployeeArName", typeof(string));
            DataColumn EmployeeId = new DataColumn("EmployeeId", typeof(int));
            DataColumn EmployeesAttendAndLeaveId = new DataColumn("EmployeesAttendAndLeaveId", typeof(int));
            DataColumn IsDeleted = new DataColumn("IsDeleted", typeof(bool));
            DataColumn In_Out = new DataColumn("In_Out", typeof(int));
            DataColumn Attend_LeaveDateTime = new DataColumn("Attend_LeaveDateTime", typeof(DateTime));

            DataColumn LatenessDetailsEmployeeId = new DataColumn("EmployeeId", typeof(int));
            DataColumn LatenessDetailsLatenessHours = new DataColumn("LatenessHours", typeof(double));
            DataColumn LatenessDetailsDate = new DataColumn("Date", typeof(DateTime));
            DataColumn LatenessDetailsUserId = new DataColumn("UserId", typeof(int));
            DataColumn LatenessDetailsIsDeleted = new DataColumn("IsDeleted", typeof(bool));
            DataColumn LatenessDetailsIsActive = new DataColumn("IsActive", typeof(bool));

            DataColumn NewOvertimeId = new DataColumn("Id", typeof(int));
            DataColumn NewOvertimeEmployeeId = new DataColumn("EmployeeId", typeof(int));
            DataColumn NewOvertimeDocumentNumber = new DataColumn("DocumentNumber", typeof(string));
            DataColumn NewOvertimeMonth = new DataColumn("Month", typeof(int));
            DataColumn NewOvertimeYear = new DataColumn("Year", typeof(int));
            DataColumn NewOvertimeIsDeleted = new DataColumn("IsDeleted", typeof(bool));
            DataColumn NewOvertimeIsActive = new DataColumn("IsActive", typeof(bool));

            DataColumn OldOvertimeId = new DataColumn("Id", typeof(int));
            DataColumn OldOvertimeEmployeeId = new DataColumn("EmployeeId", typeof(int));
            DataColumn OldOvertimeDocumentNumber = new DataColumn("DocumentNumber", typeof(string));
            DataColumn OldOvertimeMonth = new DataColumn("Month", typeof(int));
            DataColumn OldOvertimeYear = new DataColumn("Year", typeof(int));
            DataColumn OldOvertimeIsDeleted = new DataColumn("IsDeleted", typeof(bool));
            DataColumn OldOvertimeIsActive = new DataColumn("IsActive", typeof(bool));

            DataColumn NewPenalityId = new DataColumn("Id", typeof(int));
            DataColumn NewPenalityEmployeeId = new DataColumn("EmployeeId", typeof(int));
            DataColumn NewPenalityDocumentNumber = new DataColumn("DocumentNumber", typeof(string));
            DataColumn NewPenalityMonth = new DataColumn("Month", typeof(int));
            DataColumn NewPenalityYear = new DataColumn("Year", typeof(int));
            DataColumn NewPenalityIsDeleted = new DataColumn("IsDeleted", typeof(bool));
            DataColumn NewPenalityIsPosted = new DataColumn("IsPosted", typeof(bool));
            DataColumn NewPenalityDate = new DataColumn("Date", typeof(DateTime));

            DataColumn OldPenalityId = new DataColumn("Id", typeof(int));
            DataColumn OldPenalityEmployeeId = new DataColumn("EmployeeId", typeof(int));
            DataColumn OldPenalityDocumentNumber = new DataColumn("DocumentNumber", typeof(string));
            DataColumn OldPenalityMonth = new DataColumn("Month", typeof(int));
            DataColumn OldPenalityYear = new DataColumn("Year", typeof(int));
            DataColumn OldPenalityIsDeleted = new DataColumn("IsDeleted", typeof(bool));
            DataColumn OldPenalityIsPosted = new DataColumn("IsPosted", typeof(bool));
            DataColumn OldPenalityDate = new DataColumn("Date", typeof(DateTime));


            DataColumn NewOvertimesDetailsId = new DataColumn("Id", typeof(int));
            DataColumn NewOvertimesDetailsEmployeeId = new DataColumn("EmployeeId", typeof(int));
            DataColumn NewOvertimesDetailsMainDocId = new DataColumn("MainDocId", typeof(int));
            DataColumn NewOvertimesDetailsDate = new DataColumn("Date", typeof(DateTime));
            DataColumn NewOvertimesDetailsOvertimeTypeId = new DataColumn("OvertimeTypeId", typeof(int));
            DataColumn NewOvertimesDetailsEquivalent = new DataColumn("Equivalent", typeof(double));
            DataColumn NewOvertimesDetailsTotal = new DataColumn("Total", typeof(decimal));

            DataColumn OldOvertimesDetailsId = new DataColumn("Id", typeof(int));
            DataColumn OldOvertimesDetailsEmployeeId = new DataColumn("EmployeeId", typeof(int));
            DataColumn OldOvertimesDetailsMainDocId = new DataColumn("MainDocId", typeof(int));
            DataColumn OldOvertimesDetailsDate = new DataColumn("Date", typeof(DateTime));
            DataColumn OldOvertimesDetailsOvertimeTypeId = new DataColumn("OvertimeTypeId", typeof(int));
            DataColumn OldOvertimesDetailsEquivalent = new DataColumn("Equivalent", typeof(double));
            DataColumn OldOvertimesDetailsTotal = new DataColumn("Total", typeof(decimal));

            DataColumn NewPenalitiesDetailsId = new DataColumn("Id", typeof(int));
            DataColumn NewPenalitiesDetailsEmployeeId = new DataColumn("EmployeeId", typeof(int));
            DataColumn NewPenalitiesDetailsMainDocId = new DataColumn("MainDocId", typeof(int));
            DataColumn NewPenalitiesDetailsDate = new DataColumn("Date", typeof(DateTime));
            DataColumn NewPenalitiesDetailsPenalityTypeId = new DataColumn("PenaltyTypeId", typeof(int));
            DataColumn NewPenalitiesDetailsEquivalent = new DataColumn("Equivalent", typeof(double));
            DataColumn NewPenalitiesDetailsTotal = new DataColumn("Total", typeof(decimal));
            DataColumn NewPenalitiesDetailsCause = new DataColumn("Cause", typeof(string));

            DataColumn OldPenalitiesDetailsId = new DataColumn("Id", typeof(int));
            DataColumn OldPenalitiesDetailsEmployeeId = new DataColumn("EmployeeId", typeof(int));
            DataColumn OldPenalitiesDetailsMainDocId = new DataColumn("MainDocId", typeof(int));
            DataColumn OldPenalitiesDetailsDate = new DataColumn("Date", typeof(DateTime));
            DataColumn OldPenalitiesDetailsPenalityTypeId = new DataColumn("PenaltyTypeId", typeof(int));
            DataColumn OldPenalitiesDetailsEquivalent = new DataColumn("Equivalent", typeof(double));
            DataColumn OldPenalitiesDetailsTotal = new DataColumn("Total", typeof(decimal));
            DataColumn OldPenalitiesDetailsCause = new DataColumn("Cause", typeof(string));

            // Absent Employees

            DataColumn NewAbsentPenalityId = new DataColumn("Id", typeof(int));
            DataColumn NewAbsentPenalityEmployeeId = new DataColumn("EmployeeId", typeof(int));
            DataColumn NewAbsentPenalityDocumentNumber = new DataColumn("DocumentNumber", typeof(string));
            DataColumn NewAbsentPenalityMonth = new DataColumn("Month", typeof(int));
            DataColumn NewAbsentPenalityYear = new DataColumn("Year", typeof(int));
            DataColumn NewAbsentPenalityIsDeleted = new DataColumn("IsDeleted", typeof(bool));
            DataColumn NewAbsentPenalityIsPosted = new DataColumn("IsPosted", typeof(bool));
            DataColumn NewAbsentPenalityDate = new DataColumn("Date", typeof(DateTime));

            DataColumn NewAbsentPenalitiesDetailsId = new DataColumn("Id", typeof(int));
            DataColumn NewAbsentPenalitiesDetailsEmployeeId = new DataColumn("EmployeeId", typeof(int));
            DataColumn NewAbsentPenalitiesDetailsMainDocId = new DataColumn("MainDocId", typeof(int));
            DataColumn NewAbsentPenalitiesDetailsDate = new DataColumn("Date", typeof(DateTime));
            DataColumn NewAbsentPenalitiesDetailsPenalityTypeId = new DataColumn("PenaltyTypeId", typeof(int));
            DataColumn NewAbsentPenalitiesDetailsEquivalent = new DataColumn("Equivalent", typeof(double));
            DataColumn NewAbsentPenalitiesDetailsTotal = new DataColumn("Total", typeof(decimal));
            DataColumn NewAbsentPenalitiesDetailsCause = new DataColumn("Cause", typeof(string));


            EDetailsDT.Columns.Add(EDetailId);
            EDetailsDT.Columns.Add(EmpCodeOnFingerPrint);
            EDetailsDT.Columns.Add(EmployeeArName);
            EDetailsDT.Columns.Add(EmployeeId);
            EDetailsDT.Columns.Add(EmployeesAttendAndLeaveId);
            EDetailsDT.Columns.Add(IsDeleted);
            EDetailsDT.Columns.Add(In_Out);
            EDetailsDT.Columns.Add(Attend_LeaveDateTime);

            LatenessDetailsDT.Columns.Add(LatenessDetailsEmployeeId);
            LatenessDetailsDT.Columns.Add(LatenessDetailsLatenessHours);
            LatenessDetailsDT.Columns.Add(LatenessDetailsDate);
            LatenessDetailsDT.Columns.Add(LatenessDetailsUserId);
            LatenessDetailsDT.Columns.Add(LatenessDetailsIsDeleted);
            LatenessDetailsDT.Columns.Add(LatenessDetailsIsActive);

            NewOvertimeDT.Columns.Add(NewOvertimeId);
            NewOvertimeDT.Columns.Add(NewOvertimeDocumentNumber);
            NewOvertimeDT.Columns.Add(NewOvertimeMonth);
            NewOvertimeDT.Columns.Add(NewOvertimeYear);
            NewOvertimeDT.Columns.Add(NewOvertimeEmployeeId);
            NewOvertimeDT.Columns.Add(NewOvertimeIsDeleted);
            NewOvertimeDT.Columns.Add(NewOvertimeIsActive);

            OldOvertimeDT.Columns.Add(OldOvertimeId);
            OldOvertimeDT.Columns.Add(OldOvertimeDocumentNumber);
            OldOvertimeDT.Columns.Add(OldOvertimeMonth);
            OldOvertimeDT.Columns.Add(OldOvertimeYear);
            OldOvertimeDT.Columns.Add(OldOvertimeEmployeeId);
            OldOvertimeDT.Columns.Add(OldOvertimeIsDeleted);
            OldOvertimeDT.Columns.Add(OldOvertimeIsActive);

            NewPenalitiesDT.Columns.Add(NewPenalityId);
            NewPenalitiesDT.Columns.Add(NewPenalityDocumentNumber);
            NewPenalitiesDT.Columns.Add(NewPenalityMonth);
            NewPenalitiesDT.Columns.Add(NewPenalityYear);
            NewPenalitiesDT.Columns.Add(NewPenalityEmployeeId);
            NewPenalitiesDT.Columns.Add(NewPenalityIsDeleted);
            NewPenalitiesDT.Columns.Add(NewPenalityIsPosted);
            NewPenalitiesDT.Columns.Add(NewPenalityDate);

            OldPenalitiesDT.Columns.Add(OldPenalityId);
            OldPenalitiesDT.Columns.Add(OldPenalityDocumentNumber);
            OldPenalitiesDT.Columns.Add(OldPenalityMonth);
            OldPenalitiesDT.Columns.Add(OldPenalityYear);
            OldPenalitiesDT.Columns.Add(OldPenalityEmployeeId);
            OldPenalitiesDT.Columns.Add(OldPenalityIsDeleted);
            OldPenalitiesDT.Columns.Add(OldPenalityIsPosted);
            OldPenalitiesDT.Columns.Add(OldPenalityDate);

            NewOvertimesDetailsDT.Columns.Add(NewOvertimesDetailsId);
            NewOvertimesDetailsDT.Columns.Add(NewOvertimesDetailsMainDocId);
            NewOvertimesDetailsDT.Columns.Add(NewOvertimesDetailsDate);
            NewOvertimesDetailsDT.Columns.Add(NewOvertimesDetailsOvertimeTypeId);
            NewOvertimesDetailsDT.Columns.Add(NewOvertimesDetailsEquivalent);
            NewOvertimesDetailsDT.Columns.Add(NewOvertimesDetailsTotal);
            NewOvertimesDetailsDT.Columns.Add(NewOvertimesDetailsEmployeeId);

            OldOvertimesDetailsDT.Columns.Add(OldOvertimesDetailsId);
            OldOvertimesDetailsDT.Columns.Add(OldOvertimesDetailsMainDocId);
            OldOvertimesDetailsDT.Columns.Add(OldOvertimesDetailsDate);
            OldOvertimesDetailsDT.Columns.Add(OldOvertimesDetailsOvertimeTypeId);
            OldOvertimesDetailsDT.Columns.Add(OldOvertimesDetailsEquivalent);
            OldOvertimesDetailsDT.Columns.Add(OldOvertimesDetailsTotal);
            OldOvertimesDetailsDT.Columns.Add(OldOvertimesDetailsEmployeeId);

            NewPenalitiesDetailsDT.Columns.Add(NewPenalitiesDetailsId);
            NewPenalitiesDetailsDT.Columns.Add(NewPenalitiesDetailsEmployeeId);
            NewPenalitiesDetailsDT.Columns.Add(NewPenalitiesDetailsMainDocId);
            NewPenalitiesDetailsDT.Columns.Add(NewPenalitiesDetailsDate);
            NewPenalitiesDetailsDT.Columns.Add(NewPenalitiesDetailsPenalityTypeId);
            NewPenalitiesDetailsDT.Columns.Add(NewPenalitiesDetailsEquivalent);
            NewPenalitiesDetailsDT.Columns.Add(NewPenalitiesDetailsTotal);
            NewPenalitiesDetailsDT.Columns.Add(NewPenalitiesDetailsCause);

            OldPenalitiesDetailsDT.Columns.Add(OldPenalitiesDetailsId);
            OldPenalitiesDetailsDT.Columns.Add(OldPenalitiesDetailsEmployeeId);
            OldPenalitiesDetailsDT.Columns.Add(OldPenalitiesDetailsMainDocId);
            OldPenalitiesDetailsDT.Columns.Add(OldPenalitiesDetailsDate);
            OldPenalitiesDetailsDT.Columns.Add(OldPenalitiesDetailsPenalityTypeId);
            OldPenalitiesDetailsDT.Columns.Add(OldPenalitiesDetailsEquivalent);
            OldPenalitiesDetailsDT.Columns.Add(OldPenalitiesDetailsTotal);
            OldPenalitiesDetailsDT.Columns.Add(OldPenalitiesDetailsCause);

            // Absence Penality 
            NewAbsentPenalitiesDT.Columns.Add(NewAbsentPenalityId);
            NewAbsentPenalitiesDT.Columns.Add(NewAbsentPenalityDocumentNumber);
            NewAbsentPenalitiesDT.Columns.Add(NewAbsentPenalityMonth);
            NewAbsentPenalitiesDT.Columns.Add(NewAbsentPenalityYear);
            NewAbsentPenalitiesDT.Columns.Add(NewAbsentPenalityEmployeeId);
            NewAbsentPenalitiesDT.Columns.Add(NewAbsentPenalityIsDeleted);
            NewAbsentPenalitiesDT.Columns.Add(NewAbsentPenalityIsPosted);
            NewAbsentPenalitiesDT.Columns.Add(NewAbsentPenalityDate);

            NewAbsentPenalitiesDetailsDT.Columns.Add(NewAbsentPenalitiesDetailsId);
            NewAbsentPenalitiesDetailsDT.Columns.Add(NewAbsentPenalitiesDetailsEmployeeId);
            NewAbsentPenalitiesDetailsDT.Columns.Add(NewAbsentPenalitiesDetailsMainDocId);
            NewAbsentPenalitiesDetailsDT.Columns.Add(NewAbsentPenalitiesDetailsDate);
            NewAbsentPenalitiesDetailsDT.Columns.Add(NewAbsentPenalitiesDetailsPenalityTypeId);
            NewAbsentPenalitiesDetailsDT.Columns.Add(NewAbsentPenalitiesDetailsEquivalent);
            NewAbsentPenalitiesDetailsDT.Columns.Add(NewAbsentPenalitiesDetailsTotal);
            NewAbsentPenalitiesDetailsDT.Columns.Add(NewAbsentPenalitiesDetailsCause);

            var Shift_Id = 0;
            List<int?> empIds = new List<int?>();
            List<EmployeesAttendAndLeaveDetail> AttendedEmps = new List<EmployeesAttendAndLeaveDetail>();
            List<DateTime?> EmpDates = new List<DateTime?>();
            // Details 
            foreach (var i in e.EmployeesAttendAndLeaveDetails)
            {
                AttendedEmps.Add(i);
                var Employee_ShiftId = db.ShiftEmployees.Where(a => a.EmployeeId == i.EmployeeId).Select(a => a.ShiftId).FirstOrDefault();
                Shift_Id = (int)Employee_ShiftId;
                empIds.Add(i.EmployeeId);
                var Shift = db.Shifts.Where(a => a.Id == Employee_ShiftId && a.IsActive == true && a.IsDeleted == false).FirstOrDefault();
                var LocalDateTime = DateTime.Parse(i.Attend_LeaveDateTime.ToString()).ToUniversalTime().AddHours(2);
                // عشان احل مشكلة ال timezone "فرق الساعتين "
                var offset = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow);
                i.Attend_LeaveDateTime = DateTime.Parse((i.Attend_LeaveDateTime.Value.AddHours(offset.Hours)).ToString());
                //----------------------//
                if (i.In_Out == 0)
                {
                    EmpDates.Add(LocalDateTime);
                    var ShiftStartTime = Shift.ShiftDetails.Where(a => a.IsDeleted == false && a.IsVacation == false && a.EnDay.StartsWith(i.Attend_LeaveDateTime.Value.ToString("ddd")) == true).FirstOrDefault() != null ? Shift.ShiftDetails.Where(a => a.IsDeleted == false && a.IsVacation == false && a.EnDay.StartsWith(i.Attend_LeaveDateTime.Value.ToString("ddd")) == true).FirstOrDefault().StartTime : null;

                    if (ShiftStartTime != null)
                    {
                        var ShiftStartTimeTotalMinutes = ShiftStartTime.Value.TotalMinutes;

                        var EmployeeAttendTimeTotalMinutes = (LocalDateTime.TimeOfDay).TotalMinutes;

                        // Subtraction Value of Shift Start Time and Employee Attend Time
                        var Subtraction_Value = ShiftStartTimeTotalMinutes - EmployeeAttendTimeTotalMinutes;
                        if (Subtraction_Value < 0)
                        {
                            // Check for employee permission
                            //                            var employeePermission = db.LateAttendancePermissions.Where(a => a.IsDeleted == false && a.EmployeeId == i.EmployeeId &&
                            //a.ShiftId == Employee_ShiftId && a.PermissionDate.Value.Day == LocalDateTime.Day && a.PermissionDate.Value.Month == LocalDateTime.Month && a.PermissionDate.Value.Year == LocalDateTime.Year && a.PermissionDate.Value.Hour == LocalDateTime.Hour && a.PermissionDate.Value.Minute == LocalDateTime.Minute && a.IsApproved == true).FirstOrDefault();

                            // Check for employee Late Attendance permission
                            var employeePermission = db.LateAttendancePermissions.Where(a => a.IsDeleted == false && a.EmployeeId == i.EmployeeId &&
a.ShiftId == Employee_ShiftId && a.PermissionDate.Value.Day == LocalDateTime.Day && a.PermissionDate.Value.Month == LocalDateTime.Month && a.PermissionDate.Value.Year == LocalDateTime.Year && a.IsApproved == true).FirstOrDefault();

                            var timeShouldEmpAttendIn = employeePermission != null ? ShiftStartTimeTotalMinutes + (employeePermission.NoOfHours * 60) : ShiftStartTimeTotalMinutes;

                            if (employeePermission == null || EmployeeAttendTimeTotalMinutes > timeShouldEmpAttendIn)
                            {
                                // Add Employee Lateness
                                EmployeeLateness EL = new EmployeeLateness();
                                EL.EmployeeId = i.EmployeeId;
                                EL.Date = LocalDateTime;
                                EL.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                                EL.LatenessHours = Math.Abs(Subtraction_Value / 60);
                                //Latenesses.Add(EL);

                                DataRow row = LatenessDetailsDT.NewRow();
                                row["EmployeeId"] = EL.EmployeeId;
                                row["Date"] = EL.Date;
                                row["LatenessHours"] = EL.LatenessHours;
                                row["UserId"] = EL.UserId;
                                row["IsDeleted"] = false;
                                row["IsActive"] = true;

                                LatenessDetailsDT.Rows.Add(row);

                                // Add Penalty for Employee Lateness
                                PenaltyIssue penaltyIssue = db.PenaltyIssues.Where(a => a.EmployeeId == i.EmployeeId && a.IsDeleted == false && a.Month == LocalDateTime.Month && a.Year == LocalDateTime.Year).FirstOrDefault();

                                PenaltyIssueDetail penaltyIssueDetial = new PenaltyIssueDetail();

                                if (penaltyIssue == null)
                                {
                                    PenaltyIssue PI = new PenaltyIssue();
                                    PI.EmployeeId = (int)i.EmployeeId;
                                    PI.DocumentNumber = db.PenaltyIssues.OrderByDescending(q => q.Id).FirstOrDefault() != null ? (int.Parse(db.PenaltyIssues.OrderByDescending(q => q.Id).FirstOrDefault().DocumentNumber) + 1).ToString() : "1";
                                    PI.Month = i.Attend_LeaveDateTime.Value.Month;
                                    PI.Year = i.Attend_LeaveDateTime.Value.Year;

                                    PI.PenaltyIssueDetails = null;

                                    DataRow dataRow = NewPenalitiesDT.NewRow();
                                    dataRow["Id"] = PI.Id;
                                    dataRow["EmployeeId"] = PI.EmployeeId;
                                    dataRow["DocumentNumber"] = PI.DocumentNumber;
                                    dataRow["Month"] = PI.Month;
                                    dataRow["Year"] = PI.Year;
                                    dataRow["IsDeleted"] = false;
                                    //dataRow["IsPosted"] = null;
                                    dataRow["Date"] = e.Date;

                                    NewPenalitiesDT.Rows.Add(dataRow);


                                    DataRow Row = NewPenalitiesDetailsDT.NewRow();
                                    Row["Date"] = LocalDateTime;
                                    Row["EmployeeId"] = (int)i.EmployeeId;
                                    Row["MainDocId"] = PI.Id;
                                    Row["PenaltyTypeId"] = db.PenaltyTypes.Where(a => a.IsActive == true && a.IsDeleted == false).First().Id;
                                    Row["Equivalent"] = Math.Abs(Subtraction_Value / 60);
                                    Row["Total"] = ((decimal)db.EmployeeContracts.Where(a => a.EmployeeId == i.EmployeeId && a.IsActive == true && a.IsDeleted == false).FirstOrDefault().EmployeeTotalSalary) / 240 * ((decimal)Math.Abs(Subtraction_Value / 60));
                                    Row["Cause"] = "حضور متأخر";

                                    NewPenalitiesDetailsDT.Rows.Add(Row);
                                }
                                else
                                {

                                    //penaltyIssueDetial.MainDocId = penaltyIssue.Id;
                                    //penaltyIssueDetial.Date = LocalDateTime;
                                    //penaltyIssueDetial.PenaltyTypeId = db.PenaltyTypes.Where(a => a.IsActive == true && a.IsDeleted == false).First().Id;
                                    //penaltyIssueDetial.Equivalent = Math.Abs(Subtraction_Value / 60);
                                    //penaltyIssueDetial.Total = ((decimal)db.EmployeeContracts.Where(a => a.EmployeeId == i.EmployeeId && a.IsActive == true && a.IsDeleted == false).FirstOrDefault().EmployeeTotalSalary) / 240 * ((decimal)Math.Abs(Subtraction_Value / 60));
                                    //penaltyIssueDetial.Cause = "انصراف مبكر";

                                    penaltyIssue.PenaltyIssueDetails = null;

                                    DataRow dataRow = OldPenalitiesDT.NewRow();
                                    dataRow["Id"] = penaltyIssue.Id;
                                    dataRow["EmployeeId"] = penaltyIssue.EmployeeId;
                                    dataRow["DocumentNumber"] = penaltyIssue.DocumentNumber;
                                    dataRow["Month"] = penaltyIssue.Month;
                                    dataRow["Year"] = penaltyIssue.Year;
                                    dataRow["IsDeleted"] = false;
                                    //dataRow["IsPosted"] = null;
                                    OldPenalitiesDT.Rows.Add(dataRow);


                                    DataRow Row = OldPenalitiesDetailsDT.NewRow();
                                    Row["Date"] = LocalDateTime;
                                    Row["EmployeeId"] = (int)i.EmployeeId;
                                    Row["MainDocId"] = penaltyIssue.Id;
                                    Row["PenaltyTypeId"] = db.PenaltyTypes.Where(a => a.IsActive == true && a.IsDeleted == false).First().Id;
                                    Row["Equivalent"] = Math.Abs(Subtraction_Value / 60);
                                    Row["Total"] = ((decimal)db.EmployeeContracts.Where(a => a.EmployeeId == i.EmployeeId && a.IsActive == true && a.IsDeleted == false).FirstOrDefault().EmployeeTotalSalary) / 240 * ((decimal)Math.Abs(Subtraction_Value / 60));
                                    Row["Cause"] = "حضور متأخر";
                                    OldPenalitiesDetailsDT.Rows.Add(Row);
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (i.In_Out == 1)
                    {
                        var ShiftStartTime = Shift.ShiftDetails.Where(a => a.IsDeleted == false && a.IsVacation == false && a.EnDay.StartsWith(i.Attend_LeaveDateTime.Value.ToString("ddd")) == true).FirstOrDefault() != null ? Shift.ShiftDetails.Where(a => a.IsDeleted == false && a.IsVacation == false && a.EnDay.StartsWith(i.Attend_LeaveDateTime.Value.ToString("ddd")) == true).FirstOrDefault().StartTime : null;

                        var ShiftEndTime = Shift.ShiftDetails.Where(a => a.IsDeleted == false && a.IsVacation == false && a.EnDay.StartsWith(i.Attend_LeaveDateTime.Value.ToString("ddd")) == true).FirstOrDefault() != null ? Shift.ShiftDetails.Where(a => a.IsDeleted == false && a.IsVacation == false && a.EnDay.StartsWith(i.Attend_LeaveDateTime.Value.ToString("ddd")) == true).FirstOrDefault().EndTime : null;

                        if (ShiftEndTime != null)
                        {
                            var ShiftStartTimeTotalMinutes = ShiftStartTime.Value.TotalMinutes;
                            var ShiftEndTimeTotalMinutes = ShiftEndTime.Value.TotalMinutes;
                            //if (T1 > 12)
                            //{
                            //    T1 = T1 - 12;
                            //}
                            var EmployeeLeaveTimeTotalMinutes = (LocalDateTime.TimeOfDay).TotalMinutes;

                            //var empAttendRecord = db.EmployeesAttendAndLeaveDetails.Where(a => a.EmployeesAttendAndLeaveId == e.Id && a.Attend_LeaveDateTime.Value.Year == LocalDateTime.Year && a.Attend_LeaveDateTime.Value.Month == LocalDateTime.Month && a.Attend_LeaveDateTime.Value.Day == LocalDateTime.Day && a.In_Out == 0 && a.IsDeleted == false).FirstOrDefault();
                            var empAttendRecord = db.EmployeesAttendAndLeaveDetails.Where(a => a.EmployeeId == i.EmployeeId && a.Attend_LeaveDateTime.Value.Year == LocalDateTime.Year && a.Attend_LeaveDateTime.Value.Month == LocalDateTime.Month && a.Attend_LeaveDateTime.Value.Day == LocalDateTime.Day && a.In_Out == 0 && a.IsDeleted == false).FirstOrDefault();

                            var empAttendTimeInMinutes = empAttendRecord != null ? DateTime.Parse(empAttendRecord.Attend_LeaveDateTime.ToString()).ToUniversalTime().AddHours(2).TimeOfDay.TotalMinutes : 0;

                            // Subtraction Value of Shift End Time and Employee Leave Time
                            var Subtraction_Value = ShiftEndTimeTotalMinutes - EmployeeLeaveTimeTotalMinutes;

                            // Total working minutes of employee
                            var workTimeInMinutes = EmployeeLeaveTimeTotalMinutes - empAttendTimeInMinutes;

                            // Total working minutes of shift
                            var totalWorkingMinutesOfShift = ShiftEndTimeTotalMinutes - ShiftStartTimeTotalMinutes;


                            if (Subtraction_Value < 0 /*&& empAttendTimeInMinutes > 0*/ && workTimeInMinutes > totalWorkingMinutesOfShift)
                            {
                                OvertimeIssue OvertimeIssueRecord = db.OvertimeIssues.Where(a => a.EmployeeId == i.EmployeeId && a.IsDeleted == false && a.Month == LocalDateTime.Month && a.Year == LocalDateTime.Year).FirstOrDefault();

                                OvertimeIssueDetial issueDetial = new OvertimeIssueDetial();
                                //object issueDetialObj = new object();


                                if (OvertimeIssueRecord == null)
                                {
                                    OvertimeIssue OI = new OvertimeIssue();
                                    OI.EmployeeId = (int)i.EmployeeId;
                                    OI.DocumentNumber = db.OvertimeIssues.OrderByDescending(q => q.Id).FirstOrDefault() != null ? (int.Parse(db.OvertimeIssues.OrderByDescending(q => q.Id).FirstOrDefault().DocumentNumber) + 1).ToString() : "1";
                                    OI.Month = i.Attend_LeaveDateTime.Value.Month;
                                    OI.Year = i.Attend_LeaveDateTime.Value.Year;



                                    //issueDetial.MainDocId = OI.Id;
                                    //issueDetial.Date = LocalDateTime;
                                    //issueDetial.OvertimeTypeId = db.OvertimeTypes.Where(a => a.IsActive == true && a.IsDeleted == false).First().Id;
                                    //issueDetial.Equivalent = Math.Abs(Subtraction_Value/60);
                                    //issueDetial.Total = ((decimal)db.EmployeeContracts.Where(a => a.EmployeeId == i.EmployeeId && a.IsActive == true && a.IsDeleted == false).FirstOrDefault().EmployeeTotalSalary) / 240 * ((decimal)Math.Abs(Subtraction_Value/60));

                                    //issueDetialObj = new
                                    //{
                                    //    EmployeeId = (int)i.EmployeeId,
                                    //    MainDocId = OI.Id,
                                    //    Date = LocalDateTime,
                                    //    OvertimeTypeId = db.OvertimeTypes.Where(a => a.IsActive == true && a.IsDeleted == false).First().Id,
                                    //    Equivalent = Math.Abs(Subtraction_Value / 60),
                                    //    Total = ((decimal)db.EmployeeContracts.Where(a => a.EmployeeId == i.EmployeeId && a.IsActive == true && a.IsDeleted == false).FirstOrDefault().EmployeeTotalSalary) / 240 * ((decimal)Math.Abs(Subtraction_Value / 60))
                                    //};

                                    //OI.OvertimeIssueDetials.Add(issueDetial);
                                    OI.OvertimeIssueDetials = null;

                                    //NewOvertimesDetails.Add(issueDetialObj);
                                    //NewOvertimes.Add(OI);

                                    DataRow row = NewOvertimeDT.NewRow();
                                    row["Id"] = OI.Id;
                                    row["EmployeeId"] = OI.EmployeeId;
                                    row["DocumentNumber"] = OI.DocumentNumber;
                                    row["Month"] = OI.Month;
                                    row["Year"] = OI.Year;
                                    row["IsDeleted"] = false;
                                    row["IsActive"] = true;
                                    NewOvertimeDT.Rows.Add(row);


                                    DataRow Row = NewOvertimesDetailsDT.NewRow();
                                    Row["Date"] = LocalDateTime;
                                    Row["EmployeeId"] = (int)i.EmployeeId;
                                    Row["MainDocId"] = OI.Id;
                                    Row["OvertimeTypeId"] = db.OvertimeTypes.Where(a => a.IsActive == true && a.IsDeleted == false).First().Id;
                                    Row["Equivalent"] = Math.Abs(Subtraction_Value / 60) * 1.5; //overTime 1 hr = 1.5 hr
                                    Row["Total"] = ((decimal)db.EmployeeContracts.Where(a => a.EmployeeId == i.EmployeeId && a.IsActive == true && a.IsDeleted == false).FirstOrDefault().EmployeeTotalSalary) / 240 * ((decimal)Math.Abs(Subtraction_Value / 60));

                                    NewOvertimesDetailsDT.Rows.Add(Row);
                                }
                                else
                                {

                                    issueDetial.MainDocId = OvertimeIssueRecord.Id;
                                    issueDetial.Date = LocalDateTime;
                                    issueDetial.OvertimeTypeId = db.OvertimeTypes.Where(a => a.IsActive == true && a.IsDeleted == false).First().Id;
                                    issueDetial.Equivalent = Math.Abs(Subtraction_Value / 60) * 1.5; //overTime 1 hr = 1.5 hr
                                    issueDetial.Total = ((decimal)db.EmployeeContracts.Where(a => a.EmployeeId == i.EmployeeId && a.IsActive == true && a.IsDeleted == false).FirstOrDefault().EmployeeTotalSalary) / 240 * ((decimal)Math.Abs(Subtraction_Value / 60));

                                    //issueDetialObj = new
                                    //{
                                    //    EmployeeId = (int)i.EmployeeId,
                                    //    MainDocId = OvertimeIssueRecord.Id,
                                    //    Date = LocalDateTime,
                                    //    OvertimeTypeId = db.OvertimeTypes.Where(a => a.IsActive == true && a.IsDeleted == false).First().Id,
                                    //    Equivalent = Math.Abs(Subtraction_Value / 60),
                                    //    Total = ((decimal)db.EmployeeContracts.Where(a => a.EmployeeId == i.EmployeeId && a.IsActive == true && a.IsDeleted == false).FirstOrDefault().EmployeeTotalSalary) / 240 * ((decimal)Math.Abs(Subtraction_Value / 60))
                                    //};

                                    //OvertimeIssueRecord.OvertimeIssueDetials.Add(issueDetial);
                                    OvertimeIssueRecord.OvertimeIssueDetials = null;

                                    //OldOvertimesDetails.Add(issueDetialObj);
                                    //OldOvertimes.Add(OvertimeIssueRecord);

                                    DataRow row = OldOvertimeDT.NewRow();
                                    row["Id"] = OvertimeIssueRecord.Id;
                                    row["EmployeeId"] = OvertimeIssueRecord.EmployeeId;
                                    row["DocumentNumber"] = OvertimeIssueRecord.DocumentNumber;
                                    row["Month"] = OvertimeIssueRecord.Month;
                                    row["Year"] = OvertimeIssueRecord.Year;
                                    row["IsDeleted"] = false;
                                    row["IsActive"] = true;
                                    OldOvertimeDT.Rows.Add(row);


                                    DataRow Row = OldOvertimesDetailsDT.NewRow();
                                    Row["Date"] = LocalDateTime;
                                    Row["EmployeeId"] = (int)i.EmployeeId;
                                    Row["MainDocId"] = OvertimeIssueRecord.Id;
                                    Row["OvertimeTypeId"] = db.OvertimeTypes.Where(a => a.IsActive == true && a.IsDeleted == false).First().Id;
                                    Row["Equivalent"] = Math.Abs(Subtraction_Value / 60) * 1.5;//overTime 1 hr = 1.5 hr
                                    Row["Total"] = ((decimal)db.EmployeeContracts.Where(a => a.EmployeeId == i.EmployeeId && a.IsActive == true && a.IsDeleted == false).FirstOrDefault().EmployeeTotalSalary) / 240 * ((decimal)Math.Abs(Subtraction_Value / 60));

                                    OldOvertimesDetailsDT.Rows.Add(Row);
                                }


                            }
                            else if (Subtraction_Value > 0)
                            {
                                // Check For Early Leave Permission
                                var earlyLeavePermission = db.EarlyLeavePermissions.Where(a => a.IsDeleted == false && a.EmployeeId == i.EmployeeId &&
a.ShiftId == Employee_ShiftId && a.PermissionDate.Value.Day == LocalDateTime.Day && a.PermissionDate.Value.Month == LocalDateTime.Month && a.PermissionDate.Value.Year == LocalDateTime.Year && a.IsApproved == true).FirstOrDefault();

                                var timeShouldEmpLeaveIn = earlyLeavePermission != null ? ShiftEndTimeTotalMinutes - (earlyLeavePermission.NoOfHours * 60) : ShiftEndTimeTotalMinutes;

                                if (earlyLeavePermission == null || EmployeeLeaveTimeTotalMinutes < timeShouldEmpLeaveIn)
                                {
                                    // Add Penalty for early Leave
                                    PenaltyIssue penaltyIssue = db.PenaltyIssues.Where(a => a.EmployeeId == i.EmployeeId && a.IsDeleted == false && a.Month == LocalDateTime.Month && a.Year == LocalDateTime.Year).FirstOrDefault();

                                    PenaltyIssueDetail penaltyIssueDetial = new PenaltyIssueDetail();

                                    if (penaltyIssue == null)
                                    {
                                        PenaltyIssue PI = new PenaltyIssue();
                                        PI.EmployeeId = (int)i.EmployeeId;
                                        PI.DocumentNumber = db.PenaltyIssues.OrderByDescending(q => q.Id).FirstOrDefault() != null ? (int.Parse(db.PenaltyIssues.OrderByDescending(q => q.Id).FirstOrDefault().DocumentNumber) + 1).ToString() : "1";
                                        PI.Month = i.Attend_LeaveDateTime.Value.Month;
                                        PI.Year = i.Attend_LeaveDateTime.Value.Year;

                                        PI.PenaltyIssueDetails = null;

                                        DataRow row = NewPenalitiesDT.NewRow();
                                        row["Id"] = PI.Id;
                                        row["EmployeeId"] = PI.EmployeeId;
                                        row["DocumentNumber"] = PI.DocumentNumber;
                                        row["Month"] = PI.Month;
                                        row["Year"] = PI.Year;
                                        row["IsDeleted"] = false;
                                        //row["IsPosted"] = null;
                                        row["Date"] = e.Date;

                                        NewPenalitiesDT.Rows.Add(row);


                                        DataRow Row = NewPenalitiesDetailsDT.NewRow();
                                        Row["Date"] = LocalDateTime;
                                        Row["EmployeeId"] = (int)i.EmployeeId;
                                        Row["MainDocId"] = PI.Id;
                                        Row["PenaltyTypeId"] = db.PenaltyTypes.Where(a => a.IsActive == true && a.IsDeleted == false).First().Id;
                                        Row["Equivalent"] = Math.Abs(Subtraction_Value / 60);
                                        Row["Total"] = ((decimal)db.EmployeeContracts.Where(a => a.EmployeeId == i.EmployeeId && a.IsActive == true && a.IsDeleted == false).FirstOrDefault().EmployeeTotalSalary) / 240 * ((decimal)Math.Abs(Subtraction_Value / 60));
                                        Row["Cause"] = "انصراف مبكر";

                                        NewPenalitiesDetailsDT.Rows.Add(Row);
                                    }
                                    else
                                    {

                                        //penaltyIssueDetial.MainDocId = penaltyIssue.Id;
                                        //penaltyIssueDetial.Date = LocalDateTime;
                                        //penaltyIssueDetial.PenaltyTypeId = db.PenaltyTypes.Where(a => a.IsActive == true && a.IsDeleted == false).First().Id;
                                        //penaltyIssueDetial.Equivalent = Math.Abs(Subtraction_Value / 60);
                                        //penaltyIssueDetial.Total = ((decimal)db.EmployeeContracts.Where(a => a.EmployeeId == i.EmployeeId && a.IsActive == true && a.IsDeleted == false).FirstOrDefault().EmployeeTotalSalary) / 240 * ((decimal)Math.Abs(Subtraction_Value / 60));
                                        //penaltyIssueDetial.Cause = "انصراف مبكر";

                                        penaltyIssue.PenaltyIssueDetails = null;

                                        DataRow row = OldPenalitiesDT.NewRow();
                                        row["Id"] = penaltyIssue.Id;
                                        row["EmployeeId"] = penaltyIssue.EmployeeId;
                                        row["DocumentNumber"] = penaltyIssue.DocumentNumber;
                                        row["Month"] = penaltyIssue.Month;
                                        row["Year"] = penaltyIssue.Year;
                                        row["IsDeleted"] = false;
                                        //row["IsPosted"] = null;
                                        OldPenalitiesDT.Rows.Add(row);


                                        DataRow Row = OldPenalitiesDetailsDT.NewRow();
                                        Row["Date"] = LocalDateTime;
                                        Row["EmployeeId"] = (int)i.EmployeeId;
                                        Row["MainDocId"] = penaltyIssue.Id;
                                        Row["PenaltyTypeId"] = db.PenaltyTypes.Where(a => a.IsActive == true && a.IsDeleted == false).First().Id;
                                        Row["Equivalent"] = Math.Abs(Subtraction_Value / 60);
                                        Row["Total"] = ((decimal)db.EmployeeContracts.Where(a => a.EmployeeId == i.EmployeeId && a.IsActive == true && a.IsDeleted == false).FirstOrDefault().EmployeeTotalSalary) / 240 * ((decimal)Math.Abs(Subtraction_Value / 60));
                                        Row["Cause"] = "انصراف مبكر";
                                        OldPenalitiesDetailsDT.Rows.Add(Row);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // AbsencePenality
            var shiftEmployees = db.ShiftEmployees.Where(a => a.ShiftId == Shift_Id).Select(a => a.EmployeeId).ToList();
            var AbsentEmployees = shiftEmployees.Except(empIds).ToList();
            var eArr = new List<int>();
            foreach (var x in EmpDates)
            {
                foreach (var i in AbsentEmployees)
                {
                    var ShiftVacation = db.ShiftDetails.Where(a => a.IsVacation == true && a.ShiftId == Shift_Id && a.IsDeleted == false).FirstOrDefault().EnDay;
                    var OfficialVacation = db.OfficialVacations.Where(a => a.IsActive == true && a.IsDeleted == false && a.StartDate <= x && a.EndDate >= x).FirstOrDefault();
                    var VacationApprove = db.VacationRequests.Where(a => a.IsActive == true && a.IsDeleted == false/*&&a.IsAccepted==true*/&& a.IsAcceptedByManager == true && a.EmployeeId == i && a.Date == x).FirstOrDefault();

                    if (ShiftVacation.StartsWith(x.Value.ToString("ddd")) == false && OfficialVacation == null && VacationApprove == null)
                    {

                        var LocalDateTime = DateTime.Parse(x.ToString()).ToUniversalTime().AddHours(2);
                        //// Add Penalty for Absent Employees 
                        //PenaltyIssue penaltyIssue = db.PenaltyIssues.Where(a => a.EmployeeId == i && a.IsDeleted == false && a.Month == LocalDateTime.Month && a.Year == LocalDateTime.Year).FirstOrDefault();

                        PenaltyIssueDetail penaltyIssueDetial = new PenaltyIssueDetail();

                        PenaltyIssue PI = new PenaltyIssue();
                        PI.EmployeeId = (int)i/*.EmployeeId*/;
                        PI.DocumentNumber = db.PenaltyIssues.OrderByDescending(q => q.Id).FirstOrDefault() != null ? (int.Parse(db.PenaltyIssues.OrderByDescending(q => q.Id).FirstOrDefault().DocumentNumber) + 1).ToString() : "1";
                        PI.Month = x/*i.Attend_LeaveDateTime*/.Value.Month;
                        PI.Year = x /*i.Attend_LeaveDateTime*/.Value.Year;
                        PI.PenaltyIssueDetails = null;

                        DataRow dataRow = NewAbsentPenalitiesDT.NewRow();
                        dataRow["Id"] = PI.Id;
                        dataRow["EmployeeId"] = PI.EmployeeId;
                        dataRow["DocumentNumber"] = PI.DocumentNumber;
                        dataRow["Month"] = PI.Month;
                        dataRow["Year"] = PI.Year;
                        dataRow["IsDeleted"] = false;
                        //dataRow["IsPosted"] = null;
                        dataRow["Date"] = e.Date;
                        // check if Employee Had Penality in the same year and Month "if false" >> Add Penality
                        var r = db.PenaltyIssues.Where(a => a.Year == PI.Year && a.Month == PI.Month && !a.IsDeleted && a.EmployeeId == PI.EmployeeId).FirstOrDefault();
                        if (r == null && (eArr.IndexOf(PI.EmployeeId) == -1 || eArr.Exists(p => p.Equals(i)) == false)) //if the PI.EmployeeId exist in the eArr List it will return -1
                        {
                            NewAbsentPenalitiesDT.Rows.Add(dataRow);
                            eArr.Add(PI.EmployeeId);
                        }

                        DataRow Row = NewAbsentPenalitiesDetailsDT.NewRow();
                        Row["Date"] = LocalDateTime;
                        Row["EmployeeId"] = (int)i/*.EmployeeId*/;
                        Row["MainDocId"] = PI.Id;
                        Row["PenaltyTypeId"] = db.PenaltyTypes.Where(a => a.IsActive == true && a.IsDeleted == false && a.ArName == "غياب").First().Id;
                        Row["Equivalent"] = 8 /*Math.Abs(Subtraction_Value / 60)*/;
                        Row["Total"] = ((decimal)db.EmployeeContracts.Where(a => a.EmployeeId == i/*.EmployeeId*/ && a.IsActive == true && a.IsDeleted == false).FirstOrDefault().EmployeeTotalSalary) / 240 * 8 /* ((decimal)Math.Abs(Subtraction_Value / 60))*/;
                        Row["Cause"] = "غياب";

                        NewAbsentPenalitiesDetailsDT.Rows.Add(Row);

                    }
                }

            }

            MyXML.xPathName = "Details";
            var Details = MyXML.GetXML(e.EmployeesAttendAndLeaveDetails);

            //MyXML.xPathName = "LatenessDetails";
            //var Lateness_Details = MyXML.GetXML(Latenesses);

            //MyXML.xPathName = "NewOvertimes";
            //var NewOvertime = MyXML.GetXML(NewOvertimes);

            //MyXML.xPathName = "NewOvertimesDetails";
            //var NewOvertimes_Details = MyXML.GetXML(NewOvertimesDetails);

            //MyXML.xPathName = "OldOvertimes";
            //var OldOvertime = MyXML.GetXML(OldOvertimes);

            //MyXML.xPathName = "OldOvertimesDetails";
            //var OldOvertimes_Details = MyXML.GetXML(OldOvertimesDetails);


            //MyXML.xPathName = "Details";
            //var Details = MyXML.GetXML(EDetailsDT);

            MyXML.xPathName = "LatenessDetails";
            var Lateness_Details = MyXML.GetXML(LatenessDetailsDT);

            MyXML.xPathName = "NewOvertimes";
            var NewOvertime = MyXML.GetXML(NewOvertimeDT);

            MyXML.xPathName = "NewOvertimesDetails";
            var NewOvertimes_Details = MyXML.GetXML(NewOvertimesDetailsDT);

            MyXML.xPathName = "OldOvertimes";
            var OldOvertime = MyXML.GetXML(OldOvertimeDT);

            MyXML.xPathName = "OldOvertimesDetails";
            var OldOvertimes_Details = MyXML.GetXML(OldOvertimesDetailsDT);

            MyXML.xPathName = "NewPenalties";
            var NewPenalities = MyXML.GetXML(NewPenalitiesDT);

            MyXML.xPathName = "OldPenalties";
            var OldPenalities = MyXML.GetXML(OldPenalitiesDT);

            MyXML.xPathName = "NewPenaltiesDetails";
            var NewPenalities_Details = MyXML.GetXML(NewPenalitiesDetailsDT);

            MyXML.xPathName = "OldPenaltiesDetails";
            var OldPenalities_Details = MyXML.GetXML(OldPenalitiesDetailsDT);


            MyXML.xPathName = "NewAbsentPenalties";
            var NewAbsentPenalities = MyXML.GetXML(NewAbsentPenalitiesDT);

            MyXML.xPathName = "NewAbsentPenaltiesDetails";
            var NewAbsentPenalities_Details = MyXML.GetXML(NewAbsentPenalitiesDetailsDT);



            if (e.Id > 0)
            {
                // Edit
                //    db.EmployeesAttendAndLeave_Update(e.Id, null, null, e.UserId, e.IsDeleted, e.IsActive, e.Date, e.DepartmentId, Details, e.IsPosted, Lateness_Details, NewOvertime, OldOvertime, NewOvertimes_Details, OldOvertimes_Details, NewPenalities, OldPenalities, NewPenalities_Details, OldPenalities_Details, NewAbsentPenalities, NewAbsentPenalities_Details);
            }
            else
            {
                // Add
                var idOut = new ObjectParameter("Id", typeof(Int32));//Out Id
                db.EmployeesAttendAndLeave_Insert(idOut, null, null, e.UserId, e.IsDeleted, e.IsActive, e.Date, e.DepartmentId, Details, e.IsPosted, Lateness_Details, NewOvertime, OldOvertime, NewOvertimes_Details, OldOvertimes_Details, NewPenalities, OldPenalities, NewPenalities_Details, OldPenalities_Details, NewAbsentPenalities, NewAbsentPenalities_Details);
            }
        }



        [SkipERPAuthorize]
        public JsonResult GetEmployeesAttendAndLeaveDetails(int? EmployeeId)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var details = db.Employees.Where(a => a.Id == EmployeeId && a.IsActive && !a.IsDeleted).Select(a => new
            {
                EmployeeId = a.Id,
                EmployeeArName = a.Code + " - " + a.ArName,
                EmpCodeOnFingerPrint = a.EmpCodeOnFingerPrint,
                EmployeeCode = a.Code
            }).ToList();
            return Json(details, JsonRequestBehavior.AllowGet);
        }





        [HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                EmployeesAttendAndLeave e = db.EmployeesAttendAndLeaves.Find(id);
                if (e.IsPosted == true)
                {
                    return Content("false");
                }
                List<EmployeesAttendAndLeaveDetail> EmployeesDetails = db.EmployeesAttendAndLeaveDetails.Where(a => a.EmployeesAttendAndLeaveId == id).ToList();
                e.IsDeleted = true;
                e.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                for (int i = 0; i < EmployeesDetails.Count(); i++)
                {
                    EmployeesDetails[i].IsDeleted = true;
                }
                Random random = new Random();
                const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
                e.DocumentNumber = Code;
                db.Entry(e).State = EntityState.Modified;

                db.SaveChanges();

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف سجل حضور وانصراف",
                    EnAction = "AddEdit",
                    ControllerName = "EmployeesAttendAndLeave",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = e.DocumentNumber
                });
                ////--------------------Notification------------------------ -////
                Notification.GetNotification("EmployeesAttendAndLeave", "Delete", "Delete", id, null, " سجلات الحضور والانصراف");

                // -----------------------------------------------------------------------//

                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }

        [SkipERPAuthorize]
        public JsonResult SetDocNum(int id, DateTime? VoucherDate)
        {
            bool IsExistInDocumentsCoding = false;
            var noOfDigits = (int?)0;
            var YearFormat = (int?)0;
            var CodingTypeId = (int?)0;
            var IsZerosFills = (bool?)null;
            var newDocNo = "";
            var GeneratedDocNo = "";
            var lastObj = db.EmployeesAttendAndLeaves.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
            var lastDocNo = lastObj != null ? lastObj.DocumentNumber : "0";
            var DepartmentCode = db.Departments.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == id).FirstOrDefault().Code;
            DepartmentCode = double.Parse(DepartmentCode) < 10 ? "0" + DepartmentCode : DepartmentCode;
            var DepartmentDoc = db.DocumentsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id).FirstOrDefault();
            var MonthFormat = VoucherDate.Value.Month < 10 ? "0" + VoucherDate.Value.Month.ToString() : VoucherDate.Value.Month.ToString();
            if (DepartmentDoc == null)
            {
                DepartmentDoc = db.DocumentsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.AllDepartments == true).FirstOrDefault();
                if (DepartmentDoc == null)
                {
                    IsExistInDocumentsCoding = false;
                }
                else
                {
                    IsExistInDocumentsCoding = true;
                }
            }
            else
            {
                IsExistInDocumentsCoding = true;
            }
            if (IsExistInDocumentsCoding == true)
            {
                noOfDigits = DepartmentDoc.DigitsNo;
                YearFormat = DepartmentDoc.YearFormat;
                CodingTypeId = DepartmentDoc.CodingTypeId;
                IsZerosFills = DepartmentDoc.IsZerosFills;
                YearFormat = YearFormat == 2 ? int.Parse(VoucherDate.Value.Year.ToString().Substring(2, 2)) : int.Parse(VoucherDate.Value.Year.ToString());

                if (CodingTypeId == 1)//آلي
                {
                    if (lastDocNo.Contains("-"))
                    {
                        var ar = lastDocNo.Split('-');
                        if (IsZerosFills == true)
                        {
                            newDocNo = QueryHelper.FillsWithZeros(noOfDigits, (double.Parse(ar[3]) + 1).ToString());
                        }
                        else
                        {
                            newDocNo = (double.Parse(ar[3]) + 1).ToString();
                        }
                    }
                    else
                    {
                        if (IsZerosFills == true)
                        {
                            newDocNo = QueryHelper.FillsWithZeros(noOfDigits, (double.Parse(lastDocNo) + 1).ToString());
                        }
                        else
                        {
                            newDocNo = (double.Parse(lastDocNo) + 1).ToString();
                        }
                    }
                    GeneratedDocNo = DepartmentCode + "-" + YearFormat + "-" + MonthFormat + "-" + newDocNo;
                }
                else if (CodingTypeId == 2)//متصل شهري
                {
                    lastObj = db.EmployeesAttendAndLeaves.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.Date.Value.Month == VoucherDate.Value.Month && a.Date.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
                    if (lastObj != null)
                    {
                        if (lastObj.DocumentNumber.Contains("-"))
                        {
                            var ar = lastObj.DocumentNumber.Split('-');
                            if (double.Parse(ar[2]) == VoucherDate.Value.Month)
                            {
                                if (IsZerosFills == true)
                                {
                                    newDocNo = QueryHelper.FillsWithZeros(noOfDigits, (double.Parse(ar[3]) + 1).ToString());
                                }
                                else
                                {
                                    newDocNo = (double.Parse(ar[3]) + 1).ToString();
                                }
                            }
                            else
                            {
                                if (IsZerosFills == true)
                                {
                                    newDocNo = QueryHelper.FillsWithZeros(noOfDigits, "1").ToString();
                                }
                                else
                                {
                                    newDocNo = "1";
                                }
                            }
                        }
                        else
                        {
                            if (IsZerosFills == true)
                            {
                                newDocNo = QueryHelper.FillsWithZeros(noOfDigits, (double.Parse(lastObj.DocumentNumber) + 1).ToString()).ToString();
                            }
                            else
                            {
                                newDocNo = (double.Parse(lastObj.DocumentNumber) + 1).ToString();
                            }
                        }
                    }
                    else
                    {
                        if (IsZerosFills == true)
                        {
                            newDocNo = QueryHelper.FillsWithZeros(noOfDigits, "1").ToString();
                        }
                        else
                        {
                            newDocNo = "1";
                        }
                    }
                    GeneratedDocNo = DepartmentCode + "-" + YearFormat + "-" + MonthFormat + "-" + newDocNo;
                }
                else if (CodingTypeId == 3)//متصل سنوي
                {
                    lastObj = db.EmployeesAttendAndLeaves.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.Date.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

                    if (lastObj != null)
                    {
                        if (lastObj.DocumentNumber.Contains("-"))
                        {
                            var ar = lastObj.DocumentNumber.Split('-');
                            var VoucherDateFormate = int.Parse(ar[1]).ToString().Length == 2 ? int.Parse((VoucherDate.Value.Year.ToString()).Substring(2, 2)) : VoucherDate.Value.Year;
                            if (double.Parse(ar[1]) == VoucherDateFormate)
                            {
                                if (IsZerosFills == true)
                                {
                                    newDocNo = QueryHelper.FillsWithZeros(noOfDigits, (double.Parse(ar[3]) + 1).ToString());
                                }
                                else
                                {
                                    newDocNo = (double.Parse(ar[3]) + 1).ToString();
                                }
                            }
                            else
                            {
                                if (IsZerosFills == true)
                                {
                                    newDocNo = QueryHelper.FillsWithZeros(noOfDigits, "1").ToString();
                                }
                                else
                                {
                                    newDocNo = "1";
                                }
                            }
                        }
                        else
                        {
                            if (IsZerosFills == true)
                            {
                                newDocNo = QueryHelper.FillsWithZeros(noOfDigits, (double.Parse(lastObj.DocumentNumber) + 1).ToString()).ToString();
                            }
                            else
                            {
                                newDocNo = (double.Parse(lastObj.DocumentNumber) + 1).ToString();
                            }
                        }

                    }
                    else
                    {
                        if (IsZerosFills == true)
                        {
                            newDocNo = QueryHelper.FillsWithZeros(noOfDigits, "1").ToString();
                        }
                        else
                        {
                            newDocNo = "1";
                        }
                    }
                    GeneratedDocNo = DepartmentCode + "-" + YearFormat + "-" + MonthFormat + "-" + newDocNo;
                }
            }
            else
            {
                if (lastDocNo.Contains("-"))
                {
                    var ar = lastDocNo.Split('-');
                    newDocNo = (double.Parse(ar[3]) + 1).ToString();
                }
                else
                {
                    newDocNo = (double.Parse(lastDocNo) + 1).ToString();
                }
                GeneratedDocNo = newDocNo;
            }
            return Json(GeneratedDocNo, JsonRequestBehavior.AllowGet);
            //var docNo = QueryHelper.DocLastNum(id, "EmployeesAttendAndLeave");
            //double i = (docNo) + 1;
            //return Json(i, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        //// Given a cell name, parses the specified cell to get the row index.
        //private static uint GetRowIndex(string cellName)
        //{
        //    // Create a regular expression to match the row index portion the cell name.
        //    Regex regex = new Regex(@"\d+");
        //    Match match = regex.Match(cellName);
        //    return uint.Parse(match.Value);
        //}
        //// Given a cell name, parses the specified cell to get the column name.
        //private static string GetColumnName(string cellName)
        //{
        //    // Create a regular expression to match the column name portion of the cell name.
        //    Regex regex = new Regex("[A-Za-z]+");
        //    Match match = regex.Match(cellName);

        //    return match.Value;
        //}

        //// Given two columns, compares the columns.
        //private static int CompareColumn(string column1, string column2)
        //{
        //    if (column1.Length > column2.Length)
        //    {
        //        return 1;
        //    }
        //    else if (column1.Length < column2.Length)
        //    {
        //        return -1;
        //    }
        //    else
        //    {
        //        return string.Compare(column1, column2, true);
        //    }
        //}

        //// Given text and a SharedStringTablePart, creates a SharedStringItem with the specified text 
        //// and inserts it into the SharedStringTablePart. If the item already exists, returns its index.
        //private static int InsertSharedStringItem(string text, SharedStringTablePart shareStringPart)
        //{
        //    // If the part does not contain a SharedStringTable, create it.
        //    if (shareStringPart.SharedStringTable == null)
        //    {
        //        shareStringPart.SharedStringTable = new SharedStringTable();
        //    }

        //    int i = 0;
        //    foreach (SharedStringItem item in shareStringPart.SharedStringTable.Elements<SharedStringItem>())
        //    {
        //        if (item.InnerText == text)
        //        {
        //            // The text already exists in the part. Return its index.
        //            return i;
        //        }

        //        i++;
        //    }

        //    // The text does not exist in the part. Create the SharedStringItem.
        //    shareStringPart.SharedStringTable.AppendChild(new SharedStringItem(new DocumentFormat.OpenXml.Spreadsheet.Text(text)));
        //    shareStringPart.SharedStringTable.Save();

        //    return i;
        //}

        //// Given a column name, a row index, and a WorksheetPart, inserts a cell into the worksheet. 
        //// If the cell already exists, returns it. 
        //private static Cell InsertCellInWorksheet(string columnName, uint rowIndex, WorksheetPart worksheetPart)
        //{
        //    Worksheet worksheet = worksheetPart.Worksheet;
        //    SheetData sheetData = worksheet.GetFirstChild<SheetData>();
        //    string cellReference = columnName + rowIndex;

        //    // If the worksheet does not contain a row with the specified row index, insert one.
        //    Row row;
        //    if (sheetData.Elements<Row>().Where(r => r.RowIndex == rowIndex).Count() != 0)
        //    {
        //        row = sheetData.Elements<Row>().Where(r => r.RowIndex == rowIndex).First();
        //    }
        //    else
        //    {
        //        row = new Row() { RowIndex = rowIndex };
        //        sheetData.Append(row);
        //    }

        //    // If there is not a cell with the specified column name, insert one.  
        //    if (row.Elements<Cell>().Where(c => c.CellReference.Value == columnName + rowIndex).Count() > 0)
        //    {
        //        return row.Elements<Cell>().Where(c => c.CellReference.Value == cellReference).First();
        //    }
        //    else
        //    {
        //        // Cells must be in sequential order according to CellReference. Determine where to insert the new cell.
        //        Cell refCell = null;
        //        foreach (Cell cell in row.Elements<Cell>())
        //        {
        //            if (string.Compare(cell.CellReference.Value, cellReference, true) > 0)
        //            {
        //                refCell = cell;
        //                break;
        //            }
        //        }

        //        Cell newCell = new Cell() { CellReference = cellReference };
        //        row.InsertBefore(newCell, refCell);

        //        worksheet.Save();
        //        return newCell;
        //    }
        //}
    }
}