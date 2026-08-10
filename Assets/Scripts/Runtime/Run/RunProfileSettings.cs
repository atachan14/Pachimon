using UnityEngine;

namespace Pachimon.Run
{
    public enum EditorRunProfileSelection
    {
        Automatic,
        Production,
        Development,
    }

    [CreateAssetMenu(
        fileName = "RunProfileSettings",
        menuName = "Pachimon/Run/Profile Settings")]
    public sealed class RunProfileSettings : ScriptableObject
    {
        [SerializeField] private RunStartupProfile _productionProfile;
        [SerializeField] private RunStartupProfile _developmentProfile;
        [SerializeField] private EditorRunProfileSelection _editorSelection =
            EditorRunProfileSelection.Automatic;

        public RunStartupProfile ProductionProfile => _productionProfile;
        public RunStartupProfile DevelopmentProfile => _developmentProfile;

        public RunStartupProfile Resolve()
        {
#if UNITY_EDITOR
            return _editorSelection switch
            {
                EditorRunProfileSelection.Production => _productionProfile,
                EditorRunProfileSelection.Development => _developmentProfile,
                _ => _developmentProfile,
            };
#else
            return Debug.isDebugBuild
                ? _developmentProfile
                : _productionProfile;
#endif
        }
    }
}
