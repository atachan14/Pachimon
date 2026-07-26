using System;
using System.Collections.Generic;

namespace Pachimon.Battle
{
    public sealed class SeededEnemySkillSelector
    {
        private const uint EnemySkillStreamSalt = 0x41490001u;

        private readonly StableBattleRandom _random;

        public SeededEnemySkillSelector(int battleSeed)
        {
            _random = new StableBattleRandom(battleSeed, EnemySkillStreamSalt);
        }

        public int Select(IReadOnlyList<int> usableSkillIds)
        {
            if (usableSkillIds == null)
            {
                throw new ArgumentNullException(nameof(usableSkillIds));
            }

            if (usableSkillIds.Count == 0)
            {
                throw new ArgumentException(
                    "At least one usable Skill is required.",
                    nameof(usableSkillIds));
            }

            return usableSkillIds[_random.Next(usableSkillIds.Count)];
        }
    }
}
