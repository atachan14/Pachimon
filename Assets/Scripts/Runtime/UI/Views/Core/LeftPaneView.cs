using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System;
using Pachimon.Items;

namespace Pachimon.UI
{
    public sealed class LeftPaneView : MonoBehaviour
    {
        [field: SerializeField] public TMP_Text TitleText { get; private set; }
        [field: SerializeField] public TMP_Text BodyText { get; private set; }
        [field: SerializeField] public BattleNodeWindowView PartyWindow { get; private set; }

        public void Initialize(TMP_Text titleText, TMP_Text bodyText)
        {
            TitleText = titleText;
            BodyText = bodyText;
        }

        public void EnsurePartyWindow(BattleNodeWindowView template)
        {
            if (PartyWindow != null)
            {
                PartyWindow.SetVisualOrderReversed(true);
                return;
            }

            if (template == null)
            {
                Debug.LogError($"{nameof(LeftPaneView)} on '{name}' cannot create its party window because the template is missing.", this);
                return;
            }

            PartyWindow = Instantiate(template, transform, false);
            PartyWindow.name = "PlayerPartyWindow";
            var partyRect = PartyWindow.transform as RectTransform;
            if (partyRect != null)
            {
                partyRect.anchorMin = Vector2.zero;
                partyRect.anchorMax = Vector2.one;
                partyRect.offsetMin = Vector2.zero;
                partyRect.offsetMax = Vector2.zero;
            }

            PartyWindow.SetVisualOrderReversed(true);
            PartyWindow.gameObject.SetActive(true);
        }

        public void ShowPlayerParty(
            TrainerPreviewContent trainerPreview,
            IReadOnlyList<PachimonPreviewContent> pachimonPreviews)
        {
            PartyWindow?.Bind(trainerPreview, pachimonPreviews);
        }

        public void ConfigureItemDrop(
            Func<ItemInstance, int, bool> canUse,
            Func<ItemInstance, int, bool> tryUse)
        {
            var dropTarget = GetComponent<ItemDropTargetView>();
            if (dropTarget == null)
            {
                dropTarget = gameObject.AddComponent<ItemDropTargetView>();
            }

            dropTarget.Configure(
                item =>
                {
                    var partyIndex = (PartyWindow?.SelectedTabIndex ?? 0) - 1;
                    return partyIndex >= 0
                        && canUse != null
                        && canUse(item, partyIndex);
                },
                item =>
                {
                    var partyIndex = (PartyWindow?.SelectedTabIndex ?? 0) - 1;
                    return partyIndex >= 0
                        && tryUse != null
                        && tryUse(item, partyIndex);
                });
        }
    }
}
