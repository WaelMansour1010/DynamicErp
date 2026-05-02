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
using MyERP.Utils;
using System.IO;
using System.Web.Script.Serialization;

namespace MyERP.Controllers.SystemSettings
{

    public class ItemGroupsController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        public ActionResult Index(bool? IsSearch, int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "", int departmentId = 0)
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة مجموعات الأصناف",
                EnAction = "Index",
                ControllerName = "ItemGroups",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("ItemGroups", "View", "Index", null, null, "مجموعات الأصناف");
            //////-----------------------------------------------------------------------

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
//----------------- Filter By Department -----------------------//
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            var DeptId = 0;
            if (userId == 1)
            {
                var depts = db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).ToList();
                if (depts.Count != 1)
                {
                    DeptId = depts.FirstOrDefault().Id;
                    ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                    {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", DeptId);
                    if (IsSearch != true)
                    {
                        departmentId = DeptId;
                    }
                }
                else
                {
                    ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                    {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", departmentId);
                }
            }
            else
            {
                var depts = db.UserDepartments.Where(d => d.UserId == userId).ToList();
                if (depts.Count == 1)
                {
                    DeptId = depts.FirstOrDefault().Id;
                    ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                    {
                        Id = b.DepartmentId,
                        ArName = b.Department.Code + " - " + b.Department.ArName
                    }), "Id", "ArName", DeptId);
                    if (IsSearch != true)
                    {
                        departmentId = DeptId;
                    }
                }
                else
                {
                    ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                    {
                        Id = b.DepartmentId,
                        ArName = b.Department.Code + " - " + b.Department.ArName
                    }), "Id", "ArName", departmentId);
                }
            }
