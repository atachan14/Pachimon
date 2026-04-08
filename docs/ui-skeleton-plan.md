# UI Skeleton Plan

`GameScene` の UI 骨組みを先に通し、あとから本実装のロジックを差し込める状態を目指す。

## 目的
- `Header + Main + Overlay` の土台を安定させる
- `GraphicWindow` を差し替えるだけで進行できる構造にする
- `LogWindow` を `MainPane` 共通にして、battle と node 説明を同じ導線で扱う
- `Compact / Expanded` の両レイアウトを早めに確認できるようにする

## 画面の役割
### Header
- gold 表示
- stage 表示
- badge 数表示
- Map ボタン
- Item ボタン
- Settings ボタン

### LeftPane
- 味方情報表示

### MainPane
- `GraphicWindow` に node 画面を表示する
- `LogWindow` にログ / 説明 / 選択 UI を表示する

### RightPane
- 敵情報または node 詳細表示

## MainPane 配下の構造
- `GraphicWindow`
  - `StartScreen`
  - `BattleScreen`
  - `CityScreen`
  - `RestSpotScreen`
  - `LeagueGateScreen`
  - `DefeatScreen`
  - `HallOfFameScreen`
- `LogWindow`
  - `TextLog`
  - `SelectGrid`

## Overlay
### MapOverlay
- Main をほぼ覆う別表示
- 開閉は Header の `MapButton` が担当

### RewardOverlay
- `BattleScreen` の内部で表示する battle 専用 overlay
- battle 終了時に自動表示する前提
- 手動で開くボタンは持たない

## BattleScreen の最小構造
- `BattleMain`
- `RewardOverlay`

### BattleMain の最小構造
- `GraphicWindow`
- `EnemyArea`
- `Space`
- `AllyArea`

## View クラス
- `HeaderView`
- `LeftPaneView`
- `MainPaneView`
- `LogWindowView`
- `RightPaneView`
- `MapOverlayView`
- `RewardOverlayView`
- `BattleMainView`
- `BattleUnitAreaView`

## Screen クラス
- `StartScreen`
- `BattleScreen`
- `CityScreen`
- `RestSpotScreen`
- `LeagueGateScreen`
- `DefeatScreen`
- `HallOfFameScreen`

## 実装メモ
- View は表示と参照保持に集中する
- Screen は node 単位の表示切替単位として扱う
- `TextLog` は battle log も node 説明文も受け持つ
- `SelectGrid` は battle の skill 選択が本命だが、当面は仮の `次へ進む` ボタンにも使う
- `たたかう / アイテム` の分岐は battle 内では持たない

## Compact / Expanded
### Compact
- `MainPane` を主役にする
- `LeftPane` と `RightPane` は常時表示しない
- 実機寄りの見え方確認を優先する

### Expanded
- `LeftPane / MainPane / RightPane` を同時表示する
- 開発中の情報確認をしやすくする

### 方針
- `LayoutMode = Compact / Expanded` を切り替える
- 表示中の `Screen` や overlay 状態はモード切替で壊さない
- レイアウトだけを切り替える
