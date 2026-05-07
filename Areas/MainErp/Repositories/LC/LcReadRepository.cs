using System;
using System.Data.SqlClient;
using MyERP.Areas.MainErp.Interfaces;
using MyERP.Areas.MainErp.ViewModels;
using MyERP.Areas.MainErp.ViewModels.LC;

namespace MyERP.Areas.MainErp.Repositories.LC
{
    public class LcReadRepository
    {
        private readonly IMainErpDbConnectionFactory _connectionFactory;

        public LcReadRepository(IMainErpDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public PagedReadResult<LCListItemViewModel> Search(string searchText, int? bankId, int? vendorId, int? branchId, int page, int pageSize)
        {
            var result = new PagedReadResult<LCListItemViewModel>();
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : pageSize;

            try
            {
                using (var connection = _connectionFactory.CreateOpenConnection())
                using (var command = new SqlCommand(@"
WITH LcRows AS (
    SELECT
        ROW_NUMBER() OVER (ORDER BY l.TblLCID DESC) AS RowNo,
        COUNT(1) OVER() AS TotalCount,
        l.TblLCID,
        l.LCNO,
        l.Name,
        l.Value,
        l.FromDate,
        l.Todate,
        l.BranchID,
        l.Account_Code,
        b.BankName,
        c.name AS CurrencyName,
        cust.CusName AS VendorName
    FROM TblLC l
    LEFT JOIN BanksData b ON l.BankId = b.BankID
    LEFT JOIN currency c ON l.CurrencyId = c.id
    LEFT JOIN TblCustemers cust ON l.VendorId = cust.CusID
    WHERE (@SearchText IS NULL OR l.LCNO LIKE @SearchLike OR l.Name LIKE @SearchLike)
      AND (@BankId IS NULL OR l.BankId = @BankId)
      AND (@VendorId IS NULL OR l.VendorId = @VendorId)
      AND (@BranchId IS NULL OR l.BranchID = @BranchId)
)
SELECT * FROM LcRows WHERE RowNo BETWEEN @StartRow AND @EndRow ORDER BY RowNo;", connection))
                {
                    AddSearchParameters(command, searchText, bankId, vendorId, branchId, page, pageSize);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (result.TotalCount == 0 && reader["TotalCount"] != DBNull.Value)
                            {
                                result.TotalCount = Convert.ToInt32(reader["TotalCount"]);
                            }

                            result.Items.Add(new LCListItemViewModel
                            {
                                TblLCID = Convert.ToInt32(reader["TblLCID"]),
                                LCNO = Convert.ToString(reader["LCNO"]),
                                Name = Convert.ToString(reader["Name"]),
                                Value = reader["Value"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["Value"]),
                                FromDate = reader["FromDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["FromDate"]),
                                ToDate = reader["Todate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["Todate"]),
                                BranchID = reader["BranchID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["BranchID"]),
                                AccountCode = Convert.ToString(reader["Account_Code"]),
                                BankName = Convert.ToString(reader["BankName"]),
                                CurrencyName = Convert.ToString(reader["CurrencyName"]),
                                VendorName = Convert.ToString(reader["VendorName"])
                            });
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                result.Warning = "LC read model is not available in the configured database yet: " + ex.Message;
            }

            return result;
        }

        public LCDetailsViewModel GetDetails(int id)
        {
            try
            {
                using (var connection = _connectionFactory.CreateOpenConnection())
                using (var command = new SqlCommand(@"
SELECT TOP 1
    l.TblLCID, l.LCNO, l.Name, l.Value, l.FromDate, l.Todate, l.BranchID,
    l.Account_Code, l.Remarks, l.Account_CodeMargin, l.AcceptAccount_Code,
    l.AccountExpensCode, l.OpenBalance, l.OpenBalanceType,
    l.OpenValue, l.Currency_rate, l.LCTyperId, l.BankId, l.BankID2, l.BoxID,
    l.CurrencyId, l.VendorId, l.CountryId, l.project_id, l.projectName,
    l.PaymentTypeID, l.ChequeNumber, l.ChequeDueDate, l.opening_balance_voucher_id,
    l.CloseDate, l.LastParcilDate, l.AccountExpProject,
    l.Locked, l.userid, l.AccountLGParent, l.AccountMarginParent,
    l.AccountAcceptanceParent, l.AccountExpensParent, l.NoteSerial, l.NoteSerial2,
    l.NoteSerialOpen,
    b.BankName, c.name AS CurrencyName, cust.CusName AS VendorName
FROM TblLC l
LEFT JOIN BanksData b ON l.BankId = b.BankID
LEFT JOIN currency c ON l.CurrencyId = c.id
LEFT JOIN TblCustemers cust ON l.VendorId = cust.CusID
WHERE l.TblLCID = @Id;", connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    LCDetailsViewModel model;
                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return new LCDetailsViewModel { TblLCID = id, Warning = "LC record was not found." };
                        }

                        model = new LCDetailsViewModel
                        {
                            TblLCID = Convert.ToInt32(reader["TblLCID"]),
                            LCNO = Convert.ToString(reader["LCNO"]),
                            Name = Convert.ToString(reader["Name"]),
                            Value = reader["Value"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["Value"]),
                            FromDate = reader["FromDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["FromDate"]),
                            ToDate = reader["Todate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["Todate"]),
                            BranchID = reader["BranchID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["BranchID"]),
                            AccountCode = Convert.ToString(reader["Account_Code"]),
                            Remarks = Convert.ToString(reader["Remarks"]),
                            MarginAccountCode = Convert.ToString(reader["Account_CodeMargin"]),
                            AcceptAccountCode = Convert.ToString(reader["AcceptAccount_Code"]),
                            ExpenseAccountCode = Convert.ToString(reader["AccountExpensCode"]),
                            AccountExpProject = Convert.ToString(reader["AccountExpProject"]),
                            AccountLGParent = Convert.ToString(reader["AccountLGParent"]),
                            AccountMarginParent = Convert.ToString(reader["AccountMarginParent"]),
                            AccountAcceptanceParent = Convert.ToString(reader["AccountAcceptanceParent"]),
                            AccountExpensParent = Convert.ToString(reader["AccountExpensParent"]),
                            OpenValue = reader["OpenValue"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["OpenValue"]),
                            CurrencyRate = reader["Currency_rate"] == DBNull.Value ? (double?)null : Convert.ToDouble(reader["Currency_rate"]),
                            LcTypeId = reader["LCTyperId"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["LCTyperId"]),
                            BankId = reader["BankId"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["BankId"]),
                            BankId2 = reader["BankID2"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["BankID2"]),
                            BoxId = reader["BoxID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["BoxID"]),
                            CurrencyId = reader["CurrencyId"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["CurrencyId"]),
                            VendorId = reader["VendorId"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["VendorId"]),
                            CountryId = reader["CountryId"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["CountryId"]),
                            ProjectId = reader["project_id"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["project_id"]),
                            ProjectName = Convert.ToString(reader["projectName"]),
                            PaymentTypeId = reader["PaymentTypeID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["PaymentTypeID"]),
                            ChequeNumber = Convert.ToString(reader["ChequeNumber"]),
                            ChequeDueDate = reader["ChequeDueDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["ChequeDueDate"]),
                            OpeningBalanceVoucherId = reader["opening_balance_voucher_id"] == DBNull.Value ? (double?)null : Convert.ToDouble(reader["opening_balance_voucher_id"]),
                            CloseDate = reader["CloseDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["CloseDate"]),
                            LastParcilDate = reader["LastParcilDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["LastParcilDate"]),
                            Locked = reader["Locked"] == DBNull.Value ? (bool?)null : Convert.ToBoolean(reader["Locked"]),
                            UserId = reader["userid"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["userid"]),
                            NoteSerial = Convert.ToString(reader["NoteSerial"]),
                            NoteSerial2 = Convert.ToString(reader["NoteSerial2"]),
                            NoteSerialOpen = Convert.ToString(reader["NoteSerialOpen"]),
                            OpenBalance = reader["OpenBalance"] == DBNull.Value ? (double?)null : Convert.ToDouble(reader["OpenBalance"]),
                            OpenBalanceType = reader["OpenBalanceType"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["OpenBalanceType"]),
                            BankName = Convert.ToString(reader["BankName"]),
                            CurrencyName = Convert.ToString(reader["CurrencyName"]),
                            VendorName = Convert.ToString(reader["VendorName"])
                        };
                    }

                    LoadLinkedNotes(connection, model);
                    return model;
                }
            }
            catch (SqlException ex)
            {
                return new LCDetailsViewModel { TblLCID = id, Warning = "LC details are not available in the configured database yet: " + ex.Message };
            }
        }

        private static void LoadLinkedNotes(SqlConnection connection, LCDetailsViewModel model)
        {
            try
            {
                using (var command = new SqlCommand(@"
SELECT TOP 20
    NoteID,
    NoteDate,
    NoteType,
    NoteSerial,
    Note_Value,
    Remark
FROM Notes
WHERE TblLCID = @Id
ORDER BY NoteDate DESC, NoteID DESC;", connection))
                {
                    command.Parameters.AddWithValue("@Id", model.TblLCID);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            model.LinkedNotes.Add(new LCLinkedNoteViewModel
                            {
                                NoteID = Convert.ToInt32(reader["NoteID"]),
                                NoteDate = reader["NoteDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["NoteDate"]),
                                NoteType = Convert.ToString(reader["NoteType"]),
                                NoteSerial = Convert.ToString(reader["NoteSerial"]),
                                NoteValue = reader["Note_Value"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["Note_Value"]),
                                Remark = Convert.ToString(reader["Remark"])
                            });
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                model.NotesWarning = "Linked LC notes preview is not available in this database: " + ex.Message;
            }
        }

        private static void AddSearchParameters(SqlCommand command, string searchText, int? bankId, int? vendorId, int? branchId, int page, int pageSize)
        {
            command.Parameters.AddWithValue("@SearchText", string.IsNullOrWhiteSpace(searchText) ? (object)DBNull.Value : searchText);
            command.Parameters.AddWithValue("@SearchLike", string.IsNullOrWhiteSpace(searchText) ? (object)DBNull.Value : "%" + searchText + "%");
            command.Parameters.AddWithValue("@BankId", (object)bankId ?? DBNull.Value);
            command.Parameters.AddWithValue("@VendorId", (object)vendorId ?? DBNull.Value);
            command.Parameters.AddWithValue("@BranchId", (object)branchId ?? DBNull.Value);
            command.Parameters.AddWithValue("@StartRow", ((page - 1) * pageSize) + 1);
            command.Parameters.AddWithValue("@EndRow", page * pageSize);
        }
    }
}
