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

### 実行時データ
- run中に生成された個体データ
- 成長、取得報酬、並び順などを保持する

## Pachimon Master
### 役割
- 生成前のパチモン定義

### 保持したい項目
- id
- name
- ステータス重み
- 初期skill2つ(固定)
- ユニークpassive1つ(固定)
- グラフィック
- 説明文

### ステータス重み
- ランダム生成時に利用する
- 属性値の出やすさに加え、その他ステータスを含めて制御する

### グラフィック
- 前向き表示用
- 後ろ向き表示用

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

## Skill Master
### 役割
- skillの元定義

### 保持したい項目
- id
- name
- 対象
- baseTurnCD
- baseSkillCD
- manaCost
- 属性
- エフェクトグラフィック
- 説明文

>以下は共通のパラメーターとせず、テンプレをいくつか用意し、スキルごとに個別処理として実装したほうがいいかも。（baseDamageのないスキルや、シールドや、buff,debuff、それらの複合や、その他のユニークなスキル対応）
    - baseDamage
    - 参照属性
    - 係数
    - 効果種別
    - 付属効果

### 付属効果候補
- 貫通
- クリティカル
    > critチャンスに改名
- 状態異常
- バフ
- デバフ

### 要決定
- 1skillに複数の主効果を持たせるか
    > 持たせる
    > 極端な例）
    """
    [200*(1 + fire/80)]のダメージを[対象:先頭の敵]に与え（[貫通:50% * (1 + water / 100) + 貫通ボーナス,貫通:25% * (1 + leaf / 100) + 貫通ボーナス]）、[対象:自身]は[200*(1 + elec/50)]のシールドを獲得し、[対象:後衛]に[200*(1+poison/50)]のシールドを付与する。
    このスキルのskillCDはearthに応じて減少する[skillCD = baseSkillCD / (1 + earth / 100)]。

    """
- 演出用パラメーターをここに持つか
    > 持たせる

## Passive Master
### 役割
- passiveの元定義

### 保持したい項目
- id
- name
- trigger
- エフェクトグラフィック
- 説明文

> 以下もパッシブごとに個別に処理を書くべきかも
    - effect
    - value


### trigger候補
- battle開始時
- turn開始時
- turn終了時
- 攻撃時
- ダメージ計算時
- 被弾時
- 撃破時
- 死亡時
- 〇〇属性ダメージ付与時
- 〇〇属性ダメージ被弾時
- 〇〇属性のスキルが発動したとき（敵味方問わず）
- その他たくさん

## Mod Master
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
ModMasterではそのテーブルを使用したデータを保持するのはどうか

Modテーブル
- 対象ステータス(1つ)
- 値の範囲
(例:
fire:20~30
water:20~30
最大HP:200-300
)

ModMaster
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

## Trainer Master
### 役割
- 通常敵トレーナーの定義

### 保持したい項目
- id
- 肩書
- name
    > 生成時にnameプールからランダムに設定するため要らない
- グラフィック
- 得意属性
- goldレンジ
- 出現ルール
    > 全て同等にランダムなので要らない

## Gym Leader Master
### 役割
- ジム戦の定義

### 保持したい項目
- id
- name
    > 生成時にnameプールからランダムに設定
- グラフィック
- badge
- 得意属性
- goldレンジ
- 固有ルール
    > 無い

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
報酬は全て受け取ることができて、受け取った後は保存する情報はなさそう。


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
### マスターデータ向き
- Pachimon Master
- Skill Master
- Passive Master
- Mod Master
- Trainer Master
- Gym Leader Master

### 実行時データ向き
- Pachimon Instance
- BattleState
- RunState

## TODO
- 各ID命名規則
- ローカライズ文言の持ち方
    > 案1. それぞれの[name]等の表示項目を[jp_name][en_name]みたいに持たせる
- グラフィック参照方法
    > 毎回masterから引っ張ってくるのが軽いかな？
- セーブデータのversion管理
