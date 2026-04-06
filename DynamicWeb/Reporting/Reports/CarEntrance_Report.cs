using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;
using System.Web;
using System.Security.Claims;
using MyERP;
using MyERP.Models;
using System.Linq;
using DevExpress.Drawing.Printing;
using MyERP.Controllers;

/// <summary>
/// Summary description for SalesInvoice_Get
/// </summary>
public class CarEntrance_Report : DevExpress.XtraReports.UI.XtraReport
{
    private MySoftERPEntity db = new MySoftERPEntity();
    private TopMarginBand TopMargin;
    private BottomMarginBand BottomMargin;
    private DetailBand Detail;
    private DevExpress.DataAccess.Sql.SqlDataSource sqlDataSource1;
    private DetailReportBand DetailReport;
    private DetailBand Detail1;
    private GroupHeaderBand GroupHeader1;
    private CalculatedField ExtendedTotal;
    private DevExpress.XtraReports.Parameters.Parameter DocNum;
    private DevExpress.XtraReports.Parameters.Parameter DepId;
    private DevExpress.XtraReports.Parameters.Parameter DateFrom;
    private DevExpress.XtraReports.Parameters.Parameter DateTo;
    private ReportFooterBand ReportFooter;
    private XRControlStyle xrControlStyle1;
    private DevExpress.XtraReports.Parameters.Parameter CustomerId;
    private DevExpress.XtraReports.Parameters.Parameter Id;
    private DevExpress.XtraReports.Parameters.Parameter UserId;
    private XRLabel xrLabel11;
    private XRPictureBox xrPictureBox1;
    private XRLabel xrLabel40;
    private XRLabel xrLabel41;
    private XRLabel xrLabel39;
    private XRLabel xrLabel38;
    private XRLabel xrLabel42;
    private XRLabel xrLabel7;
    private XRLabel xrLabel44;
    private XRLabel xrLabel45;
    private XRLabel xrLabel57;
    private XRLabel xrLabel58;
    private XRLabel xrLabel55;
    private XRLabel xrLabel56;
    private XRLabel xrLabel53;
    private XRLabel xrLabel51;
    private XRLabel xrLabel49;
    private XRLabel xrLabel2;
    private XRLabel xrLabel48;
    private XRLabel xrLabel46;
    private XRLabel xrLabel47;
    private XRLabel xrLabel76;
    private XRLabel xrLabel77;
    private XRLabel xrLabel74;
    private XRLabel xrLabel75;
    private XRLabel xrLabel70;
    private XRLabel xrLabel36;
    private XRLabel xrLabel72;
    private XRLabel xrLabel12;
    private XRLabel xrLabel68;
    private XRLabel xrLabel69;
    private XRLabel xrLabel66;
    private XRLabel xrLabel31;
    private XRLabel xrLabel64;
    private XRLabel xrLabel13;
    private XRLabel xrLabel60;
    private XRLabel xrLabel10;
    private XRLabel xrLabel62;
    private XRLabel xrLabel28;
    private XRLabel xrLabel59;
    private XRTable xrTable4;
    private XRTableRow xrTableRow4;
    private XRTableCell xrTableCell1;
    private XRTableCell xrTableCell2;
    private XRTableCell xrTableCell4;
    private XRTableCell xrTableCell5;
    private XRTableCell xrTableCell17;
    private XRTableCell xrTableCell6;
    private XRTable xrTable3;
    private XRTableRow xrTableRow3;
    private XRTableCell xrTableCell13;
    private XRTableCell xrTableCell14;
    private XRTableCell xrTableCell15;
    private XRTableCell xrTableCell19;
    private XRTableCell xrTableCell20;
    private XRTableCell xrTableCell21;
    private XRLabel xrLabel84;
    private XRLabel xrLabel20;
    private XRLabel xrLabel83;
    private XRLabel xrLabel17;
    private XRLabel xrLabel18;
    private XRLabel xrLabel80;
    private XRLabel xrLabel21;
    private XRLabel xrLabel78;
    private XRLabel xrLabel89;
    private XRLabel xrLabel88;
    private XRLabel xrLabel87;
    private XRLabel xrLabel86;
    private XRBarCode xrBarCode1;

    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;
    private PageHeaderBand PageHeader;
    private XRPageInfo pageInfo2;
    private XRLabel Time;
    private XRLabel xrLabel6;
    private XRLabel xrLabel4;
    private XRLabel User;
    string domain = "";

