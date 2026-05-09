using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using MyERP.Areas.MainErp.Interfaces;
using MyERP.Areas.MainErp.ViewModels;
using MyERP.Areas.MainErp.ViewModels.ProjectExtracts;

namespace MyERP.Areas.MainErp.Repositories.ProjectExtracts
{
    public class ProjectExtractReadRepository
    {
        private readonly IMainErpDbConnectionFactory _connectionFactory;

        public ProjectExtractReadRepository(IMainErpDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public PagedReadResult<ProjectExtractListItemViewModel> Search(string searchText, int? projectId, int? branchId, int page, int pageSize)
        {
            var result = new PagedReadResult<ProjectExtractListItemViewModel>();
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : pageSize;

            try
            {
                using (var connection = _connectionFactory.CreateOpenConnection())
                using (var command = new SqlCommand(@"
WITH ExtractRows AS (
    SELECT
        ROW_NUMBER() OVER (ORDER BY pb.id DESC) AS RowNo,
        COUNT(1) OVER() AS TotalCount,
        pb.id,
        pb.bill_date,
        pb.NoteSerial,
        pb.ManualNO,
        pb.project_name,
        p.Fullcode AS ProjectFullCode,
        cust.CusName AS CustomerName,
        pb.total,
        pb.Results,
        pb.FATValue,
        pb.NetValue,
        pb.Branch_NO,
        pb.note_id
    FROM project_billl pb
    LEFT JOIN projects p ON pb.project_no = CONVERT(nvarchar(50), p.id)
    LEFT JOIN Notes n ON pb.note_id = n.NoteID
    LEFT JOIN TblCustemers cust ON n.CusID = cust.CusID
    WHERE (@SearchText IS NULL OR pb.NoteSerial LIKE @SearchLike OR pb.ManualNO LIKE @SearchLike OR pb.project_name LIKE @SearchLike)
      AND (@ProjectId IS NULL OR pb.project_no = CONVERT(nvarchar(50), @ProjectId))
      AND (@BranchId IS NULL OR pb.Branch_NO = @BranchId)
)
SELECT * FROM ExtractRows WHERE RowNo BETWEEN @StartRow AND @EndRow ORDER BY RowNo;", connection))
                {
                    command.Parameters.AddWithValue("@SearchText", string.IsNullOrWhiteSpace(searchText) ? (object)DBNull.Value : searchText);
                    command.Parameters.AddWithValue("@SearchLike", string.IsNullOrWhiteSpace(searchText) ? (object)DBNull.Value : "%" + searchText + "%");
                    command.Parameters.AddWithValue("@ProjectId", (object)projectId ?? DBNull.Value);
                    command.Parameters.AddWithValue("@BranchId", (object)branchId ?? DBNull.Value);
                    command.Parameters.AddWithValue("@StartRow", ((page - 1) * pageSize) + 1);
                    command.Parameters.AddWithValue("@EndRow", page * pageSize);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (result.TotalCount == 0 && reader["TotalCount"] != DBNull.Value)
                            {
                                result.TotalCount = Convert.ToInt32(reader["TotalCount"]);
                            }

                            result.Items.Add(new ProjectExtractListItemViewModel
                            {
                                Id = ReadInt(reader, "id").GetValueOrDefault(),
                                BillDate = ReadDate(reader, "bill_date"),
                                NoteSerial = ReadString(reader, "NoteSerial"),
                                ManualNo = ReadString(reader, "ManualNO"),
                                ProjectName = ReadString(reader, "project_name"),
                                ProjectFullCode = ReadString(reader, "ProjectFullCode"),
                                CustomerName = ReadString(reader, "CustomerName"),
                                Total = ReadDecimal(reader, "total"),
                                Results = ReadDouble(reader, "Results"),
                                VatValue = ReadDouble(reader, "FATValue"),
                                NetValue = ReadDouble(reader, "NetValue"),
                                BranchNo = ReadInt(reader, "Branch_NO"),
                                NoteId = ReadInt(reader, "note_id")
                            });
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                Trace.TraceWarning("MainErp Project Extract search schema warning: " + ex);
                result.Warning = "تعذر تحميل قائمة مستخلصات المشاريع من قاعدة البيانات الحالية. تأكد أن قاعدة التشغيل تحتوي على جداول وحقول مستخلصات المشاريع الخاصة بالنظام الرئيسي.";
            }

            return result;
        }

        public ProjectExtractDetailsViewModel GetDetails(int id)
        {
            var model = new ProjectExtractDetailsViewModel { Id = id };

            try
            {
                using (var connection = _connectionFactory.CreateOpenConnection())
                {
                    LoadHeader(connection, model, id);
                    if (!string.IsNullOrWhiteSpace(model.Warning))
                    {
                        return model;
                    }

                    LoadDetailLines(connection, model, id);
                    LoadAdvancePayments(connection, model, id);
                    LoadVoucherLines(connection, model, id, model.NoteId);
                }
            }
            catch (SqlException ex)
            {
                Trace.TraceWarning("MainErp Project Extract details schema warning: " + ex);
                model.Warning = "تعذر تحميل تفاصيل المستخلص من قاعدة البيانات الحالية. قد تكون قاعدة التشغيل لا تحتوي على نفس حقول مستخلصات المشاريع الموجودة في قاعدة Eng.";
            }

            return model;
        }

        private static void LoadHeader(SqlConnection connection, ProjectExtractDetailsViewModel model, int id)
        {
            using (var command = new SqlCommand(@"
SELECT TOP 1
    pb.id, pb.bill_date, pb.NoteSerial, pb.ManualNO, pb.project_name,
    p.Fullcode AS ProjectFullCode, cust.CusName AS CustomerName,
    pb.total, pb.Results, pb.FATValue, pb.NetValue, pb.Branch_NO, pb.note_id,
    pb.Remarks, pb.revenue_account, pb.End_user_account, pb.Sub_user_account, pb.AccountUnderImp,
    pb.advancedPayment, pb.PerformanceBond, pb.PreVAT, pb.AccountCodeVat,
    rev.Account_Serial AS RevenueAccountSerial, COALESCE(rev.Account_Name, rev.Account_NameEng) AS RevenueAccountName,
    endacc.Account_Serial AS EndUserAccountSerial, COALESCE(endacc.Account_Name, endacc.Account_NameEng) AS EndUserAccountName,
    subacc.Account_Serial AS SubUserAccountSerial, COALESCE(subacc.Account_Name, subacc.Account_NameEng) AS SubUserAccountName,
    underacc.Account_Serial AS UnderImpAccountSerial, COALESCE(underacc.Account_Name, underacc.Account_NameEng) AS UnderImpAccountName,
    vatacc.Account_Serial AS VatAccountSerial, COALESCE(vatacc.Account_Name, vatacc.Account_NameEng) AS VatAccountName
FROM project_billl pb
LEFT JOIN projects p ON pb.project_no = CONVERT(nvarchar(50), p.id)
LEFT JOIN Notes n ON pb.note_id = n.NoteID
LEFT JOIN TblCustemers cust ON n.CusID = cust.CusID
LEFT JOIN ACCOUNTS rev ON pb.revenue_account = rev.Account_Code
LEFT JOIN ACCOUNTS endacc ON pb.End_user_account = endacc.Account_Code
LEFT JOIN ACCOUNTS subacc ON pb.Sub_user_account = subacc.Account_Code
LEFT JOIN ACCOUNTS underacc ON pb.AccountUnderImp = underacc.Account_Code
LEFT JOIN ACCOUNTS vatacc ON pb.AccountCodeVat = vatacc.Account_Code
WHERE pb.id = @Id;", connection))
            {
                command.Parameters.AddWithValue("@Id", id);
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        model.Warning = "Project extract record was not found.";
                        return;
                    }

                    model.Id = ReadInt(reader, "id").GetValueOrDefault();
                    model.BillDate = ReadDate(reader, "bill_date");
                    model.NoteSerial = ReadString(reader, "NoteSerial");
                    model.ManualNo = ReadString(reader, "ManualNO");
                    model.ProjectName = ReadString(reader, "project_name");
                    model.ProjectFullCode = ReadString(reader, "ProjectFullCode");
                    model.CustomerName = ReadString(reader, "CustomerName");
                    model.Total = ReadDecimal(reader, "total");
                    model.Results = ReadDouble(reader, "Results");
                    model.VatValue = ReadDouble(reader, "FATValue");
                    model.NetValue = ReadDouble(reader, "NetValue");
                    model.BranchNo = ReadInt(reader, "Branch_NO");
                    model.NoteId = ReadInt(reader, "note_id");
                    model.Remarks = ReadString(reader, "Remarks");
                    model.RevenueAccount = FormatAccount(reader, "RevenueAccountSerial", "RevenueAccountName", "revenue_account");
                    model.EndUserAccount = FormatAccount(reader, "EndUserAccountSerial", "EndUserAccountName", "End_user_account");
                    model.SubUserAccount = FormatAccount(reader, "SubUserAccountSerial", "SubUserAccountName", "Sub_user_account");
                    model.AccountUnderImplementation = FormatAccount(reader, "UnderImpAccountSerial", "UnderImpAccountName", "AccountUnderImp");
                    model.VatAccountCode = FormatAccount(reader, "VatAccountSerial", "VatAccountName", "AccountCodeVat");
                    model.AdvancedPayment = ReadDouble(reader, "advancedPayment");
                    model.PerformanceBond = ReadDouble(reader, "PerformanceBond");
                    model.PreVat = ReadDouble(reader, "PreVAT");
                }
            }
        }

        private static void LoadDetailLines(SqlConnection connection, ProjectExtractDetailsViewModel model, int id)
        {
            using (var command = new SqlCommand(@"
SELECT
    d.id,
    d.item,
    d.FullCode,
    d.item_unit,
    d.Quantity,
    d.Price,
    d.cost,
    d.Pre_Quantity,
    d.Pre_Value,
    d.Curr_Quantity,
    d.Curr_value,
    d.tot_quantity,
    d.tot_value,
    d.curr_Percent,
    d.tot_percent,
    d.LineDiscount,
    d.linenetaftermainDiscountBeforevat,
    d.LineVat,
    d.linenetaftermainDiscountWithvat,
    d.PerforVLineDiscount,
    d.LineFinal,
    d.AccountCode,
    a.Account_Serial,
    COALESCE(a.Account_Name, a.Account_NameEng) AS AccountName
FROM project_bill_details d
LEFT JOIN ACCOUNTS a ON d.AccountCode = a.Account_Code
WHERE d.bill_id = @Id
ORDER BY d.line_no, d.id;", connection))
            {
                command.Parameters.AddWithValue("@Id", id);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        model.DetailLines.Add(new ProjectExtractDetailLineViewModel
                        {
                            Id = ReadInt(reader, "id").GetValueOrDefault(),
                            Item = ReadString(reader, "item"),
                            FullCode = ReadString(reader, "FullCode"),
                            Unit = ReadString(reader, "item_unit"),
                            Quantity = ReadDecimal(reader, "Quantity"),
                            Price = ReadDecimal(reader, "Price"),
                            Cost = ReadDecimal(reader, "cost"),
                            PreQuantity = ReadDecimal(reader, "Pre_Quantity"),
                            PreValue = ReadDecimal(reader, "Pre_Value"),
                            CurrQuantity = ReadDecimal(reader, "Curr_Quantity"),
                            CurrValue = ReadDecimal(reader, "Curr_value"),
                            TotalQuantity = ReadDecimal(reader, "tot_quantity"),
                            TotalValue = ReadDecimal(reader, "tot_value"),
                            CurrPercent = ReadDecimal(reader, "curr_Percent"),
                            TotalPercent = ReadDecimal(reader, "tot_percent"),
                            LineDiscount = ReadDecimal(reader, "LineDiscount"),
                            NetBeforeVat = ReadDecimal(reader, "linenetaftermainDiscountBeforevat"),
                            LineVat = ReadDecimal(reader, "LineVat"),
                            NetWithVat = ReadDecimal(reader, "linenetaftermainDiscountWithvat"),
                            PerformanceLineDiscount = ReadDecimal(reader, "PerforVLineDiscount"),
                            LineFinal = ReadDecimal(reader, "LineFinal"),
                            AccountCodeInternal = ReadString(reader, "AccountCode"),
                            AccountDisplay = FormatAccount(reader, "Account_Serial", "AccountName", "AccountCode")
                        });
                    }
                }
            }
        }

        private static void LoadAdvancePayments(SqlConnection connection, ProjectExtractDetailsViewModel model, int id)
        {
            using (var command = new SqlCommand(@"
SELECT
    'TblPayPrePayed' AS SourceTable,
    p.ID,
    p.NoteID,
    CAST(NULL AS int) AS Transaction_ID,
    CONVERT(nvarchar(50), p.NoteSerial1) AS NoteSerial,
    p.NoteDate,
    p.Note_Value,
    p.PayedValue,
    p.TransPayedValue,
    p.RemainingValue,
    p.NetValue,
    p.VAT,
    p.TypeTrans,
    p.NCashingType,
    b.branch_name AS BranchName,
    p.Account_code,
    a.Account_Serial,
    COALESCE(a.Account_Name, a.Account_NameEng) AS AccountName
FROM TblPayPrePayed p
LEFT JOIN TblBranchesData b ON p.branch_no = b.branch_id
LEFT JOIN ACCOUNTS a ON p.Account_code = a.Account_Code
WHERE p.NoteID1 = @Id
UNION ALL
SELECT
    'TblProjePayPrePayed' AS SourceTable,
    pp.ID,
    pp.NoteID,
    pp.Transaction_ID,
    CONVERT(nvarchar(50), n.NoteSerial) AS NoteSerial,
    n.NoteDate,
    pp.Note_Value,
    pp.PayedValue,
    CAST(NULL AS float) AS TransPayedValue,
    CAST(NULL AS float) AS RemainingValue,
    CAST(NULL AS float) AS NetValue,
    CAST(NULL AS float) AS VAT,
    pp.TypeTrans,
    pp.NCashingType,
    CAST(NULL AS nvarchar(250)) AS BranchName,
    CAST(NULL AS nvarchar(250)) AS Account_code,
    CAST(NULL AS nvarchar(250)) AS Account_Serial,
    CAST(NULL AS nvarchar(250)) AS AccountName
FROM TblProjePayPrePayed pp
LEFT JOIN Notes n ON pp.NoteID = n.NoteID
WHERE pp.NoteID = @Id
ORDER BY SourceTable, ID;", connection))
            {
                command.Parameters.AddWithValue("@Id", id);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        model.AdvancePayments.Add(new ProjectExtractAdvancePaymentViewModel
                        {
                            SourceTable = ReadString(reader, "SourceTable"),
                            Id = ReadInt(reader, "ID").GetValueOrDefault(),
                            NoteId = ReadInt(reader, "NoteID"),
                            TransactionId = ReadInt(reader, "Transaction_ID"),
                            NoteSerial = ReadString(reader, "NoteSerial"),
                            NoteDate = ReadDate(reader, "NoteDate"),
                            NoteValue = ReadDecimal(reader, "Note_Value"),
                            PayedValue = ReadDecimal(reader, "PayedValue"),
                            TransPayedValue = ReadDecimal(reader, "TransPayedValue"),
                            RemainingValue = ReadDecimal(reader, "RemainingValue"),
                            NetValue = ReadDecimal(reader, "NetValue"),
                            Vat = ReadDecimal(reader, "VAT"),
                            TypeTrans = ReadInt(reader, "TypeTrans"),
                            NCashingType = ReadInt(reader, "NCashingType"),
                            BranchName = ReadString(reader, "BranchName"),
                            AccountCodeInternal = ReadString(reader, "Account_code"),
                            AccountDisplay = FormatAccount(reader, "Account_Serial", "AccountName", "Account_code")
                        });
                    }
                }
            }
        }

        private static void LoadVoucherLines(SqlConnection connection, ProjectExtractDetailsViewModel model, int id, int? noteId)
        {
            using (var command = new SqlCommand(@"
SELECT
    v.Double_Entry_Vouchers_ID,
    v.DEV_ID_Line_No,
    v.Notes_ID,
    n.NoteSerial,
    v.RecordDate,
    v.Account_Code,
    a.Account_Serial,
    COALESCE(a.Account_Name, a.Account_NameEng) AS AccountName,
    CASE WHEN v.Credit_Or_Debit = 0 THEN ISNULL(v.Value, 0) ELSE ISNULL(v.depet_value, 0) END AS Debit,
    CASE WHEN v.Credit_Or_Debit = 1 THEN ISNULL(v.Value, 0) ELSE ISNULL(v.credit_value, 0) END AS Credit,
    COALESCE(v.Double_Entry_Vouchers_Description, v.des) AS Description,
    b.branch_name AS BranchName,
    pr.Project_name AS ProjectName
FROM DOUBLE_ENTREY_VOUCHERS v
LEFT JOIN Notes n ON v.Notes_ID = n.NoteID
LEFT JOIN ACCOUNTS a ON v.Account_Code = a.Account_Code
LEFT JOIN TblBranchesData b ON v.branch_id = b.branch_id
LEFT JOIN projects pr ON v.project_id = pr.id
WHERE (@NoteId IS NOT NULL AND v.Notes_ID = @NoteId)
   OR v.project_bill_no = @Id
   OR v.bill_id = @Id
ORDER BY v.Double_Entry_Vouchers_ID, v.DEV_ID_Line_No;", connection))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                command.Parameters.Add("@NoteId", SqlDbType.Int).Value = (object)noteId ?? DBNull.Value;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        model.VoucherLines.Add(new ProjectExtractVoucherLineViewModel
                        {
                            VoucherId = ReadInt(reader, "Double_Entry_Vouchers_ID").GetValueOrDefault(),
                            LineNo = ReadInt(reader, "DEV_ID_Line_No"),
                            NoteId = ReadInt(reader, "Notes_ID"),
                            NoteSerial = ReadString(reader, "NoteSerial"),
                            RecordDate = ReadDate(reader, "RecordDate"),
                            AccountCodeInternal = ReadString(reader, "Account_Code"),
                            AccountDisplay = FormatAccount(reader, "Account_Serial", "AccountName", "Account_Code"),
                            Debit = ReadDecimal(reader, "Debit").GetValueOrDefault(),
                            Credit = ReadDecimal(reader, "Credit").GetValueOrDefault(),
                            Description = ReadString(reader, "Description"),
                            BranchName = ReadString(reader, "BranchName"),
                            ProjectName = ReadString(reader, "ProjectName")
                        });
                    }
                }
            }
        }

        private static string FormatAccount(IDataRecord reader, string serialColumn, string nameColumn, string codeColumn)
        {
            var serial = ReadString(reader, serialColumn);
            var name = ReadString(reader, nameColumn);
            var code = ReadString(reader, codeColumn);

            if (!string.IsNullOrWhiteSpace(serial) && !string.IsNullOrWhiteSpace(name))
            {
                return serial + " - " + name;
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }

            if (!string.IsNullOrWhiteSpace(code))
            {
                return "الحساب غير موجود";
            }

            return string.Empty;
        }

        private static string ReadString(IDataRecord reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? string.Empty : Convert.ToString(reader.GetValue(ordinal));
        }

        private static int? ReadInt(IDataRecord reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? (int?)null : Convert.ToInt32(reader.GetValue(ordinal));
        }

        private static DateTime? ReadDate(IDataRecord reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? (DateTime?)null : Convert.ToDateTime(reader.GetValue(ordinal));
        }

        private static decimal? ReadDecimal(IDataRecord reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? (decimal?)null : Convert.ToDecimal(reader.GetValue(ordinal));
        }

        private static double? ReadDouble(IDataRecord reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? (double?)null : Convert.ToDouble(reader.GetValue(ordinal));
        }
    }
}
