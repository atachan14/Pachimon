# Scaling Mechanics

StatをDamage、状態Value、軽減、時間短縮などの係数へ変換する共通式をまとめる。

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

StatからDamageや状態Valueなどの効果量を生成する標準式。

```text
ScaleFromBase(BaseValue, Stat, Ratio)
= BaseValue
  × AmplificationMultiplier(Stat × Ratio / 100)
```

- `BaseValue`は`Stat = 0`で得られる効果量
- `Ratio`はStatの影響度とし、基本値は`100`
- `Ratio = 50`でStatの影響を半分、`200`で倍にする
- Skillの標準的なDamage・状態Value・回復・Shield・効果時間はRatioをSOに保持せず、対応属性を`100%`参照する
- 標準効果量の調整は`BaseDamage`、`BaseValue`、`BaseDurationTicks`などのBase値で行う
- 複数属性の合算、割合そのものを生成する効果、意図的に属性影響度を変える固有式だけは個別Ratioを保持できる
- 個別Ratioを持つ場合は、入力元と用途が分かる名前（例：`WindPenetrationRatio`）にする
- Valueから別の値を導出する場合は`ValueHpRatio`など、同じく入力元から出力先の順にする

### 個別Ratioを残す例外

- 複数の属性・環境値を参照し、割合生成も兼ねる効果：`日光浴`、`蒸発`
- 貫通率や軽減率など割合そのものを生成する効果：`バックファイア`、`ウォーターカッター`、`ポイズンシールド`
- 100%以外の属性影響度が固有仕様になっている効果：`ドラゴンクランカー`、`フローズンブレイク`、`セカンドウィンド`
- Base値との単純な積ではないTiming式：`氷の刃`、`ソーラービーム`

これらは式を個別に再検討するまでRatioをSOへ残す。

### 例

```text
BaseValue = 80
Fire = 100
Ratio = 50

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

## Battle中のAttribute Ratio補正

- Weather・Status・PassiveなどがAttributeの影響度を変える場合、Stat本体ではなくRatioへMultiplierを適用する
- `BattleState.ResolveAttributeRatio(Attribute, BaseRatio)`を共通入口とする
- Skillの標準効果はBaseRatioを常に`100`として補正する
- 例外的な固有効果だけはSOに設定した個別RatioをBaseRatioとして補正する
- 防御計算は攻撃・効果用Ratioを参照しない

```text
EffectiveAttributeRatio
= BaseAttributeRatio
  × WeatherRatioMultiplier
  × StatusRatioMultiplier
  × PassiveRatioMultiplier
  × OtherRatioMultiplier
```

- 現段階ではWeather Ratioのみ実装済み
- SOのBaseRatioは変更せず、Battle解決時に補正値を計算する

## 端数処理

係数の計算中は端数を維持する。整数化と最低保証は、Damage、状態、Timingなど各Mechanicsの最終処理で行う。
