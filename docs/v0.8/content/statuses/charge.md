# Charge Status

「充電」で発生開始時のElectricを保存し、Skill発動までの防御期間を経て
攻撃・行動補正へ遷移する状態。

## 共通仕様

- `Value = max(1, 発生開始時のBattle Electric)`をスナップショットとして保存する
- `StatusId`は両Phase共通の`Charge`
- Runtimeの`ChargePhase`で`Charging` / `Charged`を区別する
- 同じStatus IDでも統合せず、使用ごとに独立したStatus Instanceとして保持する
- 各Instanceは独立したValue、残り時間、Phaseを持つ
- 検索時は`GetChargeStatuses(ChargePhase)`を使用する
- Electricの乗算補正は、ほかの状態を含む全乗算補正と乗算する
- 戦闘不能時に、ほかの状態と一緒にすべて解除する

## 充電中

- `ChargePhase`: `Charging`
- `Category`: `Charge`
- 独自の効果時間は持たず、Skillの発生中だけ存在する
- ResistBonus: `Value × ChargingResistBonusRatio / 100`を直接加算
- Electric: `ChargingElectricRatio / 100`を直接乗算
- Skill発動時に自身を取り除き、同じValueの`充電完了`を付与する

## 充電完了

- `ChargePhase`: `Charged`
- `Category`: `Charge`
- 効果時間: `max(1, floor(Value × ChargedDurationRatio / 100)) tick`
- Electric: `ChargedElectricRatio / 100`を直接乗算
- Speed: `Value × ChargedSpeedRatio / 100`を直接加算

## Definition

状態名、説明、充電完了の効果時間、Stat補正率は`ChargeStatusAsset`に保持し、Inspectorから変更できる。充電Skillの発生・硬直・CDは`ChargeSkillAsset`に保持する。

`ChargeSkillAsset`は付与する`ChargeStatusAsset`への参照だけを保持する。これにより、将来Itemや別Skillから同じ充電を付与するときも状態仕様を再利用できる。

表示名と説明は`BattleStatusAsset.GetDisplayName(instance)` / `GetDescription(instance)`を通す。`ChargeStatusAsset`はこれらをOverrideし、RuntimeのPhaseに応じて「充電中」「充電完了」を返す。
