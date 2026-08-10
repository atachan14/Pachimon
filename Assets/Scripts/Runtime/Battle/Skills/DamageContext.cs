using System;
using Pachimon.Reward;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public enum DamageOriginKind
    {
        Skill = 0,
        Passive = 1,
        Status = 2,
        Item = 3,
        Self = 4,
        Field = 5,
    }

    public sealed class DamageContext
    {
        public DamageContext(
            DamageOriginKind originKind,
            int originId,
            decimal baseDamage,
            EffectivePachimonStats attackerStats,
            EffectivePachimonStats defenderStats,
            PachimonAttribute attribute,
            bool isAttack,
            bool applyAttackerAttributeMultiplier = true,
            decimal penetrationPercent = 0m,
            bool applyDamageBonusMultiplier = true,
            bool applyOutgoingModifiers = true)
        {
            if (originId <= 0) throw new ArgumentOutOfRangeException(nameof(originId));
            if (baseDamage < 0m) throw new ArgumentOutOfRangeException(nameof(baseDamage));

            OriginKind = originKind;
            OriginId = originId;
            BaseDamage = baseDamage;
            AttackerStats = attackerStats
                ?? throw new ArgumentNullException(nameof(attackerStats));
            DefenderStats = defenderStats
                ?? throw new ArgumentNullException(nameof(defenderStats));
            Attribute = attribute;
            IsAttack = isAttack;
            ApplyAttackerAttributeMultiplier = applyAttackerAttributeMultiplier;
            PenetrationPercent = penetrationPercent;
            ApplyDamageBonusMultiplier = applyDamageBonusMultiplier;
            ApplyOutgoingModifiers = applyOutgoingModifiers;
        }

        public DamageOriginKind OriginKind { get; }
        public int OriginId { get; }
        public decimal BaseDamage { get; }
        public EffectivePachimonStats AttackerStats { get; }
        public EffectivePachimonStats DefenderStats { get; }
        public PachimonAttribute Attribute { get; }
        public bool IsAttack { get; }
        public bool ApplyAttackerAttributeMultiplier { get; }
        public decimal PenetrationPercent { get; }
        public bool ApplyDamageBonusMultiplier { get; }
        public bool ApplyOutgoingModifiers { get; }

        public DamageContext WithPenetrationPercent(decimal penetrationPercent)
        {
            return new DamageContext(
                OriginKind,
                OriginId,
                BaseDamage,
                AttackerStats,
                DefenderStats,
                Attribute,
                IsAttack,
                ApplyAttackerAttributeMultiplier,
                penetrationPercent,
                ApplyDamageBonusMultiplier,
                ApplyOutgoingModifiers);
        }

        public DamageContext WithDefenderStats(
            EffectivePachimonStats defenderStats)
        {
            return new DamageContext(
                OriginKind,
                OriginId,
                BaseDamage,
                AttackerStats,
                defenderStats,
                Attribute,
                IsAttack,
                ApplyAttackerAttributeMultiplier,
                PenetrationPercent,
                ApplyDamageBonusMultiplier,
                ApplyOutgoingModifiers);
        }
    }

    public sealed class TrueDamageContext
    {
        public TrueDamageContext(
            DamageOriginKind originKind,
            int originId,
            int damage,
            bool isAttack)
        {
            if (originId <= 0) throw new ArgumentOutOfRangeException(nameof(originId));
            if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));

            OriginKind = originKind;
            OriginId = originId;
            Damage = damage;
            IsAttack = isAttack;
        }

        public DamageOriginKind OriginKind { get; }
        public int OriginId { get; }
        public int Damage { get; }
        public bool IsAttack { get; }
    }
}
