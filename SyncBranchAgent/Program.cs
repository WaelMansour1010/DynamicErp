using System;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Web.Script.Serialization;
using SyncBranchAgent.Agent;

namespace SyncBranchAgent
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            var options = BranchAgentOptions.Load();
            var logger = new FileLogger(options.LogPath);

            if (Environment.UserInteractive || args.Any(x => String.Equals(x, "--console", StringComparison.OrdinalIgnoreCase)))
            {
                return RunConsole(args, options, logger);
            }

            ServiceBase.Run(new BranchSyncWindowsService(options, logger));
            return 0;
        }

        private static int RunConsole(string[] args, BranchAgentOptions options, FileLogger logger)
        {
            try
            {
                var runner = new AgentRunner(options, logger);
                if (args.Any(x => String.Equals(x, "--health", StringComparison.OrdinalIgnoreCase)))
                {
                    var health = runner.GetHealthAsync(CancellationToken.None).GetAwaiter().GetResult();
                    Console.WriteLine(new JavaScriptSerializer().Serialize(health));
                    return health.SendEnabled && !health.DryRunSend && !health.CentralConnectivityOk ? 2 : 0;
                }

                if (args.Any(x => String.Equals(x, "--heartbeat-only", StringComparison.OrdinalIgnoreCase)))
                {
                    runner.SendHeartbeatOnlyAsync(CancellationToken.None).GetAwaiter().GetResult();
                    return 0;
                }

                if (args.Any(x => String.Equals(x, "--send-one-payload", StringComparison.OrdinalIgnoreCase)))
                {
                    runner.SendOnePendingPayloadAsync(CancellationToken.None).GetAwaiter().GetResult();
                    return 0;
                }

                if (args.Any(x => String.Equals(x, "--once", StringComparison.OrdinalIgnoreCase)))
                {
                    runner.RunOnceAsync(CancellationToken.None).GetAwaiter().GetResult();
                    return 0;
                }

                logger.Info("Branch Sync Agent console mode started. Press Ctrl+C to stop.");
                using (var stop = new CancellationTokenSource())
                {
                    Console.CancelKeyPress += (sender, eventArgs) =>
                    {
                        eventArgs.Cancel = true;
                        stop.Cancel();
                    };

                    runner.RunLoopAsync(stop.Token).GetAwaiter().GetResult();
                }

                return 0;
            }
            catch (Exception ex)
            {
                logger.Error("Fatal Branch Sync Agent error.", ex);
                return 1;
            }
        }
    }
}
