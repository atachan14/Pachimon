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

## 属性からSubStatへの派生

> 2026-08-22更新: 下記の固定対応表は廃止し、個体ごとの対応へ変更した。

### 個体ごとの属性/SubStat対応

- 各`PachimonInstance`は、8属性と8 SubStatの一対一対応をRun生成時に確定する。
- 同じ個体ではRun中、Battle中、プレビュー中を通して対応を変えない。
- Species初期値データでは、任意の属性/SubStatペアを固定できる。
- 固定されていない残りの属性とSubStatは、重複しないようランダムに組み合わせる。
- 例: Fire攻撃を軸にするSpeciesは`Fire / DamageBonus`、Fire参照Shieldを軸にするSpeciesは`Fire / SustainPower`を固定できる。
- SubStatの最終値は、対応属性と個体ごとの対応率から派生した値へ、Battle中の直接補正を加えて求める。
- 対応率の初期値は全SubStat共通で`100%`とする。
- 靴や勾玉などの恒久効果はSubStatを直接変更せず、対象SubStatの対応率を増加させる。
- `Slow`、火傷、風化などのBattle効果はSubStatを直接変更し、対応属性には影響しない。
- Pachimon Tabは8属性だけを表示し、各属性Iconの横へ対応SubStat Iconを表示する。

8属性は、対応するSubStatへ派生加算される。基礎Ratioはすべて`100%`とする。

| 属性 | 派生先SubStat | 主な役割 |
| --- | --- | --- |
| Fire | DamageBonus | 与Damage増加 |
| Aqua | GenerationPower | 生成物・天候の生成Value増加 |
| Leaf | Haste | CD短縮 |
| Electric | Speed | 発生・硬直の進行速度 |
| Ice | ResistBonus | 被Damage軽減 |
| Wind | SustainPower | HP回復量・Shield量増加 |
| Poison | StatusMastery | 与える状態Value増加 |
| Dragon | StatusResistance | 受ける状態Value軽減 |

PachimonTabでは属性そのもののenum順ではなく、対応するSubStatを基準に次の順で表示する。

| 左枠 | 右枠 |
| --- | --- |
| DamageBonus | ResistBonus |
| StatusMastery | StatusResistance |
| GenerationPower | SustainPower |
| Speed | Haste |

各枠の属性Iconは、個体生成時に決まった属性とSubStatの対応に応じて入れ替わる。

敵対的な状態の付与Valueは、付与者の`StatusMastery`と対象の
`StatusResistance`を対称に適用する。さらに状態ごとの対応属性で軽減する。

```text
最終付与Value
= 基礎Value
  × AmplificationMultiplier(付与者の参照属性 + StatusMastery)
  × ReductionMultiplier(対象の対応属性 + StatusResistance)
```

汎用SubStatとしての貫通は持たず、属性固定値貫通・属性割合貫通・
RB固定値貫通・RB割合貫通はSkillまたはPassive固有効果として扱う。

`GenerationPower`は生成・再生成時に加えるField Effect Valueと、
天候の生成・再付与Valueへ`AmplificationMultiplier(参照属性 + GenerationPower)`を1回適用する。
正負を持つ気温などは符号を維持して絶対量を増幅する。生成物の効果時間だけを表す値や、
生成後に発生するDamage・状態Valueには重ねて適用しない。

`SustainPower`はHP回復量とShield量へ`AmplificationMultiplier(参照属性 + SustainPower)`を1回適用する。
対象が自身か味方かは問わない。生成物による回復にも回復発生時に適用する。

```text
DerivedSubStat
= Attribute * DerivationRatio / 100
  + DirectSubStatModifier
```

- SubStatの基本値は原則`0`とする
- 初期生成、通常のTrainerStatus報酬、Row補正は8属性と`MaxHP / MaxMN`だけを直接変更する
- 装備による対応率補正は恒久的に保持し、Battle中の状態などだけがSubStatを直接変更できる
- 属性への直接加算は派生計算より先に適用されるため、属性の増減は対応SubStatにも反映される

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
- 属性値と全SubStatは負数を許可する
- 実装では、実行環境によって結果が変わる浮動小数点誤差を避ける

最低値の保証は端数処理と分離し、各Statや効果の仕様として適用する。
