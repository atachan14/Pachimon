using System;
using Pachimon.Items;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Pachimon.UI
{
    public sealed class ItemDropTargetView : MonoBehaviour, IDropHandler
    {
        private Func<ItemInstance, bool> _tryUse;

        public void Configure(Func<ItemInstance, bool> tryUse)
        {
            _tryUse = tryUse;
        }

        public void OnDrop(PointerEventData eventData)
        {
            var slot = eventData.pointerDrag != null
                ? eventData.pointerDrag.GetComponent<ItemSlotView>()
                : null;
            if (slot?.ItemInstance != null)
            {
                _tryUse?.Invoke(slot.ItemInstance);
            }
        }
    }
}
