# Battle State

## 境界

Pachimonデータを静的定義、Run中の個体、1戦だけの可変Stateに分ける。

```text
PachimonCatalog / PachimonDefinition
  -> 種族名、Graphic、固定Skill、Passiveなどの静的定義

PachimonInstance
  -> Run中の個体
  -> InstanceId、Stats、SkillIds、PassiveIds、CurrentHP、CurrentMN

TrainerModifierSet
  -> 対象Trainerが持つMods / Badges
  -> Trainerの全Pachimonへ共通適用

BattleUnitState
  -> 1戦だけの可変State
  -> Battle終了後に破棄する
```

## PachimonInstance

Run中に保持するもの:

- `instanceId`
- `speciesId`
- 生成済み基礎Stats
- 所持Skill ID
- 所持Passive ID
- `currentHp`
- `currentMn`

`currentHp`と`currentMn`は個体生成時に対応する最大値で初期化し、Battle終了時にPlayer側の結果を反映する。RestSpotなど後続Nodeはこの値を回復する。どちらもBattle開始時に全回復・リセットしない。

固定Passiveは`FixedPassiveId`として保持し、所持一覧`PassiveIds`の初期要素にする。Rewardで取得したPassiveは`AddPassive()`で同じ一覧へ追加し、Battle開始時に全件を登録する。

Battle中の一時的なStat増減、Cooldown、状態異常、Passive固有Stateは保持しない。将来Run中ずっと残る効果を追加する場合は、Battleの一時補正をそのまま保存せず、明示的な永続変更として分ける。

## TrainerModifierSet

Mods / Badgesによる補正は各`PachimonInstance.Stats`へ直接加算せず、Trainer単位で保持する。

`TrainerModifierSet`は表示値単位の加算値を保持し、`EffectivePachimonStats`が基礎Statsと合成する。Modごとの具体的な加算量は`ModValueSettings`から取得し、この計算層では固定しない。

```text
PachimonInstanceの基礎Stats
+ TrainerModifierSet
= 非Battle時の補正済みStats
```

- PlayerはRun中に取得したMods / Badgesを持つ
- EnemyはNode生成時に割り当てたModsまたはBadgeを持つ
- 1つのModifierSetを対象Trainerの全Pachimonへ適用する
- Paneの事前情報にも補正済みStatsを表示する
- Battle開始時は補正済みStatsを`BattleUnitState`の開始時Snapshotにする
- 同じ補正を各個体へ書き込まず、二重適用を防ぐ

Badgeは属性ごとの所持数を`TrainerModifierSet`へ保持し、対応する統合属性値へ1個につき30パーセントポイントを加算する。

```text
Effective Attribute Stat
  = floor((Base Stat + Flat Mod Addition) * (100 + BadgeCount * 30) / 100)
```

MaxHP、MaxMN、Speed、DamageBonus、ResistBonusはBadge倍率の対象外とする。

### MaxHP / MaxMN変更

TrainerModifierSetの更新によってEffective MaxHPまたはMaxMNが増えた場合、増加量だけ対応するCurrent値も増やす。

```text
hpDelta = NewMaxHP - OldMaxHP
CurrentHP = clamp(CurrentHP + max(0, hpDelta), 0, NewMaxHP)

mnDelta = NewMaxMN - OldMaxMN
CurrentMN = clamp(CurrentMN + max(0, mnDelta), 0, NewMaxMN)
```

最大値が減少する場合はCurrent値を新しい最大値以下へClampし、減少量に応じた追加DamageやMN消費は発生させない。

永続Modifierの追加は`TrainerModifierService`を経由する。MaxHP / MaxMN以外はModifierSetだけを更新し、MaxHP / MaxMNは対象Trainerの全PachimonへCurrent値の調整も同時に適用する。

DamageBonusとResistBonusはTrainer専用値ではなくPachimon固有Statである。Mod / Badgeの共通補正は`TrainerModifierSet`から全Partyへ加算するが、Itemや永続効果では特定の`PachimonInstance`だけを強化できる。Battle中の一時変化は`BattleUnitState`へ保持する。

## BattleState

Battle全体で保持するもの:

- `currentTick`
- Player / Enemyの`BattleSideState`
- Tie Priority
- Battle Seed付き乱数
- Battle Event Dispatcher
- 現在の進行Phase
- 勝敗
- BattleLogへ渡すEvent列

## BattleSideState

- PlayerまたはEnemyの所属
- Party順を維持した3つのSlot
- 生存Unitの検索
- 全滅判定

## BattleUnitState

- 参照元`instanceId`
- 所属Side
- `slotIndex`
- `currentHp`
- `currentMn`
- `nextTurnTick`
- Skill IDごとの`cooldownReadyTick`
- Battle中の加算 / 乗算Stat補正
- 状態異常
- Passive固有State
- 戦闘不能状態

基礎StatsとLoadoutは参照元InstanceからSnapshotとして読み込み、Battle中の計算は`BattleUnitState`を経由する。Battle処理から`PachimonInstance.Stats`を直接変更しない。

Battle開始時のStatsは、基礎StatsへTrainerModifierSetを適用した補正済み値とする。Battle中の一時補正はその開始時Statsへ重ねる。

## Formation

```text
slotIndex 0: 先頭
slotIndex 1: 中央
slotIndex 2: 最後尾
```

- 戦闘不能になってもSlotを詰めない
- 先頭は最小SlotIndexの生存Unit
- 最後尾は最大SlotIndexの生存Unit
- 使用者より後ろは、使用者より大きいSlotIndexの生存味方
- 使用者より前は、使用者より小さいSlotIndexの生存味方

## Result反映

Battle本体はRun Stateを直接更新せず、`BattleResult`を生成する。Battle終了接続側がPlayerの個体IDとCurrentHP / CurrentMNを検証して`PachimonInstance`へ反映する。

実装では`BattleStateFactory`がRun個体から両陣営のSnapshotを作り、`BattleResultCommitter`が勝利結果のPlayer CurrentHP / CurrentMNをRunへ反映する。EnemyはBattle開始時にEffective MaxHP / MaxMNで初期化し、Run個体側のCurrent値は参照しない。

EnemyのCurrentHP / CurrentMN、Battle中補正、状態異常、CooldownはRunへ反映しない。

## Pane表示用Snapshot

LeftPane / RightPaneは`PachimonInstance`と`BattleUnitState`を直接切り替えて参照せず、共通の不変な表示用Snapshotを受け取る。

```text
非Battle中:
PachimonInstance + TrainerModifierSet
  -> PachimonStatusSnapshot
  -> LeftPane / RightPane

Battle中:
BattleUnitState
  -> PachimonStatusSnapshot
  -> LeftPane / RightPane
```

`PachimonStatusSnapshot`の主な内容:

- Instance ID
- 名前とGraphic
- CurrentHP / Effective MaxHP
- CurrentMN / Effective MaxMN
- 現在のEffective Stats
- SkillとBattle中Cooldown
- Passive
- 状態異常

ViewはBattle中かどうかを判断せず、同じBind処理でSnapshotを表示する。Damage、回復、MN消費、Stat補正、Cooldown、状態異常の変更後に新しいSnapshotを生成し、対応PaneとHP / MN Barを更新する。

実装上の共通Snapshot型は既存UIとの互換性を保つため`PachimonPreviewContent`とし、`PachimonPreviewFactory`がRun個体／Battle Unitの両方から生成する。

Battle前のEnemy情報はNode上のTrainerModifierSetを適用して作るため、RightPaneで確認したStatsとBattle開始時Statsを一致させる。
