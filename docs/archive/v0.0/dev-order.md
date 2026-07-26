# Dev Order

このプロジェクトは、当面 `Static Data -> Run / Map -> Node -> Battle` の順で進める。
理由は、`MapGenerator` が `PachimonInfoTable` と共通ステータス生成ルールを前提にするため。
特に `row:0 の初期パチモン選択` を含めるなら、先に静的データと `RunState` の責務を固めたほうが後の手戻りが少ない。

## 方針
- `TopScene -> GameScene` の流れは維持する
- その上で、先に `Static Data` と `RunState` の責務境界を固める
- `PachimonInfoTable` と共通ステータス生成ルールを先に進める
- `Map生成` はそれらを使う前提で本実装寄りに進める
- `row:0 StartNode` を含めて `Node移動` を作る
- 各Nodeは最初スケルトンでよい
- その後 `BattleNode` を本実装化する

## 大原則
- データ構造は本実装寄りにする
- ただし各機能の完成度は段階的に上げる
- `Map生成の全部入り` を最初から狙わず、`本実装の骨格` を優先する
- `Battle` は孤立して作らず、`Run / Map / Node起動` の流れに乗せてから育てる

## Phase 0: Domain Boundary
目的:
- `Static Data` と `RunState` の責務境界を固定する

やること:
- `Static Data` / `Instance` / `RunState` / `RunContext` の役割整理
- player の現在パーティを `RunState` に持たせる前提を固定
- `Skill / Passive` を table にするか code-first にするか整理
- [domain-boundaries.md](./domain-boundaries.md) を正とする

完了条件:
- `誰が何を持つか` が docs 上で迷わない

## Phase 1: Static Data 構想
目的:
- `MapGenerator` の前提となる静的データ構造を決める

やること:
- `PachimonInfoTable`
- `GlobalStatGain`
- 必要なら `Trainer / GymLeader / Mod` の静的データ
- `Skill / Passive` は C# 定義で持つ前提を整理

完了条件:
- Unity 側でどう参照するかが決まっている

## Phase 2: Static Data 制作
目的:
- 実際に扱う静的データを作り始める

やること:
- `PachimonInfoCatalog.cs` のような最小の code-first データ作成
- `GlobalStatGain` の作成
- 必要なら `Trainer / GymLeader / Mod` の最小データ作成

完了条件:
- Map 生成に必要な最小データが揃っている

## Phase 3: TopScene
目的:
- ゲーム開始の入口を固定する

やること:
- `TopScene` を追加
- `TopSceneInstaller` を作る
- `Start` ボタンから `GameScene` へ遷移する
- 必要なら `Settings` / `Quit` のプレースホルダを置く

完了条件:
- `TopScene` から `GameScene` へ遷移できる

## Phase 4: Run 起動骨格
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

## Phase 5: Static Data 参照入口
目的:
- `MapGenerator` が静的データへアクセスできる入口を作る

やること:
- `PachimonInfoTable` の参照入口を作る
- `GlobalStatGain` の参照入口を作る
- 必要なら `Trainer / GymLeader / Mod` の参照入口を作る

完了条件:
- Run 初期化時に必要な静的データへアクセスできる

## Phase 6: RunMap / MapNode 構造
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

## Phase 7: Map生成 本実装寄り骨格
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

## Phase 8: row:0 StartNode
目的:
- 初期パチモン選択を本実装寄りに通す

やること:
- `StartNodeController`
- 初期選択候補の生成
- 選択結果を `RunState.party` に反映

完了条件:
- row:0 を通過して初期編成が決まる

## Phase 9: Node移動
目的:
- Map上で次Nodeへ進める状態を作る

やること:
- `MapRunController.SelectNode()`
- 現在node / 次node の管理
- 進行可能状態の管理
- `MapOverlay` から node を選べる流れ

完了条件:
- StartNode の後に次Nodeへ移動できる

## Phase 10: Node起動スケルトン
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

## Phase 11: BattleNode 本実装化
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

## Phase 12: Reward / Rest / City / LeagueGate
目的:
- battle 以外の node 処理も順に本実装化する

やること:
- `RewardResolver`
- `RestSpotController` 本実装
- `CityController` 本実装
- `LeagueGateController` 本実装

## Phase 13: Map生成 精密化
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
1. `Domain Boundary`
2. `Static Data` 構想
3. `Static Data` 制作
4. `Static Data` 参照入口
5. `RunBootstrap / RunState / MapRunController`
6. `RunMap / MapNode / NodeContent`
7. `Map生成`
8. `row:0 StartNode`
9. `Node移動`
10. `Nodeスケルトン`
11. `BattleNode` 本実装
