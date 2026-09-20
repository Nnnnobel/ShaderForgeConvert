using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ShaderForgeConvert
{
    public sealed class ForgeNode
    {
        public int Id { get; internal set; }
        public string Type { get; internal set; }
        public string Comment { get; internal set; }
        public string PropertyName { get; internal set; }
        public Dictionary<string, int> Inputs { get; } = new Dictionary<string, int>(StringComparer.Ordinal);
    }

    public sealed class ForgeGraph
    {
        public string Version { get; internal set; }
        public int DataStart { get; internal set; }
        public int DataEnd { get; internal set; }
        public Dictionary<int, ForgeNode> Nodes { get; } = new Dictionary<int, ForgeNode>();
        public Dictionary<string, int> FinalInputs { get; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public HashSet<int> ReachableNodes()
        {
            var result = new HashSet<int>();
            foreach (int id in FinalInputs.Values)
                Visit(id, result);
            return result;
        }

        private void Visit(int id, HashSet<int> visited)
        {
            if (!visited.Add(id) || !Nodes.TryGetValue(id, out ForgeNode node)) return;
            foreach (int inputId in node.Inputs.Values) Visit(inputId, visited);
        }
    }

    public static class ForgeGraphReader
    {
        private static readonly Regex NodeHeader = new Regex(@"^n:type:ShaderForge\.(?<type>SFN_[A-Za-z0-9_]+),id:(?<id>[0-9]+)(?:,|$)", RegexOptions.CultureInvariant);
        private static readonly Regex Input = new Regex(@"(?:^|,)(?<port>[A-Za-z][A-Za-z0-9_]*)-(?<id>[0-9]+)-(?<out>[A-Za-z0-9_]+)", RegexOptions.CultureInvariant);

        public static ForgeGraph Read(string source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            int start = source.IndexOf("/*SF_DATA;", StringComparison.Ordinal);
            if (start < 0) throw new FormatException("No Shader Forge SF_DATA block was found.");
            if (source.Substring(0, start).IndexOf("Shader \"", StringComparison.Ordinal) >= 0)
                throw new FormatException("SF_DATA must precede the Shader declaration.");
            int end = source.IndexOf("*/", start + 10, StringComparison.Ordinal);
            if (end < 0) throw new FormatException("SF_DATA block is not closed.");
            string data = source.Substring(start + 2, end - start - 2);
            if (!data.StartsWith("SF_DATA;", StringComparison.Ordinal)) throw new FormatException("Invalid SF_DATA block.");

            var graph = new ForgeGraph { DataStart = start, DataEnd = end + 2 };
            // Shader Forge itself uses semicolon-separated records. Unknown records are retained
            // as unsupported by callers rather than interpreted as HLSL.
            foreach (string record in data.Split(';'))
            {
                if (record.StartsWith("ver:", StringComparison.Ordinal))
                    graph.Version = record.Substring(4);
                if (!record.StartsWith("n:", StringComparison.Ordinal)) continue;
                string[] parts = record.Split(new[] { '|' }, 2);
                Match header = NodeHeader.Match(parts[0]);
                if (!header.Success) throw new FormatException("Malformed Shader Forge node: " + parts[0]);
                int id = int.Parse(header.Groups["id"].Value, CultureInfo.InvariantCulture);
                if (graph.Nodes.ContainsKey(id)) throw new FormatException("Duplicate node ID: " + id);
                var node = new ForgeNode
                {
                    Id = id,
                    Type = header.Groups["type"].Value,
                    Comment = Field(parts[0], "cmnt"),
                    PropertyName = Field(parts[0], "ptin")
                };
                if (parts.Length == 2)
                {
                    foreach (Match input in Input.Matches(parts[1]))
                    {
                        string port = input.Groups["port"].Value;
                        int sourceId = int.Parse(input.Groups["id"].Value, CultureInfo.InvariantCulture);
                        node.Inputs[port] = sourceId;
                        if (node.Type == "SFN_Final") graph.FinalInputs[port] = sourceId;
                    }
                }
                graph.Nodes.Add(id, node);
            }
            if (graph.Nodes.Count == 0 || graph.FinalInputs.Count == 0)
                throw new FormatException("SF_DATA has no connected Final node.");
            foreach (ForgeNode node in graph.Nodes.Values)
                foreach (int sourceId in node.Inputs.Values)
                    if (!graph.Nodes.ContainsKey(sourceId))
                        throw new FormatException("Node " + node.Id + " references missing node " + sourceId);
            return graph;
        }

        private static string Field(string header, string name)
        {
            Match match = Regex.Match(header, @"(?:^|,)" + name + @":([^,]*)", RegexOptions.CultureInvariant);
            return match.Success ? match.Groups[1].Value : null;
        }
    }
}
