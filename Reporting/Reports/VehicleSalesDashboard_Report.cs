using DevExpress.XtraCharts;
using DevExpress.XtraReports.UI;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;

namespace MyERP.Reporting.Reports
{
    /// <summary>
    /// Premium dark-theme executive dashboard for vehicle sales.
    /// Uses dbo.VehicleSalesDashboard with modes 1, 3, 4, 8.
    /// All data is loaded via ADO.NET in the constructor so no
    /// DevExpress designer / resx is needed.
    /// </summary>
    public class VehicleSalesDashboard_Report : XtraReport
    {
        // ── Colour palette ────────────────────────────────────────────────────
        private static readonly Color C_PageBg   = Color.FromArgb(13,  17,  38);
        private static readonly Color C_CardBg   = Color.FromArgb(22,  30,  58);
        private static readonly Color C_HdrBg    = Color.FromArgb( 8,  12,  28);
        private static readonly Color C_TblHdr   = Color.FromArgb(28,  42,  80);
        private static readonly Color C_RowEven  = Color.FromArgb(18,  25,  50);
        private static readonly Color C_RowOdd   = Color.FromArgb(25,  34,  65);
        private static readonly Color C_Divider  = Color.FromArgb(40,  55,  90);
        private static readonly Color C_White    = Color.White;
        private static readonly Color C_Gray     = Color.FromArgb(180, 195, 220);
        private static readonly Color C_Blue     = Color.FromArgb( 52, 152, 219);
        private static readonly Color C_Green    = Color.FromArgb( 46, 204, 113);
        private static readonly Color C_Cyan     = Color.FromArgb( 26, 188, 156);
        private static readonly Color C_Orange   = Color.FromArgb(230, 126,  34);
        private static readonly Color C_Gold     = Color.FromArgb(241, 196,  15);
        private static readonly Color C_Purple   = Color.FromArgb(155,  89, 182);
        private static readonly Color C_Pink     = Color.FromArgb(233,  30,  99);
        private static readonly Color C_Silver   = Color.FromArgb(149, 165, 166);

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

