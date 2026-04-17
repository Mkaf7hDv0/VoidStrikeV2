using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using VoidStrike.Models;

namespace VoidStrike.Services
{
    public class ReportingService
    {
        public string GenerateHtmlReport(List<Vulnerability> vulnerabilities, ScanConfig config)
        {
            var sb = new StringBuilder();
            sb.Append("<html><head><title>VoidStrike Security Report</title>");
            sb.Append("<style>body { font-family: sans-serif; margin: 40px; } ");
            sb.Append("table { width: 100%; border-collapse: collapse; margin-top: 20px; } ");
            sb.Append("th, td { border: 1px solid #ddd; padding: 12px; text-align: left; } ");
            sb.Append("th { background-color: #2c3e50; color: white; } ");
            sb.Append(".severity-critical { color: #c0392b; font-weight: bold; } ");
            sb.Append(".severity-high { color: #e67e22; font-weight: bold; } ");
            sb.Append("</style></head><body>");

            sb.Append("<h1>VoidStrike v9.0 Security Assessment Report</h1>");
            sb.Append($"<p><strong>Target URL:</strong> {config.TargetUrl}</p>");
            sb.Append($"<p><strong>Scan Date:</strong> {DateTime.Now:F}</p>");
            sb.Append($"<p><strong>Total Vulnerabilities Found:</strong> {vulnerabilities.Count}</p>");

            sb.Append("<table><tr><th>Type</th><th>Severity</th><th>Parameter</th><th>URL</th><th>Payload</th></tr>");

            foreach (var vuln in vulnerabilities)
            {
                var severityClass = $"severity-{vuln.Severity.ToLower()}";
                sb.Append("<tr>");
                sb.Append($"<td>{vuln.Type}</td>");
                sb.Append($"<td class='{severityClass}'>{vuln.Severity}</td>");
                sb.Append($"<td>{vuln.Parameter}</td>");
                sb.Append($"<td>{vuln.Url}</td>");
                sb.Append($"<td><code>{System.Web.HttpUtility.HtmlEncode(vuln.Payload)}</code></td>");
                sb.Append("</tr>");
            }

            sb.Append("</table></body></html>");
            return sb.ToString();
        }

        public void SaveReport(string html, string filePath)
        {
            File.WriteAllText(filePath, html);
        }
    }
}
