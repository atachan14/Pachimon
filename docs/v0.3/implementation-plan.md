# v0.3 Implementation Plan

BattleのDomainを先に完成させ、Presentationは後から接続する。各PhaseでEditMode Testまたは小さなRuntime確認を行う。

## Phase 1: Run CurrentHP / CurrentMN

1. [完了] `PachimonInstance`へCurrentHPを追加する
2. [完了] MaxHPで初期化する
3. [完了] 0からMaxHPへClampする更新APIを作る
4. [完了] Trainer単位のModifierSetとEffective Stats計算を設計する
5. [完了] MaxHP増加分だけCurrentHPも増やす更新処理を作る
6. [完了] 既存の`PachimonPreviewContent`をPane共通Snapshotとして生成する
7. [完了] Start / Right / Left Pane表示をSnapshot参照へ切り替える
8. [完了] `PachimonInstance`へCurrentMNを追加し、MaxMNで初期化する
9. [完了] MaxMN増加分だけCurrentMNも増やす

## Phase 2: Battle State

1. [完了] `BattleState / BattleSideState / BattleUnitState`を作る
2. [完了] Player / Enemy各3体をInstance IDからSnapshot化する
3. [完了] 基礎StatsへTrainer Modifierを適用して開始時Statsを作る
4. [完了] Formation検索を実装する
5. [完了] BattleUnitStateからPane用Snapshotを作る
6. [完了] Battle ResultからPlayer CurrentHP / CurrentMNをRunへ反映する

## Phase 3: Timeline

1. [完了] CurrentTickとNextTurnTickを実装する
2. [完了] SpeedのEffective Recoveryを実装する
3. [完了] Hasteを個体Statとして追加し、Cooldownへ適用する
4. [完了] Seed付きTie Priorityを実装する
5. [完了] 戦闘不能Unitを行動Queueから除外する

## Phase 4: Skill Runtime

1. [完了] Skill ContextとLogicの最小契約を作る
2. [完了] Target Queryを実装する
3. [完了] 統合属性値とDamageBonus / ResistBonusを使うDamageへ変更する
4. [完了] 8属性の基本Skillを実装する
5. [完了] ID 1-151のPlaceholderを8種のLogicへ接続する
6. [完了] True Damage版「わるあがき」を実装する

## Phase 5: Battle進行

1. [完了] Player Skill入力待ちを実装する
2. [完了] EnemyのSeed付きランダムSkill選択を実装する
3. [完了] Skill解決とCooldown設定を接続する
4. [完了] 戦闘不能と勝敗を判定する
5. [完了] 3対3をDomainだけで最後まで進行できるようにする
6. [完了] Startup予約、発生待ち、不発、Recovery予約を実装する

## Phase 6: Passive Event

1. [完了] Battle Event Dispatcherを作る
2. [完了] Battle開始 / Skill / Damage / 戦闘不能Eventを発火する
3. [完了] 8属性のSample Passiveを実装する
4. [完了] 登録順、追加Event Queue、Battle終了時破棄を確認する

## Phase 7: UI / Presentation

1. [完了] Trainerのスライドインと挑戦Logを実装する
2. [完了] Trainerのスライドアウトを実装する
3. [完了] Pachimon / HP Barのスライドインを実装する
4. [完了] Turn Unitの全所持SkillをSelectGridへ表示し、使用不可Skillをグレーアウトする
5. [完了] Player選択とBattle進行を接続する
6. [完了] HP BarとRight / Left PaneをBattle Stateへ追従させる
7. [完了] Battle EventをBattleLogへ順番に表示する

## Phase 8: Node接続

1. [完了] Battle / Gym / Elite NodeからBattle Contextを作る
2. [完了] 勝利時にBattle Resultを確定する
3. [完了] v0.3用の仮進行でNodeを完了する
4. [完了] 敗北時に敗北Log、全画面暗転、TitleScene遷移へ接続する
5. v0.4 Reward Controllerを起動できる境界を用意する

## 完成確認

1. Player / Enemy各3体が参加する
2. Speed込みのTick順が再現可能に進む
3. Player Turnで直接Skill一覧が表示される
4. Enemyが使用可能SkillをSeed付きランダムで選ぶ
5. 使用可能Skillなしの場合、Enemyは「わるあがき」を自動使用し、Playerは専用Buttonの入力を待つ
6. 攻撃側Skill指定Stat、DamageBonus、防御側属性値、ResistBonusがDamageへ反映される
7. 先頭Unitの戦闘不能後は次の生存Unitが対象になる
8. Passive EventがBattleをまたいで残らない
9. BattleLog / 演出中にTickが進まない
10. 勝利後にPlayer CurrentHP / CurrentMNがRunへ反映される
11. Battle中の一時StateはBattle終了時に破棄される
12. 敗北時にRun敗北へ進む
13. 非Battle時にもMods / Badges込みのStatsがPaneへ表示される
14. Battle中のHP / MN、Stat、CD、状態異常がPaneへ反映される
15. MaxHP / MaxMN増加時に増加量だけCurrentHP / CurrentMNも増える
