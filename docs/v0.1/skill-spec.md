# Skill Spec

## 目的

Skillごとに発動条件、対象決定、効果、Graphic、固有パラメーターを自由に実装できる構造とする。共通基底は、検索、Map振り分け、戦闘時間、UI表示に必須の情報だけを持つ。

## SkillAsset

すべてのSkillは`SkillAsset : ScriptableObject`を継承したAssetとして登録する。

共通項目:

```text
skillId
displayName
allocationType
isMapAssignable
baseRecoveryTicks
baseCooldownTicks
description
```

- `allocationType`はMap生成時のType一致振り分けに使用する
- `isMapAssignable = true`はランダム振り分けとType一致振り分けの両方へ参加する
- Map振り分け対象Skillは必ず`allocationType`を持つ
- Map振り分け対象外Skillは`Unassigned`を許可する
- `baseRecoveryTicks`は効果解決後、使用者が次のTurnを得るまでの基本Tickとする
- 発生を持つSkillだけ、固有Assetで`baseStartupTicks`を定義する
- 発生が0のSkillは即時解決し、仕様書とUIでは発生を省略する
- `baseCooldownTicks`は同じSkillが再使用可能になるまでの基本Tickとする
- MN消費量は共通Asset項目へ固定せず、必要なSkillの個別Logic / Assetで定義する
- 現行の基本SkillはMNを消費しない

対象数、対象決定、効果計算、発動条件、Graphic数、固有状態、固有リソースは共通化しない。各派生AssetのフィールドとC# Logicで定義する。

SkillAssetは静的定義として扱い、現在Cooldownなど戦闘中に変化する状態を保持しない。可変状態はBattle側の個体別Stateへ保持する。

## SkillCatalog

`SkillCatalog.asset`は通常Skill、技マシン限定Skill、System Skillを含む全SkillAssetへの参照を1か所で管理する。

主な用途:

- `skillId`からSkillAssetを取得する
- Map振り分け対象一覧を取得する
- AllocationType一致候補を取得する
- ID重複、必須項目、ID帯、振り分け設定を検証する

通常用と全件用にCatalogを分割しない。必要な候補は`isMapAssignable`と`allocationType`で絞り込む。

## ID帯

```text
1-151       現在のMap振り分け対象Skill
152-999     将来拡張用
1000-1999   技マシン限定Skill想定
2000以降    System Skill想定
```

ID帯は整理上の慣例とし、Map振り分け可否の判定には使用しない。判定は必ず`isMapAssignable`を参照する。

- ID 1-151はすべて`isMapAssignable = true`とする
- ID 1-151はPachimon 151種の固定Skillとして1つずつ参照される
- 固定Skillもランダム振り分けとType一致振り分けの対象となる
- 技マシンはID 1-151の通常Skillも、1000番台の限定Skillも参照できる
- 特殊Skill`わるあがき`はID 2000、`isMapAssignable = false`とする

ID範囲だけで技マシン取得可否やSystem判定を行わない。技マシン取得は将来の技マシンデータが参照する`skillId`、System Skill使用はBattle側の明示的なルールで決める。

## PachimonInstance

`PachimonInstance`はRun中の所持Skillを`skillId`一覧として保持する。Run生成直後はPachimonCatalogの`fixedSkillId`を1つ設定し、Map配置後に追加Skillを設定する。

SkillAsset本体やCooldown状態はPachimonInstanceへ複製しない。

## Map振り分け

Pachimonを各Nodeへ配置した後、次の順で追加Skillを振り分ける。

1. Gymの各PachimonへBadge属性と一致するSkillを2つ追加する
2. Eliteの各PachimonへTrainerThemeと一致するSkillを3つ追加する
3. 全PachimonへNodeのrowに応じたランダムSkillを追加する

```text
row 0-17   +2
row 18-26  +3
row 27-40  +4
```

Start候補9体にもrow 0の規則でランダムSkillを2つ追加する。同一個体へ同じSkillを重複設定しない。

候補選択では、対象候補のうちRun全体での現在採用回数が最少のSkill群を抽出し、その中からRun Seedに基づいてランダムに1つ選ぶ。固定Skillも初期採用回数へ含める。これにより完全な順番固定を避けながら、採用回数をできるだけ均等にする。

調整値は`MapGenerationSettings`へ保持する。Mapを同じ`RunPachimonPool`から再生成した場合は、追加Skillを一度除去して固定Skillだけに戻してから再振り分けする。

## v0.1の仮データ

- ID 1-151のPlaceholder SkillAssetを生成する
- ID順に8属性を均等に仮設定する
- すべてMap振り分け対象とする
- ID 2000の`わるあがき`を生成する
- Placeholderの発動Logicは実装せず、v0.3のBattle実装時に各固有Skillへ置き換える

## Editor操作

```text
Tools > Pachimon > Data > Create Skill Placeholder Catalog
Tools > Pachimon > Data > Validate Skill Catalog
```

生成処理は既存のSkillAssetを上書きせず、ID 1-151とID 2000の不足分だけを作成する。生成後は`SkillCatalog.asset`をGameSceneの`GameSceneInstaller`へ自動設定する。

## 後続工程

1. v0.3でSkillContext、使用可否、対象決定、効果解決、Battle中Stateを設計する
