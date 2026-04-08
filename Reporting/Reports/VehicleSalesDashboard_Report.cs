using DevExpress.DataAccess.Sql;
using DevExpress.XtraCharts;
using DevExpress.XtraReports.Parameters;
using DevExpress.XtraReports.UI;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;

namespace MyERP.Reporting.Reports
{
    /// <summary>
    /// Premium executive dashboard for vehicle sales.
    /// Uses dbo.VehicleSalesDashboard with modes 1, 3, 4, 8.
    /// All data is loaded via ADO.NET in the constructor so no
    /// DevExpress designer / resx is needed.
    /// </summary>
    public class VehicleSalesDashboard_Report : XtraReport
    {
        // ── Colour palette ────────────────────────────────────────────────────
        private static readonly Color C_PageBg       = ColorTranslator.FromHtml("#FAFBFC");
        private static readonly Color C_CardBg       = ColorTranslator.FromHtml("#FFFFFF");
        private static readonly Color C_HdrBg        = ColorTranslator.FromHtml("#FFFFFF");
        private static readonly Color C_TblHdr       = ColorTranslator.FromHtml("#F1F5F9");
        private static readonly Color C_RowEven      = ColorTranslator.FromHtml("#FFFFFF");
        private static readonly Color C_RowOdd       = ColorTranslator.FromHtml("#F8FAFC");
        private static readonly Color C_Divider      = ColorTranslator.FromHtml("#E2E8F0");
        private static readonly Color C_InkPrimary   = ColorTranslator.FromHtml("#0F172A");
        private static readonly Color C_InkSecondary = ColorTranslator.FromHtml("#475569");
        private static readonly Color C_Muted        = ColorTranslator.FromHtml("#64748B");
        private static readonly Color C_Blue         = ColorTranslator.FromHtml("#2563EB");
        private static readonly Color C_Teal         = ColorTranslator.FromHtml("#0EA5A4");
        private static readonly Color C_Amber        = ColorTranslator.FromHtml("#D4A017");
        private static readonly Color C_IndigoMuted  = ColorTranslator.FromHtml("#475569");
        private static readonly Color C_SlateSoft    = ColorTranslator.FromHtml("#CBD5E1");
        private TopMarginBand topMarginBand1;
        private DetailBand detailBand1;
        private BottomMarginBand bottomMarginBand1;
        private DevExpress.DataAccess.Sql.SqlDataSource sqlDataSource1;
        private System.ComponentModel.IContainer components;

        // ── Column layout ─────────────────────────────────────────────────────
        private static readonly (string Header, float Width)[] Columns = {
            ("رقم الشاسيه",   90f),
            ("اسم المركبة",  118f),
            ("النوع",          68f),
            ("الموديل",        92f),
            ("اللون",          62f),
            ("سنة الصنع",      52f),
            ("تاريخ الشراء",   73f),
            ("تاريخ البيع",    73f),
            ("تكلفة الشراء",   82f),
            ("سعر البيع",      82f),
            ("الربح",          75f),
            ("الحالة",         75f),
            ("العميل",        108f),
            ("أيام البيع",     48f),
        };
        // Sum = 90+118+68+92+62+52+73+73+82+82+75+75+108+48 = 1098 ≈ 1100

        // ── Parameterless constructor (required for DevExpress designer) ──────
        public VehicleSalesDashboard_Report()
            : this(null, null, null, null, null, null, null, null, null, null, null) { }

