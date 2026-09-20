using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ShaderForgeConvert
{
    internal static class ShaderForgeConvertMenu
    {
        [MenuItem("Tools/Shader Forge Convert/Convert Selected Shader")]
        private static void ConvertSelected()
        {
            string sourcePath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(sourcePath) || !sourcePath.EndsWith(".shader", StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("Shader Forge Convert", "Select a Shader Forge .shader asset first.", "OK");
                return;
            }
            string createdPath = null;
            try
            {
                string input = File.ReadAllText(sourcePath);
                string suggestedName = Path.GetFileNameWithoutExtension(sourcePath) + ".Code";
                string targetPath = EditorUtility.SaveFilePanelInProject(
                    "Convert Shader Forge Shader", suggestedName, "shader", "Choose a new .shader path.");
                if (string.IsNullOrEmpty(targetPath)) return;
                if (string.Equals(sourcePath, targetPath, StringComparison.OrdinalIgnoreCase) || File.Exists(targetPath))
                    throw new IOException("The output path must be new and different from the source.");
                string originalName = ShaderName(input);
                string newName = originalName + "/Code";
                if (Shader.Find(newName) != null)
                    throw new IOException("The output Shader name already exists: " + newName);
                ConversionResult result = ShaderConverter.Convert(input, newName);
                using (var writer = new StreamWriter(new FileStream(targetPath, FileMode.CreateNew, FileAccess.Write)))
                    writer.Write(result.Source);
                createdPath = targetPath;
                AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceSynchronousImport);
                Shader imported = AssetDatabase.LoadAssetAtPath<Shader>(targetPath);
                if (imported == null || ShaderUtil.ShaderHasError(imported))
                {
                    string diagnostics = imported == null ? "Unity did not import a Shader asset." : string.Join("\n", Array.ConvertAll(ShaderUtil.GetShaderMessages(imported), m => m.message));
                    AssetDatabase.DeleteAsset(targetPath);
                    throw new InvalidOperationException("Output Shader failed to compile.\n" + diagnostics);
                }
                Selection.activeObject = imported;
                Debug.Log("Shader Forge Convert: partial conversion saved to " + targetPath +
                          ". Renamed " + result.RenamedNodes.Count + " traced node variables, " +
                          result.SampleVariablesRenamed + " texture samples; extracted " + result.ExtractedFunctions + " functions. " +
                          string.Join(" ", result.Warnings), imported);
                EditorUtility.DisplayDialog("Shader Forge Convert", "Partial conversion saved. Open the Console for the conversion status and review the generated code before using it.", "OK");
            }
            catch (Exception exception)
            {
                if (createdPath != null && File.Exists(createdPath)) AssetDatabase.DeleteAsset(createdPath);
                Debug.LogError("Shader Forge Convert: " + exception);
                EditorUtility.DisplayDialog("Shader Forge Convert", exception.Message, "OK");
            }
        }

        [MenuItem("Tools/Shader Forge Convert/Convert Selected Shader", true)]
        private static bool ValidateSelected()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            return !string.IsNullOrEmpty(path) && path.EndsWith(".shader", StringComparison.OrdinalIgnoreCase);
        }

        private static string ShaderName(string source)
        {
            var match = System.Text.RegularExpressions.Regex.Match(source, "(?m)^\\s*Shader\\s+\"([^\"]+)\"");
            if (!match.Success) throw new FormatException("Shader name was not found.");
            return match.Groups[1].Value;
        }
    }
}
