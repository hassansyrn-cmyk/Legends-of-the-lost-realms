using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LostRealms {
    public static class AppIconConfig {
        [MenuItem("Lost Realms/Android/Configure App Icons")]
        public static void Apply() {
            const string iconPath = "Assets/Art/AppIcon/AppIcon.png";
            
            // Ensure texture importer is configured as a proper GUI/Sprite or default readable texture
            var importer = AssetImporter.GetAtPath(iconPath) as TextureImporter;
            if (importer != null) {
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.isReadable = true;
                importer.maxTextureSize = 512;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            AssetDatabase.Refresh();

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
            if (icon == null) {
                Debug.LogError("APP_ICON_FAILED: Could not load " + iconPath);
                return;
            }

            // Set default icon for all platforms
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new Texture2D[] { icon });

            // Set Android icons
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, new Texture2D[] { icon });

            AssetDatabase.SaveAssets();
            Debug.Log("APP_ICON_SUCCESS: Configured default and Android app icon to " + iconPath);
        }
    }
}
