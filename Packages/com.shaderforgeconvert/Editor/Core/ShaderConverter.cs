using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ShaderForgeConvert
{
    public sealed class ConversionResult
    {
        public string Source { get; internal set; }
        public string OriginalShaderName { get; internal set; }
        public string NewShaderName { get; internal set; }
        public string GraphVersion { get; internal set; }
        public int TotalNodes { get; internal set; }
        public int ReachableNodes { get; internal set; }
        public int ExtractedFunctions { get; internal set; }
        public int SampleVariablesRenamed { get; internal set; }
        public Dictionary<int, string> RenamedNodes { get; } = new Dictionary<int, string>();
        public List<string> Warnings { get; } = new List<string>();
        public bool IsPartial => Warnings.Count != 0;
    }

    public static class ShaderConverter
    {
        private static readonly Regex ShaderDeclaration = new Regex(@"(?m)^(?<indent>[ \t]*)Shader[ \t]+\""(?<name>[^\""\r\n]+)\""", RegexOptions.CultureInvariant);
        private static readonly Regex ForgeInspector = new Regex(@"(?m)^[ \t]*CustomEditor[ \t]+\""ShaderForgeMaterialInspector\""[ \t]*\r?\n?", RegexOptions.CultureInvariant);
        private static readonly Regex Declaration = new Regex(@"(?m)^(?<indent>[ \t]*)(?:float|half|fixed)(?:[1-4]|[1-4]x[1-4])?[ \t]+(?<name>[A-Za-z_][A-Za-z0-9_]*)[ \t]*=", RegexOptions.CultureInvariant);
        private static readonly HashSet<string> HlslReserved = new HashSet<string>(StringComparer.Ordinal)
        {
            "float", "half", "fixed", "int", "bool", "return", "if", "else", "for", "while", "struct", "void",
            "sample", "texture", "normal", "color", "position", "output", "input"
        };

        public static ConversionResult Convert(string input, string newShaderName)
        {
            if (string.IsNullOrWhiteSpace(newShaderName)) throw new ArgumentException("A new Shader name is required.", nameof(newShaderName));
            if (newShaderName.IndexOf('"') >= 0 || newShaderName.IndexOf('\n') >= 0 || newShaderName.IndexOf('\r') >= 0)
                throw new ArgumentException("The Shader name contains invalid characters.", nameof(newShaderName));

            ForgeGraph graph = ForgeGraphReader.Read(input);
            string body = input.Remove(graph.DataStart, graph.DataEnd - graph.DataStart);
            body = Regex.Replace(body, @"\A(?:(?:// Shader created with Shader Forge[^\r\n]*|// Shader Forge \(c\)[^\r\n]*|// Note: Manually altering this data[^\r\n]*)\r?\n)+", "");
            Match shader = ShaderDeclaration.Match(body);
            if (!shader.Success) throw new FormatException("No top-level Shader declaration was found.");
            var result = new ConversionResult
            {
                OriginalShaderName = shader.Groups["name"].Value,
                NewShaderName = newShaderName,
                GraphVersion = graph.Version,
                TotalNodes = graph.Nodes.Count,
                ReachableNodes = graph.ReachableNodes().Count
            };
            if (result.OriginalShaderName == newShaderName)
                throw new ArgumentException("The output Shader name must differ from the original.", nameof(newShaderName));
            body = body.Remove(shader.Groups["name"].Index, shader.Groups["name"].Length)
                .Insert(shader.Groups["name"].Index, newShaderName);
            body = ForgeInspector.Replace(body, "");

            HashSet<int> reachable = graph.ReachableNodes();
            var proposed = new Dictionary<string, string>(StringComparer.Ordinal);
            var usedNames = new HashSet<string>(StringComparer.Ordinal);
            var sampleCandidates = new HashSet<string>(StringComparer.Ordinal);
            foreach (ForgeNode node in graph.Nodes.Values.OrderBy(n => n.Id))
            {
                if (!reachable.Contains(node.Id) || string.IsNullOrWhiteSpace(node.Comment)) continue;
                string generatedNodeName = "node_" + node.Id.ToString(CultureInfo.InvariantCulture);
                if (ReferencedInDirective(body, generatedNodeName)) continue;
                string name = Identifier(node.Comment);
                if (name.Length == 0 || HlslReserved.Contains(name)) continue;
                name = UniqueName(name, node.Id, body, usedNames);
                if (name == generatedNodeName) continue;
                proposed[generatedNodeName] = name;
                result.RenamedNodes[node.Id] = name;
            }
            foreach (ForgeNode node in graph.Nodes.Values.OrderBy(n => n.Id))
            {
                if (!reachable.Contains(node.Id) || string.IsNullOrEmpty(node.PropertyName)) continue;
                if (node.Type != "SFN_Tex2d" && node.Type != "SFN_Tex2dAsset" && node.Type != "SFN_Cubemap") continue;
                string generated = node.PropertyName + "_var";
                if (!Regex.IsMatch(body, @"\b" + Regex.Escape(generated) + @"\b", RegexOptions.CultureInvariant)) continue;
                if (ReferencedInDirective(body, generated)) continue;
                string semantic = Identifier(node.PropertyName.TrimStart('_')) + "Sample";
                if (semantic.Length == 0) continue;
                semantic = UniqueName(semantic, node.Id, body, usedNames);
                proposed[generated] = semantic;
                sampleCandidates.Add(generated);
            }

            // Only touch HLSL/CG program regions. The tokenizer skips strings, comments and
            // preprocessor lines, so node identifiers in documentation or macros are preserved.
            var rewritten = new HashSet<string>(StringComparer.Ordinal);
            body = RewritePrograms(body, proposed, graph, result, rewritten);
            foreach (int nodeId in result.RenamedNodes.Keys.ToArray())
                if (!rewritten.Contains("node_" + nodeId.ToString(CultureInfo.InvariantCulture)))
                    result.RenamedNodes.Remove(nodeId);
            result.SampleVariablesRenamed = sampleCandidates.Count(rewritten.Contains);
            result.Source = body.TrimStart('\r', '\n');
            if (result.ExtractedFunctions == 0)
                result.Warnings.Add("No complete emission logic chain was extracted into a function.");
            result.Warnings.Add("Visual and material equivalence have not been verified for this Shader.");
            if (result.RenamedNodes.Count == 0 && result.SampleVariablesRenamed == 0 && result.ExtractedFunctions == 0)
                result.Warnings.Add("No graph-linked source expressions were restructured.");
            return result;
        }

        private static bool ReferencedInDirective(string source, string identifier)
        {
            var name = new Regex(@"\b" + Regex.Escape(identifier) + @"\b", RegexOptions.CultureInvariant);
            int position = 0;
            bool continued = false;
            while (position < source.Length)
            {
                int end = source.IndexOf('\n', position);
                if (end < 0) end = source.Length;
                string line = source.Substring(position, end - position);
                bool directive = continued || line.TrimStart(' ', '\t').StartsWith("#", StringComparison.Ordinal);
                if (directive && name.IsMatch(line)) return true;
                continued = directive && line.TrimEnd(' ', '\t', '\r').EndsWith("\\", StringComparison.Ordinal);
                position = end < source.Length ? end + 1 : end;
            }
            return false;
        }

        private static string UniqueName(string preferred, int id, string source, HashSet<string> usedNames)
        {
            string candidate = preferred;
            int suffix = id;
            while (usedNames.Contains(candidate) || Regex.IsMatch(source, @"\b" + Regex.Escape(candidate) + @"\b", RegexOptions.CultureInvariant))
                candidate = preferred + "_" + (suffix++).ToString(CultureInfo.InvariantCulture);
            usedNames.Add(candidate);
            return candidate;
        }

        private static string RewritePrograms(string source, Dictionary<string, string> names, ForgeGraph graph, ConversionResult result, HashSet<string> rewritten)
        {
            var program = new Regex(@"(?m)^[ \t]*(CGPROGRAM|HLSLPROGRAM)[ \t]*$", RegexOptions.CultureInvariant);
            var endProgram = new Regex(@"(?m)^[ \t]*(ENDCG|ENDHLSL)[ \t]*$", RegexOptions.CultureInvariant);
            var output = new StringBuilder(source.Length);
            int cursor = 0;
            while (true)
            {
                Match start = program.Match(source, cursor);
                if (!start.Success) break;
                Match end = endProgram.Match(source, start.Index + start.Length);
                if (!end.Success) throw new FormatException("Unclosed shader program region.");
                int codeStart = start.Index + start.Length;
                output.Append(source, cursor, codeStart - cursor);
                string code = source.Substring(codeStart, end.Index - codeStart);
                code = ReplaceIdentifiers(code, names, rewritten);
                code = AddLogicComments(code, graph, result);
                if (graph.FinalInputs.Count == 1 && graph.FinalInputs.ContainsKey("emission"))
                    code = TryExtractEmission(code, result);
                output.Append(code);
                cursor = end.Index;
            }
            output.Append(source, cursor, source.Length - cursor);
            return output.ToString();
        }

        private static string TryExtractEmission(string code, ConversionResult result)
        {
            const string signature = "float4 frag(VertexOutput i) : COLOR {";
            if (code.IndexOf("ComputeEmission(", StringComparison.Ordinal) >= 0) return code;
            int fragment = code.IndexOf(signature, StringComparison.Ordinal);
            if (fragment < 0) return code;
            int bodyStart = fragment + signature.Length;
            int final = code.IndexOf("float3 finalColor = emissive;", bodyStart, StringComparison.Ordinal);
            if (final < 0) return code;
            int firstClosingBrace = code.IndexOf('}', bodyStart);
            if (firstClosingBrace < 0 || final > firstClosingBrace) return code;
            int finalEnd = final + "float3 finalColor = emissive;".Length;
            string prefix = code.Substring(bodyStart, final - bodyStart);
            string[] lines = prefix.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            bool hasEmission = false;
            var keptLines = new List<string>();
            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (trimmed == "////// Lighting:" || trimmed == "////// Emissive:") continue;
                keptLines.Add(line);
                if (trimmed.Length == 0 || trimmed.StartsWith("//", StringComparison.Ordinal)) continue;
                if (!Regex.IsMatch(trimmed, @"^(?:float|half|fixed)(?:[1-4])?\s+[A-Za-z_][A-Za-z0-9_]*\s*=\s*[^;]+;$", RegexOptions.CultureInvariant))
                    return code;
                if (trimmed.StartsWith("float3 emissive =", StringComparison.Ordinal)) hasEmission = true;
            }
            if (!hasEmission) return code;
            string newline = code.Contains("\r\n") ? "\r\n" : "\n";
            string logicBody = string.Join(newline, keptLines).TrimEnd(' ', '\t', '\r', '\n') + newline;
            string helper = "float3 ComputeEmission(VertexOutput i) {" + logicBody +
                            "                return emissive;" + newline + "            }" + newline + "            ";
            string replacement = newline + "                float3 finalColor = ComputeEmission(i);";
            code = code.Remove(bodyStart, finalEnd - bodyStart).Insert(bodyStart, replacement);
            code = code.Insert(fragment, helper);
            result.ExtractedFunctions++;
            return code;
        }

        private static string AddLogicComments(string code, ForgeGraph graph, ConversionResult result)
        {
            return Declaration.Replace(code, match =>
            {
                int previousOpen = code.LastIndexOf("/*", match.Index, StringComparison.Ordinal);
                int previousClose = code.LastIndexOf("*/", match.Index, StringComparison.Ordinal);
                if (previousOpen > previousClose) return match.Value;
                string name = match.Groups["name"].Value;
                KeyValuePair<int, string> renamed = result.RenamedNodes.FirstOrDefault(x => x.Value == name);
                if (renamed.Value == null) return match.Value;
                ForgeNode node = graph.Nodes[renamed.Key];
                string comment = SafeComment(node.Comment);
                return match.Groups["indent"].Value + "// Shader Forge node " + node.Id + ": " + comment + "\n" + match.Value;
            });
        }

        private static string SafeComment(string value)
        {
            return value.Replace('\r', ' ').Replace('\n', ' ').Replace("*/", "* /");
        }

        private static string Identifier(string label)
        {
            string[] words = Regex.Split(label.Trim(), @"[^A-Za-z0-9_]+")
                .Where(x => x.Length != 0).ToArray();
            if (words.Length == 0) return "";
            var result = new StringBuilder(words[0].Substring(0, 1).ToLowerInvariant() + words[0].Substring(1));
            for (int i = 1; i < words.Length; i++)
                result.Append(words[i].Substring(0, 1).ToUpperInvariant()).Append(words[i].Substring(1));
            if (char.IsDigit(result[0])) result.Insert(0, "value");
            return result.ToString();
        }

        private static string ReplaceIdentifiers(string code, Dictionary<string, string> names, HashSet<string> rewritten)
        {
            var output = new StringBuilder(code.Length);
            int i = 0;
            bool lineStart = true;
            while (i < code.Length)
            {
                char c = code[i];
                if (c == '\n') { output.Append(c); i++; lineStart = true; continue; }
                if (lineStart && (c == ' ' || c == '\t' || c == '\r')) { output.Append(c); i++; continue; }
                if (lineStart && c == '#')
                {
                    int end;
                    do
                    {
                        end = code.IndexOf('\n', i);
                        if (end < 0) end = code.Length;
                        int last = end - 1;
                        while (last >= i && (code[last] == ' ' || code[last] == '\t' || code[last] == '\r')) last--;
                        bool continued = last >= i && code[last] == '\\' && end < code.Length;
                        output.Append(code, i, end - i);
                        if (end < code.Length) output.Append('\n');
                        i = end < code.Length ? end + 1 : end;
                        if (!continued) break;
                    } while (i < code.Length);
                    lineStart = true;
                    continue;
                }
                lineStart = false;
                if (c == '/' && i + 1 < code.Length && code[i + 1] == '/')
                {
                    int end = code.IndexOf('\n', i);
                    if (end < 0) end = code.Length;
                    output.Append(code, i, end - i); i = end; continue;
                }
                if (c == '/' && i + 1 < code.Length && code[i + 1] == '*')
                {
                    int end = code.IndexOf("*/", i + 2, StringComparison.Ordinal);
                    if (end < 0) throw new FormatException("Unclosed comment in shader program.");
                    end += 2; output.Append(code, i, end - i); i = end; continue;
                }
                if (c == '"' || c == '\'')
                {
                    int start = i++; bool closed = false;
                    while (i < code.Length)
                    {
                        if (code[i] == '\\') { i += Math.Min(2, code.Length - i); continue; }
                        if (code[i++] == c) { closed = true; break; }
                    }
                    if (!closed) throw new FormatException("Unclosed string in shader program.");
                    output.Append(code, start, i - start); continue;
                }
                if (char.IsLetter(c) || c == '_')
                {
                    int start = i++;
                    while (i < code.Length && (char.IsLetterOrDigit(code[i]) || code[i] == '_')) i++;
                    string identifier = code.Substring(start, i - start);
                    if (names.TryGetValue(identifier, out string replacement))
                    {
                        output.Append(replacement);
                        rewritten.Add(identifier);
                    }
                    else output.Append(identifier);
                    continue;
                }
                output.Append(c); i++;
            }
            return output.ToString();
        }
    }
}
