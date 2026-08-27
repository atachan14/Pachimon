# Slow Statuses

Speedを低下させる状態群。状態ごとにValue減衰型と効果時間型がある。

## 共通仕様

- `Category`: `Slow`
- `SlowStatusAsset`へStatus ID、表示、説明、毎tick減衰量、付与時軽減属性を保持する
- 状態ごとに実効Speed減少量を計算し、その合計をSpeedから減算する
- Slow適用後のSpeedは負数になり得る
- 現在進行中の初期待機、発生、硬直へ即座に反映する
- `DecayPerTick > 0`の状態は、各tickの行動時計進行後にValueを減らす
- `Slow`と`Chill`は`DecayPerTick = 1`、`Paralysis`は`DecayPerTick = 0`とする
- Value減衰型はValueが0、効果時間型はRemainingTicksが0になると終了する
- 付与時の軽減後Valueが0のSlowは保持しない
- `Slow`と`Chill`は同じStatus IDの再付与時にValueを加算する
- `Paralysis`は付与ごとに独立したスタックとして保持する
- 異なるStatus IDは別々に保持し、Speed補正時にValueを合算する

```text
TotalSlow
= 有効なSlowの実効Speed減少量の合計

BattleSpeed
= Speed - TotalSlow
```

例:

```text
麻痺 10 [50tick]を保持している対象へ麻痺 10 [100tick]を再付与
↓
麻痺 10 [50tick] + 麻痺 10 [100tick]

麻痺 20と、実効Speed減少量30の冷気を同時に保持
↓
TotalSlow 50
```

負のSpeedによる時間延長は[Timing](../mechanics/timing.md)を参照する。

## 麻痺

- `StatusId`: `Paralysis`
- `Category`: `Slow`
- `ParalysisStatus.asset`を使用する
- 付与されるValueを対象のElectricとDamage共通の`ReductionMultiplier`によって軽減する
- 効果時間中はValueを減衰させない
- 再付与は加算せず、効果時間を個別に持つ独立スタックとして追加する

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
- 冷気によるSpeed減少量はValueを直接使わず、平方根式で算出する
- `SpeedReductionScale`の仮値は`50`

```text
冷気Value
= floor(
    付与Value
    × ReductionMultiplier(対象のIce)
  )
```

```text
冷気によるSpeed減少量
= floor(sqrt(冷気Value × SpeedReductionScale))
```

- Value 50でSpeed -50、Value 100で約-70、Value 200で-100、Value 500で約-158
- 通常は毎tick Valueを1減少する
- 氷の大地が存在する間は、氷の大地Valueに応じて減衰量が低下する
- 小数の減衰量は冷気Instanceごとに蓄積し、整数になった分だけValueから減算する

`ReductionMultiplier`の符号付きStat対応は[Scaling](../mechanics/scaling.md#reductionmultiplier)を参照する。
