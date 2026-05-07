using System;
using System.Data;
using System.Data.SqlClient;
using MyERP.Areas.Sync.ViewModels;

namespace MyERP.Areas.Sync.Data
{
    public class BranchIngestionRepository
    {
        public BranchApiResult SaveHeartbeat(BranchHeartbeatRequest heartbeat)
        {
            ValidateHeartbeat(heartbeat);
            using (var connection = SyncDb.Open())
            using (var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                EnsureIngestionTables(connection, transaction);
                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = @"
MERGE dbo.Sync_BranchHeartbeat AS target
USING (SELECT @BranchId AS BranchId) AS source
ON target.BranchId = source.BranchId
WHEN MATCHED THEN
    UPDATE SET MachineName = @MachineName,
               LastSeenAt = GETDATE(),
               AgentVersion = @AgentVersion,
               ConfigVersion = @ConfigVersion,
               PayloadSchemaVersion = @PayloadSchemaVersion,
               PendingOutboxCount = @PendingOutboxCount,
               FailedOutboxCount = @FailedOutboxCount,
               LastTransactionId = @LastTransactionId,
               UpdatedAt = GETDATE(),
               LastError = NULL
WHEN NOT MATCHED THEN
    INSERT (BranchId, MachineName, LastSeenAt, AgentVersion, ConfigVersion, PayloadSchemaVersion, PendingOutboxCount, FailedOutboxCount, LastTransactionId, UpdatedAt)
    VALUES (@BranchId, @MachineName, GETDATE(), @AgentVersion, @ConfigVersion, @PayloadSchemaVersion, @PendingOutboxCount, @FailedOutboxCount, @LastTransactionId, GETDATE());";
                    command.Parameters.Add("@BranchId", SqlDbType.Int).Value = heartbeat.BranchId;
                    command.Parameters.Add("@MachineName", SqlDbType.NVarChar, 256).Value = (object)heartbeat.MachineName ?? DBNull.Value;
                    command.Parameters.Add("@AgentVersion", SqlDbType.NVarChar, 50).Value = (object)heartbeat.AgentVersion ?? DBNull.Value;
                    command.Parameters.Add("@ConfigVersion", SqlDbType.NVarChar, 50).Value = (object)heartbeat.ConfigVersion ?? DBNull.Value;
                    command.Parameters.Add("@PayloadSchemaVersion", SqlDbType.NVarChar, 50).Value = (object)heartbeat.PayloadSchemaVersion ?? DBNull.Value;
                    command.Parameters.Add("@PendingOutboxCount", SqlDbType.Int).Value = heartbeat.PendingOutboxCount;
                    command.Parameters.Add("@FailedOutboxCount", SqlDbType.Int).Value = heartbeat.FailedOutboxCount;
                    command.Parameters.Add("@LastTransactionId", SqlDbType.BigInt).Value = heartbeat.LastTransactionId;
                    command.ExecuteNonQuery();
                }

                InsertLog(connection, transaction, heartbeat.BranchId, "Heartbeat", null, "Heartbeat", "Branch heartbeat accepted.");
                transaction.Commit();
            }

            return new BranchApiResult { Accepted = true, Status = "Accepted", Message = "Heartbeat accepted." };
        }

