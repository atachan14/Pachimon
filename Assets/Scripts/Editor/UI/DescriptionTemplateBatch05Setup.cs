using Pachimon.Passives;
using Pachimon.Skills;
using UnityEditor;
using UnityEngine;

namespace Pachimon.Editor.UI
{
    public static class DescriptionTemplateBatch05Setup
    {
        private const string SkillCatalogPath =
            "Assets/GameData/Skill/SkillCatalog.asset";
        private const string PassiveCatalogPath =
            "Assets/GameData/Passive/PassiveCatalog.asset";

        [InitializeOnLoadMethod]
        private static void SetupAfterReload() =>
            EditorApplication.delayCall += TryAutoSetup;

        [MenuItem("Tools/Pachimon/Data/Setup Description Templates 33-40")]
        public static void Setup()
        {
            var skills = AssetDatabase.LoadAssetAtPath<SkillCatalog>(SkillCatalogPath);
            var passives = AssetDatabase.LoadAssetAtPath<PassiveCatalog>(PassiveCatalogPath);
            if (skills == null || passives == null)
            {
                Debug.LogError("SkillCatalog and PassiveCatalog are required.");
                return;
            }

            for (var id = 33; id <= 40; id++)
            {
                SetTemplate(skills.Get(id), CreateSkillTemplate(id));
                SetTemplate(passives.Get(id), CreatePassiveTemplate(id));
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Description templates 33-40 setup completed.");
        }

        private static void TryAutoSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var skills = AssetDatabase.LoadAssetAtPath<SkillCatalog>(SkillCatalogPath);
            var passives = AssetDatabase.LoadAssetAtPath<PassiveCatalog>(PassiveCatalogPath);
            if (skills?.Get(33)?.Description?.Contains("{value:damage}") != true
                || passives?.Get(33)?.Description?.Contains(
                    "{value:currentMissingHpPercent}") != true)
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
            33 => "Current HPが最も低い敵へ{icon:Fire}{color:Fire}{value:damage}{/color}の"
                + "{term:FireDamage|炎ダメージ}を与える。戦闘不能にした場合、MNを"
                + "{value:repeatManaCost}消費して次の対象へ再発動する。",
            34 => "味方側に値{color:Aqua}{value:fieldValue}{/color}の"
                + "{term:WaterVeil|水のベール}を生成する。毎tick味方全員を"
                + "{value:healingPerTick}回復し、Valueが{value:decayPerTick}減少する。"
                + "受ける水・炎ダメージを{value:reductionPercent}%軽減する。",
            35 => "敵の先頭と自身を{value:stunTicks}tickの{term:Stun|Stun}にする。",
            36 => "{value:startup}tickの発生開始時に{icon:Electric}電気を保存して"
                + "値{color:Electric}{value:chargeValue}{/color}の{term:Charging|充電中}になり、"
                + "発動時に同じValueの{term:Charged|充電完了}になる。",
            37 => "最も{term:Toxin|毒素}が多い敵の毒素をすべて消費し、敵全体へ"
                + "Stat由来の{icon:Poison}{color:Poison}{value:fixedDamage}{/color}に"
                + "消費Valueの{value:toxinConversion}%を加えた毒ダメージを与える。",
            38 => "自陣に{value:duration}tickの{term:IceBlade|氷の刃}を生成する。"
                + "敵へ冷気を付与するたび、その付与Valueと同値の氷ダメージを追加する。",
            39 => "自身に{color:Wind}{value:shield}{/color}のShieldを付与し、"
                + "{value:duration}tickの間{term:StillAir|無風}になって風を0にする。",
            40 => "先頭の敵のShieldをすべて破壊し、"
                + "{icon:Dragon}{color:Dragon}{value:damage}{/color}の"
                + "{term:DragonDamage|竜ダメージ}を与える。",
            _ => string.Empty,
        };

        private static string CreatePassiveTemplate(int id) => id switch
        {
            33 => "Skillダメージを与えたとき、対象の減少HPの"
                + "{value:currentMissingHpPercent}%を基礎値とする追加の"
                + "{icon:Fire}{term:FireDamage|炎ダメージ}を与える。",
            34 => "自身が生存中、味方全体のHP回復量が{icon:Aqua}水に応じて増加する。"
                + "現在の増加率は{value:currentHealingPercent}%。",
            35 => "自身が発生中または{term:Stun|Stun}中、{icon:Leaf}草の"
                + "{value:leafResistRatio}%をResistBonusへ加算する。"
                + "現在の加算値は{value:currentResistBonus}。",
            36 => "攻撃を受けるたび攻撃者へ値{value:paralysisValue}の"
                + "{term:Paralysis|麻痺}を付与する。電気基礎値{value:electricBaseValue}と"
                + "氷基礎値{value:iceBaseValue}を各Statで増幅して合計する。",
            37 => "{icon:Fire}炎の{value:percent}%を{icon:Poison}毒へ加算する。"
                + "現在の加算値は{value:contribution}。",
            38 => "氷ダメージが発生するたび、Battle中の{icon:Ice}氷が"
                + "{value:iceIncrease}増加する。",
            39 => "自身がShieldを得たとき、他の生存味方へValueの"
                + "{value:shieldPercent}%、効果時間の{value:durationPercent}%のShieldを付与する。",
            40 => "自身が与える属性ダメージの貫通率に{icon:Dragon}竜の"
                + "{value:penetrationRatio}%を加算する。現在の加算値は"
                + "{value:currentPenetration}%。",
            _ => string.Empty,
        };
    }
}
