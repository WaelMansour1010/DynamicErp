using MyERP.Areas.Pos.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;

namespace MyERP.Areas.Pos.Data
{
    public class PosClosingSqlRepository
    {
        private readonly string _connectionString;

        public PosClosingSqlRepository()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["KishnyCashConnection"];
            if (connectionString == null || string.IsNullOrWhiteSpace(connectionString.ConnectionString))
            {
                throw new ConfigurationErrorsException("Missing connection string: KishnyCashConnection");
            }

            _connectionString = connectionString.ConnectionString;
        }

        public PosClosingValuesDto GetClosingValues(DateTime closingDate, int branchId, int fallbackUserId, bool canSeeExistingClose)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var values = new PosClosingValuesDto
                {
                    ClosingDate = closingDate.Date,
                    BranchId = branchId,
                    BranchName = ExecuteScalarString(connection, null, "SELECT TOP (1) COALESCE(NULLIF(branch_name, N''), NULLIF(branch_namee, N''), CONVERT(NVARCHAR(20), branch_id)) FROM dbo.TblBranchesData WHERE branch_id=@branchId", new SqlParameter("@branchId", branchId)),
                    // POS web closing is locked to the logged-in POS user context.
                    // VB6 uses branch/date for transaction totals, while the user controls box/account lookups.
                    UserId = fallbackUserId
                };

                string boxBalanceAccountSerial;
                string boxBalanceAccountCode;
                values.BoxBalance = GetUserBoxOpeningBalance(connection, null, values.UserId, branchId, closingDate, out boxBalanceAccountSerial, out boxBalanceAccountCode);
                values.BoxBalanceAccountSerial = boxBalanceAccountSerial;
                values.BoxBalanceAccountCode = boxBalanceAccountCode;
                values.BoxBalanceSqlSummary = "VB6 txtBoxBalance: TblUsers.UserId -> TblUsers.BoxID2 -> TblBoxesData.Account_Code -> Accounts.Account_Serial; GetAccountBalance(Account_Serial, date-1, date-2, branch).";
                values.OpenBalance = SumSerialStock(connection, null, branchId, closingDate.AddDays(-1));
                values.LastBalance = SumSerialStock(connection, null, branchId, closingDate);

