# Map Generation

このファイルは、run 開始時に生成する map の仕様メモとする。
主軸は `NormalArea生成手順案` とし、周辺情報はその手順を理解するために必要な範囲だけ残す。

## 目的
- run 開始時に 1run 分の map を生成する
- `row:0` の初期選択を含め、node 内容を事前に確定する
- `MapGenerator` 実装時に、処理順がぶれない状態にする

## スコープ
- `NormalArea` の生成
- `EliteNode群` の生成
- `GhostNode群` の生成入口
- node 内容の事前確定

## 参照する静的データ
- [pachimon-info-table.md](./pachimon-info-table.md)
- 共通ステータス生成ルール
- 必要なら Trainer / GymLeader / Mod の静的データ

## Map全体構造
### row構成
1. `row0`: StartNode
2. `row1~4`: OpeningZone
3. `row5~35`: AdventureZone
4. `row36`: LeagueGate
5. `row37~40`: EliteNode群
6. `row41+`: GhostNode群

### Zone定義
#### StarterZone
- `row0`
- 初期パチモン選択

#### OpeningZone
- `row1~4`
- 通常 node のみ
- 序盤の安定区間

#### AdventureZone
- `row5~35`
- 通常 / ジム / センター / シティ が混在する主区間

#### LeagueGateZone
- `row36`
- 四天王挑戦判定
- 条件未達時は特殊敗北

#### EliteZone
- `row37~40`
- 四天王 node のみ

## 共通用語
### row
- map 上の進行段

### node
- player が進入する 1 マス

### edge
- node 間の接続

## 生成タイミング
### run開始時に生成するもの
- `NormalArea`
- `EliteNode群`
- 各 node の内容
- 各 battle node の敵構成
- 各 node の reward / gold / mod などの事前確定情報

### 後から生成するもの
- `GhostNode群`
- Ghost 用の配置詳細

## Seed
### 方針
- `NormalArea` は `runSeed` で確定する
- `GhostNode群` は到達時点の状態を使って別生成する

## NormalArea
### 役割
- run の主進行区間
- `row0` から `row40` までを含む

### 含まれる node 種別
- `Start`
- `Battle`
- `Gym`
- `RestSpot`
- `City`
- `LeagueGate`
- `Elite`

## NormalArea生成手順案
### 1. Node と Edge の土台を作る
1. `row0` に node を 1 つ生成する
2. `row1~35` は各 row に基本 3 node ずつ生成する
3. `row36~40` は各 row に node を 1 つ生成する
4. 必要なら `row2~35` に追加 node をランダム配置する
5. row 間を接続する
- 各 node は次 row の node へ最低 1 本接続する
- 最大接続数は 2
- edge 同士の明確な交差は避ける

### 2. シティを配置する
1. `row7 / 14 / 21 / 28 / 35` の横並び node からシティを配置する
2. シティは横並び条件を満たす node 群にのみ置く
3. node 土台と接続を先に作ってから配置する

### 3. センターを配置する
1. `row5~35` の未使用 node からランダムにセンターを配置する
2. 同種 node の連続を避ける
3. 別種類 node との連続は許可する

### 4. LeagueGate を配置する
1. `row36` を `LeagueGate` 固定にする

### 5. ジムを配置する
1. `row5~35` の未使用 node からランダムにジムを配置する
2. 同種 node の連続を避ける
3. 配置後に `badge 8 個以上取得可能なクリア可能ルート数` を将来的に検証できる構造にする
4. 条件未達ならジム配置のみやり直す

### 6. `row37~40` に EliteNode を配置する
1. `row37~40` は `Elite` 固定にする

### 7. パチモン内容を配置する
1. battle node と elite node に出現する pachimon を配置する
2. `PachimonInfoTable` を参照し、必要ならダミー id も含めて割り当てる
3. `NormalArea内（row0 を含む）で同種が重複しない` 前提で配置する
4. `row0` 用の候補 9 体もここで確定する

### 8. ステータスと初期 skill を決める
1. 各 pachimon の基礎情報を決める
- `name`
- `graphic`
- `fixedSkill`
- `passive`
2. ステータスは共通ルールでランダム生成する
3. 初期 skill は
- 固定 skill 1 つ
- `row0` 選択時に追加されるランダム skill 2 つ
の前提に合わせる

### 9. Mod と報酬を配置する
1. 必要な node に mod を配置する
2. gold 報酬を確定する
3. reward 内容を node ごとに事前確定する

## GhostNode群生成手順
1. 直近のサーバー状態を取得する
2. `row41+` の GhostNode群を生成する
3. Ghost 内容を配置する

## ノードが持つ情報
- `nodeId`
- `rowIndex`
- `columnIndex`
- `nodeType`
- `nextNodeIds`
- `content`
- `isResolved`

## node接続
### 現時点の本命
`土台生成 -> 特殊node配置 -> ジム保証調整`

### 理由
- シティの横並び条件を壊しにくい
- センター / ジムの連続禁止を処理順として説明しやすい
- ジム配置のみ再抽選しやすい

## ノード内容の事前生成
### 方針
- map 生成時に各 node の内容を全部確定する
- 進行中に node 内容を再抽選しない

### 事前確定するもの
- 敵構成
- gold 報酬
- reward 候補
- mod 配置

## 敵生成
### 方針
- battle node の内容として map 生成時に確定する
- `PachimonInfoTable` と共通ステータス生成ルールを使う
- gym / elite はその node 種別に応じた補正を後から足せる形にする

## 報酬生成
### 方針
- battle 後 reward は node 内容として事前確定する
- 通常戦 / ジム戦 / Elite戦で中身のルールを分ける
- item は将来追加候補として保留

## 難易度カーブ
### 調整対象
- row ごとの強さ
- gym / elite 補正
- speed 補正案

### 現時点
- 詳細式は後回し
- row に応じた段階補正を入れられる構造にする

## 保存データとの関係
### 保存したいもの
- `runSeed`
- 生成済み map
- 現在位置
- 解決済み node 状態

### 補足
- 保存タイミングは後で再検討する

## 後回しでよいもの
- Ghost の詳細ロジック
- ジム 8 ルート保証の厳密判定
- 接続アルゴリズムの最適化
- item の本実装
- 難易度カーブの細かい数式

## TODO
- `row1~35` の node 数上限を確定する
- センター配置数を確定する
- ジム配置数を確定する
- シティ統合の具体ルールを確定する
- GhostNode群の詳細配置を確定する

