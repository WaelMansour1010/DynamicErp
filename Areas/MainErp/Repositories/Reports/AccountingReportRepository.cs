using System;
using System.Data.SqlClient;
using MyERP.Areas.MainErp.Interfaces;
using MyERP.Areas.MainErp.ViewModels.Reports;

namespace MyERP.Areas.MainErp.Repositories.Reports
{
    public class AccountingReportRepository
    {
        private readonly IMainErpDbConnectionFactory _connectionFactory;

        public AccountingReportRepository(IMainErpDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public JournalEntriesReportViewModel GetJournalEntries(DateTime? fromDate, DateTime? toDate, int? branchId, string accountCode, int? noteType)
        {
            var model = new JournalEntriesReportViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                BranchId = branchId,
                AccountCode = accountCode,
                NoteType = noteType
            };

            try
            {
                using (var connection = _connectionFactory.CreateOpenConnection())
                using (var command = new SqlCommand(@"
SELECT
    COALESCE(n.NoteDate, v.RecordDate) AS NoteDate,
    CONVERT(nvarchar(50), n.NoteSerial) AS NoteSerial,
    n.NoteType,
    v.Account_Code,
    a.Account_Name,
    CASE WHEN ISNULL(v.Credit_Or_Debit, 0) = 0 THEN ISNULL(v.Value, 0) ELSE 0 END AS Debit,
    CASE WHEN ISNULL(v.Credit_Or_Debit, 0) = 1 THEN ISNULL(v.Value, 0) ELSE 0 END AS Credit,
    COALESCE(v.Double_Entry_Vouchers_Description, v.des, n.Remark) AS Description,
    b.branch_name AS BranchName,
    u.UserName
FROM DOUBLE_ENTREY_VOUCHERS v
LEFT JOIN Notes n ON v.Notes_ID = n.NoteID
LEFT JOIN ACCOUNTS a ON v.Account_Code = a.Account_Code
LEFT JOIN TblBranchesData b ON v.branch_id = b.branch_id
LEFT JOIN TblUsers u ON v.UserID = u.UserID
WHERE (@FromDate IS NULL OR COALESCE(n.NoteDate, v.RecordDate) >= @FromDate)
  AND (@ToDate IS NULL OR COALESCE(n.NoteDate, v.RecordDate) < DATEADD(day, 1, @ToDate))
  AND (@BranchId IS NULL OR v.branch_id = @BranchId)
  AND (@AccountCode IS NULL OR v.Account_Code = @AccountCode)
  AND (@NoteType IS NULL OR n.NoteType = @NoteType)
ORDER BY COALESCE(n.NoteDate, v.RecordDate), n.NoteSerial, v.Double_Entry_Vouchers_ID, v.DEV_ID_Line_No;", connection))
                {
                    AddAccountingParameters(command, fromDate, toDate, branchId, accountCode, noteType);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var row = new JournalEntriesReportRowViewModel
                            {
                                NoteDate = reader["NoteDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["NoteDate"]),
                                NoteSerial = Convert.ToString(reader["NoteSerial"]),
                                NoteType = reader["NoteType"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["NoteType"]),
                                AccountCode = Convert.ToString(reader["Account_Code"]),
                                AccountName = Convert.ToString(reader["Account_Name"]),
                                Debit = reader["Debit"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["Debit"]),
                                Credit = reader["Credit"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["Credit"]),
                                Description = Convert.ToString(reader["Description"]),
                                BranchName = Convert.ToString(reader["BranchName"]),
                                UserName = Convert.ToString(reader["UserName"])
                            };

                            model.TotalDebit += row.Debit;
                            model.TotalCredit += row.Credit;
                            model.Rows.Add(row);
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                model.Warning = "تعذر تشغيل تقرير القيود اليومية على قاعدة البيانات الحالية: " + ex.Message;
            }

            return model;
        }

        public AccountMovementReportViewModel GetAccountMovement(DateTime? fromDate, DateTime? toDate, string accountCode, int? branchId)
        {
            var model = new AccountMovementReportViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                AccountCode = accountCode,
                BranchId = branchId
            };

            if (string.IsNullOrWhiteSpace(accountCode))
            {
                model.Warning = "الحساب مطلوب لتشغيل تقرير حركة الحساب.";
                return model;
            }

            try
            {
                using (var connection = _connectionFactory.CreateOpenConnection())
                using (var command = new SqlCommand(@"
SELECT
    COALESCE(n.NoteDate, v.RecordDate) AS NoteDate,
    CONVERT(nvarchar(50), n.NoteSerial) AS NoteSerial,
    COALESCE(v.Double_Entry_Vouchers_Description, v.des, n.Remark) AS Description,
    a.Account_Name,
    CASE WHEN ISNULL(v.Credit_Or_Debit, 0) = 0 THEN ISNULL(v.Value, 0) ELSE 0 END AS Debit,
    CASE WHEN ISNULL(v.Credit_Or_Debit, 0) = 1 THEN ISNULL(v.Value, 0) ELSE 0 END AS Credit
FROM DOUBLE_ENTREY_VOUCHERS v
LEFT JOIN Notes n ON v.Notes_ID = n.NoteID
LEFT JOIN ACCOUNTS a ON v.Account_Code = a.Account_Code
WHERE v.Account_Code = @AccountCode
  AND (@FromDate IS NULL OR COALESCE(n.NoteDate, v.RecordDate) >= @FromDate)
  AND (@ToDate IS NULL OR COALESCE(n.NoteDate, v.RecordDate) < DATEADD(day, 1, @ToDate))
  AND (@BranchId IS NULL OR v.branch_id = @BranchId)
ORDER BY COALESCE(n.NoteDate, v.RecordDate), n.NoteSerial, v.Double_Entry_Vouchers_ID, v.DEV_ID_Line_No;", connection))
                {
                    command.Parameters.AddWithValue("@AccountCode", accountCode.Trim());
                    command.Parameters.AddWithValue("@FromDate", (object)fromDate ?? DBNull.Value);
                    command.Parameters.AddWithValue("@ToDate", (object)toDate ?? DBNull.Value);
                    command.Parameters.AddWithValue("@BranchId", (object)branchId ?? DBNull.Value);

                    var running = 0m;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var debit = reader["Debit"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["Debit"]);
                            var credit = reader["Credit"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["Credit"]);
                            running += debit - credit;
                            model.AccountName = string.IsNullOrWhiteSpace(model.AccountName) ? Convert.ToString(reader["Account_Name"]) : model.AccountName;
                            model.TotalDebit += debit;
                            model.TotalCredit += credit;
                            model.Rows.Add(new AccountMovementReportRowViewModel
                            {
                                NoteDate = reader["NoteDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["NoteDate"]),
                                NoteSerial = Convert.ToString(reader["NoteSerial"]),
                                Description = Convert.ToString(reader["Description"]),
                                Debit = debit,
                                Credit = credit,
                                RunningMovement = running
                            });
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(model.Warning))
                {
                    model.Warning = "الرصيد الافتتاحي غير محتسب في هذه المرحلة؛ الرصيد المتحرك المعروض هو صافي حركة الفترة فقط.";
                }
            }
            catch (SqlException ex)
            {
                model.Warning = "تعذر تشغيل تقرير حركة الحساب على قاعدة البيانات الحالية: " + ex.Message;
            }

            return model;
        }

        private static void AddAccountingParameters(SqlCommand command, DateTime? fromDate, DateTime? toDate, int? branchId, string accountCode, int? noteType)
        {
            command.Parameters.AddWithValue("@FromDate", (object)fromDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@ToDate", (object)toDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@BranchId", (object)branchId ?? DBNull.Value);
            command.Parameters.AddWithValue("@AccountCode", string.IsNullOrWhiteSpace(accountCode) ? (object)DBNull.Value : accountCode.Trim());
            command.Parameters.AddWithValue("@NoteType", (object)noteType ?? DBNull.Value);
        }
    }
}
