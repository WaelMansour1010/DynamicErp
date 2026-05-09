using System.Web.Script.Serialization;

namespace MyERP.Areas.Reports.Services
{
    public class ReportDesignerStateService
    {
        public const int CurrentDesignVersion = 2;

        public string NormalizeLayoutJson(string layoutJson)
        {
            if (string.IsNullOrWhiteSpace(layoutJson))
            {
                return EmptyLayoutJson();
            }

            try
            {
                new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.DeserializeObject(layoutJson);
                return layoutJson;
            }
            catch
            {
                return EmptyLayoutJson();
            }
        }

        public string EmptyLayoutJson()
        {
            return "{\"designVersion\":2,\"visibleColumns\":{},\"columnOrder\":[],\"captions\":{},\"widths\":{},\"alignment\":{},\"sort\":[],\"filters\":[],\"groupBy\":[],\"summaries\":{},\"formatting\":{},\"conditionalFormatting\":[],\"pageSize\":50,\"quickFilter\":\"\"}";
        }
    }
}