        // ── Constructor ───────────────────────────────────────────────────────
        public VehicleSalesDashboard_Report(
            DateTime? dateFrom,           DateTime? dateTo,
            int?      branchId,           int?      warehouseId,
            int?      carTypeId,          int?      carModelId,
            int?      carColorId,         int?      manufacturingYear,
            int?      vehicleStatusId,    string    chassisNo,
            int?      vendorOrCustomerId)
        {
            // ── Load summary data via ADO.NET (all non-detail modes) ─────────
            DataTable dtKpi      = Exec(3, dateFrom, dateTo, branchId, warehouseId, carTypeId, carModelId, carColorId, manufacturingYear, vehicleStatusId, chassisNo, vendorOrCustomerId);
            DataTable dtModels   = Exec(4, dateFrom, dateTo, branchId, warehouseId, carTypeId, carModelId, carColorId, manufacturingYear, vehicleStatusId, chassisNo, vendorOrCustomerId);
            DataTable dtStatus   = Exec(8, dateFrom, dateTo, branchId, warehouseId, carTypeId, carModelId, carColorId, manufacturingYear, vehicleStatusId, chassisNo, vendorOrCustomerId);
            DataTable dtGrouped  = Exec(2, dateFrom, dateTo, branchId, warehouseId, carTypeId, carModelId, carColorId, manufacturingYear, vehicleStatusId, chassisNo, vendorOrCustomerId);
            DataTable dtTimeline = Exec(5, dateFrom, dateTo, branchId, warehouseId, carTypeId, carModelId, carColorId, manufacturingYear, vehicleStatusId, chassisNo, vendorOrCustomerId);
            DataTable dtByModel  = Exec(6, dateFrom, dateTo, branchId, warehouseId, carTypeId, carModelId, carColorId, manufacturingYear, vehicleStatusId, chassisNo, vendorOrCustomerId);
            DataTable dtByStatus = Exec(7, dateFrom, dateTo, branchId, warehouseId, carTypeId, carModelId, carColorId, manufacturingYear, vehicleStatusId, chassisNo, vendorOrCustomerId);
            DataTable dtDetail   = Exec(1, dateFrom, dateTo, branchId, warehouseId, carTypeId, carModelId, carColorId, manufacturingYear, vehicleStatusId, chassisNo, vendorOrCustomerId);

            DataRow kpi = dtKpi.Rows.Count > 0 ? dtKpi.Rows[0] : null;

            // ── Page settings ─────────────────────────────────────────────────
            Landscape         = true;
            PaperKind         = DevExpress.Drawing.Printing.DXPaperKind.A4;
            RequestParameters = false;
            BackColor         = C_PageBg;
            Margins           = new System.Drawing.Printing.Margins(25, 25, 20, 20);

            // Root report is summary-only. Detail rows are rendered once in a
            // dedicated appendix section (BuildDetailSection).
            DataSource = null;
            DataMember = "";

            // ── Alternating-row styles ────────────────────────────────────────
            StyleSheet.Add(new XRControlStyle {
                Name        = "EvenRow",
                BackColor   = C_RowEven,
                ForeColor   = C_InkPrimary,
                BorderColor = C_Divider,
                Borders     = DevExpress.XtraPrinting.BorderSide.Bottom
            });
            StyleSheet.Add(new XRControlStyle {
                Name        = "OddRow",
                BackColor   = C_RowOdd,
                ForeColor   = C_InkPrimary,
                BorderColor = C_Divider,
                Borders     = DevExpress.XtraPrinting.BorderSide.Bottom
            });

            // ── Assemble bands ────────────────────────────────────────────────
            Bands.Add(new TopMarginBand    { HeightF = 20 });
            Bands.Add(BuildReportHeader(kpi, dtModels, dtStatus, dtGrouped, dtTimeline, dtByModel, dtByStatus, dateFrom, dateTo));
            Bands.Add(new DetailBand { HeightF = 0f, Name = "RootDetailBand" });
            Bands.Add(BuildDetailSection(dtDetail));
            Bands.Add(BuildPageFooter());
            Bands.Add(new BottomMarginBand { HeightF = 20 });
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Band builders
        // ─────────────────────────────────────────────────────────────────────

        private ReportHeaderBand BuildReportHeader(
            DataRow kpi, DataTable dtModels, DataTable dtStatus,
            DataTable dtGrouped, DataTable dtTimeline, DataTable dtByModel, DataTable dtByStatus,
            DateTime? dateFrom, DateTime? dateTo)
        {
            // Layout constants
            const float W        = 1100f;   // usable width
            const float CARD_H   =   86f;
            const float CHART_H  =  202f;
            const float CHART2_H =  158f;   // second analytics row height

            // KPI card geometry (4 cards per row, 8px gap)
            float gap    = 8f;
            float cardW  = (W - 3 * gap) / 4f;   // ≈ 272
            float cardX(int i) => i * (cardW + gap);

            // Y offsets
            float yTitle      =   0f;
            float ySubtitle   =  74f;
            float yKpiHdr     = 104f;
            float yKpiRow1    = 132f;
            float yKpiRow2    = yKpiRow1 + CARD_H + 10f;
            float yChartHdr   = yKpiRow2 + CARD_H + 16f;
            float yCharts     = yChartHdr + 24f;
            float yFirstDiv   = yCharts   + CHART_H + 8f;   // divider after first charts
            float yChart2Hdr  = yFirstDiv + 8f;              // second analytics section header
            float yChart2     = yChart2Hdr + 22f;            // second chart row
            float yFinalDiv   = yChart2   + CHART2_H + 8f;  // final divider before detail
            float totalH      = yFinalDiv + 8f;

            var band = new ReportHeaderBand { HeightF = totalH, BackColor = C_PageBg };

            // ── Title bar ─────────────────────────────────────────────────────
            var titlePanel = Panel(0, yTitle, W, 68f, C_HdrBg, DevExpress.XtraPrinting.BorderSide.All);
            titlePanel.Controls.Add(Label(
                "لوحة تحكم مبيعات السيارات  |  Vehicle Sales Executive Dashboard",
                0, 5f, W, 36f,
                new Font("Segoe UI", 16f, FontStyle.Bold), C_InkPrimary,
                DevExpress.XtraPrinting.TextAlignment.MiddleCenter));
            titlePanel.Controls.Add(Label(
                "Executive Summary • Board Reporting View",
                0, 37f, W, 16f,
                new Font("Segoe UI", 7.2f), C_Muted,
                DevExpress.XtraPrinting.TextAlignment.MiddleCenter));
            // blue accent underline
            titlePanel.Controls.Add(Panel(0, 65f, W, 3f, C_Blue));
            band.Controls.Add(titlePanel);

            // ── Subtitle (date / print time) ──────────────────────────────────
            string dr = (dateFrom.HasValue || dateTo.HasValue)
                ? $"الفترة:  {(dateFrom.HasValue ? dateFrom.Value.ToString("yyyy/MM/dd") : "---")}  ←→  {(dateTo.HasValue ? dateTo.Value.ToString("yyyy/MM/dd") : "---")}"
                : "جميع الفترات";
            band.Controls.Add(Label(
                $"{dr}          تاريخ الطباعة: {DateTime.Now:yyyy/MM/dd  HH:mm}",
                0, ySubtitle, W, 22f,
                new Font("Segoe UI", 7.4f), C_Muted,
                DevExpress.XtraPrinting.TextAlignment.MiddleCenter));

            // ── KPI section header ────────────────────────────────────────────
            var kpiHdrPanel = Panel(0, yKpiHdr, W, 20f, C_TblHdr, DevExpress.XtraPrinting.BorderSide.All);
            kpiHdrPanel.Controls.Add(Label(
                "مؤشرات الأداء الرئيسية  |  Key Performance Indicators",
                10f, 0, W - 20f, 20f,
                new Font("Segoe UI", 8.2f, FontStyle.Bold), C_InkSecondary,
                DevExpress.XtraPrinting.TextAlignment.MiddleLeft));
            band.Controls.Add(kpiHdrPanel);

            // ── KPI Row 1 ─────────────────────────────────────────────────────
            (string title, string value, Color accent)[] row1 = {
                ("إجمالي المركبات",   KpiInt  (kpi, "TotalVehicles"),  C_Blue),
                ("مركبات في المخزن",  KpiInt  (kpi, "InStockCars"),    C_Teal),
                ("مركبات مباعة",      KpiInt  (kpi, "SoldCars"),       C_IndigoMuted),
                ("مركبات مرتجعة",     KpiInt  (kpi, "ReturnedCars"),   C_Amber),
            };
            for (int i = 0; i < row1.Length; i++)
                band.Controls.Add(KpiCard(cardX(i), yKpiRow1, cardW, CARD_H,
                    row1[i].title, row1[i].value, row1[i].accent));

            // ── KPI Row 2 ─────────────────────────────────────────────────────
            (string title, string value, Color accent)[] row2 = {
                ("إجمالي المبيعات",   KpiMoney(kpi, "TotalSales"),     C_Blue),
                ("إجمالي الأرباح",    KpiMoney(kpi, "TotalProfit"),    C_Teal),
                ("متوسط سعر البيع",   KpiMoney(kpi, "AvgSalePrice"),   C_InkSecondary),
                ("متوسط أيام البيع",  KpiDays (kpi, "AvgDaysToSell"),  C_Amber),
            };
            for (int i = 0; i < row2.Length; i++)
                band.Controls.Add(KpiCard(cardX(i), yKpiRow2, cardW, CARD_H,
                    row2[i].title, row2[i].value, row2[i].accent));

            // ── Chart section header ──────────────────────────────────────────
            var chrtHdrPanel = Panel(0, yChartHdr, W, 22f, C_TblHdr, DevExpress.XtraPrinting.BorderSide.All);
            chrtHdrPanel.Controls.Add(Label(
                "تحليل وإحصائيات المبيعات  |  Sales Analytics",
                10f, 0, W - 20f, 22f,
                new Font("Segoe UI", 8.2f, FontStyle.Bold), C_InkSecondary,
                DevExpress.XtraPrinting.TextAlignment.MiddleLeft));
            band.Controls.Add(chrtHdrPanel);

            // ── First chart row (mode 4 = top models, mode 8 = status pie) ──
            float halfW  = (W - 6f) / 2f;   // ≈ 547
            band.Controls.Add(BuildBarChart(dtModels, 0f,         yCharts, halfW, CHART_H));
            band.Controls.Add(BuildPieChart(dtStatus, halfW + 6f, yCharts, halfW, CHART_H));

            // ── Divider between chart rows ────────────────────────────────────
            band.Controls.Add(Panel(0, yFirstDiv, W, 1f, C_Divider));

            // ── Second analytics section header ───────────────────────────────
            var anltHdrPanel = Panel(0, yChart2Hdr, W, 22f, C_TblHdr, DevExpress.XtraPrinting.BorderSide.All);
            anltHdrPanel.Controls.Add(Label(
                "تحليل تفصيلي إضافي  |  Detailed Analytics",
                10f, 0, W - 20f, 22f,
                new Font("Segoe UI", 8.2f, FontStyle.Bold), C_InkSecondary,
                DevExpress.XtraPrinting.TextAlignment.MiddleLeft));
            band.Controls.Add(anltHdrPanel);

            // ── Second chart row (modes 2, 5, 6, 7) ──────────────────────────
            float q4W = (W - 3 * 6f) / 4f;  // ≈ 269.5
            band.Controls.Add(BuildBarChart4(dtGrouped,  0f,              yChart2, q4W, CHART2_H,
                "تجميع حسب النوع  –  By Type (M2)",     "CarTypeName",        "TotalSales",    C_Amber));
            band.Controls.Add(BuildBarChart4(dtTimeline, q4W + 6f,        yChart2, q4W, CHART2_H,
                "خط الزمن  –  Timeline (M5)",            "SaleYear",           "TotalSales",    C_Teal));
            band.Controls.Add(BuildBarChart4(dtByModel,  2*(q4W + 6f),    yChart2, q4W, CHART2_H,
                "مبيعات الموديلات  –  By Model (M6)",   "CarModelName",       "TotalSales",    C_Blue));
            band.Controls.Add(BuildBarChart4(dtByStatus, 3*(q4W + 6f),    yChart2, q4W, CHART2_H,
                "توزيع الحالة  –  By Status (M7)",      "VehicleStatusName",  "VehicleCount",  C_IndigoMuted));

            // ── Final divider before detail table ────────────────────────────
            band.Controls.Add(Panel(0, yFinalDiv, W, 1f, C_Divider));

            return band;
        }

        private PageHeaderBand BuildPageHeader()
        {
            const float ROW_H = 24f;
            var band = new PageHeaderBand { HeightF = ROW_H + 2f, BackColor = C_TblHdr };

            var tbl = new XRTable { BoundsF = new RectangleF(0, 1f, 1100f, ROW_H), BackColor = C_TblHdr };
            var row = new XRTableRow { BackColor = C_TblHdr };

            foreach (var (hdr, w) in Columns)
            {
                row.Cells.Add(new XRTableCell {
                    Text          = hdr,
                    WidthF        = w,
                    HeightF       = ROW_H,
                    TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter,
                    Font          = new Font("Segoe UI", 7.2f, FontStyle.Bold),
                    ForeColor     = C_InkSecondary,
                    BackColor     = C_TblHdr,
                    Borders       = DevExpress.XtraPrinting.BorderSide.Right | DevExpress.XtraPrinting.BorderSide.Bottom,
                    BorderColor   = C_Divider,
                    Padding       = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0)
                });
            }

            tbl.Rows.Add(row);
            band.Controls.Add(tbl);
            return band;
        }

