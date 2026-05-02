using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using DevExpress.CodeParser;
using HROnlineModel;
using Newtonsoft.Json;

namespace HRServices.Models
{
    public class VisitDataModel
    {
        public string visittime { get; set; }
        public Guid id { get; set; }
        public string code { get; set; }
        public string name { get; set; }
        public string orderdate { get; set; }
        public string signindate { get; set; }
        public string signoutdate { get; set; }
        public string notes { get; set; }
        public string signinlocation { get; set; }
        public string signoutlocation { get; set; }
        public string visitname { get; set; }
        public string visitdate { get; set; }
        public string notes2 { get; set; }
        public string type { get; set; }
    }

    public class VisitModel
    {
        public Guid OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public int CustomerId { get; set; }
        public string FullCode { get; set; }
        public DateTime SignInDate { get; set; }
        public DateTime SignOutDate { get; set; }
        public string SignINLocation { get; set; }
        public string SignOutLocation { get; set; }
        public string Notes { get; set; }
    }
    public class PermissionModel
    {
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public string Date { get; set; }
        public string FromTime { get; set; }
        public string ToTime { get; set; }
        public string Notes { get; set; }
        public DateTime OrderDate { get; set; }
    }
    public class VactionModel
    {

        [Required(AllowEmptyStrings = false, ErrorMessage = "ادخل كود الموظف")]
        public string EmployeeCode { get; set; }
        [Required(AllowEmptyStrings = false, ErrorMessage = "ادخل اسم الموظف")]
        public string EmployeeName { get; set; }
        [Required(AllowEmptyStrings = false, ErrorMessage = "ادخل من يوم اليوم  ")]
        public string FromDate { get; set; }
        [Required(AllowEmptyStrings = false, ErrorMessage = "ادخل الى يوم اليوم  ")]
        public string ToDate { get; set; }
        //public string FromDateDay { get; set; }
        //[Required(AllowEmptyStrings = false, ErrorMessage = "ادخل الشهر  ")]
        //public string FromDateMonth { get; set; }
        //[Required(AllowEmptyStrings = false, ErrorMessage = "ادخل السنه  ")]
        //public string FromDateYear { get; set; }
        //[Required(AllowEmptyStrings = false, ErrorMessage = "ادخل  اليوم  ")]
        //public string ToDateDay { get; set; }
        //[Required(AllowEmptyStrings = false, ErrorMessage = "ادخل  الشهر  ")]
        //public string ToDateMonth { get; set; }
        //[Required(AllowEmptyStrings = false, ErrorMessage = "ادخل  السنه  ")]
        //public string ToDateYear { get; set; }
        [Required(AllowEmptyStrings = false, ErrorMessage = "ادخل نوع الراتب  ")]
        public int withSal { get; set; }
        [Required(AllowEmptyStrings = false, ErrorMessage = "ادخل  سبب الاجازه  ")]
        public string Details { get; set; }

        public DateTime OrderDate { get; set; }
        public bool IsSickleave { get; set; }

        public int AlterEmp { get; set; }
        public decimal? Balance { get; set; }
    }
   
    
    
     public class AdvanceModel
    {
       
       
        public string EmployeeCode { get; set; }
       
        public string EmployeeName { get; set; }

        public string ManegerCode { get; set; }

        public string MangerName { get; set; }
    
        public int Emp1 { get; set; }

        public int Emp2 { get; set; }
        public decimal Amount { get; set; }
        public string Notes { get; set; }
    }
    
    public class OrderModel
    {
        public int OrderNo { get; set; }
        public string Status { get; set; }
        public string OrderDate { get; set; }
        public List<OrderLineModel> Lines { get; set; }
        public string EmployeeCode { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public int ManagerId { get; set; }
    }

    public class OrderLineModel
    {
        [JsonPropertyName("itemId")]
        public int ItemId { get; set; }
        [JsonPropertyName("itemCode")]
        public string ItemCode { get; set; }
        [JsonPropertyName("itemName")]
        public string ItemName { get; set; }
        [JsonPropertyName("itemNamee")]
        public string ItemNamee { get; set; }
        [JsonPropertyName("qty")]
        public int Qty { get; set; }
        [JsonPropertyName("notes")]
        public string Notes { get; set; }

        public int? unitid { get; set; }

    }

    public class ItemModel
    {
        [JsonPropertyName("itemCode")]
        public string ItemCode { get; set; }
        [JsonPropertyName("itemId")]
        public int ItemId { get; set; }
        [JsonPropertyName("itemName")]
        public string ItemName { get; set; }
        [JsonPropertyName("itemNamee")]
        public string ItemNamee { get; set; }
        public int? unitId { get; set; }
        [JsonPropertyName("itemUnites")]
        public List<MyItemUnitModel> ItemUnites { get; set; }
        

    }

