using MyERP.Areas.Pos.Models;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;

namespace MyERP.Areas.Pos.Data
{
    public class PosSystemHealthRepository
    {
        private readonly string _connectionString;

        public PosSystemHealthRepository()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["KishnyCashConnection"];
            if (connectionString == null || string.IsNullOrWhiteSpace(connectionString.ConnectionString))
            {
                throw new ConfigurationErrorsException("Missing connection string: KishnyCashConnection");
            }

            _connectionString = connectionString.ConnectionString;
        }

        public PosSystemHealthDatabaseDto GetDatabaseHealth()
        {
            var health = new PosSystemHealthDatabaseDto();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    LoadDatabaseHealth(connection, health);
                }
            }
            catch (SqlException ex)
            {
                health.StatusMessage = IsPermissionError(ex)
                    ? "لا توجد صلاحية كافية لقراءة مؤشرات الخادم. يتطلب هذا الجزء صلاحية VIEW SERVER STATE."
                    : "تعذر قراءة مؤشرات قاعدة البيانات: " + ex.Message;
            }
            catch (Exception ex)
            {
                health.StatusMessage = "تعذر قراءة مؤشرات قاعدة البيانات: " + ex.Message;
            }

            return health;
        }

        private static void LoadDatabaseHealth(SqlConnection connection, PosSystemHealthDatabaseDto health)
        {
            using (var command = new SqlCommand("dbo.usp_POS_SystemHealth_Database", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.CommandTimeout = 30;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        health.SlowQueries.Add(new PosSlowQueryDto
                        {
                            SessionId = ReadInt(reader, "session_id"),
                            ElapsedMs = ReadInt(reader, "total_elapsed_time"),
                            Command = ReadString(reader, "command"),
                            ProcedureName = ReadString(reader, "ProcedureName"),
                            WaitType = ReadString(reader, "WaitType")
                        });
                    }

                    if (reader.NextResult())
                    {
                        while (reader.Read())
                        {
                            health.BlockingSessions.Add(new PosBlockingSessionDto
                            {
                                SessionId = ReadInt(reader, "session_id"),
                                BlockingSessionId = ReadInt(reader, "blocking_session_id"),
                                WaitType = ReadString(reader, "WaitType"),
                                WaitMs = ReadInt(reader, "wait_time"),
                                ElapsedMs = ReadInt(reader, "total_elapsed_time")
                            });
                        }
                    }

                    if (reader.NextResult() && reader.Read())
                    {
                        health.DeadlockCounter = Convert.ToInt64(reader.GetValue(reader.GetOrdinal("DeadlockCounter")), CultureInfo.InvariantCulture);
                    }

                    if (reader.NextResult() && reader.Read())
                    {
                        health.TransactionsPerMinute = ReadInt(reader, "TransactionsPerMinute");
                    }

                    if (reader.NextResult() && reader.Read())
                    {
                        health.StatusMessage = ReadString(reader, "StatusMessage");
                    }
                }
            }
        }

        private static bool IsPermissionError(SqlException ex)
        {
            if (ex == null)
            {
                return false;
            }

            foreach (SqlError error in ex.Errors)
            {
                if (error.Number == 297 || error.Number == 229)
                {
                    return true;
                }
            }

            return ex.Message.IndexOf("permission", StringComparison.OrdinalIgnoreCase) >= 0
                || ex.Message.IndexOf("VIEW SERVER STATE", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static int ReadInt(IDataRecord reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        }

        private static string ReadString(IDataRecord reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? string.Empty : Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        }
    }
}
