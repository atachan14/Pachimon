# Domain Boundaries

このファイルは、実装前に `誰が何を持つか` を固定するための整理メモとする。

## 目的
- `RunState` と `MapGenerator` の責務を混ぜない
- `row:0` 初期選択を入れても崩れにくい構造にする
- pachimon 固有情報をどこに持つかを明確にする

## 0. 用語
### Static Data
- run をまたいで共通の静的データ
- 今は主に code-first の table / catalog で供給する

### PachimonInfoTable
- pachimon 固有情報の最小テーブル
- 詳細は [pachimon-info-table.md](./pachimon-info-table.md)

### Instance
- 1run の中で生成される個体データ
- HP / MN / skill構成 / passive など、進行で変わる値を持つ

### RunState
- 1run 中の進行状態そのもの
- player の現在パーティもここに入れる

### RunContext
- `RunState` と `RunMap` と controller 群を束ねる実行時コンテナ

## 1. Static Data に入れるもの
### PachimonInfoTable
- `id`
- `name`
- `front`
- `back`
- `fixedSkillId`
- `passiveId`

### GlobalStatGain
- ステータス上昇値の共通定義

### Trainer / GymLeader / Mod
- 必要なら code-first の table / catalog として保持する
- まだ固まっていないものは保留でよい

## 2. Static Data に入れないもの
- 現在HP / 現在MN
- skill の現在CD
- battle 中の一時状態
- node 解決済み状態
- map 上の現在位置
- player の現在パーティ

これらは全部 `RunState` または battle 用 runtime state に入れる。

## 3. Skill と Passive をどう扱うか
現時点では、`Skill` と `Passive` は table より `C# 定義 + Logic` を優先する。

### 理由
- 固有パラメータが多い
- 一覧調整の価値が低い
- 主体が Logic 実装になる

### 現時点のおすすめ
- `Skill` は C# 定義 + Logic で扱う
- `Passive` は `passiveId = pachimonId` 前提の Logic のみで扱う
- 後から UI 表示やデータ項目が増えたら table 化を再検討する

## 4. RunState に入れるもの
### run 全体
- `runSeed`
- `gold`
- `badgeCount`
- `currentNodeId`
- `resolvedNodeIds`
- `isRunFinished`

### player データ
- `party`
- `inventory`

補足:
- このゲームでは player の所持 pachimon は `party` のみ
- 最初に 3匹選んで、その 3匹で最後まで進む前提で進める

### node 進行データ
- `selectedStartPachimonIds`
- `resolvedRewardIds`
- `openedCityState`

## 5. PachimonInstance に入れるもの
- `instanceId`
- `definitionId`
- `currentHp`
- `maxHp`
- `currentMn`
- `maxMn`
- `stats`
- `skillIds`
- `passiveIds`

補足:
- `mod` は取得時にステータスへ即時反映する前提なら、`modIds` は持たなくてよい
- 後で「何を積んだか」を見たくなったら追加する

## 6. MapGenerator の入力
- `runSeed`
- `PachimonInfoTable`
- `GlobalStatGain`
- 必要なら Trainer / GymLeader / Mod の静的データ
- 必要なら生成設定

## 7. MapGenerator の出力
- `RunMap`
- 各 `MapNode`
- 各 node の `NodeContent`

`MapGenerator` は player の現在HPや現在所持金を持たない。
それは `RunState` 側の責務とする。

## 8. row:0 の扱い
- `row:0` は `StartNodeContent` を持つ
- `StartNodeContent` には初期候補の pachimon 情報を持たせる
- 選択結果は `RunState.party` に反映する

## 9. 実装順のおすすめ
1. 責務境界を固定する
2. `PachimonInfoTable` の shape を決める
3. `GlobalStatGain` を決める
4. その static data を使って `MapGenerator` を作る
5. row:0 初期選択を作る
6. その後に各 node 本実装へ進む
