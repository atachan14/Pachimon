using UnityEngine;

namespace Pachimon.UI
{
    [CreateAssetMenu(
        fileName = "MapLayoutSettings",
        menuName = "Pachimon/UI/Map Layout Settings")]
    public sealed class MapLayoutSettingsAsset : ScriptableObject
    {
        [SerializeField] private MapLayoutSettings _settings = new();

        public MapLayoutSettings Settings => _settings ??= new MapLayoutSettings();
    }
}
