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
                var skillTemplate = CreateSkillTemplate(id);
                if (id == 43)
                {
                    skillTemplate += "\u5024{value:pollen}"
                        + "\uFF08{value:pollenBaseValue} \u00D7\uFF08100 + {icon:Poison}"
                        + "{value:poison} \u00D7 {value:pollenRatio}%\uFF09%\uFF09\u306E"
                        + "{term:Pollen|\u82B1\u7C89}\u3092\u4ED8\u4E0E\u3059\u308B\u3002";
                }
                SetTemplate(skills.Get(id), skillTemplate);
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
            if (skills?.Get(41)?.Description?.Contains("{value:selfDamage}") != true
                || skills?.Get(43)?.Description?.Contains("{value:pollen}") != true
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

        private static string CreateSkillTemplate(int id)
        {
            if (PenetrationDescriptionTemplates.TryGetSkill(id, out var template))
                return template;
            return id switch
        {
            41 => "自身に{color:Fire}{value:selfDamage}{/color}（{value:selfBaseDamage} × {icon:Fire}）の{icon:Fire}{color:Fire}ダメージ{/color}を与える。自身が生存した場合、敵の先頭に{color:Fire}{value:enemyDamage}{/color}（{value:enemyBaseDamage} × {icon:Fire}）の{icon:Fire}{color:Fire}ダメージ{/color}と{color:Fire}{value:burn}{/color}（{value:baseBurn} × {icon:Fire}）の火傷を与える。",
            42 => "敵の先頭へ{color:Aqua}{value:damage}{/color}（{value:baseDamage} ×（100 + {icon:Aqua}{value:aqua} × {value:damageRatio}%）%）の{icon:Aqua}{color:Aqua}ダメージ{/color}を与える。この攻撃は{value:penetration}（{value:basePenetration} ×（100 + {icon:Wind}{value:wind} × {value:penetrationRatio}%）%）%貫通する。",
            43 => "敵全体へ{value:paralysisDuration}tick（{value:baseDuration} ×（100 + {icon:Leaf}{value:leaf} × {value:durationRatio}%）%）、値{color:Electric}{value:paralysis}{/color}（{value:electricValue}（{value:baseElectricValue} ×（100 + {icon:Electric}{value:electric} × {value:electricRatio}%）%）+ {value:poisonValue}（{value:basePoisonValue} ×（100 + {icon:Poison}{value:poison} × {value:poisonRatio}%）%））の{term:Paralysis|麻痺}を付与する。",
            44 => "{value:startup}tickの発生後、敵の先頭へ{color:Electric}{value:damage}{/color}（{value:baseDamage} ×（100 + {icon:Electric}{value:electric} × {value:damageRatio}%）%）の{icon:Electric}{color:Electric}ダメージ{/color}を与える。超過ダメージは次の敵へ引き継ぐ。",
            45 => "自身へ{color:Poison}{value:shield}{/color}（{value:baseShield} ×（100 + {icon:Poison}{value:poison} × {value:shieldRatio}%）%）の{term:Shield|Shield}を{value:duration}tick付与し、自身の{term:Toxin|毒素}を{value:toxinReductionPercent}（{value:baseReduction} ×（100 + {icon:Poison}{value:poison} × {value:reductionRatio}%）%）%取り除く。",
            46 => "HPが半分以上なら敵の先頭へ{color:Ice}{value:damage}{/color}（{value:baseDamage} ×（100 + {icon:Ice}{value:ice} × {value:damageRatio}%）%）の{icon:Ice}{color:Ice}ダメージ{/color}と{value:duration}tick（{value:baseDuration} + {icon:Ice}{value:ice} × {value:durationRatio}%）の凍結を与える。半分未満なら同じ時間対象外となり、毎tick HPを{value:healingPerTick}（{value:baseHealing} ×（100 + {icon:Ice}{value:ice} × {value:healingRatio}%）%）回復する。",
            47 => "値{color:Wind}{value:value}{/color}（{value:baseValue} + {icon:Wind}{value:wind} × {value:valueRatio}%）の天気{term:WindStorm|暴風}を発生させる。",
            48 => "敵の先頭へ{color:Dragon}{value:damage}{/color}（{value:baseDamage} ×（100 + {icon:Dragon}{value:dragon} × {value:damageRatio}%）%）の{icon:Dragon}{color:Dragon}ダメージ{/color}と、値{value:crankerValue}（{value:baseCranker} + {icon:Dragon}{value:dragon} × {value:crankerRatio}%）の{term:DragonCranker|ドラゴンクランカー}を与える。",
            _ => string.Empty,
            };
        }

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
