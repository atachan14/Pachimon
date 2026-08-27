# Poison Content

## Pachimon

### パチムシ
#### 既存共通Skill：どくばり

- Status: `Implemented`
- 既存の硬直・CD・MN・Poison Damageは当面据え置く
- 先頭の敵へ既存のPoison Damageを与える
- Damage解決後、生存している対象へ毒素を付与する
- 付与時は`[対象名]に[Value]の毒素を与えた！`をDialogへ表示する

```text
毒素Value
= BaseToxinValue × AmplificationMultiplier(Poison × PoisonScalingPercent)

BaseToxinValue = 100
PoisonScalingPercent = 100%
```

`BaseToxinValue / PoisonScalingPercent`はSOで調整可能にする。


### シンケイド

- Status: `Implemented`
- Species ID: `13`
- モチーフ: 神経回路が発光するムカデ型の毒獣
- 狙い: PoisonとElectricからStunと毒素を同時に与える妨害型

#### Fixed Skill

- 名前:神経毒
- 効果:最後尾の敵にbase * amp電気 の[Stun]とbase * amp毒 の 毒素 を与える。

```text
StunTicks
= 50 × AmplificationMultiplier(Electric × 100%)

毒素Value
= 100 × AmplificationMultiplier(Poison × 100%)
```

- `BaseElectricStunTicks / ElectricStunScalingPercent`をSOで調整可能にする
- `BaseToxinValue / ToxinScalingPercent`をSOで調整可能にする
- 計算結果は最後に切り捨てる
- Stunと毒素はSkill発動時点の最後尾へまとめて付与する

#### Passive

- Status: `Implemented`
- 名前:毒素適応（仮）
- 効果:自身が毒素を付与するたびに、Battle中の自身のPoisonを10%増加する。
- Skill・フィールド生成物など付与手段を問わず、毒素を実際に付与した回数だけ発動する
- 複数の対象へ同時に付与した場合は、付与に成功した対象数だけ発動する
- 増加率は発動回数分を加算してから乗算する（2回なら1.20倍）
- Battle終了時に増加分を破棄する
- 1回ごとの増加率は`ToxinGrowthPassiveAsset`で調整可能にする

##### [状態：毒素 / Toxin]

- 効果時間を持たず、Value自体を寿命として使用する
- 付与Valueを対象のPoisonによって軽減する
- 付与履歴には軽減後Valueを記録する
- `DamageWork`を小数で保持する
- 毎tick、tick開始時の現在Valueの1%をDamage成分とする
- 毎tickのDamage成分へ対象のPoisonと`ResistBonus`による軽減を適用してから`DamageWork`へ加算する
- `floor(DamageWork)`をHPへ適用し、適用分をWorkから減算する
- 毒素Damageには最低1 Damage保証を適用しない
- 付与時に毒素Valueへ反映済みの付与者Poisonは、毎tickのDamage計算で再適用しない
- 毎tickのDamageへ付与者の`DamageBonus`を適用しない
- 毒素Damageは`IsAttack = false`とする
- Damage Originは`Status / Toxin`とし、発生源Unitを持たない
- 毎tick、現在Valueを`DecayPerTick`だけ減少させる（初期値1）
- 同じtickではDamageを先、Value減少を後に処理する
- Valueが0になった場合は解除する
- 同じ毒素を再付与した場合、既存Valueへ新しいValueを加算する
- 再付与時も蓄積済み`DamageWork`を維持する
- 付与時に即時Damageは発生させない

```text
TickAmount
= CurrentValue × 1%

UnroundedDamage
= TickAmount
× DefenseMultiplier(対象Poison)
× DefenseMultiplier(対象ResistBonus)

DamageWork += UnroundedDamage
CurrentValue -= min(CurrentValue, DecayPerTick)
```

