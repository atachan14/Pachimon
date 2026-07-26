# Battle Flow

## 全体フロー

```text
Battle Nodeを起動
  -> Battle Stateを生成
  -> Trainer開始演出
  -> Pachimon / HP Bar登場演出
  -> Battle開始イベント
  -> 次の行動Tickまで時間を進める
  -> 行動Unitを決定
     -> Player: Skill入力を待つ
     -> Enemy: 仮AIがSkillを決める
     -> 使用可能Skillなし: わるあがき
  -> Skillを解決
  -> BattleLog / 演出を再生
  -> 戦闘不能と勝敗を判定
     -> 未決着: 次の行動へ
     -> Player勝利: Battle Resultを生成
     -> Player敗北: Run敗北へ接続
```

## 参加形式

- Player / Enemyともに3体全員が最初から場に出る
- 6体がそれぞれTurnを得る
- v0.3では交代と並び替えを行わない
- 一方の3体すべてが戦闘不能になると決着する

## Tick進行

- Battle全体で`CurrentTick`を1つ持つ
- 各Unitは`NextTurnTick`を持つ
- Battle開始時は`CurrentTick = 0`とする
- Skill選択前の初回行動は`DefaultInitialBaseTurnCost = 100`へSpeedを適用して`NextTurnTick`を決める
- 2回目以降は直前に使用したSkillのTurnCostへSpeedを適用する
- 所持SkillはBattle開始時点ですべて使用可能とする
- 次の生存Unitの`NextTurnTick`まで`CurrentTick`を一気に進める
- Cooldownは残り値を毎Tick減算せず、再使用可能になる絶対Tickを保持する
- `CurrentTick >= CooldownReadyTick`ならSkillを使用できる
- Player入力待ち、BattleLog表示、演出中はTickを進めない
- Stage / rowによるSpeed補正は行わない

### Speed / Cooldown

```text
EffectiveTurnCost
= max(1, ceil(BaseTurnCost * 100 / (100 + Speed)))

EffectiveCooldown
= max(1, ceil(BaseCooldown * 100 / (100 + Haste)))
```

`BaseCooldown = 0`はHasteに関係なく、System Skill用の「Cooldownなし」として扱う。
Haste Modは生成せず、個体生成値とItem / Skill / Passiveによる補正だけを使用する。

Skill使用後、使用者の次Turnと使用Skillの再使用可能Tickを設定する。

```text
NextTurnTick = CurrentTick + EffectiveTurnCost
CooldownReadyTick = CurrentTick + EffectiveCooldown
```

## 同一Tick

- Battle開始時、Run SeedとNode情報から6体のTie Priorityを決める
- 同じ`NextTurnTick`のUnitはTie Priority順に行動する
- Tie Priorityは同じBattle入力から再現できるSeed付き乱数とする
- 実装では.NETのバージョン差を避けるため、固定アルゴリズムのBattle専用乱数を使う
- 同一Tick内で先に戦闘不能になったUnitは行動しない

## Player Turn

- 「たたかう / アイテム」の前段選択を置かない
- Turnを得た時点で所持Skillをすべて`SelectGrid`へ表示する
- 使用不可Skillは一覧へ残したままグレーアウトし、選択できないようにする
- Skill選択中はBattle進行を停止する
- 対象選択UIは表示しない
- 使用可能Skillが0件なら全所持Skillをグレーアウトし、その上へ少し大きい「わるあがき」ボタンを表示してPlayer入力を待つ
- 専用UIで決定すると、通常Skillと同じ`SubmitPlayerSkill()`経路で「わるあがき」を使用する

## Enemy Turn

- 使用可能SkillからBattle Seed付きでランダムに1つ選ぶ
- Enemy Skill抽選とTie Priorityは、同じBattle Seedから作る別々の乱数系列を使う
- 使用可能Skillが0件なら「わるあがき」を使用する
- Skillの対象はPlayerと同じSkill Logicで決める
- 戦略的な評価AIは後続工程とする

## Domain進行API

- `BattleFlowController.Advance()`は最大1回のActionだけを進める
- Player Turnは`PlayerInputRequired`を返し、Turnを保持して停止する
- `SkillChoices`へ全所持Skillと各Skillの使用可否を返す
- `SubmitPlayerSkill(skillId)`でPlayerの1Actionを解決する
- Enemy Turnは自動解決し、`ActionResolved`を返して停止する
- Playerの「わるあがき」は`RequiresStruggleConfirmation`付きの`PlayerInputRequired`として返す
- 決着Actionも先に`ActionResolved`を返し、次の`Advance()`で`BattleCompleted`を返す
- PresentationはLogと演出が終わってから次の`Advance()`を呼ぶ
- 不正なPlayer Skill入力では入力待ち状態を維持する

## 開始演出

```text
Player / Enemy Trainerがスライドイン
  -> 「[Trainer名]が勝負をしかけてきた」をBattleLogへ表示
  -> Trainerがスライドアウト
  -> Player / EnemyのPachimonとHP Barがスライドイン
  -> Battle開始
```

開始演出はPresentation側が担当し、完了通知を受けるまでBattle Tickを開始しない。メッセージをタップ送りにするか自動送りにするか、最終的な演出時間は実装確認後に決める。

v0.3では挑戦Logと各Battle Logを「おう」ボタンで送り、次の表示またはActionへ進む。TrainerとPartyのスライド中、Log送り待ち、Skill入力待ちには`BattleFlowController.Advance()`を呼ばない。

## Skill Grid

- Player Turnでは所持Skillを最大9件、3列Gridで表示する
- 使用可能Skillは黒背景、使用不可Skillは灰色で表示する
- 使用不可Skillには残りCooldown Tickを表示し、Buttonを操作不能にする
- 全Skillが使用不可でもGridは維持する
- 全Skill使用不可時はGrid前面へ少し大きい「わるあがき」Buttonを表示する
- Enemy TurnではSkill Gridを表示しない

## Battle終了

### 勝利

- Player側3体のCurrentHP / CurrentMNをRun上の個体へ反映する
- Battle中だけのStat補正、状態異常、Cooldown、Passive Stateは破棄する
- Enemy側の戦闘結果は保存しない
- v0.3では仮進行でNodeを完了する
- v0.4でBattle ResultからReward処理を起動する

## Pane同期

- Battle外のPaneはRun個体とTrainer Modifierから表示Snapshotを作る
- Battle中のPaneとHP / MN BarはBattleUnitStateから表示Snapshotを作る
- DamageやStat変化の解決後、Presentationへ更新通知を送る
- ViewはRun個体とBattle Unitのどちらを参照するか判断しない
- Battle中はAction解決ごとにLeft Pane、Right Pane、HP / MN Bar、Pachimon Graphicを同じ`BattleState`から再描画する

### 敗北

- Player側3体がすべて戦闘不能になった時点で敗北とする
- BattleLogへ`目の前が真っ暗になった...`を表示する
- `おう`でLogを送るとRunを終了し、画面全体を暗転させる
- 暗転完了後に`TitleScene`へ戻る
