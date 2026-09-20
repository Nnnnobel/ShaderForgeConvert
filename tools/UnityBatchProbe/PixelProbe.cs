using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class PixelProbe
{
    public static void Run()
    {
        const int size = 128;
        Shader source = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Samples/VertexAnimation.shader");
        Shader output = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Converted/VertexAnimation.shader");
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null || !source.isSupported || !output.isSupported)
            throw new Exception("Pixel test requires supported graphics shaders and a graphics device.");
        var sourceMaterial = new Material(source);
        var outputMaterial = new Material(output);
        var diffuse = new Texture2D(8, 8, TextureFormat.RGBA32, false);
        var normal = new Texture2D(8, 8, TextureFormat.RGBA32, false);
        for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++)
        {
            diffuse.SetPixel(x, y, ((x / 2 + y / 2) % 2 == 0) ? new Color(0.9f, 0.25f, 0.12f) : new Color(0.15f, 0.7f, 0.9f));
            normal.SetPixel(x, y, new Color(0.5f, 0.5f, 1, 1));
        }
        diffuse.Apply(); normal.Apply();
        sourceMaterial.SetTexture("_Diffuse", diffuse); outputMaterial.SetTexture("_Diffuse", diffuse);
        sourceMaterial.SetTexture("_Normals", normal); outputMaterial.SetTexture("_Normals", normal);
        var cameraObject = new GameObject("PixelProbeCamera");
        var lightObject = new GameObject("PixelProbeLight");
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var render = sphere.GetComponent<MeshRenderer>();
        var camera = cameraObject.AddComponent<Camera>();
        var light = lightObject.AddComponent<Light>();
        var target = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32);
        var read = new Texture2D(size, size, TextureFormat.RGBA32, false);
        try
        {
            cameraObject.transform.position = new Vector3(0, 0, -3);
            cameraObject.transform.LookAt(Vector3.zero);
            camera.orthographic = true;
            camera.orthographicSize = 1.05f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.targetTexture = target;
            camera.allowHDR = false;
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.shadows = LightShadows.None;
            lightObject.transform.rotation = Quaternion.Euler(25, -30, 0);
            foreach (float bulge in new[] { 0.1f, 0.4f })
            {
                sourceMaterial.SetFloat("_BulgeScale", bulge);
                outputMaterial.SetFloat("_BulgeScale", bulge);
                render.sharedMaterial = sourceMaterial;
                camera.Render();
                RenderTexture.active = target;
                read.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                read.Apply();
                Color32[] originalPixels = read.GetPixels32();
                render.sharedMaterial = outputMaterial;
                camera.Render();
                read.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                read.Apply();
                Color32[] convertedPixels = read.GetPixels32();
                int changed = 0, nonblack = 0, maxDelta = 0;
                for (int i = 0; i < originalPixels.Length; i++)
                {
                    Color32 a = originalPixels[i], b = convertedPixels[i];
                    if (a.r > 3 || a.g > 3 || a.b > 3) nonblack++;
                    int delta = Math.Max(Math.Max(Math.Abs(a.r - b.r), Math.Abs(a.g - b.g)), Math.Max(Math.Abs(a.b - b.b), Math.Abs(a.a - b.a)));
                    if (delta > 0) changed++;
                    maxDelta = Math.Max(maxDelta, delta);
                }
                Debug.Log("PIXEL_PROBE bulge=" + bulge + " device=" + SystemInfo.graphicsDeviceType + " nonblack=" + nonblack + " changed=" + changed + " maxDelta=" + maxDelta);
                if (nonblack < 2000 || maxDelta > 1) throw new Exception("Pixel comparison failed.");
            }
        }
        finally
        {
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(cameraObject);
            UnityEngine.Object.DestroyImmediate(lightObject);
            UnityEngine.Object.DestroyImmediate(sphere);
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(read);
            UnityEngine.Object.DestroyImmediate(diffuse);
            UnityEngine.Object.DestroyImmediate(normal);
            UnityEngine.Object.DestroyImmediate(sourceMaterial);
            UnityEngine.Object.DestroyImmediate(outputMaterial);
        }
    }
}
