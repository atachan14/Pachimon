using Pachimon.Passives;
using Pachimon.Skills;
using UnityEditor;
using UnityEngine;

namespace Pachimon.Editor.UI
{
    public static class DescriptionTemplateBatch04Setup
    {
        private const string SkillCatalogPath =
            "Assets/GameData/Skill/SkillCatalog.asset";
        private const string PassiveCatalogPath =
            "Assets/GameData/Passive/PassiveCatalog.asset";

        [InitializeOnLoadMethod]
        private static void SetupAfterReload() =>
            EditorApplication.delayCall += TryAutoSetup;

        [MenuItem("Tools/Pachimon/Data/Setup Description Templates 25-32")]
        public static void Setup()
        {
            var skills = AssetDatabase.LoadAssetAtPath<SkillCatalog>(SkillCatalogPath);
            var passives = AssetDatabase.LoadAssetAtPath<PassiveCatalog>(PassiveCatalogPath);
            if (skills == null || passives == null)
            {
                Debug.LogError("SkillCatalog and PassiveCatalog are required.");
                return;
            }

            for (var id = 25; id <= 32; id++)
            {
                SetTemplate(skills.Get(id), CreateSkillTemplate(id));
                SetTemplate(passives.Get(id), CreatePassiveTemplate(id));
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Description templates 25-32 setup completed.");
        }

        private static void TryAutoSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var skills = AssetDatabase.LoadAssetAtPath<SkillCatalog>(SkillCatalogPath);
            var passives = AssetDatabase.LoadAssetAtPath<PassiveCatalog>(PassiveCatalogPath);
            if (skills?.Get(25)?.Description?.Contains("{value:value}") != true
                || passives?.Get(25)?.Description?.Contains(
                    "{value:increasePercent}") != true)
            {
                Setup();
            }
        }

        private static void SetTemplate(SkillAsset asset, string template)
        {
            if (asset == null) return;
            asset.SetDescriptionTemplateForEditor(template);
            EditorUtility.SetDirty(asset);
        }

        private static void SetTemplate(PassiveAsset asset, string template)
        {
            if (asset == null) return;
            asset.SetDescriptionTemplateForEditor(template);
            EditorUtility.SetDirty(asset);
        }

        private static string CreateSkillTemplate(int id) => id switch
        {
            25 => "自陣に値{color:Fire}{value:value}{/color}、HP {value:hp}、"
                + "持続{value:duration}tickの{term:FireBarrier|炎の障壁}を生成する。"
                + "攻撃を肩代わりし、攻撃者へ値{value:burn}の{term:Burn|火傷}を付与する。"
                + "生成者の防御Statを{value:defenseRatio}%引き継ぐ。",
            26 => "次に使用するSkillの{icon:Aqua}水を{value:aquaMultiplier}%にし、"
                + "MN消費を水に応じて軽減する。現在のMN消費倍率は"
                + "{value:currentManaMultiplier}倍。Skill効果解決後に消費する。",
            27 => "敵の先頭に{icon:Leaf}{color:Leaf}{value:damage}{/color}の"
                + "{term:LeafDamage|草ダメージ}を与える。基本発生は{value:baseStartup}tickで、"
                + "正の{term:Temperature|気温}を{value:temperatureRatio}%参照して短縮する。",
            28 => "敵の先頭に{icon:Electric}{color:Electric}{value:electricDamage}{/color}の"
                + "{term:ElectricDamage|電気ダメージ}と、"
                + "{icon:Fire}{color:Fire}{value:fireDamage}{/color}の"
                + "{term:FireDamage|炎ダメージ}を与える。現在の硬直は{value:recovery}tick、"
                + "CDは{value:cooldown}tick。",
            29 => "最も{term:Toxin|毒素}が多い敵から{value:removalPercent}%を取り除き、"
                + "別の最少対象へ除去量の{value:applicationPercent}%を付与する。",
            30 => "Battle中の{term:Temperature|気温}を恒久的に"
                + "{color:Ice}{value:temperatureReduction}{/color}低下させる。",
            31 => "HP割合が最も低い味方を{color:Wind}{value:healing}{/color}回復し、"
                + "{value:duration}tickの間、{icon:Wind}風を{value:windBonus}、"
                + "Speedを{value:speedBonus}増加させる。",
            32 => "Battle中、自身の{icon:Dragon}竜を{value:dragonBonus}、"
                + "Speedを{value:speedBonus}恒久的に増加させる。再使用時は加算する。",
            _ => string.Empty,
        };

        private static string CreatePassiveTemplate(int id) => id switch
        {
            25 => "{term:Burn|火傷}している対象へ与える属性ダメージが"
                + "{value:increasePercent}%増加する。",
            26 => "MaxMNの{value:percent}%を{icon:Aqua}水へ加算する。"
                + "現在の加算値は{value:contribution}。",
            27 => "正の{term:Temperature|気温}の{value:temperatureSpeedRatio}%を"
                + "Speedへ加算する。",
            28 => "{icon:Wind}風の{value:percent}%を{icon:Electric}電気へ加算する。"
                + "現在の加算値は{value:contribution}。",
            29 => "{icon:Poison}毒の{value:percent}%をSpeedへ加算する。"
                + "現在の加算値は{value:contribution}。",
            30 => "自身が生存中、{term:Chill|冷気}を{term:Freeze|凍結}へ変化させる"
                + "全体フィールドを生成する。複数存在する場合は統合する。",
            31 => "自身が生存中、味方が与える{icon:Wind}{term:WindDamage|風ダメージ}が"
                + "{value:increasePercent}%増加する。",
            32 => "Battle中に増加したSpeedの{value:dragonFromSpeedRatio}%を"
                + "{icon:Dragon}竜へ、増加した竜の{value:speedFromDragonRatio}%を"
                + "Speedへ相互加算する。",
            _ => string.Empty,
        };
    }
}