- Valueが100未満でも小数Workを持ち越すため、蓄積値が1以上になればDamageが発生する
- Valueの増加は次のtickからDamage量へ反映する
- 毎tickの毒素DamageはDialogへ流さない
- 前回の表示から次のTurnまでに受けた毒素DamageをUnitごとに合算する
- 次のTurn表示前に、合算DamageをHPGauge上へ紫色で一時表示してからHPを減少させる
- 毒素Damageで戦闘不能になる場合はTimelineをその時点で止め、Gauge反映と戦闘不能表示を先に行う
- 毒素専用の戦闘不能Dialogは追加せず、共通の戦闘不能表示を使用する

#### 付与履歴

- 毒素Statusは現在Valueとは別に、各付与時の`付与者Instance ID / 表示名 / 追加Value`を履歴として保持する
- 付与者の`BattleUnitState`やPachimon Objectそのものは保持しない
- 履歴は将来の状態詳細Overlayで`付与者名 / 付与Value`を表示するために使用する
- 付与履歴は毒素Damageの発生源・Damage計算・Passive判定には使用しない
- 再付与時に「最新の付与者」を毒素Damageの発生源として上書きしない
- 毒素が解除された時点で付与履歴も破棄する




### スモッグン

- Status: `Implemented`
- Species ID: `21`
- モチーフ: 背中の実験槽でスモッグを作る研究獣
- 狙い: 敵陣へ継続的に毒素を撒き、生成物を強化する展開型

#### Fixed Skill

- 名前:スモッグ
- Status: `Implemented`
- 効果:
敵陣フィールドにValue=毒依存の[生成物：スモッグ]を生成する。

[生成物:スモッグ]

```text
初期Value
= 300 × AmplificationMultiplier(Poison × 100%)

TickAmount
= CurrentValue × 1%

ApplicationWork += TickAmount
```

- 毎tick、敵陣の生存Pachimon全員へ`floor(ApplicationWork)`の毒素を付与する
- 付与分を`ApplicationWork`から減算する
- 毎tick、スモッグValueを固定で1減少させ、Valueが0になると消滅する
- 同じ敵陣へ再生成した場合は既存Valueへ加算し、`ApplicationWork`を維持する
- 再生成後の付与者表示には最新の生成者を使用する
- スモッグから付与された毒素は次のtickからDamage・Value減衰を開始する
- tickごとの毒素付与Logは表示しない
- `BaseFieldValue / PoisonScalingPercent`はSOで調整可能とする

#### Passive

- 名前:科学工作
- Status: `Implemented`
- 効果:自身が生成物を生成するとき、Valueを毒依存で増加する。

```text
適用後Value
= floor(
    生成予定Value
    × AmplificationMultiplier(Poison × 30%)
  )
```

- 初回生成と同じ生成物への再生成の両方に適用する
- 生成物固有のValue計算後、フィールドへ追加する直前に適用する
- `PoisonScalingPercent = 30`をSOで調整可能にする

### ドクハコビ

- Status: `Implemented`
- Species ID: `29`
- モチーフ: 天秤棒で二つの毒壺を運ぶ俊足獣
- 狙い: 敵同士で毒素を移し替え、PoisonからSpeedを得る操作型

#### Fixed Skill

- 名前:毒渡し
- 効果:
[一番多く毒が付与されている敵]の毒を50%取り除く。
[一番少なく毒が付与されている敵]に次の毒素を与える。

```text
付与Value
= floor((除去Value + 150 × AmplificationMultiplier(Poison)) × 200%)
```

- 敵全員の毒素が0の場合は除去・倍化を行わず、先頭へ`150 × AmplificationMultiplier(Poison)`の毒素を与える

