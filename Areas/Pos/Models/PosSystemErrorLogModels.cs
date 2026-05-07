using System;
using System.Collections.Generic;

namespace MyERP.Areas.Pos.Models
{
    public class PosSystemErrorLogEntry
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Severity { get; set; }
        public string Status { get; set; }
        public int? UserId { get; set; }
        public string UserName { get; set; }
        public int? BranchId { get; set; }
        public string BranchName { get; set; }
        public string ScreenName { get; set; }
        public string ActionName { get; set; }
        public string OperationType { get; set; }
        public int? TransactionId { get; set; }
        public string ErrorMessage { get; set; }
        public string RequestSummary { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
    }

    public class PosSystemErrorLogWriteRequest
    {
        public string Severity { get; set; }
        public string Status { get; set; }
        public int? UserId { get; set; }
        public string UserName { get; set; }
        public int? BranchId { get; set; }
        public string ScreenName { get; set; }
        public string ActionName { get; set; }
        public string OperationType { get; set; }
        public int? TransactionId { get; set; }
        public string ErrorMessage { get; set; }
        public string StackTrace { get; set; }
        public string RequestSummary { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
    }

    public class PosSystemErrorLogSearchRequest
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? UserId { get; set; }
        public int? BranchId { get; set; }
        public string UserKeyword { get; set; }
        public string ScreenAction { get; set; }
        public string OperationType { get; set; }
        public string Keyword { get; set; }
        public string Severity { get; set; }
        public int PageSize { get; set; }
    }

    public class PosSystemErrorLogSearchResult
    {
        public IList<PosSystemErrorLogEntry> Items { get; set; }
        public int Count { get; set; }
    }

    public class PosSaveAttemptLogWriteRequest
    {
        public Guid SaveAttemptId { get; set; }
        public string EventName { get; set; }
        public int? UserID { get; set; }
        public int? EmpID { get; set; }
        public int? BranchId { get; set; }
        public string TransactionType { get; set; }
        public int? RetryAttempt { get; set; }
        public int? SqlErrorNumber { get; set; }
        public int? DelayMs { get; set; }
        public int? DurationMs { get; set; }
        public int? Transaction_ID { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public string RequestSummary { get; set; }
    }

    public class PosSaveAttemptSearchRequest
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? BranchId { get; set; }
        public int? UserId { get; set; }
        public string UserKeyword { get; set; }
        public string TransactionType { get; set; }
        public string EventName { get; set; }
        public string Status { get; set; }
        public int PageSize { get; set; }
    }

    public class PosSaveAttemptTimelineEntry
    {
        public int Id { get; set; }
        public string SaveAttemptId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string EventName { get; set; }
        public int? RetryAttempt { get; set; }
        public int? SqlErrorNumber { get; set; }
        public int? DelayMs { get; set; }
        public int? DurationMs { get; set; }
        public int? Transaction_ID { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public string RequestSummary { get; set; }
    }

    public class PosSaveAttemptGridRow
    {
        public string SaveAttemptId { get; set; }
        public int? UserID { get; set; }
        public string UserName { get; set; }
        public int? EmpID { get; set; }
        public int? BranchId { get; set; }
        public string BranchName { get; set; }
        public string TransactionType { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int? DurationMs { get; set; }
        public int RetryCount { get; set; }
        public string FinalStatus { get; set; }
        public int? Transaction_ID { get; set; }
        public string LastErrorMessage { get; set; }
        public IList<PosSaveAttemptTimelineEntry> Timeline { get; set; }
    }

    public class PosSaveAttemptSummary
    {
        public int TotalSaveAttempts { get; set; }
        public int DeadlockAffectedAttempts { get; set; }
        public int RetriedSucceeded { get; set; }
        public int RetriedFailed { get; set; }
        public decimal AverageDurationMs { get; set; }
        public int MaxDurationMs { get; set; }
        public string TopDeadlockBranch { get; set; }
    }

    public class PosSaveAttemptSearchResult
    {
        public IList<PosSaveAttemptGridRow> Items { get; set; }
        public PosSaveAttemptSummary Summary { get; set; }
        public int Count { get; set; }
    }
}
