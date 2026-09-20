using System;
using System.IO;
using ShaderForgeConvert;
using UnityEditor;
using UnityEngine;

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
                  " newErrors=" + newErrors);
        if (converted == 0 || conversionFailures != 0 || newErrors != 0)
            throw new Exception("Shader Forge Convert batch probe failed; inspect BATCH_* log entries.");
    }
}
