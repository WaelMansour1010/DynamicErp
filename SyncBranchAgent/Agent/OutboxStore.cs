using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using SyncBranchAgent.Models;

namespace SyncBranchAgent.Agent
{
    public class OutboxStore
    {
        private readonly string rootPath;
        private readonly JavaScriptSerializer serializer = new JavaScriptSerializer { MaxJsonLength = Int32.MaxValue };

        public OutboxStore(string rootPath)
        {
            this.rootPath = rootPath;
            Directory.CreateDirectory(PendingPath);
            Directory.CreateDirectory(SentPath);
            Directory.CreateDirectory(FailedPath);
        }

        public int PendingCount
        {
            get
            {
                Directory.CreateDirectory(PendingPath);
                return Directory.GetFiles(PendingPath, "*.json").Length;
            }
        }

        public int FailedCount
        {
            get
            {
                Directory.CreateDirectory(FailedPath);
                return Directory.GetFiles(FailedPath, "*.json").Length;
            }
        }

        private string PendingPath { get { return Path.Combine(rootPath, "pending"); } }
        private string SentPath { get { return Path.Combine(rootPath, "sent"); } }
        private string FailedPath { get { return Path.Combine(rootPath, "failed"); } }

        public bool Enqueue(InvoicePayload payload)
        {
            var file = Path.Combine(PendingPath, SafeName(payload.SyncKey) + ".json");
            if (File.Exists(file) || File.Exists(Path.Combine(SentPath, Path.GetFileName(file))))
            {
                return false;
            }

            var envelope = new OutboxEnvelope
            {
                SyncKey = payload.SyncKey,
                BranchId = payload.BranchId,
                EntityType = payload.EntityType,
                PayloadHash = payload.PayloadHash,
                PayloadSchemaVersion = payload.PayloadSchemaVersion,
                ConfigVersion = payload.ConfigVersion,
                CreatedAtUtc = DateTime.UtcNow,
                Status = "Pending",
                TryCount = 0,
                Payload = payload
            };

            File.WriteAllText(file, serializer.Serialize(envelope));
            return true;
        }

        public IList<OutboxEnvelope> GetPending(int maxCount)
        {
            var now = DateTime.UtcNow;
            return Directory.GetFiles(PendingPath, "*.json")
                .OrderBy(File.GetCreationTimeUtc)
                .Select(ReadEnvelope)
                .Where(x => x != null)
                .Where(x => !x.NextAttemptAtUtc.HasValue || x.NextAttemptAtUtc.Value <= now)
                .Take(maxCount)
                .ToList();
        }

        public void MarkSent(OutboxEnvelope envelope)
        {
            envelope.Status = "Sent";
            envelope.SentAtUtc = DateTime.UtcNow;
            Move(envelope, SentPath);
        }

        public void MarkFailed(OutboxEnvelope envelope, string error, int maxRetryDelaySeconds, int maxRetryCount)
        {
            envelope.TryCount++;
            if (envelope.TryCount >= maxRetryCount)
            {
                Quarantine(envelope, error);
                return;
            }

            envelope.Status = "Pending";
            envelope.LastAttemptAtUtc = DateTime.UtcNow;
            envelope.NextAttemptAtUtc = DateTime.UtcNow.AddSeconds(CalculateDelaySeconds(envelope.TryCount, maxRetryDelaySeconds));
            envelope.LastError = error;
            var file = Path.Combine(PendingPath, SafeName(envelope.SyncKey) + ".json");
            File.WriteAllText(file, serializer.Serialize(envelope));
        }

        public void Quarantine(OutboxEnvelope envelope, string error)
        {
            envelope.TryCount++;
            envelope.Status = "Failed";
            envelope.LastAttemptAtUtc = DateTime.UtcNow;
            envelope.NextAttemptAtUtc = null;
            envelope.LastError = error;
            Move(envelope, FailedPath);
        }

        private static int CalculateDelaySeconds(int tryCount, int maxRetryDelaySeconds)
        {
            var cappedPower = Math.Min(tryCount, 10);
            var delay = (int)Math.Pow(2, cappedPower) * 30;
            return Math.Min(delay, maxRetryDelaySeconds);
        }

        private OutboxEnvelope ReadEnvelope(string file)
        {
            try
            {
                return serializer.Deserialize<OutboxEnvelope>(File.ReadAllText(file));
            }
            catch
            {
                return null;
            }
        }

        private void Move(OutboxEnvelope envelope, string destinationPath)
        {
            var source = Path.Combine(PendingPath, SafeName(envelope.SyncKey) + ".json");
            var destination = Path.Combine(destinationPath, SafeName(envelope.SyncKey) + ".json");
            File.WriteAllText(destination, serializer.Serialize(envelope));
            if (File.Exists(source))
            {
                File.Delete(source);
            }
        }

        private static string SafeName(string value)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return new string((value ?? "").Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        }
    }
}
