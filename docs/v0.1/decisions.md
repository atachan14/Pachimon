# Decisions

仕様判断を短く記録する。確定した内容は該当仕様にも反映する。

## 決定済み

### D-001: v0.1の完成対象はMap

- Run初期化、Map生成、Map表示、Node選択を完成させる
- 各Nodeの内部処理はスケルトンでよい

### D-002: 旧docsはv0.0として凍結

- 整理前のdocsは`docs/archive/v0.0`へ保存する
- 現在の仕様は`docs/v0.1`を正とする

### D-003: CSVとDefinitionTable群は使用しない

- 静的データは当面code-firstで扱う
- Pachimon固有情報は、必要になった時点で軽量なCatalogまたはScriptableObjectを検討する
- Skill / Passiveは固有LogicをC#で実装する

### D-004: playerのPachimonはpartyだけ

- row 0で9体から3体を選ぶ
- 別のownedPachimon一覧は持たない
- 選んだ3体でRunを最後まで進める

### D-005: Mapは一度構築する

- Run開始時にNode / Edge Viewを生成する
- HeaderのMapボタンは表示と非表示だけを切り替える
- 進行時は現在、解決済み、選択可能状態だけを更新する

### D-006: Battle Rewardの範囲

- Battle Rewardは`Gold / Mod / Skill / Passive / Badge`
- ItemとCityはRewardの完成条件に含めない
- BadgeもReward処理を通して取得する

### D-007: GhostはNormalAreaの後ろへ追加

- 現在Areaを表す状態は持たない
- row 41以降にGhostNode群を追加する

### D-008: PachimonはMapより先に生成

- Pachimonは151種とする
- Runごとに不参加の1種を決める
- 残り150種を2個体ずつ、合計300体生成する
- `RunPachimonPool`をMapより先に生成し、Mapから個体IDで参照する

### D-009: NormalAreaのNode数と内訳

- row 1-35は合計149Node、平均約4.26Node、各row最大6Nodeとする
- 追加44Nodeは、6Node未満のrow 2-35から毎回1rowをランダムに選んで配置する
- Cityはrow 4刻みの8か所、各2Nodeの合計16Nodeとする
- Gymは24Nodeとする
- RestSpotは24Nodeとする
- Eventは16Nodeとする
- 残り69NodeをBattleとする
- Eliteはrow 37-40の4Nodeとする
- row 41に仮の殿堂入りNodeを1つ配置し、row 40から接続する
- Start 9体、Battle 207体、Gym 72体、Elite 12体へ300体すべてを割り当てる

### D-010: Event Nodeを追加

- AdventureZoneへEventを16Node配置する
- Event同士がEdgeで直接連続しないようにする
- 別種Nodeとの連続は許可する
- 配置候補の絞り方はRestSpotと同じとする
- v0.1ではMap上のNode種別と画面スケルトンへの接続を対象とする
- Event内容は後で決める

### D-011: Edgeは比例接続で成立を保証

- 隣接rowのNodeをcolumn順に並べ、Node数の比率で基本Edgeを割り当てる
- column最小同士とcolumn最大同士を必ず接続する
- 全Nodeに最低1本のIncoming / Outgoing Edgeを保証する
- Outgoing Edgeは最大2本とし、StartNodeだけ最大3本を許可する
- ランダム性は成立済みの基本Edgeへ近隣Edgeを追加する用途だけに使う
- 検証失敗時は無制限に再生成せず、生成エラーとして扱う

### D-012: Cityは2Nodeで1つの内容を共有

- 各Cityは横並びの2つのMapNodeで構成する
- 2Nodeは同じ`cityGroupId`とCityNodeContentを参照する
- 各Nodeは独立したEdgeを持つが、進入時は同じCityを起動する
- 通常Edge生成後、統合後のIncoming / Outgoingが各2本以上ある隣接ペアへCityを配置する
- City用の特殊Edge生成やEdge修正は行わない

### D-013: Gymクリアルートを最低1本保証

- Badgeを8個以上取得してLeagueGateへ到達できるルートを最低1本保証する
- 条件未達時はGym配置だけを再抽選する
- 判定にはDAG上の動的計画法を使う

### D-014: Skill / PassiveのReward候補は敵から導出する

- `NodeReward`はPachimon配置前に確定するGold / RewardElement 2枠 / Badgeを持つ
- Skill / Passiveの候補一覧はNodeContentへ保持しない
- Reward表示時に敵Pachimonの戦闘開始時Loadoutから候補を導出する
- UI上ではNodeRewardと導出した候補をまとめてBattle Rewardとして扱う
- 69 BattleNodeの基礎Gold総額は69,000、各Nodeは500-1500とする
- 各Battle RewardはFirstElementとSecondElementを1つずつ持つ
- 両枠とも、属性8種を各5回、MaxHp / MaxMn / Speed / DamageBonus / ResistBonusを各5回、BonusGoldを4回配置する
- FirstElementは基準量、SecondElementは50%の上昇量とする
- BonusGoldは基礎Goldとは別に、FirstElementで4,000、SecondElementで2,000を追加する
- GymのBadgeは8属性を各3個、合計24個配置する

### D-015: TrainerはNodeRewardから組み立てる

