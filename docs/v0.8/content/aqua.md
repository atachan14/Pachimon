# Aqua Content

## Pachimon

### パチナミ

- Status: `Implemented`
- Species ID: `10`
- モチーフ: 波のしっぽを持つ水獣
- 狙い: 全MNを波へ変換する、水の波動と生命の水の使い手

#### Fixed Skill

- 名前:水の波動
- 対象:先頭の敵
- 硬直:150
- CD:300
- 消費MN: 原則、使用時のCurrentMNをすべて消費する
- 効果: 効果計算用MN消費量とAquaを参照し、Aqua Damageを与える

実装案:

```text
Damage
= max(1, floor(
    効果計算用MN消費量
    × AmplificationMultiplier(Aqua)
  ))
```

- CurrentMNが1以上の場合だけ使用できる
- 通常時の効果計算用MN消費量は実消費MNと同じにする
- `進水式`の状態中だけ、実消費MNと効果計算用MN消費量が異なり得る
- 水の波動本体だけで対象を戦闘不能にできる場合は、戦闘不能に必要な最小MNのみ消費する
- 必要MNの判定には水の波動本体によるHP DamageとShield吸収を含め、Passive・状態・フィールドなどが後から発生させる追加Damageは含めない
- 水の波動本体が回避される場合や、CurrentMNを全消費しても本体だけでは戦闘不能にできない場合は、CurrentMNをすべて消費する
- DamageはAquaを100%参照し、威力は効果計算用MNで調整する

#### Passive

- 名前:生命の水
- 効果:自身がMNを消費したとき、MN消費量とAquaに応じて自身のHPを回復する

実装案:

```text
回復率
= max(0,
    BaseRecoveryRatio 20%
    + Aqua × AquaRecoveryRatio 5% / 100
  )

回復量
= floor(効果計算用MN消費量 × 回復率)
```

- `BaseRecoveryRatio`と`AquaRecoveryRatio`はPassive SOから調整可能にする
- 通常回復として扱い、最大HPを超えず、戦闘不能から復帰しない
- Skill効果解決後に回復する
- Skillの基本MN、追加MN、全MN消費をすべて集計対象にする
- 1回の効果解決中に複数回MNを消費した場合、その効果解決で使用した効果計算用MN消費量の合計を参照する

### アメガエル

- Status: `Implemented`
- Species ID: `18`
- モチーフ: 雨雲を背負うカエル
- 狙い: 雨を呼び、雨量に応じて素早くなる天候型

#### Fixed Skill

- 名前:あまごい
- 効果:[天気：雨]を付与する。

```text
雨Value
= floor(BaseValue + Aqua × 解決済AquaRatio / 100)
```

- 仮値は`BaseValue = 400`、`AquaValueRatio = 100%`
- 雨は毎tick `1`ずつ減衰する
- 同じ雨の再生成ではValueを加算する

[Weather：Rain]
- 気温が0以上なら雨として扱う
- 気温が負なら雪として扱う
- 雨ではダメージ軽減に適用されないAqua増加とFire低下を行う
- 雨ではValueに応じて[漏電]のValueを加算する
- 雪では漏電を加算しない

```text
雨中のAqua Ratio倍率
= AmplificationMultiplier(RainValue × 10%)

雨中のFire Ratio倍率
= ReductionMultiplier(RainValue × 20%)
```

- 各Ratioは`RainWeatherAsset`で調整可能

##### 雨による漏電付与

- 雨専用の状態は作らず、通常の`漏電`へ直接加算する
- 雨中は1tickごとに、現在の雨Valueに応じたValueを漏電へ加算する
- 1tickごとの端数はRain Runtimeの小数Workへ保持し、整数化できた分だけ加算する
- 雨の消滅や雪への切替後も、既に加算された漏電Valueは残る
- PachimonによるElectric攻撃を受けた場合に漏電を消費する
- 消費後も雨が存在する場合、次のtickから再びValueが蓄積する

```text
1tickの漏電加算Work
= RainValue × LeakValueRatioPerTick / 10000
```

- 仮値は`LeakValueRatioPerTick = 7`
- 雨Valueが`500`から`400`へ減衰する100tickで、漏電Valueが約`31`増える
- 雨生成時には即時付与せず、次のtickから蓄積を開始する

