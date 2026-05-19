using System;
using System.Collections.Generic;

namespace MyERP.Common.DatabaseUpdates
{
    public class SharedDatabaseUpdateDashboard
    {
        public SharedDatabaseUpdateDashboard()
        {
            Scripts = new List<SharedDatabaseUpdateScript>();
            RecentHistory = new List<SharedDatabaseUpdateHistory>();
        }

        public string DatabaseName { get; set; }
        public string ServerName { get; set; }
        public int PendingCount { get; set; }
        public int AppliedCount { get; set; }
        public int HashMismatchCount { get; set; }
        public string Message { get; set; }
        public bool IsError { get; set; }
        public IList<SharedDatabaseUpdateScript> Scripts { get; set; }
        public IList<SharedDatabaseUpdateHistory> RecentHistory { get; set; }
    }

    public class SharedDatabaseUpdateScript
    {
        public string ScriptName { get; set; }
        public string RelativePath { get; set; }
        public string Hash { get; set; }
        public string Status { get; set; }
        public bool IsPending { get; set; }
        public bool HasHashMismatch { get; set; }
        public DateTime LastModifiedOn { get; set; }
        public DateTime? AppliedOn { get; set; }
        public string AppliedBy { get; set; }
        public string Purpose { get; set; }
    }

    public class SharedDatabaseUpdateHistory
    {
        public string ScriptName { get; set; }
        public string ScriptHash { get; set; }
        public DateTime AppliedOn { get; set; }
        public string AppliedBy { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class SharedDatabaseUpdateApplyResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int AppliedCount { get; set; }
        public int FailedCount { get; set; }
    }
}