補足：
1. 敵が1人だった場合は、同じ対象から除去後、同じ対象へ上記の付与Valueを与える。
2. [一番少なく毒が付与されている敵]を選択するタイミングは、毒を取り除く前。（最初に[一番多く毒が付与されている敵]と[一番少なく毒が付与されている敵]を確定する。）
3. 最大対象はValue降順、同値なら前方優先で決定する。
4. 最小対象は最大対象以外からValue昇順、同値なら前方優先で決定する。
5. 生存している敵が1体の場合のみ、最大対象と最小対象は同じ対象になる。
6. 毒素を持たない敵はValue 0として扱う。
7. 除去量と付与量はそれぞれ切り捨てる。
8. `RemovalPercent = 50 / BaseToxinValue = 150 / PoisonScalingPercent = 100 / ApplicationPercent = 200`をSOで調整可能にする。

#### Passive

- 名前:毒走り
- Status: `Implemented`
- 効果:毒に応じてSpeedが上昇する。

```text
Speed加算値
= max(0, Poison × 30%)
```

- 非Battle中・Battle中ともに同じ派生Stat補正を適用する
- 全ての非派生加算補正後のPoisonを参照する
- `Percent = 30 / MinimumContribution = 0`をSOで調整可能にする

### バクドクガ

- Status: `Implemented`
- Species ID: `37`
- モチーフ: 火種の腹と毒袋を持つ爆発蛾
- 狙い: 蓄積した毒素を炎とPoisonで爆破する全体攻撃型

#### Fixed Skill

- 名前:毒爆破
- 効果:
敵全員の毒素を全て消費し、毒素を持っていた各対象を起点に本体Poison Damageと全体Fire Damageを発生させる。

```text
本体軽減前Poison Damage
= ConsumedToxin × 100% × AmplificationMultiplier(Poison × 100%)

AOE軽減前Fire Damage
= 本体軽減前Poison Damage × 5% × AmplificationMultiplier(Fire × 100%)
```

- 全対象の毒素をDamage解決前に全消費する
- 毒素0の対象は本体DamageもAOEの発生源にもならない
- 各発生源自身は、本体Poison Damage 1回と全発生源由来のAOE Fire Damageを受ける
- 敵3体が毒素を持つ場合、各対象は本体Poison Damageを1回、AOE Fire Damageを3回受ける
- 各Damageへ`DamageBonus / 対象属性 / 対象ResistBonus / 攻撃時Passive`を適用する
- 攻撃側Poison / Fireは式へ反映済みのため、属性倍率を二重適用しない
- `ToxinConversionPercent / PoisonScalingPercent / AoeFirePercent / FireScalingPercent`はSOで調整可能にする

#### Passive

- 名前:熱毒
- Status: `Implemented`
- 効果:炎依存で毒が増加。

```text
Poison加算値
= max(0, Fire × 100%)
```

- 非Battle中・Battle中ともに同じ派生Stat補正を適用する
- 全ての非派生加算補正後のFireを参照する
- `Percent = 100 / MinimumContribution = 0`をSOで調整可能にする

### ドクナイト

- Status: `Implemented`
- Species ID: `45`
- モチーフ: 毒液の甲羅と注射槍を備えた重装騎士獣
- 狙い: 自身を解毒しながらShieldを張り、味方にも防御を共有する支援型

#### Fixed Skill

- 名前:ポイズンシールド
- 効果:自身に毒依存のシールドを付与する。
また、自身に付与されている毒素を毒依存の割合で減少させる（回復効果）。
- Shield効果時間: `200tick`

```text
ShieldValue
= 100 × AmplificationMultiplier(Poison × 100%)

毒素減少率
= 30% × AmplificationMultiplier(Poison × 100%)

毒素減少Value
= floor(CurrentToxin × 毒素減少率)
```

- Shieldと毒素減少Valueはそれぞれ最後に切り捨てる
- 毒素減少Valueが現在Valueを超える場合は全て取り除く
- 毒素を持たない場合もShieldは通常どおり付与する
- 付与するShieldは無期限の独立したShield Instanceとする
- `BaseShieldValue / ShieldPoisonScalingPercent / BaseToxinReductionPercent / ReductionPoisonScalingPercent`はSOで調整可能にする

#### Passive

