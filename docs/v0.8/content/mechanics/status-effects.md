# Status Effect Mechanics

複数の属性ファイルから参照する状態の共通仕様をまとめる。

状態の表示名・説明はRuntime Instanceが参照する`BattleStatusAsset`から取得する。通常状態は共通実装を使い、同じStatus IDの内部Phaseによって表示が変わる状態は`GetDisplayName(instance)` / `GetDescription(instance)`をOverrideする。

## 記載項目

状態を追加するときは、必要な範囲で次を記載する。

```text
Value
効果時間
付与タイミング
発動タイミング
消費タイミング
再付与時
重複時
戦闘不能時
Battle終了時
解除可否
```

## 分類案

- `時限型`: 効果時間が0になると消滅する
- `消費型`: 条件を満たしたときに効果を発生して消滅する
- `スタック型`: ValueまたはStack数を加算して保持する
- `遷移型`: 終了時に別の状態を付与する

分類は実装を共通化するための候補であり、全状態を同じLogicへまとめることは要求しない。

## Category

状態は、固有のStatus IDとは別に複数の`StatusCategory`を持つことができる。

- 固有の発動・更新処理は個別Status Logicへ実装する
- Slow、Stun、Leakなどの共通判定はCategoryを使用する
- CategoryはC#のクラス継承関係と一致する必要はない
- 共通SystemはCategoryに属する複数の状態をまとめて検索・集計・消費できる

例:

```text
雨による漏電
- StatusId: Leak
- Categories: Leak

凍結
- StatusId: Freeze
- Categories: Stun, RemovedByFire
```

同じCategoryに属していても、Value、効果時間、再付与、解除条件はStatus IDごとに定義する。

## 端数処理

- 状態のValueを独立した整数値として保存するときに切り捨てる
- 効果時間の途中計算では端数を維持し、完了tickを確定するときに切り上げる
- 正の効果時間は最低1tickとする
- Valueの最低保証は状態ごとに指定する
- `BaseValue × AmplificationMultiplier(Stat)`で生成するValueは原則0を許容する
- 軽減後Valueが0の`Slow / Leak`は付与しない

## 共通の時間進行

- 行動時計とCooldownを進めた後に、状態の時間・Valueを減少させる
- Slowは効果時間ではなくValueを毎tick減少させる

## 共通のライフサイクル

- 戦闘不能になった時点で、そのPachimonの状態をすべて破棄する
- 戦闘不能中は状態の定期Damage・回復・遷移を発動しない
- Battle終了時にすべての状態を解除し、Runへ持ち越さない
- 戦闘不能は通常の`BattleStatusInstance`として保持せず、`IsDefeated`を正本とする
- UIでは`IsDefeated`から疑似的な状態表示として`戦闘不能`を表示してよい

## Battle Log

- Skillが対象へ状態を付与した場合、原則として付与直後にログを表示する
- 基本形式は`対象にValueの状態名を与えた！`とする
- Valueを持たず効果時間を持つ状態は、効果時間をValue位置へ表示する
- 複数対象へ付与した場合は対象ごとに1行表示する
- 同時に複数種類を付与した場合も、状態ごとに1行表示する
- Skill以外のField EffectやWeatherによる自動付与は、個別仕様で必要な場合だけ表示する

## 次のSkillで消費する状態

- Skill選択時点で保持している消費対象Valueを記録する
- 状態自体は発生中も保持し、Stat補正とPachimonTab表示を継続する
- Skill効果解決後、選択時点で記録したValueだけを消費する
- Skill効果中に追加されたValueは消費せず、次に使用するSkillへ持ち越す
- `わるあがき`や対象不在で終わったSkillも、効果解決後に消費する
- 発生中の戦闘不能によってSkillが中断された場合は消費しない
- 現在は`火傷`がこの規則を使用し、将来は`進水式`も使用する

## 再付与Policy

- 同名効果の再付与方法はStatusごとに指定できる
- Valueは加算を既定値とする
- 効果時間は、既存と新規のうち長い残り時間を維持することを既定値とする
- 置換、時間再設定、付与元ごとの独立保持が必要なStatusは固有Policyを使用する
- 効果時間を持たず、Value自体が寿命を兼ねるStatusも許可する

### 毒素

- 表示名は`毒素`、内部Status IDは`Toxin`とする案を採用する
- 属性StatとDamage Typeは従来どおり`Poison`を使用する
- Valueの1%を毎tick小数Workへ移してDamageへ反映し、Value自体は毎tick固定値だけ減少する
- 具体仕様は[Poison Content](../poison.md#状態毒素--toxin)を参照する

## 実装済み

### 毒素

- `StatusId`: `Toxin`
- `Category`: `Toxin`
- 名前、説明、毎tick Damage Ratio、毎tick減衰値を`ToxinStatusAsset`へ保持する
- 毒素を直接付与するSkill SO、または毒素を生成するField Effect SOから`ToxinStatusAsset`を参照する

### 漏電

- `StatusId`: `Leak`
- `Category`: `Leak`
- 消費型
- 同一Status IDの再付与時はValueを加算する
- 雨による付与も同じ`Leak`へValueを加算する
- 雨が終了しても、既に加算されたValueは残る
- PachimonによるElectric攻撃だけで発動する
- `Origin = Status`のDamageでは発動しない
- 発動時は対象が持つ全`Leak`のValueを合算して消費する
- 状態欄では`漏電 Value`の形式で現在Valueを表示する

### 蓄電

- `StatusId`: `StoredCharge`
- `Category`: `Charge`
- スタック型
- Electricダメージ直前に全スタックを消費する
- 消費スタック数1つにつき、対象のElectricダメージを10%増加させる
- Electricダメージ確定後に1スタック獲得する
- 0ダメージや漏電による追加ダメージも獲得対象とする

### Slow

- 具体仕様は[Slow Statuses](../statuses/slow.md)を参照する
- `Slow`、`Paralysis`、`Chill`は共通の`SlowStatusAsset`クラスを使用し、個別のDefinition SOとして保持する

### Stun

- `StatusId`: `Stun`
- `Category`: `Stun`
- 名前、説明、Iconは`StunStatusAsset`へ保持する
- 効果時間は付与元Skillが計算し、Runtime Instanceへ保持する
- 効果中はActionGaugeの進行を停止する

### 飛行

- `StatusId`: `Flying`
- `Category`: `Untargetable`
- フライングアタックの発生開始時に付与し、発動直前に解除する
- 効果中は対象候補から除外され、Windの20%のSpeedを派生加算する

### 風化

- `StatusId`: `WindErosion`
- ResistBonusをValueだけ直接減少させる
- 再付与時はValueを加算する
- Valueは1tickにつき1減少し、0で解除する

### 無風

- `StatusId`: `StillAir`
- 初期値と全補正を計算した後の最終Windへ0倍補正を適用する
- 効果時間はセカンドウィンドから指定し、現在は200tickとする
