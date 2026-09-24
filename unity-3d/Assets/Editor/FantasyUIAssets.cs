using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

/// <summary>
/// Creates the persistent, build-time Fantasy UI dependencies. Runtime code can load the
/// resulting fonts with Resources.Load&lt;TMP_FontAsset&gt;("FantasyUI/Fonts/Heading SDF")
/// and Resources.Load&lt;TMP_FontAsset&gt;("FantasyUI/Fonts/Body SDF"). The sprite atlas is
/// included in builds but intentionally is not a Resources asset; sprites retain their
/// existing Resources paths below FantasyUI.
/// </summary>
public static class FantasyUIAssets
{
    private const string FontsFolder = "Assets/Resources/FantasyUI/Fonts";
    private const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
    private const string AtlasFolder = "Assets/FantasyUI";
    private const string AtlasPath = AtlasFolder + "/FantasyUI.spriteatlas";
    private const string SpriteFolder = "Assets/Resources/FantasyUI";
    private const int FontAtlasSize = 2048;
    private const string RequiredHudSymbols = "•×←↑→↓";
    private static bool s_ImportRequested;
    private static bool s_Preparing;

    [InitializeOnLoadMethod]
    private static void ScheduleInitialPreparation()
    {
        EditorApplication.delayCall -= PrepareAfterLoad;
        EditorApplication.delayCall += PrepareAfterLoad;
    }

    private static void PrepareAfterLoad()
    {
        Prepare();
    }

    [MenuItem("Tools/Fantasy UI/Prepare Assets")]
    public static void Prepare()
    {
        if (s_Preparing)
            return;

        s_Preparing = true;
        try
        {
            if (!File.Exists(TmpSettingsPath))
            {
                RequestTmpEssentials();
                // A non-interactive package import normally completes synchronously. Force
                // the AssetDatabase to observe its result so a first build can continue.
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                if (!File.Exists(TmpSettingsPath))
                    return;
            }

            EnsureFontAsset("Body", true);
            EnsureFontAsset("Heading", false);
            EnsureHeadingFallback();
            EnsureSpriteAtlas();
            AssetDatabase.SaveAssets();
        }
        finally
        {
            s_Preparing = false;
        }
    }

    private static void RequestTmpEssentials()
    {
        if (s_ImportRequested)
            return;

        s_ImportRequested = true;
        AssetDatabase.importPackageCompleted -= OnPackageImported;
        AssetDatabase.importPackageCompleted += OnPackageImported;
        TMP_PackageResourceImporter.ImportResources(true, false, false);
    }

    private static void OnPackageImported(string packageName)
    {
        if (!string.Equals(packageName, "TMP Essential Resources", StringComparison.Ordinal))
            return;

        AssetDatabase.importPackageCompleted -= OnPackageImported;
        s_ImportRequested = false;
        EditorApplication.delayCall -= Prepare;
        EditorApplication.delayCall += Prepare;
    }

    private static void EnsureFontAsset(string name, bool requireAllHudGlyphs)
    {
        string assetPath = FontsFolder + "/" + name + " SDF.asset";
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (existing != null)
        {
            if (existing.atlasWidth >= FontAtlasSize && (!requireAllHudGlyphs || HasAllHudGlyphs(existing)))
                return;

            // Replace only assets made with an obsolete baker configuration.
            if (!AssetDatabase.DeleteAsset(assetPath))
                throw new BuildFailedException("Could not replace obsolete Fantasy UI font asset: " + assetPath);
        }

        string sourcePath = FontsFolder + "/" + name + ".ttf";
        Font source = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
        if (source == null)
            throw new BuildFailedException("Fantasy UI source font is missing or failed to import: " + sourcePath);

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            source,
            64,
            9,
            GlyphRenderMode.SDFAA,
            FontAtlasSize,
            FontAtlasSize,
            AtlasPopulationMode.Dynamic,
            false);

        if (fontAsset == null)
            throw new BuildFailedException("TextMesh Pro failed to create font asset from " + sourcePath);

        fontAsset.name = name + " SDF";
        string characters = BuildCharacterSet();
        if (!fontAsset.TryAddCharacters(characters, out string missingCharacters))
            Debug.LogWarning(name + " SDF does not contain all requested Latin glyphs: " + missingCharacters);

        if (requireAllHudGlyphs)
        {
            StringBuilder missingRequired = new StringBuilder();
            for (int codePoint = 32; codePoint <= 126; codePoint++)
            {
                if (!fontAsset.HasCharacter(codePoint))
                    missingRequired.Append((char)codePoint);
            }
            for (int i = 0; i < RequiredHudSymbols.Length; i++)
            {
                char character = RequiredHudSymbols[i];
                if (!fontAsset.HasCharacter(character, false, false))
                    missingRequired.Append(character);
            }

            if (missingRequired.Length > 0)
            {
                Object.DestroyImmediate(fontAsset);
                throw new BuildFailedException(
                    name + " font is missing required HUD glyphs: " + missingRequired);
            }
        }

        fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
        AssetDatabase.CreateAsset(fontAsset, assetPath);

        Texture2D[] atlases = fontAsset.atlasTextures;
        if (atlases != null)
        {
            for (int i = 0; i < atlases.Length; i++)
            {
                Texture2D atlas = atlases[i];
                if (atlas == null || AssetDatabase.Contains(atlas))
                    continue;

                atlas.name = name + " SDF Atlas" + (i == 0 ? string.Empty : " " + i);
                AssetDatabase.AddObjectToAsset(atlas, fontAsset);
            }
        }

        if (fontAsset.material != null && !AssetDatabase.Contains(fontAsset.material))
        {
            fontAsset.material.name = name + " SDF Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }

        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
    }

    private static string BuildCharacterSet()
    {
        StringBuilder characters = new StringBuilder(191 + RequiredHudSymbols.Length);
        for (int codePoint = 32; codePoint <= 126; codePoint++)
            characters.Append((char)codePoint);
        for (int codePoint = 160; codePoint <= 255; codePoint++)
            characters.Append((char)codePoint);
        characters.Append(RequiredHudSymbols);
        return characters.ToString();
    }

    private static bool HasAllHudGlyphs(TMP_FontAsset fontAsset)
    {
        for (int codePoint = 32; codePoint <= 126; codePoint++)
        {
            if (!fontAsset.HasCharacter(codePoint))
                return false;
        }

        for (int i = 0; i < RequiredHudSymbols.Length; i++)
        {
            if (!fontAsset.HasCharacter(RequiredHudSymbols[i], false, false))
                return false;
        }

        return true;
    }

    private static void EnsureHeadingFallback()
    {
        TMP_FontAsset heading = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontsFolder + "/Heading SDF.asset");
        TMP_FontAsset body = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontsFolder + "/Body SDF.asset");
        if (heading == null || body == null)
            throw new BuildFailedException("Fantasy UI font assets must exist before configuring fallback.");

        List<TMP_FontAsset> fallbacks = heading.fallbackFontAssetTable;
        if (fallbacks != null && fallbacks.Contains(body))
            return;

        if (fallbacks == null)
        {
            fallbacks = new List<TMP_FontAsset>();
            heading.fallbackFontAssetTable = fallbacks;
        }

        fallbacks.Add(body);
        EditorUtility.SetDirty(heading);
    }

    private static void EnsureSpriteAtlas()
    {
        if (AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AtlasPath) != null)
            return;

        if (!AssetDatabase.IsValidFolder(AtlasFolder))
            AssetDatabase.CreateFolder("Assets", "FantasyUI");

        Object spriteDirectory = AssetDatabase.LoadAssetAtPath<Object>(SpriteFolder);
        if (spriteDirectory == null)
            throw new BuildFailedException("Fantasy UI sprite folder is missing: " + SpriteFolder);

        SpriteAtlas atlas = new SpriteAtlas();
        atlas.name = "FantasyUI";
        atlas.SetIncludeInBuild(true);
        atlas.SetPackingSettings(new SpriteAtlasPackingSettings
        {
            blockOffset = 1,
            enableRotation = false,
            enableTightPacking = false,
            padding = 4
        });
        atlas.SetTextureSettings(new SpriteAtlasTextureSettings
        {
            readable = false,
            generateMipMaps = false,
            sRGB = true,
            filterMode = FilterMode.Bilinear
        });
        atlas.SetPlatformSettings(new TextureImporterPlatformSettings
        {
            name = "DefaultTexturePlatform",
            maxTextureSize = 2048,
            overridden = false,
            textureCompression = TextureImporterCompression.Compressed
        });
        atlas.Add(new[] { spriteDirectory });
        AssetDatabase.CreateAsset(atlas, AtlasPath);
    }

    public sealed class FantasyUIPreprocessBuild : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            Prepare();

            // Generated TMP atlases and the sprite atlas are editor conveniences. In a
            // clean batch-mode checkout TMP Essentials may still be importing when this
            // callback runs; FantasyUI.LoadFont has a runtime source-font fallback and
            // the individual Resources sprites are already included in the player.
            string headingSource = FontsFolder + "/Heading.ttf";
            string bodySource = FontsFolder + "/Body.ttf";
            if (AssetDatabase.LoadAssetAtPath<Font>(headingSource) == null
                || AssetDatabase.LoadAssetAtPath<Font>(bodySource) == null)
            {
                throw new BuildFailedException(
                    "Fantasy UI source fonts are missing: " + headingSource + " and " + bodySource);
            }

            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontsFolder + "/Heading SDF.asset") == null
                || AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontsFolder + "/Body SDF.asset") == null)
                Debug.LogWarning("Fantasy UI TMP SDF assets are not available yet; runtime source-font fallback will be used.");
        }
    }
}
