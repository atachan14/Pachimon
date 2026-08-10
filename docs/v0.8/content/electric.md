# Electric Content

Electric属性のPachimon、固定Skill、Passiveをまとめる。

共通仕様:

- [Damage](./mechanics/damage.md)
- [Timing](./mechanics/timing.md)
- [Status Effects](./mechanics/status-effects.md)

## Pachimon

### [Pachimon名] 2

- Status: `Verified`
- Species ID: `12`
- モチーフ:
- 狙い: AquaとElectricを組み合わせ、漏電から全体ダメージへつなげる

#### Fixed Skill

- 名前: アクアショック
- Implementation: `Implemented`
- 硬直: `80`
- CD: `200`
- MN: `80`
- 対象: 先頭の敵
- 効果:
  - `10 × AmplificationMultiplier(Electric)`のElectricダメージを与える
  - `10 × AmplificationMultiplier(Aqua)`のAquaダメージを与える
  - `Value = 10 × AmplificationMultiplier(Aqua)`の`漏電`を付与する

##### 状態: 漏電

- 保持者がPachimonによるElectric攻撃を受けるとき、漏電を消費する
- 漏電を消費した場合、保持者側のParty全体へ次のElectricダメージを発生させる

```text
追加ダメージ = 保持者が受けたElectricダメージ × 漏電Value%
```

例:

```text
Player 1がEnemy 1へ漏電を付与
Player 2がEnemy 1へElectricダメージを与える
↓
Enemy 1の漏電を消費
Enemy 1、Enemy 2、Enemy 3へElectricダメージを発生させる
```

Pachimonを発生源とする自傷ダメージも、Electric攻撃であれば漏電の発動対象とする。
漏電・毒素など`Origin = Status`のダメージや、Pachimonを発生源としないダメージでは発動しない。
追加ダメージは各対象のElectricで再度軽減される（原則、属性ダメージは全て属性値によって軽減される）

- 必要な新規仕組み:
  - 消費型状態
  - ダメージ属性を条件とした発動
  - Party全体への追加ダメージ
- 補足仕様:
  - 漏電による追加ダメージでは別の漏電を起動しない
  - 追加ダメージの発生源は`Status / Leak`とし、Pachimonは発生源として保持しない
  - 攻撃扱いにはせず、攻撃者のAttribute、DamageBonus、送出Passiveは適用しない
  - 対象のElectricとResistBonusによる軽減は適用する
  - 全体攻撃で複数の漏電保持者が同時にダメージを受けた場合、保持者ごとに漏電を発動する
  - 同じ対象へ同一Status IDの漏電を再付与した場合、既存Valueへ加算する
  - 漏電Valueは付与時に切り捨てて保持する
  - 発動時は対象が持つ全`Leak`のValueを合計して消費し、Party全体への追加ダメージを1回発生させる
  - 発動ログは`XXは漏電している！`の後、Party各対象へのダメージを連続表示する

#### Passive

- 名前: 水力発電
- Implementation: `Implemented`
- 効果: `max(0, Aqua × 30%)`をElectricへ加算する
- 発動タイミング: Stat計算時
- 必要な新規仕組み:
  - 下限を持つ派生加算補正

### [Pachimon名] 3

- Status: `Verified`
- Species ID: `20`
- モチーフ:
- 狙い: Fireを利用してElectricダメージと貫通率を高める

#### Fixed Skill

- 名前: 電気爆発
- Implementation: `Implemented`
- 硬直: `100`
- CD: `250`
- MN: `130`
- 対象: 先頭の敵
- 効果:
  - 次の暫定式によるElectricダメージを与える
  - `Fire × 0.2%`の貫通率を持つ

```text
暫定ダメージ
= 50
  × AmplificationMultiplier(Electric)
  × AmplificationMultiplier(Fire)

貫通率（%） = Fire × 0.2
```

##### 貫通

対象の属性値とResistBonusによるダメージ軽減を、貫通率分だけ減少させて計算する。
貫通率に上限は設けない。

例:

```text
30%の貫通を持つElectricダメージ
↓
対象のElectricとResistBonusを、それぞれ30%減少させて軽減計算
```

- 必要な新規仕組み:
  - ダメージ単位の貫通率（実装済み）

#### Passive

- 名前: 火力発電
- Implementation: `Implemented`
- 効果: `max(0, Fire × 30%)`をElectricへ加算する
- 発動タイミング: Stat計算時
- 必要な新規仕組み:
  - 下限を持つ派生加算補正

### [Pachimon名] 4

- Status: `Verified`
- Species ID: `28`
- モチーフ:
- 狙い: Windを利用して短い間隔で複合属性攻撃を行う

#### Fixed Skill

- 名前: 電光石火
- Implementation: `Implemented`
- 硬直: `60`
- CD: `100`
- MN: `60`
- 対象: 先頭の敵
- 効果:
  - `25 × AmplificationMultiplier(Electric)`のElectricダメージを与える
  - `10 × AmplificationMultiplier(Fire)`のFireダメージを与える
  - 硬直とCDをWindに応じて軽減する

```text
EffectiveRecovery
= ceil(
    BaseRecovery
    * TimingMultiplier(Speed)
    * TimingMultiplier(Wind)
  )

EffectiveCooldown
= ceil(
    BaseCooldown
    * TimingMultiplier(Haste)
    * TimingMultiplier(Wind)
  )
```

- 必要な新規仕組み:
  - Skill固有の硬直補正（実装済み）
  - Skill固有のCD補正（実装済み）
