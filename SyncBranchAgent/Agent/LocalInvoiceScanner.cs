using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using SyncBranchAgent.Models;

namespace SyncBranchAgent.Agent
{
    public class LocalInvoiceScanner
    {
        private readonly BranchAgentOptions options;
        private readonly JavaScriptSerializer serializer = new JavaScriptSerializer { MaxJsonLength = Int32.MaxValue };

        public LocalInvoiceScanner(BranchAgentOptions options)
        {
            this.options = options;
        }

        public IList<InvoicePayload> Scan(Watermark watermark)
        {
            var rows = new List<InvoicePayload>();
            using (var connection = new SqlConnection(options.LocalDbConnectionString))
            using (var command = connection.CreateCommand())
            {
                command.CommandText = String.IsNullOrWhiteSpace(options.InvoiceCandidateQuery)
                    ? DefaultCandidateQuery()
                    : options.InvoiceCandidateQuery;
                command.Parameters.Add("@BranchId", SqlDbType.Int).Value = options.BranchId;
                command.Parameters.Add("@LastTransactionId", SqlDbType.BigInt).Value = watermark.LastTransactionId;
                command.Parameters.Add("@BatchSize", SqlDbType.Int).Value = options.BatchSize;

                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(ToPayload(reader));
                    }
                }
            }

            return rows;
        }

        private InvoicePayload ToPayload(SqlDataReader reader)
        {
            var header = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                header[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }

            var transactionId = Convert.ToString(header.ContainsKey("Transaction_ID") ? header["Transaction_ID"] : header["TransactionId"]);
            var syncKey = options.BranchId + ":Invoice:" + transactionId;
            var canonicalJson = serializer.Serialize(header);

            return new InvoicePayload
            {
                SyncKey = syncKey,
                BranchId = options.BranchId,
                EntityType = "Invoice",
                SourceTransactionId = transactionId,
                OldTransactionId = transactionId,
                PayloadHash = Sha256(canonicalJson),
                CollectedAtUtc = DateTime.UtcNow,
                Header = header
            };
        }

        private static string Sha256(string value)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? ""));
                var builder = new StringBuilder(bytes.Length * 2);
                foreach (var b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }

                return builder.ToString();
            }
        }

        private static string DefaultCandidateQuery()
        {
            return @"
SELECT TOP (@BatchSize)
       Transaction_ID,
       branch_no,
       NoteSerial,
       NoteSerial1,
       NoteId,
       Transaction_Date,
       Total,
       Paid,
       Remain
FROM dbo.Transactions
WHERE Transaction_ID > @LastTransactionId
  AND (@BranchId = 0 OR branch_no = @BranchId)
ORDER BY Transaction_ID;";
        }
    }
}
