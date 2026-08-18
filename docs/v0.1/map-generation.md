# Map Generation

## 目的

- Run開始時にNormalAreaを生成する
- Node、Edge、NodeContentを事前に確定する
- 同じ`runSeed`と同じ生成仕様から同じMapを再現できるようにする

## 全体構成

| 範囲 | 名前 | 内容 |
| --- | --- | --- |
| row 0 | StarterZone | StartNode、初期候補9体 |
| row 1-2 | OpeningZone | 序盤のBattle Node |
| row 3-35 | AdventureZone | Battle / Gym / RestSpot / City / Event |
| row 36 | LeagueGateZone | 四天王挑戦判定 |
| row 37-40 | EliteZone | 四天王Node |
| row 41+ | GhostNode群 | NormalAreaへ後から追加するGhostNode |

NormalAreaはrow 0からrow 40までとする。GhostNode群は別Areaとして保持せず、NormalAreaの後ろへNodeを追加する。

## 生成タイミング

Run開始時:

- 151種から150種を選び、各種2個体の`RunPachimonPool`を生成
- row 0-40のNodeとEdge
- Node種別
- Trainer / GymLeader
- Pachimonに依存しない`NodeReward`（Gold / RewardElement 2枠 / Badge）
- StartNodeの候補9体
- Battle / Gym / Eliteの敵構成
- その他、Nodeへ入った後に再抽選しない内容

到達時:

- row 41以降のGhostNode群
- Ghost固有の配置内容

## Run生成の全体フロー

```text
runSeedを決定
  -> 151種から不参加の1種を決定
  -> 残り150種を2個体ずつ生成（合計300体）
  -> row 0-40のNodeとEdgeを生成（row 1-35は149Node）
  -> Node種別を配置
  -> NodeReward（Gold / RewardElement 2枠 / Badge）を配置
  -> NodeRewardに対応するTrainer / GymLeaderを配置
  -> 300体をStart / Battle / Gym / Eliteへ割り当て
  -> NodeContentを確定
```

Pachimon個体はMapより先に生成する。Map生成やGym配置だけを再試行した場合も、`RunPachimonPool`は作り直さない。

## RunPachimonPool

- Pachimonの種類数は151種とする
- Runごとに不参加の1種をランダムに選ぶ
- 残り150種を2個体ずつ生成し、合計300体とする
- 同種の2個体もStatsなどは別々に生成する
- 生成した個体には一意な`instanceId`を付ける
- MapNodeの内容は個体本体ではなく`instanceId`を参照する

個体の割り当てでは、少なくとも同種2体を同じ敵partyや同じrowへ配置しない。Edgeで直接つながるNode同士も可能な限り避ける。

## NormalArea生成手順

### 1. Nodeの土台を作る

1. row 0にNodeを1つ生成する
2. row 1-35に基本3Nodeずつ、合計105Nodeを生成する
3. row 36-40にNodeを1つずつ生成する
4. row 1-35が合計149Nodeになるまで、row 2-35へ44Nodeを1つずつ追加配置する

row 1は3Node固定とし、各rowの最大Node数は6とする。

row 1-35の平均Node数は`149 / 35 = 約4.26`となる。均等配置の例では、row 1を3Node、24rowを4Node、10rowを5Nodeにすると合計149Nodeになる。実際の生成では3-6Nodeの範囲で分布を持たせる。

追加配置では、現在のNode数が6未満のrowを候補とし、その中から毎回ランダムに1rowを選ぶ。選ばれたrowへ1Node追加し、44Nodeを配置し終えるまで繰り返す。

### 2. Edgeを作る

- 接続は原則として現在rowから次rowへ向ける
- 各Nodeは次rowへ最低1本接続する
- 1Nodeから次rowへの最大Edge数は2本とする
- StartNodeのみ、row 1の3Nodeすべてへ接続するため最大3本を許可する
- すべてのNodeがStartから到達可能で、LeagueGateへ進めることを保証する
- 見た目上の大きなEdge交差は避ける

#### 基本Edge生成アルゴリズム

隣接する2rowのNodeを`columnIndex`の昇順で並べ、交差しない基本Edgeを比率で割り当てる。