#### Passive

- 名前:雨男
- 効果:雨のときにValue依存でSpeedが上昇する。

```text
Speed倍率%
= 100 + floor(RainValue × 3%)
```

- 雪のときは発動しない
- 仮Ratioは`RainManPassiveAsset`で調整可能

### パチフネ

- Status: `Implemented`
- Species ID: `26`
- モチーフ: 小舟型の甲羅を背負う水棲獣
- 狙い: 進水式で次のSkillを補助し、MaxMNをAquaへ変換する

#### Fixed Skill

- 名前:進水式
- 硬直:20
- CD:120
- 消費MN:0
- 効果:自身に、次のTurnのSkill使用へ適用する[状態:進水式]を付与する

[状態:進水式]の実装案:

```text
MN消費倍率
= ReductionMultiplier(Aqua × MnReductionRatio 100%)
```

- Aqua 0で100%、Aqua 100で50%、Aqua 200で約33.3%消費する
- Skill選択時の使用可否と実消費MNは、進水式によるAqua 20%増加を適用した後のAquaから計算する
- 固定MN Skillでは、実消費MNを`ceil(BaseManaCost × MN消費倍率)`とする
- 全MN消費SkillではCurrentMNをすべて実消費する
- 全MN消費Skillの効果計算用MN消費量は、`実消費MN / MN消費倍率`で求める
- 効果計算用MN消費量は小数のまま保持し、各効果の最終値を求める段階で端数処理する
- 状態中は最終Aquaへ120%の乗算補正を適用する
- 進水式の効果解決時に状態を付与し、次に使用するSkillの効果解決後に消費する
- Skill選択から効果解決までは状態を保持し、PachimonTabにも表示する
- 発生中もAqua 20%増加を継続し、Skill以外のAqua参照処理にも適用する
- `わるあがき`や対象不在で終わったSkillでも、効果解決後に消費する
- 発生中に使用者が戦闘不能となりSkillが中断された場合は消費しない

#### Passive

- 名前:海の力
- 効果:MaxMNに応じてAquaが派生加算される

実装案:

```text
Aqua派生加算
= floor(max(0, MaxMN) × MaxMnRatio 2%)
```

- 非Battle中のPachimonTabにも反映する
- `MaxMnRatio`はPassive SOから調整可能にする
- 既存Stat Calculatorの派生加算段階で処理する
- MaxMNが負になる構造を将来許可した場合も、参照値の下限は0とする

### パチベール

- Status: `Implemented`
- Species ID: `34`
- モチーフ: 水膜をまとうクラゲ
- 狙い: 水のベールを生成し、味方への回復を強化する防御型

#### Fixed Skill

- 名前:水のベール
- 硬直:120
- CD:300
- 消費MN:350
- 効果:[自陣フィールド]にAqua依存のValueを持つ[水のベール]を生成する

実装案:

```text
生成Value
= floor(BaseValue 300
  × AmplificationMultiplier(Aqua × AquaValueRatio 100%))
```

- 同じ陣営へ再生成した場合はValueを加算する
- Valueは毎tick`1`減少し、0で消滅する
- Skill SOは`WaterVeilFieldEffectAsset`を参照する
- Value式と毎tick減少量はField Effect SOから調整可能にする

##### [生成物:水のベール]

- 1tickごとに生存している味方全員のHPを1回復する
- 味方が受けるAqua DamageとFire Damageを30%軽減する
- 軽減は属性・ResistBonusによる防御計算後、Shield適用前に行う
- Shieldへ入るDamageにも軽減を適用する
- 回復は生成した次のtickから開始する
- 生成者が戦闘不能になっても残存する
- 将来、複数Definitionの水のベールが存在しても、30%軽減は一度だけ適用する
- 回復値と軽減率は`WaterVeilFieldEffectAsset`から調整可能にする

#### Passive

- 名前:水の加護
- 効果:自身が生存中、味方が受けるHP回復量を自身のAquaに応じて増加させる

実装案:

