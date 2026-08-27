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
            if (skills?.Get(57)?.Description?.Contains("{value:baseFireDamage}") != true
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

        private static string CreateSkillTemplate(int id)
        {
            if (PenetrationDescriptionTemplates.TryGetSkill(id, out var template))
                return template;
            return id switch
        {
            57 => "敵の先頭へ{color:Fire}{value:damage}{/color}（{value:baseFireDamage} ×（100 + {icon:Fire}{value:fire} × {value:fireDamageRatio}%）% + {value:baseAquaDamage} ×（100 + {icon:Aqua}{value:aqua} × {value:aquaDamageRatio}%）%）の{icon:Fire}{color:Fire}ダメージ{/color}を与える。この攻撃は{value:penetration}%貫通し、値{value:weakness}の弱点を付与する。",
            58 => "敵の先頭へ{color:Aqua}{value:damage}{/color}（{value:baseDamage} ×（（100 + {icon:Aqua}{value:aqua} × {value:damageRatio}%）% + Current HP {value:currentHp} ÷ {value:hpDivisor}））の{icon:Aqua}{color:Aqua}ダメージ{/color}を与える。",
            59 => "自陣に草Value {color:Leaf}{value:leafValue}{/color}（{value:baseLeafValue} ×（100 + {icon:Leaf}{value:leaf} × {value:leafRatio}%）%）、炎Value {color:Fire}{value:fireValue}{/color}（{value:baseFireValue} ×（100 + {icon:Fire}{value:fire} × {value:fireRatio}%）%）の{term:FireVine|ファイアヴァイン}を生成する。",
            60 => "自身へ値{color:Electric}{value:shield}{/color}（{value:baseShield} ×（100 + {icon:Electric}{value:electric} × {value:shieldRatio}%）%）、{value:duration}tickのShieldと値{value:selfParalysis}（{value:baseSelfParalysis} ×（100 + {icon:Electric}{value:electric} × {value:selfRatio}%）%）の麻痺を付与する。Shield中に攻撃した相手へ{value:counterParalysisDuration}tick（{value:baseCounterDuration} ×（100 + {icon:Ice}{value:ice} × {value:counterDurationRatio}%）%）、値{value:counterParalysis}（{value:baseCounterParalysis} ×（100 + {icon:Electric}{value:electric} × {value:counterRatio}%）%）の麻痺を返す。",
            61 => "敵の先頭へ{color:Poison}{value:damage}{/color}（{value:baseDamage} ×（100 + {icon:Poison}{value:poison} × {value:ratio}%）%）の{icon:Poison}{color:Poison}ダメージ{/color}を与える。対象が最大HP未満なら値{value:normalToxin}（{value:baseNormalToxin} ×（100 + {icon:Poison}{value:poison} × {value:ratio}%）%）の毒素を与える。最大HPなら追加で{value:bonusDamage}（{value:baseBonusDamage} ×（100 + {icon:Poison}{value:poison} × {value:ratio}%）%）ダメージと値{value:toxin}（{value:baseToxin} ×（100 + {icon:Poison}{value:poison} × {value:ratio}%）%）の毒素を与える。",
            62 => "現在HPが最も低い敵へ{color:Ice}{value:damage}{/color}（{value:baseDamage} ×（100 + {icon:Ice}{value:ice} × {value:ratio}%）%）の{icon:Ice}{color:Ice}ダメージ{/color}と値{value:chill}（{value:baseChill} ×（100 + {icon:Ice}{value:ice} × {value:ratio}%）%）の冷気を与える。撃破時、MNを{value:manaRefund}、CDを{value:cooldownRefund}tick還元する。",
            63 => "敵の先頭へ{color:Fire}{value:fireDamage}{/color}（{value:baseFireDamage} ×（100 + {icon:Fire}{value:fire} × {value:fireRatio}%）%）の{icon:Fire}{color:Fire}ダメージ{/color}、{color:Aqua}{value:aquaDamage}{/color}（{value:baseAquaDamage} ×（100 + {icon:Aqua}{value:aqua} × {value:aquaRatio}%）%）の{icon:Aqua}{color:Aqua}ダメージ{/color}、{color:Leaf}{value:leafDamage}{/color}（{value:baseLeafDamage} ×（100 + {icon:Leaf}{value:leaf} × {value:leafRatio}%）%）の{icon:Leaf}{color:Leaf}ダメージ{/color}、{color:Wind}{value:windDamage}{/color}（{value:baseWindDamage} ×（100 + {icon:Wind}{value:wind} × {value:windRatio}%）%）の{icon:Wind}{color:Wind}ダメージ{/color}を与える。",
            64 => "自身へ値{color:Dragon}{value:shield}{/color}（{value:baseShield} ×（100 + {icon:Dragon}{value:dragon} × {value:shieldRatio}%）%）、{value:duration}tickのShieldを付与する。効果中は味方が受ける攻撃と状態付与を肩代わりする。",
            _ => string.Empty,
            };
        }

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
