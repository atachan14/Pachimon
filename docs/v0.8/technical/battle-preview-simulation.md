# Battle Preview Simulation

Battle中のSkill Previewは、SkillごとのPreview専用Logicを持たず、本処理を一時的なBattle State上で実行して生成する。

## 目的

- Skillの効果を`Resolve`へ一度だけ記述する
- Passive、Status、追加ダメージ、連鎖の本処理とPreviewの差をなくす
- 新しいSkillやPassiveを追加するたびにPreview専用分岐を増やさない
- Previewによる実Battle Stateへの副作用を防ぐ

## 処理フロー

```text
実Battle State
  -> BattleSimulationSnapshotを生成
  -> HP / MN / Skill / Passive / Cooldown / Statusを複製
  -> Statusの付与元をSimulation Unitへ張り替える
  -> Snapshot上でMNを消費
  -> BattleSkillResolverで通常どおりSkillを解決
  -> Passive EventとStatus反応も通常どおり発火
  -> 解決前後のHP / MN差分をSkillPreviewへ変換
  -> Snapshotを破棄
```

## 責務

### `BattleSimulationSnapshot`

- Player / Enemyの全Unitを複製する
- Statusの付与元を複製後のUnitへ変換する
- Battle Event用のPassive Logicを複製Unitへ登録する
- `BattleStartedEvent`は再発行しない

### `BattleSkillResolver`

- `BeforeSkillEvent`
- Skill Logicの`Resolve`
- Statusによる追加Effectの収集
- `SkillResolvedEvent`
- `UnitDefeatedEvent`

本処理とPreviewの両方が同じResolverを使用する。

### `BattleSkillPreviewSimulator`

- Snapshotを生成する
- Snapshot上でSkillを解決する
- 実UnitとSimulation UnitのHP / MN差分をPreviewへ変換する

## Skill実装規則

- `ISkillLogic`は`Resolve`だけを実装する
- Preview専用のDamage式やPassive分岐をSkill Logicへ追加しない
- Skillが複数Effectを持つ場合も、本処理の順序どおり`Resolve`へ記述する
- Preview表示項目をHP / MN以外へ拡張するときは、Snapshot差分の出力形式を拡張する

## 現在の制限

- UIへ表示するPreview差分はHPとMNのみ
- Status付与やStat変化はSnapshot上で処理されるが、専用Preview表示はまだ行わない
- Timelineの将来予約自体はPreview対象外とし、Skill効果解決時の状態変化を対象とする
