# Timing Mechanics

Battle中のSkill発動、tick、イベント順序に関する共通仕様をまとめる。

## Skill時間項目

- `発生 / Startup`: Skill選択から効果解決までに必要なtick
- `硬直 / Recovery`: 効果解決から次のTurnまでに必要なtick
- `CD`: 同じSkill Slotを再使用できるまでに必要なtick
- `MN`: Skill使用時に消費するMN

## Speed

Speedは発生と硬直に適用する。
0を補正なしとし、負数も許可する。
係数の定義は[Scaling](./scaling.md#reductionmultiplier)を参照する。

```text
TimingMultiplier(Speed)
= ReductionMultiplier(Speed)

ProgressPerTick
= 1 / TimingMultiplier(Speed)
```

- 正のSpeedは発生と硬直を短縮する
- 負のSpeedは絶対値に応じて発生と硬直を線形に延長する
- Speedが`-100`でもゼロ除算は発生せず、時間はBase値の200%になる
- 発生・硬直の残り作業量は、毎tickの`ProgressPerTick`だけ減少する
- Phase途中でSpeedが変化した場合、次のtickから進行速度へ反映する

## Haste

HasteはCDに適用し、Speedと同じ`TimingMultiplier`を使用する。

```text
EffectiveCooldown
= BaseCooldown == 0
  ? 0
  : max(1, ceil(BaseCooldown * TimingMultiplier(Haste)))
```

- 正のHasteはCDを短縮する
- 負のHasteはCDを線形に延長する
- CDの残り作業量は毎tickの現在Hasteで進行する

## 発生

Skillを選択してから効果が実行されるまでの時間。単位はtickとし、Speedによる軽減対象とする。

- 全Skillが内部的に`StartupTicks`を持ち、通常値は`0`とする
- 発生が`0`のSkillは即時に効果を解決する
- 発生が`0`の場合、仕様書とUIでは発生を省略する
- 発生中は次のTurnを得ない
- 発生中も他のUnitの行動とBattle Tickは進行する
- 発生中に使用者が戦闘不能になった場合、Skillを不発にする
- 対象の確定時点はSkill固有とする
- `先頭`など位置を参照するSkillは、原則として効果解決時点の対象を使用する
- 将来、選択した個体を発生中も追跡するSkillは、Skill Contextへ対象を保存する
- CDはSkill選択時に開始する
- MNを消費するSkillはSkill選択時に消費する
- 発生完了とUnitのTurnが同じtickの場合、発生完了を先に解決する
- 複数の発生が同じtickに完了する場合、使用者のTie Priority順に解決する

未決事項:

- 発生開始と不発をどのBattle Eventとして公開するか

## 硬直

- 既存の`TurnCost`を`Recovery`へ改名する
- 効果解決後から次のTurnまでの時間とする
- Speedによる軽減対象とする
- 最低値は1tickとする

```text
StartupWork = BaseStartup × SkillStartupMultiplier
RecoveryWork = BaseRecovery × SkillRecoveryMultiplier
```

## tickの端数処理

- 発生、硬直、CD、状態の効果時間は、途中計算で端数を維持する
- 完了tickを確定するときに切り上げる
- 正の計算結果は最低1tickとする
- Base値が明示的に0の場合は最低値を適用せず、0tickのままとする

発生と硬直を合計する必要がある場合だけ、計算値として`TotalActionTicks`を使用する。調整項目としては保持しない。

## Skill固有補正

- Skill固有のTiming MultiplierはPhase開始時の作業量へ乗算する
- 発生・硬直・CDへ個別のMultiplierを渡せる
- `電光石火`ではWindから同じ軽減Multiplierを計算し、硬直とCDへ適用する
- UIや順序判定用の完了tickは、残り作業量と現在Statから予測して切り上げる

## Event順序

Passiveや状態が同じダメージイベントへ反応する場合の共通優先順位は未決定。

実装前に最低限、次を区別する。

```text
ダメージ計算前
ダメージ確定後
HP反映後
戦闘不能判定後
追加ダメージ生成後
```

## 対象確定

- `先頭`など位置を参照する通常Skillは、効果解決時に対象を検索する
- 対象指定不可のPachimonは、単体・全体・ランダムの対象候補から原則除外する
- 効果解決時に対象が存在しない場合、`対象指定不可による不発`として記録する
- 不発時は`対象がいなかった！`と表示し、MN・硬直・CDは通常どおり処理する
- Skill選択時の個体を追跡する特殊Skillだけ、Skill Contextへ対象を保存する
- 保存した対象が効果解決時に対象指定不可なら、別対象へ切り替えず不発とする

## tick内の共通更新順

1. 現在のStatus・Field Effectを反映したStatで行動時計とCooldownを進める
2. Statusの定期効果を処理する
3. StatusのValue・残り時間を減少させ、終了・遷移を処理する
4. Field Effectの定期効果を処理する
5. Field EffectのValue・残り時間を減少させ、終了・遷移を処理する
6. WeatherのValueを減少させ、終了を処理する
7. 完了した発生・Turnを解決する

Field Effectがこのtickに付与したStatusは、次のtickから時間・Value減少の対象になる。
