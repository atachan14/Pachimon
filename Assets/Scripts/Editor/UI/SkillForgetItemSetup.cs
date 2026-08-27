using System.Linq;
using Pachimon.Items;
using UnityEditor;
using UnityEngine;

namespace Pachimon.Editor.UI
{
    public static class SkillForgetItemSetup
    {
        private const string CatalogPath = "Assets/GameData/Item/ItemCatalog.asset";
        private const string ItemPath = "Assets/GameData/Item/Item_004_SkillForget.asset";

        [InitializeOnLoadMethod]
        private static void ScheduleSetup()
        {
            EditorApplication.delayCall += TryAutoSetup;
        }

        [MenuItem("Tools/Pachimon/Data/Create Skill Forget Item")]
        public static void Setup()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(CatalogPath);
            if (catalog == null)
            {
                Debug.LogWarning("ItemCatalog is missing. Create the Item sample catalog first.");
                return;
            }

            var item = AssetDatabase.LoadAssetAtPath<SkillForgetItemAsset>(ItemPath);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<SkillForgetItemAsset>();
                AssetDatabase.CreateAsset(item, ItemPath);
                item.ConfigureForEditor(
                    ItemIds.SkillForget,
                    "技忘れマシン",
                    null,
                    "選択したパチモンから、選択した技を1つ忘れさせる。",
                    ItemCategory.SkillMachine,
                    500);
                EditorUtility.SetDirty(item);
            }

            if (!catalog.Items.Contains(item))
            {
                catalog.SetItemsForEditor(catalog.Items
                    .Where(candidate => candidate != null
                        && candidate.ItemId != ItemIds.SkillForget)
                    .Append(item)
                    .OrderBy(candidate => candidate.ItemId));
                EditorUtility.SetDirty(catalog);
            }

            AssetDatabase.SaveAssets();
        }

        private static void TryAutoSetup()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Setup();
            }
        }
    }
}
