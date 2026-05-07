using System.Data.SqlClient;

namespace MyERP.Areas.MainErp.Interfaces
{
    public interface IMainErpDbConnectionFactory
    {
        SqlConnection CreateOpenConnection();
    }
}
