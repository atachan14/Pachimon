using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Battle
{
    public sealed class DragonDanceRuntimeData
    {
        public DragonDanceRuntimeData(int dragonBonus, int speedBonus)
        {
            DragonBonus = dragonBonus;
            SpeedBonus = speedBonus;
        }

        public int DragonBonus { get; }
        public int SpeedBonus { get; }
    }

    [CreateAssetMenu(fileName = "DragonDanceStatus", menuName = "Pachimon/Battle/Status/Dragon Dance")]
    public sealed class DragonDanceStatusAsset : BattleStatusAsset
    {
        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (StatusId != BattleStatusId.DragonDance)
                errors?.Add("Dragon Dance Definition must use DragonDance ID.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(string displayName, string description) =>
            ConfigureDefinitionForEditor(
                BattleStatusId.DragonDance,
                displayName,
                description);
#endif
    }
}
