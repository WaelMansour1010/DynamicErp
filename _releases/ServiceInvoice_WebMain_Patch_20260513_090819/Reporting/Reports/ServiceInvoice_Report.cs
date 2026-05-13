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
using System.Globalization;

/// <summary>
/// Summary description for ServiceInvoice_Get
/// </summary>
public class ServiceInvoice_Report : DevExpress.XtraReports.UI.XtraReport
{
    private MySoftERPEntity db = new MySoftERPEntity();
    private TopMarginBand TopMargin;
    private BottomMarginBand BottomMargin;
    private DetailBand Detail;
    private DevExpress.DataAccess.Sql.SqlDataSource sqlDataSource1;
    private CalculatedField ExtendedTotal;
    private ReportFooterBand ReportFooter;
    private XRControlStyle xrControlStyle1;
    private DevExpress.XtraReports.Parameters.Parameter UserId;
    private DetailReportBand DetailReport;
    private DetailBand Detail1;
    private ReportHeaderBand ReportHeader;
    private XRTable xrTable3;
    private XRTableRow xrTableRow3;
    private XRTableCell xrTableCell30;
    private XRTableCell xrTableCell32;
    private XRTable xrTable11;
    private XRTableRow xrTableRow28;
    private XRTableCell xrTableCell74;
    private XRTableCell xrTableCell75;
    private DevExpress.XtraReports.Parameters.Parameter Id;
    private XRLine xrLine1;
    private XRTable xrTable10;
    private XRTableRow xrTableRow19;
    private XRTableCell xrTableCell39;
    private XRTableCell xrTableCell40;
    private XRTableCell xrTableCell41;
    private XRTableRow xrTableRow20;
    private XRTableCell xrTableCell42;
    private XRTableCell xrTableCell43;
    private XRTableCell xrTableCell44;
    private XRTableRow xrTableRow21;
    private XRTableCell xrTableCell45;
    private XRTableCell xrTableCell46;
    private XRTableCell xrTableCell47;
    private XRTableRow xrTableRow22;
    private XRTableCell xrTableCell48;
    private XRTableCell xrTableCell49;
    private XRTableCell xrTableCell50;
    private XRTable xrTable4;
    private XRTableRow xrTableRow4;
    private XRTableCell xrTableCell33;
    private XRTableCell xrTableCell34;
    private XRTable xrTable8;
    private XRTableRow xrTableRow10;
    private XRTableCell xrTableCell21;
    private XRTableRow xrTableRow11;
    private XRTableCell xrTableCell27;
    private XRTableRow xrTableRow12;
    private XRTableCell xrTableCell31;
    private XRTableRow xrTableRow13;
    private XRTableCell xrTableCell22;
    private XRTable xrTable7;
    private XRTableRow xrTableRow7;
    private XRTableCell xrTableCell23;
    private XRTableRow xrTableRow8;
    private XRTableCell xrTableCell26;
    private XRTableRow xrTableRow9;
    private XRTableCell xrTableCell29;
    private XRTableRow xrTableRow14;
    private XRTableCell xrTableCell24;
    private XRPictureBox xrPictureBox1;
    private XRBarCode xrBarCode1;

    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;
    string domain = "";
    private PageHeaderBand PageHeader;
    private XRLabel xrLabel4;
    private XRLabel Time;
    private XRLabel User;
    private XRLabel xrLabel6;
    private XRLabel xrLabel10;
    private XRLabel xrLabel11;
    private XRLabel xrLabel12;
    private XRLabel xrLabel13;
    private XRLabel xrLabel14;
    private XRLabel xrLabel15;
    private XRLabel xrLabel16;
    private XRLabel xrLabel1;
    private XRLabel xrLabel2;
    private XRLabel xrLabel3;
    private XRLabel xrLabel5;
    private XRLabel xrLabel7;
    private XRLabel xrLabel8;
    private XRLabel xrLabel9;
    private XRLabel xrLabel17;
    private XRLabel xrLabel18;
    private XRLabel xrLabel19;
    private XRLabel xrLabel20;
    private XRLabel xrLabel21;
    private XRLabel xrLabel22;
    private XRLabel xrLabel23;
    private XRLabel xrLabel24;
    private XRLabel xrLabel25;
    private XRLabel xrLabel27;
    private XRLabel xrLabel28;
    private XRLabel xrLabel29;
    private XRLabel xrLabel30;
    private XRLabel xrLabel31;
    private XRLabel xrLabel32;
    private XRLabel xrLabel33;
    private XRLabel xrLabel34;
    private XRLabel xrLabel35;
    private XRLabel xrLabel36;
    private XRLabel xrLabel37;
    private XRLabel xrLabel38;
    private XRLabel xrLabel39;
    private XRLabel xrLabel40;
    private XRLabel xrLabel41;
    private XRLabel xrLabel43;
    private XRLabel xrLabel44;
    private XRLabel xrLabel45;
    private XRLabel xrLabel47;
    private XRLabel xrLabel48;
    private XRLabel xrLabel49;
    private XRLabel xrLabel51;
    private XRLabel xrLabel52;
    private XRLabel xrLabel53;
    private XRLabel xrLabel55;
    private XRLabel xrLabel42;
    private XRLabel xrLabel26;
    private XRLabel xrLabel46;
    private XRLabel xrLabel50;
    private XRLabel xrLabel56;
    private XRLabel xrLabel54;
    private XRLabel xrLabel57;
    private XRLabel xrLabel58;
    private XRLabel xrLabel59;
    private XRLabel xrLabel60;
    private XRLabel xrLabel61;
    private XRLabel xrLabel62;
    private XRLabel xrLabel63;
    private XRLabel xrLabel64;
    private XRLabel xrLabel65;
    private XRLabel xrLabel66;
    private XRLabel xrLabel67;
    private XRLabel xrLabel68;
    private XRLabel xrLabel69;
    private XRLabel xrLabel70;
    private XRLabel xrLabel71;
    private XRLabel xrLabel72;
    private XRLabel xrLabel73;
    private XRLabel xrLabel74;
    private XRLabel xrLabel75;
    private XRLabel xrLabel76;
    private XRLabel xrLabel77;
    string id = "";
    public ServiceInvoice_Report(string _id, string domain = "")
    {
        this.domain = domain;
        InitializeComponent();
        this.id = _id;
        UserId.Value = int.Parse(((ClaimsIdentity)HttpContext.Current.User.Identity).FindFirst("Id").Value);
        User.Text = HttpContext.Current.User.Identity.Name;

        // TODO: Add constructor logic here
        //var helper = new HelperController().GetOpenPeriodBeginningEndingForReports();
        //this.DateFrom.ValueInfo = helper.start;
        //this.DateTo.ValueInfo = helper.end;
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
            DevExpress.DataAccess.Sql.CustomSqlQuery customSqlQuery1 = new DevExpress.DataAccess.Sql.CustomSqlQuery();
            DevExpress.DataAccess.Sql.QueryParameter queryParameter1 = new DevExpress.DataAccess.Sql.QueryParameter();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ServiceInvoice_Report));
            DevExpress.DataAccess.Sql.CustomSqlQuery customSqlQuery2 = new DevExpress.DataAccess.Sql.CustomSqlQuery();
            DevExpress.DataAccess.Sql.QueryParameter queryParameter2 = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.DataAccess.Sql.StoredProcQuery storedProcQuery1 = new DevExpress.DataAccess.Sql.StoredProcQuery();
            DevExpress.DataAccess.Sql.QueryParameter queryParameter3 = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.DataAccess.Sql.StoredProcQuery storedProcQuery2 = new DevExpress.DataAccess.Sql.StoredProcQuery();
            DevExpress.DataAccess.Sql.QueryParameter queryParameter4 = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.DataAccess.Sql.MasterDetailInfo masterDetailInfo1 = new DevExpress.DataAccess.Sql.MasterDetailInfo();
            DevExpress.DataAccess.Sql.RelationColumnInfo relationColumnInfo1 = new DevExpress.DataAccess.Sql.RelationColumnInfo();
            DevExpress.XtraPrinting.BarCode.QRCodeGenerator qrCodeGenerator1 = new DevExpress.XtraPrinting.BarCode.QRCodeGenerator();
            this.sqlDataSource1 = new DevExpress.DataAccess.Sql.SqlDataSource(this.components);
            this.TopMargin = new DevExpress.XtraReports.UI.TopMarginBand();
            this.BottomMargin = new DevExpress.XtraReports.UI.BottomMarginBand();
            this.xrLabel4 = new DevExpress.XtraReports.UI.XRLabel();
            this.Time = new DevExpress.XtraReports.UI.XRLabel();
            this.User = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel6 = new DevExpress.XtraReports.UI.XRLabel();
            this.Detail = new DevExpress.XtraReports.UI.DetailBand();
            this.xrLabel42 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel26 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel46 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel50 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel56 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrTable10 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow19 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell39 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell40 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell41 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow20 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell42 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell43 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell44 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow21 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell45 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell46 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell47 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow22 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell48 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell49 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell50 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTable4 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow4 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell33 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell34 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrLabel20 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel21 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel22 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel23 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel24 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel25 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel27 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel28 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel29 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel30 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel31 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel32 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel33 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel34 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel35 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel36 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel37 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel38 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel39 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel40 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel41 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel43 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel44 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel45 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel47 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel48 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel49 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel51 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel52 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel53 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel55 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel74 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel75 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel76 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel77 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrBarCode1 = new DevExpress.XtraReports.UI.XRBarCode();
            this.xrLine1 = new DevExpress.XtraReports.UI.XRLine();
            this.xrTable8 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow10 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell21 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow11 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell27 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow12 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell31 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow13 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell22 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTable7 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow7 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell23 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow8 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell26 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow9 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell29 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow14 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell24 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrPictureBox1 = new DevExpress.XtraReports.UI.XRPictureBox();
            this.ExtendedTotal = new DevExpress.XtraReports.UI.CalculatedField();
            this.ReportFooter = new DevExpress.XtraReports.UI.ReportFooterBand();
            this.xrTable11 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow28 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell74 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell75 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrLabel54 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel57 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel58 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel59 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel60 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel61 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel62 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel63 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel64 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel65 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel66 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel67 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel68 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel69 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel70 = new DevExpress.XtraReports.UI.XRLabel();
            this.Id = new DevExpress.XtraReports.Parameters.Parameter();
            this.xrControlStyle1 = new DevExpress.XtraReports.UI.XRControlStyle();
            this.UserId = new DevExpress.XtraReports.Parameters.Parameter();
            this.DetailReport = new DevExpress.XtraReports.UI.DetailReportBand();
            this.Detail1 = new DevExpress.XtraReports.UI.DetailBand();
            this.xrLabel10 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel11 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel12 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel13 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel14 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel15 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel16 = new DevExpress.XtraReports.UI.XRLabel();
            this.ReportHeader = new DevExpress.XtraReports.UI.ReportHeaderBand();
            this.xrTable3 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow3 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell30 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell32 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrLabel1 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel2 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel3 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel5 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel7 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel8 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel9 = new DevExpress.XtraReports.UI.XRLabel();
            this.PageHeader = new DevExpress.XtraReports.UI.PageHeaderBand();
            this.xrLabel17 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel18 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel19 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel71 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel72 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel73 = new DevExpress.XtraReports.UI.XRLabel();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable10)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable4)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable8)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable7)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable11)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            // 
            // sqlDataSource1
            // 
            this.sqlDataSource1.ConnectionName = "localhost_MySoftERP_Connection";
            this.sqlDataSource1.Name = "sqlDataSource1";
            customSqlQuery1.MetaSerializable = "<Meta X=\"570\" Y=\"30\" Width=\"100\" Height=\"201\" />";
            customSqlQuery1.Name = "Details";
            queryParameter1.Name = "Id";
            queryParameter1.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter1.Value = new DevExpress.DataAccess.Expression("?Id", typeof(int));
            customSqlQuery1.Parameters.AddRange(new DevExpress.DataAccess.Sql.QueryParameter[] {
            queryParameter1});
            customSqlQuery1.Sql = resources.GetString("customSqlQuery1.Sql");
            customSqlQuery2.MetaSerializable = "<Meta X=\"345\" Y=\"20\" Width=\"109\" Height=\"101\" />";
            customSqlQuery2.Name = "PaymentMethods";
            queryParameter2.Name = "Id";
            queryParameter2.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter2.Value = new DevExpress.DataAccess.Expression("?Id", typeof(int));
            customSqlQuery2.Parameters.AddRange(new DevExpress.DataAccess.Sql.QueryParameter[] {
            queryParameter2});
            customSqlQuery2.Sql = resources.GetString("customSqlQuery2.Sql");
            storedProcQuery1.MetaSerializable = "<Meta X=\"1004\" Y=\"20\" Width=\"108\" Height=\"81\" />";
            storedProcQuery1.Name = "Department_1";
            queryParameter3.Name = "@UserId";
            queryParameter3.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter3.Value = new DevExpress.DataAccess.Expression("?UserId", typeof(int));
            storedProcQuery1.Parameters.AddRange(new DevExpress.DataAccess.Sql.QueryParameter[] {
            queryParameter3});
            storedProcQuery1.StoredProcName = "Department_ReportUserDepartments";
            storedProcQuery2.Name = "ServiceInvoice_Get";
            queryParameter4.Name = "@Id";
            queryParameter4.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter4.Value = new DevExpress.DataAccess.Expression("?Id", typeof(int));
            storedProcQuery2.Parameters.AddRange(new DevExpress.DataAccess.Sql.QueryParameter[] {
            queryParameter4});
            storedProcQuery2.StoredProcName = "ServiceInvoice_Get";
            this.sqlDataSource1.Queries.AddRange(new DevExpress.DataAccess.Sql.SqlQuery[] {
            customSqlQuery1,
            customSqlQuery2,
            storedProcQuery1,
            storedProcQuery2});
            masterDetailInfo1.DetailQueryName = "Details";
            relationColumnInfo1.NestedKeyColumn = "MainDocId";
            relationColumnInfo1.ParentKeyColumn = "ServiceInvoiceId";
            masterDetailInfo1.KeyColumns.Add(relationColumnInfo1);
            masterDetailInfo1.MasterQueryName = "ServiceInvoice_Get";
            this.sqlDataSource1.Relations.AddRange(new DevExpress.DataAccess.Sql.MasterDetailInfo[] {
            masterDetailInfo1});
            this.sqlDataSource1.ResultSchemaSerializable = resources.GetString("sqlDataSource1.ResultSchemaSerializable");
            // 
            // TopMargin
            // 
            this.TopMargin.HeightF = 20F;
            this.TopMargin.Name = "TopMargin";
            // 
            // BottomMargin
            // 
            this.BottomMargin.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel4,
            this.Time,
            this.User,
            this.xrLabel6});
            this.BottomMargin.HeightF = 28.48269F;
            this.BottomMargin.Name = "BottomMargin";
            // 
            // xrLabel4
            // 
            this.xrLabel4.BackColor = System.Drawing.Color.White;
            this.xrLabel4.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.xrLabel4.ForeColor = System.Drawing.Color.Black;
            this.xrLabel4.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.xrLabel4.Multiline = true;
            this.xrLabel4.Name = "xrLabel4";
            this.xrLabel4.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel4.SizeF = new System.Drawing.SizeF(100F, 23F);
            this.xrLabel4.StylePriority.UseBackColor = false;
            this.xrLabel4.StylePriority.UseFont = false;
            this.xrLabel4.StylePriority.UseForeColor = false;
            this.xrLabel4.Text = "المستخدم";
            // 
            // Time
            // 
            this.Time.BackColor = System.Drawing.Color.White;
            this.Time.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.Time.ForeColor = System.Drawing.Color.Black;
            this.Time.LocationFloat = new DevExpress.Utils.PointFloat(554.6331F, 0F);
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
            // User
            // 
            this.User.BackColor = System.Drawing.Color.White;
            this.User.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.User.ForeColor = System.Drawing.Color.Black;
            this.User.LocationFloat = new DevExpress.Utils.PointFloat(100.0002F, 0F);
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
            // xrLabel6
            // 
            this.xrLabel6.BackColor = System.Drawing.Color.White;
            this.xrLabel6.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.xrLabel6.ForeColor = System.Drawing.Color.Black;
            this.xrLabel6.LocationFloat = new DevExpress.Utils.PointFloat(454.633F, 0F);
            this.xrLabel6.Multiline = true;
            this.xrLabel6.Name = "xrLabel6";
            this.xrLabel6.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel6.SizeF = new System.Drawing.SizeF(100F, 23F);
            this.xrLabel6.StylePriority.UseBackColor = false;
            this.xrLabel6.StylePriority.UseFont = false;
            this.xrLabel6.StylePriority.UseForeColor = false;
            this.xrLabel6.Text = "الوقت";
            // 
            // Detail
            // 
            this.Detail.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.Detail.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.Detail.BorderWidth = 1F;
            this.Detail.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel42,
            this.xrLabel26,
            this.xrLabel46,
            this.xrLabel50,
            this.xrLabel56,
            this.xrTable10,
            this.xrTable4,
            this.xrLabel20,
            this.xrLabel21,
            this.xrLabel22,
            this.xrLabel23,
            this.xrLabel24,
            this.xrLabel25,
            this.xrLabel27,
            this.xrLabel28,
            this.xrLabel29,
            this.xrLabel30,
            this.xrLabel31,
            this.xrLabel32,
            this.xrLabel33,
            this.xrLabel34,
            this.xrLabel35,
            this.xrLabel36,
            this.xrLabel37,
            this.xrLabel38,
            this.xrLabel39,
            this.xrLabel40,
            this.xrLabel41,
            this.xrLabel43,
            this.xrLabel44,
            this.xrLabel45,
            this.xrLabel47,
            this.xrLabel74,
            this.xrLabel75,
            this.xrLabel76,
            this.xrLabel77,
            this.xrLabel48,
            this.xrLabel49,
            this.xrLabel51,
            this.xrLabel52,
            this.xrLabel53,
            this.xrLabel55});
            this.Detail.HeightF = 414.306F;
            this.Detail.Name = "Detail";
            this.Detail.StylePriority.UseBorderDashStyle = false;
            this.Detail.StylePriority.UseBorderWidth = false;
            this.Detail.StylePriority.UseTextAlignment = false;
            this.Detail.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel42
            // 
            this.xrLabel42.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel42.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel42.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel42.BorderWidth = 1F;
            this.xrLabel42.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CustomerPostOfficeBox]")});
            this.xrLabel42.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel42.LocationFloat = new DevExpress.Utils.PointFloat(425.5693F, 276.1043F);
            this.xrLabel42.Multiline = true;
            this.xrLabel42.Name = "xrLabel42";
            this.xrLabel42.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel42.SizeF = new System.Drawing.SizeF(247.0651F, 26.30682F);
            this.xrLabel42.StylePriority.UseBorderColor = false;
            this.xrLabel42.StylePriority.UseBorderDashStyle = false;
            this.xrLabel42.StylePriority.UseBorders = false;
            this.xrLabel42.StylePriority.UseBorderWidth = false;
            this.xrLabel42.StylePriority.UseFont = false;
            this.xrLabel42.StylePriority.UsePadding = false;
            this.xrLabel42.StylePriority.UseTextAlignment = false;
            this.xrLabel42.Text = "xrTableCell66";
            this.xrLabel42.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel26
            // 
            this.xrLabel26.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel26.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel26.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel26.BorderWidth = 1F;
            this.xrLabel26.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CustomerBuildingNo]")});
            this.xrLabel26.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel26.LocationFloat = new DevExpress.Utils.PointFloat(425.5695F, 170.877F);
            this.xrLabel26.Multiline = true;
            this.xrLabel26.Name = "xrLabel26";
            this.xrLabel26.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel26.SizeF = new System.Drawing.SizeF(247.0651F, 26.30682F);
            this.xrLabel26.StylePriority.UseBorderColor = false;
            this.xrLabel26.StylePriority.UseBorderDashStyle = false;
            this.xrLabel26.StylePriority.UseBorders = false;
            this.xrLabel26.StylePriority.UseBorderWidth = false;
            this.xrLabel26.StylePriority.UseFont = false;
            this.xrLabel26.StylePriority.UsePadding = false;
            this.xrLabel26.StylePriority.UseTextAlignment = false;
            this.xrLabel26.Text = "xrTableCell43";
            this.xrLabel26.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel46
            // 
            this.xrLabel46.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel46.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel46.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel46.BorderWidth = 1F;
            this.xrLabel46.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CustomerTaxNumber]")});
            this.xrLabel46.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel46.LocationFloat = new DevExpress.Utils.PointFloat(425.5693F, 302.4111F);
            this.xrLabel46.Multiline = true;
            this.xrLabel46.Name = "xrLabel46";
            this.xrLabel46.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel46.SizeF = new System.Drawing.SizeF(247.0651F, 26.30682F);
            this.xrLabel46.StylePriority.UseBorderColor = false;
            this.xrLabel46.StylePriority.UseBorderDashStyle = false;
            this.xrLabel46.StylePriority.UseBorders = false;
            this.xrLabel46.StylePriority.UseBorderWidth = false;
            this.xrLabel46.StylePriority.UseFont = false;
            this.xrLabel46.StylePriority.UsePadding = false;
            this.xrLabel46.StylePriority.UseTextAlignment = false;
            this.xrLabel46.Text = "xrTableCell72";
            this.xrLabel46.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel50
            // 
            this.xrLabel50.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel50.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel50.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel50.BorderWidth = 1F;
            this.xrLabel50.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CustomerMobile]")});
            this.xrLabel50.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel50.LocationFloat = new DevExpress.Utils.PointFloat(425.5695F, 355.0248F);
            this.xrLabel50.Multiline = true;
            this.xrLabel50.Name = "xrLabel50";
            this.xrLabel50.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel50.SizeF = new System.Drawing.SizeF(247.0651F, 26.30682F);
            this.xrLabel50.StylePriority.UseBorderColor = false;
            this.xrLabel50.StylePriority.UseBorderDashStyle = false;
            this.xrLabel50.StylePriority.UseBorders = false;
            this.xrLabel50.StylePriority.UseBorderWidth = false;
            this.xrLabel50.StylePriority.UseFont = false;
            this.xrLabel50.StylePriority.UsePadding = false;
            this.xrLabel50.StylePriority.UseTextAlignment = false;
            this.xrLabel50.Text = "xrTableCell69";
            this.xrLabel50.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel56
            // 
            this.xrLabel56.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel56.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel56.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
            this.xrLabel56.BorderWidth = 1F;
            this.xrLabel56.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CustomerPhoneNo]")});
            this.xrLabel56.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel56.LocationFloat = new DevExpress.Utils.PointFloat(425.5694F, 381.3316F);
            this.xrLabel56.Multiline = true;
            this.xrLabel56.Name = "xrLabel56";
            this.xrLabel56.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel56.SizeF = new System.Drawing.SizeF(247.0651F, 26.30682F);
            this.xrLabel56.StylePriority.UseBorderColor = false;
            this.xrLabel56.StylePriority.UseBorderDashStyle = false;
            this.xrLabel56.StylePriority.UseBorders = false;
            this.xrLabel56.StylePriority.UseBorderWidth = false;
            this.xrLabel56.StylePriority.UseFont = false;
            this.xrLabel56.StylePriority.UsePadding = false;
            this.xrLabel56.StylePriority.UseTextAlignment = false;
            this.xrLabel56.Text = "xrTableCell49";
            this.xrLabel56.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrTable10
            // 
            this.xrTable10.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrTable10.LocationFloat = new DevExpress.Utils.PointFloat(9.551697F, 10F);
            this.xrTable10.Name = "xrTable10";
            this.xrTable10.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable10.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow19,
            this.xrTableRow20,
            this.xrTableRow21,
            this.xrTableRow22});
            this.xrTable10.SizeF = new System.Drawing.SizeF(797.4483F, 109.5702F);
            this.xrTable10.StylePriority.UseBorders = false;
            // 
            // xrTableRow19
            // 
            this.xrTableRow19.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell39,
            this.xrTableCell40,
            this.xrTableCell41});
            this.xrTableRow19.Name = "xrTableRow19";
            this.xrTableRow19.Weight = 1D;
            // 
            // xrTableCell39
            // 
            this.xrTableCell39.BorderColor = System.Drawing.Color.LightGray;
            this.xrTableCell39.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell39.Multiline = true;
            this.xrTableCell39.Name = "xrTableCell39";
            this.xrTableCell39.StylePriority.UseBorderColor = false;
            this.xrTableCell39.StylePriority.UseBorders = false;
            this.xrTableCell39.Text = "رقم الفاتورة";
            this.xrTableCell39.Weight = 0.720005011800745D;
            // 
            // xrTableCell40
            // 
            this.xrTableCell40.BorderColor = System.Drawing.Color.LightGray;
            this.xrTableCell40.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell40.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[DocumentNumber]")});
            this.xrTableCell40.Multiline = true;
            this.xrTableCell40.Name = "xrTableCell40";
            this.xrTableCell40.StylePriority.UseBorderColor = false;
            this.xrTableCell40.StylePriority.UseBorders = false;
            this.xrTableCell40.StylePriority.UseTextAlignment = false;
            this.xrTableCell40.Text = "xrTableCell9";
            this.xrTableCell40.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell40.Weight = 1.05236820370413D;
            // 
            // xrTableCell41
            // 
            this.xrTableCell41.BorderColor = System.Drawing.Color.LightGray;
            this.xrTableCell41.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell41.Multiline = true;
            this.xrTableCell41.Name = "xrTableCell41";
            this.xrTableCell41.StylePriority.UseBorderColor = false;
            this.xrTableCell41.StylePriority.UseBorders = false;
            this.xrTableCell41.StylePriority.UseTextAlignment = false;
            this.xrTableCell41.Text = ".Invoice No";
            this.xrTableCell41.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell41.Weight = 0.57232642165097913D;
            // 
            // xrTableRow20
            // 
            this.xrTableRow20.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell42,
            this.xrTableCell43,
            this.xrTableCell44});
            this.xrTableRow20.Name = "xrTableRow20";
            this.xrTableRow20.Weight = 1D;
            // 
            // xrTableCell42
            // 
            this.xrTableCell42.BorderColor = System.Drawing.Color.LightGray;
            this.xrTableCell42.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell42.Multiline = true;
            this.xrTableCell42.Name = "xrTableCell42";
            this.xrTableCell42.StylePriority.UseBorderColor = false;
            this.xrTableCell42.StylePriority.UseBorders = false;
            this.xrTableCell42.Text = "تاريخ الفاتورة";
            this.xrTableCell42.Weight = 0.720005011800745D;
            // 
            // xrTableCell43
            // 
            this.xrTableCell43.BorderColor = System.Drawing.Color.LightGray;
            this.xrTableCell43.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell43.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[VoucherDate]")});
            this.xrTableCell43.Multiline = true;
            this.xrTableCell43.Name = "xrTableCell43";
            this.xrTableCell43.StylePriority.UseBorderColor = false;
            this.xrTableCell43.StylePriority.UseBorders = false;
            this.xrTableCell43.StylePriority.UseTextAlignment = false;
            this.xrTableCell43.Text = "xrTableCell43";
            this.xrTableCell43.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell43.TextFormatString = "{0:M/d/yyyy}";
            this.xrTableCell43.Weight = 1.05236820370413D;
            // 
            // xrTableCell44
            // 
            this.xrTableCell44.BorderColor = System.Drawing.Color.LightGray;
            this.xrTableCell44.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell44.Multiline = true;
            this.xrTableCell44.Name = "xrTableCell44";
            this.xrTableCell44.StylePriority.UseBorderColor = false;
            this.xrTableCell44.StylePriority.UseBorders = false;
            this.xrTableCell44.StylePriority.UseTextAlignment = false;
            this.xrTableCell44.Text = "Invoice Date";
            this.xrTableCell44.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell44.Weight = 0.57232642165097913D;
            // 
            // xrTableRow21
            // 
            this.xrTableRow21.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell45,
            this.xrTableCell46,
            this.xrTableCell47});
            this.xrTableRow21.Name = "xrTableRow21";
            this.xrTableRow21.Weight = 1D;
            // 
            // xrTableCell45
            // 
            this.xrTableCell45.BorderColor = System.Drawing.Color.LightGray;
            this.xrTableCell45.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell45.Multiline = true;
            this.xrTableCell45.Name = "xrTableCell45";
            this.xrTableCell45.StylePriority.UseBorderColor = false;
            this.xrTableCell45.StylePriority.UseBorders = false;
            this.xrTableCell45.Text = "تاريخ الإستحقاق";
            this.xrTableCell45.Weight = 0.720005011800745D;
            // 
            // xrTableCell46
            // 
            this.xrTableCell46.BorderColor = System.Drawing.Color.LightGray;
            this.xrTableCell46.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell46.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[DueDate]")});
            this.xrTableCell46.Multiline = true;
            this.xrTableCell46.Name = "xrTableCell46";
            this.xrTableCell46.StylePriority.UseBorderColor = false;
            this.xrTableCell46.StylePriority.UseBorders = false;
            this.xrTableCell46.StylePriority.UseTextAlignment = false;
            this.xrTableCell46.Text = "xrTableCell46";
            this.xrTableCell46.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell46.TextFormatString = "{0:M/d/yyyy}";
            this.xrTableCell46.Weight = 1.05236820370413D;
            // 
            // xrTableCell47
            // 
            this.xrTableCell47.BorderColor = System.Drawing.Color.LightGray;
            this.xrTableCell47.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell47.Multiline = true;
            this.xrTableCell47.Name = "xrTableCell47";
            this.xrTableCell47.StylePriority.UseBorderColor = false;
            this.xrTableCell47.StylePriority.UseBorders = false;
            this.xrTableCell47.StylePriority.UseTextAlignment = false;
            this.xrTableCell47.Text = "Due Date";
            this.xrTableCell47.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell47.Weight = 0.57232642165097913D;
            // 
            // xrTableRow22
            // 
            this.xrTableRow22.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell48,
            this.xrTableCell49,
            this.xrTableCell50});
            this.xrTableRow22.Name = "xrTableRow22";
            this.xrTableRow22.Weight = 1D;
            // 
            // xrTableCell48
            // 
            this.xrTableCell48.BorderColor = System.Drawing.Color.LightGray;
            this.xrTableCell48.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell48.Multiline = true;
            this.xrTableCell48.Name = "xrTableCell48";
            this.xrTableCell48.StylePriority.UseBorderColor = false;
            this.xrTableCell48.StylePriority.UseBorders = false;
            this.xrTableCell48.Text = "تاريخ التوريد";
            this.xrTableCell48.Weight = 0.720005011800745D;
            // 
            // xrTableCell49
            // 
            this.xrTableCell49.BorderColor = System.Drawing.Color.LightGray;
            this.xrTableCell49.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell49.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[DeliveryDate]")});
            this.xrTableCell49.Multiline = true;
            this.xrTableCell49.Name = "xrTableCell49";
            this.xrTableCell49.StylePriority.UseBorderColor = false;
            this.xrTableCell49.StylePriority.UseBorders = false;
            this.xrTableCell49.StylePriority.UseTextAlignment = false;
            this.xrTableCell49.Text = "xrTableCell49";
            this.xrTableCell49.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell49.TextFormatString = "{0:M/d/yyyy}";
            this.xrTableCell49.Weight = 1.05236820370413D;
            // 
            // xrTableCell50
            // 
            this.xrTableCell50.BorderColor = System.Drawing.Color.LightGray;
            this.xrTableCell50.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell50.Multiline = true;
            this.xrTableCell50.Name = "xrTableCell50";
            this.xrTableCell50.StylePriority.UseBorderColor = false;
            this.xrTableCell50.StylePriority.UseBorders = false;
            this.xrTableCell50.StylePriority.UseTextAlignment = false;
            this.xrTableCell50.Text = "Delivery Date";
            this.xrTableCell50.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell50.Weight = 0.57232642165097913D;
            // 
            // xrTable4
            // 
            this.xrTable4.BackColor = System.Drawing.Color.Gray;
            this.xrTable4.BorderColor = System.Drawing.Color.Transparent;
            this.xrTable4.Font = new DevExpress.Drawing.DXFont("Arial", 15F);
            this.xrTable4.ForeColor = System.Drawing.Color.White;
            this.xrTable4.LocationFloat = new DevExpress.Utils.PointFloat(8.777832F, 119.5702F);
            this.xrTable4.Name = "xrTable4";
            this.xrTable4.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable4.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow4});
            this.xrTable4.SizeF = new System.Drawing.SizeF(798.2222F, 24.99997F);
            this.xrTable4.StylePriority.UseBackColor = false;
            this.xrTable4.StylePriority.UseBorderColor = false;
            this.xrTable4.StylePriority.UseFont = false;
            this.xrTable4.StylePriority.UseForeColor = false;
            // 
            // xrTableRow4
            // 
            this.xrTableRow4.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell33,
            this.xrTableCell34});
            this.xrTableRow4.Name = "xrTableRow4";
            this.xrTableRow4.Weight = 1D;
            // 
            // xrTableCell33
            // 
            this.xrTableCell33.BorderColor = System.Drawing.Color.Silver;
            this.xrTableCell33.Multiline = true;
            this.xrTableCell33.Name = "xrTableCell33";
            this.xrTableCell33.StylePriority.UseBorderColor = false;
            this.xrTableCell33.Text = "العميل ";
            this.xrTableCell33.Weight = 1.333905214038922D;
            // 
            // xrTableCell34
            // 
            this.xrTableCell34.BorderColor = System.Drawing.Color.Silver;
            this.xrTableCell34.Multiline = true;
            this.xrTableCell34.Name = "xrTableCell34";
            this.xrTableCell34.StylePriority.UseBorderColor = false;
            this.xrTableCell34.StylePriority.UseTextAlignment = false;
            this.xrTableCell34.Text = "Customer";
            this.xrTableCell34.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell34.Weight = 1.7115817992291944D;
            // 
            // xrLabel20
            // 
            this.xrLabel20.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel20.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel20.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel20.BorderWidth = 1F;
            this.xrLabel20.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel20.LocationFloat = new DevExpress.Utils.PointFloat(9.468323F, 144.5702F);
            this.xrLabel20.Multiline = true;
            this.xrLabel20.Name = "xrLabel20";
            this.xrLabel20.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel20.SizeF = new System.Drawing.SizeF(169.036F, 26.30682F);
            this.xrLabel20.StylePriority.UseBorderColor = false;
            this.xrLabel20.StylePriority.UseBorderDashStyle = false;
            this.xrLabel20.StylePriority.UseBorders = false;
            this.xrLabel20.StylePriority.UseBorderWidth = false;
            this.xrLabel20.StylePriority.UseFont = false;
            this.xrLabel20.StylePriority.UsePadding = false;
            this.xrLabel20.StylePriority.UseTextAlignment = false;
            this.xrLabel20.Text = "العميل ";
            this.xrLabel20.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel21
            // 
            this.xrLabel21.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel21.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel21.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel21.BorderWidth = 1F;
            this.xrLabel21.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CustomerArName]")});
            this.xrLabel21.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel21.LocationFloat = new DevExpress.Utils.PointFloat(178.5043F, 144.5702F);
            this.xrLabel21.Multiline = true;
            this.xrLabel21.Name = "xrLabel21";
            this.xrLabel21.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel21.SizeF = new System.Drawing.SizeF(247.0651F, 26.30682F);
            this.xrLabel21.StylePriority.UseBorderColor = false;
            this.xrLabel21.StylePriority.UseBorderDashStyle = false;
            this.xrLabel21.StylePriority.UseBorders = false;
            this.xrLabel21.StylePriority.UseBorderWidth = false;
            this.xrLabel21.StylePriority.UseFont = false;
            this.xrLabel21.StylePriority.UsePadding = false;
            this.xrLabel21.StylePriority.UseTextAlignment = false;
            this.xrLabel21.Text = "xrTableCell9";
            this.xrLabel21.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel22
            // 
            this.xrLabel22.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel22.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel22.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel22.BorderWidth = 1F;
            this.xrLabel22.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CustomerEnName]")});
            this.xrLabel22.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel22.LocationFloat = new DevExpress.Utils.PointFloat(425.5695F, 144.5702F);
            this.xrLabel22.Multiline = true;
            this.xrLabel22.Name = "xrLabel22";
            this.xrLabel22.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel22.SizeF = new System.Drawing.SizeF(247.0651F, 26.30682F);
            this.xrLabel22.StylePriority.UseBorderColor = false;
            this.xrLabel22.StylePriority.UseBorderDashStyle = false;
            this.xrLabel22.StylePriority.UseBorders = false;
            this.xrLabel22.StylePriority.UseBorderWidth = false;
            this.xrLabel22.StylePriority.UseFont = false;
            this.xrLabel22.StylePriority.UsePadding = false;
            this.xrLabel22.StylePriority.UseTextAlignment = false;
            this.xrLabel22.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel23
            // 
            this.xrLabel23.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel23.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel23.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel23.BorderWidth = 1F;
            this.xrLabel23.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel23.LocationFloat = new DevExpress.Utils.PointFloat(672.6346F, 144.5702F);
            this.xrLabel23.Multiline = true;
            this.xrLabel23.Name = "xrLabel23";
            this.xrLabel23.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel23.SizeF = new System.Drawing.SizeF(134.3654F, 26.30682F);
            this.xrLabel23.StylePriority.UseBorderColor = false;
            this.xrLabel23.StylePriority.UseBorderDashStyle = false;
            this.xrLabel23.StylePriority.UseBorders = false;
            this.xrLabel23.StylePriority.UseBorderWidth = false;
            this.xrLabel23.StylePriority.UseFont = false;
            this.xrLabel23.StylePriority.UsePadding = false;
            this.xrLabel23.StylePriority.UseTextAlignment = false;
            this.xrLabel23.Text = "Customer";
            this.xrLabel23.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrLabel24
            // 
            this.xrLabel24.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel24.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel24.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel24.BorderWidth = 1F;
            this.xrLabel24.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel24.LocationFloat = new DevExpress.Utils.PointFloat(9.468323F, 170.877F);
            this.xrLabel24.Multiline = true;
            this.xrLabel24.Name = "xrLabel24";
            this.xrLabel24.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel24.SizeF = new System.Drawing.SizeF(169.036F, 26.30682F);
            this.xrLabel24.StylePriority.UseBorderColor = false;
            this.xrLabel24.StylePriority.UseBorderDashStyle = false;
            this.xrLabel24.StylePriority.UseBorders = false;
            this.xrLabel24.StylePriority.UseBorderWidth = false;
            this.xrLabel24.StylePriority.UseFont = false;
            this.xrLabel24.StylePriority.UsePadding = false;
            this.xrLabel24.StylePriority.UseTextAlignment = false;
            this.xrLabel24.Text = "رقم المبنى";
            this.xrLabel24.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel25
            // 
            this.xrLabel25.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel25.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel25.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel25.BorderWidth = 1F;
            this.xrLabel25.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CustomerBuildingNo]")});
            this.xrLabel25.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel25.LocationFloat = new DevExpress.Utils.PointFloat(178.5043F, 170.877F);
            this.xrLabel25.Multiline = true;
            this.xrLabel25.Name = "xrLabel25";
            this.xrLabel25.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel25.SizeF = new System.Drawing.SizeF(247.0651F, 26.30682F);
            this.xrLabel25.StylePriority.UseBorderColor = false;
            this.xrLabel25.StylePriority.UseBorderDashStyle = false;
            this.xrLabel25.StylePriority.UseBorders = false;
            this.xrLabel25.StylePriority.UseBorderWidth = false;
            this.xrLabel25.StylePriority.UseFont = false;
            this.xrLabel25.StylePriority.UsePadding = false;
            this.xrLabel25.StylePriority.UseTextAlignment = false;
            this.xrLabel25.Text = "xrTableCell43";
            this.xrLabel25.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel27
            // 
            this.xrLabel27.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel27.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel27.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel27.BorderWidth = 1F;
            this.xrLabel27.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel27.LocationFloat = new DevExpress.Utils.PointFloat(672.6346F, 170.877F);
            this.xrLabel27.Multiline = true;
            this.xrLabel27.Name = "xrLabel27";
            this.xrLabel27.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel27.SizeF = new System.Drawing.SizeF(134.3654F, 26.30682F);
            this.xrLabel27.StylePriority.UseBorderColor = false;
            this.xrLabel27.StylePriority.UseBorderDashStyle = false;
            this.xrLabel27.StylePriority.UseBorders = false;
            this.xrLabel27.StylePriority.UseBorderWidth = false;
            this.xrLabel27.StylePriority.UseFont = false;
            this.xrLabel27.StylePriority.UsePadding = false;
            this.xrLabel27.StylePriority.UseTextAlignment = false;
            this.xrLabel27.Text = ".Building No";
            this.xrLabel27.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrLabel28
            // 
            this.xrLabel28.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel28.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel28.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel28.BorderWidth = 1F;
            this.xrLabel28.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel28.LocationFloat = new DevExpress.Utils.PointFloat(9.468323F, 197.1839F);
            this.xrLabel28.Multiline = true;
            this.xrLabel28.Name = "xrLabel28";
            this.xrLabel28.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel28.SizeF = new System.Drawing.SizeF(169.036F, 26.30682F);
            this.xrLabel28.StylePriority.UseBorderColor = false;
            this.xrLabel28.StylePriority.UseBorderDashStyle = false;
            this.xrLabel28.StylePriority.UseBorders = false;
            this.xrLabel28.StylePriority.UseBorderWidth = false;
            this.xrLabel28.StylePriority.UseFont = false;
            this.xrLabel28.StylePriority.UsePadding = false;
            this.xrLabel28.StylePriority.UseTextAlignment = false;
            this.xrLabel28.Text = "العنوان";
            this.xrLabel28.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel29
            // 
            this.xrLabel29.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel29.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel29.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel29.BorderWidth = 1F;
            this.xrLabel29.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CustomerAddress]")});
            this.xrLabel29.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel29.LocationFloat = new DevExpress.Utils.PointFloat(178.5043F, 197.1839F);
            this.xrLabel29.Multiline = true;
            this.xrLabel29.Name = "xrLabel29";
            this.xrLabel29.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel29.SizeF = new System.Drawing.SizeF(247.0651F, 26.30682F);
            this.xrLabel29.StylePriority.UseBorderColor = false;
            this.xrLabel29.StylePriority.UseBorderDashStyle = false;
            this.xrLabel29.StylePriority.UseBorders = false;
            this.xrLabel29.StylePriority.UseBorderWidth = false;
            this.xrLabel29.StylePriority.UseFont = false;
            this.xrLabel29.StylePriority.UsePadding = false;
            this.xrLabel29.StylePriority.UseTextAlignment = false;
            this.xrLabel29.Text = "xrTableCell46";
            this.xrLabel29.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel30
            // 
            this.xrLabel30.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel30.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel30.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel30.BorderWidth = 1F;
            this.xrLabel30.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel30.LocationFloat = new DevExpress.Utils.PointFloat(425.5695F, 197.1839F);
            this.xrLabel30.Multiline = true;
            this.xrLabel30.Name = "xrLabel30";
            this.xrLabel30.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel30.SizeF = new System.Drawing.SizeF(247.0651F, 26.30682F);
            this.xrLabel30.StylePriority.UseBorderColor = false;
            this.xrLabel30.StylePriority.UseBorderDashStyle = false;
            this.xrLabel30.StylePriority.UseBorders = false;
            this.xrLabel30.StylePriority.UseBorderWidth = false;
            this.xrLabel30.StylePriority.UseFont = false;
            this.xrLabel30.StylePriority.UsePadding = false;
            this.xrLabel30.StylePriority.UseTextAlignment = false;
            this.xrLabel30.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel31
            // 
            this.xrLabel31.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel31.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel31.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel31.BorderWidth = 1F;
            this.xrLabel31.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel31.LocationFloat = new DevExpress.Utils.PointFloat(672.6346F, 197.1839F);
            this.xrLabel31.Multiline = true;
            this.xrLabel31.Name = "xrLabel31";
            this.xrLabel31.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel31.SizeF = new System.Drawing.SizeF(134.3654F, 26.30682F);
            this.xrLabel31.StylePriority.UseBorderColor = false;
            this.xrLabel31.StylePriority.UseBorderDashStyle = false;
            this.xrLabel31.StylePriority.UseBorders = false;
            this.xrLabel31.StylePriority.UseBorderWidth = false;
            this.xrLabel31.StylePriority.UseFont = false;
            this.xrLabel31.StylePriority.UsePadding = false;
            this.xrLabel31.StylePriority.UseTextAlignment = false;
            this.xrLabel31.Text = "Address";
            this.xrLabel31.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrLabel32
            // 
            this.xrLabel32.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel32.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel32.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel32.BorderWidth = 1F;
            this.xrLabel32.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel32.LocationFloat = new DevExpress.Utils.PointFloat(9.468323F, 223.4907F);
            this.xrLabel32.Multiline = true;
            this.xrLabel32.Name = "xrLabel32";
            this.xrLabel32.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel32.SizeF = new System.Drawing.SizeF(169.036F, 26.30682F);
            this.xrLabel32.StylePriority.UseBorderColor = false;
            this.xrLabel32.StylePriority.UseBorderDashStyle = false;
            this.xrLabel32.StylePriority.UseBorders = false;
            this.xrLabel32.StylePriority.UseBorderWidth = false;
            this.xrLabel32.StylePriority.UseFont = false;
            this.xrLabel32.StylePriority.UsePadding = false;
            this.xrLabel32.StylePriority.UseTextAlignment = false;
            this.xrLabel32.Text = "الدولة";
            this.xrLabel32.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel33
            // 
            this.xrLabel33.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel33.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel33.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel33.BorderWidth = 1F;
            this.xrLabel33.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CustomerArCountry]")});
            this.xrLabel33.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel33.LocationFloat = new DevExpress.Utils.PointFloat(178.5043F, 223.4907F);
            this.xrLabel33.Multiline = true;
            this.xrLabel33.Name = "xrLabel33";
            this.xrLabel33.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel33.SizeF = new System.Drawing.SizeF(247.0651F, 26.30682F);
            this.xrLabel33.StylePriority.UseBorderColor = false;
            this.xrLabel33.StylePriority.UseBorderDashStyle = false;
            this.xrLabel33.StylePriority.UseBorders = false;
            this.xrLabel33.StylePriority.UseBorderWidth = false;
            this.xrLabel33.StylePriority.UseFont = false;
            this.xrLabel33.StylePriority.UsePadding = false;
            this.xrLabel33.StylePriority.UseTextAlignment = false;
            this.xrLabel33.Text = "xrTableCell60";
            this.xrLabel33.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel34
            // 
            this.xrLabel34.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel34.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel34.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel34.BorderWidth = 1F;
            this.xrLabel34.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CustomerEnCountry]")});
            this.xrLabel34.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel34.LocationFloat = new DevExpress.Utils.PointFloat(425.5695F, 223.4907F);
            this.xrLabel34.Multiline = true;
            this.xrLabel34.Name = "xrLabel34";
            this.xrLabel34.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel34.SizeF = new System.Drawing.SizeF(247.0651F, 26.30682F);
            this.xrLabel34.StylePriority.UseBorderColor = false;
            this.xrLabel34.StylePriority.UseBorderDashStyle = false;
            this.xrLabel34.StylePriority.UseBorders = false;
            this.xrLabel34.StylePriority.UseBorderWidth = false;
            this.xrLabel34.StylePriority.UseFont = false;
            this.xrLabel34.StylePriority.UsePadding = false;
            this.xrLabel34.StylePriority.UseTextAlignment = false;
            this.xrLabel34.Text = "xrTableCell94";
            this.xrLabel34.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel35
            // 
            this.xrLabel35.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel35.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel35.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel35.BorderWidth = 1F;
            this.xrLabel35.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel35.LocationFloat = new DevExpress.Utils.PointFloat(672.6346F, 223.4907F);
            this.xrLabel35.Multiline = true;
            this.xrLabel35.Name = "xrLabel35";
            this.xrLabel35.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel35.SizeF = new System.Drawing.SizeF(134.3654F, 26.30682F);
            this.xrLabel35.StylePriority.UseBorderColor = false;
            this.xrLabel35.StylePriority.UseBorderDashStyle = false;
            this.xrLabel35.StylePriority.UseBorders = false;
            this.xrLabel35.StylePriority.UseBorderWidth = false;
            this.xrLabel35.StylePriority.UseFont = false;
            this.xrLabel35.StylePriority.UsePadding = false;
            this.xrLabel35.StylePriority.UseTextAlignment = false;
            this.xrLabel35.Text = "Country";
            this.xrLabel35.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrLabel36
            // 
            this.xrLabel36.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel36.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel36.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel36.BorderWidth = 1F;
            this.xrLabel36.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel36.LocationFloat = new DevExpress.Utils.PointFloat(9.468323F, 249.7975F);
            this.xrLabel36.Multiline = true;
            this.xrLabel36.Name = "xrLabel36";
            this.xrLabel36.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel36.SizeF = new System.Drawing.SizeF(169.036F, 26.30682F);
            this.xrLabel36.StylePriority.UseBorderColor = false;
            this.xrLabel36.StylePriority.UseBorderDashStyle = false;
            this.xrLabel36.StylePriority.UseBorders = false;
            this.xrLabel36.StylePriority.UseBorderWidth = false;
            this.xrLabel36.StylePriority.UseFont = false;
            this.xrLabel36.StylePriority.UsePadding = false;
            this.xrLabel36.StylePriority.UseTextAlignment = false;
            this.xrLabel36.Text = "المدينة";
            this.xrLabel36.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel37
            // 
            this.xrLabel37.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel37.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel37.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel37.BorderWidth = 1F;
            this.xrLabel37.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CustomerArCity]")});
            this.xrLabel37.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel37.LocationFloat = new DevExpress.Utils.PointFloat(178.5043F, 249.7975F);
            this.xrLabel37.Multiline = true;
            this.xrLabel37.Name = "xrLabel37";
            this.xrLabel37.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel37.SizeF = new System.Drawing.SizeF(247.0651F, 26.30682F);
            this.xrLabel37.StylePriority.UseBorderColor = false;
            this.xrLabel37.StylePriority.UseBorderDashStyle = false;
            this.xrLabel37.StylePriority.UseBorders = false;
            this.xrLabel37.StylePriority.UseBorderWidth = false;
            this.xrLabel37.StylePriority.UseFont = false;
            this.xrLabel37.StylePriority.UsePadding = false;
            this.xrLabel37.StylePriority.UseTextAlignment = false;
            this.xrLabel37.Text = "xrTableCell63";
            this.xrLabel37.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel38
            // 
            this.xrLabel38.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel38.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel38.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel38.BorderWidth = 1F;
            this.xrLabel38.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CustomerEnCity]")});
            this.xrLabel38.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel38.LocationFloat = new DevExpress.Utils.PointFloat(425.5695F, 249.7975F);
            this.xrLabel38.Multiline = true;
            this.xrLabel38.Name = "xrLabel38";
            this.xrLabel38.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel38.SizeF = new System.Drawing.SizeF(247.0651F, 26.30682F);
            this.xrLabel38.StylePriority.UseBorderColor = false;
            this.xrLabel38.StylePriority.UseBorderDashStyle = false;
            this.xrLabel38.StylePriority.UseBorders = false;
            this.xrLabel38.StylePriority.UseBorderWidth = false;
            this.xrLabel38.StylePriority.UseFont = false;
            this.xrLabel38.StylePriority.UsePadding = false;
            this.xrLabel38.StylePriority.UseTextAlignment = false;
            this.xrLabel38.Text = "xrTableCell95";
            this.xrLabel38.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel39
            // 
            this.xrLabel39.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel39.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel39.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel39.BorderWidth = 1F;
            this.xrLabel39.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel39.LocationFloat = new DevExpress.Utils.PointFloat(672.6346F, 249.7975F);
            this.xrLabel39.Multiline = true;
            this.xrLabel39.Name = "xrLabel39";
            this.xrLabel39.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel39.SizeF = new System.Drawing.SizeF(134.3654F, 26.30682F);
            this.xrLabel39.StylePriority.UseBorderColor = false;
            this.xrLabel39.StylePriority.UseBorderDashStyle = false;
            this.xrLabel39.StylePriority.UseBorders = false;
            this.xrLabel39.StylePriority.UseBorderWidth = false;
            this.xrLabel39.StylePriority.UseFont = false;
            this.xrLabel39.StylePriority.UsePadding = false;
            this.xrLabel39.StylePriority.UseTextAlignment = false;
            this.xrLabel39.Text = "City";
            this.xrLabel39.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrLabel40
            // 
            this.xrLabel40.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel40.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel40.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel40.BorderWidth = 1F;
            this.xrLabel40.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel40.LocationFloat = new DevExpress.Utils.PointFloat(9.468323F, 276.1043F);
            this.xrLabel40.Multiline = true;
            this.xrLabel40.Name = "xrLabel40";
            this.xrLabel40.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel40.SizeF = new System.Drawing.SizeF(169.036F, 26.30682F);
            this.xrLabel40.StylePriority.UseBorderColor = false;
            this.xrLabel40.StylePriority.UseBorderDashStyle = false;
            this.xrLabel40.StylePriority.UseBorders = false;
            this.xrLabel40.StylePriority.UseBorderWidth = false;
            this.xrLabel40.StylePriority.UseFont = false;
            this.xrLabel40.StylePriority.UsePadding = false;
            this.xrLabel40.StylePriority.UseTextAlignment = false;
            this.xrLabel40.Text = "رمز  بريدى ";
            this.xrLabel40.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel41
            // 
            this.xrLabel41.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel41.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel41.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel41.BorderWidth = 1F;
            this.xrLabel41.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CustomerPostOfficeBox]")});
            this.xrLabel41.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel41.LocationFloat = new DevExpress.Utils.PointFloat(178.5043F, 276.1043F);
            this.xrLabel41.Multiline = true;
            this.xrLabel41.Name = "xrLabel41";
            this.xrLabel41.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel41.SizeF = new System.Drawing.SizeF(247.0651F, 26.30682F);
            this.xrLabel41.StylePriority.UseBorderColor = false;
            this.xrLabel41.StylePriority.UseBorderDashStyle = false;
            this.xrLabel41.StylePriority.UseBorders = false;
            this.xrLabel41.StylePriority.UseBorderWidth = false;
            this.xrLabel41.StylePriority.UseFont = false;
            this.xrLabel41.StylePriority.UsePadding = false;
            this.xrLabel41.StylePriority.UseTextAlignment = false;
            this.xrLabel41.Text = "xrTableCell66";
            this.xrLabel41.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel43
            // 
            this.xrLabel43.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel43.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel43.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel43.BorderWidth = 1F;
            this.xrLabel43.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel43.LocationFloat = new DevExpress.Utils.PointFloat(672.6346F, 276.1043F);
            this.xrLabel43.Multiline = true;
            this.xrLabel43.Name = "xrLabel43";
            this.xrLabel43.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel43.SizeF = new System.Drawing.SizeF(134.3654F, 26.30682F);
            this.xrLabel43.StylePriority.UseBorderColor = false;
            this.xrLabel43.StylePriority.UseBorderDashStyle = false;
            this.xrLabel43.StylePriority.UseBorders = false;
            this.xrLabel43.StylePriority.UseBorderWidth = false;
            this.xrLabel43.StylePriority.UseFont = false;
            this.xrLabel43.StylePriority.UsePadding = false;
            this.xrLabel43.StylePriority.UseTextAlignment = false;
            this.xrLabel43.Text = "P.O.Box";
            this.xrLabel43.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrLabel44
            // 
            this.xrLabel44.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel44.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel44.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel44.BorderWidth = 1F;
            this.xrLabel44.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel44.LocationFloat = new DevExpress.Utils.PointFloat(9.468323F, 302.4111F);
            this.xrLabel44.Multiline = true;
            this.xrLabel44.Name = "xrLabel44";
            this.xrLabel44.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel44.SizeF = new System.Drawing.SizeF(169.036F, 26.30682F);
            this.xrLabel44.StylePriority.UseBorderColor = false;
            this.xrLabel44.StylePriority.UseBorderDashStyle = false;
            this.xrLabel44.StylePriority.UseBorders = false;
            this.xrLabel44.StylePriority.UseBorderWidth = false;
            this.xrLabel44.StylePriority.UseFont = false;
            this.xrLabel44.StylePriority.UsePadding = false;
            this.xrLabel44.StylePriority.UseTextAlignment = false;
            this.xrLabel44.Text = "الرقم الضريبي ";
            this.xrLabel44.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel45
            // 
            this.xrLabel45.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel45.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel45.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel45.BorderWidth = 1F;
            this.xrLabel45.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CustomerTaxNumber]")});
            this.xrLabel45.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel45.LocationFloat = new DevExpress.Utils.PointFloat(178.5043F, 302.4111F);
            this.xrLabel45.Multiline = true;
            this.xrLabel45.Name = "xrLabel45";
            this.xrLabel45.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel45.SizeF = new System.Drawing.SizeF(247.0651F, 26.30682F);
            this.xrLabel45.StylePriority.UseBorderColor = false;
            this.xrLabel45.StylePriority.UseBorderDashStyle = false;
            this.xrLabel45.StylePriority.UseBorders = false;
            this.xrLabel45.StylePriority.UseBorderWidth = false;
            this.xrLabel45.StylePriority.UseFont = false;
            this.xrLabel45.StylePriority.UsePadding = false;
            this.xrLabel45.StylePriority.UseTextAlignment = false;
            this.xrLabel45.Text = "xrTableCell72";
            this.xrLabel45.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel47
            // 
            this.xrLabel47.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel47.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel47.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel47.BorderWidth = 1F;
            this.xrLabel47.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel47.LocationFloat = new DevExpress.Utils.PointFloat(672.6346F, 302.4111F);
            this.xrLabel47.Multiline = true;
            this.xrLabel47.Name = "xrLabel47";
            this.xrLabel47.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel47.SizeF = new System.Drawing.SizeF(134.3654F, 26.30682F);
            this.xrLabel47.StylePriority.UseBorderColor = false;
            this.xrLabel47.StylePriority.UseBorderDashStyle = false;
            this.xrLabel47.StylePriority.UseBorders = false;
            this.xrLabel47.StylePriority.UseBorderWidth = false;
            this.xrLabel47.StylePriority.UseFont = false;
            this.xrLabel47.StylePriority.UsePadding = false;
            this.xrLabel47.StylePriority.UseTextAlignment = false;
            this.xrLabel47.Text = ".VAT No";
            this.xrLabel47.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrLabel74
            // 
            this.xrLabel74.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel74.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel74.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel74.BorderWidth = 1F;
            this.xrLabel74.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel74.LocationFloat = new DevExpress.Utils.PointFloat(9.468323F, 328.718F);
            this.xrLabel74.Multiline = true;
            this.xrLabel74.Name = "xrLabel74";
            this.xrLabel74.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel74.SizeF = new System.Drawing.SizeF(169.036F, 26.30682F);
            this.xrLabel74.StylePriority.UseBorderColor = false;
            this.xrLabel74.StylePriority.UseBorderDashStyle = false;
            this.xrLabel74.StylePriority.UseBorders = false;
            this.xrLabel74.StylePriority.UseBorderWidth = false;
            this.xrLabel74.StylePriority.UseFont = false;
            this.xrLabel74.StylePriority.UsePadding = false;
            this.xrLabel74.StylePriority.UseTextAlignment = false;
            this.xrLabel74.Text = "السجل التجاري";
            this.xrLabel74.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel75
            // 
            this.xrLabel75.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel75.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel75.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel75.BorderWidth = 1F;
            this.xrLabel75.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CustomerRegistrationNo]")});
            this.xrLabel75.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel75.LocationFloat = new DevExpress.Utils.PointFloat(178.5043F, 328.718F);
            this.xrLabel75.Multiline = true;
            this.xrLabel75.Name = "xrLabel75";
            this.xrLabel75.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel75.SizeF = new System.Drawing.SizeF(247.0651F, 26.30682F);
            this.xrLabel75.StylePriority.UseBorderColor = false;
            this.xrLabel75.StylePriority.UseBorderDashStyle = false;
            this.xrLabel75.StylePriority.UseBorders = false;
            this.xrLabel75.StylePriority.UseBorderWidth = false;
            this.xrLabel75.StylePriority.UseFont = false;
            this.xrLabel75.StylePriority.UsePadding = false;
            this.xrLabel75.StylePriority.UseTextAlignment = false;
            this.xrLabel75.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel76
            // 
            this.xrLabel76.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel76.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel76.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel76.BorderWidth = 1F;
            this.xrLabel76.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CustomerRegistrationNo]")});
            this.xrLabel76.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel76.LocationFloat = new DevExpress.Utils.PointFloat(425.5695F, 328.718F);
            this.xrLabel76.Multiline = true;
            this.xrLabel76.Name = "xrLabel76";
            this.xrLabel76.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel76.SizeF = new System.Drawing.SizeF(247.0651F, 26.30682F);
            this.xrLabel76.StylePriority.UseBorderColor = false;
            this.xrLabel76.StylePriority.UseBorderDashStyle = false;
            this.xrLabel76.StylePriority.UseBorders = false;
            this.xrLabel76.StylePriority.UseBorderWidth = false;
            this.xrLabel76.StylePriority.UseFont = false;
            this.xrLabel76.StylePriority.UsePadding = false;
            this.xrLabel76.StylePriority.UseTextAlignment = false;
            this.xrLabel76.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel77
            // 
            this.xrLabel77.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel77.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel77.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel77.BorderWidth = 1F;
            this.xrLabel77.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel77.LocationFloat = new DevExpress.Utils.PointFloat(672.6346F, 328.718F);
            this.xrLabel77.Multiline = true;
            this.xrLabel77.Name = "xrLabel77";
            this.xrLabel77.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel77.SizeF = new System.Drawing.SizeF(134.3654F, 26.30682F);
            this.xrLabel77.StylePriority.UseBorderColor = false;
            this.xrLabel77.StylePriority.UseBorderDashStyle = false;
            this.xrLabel77.StylePriority.UseBorders = false;
            this.xrLabel77.StylePriority.UseBorderWidth = false;
            this.xrLabel77.StylePriority.UseFont = false;
            this.xrLabel77.StylePriority.UsePadding = false;
            this.xrLabel77.StylePriority.UseTextAlignment = false;
            this.xrLabel77.Text = ".CR No";
            this.xrLabel77.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrLabel48
            // 
            this.xrLabel48.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel48.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel48.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel48.BorderWidth = 1F;
            this.xrLabel48.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel48.LocationFloat = new DevExpress.Utils.PointFloat(9.468323F, 355.0248F);
            this.xrLabel48.Multiline = true;
            this.xrLabel48.Name = "xrLabel48";
            this.xrLabel48.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel48.SizeF = new System.Drawing.SizeF(169.036F, 26.30682F);
            this.xrLabel48.StylePriority.UseBorderColor = false;
            this.xrLabel48.StylePriority.UseBorderDashStyle = false;
            this.xrLabel48.StylePriority.UseBorders = false;
            this.xrLabel48.StylePriority.UseBorderWidth = false;
            this.xrLabel48.StylePriority.UseFont = false;
            this.xrLabel48.StylePriority.UsePadding = false;
            this.xrLabel48.StylePriority.UseTextAlignment = false;
            this.xrLabel48.Text = "الهاتف";
            this.xrLabel48.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel49
            // 
            this.xrLabel49.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel49.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel49.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel49.BorderWidth = 1F;
            this.xrLabel49.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CustomerMobile]")});
            this.xrLabel49.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel49.LocationFloat = new DevExpress.Utils.PointFloat(178.5043F, 355.0248F);
            this.xrLabel49.Multiline = true;
            this.xrLabel49.Name = "xrLabel49";
            this.xrLabel49.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel49.SizeF = new System.Drawing.SizeF(247.0651F, 26.30682F);
            this.xrLabel49.StylePriority.UseBorderColor = false;
            this.xrLabel49.StylePriority.UseBorderDashStyle = false;
            this.xrLabel49.StylePriority.UseBorders = false;
            this.xrLabel49.StylePriority.UseBorderWidth = false;
            this.xrLabel49.StylePriority.UseFont = false;
            this.xrLabel49.StylePriority.UsePadding = false;
            this.xrLabel49.StylePriority.UseTextAlignment = false;
            this.xrLabel49.Text = "xrTableCell69";
            this.xrLabel49.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel51
            // 
            this.xrLabel51.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel51.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel51.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel51.BorderWidth = 1F;
            this.xrLabel51.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel51.LocationFloat = new DevExpress.Utils.PointFloat(672.6346F, 355.0248F);
            this.xrLabel51.Multiline = true;
            this.xrLabel51.Name = "xrLabel51";
            this.xrLabel51.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel51.SizeF = new System.Drawing.SizeF(134.3654F, 26.30682F);
            this.xrLabel51.StylePriority.UseBorderColor = false;
            this.xrLabel51.StylePriority.UseBorderDashStyle = false;
            this.xrLabel51.StylePriority.UseBorders = false;
            this.xrLabel51.StylePriority.UseBorderWidth = false;
            this.xrLabel51.StylePriority.UseFont = false;
            this.xrLabel51.StylePriority.UsePadding = false;
            this.xrLabel51.StylePriority.UseTextAlignment = false;
            this.xrLabel51.Text = "Mobile";
            this.xrLabel51.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrLabel52
            // 
            this.xrLabel52.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel52.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel52.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel52.BorderWidth = 1F;
            this.xrLabel52.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel52.LocationFloat = new DevExpress.Utils.PointFloat(9.468323F, 381.3316F);
            this.xrLabel52.Multiline = true;
            this.xrLabel52.Name = "xrLabel52";
            this.xrLabel52.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel52.SizeF = new System.Drawing.SizeF(169.036F, 26.30682F);
            this.xrLabel52.StylePriority.UseBorderColor = false;
            this.xrLabel52.StylePriority.UseBorderDashStyle = false;
            this.xrLabel52.StylePriority.UseBorders = false;
            this.xrLabel52.StylePriority.UseBorderWidth = false;
            this.xrLabel52.StylePriority.UseFont = false;
            this.xrLabel52.StylePriority.UsePadding = false;
            this.xrLabel52.StylePriority.UseTextAlignment = false;
            this.xrLabel52.Text = "رقم إضافى";
            this.xrLabel52.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel53
            // 
            this.xrLabel53.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel53.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel53.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
            this.xrLabel53.BorderWidth = 1F;
            this.xrLabel53.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CustomerPhoneNo]")});
            this.xrLabel53.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel53.LocationFloat = new DevExpress.Utils.PointFloat(178.5043F, 381.3316F);
            this.xrLabel53.Multiline = true;
            this.xrLabel53.Name = "xrLabel53";
            this.xrLabel53.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel53.SizeF = new System.Drawing.SizeF(247.0651F, 26.30682F);
            this.xrLabel53.StylePriority.UseBorderColor = false;
            this.xrLabel53.StylePriority.UseBorderDashStyle = false;
            this.xrLabel53.StylePriority.UseBorders = false;
            this.xrLabel53.StylePriority.UseBorderWidth = false;
            this.xrLabel53.StylePriority.UseFont = false;
            this.xrLabel53.StylePriority.UsePadding = false;
            this.xrLabel53.StylePriority.UseTextAlignment = false;
            this.xrLabel53.Text = "xrTableCell49";
            this.xrLabel53.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel55
            // 
            this.xrLabel55.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel55.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel55.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel55.BorderWidth = 1F;
            this.xrLabel55.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel55.LocationFloat = new DevExpress.Utils.PointFloat(672.6346F, 381.3316F);
            this.xrLabel55.Multiline = true;
            this.xrLabel55.Name = "xrLabel55";
            this.xrLabel55.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel55.SizeF = new System.Drawing.SizeF(134.3654F, 26.30682F);
            this.xrLabel55.StylePriority.UseBorderColor = false;
            this.xrLabel55.StylePriority.UseBorderDashStyle = false;
            this.xrLabel55.StylePriority.UseBorders = false;
            this.xrLabel55.StylePriority.UseBorderWidth = false;
            this.xrLabel55.StylePriority.UseFont = false;
            this.xrLabel55.StylePriority.UsePadding = false;
            this.xrLabel55.StylePriority.UseTextAlignment = false;
            this.xrLabel55.Text = ".Additional No";
            this.xrLabel55.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrBarCode1
            // 
            this.xrBarCode1.AutoModule = true;
            this.xrBarCode1.LocationFloat = new DevExpress.Utils.PointFloat(348.5F, 105F);
            this.xrBarCode1.Name = "xrBarCode1";
            this.xrBarCode1.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 2, 2, 100F);
            this.xrBarCode1.ShowText = false;
            this.xrBarCode1.SizeF = new System.Drawing.SizeF(110F, 110F);
            qrCodeGenerator1.CompactionMode = DevExpress.XtraPrinting.BarCode.QRCodeCompactionMode.Byte;
            qrCodeGenerator1.ErrorCorrectionLevel = DevExpress.XtraPrinting.BarCode.QRCodeErrorCorrectionLevel.H;
            qrCodeGenerator1.Version = DevExpress.XtraPrinting.BarCode.QRCodeVersion.Version11;
            this.xrBarCode1.Symbology = qrCodeGenerator1;
            // 
            // xrLine1
            // 
            this.xrLine1.LocationFloat = new DevExpress.Utils.PointFloat(20.00011F, 274F);
            this.xrLine1.Name = "xrLine1";
            this.xrLine1.SizeF = new System.Drawing.SizeF(787F, 12.58336F);
            // 
            // xrTable8
            // 
            this.xrTable8.LocationFloat = new DevExpress.Utils.PointFloat(9.916687F, 10F);
            this.xrTable8.Name = "xrTable8";
            this.xrTable8.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable8.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow10,
            this.xrTableRow11,
            this.xrTableRow12,
            this.xrTableRow13});
            this.xrTable8.SizeF = new System.Drawing.SizeF(250F, 122.2144F);
            // 
            // xrTableRow10
            // 
            this.xrTableRow10.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell21});
            this.xrTableRow10.Name = "xrTableRow10";
            this.xrTableRow10.Weight = 1D;
            // 
            // xrTableCell21
            // 
            this.xrTableCell21.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CompanyArName]")});
            this.xrTableCell21.Multiline = true;
            this.xrTableCell21.Name = "xrTableCell21";
            this.xrTableCell21.Text = "xrTableCell9";
            this.xrTableCell21.Weight = 1D;
            // 
            // xrTableRow11
            // 
            this.xrTableRow11.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell27});
            this.xrTableRow11.Name = "xrTableRow11";
            this.xrTableRow11.Weight = 1D;
            // 
            // xrTableCell27
            // 
            this.xrTableCell27.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CountryArName]")});
            this.xrTableCell27.Multiline = true;
            this.xrTableCell27.Name = "xrTableCell27";
            this.xrTableCell27.Text = "xrTableCell25";
            this.xrTableCell27.Weight = 1D;
            // 
            // xrTableRow12
            // 
            this.xrTableRow12.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell31});
            this.xrTableRow12.Name = "xrTableRow12";
            this.xrTableRow12.Weight = 1D;
            // 
            // xrTableCell31
            // 
            this.xrTableCell31.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CityArName]")});
            this.xrTableCell31.Multiline = true;
            this.xrTableCell31.Name = "xrTableCell31";
            this.xrTableCell31.Text = "xrTableCell28";
            this.xrTableCell31.Weight = 1D;
            // 
            // xrTableRow13
            // 
            this.xrTableRow13.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell22});
            this.xrTableRow13.Name = "xrTableRow13";
            this.xrTableRow13.Weight = 1D;
            // 
            // xrTableCell22
            // 
            this.xrTableCell22.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[StreetArName]")});
            this.xrTableCell22.Multiline = true;
            this.xrTableCell22.Name = "xrTableCell22";
            this.xrTableCell22.Text = "xrTableCell22";
            this.xrTableCell22.Weight = 1D;
            // 
            // xrTable7
            // 
            this.xrTable7.LocationFloat = new DevExpress.Utils.PointFloat(547F, 9.999996F);
            this.xrTable7.Name = "xrTable7";
            this.xrTable7.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable7.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow7,
            this.xrTableRow8,
            this.xrTableRow9,
            this.xrTableRow14});
            this.xrTable7.SizeF = new System.Drawing.SizeF(250F, 122.2144F);
            this.xrTable7.StylePriority.UseTextAlignment = false;
            this.xrTable7.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrTableRow7
            // 
            this.xrTableRow7.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell23});
            this.xrTableRow7.Name = "xrTableRow7";
            this.xrTableRow7.Weight = 1D;
            // 
            // xrTableCell23
            // 
            this.xrTableCell23.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CompanyEnName]")});
            this.xrTableCell23.Multiline = true;
            this.xrTableCell23.Name = "xrTableCell23";
            this.xrTableCell23.Text = "xrTableCell17";
            this.xrTableCell23.Weight = 1D;
            // 
            // xrTableRow8
            // 
            this.xrTableRow8.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell26});
            this.xrTableRow8.Name = "xrTableRow8";
            this.xrTableRow8.Weight = 1D;
            // 
            // xrTableCell26
            // 
            this.xrTableCell26.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CountryEnName]")});
            this.xrTableCell26.Multiline = true;
            this.xrTableCell26.Name = "xrTableCell26";
            this.xrTableCell26.Text = "xrTableCell26";
            this.xrTableCell26.Weight = 1D;
            // 
            // xrTableRow9
            // 
            this.xrTableRow9.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell29});
            this.xrTableRow9.Name = "xrTableRow9";
            this.xrTableRow9.Weight = 1D;
            // 
            // xrTableCell29
            // 
            this.xrTableCell29.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CityEnName]")});
            this.xrTableCell29.Multiline = true;
            this.xrTableCell29.Name = "xrTableCell29";
            this.xrTableCell29.Text = "xrTableCell29";
            this.xrTableCell29.Weight = 1D;
            // 
            // xrTableRow14
            // 
            this.xrTableRow14.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell24});
            this.xrTableRow14.Name = "xrTableRow14";
            this.xrTableRow14.Weight = 1D;
            // 
            // xrTableCell24
            // 
            this.xrTableCell24.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[StreetEnName]")});
            this.xrTableCell24.Multiline = true;
            this.xrTableCell24.Name = "xrTableCell24";
            this.xrTableCell24.Text = "xrTableCell24";
            this.xrTableCell24.Weight = 1D;
            // 
            // xrPictureBox1
            // 
            this.xrPictureBox1.ImageUrl = "D:\\Desk2023\\Geniuses\\Geniuseslogo.jpeg";
            this.xrPictureBox1.LocationFloat = new DevExpress.Utils.PointFloat(336F, 10F);
            this.xrPictureBox1.Name = "xrPictureBox1";
            this.xrPictureBox1.SizeF = new System.Drawing.SizeF(135F, 85F);
            this.xrPictureBox1.Sizing = DevExpress.XtraPrinting.ImageSizeMode.ZoomImage;
            this.xrPictureBox1.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrPictureBox1_BeforePrint);
            // 
            // ExtendedTotal
            // 
            this.ExtendedTotal.DataMember = "ServiceInvoice_Get.ServiceInvoice_GetQuery";
            this.ExtendedTotal.Expression = "[Price] * [Qty]";
            this.ExtendedTotal.Name = "ExtendedTotal";
            // 
            // ReportFooter
            // 
            this.ReportFooter.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrTable11,
            this.xrLabel54,
            this.xrLabel57,
            this.xrLabel58,
            this.xrLabel59,
            this.xrLabel60,
            this.xrLabel61,
            this.xrLabel62,
            this.xrLabel63,
            this.xrLabel64,
            this.xrLabel65,
            this.xrLabel66,
            this.xrLabel67,
            this.xrLabel68,
            this.xrLabel69,
            this.xrLabel70});
            this.ReportFooter.HeightF = 203.0184F;
            this.ReportFooter.Name = "ReportFooter";
            this.ReportFooter.StylePriority.UseTextAlignment = false;
            this.ReportFooter.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrTable11
            // 
            this.xrTable11.BackColor = System.Drawing.Color.Gray;
            this.xrTable11.BorderColor = System.Drawing.Color.Transparent;
            this.xrTable11.Font = new DevExpress.Drawing.DXFont("Arial", 15F);
            this.xrTable11.ForeColor = System.Drawing.Color.White;
            this.xrTable11.LocationFloat = new DevExpress.Utils.PointFloat(9.385133F, 0F);
            this.xrTable11.Name = "xrTable11";
            this.xrTable11.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable11.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow28});
            this.xrTable11.SizeF = new System.Drawing.SizeF(787.6149F, 28.16454F);
            this.xrTable11.StylePriority.UseBackColor = false;
            this.xrTable11.StylePriority.UseBorderColor = false;
            this.xrTable11.StylePriority.UseFont = false;
            this.xrTable11.StylePriority.UseForeColor = false;
            // 
            // xrTableRow28
            // 
            this.xrTableRow28.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell74,
            this.xrTableCell75});
            this.xrTableRow28.Name = "xrTableRow28";
            this.xrTableRow28.Weight = 1D;
            // 
            // xrTableCell74
            // 
            this.xrTableCell74.BorderColor = System.Drawing.Color.Silver;
            this.xrTableCell74.Multiline = true;
            this.xrTableCell74.Name = "xrTableCell74";
            this.xrTableCell74.StylePriority.UseBorderColor = false;
            this.xrTableCell74.Text = "المبالغ الإجمالية";
            this.xrTableCell74.Weight = 1.3332628549440733D;
            // 
            // xrTableCell75
            // 
            this.xrTableCell75.BorderColor = System.Drawing.Color.Silver;
            this.xrTableCell75.Multiline = true;
            this.xrTableCell75.Name = "xrTableCell75";
            this.xrTableCell75.StylePriority.UseBorderColor = false;
            this.xrTableCell75.StylePriority.UseTextAlignment = false;
            this.xrTableCell75.Text = "Total Amounts";
            this.xrTableCell75.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell75.Weight = 1.6667371450559267D;
            // 
            // xrLabel54
            // 
            this.xrLabel54.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel54.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel54.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel54.LocationFloat = new DevExpress.Utils.PointFloat(9.468434F, 28.16457F);
            this.xrLabel54.Multiline = true;
            this.xrLabel54.Name = "xrLabel54";
            this.xrLabel54.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel54.SizeF = new System.Drawing.SizeF(241.859F, 25.05298F);
            this.xrLabel54.StylePriority.UseBorderColor = false;
            this.xrLabel54.StylePriority.UseBorders = false;
            this.xrLabel54.StylePriority.UseFont = false;
            this.xrLabel54.StylePriority.UsePadding = false;
            this.xrLabel54.StylePriority.UseTextAlignment = false;
            this.xrLabel54.Text = "الإجمالى ( غير شامل ضريبة القيمة المضافة )";
            this.xrLabel54.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel57
            // 
            this.xrLabel57.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel57.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel57.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Total]")});
            this.xrLabel57.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel57.LocationFloat = new DevExpress.Utils.PointFloat(251.3274F, 28.16457F);
            this.xrLabel57.Multiline = true;
            this.xrLabel57.Name = "xrLabel57";
            this.xrLabel57.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel57.SizeF = new System.Drawing.SizeF(353.5041F, 25.05298F);
            this.xrLabel57.StylePriority.UseBorderColor = false;
            this.xrLabel57.StylePriority.UseBorders = false;
            this.xrLabel57.StylePriority.UseFont = false;
            this.xrLabel57.StylePriority.UsePadding = false;
            this.xrLabel57.StylePriority.UseTextAlignment = false;
            this.xrLabel57.Text = "xrTableCell9";
            this.xrLabel57.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabel57.TextFormatString = "{0:#.00}";
            // 
            // xrLabel58
            // 
            this.xrLabel58.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel58.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel58.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel58.LocationFloat = new DevExpress.Utils.PointFloat(604.8315F, 28.16457F);
            this.xrLabel58.Multiline = true;
            this.xrLabel58.Name = "xrLabel58";
            this.xrLabel58.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel58.SizeF = new System.Drawing.SizeF(192.2518F, 25.05298F);
            this.xrLabel58.StylePriority.UseBorderColor = false;
            this.xrLabel58.StylePriority.UseBorders = false;
            this.xrLabel58.StylePriority.UseFont = false;
            this.xrLabel58.StylePriority.UsePadding = false;
            this.xrLabel58.StylePriority.UseTextAlignment = false;
            this.xrLabel58.Text = "Total (Excluded VAT) ";
            this.xrLabel58.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrLabel59
            // 
            this.xrLabel59.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel59.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel59.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel59.LocationFloat = new DevExpress.Utils.PointFloat(9.468434F, 53.21755F);
            this.xrLabel59.Multiline = true;
            this.xrLabel59.Name = "xrLabel59";
            this.xrLabel59.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel59.SizeF = new System.Drawing.SizeF(241.859F, 25.05298F);
            this.xrLabel59.StylePriority.UseBorderColor = false;
            this.xrLabel59.StylePriority.UseBorders = false;
            this.xrLabel59.StylePriority.UseFont = false;
            this.xrLabel59.StylePriority.UsePadding = false;
            this.xrLabel59.StylePriority.UseTextAlignment = false;
            this.xrLabel59.Text = "مجموع الخصومات";
            this.xrLabel59.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel60
            // 
            this.xrLabel60.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel60.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel60.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[VoucherDiscountValue]")});
            this.xrLabel60.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel60.LocationFloat = new DevExpress.Utils.PointFloat(251.3274F, 53.21755F);
            this.xrLabel60.Multiline = true;
            this.xrLabel60.Name = "xrLabel60";
            this.xrLabel60.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel60.SizeF = new System.Drawing.SizeF(353.5041F, 25.05298F);
            this.xrLabel60.StylePriority.UseBorderColor = false;
            this.xrLabel60.StylePriority.UseBorders = false;
            this.xrLabel60.StylePriority.UseFont = false;
            this.xrLabel60.StylePriority.UsePadding = false;
            this.xrLabel60.StylePriority.UseTextAlignment = false;
            this.xrLabel60.Text = "xrTableCell43";
            this.xrLabel60.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabel60.TextFormatString = "{0:#.00}";
            // 
            // xrLabel61
            // 
            this.xrLabel61.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel61.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel61.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel61.LocationFloat = new DevExpress.Utils.PointFloat(604.8315F, 53.21755F);
            this.xrLabel61.Multiline = true;
            this.xrLabel61.Name = "xrLabel61";
            this.xrLabel61.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel61.SizeF = new System.Drawing.SizeF(192.2518F, 25.05298F);
            this.xrLabel61.StylePriority.UseBorderColor = false;
            this.xrLabel61.StylePriority.UseBorders = false;
            this.xrLabel61.StylePriority.UseFont = false;
            this.xrLabel61.StylePriority.UsePadding = false;
            this.xrLabel61.StylePriority.UseTextAlignment = false;
            this.xrLabel61.Text = "Discount";
            this.xrLabel61.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrLabel62
            // 
            this.xrLabel62.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel62.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel62.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel62.LocationFloat = new DevExpress.Utils.PointFloat(9.468434F, 78.27053F);
            this.xrLabel62.Multiline = true;
            this.xrLabel62.Name = "xrLabel62";
            this.xrLabel62.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel62.SizeF = new System.Drawing.SizeF(241.859F, 25.05298F);
            this.xrLabel62.StylePriority.UseBorderColor = false;
            this.xrLabel62.StylePriority.UseBorders = false;
            this.xrLabel62.StylePriority.UseFont = false;
            this.xrLabel62.StylePriority.UsePadding = false;
            this.xrLabel62.StylePriority.UseTextAlignment = false;
            this.xrLabel62.Text = "الإجمالى  ( غير شامل ضريبة القيمة المضافة )";
            this.xrLabel62.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel63
            // 
            this.xrLabel63.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel63.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel63.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[NetTotal]")});
            this.xrLabel63.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel63.LocationFloat = new DevExpress.Utils.PointFloat(251.3274F, 78.27053F);
            this.xrLabel63.Multiline = true;
            this.xrLabel63.Name = "xrLabel63";
            this.xrLabel63.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel63.SizeF = new System.Drawing.SizeF(353.5041F, 25.05298F);
            this.xrLabel63.StylePriority.UseBorderColor = false;
            this.xrLabel63.StylePriority.UseBorders = false;
            this.xrLabel63.StylePriority.UseFont = false;
            this.xrLabel63.StylePriority.UsePadding = false;
            this.xrLabel63.StylePriority.UseTextAlignment = false;
            this.xrLabel63.Text = "xrTableCell46";
            this.xrLabel63.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabel63.TextFormatString = "{0:#.00}";
            // 
            // xrLabel64
            // 
            this.xrLabel64.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel64.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel64.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel64.LocationFloat = new DevExpress.Utils.PointFloat(604.8315F, 78.27053F);
            this.xrLabel64.Multiline = true;
            this.xrLabel64.Name = "xrLabel64";
            this.xrLabel64.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel64.SizeF = new System.Drawing.SizeF(192.2518F, 25.05298F);
            this.xrLabel64.StylePriority.UseBorderColor = false;
            this.xrLabel64.StylePriority.UseBorders = false;
            this.xrLabel64.StylePriority.UseFont = false;
            this.xrLabel64.StylePriority.UsePadding = false;
            this.xrLabel64.StylePriority.UseTextAlignment = false;
            this.xrLabel64.Text = "Total  (without Vat)";
            this.xrLabel64.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrLabel65
            // 
            this.xrLabel65.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel65.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel65.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel65.LocationFloat = new DevExpress.Utils.PointFloat(9.468434F, 103.3235F);
            this.xrLabel65.Multiline = true;
            this.xrLabel65.Name = "xrLabel65";
            this.xrLabel65.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel65.SizeF = new System.Drawing.SizeF(241.859F, 25.05298F);
            this.xrLabel65.StylePriority.UseBorderColor = false;
            this.xrLabel65.StylePriority.UseBorders = false;
            this.xrLabel65.StylePriority.UseFont = false;
            this.xrLabel65.StylePriority.UsePadding = false;
            this.xrLabel65.StylePriority.UseTextAlignment = false;
            this.xrLabel65.Text = "مجموع ضريبة القيمة المضافة";
            this.xrLabel65.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel66
            // 
            this.xrLabel66.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel66.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel66.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[SalesTaxes]")});
            this.xrLabel66.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel66.LocationFloat = new DevExpress.Utils.PointFloat(251.3274F, 103.3235F);
            this.xrLabel66.Multiline = true;
            this.xrLabel66.Name = "xrLabel66";
            this.xrLabel66.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel66.SizeF = new System.Drawing.SizeF(353.5041F, 25.05298F);
            this.xrLabel66.StylePriority.UseBorderColor = false;
            this.xrLabel66.StylePriority.UseBorders = false;
            this.xrLabel66.StylePriority.UseFont = false;
            this.xrLabel66.StylePriority.UsePadding = false;
            this.xrLabel66.StylePriority.UseTextAlignment = false;
            this.xrLabel66.Text = "xrTableCell60";
            this.xrLabel66.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabel66.TextFormatString = "{0:#.00}";
            // 
            // xrLabel67
            // 
            this.xrLabel67.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel67.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel67.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel67.LocationFloat = new DevExpress.Utils.PointFloat(604.8315F, 103.3235F);
            this.xrLabel67.Multiline = true;
            this.xrLabel67.Name = "xrLabel67";
            this.xrLabel67.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel67.SizeF = new System.Drawing.SizeF(192.2518F, 25.05298F);
            this.xrLabel67.StylePriority.UseBorderColor = false;
            this.xrLabel67.StylePriority.UseBorders = false;
            this.xrLabel67.StylePriority.UseFont = false;
            this.xrLabel67.StylePriority.UsePadding = false;
            this.xrLabel67.StylePriority.UseTextAlignment = false;
            this.xrLabel67.Text = "Total VAT";
            this.xrLabel67.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrLabel68
            // 
            this.xrLabel68.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel68.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel68.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel68.LocationFloat = new DevExpress.Utils.PointFloat(9.468434F, 128.3765F);
            this.xrLabel68.Multiline = true;
            this.xrLabel68.Name = "xrLabel68";
            this.xrLabel68.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel68.SizeF = new System.Drawing.SizeF(241.859F, 25.05298F);
            this.xrLabel68.StylePriority.UseBorderColor = false;
            this.xrLabel68.StylePriority.UseBorders = false;
            this.xrLabel68.StylePriority.UseFont = false;
            this.xrLabel68.StylePriority.UsePadding = false;
            this.xrLabel68.StylePriority.UseTextAlignment = false;
            this.xrLabel68.Text = "أجمالى المبلغ المستحق";
            this.xrLabel68.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel69
            // 
            this.xrLabel69.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel69.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel69.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[TotalAfterTaxes]")});
            this.xrLabel69.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel69.LocationFloat = new DevExpress.Utils.PointFloat(251.3274F, 128.3765F);
            this.xrLabel69.Multiline = true;
            this.xrLabel69.Name = "xrLabel69";
            this.xrLabel69.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel69.SizeF = new System.Drawing.SizeF(353.5041F, 25.05298F);
            this.xrLabel69.StylePriority.UseBorderColor = false;
            this.xrLabel69.StylePriority.UseBorders = false;
            this.xrLabel69.StylePriority.UseFont = false;
            this.xrLabel69.StylePriority.UsePadding = false;
            this.xrLabel69.StylePriority.UseTextAlignment = false;
            this.xrLabel69.Text = "xrTableCell63";
            this.xrLabel69.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabel69.TextFormatString = "{0:#.00}";
            // 
            // xrLabel70
            // 
            this.xrLabel70.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel70.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel70.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel70.LocationFloat = new DevExpress.Utils.PointFloat(604.8315F, 128.3765F);
            this.xrLabel70.Multiline = true;
            this.xrLabel70.Name = "xrLabel70";
            this.xrLabel70.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel70.SizeF = new System.Drawing.SizeF(192.2518F, 25.05298F);
            this.xrLabel70.StylePriority.UseBorderColor = false;
            this.xrLabel70.StylePriority.UseBorders = false;
            this.xrLabel70.StylePriority.UseFont = false;
            this.xrLabel70.StylePriority.UsePadding = false;
            this.xrLabel70.StylePriority.UseTextAlignment = false;
            this.xrLabel70.Text = "Total Amount Due";
            this.xrLabel70.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // Id
            // 
            this.Id.AllowNull = true;
            this.Id.Name = "Id";
            this.Id.Type = typeof(int);
            this.Id.Visible = false;
            // 
            // xrControlStyle1
            // 
            this.xrControlStyle1.BackColor = System.Drawing.Color.LightGray;
            this.xrControlStyle1.Name = "xrControlStyle1";
            this.xrControlStyle1.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            // 
            // UserId
            // 
            this.UserId.AllowNull = true;
            this.UserId.Description = "Parameter1";
            this.UserId.Name = "UserId";
            this.UserId.Type = typeof(int);
            this.UserId.Visible = false;
            // 
            // DetailReport
            // 
            this.DetailReport.Bands.AddRange(new DevExpress.XtraReports.UI.Band[] {
            this.Detail1,
            this.ReportHeader});
            this.DetailReport.DataMember = "Details";
            this.DetailReport.DataSource = this.sqlDataSource1;
            this.DetailReport.Level = 0;
            this.DetailReport.Name = "DetailReport";
            // 
            // Detail1
            // 
            this.Detail1.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel10,
            this.xrLabel11,
            this.xrLabel12,
            this.xrLabel13,
            this.xrLabel14,
            this.xrLabel15,
            this.xrLabel16});
            this.Detail1.HeightF = 27.5F;
            this.Detail1.Name = "Detail1";
            // 
            // xrLabel10
            // 
            this.xrLabel10.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel10.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[ServiceName]")});
            this.xrLabel10.Font = new DevExpress.Drawing.DXFont("Arial", 10F);
            this.xrLabel10.LocationFloat = new DevExpress.Utils.PointFloat(9.468445F, 0F);
            this.xrLabel10.Multiline = true;
            this.xrLabel10.Name = "xrLabel10";
            this.xrLabel10.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel10.SizeF = new System.Drawing.SizeF(230F, 27.5F);
            this.xrLabel10.StylePriority.UseBorders = false;
            this.xrLabel10.StylePriority.UseFont = false;
            this.xrLabel10.StylePriority.UsePadding = false;
            this.xrLabel10.StylePriority.UseTextAlignment = false;
            this.xrLabel10.Text = "xrTableCell5";
            this.xrLabel10.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel11
            // 
            this.xrLabel11.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel11.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Price]")});
            this.xrLabel11.Font = new DevExpress.Drawing.DXFont("Arial", 10F);
            this.xrLabel11.LocationFloat = new DevExpress.Utils.PointFloat(239.4684F, 0F);
            this.xrLabel11.Multiline = true;
            this.xrLabel11.Name = "xrLabel11";
            this.xrLabel11.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel11.SizeF = new System.Drawing.SizeF(82F, 27.5F);
            this.xrLabel11.StylePriority.UseBorders = false;
            this.xrLabel11.StylePriority.UseFont = false;
            this.xrLabel11.StylePriority.UsePadding = false;
            this.xrLabel11.StylePriority.UseTextAlignment = false;
            this.xrLabel11.Text = "xrTableCell6";
            this.xrLabel11.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabel11.TextFormatString = "{0:#.00}";
            // 
            // xrLabel12
            // 
            this.xrLabel12.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel12.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[DiscountPerc]")});
            this.xrLabel12.Font = new DevExpress.Drawing.DXFont("Arial", 10F);
            this.xrLabel12.LocationFloat = new DevExpress.Utils.PointFloat(321.4684F, 0F);
            this.xrLabel12.Multiline = true;
            this.xrLabel12.Name = "xrLabel12";
            this.xrLabel12.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel12.SizeF = new System.Drawing.SizeF(92F, 27.5F);
            this.xrLabel12.StylePriority.UseBorders = false;
            this.xrLabel12.StylePriority.UseFont = false;
            this.xrLabel12.StylePriority.UsePadding = false;
            this.xrLabel12.StylePriority.UseTextAlignment = false;
            this.xrLabel12.Text = "xrTableCell25";
            this.xrLabel12.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabel12.TextFormatString = "{0:#.00}";
            // 
            // xrLabel13
            // 
            this.xrLabel13.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel13.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[DiscountValue]")});
            this.xrLabel13.Font = new DevExpress.Drawing.DXFont("Arial", 10F);
            this.xrLabel13.LocationFloat = new DevExpress.Utils.PointFloat(413.4684F, 0F);
            this.xrLabel13.Multiline = true;
            this.xrLabel13.Name = "xrLabel13";
            this.xrLabel13.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel13.SizeF = new System.Drawing.SizeF(92F, 27.5F);
            this.xrLabel13.StylePriority.UseBorders = false;
            this.xrLabel13.StylePriority.UseFont = false;
            this.xrLabel13.StylePriority.UsePadding = false;
            this.xrLabel13.StylePriority.UseTextAlignment = false;
            this.xrLabel13.Text = "xrTableCell16";
            this.xrLabel13.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabel13.TextFormatString = "{0:#.00}";
            // 
            // xrLabel14
            // 
            this.xrLabel14.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel14.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[ItemTaxPercentage]")});
            this.xrLabel14.Font = new DevExpress.Drawing.DXFont("Arial", 10F);
            this.xrLabel14.LocationFloat = new DevExpress.Utils.PointFloat(505.4684F, 0F);
            this.xrLabel14.Multiline = true;
            this.xrLabel14.Name = "xrLabel14";
            this.xrLabel14.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel14.SizeF = new System.Drawing.SizeF(88F, 27.5F);
            this.xrLabel14.StylePriority.UseBorders = false;
            this.xrLabel14.StylePriority.UseFont = false;
            this.xrLabel14.StylePriority.UsePadding = false;
            this.xrLabel14.StylePriority.UseTextAlignment = false;
            this.xrLabel14.Text = "xrTableCell15";
            this.xrLabel14.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabel14.TextFormatString = "{0:0%}";
            // 
            // xrLabel15
            // 
            this.xrLabel15.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel15.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[ItemTaxValue]")});
            this.xrLabel15.Font = new DevExpress.Drawing.DXFont("Arial", 10F);
            this.xrLabel15.LocationFloat = new DevExpress.Utils.PointFloat(593.4684F, 0F);
            this.xrLabel15.Multiline = true;
            this.xrLabel15.Name = "xrLabel15";
            this.xrLabel15.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel15.SizeF = new System.Drawing.SizeF(100F, 27.5F);
            this.xrLabel15.StylePriority.UseBorders = false;
            this.xrLabel15.StylePriority.UseFont = false;
            this.xrLabel15.StylePriority.UsePadding = false;
            this.xrLabel15.StylePriority.UseTextAlignment = false;
            this.xrLabel15.Text = "xrTableCell14";
            this.xrLabel15.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabel15.TextFormatString = "{0:#.00}";
            // 
            // xrLabel16
            // 
            this.xrLabel16.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel16.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Total]")});
            this.xrLabel16.Font = new DevExpress.Drawing.DXFont("Arial", 10F);
            this.xrLabel16.LocationFloat = new DevExpress.Utils.PointFloat(693.4684F, 0F);
            this.xrLabel16.Multiline = true;
            this.xrLabel16.Name = "xrLabel16";
            this.xrLabel16.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel16.SizeF = new System.Drawing.SizeF(103.5316F, 27.5F);
            this.xrLabel16.StylePriority.UseBorders = false;
            this.xrLabel16.StylePriority.UseFont = false;
            this.xrLabel16.StylePriority.UsePadding = false;
            this.xrLabel16.StylePriority.UseTextAlignment = false;
            this.xrLabel16.Text = "xrTableCell7";
            this.xrLabel16.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabel16.TextFormatString = "{0:#.00}";
            // 
            // ReportHeader
            // 
            this.ReportHeader.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrTable3,
            this.xrLabel1,
            this.xrLabel2,
            this.xrLabel3,
            this.xrLabel5,
            this.xrLabel7,
            this.xrLabel8,
            this.xrLabel9});
            this.ReportHeader.HeightF = 66.6F;
            this.ReportHeader.Name = "ReportHeader";
            // 
            // xrTable3
            // 
            this.xrTable3.BackColor = System.Drawing.Color.Gray;
            this.xrTable3.BorderColor = System.Drawing.Color.Transparent;
            this.xrTable3.Font = new DevExpress.Drawing.DXFont("Arial", 15F);
            this.xrTable3.ForeColor = System.Drawing.Color.White;
            this.xrTable3.LocationFloat = new DevExpress.Utils.PointFloat(8.777836F, 0F);
            this.xrTable3.Name = "xrTable3";
            this.xrTable3.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable3.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow3});
            this.xrTable3.SizeF = new System.Drawing.SizeF(786.8552F, 26.58226F);
            this.xrTable3.StylePriority.UseBackColor = false;
            this.xrTable3.StylePriority.UseBorderColor = false;
            this.xrTable3.StylePriority.UseFont = false;
            this.xrTable3.StylePriority.UseForeColor = false;
            // 
            // xrTableRow3
            // 
            this.xrTableRow3.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell30,
            this.xrTableCell32});
            this.xrTableRow3.Name = "xrTableRow3";
            this.xrTableRow3.Weight = 1D;
            // 
            // xrTableCell30
            // 
            this.xrTableCell30.BorderColor = System.Drawing.Color.Silver;
            this.xrTableCell30.Multiline = true;
            this.xrTableCell30.Name = "xrTableCell30";
            this.xrTableCell30.StylePriority.UseBorderColor = false;
            this.xrTableCell30.Text = "التفاصيل";
            this.xrTableCell30.Weight = 1.3332628549440733D;
            // 
            // xrTableCell32
            // 
            this.xrTableCell32.BorderColor = System.Drawing.Color.Silver;
            this.xrTableCell32.Multiline = true;
            this.xrTableCell32.Name = "xrTableCell32";
            this.xrTableCell32.StylePriority.UseBorderColor = false;
            this.xrTableCell32.StylePriority.UseTextAlignment = false;
            this.xrTableCell32.Text = "Particular";
            this.xrTableCell32.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell32.Weight = 1.6667371450559267D;
            // 
            // xrLabel1
            // 
            this.xrLabel1.BackColor = System.Drawing.Color.White;
            this.xrLabel1.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel1.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel1.ForeColor = System.Drawing.Color.Black;
            this.xrLabel1.LocationFloat = new DevExpress.Utils.PointFloat(9.468445F, 26.58228F);
            this.xrLabel1.Multiline = true;
            this.xrLabel1.Name = "xrLabel1";
            this.xrLabel1.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel1.SizeF = new System.Drawing.SizeF(230F, 40F);
            this.xrLabel1.StylePriority.UseBackColor = false;
            this.xrLabel1.StylePriority.UseBorders = false;
            this.xrLabel1.StylePriority.UseFont = false;
            this.xrLabel1.StylePriority.UseForeColor = false;
            this.xrLabel1.StylePriority.UsePadding = false;
            this.xrLabel1.StylePriority.UseTextAlignment = false;
            this.xrLabel1.Text = "Service\r\nالخدمة";
            this.xrLabel1.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel2
            // 
            this.xrLabel2.BackColor = System.Drawing.Color.White;
            this.xrLabel2.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel2.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel2.ForeColor = System.Drawing.Color.Black;
            this.xrLabel2.LocationFloat = new DevExpress.Utils.PointFloat(239.4684F, 26.58228F);
            this.xrLabel2.Multiline = true;
            this.xrLabel2.Name = "xrLabel2";
            this.xrLabel2.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel2.SizeF = new System.Drawing.SizeF(82F, 40F);
            this.xrLabel2.StylePriority.UseBackColor = false;
            this.xrLabel2.StylePriority.UseBorders = false;
            this.xrLabel2.StylePriority.UseFont = false;
            this.xrLabel2.StylePriority.UseForeColor = false;
            this.xrLabel2.StylePriority.UsePadding = false;
            this.xrLabel2.StylePriority.UseTextAlignment = false;
            this.xrLabel2.Text = "Price \r\nالسعر ";
            this.xrLabel2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel3
            // 
            this.xrLabel3.BackColor = System.Drawing.Color.White;
            this.xrLabel3.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel3.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel3.ForeColor = System.Drawing.Color.Black;
            this.xrLabel3.LocationFloat = new DevExpress.Utils.PointFloat(321.4684F, 26.58228F);
            this.xrLabel3.Multiline = true;
            this.xrLabel3.Name = "xrLabel3";
            this.xrLabel3.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel3.SizeF = new System.Drawing.SizeF(92F, 40F);
            this.xrLabel3.StylePriority.UseBackColor = false;
            this.xrLabel3.StylePriority.UseBorders = false;
            this.xrLabel3.StylePriority.UseFont = false;
            this.xrLabel3.StylePriority.UseForeColor = false;
            this.xrLabel3.StylePriority.UsePadding = false;
            this.xrLabel3.StylePriority.UseTextAlignment = false;
            this.xrLabel3.Text = "Discount Rate\r\nنسبة الخصم";
            this.xrLabel3.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel5
            // 
            this.xrLabel5.BackColor = System.Drawing.Color.White;
            this.xrLabel5.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel5.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel5.ForeColor = System.Drawing.Color.Black;
            this.xrLabel5.LocationFloat = new DevExpress.Utils.PointFloat(413.4684F, 26.58224F);
            this.xrLabel5.Multiline = true;
            this.xrLabel5.Name = "xrLabel5";
            this.xrLabel5.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel5.SizeF = new System.Drawing.SizeF(92F, 40F);
            this.xrLabel5.StylePriority.UseBackColor = false;
            this.xrLabel5.StylePriority.UseBorders = false;
            this.xrLabel5.StylePriority.UseFont = false;
            this.xrLabel5.StylePriority.UseForeColor = false;
            this.xrLabel5.StylePriority.UsePadding = false;
            this.xrLabel5.StylePriority.UseTextAlignment = false;
            this.xrLabel5.Text = "Discount \r\nقيمة الخصم";
            this.xrLabel5.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel7
            // 
            this.xrLabel7.BackColor = System.Drawing.Color.White;
            this.xrLabel7.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel7.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel7.ForeColor = System.Drawing.Color.Black;
            this.xrLabel7.LocationFloat = new DevExpress.Utils.PointFloat(505.4684F, 26.58228F);
            this.xrLabel7.Multiline = true;
            this.xrLabel7.Name = "xrLabel7";
            this.xrLabel7.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel7.SizeF = new System.Drawing.SizeF(88F, 40F);
            this.xrLabel7.StylePriority.UseBackColor = false;
            this.xrLabel7.StylePriority.UseBorders = false;
            this.xrLabel7.StylePriority.UseFont = false;
            this.xrLabel7.StylePriority.UseForeColor = false;
            this.xrLabel7.StylePriority.UsePadding = false;
            this.xrLabel7.StylePriority.UseTextAlignment = false;
            this.xrLabel7.Text = "Tax Rate\r\nنسبة الضريبة";
            this.xrLabel7.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel8
            // 
            this.xrLabel8.BackColor = System.Drawing.Color.White;
            this.xrLabel8.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel8.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel8.ForeColor = System.Drawing.Color.Black;
            this.xrLabel8.LocationFloat = new DevExpress.Utils.PointFloat(593.4684F, 26.58224F);
            this.xrLabel8.Multiline = true;
            this.xrLabel8.Name = "xrLabel8";
            this.xrLabel8.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel8.SizeF = new System.Drawing.SizeF(100F, 40F);
            this.xrLabel8.StylePriority.UseBackColor = false;
            this.xrLabel8.StylePriority.UseBorders = false;
            this.xrLabel8.StylePriority.UseFont = false;
            this.xrLabel8.StylePriority.UseForeColor = false;
            this.xrLabel8.StylePriority.UsePadding = false;
            this.xrLabel8.StylePriority.UseTextAlignment = false;
            this.xrLabel8.Text = "Tax Amount\r\nمبلغ الضريبة";
            this.xrLabel8.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel9
            // 
            this.xrLabel9.BackColor = System.Drawing.Color.White;
            this.xrLabel9.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel9.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel9.ForeColor = System.Drawing.Color.Black;
            this.xrLabel9.LocationFloat = new DevExpress.Utils.PointFloat(693.4684F, 26.58228F);
            this.xrLabel9.Multiline = true;
            this.xrLabel9.Name = "xrLabel9";
            this.xrLabel9.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel9.SizeF = new System.Drawing.SizeF(103.5316F, 40F);
            this.xrLabel9.StylePriority.UseBackColor = false;
            this.xrLabel9.StylePriority.UseBorders = false;
            this.xrLabel9.StylePriority.UseFont = false;
            this.xrLabel9.StylePriority.UseForeColor = false;
            this.xrLabel9.StylePriority.UsePadding = false;
            this.xrLabel9.StylePriority.UseTextAlignment = false;
            this.xrLabel9.Text = "Total\r\nالإجمالى";
            this.xrLabel9.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // PageHeader
            // 
            this.PageHeader.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrBarCode1,
            this.xrLine1,
            this.xrPictureBox1,
            this.xrTable7,
            this.xrTable8,
            this.xrLabel17,
            this.xrLabel18,
            this.xrLabel19,
            this.xrLabel71,
            this.xrLabel72,
            this.xrLabel73});
            this.PageHeader.HeightF = 296.6608F;
            this.PageHeader.Name = "PageHeader";
            // 
            // xrLabel17
            // 
            this.xrLabel17.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel17.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel17.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel17.LocationFloat = new DevExpress.Utils.PointFloat(9.916626F, 228F);
            this.xrLabel17.Multiline = true;
            this.xrLabel17.Name = "xrLabel17";
            this.xrLabel17.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel17.SizeF = new System.Drawing.SizeF(104.1603F, 19.4884F);
            this.xrLabel17.StylePriority.UseBorderColor = false;
            this.xrLabel17.StylePriority.UseBorders = false;
            this.xrLabel17.StylePriority.UseFont = false;
            this.xrLabel17.StylePriority.UsePadding = false;
            this.xrLabel17.StylePriority.UseTextAlignment = false;
            this.xrLabel17.Text = "الرقم الضريبي :";
            this.xrLabel17.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // xrLabel18
            // 
            this.xrLabel18.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel18.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel18.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[TaxCardNumber]")});
            this.xrLabel18.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel18.LocationFloat = new DevExpress.Utils.PointFloat(114.0769F, 228F);
            this.xrLabel18.Multiline = true;
            this.xrLabel18.Name = "xrLabel18";
            this.xrLabel18.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel18.SizeF = new System.Drawing.SizeF(152.2419F, 19.4884F);
            this.xrLabel18.StylePriority.UseBorderColor = false;
            this.xrLabel18.StylePriority.UseBorders = false;
            this.xrLabel18.StylePriority.UseFont = false;
            this.xrLabel18.StylePriority.UsePadding = false;
            this.xrLabel18.StylePriority.UseTextAlignment = false;
            this.xrLabel18.Text = "xrTableCell9";
            this.xrLabel18.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter;
            // 
            // xrLabel19
            // 
            this.xrLabel19.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel19.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel19.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel19.LocationFloat = new DevExpress.Utils.PointFloat(266.3188F, 228F);
            this.xrLabel19.Multiline = true;
            this.xrLabel19.Name = "xrLabel19";
            this.xrLabel19.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel19.SizeF = new System.Drawing.SizeF(82.7962F, 19.4884F);
            this.xrLabel19.StylePriority.UseBorderColor = false;
            this.xrLabel19.StylePriority.UseBorders = false;
            this.xrLabel19.StylePriority.UseFont = false;
            this.xrLabel19.StylePriority.UsePadding = false;
            this.xrLabel19.StylePriority.UseTextAlignment = false;
            this.xrLabel19.Text = ":.VAT No";
            this.xrLabel19.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel71
            // 
            this.xrLabel71.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel71.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel71.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel71.LocationFloat = new DevExpress.Utils.PointFloat(9.551697F, 247.4884F);
            this.xrLabel71.Multiline = true;
            this.xrLabel71.Name = "xrLabel71";
            this.xrLabel71.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel71.SizeF = new System.Drawing.SizeF(108.206F, 19.4883F);
            this.xrLabel71.StylePriority.UseBorderColor = false;
            this.xrLabel71.StylePriority.UseBorders = false;
            this.xrLabel71.StylePriority.UseFont = false;
            this.xrLabel71.StylePriority.UsePadding = false;
            this.xrLabel71.Text = "رقم سجل الشركة :";
            // 
            // xrLabel72
            // 
            this.xrLabel72.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel72.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel72.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CommercialRegistrationNo]")});
            this.xrLabel72.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel72.LocationFloat = new DevExpress.Utils.PointFloat(117.7577F, 247.4884F);
            this.xrLabel72.Multiline = true;
            this.xrLabel72.Name = "xrLabel72";
            this.xrLabel72.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel72.SizeF = new System.Drawing.SizeF(191.6249F, 19.4883F);
            this.xrLabel72.StylePriority.UseBorderColor = false;
            this.xrLabel72.StylePriority.UseBorders = false;
            this.xrLabel72.StylePriority.UseFont = false;
            this.xrLabel72.StylePriority.UsePadding = false;
            this.xrLabel72.StylePriority.UseTextAlignment = false;
            this.xrLabel72.Text = "xrTableCell9";
            this.xrLabel72.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter;
            // 
            // xrLabel73
            // 
            this.xrLabel73.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel73.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel73.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.xrLabel73.LocationFloat = new DevExpress.Utils.PointFloat(722.0569F, 228F);
            this.xrLabel73.Multiline = true;
            this.xrLabel73.Name = "xrLabel73";
            this.xrLabel73.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel73.SizeF = new System.Drawing.SizeF(74.94308F, 19.4883F);
            this.xrLabel73.StylePriority.UseBorderColor = false;
            this.xrLabel73.StylePriority.UseBorders = false;
            this.xrLabel73.StylePriority.UseFont = false;
            this.xrLabel73.StylePriority.UsePadding = false;
            this.xrLabel73.StylePriority.UseTextAlignment = false;
            this.xrLabel73.Text = ":.CR No";
            this.xrLabel73.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // ServiceInvoice_Report
            // 
            this.Bands.AddRange(new DevExpress.XtraReports.UI.Band[] {
            this.TopMargin,
            this.BottomMargin,
            this.Detail,
            this.ReportFooter,
            this.DetailReport,
            this.PageHeader});
            this.CalculatedFields.AddRange(new DevExpress.XtraReports.UI.CalculatedField[] {
            this.ExtendedTotal});
            this.ComponentStorage.AddRange(new System.ComponentModel.IComponent[] {
            this.sqlDataSource1});
            this.DataMember = "ServiceInvoice_Get";
            this.DataSource = this.sqlDataSource1;
            this.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.Margins = new DevExpress.Drawing.DXMargins(0F, 20F, 20F, 28.48269F);
            this.PageHeight = 1169;
            this.PageWidth = 827;
            this.PaperKind = DevExpress.Drawing.Printing.DXPaperKind.A4;
            this.Parameters.AddRange(new DevExpress.XtraReports.Parameters.Parameter[] {
            this.UserId,
            this.Id});
            this.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.RightToLeftLayout = DevExpress.XtraReports.UI.RightToLeftLayout.Yes;
            this.StyleSheet.AddRange(new DevExpress.XtraReports.UI.XRControlStyle[] {
            this.xrControlStyle1});
            this.Version = "23.1";
            ((System.ComponentModel.ISupportInitialize)(this.xrTable10)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable4)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable8)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable7)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable11)).EndInit();
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
            obj = new CurrencyInfo(currency);
        }
        var totalAfterTaxesCell = GetCurrentColumnValue("TotalAfterTaxes");
        string total = totalAfterTaxesCell != null ? totalAfterTaxesCell.ToString() : "0";
        ToWord w = new ToWord(decimal.Parse(total), obj, null, null, "", "");
        ((XRLabel)sender).Text = w.ConvertToArabic();
    }

    private void xrPictureBox1_BeforePrint(object sender, CancelEventArgs e)
    {    //Set System Picture
        //SystemSetting systemSetting = new SystemSetting();
        //systemSetting.Logo = db.SystemSettings.FirstOrDefault().Logo.ToString();
        //xrPictureBox1.ImageUrl = systemSetting.Logo;
        int? DepartmentId = null;// (int?)this.DepartmentId.Value;
        var logo = HelperController.GetActivityLogo(DepartmentId);
        xrPictureBox1.ImageUrl = logo;
        this.xrBarCode1.Text = BuildZatcaQrCodeText();
    }

    private string BuildZatcaQrCodeText()
    {
        int invoiceId;
        if (this.Id.Value == null || !int.TryParse(this.Id.Value.ToString(), out invoiceId))
        {
            return string.Empty;
        }

        var serviceInvoice = db.ServiceInvoices.Find(invoiceId);
        if (serviceInvoice == null)
        {
            return string.Empty;
        }

        var companyId = serviceInvoice.Department != null ? serviceInvoice.Department.CompanyId : null;
        var company = companyId.HasValue
            ? db.Companies.FirstOrDefault(c => c.Id == companyId.Value && c.IsDeleted == false)
            : null;

        if (company == null)
        {
            company = db.Companies.FirstOrDefault(c => c.IsActive == true && c.IsDeleted == false)
                ?? db.Companies.FirstOrDefault(c => c.IsDeleted == false);
        }

        var systemSetting = db.SystemSettings.FirstOrDefault();
        var seller = company != null && !string.IsNullOrWhiteSpace(company.ArName)
            ? company.ArName
            : systemSetting != null ? systemSetting.CompanyArName : string.Empty;
        var taxNumber = company != null && !string.IsNullOrWhiteSpace(company.TaxNumber)
            ? company.TaxNumber
            : systemSetting != null ? systemSetting.TaxCardNumber : string.Empty;
        var invoiceTotal = (serviceInvoice.TotalAfterTaxes ?? 0).ToString("0.00", CultureInfo.InvariantCulture);
        var taxTotal = (serviceInvoice.SalesTaxes ?? 0).ToString("0.00", CultureInfo.InvariantCulture);

        return HelperController.EncodeTLV(seller, taxNumber, serviceInvoice.VoucherDate.ToString("s"), invoiceTotal, taxTotal);
    }

    private void Time_BeforePrint(object sender, CancelEventArgs e)
    {
        DateTime utcNow = DateTime.UtcNow;
        TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
        DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
        ((XRLabel)sender).Text = cTime.ToString("d MMMM yyyy h:mm tt");
    }
}
