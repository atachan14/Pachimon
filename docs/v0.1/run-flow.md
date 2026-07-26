# Run Flow

## New Game

```text
TitleScene: さいしょから
  -> GameSceneを読み込む
  -> GameSceneInstallerがScene参照を初期化
  -> RunBootstrapがNew Game用Runを生成
  -> runSeedを決定
  -> RunPachimonPoolを300体生成
  -> MapGeneratorがRunMapを生成
  -> RunStateを生成
  -> MapRunControllerを起動
  -> row:0のStartNode画面を表示
  -> MapOverlayを初期構築
```

GameSceneをEditorから直接起動した場合も、当面は同じNew Game処理を実行する。本番でもこのフォールバックは残してよい。将来の「つづきから」だけ、保存済みRunを渡す別入口にする。

## 責務

### RunBootstrap

- New Game開始時の組み立てを担当する
- `runSeed`、`RunPachimonPool`、`RunMap`、`RunState`、`MapRunController`を接続する
- ゲーム進行そのものは担当しない

### RunContext

- GameScene実行中に共有する参照を束ねる
- `RunPachimonPool`、`RunState`、`RunMap`、`MapRunController`を保持する
- Save対象そのものではない

### RunPachimonPool

- 151種から不参加の1種を除いた150種を使用する
- 各種2個体、合計300体のRun用個体を保持する
- Map生成より先に生成し、MapNodeからは`instanceId`で参照する
- Map生成の一部を再試行しても個体を作り直さない

### RunState

- 1run中に変化する進行状態を保持する
- `runSeed`、所持Gold、Badge、party、現在Node、解決済みNodeを持つ
- playerが所有するPachimonはpartyの3体だけとし、別のownedPachimon一覧は持たない

### RunMap

- 生成済みMapの構造と、各Nodeの事前確定内容を保持する
- Nodeの接続や敵構成など、Run中に再抽選しない情報を持つ
- 現在位置や解決済み状態は持たない

### MapRunController

- Node選択、Node画面起動、進行可能状態の切り替えを担当する
- Header、MainPane、MapOverlayへRun状態を反映する
- Node固有Controllerから完了通知を受け、次Nodeの選択を許可する

## row:0

- StartNodeには重複しないPachimon候補9体を事前配置する
- v0.1では候補データの保持と画面スケルトンまでを対象にする
- v0.2でplayerが3体を選び、選択した個体を`RunState.party`へ登録する
- 選択した各個体には固定Skill 1つに加え、ランダムSkill 2つを設定する

## Node進行

1. 現在Nodeに対応する画面とControllerを起動する
2. Node処理が未完了なら次Nodeを選択できない
3. Node処理が完了したら現在Nodeを解決済みにする
4. 接続先のNodeを選択可能にする
5. Mapを自動で開く必要があるNodeでは、完了後にMapOverlayを開く
6. playerが接続先を選択したら現在Nodeを更新し、次のNode画面を起動する

Cityは入った時点から進行可能とする。Battleは勝利後、RestSpotは回復後に進行可能になる。
