using Pachimon.Passives;
using Pachimon.Skills;
using UnityEditor;
using UnityEngine;

namespace Pachimon.Editor.UI
{
    public static class PenetrationDescriptionTemplates
    {
        public static bool TryGetSkill(int skillId, out string template)
        {
            template = skillId switch
            {
                9 => "\u6575\u306E\u6700\u5F8C\u5C3E\u306B{color:Fire}{value:damage}{/color}"
                    + "\u306E{icon:Fire}{color:Fire}\u30C0\u30E1\u30FC\u30B8{/color}\u3092\u4E0E\u3048\u308B\u3002"
                    + "\u3053\u306E\u30C0\u30E1\u30FC\u30B8\u306F{value:penetration}\u306EFire\u56FA\u5B9A\u5024\u8CAB\u901A\u3092\u6301\u3064\u3002",
                20 => "\u6575\u306E\u5148\u982D\u306B{color:Electric}{value:damage}{/color}"
                    + "\u306E{icon:Electric}{color:Electric}\u30C0\u30E1\u30FC\u30B8{/color}\u3092\u4E0E\u3048\u308B\u3002"
                    + "\u3053\u306E\u653B\u6483\u306FElectric\u3092{value:penetration}%\u8CAB\u901A\u3059\u308B"
                    + "\uFF08\u8CAB\u901AValue {value:penetrationValue} = {icon:Fire}{value:fire} \u00D7 {value:penetrationRatio}%\uFF09\u3002",
                42 => "\u6575\u306E\u5148\u982D\u3078{color:Aqua}{value:damage}{/color}"
                    + "\u306E{icon:Aqua}{color:Aqua}\u30C0\u30E1\u30FC\u30B8{/color}\u3092\u4E0E\u3048\u308B\u3002"
                    + "\u3053\u306E\u653B\u6483\u306FAqua\u3092{value:penetration}%\u8CAB\u901A\u3059\u308B"
                    + "\uFF08\u8CAB\u901AValue {value:penetrationValue} = {icon:Wind}{value:wind} \u00D7 {value:penetrationRatio}%\uFF09\u3002",
                57 => "\u6575\u306E\u5148\u982D\u3078{color:Fire}{value:damage}{/color}"
                    + "\u306E{icon:Fire}{color:Fire}\u30C0\u30E1\u30FC\u30B8{/color}\u3092\u4E0E\u3048\u308B\u3002"
                    + "\u3053\u306E\u653B\u6483\u306FFire\u3092{value:penetration}%\u8CAB\u901A\u3059\u308B"
                    + "\uFF08\u8CAB\u901AValue {value:penetrationValue}\uFF09\u3002"
                    + "\u5024{value:weakness}\u306E\u5F31\u70B9\u3092\u4ED8\u4E0E\u3059\u308B\u3002",
                _ => null,
            };
            return template != null;
        }

        public static bool TryGetPassive(int passiveId, out string template)
        {
            template = passiveId == 40
                ? "{icon:Dragon}\u7ADC\u306B\u5FDC\u3058\u3066\u3001\u81EA\u8EAB\u304C\u4E0E\u3048\u308B\u5C5E\u6027\u30C0\u30E1\u30FC\u30B8\u304C"
                  + "\u5BFE\u8C61\u306EResistBonus\u3092\u5272\u5408\u8CAB\u901A\u3059\u308B\u3002"
                  + "\u73FE\u5728\u306E\u8CAB\u901A\u7387\u306F{value:currentPenetration}%"
                  + "\uFF08\u8CAB\u901AValue {value:penetrationValue}\uFF09\u3002"
                : null;
            return template != null;
        }
    }

    public static class DescriptionTemplateBatch02Setup
    {
        private const string SkillCatalogPath =
            "Assets/GameData/Skill/SkillCatalog.asset";
        private const string PassiveCatalogPath =
            "Assets/GameData/Passive/PassiveCatalog.asset";

        [InitializeOnLoadMethod]
        private static void SetupAfterReload() =>
            EditorApplication.delayCall += TryAutoSetup;

