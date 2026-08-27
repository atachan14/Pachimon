# Dragon Content

## 共通仮実装方針

- Damageは原則`BaseDamage × AmplificationMultiplier(Dragon)`を使用する
- 未記載の発生・硬直・CD・MNと各Valueは、Skill / Passive SOから調整可能な仮値で実装する
- 仮値は動作確認用であり、コンテンツ実装後にまとめて調整する

## Pachimon

### ゴングル

- Status: `Implemented`
- 仮実装: Skill ID `16` / Passive ID `16`
- Species ID: `16`
- モチーフ: 丸い拳状の前脚を持つ格闘竜
- 狙い: ドラゴンジャブとドラゴンボクサーの連打型を視覚化する

#### Fixed Skill

- 名前:ドラゴンジャブ
- 効果:
先頭の敵に竜属性ダメージを与え、[ワン・ツー]を獲得する。

##### [ワン・ツー]
次に使用するSkillの発生と硬直を軽減する。

- Skill選択時に保持Valueを記録し、そのSkillの発生・硬直へ適用する
- Skill効果解決後に、選択時に記録したValueだけを消費する
- 発生中に追加されたValueは次のSkillへ持ち越す
- 対象不在で終わったSkillでも消費する
- 発生中に戦闘不能となりSkill自体が中断された場合は消費しない
- 仮値: Damage Base `100` / ワン・ツーValue `30`
- ワン・ツーの発生・硬直倍率は`ReductionMultiplier(Value)`とする

#### Passive

- 名前:ドラゴンボクサー
- 効果:竜属性ダメージを与えるたびに10スタックを獲得する。
スタック毎に竜属性ダメージに+1%のボーナスを得る。
竜属性以外のダメージを与えるとスタックを半分失う。

- 仮値: 竜Damage成立ごとに`10`Stack、1Stackごとに竜Damage`+1%`
- Stack半減は切り捨てる
- `AttributeDamageAppliedEvent`で成立した追加Damageも増減対象とする
- True Damage / Status Damageは増減対象外とする

### ヒラリュウ

- Status: `Implemented`
- 仮実装: Skill ID `24` / Passive ID `24`
- Species ID: `24`
- モチーフ: リボン状の翼膜を持つ細身の飛竜
- 狙い: 回避とSpeed上昇を軽快なシルエットで表す

#### Fixed Skill

- 名前:ドラゴンフットワーク
- 効果:
80 * 竜参照tickの間、次に受ける攻撃と、その攻撃に付随する状態付与を回避する。（状態：フットワーク）

##### [回避]
攻撃を無効化する。

- 仮値: 硬直 `80` / CD `300` / MN `80`
- `IsAttack = true`のAttribute Damage / True Damageを対象とする
- Damageを伴わない敵からの状態付与も攻撃として回避する
- Damageを回避した場合、そのDamageに付随する状態付与も無効化する
- 回避成立時に[フットワーク]を消費し、`AttackEvadedEvent`を発行する
- Status Damageなどの攻撃扱いでないDamageでは消費しない

#### Passive

- 名前:スイートサイエンス
- 効果:回避に成功するたびにSpeedが上昇する。

- 仮値: 回避成立ごとにSpeed `+20`
- 上昇量はBattle中恒久かつ加算する

### マイドラ

- Status: `Implemented`
- 仮実装: Skill ID `32` / Passive ID `32`
- Species ID: `32`
- モチーフ: 長い翼膜をなびかせる舞踏竜
- 狙い: 龍の舞と龍の骨格によるDragon・Speed相互強化を表す

#### Fixed Skill

- 名前:龍の舞
- 効果:dragonとspeedを増加する。
- 増加はBattle中恒久とする

- 仮値: Dragon `+50` / Speed `+20`
- 仮値: 硬直 `100` / CD `400` / MN `120`
- 再使用時は既存の増加量へ加算する

#### Passive

- 名前:龍の骨格
- 効果:speedに応じてdragonが増加し、dragonに応じてspeedが増加する。
- 両方を派生加算補正として扱う
- 同じ計算段階の`直接加算後Stat` Snapshotから一斉に計算する
- Speedから得たDragonをDragon由来Speedの計算へ再利用せず、逆方向も同様とする
- 仮値: Speedの`20%`をDragonへ、Dragonの`20%`をSpeedへ加算する
- 非Battle中とBattle開始時は共通Stat Calculatorで全Statを参照する
- Battle中は開始時Statとの差分を参照し、[龍の舞]・Buff・DebuffによるDragon / Speedの直接増減を同じ計算段階で相互補正へ反映する

### バリバキ

- Status: `Implemented`
- 仮実装: Skill ID `40` / Passive ID `40`
- Species ID: `40`
- モチーフ: 楔角と重装甲を持つサイ型の竜
- 狙い: Shield破壊と貫通を担う重量級として表す

#### Fixed Skill

- 名前:ドラゴンブレイク
- 効果:
先頭の敵のシールドを全て破壊して、竜ダメージを与える。

- Damageより先に対象の全Shieldを破壊する
- 仮値: Base Dragon Damage `100`
- 仮値: 硬直 `120` / CD `350` / MN `100`

#### Passive

