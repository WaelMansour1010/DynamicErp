using System;
using System.IO;
using System.Net;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using MyERP.Areas.Sync.Data;
using MyERP.Areas.Sync.Security;
using MyERP.Areas.Sync.ViewModels;

namespace MyERP.Areas.Sync.Controllers
{
    public class BranchApiController : SyncControllerBase
    {
        private readonly JavaScriptSerializer serializer = new JavaScriptSerializer { MaxJsonLength = Int32.MaxValue };
        private readonly BranchApiAuthenticator authenticator = new BranchApiAuthenticator();
        private readonly BranchIngestionRepository repository = new BranchIngestionRepository();

        [HttpGet]
        public ActionResult Ping()
        {
            var branchId = ParseBranchId(Request.Headers["X-Branch-Id"]);
            try
            {
                authenticator.Validate(Request, branchId, "", "ping");
                return Json(new BranchApiResult { Accepted = true, Status = "Accepted", Message = "Branch API is reachable." }, JsonRequestBehavior.AllowGet);
            }
            catch (UnauthorizedAccessException ex)
            {
                repository.RecordAuthFailure(branchId, ex.Message, Request.UserHostAddress);
                return JsonError(HttpStatusCode.Forbidden, ex.Message);
            }
            catch (Exception ex)
            {
                return JsonError(HttpStatusCode.BadRequest, ex.Message);
            }
        }

        [HttpPost]
        public ActionResult Heartbeat()
        {
            try
            {
                var raw = ReadBody();
                var heartbeat = serializer.Deserialize<BranchHeartbeatRequest>(raw);
                authenticator.Validate(Request, heartbeat != null ? heartbeat.BranchId : 0, raw, "heartbeat");
                return Json(repository.SaveHeartbeat(heartbeat));
            }
            catch (UnauthorizedAccessException ex)
            {
                var branchId = ExtractHeartbeatBranchId();
                repository.RecordAuthFailure(branchId, ex.Message, Request.UserHostAddress);
                return JsonError(HttpStatusCode.Forbidden, ex.Message);
            }
            catch (Exception ex)
            {
                return JsonError(HttpStatusCode.BadRequest, ex.Message);
            }
        }

        [HttpPost]
        public ActionResult Outbox()
        {
            try
            {
                var raw = ReadBody();
                var envelope = serializer.Deserialize<BranchOutboxEnvelope>(raw);
                authenticator.Validate(Request, envelope != null ? envelope.BranchId : 0, raw, envelope != null ? envelope.PayloadHash : "");
                var result = repository.SaveOutbox(envelope, raw, Request.UserHostAddress);
                if (!result.Accepted && String.Equals(result.Status, "Conflict", StringComparison.OrdinalIgnoreCase))
                {
                    Response.StatusCode = (int)HttpStatusCode.Conflict;
                }

                return Json(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                var branchId = ExtractOutboxBranchId();
                repository.RecordAuthFailure(branchId, ex.Message, Request.UserHostAddress);
                return JsonError(HttpStatusCode.Forbidden, ex.Message);
            }
            catch (Exception ex)
            {
                return JsonError(HttpStatusCode.BadRequest, ex.Message);
            }
        }

        [HttpPost]
        public ActionResult Ack(string syncKey)
        {
            try
            {
                var raw = ReadBody();
                authenticator.Validate(Request, ParseBranchId(syncKey), raw, "ack");
                return Json(repository.Ack(syncKey));
            }
            catch (UnauthorizedAccessException ex)
            {
                repository.RecordAuthFailure(ParseBranchId(syncKey), ex.Message, Request.UserHostAddress);
                return JsonError(HttpStatusCode.Forbidden, ex.Message);
            }
            catch (Exception ex)
            {
                return JsonError(HttpStatusCode.BadRequest, ex.Message);
            }
        }

        private string ReadBody()
        {
            Request.InputStream.Position = 0;
            using (var reader = new StreamReader(Request.InputStream))
            {
                return reader.ReadToEnd();
            }
        }

        private ActionResult JsonError(HttpStatusCode status, string message)
        {
            Response.StatusCode = (int)status;
            Response.TrySkipIisCustomErrors = true;
            Response.SuppressFormsAuthenticationRedirect = true;
            return Json(new BranchApiResult { Accepted = false, Status = status.ToString(), Message = message }, JsonRequestBehavior.AllowGet);
        }

        private static int ParseBranchId(string syncKey)
        {
            if (String.IsNullOrWhiteSpace(syncKey))
            {
                return 0;
            }

            var parts = syncKey.Split(':');
            int branchId;
            return parts.Length > 0 && Int32.TryParse(parts[0], out branchId) ? branchId : 0;
        }

        private int ExtractHeartbeatBranchId()
        {
            try
            {
                var raw = ReadBody();
                var heartbeat = serializer.Deserialize<BranchHeartbeatRequest>(raw);
                return heartbeat != null ? heartbeat.BranchId : ParseBranchId(Request.Headers["X-Branch-Id"]);
            }
            catch
            {
                return ParseBranchId(Request.Headers["X-Branch-Id"]);
            }
        }

        private int ExtractOutboxBranchId()
        {
            try
            {
                var raw = ReadBody();
                var envelope = serializer.Deserialize<BranchOutboxEnvelope>(raw);
                return envelope != null ? envelope.BranchId : ParseBranchId(Request.Headers["X-Branch-Id"]);
            }
            catch
            {
                return ParseBranchId(Request.Headers["X-Branch-Id"]);
            }
        }
    }
}
