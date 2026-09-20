using System;
using System.IO;
using ShaderForgeConvert;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Copy this file into Assets/Editor of a disposable Unity project that has the package installed.
// Put Shader Forge source samples in Assets/Samples, then run -executeMethod ConversionBatchProbe.Run.
public static class ConversionBatchProbe
{
    public static void Run()
    {
        int converted = 0;
        int sourceErrors = 0;
        int outputErrors = 0;
        int conversionFailures = 0;
        int newErrors = 0;
        int contractFailures = 0;
        Directory.CreateDirectory("Assets/Converted");
        foreach (string sourcePath in Directory.GetFiles("Assets/Samples", "*.shader"))
        {
            string stem = Path.GetFileNameWithoutExtension(sourcePath);
            string outputPath = "Assets/Converted/" + stem + ".shader";
            try
            {
                AssetDatabase.ImportAsset(sourcePath, ImportAssetOptions.ForceSynchronousImport);
                Shader sourceShader = AssetDatabase.LoadAssetAtPath<Shader>(sourcePath);
                bool sourceHasError = sourceShader == null || ShaderUtil.ShaderHasError(sourceShader);
                if (sourceHasError) sourceErrors++;

                ConversionResult result = ShaderConverter.Convert(File.ReadAllText(sourcePath), "ProbeBatch/" + stem);
                File.WriteAllText(outputPath, result.Source);
                AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceSynchronousImport);
                Shader outputShader = AssetDatabase.LoadAssetAtPath<Shader>(outputPath);
                bool outputHasError = outputShader == null || ShaderUtil.ShaderHasError(outputShader);
                if (outputHasError) outputErrors++;
                if (outputHasError && !sourceHasError)
                {
                    newErrors++;
                    if (outputShader != null)
                        foreach (var message in ShaderUtil.GetShaderMessages(outputShader))
                            Debug.LogError("BATCH_DIAGNOSTIC " + stem + " " + message.message);
                }
                if (sourceShader != null && outputShader != null)
                    contractFailures += CompareMaterialContract(stem, sourceShader, outputShader);
                converted++;
                Debug.Log("BATCH_PROBE " + stem + " sourceError=" + sourceHasError +
                          " outputError=" + outputHasError + " nodes=" + result.TotalNodes +
                          " renamed=" + result.RenamedNodes.Count + " functions=" + result.ExtractedFunctions);
            }
            catch (Exception exception)
            {
                conversionFailures++;
                Debug.LogError("BATCH_FAILURE " + stem + " " + exception);
            }
        }
        Debug.Log("BATCH_SUMMARY converted=" + converted + " sourceErrors=" + sourceErrors +
                  " outputErrors=" + outputErrors + " conversionFailures=" + conversionFailures +
                  " newErrors=" + newErrors + " contractFailures=" + contractFailures);
        if (converted == 0 || conversionFailures != 0 || newErrors != 0 || contractFailures != 0)
            throw new Exception("Shader Forge Convert batch probe failed; inspect BATCH_* log entries.");
    }

    private static int CompareMaterialContract(string stem, Shader source, Shader output)
    {
        int differences = 0;
        void Compare(bool same, string field)
        {
            if (same) return;
            differences++;
            Debug.LogError("BATCH_CONTRACT " + stem + " differs at " + field);
        }

        Compare(source.GetPropertyCount() == output.GetPropertyCount(), "property count");
        int count = Math.Min(source.GetPropertyCount(), output.GetPropertyCount());
        var originalMaterial = new Material(source);
        var convertedMaterial = new Material(output);
        try
        {
            Compare(originalMaterial.renderQueue == convertedMaterial.renderQueue, "render queue");
            Compare(source.passCount == output.passCount, "pass count");
            for (int pass = 0; pass < Math.Min(source.passCount, output.passCount); pass++)
                Compare(originalMaterial.GetPassName(pass) == convertedMaterial.GetPassName(pass), "pass " + pass + " name");
            foreach (string tag in new[] { "Queue", "RenderType", "IgnoreProjector", "DisableBatching", "PreviewType" })
                Compare(originalMaterial.GetTag(tag, false) == convertedMaterial.GetTag(tag, false), "tag " + tag);

            for (int property = 0; property < count; property++)
            {
                string name = source.GetPropertyName(property);
                ShaderPropertyType type = source.GetPropertyType(property);
                Compare(name == output.GetPropertyName(property), "property " + property + " name");
                Compare(type == output.GetPropertyType(property), "property " + name + " type");
                if (name != output.GetPropertyName(property) || type != output.GetPropertyType(property)) continue;
                switch (type)
                {
                    case ShaderPropertyType.Color:
                        Compare(Same(originalMaterial.GetColor(name), convertedMaterial.GetColor(name)), name + " default color");
                        break;
                    case ShaderPropertyType.Vector:
                        Compare(Same(originalMaterial.GetVector(name), convertedMaterial.GetVector(name)), name + " default vector");
                        break;
                    case ShaderPropertyType.Texture:
                        Compare(originalMaterial.GetTextureScale(name) == convertedMaterial.GetTextureScale(name), name + " texture scale");
                        Compare(originalMaterial.GetTextureOffset(name) == convertedMaterial.GetTextureOffset(name), name + " texture offset");
                        Texture a = originalMaterial.GetTexture(name);
                        Texture b = convertedMaterial.GetTexture(name);
                        Compare((a == null ? null : a.name) == (b == null ? null : b.name), name + " default texture");
                        break;
                    default:
                        Compare(Mathf.Abs(originalMaterial.GetFloat(name) - convertedMaterial.GetFloat(name)) <= 0.00001f, name + " default number");
                        break;
                }
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(originalMaterial);
            UnityEngine.Object.DestroyImmediate(convertedMaterial);
        }
        return differences;
    }

    private static bool Same(Vector4 a, Vector4 b)
    {
        return Mathf.Abs(a.x - b.x) <= 0.00001f && Mathf.Abs(a.y - b.y) <= 0.00001f &&
               Mathf.Abs(a.z - b.z) <= 0.00001f && Mathf.Abs(a.w - b.w) <= 0.00001f;
    }
}
