using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using VoidStrike.Models;

namespace VoidStrike.Services
{
    public class HttpProxyInterceptor
    {
        private readonly HttpClient _httpClient;
        private readonly List<HttpRequestModel> _history = new List<HttpRequestModel>();

        public HttpProxyInterceptor(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<HttpResponseMessage> SendRequest(HttpRequestModel request)
        {
            var httpRequest = new HttpRequestMessage(new HttpMethod(request.Method), request.Url);

            foreach (var header in request.Headers)
            {
                httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            if (!string.IsNullOrEmpty(request.Body) && (request.Method == "POST" || request.Method == "PUT"))
            {
                httpRequest.Content = new StringContent(request.Body, System.Text.Encoding.UTF8, "application/json");
            }

            var response = await _httpClient.SendAsync(httpRequest);

            request.ResponseCode = (int)response.StatusCode;
            request.ResponseBody = await response.Content.ReadAsStringAsync();

            _history.Add(request);

            return response;
        }

        public async Task<HttpResponseMessage> ReplayWithMutation(HttpRequestModel request, MutationEngine mutation)
        {
            // Apply intelligent mutation to the original request
            var mutatedBody = MutationEngine.MutatePayload(request.Body);
            var mutatedRequest = new HttpRequestModel
            {
                Url = request.Url,
                Method = request.Method,
                Headers = new Dictionary<string, string>(request.Headers),
                Body = mutatedBody
            };

            return await SendRequest(mutatedRequest);
        }

        public List<HttpRequestModel> GetHistory() => _history;
    }

    public class HttpRequestModel
    {
        public string Url { get; set; } = string.Empty;
        public string Method { get; set; } = "GET";
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        public string Body { get; set; } = string.Empty;
        public string ResponseBody { get; set; } = string.Empty;
        public int ResponseCode { get; set; }
    }
}
