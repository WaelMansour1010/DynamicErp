using System;
using System.Collections.Generic;

namespace MyERP.Areas.MainErp.ViewModels.LegacyOperations
{
    public class LegacyLookupItem
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public string AccountCode { get; set; }
    }

    public class LegacyOperationsIndexViewModel
    {
        public LegacyOperationsIndexViewModel()
        {
            Branches = new List<LegacyLookupItem>();
            Boxes = new List<LegacyLookupItem>();
            Cashiers = new List<LegacyLookupItem>();
            Accounts = new List<LegacyLookupItem>();
            Customers = new List<LegacyLookupItem>();
            CarTypes = new List<LegacyLookupItem>();
            CarModels = new List<LegacyLookupItem>();
            Colors = new List<LegacyLookupItem>();
            MaintenanceWorks = new List<LegacyLookupItem>();
            ExtraExpenses = new List<LegacyLookupItem>();
            PaymentTypes = new List<LegacyLookupItem>();
            Employees = new List<LegacyLookupItem>();
            Departments = new List<LegacyLookupItem>();
            Items = new List<LegacyLookupItem>();
            Cars = new List<LegacyLookupItem>();
            FixedAssets = new List<LegacyLookupItem>();
            ItemGroups = new List<LegacyItemGroupNode>();
            ItemTreeItems = new List<LegacyItemTreeItem>();
        }

        public IList<LegacyLookupItem> Branches { get; set; }
        public IList<LegacyLookupItem> Boxes { get; set; }
        public IList<LegacyLookupItem> Cashiers { get; set; }
        public IList<LegacyLookupItem> Accounts { get; set; }
        public IList<LegacyLookupItem> Customers { get; set; }
        public IList<LegacyLookupItem> CarTypes { get; set; }
        public IList<LegacyLookupItem> CarModels { get; set; }
        public IList<LegacyLookupItem> Colors { get; set; }
        public IList<LegacyLookupItem> MaintenanceWorks { get; set; }
        public IList<LegacyLookupItem> ExtraExpenses { get; set; }
        public IList<LegacyLookupItem> PaymentTypes { get; set; }
        public IList<LegacyLookupItem> Employees { get; set; }
        public IList<LegacyLookupItem> Departments { get; set; }
        public IList<LegacyLookupItem> Items { get; set; }
        public IList<LegacyLookupItem> Cars { get; set; }
        public IList<LegacyLookupItem> FixedAssets { get; set; }
        public IList<LegacyItemGroupNode> ItemGroups { get; set; }
        public IList<LegacyItemTreeItem> ItemTreeItems { get; set; }
    }

    public class LegacySaveResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int? Id { get; set; }
    }

    public class LegacyItemGroupNode
    {
        public int Id { get; set; }
        public int? ParentId { get; set; }
        public string Text { get; set; }
        public string Code { get; set; }
        public int ItemsCount { get; set; }
    }

    public class LegacyItemTreeItem
    {
        public int Id { get; set; }
        public int? GroupId { get; set; }
        public string Code { get; set; }
        public string Text { get; set; }
        public decimal Price { get; set; }
    }

    public class CashBoxListItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string BranchName { get; set; }
        public string AccountCode { get; set; }
        public decimal OpenBalance { get; set; }
    }

    public class CashBoxDetails
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public string EnglishName { get; set; }
        public string Remarks { get; set; }
        public int? BranchId { get; set; }
        public int Type { get; set; }
        public int? EmployeeId { get; set; }
        public string ParentAccountCode { get; set; }
        public string AccountCode { get; set; }
        public bool ChequeBox { get; set; }
        public int? Period { get; set; }
        public int? PeriodType { get; set; }
        public decimal BoxValue { get; set; }
        public DateTime? OpenBalanceDate { get; set; }
        public int? OpenBalanceType { get; set; }
        public decimal OpenBalance { get; set; }
    }

    public class GeneralCashingLine
    {
        public int PaymentId { get; set; }
        public string PaymentName { get; set; }
        public decimal Balance { get; set; }
        public decimal CollectedValue { get; set; }
        public decimal CommissionPercentage { get; set; }
        public decimal CommissionValue { get; set; }
        public decimal Different { get; set; }
        public decimal NetValue { get; set; }
        public string AccountsUs { get; set; }
        public string AccountCom { get; set; }
        public string AccountCode { get; set; }
        public string Remarks { get; set; }
    }

    public class GeneralCashingDetails
    {
        public GeneralCashingDetails()
        {
            Lines = new List<GeneralCashingLine>();
        }

        public int? Id { get; set; }
        public int? NoteId { get; set; }
        public string JournalSerial { get; set; }
        public string VoucherSerial { get; set; }
        public DateTime? RecordDate { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? BranchId { get; set; }
        public int? GeneralBoxId { get; set; }
        public int? SubBoxId { get; set; }
        public int? CashierId { get; set; }
        public string Remarks { get; set; }
        public IList<GeneralCashingLine> Lines { get; set; }
    }

    public class CarMaintenanceLine
    {
        public int Type { get; set; }
        public int? MainteId { get; set; }
        public string Name { get; set; }
        public decimal Value { get; set; }
        public int Count { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal Percentage { get; set; }
        public decimal TotalNet { get; set; }
        public string AccountCode { get; set; }
        public int? DepartmentId { get; set; }
        public int? EmployeeId { get; set; }
        public string Company { get; set; }
        public string BillNo { get; set; }
    }

    public class CarMaintenanceDetails
    {
        public CarMaintenanceDetails()
        {
            Lines = new List<CarMaintenanceLine>();
        }

        public int? Id { get; set; }
        public int? NoteId { get; set; }
        public string JournalSerial { get; set; }
        public string VoucherSerial { get; set; }
        public DateTime? RecordDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? BranchId { get; set; }
        public int? CustomerId { get; set; }
        public string ClientName { get; set; }
        public string Telephone { get; set; }
        public int? CarTypeId { get; set; }
        public int? CarModelId { get; set; }
        public int? ColorId { get; set; }
        public string PlateNo { get; set; }
        public int? YearFact { get; set; }
        public string WorkOrderNo { get; set; }
        public int PaymentType { get; set; }
        public int? BoxId { get; set; }
        public decimal PaymentValue { get; set; }
        public decimal Discount { get; set; }
        public int DiscountType { get; set; }
        public decimal VatPercent { get; set; }
        public decimal VatValue { get; set; }
        public decimal TotalValue { get; set; }
        public string VatAccountCode { get; set; }
        public string SparePart { get; set; }
        public IList<CarMaintenanceLine> Lines { get; set; }
    }

    public class CarDataListItem
    {
        public int Id { get; set; }
        public string FullCode { get; set; }
        public string Name { get; set; }
        public string BoardNo { get; set; }
        public string BranchName { get; set; }
        public string CarTypeName { get; set; }
    }

    public class CarDataDetails
    {
        public CarDataDetails()
        {
            Parts = new List<CarPartLine>();
        }

        public int? Id { get; set; }
        public string Code { get; set; }
        public string Prefix { get; set; }
        public string FullCode { get; set; }
        public int? BranchId { get; set; }
        public int? CarTypeId { get; set; }
        public int? CarModelId { get; set; }
        public int? ColorId { get; set; }
        public int? EmployeeId { get; set; }
        public int? FixedAssetId { get; set; }
        public string Name { get; set; }
        public string BoardNo { get; set; }
        public string LicenseNo { get; set; }
        public string Model { get; set; }
        public string EquipmentName { get; set; }
        public string LeaderName { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public DateTime? LicenseExpireDate { get; set; }
        public DateTime? InsuranceExpireDate { get; set; }
        public DateTime? TestExpireDate { get; set; }
        public decimal LastKMCounter { get; set; }
        public decimal Capacity { get; set; }
        public decimal Rate { get; set; }
        public decimal Total { get; set; }
        public string EngineNo { get; set; }
        public string Chassis { get; set; }
        public string GearNo { get; set; }
        public string Notes { get; set; }
        public bool FormOriginal { get; set; }
        public bool AuthorizeLicense { get; set; }
        public bool AuthorizeExamination { get; set; }
        public bool SpareTyre { get; set; }
        public bool Battery { get; set; }
        public bool Guarantee { get; set; }
        public IList<CarPartLine> Parts { get; set; }
    }

    public class CarPartLine
    {
        public int? Id { get; set; }
        public int? EquipmentId { get; set; }
        public int? PartId { get; set; }
        public string PartCode { get; set; }
        public string PartName { get; set; }
    }

    public class CarAuthorizationLine
    {
        public int Type { get; set; }
        public int? MainteId { get; set; }
        public string Name { get; set; }
        public decimal Value { get; set; }
        public int Count { get; set; }
        public string Company { get; set; }
        public string BillNo { get; set; }
        public decimal Hours { get; set; }
        public bool Finish { get; set; }
        public DateTime? DateEnter { get; set; }
        public DateTime? DateExit { get; set; }
        public string TimeEnter { get; set; }
        public string TimeOut { get; set; }
        public int? EmployeeId { get; set; }
        public int? SupervisorId { get; set; }
        public int? DepartmentId { get; set; }
        public decimal PriceFitter { get; set; }
        public string DepartmentColor { get; set; }
    }

    public class CarAuthorizationItem
    {
        public int? ItemId { get; set; }
        public string ItemName { get; set; }
        public decimal Qty { get; set; }
        public decimal Price { get; set; }
        public decimal VatPercent { get; set; }
        public decimal VatValue { get; set; }
        public decimal TotalWithVat { get; set; }
        public decimal BeforeVat { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal DiscountValue { get; set; }
        public string Remark { get; set; }
    }

    public class CarAuthorizationDetails
    {
        public CarAuthorizationDetails()
        {
            Lines = new List<CarAuthorizationLine>();
            Items = new List<CarAuthorizationItem>();
        }

        public int? Id { get; set; }
        public DateTime? RecordDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? BranchId { get; set; }
        public int? UserId { get; set; }
        public int? CustomerId { get; set; }
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public string Telephone { get; set; }
        public string Mobile { get; set; }
        public int? CarId { get; set; }
        public int? CarTypeId { get; set; }
        public int? CarModelId { get; set; }
        public int? ColorId { get; set; }
        public string PlateNo { get; set; }
        public int? YearFact { get; set; }
        public int OrderStatus { get; set; }
        public decimal CarMeter { get; set; }
        public decimal CarMeterOut { get; set; }
        public string Complaint { get; set; }
        public string InitialNote { get; set; }
        public string Chassis { get; set; }
        public decimal WorkOrder { get; set; }
        public decimal AuthOrder { get; set; }
        public string SparePart { get; set; }
        public bool Cash { get; set; }
        public bool Account { get; set; }
        public bool Credit { get; set; }
        public bool Accepted { get; set; }
        public bool Finished { get; set; }
        public bool Waiting { get; set; }
        public bool SubCar1 { get; set; }
        public bool SubCar2 { get; set; }
        public bool SubCar3 { get; set; }
        public bool SubCar4 { get; set; }
        public bool SubCar5 { get; set; }
        public bool SubCar6 { get; set; }
        public bool SubCar7 { get; set; }
        public bool SubCar8 { get; set; }
        public bool SubCar9 { get; set; }
        public bool SubCar10 { get; set; }
        public bool SubCar11 { get; set; }
        public bool SubCar12 { get; set; }
        public bool SubCar13 { get; set; }
        public bool SubCar14 { get; set; }
        public bool SendSms { get; set; }
        public string QrCodeData { get; set; }
        public string QrCodeDataPath { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal TotalAfterDiscount { get; set; }
        public decimal VatPercent { get; set; }
        public decimal VatValue { get; set; }
        public IList<CarAuthorizationLine> Lines { get; set; }
        public IList<CarAuthorizationItem> Items { get; set; }
    }

    public class LegacyAttachmentItem
    {
        public int Id { get; set; }
        public string ScreenName { get; set; }
        public int RecordId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string ContentType { get; set; }
        public long FileSize { get; set; }
        public bool IsPrimary { get; set; }
        public string Caption { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