        private DetailBand BuildDetail()
        {
            const float ROW_H = 21f;
            var band = new DetailBand { HeightF = ROW_H, BackColor = C_RowEven };

            var tbl = new XRTable { BoundsF = new RectangleF(0, 0, 1100f, ROW_H) };
            var row = new XRTableRow { EvenStyleName = "EvenRow", OddStyleName = "OddRow" };

            // Expression per column — matches Columns order
            string[] exprs = {
                "[ChassisNo]",
                "[ItemName]",
                "[CarTypeName]",
                "[CarModelName]",
                "[CarColorName]",
                "[ManufacturingYear]",
                "Iif(IsNull([PurchaseDate]), '', FormatString('{0:yyyy/MM/dd}', [PurchaseDate]))",
                "Iif(IsNull([SalesDate]),   '', FormatString('{0:yyyy/MM/dd}', [SalesDate]))",
                "Iif(IsNull([PurchaseCost]), '', FormatString('{0:N2}', [PurchaseCost]))",
                "Iif(IsNull([SalePrice]),   '', FormatString('{0:N2}', [SalePrice]))",
                "Iif(IsNull([ProfitValue]), '', FormatString('{0:N2}', [ProfitValue]))",
                "[VehicleStatusName]",
                "[CustomerName]",
                "Iif(IsNull([DaysToSell]), '-', [DaysToSell])"
            };

            for (int i = 0; i < Columns.Length; i++)
            {
                var cell = new XRTableCell {
                    WidthF        = Columns[i].Width,
                    HeightF       = ROW_H,
                    TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter,
                    Font          = new Font("Tahoma", 6.8f),
                    ForeColor     = C_InkPrimary,
                    Borders       = DevExpress.XtraPrinting.BorderSide.Right | DevExpress.XtraPrinting.BorderSide.Bottom,
                    BorderColor   = C_Divider,
                    Padding       = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0)
                };
                cell.ExpressionBindings.Add(
                    new ExpressionBinding("BeforePrint", "Text", exprs[i]));
                row.Cells.Add(cell);
            }

