using System;
using System.Collections.Generic;

namespace MyERP.Areas.Pos.Models
{
    public class PosSystemHealthSnapshotDto
    {
        public DateTime GeneratedAt { get; set; }
        public PosSystemHealthCoreDto Core { get; set; }
        public PosSystemHealthDatabaseDto Database { get; set; }
        public PosSystemHealthPosDto Pos { get; set; }
        public IList<PosSystemHealthResourceDto> Resources { get; set; }
        public IList<PosSystemHealthAlertDto> Alerts { get; set; }

        public PosSystemHealthSnapshotDto()
        {
            GeneratedAt = DateTime.Now;
            Core = new PosSystemHealthCoreDto();
            Database = new PosSystemHealthDatabaseDto();
            Pos = new PosSystemHealthPosDto();
            Resources = new List<PosSystemHealthResourceDto>();
            Alerts = new List<PosSystemHealthAlertDto>();
        }
    }

    public class PosSystemHealthCoreDto
    {
        public int ActiveUsers { get; set; }
        public decimal RequestsPerMinute { get; set; }
        public decimal AverageResponseMs { get; set; }
        public decimal ErrorRatePercent { get; set; }
        public int SessionRestoresLastHour { get; set; }
        public IList<PosSystemHealthErrorDto> LastErrors { get; set; }

        public PosSystemHealthCoreDto()
        {
            LastErrors = new List<PosSystemHealthErrorDto>();
        }
    }

    public class PosSystemHealthDatabaseDto
    {
        public IList<PosSlowQueryDto> SlowQueries { get; set; }
        public IList<PosBlockingSessionDto> BlockingSessions { get; set; }
        public long DeadlockCounter { get; set; }
        public int TransactionsPerMinute { get; set; }
        public string StatusMessage { get; set; }

        public PosSystemHealthDatabaseDto()
        {
            SlowQueries = new List<PosSlowQueryDto>();
            BlockingSessions = new List<PosBlockingSessionDto>();
        }
    }

    public class PosSystemHealthPosDto
    {
        public decimal InvoiceSaveAverageMs { get; set; }
        public decimal InvoiceSaveMaxMs { get; set; }
        public int FailedSavesCount { get; set; }
        public int SessionRestoreRatePerHour { get; set; }
        public IList<PosSystemHealthErrorDto> LastSaveErrors { get; set; }

        public PosSystemHealthPosDto()
        {
            LastSaveErrors = new List<PosSystemHealthErrorDto>();
        }
    }

    public class PosSystemHealthResourceDto
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string Status { get; set; }
    }

    public class PosSystemHealthAlertDto
    {
        public string Severity { get; set; }
        public string Icon { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
    }

    public class PosSystemHealthErrorDto
    {
        public DateTime Timestamp { get; set; }
        public string Path { get; set; }
        public int StatusCode { get; set; }
        public string Message { get; set; }
    }

    public class PosSlowQueryDto
    {
        public int SessionId { get; set; }
        public int ElapsedMs { get; set; }
        public string Command { get; set; }
        public string ProcedureName { get; set; }
        public string WaitType { get; set; }
    }

    public class PosBlockingSessionDto
    {
        public int SessionId { get; set; }
        public int BlockingSessionId { get; set; }
        public string WaitType { get; set; }
        public int WaitMs { get; set; }
        public int ElapsedMs { get; set; }
    }
}
