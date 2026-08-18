using System.Collections.Generic;
using System.Linq;
using Pachimon.Battle;
using Pachimon.Data;
using Pachimon.Passives;
using Pachimon.Reward;
using Pachimon.Skills;
using UnityEditor;
using UnityEngine;

namespace Pachimon.Editor.UI
{
    public static class InitialDescriptionTemplateSetup
    {
        private const string SkillCatalogPath =
            "Assets/GameData/Skill/SkillCatalog.asset";
        private const string PassiveCatalogPath =
            "Assets/GameData/Passive/PassiveCatalog.asset";
        private const string PassiveFolder = "Assets/GameData/Passive";

        [InitializeOnLoadMethod]
        private static void SetupAfterReload() =>
            EditorApplication.delayCall += TryAutoSetup;

        [MenuItem("Tools/Pachimon/Data/Setup Initial Description Templates")]
        public static void Setup()
        {
            var skillCatalog = AssetDatabase.LoadAssetAtPath<SkillCatalog>(
                SkillCatalogPath);
            var passiveCatalog = AssetDatabase.LoadAssetAtPath<PassiveCatalog>(
                PassiveCatalogPath);
            if (skillCatalog == null || passiveCatalog == null)
            {
                Debug.LogError("SkillCatalog and PassiveCatalog are required.");
                return;
            }

            for (var id = 1; id <= 8; id++)
            {
                var type = (AllocationType)id;
                if (skillCatalog.Get(id) is PlaceholderSkillAsset skill)
                {
                    skill.ConfigureForEditor(
                        skill.SkillId,
                        skill.DisplayName,
                        skill.AllocationType,
                        skill.IsMapAssignable,
                        skill.BaseRecoveryTicks,
                        skill.BaseCooldownTicks,
                        CreateSkillTemplate(type),
                        skill.BaseManaCost);
                    EditorUtility.SetDirty(skill);
                }

                var passive = GetOrCreate<OutgoingAttributeDamagePassiveAsset>(
                    $"{PassiveFolder}/Passive_{id:D3}_AttributeDamage.asset");
                passive.ConfigureForEditor(
                    id,
                    AttributePlaceholderName.FromCyclicId(id),
                    CreatePassiveTemplate(type),
                    (PachimonAttribute)(id - 1),
                    OutgoingAttributeDamagePassiveLogic.DefaultDamagePercent);
                EditorUtility.SetDirty(passive);
            }

            var passives = BuildUnique(passiveCatalog.Passives);
            for (var id = 1; id <= 8; id++)
            {
                passives[id] = AssetDatabase.LoadAssetAtPath<PassiveAsset>(
                    $"{PassiveFolder}/Passive_{id:D3}_AttributeDamage.asset");
            }

            passiveCatalog.SetPassivesForEditor(
                passives.Values.OrderBy(item => item.PassiveId));
            EditorUtility.SetDirty(passiveCatalog);
            AssetDatabase.SaveAssets();
            Debug.Log("Initial Skill/Passive description templates setup completed.");
        }

        private static void TryAutoSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            var skills = AssetDatabase.LoadAssetAtPath<SkillCatalog>(SkillCatalogPath);
            var passives = AssetDatabase.LoadAssetAtPath<PassiveCatalog>(PassiveCatalogPath);
            if (skills?.Get(1)?.Description?.Contains("{value:damage}") != true
                || passives?.Get(1) is not OutgoingAttributeDamagePassiveAsset)
            {
                Setup();
            }
        }

        private static string CreateSkillTemplate(AllocationType type)
        {
            var label = GetAttributeLabel(type);
            var text = "\u6575\u306E\u5148\u982D\u306B"
                + $"{{icon:{type}}}{{color:{type}}}{{value:damage}}{{/color}}"
                + $"\u306E{{term:{type}Damage|{label}\u30C0\u30E1\u30FC\u30B8}}"
                + "\u3092\u4E0E\u3048\u308B\u3002";
            return type switch
            {
                AllocationType.Electric => text
                    + "\u5024{value:statusValue}\u306E"
                    + "{term:Paralysis|\u9EBB\u75FA}\u3092\u4ED8\u4E0E\u3059\u308B\u3002",
                AllocationType.Poison => text
                    + "\u5024{value:statusValue}\u306E"
                    + "{term:Toxin|\u6BD2\u7D20}\u3092\u4ED8\u4E0E\u3059\u308B\u3002",
                AllocationType.Ice => text
                    + "\u5024{value:statusValue}\u306E"
                    + "{term:Chill|\u51B7\u6C17}\u3092\u4ED8\u4E0E\u3059\u308B\u3002",
                _ => text,
            };
        }

        private static string CreatePassiveTemplate(AllocationType type)
        {
            var label = GetAttributeLabel(type);
            return "\u4E0E\u3048\u308B"
                + $"{{icon:{type}}}{{term:{type}Damage|{label}\u30C0\u30E1\u30FC\u30B8}}"
                + "\u304C{value:increasePercent}%\u5897\u52A0\u3059\u308B\u3002";
        }

        private static string GetAttributeLabel(AllocationType type) => type switch
        {
            AllocationType.Fire => "\u708E",
            AllocationType.Aqua => "\u6C34",
            AllocationType.Leaf => "\u8349",
            AllocationType.Electric => "\u96FB\u6C17",
            AllocationType.Poison => "\u6BD2",
            AllocationType.Ice => "\u6C37",
            AllocationType.Wind => "\u98A8",
            AllocationType.Dragon => "\u7ADC",
            _ => string.Empty,
        };

        private static T GetOrCreate<T>(string path)
            where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static Dictionary<int, PassiveAsset> BuildUnique(
            IEnumerable<PassiveAsset> values) =>
            values.Where(value => value != null)
                .GroupBy(value => value.PassiveId)
                .ToDictionary(group => group.Key, group => group.First());
    }
}
