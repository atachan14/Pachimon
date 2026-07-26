# Stat Refactor Plan

## 目的

旧Stat構造を、統合属性、Speed、DamageBonus / ResistBonus、永続MNを含む新構造へ移行する。

基準仕様は以下を参照する。

- [`../v0.1/pachimon-stats.md`](../v0.1/pachimon-stats.md)
- [`../v0.1/map-generation.md`](../v0.1/map-generation.md)
- [`../v0.3/battle-state.md`](../v0.3/battle-state.md)
- [`../v0.3/battle-flow.md`](../v0.3/battle-flow.md)
- [`../v0.3/skill-runtime.md`](../v0.3/skill-runtime.md)
- [`../v0.4/reward-flow.md`](../v0.4/reward-flow.md)

## 変更概要

```text
16属性Power / Resist -> 8統合属性
TurnHaste             -> Speed
SkillHaste            -> Hasteへ整理し、個体Statとして採用
UniversalPower        -> DamageBonus
UniversalResist       -> ResistBonus
MN                    -> HP同様のRun永続Resourceとして追加
```

## Phase 1: 型と設定

1. `PachimonStatType`を14種へ更新する
2. 属性順を`Fire / Aqua / Leaf / Electric / Poison / Ice / Wind / Dragon`へ固定する
3. `PachimonStatGenerationSettings`へResource倍率とSpecial Stat除数を追加する
4. `ModValueSettings`を追加し、FirstElementの上昇量とSecondElement倍率を保持する
5. 旧Enum値を参照するScene / Assetがあるため、Unity上のSerialize値を移行または再生成する

## Phase 2: 個体生成

1. 14種のValue Unitを生成する
2. MaxHP / MaxMNへ最低Value Unitを設定する
3. HP / MNは10倍、Speed / Haste / DB / RBは3分の1へ変換する
4. 変換前Value Unitの合計が全個体で一致することを検証する
5. 同じRun Seedで同じ結果になることを検証する

## Phase 3: Map / Reward / Modifier

1. RewardElementを8属性、MaxHP、MaxMN、Speed、DB、RB、BonusGoldへ更新する
2. First / Secondそれぞれ69枚の同一構成Deckを生成する
3. 同一Nodeで同じElementが重複しないよう組み合わせる
4. FirstElementからTrainerThemeを決定する
5. 属性FirstElementの40NodeだけType一致Pachimonを配置する
6. Elite / Gymのエース判定を統合属性値へ変更する
7. First / Secondで異なるMod上昇量を適用する
8. Badgeを対応する統合属性値へ適用する

## Phase 4: Run Resource

1. `PachimonInstance`へCurrentMNを追加する
2. MaxMNで初期化し、Run中に保持する
3. MaxHP / MaxMN増加時にCurrent値も増加分だけ増やす
4. Preview SnapshotへCurrentMN / MaxMNを追加する
5. RestSpotでHP / MNを50%回復する

## Phase 5: Battle

1. `BattleUnitState`へCurrentMNを追加する
2. Battle開始時にRunのPlayer CurrentMNをSnapshotへ引き継ぐ
3. Battle ResultでPlayer CurrentHP / CurrentMNをRunへ戻す
4. TurnCostへSpeedを適用する
5. CooldownへHaste補正を適用する
6. DamageをSkill指定攻撃Stat、DamageBonus、防御属性値、ResistBonusの順で計算する
7. True Damageが上記補正をすべて無視することを検証する

## Phase 6: UI

1. SidePaneをHP / MNと11Stat表示へ更新する
2. BattleのResource表示へMNを追加する
3. Reward Iconと短縮Labelを新Elementへ更新する
4. Trainer Map Iconの2色を新RewardElementへ対応させる
5. 旧Serialize参照を持つ`PachimonTabLayoutSetup`とGameSceneを更新する

## Phase 7: 検証

1. 300個体の生成価値合計が一致する
2. Reward Deckが各枠69枚で、指定枚数どおりになる
3. 同一Nodeで同一RewardElementが重複しない
4. Attribute / HP / MN / Speed / DB / RB / Goldの両枠上昇量が正しい
5. Badgeが統合属性だけへ適用される
6. Battle前PaneとBattle開始時Statが一致する
7. HP / MNがBattle後とRestSpot後にRunへ正しく残る
8. UnityのConsole Errorがなく、GameSceneとTitleSceneから開始できる

## 実装上の注意

- 旧Enumの数値互換は維持しない。Save実装前のため、新構造を正としてScene / Assetを移行する
- 旧名を互換Aliasとして長期間残さず、1回のリファクタ内で参照を置き換える
- Battle中の一時StatとRun永続Statを混在させない
- ViewはRun / Battleの参照元を判断せず、共通Snapshotだけを表示する
- 各Phase完了時にC# Compileと対象Domain Testを通してから次へ進む
