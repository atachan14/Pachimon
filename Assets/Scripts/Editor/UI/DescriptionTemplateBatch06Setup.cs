using Pachimon.Passives;
using Pachimon.Skills;
using UnityEditor;
using UnityEngine;

namespace Pachimon.Editor.UI
{
    public static class DescriptionTemplateBatch06Setup
    {
        private const string SkillCatalogPath = "Assets/GameData/Skill/SkillCatalog.asset";
        private const string PassiveCatalogPath = "Assets/GameData/Passive/PassiveCatalog.asset";

        [InitializeOnLoadMethod]
        private static void SetupAfterReload() => EditorApplication.delayCall += TryAutoSetup;

        [MenuItem("Tools/Pachimon/Data/Setup Description Templates 41-48")]
        public static void Setup()
        {
            var skills = AssetDatabase.LoadAssetAtPath<SkillCatalog>(SkillCatalogPath);
            var passives = AssetDatabase.LoadAssetAtPath<PassiveCatalog>(PassiveCatalogPath);
            if (skills == null || passives == null)
            {
                Debug.LogError("SkillCatalog and PassiveCatalog are required.");
                return;
            }

            for (var id = 41; id <= 48; id++)
            {
                SetTemplate(skills.Get(id), CreateSkillTemplate(id));
                SetTemplate(passives.Get(id), CreatePassiveTemplate(id));
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Description templates 41-48 setup completed.");
        }

        private static void TryAutoSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var skills = AssetDatabase.LoadAssetAtPath<SkillCatalog>(SkillCatalogPath);
            var passives = AssetDatabase.LoadAssetAtPath<PassiveCatalog>(PassiveCatalogPath);
            if (skills?.Get(41)?.Description?.Contains("{value:damage}") != true
                || passives?.Get(41)?.Description?.Contains("{value:fireIncrease}") != true)
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
            41 => "敵の先頭と自身へ、それぞれ{icon:Fire}{color:Fire}{value:damage}{/color}の炎ダメージを与える。両者が生存している間は、MNを追加消費せず再発動する。",
            42 => "敵の先頭へ{icon:Aqua}{color:Aqua}{value:damage}{/color}の水ダメージを与える。この攻撃は{value:penetration}%貫通する。",
            43 => "敵全体へ値{color:Electric}{value:paralysis}{/color}の{term:Paralysis|麻痺}を付与する。",
            44 => "{value:startup}tickの発生後、敵の先頭へ{icon:Electric}{color:Electric}{value:damage}{/color}の電気ダメージを与える。超過ダメージは次の敵へ引き継ぐ。",
            45 => "自身へ{color:Poison}{value:shield}{/color}の{term:Shield|Shield}を{value:duration}tick付与し、自身の{term:Toxin|毒素}を{value:toxinReductionPercent}%取り除く。",
            46 => "HPが半分以上なら敵の先頭へ{icon:Ice}{color:Ice}{value:damage}{/color}の氷ダメージと{value:duration}tickの凍結を与える。半分未満なら同じ時間対象外となり、毎tick HPを{value:healingPerTick}回復する。",
            47 => "値{color:Wind}{value:value}{/color}の天気{term:WindStorm|暴風}を発生させる。",
            48 => "敵の先頭へ{icon:Dragon}{color:Dragon}{value:damage}{/color}の竜ダメージと、値{value:crankerValue}の{term:DragonCranker|ドラゴンクランカー}を与える。",
            _ => string.Empty,
        };

        private static string CreatePassiveTemplate(int id) => id switch
        {
            41 => "ダメージを受けるたび、Battle中の{icon:Fire}炎が{value:fireIncrease}増加する。",
            42 => "自身のSkillで敵を戦闘不能にしたとき、硬直せず続けてTurnを行う。",
            43 => "自身のSkillが敵へ状態を付与するたび、Battle中の{icon:Leaf}草が{value:leafIncrease}増加する。",
            44 => "電気ダメージが発生するたび蓄電を1スタック得る。次の電気ダメージで全スタックを消費し、1スタックにつきダメージが{value:increasePercent}%増加する。",
            45 => "自身が得たShieldとHP回復の{value:sharePercent}%を、他の味方にも与える。",
            46 => "自身が生存中に敵が戦闘不能になるたび、残りの敵へ合計{icon:Ice}{color:Ice}{value:damage}{/color}の氷ダメージを分散して与える。",
            47 => "発動中の天気1種類につきDamageBonusが{value:damageBonusPerWeather}増加する。",
            48 => "ドラゴンクランカーを受けている敵へ与えるダメージが{value:increasePercent}%増加する。",
            _ => string.Empty,
        };
    }
}