次rowのNode数が現在row以上の場合は、次rowの各Nodeから接続元を決める。

```text
sourceIndex = targetIndex * sourceCount / targetCount
```

現在rowのNode数が次rowより多い場合は、現在rowの各Nodeから接続先を決める。

```text
targetIndex = sourceIndex * targetCount / sourceCount
```

除算は整数除算とする。この基本Edgeにより、次を必ず満たす。

- column最小同士を接続する
- column最大同士を接続する
- 現在rowの全Nodeが最低1本のOutgoing Edgeを持つ
- 次rowの全Nodeが最低1本のIncoming Edgeを持つ
- 基本Edge同士が交差しない
- StartNode以外のOutgoing Edgeが最大2本に収まる

#### 追加Edge

基本Edge生成後、ルートの選択肢を増やすため近隣columnへランダムなEdgeを追加する。現状は、2本目を持てるNodeごとに50%の確率で追加を試す仮設定とする。

- Outgoing EdgeはStartNodeを除いて最大2本
- 接続先は次rowの近隣columnのみ
- 既存Edgeと交差させない
- 同じEdgeを重複させない
- 正規化した横方向の距離が0.4以内のNodeだけを候補にする

追加確率と最大距離は`MapGenerationSettings`に置き、実際のMap表示を見ながら調整する。候補が交差条件を満たせない場合は、2本目を追加せず基本Edgeだけを残す。

#### Edge検証

Edgeはランダム総当たりや成功までの再生成ではなく、基本アルゴリズムで成立させる。生成後に以下を検証し、失敗時は再試行せず生成エラーとして`runSeed`と対象rowを記録する。

- 全NodeがStartNodeから到達可能
- 全NodeからLeagueGateへ到達可能
- Outgoing Edge数が上限以内
- 重複Edgeがない

### 3. Cityを配置する

- row 4 / 8 / 12 / 16 / 20 / 24 / 28 / 32をCity配置rowとする
- 各Cityは横並びの2Nodeを使用する
- 8か所、合計16NodeをCityとして扱う
- 2Nodeは別々の`MapNode`としてEdgeを持つ
- 同じCityに属する2Nodeは共通の`cityGroupId`と`CityNodeContent`を参照し、1つの`MapNodeGroup`にまとめる
- どちらのNodeへ入っても同じCityを起動し、City完了時は両Nodeを解決済みにする
- Cityから進めるNodeは、2Nodeが持つOutgoing Edgeの和集合とする
- 表示上は左右2Nodeを描かず、2つの接続点を持つ横長のCity 1個として描画する
- NodeとEdgeの土台を作った後に配置する
- 横並び2NodeへのIncoming元が、重複統合後に2Node以上ある組だけを配置候補にする
- 横並び2NodeからのOutgoing先が、重複統合後に2Node以上ある組だけを配置候補にする
- 同じ外部NodeとCityの左右を結ぶ複数Edgeは、City全体では1本として数える
- 条件を満たす候補からランダムに1組を選ぶ
- City専用Edgeは生成せず、通常Edgeの追加・削除・再生成も行わない
- 生成後、各CityGroupのIncoming / Outgoingがそれぞれ2本以上あることを検証する

### 4. LeagueGateを配置する

- row 36は`LeagueGate`固定
- Badge条件を満たしていればrow 37へ進む
- 条件未達は特殊敗北として扱う
- 後から専用演出を追加できる結果型を用意する

### 5. Eliteを配置する

- row 37-40を`Elite`固定にする
- Elite戦にはGoldを含むBattle Rewardを付けない

### 6. Gymを配置する

- row 3-35の未使用Nodeから配置する
- 合計24NodeをGymとして配置する
- Gym同士がEdgeで直接連続しないようにする
- 別種Nodeとの連続は許可する
- 配置後にBadgeを8個以上取得できるルートが最低1本あることを検証する
- 条件を満たさない場合はNode / EdgeではなくGym配置だけを再抽選する

ルート判定では全ルートを列挙せず、DAG上の動的計画法で各Node到達時の最大Badge数を計算する。

### 7. RestSpotを配置する