    public class DataUnitesModel
    {
        public List<ItemsUnitModel> ItemUnites { get; set; }
        public List<UniteModel> Unites { get; set; }
    }
    public class MyItemUnitModel
    {
            public int UnitId { get; set; }
            public string UnitName { get; set; }
            public int ItemID { get; set; }
            public bool DefaultUnit { get; set; }
            public decimal UnitFactor { get; set; }
    }
    public class VBUserModel
    {

        public int UserID { get; set; }
        public string UserName { get; set; }
        public string PassWord { get; set; }
        public int Empid { get; set; }
        public int IsActive { get; set; }
    }
    public class JobModel
    {
        public int JobTypeID { get; set; }
        public string JobTypeName { get; set; }
        public string JobTypeNamee { get; set; }
        public string VisaCode { get; set; }

    }

    public class EmployeeModel
    {
        public int Emp_ID { get; set; }
        public string Emp_Code { get; set; }
        public string FullCode { get; set; }
        public string Emp_Name { get; set; }
        public string EmpNamee { get; set; }
        //public string Emp_Phone { get; set; }
        public int Sex { get; set; }
        public int MangerId { get; set; }
        public string Password { get; set; }
        public int swapedempid { get; set; }
        public int swapedempid2 { get; set; }
        public int project_id { get; set; }
        public int JobTypeID { get; set; }
        public decimal? balanceH1 { get; set; }
        public decimal? balanceH2 { get; set; }
        public decimal? balanceH3 { get; set; }
        public decimal? balanceH4 { get; set; }
        public string EmpMail { get; set; }
        public string Emp_Phone { get; set; }
        public string Emp_mobile { get; set; }
    }
    public class CustomerModel
    {
        public int CusID { get; set; }
        public string CusName { get; set; }
        public string CusNamee { get; set; }
        public string Cus_Phone { get; set; }
        public string Cus_mobile { get; set; }
        public int? Type { get; set; }
        public string FaxNumber { get; set; }
        public string E_mail { get; set; }
        public int? SaleType { get; set; }
        public int? CountryID { get; set; }
        public int? GovernmentID { get; set; }
        public int? CityID { get; set; }
        public string Address { get; set; }
        public int? EmpId { get; set; }
        public string code { get; set; }
        public string Fullcode { get; set; }
        public int? BranchId { get; set; }
        public string Company { get; set; }
        public string JobTitle { get; set; }
        public string HomeTel { get; set; }
        public string Mobile1 { get; set; }
        public string Mobile2 { get; set; }
        public string Sex { get; set; }
        public string ZipCode { get; set; }
        public string Mail2 { get; set; }
    }


    public class HomeModel
    {
        public List<ManagerTransModel> ManagerTrans { get; set; }
        public List<ManagerTransModel> MyTransModel { get; set; }
        public List<ManagerTransModel> MyTransModelLevel3 { get; set; }
        public bool ItemRequestShow { get; set; }
        public List<ManagerTransModel> EmployeeTrans { get; set; }
    }
    public class ManagerTransModel
    {
        public int OrderId { get; set; }
        public string Type { get; set; }
        public string Date { get; set; }
        public string EmployeeName { get; set; }
        public string JobName { get;set; }
        public string ProjectName { get; set; }
        public string EmployeeCode { get; set; }

        public TransTypeEnum TransType { get; set; }
        public StatusEnum Status { get; set; }
        public string Data { get; set; }
        public bool CanClose { get; set; }
        public int? LevelOne { get; set; }
        public int? LevelTow { get; set; }
        public int? LevelThree { get; set; }
        public bool? IsSickleave { get; set; }

        public LevelApproveType  EmployeeLevelType { get; set; }


        public string TransTypeName
        {
            get
            {
                switch (TransType)
                {
                    //[1:30 PM, 12 / 25 / 2023] Wael Mansor 2:   Materials Requisition
                    //[1:30 PM, 12 / 25 / 2023] Wael Mansor 2: طلب سلفة. Request An Advance
                    //[1:30 PM, 12 / 25 / 2023] Wael Mansor 2: طلب إذن.  Request for permissions
                    //[1:30 PM, 12 / 25 / 2023] Wael Mansor 2: طلب اجازة.Request For Vacation
                    case TransTypeEnum.Permssion:
                        return "Request for permissions";
                    case TransTypeEnum.Vaction:
                        return $"Request For Vacation {((IsSickleave??false)? "(Sick leave)" : "")}";
                    case TransTypeEnum.Advance:
                        return "Request An Advance";
                    case TransTypeEnum.Materials:
                        return "Materials Requisition";
                    default:
                        return "";
                }
            }
        }