        [MenuItem("Tools/Pachimon/Data/Setup Description Templates 09-16")]
        public static void Setup()
        {
            var skills = AssetDatabase.LoadAssetAtPath<SkillCatalog>(SkillCatalogPath);
            var passives = AssetDatabase.LoadAssetAtPath<PassiveCatalog>(PassiveCatalogPath);
            if (skills == null || passives == null)
            {
                Debug.LogError("SkillCatalog and PassiveCatalog are required.");
                return;
            }

            for (var id = 9; id <= 16; id++)
            {
                SetTemplate(skills.Get(id), CreateSkillTemplate(id));
                SetTemplate(passives.Get(id), CreatePassiveTemplate(id));
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Description templates 09-16 setup completed.");
        }

        private static void TryAutoSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            var skills = AssetDatabase.LoadAssetAtPath<SkillCatalog>(SkillCatalogPath);
            var passives = AssetDatabase.LoadAssetAtPath<PassiveCatalog>(PassiveCatalogPath);
            if (skills?.Get(9)?.Description?.Contains("{value:damage}") != true
                || passives?.Get(9)?.Description?.Contains("{value:conversion}") != true)
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
            9 => "\u6575\u306E\u6700\u5F8C\u5C3E\u306B"
                + "{color:Fire}{value:damage}{/color}"
                + "\uFF08{value:baseDamage} \u00D7\uFF08100 + {icon:Fire}{value:fire} \u00D7 {value:damageRatio}%\uFF09%\uFF09\u306E"
                + "{icon:Fire}{color:Fire}\u30C0\u30E1\u30FC\u30B8{/color}\u3092\u4E0E\u3048\u308B\u3002"
                + "\u3053\u306E\u30C0\u30E1\u30FC\u30B8\u306F{value:penetration}%"
                + "\uFF08{value:basePenetration} \u00D7\uFF08100 + {icon:Poison}{value:poison} \u00D7 {value:penetrationRatio}%\uFF09%\uFF09"
                + "\u306E\u8CAB\u901A\u3092\u6301\u3064\u3002",
            10 => "MaxMNの{value:maxMnCostPercent}%（{value:maxMn} × {value:maxMnCostPercent}% = {value:manaCost}）を消費し、敵の先頭に"
                + "{color:Aqua}{value:damage}{/color}（{value:manaCost} × {value:damagePerMana} ×（100 + {icon:Aqua}{value:aqua} × {value:damageRatio}%）%）の"
                + "{icon:Aqua}{color:Aqua}ダメージ{/color}を与える。",
            11 => "自身のHPを{color:Leaf}{value:healingBeforeWeather}{/color}（{value:baseHealing} ×（100 + {icon:Leaf}{value:leaf} × {value:healingRatio}%）%）回復する。"
                + "正の{term:Temperature|気温}で増加し、{term:Rain|雨}で減少する。",
            12 => "敵の先頭に{color:Electric}{value:electricDamage}{/color}（{value:electricBaseDamage} ×（100 + {icon:Electric}{value:electric} × {value:electricRatio}%）%）の"
                + "{icon:Electric}{color:Electric}ダメージ{/color}と、{color:Aqua}{value:aquaDamage}{/color}（{value:aquaBaseDamage} ×（100 + {icon:Aqua}{value:aqua} × {value:aquaRatio}%）%）の"
                + "{icon:Aqua}{color:Aqua}ダメージ{/color}を与え、値{value:leakValue}（{value:leakBaseValue} ×（100 + {icon:Aqua}{value:aqua} × {value:leakRatio}%）%）の{term:Leak|漏電}を付与する。",
            13 => "敵の最後尾に{value:stunTicks}tick（{value:baseElectricStun} ×（100 + {icon:Electric}{value:electric} × {value:electricRatio}%）%）の{term:Stun|Stun}を付与する。"
                + "さらに値{value:toxinValue}（{value:baseToxin} ×（100 + {icon:Poison}{value:poison} × {value:toxinRatio}%）%）の{term:Toxin|毒素}を付与する。",
            14 => "先頭の生存味方に{color:Ice}{value:shield}{/color}（{value:baseShield} ×（100 + {icon:Ice}{value:ice} × {value:shieldRatio}%）%）の{term:Shield|Shield}を付与する。",
            15 => "発生中は{term:Flying|飛行}し、Speedが{value:speed}（{icon:Wind}{value:wind} × {value:speedRatio}%）増加する。"
                + "発動時、敵の先頭に{color:Wind}{value:damage}{/color}（{value:baseDamage} ×（100 + {icon:Wind}{value:wind} × {value:damageRatio}%）%）の"
                + "{icon:Wind}{color:Wind}ダメージ{/color}を与える。",
            16 => "敵の先頭に{color:Dragon}{value:damage}{/color}（{value:baseDamage} ×（100 + {icon:Dragon}{value:dragon} × {value:damageRatio}%）%）の"
                + "{icon:Dragon}{color:Dragon}ダメージ{/color}を与え、{term:OneTwo|ワン・ツー}Valueを{value:oneTwoValue}獲得する。",
            _ => string.Empty,
            };
        }

