using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using MyERP.Reporting;
using MyERP.Reporting.Reports;
using MyERP.Reporting.Reports.ReportDataSetTableAdapters;

namespace MyERP.Utils
{
    public static class AdapterConnector
    {
        private static SqlConnection GetConnection()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["MyERP_ConnectionString"]?.ConnectionString;
            if (string.IsNullOrEmpty(connectionString))
                throw new Exception("لم يتم العثور على MyERP_ConnectionString في ملف Web.config");
            return new SqlConnection(connectionString);
        }

        public static void ConnectAllAdapters(ReportDataSet ds)
        {
            var conn = GetConnection();

            new PropertyRenterTableAdapter { Connection = conn }.Fill(ds.PropertyRenter);
            new DepartmentTableAdapter { Connection = conn }.Fill(ds.Department);
            new PropertyTableAdapter { Connection = conn }.Fill(ds.Property);
        }

        public static PropertyReportAdapter GetPropertyReportAdapter()
        {
            var adapter = new PropertyReportAdapter();
            adapter.SetConnection(GetConnection());
            return adapter;
        }
        public static ExpiredContractsReportAdapter GetExpiredContractsReportAdapter()
        {
            var adapter = new ExpiredContractsReportAdapter();
            adapter.SetConnection(GetConnection());
            return adapter;
        }

    }
    public class ExpiredContractsReportAdapter
    {
        private readonly GetExpiredPropertyContractsTableAdapter _adapter;

        public ExpiredContractsReportAdapter()
        {
            _adapter = new GetExpiredPropertyContractsTableAdapter();
        }

        public void SetConnection(SqlConnection conn)
        {
            _adapter.Connection = conn;
        }

        public ReportDataSet.GetExpiredPropertyContractsDataTable GetReport()
        {
            try
            {
                var table = new ReportDataSet.GetExpiredPropertyContractsDataTable();

                // منع التحقق من القيود
                if (table.DataSet != null)
                    table.DataSet.EnforceConstraints = false;

                // ربط الكونكشن يدويًا إن لزم الأمر (في بعض الحالات)
                _adapter.Connection = new SqlConnection(ConfigurationManager.ConnectionStrings["MyERP_ConnectionString"].ConnectionString);

                // استخدم Fill بدلاً من GetData
                _adapter.Fill(table);

                // الآن نقدر نفحص الصفوف
                foreach (DataRow row in table.Rows)
                {
                    if (row.HasErrors)
                    {
                        foreach (var col in row.GetColumnsInError())
                        {
                            var err = row.GetColumnError(col);
                            System.Diagnostics.Debug.WriteLine($"❗ Column: {col}, Error: {err}");
                        }
                    }
                }

                return table;
            }
            catch (Exception ex)
            {
                throw new Exception("Connection error: " + _adapter.Connection?.ConnectionString, ex);
            }
        }

    }


    public class PropertyReportAdapter
    {
        private readonly sp_GetPropertyReportTableAdapter _adapter;

        public PropertyReportAdapter()
        {
            _adapter = new sp_GetPropertyReportTableAdapter();
        }

        public void SetConnection(SqlConnection conn)
        {
            _adapter.Connection = conn;
        }

        public ReportDataSet.sp_GetPropertyReportDataTable GetReport(
            int? renterId, int? ownerId, int? propertyId, DateTime? startDate, bool onlyUnterminated)
        {
            try
            {
                return _adapter.GetData(renterId, ownerId, propertyId, startDate, onlyUnterminated);
            }
            catch (Exception ex)
            {
                throw new Exception("Connection error: " + _adapter.Connection?.ConnectionString, ex);
            }
        }
    }


}



