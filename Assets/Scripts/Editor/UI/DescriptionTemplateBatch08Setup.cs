using Pachimon.Passives;
using Pachimon.Skills;
using UnityEditor;
using UnityEngine;

namespace Pachimon.Editor.UI
{
    public static class DescriptionTemplateBatch08Setup
    {
        private const string SkillCatalogPath = "Assets/GameData/Skill/SkillCatalog.asset";
        private const string PassiveCatalogPath = "Assets/GameData/Passive/PassiveCatalog.asset";

        [InitializeOnLoadMethod]
        private static void SetupAfterReload() => EditorApplication.delayCall += TryAutoSetup;

        [MenuItem("Tools/Pachimon/Data/Setup Description Templates 57-64")]
        public static void Setup()
        {
            var skills = AssetDatabase.LoadAssetAtPath<SkillCatalog>(SkillCatalogPath);
            var passives = AssetDatabase.LoadAssetAtPath<PassiveCatalog>(PassiveCatalogPath);
            if (skills == null || passives == null)
            {
                Debug.LogError("SkillCatalog and PassiveCatalog are required.");
                return;
            }

            for (var id = 57; id <= 64; id++)
            {
                SetTemplate(skills.Get(id), CreateSkillTemplate(id));
                SetTemplate(passives.Get(id), CreatePassiveTemplate(id));
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Description templates 57-64 setup completed.");
        }

        private static void TryAutoSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var skills = AssetDatabase.LoadAssetAtPath<SkillCatalog>(SkillCatalogPath);
            var passives = AssetDatabase.LoadAssetAtPath<PassiveCatalog>(PassiveCatalogPath);
            if (skills?.Get(57)?.Description?.Contains("{value:weakness}") != true
                || passives?.Get(57)?.Description?.Contains("{value:speedBonus}") != true)
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

        private static string CreateSkillTemplate(int id) => id == 61
            ? "敵の先頭へ{icon:Poison}{color:Poison}{value:damage}{/color}の毒ダメージを与える。対象が最大HP未満なら値{value:normalToxin}の毒素を与える。最大HPなら追加で{value:bonusDamage}ダメージと値{value:toxin}の毒素を与える。"
            : id switch
        {
            57 => "敵の先頭へ{icon:Fire}{color:Fire}{value:damage}{/color}の炎ダメージを与える。この攻撃は{value:penetration}%貫通し、値{value:weakness}の弱点を付与する。",
            58 => "敵の先頭へ{icon:Aqua}{color:Aqua}{value:damage}{/color}の水ダメージを与える。ダメージは現在HP {value:currentHp}に応じて増加する。",
            59 => "自陣に草Value {color:Leaf}{value:leafValue}{/color}、炎Value {color:Fire}{value:fireValue}{/color}の{term:FireVine|ファイアヴァイン}を生成する。",
            60 => "自身へ値{color:Electric}{value:shield}{/color}、{value:duration}tickのShieldと値{value:selfParalysis}の麻痺を付与する。Shield中に攻撃した相手へ値{value:counterParalysis}の麻痺を返す。",
            61 => "敵の先頭へ{icon:Poison}{color:Poison}{value:damage}{/color}の毒ダメージを与える。対象が最大HPなら代わりに{value:bonusDamage}ダメージと値{value:toxin}の毒素を与える。",
            62 => "現在HPが最も低い敵へ{icon:Ice}{color:Ice}{value:damage}{/color}の氷ダメージと値{value:chill}の冷気を与える。撃破時、MNを{value:manaRefund}、CDを{value:cooldownRefund}tick還元する。",
            63 => "敵の先頭へ{icon:Fire}{color:Fire}{value:fireDamage}{/color}の炎、{icon:Aqua}{color:Aqua}{value:aquaDamage}{/color}の水、{icon:Wind}{color:Wind}{value:windDamage}{/color}の風ダメージを与える。",
            64 => "自身へ値{color:Dragon}{value:shield}{/color}、{value:duration}tickのShieldを付与する。効果中は味方が受ける攻撃と状態付与を肩代わりする。",
            _ => string.Empty,
        };

        private static string CreatePassiveTemplate(int id) => id switch
        {
            57 => "弱点を持つ敵へ与えるダメージが{value:increasePercent}%増加し、攻撃後にSpeedが{value:speedBonus}増加する。このSpeed増加は{value:duration}tick続く。",
            58 => "MaxHPの{value:percent}%を{icon:Aqua}水へ加算する。現在の加算値は{value:contribution}。",
            59 => "全陣営で炎ダメージが発生するたびBattle中の{icon:Leaf}草が{value:statGain}、草ダメージが発生するたび{icon:Fire}炎が{value:statGain}増加する。",
            60 => "麻痺Valueの{value:electricRatio}%を{icon:Electric}電気へ加算する。",
            61 => "自身のSkillダメージ後、対象HPが{icon:Poison}毒の{value:executionRatio}%以下なら戦闘不能にする。現在の条件は最大HPの{value:executionPercent}%。",
            62 => "自身のSkillで敵を撃破したとき、その敵の冷気の{value:spreadPercent}%を残りの敵全員へ付与する。",
            63 => "自身のSkillで風以外の属性ダメージを与えるたび、Battle中の{icon:Wind}風が{value:windGain}増加する。",
            64 => "{icon:Dragon}竜の{value:dragonRatio}%をResistBonusへ加算する。現在の加算値は{value:resistBonus}。",
            _ => string.Empty,
        };
    }
}
