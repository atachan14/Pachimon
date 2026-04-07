namespace Pachimon.Battle
{
    public sealed class BattleController
    {
        public BattleState CreateDemoState()
        {
            var state = new BattleState
            {
                TurnNumber = 1,
            };

            state.Allies.Add(new BattleUnit("ally_1", "Mochi", 0, 120, 120, 18, false));
            state.Allies.Add(new BattleUnit("ally_2", "Puka", 1, 100, 92, 14, false));
            state.Allies.Add(new BattleUnit("ally_3", "Goro", 2, 140, 140, 9, false));

            state.Enemies.Add(new BattleUnit("enemy_1", "Bitebug", 0, 90, 90, 10, true));
            state.Enemies.Add(new BattleUnit("enemy_2", "Shelln", 1, 110, 110, 8, true));
            state.Enemies.Add(new BattleUnit("enemy_3", "Zapmew", 2, 80, 80, 22, true));

            state.AddLog("Battle initialized.");
            state.AddLog("Enemy team spotted ahead.");
            state.AddLog("Mochi is ready to act.");
            return state;
        }

        public void RunDemoOpeningExchange(BattleState state)
        {
            if (state == null || state.Allies.Count == 0 || state.Enemies.Count == 0)
            {
                return;
            }

            var allyFront = state.Allies[0];
            var enemyFront = state.Enemies[0];

            enemyFront.ApplyDamage(18);
            allyFront.ChangeMana(-4);
            state.AddLog($"{allyFront.DisplayName} used Ember Bite on {enemyFront.DisplayName} for 18 damage.");

            allyFront.ApplyDamage(11);
            state.AddLog($"{enemyFront.DisplayName} countered for 11 damage.");
        }
    }
}
