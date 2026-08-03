using Pachimon.Passives;
using UnityEditor;
using UnityEngine;

namespace Pachimon.Editor.UI
{
    public static class PassiveCatalogValidation
    {
        private const string CatalogPath =
            "Assets/GameData/Passive/PassiveCatalog.asset";

        [MenuItem("Tools/Pachimon/Data/Validate Passive Catalog")]
        private static void ValidateCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PassiveCatalog>(CatalogPath);
            if (catalog == null)
            {
                Debug.LogError($"PassiveCatalog is missing: {CatalogPath}");
                return;
            }

            var errors = catalog.ValidateContent();
            if (errors.Count == 0)
            {
                Debug.Log(
                    $"PassiveCatalog is valid: {catalog.Passives.Count} Passives.",
                    catalog);
                return;
            }

            Debug.LogError(
                "PassiveCatalog validation failed:\n" + string.Join("\n", errors),
                catalog);
        }
    }
}
