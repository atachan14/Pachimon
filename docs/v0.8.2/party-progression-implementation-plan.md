# Party Progression Implementation Plan

## 調査結果

Partyを`1体 -> 2体 -> 3体`へ段階的に増やす変更は、画面だけではなくRun、Battle、Map生成、Skill配布、報酬の前提に影響する。

特に大きい変更点は次の3つ。

1. RunとBattleが現在は「必ず3体」を前提にしている。
2. Map生成が「Start候補9体 + 全戦闘3体 = 300体をすべて使い切る」構造になっている。
3. `RowIndex`が進行度、敵補正、Skill配布、Map上のY座標を兼ねている。

UIの多くは3枠を常設して存在しない枠を隠せるため、DomainとMap生成を先に直せば流用できる部分が多い。

## 1. Party Domain

### 現状

- `RunState.PartySize`は3固定。
- `TrySetInitialParty`は3体でなければ失敗する。
- `IsPartyConfirmed`は3体揃った状態を意味する。
- `BattleSideState`と`BattleStateFactory`も両陣営に3体を要求する。
- `BattleUnitState`のSlot上限も3固定値を参照する。
- `BattleRewardSession`と`RestSpotRecoveryService`もPlayer Partyが3体であることを要求する。

### 変更方針

- `PartySize`を現在人数の意味で使わず、`MaxPartySize = 3`として上限を明示する。
- `RunState`は初期加入1体を受け取り、その後に1体ずつ追加できるAPIを持つ。
- `IsPartyConfirmed`は廃止または`IsPartyInitialized`へ置き換え、`IsPartyFull`と分離する。
- Partyへの追加時はInstance重複、Species重複、上限超過を拒否する。
- `BattleSideState`と`BattleStateFactory`は、1～3体かつSlotが`0`から連続するPartyを受け付ける。
- RestSpotとBattle報酬は、その時点で存在するPlayer全員を対象にする。

### 主な対象

- `Assets/Scripts/Runtime/Run/RunState.cs`
- `Assets/Scripts/Runtime/Battle/BattleSideState.cs`
- `Assets/Scripts/Runtime/Battle/BattleStateFactory.cs`
- `Assets/Scripts/Runtime/Battle/BattleUnitState.cs`
- `Assets/Scripts/Runtime/Reward/BattleRewardSession.cs`
- `Assets/Scripts/Runtime/Run/RestSpotRecoveryService.cs`
- `Assets/Scripts/Runtime/Run/MapRunController.cs`

## 2. Map生成と300体Pool

### 現状

- `RunPachimonPoolGenerator`は常に300 Instanceを生成する。
- `MapGenerator`はStartに9枠、全Battle/Gym/Eliteに3枠を作る。
- Map上の配置枠数とPoolの300体が一致しなければ生成に失敗する。
- 配置後も、Pool内の全Instanceが重複なくMapへ割り当てられたことを検証する。

序盤のEnemyを1体・2体へ減らし、加入候補を追加すると、この一致条件は成立しなくなる。

### 変更方針

- 300体PoolはRun内で利用可能なInstance供給源として維持する。
- Mapは必要数だけPoolから割り当て、未配置Instanceを許容する。
- 検証は「全Poolを使い切ったか」ではなく、次を確認する。
  - Mapが参照するInstanceがPoolに存在する。
  - 同じInstanceを複数のMap枠へ割り当てていない。
  - 同一Node内でSpeciesが重複していない。
  - Nodeごとの期待Party人数を満たす。
- Enemy人数は共通ルールから取得する。
  - row1～10: 1体
  - row11～20: 2体
  - row21以降: 3体
- Battle、Gym、固定Encounterへ同じ進行段階ルールを適用する。Eliteはrow37以降なので3体になる。
- `MapGenerator`、検証、Skill配布が同じParty人数判定を使うよう、`PartyProgressionRules`のような共通クラスへ集約する。

### 主な対象

- `Assets/Scripts/Runtime/Run/RunPachimonPoolGenerator.cs`
- `Assets/Scripts/Runtime/Map/MapGenerator.cs`
- `Assets/Scripts/Runtime/Map/MapGenerationSettings.cs`
- `Assets/Scripts/Runtime/Map/MapSkillDistributor.cs`

