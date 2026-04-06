using DevExpress.DataAccess.Sql;
using DevExpress.XtraReports.Parameters;
using DevExpress.XtraReports.UI;
using System;
using System.Drawing;

namespace MyERP.Reporting.Reports
{
    public class CarCurrentStock_Report : XtraReport
    {
        private TopMarginBand topMarginBand1;
        private DetailBand detailBand1;
        private SqlDataSource sqlDataSource1;
        private System.ComponentModel.IContainer components;
        private BottomMarginBand bottomMarginBand1;

        public CarCurrentStock_Report(DateTime? fromDate, DateTime? toDate, int? warehouseId, int? carTypeId, int? carModelId)
        {
            var pFromDate = new Parameter { Name = "FromDate", Type = typeof(DateTime), Value = fromDate.HasValue ? (object)fromDate.Value : DBNull.Value, Visible = false };
            var pToDate = new Parameter { Name = "ToDate", Type = typeof(DateTime), Value = toDate.HasValue ? (object)toDate.Value : DBNull.Value, Visible = false };
            var pWarehouseId = new Parameter { Name = "WarehouseId", Type = typeof(int), Value = warehouseId.HasValue ? (object)warehouseId.Value : DBNull.Value, Visible = false };
            var pCarTypeId = new Parameter { Name = "CarTypeId", Type = typeof(int), Value = carTypeId.HasValue ? (object)carTypeId.Value : DBNull.Value, Visible = false };
            var pCarModelId = new Parameter { Name = "CarModelId", Type = typeof(int), Value = carModelId.HasValue ? (object)carModelId.Value : DBNull.Value, Visible = false };
            Parameters.AddRange(new[] { pFromDate, pToDate, pWarehouseId, pCarTypeId, pCarModelId });

            var sqlDataSource = new SqlDataSource("ConnectionString");
            var query = new StoredProcQuery("CarCurrentStock_Get", "CarCurrentStock_Get");
            query.Parameters.Add(new QueryParameter("FromDate", typeof(DevExpress.DataAccess.Expression), new DevExpress.DataAccess.Expression("?FromDate", typeof(DateTime))));
            query.Parameters.Add(new QueryParameter("ToDate", typeof(DevExpress.DataAccess.Expression), new DevExpress.DataAccess.Expression("?ToDate", typeof(DateTime))));
            query.Parameters.Add(new QueryParameter("WarehouseId", typeof(DevExpress.DataAccess.Expression), new DevExpress.DataAccess.Expression("?WarehouseId", typeof(int))));
            query.Parameters.Add(new QueryParameter("CarTypeId", typeof(DevExpress.DataAccess.Expression), new DevExpress.DataAccess.Expression("?CarTypeId", typeof(int))));
            query.Parameters.Add(new QueryParameter("CarModelId", typeof(DevExpress.DataAccess.Expression), new DevExpress.DataAccess.Expression("?CarModelId", typeof(int))));
            sqlDataSource.Queries.Add(query);
            sqlDataSource.RebuildResultSchema();

            DataSource = sqlDataSource;
            DataMember = "CarCurrentStock_Get";

            //TopMargin.HeightF = 20;
            //BottomMargin.HeightF = 20;

            var header = new ReportHeaderBand { HeightF = 35 };
            var title = new XRLabel
            {
                Text = "تقرير مخزون السيارات الحالي",
                BoundsF = new RectangleF(0, 0, 1100, 30),
                TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter,
                Font = new Font("Arial", 14, FontStyle.Bold)
            };
            header.Controls.Add(title);
            Bands.Add(header);

            var detail = new DetailBand { HeightF = 25 };
            var table = new XRTable { BoundsF = new RectangleF(0, 0, 1100, 25), Font = new Font("Arial", 8) };
            var row = new XRTableRow();
            AddCell(row, "[ChassisNo]");
            AddCell(row, "[CarType]");
            AddCell(row, "[CarModel]");
            AddCell(row, "[CarColor]");
            AddCell(row, "[ManufacturingYear]");
            AddCell(row, "[EngineNo]");
            AddCell(row, "[PlateNo]");
            AddCell(row, "[VendorName]");
            AddCell(row, "[PurchaseInvoiceNumber]");
            AddCell(row, "[PurchaseDate]");
            AddCell(row, "[PurchaseCost]");
            AddCell(row, "[WarehouseName]");
            AddCell(row, "[VehicleNotes]");
            table.Rows.Add(row);
            detail.Controls.Add(table);
            Bands.Add(detail);

            var groupHeader = new GroupHeaderBand { HeightF = 30 };
            var headerTable = new XRTable { BoundsF = new RectangleF(0, 0, 1100, 30), Font = new Font("Arial", 8, FontStyle.Bold) };
            var headerRow = new XRTableRow();
            foreach (var txt in new[] { "الشاسيه", "النوع", "الموديل", "اللون", "سنة الصنع", "المحرك", "اللوحة", "المورد", "رقم فاتورة الشراء", "تاريخ الشراء", "تكلفة الشراء", "المخزن", "ملاحظات" })
                headerRow.Cells.Add(new XRTableCell { Text = txt, Borders = DevExpress.XtraPrinting.BorderSide.All, TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter });
            headerTable.Rows.Add(headerRow);
            groupHeader.Controls.Add(headerTable);
            Bands.Add(groupHeader);

            Landscape = true;
            PaperKind = DevExpress.Drawing.Printing.DXPaperKind.A4;
            RequestParameters = false;
        }

