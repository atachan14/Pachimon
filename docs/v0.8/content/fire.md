# Fire Content

## Pachimon

### [Pachimon名]2

- Status: `Implemented`
- Species ID: `9`
- モチーフ:
- 狙い:

#### Fixed Skill

- 名前: バックファイア
- Implementation: `Implemented`
- 硬直: `100`
- CD: `200`
- MN: `100`
- 対象: 最後尾の敵
- 効果:
  - 次のFireダメージを与える
  - Poisonに応じた貫通率を持つ

```text
ダメージ
= 100 × AmplificationMultiplier(Fire × 100%)

貫通率
= 10 × AmplificationMultiplier(Poison × 100%)
```

- `BasePower / FireScalingPercent / BasePenetrationPercent / PoisonScalingPercent`はSOで調整する

#### Passive

- 名前:闇の炎
- Implementation: `Implemented`
- 効果:Fire Damageを与えたとき、与えた軽減前DamageのPoison依存割合を追加Poison Damageとして同じ対象へ与える。

```text
変換率
= BaseConversionPercent
× AmplificationMultiplier(Poison × PoisonScalingPercent)

追加Poison DamageのBaseDamage
= Fire Damageの軽減前Damage × 変換率 / 100

BaseConversionPercent = 20%
PoisonScalingPercent = 100%
```

- 元のFire Damageへ適用済みのDamageBonusを追加Damageへ二重適用しない
- 追加Damageには対象のPoisonとResistBonusによる軽減を適用する
- 追加DamageはPassive起点の攻撃として扱う
- 追加Damageに攻撃側Poison倍率を重ねず、他の与Damage補正も再適用しない
- 闇の炎自身が発生させた追加Damageから再発動しない
- 元のFire Damageで対象が戦闘不能になった場合は追加Damageを発生させない
- `BaseConversionPercent / PoisonScalingPercent`はSOで調整可能にする

### [Pachimon名]3

- Status: `Implemented`
- Species ID:
- モチーフ:
- 狙い:
#### Fixed Skill

- Implementation: `Implemented`
- 名前:チェインバーン
- 連鎖: 1回
- 硬直: 130
- CD: 250
- MN: 100
- 効果:
[敵の先頭] に次のFireダメージを与える。

```text
BaseDamage
= 80 × AmplificationMultiplier(Fire × 100%)
```

使用するごとに、自分へ[アドチェイン]を0.5付与する。
実際の追加連鎖回数には[アドチェイン]の小数部分を切り捨てて反映する。
`BasePower / FireScalingPercent / BaseChainCount / AddChainGainUnits`はSOで調整可能にする。

##### [連鎖]
本体を含むHit番号を`i = 0..N`、追加連鎖回数を`N`とすると、各Hitへ次の倍率を適用する。

```text
ChainRatio(i, N) = (N + 1 - i) / (N + 1)
```

- 最初は敵の先頭を対象にする
- 次は後方へ進み、最後尾へ到達したら前方へ折り返す
- 対象が1体なら同じ対象へ繰り返す
- 各Hit直前に生存中の隊列を再取得し、途中で戦闘不能になった対象を除外する
- 各Hitを独立したDamage解決として扱い、対象の軽減とDamageイベントを毎回適用する
- Damageの最終端数処理はHitごとに行う
- Skill名は最初のHitでのみ表示し、2Hit目以降は再発動扱いにせずDamage行だけを連続表示する
- 各HitはDialogue Blockを分け、次のHitへ進むタイミングでHP表示を更新する

例：
連鎖: 5回 で、敵が 3体 の場合（小数誤差適当）
e1に100%のダメージを与え（本体分のダメージ）、
e2に83%のダメージを与え（1回目）、
e3に66%のダメージを与え、
e2に50%のダメージを与え、
e1に33%のダメージを与え、
e2に16%のダメージを与える。

連鎖: 3回 で、敵が 1体 の場合
e1に100%のダメージを与え、
e1に75%、
e1に50%、
e1に25%。

##### [アドチェイン]
全ての連鎖Skillの追加連鎖回数が、値の整数部分だけ増える。
連鎖0のSkillにも適用され、1.0以上なら連鎖する。
効果時間・消費タイミングはなく、Battle終了まで増え続ける。
アドチェインの増加はBattle Logへ表示しない。

#### Passive

- 名前:コンボマスター
- Implementation: `Implemented`
- 効果:Battle中に実際に完了した最大追加連鎖回数に応じてDamageBonusが上昇する。

```text
DamageBonus増加量
= 最大追加連鎖回数 × DamageBonusPerChain

DamageBonusPerChain = 10（仮値）
```

- 本体Hitは追加連鎖回数へ含めない
- 以前の最大値以下の連鎖では補正を変更しない
- 最大値が更新された場合、過去分へ加算せず新しい最大値から補正を再計算する
- 補正はBattle終了時に破棄する
- `DamageBonusPerChain`はSOで調整可能にする

### [Pachimon名]4

- Status: `Implemented`
- Species ID:
- モチーフ:
- 狙い:

#### Fixed Skill

