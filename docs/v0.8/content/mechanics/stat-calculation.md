# Stat Calculation Mechanics

Pachimonの最終Statと、その計算内訳を求める共通仕様をまとめる。

## 補正の分類

- `直接加算補正`: 他Statを参照せず、固定値を加算する補正
- `派生加算補正`: 他Statを参照して加算値を求める補正
- `直接乗算補正`: 他Statを参照せず、固定倍率を適用する補正
- `派生乗算補正`: 他Statを参照して倍率を求める補正

補正元がPassive、Item、Badge、Skill、状態のどれであるかにかかわらず、同じ計算段階へ分類する。
各補正は、補正元の固有仕様として算出値の下限・上限を持つことができる。

## 計算順序

1. 基本Statへすべての直接加算補正を加え、`直接加算後Stat`を求める
2. 全派生加算補正を`直接加算後Stat`から一斉に計算する
3. 派生加算補正を加え、`加算完了Stat`を求める
4. 全派生乗算補正を`加算完了Stat`から一斉に計算する
5. 直接乗算補正と派生乗算補正をすべて適用し、`最終Stat`を求める
6. 最終Statと計算内訳を返す

```text
DirectAdditiveStats
= BaseStats + DirectAdditiveModifiers

AdditiveStats
= DirectAdditiveStats
+ DerivedAdditiveModifiers(DirectAdditiveStats)

FinalStats
= AdditiveStats
* DirectMultipliers
* DerivedMultipliers(AdditiveStats)
```

## 一斉計算

- 各計算段階では全Statのスナップショットを参照する
- 同じ段階に属する補正同士は互いの計算結果を参照しない
- 派生加算補正は、別の派生加算補正を参照しない
- 派生乗算補正は、別の派生乗算補正を参照しない
- 状態などによる一時的な直接加算補正も、派生加算補正の参照対象になる

## 利用範囲

同じStat計算結果を次の用途で共有する。

- 非Battle中のPachimon Tab
- Battle中のPachimon Tab
- Battleのダメージ・回復・Timing計算
- Skill / Passive詳細の計算済み実数
- Stat hover時の計算式と内訳

Battle外では恒久補正と常時Passiveを反映する。
Battle中はそれらに加え、Battle中のSkill、Item、状態による補正を反映する。

## 計算内訳

最終値だけでなく、各補正の発生元、演算種別、値を計算内訳として保持する。

```text
StatContribution
- SourceType
- SourceId
- DisplayName
- Operation
- Value
```

## 端数処理

- 派生補正と乗算補正の途中計算では端数を維持する
- 同じ段階に属する補正を個別に整数化しない
- すべての加算と乗算が完了し、最終Statを整数として確定するときに一度だけ切り捨てる
- MaxHPとMaxMNは0を最低値とする
- 属性値、Speed、Haste、DamageBonus、ResistBonusは負数を許可する
- 実装では、実行環境によって結果が変わる浮動小数点誤差を避ける

最低値の保証は端数処理と分離し、各Statや効果の仕様として適用する。
