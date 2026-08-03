using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Run;

namespace Pachimon.Reward
{
    public enum BattleRewardSlot
    {
        Gold = 0,
        Secondary = 1,
        Passive = 2,
        Skill = 3,
    }

    public sealed class BattleRewardSession
    {
        public const int MaximumSkillCount = 9;

        private readonly RunState _runState;
        private readonly RunPachimonPool _pachimonPool;
        private readonly NodeReward _nodeReward;
        private readonly ModValueSettings _settings;
        private readonly PassiveStatModifierRegistry _passiveStatModifierRegistry;
        private readonly PachimonInstance[] _playerParty;
        private readonly HashSet<BattleRewardSlot> _claimedSlots = new();

        public BattleRewardSession(
            RunState runState,
            RunPachimonPool pachimonPool,
            NodeReward nodeReward,
            PassiveStatModifierRegistry passiveStatModifierRegistry,
            ModValueSettings settings = null)
        {
            _runState = runState ?? throw new ArgumentNullException(nameof(runState));
            _pachimonPool = pachimonPool
                ?? throw new ArgumentNullException(nameof(pachimonPool));
            _nodeReward = nodeReward
                ?? throw new ArgumentNullException(nameof(nodeReward));
            _passiveStatModifierRegistry = passiveStatModifierRegistry
                ?? throw new ArgumentNullException(nameof(passiveStatModifierRegistry));
            _settings = settings != null ? settings : ModValueSettings.RuntimeDefault;
            _playerParty = runState.PlayerPachimonIds
                .Select(GetRequiredPachimon)
                .ToArray();

            if (_playerParty.Length != RunState.PartySize)
            {
                throw new InvalidOperationException(
                    $"Battle Reward requires a {RunState.PartySize}-Pachimon Player party.");
            }
        }

        public NodeReward NodeReward => _nodeReward;

        public bool UsesBadge => _nodeReward.BadgeAttribute.HasValue;

        public bool IsComplete => _claimedSlots.Count == 4;

        public bool IsClaimed(BattleRewardSlot slot)
        {
            return _claimedSlots.Contains(slot);
        }

        public bool ClaimGold()
        {
            if (IsClaimed(BattleRewardSlot.Gold))
            {
                return false;
            }

            _runState.Gold = checked(_runState.Gold + _nodeReward.Gold);
            _claimedSlots.Add(BattleRewardSlot.Gold);
            return true;
        }

        public bool ClaimSecondary()
        {
            if (IsClaimed(BattleRewardSlot.Secondary))
            {
                return false;
            }

            if (_nodeReward.BadgeAttribute is PachimonAttribute badgeAttribute)
            {
                _runState.AddBadge(badgeAttribute);
                _claimedSlots.Add(BattleRewardSlot.Secondary);
                return true;
            }

            ApplyRewardElement(_nodeReward.FirstElement, isSecondSlot: false);
            ApplyRewardElement(_nodeReward.SecondElement, isSecondSlot: true);

            _claimedSlots.Add(BattleRewardSlot.Secondary);
            return true;
        }

        public bool CanGrantSkill(int skillId, string targetInstanceId)
        {
            var target = _pachimonPool.Get(targetInstanceId);
            return skillId > 0
                && IsPlayerPartyMember(target)
                && target.SkillIds.Count < MaximumSkillCount;
        }

        public bool GrantSkill(int skillId, string targetInstanceId)
        {
            if (IsClaimed(BattleRewardSlot.Skill)
                || !CanGrantSkill(skillId, targetInstanceId))
            {
                return false;
            }

            var target = _pachimonPool.Get(targetInstanceId);
            if (!target.AddSkill(skillId))
            {
                return false;
            }

            _claimedSlots.Add(BattleRewardSlot.Skill);
            return true;
        }

        public bool CanGrantPassive(int passiveId, string targetInstanceId)
        {
            var target = _pachimonPool.Get(targetInstanceId);
            return passiveId > 0
                && IsPlayerPartyMember(target)
                && !target.PassiveIds.Contains(passiveId);
        }

        public bool GrantPassive(int passiveId, string targetInstanceId)
        {
            if (IsClaimed(BattleRewardSlot.Passive)
                || !CanGrantPassive(passiveId, targetInstanceId))
            {
                return false;
            }

            var target = _pachimonPool.Get(targetInstanceId);
            if (!target.AddPassive(passiveId))
            {
                return false;
            }

            _claimedSlots.Add(BattleRewardSlot.Passive);
            return true;
        }

        private bool IsPlayerPartyMember(PachimonInstance target)
        {
            return target != null && _playerParty.Contains(target);
        }

        private PachimonInstance GetRequiredPachimon(string instanceId)
        {
            return _pachimonPool.Get(instanceId)
                ?? throw new InvalidOperationException(
                    $"Player Pachimon '{instanceId}' is missing from the Run pool.");
        }

        private void ApplyRewardElement(RewardElement element, bool isSecondSlot)
        {
            if (element == null)
            {
                return;
            }

            var amount = _settings.GetAmount(element.Kind, isSecondSlot);
            switch (element.Kind)
            {
                case RewardElementKind.Attribute:
                    AddStat(GetAttributeStat(element.Attribute), amount);
                    break;
                case RewardElementKind.Speed:
                    AddStat(PachimonStatType.Speed, amount);
                    break;
                case RewardElementKind.DamageBonus:
                    AddStat(PachimonStatType.DamageBonus, amount);
                    break;
                case RewardElementKind.ResistBonus:
                    AddStat(PachimonStatType.ResistBonus, amount);
                    break;
                case RewardElementKind.MaxHp:
                    TrainerModifierService.AddStatModifier(
                        _runState.PlayerModifiers,
                        _playerParty,
                        PachimonStatType.MaxHp,
                        amount,
                        _passiveStatModifierRegistry);
                    break;
                case RewardElementKind.MaxMn:
                    TrainerModifierService.AddStatModifier(
                        _runState.PlayerModifiers,
                        _playerParty,
                        PachimonStatType.MaxMn,
                        amount,
                        _passiveStatModifierRegistry);
                    break;
                case RewardElementKind.BonusGold:
                    _runState.Gold = checked(_runState.Gold + amount);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(element),
                        element.Kind,
                        "Unsupported Reward Element.");
            }
        }

        private void AddStat(PachimonStatType statType, int amount)
        {
            TrainerModifierService.AddStatModifier(
                _runState.PlayerModifiers,
                _playerParty,
                statType,
                amount,
                _passiveStatModifierRegistry);
        }

        private static PachimonStatType GetAttributeStat(PachimonAttribute? attribute)
        {
            if (!attribute.HasValue)
            {
                throw new InvalidOperationException(
                    "An attribute Reward Element requires an attribute.");
            }

            return PachimonStatTypeUtility.FromAttribute(attribute.Value);
        }
    }
}
