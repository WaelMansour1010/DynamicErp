using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace MyERP.Areas.Pos.Models
{
    public class SalesRepresentativesPerformanceFilter
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? BranchId { get; set; }
        public int? UserId { get; set; }
        public string ServiceType { get; set; }
        public decimal? MonthlyRechargeTarget { get; set; }
        public int? MonthlyCardTarget { get; set; }
        public int? WorkingDaysInMonth { get; set; }
    }

    public class SalesRepresentativesPerformancePageModel
    {
        public PosUserContext Context { get; set; }
        public SalesRepresentativesPerformanceFilter Filter { get; set; }
        public IList<PosBranchDto> Branches { get; set; }
        public IList<PosUserLookupDto> SalesRepresentatives { get; set; }
        public IList<SalesRepresentativePerformanceRow> Rows { get; set; }
        public SalesRepresentativesPerformanceSummary Summary { get; set; }
        public int? LockedBranchId { get; set; }

        public SalesRepresentativesPerformancePageModel()
        {
            Filter = new SalesRepresentativesPerformanceFilter();
            Branches = new List<PosBranchDto>();
            SalesRepresentatives = new List<PosUserLookupDto>();
            Rows = new List<SalesRepresentativePerformanceRow>();
            Summary = new SalesRepresentativesPerformanceSummary();
        }
    }

    public class PosUserLookupDto
    {
        public int UserId { get; set; }
        public string DisplayName { get; set; }
        public int? BranchId { get; set; }
    }

    public class SalesRepresentativesPerformanceSummary
    {
        public int RepresentativeCount { get; set; }
        public decimal CashInTotal { get; set; }
        public decimal CashOutWithFeesTotal { get; set; }
        public decimal FeesTotal { get; set; }
        public decimal CardsTotal { get; set; }
        public int CardsCount { get; set; }
        public decimal ExpectedCashSupplyTotal { get; set; }
        public decimal OverallAchievementPercent { get; set; }
    }

    public class SalesRepresentativePerformanceRow
    {
        public int UserId { get; set; }
        public int? EmpId { get; set; }
        public string RepresentativeName { get; set; }
        public int? BranchId { get; set; }
        public string BranchName { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal CashInTotal { get; set; }
        public int CashInCount { get; set; }
        public decimal CashOutWithoutFees { get; set; }
        public decimal CashOutWithFees { get; set; }
        public int CashOutCount { get; set; }
        public decimal ViolationsTotal { get; set; }
        public decimal ViolationsFees { get; set; }
        public int ViolationsCount { get; set; }
        public decimal FeesTotal { get; set; }
        public decimal CardsTotal { get; set; }
        public int CardsCount { get; set; }
        public decimal ExpectedCashSupply { get; set; }
        public decimal RechargeTarget { get; set; }
        public decimal CardTarget { get; set; }
        public decimal AchievedRecharge { get; set; }
        public decimal AchievedCards { get; set; }
        public decimal RechargeAchievementPercent { get; set; }
        public decimal CardAchievementPercent { get; set; }
        public decimal OverallAchievementPercent { get; set; }
        public decimal RequiredDailyRecharge { get; set; }
        public decimal RequiredDailyCards { get; set; }
        public decimal ProjectedRecharge { get; set; }
        public decimal ProjectedCards { get; set; }
        public string PerformanceStatus { get; set; }
        public string PerformanceClass { get; set; }

        public SalesTargetAchievementCardModel ToTargetCard()
        {
            return new SalesTargetAchievementCardModel
            {
                TotalRecharge = AchievedRecharge,
                TotalCards = AchievedCards,
                RechargeTarget = RechargeTarget,
                RechargeTodayAchievement = AchievedRecharge,
                RechargeAchievementPercent = RechargeAchievementPercent,
                CardTarget = CardTarget,
                CardTodayAchievement = AchievedCards,
                CardAchievementPercent = CardAchievementPercent,
                OverallAchievementPercent = OverallAchievementPercent,
                PerformanceClass = PerformanceClass
            };
        }
    }

    public class SalesTargetAchievementCardModel
    {
        public decimal TotalRecharge { get; set; }
        public decimal TotalCards { get; set; }
        public decimal RechargeTarget { get; set; }
        public decimal RechargeTodayAchievement { get; set; }
        public decimal RechargeAchievementPercent { get; set; }
        public decimal CardTarget { get; set; }
        public decimal CardTodayAchievement { get; set; }
        public decimal CardAchievementPercent { get; set; }
        public decimal OverallAchievementPercent { get; set; }
        public string PerformanceClass { get; set; }
    }

    public class PosSalesTargetsPageModel
    {
        public PosUserContext Context { get; set; }
        public IList<PosBranchDto> Branches { get; set; }
        public IList<PosUserLookupDto> SalesRepresentatives { get; set; }
        public IList<PosSalesTargetRowDto> Targets { get; set; }
        public int? LockedBranchId { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public PosSalesTargetsPageModel()
        {
            Branches = new List<PosBranchDto>();
            SalesRepresentatives = new List<PosUserLookupDto>();
            Targets = new List<PosSalesTargetRowDto>();
            FromDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            ToDate = FromDate.AddMonths(1).AddDays(-1);
        }
    }

    public class PosSalesTargetSaveRequest
    {
        public string ApplyMode { get; set; }
        public int? BranchId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public decimal MonthlyRechargeTarget { get; set; }
        public int MonthlyCardTarget { get; set; }
        public int WorkingDaysInMonth { get; set; }
        public IList<int> UserIds { get; set; }

        public PosSalesTargetSaveRequest()
        {
            UserIds = new List<int>();
        }
    }

    public class PosSalesTargetRowDto
    {
        public int TargetId { get; set; }
        public int? UserId { get; set; }
        public string RepresentativeName { get; set; }
        public int? BranchId { get; set; }
        public string BranchName { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal MonthlyRechargeTarget { get; set; }
        public int MonthlyCardTarget { get; set; }
        public int WorkingDaysInMonth { get; set; }
        public decimal DailyRechargeTarget { get; set; }
        public decimal DailyCardTarget { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedByName { get; set; }
    }
}
