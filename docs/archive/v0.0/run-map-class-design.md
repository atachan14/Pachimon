# Run Map Class Design

`Run / Map 先行` で実装を進めるための、最小かつ本実装寄りのクラス設計メモ。

## 目的
- `TopScene -> GameScene -> Run開始 -> Map生成 -> row:0 StartNode -> Node移動` を安定して通す
- `Battle` を後から実装しても崩れにくい構造にする
- player の現在パーティをどこに持つかを先に固定する

## いまの最小実装
現在は、まず固定の直線 map でこの流れを通している。

- `row:0 Start`
- `row:1 Battle`
- `row:2 RestSpot`
- `row:3 City`
- `row:36 LeagueGate`

接続アルゴリズムは後から本仕様に近づける。

## 構造
### RunContext
役割:
- `GameScene` 実行中に共有する入口コンテナ

保持候補:
- `RunState`
- `RunMap`
- `MapRunController`
- 静的データ参照

補足:
- player の現在パーティ本体は `RunContext` ではなく `RunState` に持つ
- `RunContext` は長期保存データではなく、実行時の束ね役

### RunState
役割:
- 1run 中の進行状態を保持する

保持候補:
- `runSeed`
- `gold`
- `badgeCount`
- `currentNodeId`
- `party`
- `inventory`
- `resolvedNodeIds`
- `isRunFinished`

補足:
- このゲームでは player の所持 pachimon は `party` のみ
- `ownedPachimon` は持たない
- `row:0` の初期選択結果も `RunState.party` に反映する

### RunMap
役割:
- 1run 用に生成された map 全体を保持する

保持候補:
- `rows`
- `nodes`
- `startNodeId`

### MapRow
役割:
- 同じ row に属する node 群をまとめる

保持候補:
- `rowIndex`
- `nodeIds`

### MapNode
役割:
- 1つの node 情報を保持する

保持候補:
- `nodeId`
- `rowIndex`
- `columnIndex`
- `nodeType`
- `nextNodeIds`
- `content`
- `isResolved`

## NodeType
候補:
- `Start`
- `Battle`
- `RestSpot`
- `City`
- `LeagueGate`
- `Elite`
- `Ghost`

## NodeContent
方針:
- `MapNode` 本体に全部を詰めず、種別ごとの content を持たせる

### StartNodeContent
保持候補:
- `candidatePachimonIds`
- `candidateCount`
- `selectionCount`

補足:
- 現時点の前提は `candidateCount = 9`
- player はその 9 体から 3 体を選ぶ

### BattleNodeContent
保持候補:
- `enemyPartySeed`
- `goldReward`

### RestSpotNodeContent
保持候補:
- `healValue`

### CityNodeContent
保持候補:
- `shopSeed`

### LeagueGateNodeContent
保持候補:
- `requiredBadgeCount`
- `failureMode`

## RunBootstrap
役割:
- `GameScene` 開始時に Run を初期化する

責務:
- runSeed 決定
- `RunMap` 生成
- `RunState` 生成
- `MapRunController` 起動

## MapGenerator
役割:
- `map-generation.md` に沿って `RunMap` を生成する

現在の責務:
- 最小の固定 map を生成する
- `row:0` 候補パチモンを保持する
- 後から本実装寄りの生成へ差し替えられる入口になる

今後の前提:
- `MapGenerator` は `PachimonInfoTable` を参照して node 内容を生成する
- 先に揃えるのは `PachimonInfoTable` と `GlobalStatGain`
- 必要なら Trainer / GymLeader / Mod の静的データを追加で参照する
- `Skill / Passive` はいったん C# 定義と Logic 側で扱う

## MapRunController
役割:
- map / node の接続と進行を管理する

現在の責務:
- 現在 node の保持
- 次 node への移動
- `HeaderView` の stage / gold / badges 表示更新
- `MainPaneView` の `Screen` 切り替え

## 初期実装でやる範囲
- `RunContext`
- `RunState`
- `RunMap`
- `MapRow`
- `MapNode`
- `NodeType`
- `StartNodeContent`
- `BattleNodeContent`
- `MapGenerator`
- `MapRunController`
- `RunBootstrap`

## あとから広げる範囲
- save / load
- ghost 詳細
- ジム8ルート保証の厳密判定
- 高度な reward 生成
- ノードごとの演出差分
- 本仕様の node controller 群
