using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using Microsoft.Win32;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace DynamicERPSyncServerSetup
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
                Application.Run(new ServerSetupForm());
            }
            catch (Exception ex)
            {
                WriteStartupError(ex);
                MessageBox.Show(
                    "DynamicERP Sync Server Setup could not start.\r\n\r\n" + ex.Message + "\r\n\r\nSee SetupStartupError.log beside the setup file.",
                    "DynamicERP Sync Server Setup",
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
                using (var form = new DotNet48RequiredForm(GetPrerequisitesFolder(), GetInstallerPath()))
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
        public DotNet48RequiredForm(string prerequisitesFolder, string installerPath)
        {
            Text = "Microsoft .NET Framework 4.8 is required.";
            Width = 620;
            Height = 260;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var message = new Label
            {
                Text = "Microsoft .NET Framework 4.8 is required.\r\n\r\nSetup cannot continue without it.\r\n\r\nExpected offline installer:\r\n" + installerPath,
                Left = 20,
                Top = 20,
                Width = 560,
                Height = 115
            };
            Controls.Add(message);

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

    public class ServerSetupForm : Form
    {
        private readonly TextBox webRoot = new TextBox();
        private readonly TextBox baseUrl = new TextBox();
        private readonly TextBox sqlServer = new TextBox();
        private readonly TextBox database = new TextBox();
        private readonly CheckBox runSql = new CheckBox();
        private readonly CheckBox verifyUrl = new CheckBox();
        private readonly Button install = new Button();
        private readonly TextBox log = new TextBox();
        private string releaseRoot;

        public ServerSetupForm()
        {
            Text = "DynamicERP Sync Server Setup";
            Width = 900;
            Height = 680;
            Font = new Font("Segoe UI", 10);
            StartPosition = FormStartPosition.CenterScreen;

            releaseRoot = LocateReleaseRoot();
            var title = new Label { Text = "DynamicERP Sync Server Setup", Font = new Font(Font, FontStyle.Bold), AutoSize = true, Left = 20, Top = 20 };
            Controls.Add(title);
            Controls.Add(MakeLabel("ERP Web Root", 20, 65));
            webRoot.SetBounds(180, 62, 560, 28);
            Controls.Add(webRoot);
            var browse = new Button { Text = "Browse", Left = 750, Top = 60, Width = 100 };
            browse.Click += (s, e) => BrowseWebRoot();
            Controls.Add(browse);

            Controls.Add(MakeLabel("Base URL", 20, 105));
            baseUrl.SetBounds(180, 102, 560, 28);
            baseUrl.Text = "http://localhost:8096";
            Controls.Add(baseUrl);
            verifyUrl.Text = "Verify URL after copy (requires ERP site running in IIS/IIS Express)";
            verifyUrl.Left = 180;
            verifyUrl.Top = 132;
            verifyUrl.Width = 560;
            Controls.Add(verifyUrl);

            Controls.Add(MakeLabel("Pilot SQL Server", 20, 145));
            sqlServer.SetBounds(180, 162, 260, 28);
            sqlServer.Text = ".\\SQLEXPRESS";
            Controls.Add(sqlServer);
            Controls.Add(MakeLabel("Database", 460, 165));
            database.SetBounds(560, 162, 180, 28);
            database.Text = "SyncAdminPilot";
            Controls.Add(database);

            runSql.Text = "I understand - run SQL on selected pilot database";
            runSql.Left = 180;
            runSql.Top = 202;
            runSql.Width = 500;
            Controls.Add(runSql);

            var scripts = new ListBox { Left = 180, Top = 235, Width = 560, Height = 105 };
            foreach (var script in SqlScripts()) scripts.Items.Add(script);
            Controls.Add(MakeLabel("SQL Order", 20, 240));
            Controls.Add(scripts);

            install.Text = "Install / Verify";
            install.Left = 180;
            install.Top = 355;
            install.Width = 180;
            install.Height = 36;
            install.Click += (s, e) => Install();
            Controls.Add(install);

            var prepareIis = new Button { Text = "Prepare IIS / SSL", Left = 380, Top = 355, Width = 180, Height = 36 };
            prepareIis.Click += (s, e) => PrepareIisSsl();
            Controls.Add(prepareIis);

            log.Multiline = true;
            log.ScrollBars = ScrollBars.Vertical;
            log.SetBounds(20, 410, 830, 210);
            Controls.Add(log);
        }

        private void Install()
        {
            install.Enabled = false;
            var lines = new List<string>();
            try
            {
                Add(lines, "Release root: " + releaseRoot);
                ValidateNoSqlPasswordInputs();
                var root = webRoot.Text.Trim();
                if (String.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) throw new InvalidOperationException("ERP Web Root is required.");

                Preflight(root, lines);
                CopyDirectory(Path.Combine(releaseRoot, "Server", "Files", "Areas", "Sync"), Path.Combine(root, "Areas", "Sync"), lines);
                CopyFile(Path.Combine(releaseRoot, "Server", "Files", "bin", "MyERP.dll"), Path.Combine(root, "bin", "MyERP.dll"), lines);
                PostCopyCheck(root, lines);

                if (runSql.Checked)
                {
                    Add(lines, "Running SQL scripts with Windows Integrated Security.");
                    RunSqlScripts(lines);
                }
                else
                {
                    Add(lines, "SQL execution skipped by user choice.");
                }

                if (verifyUrl.Checked)
                {
                    Verify(lines);
                }
                else
                {
                    Add(lines, "URL verification skipped. This is OK when IIS verification is not requested.");
                }
                WriteReport(root, lines);
                Add(lines, "Done. InstallReport.txt and InstallReport.html were written to the ERP Web Root.");
            }
            catch (Exception ex)
            {
                Add(lines, "ERROR: " + ex.Message);
                try { WriteReport(webRoot.Text.Trim(), lines); } catch { }
            }
            finally
            {
                install.Enabled = true;
            }
        }

        private void RunSqlScripts(ICollection<string> lines)
        {
            var server = sqlServer.Text.Trim();
            var db = database.Text.Trim();
            if (String.IsNullOrWhiteSpace(server) || String.IsNullOrWhiteSpace(db)) throw new InvalidOperationException("SQL Server and database are required.");
            var connectionString = "Data Source=" + server + ";Initial Catalog=" + db + ";Integrated Security=True;TrustServerCertificate=True";
            foreach (var name in SqlScripts())
            {
                var path = Path.Combine(releaseRoot, "Server", "Sql", name);
                Add(lines, "SQL: " + name);
                ExecuteSqlFile(connectionString, path);
            }
        }

        private void Preflight(string root, ICollection<string> lines)
        {
            Add(lines, ".NET Framework 4.8: " + (IsNet48Installed() ? "OK" : "MISSING"));
            if (!IsNet48Installed())
            {
                throw new InvalidOperationException(".NET Framework 4.8 is required.");
            }

            Add(lines, "Running as Administrator: " + (IsAdministrator() ? "OK" : "WARNING - not elevated"));
            Add(lines, "ERP Web.config: " + CheckFile(Path.Combine(root, "Web.config")));
            Add(lines, "ERP bin folder: " + CheckDirectory(Path.Combine(root, "bin")));
            Add(lines, "ERP Areas folder: " + CheckDirectory(Path.Combine(root, "Areas")));

            if (!File.Exists(Path.Combine(root, "Web.config")))
            {
                throw new InvalidOperationException("Selected ERP Web Root does not contain Web.config.");
            }

            Directory.CreateDirectory(Path.Combine(root, "bin"));
            Directory.CreateDirectory(Path.Combine(root, "Areas"));
        }

        private void PostCopyCheck(string root, ICollection<string> lines)
        {
            var syncArea = Path.Combine(root, "Areas", "Sync");
            Add(lines, "After copy /Areas/Sync: " + CheckDirectory(syncArea));
            if (!Directory.Exists(syncArea))
            {
                throw new InvalidOperationException("Copy failed: Areas\\Sync was not created.");
            }
        }

        private void Verify(ICollection<string> lines)
        {
            var url = baseUrl.Text.Trim().TrimEnd('/');
            if (String.IsNullOrWhiteSpace(url)) { Add(lines, "Verification skipped: Base URL is empty."); return; }
            foreach (var route in new[] { "/sync", "/sync/queue", "/sync/diagnostics", "/sync/logs", "/sync/pilot" })
            {
                Add(lines, "VERIFY " + route + ": " + Status(url + route, false));
            }

            Add(lines, "VERIFY /sync/apply/requestapply: " + Status(url + "/sync/apply/requestapply", true));
        }

        private static string Status(string url, bool post)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Timeout = 15000;
                request.Method = post ? "POST" : "GET";
                if (post)
                {
                    var body = Encoding.UTF8.GetBytes("SyncKey=test&MaxInvoicesPerRun=1");
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.ContentLength = body.Length;
                    using (var stream = request.GetRequestStream()) stream.Write(body, 0, body.Length);
                }

                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    return ((int)response.StatusCode) + " " + response.StatusCode;
                }
            }
            catch (WebException ex)
            {
                var response = ex.Response as HttpWebResponse;
                return response == null ? "FAILED: " + ex.Message : ((int)response.StatusCode) + " " + response.StatusCode;
            }
            catch (Exception ex)
            {
                return "FAILED: " + ex.Message;
            }
        }

        private static void ExecuteSqlFile(string connectionString, string path)
        {
            var sql = File.ReadAllText(path, Encoding.UTF8);
            var batches = Regex.Split(sql, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                foreach (var batch in batches.Where(x => !String.IsNullOrWhiteSpace(x)))
                using (var command = new SqlCommand(batch, connection))
                {
                    command.CommandTimeout = 120;
                    command.ExecuteNonQuery();
                }
            }
        }

        private void BrowseWebRoot()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog(this) == DialogResult.OK) webRoot.Text = dialog.SelectedPath;
            }
        }

        private void PrepareIisSsl()
        {
            try
            {
                var script = Path.Combine(releaseRoot, "Server", "Scripts", "Prepare-SyncServerIIS.ps1");
                if (!File.Exists(script))
                {
                    MessageBox.Show(this, "Prepare-SyncServerIIS.ps1 was not found in Server\\Scripts.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var root = String.IsNullOrWhiteSpace(webRoot.Text) ? @"C:\WWWSite\DynamicERP" : webRoot.Text.Trim();
                var host = "";
                try
                {
                    var uri = new Uri(baseUrl.Text.Trim());
                    host = uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ? "sync.example.com" : uri.Host;
                }
                catch
                {
                    host = "sync.example.com";
                }

                var command = "powershell -ExecutionPolicy Bypass -File \"" + script + "\" `\r\n" +
                              "  -UseExistingErpSite `\r\n" +
                              "  -SiteName \"Default Web Site\" `\r\n" +
                              "  -PhysicalPath \"" + root + "\" `\r\n" +
                              "  -HostName \"" + host + "\" `\r\n" +
                              "  -HttpPort 80 `\r\n" +
                              "  -HttpsPort 443";

                Clipboard.SetText(command);
                MessageBox.Show(this,
                    "IIS/SSL preparation command was copied to clipboard.\r\n\r\nRun it as Administrator when you want to prepare IIS.\r\n\r\n" + command,
                    "Prepare IIS / SSL",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Prepare IIS / SSL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static Label MakeLabel(string text, int left, int top) { return new Label { Text = text, Left = left, Top = top + 4, Width = 150 }; }
        private void Add(ICollection<string> lines, string message) { var line = DateTime.Now.ToString("s") + " " + message; lines.Add(line); log.AppendText(line + Environment.NewLine); }
        private static IEnumerable<string> SqlScripts() { return new[] { "001_CreateSyncSchema.sql", "002_Sync_AdminOperations.sql", "003_Sync_BranchIngestion.sql", "004_Sync_BranchAgentHardening.sql", "005_CheckSyncSchema.sql" }; }
        private static void ValidateNoSqlPasswordInputs() { }

        private static string LocateReleaseRoot()
        {
            var current = AppDomain.CurrentDomain.BaseDirectory;
            var dir = new DirectoryInfo(current);
            while (dir != null)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, "Server")) && Directory.Exists(Path.Combine(dir.FullName, "BranchAgent"))) return dir.FullName;
                dir = dir.Parent;
            }
            throw new InvalidOperationException("Release root was not found. Run setup from inside DataSyncPilotRelease folder.");
        }

        private static void CopyFile(string source, string destination, ICollection<string> lines)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            File.Copy(source, destination, true);
            lines.Add(DateTime.Now.ToString("s") + " COPY " + source + " -> " + destination);
        }

        private static void CopyDirectory(string source, string destination, ICollection<string> lines)
        {
            foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                var relative = file.Substring(source.Length).TrimStart('\\');
                CopyFile(file, Path.Combine(destination, relative), lines);
            }
        }

        private static void WriteReport(string webRoot, IEnumerable<string> lines)
        {
            if (String.IsNullOrWhiteSpace(webRoot) || !Directory.Exists(webRoot)) return;
            var text = String.Join(Environment.NewLine, lines);
            File.WriteAllText(Path.Combine(webRoot, "InstallReport.txt"), text, Encoding.UTF8);
            File.WriteAllText(Path.Combine(webRoot, "InstallReport.html"), "<pre>" + WebUtility.HtmlEncode(text) + "</pre>", Encoding.UTF8);
        }

        private static bool IsNet48Installed()
        {
            using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"))
            {
                var release = key == null ? null : key.GetValue("Release");
                return release != null && Convert.ToInt32(release) >= 528040;
            }
        }

        private static bool IsAdministrator()
        {
            try
            {
                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        private static string CheckFile(string path)
        {
            return File.Exists(path) ? "OK - " + path : "MISSING - " + path;
        }

        private static string CheckDirectory(string path)
        {
            return Directory.Exists(path) ? "OK - " + path : "MISSING - " + path;
        }
    }
}
