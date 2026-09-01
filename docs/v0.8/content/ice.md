# Ice Content

## Pachimon

### カガミン

- Status: `Implemented`
- Species ID: `14`
- モチーフ: 氷の鏡盾を胸と背に持つアザラシ
- 狙い: 氷の盾と氷Damage軽減を、丸い守備型シルエットで表現する

#### Fixed Skill

- 名前:氷の盾
- Implementation: `Implemented`
- 効果:先頭の味方にシールドを付与
- 対象は先頭の生存味方とする
- 仮式: `BaseShield 100 × AmplificationMultiplier(Ice × 100%)`
- Shieldは時間経過で消滅せず、Damageで消費されるまで残る
- 硬直`100`、CD`300`、MN`40`
- BaseShieldとIce Ratioは`IceShieldSkillAsset`から調整可能にする

#### Passive

- 名前:
- Implementation: `Implemented`
- 仮表示名: `氷002`（Passive ID `14`。正式名決定後にSOだけ変更する）
- 効果:受ける氷ダメージが減少
- 仮倍率: `85%`（15%軽減）
- 攻撃・Passive・Statusを問わず、氷Attribute Damageへ適用する
- True Damageと他属性Damageには適用しない
- 倍率は`IncomingAttributeDamagePassiveAsset`から調整可能にする

### ツララン

- Status: `Implemented`
- Species ID: `22`
- モチーフ: 長い氷晶の耳を持つウサギ
- 狙い: アイスシャードとSlow参照を、鋭く素早い氷晶獣として表現する

#### Fixed Skill

