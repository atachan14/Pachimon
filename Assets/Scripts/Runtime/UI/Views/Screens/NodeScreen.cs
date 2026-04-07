using UnityEngine;

namespace Pachimon.UI
{
    public abstract class NodeScreen : MonoBehaviour
    {
        [field: SerializeField] public string ScreenName { get; private set; }

        public void SetScreenName(string screenName)
        {
            ScreenName = screenName;
        }
    }
}