- 補足仕様:
  - 硬直とCDには同じ軽減処理を使う
  - 正の硬直とCDは最低1tickとする

#### Passive

- 名前: 風力発電
- Implementation: `Implemented`
- 効果: `max(0, Wind × 30%)`をElectricへ加算する
- 発動タイミング: Stat計算時
- 必要な新規仕組み:
  - 下限を持つ派生加算補正

### [Pachimon名] 5

- Status: `Verified`
- Species ID:
- モチーフ:
- 狙い: 一時的に防御へ回り、その後ElectricとSpeedを強化する

#### Fixed Skill

- 名前: 充電
- Implementation: `Implemented`
- 発生: `300`
- 硬直: `0`
- CD: `500`
- MN: `400`
- 対象: 自身
- 効果: 発生開始時に`Value = 使用時のElectric`として`充電中`を付与し、発動時に同じValueの`充電完了`へ切り替える
- 状態の共通仕様と実装上の安全規則: [Charge Statuses](./statuses/charge.md)

##### 状態: 充電中

- 効果時間は持たず、Skillの発生中だけ存在する
- ResistBonusを`Value × 40%`増加させる
- Electricを`50%`減少させる
- Skill発動時に充電中を消費し、同じValueの`充電完了`を付与する

##### 状態: 充電完了

- 効果時間: `Value × 200% tick`
- Electricを`50%`増加させる
- Speedを`Value × 100%`増加させる

- 必要な新規仕組み:
  - 付与時のStatを保存する状態
  - 状態終了時の別状態への遷移
  - 時限Stat補正
- 補足仕様:
  - Valueには発生開始時のElectricをスナップショットとして保存する
  - 充電中と充電完了は、それぞれ別スタックとして重複できる
  - Electricの`±50%`は、別の状態による補正と乗算する
  - 戦闘不能になると、充電中・充電完了を含むすべての状態を取り除く

#### Passive

- 名前: 静電気
- Implementation: `Implemented`
- 効果: 攻撃を受けるたび、攻撃者へ次のValueの[麻痺](./statuses/slow.md#麻痺)を付与する

```text
麻痺Value
= floor(20 × AmplificationMultiplier(Electric))
  + floor(10 × AmplificationMultiplier(Ice))
```

- 必要な新規仕組み:
  - 属性・確定ダメージ共通の`AttackReceivedEvent`（実装済み）
  - tickごとにValueが減衰する加算型状態
- 補足仕様:
  - 自傷ダメージと継続ダメージでは発動しない
  - 攻撃者が存在するダメージでのみ発動する
  - 多段Skillでは、攻撃を受けた回数分発動する
  - 受けたダメージが0でも発動する
  - 確定ダメージでも`IsAttack = true`なら発動する

## 既存共通Skill

### ビリビリショック

- Implementation: `Implemented`
- 対象: 先頭の敵
- 既存のElectricダメージに加えて、対象へ[麻痺](./statuses/slow.md#麻痺)を付与する

```text
麻痺Value
= floor(50 × AmplificationMultiplier(Electric))
  + floor(25 × AmplificationMultiplier(Ice))
```

- 再付与と時間進行は[Slow共通仕様](./statuses/slow.md#共通仕様)に従う

### [Pachimon名] 6

- Status: `Verified`
- Species ID: `44`
- モチーフ:
- 狙い: 長い発生時間と高コストの代わりに、大ダメージと超過ダメージを与える

#### Fixed Skill

- 名前: 電磁砲
- Implementation: `Implemented`
- 硬直: `100`
- CD: `500`
- MN: `500`
- 発生: `300`
- 対象: 先頭の敵
- 効果:
  - 次のElectricダメージを与える
  - このSkillで対象を戦闘不能にした場合、超過ダメージを次の先頭の敵へ与える

```text
ダメージ = 400 × AmplificationMultiplier(Electric)
```

##### 発生

[Timing](./mechanics/timing.md)の共通仕様に従う。

- 必要な新規仕組み:
  - Skillの発生時間（実装済み）
  - 発生待機状態（実装済み）
  - 超過ダメージの引き継ぎ（実装済み）
- 補足仕様:
  - 超過ダメージは、次の対象のElectricで改めて軽減する
  - 超過ダメージへ攻撃側のElectric、DamageBonus、与ダメージPassiveを再適用しない
  - 超過ダメージが次の対象も戦闘不能にした場合、残りをさらに次の先頭へ引き継ぐ
  - 対象は効果解決時点の先頭の敵とする

#### Passive

- 名前: 蓄電
- Implementation: `Implemented`
- 効果: Battle中にElectricダメージが発生するたび、自身へ蓄電を1スタック付与する

##### 状態: 蓄電

- 自身がElectricダメージを与えるとき、蓄電をすべて消費する
- 与えるElectricダメージを`消費スタック数 × 10%`増加させる

- 必要な新規仕組み:
  - ダメージ属性を条件としたスタック獲得
  - 与ダメージ直前のスタック消費
- 補足仕様:
  - 自身がElectricダメージを発生させる際は、蓄電の消費後にスタックを獲得する
  - Electricダメージが対象ごと・ヒットごとに発生した回数だけ蓄電を獲得する
  - 全体攻撃では、ダメージを受けた対象が3体なら3スタック、2体なら2スタック、1体なら1スタック獲得する
  - 漏電などによる追加ダメージでもスタックを獲得する
  - ダメージが0でも蓄電を獲得・消費する
  - 多段Electricダメージでは、各ヒットで蓄電を適用する

## Ideas