- 名前:アイスシャード
- Implementation: `Implemented`
- 効果:
先頭の敵にダメージと[冷気](./statuses/slow.md#冷気)を付与する。
先頭以外の敵にダメージと[冷気](./statuses/slow.md#冷気)を付与
- 先頭は先頭の生存敵とする
- 先頭Damage: `BaseDamage 100 × AmplificationMultiplier(Ice × 100%)`
- 先頭冷気: `BaseChill 75 × AmplificationMultiplier(Ice × 100%)`
- 後続Damage: `BaseDamage 50 × AmplificationMultiplier(Ice × 100%)`
- 後続冷気: `BaseChill 50 × AmplificationMultiplier(Ice × 100%)`
- 硬直`100`、CD`300`、MN`150`
- 各Base値とIce Ratioは`IceShardSkillAsset`から個別に調整可能にする
- 攻撃で実際に付与した冷気ValueをBattle Logへ表示する

#### Passive

- 名前:
- Implementation: `Implemented`
- 仮表示名: `氷003`（Passive ID `22`。正式名決定後にSOだけ変更する）
- 効果:対象に付与されているSlowに応じて、自身の与えるダメージが増加。
- 参照Slowは麻痺・冷気など`BattleStatusCategory.Slow`のValue合計とする
- 仮式: `AmplificationMultiplier(対象のSlow合計 × 30%)`
- `ApplyOutgoingModifiers = true`の属性Damageへ適用する
- True DamageとStatus Damageには適用しない
- Slow Ratio`30%`は`TargetSlowDamagePassiveAsset`から調整可能にする


### サムゾウ

- Status: `Implemented`
- Species ID: `30`
- モチーフ: 雪と凍土を背負うマンモス
- 狙い: 寒冷化と氷の大地を、重量感のある地形型シルエットで表現する

#### Fixed Skill

- 名前:寒冷化
- Implementation: `Implemented`
- 硬直: `100`
- CD: `300`
- MN: `100`
- 効果:Iceに応じてBattle中の気温を恒久的に低下させる。

```text
気温低下Value
= floor(BaseValue × AmplificationMultiplier(Ice × 解決済IceRatio / 100))
```

- 仮値は`BaseValue = 20`、`IceValueRatio = 100%`
- 負の気温によるIce Ratio増加を受けるため、寒冷下で自己増幅する

##### [環境：気温 / 雪]

- 寒冷化は気温を恒久的に減少させる
- 雪は独立したWeatherではなく、`Rain > 0 && Temperature < 0`で成立する
- 気温が負の場合、Fire Ratioが低下しIce Ratioが増加する
- 雪の間、炎以外の非Status Damageを受けるたびに冷気を付与する
- True Damageは冷気付与の対象に含める
- Status Damageは冷気付与の対象外とする
- 冷気Valueは`abs(Temperature)`に応じて増加する

```text
雪による冷気Value
= floor(20 × AmplificationMultiplier(abs(Temperature) × 100%))
```

- 算出後、対象のIceによる冷気Value軽減を適用する
- BaseValueとTemperature Ratioは`RainWeatherAsset`で調整可能

#### Passive

- 名前:氷の大地
- Implementation: `Implemented`
- Passive ID: `30`
- 効果:自身が生存中、全体フィールドにValue=自身の氷の氷の大地を生成する。
氷の地のValueは自身の氷に応じて変動する。

氷の大地
存在中、全Pachimonの冷気Value減衰を遅くする。

```text
冷気Value減衰量 / tick
= 1 / (1 + 氷の大地Value / DurationDoubleValue)

冷気の実質持続倍率
= 1 + 氷の大地Value / DurationDoubleValue
```

- 仮値は`DurationDoubleValue = 500`
- 氷の大地Value 400で約1.8倍、Value 500で2倍、Value 600で2.2倍持続する
- 小数の減衰量は冷気Instanceごとに蓄積し、整数になった分だけ減算する
- 複数の所持者がいる場合も氷の大地は1つだけ表示し、各所持者のValueを加算する
- 所持者が戦闘不能になると、その所持者が加算していたValueだけを取り除く
- 全所持者が戦闘不能になると氷の大地は消滅する
- 各値は`FrozenGroundFieldEffectAsset`から調整可能にする

##### 凍結
Stunとしても扱う。Stunと同等の効果だが、炎属性ダメージを受けたとき、受けた炎ダメージ/10だけ減少する。
付与時、対象の氷によって軽減される。
- 凍結中に凍結を追加した場合はValueを加算する
- 炎Damageによる減少では、Shield適用後にHPへ実際に適用されたDamageを参照する

### ツラカマ

- Status: `Idea`
- Species ID: `38`
- モチーフ: 両腕が氷の刃になったカマキリ
- 狙い: 氷の刃と氷Damageによる成長を、細身の追撃役として表現する

#### Fixed Skill

- 名前:氷の刃
- Implementation: `Implemented`
- 効果:Ice依存の効果時間を持つ[氷の刃]を自陣に生成する。

```text
効果時間
= BaseDuration 200
+ ScalingDuration 100 × AmplificationMultiplier(Ice × IceDurationRatio 100%)
```

- Ice 0で300tick、Ice 100で400tick、Ice 200で500tick
- 再生成時は残り時間を加算する
- 硬直`100`、CD`300`、MN`100`
- 各値は`IceBladeSkillAsset`から調整可能にする

[氷の刃]
敵Pachimonが冷気を受けるとき、次の氷ダメージを同じ対象に与える。

```text
軽減前Ice Damage
= 冷気の軽減前Value × 50% × AmplificationMultiplier(生成者Ice)
```

- 対象のIceによる冷気軽減前Valueを参照する
- 氷の刃Damageには生成者のIceを式で1回だけ適用し、DamageBonus・与Damage補正は再適用しない
- 対象のIce・ResistBonusによるDamage軽減は適用する
- 氷の刃Damageは攻撃扱いにしない
- 雪による冷気の追加付与対象にはしない
- 追撃時はDamage表示の直前に`氷の刃の攻撃！`をBattle Logへ表示する
- `DamagePercent = 50`は`IceBladeFieldEffectAsset`から調整可能にする

#### Passive

- 名前:氷の力
- Implementation: `Implemented`
- 仮表示名: `氷005`（Passive ID `38`。正式名決定後にSOだけ変更する）
- 効果:氷ダメージが発生するたびに、自身の氷を10増加させる。
- 攻撃・状態を問わず、HPまたはShieldへ1以上のIce Damageが入るたびに発動する
- `DamageOverTime`タグを持つ継続Damageでは発動しない
- 自身以外が発生させたIce Damageでも発動する
- 所持者が生存している間だけ発動する
- 1回あたりの増加値は`IceGrowthOnDamagePassiveAsset`から調整可能にする

### フロマジョ

- Status: `Idea`
- Species ID: `46`
- モチーフ: 氷のフードを被った浮遊する魔女型精霊
- 狙い: フローズンブレイクと氷の魔女を、神秘的な術師シルエットで表現する

#### Fixed Skill

- 名前:フローズンブレイク
- Implementation: `Implemented`
- 効果:

自身のHPが5割以上のとき
- 硬直：200
- 先頭の敵に凍結（Ice 0で70tick、Ice 100で110tick、Ice 200で150tick）とIce Damageを与える
- 仮Damageは`BaseIceDamage 100`とし、Iceを100%参照する

自身のHPが5割未満のとき
- 硬直：1
- 自身に[フローズンブレイク（セルフ）]を付与する

[フローズンブレイク（セルフ）]
- 効果時間：`BaseDuration 70 + Ice × DurationIceRatio 40%`
- Ice 0で70tick、Ice 100で110tick、Ice 200で150tick
- 効果時間中、自身をStunさせ、対象指定不可にする
- 発生・硬直・CooldownはStunにより停止する
- 毎tick、`BaseHealPerTick 1 × AmplificationMultiplier(Ice × HealIceRatio 50%)`だけHPを回復する
- 小数回復は内部で蓄積し、整数部分が生じたtickにHPへ反映する
- ActionGaugeは通常の硬直表示より優先して、青系の経過・残り表示と`対象外 n`を表示する
- 効果終了後、停止していた通常のActionGauge表示へ戻る
- 各値は`FrozenBreakSkillAsset`から調整可能にする

#### Passive

- 名前:氷の魔女
- Implementation: `Implemented`
- Passive ID: `46`
- 効果:自身が生存中、敵のPachimonが戦闘不能になるたびに、
敵の残りのPachimonに氷ダメージを分散して与える。

```text
分散前Damage
= BaseIceDamage 200
  × AmplificationMultiplier(Ice × IceDamageRatio 100%)
```

- 分散前Damageを、効果解決時点で生存している残りの敵の数で均等に割る
- 分散後、各対象のIce・ResistBonusで個別に軽減する
- DamageBonus・与Damage補正は重ねて適用しない
- Passive Damageであり、攻撃扱いにはしない
- このDamageで別の敵が戦闘不能になった場合も、改めて氷の魔女を発動する
- BaseIceDamageとIceDamageRatioは`IceWitchPassiveAsset`から調整可能にする

### ヒョウガメ

- Status: `Implemented`
- Species ID: `54`
- モチーフ: 氷河の甲羅を持つ大型のカメ
- 狙い: 氷の礫と氷の鎧を、明確な重装甲シルエットで表現する

#### Fixed Skill

- 名前: 氷の礫
- Implementation: `Implemented`
- 対象: 先頭の生存敵
- 1つのHitとしてIceダメージと冷気を適用する

```text
Iceダメージ = floor(70 × AmplificationMultiplier(Ice))
冷気Value = floor(35 × AmplificationMultiplier(Ice × IceRatio 100%))
Shield Value = floor(70 × AmplificationMultiplier(Ice × IceRatio 100%))
Shield効果時間 = 100tick
```

- Damage、冷気、Shieldの各Base値、効果Value用Ice Ratio、Shield効果時間は`IcePebbleSkillAsset`から調整可能

#### Passive

- 名前:氷の鎧
- Implementation: `Implemented`
- Passive ID: `54`
- 効果:
自身に付与されるシールドのvalueと効果時間が氷*20%増加。

```text
Shield補正倍率 = AmplificationMultiplier(Ice × IceScalingPercent 20%)
```

- 永続ShieldにはValue補正のみを適用する
- 補正値は`IceArmorPassiveAsset`から調整可能

### ユキフクロ

- Status: `Implemented`
- Species ID: `62`
- モチーフ: 矢尻型の翼と氷晶の尾を持つ雪フクロウ
- 狙い: フロストアローと冷気拡散を、遠距離攻撃型の翼形状で表現する

#### Fixed Skill

- 名前: フロストアロー
- Implementation: `Implemented`
- 効果:
最も体力が低い敵に100 * 氷参照ダメージと30 * 氷参照の冷気を与える。
このskillで敵を戦闘不能にした場合、消費したMNが還元され、このスキルのCDも回復する。

- 「最も体力が低い」は現在HPの実数で判定する
- 同値の場合は前方の敵を優先する
- 1つのHitとしてIceダメージと冷気を適用する

```text
Iceダメージ = floor(100 × AmplificationMultiplier(Ice))
冷気Value = floor(30 × AmplificationMultiplier(Ice × IceRatio 100%))
```

- Damageと冷気のBase値、冷気Value用Ice Ratioは`FrostArrowSkillAsset`から調整可能

#### Passive

- 名前: 冷気拡散（仮）
- Implementation: `Implemented`
- Passive ID: `62`
- 効果:
自身のSkillで敵を戦闘不能にした場合、対象に付与されていた冷気の150%を残りの敵に付与する（分散せずに付与）。

- 撃破ダメージが入る直前の冷気Valueを参照する
- 残りの生存敵それぞれへ同じValueを付与する
- 端数は最後に切り捨てる
- 付与率は`ChillSpreadPassiveAsset`から調整可能


## 既存共通Skill

### 冷たい手

- Implementation: `Implemented`
- 対象: 先頭の敵
- 既存のIceダメージに加えて、対象へ[冷気](./statuses/slow.md#冷気)を付与する

```text
冷気Value
= floor(75 × AmplificationMultiplier(Ice))
```

- 再付与と時間進行は[Slow共通仕様](./statuses/slow.md#共通仕様)に従う
- 攻撃で実際に付与した冷気ValueをBattle Logへ表示する

## Ideas
