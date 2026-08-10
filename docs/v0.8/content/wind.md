# Wind Content

## Pachimon

### [Pachimon名]2

- Status: `Implemented`
- Species ID: `15`
- モチーフ:
- 狙い:

#### Fixed Skill

- 名前:フライングアタック
- Skill ID: `15`
- Implementation: `Implemented`
- 発生：100
- 硬直: `100`
- CD: `300`
- MN: `100`
- 効果:
発生時、[飛行]になる。
発動時、[飛行]を解除して、先頭の敵に120*風参照ダメージ。

[飛行]
対象指定不可になる。
飛行中、風*20%のspeedを獲得する。
- 発生開始時に付与し、Skill発動直前に解除する

[対象指定不可]
諸々の対象から除外される。
例えば先頭だった場合、先頭を指定した攻撃は次のPahicmonを対象として発動される。

#### Passive

- 名前:助走
- Passive ID: `15`
- Implementation: `Implemented`
- 効果:発生*20%のダメージボーナスを得る。
- SOに記載された基本発生値を参照する
- 発生0のSkillでは発動しない

### [Pachimon名]3

- Status: `Implemented`
- Species ID: `23`
- モチーフ:
- 狙い:

#### Fixed Skill

- 名前:風化の風
- Skill ID: `23`
- Implementation: `Implemented`
- 硬直: `100`
- CD: `300`
- MN: `100`
- 効果:
[敵の全体] に Value=20*風参照の[風化]を与える。

[風化]
対象のResistBonusをValueだけ減少させる。
Valueはtickごとに減少し、0になると破棄される。
- 減衰量は1tickにつき1
- 再付与時は既存Valueへ加算する

#### Passive

- 名前:
- Implementation: `Implemented`
- 仮表示名: `風003`（Passive ID `23`。正式名決定後にSOだけ変更する）
- 効果:自身のResistBonusが対象のResistBonusより高い場合、その差に応じて与えるダメージが増加する。
- 仮式: `AmplificationMultiplier(max(0, 自身RB - 対象RB) × 30%)`
- `ApplyOutgoingModifiers = true`の属性Damageへ適用する
- True DamageとStatus Damageには適用しない
- 差分Ratio`30%`は`ResistAdvantageDamagePassiveAsset`から調整可能にする

### [Pachimon名]4

- Status: `Implemented`
- Species ID: `31`
- モチーフ:
- 狙い:

#### Fixed Skill

- 名前:治癒の風
- Skill ID: `31`
- Implementation: `Implemented`
- 硬直: `100`
- CD: `300`
- MN: `100`
- 効果:[最も体力が低い味方]のHPを 100 * 風参照 回復し、風（50 * 風参照）とSpeed（50 * 風参照）を増加させる。
- 最も体力が低い味方はCurrent HPが低い順、同値なら前方優先
- Wind / Speed増加の効果時間は200tick

#### Passive

- 名前:
- Implementation: `Implemented`
- 仮表示名: `風004`（Passive ID `31`。正式名決定後にSOだけ変更する）
- 効果:味方の与える風ダメージが15%増加する。
- 所持者自身を味方に含める
- 所持者が生存している間だけ有効とする
- `ApplyOutgoingModifiers = true`の風Attribute Damageへ適用する
- 倍率`115%`は`TeamAttributeDamagePassiveAsset`から調整可能にする


### [Pachimon名]5

- Status: `Implemented`
- Species ID: `39`
- モチーフ:
- 狙い:

#### Fixed Skill

- 名前:セカンドウィンド
- Skill ID: `39`
- Implementation: `Implemented`
- 硬直：100
- MN:100
- CD:400
- 効果:
風 * 200% , 効果時間=200tickのシールドを獲得し、自身に200tickの[無風]を付与する。

[無風]
効果時間中、風が0になる。
- 初期値・Mods・Item・Passive・状態を含む最終Windを0にする

#### Passive

- 名前:風の加護
- Passive ID: `39`
- Implementation: `Implemented`
- 効果:自身がシールドを獲得するとき、他の味方にもvalue=20% , 効果時間=100%のシールドを付与する。
- 風の加護によって共有されたShieldでは、別の風の加護を再発動しない

### [Pachimon名]6

- Status: `Idea`
- Species ID:
- モチーフ:
- 狙い:

#### Fixed Skill

- 名前:暴風
- Status: `Implemented`
- 効果:
[天気：暴風] を フィールドに付与する。

##### [天気：暴風]
風Ratioが増加する。
Speedが風参照で増加（風*20%の）。
[Weather：Rain]の効果が増加する。気温が負で雪として扱われている場合も同様に増加する。

- 生成Value: `BaseValue + Wind × WindValueRatio / 100`
- 仮値: `BaseValue = 400`, `WindValueRatio = 100%`
- 風Ratio倍率: `AmplificationMultiplier(暴風Value × 10%)`
- Speed派生加算: `Wind × 20%`
- 雨・雪の効果倍率: `AmplificationMultiplier(暴風Value × 10%)`
- 雨・雪の効果倍率は、雨のAqua/Fire Ratio、雨による漏電加算、雪による冷気付与、雨男Passiveへ適用する
- 各係数は`WindStormSkillAsset`または`WindWeatherAsset`から調整可能にする


#### Passive

- 名前:天気の子
- Status: `Implemented`
- 効果:発動中の天気の種類数*20のダメージボーナスを獲得する。
- 気温は値が0でない場合に1種類として数える
- 雨と雪は同じRain Weatherの表示切替なので、どちらも1種類として数える
- 暴風・雷など、同時に存在する別のWeatherをそれぞれ1種類として数える
- 1種類あたりのDamageBonusは`WeatherChildPassiveAsset`から調整可能にする（仮値`20`）

## Ideas
