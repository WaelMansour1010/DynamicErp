using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web;

namespace MyERP.Areas.Pos.Services
{
    public static class PosSystemErrorLogger
    {
        public static void Log(
            PosSqlRepository repository,
            HttpRequestBase request,
            PosUserContext context,
            string screenName,
            string actionName,
            string operationType,
            int? transactionId,
            string message,
            Exception exception,
            string requestSummary,
            string severity,
            string status)
        {
            try
            {
                var log = new PosSystemErrorLogWriteRequest
                {
                    Severity = string.IsNullOrWhiteSpace(severity) ? "Error" : severity,
                    Status = string.IsNullOrWhiteSpace(status) ? "Failed" : status,
                    UserId = context == null ? (int?)null : context.UserId,
                    UserName = context == null ? null : context.UserName,
                    BranchId = context == null ? null : context.BranchId,
                    ScreenName = screenName,
                    ActionName = actionName,
                    OperationType = operationType,
                    TransactionId = transactionId,
                    ErrorMessage = Truncate(message ?? (exception == null ? null : exception.Message), 2000),
                    StackTrace = Truncate(exception == null ? null : exception.ToString(), 8000),
                    RequestSummary = Truncate(requestSummary, 4000),
                    IpAddress = Truncate(GetClientIp(request), 64),
                    UserAgent = Truncate(request == null ? null : request.UserAgent, 512)
                };

                if (repository != null)
                {
                    repository.InsertPosSystemErrorLog(log);
                    return;
                }
            }
            catch
            {
                // Logging must never break the user operation. Fall back to a small file log below.
            }

            TryWriteFallback(request, context, screenName, actionName, operationType, transactionId, message, exception, requestSummary, severity, status);
        }

        private static void TryWriteFallback(
            HttpRequestBase request,
            PosUserContext context,
            string screenName,
            string actionName,
            string operationType,
            int? transactionId,
            string message,
            Exception exception,
            string requestSummary,
            string severity,
            string status)
        {
            try
            {
                var root = HttpContext.Current == null
                    ? AppDomain.CurrentDomain.BaseDirectory
                    : HttpContext.Current.Server.MapPath("~/App_Data/Logs");
                if (!Directory.Exists(root))
                {
                    Directory.CreateDirectory(root);
                }

                var path = Path.Combine(root, "pos-system-errors-" + DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + ".log");
                var builder = new StringBuilder();
                builder.AppendLine(new string('-', 72));
                builder.AppendLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
                builder.AppendLine("Severity: " + (severity ?? "Error") + "; Status: " + (status ?? "Failed"));
                builder.AppendLine("UserId: " + (context == null ? "" : context.UserId.ToString(CultureInfo.InvariantCulture)) + "; UserName: " + (context == null ? "" : context.UserName));
                builder.AppendLine("BranchId: " + (context == null || !context.BranchId.HasValue ? "" : context.BranchId.Value.ToString(CultureInfo.InvariantCulture)));
                builder.AppendLine("Screen: " + screenName + "; Action: " + actionName + "; Operation: " + operationType);
                builder.AppendLine("TransactionId: " + (transactionId.HasValue ? transactionId.Value.ToString(CultureInfo.InvariantCulture) : ""));
                builder.AppendLine("IP: " + GetClientIp(request));
                builder.AppendLine("Message: " + message);
                builder.AppendLine("Request: " + requestSummary);
                if (exception != null)
                {
                    builder.AppendLine(exception.ToString());
                }

                File.AppendAllText(path, builder.ToString(), Encoding.UTF8);
            }
            catch
            {
                // Final fallback intentionally ignored.
            }
        }

        private static string GetClientIp(HttpRequestBase request)
        {
            if (request == null)
            {
                return null;
            }

            var forwardedFor = request.Headers["X-Forwarded-For"];
            if (!string.IsNullOrWhiteSpace(forwardedFor))
            {
                var comma = forwardedFor.IndexOf(',');
                return comma > 0 ? forwardedFor.Substring(0, comma).Trim() : forwardedFor.Trim();
            }

            return request.UserHostAddress;
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value;
            }

            return value.Substring(0, maxLength);
        }
    }
}
