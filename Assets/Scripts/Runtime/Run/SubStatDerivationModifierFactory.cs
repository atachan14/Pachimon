using System.Collections.Generic;
using System;
using System.Linq;
using Pachimon.Data;

namespace Pachimon.Run
{
    public sealed class PachimonSubStatBindings
    {
        public const int BaseDerivationRatio = 100;

        public static readonly PachimonStatType[] Attributes =
        {
            PachimonStatType.Fire, PachimonStatType.Aqua,
            PachimonStatType.Leaf, PachimonStatType.Electric,
            PachimonStatType.Ice, PachimonStatType.Wind,
            PachimonStatType.Poison, PachimonStatType.Dragon,
        };

        public static readonly PachimonStatType[] SubStats =
        {
            PachimonStatType.DamageBonus, PachimonStatType.GenerationPower,
            PachimonStatType.Haste, PachimonStatType.Speed,
            PachimonStatType.ResistBonus, PachimonStatType.SustainPower,
            PachimonStatType.StatusMastery, PachimonStatType.StatusResistance,
        };

        private readonly Dictionary<PachimonStatType, PachimonStatType> _attributeToSubStat;
        private readonly Dictionary<PachimonStatType, PachimonStatType> _subStatToAttribute;
        private readonly Dictionary<PachimonStatType, int> _derivationRatioBonuses;

        private PachimonSubStatBindings(
            IEnumerable<KeyValuePair<PachimonStatType, PachimonStatType>> pairs)
        {
            _attributeToSubStat = pairs.ToDictionary(pair => pair.Key, pair => pair.Value);
            if (_attributeToSubStat.Count != Attributes.Length
                || _attributeToSubStat.Keys.Any(attribute => !IsAttribute(attribute))
                || _attributeToSubStat.Values.Any(subStat => !IsSubStat(subStat))
                || _attributeToSubStat.Values.Distinct().Count() != SubStats.Length)
            {
                throw new ArgumentException("SubStat bindings must be a one-to-one mapping.", nameof(pairs));
            }
            _subStatToAttribute = _attributeToSubStat.ToDictionary(
                pair => pair.Value,
                pair => pair.Key);
            _derivationRatioBonuses = SubStats.ToDictionary(subStat => subStat, _ => 0);
        }

        public static PachimonSubStatBindings CreateDefault() =>
            new(new[]
            {
                Pair(PachimonStatType.Fire, PachimonStatType.DamageBonus),
                Pair(PachimonStatType.Aqua, PachimonStatType.GenerationPower),
                Pair(PachimonStatType.Leaf, PachimonStatType.Haste),
                Pair(PachimonStatType.Electric, PachimonStatType.Speed),
                Pair(PachimonStatType.Ice, PachimonStatType.ResistBonus),
                Pair(PachimonStatType.Wind, PachimonStatType.SustainPower),
                Pair(PachimonStatType.Poison, PachimonStatType.StatusMastery),
                Pair(PachimonStatType.Dragon, PachimonStatType.StatusResistance),
            });

        public static PachimonSubStatBindings CreateRandom(
            Random random,
            PachimonInitialStats initialStats)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            if (initialStats == null) throw new ArgumentNullException(nameof(initialStats));
            var pairs = new List<KeyValuePair<PachimonStatType, PachimonStatType>>();
            var remainingSubStats = SubStats.ToList();
            foreach (var attribute in Attributes)
            {
                if (!initialStats.TryGetFixedSubStat(attribute, out var subStat)) continue;
                if (!remainingSubStats.Remove(subStat))
                    throw new InvalidOperationException($"Initial SubStat binding uses {subStat} more than once.");
                pairs.Add(Pair(attribute, subStat));
            }
            for (var index = remainingSubStats.Count - 1; index > 0; index--)
            {
                var swapIndex = random.Next(index + 1);
                (remainingSubStats[index], remainingSubStats[swapIndex]) =
                    (remainingSubStats[swapIndex], remainingSubStats[index]);
            }
            var remainingAttributes = Attributes
                .Where(attribute => pairs.All(pair => pair.Key != attribute))
                .ToArray();
            for (var index = 0; index < remainingAttributes.Length; index++)
                pairs.Add(Pair(remainingAttributes[index], remainingSubStats[index]));
            return new PachimonSubStatBindings(pairs);
        }

        public PachimonStatType GetSubStat(PachimonStatType attribute) =>
            _attributeToSubStat.TryGetValue(attribute, out var subStat)
                ? subStat : throw new ArgumentOutOfRangeException(nameof(attribute));

        public PachimonStatType GetAttribute(PachimonStatType subStat) =>
            _subStatToAttribute.TryGetValue(subStat, out var attribute)
                ? attribute : throw new ArgumentOutOfRangeException(nameof(subStat));

        public int GetDerivationRatio(PachimonStatType subStat)
        {
            if (!IsSubStat(subStat))
            {
                throw new ArgumentOutOfRangeException(nameof(subStat));
            }

            return checked(BaseDerivationRatio + _derivationRatioBonuses[subStat]);
        }

        public void AddDerivationRatio(PachimonStatType subStat, int amount)
        {
            if (!IsSubStat(subStat))
            {
                throw new ArgumentOutOfRangeException(nameof(subStat));
            }
            if (amount == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            _derivationRatioBonuses[subStat] = checked(
                _derivationRatioBonuses[subStat] + amount);
        }

        public static bool IsAttribute(PachimonStatType statType) =>
            Array.IndexOf(Attributes, statType) >= 0;

        public static bool IsSubStat(PachimonStatType statType) =>
            Array.IndexOf(SubStats, statType) >= 0;

        private static KeyValuePair<PachimonStatType, PachimonStatType> Pair(
            PachimonStatType attribute,
            PachimonStatType subStat) => new(attribute, subStat);
    }

}
