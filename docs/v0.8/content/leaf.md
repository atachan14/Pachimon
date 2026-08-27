# Leaf Content

> 花粉の共通仕様と付与量は [Pollen](./statuses/pollen.md) を参照。

## Pachimon

### ヒナボッコ

- Status: `Implemented`
- Species ID: `11`
- モチーフ: 大きな若葉で日光浴する芽獣
- 狙い: 日光浴と健康植物で回復を伸ばす自己回復型

#### Fixed Skill: 日光浴

- 自身を `150 x AmplificationMultiplier(Leaf x 100%)` 回復する。
- 正の気温に応じて回復量を増加する。
- 雨の場合、雨Valueを `ReductionMultiplier` へ通して回復量を低下する。
- 仮値: 硬直100 / CD250 / MN100。

#### Passive: 健康植物

- 自身が受ける回復効果を `15% + Leaf x 10%` 増加する。

### ツタヘビ

- Status: `Implemented`
- Species ID: `19`
- モチーフ: 輪状の蔦が連なる植物蛇
- 狙い: 連鎖攻撃とSlowを複数の敵へつなげる妨害型

#### Fixed Skill: 連鎖する蔦

- 追加連鎖数2。
- 連鎖先へLeafダメージとSlowを与える。
- 使用時、自身へアドチェインを1付与する。
- 仮値: Damage 70 / Slow 25 / Slow用Leaf Ratio 100%。DamageはLeafを100%参照する。

#### Passive: 絡まる蔦

- 自身が与えるSlowのValueをLeafに応じて増加する。

### ソラバナ

- Status: `Implemented`
- Species ID: `27`
- モチーフ: 太陽レンズを背負う花獣
- 狙い: 気温で発生を短縮し、ソーラービームを撃つ砲撃型

#### Fixed Skill: ソーラービーム

- 発生100 / 硬直100。
- 敵先頭へLeafダメージを与える。
- 正の気温を `ReductionMultiplier` へ通し、発生を短縮する。
- 本処理とPreviewは同じTiming Calculatorを使用する。

#### Passive: 温暖植物

- 気温が正の場合、`floor(気温 x 30%)` だけSpeedが増加する。

### マキツル

- Status: `Implemented`
- Species ID: `35`
- モチーフ: 太い蔓と葉を鎧のように巻いた植物獣
- 狙い: 発生中やStun中に耐久を高め、敵もろとも動きを止める防御型

#### Fixed Skill: 絡み合う蔓

- 敵先頭と自身を `100 x AmplificationMultiplier(Leaf x 100%)` tick Stunさせる。

#### Passive: 堅牢な植物

- 発生中またはStun中、`floor(Leaf x 60%)` だけResistBonusが増加する。

### シビレダケ

- Status: `Implemented`
- Species ID: `43`
- モチーフ: 麻痺粉を頬に蓄える二段傘のキノコ獣
- 狙い: 全体へ状態を撒き、その回数で自身のLeafを育てる妨害型

#### Fixed Skill: しびれ粉

- 全敵へ次の麻痺を与える。
- Duration: `50 x AmplificationMultiplier(Leaf x 100%)`
- Value:
  - `60 x AmplificationMultiplier(Electric x 100%)`
  - `40 x AmplificationMultiplier(Poison x 100%)`

#### Passive: 粉植物

- 自身のSkillが敵1体へ状態を付与するたび、自身のLeafがBattle中10増加する。
- 全体攻撃で3体へ付与した場合は3回発動する。

## Temporary Tuning

- 数値はテスト用の仮値。各Skill / Passive SOから調整する。
- 技マシーンは開発用Run Profileの初期Itemへ登録済み。

### ビートン

- Status: `Implemented`
- Species ID: `51`
- モチーフ: 鼓動する赤い種を葉装甲で守る植物獣
- 狙い: 植物を並べて定期攻撃し、植物数に応じて火力を伸ばす展開型

#### Fixed Skill: ビートヴァイン

- `Value = 30 × AmplificationMultiplier(Leaf × 100%)` の
  `[生成物：植物：ビートヴァイン]` を生成する。
- 仮値: 硬直100 / CD300 / MN100。

[ビートヴァイン]

- 生成から100tick後に最初の攻撃を行い、以後100tickごとに攻撃する。
- 敵先頭へValueの草ダメージを与える。
- ダメージには対象のLeafとResistBonusによる軽減を適用する。
- 攻撃側のLeafとDamageBonusは重ねて適用しない。
- 効果時間はBattle中恒久。
- 再生成時は統合せず、別の植物として追加する。

#### Passive: 植物園

- 所有者が生存している間、DamageBonusを`自陣の植物数 × 15`増加させる。
- 植物は種類を問わず、個別に生成された各インスタンスを1つと数える。

### カエンバナ

- Status: `Implemented`
- Species ID: `59`
- モチーフ: 炎色の花弁と火種の尾を持つ細身の花獣
- 狙い: 炎と草の連携ダメージを連鎖させ、双方の属性値を育てる混成型

#### Fixed Skill: ファイアヴァイン

- `草Value = 15 × AmplificationMultiplier(Leaf × 100%)`。
- `炎Value = 15 × AmplificationMultiplier(Fire × 100%)`。
- 上記Valueを持つ`[生成物：植物：ファイアヴァイン]`を生成する。
- 仮値: 硬直100 / CD300 / MN100。

[ファイアヴァイン]

- 味方Pachimonが炎または草ダメージを与え、HPかShieldへ1以上反映された時に発動する。
- 同じ対象へ草Valueの草ダメージ、続けて炎Valueの炎ダメージを与える。
- Field由来のダメージでは発動しないため、ファイアヴァイン同士は再帰しない。
- 複数存在する場合は、それぞれが1回ずつ発動する。
- 効果時間はBattle中恒久。
- 再生成時は統合せず、別の植物として追加する。

#### Passive: 燃える花

- 所有者が生存している間、全陣営で発生したダメージを監視する。
- 炎ダメージがHPかShieldへ1以上反映されるたびに、所有者のLeafがBattle中恒久で5増加する。
- 草ダメージがHPかShieldへ1以上反映されるたびに、所有者のFireがBattle中恒久で5増加する。
- 攻撃・状態・Fieldなど、Damage Originは問わない。
