# Game Scene Hierarchy

`GameScene` を Editor 上で常設 UI に移していくための配置メモ。
`GameScene` は Scene 常設 UI 前提で動かす。参照不足がある場合は `GameSceneInstaller` がエラーを出して停止する。

## 方針
- まずは `GameScene` 上に UI の枠だけを常設配置する
- 参照が揃ったら `GameSceneInstaller` が Scene 上の `View / Screen` を初期化する
- 常設 UI を前提に、Scene 上の参照をそのまま初期化する

## 最小ヒエラルキー
- `GameSceneInstaller`
- `PachimonUiCanvas`
- `GameRoot`
- `Header`
- `Main`
- `Content`
- `LeftPane`
- `MainPane`
- `RightPane`
- `MapOverlay`
- `StartScreen`
- `BattleScreen`
- `CityScreen`
- `RestSpotScreen`
- `LeagueGateScreen`
- `DefeatScreen`
- `HallOfFameScreen`

## BattleScreen の子
- `BattleMain`
- `RewardOverlay`

## BattleMain の子
- `GraphicWindow`
- `EnemyArea`
- `Space`
- `AllyArea`
- `BattleLogWindow`

## BattleLogWindow の子
- `Outer`
- `BattleLog`
- `SkillSelector`

## GameSceneInstaller に刺す参照
- `GameRootView`
- `HeaderView`
- `LeftPaneView`
- `MainPaneView`
- `RightPaneView`
- `MapOverlayView`
- `StartScreen`
- `BattleScreen`
- `CityScreen`
- `RestSpotScreen`
- `LeagueGateScreen`
- `DefeatScreen`
- `HallOfFameScreen`
- 必要なら `InitialScreen`

## 段階的な移行手順
1. `GameScene` を開く
2. 常設オブジェクトをヒエラルキーに置く
3. 各オブジェクトに対応する `View / Screen` コンポーネントを付ける
4. `GameSceneInstaller` に参照を刺す
5. 再生して、Scene 常設 UI が正しく初期化されることを確認する
6. 常設 UI 前提で次の Run / Map 実装へ進む

## 補足
- Scene 常設化は、Codex が YAML を直接編集するより Editor 上で組むほうが安全
- 以後の Scene 生成や細かい RectTransform 調整は、必要なら user 側で行ってもらう進め方がかなり相性がいい
- こちらはそれに合わせて、参照スクリプトや接続ロジック、必要な手順書を整える

## Editor Setup Checklist
- `GameScene` 常設 UI の具体手順は `docs/game-scene-setup-checklist.md` を参照する
- Scene 配置は user 側、コードと接続整理は Codex 側、の分担を前提にする
