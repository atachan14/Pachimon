# Leaf Content

## Pachimon

### 草002

- Status: `Implemented`
- Species ID: `11`

#### Fixed Skill: 日光浴

- 自身を `150 x AmplificationMultiplier(Leaf x 100%)` 回復する。
- 正の気温に応じて回復量を増加する。
- 雨の場合、雨Valueを `ReductionMultiplier` へ通して回復量を低下する。
- 仮値: 硬直100 / CD250 / MN100。

#### Passive: 健康植物

- 自身が受ける回復効果を `15% + Leaf x 10%` 増加する。

### 草003

- Status: `Implemented`
- Species ID: `19`

#### Fixed Skill: 連鎖する蔦

- 追加連鎖数2。
- 連鎖先へLeafダメージとSlowを与える。
- 使用時、自身へアドチェインを0.5付与する。
- 仮値: Damage 70 / Slow 25 / 各Leaf Ratio 100%。

#### Passive: 絡まる蔦

- 自身が与えるSlowのValueをLeafに応じて増加する。

### 草004

- Status: `Implemented`
- Species ID: `27`

#### Fixed Skill: ソーラービーム

- 発生200 / 硬直100。
- 敵先頭へLeafダメージを与える。
- 正の気温を `ReductionMultiplier` へ通し、発生を短縮する。
- 本処理とPreviewは同じTiming Calculatorを使用する。

#### Passive: 温暖植物

- 気温が正の場合、`floor(気温 x 30%)` だけSpeedが増加する。

### 草005

- Status: `Implemented`
- Species ID: `35`

#### Fixed Skill: 絡み合う蔓

- 敵先頭と自身を `100 x AmplificationMultiplier(Leaf x 100%)` tick Stunさせる。

#### Passive: 堅牢な植物

- 発生中またはStun中、`floor(Leaf x 60%)` だけResistBonusが増加する。

### 草006

- Status: `Implemented`
- Species ID: `43`

#### Fixed Skill: しびれ粉

- 全敵へ次の合計値の麻痺を与える。
- `50 x AmplificationMultiplier(Leaf x 100%)`
- `50 x AmplificationMultiplier(Poison x 100%)`

#### Passive: 粉植物

- 自身のSkillが敵1体へ状態を付与するたび、自身のLeafがBattle中10増加する。
- 全体攻撃で3体へ付与した場合は3回発動する。

## Temporary Tuning

- 数値はテスト用の仮値。各Skill / Passive SOから調整する。
- 技マシーンは開発用Run Profileの初期Itemへ登録済み。
