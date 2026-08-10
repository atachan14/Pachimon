# Slow Statuses

Speedを低下させ、Valueが時間経過で減衰する状態群。

## 共通仕様

- `Category`: `Slow`
- `SlowStatusAsset`へStatus ID、表示、説明、毎tick減衰量、付与時軽減属性を保持する
- 有効な全Slowの`Value * StackCount`を合計し、Speedから減算する
- Slow適用後のSpeedは負数になり得る
- 現在進行中の初期待機、発生、硬直へ即座に反映する
- 各tickの行動時計進行後にDefinitionの`DecayPerTick`だけValueを減らす
- 現在の`Slow`、`Paralysis`、`Chill`はすべて`DecayPerTick = 1`とする
- Valueが0になったSlowは終了する
- 付与時の軽減後Valueが0のSlowは保持しない
- 同じStatus IDを再付与した場合はValueを加算する
- 異なるStatus IDは別々に保持し、Speed補正時にValueを合算する

```text
TotalSlow
= 有効なSlowのValue合計

BattleSpeed
= Speed - TotalSlow
```

例:

```text
麻痺 10を保持している対象へ麻痺 10を再付与
↓
麻痺 20

麻痺 20と冷気 30を同時に保持
↓
TotalSlow 50
```

負のSpeedによる時間延長は[Timing](../mechanics/timing.md)を参照する。

## 麻痺

- `StatusId`: `Paralysis`
- `Category`: `Slow`
- `ParalysisStatus.asset`を使用する
- 付与されるValueを対象のElectricとDamage共通の`ReductionMultiplier`によって軽減する
- 同じ対象への麻痺再付与では、軽減後Valueを既存の麻痺へ加算する

```text
麻痺Value
= floor(
    付与Value
    × ReductionMultiplier(対象のElectric)
  )
```

## 冷気

- `StatusId`: `Chill`
- `Category`: `Slow`
- `ChillStatus.asset`を使用する
- 付与されるValueを対象のIceとDamage共通の`ReductionMultiplier`によって軽減する
- 同じ対象への冷気再付与では、軽減後Valueを既存の冷気へ加算する

```text
冷気Value
= floor(
    付与Value
    × ReductionMultiplier(対象のIce)
  )
```

`ReductionMultiplier`の符号付きStat対応は[Scaling](../mechanics/scaling.md#reductionmultiplier)を参照する。
