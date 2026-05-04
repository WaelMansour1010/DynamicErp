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
}
