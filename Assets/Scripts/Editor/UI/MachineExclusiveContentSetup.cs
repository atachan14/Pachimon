using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Battle;
using Pachimon.Data;
using Pachimon.Items;
using Pachimon.Skills;
using UnityEditor;
using UnityEngine;

namespace Pachimon.Editor.UI
{
    public static class MachineExclusiveContentSetup
    {
        private const string MenuPath =
            "Tools/Pachimon/Data/Setup Machine-exclusive Skills";
        private const string SkillFolder = "Assets/GameData/Skill/Machine";
        private const string ItemFolder = "Assets/GameData/Item";
        private const string StatusFolder = "Assets/GameData/Battle/Status";
        private const string SkillCatalogPath =
            "Assets/GameData/Skill/SkillCatalog.asset";
        private const string ItemCatalogPath =
            "Assets/GameData/Item/ItemCatalog.asset";
        private const string StunStatusPath =
            "Assets/GameData/Battle/Status/StunStatus.asset";
        private const string BurnStatusPath =
            "Assets/GameData/Battle/Status/BurnStatus.asset";
        private const string ToxinStatusPath =
            "Assets/GameData/Battle/Status/ToxinStatus.asset";
        private const string FreezeStatusPath =
            "Assets/GameData/Battle/Status/FreezeStatus.asset";
        private const string FieldFolder =
            "Assets/GameData/Battle/FieldEffect";
        private const string CombustionLegacyPath =
            "Assets/GameData/Skill/Placeholder/Skill_041.asset";
        private const string CombustionMachinePath =
            "Assets/GameData/Skill/Machine/Skill_1008_Combustion.asset";
        private const string WaterPulseLegacyPath =
            "Assets/GameData/Skill/Placeholder/Skill_010.asset";
        private const string SeaPulseMachinePath =
            "Assets/GameData/Skill/Machine/Skill_1009_SeaPulse.asset";

        [InitializeOnLoadMethod]
        private static void SetupAfterReload()
        {
            EditorApplication.delayCall += TryAutoSetup;
        }

        private static void TryAutoSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || IsConfigured())
                return;

