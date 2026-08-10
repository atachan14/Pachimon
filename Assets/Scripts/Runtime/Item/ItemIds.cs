using System;

namespace Pachimon.Items
{
    public static class ItemIds
    {
        public const int Potion = 1;
        public const int Stone = 2;
        public const int SkillMachineItemBase = 10000;

        public static int GetSkillMachineItemId(int skillId)
        {
            return checked(SkillMachineItemBase + skillId);
        }

        public static int GetSkillMachineSkillId(int itemId)
        {
            if (!TryGetSkillMachineSkillId(itemId, out var skillId))
            {
                throw new ArgumentOutOfRangeException(nameof(itemId));
            }

            return skillId;
        }

        public static bool TryGetSkillMachineSkillId(
            int itemId,
            out int skillId)
        {
            skillId = itemId - SkillMachineItemBase;
            return skillId > 0;
        }
    }
}
