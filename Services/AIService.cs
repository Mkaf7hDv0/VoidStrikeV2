using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using VoidStrike.Models;

namespace VoidStrike.Services
{
    public class AIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private const string ApiUrl = "https://api.deepseek.com/chat/completions";

        public AIService(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }

        public async Task<string> AnalyzeVulnerabilityAsync(Vulnerability vuln)
        {
            var prompt = $"Analyze this security vulnerability and provide a professional assessment, remediation steps, and potential impact:\n\n" +
                         $"Type: {vuln.Type}\n" +
                         $"URL: {vuln.Url}\n" +
                         $"Parameter: {vuln.Parameter}\n" +
                         $"Payload: {vuln.Payload}\n" +
                         $"Severity: {vuln.Severity}\n\n" +
                         $"Please provide the response in markdown format.";

            var requestBody = new
            {
                model = "deepseek-chat",
                messages = new[]
                {
                    new { role = "system", content = "You are an expert security researcher and penetration tester." },
                    new { role = "user", content = prompt }
                },
                stream = false
            };

            var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(ApiUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    dynamic result = JsonConvert.DeserializeObject(responseBody);
                    return result.choices[0].message.content;
                }
                return $"AI Analysis failed: {response.StatusCode}";
            }
            catch (Exception ex)
            {
                return $"AI Analysis error: {ex.Message}";
            }
        }
    }
}
