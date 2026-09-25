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
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            var transparentForeground = AssetDatabase.LoadAssetAtPath<Texture2D>(AdaptiveForegroundPath);
            if (icon == null)
            {
                Debug.LogError("APP_ICON_FAILED: Could not load " + IconPath);
                return;
            }

            ConfigureImporter(IconPath);
            ConfigureImporter(AdaptiveForegroundPath);

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
            Debug.Log("APP_ICON_SUCCESS: Configured legacy, round, and adaptive Android icons from " + IconPath);
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
