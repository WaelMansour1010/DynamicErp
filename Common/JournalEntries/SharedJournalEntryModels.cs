using System;
using System.Collections.Generic;

namespace MyERP.Common.JournalEntries
{
    public sealed class SharedJournalProfile
    {
        public SharedJournalEntryMode Mode { get; set; }
        public string HeaderTable { get; set; }
        public string DetailTable { get; set; }
        public int ManualNoteType { get; set; }
        public string ManualNoteTypeName { get; set; }
        public bool IsOpeningBalance { get; set; }
        public bool AllowsAutomaticOverride { get; set; }

        public static SharedJournalProfile ForMode(SharedJournalEntryMode mode)
        {
            if (mode == SharedJournalEntryMode.OpeningBalance)
            {
                return new SharedJournalProfile
                {
                    Mode = mode,
                    HeaderTable = "Notes1",
                    DetailTable = "DOUBLE_ENTREY_VOUCHERS1",
                    ManualNoteType = 101,
                    ManualNoteTypeName = "قيد افتتاحي",
                    IsOpeningBalance = true,
                    AllowsAutomaticOverride = false
                };
            }

            return new SharedJournalProfile
            {
                Mode = SharedJournalEntryMode.Normal,
                HeaderTable = "Notes",
                DetailTable = "DOUBLE_ENTREY_VOUCHERS",
                ManualNoteType = 57,
                ManualNoteTypeName = "سند قيد تسوية يدوي",
                IsOpeningBalance = false,
                AllowsAutomaticOverride = true
            };
        }
    }

    public class SharedJournalSearchRequest
    {
        public string VoucherNo { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string AccountCode { get; set; }
        public string AccountCodes { get; set; }
        public string Description { get; set; }
        public int? BranchId { get; set; }
    }

    public class SharedManualJournalSaveRequest
    {
        public int? NoteId { get; set; }
        public DateTime NoteDate { get; set; }
        public int? BranchId { get; set; }
        public string Description { get; set; }
        public string AdminPassword { get; set; }
        public IList<SharedManualJournalLineDto> Lines { get; set; }

        public SharedManualJournalSaveRequest()
        {
            Lines = new List<SharedManualJournalLineDto>();
        }
    }

    public class SharedManualJournalLineDto
    {
        public string AccountCode { get; set; }
        public string AccountSerial { get; set; }
        public string AccountName { get; set; }
        public string Description { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public int? OpeningBalanceVoucherId { get; set; }
    }

    public class SharedJournalHeaderDto
    {
        public int NoteId { get; set; }
        public string NoteSerial { get; set; }
        public string NoteSerial1 { get; set; }
        public DateTime? NoteDate { get; set; }
        public int? BranchId { get; set; }
        public string BranchName { get; set; }
        public string Description { get; set; }
        public bool IsManual { get; set; }
        public string EntryKind { get; set; }
        public int? NoteType { get; set; }
        public string NoteTypeName { get; set; }
        public int? AutoSourceId { get; set; }
        public string AutoSourceName { get; set; }
        public string AutoSourceUrl { get; set; }
        public decimal DebitTotal { get; set; }
        public decimal CreditTotal { get; set; }
        public IList<SharedManualJournalLineDto> Lines { get; set; }

        public SharedJournalHeaderDto()
        {
            Lines = new List<SharedManualJournalLineDto>();
        }
    }

    public class SharedAccountTreeDto
    {
        public string AccountCode { get; set; }
        public string AccountSerial { get; set; }
        public string AccountName { get; set; }
        public string ParentAccountCode { get; set; }
        public bool IsLastAccount { get; set; }
        public bool HasChildren { get; set; }
        public IList<SharedAccountTreeDto> Children { get; set; }

        public SharedAccountTreeDto()
        {
            Children = new List<SharedAccountTreeDto>();
        }
    }

    public class SharedLookupDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Extra { get; set; }
    }

    public class SharedBranchDto
    {
        public int BranchId { get; set; }
        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public string BranchNameEnglish { get; set; }
    }

    public class SharedJournalWorkspaceViewModel
    {
        public string Title { get; set; }
        public string Intro { get; set; }
        public string CommandTitle { get; set; }
        public string CommandIntro { get; set; }
        public string SearchUrl { get; set; }
        public string GetUrl { get; set; }
        public string SaveUrl { get; set; }
        public string AccountLookupUrl { get; set; }
        public string AccountTreeUrl { get; set; }
        public bool CanCreate { get; set; }
        public bool CanEdit { get; set; }
        public bool IsAdmin { get; set; }
        public int? SelectedBranchId { get; set; }
        public IList<SharedBranchDto> Branches { get; set; }

        public SharedJournalWorkspaceViewModel()
        {
            Branches = new List<SharedBranchDto>();
        }
    }
}
