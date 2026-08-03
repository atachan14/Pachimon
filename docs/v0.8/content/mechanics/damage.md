# Damage Mechanics

複数の属性ファイルから参照するダメージ関連の共通仕様をまとめる。

## Formula

- 汎用係数は[Scaling](./scaling.md)を参照する
- 増幅倍率としてStatを参照する場合は`AmplificationMultiplier(Stat)`を使用する
- 割合そのものを表す場合は、式の結果が`%`であることを明記する
- Damage Typeは`Fire / Aqua / Leaf / Electric / Poison / Ice / Wind / Dragon / True`を使用する
- 実装では`DamageContext`が発生源、属性、基礎Damage、貫通率、`IsAttack`を保持する
- `DamageCalculationResult`が軽減前Damage、軽減用Stat、軽減後Damage、最終Damageを保持する

## 攻撃判定

ダメージ生成側が`IsAttack`を明示する。Passive側で`Skill`、`Status`などの発生源から攻撃かどうかを推測しない。

- 属性ダメージは`DamageContext.IsAttack`を使用する
- 確定ダメージは`TrueDamageContext.IsAttack`を使用する
- `IsAttack = true`の場合、HP適用後に`AttackReceivedEvent`を発行する
- 発生源と対象が同じ自傷攻撃も`AttackReceivedEvent`の対象とする
- `AttackReceivedEvent`は属性ダメージと確定ダメージの両方を対象とする
- 実ダメージが0でも攻撃として適用された場合は発行する
- 自傷、状態異常、Itemなどは、個別仕様で攻撃と明記されない限り`IsAttack = false`とする

`AttackReceivedEvent`は次の情報を保持する。

- 攻撃者と対象
- 発生源種別とID
- 属性ダメージか確定ダメージか
- 属性ダメージの場合の属性
- 軽減後Damageと実際のHP減少量

例:

- わるあがきの敵側への確定ダメージ: `IsAttack = true`
- わるあがきの自傷確定ダメージ: `IsAttack = false`
- 漏電による追加ダメージ: `IsAttack = false`

### 攻撃倍率

属性値とDamageBonusは、攻撃側の仕様で参照された場合に[AmplificationMultiplier](./scaling.md#amplificationmultiplier)を使用する。

- 正数は与えるDamageを増加させる
- 負数は与えるDamageを漸減させる
- Statが負数でも倍率は0未満にならない

### 防御倍率

属性値とResistBonusは、防御側で参照された場合に[ReductionMultiplier](./scaling.md#reductionmultiplier)を`DefenseMultiplier`として使用する。

- 正数は受けるDamageを軽減する
- 負数は受けるDamageを線形に増加させる

### 直接Statを参照する効果

StatからDamageを生成する場合は、原則として次の式を使用する。

```text
Damage = BasePower × AmplificationMultiplier(Stat)
```

- `Stat = 0`でも`BasePower`分のDamageを持つ
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

貫通率を持つダメージは、対象の該当属性値とResistBonusによる軽減を、貫通率分だけ減少させて計算する。

```text
軽減計算用属性値 = 対象属性値 × (1 - 貫通率)
軽減計算用ResistBonus = ResistBonus × (1 - 貫通率)
```

- 貫通率に上限は設けない
- 貫通率が100%を超えた場合、軽減計算用属性値とResistBonusは負数になり得る

- 属性値とResistBonusのそれぞれに共通の`DefenseMultiplier`を適用する
- 属性値とResistBonusの防御倍率はそれぞれ適用する
- 防御倍率の途中計算では端数を維持し、最終Damageで一度だけ切り捨てる
- `DamageContext.PenetrationPercent`として実装済み

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
