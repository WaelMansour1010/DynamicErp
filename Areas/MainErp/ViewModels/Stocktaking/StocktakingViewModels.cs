using System;
using System.Collections.Generic;

namespace MyERP.Areas.MainErp.ViewModels.Stocktaking
{
    public class StocktakingIndexViewModel
    {
        public StocktakingIndexViewModel()
        {
            Transactions = new List<StocktakingListItemViewModel>();
            Stores = new List<StocktakingLookupItem>();
            Branches = new List<StocktakingLookupItem>();
            Items = new List<StocktakingItemLookup>();
            Units = new List<StocktakingLookupItem>();
            Permissions = new StocktakingPermissionsViewModel();
        }

        public string SearchText { get; set; }
        public int? StoreId { get; set; }
        public int? BranchId { get; set; }
        public string Mode { get; set; }
        public IList<StocktakingListItemViewModel> Transactions { get; set; }
        public IList<StocktakingLookupItem> Stores { get; set; }
        public IList<StocktakingLookupItem> Branches { get; set; }
        public IList<StocktakingItemLookup> Items { get; set; }
        public IList<StocktakingLookupItem> Units { get; set; }
        public StocktakingPermissionsViewModel Permissions { get; set; }
    }

    public class StocktakingPermissionsViewModel
    {
        public bool CanView { get; set; }
        public bool CanAdd { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }

    public class StocktakingListItemViewModel
    {
        public int Id { get; set; }
        public string Serial { get; set; }
        public DateTime? Date { get; set; }
        public string StoreName { get; set; }
        public string BranchName { get; set; }
        public int LinesCount { get; set; }
        public decimal TotalValue { get; set; }
        public int? NotS { get; set; }
        public int? NotS2 { get; set; }
    }

    public class StocktakingLookupItem
    {
        public string Id { get; set; }
        public string Text { get; set; }
    }

    public class StocktakingItemLookup : StocktakingLookupItem
    {
        public string Code { get; set; }
        public bool HaveSerial { get; set; }
        public decimal Price { get; set; }
        public int? DefaultUnitId { get; set; }
        public string DefaultUnitName { get; set; }
    }

    public class StocktakingDetailsViewModel
    {
        public StocktakingDetailsViewModel()
        {
            Lines = new List<StocktakingLineViewModel>();
        }

        public int? Id { get; set; }
        public string Serial { get; set; }
        public DateTime? Date { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? BranchId { get; set; }
        public int? StoreId { get; set; }
        public int GardEntryType { get; set; }
        public bool StartGard { get; set; }
        public bool StartSettlement { get; set; }
        public bool AutoDetect { get; set; }
        public string Account1 { get; set; }
        public string Account2 { get; set; }
        public int? NotS { get; set; }
        public int? NotS2 { get; set; }
        public IList<StocktakingLineViewModel> Lines { get; set; }
    }

    public class StocktakingLineViewModel
    {
        public int? ItemId { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public int? UnitId { get; set; }
        public string UnitName { get; set; }
        public decimal Count { get; set; }
        public decimal Price { get; set; }
        public string Serial { get; set; }
        public int ItemCase { get; set; }
        public int ColorId { get; set; }
        public string ItemSize { get; set; }
        public int ClassId { get; set; }
        public decimal? GardQty { get; set; }
        public decimal? GardResult { get; set; }
        public decimal? GardResult1 { get; set; }
        public decimal? GardResult2 { get; set; }
        public string LotNo { get; set; }
        public DateTime? ProductionDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string PartNoCode { get; set; }
        public string ItemDetailedCode { get; set; }
        public int AutoDetect { get; set; }
        public decimal? Height { get; set; }
        public decimal? Width { get; set; }
        public decimal? Length { get; set; }
        public decimal? Area { get; set; }
    }

    public class StocktakingSaveRequest : StocktakingDetailsViewModel
    {
        public string Mode { get; set; }
    }

    public class StocktakingSaveResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int? Id { get; set; }
        public string Serial { get; set; }
    }
}
