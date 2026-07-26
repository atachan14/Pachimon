# Map Data Model

## 原則

生成時に固定されるMap情報と、進行によって変化するRun情報を分ける。

```text
RunContext
├─ RunPachimonPool  Run開始時に生成した300個体
├─ RunMap           生成済みMapとNodeContent
├─ RunState         現在位置、party、Gold、解決済みNode
└─ MapRunController
```

Run開始前の151種共通情報は`PachimonCatalog.asset`に保持する。`RunPachimonPool`はCatalogから不参加1種を除き、残り150種を各2個体生成する。

## RunPachimonPool

- 150種、各2個体の合計300体を保持する
- 各個体を一意な`instanceId`で検索できる
- Pachimon個体はMapより先に生成する
- MapNodeの内容は`instanceId`だけを保持し、個体情報はPoolから取得する
- Statsなど同種でも異なる個体情報を保持する

Poolの具体的なコレクション型と、Battle中に変化するHPなどを同じ個体へ持たせるかはv0.2からv0.3で確定する。

## RunMap

- `rows: List<MapRow>`
- `nodes: Dictionary<NodeId, MapNode>`
- `nodeGroups: Dictionary<GroupId, MapNodeGroup>`
- `startNodeId: NodeId`

Node検索はDictionary、描画順はRowsを使う。

## MapRow

- `rowIndex`
- `nodeIds`

## MapNode

- `nodeId`
- `rowIndex`
- `columnIndex`
- `nodeType`
- `nextNodeIds`
- `content`

MapNodeは生成済みMapの定義として扱う。`isResolved`はRun中に変化するため、最終的にはMapNodeではなくRunStateだけで保持する。

## MapNodeGroup

- `groupId`
- `nodeType`
- `nodeIds`

複数の接続点を1つの訪問地点として扱うための論理グループ。Cityでは横並びの2つの`MapNode`を保持し、Edge生成は各Node単位、画面起動・完了・表示はGroup単位で扱う。

- City完了時はGroup内の全Nodeを`resolvedNodeIds`へ追加する
- Cityからの移動候補はGroup内全Nodeの`nextNodeIds`の和集合とする
- Cityへ入る際は、実際にEdgeが接続しているNode IDを`currentNodeId`へ設定する

## NodeType

目標:

- `Start`
- `Battle`
- `Gym`
- `RestSpot`
- `City`
- `Event`
- `LeagueGate`
- `Elite`
- `Ghost`

`Gym / Event`を含め、v0.1で必要なNodeTypeは実装済み。

## NodeContent

Node種別固有の事前確定情報を持つ。進行中に変化する状態は持たない。

### StartNodeContent

- `candidatePachimonInstanceIds`: 9体
- `selectionCount`: 3体

候補配列の長さから候補数を取得できるため、現状の`candidateCount`は将来的に削除候補とする。

### BattleNodeContent

- `enemyPachimonInstanceIds`: 3体
- `nodeReward`: Pachimonに依存しないGold / Mod / Badge
- `trainerProfile`: TrainerのStyle / Name参照

Skill / PassiveのReward候補は保持せず、Reward表示時に`enemyPachimonInstanceIds`から各個体の戦闘開始時Loadoutを参照して導出する。UI上では`nodeReward`と導出した候補をまとめてBattle Rewardとして扱う。現状のコードは`enemyPachimonInstanceIds / nodeReward / trainerProfile`を保持する。

### TrainerProfile

- `role`: Normal / GymLeader / Elite
- `styleId`
- `nameId`

`styleId`はTheme・性別・Graphic・StyleCategoryを持つ静的な`TrainerStyle`を参照する。`nameId`はStyleの性別に対応する名前候補を参照する。Nodeには表示文字列やGraphic本体を複製しない。

肩書はRoleに応じて決定する。

- Normal: `TrainerStyle.normalTitle`
- GymLeader: `ジムリーダー`
- Elite: `四天王`

### TrainerStyle

- `styleId`
- `theme`
- `gender`
- `graphic`
- `styleCategory`: Normal / League
- `normalTitle`: Normal Styleだけが使用

通常Trainer / GymLeader / Eliteで同じ型を使用する。通常TrainerはThemeに合うNormal Styleからランダム選択する。Gym / Eliteは8属性Themeごとに4体、合計32体のLeague Styleを共有し、1Run内で重複なく使用する。

### GymNodeContent

- `enemyPachimonInstanceIds`: 3体
- `trainerProfile`
- Badge属性
- Badgeを含むReward候補
- Gym固有情報

現状のコードは`enemyPachimonInstanceIds / nodeReward / trainerProfile`を保持する。Badge属性は`nodeReward`内に置く。

### RestSpotNodeContent

- 回復ルール

現状は`healValue`を保持する。

### CityNodeContent

- `cityGroupId`
- Shop内容、または再現可能な生成情報

横並びの2つのCity Nodeは別々のMapNodeとして接続を持ち、同じ`cityGroupId`とCityNodeContentを参照する。現状は`shopSeed`だけを保持する仮実装。

### EventNodeContent

- Eventの種類
- Event内容の生成済みデータ、または再現可能な生成情報

v0.1ではNode種別と画面スケルトンへ接続できる形だけ用意し、具体的なEvent内容は後で決める。

### LeagueGateNodeContent

- `requiredBadgeCount`
- 条件未達時の結果

文字列の`failureMode`ではなく、後から専用演出を追加しやすいenumまたは結果型への変更を検討する。

### EliteNodeContent / GhostNodeContent

未実装。BattleNodeContentとの共通部分を持たせるか、Node種別ごとに分けるかはBattle設計時に決める。

EliteNodeContentは3体の`enemyPachimonInstanceIds`と`trainerProfile`を持ち、四天王の固有Style・名前・属性Themeを参照する。Ghostは通常のRunPachimonPool外の個体を扱う可能性があるため、詳細をv1.0で決める。

## RunState

- `runSeed`
- `gold`
- Badge状態
- `currentNodeId`
- `resolvedNodeIds`
- `revealedNodeIds`
- `party`
- `inventory`
- `isRunFinished`

現状の`PlayerPachimonIds`は仮実装。v0.2で`PachimonInstance` 3体を持つ`party`へ置き換える。

`resolvedNodeIds`は攻略済みNode、`revealedNodeIds`はPachimon情報を公開済みのNodeを表す。一度進行可能になったNodeは`revealedNodeIds`へ残し、別ルートを進んだ後も公開状態を維持する。Cityの片側が公開された場合は同じGroupの2Nodeをまとめて公開する。EliteはRun開始時に全Nodeを公開済みへ登録する。

## ID

- 現状はNode IDとPachimon IDに文字列を使用している
- v0.1では文字列のまま進めてよい
- 型の取り違えが増えた段階で`NodeId`などの値型導入を再検討する

## 現状実装との差分

実装済み:

- `NodeType.Gym / Event`
- 解決済み状態の`RunState.ResolvedNodeIds`への一本化
- `MapNode.IsResolved`の削除
- `RunPachimonPool`と個体参照用`instanceId`
- Start / Battle / Gym / EliteのNodeContentからの個体ID参照
- `EventNodeContent`と仮画面への接続

未実装:

- MapOverlay用の選択可能判定
- `TryMoveToNextNode()`の先頭固定移動を、選択したNode IDによる移動へ変更
