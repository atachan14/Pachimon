using System;
using NUnit.Framework;
using Pachimon.Data;
using Pachimon.Run;
using UnityEngine;

namespace Pachimon.Editor.Tests
{
    public sealed class PachimonStatsGenerationTests
    {
        [Test]
        public void Generate_UsesIndependentAttributeAndCommonBudgets()
        {
            var generator = new PachimonStatsGenerator();
            for (var seed = 0; seed < 100; seed++)
            {
                var stats = generator.Generate(new System.Random(seed));
                var attributeTotal = 0;
                for (var stat = PachimonStatType.Fire;
                     stat <= PachimonStatType.Dragon;
                     stat++)
                {
                    attributeTotal += stats.GetValueUnits(stat);
                }

                var commonAllocationTotal =
                    stats.GetValueUnits(PachimonStatType.MaxHp) - 100
                    + stats.GetValueUnits(PachimonStatType.MaxMn) - 100
                    + stats.GetValueUnits(PachimonStatType.Speed)
                    + stats.GetValueUnits(PachimonStatType.Haste)
                    + stats.GetValueUnits(PachimonStatType.DamageBonus)
                    + stats.GetValueUnits(PachimonStatType.ResistBonus);

                Assert.That(attributeTotal, Is.EqualTo(800));
                Assert.That(commonAllocationTotal, Is.EqualTo(200));
                Assert.That(stats.MaxHp, Is.GreaterThanOrEqualTo(500));
                Assert.That(stats.MaxMn, Is.GreaterThanOrEqualTo(500));
            }
        }

        [Test]
        public void Generate_SpendsSpeciesInitialStatsBeforeRandomAllocation()
        {
            var species = ScriptableObject.CreateInstance<PachimonSpeciesAsset>();
            try
            {
                species.InitialStats.ConfigureForEditor(
                    maxHp: 250,
                    fire: 100,
                    speed: 20);
                var stats = new PachimonStatsGenerator().Generate(
                    new System.Random(123),
                    species);

                Assert.That(
                    stats.GetDisplayedValue(PachimonStatType.Fire),
                    Is.GreaterThanOrEqualTo(100));
                Assert.That(stats.MaxHp, Is.GreaterThanOrEqualTo(750));
                Assert.That(
                    stats.GetDisplayedValue(PachimonStatType.Speed),
                    Is.GreaterThanOrEqualTo(20));
                Assert.That(stats.GetTotalValueUnits(), Is.EqualTo(1200));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(species);
            }
        }
    }
}