        public Guid? RowId { get; set; }
        public string AlterEmp
        
        
        { get; set; }
    }

    public enum StatusEnum
    {
        Pinding = 0,
        Accepted = 1,
        Rejected = 2,
        Approved = 3,
        Closed = 4
    }

    public enum TransTypeEnum
    {
        Vaction = 1,
        Permssion = 2,
        Advance = 3,
        Materials = 4




    }

    public enum LevelApproveType
    {
        LevelOne=1,
        LevelTow=2,
        LevelThree=3
    }

    public class PostModel
    {
        //public string orgCode { get; set; }
        //public int? type1 { get; set; }
        //public int? type2 { get; set; }
        //public int? type3 { get; set; }

        /// <summary>
        /// //////////////////////////////////////
        /// </summary>
        public int draw { get; set; }
        public int start { get; set; }
        public int length { get; set; }
        public List<Column> columns { get; set; }
        public Search search { get; set; }
        public List<Order> order { get; set; }

    }
    public class Column
    {
        public string data { get; set; }
        public string name { get; set; }
        public bool searchable { get; set; }
        public bool orderable { get; set; }
        public Search search { get; set; }
    }


    public class Search
    {
        public string value { get; set; }
        public string regex { get; set; }
    }
    public class Order
    {
        public int column { get; set; }
        public string dir { get; set; }
    }

    public class opr_employee_detailModel
    {
        //public opr_employee_detail()
        //{
        //    this.Ended = 0;
        //}

        public int id { get; set; }
        public int? pk_id { get; set; }
        public int? Emp_id { get; set; }
        public string Emp_code { get; set; }
        public string emp_name { get; set; }
        public string JobTypeName { get; set; }
        public int? Project_id { get; set; }
        public string term_Fullcode { get; set; }
        public string opr_Fullcode { get; set; }
        public DateTime? Start_date { get; set; }
        public int? opr_type { get; set; }
        public DateTime? end_date { get; set; }
        public int? no_of_days { get; set; }
        public int? Ended { get; set; }
        public int? opreration_type { get; set; }
        public int? to_project { get; set; }
        public string to_term { get; set; }
        public string to_opr { get; set; }
        public int? person_id { get; set; }
        public string person_name { get; set; }
        public string person_code { get; set; }
        public string person_joptypename { get; set; }
        public string to_project_name { get; set; }
        public int? JobTypeID { get; set; }
        public decimal? daysalary { get; set; }
        public int? Count { get; set; }
        public decimal? Total { get; set; }
        public int? toid { get; set; }
        public int? interval { get; set; }
        public int? ProjectID { get; set; }
        public int? PandID { get; set; }
        public int? OperID { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? ContProjSalar { get; set; }
        public string NumEkama { get; set; }
        public int? DepartmentID { get; set; }
        public int? BranchId { get; set; }
        public int? SpecificationID { get; set; }
        public int? IDTransfer { get; set; }
        public DateTime? OldToDate { get; set; }
        public int? FromProjectID { get; set; }
    }

