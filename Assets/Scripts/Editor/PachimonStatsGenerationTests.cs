using System;
using System.Linq;
using NUnit.Framework;
using Pachimon.Data;
using Pachimon.Run;
using UnityEngine;

namespace Pachimon.Editor.Tests
{
    public sealed class PachimonStatsGenerationTests
    {
        [Test]
        public void Generate_UsesOneSharedGeneratedStatBudget()
        {
            var generator = new PachimonStatsGenerator();
            for (var seed = 0; seed < 100; seed++)
            {
                var stats = generator.Generate(new System.Random(seed));
                var generatedTotal = new[]
                    {
                        PachimonStatType.MaxHp,
                        PachimonStatType.MaxMn,
                        PachimonStatType.Fire,
                        PachimonStatType.Aqua,
                        PachimonStatType.Leaf,
                        PachimonStatType.Electric,
                        PachimonStatType.Poison,
                        PachimonStatType.Ice,
                        PachimonStatType.Wind,
                        PachimonStatType.Dragon,
                    }
                    .Sum(stats.GetValueUnits);

                Assert.That(generatedTotal, Is.EqualTo(500));
                Assert.That(stats.GetTotalValueUnits(), Is.EqualTo(500));
                Assert.That(stats.MaxHp, Is.GreaterThanOrEqualTo(500));
                Assert.That(stats.MaxMn, Is.GreaterThanOrEqualTo(500));
                foreach (var stat in new[]
                         {
                             PachimonStatType.Speed,
                             PachimonStatType.Haste,
                             PachimonStatType.DamageBonus,
                             PachimonStatType.ResistBonus,
                             PachimonStatType.GenerationPower,
                             PachimonStatType.StatusMastery,
                             PachimonStatType.SustainPower,
                             PachimonStatType.StatusResistance,
                         })
                {
                    Assert.That(stats.GetValueUnits(stat), Is.Zero);
                }
            }
        }

        [Test]
        public void Generate_ResourceBaseValueDoesNotConsumeBudget()
        {
            var settings = new PachimonStatGenerationSettings(allocationBudget: 0);
            var stats = new PachimonStatsGenerator(settings).Generate(new System.Random(123));

            Assert.That(stats.MaxHp, Is.EqualTo(500));
            Assert.That(stats.MaxMn, Is.EqualTo(500));
            Assert.That(stats.GetTotalValueUnits(), Is.Zero);
        }

        [Test]
        public void Generate_SpendsSpeciesInitialStatsBeforeRandomAllocation()
        {
            var species = ScriptableObject.CreateInstance<PachimonSpeciesAsset>();
            try
            {
                species.InitialStats.ConfigureForEditor(
                    maxHp: 240,
                    fire: 50);
                var stats = new PachimonStatsGenerator().Generate(
                    new System.Random(123),
                    species);

                Assert.That(
                    stats.GetDisplayedValue(PachimonStatType.Fire),
                    Is.GreaterThanOrEqualTo(50));
                Assert.That(stats.MaxHp, Is.GreaterThanOrEqualTo(740));
                Assert.That(stats.GetDisplayedValue(PachimonStatType.Speed), Is.Zero);
                Assert.That(stats.GetTotalValueUnits(), Is.EqualTo(500));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(species);
            }
        }

        [Test]
        public void SubStatBinding_DirectModifierDoesNotChangeBoundAttribute()
        {
            var initialStats = new PachimonInitialStats();
            initialStats.ConfigureFixedSubStatsForEditor(
                fire: FixedSubStatBinding.Speed);
            var bindings = PachimonSubStatBindings.CreateRandom(
                new System.Random(123),
                initialStats);
            var values = new int[(int)PachimonStatType.Count];
            values[(int)PachimonStatType.Fire] = 50;
            var stats = new PachimonStats(values, 1, 1);

            var effective = EffectivePachimonStats.Calculate(
                stats,
                new[]
                {
                    new FixedStatModifier(
                        PachimonStatType.Speed,
                        StatModifierOperation.DirectAdditive,
                        -30,
                        new StatModifierSource(
                            StatModifierSourceType.StatusEffect,
                            "test:slow",
                            "Slow")),
                },
                bindings);

            Assert.That(effective.GetValue(PachimonStatType.Speed), Is.EqualTo(20));
            Assert.That(effective.GetValue(PachimonStatType.Fire), Is.EqualTo(50));
        }

        [Test]
        public void SubStatBinding_FillsRemainingPairsWithoutDuplicates()
        {
            var initialStats = new PachimonInitialStats();
            initialStats.ConfigureFixedSubStatsForEditor(
                fire: FixedSubStatBinding.DamageBonus,
                aqua: FixedSubStatBinding.SustainPower);
            var bindings = PachimonSubStatBindings.CreateRandom(
                new System.Random(456),
                initialStats);

            Assert.That(
                bindings.GetSubStat(PachimonStatType.Fire),
                Is.EqualTo(PachimonStatType.DamageBonus));
            Assert.That(
                bindings.GetSubStat(PachimonStatType.Aqua),
                Is.EqualTo(PachimonStatType.SustainPower));
            Assert.That(
                PachimonSubStatBindings.Attributes
                    .Select(bindings.GetSubStat)
                    .Distinct()
                    .Count(),
                Is.EqualTo(8));
            Assert.That(
                PachimonSubStatBindings.SubStats
                    .All(subStat => bindings.GetDerivationRatio(subStat)
                        == PachimonSubStatBindings.BaseDerivationRatio),
                Is.True);
        }
    }
}
