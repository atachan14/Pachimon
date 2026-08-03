# Skill Runtime

## 共通方針

- SkillAssetは静的定義であり、Battle中Stateを保持しない
- 使用可否、対象、効果はSkill Logicが決める
- 対象選択UIは持たない
- CooldownはUnitごと、Skill IDごとに保持する
- SkillごとのMN消費と使用条件は個別Logicが決める
- 現行の基本SkillはMNを消費しない
- 対象はボタン表示時ではなく、Skill効果を解決する時点で決める
- `SkillLogicRegistry`がSkill IDとC# Logicを接続する
- ID 1-151はAllocation Typeからv0.3基本Logicへ接続し、後から`RegisterOrReplace`でID単位の固有Logicへ差し替えられる
- `BattleSkillRuntime`が使用可否確認、発生予約、Logic解決、Cooldownと次Turnの予約を担当する

## Target Query

Battle側は個別Skillから使える検索Helperだけを提供する。

```text
GetSelf()
GetFrontEnemy()
GetBackEnemy()
GetAllEnemies()
GetFrontAlly()
GetAlliesBehind(user)
GetAlliesAhead(user)
```

Skill共通のTarget Type Enumへ全挙動を固定しない。全体攻撃、最後尾攻撃、味方対象、特殊な複数対象は個別LogicがQueryを組み合わせて決める。

貫通攻撃はFormation順に解決する。空Slotを飛ばすか、空SlotでもDamageを減衰するかは各Skill Logicが決める。

実装上は`BattleTargetQuery`がFormation検索を公開し、LogicはUIやGameObjectを参照しない。

## Damage

```text
RawDamage
= Skill Logicが攻撃側Statから算出

AfterDamageBonus
= RawDamage * OffenseMultiplier(AttackerDamageBonus)

AfterAttribute
= AfterDamageBonus * DefenseMultiplier(DefenderAttribute)

FinalDamage
= floor(AfterAttribute * DefenseMultiplier(DefenderResistBonus))
```

- 攻撃側が参照するStatはSkill Logicが決める
- v0.3基本属性Skillは使用者の対応属性値を攻撃値として参照する
- 防御側はDamage属性に対応する属性値を参照する
- DamageBonus / ResistBonusはPachimon固有Statとする
- 途中計算では端数を維持する
- 最終Damageを整数として確定するときに一度だけ切り捨てる
- 通常の属性Damageは最低1とする

例:

```text
BaseDamage = 100
AttackerFire = 100
AttackerDamageBonus = 0
DefenderFire = 100
DefenderResistBonus = 0

RawDamage = 200
FinalDamage = 100
```

## v0.3基本Skill

| Attribute | Skill | BaseDamage | Startup | Recovery | Cooldown | Target |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| Fire | ひのこ | 100 | 0 | 100 | 200 | 先頭の生存Enemy |
| Aqua | みずでっぽう | 100 | 0 | 100 | 200 | 先頭の生存Enemy |
| Leaf | はっぱスライサー | 100 | 0 | 100 | 200 | 先頭の生存Enemy |
| Electric | ビリビリショック | 100 | 0 | 100 | 200 | 先頭の生存Enemy |
| Poison | どくばり | 100 | 0 | 100 | 200 | 先頭の生存Enemy |
| Ice | 冷たい手 | 100 | 0 | 100 | 200 | 先頭の生存Enemy |
| Wind | かぜでっぽう | 100 | 0 | 100 | 200 | 先頭の生存Enemy |
| Dragon | ドラゴンストレート | 100 | 0 | 100 | 200 | 先頭の生存Enemy |

8Skillは属性以外の処理が同一なため、v0.3では共通の基本属性Damage Logicを使ってよい。個性的な後続Skillは個別Asset / Logicで実装する。

## Placeholder Skill 1-151

- Skill ID 1-151は固有IDのまま維持する
- 各PlaceholderのAllocation Typeに対応する基本Skill名とLogicを設定する
- 8種類の表示名と効果は複数IDで重複してよい
- `isMapAssignable = true`と既存の振り分け回数管理を維持する
- 後からIDごとに本番Skillへ差し替える

## わるあがき

```text
Skill ID: 2000
Startup: 0
Recovery: 100
Cooldown: 0
Target: 先頭の生存Enemy
Damage: 使用者の8属性のうち最も低い値の100%をTrue Damageとして使用する
```

- 所持Skill一覧へ通常表示しない
- 使用可能な所持Skillが0件の場合だけ使用可能になる
- Playerは専用UIで入力を待ち、Enemyは自動使用する
- 攻撃側の属性値とDamageBonusを参照しない
- 防御側の属性値とResistBonusによる軽減を受けない
- 敵の先頭と使用者自身へ同じTrue Damageを与える
- 最低属性値が0なら、双方へ0 True Damageを与える
- DamageBonus、対象属性値、ResistBonus、Passiveの属性Damage補正を参照しない

## Skill実行順

```text
使用可能判定
  -> 発生あり: 発生完了Tickを予約
  -> CooldownReadyTickを設定
  -> 発生完了時に使用者が戦闘不能: 不発
  -> Skill Logicが発生時点の対象を解決
  -> BeforeSkill Event
  -> Damage / Effectを解決
  -> SkillResolved Event
  -> 硬直後のNextTurnTickを設定
  -> 戦闘不能 / 勝敗判定
```

Passiveによる割り込みを含む最終的な細分順は、実装するSample Passiveに合わせて`passive-events.md`へ追加する。
