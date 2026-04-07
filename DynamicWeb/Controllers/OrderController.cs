using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Security.Claims;
using MyERP.Models;
using Newtonsoft.Json;

namespace MyERP.Controllers
{

    public class OrderController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        public ActionResult Index(int? statId, int? EmployeeId, DateTime? DateFrom, DateTime? DateTo, int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة الطلبات ",
                EnAction = "Index",
                ControllerName = "Order",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("Order", "View", "Index", null, null, "الطلبات");
            DateTime utcNow = DateTime.UtcNow;

            TimeZone curTimeZone = TimeZone.CurrentTimeZone;
            // TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById(curTimeZone.StandardName);
            DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
            
            ViewBag.StatusId = new SelectList(db.OrderStatus.Select(s => new { s.Id, s.ArName }), "Id", "ArName", statId);
            ViewBag.EmployeeId = new SelectList(db.Employees.Where(s => s.IsActive == true && s.IsDeleted == false).Select(s => new { s.Id, s.ArName }), "Id", "ArName", EmployeeId);
            ViewBag.DateFrom = DateFrom == null ? db.Orders.Min(j => j.Date)!=null? db.Orders.Min(j => j.Date).Value.ToString("yyyy-MM-ddTHH:mm"): cTime.ToString("yyyy-MM-ddTHH:mm") : DateTime.Parse(DateFrom.ToString()).ToString("yyyy-MM-ddTHH:mm");
            ViewBag.DateTo = DateTo == null ? db.Orders.Max(j => j.Date)!=null? db.Orders.Max(j => j.Date).Value.ToString("yyyy-MM-ddTHH:mm"): cTime.ToString("yyyy-MM-ddTHH:mm") : DateTime.Parse(DateTo.ToString()).ToString("yyyy-MM-ddTHH:mm");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<Order> orders;

            if (string.IsNullOrEmpty(searchWord))
            {
                orders = db.Orders.Where(s => s.IsDeleted == false && s.IsActive == true && (statId == null || s.StatusId == statId) && (EmployeeId == null || s.EmployeeId == EmployeeId) && (DateFrom == null || s.Date > DateFrom) && (DateTo == null || s.Date < DateTo)).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Orders.Where(s => s.IsDeleted == false && s.IsActive == true && (statId == null || s.StatusId == statId) && (EmployeeId == null || s.EmployeeId == EmployeeId) && (DateFrom == null || s.Date > DateFrom) && (DateTo == null || s.Date < DateTo)).Count();
            }
            else
            {
                orders = db.Orders.Where(s => (statId == null || s.StatusId == statId) && (EmployeeId == null || s.EmployeeId == EmployeeId) && (DateFrom == null || s.Date > DateFrom) && (DateTo == null || s.Date < DateTo) && s.IsDeleted == false && s.IsActive == true && (s.Customer.ArName.Contains(searchWord) || s.ServicesCategory.ArName.Contains(searchWord) || s.OrderNumber.Contains(searchWord) || s.Date.ToString().Contains(searchWord) || s.Techanician.ArName.Contains(searchWord) || s.Notes.Contains(searchWord))).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Orders.Where(s => (statId == null || s.StatusId == statId) && (EmployeeId == null || s.EmployeeId == EmployeeId) && (DateFrom == null || s.Date > DateFrom) && (DateTo == null || s.Date < DateTo) && s.IsDeleted == false && s.IsActive == true && (s.Customer.ArName.Contains(searchWord) || s.ServicesCategory.ArName.Contains(searchWord) || s.OrderNumber.Contains(searchWord) || s.Date.ToString().Contains(searchWord) || s.Techanician.ArName.Contains(searchWord) || s.Notes.Contains(searchWord))).Count();

            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(orders.ToList());
        }
        [SkipERPAuthorize]
        public JsonResult SetOrderNum(int? id, DateTime? VoucherDate)
        {
            bool IsExistInDocumentsCoding = false;
            var noOfDigits = (int?)0;
            var YearFormat = (int?)0;
            var CodingTypeId = (int?)0;
            var IsZerosFills = (bool?)null;
            var newDocNo = "";
            var GeneratedDocNo = "";
            var lastObj = db.Orders.Where(a => a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
            var lastDocNo = lastObj != null ? lastObj.OrderNumber : "0";
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
                    lastObj = db.Orders.Where(a => a.IsDeleted == false && a.DepartmentId == id && a.Date.Value.Month == VoucherDate.Value.Month && a.Date.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
                    if (lastObj != null)
                    {
                        if (lastObj.OrderNumber.Contains("-"))
                        {
                            var ar = lastObj.OrderNumber.Split('-');
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
                                newDocNo = QueryHelper.FillsWithZeros(noOfDigits, (double.Parse(lastObj.OrderNumber) + 1).ToString()).ToString();
                            }
                            else
                            {
                                newDocNo = (double.Parse(lastObj.OrderNumber) + 1).ToString();
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
                    lastObj = db.Orders.Where(a => a.IsDeleted == false && a.DepartmentId == id && a.Date.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

                    if (lastObj != null)
                    {
                        if (lastObj.OrderNumber.Contains("-"))
                        {
                            var ar = lastObj.OrderNumber.Split('-');
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
                                newDocNo = QueryHelper.FillsWithZeros(noOfDigits, (double.Parse(lastObj.OrderNumber) + 1).ToString()).ToString();
                            }
                            else
                            {
                                newDocNo = (double.Parse(lastObj.OrderNumber) + 1).ToString();
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
            //var orderNo = QueryHelper.OrderLastNum("Order");
            //return Json(orderNo + 1, JsonRequestBehavior.AllowGet);
        }
        //[SkipERPAuthorize]
        //public JsonResult GetCompanyPrec(int servId)
        //{
        //    var servObj=db.Services.Where(s=>s.Id==servId).FirstOrDefault();
        //    var servCompanyPerc = servObj.CompanyPercentage;
        //    return Json(servCompanyPerc, JsonRequestBehavior.AllowGet);
        //}


        //AddEdit
        public ActionResult AddEdit(int? id)
        {
            
            var newObj = new Order();
            var srevPrice = db.Services.Where(c => c.IsDeleted == false && c.IsActive == true && c.Type == 3).Select(c => c.Points);
            ViewBag.PointValue = db.ServiceSettings.Select(c => c.PointValue).FirstOrDefault();
            ViewBag.OrderFeesPercentage = db.ServiceSettings.Select(c => c.OrderFeesPercentage).FirstOrDefault();
            ViewBag.OrderItemId = new SelectList(db.Items.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");
            //ViewBag.Services = JsonConvert.SerializeObject(srevPrice);
            if (id == null)
            {
                ViewBag.ItemId = new SelectList(db.Items.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.CustomerId = new SelectList(db.Customers.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName + " - " + b.Mobile
                }), "Id", "ArName");

                ViewBag.VendorId = new SelectList(db.Vendors.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.ServiceId = new SelectList(db.Services.Where(a => a.IsActive == true && a.IsDeleted == false && a.Type == 3).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArServiceName
                }), "Id", "ArName");
                ViewBag.MainServiceId = new SelectList(db.Services.Where(a => a.IsActive == true && a.IsDeleted == false && a.Type == 1).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArServiceName
                }), "Id", "ArName");
                ViewBag.ChoosenTechanicianId = new SelectList(db.Techanicians.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.StatusId = new SelectList(db.OrderStatus.Where(a => (a.IsActive == true && a.IsDeleted == false)), "Id", "ArName");
                ViewBag.ServiceCategoryId = new SelectList(db.ServicesCategories.Where(a => (a.IsActive == true && a.IsDeleted == false /*&& a.Type == 1*/)).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.WarrantyTypeId = new SelectList(db.WarrantyTypes.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.ArName
                }), "Id", "ArName");
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                return View(newObj);
            }
            Order order = db.Orders.Find(id);
            if (order == null)
            {
                return HttpNotFound();
            }
            else
            {
                if (order.PropertyContract != null)
                {
                    var PropUnit = db.PropertyDetails.Where(a => a.Id == order.PropertyContract.PropertyUnitId).FirstOrDefault();
                    
                    ViewBag.UnitCode = PropUnit != null ? PropUnit.PropertyUnitNo : string.Empty; // تعيين قيمة فارغة إذا كانت PropUnit null

                }
                ViewBag.ItemId = new SelectList(db.Items.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", order.ItemId);
                ViewBag.SubmittedTechnicians = db.Techanicians.Where(t => (db.SubmittedTechnicians.Where(o => o.OrderId == id).Select(o => o.TechnicianId).Contains(t.Id)) && t.IsDeleted == false && t.IsActive == true).ToList();
                ViewBag.AvaliableTechnicians = db.GetTechniciansInAllowedDistance(order.Latitude, order.Longitude, order.ServiceCategoryId).ToList();

                ViewBag.Next = QueryHelper.Next((int)id, "Order");
                ViewBag.Previous = QueryHelper.Previous((int)id, "Order");
                ViewBag.Last = QueryHelper.GetLast("Order");
                ViewBag.First = QueryHelper.GetFirst("Order");
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "فتح تفاصيل الطلب ",
                    EnAction = "AddEdit",
                    ControllerName = "Order",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "GET",
                    SelectedItem = order.Id,

                    CodeOrDocNo = order.OrderNumber
                });
                ViewBag.CustomerId = new SelectList(db.Customers.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName + " - " + b.Mobile
                }), "Id", "ArName", order.CustomerId);
                ViewBag.ChoosenTechanicianId = new SelectList(db.Techanicians.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", order.ChoosenTechanicianId);
                ViewBag.VendorId = new SelectList(db.Vendors.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", order.OrderSpareParts.Select(a => a.VendorId).FirstOrDefault());
                ViewBag.sparepartPrice = db.OrderSpareParts.Where(a => a.OrderId == order.Id).Select(a => a.Price).FirstOrDefault();
                ViewBag.ServiceId = new SelectList(db.Services.Where(a => a.IsActive == true && a.IsDeleted == false && a.Type == 3).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArServiceName
                }), "Id", "ArName");
                ViewBag.ServiceCategoryId = new SelectList(db.ServicesCategories.Where(a => (a.IsActive == true && a.IsDeleted == false /*&& a.Type == 1*/)).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", order.ServiceCategoryId);
                ViewBag.EmployeeId = new SelectList(db.TechanicianServices.Where(a => (a.IsActive == true && a.IsDeleted == false && a.ServiceId == order.ServiceCategoryId)).Select(b => new
                {
                    Id = b.EmployeeId,
                    ArName = b.Employee.Code + " - " + b.Employee.ArName
                }), "Id", "ArName", order.EmployeeId);
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", order.DepartmentId);
                ViewBag.MainServiceId = new SelectList(db.Services.Where(a => a.IsActive == true && a.IsDeleted == false && a.Type == 1).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArServiceName
                }), "Id", "ArName");
                ViewBag.WarrantyTypeId = new SelectList(db.WarrantyTypes.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.ArName
                }), "Id", "ArName", order.WarrantyTypeId);
                ViewBag.StatusId = new SelectList(db.OrderStatus.Where(a => (a.IsActive == true && a.IsDeleted == false)), "Id", "ArName", order.StatusId);

                try
                {
                    ViewBag.Date = order.Date.Value.ToString("yyyy-MM-ddTHH:mm");


                }
                catch (Exception)
                {
                }
                var OrderSystemPageId = db.SystemPages.Where(s => s.TableName == "Order" && s.IsDeleted == false && s.IsActive == true).Select(s => s.Id).FirstOrDefault();
                var OrderSalesInvoice = db.SalesInvoices.Where(a => a.IsActive == true && a.IsDeleted == false && a.SystemPageId == OrderSystemPageId && a.SelectedId == id).FirstOrDefault();
                ViewBag.IsLinkedWithOrderSalesInvoice = OrderSalesInvoice != null ? true : false;
                return View(order);
            }

        }
        [HttpPost]
        public ActionResult AddEdit(Order order, HttpPostedFileBase[] upload)
        {
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            if (order.ChoosenTechanicianId == 0)
            {
                order.ChoosenTechanicianId = null;
            }
            int userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            order.UserId = userId;
            order.IsDeleted = false;
            var id = order.Id;
            if (order.Id > 0)
            {
                Order orderOld = db.Orders.Find(id);
                var OldOrderStatus = orderOld.StatusId;

                order.IsActive = true;
                MyXML.xPathName = "OrderServices";
                var orderServices = MyXML.GetXML(order.OrderServices);
                MyXML.xPathName = "SpareParts";
                var spareParts = MyXML.GetXML(order.OrderSpareParts);
                MyXML.xPathName = "OrderItems";
                var OrderItems = MyXML.GetXML(order.OrderItems);

                db.UpdateOrder(order.Id, order.OrderNumber, order.Longitude, order.Latitude, order.Date, order.CustomerId, order.ServiceCategoryId, order.SparePartsPrice, order.ServiceCost, order.StatusId, order.ChoosenTechanicianId, order.TechnicianRating, order.TechnicianEvaluation, order.TotalMoneyAmount, order.WithdrawalCause, true, false, order.Notes, order.TechnicianNotes, order.Problems, order.Image, order.UserId, order.WarrantyTypeId, order.CompanyFees, order.OrderFees, order.TechnicianFees, orderServices, spareParts, order.Paid, order.Address, order.CartId, order.DeviceModelId, order.DeviceBrandId, order.DeviceTypeId, OrderItems, order.ItemId, order.EmployeeId, order.DepartmentId,order.MaintenanceDate,order.MaintenanceTimeFrom,order.MaintenanceTimeTo,order.RenterId,order.PropertyContractId);

                ////-------------------- Notification-------------------------////
                Notification.GetNotification("Order", "Edit", "AddEdit", id, null, "الطلبات");
            }
            else
            {

                order.IsActive = true;
                MyXML.xPathName = "OrderServices";
                var orderServices = MyXML.GetXML(order.OrderServices);
                MyXML.xPathName = "SpareParts";
                var spareParts = MyXML.GetXML(order.OrderSpareParts);
                MyXML.xPathName = "OrderItems";
                var OrderItems = MyXML.GetXML(order.OrderItems);
                db.InsertOrder(order.OrderNumber, order.Longitude, order.Latitude, order.Date, order.CustomerId, order.ServiceCategoryId, order.SparePartsPrice, order.ServiceCost, order.StatusId, order.ChoosenTechanicianId, order.TechnicianRating, order.TechnicianEvaluation, order.TotalMoneyAmount, order.WithdrawalCause, true, false, order.Notes, order.TechnicianNotes, order.Problems, order.Image, order.UserId, order.WarrantyTypeId, order.CompanyFees, order.OrderFees, order.TechnicianFees, orderServices, spareParts, order.Paid, order.Address, order.CartId, order.DeviceModelId, order.DeviceBrandId, order.DeviceTypeId, OrderItems, order.ItemId, order.EmployeeId, order.DepartmentId,order.MaintenanceDate, order.MaintenanceTimeFrom, order.MaintenanceTimeTo, order.RenterId, order.PropertyContractId);

                ////-------------------- Notification-------------------------////
                Notification.GetNotification("Order", "Add", "AddEdit", id, null, "الطلبات");

            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = id > 0 ? "تعديل طلب " : "اضافة   طلب",
                EnAction = "AddEdit",
                ControllerName = "Order",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "POST",
                SelectedItem = order.Id > 0 ? order.Id : db.Orders.Max(i => i.Id),
                CodeOrDocNo = order.OrderNumber
            });
            return Content("true");

        }

        public ActionResult AddEdit2() // Add New Order
        {
            ViewBag.IsPropertyManagement = db.AllowedModules.Where(a => a.IsSelected == true && a.SystemPageId == 10429).FirstOrDefault() != null ? true : false;

            ViewBag.DepartmentId = new SelectList(db.Departments.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");
            ViewBag.CustomerId = new SelectList(db.Customers.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");
            ViewBag.GroupId = new SelectList(db.CustomersGroups.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");
            ViewBag.ItemId = new SelectList(db.Items.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");
            ViewBag.WarrantyTypeId = new SelectList(db.WarrantyTypes.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.ArName
            }), "Id", "ArName");
            return View();
        }

        //calc service price,orderFees,companyFees
        [SkipERPAuthorize]
        public JsonResult SetServicePrice(int serviceId, bool AllowDiscount)
        {
            var ServicePrice = db.GetServicePrice(serviceId, AllowDiscount).FirstOrDefault();
            return Json(ServicePrice, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult SetServiceCompanyFees(int serviceId, bool AllowDiscount)
        {
            var ServiceCompanyFees = db.ServiceCompanyFees(serviceId, AllowDiscount).FirstOrDefault();
            return Json(ServiceCompanyFees, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult SetServiceOrderFees(int serviceId, bool AllowDiscount)
        {
            var ServiceOrderFees = db.ServiceOrderFees(serviceId, AllowDiscount).FirstOrDefault();
            return Json(ServiceOrderFees, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult SetServicePriceForWrittenPrice(int serviceId, bool AllowDiscount, decimal price)
        {
            var ServicePrice = db.GetServicePriceForZeroPoint(price, serviceId, AllowDiscount).FirstOrDefault();
            return Json(ServicePrice, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult SetServiceCompanyFeesForWrittenPrice(int serviceId, bool AllowDiscount, decimal price)
        {
            var ServiceCompanyFees = db.ServiceCompanyFeesForZeroPoints(serviceId, price, AllowDiscount).FirstOrDefault();
            return Json(ServiceCompanyFees, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult SetServiceOrderFeesForWrittenPrice(int serviceId, bool AllowDiscount, decimal price)
        {
            var ServiceOrderFees = db.ServiceOrderFeesForZeroPoints(serviceId, price, AllowDiscount).FirstOrDefault();
            return Json(ServiceOrderFees, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult DeleteSubmittedTechnicians(int techId, int orderId)
        {
            try
            {
                db.DeleteSubmittedTechnicians(orderId, techId);
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(false, JsonRequestBehavior.AllowGet);

            }

        }
        [SkipERPAuthorize]
        [HttpPost]
        public ActionResult SetSparePartImage(int orderId, HttpPostedFileBase upload)
        {
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);

            var order = db.Orders.Single(t => t.Id == orderId);
            if (upload != null)
            {
                upload.SaveAs(Server.MapPath("/images/OrderImages/") + upload.FileName);

                order.Image = domainName + ("/images/OrderImages/") + upload.FileName;

            }
            db.Entry(order).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        [SkipERPAuthorize]
        public JsonResult SubServiceCategories(int id)
        {
            ServicesCategory serCat = db.ServicesCategories.Find(id);
            if (serCat.HasDetails != true)
            {
                return Json(new { HasDetails = "false", ServicesCategories = db.ServicesCategories.Where(s => s.ParentId == id && s.IsDeleted == false && s.IsActive == true).Select(s => new { s.Id, ArName = s.Code + " - " + s.ArName }) }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { HasDetails = "true", ServicesCategories = db.ServicesCategories.Where(s => s.ParentId == id && s.IsDeleted == false && s.IsActive == true).Select(s => new { s.Id, ArName = s.Code + " - " + s.ArName }) }, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult SubServices(int id)
        {
            Service Services = db.Services.Find(id);

            return Json(new { Services = db.Services.Where(s => s.TypeId == id && s.IsDeleted == false && s.IsActive == true).OrderByDescending(a => a.Points).Select(s => new { s.Id, ArName = s.Code + " - " + s.ArServiceName }) }, JsonRequestBehavior.AllowGet);
        }
        public JsonResult ParentServiceCategory(int id)
        {
            return Json(db.ServiceCategory_GetParents(id), JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]

        [HttpPost, ActionName("Delete")]

        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                Order order = db.Orders.Find(id);

                order.IsDeleted = true;
                db.Entry(order).State = EntityState.Modified;


                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف طلب",
                    EnAction = "AddEdit",
                    ControllerName = "Order",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,


                });
                Notification.GetNotification("Order", "Delete", "Delete", id, null, "الطلبات");

                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

        }

        [SkipERPAuthorize]
        public JsonResult GetTechniciansInAllowedDistance(int orderId, string lat, string lon)
        {
            return Json(db.GetTechniciansInAllowedDistance(lat, lon, orderId).Select(t => new { Id = t.Id, ArName = t.Code + " - " + t.ArName }), JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult GetItemPrice(int ItemId)
        {
            var Item = db.ItemPrices.Where(a => a.IsDeleted == false && a.IsActive == true && a.IsDefault == true && a.ItemId == ItemId).Select(a => new
            {
                a.ItemId,
                ItemArName = a.Item.ArName,
                ItemPrice = a.Price
            }).FirstOrDefault();
            return Json(Item, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult GetCustomerSalesInvoice(int CustomerId)
        {
            var SalesInvoices = db.SalesInvoices.Where(a => a.IsDeleted == false && a.IsActive == true && a.VendorOrCustomerId == CustomerId).Select(a => new
            {
                a.Id,
                a.DocumentNumber
            }).ToList();
            return Json(SalesInvoices, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult GetCustomerSalesInvoiceDetails(int SalesInvoiceId)
        {
            var Details = db.SalesInvoiceDetails.Where(a => a.SalesInvoice.IsDeleted == false && a.MainDocId == SalesInvoiceId).Select(a => new
            {
                a.Id,
                a.ItemId,
                ItemArName = a.Item.ArName
            }).ToList();
            return Json(Details, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult GetTechanicianServiceEmployee(int? ServiceCategoryId)
        {
            var Technician = db.TechanicianServices.Where(a => a.IsDeleted == false && a.IsActive == true && a.ServiceId == ServiceCategoryId).Select(a => new
            {
                a.EmployeeId,
                ArName = a.Employee.Code + " - " + a.Employee.ArName
            }).ToList();
            return Json(Technician, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult CheckItemWarrantyAndInvoiceDate(int? ItemId, int? InvoiceId)
        {
            var items = db.Items.Where(a => a.IsDeleted == false && a.IsActive == true && a.Id == ItemId).Select(a => new
            {
                a.Id,
                ArName = a.Code + " - " + a.ArName,
                a.HasWarranty,
                a.WarrantyDays,
                a.WarrantyMonths,
                a.WarrantyYears,
            }).ToList();
            var invoice = db.SalesInvoices.Where(a => a.IsDeleted == false && a.IsActive == true && a.Id == InvoiceId).Select(a => new
            {
                a.Id,
                a.DocumentNumber,
                a.VoucherDate
            }).ToList();
            return Json(new { items, invoice }, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult CheckTechnicianOrder(int? TechnicianId, DateTime? MaintenanceDate, TimeSpan? MaintenanceTimeFrom, TimeSpan? MaintenanceTimeTo)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var TechnicianOrders = db.Orders.Where(a => a.IsActive == true && a.IsDeleted == false && a.EmployeeId == TechnicianId
              && (a.Date.Value.Year == MaintenanceDate.Value.Year && a.Date.Value.Month == MaintenanceDate.Value.Month && a.Date.Value.Day == MaintenanceDate.Value.Day)
              && (
              ((a.MaintenanceTimeFrom == MaintenanceTimeFrom || a.MaintenanceTimeTo == MaintenanceTimeFrom) || (a.MaintenanceTimeFrom == MaintenanceTimeTo || a.MaintenanceTimeTo == MaintenanceTimeTo))
              || ((a.MaintenanceTimeFrom < MaintenanceTimeFrom && a.MaintenanceTimeTo > MaintenanceTimeFrom) || (a.MaintenanceTimeFrom < MaintenanceTimeTo && a.MaintenanceTimeTo > MaintenanceTimeTo))
              )
                         ).ToList();
            return Json(TechnicianOrders, JsonRequestBehavior.AllowGet);
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
