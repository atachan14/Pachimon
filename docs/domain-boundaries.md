# Domain Boundaries

このファイルは、実装前に `誰が何を持つか` を固定するための整理メモとする。

## 目的
- `DefinitionTable` と `RunState` の責務を混ぜない
- `MapGenerator` が何を入力にして何を返すかを明確にする
- `row:0` 初期選択を入れても崩れにくい構造にする

## 0. 用語
### CSV
- Google Sheets からエクスポートして作る編集用データ
- 列の増減や試行錯誤をしやすい元ファイル

### DefinitionTable
- CSV を Unity 用に取り込んだ後の定義データの器
- 実装上は `DefinitionTable.asset` や、それに相当する runtime 参照入口を指す

### Definition
- run をまたいで共通の静的データ
- CSV と DefinitionTable から供給する

### Instance
- 1run の中で生成される個体データ
- HP / MN / skill構成 / passive など、進行で変わる値を持つ

### RunState
- 1run 中の進行状態そのもの
- player の現在パーティもここに入れる

### RunContext
- `RunState` と `RunMap` と controller 群を束ねる実行時コンテナ

## 1. CSV と DefinitionTable の関係
1. Google Sheets で編集する
2. CSV にする
3. Unity に取り込む
4. DefinitionTable として参照する

つまり、
- `.csv` は編集用の元データ
- `DefinitionTable` は Unity 側の受け皿

`PachimonDefinitionTable を確定する` という言い方は、
- CSV の列構成を決める
- Unity 側でどう受けるかを決める

の両方を含むものとする。

## 2. DefinitionTable に入れるもの
### PachimonDefinitionTable
- `id`
- `name`
- `description`
- `favoriteAttribute`
- `fixedSkillId`
- `initialRandomSkillPool` に使う条件の元情報
- ステータス生成用の `weight_*`

### ModDefinitionTable
- `id`
- `name`
- `description`
- `icon`
- 効果量の元データ

### TrainerDefinitionTable
- `id`
- `rank`
- `goldRange`
- 出現ルールに必要な最小情報

### GymLeaderDefinitionTable
- `id`
- `favoriteAttribute`
- `goldRange`
- 出現ルールに必要な最小情報

## 3. DefinitionTable に入れないもの
- 現在HP / 現在MN
- skill の現在CD
- battle 中の一時状態
- node 解決済み状態
- map 上の現在位置

これらは全部 `RunState` または battle 用 runtime state に入れる。

## 4. Skill と Passive をどう扱うか
現時点では、`SkillDefinitionTable` と `PassiveDefinitionTable` は必須ではない。

### 理由
- 固有パラメータが多い
- 一覧調整の価値が低い
- 主体が Logic 実装になる

### 現時点のおすすめ
- `Skill` は C# 定義 + Logic で扱う
- `Passive` は `passiveId = pachimonId` 前提の Logic のみで扱う
- 後から UI 表示やデータ項目が増えたら DefinitionTable を追加する

## 5. RunState に入れるもの
### run 全体
- `runSeed`
- `gold`
- `badgeCount`
- `currentNodeId`
- `resolvedNodeIds`
- `isRunFinished`

### player データ
- `party`
- `inventory`

補足:
- このゲームでは player の所持 pachimon は `party` のみ
- `ownedPachimon` は持たない
- 最初に 3匹選んで、その 3匹で最後まで進む前提で進める

### node 進行データ
- `selectedStartPachimonIds`
- `resolvedRewardIds`
- `openedCityState`

## 6. PachimonInstance に入れるもの
- `instanceId`
- `definitionId`
- `currentHp`
- `maxHp`
- `currentMn`
- `maxMn`
- `stats`
- `skillIds`
- `passiveIds`

補足:
- `mod` は取得時にステータスへ即時反映する前提なら、`modIds` は持たなくてよい
- 後で「何を積んだか」を見たくなったら追加する

## 7. MapGenerator の入力
- `runSeed`
- `DefinitionProvider`
- 必要なら生成設定

## 8. MapGenerator の出力
- `RunMap`
- 各 `MapNode`
- 各 node の `NodeContent`

`MapGenerator` は player の現在HPや現在所持金を持たない。
それは `RunState` 側の責務とする。

## 9. row:0 の扱い
- `row:0` は `StartNodeContent` を持つ
- `StartNodeContent` には初期候補の pachimon 情報を持たせる
- 選択結果は `RunState.party` に反映する

## 10. 実装順のおすすめ
1. 責務境界を固定する
2. DefinitionTable の schema を決める
3. CSV を作る
4. DefinitionTable の器を作る
5. DefinitionProvider を作る
6. その Definition を使って MapGenerator を作る
7. row:0 初期選択を作る
8. その後に各 node 本実装へ進む
