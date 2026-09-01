using System;
using Pachimon.Reward;
using Pachimon.Run;

namespace Pachimon.Battle
{
    [Flags]
    public enum DamageTag
    {
        None = 0,
        DamageOverTime = 1 << 0,
    }

    public enum DamageOriginKind
    {
        Skill = 0,
        Passive = 1,
        Status = 2,
        Item = 3,
        Self = 4,
        Field = 5,
    }

    public readonly struct DamagePenetration
    {
        public DamagePenetration(
            decimal attributeFixed = 0m,
            decimal attributePercentage = 0m,
            decimal resistBonusFixed = 0m,
            decimal resistBonusPercentage = 0m)
        {
            if (attributeFixed < 0m)
                throw new ArgumentOutOfRangeException(nameof(attributeFixed));
            if (resistBonusFixed < 0m)
                throw new ArgumentOutOfRangeException(nameof(resistBonusFixed));
            ValidatePercentage(attributePercentage, nameof(attributePercentage));
            ValidatePercentage(resistBonusPercentage, nameof(resistBonusPercentage));

            AttributeFixed = attributeFixed;
            AttributePercentage = attributePercentage;
            ResistBonusFixed = resistBonusFixed;
            ResistBonusPercentage = resistBonusPercentage;
        }

        public decimal AttributeFixed { get; }
        public decimal AttributePercentage { get; }
        public decimal ResistBonusFixed { get; }
        public decimal ResistBonusPercentage { get; }

        public DamagePenetration WithAdditionalResistBonusPercentage(
            decimal percentage)
        {
            return new DamagePenetration(
                AttributeFixed,
                AttributePercentage,
                ResistBonusFixed,
                PenetrationMath.CombinePercentages(
                    ResistBonusPercentage,
                    percentage));
        }

        private static void ValidatePercentage(decimal value, string parameterName)
        {
            if (value < 0m || value >= 100m)
                throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    public static class PenetrationMath
    {
        public static decimal CalculateDiminishingPercentage(decimal value)
        {
            return value <= 0m ? 0m : value / (100m + value) * 100m;
        }

        public static decimal CombinePercentages(decimal first, decimal second)
        {
            ValidatePercentage(first, nameof(first));
            ValidatePercentage(second, nameof(second));
            return 100m - (100m - first) * (100m - second) / 100m;
        }

        public static decimal ApplyToDefense(
            decimal defense,
            decimal percentage,
            decimal fixedValue)
        {
            ValidatePercentage(percentage, nameof(percentage));
            if (fixedValue < 0m)
                throw new ArgumentOutOfRangeException(nameof(fixedValue));

            var negativeDefense = Math.Min(0m, defense);
            var positiveDefense = Math.Max(0m, defense);
            return negativeDefense
                + positiveDefense * (1m - percentage / 100m)
                - fixedValue;
        }

        private static void ValidatePercentage(decimal value, string parameterName)
        {
            if (value < 0m || value >= 100m)
                throw new ArgumentOutOfRangeException(parameterName);
        }
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
            DamagePenetration penetration = default,
            bool applyDamageBonusMultiplier = true,
            bool applyOutgoingModifiers = true,
            decimal? attackerAttributeValue = null,
            DamageTag tags = DamageTag.None)
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
            Penetration = penetration;
            ApplyDamageBonusMultiplier = applyDamageBonusMultiplier;
            ApplyOutgoingModifiers = applyOutgoingModifiers;
            AttackerAttributeValue = attackerAttributeValue;
            Tags = tags;
        }

        public DamageOriginKind OriginKind { get; }
        public int OriginId { get; }
        public decimal BaseDamage { get; }
        public EffectivePachimonStats AttackerStats { get; }
        public EffectivePachimonStats DefenderStats { get; }
        public PachimonAttribute Attribute { get; }
        public bool IsAttack { get; }
        public bool ApplyAttackerAttributeMultiplier { get; }
        public DamagePenetration Penetration { get; }
        public bool ApplyDamageBonusMultiplier { get; }
        public bool ApplyOutgoingModifiers { get; }
        public decimal? AttackerAttributeValue { get; }
        public DamageTag Tags { get; }

        public DamageContext WithPenetration(DamagePenetration penetration)
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
                penetration,
                ApplyDamageBonusMultiplier,
                ApplyOutgoingModifiers,
                AttackerAttributeValue,
                Tags);
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
                Penetration,
                ApplyDamageBonusMultiplier,
                ApplyOutgoingModifiers,
                AttackerAttributeValue,
                Tags);
        }

        public DamageContext WithAttackerAttributeValue(decimal value)
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
                Penetration,
                ApplyDamageBonusMultiplier,
                ApplyOutgoingModifiers,
                value,
                Tags);
        }
    }

    public sealed class TrueDamageContext
    {
        public TrueDamageContext(
            DamageOriginKind originKind,
            int originId,
            int damage,
            bool isAttack,
            DamageTag tags = DamageTag.None)
        {
            if (originId <= 0) throw new ArgumentOutOfRangeException(nameof(originId));
            if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));

            OriginKind = originKind;
            OriginId = originId;
            Damage = damage;
            IsAttack = isAttack;
            Tags = tags;
        }

        public DamageOriginKind OriginKind { get; }
        public int OriginId { get; }
        public int Damage { get; }
        public bool IsAttack { get; }
        public DamageTag Tags { get; }
    }
}
