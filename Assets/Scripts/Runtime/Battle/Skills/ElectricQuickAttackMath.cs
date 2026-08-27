using System;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public static class ElectricQuickAttackMath
    {
        public static decimal CalculateElectricBaseDamage(
            ElectricQuickAttackSkillAsset skill,
            decimal electric)
        {
            if (skill == null) throw new ArgumentNullException(nameof(skill));
            return SignedStatMath.ScaleFromBase(
                skill.ElectricBaseDamage,
                electric);
        }
    }
}