- 名前: 炎の障壁
- Implementation: `Implemented`
- BaseValue: `100`
- FireValueRatio: `100`
- 効果:
  - 自陣へ[生成物: 炎の障壁]を生成する

```text
生成Value
= BaseValue
  × AmplificationMultiplier(Fire × FireValueRatio / 100)
```

##### [生成物: 炎の障壁]

- ValueHpRatio: `100`
- ValueDurationRatio: `100`
- ValueBurnRatio: `20`
- DefenseSnapshotRatio: `50`

```text
追加HP
= floor(追加Value × ValueHpRatio / 100)

追加効果時間
= ceil(追加Value × ValueDurationRatio / 100)

火傷Value
= floor(現在Value × ValueBurnRatio / 100)
```

- 生成時に、生成者の8属性値とResistBonusをそれぞれ`DefenseSnapshotRatio%`でスナップショットする
- 味方が受ける攻撃の軽減前Damageを代わりに受ける
- 肩代わりしたDamageは、Damage属性に対応する障壁の属性値とResistBonusで軽減する
- True Damageは属性軽減を受けない
- 障壁による軽減後Damageを現在HPへ適用する
- 障壁の現在HPを超えた余剰Damageは、元の対象へ引き継ぐ
- 元の対象へ引き継いだ余剰Damageには、対象自身の属性値とResistBonusによる軽減を適用する
- 元の対象が保持するShieldは、引き継いだ余剰Damageを通常どおり吸収できる
- 攻撃を受けたとき、攻撃者へ[状態: 火傷]を付与する
- HPが0になるか、効果時間が0になると消滅する
- 同じ陣営へ再生成した場合、Value・現在HP・最大HP・残り時間を加算する
- 再生成時の防御Snapshotは、最新の生成者から取得した値で上書きする
- 防御Snapshotは生成後の生成者のStat変化によって変動しない

##### [状態：火傷]

- Value分のDamageBonusを減少する
- 付与Valueを対象のFireによって軽減する
- 同じ対象へ再付与した場合、Valueを加算する
- 次に使用するSkillの効果解決後、Skill選択時点で保持していた火傷Valueを消費する
- Skill選択から効果解決までは火傷を保持し、発生中のDamageBonusにも反映する
- Skill効果中に新しく付与された火傷Valueは消費せず、次に使用するSkillへ持ち越す
- `わるあがき`や対象不在で終わったSkillでも、効果解決後に消費する
- 発生中に使用者が戦闘不能となりSkillが中断された場合は消費しない

#### Passive

- 名前:追い打ち
- Implementation: `Implemented`
- 効果:火傷している対象へのダメージが30%増加する。
- 現在は`ApplyOutgoingModifiers = true`の属性Damageへ適用する
- `DamagePercent = 130`をSOで調整可能にする


### [Pachimon名]5

- Status: `Implemented`
- Species ID: `33`
- モチーフ:
- 狙い:

#### Fixed Skill

- 名前: ファイアアロー
- Implementation: `Implemented`
- 硬直: `100`
- CD: `250`
- MN: `100`
- 対象: 生存中でCurrentHPが最も低い敵
- 効果:
  - `100 × AmplificationMultiplier(Fire × 100%)`のFireダメージを与える
  - 対象を戦闘不能にした場合、MNを再度消費して再発動する
- 補足仕様:
  - CurrentHPが同値なら前方の敵を優先する
  - 再発動ごとに対象を再選択する
  - MN不足、対象なし、使用者の戦闘不能、または対象を戦闘不能にできなかった場合は終了する
  - CDと硬直は最初の使用時に一度だけ適用する
  - `BasePower / FireScalingPercent`はSOで調整する

#### Passive

- 名前:ファイアアーチャー
- Implementation: `Implemented`
- 効果:Skill Damageを与えた際、対象の減少HPと自身のFireに応じた追加Fire Damageを同じ対象へ与える。

```text
追加Fire DamageのBaseDamage
= 対象の減少HP
× MissingHpPercent / 100
× AmplificationMultiplier(Fire × FireScalingPercent)

MissingHpPercent = 5%
FireScalingPercent = 100%
```

- 減少HPは元のSkill Damage適用後の`MaxHP - CurrentHP`を参照する
- 元DamageがHPまたはShieldへ1以上適用され、対象が生存している場合に発動する
- 追加DamageにはDamageBonus、対象Fire、対象ResistBonusを適用する
- Fireは式へ反映済みのため、攻撃側Fire倍率を二重適用しない
- Passive・Status・Item起点のDamageからは発動しない
- 自身が生成した追加Fire Damageから再発動しない
- 追加Fire Damageには闇の炎など、Damage適用後に反応する別Passiveを適用できる
- `MissingHpPercent / FireScalingPercent`はSOで調整可能にする

### [Pachimon名]6

- Status: `Implemented`
- Species ID: `41`
- モチーフ:
- 狙い:

#### Fixed Skill

