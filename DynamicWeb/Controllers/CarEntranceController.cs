using MyERP.Models;
using MyERP.Repository;
using MyERP.Utils;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace MyERP.Controllers
{
    public class CarEntranceController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: CarEntrance
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح حركة إدخال السيارات",
                EnAction = "Index",
                ControllerName = "CarEntrance",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("CarEntrance", "View", "Index", null, null, "حركة إدخال السيارات");
            //////////-----------------------------------------------------------------------
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<CarEntrance> carEntrances;

            if (string.IsNullOrEmpty(searchWord))
            {
                carEntrances = db.CarEntrances.Where(s => s.IsDeleted == false).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.CarEntrances.Where(s => s.IsDeleted == false).Count();

            }
            else
            {
                carEntrances = db.CarEntrances.Where(s => s.IsDeleted == false && (s.DocumentNumber.Contains(searchWord) || s.Customer.ArName.Contains(searchWord) || s.CarModel.ArName.Contains(searchWord) || s.VoucherDate.ToString().Contains(searchWord) || s.Notes.Contains(searchWord))).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.CarEntrances.Where(s => s.IsDeleted == false && (s.DocumentNumber.Contains(searchWord) || s.Customer.ArName.Contains(searchWord) || s.CarModel.ArName.Contains(searchWord) || s.VoucherDate.ToString().Contains(searchWord) || s.Notes.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(carEntrances.ToList());
        }


        // GET: CarEntrance/Edit/5
        public async Task<ActionResult> AddEdit(int? id)
        {
            SystemSetting systemSetting = await db.SystemSettings.AnyAsync() ? await db.SystemSettings.FirstOrDefaultAsync() : new SystemSetting();
            ViewBag.ApplyTaxes = systemSetting.ApplyTaxesOnSalesIvoiceAuto;
            UserRepository userRepository = new UserRepository(db);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            if (id == null)
            {

                ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", systemSetting.DefaultDepartmentId);

                ViewBag.CustomerId = new SelectList(db.Customers.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.CarTypeId = new SelectList(db.CarTypes.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.CarModelId = new SelectList(db.CarModels.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.CarColorId = new SelectList(db.CarColors.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.CarServiceCategoryId = new SelectList(db.CarServiceCategories.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.CarServiceId = new SelectList(db.CarServices.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
                {
                    b.Id,
                    ArName = b.ArName
                }), "Id", "ArName");

                DateTime utcNow = DateTime.UtcNow;
                TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
                DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
                ViewBag.VoucherDate = cTime.ToString("yyyy-MM-ddTHH:mm");

                return View();
            }

            CarEntrance carEntrance = await db.CarEntrances.FindAsync(id);
            if (carEntrance == null)
                return HttpNotFound();

            int sysPageId = QueryHelper.SourcePageId("CarEntrance");
            ViewBag.HasCarSalesInvoive = db.CarSalesInvoices.Where(a => a.IsDeleted == false && a.SelectedId == id && a.SystemPageId == sysPageId).Select(a => a.Id).FirstOrDefault();
            ViewBag.HasCarWorkOrder = db.CarWorkOrders.Where(a => a.IsDeleted == false && a.SelectedId == id && a.SystemPageId == sysPageId).Select(a => a.Id).FirstOrDefault();

            ViewBag.Next = QueryHelper.Next((int)id, "CarEntrance");
            ViewBag.Previous = QueryHelper.Previous((int)id, "CarEntrance");
            ViewBag.Last = QueryHelper.GetLast("CarEntrance");
            ViewBag.First = QueryHelper.GetFirst("CarEntrance");


            //JournalEntry journal = db.JournalEntries.FirstOrDefault(j => j.SourceId == id && j.SourcePageId == sysPageId);
            //ViewBag.Journal = journal;


            ViewBag.CustomerId = new SelectList(db.Customers.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", carEntrance.CustomerId);

            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", carEntrance.DepartmentId);

            ViewBag.CarTypeId = new SelectList(db.CarTypes.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", carEntrance.CarTypeId);

            ViewBag.CarModelId = new SelectList(db.CarModels.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", carEntrance.CarModelId);

            ViewBag.CarColorId = new SelectList(db.CarColors.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", carEntrance.CarColorId);

            ViewBag.CarServiceCategoryId = new SelectList(db.CarServiceCategories.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", carEntrance.CarServiceCategoryId);

            //for (int i = 1; i <= carEntrance.CarEntranceDetails.Count(); i++)
            //{
            //    var carServiceId = carEntrance.CarEntranceDetails.
            ViewBag.CarServiceId = new SelectList(db.CarServices.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
            {
                b.Id,
                ArName = b.ArName
            }), "Id", "ArName");
            //}


            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل حركة إدخال السيارة",
                EnAction = "AddEdit",
                ControllerName = "CarEntrance",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = carEntrance.Id,
                CodeOrDocNo = carEntrance.DocumentNumber
            });
            try
            {
                ViewBag.VoucherDate = carEntrance.VoucherDate.Value.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.OutDate = carEntrance.OutDate.Value.ToString("yyyy-MM-ddTHH:mm");
            }
            catch (Exception)
            {
            }

            return View(carEntrance);
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult AddEdit(CarEntrance carEntrance)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            //bool isECommerce = System.Web.Configuration.WebConfigurationManager.AppSettings["ECommerce"] == "true" ? true : false;
            if (ModelState.IsValid)
            {
                var id = carEntrance.Id;
                carEntrance.IsDeleted = false;
                if (carEntrance.Id > 0)
                {
                    carEntrance.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                    // use another object to prevent entity error
                    var old = db.CarEntrances.Find(id);
                    db.CarEntranceDetails.RemoveRange(db.CarEntranceDetails.Where(p => p.MainDocId == old.Id).ToList());

                    old.DocumentNumber = carEntrance.DocumentNumber;
                    old.DepartmentId = carEntrance.DepartmentId;
                    old.VoucherDate = carEntrance.VoucherDate;
                    old.OutDate = carEntrance.OutDate;
                    old.CarPlateNumber = carEntrance.CarPlateNumber;
                    old.CarSerialNumber = carEntrance.CarSerialNumber;
                    old.CarTypeId = carEntrance.CarTypeId;
                    old.CarColorId = carEntrance.CarColorId;
                    old.CarModelId = carEntrance.CarModelId;
                    old.CarServiceCategoryId = carEntrance.CarServiceCategoryId;
                    old.CustomerId = carEntrance.CustomerId;
                    old.Notes = carEntrance.Notes;
                    old.Image = carEntrance.Image;
                    old.Total = carEntrance.Total;
                    old.TotalAfterTaxes = carEntrance.TotalAfterTaxes;
                    old.VoucherDiscountValue = carEntrance.VoucherDiscountValue;
                    old.VoucherDiscountPercentage = carEntrance.VoucherDiscountPercentage;
                    old.NetTotal = carEntrance.NetTotal;
                    old.SalesTaxes = carEntrance.SalesTaxes;
                    old.CarInOdometer = carEntrance.CarInOdometer;
                    old.CarOutOdometer = carEntrance.CarOutOdometer;


                    foreach (var item in carEntrance.CarEntranceDetails)
                    {
                        old.CarEntranceDetails.Add(item);
                    }

                    db.Entry(old).State = EntityState.Modified;

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("CarEntrance", "Edit", "AddEdit", carEntrance.Id, null, "حركة إدخال السيارات");

                    List<CarEntranceImage> imageList = db.CarEntranceImages.Where(i => i.MainDocId == carEntrance.Id).ToList();

                    if (carEntrance.CarEntranceImages != null)
                    {
                        // For Deleting .
                        foreach (CarEntranceImage deletedImage in imageList.Where(i => i.MainDocId == carEntrance.Id).ToList())
                        {
                            db.CarEntranceImages.Remove(deletedImage);
                            db.SaveChanges();

                        }
                        var bytes = new byte[7000];
                        string fileName = "";
                        foreach (var img in carEntrance.CarEntranceImages)
                        {


                            if (img != null && img.Image.Contains("base64"))
                            {

                                fileName = "/images/Cars/Car_" + carEntrance.Id.ToString() + "_" + DateTime.Now.Ticks + ".jpeg";
                                if (img.Image.Contains("jpeg"))
                                {
                                    bytes = Convert.FromBase64String(img.Image.Replace("data:image/jpeg;base64,", ""));
                                }
                                else
                                {
                                    bytes = Convert.FromBase64String(img.Image.Replace("data:image/png;base64,", ""));
                                }
                                //img.Image = 
                                using (var imageFile = new FileStream(Server.MapPath(fileName), FileMode.Create))
                                {
                                    imageFile.Write(bytes, 0, bytes.Length);
                                    imageFile.Flush();
                                }

                                db.CarEntranceImages.Add(new CarEntranceImage()
                                {
                                    Image = domainName + fileName,
                                    MainDocId = carEntrance.Id
                                });
                                db.SaveChanges();

                            }
                            else
                            {//previous images
                                db.CarEntranceImages.Add(new CarEntranceImage()
                                {
                                    Image = img.Image,
                                    MainDocId = carEntrance.Id
                                });
                                db.SaveChanges();
                            }

                        }
                    }
                }
                else
                {
                    carEntrance.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    carEntrance.IsActive = true;
                    int LastItemID = QueryHelper.GetLast("CarEntrance").HasValue ? QueryHelper.GetLast("CarEntrance").Value : 0;
                    var bytes = new byte[7000];
                    foreach (var img in carEntrance.CarEntranceImages)
                    {
                        if (img != null && img.Image.Contains("base64"))
                        {
                            string fileName = "/images/Cars/Car_" + LastItemID.ToString() + "_" + DateTime.Now.Ticks + ".jpeg";

                            //if(isECommerce == true)
                            //{
                            //    if (new AmazonHelper().WritingAnObject("Cars/" + fileName, img.Image))
                            //    {
                            //        db.CarEntranceImages.Add(new CarEntranceImage()
                            //        {
                            //            Image = fileName,
                            //            MainDocId = LastItemID
                            //        });
                            //        db.SaveChanges();
                            //    }
                            //}
                            //else
                            //{

                            if (img.Image.Contains("jpeg"))
                            {
                                bytes = Convert.FromBase64String(img.Image.Replace("data:image/jpeg;base64,", ""));
                            }
                            else
                            {
                                bytes = Convert.FromBase64String(img.Image.Replace("data:image/png;base64,", ""));
                            }
                            using (var imageFile = new FileStream(Server.MapPath(fileName), FileMode.Create))
                            {
                                imageFile.Write(bytes, 0, bytes.Length);
                                imageFile.Flush();
                            }
                            db.CarEntranceImages.Add(new CarEntranceImage()
                            {
                                Image = domainName + fileName,
                                MainDocId = LastItemID
                            });
                            db.SaveChanges();
                        }
                        //}

                    }
                    carEntrance.DocumentNumber = new JavaScriptSerializer().Serialize(SetDocNum((int)carEntrance.DepartmentId, carEntrance.VoucherDate).Data).ToString().Trim('"');
                    db.CarEntrances.Add(carEntrance);
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("CarEntrance", "Add", "AddEdit", carEntrance.Id, null, "حركة إدخال السيارات");
                }
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = carEntrance.Id > 0 ? "تعديل حركة إدخال سيارة" : "اضافة حركة إدخال سيارة",
                    EnAction = "AddEdit",
                    ControllerName = "CarEntrance",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "Post",
                    SelectedItem = carEntrance.Id,
                    CodeOrDocNo = carEntrance.DocumentNumber
                });

                return Json(new { success = "true" });

            }


            ViewBag.CustomerId = new SelectList(db.Customers.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", carEntrance.CustomerId);

            //ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", carEntrance.DepartmentId);

            ViewBag.CarTypeId = new SelectList(db.CarTypes.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", carEntrance.CarTypeId);

            ViewBag.CarModelId = new SelectList(db.CarModels.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", carEntrance.CarModelId);

            ViewBag.CarColorId = new SelectList(db.CarColors.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", carEntrance.CarColorId);

            ViewBag.CarServiceCategorieId = new SelectList(db.CarServiceCategories.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", carEntrance.CarServiceCategoryId);

            ViewBag.CarServiceId = new SelectList(db.CarServices.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");

            return View(carEntrance);
        }

        // POST: SalesInvoice/Delete/5
        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            int userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            db.CarEntrance_Delete(id, userId);

            //CarEntrance carEntrance = db.CarEntrances.Find(id);
            //carEntrance.IsDeleted = true;
            //carEntrance.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            //db.Entry(carEntrance).State = EntityState.Modified;

            //db.SaveChanges();

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "حذف حركة إدخال سيارة",
                EnAction = "Delete",
                ControllerName = "CarEntrance",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "POST",
                SelectedItem = id,
                //CodeOrDocNo = carEntrance.DocumentNumber
            });

            ////-------------------- Notification-------------------------////
            Notification.GetNotification("CarEntrance", "Delete", "Delete", id, null, "حركة إدخال السيارات");

            return Content("true");

        }


        [SkipERPAuthorize]
        public JsonResult GetCarEntrance(int id)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var CarEntrance = db.CarEntrances.Where(a => a.Id == id).FirstOrDefault();
            return Json(CarEntrance, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult GetService()
        {
            db.Configuration.ProxyCreationEnabled = false;
            var CarService = db.CarServices.ToList();
            return Json(CarService, JsonRequestBehavior.AllowGet);
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
            var lastObj = db.CarEntrances.Where(a => a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.CarEntrances.Where(a => a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Value.Month == VoucherDate.Value.Month && a.VoucherDate.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.CarEntrances.Where(a => a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

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
            //var docNo = QueryHelper.DocLastNum(id, "CarEntrance");
            //double i = (docNo) + 1;
            //return Json(i, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult CheckCarSalesInvoice(int? id, string key)
        {
            db.Configuration.ProxyCreationEnabled = false;
            int sysPageId = QueryHelper.SourcePageId("CarEntrance");
            var HasCarSalesInvoive = db.CarSalesInvoices.Where(a => a.IsDeleted == false && a.SelectedId == id && a.SystemPageId == sysPageId).Select(a => a.Id).FirstOrDefault();
            int sysPageId0 = QueryHelper.SourcePageId("CarSalesInvoice");
            var journal = db.JournalEntries.Where(a => a.SourcePageId == sysPageId0 && a.SourceId == HasCarSalesInvoive).Select(a => a.Id).FirstOrDefault();

            if (key == "Report")
            {
                return Json(HasCarSalesInvoive, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(journal, JsonRequestBehavior.AllowGet);
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