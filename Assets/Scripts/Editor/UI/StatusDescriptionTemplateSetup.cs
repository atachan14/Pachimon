using Pachimon.Battle;
using UnityEditor;
using UnityEngine;

namespace Pachimon.Editor.UI
{
    public static class StatusDescriptionTemplateSetup
    {
        private const string Root = "Assets/GameData/Battle/Status/";

        [InitializeOnLoadMethod]
        private static void SetupAfterReload() => EditorApplication.delayCall += TryAutoSetup;

        [MenuItem("Tools/Pachimon/Data/Setup Status Description Templates")]
        public static void Setup()
        {
            Set("BurnStatus.asset", "DamageBonusを{value:totalValue}減少させ、自身のTurn終了時に消滅する。");
            Set("ChillStatus.asset", "Speedを{value:totalValue}減少させ、毎tick Valueが{value:decayPerTick}減少する。付与Valueは氷で軽減される。");
            Set("ParalysisStatus.asset", "Speedを{value:totalValue}減少させ、毎tick Valueが{value:decayPerTick}減少する。付与Valueは電気で軽減される。");
            Set("SlowStatus.asset", "Speedを{value:totalValue}減少させ、毎tick Valueが{value:decayPerTick}減少する。");
            Set("ToxinStatus.asset", "毎tick、軽減前{color:Poison}{value:damagePerTick}{/color}の毒ダメージを与え、Valueを{value:decayPerTick}減少させる。ダメージは現在Valueの{value:damagePerTickRatio}%を蓄積して計算する。");
            Set("FreezeStatus.asset", "効果中はActionGaugeの進行を停止する。炎ダメージ{value:fireDamagePerDecay}につきValueが1減少する。現在Valueは{value:value}。");
            Set("KnockoutStatus.asset", "Stunとして扱い、ダメージを受けるたび、そのダメージの{value:damageDurationRatio}%だけ残り時間が延長される。");
            Set("FlyingStatus.asset", "対象指定不可になり、風の{value:windSpeedRatio}%をSpeedへ加算する。");
            Set("LaunchCeremonyStatus.asset", "次のSkill解決まで水を{value:aquaMultiplier}%にし、水に応じてMN消費を軽減する。MN軽減Ratioは{value:manaReductionRatio}%。");
            Set("OneTwoStatus.asset", "次に使用するSkillの発生と硬直を{value:value}%に応じて軽減し、Skill解決後に消費する。");
            Set("DragonBoxerStatus.asset", "竜ダメージが合計スタックValue {value:totalValue}%に応じて増加する。");
            Set("DragonCrankerStatus.asset", "次に受ける竜ダメージが{value:value}%増加し、適用後に消費される。");
            Set("DragonDanceStatus.asset", "Battle中、竜が{value:dragonBonus}、Speedが{value:speedBonus}増加する。");
            Set("ElectricShieldStatus.asset", "Shieldの持続中に攻撃を受けると、攻撃者へ値{value:value}の麻痺を付与する。");
            Set("FrozenBreakStatus.asset", "{value:remainingTicks}tickの間、Stun・対象指定不可となり、毎tick HPを{value:healingPerTick}回復する。");
            Set("HealingWindStatus.asset", "風が{value:windBonus}、Speedが{value:speedBonus}増加する。残り{value:remainingTicks}tick。");
            Set("WeaknessStatus.asset", "次に受ける属性ダメージが{value:value}%増加し、適用後に消費される。");
            Set("WindErosionStatus.asset", "ResistBonusを{value:totalValue}減少させ、毎tick Valueが{value:decayPerTick}減少する。");
            Set("WeaklingBullySpeedStatus.asset", "Speedが{value:value}増加する。残り{value:remainingTicks}tick。");
            Set("SweetScienceStatus.asset", "回避に成功するたびSpeedが増加する。現在の増加値は{value:totalValue}。");
            Set("BurningFlowerLeafGrowth.asset", "炎ダメージが発生するたび草が増加する。現在の増加値は{value:totalValue}。");
            Set("BurningFlowerFireGrowth.asset", "草ダメージが発生するたび炎が増加する。現在の増加値は{value:totalValue}。");
            Set("PoisonMagicianGrowthStatus.asset", "毒以外の属性Skillダメージを与えるたび毒が増加する。現在の増加値は{value:totalValue}。");
            Set("WindRiderGrowthStatus.asset", "風ダメージを与えるたびSpeedが増加する。現在の増加値は{value:totalValue}。");
            Set("WindMagicianGrowthStatus.asset", "風以外の属性ダメージを与えるたび風が増加する。現在の増加値は{value:totalValue}。");

            var charge = AssetDatabase.LoadAssetAtPath<ChargeStatusAsset>(
                Root + "ChargeStatus.asset");
            if (charge != null)
            {
                charge.SetPhaseDescriptionTemplatesForEditor(
                    "充電中。ResistBonusが{value:resistBonus}増加し、電気が{value:electricMultiplier}%になる。保存Valueは{value:value}。",
                    "充電完了。Speedが{value:speedBonus}増加し、電気が{value:electricMultiplier}%になる。残り{value:remainingTicks}tick。");
                EditorUtility.SetDirty(charge);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Status description templates setup completed.");
        }

        private static void TryAutoSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var burn = AssetDatabase.LoadAssetAtPath<BattleStatusAsset>(
                Root + "BurnStatus.asset");
            var charge = AssetDatabase.LoadAssetAtPath<ChargeStatusAsset>(
                Root + "ChargeStatus.asset");
            if (burn?.Description?.Contains("{value:totalValue}") != true
                || charge?.ChargingDescription?.Contains("{value:resistBonus}") != true)
            {
                Setup();
            }
        }

        private static void Set(string fileName, string template)
        {
            var asset = AssetDatabase.LoadAssetAtPath<BattleStatusAsset>(Root + fileName);
            if (asset == null)
            {
                Debug.LogWarning($"Status asset was not found: {fileName}");
                return;
            }

            asset.SetDescriptionTemplateForEditor(template);
            EditorUtility.SetDirty(asset);
        }
    }
}
