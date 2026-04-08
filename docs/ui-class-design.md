# UI Class Design

`GameScene` の UI を、表示領域としての `View` と、node ごとの画面単位としての `Screen` に分けて扱うための設計メモ。

## 方針
- `View` はレイアウト領域や部品を表す
- `Screen` は `GraphicWindow` 内で切り替わる node 画面を表す
- `LogWindow` は `MainPane` 共通のログ / 選択 UI として扱う
- `RewardOverlay` は battle 専用 overlay のまま `BattleScreen` 配下に持たせる

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
- `GraphicWindow` 内の `NodeScreen` を登録して切り替える
- `LogWindowView` を共通ログ領域として保持する

主な参照:
- `GraphicWindow`
- `LogWindowView`

### LogWindowView
役割:
- `MainPane` 共通のログ / 説明文表示を担当する
- `SelectGrid` の button 群を動的生成する

主な参照:
- `TextLogText`
- `SelectGridRoot`

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
- battle 中のログ / 選択 UI は `LogWindowView` に流す

### BattleMainView
役割:
- battle 画面のグラフィック領域を担当する
- 味方・敵の表示だけを持つ

主な参照:
- `GraphicWindow`
- `EnemyArea`
- `AllyArea`

### BattleUnitAreaView
役割:
- `EnemyArea` / `AllyArea` の共通表示を扱う
- `BarsRoot` と `GraphicsRoot` を持つ

## Screen
### NodeScreen
- `GraphicWindow` 内で切り替わる画面の基底

### 個別 Screen
- `StartScreen`
- `BattleScreen`
- `CityScreen`
- `RestSpotScreen`
- `LeagueGateScreen`
- `DefeatScreen`
- `HallOfFameScreen`

## MainPane の構造
```text
MainPane
  GraphicWindow
    StartScreen
    BattleScreen
    CityScreen
    RestSpotScreen
    LeagueGateScreen
    DefeatScreen
    HallOfFameScreen
  LogWindow
    TextLog
    SelectGrid
```

## LogWindow の扱い
- `TextLog` は battle log も node 説明文もここに流す
- `SelectGrid` は battle の skill 選択が本命
- ただし当面は `次へ進む` などの仮導線にも使う

## LayoutMode
### Compact
- `MainPane` を主表示にする
- `LeftPane` と `RightPane` は常時表示しない

### Expanded
- `LeftPane / MainPane / RightPane` を同時表示する
- 開発時の情報確認をしやすくする
