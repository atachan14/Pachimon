# Content Schema

`firstDocument.md` はアーカイブとして残し、このファイルを現行のコンテンツ定義メモとする。

## 目的
- 生成前データの構造を整理する
- 実行時データと元データを分けて考えられるようにする
- Unity実装時のScriptableObjectや保存データの土台にする

## データの分け方
### 生成前データ
- まだrunに存在していない定義データ
- マスターデータとして保持する
- 基本は `DefinitionTable` として保持する

### 処理データ
- 定義データとは別に、個別挙動を C# 側で保持する
- 主に skill / passive の発動処理や条件判定を担当する

### 紐づけ
- `id` を使って Definition と Logic を紐づける
- battle中は `id -> Logic` を引く仕組みを使う

### 実行時データ
- run中に生成された個体データ
- 成長、取得報酬、並び順などを保持する

## Pachimon DefinitionTable
### 役割
- 生成前のパチモン定義

### 保持したい項目
- id
- name
- ステータスweight
- 初期skill2つ(固定)
- ユニークpassive1つ(固定)
- 説明文

### ステータスweight
- ランダム生成時に利用する
- ステータス抽選時の選ばれやすさを表す
- gain値そのものは全Pachimon共通のテーブルとして別管理する
- 抽選回数や総量は Map生成側の row / node種別 / 難易度側で管理する

### 共通gain
- 全Pachimon共通で使うステータス増加量テーブルを別管理する
- 抽選で選ばれたステータスに対応する `gain` 値だけ上昇させる
- `draw_count` は PachimonDefinition ではなく Map生成側が持つ

### グラフィック
- CSV では保持しない
- Unity側の `PachimonGraphicTable.asset` で `id -> front/back` を解決する

## Pachimon Instance
### 役割
- run内で実際に保持される個体

### 保持したい項目
- instanceId
- masterId
- 現在HP
- 最大HP
- 現在MN
- 最大MN
- MN回復量
- 属性値
- speed
- skillheist
- 貫通ボーナス
- 脅威
- 破壊
- crit倍率
- critチャンス
- ダメージ倍率
- ダメージ軽減倍率
- 所持skill一覧
- 所持passive一覧
- 所持mod一覧
    > mod情報は適用時に廃棄し、適用後のステータスのみを保持するほうが楽かも。
- 隊列index
    > 親の配列順での管理のほうが楽かも。(pachimonlist)

## Skill DefinitionTable
### 役割
- skillの共通定義データ

### 保持したい項目
- id
- name
- baseTurnCD
- baseSkillCD
- manaCost
- 属性
- 説明文

### 方針
- 共通項目は DefinitionTable に持たせる
- 発動処理や条件判定は個別の C# Logic に持たせる
- `id` を使って Definition と Logic を紐づける
- 画像や演出参照も基本は Logic 側で持たせる
- `manaCost` は数値なら固定消費、文字列なら特殊仕様として解釈する

### Logic 側へ寄せるもの
- 対象
- baseDamage
- 参照属性
- 係数
- 効果種別
- 付属効果
- 複数主効果の解決順
- 特殊なCD計算
- 特殊なMN仕様

### 付属効果候補
- 貫通
- クリティカル
    > critチャンスに改名
- 状態異常
- バフ
- デバフ

### Skill Logic
### 役割
- skillごとの個別処理を担当する C# コード

### 担当すること
- 発動条件判定
- 対象確定
- 効果解決
- ダメージやシールド計算
- 特殊CD計算
- 複数主効果の解決

### 方針
- 1skillに複数の主効果を持たせる
- 極端に特殊なskillも Logic 側で吸収する
- 演出用パラメーターは必要に応じて DefinitionTable 側に持たせる

### Skill Logic Registry
### 役割
- `skillId` から対応する Skill Logic を取得する
- battle側が `id` を渡すだけで処理を呼べるようにする

## Passive DefinitionTable
### 役割
- passiveの共通定義データ

### 保持したい項目
- id
- name
- 説明文

### 方針
- triggerや条件判定も Logic 側に持たせる
- 効果本体は個別の C# Logic に持たせる

### Passive Logic
### 役割
- passiveごとの個別処理を担当する C# コード

### 担当すること
- trigger監視
- 発動条件判定
- 効果解決
- battleイベントへの応答

### Passive Logic Registry
### 役割
- `passiveId` から対応する Passive Logic を取得する

## Mod DefinitionTable
### 役割
- 通常戦などで取得する強化定義

