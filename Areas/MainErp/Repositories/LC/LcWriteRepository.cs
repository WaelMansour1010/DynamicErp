using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.Interfaces;
using MyERP.Areas.MainErp.Services.Accounting;
using MyERP.Areas.MainErp.ViewModels.LC;

namespace MyERP.Areas.MainErp.Repositories.LC
{
    public class LcWriteRepository
    {
        private readonly IMainErpDbConnectionFactory _connectionFactory;
        private readonly IManualIdGenerator _manualIdGenerator;

        public LcWriteRepository(IMainErpDbConnectionFactory connectionFactory)
            : this(connectionFactory, new ManualIdGenerator(connectionFactory))
        {
        }

        public LcWriteRepository(IMainErpDbConnectionFactory connectionFactory, IManualIdGenerator manualIdGenerator)
        {
            _connectionFactory = connectionFactory;
            _manualIdGenerator = manualIdGenerator;
        }

        public LCEditViewModel CreateNew(int? branchId, int? userId)
        {
            var model = new LCEditViewModel
            {
                FromDate = DateTime.Today,
                ToDate = DateTime.Today,
                CurrencyRate = 1m,
                PercentV = 5m,
                BranchID = branchId,
                AutoCreateMissingAccounts = true
            };

            PrepareEditModel(model);
            return model;
        }

        public void PrepareEditModel(LCEditViewModel model)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                if (model.TblLCID.HasValue && model.TblLCID.Value > 0)
                {
                    LoadEditableGridRows(connection, model);
                }
                else
                {
                    EnsureEditableGridPlaceholders(model);
                }

