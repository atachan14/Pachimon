using System;
using Pachimon.Passives;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public sealed class RunningStartPassiveLogic : IPassiveLogic
    {
        private readonly RunningStartPassiveAsset _definition;
        private int _activeSkillId;
        private int _activeStartupTicks;

        public RunningStartPassiveLogic(BattleUnitState owner, RunningStartPassiveAsset definition)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        public BattleUnitState Owner { get; }

        public void Handle(IBattleEvent battleEvent)
        {
            if (battleEvent is BeforeSkillEvent before
                && ReferenceEquals(before.Source, Owner))
            {
                _activeSkillId = before.Skill.SkillId;
                _activeStartupTicks = before.Skill.BaseStartupTicks;
                return;
            }

            if (battleEvent is BeforeAttributeDamageEvent damage
                && ReferenceEquals(damage.Source, Owner)
                && damage.Calculation?.Context.OriginKind == DamageOriginKind.Skill
                && damage.Calculation.Context.OriginId == _activeSkillId
                && damage.Calculation.Context.ApplyOutgoingModifiers
                && _activeStartupTicks > 0)
            {
                var bonus = _activeStartupTicks
                    * _definition.StartupDamageBonusRatio / 100m;
                damage.MultiplyDamage(
                    SignedStatMath.AmplificationMultiplier(bonus));
                return;
            }

            if (battleEvent is SkillResolvedEvent resolved
                && ReferenceEquals(resolved.Source, Owner))
            {
                _activeSkillId = 0;
                _activeStartupTicks = 0;
            }
        }
    }
}
