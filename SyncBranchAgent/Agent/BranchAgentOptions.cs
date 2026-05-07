using System;
using System.Configuration;

namespace SyncBranchAgent.Agent
{
    public class BranchAgentOptions
    {
        public int BranchId { get; set; }
        public string ServiceName { get; set; }
        public int PollSeconds { get; set; }
        public int BatchSize { get; set; }
        public string LocalDbConnectionString { get; set; }
        public string CentralApiBaseUrl { get; set; }
        public string ApiTokenEnvironmentVariable { get; set; }
        public string OutboxPath { get; set; }
        public string WatermarkPath { get; set; }
        public string LogPath { get; set; }
        public bool EnableSend { get; set; }
        public bool DryRunSend { get; set; }
        public bool RequireHttps { get; set; }
        public string InvoiceCandidateQuery { get; set; }
        public string ConfigVersion { get; set; }
        public string PayloadSchemaVersion { get; set; }
        public int MaxRetryCount { get; set; }
        public int MaxRetryDelaySeconds { get; set; }
        public string CentralPingPath { get; set; }

        public static BranchAgentOptions Load()
        {
            var localConnectionName = Get("BranchAgent.LocalDbConnectionName", "BranchLocalDb");
            var connection = ConfigurationManager.ConnectionStrings[localConnectionName];
            if (connection == null || String.IsNullOrWhiteSpace(connection.ConnectionString))
            {
                throw new ConfigurationErrorsException("Branch local POS connection string is missing.");
            }

            var options = new BranchAgentOptions
            {
                BranchId = GetInt("BranchAgent.BranchId", 0),
                ServiceName = Get("BranchAgent.ServiceName", "SatriahBranchSyncAgent"),
                PollSeconds = Math.Max(10, GetInt("BranchAgent.PollSeconds", 60)),
                BatchSize = Math.Max(1, Math.Min(100, GetInt("BranchAgent.BatchSize", 25))),
                LocalDbConnectionString = connection.ConnectionString,
                CentralApiBaseUrl = Get("BranchAgent.CentralApiBaseUrl", ""),
                ApiTokenEnvironmentVariable = Get("BranchAgent.ApiTokenEnvironmentVariable", "SATRIAH_BRANCH_SYNC_TOKEN"),
                OutboxPath = Expand(Get("BranchAgent.OutboxPath", @"%ProgramData%\Satriah\BranchSyncAgent\outbox")),
                WatermarkPath = Expand(Get("BranchAgent.WatermarkPath", @"%ProgramData%\Satriah\BranchSyncAgent\watermark.json")),
                LogPath = Expand(Get("BranchAgent.LogPath", @"%ProgramData%\Satriah\BranchSyncAgent\logs")),
                EnableSend = GetBool("BranchAgent.EnableSend", false),
                DryRunSend = GetBool("BranchAgent.DryRunSend", true),
                RequireHttps = GetBool("BranchAgent.RequireHttps", true),
                InvoiceCandidateQuery = Get("BranchAgent.InvoiceCandidateQuery", ""),
                ConfigVersion = Get("BranchAgent.ConfigVersion", "1.0"),
                PayloadSchemaVersion = Get("BranchAgent.PayloadSchemaVersion", "1.0"),
                MaxRetryCount = Math.Max(1, GetInt("BranchAgent.MaxRetryCount", 12)),
                MaxRetryDelaySeconds = Math.Max(30, GetInt("BranchAgent.MaxRetryDelaySeconds", 3600)),
                CentralPingPath = Get("BranchAgent.CentralPingPath", "sync/api/branch/ping")
            };

            Validate(options);
            return options;
        }

        private static void Validate(BranchAgentOptions options)
        {
            if (options.BranchId <= 0)
            {
                throw new ConfigurationErrorsException("BranchAgent.BranchId must be configured.");
            }

            if (options.EnableSend)
            {
                if (String.IsNullOrWhiteSpace(options.CentralApiBaseUrl))
                {
                    throw new ConfigurationErrorsException("Central API URL is required when sending is enabled.");
                }

                var uri = new Uri(options.CentralApiBaseUrl, UriKind.Absolute);
                if (options.RequireHttps && !String.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ConfigurationErrorsException("Central API URL must use HTTPS.");
                }
            }
        }

        private static string Get(string key, string defaultValue)
        {
            var value = ConfigurationManager.AppSettings[key];
            return String.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();
        }

        private static int GetInt(string key, int defaultValue)
        {
            int value;
            return Int32.TryParse(ConfigurationManager.AppSettings[key], out value) ? value : defaultValue;
        }

        private static bool GetBool(string key, bool defaultValue)
        {
            bool value;
            return Boolean.TryParse(ConfigurationManager.AppSettings[key], out value) ? value : defaultValue;
        }

        private static string Expand(string path)
        {
            return Environment.ExpandEnvironmentVariables(path);
        }
    }
}
