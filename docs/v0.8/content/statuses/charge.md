# Charge Statuses

「充電」で使用時のElectricを保存し、防御期間を経て攻撃・行動補正へ
遷移する時限状態異常群。

## 共通仕様

- `Value = max(1, 使用時のBattle Electric)`をスナップショットとして保存する
- `充電中`と`充電完了`は同じStatus ID同士でも統合せず、別スタックとして保持する
- 各スタックは独立したValueと残り時間を持つ
- Electricの乗算補正は、ほかの状態異常を含む全乗算補正と乗算する
- 戦闘不能時に、ほかの状態異常と一緒にすべて解除する

## 充電中

- `StatusId`: `Charging`
- `Category`: `Charge`
- 効果時間: `max(1, floor(Value × 400%)) tick`
- ResistBonus: `Value × 40%`を直接加算
- Electric: `50%`を直接乗算
- 効果時間終了時に自身を取り除き、同じValueの`充電完了`を付与する

## 充電完了

- `StatusId`: `Charged`
- `Category`: `Charge`
- 効果時間: `max(1, floor(Value × 200%)) tick`
- Electric: `150%`を直接乗算
- Speed: `Value × 100%`を直接加算

## 調整数値

各割合は`ChargeSkillAsset`に保持し、Inspectorから変更できる。