    public class projectModel
    {
        public int id { get; set; }
        public string End_user_Account { get; set; }
        public string End_user_name { get; set; }
        public string sub_contractor_Account { get; set; }
        public string sub_contractor_name { get; set; }
        public string Fullcode { get; set; }
        public string prifix { get; set; }
        public string Code { get; set; }
        public string Project_name { get; set; }
        public string Contract_type { get; set; }
        public string Project_status { get; set; }
        public string expanses_account { get; set; }
        public string REVENUE_account { get; set; }
        public string Project_account { get; set; }
        public decimal? project_cost { get; set; }
        public int? branch_no { get; set; }
        public string departement { get; set; }
        public string project_code { get; set; }
        public string branche_ID { get; set; }
        public decimal? expanses_account_balance { get; set; }
        public decimal? expansese_account_balance { get; set; }
        public decimal? expansesm_account_balance { get; set; }
        public decimal? expansess_account_balance { get; set; }
        public decimal? REVENUE_account_balance { get; set; }
        public decimal? Project_account_balance { get; set; }
        public string Contract_type_name { get; set; }
        public string End_user_id { get; set; }
        public string sub_contractor_id { get; set; }
        public decimal? general_discount { get; set; }
        public decimal? cost_after_discount { get; set; }
        public decimal? total { get; set; }
        public decimal? sub_discount_total { get; set; }
        public decimal? net { get; set; }
        public string Material_account { get; set; }
        public string Salary_account { get; set; }
        public string legal { get; set; }
        public decimal? items_total { get; set; }
        public decimal? Legal_account_balance { get; set; }
        public int? CurrencyID { get; set; }
        public DateTime? StartDate { get; set; }
        public string Project_nameE { get; set; }
        public double? opening_balance_voucher_id { get; set; }
        public DateTime? OpenBalanceDate { get; set; }
        public int? OpenBalanceType { get; set; }
        public double? OpenBalance { get; set; }
        public int? OpenBalanceType1 { get; set; }
        public double? OpenBalance1 { get; set; }
        public int? OpenBalanceType2 { get; set; }
        public double? OpenBalance2 { get; set; }
        public int? OpenBalanceType3 { get; set; }
        public double? OpenBalance3 { get; set; }
        public int? OpenBalanceType4 { get; set; }
        public double? OpenBalance4 { get; set; }
        public int? EmpId { get; set; }
        public int? EmpId1 { get; set; }
        public int? Pstate { get; set; }
        public double? CashingValue { get; set; }
        public DateTime? EndDate { get; set; }
        public double? DiscountPercentage { get; set; }
        public int? Dept_ID { get; set; }
        public string Remarkss { get; set; }
        public DateTime? DpNearEndDate { get; set; }
        public double? JobeDO { get; set; }
        public double? JobeDOPercent { get; set; }
        public double? JobeGet { get; set; }
        public double? JobeRest { get; set; }
        public int? JobeTimeLeft { get; set; }
        public string JobeTime { get; set; }
        public int? JobeWork { get; set; }
        public int? JobeContractorID { get; set; }
        public int? WarrantyNO { get; set; }
        public double? WarrantyValue { get; set; }
        public DateTime? WarrDateStart { get; set; }
        public DateTime? WarrDateEnd { get; set; }
        public double? WarrExtension { get; set; }
        public int? WarrBank { get; set; }
        public int? Amanhid { get; set; }
        public int? Municipalityid { get; set; }
        public int? UserID { get; set; }
        public int? OrderType { get; set; }
        public string OrderNo { get; set; }
        public double? EmpSalary { get; set; }
        public double? MangSalary { get; set; }
        public string ContractNo { get; set; }
        public string AccountUnderImp { get; set; }
        public int? UnderImp { get; set; }
        public double? OpenBalance5 { get; set; }
        public int? OpenBalanceType5 { get; set; }
        public double? TotalMainDes { get; set; }
        public int? TypeImport { get; set; }
        public string Path { get; set; }
        public double? TotalMainDesExe { get; set; }
        public double? UnderAccountBalance { get; set; }
        public string AcountGood { get; set; }
        public double? OpenBalance6 { get; set; }
        public int? OpenBalanceType6 { get; set; }
        public double? OpenBalance8 { get; set; }
        public int? OpenBalanceType8 { get; set; }
        public string NoteSerial { get; set; }
        public int? NoteId { get; set; }
        public double? Insurance { get; set; }
    }


    public class EmployeeProjectModelData
    {
        public List<projectModel> ProjectData { get; set; }
        public List<opr_employee_detailModel> oprData { get; set; }
    }




    public class ItemsUnitModel
    {
        public int? JunckID { get; set; }
        public int? ItemID { get; set; }
        public int? UnitID { get; set; }
        public decimal? UnitFactor { get; set; }
        public int? SecOrder { get; set; }
        public int? DefaultUnit { get; set; }
        public decimal? UnitSalesPrice { get; set; }
        public decimal? UnitPurPrice { get; set; }
        public decimal? FactorByDefaultUnit { get; set; }
        public decimal? FactorBySmallUnit { get; set; }
        public double? MinSelingPrice { get; set; }
        public double? ForUnit { get; set; }
        public double? MethodCalc { get; set; }
        public double? PartItemQty { get; set; }
        public string SessionCode { get; set; }
        public string barCodeNo2 { get; set; }
        public double? UnitWholeSalePrice { get; set; }
        public double? OldUnitSalesPrice { get; set; }
        public double? OldUnitWholeSalePrice { get; set; }
        public double? MaxSelingPrice { get; set; }
        public double? SelingPriceDestr { get; set; }
    }

    public class UniteModel
    {
        public int UnitID { get; set; }
        public string UnitName { get; set; }
        public string UnitNamee { get; set; }
        public string SessionCode { get; set; }
        public string QRCODE { get; set; }
        public int? HaveWeight { get; set; }
    }


}
