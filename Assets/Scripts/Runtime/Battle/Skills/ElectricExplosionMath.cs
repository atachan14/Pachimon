using System;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public static class ElectricExplosionMath
    {
        public static decimal CalculateBaseDamage(
            ElectricExplosionSkillAsset skill,
            decimal electric,
            decimal fire)
        {
            if (skill == null) throw new ArgumentNullException(nameof(skill));

            return SignedStatMath.ScaleFromBase(
                    skill.BasePower,
                    electric,
                    skill.ElectricScalingPercent)
                * SignedStatMath.AmplificationMultiplier(
                    fire * skill.FireScalingPercent / 100m);
        }

        public static decimal CalculatePenetrationPercent(
            ElectricExplosionSkillAsset skill,
            decimal fire)
        {
            if (skill == null) throw new ArgumentNullException(nameof(skill));
            return fire * skill.PenetrationPercentAtFire100 / 100m;
        }
    }
}
