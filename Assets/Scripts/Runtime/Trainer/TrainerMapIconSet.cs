using UnityEngine;

namespace Pachimon.Trainer
{
    [CreateAssetMenu(
        fileName = "TrainerMapIconSet",
        menuName = "Pachimon/Trainer Map Icon Set")]
    public sealed class TrainerMapIconSet : ScriptableObject
    {
        [SerializeField] private TrainerVisualLayers _layers;

        public TrainerVisualLayers Layers => _layers;

#if UNITY_EDITOR
        public void ConfigureForEditor(TrainerVisualLayers layers)
        {
            _layers = layers;
        }
#endif
    }
}
