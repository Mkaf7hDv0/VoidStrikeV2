using System.Collections.Generic;

namespace VoidStrike.Models
{
    public class AttackGraph
    {
        public List<Node> Nodes { get; set; } = new List<Node>();
        public List<Edge> Edges { get; set; } = new List<Edge>();
    }

    public class Node
    {
        public string Id { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Type { get; set; } = "Endpoint"; // Endpoint, Vulnerability, Parameter
        public double RiskScore { get; set; }
    }

    public class Edge
    {
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public string Relation { get; set; } = "links_to"; // calls, submits_to, has_vuln
    }
}
