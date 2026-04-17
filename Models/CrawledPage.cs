using System;
using System.Collections.Generic;

namespace VoidStrike.Models
{
    public class CrawledPage
    {
        public string Url { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int Depth { get; set; }
        public List<string> Parameters { get; set; } = new();
        public List<string> Forms { get; set; } = new();
        public bool IsApi { get; set; }
        public string Method { get; set; } = "GET";
        public string ContentType { get; set; } = "text/html";
        public int StatusCode { get; set; }

        // Elite Metadata
        public double HeuristicScore { get; set; }
        public string StructureHash { get; set; } = string.Empty;
        public List<string> ExtractedTokens { get; set; } = new();
        public List<string> ExtractedEmails { get; set; } = new();
        public List<string> JsEndpoints { get; set; } = new();
        public List<string> ApiEndpoints { get; set; } = new();
        public List<string> Links { get; set; } = new();
        public Dictionary<string, string> Headers { get; set; } = new();
        public List<string> PiiLeaked { get; set; } = new();
        public string ApiVersion { get; set; } = "v1";
        public bool AuthRequired { get; set; }
        public string WorkflowStep { get; set; } = string.Empty;
        public DateTime DiscoveryTime { get; set; } = DateTime.Now;
    }

    public class Vulnerability
    {
        public string Type { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty; // Critical, High, Medium, Low
        public string Description { get; set; } = string.Empty;
        public string Parameter { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public string ResponseEvidence { get; set; } = string.Empty;
        public double Confidence { get; set; } // 0.0 to 1.0
        public string CveId { get; set; } = "N/A";
        public string Remediation { get; set; } = string.Empty;
        public string AttackTimeline { get; set; } = string.Empty;
    }

    public enum ScanGoal { FullAudit, LoginBypass, ApiAbuse, DataLeak, FastStrike }
    public enum ScanMode { Stealth, Deep, Quick, Brutal }

    public class ScanConfig
    {
        public string TargetUrl { get; set; } = string.Empty;
        public int MaxDepth { get; set; } = 5;
        public int MaxThreads { get; set; } = 20;
        public ScanMode Mode { get; set; } = ScanMode.Deep;
        public ScanGoal Goal { get; set; } = ScanGoal.FullAudit;
        public bool AdaptiveSpeedControl { get; set; } = true;
        public bool SubdomainTakeover { get; set; } = true;
        public bool ScanCloudMisconfig { get; set; } = true;
        public bool ExportToJson { get; set; } = true;
        public bool ExportToHtml { get; set; } = true;
        public string UserAgent { get; set; } = "VoidStrike-Elite/12.0 (Autonomous AI)";

        // Engine Phases
        public bool PhaseRecon { get; set; } = true;
        public bool PhaseMapping { get; set; } = true;
        public bool PhaseTesting { get; set; } = true;
        public bool PhaseVerification { get; set; } = true;
        public bool SelfDrivingMode { get; set; } = true;
    }
}
