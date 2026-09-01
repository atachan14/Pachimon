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
                var skillTemplate = CreateSkillTemplate(id);
                if (id == 27)
                {
                    skillTemplate += "\u5024{value:pollen}"
                        + "\uFF08{value:pollenBaseValue} \u00D7\uFF08100 + {icon:Wind}"
                        + "{value:wind} \u00D7 {value:pollenRatio}%\uFF09%\uFF09\u306E"
                        + "{term:Pollen|\u82B1\u7C89}\u3092\u4ED8\u4E0E\u3059\u308B\u3002";
                }
                SetTemplate(skills.Get(id), skillTemplate);
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
            if (skills?.Get(25)?.Description?.Contains("{value:baseValue}") != true
                || skills?.Get(27)?.Description?.Contains("{value:pollen}") != true
                || skills?.Get(29)?.Description?.Contains(
                    "{value:baseApplicationPercent}") != true
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
            25 => "自陣にValue {color:Fire}{value:value}{/color}（{value:baseValue} ×（100 + "
                + "{icon:Fire}{value:fire} × {value:valueRatio}%）%）の"
                + "{term:FireBarrier|炎の障壁}を生成する。Valueは毎tick 1減少し、"
                + "味方への攻撃を肩代わりする。攻撃者へ被弾直前Valueの"
                + "{value:burnRatio}%の{term:Burn|火傷}を付与する。"
                + "防御Statは炎200、水0、その他の属性100、RB 0。",
            26 => "次に使用するSkillの{icon:Aqua}水を{value:aquaMultiplier}%にし、"
                + "MN消費を水に応じて軽減する。現在のMN消費倍率は{value:currentManaMultiplier}倍"
                + "（100 ÷（100 + {icon:Aqua}{value:aqua} × {value:manaReductionRatio}%））。"
                + "Skill効果解決後に消費する。",
            27 => "敵の先頭に{color:Leaf}{value:damage}{/color}（{value:baseDamage} ×（100 + "
                + "{icon:Leaf}{value:leaf} × {value:damageRatio}%）%）の"
                + "{icon:Leaf}{color:Leaf}ダメージ{/color}を与える。基本発生は{value:baseStartup}tickで、"
                + "正の{term:Temperature|気温}を{value:temperatureRatio}%参照して短縮する。",
            28 => "敵の先頭に{color:Electric}{value:electricDamage}{/color}（"
                + "{value:electricBaseDamage} ×（100 + {icon:Electric}{value:electric} × "
                + "{value:damageRatio}%）%）の{icon:Electric}{color:Electric}ダメージ{/color}を与える。"
                + "発生と硬直を{icon:Fire}炎の{value:fireTimingRatio}%参照して短縮する。"
                + "現在の硬直は{value:recovery}tick、CDは{value:cooldown}tick。",
            29 => "最も{term:Toxin|毒素}が多い敵から{value:removalPercent}%を取り除き、"
                + "その対象を除く生存敵へ（除去量＋{color:Poison}{value:baseToxin}{/color}（"
                + "{value:rawBaseToxin} ×（100 + {icon:Poison}{value:poison} × "
                + "{value:toxinRatio}%）%））の{value:applicationPercent}%（"
                + "{value:baseApplicationPercent}% + {value:scaledApplicationBasePercent}% ×（100 + "
                + "{icon:Poison}{value:poison} × {value:applicationPoisonRatio}%）%）を、"
                + "最大2体へ均等に分配して付与する。"
                + "敵全員が毒素0なら、先頭へ同じ基礎毒素を付与する。",
            30 => "Battle中の{term:Temperature|気温}を恒久的に"
                + "{color:Ice}{value:temperatureReduction}{/color}（{value:baseValue} ×（100 + "
                + "{icon:Ice}{value:ice} × {value:iceRatio}%）%）低下させる。",
            31 => "HP割合が最も低い味方を{color:Wind}{value:healing}{/color}（"
                + "{value:baseHealing} ×（100 + {icon:Wind}{value:wind} × {value:windRatio}%）%）回復し、"
                + "{value:duration}tickの間、{icon:Wind}風を{value:windBonus}（"
                + "{value:baseWindBonus} ×（100 + {icon:Wind}{value:wind} × {value:windRatio}%）%）、"
                + "Speedを{value:speedBonus}（{value:baseSpeedBonus} ×（100 + "
                + "{icon:Wind}{value:wind} × {value:windRatio}%）%）増加させる。",
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
