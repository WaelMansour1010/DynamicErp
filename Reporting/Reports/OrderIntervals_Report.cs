using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;
using System.Web;
using MyERP.Models;
using System.Linq;
using MyERP.Controllers;

/// <summary>
/// Summary description for OrderIntervals_Report
/// </summary>
public class OrderIntervals_Report : DevExpress.XtraReports.UI.XtraReport
{
    private MySoftERPEntity db = new MySoftERPEntity();
    private DevExpress.DataAccess.Sql.SqlDataSource sqlDataSource1;
    private XRControlStyle Title;
    private XRControlStyle DetailCaption1;
    private XRControlStyle DetailData1;
    private XRControlStyle DetailData3_Odd;
    private XRControlStyle PageInfo;
    private TopMarginBand TopMargin;
    private BottomMarginBand BottomMargin;
    private XRPageInfo pageInfo2;
    private ReportHeaderBand ReportHeader;
    private GroupHeaderBand GroupHeader1;
    private XRTable table1;
    private XRTableRow tableRow1;
    private XRTableCell tableCell1;
    private XRTableCell tableCell2;
    private XRTableCell tableCell3;
    private XRTableCell tableCell4;
    private XRTableCell tableCell5;
    private XRTableCell tableCell6;
    private DetailBand Detail;
    private XRTable table2;
    private XRTableRow tableRow2;
    private XRTableCell tableCell7;
    private XRTableCell tableCell8;
    private XRTableCell tableCell9;
    private XRTableCell tableCell10;
    private XRTableCell tableCell11;
    private XRTableCell tableCell12;
    private DevExpress.XtraReports.Parameters.Parameter DateFrom;
    private DevExpress.XtraReports.Parameters.Parameter DateTo;
    private XRLabel xrLabel4;
    private XRLabel Time;
    private XRLabel xrLabel6;
    private XRLabel User;
    private XRLabel xrLabel7;
    private XRLabel xrLabel8;
    private XRLabel xrLabel16;
    private XRLabel xrLabel15;
    private XRLabel xrLabel14;
    private XRLabel xrLabel13;

    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    public OrderIntervals_Report()
    {
        InitializeComponent();
        User.Text = HttpContext.Current.User.Identity.Name;
        //
        // TODO: Add constructor logic here
        //
        var helper = new HelperController().GetOpenPeriodBeginningEndingForReports();
        this.DateFrom.ValueInfo = helper.start;
        this.DateTo.ValueInfo = helper.end;
    }

    /// <summary> 
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
     