### 保持したい項目
<!-- - id
- name
- 対象ステータス
- 加算値
- 表示用説明 -->
Modはステータスの変動のみなので
生成後は対象ステータスと値とアイコンのみを保持すれば十分そう
また、値は範囲付きランダムかつ、行に応じて（敵に適用時のみ）増加されるので、ModMasterとは別でステータスに応じた値の範囲のみをテーブルで保持し、
ModDefinitionTableではそのテーブルを使用したデータを保持するのはどうか

Modテーブル
- 対象ステータス(1つ)
- 値の範囲
(例:
fire:20~30
water:20~30
最大HP:200-300
)

ModDefinition
- id
- name
- icon
- [対象ステータス,パーセント]のリスト
(例1:
id:1
name:fire
icon:.png
list:[fire,100%]

例2:
id:x
name:allElement
icon:.png
list:[fire,12.5][water,12.5]...
)

## Trainer DefinitionTable
### 役割
- 通常敵トレーナーの定義

### 保持したい項目
- id
- 肩書
- name
    > 生成時にnameプールからランダムに設定するため要らない
- 得意属性
- goldレンジ
- 出現ルール
    > 全て同等にランダムなので要らない

### グラフィック
- CSV では保持しない
- Unity側の `TrainerGraphicTable.asset` で `id -> graphic` を解決する

## GymLeader DefinitionTable
### 役割
- ジム戦の定義

### 保持したい項目
- id
- name
    > 生成時にnameプールからランダムに設定するため要らない
- 得意属性
    > badge属性と一致するため、favorite_attribute のみ持てばよい
- goldレンジ
- 固有ルール
    > 無い

### グラフィック
- CSV では保持しない
- Unity側の `GymLeaderGraphicTable.asset` で `id -> graphic` を解決する

## Enemy Generation Rule
<!-- ### 入力
- 現在area
- 現在行
- ノード種別

### 出力
- 敵パチモン3体
- スキル数
- mod
- 難易度補正

### 現在メモ
- 種族ランダム
- 属性重み付き配分
- 行難易度補正
- スキル数補正
- mod付与 -->

Map生成時に全ての敵や補正を配置するため、MapGenerationに移行したほうが良さそう。

## Reward Schema
### 報酬種別
- 能力
    - skill
    - mod
    - passive
- gold
- badge

### 保持したい項目
<!-- - rewardType
- sourceType
- candidateCount
- selectableCount -->
ちょっとよくわからない。
報酬は全て受け取ることができるから、受け取った後に保存する情報はなさそう。


## Ghost Data
### 役割
- GhostAreaでの敗北run(or GhostArea全走破run)の非同期対戦データ

### 保存候補
- username
- time
- map出現位置
- pachimonlist
    - index1
        - type（種族ごとのグラフィックデータ等をMasterから持ってくる）
        - stats
        - skills
        - passives
    - index2...

## Save Data
### 最小構成候補
- username
- token
- runSeed
- 現在位置
- 所持パチモン
- 進行状況

## Unity実装メモ
### DefinitionTable向き
- Pachimon DefinitionTable
- GlobalStatGainTable
- Skill DefinitionTable
- Passive DefinitionTable
- Mod DefinitionTable
- Trainer DefinitionTable
- GymLeader DefinitionTable

### Logic向き
- Skill Logic
- Passive Logic
- Skill Logic Registry
- Passive Logic Registry

### 実行時データ向き
- Pachimon Instance
- BattleState
- RunState

## TODO
- 各ID命名規則
- ローカライズ文言の持ち方
    > 案1. それぞれの[name]等の表示項目を[jp_name][en_name]みたいに持たせる
- グラフィック参照方法
    > GraphicTable.asset で `id -> 参照` を解決する方針
- セーブデータのversion管理

## 2026-04-07 �ύX����
### ����skill�\��
- �e pachimon �͌Œ� skill �� 1 ����
- ���� skill ���v�� 3 �Ƃ���
- �c�� 2 �� row:0 �� StartNode �ŁA���� run �p�� skill �ꗗ���烉���_���Ɍ��肷��

### Skill�Ɋւ���⑫
- �g�p�\ skill �� 1 ���Ȃ��ꍇ�� `��邠����` ���g�p����
- `��邠����` �� DefinitionTable ��̒ʏ� skill �Ƃ͕ʘg�̓���s���Ƃ��Ĉ����Ă悢

### Mod��V
- mod ��V�́A�I������ 1 �̂ł͂Ȃ��S���� pachimon �ɓK�p����Ă��̗p���Ƃ���
- �ΏۑI�����K�v�Ȃ̂� skill / passive �݂̂Ƃ���

### Reward��item
- item �����͌�񂵂ł悢
- �����������I�ɂ́A����v�[�����烉���_���� item �� 1 �A����� reward �ɕK��������Ă�����
- item �� Header ����g�p����O��Ƃ���
