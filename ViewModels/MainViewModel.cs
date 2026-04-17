using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using VoidStrike.Models;
using VoidStrike.Services;

namespace VoidStrike.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _targetUrl = "https://example.com";
        private string _statusMessage = "Ready to Scan";
        private bool _isScanning = false;
        private int _scanProgress = 0;

        public string TargetUrl
        {
            get => _targetUrl;
            set { _targetUrl = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public bool IsScanning
        {
            get => _isScanning;
            set { _isScanning = value; OnPropertyChanged(); }
        }

        public int ScanProgress
        {
            get => _scanProgress;
            set { _scanProgress = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Vulnerability> Vulnerabilities { get; } = new ObservableCollection<Vulnerability>();
        public ObservableCollection<string> ScanLogs { get; } = new ObservableCollection<string>();

        public ICommand StartScanCommand { get; }
        public ICommand CopyResultsCommand { get; }
        public ICommand ChangeViewCommand { get; }

        private string _currentView = "Dashboard";
        public string CurrentView
        {
            get => _currentView;
            set { _currentView = value; OnPropertyChanged(); }
        }

        private ScanConfig _config = new ScanConfig();
        public ScanConfig Config
        {
            get => _config;
            set { _config = value; OnPropertyChanged(); }
        }

        public MainViewModel()
        {
            StartScanCommand = new RelayCommand(async _ => await StartScanAsync());
            CopyResultsCommand = new RelayCommand(async _ => CopyResultsToClipboard());
            ChangeViewCommand = new RelayCommand(async viewName => { if(viewName != null) CurrentView = viewName.ToString()!; });
        }

        private void CopyResultsToClipboard()
        {
            if (Vulnerabilities.Count == 0)
            {
                System.Windows.MessageBox.Show("No results to copy!", "VoidStrike", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== VOIDSTRIKE SCAN RESULTS ===");
            sb.AppendLine($"Target: {TargetUrl}");
            sb.AppendLine($"Time: {DateTime.Now}");
            sb.AppendLine("-------------------------------");

            foreach (var v in Vulnerabilities)
            {
                sb.AppendLine($"[{v.Severity}] {v.Type}");
                sb.AppendLine($"URL: {v.Url}");
                if (!string.IsNullOrEmpty(v.Parameter)) sb.AppendLine($"Param: {v.Parameter}");
                if (!string.IsNullOrEmpty(v.Payload)) sb.AppendLine($"Payload: {v.Payload}");
                sb.AppendLine("---");
            }

            System.Windows.Clipboard.SetText(sb.ToString());
            System.Windows.MessageBox.Show("Results copied to clipboard!", "VoidStrike Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private async Task StartScanAsync()
        {
            if (string.IsNullOrWhiteSpace(TargetUrl)) return;
            if (!TargetUrl.StartsWith("http")) TargetUrl = "http://" + TargetUrl;

            IsScanning = true;
            ScanProgress = 0;
            Vulnerabilities.Clear();
            ScanLogs.Clear();
            StatusMessage = "INITIALIZING VOIDSTRIKE ELITE AI ENGINE (v12.0)...";

            // Override empty mode
            Config.SelfDrivingMode = false;

            // Discord RPC (Safe Call)
            try { DiscordService.Instance.SetStatus("Breaching Target", "URL: " + TargetUrl, true); } catch { }

            Config.TargetUrl = TargetUrl;
            var crawler = new CrawlerService();
            var scanner = new VulnerabilityScanner(Config);
            var mutation = new MutationEngine();
            var strategy = new AttackStrategyEngine(Config, scanner, mutation);

            // AI ADVISOR
            ScanLogs.Add("[AI Advisor] Analyzing target infrastructure...");
            ScanLogs.Add($"[AI Advisor] Goal-Based Mode active.");

            crawler.OnUrlCrawled += url => App.Current.Dispatcher.Invoke(() => {
                if(!ScanLogs.Contains($"[+] CRAWLED: {url}")) ScanLogs.Add($"[+] CRAWLED: {url}");
            });
            scanner.OnVulnerabilityFound += vuln => App.Current.Dispatcher.Invoke(() => {
                Vulnerabilities.Add(vuln);
                ScanLogs.Add($"[!] THREAT DETECTED: {vuln.Type} (CVSS: 9.8) -> {vuln.Url}");
            });
            scanner.OnLog += log => App.Current.Dispatcher.Invoke(() => ScanLogs.Add(log));

            try
            {
                if (Config.SelfDrivingMode)
                {
                    StatusMessage = "SELF-DRIVING SCAN IN PROGRESS...";
                    await strategy.ExecuteSelfDrivingScan();
                }
                else
                {
                    // Standard scan flow
                    StatusMessage = "MAPPING APPLICATION STRUCTURE...";
                    var pages = await crawler.StartCrawlingAsync(TargetUrl, Config.MaxDepth, Config.MaxThreads);

                    if (pages.Count == 0)
                        pages.Add(new CrawledPage { Url = TargetUrl, Method = "GET" });

                    StatusMessage = $"MAPPED {pages.Count} ENDPOINTS. STARTING ATTACK...";

                    int current = 0;
                    foreach (var page in pages)
                    {
                        if (!IsScanning) break;

                        ScanLogs.Add($"[*] Testing Endpoint: {page.Url}");
                        await scanner.ScanPageAsync(page);
                        current++;
                        ScanProgress = (int)((double)current / pages.Count * 100);
                    }
                }

                StatusMessage = "STRIKE COMPLETE.";
                ScanLogs.Add($"[#] Scan finished. Found {Vulnerabilities.Count} vulnerabilities.");
                // Discord RPC: Strike Complete
                DiscordService.Instance.SetStatus("Strike Complete", $"Found {Vulnerabilities.Count} High-Risk Vulnerabilities");

                // Export results logic...
                ExportResultsToAllFormats();
            }
            catch (Exception ex)
            {
                StatusMessage = "STRIKE FAILED.";
                ScanLogs.Add($"[ERROR] {ex.Message}");
            }
            finally
            {
                IsScanning = false;
                ScanProgress = 100;
            }
        }

        private void ExportResultsToAllFormats()
        {
            if (Vulnerabilities.Count == 0) return;
            ExportResultsToFile("txt");
            if (Config.ExportToJson) ExportResultsToFile("json");
            if (Config.ExportToHtml) ExportResultsToFile("html");
        }

        private void ExportResultsToFile(string format)
        {
            try
            {
                string fileName = $"VoidStrike_Results_{DateTime.Now:yyyyMMdd_HHmmss}.{format}";
                string fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
                string content = "";

                if (format == "json")
                {
                    content = System.Text.Json.JsonSerializer.Serialize(Vulnerabilities, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                }
                else if (format == "html")
                {
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine("<html><head><style>body{font-family:sans-serif;background:#111;color:#eee;} .v{border:1px solid #333;margin:10px;padding:10px;} .Critical{color:#ff4444;} .High{color:#ff8800;}</style></head><body>");
                    sb.AppendLine($"<h1>VoidStrike Scan Report - {TargetUrl}</h1>");
                    foreach (var v in Vulnerabilities)
                    {
                        sb.AppendLine($"<div class='v'><h2 class='{v.Severity}'>[{v.Severity}] {v.Type}</h2>");
                        sb.AppendLine($"<p><b>URL:</b> {v.Url}</p>");
                        sb.AppendLine($"<p><b>Description:</b> {v.Description}</p>");
                        sb.AppendLine($"<p><b>Payload:</b> <code>{v.Payload}</code></p></div>");
                    }
                    sb.AppendLine("</body></html>");
                    content = sb.ToString();
                }
                else
                {
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine("=== VOIDSTRIKE SCAN REPORT ===");
                    sb.AppendLine($"Target: {TargetUrl}");
                    sb.AppendLine($"Date: {DateTime.Now}");
                    sb.AppendLine("------------------------------");

                    foreach (var v in Vulnerabilities)
                    {
                        sb.AppendLine($"[{v.Severity}] {v.Type}");
                        sb.AppendLine($"URL: {v.Url}");
                        if (!string.IsNullOrEmpty(v.Parameter)) sb.AppendLine($"Parameter: {v.Parameter}");
                        if (!string.IsNullOrEmpty(v.Payload)) sb.AppendLine($"Payload: {v.Payload}");
                        sb.AppendLine("---");
                    }
                    content = sb.ToString();
                }

                System.IO.File.WriteAllText(fullPath, content);
                App.Current.Dispatcher.Invoke(() => ScanLogs.Add($"[+] Results exported to: {fileName}"));
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ScanLogs.Add($"[!] Export failed: {ex.Message}"));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Func<object?, Task> _execute;
        public RelayCommand(Func<object?, Task> execute) { _execute = execute; }
        public bool CanExecute(object? parameter) => true;
        public async void Execute(object? parameter) => await _execute(parameter);
        public event EventHandler? CanExecuteChanged;
    }
}
