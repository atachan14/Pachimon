# Battle Presentation Timeline

Status: `Implemented (foundation)`

Battleの計算順と、Dialog・Gaugeでプレイヤーへ見せる順序を一致させるための仮仕様。

## 目的

- Passive発動、Damage、回復、HP/MN増減を発生順に表示する
- 1回の再発動内で起きるGauge変化を、Block開始時にまとめて再生する
- Battleの正否をUIの再生速度やAnimationへ依存させない
- Previewは従来どおり、行動完了後の合計変化を表示する

## 現状の問題

現状の`SkillResolution`は最終的な`SkillEffectResult`一覧を返し、UIは最終BattleStateを描画してからLogを順番に表示する。

この構造では、複数回攻撃やPassive発動の途中経過に合わせて、HP/MN Gaugeを1回ずつ減らせない。

## 基本方針

Battle処理は状態更新と同時に、UI再生専用の順序付き`BattlePresentationStep`を記録する。

UIはBattleStateを再計算せず、記録済みStepを先頭から再生する。計算用の`BattleEvent`と表示用の`BattlePresentationStep`は分離する。

```text
BattleEvent
  PassiveやStatusが戦闘ルールへ反応するためのイベント

BattlePresentationStep
  計算済みの結果をDialogとGaugeで再生するための記録
```

## BattlePresentationStep案

1つのStepは次の情報を保持できる。

- 表示テキスト
- 発生源Unit
- 対象Unit
- 発生元のSkill / Passive / Status ID
- HPのBefore / After
- MNのBefore / After
- Damage属性とDamage量
- Step種別
  - `SkillStarted`
  - `PassiveTriggered`
  - `DamageApplied`
  - `RecoveryApplied`
  - `StatusChanged`
  - `UnitDefeated`

HPとMNの変化を同じStepへ含められる構造にする。これにより、再発動時のMN減少と敵へのDamageを同時に演出できる。

## Dialog構造

`Page / Block / Line`はBattle固有ではなく、他Nodeでも利用できる汎用UI構造とする。

```text
DialoguePage
  1回のTurn全体

DialogueBlock
  Skillの初回発動または再発動1回分
  Block末尾でクリック待ち

DialogueLine
  Block内の個別メッセージ
  表示枠内に収まる間は止めずに次のLineへ進む
```

Blockが表示可能行数を超えた場合、最初の表示枠以降はクリックごとに1行送る。文字送り中のクリックでは、現在表示中の範囲を最後まで表示する。

## 燃焼の再生順

初回使用時のMNはSkill決定時に消費する。HP/MN Gaugeは各Blockの先頭Lineが表示された時点で、そのBlock内の最終値までまとめてアニメーションする。

```text
Passive発動Step
敵へのDamageStep + 敵HP減少
自身へのDamageStep + 自身HP減少

Passive発動Step
敵へのDamageStep + 敵HP減少 + 使用者MN減少
自身へのDamageStep + 自身HP減少

Passive発動Step
敵へのDamageStep + 敵HP減少 + 使用者MN減少
自身へのDamageStep + 自身HP減少
```

内部記録では各変化を発生順に保持し、UI変換時に同じBlockの変化をUnit単位で集約する。再発動できない場合は次のBlockを生成しない。

## Passiveのタイミング

Damage計算へ影響するPassiveは、UI処理をDamage Calculatorへ直接差し込まない。

1. Damage Contextを作る
2. Damage計算前Eventを発行する
3. PassiveがDamage値を変更し、実際に発動した場合は`PassiveTriggered` Stepを記録する
4. 最終Damageを適用する
5. `DamageApplied` Stepを記録する
6. Damage適用後Eventを発行する
7. 反撃などが発動した場合は、続くStepとして記録する

同じタイミングで複数Passiveが発動する場合に備え、将来はPassive Priorityと同Priority時の安定した並び順を定義する。

## 自傷攻撃

- 自身を発生源かつ対象とした`IsAttack = true`のDamageとして扱う
- 攻撃側のDamageBonusと与ダメージPassiveを適用する
- 防御側の属性値、ResistBonus、被攻撃Passiveを適用する
- 再帰発動を禁止したいPassiveは、Passive固有の発動条件でOriginや発生源を判定する

## 実装状況

- `BattlePresentationStep`と`BattlePresentationRecorder`を実装済み
- DamageとMN消費のBefore / After記録を実装済み
- `SkillResolution`へ順序付きTimelineを接続済み
- `BattleScreen`はStepをTurn単位のPageと再発動単位のBlockへ変換する
- 各Blockの先頭Lineで、そのBlock内のHP/MN Gauge変化をまとめて更新する
- `LogWindowView`が汎用`DialoguePage / DialogueBlock / DialogueLine`を再生する
- 現在のPassive発動テキストは`BattleState.AddLog`からStepへ記録する
- PreviewはPresentationを再生せず、従来どおり合計変化を表示する

## 今後の拡張

- Passive ID・所有者・表示名を持つ専用`PassiveTriggered`記録APIへ移行する
- 回復、Status付与・解除、戦闘不能復帰を専用Stepへ接続する
- 同時発動PassiveのPriorityを明文化する
- Left / Right PaneをStep再生へ同期するか、行動終了時更新を維持するかUX確認後に決定する
