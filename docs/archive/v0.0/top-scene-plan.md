# Top Scene Plan

`TopScene` の最小実装方針。

## 目的
- ゲーム開始の入口を固定する
- `Top -> Game` の遷移を早めに通す

## 現在の前提
- まずは `さいしょから` ボタン 1 個で進める
- 設定や終了は後から足してよい

## 最小構成
- `TopSceneInstaller`
- `TopRoot`
- `TitlePanel`
- `NewGameButton`

## 最初にやること
1. `TopScene` を追加する
2. `NewGameButton` で `GameScene` をロードする
3. あとは `TopSceneInstaller` で入口を固定する

## TopSceneInstaller の責務
- Scene 起動時の UI 初期化
- `NewGameButton -> GameScene` 遷移
- 将来 `Settings` や `Continue` が増えても、入口の配線役として使い続ける

## ここではまだやらないこと
- セーブデータ選択
- タイトル演出の完成
- BGM / フェードの細部
- 設定画面
- 終了ボタン
