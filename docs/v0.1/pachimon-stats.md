# Pachimon Stats

## 方針

- StatsはPachimon種ごとの固定値ではなく、Run開始時に個体ごとに生成する
- 全個体のステータス価値合計を一致させる
- HPとMNは最低保証を持つ
- 属性ごとのPower / Resistは分けず、攻撃と防御で同じ属性値を参照する
- Speed、Haste、DamageBonus、ResistBonusはPachimon固有Statとする
- Haste Modは生成しない。Hasteは初期生成とItem / Skill / Passiveによる個体補正で扱う
- 生成結果は整数1刻みとし、5刻みや10刻みへ固定しない
- 稀に0のステータスが生成されることを許容する

## Stat一覧

1. MaxHP
2. MaxMN
3. Fire
4. Aqua
5. Leaf
6. Electric
7. Poison
8. Ice
9. Wind
10. Dragon
11. Speed
12. Haste
13. DamageBonus
14. ResistBonus

内部Enum、生成、表示の属性順はすべて`Fire / Aqua / Leaf / Electric / Poison / Ice / Wind / Dragon`へ統一する。Save互換が必要になる前の段階で数値も揃え、表示順と内部値の食い違いを作らない。

略称は`DamageBonus = DB`、`ResistBonus = RB`とする。

## 各Statの役割

- `MaxHP`: 最大HP
- `MaxMN`: 最大MN。HPと同様にRun中を通して保持するリソース
- 8属性: Skillが参照する攻撃値と、同属性Damageを受ける際の防御値を兼ねる
- `Speed`: Skill使用後、次のTurnを得るまでのTickを短縮する
- `Haste`: SkillのCooldown Tickを短縮する
- `DamageBonus`: 通常Damageへ加える個体共通の攻撃補正
- `ResistBonus`: 通常Damageへ加える個体共通の防御補正

攻撃側がどのStatを参照するかはSkill Logicが決める。属性Skillであっても、共通Damage処理から使用者の同属性値を自動参照しない。

## Value Unitと表示値

生成時は14種すべての内部Value Unitを保持し、価値合計の検証には変換前の値を使用する。

```text
MaxHP       = MaxHP Value Units * 10
MaxMN       = MaxMN Value Units * 10
Attribute   = Attribute Value Units
Speed       = floor(Speed Value Units / 3)
Haste       = floor(Haste Value Units / 3)
DamageBonus = floor(DamageBonus Value Units / 3)
ResistBonus = floor(ResistBonus Value Units / 3)
```

`Speed / Haste / DamageBonus / ResistBonus`の端数を捨てても、生成価値そのものは失われた扱いにしない。将来倍率を変更できるよう、表示値だけでなく内部Value Unitを生成結果へ残す。

## 暫定設定

```text
allocationBudget          = 1300
maxHpMinimumValueUnits    = 50
maxMnMinimumValueUnits    = 50
resourceDisplayMultiplier = 10
specialStatDivisor        = 3
initialMaxAllocation      = 100
additionalMaxAllocation   = 100
totalValueUnits           = 1400
```

数値はバランス調整前の仮値とする。生成ロジックから分離した`PachimonStatGenerationSettings`で変更可能にする。

`initialMaxAllocation`と`additionalMaxAllocation`は別々に調整できる。

- initial: 各Statへ最初に配る値の範囲と、初期値0の発生率に影響する
- additional: 残りBudgetの配分回数と、最終結果の尖り方に影響する

## 生成手順

1. MaxHPとMaxMNのValue Unitへ最低保証を設定する
2. 配分対象14種の順序をシャッフルする
3. 各Statへ一度ずつ`0..initialMaxAllocation`を配分する
4. 残りBudgetがなくなるまで、ランダムなStatへ`1..additionalMaxAllocation`を配分する
5. 最後の配分量が残りBudgetを超える場合は、残りBudgetだけ配分する
6. 価値合計が`maxHpMinimumValueUnits + maxMnMinimumValueUnits + allocationBudget`と一致することを検証する
7. Value Unitから表示値へ変換する

同じ`runSeed`では同じ個体Statsを生成する。

## CurrentHP / CurrentMN

- 個体生成時は`CurrentHP = MaxHP`、`CurrentMN = MaxMN`とする
- CurrentHPとCurrentMNはBattleをまたいでRun中の個体へ保持する
- Battle開始時に全回復・リセットしない
- MaxHPまたはMaxMNが増えた場合は、増加量だけ対応するCurrent値も増やす
- 最大値が減った場合はCurrent値を新しい最大値以下へClampする
- RestSpotでは戦闘不能を解除し、MaxHP / MaxMNの50%をそれぞれ回復する

現行の基本SkillはMNを消費しない。将来のSkillは個別LogicでMN消費量と使用条件を定義する。
