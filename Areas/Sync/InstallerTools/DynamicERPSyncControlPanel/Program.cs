using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.ServiceProcess;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace DynamicERPSyncControlPanel
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new ControlPanelForm());
            }
            catch (Exception ex)
            {
                WriteStartupError(ex);
                MessageBox.Show(
                    "DynamicERP Sync Control Panel could not start.\r\n\r\n" + ex.Message + "\r\n\r\nSee ControlPanelStartupError.log beside the file.",
                    "DynamicERP Sync Control Panel",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static void WriteStartupError(Exception ex)
        {
            try
            {
                File.WriteAllText(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ControlPanelStartupError.log"),
                    DateTime.Now.ToString("s") + Environment.NewLine + ex,
                    Encoding.UTF8);
            }
            catch { }
        }
    }

    public class ControlPanelForm : Form
    {
        private const string ServiceName = "DynamicERPSyncBranchAgent";
        private readonly Label status = new Label();
        private readonly Label branch = new Label();
        private readonly Label send = new Label();
        private readonly Label dryRun = new Label();
        private readonly TextBox output = new TextBox();
        private string agentExe;
        private string logPath;

        public ControlPanelForm()
        {
            Text = "DynamicERP Sync Control Panel";
            Width = 780;
            Height = 560;
            Font = new Font("Segoe UI", 10);
            StartPosition = FormStartPosition.CenterScreen;
            agentExe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SyncBranchAgent.exe");
            ReadConfig();
            Controls.Add(new Label { Text = "DynamicERP Sync Control Panel", Font = new Font(Font, FontStyle.Bold), AutoSize = true, Left = 20, Top = 20 });
            status.SetBounds(20, 65, 700, 25); Controls.Add(status);
            branch.SetBounds(20, 95, 700, 25); Controls.Add(branch);
            send.SetBounds(20, 125, 700, 25); Controls.Add(send);
            dryRun.SetBounds(20, 155, 700, 25); Controls.Add(dryRun);
            AddButton("Start", 20, 195, (s, e) => Service("start"));
            AddButton("Stop", 120, 195, (s, e) => Service("stop"));
            AddButton("Restart", 220, 195, (s, e) => { Service("stop"); Service("start"); });
            AddButton("Open Logs", 340, 195, (s, e) => OpenLogs());
            AddButton("Run Health Check", 460, 195, (s, e) => RunAgent("--console --health"));
            AddButton("Send Test Heartbeat", 610, 195, (s, e) => RunAgent("--console --heartbeat-only"));
            output.Multiline = true;
            output.ScrollBars = ScrollBars.Vertical;
            output.SetBounds(20, 245, 720, 260);
            Controls.Add(output);
            RefreshStatus();
            RunAgent("--console --health");
        }

        private void RefreshStatus()
        {
            try
            {
                using (var service = new ServiceController(ServiceName))
                {
                    status.Text = "Service: " + service.Status;
                }
            }
            catch
            {
                status.Text = "Service: not installed";
            }
        }

        private void ReadConfig()
        {
            try
            {
                var config = agentExe + ".config";
                var xml = new XmlDocument();
                xml.Load(config);
                branch.Text = "BranchId: " + Get(xml, "BranchAgent.BranchId");
                send.Text = "SendEnabled: " + Get(xml, "BranchAgent.EnableSend");
                dryRun.Text = "DryRun: " + Get(xml, "BranchAgent.DryRunSend");
                logPath = Environment.ExpandEnvironmentVariables(Get(xml, "BranchAgent.LogPath"));
            }
            catch
            {
                branch.Text = "BranchId: unknown";
                send.Text = "SendEnabled: unknown";
                dryRun.Text = "DryRun: unknown";
            }
        }

        private void Service(string action)
        {
            try
            {
                var result = RunProcess("sc.exe", action + " \"" + ServiceName + "\"");
                Append(result);
            }
            catch (Exception ex) { Append("ERROR: " + ex.Message); }
            RefreshStatus();
        }

        private void RunAgent(string args)
        {
            try
            {
                var text = RunProcess(agentExe, args);
                Append(Humanize(text));
            }
            catch (Exception ex) { Append("ERROR: " + ex.Message); }
            RefreshStatus();
        }

        private static string Humanize(string text)
        {
            var builder = new StringBuilder();
            if (text.Contains("\"SendEnabled\":false")) builder.AppendLine("Send is disabled. This is the safe default.");
            if (text.Contains("\"DryRunSend\":true")) builder.AppendLine("Dry run is enabled.");
            if (text.Contains("\"CentralConnectivityOk\":true")) builder.AppendLine("Central API is reachable.");
            builder.AppendLine(text);
            return builder.ToString();
        }

        private void OpenLogs()
        {
            if (!String.IsNullOrWhiteSpace(logPath))
            {
                Directory.CreateDirectory(logPath);
                Process.Start("explorer.exe", logPath);
            }
        }

        private void AddButton(string text, int left, int top, EventHandler click)
        {
            var button = new Button { Text = text, Left = left, Top = top, Width = 120, Height = 34 };
            button.Click += click;
            Controls.Add(button);
        }

        private void Append(string text)
        {
            output.AppendText(DateTime.Now.ToString("s") + Environment.NewLine + text + Environment.NewLine);
        }

        private static string RunProcess(string file, string args)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo(file, args) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true }
            };
            process.Start();
            var text = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
            process.WaitForExit(30000);
            return text;
        }

        private static string Get(XmlDocument xml, string key)
        {
            var node = xml.SelectSingleNode("/configuration/appSettings/add[@key='" + key + "']");
            return node == null ? "" : node.Attributes["value"].Value;
        }
    }
}
