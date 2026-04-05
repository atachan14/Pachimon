# PachiMon（パチモン）仕様まとめ v0.1
初期ドラフトを残したものであり、変更点は記載しない。
他のファイルとの不整合に関しては他のファイルの仕様を優先する。

## 概要

PachiMonは以下の特徴を持つローグライク戦術ゲーム

3vs3フォーメーション戦闘
tickベース完全ターン制
属性倍率型ステータス構造
スキル構築型ローグライク
敵mod取得ビルド形成
ジム突破型進行構造
ghost（過去プレイヤー）非同期対戦
ブラウザ版先行公開 → Steam版展開予定

## 戦闘システム
### 基本構造
3vs3戦闘
各パチモンが独立したTurnCDを持つ
TurnReadyになった個体のみ行動可能
1ターンにつき1スキル使用
各SkillもSkillCDを持つ
Speedが高いほど行動頻度が上昇
Speedが高いほど連続行動が可能

### tick制ターン管理

内部時間は整数tickで管理

管理対象

- GlobalTick
- TurnReadyTick
- SkillReadyTick

SpeedによってTurnCDが短縮される

例

nextTurnTick = now + baseTurnCD / (1 + Speed / 100)

## 編成
### 初期編成

ゲーム開始時

同一プールから3体選択

共通プール対象

- プレイヤー
- 通常敵
- ジム
- 四天王
- ghost

### フォーメーション

配置

敵

[敵1][敵2][敵3]

味方

[味方1][味方2][味方3]

対象選択なし

単体攻撃

基本的に敵の先頭対象

範囲攻撃

- 全体攻撃
- 貫通攻撃
- スキル依存

死亡時

前詰め処理

例

味方1死亡 → 味方2が前へ移動

## 属性システム

属性はタイプとしては存在しない

代わりに属性ステータスを持つ

属性は8種類

（仮）
- fire
- water
- leaf
- electric
- poison
- earth
- ice
- dragon

属性値は攻撃と防御を兼ねる

## 属性倍率システム

攻撃倍率

damage倍率 = 1 + attacker_attribute / 100

例

attribute = 100 → 2倍
attribute = 200 → 3倍
attribute = 300 → 4倍

防御軽減

damage軽減率 = attribute / (attribute + 100)

例

attribute = 100 → 50%軽減
attribute = 200 → 66%軽減
attribute = 300 → 75%軽減

最終ダメージ計算例

final_damage = base_damage × (100 + attacker_attribute) / (100 + defender_attribute)

## 属性値構造

属性値は

初期値ランダム
- 重み付きランダム配分
- 上限なし
- 加算型スタック

## スキルシステム

### スキルスロット

装備可能数

9スロット（3×3）

取得スキルが9を超える場合、そのスキルは習得できない。

### multi-scalingスキル

複数属性参照可能

例

fire × 0.8 damage
wind × 0.2 speed buff

## Passive

構造変化系能力

例

初回SkillCD短縮
shield時speed増加
DoT延長
row buff
条件付き倍率強化

取得方法

通常敵撃破

## Modシステム

通常敵は1つのmodを持つ

撃破すると取得可能

例

speed +12
fire +18
wind +9

特徴

スタック可能
行が進むほど強化
map上で事前確認可能

報酬取得時

強化対象パチモンを1体選択

## 報酬システム

通常戦闘報酬

以下から取得

スキル
mod
passive

取得時

対象パチモンを1体選択して付与

## スキル取得仕様

取得元

撃破したパチモンの所持スキルからランダムな選択肢

→ 選択肢から任意のスキルを選択


## マップ構造

### 通常area
ノード種類

- 通常
- ジム
- パチモンセンター（回復）
- ショップ

分岐あり

行数

約32行

#### ジム

役割

badge取得

四天王挑戦条件

badge 8個

badgeは属性強化予定

例

leaf badge → leaf強化
fire badge → fire強化

### 四天王area

ノード種類

- 四天王

役割

最終試験

特徴

高倍率補正
報酬なし

### ghostarea

ノード種類
- ghost


## 敵生成

敵生成構造

- 種族ランダム
- 属性重み付き配分
- 行難易度補正
- スキル数補正
- mod付与

行が進むほど

- 属性値増加
- スキル数増加
- mod強化


## ghostシステム

プレイヤー死亡時

ghost保存

保存内容

- username
- time
- pachimons
    - stats
    - skills
    - passives
    - 出現位置
    - 並び順
   
以降のプレイヤーのmapに出現

殿堂入り時

runデータ削除

ghostのみ保持

## セーブ仕様（ブラウザ版）

保存内容

- username
- token

一致

continue

未存在

new game

殿堂入り

セーブ削除

ghost保持

## UI構造

ターン開始時表示

- たたかう
- アイテム

たたかう選択時

6スキル表示

配置

3×2グリッド表示

## アイテム

現状仕様

消費型のみ予定

詳細未確定

## 公開計画

公開順

ブラウザ版
↓
Steam版

ブラウザ版目的

- バランス調整
- ghost生成
- プレイテスト

Steam版

完成版リリース