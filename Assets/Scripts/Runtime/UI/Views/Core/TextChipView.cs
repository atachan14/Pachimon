using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public sealed class TextChipView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;

        public void Configure(TMP_Text label) => _label = label;

        public void Bind(string label)
        {
            Bind(label, null);
        }

        public void Bind(string label, Action onClicked)
        {
            if (_label != null) _label.text = label;

            var button = GetComponent<Button>();
            if (onClicked == null)
            {
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    button.interactable = false;
                }

                return;
            }

            button ??= gameObject.AddComponent<Button>();
            button.targetGraphic = GetComponent<Graphic>();
            button.transition = Selectable.Transition.ColorTint;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClicked());
            button.interactable = true;
        }
    }
}
