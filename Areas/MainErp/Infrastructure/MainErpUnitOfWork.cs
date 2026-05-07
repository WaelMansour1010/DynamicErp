using System;
using System.Data;
using System.Data.SqlClient;
using MyERP.Areas.MainErp.Interfaces;

namespace MyERP.Areas.MainErp.Infrastructure
{
    public sealed class MainErpUnitOfWork : IMainErpUnitOfWork
    {
        private readonly IMainErpDbConnectionFactory _connectionFactory;
        private bool _disposed;
        private bool _ownsConnection;

        public MainErpUnitOfWork(IMainErpDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
            CorrelationId = Guid.NewGuid();
        }

        public SqlConnection Connection { get; private set; }
        public SqlTransaction Transaction { get; private set; }
        public Guid CorrelationId { get; private set; }
        public bool IsTransactionActive { get { return Transaction != null; } }

        public void Begin()
        {
            ThrowIfDisposed();
            if (Transaction != null)
            {
                throw new InvalidOperationException("Nested MainErp transactions are not supported. Pass the active unit of work into child services.");
            }

            if (Connection == null)
            {
                Connection = _connectionFactory.CreateOpenConnection();
                _ownsConnection = true;
            }

            Transaction = Connection.BeginTransaction(IsolationLevel.ReadCommitted);
        }

        public void Commit()
        {
            ThrowIfDisposed();
            if (Transaction == null)
            {
                throw new InvalidOperationException("Cannot commit because no MainErp transaction is active.");
            }

            Transaction.Commit();
            Transaction.Dispose();
            Transaction = null;
        }

        public void Rollback()
        {
            if (_disposed || Transaction == null)
            {
                return;
            }

            try
            {
                Transaction.Rollback();
            }
            finally
            {
                Transaction.Dispose();
                Transaction = null;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Rollback();
            if (_ownsConnection && Connection != null)
            {
                Connection.Dispose();
            }

            Connection = null;
            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}
