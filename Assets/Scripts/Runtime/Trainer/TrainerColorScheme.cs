using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Pachimon.Trainer
{
    [Serializable]
    public struct TrainerColorScheme
    {
        [FormerlySerializedAs("_topsColor")]
        [SerializeField] private Color _primaryColor;
        [FormerlySerializedAs("_bottomsColor")]
        [SerializeField] private Color _secondaryColor;

        public TrainerColorScheme(Color primaryColor, Color secondaryColor)
        {
            _primaryColor = primaryColor;
            _secondaryColor = secondaryColor;
        }

        public Color PrimaryColor => _primaryColor;
        public Color SecondaryColor => _secondaryColor;
    }
}
