using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SyncBranchAgent.Models;

namespace SyncBranchAgent.Agent
{
    public class AgentRunner
    {
        private readonly BranchAgentOptions options;
        private readonly FileLogger logger;
        private readonly WatermarkStore watermarkStore;
        private readonly OutboxStore outboxStore;
        private readonly LocalInvoiceScanner scanner;
        private readonly CentralSyncApiClient apiClient;

        public AgentRunner(BranchAgentOptions options, FileLogger logger)
        {
            this.options = options;
            this.logger = logger;
            watermarkStore = new WatermarkStore(options.WatermarkPath);
            outboxStore = new OutboxStore(options.OutboxPath);
            scanner = new LocalInvoiceScanner(options);
            apiClient = new CentralSyncApiClient(options);
        }

        public async Task RunLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await RunOnceAsync(cancellationToken).ConfigureAwait(false);
                await Task.Delay(TimeSpan.FromSeconds(options.PollSeconds), cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task RunOnceAsync(CancellationToken cancellationToken)
        {
            try
            {
                var watermark = watermarkStore.Read();
                var invoices = scanner.Scan(watermark);
                var added = 0;

                foreach (var invoice in invoices)
                {
                    invoice.ConfigVersion = options.ConfigVersion;
                    invoice.PayloadSchemaVersion = options.PayloadSchemaVersion;
                    if (outboxStore.Enqueue(invoice))
                    {
                        added++;
                    }

                    long transactionId;
                    if (Int64.TryParse(invoice.SourceTransactionId, out transactionId))
                    {
                        watermark.LastTransactionId = Math.Max(watermark.LastTransactionId, transactionId);
                    }
                }

                watermark.LastScanUtc = DateTime.UtcNow;
                watermarkStore.Write(watermark);
                logger.Info("Scan completed. Candidates=" + invoices.Count + ", queued=" + added + ", pending=" + outboxStore.PendingCount + ".");

                await SendPendingAsync(cancellationToken).ConfigureAwait(false);
                await SendHeartbeatAsync(watermark, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.Error("Branch agent cycle failed; pending outbox is retained for retry.", ex);
            }
        }

        public async Task SendOnePendingPayloadAsync(CancellationToken cancellationToken)
        {
            var pending = outboxStore.GetPending(1);
            if (!pending.Any())
            {
                logger.Info("No pending payload is available for controlled send.");
                return;
            }

            await SendPendingAsync(cancellationToken, 1).ConfigureAwait(false);
        }

        public async Task SendHeartbeatOnlyAsync(CancellationToken cancellationToken)
        {
            var watermark = watermarkStore.Read();
            await SendHeartbeatAsync(watermark, cancellationToken).ConfigureAwait(false);
        }

        private async Task SendPendingAsync(CancellationToken cancellationToken)
        {
            await SendPendingAsync(cancellationToken, options.BatchSize).ConfigureAwait(false);
        }

        private async Task SendPendingAsync(CancellationToken cancellationToken, int maxCount)
        {
            var pending = outboxStore.GetPending(maxCount);
            if (!pending.Any())
            {
                return;
            }

            if (!options.EnableSend)
            {
                logger.Info("Send disabled. Pending payloads retained locally: " + pending.Count + ".");
                return;
            }

            if (options.DryRunSend)
            {
                logger.Info("Dry-run send enabled. Would send pending payloads: " + pending.Count + ". No central API call was made.");
                return;
            }

            foreach (var envelope in pending)
            {
                try
                {
                    var sent = await apiClient.SendOutboxAsync(envelope, cancellationToken).ConfigureAwait(false);
                    if (sent)
                    {
                        outboxStore.MarkSent(envelope);
                        var watermark = watermarkStore.Read();
                        watermark.LastSendUtc = DateTime.UtcNow;
                        watermarkStore.Write(watermark);
                        logger.Info("Outbox sent: " + envelope.SyncKey + ".");
                    }
                    else
                    {
                        outboxStore.MarkFailed(envelope, "Central API returned a non-success status.", options.MaxRetryDelaySeconds, options.MaxRetryCount);
                    }
                }
                catch (Exception ex)
                {
                    outboxStore.MarkFailed(envelope, ex.Message, options.MaxRetryDelaySeconds, options.MaxRetryCount);
                    logger.Warn("Outbox send failed and will retry: " + envelope.SyncKey + ".");
                }
            }
        }

        private async Task SendHeartbeatAsync(Watermark watermark, CancellationToken cancellationToken)
        {
            var heartbeat = new BranchHeartbeat
            {
                BranchId = options.BranchId,
                MachineName = Environment.MachineName,
                SentAtUtc = DateTime.UtcNow,
                AgentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(),
                ConfigVersion = options.ConfigVersion,
                PayloadSchemaVersion = options.PayloadSchemaVersion,
                PendingOutboxCount = outboxStore.PendingCount,
                FailedOutboxCount = outboxStore.FailedCount,
                LastTransactionId = watermark.LastTransactionId
            };

            if (!options.EnableSend)
            {
                logger.Info("Heartbeat prepared locally. Sending is disabled.");
                return;
            }

            if (options.DryRunSend)
            {
                logger.Info("Dry-run send enabled. Heartbeat prepared but not sent.");
                return;
            }

            try
            {
                if (await apiClient.SendHeartbeatAsync(heartbeat, cancellationToken).ConfigureAwait(false))
                {
                    watermark.LastHeartbeatUtc = DateTime.UtcNow;
                    watermarkStore.Write(watermark);
                    logger.Info("Heartbeat sent.");
                }
                else
                {
                    logger.Warn("Heartbeat was not accepted by central API.");
                }
            }
            catch (Exception ex)
            {
                logger.Error("Heartbeat failed; service will retry on next cycle.", ex);
            }
        }

        public async Task<HealthSnapshot> GetHealthAsync(CancellationToken cancellationToken)
        {
            var watermark = watermarkStore.Read();
            var snapshot = new HealthSnapshot
            {
                BranchId = options.BranchId,
                MachineName = Environment.MachineName,
                AgentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(),
                ConfigVersion = options.ConfigVersion,
                PayloadSchemaVersion = options.PayloadSchemaVersion,
                LastScanUtc = watermark.LastScanUtc,
                LastSendUtc = watermark.LastSendUtc,
                LastHeartbeatUtc = watermark.LastHeartbeatUtc,
                PendingLocalOutboxCount = outboxStore.PendingCount,
                FailedLocalOutboxCount = outboxStore.FailedCount,
                SendEnabled = options.EnableSend,
                DryRunSend = options.DryRunSend
            };

            if (!options.EnableSend)
            {
                snapshot.CentralConnectivityOk = false;
                snapshot.CentralConnectivityMessage = "Send disabled; central connectivity was not tested.";
                return snapshot;
            }

            if (options.DryRunSend)
            {
                snapshot.CentralConnectivityOk = false;
                snapshot.CentralConnectivityMessage = "DryRunSend enabled; central connectivity was not tested.";
                return snapshot;
            }

            try
            {
                snapshot.CentralConnectivityOk = await apiClient.TestConnectivityAsync(cancellationToken).ConfigureAwait(false);
                snapshot.CentralConnectivityMessage = snapshot.CentralConnectivityOk ? "Central API reachable." : "Central API rejected the ping.";
            }
            catch (Exception ex)
            {
                snapshot.CentralConnectivityOk = false;
                snapshot.CentralConnectivityMessage = ex.Message;
            }

            return snapshot;
        }
    }
}
