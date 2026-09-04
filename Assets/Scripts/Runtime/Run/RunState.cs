using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Items;
using Pachimon.Reward;

namespace Pachimon.Run
{
    public sealed class RunState
    {
        public const int MaxPartySize = 3;

        private readonly List<string> _playerPachimonIds = new();

        public RunState(int runSeed, string playerName)
        {
            RunSeed = runSeed;
            PlayerName = string.IsNullOrWhiteSpace(playerName) ? "ゲスト" : playerName.Trim();
        }

        public int RunSeed { get; }

        public string PlayerName { get; }

        public int Gold { get; set; }

        public int BadgeCount { get; private set; }

        public TrainerModifierSet PlayerModifiers { get; } = new();

        public ItemInventory ItemInventory { get; } = new();

        public string CurrentNodeId { get; set; }

        public bool IsRunFinished { get; set; }

        public IReadOnlyList<string> PlayerPachimonIds => _playerPachimonIds;

        public bool IsPartyInitialized => _playerPachimonIds.Count > 0;

        public bool IsPartyFull => _playerPachimonIds.Count == MaxPartySize;

        public HashSet<string> ResolvedNodeIds { get; } = new();

        public bool TrySetInitialParty(IEnumerable<string> pachimonIds)
        {
            if (IsPartyInitialized || pachimonIds == null)
            {
                return false;
            }

            var ids = pachimonIds.ToArray();
            if (ids.Length != 1
                || ids.Any(string.IsNullOrWhiteSpace)
                || ids.Distinct(StringComparer.Ordinal).Count() != ids.Length)
            {
                return false;
            }

            _playerPachimonIds.AddRange(ids);
            return true;
        }

        public bool TryAddPartyMember(string pachimonId)
        {
            if (string.IsNullOrWhiteSpace(pachimonId)
                || IsPartyFull
                || _playerPachimonIds.Contains(pachimonId, StringComparer.Ordinal))
            {
                return false;
            }

            _playerPachimonIds.Add(pachimonId);
            return true;
        }

        public int GetBadgeCount(PachimonAttribute attribute)
        {
            return PlayerModifiers.GetBadgeCount(attribute);
        }

        public void AddBadge(PachimonAttribute attribute)
        {
            PlayerModifiers.AddBadge(attribute);
            BadgeCount = checked(BadgeCount + 1);
        }
    }
}