        this.components = new System.ComponentModel.Container();
        DevExpress.DataAccess.Sql.StoredProcQuery storedProcQuery1 = new DevExpress.DataAccess.Sql.StoredProcQuery();
        DevExpress.DataAccess.Sql.QueryParameter queryParameter1 = new DevExpress.DataAccess.Sql.QueryParameter();
        DevExpress.DataAccess.Sql.QueryParameter queryParameter2 = new DevExpress.DataAccess.Sql.QueryParameter();
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(OrderIntervals_Report));
        this.sqlDataSource1 = new DevExpress.DataAccess.Sql.SqlDataSource(this.components);
        this.Title = new DevExpress.XtraReports.UI.XRControlStyle();
        this.DetailCaption1 = new DevExpress.XtraReports.UI.XRControlStyle();
        this.DetailData1 = new DevExpress.XtraReports.UI.XRControlStyle();
        this.DetailData3_Odd = new DevExpress.XtraReports.UI.XRControlStyle();
        this.PageInfo = new DevExpress.XtraReports.UI.XRControlStyle();
        this.TopMargin = new DevExpress.XtraReports.UI.TopMarginBand();
        this.BottomMargin = new DevExpress.XtraReports.UI.BottomMarginBand();
        this.pageInfo2 = new DevExpress.XtraReports.UI.XRPageInfo();
        this.ReportHeader = new DevExpress.XtraReports.UI.ReportHeaderBand();
        this.xrLabel16 = new DevExpress.XtraReports.UI.XRLabel();
        this.xrLabel15 = new DevExpress.XtraReports.UI.XRLabel();
        this.xrLabel14 = new DevExpress.XtraReports.UI.XRLabel();
        this.xrLabel13 = new DevExpress.XtraReports.UI.XRLabel();
        this.xrLabel7 = new DevExpress.XtraReports.UI.XRLabel();
        this.xrLabel8 = new DevExpress.XtraReports.UI.XRLabel();
        this.xrLabel4 = new DevExpress.XtraReports.UI.XRLabel();
        this.Time = new DevExpress.XtraReports.UI.XRLabel();
        this.xrLabel6 = new DevExpress.XtraReports.UI.XRLabel();
        this.User = new DevExpress.XtraReports.UI.XRLabel();
        this.GroupHeader1 = new DevExpress.XtraReports.UI.GroupHeaderBand();
        this.table1 = new DevExpress.XtraReports.UI.XRTable();
        this.tableRow1 = new DevExpress.XtraReports.UI.XRTableRow();
        this.tableCell1 = new DevExpress.XtraReports.UI.XRTableCell();
        this.tableCell2 = new DevExpress.XtraReports.UI.XRTableCell();
        this.tableCell3 = new DevExpress.XtraReports.UI.XRTableCell();
        this.tableCell4 = new DevExpress.XtraReports.UI.XRTableCell();
        this.tableCell5 = new DevExpress.XtraReports.UI.XRTableCell();
        this.tableCell6 = new DevExpress.XtraReports.UI.XRTableCell();
        this.Detail = new DevExpress.XtraReports.UI.DetailBand();
        this.table2 = new DevExpress.XtraReports.UI.XRTable();
        this.tableRow2 = new DevExpress.XtraReports.UI.XRTableRow();
        this.tableCell7 = new DevExpress.XtraReports.UI.XRTableCell();
        this.tableCell8 = new DevExpress.XtraReports.UI.XRTableCell();
        this.tableCell9 = new DevExpress.XtraReports.UI.XRTableCell();
        this.tableCell10 = new DevExpress.XtraReports.UI.XRTableCell();
        this.tableCell11 = new DevExpress.XtraReports.UI.XRTableCell();
        this.tableCell12 = new DevExpress.XtraReports.UI.XRTableCell();
        this.DateFrom = new DevExpress.XtraReports.Parameters.Parameter();
        this.DateTo = new DevExpress.XtraReports.Parameters.Parameter();
        ((System.ComponentModel.ISupportInitialize)(this.table1)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.table2)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
        // 
        // sqlDataSource1
        // 
        this.sqlDataSource1.ConnectionName = "localhost_MySoftERP_Connection";
        this.sqlDataSource1.Name = "sqlDataSource1";
        storedProcQuery1.Name = "Order_StatusChangeIntervals";
        queryParameter1.Name = "@dateFrom";
        queryParameter1.Type = typeof(DevExpress.DataAccess.Expression);
        queryParameter1.Value = new DevExpress.DataAccess.Expression("?DateFrom", typeof(System.DateTime));
        queryParameter2.Name = "@dateTo";
        queryParameter2.Type = typeof(DevExpress.DataAccess.Expression);
        queryParameter2.Value = new DevExpress.DataAccess.Expression("?DateTo", typeof(System.DateTime));
        storedProcQuery1.Parameters.Add(queryParameter1);
        storedProcQuery1.Parameters.Add(queryParameter2);
        storedProcQuery1.StoredProcName = "Order_StatusChangeIntervals";
        this.sqlDataSource1.Queries.AddRange(new DevExpress.DataAccess.Sql.SqlQuery[] {
            storedProcQuery1});
        this.sqlDataSource1.ResultSchemaSerializable = resources.GetString("sqlDataSource1.ResultSchemaSerializable");
        // 
        // Title
        // 
        this.Title.BackColor = System.Drawing.Color.Transparent;
        this.Title.BorderColor = System.Drawing.Color.Black;
        this.Title.Borders = DevExpress.XtraPrinting.BorderSide.None;
        this.Title.BorderWidth = 1F;
        this.Title.Font = new DevExpress.Drawing.DXFont("Arial", 14.25F);
        this.Title.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(70)))), ((int)(((byte)(80)))));
        this.Title.Name = "Title";
        // 
        // DetailCaption1
        // 
        this.DetailCaption1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(57)))), ((int)(((byte)(159)))), ((int)(((byte)(228)))));
        this.DetailCaption1.BorderColor = System.Drawing.Color.White;
        this.DetailCaption1.Borders = DevExpress.XtraPrinting.BorderSide.Left;
        this.DetailCaption1.BorderWidth = 2F;
        this.DetailCaption1.Font = new DevExpress.Drawing.DXFont("Arial", 8.25F, DevExpress.Drawing.DXFontStyle.Bold);
        this.DetailCaption1.ForeColor = System.Drawing.Color.White;
        this.DetailCaption1.Name = "DetailCaption1";
        this.DetailCaption1.Padding = new DevExpress.XtraPrinting.PaddingInfo(6, 6, 0, 0, 100F);
        this.DetailCaption1.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
        // 
        // DetailData1
        // 
        this.DetailData1.BorderColor = System.Drawing.Color.Transparent;
        this.DetailData1.Borders = DevExpress.XtraPrinting.BorderSide.Left;
        this.DetailData1.BorderWidth = 2F;
        this.DetailData1.Font = new DevExpress.Drawing.DXFont("Arial", 8.25F);
        this.DetailData1.ForeColor = System.Drawing.Color.Black;
        this.DetailData1.Name = "DetailData1";
        this.DetailData1.Padding = new DevExpress.XtraPrinting.PaddingInfo(6, 6, 0, 0, 100F);
        this.DetailData1.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
        // 
        // DetailData3_Odd
        // 
        this.DetailData3_Odd.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(243)))), ((int)(((byte)(245)))), ((int)(((byte)(248)))));
        this.DetailData3_Odd.BorderColor = System.Drawing.Color.Transparent;
        this.DetailData3_Odd.Borders = DevExpress.XtraPrinting.BorderSide.None;
        this.DetailData3_Odd.BorderWidth = 1F;
        this.DetailData3_Odd.Font = new DevExpress.Drawing.DXFont("Arial", 8.25F);
        this.DetailData3_Odd.ForeColor = System.Drawing.Color.Black;
        this.DetailData3_Odd.Name = "DetailData3_Odd";
        this.DetailData3_Odd.Padding = new DevExpress.XtraPrinting.PaddingInfo(6, 6, 0, 0, 100F);
        this.DetailData3_Odd.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
        // 
        // PageInfo
        // 
        this.PageInfo.Font = new DevExpress.Drawing.DXFont("Arial", 8.25F, DevExpress.Drawing.DXFontStyle.Bold);
        this.PageInfo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(70)))), ((int)(((byte)(80)))));
        this.PageInfo.Name = "PageInfo";
        this.PageInfo.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
        // 
        // TopMargin
        // 
        this.TopMargin.HeightF = 40F;
        this.TopMargin.Name = "TopMargin";
        // 
        // BottomMargin
        // 
        this.BottomMargin.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.pageInfo2});
        this.BottomMargin.HeightF = 40F;
        this.BottomMargin.Name = "BottomMargin";
        // 
        // pageInfo2
        // 
        this.pageInfo2.LocationFloat = new DevExpress.Utils.PointFloat(391F, 6F);
        this.pageInfo2.Name = "pageInfo2";
        this.pageInfo2.SizeF = new System.Drawing.SizeF(373F, 23F);
        this.pageInfo2.StyleName = "PageInfo";
        this.pageInfo2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
        this.pageInfo2.TextFormatString = "Page {0} of {1}";
        // 
        // ReportHeader
        // 
        this.ReportHeader.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel16,
            this.xrLabel15,
            this.xrLabel14,
            this.xrLabel13,
            this.xrLabel7,
            this.xrLabel8,
            this.xrLabel4,
            this.Time,
            this.xrLabel6,
            this.User});
        this.ReportHeader.HeightF = 130.9167F;
        this.ReportHeader.Name = "ReportHeader";
        // 
        // xrLabel16
        // 
        this.xrLabel16.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "?DateTo")});
        this.xrLabel16.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F, DevExpress.Drawing.DXFontStyle.Bold);
        this.xrLabel16.LocationFloat = new DevExpress.Utils.PointFloat(535.1013F, 107.9167F);
        this.xrLabel16.Multiline = true;
        this.xrLabel16.Name = "xrLabel16";
        this.xrLabel16.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
        this.xrLabel16.SizeF = new System.Drawing.SizeF(163.7918F, 23F);
        this.xrLabel16.StylePriority.UseFont = false;
        this.xrLabel16.StylePriority.UseTextAlignment = false;
        this.xrLabel16.Text = "xrLabel2";
        this.xrLabel16.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
        // 
        // xrLabel15
        // 
        this.xrLabel15.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "?DateFrom")});
        this.xrLabel15.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F, DevExpress.Drawing.DXFontStyle.Bold);
        this.xrLabel15.LocationFloat = new DevExpress.Utils.PointFloat(235.8335F, 107.9167F);
        this.xrLabel15.Multiline = true;
        this.xrLabel15.Name = "xrLabel15";
        this.xrLabel15.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
        this.xrLabel15.SizeF = new System.Drawing.SizeF(161.9951F, 23F);
        this.xrLabel15.StylePriority.UseFont = false;
        this.xrLabel15.StylePriority.UseTextAlignment = false;
        this.xrLabel15.Text = "xrLabel1";
        this.xrLabel15.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
        // 
        // xrLabel14
        // 
        this.xrLabel14.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F, DevExpress.Drawing.DXFontStyle.Bold);
        this.xrLabel14.LocationFloat = new DevExpress.Utils.PointFloat(435.1011F, 107.9167F);
        this.xrLabel14.Multiline = true;
        this.xrLabel14.Name = "xrLabel14";
        this.xrLabel14.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
        this.xrLabel14.SizeF = new System.Drawing.SizeF(100F, 23F);
        this.xrLabel14.StylePriority.UseFont = false;
        this.xrLabel14.StylePriority.UseTextAlignment = false;
        this.xrLabel14.Text = "إلى تاريخ:";
        this.xrLabel14.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
        // 
        // xrLabel13
        // 
        this.xrLabel13.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F, DevExpress.Drawing.DXFontStyle.Bold);
        this.xrLabel13.LocationFloat = new DevExpress.Utils.PointFloat(135.8336F, 107.9167F);
        this.xrLabel13.Multiline = true;
        this.xrLabel13.Name = "xrLabel13";
        this.xrLabel13.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
        this.xrLabel13.SizeF = new System.Drawing.SizeF(100F, 23F);
        this.xrLabel13.StylePriority.UseFont = false;
        this.xrLabel13.StylePriority.UseTextAlignment = false;
        this.xrLabel13.Text = "من تاريخ:";
        this.xrLabel13.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
        // 
        // xrLabel7
        // 
        this.xrLabel7.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(3)))), ((int)(((byte)(142)))), ((int)(((byte)(188)))));
        this.xrLabel7.Font = new DevExpress.Drawing.DXFont("Arial", 12F, DevExpress.Drawing.DXFontStyle.Bold);
        this.xrLabel7.ForeColor = System.Drawing.Color.White;
        this.xrLabel7.LocationFloat = new DevExpress.Utils.PointFloat(345.4165F, 47.50001F);
        this.xrLabel7.Multiline = true;
        this.xrLabel7.Name = "xrLabel7";
        this.xrLabel7.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
        this.xrLabel7.SizeF = new System.Drawing.SizeF(100F, 23F);
        this.xrLabel7.StylePriority.UseBackColor = false;
        this.xrLabel7.StylePriority.UseFont = false;
        this.xrLabel7.StylePriority.UseForeColor = false;
        this.xrLabel7.StylePriority.UseTextAlignment = false;
        this.xrLabel7.Text = "فترات الطلبات";
        this.xrLabel7.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
        // 
        // xrLabel8
        // 
        this.xrLabel8.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(128)))), ((int)(((byte)(192)))));
        this.xrLabel8.Font = new DevExpress.Drawing.DXFont("Arial", 14F, DevExpress.Drawing.DXFontStyle.Bold);
        this.xrLabel8.ForeColor = System.Drawing.Color.White;
        this.xrLabel8.LocationFloat = new DevExpress.Utils.PointFloat(194.375F, 70.49999F);
        this.xrLabel8.Multiline = true;
        this.xrLabel8.Name = "xrLabel8";
        this.xrLabel8.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
        this.xrLabel8.SizeF = new System.Drawing.SizeF(405.2085F, 23F);
        this.xrLabel8.StylePriority.UseBackColor = false;
        this.xrLabel8.StylePriority.UseFont = false;
        this.xrLabel8.StylePriority.UseForeColor = false;
        this.xrLabel8.StylePriority.UseTextAlignment = false;
        this.xrLabel8.Text = "Order Intervals";
        this.xrLabel8.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
        // 
        // xrLabel4
        // 
        this.xrLabel4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(3)))), ((int)(((byte)(142)))), ((int)(((byte)(188)))));
        this.xrLabel4.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
        this.xrLabel4.ForeColor = System.Drawing.Color.White;
        this.xrLabel4.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
        this.xrLabel4.Multiline = true;
        this.xrLabel4.Name = "xrLabel4";
        this.xrLabel4.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
        this.xrLabel4.SizeF = new System.Drawing.SizeF(72.83331F, 23F);
        this.xrLabel4.StylePriority.UseBackColor = false;
        this.xrLabel4.StylePriority.UseFont = false;
        this.xrLabel4.StylePriority.UseForeColor = false;
        this.xrLabel4.Text = "المستخدم";
        // 
        // Time
        // 
        this.Time.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(3)))), ((int)(((byte)(142)))), ((int)(((byte)(188)))));
        this.Time.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
        this.Time.ForeColor = System.Drawing.Color.White;
        this.Time.LocationFloat = new DevExpress.Utils.PointFloat(72.83331F, 23.00002F);
        this.Time.Multiline = true;
        this.Time.Name = "Time";
        this.Time.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
        this.Time.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.No;
        this.Time.SizeF = new System.Drawing.SizeF(214.8987F, 23F);
        this.Time.StylePriority.UseBackColor = false;
        this.Time.StylePriority.UseFont = false;
        this.Time.StylePriority.UseForeColor = false;
        this.Time.StylePriority.UseTextAlignment = false;
        this.Time.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter;
        this.Time.BeforePrint += new BeforePrintEventHandler(this.Time_BeforePrint);
        // 
        // xrLabel6
        // 
        this.xrLabel6.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(3)))), ((int)(((byte)(142)))), ((int)(((byte)(188)))));
        this.xrLabel6.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
        this.xrLabel6.ForeColor = System.Drawing.Color.White;
        this.xrLabel6.LocationFloat = new DevExpress.Utils.PointFloat(0F, 23.00002F);
        this.xrLabel6.Multiline = true;
        this.xrLabel6.Name = "xrLabel6";
        this.xrLabel6.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
        this.xrLabel6.SizeF = new System.Drawing.SizeF(72.83331F, 23F);
        this.xrLabel6.StylePriority.UseBackColor = false;
        this.xrLabel6.StylePriority.UseFont = false;
        this.xrLabel6.StylePriority.UseForeColor = false;
        this.xrLabel6.Text = "الوقت";
        // 
        // User
        // 
        this.User.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(3)))), ((int)(((byte)(142)))), ((int)(((byte)(188)))));
        this.User.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
        this.User.ForeColor = System.Drawing.Color.White;
        this.User.LocationFloat = new DevExpress.Utils.PointFloat(72.83331F, 0F);
        this.User.Multiline = true;
        this.User.Name = "User";
        this.User.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
        this.User.SizeF = new System.Drawing.SizeF(214.8987F, 23F);
        this.User.StylePriority.UseBackColor = false;
        this.User.StylePriority.UseFont = false;
        this.User.StylePriority.UseForeColor = false;
        this.User.StylePriority.UseTextAlignment = false;
        this.User.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter;
        // 
        // GroupHeader1
        // 
        this.GroupHeader1.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.table1});
        this.GroupHeader1.GroupUnion = DevExpress.XtraReports.UI.GroupUnion.WithFirstDetail;
        this.GroupHeader1.HeightF = 28F;
        this.GroupHeader1.Name = "GroupHeader1";
        // 
        // table1
        // 
        this.table1.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
        this.table1.Name = "table1";
        this.table1.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.tableRow1});
        this.table1.SizeF = new System.Drawing.SizeF(770F, 28F);
        // 
        // tableRow1
        // 
        this.tableRow1.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.tableCell1,
            this.tableCell2,
            this.tableCell3,
            this.tableCell4,
            this.tableCell5,
            this.tableCell6});
        this.tableRow1.Name = "tableRow1";
        this.tableRow1.Weight = 1D;
        // 
        // tableCell1
        // 
        this.tableCell1.Borders = DevExpress.XtraPrinting.BorderSide.None;
        this.tableCell1.Name = "tableCell1";
        this.tableCell1.StyleName = "DetailCaption1";
        this.tableCell1.StylePriority.UseBorders = false;
        this.tableCell1.Text = "رقم الطلب";
        this.tableCell1.Weight = 0.131467339626187D;
        // 
        // tableCell2
        // 
        this.tableCell2.Name = "tableCell2";
        this.tableCell2.StyleName = "DetailCaption1";
        this.tableCell2.Text = "تاريخ الطلب";
        this.tableCell2.Weight = 0.17481000488173396D;
        // 
        // tableCell3
        // 
        this.tableCell3.Name = "tableCell3";
        this.tableCell3.StyleName = "DetailCaption1";
        this.tableCell3.Text = "تاريخ بدء المهمة";
        this.tableCell3.Weight = 0.20451127330736924D;
        // 
        // tableCell4
        // 
        this.tableCell4.Name = "tableCell4";
        this.tableCell4.StyleName = "DetailCaption1";
        this.tableCell4.Text = "تاريخ الإنتهاء";
        this.tableCell4.Weight = 0.18147323185044217D;
        // 
        // tableCell5
        // 
        this.tableCell5.Name = "tableCell5";
        this.tableCell5.StyleName = "DetailCaption1";
        this.tableCell5.StylePriority.UseTextAlignment = false;
        this.tableCell5.Text = "فترة بدء المهمة";
        this.tableCell5.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
        this.tableCell5.Weight = 0.16364308283408874D;
        // 
        // tableCell6
        // 
        this.tableCell6.Name = "tableCell6";
        this.tableCell6.StyleName = "DetailCaption1";
        this.tableCell6.StylePriority.UseTextAlignment = false;
        this.tableCell6.Text = "فترة التنفيذ";
        this.tableCell6.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
        this.tableCell6.Weight = 0.14409500805035141D;
        // 
        // Detail
        // 
        this.Detail.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.table2});
        this.Detail.HeightF = 25F;
        this.Detail.Name = "Detail";
        // 
        // table2
        // 
        this.table2.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
        this.table2.Name = "table2";
        this.table2.OddStyleName = "DetailData3_Odd";
        this.table2.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.tableRow2});
        this.table2.SizeF = new System.Drawing.SizeF(770F, 25F);
        // 
        // tableRow2
        // 
        this.tableRow2.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.tableCell7,
            this.tableCell8,
            this.tableCell9,
            this.tableCell10,
            this.tableCell11,
            this.tableCell12});
        this.tableRow2.Name = "tableRow2";
        this.tableRow2.Weight = 11.5D;
        // 
        // tableCell7
        // 
        this.tableCell7.Borders = DevExpress.XtraPrinting.BorderSide.None;
        this.tableCell7.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[OrderNumber]")});
        this.tableCell7.Name = "tableCell7";
        this.tableCell7.StyleName = "DetailData1";
        this.tableCell7.StylePriority.UseBorders = false;
        this.tableCell7.Weight = 0.13146734824770193D;
        // 
        // tableCell8
        // 
        this.tableCell8.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[OrderDate]")});
        this.tableCell8.Name = "tableCell8";
        this.tableCell8.StyleName = "DetailData1";
        this.tableCell8.TextFormatString = "{0:yyyy-MM-dd H:mm}";
        this.tableCell8.Weight = 0.17481001929381021D;
        // 
        // tableCell9
        // 
        this.tableCell9.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[TechnicianSelectedDate]")});
        this.tableCell9.Name = "tableCell9";
        this.tableCell9.StyleName = "DetailData1";
        this.tableCell9.TextFormatString = "{0:yyyy-MM-dd H:mm}";
        this.tableCell9.Weight = 0.20451128990699638D;
        // 
        // tableCell10
        // 
        this.tableCell10.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[FinishedDate]")});
        this.tableCell10.Name = "tableCell10";
        this.tableCell10.StyleName = "DetailData1";
        this.tableCell10.TextFormatString = "{0:yyyy-MM-dd H:mm}";
        this.tableCell10.Weight = 0.18147324476125515D;
        // 
        // tableCell11
        // 
        this.tableCell11.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[MissionStartTimeDays]+\':\'+[MissionStartTimeHours]")});
        this.tableCell11.Name = "tableCell11";
        this.tableCell11.StyleName = "DetailData1";
        this.tableCell11.StylePriority.UseTextAlignment = false;
        this.tableCell11.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
        this.tableCell11.Weight = 0.16364309091943877D;
        // 
        // tableCell12
        // 
        this.tableCell12.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[ExecutionTimeDays]+\':\'+[ExecutionTimeHours]")});
        this.tableCell12.Name = "tableCell12";
        this.tableCell12.StyleName = "DetailData1";
        this.tableCell12.StylePriority.UseTextAlignment = false;
        this.tableCell12.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
        this.tableCell12.Weight = 0.14409502668740673D;
        // 
        // DateFrom
        // 
        //this.DateFrom.AllowNull = true;
        this.DateFrom.Description = "التاريخ من";
        this.DateFrom.Name = "DateFrom";
        this.DateFrom.Type = typeof(System.DateTime);
        // 
        // DateTo
        // 
        //this.DateTo.AllowNull = true;
        this.DateTo.Description = "التاريخ إلى";
        this.DateTo.Name = "DateTo";
        this.DateTo.Type = typeof(System.DateTime);
        // 
        // OrderIntervals_Report
        // 
        this.Bands.AddRange(new DevExpress.XtraReports.UI.Band[] {
            this.TopMargin,
            this.BottomMargin,
            this.ReportHeader,
            this.GroupHeader1,
            this.Detail});
        this.ComponentStorage.AddRange(new System.ComponentModel.IComponent[] {
            this.sqlDataSource1});
        this.DataMember = "Order_StatusChangeIntervals";
        this.DataSource = this.sqlDataSource1;
        this.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
        this.Margins = new DevExpress.Drawing.DXMargins(40, 40, 40, 40);
        this.Parameters.AddRange(new DevExpress.XtraReports.Parameters.Parameter[] {
            this.DateFrom,
            this.DateTo});
        this.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
        this.RightToLeftLayout = DevExpress.XtraReports.UI.RightToLeftLayout.Yes;
        this.StyleSheet.AddRange(new DevExpress.XtraReports.UI.XRControlStyle[] {
            this.Title,
            this.DetailCaption1,
            this.DetailData1,
            this.DetailData3_Odd,
            this.PageInfo});
        this.Version = "18.2";
        ((System.ComponentModel.ISupportInitialize)(this.table1)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.table2)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this)).EndInit();

    }

    #endregion

    private void Time_BeforePrint(object sender, CancelEventArgs e)
    {
        DateTime utcNow = DateTime.UtcNow;
        TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
        DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
        ((XRLabel)sender).Text = cTime.ToString("d MMMM yyyy h:mm tt");

    }
}