            tbl.Rows.Add(row);
            band.Controls.Add(tbl);
            return band;
        }

        private DetailReportBand BuildDetailSection(DataTable dtDetail)
        {
            var detailReport = new DetailReportBand {
                DataSource = dtDetail,
                DataMember = ""
            };

            var coverHeader = new ReportHeaderBand {
                HeightF = 62f,
                PageBreak = DevExpress.XtraReports.UI.PageBreak.BeforeBand
            };
            coverHeader.Controls.Add(Panel(0, 0, 1100f, 44f, C_HdrBg, DevExpress.XtraPrinting.BorderSide.All));
            coverHeader.Controls.Add(Label(
                "البيانات التفصيلية للمركبات  |  Detailed Vehicle Sales Appendix",
                0f, 2f, 1100f, 26f,
                new Font("Segoe UI", 12f, FontStyle.Bold), C_InkPrimary,
                DevExpress.XtraPrinting.TextAlignment.MiddleCenter));
            coverHeader.Controls.Add(Label(
                "تفاصيل العمليات حسب نتائج التقرير التنفيذي",
                0f, 24f, 1100f, 16f,
                new Font("Tahoma", 7f), C_Muted,
                DevExpress.XtraPrinting.TextAlignment.MiddleCenter));
            coverHeader.Controls.Add(Panel(0, 43f, 1100f, 1f, C_Divider));

            detailReport.Bands.Add(coverHeader);
            detailReport.Bands.Add(BuildPageHeader());
            detailReport.Bands.Add(BuildDetail());
            return detailReport;
        }