        private static string CreatePassiveTemplate(int id) => id switch
        {
            9 => "{icon:Fire}{term:FireDamage|\u708E\u30C0\u30E1\u30FC\u30B8}\u3092\u4E0E\u3048\u305F\u3068\u304D\u3001"
                + "\u8EFD\u6E1B\u524DValue\u3092\u57FA\u306B{icon:Poison}{term:PoisonDamage|\u6BD2\u30C0\u30E1\u30FC\u30B8}\u3092\u8FFD\u52A0\u3059\u308B\u3002"
                + "\u73FE\u5728\u306E\u5909\u63DB\u7387\u306F{value:conversion}%\u3002",
            10 => "MN\u3092\u6D88\u8CBB\u3057\u305FSkill\u306E\u52B9\u679C\u89E3\u6C7A\u5F8C\u3001"
                + "\u6D88\u8CBBMN\u306E{value:baseRecoveryRatio}%\u3092\u57FA\u6E96\u306B\u3001"
                + "{icon:Aqua}\u6C34\u306B\u5FDC\u3058\u3066\u81EA\u8EAB\u3092\u56DE\u5FA9\u3059\u308B\u3002",
            11 => "\u81EA\u8EAB\u304C\u53D7\u3051\u308B\u56DE\u5FA9\u52B9\u679C\u304C\u3001"
                + "{icon:Leaf}\u8349\u306B\u5FDC\u3058\u3066\u5897\u52A0\u3059\u308B\u3002"
                + "\u57FA\u6E96\u5897\u52A0\u7387\u306F{value:baseHealingRatio}%\u3002",
            12 => "{icon:Aqua}\u6C34\u306E{value:percent}%\u3092{icon:Electric}\u96FB\u6C17\u3078\u52A0\u7B97\u3059\u308B\u3002"
                + "\u73FE\u5728\u306E\u52A0\u7B97\u5024\u306F{value:contribution}\u3002",
            13 => "\u81EA\u8EAB\u304C{term:Toxin|\u6BD2\u7D20}\u3092\u4ED8\u4E0E\u3059\u308B\u305F\u3073\u3001"
                + "Battle\u4E2D\u306E{icon:Poison}\u6BD2\u304C{value:poisonPercent}%\u5897\u52A0\u3059\u308B\u3002",
            14 => "\u53D7\u3051\u308B{icon:Ice}{term:IceDamage|\u6C37\u30C0\u30E1\u30FC\u30B8}\u304C"
                + "{value:reductionPercent}%\u6E1B\u5C11\u3059\u308B\u3002",
            15 => "Skill\u306E\u57FA\u672C\u767A\u751F\u5024\u306E{value:startupRatio}%\u3060\u3051DamageBonus\u3092\u5F97\u308B\u3002",
            16 => "{icon:Dragon}{term:DragonDamage|\u7ADC\u30C0\u30E1\u30FC\u30B8}\u3092\u4E0E\u3048\u308B\u305F\u3073"
                + "{value:stackGain}Stack\u7372\u5F97\u3057\u30011Stack\u3054\u3068\u306B\u7ADC\u30C0\u30E1\u30FC\u30B8\u304C"
                + "{value:damagePerStack}%\u5897\u52A0\u3059\u308B\u3002\u4ED6\u5C5E\u6027\u30C0\u30E1\u30FC\u30B8\u3067Stack\u304C\u534A\u6E1B\u3059\u308B\u3002",
            _ => string.Empty,
        };
    }
}
