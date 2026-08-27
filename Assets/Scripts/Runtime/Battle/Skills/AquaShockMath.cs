using System;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public static class AquaShockMath
    {
        public static decimal CalculateElectricBaseDamage(
            AquaShockSkillAsset skill,
            decimal electric)
        {
            if (skill == null) throw new ArgumentNullException(nameof(skill));
            return SignedStatMath.ScaleFromBase(
                skill.ElectricBaseDamage,
                electric);
        }

        public static decimal CalculateAquaBaseDamage(
            AquaShockSkillAsset skill,
            decimal aqua,
            decimal aquaScalingPercent = 100m)
        {
            if (skill == null) throw new ArgumentNullException(nameof(skill));
            return SignedStatMath.ScaleFromBase(
                skill.AquaBaseDamage,
                aqua,
                aquaScalingPercent);
        }

        public static int CalculateLeakValue(
            AquaShockSkillAsset skill,
            decimal aqua,
            decimal aquaScalingPercent = 100m)
        {
            if (skill == null) throw new ArgumentNullException(nameof(skill));
            return SignedStatMath.FloorNonNegative(
                SignedStatMath.ScaleFromBase(
                    skill.LeakBaseValue,
                    aqua,
                    aquaScalingPercent));
        }
    }
}
