# Stat Pipeline Refactor

v0.8の新しいSkill・Passiveを実装する前に必要な、Stat計算基盤のリファクタリングをまとめる。

この文書の`Stage`は技術タスク内の区分であり、[v0.8実装手順](../implementation-plan.md)のコンテンツ進行を表す`Phase`とは別に扱う。

## Status

- `Stage A`: 完了
- `Stage B`: 完了

## Stage A: 符号付きStat・端数処理基盤

- [x] 符号付きMultiplierを共通化する
- [x] 属性、Speed、Haste、DamageBonus、ResistBonusの負数を許可する
- [x] Damageの途中計算で端数を維持し、最終Damage確定時に一度だけ切り捨てる
- [x] Timingの負数補正に対応し、完了tick確定時に一度だけ切り上げる
- [x] RestSpotの回復量を切り捨て、正の回復には最低1を保証する
- [x] Damage本処理とPreviewで同じCalculatorを使用する
- [x] Battle StateのSimulation Snapshot上で本処理と同じSkill Resolverを実行する
- [x] 既存Skill経路と境界値の回帰テストを追加する

### 実装済みの責務

- `SignedStatMath`
  - 攻撃側Multiplier
  - 防御側Multiplier
  - StatとDamageの切り捨て
  - tickの切り上げ
- `AttributeDamageCalculator`
  - 途中端数を維持したDamage計算
  - 最終Damageの整数化
- `BattleTickMath`
  - 符号付きSpeed / HasteによるTiming計算
- `RestSpotRecoveryService`
  - RestSpot固有の回復量確定

## Stage B: 共通Stat Calculator・派生補正基盤

- [x] 共通`StatCalculator`を作る
- [x] 直接加算、派生加算、直接乗算、派生乗算を段階処理する
- [x] `StatCalculationResult`と計算内訳を返す
- [x] Run中とBattle中で同じCalculatorを使用する
- [x] 水力発電など、最初の派生Passiveを登録する
- [x] Passive詳細の計算済み実数表示へ接続する

### Stage Bの完了条件

- 非Battle中とBattle中のPachimon Tabが、同じStat計算結果を参照する
- BattleのDamage、回復、Timing計算が同じ最終Statを参照する
- 恒久補正、常時Passive、Battle中の一時補正を入力として切り替えられる
- 最終値と、値を構成した補正の内訳を取得できる
- 派生Passiveの計算結果をPassive詳細へ表示できる

## 実装境界

`SignedStatMath`は個々の数値に対するMultiplierと端数処理を担当する。

`StatCalculator`は複数のStatと補正を受け取り、[Stat Calculation Mechanics](../content/mechanics/stat-calculation.md)の順序で最終Statと内訳を構築する。

この2つを分離し、SkillやPassiveの固有Logicへ計算順序を分散させない。

## Stage B実装メモ

- `EffectivePachimonStats`は`StatCalculationResult`の互換ラッパーとして扱う
- `PachimonStatService`がTrainer補正、Passive補正、Context固有補正を合成する
- Run中のPreview、RestSpot、Itemと、Battle開始時のStat生成は同じServiceを使用する
- 水力発電は`Passive ID 12`へ登録する
- 水力発電はBattle Eventを購読せず、Stat計算時だけ補正を提供する
