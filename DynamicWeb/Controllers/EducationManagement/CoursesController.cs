using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using MyERP.Repository;

namespace MyERP.Controllers.EducationManagement
{
    [AllowAnonymous]
    [SkipERPAuthorize]
    public class CoursesController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        // GET: CoursesBooking
        public ActionResult Index()
        {
            var Courses = db.EducationalSubjects.Where(a => a.IsActive == true && a.IsDeleted == false)
                //.Select(a => new { a.Id, a.ArName, a.Image, a.NoOfLessons, a.Notes })
                .ToList();
            return View(Courses);
        }
        public ActionResult Course(int? Id)
        {
            var Courses = db.Lessons.Where(a => a.IsActive == true && a.IsDeleted == false && a.EducationalSubjectId == Id).ToList();
            var sub = db.EducationalSubjects.Where(a => a.Id == Id).FirstOrDefault();
            ViewBag.EducationalSubjectImage = sub != null ? sub.Image : null;
            ViewBag.EducationalSubjectName = sub != null ? sub.ArName : null;
            return View(Courses);
        }

        //public JsonResult GetCourses()
        //{
        //    var Courses = db.EducationalSubjects.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => new { a.Id, a.ArName, a.Image,a.NoOfLessons,a.Notes }).ToList();
        //    return Json(Courses, JsonRequestBehavior.AllowGet);
        //}
    }
}