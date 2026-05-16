using System.Collections.Generic;

namespace MyERP.Areas.MainErp.ViewModels.Branches
{
    public class BranchesIndexViewModel
    {
        public BranchesIndexViewModel()
        {
            Results = new List<BranchListItemViewModel>();
            Selected = new BranchEditViewModel();
            Accounts = new List<BranchAccountLookupViewModel>();
            Employees = new List<BranchLookupViewModel>();
            ActivityTypes = new List<BranchLookupViewModel>();
            AccountDefinitions = BranchAccountDefinition.All;
            Permissions = new BranchPermissionsViewModel();
        }

        public string SearchText { get; set; }
        public IList<BranchListItemViewModel> Results { get; set; }
        public BranchEditViewModel Selected { get; set; }
        public IList<BranchAccountLookupViewModel> Accounts { get; set; }
        public IList<BranchLookupViewModel> Employees { get; set; }
        public IList<BranchLookupViewModel> ActivityTypes { get; set; }
        public IList<BranchAccountDefinition> AccountDefinitions { get; set; }
        public BranchPermissionsViewModel Permissions { get; set; }
    }

    public class BranchPermissionsViewModel
    {
        public bool CanView { get; set; }
        public bool CanAdd { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }

    public class BranchListItemViewModel
    {
        public int Id { get; set; }
        public string NameAr { get; set; }
        public string NameEn { get; set; }
        public string Phone { get; set; }
        public string ManagerName { get; set; }
        public string ActivityName { get; set; }
    }

    public class BranchEditViewModel
    {
        public BranchEditViewModel()
        {
            Accounts = new Dictionary<string, string>();
        }

        public int? BranchId { get; set; }
        public string NameAr { get; set; }
        public string NameEn { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public int? ManagerId { get; set; }
        public int? ActivityTypeId { get; set; }
        public IDictionary<string, string> Accounts { get; set; }
    }

    public class BranchLookupViewModel
    {
        public string Id { get; set; }
        public string Text { get; set; }
    }

    public class BranchAccountLookupViewModel
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public bool IsLastAccount { get; set; }
        public string Display
        {
            get { return string.IsNullOrWhiteSpace(Code) ? Name : Code + " - " + Name; }
        }
    }

    public class BranchAccountDefinition
    {
        public BranchAccountDefinition(int index, string fieldName, string label, string category)
        {
            Index = index;
            FieldName = fieldName;
            Label = label;
            Category = category;
        }

        public int Index { get; private set; }
        public string FieldName { get; private set; }
        public string Label { get; private set; }
        public string Category { get; private set; }

