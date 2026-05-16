using System;
using System.Collections.Generic;

namespace MyERP.Common.JournalEntries
{
    public enum SharedJournalEntryMode
    {
        Normal = 0,
        OpeningBalance = 1
    }

    public sealed class SharedJournalEntryContext
    {
        public SharedJournalEntryContext()
        {
            Messages = new List<string>();
        }

        public string AreaName { get; set; }
        public SharedJournalEntryMode Mode { get; set; }
        public string ScreenTitle { get; set; }
        public string ScreenIntro { get; set; }
        public string ModeBadge { get; set; }
        public bool IsReadOnly { get; set; }
        public bool IsPostingEnabled { get; set; }
        public bool RequiresVb6OpeningBalanceTrace { get; set; }
        public IList<string> Messages { get; private set; }
    }

    public sealed class SharedJournalEntrySearchCriteria
    {
        public string SearchText { get; set; }
        public int? BranchId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public SharedJournalEntryMode Mode { get; set; }
    }

    public sealed class SharedJournalEntryCore
    {
        public SharedJournalEntryContext BuildContext(string areaName, SharedJournalEntryMode mode)
        {
            var context = new SharedJournalEntryContext
            {
                AreaName = areaName,
                Mode = mode,
                IsReadOnly = true,
                IsPostingEnabled = false
            };

            if (mode == SharedJournalEntryMode.OpeningBalance)
            {
                context.ScreenTitle = "قيد افتتاحي";
                context.ScreenIntro = "وضع القيد الافتتاحي متاح كسياق آمن فقط حتى اكتمال تتبع قواعد Main Original VB6 قبل أي ترحيل أو حفظ مالي.";
                context.ModeBadge = "OpeningBalance";
                context.RequiresVb6OpeningBalanceTrace = true;
                context.Messages.Add("تم تتبع FrmAccEditJournal1.frm في Main Original VB6 كمرجع القيد الافتتاحي، لكن منطق الحفظ ما زال مغلقا حتى اكتمال مقارنة Notes و Notes1 بأمان.");
                return context;
            }

            context.ScreenTitle = "اليومية العامة";
            context.ScreenIntro = "شاشة القيود تعمل الآن عبر هيكل shared journal core مع بقاء الحفظ المالي مغلقا في MainErp إلى أن يتم نقل السلوك بأمان.";
            context.ModeBadge = "SharedCore";
            return context;
        }

        public SharedJournalEntrySearchCriteria BuildSearchCriteria(
            SharedJournalEntryMode mode,
            string searchText,
            int? branchId,
            DateTime? fromDate,
            DateTime? toDate,
            int page,
            int pageSize)
        {
            return new SharedJournalEntrySearchCriteria
            {
                Mode = mode,
                SearchText = searchText,
                BranchId = branchId,
                FromDate = fromDate,
                ToDate = toDate,
                Page = page < 1 ? 1 : page,
                PageSize = pageSize < 1 ? 25 : pageSize
            };
        }
    }
}
