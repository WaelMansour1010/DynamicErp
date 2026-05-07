using System;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace SyncBranchAgent.Agent
{
    public class BranchSyncWindowsService : ServiceBase
    {
        private readonly BranchAgentOptions options;
        private readonly FileLogger logger;
        private CancellationTokenSource stop;
        private Task workerTask;

        public BranchSyncWindowsService(BranchAgentOptions options, FileLogger logger)
        {
            this.options = options;
            this.logger = logger;
            ServiceName = options.ServiceName;
            CanStop = true;
            CanPauseAndContinue = false;
            AutoLog = true;
        }

        protected override void OnStart(string[] args)
        {
            stop = new CancellationTokenSource();
            var runner = new AgentRunner(options, logger);
            workerTask = Task.Run(() => runner.RunLoopAsync(stop.Token));
            logger.Info("Branch Sync Agent service started.");
        }

        protected override void OnStop()
        {
            if (stop != null)
            {
                stop.Cancel();
            }

            try
            {
                if (workerTask != null)
                {
                    workerTask.Wait(TimeSpan.FromSeconds(20));
                }
            }
            catch (AggregateException ex)
            {
                logger.Error("Branch Sync Agent stopped with worker error.", ex);
            }

            logger.Info("Branch Sync Agent service stopped.");
        }
    }
}