    public CarEntrance_Report(string domain = "")
    {
        this.domain = domain;
        InitializeComponent();
        UserId.Value = int.Parse(((ClaimsIdentity)HttpContext.Current.User.Identity).FindFirst("Id").Value);
        User.Text = HttpContext.Current.User.Identity.Name;
        //
        // TODO: Add constructor logic here
        var helper = new HelperController().GetOpenPeriodBeginningEndingForReports();
        this.DateFrom.ValueInfo = helper.start;
        this.DateTo.ValueInfo = helper.end;

    }
    public CarEntrance_Report(string paperKind, bool? printPrice)
    {
        InitializeComponent();
        UserId.Value = int.Parse(((ClaimsIdentity)HttpContext.Current.User.Identity).FindFirst("Id").Value);
        User.Text = HttpContext.Current.User.Identity.Name;
        if (printPrice == false)
        {
            xrTableCell2.WidthF += xrTableCell4.WidthF;
            //xrTableCell8.WidthF += xrTableCell10.WidthF;
            //xrTableCell10.WidthF = 0;
            xrTableCell4.WidthF = 0;
            xrTableCell5.WidthF += xrTableCell6.WidthF;
            //xrTableCell11.WidthF += xrTableCell12.WidthF;
            xrTableCell6.WidthF = 0;
            // xrTableCell12.WidthF = 0;
        }

        if (!string.IsNullOrEmpty(paperKind))
        {
            switch (paperKind)
            {
                case "A4":
                    PaperKind = DXPaperKind.A4;
                    break;
                case "A5":
                    PaperKind = DXPaperKind.A5;
                    break;
                case "Letter":
                    PaperKind = DXPaperKind.Letter;
                    break;
                default:
                    break;
            }
        }
        //
        // TODO: Add constructor logic here
        //
        
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
            DevExpress.DataAccess.Sql.CustomSqlQuery customSqlQuery1 = new DevExpress.DataAccess.Sql.CustomSqlQuery();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CarEntrance_Report));
            DevExpress.DataAccess.Sql.StoredProcQuery storedProcQuery2 = new DevExpress.DataAccess.Sql.StoredProcQuery();
            DevExpress.DataAccess.Sql.QueryParameter queryParameter2 = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.DataAccess.Sql.MasterDetailInfo masterDetailInfo1 = new DevExpress.DataAccess.Sql.MasterDetailInfo();
            DevExpress.DataAccess.Sql.RelationColumnInfo relationColumnInfo1 = new DevExpress.DataAccess.Sql.RelationColumnInfo();
            DevExpress.XtraPrinting.BarCode.QRCodeGenerator qrCodeGenerator1 = new DevExpress.XtraPrinting.BarCode.QRCodeGenerator();
            DevExpress.XtraReports.Parameters.DynamicListLookUpSettings dynamicListLookUpSettings1 = new DevExpress.XtraReports.Parameters.DynamicListLookUpSettings();
            this.sqlDataSource1 = new DevExpress.DataAccess.Sql.SqlDataSource(this.components);
            this.TopMargin = new DevExpress.XtraReports.UI.TopMarginBand();
            this.xrLabel53 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel51 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrBarCode1 = new DevExpress.XtraReports.UI.XRBarCode();
            this.xrPictureBox1 = new DevExpress.XtraReports.UI.XRPictureBox();
            this.BottomMargin = new DevExpress.XtraReports.UI.BottomMarginBand();
            this.Detail = new DevExpress.XtraReports.UI.DetailBand();
            this.xrLabel76 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel77 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel74 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel75 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel70 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel36 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel72 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel12 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel68 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel69 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel66 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel31 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel64 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel13 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel60 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel10 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel62 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel28 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel59 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel57 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel58 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel55 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel56 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel49 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel2 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel48 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel46 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel47 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel44 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel45 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel42 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel7 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel40 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel41 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel39 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel38 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel11 = new DevExpress.XtraReports.UI.XRLabel();
            this.DetailReport = new DevExpress.XtraReports.UI.DetailReportBand();
            this.Detail1 = new DevExpress.XtraReports.UI.DetailBand();
            this.xrTable4 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow4 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell1 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell2 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell4 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell5 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell17 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell6 = new DevExpress.XtraReports.UI.XRTableCell();
            this.GroupHeader1 = new DevExpress.XtraReports.UI.GroupHeaderBand();
            this.xrTable3 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow3 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell13 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell14 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell15 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell19 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell20 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell21 = new DevExpress.XtraReports.UI.XRTableCell();
            this.ExtendedTotal = new DevExpress.XtraReports.UI.CalculatedField();
            this.DocNum = new DevExpress.XtraReports.Parameters.Parameter();
            this.DepId = new DevExpress.XtraReports.Parameters.Parameter();
            this.DateFrom = new DevExpress.XtraReports.Parameters.Parameter();
            this.DateTo = new DevExpress.XtraReports.Parameters.Parameter();
            this.ReportFooter = new DevExpress.XtraReports.UI.ReportFooterBand();
            this.xrLabel89 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel88 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel87 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel86 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel84 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel20 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel83 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel17 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel18 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel80 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel21 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel78 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrControlStyle1 = new DevExpress.XtraReports.UI.XRControlStyle();
            this.CustomerId = new DevExpress.XtraReports.Parameters.Parameter();
            this.Id = new DevExpress.XtraReports.Parameters.Parameter();
            this.UserId = new DevExpress.XtraReports.Parameters.Parameter();
            this.PageHeader = new DevExpress.XtraReports.UI.PageHeaderBand();
            this.pageInfo2 = new DevExpress.XtraReports.UI.XRPageInfo();
            this.Time = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel6 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel4 = new DevExpress.XtraReports.UI.XRLabel();
            this.User = new DevExpress.XtraReports.UI.XRLabel();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable4)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            // 
            // sqlDataSource1
            // 
            this.sqlDataSource1.ConnectionName = "localhost_MySoftERP_Connection";
            this.sqlDataSource1.Name = "sqlDataSource1";
            storedProcQuery1.Name = "CarEntrance_Get";
            queryParameter1.Name = "@Id";
            queryParameter1.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter1.Value = new DevExpress.DataAccess.Expression("?Id", typeof(int));
            storedProcQuery1.Parameters.Add(queryParameter1);
            storedProcQuery1.StoredProcName = "CarEntrance_Get";
            customSqlQuery1.MetaSerializable = "<Meta X=\"225\" Y=\"20\" Width=\"155\" Height=\"236\" />";
            customSqlQuery1.Name = "Query";
            customSqlQuery1.Sql = resources.GetString("customSqlQuery1.Sql");
            storedProcQuery2.MetaSerializable = "<Meta X=\"400\" Y=\"10\" Width=\"108\" Height=\"89\" />";
            storedProcQuery2.Name = "Department_1";
            queryParameter2.Name = "@UserId";
            queryParameter2.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter2.Value = new DevExpress.DataAccess.Expression("?UserId", typeof(int));
            storedProcQuery2.Parameters.Add(queryParameter2);
            storedProcQuery2.StoredProcName = "Department_ReportUserDepartments";
            this.sqlDataSource1.Queries.AddRange(new DevExpress.DataAccess.Sql.SqlQuery[] {
            storedProcQuery1,
            customSqlQuery1,
            storedProcQuery2});
            masterDetailInfo1.DetailQueryName = "Query";
            relationColumnInfo1.NestedKeyColumn = "MainDocId";
            relationColumnInfo1.ParentKeyColumn = "Id";
            masterDetailInfo1.KeyColumns.Add(relationColumnInfo1);
            masterDetailInfo1.MasterQueryName = "CarEntrance_Get";
            this.sqlDataSource1.Relations.AddRange(new DevExpress.DataAccess.Sql.MasterDetailInfo[] {
            masterDetailInfo1});
            this.sqlDataSource1.ResultSchemaSerializable = resources.GetString("sqlDataSource1.ResultSchemaSerializable");
            // 
            // TopMargin
            // 
            this.TopMargin.HeightF = 28.1329F;
            this.TopMargin.Name = "TopMargin";
            // 
            // xrLabel53
            // 
            this.xrLabel53.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel53.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[StreetArName]")});
            this.xrLabel53.Font = new DevExpress.Drawing.DXFont("Arial", 12F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel53.LocationFloat = new DevExpress.Utils.PointFloat(10F, 59.04169F);
            this.xrLabel53.Name = "xrLabel53";
            this.xrLabel53.SizeF = new System.Drawing.SizeF(268.5456F, 35.70849F);
            this.xrLabel53.StylePriority.UseBorders = false;
            this.xrLabel53.StylePriority.UseFont = false;
            this.xrLabel53.Text = "CompanyAddress";
            // 
            // xrLabel51
            // 
            this.xrLabel51.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel51.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CompanyArName]")});
            this.xrLabel51.Font = new DevExpress.Drawing.DXFont("Arial", 18F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel51.ForeColor = System.Drawing.Color.DarkBlue;
            this.xrLabel51.LocationFloat = new DevExpress.Utils.PointFloat(10F, 10.00001F);
            this.xrLabel51.Name = "xrLabel51";
            this.xrLabel51.SizeF = new System.Drawing.SizeF(268.6288F, 49.04168F);
            this.xrLabel51.StylePriority.UseBorders = false;
            this.xrLabel51.StylePriority.UseFont = false;
            this.xrLabel51.StylePriority.UseForeColor = false;
            this.xrLabel51.Text = "CompanyName";
            // 
            // xrBarCode1
            // 
            this.xrBarCode1.Alignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrBarCode1.LocationFloat = new DevExpress.Utils.PointFloat(555.5668F, 0F);
            this.xrBarCode1.Name = "xrBarCode1";
            this.xrBarCode1.Padding = new DevExpress.XtraPrinting.PaddingInfo(10, 10, 0, 0, 100F);
            this.xrBarCode1.ShowText = false;
            this.xrBarCode1.SizeF = new System.Drawing.SizeF(231.3107F, 157.9247F);
            qrCodeGenerator1.CompactionMode = DevExpress.XtraPrinting.BarCode.QRCodeCompactionMode.Byte;
            qrCodeGenerator1.ErrorCorrectionLevel = DevExpress.XtraPrinting.BarCode.QRCodeErrorCorrectionLevel.H;
            qrCodeGenerator1.Version = DevExpress.XtraPrinting.BarCode.QRCodeVersion.Version11;
            this.xrBarCode1.Symbology = qrCodeGenerator1;
            // 
            // xrPictureBox1
            // 
            this.xrPictureBox1.ImageUrl = "http://genoise1.mysoft-eg.com/assets/images/logo.png";
            this.xrPictureBox1.LocationFloat = new DevExpress.Utils.PointFloat(316.2753F, 0F);
            this.xrPictureBox1.Name = "xrPictureBox1";
            this.xrPictureBox1.SizeF = new System.Drawing.SizeF(233.1318F, 147.9247F);
            this.xrPictureBox1.Sizing = DevExpress.XtraPrinting.ImageSizeMode.ZoomImage;
            this.xrPictureBox1.BeforePrint += new BeforePrintEventHandler(this.xrPictureBox1_BeforePrint);
            // 
            // BottomMargin
            // 
            this.BottomMargin.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel4,
            this.User,
            this.pageInfo2,
            this.Time,
            this.xrLabel6});
            this.BottomMargin.HeightF = 68.42524F;
            this.BottomMargin.Name = "BottomMargin";
            // 
            // Detail
            // 
            this.Detail.HeightF = 10.19227F;
            this.Detail.Name = "Detail";
            this.Detail.StylePriority.UseTextAlignment = false;
            this.Detail.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel76
            // 
            this.xrLabel76.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel76.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel76.LocationFloat = new DevExpress.Utils.PointFloat(413.824F, 453.4248F);
            this.xrLabel76.Name = "xrLabel76";
            this.xrLabel76.SizeF = new System.Drawing.SizeF(135.8333F, 23F);
            this.xrLabel76.StylePriority.UseBorders = false;
            this.xrLabel76.StylePriority.UseFont = false;
            this.xrLabel76.StylePriority.UseTextAlignment = false;
            this.xrLabel76.Text = "عداد السيارة عند الخروج:";
            this.xrLabel76.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel77
            // 
            this.xrLabel77.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel77.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CarOutOdometer]")});
            this.xrLabel77.LocationFloat = new DevExpress.Utils.PointFloat(413.8238F, 476.4248F);
            this.xrLabel77.Name = "xrLabel77";
            this.xrLabel77.SizeF = new System.Drawing.SizeF(135.5F, 23F);
            this.xrLabel77.StylePriority.UseBorders = false;
            this.xrLabel77.StylePriority.UseTextAlignment = false;
            this.xrLabel77.Text = "عداد السيارة عند الخروج";
            this.xrLabel77.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel74
            // 
            this.xrLabel74.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel74.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel74.LocationFloat = new DevExpress.Utils.PointFloat(231.1156F, 453.4248F);
            this.xrLabel74.Name = "xrLabel74";
            this.xrLabel74.SizeF = new System.Drawing.SizeF(135.8333F, 23F);
            this.xrLabel74.StylePriority.UseBorders = false;
            this.xrLabel74.StylePriority.UseFont = false;
            this.xrLabel74.StylePriority.UseTextAlignment = false;
            this.xrLabel74.Text = "عداد السيارة عند الدخول :";
            this.xrLabel74.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel75
            // 
            this.xrLabel75.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel75.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CarInOdometer]")});
            this.xrLabel75.LocationFloat = new DevExpress.Utils.PointFloat(231.1154F, 476.4248F);
            this.xrLabel75.Name = "xrLabel75";
            this.xrLabel75.SizeF = new System.Drawing.SizeF(135.5F, 23F);
            this.xrLabel75.StylePriority.UseBorders = false;
            this.xrLabel75.StylePriority.UseTextAlignment = false;
            this.xrLabel75.Text = "عداد السيارة عند الدخول";
            this.xrLabel75.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel70
            // 
            this.xrLabel70.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel70.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel70.LocationFloat = new DevExpress.Utils.PointFloat(85.37286F, 453.4248F);
            this.xrLabel70.Name = "xrLabel70";
            this.xrLabel70.SizeF = new System.Drawing.SizeF(110.8335F, 23F);
            this.xrLabel70.StylePriority.UseBorders = false;
            this.xrLabel70.StylePriority.UseFont = false;
            this.xrLabel70.StylePriority.UseTextAlignment = false;
            this.xrLabel70.Text = "تاريخ دخول السيارة :";
            this.xrLabel70.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel36
            // 
            this.xrLabel36.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel36.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[VoucherDate]")});
            this.xrLabel36.LocationFloat = new DevExpress.Utils.PointFloat(85.37292F, 476.4249F);
            this.xrLabel36.Name = "xrLabel36";
            this.xrLabel36.SizeF = new System.Drawing.SizeF(110.5002F, 23F);
            this.xrLabel36.StylePriority.UseBorders = false;
            this.xrLabel36.StylePriority.UseTextAlignment = false;
            this.xrLabel36.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabel36.TextFormatString = "{0:yyyy-MM-dd}";
            // 
            // xrLabel72
            // 
            this.xrLabel72.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel72.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel72.LocationFloat = new DevExpress.Utils.PointFloat(596.6984F, 453.4248F);
            this.xrLabel72.Name = "xrLabel72";
            this.xrLabel72.SizeF = new System.Drawing.SizeF(111.1669F, 23F);
            this.xrLabel72.StylePriority.UseBorders = false;
            this.xrLabel72.StylePriority.UseFont = false;
            this.xrLabel72.StylePriority.UseTextAlignment = false;
            this.xrLabel72.Text = "تاريخ خروج السيارة :";
            this.xrLabel72.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel12
            // 
            this.xrLabel12.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel12.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[OutDate]")});
            this.xrLabel12.LocationFloat = new DevExpress.Utils.PointFloat(596.0317F, 476.4248F);
            this.xrLabel12.Name = "xrLabel12";
            this.xrLabel12.SizeF = new System.Drawing.SizeF(111.8336F, 23F);
            this.xrLabel12.StylePriority.UseBorders = false;
            this.xrLabel12.StylePriority.UseTextAlignment = false;
            this.xrLabel12.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabel12.TextFormatString = "{0:yyyy-MM-dd}";
            // 
            // xrLabel68
            // 
            this.xrLabel68.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel68.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel68.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.xrLabel68.LocationFloat = new DevExpress.Utils.PointFloat(514.2252F, 411.6331F);
            this.xrLabel68.Name = "xrLabel68";
            this.xrLabel68.SizeF = new System.Drawing.SizeF(99.33322F, 23F);
            this.xrLabel68.StylePriority.UseBorders = false;
            this.xrLabel68.StylePriority.UseFont = false;
            this.xrLabel68.StylePriority.UseForeColor = false;
            this.xrLabel68.Text = "رقم الشاسيه:";
            // 
            // xrLabel69
            // 
            this.xrLabel69.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel69.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CarSerialNumber]")});
            this.xrLabel69.LocationFloat = new DevExpress.Utils.PointFloat(613.7249F, 411.6331F);
            this.xrLabel69.Name = "xrLabel69";
            this.xrLabel69.SizeF = new System.Drawing.SizeF(146.4166F, 23.00001F);
            this.xrLabel69.StylePriority.UseBorders = false;
            this.xrLabel69.Text = "رقم الشاسيه";
            // 
            // xrLabel66
            // 
            this.xrLabel66.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel66.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel66.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.xrLabel66.LocationFloat = new DevExpress.Utils.PointFloat(514.7251F, 388.633F);
            this.xrLabel66.Name = "xrLabel66";
            this.xrLabel66.SizeF = new System.Drawing.SizeF(99.33337F, 23F);
            this.xrLabel66.StylePriority.UseBorders = false;
            this.xrLabel66.StylePriority.UseFont = false;
            this.xrLabel66.StylePriority.UseForeColor = false;
            this.xrLabel66.Text = "رقم اللوحة :";
            // 
            // xrLabel31
            // 
            this.xrLabel31.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel31.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CarPlateNumber]")});
            this.xrLabel31.LocationFloat = new DevExpress.Utils.PointFloat(614.3919F, 388.633F);
            this.xrLabel31.Name = "xrLabel31";
            this.xrLabel31.SizeF = new System.Drawing.SizeF(145.4166F, 23F);
            this.xrLabel31.StylePriority.UseBorders = false;
            // 
            // xrLabel64
            // 
            this.xrLabel64.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel64.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel64.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.xrLabel64.LocationFloat = new DevExpress.Utils.PointFloat(514.7249F, 365.633F);
            this.xrLabel64.Name = "xrLabel64";
            this.xrLabel64.SizeF = new System.Drawing.SizeF(100F, 23F);
            this.xrLabel64.StylePriority.UseBorders = false;
            this.xrLabel64.StylePriority.UseFont = false;
            this.xrLabel64.StylePriority.UseForeColor = false;
            this.xrLabel64.Text = "موديل السيارة :";
            // 
            // xrLabel13
            // 
            this.xrLabel13.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel13.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CarModelArName]")});
            this.xrLabel13.LocationFloat = new DevExpress.Utils.PointFloat(615.0584F, 365.633F);
            this.xrLabel13.Name = "xrLabel13";
            this.xrLabel13.SizeF = new System.Drawing.SizeF(144.75F, 23F);
            this.xrLabel13.StylePriority.UseBorders = false;
            // 
            // xrLabel60
            // 
            this.xrLabel60.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel60.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel60.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.xrLabel60.LocationFloat = new DevExpress.Utils.PointFloat(515.0584F, 319.6331F);
            this.xrLabel60.Name = "xrLabel60";
            this.xrLabel60.SizeF = new System.Drawing.SizeF(100F, 23F);
            this.xrLabel60.StylePriority.UseBorders = false;
            this.xrLabel60.StylePriority.UseFont = false;
            this.xrLabel60.StylePriority.UseForeColor = false;
            this.xrLabel60.Text = "نوع السيارة :";
            // 
            // xrLabel10
            // 
            this.xrLabel10.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel10.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CarTypeArName]")});
            this.xrLabel10.LocationFloat = new DevExpress.Utils.PointFloat(615.3917F, 319.6331F);
            this.xrLabel10.Name = "xrLabel10";
            this.xrLabel10.SizeF = new System.Drawing.SizeF(144.4166F, 22.99998F);
            this.xrLabel10.StylePriority.UseBorders = false;
            // 
            // xrLabel62
            // 
            this.xrLabel62.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel62.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel62.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.xrLabel62.LocationFloat = new DevExpress.Utils.PointFloat(515.3916F, 342.6331F);
            this.xrLabel62.Name = "xrLabel62";
            this.xrLabel62.SizeF = new System.Drawing.SizeF(100F, 23F);
            this.xrLabel62.StylePriority.UseBorders = false;
            this.xrLabel62.StylePriority.UseFont = false;
            this.xrLabel62.StylePriority.UseForeColor = false;
            this.xrLabel62.Text = "لون السيارة :";
            // 
            // xrLabel28
            // 
            this.xrLabel28.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel28.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CarColorArName]")});
            this.xrLabel28.LocationFloat = new DevExpress.Utils.PointFloat(615.7249F, 342.6331F);
            this.xrLabel28.Name = "xrLabel28";
            this.xrLabel28.SizeF = new System.Drawing.SizeF(143.7499F, 23.00002F);
            this.xrLabel28.StylePriority.UseBorders = false;
            // 
            // xrLabel59
            // 
            this.xrLabel59.BorderColor = System.Drawing.Color.Silver;
            this.xrLabel59.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel59.Font = new DevExpress.Drawing.DXFont("Arial", 12F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel59.ForeColor = System.Drawing.Color.DarkBlue;
            this.xrLabel59.LocationFloat = new DevExpress.Utils.PointFloat(514.5584F, 284.2997F);
            this.xrLabel59.Multiline = true;
            this.xrLabel59.Name = "xrLabel59";
            this.xrLabel59.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel59.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel59.SizeF = new System.Drawing.SizeF(117.7502F, 34.37502F);
            this.xrLabel59.StylePriority.UseBorderColor = false;
            this.xrLabel59.StylePriority.UseBorders = false;
            this.xrLabel59.StylePriority.UseFont = false;
            this.xrLabel59.StylePriority.UseForeColor = false;
            this.xrLabel59.StylePriority.UseTextAlignment = false;
            this.xrLabel59.Text = "معلومات السيارة";
            this.xrLabel59.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel57
            // 
            this.xrLabel57.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel57.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CustomerMobile]")});
            this.xrLabel57.LocationFloat = new DevExpress.Utils.PointFloat(100.75F, 410.6746F);
            this.xrLabel57.Name = "xrLabel57";
            this.xrLabel57.SizeF = new System.Drawing.SizeF(247.375F, 23.00003F);
            this.xrLabel57.StylePriority.UseBorders = false;
            this.xrLabel57.Text = "PhoneNumbe";
            // 
            // xrLabel58
            // 
            this.xrLabel58.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel58.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel58.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.xrLabel58.LocationFloat = new DevExpress.Utils.PointFloat(0.5835571F, 410.6747F);
            this.xrLabel58.Name = "xrLabel58";
            this.xrLabel58.SizeF = new System.Drawing.SizeF(99.99991F, 23F);
            this.xrLabel58.StylePriority.UseBorders = false;
            this.xrLabel58.StylePriority.UseFont = false;
            this.xrLabel58.StylePriority.UseForeColor = false;
            this.xrLabel58.Text = "رقم الهاتف :";
            // 
            // xrLabel55
            // 
            this.xrLabel55.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel55.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CustomersGroupArName]")});
            this.xrLabel55.LocationFloat = new DevExpress.Utils.PointFloat(100.7501F, 387.6746F);
            this.xrLabel55.Name = "xrLabel55";
            this.xrLabel55.SizeF = new System.Drawing.SizeF(247.3749F, 23.00002F);
            this.xrLabel55.StylePriority.UseBorders = false;
            this.xrLabel55.Text = "CustomerType";
            // 
            // xrLabel56
            // 
            this.xrLabel56.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel56.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel56.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.xrLabel56.LocationFloat = new DevExpress.Utils.PointFloat(0.9168701F, 387.6746F);
            this.xrLabel56.Name = "xrLabel56";
            this.xrLabel56.SizeF = new System.Drawing.SizeF(99.6666F, 23F);
            this.xrLabel56.StylePriority.UseBorders = false;
            this.xrLabel56.StylePriority.UseFont = false;
            this.xrLabel56.StylePriority.UseForeColor = false;
            this.xrLabel56.Text = "نوع العميل :";
            // 
            // xrLabel49
            // 
            this.xrLabel49.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel49.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel49.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.xrLabel49.LocationFloat = new DevExpress.Utils.PointFloat(1.000183F, 364.6746F);
            this.xrLabel49.Name = "xrLabel49";
            this.xrLabel49.SizeF = new System.Drawing.SizeF(99.6666F, 23F);
            this.xrLabel49.StylePriority.UseBorders = false;
            this.xrLabel49.StylePriority.UseFont = false;
            this.xrLabel49.StylePriority.UseForeColor = false;
            this.xrLabel49.Text = "اسم العميل :";
            // 
            // xrLabel2
            // 
            this.xrLabel2.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel2.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CustomerArName]")});
            this.xrLabel2.LocationFloat = new DevExpress.Utils.PointFloat(101.1668F, 364.6746F);
            this.xrLabel2.Name = "xrLabel2";
            this.xrLabel2.SizeF = new System.Drawing.SizeF(247.3749F, 23.00002F);
            this.xrLabel2.StylePriority.UseBorders = false;
            // 
            // xrLabel48
            // 
            this.xrLabel48.BorderColor = System.Drawing.Color.Silver;
            this.xrLabel48.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel48.Font = new DevExpress.Drawing.DXFont("Arial", 12F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel48.ForeColor = System.Drawing.Color.DarkBlue;
            this.xrLabel48.LocationFloat = new DevExpress.Utils.PointFloat(0.2501831F, 330.2996F);
            this.xrLabel48.Multiline = true;
            this.xrLabel48.Name = "xrLabel48";
            this.xrLabel48.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel48.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel48.SizeF = new System.Drawing.SizeF(117.7502F, 34.37502F);
            this.xrLabel48.StylePriority.UseBorderColor = false;
            this.xrLabel48.StylePriority.UseBorders = false;
            this.xrLabel48.StylePriority.UseFont = false;
            this.xrLabel48.StylePriority.UseForeColor = false;
            this.xrLabel48.StylePriority.UseTextAlignment = false;
            this.xrLabel48.Text = "معلومات العميل";
            this.xrLabel48.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel46
            // 
            this.xrLabel46.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel46.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel46.ForeColor = System.Drawing.Color.DarkBlue;
            this.xrLabel46.LocationFloat = new DevExpress.Utils.PointFloat(514.8919F, 261.2997F);
            this.xrLabel46.Name = "xrLabel46";
            this.xrLabel46.SizeF = new System.Drawing.SizeF(100F, 23F);
            this.xrLabel46.StylePriority.UseBorders = false;
            this.xrLabel46.StylePriority.UseFont = false;
            this.xrLabel46.StylePriority.UseForeColor = false;
            this.xrLabel46.Text = "كود العميل :";
            // 
            // xrLabel47
            // 
            this.xrLabel47.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel47.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CustomerCode]")});
            this.xrLabel47.LocationFloat = new DevExpress.Utils.PointFloat(615.2252F, 261.2997F);
            this.xrLabel47.Name = "xrLabel47";
            this.xrLabel47.SizeF = new System.Drawing.SizeF(162.6083F, 23.00001F);
            this.xrLabel47.StylePriority.UseBorders = false;
            this.xrLabel47.Text = "CustomerCode";
            // 
            // xrLabel44
            // 
            this.xrLabel44.BorderColor = System.Drawing.Color.Black;
            this.xrLabel44.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel44.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel44.ForeColor = System.Drawing.Color.DarkBlue;
            this.xrLabel44.LocationFloat = new DevExpress.Utils.PointFloat(515.2252F, 215.2997F);
            this.xrLabel44.Name = "xrLabel44";
            this.xrLabel44.SizeF = new System.Drawing.SizeF(100F, 23F);
            this.xrLabel44.StylePriority.UseBorderColor = false;
            this.xrLabel44.StylePriority.UseBorders = false;
            this.xrLabel44.StylePriority.UseFont = false;
            this.xrLabel44.StylePriority.UseForeColor = false;
            this.xrLabel44.Text = "التاريخ :";
            // 
            // xrLabel45
            // 
            this.xrLabel45.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel45.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[VoucherDate]")});
            this.xrLabel45.LocationFloat = new DevExpress.Utils.PointFloat(615.5585F, 215.2997F);
            this.xrLabel45.Name = "xrLabel45";
            this.xrLabel45.SizeF = new System.Drawing.SizeF(162.6083F, 23.00001F);
            this.xrLabel45.StylePriority.UseBorders = false;
            this.xrLabel45.Text = "Date";
            // 
            // xrLabel42
            // 
            this.xrLabel42.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel42.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel42.ForeColor = System.Drawing.Color.DarkBlue;
            this.xrLabel42.LocationFloat = new DevExpress.Utils.PointFloat(515.5585F, 238.2997F);
            this.xrLabel42.Name = "xrLabel42";
            this.xrLabel42.SizeF = new System.Drawing.SizeF(100F, 23F);
            this.xrLabel42.StylePriority.UseBorders = false;
            this.xrLabel42.StylePriority.UseFont = false;
            this.xrLabel42.StylePriority.UseForeColor = false;
            this.xrLabel42.Text = "رقم الفاتورة :";
            // 
            // xrLabel7
            // 
            this.xrLabel7.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel7.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[DocumentNumber]")});
            this.xrLabel7.LocationFloat = new DevExpress.Utils.PointFloat(615.8919F, 238.2997F);
            this.xrLabel7.Name = "xrLabel7";
            this.xrLabel7.SizeF = new System.Drawing.SizeF(162.6083F, 23.00001F);
            this.xrLabel7.StylePriority.UseBorders = false;
            // 
            // xrLabel40
            // 
            this.xrLabel40.Font = new DevExpress.Drawing.DXFont("Arial", 12F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel40.ForeColor = System.Drawing.Color.DarkBlue;
            this.xrLabel40.LocationFloat = new DevExpress.Utils.PointFloat(0.2501831F, 261.2997F);
            this.xrLabel40.Multiline = true;
            this.xrLabel40.Name = "xrLabel40";
            this.xrLabel40.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel40.SizeF = new System.Drawing.SizeF(136.1667F, 23F);
            this.xrLabel40.StylePriority.UseFont = false;
            this.xrLabel40.StylePriority.UseForeColor = false;
            this.xrLabel40.Text = "السجل التجارى";
            // 
            // xrLabel41
            // 
            this.xrLabel41.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CommercialRegistrationNo]")});
            this.xrLabel41.LocationFloat = new DevExpress.Utils.PointFloat(0.2501831F, 284.2997F);
            this.xrLabel41.Multiline = true;
            this.xrLabel41.Name = "xrLabel41";
            this.xrLabel41.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel41.SizeF = new System.Drawing.SizeF(247.2083F, 22.99999F);
            this.xrLabel41.Text = "1010656264";
            // 
            // xrLabel39
            // 
            this.xrLabel39.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[TaxCardNumber]")});
            this.xrLabel39.LocationFloat = new DevExpress.Utils.PointFloat(0.5002441F, 238.2997F);
            this.xrLabel39.Multiline = true;
            this.xrLabel39.Name = "xrLabel39";
            this.xrLabel39.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel39.SizeF = new System.Drawing.SizeF(246.4582F, 23F);
            this.xrLabel39.Text = "310730519400003";
            // 
            // xrLabel38
            // 
            this.xrLabel38.Font = new DevExpress.Drawing.DXFont("Arial", 12F, DevExpress.Drawing.DXFontStyle.Bold, DevExpress.Drawing.DXGraphicsUnit.Point, new DevExpress.Drawing.DXFontAdditionalProperty[] {new DevExpress.Drawing.DXFontAdditionalProperty("GdiCharSet", ((byte)(0)))});
            this.xrLabel38.ForeColor = System.Drawing.Color.DarkBlue;
            this.xrLabel38.LocationFloat = new DevExpress.Utils.PointFloat(0.2501831F, 215.2999F);
            this.xrLabel38.Multiline = true;
            this.xrLabel38.Name = "xrLabel38";
            this.xrLabel38.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel38.SizeF = new System.Drawing.SizeF(136.8333F, 23F);
            this.xrLabel38.StylePriority.UseFont = false;
            this.xrLabel38.StylePriority.UseForeColor = false;
            this.xrLabel38.Text = "الرقم الضريبى";
            // 
            // xrLabel11
            // 
            this.xrLabel11.BorderColor = System.Drawing.Color.Silver;
            this.xrLabel11.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel11.Font = new DevExpress.Drawing.DXFont("Arial", 18F, DevExpress.Drawing.DXFontStyle.Bold, DevExpress.Drawing.DXGraphicsUnit.Point, new DevExpress.Drawing.DXFontAdditionalProperty[] {new DevExpress.Drawing.DXFontAdditionalProperty("GdiCharSet", ((byte)(0)))});
            this.xrLabel11.ForeColor = System.Drawing.Color.DarkBlue;
            this.xrLabel11.LocationFloat = new DevExpress.Utils.PointFloat(284.2819F, 157.9247F);
            this.xrLabel11.Multiline = true;
            this.xrLabel11.Name = "xrLabel11";
            this.xrLabel11.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel11.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel11.SizeF = new System.Drawing.SizeF(265.0419F, 34.37502F);
            this.xrLabel11.StylePriority.UseBorderColor = false;
            this.xrLabel11.StylePriority.UseBorders = false;
            this.xrLabel11.StylePriority.UseFont = false;
            this.xrLabel11.StylePriority.UseForeColor = false;
            this.xrLabel11.StylePriority.UseTextAlignment = false;
            this.xrLabel11.Text = "فاتورة ضريبية";
            this.xrLabel11.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter;
            // 
            // DetailReport
            // 
            this.DetailReport.Bands.AddRange(new DevExpress.XtraReports.UI.Band[] {
            this.Detail1,
            this.GroupHeader1});
            this.DetailReport.DataMember = "CarEntrance_Get.CarEntrance_GetQuery";
            this.DetailReport.DataSource = this.sqlDataSource1;
            this.DetailReport.Level = 0;
            this.DetailReport.Name = "DetailReport";
            this.DetailReport.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // Detail1
            // 
            this.Detail1.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrTable4});
            this.Detail1.HeightF = 25F;
            this.Detail1.Name = "Detail1";
            // 
            // xrTable4
            // 
            this.xrTable4.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable4.EvenStyleName = "xrControlStyle1";
            this.xrTable4.LocationFloat = new DevExpress.Utils.PointFloat(0.3333836F, 0F);
            this.xrTable4.Name = "xrTable4";
            this.xrTable4.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow4});
            this.xrTable4.SizeF = new System.Drawing.SizeF(787.5833F, 25F);
            this.xrTable4.StylePriority.UseBorders = false;
            // 
            // xrTableRow4
            // 
            this.xrTableRow4.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell1,
            this.xrTableCell2,
            this.xrTableCell4,
            this.xrTableCell5,
            this.xrTableCell17,
            this.xrTableCell6});
            this.xrTableRow4.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrTableRow4.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.xrTableRow4.Name = "xrTableRow4";
            this.xrTableRow4.StylePriority.UseFont = false;
            this.xrTableRow4.StylePriority.UseForeColor = false;
            this.xrTableRow4.Weight = 11.5D;
            // 
            // xrTableCell1
            // 
            this.xrTableCell1.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CarServiceCode]")});
            this.xrTableCell1.Multiline = true;
            this.xrTableCell1.Name = "xrTableCell1";
            this.xrTableCell1.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTableCell1.Text = "xrTableCell1";
            this.xrTableCell1.Weight = 0.16156941936081012D;
            // 
            // xrTableCell2
            // 
            this.xrTableCell2.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CarServiceName]")});
            this.xrTableCell2.Multiline = true;
            this.xrTableCell2.Name = "xrTableCell2";
            this.xrTableCell2.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTableCell2.Text = "xrTableCell2";
            this.xrTableCell2.Weight = 0.36873342289397437D;
            // 
            // xrTableCell4
            // 
            this.xrTableCell4.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Price]")});
            this.xrTableCell4.Multiline = true;
            this.xrTableCell4.Name = "xrTableCell4";
            this.xrTableCell4.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTableCell4.Text = "xrTableCell4";
            this.xrTableCell4.TextFormatString = "{0:0.00}";
            this.xrTableCell4.Weight = 0.16797952571275174D;
            // 
            // xrTableCell5
            // 
            this.xrTableCell5.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[ServiceDiscountPercentage]")});
            this.xrTableCell5.Multiline = true;
            this.xrTableCell5.Name = "xrTableCell5";
            this.xrTableCell5.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTableCell5.Text = "xrTableCell5";
            this.xrTableCell5.Weight = 0.17062479825454957D;
            // 
            // xrTableCell17
            // 
            this.xrTableCell17.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[ServiceDiscountValue]")});
            this.xrTableCell17.Multiline = true;
            this.xrTableCell17.Name = "xrTableCell17";
            this.xrTableCell17.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrTableCell17.Text = "xrTableCell17";
            this.xrTableCell17.Weight = 0.29713270856460006D;
            // 
            // xrTableCell6
            // 
            this.xrTableCell6.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[ServiceNetTotal]")});
            this.xrTableCell6.Multiline = true;
            this.xrTableCell6.Name = "xrTableCell6";
            this.xrTableCell6.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTableCell6.Text = "xrTableCell6";
            this.xrTableCell6.TextFormatString = "{0:0.00}";
            this.xrTableCell6.Weight = 0.33473134525600279D;
            // 
            // GroupHeader1
            // 
            this.GroupHeader1.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrTable3});
            this.GroupHeader1.HeightF = 27.83318F;
            this.GroupHeader1.Name = "GroupHeader1";
            this.GroupHeader1.RepeatEveryPage = true;
            // 
            // xrTable3
            // 
            this.xrTable3.BorderColor = System.Drawing.Color.DimGray;
            this.xrTable3.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable3.ForeColor = System.Drawing.Color.DimGray;
            this.xrTable3.LocationFloat = new DevExpress.Utils.PointFloat(9.536743E-06F, 0F);
            this.xrTable3.Name = "xrTable3";
            this.xrTable3.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow3});
            this.xrTable3.SizeF = new System.Drawing.SizeF(787.9167F, 25F);
            this.xrTable3.StylePriority.UseBorderColor = false;
            this.xrTable3.StylePriority.UseBorders = false;
            this.xrTable3.StylePriority.UseForeColor = false;
            // 
            // xrTableRow3
            // 
            this.xrTableRow3.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell13,
            this.xrTableCell14,
            this.xrTableCell15,
            this.xrTableCell19,
            this.xrTableCell20,
            this.xrTableCell21});
            this.xrTableRow3.Name = "xrTableRow3";
            this.xrTableRow3.Weight = 11.5D;
            // 
            // xrTableCell13
            // 
            this.xrTableCell13.BackColor = System.Drawing.Color.DarkBlue;
            this.xrTableCell13.BorderColor = System.Drawing.Color.DimGray;
            this.xrTableCell13.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell13.ForeColor = System.Drawing.Color.White;
            this.xrTableCell13.Multiline = true;
            this.xrTableCell13.Name = "xrTableCell13";
            this.xrTableCell13.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTableCell13.StylePriority.UseBackColor = false;
            this.xrTableCell13.StylePriority.UseBorderColor = false;
            this.xrTableCell13.StylePriority.UseFont = false;
            this.xrTableCell13.StylePriority.UseForeColor = false;
            this.xrTableCell13.Text = "رقم الخدمة";
            this.xrTableCell13.Weight = 0.18656482987937717D;
            // 
            // xrTableCell14
            // 
            this.xrTableCell14.BackColor = System.Drawing.Color.DarkBlue;
            this.xrTableCell14.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell14.ForeColor = System.Drawing.Color.White;
            this.xrTableCell14.Multiline = true;
            this.xrTableCell14.Name = "xrTableCell14";
            this.xrTableCell14.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTableCell14.StylePriority.UseBackColor = false;
            this.xrTableCell14.StylePriority.UseFont = false;
            this.xrTableCell14.StylePriority.UseForeColor = false;
            this.xrTableCell14.StylePriority.UseTextAlignment = false;
            this.xrTableCell14.Text = "وصف الخدمة";
            this.xrTableCell14.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrTableCell14.Weight = 0.41758057381715696D;
            // 
            // xrTableCell15
            // 
            this.xrTableCell15.BackColor = System.Drawing.Color.DarkBlue;
            this.xrTableCell15.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell15.ForeColor = System.Drawing.Color.White;
            this.xrTableCell15.Multiline = true;
            this.xrTableCell15.Name = "xrTableCell15";
            this.xrTableCell15.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrTableCell15.StylePriority.UseBackColor = false;
            this.xrTableCell15.StylePriority.UseFont = false;
            this.xrTableCell15.StylePriority.UseForeColor = false;
            this.xrTableCell15.Text = "سعرالخدمة";
            this.xrTableCell15.Weight = 0.19070812248288943D;
            // 
            // xrTableCell19
            // 
            this.xrTableCell19.BackColor = System.Drawing.Color.DarkBlue;
            this.xrTableCell19.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell19.ForeColor = System.Drawing.Color.White;
            this.xrTableCell19.Multiline = true;
            this.xrTableCell19.Name = "xrTableCell19";
            this.xrTableCell19.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTableCell19.StylePriority.UseBackColor = false;
            this.xrTableCell19.StylePriority.UseFont = false;
            this.xrTableCell19.StylePriority.UseForeColor = false;
            this.xrTableCell19.Text = "نسبة الخصم";
            this.xrTableCell19.Weight = 0.19266675920308915D;
            // 
            // xrTableCell20
            // 
            this.xrTableCell20.BackColor = System.Drawing.Color.DarkBlue;
            this.xrTableCell20.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell20.ForeColor = System.Drawing.Color.White;
            this.xrTableCell20.Multiline = true;
            this.xrTableCell20.Name = "xrTableCell20";
            this.xrTableCell20.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrTableCell20.StylePriority.UseBackColor = false;
            this.xrTableCell20.StylePriority.UseFont = false;
            this.xrTableCell20.StylePriority.UseForeColor = false;
            this.xrTableCell20.Text = "قيمة الخصم";
            this.xrTableCell20.Weight = 0.33629186822662371D;
            // 
            // xrTableCell21
            // 
            this.xrTableCell21.BackColor = System.Drawing.Color.DarkBlue;
            this.xrTableCell21.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell21.ForeColor = System.Drawing.Color.White;
            this.xrTableCell21.Multiline = true;
            this.xrTableCell21.Name = "xrTableCell21";
            this.xrTableCell21.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTableCell21.StylePriority.UseBackColor = false;
            this.xrTableCell21.StylePriority.UseFont = false;
            this.xrTableCell21.StylePriority.UseForeColor = false;
            this.xrTableCell21.Text = "الإجمالى";
            this.xrTableCell21.Weight = 0.37688841743196755D;
            // 
            // ExtendedTotal
            // 
            this.ExtendedTotal.DataMember = "SalesInvoice_Get.SalesInvoice_GetQuery";
            this.ExtendedTotal.Expression = "[Price] * [Qty]";
            this.ExtendedTotal.Name = "ExtendedTotal";
            // 
            // DocNum
            // 
            this.DocNum.AllowNull = true;
            this.DocNum.Description = "رقم المستند";
            this.DocNum.Name = "DocNum";
            // 
            // DepId
            // 
            this.DepId.AllowNull = true;
            this.DepId.Description = "الفرع";
            this.DepId.Name = "DepId";
            this.DepId.Type = typeof(int);
            dynamicListLookUpSettings1.DataMember = "Department_1";
            dynamicListLookUpSettings1.DataSource = this.sqlDataSource1;
            dynamicListLookUpSettings1.DisplayMember = "ArName";
            dynamicListLookUpSettings1.ValueMember = "Id";
            this.DepId.ValueSourceSettings = dynamicListLookUpSettings1;
            // 
            // DateFrom
            // 
            this.DateFrom.Description = "التاريخ من";
            this.DateFrom.Name = "DateFrom";
            this.DateFrom.Type = typeof(System.DateTime);
            // 
            // DateTo
            // 
            this.DateTo.Description = "التاريخ إلى";
            this.DateTo.Name = "DateTo";
            this.DateTo.Type = typeof(System.DateTime);
            // 
            // ReportFooter
            // 
            this.ReportFooter.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel89,
            this.xrLabel88,
            this.xrLabel87,
            this.xrLabel86,
            this.xrLabel84,
            this.xrLabel20,
            this.xrLabel83,
            this.xrLabel17,
            this.xrLabel18,
            this.xrLabel80,
            this.xrLabel21,
            this.xrLabel78});
            this.ReportFooter.HeightF = 221.6257F;
            this.ReportFooter.Name = "ReportFooter";
            this.ReportFooter.StylePriority.UseTextAlignment = false;
            this.ReportFooter.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel89
            // 
            this.xrLabel89.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel89.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel89.ForeColor = System.Drawing.Color.DarkBlue;
            this.xrLabel89.LocationFloat = new DevExpress.Utils.PointFloat(555.5668F, 188.6257F);
            this.xrLabel89.Multiline = true;
            this.xrLabel89.Name = "xrLabel89";
            this.xrLabel89.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel89.SizeF = new System.Drawing.SizeF(103.8275F, 23F);
            this.xrLabel89.StylePriority.UseBorders = false;
            this.xrLabel89.StylePriority.UseFont = false;
            this.xrLabel89.StylePriority.UseForeColor = false;
            this.xrLabel89.StylePriority.UseTextAlignment = false;
            this.xrLabel89.Text = "ختم المركز";
            this.xrLabel89.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel88
            // 
            this.xrLabel88.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel88.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel88.ForeColor = System.Drawing.Color.DarkBlue;
            this.xrLabel88.LocationFloat = new DevExpress.Utils.PointFloat(378.3413F, 188.6257F);
            this.xrLabel88.Multiline = true;
            this.xrLabel88.Name = "xrLabel88";
            this.xrLabel88.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel88.SizeF = new System.Drawing.SizeF(103.8275F, 23F);
            this.xrLabel88.StylePriority.UseBorders = false;
            this.xrLabel88.StylePriority.UseFont = false;
            this.xrLabel88.StylePriority.UseForeColor = false;
            this.xrLabel88.StylePriority.UseTextAlignment = false;
            this.xrLabel88.Text = "توقيع العميل ";
            this.xrLabel88.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel87
            // 
            this.xrLabel87.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel87.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel87.ForeColor = System.Drawing.Color.DarkBlue;
            this.xrLabel87.LocationFloat = new DevExpress.Utils.PointFloat(202.8169F, 188.6257F);
            this.xrLabel87.Multiline = true;
            this.xrLabel87.Name = "xrLabel87";
            this.xrLabel87.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel87.SizeF = new System.Drawing.SizeF(103.8275F, 23F);
            this.xrLabel87.StylePriority.UseBorders = false;
            this.xrLabel87.StylePriority.UseFont = false;
            this.xrLabel87.StylePriority.UseForeColor = false;
            this.xrLabel87.StylePriority.UseTextAlignment = false;
            this.xrLabel87.Text = "توقيع المحاسب";
            this.xrLabel87.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel86
            // 
            this.xrLabel86.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel86.Font = new DevExpress.Drawing.DXFont("Arial", 11F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel86.ForeColor = System.Drawing.Color.DarkBlue;
            this.xrLabel86.LocationFloat = new DevExpress.Utils.PointFloat(0F, 102.4167F);
            this.xrLabel86.Multiline = true;
            this.xrLabel86.Name = "xrLabel86";
            this.xrLabel86.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel86.SizeF = new System.Drawing.SizeF(787.3331F, 67.79165F);
            this.xrLabel86.StylePriority.UseBorders = false;
            this.xrLabel86.StylePriority.UseFont = false;
            this.xrLabel86.StylePriority.UseForeColor = false;
            this.xrLabel86.StylePriority.UseTextAlignment = false;
            this.xrLabel86.Text = resources.GetString("xrLabel86.Text");
            this.xrLabel86.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel84
            // 
            this.xrLabel84.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel84.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel84.ForeColor = System.Drawing.Color.DarkBlue;
            this.xrLabel84.LocationFloat = new DevExpress.Utils.PointFloat(503.8224F, 3.178914E-05F);
            this.xrLabel84.Multiline = true;
            this.xrLabel84.Name = "xrLabel84";
            this.xrLabel84.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel84.SizeF = new System.Drawing.SizeF(106.2619F, 23F);
            this.xrLabel84.StylePriority.UseBorders = false;
            this.xrLabel84.StylePriority.UseFont = false;
            this.xrLabel84.StylePriority.UseForeColor = false;
            this.xrLabel84.StylePriority.UseTextAlignment = false;
            this.xrLabel84.Text = "إجمالى الخصم:";
            this.xrLabel84.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel20
            // 
            this.xrLabel20.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel20.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[VoucherDiscountValue]")});
            this.xrLabel20.LocationFloat = new DevExpress.Utils.PointFloat(610.3778F, 0F);
            this.xrLabel20.Multiline = true;
            this.xrLabel20.Name = "xrLabel20";
            this.xrLabel20.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel20.SizeF = new System.Drawing.SizeF(136.6375F, 23F);
            this.xrLabel20.StylePriority.UseBorders = false;
            this.xrLabel20.StylePriority.UseTextAlignment = false;
            this.xrLabel20.Text = "xrLabel20";
            this.xrLabel20.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel20.TextFormatString = "{0:0.00}";
            // 
            // xrLabel83
            // 
            this.xrLabel83.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel83.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel83.ForeColor = System.Drawing.Color.DarkBlue;
            this.xrLabel83.LocationFloat = new DevExpress.Utils.PointFloat(503.9057F, 68.99999F);
            this.xrLabel83.Multiline = true;
            this.xrLabel83.Name = "xrLabel83";
            this.xrLabel83.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel83.SizeF = new System.Drawing.SizeF(106.9719F, 23F);
            this.xrLabel83.StylePriority.UseBorders = false;
            this.xrLabel83.StylePriority.UseFont = false;
            this.xrLabel83.StylePriority.UseForeColor = false;
            this.xrLabel83.StylePriority.UseTextAlignment = false;
            this.xrLabel83.Text = "الإجمالي :";
            this.xrLabel83.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel17
            // 
            this.xrLabel17.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel17.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[NetTotal]")});
            this.xrLabel17.LocationFloat = new DevExpress.Utils.PointFloat(613.048F, 23.00002F);
            this.xrLabel17.Multiline = true;
            this.xrLabel17.Name = "xrLabel17";
            this.xrLabel17.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel17.SizeF = new System.Drawing.SizeF(134.0507F, 23F);
            this.xrLabel17.StylePriority.UseBorders = false;
            this.xrLabel17.StylePriority.UseTextAlignment = false;
            this.xrLabel17.Text = "xrLabel17";
            this.xrLabel17.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel17.TextFormatString = "{0:0.00}";
            // 
            // xrLabel18
            // 
            this.xrLabel18.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel18.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[TotalAfterTaxes]")});
            this.xrLabel18.LocationFloat = new DevExpress.Utils.PointFloat(613.0479F, 68.99999F);
            this.xrLabel18.Multiline = true;
            this.xrLabel18.Name = "xrLabel18";
            this.xrLabel18.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel18.SizeF = new System.Drawing.SizeF(134.0506F, 23F);
            this.xrLabel18.StylePriority.UseBorders = false;
            this.xrLabel18.StylePriority.UseTextAlignment = false;
            this.xrLabel18.Text = "xrLabel18";
            this.xrLabel18.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel18.TextFormatString = "{0:0.00}";
            // 
            // xrLabel80
            // 
            this.xrLabel80.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel80.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel80.ForeColor = System.Drawing.Color.DarkBlue;
            this.xrLabel80.LocationFloat = new DevExpress.Utils.PointFloat(504.699F, 45.99997F);
            this.xrLabel80.Multiline = true;
            this.xrLabel80.Name = "xrLabel80";
            this.xrLabel80.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel80.SizeF = new System.Drawing.SizeF(106.262F, 23F);
            this.xrLabel80.StylePriority.UseBorders = false;
            this.xrLabel80.StylePriority.UseFont = false;
            this.xrLabel80.StylePriority.UseForeColor = false;
            this.xrLabel80.StylePriority.UseTextAlignment = false;
            this.xrLabel80.Text = "ضريبة القيمة المضافة:";
            this.xrLabel80.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel21
            // 
            this.xrLabel21.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel21.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[SalesTaxes]")});
            this.xrLabel21.LocationFloat = new DevExpress.Utils.PointFloat(612.3378F, 46.00003F);
            this.xrLabel21.Multiline = true;
            this.xrLabel21.Name = "xrLabel21";
            this.xrLabel21.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel21.SizeF = new System.Drawing.SizeF(134.7608F, 23F);
            this.xrLabel21.StylePriority.UseBorders = false;
            this.xrLabel21.StylePriority.UseTextAlignment = false;
            this.xrLabel21.Text = "xrLabel21";
            this.xrLabel21.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel21.TextFormatString = "{0:0.00}";
            // 
            // xrLabel78
            // 
            this.xrLabel78.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel78.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel78.ForeColor = System.Drawing.Color.DarkBlue;
            this.xrLabel78.LocationFloat = new DevExpress.Utils.PointFloat(504.7824F, 23.00002F);
            this.xrLabel78.Multiline = true;
            this.xrLabel78.Name = "xrLabel78";
            this.xrLabel78.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel78.SizeF = new System.Drawing.SizeF(107.3054F, 23F);
            this.xrLabel78.StylePriority.UseBorders = false;
            this.xrLabel78.StylePriority.UseFont = false;
            this.xrLabel78.StylePriority.UseForeColor = false;
            this.xrLabel78.StylePriority.UseTextAlignment = false;
            this.xrLabel78.Text = "الاجمالي بعد الخصم :";
            this.xrLabel78.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrControlStyle1
            // 
            this.xrControlStyle1.BackColor = System.Drawing.Color.LightGray;
            this.xrControlStyle1.Name = "xrControlStyle1";
            this.xrControlStyle1.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            // 
            // CustomerId
            // 
            this.CustomerId.AllowNull = true;
            this.CustomerId.Description = "العميل";
            this.CustomerId.Name = "CustomerId";
            this.CustomerId.Type = typeof(int);
            this.CustomerId.Visible = false;
            // 
            // Id
            // 
            this.Id.AllowNull = true;
            this.Id.Name = "Id";
            this.Id.Type = typeof(int);
            this.Id.Visible = false;
            // 
            // UserId
            // 
            this.UserId.AllowNull = true;
            this.UserId.Description = "Parameter1";
            this.UserId.Name = "UserId";
            this.UserId.Type = typeof(int);
            this.UserId.Visible = false;
            // 
            // PageHeader
            // 
            this.PageHeader.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel76,
            this.xrLabel77,
            this.xrLabel74,
            this.xrLabel75,
            this.xrLabel70,
            this.xrLabel36,
            this.xrLabel72,
            this.xrLabel12,
            this.xrLabel68,
            this.xrLabel69,
            this.xrLabel66,
            this.xrLabel31,
            this.xrLabel64,
            this.xrLabel13,
            this.xrLabel60,
            this.xrLabel10,
            this.xrLabel62,
            this.xrLabel28,
            this.xrLabel59,
            this.xrLabel57,
            this.xrLabel58,
            this.xrLabel55,
            this.xrLabel56,
            this.xrLabel49,
            this.xrLabel2,
            this.xrLabel48,
            this.xrLabel46,
            this.xrLabel47,
            this.xrLabel44,
            this.xrLabel45,
            this.xrLabel42,
            this.xrLabel7,
            this.xrLabel40,
            this.xrLabel41,
            this.xrLabel39,
            this.xrLabel38,
            this.xrLabel11,
            this.xrLabel53,
            this.xrPictureBox1,
            this.xrBarCode1,
            this.xrLabel51});
            this.PageHeader.HeightF = 499.4249F;
            this.PageHeader.Name = "PageHeader";
            // 
            // pageInfo2
            // 
            this.pageInfo2.LocationFloat = new DevExpress.Utils.PointFloat(427.1666F, 43.06679F);
            this.pageInfo2.Name = "pageInfo2";
            this.pageInfo2.SizeF = new System.Drawing.SizeF(351F, 23F);
            this.pageInfo2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.pageInfo2.TextFormatString = "Page {0} of {1}";
            // 
            // Time
            // 
            this.Time.BackColor = System.Drawing.Color.White;
            this.Time.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.Time.ForeColor = System.Drawing.Color.Black;
            this.Time.LocationFloat = new DevExpress.Utils.PointFloat(538.1256F, 9.999974F);
            this.Time.Multiline = true;
            this.Time.Name = "Time";
            this.Time.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.Time.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.No;
            this.Time.SizeF = new System.Drawing.SizeF(241F, 23F);
            this.Time.StylePriority.UseBackColor = false;
            this.Time.StylePriority.UseFont = false;
            this.Time.StylePriority.UseForeColor = false;
            this.Time.StylePriority.UseTextAlignment = false;
            this.Time.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter;
            this.Time.BeforePrint += new BeforePrintEventHandler(this.Time_BeforePrint);
            // 
            // xrLabel6
            // 
            this.xrLabel6.BackColor = System.Drawing.Color.White;
            this.xrLabel6.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.xrLabel6.ForeColor = System.Drawing.Color.Black;
            this.xrLabel6.LocationFloat = new DevExpress.Utils.PointFloat(438.1256F, 9.999974F);
            this.xrLabel6.Multiline = true;
            this.xrLabel6.Name = "xrLabel6";
            this.xrLabel6.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel6.SizeF = new System.Drawing.SizeF(100F, 23F);
            this.xrLabel6.StylePriority.UseBackColor = false;
            this.xrLabel6.StylePriority.UseFont = false;
            this.xrLabel6.StylePriority.UseForeColor = false;
            this.xrLabel6.Text = "الوقت";
            // 
            // xrLabel4
            // 
            this.xrLabel4.BackColor = System.Drawing.Color.White;
            this.xrLabel4.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.xrLabel4.ForeColor = System.Drawing.Color.Black;
            this.xrLabel4.LocationFloat = new DevExpress.Utils.PointFloat(9.916626F, 9.999974F);
            this.xrLabel4.Multiline = true;
            this.xrLabel4.Name = "xrLabel4";
            this.xrLabel4.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel4.SizeF = new System.Drawing.SizeF(100F, 23F);
            this.xrLabel4.StylePriority.UseBackColor = false;
            this.xrLabel4.StylePriority.UseFont = false;
            this.xrLabel4.StylePriority.UseForeColor = false;
            this.xrLabel4.Text = "المستخدم";
            // 
            // User
            // 
            this.User.BackColor = System.Drawing.Color.White;
            this.User.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.User.ForeColor = System.Drawing.Color.Black;
            this.User.LocationFloat = new DevExpress.Utils.PointFloat(109.917F, 9.999974F);
            this.User.Multiline = true;
            this.User.Name = "User";
            this.User.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.User.SizeF = new System.Drawing.SizeF(214.0416F, 23F);
            this.User.StylePriority.UseBackColor = false;
            this.User.StylePriority.UseFont = false;
            this.User.StylePriority.UseForeColor = false;
            this.User.StylePriority.UseTextAlignment = false;
            this.User.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter;
            // 
            // CarEntrance_Report
            // 
            this.Bands.AddRange(new DevExpress.XtraReports.UI.Band[] {
            this.TopMargin,
            this.BottomMargin,
            this.Detail,
            this.DetailReport,
            this.ReportFooter,
            this.PageHeader});
            this.CalculatedFields.AddRange(new DevExpress.XtraReports.UI.CalculatedField[] {
            this.ExtendedTotal});
            this.ComponentStorage.AddRange(new System.ComponentModel.IComponent[] {
            this.sqlDataSource1});
            this.DataMember = "CarEntrance_Get";
            this.DataSource = this.sqlDataSource1;
            this.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.Margins = new DevExpress.Drawing.DXMargins(20, 19, 28, 68);
            this.PageHeight = 1169;
            this.PageWidth = 827;
            this.PaperKind = DevExpress.Drawing.Printing.DXPaperKind.A4;
            this.Parameters.AddRange(new DevExpress.XtraReports.Parameters.Parameter[] {
            this.DocNum,
            this.DepId,
            this.DateFrom,
            this.DateTo,
            this.CustomerId,
            this.Id,
            this.UserId});
            this.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.RightToLeftLayout = DevExpress.XtraReports.UI.RightToLeftLayout.Yes;
            this.StyleSheet.AddRange(new DevExpress.XtraReports.UI.XRControlStyle[] {
            this.xrControlStyle1});
            this.Version = "20.1";
            ((System.ComponentModel.ISupportInitialize)(this.xrTable4)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();

    }

    #endregion

    private void xrLabel8_BeforePrint(object sender, CancelEventArgs e)
    {
        Currency DefaultCurrency = db.Currencies.Where(a => a.IsActive == true && a.IsDeleted == false && a.IsDefault == true).FirstOrDefault();
        CurrencyInfo obj = new CurrencyInfo(DefaultCurrency);
        var totalAfterTaxesCell = GetCurrentColumnValue("TotalAfterTaxes");
        string total = totalAfterTaxesCell != null ? totalAfterTaxesCell.ToString() : "0";
        ToWord w = new ToWord(decimal.Parse(total), obj, null, null, "", "");
        ((XRLabel)sender).Text = w.ConvertToArabic();
    }


    private void xrPictureBox1_BeforePrint(object sender, CancelEventArgs e)
    {
        //Set System Picture
        //SystemSetting systemSetting = new SystemSetting();
        //systemSetting.Logo = db.SystemSettings.FirstOrDefault().Logo.ToString();
        //xrPictureBox1.ImageUrl = systemSetting.Logo;
        int? DepartmentId = (int?)this.DepId.Value;
        var logo = HelperController.GetActivityLogo(DepartmentId);
        xrPictureBox1.ImageUrl = logo;
        int? id = (int?)this.Id.Value;
        var invoice = db.CarEntrance_Get(id).ToList();

        var Seller = invoice[0].CompanyEnName.ToString();
        var TaxNumber = invoice[0].TaxCardNumber.ToString();
        var InvoiceDate = invoice[0].VoucherDate.ToString();
        var InvoiceTotalAmount = invoice[0].TotalAfterTaxes.ToString();
        var InvoiceTaxAmount = invoice[0].SalesTaxes.ToString();
        var x = HelperController.EncodeTLV(Seller, TaxNumber, InvoiceDate, InvoiceTotalAmount, InvoiceTaxAmount);
        this.xrBarCode1.Text = x.ToString();

       // this.xrBarCode1.Text = domain + "/Report/CarEntrance/id=" + this.Id.Value + "?print=True";
    }

    private void Time_BeforePrint(object sender, CancelEventArgs e)
    {
        DateTime utcNow = DateTime.UtcNow;
        TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
        DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
        ((XRLabel)sender).Text = cTime.ToString("d MMMM yyyy h:mm tt");
    }
}
