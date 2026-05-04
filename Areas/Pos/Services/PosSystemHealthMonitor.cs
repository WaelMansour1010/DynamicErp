using MyERP.Areas.Pos.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Web;

namespace MyERP.Areas.Pos.Services
{
    public static class PosSystemHealthMonitor
    {
        private const string RequestStartKey = "DynamicErp.PosHealth.RequestStart";
        private static readonly object Sync = new object();
        private static readonly Queue<RequestMetric> Requests = new Queue<RequestMetric>();
        private static readonly Queue<PosSystemHealthErrorDto> Errors = new Queue<PosSystemHealthErrorDto>();
        private static readonly Queue<DateTime> SessionRestores = new Queue<DateTime>();
        private static readonly Dictionary<int, UserActivity> ActiveUsers = new Dictionary<int, UserActivity>();

        public static void BeginRequest(HttpContext context)
        {
            if (!IsPosRequest(context))
            {
                return;
            }

            context.Items[RequestStartKey] = Stopwatch.StartNew();
        }

        public static void EndRequest(HttpContext context)
        {
            if (!IsPosRequest(context))
            {
                return;
            }

            var stopwatch = context.Items[RequestStartKey] as Stopwatch;
            if (stopwatch == null)
            {
                return;
            }

            stopwatch.Stop();
            var statusCode = context.Response != null ? context.Response.StatusCode : 0;
            var path = context.Request != null ? context.Request.RawUrl : string.Empty;
            var error = context.Server != null ? context.Server.GetLastError() : null;
            RecordRequest(path, statusCode, stopwatch.ElapsedMilliseconds, error);
        }

        public static void TouchUser(PosUserContext context, HttpRequestBase request)
        {
            if (context == null || context.UserId <= 0)
            {
                return;
            }

            lock (Sync)
            {
                ActiveUsers[context.UserId] = new UserActivity
                {
                    UserId = context.UserId,
                    BranchId = context.BranchId,
                    UserName = context.UserName,
                    LastSeenUtc = DateTime.UtcNow,
                    Ip = request != null ? request.UserHostAddress : string.Empty
                };
                PruneActiveUsers(DateTime.UtcNow);
            }
        }

        public static void RecordSessionRestore(PosUserContext context)
        {
            lock (Sync)
            {
                SessionRestores.Enqueue(DateTime.UtcNow);
                PruneSessionRestores(DateTime.UtcNow);
            }
        }

        public static PosSystemHealthCoreDto GetCoreMetrics()
        {
            lock (Sync)
            {
                var now = DateTime.UtcNow;
                PruneRequests(now);
                PruneActiveUsers(now);
                PruneSessionRestores(now);

                var recentMinute = Requests.Where(r => r.TimestampUtc >= now.AddMinutes(-1)).ToList();
                var recentFiveMinutes = Requests.Where(r => r.TimestampUtc >= now.AddMinutes(-5)).ToList();
                var failed = recentFiveMinutes.Count(r => r.StatusCode >= 400);

                return new PosSystemHealthCoreDto
                {
                    ActiveUsers = ActiveUsers.Count,
                    RequestsPerMinute = recentMinute.Count,
                    AverageResponseMs = recentFiveMinutes.Count == 0 ? 0 : Math.Round((decimal)recentFiveMinutes.Average(r => r.ElapsedMs), 2),
                    ErrorRatePercent = recentFiveMinutes.Count == 0 ? 0 : Math.Round((decimal)failed * 100M / recentFiveMinutes.Count, 2),
                    SessionRestoresLastHour = SessionRestores.Count,
                    LastErrors = Errors.Reverse().Take(5).ToList()
                };
            }
        }

        public static PosSystemHealthPosDto GetPosMetrics()
        {
            lock (Sync)
            {
                var now = DateTime.UtcNow;
                PruneRequests(now);
                PruneSessionRestores(now);

                var saves = Requests
                    .Where(r => r.TimestampUtc >= now.AddHours(-1)
                        && r.Path.IndexOf("/Pos/PosTransaction/Save", StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();

                var failedSaves = saves.Where(r => r.StatusCode >= 400).ToList();

                return new PosSystemHealthPosDto
                {
                    InvoiceSaveAverageMs = saves.Count == 0 ? 0 : Math.Round((decimal)saves.Average(r => r.ElapsedMs), 2),
                    InvoiceSaveMaxMs = saves.Count == 0 ? 0 : saves.Max(r => r.ElapsedMs),
                    FailedSavesCount = failedSaves.Count,
                    SessionRestoreRatePerHour = SessionRestores.Count,
                    LastSaveErrors = Errors
                        .Reverse()
                        .Where(e => (e.Path ?? string.Empty).IndexOf("/Pos/PosTransaction/Save", StringComparison.OrdinalIgnoreCase) >= 0)
                        .Take(5)
                        .ToList()
                };
            }
        }

        private static void RecordRequest(string path, int statusCode, long elapsedMs, Exception error)
        {
            lock (Sync)
            {
                Requests.Enqueue(new RequestMetric
                {
                    TimestampUtc = DateTime.UtcNow,
                    Path = path ?? string.Empty,
                    StatusCode = statusCode,
                    ElapsedMs = elapsedMs
                });

                if (statusCode >= 400 || error != null)
                {
                    Errors.Enqueue(new PosSystemHealthErrorDto
                    {
                        Timestamp = DateTime.Now,
                        Path = SafePath(path),
                        StatusCode = statusCode,
                        Message = error != null ? error.GetType().Name : "HTTP " + statusCode.ToString(CultureInfo.InvariantCulture)
                    });
                }

                PruneRequests(DateTime.UtcNow);
                while (Errors.Count > 100)
                {
                    Errors.Dequeue();
                }
            }
        }

        private static bool IsPosRequest(HttpContext context)
        {
            if (context == null || context.Request == null)
            {
                return false;
            }

            var path = context.Request.AppRelativeCurrentExecutionFilePath ?? string.Empty;
            return path.StartsWith("~/Pos", StringComparison.OrdinalIgnoreCase);
        }

        private static string SafePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            var queryIndex = path.IndexOf('?');
            return queryIndex >= 0 ? path.Substring(0, queryIndex) : path;
        }

        private static void PruneRequests(DateTime nowUtc)
        {
            var cutoff = nowUtc.AddMinutes(-10);
            while (Requests.Count > 0 && Requests.Peek().TimestampUtc < cutoff)
            {
                Requests.Dequeue();
            }
        }

        private static void PruneSessionRestores(DateTime nowUtc)
        {
            var cutoff = nowUtc.AddHours(-1);
            while (SessionRestores.Count > 0 && SessionRestores.Peek() < cutoff)
            {
                SessionRestores.Dequeue();
            }
        }

        private static void PruneActiveUsers(DateTime nowUtc)
        {
            var cutoff = nowUtc.AddMinutes(-15);
            foreach (var key in ActiveUsers.Where(pair => pair.Value.LastSeenUtc < cutoff).Select(pair => pair.Key).ToList())
            {
                ActiveUsers.Remove(key);
            }
        }

        private class RequestMetric
        {
            public DateTime TimestampUtc { get; set; }
            public string Path { get; set; }
            public int StatusCode { get; set; }
            public long ElapsedMs { get; set; }
        }

        private class UserActivity
        {
            public int UserId { get; set; }
            public int? BranchId { get; set; }
            public string UserName { get; set; }
            public DateTime LastSeenUtc { get; set; }
            public string Ip { get; set; }
        }
    }
}