- row 3-35の未使用Nodeから配置する
- 合計24NodeをRestSpotとして配置する
- RestSpot同士がEdgeで直接連続しないようにする
- 別種Nodeとの連続は許可する

配置時は未使用Node全体を候補とし、1つ配置するたびに接続中の同種禁止Nodeを次回候補から外す。

### 8. Eventを配置する

- row 3-35の未使用Nodeから配置する
- 合計16NodeをEventとして配置する
- Event同士がEdgeで直接連続しないようにする
- 別種Nodeとの連続は許可する
- 配置時は未使用Node全体を候補とし、1つ配置するたびに接続中のEvent Nodeを次回候補から外す
- Eventの具体的な内容は後で決める

### 9. 残りをBattleにする

- row 1-35の未使用NodeをすべてBattleにする
- 特殊Nodeを配置しないrow 1-2は、すべてBattleになる
- `149 - City 16 - Gym 24 - RestSpot 24 - Event 16 = 69`より、Battleは合計69Nodeになる
- Battleを個別にランダム配置する処理は持たない

### 10. NodeRewardを配置する

Pachimonに依存しないRewardを、PachimonとTrainerの配置より先に確定する。

- `NodeReward`は`Gold / FirstElement / SecondElement / Badge`を保持する
- BattleNodeのアイコン上部色はFirstElement、下部色はSecondElementから決める
- TrainerThemeはFirstElementから決める

#### Gold

- 69 BattleNodeすべてにGoldを配置する
- 1Nodeあたり500-1500とする
- 69Nodeの総額を69,000に固定し、平均を正確に1,000にする
- 24 GymNodeすべてにもGoldを配置する
- Gymも1Nodeあたり500-1500とし、総額を24,000、平均を正確に1,000にする
- `BonusGold`はこの基礎Goldとは別のRewardElementとして配置する
- `BonusGold`取得時は、枠に応じて4,000または2,000を追加取得する

#### Badge

- 24 GymNodeへBadgeを1個ずつ配置する
- 8属性を各3個ずつ使用し、合計24個とする
- Badge属性はGymLeaderの得意属性としても使用する

#### RewardElement

69 BattleNodeすべてへ、FirstElementとSecondElementを1つずつ配置する。各枠は69要素のDeckを個別に作成・シャッフルして組み合わせる。

FirstElement / SecondElement共通:

- 属性8種を各5回、合計40要素
- MaxHp / MaxMn / Speed / DamageBonus / ResistBonusを各5回、合計25要素
- BonusGoldを4回

各枠は`40 + 25 + 4 = 69`要素となる。FirstElementとSecondElementは同じ内訳のDeckを別々に作成する。

- すべてのRewardElementをどちらの枠にも配置できる
- 同じNodeで同一要素を重複させない
- FirstElementは基準上昇量、SecondElementはその50%の上昇量とする
- FirstElementだけをTrainerThemeと一致Pachimon配置の基準に使う
- 上昇量は`ModValueSettings`へ集約し、生成ロジックへ定数を埋め込まない

仮の上昇量:

| Element | FirstElement | SecondElement |
| --- | ---: | ---: |
| 8属性 | 60 | 30 |
| MaxHP | 100 | 50 |
| MaxMN | 100 | 50 |
| Speed | 20 | 10 |
| DamageBonus | 20 | 10 |
| ResistBonus | 20 | 10 |
| BonusGold | 4,000 | 2,000 |

`BonusGold`は通常の`NodeReward.Gold`とは別の追加報酬とする。

### 11. NodeRewardに合わせてTrainerを配置する

通常Trainerは完全な組み合わせデータをModごとに用意せず、`TrainerTheme`、複数の`TrainerStyle`、`TrainerName`に分けて組み立てる。

```text
NodeRewardを参照
  -> TrainerThemeを決定
  -> Themeに対応するTrainerStyleを選択
  -> Styleの性別に対応するNameDeckから名前を取得
  -> TrainerProfileをNodeへ設定
```

#### TrainerTheme

