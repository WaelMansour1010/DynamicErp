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

/// <summary>
/// Summary description for EmployeesContract
/// </summary>
public class EmployeesContract_Report : DevExpress.XtraReports.UI.XtraReport
{
    private MySoftERPEntity db = new MySoftERPEntity();
    private TopMarginBand TopMargin;
    private BottomMarginBand BottomMargin;
    private DetailBand Detail;
    private DevExpress.DataAccess.Sql.SqlDataSource sqlDataSource1;
    private CalculatedField ExtendedTotal;
    private XRControlStyle xrControlStyle1;
    private DevExpress.XtraReports.Parameters.Parameter UserId;

    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;
    private XRLabel xrLabel3;
    private XRTable xrTable26;
    private XRTableRow xrTableRow54;
    private XRTableCell xrTableCell107;
    private XRTableCell xrTableCell108;
    private XRTable xrTable27;
    private XRTableRow xrTableRow55;
    private XRTableCell xrTableCell109;
    private XRTableCell xrTableCell110;
    private XRTableRow xrTableRow58;
    private XRTableCell xrTableCell115;
    private XRTableCell xrTableCell116;
    private XRTableRow xrTableRow57;
    private XRTableCell xrTableCell113;
    private XRTableCell xrTableCell114;
    private XRTableRow xrTableRow56;
    private XRTableCell xrTableCell111;
    private XRTableCell xrTableCell112;
    private XRTable xrTable24;
    private XRTableRow xrTableRow52;
    private XRTableCell xrTableCell103;
    private XRTableCell xrTableCell104;
    private XRTable xrTable25;
    private XRTableRow xrTableRow53;
    private XRTableCell xrTableCell105;
    private XRTableCell xrTableCell106;
    private XRTable xrTable22;
    private XRTableRow xrTableRow50;
    private XRTableCell xrTableCell99;
    private XRTableCell xrTableCell100;
    private XRTable xrTable23;
    private XRTableRow xrTableRow51;
    private XRTableCell xrTableCell101;
    private XRTableCell xrTableCell102;
    private XRTable xrTable20;
    private XRTableRow xrTableRow44;
    private XRTableCell xrTableCell87;
    private XRTableCell xrTableCell88;
    private XRTable xrTable21;
    private XRTableRow xrTableRow49;
    private XRTableCell xrTableCell97;
    private XRTableCell xrTableCell98;
    private XRTable xrTable17;
    private XRTableRow xrTableRow39;
    private XRTableCell xrTableCell77;
    private XRTableCell xrTableCell78;
    private XRTable xrTable18;
    private XRTableRow xrTableRow40;
    private XRTableCell xrTableCell79;
    private XRTableCell xrTableCell80;
    private XRTableRow xrTableRow41;
    private XRTableCell xrTableCell81;
    private XRTableCell xrTableCell82;
    private XRTableRow xrTableRow42;
    private XRTableCell xrTableCell83;
    private XRTableCell xrTableCell84;
    private XRTableRow xrTableRow43;
    private XRTableCell xrTableCell85;
    private XRTableCell xrTableCell86;
    private XRTable xrTable19;
    private XRTableRow xrTableRow45;
    private XRTableCell xrTableCell89;
    private XRTableCell xrTableCell90;
    private XRTableRow xrTableRow46;
    private XRTableCell xrTableCell91;
    private XRTableCell xrTableCell92;
    private XRTableRow xrTableRow47;
    private XRTableCell xrTableCell93;
    private XRTableCell xrTableCell94;
    private XRTableRow xrTableRow48;
    private XRTableCell xrTableCell95;
    private XRTableCell xrTableCell96;
    private XRTable xrTable15;
    private XRTableRow xrTableRow26;
    private XRTableCell xrTableCell51;
    private XRTableCell xrTableCell52;
    private XRTableRow xrTableRow27;
    private XRTableCell xrTableCell53;
    private XRTableCell xrTableCell54;
    private XRTableRow xrTableRow31;
    private XRTableCell xrTableCell61;
    private XRTableCell xrTableCell62;
    private XRTableRow xrTableRow32;
    private XRTableCell xrTableCell63;
    private XRTableCell xrTableCell64;
    private XRTableRow xrTableRow33;
    private XRTableCell xrTableCell65;
    private XRTableCell xrTableCell66;
    private XRTable xrTable12;
    private XRTableRow xrTableRow20;
    private XRTableCell xrTableCell39;
    private XRTableCell xrTableCell40;
    private XRTable xrTable16;
    private XRTableRow xrTableRow34;
    private XRTableCell xrTableCell67;
    private XRTableCell xrTableCell68;
    private XRTableRow xrTableRow35;
    private XRTableCell xrTableCell69;
    private XRTableCell xrTableCell70;
    private XRTableRow xrTableRow36;
    private XRTableCell xrTableCell71;
    private XRTableCell xrTableCell72;
    private XRTableRow xrTableRow37;
    private XRTableCell xrTableCell73;
    private XRTableCell xrTableCell74;
    private XRTableRow xrTableRow38;
    private XRTableCell xrTableCell75;
    private XRTableCell xrTableCell76;
    private XRTable xrTable2;
    private XRTableRow xrTableRow15;
    private XRTableCell xrTableCell29;
    private XRTableCell xrTableCell30;
    private XRTable xrTable14;
    private XRTableRow xrTableRow25;
    private XRTableCell xrTableCell49;
    private XRTableCell xrTableCell50;
    private XRTable xrTable11;
    private XRTableRow xrTableRow30;
    private XRTableCell xrTableCell59;
    private XRTableCell xrTableCell60;
    private XRTable xrTable10;
    private XRTableRow xrTableRow29;
    private XRTableCell xrTableCell57;
    private XRTableCell xrTableCell58;
    private XRTable xrTable9;
    private XRTableRow xrTableRow28;
    private XRTableCell xrTableCell55;
    private XRTableCell xrTableCell56;
    private XRTable xrTable4;
    private XRTableRow xrTableRow21;
    private XRTableCell xrTableCell41;
    private XRTableCell xrTableCell42;
    private XRTableRow xrTableRow23;
    private XRTableCell xrTableCell45;
    private XRTableCell xrTableCell46;
    private XRTableRow xrTableRow24;
    private XRTableCell xrTableCell47;
    private XRTableCell xrTableCell48;
    private XRTable xrTable1;
    private XRTableRow xrTableRow14;
    private XRTableCell xrTableCell25;
    private XRTableCell xrTableCell28;
    private XRTable xrTable3;
    private XRTableRow xrTableRow16;
    private XRTableCell xrTableCell31;
    private XRTableCell xrTableCell32;
    private XRTableRow xrTableRow18;
    private XRTableCell xrTableCell35;
    private XRTableCell xrTableCell36;
    private XRTableRow xrTableRow19;
    private XRTableCell xrTableCell37;
    private XRTableCell xrTableCell38;
    private XRTable xrTable6;
    private XRTableRow xrTableRow6;
    private XRTableCell xrTableCell17;
    private XRTableCell xrTableCell18;
    private XRTable xrTable5;
    private XRTableRow xrTableRow5;
    private XRTableCell xrTableCell3;
    private XRTableCell xrTableCell9;
    private XRTable xrTable13;
    private XRTableRow xrTableRow9;
    private XRTableCell xrTableCell26;
    private XRTableCell xrTableCell27;
    private XRTable xrTable8;
    private XRTableRow xrTableRow8;
    private XRTableCell xrTableCell23;
    private XRTableCell xrTableCell24;
    private XRTable xrTable7;
    private XRTableRow xrTableRow7;
    private XRTableCell xrTableCell21;
    private XRTableCell xrTableCell22;
    private XRTable xrTable30;
    private XRTableRow xrTableRow61;
    private XRTableCell xrTableCell121;
    private XRTableCell xrTableCell122;
    private XRTable xrTable31;
    private XRTableRow xrTableRow62;
    private XRTableCell xrTableCell123;
    private XRTableCell xrTableCell124;
    private XRTableRow xrTableRow63;
    private XRTableCell xrTableCell125;
    private XRTableCell xrTableCell126;
    private XRTableRow xrTableRow64;
    private XRTableCell xrTableCell127;
    private XRTableCell xrTableCell128;
    private XRTableRow xrTableRow65;
    private XRTableCell xrTableCell129;
    private XRTableCell xrTableCell130;
    private XRTableRow xrTableRow66;
    private XRTableCell xrTableCell131;
    private XRTableCell xrTableCell132;
    private XRTableRow xrTableRow68;
    private XRTableCell xrTableCell135;
    private XRTableCell xrTableCell136;
    private XRTableRow xrTableRow72;
    private XRTableCell xrTableCell143;
    private XRTableCell xrTableCell144;
    private XRTableRow xrTableRow71;
    private XRTableCell xrTableCell141;
    private XRTableCell xrTableCell142;
    private XRTableRow xrTableRow70;
    private XRTableCell xrTableCell139;
    private XRTableCell xrTableCell140;
    private XRTableRow xrTableRow69;
    private XRTableCell xrTableCell137;
    private XRTableCell xrTableCell138;
    private XRTableRow xrTableRow67;
    private XRTableCell xrTableCell133;
    private XRTableCell xrTableCell134;
    private XRTable xrTable28;
    private XRTableRow xrTableRow59;
    private XRTableCell xrTableCell117;
    private XRTableCell xrTableCell118;
    private XRTable xrTable29;
    private XRTableRow xrTableRow60;
    private XRTableCell xrTableCell119;
    private XRTableCell xrTableCell120;
    private XRTable xrTable32;
    private XRTableRow xrTableRow73;
    private XRTableCell xrTableCell145;
    private XRTableCell xrTableCell146;
    private XRTable xrTable33;
    private XRTableRow xrTableRow74;
    private XRTableCell xrTableCell147;
    private XRTableCell xrTableCell148;
    private XRTableRow xrTableRow75;
    private XRTableCell xrTableCell149;
    private XRTableCell xrTableCell150;
    private XRTableRow xrTableRow76;
    private XRTableCell xrTableCell151;
    private XRTableCell xrTableCell152;
    private XRTableRow xrTableRow77;
    private XRTableCell xrTableCell153;
    private XRTableCell xrTableCell154;
    private XRTable xrTable41;
    private XRTableRow xrTableRow89;
    private XRTableCell xrTableCell177;
    private XRTableCell xrTableCell178;
    private XRTable xrTable40;
    private XRTableRow xrTableRow88;
    private XRTableCell xrTableCell175;
    private XRTableCell xrTableCell176;
    private XRTable xrTable38;
    private XRTableRow xrTableRow84;
    private XRTableCell xrTableCell167;
    private XRTableCell xrTableCell168;
    private XRTable xrTable39;
    private XRTableRow xrTableRow85;
    private XRTableCell xrTableCell169;
    private XRTableCell xrTableCell170;
    private XRTableRow xrTableRow86;
    private XRTableCell xrTableCell171;
    private XRTableCell xrTableCell172;
    private XRTableRow xrTableRow87;
    private XRTableCell xrTableCell173;
    private XRTableCell xrTableCell174;
    private XRTable xrTable36;
    private XRTableRow xrTableRow81;
    private XRTableCell xrTableCell161;
    private XRTableCell xrTableCell162;
    private XRTable xrTable37;
    private XRTableRow xrTableRow82;
    private XRTableCell xrTableCell163;
    private XRTableCell xrTableCell164;
    private XRTableRow xrTableRow83;
    private XRTableCell xrTableCell165;
    private XRTableCell xrTableCell166;
    private XRTable xrTable35;
    private XRTableRow xrTableRow79;
    private XRTableCell xrTableCell157;
    private XRTableCell xrTableCell158;
    private XRTableRow xrTableRow80;
    private XRTableCell xrTableCell159;
    private XRTableCell xrTableCell160;
    private XRTable xrTable34;
    private XRTableRow xrTableRow78;
    private XRTableCell xrTableCell155;
    private XRTableCell xrTableCell156;
    private XRLine xrLine2;
    private XRPageInfo pageInfo2;
    private XRTableCell xrTableCell1;
    private XRTableCell xrTableCell2;
    private XRTableCell xrTableCell4;
    private DevExpress.XtraReports.Parameters.Parameter Id;
    private PageHeaderBand PageHeader;
    private XRPictureBox xrPictureBox1;
    private XRLabel CompanyName;
    private XRLabel xrLabel7;
    public EmployeesContract_Report(int Id)
    {
        InitializeComponent();
        this.Id.Value = Id;
       // UserId.Value = int.Parse(((ClaimsIdentity)HttpContext.Current.User.Identity).FindFirst("Id").Value);
        // 
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
            DevExpress.DataAccess.Sql.StoredProcQuery storedProcQuery1 = new DevExpress.DataAccess.Sql.StoredProcQuery();
            DevExpress.DataAccess.Sql.QueryParameter queryParameter1 = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.DataAccess.Sql.MasterDetailInfo masterDetailInfo1 = new DevExpress.DataAccess.Sql.MasterDetailInfo();
            DevExpress.DataAccess.Sql.RelationColumnInfo relationColumnInfo1 = new DevExpress.DataAccess.Sql.RelationColumnInfo();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EmployeesContract_Report));
            this.sqlDataSource1 = new DevExpress.DataAccess.Sql.SqlDataSource(this.components);
            this.TopMargin = new DevExpress.XtraReports.UI.TopMarginBand();
            this.BottomMargin = new DevExpress.XtraReports.UI.BottomMarginBand();
            this.pageInfo2 = new DevExpress.XtraReports.UI.XRPageInfo();
            this.Detail = new DevExpress.XtraReports.UI.DetailBand();
            this.xrLine2 = new DevExpress.XtraReports.UI.XRLine();
            this.xrTable41 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow89 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell177 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell178 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell4 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTable40 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow88 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell175 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell176 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTable38 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow84 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell167 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell168 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTable39 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow85 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell169 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell170 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow86 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell171 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell172 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow87 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell173 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell174 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTable36 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow81 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell161 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell162 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTable37 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow82 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell163 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell164 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow83 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell165 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell166 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTable35 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow79 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell157 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell158 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow80 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell159 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell160 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTable34 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow78 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell155 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell156 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTable32 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow73 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell145 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell146 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTable33 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow74 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell147 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell148 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow75 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell149 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell150 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow76 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell151 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell152 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow77 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell153 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell154 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTable30 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow61 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell121 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell122 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTable31 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow62 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell123 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell124 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow63 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell125 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell126 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow64 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell127 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell128 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow65 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell129 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell130 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow66 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell131 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell132 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow68 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell135 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell136 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow72 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell143 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell144 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow71 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell141 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell142 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow70 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell139 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell140 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow69 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell137 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell138 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow67 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell133 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell134 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTable28 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow59 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell117 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell118 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTable29 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow60 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell119 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell120 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTable26 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow54 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell107 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell108 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTable27 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow55 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell109 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell110 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow58 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell115 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell116 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow57 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell113 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell114 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow56 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell111 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell112 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTable24 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow52 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell103 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell104 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTable25 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow53 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell105 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell106 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTable22 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow50 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell99 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell100 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTable23 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow51 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell101 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell102 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTable20 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow44 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell87 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell88 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTable21 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow49 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell97 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell98 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTable17 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow39 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell77 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell78 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTable18 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow40 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell79 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell80 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow41 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell81 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell82 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow42 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell83 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell84 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow43 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell85 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell86 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTable19 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow45 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell89 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell90 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow46 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell91 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell92 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow47 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell93 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell94 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow48 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell95 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell96 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTable15 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow26 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell51 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell52 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow27 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell53 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell54 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow31 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell61 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell62 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow32 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell63 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell64 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow33 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell65 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell66 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTable12 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow20 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell39 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell40 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTable16 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow34 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell67 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell68 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow35 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell69 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell70 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow36 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell71 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell72 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow37 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell73 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell74 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow38 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell75 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell76 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTable2 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow15 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell29 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell30 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTable14 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow25 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell49 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell50 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTable11 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow30 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell59 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell60 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTable10 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow29 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell57 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell58 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTable9 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow28 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell55 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell56 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTable4 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow21 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell41 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell42 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow23 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell45 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell46 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow24 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell47 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell48 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTable1 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow14 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell25 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell28 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTable3 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow16 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell31 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell32 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow18 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell35 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell36 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow19 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell37 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell38 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTable6 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow6 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell17 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell18 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTable5 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow5 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell3 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell9 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTable13 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow9 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell26 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell27 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTable8 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow8 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell23 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell24 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTable7 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow7 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell21 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell1 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell2 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell22 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrLabel3 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel7 = new DevExpress.XtraReports.UI.XRLabel();
            this.ExtendedTotal = new DevExpress.XtraReports.UI.CalculatedField();
            this.xrControlStyle1 = new DevExpress.XtraReports.UI.XRControlStyle();
            this.UserId = new DevExpress.XtraReports.Parameters.Parameter();
            this.Id = new DevExpress.XtraReports.Parameters.Parameter();
            this.PageHeader = new DevExpress.XtraReports.UI.PageHeaderBand();
            this.xrPictureBox1 = new DevExpress.XtraReports.UI.XRPictureBox();
            this.CompanyName = new DevExpress.XtraReports.UI.XRLabel();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable41)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable40)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable38)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable39)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable36)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable37)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable35)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable34)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable32)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable33)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable30)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable31)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable28)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable29)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable26)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable27)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable24)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable25)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable22)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable23)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable20)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable21)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable17)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable18)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable19)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable15)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable12)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable16)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable14)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable11)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable10)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable9)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable4)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable6)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable5)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable13)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable8)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable7)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            // 
            // sqlDataSource1
            // 
            this.sqlDataSource1.ConnectionName = "localhost_MySoftERP_Connection";
            this.sqlDataSource1.Name = "sqlDataSource1";
            storedProcQuery1.Name = "EmployeesContract_Get";
            queryParameter1.Name = "@Id";
            queryParameter1.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter1.Value = new DevExpress.DataAccess.Expression("?Id", typeof(int));
            storedProcQuery1.Parameters.Add(queryParameter1);
            storedProcQuery1.StoredProcName = "EmployeesContract_Get";
            this.sqlDataSource1.Queries.AddRange(new DevExpress.DataAccess.Sql.SqlQuery[] {
            storedProcQuery1});
            masterDetailInfo1.DetailQueryName = "Details";
            relationColumnInfo1.NestedKeyColumn = "MainDocId";
            relationColumnInfo1.ParentKeyColumn = "SalesInvoiceId";
            masterDetailInfo1.KeyColumns.Add(relationColumnInfo1);
            masterDetailInfo1.MasterQueryName = "EmployeesContract_Get";
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
            this.pageInfo2});
            this.BottomMargin.HeightF = 48F;
            this.BottomMargin.Name = "BottomMargin";
            // 
            // pageInfo2
            // 
            this.pageInfo2.LocationFloat = new DevExpress.Utils.PointFloat(550.0834F, 10.00001F);
            this.pageInfo2.Name = "pageInfo2";
            this.pageInfo2.SizeF = new System.Drawing.SizeF(256.9167F, 23F);
            this.pageInfo2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.pageInfo2.TextFormatString = "Page {0} of {1}";
            // 
            // Detail
            // 
            this.Detail.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.Detail.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.Detail.BorderWidth = 1F;
            this.Detail.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLine2,
            this.xrTable41,
            this.xrTable40,
            this.xrTable38,
            this.xrTable39,
            this.xrTable36,
            this.xrTable37,
            this.xrTable35,
            this.xrTable34,
            this.xrTable32,
            this.xrTable33,
            this.xrTable30,
            this.xrTable31,
            this.xrTable28,
            this.xrTable29,
            this.xrTable26,
            this.xrTable27,
            this.xrTable24,
            this.xrTable25,
            this.xrTable22,
            this.xrTable23,
            this.xrTable20,
            this.xrTable21,
            this.xrTable17,
            this.xrTable18,
            this.xrTable19,
            this.xrTable15,
            this.xrTable12,
            this.xrTable16,
            this.xrTable2,
            this.xrTable14,
            this.xrTable11,
            this.xrTable10,
            this.xrTable9,
            this.xrTable4,
            this.xrTable1,
            this.xrTable3,
            this.xrTable6,
            this.xrTable5,
            this.xrTable13,
            this.xrTable8});
            this.Detail.HeightF = 4409.081F;
            this.Detail.Name = "Detail";
            this.Detail.StylePriority.UseBorderDashStyle = false;
            this.Detail.StylePriority.UseBorderWidth = false;
            this.Detail.StylePriority.UseTextAlignment = false;
            this.Detail.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLine2
            // 
            this.xrLine2.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.xrLine2.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLine2.LocationFloat = new DevExpress.Utils.PointFloat(9.468443F, 4294.575F);
            this.xrLine2.Name = "xrLine2";
            this.xrLine2.SizeF = new System.Drawing.SizeF(787F, 12.58336F);
            this.xrLine2.StylePriority.UseBorderColor = false;
            this.xrLine2.StylePriority.UseBorders = false;
            // 
            // xrTable41
            // 
            this.xrTable41.BackColor = System.Drawing.Color.LightGray;
            this.xrTable41.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.xrTable41.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable41.Font = new DevExpress.Drawing.DXFont("Arial", 15F);
            this.xrTable41.ForeColor = System.Drawing.Color.Black;
            this.xrTable41.LocationFloat = new DevExpress.Utils.PointFloat(11.03187F, 4341.613F);
            this.xrTable41.Name = "xrTable41";
            this.xrTable41.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable41.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow89});
            this.xrTable41.SizeF = new System.Drawing.SizeF(785.2698F, 58.61377F);
            this.xrTable41.StylePriority.UseBackColor = false;
            this.xrTable41.StylePriority.UseBorderColor = false;
            this.xrTable41.StylePriority.UseBorders = false;
            this.xrTable41.StylePriority.UseFont = false;
            this.xrTable41.StylePriority.UseForeColor = false;
            // 
            // xrTableRow89
            // 
            this.xrTableRow89.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell177,
            this.xrTableCell178,
            this.xrTableCell4});
            this.xrTableRow89.Name = "xrTableRow89";
            this.xrTableRow89.Weight = 1D;
            // 
            // xrTableCell177
            // 
            this.xrTableCell177.BackColor = System.Drawing.Color.Transparent;
            this.xrTableCell177.BorderColor = System.Drawing.Color.Silver;
            this.xrTableCell177.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CompanyArName]")});
            this.xrTableCell177.Multiline = true;
            this.xrTableCell177.Name = "xrTableCell177";
            this.xrTableCell177.StylePriority.UseBackColor = false;
            this.xrTableCell177.StylePriority.UseBorderColor = false;
            this.xrTableCell177.StylePriority.UseTextAlignment = false;
            this.xrTableCell177.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter;
            this.xrTableCell177.Weight = 2.1889331156407765D;
            // 
            // xrTableCell178
            // 
            this.xrTableCell178.BackColor = System.Drawing.Color.Transparent;
            this.xrTableCell178.BorderColor = System.Drawing.Color.Silver;
            this.xrTableCell178.Multiline = true;
            this.xrTableCell178.Name = "xrTableCell178";
            this.xrTableCell178.StylePriority.UseBackColor = false;
            this.xrTableCell178.StylePriority.UseBorderColor = false;
            this.xrTableCell178.StylePriority.UseTextAlignment = false;
            this.xrTableCell178.Text = "الموظف :";
            this.xrTableCell178.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrTableCell178.Weight = 0.81170924345407236D;
            // 
            // xrTableCell4
            // 
            this.xrTableCell4.BackColor = System.Drawing.Color.Transparent;
            this.xrTableCell4.BorderColor = System.Drawing.Color.Silver;
            this.xrTableCell4.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[EmployeeName]")});
            this.xrTableCell4.Multiline = true;
            this.xrTableCell4.Name = "xrTableCell4";
            this.xrTableCell4.StylePriority.UseBackColor = false;
            this.xrTableCell4.StylePriority.UseBorderColor = false;
            this.xrTableCell4.StylePriority.UseTextAlignment = false;
            this.xrTableCell4.Text = "xrTableCell4";
            this.xrTableCell4.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrTableCell4.Weight = 1.4332872433639632D;
            // 
            // xrTable40
            // 
            this.xrTable40.BackColor = System.Drawing.Color.LightGray;
            this.xrTable40.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.xrTable40.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable40.Font = new DevExpress.Drawing.DXFont("Arial", 15F);
            this.xrTable40.ForeColor = System.Drawing.Color.Black;
            this.xrTable40.LocationFloat = new DevExpress.Utils.PointFloat(9.733971F, 4307.159F);
            this.xrTable40.Name = "xrTable40";
            this.xrTable40.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable40.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow88});
            this.xrTable40.SizeF = new System.Drawing.SizeF(786.6353F, 34.4541F);
            this.xrTable40.StylePriority.UseBackColor = false;
            this.xrTable40.StylePriority.UseBorderColor = false;
            this.xrTable40.StylePriority.UseBorders = false;
            this.xrTable40.StylePriority.UseFont = false;
            this.xrTable40.StylePriority.UseForeColor = false;
            // 
            // xrTableRow88
            // 
            this.xrTableRow88.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell175,
            this.xrTableCell176});
            this.xrTableRow88.Name = "xrTableRow88";
            this.xrTableRow88.Weight = 0.40594401169011973D;
            // 
            // xrTableCell175
            // 
            this.xrTableCell175.BackColor = System.Drawing.Color.Transparent;
            this.xrTableCell175.BorderColor = System.Drawing.Color.Silver;
            this.xrTableCell175.Font = new DevExpress.Drawing.DXFont("Arial", 15F, ((DevExpress.Drawing.DXFontStyle)((DevExpress.Drawing.DXFontStyle.Bold | DevExpress.Drawing.DXFontStyle.Underline))), DevExpress.Drawing.DXGraphicsUnit.Point, new DevExpress.Drawing.DXFontAdditionalProperty[] {new DevExpress.Drawing.DXFontAdditionalProperty("GdiCharSet", ((byte)(0)))});
            this.xrTableCell175.Multiline = true;
            this.xrTableCell175.Name = "xrTableCell175";
            this.xrTableCell175.StylePriority.UseBackColor = false;
            this.xrTableCell175.StylePriority.UseBorderColor = false;
            this.xrTableCell175.StylePriority.UseFont = false;
            this.xrTableCell175.StylePriority.UseTextAlignment = false;
            this.xrTableCell175.Text = "الطرف الأول (First Party)";
            this.xrTableCell175.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter;
            this.xrTableCell175.Weight = 1.4843630411355744D;
            // 
            // xrTableCell176
            // 
            this.xrTableCell176.BackColor = System.Drawing.Color.Transparent;
            this.xrTableCell176.BorderColor = System.Drawing.Color.Silver;
            this.xrTableCell176.Font = new DevExpress.Drawing.DXFont("Arial", 14.25F, ((DevExpress.Drawing.DXFontStyle)((DevExpress.Drawing.DXFontStyle.Bold | DevExpress.Drawing.DXFontStyle.Underline))), DevExpress.Drawing.DXGraphicsUnit.Point, new DevExpress.Drawing.DXFontAdditionalProperty[] {new DevExpress.Drawing.DXFontAdditionalProperty("GdiCharSet", ((byte)(0)))});
            this.xrTableCell176.Multiline = true;
            this.xrTableCell176.Name = "xrTableCell176";
            this.xrTableCell176.StylePriority.UseBackColor = false;
            this.xrTableCell176.StylePriority.UseBorderColor = false;
            this.xrTableCell176.StylePriority.UseFont = false;
            this.xrTableCell176.StylePriority.UseTextAlignment = false;
            this.xrTableCell176.Text = "الطرف الثاني (Second Party)";
            this.xrTableCell176.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter;
            this.xrTableCell176.Weight = 1.516915983783891D;
            // 
            // xrTable38
            // 
            this.xrTable38.BackColor = System.Drawing.Color.LightGray;
            this.xrTable38.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.xrTable38.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable38.Font = new DevExpress.Drawing.DXFont("Arial", 15F);
            this.xrTable38.ForeColor = System.Drawing.Color.Black;
            this.xrTable38.LocationFloat = new DevExpress.Utils.PointFloat(13.79399F, 3985.297F);
            this.xrTable38.Name = "xrTable38";
            this.xrTable38.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable38.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow84});
            this.xrTable38.SizeF = new System.Drawing.SizeF(786.4684F, 29.20166F);
            this.xrTable38.StylePriority.UseBackColor = false;
            this.xrTable38.StylePriority.UseBorderColor = false;
            this.xrTable38.StylePriority.UseBorders = false;
            this.xrTable38.StylePriority.UseFont = false;
            this.xrTable38.StylePriority.UseForeColor = false;
            // 
            // xrTableRow84
            // 
            this.xrTableRow84.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell167,
            this.xrTableCell168});
            this.xrTableRow84.Name = "xrTableRow84";
            this.xrTableRow84.Weight = 1D;
            // 
            // xrTableCell167
            // 
            this.xrTableCell167.BorderColor = System.Drawing.Color.Silver;
            this.xrTableCell167.Multiline = true;
            this.xrTableCell167.Name = "xrTableCell167";
            this.xrTableCell167.StylePriority.UseBorderColor = false;
            this.xrTableCell167.StylePriority.UseTextAlignment = false;
            this.xrTableCell167.Text = "البند التاسع: الإخطارات ونسخ العقد";
            this.xrTableCell167.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrTableCell167.Weight = 1.4843630411355744D;
            // 
            // xrTableCell168
            // 
            this.xrTableCell168.BorderColor = System.Drawing.Color.Silver;
            this.xrTableCell168.Multiline = true;
            this.xrTableCell168.Name = "xrTableCell168";
            this.xrTableCell168.StylePriority.UseBorderColor = false;
            this.xrTableCell168.StylePriority.UseTextAlignment = false;
            this.xrTableCell168.Text = "Article (9): Notices and Contract Copies";
            this.xrTableCell168.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell168.Weight = 1.5162793179592744D;
            // 
            // xrTable39
            // 
            this.xrTable39.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.xrTable39.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable39.LocationFloat = new DevExpress.Utils.PointFloat(15.09171F, 4014.499F);
            this.xrTable39.Name = "xrTable39";
            this.xrTable39.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable39.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow85,
            this.xrTableRow86,
            this.xrTableRow87});
            this.xrTable39.SizeF = new System.Drawing.SizeF(785.49F, 280.0757F);
            this.xrTable39.StylePriority.UseBorderColor = false;
            this.xrTable39.StylePriority.UseBorders = false;
            // 
            // xrTableRow85
            // 
            this.xrTableRow85.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell169,
            this.xrTableCell170});
            this.xrTableRow85.Name = "xrTableRow85";
            this.xrTableRow85.Weight = 0.5482096770438093D;
            // 
            // xrTableCell169
            // 
            this.xrTableCell169.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.xrTableCell169.Multiline = true;
            this.xrTableCell169.Name = "xrTableCell169";
            this.xrTableCell169.StylePriority.UseFont = false;
            this.xrTableCell169.StylePriority.UseTextAlignment = false;
            this.xrTableCell169.Text = resources.GetString("xrTableCell169.Text");
            this.xrTableCell169.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrTableCell169.Weight = 0.88690134707848545D;
            // 
            // xrTableCell170
            // 
            this.xrTableCell170.Font = new DevExpress.Drawing.DXFont("Arial", 10F);
            this.xrTableCell170.Multiline = true;
            this.xrTableCell170.Name = "xrTableCell170";
            this.xrTableCell170.StylePriority.UseFont = false;
            this.xrTableCell170.StylePriority.UseTextAlignment = false;
            this.xrTableCell170.Text = resources.GetString("xrTableCell170.Text");
            this.xrTableCell170.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell170.Weight = 0.90685184690168552D;
            // 
            // xrTableRow86
            // 
            this.xrTableRow86.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell171,
            this.xrTableCell172});
            this.xrTableRow86.Name = "xrTableRow86";
            this.xrTableRow86.Weight = 0.3282128637360362D;
            // 
            // xrTableCell171
            // 
            this.xrTableCell171.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.xrTableCell171.Multiline = true;
            this.xrTableCell171.Name = "xrTableCell171";
            this.xrTableCell171.StylePriority.UseFont = false;
            this.xrTableCell171.StylePriority.UseTextAlignment = false;
            this.xrTableCell171.Text = "حرر هذا العقد من نسختين اصليتين واحتفظ كل طرف بنسخة للعمل بها\r\nلغة العقد (عربي) و" +
    "في حال تعارض النصين فإن النص العربي هو النص المعتمد والمعتبر حسب أحكام نظام العم" +
    "ل\r\n";
            this.xrTableCell171.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrTableCell171.Weight = 0.88690134707848545D;
            // 
            // xrTableCell172
            // 
            this.xrTableCell172.Font = new DevExpress.Drawing.DXFont("Arial", 10F);
            this.xrTableCell172.Multiline = true;
            this.xrTableCell172.Name = "xrTableCell172";
            this.xrTableCell172.StylePriority.UseFont = false;
            this.xrTableCell172.StylePriority.UseTextAlignment = false;
            this.xrTableCell172.Text = resources.GetString("xrTableCell172.Text");
            this.xrTableCell172.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell172.Weight = 0.90685184690168552D;
            // 
            // xrTableRow87
            // 
            this.xrTableRow87.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell173,
            this.xrTableCell174});
            this.xrTableRow87.Font = new DevExpress.Drawing.DXFont("Arial", 12F, ((DevExpress.Drawing.DXFontStyle)((DevExpress.Drawing.DXFontStyle.Bold | DevExpress.Drawing.DXFontStyle.Underline))), DevExpress.Drawing.DXGraphicsUnit.Point, new DevExpress.Drawing.DXFontAdditionalProperty[] {new DevExpress.Drawing.DXFontAdditionalProperty("GdiCharSet", ((byte)(0)))});
            this.xrTableRow87.Name = "xrTableRow87";
            this.xrTableRow87.StylePriority.UseFont = false;
            this.xrTableRow87.Weight = 0.17106784778203035D;
            // 
            // xrTableCell173
            // 
            this.xrTableCell173.Font = new DevExpress.Drawing.DXFont("Arial", 12F, ((DevExpress.Drawing.DXFontStyle)((DevExpress.Drawing.DXFontStyle.Bold | DevExpress.Drawing.DXFontStyle.Underline))), DevExpress.Drawing.DXGraphicsUnit.Point, new DevExpress.Drawing.DXFontAdditionalProperty[] {new DevExpress.Drawing.DXFontAdditionalProperty("GdiCharSet", ((byte)(0)))});
            this.xrTableCell173.Multiline = true;
            this.xrTableCell173.Name = "xrTableCell173";
            this.xrTableCell173.StylePriority.UseFont = false;
            this.xrTableCell173.StylePriority.UseTextAlignment = false;
            this.xrTableCell173.Text = "ملاحظة: إن أي تـــــعديـل أو كشــــــط علـــــى هذا الــــعقد يجعله لاغي";
            this.xrTableCell173.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrTableCell173.Weight = 0.88690134707848545D;
            // 
            // xrTableCell174
            // 
            this.xrTableCell174.Font = new DevExpress.Drawing.DXFont("Arial", 12F, ((DevExpress.Drawing.DXFontStyle)((DevExpress.Drawing.DXFontStyle.Bold | DevExpress.Drawing.DXFontStyle.Underline))), DevExpress.Drawing.DXGraphicsUnit.Point, new DevExpress.Drawing.DXFontAdditionalProperty[] {new DevExpress.Drawing.DXFontAdditionalProperty("GdiCharSet", ((byte)(0)))});
            this.xrTableCell174.Multiline = true;
            this.xrTableCell174.Name = "xrTableCell174";
            this.xrTableCell174.StylePriority.UseFont = false;
            this.xrTableCell174.StylePriority.UseTextAlignment = false;
            this.xrTableCell174.Text = "Note: Any deletion or alteration effected to this Contract will render it invalid" +
    ".";
            this.xrTableCell174.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell174.Weight = 0.90685184690168552D;
            // 
            // xrTable36
            // 
            this.xrTable36.BackColor = System.Drawing.Color.LightGray;
            this.xrTable36.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.xrTable36.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable36.Font = new DevExpress.Drawing.DXFont("Arial", 15F);
            this.xrTable36.ForeColor = System.Drawing.Color.Black;
            this.xrTable36.LocationFloat = new DevExpress.Utils.PointFloat(12.49628F, 3678.693F);
            this.xrTable36.Name = "xrTable36";
            this.xrTable36.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable36.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow81});
            this.xrTable36.SizeF = new System.Drawing.SizeF(786.4684F, 48.10938F);
            this.xrTable36.StylePriority.UseBackColor = false;
            this.xrTable36.StylePriority.UseBorderColor = false;
            this.xrTable36.StylePriority.UseBorders = false;
            this.xrTable36.StylePriority.UseFont = false;
            this.xrTable36.StylePriority.UseForeColor = false;
            // 
            // xrTableRow81
            // 
            this.xrTableRow81.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell161,
            this.xrTableCell162});
            this.xrTableRow81.Name = "xrTableRow81";
            this.xrTableRow81.Weight = 1D;
            // 
            // xrTableCell161
            // 
            this.xrTableCell161.BorderColor = System.Drawing.Color.Silver;
            this.xrTableCell161.Multiline = true;
            this.xrTableCell161.Name = "xrTableCell161";
            this.xrTableCell161.StylePriority.UseBorderColor = false;
            this.xrTableCell161.StylePriority.UseTextAlignment = false;
            this.xrTableCell161.Text = "البند الثامن: النظام الواجب التطبيق والاختصاص القضائي";
            this.xrTableCell161.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrTableCell161.Weight = 1.4843630411355744D;
            // 
            // xrTableCell162
            // 
            this.xrTableCell162.BorderColor = System.Drawing.Color.Silver;
            this.xrTableCell162.Multiline = true;
            this.xrTableCell162.Name = "xrTableCell162";
            this.xrTableCell162.StylePriority.UseBorderColor = false;
            this.xrTableCell162.StylePriority.UseTextAlignment = false;
            this.xrTableCell162.Text = "Article (8): Applicable Law and Specialized Jurisdiction";
            this.xrTableCell162.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell162.Weight = 1.5162793179592744D;
            // 
            // xrTable37
            // 
            this.xrTable37.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.xrTable37.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable37.LocationFloat = new DevExpress.Utils.PointFloat(13.79399F, 3726.802F);
            this.xrTable37.Name = "xrTable37";
            this.xrTable37.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable37.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow82,
            this.xrTableRow83});
            this.xrTable37.SizeF = new System.Drawing.SizeF(785.1707F, 258.4946F);
            this.xrTable37.StylePriority.UseBorderColor = false;
            this.xrTable37.StylePriority.UseBorders = false;
            // 
            // xrTableRow82
            // 
            this.xrTableRow82.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell163,
            this.xrTableCell164});
            this.xrTableRow82.Name = "xrTableRow82";
            this.xrTableRow82.Weight = 0.571780242416883D;
            // 
            // xrTableCell163
            // 
            this.xrTableCell163.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.xrTableCell163.Multiline = true;
            this.xrTableCell163.Name = "xrTableCell163";
            this.xrTableCell163.StylePriority.UseFont = false;
            this.xrTableCell163.StylePriority.UseTextAlignment = false;
            this.xrTableCell163.Text = resources.GetString("xrTableCell163.Text");
            this.xrTableCell163.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrTableCell163.Weight = 0.88690134707848545D;
            // 
            // xrTableCell164
            // 
            this.xrTableCell164.Font = new DevExpress.Drawing.DXFont("Arial", 10F);
            this.xrTableCell164.Multiline = true;
            this.xrTableCell164.Name = "xrTableCell164";
            this.xrTableCell164.StylePriority.UseFont = false;
            this.xrTableCell164.StylePriority.UseTextAlignment = false;
            this.xrTableCell164.Text = resources.GetString("xrTableCell164.Text");
            this.xrTableCell164.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell164.Weight = 0.906122677661831D;
            // 
            // xrTableRow83
            // 
            this.xrTableRow83.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell165,
            this.xrTableCell166});
            this.xrTableRow83.Name = "xrTableRow83";
            this.xrTableRow83.Weight = 0.39499643665718753D;
            // 
            // xrTableCell165
            // 
            this.xrTableCell165.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.xrTableCell165.Multiline = true;
            this.xrTableCell165.Name = "xrTableCell165";
            this.xrTableCell165.StylePriority.UseFont = false;
            this.xrTableCell165.StylePriority.UseTextAlignment = false;
            this.xrTableCell165.Text = resources.GetString("xrTableCell165.Text");
            this.xrTableCell165.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrTableCell165.Weight = 0.88690134707848545D;
            // 
            // xrTableCell166
            // 
            this.xrTableCell166.Font = new DevExpress.Drawing.DXFont("Arial", 10F);
            this.xrTableCell166.Multiline = true;
            this.xrTableCell166.Name = "xrTableCell166";
            this.xrTableCell166.StylePriority.UseFont = false;
            this.xrTableCell166.StylePriority.UseTextAlignment = false;
            this.xrTableCell166.Text = resources.GetString("xrTableCell166.Text");
            this.xrTableCell166.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell166.Weight = 0.906122677661831D;
            // 
            // xrTable35
            // 
            this.xrTable35.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.xrTable35.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable35.LocationFloat = new DevExpress.Utils.PointFloat(12.49628F, 3422.298F);
            this.xrTable35.Name = "xrTable35";
            this.xrTable35.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable35.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow79,
            this.xrTableRow80});
            this.xrTable35.SizeF = new System.Drawing.SizeF(785.1707F, 256.3943F);
            this.xrTable35.StylePriority.UseBorderColor = false;
            this.xrTable35.StylePriority.UseBorders = false;
            // 
            // xrTableRow79
            // 
            this.xrTableRow79.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell157,
            this.xrTableCell158});
            this.xrTableRow79.Name = "xrTableRow79";
            this.xrTableRow79.Weight = 0.5482096770438093D;
            // 
            // xrTableCell157
            // 
            this.xrTableCell157.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.xrTableCell157.Multiline = true;
            this.xrTableCell157.Name = "xrTableCell157";
            this.xrTableCell157.StylePriority.UseFont = false;
            this.xrTableCell157.StylePriority.UseTextAlignment = false;
            this.xrTableCell157.Text = resources.GetString("xrTableCell157.Text");
            this.xrTableCell157.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrTableCell157.Weight = 0.88690134707848545D;
            // 
            // xrTableCell158
            // 
            this.xrTableCell158.Font = new DevExpress.Drawing.DXFont("Arial", 10F);
            this.xrTableCell158.Multiline = true;
            this.xrTableCell158.Name = "xrTableCell158";
            this.xrTableCell158.StylePriority.UseFont = false;
            this.xrTableCell158.StylePriority.UseTextAlignment = false;
            this.xrTableCell158.Text = resources.GetString("xrTableCell158.Text");
            this.xrTableCell158.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell158.Weight = 0.906122677661831D;
            // 
            // xrTableRow80
            // 
            this.xrTableRow80.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell159,
            this.xrTableCell160});
            this.xrTableRow80.Name = "xrTableRow80";
            this.xrTableRow80.Weight = 0.4107116687264511D;
            // 
            // xrTableCell159
            // 
            this.xrTableCell159.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.xrTableCell159.Multiline = true;
            this.xrTableCell159.Name = "xrTableCell159";
            this.xrTableCell159.StylePriority.UseFont = false;
            this.xrTableCell159.StylePriority.UseTextAlignment = false;
            this.xrTableCell159.Text = resources.GetString("xrTableCell159.Text");
            this.xrTableCell159.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrTableCell159.Weight = 0.88690134707848545D;
            // 
            // xrTableCell160
            // 
            this.xrTableCell160.Font = new DevExpress.Drawing.DXFont("Arial", 10F);
            this.xrTableCell160.Multiline = true;
            this.xrTableCell160.Name = "xrTableCell160";
            this.xrTableCell160.StylePriority.UseFont = false;
            this.xrTableCell160.StylePriority.UseTextAlignment = false;
            this.xrTableCell160.Text = resources.GetString("xrTableCell160.Text");
            this.xrTableCell160.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell160.Weight = 0.906122677661831D;
            // 
            // xrTable34
            // 
            this.xrTable34.BackColor = System.Drawing.Color.LightGray;
            this.xrTable34.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.xrTable34.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable34.Font = new DevExpress.Drawing.DXFont("Arial", 15F);
            this.xrTable34.ForeColor = System.Drawing.Color.Black;
            this.xrTable34.LocationFloat = new DevExpress.Utils.PointFloat(11.19856F, 3393.096F);
            this.xrTable34.Name = "xrTable34";
            this.xrTable34.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable34.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow78});
            this.xrTable34.SizeF = new System.Drawing.SizeF(786.4684F, 29.20166F);
            this.xrTable34.StylePriority.UseBackColor = false;
            this.xrTable34.StylePriority.UseBorderColor = false;
            this.xrTable34.StylePriority.UseBorders = false;
            this.xrTable34.StylePriority.UseFont = false;
            this.xrTable34.StylePriority.UseForeColor = false;
            // 
            // xrTableRow78
            // 
            this.xrTableRow78.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell155,
            this.xrTableCell156});
            this.xrTableRow78.Name = "xrTableRow78";
            this.xrTableRow78.Weight = 1D;
            // 
            // xrTableCell155
            // 
            this.xrTableCell155.BorderColor = System.Drawing.Color.Silver;
            this.xrTableCell155.Multiline = true;
            this.xrTableCell155.Name = "xrTableCell155";
            this.xrTableCell155.StylePriority.UseBorderColor = false;
            this.xrTableCell155.StylePriority.UseTextAlignment = false;
            this.xrTableCell155.Text = "البند السابع: مكافأة نهاية الخدمة";
            this.xrTableCell155.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrTableCell155.Weight = 1.4843630411355744D;
            // 
            // xrTableCell156
            // 
            this.xrTableCell156.BorderColor = System.Drawing.Color.Silver;
            this.xrTableCell156.Multiline = true;
            this.xrTableCell156.Name = "xrTableCell156";
            this.xrTableCell156.StylePriority.UseBorderColor = false;
            this.xrTableCell156.StylePriority.UseTextAlignment = false;
            this.xrTableCell156.Text = "Article (7): The End of Service Award";
            this.xrTableCell156.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell156.Weight = 1.5162793179592744D;
            // 
            // xrTable32
            // 
            this.xrTable32.BackColor = System.Drawing.Color.LightGray;
            this.xrTable32.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.xrTable32.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable32.Font = new DevExpress.Drawing.DXFont("Arial", 15F);
            this.xrTable32.ForeColor = System.Drawing.Color.Black;
            this.xrTable32.LocationFloat = new DevExpress.Utils.PointFloat(11.19856F, 3126.316F);
            this.xrTable32.Name = "xrTable32";
            this.xrTable32.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable32.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow73});
            this.xrTable32.SizeF = new System.Drawing.SizeF(786.4684F, 51.2605F);
            this.xrTable32.StylePriority.UseBackColor = false;
            this.xrTable32.StylePriority.UseBorderColor = false;
            this.xrTable32.StylePriority.UseBorders = false;
            this.xrTable32.StylePriority.UseFont = false;
            this.xrTable32.StylePriority.UseForeColor = false;
            // 
            // xrTableRow73
            // 
            this.xrTableRow73.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell145,
            this.xrTableCell146});
            this.xrTableRow73.Name = "xrTableRow73";
            this.xrTableRow73.Weight = 1D;
            // 
            // xrTableCell145
            // 
            this.xrTableCell145.BorderColor = System.Drawing.Color.Silver;
            this.xrTableCell145.Multiline = true;
            this.xrTableCell145.Name = "xrTableCell145";
            this.xrTableCell145.StylePriority.UseBorderColor = false;
            this.xrTableCell145.StylePriority.UseTextAlignment = false;
            this.xrTableCell145.Text = "البند السادس: انتهاء العقد او إنهاءه";
            this.xrTableCell145.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrTableCell145.Weight = 1.4843630411355744D;
            // 
            // xrTableCell146
            // 
            this.xrTableCell146.BorderColor = System.Drawing.Color.Silver;
            this.xrTableCell146.Multiline = true;
            this.xrTableCell146.Name = "xrTableCell146";
            this.xrTableCell146.StylePriority.UseBorderColor = false;
            this.xrTableCell146.StylePriority.UseTextAlignment = false;
            this.xrTableCell146.Text = "Article (6):Contract Expiration and Termination";
            this.xrTableCell146.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell146.Weight = 1.5162793179592744D;
            // 
            // xrTable33
            // 
            this.xrTable33.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.xrTable33.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable33.LocationFloat = new DevExpress.Utils.PointFloat(10.36502F, 3177.576F);
            this.xrTable33.Name = "xrTable33";
            this.xrTable33.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable33.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow74,
            this.xrTableRow75,
            this.xrTableRow76,
            this.xrTableRow77});
            this.xrTable33.SizeF = new System.Drawing.SizeF(785.1707F, 215.5195F);
            this.xrTable33.StylePriority.UseBorderColor = false;
            this.xrTable33.StylePriority.UseBorders = false;
            // 
            // xrTableRow74
            // 
            this.xrTableRow74.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell147,
            this.xrTableCell148});
            this.xrTableRow74.Name = "xrTableRow74";
            this.xrTableRow74.Weight = 0.14749271694731336D;
            // 
            // xrTableCell147
            // 
            this.xrTableCell147.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.xrTableCell147.Multiline = true;
            this.xrTableCell147.Name = "xrTableCell147";
            this.xrTableCell147.StylePriority.UseFont = false;
            this.xrTableCell147.StylePriority.UseTextAlignment = false;
            this.xrTableCell147.Text = "اتفق الطرفان على أنه في حال فسخ العقد دون سبب مشروع على ما يلي";
            this.xrTableCell147.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrTableCell147.Weight = 0.88690134707848545D;
            // 
            // xrTableCell148
            // 
            this.xrTableCell148.Font = new DevExpress.Drawing.DXFont("Arial", 10F);
            this.xrTableCell148.Multiline = true;
            this.xrTableCell148.Name = "xrTableCell148";
            this.xrTableCell148.StylePriority.UseFont = false;
            this.xrTableCell148.StylePriority.UseTextAlignment = false;
            this.xrTableCell148.Text = "If the contract is terminated without a legitimate reason, the parties agree upon" +
    " the followings:";
            this.xrTableCell148.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell148.Weight = 0.906122677661831D;
            // 
            // xrTableRow75
            // 
            this.xrTableRow75.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell149,
            this.xrTableCell150});
            this.xrTableRow75.Name = "xrTableRow75";
            this.xrTableRow75.Weight = 0.25749660215517195D;
            // 
            // xrTableCell149
            // 
            this.xrTableCell149.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.xrTableCell149.Multiline = true;
            this.xrTableCell149.Name = "xrTableCell149";
            this.xrTableCell149.StylePriority.UseFont = false;
            this.xrTableCell149.StylePriority.UseTextAlignment = false;
            this.xrTableCell149.Text = "اتفق الطرفان انه في حال فسخ العقد من قبل أحد الطرفين دون سبب مشروع يحق للطرف الاخ" +
    "ر مقابل الانهاء تعويضا قدره شهرين من اجر الموظف";
            this.xrTableCell149.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrTableCell149.Weight = 0.88690134707848545D;
            // 
            // xrTableCell150
            // 
            this.xrTableCell150.Font = new DevExpress.Drawing.DXFont("Arial", 10F);
            this.xrTableCell150.Multiline = true;
            this.xrTableCell150.Name = "xrTableCell150";
            this.xrTableCell150.StylePriority.UseFont = false;
            this.xrTableCell150.StylePriority.UseTextAlignment = false;
            this.xrTableCell150.Text = resources.GetString("xrTableCell150.Text");
            this.xrTableCell150.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell150.Weight = 0.906122677661831D;
            // 
            // xrTableRow76
            // 
            this.xrTableRow76.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell151,
            this.xrTableCell152});
            this.xrTableRow76.Name = "xrTableRow76";
            this.xrTableRow76.Weight = 0.19856471016851562D;
            // 
            // xrTableCell151
            // 
            this.xrTableCell151.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.xrTableCell151.Multiline = true;
            this.xrTableCell151.Name = "xrTableCell151";
            this.xrTableCell151.StylePriority.UseFont = false;
            this.xrTableCell151.StylePriority.UseTextAlignment = false;
            this.xrTableCell151.Text = "يحق للطرف الأول فسخ العقد بدون أي مكافأة او اشعار للموظف او تعويض في حال مخالفة ن" +
    "ص المادة ثمانون من نظام العمل";
            this.xrTableCell151.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrTableCell151.Weight = 0.88690134707848545D;
            // 
            // xrTableCell152
            // 
            this.xrTableCell152.Font = new DevExpress.Drawing.DXFont("Arial", 10F);
            this.xrTableCell152.Multiline = true;
            this.xrTableCell152.Name = "xrTableCell152";
            this.xrTableCell152.StylePriority.UseFont = false;
            this.xrTableCell152.StylePriority.UseTextAlignment = false;
            this.xrTableCell152.Text = "First Party shall have the right to terminate the contract without any remunerati" +
    "on or notice to the employee or compensation in the event of the violation of Ar" +
    "ticle 80 of the Labor Law.";
            this.xrTableCell152.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell152.Weight = 0.906122677661831D;
            // 
            // xrTableRow77
            // 
            this.xrTableRow77.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell153,
            this.xrTableCell154});
            this.xrTableRow77.Name = "xrTableRow77";
            this.xrTableRow77.Weight = 0.20249465955124235D;
            // 
            // xrTableCell153
            // 
            this.xrTableCell153.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.xrTableCell153.Multiline = true;
            this.xrTableCell153.Name = "xrTableCell153";
            this.xrTableCell153.StylePriority.UseFont = false;
            this.xrTableCell153.StylePriority.UseTextAlignment = false;
            this.xrTableCell153.Text = "يحق للموظف ترك العمل مع الاحتفاظ بكامل الحقوق في حال مخالفة الطرف الأول لنص الماد" +
    "ة واحد وثمانون من نظام العمل";
            this.xrTableCell153.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrTableCell153.Weight = 0.88690134707848545D;
            // 
            // xrTableCell154
            // 
            this.xrTableCell154.Font = new DevExpress.Drawing.DXFont("Arial", 10F);
            this.xrTableCell154.Multiline = true;
            this.xrTableCell154.Name = "xrTableCell154";
            this.xrTableCell154.StylePriority.UseFont = false;
            this.xrTableCell154.StylePriority.UseTextAlignment = false;
            this.xrTableCell154.Text = "The Employee shall have the right to leave the job with retaining full rights in " +
    "the event of violation of the Article 81 of the Labor Law.";
            this.xrTableCell154.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell154.Weight = 0.906122677661831D;
            // 
            // xrTable30
            // 
            this.xrTable30.BackColor = System.Drawing.Color.LightGray;
            this.xrTable30.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.xrTable30.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable30.Font = new DevExpress.Drawing.DXFont("Arial", 15F);
            this.xrTable30.ForeColor = System.Drawing.Color.Black;
            this.xrTable30.LocationFloat = new DevExpress.Utils.PointFloat(11.11522F, 2231.589F);
            this.xrTable30.Name = "xrTable30";
            this.xrTable30.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable30.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow61});
            this.xrTable30.SizeF = new System.Drawing.SizeF(786.4684F, 24.99997F);
            this.xrTable30.StylePriority.UseBackColor = false;
            this.xrTable30.StylePriority.UseBorderColor = false;
            this.xrTable30.StylePriority.UseBorders = false;
            this.xrTable30.StylePriority.UseFont = false;
            this.xrTable30.StylePriority.UseForeColor = false;
            // 
            // xrTableRow61
            // 
            this.xrTableRow61.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell121,
            this.xrTableCell122});
            this.xrTableRow61.Name = "xrTableRow61";
            this.xrTableRow61.Weight = 1D;
            // 
            // xrTableCell121
            // 
            this.xrTableCell121.BorderColor = System.Drawing.Color.Silver;
            this.xrTableCell121.Multiline = true;
            this.xrTableCell121.Name = "xrTableCell121";
            this.xrTableCell121.StylePriority.UseBorderColor = false;
            this.xrTableCell121.Text = "البند الخامس: التزامات الموظف";
            this.xrTableCell121.Weight = 1.4843630411355744D;
            // 
            // xrTableCell122
            // 
            this.xrTableCell122.BorderColor = System.Drawing.Color.Silver;
            this.xrTableCell122.Multiline = true;
            this.xrTableCell122.Name = "xrTableCell122";
            this.xrTableCell122.StylePriority.UseBorderColor = false;
            this.xrTableCell122.StylePriority.UseTextAlignment = false;
            this.xrTableCell122.Text = "Article (5): Obligation of Employee";
            this.xrTableCell122.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell122.Weight = 1.5162793179592744D;
            // 
            // xrTable31
            // 
            this.xrTable31.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.xrTable31.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable31.LocationFloat = new DevExpress.Utils.PointFloat(11.19856F, 2256.589F);
            this.xrTable31.Name = "xrTable31";
            this.xrTable31.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable31.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow62,
            this.xrTableRow63,
            this.xrTableRow64,
            this.xrTableRow65,
            this.xrTableRow66,
            this.xrTableRow68,
            this.xrTableRow72,
            this.xrTableRow71,
            this.xrTableRow70,
            this.xrTableRow69,
            this.xrTableRow67});
            this.xrTable31.SizeF = new System.Drawing.SizeF(784.8514F, 869.7268F);
            this.xrTable31.StylePriority.UseBorderColor = false;
            this.xrTable31.StylePriority.UseBorders = false;
            // 
            // xrTableRow62
            // 
            this.xrTableRow62.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell123,
            this.xrTableCell124});
            this.xrTableRow62.Name = "xrTableRow62";
            this.xrTableRow62.Weight = 0.31249397929745776D;
            // 
            // xrTableCell123
            // 
            this.xrTableCell123.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.xrTableCell123.Multiline = true;
            this.xrTableCell123.Name = "xrTableCell123";
            this.xrTableCell123.StylePriority.UseFont = false;
            this.xrTableCell123.StylePriority.UseTextAlignment = false;
            this.xrTableCell123.Text = "أن ينجز العمل الموكل أليه وفقاً لأصول المهنة، ووفق تعليمات الطرف الأول، إذا لم يك" +
    "ن في هذه التعليمات ما يخالف العقد، أو النظام، أو الآداب العامة، ولم يكن في تنفيذ" +
    "ها ما يعرضه للخطر.";
            this.xrTableCell123.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrTableCell123.Weight = 0.88690134707848545D;
            // 
            // xrTableCell124
            // 
            this.xrTableCell124.Font = new DevExpress.Drawing.DXFont("Arial", 10F);
            this.xrTableCell124.Multiline = true;
            this.xrTableCell124.Name = "xrTableCell124";
            this.xrTableCell124.StylePriority.UseFont = false;
            this.xrTableCell124.StylePriority.UseTextAlignment = false;
            this.xrTableCell124.Text = resources.GetString("xrTableCell124.Text");
            this.xrTableCell124.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell124.Weight = 0.90539364777417042D;
            // 
            // xrTableRow63
            // 
            this.xrTableRow63.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell125,
            this.xrTableCell126});
            this.xrTableRow63.Name = "xrTableRow63";
            this.xrTableRow63.Weight = 0.36356871179564637D;
            // 
            // xrTableCell125
            // 
            this.xrTableCell125.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.xrTableCell125.Multiline = true;
            this.xrTableCell125.Name = "xrTableCell125";
            this.xrTableCell125.StylePriority.UseFont = false;
            this.xrTableCell125.StylePriority.UseTextAlignment = false;
            this.xrTableCell125.Text = resources.GetString("xrTableCell125.Text");
            this.xrTableCell125.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrTableCell125.Weight = 0.88690134707848545D;
            // 
            // xrTableCell126
            // 
            this.xrTableCell126.Font = new DevExpress.Drawing.DXFont("Arial", 10F);
            this.xrTableCell126.Multiline = true;
            this.xrTableCell126.Name = "xrTableCell126";
            this.xrTableCell126.StylePriority.UseFont = false;
            this.xrTableCell126.StylePriority.UseTextAlignment = false;
            this.xrTableCell126.Text = resources.GetString("xrTableCell126.Text");
            this.xrTableCell126.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell126.Weight = 0.90539364777417042D;
            // 
            // xrTableRow64
            // 
            this.xrTableRow64.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell127,
            this.xrTableCell128});
            this.xrTableRow64.Name = "xrTableRow64";
            this.xrTableRow64.Weight = 0.19856471016851562D;
            // 
            // xrTableCell127
            // 
            this.xrTableCell127.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.xrTableCell127.Multiline = true;
            this.xrTableCell127.Name = "xrTableCell127";
            this.xrTableCell127.StylePriority.UseFont = false;
            this.xrTableCell127.StylePriority.UseTextAlignment = false;
            this.xrTableCell127.Text = "أن يقدم كل عون، ومساعدة دون أن يشترط لذلك أجراً إضافيا في حالات الأخطار التي تهدد" +
    " سلامة مكان العمل، أو الأشخاص العاملين فيه";
            this.xrTableCell127.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrTableCell127.Weight = 0.88690134707848545D;
            // 
            // xrTableCell128
            // 
            this.xrTableCell128.Font = new DevExpress.Drawing.DXFont("Arial", 10F);
            this.xrTableCell128.Multiline = true;
            this.xrTableCell128.Name = "xrTableCell128";
            this.xrTableCell128.StylePriority.UseFont = false;
            this.xrTableCell128.StylePriority.UseTextAlignment = false;
            this.xrTableCell128.Text = "The Employee shall extend all assistance and cooperation without requiring additi" +
    "onal wages in cases of threats of the safety of the place of work or persons who" +
    " work in such place.";
            this.xrTableCell128.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell128.Weight = 0.90539364777417042D;
            // 
            // xrTableRow65
            // 
            this.xrTableRow65.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell129,
            this.xrTableCell130});
            this.xrTableRow65.Name = "xrTableRow65";
            this.xrTableRow65.Weight = 0.37928120458792358D;
            // 
            // xrTableCell129
            // 
            this.xrTableCell129.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.xrTableCell129.Multiline = true;
            this.xrTableCell129.Name = "xrTableCell129";
            this.xrTableCell129.StylePriority.UseFont = false;
            this.xrTableCell129.StylePriority.UseTextAlignment = false;
            this.xrTableCell129.Text = resources.GetString("xrTableCell129.Text");
            this.xrTableCell129.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrTableCell129.Weight = 0.88690134707848545D;
            // 
            // xrTableCell130
            // 
            this.xrTableCell130.Font = new DevExpress.Drawing.DXFont("Arial", 10F);
            this.xrTableCell130.Multiline = true;
            this.xrTableCell130.Name = "xrTableCell130";
            this.xrTableCell130.StylePriority.UseFont = false;
            this.xrTableCell130.StylePriority.UseTextAlignment = false;
            this.xrTableCell130.Text = resources.GetString("xrTableCell130.Text");
            this.xrTableCell130.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell130.Weight = 0.90539364777417042D;
            // 
            // xrTableRow66
            // 
            this.xrTableRow66.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell131,
            this.xrTableCell132});
            this.xrTableRow66.Name = "xrTableRow66";
            this.xrTableRow66.Weight = 0.39106648727446036D;
            // 
            // xrTableCell131
            // 
            this.xrTableCell131.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.xrTableCell131.Multiline = true;
            this.xrTableCell131.Name = "xrTableCell131";
            this.xrTableCell131.StylePriority.UseFont = false;
            this.xrTableCell131.StylePriority.UseTextAlignment = false;
            this.xrTableCell131.Text = resources.GetString("xrTableCell131.Text");
            this.xrTableCell131.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrTableCell131.Weight = 0.88690134707848545D;
            // 
            // xrTableCell132
            // 
            this.xrTableCell132.Font = new DevExpress.Drawing.DXFont("Arial", 10F);
            this.xrTableCell132.Multiline = true;
            this.xrTableCell132.Name = "xrTableCell132";
            this.xrTableCell132.StylePriority.UseFont = false;
            this.xrTableCell132.StylePriority.UseTextAlignment = false;
            this.xrTableCell132.Text = resources.GetString("xrTableCell132.Text");
            this.xrTableCell132.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell132.Weight = 0.90539364777417042D;
            // 
            // xrTableRow68
            // 
            this.xrTableRow68.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell135,
            this.xrTableCell136});
            this.xrTableRow68.Name = "xrTableRow68";
            this.xrTableRow68.Weight = 0.25749477597051429D;
            // 
            // xrTableCell135
            // 
            this.xrTableCell135.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.xrTableCell135.Multiline = true;
            this.xrTableCell135.Name = "xrTableCell135";
            this.xrTableCell135.StylePriority.UseFont = false;
            this.xrTableCell135.StylePriority.UseTextAlignment = false;
            this.xrTableCell135.Text = "لا يجوز للموظف العمل لدى طرف ثالث بأجر أو بدون أجر بصورة مباشرة أو غير مباشرة أثن" +
    "اء مدة تنفيذ هذا العقد الا بموافقة خطية من الطرف الأول";
            this.xrTableCell135.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrTableCell135.Weight = 0.88690134707848545D;
            // 
            // xrTableCell136
            // 
            this.xrTableCell136.Font = new DevExpress.Drawing.DXFont("Arial", 10F);
            this.xrTableCell136.Multiline = true;
            this.xrTableCell136.Name = "xrTableCell136";
            this.xrTableCell136.StylePriority.UseFont = false;
            this.xrTableCell136.StylePriority.UseTextAlignment = false;
            this.xrTableCell136.Text = resources.GetString("xrTableCell136.Text");
            this.xrTableCell136.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell136.Weight = 0.90539364777417042D;
            // 
            // xrTableRow72
            // 
            this.xrTableRow72.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell143,
            this.xrTableCell144});
            this.xrTableRow72.Name = "xrTableRow72";
            this.xrTableRow72.Weight = 0.19856653635317306D;
            // 
            // xrTableCell143
            // 
            this.xrTableCell143.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.xrTableCell143.Multiline = true;
            this.xrTableCell143.Name = "xrTableCell143";
            this.xrTableCell143.StylePriority.UseFont = false;
            this.xrTableCell143.StylePriority.UseTextAlignment = false;
            this.xrTableCell143.Text = "يلتزم الموظف بان لا يقوم بعد انتهاء العقد بمنافسة الطرف الأول لمدة سنتين، وذلك في" +
    " اي مكان في المملكة العربية السعودية";
            this.xrTableCell143.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrTableCell143.Weight = 0.88690134707848545D;
            // 
            // xrTableCell144
            // 
            this.xrTableCell144.Font = new DevExpress.Drawing.DXFont("Arial", 10F);
            this.xrTableCell144.Multiline = true;
            this.xrTableCell144.Name = "xrTableCell144";
            this.xrTableCell144.StylePriority.UseFont = false;
            this.xrTableCell144.StylePriority.UseTextAlignment = false;
            this.xrTableCell144.Text = "The Employee shall not compete with First Party for (2) years after the expiratio" +
    "n of the Contract, at any place of the Kingdom of Saudi Arabia.";
            this.xrTableCell144.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell144.Weight = 0.90539364777417042D;
            // 
            // xrTableRow71
            // 
            this.xrTableRow71.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell141,
            this.xrTableCell142});
            this.xrTableRow71.Name = "xrTableRow71";
            this.xrTableRow71.Weight = 0.31249397929745759D;
            // 
            // xrTableCell141
            // 
            this.xrTableCell141.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.xrTableCell141.Multiline = true;
            this.xrTableCell141.Name = "xrTableCell141";
            this.xrTableCell141.StylePriority.UseFont = false;
            this.xrTableCell141.StylePriority.UseTextAlignment = false;
            this.xrTableCell141.Text = "يلتزم الموظف بعدم إفشاء معلومات و/ أو اسرار و/أو وثائق و/أو غيرها من المواد التي " +
    "تحصل عليها أو تكون بحوزته أو أطلع عليها خلال فترة عمله لدى الطرف الأول بعد انتها" +
    "ء عقد العمل وذلك في أي مكان";
            this.xrTableCell141.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrTableCell141.Weight = 0.88690134707848545D;
            // 
            // xrTableCell142
            // 
            this.xrTableCell142.Font = new DevExpress.Drawing.DXFont("Arial", 10F);
            this.xrTableCell142.Multiline = true;
            this.xrTableCell142.Name = "xrTableCell142";
            this.xrTableCell142.StylePriority.UseFont = false;
            this.xrTableCell142.StylePriority.UseTextAlignment = false;
            this.xrTableCell142.Text = resources.GetString("xrTableCell142.Text");
            this.xrTableCell142.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell142.Weight = 0.90539364777417042D;
            // 
            // xrTableRow70
            // 
            this.xrTableRow70.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell139,
            this.xrTableCell140});
            this.xrTableRow70.Name = "xrTableRow70";
            this.xrTableRow70.Weight = 0.19463750006277503D;
            // 
            // xrTableCell139
            // 
            this.xrTableCell139.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.xrTableCell139.Multiline = true;
            this.xrTableCell139.Name = "xrTableCell139";
            this.xrTableCell139.StylePriority.UseFont = false;
            this.xrTableCell139.StylePriority.UseTextAlignment = false;
            this.xrTableCell139.Text = "لا يجوز للموظف لمدة سنتين (2) بعد وقف العمل أن يحفز أو يشجع أي موظف عند الطرف الأ" +
    "ول لترك عمله";
            this.xrTableCell139.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrTableCell139.Weight = 0.88690134707848545D;
            // 
            // xrTableCell140
            // 
            this.xrTableCell140.Font = new DevExpress.Drawing.DXFont("Arial", 10F);
            this.xrTableCell140.Multiline = true;
            this.xrTableCell140.Name = "xrTableCell140";
            this.xrTableCell140.StylePriority.UseFont = false;
            this.xrTableCell140.StylePriority.UseTextAlignment = false;
            this.xrTableCell140.Text = "After suspension, the Employee shall not encourage or motivate any employee in Fi" +
    "rst Party to leave his/ her work (if it possible, to be combined with the confid" +
    "entiality clause).";
            this.xrTableCell140.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell140.Weight = 0.90539364777417042D;
            // 
            // xrTableRow69
            // 
            this.xrTableRow69.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell137,
            this.xrTableCell138});
            this.xrTableRow69.Name = "xrTableRow69";
            this.xrTableRow69.Weight = 0.38321024087832156D;
            // 
            // xrTableCell137
            // 
            this.xrTableCell137.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.xrTableCell137.Multiline = true;
            this.xrTableCell137.Name = "xrTableCell137";
            this.xrTableCell137.StylePriority.UseFont = false;
            this.xrTableCell137.StylePriority.UseTextAlignment = false;
            this.xrTableCell137.Text = resources.GetString("xrTableCell137.Text");
            this.xrTableCell137.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrTableCell137.Weight = 0.88690134707848545D;
            // 
            // xrTableCell138
            // 
            this.xrTableCell138.Font = new DevExpress.Drawing.DXFont("Arial", 10F);
            this.xrTableCell138.Multiline = true;
            this.xrTableCell138.Name = "xrTableCell138";
            this.xrTableCell138.StylePriority.UseFont = false;
            this.xrTableCell138.StylePriority.UseTextAlignment = false;
            this.xrTableCell138.Text = resources.GetString("xrTableCell138.Text");
            this.xrTableCell138.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell138.Weight = 0.90539364777417042D;
            // 
            // xrTableRow67
            // 
            this.xrTableRow67.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell133,
            this.xrTableCell134});
            this.xrTableRow67.Name = "xrTableRow67";
            this.xrTableRow67.Weight = 0.26142289916858374D;
            // 
            // xrTableCell133
            // 
            this.xrTableCell133.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.xrTableCell133.Multiline = true;
            this.xrTableCell133.Name = "xrTableCell133";
            this.xrTableCell133.StylePriority.UseFont = false;
            this.xrTableCell133.StylePriority.UseTextAlignment = false;
            this.xrTableCell133.Text = "يجوز للطرف الأول أن ينقل الطرف الثاني إلى أي إدارة أو فرع من إدارات أو فروع الطرف" +
    " الأول بنفس الدرجة الوظيفية داخل المملكة إذا اقتضت مصلحة العمل ذلك";
            this.xrTableCell133.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrTableCell133.Weight = 0.88690134707848545D;
            // 
            // xrTableCell134
            // 
            this.xrTableCell134.Font = new DevExpress.Drawing.DXFont("Arial", 10F);
            this.xrTableCell134.Multiline = true;
            this.xrTableCell134.Name = "xrTableCell134";
            this.xrTableCell134.StylePriority.UseFont = false;
            this.xrTableCell134.StylePriority.UseTextAlignment = false;
            this.xrTableCell134.Text = resources.GetString("xrTableCell134.Text");
            this.xrTableCell134.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell134.Weight = 0.90539364777417042D;
            // 
            // xrTable28
            // 
            this.xrTable28.BackColor = System.Drawing.Color.LightGray;
            this.xrTable28.BorderColor = System.Drawing.Color.Transparent;
            this.xrTable28.Font = new DevExpress.Drawing.DXFont("Arial", 15F);
            this.xrTable28.ForeColor = System.Drawing.Color.Black;
            this.xrTable28.LocationFloat = new DevExpress.Utils.PointFloat(11.03187F, 1835.22F);
            this.xrTable28.Name = "xrTable28";
            this.xrTable28.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable28.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow59});
            this.xrTable28.SizeF = new System.Drawing.SizeF(786.4684F, 24.99997F);
            this.xrTable28.StylePriority.UseBackColor = false;
            this.xrTable28.StylePriority.UseBorderColor = false;
            this.xrTable28.StylePriority.UseFont = false;
            this.xrTable28.StylePriority.UseForeColor = false;
            // 
            // xrTableRow59
            // 
            this.xrTableRow59.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell117,
            this.xrTableCell118});
            this.xrTableRow59.Name = "xrTableRow59";
            this.xrTableRow59.Weight = 1D;
            // 
            // xrTableCell117
            // 
            this.xrTableCell117.BorderColor = System.Drawing.Color.Silver;
            this.xrTableCell117.Multiline = true;
            this.xrTableCell117.Name = "xrTableCell117";
            this.xrTableCell117.StylePriority.UseBorderColor = false;
            this.xrTableCell117.Text = "تذاكر السفر لغير السعوديين";
            this.xrTableCell117.Weight = 1.4843630411355744D;
            // 
            // xrTableCell118
            // 
            this.xrTableCell118.BorderColor = System.Drawing.Color.Silver;
            this.xrTableCell118.Multiline = true;
            this.xrTableCell118.Name = "xrTableCell118";
            this.xrTableCell118.StylePriority.UseBorderColor = false;
            this.xrTableCell118.StylePriority.UseTextAlignment = false;
            this.xrTableCell118.Text = "TRAVEL TICKETS for NON-SAUDIS";
            this.xrTableCell118.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell118.Weight = 1.5162793179592744D;
            // 
            // xrTable29
            // 
            this.xrTable29.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.xrTable29.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable29.LocationFloat = new DevExpress.Utils.PointFloat(12.23199F, 1860.22F);
            this.xrTable29.Name = "xrTable29";
            this.xrTable29.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable29.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow60});
            this.xrTable29.SizeF = new System.Drawing.SizeF(784.1531F, 371.3691F);
            this.xrTable29.StylePriority.UseBorderColor = false;
            this.xrTable29.StylePriority.UseBorders = false;
            // 
            // xrTableRow60
            // 
            this.xrTableRow60.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell119,
            this.xrTableCell120});
            this.xrTableRow60.Name = "xrTableRow60";
            this.xrTableRow60.Weight = 0.80251687200070909D;
            // 
            // xrTableCell119
            // 
            this.xrTableCell119.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.xrTableCell119.Multiline = true;
            this.xrTableCell119.Name = "xrTableCell119";
            this.xrTableCell119.StylePriority.UseFont = false;
            this.xrTableCell119.StylePriority.UseTextAlignment = false;
            this.xrTableCell119.Text = resources.GetString("xrTableCell119.Text");
            this.xrTableCell119.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrTableCell119.Weight = 0.88690134707848545D;
            // 
            // xrTableCell120
            // 
            this.xrTableCell120.Font = new DevExpress.Drawing.DXFont("Arial", 10F);
            this.xrTableCell120.Multiline = true;
            this.xrTableCell120.Name = "xrTableCell120";
            this.xrTableCell120.StylePriority.UseFont = false;
            this.xrTableCell120.StylePriority.UseTextAlignment = false;
            this.xrTableCell120.Text = resources.GetString("xrTableCell120.Text");
            this.xrTableCell120.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell120.Weight = 0.90539364777417042D;
            // 
            // xrTable26
            // 
            this.xrTable26.BackColor = System.Drawing.Color.LightGray;
            this.xrTable26.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.xrTable26.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable26.Font = new DevExpress.Drawing.DXFont("Arial", 15F);
            this.xrTable26.ForeColor = System.Drawing.Color.Black;
            this.xrTable26.LocationFloat = new DevExpress.Utils.PointFloat(10.94853F, 1275.373F);
            this.xrTable26.Name = "xrTable26";
            this.xrTable26.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable26.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow54});
            this.xrTable26.SizeF = new System.Drawing.SizeF(786.4684F, 24.99997F);
            this.xrTable26.StylePriority.UseBackColor = false;
            this.xrTable26.StylePriority.UseBorderColor = false;
            this.xrTable26.StylePriority.UseBorders = false;
            this.xrTable26.StylePriority.UseFont = false;
            this.xrTable26.StylePriority.UseForeColor = false;
            // 
            // xrTableRow54
            // 
            this.xrTableRow54.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell107,
            this.xrTableCell108});
            this.xrTableRow54.Name = "xrTableRow54";
            this.xrTableRow54.Weight = 1D;
            // 
            // xrTableCell107
            // 
            this.xrTableCell107.BorderColor = System.Drawing.Color.Silver;
            this.xrTableCell107.Multiline = true;
            this.xrTableCell107.Name = "xrTableCell107";
            this.xrTableCell107.StylePriority.UseBorderColor = false;
            this.xrTableCell107.Text = "البند الرابع: التزامات الطرف الأول";
            this.xrTableCell107.Weight = 1.4843630411355744D;
            // 
            // xrTableCell108
            // 
            this.xrTableCell108.BorderColor = System.Drawing.Color.Silver;
            this.xrTableCell108.Multiline = true;
            this.xrTableCell108.Name = "xrTableCell108";
            this.xrTableCell108.StylePriority.UseBorderColor = false;
            this.xrTableCell108.StylePriority.UseTextAlignment = false;
            this.xrTableCell108.Text = "Article (4): Obligation of Adeed";
            this.xrTableCell108.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell108.Weight = 1.5162793179592744D;
            // 
            // xrTable27
            // 
            this.xrTable27.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.xrTable27.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable27.LocationFloat = new DevExpress.Utils.PointFloat(11.03187F, 1300.373F);
            this.xrTable27.Name = "xrTable27";
            this.xrTable27.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable27.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow55,
            this.xrTableRow58,
            this.xrTableRow57,
            this.xrTableRow56});
            this.xrTable27.SizeF = new System.Drawing.SizeF(784.8514F, 534.8472F);
            this.xrTable27.StylePriority.UseBorderColor = false;
            this.xrTable27.StylePriority.UseBorders = false;
            // 
            // xrTableRow55
            // 
            this.xrTableRow55.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell109,
            this.xrTableCell110});
            this.xrTableRow55.Name = "xrTableRow55";
            this.xrTableRow55.Weight = 0.614996902334275D;
            // 
            // xrTableCell109
            // 
            this.xrTableCell109.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.xrTableCell109.Multiline = true;
            this.xrTableCell109.Name = "xrTableCell109";
            this.xrTableCell109.StylePriority.UseFont = false;
            this.xrTableCell109.StylePriority.UseTextAlignment = false;
            this.xrTableCell109.Text = resources.GetString("xrTableCell109.Text");
            this.xrTableCell109.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrTableCell109.Weight = 0.88690134707848545D;
            // 
            // xrTableCell110
            // 
            this.xrTableCell110.Font = new DevExpress.Drawing.DXFont("Arial", 10F);
            this.xrTableCell110.Multiline = true;
            this.xrTableCell110.Name = "xrTableCell110";
            this.xrTableCell110.StylePriority.UseFont = false;
            this.xrTableCell110.StylePriority.UseTextAlignment = false;
            this.xrTableCell110.Text = resources.GetString("xrTableCell110.Text");
            this.xrTableCell110.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell110.Weight = 0.90539364777417042D;
            // 
            // xrTableRow58
            // 
            this.xrTableRow58.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell115,
            this.xrTableCell116});
            this.xrTableRow58.Name = "xrTableRow58";
            this.xrTableRow58.Weight = 0.38321024087832178D;
            // 
            // xrTableCell115
            // 
            this.xrTableCell115.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.xrTableCell115.Multiline = true;
            this.xrTableCell115.Name = "xrTableCell115";
            this.xrTableCell115.StylePriority.UseFont = false;
            this.xrTableCell115.StylePriority.UseTextAlignment = false;
            this.xrTableCell115.Text = "يلتزم الطرف الأول بتوفير الرعاية الطبية للموظف بالتأمين الصحي: وفقا لأحكام نظام ا" +
    "لضمان الصحي التعاوني وحسب سياسة الطرف الأول كما يلتزم بتسجيل الموظف لدى مؤسسة ال" +
    "تأمينات الاجتماعية ";
            this.xrTableCell115.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrTableCell115.Weight = 0.88690134707848545D;
            // 
            // xrTableCell116
            // 
            this.xrTableCell116.Font = new DevExpress.Drawing.DXFont("Arial", 10F);
            this.xrTableCell116.Multiline = true;
            this.xrTableCell116.Name = "xrTableCell116";
            this.xrTableCell116.StylePriority.UseFont = false;
            this.xrTableCell116.StylePriority.UseTextAlignment = false;
            this.xrTableCell116.Text = resources.GetString("xrTableCell116.Text");
            this.xrTableCell116.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell116.Weight = 0.90539364777417042D;
            // 
            // xrTableRow57
            // 
            this.xrTableRow57.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell113,
            this.xrTableCell114});
            this.xrTableRow57.Name = "xrTableRow57";
            this.xrTableRow57.Weight = 0.62285451836890648D;
            // 
            // xrTableCell113
            // 
            this.xrTableCell113.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.xrTableCell113.Multiline = true;
            this.xrTableCell113.Name = "xrTableCell113";
            this.xrTableCell113.StylePriority.UseFont = false;
            this.xrTableCell113.StylePriority.UseTextAlignment = false;
            this.xrTableCell113.Text = resources.GetString("xrTableCell113.Text");
            this.xrTableCell113.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrTableCell113.Weight = 0.88690134707848545D;
            // 
            // xrTableCell114
            // 
            this.xrTableCell114.Font = new DevExpress.Drawing.DXFont("Arial", 10F);
            this.xrTableCell114.Multiline = true;
            this.xrTableCell114.Name = "xrTableCell114";
            this.xrTableCell114.StylePriority.UseFont = false;
            this.xrTableCell114.StylePriority.UseTextAlignment = false;
            this.xrTableCell114.Text = resources.GetString("xrTableCell114.Text");
            this.xrTableCell114.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell114.Weight = 0.90539364777417042D;
            // 
            // xrTableRow56
            // 
            this.xrTableRow56.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell111,
            this.xrTableCell112});
            this.xrTableRow56.Name = "xrTableRow56";
            this.xrTableRow56.Weight = 0.37928074804175921D;
            // 
            // xrTableCell111
            // 
            this.xrTableCell111.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.xrTableCell111.Multiline = true;
            this.xrTableCell111.Name = "xrTableCell111";
            this.xrTableCell111.StylePriority.UseFont = false;
            this.xrTableCell111.StylePriority.UseTextAlignment = false;
            this.xrTableCell111.Text = "يلتزم الطرف الأول بتوفير الرعاية الطبية للموظف بالتأمين الصحي: وفقا لأحكام نظام ا" +
    "لضمان الصحي التعاوني وحسب سياسة الطرف الأول كما يلتزم بتسجيل الموظف لدى مؤسسة ال" +
    "تأمينات الاجتماعية ";
            this.xrTableCell111.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrTableCell111.Weight = 0.88690134707848545D;
            // 
            // xrTableCell112
            // 
            this.xrTableCell112.Font = new DevExpress.Drawing.DXFont("Arial", 10F);
            this.xrTableCell112.Multiline = true;
            this.xrTableCell112.Name = "xrTableCell112";
            this.xrTableCell112.StylePriority.UseFont = false;
            this.xrTableCell112.StylePriority.UseTextAlignment = false;
            this.xrTableCell112.Text = resources.GetString("xrTableCell112.Text");
            this.xrTableCell112.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell112.Weight = 0.90539364777417042D;
            // 
            // xrTable24
            // 
            this.xrTable24.BackColor = System.Drawing.Color.LightGray;
            this.xrTable24.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.xrTable24.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable24.Font = new DevExpress.Drawing.DXFont("Arial", 15F);
            this.xrTable24.ForeColor = System.Drawing.Color.Black;
            this.xrTable24.LocationFloat = new DevExpress.Utils.PointFloat(10.94853F, 1086.987F);
            this.xrTable24.Name = "xrTable24";
            this.xrTable24.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable24.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow52});
            this.xrTable24.SizeF = new System.Drawing.SizeF(786.4684F, 24.99997F);
            this.xrTable24.StylePriority.UseBackColor = false;
            this.xrTable24.StylePriority.UseBorderColor = false;
            this.xrTable24.StylePriority.UseBorders = false;
            this.xrTable24.StylePriority.UseFont = false;
            this.xrTable24.StylePriority.UseForeColor = false;
            // 
            // xrTableRow52
            // 
            this.xrTableRow52.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell103,
            this.xrTableCell104});
            this.xrTableRow52.Name = "xrTableRow52";
            this.xrTableRow52.Weight = 1D;
            // 
            // xrTableCell103
            // 
            this.xrTableCell103.BorderColor = System.Drawing.Color.Silver;
            this.xrTableCell103.Multiline = true;
            this.xrTableCell103.Name = "xrTableCell103";
            this.xrTableCell103.StylePriority.UseBorderColor = false;
            this.xrTableCell103.Text = "البند الثالث: ايام وساعات العمل";
            this.xrTableCell103.Weight = 1.4843630411355744D;
            // 
            // xrTableCell104
            // 
            this.xrTableCell104.BorderColor = System.Drawing.Color.Silver;
            this.xrTableCell104.Multiline = true;
            this.xrTableCell104.Name = "xrTableCell104";
            this.xrTableCell104.StylePriority.UseBorderColor = false;
            this.xrTableCell104.StylePriority.UseTextAlignment = false;
            this.xrTableCell104.Text = "Article (3): Days & Hours of Work";
            this.xrTableCell104.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell104.Weight = 1.5162793179592744D;
            // 
            // xrTable25
            // 
            this.xrTable25.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.xrTable25.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable25.LocationFloat = new DevExpress.Utils.PointFloat(11.03187F, 1111.987F);
            this.xrTable25.Name = "xrTable25";
            this.xrTable25.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable25.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow53});
            this.xrTable25.SizeF = new System.Drawing.SizeF(784.8514F, 163.3862F);
            this.xrTable25.StylePriority.UseBorderColor = false;
            this.xrTable25.StylePriority.UseBorders = false;
            // 
            // xrTableRow53
            // 
            this.xrTableRow53.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell105,
            this.xrTableCell106});
            this.xrTableRow53.Name = "xrTableRow53";
            this.xrTableRow53.Weight = 1D;
            // 
            // xrTableCell105
            // 
            this.xrTableCell105.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.xrTableCell105.Multiline = true;
            this.xrTableCell105.Name = "xrTableCell105";
            this.xrTableCell105.StylePriority.UseFont = false;
            this.xrTableCell105.StylePriority.UseTextAlignment = false;
            this.xrTableCell105.Text = resources.GetString("xrTableCell105.Text");
            this.xrTableCell105.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrTableCell105.Weight = 0.88690134707848545D;
            // 
            // xrTableCell106
            // 
            this.xrTableCell106.Font = new DevExpress.Drawing.DXFont("Arial", 10F);
            this.xrTableCell106.Multiline = true;
            this.xrTableCell106.Name = "xrTableCell106";
            this.xrTableCell106.StylePriority.UseFont = false;
            this.xrTableCell106.StylePriority.UseTextAlignment = false;
            this.xrTableCell106.Text = resources.GetString("xrTableCell106.Text");
            this.xrTableCell106.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell106.Weight = 0.90539364777417042D;
            // 
            // xrTable22
            // 
            this.xrTable22.BackColor = System.Drawing.Color.LightGray;
            this.xrTable22.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.xrTable22.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable22.Font = new DevExpress.Drawing.DXFont("Arial", 15F);
            this.xrTable22.ForeColor = System.Drawing.Color.Black;
            this.xrTable22.LocationFloat = new DevExpress.Utils.PointFloat(10.86518F, 794.609F);
            this.xrTable22.Name = "xrTable22";
            this.xrTable22.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable22.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow50});
            this.xrTable22.SizeF = new System.Drawing.SizeF(786.4684F, 24.99997F);
            this.xrTable22.StylePriority.UseBackColor = false;
            this.xrTable22.StylePriority.UseBorderColor = false;
            this.xrTable22.StylePriority.UseBorders = false;
            this.xrTable22.StylePriority.UseFont = false;
            this.xrTable22.StylePriority.UseForeColor = false;
            // 
            // xrTableRow50
            // 
            this.xrTableRow50.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell99,
            this.xrTableCell100});
            this.xrTableRow50.Name = "xrTableRow50";
            this.xrTableRow50.Weight = 1D;
            // 
            // xrTableCell99
            // 
            this.xrTableCell99.BorderColor = System.Drawing.Color.Silver;
            this.xrTableCell99.Multiline = true;
            this.xrTableCell99.Name = "xrTableCell99";
            this.xrTableCell99.StylePriority.UseBorderColor = false;
            this.xrTableCell99.Text = "البند الثاني: مدة العقد";
            this.xrTableCell99.Weight = 1.4843630411355744D;
            // 
            // xrTableCell100
            // 
            this.xrTableCell100.BorderColor = System.Drawing.Color.Silver;
            this.xrTableCell100.Multiline = true;
            this.xrTableCell100.Name = "xrTableCell100";
            this.xrTableCell100.StylePriority.UseBorderColor = false;
            this.xrTableCell100.StylePriority.UseTextAlignment = false;
            this.xrTableCell100.Text = "Article (2) TERM OF CONTRACT";
            this.xrTableCell100.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell100.Weight = 1.5162793179592744D;
            // 
            // xrTable23
            // 
            this.xrTable23.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.xrTable23.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable23.LocationFloat = new DevExpress.Utils.PointFloat(10.94853F, 819.609F);
            this.xrTable23.Name = "xrTable23";
            this.xrTable23.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable23.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow51});
            this.xrTable23.SizeF = new System.Drawing.SizeF(784.8514F, 267.3778F);
            this.xrTable23.StylePriority.UseBorderColor = false;
            this.xrTable23.StylePriority.UseBorders = false;
            // 
            // xrTableRow51
            // 
            this.xrTableRow51.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell101,
            this.xrTableCell102});
            this.xrTableRow51.Name = "xrTableRow51";
            this.xrTableRow51.Weight = 1D;
            // 
            // xrTableCell101
            // 
            this.xrTableCell101.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.xrTableCell101.Multiline = true;
            this.xrTableCell101.Name = "xrTableCell101";
            this.xrTableCell101.StylePriority.UseFont = false;
            this.xrTableCell101.StylePriority.UseTextAlignment = false;
            this.xrTableCell101.Text = resources.GetString("xrTableCell101.Text");
            this.xrTableCell101.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrTableCell101.Weight = 0.88690134707848545D;
            // 
            // xrTableCell102
            // 
            this.xrTableCell102.Font = new DevExpress.Drawing.DXFont("Arial", 10F);
            this.xrTableCell102.Multiline = true;
            this.xrTableCell102.Name = "xrTableCell102";
            this.xrTableCell102.StylePriority.UseFont = false;
            this.xrTableCell102.StylePriority.UseTextAlignment = false;
            this.xrTableCell102.Text = resources.GetString("xrTableCell102.Text");
            this.xrTableCell102.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell102.Weight = 0.90539364777417042D;
            // 
            // xrTable20
            // 
            this.xrTable20.BackColor = System.Drawing.Color.LightGray;
            this.xrTable20.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.xrTable20.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable20.Font = new DevExpress.Drawing.DXFont("Arial", 15F);
            this.xrTable20.ForeColor = System.Drawing.Color.Black;
            this.xrTable20.LocationFloat = new DevExpress.Utils.PointFloat(10.78183F, 716.5791F);
            this.xrTable20.Name = "xrTable20";
            this.xrTable20.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable20.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow44});
            this.xrTable20.SizeF = new System.Drawing.SizeF(786.4684F, 24.99997F);
            this.xrTable20.StylePriority.UseBackColor = false;
            this.xrTable20.StylePriority.UseBorderColor = false;
            this.xrTable20.StylePriority.UseBorders = false;
            this.xrTable20.StylePriority.UseFont = false;
            this.xrTable20.StylePriority.UseForeColor = false;
            // 
            // xrTableRow44
            // 
            this.xrTableRow44.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell87,
            this.xrTableCell88});
            this.xrTableRow44.Name = "xrTableRow44";
            this.xrTableRow44.Weight = 1D;
            // 
            // xrTableCell87
            // 
            this.xrTableCell87.BorderColor = System.Drawing.Color.Silver;
            this.xrTableCell87.Multiline = true;
            this.xrTableCell87.Name = "xrTableCell87";
            this.xrTableCell87.StylePriority.UseBorderColor = false;
            this.xrTableCell87.Text = "البند الأول: التمهيد";
            this.xrTableCell87.Weight = 1.4843630411355744D;
            // 
            // xrTableCell88
            // 
            this.xrTableCell88.BorderColor = System.Drawing.Color.Silver;
            this.xrTableCell88.Multiline = true;
            this.xrTableCell88.Name = "xrTableCell88";
            this.xrTableCell88.StylePriority.UseBorderColor = false;
            this.xrTableCell88.StylePriority.UseTextAlignment = false;
            this.xrTableCell88.Text = "Article (1) the Preamble";
            this.xrTableCell88.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell88.Weight = 1.5162793179592744D;
            // 
            // xrTable21
            // 
            this.xrTable21.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.xrTable21.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable21.LocationFloat = new DevExpress.Utils.PointFloat(10.86518F, 741.579F);
            this.xrTable21.Name = "xrTable21";
            this.xrTable21.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable21.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow49});
            this.xrTable21.SizeF = new System.Drawing.SizeF(784.8514F, 53.03003F);
            this.xrTable21.StylePriority.UseBorderColor = false;
            this.xrTable21.StylePriority.UseBorders = false;
            // 
            // xrTableRow49
            // 
            this.xrTableRow49.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell97,
            this.xrTableCell98});
            this.xrTableRow49.Name = "xrTableRow49";
            this.xrTableRow49.Weight = 1D;
            // 
            // xrTableCell97
            // 
            this.xrTableCell97.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.xrTableCell97.Multiline = true;
            this.xrTableCell97.Name = "xrTableCell97";
            this.xrTableCell97.StylePriority.UseFont = false;
            this.xrTableCell97.StylePriority.UseTextAlignment = false;
            this.xrTableCell97.Text = "يعتبر موضوع العقد وبيانات الوظيفة والبدلات والمزايا المذكورة بعالية جزءاً لا يتجز" +
    "أ من هذا العقد.";
            this.xrTableCell97.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrTableCell97.Weight = 0.88690134707848545D;
            // 
            // xrTableCell98
            // 
            this.xrTableCell98.Font = new DevExpress.Drawing.DXFont("Arial", 10F);
            this.xrTableCell98.Multiline = true;
            this.xrTableCell98.Name = "xrTableCell98";
            this.xrTableCell98.StylePriority.UseFont = false;
            this.xrTableCell98.StylePriority.UseTextAlignment = false;
            this.xrTableCell98.Text = "The Contract Subject, Position Details and the Employment Allowances and Benefits" +
    " are deemed an integral part of this .Contract";
            this.xrTableCell98.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell98.Weight = 0.90539364777417042D;
            // 
            // xrTable17
            // 
            this.xrTable17.BackColor = System.Drawing.Color.LightGray;
            this.xrTable17.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.xrTable17.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable17.Font = new DevExpress.Drawing.DXFont("Arial", 15F);
            this.xrTable17.ForeColor = System.Drawing.Color.Black;
            this.xrTable17.LocationFloat = new DevExpress.Utils.PointFloat(10.78177F, 600.2921F);
            this.xrTable17.Name = "xrTable17";
            this.xrTable17.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable17.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow39});
            this.xrTable17.SizeF = new System.Drawing.SizeF(786.4684F, 24.99997F);
            this.xrTable17.StylePriority.UseBackColor = false;
            this.xrTable17.StylePriority.UseBorderColor = false;
            this.xrTable17.StylePriority.UseBorders = false;
            this.xrTable17.StylePriority.UseFont = false;
            this.xrTable17.StylePriority.UseForeColor = false;
            // 
            // xrTableRow39
            // 
            this.xrTableRow39.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell77,
            this.xrTableCell78});
            this.xrTableRow39.Name = "xrTableRow39";
            this.xrTableRow39.Weight = 1D;
            // 
            // xrTableCell77
            // 
            this.xrTableCell77.BorderColor = System.Drawing.Color.Silver;
            this.xrTableCell77.Multiline = true;
            this.xrTableCell77.Name = "xrTableCell77";
            this.xrTableCell77.StylePriority.UseBorderColor = false;
            this.xrTableCell77.Text = "البدلات والمزايا";
            this.xrTableCell77.Weight = 1.4843630411355744D;
            // 
            // xrTableCell78
            // 
            this.xrTableCell78.BorderColor = System.Drawing.Color.Silver;
            this.xrTableCell78.Multiline = true;
            this.xrTableCell78.Name = "xrTableCell78";
            this.xrTableCell78.StylePriority.UseBorderColor = false;
            this.xrTableCell78.StylePriority.UseTextAlignment = false;
            this.xrTableCell78.Text = "The Employment Allowances & Benefits";
            this.xrTableCell78.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell78.Weight = 1.5162793179592744D;
            // 
            // xrTable18
            // 
            this.xrTable18.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.xrTable18.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable18.LocationFloat = new DevExpress.Utils.PointFloat(399.9167F, 625.2921F);
            this.xrTable18.Name = "xrTable18";
            this.xrTable18.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable18.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow40,
            this.xrTableRow41,
            this.xrTableRow42,
            this.xrTableRow43});
            this.xrTable18.SizeF = new System.Drawing.SizeF(395.9665F, 91.28687F);
            this.xrTable18.StylePriority.UseBorderColor = false;
            this.xrTable18.StylePriority.UseBorders = false;
            // 
            // xrTableRow40
            // 
            this.xrTableRow40.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell79,
            this.xrTableCell80});
            this.xrTableRow40.Name = "xrTableRow40";
            this.xrTableRow40.Weight = 1D;
            // 
            // xrTableCell79
            // 
            this.xrTableCell79.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[BasicSalaryItem]")});
            this.xrTableCell79.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell79.Multiline = true;
            this.xrTableCell79.Name = "xrTableCell79";
            this.xrTableCell79.StylePriority.UseFont = false;
            this.xrTableCell79.StylePriority.UseTextAlignment = false;
            this.xrTableCell79.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell79.TextFormatString = "{0:#.00}";
            this.xrTableCell79.Weight = 0.88690134707848545D;
            // 
            // xrTableCell80
            // 
            this.xrTableCell80.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell80.Multiline = true;
            this.xrTableCell80.Name = "xrTableCell80";
            this.xrTableCell80.StylePriority.UseFont = false;
            this.xrTableCell80.StylePriority.UseTextAlignment = false;
            this.xrTableCell80.Text = " : Basic Salary ";
            this.xrTableCell80.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell80.Weight = 0.90539364777417042D;
            // 
            // xrTableRow41
            // 
            this.xrTableRow41.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell81,
            this.xrTableCell82});
            this.xrTableRow41.Name = "xrTableRow41";
            this.xrTableRow41.Weight = 1D;
            // 
            // xrTableCell81
            // 
            this.xrTableCell81.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[TransportationSalaryItem]")});
            this.xrTableCell81.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell81.Multiline = true;
            this.xrTableCell81.Name = "xrTableCell81";
            this.xrTableCell81.StylePriority.UseFont = false;
            this.xrTableCell81.StylePriority.UseTextAlignment = false;
            this.xrTableCell81.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell81.TextFormatString = "{0:#.00}";
            this.xrTableCell81.Weight = 0.88690134707848545D;
            // 
            // xrTableCell82
            // 
            this.xrTableCell82.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell82.Multiline = true;
            this.xrTableCell82.Name = "xrTableCell82";
            this.xrTableCell82.StylePriority.UseFont = false;
            this.xrTableCell82.StylePriority.UseTextAlignment = false;
            this.xrTableCell82.Text = " : Transportation ";
            this.xrTableCell82.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell82.Weight = 0.90539364777417042D;
            // 
            // xrTableRow42
            // 
            this.xrTableRow42.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell83,
            this.xrTableCell84});
            this.xrTableRow42.Name = "xrTableRow42";
            this.xrTableRow42.Weight = 1D;
            // 
            // xrTableCell83
            // 
            this.xrTableCell83.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[HomeSalaryItem]")});
            this.xrTableCell83.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell83.Multiline = true;
            this.xrTableCell83.Name = "xrTableCell83";
            this.xrTableCell83.StylePriority.UseFont = false;
            this.xrTableCell83.StylePriority.UseTextAlignment = false;
            this.xrTableCell83.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell83.TextFormatString = "{0:#.00}";
            this.xrTableCell83.Weight = 0.88690134707848545D;
            // 
            // xrTableCell84
            // 
            this.xrTableCell84.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell84.Multiline = true;
            this.xrTableCell84.Name = "xrTableCell84";
            this.xrTableCell84.StylePriority.UseFont = false;
            this.xrTableCell84.StylePriority.UseTextAlignment = false;
            this.xrTableCell84.Text = "  : Housing Allowance ";
            this.xrTableCell84.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell84.Weight = 0.90539364777417042D;
            // 
            // xrTableRow43
            // 
            this.xrTableRow43.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell85,
            this.xrTableCell86});
            this.xrTableRow43.Name = "xrTableRow43";
            this.xrTableRow43.Weight = 1D;
            // 
            // xrTableCell85
            // 
            this.xrTableCell85.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[AnnualVacationEn]")});
            this.xrTableCell85.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell85.Multiline = true;
            this.xrTableCell85.Name = "xrTableCell85";
            this.xrTableCell85.StylePriority.UseFont = false;
            this.xrTableCell85.StylePriority.UseTextAlignment = false;
            this.xrTableCell85.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell85.Weight = 0.88690134707848545D;
            // 
            // xrTableCell86
            // 
            this.xrTableCell86.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell86.Multiline = true;
            this.xrTableCell86.Name = "xrTableCell86";
            this.xrTableCell86.StylePriority.UseFont = false;
            this.xrTableCell86.StylePriority.UseTextAlignment = false;
            this.xrTableCell86.Text = " : Annual Vacation";
            this.xrTableCell86.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell86.Weight = 0.90539364777417042D;
            // 
            // xrTable19
            // 
            this.xrTable19.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.xrTable19.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable19.LocationFloat = new DevExpress.Utils.PointFloat(10.78183F, 625.2921F);
            this.xrTable19.Name = "xrTable19";
            this.xrTable19.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable19.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow45,
            this.xrTableRow46,
            this.xrTableRow47,
            this.xrTableRow48});
            this.xrTable19.SizeF = new System.Drawing.SizeF(389.1348F, 91.28691F);
            this.xrTable19.StylePriority.UseBorderColor = false;
            this.xrTable19.StylePriority.UseBorders = false;
            // 
            // xrTableRow45
            // 
            this.xrTableRow45.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell89,
            this.xrTableCell90});
            this.xrTableRow45.Name = "xrTableRow45";
            this.xrTableRow45.Weight = 1D;
            // 
            // xrTableCell89
            // 
            this.xrTableCell89.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell89.Multiline = true;
            this.xrTableCell89.Name = "xrTableCell89";
            this.xrTableCell89.StylePriority.UseFont = false;
            this.xrTableCell89.Text = "الراتب الأساسي:";
            this.xrTableCell89.Weight = 0.88690134707848545D;
            // 
            // xrTableCell90
            // 
            this.xrTableCell90.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[BasicSalaryItem]")});
            this.xrTableCell90.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell90.Multiline = true;
            this.xrTableCell90.Name = "xrTableCell90";
            this.xrTableCell90.StylePriority.UseFont = false;
            this.xrTableCell90.StylePriority.UseTextAlignment = false;
            this.xrTableCell90.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrTableCell90.TextFormatString = "{0:#.00}";
            this.xrTableCell90.Weight = 0.90539364777417042D;
            // 
            // xrTableRow46
            // 
            this.xrTableRow46.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell91,
            this.xrTableCell92});
            this.xrTableRow46.Name = "xrTableRow46";
            this.xrTableRow46.Weight = 1D;
            // 
            // xrTableCell91
            // 
            this.xrTableCell91.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell91.Multiline = true;
            this.xrTableCell91.Name = "xrTableCell91";
            this.xrTableCell91.StylePriority.UseFont = false;
            this.xrTableCell91.Text = "بدل النقل :";
            this.xrTableCell91.Weight = 0.88690134707848545D;
            // 
            // xrTableCell92
            // 
            this.xrTableCell92.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[TransportationSalaryItem]")});
            this.xrTableCell92.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell92.Multiline = true;
            this.xrTableCell92.Name = "xrTableCell92";
            this.xrTableCell92.StylePriority.UseFont = false;
            this.xrTableCell92.StylePriority.UseTextAlignment = false;
            this.xrTableCell92.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrTableCell92.TextFormatString = "{0:#.00}";
            this.xrTableCell92.Weight = 0.90539364777417042D;
            // 
            // xrTableRow47
            // 
            this.xrTableRow47.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell93,
            this.xrTableCell94});
            this.xrTableRow47.Name = "xrTableRow47";
            this.xrTableRow47.Weight = 1D;
            // 
            // xrTableCell93
            // 
            this.xrTableCell93.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell93.Multiline = true;
            this.xrTableCell93.Name = "xrTableCell93";
            this.xrTableCell93.StylePriority.UseFont = false;
            this.xrTableCell93.Text = "بدل السكن :";
            this.xrTableCell93.Weight = 0.88690134707848545D;
            // 
            // xrTableCell94
            // 
            this.xrTableCell94.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[HomeSalaryItem]")});
            this.xrTableCell94.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell94.Multiline = true;
            this.xrTableCell94.Name = "xrTableCell94";
            this.xrTableCell94.StylePriority.UseFont = false;
            this.xrTableCell94.StylePriority.UseTextAlignment = false;
            this.xrTableCell94.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrTableCell94.TextFormatString = "{0:#.00}";
            this.xrTableCell94.Weight = 0.90539364777417042D;
            // 
            // xrTableRow48
            // 
            this.xrTableRow48.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell95,
            this.xrTableCell96});
            this.xrTableRow48.Name = "xrTableRow48";
            this.xrTableRow48.Weight = 1D;
            // 
            // xrTableCell95
            // 
            this.xrTableCell95.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell95.Multiline = true;
            this.xrTableCell95.Name = "xrTableCell95";
            this.xrTableCell95.StylePriority.UseFont = false;
            this.xrTableCell95.Text = "الأجازة السنوية :";
            this.xrTableCell95.Weight = 0.88690134707848545D;
            // 
            // xrTableCell96
            // 
            this.xrTableCell96.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[AnnualVacation]")});
            this.xrTableCell96.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell96.Multiline = true;
            this.xrTableCell96.Name = "xrTableCell96";
            this.xrTableCell96.StylePriority.UseFont = false;
            this.xrTableCell96.StylePriority.UseTextAlignment = false;
            this.xrTableCell96.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrTableCell96.Weight = 0.90539364777417042D;
            // 
            // xrTable15
            // 
            this.xrTable15.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.xrTable15.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable15.LocationFloat = new DevExpress.Utils.PointFloat(399.9166F, 486.1835F);
            this.xrTable15.Name = "xrTable15";
            this.xrTable15.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable15.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow26,
            this.xrTableRow27,
            this.xrTableRow31,
            this.xrTableRow32,
            this.xrTableRow33});
            this.xrTable15.SizeF = new System.Drawing.SizeF(395.9665F, 114.1086F);
            this.xrTable15.StylePriority.UseBorderColor = false;
            this.xrTable15.StylePriority.UseBorders = false;
            this.xrTable15.StylePriority.UseTextAlignment = false;
            this.xrTable15.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrTableRow26
            // 
            this.xrTableRow26.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell51,
            this.xrTableCell52});
            this.xrTableRow26.Name = "xrTableRow26";
            this.xrTableRow26.Weight = 1D;
            // 
            // xrTableCell51
            // 
            this.xrTableCell51.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[JobEnName]")});
            this.xrTableCell51.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell51.Multiline = true;
            this.xrTableCell51.Name = "xrTableCell51";
            this.xrTableCell51.StylePriority.UseFont = false;
            this.xrTableCell51.Weight = 0.88690134707848545D;
            // 
            // xrTableCell52
            // 
            this.xrTableCell52.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell52.Multiline = true;
            this.xrTableCell52.Name = "xrTableCell52";
            this.xrTableCell52.StylePriority.UseFont = false;
            this.xrTableCell52.StylePriority.UseTextAlignment = false;
            this.xrTableCell52.Text = " : Job Title ";
            this.xrTableCell52.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell52.Weight = 0.90539364777417042D;
            // 
            // xrTableRow27
            // 
            this.xrTableRow27.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell53,
            this.xrTableCell54});
            this.xrTableRow27.Name = "xrTableRow27";
            this.xrTableRow27.Weight = 1D;
            // 
            // xrTableCell53
            // 
            this.xrTableCell53.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[ContractPeriodEn]")});
            this.xrTableCell53.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell53.Multiline = true;
            this.xrTableCell53.Name = "xrTableCell53";
            this.xrTableCell53.StylePriority.UseFont = false;
            this.xrTableCell53.Weight = 0.88690134707848545D;
            // 
            // xrTableCell54
            // 
            this.xrTableCell54.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell54.Multiline = true;
            this.xrTableCell54.Name = "xrTableCell54";
            this.xrTableCell54.StylePriority.UseFont = false;
            this.xrTableCell54.StylePriority.UseTextAlignment = false;
            this.xrTableCell54.Text = " : Contract Duration";
            this.xrTableCell54.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell54.Weight = 0.90539364777417042D;
            // 
            // xrTableRow31
            // 
            this.xrTableRow31.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell61,
            this.xrTableCell62});
            this.xrTableRow31.Name = "xrTableRow31";
            this.xrTableRow31.Weight = 1D;
            // 
            // xrTableCell61
            // 
            this.xrTableCell61.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[ContractStartTime]")});
            this.xrTableCell61.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell61.Multiline = true;
            this.xrTableCell61.Name = "xrTableCell61";
            this.xrTableCell61.StylePriority.UseFont = false;
            this.xrTableCell61.TextFormatString = "{0:MM/dd/yyyy}";
            this.xrTableCell61.Weight = 0.88690134707848545D;
            // 
            // xrTableCell62
            // 
            this.xrTableCell62.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell62.Multiline = true;
            this.xrTableCell62.Name = "xrTableCell62";
            this.xrTableCell62.StylePriority.UseFont = false;
            this.xrTableCell62.StylePriority.UseTextAlignment = false;
            this.xrTableCell62.Text = "  : Contract Start Date ";
            this.xrTableCell62.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell62.Weight = 0.90539364777417042D;
            // 
            // xrTableRow32
            // 
            this.xrTableRow32.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell63,
            this.xrTableCell64});
            this.xrTableRow32.Name = "xrTableRow32";
            this.xrTableRow32.Weight = 1D;
            // 
            // xrTableCell63
            // 
            this.xrTableCell63.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[ContractEndTime]")});
            this.xrTableCell63.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell63.Multiline = true;
            this.xrTableCell63.Name = "xrTableCell63";
            this.xrTableCell63.StylePriority.UseFont = false;
            this.xrTableCell63.TextFormatString = "{0:MM/dd/yyyy}";
            this.xrTableCell63.Weight = 0.88690134707848545D;
            // 
            // xrTableCell64
            // 
            this.xrTableCell64.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell64.Multiline = true;
            this.xrTableCell64.Name = "xrTableCell64";
            this.xrTableCell64.StylePriority.UseFont = false;
            this.xrTableCell64.StylePriority.UseTextAlignment = false;
            this.xrTableCell64.Text = " : Contract End Date";
            this.xrTableCell64.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell64.Weight = 0.90539364777417042D;
            // 
            // xrTableRow33
            // 
            this.xrTableRow33.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell65,
            this.xrTableCell66});
            this.xrTableRow33.Name = "xrTableRow33";
            this.xrTableRow33.Weight = 1D;
            // 
            // xrTableCell65
            // 
            this.xrTableCell65.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[NoticePeriod]")});
            this.xrTableCell65.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell65.Multiline = true;
            this.xrTableCell65.Name = "xrTableCell65";
            this.xrTableCell65.StylePriority.UseFont = false;
            this.xrTableCell65.Weight = 0.88690134707848545D;
            // 
            // xrTableCell66
            // 
            this.xrTableCell66.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell66.Multiline = true;
            this.xrTableCell66.Name = "xrTableCell66";
            this.xrTableCell66.StylePriority.UseFont = false;
            this.xrTableCell66.StylePriority.UseTextAlignment = false;
            this.xrTableCell66.Text = " : Notice Period";
            this.xrTableCell66.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell66.Weight = 0.90539364777417042D;
            // 
            // xrTable12
            // 
            this.xrTable12.BackColor = System.Drawing.Color.LightGray;
            this.xrTable12.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.xrTable12.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable12.Font = new DevExpress.Drawing.DXFont("Arial", 15F);
            this.xrTable12.ForeColor = System.Drawing.Color.Black;
            this.xrTable12.LocationFloat = new DevExpress.Utils.PointFloat(10.78171F, 461.1835F);
            this.xrTable12.Name = "xrTable12";
            this.xrTable12.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable12.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow20});
            this.xrTable12.SizeF = new System.Drawing.SizeF(786.4684F, 24.99997F);
            this.xrTable12.StylePriority.UseBackColor = false;
            this.xrTable12.StylePriority.UseBorderColor = false;
            this.xrTable12.StylePriority.UseBorders = false;
            this.xrTable12.StylePriority.UseFont = false;
            this.xrTable12.StylePriority.UseForeColor = false;
            // 
            // xrTableRow20
            // 
            this.xrTableRow20.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell39,
            this.xrTableCell40});
            this.xrTableRow20.Name = "xrTableRow20";
            this.xrTableRow20.Weight = 1D;
            // 
            // xrTableCell39
            // 
            this.xrTableCell39.BorderColor = System.Drawing.Color.Silver;
            this.xrTableCell39.Multiline = true;
            this.xrTableCell39.Name = "xrTableCell39";
            this.xrTableCell39.StylePriority.UseBorderColor = false;
            this.xrTableCell39.Text = "بيانات الوظيفة";
            this.xrTableCell39.Weight = 1.4843630411355744D;
            // 
            // xrTableCell40
            // 
            this.xrTableCell40.BorderColor = System.Drawing.Color.Silver;
            this.xrTableCell40.Multiline = true;
            this.xrTableCell40.Name = "xrTableCell40";
            this.xrTableCell40.StylePriority.UseBorderColor = false;
            this.xrTableCell40.StylePriority.UseTextAlignment = false;
            this.xrTableCell40.Text = "Position ";
            this.xrTableCell40.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell40.Weight = 1.5162793179592744D;
            // 
            // xrTable16
            // 
            this.xrTable16.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.xrTable16.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable16.LocationFloat = new DevExpress.Utils.PointFloat(10.78177F, 486.1835F);
            this.xrTable16.Name = "xrTable16";
            this.xrTable16.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable16.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow34,
            this.xrTableRow35,
            this.xrTableRow36,
            this.xrTableRow37,
            this.xrTableRow38});
            this.xrTable16.SizeF = new System.Drawing.SizeF(389.1348F, 114.1086F);
            this.xrTable16.StylePriority.UseBorderColor = false;
            this.xrTable16.StylePriority.UseBorders = false;
            // 
            // xrTableRow34
            // 
            this.xrTableRow34.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell67,
            this.xrTableCell68});
            this.xrTableRow34.Name = "xrTableRow34";
            this.xrTableRow34.Weight = 1D;
            // 
            // xrTableCell67
            // 
            this.xrTableCell67.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell67.Multiline = true;
            this.xrTableCell67.Name = "xrTableCell67";
            this.xrTableCell67.StylePriority.UseFont = false;
            this.xrTableCell67.Text = "المسمى الوظيفي :";
            this.xrTableCell67.Weight = 0.88690134707848545D;
            // 
            // xrTableCell68
            // 
            this.xrTableCell68.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[JobName]")});
            this.xrTableCell68.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell68.Multiline = true;
            this.xrTableCell68.Name = "xrTableCell68";
            this.xrTableCell68.StylePriority.UseFont = false;
            this.xrTableCell68.StylePriority.UseTextAlignment = false;
            this.xrTableCell68.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrTableCell68.Weight = 0.90539364777417042D;
            // 
            // xrTableRow35
            // 
            this.xrTableRow35.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell69,
            this.xrTableCell70});
            this.xrTableRow35.Name = "xrTableRow35";
            this.xrTableRow35.Weight = 1D;
            // 
            // xrTableCell69
            // 
            this.xrTableCell69.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell69.Multiline = true;
            this.xrTableCell69.Name = "xrTableCell69";
            this.xrTableCell69.StylePriority.UseFont = false;
            this.xrTableCell69.Text = "مدة العقد :";
            this.xrTableCell69.Weight = 0.88690134707848545D;
            // 
            // xrTableCell70
            // 
            this.xrTableCell70.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[ContractPeriod]")});
            this.xrTableCell70.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell70.Multiline = true;
            this.xrTableCell70.Name = "xrTableCell70";
            this.xrTableCell70.StylePriority.UseFont = false;
            this.xrTableCell70.StylePriority.UseTextAlignment = false;
            this.xrTableCell70.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrTableCell70.Weight = 0.90539364777417042D;
            // 
            // xrTableRow36
            // 
            this.xrTableRow36.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell71,
            this.xrTableCell72});
            this.xrTableRow36.Name = "xrTableRow36";
            this.xrTableRow36.Weight = 1D;
            // 
            // xrTableCell71
            // 
            this.xrTableCell71.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell71.Multiline = true;
            this.xrTableCell71.Name = "xrTableCell71";
            this.xrTableCell71.StylePriority.UseFont = false;
            this.xrTableCell71.Text = "تاريخ بداية العقد :";
            this.xrTableCell71.Weight = 0.88690134707848545D;
            // 
            // xrTableCell72
            // 
            this.xrTableCell72.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[ContractStartTime]")});
            this.xrTableCell72.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell72.Multiline = true;
            this.xrTableCell72.Name = "xrTableCell72";
            this.xrTableCell72.StylePriority.UseFont = false;
            this.xrTableCell72.StylePriority.UseTextAlignment = false;
            this.xrTableCell72.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrTableCell72.TextFormatString = "{0:MM/dd/yyyy}";
            this.xrTableCell72.Weight = 0.90539364777417042D;
            // 
            // xrTableRow37
            // 
            this.xrTableRow37.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell73,
            this.xrTableCell74});
            this.xrTableRow37.Name = "xrTableRow37";
            this.xrTableRow37.Weight = 1D;
            // 
            // xrTableCell73
            // 
            this.xrTableCell73.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell73.Multiline = true;
            this.xrTableCell73.Name = "xrTableCell73";
            this.xrTableCell73.StylePriority.UseFont = false;
            this.xrTableCell73.Text = "تاريخ نهاية العقد :";
            this.xrTableCell73.Weight = 0.88690134707848545D;
            // 
            // xrTableCell74
            // 
            this.xrTableCell74.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[ContractEndTime]")});
            this.xrTableCell74.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell74.Multiline = true;
            this.xrTableCell74.Name = "xrTableCell74";
            this.xrTableCell74.StylePriority.UseFont = false;
            this.xrTableCell74.StylePriority.UseTextAlignment = false;
            this.xrTableCell74.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrTableCell74.TextFormatString = "{0:MM/dd/yyyy}";
            this.xrTableCell74.Weight = 0.90539364777417042D;
            // 
            // xrTableRow38
            // 
            this.xrTableRow38.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell75,
            this.xrTableCell76});
            this.xrTableRow38.Name = "xrTableRow38";
            this.xrTableRow38.Weight = 1D;
            // 
            // xrTableCell75
            // 
            this.xrTableCell75.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell75.Multiline = true;
            this.xrTableCell75.Name = "xrTableCell75";
            this.xrTableCell75.StylePriority.UseFont = false;
            this.xrTableCell75.Text = "فترة الإنذار :";
            this.xrTableCell75.Weight = 0.88690134707848545D;
            // 
            // xrTableCell76
            // 
            this.xrTableCell76.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[NoticePeriod]")});
            this.xrTableCell76.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell76.Multiline = true;
            this.xrTableCell76.Name = "xrTableCell76";
            this.xrTableCell76.StylePriority.UseFont = false;
            this.xrTableCell76.StylePriority.UseTextAlignment = false;
            this.xrTableCell76.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrTableCell76.Weight = 0.90539364777417042D;
            // 
            // xrTable2
            // 
            this.xrTable2.BackColor = System.Drawing.Color.LightGray;
            this.xrTable2.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.xrTable2.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable2.Font = new DevExpress.Drawing.DXFont("Arial", 15F);
            this.xrTable2.ForeColor = System.Drawing.Color.Black;
            this.xrTable2.LocationFloat = new DevExpress.Utils.PointFloat(10.69836F, 289.4035F);
            this.xrTable2.Name = "xrTable2";
            this.xrTable2.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable2.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow15});
            this.xrTable2.SizeF = new System.Drawing.SizeF(786.4684F, 24.99997F);
            this.xrTable2.StylePriority.UseBackColor = false;
            this.xrTable2.StylePriority.UseBorderColor = false;
            this.xrTable2.StylePriority.UseBorders = false;
            this.xrTable2.StylePriority.UseFont = false;
            this.xrTable2.StylePriority.UseForeColor = false;
            // 
            // xrTableRow15
            // 
            this.xrTableRow15.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell29,
            this.xrTableCell30});
            this.xrTableRow15.Name = "xrTableRow15";
            this.xrTableRow15.Weight = 1D;
            // 
            // xrTableCell29
            // 
            this.xrTableCell29.BorderColor = System.Drawing.Color.Silver;
            this.xrTableCell29.Multiline = true;
            this.xrTableCell29.Name = "xrTableCell29";
            this.xrTableCell29.StylePriority.UseBorderColor = false;
            this.xrTableCell29.Text = "موضوع العقد";
            this.xrTableCell29.Weight = 1.4843630411355744D;
            // 
            // xrTableCell30
            // 
            this.xrTableCell30.BorderColor = System.Drawing.Color.Silver;
            this.xrTableCell30.Multiline = true;
            this.xrTableCell30.Name = "xrTableCell30";
            this.xrTableCell30.StylePriority.UseBorderColor = false;
            this.xrTableCell30.StylePriority.UseTextAlignment = false;
            this.xrTableCell30.Text = "Contract Subject";
            this.xrTableCell30.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell30.Weight = 1.5162793179592744D;
            // 
            // xrTable14
            // 
            this.xrTable14.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.xrTable14.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable14.LocationFloat = new DevExpress.Utils.PointFloat(10.78171F, 314.4034F);
            this.xrTable14.Name = "xrTable14";
            this.xrTable14.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable14.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow25});
            this.xrTable14.SizeF = new System.Drawing.SizeF(784.8514F, 146.7801F);
            this.xrTable14.StylePriority.UseBorderColor = false;
            this.xrTable14.StylePriority.UseBorders = false;
            // 
            // xrTableRow25
            // 
            this.xrTableRow25.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell49,
            this.xrTableCell50});
            this.xrTableRow25.Name = "xrTableRow25";
            this.xrTableRow25.Weight = 1D;
            // 
            // xrTableCell49
            // 
            this.xrTableCell49.Font = new DevExpress.Drawing.DXFont("Arial", 12F);
            this.xrTableCell49.Multiline = true;
            this.xrTableCell49.Name = "xrTableCell49";
            this.xrTableCell49.StylePriority.UseFont = false;
            this.xrTableCell49.StylePriority.UseTextAlignment = false;
            this.xrTableCell49.Text = resources.GetString("xrTableCell49.Text");
            this.xrTableCell49.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrTableCell49.Weight = 0.88690134707848545D;
            // 
            // xrTableCell50
            // 
            this.xrTableCell50.Font = new DevExpress.Drawing.DXFont("Arial", 10F);
            this.xrTableCell50.Multiline = true;
            this.xrTableCell50.Name = "xrTableCell50";
            this.xrTableCell50.StylePriority.UseFont = false;
            this.xrTableCell50.StylePriority.UseTextAlignment = false;
            this.xrTableCell50.Text = resources.GetString("xrTableCell50.Text");
            this.xrTableCell50.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell50.Weight = 0.90539364777417042D;
            // 
            // xrTable11
            // 
            this.xrTable11.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.xrTable11.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable11.LocationFloat = new DevExpress.Utils.PointFloat(10.69837F, 186.7531F);
            this.xrTable11.Name = "xrTable11";
            this.xrTable11.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable11.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow30});
            this.xrTable11.SizeF = new System.Drawing.SizeF(784.8514F, 23.86349F);
            this.xrTable11.StylePriority.UseBorderColor = false;
            this.xrTable11.StylePriority.UseBorders = false;
            // 
            // xrTableRow30
            // 
            this.xrTableRow30.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell59,
            this.xrTableCell60});
            this.xrTableRow30.Name = "xrTableRow30";
            this.xrTableRow30.Weight = 1D;
            // 
            // xrTableCell59
            // 
            this.xrTableCell59.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell59.Multiline = true;
            this.xrTableCell59.Name = "xrTableCell59";
            this.xrTableCell59.StylePriority.UseFont = false;
            this.xrTableCell59.Text = "ويطلق عليه في هذا العقد (موظف)";
            this.xrTableCell59.Weight = 0.88690134707848545D;
            // 
            // xrTableCell60
            // 
            this.xrTableCell60.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell60.Multiline = true;
            this.xrTableCell60.Name = "xrTableCell60";
            this.xrTableCell60.StylePriority.UseFont = false;
            this.xrTableCell60.StylePriority.UseTextAlignment = false;
            this.xrTableCell60.Text = "Hereinafter referred to as \"The Employee\"";
            this.xrTableCell60.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell60.Weight = 0.90539364777417042D;
            // 
            // xrTable10
            // 
            this.xrTable10.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.xrTable10.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable10.LocationFloat = new DevExpress.Utils.PointFloat(399.8332F, 152.4731F);
            this.xrTable10.Name = "xrTable10";
            this.xrTable10.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable10.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow29});
            this.xrTable10.SizeF = new System.Drawing.SizeF(395.8832F, 34.28003F);
            this.xrTable10.StylePriority.UseBorderColor = false;
            this.xrTable10.StylePriority.UseBorders = false;
            // 
            // xrTableRow29
            // 
            this.xrTableRow29.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell57,
            this.xrTableCell58});
            this.xrTableRow29.Name = "xrTableRow29";
            this.xrTableRow29.Weight = 1D;
            // 
            // xrTableCell57
            // 
            this.xrTableCell57.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[EmployeeEnName]")});
            this.xrTableCell57.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell57.Multiline = true;
            this.xrTableCell57.Name = "xrTableCell57";
            this.xrTableCell57.StylePriority.UseFont = false;
            this.xrTableCell57.StylePriority.UseTextAlignment = false;
            this.xrTableCell57.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell57.Weight = 0.88690134707848545D;
            // 
            // xrTableCell58
            // 
            this.xrTableCell58.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell58.Multiline = true;
            this.xrTableCell58.Name = "xrTableCell58";
            this.xrTableCell58.StylePriority.UseFont = false;
            this.xrTableCell58.StylePriority.UseTextAlignment = false;
            this.xrTableCell58.Text = " : Mr/Mrs";
            this.xrTableCell58.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell58.Weight = 0.90539364777417042D;
            // 
            // xrTable9
            // 
            this.xrTable9.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.xrTable9.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable9.LocationFloat = new DevExpress.Utils.PointFloat(10.69836F, 152.4731F);
            this.xrTable9.Name = "xrTable9";
            this.xrTable9.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable9.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow28});
            this.xrTable9.SizeF = new System.Drawing.SizeF(389.1348F, 34.28003F);
            this.xrTable9.StylePriority.UseBorderColor = false;
            this.xrTable9.StylePriority.UseBorders = false;
            // 
            // xrTableRow28
            // 
            this.xrTableRow28.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell55,
            this.xrTableCell56});
            this.xrTableRow28.Name = "xrTableRow28";
            this.xrTableRow28.Weight = 1D;
            // 
            // xrTableCell55
            // 
            this.xrTableCell55.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell55.Multiline = true;
            this.xrTableCell55.Name = "xrTableCell55";
            this.xrTableCell55.StylePriority.UseFont = false;
            this.xrTableCell55.Text = "السيد/السيدة :";
            this.xrTableCell55.Weight = 0.88690134707848545D;
            // 
            // xrTableCell56
            // 
            this.xrTableCell56.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[EmployeeName]")});
            this.xrTableCell56.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell56.Multiline = true;
            this.xrTableCell56.Name = "xrTableCell56";
            this.xrTableCell56.StylePriority.UseFont = false;
            this.xrTableCell56.StylePriority.UseTextAlignment = false;
            this.xrTableCell56.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrTableCell56.Weight = 0.90539364777417042D;
            // 
            // xrTable4
            // 
            this.xrTable4.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.xrTable4.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable4.LocationFloat = new DevExpress.Utils.PointFloat(10.61505F, 210.6166F);
            this.xrTable4.Name = "xrTable4";
            this.xrTable4.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable4.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow21,
            this.xrTableRow23,
            this.xrTableRow24});
            this.xrTable4.SizeF = new System.Drawing.SizeF(389.1348F, 78.7869F);
            this.xrTable4.StylePriority.UseBorderColor = false;
            this.xrTable4.StylePriority.UseBorders = false;
            // 
            // xrTableRow21
            // 
            this.xrTableRow21.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell41,
            this.xrTableCell42});
            this.xrTableRow21.Name = "xrTableRow21";
            this.xrTableRow21.Weight = 1D;
            // 
            // xrTableCell41
            // 
            this.xrTableCell41.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell41.Multiline = true;
            this.xrTableCell41.Name = "xrTableCell41";
            this.xrTableCell41.StylePriority.UseFont = false;
            this.xrTableCell41.Text = "رقم الإقامة :";
            this.xrTableCell41.Weight = 0.88690134707848545D;
            // 
            // xrTableCell42
            // 
            this.xrTableCell42.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[NationalId]")});
            this.xrTableCell42.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell42.Multiline = true;
            this.xrTableCell42.Name = "xrTableCell42";
            this.xrTableCell42.StylePriority.UseFont = false;
            this.xrTableCell42.StylePriority.UseTextAlignment = false;
            this.xrTableCell42.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrTableCell42.Weight = 0.90539364777417042D;
            // 
            // xrTableRow23
            // 
            this.xrTableRow23.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell45,
            this.xrTableCell46});
            this.xrTableRow23.Name = "xrTableRow23";
            this.xrTableRow23.Weight = 1D;
            // 
            // xrTableCell45
            // 
            this.xrTableCell45.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell45.Multiline = true;
            this.xrTableCell45.Name = "xrTableCell45";
            this.xrTableCell45.StylePriority.UseFont = false;
            this.xrTableCell45.Text = "الجنسية :";
            this.xrTableCell45.Weight = 0.88690134707848545D;
            // 
            // xrTableCell46
            // 
            this.xrTableCell46.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Nationality]")});
            this.xrTableCell46.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell46.Multiline = true;
            this.xrTableCell46.Name = "xrTableCell46";
            this.xrTableCell46.StylePriority.UseFont = false;
            this.xrTableCell46.StylePriority.UseTextAlignment = false;
            this.xrTableCell46.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrTableCell46.Weight = 0.90539364777417042D;
            // 
            // xrTableRow24
            // 
            this.xrTableRow24.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell47,
            this.xrTableCell48});
            this.xrTableRow24.Name = "xrTableRow24";
            this.xrTableRow24.Weight = 1D;
            // 
            // xrTableCell47
            // 
            this.xrTableCell47.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell47.Multiline = true;
            this.xrTableCell47.Name = "xrTableCell47";
            this.xrTableCell47.StylePriority.UseFont = false;
            this.xrTableCell47.Text = "العنوان :";
            this.xrTableCell47.Weight = 0.88690134707848545D;
            // 
            // xrTableCell48
            // 
            this.xrTableCell48.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[LivingAddress]")});
            this.xrTableCell48.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell48.Multiline = true;
            this.xrTableCell48.Name = "xrTableCell48";
            this.xrTableCell48.StylePriority.UseFont = false;
            this.xrTableCell48.StylePriority.UseTextAlignment = false;
            this.xrTableCell48.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrTableCell48.Weight = 0.90539364777417042D;
            // 
            // xrTable1
            // 
            this.xrTable1.BackColor = System.Drawing.Color.LightGray;
            this.xrTable1.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.xrTable1.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable1.Font = new DevExpress.Drawing.DXFont("Arial", 15F);
            this.xrTable1.ForeColor = System.Drawing.Color.Black;
            this.xrTable1.LocationFloat = new DevExpress.Utils.PointFloat(10.61499F, 127.4731F);
            this.xrTable1.Name = "xrTable1";
            this.xrTable1.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable1.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow14});
            this.xrTable1.SizeF = new System.Drawing.SizeF(786.4684F, 24.99997F);
            this.xrTable1.StylePriority.UseBackColor = false;
            this.xrTable1.StylePriority.UseBorderColor = false;
            this.xrTable1.StylePriority.UseBorders = false;
            this.xrTable1.StylePriority.UseFont = false;
            this.xrTable1.StylePriority.UseForeColor = false;
            // 
            // xrTableRow14
            // 
            this.xrTableRow14.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell25,
            this.xrTableCell28});
            this.xrTableRow14.Name = "xrTableRow14";
            this.xrTableRow14.Weight = 1D;
            // 
            // xrTableCell25
            // 
            this.xrTableCell25.BorderColor = System.Drawing.Color.Silver;
            this.xrTableCell25.Multiline = true;
            this.xrTableCell25.Name = "xrTableCell25";
            this.xrTableCell25.StylePriority.UseBorderColor = false;
            this.xrTableCell25.Text = "الطرف الثاني";
            this.xrTableCell25.Weight = 1.4843630411355744D;
            // 
            // xrTableCell28
            // 
            this.xrTableCell28.BorderColor = System.Drawing.Color.Silver;
            this.xrTableCell28.Multiline = true;
            this.xrTableCell28.Name = "xrTableCell28";
            this.xrTableCell28.StylePriority.UseBorderColor = false;
            this.xrTableCell28.StylePriority.UseTextAlignment = false;
            this.xrTableCell28.Text = "Second Party";
            this.xrTableCell28.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell28.Weight = 1.5162793179592744D;
            // 
            // xrTable3
            // 
            this.xrTable3.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.xrTable3.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable3.LocationFloat = new DevExpress.Utils.PointFloat(399.7499F, 210.6166F);
            this.xrTable3.Name = "xrTable3";
            this.xrTable3.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable3.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow16,
            this.xrTableRow18,
            this.xrTableRow19});
            this.xrTable3.SizeF = new System.Drawing.SizeF(395.9665F, 78.7869F);
            this.xrTable3.StylePriority.UseBorderColor = false;
            this.xrTable3.StylePriority.UseBorders = false;
            // 
            // xrTableRow16
            // 
            this.xrTableRow16.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell31,
            this.xrTableCell32});
            this.xrTableRow16.Name = "xrTableRow16";
            this.xrTableRow16.Weight = 1D;
            // 
            // xrTableCell31
            // 
            this.xrTableCell31.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[NationalId]")});
            this.xrTableCell31.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell31.Multiline = true;
            this.xrTableCell31.Name = "xrTableCell31";
            this.xrTableCell31.StylePriority.UseFont = false;
            this.xrTableCell31.StylePriority.UseTextAlignment = false;
            this.xrTableCell31.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell31.Weight = 0.88690134707848545D;
            // 
            // xrTableCell32
            // 
            this.xrTableCell32.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell32.Multiline = true;
            this.xrTableCell32.Name = "xrTableCell32";
            this.xrTableCell32.StylePriority.UseFont = false;
            this.xrTableCell32.StylePriority.UseTextAlignment = false;
            this.xrTableCell32.Text = " : .ID No ";
            this.xrTableCell32.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell32.Weight = 0.90539364777417042D;
            // 
            // xrTableRow18
            // 
            this.xrTableRow18.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell35,
            this.xrTableCell36});
            this.xrTableRow18.Name = "xrTableRow18";
            this.xrTableRow18.Weight = 1D;
            // 
            // xrTableCell35
            // 
            this.xrTableCell35.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[NationalityEnName]")});
            this.xrTableCell35.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell35.Multiline = true;
            this.xrTableCell35.Name = "xrTableCell35";
            this.xrTableCell35.StylePriority.UseFont = false;
            this.xrTableCell35.StylePriority.UseTextAlignment = false;
            this.xrTableCell35.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell35.Weight = 0.88690134707848545D;
            // 
            // xrTableCell36
            // 
            this.xrTableCell36.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell36.Multiline = true;
            this.xrTableCell36.Name = "xrTableCell36";
            this.xrTableCell36.StylePriority.UseFont = false;
            this.xrTableCell36.StylePriority.UseTextAlignment = false;
            this.xrTableCell36.Text = "  : Nationality ";
            this.xrTableCell36.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell36.Weight = 0.90539364777417042D;
            // 
            // xrTableRow19
            // 
            this.xrTableRow19.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell37,
            this.xrTableCell38});
            this.xrTableRow19.Name = "xrTableRow19";
            this.xrTableRow19.Weight = 1D;
            // 
            // xrTableCell37
            // 
            this.xrTableCell37.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[LivingAddress]")});
            this.xrTableCell37.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell37.Multiline = true;
            this.xrTableCell37.Name = "xrTableCell37";
            this.xrTableCell37.StylePriority.UseFont = false;
            this.xrTableCell37.StylePriority.UseTextAlignment = false;
            this.xrTableCell37.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell37.Weight = 0.88690134707848545D;
            // 
            // xrTableCell38
            // 
            this.xrTableCell38.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell38.Multiline = true;
            this.xrTableCell38.Name = "xrTableCell38";
            this.xrTableCell38.StylePriority.UseFont = false;
            this.xrTableCell38.StylePriority.UseTextAlignment = false;
            this.xrTableCell38.Text = " : Address";
            this.xrTableCell38.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell38.Weight = 0.90539364777417042D;
            // 
            // xrTable6
            // 
            this.xrTable6.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.xrTable6.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable6.LocationFloat = new DevExpress.Utils.PointFloat(399.6666F, 94.23476F);
            this.xrTable6.Name = "xrTable6";
            this.xrTable6.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable6.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow6});
            this.xrTable6.SizeF = new System.Drawing.SizeF(395.9665F, 33.23836F);
            this.xrTable6.StylePriority.UseBorderColor = false;
            this.xrTable6.StylePriority.UseBorders = false;
            // 
            // xrTableRow6
            // 
            this.xrTableRow6.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell17,
            this.xrTableCell18});
            this.xrTableRow6.Name = "xrTableRow6";
            this.xrTableRow6.Weight = 1D;
            // 
            // xrTableCell17
            // 
            this.xrTableCell17.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CommercialRegistrationNo]")});
            this.xrTableCell17.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell17.Multiline = true;
            this.xrTableCell17.Name = "xrTableCell17";
            this.xrTableCell17.StylePriority.UseFont = false;
            this.xrTableCell17.StylePriority.UseTextAlignment = false;
            this.xrTableCell17.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell17.Weight = 0.88690134707848545D;
            // 
            // xrTableCell18
            // 
            this.xrTableCell18.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell18.Multiline = true;
            this.xrTableCell18.Name = "xrTableCell18";
            this.xrTableCell18.StylePriority.UseFont = false;
            this.xrTableCell18.StylePriority.UseTextAlignment = false;
            this.xrTableCell18.Text = " : .C. R. No ";
            this.xrTableCell18.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell18.Weight = 0.90539364777417042D;
            // 
            // xrTable5
            // 
            this.xrTable5.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.xrTable5.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable5.LocationFloat = new DevExpress.Utils.PointFloat(10.53174F, 94.23476F);
            this.xrTable5.Name = "xrTable5";
            this.xrTable5.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable5.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow5});
            this.xrTable5.SizeF = new System.Drawing.SizeF(389.1348F, 33.23836F);
            this.xrTable5.StylePriority.UseBorderColor = false;
            this.xrTable5.StylePriority.UseBorders = false;
            // 
            // xrTableRow5
            // 
            this.xrTableRow5.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell3,
            this.xrTableCell9});
            this.xrTableRow5.Name = "xrTableRow5";
            this.xrTableRow5.Weight = 1D;
            // 
            // xrTableCell3
            // 
            this.xrTableCell3.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell3.Multiline = true;
            this.xrTableCell3.Name = "xrTableCell3";
            this.xrTableCell3.StylePriority.UseFont = false;
            this.xrTableCell3.Text = "رقم س. ت . :";
            this.xrTableCell3.Weight = 0.88690134707848545D;
            // 
            // xrTableCell9
            // 
            this.xrTableCell9.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CommercialRegistrationNo]")});
            this.xrTableCell9.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell9.Multiline = true;
            this.xrTableCell9.Name = "xrTableCell9";
            this.xrTableCell9.StylePriority.UseFont = false;
            this.xrTableCell9.StylePriority.UseTextAlignment = false;
            this.xrTableCell9.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrTableCell9.Weight = 0.90539364777417042D;
            // 
            // xrTable13
            // 
            this.xrTable13.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.xrTable13.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable13.LocationFloat = new DevExpress.Utils.PointFloat(10.53174F, 35F);
            this.xrTable13.Name = "xrTable13";
            this.xrTable13.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable13.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow9});
            this.xrTable13.SizeF = new System.Drawing.SizeF(786.5516F, 59.23476F);
            this.xrTable13.StylePriority.UseBorderColor = false;
            this.xrTable13.StylePriority.UseBorders = false;
            // 
            // xrTableRow9
            // 
            this.xrTableRow9.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell26,
            this.xrTableCell27});
            this.xrTableRow9.Name = "xrTableRow9";
            this.xrTableRow9.Weight = 1D;
            // 
            // xrTableCell26
            // 
            this.xrTableCell26.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CompanyArName]")});
            this.xrTableCell26.Font = new DevExpress.Drawing.DXFont("Arial", 14F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell26.Multiline = true;
            this.xrTableCell26.Name = "xrTableCell26";
            this.xrTableCell26.StylePriority.UseFont = false;
            this.xrTableCell26.StylePriority.UseTextAlignment = false;
            this.xrTableCell26.Text = "شركة رايات العناية التجارية";
            this.xrTableCell26.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrTableCell26.Weight = 0.88690134707848545D;
            // 
            // xrTableCell27
            // 
            this.xrTableCell27.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CompanyEnName]")});
            this.xrTableCell27.Font = new DevExpress.Drawing.DXFont("Arial", 14F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell27.Multiline = true;
            this.xrTableCell27.Name = "xrTableCell27";
            this.xrTableCell27.StylePriority.UseFont = false;
            this.xrTableCell27.StylePriority.UseTextAlignment = false;
            this.xrTableCell27.Text = ".Rayat Aleinaya Co";
            this.xrTableCell27.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell27.Weight = 0.90539364777417042D;
            // 
            // xrTable8
            // 
            this.xrTable8.BackColor = System.Drawing.Color.LightGray;
            this.xrTable8.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.xrTable8.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable8.Font = new DevExpress.Drawing.DXFont("Arial", 15F);
            this.xrTable8.ForeColor = System.Drawing.Color.Black;
            this.xrTable8.LocationFloat = new DevExpress.Utils.PointFloat(10.53168F, 10F);
            this.xrTable8.Name = "xrTable8";
            this.xrTable8.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable8.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow8});
            this.xrTable8.SizeF = new System.Drawing.SizeF(785.3516F, 24.99997F);
            this.xrTable8.StylePriority.UseBackColor = false;
            this.xrTable8.StylePriority.UseBorderColor = false;
            this.xrTable8.StylePriority.UseBorders = false;
            this.xrTable8.StylePriority.UseFont = false;
            this.xrTable8.StylePriority.UseForeColor = false;
            // 
            // xrTableRow8
            // 
            this.xrTableRow8.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell23,
            this.xrTableCell24});
            this.xrTableRow8.Name = "xrTableRow8";
            this.xrTableRow8.Weight = 1D;
            // 
            // xrTableCell23
            // 
            this.xrTableCell23.BorderColor = System.Drawing.Color.Silver;
            this.xrTableCell23.Multiline = true;
            this.xrTableCell23.Name = "xrTableCell23";
            this.xrTableCell23.StylePriority.UseBorderColor = false;
            this.xrTableCell23.Text = "الطرف الأول";
            this.xrTableCell23.Weight = 1.4843630411355744D;
            // 
            // xrTableCell24
            // 
            this.xrTableCell24.BorderColor = System.Drawing.Color.Silver;
            this.xrTableCell24.Multiline = true;
            this.xrTableCell24.Name = "xrTableCell24";
            this.xrTableCell24.StylePriority.UseBorderColor = false;
            this.xrTableCell24.StylePriority.UseTextAlignment = false;
            this.xrTableCell24.Text = "First Party";
            this.xrTableCell24.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrTableCell24.Weight = 1.512018267754873D;
            // 
            // xrTable7
            // 
            this.xrTable7.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.xrTable7.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable7.LocationFloat = new DevExpress.Utils.PointFloat(15.09171F, 128.7997F);
            this.xrTable7.Name = "xrTable7";
            this.xrTable7.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.xrTable7.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow7});
            this.xrTable7.SizeF = new System.Drawing.SizeF(786.8852F, 36.15508F);
            this.xrTable7.StylePriority.UseBorderColor = false;
            this.xrTable7.StylePriority.UseBorders = false;
            // 
            // xrTableRow7
            // 
            this.xrTableRow7.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell21,
            this.xrTableCell1,
            this.xrTableCell2,
            this.xrTableCell22});
            this.xrTableRow7.Name = "xrTableRow7";
            this.xrTableRow7.Weight = 1D;
            // 
            // xrTableCell21
            // 
            this.xrTableCell21.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell21.Multiline = true;
            this.xrTableCell21.Name = "xrTableCell21";
            this.xrTableCell21.StylePriority.UseFont = false;
            this.xrTableCell21.Text = "التاريخ : ";
            this.xrTableCell21.Weight = 0.88671143435920718D;
            // 
            // xrTableCell1
            // 
            this.xrTableCell1.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell1.Multiline = true;
            this.xrTableCell1.Name = "xrTableCell1";
            this.xrTableCell1.StylePriority.UseFont = false;
            this.xrTableCell1.StylePriority.UseTextAlignment = false;
            this.xrTableCell1.Text = "xrTableCell1";
            this.xrTableCell1.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrTableCell1.Weight = 0.86345911305282641D;
            // 
            // xrTableCell2
            // 
            this.xrTableCell2.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell2.Multiline = true;
            this.xrTableCell2.Name = "xrTableCell2";
            this.xrTableCell2.StylePriority.UseFont = false;
            this.xrTableCell2.StylePriority.UseTextAlignment = false;
            this.xrTableCell2.Text = "xrTableCell2";
            this.xrTableCell2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell2.Weight = 0.909963755665588D;
            // 
            // xrTableCell22
            // 
            this.xrTableCell22.Font = new DevExpress.Drawing.DXFont("Arial", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell22.Multiline = true;
            this.xrTableCell22.Name = "xrTableCell22";
            this.xrTableCell22.StylePriority.UseFont = false;
            this.xrTableCell22.StylePriority.UseTextAlignment = false;
            this.xrTableCell22.Text = " : Date ";
            this.xrTableCell22.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell22.Weight = 0.90558356049344868D;
            // 
            // xrLabel3
            // 
            this.xrLabel3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(3)))), ((int)(((byte)(142)))), ((int)(((byte)(188)))));
            this.xrLabel3.Font = new DevExpress.Drawing.DXFont("Arial", 12F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel3.ForeColor = System.Drawing.Color.White;
            this.xrLabel3.LocationFloat = new DevExpress.Utils.PointFloat(329.4363F, 21.48817F);
            this.xrLabel3.Multiline = true;
            this.xrLabel3.Name = "xrLabel3";
            this.xrLabel3.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel3.SizeF = new System.Drawing.SizeF(187.3335F, 41.90756F);
            this.xrLabel3.StylePriority.UseBackColor = false;
            this.xrLabel3.StylePriority.UseFont = false;
            this.xrLabel3.StylePriority.UseForeColor = false;
            this.xrLabel3.StylePriority.UseTextAlignment = false;
            this.xrLabel3.Text = "عقد عمل";
            this.xrLabel3.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel7
            // 
            this.xrLabel7.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(3)))), ((int)(((byte)(142)))), ((int)(((byte)(188)))));
            this.xrLabel7.BorderColor = System.Drawing.SystemColors.ControlLight;
            this.xrLabel7.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel7.Font = new DevExpress.Drawing.DXFont("Arial", 12F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel7.ForeColor = System.Drawing.Color.White;
            this.xrLabel7.LocationFloat = new DevExpress.Utils.PointFloat(258.8463F, 63.39574F);
            this.xrLabel7.Multiline = true;
            this.xrLabel7.Name = "xrLabel7";
            this.xrLabel7.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel7.SizeF = new System.Drawing.SizeF(330.359F, 41.90756F);
            this.xrLabel7.StylePriority.UseBackColor = false;
            this.xrLabel7.StylePriority.UseBorderColor = false;
            this.xrLabel7.StylePriority.UseBorders = false;
            this.xrLabel7.StylePriority.UseFont = false;
            this.xrLabel7.StylePriority.UseForeColor = false;
            this.xrLabel7.StylePriority.UseTextAlignment = false;
            this.xrLabel7.Text = "Employee Contract";
            this.xrLabel7.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // ExtendedTotal
            // 
            this.ExtendedTotal.DataMember = "EmployeesContract_Get.EmployeesContract_GetQuery";
            this.ExtendedTotal.Expression = "[Price] * [Qty]";
            this.ExtendedTotal.Name = "ExtendedTotal";
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
            // Id
            // 
            this.Id.Description = "Id";
            this.Id.Name = "Id";
            this.Id.Type = typeof(int);
            this.Id.ValueInfo = "0";
            this.Id.Visible = false;
            // 
            // PageHeader
            // 
            this.PageHeader.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.CompanyName,
            this.xrPictureBox1,
            this.xrTable7,
            this.xrLabel7,
            this.xrLabel3});
            this.PageHeader.HeightF = 174.9548F;
            this.PageHeader.Name = "PageHeader";
            // 
            // xrPictureBox1
            // 
            this.xrPictureBox1.ImageUrl = "assets\\images\\logo.png";
            this.xrPictureBox1.LocationFloat = new DevExpress.Utils.PointFloat(602.0487F, 10.00001F);
            this.xrPictureBox1.Name = "xrPictureBox1";
            this.xrPictureBox1.SizeF = new System.Drawing.SizeF(193.4038F, 95.3033F);
            this.xrPictureBox1.Sizing = DevExpress.XtraPrinting.ImageSizeMode.ZoomImage;
            this.xrPictureBox1.BeforePrint += new BeforePrintEventHandler(this.xrPictureBox1_BeforePrint);
            // 
            // CompanyName
            // 
            this.CompanyName.BackColor = System.Drawing.Color.LightGray;
            this.CompanyName.BorderColor = System.Drawing.Color.Silver;
            this.CompanyName.Font = new DevExpress.Drawing.DXFont("Arial", 12F, DevExpress.Drawing.DXFontStyle.Bold);
            this.CompanyName.ForeColor = System.Drawing.Color.White;
            this.CompanyName.LocationFloat = new DevExpress.Utils.PointFloat(22.99078F, 40.39574F);
            this.CompanyName.Multiline = true;
            this.CompanyName.Name = "CompanyName";
            this.CompanyName.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.CompanyName.SizeF = new System.Drawing.SizeF(187.6977F, 23F);
            this.CompanyName.StylePriority.UseBackColor = false;
            this.CompanyName.StylePriority.UseBorderColor = false;
            this.CompanyName.StylePriority.UseFont = false;
            this.CompanyName.StylePriority.UseForeColor = false;
            this.CompanyName.StylePriority.UseTextAlignment = false;
            this.CompanyName.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // EmployeesContract_Report
            // 
            this.Bands.AddRange(new DevExpress.XtraReports.UI.Band[] {
            this.TopMargin,
            this.BottomMargin,
            this.Detail,
            this.PageHeader});
            this.CalculatedFields.AddRange(new DevExpress.XtraReports.UI.CalculatedField[] {
            this.ExtendedTotal});
            this.ComponentStorage.AddRange(new System.ComponentModel.IComponent[] {
            this.sqlDataSource1});
            this.DataMember = "EmployeesContract_Get";
            this.DataSource = this.sqlDataSource1;
            this.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.Margins = new DevExpress.Drawing.DXMargins(0, 16, 20, 48);
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
            this.Version = "20.1";
            this.BeforePrint += new BeforePrintEventHandler(this.EmployeesContract_Report_BeforePrint);
            ((System.ComponentModel.ISupportInitialize)(this.xrTable41)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable40)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable38)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable39)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable36)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable37)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable35)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable34)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable32)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable33)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable30)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable31)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable28)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable29)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable26)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable27)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable24)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable25)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable22)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable23)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable20)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable21)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable17)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable18)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable19)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable15)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable12)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable16)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable14)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable11)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable10)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable9)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable4)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable6)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable5)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable13)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable8)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable7)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();

    }



    #endregion

    private void EmployeesContract_Report_BeforePrint(object sender, CancelEventArgs e)
    {
        //------ Time Zone Depends On Currency --------//
        var Currency = db.Currencies.Where(a => a.IsActive == true && a.IsDeleted == false && a.IsDefault == true).FirstOrDefault();
        var CurrencyCode = Currency != null ? Currency.Code : "";
        TimeZoneInfo info;
        if (CurrencyCode == "SAR")
        {
            //info = TimeZoneInfo.FindSystemTimeZoneById("Arabian Standard Time");//+2H from Egypt Standard Time
            info = TimeZoneInfo.FindSystemTimeZoneById("Arab Standard Time");// +1H from Egypt Standard Time
        }
        else
        {
            info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
        }
        DateTime utcNow = DateTime.UtcNow;
        DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
        xrTableCell1.Text = cTime.ToString("d MMMM yyyy h:mm tt");
        xrTableCell2.Text = cTime.ToString("d MMMM yyyy h:mm tt");
    }
    
    private void xrPictureBox1_BeforePrint(object sender, CancelEventArgs e)
    {
        int? DepartmentId = null;// (int?)this.DepartmentId.Value;
        var logo = HelperController.GetActivityLogo(DepartmentId);
        xrPictureBox1.ImageUrl = logo;
        CompanyName.Text = db.SystemSettings.FirstOrDefault().CompanyArName;
    }
}