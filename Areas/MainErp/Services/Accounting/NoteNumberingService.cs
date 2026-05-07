using System;
using System.Data.SqlClient;
using MyERP.Areas.MainErp.Interfaces;
using MyERP.Areas.MainErp.Models.Accounting;

namespace MyERP.Areas.MainErp.Services.Accounting
{
    public class NoteNumberingService : INoteNumberingService
    {
        private readonly IMainErpDbConnectionFactory _connectionFactory;

        public NoteNumberingService(IMainErpDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public NumberingPreview PreviewNoteSerial(int branchId, DateTime noteDate)
        {
            return PreviewFromNotes("Notes_coding", branchId, noteDate, null, null);
        }

        public NumberingPreview PreviewVoucherSerial(int branchId, DateTime voucherDate, int sanadNo, int transactionType, int? billTo)
        {
            return PreviewFromNotes("Voucher_coding", branchId, voucherDate, sanadNo, transactionType);
        }

        private NumberingPreview PreviewFromNotes(string type, int branchId, DateTime date, int? sanadNo, int? transactionType)
        {
            var preview = new NumberingPreview
            {
                NumberingType = type,
                BranchId = branchId,
                Date = date
            };

            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                // Placeholder mirrors the visible Notes-based serial behavior safely for preview.
                // Final implementation must map sanad_numbering and user/branch options before reservation.
                using (var command = new SqlCommand(@"SELECT ISNULL(MAX(CAST(NoteSerial AS bigint)), 0) + 1
FROM Notes WITH (UPDLOCK, HOLDLOCK)
WHERE ISNULL(branch_no, 0) = @BranchId
  AND sanad_year = @Year", connection))
                {
                    command.Parameters.AddWithValue("@BranchId", branchId);
                    command.Parameters.AddWithValue("@Year", date.Year);
                    preview.NextNumber = Convert.ToInt64(command.ExecuteScalar());
                    preview.Success = true;
                    preview.Warning = "Preview only. Full VB6 sanad_numbering behavior still requires mapping before reservation.";
                }
            }

            return preview;
        }
    }
}
