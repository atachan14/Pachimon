using Pachimon.Passives;
using Pachimon.Skills;
using UnityEditor;
using UnityEngine;

namespace Pachimon.Editor.UI
{
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

        private static string CreateSkillTemplate(int id) => id switch
        {
            9 => "\u6575\u306E\u6700\u5F8C\u5C3E\u306B{icon:Fire}{color:Fire}{value:damage}{/color}"
                + "\u306E{term:FireDamage|\u708E\u30C0\u30E1\u30FC\u30B8}\u3092\u4E0E\u3048\u308B\u3002"
                + "{icon:Poison}\u6BD2\u306B\u3088\u308B\u8CAB\u901A\u7387\u306F{value:penetration}%\u3002",
            10 => "\u73FE\u5728MN\u3092\u3059\u3079\u3066\u6D88\u8CBB\u3057\u3001\u6575\u306E\u5148\u982D\u306B"
                + "{icon:Aqua}{color:Aqua}{value:damage}{/color}\u306E"
                + "{term:AquaDamage|\u6C34\u30C0\u30E1\u30FC\u30B8}\u3092\u4E0E\u3048\u308B\u3002"
                + "\u6C34\u306E\u6CE2\u52D5\u672C\u4F53\u3060\u3051\u3067\u6226\u95D8\u4E0D\u80FD\u306B\u3067\u304D\u308B\u5834\u5408\u306F\u3001"
                + "\u5FC5\u8981\u306AMN\u306E\u307F\u6D88\u8CBB\u3059\u308B\u3002",
            11 => "\u81EA\u8EAB\u306EHP\u3092{color:Leaf}{value:healingBeforeWeather}{/color}\u56DE\u5FA9\u3059\u308B\u3002"
                + "\u6B63\u306E{term:Temperature|\u6C17\u6E29}\u3067\u5897\u52A0\u3057\u3001"
                + "{term:Rain|\u96E8}\u3067\u6E1B\u5C11\u3059\u308B\u3002",
            12 => "\u6575\u306E\u5148\u982D\u306B{icon:Electric}{color:Electric}{value:electricDamage}{/color}\u306E"
                + "{term:ElectricDamage|\u96FB\u6C17\u30C0\u30E1\u30FC\u30B8}\u3068"
                + "{icon:Aqua}{color:Aqua}{value:aquaDamage}{/color}\u306E"
                + "{term:AquaDamage|\u6C34\u30C0\u30E1\u30FC\u30B8}\u3092\u4E0E\u3048\u3001"
                + "\u5024{value:leakValue}\u306E{term:Leak|\u6F0F\u96FB}\u3092\u4ED8\u4E0E\u3059\u308B\u3002",
            13 => "\u6575\u306E\u6700\u5F8C\u5C3E\u306B{value:stunTicks}tick\u306E"
                + "{term:Stun|Stun}\u3068\u3001\u5024{value:toxinValue}\u306E"
                + "{term:Toxin|\u6BD2\u7D20}\u3092\u4ED8\u4E0E\u3059\u308B\u3002",
            14 => "\u5148\u982D\u306E\u751F\u5B58\u5473\u65B9\u306B{color:Ice}{value:shield}{/color}\u306E"
                + "{term:Shield|Shield}\u3092\u4ED8\u4E0E\u3059\u308B\u3002",
            15 => "\u767A\u751F\u4E2D\u306F{term:Flying|\u98DB\u884C}\u3057\u3001Speed\u304C{value:speed}\u5897\u52A0\u3059\u308B\u3002"
                + "\u767A\u52D5\u6642\u3001\u6575\u306E\u5148\u982D\u306B{icon:Wind}{color:Wind}{value:damage}{/color}\u306E"
                + "{term:WindDamage|\u98A8\u30C0\u30E1\u30FC\u30B8}\u3092\u4E0E\u3048\u308B\u3002",
            16 => "\u6575\u306E\u5148\u982D\u306B{icon:Dragon}{color:Dragon}{value:damage}{/color}\u306E"
                + "{term:DragonDamage|\u7ADC\u30C0\u30E1\u30FC\u30B8}\u3092\u4E0E\u3048\u3001"
                + "{term:OneTwo|\u30EF\u30F3\u30FB\u30C4\u30FC}Value\u3092{value:oneTwoValue}\u7372\u5F97\u3059\u308B\u3002",
            _ => string.Empty,
        };

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