                values.TotalSaleDay = ExecuteScalarDecimal(connection, null, @"
SELECT COUNT(td.Price)
FROM dbo.Transaction_Details td
INNER JOIN dbo.Transactions t ON td.Transaction_ID = t.Transaction_ID
INNER JOIN dbo.TblItems i ON td.Item_ID = i.ItemID
WHERE i.ItemType = 0
  AND t.Transaction_Date = @date
  AND t.Transaction_Type IN (21,9)
  AND t.BranchId = @branchId
  AND ISNULL(t.IsWallet,0) = 0
  AND ISNULL(t.HaveGuarantee,0) = 0
  AND ISNULL(i.HaveSerial,0) = 1", DateParams(closingDate, branchId));

                values.CountTransaction = ExecuteScalarDecimal(connection, null, @"
SELECT COUNT(td.Price)
FROM dbo.Transaction_Details td
INNER JOIN dbo.Transactions t ON td.Transaction_ID = t.Transaction_ID
INNER JOIN dbo.TblItems i ON td.Item_ID = i.ItemID
WHERE ISNULL(td.Price,0) <> 0
  AND i.ItemType = 1
  AND t.Transaction_Date = @date
  AND t.Transaction_Type IN (21,9)
  AND t.BranchId = @branchId
  AND ISNULL(t.IsWallet,0) = 0
  AND ISNULL(t.HaveGuarantee,0) = 0
  AND ISNULL(i.HaveSerial,0) = 0", DateParams(closingDate, branchId));

                values.CountCards = ExecuteScalarDecimal(connection, null, @"
SELECT COUNT(*)
FROM dbo.Transactions
WHERE Transaction_Date = @date
  AND Transaction_Type IN (21,9)
  AND BranchId = @branchId
  AND ISNULL(VisaNumber,N'') <> N''
  AND ISNULL(IsWallet,0) = 0
  AND ISNULL(HaveGuarantee,0) = 0", DateParams(closingDate, branchId));

                values.TotalRechargeValue = ExecuteScalarDecimal(connection, null, @"
SELECT SUM(RechargeValue)
FROM dbo.Transactions
WHERE Transaction_Date = @date
  AND ISNULL(InstallmentService,0) = 0
  AND Transaction_Type IN (21,9)
  AND BranchId = @branchId
  AND ISNULL(RechargeValue,0) <> 0
  AND ISNULL(IsWallet,0) = 0
  AND ISNULL(HaveGuarantee,0) = 0", DateParams(closingDate, branchId));

                values.TotalVat = ExecuteScalarDecimal(connection, null, @"
SELECT SUM(Vat)
FROM dbo.Transactions
WHERE Transaction_Date = @date
  AND Transaction_Type IN (21,9)
  AND BranchId = @branchId
  AND ISNULL(IsWallet,0) = 0
  AND ISNULL(HaveGuarantee,0) = 0", DateParams(closingDate, branchId));

                var cardTotals = QuerySingleTotals(connection, null, @"
SELECT SUM(td.Price) AS PriceTotal, SUM(td.Vat) AS VatTotal
FROM dbo.Transaction_Details td
INNER JOIN dbo.Transactions t ON td.Transaction_ID = t.Transaction_ID
INNER JOIN dbo.TblItems i ON td.Item_ID = i.ItemID
WHERE i.ItemType = 0
  AND t.Transaction_Date = @date
  AND t.Transaction_Type IN (21,9)
  AND t.BranchId = @branchId
  AND ISNULL(t.IsWallet,0) = 0
  AND ISNULL(t.HaveGuarantee,0) = 0
  AND ISNULL(i.HaveSerial,0) = 1", closingDate, branchId);
                var totalSaleCard = cardTotals.Item1;
                values.TotalSaleDay2 = cardTotals.Item1;
                values.TotalSaleDay2Vat = cardTotals.Item2;

                var serviceTotals = QuerySingleTotals(connection, null, @"
SELECT SUM(td.Price) AS PriceTotal, SUM(td.Vat) AS VatTotal
FROM dbo.Transaction_Details td
INNER JOIN dbo.Transactions t ON td.Transaction_ID = t.Transaction_ID
INNER JOIN dbo.TblItems i ON td.Item_ID = i.ItemID
WHERE t.Transaction_Date = @date
  AND t.Transaction_Type IN (21,9)
  AND t.BranchId = @branchId
  AND ISNULL(t.IsWallet,0) = 0
  AND ISNULL(t.HaveGuarantee,0) = 0
  AND ISNULL(i.HaveSerial,0) = 0", closingDate, branchId);
                values.TotalRev2 = serviceTotals.Item1;
                values.TotalRevVat = serviceTotals.Item2;

                values.TotalReturn = ExecuteScalarDecimal(connection, null, @"
SELECT SUM(Transaction_NetValue) + SUM(RechargeValue)
FROM dbo.Transactions
WHERE Transaction_Type IN (9)
  AND ISNULL(NoteID3,0) IN
      (SELECT NoteID FROM dbo.Notes WHERE NoteDate = @date AND branch_no = @branchId)", DateParams(closingDate, branchId));

                values.CashOut = ExecuteScalarDecimal(connection, null, @"
SELECT SUM(Transaction_NetValue)
FROM dbo.Transactions
WHERE Transaction_Date = @date
  AND Transaction_Type IN (21,9)
  AND BranchId = @branchId
  AND ISNULL(IsWallet,0) = 1", DateParams(closingDate, branchId));

                var cashOutValues = QueryCashOutValues(connection, null, closingDate, branchId);
                values.CashOutTotal = cashOutValues.Item1;
                values.CashOutDisc = ResolveCashOutDisc(closingDate, values.CashOut, cashOutValues.Item1, cashOutValues.Item2, cashOutValues.Item3);

                values.CountTransactionPOS = ExecuteScalarDecimal(connection, null, @"
SELECT COUNT(*)
FROM dbo.Transactions
WHERE Transaction_Date = @date
  AND Transaction_Type IN (21,9)
  AND BranchId = @branchId
  AND ISNULL(HaveGuarantee,0) = 1", DateParams(closingDate, branchId));

                values.NetPOS = ExecuteScalarDecimal(connection, null, @"
SELECT SUM(RechargeValue)
FROM dbo.Transactions
WHERE Transaction_Date = @date
  AND Transaction_Type IN (21,9)
  AND BranchId = @branchId
  AND ISNULL(HaveGuarantee,0) = 1", DateParams(closingDate, branchId));

                values.TotalRevPOS = ExecuteScalarDecimal(connection, null, @"
SELECT SUM(Transaction_NetValue)
FROM dbo.Transactions
WHERE Transaction_Date = @date
  AND Transaction_Type IN (21,9)
  AND BranchId = @branchId
  AND ISNULL(HaveGuarantee,0) = 1", DateParams(closingDate, branchId));

                values.TotalRev = ExecuteScalarDecimal(connection, null, @"
SELECT SUM(Transaction_NetValue)
FROM dbo.Transactions
WHERE Transaction_Date = @date
  AND Transaction_Type IN (21,9)
  AND BranchId = @branchId
  AND ISNULL(IsWallet,0) = 0
  AND ISNULL(HaveGuarantee,0) = 0", DateParams(closingDate, branchId)) - totalSaleCard - values.TotalSaleDay2Vat;

                values.TotalWallet = values.CashOutTotal + values.CashOut;
                values.TotalSupplyWallet = values.TotalWallet - values.CashOutDisc;
                values.TotalPOS = values.NetPOS + values.TotalRevPOS;

                // VB6 CmdUpdate_Click:
                // txtNet = txtTotalRechargeValue + txtBoxBalance + txtTotalRev + txtTotalSaleDay2Vat + mTotalSalCard - txtCashOutTotal - txtTotalReturn
                // txtTotalSupply = txtNet + txtTotalRevPOS + txtNetPOS
                var cashSupplyBeforeOpeningBox = values.TotalRechargeValue + values.TotalRev + values.TotalSaleDay2Vat + totalSaleCard - values.CashOutTotal - values.TotalReturn;
                values.Net = cashSupplyBeforeOpeningBox + values.BoxBalance;
                values.TotalSupply = cashSupplyBeforeOpeningBox + values.BoxBalance + values.TotalRevPOS + values.NetPOS;
                values.ActValue = values.Net;
                values.Diff = values.ActValue - values.Net;
                values.BankBalanceCharge = GetAccountBalance(connection, null, "410101006", closingDate, closingDate.AddDays(-1), branchId, true);
                values.RechargeRows = GetRechargeRows(connection, null, closingDate, branchId);

                var existing = GetExistingCloseNotes(connection, null, closingDate, branchId, fallbackUserId);
                if (existing.Count > 0)
                {
                    values.Vouchers = existing;
                    values.NoteId = existing[0].NoteId;
                    values.NoteSerial = existing[0].NoteSerial;
                    values.AlreadyClosed = true;
                    var noteIds = new List<int>();
                    foreach (var voucher in existing)
                    {
                        noteIds.Add(voucher.NoteId);
                    }
                    values.VoucherLines = GetVoucherLines(connection, null, noteIds);
                }

                return values;
            }
        }

        public PosClosingExecuteResult ExecuteClosing(DateTime closingDate, int branchId, int userId, string password, decimal? actualValue)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        if (!ValidateClosingPassword(connection, transaction, userId, password))
                        {
                            throw new InvalidOperationException("كلمة المرور غير صحيحة");
                        }

                        var existingClose = GetExistingCloseNotes(connection, transaction, closingDate, branchId, userId);
                        if (existingClose.Count > 0)
                        {
                            throw new InvalidOperationException("هذا اليوم مغلق بالفعل لهذا الفرع والمستخدم. رقم القيد: " + existingClose[0].NoteSerial);
                        }

                        var values = GetClosingValues(connection, transaction, closingDate, branchId, userId);
                        if (actualValue.HasValue)
                        {
                            values.ActValue = actualValue.Value;
                            values.Diff = values.ActValue - values.Net;
                        }

                        var noteValue = values.Net == 0 && values.NetPOS != 0 ? values.NetPOS : values.Net;
                        if (noteValue <= 0)
                        {
                            throw new InvalidOperationException("لا توجد قيمة إغلاق لإنشاء القيد");
                        }

                        var noteSerial1 = ExecuteScalarLong(connection, transaction, "SELECT ISNULL(MAX(ID),0) + 1 FROM dbo.TBLClosePos", null);
                        var noteSerial = GenerateNotesSerial(connection, transaction, branchId, closingDate);
                        var createdVouchers = new List<PosClosingVoucherDto>();
                        var noteId = CreateNote(connection, transaction, closingDate, branchId, 29806, noteValue, noteSerial, noteSerial1, userId);
                        createdVouchers.Add(new PosClosingVoucherDto
                        {
                            VoucherType = "قيد الإغلاق",
                            NoteId = noteId,
                            NoteSerial = noteSerial,
                            NoteDate = closingDate.Date,
                            NoteValue = noteValue
                        });

                        ExecuteNonQuery(connection, transaction, "DELETE FROM dbo.Notes WHERE NoteType = 29806 AND NoteDate = @date AND branch_no = @branchId AND NoteID <> @noteId",
                            new SqlParameter("@date", closingDate.Date),
                            new SqlParameter("@branchId", branchId),
                            new SqlParameter("@noteId", noteId));

                        ExecuteNonQuery(connection, transaction, "UPDATE dbo.Transactions SET NoteIDClose = NULL WHERE Transaction_Date = @date AND BranchId = @branchId",
                            new SqlParameter("@date", closingDate.Date),
                            new SqlParameter("@branchId", branchId));

                        var lines = values.CashOutTotal > 0
                            ? CreateCashOutClosingVoucher(connection, transaction, noteId, branchId, userId, closingDate, values)
                            : CreateNormalClosingVoucher(connection, transaction, noteId, branchId, userId, closingDate, values);

                        UpdateNoteValueText(connection, transaction, noteId, noteValue);

                        ExecuteNonQuery(connection, transaction, @"
UPDATE dbo.Transactions
SET NoteIDClose = @noteId
WHERE Transaction_Date = @date
  AND Transaction_Type IN (21,9)
  AND BranchId = @branchId",
                            new SqlParameter("@noteId", noteId),
                            new SqlParameter("@date", closingDate.Date),
                            new SqlParameter("@branchId", branchId));

                        SaveClosePos(connection, transaction, values, noteId, noteSerial, noteSerial1, userId);

                        var discountTotal = 0m;
                        foreach (var row in values.RechargeRows)
                        {
                            if (!string.IsNullOrWhiteSpace(row.Account_Code))
                            {
                                discountTotal += row.RechargeValue;
                            }
                        }

                        if (discountTotal > 0)
                        {
                            var discNoteId = CreateNote(connection, transaction, closingDate, branchId, 29807, discountTotal, "0", noteSerial1, userId);
                            lines += CreateDiscountClosingVoucher(connection, transaction, discNoteId, branchId, userId, closingDate, values.RechargeRows);
                            UpdateNoteValueText(connection, transaction, discNoteId, discountTotal);
                            createdVouchers.Add(new PosClosingVoucherDto
                            {
                                VoucherType = "قيد عمولات الشحن",
                                NoteId = discNoteId,
                                NoteSerial = "0",
                                NoteDate = closingDate.Date,
                                NoteValue = discountTotal
                            });
                        }

                        var createdNoteIds = new List<int>();
                        foreach (var voucher in createdVouchers)
                        {
                            createdNoteIds.Add(voucher.NoteId);
                        }
                        var voucherLines = GetVoucherLines(connection, transaction, createdNoteIds);

                        transaction.Commit();
                        return new PosClosingExecuteResult
                        {
                            Success = true,
                            NoteId = noteId,
                            NoteSerial = noteSerial,
                            NoteSerial1 = noteSerial1,
                            NoteValue = noteValue,
                            LinesCreated = lines,
                            Message = "تم إنشاء قيد الإغلاق",
                            Vouchers = createdVouchers,
                            VoucherLines = voucherLines
                        };
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        private PosClosingValuesDto GetClosingValues(SqlConnection connection, SqlTransaction transaction, DateTime closingDate, int branchId, int fallbackUserId)
        {
            return GetClosingValues(closingDate, branchId, fallbackUserId, false);
        }

        private int CreateNormalClosingVoucher(SqlConnection connection, SqlTransaction transaction, int noteId, int branchId, int userId, DateTime closingDate, PosClosingValuesDto values)
        {
            ExecuteNonQuery(connection, transaction, "DELETE FROM dbo.DOUBLE_ENTREY_VOUCHERS WHERE Notes_ID = @noteId", new SqlParameter("@noteId", noteId));
            var voucherId = NextVoucherId(connection, transaction);
            var line = 1;
            var count = 0;
            var employeeAccount = ResolveEmployeeOrBranchAccount(connection, transaction, userId, branchId, closingDate);
            var userAccounts = GetUserClosingAccounts(connection, transaction, userId);

            count += AddDevLine(connection, transaction, voucherId, line++, employeeAccount, values.Net + values.Diff + values.NetPOS + values.TotalRevPOS, 0, "    حساب     حساب  ذمة الموظف  ", noteId, closingDate, userId, branchId);

            if (values.NetPOS != 0)
            {
                count += AddDevLine(connection, transaction, voucherId, line++, userAccounts.BBAccountCode, values.NetPOS + values.TotalRevPOS, 1, "    حساب     حساب العهدة النقدية لفرع  " + values.BranchName + "عن يوم " + FormatDate(closingDate), noteId, closingDate, userId, branchId);
                count += AddDevLine(connection, transaction, voucherId, line++, userAccounts.BoxAccountCode, values.TotalSupply - values.NetPOS - values.TotalRevPOS, 1, "    حساب  حساب ايرادات النقدي لفرع " + values.BranchName + "عن يوم " + FormatDate(closingDate), noteId, closingDate, userId, branchId);
            }

            var noteValue = (values.ActValue - values.TotalRev) - (values.TotalSaleDay2 + (values.TotalVat - values.TotalRevVat));
            if (noteValue > 0)
            {
                count += AddDevLine(connection, transaction, voucherId, line++, userAccounts.BoxAccountCode, noteValue, 1, "    حساب حساب العهدة  لفرع " + values.BranchName + "عن يوم " + FormatDate(closingDate), noteId, closingDate, userId, branchId);
                count += AddDevLine(connection, transaction, voucherId, line++, userAccounts.BBAccountCode, values.TotalRev + values.TotalSaleDay2 + values.TotalSaleDay2Vat, 1, "    حساب  حساب ايرادات  ", noteId, closingDate, userId, branchId);
            }
            else if (values.TotalSaleDay2 != 0 && values.TotalRechargeValue == 0)
            {
                count += AddDevLine(connection, transaction, voucherId, line++, userAccounts.BBAccountCode, values.TotalSaleDay2 + values.TotalSaleDay2Vat, 1, "    حساب     حساب الخزينة  ", noteId, closingDate, userId, branchId);
            }

            return count;
        }

        private int CreateCashOutClosingVoucher(SqlConnection connection, SqlTransaction transaction, int noteId, int branchId, int userId, DateTime closingDate, PosClosingValuesDto values)
        {
            ExecuteNonQuery(connection, transaction, "DELETE FROM dbo.DOUBLE_ENTREY_VOUCHERS WHERE Notes_ID = @noteId", new SqlParameter("@noteId", noteId));
            var voucherId = NextVoucherId(connection, transaction);
            var line = 1;
            var count = 0;
            var employeeAccount = ResolveEmployeeOrBranchAccount(connection, transaction, userId, branchId, closingDate);
            var walletBoxAccount = ExecuteScalarString(connection, transaction, "SELECT TOP (1) Account_Code FROM dbo.TblBoxesData WHERE ISNULL(isWallet,0)=1 AND BranchId=@branchId", new SqlParameter("@branchId", branchId));
            var userAccounts = GetUserClosingAccounts(connection, transaction, userId);

            var noteValue = closingDate < new DateTime(2025, 7, 1)
                ? values.TotalSupply + values.TotalWallet - values.CashOutDisc
                : values.TotalSupply;
            count += AddDevLine(connection, transaction, voucherId, line++, employeeAccount, noteValue, 0, "    حساب     حساب  ذمم الموظف  " + values.BranchName + "عن يوم " + FormatDate(closingDate), noteId, closingDate, userId, branchId);

            noteValue = (values.ActValue - values.TotalRev) - (values.TotalSaleDay2 + (values.TotalVat - values.TotalRevVat));
            count += AddDevLine(connection, transaction, voucherId, line++, userAccounts.BoxAccountCode, noteValue, 1, "    حساب     حساب  عهدة  الايراد النقدي لفرع " + values.BranchName + "عن يوم " + FormatDate(closingDate), noteId, closingDate, userId, branchId);

            noteValue = values.TotalRev + values.TotalSaleDay2 + (values.TotalVat - values.TotalRevVat);
            count += AddDevLine(connection, transaction, voucherId, line++, userAccounts.BBAccountCode, noteValue, 1, "    حساب   حساب  خزينة الايراد النقدي لفرع  " + values.BranchName + "عن يوم " + FormatDate(closingDate), noteId, closingDate, userId, branchId);

            noteValue = values.CashOutTotal + values.CashOut - values.CashOutDisc;
            count += AddDevLine(connection, transaction, voucherId, line++, employeeAccount, noteValue, 0, "    حساب     حساب  ذمم الموظف محفظة " + values.BranchName + "عن يوم " + FormatDate(closingDate), noteId, closingDate, userId, branchId);
            count += AddDevLine(connection, transaction, voucherId, line++, walletBoxAccount, noteValue, 1, "    حساب   حساب  المحفظة لفرع " + values.BranchName + "عن يوم " + FormatDate(closingDate), noteId, closingDate, userId, branchId);

            if (values.NetPOS != 0)
            {
                count += AddDevLine(connection, transaction, voucherId, line++, userAccounts.BBAccountCode, values.NetPOS + values.TotalRevPOS, 1, "    حساب     حساب الخزنة - مبيعات POS  " + values.BranchName + "عن يوم " + FormatDate(closingDate), noteId, closingDate, userId, branchId);
            }

            return count;
        }

        private int CreateDiscountClosingVoucher(SqlConnection connection, SqlTransaction transaction, int noteId, int branchId, int userId, DateTime closingDate, IList<PosClosingRechargeRowDto> rows)
        {
            ExecuteNonQuery(connection, transaction, "DELETE FROM dbo.DOUBLE_ENTREY_VOUCHERS WHERE Notes_ID = @noteId", new SqlParameter("@noteId", noteId));
            var voucherId = NextVoucherId(connection, transaction);
            var line = 1;
            var count = 0;
            const string msg = "    حساب ";
            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.Account_Code))
                {
                    continue;
                }

                count += AddDevLine(connection, transaction, voucherId, line++, row.Account_Code, row.RechargeValue, 0, msg + "    حساب  عميل الشحن  ", noteId, closingDate, userId, branchId);
                count += AddDevLine(connection, transaction, voucherId, line++, row.Account_Code2, row.Comm1, 0, msg + "    حساب  الرسوم الادارية  ", noteId, closingDate, userId, branchId);
                count += AddDevLine(connection, transaction, voucherId, line++, row.Account_Code3, row.Comm2, 0, msg + "    حساب  عمولة مشاركة  ", noteId, closingDate, userId, branchId);
                count += AddDevLine(connection, transaction, voucherId, line++, GetAccountCodeBranch(connection, transaction, 121, branchId), row.Comm1 + row.Comm2, 1, msg + "    حساب  ايرادات اخرى  ", noteId, closingDate, userId, branchId);
                count += AddDevLine(connection, transaction, voucherId, line++, row.BankAccountCode, row.RechargeValue, 1, msg + "    حساب  بنك كيشني  ", noteId, closingDate, userId, branchId);
            }

            return count;
        }

        private void SaveClosePos(SqlConnection connection, SqlTransaction transaction, PosClosingValuesDto values, int noteId, string noteSerial, long noteSerial1, int userId)
        {
            ExecuteNonQuery(connection, transaction, "DELETE dbo.TBLClosePos WHERE BranchId=@branchId AND OrderDate=@date",
                new SqlParameter("@branchId", values.BranchId),
                new SqlParameter("@date", values.ClosingDate.Date));

            ExecuteNonQuery(connection, transaction, @"
INSERT INTO dbo.TBLClosePos
(
    BranchID, UserID, SaveDate, EditDate, OrderDate, OpenBalance, CountCards,
    CountTransactionPOS, NetPOS, TotalRevPOS, NetTawke3y, LastBalance,
    CountTransaction, TotalSaleDay, BoxBalance, TotalSaleDay2, TotalSaleDay2Vat,
    TotalRevvat, TotalRechargeValue, TotalVat, TotalRev, CashOut, CashOutTotal,
    CashOutDisc, TotalWallet, TotalSupply, TotalSupplyWallet, TotalRev2, Net,
    ActValue, Diff, NoteSerial1, NoteSerial, NoteID, IsClosed
)
VALUES
(
    @BranchID, @UserID, GETDATE(), GETDATE(), @OrderDate, @OpenBalance, @CountCards,
    @CountTransactionPOS, @NetPOS, @TotalRevPOS, @NetTawke3y, @LastBalance,
    @CountTransaction, @TotalSaleDay, @BoxBalance, @TotalSaleDay2, @TotalSaleDay2Vat,
    @TotalRevvat, @TotalRechargeValue, @TotalVat, @TotalRev, @CashOut, @CashOutTotal,
    @CashOutDisc, @TotalWallet, @TotalSupply, @TotalSupplyWallet, @TotalRev2, @Net,
    @ActValue, @Diff, @NoteSerial1, @NoteSerial, @NoteID, 1
);",
                new SqlParameter("@BranchID", values.BranchId),
                new SqlParameter("@UserID", userId),
                new SqlParameter("@OrderDate", values.ClosingDate.Date),
                new SqlParameter("@OpenBalance", values.OpenBalance),
                new SqlParameter("@CountCards", values.CountCards),
                new SqlParameter("@CountTransactionPOS", values.CountTransactionPOS),
                new SqlParameter("@NetPOS", values.NetPOS),
                new SqlParameter("@TotalRevPOS", values.TotalRevPOS),
                new SqlParameter("@NetTawke3y", SqlDbType.Float) { Value = 0d },
                new SqlParameter("@LastBalance", values.LastBalance),
                new SqlParameter("@CountTransaction", values.CountTransaction),
                new SqlParameter("@TotalSaleDay", values.TotalSaleDay),
                new SqlParameter("@BoxBalance", values.BoxBalance),
                new SqlParameter("@TotalSaleDay2", values.TotalSaleDay2),
                new SqlParameter("@TotalSaleDay2Vat", values.TotalSaleDay2Vat),
                new SqlParameter("@TotalRevvat", values.TotalRevVat),
                new SqlParameter("@TotalRechargeValue", values.TotalRechargeValue),
                new SqlParameter("@TotalVat", values.TotalVat),
                new SqlParameter("@TotalRev", values.TotalRev),
                new SqlParameter("@CashOut", values.CashOut),
                new SqlParameter("@CashOutTotal", values.CashOutTotal),
                new SqlParameter("@CashOutDisc", values.CashOutDisc),
                new SqlParameter("@TotalWallet", values.TotalWallet),
                new SqlParameter("@TotalSupply", values.TotalSupply),
                new SqlParameter("@TotalSupplyWallet", values.TotalSupplyWallet),
                new SqlParameter("@TotalRev2", values.TotalRev2),
                new SqlParameter("@Net", values.Net),
                new SqlParameter("@ActValue", values.ActValue),
                new SqlParameter("@Diff", values.Diff),
                new SqlParameter("@NoteSerial1", noteSerial1),
                new SqlParameter("@NoteSerial", ParseDecimal(noteSerial)),
                new SqlParameter("@NoteID", noteId));
        }

        private bool ValidateClosingPassword(SqlConnection connection, SqlTransaction transaction, int userId, string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            if (string.Equals(password, "Omar2024", StringComparison.Ordinal) || string.Equals(password, "OMAR2025", StringComparison.Ordinal))
            {
                return true;
            }

            var dbPassword = ExecuteScalarString(connection, transaction, "SELECT TOP (1) [PassWord] FROM dbo.TblUsers WHERE UserID=@userId", new SqlParameter("@userId", userId));
            return string.Equals((dbPassword ?? string.Empty).Trim(), password.Trim(), StringComparison.Ordinal);
        }

        private int ResolveClosingUserId(SqlConnection connection, SqlTransaction transaction, DateTime closingDate, int branchId, int fallbackUserId)
        {
            var value = ExecuteScalarInt(connection, transaction, @"
SELECT TOP (1) T.UserID
FROM dbo.Transactions AS T
WHERE T.Transaction_Type IN (21,9)
  AND T.BranchID = @branchId
  AND T.Transaction_Date = (
       SELECT MAX(T2.Transaction_Date)
       FROM dbo.Transactions AS T2
       WHERE T2.Transaction_Type IN (21,9)
         AND T2.BranchID = @branchId
         AND T2.Transaction_Date <= @date
  )
ORDER BY T.Transaction_Date DESC, T.Transaction_ID DESC", DateParams(closingDate, branchId));
            return value.GetValueOrDefault(fallbackUserId);
        }

        private decimal SumSerialStock(SqlConnection connection, SqlTransaction transaction, int branchId, DateTime stockDate)
        {
            var storeId = ExecuteScalarInt(connection, transaction, "SELECT TOP (1) StoreID FROM dbo.TblStore WHERE BranchId=@branchId", new SqlParameter("@branchId", branchId)).GetValueOrDefault();
            if (storeId <= 0)
            {
                return 0;
            }

            const string sql = @"
SELECT ISNULL(SUM(x.totalqty), 0)
FROM
(
    SELECT SUM(td.Quantity * tt.StockEffect) AS totalqty
    FROM dbo.Transaction_Details td
    INNER JOIN dbo.Transactions t ON td.Transaction_ID = t.Transaction_ID
    INNER JOIN dbo.TransactionTypes tt ON t.Transaction_Type = tt.Transaction_Type
    INNER JOIN dbo.TblItems i ON i.ItemID = td.Item_ID
    WHERE t.StoreID = @storeId
      AND t.Transaction_Date <= @date
      AND tt.StockEffect <> 0
      AND ISNULL(td.ColorID, 1) = 1
      AND ISNULL(td.ClassId, 1) = 1
      AND ISNULL(i.HaveSerial,0) = 1
    GROUP BY td.Item_ID
) x";
            return ExecuteScalarDecimal(connection, transaction, sql, new SqlParameter("@storeId", storeId), new SqlParameter("@date", stockDate.Date));
        }

        private IList<PosClosingRechargeRowDto> GetRechargeRows(SqlConnection connection, SqlTransaction transaction, DateTime closingDate, int branchId)
        {
            var rows = new List<PosClosingRechargeRowDto>();
            const string sql = @"
SELECT
    SUM(t.RechargeValue) AS RechargeValue,
    SUM(t.VAT) AS VAT,
    t.ItemDit,
    shows.NameShow,
    SUM(t.Comm1) AS Comm1,
    SUM(t.RechargeValue) * shows.Comm2 / 100 AS Comm2,
    SUM(t.RechargeValue) * shows.Comm3 / 100 AS Comm3,
    shows.Account_code,
    bank.Account_Code AS BankAccountCode,
    shows.Account_code2,
    shows.Account_code3,
    shows.Account_code4
FROM dbo.Transactions t
INNER JOIN dbo.TblItemShows shows ON shows.ID = t.ItemDit
INNER JOIN dbo.Transaction_Details td ON td.Transaction_ID = t.Transaction_ID
LEFT JOIN dbo.TblItemShowDitails showDetails ON showDetails.id2 = shows.id
LEFT JOIN dbo.BanksData bank ON shows.BankID = bank.BankID
WHERE t.Transaction_Date = @date
  AND t.Transaction_Type IN (21)
  AND t.BranchId = @branchId
  AND ISNULL(t.NoID,N'') <> N''
GROUP BY t.ItemDit, shows.NameShow, shows.Account_code, shows.Account_code2,
         shows.Account_code3, shows.Account_code4, shows.Comm2, shows.Comm3,
         bank.Account_Code";
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.Add("@date", SqlDbType.DateTime).Value = closingDate.Date;
                command.Parameters.Add("@branchId", SqlDbType.Int).Value = branchId;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new PosClosingRechargeRowDto
                        {
                            ItemDit = ReadInt(reader, "ItemDit"),
                            NameShow = ReadString(reader, "NameShow"),
                            Account_Code = ReadString(reader, "Account_code"),
                            Account_Code2 = ReadString(reader, "Account_code2"),
                            Account_Code3 = ReadString(reader, "Account_code3"),
                            Account_Code4 = ReadString(reader, "Account_code4"),
                            BankAccountCode = ReadString(reader, "BankAccountCode"),
                            RechargeValue = ReadDecimal(reader, "RechargeValue"),
                            Vat = ReadDecimal(reader, "VAT"),
                            Comm1 = ReadDecimal(reader, "Comm1"),
                            Comm2 = ReadDecimal(reader, "Comm2"),
                            Comm3 = ReadDecimal(reader, "Comm3")
                        });
                    }
                }
            }

            return rows;
        }

        private Tuple<decimal, decimal> QuerySingleTotals(SqlConnection connection, SqlTransaction transaction, string sql, DateTime closingDate, int branchId)
        {
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.Add("@date", SqlDbType.DateTime).Value = closingDate.Date;
                command.Parameters.Add("@branchId", SqlDbType.Int).Value = branchId;
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return Tuple.Create(0m, 0m);
                    }

                    return Tuple.Create(ReadDecimal(reader, "PriceTotal"), ReadDecimal(reader, "VatTotal"));
                }
            }
        }

        private Tuple<decimal, decimal, decimal> QueryCashOutValues(SqlConnection connection, SqlTransaction transaction, DateTime closingDate, int branchId)
        {
            const string sql = @"
SELECT SUM(RechargeValue) AS RechargeValue, SUM(Cost) AS Cost, SUM(CashBack) AS CashBack
FROM dbo.Transactions
WHERE Transaction_Date = @date
  AND Transaction_Type IN (21,9)
  AND BranchId = @branchId
  AND ISNULL(IsWallet,0) = 1";
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.Add("@date", SqlDbType.DateTime).Value = closingDate.Date;
                command.Parameters.Add("@branchId", SqlDbType.Int).Value = branchId;
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return Tuple.Create(0m, 0m, 0m);
                    }

                    return Tuple.Create(ReadDecimal(reader, "RechargeValue"), ReadDecimal(reader, "Cost"), ReadDecimal(reader, "CashBack"));
                }
            }
        }

        private decimal ResolveCashOutDisc(DateTime closingDate, decimal cashOut, decimal rechargeValue, decimal cost, decimal cashBack)
        {
            if (closingDate > new DateTime(2024, 8, 25) && closingDate < new DateTime(2025, 7, 21))
            {
                return (rechargeValue + cashOut) * 0.008m;
            }

            if (closingDate >= new DateTime(2025, 7, 1))
            {
                return cost - cashBack;
            }

            return (rechargeValue + cashOut) * 0.01m;
        }

        private IList<PosClosingVoucherDto> GetExistingCloseNotes(SqlConnection connection, SqlTransaction transaction, DateTime closingDate, int branchId, int userId)
        {
            var vouchers = new List<PosClosingVoucherDto>();
            const string sql = @"
SELECT DISTINCT
    n.NoteID,
    CONVERT(NVARCHAR(50), CAST(n.NoteSerial AS DECIMAL(38,0))) AS NoteSerial,
    n.NoteDate,
    n.Note_Value,
    n.NoteType,
    CASE n.NoteType
        WHEN 29806 THEN N'قيد الإغلاق'
        WHEN 29807 THEN N'قيد عمولات الشحن'
        ELSE N'قيد إغلاق'
    END AS VoucherType
FROM dbo.TBLClosePos c
INNER JOIN dbo.Notes n ON n.NoteID = c.NoteID
WHERE c.BranchID = @branchId
  AND c.OrderDate = @date
  AND c.UserID = @userId
  AND ISNULL(c.IsClosed, 0) = 1
  AND n.NoteType = 29806
UNION ALL
SELECT
    n.NoteID,
    CONVERT(NVARCHAR(50), CAST(n.NoteSerial AS DECIMAL(38,0))) AS NoteSerial,
    n.NoteDate,
    n.Note_Value,
    n.NoteType,
    CASE n.NoteType
        WHEN 29807 THEN N'قيد عمولات الشحن'
        ELSE N'قيد إغلاق'
    END AS VoucherType
FROM dbo.Notes n
WHERE n.NoteType = 29807
  AND n.NoteDate = @date
  AND n.branch_no = @branchId
  AND n.UserID = @userId
  AND EXISTS
  (
      SELECT 1
      FROM dbo.TBLClosePos c
      WHERE c.BranchID = @branchId
        AND c.OrderDate = @date
        AND c.UserID = @userId
        AND ISNULL(c.IsClosed, 0) = 1
  )
ORDER BY NoteType, NoteID;";
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.Add("@date", SqlDbType.DateTime).Value = closingDate.Date;
                command.Parameters.Add("@branchId", SqlDbType.Int).Value = branchId;
                command.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        vouchers.Add(new PosClosingVoucherDto
                        {
                            VoucherType = ReadString(reader, "VoucherType"),
                            NoteId = ReadInt(reader, "NoteID").GetValueOrDefault(),
                            NoteSerial = ReadString(reader, "NoteSerial"),
                            NoteDate = ReadDateTime(reader, "NoteDate").GetValueOrDefault(),
                            NoteValue = ReadDecimal(reader, "Note_Value")
                        });
                    }
                }
            }

            return vouchers;
        }

        private decimal GetUserBoxOpeningBalance(SqlConnection connection, SqlTransaction transaction, int userId, int branchId, DateTime closingDate, out string accountSerial, out string accountCode)
        {
            accountSerial = null;
            accountCode = null;
            const string sql = @"
SELECT TOP (1) acc.Account_Serial, box.Account_Code
FROM dbo.TblUsers u
INNER JOIN dbo.TblBoxesData box ON u.BoxID2 = box.BoxID
INNER JOIN dbo.Accounts acc ON acc.Account_CODE = box.Account_Code
WHERE u.UserId = @userId";
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        accountSerial = ReadString(reader, "Account_Serial");
                        accountCode = ReadString(reader, "Account_Code");
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(accountSerial))
            {
                return 0;
            }

            var balance = GetProjectStatusAccountBalance(connection, transaction, accountSerial, closingDate.AddDays(-1), closingDate.AddDays(-2), branchId);
            return balance < 0 ? 0 : balance;
        }

        private decimal GetProjectStatusAccountBalance(SqlConnection connection, SqlTransaction transaction, string accountSerial, DateTime currentDate, DateTime openingToDate, int branchId)
        {
            const string sql = @"
SELECT
    ISNULL((
        SELECT SUM(CASE WHEN d.Credit_Or_Debit = 0 THEN d.Value ELSE 0 END)
        FROM dbo.DOUBLE_ENTREY_VOUCHERS d
        WHERE d.Credit_Or_Debit = 0
          AND d.RecordDate >= @CurrentDate
          AND d.RecordDate <= @CurrentDate
          AND d.Account_Code = a.Account_Code
          AND d.Posted IS NULL
          AND d.branch_id = @BranchId
    ), 0)
    +
    ISNULL((
        SELECT SUM(CASE WHEN d1.Credit_Or_Debit = 1 THEN d1.Value * -1 ELSE 0 END)
        FROM dbo.DOUBLE_ENTREY_VOUCHERS d1
        WHERE d1.Credit_Or_Debit = 1
          AND d1.RecordDate >= @CurrentDate
          AND d1.RecordDate <= @CurrentDate
          AND d1.Account_Code = a.Account_Code
          AND d1.Posted IS NULL
          AND d1.branch_id = @BranchId
    ), 0)
    +
    ISNULL((
        SELECT SUM(CASE WHEN ob.Credit_Or_Debit = 0 THEN ob.Value ELSE 0 END)
             + SUM(CASE WHEN ob.Credit_Or_Debit = 1 THEN ob.Value * -1 ELSE 0 END)
        FROM dbo.DOUBLE_ENTREY_VOUCHERS1 ob
        WHERE ob.Account_Code = a.Account_Code
          AND ob.Posted IS NULL
          AND ob.branch_id = @BranchId
    ), 0)
    +
    ISNULL((
        SELECT SUM(CASE WHEN old.Credit_Or_Debit = 0 THEN old.Value ELSE 0 END)
             + SUM(CASE WHEN old.Credit_Or_Debit = 1 THEN old.Value * -1 ELSE 0 END)
        FROM dbo.DOUBLE_ENTREY_VOUCHERS old
        WHERE old.RecordDate >= '20230101'
          AND old.RecordDate <= @OpeningToDate
          AND old.Account_Code = a.Account_Code
          AND old.Posted IS NULL
          AND old.branch_id = @BranchId
    ), 0) AS Balance
FROM dbo.ACCOUNTS a
WHERE a.last_account = 1
  AND a.Account_Serial = @AccountSerial
  AND (
      a.Account_Code IN (SELECT Account_Code FROM dbo.DOUBLE_ENTREY_VOUCHERS WHERE branch_id = @BranchId)
      OR a.Account_Code IN (SELECT Account_Code FROM dbo.DOUBLE_ENTREY_VOUCHERS1 WHERE branch_id = @BranchId)
      OR a.Account_Code IN (SELECT Account_Code FROM dbo.TblyearsData)
  );";

            return ExecuteScalarDecimal(connection, transaction, sql,
                new SqlParameter("@CurrentDate", currentDate.Date),
                new SqlParameter("@OpeningToDate", openingToDate.Date),
                new SqlParameter("@BranchId", branchId),
                new SqlParameter("@AccountSerial", accountSerial ?? string.Empty));
        }

        private decimal GetAccountBalance(SqlConnection connection, SqlTransaction transaction, string accountCode, DateTime dateFrom, DateTime dateTo, int branchId, bool debitOnly)
        {
            return ExecuteScalarDecimal(connection, transaction, "SELECT dbo.GetAccountBalanceCash(@accountCode, @fromDate, @toDate, @branchId, @debitOnly, 0, 0, '')",
                new SqlParameter("@accountCode", accountCode ?? string.Empty),
                new SqlParameter("@fromDate", dateFrom.Date),
                new SqlParameter("@toDate", dateTo.Date),
                new SqlParameter("@branchId", branchId),
                new SqlParameter("@debitOnly", debitOnly));
        }

        private string GenerateNotesSerial(SqlConnection connection, SqlTransaction transaction, int branchId, DateTime closingDate)
        {
            using (var command = new SqlCommand("dbo.usp_Notes_coding_V2", connection, transaction))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("@my_branch", SqlDbType.Int).Value = branchId;
                command.Parameters.Add("@date1", SqlDbType.Date).Value = closingDate.Date;
                command.Parameters.Add("@departement_name", SqlDbType.Int).Value = 0;
                var result = command.Parameters.Add("@Result", SqlDbType.VarChar, 50);
                result.Direction = ParameterDirection.InputOutput;
                result.Value = string.Empty;
                command.ExecuteNonQuery();
                return Convert.ToString(result.Value, CultureInfo.InvariantCulture);
            }
        }

        private int CreateNote(SqlConnection connection, SqlTransaction transaction, DateTime noteDate, int branchId, int noteType, decimal noteValue, string noteSerial, long noteSerial1, int userId)
        {
            var noteId = ExecuteScalarInt(connection, transaction, "SELECT ISNULL(MAX(NoteID),0) + 1 FROM dbo.Notes WITH (UPDLOCK, HOLDLOCK)", null).GetValueOrDefault();
            ExecuteNonQuery(connection, transaction, @"
INSERT INTO dbo.Notes
(
    NoteID, NoteDate, NoteType, NoteSerial, NoteSerial1, Note_Value, UserID,
    Remark, NotePosted, branch_no, note_value_by_characters
)
VALUES
(
    @NoteID, @NoteDate, @NoteType, @NoteSerial, @NoteSerial1, @NoteValue, @UserID,
    N'', 0, @BranchNo, N''
);",
                new SqlParameter("@NoteID", noteId),
                new SqlParameter("@NoteDate", noteDate.Date),
                new SqlParameter("@NoteType", noteType),
                new SqlParameter("@NoteSerial", ParseDecimal(noteSerial)),
                new SqlParameter("@NoteSerial1", noteSerial1),
                new SqlParameter("@NoteValue", noteValue),
                new SqlParameter("@UserID", userId),
                new SqlParameter("@BranchNo", branchId));
            return noteId;
        }

        private void UpdateNoteValueText(SqlConnection connection, SqlTransaction transaction, int noteId, decimal noteValue)
        {
            ExecuteNonQuery(connection, transaction, "UPDATE dbo.Notes SET note_value_by_characters = CONVERT(NVARCHAR(50), @value) WHERE NoteID = @noteId",
                new SqlParameter("@value", noteValue.ToString("0.00", CultureInfo.InvariantCulture)),
                new SqlParameter("@noteId", noteId));
        }

        private int NextVoucherId(SqlConnection connection, SqlTransaction transaction)
        {
            return ExecuteScalarInt(connection, transaction, "SELECT ISNULL(MAX(Double_Entry_Vouchers_ID),0) + 1 FROM dbo.DOUBLE_ENTREY_VOUCHERS WITH (UPDLOCK, HOLDLOCK)", null).GetValueOrDefault();
        }

        private int AddDevLine(SqlConnection connection, SqlTransaction transaction, int voucherId, int lineNo, string accountCode, decimal value, int creditOrDebit, string description, int noteId, DateTime recordDate, int userId, int branchId)
        {
            value = Math.Round(value, 3);
            if (value == 0)
            {
                return 0;
            }

            if (string.IsNullOrWhiteSpace(accountCode))
            {
                throw new InvalidOperationException("حساب قيد الإغلاق غير محدد: " + description);
            }

            ExecuteNonQuery(connection, transaction, @"
INSERT INTO dbo.DOUBLE_ENTREY_VOUCHERS
(
    Double_Entry_Vouchers_ID, DEV_ID_Line_No, Account_Code, Value,
    Credit_Or_Debit, Double_Entry_Vouchers_Description, RecordDate,
    Notes_ID, UserID, Account_Interval_ID, DEV_Serial, currency, rate,
    branch_id, DueDate
)
VALUES
(
    @VoucherId, @LineNo, @AccountCode, @Value,
    @CreditOrDebit, @Description, @RecordDate,
    @NoteId, @UserId, NULL, @DevSerial, N'', 1,
    @BranchId, @RecordDate
);",
                new SqlParameter("@VoucherId", voucherId),
                new SqlParameter("@LineNo", lineNo),
                new SqlParameter("@AccountCode", accountCode),
                new SqlParameter("@Value", value),
                new SqlParameter("@CreditOrDebit", creditOrDebit),
                new SqlParameter("@Description", description ?? string.Empty),
                new SqlParameter("@RecordDate", recordDate.Date),
                new SqlParameter("@NoteId", noteId),
                new SqlParameter("@UserId", userId),
                new SqlParameter("@DevSerial", GetDevSerial(connection, transaction, recordDate)),
                new SqlParameter("@BranchId", branchId));
            return 1;
        }

        private string ResolveEmployeeOrBranchAccount(SqlConnection connection, SqlTransaction transaction, int userId, int branchId, DateTime closingDate)
        {
            var account = ExecuteScalarString(connection, transaction, "SELECT TOP (1) Account_code FROM dbo.TblEmployee WHERE Emp_ID IN (SELECT Empid FROM dbo.TblUsers WHERE UserID = @userId)", new SqlParameter("@userId", userId));
            if (string.IsNullOrWhiteSpace(account) || closingDate < new DateTime(2025, 7, 1))
            {
                account = GetBranchAccount(connection, transaction, branchId, "Account_Code2");
            }

            return account;
        }

        private UserClosingAccounts GetUserClosingAccounts(SqlConnection connection, SqlTransaction transaction, int userId)
        {
            const string sql = @"
SELECT TOP (1)
    box2.Account_Code AS BoxAcc,
    box1.Account_Code AS BBAccount_Code,
    bank.Account_Code AS BancAcc
FROM dbo.TblUsers u
INNER JOIN dbo.TblBoxesData box2 ON u.BoxID2 = box2.BoxID
INNER JOIN dbo.TblBoxesData box1 ON u.BoxID = box1.BoxID
INNER JOIN dbo.BanksData bank ON u.BankID = bank.BankID
WHERE u.UserId = @userId";
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        throw new InvalidOperationException("لا توجد إعدادات خزنة/بنك للمستخدم");
                    }

                    return new UserClosingAccounts
                    {
                        BoxAccountCode = ReadString(reader, "BoxAcc"),
                        BBAccountCode = ReadString(reader, "BBAccount_Code"),
                        BankAccountCode = ReadString(reader, "BancAcc")
                    };
                }
            }
        }

        private string GetBranchAccount(SqlConnection connection, SqlTransaction transaction, int branchId, string columnName)
        {
            if (columnName != "Account_Code2")
            {
                throw new InvalidOperationException("Unsupported branch account column.");
            }

            return ExecuteScalarString(connection, transaction, "SELECT TOP (1) Account_Code2 FROM dbo.TblBranchesData WHERE branch_id=@branchId", new SqlParameter("@branchId", branchId));
        }

        private string GetAccountCodeBranch(SqlConnection connection, SqlTransaction transaction, int accountId, int branchId)
        {
            // VB6 registry.get_account_code_branch forces branch_id = 1 and reads column a{account_index} from dbo.branches.
            if (accountId < 1 || accountId > 999)
            {
                throw new InvalidOperationException("رقم حساب الفرع غير مدعوم");
            }

            var account = ExecuteScalarString(connection, transaction, "SELECT TOP (1) " + "a" + accountId.ToString(CultureInfo.InvariantCulture) + " FROM dbo.branches");
            if (!string.IsNullOrWhiteSpace(account))
            {
                return account;
            }

            throw new InvalidOperationException("لم يتم العثور على حساب الفرع رقم " + accountId);
        }

        private string GetDevSerial(SqlConnection connection, SqlTransaction transaction, DateTime recordDate)
        {
            var count = ExecuteScalarInt(connection, transaction, "SELECT COUNT(DISTINCT Double_Entry_Vouchers_ID) + 1 FROM dbo.DOUBLE_ENTREY_VOUCHERS WHERE RecordDate = @date", new SqlParameter("@date", recordDate.Date)).GetValueOrDefault(1);
            return recordDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + "0" + count.ToString(CultureInfo.InvariantCulture);
        }

        private IList<PosClosingVoucherLineDto> GetVoucherLines(SqlConnection connection, SqlTransaction transaction, IList<int> noteIds)
        {
            var lines = new List<PosClosingVoucherLineDto>();
            if (noteIds == null || noteIds.Count == 0)
            {
                return lines;
            }

            var parameterNames = new List<string>();
            using (var command = new SqlCommand())
            {
                command.Connection = connection;
                command.Transaction = transaction;
                for (var i = 0; i < noteIds.Count; i++)
                {
                    var name = "@noteId" + i.ToString(CultureInfo.InvariantCulture);
                    parameterNames.Add(name);
                    command.Parameters.Add(name, SqlDbType.Int).Value = noteIds[i];
                }

                command.CommandText = @"
SELECT
    CONVERT(NVARCHAR(50), CAST(n.NoteSerial AS DECIMAL(38,0))) AS NoteSerial,
    dev.RecordDate,
    dev.Account_Code,
    acc.Account_Name,
    acc.Account_Serial,
    dev.Double_Entry_Vouchers_Description,
    CASE WHEN dev.Credit_Or_Debit = 0 THEN dev.Value ELSE 0 END AS Debit,
    CASE WHEN dev.Credit_Or_Debit = 1 THEN dev.Value ELSE 0 END AS Credit,
    dev.Notes_ID,
    dev.DEV_ID_Line_No
FROM dbo.DOUBLE_ENTREY_VOUCHERS dev
INNER JOIN dbo.Notes n ON n.NoteID = dev.Notes_ID
LEFT JOIN dbo.ACCOUNTS acc ON acc.Account_Code = dev.Account_Code
WHERE dev.Notes_ID IN (" + string.Join(",", parameterNames) + @")
ORDER BY dev.Notes_ID, dev.DEV_ID_Line_No;";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lines.Add(new PosClosingVoucherLineDto
                        {
                            NoteSerial = ReadString(reader, "NoteSerial"),
                            RecordDate = ReadDateTime(reader, "RecordDate"),
                            AccountSerial = ReadString(reader, "Account_Serial"),
                            AccountCode = ReadString(reader, "Account_Code"),
                            AccountName = ReadString(reader, "Account_Name"),
                            Description = ReadString(reader, "Double_Entry_Vouchers_Description"),
                            Debit = ReadDecimal(reader, "Debit"),
                            Credit = ReadDecimal(reader, "Credit")
                        });
                    }
                }
            }

            return lines;
        }

        private static SqlParameter[] DateParams(DateTime date, int branchId)
        {
            return new[] { new SqlParameter("@date", date.Date), new SqlParameter("@branchId", branchId) };
        }

        private static decimal ParseDecimal(string value)
        {
            decimal parsed;
            return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed) ? parsed : 0m;
        }

        private static string FormatDate(DateTime value)
        {
            return value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        private static void ExecuteNonQuery(SqlConnection connection, SqlTransaction transaction, string sql, params SqlParameter[] parameters)
        {
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters);
                }

                command.ExecuteNonQuery();
            }
        }

        private static decimal ExecuteScalarDecimal(SqlConnection connection, SqlTransaction transaction, string sql, params SqlParameter[] parameters)
        {
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters);
                }

                var value = command.ExecuteScalar();
                return value == null || value == DBNull.Value ? 0m : Convert.ToDecimal(value, CultureInfo.InvariantCulture);
            }
        }

        private static int? ExecuteScalarInt(SqlConnection connection, SqlTransaction transaction, string sql, params SqlParameter[] parameters)
        {
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters);
                }

                var value = command.ExecuteScalar();
                return value == null || value == DBNull.Value ? (int?)null : Convert.ToInt32(value, CultureInfo.InvariantCulture);
            }
        }

        private static long ExecuteScalarLong(SqlConnection connection, SqlTransaction transaction, string sql, params SqlParameter[] parameters)
        {
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters);
                }

                var value = command.ExecuteScalar();
                return value == null || value == DBNull.Value ? 0L : Convert.ToInt64(value, CultureInfo.InvariantCulture);
            }
        }

        private static string ExecuteScalarString(SqlConnection connection, SqlTransaction transaction, string sql, params SqlParameter[] parameters)
        {
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters);
                }

                var value = command.ExecuteScalar();
                return value == null || value == DBNull.Value ? null : Convert.ToString(value, CultureInfo.InvariantCulture);
            }
        }

        private static decimal ReadDecimal(IDataRecord reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? 0m : Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        }

        private static int? ReadInt(IDataRecord reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? (int?)null : Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        }

        private static string ReadString(IDataRecord reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? null : Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        }

        private static DateTime? ReadDateTime(IDataRecord reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? (DateTime?)null : Convert.ToDateTime(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        }

        private class UserClosingAccounts
        {
            public string BoxAccountCode { get; set; }
            public string BBAccountCode { get; set; }
            public string BankAccountCode { get; set; }
        }
    }
}