        // ── Constructor ───────────────────────────────────────────────────────
        public VehicleSalesDashboard_Report(
            DateTime? dateFrom,           DateTime? dateTo,
            int?      branchId,           int?      warehouseId,
            int?      carTypeId,          int?      carModelId,
            int?      carColorId,         int?      manufacturingYear,
            int?      vehicleStatusId,    string    chassisNo,
            int?      vendorOrCustomerId)
        {
            // ── Load all data up-front ────────────────────────────────────────
            DataTable dtKpi      = Exec(3, dateFrom, dateTo, branchId, warehouseId, carTypeId, carModelId, carColorId, manufacturingYear, vehicleStatusId, chassisNo, vendorOrCustomerId);
            DataTable dtModels   = Exec(4, dateFrom, dateTo, branchId, warehouseId, carTypeId, carModelId, carColorId, manufacturingYear, vehicleStatusId, chassisNo, vendorOrCustomerId);
            DataTable dtStatus   = Exec(8, dateFrom, dateTo, branchId, warehouseId, carTypeId, carModelId, carColorId, manufacturingYear, vehicleStatusId, chassisNo, vendorOrCustomerId);
            DataTable dtDetail   = Exec(1, dateFrom, dateTo, branchId, warehouseId, carTypeId, carModelId, carColorId, manufacturingYear, vehicleStatusId, chassisNo, vendorOrCustomerId);

            DataRow   kpi        = dtKpi.Rows.Count > 0 ? dtKpi.Rows[0] : null;

            // ── Page settings ─────────────────────────────────────────────────
            Landscape              = true;
            PaperKind              = DevExpress.Drawing.Printing.DXPaperKind.A4;
            RequestParameters      = false;
            BackColor              = C_PageBg;
            Margins                = new System.Drawing.Printing.Margins(25, 25, 20, 20);

            // ── Report data source (detail table) ─────────────────────────────
            DataSource             = dtDetail;
            DataMember             = "";

            // ── Alternating-row styles ────────────────────────────────────────
            StyleSheet.Add(new XRControlStyle {
                Name        = "EvenRow",
                BackColor   = C_RowEven,
                ForeColor   = C_White,
                BorderColor = C_Divider,
                Borders     = DevExpress.XtraPrinting.BorderSide.Bottom
            });
            StyleSheet.Add(new XRControlStyle {
                Name        = "OddRow",
                BackColor   = C_RowOdd,
                ForeColor   = C_White,
                BorderColor = C_Divider,
                Borders     = DevExpress.XtraPrinting.BorderSide.Bottom
            });

            // ── Assemble bands ────────────────────────────────────────────────
            Bands.Add(new TopMarginBand    { HeightF = 20 });
            Bands.Add(BuildReportHeader(kpi, dtModels, dtStatus, dateFrom, dateTo));
            Bands.Add(BuildPageHeader());
            Bands.Add(BuildDetail());
            Bands.Add(BuildPageFooter());
            Bands.Add(new BottomMarginBand { HeightF = 20 });
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Band builders
        // ─────────────────────────────────────────────────────────────────────

        private ReportHeaderBand BuildReportHeader(
            DataRow kpi, DataTable dtModels, DataTable dtStatus,
            DateTime? dateFrom, DateTime? dateTo)
        {
            // Layout constants
            const float W        = 1100f;   // usable width
            const float CARD_H   =   82f;
            const float CHART_H  =  202f;

            // KPI card geometry (4 cards per row, 8px gap)
            float gap    = 8f;
            float cardW  = (W - 3 * gap) / 4f;   // ≈ 272
            float cardX(int i) => i * (cardW + gap);

            // Y offsets
            float yTitle      =   0f;
            float ySubtitle   =  58f;
            float yKpiHdr     =  84f;
            float yKpiRow1    = 106f;
            float yKpiRow2    = yKpiRow1 + CARD_H + 8f;
            float yChartHdr   = yKpiRow2 + CARD_H + 10f;
            float yCharts     = yChartHdr + 24f;
            float yDivider    = yCharts  + CHART_H + 4f;
            float totalH      = yDivider + 4f;

            var band = new ReportHeaderBand { HeightF = totalH, BackColor = C_PageBg };

            // ── Title bar ─────────────────────────────────────────────────────
            var titlePanel = Panel(0, yTitle, W, 54f, C_HdrBg);
            titlePanel.Controls.Add(Label(
                "لوحة تحكم مبيعات السيارات  |  Vehicle Sales Executive Dashboard",
                0, 0, W, 54f,
                new Font("Arial", 15f, FontStyle.Bold), C_White,
                DevExpress.XtraPrinting.TextAlignment.MiddleCenter));
            // blue accent underline
            titlePanel.Controls.Add(Panel(0, 51f, W, 3f, C_Blue));
            band.Controls.Add(titlePanel);

            // ── Subtitle (date / print time) ──────────────────────────────────
            string dr = (dateFrom.HasValue || dateTo.HasValue)
                ? $"الفترة:  {(dateFrom.HasValue ? dateFrom.Value.ToString("yyyy/MM/dd") : "---")}  ←→  {(dateTo.HasValue ? dateTo.Value.ToString("yyyy/MM/dd") : "---")}"
                : "جميع الفترات";
            band.Controls.Add(Label(
                $"{dr}          تاريخ الطباعة: {DateTime.Now:yyyy/MM/dd  HH:mm}",
                0, ySubtitle, W, 22f,
                new Font("Arial", 7.5f), C_Gray,
                DevExpress.XtraPrinting.TextAlignment.MiddleCenter));

            // ── KPI section header ────────────────────────────────────────────
            var kpiHdrPanel = Panel(0, yKpiHdr, W, 20f, C_TblHdr);
            kpiHdrPanel.Controls.Add(Label(
                "◆  مؤشرات الأداء الرئيسية  –  Key Performance Indicators",
                10f, 0, W - 20f, 20f,
                new Font("Arial", 8f, FontStyle.Bold), C_Gold,
                DevExpress.XtraPrinting.TextAlignment.MiddleCenter));
            band.Controls.Add(kpiHdrPanel);

            // ── KPI Row 1 ─────────────────────────────────────────────────────
            (string title, string value, Color accent)[] row1 = {
                ("إجمالي المركبات",   KpiInt  (kpi, "TotalVehicles"),  C_Blue),
                ("مركبات في المخزن",  KpiInt  (kpi, "InStockCars"),    C_Green),
                ("مركبات مباعة",      KpiInt  (kpi, "SoldCars"),       C_Cyan),
                ("مركبات مرتجعة",     KpiInt  (kpi, "ReturnedCars"),   C_Orange),
            };
            for (int i = 0; i < row1.Length; i++)
                band.Controls.Add(KpiCard(cardX(i), yKpiRow1, cardW, CARD_H,
                    row1[i].title, row1[i].value, row1[i].accent));

            // ── KPI Row 2 ─────────────────────────────────────────────────────
            (string title, string value, Color accent)[] row2 = {
                ("إجمالي المبيعات",   KpiMoney(kpi, "TotalSales"),     C_Gold),
                ("إجمالي الأرباح",    KpiMoney(kpi, "TotalProfit"),    C_Purple),
                ("متوسط سعر البيع",   KpiMoney(kpi, "AvgSalePrice"),   C_Pink),
                ("متوسط أيام البيع",  KpiDays (kpi, "AvgDaysToSell"),  C_Silver),
            };
            for (int i = 0; i < row2.Length; i++)
                band.Controls.Add(KpiCard(cardX(i), yKpiRow2, cardW, CARD_H,
                    row2[i].title, row2[i].value, row2[i].accent));

            // ── Chart section header ──────────────────────────────────────────
            var chrtHdrPanel = Panel(0, yChartHdr, W, 22f, C_TblHdr);
            chrtHdrPanel.Controls.Add(Label(
                "◆  تحليل وإحصائيات المبيعات  –  Sales Analytics",
                10f, 0, W - 20f, 22f,
                new Font("Arial", 8f, FontStyle.Bold), C_Cyan,
                DevExpress.XtraPrinting.TextAlignment.MiddleCenter));
            band.Controls.Add(chrtHdrPanel);

            // ── Charts ────────────────────────────────────────────────────────
            float halfW  = (W - 6f) / 2f;   // ≈ 547
            band.Controls.Add(BuildBarChart (dtModels, 0f,           yCharts, halfW, CHART_H));
            band.Controls.Add(BuildPieChart (dtStatus, halfW + 6f,   yCharts, halfW, CHART_H));

            // ── Divider before detail table ───────────────────────────────────
            band.Controls.Add(Panel(0, yDivider, W, 3f, C_Blue));

            return band;
        }

        private PageHeaderBand BuildPageHeader()
        {
            const float ROW_H = 26f;
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
                    Font          = new Font("Arial", 7.5f, FontStyle.Bold),
                    ForeColor     = C_White,
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
            const float ROW_H = 20f;
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
                    Font          = new Font("Arial", 7f),
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

        private PageFooterBand BuildPageFooter()
        {
            var band = new PageFooterBand { HeightF = 24f, BackColor = C_HdrBg };

            band.Controls.Add(new XRPageInfo {
                BoundsF       = new RectangleF(0, 3f, 260f, 18f),
                PageInfo      = DevExpress.XtraPrinting.PageInfo.NumberOfTotal,
                Format        = "صفحة {0} من {1}",
                TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter,
                Font          = new Font("Arial", 7f),
                ForeColor     = C_Gray,
                BackColor     = Color.Transparent
            });

            band.Controls.Add(Label(
                "نظام MySoft ERP  —  لوحة تحكم مبيعات السيارات  |  Vehicle Sales Executive Dashboard",
                260f, 3f, 580f, 18f,
                new Font("Arial", 7f), C_Gray,
                DevExpress.XtraPrinting.TextAlignment.MiddleCenter));

            band.Controls.Add(Label(
                DateTime.Now.ToString("yyyy/MM/dd"),
                840f, 3f, 260f, 18f,
                new Font("Arial", 7f), C_Gray,
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
                Borders     = DevExpress.XtraPrinting.BorderSide.All
            };

            chart.Titles.Add(new ChartTitle {
                Text      = "أفضل الموديلات مبيعاً  –  Top Models by Revenue",
                TextColor = C_White,
                Font      = new Font("Arial", 8f, FontStyle.Bold),
                Alignment = System.Drawing.StringAlignment.Center
            });

            var series = new Series("المبيعات", ViewType.Bar) {
                ArgumentDataMember = "CarModelName",
                DataSource         = dt,
                DataSourceMember   = ""
            };
            series.ValueDataMembers.AddRange(new[] { "TotalSales" });

            var barView = (BarSeriesView)series.View;
            barView.Color       = C_Blue;
            barView.BorderColor = Color.Transparent;

            series.Label.Visible        = true;
            series.Label.TextColor      = C_White;
            series.Label.Font           = new Font("Arial", 6f);
            series.Label.BackColor      = Color.Transparent;
            series.Label.Border.Visible = false;

            chart.Series.Add(series);

            // Style the auto-created XY diagram
            if (chart.Diagram is XYDiagram d)
            {
                d.DefaultPane.BackColor   = Color.Transparent;
                d.DefaultPane.BorderColor = Color.Transparent;

                d.AxisX.Label.TextColor        = C_Gray;
                d.AxisX.Label.Font             = new Font("Arial", 6f);
                d.AxisX.LineColor              = C_Divider;
                d.AxisX.Color                  = C_Divider;
                d.AxisX.MajorGridlines.Visible = false;
                d.AxisX.MinorGridlines.Visible = false;
                d.AxisX.Tickmarks.Visible      = false;

                d.AxisY.Label.TextColor        = C_Gray;
                d.AxisY.Label.Font             = new Font("Arial", 6f);
                d.AxisY.LineColor              = C_Divider;
                d.AxisY.Color                  = C_Divider;
                d.AxisY.MajorGridlines.Color   = Color.FromArgb(35, 50, 80);
                d.AxisY.MajorGridlines.Visible = true;
                d.AxisY.MinorGridlines.Visible = false;
                d.AxisY.Tickmarks.Visible      = false;
            }

            chart.Legend.Visible = false;

            return chart;
        }

        private XRChart BuildPieChart(DataTable dt, float x, float y, float w, float h)
        {
            var chart = new XRChart {
                BoundsF     = new RectangleF(x, y, w, h),
                BackColor   = C_CardBg,
                BorderColor = C_Divider,
                Borders     = DevExpress.XtraPrinting.BorderSide.All
            };

            chart.Titles.Add(new ChartTitle {
                Text      = "توزيع المركبات حسب الحالة  –  Status Distribution",
                TextColor = C_White,
                Font      = new Font("Arial", 8f, FontStyle.Bold),
                Alignment = System.Drawing.StringAlignment.Center
            });

            var series = new Series("الحالة", ViewType.Pie) {
                ArgumentDataMember = "VehicleStatusName",
                DataSource         = dt,
                DataSourceMember   = ""
            };
            series.ValueDataMembers.AddRange(new[] { "CarsCount" });

            var pieView = (PieSeriesView)series.View;
            pieView.HoleRadiusPercent = 38;  // donut style
            pieView.Border.Color      = C_PageBg;

            series.Label.Visible        = false;  // legend handles labelling

            chart.Series.Add(series);

            chart.Legend.Visible                = true;
            chart.Legend.BackColor              = Color.Transparent;
            chart.Legend.TextColor              = C_Gray;
            chart.Legend.Font                   = new Font("Arial", 7f);
            chart.Legend.Border.Color           = Color.Transparent;
            chart.Legend.AlignmentHorizontal    = LegendAlignmentHorizontal.Right;
            chart.Legend.AlignmentVertical      = LegendAlignmentVertical.Center;

            return chart;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Widget helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>A solid-colour XRPanel (no border).</summary>
        private static XRPanel Panel(float x, float y, float w, float h, Color bg)
        {
            return new XRPanel {
                BoundsF     = new RectangleF(x, y, w, h),
                BackColor   = bg,
                BorderColor = Color.Transparent,
                Borders     = DevExpress.XtraPrinting.BorderSide.None
            };
        }

        /// <summary>KPI card: coloured top bar + title + large value.</summary>
        private static XRPanel KpiCard(float x, float y, float w, float h,
            string title, string value, Color accent)
        {
            var card = new XRPanel {
                BoundsF     = new RectangleF(x, y, w, h),
                BackColor   = C_CardBg,
                BorderColor = Color.Transparent,
                Borders     = DevExpress.XtraPrinting.BorderSide.None
            };

            // Thick top accent bar
            card.Controls.Add(new XRPanel {
                BoundsF   = new RectangleF(0, 0, w, 5f),
                BackColor = accent,
                Borders   = DevExpress.XtraPrinting.BorderSide.None
            });
            // Thin left accent strip
            card.Controls.Add(new XRPanel {
                BoundsF   = new RectangleF(0, 5f, 3f, h - 5f),
                BackColor = Color.FromArgb(90, accent),
                Borders   = DevExpress.XtraPrinting.BorderSide.None
            });

            // Title
            card.Controls.Add(Label(title,
                8f, 8f, w - 14f, 18f,
                new Font("Arial", 7.5f), C_Gray,
                DevExpress.XtraPrinting.TextAlignment.MiddleCenter));

            // Large KPI value
            card.Controls.Add(Label(value,
                8f, 28f, w - 14f, 40f,
                new Font("Arial", 20f, FontStyle.Bold), accent,
                DevExpress.XtraPrinting.TextAlignment.MiddleCenter));

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
    }
}