                LoadLookups(connection, model);
            }
        }

        public LCEditViewModel GetForEdit(int id)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                LCEditViewModel model;
                int? noteId;
                using (var command = new SqlCommand(@"
SELECT TOP 1
    TblLCID, LCNO, LCTyperId, BankId, BankID2, BoxID, Value, OpenValue, CurrencyId,
    Currency_rate, PercentV, VendorId, CountryId, FromDate, Todate, CloseDate,
    LastParcilDate, OpenBalanceDate, OpenBalance, OpenBalanceType, opening_balance_voucher_id,
    BranchID, Remarks, project_id, projectName, PaymentTypeID,
    ChequeNumber, ChequeDueDate, Locked, AccountLGParent, AccountMarginParent,
    AccountAcceptanceParent, AccountExpensParent, LCAccount_Code, Account_Code,
    Account_CodeMargin, MarginAccount_Code, AcceptAccount_Code, AccountExpensCode,
    AccountExpProject, NoteID
FROM TblLC
WHERE TblLCID = @Id;", connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return new LCEditViewModel { TblLCID = id, Warning = "LC was not found." };
                        }

                        noteId = ReadInt(reader, "NoteID");
                        model = new LCEditViewModel
                        {
                            TblLCID = ReadInt(reader, "TblLCID"),
                            LCNO = ReadString(reader, "LCNO"),
                            LCTyperId = ReadInt(reader, "LCTyperId"),
                            BankId = ReadInt(reader, "BankId"),
                            BankID2 = ReadInt(reader, "BankID2"),
                            BoxID = ReadInt(reader, "BoxID"),
                            Value = ReadDecimal(reader, "Value"),
                            OpenValue = ReadDecimal(reader, "OpenValue"),
                            CurrencyId = ReadInt(reader, "CurrencyId"),
                            CurrencyRate = ReadDecimal(reader, "Currency_rate"),
                            PercentV = ReadDecimal(reader, "PercentV"),
                            VendorId = ReadInt(reader, "VendorId"),
                            CountryId = ReadInt(reader, "CountryId"),
                            FromDate = ReadDate(reader, "FromDate"),
                            ToDate = ReadDate(reader, "Todate"),
                            CloseDate = ReadDate(reader, "CloseDate"),
                            LastParcilDate = ReadDate(reader, "LastParcilDate"),
                            OpenBalanceDate = ReadDate(reader, "OpenBalanceDate"),
                            OpenBalance = ReadDecimal(reader, "OpenBalance"),
                            OpenBalanceType = ReadInt(reader, "OpenBalanceType"),
                            OpeningBalanceVoucherId = ReadDouble(reader, "opening_balance_voucher_id"),
                            BranchID = ReadInt(reader, "BranchID"),
                            Remarks = ReadString(reader, "Remarks"),
                            ProjectId = ReadInt(reader, "project_id"),
                            ProjectName = ReadString(reader, "projectName"),
                            PaymentTypeID = ReadInt(reader, "PaymentTypeID"),
                            ChequeNumber = ReadString(reader, "ChequeNumber"),
                            ChequeDueDate = ReadDate(reader, "ChequeDueDate"),
                            Locked = ReadBool(reader, "Locked") == true,
                            AccountLGParent = ReadString(reader, "AccountLGParent"),
                            AccountMarginParent = ReadString(reader, "AccountMarginParent"),
                            AccountAcceptanceParent = ReadString(reader, "AccountAcceptanceParent"),
                            AccountExpensParent = ReadString(reader, "AccountExpensParent"),
                            LCAccountCode = FirstText(ReadString(reader, "LCAccount_Code"), ReadString(reader, "Account_Code")),
                            MarginAccountCode = FirstText(ReadString(reader, "Account_CodeMargin"), ReadString(reader, "MarginAccount_Code")),
                            AcceptanceAccountCode = ReadString(reader, "AcceptAccount_Code"),
                            ExpenseAccountCode = ReadString(reader, "AccountExpensCode"),
                            ProjectExpenseAccountCode = ReadString(reader, "AccountExpProject"),
                            AutoCreateMissingAccounts = false,
                            HasPostedVoucher = false
                        };
                    }
                }

                model.HasPostedVoucher = HasPostedVoucher(connection, noteId);
                LoadEditableGridRows(connection, model);
                LoadLookups(connection, model);
                return model;
            }
        }

        public int Save(LCEditViewModel model, int? userId)
        {
            Validate(model);

            using (var unitOfWork = new MainErpUnitOfWork(_connectionFactory))
            {
                unitOfWork.Begin();
                try
                {
                    var isNew = !model.TblLCID.HasValue || model.TblLCID.Value == 0;
                    if (isNew)
                    {
                        model.TblLCID = Convert.ToInt32(_manualIdGenerator.Allocate(ManualIdTarget.TblLcId, unitOfWork).Value);
                    }

                    if (model.AutoCreateMissingAccounts)
                    {
                        EnsureLcAccounts(model, unitOfWork, userId);
                    }

                    if (isNew)
                    {
                        InsertHeader(model, userId, unitOfWork);
                    }
                    else
                    {
                        UpdateHeader(model, userId, unitOfWork);
                    }

                    SaveEditableGridRows(model, unitOfWork);

                    unitOfWork.Commit();
                    return model.TblLCID.Value;
                }
                catch
                {
                    unitOfWork.Rollback();
                    throw;
                }
            }
        }

        public LCPostingResultViewModel CreateNormalOpeningVoucher(int tblLcId, int? userId)
        {
            using (var unitOfWork = new MainErpUnitOfWork(_connectionFactory))
            {
                unitOfWork.Begin();
                try
                {
                    var lc = LoadPostingInfo(tblLcId, unitOfWork);
                    if (lc == null)
                    {
                        throw new InvalidOperationException("LC was not found.");
                    }

                    if (lc.NoteId.HasValue && HasVoucherRows(lc.NoteId.Value, unitOfWork))
                    {
                        return new LCPostingResultViewModel
                        {
                            Success = true,
                            TblLCID = tblLcId,
                            NoteId = lc.NoteId,
                            VoucherId = GetVoucherId(lc.NoteId.Value, unitOfWork),
                            Message = "القيد موجود بالفعل، لم يتم إنشاء قيد مكرر."
                        };
                    }

                    var bankAccount = GetBankAccount(lc.BankId, unitOfWork);
                    if (string.IsNullOrWhiteSpace(bankAccount))
                    {
                        throw new InvalidOperationException("لا يوجد حساب بنك مرتبط بالبنك المختار.");
                    }

                    if (string.IsNullOrWhiteSpace(lc.MarginAccountCode))
                    {
                        throw new InvalidOperationException("حساب الهامش غير موجود. يجب حفظ/إنشاء حسابات الاعتماد أولًا.");
                    }

                    var voucherValue = Math.Round((lc.Value ?? 0m) * ((lc.PercentV ?? 0m) / 100m) * (lc.CurrencyRate ?? 1m), 4);
                    if (voucherValue <= 0m)
                    {
                        throw new InvalidOperationException("قيمة قيد الاعتماد يجب أن تكون أكبر من صفر.");
                    }

                    var noteId = lc.NoteId ?? Convert.ToInt32(_manualIdGenerator.Allocate(ManualIdTarget.NotesNoteId, unitOfWork).Value);
                    var noteSerial = lc.NoteSerial.HasValue ? lc.NoteSerial.Value : AllocateNoteSerial(22001, lc.FromDate ?? DateTime.Today, unitOfWork);
                    var voucherId = Convert.ToInt64(_manualIdGenerator.Allocate(ManualIdTarget.VoucherId, unitOfWork).Value);
                    var rowId = Guid.NewGuid();

                    UpsertNormalLcNote(lc, noteId, noteSerial, voucherValue, voucherId, rowId, userId, unitOfWork);
                    DeleteVoucherRowsForNote(noteId, unitOfWork);
                    InsertVoucherRow(voucherId, 1, lc.MarginAccountCode, voucherValue, 0, "    حساب " + lc.LCNO + "Margin Account.", lc.FromDate, noteId, userId, lc.BranchId, unitOfWork);
                    InsertVoucherRow(voucherId, 2, bankAccount, voucherValue, 1, "    حساب " + lc.LCNO + " Bank account", lc.FromDate, noteId, userId, lc.BranchId, unitOfWork);

                    using (var command = new SqlCommand(@"
UPDATE TblLC
SET NoteID = @NoteID, NoteSerial = @NoteSerial, NoteIDRowId = @RowId
WHERE TblLCID = @TblLCID;", unitOfWork.Connection, unitOfWork.Transaction))
                    {
                        command.Parameters.AddWithValue("@NoteID", noteId);
                        command.Parameters.AddWithValue("@NoteSerial", noteSerial);
                        command.Parameters.AddWithValue("@RowId", rowId);
                        command.Parameters.AddWithValue("@TblLCID", tblLcId);
                        command.ExecuteNonQuery();
                    }

                    TryWriteAudit(unitOfWork, "LC.PostHeader", tblLcId, userId, "Created LC opening voucher. NoteId=" + noteId + "; VoucherId=" + voucherId + "; Value=" + voucherValue.ToString(CultureInfo.InvariantCulture), null, null);
                    unitOfWork.Commit();
                    return new LCPostingResultViewModel
                    {
                        Success = true,
                        TblLCID = tblLcId,
                        NoteId = noteId,
                        VoucherId = voucherId,
                        Debit = voucherValue,
                        Credit = voucherValue,
                        Message = "تم إنشاء قيد فتح الاعتماد الأساسي بنجاح."
                    };
                }
                catch
                {
                    unitOfWork.Rollback();
                    throw;
                }
            }
        }

        public LCPostingResultViewModel CreateOpenExpenseVoucher(int tblLcId, int? userId)
        {
            using (var unitOfWork = new MainErpUnitOfWork(_connectionFactory))
            {
                unitOfWork.Begin();
                try
                {
                    var lc = LoadPostingInfo(tblLcId, unitOfWork);
                    if (lc == null)
                    {
                        throw new InvalidOperationException("LC was not found.");
                    }

                    if (lc.NoteIdOpen.HasValue && HasVoucherRows(lc.NoteIdOpen.Value, unitOfWork))
                    {
                        return new LCPostingResultViewModel
                        {
                            Success = true,
                            TblLCID = tblLcId,
                            NoteId = lc.NoteIdOpen,
                            VoucherId = GetVoucherId(lc.NoteIdOpen.Value, unitOfWork),
                            Message = "قيد مصاريف فتح الاعتماد موجود بالفعل، لم يتم إنشاء قيد مكرر."
                        };
                    }

                    var bankAccount = GetBankAccount(lc.BankId, unitOfWork);
                    if (string.IsNullOrWhiteSpace(bankAccount))
                    {
                        throw new InvalidOperationException("لا يوجد حساب بنك مرتبط بالبنك المختار.");
                    }

                    if (string.IsNullOrWhiteSpace(lc.ExpenseAccountCode))
                    {
                        throw new InvalidOperationException("حساب مصروفات الاعتماد غير موجود.");
                    }

                    var total = Math.Round((lc.OpenValue ?? 0m) * (lc.CurrencyRate ?? 1m), 4);
                    if (total <= 0m)
                    {
                        throw new InvalidOperationException("قيمة مصاريف فتح الاعتماد يجب أن تكون أكبر من صفر.");
                    }

                    var vatPercent = FindVatPercent(unitOfWork);
                    var netExpense = Math.Round(total / (1m + (vatPercent / 100m)), 4);
                    var vatValue = Math.Round(total - netExpense, 4);
                    var vatAccount = vatValue == 0m ? null : FindVatInputAccount(unitOfWork);
                    if (vatValue != 0m && string.IsNullOrWhiteSpace(vatAccount))
                    {
                        throw new InvalidOperationException("لم يتم العثور على حساب مدخلات ضريبة القيمة المضافة.");
                    }

                    var noteId = lc.NoteIdOpen ?? Convert.ToInt32(_manualIdGenerator.Allocate(ManualIdTarget.NotesNoteId, unitOfWork).Value);
                    var noteSerial = lc.NoteSerialOpen.HasValue ? lc.NoteSerialOpen.Value : AllocateNoteSerial(22010, lc.FromDate ?? DateTime.Today, unitOfWork);
                    var voucherId = Convert.ToInt64(_manualIdGenerator.Allocate(ManualIdTarget.VoucherId, unitOfWork).Value);
                    var rowId = Guid.NewGuid();

                    UpsertLcNote(lc, noteId, noteSerial, 22010, total, voucherId, rowId, userId, unitOfWork, "    حساب ال" + lc.LCNO, "NoteIDOpen", "NoteSerialOpen", "NoteIDOpenRowId");
                    DeleteVoucherRowsForNote(noteId, unitOfWork);

                    var lineNo = 1;
                    InsertVoucherRow(voucherId, lineNo++, lc.ExpenseAccountCode, netExpense, 0, "    حساب " + lc.LCNO + "    Account for the expenses of opening the LC  ", lc.FromDate, noteId, userId, lc.BranchId, unitOfWork);
                    if (vatValue != 0m)
                    {
                        InsertVoucherRow(voucherId, lineNo++, vatAccount, vatValue, 0, " Vat account", lc.FromDate, noteId, userId, lc.BranchId, unitOfWork);
                    }
                    InsertVoucherRow(voucherId, lineNo, bankAccount, total, 1, "    حساب " + lc.LCNO + "    Bank account", lc.FromDate, noteId, userId, lc.BranchId, unitOfWork);

                    TryWriteAudit(unitOfWork, "LC.PostOpenExpense", tblLcId, userId, "Created LC open expense voucher. NoteId=" + noteId + "; VoucherId=" + voucherId + "; Total=" + total.ToString(CultureInfo.InvariantCulture), null, null);
                    unitOfWork.Commit();
                    return new LCPostingResultViewModel
                    {
                        Success = true,
                        TblLCID = tblLcId,
                        NoteId = noteId,
                        VoucherId = voucherId,
                        Debit = total,
                        Credit = total,
                        Message = "تم إنشاء قيد مصاريف فتح الاعتماد بنجاح."
                    };
                }
                catch
                {
                    unitOfWork.Rollback();
                    throw;
                }
            }
        }

        public LCPostingResultViewModel CreateCloseVoucher(int tblLcId, int? userId)
        {
            using (var unitOfWork = new MainErpUnitOfWork(_connectionFactory))
            {
                unitOfWork.Begin();
                try
                {
                    var lc = LoadPostingInfo(tblLcId, unitOfWork);
                    if (lc == null)
                    {
                        throw new InvalidOperationException("LC was not found.");
                    }

                    if (lc.NoteIdClose.HasValue && HasVoucherRows(lc.NoteIdClose.Value, unitOfWork))
                    {
                        return new LCPostingResultViewModel
                        {
                            Success = true,
                            TblLCID = tblLcId,
                            NoteId = lc.NoteIdClose,
                            VoucherId = GetVoucherId(lc.NoteIdClose.Value, unitOfWork),
                            Message = "قيد إغلاق الاعتماد موجود بالفعل، لم يتم إنشاء قيد مكرر."
                        };
                    }

                    var bankAccount = GetBankAccount(lc.BankId, unitOfWork);
                    if (string.IsNullOrWhiteSpace(bankAccount))
                    {
                        throw new InvalidOperationException("لا يوجد حساب بنك مرتبط بالبنك المختار.");
                    }

                    if (string.IsNullOrWhiteSpace(lc.MarginAccountCode))
                    {
                        throw new InvalidOperationException("حساب الهامش غير موجود.");
                    }

                    var voucherValue = Math.Round((lc.Value ?? 0m) * ((lc.PercentV ?? 0m) / 100m) * (lc.CurrencyRate ?? 1m), 4);
                    if (voucherValue <= 0m)
                    {
                        throw new InvalidOperationException("قيمة قيد الإغلاق يجب أن تكون أكبر من صفر.");
                    }

                    var noteDate = lc.ToDate ?? lc.FromDate ?? DateTime.Today;
                    var noteId = lc.NoteIdClose ?? Convert.ToInt32(_manualIdGenerator.Allocate(ManualIdTarget.NotesNoteId, unitOfWork).Value);
                    var noteSerial = lc.NoteSerialClose.HasValue ? lc.NoteSerialClose.Value : AllocateNoteSerial(22005, noteDate, unitOfWork);
                    var voucherId = Convert.ToInt64(_manualIdGenerator.Allocate(ManualIdTarget.VoucherId, unitOfWork).Value);
                    var rowId = Guid.NewGuid();

                    lc.FromDate = noteDate;
                    UpsertLcNote(lc, noteId, noteSerial, 22005, voucherValue, voucherId, rowId, userId, unitOfWork, "    حساب ال" + lc.LCNO, "NoteID2", "NoteSerial2", "NoteID2RowId");
                    DeleteVoucherRowsForNote(noteId, unitOfWork);
                    InsertVoucherRow(voucherId, 1, bankAccount, voucherValue, 0, "    حساب " + lc.LCNO + " Bank account", noteDate, noteId, userId, lc.BranchId, unitOfWork);
                    InsertVoucherRow(voucherId, 2, lc.MarginAccountCode, voucherValue, 1, "    حساب " + lc.LCNO + "Margin Account.", noteDate, noteId, userId, lc.BranchId, unitOfWork);

                    using (var command = new SqlCommand("UPDATE TblLC SET Locked = 1, CloseDate = ISNULL(CloseDate, @CloseDate), Todate = @CloseDate WHERE TblLCID = @TblLCID;", unitOfWork.Connection, unitOfWork.Transaction))
                    {
                        command.Parameters.AddWithValue("@CloseDate", noteDate);
                        command.Parameters.AddWithValue("@TblLCID", tblLcId);
                        command.ExecuteNonQuery();
                    }

                    TryWriteAudit(unitOfWork, "LC.Close", tblLcId, userId, "Created LC close voucher and locked LC. NoteId=" + noteId + "; VoucherId=" + voucherId + "; Value=" + voucherValue.ToString(CultureInfo.InvariantCulture), null, "Locked=1; CloseDate=" + noteDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                    unitOfWork.Commit();
                    return new LCPostingResultViewModel
                    {
                        Success = true,
                        TblLCID = tblLcId,
                        NoteId = noteId,
                        VoucherId = voucherId,
                        Debit = voucherValue,
                        Credit = voucherValue,
                        Message = "تم إنشاء قيد إغلاق الاعتماد بنجاح."
                    };
                }
                catch
                {
                    unitOfWork.Rollback();
                    throw;
                }
            }
        }

        public LCPostingResultViewModel CreateOpeningBalanceVoucher(int tblLcId, int? userId)
        {
            using (var unitOfWork = new MainErpUnitOfWork(_connectionFactory))
            {
                unitOfWork.Begin();
                try
                {
                    var lc = LoadPostingInfo(tblLcId, unitOfWork);
                    if (lc == null)
                    {
                        throw new InvalidOperationException("LC was not found.");
                    }

                    if (lc.OpeningBalanceVoucherId.HasValue && HasOpeningVoucherRows(lc.OpeningBalanceVoucherId.Value, unitOfWork))
                    {
                        return new LCPostingResultViewModel
                        {
                            Success = true,
                            TblLCID = tblLcId,
                            VoucherId = Convert.ToInt64(lc.OpeningBalanceVoucherId.Value),
                            Message = "قيد الرصيد الافتتاحي موجود بالفعل، لم يتم إنشاء قيد مكرر."
                        };
                    }

                    var openingBalance = Math.Round(lc.OpenBalance ?? 0m, 4);
                    if (openingBalance <= 0m)
                    {
                        throw new InvalidOperationException("قيمة الرصيد الافتتاحي يجب أن تكون أكبر من صفر.");
                    }

                    if (!lc.OpenBalanceType.HasValue || (lc.OpenBalanceType.Value != 0 && lc.OpenBalanceType.Value != 1))
                    {
                        throw new InvalidOperationException("يجب تحديد نوع الرصيد الافتتاحي: مدين أو دائن.");
                    }

                    var bankAccount = GetBankAccount(lc.BankId, unitOfWork);
                    var lcAccount = lc.LCAccountCode;
                    if (string.IsNullOrWhiteSpace(lcAccount))
                    {
                        throw new InvalidOperationException("حساب الاعتماد غير موجود. احفظ أو أنشئ حسابات الاعتماد أولًا.");
                    }

                    if (string.IsNullOrWhiteSpace(bankAccount))
                    {
                        throw new InvalidOperationException("لا يوجد حساب بنك مرتبط بالبنك المختار.");
                    }

                    var noteDate = lc.OpenBalanceDate ?? lc.FromDate ?? DateTime.Today;
                    var noteId = Convert.ToInt32(_manualIdGenerator.Allocate(ManualIdTarget.Notes1NoteId, unitOfWork).Value);
                    var noteSerial = AllocateOpeningNoteSerial(101, noteDate, unitOfWork);
                    var voucherId = Convert.ToInt64(_manualIdGenerator.Allocate(ManualIdTarget.OpeningBalanceVoucherId, unitOfWork).Value);
                    var openingBalanceGroupId = voucherId;
                    var rowId = Guid.NewGuid();
                    var debitAccount = lc.OpenBalanceType.Value == 0 ? lcAccount : bankAccount;
                    var creditAccount = lc.OpenBalanceType.Value == 0 ? bankAccount : lcAccount;

                    UpsertOpeningLcNote(lc, noteId, noteSerial, openingBalance, voucherId, rowId, userId, noteDate, unitOfWork);
                    InsertOpeningVoucherRow(voucherId, 1, debitAccount, openingBalance, 0, "Opening balance for LC " + lc.LCNO, noteDate, noteId, userId, lc.BranchId, openingBalanceGroupId, unitOfWork);
                    InsertOpeningVoucherRow(voucherId, 2, creditAccount, openingBalance, 1, "Opening balance for LC " + lc.LCNO, noteDate, noteId, userId, lc.BranchId, openingBalanceGroupId, unitOfWork);

                    using (var command = new SqlCommand(@"
UPDATE TblLC
SET opening_balance_voucher_id = @OpeningBalanceVoucherId,
    OpenBalanceDate = @OpenBalanceDate
WHERE TblLCID = @TblLCID;", unitOfWork.Connection, unitOfWork.Transaction))
                    {
                        command.Parameters.AddWithValue("@OpeningBalanceVoucherId", openingBalanceGroupId);
                        command.Parameters.AddWithValue("@OpenBalanceDate", noteDate);
                        command.Parameters.AddWithValue("@TblLCID", tblLcId);
                        command.ExecuteNonQuery();
                    }

                    TryWriteAudit(unitOfWork, "LC.PostOpeningBalance", tblLcId, userId, "Created LC opening balance voucher in DOUBLE_ENTREY_VOUCHERS1. NoteId=" + noteId + "; VoucherId=" + voucherId + "; Value=" + openingBalance.ToString(CultureInfo.InvariantCulture), null, null);
                    unitOfWork.Commit();
                    return new LCPostingResultViewModel
                    {
                        Success = true,
                        TblLCID = tblLcId,
                        NoteId = noteId,
                        VoucherId = voucherId,
                        Debit = openingBalance,
                        Credit = openingBalance,
                        Message = "تم ترحيل الرصيد الافتتاحي على DOUBLE_ENTREY_VOUCHERS1 بنجاح."
                    };
                }
                catch
                {
                    unitOfWork.Rollback();
                    throw;
                }
            }
        }

        public LCPostingResultViewModel CreateGridVouchers(int tblLcId, int? userId)
        {
            using (var unitOfWork = new MainErpUnitOfWork(_connectionFactory))
            {
                unitOfWork.Begin();
                try
                {
                    var lc = LoadPostingInfo(tblLcId, unitOfWork);
                    if (lc == null)
                    {
                        throw new InvalidOperationException("LC was not found.");
                    }

                    var result = new LCPostingResultViewModel
                    {
                        Success = true,
                        TblLCID = tblLcId,
                        Message = "تم إنشاء قيود جريدات الاعتماد الناقصة."
                    };

                    PostHistoryGridRows(lc, userId, result, unitOfWork);
                    PostMarginGridRows(lc, "TBLLCMargin", 1, userId, result, unitOfWork);
                    PostMarginGridRows(lc, "TBLLCMargin2", 6, userId, result, unitOfWork);
                    PostOpenBalanceGridRows(lc, userId, result, unitOfWork);

                    result.Message = result.VoucherId.HasValue
                        ? result.Message
                        : "لا توجد صفوف جديدة تحتاج إنشاء قيود جريدات.";
                    TryWriteAudit(unitOfWork, "LC.PostGridVouchers", tblLcId, userId, result.Message + " Debit=" + result.Debit.ToString(CultureInfo.InvariantCulture) + "; Credit=" + result.Credit.ToString(CultureInfo.InvariantCulture), null, null);
                    unitOfWork.Commit();
                    return result;
                }
                catch
                {
                    unitOfWork.Rollback();
                    throw;
                }
            }
        }

        public LCPostingResultViewModel RebuildVouchers(int tblLcId, string confirmationText, int? userId)
        {
            var expected = "REBUILD-LC-" + tblLcId.ToString(CultureInfo.InvariantCulture);
            if (!string.Equals(confirmationText, expected, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("نص التأكيد غير صحيح. اكتب: " + expected);
            }

            using (var unitOfWork = new MainErpUnitOfWork(_connectionFactory))
            {
                unitOfWork.Begin();
                try
                {
                    var lc = LoadPostingInfo(tblLcId, unitOfWork);
                    if (lc == null)
                    {
                        throw new InvalidOperationException("LC was not found.");
                    }

                    DeleteAllLcAccounting(tblLcId, unitOfWork, false);
                    using (var command = new SqlCommand(@"
UPDATE TblLC
SET NoteID = NULL, NoteSerial = NULL, NoteIDRowId = NULL,
    NoteIDOpen = NULL, NoteSerialOpen = NULL, NoteIDOpenRowId = NULL,
    NoteID2 = NULL, NoteSerial2 = NULL, NoteID2RowId = NULL,
    opening_balance_voucher_id = NULL
WHERE TblLCID = @TblLCID;", unitOfWork.Connection, unitOfWork.Transaction))
                    {
                        command.Parameters.AddWithValue("@TblLCID", tblLcId);
                        command.ExecuteNonQuery();
                    }

                    TryWriteAudit(unitOfWork, "LC.RebuildCore", tblLcId, userId, "Core LC vouchers were deleted for rebuild. Grid notes were preserved.", "Header notes cleared", null);
                    unitOfWork.Commit();

                    var opening = CreateNormalOpeningVoucher(tblLcId, userId);
                    if ((lc.OpenValue ?? 0m) > 0m)
                    {
                        CreateOpenExpenseVoucher(tblLcId, userId);
                    }

                    if ((lc.OpenBalance ?? 0m) > 0m)
                    {
                        CreateOpeningBalanceVoucher(tblLcId, userId);
                    }

                    return new LCPostingResultViewModel
                    {
                        Success = true,
                        TblLCID = tblLcId,
                        NoteId = opening.NoteId,
                        VoucherId = opening.VoucherId,
                        Debit = opening.Debit,
                        Credit = opening.Credit,
                        Message = "تم حذف وإعادة إنشاء قيود الاعتماد الأساسية والرصيد الافتتاحي. قيود الجريدات لا تزال تتطلب مراجعة مستقلة."
                    };
                }
                catch
                {
                    unitOfWork.Rollback();
                    throw;
                }
            }
        }

        public LCPostingResultViewModel DeleteLc(int tblLcId, string confirmationText, int? userId)
        {
            var expected = "DELETE-LC-" + tblLcId.ToString(CultureInfo.InvariantCulture);
            if (!string.Equals(confirmationText, expected, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("نص التأكيد غير صحيح. اكتب: " + expected);
            }

            using (var unitOfWork = new MainErpUnitOfWork(_connectionFactory))
            {
                unitOfWork.Begin();
                try
                {
                    if (LoadPostingInfo(tblLcId, unitOfWork) == null)
                    {
                        throw new InvalidOperationException("LC was not found.");
                    }

                    DeleteAllLcAccounting(tblLcId, unitOfWork, true);
                    TryWriteAudit(unitOfWork, "LC.Delete", tblLcId, userId, "LC and linked notes/vouchers/grids/accounts deleted.", "Full LC exists before delete", "Deleted");
                    ExecuteNonQuery(unitOfWork, "DELETE FROM TBLLCHistory WHERE TblLCID = @TblLCID;", tblLcId);
                    ExecuteNonQuery(unitOfWork, "DELETE FROM TBLLCMargin WHERE TblLCID = @TblLCID;", tblLcId);
                    ExecuteNonQuery(unitOfWork, "DELETE FROM TBLLCMargin2 WHERE TblLCID = @TblLCID;", tblLcId);
                    ExecuteNonQuery(unitOfWork, "DELETE FROM tblLCOpenB WHERE TblLCID = @TblLCID;", tblLcId);
                    ExecuteNonQuery(unitOfWork, "DELETE FROM ACCOUNTS WHERE TblLCID = @TblLCID;", tblLcId);
                    ExecuteNonQuery(unitOfWork, "DELETE FROM TblLC WHERE TblLCID = @TblLCID;", tblLcId);

                    unitOfWork.Commit();
                    return new LCPostingResultViewModel
                    {
                        Success = true,
                        TblLCID = tblLcId,
                        Message = "تم حذف الاعتماد والقيود والجريدات المرتبطة به من قاعدة الاختبار."
                    };
                }
                catch
                {
                    unitOfWork.Rollback();
                    throw;
                }
            }
        }

        public LCPostingResultViewModel DeleteGridRow(int tblLcId, string sourceTable, int rowId, string confirmationText, int? userId)
        {
            sourceTable = NormalizeLcGridTable(sourceTable);
            var expected = "DELETE-LC-GRID-" + tblLcId.ToString(CultureInfo.InvariantCulture) + "-" + sourceTable + "-" + rowId.ToString(CultureInfo.InvariantCulture);
            if (!string.Equals(confirmationText, expected, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("نص التأكيد غير صحيح. اكتب: " + expected);
            }

            using (var unitOfWork = new MainErpUnitOfWork(_connectionFactory))
            {
                unitOfWork.Begin();
                try
                {
                    if (LoadPostingInfo(tblLcId, unitOfWork) == null)
                    {
                        throw new InvalidOperationException("LC was not found.");
                    }

                    var row = LoadGridRowForDelete(sourceTable, rowId, tblLcId, unitOfWork);
                    if (row == null)
                    {
                        throw new InvalidOperationException("صف الجريد غير موجود أو لا يتبع الاعتماد الحالي.");
                    }

                    var beforeSnapshot = BuildGridDeleteSnapshot(sourceTable, row);
                    DeleteGridRowAccounting(row, unitOfWork);
                    using (var command = new SqlCommand("DELETE FROM " + Bracket(sourceTable) + " WHERE ID = @RowID AND TblLCID = @TblLCID;", unitOfWork.Connection, unitOfWork.Transaction))
                    {
                        command.Parameters.AddWithValue("@RowID", rowId);
                        command.Parameters.AddWithValue("@TblLCID", tblLcId);
                        command.ExecuteNonQuery();
                    }

                    TryWriteAudit(unitOfWork, "LC.DeleteGridRow", tblLcId, userId, "Deleted " + sourceTable + " row #" + rowId.ToString(CultureInfo.InvariantCulture) + " and its row-level accounting only.", beforeSnapshot, "Deleted");
                    unitOfWork.Commit();

                    return new LCPostingResultViewModel
                    {
                        Success = true,
                        TblLCID = tblLcId,
                        Message = "تم حذف صف الجريد وتنظيف قيوده المرتبطة فقط."
                    };
                }
                catch
                {
                    unitOfWork.Rollback();
                    throw;
                }
            }
        }

        public LCPostingResultViewModel RebuildGridVouchers(int tblLcId, string confirmationText, int? userId)
        {
            var expected = "REBUILD-LC-GRIDS-" + tblLcId.ToString(CultureInfo.InvariantCulture);
            if (!string.Equals(confirmationText, expected, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("نص التأكيد غير صحيح. اكتب: " + expected);
            }

            using (var unitOfWork = new MainErpUnitOfWork(_connectionFactory))
            {
                unitOfWork.Begin();
                try
                {
                    var lc = LoadPostingInfo(tblLcId, unitOfWork);
                    if (lc == null)
                    {
                        throw new InvalidOperationException("LC was not found.");
                    }

                    DeleteAllGridAccounting(tblLcId, unitOfWork);
                    ClearAllGridNoteLinks(tblLcId, unitOfWork);

                    var result = new LCPostingResultViewModel
                    {
                        Success = true,
                        TblLCID = tblLcId,
                        Message = "تم حذف وإعادة إنشاء قيود جريدات الاعتماد فقط."
                    };

                    PostHistoryGridRows(lc, userId, result, unitOfWork);
                    PostMarginGridRows(lc, "TBLLCMargin", 1, userId, result, unitOfWork);
                    PostMarginGridRows(lc, "TBLLCMargin2", 6, userId, result, unitOfWork);
                    PostOpenBalanceGridRows(lc, userId, result, unitOfWork);

                    if (!result.VoucherId.HasValue)
                    {
                        result.Message = "تم تنظيف روابط قيود الجريدات، ولا توجد صفوف جريدات حالية تحتاج إنشاء قيود.";
                    }

                    TryWriteAudit(unitOfWork, "LC.RebuildGridVouchers", tblLcId, userId, result.Message + " Debit=" + result.Debit.ToString(CultureInfo.InvariantCulture) + "; Credit=" + result.Credit.ToString(CultureInfo.InvariantCulture), "Grid notes deleted and note fields cleared", "Grid vouchers rebuilt");
                    unitOfWork.Commit();
                    return result;
                }
                catch
                {
                    unitOfWork.Rollback();
                    throw;
                }
            }
        }

        private static string NormalizeLcGridTable(string sourceTable)
        {
            if (string.Equals(sourceTable, "TBLLCHistory", StringComparison.OrdinalIgnoreCase))
            {
                return "TBLLCHistory";
            }

            if (string.Equals(sourceTable, "TBLLCMargin", StringComparison.OrdinalIgnoreCase))
            {
                return "TBLLCMargin";
            }

            if (string.Equals(sourceTable, "TBLLCMargin2", StringComparison.OrdinalIgnoreCase))
            {
                return "TBLLCMargin2";
            }

            if (string.Equals(sourceTable, "tblLCOpenB", StringComparison.OrdinalIgnoreCase))
            {
                return "tblLCOpenB";
            }

            throw new InvalidOperationException("جدول جريد الاعتماد غير مسموح بحذفه من هذه الشاشة.");
        }

        private static LcGridDeleteRow LoadGridRowForDelete(string sourceTable, int rowId, int tblLcId, IMainErpUnitOfWork unitOfWork)
        {
            if (!TableExists(unitOfWork, sourceTable) || !ColumnExists(unitOfWork, sourceTable, "ID") || !ColumnExists(unitOfWork, sourceTable, "TblLCID"))
            {
                throw new InvalidOperationException("جدول الجريد لا يحتوي على مفاتيح حذف آمنة.");
            }

            var hasNoteId = ColumnExists(unitOfWork, sourceTable, "NoteID");
            var hasNoteId2 = ColumnExists(unitOfWork, sourceTable, "NoteID2");
            var hasNoteId3 = ColumnExists(unitOfWork, sourceTable, "NoteID3");
            var hasNoteSerial = ColumnExists(unitOfWork, sourceTable, "NoteSerial");
            var hasAmount = ColumnExists(unitOfWork, sourceTable, "Amount");
            var hasPayedAmount = ColumnExists(unitOfWork, sourceTable, "PayedAmount");
            var hasIsOpenBalance = ColumnExists(unitOfWork, sourceTable, "IsOpenBalance");

            var sql = @"
SELECT TOP 1
    ID,
    TblLCID" +
    (hasNoteId ? ", NoteID" : ", CAST(NULL AS int) AS NoteID") +
    (hasNoteId2 ? ", NoteID2" : ", CAST(NULL AS int) AS NoteID2") +
    (hasNoteId3 ? ", NoteID3" : ", CAST(NULL AS int) AS NoteID3") +
    (hasNoteSerial ? ", NoteSerial" : ", CAST(NULL AS int) AS NoteSerial") +
    (hasAmount ? ", Amount" : ", CAST(NULL AS decimal(18,4)) AS Amount") +
    (hasPayedAmount ? ", PayedAmount" : ", CAST(NULL AS decimal(18,4)) AS PayedAmount") +
    (hasIsOpenBalance ? ", IsOpenBalance" : ", CAST(0 AS bit) AS IsOpenBalance") + @"
FROM " + Bracket(sourceTable) + @"
WHERE ID = @RowID AND TblLCID = @TblLCID;";

            using (var command = new SqlCommand(sql, unitOfWork.Connection, unitOfWork.Transaction))
            {
                command.Parameters.AddWithValue("@RowID", rowId);
                command.Parameters.AddWithValue("@TblLCID", tblLcId);
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    return new LcGridDeleteRow
                    {
                        SourceTable = sourceTable,
                        RowId = ReadInt(reader, "ID").GetValueOrDefault(),
                        TblLcId = ReadInt(reader, "TblLCID").GetValueOrDefault(),
                        NoteId = ReadInt(reader, "NoteID"),
                        NoteId2 = ReadInt(reader, "NoteID2"),
                        NoteId3 = ReadInt(reader, "NoteID3"),
                        NoteSerial = ReadInt(reader, "NoteSerial"),
                        Amount = ReadDecimal(reader, "Amount"),
                        PayedAmount = ReadDecimal(reader, "PayedAmount"),
                        IsOpenBalance = ReadBool(reader, "IsOpenBalance") == true
                    };
                }
            }
        }

        private static void DeleteGridRowAccounting(LcGridDeleteRow row, IMainErpUnitOfWork unitOfWork)
        {
            if (row.NoteId.HasValue && row.NoteId.Value > 0)
            {
                if (row.IsOpenBalance && string.Equals(row.SourceTable, "TBLLCMargin2", StringComparison.OrdinalIgnoreCase))
                {
                    DeleteOpeningVoucherRowsForNote(row.NoteId.Value, unitOfWork);
                    DeleteNoteById("Notes1", row.NoteId.Value, unitOfWork);
                }
                else
                {
                    DeleteVoucherRowsForNote(row.NoteId.Value, unitOfWork);
                    DeleteNoteById("Notes", row.NoteId.Value, unitOfWork);
                }
            }

            DeleteNormalNoteIfPresent(row.NoteId2, unitOfWork);
            DeleteNormalNoteIfPresent(row.NoteId3, unitOfWork);
        }

        private static void DeleteNormalNoteIfPresent(int? noteId, IMainErpUnitOfWork unitOfWork)
        {
            if (!noteId.HasValue || noteId.Value <= 0)
            {
                return;
            }

            DeleteVoucherRowsForNote(noteId.Value, unitOfWork);
            DeleteNoteById("Notes", noteId.Value, unitOfWork);
        }

        private static void DeleteNoteById(string tableName, int noteId, IMainErpUnitOfWork unitOfWork)
        {
            if (!TableExists(unitOfWork, tableName))
            {
                return;
            }

            using (var command = new SqlCommand("DELETE FROM " + Bracket(tableName) + " WHERE NoteID = @NoteID;", unitOfWork.Connection, unitOfWork.Transaction))
            {
                command.Parameters.AddWithValue("@NoteID", noteId);
                command.ExecuteNonQuery();
            }
        }

        private static void DeleteAllGridAccounting(int tblLcId, IMainErpUnitOfWork unitOfWork)
        {
            DeleteGridNormalNoteColumn("TBLLCHistory", "NoteID", null, tblLcId, unitOfWork);
            DeleteGridNormalNoteColumn("TBLLCMargin", "NoteID", null, tblLcId, unitOfWork);
            DeleteGridNormalNoteColumn("TBLLCMargin", "NoteID2", null, tblLcId, unitOfWork);
            DeleteGridNormalNoteColumn("TBLLCMargin", "NoteID3", null, tblLcId, unitOfWork);
            DeleteGridNormalNoteColumn("TBLLCMargin2", "NoteID", "ISNULL(IsOpenBalance, 0) = 0", tblLcId, unitOfWork);
            DeleteGridNormalNoteColumn("TBLLCMargin2", "NoteID2", null, tblLcId, unitOfWork);
            DeleteGridNormalNoteColumn("TBLLCMargin2", "NoteID3", null, tblLcId, unitOfWork);
            DeleteGridOpeningNoteColumn("TBLLCMargin2", "NoteID", "ISNULL(IsOpenBalance, 0) = 1", tblLcId, unitOfWork);
            DeleteGridNormalNoteColumn("tblLCOpenB", "NoteID", null, tblLcId, unitOfWork);
            DeleteGridNormalNoteColumn("tblLCOpenB", "NoteID2", null, tblLcId, unitOfWork);
            DeleteGridNormalNoteColumn("tblLCOpenB", "NoteID3", null, tblLcId, unitOfWork);
        }

        private static void DeleteGridNormalNoteColumn(string tableName, string noteColumn, string extraPredicate, int tblLcId, IMainErpUnitOfWork unitOfWork)
        {
            if (!TableExists(unitOfWork, tableName) || !ColumnExists(unitOfWork, tableName, noteColumn))
            {
                return;
            }

            var predicate = "TblLCID = @TblLCID AND " + Bracket(noteColumn) + " IS NOT NULL AND " + Bracket(noteColumn) + " <> 0"
                + (string.IsNullOrWhiteSpace(extraPredicate) ? string.Empty : " AND " + extraPredicate);
            using (var command = new SqlCommand(@"
DELETE FROM DOUBLE_ENTREY_VOUCHERS
WHERE Notes_ID IN (SELECT " + Bracket(noteColumn) + " FROM " + Bracket(tableName) + " WHERE " + predicate + @");

DELETE FROM Notes
WHERE NoteID IN (SELECT " + Bracket(noteColumn) + " FROM " + Bracket(tableName) + " WHERE " + predicate + @");", unitOfWork.Connection, unitOfWork.Transaction))
            {
                command.Parameters.AddWithValue("@TblLCID", tblLcId);
                command.ExecuteNonQuery();
            }
        }

        private static void DeleteGridOpeningNoteColumn(string tableName, string noteColumn, string extraPredicate, int tblLcId, IMainErpUnitOfWork unitOfWork)
        {
            if (!TableExists(unitOfWork, tableName) || !TableExists(unitOfWork, "Notes1") || !ColumnExists(unitOfWork, tableName, noteColumn))
            {
                return;
            }

            var predicate = "TblLCID = @TblLCID AND " + Bracket(noteColumn) + " IS NOT NULL AND " + Bracket(noteColumn) + " <> 0"
                + (string.IsNullOrWhiteSpace(extraPredicate) ? string.Empty : " AND " + extraPredicate);
            using (var command = new SqlCommand(@"
DELETE FROM DOUBLE_ENTREY_VOUCHERS1
WHERE Notes_ID IN (SELECT " + Bracket(noteColumn) + " FROM " + Bracket(tableName) + " WHERE " + predicate + @");

DELETE FROM Notes1
WHERE NoteID IN (SELECT " + Bracket(noteColumn) + " FROM " + Bracket(tableName) + " WHERE " + predicate + @");", unitOfWork.Connection, unitOfWork.Transaction))
            {
                command.Parameters.AddWithValue("@TblLCID", tblLcId);
                command.ExecuteNonQuery();
            }
        }

        private static void ClearAllGridNoteLinks(int tblLcId, IMainErpUnitOfWork unitOfWork)
        {
            ClearGridNoteLinks("TBLLCHistory", tblLcId, unitOfWork);
            ClearGridNoteLinks("TBLLCMargin", tblLcId, unitOfWork);
            ClearGridNoteLinks("TBLLCMargin2", tblLcId, unitOfWork);
            ClearGridNoteLinks("tblLCOpenB", tblLcId, unitOfWork);
        }

        private static void ClearGridNoteLinks(string tableName, int tblLcId, IMainErpUnitOfWork unitOfWork)
        {
            if (!TableExists(unitOfWork, tableName) || !ColumnExists(unitOfWork, tableName, "TblLCID"))
            {
                return;
            }

            var columns = new[] { "NoteID", "NoteSerial", "NoteIDRowId", "NoteID2", "NoteSerial2", "NoteID2RowId", "NoteID3", "NoteSerial3", "NoteID3RowId", "opening_balance_voucher_id" };
            var assignments = columns
                .Where(column => ColumnExists(unitOfWork, tableName, column))
                .Select(column => Bracket(column) + " = NULL")
                .ToList();
            if (assignments.Count == 0)
            {
                return;
            }

            using (var command = new SqlCommand("UPDATE " + Bracket(tableName) + " SET " + string.Join(", ", assignments) + " WHERE TblLCID = @TblLCID;", unitOfWork.Connection, unitOfWork.Transaction))
            {
                command.Parameters.AddWithValue("@TblLCID", tblLcId);
                command.ExecuteNonQuery();
            }
        }

        private static string BuildGridDeleteSnapshot(string sourceTable, LcGridDeleteRow row)
        {
            return "Table=" + sourceTable
                + "; RowID=" + row.RowId.ToString(CultureInfo.InvariantCulture)
                + "; NoteID=" + (row.NoteId.HasValue ? row.NoteId.Value.ToString(CultureInfo.InvariantCulture) : "")
                + "; NoteID2=" + (row.NoteId2.HasValue ? row.NoteId2.Value.ToString(CultureInfo.InvariantCulture) : "")
                + "; NoteID3=" + (row.NoteId3.HasValue ? row.NoteId3.Value.ToString(CultureInfo.InvariantCulture) : "")
                + "; Amount=" + (row.Amount.HasValue ? row.Amount.Value.ToString(CultureInfo.InvariantCulture) : "")
                + "; PayedAmount=" + (row.PayedAmount.HasValue ? row.PayedAmount.Value.ToString(CultureInfo.InvariantCulture) : "")
                + "; IsOpenBalance=" + row.IsOpenBalance.ToString(CultureInfo.InvariantCulture);
        }

        private static string Bracket(string name)
        {
            return "[" + name.Replace("]", "]]") + "]";
        }

        private static void LoadLookups(SqlConnection connection, LCEditViewModel model)
        {
            TryLoadLookup(connection, model.LcTypes, "SELECT CAST(id AS nvarchar(50)) Value, ISNULL(name, namee) Text FROM LCTypes ORDER BY name;");
            TryLoadLookup(connection, model.Banks, "SELECT CAST(BankID AS nvarchar(50)) Value, ISNULL(BankName, BankNamee) Text FROM BanksData ORDER BY BankName;");
            TryLoadLookup(connection, model.Boxes, "SELECT CAST(BoxID AS nvarchar(50)) Value, ISNULL(BoxName, BoxNameE) Text FROM TblBoxesData ORDER BY BoxName;");
            TryLoadLookup(connection, model.Currencies, "SELECT CAST(id AS nvarchar(50)) Value, ISNULL(name, nameE) Text FROM currency ORDER BY name;");
            TryLoadLookup(connection, model.Countries, "SELECT CAST(CountryID AS nvarchar(50)) Value, ISNULL(CountryName, ECountryName) Text FROM TblCountriesData ORDER BY CountryName;");
            TryLoadLookup(connection, model.Vendors, "SELECT TOP 1000 CAST(CusID AS nvarchar(50)) Value, ISNULL(CusName, CusNamee) Text FROM TblCustemers ORDER BY CusName;");
            TryLoadLookup(connection, model.Branches, "SELECT CAST(branch_id AS nvarchar(50)) Value, ISNULL(branch_name, branch_namee) Text FROM TblBranchesData ORDER BY branch_name;");
            LoadAccountLookup(connection, model);
        }

        private static void LoadAccountLookup(SqlConnection connection, LCEditViewModel model)
        {
            try
            {
                var selectedCodes = CollectSelectedAccountCodes(model)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var sql = @"
WITH AccountSeed AS (
    SELECT Account_Code, Account_Serial, Account_Name, Account_NameEng
    FROM (
        SELECT TOP 220 Account_Code, Account_Serial, Account_Name, Account_NameEng
        FROM ACCOUNTS
        ORDER BY Account_Serial, Account_Name
    ) TopAccounts
    {0}
)
SELECT Account_Code Value,
       ISNULL(NULLIF(Account_Serial, N''), Account_Code) + N' - ' + ISNULL(Account_Name, ISNULL(Account_NameEng, N'')) Text
FROM AccountSeed
GROUP BY Account_Code, Account_Serial, Account_Name, Account_NameEng
ORDER BY Account_Serial, Account_Name;";

                var selectedSql = string.Empty;
                if (selectedCodes.Count > 0)
                {
                    selectedSql = @"
    UNION
    SELECT Account_Code, Account_Serial, Account_Name, Account_NameEng
    FROM ACCOUNTS
    WHERE Account_Code IN (" + string.Join(",", selectedCodes.Select((x, i) => "@AccountCode" + i)) + ")";
                }

                using (var command = new SqlCommand(string.Format(sql, selectedSql), connection))
                {
                    for (var i = 0; i < selectedCodes.Count; i++)
                    {
                        command.Parameters.AddWithValue("@AccountCode" + i, selectedCodes[i]);
                    }

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            model.Accounts.Add(new LCLookupOption
                            {
                                Value = ReadString(reader, "Value"),
                                Text = ReadString(reader, "Text")
                            });
                        }
                    }
                }
            }
            catch (SqlException)
            {
                // Account lookup is intentionally non-fatal because legacy databases can differ.
            }
        }

        private static System.Collections.Generic.IEnumerable<string> CollectSelectedAccountCodes(LCEditViewModel model)
        {
            yield return model.AccountLGParent;
            yield return model.AccountMarginParent;
            yield return model.AccountAcceptanceParent;
            yield return model.AccountExpensParent;
            yield return model.LCAccountCode;
            yield return model.MarginAccountCode;
            yield return model.AcceptanceAccountCode;
            yield return model.ExpenseAccountCode;
            yield return model.ProjectExpenseAccountCode;

            foreach (var row in model.MarginRows)
            {
                if (row == null) continue;
                yield return row.MarginAccountCode;
                yield return row.BankAccountCode;
            }

            foreach (var row in model.Margin2Rows)
            {
                if (row == null) continue;
                yield return row.MarginAccountCode;
                yield return row.BankAccountCode;
                yield return row.AccountMargen2;
            }

            foreach (var row in model.OpenBalanceRows)
            {
                if (row == null) continue;
                yield return row.MarginAccountCode;
                yield return row.BankAccountCode;
            }
        }

        private static void TryLoadLookup(SqlConnection connection, System.Collections.Generic.IList<LCLookupOption> target, string sql)
        {
            try
            {
                using (var command = new SqlCommand(sql, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        target.Add(new LCLookupOption
                        {
                            Value = ReadString(reader, "Value"),
                            Text = ReadString(reader, "Text")
                        });
                    }
                }
            }
            catch (SqlException)
            {
                // Lookup tables differ between legacy databases. The screen falls back to the saved value.
            }
        }

        private static void LoadEditableGridRows(SqlConnection connection, LCEditViewModel model)
        {
            LoadHistoryRows(connection, model);
            LoadMarginRows(connection, model, "TBLLCMargin", model.MarginRows);
            LoadMarginRows(connection, model, "TBLLCMargin2", model.Margin2Rows);
            LoadOpenBalanceRows(connection, model);
            EnsureEditableGridPlaceholders(model);
        }

        private static void EnsureEditableGridPlaceholders(LCEditViewModel model)
        {
            EnsureHistoryPlaceholders(model, 3);
            EnsureMarginPlaceholders(model.MarginRows, 3);
            EnsureMarginPlaceholders(model.Margin2Rows, 3);
            EnsureOpenBalancePlaceholders(model, 3);
        }

        private static void EnsureHistoryPlaceholders(LCEditViewModel model, int minimumBlankRows)
        {
            var blanks = model.HistoryRows.Count(row => row == null || !IsMeaningfulHistory(row));
            for (var i = blanks; i < minimumBlankRows; i++)
            {
                model.HistoryRows.Add(new LcHistoryEditRowViewModel
                {
                    Serial = model.HistoryRows.Count + 1
                });
            }
        }

        private static void EnsureMarginPlaceholders(System.Collections.Generic.IList<LcMarginEditRowViewModel> rows, int minimumBlankRows)
        {
            var blanks = rows.Count(row => row == null || !IsMeaningfulMargin(row));
            for (var i = blanks; i < minimumBlankRows; i++)
            {
                rows.Add(new LcMarginEditRowViewModel
                {
                    Serial = rows.Count + 1
                });
            }
        }

        private static void EnsureOpenBalancePlaceholders(LCEditViewModel model, int minimumBlankRows)
        {
            var blanks = model.OpenBalanceRows.Count(row => row == null || !IsMeaningfulOpenBalance(row));
            for (var i = blanks; i < minimumBlankRows; i++)
            {
                model.OpenBalanceRows.Add(new LcOpenBalanceEditRowViewModel
                {
                    Serial = model.OpenBalanceRows.Count + 1
                });
            }
        }

        private static void LoadHistoryRows(SqlConnection connection, LCEditViewModel model)
        {
            using (var command = new SqlCommand(@"
SELECT ID, serial, GuaranteeAmount, AmountPlus, AmountMin, Total, MarginNo, NoteID, NoteSerial, Code, Name
FROM TBLLCHistory
WHERE TblLCID = @TblLCID
ORDER BY ID;", connection))
            {
                command.Parameters.AddWithValue("@TblLCID", model.TblLCID.Value);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        model.HistoryRows.Add(new LcHistoryEditRowViewModel
                        {
                            ID = ReadInt(reader, "ID"),
                            Serial = ReadInt(reader, "serial"),
                            GuaranteeAmount = ReadDecimal(reader, "GuaranteeAmount"),
                            AmountPlus = ReadDecimal(reader, "AmountPlus"),
                            AmountMin = ReadDecimal(reader, "AmountMin"),
                            Total = ReadDecimal(reader, "Total"),
                            MarginNo = ReadInt(reader, "MarginNo"),
                            NoteID = ReadInt(reader, "NoteID"),
                            NoteSerial = ReadInt(reader, "NoteSerial"),
                            Code = ReadString(reader, "Code"),
                            Name = ReadString(reader, "Name")
                        });
                    }
                }
            }
        }

        private static void LoadMarginRows(SqlConnection connection, LCEditViewModel model, string tableName, System.Collections.Generic.IList<LcMarginEditRowViewModel> target)
        {
            var hasIsOpenBalance = string.Equals(tableName, "TBLLCMargin2", StringComparison.OrdinalIgnoreCase);
            using (var command = new SqlCommand(@"
SELECT ID, serial, MarginNo, GuaranteeDate, Amount, MarginAccountCode, BankAccountCode,
       AccountMargen2, MargenValue, OrderDate, PayDate, Type, PayedAmount, StillAmount,
       NoteID, NoteSerial, NoteID2, NoteSerial2, IsFullPayed, BankAccountCode2,
       " + (hasIsOpenBalance ? "ISNULL(IsOpenBalance, 0)" : "CAST(0 AS bit)") + @" AS IsOpenBalance
FROM " + tableName + @"
WHERE TblLCID = @TblLCID
ORDER BY ID;", connection))
            {
                command.Parameters.AddWithValue("@TblLCID", model.TblLCID.Value);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        target.Add(new LcMarginEditRowViewModel
                        {
                            ID = ReadInt(reader, "ID"),
                            Serial = ReadInt(reader, "serial"),
                            MarginNo = ReadInt(reader, "MarginNo"),
                            GuaranteeDate = ReadDate(reader, "GuaranteeDate"),
                            Amount = ReadDecimal(reader, "Amount"),
                            MarginAccountCode = ReadString(reader, "MarginAccountCode"),
                            BankAccountCode = ReadString(reader, "BankAccountCode"),
                            AccountMargen2 = ReadString(reader, "AccountMargen2"),
                            MargenValue = ReadDecimal(reader, "MargenValue"),
                            OrderDate = ReadDate(reader, "OrderDate"),
                            PayDate = ReadDate(reader, "PayDate"),
                            Type = ReadInt(reader, "Type"),
                            PayedAmount = ReadDecimal(reader, "PayedAmount"),
                            StillAmount = ReadDecimal(reader, "StillAmount"),
                            NoteID = ReadInt(reader, "NoteID"),
                            NoteSerial = ReadInt(reader, "NoteSerial"),
                            NoteID2 = ReadInt(reader, "NoteID2"),
                            NoteSerial2 = ReadInt(reader, "NoteSerial2"),
                            IsFullPayed = ReadBool(reader, "IsFullPayed") == true,
                            BankAccountCode2 = ReadString(reader, "BankAccountCode2"),
                            IsOpenBalance = ReadBool(reader, "IsOpenBalance") == true
                        });
                    }
                }
            }
        }

        private static void LoadOpenBalanceRows(SqlConnection connection, LCEditViewModel model)
        {
            using (var command = new SqlCommand(@"
SELECT ID, serial, MarginNo, GuaranteeDate, Amount, AmountP, TotalAmount, ExpAmount,
       InsuranceAmount, PercentA, MarginAccountCode, BankAccountCode, PayDate,
       NoteID2, NoteSerial2, Type, NoteID, NoteSerial, IsFullPayed
FROM tblLCOpenB
WHERE TblLCID = @TblLCID
ORDER BY ID;", connection))
            {
                command.Parameters.AddWithValue("@TblLCID", model.TblLCID.Value);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        model.OpenBalanceRows.Add(new LcOpenBalanceEditRowViewModel
                        {
                            ID = ReadInt(reader, "ID"),
                            Serial = ReadInt(reader, "serial"),
                            MarginNo = ReadInt(reader, "MarginNo"),
                            GuaranteeDate = ReadDate(reader, "GuaranteeDate"),
                            Amount = ReadDecimal(reader, "Amount"),
                            AmountP = ReadDecimal(reader, "AmountP"),
                            TotalAmount = ReadDecimal(reader, "TotalAmount"),
                            ExpAmount = ReadDecimal(reader, "ExpAmount"),
                            InsuranceAmount = ReadDecimal(reader, "InsuranceAmount"),
                            PercentA = ReadDecimal(reader, "PercentA"),
                            MarginAccountCode = ReadString(reader, "MarginAccountCode"),
                            BankAccountCode = ReadString(reader, "BankAccountCode"),
                            PayDate = ReadDate(reader, "PayDate"),
                            NoteID2 = ReadInt(reader, "NoteID2"),
                            NoteSerial2 = ReadInt(reader, "NoteSerial2"),
                            Type = ReadInt(reader, "Type"),
                            NoteID = ReadInt(reader, "NoteID"),
                            NoteSerial = ReadInt(reader, "NoteSerial"),
                            IsFullPayed = ReadBool(reader, "IsFullPayed") == true
                        });
                    }
                }
            }
        }

        private static void SaveEditableGridRows(LCEditViewModel model, IMainErpUnitOfWork unitOfWork)
        {
            SaveHistoryRows(model, unitOfWork);
            SaveMarginRows(model, unitOfWork, "TBLLCMargin", model.MarginRows, false);
            SaveMarginRows(model, unitOfWork, "TBLLCMargin2", model.Margin2Rows, true);
            SaveOpenBalanceRows(model, unitOfWork);
        }

        private static void SaveHistoryRows(LCEditViewModel model, IMainErpUnitOfWork unitOfWork)
        {
            foreach (var row in model.HistoryRows)
            {
                if (row == null || !IsMeaningfulHistory(row)) continue;
                using (var command = new SqlCommand(@"
IF @ID IS NOT NULL AND EXISTS (SELECT 1 FROM TBLLCHistory WHERE ID = @ID AND TblLCID = @TblLCID)
BEGIN
    UPDATE TBLLCHistory SET serial=@Serial, GuaranteeAmount=@GuaranteeAmount, AmountPlus=@AmountPlus,
        AmountMin=@AmountMin, Total=@Total, MarginNo=@MarginNo, Code=@Code, Name=@Name
    WHERE ID=@ID AND TblLCID=@TblLCID;
END
ELSE
BEGIN
    INSERT INTO TBLLCHistory (TblLCID, serial, GuaranteeAmount, AmountPlus, AmountMin, Total, MarginNo, Code, Name)
    VALUES (@TblLCID, @Serial, @GuaranteeAmount, @AmountPlus, @AmountMin, @Total, @MarginNo, @Code, @Name);
END", unitOfWork.Connection, unitOfWork.Transaction))
                {
                    command.Parameters.AddWithValue("@ID", (object)row.ID ?? DBNull.Value);
                    command.Parameters.AddWithValue("@TblLCID", model.TblLCID.Value);
                    command.Parameters.AddWithValue("@Serial", (object)row.Serial ?? DBNull.Value);
                    command.Parameters.AddWithValue("@GuaranteeAmount", (object)row.GuaranteeAmount ?? DBNull.Value);
                    command.Parameters.AddWithValue("@AmountPlus", (object)row.AmountPlus ?? DBNull.Value);
                    command.Parameters.AddWithValue("@AmountMin", (object)row.AmountMin ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Total", (object)row.Total ?? DBNull.Value);
                    command.Parameters.AddWithValue("@MarginNo", (object)row.MarginNo ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Code", (object)row.Code ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Name", (object)row.Name ?? DBNull.Value);
                    command.ExecuteNonQuery();
                }
            }
        }

        private static void SaveMarginRows(LCEditViewModel model, IMainErpUnitOfWork unitOfWork, string tableName, System.Collections.Generic.IEnumerable<LcMarginEditRowViewModel> rows, bool hasIsOpenBalance)
        {
            foreach (var row in rows)
            {
                if (row == null || !IsMeaningfulMargin(row)) continue;
                var sql = @"
IF @ID IS NOT NULL AND EXISTS (SELECT 1 FROM " + tableName + @" WHERE ID = @ID AND TblLCID = @TblLCID)
BEGIN
    UPDATE " + tableName + @" SET serial=@Serial, MarginNo=@MarginNo, GuaranteeDate=@GuaranteeDate,
        Amount=@Amount, MarginAccountCode=@MarginAccountCode, BankAccountCode=@BankAccountCode,
        AccountMargen2=@AccountMargen2, MargenValue=@MargenValue, OrderDate=@OrderDate, PayDate=@PayDate,
        Type=@Type, PayedAmount=@PayedAmount, StillAmount=@StillAmount, IsFullPayed=@IsFullPayed,
        BankAccountCode2=@BankAccountCode2" + (hasIsOpenBalance ? ", IsOpenBalance=@IsOpenBalance" : "") + @"
    WHERE ID=@ID AND TblLCID=@TblLCID;
END
ELSE
BEGIN
    INSERT INTO " + tableName + @" (TblLCID, serial, MarginNo, GuaranteeDate, Amount, MarginAccountCode,
        BankAccountCode, AccountMargen2, MargenValue, OrderDate, PayDate, Type, PayedAmount, StillAmount,
        IsFullPayed, BankAccountCode2" + (hasIsOpenBalance ? ", IsOpenBalance" : "") + @", RowId)
    VALUES (@TblLCID, @Serial, @MarginNo, @GuaranteeDate, @Amount, @MarginAccountCode,
        @BankAccountCode, @AccountMargen2, @MargenValue, @OrderDate, @PayDate, @Type, @PayedAmount, @StillAmount,
        @IsFullPayed, @BankAccountCode2" + (hasIsOpenBalance ? ", @IsOpenBalance" : "") + @", NEWID());
END";
                using (var command = new SqlCommand(sql, unitOfWork.Connection, unitOfWork.Transaction))
                {
                    AddMarginParameters(command, model, row, hasIsOpenBalance);
                    command.ExecuteNonQuery();
                }
            }
        }

        private static void AddMarginParameters(SqlCommand command, LCEditViewModel model, LcMarginEditRowViewModel row, bool includeOpenBalance)
        {
            command.Parameters.AddWithValue("@ID", (object)row.ID ?? DBNull.Value);
            command.Parameters.AddWithValue("@TblLCID", model.TblLCID.Value);
            command.Parameters.AddWithValue("@Serial", (object)row.Serial ?? DBNull.Value);
            command.Parameters.AddWithValue("@MarginNo", (object)row.MarginNo ?? DBNull.Value);
            command.Parameters.AddWithValue("@GuaranteeDate", (object)row.GuaranteeDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@Amount", (object)row.Amount ?? DBNull.Value);
            command.Parameters.AddWithValue("@MarginAccountCode", (object)row.MarginAccountCode ?? DBNull.Value);
            command.Parameters.AddWithValue("@BankAccountCode", (object)row.BankAccountCode ?? DBNull.Value);
            command.Parameters.AddWithValue("@AccountMargen2", (object)row.AccountMargen2 ?? DBNull.Value);
            command.Parameters.AddWithValue("@MargenValue", (object)row.MargenValue ?? DBNull.Value);
            command.Parameters.AddWithValue("@OrderDate", (object)row.OrderDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@PayDate", (object)row.PayDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@Type", (object)row.Type ?? DBNull.Value);
            command.Parameters.AddWithValue("@PayedAmount", (object)row.PayedAmount ?? DBNull.Value);
            command.Parameters.AddWithValue("@StillAmount", (object)row.StillAmount ?? DBNull.Value);
            command.Parameters.AddWithValue("@IsFullPayed", row.IsFullPayed);
            command.Parameters.AddWithValue("@BankAccountCode2", (object)row.BankAccountCode2 ?? DBNull.Value);
            if (includeOpenBalance) command.Parameters.AddWithValue("@IsOpenBalance", row.IsOpenBalance);
        }

        private static void SaveOpenBalanceRows(LCEditViewModel model, IMainErpUnitOfWork unitOfWork)
        {
            foreach (var row in model.OpenBalanceRows)
            {
                if (row == null || !IsMeaningfulOpenBalance(row)) continue;
                using (var command = new SqlCommand(@"
IF @ID IS NOT NULL AND EXISTS (SELECT 1 FROM tblLCOpenB WHERE ID = @ID AND TblLCID = @TblLCID)
BEGIN
    UPDATE tblLCOpenB SET serial=@Serial, MarginNo=@MarginNo, GuaranteeDate=@GuaranteeDate,
        Amount=@Amount, AmountP=@AmountP, TotalAmount=@TotalAmount, ExpAmount=@ExpAmount,
        InsuranceAmount=@InsuranceAmount, PercentA=@PercentA, MarginAccountCode=@MarginAccountCode,
        BankAccountCode=@BankAccountCode, PayDate=@PayDate, Type=@Type, IsFullPayed=@IsFullPayed
    WHERE ID=@ID AND TblLCID=@TblLCID;
END
ELSE
BEGIN
    INSERT INTO tblLCOpenB (TblLCID, serial, MarginNo, GuaranteeDate, Amount, AmountP, TotalAmount,
        ExpAmount, InsuranceAmount, PercentA, MarginAccountCode, BankAccountCode, PayDate, Type, IsFullPayed, RowId)
    VALUES (@TblLCID, @Serial, @MarginNo, @GuaranteeDate, @Amount, @AmountP, @TotalAmount,
        @ExpAmount, @InsuranceAmount, @PercentA, @MarginAccountCode, @BankAccountCode, @PayDate, @Type, @IsFullPayed, NEWID());
END", unitOfWork.Connection, unitOfWork.Transaction))
                {
                    command.Parameters.AddWithValue("@ID", (object)row.ID ?? DBNull.Value);
                    command.Parameters.AddWithValue("@TblLCID", model.TblLCID.Value);
                    command.Parameters.AddWithValue("@Serial", (object)row.Serial ?? DBNull.Value);
                    command.Parameters.AddWithValue("@MarginNo", (object)row.MarginNo ?? DBNull.Value);
                    command.Parameters.AddWithValue("@GuaranteeDate", (object)row.GuaranteeDate ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Amount", (object)row.Amount ?? DBNull.Value);
                    command.Parameters.AddWithValue("@AmountP", (object)row.AmountP ?? DBNull.Value);
                    command.Parameters.AddWithValue("@TotalAmount", (object)row.TotalAmount ?? DBNull.Value);
                    command.Parameters.AddWithValue("@ExpAmount", (object)row.ExpAmount ?? DBNull.Value);
                    command.Parameters.AddWithValue("@InsuranceAmount", (object)row.InsuranceAmount ?? DBNull.Value);
                    command.Parameters.AddWithValue("@PercentA", (object)row.PercentA ?? DBNull.Value);
                    command.Parameters.AddWithValue("@MarginAccountCode", (object)row.MarginAccountCode ?? DBNull.Value);
                    command.Parameters.AddWithValue("@BankAccountCode", (object)row.BankAccountCode ?? DBNull.Value);
                    command.Parameters.AddWithValue("@PayDate", (object)row.PayDate ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Type", (object)row.Type ?? DBNull.Value);
                    command.Parameters.AddWithValue("@IsFullPayed", row.IsFullPayed);
                    command.ExecuteNonQuery();
                }
            }
        }

        private void PostHistoryGridRows(LcPostingInfo lc, int? userId, LCPostingResultViewModel result, IMainErpUnitOfWork unitOfWork)
        {
            var rows = new System.Collections.Generic.List<LcHistoryPostRow>();
            using (var command = new SqlCommand(@"
SELECT ID, AmountPlus, AmountMin, NoteID, NoteSerial
FROM TBLLCHistory
WHERE TblLCID = @TblLCID
ORDER BY ID;", unitOfWork.Connection, unitOfWork.Transaction))
            {
                command.Parameters.AddWithValue("@TblLCID", lc.TblLCID);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new LcHistoryPostRow
                        {
                            ID = ReadInt(reader, "ID").GetValueOrDefault(),
                            AmountPlus = ReadDecimal(reader, "AmountPlus") ?? 0m,
                            AmountMin = ReadDecimal(reader, "AmountMin") ?? 0m,
                            NoteID = ReadInt(reader, "NoteID"),
                            NoteSerial = ReadInt(reader, "NoteSerial")
                        });
                    }
                }
            }

            foreach (var row in rows)
            {
                var noteValue = Math.Round((row.AmountPlus - row.AmountMin) * (lc.PercentV ?? 0m) / 100m, 4);
                if (noteValue == 0m || HasVoucherRowsForOptionalNote(row.NoteID, false, unitOfWork))
                {
                    continue;
                }

                var marginAccount = lc.MarginAccountCode;
                var bankAccount = GetBankAccount(lc.BankId, unitOfWork);
                if (string.IsNullOrWhiteSpace(marginAccount) || string.IsNullOrWhiteSpace(bankAccount))
                {
                    throw new InvalidOperationException("بيانات حسابات تاريخ الاعتماد غير مكتملة.");
                }

                var noteDate = DateTime.Today;
                var noteId = row.NoteID.GetValueOrDefault();
                if (noteId == 0)
                {
                    noteId = Convert.ToInt32(_manualIdGenerator.Allocate(ManualIdTarget.NotesNoteId, unitOfWork).Value);
                }

                var noteSerial = row.NoteSerial.GetValueOrDefault();
                if (noteSerial == 0)
                {
                    noteSerial = AllocateNoteSerial(22004, noteDate, unitOfWork);
                }

                var voucherId = Convert.ToInt64(_manualIdGenerator.Allocate(ManualIdTarget.VoucherId, unitOfWork).Value);
                var amount = Math.Abs(noteValue);
                UpsertGridNote(lc, "TBLLCHistory", row.ID, false, false, noteId, noteSerial, 22004, amount, voucherId, Guid.NewGuid(), userId, noteDate, unitOfWork);
                DeleteVoucherRowsForNote(noteId, unitOfWork);

                var debitAccount = noteValue < 0m ? bankAccount : marginAccount;
                var creditAccount = noteValue < 0m ? marginAccount : bankAccount;
                InsertGridVoucherRow(voucherId, 1, debitAccount, amount, 0, "LC " + lc.LCNO + " history margin", noteDate, noteId, userId, lc.BranchId, false, voucherId, unitOfWork);
                InsertGridVoucherRow(voucherId, 2, creditAccount, amount, 1, "LC " + lc.LCNO + " history bank", noteDate, noteId, userId, lc.BranchId, false, voucherId, unitOfWork);

                result.VoucherId = voucherId;
                result.NoteId = noteId;
                result.Debit += amount;
                result.Credit += amount;
            }
        }

        private void PostMarginGridRows(LcPostingInfo lc, string tableName, int typeGrid, int? userId, LCPostingResultViewModel result, IMainErpUnitOfWork unitOfWork)
        {
            var rows = new System.Collections.Generic.List<LcMarginPostRow>();
            var isMargin2 = string.Equals(tableName, "TBLLCMargin2", StringComparison.OrdinalIgnoreCase);
            using (var command = new SqlCommand(@"
SELECT ID, Amount, PayedAmount, MargenValue, MarginAccountCode, BankAccountCode,
       BankAccountCode2, AccountMargen2, OrderDate, PayDate, NoteID, NoteSerial,
       NoteID2, NoteSerial2, " + (isMargin2 ? "ISNULL(IsOpenBalance, 0)" : "CAST(0 AS bit)") + @" AS IsOpenBalance
FROM " + tableName + @"
WHERE TblLCID = @TblLCID
ORDER BY ID;", unitOfWork.Connection, unitOfWork.Transaction))
            {
                command.Parameters.AddWithValue("@TblLCID", lc.TblLCID);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new LcMarginPostRow
                        {
                            ID = ReadInt(reader, "ID").GetValueOrDefault(),
                            Amount = ReadDecimal(reader, "Amount") ?? 0m,
                            PayedAmount = ReadDecimal(reader, "PayedAmount") ?? 0m,
                            MargenValue = ReadDecimal(reader, "MargenValue") ?? 0m,
                            MarginAccountCode = ReadString(reader, "MarginAccountCode"),
                            BankAccountCode = ReadString(reader, "BankAccountCode"),
                            BankAccountCode2 = ReadString(reader, "BankAccountCode2"),
                            AccountMargen2 = ReadString(reader, "AccountMargen2"),
                            OrderDate = ReadDate(reader, "OrderDate"),
                            PayDate = ReadDate(reader, "PayDate"),
                            NoteID = ReadInt(reader, "NoteID"),
                            NoteSerial = ReadInt(reader, "NoteSerial"),
                            NoteID2 = ReadInt(reader, "NoteID2"),
                            NoteSerial2 = ReadInt(reader, "NoteSerial2"),
                            IsOpenBalance = ReadBool(reader, "IsOpenBalance") == true
                        });
                    }
                }
            }

            foreach (var row in rows)
            {
                if (row.Amount != 0m && !HasVoucherRowsForOptionalNote(row.NoteID, row.IsOpenBalance, unitOfWork))
                {
                    PostMarginGridEntry(lc, row, tableName, typeGrid, false, userId, result, unitOfWork);
                }

                if (row.PayedAmount != 0m && !HasVoucherRowsForOptionalNote(row.NoteID2, false, unitOfWork))
                {
                    PostMarginGridEntry(lc, row, tableName, typeGrid, true, userId, result, unitOfWork);
                }
            }
        }

        private void PostMarginGridEntry(LcPostingInfo lc, LcMarginPostRow row, string tableName, int typeGrid, bool isPay, int? userId, LCPostingResultViewModel result, IMainErpUnitOfWork unitOfWork)
        {
            var bankAccount = FirstText(row.BankAccountCode, GetBankAccount(lc.BankId, unitOfWork));
            var bankAccount2 = FirstText(row.BankAccountCode2, typeGrid == 6 ? GetBankAccount(lc.BankId, unitOfWork) : row.MarginAccountCode);
            var isOpening = typeGrid == 6 && row.IsOpenBalance && !isPay;
            var noteType = isOpening ? 101 : typeGrid == 6 ? (isPay ? 22009 : 22008) : (isPay ? 22003 : 22002);
            var noteDate = isPay ? (row.PayDate ?? lc.FromDate ?? DateTime.Today) : (row.OrderDate ?? lc.FromDate ?? DateTime.Today);
            var noteValue = Math.Round((isPay ? row.PayedAmount : row.Amount) * (lc.CurrencyRate ?? 1m), 4);
            if (noteValue == 0m)
            {
                return;
            }

            var marginAccount = row.MarginAccountCode;
            if (string.IsNullOrWhiteSpace(marginAccount))
            {
                throw new InvalidOperationException("يوجد صف هامش اعتماد بدون حساب هامش.");
            }

            if (string.IsNullOrWhiteSpace(bankAccount))
            {
                throw new InvalidOperationException("يوجد صف هامش اعتماد بدون حساب بنك.");
            }

            var noteId = isPay
                ? row.NoteID2.GetValueOrDefault()
                : row.NoteID.GetValueOrDefault();
            if (noteId == 0)
            {
                noteId = Convert.ToInt32(_manualIdGenerator.Allocate(isOpening ? ManualIdTarget.Notes1NoteId : ManualIdTarget.NotesNoteId, unitOfWork).Value);
            }

            var noteSerial = isPay
                ? row.NoteSerial2.GetValueOrDefault()
                : row.NoteSerial.GetValueOrDefault();
            if (noteSerial == 0)
            {
                noteSerial = isOpening
                    ? AllocateOpeningNoteSerial(noteType, noteDate, unitOfWork)
                    : AllocateNoteSerial(noteType, noteDate, unitOfWork);
            }

            var voucherId = Convert.ToInt64(_manualIdGenerator.Allocate(isOpening ? ManualIdTarget.OpeningBalanceVoucherId : ManualIdTarget.VoucherId, unitOfWork).Value);
            var rowId = Guid.NewGuid();
            UpsertGridNote(lc, tableName, row.ID, isPay, isOpening, noteId, noteSerial, noteType, Math.Abs(noteValue), voucherId, rowId, userId, noteDate, unitOfWork);

            if (isOpening)
            {
                DeleteOpeningVoucherRowsForNote(noteId, unitOfWork);
            }
            else
            {
                DeleteVoucherRowsForNote(noteId, unitOfWork);
            }

            var debitAccount = isPay ? bankAccount : marginAccount;
            var creditAccount = isPay ? bankAccount2 : bankAccount;
            InsertGridVoucherRow(voucherId, 1, debitAccount, Math.Abs(noteValue), 0, "LC " + lc.LCNO + " grid financing", noteDate, noteId, userId, lc.BranchId, isOpening, voucherId, unitOfWork);
            InsertGridVoucherRow(voucherId, 2, creditAccount, Math.Abs(noteValue), 1, "LC " + lc.LCNO + " grid bank", noteDate, noteId, userId, lc.BranchId, isOpening, voucherId, unitOfWork);

            if (!isPay && row.MargenValue != 0m && !string.IsNullOrWhiteSpace(row.AccountMargen2) && !string.IsNullOrWhiteSpace(bankAccount2))
            {
                var marginValue = Math.Abs(row.MargenValue);
                InsertGridVoucherRow(voucherId, 3, row.AccountMargen2, marginValue, 0, "LC " + lc.LCNO + " margin value", noteDate, noteId, userId, lc.BranchId, isOpening, voucherId, unitOfWork);
                InsertGridVoucherRow(voucherId, 4, bankAccount2, marginValue, 1, "LC " + lc.LCNO + " margin bank", noteDate, noteId, userId, lc.BranchId, isOpening, voucherId, unitOfWork);
                result.Debit += marginValue;
                result.Credit += marginValue;
            }

            result.VoucherId = voucherId;
            result.NoteId = noteId;
            result.Debit += Math.Abs(noteValue);
            result.Credit += Math.Abs(noteValue);
        }

        private void PostOpenBalanceGridRows(LcPostingInfo lc, int? userId, LCPostingResultViewModel result, IMainErpUnitOfWork unitOfWork)
        {
            var rows = new System.Collections.Generic.List<LcOpenBalancePostRow>();
            using (var command = new SqlCommand(@"
SELECT ID, GuaranteeDate, InsuranceAmount, ExpAmount, MarginAccountCode, BankAccountCode,
       NoteID, NoteSerial
FROM tblLCOpenB
WHERE TblLCID = @TblLCID
ORDER BY ID;", unitOfWork.Connection, unitOfWork.Transaction))
            {
                command.Parameters.AddWithValue("@TblLCID", lc.TblLCID);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new LcOpenBalancePostRow
                        {
                            ID = ReadInt(reader, "ID").GetValueOrDefault(),
                            GuaranteeDate = ReadDate(reader, "GuaranteeDate"),
                            InsuranceAmount = ReadDecimal(reader, "InsuranceAmount") ?? 0m,
                            ExpAmount = ReadDecimal(reader, "ExpAmount") ?? 0m,
                            MarginAccountCode = ReadString(reader, "MarginAccountCode"),
                            BankAccountCode = ReadString(reader, "BankAccountCode"),
                            NoteID = ReadInt(reader, "NoteID"),
                            NoteSerial = ReadInt(reader, "NoteSerial")
                        });
                    }
                }
            }

            foreach (var row in rows)
            {
                if ((row.InsuranceAmount + row.ExpAmount) == 0m || HasVoucherRowsForOptionalNote(row.NoteID, false, unitOfWork))
                {
                    continue;
                }

                var bankAccount = FirstText(row.BankAccountCode, GetBankAccount(lc.BankId, unitOfWork));
                var marginAccount = FirstText(row.MarginAccountCode, lc.MarginAccountCode);
                var expenseAccount = lc.ExpenseAccountCode;
                if (string.IsNullOrWhiteSpace(bankAccount) || string.IsNullOrWhiteSpace(marginAccount) || string.IsNullOrWhiteSpace(expenseAccount))
                {
                    throw new InvalidOperationException("بيانات حسابات الرصيد الافتتاحي التفصيلي غير مكتملة.");
                }

                var noteDate = row.GuaranteeDate ?? lc.FromDate ?? DateTime.Today;
                var expenseGross = Math.Abs(row.ExpAmount * (lc.CurrencyRate ?? 1m));
                var total = Math.Round(Math.Abs(row.InsuranceAmount) + expenseGross, 4);
                var noteId = row.NoteID.GetValueOrDefault();
                if (noteId == 0)
                {
                    noteId = Convert.ToInt32(_manualIdGenerator.Allocate(ManualIdTarget.NotesNoteId, unitOfWork).Value);
                }

                var noteSerial = row.NoteSerial.GetValueOrDefault();
                if (noteSerial == 0)
                {
                    noteSerial = AllocateNoteSerial(22006, noteDate, unitOfWork);
                }

                var voucherId = Convert.ToInt64(_manualIdGenerator.Allocate(ManualIdTarget.VoucherId, unitOfWork).Value);
                UpsertGridNote(lc, "tblLCOpenB", row.ID, false, false, noteId, noteSerial, 22006, total, voucherId, Guid.NewGuid(), userId, noteDate, unitOfWork);
                DeleteVoucherRowsForNote(noteId, unitOfWork);

                var lineNo = 1;
                if (row.InsuranceAmount != 0m)
                {
                    InsertVoucherRow(voucherId, lineNo++, marginAccount, Math.Abs(row.InsuranceAmount), 0, "LC " + lc.LCNO + " insurance", noteDate, noteId, userId, lc.BranchId, unitOfWork);
                }

                if (row.ExpAmount != 0m)
                {
                    var vatPercent = FindVatPercent(unitOfWork);
                    var gross = expenseGross;
                    var net = Math.Round(gross / (1m + vatPercent / 100m), 4);
                    var vat = Math.Round(gross - net, 4);
                    var vatAccount = vat == 0m ? null : FindVatInputAccount(unitOfWork);
                    if (vat != 0m && string.IsNullOrWhiteSpace(vatAccount))
                    {
                        throw new InvalidOperationException("لم يتم العثور على حساب ضريبة مدخلات لصف الرصيد الافتتاحي.");
                    }

                    if (vat != 0m)
                    {
                        InsertVoucherRow(voucherId, lineNo++, vatAccount, vat, 0, "LC " + lc.LCNO + " VAT", noteDate, noteId, userId, lc.BranchId, unitOfWork);
                    }
                    InsertVoucherRow(voucherId, lineNo++, expenseAccount, net, 0, "LC " + lc.LCNO + " expenses", noteDate, noteId, userId, lc.BranchId, unitOfWork);
                }

                InsertVoucherRow(voucherId, lineNo, bankAccount, total, 1, "LC " + lc.LCNO + " bank", noteDate, noteId, userId, lc.BranchId, unitOfWork);
                result.VoucherId = voucherId;
                result.NoteId = noteId;
                result.Debit += total;
                result.Credit += total;
            }
        }

        private static bool IsMeaningfulHistory(LcHistoryEditRowViewModel row)
        {
            return row.ID.HasValue || row.GuaranteeAmount.HasValue || row.AmountPlus.HasValue || row.AmountMin.HasValue || row.Total.HasValue || !string.IsNullOrWhiteSpace(row.Code) || !string.IsNullOrWhiteSpace(row.Name);
        }

        private static bool IsMeaningfulMargin(LcMarginEditRowViewModel row)
        {
            return row.ID.HasValue || row.Amount.HasValue || row.PayedAmount.HasValue || row.StillAmount.HasValue || row.MarginNo.HasValue || !string.IsNullOrWhiteSpace(row.MarginAccountCode) || !string.IsNullOrWhiteSpace(row.BankAccountCode);
        }

        private static bool IsMeaningfulOpenBalance(LcOpenBalanceEditRowViewModel row)
        {
            return row.ID.HasValue || row.Amount.HasValue || row.AmountP.HasValue || row.TotalAmount.HasValue || row.ExpAmount.HasValue || row.InsuranceAmount.HasValue || row.MarginNo.HasValue;
        }

        private static void Validate(LCEditViewModel model)
        {
            if (model == null) throw new ArgumentNullException("model");
            if (string.IsNullOrWhiteSpace(model.LCNO)) throw new InvalidOperationException("رقم الاعتماد مطلوب.");
            if (!model.LCTyperId.HasValue) throw new InvalidOperationException("نوع الاعتماد مطلوب.");
            if (!model.BankId.HasValue) throw new InvalidOperationException("البنك مطلوب.");
            if (!model.BranchID.HasValue) throw new InvalidOperationException("الفرع مطلوب.");
            if (!model.CurrencyId.HasValue) throw new InvalidOperationException("العملة مطلوبة.");
            if (!model.CurrencyRate.HasValue || model.CurrencyRate.Value <= 0m) throw new InvalidOperationException("سعر الصرف يجب أن يكون أكبر من صفر.");
            if (!model.Value.HasValue || model.Value.Value < 0m) throw new InvalidOperationException("قيمة الاعتماد غير صحيحة.");
            if (model.OpenValue.HasValue && model.OpenValue.Value < 0m) throw new InvalidOperationException("قيمة مصاريف/فتح الاعتماد غير صحيحة.");
            if (model.FromDate.HasValue && model.ToDate.HasValue && model.FromDate.Value > model.ToDate.Value) throw new InvalidOperationException("تاريخ البداية أكبر من تاريخ النهاية.");
            if (string.IsNullOrWhiteSpace(model.AccountMarginParent) && string.IsNullOrWhiteSpace(model.MarginAccountCode)) throw new InvalidOperationException("حساب/أب الهامش مطلوب.");
            if (string.IsNullOrWhiteSpace(model.AccountExpensParent) && string.IsNullOrWhiteSpace(model.ExpenseAccountCode)) throw new InvalidOperationException("حساب/أب المصروفات مطلوب.");
        }

        private void EnsureLcAccounts(LCEditViewModel model, IMainErpUnitOfWork unitOfWork, int? userId)
        {
            if (string.IsNullOrWhiteSpace(model.LCAccountCode))
            {
                model.LCAccountCode = CreateChildAccount(model.AccountLGParent, "LC " + model.LCNO, model.TblLCID.Value, model.BranchID, userId, unitOfWork);
            }

            if (string.IsNullOrWhiteSpace(model.MarginAccountCode))
            {
                model.MarginAccountCode = CreateChildAccount(model.AccountMarginParent, "LC Margin " + model.LCNO, model.TblLCID.Value, model.BranchID, userId, unitOfWork);
            }

            if (string.IsNullOrWhiteSpace(model.AcceptanceAccountCode) && !string.IsNullOrWhiteSpace(model.AccountAcceptanceParent))
            {
                model.AcceptanceAccountCode = CreateChildAccount(model.AccountAcceptanceParent, "LC Acceptance " + model.LCNO, model.TblLCID.Value, model.BranchID, userId, unitOfWork);
            }

            if (string.IsNullOrWhiteSpace(model.ExpenseAccountCode))
            {
                model.ExpenseAccountCode = CreateChildAccount(model.AccountExpensParent, "LC Expenses " + model.LCNO, model.TblLCID.Value, model.BranchID, userId, unitOfWork);
            }
        }

        private string CreateChildAccount(string parentCode, string accountName, int tblLcId, int? branchId, int? userId, IMainErpUnitOfWork unitOfWork)
        {
            if (string.IsNullOrWhiteSpace(parentCode))
            {
                return null;
            }

            using (var lockCommand = new SqlCommand("sp_getapplock", unitOfWork.Connection, unitOfWork.Transaction))
            {
                lockCommand.CommandType = CommandType.StoredProcedure;
                lockCommand.Parameters.AddWithValue("@Resource", "MainErp.LC.Account." + parentCode);
                lockCommand.Parameters.AddWithValue("@LockMode", "Exclusive");
                lockCommand.Parameters.AddWithValue("@LockOwner", "Transaction");
                lockCommand.Parameters.AddWithValue("@LockTimeout", 15000);
                var returnValue = lockCommand.Parameters.Add("@ReturnValue", SqlDbType.Int);
                returnValue.Direction = ParameterDirection.ReturnValue;
                lockCommand.ExecuteNonQuery();
                if (Convert.ToInt32(returnValue.Value) < 0)
                {
                    throw new TimeoutException("Could not acquire account generation lock.");
                }
            }

            AccountRow parent;
            using (var command = new SqlCommand(@"
SELECT TOP 1 Account_Code, Account_Serial, Account_Name, Account_NameEng, last_account, Level
FROM ACCOUNTS
WHERE Account_Code = @Parent;", unitOfWork.Connection, unitOfWork.Transaction))
            {
                command.Parameters.AddWithValue("@Parent", parentCode);
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        throw new InvalidOperationException("الحساب الأب غير موجود: " + parentCode);
                    }

                    parent = new AccountRow
                    {
                        AccountCode = ReadString(reader, "Account_Code"),
                        AccountSerial = ReadString(reader, "Account_Serial"),
                        LastAccount = ReadBool(reader, "last_account") == true,
                        Level = ReadInt(reader, "Level")
                    };
                }
            }

            if (parent.LastAccount)
            {
                throw new InvalidOperationException("الحساب الأب معلّم كآخر حساب ولا يمكن إنشاء حساب فرعي تحته: " + parentCode);
            }

            var nextCode = GenerateNextAccountCode(parentCode, unitOfWork);
            var nextSerial = GenerateNextAccountSerial(parent.AccountSerial, parentCode, unitOfWork);

            using (var command = new SqlCommand(@"
INSERT INTO ACCOUNTS (
    Account_Code, Account_Name, Parent_Account_Code, last_account, cannot_del,
    Account_Serial, BasicAccount, DateCreated, Account_NameEng, UserId, BranchID,
    TblLCID, Level
) VALUES (
    @AccountCode, @AccountName, @ParentAccountCode, 1, 0,
    @AccountSerial, 0, GETDATE(), @AccountNameEng, @UserId, @BranchID,
    @TblLCID, @Level
);", unitOfWork.Connection, unitOfWork.Transaction))
            {
                command.Parameters.AddWithValue("@AccountCode", nextCode);
                command.Parameters.AddWithValue("@AccountName", accountName);
                command.Parameters.AddWithValue("@ParentAccountCode", parentCode);
                command.Parameters.AddWithValue("@AccountSerial", (object)nextSerial ?? DBNull.Value);
                command.Parameters.AddWithValue("@AccountNameEng", accountName);
                command.Parameters.AddWithValue("@UserId", (object)userId ?? DBNull.Value);
                command.Parameters.AddWithValue("@BranchID", (object)branchId ?? DBNull.Value);
                command.Parameters.AddWithValue("@TblLCID", tblLcId);
                command.Parameters.AddWithValue("@Level", parent.Level.HasValue ? (object)(parent.Level.Value + 1) : DBNull.Value);
                command.ExecuteNonQuery();
            }

            return nextCode;
        }

        private static string GenerateNextAccountCode(string parentCode, IMainErpUnitOfWork unitOfWork)
        {
            var maxSuffix = 0;
            using (var command = new SqlCommand("SELECT Account_Code FROM ACCOUNTS WITH (UPDLOCK, HOLDLOCK) WHERE Parent_Account_Code = @Parent;", unitOfWork.Connection, unitOfWork.Transaction))
            {
                command.Parameters.AddWithValue("@Parent", parentCode);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var code = ReadString(reader, "Account_Code");
                        if (string.IsNullOrWhiteSpace(code) || !code.StartsWith(parentCode + "a", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        var suffixText = code.Substring((parentCode + "a").Length);
                        int suffix;
                        if (int.TryParse(suffixText, out suffix) && suffix > maxSuffix)
                        {
                            maxSuffix = suffix;
                        }
                    }
                }
            }

            return parentCode + "a" + (maxSuffix + 1).ToString(CultureInfo.InvariantCulture);
        }

        private static string GenerateNextAccountSerial(string parentSerial, string parentCode, IMainErpUnitOfWork unitOfWork)
        {
            decimal maxSerial = 0;
            using (var command = new SqlCommand(@"
SELECT Account_Serial
FROM ACCOUNTS WITH (UPDLOCK, HOLDLOCK)
WHERE Parent_Account_Code = @Parent;", unitOfWork.Connection, unitOfWork.Transaction))
            {
                command.Parameters.AddWithValue("@Parent", parentCode);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        decimal serial;
                        if (decimal.TryParse(ReadString(reader, "Account_Serial"), NumberStyles.Any, CultureInfo.InvariantCulture, out serial) && serial > maxSerial)
                        {
                            maxSerial = serial;
                        }
                    }
                }
            }

            if (maxSerial > 0)
            {
                return (maxSerial + 1).ToString("0", CultureInfo.InvariantCulture);
            }

            if (!string.IsNullOrWhiteSpace(parentSerial) && Regex.IsMatch(parentSerial, "^[0-9]+$"))
            {
                return parentSerial + "001";
            }

            return null;
        }

        private static void InsertHeader(LCEditViewModel model, int? userId, IMainErpUnitOfWork unitOfWork)
        {
            using (var command = new SqlCommand(@"
INSERT INTO TblLC (
    TblLCID, LCNO, LCTyperId, BankId, BankID2, BoxID, Value, OpenValue,
    CurrencyId, Currency_rate, PercentV, VendorId, CountryId, FromDate,
    Todate, CloseDate, LastParcilDate, OpenBalanceDate, OpenBalance, OpenBalanceType,
    opening_balance_voucher_id, BranchID, userid, Remarks,
    project_id, projectName, PaymentTypeID, ChequeNumber, ChequeDueDate,
    Locked, AccountLGParent, AccountMarginParent, AccountAcceptanceParent,
    AccountExpensParent, Account_Code, LCAccount_Code, Account_CodeMargin,
    MarginAccount_Code, AcceptAccount_Code, AccountExpensCode, AccountExpProject,
    Name
) VALUES (
    @TblLCID, @LCNO, @LCTyperId, @BankId, @BankID2, @BoxID, @Value, @OpenValue,
    @CurrencyId, @CurrencyRate, @PercentV, @VendorId, @CountryId, @FromDate,
    @ToDate, @CloseDate, @LastParcilDate, @OpenBalanceDate, @OpenBalance, @OpenBalanceType,
    @OpeningBalanceVoucherId, @BranchID, @UserId, @Remarks,
    @ProjectId, @ProjectName, @PaymentTypeID, @ChequeNumber, @ChequeDueDate,
    @Locked, @AccountLGParent, @AccountMarginParent, @AccountAcceptanceParent,
    @AccountExpensParent, @AccountCode, @LCAccountCode, @MarginAccountCode,
    @MarginAccountCode, @AcceptanceAccountCode, @ExpenseAccountCode, @ProjectExpenseAccountCode,
    @Name
);", unitOfWork.Connection, unitOfWork.Transaction))
            {
                AddHeaderParameters(command, model, userId);
                command.ExecuteNonQuery();
            }
        }

        private static void UpdateHeader(LCEditViewModel model, int? userId, IMainErpUnitOfWork unitOfWork)
        {
            using (var command = new SqlCommand(@"
UPDATE TblLC SET
    LCNO = @LCNO,
    LCTyperId = @LCTyperId,
    BankId = @BankId,
    BankID2 = @BankID2,
    BoxID = @BoxID,
    Value = @Value,
    OpenValue = @OpenValue,
    CurrencyId = @CurrencyId,
    Currency_rate = @CurrencyRate,
    PercentV = @PercentV,
    VendorId = @VendorId,
    CountryId = @CountryId,
    FromDate = @FromDate,
    Todate = @ToDate,
    CloseDate = @CloseDate,
    LastParcilDate = @LastParcilDate,
    OpenBalanceDate = @OpenBalanceDate,
    OpenBalance = @OpenBalance,
    OpenBalanceType = @OpenBalanceType,
    opening_balance_voucher_id = @OpeningBalanceVoucherId,
    BranchID = @BranchID,
    userid = @UserId,
    Remarks = @Remarks,
    project_id = @ProjectId,
    projectName = @ProjectName,
    PaymentTypeID = @PaymentTypeID,
    ChequeNumber = @ChequeNumber,
    ChequeDueDate = @ChequeDueDate,
    Locked = @Locked,
    AccountLGParent = @AccountLGParent,
    AccountMarginParent = @AccountMarginParent,
    AccountAcceptanceParent = @AccountAcceptanceParent,
    AccountExpensParent = @AccountExpensParent,
    Account_Code = @AccountCode,
    LCAccount_Code = @LCAccountCode,
    Account_CodeMargin = @MarginAccountCode,
    MarginAccount_Code = @MarginAccountCode,
    AcceptAccount_Code = @AcceptanceAccountCode,
    AccountExpensCode = @ExpenseAccountCode,
    AccountExpProject = @ProjectExpenseAccountCode,
    Name = @Name
WHERE TblLCID = @TblLCID;", unitOfWork.Connection, unitOfWork.Transaction))
            {
                AddHeaderParameters(command, model, userId);
                command.ExecuteNonQuery();
            }
        }

        private static void AddHeaderParameters(SqlCommand command, LCEditViewModel model, int? userId)
        {
            command.Parameters.AddWithValue("@TblLCID", model.TblLCID.Value);
            command.Parameters.AddWithValue("@LCNO", (object)model.LCNO ?? DBNull.Value);
            command.Parameters.AddWithValue("@LCTyperId", (object)model.LCTyperId ?? DBNull.Value);
            command.Parameters.AddWithValue("@BankId", (object)model.BankId ?? DBNull.Value);
            command.Parameters.AddWithValue("@BankID2", (object)model.BankID2 ?? DBNull.Value);
            command.Parameters.AddWithValue("@BoxID", (object)model.BoxID ?? DBNull.Value);
            command.Parameters.AddWithValue("@Value", (object)model.Value ?? DBNull.Value);
            command.Parameters.AddWithValue("@OpenValue", (object)model.OpenValue ?? DBNull.Value);
            command.Parameters.AddWithValue("@CurrencyId", (object)model.CurrencyId ?? DBNull.Value);
            command.Parameters.AddWithValue("@CurrencyRate", (object)model.CurrencyRate ?? DBNull.Value);
            command.Parameters.AddWithValue("@PercentV", (object)model.PercentV ?? DBNull.Value);
            command.Parameters.AddWithValue("@VendorId", (object)model.VendorId ?? DBNull.Value);
            command.Parameters.AddWithValue("@CountryId", (object)model.CountryId ?? DBNull.Value);
            command.Parameters.AddWithValue("@FromDate", (object)model.FromDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@ToDate", (object)model.ToDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@CloseDate", (object)model.CloseDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@LastParcilDate", (object)model.LastParcilDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@OpenBalanceDate", (object)model.OpenBalanceDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@OpenBalance", (object)model.OpenBalance ?? DBNull.Value);
            command.Parameters.AddWithValue("@OpenBalanceType", (object)model.OpenBalanceType ?? DBNull.Value);
            command.Parameters.AddWithValue("@OpeningBalanceVoucherId", (object)model.OpeningBalanceVoucherId ?? DBNull.Value);
            command.Parameters.AddWithValue("@BranchID", (object)model.BranchID ?? DBNull.Value);
            command.Parameters.AddWithValue("@UserId", (object)userId ?? DBNull.Value);
            command.Parameters.AddWithValue("@Remarks", (object)model.Remarks ?? DBNull.Value);
            command.Parameters.AddWithValue("@ProjectId", (object)model.ProjectId ?? DBNull.Value);
            command.Parameters.AddWithValue("@ProjectName", (object)model.ProjectName ?? DBNull.Value);
            command.Parameters.AddWithValue("@PaymentTypeID", (object)model.PaymentTypeID ?? DBNull.Value);
            command.Parameters.AddWithValue("@ChequeNumber", (object)model.ChequeNumber ?? DBNull.Value);
            command.Parameters.AddWithValue("@ChequeDueDate", (object)model.ChequeDueDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@Locked", model.Locked);
            command.Parameters.AddWithValue("@AccountLGParent", (object)model.AccountLGParent ?? DBNull.Value);
            command.Parameters.AddWithValue("@AccountMarginParent", (object)model.AccountMarginParent ?? DBNull.Value);
            command.Parameters.AddWithValue("@AccountAcceptanceParent", (object)model.AccountAcceptanceParent ?? DBNull.Value);
            command.Parameters.AddWithValue("@AccountExpensParent", (object)model.AccountExpensParent ?? DBNull.Value);
            command.Parameters.AddWithValue("@AccountCode", (object)model.LCAccountCode ?? DBNull.Value);
            command.Parameters.AddWithValue("@LCAccountCode", (object)model.LCAccountCode ?? DBNull.Value);
            command.Parameters.AddWithValue("@MarginAccountCode", (object)model.MarginAccountCode ?? DBNull.Value);
            command.Parameters.AddWithValue("@AcceptanceAccountCode", (object)model.AcceptanceAccountCode ?? DBNull.Value);
            command.Parameters.AddWithValue("@ExpenseAccountCode", (object)model.ExpenseAccountCode ?? DBNull.Value);
            command.Parameters.AddWithValue("@ProjectExpenseAccountCode", (object)model.ProjectExpenseAccountCode ?? DBNull.Value);
            command.Parameters.AddWithValue("@Name", (object)model.LCNO ?? DBNull.Value);
        }

        private static LcPostingInfo LoadPostingInfo(int tblLcId, IMainErpUnitOfWork unitOfWork)
        {
            using (var command = new SqlCommand(@"
SELECT TOP 1 TblLCID, LCNO, Value, OpenValue, PercentV, Currency_rate, FromDate, Todate, BankId,
    BranchID, NoteID, NoteSerial, NoteIDOpen, NoteSerialOpen, NoteID2, NoteSerial2,
    Account_Code, LCAccount_Code, Account_CodeMargin, MarginAccount_Code, AccountExpensCode,
    OpenBalance, OpenBalanceType, OpenBalanceDate, opening_balance_voucher_id
FROM TblLC
WHERE TblLCID = @TblLCID;", unitOfWork.Connection, unitOfWork.Transaction))
            {
                command.Parameters.AddWithValue("@TblLCID", tblLcId);
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    return new LcPostingInfo
                    {
                        TblLCID = tblLcId,
                        LCNO = ReadString(reader, "LCNO"),
                        Value = ReadDecimal(reader, "Value"),
                        OpenValue = ReadDecimal(reader, "OpenValue"),
                        PercentV = ReadDecimal(reader, "PercentV"),
                        CurrencyRate = ReadDecimal(reader, "Currency_rate"),
                        FromDate = ReadDate(reader, "FromDate"),
                        ToDate = ReadDate(reader, "Todate"),
                        BankId = ReadInt(reader, "BankId"),
                        BranchId = ReadInt(reader, "BranchID"),
                        NoteId = ReadInt(reader, "NoteID"),
                        NoteSerial = ReadInt(reader, "NoteSerial"),
                        NoteIdOpen = ReadInt(reader, "NoteIDOpen"),
                        NoteSerialOpen = ReadInt(reader, "NoteSerialOpen"),
                        NoteIdClose = ReadInt(reader, "NoteID2"),
                        NoteSerialClose = ReadInt(reader, "NoteSerial2"),
                        LCAccountCode = FirstText(ReadString(reader, "LCAccount_Code"), ReadString(reader, "Account_Code")),
                        MarginAccountCode = FirstText(ReadString(reader, "Account_CodeMargin"), ReadString(reader, "MarginAccount_Code")),
                        ExpenseAccountCode = ReadString(reader, "AccountExpensCode"),
                        OpenBalance = ReadDecimal(reader, "OpenBalance"),
                        OpenBalanceType = ReadInt(reader, "OpenBalanceType"),
                        OpenBalanceDate = ReadDate(reader, "OpenBalanceDate"),
                        OpeningBalanceVoucherId = ReadDouble(reader, "opening_balance_voucher_id")
                    };
                }
            }
        }

        private static string GetBankAccount(int? bankId, IMainErpUnitOfWork unitOfWork)
        {
            using (var command = new SqlCommand("SELECT TOP 1 Account_Code FROM BanksData WHERE BankID = @BankId;", unitOfWork.Connection, unitOfWork.Transaction))
            {
                command.Parameters.AddWithValue("@BankId", (object)bankId ?? DBNull.Value);
                return Convert.ToString(command.ExecuteScalar());
            }
        }

        private static int AllocateNoteSerial(int noteType, DateTime noteDate, IMainErpUnitOfWork unitOfWork)
        {
            using (var command = new SqlCommand(@"
SELECT ISNULL(MAX(CAST(NoteSerial AS int)), 0) + 1
FROM Notes WITH (UPDLOCK, HOLDLOCK)
WHERE NoteType = @NoteType AND sanad_year = @Year;", unitOfWork.Connection, unitOfWork.Transaction))
            {
                command.Parameters.AddWithValue("@NoteType", noteType);
                command.Parameters.AddWithValue("@Year", noteDate.Year);
                var value = Convert.ToInt32(command.ExecuteScalar());
                if (value <= 1)
                {
                    value = Convert.ToInt32(noteDate.Year.ToString(CultureInfo.InvariantCulture) + noteDate.Month.ToString("00", CultureInfo.InvariantCulture) + "001", CultureInfo.InvariantCulture);
                }

                return value;
            }
        }

        private static int AllocateOpeningNoteSerial(int noteType, DateTime noteDate, IMainErpUnitOfWork unitOfWork)
        {
            using (var command = new SqlCommand(@"
SELECT ISNULL(MAX(CAST(NoteSerial AS int)), 0) + 1
FROM Notes1 WITH (UPDLOCK, HOLDLOCK)
WHERE NoteType = @NoteType AND sanad_year = @Year;", unitOfWork.Connection, unitOfWork.Transaction))
            {
                command.Parameters.AddWithValue("@NoteType", noteType);
                command.Parameters.AddWithValue("@Year", noteDate.Year);
                var value = Convert.ToInt32(command.ExecuteScalar());
                if (value <= 1)
                {
                    value = Convert.ToInt32(noteDate.Year.ToString(CultureInfo.InvariantCulture) + noteDate.Month.ToString("00", CultureInfo.InvariantCulture) + "001", CultureInfo.InvariantCulture);
                }

                return value;
            }
        }

        private static void UpsertNormalLcNote(LcPostingInfo lc, int noteId, int noteSerial, decimal noteValue, long voucherId, Guid rowId, int? userId, IMainErpUnitOfWork unitOfWork)
        {
            UpsertLcNote(lc, noteId, noteSerial, 22001, noteValue, voucherId, rowId, userId, unitOfWork, "    حساب ال" + lc.LCNO, "NoteID", "NoteSerial", "NoteIDRowId");
        }

        private static void UpsertLcNote(LcPostingInfo lc, int noteId, int noteSerial, int noteType, decimal noteValue, long voucherId, Guid rowId, int? userId, IMainErpUnitOfWork unitOfWork, string remark, string noteIdField, string noteSerialField, string rowIdField)
        {
            using (var command = new SqlCommand(@"
IF EXISTS (SELECT 1 FROM Notes WHERE NoteID = @NoteID)
BEGIN
    UPDATE Notes SET
        NoteDate = @NoteDate, NoteType = @NoteType, NoteSerial = @NoteSerial,
        Note_Value = @NoteValue, UserID = @UserID, Remark = @Remark,
        branch_no = @BranchID, numbering_type = 2, sanad_year = @Year,
        sanad_month = @Month, Double_Entry_Vouchers_ID = @VoucherId,
        TblLCID = @TblLCID, RowId = @RowId
    WHERE NoteID = @NoteID;
END
ELSE
BEGIN
    INSERT INTO Notes (
        NoteID, NoteDate, NoteType, NoteSerial, Note_Value, UserID, Remark,
        branch_no, numbering_type, sanad_year, sanad_month,
        Double_Entry_Vouchers_ID, TblLCID, RowId
    ) VALUES (
        @NoteID, @NoteDate, @NoteType, @NoteSerial, @NoteValue, @UserID, @Remark,
        @BranchID, 2, @Year, @Month,
        @VoucherId, @TblLCID, @RowId
    );
END

UPDATE TblLC
SET " + noteIdField + @" = @NoteID,
    " + noteSerialField + @" = @NoteSerial,
    " + rowIdField + @" = @RowId
WHERE TblLCID = @TblLCID;", unitOfWork.Connection, unitOfWork.Transaction))
            {
                var noteDate = lc.FromDate ?? DateTime.Today;
                command.Parameters.AddWithValue("@NoteID", noteId);
                command.Parameters.AddWithValue("@NoteDate", noteDate);
                command.Parameters.AddWithValue("@NoteType", noteType);
                command.Parameters.AddWithValue("@NoteSerial", noteSerial);
                command.Parameters.AddWithValue("@NoteValue", noteValue);
                command.Parameters.AddWithValue("@UserID", (object)userId ?? DBNull.Value);
                command.Parameters.AddWithValue("@Remark", remark);
                command.Parameters.AddWithValue("@BranchID", (object)lc.BranchId ?? DBNull.Value);
                command.Parameters.AddWithValue("@Year", noteDate.Year);
                command.Parameters.AddWithValue("@Month", noteDate.Month);
                command.Parameters.AddWithValue("@VoucherId", voucherId);
                command.Parameters.AddWithValue("@TblLCID", lc.TblLCID);
                command.Parameters.AddWithValue("@RowId", rowId);
                command.ExecuteNonQuery();
            }
        }

        private static void UpsertOpeningLcNote(LcPostingInfo lc, int noteId, int noteSerial, decimal noteValue, long voucherId, Guid rowId, int? userId, DateTime noteDate, IMainErpUnitOfWork unitOfWork)
        {
            using (var command = new SqlCommand(@"
IF EXISTS (SELECT 1 FROM Notes1 WHERE NoteID = @NoteID)
BEGIN
    UPDATE Notes1 SET
        NoteDate = @NoteDate, NoteType = 101, NoteSerial = @NoteSerial,
        Note_Value = @NoteValue, UserID = @UserID, Remark = @Remark,
        branch_no = @BranchID, numbering_type = 2, sanad_year = @Year,
        sanad_month = @Month, Double_Entry_Vouchers_ID = @VoucherId,
        TblLCID = @TblLCID, RowId = @RowId
    WHERE NoteID = @NoteID;
END
ELSE
BEGIN
    INSERT INTO Notes1 (
        NoteID, NoteDate, NoteType, NoteSerial, Note_Value, UserID, Remark,
        branch_no, numbering_type, sanad_year, sanad_month,
        Double_Entry_Vouchers_ID, TblLCID, RowId
    ) VALUES (
        @NoteID, @NoteDate, 101, @NoteSerial, @NoteValue, @UserID, @Remark,
        @BranchID, 2, @Year, @Month,
        @VoucherId, @TblLCID, @RowId
    );
END", unitOfWork.Connection, unitOfWork.Transaction))
            {
                command.Parameters.AddWithValue("@NoteID", noteId);
                command.Parameters.AddWithValue("@NoteDate", noteDate);
                command.Parameters.AddWithValue("@NoteSerial", noteSerial);
                command.Parameters.AddWithValue("@NoteValue", noteValue);
                command.Parameters.AddWithValue("@UserID", (object)userId ?? DBNull.Value);
                command.Parameters.AddWithValue("@Remark", "Opening balance for LC " + lc.LCNO);
                command.Parameters.AddWithValue("@BranchID", (object)lc.BranchId ?? DBNull.Value);
                command.Parameters.AddWithValue("@Year", noteDate.Year);
                command.Parameters.AddWithValue("@Month", noteDate.Month);
                command.Parameters.AddWithValue("@VoucherId", voucherId);
                command.Parameters.AddWithValue("@TblLCID", lc.TblLCID);
                command.Parameters.AddWithValue("@RowId", rowId);
                command.ExecuteNonQuery();
            }
        }

        private static decimal FindVatPercent(IMainErpUnitOfWork unitOfWork)
        {
            using (var command = new SqlCommand(@"
SELECT TOP 1 VATPer
FROM TblVATSettingsDet
WHERE ISNULL(VATPer, 0) > 0
ORDER BY ID DESC;", unitOfWork.Connection, unitOfWork.Transaction))
            {
                var value = command.ExecuteScalar();
                if (value != null && value != DBNull.Value)
                {
                    return Convert.ToDecimal(value);
                }
            }

            return 15m;
        }

        private static string FindVatInputAccount(IMainErpUnitOfWork unitOfWork)
        {
            using (var command = new SqlCommand(@"
SELECT TOP 1 Account_Code
FROM ACCOUNTS
WHERE Account_Name LIKE N'%مدخلات ضريبة القيمة المضافة%'
   OR Account_NameEng LIKE N'%Value Added%'
ORDER BY Account_Code;", unitOfWork.Connection, unitOfWork.Transaction))
            {
                return Convert.ToString(command.ExecuteScalar());
            }
        }

        private static void DeleteVoucherRowsForNote(int noteId, IMainErpUnitOfWork unitOfWork)
        {
            using (var command = new SqlCommand("DELETE FROM DOUBLE_ENTREY_VOUCHERS WHERE Notes_ID = @NoteID;", unitOfWork.Connection, unitOfWork.Transaction))
            {
                command.Parameters.AddWithValue("@NoteID", noteId);
                command.ExecuteNonQuery();
            }
        }

        private static void DeleteOpeningVoucherRowsForLc(int tblLcId, IMainErpUnitOfWork unitOfWork)
        {
            using (var command = new SqlCommand(@"
DELETE FROM DOUBLE_ENTREY_VOUCHERS1
WHERE Notes_ID IN (SELECT NoteID FROM Notes1 WHERE TblLCID = @TblLCID)
   OR opening_balance_voucher_id IN (
        SELECT opening_balance_voucher_id
        FROM TblLC
        WHERE TblLCID = @TblLCID AND opening_balance_voucher_id IS NOT NULL
   );", unitOfWork.Connection, unitOfWork.Transaction))
            {
                command.Parameters.AddWithValue("@TblLCID", tblLcId);
                command.ExecuteNonQuery();
            }
        }

        private static void DeleteHeaderOpeningVoucherRowsForLc(int tblLcId, IMainErpUnitOfWork unitOfWork)
        {
            using (var command = new SqlCommand(@"
DELETE FROM DOUBLE_ENTREY_VOUCHERS1
WHERE opening_balance_voucher_id IN (
        SELECT opening_balance_voucher_id
        FROM TblLC
        WHERE TblLCID = @TblLCID AND opening_balance_voucher_id IS NOT NULL
   );

DELETE FROM Notes1
WHERE Double_Entry_Vouchers_ID IN (
        SELECT opening_balance_voucher_id
        FROM TblLC
        WHERE TblLCID = @TblLCID AND opening_balance_voucher_id IS NOT NULL
   );", unitOfWork.Connection, unitOfWork.Transaction))
            {
                command.Parameters.AddWithValue("@TblLCID", tblLcId);
                command.ExecuteNonQuery();
            }
        }

        private static void DeleteOpeningVoucherRowsForNote(int noteId, IMainErpUnitOfWork unitOfWork)
        {
            using (var command = new SqlCommand("DELETE FROM DOUBLE_ENTREY_VOUCHERS1 WHERE Notes_ID = @NoteID;", unitOfWork.Connection, unitOfWork.Transaction))
            {
                command.Parameters.AddWithValue("@NoteID", noteId);
                command.ExecuteNonQuery();
            }
        }

        private static void InsertVoucherRow(long voucherId, int lineNo, string accountCode, decimal value, int creditOrDebit, string description, DateTime? recordDate, int noteId, int? userId, int? branchId, IMainErpUnitOfWork unitOfWork)
        {
            using (var command = new SqlCommand(@"
INSERT INTO DOUBLE_ENTREY_VOUCHERS (
    Double_Entry_Vouchers_ID, DEV_ID_Line_No, Account_Code, Value,
    Credit_Or_Debit, Double_Entry_Vouchers_Description, RecordDate,
    Notes_ID, UserID, branch_id, depet_value, credit_value, des
) VALUES (
    @VoucherId, @LineNo, @AccountCode, @Value,
    @CreditOrDebit, @Description, @RecordDate,
    @NoteId, @UserId, @BranchId,
    CASE WHEN @CreditOrDebit = 0 THEN @Value ELSE 0 END,
    CASE WHEN @CreditOrDebit = 1 THEN @Value ELSE 0 END,
    @Description
);", unitOfWork.Connection, unitOfWork.Transaction))
            {
                command.Parameters.AddWithValue("@VoucherId", voucherId);
                command.Parameters.AddWithValue("@LineNo", lineNo);
                command.Parameters.AddWithValue("@AccountCode", accountCode);
                command.Parameters.AddWithValue("@Value", value);
                command.Parameters.AddWithValue("@CreditOrDebit", creditOrDebit);
                command.Parameters.AddWithValue("@Description", description);
                command.Parameters.AddWithValue("@RecordDate", (object)recordDate ?? DateTime.Today);
                command.Parameters.AddWithValue("@NoteId", noteId);
                command.Parameters.AddWithValue("@UserId", (object)userId ?? DBNull.Value);
                command.Parameters.AddWithValue("@BranchId", (object)branchId ?? DBNull.Value);
                command.ExecuteNonQuery();
            }
        }

        private static void InsertOpeningVoucherRow(long voucherId, int lineNo, string accountCode, decimal value, int creditOrDebit, string description, DateTime? recordDate, int noteId, int? userId, int? branchId, long openingBalanceVoucherId, IMainErpUnitOfWork unitOfWork)
        {
            using (var command = new SqlCommand(@"
INSERT INTO DOUBLE_ENTREY_VOUCHERS1 (
    Double_Entry_Vouchers_ID, DEV_ID_Line_No, Account_Code, Value,
    Credit_Or_Debit, Double_Entry_Vouchers_Description, RecordDate,
    Notes_ID, UserID, branch_id, depet_value, credit_value, des,
    rate, valuee, opening_balance_voucher_id, IsHiddenInv
) VALUES (
    @VoucherId, @LineNo, @AccountCode, @Value,
    @CreditOrDebit, @Description, @RecordDate,
    @NoteId, @UserId, @BranchId,
    CASE WHEN @CreditOrDebit = 0 THEN @Value ELSE 0 END,
    CASE WHEN @CreditOrDebit = 1 THEN @Value ELSE 0 END,
    @Description, 1, @Value, @OpeningBalanceVoucherId, 0
);", unitOfWork.Connection, unitOfWork.Transaction))
            {
                command.Parameters.AddWithValue("@VoucherId", voucherId);
                command.Parameters.AddWithValue("@LineNo", lineNo);
                command.Parameters.AddWithValue("@AccountCode", accountCode);
                command.Parameters.AddWithValue("@Value", value);
                command.Parameters.AddWithValue("@CreditOrDebit", creditOrDebit);
                command.Parameters.AddWithValue("@Description", description);
                command.Parameters.AddWithValue("@RecordDate", (object)recordDate ?? DateTime.Today);
                command.Parameters.AddWithValue("@NoteId", noteId);
                command.Parameters.AddWithValue("@UserId", (object)userId ?? DBNull.Value);
                command.Parameters.AddWithValue("@BranchId", (object)branchId ?? DBNull.Value);
                command.Parameters.AddWithValue("@OpeningBalanceVoucherId", openingBalanceVoucherId);
                command.ExecuteNonQuery();
            }
        }

        private static void InsertGridVoucherRow(long voucherId, int lineNo, string accountCode, decimal value, int creditOrDebit, string description, DateTime? recordDate, int noteId, int? userId, int? branchId, bool openingBalance, long openingBalanceVoucherId, IMainErpUnitOfWork unitOfWork)
        {
            if (openingBalance)
            {
                InsertOpeningVoucherRow(voucherId, lineNo, accountCode, value, creditOrDebit, description, recordDate, noteId, userId, branchId, openingBalanceVoucherId, unitOfWork);
                return;
            }

            InsertVoucherRow(voucherId, lineNo, accountCode, value, creditOrDebit, description, recordDate, noteId, userId, branchId, unitOfWork);
        }

        private static void UpsertGridNote(LcPostingInfo lc, string tableName, int rowIdValue, bool isPay, bool openingBalance, int noteId, int noteSerial, int noteType, decimal noteValue, long voucherId, Guid rowId, int? userId, DateTime noteDate, IMainErpUnitOfWork unitOfWork)
        {
            var noteTable = openingBalance ? "Notes1" : "Notes";
            var noteIdField = isPay ? "NoteID2" : "NoteID";
            var noteSerialField = isPay ? "NoteSerial2" : "NoteSerial";

            using (var command = new SqlCommand(@"
IF EXISTS (SELECT 1 FROM " + noteTable + @" WHERE NoteID = @NoteID)
BEGIN
    UPDATE " + noteTable + @" SET
        NoteDate = @NoteDate, NoteType = @NoteType, NoteSerial = @NoteSerial,
        Note_Value = @NoteValue, UserID = @UserID, Remark = @Remark,
        branch_no = @BranchID, numbering_type = 2, sanad_year = @Year,
        sanad_month = @Month, Double_Entry_Vouchers_ID = @VoucherId,
        TblLCID = @TblLCID, RowId = @RowId
    WHERE NoteID = @NoteID;
END
ELSE
BEGIN
    INSERT INTO " + noteTable + @" (
        NoteID, NoteDate, NoteType, NoteSerial, Note_Value, UserID, Remark,
        branch_no, numbering_type, sanad_year, sanad_month,
        Double_Entry_Vouchers_ID, TblLCID, RowId
    ) VALUES (
        @NoteID, @NoteDate, @NoteType, @NoteSerial, @NoteValue, @UserID, @Remark,
        @BranchID, 2, @Year, @Month,
        @VoucherId, @TblLCID, @RowId
    );
END

UPDATE " + tableName + @"
SET " + noteIdField + @" = @NoteID,
    " + noteSerialField + @" = @NoteSerial
WHERE ID = @RowIDValue AND TblLCID = @TblLCID;", unitOfWork.Connection, unitOfWork.Transaction))
            {
                command.Parameters.AddWithValue("@NoteID", noteId);
                command.Parameters.AddWithValue("@NoteDate", noteDate);
                command.Parameters.AddWithValue("@NoteType", noteType);
                command.Parameters.AddWithValue("@NoteSerial", noteSerial);
                command.Parameters.AddWithValue("@NoteValue", noteValue);
                command.Parameters.AddWithValue("@UserID", (object)userId ?? DBNull.Value);
                command.Parameters.AddWithValue("@Remark", "LC grid " + lc.LCNO);
                command.Parameters.AddWithValue("@BranchID", (object)lc.BranchId ?? DBNull.Value);
                command.Parameters.AddWithValue("@Year", noteDate.Year);
                command.Parameters.AddWithValue("@Month", noteDate.Month);
                command.Parameters.AddWithValue("@VoucherId", voucherId);
                command.Parameters.AddWithValue("@TblLCID", lc.TblLCID);
                command.Parameters.AddWithValue("@RowId", rowId);
                command.Parameters.AddWithValue("@RowIDValue", rowIdValue);
                command.ExecuteNonQuery();
            }
        }

        private static bool HasOpeningVoucherRows(double openingBalanceVoucherId, IMainErpUnitOfWork unitOfWork)
        {
            using (var command = new SqlCommand("SELECT COUNT(1) FROM DOUBLE_ENTREY_VOUCHERS1 WHERE opening_balance_voucher_id = @OpeningBalanceVoucherId;", unitOfWork.Connection, unitOfWork.Transaction))
            {
                command.Parameters.AddWithValue("@OpeningBalanceVoucherId", openingBalanceVoucherId);
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        private static void DeleteAllLcAccounting(int tblLcId, IMainErpUnitOfWork unitOfWork, bool includeGridNotes)
        {
            var normalNoteFilter = includeGridNotes
                ? "SELECT NoteID FROM Notes WHERE TblLCID = @TblLCID"
                : @"SELECT NoteID FROM TblLC WHERE TblLCID = @TblLCID AND NoteID IS NOT NULL
                   UNION ALL
                   SELECT NoteIDOpen FROM TblLC WHERE TblLCID = @TblLCID AND NoteIDOpen IS NOT NULL
                   UNION ALL
                   SELECT NoteID2 FROM TblLC WHERE TblLCID = @TblLCID AND NoteID2 IS NOT NULL";

            using (var command = new SqlCommand(@"
DELETE FROM DOUBLE_ENTREY_VOUCHERS
WHERE Notes_ID IN (" + normalNoteFilter + @");

DELETE FROM Notes
WHERE NoteID IN (" + normalNoteFilter + @");
", unitOfWork.Connection, unitOfWork.Transaction))
            {
                command.Parameters.AddWithValue("@TblLCID", tblLcId);
                command.ExecuteNonQuery();
            }

            if (includeGridNotes)
            {
                DeleteOpeningVoucherRowsForLc(tblLcId, unitOfWork);
                ExecuteNonQuery(unitOfWork, "DELETE FROM Notes1 WHERE TblLCID = @TblLCID;", tblLcId);
            }
            else
            {
                DeleteHeaderOpeningVoucherRowsForLc(tblLcId, unitOfWork);
            }

            // When rebuilding core LC vouchers, keep grid voucher links intact.
            // Full delete removes the grid rows themselves, so no reference cleanup is needed here.
        }

        private static void ExecuteNonQuery(IMainErpUnitOfWork unitOfWork, string sql, int tblLcId)
        {
            using (var command = new SqlCommand(sql, unitOfWork.Connection, unitOfWork.Transaction))
            {
                command.Parameters.AddWithValue("@TblLCID", tblLcId);
                command.ExecuteNonQuery();
            }
        }

        private static void TryWriteAudit(IMainErpUnitOfWork unitOfWork, string operationName, int tblLcId, int? userId, string message, string beforeSnapshot, string afterSnapshot)
        {
            try
            {
                if (!TableExists(unitOfWork, "MainErp_AuditLog"))
                {
                    return;
                }

                var hasBefore = ColumnExists(unitOfWork, "MainErp_AuditLog", "BeforeSnapshot");
                var hasAfter = ColumnExists(unitOfWork, "MainErp_AuditLog", "AfterSnapshot");
                var sql = @"
INSERT INTO dbo.MainErp_AuditLog (
    OperationName, EntityName, EntityKey, UserId, CorrelationId, Message" +
    (hasBefore ? ", BeforeSnapshot" : string.Empty) +
    (hasAfter ? ", AfterSnapshot" : string.Empty) + @"
) VALUES (
    @OperationName, N'TblLC', @EntityKey, @UserId, @CorrelationId, @Message" +
    (hasBefore ? ", @BeforeSnapshot" : string.Empty) +
    (hasAfter ? ", @AfterSnapshot" : string.Empty) + @"
);";

                using (var command = new SqlCommand(sql, unitOfWork.Connection, unitOfWork.Transaction))
                {
                    command.Parameters.AddWithValue("@OperationName", operationName);
                    command.Parameters.AddWithValue("@EntityKey", tblLcId.ToString(CultureInfo.InvariantCulture));
                    command.Parameters.AddWithValue("@UserId", (object)userId ?? DBNull.Value);
                    command.Parameters.AddWithValue("@CorrelationId", Guid.NewGuid());
                    command.Parameters.AddWithValue("@Message", (object)message ?? DBNull.Value);
                    if (hasBefore)
                    {
                        command.Parameters.AddWithValue("@BeforeSnapshot", (object)beforeSnapshot ?? DBNull.Value);
                    }

                    if (hasAfter)
                    {
                        command.Parameters.AddWithValue("@AfterSnapshot", (object)afterSnapshot ?? DBNull.Value);
                    }

                    command.ExecuteNonQuery();
                }
            }
            catch
            {
                // Audit must never block the accounting transaction in legacy migration mode.
            }
        }

        private static bool TableExists(IMainErpUnitOfWork unitOfWork, string tableName)
        {
            using (var command = new SqlCommand("SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @TableName;", unitOfWork.Connection, unitOfWork.Transaction))
            {
                command.Parameters.AddWithValue("@TableName", tableName);
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        private static bool ColumnExists(IMainErpUnitOfWork unitOfWork, string tableName, string columnName)
        {
            using (var command = new SqlCommand("SELECT COUNT(1) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName AND COLUMN_NAME = @ColumnName;", unitOfWork.Connection, unitOfWork.Transaction))
            {
                command.Parameters.AddWithValue("@TableName", tableName);
                command.Parameters.AddWithValue("@ColumnName", columnName);
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        private static bool HasPostedVoucher(SqlConnection connection, int? noteId)
        {
            if (!noteId.HasValue)
            {
                return false;
            }

            using (var command = new SqlCommand("SELECT COUNT(1) FROM DOUBLE_ENTREY_VOUCHERS WHERE Notes_ID = @NoteID;", connection))
            {
                command.Parameters.AddWithValue("@NoteID", noteId.Value);
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        private static bool HasVoucherRows(int noteId, IMainErpUnitOfWork unitOfWork)
        {
            using (var command = new SqlCommand("SELECT COUNT(1) FROM DOUBLE_ENTREY_VOUCHERS WHERE Notes_ID = @NoteID;", unitOfWork.Connection, unitOfWork.Transaction))
            {
                command.Parameters.AddWithValue("@NoteID", noteId);
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        private static bool HasVoucherRowsForOptionalNote(int? noteId, bool openingBalance, IMainErpUnitOfWork unitOfWork)
        {
            if (!noteId.HasValue || noteId.Value == 0)
            {
                return false;
            }

            var tableName = openingBalance ? "DOUBLE_ENTREY_VOUCHERS1" : "DOUBLE_ENTREY_VOUCHERS";
            using (var command = new SqlCommand("SELECT COUNT(1) FROM " + tableName + " WHERE Notes_ID = @NoteID;", unitOfWork.Connection, unitOfWork.Transaction))
            {
                command.Parameters.AddWithValue("@NoteID", noteId.Value);
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        private static long? GetVoucherId(int noteId, IMainErpUnitOfWork unitOfWork)
        {
            using (var command = new SqlCommand("SELECT TOP 1 Double_Entry_Vouchers_ID FROM DOUBLE_ENTREY_VOUCHERS WHERE Notes_ID = @NoteID ORDER BY Double_Entry_Vouchers_ID DESC;", unitOfWork.Connection, unitOfWork.Transaction))
            {
                command.Parameters.AddWithValue("@NoteID", noteId);
                var value = command.ExecuteScalar();
                return value == null || value == DBNull.Value ? (long?)null : Convert.ToInt64(value);
            }
        }

        private static string FirstText(string first, string second)
        {
            return !string.IsNullOrWhiteSpace(first) ? first : second;
        }

        private static string ReadString(IDataRecord reader, string columnName)
        {
            return reader[columnName] == DBNull.Value ? null : Convert.ToString(reader[columnName]);
        }

        private static int? ReadInt(IDataRecord reader, string columnName)
        {
            return reader[columnName] == DBNull.Value ? (int?)null : Convert.ToInt32(reader[columnName]);
        }

        private static decimal? ReadDecimal(IDataRecord reader, string columnName)
        {
            return reader[columnName] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader[columnName]);
        }

        private static double? ReadDouble(IDataRecord reader, string columnName)
        {
            return reader[columnName] == DBNull.Value ? (double?)null : Convert.ToDouble(reader[columnName]);
        }

        private static DateTime? ReadDate(IDataRecord reader, string columnName)
        {
            return reader[columnName] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader[columnName]);
        }

        private static bool? ReadBool(IDataRecord reader, string columnName)
        {
            return reader[columnName] == DBNull.Value ? (bool?)null : Convert.ToBoolean(reader[columnName]);
        }

        private sealed class AccountRow
        {
            public string AccountCode { get; set; }
            public string AccountSerial { get; set; }
            public bool LastAccount { get; set; }
            public int? Level { get; set; }
        }

        private sealed class LcPostingInfo
        {
            public int TblLCID { get; set; }
            public string LCNO { get; set; }
            public decimal? Value { get; set; }
            public decimal? OpenValue { get; set; }
            public decimal? PercentV { get; set; }
            public decimal? CurrencyRate { get; set; }
            public DateTime? FromDate { get; set; }
            public DateTime? ToDate { get; set; }
            public int? BankId { get; set; }
            public int? BranchId { get; set; }
            public int? NoteId { get; set; }
            public int? NoteSerial { get; set; }
            public int? NoteIdOpen { get; set; }
            public int? NoteSerialOpen { get; set; }
            public int? NoteIdClose { get; set; }
            public int? NoteSerialClose { get; set; }
            public string LCAccountCode { get; set; }
            public string MarginAccountCode { get; set; }
            public string ExpenseAccountCode { get; set; }
            public decimal? OpenBalance { get; set; }
            public int? OpenBalanceType { get; set; }
            public DateTime? OpenBalanceDate { get; set; }
            public double? OpeningBalanceVoucherId { get; set; }
        }

        private sealed class LcMarginPostRow
        {
            public int ID { get; set; }
            public decimal Amount { get; set; }
            public decimal PayedAmount { get; set; }
            public decimal MargenValue { get; set; }
            public string MarginAccountCode { get; set; }
            public string BankAccountCode { get; set; }
            public string BankAccountCode2 { get; set; }
            public string AccountMargen2 { get; set; }
            public DateTime? OrderDate { get; set; }
            public DateTime? PayDate { get; set; }
            public int? NoteID { get; set; }
            public int? NoteSerial { get; set; }
            public int? NoteID2 { get; set; }
            public int? NoteSerial2 { get; set; }
            public bool IsOpenBalance { get; set; }
        }

        private sealed class LcHistoryPostRow
        {
            public int ID { get; set; }
            public decimal AmountPlus { get; set; }
            public decimal AmountMin { get; set; }
            public int? NoteID { get; set; }
            public int? NoteSerial { get; set; }
        }

        private sealed class LcOpenBalancePostRow
        {
            public int ID { get; set; }
            public DateTime? GuaranteeDate { get; set; }
            public decimal InsuranceAmount { get; set; }
            public decimal ExpAmount { get; set; }
            public string MarginAccountCode { get; set; }
            public string BankAccountCode { get; set; }
            public int? NoteID { get; set; }
            public int? NoteSerial { get; set; }
        }

        private sealed class LcGridDeleteRow
        {
            public string SourceTable { get; set; }
            public int RowId { get; set; }
            public int TblLcId { get; set; }
            public int? NoteId { get; set; }
            public int? NoteId2 { get; set; }
            public int? NoteId3 { get; set; }
            public int? NoteSerial { get; set; }
            public decimal? Amount { get; set; }
            public decimal? PayedAmount { get; set; }
            public bool IsOpenBalance { get; set; }
        }
    }
}
