using UnityEngine;
using TMPro;

namespace Pachimon.UI
{
    public sealed class MapOverlayView : MonoBehaviour
    {
        [field: SerializeField] public TMP_Text TitleText { get; private set; }
        [field: SerializeField] public TMP_Text BodyText { get; private set; }

        public bool IsOpen => gameObject.activeSelf;

        public void Initialize(TMP_Text titleText, TMP_Text bodyText)
        {
            TitleText = titleText;
            BodyText = bodyText;
        }

        public void Open()
        {
            gameObject.SetActive(true);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }
    }
}
