using Pachimon.Passives;
using Pachimon.Skills;
using UnityEditor;
using UnityEngine;

namespace Pachimon.Editor.UI
{
    public static class DescriptionTemplateBatch07Setup
    {
        private const string SkillCatalogPath = "Assets/GameData/Skill/SkillCatalog.asset";
        private const string PassiveCatalogPath = "Assets/GameData/Passive/PassiveCatalog.asset";

        [InitializeOnLoadMethod]
        private static void SetupAfterReload() => EditorApplication.delayCall += TryAutoSetup;

        [MenuItem("Tools/Pachimon/Data/Setup Description Templates 49-56")]
        public static void Setup()
        {
            var skills = AssetDatabase.LoadAssetAtPath<SkillCatalog>(SkillCatalogPath);
            var passives = AssetDatabase.LoadAssetAtPath<PassiveCatalog>(PassiveCatalogPath);
            if (skills == null || passives == null)
            {
                Debug.LogError("SkillCatalog and PassiveCatalog are required.");
                return;
            }

            for (var id = 49; id <= 56; id++)
            {
                SetTemplate(skills.Get(id), CreateSkillTemplate(id));
                SetTemplate(passives.Get(id), CreatePassiveTemplate(id));
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Description templates 49-56 setup completed.");
        }

        private static void TryAutoSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var skills = AssetDatabase.LoadAssetAtPath<SkillCatalog>(SkillCatalogPath);
            var passives = AssetDatabase.LoadAssetAtPath<PassiveCatalog>(PassiveCatalogPath);
            if (skills?.Get(49)?.Description?.Contains("{value:temperature}") != true
                || passives?.Get(49)?.Description?.Contains("{value:increasePercent}") != true)
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
            49 => "Battle中の気温を{color:Fire}{value:temperature}{/color}上昇させる。気温はBattle終了まで減衰しない。",
            50 => "敵の先頭へ{icon:Aqua}{color:Aqua}{value:damage}{/color}の水ダメージと、値{color:Poison}{value:slow}{/color}のSlowを与える。",
            51 => "自陣に値{color:Leaf}{value:value}{/color}の{term:BeatVine|ビートヴァイン}を生成する。{value:interval}tickごとに敵を攻撃する。",
            52 => "値{color:Electric}{value:value}{/color}の天気{term:Thunder|雷}を発生させる。",
            53 => "自陣に初期値{color:Poison}{value:value}{/color}、最小値{color:Wind}{value:minimumValue}{/color}、{value:duration}tickの{term:PoisonMist|毒の霧}を生成する。現在Value以下の軽減前Damageとなる敵Skill攻撃を回避する。",
            54 => "敵の先頭へ{icon:Ice}{color:Ice}{value:damage}{/color}の氷ダメージと値{value:chill}の冷気を与え、自身へ値{value:shield}、{value:duration}tickのShieldを付与する。",
            55 => "{value:hitCount}体へ連鎖し、各対象へ{icon:Wind}{color:Wind}{value:damage}{/color}の風ダメージと値{value:erosion}の風化を与える。使用後、アドチェインを{value:addChain}得る。",
            56 => "敵の先頭へ{icon:Dragon}{color:Dragon}{value:damage}{/color}の竜ダメージと、{value:knockoutDuration}tickのノックアウトを与える。",
            _ => string.Empty,
        };

        private static string CreatePassiveTemplate(int id) => id switch
        {
            49 => "自身が生存中、気温が正ならSpeedが{value:increasePercent}%増加する。",
            50 => "{icon:Poison}毒の{value:percent}%を{icon:Aqua}水へ加算する。現在の加算値は{value:contribution}。",
            51 => "自陣の植物1つにつきDamageBonusが{value:damageBonusPerPlant}増加する。",
            52 => "雷が存在する間、Speedが{value:speedBonus}増加する。",
            53 => "自身のSkillで毒以外の属性ダメージを与えるたび、Battle中の{icon:Poison}毒が{value:poisonGain}増加する。",
            54 => "自身が得るShieldのValueと効果時間が、{icon:Ice}氷の{value:iceScalingPercent}%に応じて増加する。",
            55 => "風ダメージを与えるたび、Battle中のSpeedが{value:speedGain}増加する。",
            56 => "Stunしている敵へ与えるダメージが{value:increasePercent}%増加する。",
            _ => string.Empty,
        };
    }
}
