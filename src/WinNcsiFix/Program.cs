using Microsoft.Win32;
using Spectre.Console;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.Json;

namespace WinNcsiFix;

internal static class Program
{
    private const string Version = "3.0.0";
    private const string RegPath = @"SYSTEM\CurrentControlSet\Services\NlaSvc\Parameters\Internet";
    private static readonly string LogDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "WinNcsiFix", "logs");
    private static readonly string BackupDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "WinNcsiFix", "backups");

    private static readonly Dictionary<string, object> Defaults = new(StringComparer.OrdinalIgnoreCase)
    {
        ["EnableActiveProbing"] = 1,
        ["ActiveWebProbeHost"] = "www.msftconnecttest.com",
        ["ActiveWebProbePath"] = "connecttest.txt",
        ["ActiveWebProbeContent"] = "Microsoft Connect Test",
        ["ActiveDnsProbeHost"] = "dns.msftncsi.com",
        ["ActiveDnsProbeContent"] = "131.107.255.255"
    };

    public static int Main(string[] args)
    {
        System.Console.OutputEncoding = Encoding.UTF8;
        Directory.CreateDirectory(LogDir);
        Directory.CreateDirectory(BackupDir);

        try
        {
            if (args.Length == 0)
            {
                ShowInteractiveMenu();
                return 0;
            }

            var command = args[0].Trim().ToLowerInvariant();
            return command switch
            {
                "status" => RunStatus(args),
                "disable" => RunDisable(args),
                "enable" => RunEnable(args),
                "backup" => RunBackup(args),
                "restore" => RunRestore(args),
                "diagnose" or "diag" => RunDiagnose(args),
                "custom-probe" or "custom" => RunCustomProbe(args),
                "reset-defaults" or "defaults" => RunResetDefaults(args),
                "restart-nla" or "restart" => RunRestartNla(args),
                "about" => RunAbout(),
                "open" => RunOpen(args),
                "help" or "--help" or "-h" => RunHelp(),
                "version" or "--version" or "-v" => RunVersion(),
                _ => UnknownCommand(command)
            };
        }
        catch (Exception ex)
        {
            RenderHeader();
            Error("Unexpected error", ex.Message);
            Log("ERROR", ex.ToString());
            return 1;
        }
    }

    private static void ShowInteractiveMenu()
    {
        while (true)
        {
            AnsiConsole.Clear();
            RenderHeader();
            var config = ReadConfig();
            RenderDashboard(config);

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold deeppink2]Choose an action[/]")
                    .PageSize(12)
                    .HighlightStyle(new Style(foreground: Color.Black, background: Color.Aquamarine1, decoration: Decoration.Bold))
                    .AddChoices(new[]
                    {
                        "1  Disable Microsoft NCSI active probe",
                        "2  Enable Microsoft NCSI active probe",
                        "3  Diagnose connectivity detection",
                        "4  Backup current registry config",
                        "5  Restore latest backup",
                        "6  Configure custom probe endpoint",
                        "7  Reset Windows defaults",
                        "8  Restart NLA service",
                        "9  Show About / Developer info",
                        "0  Exit"
                    }));

            switch (choice[0])
            {
                case '1': RunDisable(Array.Empty<string>()); Pause(); break;
                case '2': RunEnable(Array.Empty<string>()); Pause(); break;
                case '3': RunDiagnose(Array.Empty<string>()); Pause(); break;
                case '4': RunBackup(Array.Empty<string>()); Pause(); break;
                case '5': RestoreLatestBackup(); Pause(); break;
                case '6': InteractiveCustomProbe(); Pause(); break;
                case '7': RunResetDefaults(Array.Empty<string>()); Pause(); break;
                case '8': RunRestartNla(Array.Empty<string>()); Pause(); break;
                case '9': RunAbout(); Pause(); break;
                default: return;
            }
        }
    }

    private static int RunStatus(string[] args)
    {
        var json = HasFlag(args, "--json");
        var config = ReadConfig();
        if (json)
        {
            var payload = new
            {
                version = Version,
                isAdmin = IsAdministrator(),
                registryPath = @"HKLM\" + RegPath,
                config,
                nlaService = GetServiceState("nlasvc"),
                timestampUtc = DateTimeOffset.UtcNow
            };
            System.Console.WriteLine(JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        RenderHeader();
        RenderStatusTable(config);
        return 0;
    }

    private static int RunDisable(string[] args)
    {
        EnsureWindows();
        if (!IsAdministrator()) return NeedAdmin();
        if (!ShouldProceed(args, "Disable Windows NCSI Active Probing?")) return 2;

        string backup = string.Empty;
        AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .SpinnerStyle(Style.Parse("deeppink2"))
            .Start("[bold cyan1]Applying registry changes...[/]", _ =>
            {
                backup = CreateBackup();
                SetDword("EnableActiveProbing", 0);
            });

        Success("NCSI Active Probing disabled", "Windows will stop checking Microsoft probe endpoints.");
        Note("Backup created", backup);
        Log("DISABLE", "EnableActiveProbing set to 0. Backup: " + backup);
        return 0;
    }

    private static int RunEnable(string[] args)
    {
        EnsureWindows();
        if (!IsAdministrator()) return NeedAdmin();
        if (!ShouldProceed(args, "Enable Windows NCSI Active Probing?")) return 2;

        string backup = string.Empty;
        AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .SpinnerStyle(Style.Parse("aquamarine1"))
            .Start("[bold cyan1]Applying registry changes...[/]", _ =>
            {
                backup = CreateBackup();
                SetDword("EnableActiveProbing", 1);
            });

        Success("NCSI Active Probing enabled", "Microsoft connectivity probing is active again.");
        Note("Backup created", backup);
        Log("ENABLE", "EnableActiveProbing set to 1. Backup: " + backup);
        return 0;
    }

    private static int RunBackup(string[] args)
    {
        EnsureWindows();
        if (!IsAdministrator()) return NeedAdmin();

        string backup = string.Empty;
        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("yellow"))
            .Start("[bold cyan1]Creating registry backup...[/]", _ => backup = CreateBackup());

        Success("Backup created", backup);
        return 0;
    }

    private static int RunRestore(string[] args)
    {
        EnsureWindows();
        if (!IsAdministrator()) return NeedAdmin();
        if (args.Length < 2)
        {
            return RestoreLatestBackup();
        }
        return RestoreBackup(args[1]);
    }

    private static int RestoreLatestBackup()
    {
        var latest = Directory.GetFiles(BackupDir, "*.reg").OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault();
        if (latest is null)
        {
            Warning("No backup files found", BackupDir);
            return 1;
        }
        return RestoreBackup(latest);
    }

    private static int RestoreBackup(string file)
    {
        if (!File.Exists(file))
        {
            Error("Backup file not found", file);
            return 1;
        }

        ProcessResult result = new(1, string.Empty, string.Empty);
        AnsiConsole.Status()
            .Spinner(Spinner.Known.Line)
            .SpinnerStyle(Style.Parse("cyan1"))
            .Start("[bold cyan1]Restoring backup...[/]", _ => result = RunProcess("reg.exe", "import " + Quote(file)));

        if (result.ExitCode == 0)
        {
            Success("Backup restored successfully", file);
            Log("RESTORE", "Restored: " + file);
            return 0;
        }

        Error("Restore failed", (result.Error + result.Output).Trim());
        return result.ExitCode == 0 ? 1 : result.ExitCode;
    }

    private static int RunDiagnose(string[] args)
    {
        var json = HasFlag(args, "--json");
        var config = ReadConfig();
        List<DiagItem> results = new();

        AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .SpinnerStyle(Style.Parse("mediumpurple"))
            .Start("[bold cyan1]Running diagnostics...[/]", _ =>
            {
                results = BuildDiagnostics(config);
            });

        if (json)
        {
            System.Console.WriteLine(JsonSerializer.Serialize(new { version = Version, config, results }, new JsonSerializerOptions { WriteIndented = true }));
            return results.Any(r => !r.Pass) ? 1 : 0;
        }

        RenderHeader();
        RenderDiagnosticTable(results);
        Note("Tip", "If Microsoft probe hosts are blocked, use 'disable' or 'custom-probe'.");
        Log("DIAGNOSE", JsonSerializer.Serialize(results));
        return results.Any(r => !r.Pass) ? 1 : 0;
    }

    private static List<DiagItem> BuildDiagnostics(Dictionary<string, object?> config)
    {
        var results = new List<DiagItem>
        {
            Check("Running as Administrator", IsAdministrator(), IsAdministrator() ? "OK" : "Registry changes need elevation"),
            Check("Windows OS", RuntimeInformation.IsOSPlatform(OSPlatform.Windows), RuntimeInformation.OSDescription),
            Check("NLA service", GetServiceState("nlasvc").Equals("RUNNING", StringComparison.OrdinalIgnoreCase), GetServiceState("nlasvc")),
            Check("EnableActiveProbing", Convert.ToString(config.GetValueOrDefault("EnableActiveProbing")) == "1", "Current value: " + (config.GetValueOrDefault("EnableActiveProbing") ?? "<missing>")),
            Check("Network interface up", NetworkInterface.GetAllNetworkInterfaces().Any(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback), "At least one non-loopback adapter is up")
        };

        var dnsHost = Convert.ToString(config.GetValueOrDefault("ActiveDnsProbeHost")) ?? "dns.msftncsi.com";
        var webHost = Convert.ToString(config.GetValueOrDefault("ActiveWebProbeHost")) ?? "www.msftconnecttest.com";
        var webPath = Convert.ToString(config.GetValueOrDefault("ActiveWebProbePath")) ?? "connecttest.txt";

        results.Add(TestDns(dnsHost));
        results.Add(TestHttp(webHost, webPath));
        return results;
    }

    private static int RunCustomProbe(string[] args)
    {
        EnsureWindows();
        if (!IsAdministrator()) return NeedAdmin();

        var host = GetOption(args, "--host");
        var path = GetOption(args, "--path") ?? "connecttest.txt";
        var content = GetOption(args, "--content") ?? "OK";
        var dnsHost = GetOption(args, "--dns-host") ?? host;
        var dnsContent = GetOption(args, "--dns-content") ?? "127.0.0.1";

        if (string.IsNullOrWhiteSpace(host))
        {
            Error("Missing parameter", "Example: WinNcsiFix.exe custom-probe --host example.com --path connecttest.txt --content OK");
            return 1;
        }

        string backup = string.Empty;
        AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .SpinnerStyle(Style.Parse("hotpink"))
            .Start("[bold cyan1]Configuring custom probe...[/]", _ =>
            {
                backup = CreateBackup();
                SetDword("EnableActiveProbing", 1);
                SetString("ActiveWebProbeHost", CleanHost(host));
                SetString("ActiveWebProbePath", path.TrimStart('/'));
                SetString("ActiveWebProbeContent", content);
                if (!string.IsNullOrWhiteSpace(dnsHost)) SetString("ActiveDnsProbeHost", CleanHost(dnsHost));
                SetString("ActiveDnsProbeContent", dnsContent);
            });

        Success("Custom NCSI probe configured", $"http://{CleanHost(host)}/{path.TrimStart('/')}");
        Note("Expected content", content);
        Note("Backup created", backup);
        Log("CUSTOM", "Custom probe configured. Backup: " + backup);
        return 0;
    }

    private static int RunResetDefaults(string[] args)
    {
        EnsureWindows();
        if (!IsAdministrator()) return NeedAdmin();
        if (!ShouldProceed(args, "Reset NCSI registry values to Windows defaults?")) return 2;

        string backup = string.Empty;
        AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .SpinnerStyle(Style.Parse("yellow"))
            .Start("[bold cyan1]Restoring Windows defaults...[/]", _ =>
            {
                backup = CreateBackup();
                foreach (var item in Defaults)
                {
                    if (item.Value is int i) SetDword(item.Key, i);
                    else SetString(item.Key, Convert.ToString(item.Value) ?? string.Empty);
                }
            });

        Success("Windows default NCSI values restored", "Registry values were reset to the Microsoft defaults.");
        Note("Backup created", backup);
        Log("RESET_DEFAULTS", "Defaults restored. Backup: " + backup);
        return 0;
    }

    private static int RunRestartNla(string[] args)
    {
        EnsureWindows();
        if (!IsAdministrator()) return NeedAdmin();
        Warning("Service refresh notice", "Restarting NLA may briefly refresh network status.");

        ProcessResult stop = new(0, string.Empty, string.Empty);
        ProcessResult start = new(1, string.Empty, string.Empty);
        AnsiConsole.Status()
            .Spinner(Spinner.Known.Bounce)
            .SpinnerStyle(Style.Parse("aquamarine1"))
            .Start("[bold cyan1]Restarting NLA service...[/]", _ =>
            {
                stop = RunProcess("sc.exe", "stop nlasvc");
                Thread.Sleep(1500);
                start = RunProcess("sc.exe", "start nlasvc");
            });

        if (start.ExitCode == 0)
        {
            Success("NLA service restart requested", "Windows accepted the restart command.");
            Log("RESTART_NLA", "NLA restart requested.");
            return 0;
        }

        Warning("Windows blocked the full restart", "A reboot can apply the changes if the service is busy.");
        if (!string.IsNullOrWhiteSpace(start.Error + start.Output))
        {
            Note("Service output", (start.Error + start.Output).Trim());
        }
        return 1;
    }

    private static int RunAbout()
    {
        RenderHeader();
        var about = new Grid().AddColumn().AddColumn();
        about.AddRow("[bold aqua]Developer[/]", "[white]Mohammad Mehdi Azizi[/]");
        about.AddRow("[bold aqua]Version[/]", $"[white]{Markup.Escape(Version)}[/]");
        about.AddRow("[bold aqua]X / Twitter[/]", "[link=https://x.com/the_azzi]https://x.com/the_azzi[/]");
        about.AddRow("[bold aqua]Telegram[/]", "[link=https://t.me/luluch_code]https://t.me/luluch_code[/]");
        about.AddRow("[bold aqua]GitHub[/]", "[link=https://github.com/TheGreatAzizi]https://github.com/TheGreatAzizi[/]");

        AnsiConsole.Write(new Panel(about)
            .Header("[bold deeppink2]About this project[/]")
            .Border(BoxBorder.Rounded)
            .BorderStyle(new Style(Color.MediumPurple))
            .Expand());

        Note("Description", "A stylish Windows utility for fixing and diagnosing NCSI / connectivity detection problems.");
        Note("Quick open", "Use 'WinNcsiFix.exe open github' or 'open x' or 'open telegram'.");
        return 0;
    }

    private static int RunOpen(string[] args)
    {
        if (args.Length < 2)
        {
            Error("Usage error", "WinNcsiFix.exe open github|x|telegram");
            return 1;
        }
        var url = args[1].ToLowerInvariant() switch
        {
            "github" => "https://github.com/TheGreatAzizi",
            "x" or "twitter" => "https://x.com/the_azzi",
            "telegram" or "tg" => "https://t.me/luluch_code",
            _ => args[1]
        };
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        Success("Opened link", url);
        return 0;
    }

    private static int RunHelp()
    {
        RenderHeader();
        var table = MakeTable("[bold deeppink2]Commands[/]", "[aqua]Command[/]", "[aqua]Description[/]");
        table.AddRow("status [grey]--json[/]", "Show current NCSI config");
        table.AddRow("disable [grey]--yes[/]", "Disable Microsoft active probe");
        table.AddRow("enable [grey]--yes[/]", "Enable Microsoft active probe");
        table.AddRow("diagnose [grey]--json[/]", "Run connectivity detection checks");
        table.AddRow("backup", "Export current registry values");
        table.AddRow("restore <backup.reg>", "Restore a backup, latest if omitted");
        table.AddRow("custom-probe --host HOST", "Use your own probe endpoint");
        table.AddRow("reset-defaults [grey]--yes[/]", "Restore Windows default NCSI values");
        table.AddRow("restart-nla", "Restart Network Location Awareness");
        table.AddRow("about", "Developer and project information");
        table.AddRow("open github|x|telegram", "Open developer links");
        AnsiConsole.Write(table);
        return 0;
    }

    private static int RunVersion()
    {
        System.Console.WriteLine(Version);
        return 0;
    }

    private static int UnknownCommand(string command)
    {
        Error("Unknown command", command);
        return RunHelp();
    }

    private static void InteractiveCustomProbe()
    {
        var host = AnsiConsole.Prompt(
            new TextPrompt<string>("[bold aqua]Host[/] [grey](example.com)[/]:")
                .Validate(value => string.IsNullOrWhiteSpace(value)
                    ? ValidationResult.Error("Host is required")
                    : ValidationResult.Success()));

        var path = AnsiConsole.Prompt(new TextPrompt<string>("[bold aqua]Path[/] [grey](connecttest.txt)[/]:").DefaultValue("connecttest.txt"));
        var content = AnsiConsole.Prompt(new TextPrompt<string>("[bold aqua]Expected content[/] [grey](OK)[/]:").DefaultValue("OK"));
        RunCustomProbe(new[] { "custom-probe", "--host", host, "--path", path, "--content", content });
    }

    private static Dictionary<string, object?> ReadConfig()
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        using var key = Registry.LocalMachine.OpenSubKey(RegPath, false);
        if (key is null) return result;
        foreach (var name in new[] { "EnableActiveProbing", "ActiveWebProbeHost", "ActiveWebProbePath", "ActiveWebProbeContent", "ActiveDnsProbeHost", "ActiveDnsProbeContent" })
        {
            result[name] = key.GetValue(name);
        }
        return result;
    }

    private static void SetDword(string name, int value)
    {
        using var key = Registry.LocalMachine.CreateSubKey(RegPath, true) ?? throw new InvalidOperationException("Could not open registry key.");
        key.SetValue(name, value, RegistryValueKind.DWord);
    }

    private static void SetString(string name, string value)
    {
        using var key = Registry.LocalMachine.CreateSubKey(RegPath, true) ?? throw new InvalidOperationException("Could not open registry key.");
        key.SetValue(name, value, RegistryValueKind.String);
    }

    private static string CreateBackup()
    {
        var file = Path.Combine(BackupDir, "ncsi-backup-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".reg");
        var result = RunProcess("reg.exe", "export " + Quote(@"HKLM\" + RegPath) + " " + Quote(file) + " /y");
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException("Backup failed: " + result.Error + result.Output);
        }

        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (!string.IsNullOrWhiteSpace(desktop) && Directory.Exists(desktop))
        {
            File.Copy(file, Path.Combine(desktop, Path.GetFileName(file)), true);
        }
        return file;
    }

    private static DiagItem TestDns(string host)
    {
        try
        {
            var addresses = Dns.GetHostAddresses(host);
            return Check("DNS resolve: " + host, addresses.Length > 0, string.Join(", ", addresses.Select(a => a.ToString())));
        }
        catch (Exception ex)
        {
            return Check("DNS resolve: " + host, false, ex.Message);
        }
    }

    private static DiagItem TestHttp(string host, string path)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var url = "http://" + CleanHost(host) + "/" + path.TrimStart('/');
            var response = client.GetAsync(url).GetAwaiter().GetResult();
            var text = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var detail = ((int)response.StatusCode) + " " + response.ReasonPhrase + ", " + Math.Min(text.Length, 80) + " chars";
            return Check("HTTP probe: " + url, response.IsSuccessStatusCode, detail);
        }
        catch (Exception ex)
        {
            return Check("HTTP probe", false, ex.Message);
        }
    }

    private static string GetServiceState(string serviceName)
    {
        var result = RunProcess("sc.exe", "query " + serviceName);
        var text = result.Output + result.Error;
        if (text.Contains("RUNNING", StringComparison.OrdinalIgnoreCase)) return "RUNNING";
        if (text.Contains("STOPPED", StringComparison.OrdinalIgnoreCase)) return "STOPPED";
        if (text.Contains("START_PENDING", StringComparison.OrdinalIgnoreCase)) return "START_PENDING";
        if (text.Contains("STOP_PENDING", StringComparison.OrdinalIgnoreCase)) return "STOP_PENDING";
        return string.IsNullOrWhiteSpace(text) ? "UNKNOWN" : text.Trim().Split('\n').FirstOrDefault()?.Trim() ?? "UNKNOWN";
    }

    private static ProcessResult RunProcess(string file, string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo(file, arguments)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var process = Process.Start(psi)!;
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit(15000);
            return new ProcessResult(process.ExitCode, output, error);
        }
        catch (Exception ex)
        {
            return new ProcessResult(1, string.Empty, ex.Message);
        }
    }

    private static void RenderHeader()
    {
        AnsiConsole.Write(new FigletText("NCSI FIX")
            .LeftJustified()
            .Color(Color.DeepSkyBlue1));

        var subtitle = new Panel(new Markup(
            "[bold deeppink2]Windows Connectivity Status Indicator fixer[/]\n" +
            "[grey]Fancy edition • diagnostics • backup • restore • custom probe[/]"))
            .Border(BoxBorder.Rounded)
            .BorderStyle(new Style(Color.MediumPurple))
            .Expand();
        AnsiConsole.Write(subtitle);

        var meta = new Markup($"[grey]v{Markup.Escape(Version)}[/]  [mediumpurple]•[/]  [white]by Mohammad Mehdi Azizi[/]  [mediumpurple]•[/]  [link=https://github.com/TheGreatAzizi]github.com/TheGreatAzizi[/]");
        AnsiConsole.Write(meta);
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();
    }

    private static void RenderDashboard(Dictionary<string, object?> config)
    {
        var probeEnabled = Convert.ToString(config.GetValueOrDefault("EnableActiveProbing")) == "1";
        var probe = probeEnabled ? "[black on aquamarine1] ENABLED [/]" : "[black on yellow] DISABLED [/]";
        var serviceState = GetServiceState("nlasvc");
        var service = serviceState.Equals("RUNNING", StringComparison.OrdinalIgnoreCase)
            ? "[black on aquamarine1] RUNNING [/]"
            : $"[black on orange1] {Markup.Escape(serviceState)} [/]";
        var admin = IsAdministrator()
            ? "[black on aquamarine1] YES [/]"
            : "[black on red1] NO [/]";

        var grid = new Grid();
        grid.AddColumn(new GridColumn().NoWrap());
        grid.AddColumn();
        grid.AddRow("[bold aqua]Active probing[/]", probe);
        grid.AddRow("[bold aqua]NLA service[/]", service);
        grid.AddRow("[bold aqua]Administrator[/]", admin);
        grid.AddRow("[bold aqua]Registry path[/]", $"[grey]HKLM\\{Markup.Escape(RegPath)}[/]");
        grid.AddRow("[bold aqua]Backup folder[/]", $"[grey]{Markup.Escape(BackupDir)}[/]");

        AnsiConsole.Write(new Panel(grid)
            .Header("[bold deeppink2]Live dashboard[/]")
            .Border(BoxBorder.Rounded)
            .BorderStyle(new Style(Color.DeepSkyBlue1))
            .Expand());
        AnsiConsole.WriteLine();
    }

    private static void RenderStatusTable(Dictionary<string, object?> config)
    {
        var table = MakeTable("[bold deeppink2]Current NCSI registry values[/]", "[aqua]Registry value[/]", "[aqua]Current data[/]");
        foreach (var key in Defaults.Keys)
        {
            table.AddRow(Markup.Escape(key), Markup.Escape(Convert.ToString(config.GetValueOrDefault(key)) ?? "<missing>"));
        }
        AnsiConsole.Write(table);
        Note("Administrator", IsAdministrator() ? "YES" : "NO");
        Note("NLA service", GetServiceState("nlasvc"));
    }

    private static void RenderDiagnosticTable(List<DiagItem> results)
    {
        var table = MakeTable("[bold deeppink2]Diagnostic report[/]", "[aqua]Check[/]", "[aqua]Result[/]", "[aqua]Detail[/]");
        foreach (var item in results)
        {
            table.AddRow(
                Markup.Escape(item.Name),
                item.Pass ? "[black on aquamarine1] PASS [/]" : "[black on yellow] WARN [/]",
                Markup.Escape(item.Detail));
        }
        AnsiConsole.Write(table);
    }

    private static Table MakeTable(string title, params string[] headers)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.MediumPurple)
            .Title(title)
            .Expand();

        foreach (var header in headers)
            table.AddColumn(header);

        return table;
    }

    private static bool IsAdministrator()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return false;
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static int NeedAdmin()
    {
        Error("Administrator permission is required", "Right-click and choose 'Run as administrator'.");
        return 740;
    }

    private static void EnsureWindows()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new PlatformNotSupportedException("This tool is designed for Windows only.");
    }

    private static bool ShouldProceed(string[] args, string message)
    {
        if (HasFlag(args, "--yes")) return true;
        if (args.Length == 0) return AnsiConsole.Confirm($"[bold yellow]{Markup.Escape(message)}[/]");
        return AnsiConsole.Confirm($"[bold yellow]{Markup.Escape(message)}[/]");
    }

    private static string? GetOption(string[] args, string name)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                return args[i + 1];
        }
        return null;
    }

    private static bool HasFlag(string[] args, string name) => args.Any(a => a.Equals(name, StringComparison.OrdinalIgnoreCase));
    private static string CleanHost(string host) => host.Replace("https://", string.Empty, StringComparison.OrdinalIgnoreCase).Replace("http://", string.Empty, StringComparison.OrdinalIgnoreCase).Trim('/');
    private static string Quote(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";
    private static DiagItem Check(string name, bool pass, string detail) => new(name, pass, detail);

    private static void Pause()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
        System.Console.ReadKey(true);
    }

    private static void Success(string title, string detail)
    {
        AnsiConsole.MarkupLine($"[black on aquamarine1] SUCCESS [/] [white]{Markup.Escape(title)}[/]");
        if (!string.IsNullOrWhiteSpace(detail))
            AnsiConsole.MarkupLine($"[grey]{Markup.Escape(detail)}[/]");
    }

    private static void Warning(string title, string detail)
    {
        AnsiConsole.MarkupLine($"[black on yellow] WARNING [/] [white]{Markup.Escape(title)}[/]");
        if (!string.IsNullOrWhiteSpace(detail))
            AnsiConsole.MarkupLine($"[grey]{Markup.Escape(detail)}[/]");
    }

    private static void Error(string title, string detail)
    {
        AnsiConsole.MarkupLine($"[black on red1] ERROR [/] [white]{Markup.Escape(title)}[/]");
        if (!string.IsNullOrWhiteSpace(detail))
            AnsiConsole.MarkupLine($"[grey]{Markup.Escape(detail)}[/]");
    }

    private static void Note(string title, string detail)
    {
        AnsiConsole.MarkupLine($"[mediumpurple]•[/] [bold aqua]{Markup.Escape(title)}:[/] [grey]{Markup.Escape(detail)}[/]");
    }

    private static void Log(string action, string message)
    {
        try
        {
            var line = DateTimeOffset.Now.ToString("O") + " [" + action + "] " + message + Environment.NewLine;
            File.AppendAllText(Path.Combine(LogDir, "win-ncsi-fix.log"), line, Encoding.UTF8);
        }
        catch
        {
        }
    }

    private sealed record ProcessResult(int ExitCode, string Output, string Error);
    private sealed record DiagItem(string Name, bool Pass, string Detail);
}