            Setup();
        }

        [MenuItem(MenuPath)]
        public static void Setup()
        {
            EnsureAssetFolder(SkillFolder);
            var stun = AssetDatabase.LoadAssetAtPath<StunStatusAsset>(
                StunStatusPath);
            var burn = AssetDatabase.LoadAssetAtPath<BurnStatusAsset>(
                BurnStatusPath);
            var toxin = AssetDatabase.LoadAssetAtPath<ToxinStatusAsset>(
                ToxinStatusPath);
            var freeze = AssetDatabase.LoadAssetAtPath<FreezeStatusAsset>(
                FreezeStatusPath);
            if (stun == null || burn == null || toxin == null || freeze == null)
            {
                Debug.LogError(
                    "Stun, Burn, Toxin, and Freeze Status Assets are required before machine Skills can be created.");
                return;
            }

            var charm = GetOrCreate<CharmStatusAsset>(
                $"{StatusFolder}/CharmStatus.asset");
            charm.ConfigureForEditor(
                "魅力",
                "Battle中に蓄積する恒久Stack。セクシーポーズのStun量に使用する。");
            var intangible = GetOrCreate<IntangibleStatusAsset>(
                $"{StatusFolder}/IntangibleStatus.asset");
            intangible.ConfigureForEditor(
                "無形化",
                "発生中に受ける正のDamageを1にする。");
            var clone = GetOrCreate<CloneStatusAsset>(
                $"{StatusFolder}/CloneStatus.asset");
            clone.ConfigureForEditor(
                "分身",
                "次に選択するSkillの解決回数をStack数だけ増加する。");

            var windGodStatus = GetOrCreate<WindGodStatusAsset>(
                $"{StatusFolder}/WindGodStatus.asset");
            windGodStatus.ConfigureForEditor(
                "風神",
                "全属性値とRBを0にする。");
            var dragonInstallStatus = GetOrCreate<DragonInstallStatusAsset>(
                $"{StatusFolder}/DragonInstallStatus.asset");
            dragonInstallStatus.ConfigureForEditor(
                "ドラゴンインストール",
                "Dragon、SPD、HSTを乗算する。");
            var responsivePlant = GetOrCreate<ResponsivePlantFieldEffectAsset>(
                $"{FieldFolder}/ResponsivePlantFieldEffect.asset");
            responsivePlant.ConfigureForEditor(
                "呼応する植物",
                "味方の他の植物が攻撃するたび、先頭の敵を攻撃する。",
                40,
                100);

            var triAttack = GetOrCreate<TriAttackSkillAsset>(
                $"{SkillFolder}/Skill_1000_TriAttack.asset");
            triAttack.ConfigureForEditor(
                1000, 100, 100, 200, 20, 50, 100,
                "先頭の敵へ、最も高い3つの属性値を参照した3属性のDamageを与える。");

            var bodySlam = GetOrCreate<BodySlamSkillAsset>(
                $"{SkillFolder}/Skill_1001_BodySlam.asset");
            bodySlam.ConfigureForEditor(
                1001, 100, 100, 200, 20, 5,
                "先頭の敵へ、自身のCurrentHPの5%のTrue Damageを与える。");

            var fakeOut = GetOrCreate<FakeOutSkillAsset>(
                $"{SkillFolder}/Skill_1002_FakeOut.asset");
            fakeOut.ConfigureForEditor(
                1002, 100, 100, stun,
                "Battle中にSlotごとに1度だけ使用できる。先頭の敵へTrue DamageとStunを与える。");

            var destructionBeam = GetOrCreate<DestructionBeamSkillAsset>(
                $"{SkillFolder}/Skill_1006_DestructionBeam.asset");
            destructionBeam.ConfigureForEditor(
                1006, 100, 500, 1000, 100, 25,
                "先頭の敵へ、対象のMaxHPの25%のTrue Damageを与える。");

            var sexyPose = GetOrCreate<SexyPoseSkillAsset>(
                $"{SkillFolder}/Skill_1003_SexyPose.asset");
            sexyPose.ConfigureForEditor(
                1003, 15, 100, charm, stun,
                "魅力を15獲得し、敵全体へ現在の魅力に応じたStunを与える。");

            var intangibility = GetOrCreate<IntangibilitySkillAsset>(
                $"{SkillFolder}/Skill_1005_Intangibility.asset");
            intangibility.ConfigureForEditor(
                1005, intangible,
                "発生中、受ける正のDamageをすべて1にする。");

            var cloneTechnique = GetOrCreate<CloneTechniqueSkillAsset>(
                $"{SkillFolder}/Skill_1004_CloneTechnique.asset");
            cloneTechnique.ConfigureForEditor(
                1004, 1, clone,
                "分身を1獲得する。次に選択するSkillを追加コストなしで再解決する。");

            var spiritBomb = GetOrCreate<SpiritBombSkillAsset>(
                $"{SkillFolder}/Skill_1007_SpiritBomb.asset");
            spiritBomb.ConfigureForEditor(
                1007, 20, 2,
                "生存している味方全員のCurrentMNを20%ずつ消費し、合計消費MNの2倍のTrue Damageを敵全体へ分散する。");

            var plantRage = GetOrCreate<PlantRageSkillAsset>(
                $"{SkillFolder}/Skill_1010_PlantRage.asset");
            plantRage.ConfigureForEditor(
                1010, 100, 100, 300, 80, responsivePlant,
                "呼応する植物を生成し、このTurn中、味方の植物へDB+100%を適用して一斉攻撃させる。");
            var chainThunder = GetOrCreate<ChainThunderSkillAsset>(
                $"{SkillFolder}/Skill_1011_ChainThunder.asset");
            chainThunder.ConfigureForEditor(
                1011, 100, 120, 300, 80, 40, 100,
                "Battle中に発生したElectric Damage回数だけ追加連鎖するElectric Damageを与える。");
            var deathmatch = GetOrCreate<DeathmatchSkillAsset>(
                $"{SkillFolder}/Skill_1012_Deathmatch.asset");
            deathmatch.ConfigureForEditor(
                1012, 100, 150, 400, 100, toxin,
                "自身のCurrentHPと同値の毒素を、自身と敵全体へ付与する。");
            var freezing = GetOrCreate<FreezingSkillAsset>(
                $"{SkillFolder}/Skill_1013_Freezing.asset");
            freezing.ConfigureForEditor(
                1013, 100, 100, 300, 60, 50, freeze,
                "先頭の敵の冷気を解除し、同値の凍結と冷気の50%のTrue Damageを与える。");
            var windGod = GetOrCreate<WindGodSkillAsset>(
                $"{SkillFolder}/Skill_1014_WindGod.asset");
            windGod.ConfigureForEditor(
                1014, 100, 150, 400, 100, 250, 100, 300,
                windGodStatus,
                "先頭の敵へWind Damageを与え、300tickの間、自身の全属性値とRBを0にする。");
            var dragonInstall = GetOrCreate<DragonInstallSkillAsset>(
                $"{SkillFolder}/Skill_1015_DragonInstall.asset");
            dragonInstall.ConfigureForEditor(
                1015, 100, 100, 400, 80, 400, 200,
                dragonInstallStatus,
                "CurrentHPを半分にし、400tickの間、Dragon、SPD、HSTを2倍にする。");

            var (combustion, burningStrike) = MigrateCombustion(burn);
            var (seaPulse, waterPulse) = MigrateWaterPulse();

            var skills = new SkillAsset[]
            {
                triAttack, bodySlam, fakeOut, sexyPose,
                cloneTechnique, intangibility, destructionBeam, spiritBomb,
                combustion, seaPulse, burningStrike, waterPulse,
                plantRage, chainThunder, deathmatch, freezing,
                windGod, dragonInstall,
            };
            ReplaceCatalogEntries(
                AssetDatabase.LoadAssetAtPath<SkillCatalog>(SkillCatalogPath),
                skills,
                skill => skill.SkillId,
                (catalog, values) => catalog.SetSkillsForEditor(values));

            var machines = skills.Select(ConfigureSkillMachine).ToArray();
            ReplaceCatalogEntries(
                AssetDatabase.LoadAssetAtPath<ItemCatalog>(ItemCatalogPath),
                machines.Cast<ItemAsset>(),
                item => item.ItemId,
                (catalog, values) => catalog.SetItemsForEditor(values));

            foreach (var asset in skills.Cast<UnityEngine.Object>()
                         .Concat(machines)
                         .Concat(new UnityEngine.Object[]
                         {
                             charm, intangible, clone, windGodStatus,
                             dragonInstallStatus, responsivePlant,
                         }))
            {
                EditorUtility.SetDirty(asset);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Machine-exclusive Skill setup completed.");
        }

        private static bool IsConfigured()
        {
            var skillCatalog = AssetDatabase.LoadAssetAtPath<SkillCatalog>(
                SkillCatalogPath);
            var itemCatalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(
                ItemCatalogPath);
            var neutralIds = new[]
            {
                1000, 1001, 1002, 1003,
                1004, 1005, 1006, 1007,
            };
            var specialIds = new[]
            {
                1008, 1009, 1010, 1011, 1012, 1013, 1014, 1015,
            };
            var expectedTypes = new Dictionary<int, AllocationType>
            {
                [1008] = AllocationType.Fire,
                [1009] = AllocationType.Aqua,
                [1010] = AllocationType.Leaf,
                [1011] = AllocationType.Electric,
                [1012] = AllocationType.Poison,
                [1013] = AllocationType.Ice,
                [1014] = AllocationType.Wind,
                [1015] = AllocationType.Dragon,
            };
            var machineIds = neutralIds.Concat(specialIds);
            return skillCatalog != null
                && itemCatalog != null
                && neutralIds.All(id =>
                    skillCatalog.Get(id) is MachineExclusiveSkillAsset skill
                    && skill.AllocationType == AllocationType.Unassigned)
                && skillCatalog.Get(1008) is CombustionSkillAsset combustion
                && !combustion.IsMapAssignable
                && skillCatalog.Get(1009) is WaterPulseSkillAsset seaPulse
                && !seaPulse.IsMapAssignable
                && specialIds.All(id =>
                    skillCatalog.Get(id) is SkillAsset skill
                    && !skill.IsMapAssignable
                    && skill.AllocationType == expectedTypes[id])
                && machineIds.All(id => itemCatalog.Get(
                    ItemIds.GetSkillMachineItemId(id)) is SkillMachineItemAsset)
                && skillCatalog.Get(41) is BurningStrikeSkillAsset
                && skillCatalog.Get(10) is WaterPulseReplacementSkillAsset;
        }

        private static (CombustionSkillAsset machine, BurningStrikeSkillAsset replacement)
            MigrateCombustion(BurnStatusAsset burn)
        {
            var machine = MoveLegacySkill<CombustionSkillAsset>(
                CombustionLegacyPath,
                CombustionMachinePath);
            var startup = machine.BaseStartupTicks;
            var recovery = machine.BaseRecoveryTicks;
            var cooldown = machine.BaseCooldownTicks;
            var mana = machine.BaseManaCost;
            var description = machine.Description;
            var baseDamage = machine.BaseDamage;
            var fireRatio = machine.FireScalingPercent;
            machine.ConfigureForEditor(
                1008,
                "燃焼",
                recovery,
                cooldown,
                mana,
                description,
                baseDamage,
                fireRatio,
                isMapAssignable: false,
                baseStartupTicks: startup);

            MoveLegacyMachineItem(
                "Assets/GameData/Item/Item_10041_TM_Combustion.asset",
                "Assets/GameData/Item/Item_11008_TM_Combustion.asset",
                machine);

            var replacement = ReplaceWith<BurningStrikeSkillAsset>(
                CombustionLegacyPath);
            replacement.ConfigureForEditor(
                41, 100, 100, 300, 60,
                100, 100, 300, 100, 20, 100, burn,
                "自身へFire Damage。その後、生存していれば先頭の敵へFire Damageと火傷を与える。");
            replacement.SetDescriptionTemplateForEditor(
                "自身に{color:Fire}{value:selfDamage}{/color}（{value:selfBaseDamage} × {icon:Fire}）の"
                + "{icon:Fire}{color:Fire}ダメージ{/color}を与える。自身が生存した場合、"
                + "敵の先頭に{color:Fire}{value:enemyDamage}{/color}（{value:enemyBaseDamage} × {icon:Fire}）の"
                + "{icon:Fire}{color:Fire}ダメージ{/color}と{color:Fire}{value:burn}{/color}"
                + "（{value:baseBurn} × {icon:Fire}）の火傷を与える。");
            return (machine, replacement);
        }

        private static (WaterPulseSkillAsset machine, WaterPulseReplacementSkillAsset replacement)
            MigrateWaterPulse()
        {
            var machine = MoveLegacySkill<WaterPulseSkillAsset>(
                WaterPulseLegacyPath,
                SeaPulseMachinePath);
            var startup = machine.BaseStartupTicks;
            var recovery = machine.BaseRecoveryTicks;
            var cooldown = machine.BaseCooldownTicks;
            var description = machine.Description;
            var aquaRatio = machine.AquaDamageRatio;
            machine.ConfigureForEditor(
                1009,
                "海の波動",
                recovery,
                cooldown,
                description,
                aquaRatio,
                isMapAssignable: false,
                baseStartupTicks: startup);

            MoveLegacyMachineItem(
                "Assets/GameData/Item/Item_10010_TM_WaterPulse.asset",
                "Assets/GameData/Item/Item_11009_TM_WaterPulse.asset",
                machine);

            var replacement = ReplaceWith<WaterPulseReplacementSkillAsset>(
                WaterPulseLegacyPath);
            replacement.ConfigureForEditor(
                10, 100, 150, 300, 4, 3, 100,
                "MaxMNの4%を消費し、消費MNの3倍を基礎値とするAqua Damageを先頭の敵へ与える。");
            return (machine, replacement);
        }

        private static T MoveLegacySkill<T>(string legacyPath, string machinePath)
            where T : SkillAsset
        {
            var machine = AssetDatabase.LoadAssetAtPath<T>(machinePath);
            if (machine != null)
                return machine;

            var legacy = AssetDatabase.LoadAssetAtPath<T>(legacyPath);
            if (legacy != null)
            {
                MoveAssetOrThrow(legacyPath, machinePath);
                return AssetDatabase.LoadAssetAtPath<T>(machinePath);
            }

            machine = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(machine, machinePath);
            return machine;
        }

        private static void MoveLegacyMachineItem(
            string legacyPath,
            string machinePath,
            SkillAsset expectedSkill)
        {
            if (AssetDatabase.LoadAssetAtPath<SkillMachineItemAsset>(machinePath) != null)
                return;

            var legacy = AssetDatabase.LoadAssetAtPath<SkillMachineItemAsset>(
                legacyPath);
            if (legacy != null && ReferenceEquals(legacy.Skill, expectedSkill))
                MoveAssetOrThrow(legacyPath, machinePath);
        }

        private static void MoveAssetOrThrow(string source, string destination)
        {
            var error = AssetDatabase.MoveAsset(source, destination);
            if (!string.IsNullOrEmpty(error))
                throw new InvalidOperationException(
                    $"Failed to move Asset from '{source}' to '{destination}': {error}");
        }

        private static SkillMachineItemAsset ConfigureSkillMachine(
            SkillAsset skill)
        {
            var itemId = ItemIds.GetSkillMachineItemId(skill.SkillId);
            var typeName = skill switch
            {
                WaterPulseReplacementSkillAsset => "WaterPulse",
                _ => skill.GetType().Name.Replace("SkillAsset", string.Empty),
            };
            var item = GetOrCreate<SkillMachineItemAsset>(
                $"{ItemFolder}/Item_{itemId}_TM_{typeName}.asset");
            item.ConfigureForEditor(
                itemId,
                $"技マシーン[{skill.DisplayName}]",
                null,
                $"対象の味方パチモンが「{skill.DisplayName}」を習得する。",
                ItemCategory.SkillMachine,
                1000);
            item.ConfigureSkillForEditor(skill);
            return item;
        }

        private static T GetOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static T ReplaceWith<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                AssetDatabase.DeleteAsset(path);
            return GetOrCreate<T>(path);
        }

        private static void ReplaceCatalogEntries<TCatalog, TEntry>(
            TCatalog catalog,
            IEnumerable<TEntry> replacements,
            Func<TEntry, int> getId,
            Action<TCatalog, IEnumerable<TEntry>> setEntries)
            where TCatalog : ScriptableObject
            where TEntry : UnityEngine.Object
        {
            if (catalog == null)
                throw new InvalidOperationException($"{typeof(TCatalog).Name} is missing.");

            IEnumerable<TEntry> current = catalog switch
            {
                SkillCatalog skills => skills.Skills.Cast<TEntry>(),
                ItemCatalog items => items.Items.Cast<TEntry>(),
                _ => throw new InvalidOperationException("Unsupported Catalog type."),
            };
            var byId = new Dictionary<int, TEntry>();
            foreach (var entry in current.Where(entry => entry != null))
            {
                var id = getId(entry);
                if (id > 0 && !byId.ContainsKey(id))
                    byId.Add(id, entry);
            }
            foreach (var replacement in replacements)
                byId[getId(replacement)] = replacement;

            setEntries(catalog, byId.Values.OrderBy(getId));
            EditorUtility.SetDirty(catalog);
        }

        private static void EnsureAssetFolder(string folder)
        {
            var segments = folder.Split('/');
            var current = segments[0];
            for (var index = 1; index < segments.Length; index++)
            {
                var next = $"{current}/{segments[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, segments[index]);
                current = next;
            }
        }
    }
}
