using UnityEngine;
using TMPro;

namespace Pachimon.UI
{
    public sealed class RightPaneView : MonoBehaviour
    {
        [field: SerializeField] public TMP_Text TitleText { get; private set; }
        [field: SerializeField] public TMP_Text BodyText { get; private set; }

        public void Initialize(TMP_Text titleText, TMP_Text bodyText)
        {
            TitleText = titleText;
            BodyText = bodyText;
        }
    }
}
