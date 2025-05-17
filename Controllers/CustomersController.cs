using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Security.Claims;
using MyERP.Models;
using System.Threading.Tasks;
using System.Threading;
using System.Globalization;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Web.Script.Serialization;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace MyERP.Controllers
{
    public class CustomersController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: Customers
        public async Task<ActionResult> Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            ViewBag.domainName = domainName;
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمةالعملاء ",
                EnAction = "Index",
                ControllerName = "Customers",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("Customers", "View", "Index", null, null, "العملاء");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<Customer> customers;

            if (string.IsNullOrEmpty(searchWord))
            {
                customers = db.Customers.Include(c => c.Orders).Where(s => s.IsDeleted == false).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = await db.Customers.Where(s => s.IsDeleted == false).CountAsync();
            }
            else
            {
                customers = db.Customers.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.Mobile.Contains(searchWord) || s.EnName.Contains(searchWord) || s.Company.ArName.Contains(searchWord) || s.Notes.Contains(searchWord) || s.CustomersGroup.ArName.Contains(searchWord) || s.Address.Contains(searchWord))).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = await db.Customers.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.Mobile.Contains(searchWord) || s.EnName.Contains(searchWord) || s.Company.ArName.Contains(searchWord) || s.Notes.Contains(searchWord) || s.CustomersGroup.ArName.Contains(searchWord) || s.Address.Contains(searchWord))).CountAsync();
            }
            ViewBag.searchWord = searchWord;

            ViewBag.wantedRowsNo = wantedRowsNo;
            var z = await customers.ToListAsync();
            return View(await customers.ToListAsync());
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
                    var code = db.Customers.Where(a => a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
                    var code = db.Customers.Where(a => a.Code.Contains(fixedPart) && a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
                var code = db.Customers.Where(a => a.FieldsCodingId == null && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
            //var code = QueryHelper.CodeLastNum("Customer");
            //return Json(code + 1, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult SetCodeNumExcel(int? FieldsCodingId,int row)
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
                    var code = db.Customers.Where(a => a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
                    if (code.FirstOrDefault() == null)
                    {
                        result = 0 + 1+row;
                    }
                    else
                    {
                        result = (double.Parse(code.FirstOrDefault().ToString())) + 1 + row;
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
                    var code = db.Customers.Where(a => a.Code.Contains(fixedPart) && a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
                    if (code.FirstOrDefault() == null)
                    {
                        result = 0 + 1 + row;
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
                        result = double.Parse(LastCode.Substring(LastCode.LastIndexOf(fixedPart) + fixedPart.Length)) + 1 + row;
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
                var code = db.Customers.Where(a => a.FieldsCodingId == null && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
                if (code.FirstOrDefault() == null)
                {
                    result = 0 + 1 + row;
                }
                else
                {
                    result = double.Parse(code.FirstOrDefault().ToString()) + 1 + row;
                }
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            //var code = QueryHelper.CodeLastNum("Customer");
            //return Json(code + 1, JsonRequestBehavior.AllowGet);
        }

        //AddEdit
        public ActionResult AddEdit(int? id)
        {
            var SystemPageId = db.SystemPages.Where(a => a.IsActive == true && a.IsDeleted == false && a.ControllerName == "Customers").FirstOrDefault().Id;

            if (id == null)
            {
                ViewBag.SourceTypeId = new SelectList(db.CustomerSourceTypes.Where(x => x.IsActive == true && x.IsDeleted == false).Select(x => new { x.Id, x.ArName }), "Id", "ArName");
                ViewBag.CustomersGroupId = new SelectList(db.CustomersGroups.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                //CustomerId
                ViewBag.CustomerId = new SelectList(db.Customers.Where(x => x.IsActive == true && x.IsDeleted == false).Select(x => new { x.Id, x.ArName }), "Id", "ArName");
                //EmployeeId
                ViewBag.EmployeeId = new SelectList(db.Employees.Where(x => x.IsActive == true && x.IsDeleted == false).Select(x => new { x.Id, x.ArName }), "Id", "ArName");
                ViewBag.CountryId = new SelectList(db.Countries.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.CityId = new SelectList(db.Cities.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
                {
                    b.Id,
                    ArName = b.FixedPart + " - " + b.ArName
                }), "Id", "ArName");
                DateTime utcNow = DateTime.UtcNow;
                TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
                DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
                ViewBag.DealingDate = cTime.ToString("yyyy-MM-ddTHH:mm");
                return View();
            }
            Customer customer = db.Customers.Find(id);
            if (customer == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل العميل ",
                EnAction = "AddEdit",
                ControllerName = "Customers",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = customer.Id,
                ArItemName = customer.ArName,
                EnItemName = customer.EnName,
                CodeOrDocNo = customer.Code
            });
            ViewBag.SourceTypeId = new SelectList(db.CustomerSourceTypes.Where(x => x.IsActive == true && x.IsDeleted == false).Select(x => new { x.Id, x.ArName }), "Id", "ArName", customer.SourceTypeId);
            ViewBag.CustomersGroupId = new SelectList(db.CustomersGroups.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", customer.CustomersGroupId);
            ViewBag.DepartmentId = new SelectList(db.Departments.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", customer.DepartmentId);
            ViewBag.CountryId = new SelectList(db.Countries.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", customer.CountryId);
            ViewBag.CityId = new SelectList(db.Cities.Where(a => (a.IsActive == true && a.IsDeleted == false && a.CountryId == customer.CountryId)).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", customer.CityId);
            ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
            {
                b.Id,
                ArName = b.FixedPart + " - " + b.ArName
            }), "Id", "ArName", customer.FieldsCodingId);
            ViewBag.Next = QueryHelper.Next((int)id, "Customer");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Customer");
            ViewBag.Last = QueryHelper.GetLast("Customer");
            ViewBag.First = QueryHelper.GetFirst("Customer");

            return View(customer);
        }

        [HttpPost]
        [SkipERPAuthorize]//to allow casheir to add customer
        [AllowAnonymous]
        public ActionResult AddEdit(Customer customer)
        {
            if (ModelState.IsValid)
            {
                if (customer.AllowCreditLimit == null)
                {
                    customer.AllowCreditLimit = false;
                }
                var id = customer.Id;
                customer.IsDeleted = false;
                customer.IsActive = true;
                customer.ObCredit = 0;
                customer.ObDebit = 0;
                if (customer.Id > 0)
                {
                    var old = db.Customers.Find(id);
                    db.CustomerReps.RemoveRange(db.CustomerReps.Where(p => p.CustomerId == old.Id).ToList());
                    old.ArName = customer.ArName;
                    old.EnName = customer.EnName;
                    old.DepartmentId = customer.DepartmentId;
                    old.CustomersGroupId = customer.CustomersGroupId;
                    old.Mobile = customer.Mobile;
                    old.Email = customer.Email;
                    old.IsActive = customer.IsActive;
                    old.IsDeleted = customer.IsDeleted;
                    old.IsBranch = customer.IsBranch;
                    old.Address = customer.Address;
                    old.Notes = customer.Notes;
                    old.TaxNumber = customer.TaxNumber;
                    old.WorkNature = customer.WorkNature;
                    old.AllowCreditLimit = customer.AllowCreditLimit;
                    old.CreditLimitAmount = customer.CreditLimitAmount;
                    old.CreditLimitPeriod = customer.CreditLimitPeriod;
                    old.IncludeFees = customer.IncludeFees;
                    old.BuildingNo = customer.BuildingNo;
                    old.CountryId = customer.CountryId;
                    old.CityId = customer.CityId;
                    old.PostOfficeBox = customer.PostOfficeBox;
                    old.PhoneNo = customer.PhoneNo;
                    old.VATNumber = customer.VATNumber;
                    old.ContactPerson = customer.ContactPerson;
                    old.FieldsCodingId = customer.FieldsCodingId;
                    old.NationalId = customer.NationalId;
                    old.IsBlocked = customer.IsBlocked;
                    old.InsuranceCompany = customer.InsuranceCompany;
                    old.InsuranceCompanyPercentage = customer.InsuranceCompanyPercentage;
                    foreach (var item in customer.CustomerReps)
                    {
                        old.CustomerReps.Add(item);
                    }

                    db.Entry(old).State = EntityState.Modified;

                    // db.Entry(customer).State = EntityState.Modified;
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("Customers", "Edit", "AddEdit", id, null, "العملاء");

                    // Add DB Change
                    QueryHelper.AddDBChange(new DBChange()
                    {
                        TableName = "Customer",
                        SelectedId = old.Id,
                        IsMasterChange = true,
                        IsNew = false,
                        IsTransaction = false
                    });
                }
                else
                {
                    if (System.Web.Configuration.WebConfigurationManager.AppSettings["ProjectName"] != "FunTime")
                    {
                        customer.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    }
                    // customer.Code = (QueryHelper.CodeLastNum("Customer") + 1).ToString();
                    customer.Code = new JavaScriptSerializer().Serialize(SetCodeNum(customer.FieldsCodingId).Data).ToString().Trim('"');

                    db.Customers.Add(customer);
                    db.SaveChanges();

                    //-------------------- Notification-------------------------////
                    Notification.GetNotification("Customers", "Add", "AddEdit", id, null, "العملاء");

                    // Add DB Change
                    var SelectedId = db.Customers.OrderByDescending(r => r.Id).FirstOrDefault().Id;
                    QueryHelper.AddDBChange(new DBChange()
                    {
                        TableName = "Customer",
                        SelectedId = SelectedId,
                        IsMasterChange = true,
                        IsNew = true,
                        IsTransaction = false
                    });
                }

                db.SaveChanges();
                if (System.Web.Configuration.WebConfigurationManager.AppSettings["ProjectName"] != "FunTime")
                {
                    QueryHelper.AddLog(new MyLog()
                    {
                        ArAction = id > 0 ? "تعديل بيانات العميل" : "اضافة عميل",
                        EnAction = "AddEdit",
                        ControllerName = "Customers",
                        UserName = User.Identity.Name,
                        UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                        LogDate = DateTime.Now,
                        RequestMethod = "POST",
                        SelectedItem = customer.Id > 0 ? customer.Id : db.Customers.Max(i => i.Id),
                        ArItemName = customer.ArName,
                        EnItemName = customer.EnName,
                        CodeOrDocNo = customer.Code
                    });
                }
                //if (Request.IsAjaxRequest())
                //{
                //    return Json(new { customer.Id });
                //}

                return Json(new { success = "true", customer.Id });
            }
            else
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();

                ViewBag.CustomersGroupId = new SelectList(db.CustomersGroups.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", customer.CustomersGroupId);

                return View(customer);
            }
        }

        [HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                //-- Check if this Customer is used in other Transactions 
                var check = db.CheckCustomerExistanceInOtherTransactions(id).FirstOrDefault();
                if (check > 0)
                {
                    return Content("False");
                }
                else
                {
                    var balance = db.CustomerOpenningBalances.Where(c => c.CustomerId == id).Sum(c => c.OBDebit - c.OBCredit);
                    if (balance != null && Math.Abs((decimal)balance) > 0)
                    {
                        return Content("hasBalance");
                    }
                    Customer customer = db.Customers.Find(id);

                    customer.IsDeleted = true;
                    customer.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    Random random = new Random();
                    const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                    var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
                    customer.Code = Code;
                    customer.FieldsCodingId = null;
                    db.Entry(customer).State = EntityState.Modified;


                    db.SaveChanges();
                    QueryHelper.AddLog(new MyLog()
                    {
                        ArAction = "حذف عميل",
                        EnAction = "AddEdit",
                        ControllerName = "Customers",
                        UserName = User.Identity.Name,
                        UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                        LogDate = DateTime.Now,
                        RequestMethod = "POST",
                        SelectedItem = id,
                        EnItemName = customer.EnName,
                        ArItemName = customer.ArName,
                        CodeOrDocNo = customer.Code
                    });
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("Customers", "Delete", "Delete", id, null, "العملاء");

                    // Add DB Change
                    QueryHelper.AddDBChange(new DBChange()
                    {
                        TableName = "Customer",
                        SelectedId = customer.Id,
                        IsMasterChange = true,
                        IsNew = false,
                        IsTransaction = false
                    });

                    return Content("true");
                }
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
                Customer customer = db.Customers.Find(id);
                if (customer.IsActive == true)
                {
                    customer.IsActive = false;
                }
                else
                {
                    customer.IsActive = true;
                }

                db.Entry(customer).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)customer.IsActive ? "تنشيط عميل" : "إلغاء تنشيط عميل",
                    EnAction = "AddEdit",
                    ControllerName = "Customers",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = customer.Id,
                    EnItemName = customer.EnName,
                    ArItemName = customer.ArName,
                    CodeOrDocNo = customer.Code
                });

                ////-------------------- Notification-------------------------////
                if (customer.IsActive == true)
                {
                    Notification.GetNotification("Customers", "Activate/Deactivate", "ActivateDeactivate", id, true, "العملاء");
                }
                else
                {

                    Notification.GetNotification("Customers", "Activate/Deactivate", "ActivateDeactivate", id, false, "العملاء");
                }

                // Add DB Change
                QueryHelper.AddDBChange(new DBChange()
                {
                    TableName = "Customer",
                    SelectedId = customer.Id,
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

        public ActionResult EncodeCustomerId(int id)
        {
            try
            {
                byte[] inputByteArray = Encoding.UTF8.GetBytes(id.ToString());
                byte[] rgbIV = { 0x21, 0x43, 0x56, 0x87, 0x10, 0xfd, 0xea, 0x1c };
                byte[] key = { };
                key = Encoding.UTF8.GetBytes("Z4a2rX3T");
                DESCryptoServiceProvider des = new DESCryptoServiceProvider();
                MemoryStream ms = new MemoryStream();
                CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(key, rgbIV), CryptoStreamMode.Write);
                cs.Write(inputByteArray, 0, inputByteArray.Length);
                cs.FlushFinalBlock();
                var str = Convert.ToBase64String(ms.ToArray()).Replace("+", "_pl_").Replace("=", "_eq_").Replace("/", "_sl_").Replace(@"\", "_bsl_");
                return Json(str, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return null;
            }
        }
        //public ActionResult DecodeCustomerId(string _id)
        //{
        //    if (_id != null)
        //    {
        //        _id = _id.Replace("_pl_", "+").Replace("_eq_", "=").Replace("_sl_", "/").Replace("_bsl_", @"\");
        //        byte[] inputByteArray = new byte[_id.Length + 1];
        //        byte[] rgbIV = { 0x21, 0x43, 0x56, 0x87, 0x10, 0xfd, 0xea, 0x1c };
        //        byte[] key = { };

        //        try
        //        {
        //            key = System.Text.Encoding.UTF8.GetBytes("Z4a2rX3T");
        //            DESCryptoServiceProvider des = new DESCryptoServiceProvider();
        //            inputByteArray = Convert.FromBase64String(_id);
        //            MemoryStream ms = new MemoryStream();
        //            CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(key, rgbIV), CryptoStreamMode.Write);
        //            cs.Write(inputByteArray, 0, inputByteArray.Length);
        //            cs.FlushFinalBlock();
        //            Encoding encoding = Encoding.UTF8;
        //            var CustomerId= encoding.GetString(ms.ToArray());
        //            return RedirectToAction("CustomerBalanceSheet", "Customers", new { _id = CustomerId });
        //            //return Json(CustomerId, JsonRequestBehavior.AllowGet);
        //        }
        //        catch (Exception e)
        //        {
        //            return null;
        //        }
        //    }
        //    else
        //    {
        //        return null;
        //    }
        //}

        [AllowAnonymous]
        public ActionResult CustomerBalanceSheet(string _id, int pageIndex = 1, int wantedRowsNo = 10)
        {
            if (_id != null)
            {
                _id = _id.Replace("_pl_", "+").Replace("_eq_", "=").Replace("_sl_", "/").Replace("_bsl_", @"\");
                byte[] inputByteArray = new byte[_id.Length + 1];
                byte[] rgbIV = { 0x21, 0x43, 0x56, 0x87, 0x10, 0xfd, 0xea, 0x1c };
                byte[] key = { };

                try
                {
                    key = System.Text.Encoding.UTF8.GetBytes("Z4a2rX3T");
                    DESCryptoServiceProvider des = new DESCryptoServiceProvider();
                    inputByteArray = Convert.FromBase64String(_id);
                    MemoryStream ms = new MemoryStream();
                    CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(key, rgbIV), CryptoStreamMode.Write);
                    cs.Write(inputByteArray, 0, inputByteArray.Length);
                    cs.FlushFinalBlock();
                    Encoding encoding = Encoding.UTF8;
                    var customerId = encoding.GetString(ms.ToArray());
                    ViewBag.PageIndex = pageIndex;
                    ViewBag.Id = _id;
                    int skipRowsNo = 0;
                    if (pageIndex > 1)
                        skipRowsNo = (pageIndex - 1) * wantedRowsNo;
                    var customerBalanceSheet = db.CustomerOB_Get(int.Parse(customerId), null, null, null).Skip(skipRowsNo).Take(wantedRowsNo).ToList();
                    ViewBag.Count = db.CustomerOB_Get(int.Parse(customerId), null, null, null).Count();
                    ViewBag.wantedRowsNo = wantedRowsNo;
                    var AllCustomerBalanceSheet = db.CustomerOB_Get(int.Parse(customerId), null, null, null).ToList();
                    if (AllCustomerBalanceSheet.Count() > 0)
                    {
                        ViewBag.CustomerName = customerBalanceSheet.FirstOrDefault().CustomerArName;
                        var SumDebit = AllCustomerBalanceSheet.Sum(a => a.Debit);
                        var SumCredit = AllCustomerBalanceSheet.Sum(a => a.Credit);
                        var SumDebitBalance = (SumDebit - SumCredit) > 0 ? (SumDebit - SumCredit) : 0;
                        var SumCreditBalance = (SumCredit - SumDebit) > 0 ? (SumCredit - SumDebit) : 0;
                        ViewBag.SumDebit = SumDebit;
                        ViewBag.SumCredit = SumCredit;
                        ViewBag.SumDebitBalance = SumDebitBalance;
                        ViewBag.SumCreditBalance = SumCreditBalance;
                    }

                    return View(customerBalanceSheet);
                }
                catch (Exception e)
                {
                    return View();
                }
            }
            else
            {
                return View();
            }

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
                    List<Customer> customers = new List<Customer>();

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
                        for (int i = 0; i < row.Descendants<Cell>().Count(); i++)
                        {
                            Cell cell = row.Descendants<Cell>().ElementAt(i);
                            int actualCellIndex = CellReferenceToIndex(cell);
                            tempRow[actualCellIndex] = GetCellValue(spreadsheet, cell);
                        }
                        dt.Rows.Add(tempRow);
                    }
                    var CustomerSystemPageId = db.SystemPages.Where(a => a.TableName == "Customer").FirstOrDefault().Id;
                    var CustomerFieldCoding = db.FieldsCodings.Where(a => a.PageId == CustomerSystemPageId).FirstOrDefault();
                    var CustomerFieldCodingId = 0;
                    if (CustomerFieldCoding!=null)
                    {
                        CustomerFieldCodingId = CustomerFieldCoding.Id;
                    }
                    var DataTableRows = dt.Rows;
                   // var CustomerCode = new JavaScriptSerializer().Serialize(SetCodeNum(null).Data).ToString().Trim('"');
                    for (int row = 1; row < DataTableRows.Count; row++)
                    {
                        var RowData = DataTableRows[row];
                        Customer record = new Customer();
                        if (row == 1)
                        {
                            record.Code = new JavaScriptSerializer().Serialize(SetCodeNumExcel(CustomerFieldCodingId, 0).Data).ToString().Trim('"'); //(double.Parse(CustomerCode)).ToString();
                        }
                        else
                        {
                            record.Code = new JavaScriptSerializer().Serialize(SetCodeNumExcel(CustomerFieldCodingId, row - 1).Data).ToString().Trim('"'); //(double.Parse(CustomerCode) + row - 1).ToString();
                        }
                        record.ArName = RowData[0] != null ? (RowData[0]).ToString() : null;
                        record.EnName = RowData[1] != null ? (RowData[1]).ToString() : null;
                        var GroupCode = RowData[2] != null ? (RowData[2]).ToString() : null;
                        var Group = db.CustomersGroups.Where(a => a.IsActive == true && a.IsDeleted == false && a.Code == GroupCode).FirstOrDefault();
                        record.CustomersGroupId = Group != null ? Group.Id : 0;
                        record.CustomersGroupId = record.CustomersGroupId == 0 ? null : record.CustomersGroupId;
                        record.Email = RowData[3] != null ? (RowData[3]).ToString() : null;
                        record.Mobile = RowData[4] != null ? (RowData[4]).ToString() : null;
                        record.Address = RowData[5] != null ? (RowData[5]).ToString() : null;
                        record.NationalId = RowData[6] != null ? (RowData[6]).ToString() : null;
                        var Dept = RowData[7] != null ? (RowData[7]).ToString() : null;
                        var DepartmentId = db.Departments.Where(a => a.IsActive == true && a.IsDeleted == false && a.Code == Dept).FirstOrDefault();
                        record.DepartmentId = DepartmentId != null ? DepartmentId.Id : 0;
                        if (record.DepartmentId == 0)
                        {
                            record.DepartmentId = null;
                        }
                        else
                        {
                            record.IsBranch = true;
                        }
                        record.VATNumber = RowData[8] != null ? (RowData[8]).ToString() : null;
                        record.TaxNumber = RowData[9] != null ? (RowData[9]).ToString() : null;

                        record.IsActive = true;
                        record.IsDeleted = false;
                        record.ObCredit = 0;
                        record.ObDebit = 0;
                        if (CustomerFieldCodingId > 0)
                        {
                            record.FieldsCodingId = CustomerFieldCodingId;
                        }
                        customers.Add(record);
                    }
                    db.Customers.AddRange(customers);
                    try
                    {
                        db.SaveChanges();
                      //  spreadsheet.Close();
                        return Json("success", JsonRequestBehavior.AllowGet);
                    }
                    catch (Exception ex)
                    {
                        var errors = ex.InnerException.InnerException.Message;
                    }
                   // spreadsheet.Close();
                    return Json("Error", JsonRequestBehavior.AllowGet);


                    // ----------- End Insert Into Data Table To Read Blank Cells From Excel ------- //
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
            string value = cell.CellValue!=null?cell.CellValue.InnerXml:null;

            if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
            {
                return stringTablePart.SharedStringTable.ChildElements[Int32.Parse(value)].InnerText;
            }
            //else if (cell.CellReference.ToString().Contains("C") || cell.CellReference.ToString().Contains("F")) //Attend/LeaveDate
            //{
            //    // Read Date
            //    var date = DateTime.FromOADate(double.Parse(cell.CellValue.Text)).ToString("yyyy-MM-ddTHH:mm");//ToString("dd/MM/yyyy");
            //    return date;
            //}
            else
            {
                return value;
            }
        }
        private static int CellReferenceToIndex(Cell cell)
        {
            int index = 0;
            string reference = cell.CellReference.ToString().ToUpper();
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
