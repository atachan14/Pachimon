# Scaling Mechanics

StatをDamage、状態異常Value、軽減、時間短縮などの係数へ変換する共通式をまとめる。

## AmplificationMultiplier

Statが高いほど効果量を増やす場合に使用する。

```text
AmplificationMultiplier(Stat)
= Stat >= 0
  ? 1 + Stat / 100
  : 100 / (100 - Stat)
```

| Stat | Multiplier |
| ---: | ---: |
| -200 | 0.333... |
| -100 | 0.5 |
| 0 | 1.0 |
| 50 | 1.5 |
| 100 | 2.0 |
| 200 | 3.0 |

- 正数は線形に効果量を増加させる
- 負数は効果量を0未満にせず漸減させる
- `Stat = 0`は等倍とする

## ReductionMultiplier

Statが高いほど対象の量を小さくする場合に使用する。

```text
ReductionMultiplier(Stat)
= Stat >= 0
  ? 100 / (100 + Stat)
  : 1 + (-Stat / 100)
```

| Stat | Multiplier |
| ---: | ---: |
| -200 | 3.0 |
| -100 | 2.0 |
| 0 | 1.0 |
| 50 | 0.666... |
| 100 | 0.5 |
| 200 | 0.333... |

- 正数は対象の量を0未満にせず漸減させる
- 負数は対象の量を線形に増加させる
- Damage軽減では`DefenseMultiplier`として使用する
- Speed / Hasteによる時間短縮では`TimingMultiplier`として使用する
- `DefenseMultiplier`と`TimingMultiplier`は用途名であり、数式は`ReductionMultiplier`と同じ

## ScaleFromBase

StatからDamageや状態異常Valueなどの効果量を生成する標準式。

```text
ScaleFromBase(BaseValue, Stat, ScalingRatio)
= BaseValue
  × AmplificationMultiplier(Stat × ScalingRatio / 100)
```

- `BaseValue`は`Stat = 0`で得られる効果量
- `ScalingRatio`はStatの影響度とし、基本値は`100`
- `ScalingRatio = 50`でStatの影響を半分、`200`で倍にする
- SOでは`BasePower / BaseValue`と`XxxScalingPercent`のように別々の調整項目として保持する

### 例

```text
BaseValue = 80
Fire = 100
ScalingRatio = 50

EffectValue
= 80 × AmplificationMultiplier(50)
= 120
```

## 個別式を使う値

`ScaleFromBase`は効果量を生成するための式であり、入力値をそのまま保存する用途には使わない。

- 付与時のStatを保存するスナップショット
- 受けたDamageに対する割合
- スタック数
- 派生Stat加算

例えば充電のValueに使用時のElectricをそのまま保存する場合は、次の個別式とする。

```text
Value = Electric × SnapshotScalingRatio / 100
```

## 端数処理

係数の計算中は端数を維持する。整数化と最低保証は、Damage、状態異常、Timingなど各Mechanicsの最終処理で行う。
