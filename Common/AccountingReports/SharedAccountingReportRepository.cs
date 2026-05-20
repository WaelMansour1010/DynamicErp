using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using MyERP.Models.Reports;

namespace MyERP.Common.AccountingReports
{
    public class SharedAccountingAccountTreeItem
    {
        public string AccountCode { get; set; }
        public string AccountSerial { get; set; }
        public string AccountName { get; set; }
        public string ParentAccountCode { get; set; }
        public bool IsLastAccount { get; set; }
        public bool HasChildren { get; set; }
    }

    public class SharedAccountingReportRepository
    {
        private readonly Func<SqlConnection> _createOpenConnection;

        public SharedAccountingReportRepository(Func<SqlConnection> createOpenConnection)
        {
            if (createOpenConnection == null) { throw new ArgumentNullException("createOpenConnection"); }
            _createOpenConnection = createOpenConnection;
        }

        public DataTable RunReport(HtmlReportFilterModel filter, int userId, bool canChangeDefaults)
        {
            filter = filter ?? new HtmlReportFilterModel();
            var table = new DataTable();
            using (var connection = _createOpenConnection())
            using (var command = new SqlCommand("dbo.usp_Shared_AccountingReports_Run", connection))
            using (var adapter = new SqlDataAdapter(command))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.CommandTimeout = 180;
                command.Parameters.Add("@reportKey", SqlDbType.NVarChar, 100).Value = (filter.ReportKey ?? string.Empty).Trim();
                command.Parameters.Add("@fromDate", SqlDbType.DateTime).Value = filter.FromDate.HasValue ? (object)filter.FromDate.Value.Date : DBNull.Value;
                command.Parameters.Add("@toDate", SqlDbType.DateTime).Value = filter.ToDate.HasValue ? (object)filter.ToDate.Value.Date : DBNull.Value;
                Add(command, "@branchId", SqlDbType.Int, filter.BranchId);
                AddString(command, "@accountFrom", 50, filter.AccountFrom);
                AddString(command, "@accountTo", 50, filter.AccountTo);
                AddString(command, "@accountCodes", -1, filter.AccountCodes);
                Add(command, "@costCenterId", SqlDbType.Int, filter.CostCenterId);
                Add(command, "@projectId", SqlDbType.Int, filter.ProjectId);
                Add(command, "@activityId", SqlDbType.Int, filter.ActivityId);
                Add(command, "@regionId", SqlDbType.Int, filter.RegionId);
                Add(command, "@noteType", SqlDbType.Int, filter.NoteType);
                Add(command, "@accountLevel", SqlDbType.Int, filter.AccountLevel);
                command.Parameters.Add("@hideZeroBalance", SqlDbType.Bit).Value = filter.HideZeroBalance.GetValueOrDefault(true);
                command.Parameters.Add("@detailed", SqlDbType.Bit).Value = filter.Detailed.GetValueOrDefault(false);
                command.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
                command.Parameters.Add("@canChangeDefaults", SqlDbType.Bit).Value = canChangeDefaults;
                adapter.Fill(table);
            }
            return table;
        }

        public IList<HtmlReportLookupItem> GetBranches()
        {
            var list = new List<HtmlReportLookupItem>();
            using (var connection = _createOpenConnection())
            using (var command = new SqlCommand("dbo.usp_Shared_AccountingReports_Branches", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new HtmlReportLookupItem
                        {
                            Id = ReadInt(reader, "Id"),
                            Name = ReadString(reader, "Name")
                        });
                    }
                }
            }
            return list;
        }

        public IList<SharedAccountingAccountTreeItem> GetAccountTree(string parentCode, string term)
        {
            var list = new List<SharedAccountingAccountTreeItem>();
            using (var connection = _createOpenConnection())
            using (var command = new SqlCommand("dbo.usp_Shared_AccountingReports_AccountTree", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                AddString(command, "@parentCode", 50, parentCode);
                AddString(command, "@term", 100, term);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new SharedAccountingAccountTreeItem
                        {
                            AccountCode = ReadString(reader, "AccountCode"),
                            AccountSerial = ReadString(reader, "AccountSerial"),
                            AccountName = ReadString(reader, "AccountName"),
                            ParentAccountCode = ReadString(reader, "ParentAccountCode"),
                            IsLastAccount = ReadBool(reader, "IsLastAccount"),
                            HasChildren = ReadBool(reader, "HasChildren")
                        });
                    }
                }
            }
            return list;
        }

        private static void Add(SqlCommand command, string name, SqlDbType type, int? value)
        {
            command.Parameters.Add(name, type).Value = value.HasValue ? (object)value.Value : DBNull.Value;
        }

        private static void AddString(SqlCommand command, string name, int size, string value)
        {
            command.Parameters.Add(name, SqlDbType.NVarChar, size).Value = string.IsNullOrWhiteSpace(value) ? (object)DBNull.Value : value.Trim();
        }

        private static string ReadString(IDataRecord reader, string name)
        {
            var value = reader[name];
            return value == DBNull.Value ? string.Empty : Convert.ToString(value);
        }

        private static int ReadInt(IDataRecord reader, string name)
        {
            var value = reader[name];
            return value == DBNull.Value ? 0 : Convert.ToInt32(value);
        }

        private static bool ReadBool(IDataRecord reader, string name)
        {
            var value = reader[name];
            return value != DBNull.Value && Convert.ToBoolean(value);
        }
    }
}
