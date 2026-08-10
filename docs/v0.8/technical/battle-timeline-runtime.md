# Battle Timeline Runtime

ActionGauge、Stun、Slowを追加できるようにするためのBattle時間管理仕様。

## Unit Timing State

各`BattleUnitState`は`BattleUnitTimingState`を1つ保持する。

| Phase | 意味 |
| --- | --- |
| `InitialDelay` | Battle開始から最初のTurnまで |
| `Ready` | Skill入力または自動選択が可能 |
| `Startup` | Skill選択後、効果解決前の発生待ち |
| `Recovery` | Skill効果解決後、次のTurnまでの硬直 |
| `Defeated` | 戦闘不能 |

Timing Stateは絶対tickではなく、以下を保持する。

- `TotalWork`: Phase開始時の総作業量
- `RemainingWork`: 現在の残り作業量
- `IsPaused`: Stunなどによる行動時計の停止
- `Progress`: `1 - RemainingWork / TotalWork`

UI用の残りtickは、現在のSpeedとSlowの今後の減衰を使って都度予測する。

## 時間進行

Battle全体の`CurrentTick`を1tick進めるたび、全Unitを次の順序で更新する。

1. 現在のSpeed/Hasteから1tick分の進行量を求める
2. 発生、硬直、初期待機、Cooldownの残り作業量を減らす
3. SlowのValueを1減らす
4. Valueが0になったSlowを取り除く
5. `CurrentTick`を進める

`IsPaused`のUnitは行動時計とCooldownの両方を進めない。
状態自身の残り時間は別の時計として扱い、Stun中も進める。

## Timed Status

時間制Statusは`RemainingTicks`を保持する。永続Statusは残り時間を保持しない。

- Timelineは次のTurn、発生完了、Status期限のうち最も早いtickを予測する
- 実際の進行は1tickずつ行い、その時点のStatとStatusを参照する
- tick開始時にStun中なら、そのtickでは行動時計とCooldownを進めない
- Status期限到達後にStunがすべて消えた場合、次の区間から時計を再開する
- 複数のStunはStatus IDごとに独立した残り時間を持つ

## Stun

`Stun` Categoryを1つ以上持つUnitは`IsPaused`になる。

- 発生、硬直、初期待機を停止する
- Skill Cooldownを停止する
- Stun自身を含む時間制Statusの残り時間は進む
- すべての`Stun` Categoryが消えた時点で再開する

## Slow

`Slow` Categoryの`Value * StackCount`を合計し、Battle中のSpeedから減算する。

```text
BattleSpeed = StartingSpeed - TotalSlow
```

- Speedは負数になり得る
- Slowは時間を持たず、各tickの進行後にValueを1減らす
- Valueが0になったSlowは終了する
- 現在進行中の発生・硬直・初期待機にも即座に反映する
- 同じStatus IDのSlowを再付与した場合はValueを加算する
- 麻痺と冷気など異なるStatus IDのSlowは別々に保持し、合計する

## Statの参照タイミング

SpeedとHasteは各tickで現在値を参照する。
Skill固有Timing補正はSkill選択時に作業量へ反映する。

- Speedは発生、硬直、初期待機の1tick分の進行量を変える
- HasteはCooldownの1tick分の進行量を変える
- Phase途中のSpeed/Haste変化も次のtickから反映する
- 残り作業量自体は巻き戻さず、進行速度だけを変える

## 同tick優先順位

- Skillの発生完了とUnitのTurn開始が同tickなら、発生完了を先に解決する
- 複数Skillの発生が同tickで完了した場合は、使用者の`TiePriority`順
- 複数UnitのTurnが同tickなら、Unitの`TiePriority`順
- 同tickでTurn待ちになったUnitは、実際に順番が来るまで直前Phaseと残り`0`を維持する
- `Ready`へ変わるのは、Timelineが実際にそのUnitへTurnを渡した瞬間だけ

## Cooldown

各Skill Slotは`BattleSkillCooldownState`を保持する。

- `TotalWork`
- `RemainingWork`
- `IsReady`

UI向けのReady Tickは、必要な時点で
現在のHasteから予測して導出する。

## Preview

実処理とPreviewは共通の`BattleSkillTimingPlan`を使用する。

- `StartupWork`
- `RecoveryWork`
- `CooldownWork`

Preview結果にはTiming Planも保持する。現行UIではHP/MN差分のみ表示し、
Timing Planは将来の詳細Preview用データとして保持する。

## UI命名

Battle画面のGaugeは以下に統一する。

- `HpGauge`
- `MnGauge`
- `ActionGauge`

`HpGauge`と`MnGauge`は`Track`、`Fill`、`Preview`、`Value`を基本構造とする。

`ActionGauge`は細い線として、以下の2区間を表示する。

- `Elapsed`: 経過済み区間
- `Remaining`: 残り区間

右側の`ActionGaugeValue`へ残りtickまたは`Turn`を表示する。

Gaugeの進行はBattle Stateを書き換えず、`ActionGaugeView`が直前表示から
次の表示までを補間する。補間時間はtick差に応じて`0.12`秒から`0.8`秒とする。

`HpGauge`と`MnGauge`は`ResourceGaugeView`がFill、現在値、色を同期して補間する。
Battle StateのHP/MNは先に確定し、UI補間はゲーム進行へ影響させない。
Skill Previewの区間表示と増減値は補間中も別レイヤーとして表示する。
