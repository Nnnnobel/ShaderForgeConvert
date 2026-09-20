using ShaderForgeConvert;

const string fixture = """
// Shader created with Shader Forge v1.38
// Shader Forge (c) Freya Holmer
// Note: Manually altering this data may prevent you from opening it in Shader Forge
/*SF_DATA;ver:1.38;sub:START;pass:START;n:type:ShaderForge.SFN_Final,id:1|emission-2-OUT;n:type:ShaderForge.SFN_Power,id:2,cmnt:Panning gradient|VAL-3-OUT;n:type:ShaderForge.SFN_Vector1,id:3,v1:1;pass:END;sub:END;*/
Shader "Test/Original" {
SubShader { Pass {
CGPROGRAM
float node_2 = pow(0.5, 2.0); // node_2 should remain in comment
float4 frag() : SV_Target { return node_2; }
ENDCG
} }
CustomEditor "ShaderForgeMaterialInspector"
}
""";

void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

ForgeGraph graph = ForgeGraphReader.Read(fixture);
Check(graph.Nodes.Count == 3, "Graph node count");
Check(graph.ReachableNodes().SetEquals(new[] { 2, 3 }), "Reachability");
ConversionResult output = ShaderConverter.Convert(fixture, "Test/Code");
Check(output.Source.Contains("float panningGradient = pow"), "Semantic rename");
Check(output.Source.Contains("return panningGradient"), "References renamed");
Check(output.Source.Contains("// node_2 should remain in comment"), "Comment remains untouched");
Check(output.Source.Contains("// Shader Forge node 2: Panning gradient"), "Logic annotation");
Check(!output.Source.Contains("SF_DATA"), "Graph metadata removed");
Check(!output.Source.Contains("ShaderForgeMaterialInspector"), "Inspector dependency removed");
Check(output.IsPartial, "MVP reports partial conversion");
bool rejectedBrokenData = false;
try { ForgeGraphReader.Read(fixture.Replace("pass:END;sub:END;*/", "pass:END;sub:END;")); }
catch (FormatException) { rejectedBrokenData = true; }
Check(rejectedBrokenData, "Unclosed graph data rejected");
bool rejectedSameName = false;
try { ShaderConverter.Convert(fixture, "Test/Original"); }
catch (ArgumentException) { rejectedSameName = true; }
Check(rejectedSameName, "Shader name collision rejected");

string sourcePath = Environment.GetEnvironmentVariable("SHADER_FORGE_SAMPLE");
if (!string.IsNullOrEmpty(sourcePath))
{
    string sample = File.ReadAllText(sourcePath);
    ForgeGraph realGraph = ForgeGraphReader.Read(sample);
    ConversionResult real = ShaderConverter.Convert(sample, "Shader Forge Convert/Probe");
    Check(realGraph.Nodes.Count > 0, "Real graph parsed");
    Check(real.Source.Contains("Shader \"Shader Forge Convert/Probe\""), "Real Shader renamed");
    if (Path.GetFileName(sourcePath).StartsWith("PresetParticleAdditive", StringComparison.Ordinal))
    {
        Check(real.ExtractedFunctions == 1 && real.Source.Contains("ComputeEmission(i)") && real.Source.Contains("mainTexSample"), "Emission function extraction");
        Check(real.Source.Contains("Blend One One") && real.Source.Contains("ZWrite Off") && real.Source.Contains("UNITY_APPLY_FOG_COLOR"), "Render state and fog retained");
    }
    Console.WriteLine($"Real sample: {realGraph.Nodes.Count} nodes, {real.RenamedNodes.Count} semantic names, {real.SampleVariablesRenamed} sampled variables, {real.ExtractedFunctions} extracted functions");
}

Console.WriteLine("Smoke tests passed");
