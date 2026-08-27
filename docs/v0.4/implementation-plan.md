# v0.4 Implementation Plan

## Phase 1: Reward Domain

1. [完了] `BattleRewardSession`を作る
2. [完了] 4枠の取得状態を保持する
3. [完了] Gold / Mod / Badgeの反映処理を作る
4. [完了] Skill / Passiveの取得可否と付与処理を作る
5. [完了] 全取得判定を作る

## Phase 2: Reward Presentation

1. [完了] `RewardOverlayView`へRuntime UI骨格を作る
2. [完了] 上からのOpen / Close animationを作る
3. [完了] 4つのReward Buttonを作る
4. [完了] 取得Buttonの縮小Animationを作る

## Phase 3: Skill / Passive選択

1. [完了] 縦Scroll式の選択Windowを作る
2. [完了] Enemy3Columnと候補を表示する
3. [完了] Player3Columnと取得可否を表示する
4. [完了] 候補選択時の自動Scrollを作る
5. [完了] Scale Inと回転縮小Closeを作る

## Phase 4: Battle接続

1. [完了] Battle / Gym勝利時にReward Sessionを生成する
2. [完了] Enemy Loadoutから候補を導出する
3. [要更新] Player CurrentHP / CurrentMN反映後にReward Windowを開く
4. [完了] 全取得後にNodeを完了する
5. [完了] EliteはRewardなしで従来どおりNodeを完了する

## 完成確認

1. 通常Battleで4枠を取得できる
2. GymでModの代わりにBadgeを取得できる
3. Skill候補を変更してから取得先を選べる
4. Passive候補を変更してから取得先を選べる
5. Skill最大6枠を守り、同じSkillの重複取得を禁止する
6. Passive重複制約が機能する
7. 全取得前にNodeが完了しない
8. 全取得後にMapが開く
