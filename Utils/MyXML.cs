using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;
using System.Reflection;
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
            var memberNames = GetSerializableMemberNames(typeof(T));
            using (var reader = memberNames.Length > 0
                ? ObjectReader.Create(collection, memberNames)
                : ObjectReader.Create(collection))
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

        private static string[] GetSerializableMemberNames(Type type)
        {
            return type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                .Where(p => IsSupportedColumnType(p.PropertyType))
                .Select(p => p.Name)
                .ToArray();
        }

        private static bool IsSupportedColumnType(Type type)
        {
            var underlying = Nullable.GetUnderlyingType(type) ?? type;

            if (underlying.IsEnum) return true;
            if (underlying.IsPrimitive) return true;
            if (underlying == typeof(string)) return true;
            if (underlying == typeof(decimal)) return true;
            if (underlying == typeof(DateTime)) return true;
            if (underlying == typeof(DateTimeOffset)) return true;
            if (underlying == typeof(Guid)) return true;
            if (underlying == typeof(TimeSpan)) return true;
            if (underlying == typeof(byte[])) return true;

            if (typeof(IEnumerable).IsAssignableFrom(underlying) && underlying != typeof(string))
                return false;

            return false;
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
