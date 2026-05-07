using System;
using System.Collections.Generic;

namespace MyERP.Areas.Pos.Models
{
    public class PosFinancialIntelligencePageModel
    {
        public PosFinancialIntelligencePageModel()
        {
            Branches = new List<PosBranchDto>();
            FromDate = DateTime.Today.AddDays(-30);
            ToDate = DateTime.Today;
        }

        public PosUserContext Context { get; set; }
        public IList<PosBranchDto> Branches { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int? LockedBranchId { get; set; }
        public string InitialAccountCode { get; set; }
    }

    public class PosFinancialIntelligenceFilter
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? BranchId { get; set; }
        public int? UserId { get; set; }
        public int? EmployeeId { get; set; }
        public string AccountCode { get; set; }
        public int? ReceivableParentSerial { get; set; }
        public int? CustodyParentSerial { get; set; }
    }

    public class PosFinancialIntelligenceResult
    {
        public PosFinancialIntelligenceResult()
        {
            Tables = new List<PosFinancialIntelligenceTable>();
        }

        public IList<PosFinancialIntelligenceTable> Tables { get; set; }
    }

    public class PosFinancialIntelligenceTable
    {
        public PosFinancialIntelligenceTable()
        {
            Columns = new List<string>();
            Rows = new List<IDictionary<string, object>>();
        }

        public string Name { get; set; }
        public IList<string> Columns { get; set; }
        public IList<IDictionary<string, object>> Rows { get; set; }
    }
}
