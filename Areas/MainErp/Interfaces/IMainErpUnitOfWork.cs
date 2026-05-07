using System;
using System.Data.SqlClient;

namespace MyERP.Areas.MainErp.Interfaces
{
    public interface IMainErpUnitOfWork : IDisposable
    {
        SqlConnection Connection { get; }
        SqlTransaction Transaction { get; }
        Guid CorrelationId { get; }
        bool IsTransactionActive { get; }
        void Begin();
        void Commit();
        void Rollback();
    }
}
