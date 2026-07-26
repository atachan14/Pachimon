using TMPro;
using UnityEngine;

namespace Pachimon.UI
{
    public sealed class SimpleNodeWindowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _detailsText;

        public void Configure(TMP_Text titleText, TMP_Text detailsText)
        {
            _titleText = titleText;
            _detailsText = detailsText;
        }

        public void Bind(string title, string details)
        {
            if (_titleText != null) _titleText.text = title;
            if (_detailsText != null) _detailsText.text = details;
        }
    }
}
