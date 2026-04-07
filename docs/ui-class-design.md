# UI Class Design

`GameScene` の UI を、表示領域としての `View` と、node ごとの画面単位としての `Screen` に分けて扱うための設計メモ。

## 方針
- `View` はレイアウト領域や部品を表す
- `Screen` は `MainPane` 内で切り替わる node 画面を表す
- `Overlay` は `Screen` と別レイヤーの表示として扱う
- battle 専用の overlay は `BattleScreen` 配下に持たせる

## Core View
### GameRootView
役割:
- 画面全体のレイアウトを管理する
- `Compact / Expanded` を切り替える
- `MapOverlayView` の開閉を制御する

保持する参照:
- `HeaderView`
- `LeftPaneView`
- `MainPaneView`
- `RightPaneView`
- `MapOverlayView`

### HeaderView
役割:
- header 内の表示とボタン参照を持つ

主な参照:
- `GoldText`
- `StageText`
- `BadgeText`
- `MapButton`
- `ItemButton`
- `SettingsButton`

### LeftPaneView / RightPaneView
役割:
- 補助情報の表示領域
- まずはタイトルと本文の仮表示を持つ

### MainPaneView
役割:
- `NodeScreen` を登録して切り替える
- 現在表示中の screen を 1 つだけ active にする

## Overlay View
### MapOverlayView
役割:
- map を `Main` の上に重ねて表示する
- 開閉は Header の `MapButton` から行う

### RewardOverlayView
役割:
- battle 専用の reward 表示を行う
- `BattleScreen` 配下に置く
- battle 終了時に開く前提で、手動 close button は持たない

## Battle View
### BattleScreen
役割:
- battle node の画面本体
- `BattleMainView` と `RewardOverlayView` を持つ

### BattleMainView
役割:
- battle 画面の本体表示
- 味方・敵の表示とログ入力 UI の領域を持つ

主な参照:
- `GraphicWindow`
- `EnemyArea`
- `AllyArea`
- `BattleLogRoot`
- `SkillSelectorRoot`

### BattleUnitAreaView
役割:
- `EnemyArea` / `AllyArea` の共通表示を扱う
- `BarsRoot` と `GraphicsRoot` を持つ

## Screen
- `StartScreen`
- `BattleScreen`
- `CityScreen`
- `RestSpotScreen`
- `LeagueGateScreen`
- `DefeatScreen`
- `HallOfFameScreen`

## BattleLogWindow の扱い
battle では `BattleLogWindow` を 2 領域に分ける。

- `BattleLog`: 行動ログの表示
- `SkillSelector`: Turn 時の skill 選択 UI

この構造により、Turn が来たら直接 skill 一覧を出す現在仕様と揃えやすくする。

## LayoutMode
### Compact
- `MainPane` を主表示にする
- `LeftPane` と `RightPane` は常時表示しない

### Expanded
- `LeftPane / MainPane / RightPane` を同時表示する
- 開発時の情報確認をしやすくする
