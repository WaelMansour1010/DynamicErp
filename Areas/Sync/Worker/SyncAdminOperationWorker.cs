using System;
using System.Data;
using System.Data.SqlClient;
using MyERP.Areas.Sync.Data;

namespace MyERP.Areas.Sync.Worker
{
    public class SyncAdminOperationWorker
    {
        public void PollOnce(string workerName)
        {
            using (var connection = SyncDb.Open())
            using (var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                var operationId = ReserveNextOperation(connection, transaction, workerName);
                if (operationId == 0)
                {
                    transaction.Commit();
                    return;
                }

                // Execution adapter is intentionally not wired here. Real apply must remain
                // behind the offline pilot gates and single SyncKey runner approval.
                MarkBlocked(connection, transaction, operationId, "Worker listener reserved the request; execution adapter is not enabled.");
                InsertAudit(connection, transaction, operationId, workerName, "Blocked", "Worker listener reserved the request; execution adapter is not enabled.");
                transaction.Commit();
            }
        }

        private static long ReserveNextOperation(SqlConnection connection, SqlTransaction transaction, string workerName)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
DECLARE @Next TABLE (AdminOperationId BIGINT);

;WITH next_operation AS
(
    SELECT TOP (1) AdminOperationId
    FROM dbo.Sync_AdminOperation WITH (UPDLOCK, READPAST, ROWLOCK)
    WHERE Status = N'PendingWorker'
      AND ApplySingleSyncKeyOnly = 1
      AND MaxInvoicesPerRun = 1
    ORDER BY CreatedAt
)
UPDATE o
SET Status = N'InProgress',
    StartedAt = GETDATE(),
    WorkerName = @WorkerName
OUTPUT inserted.AdminOperationId INTO @Next
FROM dbo.Sync_AdminOperation o
INNER JOIN next_operation n ON n.AdminOperationId = o.AdminOperationId;

SELECT ISNULL((SELECT TOP (1) AdminOperationId FROM @Next), 0);";
                command.Parameters.Add("@WorkerName", SqlDbType.NVarChar, 256).Value = workerName ?? Environment.MachineName;
                return Convert.ToInt64(command.ExecuteScalar());
            }
        }

        private static void MarkBlocked(SqlConnection connection, SqlTransaction transaction, long operationId, string reason)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
UPDATE dbo.Sync_AdminOperation
SET Status = N'Blocked',
    Result = N'Blocked',
    CompletedAt = GETDATE(),
    LastError = @Reason
WHERE AdminOperationId = @AdminOperationId;";
                command.Parameters.Add("@Reason", SqlDbType.NVarChar).Value = reason;
                command.Parameters.Add("@AdminOperationId", SqlDbType.BigInt).Value = operationId;
                command.ExecuteNonQuery();
            }
        }

        private static void InsertAudit(SqlConnection connection, SqlTransaction transaction, long operationId, string workerName, string result, string details)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
INSERT dbo.Sync_AdminAudit
(
    CreatedAt, UserName, MachineName, IpAddress, Operation, Permission,
    ProfileName, SyncKey, Result, Reason, Details
)
SELECT
    GETDATE(), @WorkerName, @WorkerName, NULL, N'WorkerPoll',
    Permission, ProfileName, SyncKey, @Result, LastError, @Details
FROM dbo.Sync_AdminOperation
WHERE AdminOperationId = @AdminOperationId;";
                command.Parameters.Add("@WorkerName", SqlDbType.NVarChar, 256).Value = workerName ?? Environment.MachineName;
                command.Parameters.Add("@Result", SqlDbType.NVarChar, 50).Value = result;
                command.Parameters.Add("@Details", SqlDbType.NVarChar).Value = details;
                command.Parameters.Add("@AdminOperationId", SqlDbType.BigInt).Value = operationId;
                command.ExecuteNonQuery();
            }
        }
    }
}
