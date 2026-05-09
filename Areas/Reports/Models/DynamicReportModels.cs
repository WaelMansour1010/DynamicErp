using System;
using System.Collections.Generic;

namespace MyERP.Areas.Reports.Models
{
    public static class DynamicReportScopes
    {
        public const string Web = "Web";
        public const string Pos = "POS";
        public const string MainErp = "MainErp";
        public const string Shared = "Shared";

        public static string Normalize(string scope)
        {
            if (string.Equals(scope, Pos, StringComparison.OrdinalIgnoreCase)) return Pos;
            if (string.Equals(scope, MainErp, StringComparison.OrdinalIgnoreCase)) return MainErp;
            if (string.Equals(scope, Shared, StringComparison.OrdinalIgnoreCase)) return Shared;
            return Web;
        }
    }

    public static class DynamicReportCatalogStatus
    {
        public const string Pending = "Pending";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
        public const string Risky = "Risky";
        public const string Imported = "Imported";
    }

    public class DynamicReportUserContext
    {
        public int UserId { get; set; }
        public int? RoleId { get; set; }
        public string UserName { get; set; }
        public string ProjectScope { get; set; }
        public bool IsAdmin { get; set; }
    }

    public class DynamicReportDefinition
    {
        public DynamicReportDefinition()
        {
            Parameters = new List<DynamicReportParameter>();
            Columns = new List<DynamicReportColumn>();
            ProjectScope = DynamicReportScopes.Shared;
            SourceType = "StoredProcedure";
            MaxRows = 1000;
            CommandTimeoutSeconds = 30;
            IsActive = true;
            LifecycleStatus = LifecycleStatusEnum.Draft;
            CertificationLevel = DynamicReportCertificationLevel.Internal;
        }

        public int ReportId { get; set; }
        public string ReportCode { get; set; }
        public string ReportNameAr { get; set; }
        public string ReportNameEn { get; set; }
        public string ProjectScope { get; set; }
        public string SourceType { get; set; }
        public string SourceName { get; set; }
        public bool RequireDateRange { get; set; }
        public int MaxRows { get; set; }
        public int CommandTimeoutSeconds { get; set; }
        public bool IsActive { get; set; }
        public string LifecycleStatus { get; set; }
        public string CertificationLevel { get; set; }
        public DateTime? LastValidatedAt { get; set; }
        public string LastValidationLog { get; set; }
        public int? ActivatedBy { get; set; }
        public DateTime? ActivatedAt { get; set; }
        public int? ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public IList<DynamicReportParameter> Parameters { get; set; }
        public IList<DynamicReportColumn> Columns { get; set; }
    }

    public class DynamicReportParameter
    {
        public int ParameterId { get; set; }
        public int ReportId { get; set; }
        public string ParameterName { get; set; }
        public string CaptionAr { get; set; }
        public string CaptionEn { get; set; }
        public string DataType { get; set; }
        public bool IsRequired { get; set; }
        public string DefaultValue { get; set; }
        public string LookupKey { get; set; }
        public string LookupSql { get; set; }
        public int SortOrder { get; set; }
    }

    public class DynamicReportColumn
    {
        public int ColumnId { get; set; }
        public int ReportId { get; set; }
        public string FieldName { get; set; }
        public string CaptionAr { get; set; }
        public string CaptionEn { get; set; }
        public string DataType { get; set; }
        public bool IsVisibleDefault { get; set; }
        public bool IsFilterable { get; set; }
        public bool IsSortable { get; set; }
        public bool IsGroupable { get; set; }
        public bool IsSummable { get; set; }
        public int? Width { get; set; }
        public int SortOrder { get; set; }
    }

    public class DynamicReportLayout
    {
        public int LayoutId { get; set; }
        public int ReportId { get; set; }
        public int UserId { get; set; }
        public string ProjectScope { get; set; }
        public string LayoutName { get; set; }
        public string LayoutJson { get; set; }
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class DynamicReportExecutionRequest
    {
        public DynamicReportExecutionRequest()
        {
            Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public int ReportId { get; set; }
        public string ProjectScope { get; set; }
        public IDictionary<string, string> Parameters { get; set; }
    }

    public class DynamicReportExecutionResult
    {
        public DynamicReportExecutionResult()
        {
            Columns = new List<DynamicReportColumn>();
            Rows = new List<IDictionary<string, object>>();
        }

        public bool Success { get; set; }
        public string Message { get; set; }
        public IList<DynamicReportColumn> Columns { get; set; }
        public IList<IDictionary<string, object>> Rows { get; set; }
        public int RowCount { get; set; }
        public int MaxRows { get; set; }
    }

    public class DynamicReportPermission
    {
        public int PermissionId { get; set; }
        public int ReportId { get; set; }
        public string ProjectScope { get; set; }
        public int? UserId { get; set; }
        public int? RoleId { get; set; }
        public bool CanView { get; set; }
        public bool CanDesign { get; set; }
        public bool CanExport { get; set; }
        public DateTime CreatedAt { get; set; }
        public string DisplayName { get; set; }
    }

    public class DynamicReportPermissionInput
    {
        public int ReportId { get; set; }
        public string ProjectScope { get; set; }
        public int? UserId { get; set; }
        public int? RoleId { get; set; }
        public bool CanView { get; set; }
        public bool CanDesign { get; set; }
        public bool CanExport { get; set; }
    }

    public class EffectivePermission
    {
        public bool CanView { get; set; }
        public bool CanDesign { get; set; }
        public bool CanExport { get; set; }
        public string Source { get; set; }
    }
}
