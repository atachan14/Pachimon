# v0.3: Battle

v0.3の完成目標は、Battle / Gym / Elite Nodeで3対3の戦闘を開始し、Skill選択、Tick進行、Damage、Passiveイベント、勝敗、BattleLogを通して動かせる状態にすること。

## Status

要件整理中。共通Battle Stateと最小Skillから本実装へ進む。

## スコープ

- Player / Enemy各3体が同時に参加するBattle
- Battle専用の可変State
- Run中に持ち越すPlayer CurrentHP / CurrentMN
- 共通TickとTurn順
- Speed
- PlayerのSkill選択
- Enemyの仮AI
- 8属性の基本Skill
- True DamageのSystem Skill「わるあがき」
- Skill Logicによる自動対象決定
- Passive用Battle Event Dispatcher
- Battle開始演出
- BattleLog
- 勝利 / 敗北判定

## v0.3では完成させないもの

- Battle Rewardの取得処理
- Item使用
- 戦略的なEnemy AI
- 戦闘中の手動対象選択
- 戦闘中のParty並び替えと交代
- Stage / rowに応じたSpeed補正
- Skill / Passiveの本番コンテンツ追加
- 最終版の演出時間、SE、BGM、画面効果

Rewardはv0.4で実装する。v0.3の勝利後はRewardへ接続可能なBattle Resultを作り、仮進行でNodeを完了できるところまでを対象とする。

統合属性、MN、Speed、DamageBonus / ResistBonusへの移行手順は[`../v0.6.5/stat-refactor-plan.md`](../v0.6.5/stat-refactor-plan.md)を参照する。

## 確定方針

- MNはHP同様にRun中を通して保持し、Battle開始時にリセットしない
- 現行の基本SkillはMNを消費しない
- Turn開始時は直接Skill一覧を表示する
- 使用可能Skillがない場合、Enemyは自動で「わるあがき」を使用し、Playerは専用Buttonの入力を待つ
- Skillの対象はLogicが自動決定し、対象選択UIは持たない
- Party順をFormationとして扱う
- Player入力待ち、BattleLog、演出中はTickを進めない
- Battle中の可変Stateを`PachimonInstance`へ直接混在させない

## 読む順番

1. [`battle-flow.md`](./battle-flow.md)
2. [`battle-state.md`](./battle-state.md)
3. [`skill-runtime.md`](./skill-runtime.md)
4. [`passive-events.md`](./passive-events.md)
5. [`implementation-plan.md`](./implementation-plan.md)
