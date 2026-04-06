using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;
using System.Web;
using System.Security.Claims;
using MyERP;
using MyERP.Models;
using MyERP.Controllers;
using System.Linq;
using DevExpress.Drawing.Printing;

/// <summary>
/// Summary description for SalesInvoice_Get
/// </summary>
public class CarEntranceWorkOrder_Report : DevExpress.XtraReports.UI.XtraReport
{
    private MySoftERPEntity db = new MySoftERPEntity();
    private TopMarginBand TopMargin;
    private BottomMarginBand BottomMargin;
    private DetailBand Detail;
    private DevExpress.DataAccess.Sql.SqlDataSource sqlDataSource1;
    private DetailReportBand DetailReport;
    private DetailBand Detail1;
    private XRTable xrTable1;
    private XRTableRow xrTableRow1;
    private XRTableCell xrTableCell1;
    private XRTableCell xrTableCell2;
    private GroupHeaderBand GroupHeader1;
    private CalculatedField ExtendedTotal;
    private XRLabel xrLabel2;
    private XRLabel xrLabel12;
    private XRLabel xrLabel10;
    private DevExpress.XtraReports.Parameters.Parameter DocNum;
    private DevExpress.XtraReports.Parameters.Parameter DepId;
    private DevExpress.XtraReports.Parameters.Parameter DateFrom;
    private DevExpress.XtraReports.Parameters.Parameter DateTo;
    private XRControlStyle xrControlStyle1;
    private DevExpress.XtraReports.Parameters.Parameter CustomerId;
    private DevExpress.XtraReports.Parameters.Parameter Id;
    private DevExpress.XtraReports.Parameters.Parameter UserId;
    private XRLabel xrLabel28;
    private XRLabel xrLabel13;
    private XRLabel xrLabel31;
    private XRLabel xrLabel7;
    private XRLabel xrLabel36;
    private XRLabel xrLabel11;
    private XRLabel xrLabel38;
    private XRLabel xrLabel39;
    private XRLabel xrLabel41;
    private XRLabel xrLabel40;
    private XRLabel xrLabel42;
    private XRLabel xrLabel45;
    private XRLabel xrLabel44;
    private XRLabel xrLabel47;
    private XRLabel xrLabel46;
    private XRLabel xrLabel48;
    private XRLabel xrLabel49;
    private XRLabel xrLabel52;
    private XRLabel xrLabel51;
    private XRLabel xrLabel54;
    private XRLabel xrLabel53;
    private XRLabel xrLabel56;
    private XRLabel xrLabel55;
    private XRLabel xrLabel58;
    private XRLabel xrLabel57;
    private XRLabel xrLabel59;
    private XRLabel xrLabel62;
    private XRLabel xrLabel60;
    private XRLabel xrLabel64;
    private XRLabel xrLabel66;
    private XRLabel xrLabel69;
    private XRLabel xrLabel68;
    private XRLabel xrLabel72;
    private XRLabel xrLabel70;
    private XRLabel xrLabel75;
    private XRLabel xrLabel74;
    private XRLabel xrLabel77;
    private XRLabel xrLabel76;
    private XRTable xrTable3;
    private XRTableRow xrTableRow3;
    private XRTableCell xrTableCell13;
    private XRTableCell xrTableCell14;
    private ReportFooterBand ReportFooter;
    private XRLabel xrLabel87;
    private XRLabel xrLabel88;
    private XRLabel xrLabel89;
    private XRPictureBox xrPictureBox1;
    private PageHeaderBand PageHeader;
    private XRLabel xrLabel6;
    private XRLabel Time;
    private XRPageInfo pageInfo2;
    private XRLabel User;
    private XRLabel xrLabel4;

    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    public CarEntranceWorkOrder_Report()
    {
        InitializeComponent();
        UserId.Value= int.Parse(((ClaimsIdentity)HttpContext.Current.User.Identity).FindFirst("Id").Value);
        User.Text = HttpContext.Current.User.Identity.Name;
        //
        // TODO: Add constructor logic here

    }
    public CarEntranceWorkOrder_Report(string paperKind, bool? printPrice)
    {
        InitializeComponent();
        UserId.Value = int.Parse(((ClaimsIdentity)HttpContext.Current.User.Identity).FindFirst("Id").Value);
        User.Text = HttpContext.Current.User.Identity.Name;

        if (!string.IsNullOrEmpty(paperKind))
        {
            switch (paperKind)
            {
                case "A4":
                    PaperKind = DXPaperKind.A4; //System.Drawing.Printing.PaperKind.A4;
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CarEntranceWorkOrder_Report));
            DevExpress.DataAccess.Sql.StoredProcQuery storedProcQuery2 = new DevExpress.DataAccess.Sql.StoredProcQuery();
            DevExpress.DataAccess.Sql.QueryParameter queryParameter2 = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.DataAccess.Sql.MasterDetailInfo masterDetailInfo1 = new DevExpress.DataAccess.Sql.MasterDetailInfo();
            DevExpress.DataAccess.Sql.RelationColumnInfo relationColumnInfo1 = new DevExpress.DataAccess.Sql.RelationColumnInfo();
            DevExpress.DataAccess.Sql.MasterDetailInfo masterDetailInfo2 = new DevExpress.DataAccess.Sql.MasterDetailInfo();
            DevExpress.DataAccess.Sql.RelationColumnInfo relationColumnInfo2 = new DevExpress.DataAccess.Sql.RelationColumnInfo();
            DevExpress.XtraReports.Parameters.DynamicListLookUpSettings dynamicListLookUpSettings1 = new DevExpress.XtraReports.Parameters.DynamicListLookUpSettings();
            this.sqlDataSource1 = new DevExpress.DataAccess.Sql.SqlDataSource(this.components);
            this.TopMargin = new DevExpress.XtraReports.UI.TopMarginBand();
            this.xrPictureBox1 = new DevExpress.XtraReports.UI.XRPictureBox();
            this.BottomMargin = new DevExpress.XtraReports.UI.BottomMarginBand();
            this.xrLabel6 = new DevExpress.XtraReports.UI.XRLabel();
            this.Time = new DevExpress.XtraReports.UI.XRLabel();
            this.pageInfo2 = new DevExpress.XtraReports.UI.XRPageInfo();
            this.User = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel4 = new DevExpress.XtraReports.UI.XRLabel();
            this.Detail = new DevExpress.XtraReports.UI.DetailBand();
            this.xrLabel72 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel70 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel75 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel74 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel77 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel76 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel48 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel49 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel52 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel51 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel54 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel53 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel56 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel55 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel58 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel57 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel59 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel62 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel60 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel64 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel66 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel69 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel68 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel42 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel45 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel44 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel47 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel46 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel11 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel38 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel39 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel41 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel40 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel7 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel36 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel31 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel28 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel13 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel12 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel10 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel2 = new DevExpress.XtraReports.UI.XRLabel();
            this.DetailReport = new DevExpress.XtraReports.UI.DetailReportBand();
            this.Detail1 = new DevExpress.XtraReports.UI.DetailBand();
            this.xrTable1 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow1 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell1 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell2 = new DevExpress.XtraReports.UI.XRTableCell();
            this.GroupHeader1 = new DevExpress.XtraReports.UI.GroupHeaderBand();
            this.xrTable3 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow3 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell13 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell14 = new DevExpress.XtraReports.UI.XRTableCell();
            this.ExtendedTotal = new DevExpress.XtraReports.UI.CalculatedField();
            this.DocNum = new DevExpress.XtraReports.Parameters.Parameter();
            this.DepId = new DevExpress.XtraReports.Parameters.Parameter();
            this.DateFrom = new DevExpress.XtraReports.Parameters.Parameter();
            this.DateTo = new DevExpress.XtraReports.Parameters.Parameter();
            this.xrControlStyle1 = new DevExpress.XtraReports.UI.XRControlStyle();
            this.CustomerId = new DevExpress.XtraReports.Parameters.Parameter();
            this.Id = new DevExpress.XtraReports.Parameters.Parameter();
            this.UserId = new DevExpress.XtraReports.Parameters.Parameter();
            this.ReportFooter = new DevExpress.XtraReports.UI.ReportFooterBand();
            this.xrLabel87 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel88 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel89 = new DevExpress.XtraReports.UI.XRLabel();
            this.PageHeader = new DevExpress.XtraReports.UI.PageHeaderBand();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable1)).BeginInit();
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
            storedProcQuery1.Parameters.AddRange(new DevExpress.DataAccess.Sql.QueryParameter[] {
            queryParameter1});
            storedProcQuery1.StoredProcName = "CarEntrance_Get";
            customSqlQuery1.MetaSerializable = "<Meta X=\"225\" Y=\"20\" Width=\"155\" Height=\"236\" />";
            customSqlQuery1.Name = "Query";
            customSqlQuery1.Sql = resources.GetString("customSqlQuery1.Sql");
            storedProcQuery2.MetaSerializable = "<Meta X=\"400\" Y=\"10\" Width=\"108\" Height=\"89\" />";
            storedProcQuery2.Name = "Department_1";
            queryParameter2.Name = "@UserId";
            queryParameter2.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter2.Value = new DevExpress.DataAccess.Expression("?UserId", typeof(int));
            storedProcQuery2.Parameters.AddRange(new DevExpress.DataAccess.Sql.QueryParameter[] {
            queryParameter2});
            storedProcQuery2.StoredProcName = "Department_ReportUserDepartments";
            this.sqlDataSource1.Queries.AddRange(new DevExpress.DataAccess.Sql.SqlQuery[] {
            storedProcQuery1,
            customSqlQuery1,
            storedProcQuery2});
            masterDetailInfo1.DetailQueryName = "Department_1";
            relationColumnInfo1.NestedKeyColumn = "Id";
            relationColumnInfo1.ParentKeyColumn = "Department";
            masterDetailInfo1.KeyColumns.Add(relationColumnInfo1);
            masterDetailInfo1.MasterQueryName = "CarEntrance_Get";
            masterDetailInfo2.DetailQueryName = "Query";
            relationColumnInfo2.NestedKeyColumn = "MainDocId";
            relationColumnInfo2.ParentKeyColumn = "Id";
            masterDetailInfo2.KeyColumns.Add(relationColumnInfo2);
            masterDetailInfo2.MasterQueryName = "CarEntrance_Get";
            this.sqlDataSource1.Relations.AddRange(new DevExpress.DataAccess.Sql.MasterDetailInfo[] {
            masterDetailInfo1,
            masterDetailInfo2});
            this.sqlDataSource1.ResultSchemaSerializable = resources.GetString("sqlDataSource1.ResultSchemaSerializable");
            // 
            // TopMargin
            // 
            this.TopMargin.HeightF = 28.33333F;
            this.TopMargin.Name = "TopMargin";
            // 
            // xrPictureBox1
            // 
            this.xrPictureBox1.ImageUrl = "http://genoise1.mysoft-eg.com/assets/images/logo.png";
            this.xrPictureBox1.LocationFloat = new DevExpress.Utils.PointFloat(116.4168F, 0F);
            this.xrPictureBox1.Name = "xrPictureBox1";
            this.xrPictureBox1.SizeF = new System.Drawing.SizeF(542.6666F, 106.3333F);
            this.xrPictureBox1.Sizing = DevExpress.XtraPrinting.ImageSizeMode.ZoomImage;
            this.xrPictureBox1.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrPictureBox1_BeforePrint);
            // 
            // BottomMargin
            // 
            this.BottomMargin.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel6,
            this.Time,
            this.pageInfo2,
            this.User,
            this.xrLabel4});
            this.BottomMargin.HeightF = 56.06681F;
            this.BottomMargin.Name = "BottomMargin";
            // 
            // xrLabel6
            // 
            this.xrLabel6.BackColor = System.Drawing.Color.White;
            this.xrLabel6.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.xrLabel6.ForeColor = System.Drawing.Color.Black;
            this.xrLabel6.LocationFloat = new DevExpress.Utils.PointFloat(438.1256F, 0F);
            this.xrLabel6.Multiline = true;
            this.xrLabel6.Name = "xrLabel6";
            this.xrLabel6.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel6.SizeF = new System.Drawing.SizeF(100F, 23F);
            this.xrLabel6.StylePriority.UseBackColor = false;
            this.xrLabel6.StylePriority.UseFont = false;
            this.xrLabel6.StylePriority.UseForeColor = false;
            this.xrLabel6.Text = "الوقت";
            // 
            // Time
            // 
            this.Time.BackColor = System.Drawing.Color.White;
            this.Time.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.Time.ForeColor = System.Drawing.Color.Black;
            this.Time.LocationFloat = new DevExpress.Utils.PointFloat(538.1256F, 0F);
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
            this.Time.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.Time_BeforePrint);
            // 
            // pageInfo2
            // 
            this.pageInfo2.LocationFloat = new DevExpress.Utils.PointFloat(427.1666F, 33.06681F);
            this.pageInfo2.Name = "pageInfo2";
            this.pageInfo2.SizeF = new System.Drawing.SizeF(351F, 23F);
            this.pageInfo2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.pageInfo2.TextFormatString = "Page {0} of {1}";
            // 
            // User
            // 
            this.User.BackColor = System.Drawing.Color.White;
            this.User.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.User.ForeColor = System.Drawing.Color.Black;
            this.User.LocationFloat = new DevExpress.Utils.PointFloat(109.917F, 0F);
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
            // xrLabel4
            // 
            this.xrLabel4.BackColor = System.Drawing.Color.White;
            this.xrLabel4.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.xrLabel4.ForeColor = System.Drawing.Color.Black;
            this.xrLabel4.LocationFloat = new DevExpress.Utils.PointFloat(9.916626F, 0F);
            this.xrLabel4.Multiline = true;
            this.xrLabel4.Name = "xrLabel4";
            this.xrLabel4.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel4.SizeF = new System.Drawing.SizeF(100F, 23F);
            this.xrLabel4.StylePriority.UseBackColor = false;
            this.xrLabel4.StylePriority.UseFont = false;
            this.xrLabel4.StylePriority.UseForeColor = false;
            this.xrLabel4.Text = "المستخدم";
            // 
            // Detail
            // 
            this.Detail.HeightF = 17.00026F;
            this.Detail.Name = "Detail";
            this.Detail.StylePriority.UseTextAlignment = false;
            this.Detail.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel72
            // 
            this.xrLabel72.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel72.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel72.LocationFloat = new DevExpress.Utils.PointFloat(568.3332F, 412.5415F);
            this.xrLabel72.Name = "xrLabel72";
            this.xrLabel72.SizeF = new System.Drawing.SizeF(111.1669F, 23F);
            this.xrLabel72.StylePriority.UseBorders = false;
            this.xrLabel72.StylePriority.UseFont = false;
            this.xrLabel72.StylePriority.UseTextAlignment = false;
            this.xrLabel72.Text = "تاريخ خروج السيارة :";
            this.xrLabel72.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel70
            // 
            this.xrLabel70.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel70.LocationFloat = new DevExpress.Utils.PointFloat(136.8334F, 412.5415F);
            this.xrLabel70.Name = "xrLabel70";
            this.xrLabel70.SizeF = new System.Drawing.SizeF(110.8335F, 23F);
            this.xrLabel70.StylePriority.UseBorders = false;
            this.xrLabel70.StylePriority.UseFont = false;
            this.xrLabel70.StylePriority.UseTextAlignment = false;
            this.xrLabel70.Text = "تاريخ دخول السيارة :";
            this.xrLabel70.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel75
            // 
            this.xrLabel75.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel75.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CarInOdometer]")});
            this.xrLabel75.LocationFloat = new DevExpress.Utils.PointFloat(265.3753F, 435.5415F);
            this.xrLabel75.Name = "xrLabel75";
            this.xrLabel75.SizeF = new System.Drawing.SizeF(135.5F, 23F);
            this.xrLabel75.StylePriority.UseBorders = false;
            this.xrLabel75.StylePriority.UseTextAlignment = false;
            this.xrLabel75.Text = "عداد السيارة عند الدخول";
            this.xrLabel75.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel74
            // 
            this.xrLabel74.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel74.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel74.LocationFloat = new DevExpress.Utils.PointFloat(265.3753F, 412.5415F);
            this.xrLabel74.Name = "xrLabel74";
            this.xrLabel74.SizeF = new System.Drawing.SizeF(135.8333F, 23F);
            this.xrLabel74.StylePriority.UseBorders = false;
            this.xrLabel74.StylePriority.UseFont = false;
            this.xrLabel74.StylePriority.UseTextAlignment = false;
            this.xrLabel74.Text = "عداد السيارة عند الدخول :";
            this.xrLabel74.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel77
            // 
            this.xrLabel77.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel77.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CarOutOdometer]")});
            this.xrLabel77.LocationFloat = new DevExpress.Utils.PointFloat(416.6668F, 435.5415F);
            this.xrLabel77.Name = "xrLabel77";
            this.xrLabel77.SizeF = new System.Drawing.SizeF(135.5F, 23F);
            this.xrLabel77.StylePriority.UseBorders = false;
            this.xrLabel77.StylePriority.UseTextAlignment = false;
            this.xrLabel77.Text = "عداد السيارة عند الخروج";
            this.xrLabel77.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel76
            // 
            this.xrLabel76.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel76.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel76.LocationFloat = new DevExpress.Utils.PointFloat(416.6667F, 412.5415F);
            this.xrLabel76.Name = "xrLabel76";
            this.xrLabel76.SizeF = new System.Drawing.SizeF(135.8333F, 23F);
            this.xrLabel76.StylePriority.UseBorders = false;
            this.xrLabel76.StylePriority.UseFont = false;
            this.xrLabel76.StylePriority.UseTextAlignment = false;
            this.xrLabel76.Text = "عداد السيارة عند الخروج:";
            this.xrLabel76.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel48
            // 
            this.xrLabel48.BorderColor = System.Drawing.Color.Silver;
            this.xrLabel48.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel48.Font = new DevExpress.Drawing.DXFont("Arial", 12F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel48.ForeColor = System.Drawing.Color.DarkBlue;
            this.xrLabel48.LocationFloat = new DevExpress.Utils.PointFloat(9.916687F, 246.0416F);
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
            this.xrLabel48.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel49
            // 
            this.xrLabel49.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel49.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel49.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.xrLabel49.LocationFloat = new DevExpress.Utils.PointFloat(9.916687F, 280.4166F);
            this.xrLabel49.Name = "xrLabel49";
            this.xrLabel49.SizeF = new System.Drawing.SizeF(99.6666F, 23F);
            this.xrLabel49.StylePriority.UseBorders = false;
            this.xrLabel49.StylePriority.UseFont = false;
            this.xrLabel49.StylePriority.UseForeColor = false;
            this.xrLabel49.Text = "اسم العميل :";
            // 
            // xrLabel52
            // 
            this.xrLabel52.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel52.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel52.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.xrLabel52.LocationFloat = new DevExpress.Utils.PointFloat(9.916687F, 303.4166F);
            this.xrLabel52.Name = "xrLabel52";
            this.xrLabel52.SizeF = new System.Drawing.SizeF(99.6666F, 23F);
            this.xrLabel52.StylePriority.UseBorders = false;
            this.xrLabel52.StylePriority.UseFont = false;
            this.xrLabel52.StylePriority.UseForeColor = false;
            this.xrLabel52.Text = "اسم الشركة :";
            // 
            // xrLabel51
            // 
            this.xrLabel51.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel51.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CompanyArName]")});
            this.xrLabel51.LocationFloat = new DevExpress.Utils.PointFloat(109.5833F, 303.4166F);
            this.xrLabel51.Name = "xrLabel51";
            this.xrLabel51.SizeF = new System.Drawing.SizeF(143.5766F, 23.00002F);
            this.xrLabel51.StylePriority.UseBorders = false;
            this.xrLabel51.Text = "CompanyName";
            // 
            // xrLabel54
            // 
            this.xrLabel54.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel54.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel54.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.xrLabel54.LocationFloat = new DevExpress.Utils.PointFloat(9.916687F, 326.4166F);
            this.xrLabel54.Name = "xrLabel54";
            this.xrLabel54.SizeF = new System.Drawing.SizeF(99.6666F, 23F);
            this.xrLabel54.StylePriority.UseBorders = false;
            this.xrLabel54.StylePriority.UseFont = false;
            this.xrLabel54.StylePriority.UseForeColor = false;
            this.xrLabel54.Text = "عنوان الشركة :";
            // 
            // xrLabel53
            // 
            this.xrLabel53.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel53.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CompanyArName]")});
            this.xrLabel53.LocationFloat = new DevExpress.Utils.PointFloat(109.5833F, 326.4166F);
            this.xrLabel53.Name = "xrLabel53";
            this.xrLabel53.SizeF = new System.Drawing.SizeF(143.2433F, 23.00002F);
            this.xrLabel53.StylePriority.UseBorders = false;
            this.xrLabel53.Text = "CompanyAddress";
            // 
            // xrLabel56
            // 
            this.xrLabel56.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel56.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel56.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.xrLabel56.LocationFloat = new DevExpress.Utils.PointFloat(9.833374F, 349.4165F);
            this.xrLabel56.Name = "xrLabel56";
            this.xrLabel56.SizeF = new System.Drawing.SizeF(99.6666F, 23F);
            this.xrLabel56.StylePriority.UseBorders = false;
            this.xrLabel56.StylePriority.UseFont = false;
            this.xrLabel56.StylePriority.UseForeColor = false;
            this.xrLabel56.Text = "نوع العميل :";
            // 
            // xrLabel55
            // 
            this.xrLabel55.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel55.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CustomersGroupArName]")});
            this.xrLabel55.LocationFloat = new DevExpress.Utils.PointFloat(109.4998F, 349.4166F);
            this.xrLabel55.Name = "xrLabel55";
            this.xrLabel55.SizeF = new System.Drawing.SizeF(143.66F, 23.00003F);
            this.xrLabel55.StylePriority.UseBorders = false;
            this.xrLabel55.Text = "CustomerType";
            // 
            // xrLabel58
            // 
            this.xrLabel58.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel58.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel58.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.xrLabel58.LocationFloat = new DevExpress.Utils.PointFloat(9.500122F, 372.4166F);
            this.xrLabel58.Name = "xrLabel58";
            this.xrLabel58.SizeF = new System.Drawing.SizeF(99.99991F, 23F);
            this.xrLabel58.StylePriority.UseBorders = false;
            this.xrLabel58.StylePriority.UseFont = false;
            this.xrLabel58.StylePriority.UseForeColor = false;
            this.xrLabel58.Text = "رقم الهاتف :";
            // 
            // xrLabel57
            // 
            this.xrLabel57.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel57.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CustomerMobile]")});
            this.xrLabel57.LocationFloat = new DevExpress.Utils.PointFloat(109.5F, 372.4167F);
            this.xrLabel57.Name = "xrLabel57";
            this.xrLabel57.SizeF = new System.Drawing.SizeF(143.5767F, 23.00002F);
            this.xrLabel57.StylePriority.UseBorders = false;
            this.xrLabel57.Text = "PhoneNumbe";
            // 
            // xrLabel59
            // 
            this.xrLabel59.BorderColor = System.Drawing.Color.Silver;
            this.xrLabel59.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel59.Font = new DevExpress.Drawing.DXFont("Arial", 12F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel59.ForeColor = System.Drawing.Color.DarkBlue;
            this.xrLabel59.LocationFloat = new DevExpress.Utils.PointFloat(518.4584F, 233.5416F);
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
            this.xrLabel59.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel62
            // 
            this.xrLabel62.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel62.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel62.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.xrLabel62.LocationFloat = new DevExpress.Utils.PointFloat(519.2915F, 291.875F);
            this.xrLabel62.Name = "xrLabel62";
            this.xrLabel62.SizeF = new System.Drawing.SizeF(100F, 23F);
            this.xrLabel62.StylePriority.UseBorders = false;
            this.xrLabel62.StylePriority.UseFont = false;
            this.xrLabel62.StylePriority.UseForeColor = false;
            this.xrLabel62.Text = "لون السيارة :";
            // 
            // xrLabel60
            // 
            this.xrLabel60.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel60.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel60.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.xrLabel60.LocationFloat = new DevExpress.Utils.PointFloat(518.9584F, 268.875F);
            this.xrLabel60.Name = "xrLabel60";
            this.xrLabel60.SizeF = new System.Drawing.SizeF(100F, 23F);
            this.xrLabel60.StylePriority.UseBorders = false;
            this.xrLabel60.StylePriority.UseFont = false;
            this.xrLabel60.StylePriority.UseForeColor = false;
            this.xrLabel60.Text = "نوع السيارة :";
            // 
            // xrLabel64
            // 
            this.xrLabel64.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel64.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel64.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.xrLabel64.LocationFloat = new DevExpress.Utils.PointFloat(518.625F, 314.8749F);
            this.xrLabel64.Name = "xrLabel64";
            this.xrLabel64.SizeF = new System.Drawing.SizeF(100F, 23F);
            this.xrLabel64.StylePriority.UseBorders = false;
            this.xrLabel64.StylePriority.UseFont = false;
            this.xrLabel64.StylePriority.UseForeColor = false;
            this.xrLabel64.Text = "موديل السيارة :";
            // 
            // xrLabel66
            // 
            this.xrLabel66.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel66.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel66.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.xrLabel66.LocationFloat = new DevExpress.Utils.PointFloat(518.6249F, 337.8749F);
            this.xrLabel66.Name = "xrLabel66";
            this.xrLabel66.SizeF = new System.Drawing.SizeF(99.33337F, 23F);
            this.xrLabel66.StylePriority.UseBorders = false;
            this.xrLabel66.StylePriority.UseFont = false;
            this.xrLabel66.StylePriority.UseForeColor = false;
            this.xrLabel66.Text = "رقم اللوحة :";
            // 
            // xrLabel69
            // 
            this.xrLabel69.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel69.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CarSerialNumber]")});
            this.xrLabel69.LocationFloat = new DevExpress.Utils.PointFloat(617.4584F, 360.8749F);
            this.xrLabel69.Name = "xrLabel69";
            this.xrLabel69.SizeF = new System.Drawing.SizeF(146.4166F, 23.00001F);
            this.xrLabel69.StylePriority.UseBorders = false;
            this.xrLabel69.Text = "رقم الشاسيه";
            // 
            // xrLabel68
            // 
            this.xrLabel68.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel68.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel68.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.xrLabel68.LocationFloat = new DevExpress.Utils.PointFloat(518.1251F, 360.8749F);
            this.xrLabel68.Name = "xrLabel68";
            this.xrLabel68.SizeF = new System.Drawing.SizeF(99.33322F, 23F);
            this.xrLabel68.StylePriority.UseBorders = false;
            this.xrLabel68.StylePriority.UseFont = false;
            this.xrLabel68.StylePriority.UseForeColor = false;
            this.xrLabel68.Text = "رقم الشاسيه:";
            // 
            // xrLabel42
            // 
            this.xrLabel42.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel42.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel42.ForeColor = System.Drawing.Color.DarkBlue;
            this.xrLabel42.LocationFloat = new DevExpress.Utils.PointFloat(518.625F, 187.5417F);
            this.xrLabel42.Name = "xrLabel42";
            this.xrLabel42.SizeF = new System.Drawing.SizeF(100F, 23F);
            this.xrLabel42.StylePriority.UseBorders = false;
            this.xrLabel42.StylePriority.UseFont = false;
            this.xrLabel42.StylePriority.UseForeColor = false;
            this.xrLabel42.Text = "رقم امر الشغل :";
            // 
            // xrLabel45
            // 
            this.xrLabel45.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel45.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[VoucherDate]")});
            this.xrLabel45.LocationFloat = new DevExpress.Utils.PointFloat(619.2916F, 164.5416F);
            this.xrLabel45.Name = "xrLabel45";
            this.xrLabel45.SizeF = new System.Drawing.SizeF(139.6667F, 23.00001F);
            this.xrLabel45.StylePriority.UseBorders = false;
            this.xrLabel45.Text = "Date";
            // 
            // xrLabel44
            // 
            this.xrLabel44.BorderColor = System.Drawing.Color.Black;
            this.xrLabel44.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel44.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel44.ForeColor = System.Drawing.Color.DarkBlue;
            this.xrLabel44.LocationFloat = new DevExpress.Utils.PointFloat(518.9583F, 164.5417F);
            this.xrLabel44.Name = "xrLabel44";
            this.xrLabel44.SizeF = new System.Drawing.SizeF(100F, 23F);
            this.xrLabel44.StylePriority.UseBorderColor = false;
            this.xrLabel44.StylePriority.UseBorders = false;
            this.xrLabel44.StylePriority.UseFont = false;
            this.xrLabel44.StylePriority.UseForeColor = false;
            this.xrLabel44.Text = "التاريخ :";
            // 
            // xrLabel47
            // 
            this.xrLabel47.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel47.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CustomerCode]")});
            this.xrLabel47.LocationFloat = new DevExpress.Utils.PointFloat(618.9584F, 210.5416F);
            this.xrLabel47.Name = "xrLabel47";
            this.xrLabel47.SizeF = new System.Drawing.SizeF(139.6667F, 23.00001F);
            this.xrLabel47.StylePriority.UseBorders = false;
            this.xrLabel47.Text = "CustomerCode";
            // 
            // xrLabel46
            // 
            this.xrLabel46.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel46.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel46.ForeColor = System.Drawing.Color.DarkBlue;
            this.xrLabel46.LocationFloat = new DevExpress.Utils.PointFloat(518.6251F, 210.5416F);
            this.xrLabel46.Name = "xrLabel46";
            this.xrLabel46.SizeF = new System.Drawing.SizeF(100F, 23F);
            this.xrLabel46.StylePriority.UseBorders = false;
            this.xrLabel46.StylePriority.UseFont = false;
            this.xrLabel46.StylePriority.UseForeColor = false;
            this.xrLabel46.Text = "كود العميل :";
            // 
            // xrLabel11
            // 
            this.xrLabel11.BorderColor = System.Drawing.Color.Silver;
            this.xrLabel11.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel11.Font = new DevExpress.Drawing.DXFont("Arial", 18F, DevExpress.Drawing.DXFontStyle.Bold, DevExpress.Drawing.DXGraphicsUnit.Point, new DevExpress.Drawing.DXFontAdditionalProperty[] {
            new DevExpress.Drawing.DXFontAdditionalProperty("GdiCharSet", ((byte)(0)))});
            this.xrLabel11.ForeColor = System.Drawing.Color.DarkBlue;
            this.xrLabel11.LocationFloat = new DevExpress.Utils.PointFloat(9.916687F, 119.6666F);
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
            this.xrLabel11.Text = "أمر شغل";
            this.xrLabel11.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // xrLabel38
            // 
            this.xrLabel38.Font = new DevExpress.Drawing.DXFont("Arial", 12F, DevExpress.Drawing.DXFontStyle.Bold, DevExpress.Drawing.DXGraphicsUnit.Point, new DevExpress.Drawing.DXFontAdditionalProperty[] {
            new DevExpress.Drawing.DXFontAdditionalProperty("GdiCharSet", ((byte)(0)))});
            this.xrLabel38.ForeColor = System.Drawing.Color.DarkBlue;
            this.xrLabel38.LocationFloat = new DevExpress.Utils.PointFloat(9.916687F, 154.0416F);
            this.xrLabel38.Multiline = true;
            this.xrLabel38.Name = "xrLabel38";
            this.xrLabel38.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel38.SizeF = new System.Drawing.SizeF(136.8333F, 23F);
            this.xrLabel38.StylePriority.UseFont = false;
            this.xrLabel38.StylePriority.UseForeColor = false;
            this.xrLabel38.Text = "الرقم الضريبى";
            // 
            // xrLabel39
            // 
            this.xrLabel39.LocationFloat = new DevExpress.Utils.PointFloat(9.916687F, 177.0416F);
            this.xrLabel39.Multiline = true;
            this.xrLabel39.Name = "xrLabel39";
            this.xrLabel39.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel39.SizeF = new System.Drawing.SizeF(136.5F, 23F);
            this.xrLabel39.Text = "310730519400003";
            // 
            // xrLabel41
            // 
            this.xrLabel41.LocationFloat = new DevExpress.Utils.PointFloat(9.916687F, 223.0416F);
            this.xrLabel41.Multiline = true;
            this.xrLabel41.Name = "xrLabel41";
            this.xrLabel41.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel41.SizeF = new System.Drawing.SizeF(136.5F, 22.99999F);
            this.xrLabel41.Text = "1010656264";
            // 
            // xrLabel40
            // 
            this.xrLabel40.Font = new DevExpress.Drawing.DXFont("Arial", 12F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel40.ForeColor = System.Drawing.Color.DarkBlue;
            this.xrLabel40.LocationFloat = new DevExpress.Utils.PointFloat(9.916687F, 200.0417F);
            this.xrLabel40.Multiline = true;
            this.xrLabel40.Name = "xrLabel40";
            this.xrLabel40.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel40.SizeF = new System.Drawing.SizeF(136.1667F, 23F);
            this.xrLabel40.StylePriority.UseFont = false;
            this.xrLabel40.StylePriority.UseForeColor = false;
            this.xrLabel40.Text = "السجل التجارى";
            // 
            // xrLabel7
            // 
            this.xrLabel7.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel7.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[DocumentNumber]")});
            this.xrLabel7.LocationFloat = new DevExpress.Utils.PointFloat(618.9584F, 187.5417F);
            this.xrLabel7.Name = "xrLabel7";
            this.xrLabel7.SizeF = new System.Drawing.SizeF(139.6666F, 22.99999F);
            this.xrLabel7.StylePriority.UseBorders = false;
            this.xrLabel7.TextFormatString = "{0:yyyy-MM-dd}";
            // 
            // xrLabel36
            // 
            this.xrLabel36.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel36.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[VoucherDate]")});
            this.xrLabel36.LocationFloat = new DevExpress.Utils.PointFloat(136.8334F, 435.5415F);
            this.xrLabel36.Name = "xrLabel36";
            this.xrLabel36.SizeF = new System.Drawing.SizeF(110.5002F, 23F);
            this.xrLabel36.StylePriority.UseBorders = false;
            this.xrLabel36.TextFormatString = "{0:yyyy-MM-dd}";
            // 
            // xrLabel31
            // 
            this.xrLabel31.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel31.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CarPlateNumber]")});
            this.xrLabel31.LocationFloat = new DevExpress.Utils.PointFloat(617.9583F, 337.8751F);
            this.xrLabel31.Name = "xrLabel31";
            this.xrLabel31.SizeF = new System.Drawing.SizeF(145.8334F, 23F);
            this.xrLabel31.StylePriority.UseBorders = false;
            // 
            // xrLabel28
            // 
            this.xrLabel28.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel28.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CarColorArName]")});
            this.xrLabel28.LocationFloat = new DevExpress.Utils.PointFloat(619.2915F, 291.8752F);
            this.xrLabel28.Name = "xrLabel28";
            this.xrLabel28.SizeF = new System.Drawing.SizeF(144.5002F, 23F);
            this.xrLabel28.StylePriority.UseBorders = false;
            // 
            // xrLabel13
            // 
            this.xrLabel13.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel13.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CarModelArName]")});
            this.xrLabel13.LocationFloat = new DevExpress.Utils.PointFloat(618.625F, 314.8751F);
            this.xrLabel13.Name = "xrLabel13";
            this.xrLabel13.SizeF = new System.Drawing.SizeF(145.1667F, 23F);
            this.xrLabel13.StylePriority.UseBorders = false;
            // 
            // xrLabel12
            // 
            this.xrLabel12.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel12.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[OutDate]")});
            this.xrLabel12.LocationFloat = new DevExpress.Utils.PointFloat(567.9998F, 435.5415F);
            this.xrLabel12.Name = "xrLabel12";
            this.xrLabel12.SizeF = new System.Drawing.SizeF(111.1669F, 23F);
            this.xrLabel12.StylePriority.UseBorders = false;
            this.xrLabel12.TextFormatString = "{0:yyyy-MM-dd}";
            // 
            // xrLabel10
            // 
            this.xrLabel10.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel10.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CarTypeArName]")});
            this.xrLabel10.LocationFloat = new DevExpress.Utils.PointFloat(618.9584F, 268.875F);
            this.xrLabel10.Name = "xrLabel10";
            this.xrLabel10.SizeF = new System.Drawing.SizeF(144.8333F, 22.99997F);
            this.xrLabel10.StylePriority.UseBorders = false;
            // 
            // xrLabel2
            // 
            this.xrLabel2.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel2.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CustomerArName]")});
            this.xrLabel2.LocationFloat = new DevExpress.Utils.PointFloat(109.9166F, 280.4166F);
            this.xrLabel2.Name = "xrLabel2";
            this.xrLabel2.SizeF = new System.Drawing.SizeF(142.9099F, 23.00002F);
            this.xrLabel2.StylePriority.UseBorders = false;
            // 
            // DetailReport
            // 
            this.DetailReport.Bands.AddRange(new DevExpress.XtraReports.UI.Band[] {
            this.Detail1,
            this.GroupHeader1});
            this.DetailReport.DataMember = "Query";
            this.DetailReport.DataSource = this.sqlDataSource1;
            this.DetailReport.Level = 0;
            this.DetailReport.Name = "DetailReport";
            this.DetailReport.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // Detail1
            // 
            this.Detail1.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrTable1});
            this.Detail1.HeightF = 26.04167F;
            this.Detail1.Name = "Detail1";
            // 
            // xrTable1
            // 
            this.xrTable1.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable1.EvenStyleName = "xrControlStyle1";
            this.xrTable1.LocationFloat = new DevExpress.Utils.PointFloat(0F, 1.041667F);
            this.xrTable1.Name = "xrTable1";
            this.xrTable1.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow1});
            this.xrTable1.SizeF = new System.Drawing.SizeF(786.8333F, 25F);
            this.xrTable1.StylePriority.UseBorders = false;
            // 
            // xrTableRow1
            // 
            this.xrTableRow1.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell1,
            this.xrTableCell2});
            this.xrTableRow1.Name = "xrTableRow1";
            this.xrTableRow1.Weight = 11.5D;
            // 
            // xrTableCell1
            // 
            this.xrTableCell1.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CarServiceCode]")});
            this.xrTableCell1.Multiline = true;
            this.xrTableCell1.Name = "xrTableCell1";
            this.xrTableCell1.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTableCell1.Text = "xrTableCell1";
            this.xrTableCell1.Weight = 0.59424934580824162D;
            // 
            // xrTableCell2
            // 
            this.xrTableCell2.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CarServiceName]")});
            this.xrTableCell2.Multiline = true;
            this.xrTableCell2.Name = "xrTableCell2";
            this.xrTableCell2.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTableCell2.Text = "xrTableCell2";
            this.xrTableCell2.Weight = 0.75533807936965314D;
            // 
            // GroupHeader1
            // 
            this.GroupHeader1.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrTable3});
            this.GroupHeader1.HeightF = 25.41644F;
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
            this.xrTable3.LocationFloat = new DevExpress.Utils.PointFloat(9.536743E-06F, 0.4164378F);
            this.xrTable3.Name = "xrTable3";
            this.xrTable3.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow3});
            this.xrTable3.SizeF = new System.Drawing.SizeF(786.9167F, 25F);
            this.xrTable3.StylePriority.UseBorderColor = false;
            this.xrTable3.StylePriority.UseBorders = false;
            this.xrTable3.StylePriority.UseForeColor = false;
            // 
            // xrTableRow3
            // 
            this.xrTableRow3.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell13,
            this.xrTableCell14});
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
            this.xrTableCell13.Weight = 0.74941195283393658D;
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
            this.xrTableCell14.Weight = 0.95233340389186627D;
            // 
            // ExtendedTotal
            // 
            this.ExtendedTotal.DataMember = "CarEntrance_Get";
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
            this.DateFrom.ValueInfo = "01/06/2000 14:30:26";
            // 
            // DateTo
            // 
            this.DateTo.Description = "التاريخ إلى";
            this.DateTo.Name = "DateTo";
            this.DateTo.Type = typeof(System.DateTime);
            this.DateTo.ValueInfo = "01/06/3000 14:30:26";
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
            // ReportFooter
            // 
            this.ReportFooter.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel87,
            this.xrLabel88,
            this.xrLabel89});
            this.ReportFooter.HeightF = 50.6251F;
            this.ReportFooter.Name = "ReportFooter";
            // 
            // xrLabel87
            // 
            this.xrLabel87.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel87.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel87.ForeColor = System.Drawing.Color.DarkBlue;
            this.xrLabel87.LocationFloat = new DevExpress.Utils.PointFloat(169.6311F, 9.999974F);
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
            // xrLabel88
            // 
            this.xrLabel88.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel88.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel88.ForeColor = System.Drawing.Color.DarkBlue;
            this.xrLabel88.LocationFloat = new DevExpress.Utils.PointFloat(345.1556F, 9.999974F);
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
            // xrLabel89
            // 
            this.xrLabel89.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel89.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel89.ForeColor = System.Drawing.Color.DarkBlue;
            this.xrLabel89.LocationFloat = new DevExpress.Utils.PointFloat(522.3811F, 9.999974F);
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
            // PageHeader
            // 
            this.PageHeader.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel72,
            this.xrLabel70,
            this.xrLabel75,
            this.xrLabel74,
            this.xrLabel77,
            this.xrLabel76,
            this.xrLabel48,
            this.xrLabel49,
            this.xrLabel52,
            this.xrLabel51,
            this.xrLabel54,
            this.xrLabel53,
            this.xrLabel56,
            this.xrLabel55,
            this.xrLabel58,
            this.xrLabel57,
            this.xrLabel59,
            this.xrLabel62,
            this.xrLabel60,
            this.xrLabel64,
            this.xrLabel66,
            this.xrLabel69,
            this.xrLabel68,
            this.xrLabel42,
            this.xrLabel45,
            this.xrLabel44,
            this.xrLabel47,
            this.xrLabel46,
            this.xrLabel2,
            this.xrLabel38,
            this.xrLabel39,
            this.xrLabel41,
            this.xrLabel40,
            this.xrLabel7,
            this.xrLabel36,
            this.xrLabel31,
            this.xrLabel28,
            this.xrLabel13,
            this.xrLabel12,
            this.xrLabel10,
            this.xrLabel11,
            this.xrPictureBox1});
            this.PageHeader.HeightF = 458.5415F;
            this.PageHeader.Name = "PageHeader";
            // 
            // CarEntranceWorkOrder_Report
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
            this.DataMember = "Query";
            this.DataSource = this.sqlDataSource1;
            this.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.Margins = new DevExpress.Drawing.DXMargins(20F, 20F, 28.33333F, 56.06681F);
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
            this.Version = "23.1";
            ((System.ComponentModel.ISupportInitialize)(this.xrTable1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();

    }

    #endregion

    private void xrLabel8_BeforePrint(object sender, CancelEventArgs e)
    {
        Currency currency = db.Currencies.Where(a => a.IsActive == true && a.IsDeleted == false && a.IsDefault == true).FirstOrDefault();
        Currency DefaultCurrencyBySystem = db.Currencies.Where(a => a.IsActive == true && a.IsDeleted == false && a.Code == "EGP").FirstOrDefault();
        CurrencyInfo obj = new CurrencyInfo(DefaultCurrencyBySystem);
        if (currency != null)
        {
            //CurrencyInfo obj = new CurrencyInfo(CurrencyInfo.Currencies.Egy);
            obj = new CurrencyInfo(currency);
        }
        var totalAfterTaxesCell = GetCurrentColumnValue("TotalAfterTaxes");
        string total = totalAfterTaxesCell!=null? totalAfterTaxesCell.ToString():"0";
        ToWord w = new ToWord(decimal.Parse(total), obj, null, null, "", "");
        ((XRLabel)sender).Text = w.ConvertToArabic();
    }

    private void xrPictureBox1_BeforePrint(object sender, CancelEventArgs e)
    {
        ////Set System Picture
        //SystemSetting systemSetting = new SystemSetting();
        //systemSetting.Logo = db.SystemSettings.FirstOrDefault().Logo.ToString();
        //xrPictureBox1.ImageUrl = systemSetting.Logo;
        int? DepartmentId = (int?)this.DepId.Value;
        var logo = HelperController.GetActivityLogo(DepartmentId);
        xrPictureBox1.ImageUrl = logo;
    }

    private void Time_BeforePrint(object sender, CancelEventArgs e)
    {
        DateTime utcNow = DateTime.UtcNow;
        TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
        DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
        ((XRLabel)sender).Text = cTime.ToString("d MMMM yyyy h:mm tt");
    }
}
