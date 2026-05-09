using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using MyERP.Areas.MainErp.Interfaces;
using MyERP.Areas.MainErp.ViewModels.Payments;

namespace MyERP.Areas.MainErp.Repositories.Payments
{
    public class PaymentVoucherReadRepository
    {
        private readonly IMainErpDbConnectionFactory _connectionFactory;

        public PaymentVoucherReadRepository(IMainErpDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public PaymentVoucherSearchViewModel Search(DateTime? fromDate, DateTime? toDate, string serial, string party, int? branchId, string cashboxOrBank, decimal? amount, int page = 1, int pageSize = 50)
        {
            return SearchCore(5, fromDate, toDate, serial, party, branchId, cashboxOrBank, amount, page, pageSize);
        }

        public PaymentVoucherDetailsViewModel GetDetails(int id)
        {
            return GetDetailsCore(5, id);
        }

        protected PaymentVoucherSearchViewModel SearchCore(int noteType, DateTime? fromDate, DateTime? toDate, string serial, string party, int? branchId, string cashboxOrBank, decimal? amount, int page = 1, int pageSize = 50)
        {
            page = Math.Max(1, page);
            pageSize = Math.Max(10, Math.Min(200, pageSize));
            var model = new PaymentVoucherSearchViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                Serial = serial,
                Party = party,
                BranchId = branchId,
                CashboxOrBank = cashboxOrBank,
                Amount = amount,
                Page = page,
                PageSize = pageSize
            };

            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = CreateStoredProcedureCommand(connection, "dbo.usp_DynamicErpVoucher_Search"))
            {
                AddSearchParameters(command, noteType, fromDate, toDate, serial, party, branchId, cashboxOrBank, amount, page, pageSize);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (model.TotalCount == 0)
                        {
                            model.TotalCount = ReadInt(reader, "TotalRows");
                        }

                        var debit = ReadDecimal(reader, "DebitTotal");
                        var credit = ReadDecimal(reader, "CreditTotal");
                        model.Items.Add(new PaymentVoucherListItemViewModel
                        {
                            NoteId = ReadInt(reader, "NoteID"),
                            NoteDate = ReadDate(reader, "NoteDate"),
                            NoteSerial = ReadSerialString(reader, "NoteSerial"),
                            PartyDisplay = ReadString(reader, "PartyDisplay"),
                            BranchDisplay = ReadString(reader, "BranchDisplay"),
                            CashboxOrBankDisplay = ReadString(reader, "CashboxOrBankDisplay"),
                            Amount = ReadDecimal(reader, "Amount"),
                            Vat = ReadDecimal(reader, "Vat"),
                            Total = ReadDecimal(reader, "Total"),
                            BalancedStatus = Math.Abs(debit - credit) < 0.01m ? "متوازن" : "غير متوازن"
                        });
                    }
                }
            }

            if (model.TotalCount == 0)
            {
                model.TotalCount = model.Items.Count;
            }

            return model;
        }

        protected PaymentVoucherDetailsViewModel GetDetailsCore(int noteType, int id)
        {
            var model = new PaymentVoucherDetailsViewModel();
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                LoadHeader(connection, model, noteType, id);
                LoadRelatedNotes(connection, model, id);
                LoadAccounting(connection, model, id);
                ApplyFallbackPartyAccount(model, noteType);
                LoadAllocations(connection, model, noteType, id);
            }

            return model.Header.NoteId == 0 ? null : model;
        }

        private static void LoadHeader(SqlConnection connection, PaymentVoucherDetailsViewModel model, int noteType, int id)
        {
            using (var command = CreateStoredProcedureCommand(connection, "dbo.usp_DynamicErpVoucher_Header"))
            {
                command.Parameters.Add("@noteType", SqlDbType.Int).Value = noteType;
                command.Parameters.Add("@id", SqlDbType.Int).Value = id;
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return;
                    }

                    model.Header.NoteId = ReadInt(reader, "NoteID");
                    model.Header.NoteSerial = ReadSerialString(reader, "NoteSerial");
                    model.Header.NoteDate = ReadDate(reader, "NoteDate");
                    model.Header.ManualNo = ReadString(reader, "ManualNo");
                    model.Header.OrderNo = ReadString(reader, "OrderNo");
                    model.Header.PartyDisplay = ReadString(reader, "PartyDisplay");
                    model.Header.AccountDisplay = ReadString(reader, "AccountDisplay");
                    model.Header.BranchDisplay = ReadString(reader, "BranchDisplay");
                    model.Header.CashboxDisplay = ReadString(reader, "CashboxDisplay");
                    model.Header.BankDisplay = ReadString(reader, "BankDisplay");
                    model.Header.ChequeNumber = ReadString(reader, "ChequeNumber");
                    model.Header.ChequeDueDate = ReadDate(reader, "ChequeDueDate");
                    model.Header.CurrencyRate = ReadDecimal(reader, "CurrencyRate");
                    model.Header.CurrencyDisplay = ReadString(reader, "CurrencyDisplay");
                    model.Header.CostCenterDisplay = ReadString(reader, "CostCenterDisplay");
                    model.Header.ProjectDisplay = ReadString(reader, "ProjectDisplay");
                    model.Header.IncludeVat = ReadNullableInt(reader, "IncludVAT").GetValueOrDefault() != 0;
                    model.Header.PayDescription = ReadString(reader, "PayDes");
                    model.Header.PayDescription2 = ReadString(reader, "PayDes1");
                    model.Header.PaymentMethodDisplay = MapPaymentType(ReadNullableInt(reader, "NoteCashingType") ?? ReadNullableInt(reader, "PaymentType"));
                    model.Header.CashingTypeDisplay = MapCashingType(noteType, ReadNullableInt(reader, "CashingType"), ReadNullableInt(reader, "NoteCashingType"));
                    model.Header.ReceiptClassDisplay = MapReceiptClass(ReadNullableInt(reader, "NCashingType"));
                    model.Header.Remark = ReadString(reader, "Remark");
                    model.Header.Amount = ReadDecimal(reader, "Amount");
                    model.Header.Vat = ReadDecimal(reader, "Vat");
                    model.Header.Total = ReadDecimal(reader, "Total");
                    model.Header.VoucherId = ReadNullableInt(reader, "Double_Entry_Vouchers_ID");
                    model.Header.ReportName = ReadString(reader, "ReportName");
                }
            }
        }

        private static void LoadAccounting(SqlConnection connection, PaymentVoucherDetailsViewModel model, int id)
        {
            using (var command = CreateStoredProcedureCommand(connection, "dbo.usp_DynamicErpVoucher_Accounting"))
            {
                command.Parameters.Add("@id", SqlDbType.Int).Value = id;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var line = new PaymentVoucherAccountingLineViewModel
                        {
                            VoucherId = ReadInt(reader, "Double_Entry_Vouchers_ID"),
                            LineNo = ReadInt(reader, "DEV_ID_Line_No"),
                            RecordDate = ReadDate(reader, "RecordDate"),
                            AccountDisplay = ReadString(reader, "AccountDisplay"),
                            Debit = ReadDecimal(reader, "Debit"),
                            Credit = ReadDecimal(reader, "Credit"),
                            Description = ReadString(reader, "Double_Entry_Vouchers_Description"),
                            BranchId = ReadNullableInt(reader, "branch_id")
                        };
                        model.DebitTotal += line.Debit;
                        model.CreditTotal += line.Credit;
                        model.AccountingEntries.Add(line);
                    }
                }
            }
        }

        private static void ApplyFallbackPartyAccount(PaymentVoucherDetailsViewModel model, int noteType)
        {
            if (model == null || model.Header == null || !string.IsNullOrWhiteSpace(model.Header.AccountDisplay))
            {
                return;
            }

            PaymentVoucherAccountingLineViewModel candidate = null;
            foreach (var line in model.AccountingEntries)
            {
                if (line == null || string.IsNullOrWhiteSpace(line.AccountDisplay))
                {
                    continue;
                }

                if (noteType == 5 && line.Debit > 0)
                {
                    candidate = line;
                    break;
                }

                if (noteType == 4 && line.Credit > 0)
                {
                    candidate = line;
                    break;
                }

                if (candidate == null)
                {
                    candidate = line;
                }
            }

            if (candidate != null)
            {
                model.Header.AccountDisplay = candidate.AccountDisplay;
            }
        }

        private static void LoadRelatedNotes(SqlConnection connection, PaymentVoucherDetailsViewModel model, int id)
        {
            using (var command = CreateStoredProcedureCommand(connection, "dbo.usp_DynamicErpVoucher_RelatedNotes"))
            {
                command.Parameters.Add("@id", SqlDbType.Int).Value = id;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        model.RelatedNotes.Add(new PaymentVoucherNoteTraceViewModel
                        {
                            NoteId = ReadInt(reader, "NoteID"),
                            NoteSerial = ReadSerialString(reader, "NoteSerial"),
                            NoteType = ReadNullableInt(reader, "NoteType"),
                            NoteDate = ReadDate(reader, "NoteDate"),
                            Amount = ReadDecimal(reader, "Amount"),
                            Remark = ReadString(reader, "Remark")
                        });
                    }
                }
            }
        }

        private static void LoadAllocations(SqlConnection connection, PaymentVoucherDetailsViewModel model, int noteType, int id)
        {
            using (var command = CreateStoredProcedureCommand(connection, "dbo.usp_DynamicErpVoucher_Allocations"))
            {
                command.Parameters.Add("@noteType", SqlDbType.Int).Value = noteType;
                command.Parameters.Add("@id", SqlDbType.Int).Value = id;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        model.Allocations.Add(new PaymentVoucherAllocationLineViewModel
                        {
                            Source = ReadString(reader, "Source"),
                            SourceDisplay = MapAllocationSource(ReadString(reader, "Source")),
                            SourceKind = MapAllocationKind(ReadString(reader, "Source")),
                            Serial = ReadSerialString(reader, "Serial"),
                            Date = ReadDate(reader, "Date"),
                            OriginalValue = ReadDecimal(reader, "OriginalValue"),
                            PaidValue = ReadDecimal(reader, "PaidValue"),
                            RemainingValue = ReadDecimal(reader, "RemainingValue"),
                            Description = ReadString(reader, "Description")
                        });
                    }
                }
            }
        }

        private static void AddSearchParameters(SqlCommand command, int noteType, DateTime? fromDate, DateTime? toDate, string serial, string party, int? branchId, string cashboxOrBank, decimal? amount, int page, int pageSize)
        {
            command.Parameters.Add("@noteType", SqlDbType.Int).Value = noteType;
            command.Parameters.Add("@fromDate", SqlDbType.DateTime).Value = (object)fromDate ?? DBNull.Value;
            command.Parameters.Add("@toDate", SqlDbType.DateTime).Value = (object)toDate ?? DBNull.Value;
            command.Parameters.Add("@serial", SqlDbType.NVarChar, 50).Value = (serial ?? string.Empty).Trim();
            command.Parameters.Add("@party", SqlDbType.NVarChar, 200).Value = (party ?? string.Empty).Trim();
            command.Parameters.Add("@branchId", SqlDbType.Int).Value = (object)branchId ?? DBNull.Value;
            command.Parameters.Add("@cashboxOrBank", SqlDbType.NVarChar, 200).Value = (cashboxOrBank ?? string.Empty).Trim();
            command.Parameters.Add("@amount", SqlDbType.Decimal).Value = (object)amount ?? DBNull.Value;
            command.Parameters.Add("@pageNumber", SqlDbType.Int).Value = page;
            command.Parameters.Add("@pageSize", SqlDbType.Int).Value = pageSize;
        }

        private static SqlCommand CreateStoredProcedureCommand(SqlConnection connection, string procedureName)
        {
            return new SqlCommand(procedureName, connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 60
            };
        }

        private static string MapPaymentType(int? paymentType)
        {
            if (!paymentType.HasValue)
            {
                return string.Empty;
            }

            switch (paymentType.Value)
            {
                case 0: return "نقدي";
                case 1: return "شيك";
                case 2: return "تحويل بنكي";
                case 3: return "شيك مؤجل";
                case 4: return "حساب";
                case 5: return "دفع آجل";
                default: return "طريقة دفع " + paymentType.Value;
            }
        }

        private static string MapCashingType(int noteType, int? cashingType, int? noteCashingType)
        {
            if (!cashingType.HasValue && !noteCashingType.HasValue)
            {
                return string.Empty;
            }

            if (noteType == 5)
            {
                switch (cashingType.GetValueOrDefault(-1))
                {
                    case 0: return "صرف لعميل";
                    case 1: return "صرف لمورد";
                    case 2: return "مقاول باطن";
                    case 3: return "صرف لمشروع";
                    case 4: return "صرف لموظف";
                    case 5: return "صرف لحساب";
                    case 6: return "رواتب";
                    case 7: return "دفعات مقدمة";
                    case 8: return "مستحق إجازة";
                    case 9: return "صرف لمورد";
                    case 10: return "نهاية خدمة";
                    case 11: return "بدلات";
                    case 12: return "نوع صرف إضافي";
                    case 13: return "تحويلات بنكية مدينة";
                    case 14: return "تحويلات بنكية دائنة";
                    default: return "نوع صرف " + cashingType.Value;
                }
            }

            switch (cashingType.GetValueOrDefault(-1))
            {
                case 0: return "قبض من عميل";
                case 1: return "قبض من مورد";
                case 2: return "مقاول باطن";
                case 3: return "إيرادات أخرى";
                case 4: return "دفعة مقدمة";
                case 5: return "مشروعات";
                default: return "نوع قبض " + cashingType.Value;
            }
        }

        private static string MapReceiptClass(int? nCashingType)
        {
            if (!nCashingType.HasValue)
            {
                return string.Empty;
            }

            switch (nCashingType.Value)
            {
                case 0: return "غير مصنف";
                case 1: return "سكني";
                case 2: return "تجاري";
                case 3: return "إداري";
                case 7: return "تصنيف خاص";
                default: return "تصنيف " + nCashingType.Value;
            }
        }

        private static string MapAllocationSource(string source)
        {
            switch ((source ?? string.Empty).Trim())
            {
                case "TblNotesBillBuyPayment":
                    return "فواتير مشتريات";
                case "TblNotesBillProjectPayment":
                    return "فواتير مشاريع";
                case "TblNotesBillVindorPayment":
                    return "فواتير موردين";
                case "ContracttBillInstallmentsDone":
                case "TblContractInstallments by contract":
                    return "أقساط عقارية";
                case "TblAqarCommissions":
                    return "عمولات عقارية";
                case "TblNotesSales":
                    return "مبيعات مرتبطة";
                case "TblUnitNoInformation":
                    return "بيانات وحدة";
                case "TblAqrEarnest":
                    return "عربون عقار";
                case "TblOtheExpensAqar":
                    return "مصروفات عقارية";
                case "TblFiterWaiver":
                    return "تنازل / تسوية";
                default:
                    return string.IsNullOrWhiteSpace(source) ? "تخصيص غير مصنف" : source;
            }
        }

        private static string MapAllocationKind(string source)
        {
            switch ((source ?? string.Empty).Trim())
            {
                case "TblNotesBillBuyPayment":
                    return "purchase";
                case "TblNotesBillProjectPayment":
                    return "project";
                case "TblNotesBillVindorPayment":
                    return "vendor";
                case "ContracttBillInstallmentsDone":
                case "TblContractInstallments by contract":
                case "TblAqarCommissions":
                case "TblNotesSales":
                case "TblUnitNoInformation":
                case "TblAqrEarnest":
                case "TblOtheExpensAqar":
                case "TblFiterWaiver":
                    return "real-estate";
                default:
                    return "other";
            }
        }

        public static string ReadString(SqlDataReader reader, string name)
        {
            var value = reader[name];
            return value == DBNull.Value ? string.Empty : Convert.ToString(value);
        }

        public static string ReadSerialString(SqlDataReader reader, string name)
        {
            var value = reader[name];
            if (value == DBNull.Value)
            {
                return string.Empty;
            }

            if (value is decimal || value is double || value is float)
            {
                return Convert.ToDecimal(value, CultureInfo.InvariantCulture).ToString("0.############################", CultureInfo.InvariantCulture);
            }

            var text = Convert.ToString(value, CultureInfo.InvariantCulture);
            decimal numericSerial;
            if (!string.IsNullOrWhiteSpace(text)
                && text.IndexOf("e", StringComparison.OrdinalIgnoreCase) >= 0
                && decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out numericSerial))
            {
                return numericSerial.ToString("0.############################", CultureInfo.InvariantCulture);
            }

            return text;
        }

        public static int ReadInt(SqlDataReader reader, string name)
        {
            var value = reader[name];
            return value == DBNull.Value ? 0 : Convert.ToInt32(value);
        }

        public static int? ReadNullableInt(SqlDataReader reader, string name)
        {
            var value = reader[name];
            return value == DBNull.Value ? (int?)null : Convert.ToInt32(value);
        }

        public static DateTime? ReadDate(SqlDataReader reader, string name)
        {
            var value = reader[name];
            return value == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(value);
        }

        public static decimal ReadDecimal(SqlDataReader reader, string name)
        {
            var value = reader[name];
            return value == DBNull.Value ? 0m : Convert.ToDecimal(value);
        }
    }
}
