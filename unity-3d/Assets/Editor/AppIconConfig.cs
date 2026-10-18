using System;
using System.IO;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;

namespace LostRealms
{
    public static class AppIconConfig
    {
        private const string IconPath = "Assets/Art/AppIcon/AppIcon.png";
        private const string AdaptiveForegroundPath = "Assets/Art/AppIcon/AppIcon_AdaptiveForeground.png";

        [MenuItem("Lost Realms/Android/Configure App Icons")]
        public static void Apply()
        {
            ConfigureImporter(IconPath);
            ConfigureImporter(AdaptiveForegroundPath);
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            var transparentForeground = AssetDatabase.LoadAssetAtPath<Texture2D>(AdaptiveForegroundPath);
            if (icon == null)
            {
                throw new InvalidOperationException("APP_ICON_FAILED: Could not load " + IconPath);
            }

            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { icon });
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, new[] { icon });
            if (transparentForeground != null)
            {
                var adaptive = PlayerSettings.GetPlatformIcons(BuildTargetGroup.Android, AndroidPlatformIconKind.Adaptive);
                for (var i = 0; i < adaptive.Length; i++)
                    adaptive[i].SetTextures(new[] { icon, transparentForeground });
                PlayerSettings.SetPlatformIcons(BuildTargetGroup.Android, AndroidPlatformIconKind.Adaptive, adaptive);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            var report = "Default icon: " + IconPath + "\n";
            foreach (var kind in new[] { AndroidPlatformIconKind.Adaptive })
            {
                var slots = PlayerSettings.GetPlatformIcons(BuildTargetGroup.Android, kind);
                if (slots.Length == 0) throw new InvalidOperationException("Missing Android icon slots: " + kind);
                foreach (var slot in slots)
                    if (slot.GetTexture(0) != icon || (kind == AndroidPlatformIconKind.Adaptive && slot.GetTexture(1) != transparentForeground))
                        throw new InvalidOperationException("Incorrect Android icon assignment: " + kind);
                report += kind + ": " + slots.Length + " slots assigned\n";
            }
            Directory.CreateDirectory("Validation");
            File.WriteAllText("Validation/app-icon.txt", report + "APP_ICON_SUCCESS\n");
            Debug.Log("APP_ICON_SUCCESS: Configured default and adaptive Android icons from " + IconPath);
        }

        [MenuItem("Lost Realms/Android/Rebuild App Icon Sizes")]
        public static void RebuildSizes()
        {
            // Resize the supplied artwork without redrawing or changing its crop.
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!source.LoadImage(File.ReadAllBytes(IconPath))) throw new InvalidOperationException("Invalid app icon PNG");
                foreach (int size in new[] { 48, 72, 96, 144, 192, 512, 1024 })
                {
                    var target = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32);
                    var previous = RenderTexture.active;
                    var output = new Texture2D(size, size, TextureFormat.RGBA32, false);
                    try
                    {
                        Graphics.Blit(source, target);RenderTexture.active = target;
                        output.ReadPixels(new Rect(0, 0, size, size), 0, 0);output.Apply();
                        byte[] png = output.EncodeToPNG();
                        File.WriteAllBytes("Assets/Art/AppIcon/AppIcon_" + size + ".png", png);
                        if (size == 512)
                        {
                            const string wrapper = "../app/src/main/res/drawable-nodpi/";
                            if (Directory.Exists(wrapper))
                            {
                                File.WriteAllBytes(wrapper + "app_icon_premium.png", png);
                                File.WriteAllBytes(wrapper + "app_icon_launcher.png", png);
                            }
                        }
                    }
                    finally
                    {
                        RenderTexture.active = previous;RenderTexture.ReleaseTemporary(target);
                        UnityEngine.Object.DestroyImmediate(output);
                    }
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(source); }
            AssetDatabase.Refresh();Apply();
        }

        private static void ConfigureImporter(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.isReadable = true;
            importer.maxTextureSize = 512;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
}
