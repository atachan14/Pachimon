using UnityEditor;
using UnityEngine;

namespace Pachimon.Editor.UI
{
    public static class SubStatIconImporterSetup
    {
        private const string Folder = "Assets/Resources/UI/SubStatIcons";

        [InitializeOnLoadMethod]
        private static void ConfigureAfterReload()
        {
            EditorApplication.delayCall += ConfigureAll;
        }

        [MenuItem("Tools/Pachimon/UI/Configure SubStat Icons")]
        public static void ConfigureAll()
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Folder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                {
                    continue;
                }

                var requiresUpdate = importer.textureType != TextureImporterType.Sprite
                    || importer.spriteImportMode != SpriteImportMode.Single
                    || importer.mipmapEnabled
                    || !importer.alphaIsTransparency
                    || importer.textureCompression != TextureImporterCompression.Uncompressed
                    || importer.filterMode != FilterMode.Bilinear
                    || importer.maxTextureSize != 256;
                if (!requiresUpdate)
                {
                    continue;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 256;
                importer.SaveAndReimport();
            }
        }
    }
}
