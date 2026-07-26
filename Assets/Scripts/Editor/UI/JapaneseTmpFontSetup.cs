using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Pachimon.Editor.UI
{
    public static class JapaneseTmpFontSetup
    {
        private const string MenuPath = "Tools/Pachimon/UI/Setup Japanese TMP Font";
        private const string SourceFontPath = "Assets/Fonts/NotoSansJP-VF.otf";
        private const string FontAssetPath = "Assets/Fonts/NotoSansJP Dynamic SDF.asset";
        private const string ValidationCharacters =
            "初期候補体選択進行現在解決済戦闘報酬回復街道具設定金所持属性火水風土光闇";

        [MenuItem(MenuPath)]
        private static void SetupJapaneseTmpFont()
        {
            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            if (sourceFont == null)
            {
                EditorUtility.DisplayDialog(
                    "Japanese TMP Font Setup",
                    $"Source font was not found at:\n{SourceFontPath}",
                    "OK");
                return;
            }

            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (fontAsset == null)
            {
                fontAsset = CreateDynamicFontAsset(sourceFont);
                if (fontAsset == null)
                {
                    EditorUtility.DisplayDialog(
                        "Japanese TMP Font Setup",
                        "TMP Font Assetの生成に失敗しました。Consoleを確認してください。",
                        "OK");
                    return;
                }
            }

            fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            fontAsset.isMultiAtlasTexturesEnabled = true;
            fontAsset.normalStyle = 0.75f;
            fontAsset.material.SetFloat(ShaderUtilities.ID_WeightNormal, fontAsset.normalStyle);
            fontAsset.TryAddCharacters(ValidationCharacters, out var missingCharacters);
            EditorUtility.SetDirty(fontAsset);

            var settings = TMP_Settings.instance;
            if (settings == null)
            {
                EditorUtility.DisplayDialog(
                    "Japanese TMP Font Setup",
                    "TMP Settings.assetが見つかりませんでした。",
                    "OK");
                return;
            }

            Undo.RecordObject(settings, "Register Japanese TMP Fallback");
            TMP_Settings.defaultFontAsset = fontAsset;
            TMP_Settings.fallbackFontAssets ??= new List<TMP_FontAsset>();
            if (!TMP_Settings.fallbackFontAssets.Contains(fontAsset))
            {
                TMP_Settings.fallbackFontAssets.Add(fontAsset);
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = fontAsset;

            var message = string.IsNullOrEmpty(missingCharacters)
                ? "Noto Sans JPをTMP Fallbackへ登録しました。"
                : $"Fallback登録は完了しましたが、次の文字を追加できませんでした:\n{missingCharacters}";
            Debug.Log(message, fontAsset);
            EditorUtility.DisplayDialog("Japanese TMP Font Setup", message, "OK");
        }

        private static TMP_FontAsset CreateDynamicFontAsset(Font sourceFont)
        {
            var fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                90,
                9,
                GlyphRenderMode.SDFAA,
                2048,
                2048,
                AtlasPopulationMode.Dynamic,
                true);
            if (fontAsset == null)
            {
                return null;
            }

            fontAsset.name = "NotoSansJP Dynamic SDF";
            var atlasTexture = fontAsset.atlasTextures[0];
            var material = fontAsset.material;
            atlasTexture.name = "NotoSansJP Dynamic SDF Atlas";
            material.name = "NotoSansJP Dynamic SDF Material";

            AssetDatabase.CreateAsset(fontAsset, FontAssetPath);
            AssetDatabase.AddObjectToAsset(atlasTexture, fontAsset);
            AssetDatabase.AddObjectToAsset(material, fontAsset);
            AssetDatabase.ImportAsset(FontAssetPath);
            return fontAsset;
        }
    }
}
