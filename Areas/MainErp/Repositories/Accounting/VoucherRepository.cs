using System;
using System.Data.SqlClient;
using MyERP.Areas.MainErp.Interfaces;
using MyERP.Areas.MainErp.Models.Accounting;

namespace MyERP.Areas.MainErp.Repositories.Accounting
{
    public class VoucherRepository
    {
        public void InsertEntry(long voucherId, VoucherBatch batch, VoucherEntry entry, IMainErpUnitOfWork unitOfWork)
        {
            var tableName = batch.OpeningBalanceMode ? "DOUBLE_ENTREY_VOUCHERS1" : "DOUBLE_ENTREY_VOUCHERS";
            var sql = @"INSERT INTO " + tableName + @" (
Double_Entry_Vouchers_ID,
DEV_ID_Line_No,
Account_Code,
Value,
Credit_Or_Debit,
Double_Entry_Vouchers_Description,
Double_Entry_Vouchers_Descriptione,
RecordDate,
Notes_ID,
Transaction_ID,
UserID,
Account_Interval_ID,
project_bill_no,
project_id,
bill_id,
branch_id,
rate,
valuee,
opening_balance_voucher_id,
IsHiddenInv
) VALUES (
@VoucherId,
@LineNumber,
@AccountCode,
@Value,
@CreditOrDebit,
@Description,
@DescriptionEnglish,
@RecordDate,
@NotesId,
@TransactionId,
@UserId,
@AccountIntervalId,
@ProjectBillNo,
@ProjectId,
@BillId,
@BranchId,
@Rate,
@OriginalCurrencyValue,
@OpeningBalanceVoucherId,
@IsHiddenInv
)";

            using (var command = new SqlCommand(sql, unitOfWork.Connection, unitOfWork.Transaction))
            {
                command.Parameters.AddWithValue("@VoucherId", voucherId);
                command.Parameters.AddWithValue("@LineNumber", entry.LineNumber);
                command.Parameters.AddWithValue("@AccountCode", (object)entry.AccountCode ?? DBNull.Value);
                command.Parameters.AddWithValue("@Value", entry.Value);
                command.Parameters.AddWithValue("@CreditOrDebit", (int)entry.EntryType);
                command.Parameters.AddWithValue("@Description", (object)entry.Description ?? DBNull.Value);
                command.Parameters.AddWithValue("@DescriptionEnglish", (object)entry.DescriptionEnglish ?? DBNull.Value);
                command.Parameters.AddWithValue("@RecordDate", entry.RecordDate == default(DateTime) ? DateTime.Today : entry.RecordDate);
                command.Parameters.AddWithValue("@NotesId", (object)entry.NotesId ?? DBNull.Value);
                command.Parameters.AddWithValue("@TransactionId", (object)entry.TransactionId ?? DBNull.Value);
                command.Parameters.AddWithValue("@UserId", (object)entry.UserId ?? DBNull.Value);
                command.Parameters.AddWithValue("@AccountIntervalId", (object)entry.AccountIntervalId ?? DBNull.Value);
                command.Parameters.AddWithValue("@ProjectBillNo", (object)entry.ProjectBillNo ?? DBNull.Value);
                command.Parameters.AddWithValue("@ProjectId", (object)entry.ProjectId ?? DBNull.Value);
                command.Parameters.AddWithValue("@BillId", (object)entry.BillId ?? DBNull.Value);
                command.Parameters.AddWithValue("@BranchId", (object)entry.BranchId ?? DBNull.Value);
                command.Parameters.AddWithValue("@Rate", (object)entry.CurrencyRate ?? DBNull.Value);
                command.Parameters.AddWithValue("@OriginalCurrencyValue", (object)entry.OriginalCurrencyValue ?? DBNull.Value);
                command.Parameters.AddWithValue("@OpeningBalanceVoucherId", (object)batch.OpeningBalanceVoucherId ?? DBNull.Value);
                command.Parameters.AddWithValue("@IsHiddenInv", entry.IsHiddenInvoice);
                command.ExecuteNonQuery();
            }
        }
    }
}