        public BranchApiResult SaveOutbox(BranchOutboxEnvelope envelope, string rawJson, string remoteIp)
        {
            ValidateEnvelope(envelope, rawJson);
            var payloadHash = HexToBytes(envelope.PayloadHash);

            using (var connection = SyncDb.Open())
            using (var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                EnsureIngestionTables(connection, transaction);
                var existing = FindExisting(connection, transaction, envelope.BranchId, envelope.EntityType, envelope.SyncKey);
                if (existing != null)
                {
                    if (String.Equals(existing.PayloadHashHex, envelope.PayloadHash, StringComparison.OrdinalIgnoreCase))
                    {
                        InsertUpload(connection, transaction, envelope, payloadHash, "DuplicateAccepted", "Duplicate SyncKey with same hash accepted idempotently.", remoteIp);
                        InsertLog(connection, transaction, envelope.BranchId, envelope.EntityType, envelope.SyncKey, "Duplicate", "Duplicate branch payload accepted idempotently.");
                        UpdateHeartbeatPayload(connection, transaction, envelope.BranchId, envelope.SyncKey, null);
                        transaction.Commit();
                        return new BranchApiResult { Accepted = true, Status = "DuplicateAccepted", SyncKey = envelope.SyncKey, Message = "Duplicate payload already exists." };
                    }

                    var conflict = "Duplicate SyncKey with different PayloadHash rejected.";
                    InsertUpload(connection, transaction, envelope, payloadHash, "Rejected", conflict, remoteIp);
                    InsertError(connection, transaction, envelope.BranchId, envelope.EntityType, envelope.SyncKey, conflict, null);
                    IncrementRejected(connection, transaction, envelope.BranchId, conflict);
                    transaction.Commit();
                    return new BranchApiResult { Accepted = false, Status = "Conflict", SyncKey = envelope.SyncKey, Message = conflict };
                }

                InsertOutbox(connection, transaction, envelope, payloadHash, rawJson);
                InsertUpload(connection, transaction, envelope, payloadHash, "Accepted", "Payload inserted into Sync_Outbox only.", remoteIp);
                InsertLog(connection, transaction, envelope.BranchId, envelope.EntityType, envelope.SyncKey, "Pending", "Branch payload inserted into Sync_Outbox. ApplyMode not executed.");
                UpdateHeartbeatPayload(connection, transaction, envelope.BranchId, envelope.SyncKey, null);
                transaction.Commit();
            }

            return new BranchApiResult { Accepted = true, Status = "Accepted", SyncKey = envelope.SyncKey, Message = "Payload queued for central review." };
        }

        public BranchApiResult Ack(string syncKey)
        {
            if (String.IsNullOrWhiteSpace(syncKey))
            {
                throw new InvalidOperationException("SyncKey is required.");
            }

            return new BranchApiResult { Accepted = true, Status = "AckNotRequired", SyncKey = syncKey, Message = "Central acceptance response is the acknowledgement." };
        }

        public void RecordAuthFailure(int branchId, string message, string remoteIp)
        {
            if (branchId <= 0)
            {
                return;
            }

            using (var connection = SyncDb.Open())
            using (var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                EnsureIngestionTables(connection, transaction);
                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = @"
MERGE dbo.Sync_BranchHeartbeat AS target
USING (SELECT @BranchId AS BranchId) AS source
ON target.BranchId = source.BranchId
WHEN MATCHED THEN
    UPDATE SET LastSeenAt = GETDATE(), AuthFailureCount = AuthFailureCount + 1, LastAuthFailureAt = GETDATE(), LastError = @Message, UpdatedAt = GETDATE()
WHEN NOT MATCHED THEN
    INSERT (BranchId, LastSeenAt, AuthFailureCount, LastAuthFailureAt, LastError, UpdatedAt)
    VALUES (@BranchId, GETDATE(), 1, GETDATE(), @Message, GETDATE());";
                    command.Parameters.Add("@BranchId", SqlDbType.Int).Value = branchId;
                    command.Parameters.Add("@Message", SqlDbType.NVarChar).Value = message;
                    command.ExecuteNonQuery();
                }

                InsertError(connection, transaction, branchId, "BranchAuth", null, message, "RemoteIp=" + (remoteIp ?? ""));
                transaction.Commit();
            }
        }

        private static void ValidateHeartbeat(BranchHeartbeatRequest heartbeat)
        {
            if (heartbeat == null || heartbeat.BranchId <= 0)
            {
                throw new InvalidOperationException("Valid BranchId is required.");
            }
        }

        private static void ValidateEnvelope(BranchOutboxEnvelope envelope, string rawJson)
        {
            if (envelope == null)
            {
                throw new InvalidOperationException("Payload body is required.");
            }

            if (envelope.BranchId <= 0 || envelope.Payload == null || envelope.Payload.BranchId != envelope.BranchId)
            {
                throw new InvalidOperationException("Payload BranchId is invalid.");
            }

            if (String.IsNullOrWhiteSpace(envelope.SyncKey) || !String.Equals(envelope.SyncKey, envelope.Payload.SyncKey, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Payload SyncKey is invalid.");
            }

            if (String.IsNullOrWhiteSpace(envelope.EntityType) || !String.Equals(envelope.EntityType, "Invoice", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Only invoice payloads are accepted by this endpoint.");
            }

            if (String.IsNullOrWhiteSpace(envelope.PayloadHash) || envelope.PayloadHash.Length != 64)
            {
                throw new InvalidOperationException("PayloadHash must be a SHA-256 hex string.");
            }

            if (String.IsNullOrWhiteSpace(rawJson))
            {
                throw new InvalidOperationException("Raw payload JSON is required.");
            }
        }

        private static void EnsureIngestionTables(SqlConnection connection, SqlTransaction transaction)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
IF OBJECT_ID('dbo.Sync_Outbox','U') IS NULL RAISERROR('Sync_Outbox table is missing. Apply approved SQL script 003 first.', 16, 1);
IF OBJECT_ID('dbo.Sync_Log','U') IS NULL RAISERROR('Sync_Log table is missing. Apply approved SQL script 003 first.', 16, 1);
IF OBJECT_ID('dbo.Sync_Error','U') IS NULL RAISERROR('Sync_Error table is missing. Apply approved SQL script 003 first.', 16, 1);
IF OBJECT_ID('dbo.Sync_BranchHeartbeat','U') IS NULL RAISERROR('Sync_BranchHeartbeat table is missing. Apply approved SQL script 003 first.', 16, 1);
IF OBJECT_ID('dbo.Sync_BranchUpload','U') IS NULL RAISERROR('Sync_BranchUpload table is missing. Apply approved SQL script 003 first.', 16, 1);";
                command.ExecuteNonQuery();
            }
        }

        private static ExistingOutbox FindExisting(SqlConnection connection, SqlTransaction transaction, int branchId, string entityType, string syncKey)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
