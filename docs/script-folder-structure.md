# Script Folder Structure

`Assets/Scripts` 配下の基本方針。
目的は、実装が進んだあとも `App / UI / Battle / Map / Run / Data / Editor` の責務が混ざらないようにすること。

## ルート方針
- `Runtime`: 実行時コード
- `Editor`: Unity Editor 専用コード

## Runtime
### `Assets/Scripts/Runtime/App`
- Scene 遷移やアプリ全体で使う薄い共通機能
- いまは `SceneLoader` を置く

### `Assets/Scripts/Runtime/UI`
- UI に関するコードを置く
- 表示、初期化、Scene 接続まで
- battle の計算ロジックは置かない

### `Assets/Scripts/Runtime/UI/Views`
- UI の表示クラス群
- さらに `Core / Overlays / Battle / Screens` に分ける

### `Assets/Scripts/Runtime/UI/Views/Core`
- `GameRootView`
- `HeaderView`
- `LeftPaneView`
- `MainPaneView`
- `RightPaneView`
- `LayoutMode`

### `Assets/Scripts/Runtime/UI/Views/Overlays`
- `MapOverlayView`
- `RewardOverlayView`

### `Assets/Scripts/Runtime/UI/Views/Battle`
- `BattleScreen`
- `BattleMainView`
- `BattleUnitAreaView`

### `Assets/Scripts/Runtime/UI/Views/Screens`
- `NodeScreen`
- `StartScreen`
- `CityScreen`
- `RestSpotScreen`
- `LeagueGateScreen`
- `DefeatScreen`
- `HallOfFameScreen`

### `Assets/Scripts/Runtime/UI/Installers`
- `GameSceneInstaller`
- `TopSceneInstaller`
- Scene 上の参照を束ねて接続する役割

### `Assets/Scripts/Runtime/Battle`
- `BattleController`
- `BattleResolver`
- `BattleState`
- `BattleUnit`
- `SkillRuntime`
- battle ログや戦闘進行

### `Assets/Scripts/Runtime/Map`
- `NodeType`
- `NodeContent`
- `MapNode`
- `MapRow`
- `RunMap`
- `MapGenerator`
- node 接続や map 生成

### `Assets/Scripts/Runtime/Run`
- `RunBootstrap`
- `RunContext`
- `RunState`
- `MapRunController`
- `RewardResolver`
- `CityController`
- `RestSpotController`
- `LeagueGateController`

### `Assets/Scripts/Runtime/Data`
- Runtime 側で参照する定義データの受け皿
- `DefinitionTable` を参照するランタイム用 struct / class
- importer 生成先のアセット参照コードなど

## Editor
### `Assets/Scripts/Editor/Definitions`
- CSV importer
- DefinitionTable 生成
- import メニュー
- Editor 専用検証

## 今の配置
- `SceneLoader.cs` -> `Runtime/App`
- `GameSceneInstaller.cs` / `TopSceneInstaller.cs` -> `Runtime/UI/Installers`
- View / Screen 群 -> `Runtime/UI/Views/*`
- `MapGenerator.cs` など -> `Runtime/Map`
- `RunBootstrap.cs` など -> `Runtime/Run`

## 命名ルール
- View: `XxxView`
- Screen: `XxxScreen`
- Installer: `XxxInstaller`
- Bootstrap: `XxxBootstrap`
- Controller: `XxxController`
- Resolver: `XxxResolver`
- Generator: `XxxGenerator`
- State: `XxxState`
- Context: `XxxContext`

## 補足
- まだファイル数が少ないうちは、細かく分けすぎない
- ただし `App` と `UI` と `Battle` と `Map` と `Run` と `Editor` は最初から分ける
- UI は `Core / Overlays / Battle / Screens` まで分けてよい
