using System;
using NUnit.Framework;
using Pachimon.Run;

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
                var stats = generator.Generate(new Random(seed));
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
                Assert.That(commonAllocationTotal, Is.EqualTo(600));
                Assert.That(stats.MaxHp, Is.GreaterThanOrEqualTo(500));
                Assert.That(stats.MaxMn, Is.GreaterThanOrEqualTo(500));
            }
        }
    }
}
