using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using System.ServiceProcess;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace DynamicERPSyncBranchAgentSetup
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
                if (!DotNet48Bootstrap.EnsureInstalled()) return;
                Application.Run(new BranchSetupForm());
            }
            catch (Exception ex)
            {
                WriteStartupError(ex);
                MessageBox.Show(
                    "DynamicERP Sync Branch Agent Setup could not start.\r\n\r\n" + ex.Message + "\r\n\r\nSee SetupStartupError.log beside the setup file.",
                    "DynamicERP Sync Branch Agent Setup",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static void WriteStartupError(Exception ex)
        {
            try
            {
                File.WriteAllText(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SetupStartupError.log"),
                    DateTime.Now.ToString("s") + Environment.NewLine + ex,
                    Encoding.UTF8);
            }
            catch { }
        }
    }

    internal static class DotNet48Bootstrap
    {
        private const int Net48Release = 528040;
        private const string InstallerFileName = "ndp48-x86-x64-allos-enu.exe";

        public static bool EnsureInstalled()
        {
            if (IsNet48Installed()) return true;

            while (!IsNet48Installed())
            {
                using (var form = new DotNet48RequiredForm(GetInstallerPath()))
                {
                    var result = form.ShowDialog();
                    if (result == DialogResult.Yes)
                    {
                        if (!RunInstaller()) return false;
                        if (IsNet48Installed()) return true;

                        MessageBox.Show(
                            "Microsoft .NET Framework 4.8 installation completed, but Windows still reports it is not available.\r\n\r\nRestart Windows if requested, then run setup again.",
                            "Microsoft .NET Framework 4.8 is required.",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return false;
                    }

                    if (result == DialogResult.No)
                    {
                        OpenPrerequisitesFolder();
                        continue;
                    }

                    return false;
                }
            }

            return true;
        }

        private static bool RunInstaller()
        {
            var installer = GetInstallerPath();
            if (!File.Exists(installer))
            {
                MessageBox.Show(
                    "The offline installer was not found:\r\n\r\n" + installer + "\r\n\r\nOpen the Prerequisites folder and run Download-DotNet48.ps1, or place the official Microsoft offline installer there.",
                    "Microsoft .NET Framework 4.8 is required.",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                OpenPrerequisitesFolder();
                return false;
            }

            try
            {
                var process = Process.Start(new ProcessStartInfo(installer, "/passive /norestart") { UseShellExecute = true });
                if (process == null) return false;
                process.WaitForExit();

                if (process.ExitCode == 3010 || process.ExitCode == 1641)
                {
                    MessageBox.Show(
                        "Microsoft .NET Framework 4.8 was installed and Windows restart is required.\r\n\r\nPlease restart, then run setup again.",
                        "Restart required",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return false;
                }

                if (process.ExitCode != 0 && !IsNet48Installed())
                {
                    MessageBox.Show(
                        "Microsoft .NET Framework 4.8 installer finished with exit code " + process.ExitCode + ".\r\n\r\nSetup cannot continue until .NET Framework 4.8 is installed.",
                        "Microsoft .NET Framework 4.8 is required.",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Microsoft .NET Framework 4.8 is required.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private static void OpenPrerequisitesFolder()
        {
            var folder = GetPrerequisitesFolder();
            Directory.CreateDirectory(folder);
            Process.Start("explorer.exe", folder);
        }

        private static string GetInstallerPath()
        {
            return Path.Combine(GetPrerequisitesFolder(), InstallerFileName);
        }

        private static string GetPrerequisitesFolder()
        {
            return Path.Combine(LocateReleaseRoot(), "Prerequisites");
        }

        private static string LocateReleaseRoot()
        {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (dir != null)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, "Server")) && Directory.Exists(Path.Combine(dir.FullName, "BranchAgent"))) return dir.FullName;
                dir = dir.Parent;
            }

            return AppDomain.CurrentDomain.BaseDirectory;
        }

        private static bool IsNet48Installed()
        {
            using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"))
            {
                var release = key == null ? null : key.GetValue("Release");
                return release != null && Convert.ToInt32(release) >= Net48Release;
            }
        }
    }

    internal sealed class DotNet48RequiredForm : Form
    {
        public DotNet48RequiredForm(string installerPath)
        {
            Text = "Microsoft .NET Framework 4.8 is required.";
            Width = 620;
            Height = 260;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            Controls.Add(new Label
            {
                Text = "Microsoft .NET Framework 4.8 is required.\r\n\r\nSetup cannot continue without it.\r\n\r\nExpected offline installer:\r\n" + installerPath,
                Left = 20,
                Top = 20,
                Width = 560,
                Height = 115
            });

            var install = new Button { Text = "Install Now", Left = 70, Top = 155, Width = 135, Height = 35, DialogResult = DialogResult.Yes };
            var open = new Button { Text = "Open Prerequisites Folder", Left = 225, Top = 155, Width = 190, Height = 35, DialogResult = DialogResult.No };
            var exit = new Button { Text = "Exit", Left = 435, Top = 155, Width = 100, Height = 35, DialogResult = DialogResult.Cancel };
            Controls.Add(install);
            Controls.Add(open);
            Controls.Add(exit);
            AcceptButton = install;
            CancelButton = exit;
        }
    }

    public class BranchSetupForm : Form
    {
        private const string ServiceName = "DynamicERPSyncBranchAgent";
        private const string DisplayName = "DynamicERP Sync Branch Agent";
        private readonly TextBox installFolder = new TextBox();
        private readonly TextBox branchId = new TextBox();
        private readonly TextBox centralUrl = new TextBox();
        private readonly TextBox localDb = new TextBox();
        private readonly TextBox tokenName = new TextBox();
        private readonly TextBox tokenValue = new TextBox();
        private readonly TextBox outbox = new TextBox();
        private readonly TextBox logs = new TextBox();
        private readonly NumericUpDown interval = new NumericUpDown();
        private readonly TextBox output = new TextBox();
        private string releaseRoot;

        public BranchSetupForm()
        {
            Text = "DynamicERP Sync Branch Agent Setup";
            Width = 930;
            Height = 760;
            Font = new Font("Segoe UI", 10);
            StartPosition = FormStartPosition.CenterScreen;
            releaseRoot = LocateReleaseRoot();

            var y = 20;
            Controls.Add(new Label { Text = "DynamicERP Sync Branch Agent Setup", Font = new Font(Font, FontStyle.Bold), AutoSize = true, Left = 20, Top = y });
            y += 45;
            AddRow("Install Folder", installFolder, ref y, @"C:\Program Files\DynamicERP Sync Branch Agent", true);
            AddRow("BranchId", branchId, ref y, "10", false);
            AddRow("Central URL", centralUrl, ref y, "https://central.example.com", false);
            AddRow("Local DB Connection", localDb, ref y, @"Data Source=.\SQLEXPRESS;Initial Catalog=BranchPosTest;Integrated Security=True;MultipleActiveResultSets=True;TrustServerCertificate=True", false);
            AddRow("Token Env Var", tokenName, ref y, "SATRIAH_BRANCH_SYNC_TOKEN_10", false);
            AddRow("Token (optional)", tokenValue, ref y, "", false);
            tokenValue.PasswordChar = '*';
            AddRow("Outbox Folder", outbox, ref y, @"%ProgramData%\DynamicERP\SyncBranchAgent\outbox", false);
            AddRow("Log Folder", logs, ref y, @"%ProgramData%\DynamicERP\SyncBranchAgent\logs", false);
            Controls.Add(new Label { Text = "Scan Interval", Left = 20, Top = y + 4, Width = 160 });
            interval.SetBounds(190, y, 120, 28);
            interval.Minimum = 10;
            interval.Maximum = 3600;
            interval.Value = 60;
            Controls.Add(interval);
            y += 45;

            AddButton("Install Service", 190, y, (s, e) => Install());
            AddButton("Health Check", 330, y, (s, e) => RunAgent("--console --health"));
            AddButton("Read-only Scan", 470, y, (s, e) => RunAgent("--console --once"));
            AddButton("Heartbeat Only", 610, y, (s, e) => RunAgent("--console --heartbeat-only"));
            AddButton("Send One Payload", 750, y, (s, e) => RunAgent("--console --send-one-payload"));
            y += 45;
            AddButton("Open Control Panel", 190, y, (s, e) => OpenControlPanel());
            AddButton("Open Logs", 360, y, (s, e) => OpenLogs());
            y += 50;

            output.Multiline = true;
            output.ScrollBars = ScrollBars.Vertical;
            output.SetBounds(20, y, 850, 250);
            Controls.Add(output);
        }

        private void Install()
        {
            try
            {
                var folder = installFolder.Text.Trim();
                Directory.CreateDirectory(folder);
                CopyAgentFiles(folder);
                WriteConfig(folder);
                ConfigureToken();
                InstallService(folder);
                Log("Installed with safe defaults: EnableSend=false, DryRunSend=true, RequireHttps=true.");
                RunAgent("--console --health");
            }
            catch (Exception ex)
            {
                Log("ERROR: " + ex.Message);
            }
        }

        private void CopyAgentFiles(string folder)
        {
            var source = Path.Combine(releaseRoot, "BranchAgent", "Binaries", "SyncBranchAgent.exe");
            File.Copy(source, Path.Combine(folder, "SyncBranchAgent.exe"), true);
            var controlPanel = Path.Combine(releaseRoot, "BranchAgent", "Installer", "DynamicERPSyncControlPanel.exe");
            if (File.Exists(controlPanel)) File.Copy(controlPanel, Path.Combine(folder, "DynamicERP Sync Control Panel.exe"), true);
            Log("Copied branch agent binaries.");
        }

        private void WriteConfig(string folder)
        {
            if (localDb.Text.IndexOf("Password=", StringComparison.OrdinalIgnoreCase) >= 0)
                throw new InvalidOperationException("Plaintext SQL passwords are blocked. Use Windows Integrated Security or encrypted deployment config.");
            var xml = new XmlDocument();
            xml.Load(Path.Combine(releaseRoot, "BranchAgent", "Config", "BranchSyncAgent.config.template"));
            xml.SelectSingleNode("/configuration/connectionStrings/add").Attributes["connectionString"].Value = localDb.Text.Trim();
            Set(xml, "BranchAgent.BranchId", branchId.Text.Trim());
            Set(xml, "BranchAgent.CentralApiBaseUrl", centralUrl.Text.Trim());
            Set(xml, "BranchAgent.ApiTokenEnvironmentVariable", tokenName.Text.Trim());
            Set(xml, "BranchAgent.OutboxPath", outbox.Text.Trim());
            Set(xml, "BranchAgent.LogPath", logs.Text.Trim());
            Set(xml, "BranchAgent.WatermarkPath", Path.Combine(Path.GetDirectoryName(Environment.ExpandEnvironmentVariables(outbox.Text.Trim())) ?? "", "watermark.json"));
            Set(xml, "BranchAgent.PollSeconds", ((int)interval.Value).ToString());
            Set(xml, "BranchAgent.EnableSend", "false");
            Set(xml, "BranchAgent.DryRunSend", "true");
            Set(xml, "BranchAgent.RequireHttps", "true");
            xml.Save(Path.Combine(folder, "SyncBranchAgent.exe.config"));
            Log("Generated safe branch config.");
        }

        private void ConfigureToken()
        {
            if (String.IsNullOrWhiteSpace(tokenValue.Text)) { Log("Token value not set by installer. Configure environment variable before controlled send."); return; }
            Environment.SetEnvironmentVariable(tokenName.Text.Trim(), tokenValue.Text, EnvironmentVariableTarget.Machine);
            Log("Token environment variable configured: " + tokenName.Text.Trim() + " (value hidden).");
        }

        private void InstallService(string folder)
        {
            var exe = Path.Combine(folder, "SyncBranchAgent.exe");
            RunProcess("sc.exe", "stop \"" + ServiceName + "\"", true);
            RunProcess("sc.exe", "delete \"" + ServiceName + "\"", true);
            RunProcess("sc.exe", "create \"" + ServiceName + "\" binPath= \"" + exe + "\" start= delayed-auto DisplayName= \"" + DisplayName + "\"", false);
            RunProcess("sc.exe", "failure \"" + ServiceName + "\" reset= 86400 actions= restart/60000/restart/60000/restart/60000", true);
            Log("Windows Service installed as: " + DisplayName);
        }

        private void RunAgent(string args)
        {
            try
            {
                var exe = Path.Combine(installFolder.Text.Trim(), "SyncBranchAgent.exe");
                if (!File.Exists(exe)) exe = Path.Combine(releaseRoot, "BranchAgent", "Binaries", "SyncBranchAgent.exe");
                var result = RunProcess(exe, args, false);
                Log(Humanize(result));
            }
            catch (Exception ex) { Log("ERROR: " + ex.Message); }
        }

        private static string Humanize(string text)
        {
            if (text.IndexOf("SendEnabled\":false", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Health: sending is disabled, dry run is enabled. This is the safe pilot default." + Environment.NewLine + text;
            if (text.IndexOf("CentralConnectivityOk\":true", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Health: central API is reachable." + Environment.NewLine + text;
            return text;
        }

        private void OpenControlPanel()
        {
            var path = Path.Combine(installFolder.Text.Trim(), "DynamicERP Sync Control Panel.exe");
            if (!File.Exists(path)) path = Path.Combine(releaseRoot, "BranchAgent", "Installer", "DynamicERPSyncControlPanel.exe");
            if (File.Exists(path)) Process.Start(path);
        }

        private void OpenLogs()
        {
            var path = Environment.ExpandEnvironmentVariables(logs.Text.Trim());
            Directory.CreateDirectory(path);
            Process.Start("explorer.exe", path);
        }

        private void AddRow(string label, TextBox box, ref int y, string value, bool browse)
        {
            Controls.Add(new Label { Text = label, Left = 20, Top = y + 4, Width = 160 });
            box.SetBounds(190, y, browse ? 560 : 680, 28);
            box.Text = value;
            Controls.Add(box);
            if (browse)
            {
                var button = new Button { Text = "Browse", Left = 760, Top = y, Width = 90 };
                button.Click += (s, e) => { using (var d = new FolderBrowserDialog()) if (d.ShowDialog(this) == DialogResult.OK) box.Text = d.SelectedPath; };
                Controls.Add(button);
            }
            y += 40;
        }

        private void AddButton(string text, int left, int top, EventHandler click)
        {
            var button = new Button { Text = text, Left = left, Top = top, Width = 130, Height = 34 };
            button.Click += click;
            Controls.Add(button);
        }

        private void Log(string text)
        {
            var line = DateTime.Now.ToString("s") + " " + text;
            output.AppendText(line + Environment.NewLine);
            try
            {
                Directory.CreateDirectory(Environment.ExpandEnvironmentVariables(logs.Text.Trim()));
                File.AppendAllText(Path.Combine(Environment.ExpandEnvironmentVariables(logs.Text.Trim()), "installer.log"), line + Environment.NewLine, Encoding.UTF8);
            }
            catch { }
        }

        private static string RunProcess(string file, string args, bool ignoreErrors)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo(file, args) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true }
            };
            process.Start();
            var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
            process.WaitForExit(30000);
            if (process.ExitCode != 0 && !ignoreErrors) throw new InvalidOperationException(output);
            return output;
        }

        private static void Set(XmlDocument xml, string key, string value)
        {
            var node = xml.SelectSingleNode("/configuration/appSettings/add[@key='" + key + "']");
            if (node != null) node.Attributes["value"].Value = value;
        }

        private static string LocateReleaseRoot()
        {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (dir != null)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, "Server")) && Directory.Exists(Path.Combine(dir.FullName, "BranchAgent"))) return dir.FullName;
                dir = dir.Parent;
            }
            throw new InvalidOperationException("Release root was not found.");
        }
    }
}
