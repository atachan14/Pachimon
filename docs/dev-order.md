# Dev Order

このプロジェクトは、`Battle最小実装先行` ではなく、`Run / Map 先行` で進める。
理由は、`row:0 の初期パチモン選択`、`Map生成時のNode内容確定`、`csv読み込みからRun開始までの流れ` を先に固めたほうが、後の手戻りが少ないため。

## 方針
- `TopScene -> GameScene` の流れを先に通す
- `GameScene` 開始時に `Run` を初期化する
- `DefinitionTable` の読み込み入口を先に作る
- `Map生成` は本実装寄りの骨格で進める
- `row:0 StartNode` を含めて `Node移動` を作る
- 各Nodeは最初スケルトンでよい
- その後 `BattleNode` を本実装化する

## 大原則
- データ構造は本実装寄りにする
- ただし各機能の完成度は段階的に上げる
- `Map生成の全部入り` を最初から狙わず、`本実装の骨格` を優先する
- `Battle` は孤立して作らず、`Run / Map / Node起動` の流れに乗せてから育てる

## Phase 0: Data / UI 土台
目的:
- 既に作った docs と UI骨組みを実装入口として使える状態にする

やること:
- `DefinitionTable / Logic / Registry / GraphicTable` 方針の維持
- `GameScene` の常設 UI 維持
- `TopScene` を追加する前提整理

## Phase 1: TopScene
目的:
- ゲーム開始の入口を固定する

やること:
- `TopScene` を追加
- `TopSceneInstaller` を作る
- `Start` ボタンから `GameScene` へ遷移する
- 必要なら `Settings` / `Quit` のプレースホルダを置く

完了条件:
- `TopScene` から `GameScene` へ遷移できる

## Phase 2: Run 起動骨格
目的:
- `GameScene` 開始時に Run が立ち上がる流れを作る

やること:
- `RunBootstrap`
- `RunState`
- `RunContext`
- `MapRunController`
- `GameSceneInstaller` から Run 起動

完了条件:
- `GameScene` 開始時に Run 初期化が呼ばれる

## Phase 3: Definition 読み込み入口
目的:
- `csv -> DefinitionTable` を使う前提の入口を作る

やること:
- 読み込み済み `DefinitionTable.asset` の参照入口を作る
- `PachimonDefinitionTable`
- `SkillDefinitionTable`
- `PassiveDefinitionTable`
- `ModDefinitionTable`
- `TrainerDefinitionTable`
- `GymLeaderDefinitionTable`
- `GlobalStatGainTable`

完了条件:
- Run 初期化時に定義データへアクセスできる

## Phase 4: RunMap / MapNode 構造
目的:
- 1run 中に使う map データ構造を本実装寄りで固める

やること:
- `RunMap`
- `MapRow`
- `MapNode`
- `NodeType`
- `NodeContent`
- `StartNodeContent`
- `BattleNodeContent`
- `RestSpotNodeContent`
- `CityNodeContent`
- `LeagueGateNodeContent`

完了条件:
- RunMap が `rowIndex / nodeType / node content` を保持できる

## Phase 5: Map生成 本実装寄り骨格
目的:
- `map-generation.md` に沿う生成骨格を先に通す

やること:
- `RunSeed` ベース生成
- `row:0` を含む生成
- `row:36 LeagueGateZone`
- `row:37~40 EliteNode群`
- `row:41+ GhostNode群` への拡張余地
- `NormalArea内で同種重複なし` の前提を保てる構造
- 各Nodeへの内容配置

あと回しでよいもの:
- 接続アルゴリズムの最適化
- ジム8ルート保証の厳密判定
- バランスの細部調整

完了条件:
- 1run 用 map を生成して保持できる

## Phase 6: row:0 StartNode
目的:
- 初期パチモン選択を本実装寄りに通す

やること:
- `StartNodeController`
- 初期選択候補の生成
- 選択結果を `RunState` に反映
- `PlayerData` / `PartyState` へ反映

完了条件:
- row:0 を通過して初期編成が決まる

## Phase 7: Node移動
目的:
- Map上で次Nodeへ進める状態を作る

やること:
- `MapRunController.SelectNode()`
- 現在node / 次node の管理
- 進行可能状態の管理
- `MapOverlay` から node を選べる流れ

完了条件:
- StartNode の後に次Nodeへ移動できる

## Phase 8: Node起動スケルトン
目的:
- 各Node種別が最低限起動する

やること:
- `NodeControllerBase`
- `BattleNodeController`
- `RestSpotNodeController`
- `CityNodeController`
- `LeagueGateNodeController`
- 最初は各Nodeに `次に進む` 相当の仮導線だけ置く

完了条件:
- Node種別ごとに画面が切り替わる

## Phase 9: BattleNode 本実装化
目的:
- 進行骨格の上で Battle を本実装化する

やること:
- `BattleState`
- `BattleController`
- `BattleResolver`
- `BattleLog`
- `SkillSelector`
- 勝敗判定
- `Battle終了 -> RewardOverlay表示`

完了条件:
- BattleNode を通過して結果を RunState に戻せる

## Phase 10: Reward / Rest / City / LeagueGate
目的:
- battle 以外の node 処理も順に本実装化する

やること:
- `RewardResolver`
- `RestSpotController` 本実装
- `CityController` 本実装
- `LeagueGateController` 本実装

## Phase 11: Map生成 精密化
目的:
- 今まで骨格で止めていた生成ルールを本仕様に近づける

やること:
- 接続アルゴリズム改善
- シティ統合
- センター配置
- ジム配置
- ルート保証
- GhostNode群詳細

## いまの優先順位
1. `TopScene`
2. `Top -> Game` 遷移
3. `RunBootstrap / RunState / MapRunController`
4. `DefinitionTable` 読み込み入口
5. `RunMap / MapNode / NodeContent`
6. `Map生成`
7. `row:0 StartNode`
8. `Node移動`
9. `Nodeスケルトン`
10. `BattleNode` 本実装