- NodeRewardを先に配置し、その内容からTrainerThemeを決定する
- FirstElementからTrainerThemeを決定し、SecondElementはTheme決定に使わない
- TrainerStyleはTheme・性別・Battle / RightPane共用Graphicを持ち、通常Battle用Styleだけ肩書も持つ
- 性別は先に分配せず、選ばれたTrainerStyleから決定する
- 名前は性別ごとのNameDeckから、候補を一巡するまで重複なしで割り当てる
- Nodeは生成済みの`role / styleId / nameId`をTrainerProfileとして保持する
- 同じThemeへ複数のTrainerStyleを登録し、その中からランダムに選択できる

### D-016: GymLeader / Eliteは32体のLeague Styleを共有する

- 通常Trainer / GymLeader / Eliteで共通のTrainerStyle型を使用する
- 8属性Themeごとに4体、合計32体の固有League Styleを用意する
- 各StyleはTheme・性別・Battle / RightPane共用の一枚絵Graphicを持つ
- MapIconはStyleに依存しない共通4レイヤーとし、FirstElement / SecondElementに応じた色だけを適用する
- GymLeaderの肩書は`ジムリーダー`、Eliteの肩書は`四天王`で固定する
- 名前はStyleの性別に対応するNameDeckから割り当てる
- Gymは各属性3体ずつ、合計24体を使用する
- Eliteは重複なしで選んだ4属性から各1体、合計4体を使用する
- Eliteに選ばれなかった4属性の残り4体は、そのRunでは不参加とする

### D-017: Pachimonの配置用TypeとTrainer編成

- Pachimon Speciesは戦闘に影響しない8種の`AllocationType`を持つ
- Elite / Gymは対象属性値最上位のエース1体と、Type一致2体で編成する
- FirstElementが属性の通常TrainerはType一致1体とランダム2体で編成する
- FirstElementが属性以外の通常Trainerは3体すべてランダム編成とする
- TrainerProfileへ配置用Typeを重複保持せず、FirstElementの属性またはLeagueのThemeから必要時に導出する

### D-018: 属性名とHeader表示順

- 属性は`Fire / Aqua / Leaf / Electric / Poison / Ice / Wind / Dragon`の8種とする
- 旧`Water`は`Aqua`、旧`Earth / Ground`は`Wind`へ改名する
- Headerは上段を`Fire / Aqua / Leaf / Electric`とする
- Headerは下段を`Poison / Ice / Wind / Dragon`とする
- Headerの`Electric`表示は`Elec`へ省略してよい
- 仮配色は`#E84B3C / #356AE0 / #288A47 / #F2C94C / #FFA7DF / #62D5E6 / #91C83E / #707887`とする
- 保存済みEnum値を壊さないため、内部の数値順とHeaderの表示順は分離する
- 色だけに依存せず、将来は属性ごとの頭文字またはIconも併用する

### D-019: Skillは継承SOと単一Catalogで管理する

- Skillは最小共通項目を持つ`SkillAsset`を継承し、固有Logicと固有データは派生Assetへ持たせる
- 全Skillを単一の`SkillCatalog`へ登録する
- Map振り分け判定は`isMapAssignable`で行う
- ID 1-151は固定Skillであると同時に、ランダム・Type一致振り分けの対象とする
- ID 1000-1999は技マシン限定Skill、ID 2000以降はSystem Skillの慣例帯とする
- ID帯は判定ロジックに使わず、技マシンは通常Skillも限定Skillも参照可能とする
- `わるあがき`はID 2000のMap振り分け対象外Skillとする

### D-020: Map上の全Node情報をRun開始時から公開

- Battle、Gym、Elite NodeのTrainer/Pachimon情報は、到達状況にかかわらず常に閲覧可能とする
- 情報の閲覧可否とNodeへの進行可否は分離し、進行可能なNodeだけ決定操作を表示する
- 公開状態は変化しないため、`RunState`に`revealedNodeIds`を保持しない

### D-021: Statを14種へ再構成する

- 属性ごとのPower / Resistを8つの統合属性へ変更する
- 攻撃側が参照するStatはSkill Logicが決め、防御側はDamage属性に対応する統合属性値を使う
- TurnHasteをSpeedへ改名し、HasteをCooldown短縮用の個体Statとして採用する
- Haste Modは生成しない
- UniversalPower / UniversalResistをDamageBonus / ResistBonusへ改名する
- DamageBonus / ResistBonusはTrainer専用値ではなくPachimon固有Statとする
- MNをHP同様のRun永続Resourceとして追加し、Battle開始時にはリセットしない
- 現行の基本SkillはMNを消費しない
- MaxHP / MaxMNはValue Unitを10倍し、Speed / Haste / DamageBonus / ResistBonusはValue Unitを3分の1にして表示する
- First / Second Reward Deckは同じ69要素構成とし、SecondElementの上昇量をFirstElementの50%にする

## v0.1で決めること

- 追加Edgeの出現率
- 追加44Nodeの分布バランス
- 同種2個体をどこまで離すか
- Eventの内容
- MapContentの行間隔、列間隔、スクロール方向
- Map Node / EdgeのPrefab構成

## 後のバージョンで決めること

- Pachimon固有情報の最終的な保存形式
- Stats生成設定の最終的なバランス値
- Passiveの登録方式
- Battle Rewardの抽選詳細
- Item、City、Ghostの詳細
- Save / Load形式
