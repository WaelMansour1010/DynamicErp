using System;
using System.Configuration;
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
