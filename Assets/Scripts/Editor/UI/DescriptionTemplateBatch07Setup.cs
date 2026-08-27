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
                var skillTemplate = CreateSkillTemplate(id);
                if (id == 51)
                {
                    skillTemplate += "\u653B\u6483\u3054\u3068\u306B\u30C0\u30E1\u30FC\u30B8Value\u306E"
                        + "{value:pollenRatio}%\uFF08\u73FE\u5728{value:pollen}\uFF09\u306E"
                        + "{term:Pollen|\u82B1\u7C89}\u3092\u4ED8\u4E0E\u3059\u308B\u3002";
                }
                SetTemplate(skills.Get(id), skillTemplate);
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
            if (skills?.Get(49)?.Description?.Contains("晴天") != true
                || skills?.Get(51)?.Description?.Contains("{value:pollen}") != true
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
            49 => "{icon:Fire}に応じて晴天を{color:Fire}{value:temperature}{/color}発生させる。晴天は10tick毎に気温を上げ、湿潤を下げる。",
            50 => "敵の先頭へ{color:Aqua}{value:damage}{/color}（{value:baseDamage} ×（100 + {icon:Aqua}{value:aqua} × {value:damageRatio}%）%）の{icon:Aqua}{color:Aqua}ダメージ{/color}と、値{color:Poison}{value:slow}{/color}（{value:baseSlow} ×（100 + {icon:Poison}{value:poison} × {value:slowRatio}%）%）のSlowを与える。",
            51 => "自陣に値{color:Leaf}{value:value}{/color}（{value:baseValue} ×（100 + {icon:Leaf}{value:leaf} × {value:valueRatio}%）%）の{term:BeatVine|ビートヴァイン}を生成する。{value:interval}tickごとに敵を攻撃する。",
            52 => "値{color:Electric}{value:value}{/color}（{value:baseValue} + {icon:Electric}{value:electric} × {value:valueRatio}%）の天気{term:Thunder|雷}を発生させる。",
            53 => "自陣に初期値{color:Poison}{value:value}{/color}（{value:baseValue} ×（100 + {icon:Poison}{value:poison} × {value:valueRatio}%）%）、最小値{color:Wind}{value:minimumValue}{/color}（{value:baseMinimum} ×（100 + {icon:Poison}{value:poison} × 100% + {icon:Wind}{value:wind} × {value:minimumRatio}%）%）、{value:duration}tick（{value:baseDuration} ×（100 + {icon:Aqua}{value:aqua} × {value:durationRatio}%）%）の{term:PoisonMist|毒の霧}を生成する。現在Value以下の軽減前Damageとなる敵Skill攻撃を回避する。",
            54 => "敵の先頭へ{color:Ice}{value:damage}{/color}（{value:baseDamage} ×（100 + {icon:Ice}{value:ice} × {value:ratio}%）%）の{icon:Ice}{color:Ice}ダメージ{/color}と値{value:chill}（{value:baseChill} ×（100 + {icon:Ice}{value:ice} × {value:ratio}%）%）の冷気を与え、自身へ値{value:shield}（{value:baseShield} ×（100 + {icon:Ice}{value:ice} × {value:ratio}%）%）、{value:duration}tickのShieldを付与する。",
            55 => "{value:hitCount}体へ連鎖し、各対象へ{color:Wind}{value:damage}{/color}（{value:baseDamage} ×（100 + {icon:Wind}{value:wind} × {value:damageRatio}%）%）の{icon:Wind}{color:Wind}ダメージ{/color}と値{value:erosion}（{value:baseErosion} ×（100 + {icon:Wind}{value:wind} × {value:erosionRatio}%）%）の風化を与える。使用後、アドチェインを{value:addChain}得る。",
            56 => "敵の先頭へ{color:Dragon}{value:damage}{/color}（{value:baseDamage} ×（100 + {icon:Dragon}{value:dragon} × {value:damageRatio}%）%）の{icon:Dragon}{color:Dragon}ダメージ{/color}と、{value:knockoutDuration}tickのノックアウトを与える。",
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