SELECT TOP (1) CONVERT(NVARCHAR(64), PayloadHash, 2) AS PayloadHashHex
FROM dbo.Sync_Outbox WITH (UPDLOCK, HOLDLOCK)
WHERE BranchId = @BranchId AND EntityType = @EntityType AND EntityKey = @SyncKey;";
                command.Parameters.Add("@BranchId", SqlDbType.Int).Value = branchId;
                command.Parameters.Add("@EntityType", SqlDbType.NVarChar, 50).Value = entityType;
                command.Parameters.Add("@SyncKey", SqlDbType.NVarChar, 100).Value = syncKey;
                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() ? new ExistingOutbox { PayloadHashHex = Convert.ToString(reader["PayloadHashHex"]) } : null;
                }
            }
        }

        private static void InsertOutbox(SqlConnection connection, SqlTransaction transaction, BranchOutboxEnvelope envelope, byte[] payloadHash, string rawJson)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
INSERT dbo.Sync_Outbox
(BranchId, EntityType, EntityKey, OperationType, Direction, Status, TryCount, CreatedAt, PayloadJson, PayloadSummary, PayloadHash)
VALUES
(@BranchId, @EntityType, @SyncKey, N'Upload', N'BranchToCentral', N'Pending', 0, GETDATE(), @PayloadJson, @PayloadSummary, @PayloadHash);";
                command.Parameters.Add("@BranchId", SqlDbType.Int).Value = envelope.BranchId;
                command.Parameters.Add("@EntityType", SqlDbType.NVarChar, 50).Value = envelope.EntityType;
                command.Parameters.Add("@SyncKey", SqlDbType.NVarChar, 100).Value = envelope.SyncKey;
                command.Parameters.Add("@PayloadJson", SqlDbType.NVarChar).Value = rawJson;
                command.Parameters.Add("@PayloadSummary", SqlDbType.NVarChar).Value = "BranchUpload";
                command.Parameters.Add("@PayloadHash", SqlDbType.VarBinary, 32).Value = payloadHash;
                command.ExecuteNonQuery();
            }
        }

        private static void InsertUpload(SqlConnection connection, SqlTransaction transaction, BranchOutboxEnvelope envelope, byte[] payloadHash, string status, string message, string remoteIp)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