        private static void AddCell(XRTableRow row, string expression)
        {
            var cell = new XRTableCell { Borders = DevExpress.XtraPrinting.BorderSide.All, TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter };
            cell.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", expression));
            row.Cells.Add(cell);
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CarCurrentStock_Report));
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
            this.sqlDataSource1.ConnectionName = "MyErpConnectionString";
            this.sqlDataSource1.Name = "sqlDataSource1";
            storedProcQuery1.Name = "CarCurrentStock_Get";
            queryParameter1.Name = "@FromDate";
            queryParameter1.Type = typeof(System.DateTime);
            queryParameter1.ValueInfo = "1753-01-01";
            queryParameter2.Name = "@ToDate";
            queryParameter2.Type = typeof(System.DateTime);
            queryParameter2.ValueInfo = "1753-01-01";
            queryParameter3.Name = "@WarehouseId";
            queryParameter3.Type = typeof(int);
            queryParameter3.ValueInfo = "0";
            queryParameter4.Name = "@CarTypeId";
            queryParameter4.Type = typeof(int);
            queryParameter4.ValueInfo = "0";
            queryParameter5.Name = "@CarModelId";
            queryParameter5.Type = typeof(int);
            queryParameter5.ValueInfo = "0";
            storedProcQuery1.Parameters.AddRange(new DevExpress.DataAccess.Sql.QueryParameter[] {
            queryParameter1,
            queryParameter2,
            queryParameter3,
            queryParameter4,
            queryParameter5});
            storedProcQuery1.StoredProcName = "CarCurrentStock_Get";
            this.sqlDataSource1.Queries.AddRange(new DevExpress.DataAccess.Sql.SqlQuery[] {
            storedProcQuery1});
            this.sqlDataSource1.ResultSchemaSerializable = resources.GetString("sqlDataSource1.ResultSchemaSerializable");
            // 
            // CarCurrentStock_Report
            // 
            this.Bands.AddRange(new DevExpress.XtraReports.UI.Band[] {
            this.topMarginBand1,
            this.detailBand1,
            this.bottomMarginBand1});
            this.ComponentStorage.AddRange(new System.ComponentModel.IComponent[] {
            this.sqlDataSource1});
            this.DataMember = "CarCurrentStock_Get";
            this.DataSource = this.sqlDataSource1;
            this.Version = "23.1";
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();

        }
    }
}