//------------------------------------------------------------------------------------------------------------------------------------//
            /////////////////////////// Search ////////////////////

            IQueryable<ItemGroup> itemGroups;

            if (string.IsNullOrEmpty(searchWord))
            {
                if (userId == 1)
                {
                    itemGroups = db.ItemGroups.Where(s => s.IsDeleted == false && (departmentId == 0 ||s.IsInAllDepartments == true || s.ItemGroupDepartments.Any(a=>a.DepartmentId == departmentId))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.ItemGroups.Where(c => c.IsDeleted == false && (departmentId == 0 || c.IsInAllDepartments == true || c.ItemGroupDepartments.Any(a => a.DepartmentId == departmentId))).Count();
                }
                else
                {
                    itemGroups = db.ItemGroups.Where(s => s.IsDeleted == false && (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains( departmentId)) && (departmentId == 0 || s.IsInAllDepartments == true || s.ItemGroupDepartments.Any(a => a.DepartmentId == departmentId))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.ItemGroups.Where(c => c.IsDeleted == false && (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(departmentId)) && (departmentId == 0 || c.IsInAllDepartments == true || c.ItemGroupDepartments.Any(a => a.DepartmentId == departmentId))).Count();
                }
            }
            else
            {
                if (userId == 1)
                {
                    itemGroups = db.ItemGroups.Where(s => s.IsDeleted == false && (departmentId == 0 || s.IsInAllDepartments == true || s.ItemGroupDepartments.Any(a => a.DepartmentId == departmentId)) && (s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.Code.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.ItemGroups.Where(s => s.IsDeleted == false&& (departmentId == 0 || s.IsInAllDepartments == true || s.ItemGroupDepartments.Any(a=>a.DepartmentId == departmentId)) && (s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.Code.Contains(searchWord))).OrderBy(s => s.Id).Count();
                }
                else
                {
                    itemGroups = db.ItemGroups.Where(s => s.IsDeleted == false && (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(departmentId)) && (departmentId == 0 || s.IsInAllDepartments == true || s.ItemGroupDepartments.Any(a => a.DepartmentId == departmentId)) && (s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.Code.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.ItemGroups.Where(s => s.IsDeleted == false&& (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(departmentId)) && (departmentId == 0|| s.IsInAllDepartments == true || s.ItemGroupDepartments.Any(a => a.DepartmentId == departmentId)) && (s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.Code.Contains(searchWord))).OrderBy(s => s.Id).Count();
                }
                ///////////////////////////////////////////////////////////////////////////////

            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(itemGroups.ToList());


        }

        public ActionResult AddEdit(int? id)
        {
            var SystemPageId = db.SystemPages.Where(a => a.IsActive == true && a.IsDeleted == false && a.ControllerName == "ItemGroups").FirstOrDefault().Id;

            if (id == null)
            {
                ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
                {
                    b.Id,
                    ArName = b.FixedPart + " - " + b.ArName
                }), "Id", "ArName");
                //Department
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.ParentItemGroupId = new SelectList(db.ItemGroups.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.TypeId = new SelectList(new List<dynamic> {
                   new { Id=1, ArName="رئيسى"},
                    new { Id=2, ArName="وسيط"} ,
                    new { Id=3, ArName="فرعي"}}, "Id", "ArName");
                return View();
            }
            ItemGroup itemGroup = db.ItemGroups.Find(id);
            if (itemGroup == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل مجموعة الأصناف ",
                EnAction = "AddEdit",
                ControllerName = "ItemGroups",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = itemGroup.Id,
                ArItemName = itemGroup.ArName,
                EnItemName = itemGroup.EnName,
                CodeOrDocNo = itemGroup.Code
            });
            ViewBag.Next = QueryHelper.Next((int)id, "ItemGroup");
            ViewBag.Previous = QueryHelper.Previous((int)id, "ItemGroup");
            ViewBag.Last = QueryHelper.GetLast("ItemGroup");
            ViewBag.First = QueryHelper.GetFirst("ItemGroup");

            //Department
            ViewBag.DepartmentId = new SelectList(db.Departments.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");



            //ViewBag.ParentItemGroupId = new SelectList(db.ItemGroups.Where(a => a.IsActive == true && a.IsDeleted == false && (id.HasValue ? true : a.Id != id.Value)).Select(b => new
            //{
            //    Id = b.Id,
            //    ArName = b.Code + " - " + b.ArName
            //}), "Id", "ArName",itemGroup);

            ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
            {
                b.Id,
                ArName = b.FixedPart + " - " + b.ArName
            }), "Id", "ArName", itemGroup.FieldsCodingId);

            ViewBag.TypeId = new SelectList(new List<dynamic> {
                   new { Id=1, ArName="رئيسى"},
                    new { Id=2, ArName="وسيط"} ,
                    new { Id=3, ArName="فرعي"}}, "Id", "ArName", itemGroup.TypeId);

            if (itemGroup.TypeId == 3) // فرعى
            {
                ViewBag.ParentItemGroupId = new SelectList(db.ItemGroups.Where(c => c.IsActive == true && c.IsDeleted == false && c.TypeId == 2).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }).ToList(), "Id", "ArName", itemGroup.ParentItemGroupId);
            }
            else if (itemGroup.TypeId == 2)
            {
                ViewBag.ParentItemGroupId = new SelectList(db.ItemGroups.Where(c => c.IsActive == true && c.IsDeleted == false && c.TypeId == 1).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }).ToList(), "Id", "ArName", itemGroup.ParentItemGroupId);
            }
            else
            {
                ViewBag.ParentItemGroupId = new SelectList(db.ItemGroups.Where(c => c.IsActive == true && c.IsDeleted == false && c.ParentItemGroupId == null && c.TypeId == 1).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }).ToList(), "Id", "ArName", itemGroup.ParentItemGroupId);
            }
            return View(itemGroup);
        }

        [HttpPost]
        //   [ValidateAntiForgeryToken]
        public ActionResult AddEdit(ItemGroup itemGroup)
        {
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            bool isECommerce = System.Web.Configuration.WebConfigurationManager.AppSettings["ECommerce"] == "true" ? true : false;
            bool isOnline = System.Web.Configuration.WebConfigurationManager.AppSettings["Online"] == "true" ? true : false;

            if (ModelState.IsValid)
            {
                var id = itemGroup.Id;
                itemGroup.IsDeleted = false;

                if (itemGroup.Id > 0)
                {
                    itemGroup.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                    var old = db.ItemGroups.Find(id);
                    old.ArName = itemGroup.ArName;
                    old.EnName = itemGroup.EnName;
                    old.Code = itemGroup.Code;
                    old.IsActive = itemGroup.IsActive;
                    old.IsDeleted = itemGroup.IsDeleted;
                    old.IsInAllDepartments = itemGroup.IsInAllDepartments;
                    old.IsInPos = itemGroup.IsInPos;
                    old.ParentItemGroupId = itemGroup.ParentItemGroupId;
                    old.ShowInMainPage = itemGroup.ShowInMainPage;
                    old.ShowInMenu = itemGroup.ShowInMenu;
                    old.MenuOrder = itemGroup.MenuOrder;
                    old.MainPageOrder = itemGroup.MainPageOrder;
                    old.FieldsCodingId = itemGroup.FieldsCodingId;
                    old.TypeId = itemGroup.TypeId;
                    old.PrinterName = itemGroup.PrinterName;

                    db.ItemGroupDepartments.RemoveRange(db.ItemGroupDepartments.Where(p => p.ItemGroupId == old.Id).ToList());

                    foreach (var item in itemGroup.ItemGroupDepartments)
                    {
                        old.ItemGroupDepartments.Add(item);
                    }

                    if (isECommerce == true)
                    {
                        using (MySoftERPEntity dbItemGroup = new MySoftERPEntity())
                        {
                            ItemGroup deletedImage = dbItemGroup.ItemGroups.FirstOrDefault(i => i.Id == itemGroup.Id);

                            if (itemGroup.Image != null && itemGroup.Image.Contains("base64"))
                                new AmazonHelper().DeleteAnObject("Groups/" + deletedImage.Image);
                        }
                    }



                    if (itemGroup.Image != null && itemGroup.Image.Contains("base64"))
                    {
                        string fileName = "";

                        if (isECommerce == false)
                        {
                            string directoryPath = Server.MapPath("/images/Groups/");
                            if (!Directory.Exists(directoryPath))
                            {
                                Directory.CreateDirectory(directoryPath);
                            }
                            fileName = "/images/Groups/ItemGroup_" + itemGroup.Id.ToString() + "_" + DateTime.Now.Ticks + ".jpeg";
                            var bytes = new byte[7000];
                            if (itemGroup.Image.Contains("jpeg"))
                            {
                                bytes = Convert.FromBase64String(itemGroup.Image.Replace("data:image/jpeg;base64,", ""));
                            }
                            else
                            {
                                bytes = Convert.FromBase64String(itemGroup.Image.Replace("data:image/png;base64,", ""));
                            }
                            using (var imageFile = new FileStream(Server.MapPath(fileName), FileMode.Create))
                            {
                                imageFile.Write(bytes, 0, bytes.Length);
                                imageFile.Flush();
                            }
                            old.Image = domainName + fileName;
                        }
                        else
                        {
                            fileName = "ItemGroup_" + itemGroup.Id.ToString() + "_" + DateTime.Now.Ticks + ".jpeg";
                            if (new AmazonHelper().WritingAnObject("Groups/" + fileName, itemGroup.Image))
                                old.Image = fileName;
                        }
                    }
                    db.Entry(old).State = EntityState.Modified;

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("ItemGroups", "Edit", "AddEdit", id, null, " مجموعات الأصناف");
                    /////////-----------------------------------------------------------------------

                    // Add DB Change
                    QueryHelper.AddDBChange(new DBChange()
                    {
                        TableName = "ItemGroup",
                        SelectedId = old.Id,
                        IsMasterChange = isOnline,
                        IsNew = false,
                        IsTransaction = false
                    });
                }
                else
                {
                    if (itemGroup.Image != null && itemGroup.Image.Contains("base64"))
                    {
                        string fileName = "";
                        if (isECommerce == false)
                        {
                            fileName = "/images/Groups/ItemGroup_" + DateTime.Now.Ticks + ".jpeg";
                            var bytes = new byte[7000];
                            if (itemGroup.Image.Contains("jpeg"))
                            {
                                bytes = Convert.FromBase64String(itemGroup.Image.Replace("data:image/jpeg;base64,", ""));
                            }
                            else
                            {
                                bytes = Convert.FromBase64String(itemGroup.Image.Replace("data:image/png;base64,", ""));
                            }
                            using (var imageFile = new FileStream(Server.MapPath(fileName), FileMode.Create))
                            {
                                imageFile.Write(bytes, 0, bytes.Length);
                                imageFile.Flush();
                            }
                            itemGroup.Image = domainName + fileName;
                        }
                        else
                        {
                            fileName = "ItemGroup_" + DateTime.Now.Ticks + ".jpeg";
                            if (new AmazonHelper().WritingAnObject("Groups/" + fileName, itemGroup.Image))
                                itemGroup.Image = fileName;
                        }
                    }
                    itemGroup.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    // itemGroup.Code = (QueryHelper.CodeLastNum("ItemGroup") + 1).ToString();
                    itemGroup.Code = new JavaScriptSerializer().Serialize(SetCodeNum(itemGroup.FieldsCodingId).Data).ToString().Trim('"');

                    itemGroup.IsActive = true;
                    db.ItemGroups.Add(itemGroup);
                    db.SaveChanges();

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("ItemGroups", "Add", "AddEdit", itemGroup.Id, null, "مجموعات الأصناف");

                    ///////////-----------------------------------------------------------------------

                    // Add DB Change
                    var SelectedId = db.ItemGroups.OrderByDescending(r => r.Id).FirstOrDefault().Id;
                    QueryHelper.AddDBChange(new DBChange()
                    {
                        TableName = "ItemGroup",
                        SelectedId = SelectedId,
                        IsMasterChange = isOnline,
                        IsNew = true,
                        IsTransaction = false
                    });
                }

                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {

                    var mod = ModelState.First(c => c.Key == "Code");  // this

                    mod.Value.Errors.Add("هذا الكود موجود من قبل");
                    return View(itemGroup);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل مجموعة الأصناف" : "اضافة مجموعة أصناف",
                    EnAction = "AddEdit",
                    ControllerName = "ItemGroups",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = itemGroup.Id,
                    ArItemName = itemGroup.ArName,
                    EnItemName = itemGroup.EnName,
                    CodeOrDocNo = itemGroup.Code
                });
                return Json(new { success = "true" });
            }
            return View(itemGroup);
        }


        [HttpPost, ActionName("Delete")]

        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                ItemGroup itemGroup = db.ItemGroups.Find(id);
                itemGroup.IsDeleted = true;
                itemGroup.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                Random random = new Random();
                const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
                itemGroup.Code = Code;
                itemGroup.FieldsCodingId = null;
                db.Entry(itemGroup).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف مجموعة الأصناف",
                    EnAction = "AddEdit",
                    ControllerName = "ItemGroups",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = itemGroup.EnName,
                    ArItemName = itemGroup.ArName,
                    CodeOrDocNo = itemGroup.Code
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("ItemGroups", "Delete", "Delete", id, null, "مجموعات الأصناف");

                //int pageid = db.Get_PageId("ItemGroups").SingleOrDefault().Value;
                //var actionId = db.PageActions.FirstOrDefault(c => c.Action == "Delete" && c.EnName == "Delete" && c.PageId == pageid).Id;
                //int userid = db.UserNotifications.FirstOrDefault(c => c.ActionId == actionId && c.PageId == pageid).UserId.Value;
                //var UserName = User.Identity.Name;
                //db.Sp_OccuredNotification(actionId, $"بحذف بيانات في شاشة مجموعات الأصناف  {UserName}قام المستخدم  ");
                ////////////////-----------------------------------------------------------------------

                // Add DB Change
                QueryHelper.AddDBChange(new DBChange()
                {
                    TableName = "ItemGroup",
                    SelectedId = itemGroup.Id,
                    IsMasterChange = true,
                    IsNew = false,
                    IsTransaction = false
                });

                return Content("true");
            }
            catch (Exception)
            {

                throw;
            }
        }

        [HttpPost]
        public ActionResult ActivateDeactivate(int id)
        {
            try
            {
                ItemGroup itemGroup = db.ItemGroups.Find(id);
                if (itemGroup.IsActive == true)
                {
                    itemGroup.IsActive = false;
                }
                else
                {
                    itemGroup.IsActive = true;
                }
                itemGroup.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(itemGroup).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)itemGroup.IsActive ? "تنشيط مجموعة الأصناف" : "إلغاء تنشيط مجموعة الأصناف",
                    EnAction = "AddEdit",
                    ControllerName = "ItemGroups",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = itemGroup.Id,
                    EnItemName = itemGroup.EnName,
                    ArItemName = itemGroup.ArName,
                    CodeOrDocNo = itemGroup.Code
                });
                ////-------------------- Notification-------------------------////
                if (itemGroup.IsActive == true)
                {
                    Notification.GetNotification("ItemGroups", "Activate/Deactivate", "ActivateDeactivate", id, true, "مجموعات الأصناف");
                }
                else
                {

                    Notification.GetNotification("ItemGroups", "Activate/Deactivate", "ActivateDeactivate", id, false, "مجموعات الأصناف");
                }
                //int pageid = db.Get_PageId("ItemGroups").SingleOrDefault().Value;
                //var actionId = db.PageActions.FirstOrDefault(c => c.Action == "ActivateDeactivate" && c.EnName == "Activate/Deactivate" && c.PageId == pageid).Id;
                //int userid = db.UserNotifications.FirstOrDefault(c => c.ActionId == actionId && c.PageId == pageid).UserId.Value;
                //var UserName = User.Identity.Name;
                //db.Sp_OccuredNotification(actionId, (bool)itemGroup.IsActive ? $" تنشيط  في شاشة مجموعات الأصناف{UserName}قام المستخدم  " : $"إلغاء تنشيط  في شاشة مجموعات الأصناف{UserName}قام المستخدم  ");
                ////////////////-----------------------------------------------------------------------

                // Add DB Change
                QueryHelper.AddDBChange(new DBChange()
                {
                    TableName = "ItemGroup",
                    SelectedId = itemGroup.Id,
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

        [SkipERPAuthorize]
        public JsonResult SetCodeNum(int? FieldsCodingId)
        {
            //var code = QueryHelper.CodeLastNum("ItemGroup");
            //return Json(code + 1, JsonRequestBehavior.AllowGet);
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
                    var code = db.ItemGroups.Where(a => a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
                    var code = db.ItemGroups.Where(a => a.Code.Contains(fixedPart) && a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
                var code = db.ItemGroups.Where(a => a.FieldsCodingId == null && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
        }

        [SkipERPAuthorize]
        public ActionResult GetItemByType(int? TypeId)
        {
            var parent = db.ItemGroups.Where(a => a.IsActive == true && a.IsDeleted == false && a.ParentItemGroupId == null && a.TypeId == 1)
                               .Select(a => new
                               {
                                   a.Id,
                                   ArName = a.Code + " - " + a.ArName
                               }).ToList();
            if (TypeId == 3)
            {
                parent = db.ItemGroups.Where(a => a.IsActive == true && a.IsDeleted == false && (a.TypeId == 2||a.TypeId==1))
                              .Select(a => new
                              {
                                  a.Id,
                                  ArName = a.Code + " - " + a.ArName
                              }).ToList();
            }

            return Json(parent, JsonRequestBehavior.AllowGet);
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
