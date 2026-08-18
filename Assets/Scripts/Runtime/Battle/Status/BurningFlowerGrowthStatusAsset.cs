using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(
        fileName = "BurningFlowerGrowthStatus",
        menuName = "Pachimon/Battle/Status/Burning Flower Growth")]
    public sealed class BurningFlowerGrowthStatusAsset : BattleStatusAsset
    {
        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (StatusId is not (
                BattleStatusId.BurningFlowerLeaf
                or BattleStatusId.BurningFlowerFire))
            {
                errors?.Add(
                    "Burning Flower Growth must use a matching growth ID.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            BattleStatusId statusId,
            string displayName,
            string description)
        {
            if (statusId is not (
                BattleStatusId.BurningFlowerLeaf
                or BattleStatusId.BurningFlowerFire))
            {
                throw new ArgumentOutOfRangeException(nameof(statusId));
            }
            ConfigureDefinitionForEditor(statusId, displayName, description);
        }
#endif
    }
}
