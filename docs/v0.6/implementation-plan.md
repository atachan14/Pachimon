# v0.6 Implementation Plan

## Phase 0: Skill Slot

1. [完了] Skill所持をSlot単位で識別する
2. [完了] 同一Skillの複数所持を許可する
3. [完了] CooldownをSkill ID単位からSlot ID単位へ変更する
4. [完了] Rewardで同一Skillを再取得できるようにする
5. [完了] Map生成時のSkill重複禁止は維持する

## Phase 1: Item Domain

1. [完了] Item ID、Item Instance、Inventoryを作る
2. [完了] 最大6SlotとSkill重複禁止を実装する
3. [完了] Item Logicと対象ルールを分離する
4. [完了] Run用とBattle用の使用Contextを作る
5. [完了] `きずぐすり`のLogicを実装する

## Phase 2: Item Panel

1. [完了] HeaderへItem開閉操作を接続する
2. [完了] 3x3のItem Panelを作る
3. [完了] MapやPaneと競合しない前面管理へ接続する
4. [完了] ExpandedのClick詳細表示を作る
5. [完了] CompactのLong Press詳細表示を作る

## Phase 3: Drag & Drop

1. [完了] Item SlotをDrag Sourceにする
2. [完了] MainPane、LeftPane、RightPaneへDrop Targetを設ける
3. [完了] 対象PachimonのInstance IDをDrop結果へ渡す
4. [完了] 無効Drop時の非消費とPanel復帰を実装する
5. [完了] 成功時に効果適用、Item消費、各Pane更新を行う

## Phase 4: Battle接続

1. [完了] Player Skill入力待ち中だけItemを使用可能にする
2. [完了] BattleUnitStateへ効果を反映する
3. [完了] Item使用後も同じPlayer入力待ちを継続する
4. [完了] Battle終了時のRun側同期を確認する

## Phase 5: 事前Enemy Item

1. [完了] RightPaneのEnemy表示からInstance IDを取得できるようにする
2. [完了] 生成済みの全Battle / Gym / Elite Nodeを対象可能にする
3. [完了] `PachimonInstance`へ事前効果を適用する
4. [完了] Battle開始時に事前効果を引き継ぐ
