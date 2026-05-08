using System.Configuration;
using System.Data.SqlClient;
using MyERP.Areas.MainErp.Interfaces;

namespace MyERP.Areas.Pos.Repositories.Payments
{
    public class PaymentVoucherReadRepository : MyERP.Areas.MainErp.Repositories.Payments.PaymentVoucherReadRepository
    {
        public PaymentVoucherReadRepository()
            : base(new KishnyPosConnectionFactory())
        {
        }

        private class KishnyPosConnectionFactory : IMainErpDbConnectionFactory
        {
            public SqlConnection CreateOpenConnection()
            {
                var setting = ConfigurationManager.ConnectionStrings["KishnyCashConnection"];
                if (setting == null || string.IsNullOrWhiteSpace(setting.ConnectionString))
                {
                    throw new ConfigurationErrorsException("Missing connection string: KishnyCashConnection");
                }

                var connection = new SqlConnection(setting.ConnectionString);
                connection.Open();
                return connection;
            }
        }
    }
}
