using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;
using System.Web;
using System.Security.Claims;
using MyERP.Models;
using System.Linq;
using MyERP.Controllers;

/// <summary>
/// Summary description for PropertyContractTotal_Report
/// </summary>
public class PropertyContractTotal_Report : DevExpress.XtraReports.UI.XtraReport
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
    private GroupHeaderBand GroupHeader1;
    private XRTable table1;
    private XRTableRow tableRow1;
    private XRTableCell tableCell1;
    private XRTableCell tableCell3;
    private DetailBand Detail;
    private XRTable table2;
    private XRTableRow tableRow2;
    private XRTableCell tableCell15;
    private XRTableCell tableCell17;
    private XRTableCell tableCell18;
    private XRTableCell tableCell20;
    private XRLabel xrLabel8;
    private XRLabel xrLabel7;
    private XRTableCell xrTableCell1;
    private XRTableCell xrTableCell2;
    private XRTableCell xrTableCell3;
    private XRTableCell xrTableCell4;
    private XRTableCell xrTableCell5;
    private XRTableCell xrTableCell6;
    private XRTableCell xrTableCell7;
    private XRTableCell xrTableCell8;
    private XRTableCell xrTableCell9;
    private XRTableCell xrTableCell10;
    private XRTableCell xrTableCell11;
    private XRTableCell xrTableCell12;
    private XRTableCell xrTableCell13;
    private XRTableCell xrTableCell14;
    private XRPictureBox xrPictureBox1;
    private DevExpress.XtraReports.Parameters.Parameter RenterId;
    private XRTableCell xrTableCell16;
    private XRTableCell xrTableCell15;
    private DevExpress.XtraReports.Parameters.Parameter OwnerId;
    private DevExpress.XtraReports.Parameters.Parameter PropertyId;
    private XRTableCell xrTableCell17;
    private XRTableCell xrTableCell18;
    private DevExpress.XtraReports.Parameters.Parameter DepartmentId;
    private DevExpress.XtraReports.Parameters.Parameter IsLiquidated;
    private DevExpress.XtraReports.Parameters.Parameter IsActive;

    // New cells for additional columns
    private XRTableCell xrTableCellStartDate;
    private XRTableCell xrTableCellEndDate;
    private XRTableCell xrTableCellTotalCollected;
    private XRTableCell xrTableCellStartDateData;
    private XRTableCell xrTableCellEndDateData;
    private XRTableCell xrTableCellTotalCollectedData;

    // Department Group Header
    private GroupHeaderBand GroupHeaderDepartment;
    private XRLabel xrLabelDepartmentHeader;

    // Department Group Footer
    private GroupFooterBand GroupFooterDepartment;
    private XRLabel xrLabelDeptTotalCaption;
    private XRLabel xrLabelDeptTotalValue;
    private XRLabel xrLabelDeptCollectedCaption;
    private XRLabel xrLabelDeptCollectedValue;

    // Filter Panel
    private XRPanel xrPanelFilters;
    private XRLabel xrLabelFilterInfo;

    private GroupFooterBand GroupFooter1;
    private XRLabel xrLabel27;
    private XRLabel xrLabel26;
    private PageHeaderBand PageHeader;
    private XRLabel User;
    private XRLabel xrLabel5;
    private XRLabel xrLabel6;
    private XRLabel Time;
    private XRLabel CompanyName;

    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    public PropertyContractTotal_Report()
    {
        InitializeComponent();
        User.Text = HttpContext.Current.User.Identity.Name;

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
            DevExpress.DataAccess.Sql.QueryParameter queryParameter3 = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.DataAccess.Sql.QueryParameter queryParameter4 = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.DataAccess.Sql.QueryParameter queryParameter5 = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.DataAccess.Sql.QueryParameter queryParameter6 = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.DataAccess.Sql.CustomSqlQuery customSqlQuery1 = new DevExpress.DataAccess.Sql.CustomSqlQuery();
            DevExpress.DataAccess.Sql.CustomSqlQuery customSqlQuery2 = new DevExpress.DataAccess.Sql.CustomSqlQuery();
            DevExpress.DataAccess.Sql.CustomSqlQuery customSqlQuery3 = new DevExpress.DataAccess.Sql.CustomSqlQuery();
            DevExpress.DataAccess.Sql.CustomSqlQuery customSqlQuery4 = new DevExpress.DataAccess.Sql.CustomSqlQuery();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PropertyContractTotal_Report));
            DevExpress.XtraReports.Parameters.DynamicListLookUpSettings dynamicListLookUpSettings1 = new DevExpress.XtraReports.Parameters.DynamicListLookUpSettings();
            DevExpress.XtraReports.Parameters.DynamicListLookUpSettings dynamicListLookUpSettings2 = new DevExpress.XtraReports.Parameters.DynamicListLookUpSettings();
            DevExpress.XtraReports.Parameters.DynamicListLookUpSettings dynamicListLookUpSettings3 = new DevExpress.XtraReports.Parameters.DynamicListLookUpSettings();
            DevExpress.XtraReports.Parameters.DynamicListLookUpSettings dynamicListLookUpSettings4 = new DevExpress.XtraReports.Parameters.DynamicListLookUpSettings();
            this.sqlDataSource1 = new DevExpress.DataAccess.Sql.SqlDataSource(this.components);
            this.Title = new DevExpress.XtraReports.UI.XRControlStyle();
            this.DetailCaption1 = new DevExpress.XtraReports.UI.XRControlStyle();
            this.DetailData1 = new DevExpress.XtraReports.UI.XRControlStyle();
            this.DetailData3_Odd = new DevExpress.XtraReports.UI.XRControlStyle();
            this.PageInfo = new DevExpress.XtraReports.UI.XRControlStyle();
            this.TopMargin = new DevExpress.XtraReports.UI.TopMarginBand();
            this.BottomMargin = new DevExpress.XtraReports.UI.BottomMarginBand();
            this.User = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel5 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel6 = new DevExpress.XtraReports.UI.XRLabel();
            this.Time = new DevExpress.XtraReports.UI.XRLabel();
            this.pageInfo2 = new DevExpress.XtraReports.UI.XRPageInfo();
            this.xrPictureBox1 = new DevExpress.XtraReports.UI.XRPictureBox();
            this.xrLabel8 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel7 = new DevExpress.XtraReports.UI.XRLabel();
            this.GroupHeader1 = new DevExpress.XtraReports.UI.GroupHeaderBand();
            this.table1 = new DevExpress.XtraReports.UI.XRTable();
            this.tableRow1 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell16 = new DevExpress.XtraReports.UI.XRTableCell();
            this.tableCell1 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCellStartDate = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCellEndDate = new DevExpress.XtraReports.UI.XRTableCell();
            this.tableCell3 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell15 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell1 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell3 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell4 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell5 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell6 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell7 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell8 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell17 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCellTotalCollected = new DevExpress.XtraReports.UI.XRTableCell();
            this.Detail = new DevExpress.XtraReports.UI.DetailBand();
            this.table2 = new DevExpress.XtraReports.UI.XRTable();
            this.tableRow2 = new DevExpress.XtraReports.UI.XRTableRow();
            this.tableCell15 = new DevExpress.XtraReports.UI.XRTableCell();
            this.tableCell17 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCellStartDateData = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCellEndDateData = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell2 = new DevExpress.XtraReports.UI.XRTableCell();
            this.tableCell18 = new DevExpress.XtraReports.UI.XRTableCell();
            this.tableCell20 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell9 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell10 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell11 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell12 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell13 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell14 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell18 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCellTotalCollectedData = new DevExpress.XtraReports.UI.XRTableCell();
            this.RenterId = new DevExpress.XtraReports.Parameters.Parameter();
            this.OwnerId = new DevExpress.XtraReports.Parameters.Parameter();
            this.PropertyId = new DevExpress.XtraReports.Parameters.Parameter();
            this.DepartmentId = new DevExpress.XtraReports.Parameters.Parameter();
            this.IsLiquidated = new DevExpress.XtraReports.Parameters.Parameter();
            this.IsActive = new DevExpress.XtraReports.Parameters.Parameter();
            this.GroupHeaderDepartment = new DevExpress.XtraReports.UI.GroupHeaderBand();
            this.xrLabelDepartmentHeader = new DevExpress.XtraReports.UI.XRLabel();
            this.GroupFooterDepartment = new DevExpress.XtraReports.UI.GroupFooterBand();
            this.xrLabelDeptTotalCaption = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelDeptTotalValue = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelDeptCollectedCaption = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelDeptCollectedValue = new DevExpress.XtraReports.UI.XRLabel();
            this.GroupFooter1 = new DevExpress.XtraReports.UI.GroupFooterBand();
            this.xrLabel27 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel26 = new DevExpress.XtraReports.UI.XRLabel();
            this.PageHeader = new DevExpress.XtraReports.UI.PageHeaderBand();
            this.xrPanelFilters = new DevExpress.XtraReports.UI.XRPanel();
            this.xrLabelFilterInfo = new DevExpress.XtraReports.UI.XRLabel();
            this.CompanyName = new DevExpress.XtraReports.UI.XRLabel();
            ((System.ComponentModel.ISupportInitialize)(this.table1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.table2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            // 
            // sqlDataSource1
            // 
            this.sqlDataSource1.ConnectionName = "localhost_MySoftERP_Connection";
            this.sqlDataSource1.Name = "sqlDataSource1";
            storedProcQuery1.Name = "SP_PropertyContractTotal_Report";
            queryParameter1.Name = "@RenterId";
            queryParameter1.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter1.Value = new DevExpress.DataAccess.Expression("?RenterId", typeof(int));
            queryParameter2.Name = "@OwnerId";
            queryParameter2.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter2.Value = new DevExpress.DataAccess.Expression("?OwnerId", typeof(int));
            queryParameter3.Name = "@PropertyId";
            queryParameter3.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter3.Value = new DevExpress.DataAccess.Expression("?PropertyId", typeof(int));
            queryParameter4.Name = "@DepartmentId";
            queryParameter4.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter4.Value = new DevExpress.DataAccess.Expression("?DepartmentId", typeof(int));
            queryParameter5.Name = "@IsLiquidated";
            queryParameter5.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter5.Value = new DevExpress.DataAccess.Expression("?IsLiquidated", typeof(bool));
            queryParameter6.Name = "@IsActive";
            queryParameter6.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter6.Value = new DevExpress.DataAccess.Expression("?IsActive", typeof(bool));
            storedProcQuery1.Parameters.AddRange(new DevExpress.DataAccess.Sql.QueryParameter[] {
            queryParameter1,
            queryParameter2,
            queryParameter3,
            queryParameter4,
            queryParameter5,
            queryParameter6});
            storedProcQuery1.StoredProcName = "SP_PropertyContractTotal_Report";
            customSqlQuery1.Name = "Renter";
            customSqlQuery1.Sql = "select Id,ArName from PropertyRenter where IsActive=1 and IsDeleted=0";
            customSqlQuery2.Name = "Property";
            customSqlQuery2.Sql = "select Id,ArName from Property where IsDeleted=0 and IsActive=1";
            customSqlQuery3.Name = "Owner";
            customSqlQuery3.Sql = "select Id,ArName from PropertyOwner where IsActive=1 and IsDeleted=0";
            customSqlQuery4.Name = "Department";
            customSqlQuery4.Sql = "select Id,ArName from Department where IsDeleted=0";
            this.sqlDataSource1.Queries.AddRange(new DevExpress.DataAccess.Sql.SqlQuery[] {
            storedProcQuery1,
            customSqlQuery1,
            customSqlQuery2,
            customSqlQuery3,
            customSqlQuery4});
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
            this.User,
            this.xrLabel5,
            this.xrLabel6,
            this.Time,
            this.pageInfo2});
            this.BottomMargin.HeightF = 73.20897F;
            this.BottomMargin.Name = "BottomMargin";
            // 
            // User
            // 
            this.User.BackColor = System.Drawing.Color.White;
            this.User.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.User.ForeColor = System.Drawing.Color.Black;
            this.User.LocationFloat = new DevExpress.Utils.PointFloat(109.8336F, 9.999974F);
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
            // xrLabel5
            // 
            this.xrLabel5.BackColor = System.Drawing.Color.White;
            this.xrLabel5.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.xrLabel5.ForeColor = System.Drawing.Color.Black;
            this.xrLabel5.LocationFloat = new DevExpress.Utils.PointFloat(9.833252F, 9.999974F);
            this.xrLabel5.Multiline = true;
            this.xrLabel5.Name = "xrLabel5";
            this.xrLabel5.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel5.SizeF = new System.Drawing.SizeF(100F, 23F);
            this.xrLabel5.StylePriority.UseBackColor = false;
            this.xrLabel5.StylePriority.UseFont = false;
            this.xrLabel5.StylePriority.UseForeColor = false;
            this.xrLabel5.Text = "المستخدم";
            // 
            // xrLabel6
            // 
            this.xrLabel6.BackColor = System.Drawing.Color.White;
            this.xrLabel6.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.xrLabel6.ForeColor = System.Drawing.Color.Black;
            this.xrLabel6.LocationFloat = new DevExpress.Utils.PointFloat(393.5767F, 9.999974F);
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
            this.Time.LocationFloat = new DevExpress.Utils.PointFloat(493.5767F, 9.999974F);
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
            this.pageInfo2.LocationFloat = new DevExpress.Utils.PointFloat(366.9166F, 46.50043F);
            this.pageInfo2.Name = "pageInfo2";
            this.pageInfo2.SizeF = new System.Drawing.SizeF(370.0001F, 23F);
            this.pageInfo2.StyleName = "PageInfo";
            this.pageInfo2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.pageInfo2.TextFormatString = "Page {0} of {1}";
            // 
            // xrPictureBox1
            // 
            this.xrPictureBox1.ImageUrl = "http://genoise1.mysoft-eg.com/assets/images/logo.png";
            this.xrPictureBox1.LocationFloat = new DevExpress.Utils.PointFloat(917.5641F, 0F);
            this.xrPictureBox1.Name = "xrPictureBox1";
            this.xrPictureBox1.SizeF = new System.Drawing.SizeF(148.0608F, 140.2916F);
            this.xrPictureBox1.Sizing = DevExpress.XtraPrinting.ImageSizeMode.ZoomImage;
            this.xrPictureBox1.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrPictureBox1_BeforePrint);
            // 
            // xrLabel8
            // 
            this.xrLabel8.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(128)))), ((int)(((byte)(192)))));
            this.xrLabel8.Font = new DevExpress.Drawing.DXFont("Arial", 14F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel8.ForeColor = System.Drawing.Color.White;
            this.xrLabel8.LocationFloat = new DevExpress.Utils.PointFloat(433.2383F, 90.41663F);
            this.xrLabel8.Multiline = true;
            this.xrLabel8.Name = "xrLabel8";
            this.xrLabel8.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel8.SizeF = new System.Drawing.SizeF(316.8781F, 23F);
            this.xrLabel8.StylePriority.UseBackColor = false;
            this.xrLabel8.StylePriority.UseFont = false;
            this.xrLabel8.StylePriority.UseForeColor = false;
            this.xrLabel8.StylePriority.UseTextAlignment = false;
            this.xrLabel8.Text = "Property Contract Total";
            this.xrLabel8.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel7
            // 
            this.xrLabel7.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(3)))), ((int)(((byte)(142)))), ((int)(((byte)(188)))));
            this.xrLabel7.Font = new DevExpress.Drawing.DXFont("Arial", 12F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel7.ForeColor = System.Drawing.Color.White;
            this.xrLabel7.LocationFloat = new DevExpress.Utils.PointFloat(516.7758F, 67.4166F);
            this.xrLabel7.Multiline = true;
            this.xrLabel7.Name = "xrLabel7";
            this.xrLabel7.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel7.SizeF = new System.Drawing.SizeF(160.4812F, 23F);
            this.xrLabel7.StylePriority.UseBackColor = false;
            this.xrLabel7.StylePriority.UseFont = false;
            this.xrLabel7.StylePriority.UseForeColor = false;
            this.xrLabel7.StylePriority.UseTextAlignment = false;
            this.xrLabel7.Text = "تقرير إجمالي العقود";
            this.xrLabel7.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // GroupHeader1
            // 
            this.GroupHeader1.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.table1});
            this.GroupHeader1.GroupUnion = DevExpress.XtraReports.UI.GroupUnion.WithFirstDetail;
            this.GroupHeader1.HeightF = 28F;
            this.GroupHeader1.Name = "GroupHeader1";
            this.GroupHeader1.RepeatEveryPage = true;
            // 
            // table1
            // 
            this.table1.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.table1.Name = "table1";
            this.table1.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.tableRow1});
            this.table1.SizeF = new System.Drawing.SizeF(1089F, 28F);
            this.table1.StylePriority.UseTextAlignment = false;
            this.table1.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // tableRow1
            // 
            this.tableRow1.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell16,
            this.tableCell1,
            this.xrTableCellStartDate,
            this.xrTableCellEndDate,
            this.tableCell3,
            this.xrTableCell15,
            this.xrTableCell1,
            this.xrTableCell3,
            this.xrTableCell4,
            this.xrTableCell5,
            this.xrTableCell6,
            this.xrTableCell7,
            this.xrTableCell8,
            this.xrTableCell17,
            this.xrTableCellTotalCollected});
            this.tableRow1.Name = "tableRow1";
            this.tableRow1.Weight = 1D;
            // 
            // xrTableCell16
            // 
            this.xrTableCell16.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrTableCell16.Multiline = true;
            this.xrTableCell16.Name = "xrTableCell16";
            this.xrTableCell16.StyleName = "DetailCaption1";
            this.xrTableCell16.StylePriority.UseBorders = false;
            this.xrTableCell16.StylePriority.UseTextAlignment = false;
            this.xrTableCell16.Text = "رقم العقد";
            this.xrTableCell16.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell16.Weight = 0.0820081203579935D;
            // 
            // tableCell1
            // 
            this.tableCell1.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.tableCell1.Name = "tableCell1";
            this.tableCell1.StyleName = "DetailCaption1";
            this.tableCell1.StylePriority.UseBorders = false;
            this.tableCell1.StylePriority.UseTextAlignment = false;
            this.tableCell1.Text = "نوع العقد";
            this.tableCell1.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.tableCell1.Weight = 0.0820081203579935D;
            // 
            // xrTableCellStartDate
            // 
            this.xrTableCellStartDate.Multiline = true;
            this.xrTableCellStartDate.Name = "xrTableCellStartDate";
            this.xrTableCellStartDate.StyleName = "DetailCaption1";
            this.xrTableCellStartDate.StylePriority.UseTextAlignment = false;
            this.xrTableCellStartDate.Text = "تاريخ البداية";
            this.xrTableCellStartDate.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCellStartDate.Weight = 0.064D;
            // 
            // xrTableCellEndDate
            // 
            this.xrTableCellEndDate.Multiline = true;
            this.xrTableCellEndDate.Name = "xrTableCellEndDate";
            this.xrTableCellEndDate.StyleName = "DetailCaption1";
            this.xrTableCellEndDate.StylePriority.UseTextAlignment = false;
            this.xrTableCellEndDate.Text = "تاريخ النهاية";
            this.xrTableCellEndDate.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCellEndDate.Weight = 0.064D;
            // 
            // tableCell3
            // 
            this.tableCell3.Name = "tableCell3";
            this.tableCell3.StyleName = "DetailCaption1";
            this.tableCell3.StylePriority.UseTextAlignment = false;
            this.tableCell3.Text = "المالك";
            this.tableCell3.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.tableCell3.Weight = 0.086397740562176445D;
            // 
            // xrTableCell15
            // 
            this.xrTableCell15.Multiline = true;
            this.xrTableCell15.Name = "xrTableCell15";
            this.xrTableCell15.StyleName = "DetailCaption1";
            this.xrTableCell15.StylePriority.UseTextAlignment = false;
            this.xrTableCell15.Text = "اسم العقار";
            this.xrTableCell15.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell15.Weight = 0.086397740562176445D;
            // 
            // xrTableCell1
            // 
            this.xrTableCell1.Multiline = true;
            this.xrTableCell1.Name = "xrTableCell1";
            this.xrTableCell1.StyleName = "DetailCaption1";
            this.xrTableCell1.StylePriority.UseTextAlignment = false;
            this.xrTableCell1.Text = "النوع";
            this.xrTableCell1.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell1.Weight = 0.075318947057633187D;
            // 
            // xrTableCell3
            // 
            this.xrTableCell3.Multiline = true;
            this.xrTableCell3.Name = "xrTableCell3";
            this.xrTableCell3.StyleName = "DetailCaption1";
            this.xrTableCell3.StylePriority.UseTextAlignment = false;
            this.xrTableCell3.Text = "رقم";
            this.xrTableCell3.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell3.Weight = 0.091561339511026185D;
            // 
            // xrTableCell4
            // 
            this.xrTableCell4.Multiline = true;
            this.xrTableCell4.Name = "xrTableCell4";
            this.xrTableCell4.StyleName = "DetailCaption1";
            this.xrTableCell4.StylePriority.UseTextAlignment = false;
            this.xrTableCell4.Text = "المستأجر";
            this.xrTableCell4.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell4.Weight = 0.091561339511026185D;
            // 
            // xrTableCell5
            // 
            this.xrTableCell5.Multiline = true;
            this.xrTableCell5.Name = "xrTableCell5";
            this.xrTableCell5.StyleName = "DetailCaption1";
            this.xrTableCell5.StylePriority.UseTextAlignment = false;
            this.xrTableCell5.Text = "الإيجار";
            this.xrTableCell5.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell5.Weight = 0.091561339511026185D;
            // 
            // xrTableCell6
            // 
            this.xrTableCell6.Multiline = true;
            this.xrTableCell6.Name = "xrTableCell6";
            this.xrTableCell6.StyleName = "DetailCaption1";
            this.xrTableCell6.StylePriority.UseTextAlignment = false;
            this.xrTableCell6.Text = "السعي";
            this.xrTableCell6.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell6.Weight = 0.091561339511026185D;
            // 
            // xrTableCell7
            // 
            this.xrTableCell7.Multiline = true;
            this.xrTableCell7.Name = "xrTableCell7";
            this.xrTableCell7.StyleName = "DetailCaption1";
            this.xrTableCell7.StylePriority.UseTextAlignment = false;
            this.xrTableCell7.Text = "المياه";
            this.xrTableCell7.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell7.Weight = 0.091561339511026185D;
            // 
            // xrTableCell8
            // 
            this.xrTableCell8.Multiline = true;
            this.xrTableCell8.Name = "xrTableCell8";
            this.xrTableCell8.StyleName = "DetailCaption1";
            this.xrTableCell8.StylePriority.UseTextAlignment = false;
            this.xrTableCell8.Text = "الخدمات";
            this.xrTableCell8.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell8.Weight = 0.091561339511026185D;
            // 
            // xrTableCell17
            // 
            this.xrTableCell17.Multiline = true;
            this.xrTableCell17.Name = "xrTableCell17";
            this.xrTableCell17.StyleName = "DetailCaption1";
            this.xrTableCell17.StylePriority.UseTextAlignment = false;
            this.xrTableCell17.Text = "الإجمالي";
            this.xrTableCell17.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell17.Weight = 0.069D;
            // 
            // xrTableCellTotalCollected
            // 
            this.xrTableCellTotalCollected.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.xrTableCellTotalCollected.BorderColor = System.Drawing.Color.Black;
            this.xrTableCellTotalCollected.ForeColor = System.Drawing.Color.Black;
            this.xrTableCellTotalCollected.Multiline = true;
            this.xrTableCellTotalCollected.Name = "xrTableCellTotalCollected";
            this.xrTableCellTotalCollected.StyleName = "DetailCaption1";
            this.xrTableCellTotalCollected.StylePriority.UseBackColor = false;
            this.xrTableCellTotalCollected.StylePriority.UseBorderColor = false;
            this.xrTableCellTotalCollected.StylePriority.UseForeColor = false;
            this.xrTableCellTotalCollected.StylePriority.UseTextAlignment = false;
            this.xrTableCellTotalCollected.Text = "المحصل";
            this.xrTableCellTotalCollected.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCellTotalCollected.Weight = 0.069D;
            // 
            // Detail
            // 
            this.Detail.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.table2});
            this.Detail.HeightF = 25.4583F;
            this.Detail.Name = "Detail";
            // 
            // table2
            // 
            this.table2.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.table2.Name = "table2";
            this.table2.OddStyleName = "DetailData3_Odd";
            this.table2.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.tableRow2});
            this.table2.SizeF = new System.Drawing.SizeF(1089F, 25F);
            // 
            // tableRow2
            // 
            this.tableRow2.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.tableCell15,
            this.tableCell17,
            this.xrTableCellStartDateData,
            this.xrTableCellEndDateData,
            this.xrTableCell2,
            this.tableCell18,
            this.tableCell20,
            this.xrTableCell9,
            this.xrTableCell10,
            this.xrTableCell11,
            this.xrTableCell12,
            this.xrTableCell13,
            this.xrTableCell14,
            this.xrTableCell18,
            this.xrTableCellTotalCollectedData});
            this.tableRow2.Name = "tableRow2";
            this.tableRow2.Weight = 11.5D;
            // 
            // tableCell15
            // 
            this.tableCell15.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.tableCell15.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[ContractNo]")});
            this.tableCell15.Name = "tableCell15";
            this.tableCell15.StyleName = "DetailData1";
            this.tableCell15.StylePriority.UseBorders = false;
            this.tableCell15.StylePriority.UseTextAlignment = false;
            this.tableCell15.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.tableCell15.Weight = 0.082008120483374428D;
            // 
            // tableCell17
            // 
            this.tableCell17.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[ContractType]")});
            this.tableCell17.Name = "tableCell17";
            this.tableCell17.StyleName = "DetailData1";
            this.tableCell17.StylePriority.UseTextAlignment = false;
            this.tableCell17.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.tableCell17.Weight = 0.086397740770168374D;
            // 
            // xrTableCellStartDateData
            // 
            this.xrTableCellStartDateData.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[StartDate]")});
            this.xrTableCellStartDateData.Multiline = true;
            this.xrTableCellStartDateData.Name = "xrTableCellStartDateData";
            this.xrTableCellStartDateData.StyleName = "DetailData1";
            this.xrTableCellStartDateData.StylePriority.UseTextAlignment = false;
            this.xrTableCellStartDateData.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCellStartDateData.TextFormatString = "{0:dd/MM/yyyy}";
            this.xrTableCellStartDateData.Weight = 0.064D;
            // 
            // xrTableCellEndDateData
            // 
            this.xrTableCellEndDateData.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[EndDate]")});
            this.xrTableCellEndDateData.Multiline = true;
            this.xrTableCellEndDateData.Name = "xrTableCellEndDateData";
            this.xrTableCellEndDateData.StyleName = "DetailData1";
            this.xrTableCellEndDateData.StylePriority.UseTextAlignment = false;
            this.xrTableCellEndDateData.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCellEndDateData.TextFormatString = "{0:dd/MM/yyyy}";
            this.xrTableCellEndDateData.Weight = 0.064D;
            // 
            // xrTableCell2
            // 
            this.xrTableCell2.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Owner]")});
            this.xrTableCell2.Multiline = true;
            this.xrTableCell2.Name = "xrTableCell2";
            this.xrTableCell2.StyleName = "DetailData1";
            this.xrTableCell2.StylePriority.UseTextAlignment = false;
            this.xrTableCell2.Text = "xrTableCell2";
            this.xrTableCell2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell2.Weight = 0.075318864060200275D;
            // 
            // tableCell18
            // 
            this.tableCell18.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Property]")});
            this.tableCell18.Name = "tableCell18";
            this.tableCell18.StyleName = "DetailData1";
            this.tableCell18.StylePriority.UseTextAlignment = false;
            this.tableCell18.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.tableCell18.Weight = 0.075318864060200275D;
            // 
            // tableCell20
            // 
            this.tableCell20.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[PropertyType]")});
            this.tableCell20.Name = "tableCell20";
            this.tableCell20.StyleName = "DetailData1";
            this.tableCell20.StylePriority.UseTextAlignment = false;
            this.tableCell20.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.tableCell20.Weight = 0.091686016726004849D;
            // 
            // xrTableCell9
            // 
            this.xrTableCell9.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[PropertyNo]")});
            this.xrTableCell9.Multiline = true;
            this.xrTableCell9.Name = "xrTableCell9";
            this.xrTableCell9.StyleName = "DetailData1";
            this.xrTableCell9.StylePriority.UseTextAlignment = false;
            this.xrTableCell9.Text = "xrTableCell9";
            this.xrTableCell9.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell9.Weight = 0.091686016726004849D;
            // 
            // xrTableCell10
            // 
            this.xrTableCell10.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Renter]")});
            this.xrTableCell10.Multiline = true;
            this.xrTableCell10.Name = "xrTableCell10";
            this.xrTableCell10.StyleName = "DetailData1";
            this.xrTableCell10.StylePriority.UseTextAlignment = false;
            this.xrTableCell10.Text = "xrTableCell10";
            this.xrTableCell10.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell10.Weight = 0.091686016726004849D;
            // 
            // xrTableCell11
            // 
            this.xrTableCell11.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[RentValue]")});
            this.xrTableCell11.Multiline = true;
            this.xrTableCell11.Name = "xrTableCell11";
            this.xrTableCell11.StyleName = "DetailData1";
            this.xrTableCell11.StylePriority.UseTextAlignment = false;
            this.xrTableCell11.Text = "xrTableCell11";
            this.xrTableCell11.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell11.TextFormatString = "{0:#.00}";
            this.xrTableCell11.Weight = 0.091686016726004849D;
            // 
            // xrTableCell12
            // 
            this.xrTableCell12.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CommissionValue]")});
            this.xrTableCell12.Multiline = true;
            this.xrTableCell12.Name = "xrTableCell12";
            this.xrTableCell12.StyleName = "DetailData1";
            this.xrTableCell12.StylePriority.UseTextAlignment = false;
            this.xrTableCell12.Text = "xrTableCell12";
            this.xrTableCell12.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell12.TextFormatString = "{0:#.00}";
            this.xrTableCell12.Weight = 0.091686016726004849D;
            // 
            // xrTableCell13
            // 
            this.xrTableCell13.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[WaterValue]")});
            this.xrTableCell13.Multiline = true;
            this.xrTableCell13.Name = "xrTableCell13";
            this.xrTableCell13.StyleName = "DetailData1";
            this.xrTableCell13.StylePriority.UseTextAlignment = false;
            this.xrTableCell13.Text = "xrTableCell13";
            this.xrTableCell13.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell13.TextFormatString = "{0:#.00}";
            this.xrTableCell13.Weight = 0.091686016726004849D;
            // 
            // xrTableCell14
            // 
            this.xrTableCell14.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[ServicesValue]")});
            this.xrTableCell14.Multiline = true;
            this.xrTableCell14.Name = "xrTableCell14";
            this.xrTableCell14.StyleName = "DetailData1";
            this.xrTableCell14.StylePriority.UseTextAlignment = false;
            this.xrTableCell14.Text = "xrTableCell14";
            this.xrTableCell14.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell14.TextFormatString = "{0:#.00}";
            this.xrTableCell14.Weight = 0.091686016726004849D;
            // 
            // xrTableCell18
            // 
            this.xrTableCell18.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[TotalContractValue]")});
            this.xrTableCell18.Multiline = true;
            this.xrTableCell18.Name = "xrTableCell18";
            this.xrTableCell18.StyleName = "DetailData1";
            this.xrTableCell18.StylePriority.UseTextAlignment = false;
            this.xrTableCell18.Text = "xrTableCell18";
            this.xrTableCell18.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell18.TextFormatString = "{0:#.00}";
            this.xrTableCell18.Weight = 0.069D;
            // 
            // xrTableCellTotalCollectedData
            // 
            this.xrTableCellTotalCollectedData.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(245)))), ((int)(((byte)(233)))));
            this.xrTableCellTotalCollectedData.BorderColor = System.Drawing.Color.Black;
            this.xrTableCellTotalCollectedData.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[TotalCollected]")});
            this.xrTableCellTotalCollectedData.Multiline = true;
            this.xrTableCellTotalCollectedData.Name = "xrTableCellTotalCollectedData";
            this.xrTableCellTotalCollectedData.StyleName = "DetailData1";
            this.xrTableCellTotalCollectedData.StylePriority.UseBackColor = false;
            this.xrTableCellTotalCollectedData.StylePriority.UseBorderColor = false;
            this.xrTableCellTotalCollectedData.StylePriority.UseTextAlignment = false;
            this.xrTableCellTotalCollectedData.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCellTotalCollectedData.TextFormatString = "{0:N2}";
            this.xrTableCellTotalCollectedData.Weight = 0.069D;
            // 
            // RenterId
            // 
            this.RenterId.AllowNull = true;
            this.RenterId.Description = "المستأجر";
            this.RenterId.Name = "RenterId";
            this.RenterId.Type = typeof(int);
            dynamicListLookUpSettings1.DataMember = "Renter";
            dynamicListLookUpSettings1.DataSource = this.sqlDataSource1;
            dynamicListLookUpSettings1.DisplayMember = "ArName";
            dynamicListLookUpSettings1.FilterString = null;
            dynamicListLookUpSettings1.SortMember = "Id";
            dynamicListLookUpSettings1.SortOrder = DevExpress.Data.ColumnSortOrder.Ascending;
            dynamicListLookUpSettings1.ValueMember = "Id";
            this.RenterId.ValueSourceSettings = dynamicListLookUpSettings1;
            // 
            // OwnerId
            // 
            this.OwnerId.AllowNull = true;
            this.OwnerId.Description = "المالك";
            this.OwnerId.Name = "OwnerId";
            this.OwnerId.Type = typeof(int);
            dynamicListLookUpSettings2.DataMember = "Owner";
            dynamicListLookUpSettings2.DataSource = this.sqlDataSource1;
            dynamicListLookUpSettings2.DisplayMember = "ArName";
            dynamicListLookUpSettings2.FilterString = null;
            dynamicListLookUpSettings2.SortMember = "Id";
            dynamicListLookUpSettings2.SortOrder = DevExpress.Data.ColumnSortOrder.Ascending;
            dynamicListLookUpSettings2.ValueMember = "Id";
            this.OwnerId.ValueSourceSettings = dynamicListLookUpSettings2;
            // 
            // PropertyId
            // 
            this.PropertyId.AllowNull = true;
            this.PropertyId.Description = "العقار";
            this.PropertyId.Name = "PropertyId";
            this.PropertyId.Type = typeof(int);
            dynamicListLookUpSettings3.DataMember = "Property";
            dynamicListLookUpSettings3.DataSource = this.sqlDataSource1;
            dynamicListLookUpSettings3.DisplayMember = "ArName";
            dynamicListLookUpSettings3.FilterString = null;
            dynamicListLookUpSettings3.SortMember = "Id";
            dynamicListLookUpSettings3.SortOrder = DevExpress.Data.ColumnSortOrder.Ascending;
            dynamicListLookUpSettings3.ValueMember = "Id";
            this.PropertyId.ValueSourceSettings = dynamicListLookUpSettings3;
            // 
            // DepartmentId
            // 
            this.DepartmentId.AllowNull = true;
            this.DepartmentId.Description = "القسم";
            this.DepartmentId.Name = "DepartmentId";
            this.DepartmentId.Type = typeof(int);
            dynamicListLookUpSettings4.DataMember = "Department";
            dynamicListLookUpSettings4.DataSource = this.sqlDataSource1;
            dynamicListLookUpSettings4.DisplayMember = "ArName";
            dynamicListLookUpSettings4.FilterString = null;
            dynamicListLookUpSettings4.SortMember = "Id";
            dynamicListLookUpSettings4.SortOrder = DevExpress.Data.ColumnSortOrder.Ascending;
            dynamicListLookUpSettings4.ValueMember = "Id";
            this.DepartmentId.ValueSourceSettings = dynamicListLookUpSettings4;
            // 
            // IsLiquidated
            // 
            this.IsLiquidated.AllowNull = true;
            this.IsLiquidated.Description = "حالة التصفية";
            this.IsLiquidated.Name = "IsLiquidated";
            this.IsLiquidated.Type = typeof(bool);
            // 
            // IsActive
            // 
            this.IsActive.AllowNull = true;
            this.IsActive.Description = "حالة العقد";
            this.IsActive.Name = "IsActive";
            this.IsActive.Type = typeof(bool);
            // 
            // GroupHeaderDepartment
            // 
            this.GroupHeaderDepartment.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabelDepartmentHeader});
            this.GroupHeaderDepartment.GroupFields.AddRange(new DevExpress.XtraReports.UI.GroupField[] {
            new DevExpress.XtraReports.UI.GroupField("Department", DevExpress.XtraReports.UI.XRColumnSortOrder.Ascending)});
            this.GroupHeaderDepartment.HeightF = 35F;
            this.GroupHeaderDepartment.Level = 1;
            this.GroupHeaderDepartment.Name = "GroupHeaderDepartment";
            // 
            // xrLabelDepartmentHeader
            // 
            this.xrLabelDepartmentHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(58)))), ((int)(((byte)(159)))), ((int)(((byte)(228)))));
            this.xrLabelDepartmentHeader.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "\'القسم: \' + [Department]")});
            this.xrLabelDepartmentHeader.Font = new DevExpress.Drawing.DXFont("Arial", 12F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabelDepartmentHeader.ForeColor = System.Drawing.Color.White;
            this.xrLabelDepartmentHeader.LocationFloat = new DevExpress.Utils.PointFloat(0F, 5F);
            this.xrLabelDepartmentHeader.Name = "xrLabelDepartmentHeader";
            this.xrLabelDepartmentHeader.Padding = new DevExpress.XtraPrinting.PaddingInfo(10, 2, 0, 0, 100F);
            this.xrLabelDepartmentHeader.SizeF = new System.Drawing.SizeF(1089F, 25F);
            this.xrLabelDepartmentHeader.StylePriority.UseBackColor = false;
            this.xrLabelDepartmentHeader.StylePriority.UseFont = false;
            this.xrLabelDepartmentHeader.StylePriority.UseForeColor = false;
            this.xrLabelDepartmentHeader.StylePriority.UsePadding = false;
            this.xrLabelDepartmentHeader.StylePriority.UseTextAlignment = false;
            this.xrLabelDepartmentHeader.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // GroupFooterDepartment
            // 
            this.GroupFooterDepartment.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabelDeptTotalCaption,
            this.xrLabelDeptTotalValue,
            this.xrLabelDeptCollectedCaption,
            this.xrLabelDeptCollectedValue});
            this.GroupFooterDepartment.HeightF = 30F;
            this.GroupFooterDepartment.Level = 1;
            this.GroupFooterDepartment.Name = "GroupFooterDepartment";
            // 
            // xrLabelDeptTotalCaption
            // 
            this.xrLabelDeptTotalCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(250)))));
            this.xrLabelDeptTotalCaption.Font = new DevExpress.Drawing.DXFont("Arial", 11F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabelDeptTotalCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(128)))));
            this.xrLabelDeptTotalCaption.LocationFloat = new DevExpress.Utils.PointFloat(969F, 3F);
            this.xrLabelDeptTotalCaption.Name = "xrLabelDeptTotalCaption";
            this.xrLabelDeptTotalCaption.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 10, 0, 0, 100F);
            this.xrLabelDeptTotalCaption.SizeF = new System.Drawing.SizeF(120F, 23F);
            this.xrLabelDeptTotalCaption.StylePriority.UseBackColor = false;
            this.xrLabelDeptTotalCaption.StylePriority.UseFont = false;
            this.xrLabelDeptTotalCaption.StylePriority.UseForeColor = false;
            this.xrLabelDeptTotalCaption.StylePriority.UsePadding = false;
            this.xrLabelDeptTotalCaption.StylePriority.UseTextAlignment = false;
            this.xrLabelDeptTotalCaption.Text = "إجمالي القسم:";
            this.xrLabelDeptTotalCaption.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrLabelDeptTotalValue
            // 
            this.xrLabelDeptTotalValue.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "sumSum([TotalContractValue])")});
            this.xrLabelDeptTotalValue.Font = new DevExpress.Drawing.DXFont("Arial", 11F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabelDeptTotalValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(128)))));
            this.xrLabelDeptTotalValue.LocationFloat = new DevExpress.Utils.PointFloat(859F, 3F);
            this.xrLabelDeptTotalValue.Name = "xrLabelDeptTotalValue";
            this.xrLabelDeptTotalValue.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 10, 0, 0, 100F);
            this.xrLabelDeptTotalValue.SizeF = new System.Drawing.SizeF(110F, 23F);
            this.xrLabelDeptTotalValue.StylePriority.UseFont = false;
            this.xrLabelDeptTotalValue.StylePriority.UseForeColor = false;
            this.xrLabelDeptTotalValue.StylePriority.UsePadding = false;
            this.xrLabelDeptTotalValue.StylePriority.UseTextAlignment = false;
            this.xrLabelDeptTotalValue.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrLabelDeptTotalValue.TextFormatString = "{0:N2}";
            // 
            // xrLabelDeptCollectedCaption
            // 
            this.xrLabelDeptCollectedCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(245)))), ((int)(((byte)(233)))));
            this.xrLabelDeptCollectedCaption.Font = new DevExpress.Drawing.DXFont("Arial", 11F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabelDeptCollectedCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(100)))), ((int)(((byte)(0)))));
            this.xrLabelDeptCollectedCaption.LocationFloat = new DevExpress.Utils.PointFloat(739F, 3F);
            this.xrLabelDeptCollectedCaption.Name = "xrLabelDeptCollectedCaption";
            this.xrLabelDeptCollectedCaption.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 10, 0, 0, 100F);
            this.xrLabelDeptCollectedCaption.SizeF = new System.Drawing.SizeF(120F, 23F);
            this.xrLabelDeptCollectedCaption.StylePriority.UseBackColor = false;
            this.xrLabelDeptCollectedCaption.StylePriority.UseFont = false;
            this.xrLabelDeptCollectedCaption.StylePriority.UseForeColor = false;
            this.xrLabelDeptCollectedCaption.StylePriority.UsePadding = false;
            this.xrLabelDeptCollectedCaption.StylePriority.UseTextAlignment = false;
            this.xrLabelDeptCollectedCaption.Text = "المبلغ المحصل:";
            this.xrLabelDeptCollectedCaption.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrLabelDeptCollectedValue
            // 
            this.xrLabelDeptCollectedValue.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "sumSum([TotalCollected])")});
            this.xrLabelDeptCollectedValue.Font = new DevExpress.Drawing.DXFont("Arial", 11F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabelDeptCollectedValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(100)))), ((int)(((byte)(0)))));
            this.xrLabelDeptCollectedValue.LocationFloat = new DevExpress.Utils.PointFloat(629F, 3F);
            this.xrLabelDeptCollectedValue.Name = "xrLabelDeptCollectedValue";
            this.xrLabelDeptCollectedValue.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 10, 0, 0, 100F);
            this.xrLabelDeptCollectedValue.SizeF = new System.Drawing.SizeF(110F, 23F);
            this.xrLabelDeptCollectedValue.StylePriority.UseFont = false;
            this.xrLabelDeptCollectedValue.StylePriority.UseForeColor = false;
            this.xrLabelDeptCollectedValue.StylePriority.UsePadding = false;
            this.xrLabelDeptCollectedValue.StylePriority.UseTextAlignment = false;
            this.xrLabelDeptCollectedValue.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrLabelDeptCollectedValue.TextFormatString = "{0:N2}";
            // 
            // GroupFooter1
            // 
            this.GroupFooter1.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel27,
            this.xrLabel26});
            this.GroupFooter1.HeightF = 35.41667F;
            this.GroupFooter1.Name = "GroupFooter1";
            // 
            // xrLabel27
            // 
            this.xrLabel27.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "Sum([TotalContractValue])")});
            this.xrLabel27.Font = new DevExpress.Drawing.DXFont("Arial", 14F);
            this.xrLabel27.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.xrLabel27.LocationFloat = new DevExpress.Utils.PointFloat(357.2995F, 12.41667F);
            this.xrLabel27.Multiline = true;
            this.xrLabel27.Name = "xrLabel27";
            this.xrLabel27.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel27.SizeF = new System.Drawing.SizeF(185.1921F, 23F);
            this.xrLabel27.StylePriority.UseFont = false;
            this.xrLabel27.StylePriority.UseForeColor = false;
            this.xrLabel27.StylePriority.UseTextAlignment = false;
            this.xrLabel27.Text = "xrLabel18";
            this.xrLabel27.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabel27.TextFormatString = "{0:0.00}";
            // 
            // xrLabel26
            // 
            this.xrLabel26.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(57)))), ((int)(((byte)(159)))), ((int)(((byte)(228)))));
            this.xrLabel26.ForeColor = System.Drawing.Color.White;
            this.xrLabel26.LocationFloat = new DevExpress.Utils.PointFloat(249.2162F, 12.41667F);
            this.xrLabel26.Multiline = true;
            this.xrLabel26.Name = "xrLabel26";
            this.xrLabel26.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel26.SizeF = new System.Drawing.SizeF(108.0834F, 23F);
            this.xrLabel26.StylePriority.UseBackColor = false;
            this.xrLabel26.StylePriority.UseForeColor = false;
            this.xrLabel26.StylePriority.UseTextAlignment = false;
            this.xrLabel26.Text = "الإجمالي العام:";
            this.xrLabel26.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // PageHeader
            // 
            this.PageHeader.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrPanelFilters,
            this.CompanyName,
            this.xrLabel8,
            this.xrLabel7,
            this.xrPictureBox1});
            this.PageHeader.HeightF = 195F;
            this.PageHeader.Name = "PageHeader";
            // 
            // xrPanelFilters
            // 
            this.xrPanelFilters.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(248)))), ((int)(((byte)(220)))));
            this.xrPanelFilters.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(193)))), ((int)(((byte)(7)))));
            this.xrPanelFilters.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrPanelFilters.BorderWidth = 1F;
            this.xrPanelFilters.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabelFilterInfo});
            this.xrPanelFilters.LocationFloat = new DevExpress.Utils.PointFloat(0F, 165F);
            this.xrPanelFilters.Name = "xrPanelFilters";
            this.xrPanelFilters.SizeF = new System.Drawing.SizeF(1089F, 27F);
            this.xrPanelFilters.StylePriority.UseBackColor = false;
            this.xrPanelFilters.StylePriority.UseBorderColor = false;
            this.xrPanelFilters.StylePriority.UseBorders = false;
            this.xrPanelFilters.StylePriority.UseBorderWidth = false;
            // 
            // xrLabelFilterInfo
            // 
            this.xrLabelFilterInfo.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabelFilterInfo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(102)))), ((int)(((byte)(51)))), ((int)(((byte)(0)))));
            this.xrLabelFilterInfo.LocationFloat = new DevExpress.Utils.PointFloat(5F, 3F);
            this.xrLabelFilterInfo.Name = "xrLabelFilterInfo";
            this.xrLabelFilterInfo.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 10, 0, 0, 100F);
            this.xrLabelFilterInfo.SizeF = new System.Drawing.SizeF(1079F, 20F);
            this.xrLabelFilterInfo.StylePriority.UseFont = false;
            this.xrLabelFilterInfo.StylePriority.UseForeColor = false;
            this.xrLabelFilterInfo.StylePriority.UsePadding = false;
            this.xrLabelFilterInfo.StylePriority.UseTextAlignment = false;
            this.xrLabelFilterInfo.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrLabelFilterInfo.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrLabelFilterInfo_BeforePrint);
            // 
            // CompanyName
            // 
            this.CompanyName.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(3)))), ((int)(((byte)(142)))), ((int)(((byte)(188)))));
            this.CompanyName.Font = new DevExpress.Drawing.DXFont("Arial", 12F, DevExpress.Drawing.DXFontStyle.Bold);
            this.CompanyName.ForeColor = System.Drawing.Color.White;
            this.CompanyName.LocationFloat = new DevExpress.Utils.PointFloat(26.49982F, 31.49983F);
            this.CompanyName.Multiline = true;
            this.CompanyName.Name = "CompanyName";
            this.CompanyName.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.CompanyName.SizeF = new System.Drawing.SizeF(187.6977F, 23F);
            this.CompanyName.StylePriority.UseBackColor = false;
            this.CompanyName.StylePriority.UseFont = false;
            this.CompanyName.StylePriority.UseForeColor = false;
            this.CompanyName.StylePriority.UseTextAlignment = false;
            this.CompanyName.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // PropertyContractTotal_Report
            // 
            this.Bands.AddRange(new DevExpress.XtraReports.UI.Band[] {
            this.TopMargin,
            this.BottomMargin,
            this.PageHeader,
            this.GroupHeaderDepartment,
            this.GroupHeader1,
            this.Detail,
            this.GroupFooter1,
            this.GroupFooterDepartment});
            this.ComponentStorage.AddRange(new System.ComponentModel.IComponent[] {
            this.sqlDataSource1});
            this.DataMember = "SP_PropertyContractTotal_Report";
            this.DataSource = this.sqlDataSource1;
            this.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.Landscape = true;
            this.Margins = new DevExpress.Drawing.DXMargins(40F, 40F, 40F, 73.20897F);
            this.PageHeight = 827;
            this.PageWidth = 1169;
            this.PaperKind = DevExpress.Drawing.Printing.DXPaperKind.A4;
            this.Parameters.AddRange(new DevExpress.XtraReports.Parameters.Parameter[] {
            this.RenterId,
            this.OwnerId,
            this.PropertyId,
            this.DepartmentId,
            this.IsLiquidated,
            this.IsActive});
            this.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.RightToLeftLayout = DevExpress.XtraReports.UI.RightToLeftLayout.Yes;
            this.StyleSheet.AddRange(new DevExpress.XtraReports.UI.XRControlStyle[] {
            this.Title,
            this.DetailCaption1,
            this.DetailData1,
            this.DetailData3_Odd,
            this.PageInfo});
            this.Version = "23.1";
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

    private void xrPictureBox1_BeforePrint(object sender, CancelEventArgs e)
    {
        int? DepartmentId = null;// (int?)this.DepartmentId.Value;
        var logo = HelperController.GetActivityLogo(DepartmentId);
        xrPictureBox1.ImageUrl = logo;
        CompanyName.Text = db.SystemSettings.FirstOrDefault().CompanyArName;
    }

    private void xrLabelFilterInfo_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
    {
        var filters = new System.Collections.Generic.List<string>();

        try
        {
            // فحص المستأجر
            if (this.Parameters["RenterId"].Value != null &&
                !string.IsNullOrEmpty(this.Parameters["RenterId"].Value.ToString()))
            {
                var renterId = Convert.ToInt32(this.Parameters["RenterId"].Value);
                var renter = db.PropertyRenters.Find(renterId);
                if (renter != null)
                    filters.Add($"المستأجر: {renter.ArName}");
            }

            // فحص المالك
            if (this.Parameters["OwnerId"].Value != null &&
                !string.IsNullOrEmpty(this.Parameters["OwnerId"].Value.ToString()))
            {
                var ownerId = Convert.ToInt32(this.Parameters["OwnerId"].Value);
                var owner = db.PropertyOwners.Find(ownerId);
                if (owner != null)
                    filters.Add($"المالك: {owner.ArName}");
            }

            // فحص العقار
            if (this.Parameters["PropertyId"].Value != null &&
                !string.IsNullOrEmpty(this.Parameters["PropertyId"].Value.ToString()))
            {
                var propertyId = Convert.ToInt32(this.Parameters["PropertyId"].Value);
                var property = db.Properties.Find(propertyId);
                if (property != null)
                    filters.Add($"العقار: {property.ArName}");
            }

            // فحص القسم
            if (this.Parameters["DepartmentId"].Value != null &&
                !string.IsNullOrEmpty(this.Parameters["DepartmentId"].Value.ToString()))
            {
                var deptId = Convert.ToInt32(this.Parameters["DepartmentId"].Value);
                var dept = db.Departments.Find(deptId);
                if (dept != null)
                    filters.Add($"القسم: {dept.ArName}");
            }

            // فحص حالة التصفية
            if (this.Parameters["IsLiquidated"].Value != null)
            {
                var isLiquidated = Convert.ToBoolean(this.Parameters["IsLiquidated"].Value);
                filters.Add(isLiquidated ? "✓ العقود المصفاة فقط" : "✗ العقود غير المصفاة فقط");
            }

            // فحص حالة العقد
            if (this.Parameters["IsActive"].Value != null)
            {
                var isActive = Convert.ToBoolean(this.Parameters["IsActive"].Value);
                filters.Add(isActive ? "✓ العقود النشطة فقط" : "✗ العقود غير النشطة فقط");
            }

            // عرض الفلاتر
            if (filters.Count > 0)
            {
                this.xrLabelFilterInfo.Text = "الفلاتر المطبقة: " + string.Join(" | ", filters);
                this.xrPanelFilters.Visible = true;
            }
            else
            {
                this.xrLabelFilterInfo.Text = "جميع العقود (بدون فلاتر)";
                this.xrPanelFilters.Visible = true;
            }
        }
        catch
        {
            this.xrLabelFilterInfo.Text = "عرض الفلاتر...";
            this.xrPanelFilters.Visible = true;
        }
    }
}
