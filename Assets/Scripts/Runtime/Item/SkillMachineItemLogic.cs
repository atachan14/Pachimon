using System;

namespace Pachimon.Items
{
    public sealed class SkillMachineItemLogic : IItemLogic
    {
        public ItemUseFailureReason CanUse(
            ItemAsset item,
            ItemUseContext context)
        {
            if (item is not SkillMachineItemAsset skillMachine)
            {
                throw new ArgumentException(
                    "SkillMachineItemLogic requires a SkillMachineItemAsset.",
                    nameof(item));
            }

            if (context == null) throw new ArgumentNullException(nameof(context));
            if (context.Affiliation != ItemTargetAffiliation.Ally
                || context.RunTarget == null
                || skillMachine.Skill == null)
            {
                return ItemUseFailureReason.InvalidTarget;
            }

            if (!context.RunTarget.CanAddSkill
                || (context.Kind == ItemUseContextKind.Battle
                    && !context.BattleTarget.CanAddSkill))
            {
                return ItemUseFailureReason.SkillSlotsFull;
            }

            return ItemUseFailureReason.None;
        }

        public int Apply(ItemAsset item, ItemUseContext context)
        {
            if (item is not SkillMachineItemAsset skillMachine)
            {
                throw new ArgumentException(
                    "SkillMachineItemLogic requires a SkillMachineItemAsset.",
                    nameof(item));
            }

            if (!context.RunTarget.AddSkill(skillMachine.SkillId))
            {
                return 0;
            }

            if (context.Kind == ItemUseContextKind.Battle
                && !context.BattleTarget.AddSkill(skillMachine.SkillId))
            {
                throw new InvalidOperationException(
                    "Battle Unit could not receive a Skill already added "
                    + "to its Run Instance.");
            }

            return 1;
        }
    }
}
