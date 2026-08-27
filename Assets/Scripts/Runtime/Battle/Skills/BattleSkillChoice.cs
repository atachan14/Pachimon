using System;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class BattleSkillChoice
    {
        public BattleSkillChoice(
            int slotId,
            SkillAsset skill,
            int upgradeLevel,
            int manaCost,
            bool isUsable,
            long cooldownReadyTick,
            bool isCooldownReady,
            bool hasEnoughMn)
        {
            if (slotId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(slotId));
            }

            if (cooldownReadyTick < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cooldownReadyTick));
            }

            if (upgradeLevel < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(upgradeLevel));
            }

            if (manaCost < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(manaCost));
            }

            SlotId = slotId;
            Skill = skill ?? throw new ArgumentNullException(nameof(skill));
            UpgradeLevel = upgradeLevel;
            ManaCost = manaCost;
            IsUsable = isUsable;
            CooldownReadyTick = cooldownReadyTick;
            IsCooldownReady = isCooldownReady;
            HasEnoughMn = hasEnoughMn;
        }

        public int SlotId { get; }
        public SkillAsset Skill { get; }
        public int UpgradeLevel { get; }
        public int ManaCost { get; }
        public string DisplayName => SkillUpgradeMath.FormatDisplayName(
            Skill.DisplayName,
            UpgradeLevel);
        public bool IsUsable { get; }
        public long CooldownReadyTick { get; }
        public bool IsCooldownReady { get; }
        public bool HasEnoughMn { get; }
    }
}