- FirstElementが属性なら、その属性をTrainerThemeとする
- SecondElementはTrainerThemeの決定には使わない
- 例: `Poison / Ice`は`Poison` Themeとなる
- MaxHp / MaxMn / Speed / DamageBonus / ResistBonus / BonusGoldは対応Themeを使う
- 将来、特定のRewardElement組み合わせへ専用Themeを設定できる余地は残す

#### TrainerStyle

`TrainerStyle`は以下を持つ静的データとする。

- `styleId`
- `theme`
- `gender`
- `graphic`
- `normalTitle`: 通常Battleで使用する肩書。Gym / Elite用Styleでは不要
- `styleCategory`: Normal / League

性別数や男女比は事前に決めない。Themeに対応するStyle群から先にStyleを選び、そのStyleが持つ性別を名前選択に使用する。

- Poison男性例: `虫取り少年`と対応Graphic
- Poison女性例: `虫取り少女`と対応Graphic
- Poison別候補例: `オカルト研究会`と対応Graphic
- Leaf女性例: `森ガール`と対応Graphic
- Gold男性例: `ジェントルマン`と対応Graphic

同じThemeへ複数の`TrainerStyle`を登録できるため、Poison Themeでも`虫取り少年 / 虫取り少女 / オカルト研究会`などからランダムに選べる。

通常TrainerのStyleは別Nodeでの再使用を許可する。GymLeader / EliteのLeague Styleだけは、固有キャラクターとして1Run内で重複使用しない。

#### NameDeck

- 性別ごとに名前候補をシャッフルした`NameDeck`を作る
- 選択済みTrainerStyleの`gender`に対応するDeckから名前を取得する
- 性別比率は登録されたTrainerStyleとランダム選択の結果に任せる
- 同じ性別の名前は、候補を一巡するまで重複させない
- 候補を使い切った場合は再シャッフルして次の周回へ進む
- 性別ごとの名前候補は、通常Trainer / GymLeader / Eliteで共有できる十分な数を用意する

#### TrainerProfile

Nodeが保持する生成済みTrainer情報。表示用データ本体を複製せず、静的データを参照するIDを持つ。

- `styleId`
- `nameId`
- `role`: Normal / GymLeader / Elite

`styleId`からTheme・性別・Graphicを、`nameId`から表示名を取得する。通常Battleの肩書はStyleの`normalTitle`、GymLeader / Eliteの肩書は`role`から決定する。表示例は`虫取り少年のタクヤ`となる。

### 12. GymLeader / Eliteの固有Styleを配置する

GymLeaderとEliteも通常Trainerと同じ`TrainerStyle`型を使用する。ただし`styleCategory = League`の32体を固有キャラクターとして扱う。

#### League TrainerStyle

8属性Themeごとに4体、合計32体を静的データとして用意する。例えばFire Themeに4体のTrainerStyleを登録する。

- `styleId`
- `theme`: 対応属性
- `gender`
- `graphic`
- `styleCategory`: League

肩書と名前はStyleへ固定しない。

- GymLeaderの肩書は常に`ジムリーダー`
- Eliteの肩書は常に`四天王`
- 名前はStyleの性別に対応する、通常Trainerと共通のNameDeckからランダムに割り当てる
- 男性用・女性用で別クラスを作らず、各Styleが`gender`を持つ
- 各属性4体の性別構成は固定せず、制作するGraphicに合わせる

#### 配置手順

```text
属性Themeごとに4体のLeague Styleをシャッフル
  -> 各Themeの先頭3体を同属性のGym 3Nodeへ配置
  -> 各Themeに1体ずつ残す
  -> Elite用に8属性から4属性を重複なしで選ぶ
  -> 選ばれた各Themeの残り1体をEliteへ配置
  -> 選ばれなかった4属性の残り4体は、そのRunでは不参加
```

- Gymでは8属性を各3体、合計24体使用する
- Eliteではランダムな4属性を各1体、合計4体使用する
- 1Runで使用するLeague Styleは合計28体となる
- 同じLeague Styleを1Run内で重複使用しない
- GymLeaderの属性はBadge属性と一致させる
- Eliteの属性は、Elite戦の属性設定にも使用できる形で保持する

### 13. Pachimonを配置する

