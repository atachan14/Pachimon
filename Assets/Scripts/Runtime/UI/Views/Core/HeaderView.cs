using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public sealed class HeaderView : MonoBehaviour
    {
        [field: SerializeField] public TMP_Text GoldText { get; private set; }
        [field: FormerlySerializedAs("RowText")]
        [field: SerializeField] public TMP_Text StageText { get; private set; }
        [field: SerializeField] public TMP_Text BadgeText { get; private set; }
        [field: SerializeField] public Button MapButton { get; private set; }
        [field: SerializeField] public Button ItemButton { get; private set; }
        [field: SerializeField] public Button SettingsButton { get; private set; }

        public void Initialize(
            TMP_Text goldText,
            TMP_Text stageText,
            TMP_Text badgeText,
            Button mapButton,
            Button itemButton,
            Button settingsButton)
        {
            GoldText = goldText;
            StageText = stageText;
            BadgeText = badgeText;
            MapButton = mapButton;
            ItemButton = itemButton;
            SettingsButton = settingsButton;
        }

        private void Awake()
        {
            LogMissingReferences();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            LogMissingReferences();
        }
#endif

        private void LogMissingReferences()
        {
            var missing = new List<string>();

            if (GoldText == null) missing.Add(nameof(GoldText));
            if (StageText == null) missing.Add(nameof(StageText));
            if (BadgeText == null) missing.Add(nameof(BadgeText));
            if (MapButton == null) missing.Add(nameof(MapButton));
            if (ItemButton == null) missing.Add(nameof(ItemButton));
            if (SettingsButton == null) missing.Add(nameof(SettingsButton));

            if (missing.Count == 0)
            {
                return;
            }

            Debug.LogWarning($"{nameof(HeaderView)} on '{name}' is missing references: {string.Join(", ", missing)}", this);
        }
    }
}
