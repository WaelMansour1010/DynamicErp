using MyERP.Areas.Pos.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;

namespace MyERP.Areas.Pos.Data
{
    public class PosSalesPerformanceRepository
    {
        private readonly string _connectionString;

        public PosSalesPerformanceRepository()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["KishnyCashConnection"];
            if (connectionString == null || string.IsNullOrWhiteSpace(connectionString.ConnectionString))
            {
                throw new ConfigurationErrorsException("Missing connection string: KishnyCashConnection");
            }

            _connectionString = connectionString.ConnectionString;
        }

        public IList<SalesRepresentativePerformanceRow> GetPerformance(SalesRepresentativesPerformanceFilter filter, int? lockedBranchId)
        {
            filter = Normalize(filter);

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("dbo.usp_POS_SalesRepresentativesPerformanceDashboard", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.CommandTimeout = 90;
                command.Parameters.Add("@FromDate", SqlDbType.DateTime).Value = filter.FromDate.Value.Date;
                command.Parameters.Add("@ToDate", SqlDbType.DateTime).Value = filter.ToDate.Value.Date;
                command.Parameters.Add("@BranchId", SqlDbType.Int).Value = DbValue(lockedBranchId ?? filter.BranchId);
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = DbValue(filter.UserId);
                command.Parameters.Add("@ServiceType", SqlDbType.NVarChar, 30).Value = DbValue(filter.ServiceType);
                command.Parameters.Add("@MonthlyRechargeTarget", SqlDbType.Decimal).Value = DbValue(filter.MonthlyRechargeTarget);
                command.Parameters["@MonthlyRechargeTarget"].Precision = 18;
                command.Parameters["@MonthlyRechargeTarget"].Scale = 2;
                command.Parameters.Add("@MonthlyCardTarget", SqlDbType.Int).Value = DbValue(filter.MonthlyCardTarget);
                command.Parameters.Add("@WorkingDaysInMonth", SqlDbType.Int).Value = DbValue(filter.WorkingDaysInMonth);

                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    var rows = new List<SalesRepresentativePerformanceRow>();
                    while (reader.Read())
                    {
                        rows.Add(new SalesRepresentativePerformanceRow
                        {
                            UserId = ReadInt(reader, "UserId").GetValueOrDefault(),
                            EmpId = ReadInt(reader, "EmpId"),
                            RepresentativeName = ReadString(reader, "RepresentativeName"),
                            BranchId = ReadInt(reader, "BranchId"),
                            BranchName = ReadString(reader, "BranchName"),
                            FromDate = ReadDate(reader, "FromDate").GetValueOrDefault(filter.FromDate.Value.Date),
                            ToDate = ReadDate(reader, "ToDate").GetValueOrDefault(filter.ToDate.Value.Date),
                            CashInTotal = ReadDecimal(reader, "CashInTotal"),
                            CashInCount = ReadInt(reader, "CashInCount").GetValueOrDefault(),
                            CashOutWithoutFees = ReadDecimal(reader, "CashOutWithoutFees"),
                            CashOutWithFees = ReadDecimal(reader, "CashOutWithFees"),
                            CashOutCount = ReadInt(reader, "CashOutCount").GetValueOrDefault(),
                            ViolationsTotal = ReadDecimal(reader, "ViolationsTotal"),
                            ViolationsFees = ReadDecimal(reader, "ViolationsFees"),
                            ViolationsCount = ReadInt(reader, "ViolationsCount").GetValueOrDefault(),
                            FeesTotal = ReadDecimal(reader, "FeesTotal"),
                            CardsTotal = ReadDecimal(reader, "CardsTotal"),
                            CardsCount = ReadInt(reader, "CardsCount").GetValueOrDefault(),
                            ExpectedCashSupply = ReadDecimal(reader, "ExpectedCashSupply"),
                            RechargeTarget = ReadDecimal(reader, "RechargeTarget"),
                            CardTarget = ReadDecimal(reader, "CardTarget"),
                            AchievedRecharge = ReadDecimal(reader, "AchievedRecharge"),
                            AchievedCards = ReadDecimal(reader, "AchievedCards"),
                            RechargeAchievementPercent = ReadDecimal(reader, "RechargeAchievementPercent"),
                            CardAchievementPercent = ReadDecimal(reader, "CardAchievementPercent"),
                            OverallAchievementPercent = ReadDecimal(reader, "OverallAchievementPercent"),
                            RequiredDailyRecharge = ReadDecimal(reader, "RequiredDailyRecharge"),
                            RequiredDailyCards = ReadDecimal(reader, "RequiredDailyCards"),
                            ProjectedRecharge = ReadDecimal(reader, "ProjectedRecharge"),
                            ProjectedCards = ReadDecimal(reader, "ProjectedCards"),
                            PerformanceStatus = ReadString(reader, "PerformanceStatus"),
                            PerformanceClass = ReadString(reader, "PerformanceClass")
                        });
                    }

                    return rows;
                }
            }
        }

        public IList<PosUserLookupDto> GetSalesRepresentatives(int? lockedBranchId)
        {
            const string sql = @"
SELECT
    u.UserID,
    u.BranchId,
    DisplayName = COALESCE(NULLIF(e.Emp_Name, N''), NULLIF(u.UserName, N''), N'User ' + CONVERT(NVARCHAR(20), u.UserID))
FROM dbo.TblUsers u
LEFT JOIN dbo.TblEmployee e ON e.Emp_ID = u.Empid
WHERE ISNULL(u.isDeactivated, 0) = 0
  AND (@BranchId IS NULL OR u.BranchId = @BranchId)
  AND (
        LTRIM(RTRIM(ISNULL(u.UserCategory, N''))) IN (N'تلر', N'Teller')
        OR EXISTS
        (
            SELECT 1
            FROM dbo.POS_UserPermissions p
            WHERE p.UserID = u.UserID
              AND p.PermissionKey = N'CanTeller'
              AND ISNULL(p.IsAllowed, 0) = 1
        )
        OR EXISTS (SELECT 1 FROM dbo.TBLSalesRepData sr WHERE sr.EmpID = u.Empid)
        OR EXISTS (SELECT 1 FROM dbo.TBLSalesRepData2 sr WHERE sr.EmpID = u.Empid)
        OR EXISTS (SELECT 1 FROM dbo.TBLSalesRepData3 sr WHERE sr.EmpID = u.Empid)
      )
ORDER BY DisplayName;";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@BranchId", SqlDbType.Int).Value = DbValue(lockedBranchId);
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    var users = new List<PosUserLookupDto>();
                    while (reader.Read())
                    {
                        users.Add(new PosUserLookupDto
                        {
                            UserId = ReadInt(reader, "UserID").GetValueOrDefault(),
                            BranchId = ReadInt(reader, "BranchId"),
                            DisplayName = ReadString(reader, "DisplayName")
                        });
                    }

                    return users;
                }
            }
        }

        public IList<PosSalesTargetRowDto> GetSalesTargets(DateTime fromDate, DateTime toDate, int? branchId, int? userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("dbo.usp_POS_SalesTargets_List", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.CommandTimeout = 60;
                command.Parameters.Add("@FromDate", SqlDbType.DateTime).Value = fromDate.Date;
                command.Parameters.Add("@ToDate", SqlDbType.DateTime).Value = toDate.Date;
                command.Parameters.Add("@BranchId", SqlDbType.Int).Value = DbValue(branchId);
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = DbValue(userId);

                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    var rows = new List<PosSalesTargetRowDto>();
                    while (reader.Read())
                    {
                        rows.Add(new PosSalesTargetRowDto
                        {
                            TargetId = ReadInt(reader, "TargetId").GetValueOrDefault(),
                            UserId = ReadInt(reader, "UserID"),
                            RepresentativeName = ReadString(reader, "RepresentativeName"),
                            BranchId = ReadInt(reader, "BranchId"),
                            BranchName = ReadString(reader, "BranchName"),
                            FromDate = ReadDate(reader, "FromDate").GetValueOrDefault(),
                            ToDate = ReadDate(reader, "ToDate").GetValueOrDefault(),
                            MonthlyRechargeTarget = ReadDecimal(reader, "MonthlyRechargeTarget"),
                            MonthlyCardTarget = ReadInt(reader, "MonthlyCardTarget").GetValueOrDefault(),
                            WorkingDaysInMonth = ReadInt(reader, "WorkingDaysInMonth").GetValueOrDefault(),
                            DailyRechargeTarget = ReadDecimal(reader, "DailyRechargeTarget"),
                            DailyCardTarget = ReadDecimal(reader, "DailyCardTarget"),
                            CreatedAt = ReadDate(reader, "CreatedAt").GetValueOrDefault(),
                            CreatedByName = ReadString(reader, "CreatedByName")
                        });
                    }

                    return rows;
                }
            }
        }

        public void SaveSalesTargets(PosSalesTargetSaveRequest request, int? lockedBranchId, int createdByUserId)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            var userIds = new List<string>();
            if (request.UserIds != null)
            {
                foreach (var userId in request.UserIds)
                {
                    if (userId > 0)
                    {
                        userIds.Add(userId.ToString(CultureInfo.InvariantCulture));
                    }
                }
            }

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("dbo.usp_POS_SalesTargets_Save", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.CommandTimeout = 60;
                command.Parameters.Add("@ApplyMode", SqlDbType.NVarChar, 20).Value = DbValue(request.ApplyMode);
                command.Parameters.Add("@UserIds", SqlDbType.NVarChar, -1).Value = string.Join(",", userIds.ToArray());
                command.Parameters.Add("@BranchId", SqlDbType.Int).Value = DbValue(lockedBranchId ?? request.BranchId);
                command.Parameters.Add("@FromDate", SqlDbType.DateTime).Value = request.FromDate.GetValueOrDefault(DateTime.Today).Date;
                command.Parameters.Add("@ToDate", SqlDbType.DateTime).Value = request.ToDate.GetValueOrDefault(DateTime.Today).Date;
                command.Parameters.Add("@MonthlyRechargeTarget", SqlDbType.Decimal).Value = request.MonthlyRechargeTarget;
                command.Parameters["@MonthlyRechargeTarget"].Precision = 18;
                command.Parameters["@MonthlyRechargeTarget"].Scale = 2;
                command.Parameters.Add("@MonthlyCardTarget", SqlDbType.Int).Value = request.MonthlyCardTarget;
                command.Parameters.Add("@WorkingDaysInMonth", SqlDbType.Int).Value = request.WorkingDaysInMonth;
                command.Parameters.Add("@CreatedByUserID", SqlDbType.Int).Value = createdByUserId;

                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        public void DeactivateSalesTarget(int targetId, int updatedByUserId)
        {
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("dbo.usp_POS_SalesTargets_Deactivate", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.CommandTimeout = 60;
                command.Parameters.Add("@TargetId", SqlDbType.Int).Value = targetId;
                command.Parameters.Add("@UpdatedByUserID", SqlDbType.Int).Value = updatedByUserId;

                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        public SalesRepresentativesPerformanceSummary BuildSummary(IList<SalesRepresentativePerformanceRow> rows)
        {
            var summary = new SalesRepresentativesPerformanceSummary();
            if (rows == null)
            {
                return summary;
            }

            decimal weightedPercentTotal = 0;
            foreach (var row in rows)
            {
                summary.RepresentativeCount++;
                summary.CashInTotal += row.CashInTotal;
                summary.CashOutWithFeesTotal += row.CashOutWithFees;
                summary.FeesTotal += row.FeesTotal;
                summary.CardsTotal += row.CardsTotal;
                summary.CardsCount += row.CardsCount;
                summary.ExpectedCashSupplyTotal += row.ExpectedCashSupply;
                weightedPercentTotal += row.OverallAchievementPercent;
            }

            summary.OverallAchievementPercent = summary.RepresentativeCount == 0
                ? 0
                : weightedPercentTotal / summary.RepresentativeCount;
            return summary;
        }

        private static SalesRepresentativesPerformanceFilter Normalize(SalesRepresentativesPerformanceFilter filter)
        {
            filter = filter ?? new SalesRepresentativesPerformanceFilter();
            filter.FromDate = (filter.FromDate ?? DateTime.Today).Date;
            filter.ToDate = (filter.ToDate ?? DateTime.Today).Date;
            if (filter.ToDate.Value < filter.FromDate.Value)
            {
                var from = filter.FromDate;
                filter.FromDate = filter.ToDate;
                filter.ToDate = from;
            }

            filter.ServiceType = (filter.ServiceType ?? string.Empty).Trim();
            return filter;
        }

        private static object DbValue(object value)
        {
            if (value == null)
            {
                return DBNull.Value;
            }

            var text = value as string;
            if (text != null && string.IsNullOrWhiteSpace(text))
            {
                return DBNull.Value;
            }

            return value;
        }

        private static string ReadString(IDataRecord reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? string.Empty : Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        }

        private static int? ReadInt(IDataRecord reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? (int?)null : Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        }

        private static DateTime? ReadDate(IDataRecord reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? (DateTime?)null : Convert.ToDateTime(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        }

        private static decimal ReadDecimal(IDataRecord reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? 0 : Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        }
    }
}
