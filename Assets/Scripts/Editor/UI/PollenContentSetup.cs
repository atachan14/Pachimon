using Pachimon.Battle;
using Pachimon.Skills;
using UnityEditor;
using UnityEngine;

namespace Pachimon.Editor.UI
{
    public static class PollenContentSetup
    {
        private const string StatusPath =
            "Assets/GameData/Battle/Status/PollenStatus.asset";
        private const string LeafSlicerPath =
            "Assets/GameData/Skill/Placeholder/Skill_003.asset";
        private const string SolarBeamPath =
            "Assets/GameData/Skill/Placeholder/Skill_027.asset";
        private const string ParalysisPowderPath =
            "Assets/GameData/Skill/Placeholder/Skill_043.asset";
        private const string BeatVinePath =
            "Assets/GameData/Battle/FieldEffect/BeatVineFieldEffect.asset";

        [InitializeOnLoadMethod]
        private static void SetupAfterReload() =>
            EditorApplication.delayCall += TryAutoSetup;

        [MenuItem("Tools/Pachimon/Data/Setup Pollen Content")]
        public static void Setup()
        {
            var pollen = GetOrCreate<PollenStatusAsset>(StatusPath);
            pollen.ConfigureForEditor(
                "\u82B1\u7C89",
                "Haste\u3092{value:totalValue}\u6E1B\u5C11\u3055\u305B\u3001"
                + "\u6BCEtick Value\u3092{value:decayPerTick}"
                + "\u6E1B\u5C11\u3055\u305B\u308B\u3002");

            var leafSlicer = AssetDatabase.LoadAssetAtPath<LeafSlicerSkillAsset>(
                LeafSlicerPath);
            var solarBeam = AssetDatabase.LoadAssetAtPath<SolarBeamSkillAsset>(
                SolarBeamPath);
            var paralysisPowder =
                AssetDatabase.LoadAssetAtPath<ParalysisPowderSkillAsset>(
                    ParalysisPowderPath);
            var beatVine = AssetDatabase.LoadAssetAtPath<BeatVineFieldEffectAsset>(
                BeatVinePath);

            leafSlicer?.ConfigurePollenForEditor(pollen, 50);
            solarBeam?.ConfigurePollenForEditor(pollen, 100);
            paralysisPowder?.ConfigurePollenForEditor(pollen, 50);
            beatVine?.ConfigurePollenForEditor(pollen, 50);

            EditorUtility.SetDirty(pollen);
            if (leafSlicer != null) EditorUtility.SetDirty(leafSlicer);
            if (solarBeam != null) EditorUtility.SetDirty(solarBeam);
            if (paralysisPowder != null) EditorUtility.SetDirty(paralysisPowder);
            if (beatVine != null) EditorUtility.SetDirty(beatVine);
            AssetDatabase.SaveAssets();
        }

        private static void TryAutoSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var pollen = AssetDatabase.LoadAssetAtPath<PollenStatusAsset>(StatusPath);
            var leafSlicer = AssetDatabase.LoadAssetAtPath<LeafSlicerSkillAsset>(
                LeafSlicerPath);
            var solarBeam = AssetDatabase.LoadAssetAtPath<SolarBeamSkillAsset>(
                SolarBeamPath);
            var paralysisPowder =
                AssetDatabase.LoadAssetAtPath<ParalysisPowderSkillAsset>(
                    ParalysisPowderPath);
            var beatVine = AssetDatabase.LoadAssetAtPath<BeatVineFieldEffectAsset>(
                BeatVinePath);
            if (pollen == null
                || leafSlicer?.PollenStatus == null
                || solarBeam?.PollenStatus == null
                || paralysisPowder?.PollenStatus == null
                || beatVine?.PollenStatus == null)
            {
                Setup();
            }
        }

        private static T GetOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
