using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using VoidStrike.Models;

namespace VoidStrike.Services
{
    public class AccessControlEngine
    {
        private readonly HttpClient _httpClient;
        private readonly Dictionary<string, string> _userSessions = new(); // Role -> Cookie/Token

        public AccessControlEngine(HttpClient client)
        {
            _httpClient = client;
        }

        public void AddSession(string role, string token)
        {
            _userSessions[role] = token;
        }

        public async Task<List<Vulnerability>> TestPrivilegeEscalation(CrawledPage page)
        {
            var vulns = new List<Vulnerability>();

            if (_userSessions.Count < 2) return vulns;

            // Scenario: Accessing 'Admin' resource with 'User' session
            var adminResources = new[] { "/admin", "/api/v1/delete", "/api/v1/users", "/config" };
            bool isPotentialAdmin = adminResources.Any(r => page.Url.Contains(r));

            if (isPotentialAdmin)
            {
                foreach (var role in _userSessions.Keys.Where(k => k != "Admin"))
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, page.Url);
                    request.Headers.Add("Cookie", _userSessions[role]);

                    var response = await _httpClient.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        vulns.Add(new Vulnerability
                        {
                            Type = "Vertical Privilege Escalation",
                            Url = page.Url,
                            Severity = "Critical",
                            Description = $"Low-privileged role '{role}' can access admin resource.",
                            Confidence = 0.9
                        });
                    }
                }
            }

            return vulns;
        }

        public async Task<List<Vulnerability>> TestHorizontalIDOR(CrawledPage page, string param, string id1, string id2, string session1)
        {
            var vulns = new List<Vulnerability>();

            // Try to access ID2 using Session1
            var testUrl = page.Url.Replace($"{param}={id1}", $"{param}={id2}");
            if (testUrl == page.Url) return vulns;

            var request = new HttpRequestMessage(HttpMethod.Get, testUrl);
            request.Headers.Add("Cookie", session1);

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                // Heuristic: If content looks like a profile or private data
                if (content.Contains("email") || content.Contains("address") || content.Length > 500)
                {
                    vulns.Add(new Vulnerability
                    {
                        Type = "Horizontal IDOR",
                        Url = testUrl,
                        Severity = "High",
                        Description = "Accessing another user's data via ID manipulation.",
                        Confidence = 0.85
                    });
                }
            }

            return vulns;
        }
    }
}
