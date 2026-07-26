using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Pachimon.Trainer
{
    [Serializable]
    public sealed class TrainerVisualLayers
    {
        [SerializeField] private Sprite _base;
        [FormerlySerializedAs("_tops")]
        [SerializeField] private Sprite _primary;
        [FormerlySerializedAs("_bottoms")]
        [SerializeField] private Sprite _secondary;
        [SerializeField] private Sprite _detail;

        public TrainerVisualLayers(Sprite baseSprite, Sprite primary, Sprite secondary, Sprite detail)
        {
            _base = baseSprite;
            _primary = primary;
            _secondary = secondary;
            _detail = detail;
        }

        public Sprite Base => _base;
        public Sprite Primary => _primary;
        public Sprite Secondary => _secondary;
        public Sprite Detail => _detail;
    }

}
