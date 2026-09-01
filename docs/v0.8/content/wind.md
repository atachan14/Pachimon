# Wind Content

## Pachimon

### トビチタ

- Status: `Implemented`
- Species ID: `15`
- モチーフ: 翼状の前脚を持つ飛行チーター
- 狙い: フライングアタックと助走を、跳躍中の高速シルエットで表現する

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

### フウカロン

- Status: `Implemented`
- Species ID: `23`
- モチーフ: 風に削られた砂岩と風の帯からなる獣
- 狙い: 風化とResistBonus差を、侵食された装甲として表現する

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

### ナギシカ

- Status: `Implemented`
- Species ID: `31`
- モチーフ: そよ風の角とたてがみを持つ鹿
- 狙い: 治癒の風による回復を、穏やかな支援役として表現する

#### Fixed Skill

- 名前:治癒の風
- Skill ID: `31`
- Implementation: `Implemented`
- 硬直: `100`
- CD: `300`
- MN: `100`
- 効果:[HP割合が最も低い味方]のHPを 100 * 風参照 回復する。
- HP割合が低い順で選び、同率なら前方を優先する

#### Passive

- 名前:
- Implementation: `Implemented`
- 仮表示名: `風004`（Passive ID `31`。正式名決定後にSOだけ変更する）
- 効果:味方の与える風ダメージが15%増加する。
- 所持者自身を味方に含める
- 所持者が生存している間だけ有効とする
- `ApplyOutgoingModifiers = true`の風Attribute Damageへ適用する
- 倍率`115%`は`TeamAttributeDamagePassiveAsset`から調整可能にする


### ナギマル

- Status: `Implemented`
- Species ID: `39`
- モチーフ: 風の膜をまとった丸い浮遊獣
- 狙い: セカンドウィンドのShieldと無風を、静かな防護膜として表現する

#### Fixed Skill

- 名前:セカンドウィンド
- Skill ID: `39`
- Implementation: `Implemented`
- 硬直：100
- MN:30
- CD:400
- 効果:
`BaseShieldValue 100 * AmplificationMultiplier(風 * WindShieldRatio 100%)`、効果時間200tickのシールドを獲得し、自身に200tickの[無風]を付与する。

[無風]
効果時間中、風が0になる。
- 初期値・Mods・Item・Passive・状態を含む最終Windを0にする

#### Passive

- 名前:風の加護
- Passive ID: `39`
- Implementation: `Implemented`
- 効果:自身がシールドを獲得するとき、他の味方にもvalue=20% , 効果時間=100%のシールドを付与する。
- 風の加護によって共有されたShieldでは、別の風の加護を再発動しない

### アラシープ

- Status: `Idea`
- Species ID: `47`
- モチーフ: 渦巻く嵐雲の毛を持つ羊
- 狙い: 暴風と天気の子を、複数の風が回る天候型シルエットで表現する

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

### カマツバメ

- Status: `Implemented`
- Species ID: `55`
- モチーフ: 三日月状の長い翼を持つツバメ
- 狙い: きりきり舞いと風乗りを、連続攻撃向きの高速飛行シルエットで表現する

#### Fixed Skill: きりきり舞い

- Implementation: `Implemented`
- 追加連鎖数: `2`
- 各HitでWindダメージと風化を付与する
- ダメージと風化の両方へ同じ連鎖減衰率を適用する

```text
Windダメージ = floor(100 × AmplificationMultiplier(Wind × 100%) × ChainRatio)
風化Value = floor(20 × AmplificationMultiplier(Wind × 100%) × ChainRatio)
```

- 使用後にきりきり舞い追加連鎖数を`1`獲得する。他の連鎖Skillには影響しない
- Base値・Ratio・連鎖数・追加連鎖数の獲得量は`CuttingDanceSkillAsset`から調整可能

#### Passive:風乗り

- Implementation: `Implemented`
- Passive ID: `55`
- 自身のSkillでWindダメージを1以上与えるたびにSpeedを増加する
- 仮値: 1 Hitにつき`20`
- Battle中恒久で、複数回発動時は加算する
- 増加量は`WindRiderPassiveAsset`から調整可能

### カザクジャ

- Status: `Implemented`
- Species ID: `63`
- モチーフ: 炎・水・風の三種の尾羽を持つ孔雀
- 狙い: 花鳥風月と風の魔術師を、複数属性が共存する扇状の尾羽で表現する

#### Fixed Skill: 花鳥風月

- Implementation: `Implemented`
- 先頭の生存敵を対象とする
- Fire・Aqua・Leaf・Windの4成分を同じ1 Hitとして順番に適用する
- 回避・肩代わりの判定は4成分で共有する

```text
Fireダメージ = floor(50 × AmplificationMultiplier(Fire × 100%))
Aquaダメージ = floor(50 × AmplificationMultiplier(Aqua × 100%))
Leafダメージ = floor(50 × AmplificationMultiplier(Leaf × 100%))
Windダメージ = floor(50 × AmplificationMultiplier(Wind × 100%))
```

- 各成分のBase値とRatioは`KachofugetsuSkillAsset`から調整可能

#### Passive: 風の魔術師

- Implementation: `Implemented`
- Passive ID: `63`
- 自身のSkillでWind以外の属性ダメージを1以上与えるたびにWindを`10`増加する
- 複合Skillでは、条件を満たした属性成分ごとに発動する
- Battle中恒久で、複数回発動時は加算する
- 増加量は`WindMagicianPassiveAsset`から調整可能

## Ideas
