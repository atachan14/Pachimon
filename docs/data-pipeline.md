# Data Pipeline

このファイルは、DefinitionTable の編集元と Unity 取り込みフローを整理するメモとする。

## 結論
- 正本は `Google Sheets`
- Unity 取り込み形式は `CSV`
- 最初は手動エクスポート
- 必要になったら Google Sheets からの自動取得を追加する

## 目的
- 一覧編集しやすい形で定義データを管理する
- Unity 側では安定した形式で取り込む
- 実装初期に運用コストを上げすぎない

## 基本フロー
1. Google Sheets で定義データを編集する
2. シートを `CSV` としてエクスポートする
3. `Assets/Definitions/SourceCsv/` に配置する
4. Unity Editor 上で importer を実行する
5. DefinitionTable asset を更新する

## なぜ Google Sheets を正本にするか
- 一覧比較しやすい
- 検索とフィルタがしやすい
- 複製が楽
- 数値調整がしやすい
- 履歴管理が使いやすい

## なぜ Unity には CSV で入れるか
- importer 実装が単純
- Unity 側で扱う入力形式を固定できる
- 取り込み失敗時の切り分けがしやすい
- Git に載せやすい

## 初期運用
### 推奨
- Google Sheets で編集
- 手動で CSV エクスポート
- Unity に取り込む

### この段階でやらないこと
- Google API 認証
- 自動同期
- エディタ起動時の自動更新

## 将来拡張
### 拡張案1
- 公開された Google Sheets の CSV URL から editor で fetch する

### 拡張案2
- Google Sheets API を使って editor から直接取得する

### 今のおすすめ
- まずは手動 CSV で十分
- データ更新頻度が高くなってから自動化する

## ディレクトリ案
```text
Assets/
  Definitions/
    SourceCsv/
      pachimon.csv
      global_stat_gain.csv
      skill.csv
      passive.csv
      mod.csv
      trainer.csv
      gym_leader.csv
    Generated/
      PachimonDefinitionTable.asset
      GlobalStatGainTable.asset
      SkillDefinitionTable.asset
      PassiveDefinitionTable.asset
      ModDefinitionTable.asset
      TrainerDefinitionTable.asset
      GymLeaderDefinitionTable.asset
```

## DefinitionTable ごとのおすすめ
### Pachimon
- Google Sheets 正本を強くおすすめ
- 件数が多く、一覧比較が重要

### Skill / Passive
- 件数が少ないうちは Unity 内でも可能
- ただし最終的には Google Sheets へ寄せたほうが統一しやすい

### Mod / Trainer / GymLeader
- Google Sheets にまとめて問題ない
- 調整項目は比較的シンプル

## CSV importer の最小仕様
### 入力
- 1行目は header
- 2行目以降がデータ
- `id` 列は必須

### importer の役割
- CSV を読む
- 各行を row データに変換する
- DefinitionTable asset を更新する
- `id` 重複や必須列不足を検出する

### importer でやるべき検証
- `id` が空でない
- `id` が重複していない
- 数値列が数値として読める
- 参照IDが存在する

## Google Sheets の列設計方針
### 共通ルール
- 1列目は `id`
- 表示名は `name_ja` `name_en`
- 参照は名前ではなく `id`
- 複数値は区切り文字で持つか、別シートへ分ける

### 複数値の扱い
#### 方式A
- 1セルに `fire_bite|leaf_shield`

向いている:
- 初期skill2つ
- 参照数が少ない項目

#### 方式B
- 別シートで中間テーブルを持つ

向いている:
- 将来的に数が増える関連
- 並び順を厳密に持ちたい関連

### 今のおすすめ
- 初期は方式A
- 後で複雑になったら方式Bへ移行

## Pachimon シートの列案
- `id`
- `name_ja`
- `name_en`
- `description_ja`
- `description_en`
- `favorite_attribute`
- `initial_skill_ids`
- `unique_passive_id`
- `weight_hp`
- `weight_mn`
- `weight_mnreg`
- `weight_fire`
- `weight_water`
- `weight_leaf`
- `weight_electric`
- `weight_poison`
- `weight_earth`
- `weight_ice`
- `weight_dragon`
- `weight_speed`
- `weight_skillheist`
- `weight_pierce_bonus`
- `weight_threat`
- `weight_break`
- `weight_crit_rate`
- `weight_crit_damage`

### 補足
- ステータス抽選は `weight_*` に従って行う
- gain値そのものは全Pachimon共通の `GlobalStatGainTable` 側で管理する
- `draw_count` は Pachimon シートではなく Map生成側で管理する

## GlobalStatGain シートの列案
- `stat_id`
- `gain_value`

## Skill シートの列案
- `id`
- `name_ja`
- `name_en`
- `description_ja`
- `description_en`
- `base_turn_cd`
- `base_skill_cd`
- `mana_cost`
- `attribute`
- `logic_id`

### 補足
- `logic_id` は基本的に `id` と同じでよい
- 将来まとめたい場合だけ分ける
- `mana_cost` は数値なら固定消費として解釈する
- `mana_cost` に文字列が入っている場合は特殊仕様として Logic 側で解釈する

## Passive シートの列案
- `id`
- `name_ja`
- `name_en`
- `description_ja`
- `description_en`
- `logic_id`

### 補足
- triggerや発動条件は Logic 側で持つ

## Mod シートの列案
- `id`
- `name_ja`
- `name_en`
- `stat_list`
- `ratio_list`

## Trainer シートの列案
- `id`
- `title_ja`
- `title_en`
- `favorite_attribute`
- `gold_min`
- `gold_max`

## GymLeader シートの列案
- `id`
- `favorite_attribute`
- `gold_min`
- `gold_max`

## GraphicTable.asset 方針
### 基本
- CSV 側では画像参照を持たない
- Unity 側の `GraphicTable.asset` で `id -> 実参照` を解決する

### 例
- `PachimonGraphicTable.asset`
  - `id`
  - `front`
  - `back`
- `TrainerGraphicTable.asset`
  - `id`
  - `graphic`
- `GymLeaderGraphicTable.asset`
  - `id`
  - `graphic`
- `ModGraphicTable.asset`
  - `id`
  - `icon`

### 理由
- CSV 側で path を管理しなくてよい
- Unity 上で参照切れを見つけやすい
- Skill / Passive のような可変個数の演出参照とは分離しやすい

## Logic との接続
### Skill
- `SkillDefinitionTable.id`
- `SkillLogicRegistry`
- `SkillLogic`

### Passive
- `PassiveDefinitionTable.id`
- `PassiveLogicRegistry`
- `PassiveLogic`

## 実装初期のおすすめ
1. `pachimon.csv`
2. `global_stat_gain.csv`
3. `skill.csv`
4. `passive.csv`
5. `mod.csv`

ここまで作れればかなり前に進める

## TODO
- 区切り文字を `|` にするかどうか
- GraphicTable.asset の editor 管理方法
- CSV importer のエラーメッセージ方針
