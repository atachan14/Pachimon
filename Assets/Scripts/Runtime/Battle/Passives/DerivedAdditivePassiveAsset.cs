using System;
using System.Collections.Generic;
using Pachimon.Run;
using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(
        fileName = "DerivedAdditivePassive",
        menuName = "Pachimon/Passives/Derived Additive Passive")]
    public sealed class DerivedAdditivePassiveAsset : PassiveAsset
    {
        [SerializeField] private PachimonStatType _targetStat;
        [SerializeField] private PachimonStatType _referenceStat;
        [SerializeField, Min(0)] private int _percent;
        [SerializeField] private int _minimumContribution;

        public PachimonStatType TargetStat => _targetStat;

        public PachimonStatType ReferenceStat => _referenceStat;

        public int Percent => _percent;

        public decimal MinimumContribution => _minimumContribution;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_percent < 0)
            {
                errors.Add($"Passive {PassiveId}: percent cannot be negative.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int passiveId,
            string displayName,
            string description,
            PachimonStatType targetStat,
            PachimonStatType referenceStat,
            int percent,
            int minimumContribution)
        {
            ConfigureBaseForEditor(passiveId, displayName, description);
            _targetStat = targetStat;
            _referenceStat = referenceStat;
            _percent = percent;
            _minimumContribution = minimumContribution;
        }
#endif
    }
}
