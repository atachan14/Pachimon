using Pachimon.Passives;
using Pachimon.Skills;
using UnityEditor;
using UnityEngine;

namespace Pachimon.Editor.UI
{
    public static class DescriptionTemplateBatch03Setup
    {
        private const string SkillCatalogPath =
            "Assets/GameData/Skill/SkillCatalog.asset";
        private const string PassiveCatalogPath =
            "Assets/GameData/Passive/PassiveCatalog.asset";

        [InitializeOnLoadMethod]
        private static void SetupAfterReload() =>
            EditorApplication.delayCall += TryAutoSetup;

        [MenuItem("Tools/Pachimon/Data/Setup Description Templates 17-24")]
        public static void Setup()
        {
            var skills = AssetDatabase.LoadAssetAtPath<SkillCatalog>(SkillCatalogPath);
            var passives = AssetDatabase.LoadAssetAtPath<PassiveCatalog>(PassiveCatalogPath);
            if (skills == null || passives == null)
            {
                Debug.LogError("SkillCatalog and PassiveCatalog are required.");
                return;
            }

            for (var id = 17; id <= 24; id++)
            {
                SetTemplate(skills.Get(id), CreateSkillTemplate(id));
                SetTemplate(passives.Get(id), CreatePassiveTemplate(id));
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Description templates 17-24 setup completed.");
        }

        private static void TryAutoSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            var skills = AssetDatabase.LoadAssetAtPath<SkillCatalog>(SkillCatalogPath);
            var passives = AssetDatabase.LoadAssetAtPath<PassiveCatalog>(PassiveCatalogPath);
            if (skills?.Get(17)?.Description?.Contains("{value:damage}") != true
                || passives?.Get(17)?.Description?.Contains(
                    "{value:damageBonusPerChain}") != true)
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
            17 => "先頭から後方へ{value:hitCount}回連鎖し、初撃で"
                + "{icon:Fire}{color:Fire}{value:damage}{/color}の"
                + "{term:FireDamage|炎ダメージ}を与える。以降は連鎖順に減衰する。"
                + "使用ごとに{term:AddChain|アドチェイン}を{value:addChain}獲得する。",
            18 => "値{color:Aqua}{value:rainValue}{/color}の{term:Rain|雨}を発生させる。",
            19 => "先頭から後方へ{value:hitCount}回連鎖し、初撃で"
                + "{icon:Leaf}{color:Leaf}{value:damage}{/color}の"
                + "{term:LeafDamage|草ダメージ}と値{value:slow}の{term:Slow|Slow}を与える。"
                + "以降は連鎖順に減衰する。使用ごとに{term:AddChain|アドチェイン}を"
                + "{value:addChain}獲得する。",
            20 => "敵の先頭に{icon:Electric}{color:Electric}{value:damage}{/color}の"
                + "{term:ElectricDamage|電気ダメージ}を与える。"
                + "{icon:Fire}炎による貫通率は{value:penetration}%。",
            21 => "敵陣に値{color:Poison}{value:fieldValue}{/color}の"
                + "{term:Smog|スモッグ}を生成する。",
            22 => "敵の先頭に{icon:Ice}{color:Ice}{value:frontDamage}{/color}の"
                + "{term:IceDamage|氷ダメージ}と値{value:frontChill}の{term:Chill|冷気}を与える。"
                + "他の敵には{icon:Ice}{color:Ice}{value:otherDamage}{/color}と"
                + "値{value:otherChill}の冷気を与える。",
            23 => "敵全体へ値{color:Wind}{value:erosionValue}{/color}の"
                + "{term:WindErosion|風化}を付与する。風化はRBをValueだけ減少させ、"
                + "毎tick1減少する。",
            24 => "次に受ける{term:Attack|攻撃}と、それに付随する状態を回避する。",
            _ => string.Empty,
        };

        private static string CreatePassiveTemplate(int id) => id switch
        {
            17 => "Battle中に完了した最大追加連鎖回数1回につき、DamageBonusが"
                + "{value:damageBonusPerChain}増加する。",
            18 => "{term:Rain|雨}のとき、Speedを基本{value:baseSpeedPercent}%にし、"
                + "雨Valueの{value:rainValueRatio}%を追加する。",
            19 => "自身が与える{term:Slow|Slow}のValueを、{icon:Leaf}草に応じて増幅する。"
                + "草の参照率は{value:leafSlowRatio}%。",
            20 => "{icon:Fire}炎の{value:percent}%を{icon:Electric}電気へ加算する。"
                + "現在の加算値は{value:contribution}。",
            21 => "自身が生成物を生成するとき、生成予定Valueを{icon:Poison}毒に応じて増幅する。"
                + "毒の参照率は{value:poisonScalingPercent}%で、現在の倍率は"
                + "{value:currentMultiplier}倍。",
            22 => "対象に付与されている{term:Slow|Slow}に応じて与Damageが増加する。"
                + "Slowの参照率は{value:slowRatio}%。",
            23 => "自身のResistBonusが対象より高い場合、差に応じて与Damageが増加する。"
                + "差の参照率は{value:resistDifferenceRatio}%。",
            24 => "回避に成功するたび、Battle中のSpeedが{value:speedGain}増加する。",
            _ => string.Empty,
        };
    }
}