        private PageFooterBand BuildPageFooter()
        {
            var band = new PageFooterBand { HeightF = 24f, BackColor = C_HdrBg };

            band.Controls.Add(new XRPageInfo {
                BoundsF       = new RectangleF(0, 3f, 260f, 18f),
                PageInfo      = DevExpress.XtraPrinting.PageInfo.NumberOfTotal,
                Format        = "صفحة {0} من {1}",
                TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter,
                Font          = new Font("Segoe UI", 7f),
                ForeColor     = C_Muted,
                BackColor     = Color.Transparent
            });

            band.Controls.Add(Label(
                "نظام MySoft ERP  —  لوحة تحكم مبيعات السيارات  |  Vehicle Sales Executive Dashboard",
                260f, 3f, 580f, 18f,
                new Font("Segoe UI", 7f), C_Muted,
                DevExpress.XtraPrinting.TextAlignment.MiddleCenter));

            band.Controls.Add(Label(
                DateTime.Now.ToString("yyyy/MM/dd"),
                840f, 3f, 260f, 18f,
                new Font("Segoe UI", 7f), C_Muted,
                DevExpress.XtraPrinting.TextAlignment.MiddleCenter));

            return band;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Chart builders
        // ─────────────────────────────────────────────────────────────────────

        private XRChart BuildBarChart(DataTable dt, float x, float y, float w, float h)
        {
            var chart = new XRChart {
                BoundsF     = new RectangleF(x, y, w, h),
                BackColor   = C_CardBg,
                BorderColor = C_Divider,
                Borders     = DevExpress.XtraPrinting.BorderSide.All,
                BorderWidth = 1f
            };
            StyleChartArea(chart);

            var title = new ChartTitle();
            title.Text      = "أفضل الموديلات مبيعاً  –  Top Models by Revenue";
            title.TextColor = C_InkSecondary;
            title.Font      = new Font("Segoe UI", 8.3f, FontStyle.Bold);
            title.Alignment = System.Drawing.StringAlignment.Center;
            chart.Titles.Add(title);

            // DataSource set on the series so it is independent of the report's
            // main DataSource (the mode-1 detail DataTable).
            var series = new Series("المبيعات", ViewType.Bar);
            series.ArgumentDataMember = "CarModelName";
            series.DataSource         = dt;
            series.ValueDataMembers.AddRange(new[] { "TotalSales" });

            var barView = (BarSeriesView)series.View;
            barView.Color = C_Blue;

            series.Label.Visible = false;

            chart.Series.Add(series);
            chart.Legend.Visible = false;

            return chart;
        }

        private XRChart BuildPieChart(DataTable dt, float x, float y, float w, float h)
        {
            var chart = new XRChart {
                BoundsF     = new RectangleF(x, y, w, h),
                BackColor   = C_CardBg,
                BorderColor = C_Divider,
                Borders     = DevExpress.XtraPrinting.BorderSide.All,
                BorderWidth = 1f
            };
            StyleChartArea(chart);

            var title = new ChartTitle();
            title.Text      = "توزيع المركبات حسب الحالة  –  Status Distribution";
            title.TextColor = C_InkSecondary;
            title.Font      = new Font("Segoe UI", 8.3f, FontStyle.Bold);
            title.Alignment = System.Drawing.StringAlignment.Center;
            chart.Titles.Add(title);

            var series = new Series("الحالة", ViewType.Pie);
            series.ArgumentDataMember = "VehicleStatusName";
            series.DataSource         = dt;
            series.ValueDataMembers.AddRange(new[] { "CarsCount" });
            series.Label.Visible = false;

            chart.Series.Add(series);
            chart.PaletteBaseColorNumber = 4;
            chart.PaletteName = "Aspect";

            chart.Legend.Visible             = true;
            chart.Legend.BackColor           = Color.Transparent;
            chart.Legend.TextColor           = C_Muted;
            chart.Legend.Font                = new Font("Segoe UI", 6.6f);
            chart.Legend.AlignmentHorizontal = LegendAlignmentHorizontal.Right;
            chart.Legend.AlignmentVertical   = LegendAlignmentVertical.Center;

            return chart;
        }

        /// <summary>Generic parameterised bar chart for the second analytics row.</summary>
        private XRChart BuildBarChart4(DataTable dt, float x, float y, float w, float h,
            string titleText, string argMember, string valueMember, Color barColor)
        {
            var chart = new XRChart {
                BoundsF     = new RectangleF(x, y, w, h),
                BackColor   = C_CardBg,
                BorderColor = C_Divider,
                Borders     = DevExpress.XtraPrinting.BorderSide.All,
                BorderWidth = 1f
            };
            StyleChartArea(chart);
            var ct = new ChartTitle();
            ct.Text      = titleText;
            ct.TextColor = C_InkSecondary;
            ct.Font      = new Font("Segoe UI", 7.4f, FontStyle.Bold);
            ct.Alignment = System.Drawing.StringAlignment.Center;
            chart.Titles.Add(ct);

            var series = new Series("", ViewType.Bar);
            series.ArgumentDataMember = argMember;
            series.DataSource         = dt;
            series.ValueDataMembers.AddRange(new[] { valueMember });
            ((BarSeriesView)series.View).Color = barColor;
            series.Label.Visible = false;
            chart.Series.Add(series);
            chart.Legend.Visible = false;
            return chart;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Widget helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>A solid-colour XRPanel with optional soft border.</summary>
        private static XRPanel Panel(float x, float y, float w, float h, Color bg,
            DevExpress.XtraPrinting.BorderSide borders = DevExpress.XtraPrinting.BorderSide.None)
        {
            return new XRPanel {
                BoundsF     = new RectangleF(x, y, w, h),
                BackColor   = bg,
                BorderColor = C_Divider,
                Borders     = borders
            };
        }

        /// <summary>KPI card: coloured top bar + title + large value.</summary>
        private static XRPanel KpiCard(float x, float y, float w, float h,
            string title, string value, Color accent)
        {
            var card = new XRPanel {
                BoundsF     = new RectangleF(x, y, w, h),
                BackColor   = C_CardBg,
                BorderColor = C_Divider,
                Borders     = DevExpress.XtraPrinting.BorderSide.All,
                BorderWidth = 1f
            };

            // Thin top accent bar
            card.Controls.Add(new XRPanel {
                BoundsF   = new RectangleF(0, 0, w, 2f),
                BackColor = accent,
                Borders   = DevExpress.XtraPrinting.BorderSide.None
            });
            // restrained left accent
            card.Controls.Add(new XRPanel {
                BoundsF   = new RectangleF(0, 2f, 2f, h - 2f),
                BackColor = Color.FromArgb(75, accent),
                Borders   = DevExpress.XtraPrinting.BorderSide.None
            });

            // Title
            card.Controls.Add(Label(title,
                8f, 8f, w - 14f, 18f,
                new Font("Segoe UI", 7.7f), C_Muted,
                DevExpress.XtraPrinting.TextAlignment.MiddleCenter));

            // Large KPI value
            card.Controls.Add(Label(value,
                8f, 28f, w - 14f, 40f,
                new Font("Segoe UI", 19f, FontStyle.Bold), C_InkPrimary,
                DevExpress.XtraPrinting.TextAlignment.MiddleCenter));
            card.Controls.Add(Panel(8f, h - 9f, w - 16f, 1f, C_Divider));

            return card;
        }

        private static XRLabel Label(string text, float x, float y, float w, float h,
            Font font, Color fore, DevExpress.XtraPrinting.TextAlignment align)
        {
            return new XRLabel {
                Text          = text,
                BoundsF       = new RectangleF(x, y, w, h),
                Font          = font,
                ForeColor     = fore,
                BackColor     = Color.Transparent,
                TextAlignment = align,
                Borders       = DevExpress.XtraPrinting.BorderSide.None,
                CanGrow       = false,
                WordWrap      = false
            };
        }



        private static void StyleChartArea(XRChart chart)
        {
            if (chart.Diagram is XYDiagram xy)
            {
                xy.DefaultPane.BackColor = Color.Transparent;
                xy.AxisX.Label.Font = new Font("Segoe UI", 6.6f);
                xy.AxisY.Label.Font = new Font("Segoe UI", 6.6f);
                xy.AxisX.Label.TextColor = C_Muted;
                xy.AxisY.Label.TextColor = C_Muted;
                xy.AxisX.GridLines.Visible = false;
                xy.AxisY.GridLines.Visible = true;
                xy.AxisY.GridLines.Color = C_SlateSoft;
                xy.AxisX.Color = C_SlateSoft;
                xy.AxisY.Color = C_SlateSoft;
                xy.AxisX.Tickmarks.MinorVisible = false;
                xy.AxisY.Tickmarks.MinorVisible = false;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  KPI value formatters
        // ─────────────────────────────────────────────────────────────────────

        private static string KpiInt(DataRow r, string col)
        {
            if (r == null || r.IsNull(col)) return "0";
            return long.TryParse(r[col].ToString(), out long v) ? v.ToString("N0") : r[col].ToString();
        }

        private static string KpiMoney(DataRow r, string col)
        {
            if (r == null || r.IsNull(col)) return "0.00";
            return decimal.TryParse(r[col].ToString(), out decimal v) ? v.ToString("N2") : r[col].ToString();
        }

        private static string KpiDays(DataRow r, string col)
        {
            if (r == null || r.IsNull(col)) return "-";
            return decimal.TryParse(r[col].ToString(), out decimal v) ? Math.Round(v, 0).ToString("N0") : r[col].ToString();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Data access
        // ─────────────────────────────────────────────────────────────────────

        private static DataTable Exec(int mode,
            DateTime? dateFrom, DateTime? dateTo,
            int? branchId, int? warehouseId,
            int? carTypeId, int? carModelId, int? carColorId,
            int? manufacturingYear, int? vehicleStatusId,
            string chassisNo, int? vendorOrCustomerId)
        {
            var dt = new DataTable();
            try
            {
                string cs = ConfigurationManager
                    .ConnectionStrings["MyERP_ConnectionString"]?.ConnectionString;
                if (string.IsNullOrEmpty(cs)) return dt;

                using (var conn = new SqlConnection(cs))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand("dbo.VehicleSalesDashboard", conn))
                    {
                        cmd.CommandType    = CommandType.StoredProcedure;
                        cmd.CommandTimeout = 60;

                        cmd.Parameters.AddWithValue("@ReportMode",         mode);
                        cmd.Parameters.AddWithValue("@DateFrom",           (object)dateFrom          ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DateTo",             (object)dateTo             ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@BranchId",           (object)branchId           ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@WarehouseId",        (object)warehouseId        ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CarTypeId",          (object)carTypeId          ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CarModelId",         (object)carModelId         ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CarColorId",         (object)carColorId         ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ManufacturingYear",  (object)manufacturingYear  ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@VehicleStatusId",    (object)vehicleStatusId    ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ChassisNo",          (object)chassisNo          ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@VendorOrCustomerId", (object)vendorOrCustomerId ?? DBNull.Value);

                        new SqlDataAdapter(cmd).Fill(dt);
                    }
                }
            }
            catch { /* return empty table so report still renders */ }
            return dt;
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            DevExpress.DataAccess.Sql.StoredProcQuery storedProcQuery1 = new DevExpress.DataAccess.Sql.StoredProcQuery();
            DevExpress.DataAccess.Sql.QueryParameter queryParameter1 = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.DataAccess.Sql.QueryParameter queryParameter2 = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.DataAccess.Sql.QueryParameter queryParameter3 = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.DataAccess.Sql.QueryParameter queryParameter4 = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.DataAccess.Sql.QueryParameter queryParameter5 = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.DataAccess.Sql.QueryParameter queryParameter6 = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.DataAccess.Sql.QueryParameter queryParameter7 = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.DataAccess.Sql.QueryParameter queryParameter8 = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.DataAccess.Sql.QueryParameter queryParameter9 = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.DataAccess.Sql.QueryParameter queryParameter10 = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.DataAccess.Sql.QueryParameter queryParameter11 = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.DataAccess.Sql.QueryParameter queryParameter12 = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.DataAccess.Sql.QueryParameter queryParameter13 = new DevExpress.DataAccess.Sql.QueryParameter();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VehicleSalesDashboard_Report));
            this.topMarginBand1 = new DevExpress.XtraReports.UI.TopMarginBand();
            this.detailBand1 = new DevExpress.XtraReports.UI.DetailBand();
            this.bottomMarginBand1 = new DevExpress.XtraReports.UI.BottomMarginBand();
            this.sqlDataSource1 = new DevExpress.DataAccess.Sql.SqlDataSource(this.components);
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            // 
            // topMarginBand1
            // 
            this.topMarginBand1.Name = "topMarginBand1";
            // 
            // detailBand1
            // 
            this.detailBand1.Name = "detailBand1";
            // 
            // bottomMarginBand1
            // 
            this.bottomMarginBand1.Name = "bottomMarginBand1";
            // 
            // sqlDataSource1
            // 
            this.sqlDataSource1.ConnectionName = "MyERP_ConnectionString";
            this.sqlDataSource1.Name = "sqlDataSource1";
            storedProcQuery1.Name = "VehicleSalesDashboard";
            queryParameter1.Name = "@ReportMode";
            queryParameter1.Type = typeof(int);
            queryParameter1.ValueInfo = "1";
            queryParameter2.Name = "@DepartmentId";
            queryParameter2.Type = typeof(int);
            queryParameter2.ValueInfo = "0";
            queryParameter3.Name = "@WarehouseId";
            queryParameter3.Type = typeof(int);
            queryParameter3.ValueInfo = "0";
            queryParameter4.Name = "@BranchId";
            queryParameter4.Type = typeof(int);
            queryParameter4.ValueInfo = "0";
            queryParameter5.Name = "@DateFrom";
            queryParameter5.Type = typeof(System.DateTime);
            queryParameter5.ValueInfo = "1753-01-01";
            queryParameter6.Name = "@DateTo";
            queryParameter6.Type = typeof(System.DateTime);
            queryParameter6.ValueInfo = "1753-01-01";
            queryParameter7.Name = "@CarTypeId";
            queryParameter7.Type = typeof(int);
            queryParameter7.ValueInfo = "0";
            queryParameter8.Name = "@CarModelId";
            queryParameter8.Type = typeof(int);
            queryParameter8.ValueInfo = "0";
            queryParameter9.Name = "@CarColorId";
            queryParameter9.Type = typeof(int);
            queryParameter9.ValueInfo = "0";
            queryParameter10.Name = "@ManufacturingYear";
            queryParameter10.Type = typeof(int);
            queryParameter10.ValueInfo = "0";
            queryParameter11.Name = "@VendorOrCustomerId";
            queryParameter11.Type = typeof(int);
            queryParameter11.ValueInfo = "0";
            queryParameter12.Name = "@VehicleStatusId";
            queryParameter12.Type = typeof(int);
            queryParameter12.ValueInfo = "0";
            queryParameter13.Name = "@ChassisNo";
            queryParameter13.Type = typeof(string);
            storedProcQuery1.Parameters.AddRange(new DevExpress.DataAccess.Sql.QueryParameter[] {
            queryParameter1,
            queryParameter2,
            queryParameter3,
            queryParameter4,
            queryParameter5,
            queryParameter6,
            queryParameter7,
            queryParameter8,
            queryParameter9,
            queryParameter10,
            queryParameter11,
            queryParameter12,
            queryParameter13});
            storedProcQuery1.StoredProcName = "VehicleSalesDashboard";
            this.sqlDataSource1.Queries.AddRange(new DevExpress.DataAccess.Sql.SqlQuery[] {
            storedProcQuery1});
            this.sqlDataSource1.ResultSchemaSerializable = resources.GetString("sqlDataSource1.ResultSchemaSerializable");
            // 
            // VehicleSalesDashboard_Report
            // 
            this.Bands.AddRange(new DevExpress.XtraReports.UI.Band[] {
            this.topMarginBand1,
            this.detailBand1,
            this.bottomMarginBand1});
            this.ComponentStorage.AddRange(new System.ComponentModel.IComponent[] {
            this.sqlDataSource1});
            this.DataMember = "VehicleSalesDashboard";
            this.DataSource = this.sqlDataSource1;
            this.Version = "23.1";
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();

        }
    }
}
