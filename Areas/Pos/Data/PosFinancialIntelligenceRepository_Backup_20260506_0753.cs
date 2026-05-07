using MyERP.Areas.Pos.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;

namespace MyERP.Areas.Pos.Data
{
    public class PosFinancialIntelligenceRepository
    {
        private readonly string _connectionString;

        public PosFinancialIntelligenceRepository()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["KishnyCashConnection"];
            if (connectionString == null || string.IsNullOrWhiteSpace(connectionString.ConnectionString))
            {
                throw new ConfigurationErrorsException("Missing connection string: KishnyCashConnection");
            }

            _connectionString = connectionString.ConnectionString;
        }

        public PosFinancialIntelligenceResult GetAccountingHealthDashboard(PosFinancialIntelligenceFilter filter, int? lockedBranchId)
        {
            return Execute("dbo.usp_POS_FI_AccountingHealthDashboard", filter, lockedBranchId, true, true);
        }

        public PosFinancialIntelligenceResult GetEmployeeReceivableDiagnostics(PosFinancialIntelligenceFilter filter, int? lockedBranchId)
        {
            return Execute("dbo.usp_POS_FI_EmployeeReceivableDiagnostics", filter, lockedBranchId, true, false);
        }

        public PosFinancialIntelligenceResult GetCustodyDiagnostics(PosFinancialIntelligenceFilter filter, int? lockedBranchId)
        {
            return Execute("dbo.usp_POS_FI_CustodyDiagnostics", filter, lockedBranchId, false, true);
        }

        public PosFinancialIntelligenceResult GetAbnormalJournalDetection(PosFinancialIntelligenceFilter filter, int? lockedBranchId)
        {
            return Execute("dbo.usp_POS_FI_AbnormalJournalDetection", filter, lockedBranchId, true, true);
        }

        public PosFinancialIntelligenceResult GetRootCauseAnalyzer(PosFinancialIntelligenceFilter filter, int? lockedBranchId)
        {
            return Execute("dbo.usp_POS_FI_RootCauseAnalyzer", filter, lockedBranchId, false, false);
        }

        private PosFinancialIntelligenceResult Execute(string procedureName, PosFinancialIntelligenceFilter filter, int? lockedBranchId, bool includeReceivableParent, bool includeCustodyParent)
        {
            filter = filter ?? new PosFinancialIntelligenceFilter();
            var from = (filter.FromDate ?? DateTime.Today.AddDays(-30)).Date;
            var to = (filter.ToDate ?? DateTime.Today).Date;
            if (to < from)
            {
                var temp = from;
                from = to;
                to = temp;
            }

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(procedureName, connection))
            using (var adapter = new SqlDataAdapter(command))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.CommandTimeout = 90;
                command.Parameters.Add("@FromDate", SqlDbType.DateTime).Value = from;
                command.Parameters.Add("@ToDate", SqlDbType.DateTime).Value = to;
                command.Parameters.Add("@BranchId", SqlDbType.Int).Value = DbValue(lockedBranchId ?? filter.BranchId);

                if (procedureName.EndsWith("AccountingHealthDashboard", StringComparison.OrdinalIgnoreCase)
                    || procedureName.EndsWith("AbnormalJournalDetection", StringComparison.OrdinalIgnoreCase))
                {
                    command.Parameters.Add("@UserId", SqlDbType.Int).Value = DbValue(filter.UserId);
                }

                if (procedureName.EndsWith("EmployeeReceivableDiagnostics", StringComparison.OrdinalIgnoreCase)
                    || procedureName.EndsWith("CustodyDiagnostics", StringComparison.OrdinalIgnoreCase))
                {
                    command.Parameters.Add("@EmployeeId", SqlDbType.Int).Value = DbValue(filter.EmployeeId);
                    command.Parameters.Add("@AccountCode", SqlDbType.NVarChar, 50).Value = DbValue(filter.AccountCode);
                }

                if (procedureName.EndsWith("RootCauseAnalyzer", StringComparison.OrdinalIgnoreCase))
                {
                    command.Parameters.Add("@AccountCode", SqlDbType.NVarChar, 50).Value = DbValue(filter.AccountCode);
                }

                if (includeReceivableParent)
                {
                    command.Parameters.Add("@ReceivableParentSerial", SqlDbType.NVarChar, 50).Value = DbValue(filter.ReceivableParentSerial.HasValue ? filter.ReceivableParentSerial.Value.ToString(CultureInfo.InvariantCulture) : null);
                }

                if (includeCustodyParent)
                {
                    command.Parameters.Add("@CustodyParentSerial", SqlDbType.NVarChar, 50).Value = DbValue(filter.CustodyParentSerial.HasValue ? filter.CustodyParentSerial.Value.ToString(CultureInfo.InvariantCulture) : null);
                }

                var dataSet = new DataSet { Locale = CultureInfo.InvariantCulture };
                adapter.Fill(dataSet);
                return Convert(dataSet);
            }
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

        private static PosFinancialIntelligenceResult Convert(DataSet dataSet)
        {
            var result = new PosFinancialIntelligenceResult();
            if (dataSet == null)
            {
                return result;
            }

            for (var index = 0; index < dataSet.Tables.Count; index++)
            {
                var table = dataSet.Tables[index];
                var item = new PosFinancialIntelligenceTable
                {
                    Name = index == 0 ? "Table" : "Table" + index.ToString(CultureInfo.InvariantCulture)
                };

                foreach (DataColumn column in table.Columns)
                {
                    item.Columns.Add(column.ColumnName);
                }

                foreach (DataRow row in table.Rows)
                {
                    var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    foreach (DataColumn column in table.Columns)
                    {
                        values[column.ColumnName] = row[column] == DBNull.Value ? null : row[column];
                    }

                    item.Rows.Add(values);
                }

                result.Tables.Add(item);
            }

            return result;
        }
    }
}
