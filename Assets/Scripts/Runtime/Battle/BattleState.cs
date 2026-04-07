using System.Collections.Generic;

namespace Pachimon.Battle
{
    public sealed class BattleState
    {
        public List<BattleUnit> Allies { get; } = new();
        public List<BattleUnit> Enemies { get; } = new();
        public List<string> LogEntries { get; } = new();

        public int TurnNumber { get; set; }

        public void AddLog(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            LogEntries.Add(message);
        }
    }
}
