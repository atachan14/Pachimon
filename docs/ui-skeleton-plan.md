# UI Skeleton Plan

`GameScene` の UI 骨組みを先に通し、あとから本実装のロジックを差し込める状態を目指す。

## 目的
- `Header + Main + Overlay` の土台を安定させる
- `NodeScreen` を差し替えるだけで進行できる構造にする
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
- node 画面の本体表示

### RightPane
- 敵情報または node 詳細表示

## MainPane 配下の Screen
- `StartScreen`
- `BattleScreen`
- `CityScreen`
- `RestSpotScreen`
- `LeagueGateScreen`
- `DefeatScreen`
- `HallOfFameScreen`

## Overlay
### MapOverlay
- MainPane をほぼ覆う別表示
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
- `BattleLogWindow`

### BattleLogWindow の最小構造
- `Outer`
- `BattleLog`
- `SkillSelector`

## View クラス
- `HeaderView`
- `LeftPaneView`
- `MainPaneView`
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
- battle の入力 UI は `SkillSelector` に寄せる
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