## 3. 固定Encounter Node

### 変更方針

- row10と11の間、row20と21の間に必ず通過する固定Encounterを追加する。
- `row10.5`と`row20.5`は表示位置であり、既存の整数`RowIndex`へ小数を混ぜない。
- `RowIndex`は進行度、敵補正、Skill配布の基準として維持する。
- `MapNode`へ表示専用の順序または座標を追加し、`MapLayoutCalculator`だけがそれを参照する。
- Node種別はRivalとパチパチ団で別々に増やさず、汎用の`PartyEncounter`と`PartyEncounterKind`で表現する。
- Encounter Contentは最低限、次を持つ。
  - Encounter種別
  - Trainer情報
  - Enemy Instance IDs
  - 加入候補Instance IDs
  - 選択数
- 博士Iconは独立した進行Nodeではなく、Encounterに付随する候補確認用の副操作として扱う。
- Rival戦は加入前の1対1、パチパチ団戦は加入前の2対2を実装時の初期値とする。

### 主な対象

- `Assets/Scripts/Runtime/Map/NodeType.cs`
- `Assets/Scripts/Runtime/Map/NodeContent.cs`
- `Assets/Scripts/Runtime/Map/MapNode.cs`
- `Assets/Scripts/Runtime/Map/RunMap.cs`
- `Assets/Scripts/Runtime/Map/MapGenerator.cs`
- `Assets/Scripts/Runtime/UI/Map/MapLayoutCalculator.cs`
- `Assets/Scripts/Runtime/UI/Map/MapNodeView.cs`
- `Assets/Scripts/Runtime/UI/Views/Overlays/MapOverlayView.cs`
- `Assets/Scripts/Runtime/Run/MapRunController.cs`

## 4. 加入候補の生成

### 方針

- Start、Rival後、パチパチ団後の全18候補でSpeciesを重複させない。
- これにより候補をMap生成時に固定したまま、どの選択経路でもPartyとのSpecies重複を確実に防げる。
- この全候補Species一意ルールは暫定対応ではなく、将来も維持する。
- 過去に選ばれなかった候補Speciesも、後の加入候補へ再登場させない。
- 候補はRun SeedからMap生成時に一括決定し、加入地点での遅延抽選は行わない。

候補とEnemyは同Speciesでも別Instanceを使う。これは現在の`RunPachimonPool`で対応可能。

## 5. Skill・Passiveの序盤制限

### 現状

- `SkillAsset`にはMap配布可否と属性はあるが、Party人数や進行段階の制限はない。
- `PassiveAsset`にも進行段階の制限はない。
- row0～17では、固定Skillに加えて同属性と別属性の追加Skillを配る前提になっている。

### 変更方針

- `SkillAsset`と`PassiveAsset`へ、使用可能になる最小Party人数を追加する。
- 初期値は1として既存Assetとの互換性を保つ。
- AOEや複数の味方を必要とする効果は2または3を設定する。
- Speciesの加入可能段階は固定SkillとPassiveの必要人数から導出する。
- row0～10は固定Skill + 同属性Skill 1個、合計2Skillとする。
- row11以降は既存の通常Loadoutへ戻す。
- Map配布候補の抽出と生成後Validationの両方で同じ制限を使う。

### 主な対象

- `Assets/Scripts/Runtime/Skill/SkillAsset.cs`
- `Assets/Scripts/Runtime/Battle/Passives/PassiveAsset.cs`
- `Assets/Scripts/Runtime/Pachimon/PachimonSpeciesAsset.cs`
- `Assets/Scripts/Runtime/Map/MapSkillDistributor.cs`
- 各Editor SetupとCatalog Validation

## 6. StartNodeと加入フロー

### StartNode

- `StartNodeContent`と`StartNodeController`は候補数・選択数を引数で持つため、基本構造は再利用できる。
- Map生成値を候補3体・選択1体へ変更する。
- 文言の`9匹/3匹`を`3匹/1匹`へ更新する。
- `StartScreen`の3 x 3前提を、候補数に応じた配置へ変更する。
- 選択後の整列Animationは1体でも成立するよう確認する。

### 追加加入

