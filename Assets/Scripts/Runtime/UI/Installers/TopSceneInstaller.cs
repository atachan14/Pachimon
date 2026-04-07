using Pachimon.App;
using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public sealed class TopSceneInstaller : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private GameObject _topRoot;
        [SerializeField] private Button _newGameButton;

        private void Awake()
        {
            ValidateReferences();
            WireButtons();
        }

        private void WireButtons()
        {
            if (_newGameButton == null)
            {
                return;
            }

            _newGameButton.onClick.RemoveAllListeners();
            _newGameButton.onClick.AddListener(SceneLoader.LoadGameScene);
        }

        private void ValidateReferences()
        {
            if (_topRoot == null)
            {
                Debug.LogWarning($"{nameof(TopSceneInstaller)} on '{name}' is missing TopRoot.", this);
            }

            if (_newGameButton == null)
            {
                Debug.LogWarning($"{nameof(TopSceneInstaller)} on '{name}' is missing NewGameButton.", this);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ValidateReferences();
        }
#endif
    }
}