- 名前:毒の騎士
- Status: `Implemented`
- 効果:自身が受けるShield効果とHP回復効果を、他の味方にもPoison依存の割合で与える。

```text
共有率
= BaseSharePercent
× AmplificationMultiplier(Poison × PoisonScalingPercent)

共有Value
= floor(自身への実適用Value × 共有率 / 100)

BaseSharePercent = 30%
PoisonScalingPercent = 100%
```

- 同じSideの生存中かつ自身以外のPachimon全員へ、それぞれ同じ共有Valueを適用する
- Shieldは自身へ付与されたValueを参照し、残りtickがある場合は共有先にも引き継ぐ
- HP回復はオーバーヒールを除いた、自身への実回復量を参照する
- 共有Valueは最後に切り捨て、0の場合は適用しない
- 共有によって発生したShield・回復効果から、別の`毒の騎士`を再発動させない
- Battle中のItemによるHP回復も発動条件に含む
- `BaseSharePercent / PoisonScalingPercent`はSOで調整可能にする

### キリマジョ

- Status: `Implemented`
- 実装ID: Skill / Passive `53`
- モチーフ: 薬雫を吊るした毒霧の浮遊魔術獣

#### Fixed Skill: 毒の霧
自陣に[Aqua参照の効果時間、Poison参照の初期Value、Wind参照の最小Value]を持つ毒の霧を生成する。

[毒の霧]
味方が現在Value以下の軽減前ダメージを受けるとき、回避する。

- 効果時間は`floor(75 * AmplificationMultiplier(Aqua * 100%))`、最低`1tick`
- 初期Valueは`floor(100 * AmplificationMultiplier(Poison * 100%))`
- 最小Valueは`floor(20 * AmplificationMultiplier(Wind * 100%))`
- 最小Valueが初期Valueを超える場合は、初期Valueを上限とする
- 効果時間をかけて、初期Valueから最小Valueまで直線的に減衰する
- 全属性値100の場合は`効果時間150tick / 初期Value200 / 最小Value40`
- 属性・確定ダメージを問わず、敵PachimonのSkill攻撃をShield判定前に回避する
- Self / Status / Field由来のダメージは回避しない
- 効果時間中に再生成した場合は、現在Value・残り効果時間・最小Valueへ新しい各値を加算する
- 統合後は、加算後の現在Valueから最小Valueまで、加算後の残り時間をかけて減衰し直す

#### Passive: 毒の魔術師
自身のskillで毒以外のダメージを与えるたびに、自身の毒を20増加する。

- 7属性のSkillダメージが実際にHPまたはShieldへ1以上適用されたHitごとに発動する
- Poison / True / Status / Field / Selfダメージでは発動しない
- 全体攻撃では、条件を満たした対象数だけ発動する

### サキドク

- Status: `Implemented`
- 実装ID: Skill / Passive `61`
- モチーフ: 葉刃を構えて初撃を狙う伏撃獣

#### Fixed Skill: ファーストタッチ
先頭の敵に75 * 毒参照の毒ダメージを与える。
対象のHPが100%未満だった場合、50 * 毒参照の毒素を与える。
対象のHPが100%だった場合、通常ダメージの代わりに300 * 毒参照の強化ダメージと150 * 毒参照の毒素を与える。

- HP100%判定とPoisonはSkill解決開始時の値を使用する
- HP100%時の強化ダメージは通常ダメージを置換し、1Hitだけ行う
- 強化Hitが回避された場合は毒素も付与しない

#### Passive: ラストタッチ
自身のSkillでダメージを与えたとき、対象のHPが毒 * 4%以下なら戦闘不能にする（残りHP分の確定ダメージを追加で与える）。

- 例：Poison`100`なら最大HPの`4%`、Poison`250`なら`10%`が閾値
- HP判定は各Skillダメージ適用後に行う
- 処刑ダメージはShieldを消費せず、対象の残りHPへ直接適用する

## Ideas
