using Pachimon.Run;

namespace Pachimon.Battle
{
    public sealed class BattleController
    {
        public BattleState CreateDemoState()
        {
            var player = new BattleSideState(
                BattleSide.Player,
                new[]
                {
                    CreateDemoUnit("ally_1", "Mochi", BattleSide.Player, 0, 120, 120),
                    CreateDemoUnit("ally_2", "Puka", BattleSide.Player, 1, 100, 92),
                    CreateDemoUnit("ally_3", "Goro", BattleSide.Player, 2, 140, 140),
                });
            var enemy = new BattleSideState(
                BattleSide.Enemy,
                new[]
                {
                    CreateDemoUnit("enemy_1", "Bitebug", BattleSide.Enemy, 0, 90, 90),
                    CreateDemoUnit("enemy_2", "Shelln", BattleSide.Enemy, 1, 110, 110),
                    CreateDemoUnit("enemy_3", "Zapmew", BattleSide.Enemy, 2, 80, 80),
                });
            var state = new BattleState(12345, player, enemy);

            state.AddLog("Battle initialized.");
            state.AddLog("Enemy team spotted ahead.");
            state.AddLog("Mochi is ready to act.");
            return state;
        }

        public void RunDemoOpeningExchange(BattleState state)
        {
            if (state == null)
            {
                return;
            }

            var allyFront = state.Player.GetFrontLiving();
            var enemyFront = state.Enemy.GetFrontLiving();
            if (allyFront == null || enemyFront == null)
            {
                return;
            }

            enemyFront.ApplyDamage(18);
            state.AddLog($"{allyFront.DisplayName} used Ember Bite on {enemyFront.DisplayName} for 18 damage.");

            allyFront.ApplyDamage(11);
            state.AddLog($"{enemyFront.DisplayName} countered for 11 damage.");
        }

        private static BattleUnitState CreateDemoUnit(
            string instanceId,
            string displayName,
            BattleSide side,
            int slotIndex,
            int maxHp,
            int currentHp)
        {
            var values = new int[(int)PachimonStatType.Count];
            values[(int)PachimonStatType.MaxHp] = maxHp;
            values[(int)PachimonStatType.MaxMn] = 100;
            var stats = new EffectivePachimonStats(new PachimonStats(values, 1, 1), null);
            return new BattleUnitState(
                instanceId,
                slotIndex + 1,
                displayName,
                side,
                slotIndex,
                stats,
                currentHp,
                stats.MaxMn,
                new[] { new PachimonSkillSlot(1, slotIndex + 1) },
                new[] { slotIndex + 1 });
        }
    }
}
