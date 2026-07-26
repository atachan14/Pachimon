using Pachimon.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.Editor.UI
{
    public static class HeaderPaletteSetup
    {
        private const string MenuPath = "Tools/Pachimon/UI/Apply Header Palette";

        [MenuItem(MenuPath)]
        private static void ApplyFromMenu()
        {
            var headers = Object.FindObjectsByType<HeaderView>(FindObjectsInactive.Include);
            if (headers.Length == 0)
            {
                Debug.LogError("HeaderView was not found in the open Scene.");
                return;
            }

            Undo.SetCurrentGroupName("Apply Header Palette");
            var undoGroup = Undo.GetCurrentGroup();
            foreach (var header in headers)
            {
                ApplyHeader(header);
            }

            EditorSceneManager.MarkSceneDirty(headers[0].gameObject.scene);
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = headers[0].gameObject;
            Debug.Log($"Applied Header palette to {headers.Length} HeaderView.", headers[0]);
        }

        private static void ApplyHeader(HeaderView header)
        {
            SetImageColor(header.gameObject, GameUiPalette.HeaderBackground);
            foreach (var text in header.GetComponentsInChildren<TMP_Text>(true))
            {
                SetTextColor(text, GameUiPalette.HeaderText);
            }
        }

        private static void SetImageColor(GameObject target, Color color)
        {
            if (target == null || !target.TryGetComponent<Image>(out var image)) return;
            Undo.RecordObject(image, "Apply Header Palette");
            image.color = color;
            EditorUtility.SetDirty(image);
        }

        private static void SetTextColor(TMP_Text text, Color color)
        {
            if (text == null) return;
            Undo.RecordObject(text, "Apply Header Palette");
            text.color = color;
            EditorUtility.SetDirty(text);
        }
    }
}
