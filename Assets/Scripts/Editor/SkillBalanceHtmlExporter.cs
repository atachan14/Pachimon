using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Pachimon.Battle;
using Pachimon.Run;
using Pachimon.Skills;
using Pachimon.Passives;
using UnityEditor;
using UnityEngine;

namespace Pachimon.Editor.Balance
{
    public static class SkillBalanceHtmlExporter
    {
        private const string SkillCatalogPath =
            "Assets/GameData/Skill/SkillCatalog.asset";
        private const string PassiveCatalogPath =
            "Assets/GameData/Passive/PassiveCatalog.asset";
        private const string HtmlRelativePath =
            "docs/v0.8/skill-balance.html";
        private const int LastComparedSkillId = 64;
        private const int BaselinePassiveId = 999999;
        private const string GeneratedStart =
            "// GENERATED SKILL DATA START";
        private const string GeneratedEnd =
            "// GENERATED SKILL DATA END";

        private static readonly Regex RowPattern = new(
            @"^\s*S\((?<id>\d+),""(?<name>[^""]*)"",""(?<attribute>[^""]*)"",""(?<kind>[^""]*)"",(?<startup>\d+),(?<recovery>\d+),(?<cooldown>\d+),(?<mana>\d+),(?<damageOne>-?\d+),(?<damageThree>-?\d+),(?<healingOne>-?\d+),(?<healingThree>-?\d+),(?<shield>-?\d+),(?<selfDamage>-?\d+),""(?<note>[^""]*)""(?:,(?<actionTicks>\d+),(?<repeatTicks>\d+))?\),?$",
            RegexOptions.Multiline | RegexOptions.CultureInvariant);

        private static readonly Regex GeneratedBlockPattern = new(
            @"(?ms)^(?<start>\s*// GENERATED SKILL DATA START\s*$).*?^(?<end>\s*// GENERATED SKILL DATA END\s*$)",
            RegexOptions.CultureInvariant);

        private static bool _scheduled;

        [MenuItem("Tools/Pachimon/Balance/Export Skill Balance HTML")]
        public static void ExportFromMenu()
        {
            Export(logCompletion: true);
        }

        internal static void ScheduleExport()
        {
            if (_scheduled)
            {
                return;
            }

            _scheduled = true;
            EditorApplication.delayCall += RunScheduledExport;
        }

        private static void RunScheduledExport()
        {
            _scheduled = false;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                ScheduleExport();
                return;
            }