        public static readonly IList<BranchAccountDefinition> All = new List<BranchAccountDefinition>
        {
            new BranchAccountDefinition(0, "a0", "حساب المخزون", "المخازن والمبيعات والمشتريات"),
            new BranchAccountDefinition(10, "a10", "خسائر فقد وتلف", "المخازن والمبيعات والمشتريات"),
            new BranchAccountDefinition(11, "a11", "التسويات الجردية", "المخازن والمبيعات والمشتريات"),
            new BranchAccountDefinition(1, "a1", "حساب تكلفة المبيعات", "المخازن والمبيعات والمشتريات"),
            new BranchAccountDefinition(2, "a2", "حساب المبيعات", "المخازن والمبيعات والمشتريات"),
            new BranchAccountDefinition(3, "a3", "حساب مردودات المبيعات", "المخازن والمبيعات والمشتريات"),
            new BranchAccountDefinition(4, "a4", "حساب المشتريات", "المخازن والمبيعات والمشتريات"),
            new BranchAccountDefinition(5, "a5", "حساب مردودات المشتريات", "المخازن والمبيعات والمشتريات"),
            new BranchAccountDefinition(12, "a12", "خصم مسموح به", "المخازن والمبيعات والمشتريات"),
            new BranchAccountDefinition(13, "a13", "خصم مكتسب", "المخازن والمبيعات والمشتريات"),
            new BranchAccountDefinition(17, "a17", "هدايا وعينات", "المخازن والمبيعات والمشتريات"),
            new BranchAccountDefinition(23, "a23", "إيرادات الخدمات", "المخازن والمبيعات والمشتريات"),
            new BranchAccountDefinition(8, "a8", "حساب العملاء", "الذمم والتحصيل"),
            new BranchAccountDefinition(9, "a9", "حساب الموردين", "الذمم والتحصيل"),
            new BranchAccountDefinition(93, "a93", "ديون معدومة", "الذمم والتحصيل"),
            new BranchAccountDefinition(139, "a139", "حساب إشعار مدين", "الذمم والتحصيل"),
            new BranchAccountDefinition(140, "a140", "حساب إشعار دائن", "الذمم والتحصيل"),
            new BranchAccountDefinition(6, "a6", "حساب الصندوق", "الخزن والبنوك"),
            new BranchAccountDefinition(20, "a20", "حساب البنوك", "الخزن والبنوك"),
            new BranchAccountDefinition(21, "a22", "عجز في النقدية", "الخزن والبنوك"),
            new BranchAccountDefinition(22, "a21", "زيادة في النقدية", "الخزن والبنوك"),
            new BranchAccountDefinition(35, "a35", "حساب العهد", "الخزن والبنوك"),
            new BranchAccountDefinition(50, "a50", "عمولة البنوك", "الخزن والبنوك"),
            new BranchAccountDefinition(51, "a51", "الاعتمادات المستندية", "الخزن والبنوك"),
            new BranchAccountDefinition(52, "a52", "مصروفات بنكية", "الخزن والبنوك"),
            new BranchAccountDefinition(24, "a24", "حساب الأصل", "الأصول"),
            new BranchAccountDefinition(25, "a25", "مصروف الإهلاك", "الأصول"),
            new BranchAccountDefinition(26, "a26", "مجمع الإهلاك", "الأصول"),
            new BranchAccountDefinition(31, "a31", "أرباح بيع أصول ثابتة", "الأصول"),
            new BranchAccountDefinition(40, "a40", "خسائر بيع أصول ثابتة", "الأصول"),
            new BranchAccountDefinition(14, "a14", "مصروفات المشاريع", "المشاريع"),
            new BranchAccountDefinition(15, "a15", "إيرادات المشاريع", "المشاريع"),
            new BranchAccountDefinition(27, "a27", "مواد المشاريع", "المشاريع"),
            new BranchAccountDefinition(28, "a28", "أجور المشاريع", "المشاريع"),
            new BranchAccountDefinition(32, "a32", "مستخلصات المشاريع", "المشاريع"),
            new BranchAccountDefinition(36, "a36", "مقاولو الباطن", "المشاريع"),
            new BranchAccountDefinition(99, "a99", "حسميات مستخلصات", "المشاريع"),
            new BranchAccountDefinition(100, "a100", "دفعات مقدمة عملاء", "المشاريع"),
            new BranchAccountDefinition(136, "a136", "مشروعات تحت التنفيذ", "المشاريع"),
            new BranchAccountDefinition(7, "a7", "ذمم الموظفين", "الموظفين والرواتب"),
            new BranchAccountDefinition(16, "a16", "حساب الأجور", "الموظفين والرواتب"),
            new BranchAccountDefinition(29, "a29", "الأجور المستحقة", "الموظفين والرواتب"),
            new BranchAccountDefinition(30, "a30", "مخصص الإجازات", "الموظفين والرواتب"),
            new BranchAccountDefinition(53, "a53", "حساب الخصم", "الموظفين والرواتب"),
            new BranchAccountDefinition(54, "a54", "حساب المكافأة", "الموظفين والرواتب"),
            new BranchAccountDefinition(55, "a55", "مصروف الإجازة", "الموظفين والرواتب"),
            new BranchAccountDefinition(56, "a56", "مصروف ترك الخدمة", "الموظفين والرواتب"),
            new BranchAccountDefinition(72, "a72", "مخصص نهاية الخدمة", "الموظفين والرواتب"),
            new BranchAccountDefinition(37, "a37", "مصاريف الإنتاج مواد", "الإنتاج"),
            new BranchAccountDefinition(38, "a38", "مصاريف الإنتاج أجور", "الإنتاج"),
            new BranchAccountDefinition(39, "a39", "مصاريف الإنتاج وقود", "الإنتاج"),
            new BranchAccountDefinition(66, "a66", "تشغيل إنتاج نصف مصنع", "الإنتاج"),
            new BranchAccountDefinition(75, "a75", "مصاريف الإنتاج كهرباء", "الإنتاج"),
            new BranchAccountDefinition(144, "a144", "مصاريف صناعية", "الإنتاج"),
            new BranchAccountDefinition(69, "a69", "إيرادات عمولات", "قطاع النقل"),
            new BranchAccountDefinition(70, "a70", "جاري الفروع", "قطاع النقل"),
            new BranchAccountDefinition(95, "a95", "تحويلات مدينة شحن", "قطاع النقل"),
            new BranchAccountDefinition(96, "a96", "تحويلات دائنة شحن", "قطاع النقل"),
            new BranchAccountDefinition(101, "a101", "إيرادات النقل", "قطاع النقل"),
            new BranchAccountDefinition(102, "a102", "تكلفة النقل", "قطاع النقل"),
            new BranchAccountDefinition(103, "a103", "دفعات المتعهدين المستحقة", "قطاع النقل"),
            new BranchAccountDefinition(47, "a47", "حساب الملاك", "العقارات والمساهمات"),
            new BranchAccountDefinition(48, "a48", "حساب المستأجرين والمشترين", "العقارات والمساهمات"),
            new BranchAccountDefinition(80, "a80", "إيراد السعي والعمولات", "العقارات والمساهمات"),
            new BranchAccountDefinition(82, "a82", "إيرادات الإيجارات", "العقارات والمساهمات"),
            new BranchAccountDefinition(104, "a104", "حساب المساهمين أراضي", "العقارات والمساهمات"),
            new BranchAccountDefinition(106, "a106", "حساب المساهمين عقارات", "العقارات والمساهمات"),
            new BranchAccountDefinition(107, "a107", "حساب المساهمات أراضي", "العقارات والمساهمات"),
            new BranchAccountDefinition(108, "a108", "حساب المساهمات عقارات", "العقارات والمساهمات"),
            new BranchAccountDefinition(109, "a109", "مبيعات أراضي", "العقارات والمساهمات"),
            new BranchAccountDefinition(110, "a110", "مبيعات عقارات", "العقارات والمساهمات"),
            new BranchAccountDefinition(111, "a111", "مشتريات أراضي", "العقارات والمساهمات"),
            new BranchAccountDefinition(112, "a112", "مشتريات عقارات", "العقارات والمساهمات"),
            new BranchAccountDefinition(113, "a113", "أرباح أراضي", "العقارات والمساهمات"),
            new BranchAccountDefinition(114, "a114", "أرباح عقارات", "العقارات والمساهمات"),
            new BranchAccountDefinition(115, "a115", "خسائر أراضي", "العقارات والمساهمات"),
            new BranchAccountDefinition(116, "a116", "خسائر عقارات", "العقارات والمساهمات"),
            new BranchAccountDefinition(42, "a42", "زيادة ونقص الأسهم - ميزانية", "الأسهم"),
            new BranchAccountDefinition(43, "a43", "مشتريات الأسهم - ميزانية", "الأسهم"),
            new BranchAccountDefinition(44, "a44", "مبيعات الأسهم", "الأسهم"),
            new BranchAccountDefinition(45, "a45", "زيادة ونقص الأسهم - أ خ", "الأسهم"),
            new BranchAccountDefinition(19, "a19", "وسيط افتتاحي مخزون", "الحسابات الافتتاحية"),
            new BranchAccountDefinition(41, "a41", "وسيط افتتاحي أصول ثابتة", "الحسابات الافتتاحية"),
            new BranchAccountDefinition(57, "a57", "وسيط افتتاحي خزن وعهد", "الحسابات الافتتاحية"),
            new BranchAccountDefinition(58, "a58", "وسيط افتتاحي بنوك", "الحسابات الافتتاحية"),
            new BranchAccountDefinition(59, "a59", "وسيط افتتاحي عملاء", "الحسابات الافتتاحية"),
            new BranchAccountDefinition(60, "a60", "وسيط افتتاحي موردين", "الحسابات الافتتاحية"),
            new BranchAccountDefinition(61, "a61", "وسيط افتتاحي موظفين", "الحسابات الافتتاحية"),
            new BranchAccountDefinition(62, "a62", "وسيط الاعتمادات المستندية", "الحسابات الافتتاحية"),
            new BranchAccountDefinition(71, "a71", "وسيط افتتاحي المشاريع", "الحسابات الافتتاحية"),
            new BranchAccountDefinition(33, "a33", "المصروفات", "أخرى"),
            new BranchAccountDefinition(34, "a34", "الإيرادات", "أخرى"),
            new BranchAccountDefinition(49, "a49", "أرباح وخسائر عام", "أخرى"),
            new BranchAccountDefinition(63, "a63", "إيرادات التقسيط", "أخرى"),
            new BranchAccountDefinition(73, "a73", "إيرادات الصيانة", "أخرى"),
            new BranchAccountDefinition(74, "a74", "مصروفات الصيانة", "أخرى"),
            new BranchAccountDefinition(138, "a138", "هيئة الزكاة - القيمة المضافة", "أخرى"),
            new BranchAccountDefinition(141, "a141", "القيمة المضافة للجمارك", "أخرى")
        };
    }

    public class BranchSaveResult
    {
        public bool Success { get; set; }
        public int Id { get; set; }
        public string Message { get; set; }
    }
}
