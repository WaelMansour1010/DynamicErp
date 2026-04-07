using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using FastMember;

namespace MyERP
{
    public class MyXML
    {
        public static string xPathName;
        public static string GetXML(DataTable dt)
        {
            foreach (DataColumn dc in dt.Columns)
            {

                if (dc.DataType == typeof(DateTime))
                {
                    dc.DateTimeMode = DataSetDateTime.Unspecified;
                }
            }
            string strXML = null;
            using (var sw = new StringWriter())
            {
                dt.WriteXml(sw);
                strXML = sw.ToString();
            }
            return strXML;
        }

        public static DataTable GetTable<T>(IEnumerable<T> collection)
        {
            DataTable table = new DataTable();
            using (var reader = ObjectReader.Create(collection))
            {
                table.Load(reader);
                table.TableName = xPathName;
                xPathName = null;
            }
            foreach (DataColumn dc in table.Columns)
            {

                if (dc.DataType == typeof(DateTime))
                {
                    dc.DateTimeMode = DataSetDateTime.Unspecified;
                }
            }
            return table;
        }

        public static string GetXML<T>(IEnumerable<T> collection)
        {
            if (collection != null)
            {
                return GetXML(GetTable(collection));
            }
            else
            {
                return null;
            }
        }
    }
}