INSERT dbo.Sync_BranchUpload
(BranchId, SyncKey, PayloadHash, Status, Message, RemoteIp)
VALUES
(@BranchId, @SyncKey, @PayloadHash, @Status, @Message, @RemoteIp);";
                command.Parameters.Add("@BranchId", SqlDbType.Int).Value = envelope.BranchId;
                command.Parameters.Add("@SyncKey", SqlDbType.NVarChar, 100).Value = envelope.SyncKey;
                command.Parameters.Add("@PayloadHash", SqlDbType.VarBinary, 32).Value = payloadHash;
                command.Parameters.Add("@Status", SqlDbType.NVarChar, 50).Value = status;
                command.Parameters.Add("@Message", SqlDbType.NVarChar).Value = message;
                command.Parameters.Add("@RemoteIp", SqlDbType.NVarChar, 64).Value = (object)remoteIp ?? DBNull.Value;
                command.ExecuteNonQuery();
            }
        }

        private static void InsertLog(SqlConnection connection, SqlTransaction transaction, int branchId, string entityType, string syncKey, string status, string message)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "INSERT dbo.Sync_Log (BranchId, EntityType, EntityKey, Status, Message) VALUES (@BranchId, @EntityType, @SyncKey, @Status, @Message);";
                command.Parameters.Add("@BranchId", SqlDbType.Int).Value = branchId;
                command.Parameters.Add("@EntityType", SqlDbType.NVarChar, 50).Value = (object)entityType ?? DBNull.Value;
                command.Parameters.Add("@SyncKey", SqlDbType.NVarChar, 100).Value = (object)syncKey ?? DBNull.Value;
                command.Parameters.Add("@Status", SqlDbType.NVarChar, 50).Value = status;
                command.Parameters.Add("@Message", SqlDbType.NVarChar).Value = message;
                command.ExecuteNonQuery();
            }
        }

        private static void InsertError(SqlConnection connection, SqlTransaction transaction, int branchId, string entityType, string syncKey, string message, string detail)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "INSERT dbo.Sync_Error (BranchId, EntityType, EntityKey, ErrorMessage, LastSql) VALUES (@BranchId, @EntityType, @SyncKey, @Message, @Detail);";
                command.Parameters.Add("@BranchId", SqlDbType.Int).Value = branchId;
                command.Parameters.Add("@EntityType", SqlDbType.NVarChar, 50).Value = (object)entityType ?? DBNull.Value;
                command.Parameters.Add("@SyncKey", SqlDbType.NVarChar, 100).Value = (object)syncKey ?? DBNull.Value;
                command.Parameters.Add("@Message", SqlDbType.NVarChar).Value = message;
                command.Parameters.Add("@Detail", SqlDbType.NVarChar).Value = (object)detail ?? DBNull.Value;
                command.ExecuteNonQuery();
            }
        }

        private static void UpdateHeartbeatPayload(SqlConnection connection, SqlTransaction transaction, int branchId, string syncKey, string error)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
MERGE dbo.Sync_BranchHeartbeat AS target
USING (SELECT @BranchId AS BranchId) AS source
ON target.BranchId = source.BranchId
WHEN MATCHED THEN
    UPDATE SET LastSeenAt = GETDATE(), LastPayloadSyncKey = @SyncKey, LastError = @Error, UpdatedAt = GETDATE()
WHEN NOT MATCHED THEN
    INSERT (BranchId, LastSeenAt, LastPayloadSyncKey, LastError, UpdatedAt)
    VALUES (@BranchId, GETDATE(), @SyncKey, @Error, GETDATE());";
                command.Parameters.Add("@BranchId", SqlDbType.Int).Value = branchId;
                command.Parameters.Add("@SyncKey", SqlDbType.NVarChar, 100).Value = (object)syncKey ?? DBNull.Value;
                command.Parameters.Add("@Error", SqlDbType.NVarChar).Value = (object)error ?? DBNull.Value;
                command.ExecuteNonQuery();
            }
        }

        private static void IncrementRejected(SqlConnection connection, SqlTransaction transaction, int branchId, string error)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
MERGE dbo.Sync_BranchHeartbeat AS target
USING (SELECT @BranchId AS BranchId) AS source
ON target.BranchId = source.BranchId
WHEN MATCHED THEN
    UPDATE SET LastSeenAt = GETDATE(), RejectedPayloadCount = RejectedPayloadCount + 1, LastError = @Error, UpdatedAt = GETDATE()
WHEN NOT MATCHED THEN
    INSERT (BranchId, LastSeenAt, RejectedPayloadCount, LastError, UpdatedAt)
    VALUES (@BranchId, GETDATE(), 1, @Error, GETDATE());";
                command.Parameters.Add("@BranchId", SqlDbType.Int).Value = branchId;
                command.Parameters.Add("@Error", SqlDbType.NVarChar).Value = error;
                command.ExecuteNonQuery();
            }
        }

        private static byte[] HexToBytes(string hex)
        {
            var bytes = new byte[hex.Length / 2];
            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }

            return bytes;
        }

        private class ExistingOutbox
        {
            public string PayloadHashHex { get; set; }
        }
    }
}