- StartNodeをそのまま再利用するのではなく、候補選択部分を共通Componentへ切り出す。
- Rival/パチパチ団は、Battle勝利後にDialogueを挟んで共通候補選択へ遷移する。
- 決定時に`RunState.TryAddPartyMember`を呼び、末尾へ追加する。
- 新規加入個体だけEffective MaxHP/MaxMNまで全快させる。
- 既存PartyのHP/MNは変更しない。

### 主な対象

- `Assets/Scripts/Runtime/Run/StartNodeController.cs`
- `Assets/Scripts/Runtime/Run/MapRunController.cs`
- `Assets/Scripts/Runtime/UI/Views/Screens/StartScreen.cs`
- `Assets/Scripts/Runtime/UI/Views/Core/StartCandidateWindowView.cs`
- 候補選択を担う新しい共通View/Controller

## 7. Battle・SidePane・報酬UI

### 流用できる部分

- `BattleUnitAreaView`は3つの表示Slotを走査し、存在しないUnitには`null`を渡せる。
- 前方、後方、全体、対象可能Unitの検索は実在Unitリストを基準にしている。
- LeftPaneは3枠を維持しながら未加入枠をHidden表示できる。

### 調整が必要な部分

- Battle開始条件を「3体揃っている」から「1体以上加入済み」へ変更する。
- 1体・2体Battleで空Slot、クリック、Item Drop Target、Action Gaugeが非表示になることを確認する。
- RewardのEnemy列とPlayer選択列は現在3 Column固定なので、実人数に応じて1～3 Columnへ切り替えて中央寄せする。
- Skill/Passive報酬はダミーEnemyを補充せず、実際に戦ったEnemyだけをSourceにする。
- Trainer/Pachimon Tabは実人数分だけTabを表示し、送り操作を循環させる。

### 主な対象

- `Assets/Scripts/Runtime/UI/Views/Battle/BattleUnitAreaView.cs`
- `Assets/Scripts/Runtime/UI/Views/Battle/BattleMainView.cs`
- `Assets/Scripts/Runtime/UI/Views/Overlays/RewardOverlayView.cs`
- `Assets/Scripts/Runtime/UI/Views/Core/BattleNodeWindowView.cs`
- `Assets/Scripts/Runtime/Run/MapRunController.cs`

## 8. テスト影響

### 新規テスト

- RunStateが1体で初期化でき、2体目・3体目を末尾へ追加できる。
- Instance重複、Species重複、4体目を拒否する。
- BattleSideが1体、2体、3体を受け入れ、0体、4体、Slot欠番を拒否する。
- 1対1、2対2、3対3でBattle生成、対象検索、勝敗判定が成立する。
- RestSpot、Item、Battle結果反映、報酬対象選択が各Party人数で成立する。
- row帯ごとのEnemy人数が1、2、3になる。
- 固定Encounterが全経路で必須通過になる。
- 加入候補がSeed再現可能で、候補内とParty内のSpeciesが重複しない。
- 序盤対象外Skill/Passiveを持つSpeciesがrow0～10へ配置されない。
- row0～10の追加Skillが同属性1個だけになる。

### 既存テスト

- Mapの総割当数300を期待するTestは、使用Instanceの一意性検証へ変更する。
- Start候補9体、Battle常時3体、Skill総数を固定値で期待するTestを更新する。
- 既に1体や2体の`BattleSideState`を作るEditor Testが複数あるため、可変Party対応後はこれらも正式な前提として整理する。

## 実装順

1. `MaxPartySize`と可変Party Domainを導入する。
2. Battle、RestSpot、報酬を1～3体対応にする。
3. Enemy人数ルールと序盤Skill/Passive制限のメタデータを追加する。
4. Mapの300体使い切り制約を外し、可変Slot割当へ変更する。
5. StartNodeを3候補・1体選択へ変更する。
6. 固定Encounter Nodeと表示専用Row位置を追加する。
7. 共通加入候補UIとRival/パチパチ団の加入フローを実装する。
8. Reward、SidePane、Compactの人数可変レイアウトを仕上げる。
9. Map生成・Battle・加入フローのEditor TestとUnity上の通し確認を行う。

この順序なら、Map生成へ手を入れる前に可変Partyの土台を安定させられ、各段階でコンパイルと回帰確認を行える。
