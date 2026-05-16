using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using System;
using System.Web.Mvc;

namespace MyERP.Areas.Pos.Controllers
{
    public class PosLegacyAdminController : Controller
    {
        private readonly PosSqlRepository _posRepository;
        private readonly PosLegacyAdminRepository _repository;

        public PosLegacyAdminController()
        {
            _posRepository = new PosSqlRepository();
            _repository = new PosLegacyAdminRepository();
        }

        public ActionResult Users(string searchText, int? id)
        {
            var context = RequireAdmin();
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            ViewBag.ActiveScreen = "legacy-users";
            return View(_repository.LoadUsers(searchText, id));
        }

        public ActionResult BranchesData(int? id)
        {
            var context = RequireAdmin();
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            ViewBag.ActiveScreen = "legacy-branches-data";
            return View(_repository.LoadBranchesData(id));
        }

        [HttpGet]
        public JsonResult NewUser()
        {
            if (RequireAdminJson() == null)
            {
                return Json(new { success = false, message = "هذه الشاشة للمدير فقط" }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, data = _repository.NewUser() }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult UserDetails(int id)
        {
            if (RequireAdminJson() == null)
            {
                return Json(new { success = false, message = "هذه الشاشة للمدير فقط" }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, data = _repository.GetUser(id) }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SaveUser(PosLegacyUserEditModel request)
        {
            if (RequireAdminJson() == null)
            {
                return Json(new PosLegacySaveResult { Success = false, Message = "هذه الشاشة للمدير فقط" });
            }

            try
            {
                var result = _repository.SaveUser(request);
                if (!result.Success) { Response.StatusCode = 400; }
                return Json(result);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 400;
                return Json(new PosLegacySaveResult { Success = false, Message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult DeleteUser(int id)
        {
            if (RequireAdminJson() == null)
            {
                return Json(new PosLegacySaveResult { Success = false, Message = "هذه الشاشة للمدير فقط" });
            }

            try
            {
                var result = _repository.DeleteUser(id);
                if (!result.Success) { Response.StatusCode = 400; }
                return Json(result);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 400;
                return Json(new PosLegacySaveResult { Success = false, Message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SaveBranchesData(PosActivityEditModel activity, System.Collections.Generic.IList<PosBranchDataEditModel> branches)
        {
            if (RequireAdminJson() == null)
            {
                return Json(new PosLegacySaveResult { Success = false, Message = "هذه الشاشة للمدير فقط" });
            }

            try
            {
                var result = _repository.SaveBranchesData(activity, branches);
                if (!result.Success) { Response.StatusCode = 400; }
                return Json(result);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 400;
                return Json(new PosLegacySaveResult { Success = false, Message = ex.Message });
            }
        }

        private PosUserContext RequireAdmin()
        {
            var context = PosLoginController.RestorePosContext(Request, Session, _posRepository);
            if (context == null || !context.IsFullAccess)
            {
                return null;
            }

            return context;
        }

        private PosUserContext RequireAdminJson()
        {
            var context = PosLoginController.RestorePosContext(Request, Session, _posRepository);
            if (context == null)
            {
                Response.StatusCode = 401;
                return null;
            }

            if (!context.IsFullAccess)
            {
                Response.StatusCode = 403;
                return null;
            }

            return context;
        }
    }
}
