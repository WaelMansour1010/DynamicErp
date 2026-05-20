using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MyERP.Areas.MainErp.ViewModels.Projects
{
    public class ProjectsIndexViewModel
    {
        public ProjectsIndexViewModel()
        {
            Items = new List<ProjectListItemViewModel>();
            Statuses = new List<ProjectLookupItem>();
            Branches = new List<ProjectLookupItem>();
        }

        public string SearchText { get; set; }
        public int? StatusId { get; set; }
        public int? BranchId { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public string Warning { get; set; }
        public string SuccessMessage { get; set; }
        public IList<ProjectListItemViewModel> Items { get; private set; }
        public IList<ProjectLookupItem> Statuses { get; private set; }
        public IList<ProjectLookupItem> Branches { get; private set; }
    }

    public class ProjectListItemViewModel
    {
        public int Id { get; set; }
        public string FullCode { get; set; }
        public string ProjectName { get; set; }
        public string ProjectNameEnglish { get; set; }
        public string CustomerName { get; set; }
        public string StatusName { get; set; }
        public string BranchName { get; set; }
        public decimal? ProjectCost { get; set; }
        public decimal? NetCost { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class ProjectEditViewModel
    {
        public ProjectEditViewModel()
        {
            StartDate = DateTime.Today;
            EndDate = DateTime.Today;
            ProjectCost = 0m;
            GeneralDiscount = 0m;
            DiscountPercentage = 0m;
            CurrencyId = 1;
            StatusId = 1;
            PState = 0;
            UnderImplementation = 0;
            Statuses = new List<ProjectLookupItem>();
            Branches = new List<ProjectLookupItem>();
            Currencies = new List<ProjectLookupItem>();
            ContractTypes = new List<ProjectLookupItem>();
            Customers = new List<ProjectLookupItem>();
            Accounts = new List<ProjectLookupItem>();
            Employees = new List<ProjectLookupItem>();
            ProjectItems = new List<ProjectMainDesViewModel>();
        }

        public int Id { get; set; }
        public bool IsNewProject { get; set; }

        [StringLength(50, ErrorMessage = "كود المشروع لا يزيد عن 50 حرف.")]
        public string Code { get; set; }

        [StringLength(50, ErrorMessage = "بادئة الكود لا تزيد عن 50 حرف.")]
        public string Prefix { get; set; }

        public string FullCode { get; set; }

        [Required(ErrorMessage = "لابد من تحديد اسم المشروع العربي.")]
        [StringLength(4000, ErrorMessage = "اسم المشروع طويل جدًا.")]
        public string ProjectName { get; set; }

        [StringLength(4000, ErrorMessage = "اسم المشروع الإنجليزي طويل جدًا.")]
        public string ProjectNameEnglish { get; set; }

        [Required(ErrorMessage = "لابد من تحديد العميل النهائي.")]
        public int? EndUserId { get; set; }

        public string EndUserAccount { get; set; }
        public int? SubContractorId { get; set; }
        public string SubContractorAccount { get; set; }

        [Required(ErrorMessage = "حدد الفرع أولًا.")]
        public int? BranchNo { get; set; }

        [Required(ErrorMessage = "حدد حالة المشروع.")]
        public int? StatusId { get; set; }

        public string ContractType { get; set; }
        public int? CurrencyId { get; set; }

        [Range(0, 999999999999, ErrorMessage = "قيمة المشروع يجب أن تكون رقمًا موجبًا.")]
        public decimal? ProjectCost { get; set; }

        [Range(0, 999999999999, ErrorMessage = "قيمة الخصم يجب أن تكون رقمًا موجبًا.")]
        public decimal? GeneralDiscount { get; set; }

        [Range(0, 100, ErrorMessage = "نسبة الخصم يجب أن تكون بين 0 و 100.")]
        public decimal? DiscountPercentage { get; set; }

        public decimal? CostAfterDiscount { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? NearEndDate { get; set; }
        public int? ManagerEmployeeId { get; set; }
        public int? SalesEmployeeId { get; set; }
        public int? DepartmentId { get; set; }
        public int? PState { get; set; }
        public int? UnderImplementation { get; set; }
        public string ContractNo { get; set; }
        public double? Insurance { get; set; }
        public string Remarks { get; set; }

        public IList<ProjectLookupItem> Statuses { get; private set; }
        public IList<ProjectLookupItem> Branches { get; private set; }
        public IList<ProjectLookupItem> Currencies { get; private set; }
        public IList<ProjectLookupItem> ContractTypes { get; private set; }
        public IList<ProjectLookupItem> Customers { get; private set; }
        public IList<ProjectLookupItem> Accounts { get; private set; }
        public IList<ProjectLookupItem> Employees { get; private set; }

        public string AccountUnderImp { get; set; }
        public IList<ProjectMainDesViewModel> ProjectItems { get; set; }

        public IList<ProjectMainDesViewModel> ProjectMainDes
        {
            get { return ProjectItems; }
            set { ProjectItems = value; }
        }
    }

    public class ProjectMainDesViewModel
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string Item { get; set; }
        public string ItemName
        {
            get { return Item; }
            set { Item = value; }
        }
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public decimal Value { get; set; }
        public string Remarks { get; set; }
    }

    public class ProjectLookupItem
    {
        public string Value { get; set; }
        public string Text { get; set; }
    }

    public class ProjectExtractCreateViewModel
    {
        public ProjectExtractCreateViewModel()
        {
            BillDate = DateTime.Today;
            Total = 0m;
            VatValue = 0m;
            NetValue = 0m;
            PerforValue = 0m;
            AdvancedPayment = 0m;
            GeneralDiscount = 0m;
            Projects = new List<ProjectLookupItem>();
            ExtractItems = new List<ProjectExtractItemViewModel>();
        }

        public IList<ProjectLookupItem> Projects { get; private set; }
        public IList<ProjectExtractItemViewModel> ExtractItems { get; set; }

        public int Id { get; set; }

        [Required(ErrorMessage = "حدد المشروع.")]
        public int? ProjectId { get; set; }

        public string ProjectName { get; set; }
        public string ProjectFullCode { get; set; }

        [Required(ErrorMessage = "تاريخ المستخلص مطلوب.")]
        public DateTime? BillDate { get; set; }

        [StringLength(4000, ErrorMessage = "الرقم اليدوي طويل جدًا.")]
        public string ManualNo { get; set; }

        [Range(0, 999999999999, ErrorMessage = "قيمة المستخلص يجب أن تكون رقمًا موجبًا.")]
        public decimal? Total { get; set; }

        [Range(0, 999999999999, ErrorMessage = "قيمة الضريبة يجب أن تكون رقمًا موجبًا.")]
        public decimal? VatValue { get; set; }

        [Range(0, 999999999999, ErrorMessage = "الصافي يجب أن يكون رقمًا موجبًا.")]
        public decimal? NetValue { get; set; }

        public decimal? PerforValue { get; set; }
        public decimal? AdvancedPayment { get; set; }
        public decimal? GeneralDiscount { get; set; }

        public int? BranchNo { get; set; }
        public string Remarks { get; set; }
        public string Warning { get; set; }
    }

    public class ProjectExtractItemViewModel
    {
        public int Id { get; set; }
        public int PrMainDesID { get; set; }
        public string Item { get; set; }
        public string FullCode { get; set; }
        public decimal ContractQuantity { get; set; }
        public decimal Price { get; set; }
        public decimal PreviousQuantity { get; set; }
        public decimal PreviousValue { get; set; }
        public decimal CurrentQuantity { get; set; }
        public decimal CurrentValue { get; set; }
    }
}