            try
            {
                Export(logCompletion: false);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"Skill Balance HTML auto-export failed: {exception.Message}");
            }
        }

        private static void Export(bool logCompletion)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<SkillCatalog>(
                SkillCatalogPath);
            var passiveCatalog = AssetDatabase.LoadAssetAtPath<PassiveCatalog>(
                PassiveCatalogPath);
            if (catalog == null)
            {
                throw new InvalidOperationException(
                    $"Skill Catalog was not found at '{SkillCatalogPath}'.");
            }

            var htmlPath = Path.Combine(
                Directory.GetParent(Application.dataPath)?.FullName
                    ?? throw new InvalidOperationException(
                        "Project root could not be resolved."),
                HtmlRelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(htmlPath))
            {
                throw new FileNotFoundException(
                    "Skill Balance HTML was not found.", htmlPath);
            }

            var html = File.ReadAllText(htmlPath, Encoding.UTF8);
            var existingRows = ParseExistingRows(html);
            var logicRegistry = new SkillLogicRegistry(catalog, passiveCatalog);
            var warnings = new List<string>();
            var rows = catalog.Skills
                .Where(skill => skill != null
                    && skill.SkillId >= 1
                    && skill.SkillId <= LastComparedSkillId)
                .OrderBy(skill => skill.SkillId)
                .Select(skill => BuildRow(
                    skill,
                    logicRegistry,
                    existingRows,
                    warnings))
                .ToArray();

            if (rows.Length != LastComparedSkillId)
            {
                throw new InvalidOperationException(
                    $"Expected {LastComparedSkillId} Skills, but found {rows.Length}.");
            }

            var generated = new StringBuilder()
                .AppendLine(GeneratedStart)
                .AppendLine("    const skills = [")
                .AppendJoin(",\n", rows.Select(row => "      " + row))
                .AppendLine()
                .AppendLine("    ];")
                .Append("    ")
                .Append(GeneratedEnd)
                .ToString();
            var updated = GeneratedBlockPattern.Replace(html, generated, 1);
            if (ReferenceEquals(updated, html) || updated == html)
            {
                if (!GeneratedBlockPattern.IsMatch(html))
                {
                    throw new InvalidOperationException(
                        "Generated Skill data markers were not found in the HTML.");
                }
            }
            else
            {
                File.WriteAllText(
                    htmlPath,
                    updated,
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            }

            if (warnings.Count > 0)
            {
                Debug.LogWarning(
                    "Skill Balance HTML retained previous output values for:\n"
                    + string.Join("\n", warnings));
            }
            if (logCompletion)
            {
                Debug.Log(
                    $"Exported {rows.Length} Skills to {HtmlRelativePath}.");
            }
        }

        private static string BuildRow(
            SkillAsset skill,
            SkillLogicRegistry logicRegistry,
            IReadOnlyDictionary<int, ExistingRow> existingRows,
            ICollection<string> warnings)
        {
            existingRows.TryGetValue(skill.SkillId, out var existing);
            if (!logicRegistry.TryGet(skill.SkillId, out var logic))
            {
                warnings.Add($"Skill {skill.SkillId}: no Skill Logic");
                return CreateFallbackRow(skill, existing);
            }

            try
            {
                var one = EvaluateScenario(skill, logic, includeAllUnits: false);
                var three = EvaluateScenario(skill, logic, includeAllUnits: true);
                var note = existing?.Note ?? string.Empty;
                var attribute = existing?.Attribute
                    ?? skill.AllocationType.ToString();
                var kind = existing?.Kind ?? "未分類";
                return FormatRow(
                    skill.SkillId,
                    skill.DisplayName,
                    attribute,
                    kind,
                    one.Timing.StartupTicks,
                    one.Timing.RecoveryTicks,
                    one.Timing.CooldownTicks,
                    one.Mana,
                    one.Damage,
                    three.Damage,
                    one.Healing,
                    three.Healing,
                    one.Shield,
                    one.SelfDamage,
                    note,
                    checked(one.Timing.StartupTicks
                        + one.Timing.RecoveryTicks),
                    Math.Max(
                        checked(one.Timing.StartupTicks
                            + one.Timing.RecoveryTicks),
                        one.Timing.CooldownTicks));
            }
            catch (Exception exception)
            {
                warnings.Add(
                    $"Skill {skill.SkillId} ({skill.DisplayName}): "
                    + exception.GetType().Name + " - " + exception.Message);
                return CreateFallbackRow(skill, existing);
            }
        }

        private static string CreateFallbackRow(
            SkillAsset skill,
            ExistingRow existing)
        {
            var timing = SkillTimingCalculator.CreatePlan(
                skill,
                CreateEffectiveStats());
            var actionTicks = checked(timing.StartupTicks + timing.RecoveryTicks);
            return FormatRow(
                skill.SkillId,
                skill.DisplayName,
                existing?.Attribute ?? skill.AllocationType.ToString(),
                existing?.Kind ?? "未分類",
                timing.StartupTicks,
                timing.RecoveryTicks,
                timing.CooldownTicks,
                skill.BaseManaCost,
                existing?.DamageOne ?? 0,
                existing?.DamageThree ?? 0,
                existing?.HealingOne ?? 0,
                existing?.HealingThree ?? 0,
                existing?.Shield ?? 0,
                existing?.SelfDamage ?? 0,
                existing?.Note ?? string.Empty,
                actionTicks,
                Math.Max(actionTicks, timing.CooldownTicks));
        }

        private static ScenarioResult EvaluateScenario(
            SkillAsset skill,
            ISkillLogic logic,
            bool includeAllUnits)
        {
            var player = CreateSide(
                BattleSide.Player,
                skill.SkillId,
                includeAllUnits,
                currentHp: 1000);
            var enemy = CreateSide(
                BattleSide.Enemy,
                skill.SkillId,
                includeAllUnits,
                currentHp: 2000);
            var state = new BattleState(
                12345,
                player,
                enemy,
                CreateBaselinePassiveRegistry(),
                publishBattleStarted: false);
            var user = player.Units[0];
            var timing = SkillTimingCalculator.CreatePlan(skill, user, state);
            var manaPlan = BattleSkillManaCostCalculator.CreatePlan(
                state,
                user,
                skill);
            var playerHpBefore = player.Units.Select(unit => unit.CurrentHp).ToArray();
            var enemyHpBefore = enemy.Units.Select(unit => unit.CurrentHp).ToArray();
            var shieldBefore = player.Units.Sum(unit => unit.TotalShield);

            if (manaPlan.Actual > 0 && !user.TrySpendMn(manaPlan.Actual))
            {
                throw new InvalidOperationException(
                    $"Baseline Unit cannot spend {manaPlan.Actual} MN.");
            }

            BattleSkillResolver.Resolve(
                state,
                user,
                skill,
                logic,
                actualManaSpent: manaPlan.Actual,
                effectiveManaSpent: manaPlan.Effective,
                skillSlotId: 1);

            var damage = enemy.Units
                .Select((unit, index) => Math.Max(
                    0,
                    enemyHpBefore[index] - unit.CurrentHp))
                .Sum();
            var healing = player.Units
                .Select((unit, index) => Math.Max(
                    0,
                    unit.CurrentHp - playerHpBefore[index]))
                .Sum();
            var selfDamage = player.Units
                .Select((unit, index) => Math.Max(
                    0,
                    playerHpBefore[index] - unit.CurrentHp))
                .Sum();
            var shield = Math.Max(
                0,
                player.Units.Sum(unit => unit.TotalShield) - shieldBefore);

            foreach (var effect in state.Fields.Effects
                         .Where(effect => effect.TargetSide == BattleSide.Player))
            {
                if (effect.EffectId == BattleFieldEffectId.FireBarrier)
                {
                    shield = checked(shield + effect.Value);
                }
                else if (effect.EffectId == BattleFieldEffectId.WaterVeil
                    && effect.Definition is WaterVeilFieldEffectAsset veil
                    && veil.DecayPerTick > 0)
                {
                    var ticks = (effect.Value + veil.DecayPerTick - 1)
                        / veil.DecayPerTick;
                    var livingCount = player.Units.Count(unit => unit.IsAlive);
                    healing = checked(
                        ticks * veil.HealingPerTick * livingCount);
                }
            }

            return new ScenarioResult(
                damage,
                healing,
                shield,
                selfDamage,
                manaPlan.Actual,
                timing);
        }

        private static BattleSideState CreateSide(
            BattleSide side,
            int skillId,
            bool includeAllUnits,
            int currentHp)
        {
            return new BattleSideState(
                side,
                Enumerable.Range(0, BattleSideState.MaxPartySize)
                    .Select(index => new BattleUnitState(
                        $"balance_{side}_{index}",
                        index + 1,
                        $"Balance {side} {index + 1}",
                        side,
                        index,
                        CreateEffectiveStats(),
                        index == 0 || includeAllUnits ? currentHp : 0,
                        1000,
                        new[] { new PachimonSkillSlot(1, skillId) },
                        new[] { BaselinePassiveId })));
        }

        private static PassiveLogicRegistry CreateBaselinePassiveRegistry()
        {
            var registry = new PassiveLogicRegistry();
            registry.RegisterOrReplace(
                BaselinePassiveId,
                owner => new BaselinePassiveLogic(owner));
            return registry;
        }

        private static EffectivePachimonStats CreateEffectiveStats()
        {
            var values = new int[(int)PachimonStatType.Count];
            values[(int)PachimonStatType.MaxHp] = 2000;
            values[(int)PachimonStatType.MaxMn] = 1000;
            for (var stat = PachimonStatType.Fire;
                 stat <= PachimonStatType.Dragon;
                 stat++)
            {
                values[(int)stat] = 100;
            }
            return new EffectivePachimonStats(
                new PachimonStats(
                    values,
                    resourceDisplayMultiplier: 1,
                    specialStatDivisor: 1),
                modifiers: null);
        }

        private static Dictionary<int, ExistingRow> ParseExistingRows(string html)
        {
            return RowPattern.Matches(html)
                .Cast<Match>()
                .Select(match => new ExistingRow(match))
                .ToDictionary(row => row.Id);
        }

        private static string FormatRow(
            int id,
            string name,
            string attribute,
            string kind,
            int startup,
            int recovery,
            int cooldown,
            int mana,
            int damageOne,
            int damageThree,
            int healingOne,
            int healingThree,
            int shield,
            int selfDamage,
            string note,
            int actionTicks,
            int repeatTicks)
        {
            return $"S({id},\"{Escape(name)}\",\"{Escape(attribute)}\","
                + $"\"{Escape(kind)}\",{startup},{recovery},{cooldown},{mana},"
                + $"{damageOne},{damageThree},{healingOne},{healingThree},"
                + $"{shield},{selfDamage},\"{Escape(note)}\","
                + $"{actionTicks},{repeatTicks})";
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", string.Empty)
                .Replace("\n", "\\n");
        }

        private sealed class ExistingRow
        {
            public ExistingRow(Match match)
            {
                Id = Parse(match, "id");
                Attribute = match.Groups["attribute"].Value;
                Kind = match.Groups["kind"].Value;
                DamageOne = Parse(match, "damageOne");
                DamageThree = Parse(match, "damageThree");
                HealingOne = Parse(match, "healingOne");
                HealingThree = Parse(match, "healingThree");
                Shield = Parse(match, "shield");
                SelfDamage = Parse(match, "selfDamage");
                Note = match.Groups["note"].Value;
            }

            public int Id { get; }
            public string Attribute { get; }
            public string Kind { get; }
            public int DamageOne { get; }
            public int DamageThree { get; }
            public int HealingOne { get; }
            public int HealingThree { get; }
            public int Shield { get; }
            public int SelfDamage { get; }
            public string Note { get; }

            private static int Parse(Match match, string group) =>
                int.Parse(match.Groups[group].Value);
        }

        private readonly struct ScenarioResult
        {
            public ScenarioResult(
                int damage,
                int healing,
                int shield,
                int selfDamage,
                int mana,
                BattleSkillTimingPlan timing)
            {
                Damage = damage;
                Healing = healing;
                Shield = shield;
                SelfDamage = selfDamage;
                Mana = mana;
                Timing = timing;
            }

            public int Damage { get; }
            public int Healing { get; }
            public int Shield { get; }
            public int SelfDamage { get; }
            public int Mana { get; }
            public BattleSkillTimingPlan Timing { get; }
        }

        private sealed class BaselinePassiveLogic : IPassiveLogic
        {
            public BaselinePassiveLogic(BattleUnitState owner)
            {
                Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            }

            public BattleUnitState Owner { get; }

            public void Handle(IBattleEvent battleEvent)
            {
            }
        }
    }

    [InitializeOnLoad]
    internal static class SkillBalanceHtmlAutoExport
    {
        static SkillBalanceHtmlAutoExport()
        {
            SkillBalanceHtmlExporter.ScheduleExport();
        }
    }

    internal sealed class SkillBalanceAssetPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (importedAssets.Concat(movedAssets).Any(IsRelevantAsset))
            {
                SkillBalanceHtmlExporter.ScheduleExport();
            }
        }

        internal static bool IsRelevantAsset(string path)
        {
            return path.StartsWith(
                    "Assets/GameData/Skill/",
                    StringComparison.Ordinal)
                || path.StartsWith(
                    "Assets/GameData/Battle/FieldEffect/",
                    StringComparison.Ordinal);
        }
    }

    internal sealed class SkillBalanceAssetSaveProcessor
        : AssetModificationProcessor
    {
        private static string[] OnWillSaveAssets(string[] paths)
        {
            if (paths.Any(SkillBalanceAssetPostprocessor.IsRelevantAsset))
            {
                SkillBalanceHtmlExporter.ScheduleExport();
            }

            return paths;
        }
    }
}