- 事前生成した300体を重複なく各配置枠へ割り当てる
- 各Speciesは、戦闘には影響しない配置用の`AllocationType`を1つ持つ
- StartNodeへ候補9体を配置し、v0.2でその中から3体を選択する
- Elite 4Nodeへ12体を配置する
- Gym 24Nodeへ72体を配置する
- Battle 69Nodeへ207体を配置する
- `9 + 12 + 72 + 207 = 300`となり、生成した全個体の配置先が決まる
- 同じ敵Partyと同じrowには同種を重複配置しない
- Edgeで直接つながるNodeへの同種配置も可能な限り避ける

#### 配置順

1. Elite / Gymのエース枠28体を配置する
2. Elite / GymのType一致枠56体を配置する
3. FirstElementが属性の通常Trainer 40人へType一致枠を1体ずつ配置する
4. Start候補9体をランダム配置する
5. 残り167体を、未配置枠へランダム配置する

`28 + 56 + 40 + 9 + 167 = 300`となる。

#### Elite / Gym

- Trainerの対象属性と同じ`AllocationType`を一致Typeとする
- エース枠には、未配置個体の中で対象属性値が最も高い個体を配置する
- 高rowから順にエースを確定する
- 残り2枠は、一致Typeからランダムに配置する
- エース自身の`AllocationType`は問わない

#### 通常Trainer

- FirstElementが属性の40Nodeでは、その属性を一致Typeとして一致枠へ1体配置する
- 上記Nodeの残り2体は完全ランダムとする
- FirstElementが属性以外の29Nodeは、3体すべて完全ランダムとする
- TrainerThemeは肩書・Graphicの決定に使い、Pachimon配置用Typeを別途保持しない

#### Enemy Partyの最終順序

- Battle / Gym / Eliteの3体は、全枠の割り当て後にMaxHPが高い順へ並べる
- MaxHPが同値の場合は、割り当て時の順序を維持する
- RightPaneの事前情報とBattle Formationは、この確定済み順序を共通で使用する

配置後、`skill-spec.md`に従ってGymへ一致Skillを2つ、Eliteへ一致Skillを3つ、全個体へrow別ランダムSkillを2-4つ振り分ける。候補は現在の採用回数が最少のSkill群から選び、Skillごとの採用回数をできるだけ均等にする。

### 14. Skill / PassiveのReward選択肢を導出する

- Skill / Passiveの候補一覧はNodeContentへ重複保持しない
- Battle勝利時、`enemyPachimonInstanceIds`から敵の戦闘開始時Loadoutを参照する
- 敵Partyの全Pachimonが保持するSkill / Passiveを取得候補としてその場で組み立てる
- 候補の表示順は敵Party順と各Loadout内の順序から決め、同じRun状態なら再導出しても変化させない
- プレイヤーは候補から取得対象を選択する
- 同じSkill / Passiveが複数の敵に含まれる場合の重複候補の扱いは、Reward実装時に決める
- 将来候補をランダムに絞り込む場合だけ、その抽選Seedまたは確定候補IDを保存する

### 15. NodeContentを確定する

- `NodeReward`: Gold / RewardElement 2枠 / Badge
- `TrainerProfile`: 全Trainer共通のRole / Style / Name参照
- Skill / Passiveは敵Pachimonから導出し、NodeContentには候補一覧を持たせない
- UI上ではNodeRewardと導出したSkill / PassiveをまとめてBattle Rewardとして表示する
- Itemはv0.4のReward生成対象に含めない
- Run中にNodeへ入るたび内容を再抽選しない

EliteはGold Rewardなしとする。Gold以外のRewardを付与するかは未決定とし、v0.1では後から設定できる構造だけを用意する。

## Seed

- NormalAreaは`runSeed`から生成する
- 生成処理内では再現可能な乱数源を使用する
- Unityのグローバルな`Random`状態へ依存しない形を目標とする
- GhostNode群は到達時点の情報と専用Seedから生成できるようにする

## v0.1で後回しにする精密化

- 追加44Nodeの分布バランス調整
- 同種2個体の距離保証
- Eventの内容
- 敵とRewardの最終バランス
- GhostNode群の生成詳細
