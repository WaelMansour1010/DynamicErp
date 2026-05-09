using System;
using System.Data;
using System.Data.SqlClient;
using MyERP.Areas.MainErp.Interfaces;
using MyERP.Areas.MainErp.ViewModels.Payments;

namespace MyERP.Areas.MainErp.Repositories.Payments
{
    public class PaymentVoucherWriteRepository
    {
        private readonly IMainErpDbConnectionFactory _connectionFactory;

        public PaymentVoucherWriteRepository(IMainErpDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public PaymentVoucherEditViewModel CreateEditModel(int noteType, string title, string screenName, string backController, string backAction)
        {
            var model = NewModel(noteType, title, screenName, backController, backAction);
            LoadLookups(model);
            return model;
        }

        public PaymentVoucherEditViewModel WithLookups(PaymentVoucherEditViewModel model)
        {
            LoadLookups(model);
            return model;
        }

        public PaymentVoucherEditViewModel CreateEditModelFromDetails(PaymentVoucherDetailsViewModel details, int noteType, string title, string screenName, string backController, string backAction)
        {
            var model = NewModel(noteType, title, screenName, backController, backAction);
            if (details != null && details.Header != null)
            {
                var h = details.Header;
                model.NoteId = h.NoteId;
                model.NoteSerial = h.NoteSerial;
                model.NoteDate = h.NoteDate ?? DateTime.Today;
                model.ManualNo = h.ManualNo;
                model.OrderNo = h.OrderNo;
                model.PartyDisplay = h.PartyDisplay;
                model.PartyAccountDisplay = h.AccountDisplay;
                model.Amount = h.Amount;
                model.Vat = h.Vat;
                model.IncludeVat = h.IncludeVat;
                model.Remark = h.Remark;
                model.PayDescription = h.PayDescription;
                model.PayDescription2 = h.PayDescription2;
                model.ChequeNumber = h.ChequeNumber;
                model.ChequeDueDate = h.ChequeDueDate;
            }

            LoadWritableHeader(model);
            LoadLookups(model);
            return model;
        }

        public PaymentVoucherSaveResultViewModel Save(PaymentVoucherEditViewModel model, int userId)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = new SqlCommand("dbo.usp_DynamicErpVoucher_Save", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.CommandTimeout = 90;
                command.Parameters.Add("@noteType", SqlDbType.Int).Value = model.NoteType;
                command.Parameters.Add("@noteId", SqlDbType.Int).Value = (object)model.NoteId ?? DBNull.Value;
                command.Parameters.Add("@noteDate", SqlDbType.DateTime).Value = model.NoteDate == DateTime.MinValue ? DateTime.Today : model.NoteDate;
                command.Parameters.Add("@manualNo", SqlDbType.NVarChar, 255).Value = (object)Trim(model.ManualNo) ?? DBNull.Value;
                command.Parameters.Add("@orderNo", SqlDbType.NVarChar, 50).Value = (object)Trim(model.OrderNo) ?? DBNull.Value;
                command.Parameters.Add("@partyAccountCode", SqlDbType.NVarChar, 55).Value = Trim(model.PartyAccountCode);
                command.Parameters.Add("@partyDisplay", SqlDbType.NVarChar, 4000).Value = (object)Trim(model.PartyDisplay) ?? DBNull.Value;
                command.Parameters.Add("@branchId", SqlDbType.Int).Value = (object)model.BranchId ?? DBNull.Value;
                command.Parameters.Add("@boxId", SqlDbType.Int).Value = (object)model.BoxId ?? DBNull.Value;
                command.Parameters.Add("@bankId", SqlDbType.Int).Value = (object)model.BankId ?? DBNull.Value;
                command.Parameters.Add("@paymentMethod", SqlDbType.Int).Value = (object)model.PaymentMethod ?? DBNull.Value;
                command.Parameters.Add("@cashingType", SqlDbType.Int).Value = (object)model.CashingType ?? DBNull.Value;
                command.Parameters.Add("@receiptClass", SqlDbType.Int).Value = (object)model.ReceiptClass ?? DBNull.Value;
                command.Parameters.Add("@chequeNumber", SqlDbType.NVarChar, 255).Value = (object)Trim(model.ChequeNumber) ?? DBNull.Value;
                command.Parameters.Add("@chequeDueDate", SqlDbType.DateTime).Value = (object)model.ChequeDueDate ?? DBNull.Value;
                command.Parameters.Add("@amount", SqlDbType.Decimal).Value = model.Amount;
                command.Parameters.Add("@vat", SqlDbType.Decimal).Value = model.Vat;
                command.Parameters.Add("@includeVat", SqlDbType.Int).Value = model.IncludeVat ? 1 : 0;
                command.Parameters.Add("@remark", SqlDbType.NVarChar, 4000).Value = (object)Trim(model.Remark) ?? DBNull.Value;
                command.Parameters.Add("@payDes", SqlDbType.NVarChar, 4000).Value = (object)Trim(model.PayDescription) ?? DBNull.Value;
                command.Parameters.Add("@payDes1", SqlDbType.NVarChar, 4000).Value = (object)Trim(model.PayDescription2) ?? DBNull.Value;
                command.Parameters.Add("@userId", SqlDbType.Int).Value = userId;

                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        throw new InvalidOperationException("لم يرجع إجراء الحفظ نتيجة.");
                    }

                    return new PaymentVoucherSaveResultViewModel
                    {
                        NoteId = PaymentVoucherReadRepository.ReadInt(reader, "NoteID"),
                        NoteSerial = PaymentVoucherReadRepository.ReadSerialString(reader, "NoteSerial"),
                        VoucherId = PaymentVoucherReadRepository.ReadInt(reader, "Double_Entry_Vouchers_ID"),
                        Message = PaymentVoucherReadRepository.ReadString(reader, "ResultMessage")
                    };
                }
            }
        }

        public void Post(int noteType, int noteId, int userId)
        {
            ExecuteSimple("dbo.usp_DynamicErpVoucher_Post", noteType, noteId, userId);
        }

        public void Delete(int noteType, int noteId, int userId)
        {
            ExecuteSimple("dbo.usp_DynamicErpVoucher_Delete", noteType, noteId, userId);
        }

        private void ExecuteSimple(string procedureName, int noteType, int noteId, int userId)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = new SqlCommand(procedureName, connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("@noteType", SqlDbType.Int).Value = noteType;
                command.Parameters.Add("@noteId", SqlDbType.Int).Value = noteId;
                command.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
                command.ExecuteNonQuery();
            }
        }

        private static PaymentVoucherEditViewModel NewModel(int noteType, string title, string screenName, string backController, string backAction)
        {
            return new PaymentVoucherEditViewModel
            {
                NoteType = noteType,
                Title = title,
                ScreenName = screenName,
                BackController = backController,
                BackAction = backAction,
                PaymentMethod = 0,
                CashingType = 0,
                ReceiptClass = 0
            };
        }

        private void LoadWritableHeader(PaymentVoucherEditViewModel model)
        {
            if (!model.NoteId.HasValue)
            {
                return;
            }

            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = new SqlCommand("dbo.usp_DynamicErpVoucher_EditHeader", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("@noteType", SqlDbType.Int).Value = model.NoteType;
                command.Parameters.Add("@id", SqlDbType.Int).Value = model.NoteId.Value;
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return;
                    }

                    model.PartyAccountCode = PaymentVoucherReadRepository.ReadString(reader, "PartyAccountCode");
                    model.BranchId = PaymentVoucherReadRepository.ReadNullableInt(reader, "BranchId");
                    model.BoxId = PaymentVoucherReadRepository.ReadNullableInt(reader, "BoxId");
                    model.BankId = PaymentVoucherReadRepository.ReadNullableInt(reader, "BankId");
                    model.PaymentMethod = PaymentVoucherReadRepository.ReadNullableInt(reader, "PaymentMethod");
                    model.CashingType = PaymentVoucherReadRepository.ReadNullableInt(reader, "CashingType");
                    model.ReceiptClass = PaymentVoucherReadRepository.ReadNullableInt(reader, "ReceiptClass");
                    model.IsPosted = PaymentVoucherReadRepository.ReadNullableInt(reader, "IsPosted").GetValueOrDefault() != 0;
                }
            }
        }

        private void LoadLookups(PaymentVoucherEditViewModel model)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = new SqlCommand("dbo.usp_DynamicErpVoucher_EditLookups", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                using (var reader = command.ExecuteReader())
                {
                    ReadLookup(reader, model.Branches);
                    if (reader.NextResult()) ReadLookup(reader, model.Boxes);
                    if (reader.NextResult()) ReadLookup(reader, model.Banks);
                    if (reader.NextResult()) ReadLookup(reader, model.Accounts);
                }
            }
        }

        private static void ReadLookup(SqlDataReader reader, System.Collections.Generic.List<PaymentVoucherLookupItemViewModel> target)
        {
            while (reader.Read())
            {
                target.Add(new PaymentVoucherLookupItemViewModel
                {
                    Value = PaymentVoucherReadRepository.ReadString(reader, "Value"),
                    Text = PaymentVoucherReadRepository.ReadString(reader, "Text")
                });
            }
        }

        private static string Trim(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
