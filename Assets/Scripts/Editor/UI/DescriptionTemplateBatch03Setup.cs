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
            if (skills?.Get(17)?.Description?.Contains("{value:baseDamage}") != true
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

        private static string CreateSkillTemplate(int id)
        {
            if (PenetrationDescriptionTemplates.TryGetSkill(id, out var template))
                return template;
            return id switch
        {
            17 => "先頭から後方へ{value:hitCount}回連鎖し、初撃で"
                + "{color:Fire}{value:damage}{/color}（{value:baseDamage} ×（100 + "
                + "{icon:Fire}{value:fire} × {value:damageRatio}%）%）の"
                + "{icon:Fire}{color:Fire}ダメージ{/color}を与える。以降は連鎖順に減衰する。"
                + "使用ごとに{term:AddChain|アドチェイン}を{value:addChain}獲得する。",
            18 => "値{color:Aqua}{value:rainValue}{/color}（{value:baseValue} + "
                + "{icon:Aqua}{value:aqua} × {value:aquaRatio}%）の{term:Rain|雨}を発生させる。",
            19 => "先頭から後方へ{value:hitCount}回連鎖し、初撃で"
                + "{color:Leaf}{value:damage}{/color}（{value:baseDamage} ×（100 + "
                + "{icon:Leaf}{value:leaf} × {value:damageRatio}%）%）の"
                + "{icon:Leaf}{color:Leaf}ダメージ{/color}と、値{value:slow}（{value:baseSlow} ×（100 + "
                + "{icon:Leaf}{value:leaf} × {value:slowRatio}%）%）の{term:Slow|Slow}を与える。"
                + "以降は連鎖順に減衰する。使用ごとに{term:AddChain|アドチェイン}を"
                + "{value:addChain}獲得する。",
            20 => "敵の先頭に{color:Electric}{value:damage}{/color}（{value:baseDamage} ×（100 + "
                + "{icon:Electric}{value:electric} × {value:electricRatio}%）% ×（100 + "
                + "{icon:Fire}{value:fire} × {value:fireRatio}%）%）の"
                + "{icon:Electric}{color:Electric}ダメージ{/color}を与える。貫通率は"
                + "{value:penetration}（{icon:Fire}{value:fire} × {value:basePenetration}%）%。",
            21 => "敵陣に値{color:Poison}{value:fieldValue}{/color}（{value:baseValue} ×（100 + "
                + "{icon:Poison}{value:poison} × {value:ratio}%）%）の"
                + "{term:Smog|スモッグ}を生成する。",
            22 => "敵の先頭に{color:Ice}{value:frontDamage}{/color}（{value:frontBaseDamage} ×（100 + "
                + "{icon:Ice}{value:ice} × {value:frontDamageRatio}%）%）の"
                + "{icon:Ice}{color:Ice}ダメージ{/color}と、値{value:frontChill}（"
                + "{value:frontBaseChill} ×（100 + {icon:Ice}{value:ice} × "
                + "{value:frontChillRatio}%）%）の{term:Chill|冷気}を与える。"
                + "他の敵には{color:Ice}{value:otherDamage}{/color}（{value:otherBaseDamage} ×（100 + "
                + "{icon:Ice}{value:ice} × {value:otherDamageRatio}%）%）の"
                + "{icon:Ice}{color:Ice}ダメージ{/color}と、値{value:otherChill}（"
                + "{value:otherBaseChill} ×（100 + {icon:Ice}{value:ice} × "
                + "{value:otherChillRatio}%）%）の冷気を与える。",
            23 => "敵全体へ値{color:Wind}{value:erosionValue}{/color}（{value:baseValue} ×（100 + "
                + "{icon:Wind}{value:wind} × {value:ratio}%）%）の"
                + "{term:WindErosion|風化}を付与する。風化はRBをValueだけ減少させ、"
                + "毎tick1減少する。",
            24 => "{value:duration}tick（{value:baseDuration} ×（100 + "
                + "{icon:Dragon}{value:dragon} × {value:durationRatio}%）%）の間、次に受ける"
                + "{term:Attack|攻撃}と、それに付随する状態を回避する。",
            _ => string.Empty,
            };
        }

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
