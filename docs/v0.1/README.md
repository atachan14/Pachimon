# v0.1: Map

v0.1 の完成目標は、New Game で Run を開始し、生成された Map の Node と Edgeを画面上で確認し、選択可能な次 Node へ移動できる状態にすること。

## スコープ

- `TitleScene -> GameScene` の遷移
- New Game としての Run 初期化
- 150種、合計300体のRunPachimonPool生成
- `runSeed` に基づく NormalArea の生成
- 16個のEvent Nodeを含むMap構造
- `row:0` の StartNode と候補9体の保持
- Node / Edge の MapOverlay 表示
- 現在、解決済み、選択可能 Node の表示差分
- 選択可能 Node への移動
- Node 種別に対応する画面のスケルトン起動

## v0.1では完成させないもの

- StartNode での実際の3体選択
- Battle、Reward、RestSpot、City、Event の本処理
- GhostNode群の詳細生成
- Save / Load
- 最終的なグラフィックや演出

StartNode の本処理は v0.2、Battle の本処理は v0.3 で扱う。ただし、後から接続できるデータ構造は v0.1 で用意する。

## 現在地

実装済み:

- `TitleScene -> GameScene`
- `RunBootstrap / RunContext / RunState`
- 150種、300個体の`RunPachimonPool`生成
- `RunMap / MapRow / MapNode / NodeContent`
- 149Nodeと保証付きEdgeのMap生成
- City / Gym / RestSpot / Event / Battle / LeagueGate / Elite配置
- Badge 8個取得可能ルートの保証
- 300個体のStart / Battle / Gym / Eliteへの割り当て
- Node 画面のスケルトン切り替え
- MapOverlayでのNode / Edge表示
- 現在、解決済み、選択可能、仮選択Nodeの表示差分
- RightPaneでの移動先確認と決定・キャンセル
- Reward / TrainerのMap生成時割り当て

次に行うこと:

1. 生成されたMapを通しで操作してNode移動を確認する
2. [`../backlog.md`](../backlog.md)のUI調整とCity Edge問題を解消する
3. Mod構造を再検討してからBattle / Gym / EliteのMap Iconを実装する

## 作業メモ

実装中に思いついた改善案や、後回しにする検討事項は[`../backlog.md`](../backlog.md)へ記録する。確定した内容だけを各仕様書と`decisions.md`へ移す。

## 読む順番

1. [`roadmap.md`](./roadmap.md)
2. [`run-flow.md`](./run-flow.md)
3. [`map-generation.md`](./map-generation.md)
4. [`map-data-model.md`](./map-data-model.md)
5. [`map-rendering.md`](./map-rendering.md)
6. [`trainer-style.md`](./trainer-style.md)
7. [`right-pane.md`](./right-pane.md)
8. [`pachimon-stats.md`](./pachimon-stats.md)
9. [`pachimon-catalog.md`](./pachimon-catalog.md)
10. [`../backlog.md`](../backlog.md)
11. [`decisions.md`](./decisions.md)
