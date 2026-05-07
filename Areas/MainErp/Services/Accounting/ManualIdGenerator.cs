using System;
using System.Data;
using System.Data.SqlClient;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.Interfaces;

namespace MyERP.Areas.MainErp.Services.Accounting
{
    public class ManualIdGenerator : IManualIdGenerator
    {
        private const string StrategyText = "sp_getapplock + UPDLOCK/HOLDLOCK max scan. Placeholder until legacy new_id behavior is fully replaced.";
        private readonly IMainErpDbConnectionFactory _connectionFactory;

        public ManualIdGenerator(IMainErpDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public ManualIdAllocation Allocate(ManualIdTarget target, IMainErpUnitOfWork unitOfWork)
        {
            if (target == null) throw new ArgumentNullException("target");
            if (unitOfWork == null) throw new ArgumentNullException("unitOfWork");
            if (!unitOfWork.IsTransactionActive)
            {
                throw new InvalidOperationException("Manual id allocation must run inside a MainErp transaction.");
            }

            AcquireApplicationLock(target, unitOfWork);
            var value = ReadNextValue(target, unitOfWork.Connection, unitOfWork.Transaction);
            return CreateAllocation(target, value, false, null);
        }

        public ManualIdAllocation Preview(ManualIdTarget target)
        {
            if (target == null) throw new ArgumentNullException("target");

            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                // Preview deliberately takes the same lock shape, then rolls back, so callers can see realistic contention behavior.
                var shim = new ManualIdUnitOfWorkShim(connection, transaction);
                AcquireApplicationLock(target, shim);
                var value = ReadNextValue(target, connection, transaction);
                transaction.Rollback();
                return CreateAllocation(target, value, true, "Preview only. No id was reserved.");
            }
        }

        private static void AcquireApplicationLock(ManualIdTarget target, IMainErpUnitOfWork unitOfWork)
        {
            using (var command = new SqlCommand("sp_getapplock", unitOfWork.Connection, unitOfWork.Transaction))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Resource", target.ApplicationLockName);
                command.Parameters.AddWithValue("@LockMode", "Exclusive");
                command.Parameters.AddWithValue("@LockOwner", "Transaction");
                command.Parameters.AddWithValue("@LockTimeout", 15000);
                var returnValue = command.Parameters.Add("@ReturnValue", SqlDbType.Int);
                returnValue.Direction = ParameterDirection.ReturnValue;

                command.ExecuteNonQuery();
                var result = Convert.ToInt32(returnValue.Value);
                if (result < 0)
                {
                    throw new TimeoutException("Could not acquire manual id allocation lock for " + target.TableName + "." + target.ColumnName);
                }
            }
        }

        private static long ReadNextValue(ManualIdTarget target, SqlConnection connection, SqlTransaction transaction)
        {
            var sql = string.Format(
                "SELECT ISNULL(MAX([{0}]), 0) + 1 FROM [{1}] WITH (UPDLOCK, HOLDLOCK)",
                target.ColumnName,
                target.TableName);

            using (var command = new SqlCommand(sql, connection, transaction))
            {
                var raw = command.ExecuteScalar();
                return Convert.ToInt64(raw);
            }
        }

        private static ManualIdAllocation CreateAllocation(ManualIdTarget target, long value, bool preview, string warning)
        {
            return new ManualIdAllocation
            {
                TableName = target.TableName,
                ColumnName = target.ColumnName,
                Value = value,
                IsPreview = preview,
                Strategy = StrategyText,
                Warning = warning
            };
        }

        private sealed class ManualIdUnitOfWorkShim : IMainErpUnitOfWork
        {
            public ManualIdUnitOfWorkShim(SqlConnection connection, SqlTransaction transaction)
            {
                Connection = connection;
                Transaction = transaction;
                CorrelationId = Guid.NewGuid();
            }

            public SqlConnection Connection { get; private set; }
            public SqlTransaction Transaction { get; private set; }
            public Guid CorrelationId { get; private set; }
            public bool IsTransactionActive { get { return true; } }
            public void Begin() { throw new NotSupportedException(); }
            public void Commit() { throw new NotSupportedException(); }
            public void Rollback() { throw new NotSupportedException(); }
            public void Dispose() { }
        }
    }
}