- 名前: 燃焼
- Implementation: `Implemented`
- 硬直: `100`
- CD: `300`
- MN: `100`
- 対象: 先頭の敵と自身
- 効果:
  - 先頭の敵と自身に、それぞれ`100 × AmplificationMultiplier(Fire × 100%)`のFireダメージを与える
  - 両者が生存し、MNを追加消費できる間は再発動する
- 補足仕様:
  - 1回の発動で敵へのDamage、自傷Damageの順に両方を解決する
  - 敵が戦闘不能になった発動でも自傷Damageは発生する
  - 自傷は自身を対象にした攻撃として扱う
  - 自傷にもDamageBonus、与ダメージPassive、被攻撃Passiveを適用する
  - 自傷は自身のFireとResistBonusによる軽減を受ける
  - 再発動してもCDと硬直は初回の一度だけ適用する
  - 敵と自身は共通の`BasePower / FireScalingPercent`をSOで調整する
  - Previewは現行どおり、全発動分をまとめた最終変化を表示する

#### Passive

- 名前:燃える男
- Implementation: `Implemented`
- 効果:Damageを受けるたびに、Battle中の自身のFireを20増加する。
- 属性Damage・True Damage・状態Damage・自傷を発生源に関係なく数える
- HP DamageとShield吸収Damageの合計が1以上なら1回発動する
- 1回のDamage解決でHPとShieldの両方が減っても発動は1回とする
- 0 Damageと、Damageによって戦闘不能になった場合は発動しない
- 増加分はBattle終了時に破棄する
- `FireIncreasePerDamage = 20`をSOで調整可能にする

### [Pachimon名]7

- Status: `Implemented`
- Species ID: `49`
- モチーフ:
- 狙い:

#### Fixed Skill

- 名前:温暖化
- Implementation: `Implemented`
- 硬直: `100`（仮値・SOで調整可能）
- CD: `300`（仮値・SOで調整可能）
- MN: `100`（仮値・SOで調整可能）
- `BaseValue: 400`
- `ValueFireRatio: 100%`
- 効果:Battle中の[気温]を次の値だけ恒久的に増加させる

```text
TemperatureGain
= max(1, floor(
    BaseValue
    + Fire × 補正後ValueFireRatio / 100
  ))
```

- Fire `0 / 100 / 200`では、気温0の場合に`400 / 500 / 600`増加する
- 現在の気温によるFire Ratio補正を`ValueFireRatio`にも適用する
- 正の気温では自己増幅し、負の気温では増加量が抑制される
- 例としてFire100・気温+500では、`ValueFireRatio 100% × 1.5 = 150%`となり、気温を550増加させる

##### [環境パラメーター：気温]

- 敵味方を含む全員へ効果する符号付き整数値
- 初期値は0で、Battle中は時間経過で減衰しない
- 正負の変更量を加算し、0のときはFieldへ表示しない
- 正の値を旧「晴れ」相当、負の値を寒冷相当として扱う

- `FireRatioScalingPercent: 10%`
- `AquaRatioScalingPercent: 20%`
- `IceRatioScalingPercent: 20%`
- `ColdFireRatioScalingPercent: 20%`
- `ColdIceRatioScalingPercent: 10%`

```text
FireRatioMultiplier
= AmplificationMultiplier(
    Temperature × FireRatioScalingPercent / 100
  )

AquaRatioMultiplier
= ReductionMultiplier(
    Temperature × AquaRatioScalingPercent / 100
  )

IceRatioMultiplier
= ReductionMultiplier(
    Temperature × IceRatioScalingPercent / 100
  )

ColdFireRatioMultiplier
= ReductionMultiplier(
    abs(Temperature) × ColdFireRatioScalingPercent / 100
  )

ColdIceRatioMultiplier
= AmplificationMultiplier(
    abs(Temperature) × ColdIceRatioScalingPercent / 100
  )

補正後AttributeRatio
= 基本AttributeRatio × AttributeRatioMultiplier
```

| Temperature | Fire Ratio | Aqua Ratio | Ice Ratio |
| ---: | ---: | ---: | ---: |
| 400 | 1.4倍 | 約0.556倍 | 約0.556倍 |
| 500 | 1.5倍 | 0.5倍 | 0.5倍 |
| 600 | 1.6倍 | 約0.455倍 | 約0.455倍 |

- Damageと、状態Value・生成物ValueなどSkill/Passive固有効果のAttribute Ratioへ適用する
- 防御側の属性値とResistBonusには適用しない
- 正の気温によるAqua/Ice Ratioは気温が増えるほど0へ近づくが、有限値では0にならない
- 気温が負の場合、Fire Ratioを減少させ、Ice Ratioを増加させる
- 気温が負の場合、Aqua Ratioは変化しない

#### Passive

- 名前:晴れ男
- Implementation: `Implemented`
- 効果:戦闘参加中（戦闘不能時を含まない）、気温が正のときにSpeedが30%上昇する。
- Speed倍率`130%`はSOで調整可能にする
- 気温が0以下の場合は補正しない
- 気温によるAttribute Ratio補正は攻撃・効果値の計算にのみ使用し、防御側の属性軽減には使用しない

## Ideas
