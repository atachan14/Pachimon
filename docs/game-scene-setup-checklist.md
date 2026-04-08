# Game Scene Setup Checklist

`GameScene` を Editor 上で常設 UI に移すための、実作業向けチェックリスト。
この手順では、見た目の微調整より先に `GameSceneInstaller` が Scene 参照で初期化できる状態を目指す。

## 0. 前提
- Scene: `Assets/Scenes/GameScene.unity`
- Installer script: `Pachimon.UI.GameSceneInstaller`
- `GameScene` は Scene 常設 UI 前提で動かす
- `MainPane` は `GraphicWindow + LogWindow` の構成で組む

## 1. ルート構成
### 1-1. Scene root に置くもの
- `Main Camera`
- `Global Light 2D`
- `GameSceneInstaller`
- `PachimonUiCanvas`

### 1-2. `PachimonUiCanvas` の推奨コンポーネント
- `Canvas`
- `CanvasScaler`
- `GraphicRaycaster`

### 1-3. `CanvasScaler` の推奨設定
- `UI Scale Mode`: `Scale With Screen Size`
- `Reference Resolution`: `1920 x 1080`
- `Screen Match Mode`: `Match Width Or Height`
- `Match`: `0.5`

## 2. ヒエラルキー
### 2-1. 親子関係
- `PachimonUiCanvas`
- `GameRoot`
- `Header`
- `Main`
- `Content`
- `LeftPane`
- `MainPane`
- `RightPane`
- `MapOverlay`

### 2-2. `MainPane` の子
- `GraphicWindow`
- `LogWindow`

### 2-3. `GraphicWindow` の子
- `StartScreen`
- `BattleScreen`
- `CityScreen`
- `RestSpotScreen`
- `LeagueGateScreen`
- `DefeatScreen`
- `HallOfFameScreen`

### 2-4. `LogWindow` の子
- `TextLog`
- `SelectGrid`

### 2-5. `BattleScreen` の子
- `BattleMain`
- `RewardOverlay`

### 2-6. `BattleMain` の子
- `GraphicWindow`
- `EnemyArea`
- `Space`
- `AllyArea`

### 2-7. `EnemyArea` / `AllyArea` の子
- `Bars`
- `Graphics`

### 2-8. `RewardOverlay` の子
- `RewardTitle`
- `RewardBody`

### 2-9. `Header` の子
- `GoldArea`
- `StageArea`
- `BadgeArea`
- `DetailArea`
- `MapButton`
- `ItemButton`
- `SettingsButton`

## 3. 必須コンポーネント
### 3-1. ルート
- `GameRoot`: `GameRootView`
- `Header`: `HeaderView`
- `LeftPane`: `LeftPaneView`
- `MainPane`: `MainPaneView`
- `LogWindow`: `LogWindowView`
- `RightPane`: `RightPaneView`
- `MapOverlay`: `MapOverlayView`

### 3-2. Screen
- `StartScreen`: `StartScreen`
- `BattleScreen`: `BattleScreen`
- `CityScreen`: `CityScreen`
- `RestSpotScreen`: `RestSpotScreen`
- `LeagueGateScreen`: `LeagueGateScreen`
- `DefeatScreen`: `DefeatScreen`
- `HallOfFameScreen`: `HallOfFameScreen`

### 3-3. Battle
- `BattleMain`: `BattleMainView`
- `EnemyArea`: `BattleUnitAreaView`
- `AllyArea`: `BattleUnitAreaView`

### 3-4. Reward
- `RewardOverlay`: `RewardOverlayView`

### 3-5. テキスト
- `TextMeshPro - Text(UI)` を前提にして OK
- 文字表示の見た目は後で差し替えてよい

## 4. Installer に刺す参照
### 4-1. Scene References
- `Game Root View`: `GameRoot`
- `Header View`: `Header`
- `Left Pane View`: `LeftPane`
- `Main Pane View`: `MainPane`
- `Right Pane View`: `RightPane`
- `Map Overlay View`: `MapOverlay`

### 4-2. Main Screens
- `Start Screen`: `StartScreen`
- `Battle Screen`: `BattleScreen`
- `City Screen`: `CityScreen`
- `Rest Spot Screen`: `RestSpotScreen`
- `League Gate Screen`: `LeagueGateScreen`
- `Defeat Screen`: `DefeatScreen`
- `Hall Of Fame Screen`: `HallOfFameScreen`

### 4-3. Layout
- `Compact Breakpoint`: `1100`
- `Initial Screen`: 任意。通常は `StartScreen` でも `BattleScreen` でもよい

## 5. 各 View / Screen の初期化に必要な参照
### 5-1. `HeaderView`
- `GoldText`
- `StageText`
- `BadgeText`
- `MapButton`
- `ItemButton`
- `SettingsButton`

### 5-2. `LeftPaneView`
- `TitleText`
- `BodyText`

### 5-3. `RightPaneView`
- `TitleText`
- `BodyText`

### 5-4. `MapOverlayView`
- `TitleText`
- `BodyText`

### 5-5. `MainPaneView`
- `GraphicWindow`
- `LogWindowView`

### 5-6. `LogWindowView`
- `TextLogText`
- `SelectGridRoot`

### 5-7. `BattleScreen`
- `BattleMainView`
- `RewardOverlayView`

### 5-8. `BattleMainView`
- `GraphicWindow`
- `EnemyArea`
- `AllyArea`

### 5-9. `BattleUnitAreaView`
- `BarsRoot`
- `GraphicsRoot`

### 5-10. `RewardOverlayView`
- `TitleText`
- `BodyText`

## 6. 最初の確認ポイント
1. `GameScene` 再生で Scene 常設 UI が正しく初期化される
2. `StartScreen` が `GraphicWindow` に表示される
3. `TextLog` に開始ノード情報が表示される
4. `SelectGrid` のボタンで `BattleScreen` へ進める
5. `BattleScreen` に切り替わったとき、`TextLog` が battle log に置き換わる

## 7. まだ後回しでよいもの
- ItemPanel 実体
- Left / Right の compact 時スライドイン導線
- 実データ接続
- battle の本ロジック
- 見た目の完成調整

## 8. Editor 作業のコツ
- まずは docs の名前に合わせてそのまま置く
- `RectTransform` はざっくりでよい。あとで詰める
- 参照を刺し終わってから崩れを直す
- `MainPane > GraphicWindow / LogWindow` の構造を先に固めると、あとがかなり楽
