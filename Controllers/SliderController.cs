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

namespace MyERP.Controllers
{

    public class SliderController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة Slider",
                EnAction = "Index",
                ControllerName = "Slider",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("Slider", "View", "Index", null, null, "Slider");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<Slider> Sliders;

            Sliders = db.Sliders.Where(s => !s.IsDeleted).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
            ViewBag.Count = db.Sliders.Where(s => !s.IsDeleted).Count();

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(Sliders.ToList());


        }

        public ActionResult AddEdit(int? id)
        {

            if (id == null)
            {
                Slider NewObj = new Slider();

                return View(NewObj);
            }
            Slider Slider = db.Sliders.Find(id);
            if (Slider == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل Slider ",
                EnAction = "AddEdit",
                ControllerName = "Slider",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = Slider.Id,
                //ArItemName = Slider.ArName,
                //EnItemName = Slider.EnName,
                //CodeOrDocNo = Slider.Code
            });
            ViewBag.Next = QueryHelper.Next((int)id, "Slider");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Slider");
            ViewBag.Last = QueryHelper.GetLast("Slider");
            ViewBag.First = QueryHelper.GetFirst("Slider");
            return View(Slider);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddEdit(Slider Slider)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (Slider.Id > 0)
                    {
                        using (MySoftERPEntity dbSlider = new MySoftERPEntity())
                        {
                            Slider deletedImage = dbSlider.Sliders.FirstOrDefault(i => i.Id == Slider.Id);

                            if (Slider.Image != null && Slider.Image.Contains("base64"))
                                new AmazonHelper().DeleteAnObject("Slider/" + deletedImage.Image);
                        }

                        db.Entry(Slider).State = EntityState.Modified;

                        string fileName = "Slider_" + Slider.Id.ToString() + "_" + DateTime.Now.Ticks + ".jpeg";

                        if (Slider.Image.Contains("base64"))
                        {

                            if (new AmazonHelper().WritingAnObject("Slider/" + fileName, Slider.Image))
                                Slider.Image = fileName;

                            Slider.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                            Slider.URL = Slider.URL;
                            Slider.SliderOrder = Slider.SliderOrder;
                            db.SaveChanges();
                        }

                        ////-------------------- Notification-------------------------////
                        Notification.GetNotification("Slider", "Edit", "AddEdit", Slider.Id, null, "Slider");

                    }
                    else
                    {
                        string fileName = "Slider_" + DateTime.Now.Ticks + ".jpeg";

                        if (new AmazonHelper().WritingAnObject("Slider/" + fileName, Slider.Image))
                        {
                            db.Sliders.Add(new Slider()
                            {
                                IsActive = true,
                                IsDeleted = false,
                                Image = fileName,
                                URL = Slider.URL,
                                SliderOrder = Slider.SliderOrder,
                                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value)
                            });
                            db.SaveChanges();
                        }


                        ////-------------------- Notification-------------------------////
                        Notification.GetNotification("Slider", "Add", "AddEdit", Slider.Id, null, "Slider");

                    }



                }
                catch (Exception ex)
                {


                    return View(Slider);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = Slider.Id > 0 ? "تعديل Slider" : "اضافة دولة",
                    EnAction = "AddEdit",
                    ControllerName = "Slider",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = Slider.Id,
                    //ArItemName = Slider.ArName,
                    //EnItemName = Slider.EnName,
                    //CodeOrDocNo = Slider.Code
                });

                return RedirectToAction("Index");

            }

            return View(Slider);
        }


        [HttpPost, ActionName("Delete")]

        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                Slider Slider = db.Sliders.Find(id);
                Slider.IsDeleted = true;
                Slider.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                new AmazonHelper().DeleteAnObject("Slider/" + Slider.Image);

                db.Entry(Slider).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف Slider",
                    EnAction = "AddEdit",
                    ControllerName = "Slider",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    //EnItemName = Slider.EnName,
                    //ArItemName = Slider.ArName,
                    //CodeOrDocNo = Slider.Code
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("Slider", "Delete", "Delete", id, null, "Slider");
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
                Slider Slider = db.Sliders.Find(id);
                if (Slider.IsActive == true)
                {
                    Slider.IsActive = false;
                }
                else
                {
                    Slider.IsActive = true;
                }
                Slider.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(Slider).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)Slider.IsActive ? "تنشيط Slider" : "إلغاء تنشيط Slider",
                    EnAction = "AddEdit",
                    ControllerName = "Slider",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = Slider.Id,
                    //EnItemName = Slider.EnName,
                    //ArItemName = Slider.ArName,
                    //CodeOrDocNo = Slider.Code
                });
                ////-------------------- Notification-------------------------////
                if (Slider.IsActive == true)
                {
                    Notification.GetNotification("Slider", "Activate/Deactivate", "ActivateDeactivate", id, true, "Slider");
                }
                else
                {

                    Notification.GetNotification("Slider", "Activate/Deactivate", "ActivateDeactivate", Slider.Id, false, "Slider");
                }


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