- 名前:龍の怒り
- 効果:竜に応じて、自身の与えるAttribute DamageがRB割合貫通を得る。

- 貫通Value `= Dragon × PenetrationRatio 25%`
- RB割合貫通率 `= 貫通Value / (100 + 貫通Value)`
- 他のRB割合貫通とは乗算合成する
- `ApplyOutgoingModifiers = true`のAttribute Damageを対象とする
- 係数は`DragonRagePassiveAsset`から調整可能にする

### カギヅメ

- Status: `Implemented`
- 仮実装: Skill ID `48` / Passive ID `48`
- Species ID: `48`
- モチーフ: 鉤角と鉤爪を持つ技巧派の竜
- 狙い: ドラゴンフックとクランカー連携を鉤状の造形で表す

#### Fixed Skill

- 名前:ドラゴンフック
- 効果:
先頭の敵に竜ダメージとvalue = 30 + 10 * 竜参照の[状態：ドラゴンクランカー]を与える。

##### [ドラゴンクランカー]
次に受けるドラゴンダメージがvalue%増加する。

- 次のDragon Damage計算直前に倍率を適用して消費する
- Shieldに全Damageを吸収された場合も消費済みとする
- 再付与時は既存Valueへ加算する
- 仮式: Value `= 30 + floor(Dragon × 10%)`
- 仮値: Base Dragon Damage `100`
- 仮値: 硬直 `100` / CD `300` / MN `80`
- 回避によりDamageが0になった場合は付与しない

#### Passive

- 名前:滅多打ち
- 効果:ドラゴンクランカーを受けている敵へのダメージが50%増加する。

- 仮倍率: `150%`
- `ApplyOutgoingModifiers = true`のAttribute Damageを対象とする
- Dragon Damageの場合、滅多打ちの対象判定後にドラゴンクランカー倍率を適用して消費する

### ノックロン

- Status: `Implemented`
- 仮実装: Skill ID `56` / Passive ID `56`
- Species ID: `56`
- モチーフ: 顎角と大きな拳を持つ重量級の竜
- 狙い: ドラゴンアッパーとStun追撃を担う拳闘型として表す

#### Fixed Skill

- 名前:ドラゴンアッパ－
- 効果:先頭の敵に竜ダメージとノックアウトを与える。

##### [状態：ノックアウト]
Stunとしても扱う。Stunと同等だが、ダメージを受けるたびにダメージ*10%効果時間が伸びる。

- 仮値: Base Dragon Damage `100`
- 仮値: ノックアウト `200tick`
- 仮値: 硬直 `120` / CD `400` / MN `120`
- Damageによる延長率は`KnockoutStatusAsset`から調整可能にする
- Attribute / True / Status Damageの最終Damageを延長量の計算に使用する
- Shieldが吸収したDamageも延長対象とする
- 回避によりDamageが0になった場合は付与しない

##### [状態：Stun]
効果時間中、cdや硬直や発生が進まない。Stun中にStunを受けても効果時間が加算されたりはしない（Stun毎に個別に扱う）。

#### Passive

- 名前:
- Implementation: `Implemented`
- 仮表示名: `竜007`（Passive ID `56`。正式名決定後にSOだけ変更する）
- 効果:Stunしている敵へのダメージが増加する。
- `BattleStatusCategory.Stun`を持つ対象へ適用する
- 仮倍率: `130%`
- `ApplyOutgoingModifiers = true`の属性Damageへ適用する
- True DamageとStatus Damageには適用しない
- 倍率は`TargetStatusDamagePassiveAsset`から調整可能にする

### タテゴン

- Status: `Implemented`
- 仮実装: Skill ID `64` / Passive ID `64`
- Species ID: `64`
- モチーフ: 城塞状の甲羅を持つ守護竜
- 狙い: ドラゴンディフェンスとResistBonus補正を要塞風に表す

#### Fixed Skill

- 名前:ドラゴンディフェンス
- 効果:
シールドと[状態：ドラゴンディフェンス]を獲得する。

[状態：ドラゴンディフェンス]
期間中、味方が受けるダメージを代わりに受ける。

- 仮値: Shield Base `150` / Dragon Shield Ratio `100%`
- 仮値: 効果時間 `300tick`
- 仮値: 硬直 `100` / CD `400` / MN `60`
- 敵によるAttribute / True Damageと、ダメージを伴わない状態付与Hitを肩代わりする
- 全体状態付与は対象ごとにHitを解決し、すべて肩代わりできる
- Status Damage、自傷、味方からのDamageは肩代わりしない
- 肩代わり側の属性値・ResistBonus・Shield・回避を使用する
- 複数の使用者がいる場合は前方の生存使用者を優先する
- 使用者自身が攻撃対象の場合は別の使用者へ再転送しない
- 使用者が戦闘不能になるか効果時間が終了すると肩代わりしない

#### Passive

- 名前:龍の守り
- 効果:ResistBonusが竜*20%増加。

- 派生加算補正として扱う
- 非Battle中とBattle開始時は共通Stat Calculatorで全Dragonを参照する
- Battle中は開始時Dragonとの差分を参照し、Buff / Debuffによる増減へ追従する
- 仮値: Dragonの`20%`

## Ideas
