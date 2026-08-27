# Damage Mechanics

複数の属性ファイルから参照するダメージ関連の共通仕様をまとめる。

## Formula

- 汎用係数は[Scaling](./scaling.md)を参照する
- 増幅倍率としてStatを参照する場合は`AmplificationMultiplier(Stat)`を使用する
- 割合そのものを表す場合は、式の結果が`%`であることを明記する
- Damage Typeは`Fire / Aqua / Leaf / Electric / Poison / Ice / Wind / Dragon / True`を使用する
- 実装では`DamageContext`が発生源、属性、基礎Damage、`DamagePenetration`、`IsAttack`を保持する
- `DamageCalculationResult`が軽減前Damage、軽減用Stat、軽減後Damage、最終Damageを保持する

## 攻撃判定

ダメージ生成側が`IsAttack`を明示する。Passive側で`Skill`、`Status`などの発生源から攻撃かどうかを推測しない。

- 属性ダメージは`DamageContext.IsAttack`を使用する
- 確定ダメージは`TrueDamageContext.IsAttack`を使用する
- `IsAttack = true`の場合、HP適用後に`AttackReceivedEvent`を発行する
- 発生源と対象が同じ自傷攻撃も`AttackReceivedEvent`の対象とする
- `AttackReceivedEvent`は属性ダメージと確定ダメージの両方を対象とする
- 実ダメージが0でも攻撃として適用された場合は発行する
- 自傷、状態、Itemなどは、個別仕様で攻撃と明記されない限り`IsAttack = false`とする

`AttackReceivedEvent`は次の情報を保持する。

- 攻撃者と対象
- 発生源種別とID
- 属性ダメージか確定ダメージか
- 属性ダメージの場合の属性
- 軽減後Damageと実際のHP減少量

例:

- わるあがきの敵側への確定ダメージ: `IsAttack = true`
- わるあがきの自傷確定ダメージ: `IsAttack = false`
- 漏電による追加ダメージ: `Origin = Status / Leak`、`Source = null`、`IsAttack = false`
  - 攻撃者のAttribute、DamageBonus、送出Passiveは適用しない
  - 対象のElectricとResistBonusによる軽減は適用する

### 攻撃倍率

属性値とDamageBonusは先に加算し、攻撃側の仕様で参照された場合に`AmplificationMultiplier(属性値 + DamageBonus)`を1回使用する。

- 正数は与えるDamageを増加させる
- 負数は与えるDamageを漸減させる
- Statが負数でも倍率は0未満にならない

### 防御倍率

属性値とResistBonusは貫通を個別に適用してから加算し、`ReductionMultiplier(軽減後属性値 + 軽減後ResistBonus)`を`DefenseMultiplier`として1回使用する。

- 正数は受けるDamageを軽減する
- 負数は受けるDamageを線形に増加させる

### 状態Damage

- 状態Damageも原則として属性またはTrueのDamage Typeを持つ
- 状態そのものをDamage Originとし、発生源Unitを持たないDamageを許可する
- 属性を持つ状態Damageには、対象側の同属性Statと`ResistBonus`による軽減を適用する
- 状態Valueを付与時の攻撃側Statから生成した場合、発動時に同じ攻撃側Statを再適用しない
- 発動時の`DamageBonus`や与Damage Passiveは、状態固有仕様で明記された場合だけ適用する
- 状態Damageは、固有仕様で攻撃と明記されない限り`IsAttack = false`とする
- Trueの状態Damageは属性Statと`ResistBonus`による軽減を受けない

### 直接Statを参照する効果

StatからDamageを生成する場合は、原則として次の式を使用する。

```text
Damage = BaseDamage × AmplificationMultiplier(Stat)
```

- `Stat = 0`でも`BaseDamage`分のDamageを持つ
- 複数Statを別々のDamage成分へ変換する場合は、成分ごとに計算する
- 複数Statを同じDamageへ乗算する場合は、各Statの`AmplificationMultiplier`を乗算する
- スナップショット、割合Damage、スタック由来Damageなどは個別仕様を優先する

## 端数処理

- Skill固有式、攻撃側補正、防御側軽減を含む途中計算では端数を維持する
- 最終Damageを整数として確定するときに一度だけ切り捨てる
- 最低Damage保証は端数処理と分離し、SkillまたはDamage Effectごとに指定する
- 既存の通常属性Damageは最低1とする
- `わるあがき`など、0 Damageを明示的に許可する効果には最低保証を適用しない

## 貫通

貫通は対象と方式を分けて扱う。

- 属性固定値貫通：Damageと同じ属性の防御値を固定値で減少
- 属性割合貫通：Damageと同じ属性の正の防御値を割合で減少
- RB固定値貫通：ResistBonusを固定値で減少
- RB割合貫通：正のResistBonusを割合で減少

割合貫通は、まず参照StatとSkill SOのRatioから貫通Valueを作り、次の漸減式で実効率へ変換する。

```text
貫通Value = 参照Stat × Ratio / 100
貫通率 = 貫通Value / (100 + 貫通Value)

例：参照Stat 200、Ratio 25%
貫通Value = 50
貫通率 = 50 / 150 = 33.33...%
```

- 割合貫通は100%へ漸近し、100%以上にはならない
- 算出した貫通Valueが負数の場合は0として扱う
- 複数の割合貫通は`1 - Π(1 - 各貫通率)`で乗算合成する
- 割合貫通は正の防御値にのみ適用し、元から負の防御値を変化させない
- 固定値貫通は上限を持たず、適用後の防御値が負数になることを許容する

```text
軽減計算用防御値
= min(0, 元の防御値)
  + max(0, 元の防御値) × (1 - 割合貫通率)
  - 固定値貫通
```

- 属性値とResistBonusへ貫通を個別に適用した後、両者を加算する
- 加算した防御値へ共通の`DefenseMultiplier`を1回適用する
- 防御倍率の途中計算では端数を維持し、最終Damageで一度だけ切り捨てる
- 汎用SubStatとしての貫通は持たない
- Skill・Passive固有の貫通を`DamageContext.Penetration`として適用する

## 超過ダメージ

対象を戦闘不能にするために必要な値を超えたダメージ。

```text
超過ダメージ
= 対象への軽減後Damage - 対象のCurrentHP
```

- 超過ダメージが正の場合、同じDamage Typeのまま次の対象へ渡す
- 攻撃側のStat、DamageBonus、Skill倍率など、計算済みの攻撃側補正は再適用しない
- 次の対象の属性値とResistBonusによる軽減を改めて適用する
- 軽減後も超過ダメージが残る場合、さらに次の対象へ連続して引き継ぐ
- 超過ダメージが0以下になるか、次の対象が存在しなくなった時点で終了する

## Shield

- Shieldは原則として属性Damage、TrueDamage、継続Damageをすべて吸収する
- TrueDamageは属性・Resist軽減を受けないが、Shieldは無視しない
- Damage軽減計算後、HPへ適用する直前にShieldへDamageを適用する
- 複数Shieldがある場合、残り時間が短いものから消費する
- 残り時間が同じ場合、無期限同士を含めて先に付与されたものから消費する
- Shieldを超過したDamageはHPへ引き継ぐ
- 再付与時は既存Shieldへ合算せず、独立したShield Instanceとして追加する
- 戦闘不能時とBattle終了時に、そのPachimonが保持するShieldをすべて破棄する
