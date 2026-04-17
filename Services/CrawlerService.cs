using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using VoidStrike.Models;

namespace VoidStrike.Services
{
    public class CrawlerService
    {
        private readonly HttpClient _httpClient;
        private readonly CookieContainer _cookieContainer = new CookieContainer();
        private readonly ConcurrentDictionary<string, byte> _visitedUrls = new ConcurrentDictionary<string, byte>();
        private readonly ConcurrentBag<CrawledPage> _crawledPages = new ConcurrentBag<CrawledPage>();
        private readonly ConcurrentDictionary<string, byte> _normalizedEndpoints = new ConcurrentDictionary<string, byte>();

        // Priority Queue for Heuristic Scoring (Heuristic Scoring)
        private readonly PriorityQueue<(string Url, int Depth, string Method), double> _urlQueue = new PriorityQueue<(string, int, string), double>();

        private string _baseDomain = string.Empty;
        private int _maxDepth = 3;
        private int _delayMs = 100;

        public event Action<string>? OnUrlCrawled;
        public event Action<string>? OnDiscoveryLog;

        public CrawlerService()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
                CookieContainer = _cookieContainer,
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            _httpClient = new HttpClient(handler);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "VoidStrike/v12.0 (Elite AI Crawler)");
            _httpClient.Timeout = TimeSpan.FromSeconds(20);
        }

        public async Task<List<CrawledPage>> StartCrawlingAsync(string startUrl, int maxDepth, int maxThreads, int delay = 100)
        {
            _maxDepth = maxDepth;
            _delayMs = delay;
            _visitedUrls.Clear();
            _normalizedEndpoints.Clear();
            while (_crawledPages.TryTake(out _)) { }
            while (_urlQueue.Count > 0) _urlQueue.Dequeue();

            Uri baseUri = new Uri(startUrl);
            _baseDomain = baseUri.Host;

            // Initial Page setup to ensure scan doesn't stop if crawling is slow
            _crawledPages.Add(new CrawledPage { Url = startUrl, Method = "GET" });

            // Initial URL with high priority
            _urlQueue.Enqueue((startUrl, 0, "GET"), 100);
            OnUrlCrawled?.Invoke($"[Target-Seed] Starting discovery at {startUrl}");

            var semaphore = new SemaphoreSlim(maxThreads);
            var tasks = new List<Task>();

            while (_urlQueue.Count > 0 || tasks.Any(t => !t.IsCompleted))
            {
                if (_urlQueue.Count > 0)
                {
                    var current = _urlQueue.Dequeue();

                    if (current.Depth > _maxDepth || _visitedUrls.ContainsKey(current.Url))
                        continue;

                    if (IsStaticFile(current.Url)) continue;

                    _visitedUrls.TryAdd(current.Url, 0);
                    OnUrlCrawled?.Invoke($"[{current.Method}] {current.Url}");

                    await semaphore.WaitAsync();
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            await ProcessUrl(current.Url, current.Depth, current.Method);
                            await Task.Delay(CalculateAdaptiveDelay());
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }));
                }
                else
                {
                    await Task.Delay(200);
                }
                tasks.RemoveAll(t => t.IsCompleted);
            }

            // Wordlist-Based Crawling & Backup File Discovery
            await DiscoverHiddenPaths(startUrl);

            await Task.WhenAll(tasks);
            return _crawledPages.ToList();
        }

        private int CalculateAdaptiveDelay()
        {
            // Adaptive Delay: Dynamic adjustment based on response time could be added here
            return _delayMs;
        }

        private async Task ProcessUrl(string url, int depth, string method)
        {
            try
            {
                var request = new HttpRequestMessage(new HttpMethod(method), url);
                var response = await _httpClient.SendAsync(request);

                var page = new CrawledPage
                {
                    Url = url,
                    Method = method,
                    StatusCode = (int)response.StatusCode,
                    Headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value))
                };

                // Session Tracking: Cookies are automatically handled by CookieContainer

                if (!response.IsSuccessStatusCode)
                {
                    // Retry Logic / Error Handling
                    if (response.StatusCode == HttpStatusCode.TooManyRequests) await Task.Delay(5000);
                    return;
                }

                var content = await response.Content.ReadAsStringAsync();
                page.Content = content;

                // GraphQL Detection
                if (url.Contains("/graphql") || content.Contains("__schema"))
                    OnDiscoveryLog?.Invoke($"[GraphQL] Detected potential endpoint: {url}");

                // REST API Mapping
                if (response.Content.Headers.ContentType?.MediaType == "application/json")
                {
                    page.IsApi = true;
                    ExtractJsonEndpoints(content, page);
                }
                else
                {
                    var doc = new HtmlDocument();
                    doc.LoadHtml(content);
                    ExtractHtmlElements(doc, url, depth, page);

                    // Static JS Parsing
                    await AnalyzeJavaScript(doc, url, page);

                    // Information Extraction (Emails, Tokens)
                    ExtractInformation(content, page);
                }

                // Deep Discovery
                DiscoverRegexUrls(content, url, depth, page);
                MapParameters(url, page);

                _crawledPages.Add(page);
            }
            catch (Exception ex)
            {
                OnDiscoveryLog?.Invoke($"[!] Error crawling {url}: {ex.Message}");
            }
        }

        private void ExtractHtmlElements(HtmlDocument doc, string baseUrl, int depth, CrawledPage page)
        {
            // Extract <a> links
            var links = doc.DocumentNode.SelectNodes("//a[@href]");
            if (links != null && depth < _maxDepth)
            {
                foreach (var link in links)
                {
                    var href = link.GetAttributeValue("href", string.Empty);
                    AddUrlToQueue(href, baseUrl, depth + 1, page);
                }
            }

            // Extract <form> & Auto-fill/Submit behavior
            var forms = doc.DocumentNode.SelectNodes("//form");
            if (forms != null)
            {
                foreach (var form in forms)
                {
                    var action = form.GetAttributeValue("action", string.Empty);
                    var method = form.GetAttributeValue("method", "GET").ToUpper();

                    // Identify fields for Auto-fill
                    var inputs = form.SelectNodes(".//input[@name]");
                    var formParams = new List<string>();
                    if (inputs != null)
                    {
                        foreach (var input in inputs)
                        {
                            var name = input.GetAttributeValue("name", "");
                            var type = input.GetAttributeValue("type", "text");
                            formParams.Add($"{name}={type}");

                            // Specific behavior for Search boxes
                            if (name.Contains("search") || name.Contains("query") || name.Contains("q"))
                            {
                                AddUrlToQueue($"{action}?{name}=voidstrike_test", baseUrl, depth + 1, page);
                            }
                        }
                    }

                    page.Forms.Add($"[{method}] {action} | Params: {string.Join(", ", formParams)}");

                    // Priority-Based Crawling: Forms are high priority
                    if (!string.IsNullOrEmpty(action))
                        AddUrlToQueue(action, baseUrl, depth + 1, page, method);
                }
            }
        }

        private void ExtractInformation(string content, CrawledPage page)
        {
            // Email Extraction
            var emailRegex = new Regex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled);
            foreach (Match match in emailRegex.Matches(content))
                page.ExtractedEmails.Add(match.Value);

            // API keys / Tokens (Sensitive Information Extraction)
            var tokenPatterns = new[] {
                @"(?:api_key|auth_token|secret|access_token|key)['""]?\s*[:=]\s*['""]([a-zA-Z0-9_\-\.]{16,})['""]",
                @"AIza[0-9A-Za-z-_]{35}" // Google API Key
            };
            foreach (var pattern in tokenPatterns)
                foreach (Match match in Regex.Matches(content, pattern, RegexOptions.IgnoreCase))
                    page.ExtractedTokens.Add(match.Value);
        }

        private void ExtractJsonEndpoints(string json, CrawledPage page)
        {
            // Nested Parameters & Endpoint Discovery in JSON
            var regex = new Regex(@"['""](/[a-zA-Z0-9_\-/]+)['""]", RegexOptions.Compiled);
            foreach (Match match in regex.Matches(json))
                page.ApiEndpoints.Add(match.Groups[1].Value);
        }

        private async Task AnalyzeJavaScript(HtmlDocument doc, string baseUrl, CrawledPage page)
        {
            var scripts = doc.DocumentNode.SelectNodes("//script");
            if (scripts != null)
            {
                foreach (var script in scripts)
                {
                    var jsContent = script.InnerText;
                    var src = script.GetAttributeValue("src", string.Empty);

                    if (!string.IsNullOrEmpty(src))
                    {
                        try {
                            var jsUrl = new Uri(new Uri(baseUrl), src).AbsoluteUri;
                            page.Links.Add($"[JS-File] {jsUrl}");
                        } catch {}
                    }
                    if (!string.IsNullOrEmpty(jsContent))
                    {
                        ExtractApiEndpointsFromJs(jsContent, page);
                    }
                }
            }
        }

        private void ExtractApiEndpointsFromJs(string jsCode, CrawledPage page)
        {
            // Static JS Parsing: Extraction of routes and endpoints
            string[] patterns = {
                @"fetch\s*\(\s*['""](.*?)['""]",
                @"axios\.(?:get|post|put|delete)\s*\(\s*['""](.*?)['""]",
                @"['""]/(api/v[0-9]/.*?)['""]",
                @"path:\s*['""](.*?)['""]",
                @"route:\s*['""](.*?)['""]"
            };

            foreach (var pattern in patterns)
            {
                foreach (Match match in Regex.Matches(jsCode, pattern))
                {
                    var endpoint = match.Groups[1].Value;
                    page.ApiEndpoints.Add(endpoint);
                    AddUrlToQueue(endpoint, page.Url, 1, page);
                }
            }
        }

        private void DiscoverRegexUrls(string content, string baseUrl, int depth, CrawledPage page)
        {
            var urlRegex = new Regex(@"https?://[^\s'""<>]+", RegexOptions.Compiled);
            foreach (Match match in urlRegex.Matches(content))
                AddUrlToQueue(match.Value, baseUrl, depth + 1, page);
        }

        private void AddUrlToQueue(string href, string baseUrl, int depth, CrawledPage page, string method = "GET")
        {
            try
            {
                if (string.IsNullOrEmpty(href) || href.StartsWith("javascript:") || href.StartsWith("mailto:") || href.StartsWith("#")) return;

                var absoluteUri = new Uri(new Uri(baseUrl), href);
                var absoluteUrl = absoluteUri.AbsoluteUri.Split('#')[0];

                if (absoluteUri.Host == _baseDomain)
                {
                    var normalized = absoluteUrl.Split('?')[0];
                    if (_normalizedEndpoints.TryAdd(normalized, 0))
                    {
                        // Priority-Based Crawling (Heuristic Scoring)
                        double score = CalculateHeuristicScore(absoluteUrl, method);
                        _urlQueue.Enqueue((absoluteUrl, depth, method), -score);
                        page.Links.Add(absoluteUrl);
                    }
                }
            }
            catch { }
        }

        private double CalculateHeuristicScore(string url, string method)
        {
            double score = 1.0;
            if (url.Contains("?") || url.Contains("=")) score += 10.0; // Parameters
            if (url.Contains("/api/")) score += 15.0; // API Endpoints
            if (method == "POST") score += 20.0; // Forms
            if (url.Contains("login") || url.Contains("auth") || url.Contains("sign")) score += 25.0; // Auth/Workflows
            if (url.EndsWith(".js")) score += 5.0; // JS Parsing
            return score;
        }

        private void MapParameters(string url, CrawledPage page)
        {
            try {
                var uri = new Uri(url);
                var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                foreach (string? key in query.AllKeys)
                {
                    if (key == null) continue;
                    var value = query[key];
                    page.Parameters.Add(key);

                    // Encoded Parameters Detection (Base64)
                    if (!string.IsNullOrEmpty(value) && IsBase64(value))
                    {
                        OnDiscoveryLog?.Invoke($"[Encoded-Param] Found Base64 in {key}: {value}");
                        // Auto Parameter Mutation: Try common decodings
                        try {
                            var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(value));
                            OnDiscoveryLog?.Invoke($"[Decoded-Value] {key} -> {decoded}");
                        } catch {}
                    }
                }
            } catch { }
        }

        private bool IsBase64(string base64String)
        {
            if (string.IsNullOrEmpty(base64String) || base64String.Length < 8 || base64String.Length % 4 != 0
                || base64String.Contains(" ") || base64String.Contains("\t") || base64String.Contains("\r") || base64String.Contains("\n"))
                return false;
            try {
                Convert.FromBase64String(base64String);
                return true;
            } catch { return false; }
        }

        private bool IsStaticFile(string url)
        {
            string[] extensions = { ".jpg", ".jpeg", ".png", ".gif", ".css", ".woff", ".woff2", ".ttf", ".svg", ".ico", ".pdf" };
            return extensions.Any(ext => url.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
        }

        public async Task DiscoverHiddenPaths(string baseUrl)
        {
            // Wordlist-Based Crawling & Backup File Discovery
            string[] wordlist = {
                "admin", "backup", "api/v1", "api/v2", "dev", "test", "config", ".env", ".git", "graphql", "swagger",
                "openapi.json", "setup", "manage", "old", "backup.zip", "data.sql", "db.sqlite"
            };
            foreach (var word in wordlist)
            {
                var target = $"{baseUrl.TrimEnd('/')}/{word}";
                try {
                    var res = await _httpClient.GetAsync(target);
                    if (res.IsSuccessStatusCode || res.StatusCode == HttpStatusCode.Forbidden)
                        OnDiscoveryLog?.Invoke($"[Hidden-Path/Discovery] Found: {target} ({res.StatusCode})");
                } catch {}
            }
        }
    }
}
