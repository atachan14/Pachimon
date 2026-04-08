# DefinitionTable Plan

このファイルは、`DefinitionTable を先に固める` 方針の作業順メモとする。

## 結論
`MapGenerator` より先に `DefinitionTable` を作る。

理由:
- Map 生成時に node 内容を事前確定したい
- `row:0` の初期候補にも pachimon 情報が必要
- 後から battle や reward をつないでも、定義データの入口がぶれにくい

## 0. まず揃える認識
- `.csv` は編集用の元データ
- `DefinitionTable` は Unity 側の受け皿
- `PachimonDefinitionTable を確定する` とは
  - CSV の列構成を決める
  - Unity 側でどう受けるかを決める
  の両方を指す

## 1. 先に決めること
### 共通
- `id` 命名規則
- `name` / `description` の持ち方
- 列名は安定した機械用キーにする
- importer は列名ベースで読む

### Pachimon
- `fixedSkillId`
- `initialRandomSkillPool` をどう表すか
- `weight_*`
- `favoriteAttribute`

### Mod
- 全パーティに適用する前提で持たせる列

### Trainer / GymLeader
- map 生成に必要な最小列だけに絞る

## 2. まず作る DefinitionTable
1. `PachimonDefinitionTable`
2. `ModDefinitionTable`
3. `TrainerDefinitionTable`
4. `GymLeaderDefinitionTable`
5. `GlobalStatGainTable`

## 3. Skill と Passive の扱い
現時点では、`SkillDefinitionTable` と `PassiveDefinitionTable` は保留でよい。

### Skill
- 固有パラメータが多い
- 一覧調整の価値が低い
- 主体は C# の定義 + Logic に寄せる

### Passive
- `passiveId` と `pachimonId` が一致する前提がある
- 他の可変項目をほぼ持たない
- 主体は Logic 実装になりそう

ただし、後で
- 名称や説明を個別表示したい
- データ項目を増やしたい
- 付け替えや独立参照をしたい

となったら追加する。

## 4. 実装順のおすすめ
1. schema を決める
2. CSV サンプルを確定する
3. DefinitionTable asset の器を作る
4. 読み込み入口を作る
5. MapGenerator がそれを使う

## 5. 完了条件
- `MapGenerator` が必要な定義データへアクセスできる
- `row:0` 初期候補を定義から生成できる
- `RunState.party` に選択結果を反映できる
