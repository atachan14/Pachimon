using NUnit.Framework;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Trainer;

namespace Pachimon.Editor.Tests
{
    public sealed class EnemyTrainerScalingTests
    {
        [Test]
        public void Apply_NormalTrainer_UsesRowScalingForEveryStat()
        {
            var modifiers = new TrainerModifierSet();
            var profile = new TrainerProfile(TrainerRole.Normal, "style", "name");

            EnemyTrainerScalingService.Apply(modifiers, 10, profile);

            Assert.That(modifiers.GetStatAddition(PachimonStatType.MaxHp), Is.EqualTo(200));
            Assert.That(modifiers.GetStatAddition(PachimonStatType.MaxMn), Is.EqualTo(200));
            Assert.That(modifiers.GetStatAddition(PachimonStatType.Fire), Is.EqualTo(40));
            Assert.That(modifiers.GetStatAddition(PachimonStatType.Haste), Is.EqualTo(40));
            Assert.That(modifiers.GetStatAddition(PachimonStatType.ResistBonus), Is.EqualTo(40));
        }

        [Test]
        public void Apply_GymLeader_AddsFavoredAndWeakAttributeAdjustments()
        {
            var modifiers = new TrainerModifierSet();
            var profile = new TrainerProfile(
                TrainerRole.GymLeader,
                "style",
                "name",
                PachimonAttribute.Fire,
                PachimonAttribute.Aqua);

            EnemyTrainerScalingService.Apply(modifiers, 5, profile);

            Assert.That(modifiers.GetStatAddition(PachimonStatType.MaxHp), Is.EqualTo(100));
            Assert.That(modifiers.GetStatAddition(PachimonStatType.Fire), Is.EqualTo(120));
            Assert.That(modifiers.GetStatAddition(PachimonStatType.Aqua), Is.EqualTo(-80));
            Assert.That(modifiers.GetStatAddition(PachimonStatType.Speed), Is.EqualTo(20));
        }

        [Test]
        public void Apply_Elite_AddsAllStatAndAttributeAdjustments()
        {
            var modifiers = new TrainerModifierSet();
            var profile = new TrainerProfile(
                TrainerRole.Elite,
                "style",
                "name",
                PachimonAttribute.Fire,
                PachimonAttribute.Aqua);

            EnemyTrainerScalingService.Apply(modifiers, 40, profile);

            Assert.That(modifiers.GetStatAddition(PachimonStatType.MaxHp), Is.EqualTo(1300));
            Assert.That(modifiers.GetStatAddition(PachimonStatType.MaxMn), Is.EqualTo(1300));
            Assert.That(modifiers.GetStatAddition(PachimonStatType.Fire), Is.EqualTo(560));
            Assert.That(modifiers.GetStatAddition(PachimonStatType.Aqua), Is.EqualTo(-40));
            Assert.That(modifiers.GetStatAddition(PachimonStatType.Speed), Is.EqualTo(260));
        }

        [Test]
        public void PreserveMissingResource_AddsMaximumIncreaseWithoutHealingDamage()
        {
            Assert.That(
                EnemyTrainerScalingService.PreserveMissingResource(900, 1000, 1800),
                Is.EqualTo(1700));
            Assert.That(
                EnemyTrainerScalingService.PreserveMissingResource(1000, 1000, 1800),
                Is.EqualTo(1800));
        }
    }
}