```text
回復増加率
= max(0,
    BaseRecoveryBonus 15%
    + Aqua × AquaRecoveryRatio 10% / 100
  )

最終回復量
= floor(
    元の回復量
    × Π(各水の加護の(100% + 回復増加率))
  )
```

- `BaseRecoveryBonus`と`AquaRecoveryRatio`はPassive SOから調整可能にする
- 所持者が戦闘不能中は適用しない
- Skill、Passive、Status、Field Effect、ItemによるBattle中のHP回復を対象にする
- 所持者自身が受ける回復にも適用する
- 複数の水の加護が存在する場合、それぞれの増加率を個別に乗算する
- 回復増加率の下限は0とし、Aquaが負でも回復量を減少させない
- 非Battle中のItemやRestSpotによる回復には適用しない

### ミズノコ

- Status: `Implemented`
- Species ID: `42`
- モチーフ: 水刃のヒレを持つノコギリエイ
- 狙い: 貫通攻撃で敵を倒し、追加Turnへつなげる攻撃型

#### Fixed Skill

- 名前:ウォーターカッター
- 硬直:100
- CD:300
- 消費MN:100
- 効果:先頭の敵にWind参照の貫通を持つAqua Damageを与える

実装式:

```text
Damage
= floor(BaseAquaDamage 100
  × AmplificationMultiplier(Aqua))

貫通Value
= Wind × WindPenetrationRatio 25%

Aqua割合貫通率
= 貫通Value / (100 + 貫通Value)
```

- BaseDamageと貫通率はSkill SOから調整可能にする
- 割合貫通率は対象のAquaによる防御値だけへ適用する

#### Passive

- 名前:水切り
- 効果:自身のSkillで敵を戦闘不能にしたとき、続けてTurnを行う
- 硬直を0にするだけではなく、同tickの待機者より優先して自身のTurnを開始する
- 複数の敵を同時に戦闘不能にしても、追加Turnは1回だけ得る
- 戦闘が終了した場合は追加Turnを開始しない

### ロカドン

- Status: `Implemented`
- Species ID: `50`
- モチーフ: 泥をろ過する両生獣
- 狙い: 泥水でSlowを与え、PoisonをAquaへ変換する複合型

#### Fixed Skill

- 名前:泥水
- 硬直:100
- CD:300
- 消費MN:100
- 効果:先頭の敵にAqua DamageとPoison参照のSlowを与える

実装式:

```text
Damage
= floor(BaseAquaDamage 100
  × AmplificationMultiplier(Aqua))

Slow Value
= floor(BaseSlow 100
  × AmplificationMultiplier(Poison × PoisonSlowRatio 100%))
```

- DamageとSlowは同じSkill Hitとして扱う
- 回避や肩代わりが発生した場合、DamageとSlowの対象を分離しない
- 各Base、PoisonSlowRatio、Slow DefinitionはSkill SOから調整可能にする

#### Passive

- 名前:ろ過水
- 効果:AquaをPoisonの30%増加する
- 既存Stat Calculatorの派生加算段階で処理する
- 非Battle中のPachimonTabにも反映する
- RatioはPassive SOから調整可能にする

### クジラン

- Status: `Implemented`
- Species ID: `58`
- モチーフ: 潮模様を持つ小型クジラ
- 狙い: 高い現在HPとMaxHPをAqua攻撃へ変換する重量型

#### Fixed Skill

- 名前:しおふき
- 硬直:120
- CD:350
- 消費MN:120
- 効果:先頭の敵にAquaと現在HPを参照するAqua Damageを与える

実装式:

```text
Damage
= floor(BaseAquaDamage 100 × (
    AmplificationMultiplier(Aqua)
    + CurrentHP / CurrentHpDivisor 2000
  ))
```

- 現在HPはSkill効果解決時の値を参照する
- BaseDamageとCurrentHpDivisorはSkill SOから調整可能にする

#### Passive

- 名前:クジラ
- 効果:AquaをMaxHPの1.5%増加する
- 既存Stat Calculatorの派生加算段階で処理する
- 非Battle中のPachimonTabにも反映する
- 小数Ratioを扱えるよう、派生加算PassiveのRatioはfloatで保持する
- RatioはPassive SOから調整可能にする

## Ideas
