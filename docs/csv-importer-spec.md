# CSV Importer Spec

このファイルは、`Assets/Definitions/SourceCsv/` の CSV を Unity 内の DefinitionTable asset に変換する importer 仕様メモとする。

## 目的
- CSV から Unity で使える定義データを生成する
- 手動編集しやすい元データと、実行時に扱いやすいアセットを分離する
- 取り込み時の検証ルールを明確にする

## 対象
- `pachimon.csv`
- `skill.csv`
- `passive.csv`
- `mod.csv`
- `trainer.csv`
- `gym_leader.csv`
- `global_stat_gain.csv`

## 入力元
- ディレクトリ: `Assets/Definitions/SourceCsv/`
- 文字コード: UTF-8
- 1行目: header
- 2行目以降: data row

## 出力先
- ディレクトリ: `Assets/Definitions/Generated/`

### 出力asset
- `PachimonDefinitionTable.asset`
- `SkillDefinitionTable.asset`
- `PassiveDefinitionTable.asset`
- `ModDefinitionTable.asset`
- `TrainerDefinitionTable.asset`
- `GymLeaderDefinitionTable.asset`
- `GlobalStatGainTable.asset`

## importer の責務
1. CSV を読む
2. header と列順を解釈する
3. 各行を row データへ変換する
4. 必須項目を検証する
5. 参照整合性を検証する
6. DefinitionTable asset を生成または更新する
7. エラーがあれば editor 上でわかるように表示する

## importer の非責務
- Skill Logic の生成
- Passive Logic の生成
- GraphicTable.asset の生成
- battleバランスの自動調整
- CSV の自動修正

## 実装形のおすすめ
### 入口
- Unity Editor メニューから手動実行

例:
- `Tools/Pachimon/Import Definitions`
- `Tools/Pachimon/Import Pachimon`
- `Tools/Pachimon/Import All Definitions`

### 理由
- 最初は明示的に更新したい
- 自動再import よりデバッグしやすい
- エラー原因を追いやすい

## 基本構成
### 共通部
- `CsvTableReader`
- `CsvRowReader`
- `ImportReport`
- `DefinitionImportUtility`

### 定義別 importer
- `PachimonDefinitionImporter`
- `SkillDefinitionImporter`
- `PassiveDefinitionImporter`
- `ModDefinitionImporter`
- `TrainerDefinitionImporter`
- `GymLeaderDefinitionImporter`
- `GlobalStatGainImporter`

## DefinitionTable asset の形
### 方針
- 各 DefinitionTable asset は `List<Row>` を持つ
- row は serializable class とする
- `id` をキーとして検索できるよう補助辞書を runtime 初期化で作ってもよい

### 例
```csharp
[CreateAssetMenu(...)]
public class SkillDefinitionTable : ScriptableObject
{
    public List<SkillDefinitionRow> rows;
}
```

## CSV ごとの想定 row
### PachimonDefinitionRow
- `id`
- `nameJa`
- `nameEn`
- `descriptionJa`
- `descriptionEn`
- `favoriteAttribute`
- `initialSkillIds`
- `uniquePassiveId`
- `weightHp`
- `weightMn`
- `weightMnreg`
- `weightFire`
- `weightWater`
- `weightLeaf`
- `weightElectric`
- `weightPoison`
- `weightEarth`
- `weightIce`
- `weightDragon`
- `weightSpeed`
- `weightSkillheist`
- `weightPierceBonus`
- `weightThreat`
- `weightBreak`
- `weightCritRate`
- `weightCritDamage`

### SkillDefinitionRow
- `id`
- `nameJa`
- `nameEn`
- `descriptionJa`
- `descriptionEn`
- `baseTurnCd`
- `baseSkillCd`
- `manaCost`
- `attribute`
- `logicId`

### PassiveDefinitionRow
- `id`
- `nameJa`
- `nameEn`
- `descriptionJa`
- `descriptionEn`
- `logicId`

### ModDefinitionRow
- `id`
- `nameJa`
- `nameEn`
- `statList`
- `ratioList`

### TrainerDefinitionRow
- `id`
- `titleJa`
- `titleEn`
- `favoriteAttribute`
- `goldMin`
- `goldMax`

### GymLeaderDefinitionRow
- `id`
- `favoriteAttribute`
- `goldMin`
- `goldMax`

### GlobalStatGainRow
- `statId`
- `gainValue`

## 参照の扱い
### 基本方針
- CSV 内では参照をすべて `id` で持つ
- importer ではまず文字列のまま row に格納してよい
- 実参照への解決は runtime 初期化か、DefinitionTable 側の helper で行ってよい

### 例
- `initialSkillIds = skill_fire_bite|skill_guard_stance`
- `uniquePassiveId = passive_fast_start`
- `logicId = skill_fire_bite`

## 複数値の扱い
### 現在方針
- 区切り文字は `|`

### importer の処理
- 空文字なら空配列
- 値がある場合は `|` で split
- 前後空白は trim

### 対象例
- `initialSkillIds`
- `statList`
- `ratioList`

## 検証ルール
### 共通
- `id` は必須
- `id` は一意
- 必須列が存在する
- 空行は無視する

### Pachimon
- `uniquePassiveId` が `passive.csv` に存在する
- `initialSkillIds` の各値が `skill.csv` に存在する
- `weight_*` が 0 以上

### Skill
- `logicId` が空でない
- `baseTurnCd` が 0 以上
- `baseSkillCd` が 0 以上
- `manaCost` が数値なら 0 以上
- `manaCost` が文字列なら特殊仕様として許可する

### Passive
- `logicId` が空でない

### GlobalStatGain
- `statId` が空でない
- `gainValue` が 0 以上

### Mod
- `statList` と `ratioList` の要素数が一致する
- `ratioList` が数値として読める

### Trainer / GymLeader
- `goldMin <= goldMax`

## エラー方針
### import を止めるエラー
- `id` 重複
- 必須列欠落
- 参照先ID欠落
- 数値変換失敗
- `statList` と `ratioList` の数不一致

### 警告で済ませる候補
- 未使用の列がある
- 説明文が空
- `name_en` が空

## レポート表示
### ほしいもの
- 何件成功したか
- 何件失敗したか
- 何行目で失敗したか
- どの列が原因か

### 例
```text
[Pachimon Import]
Success: 148
Error: row 12, unique_passive_id = passive_xxx not found
Error: row 35, draw_count must be >= 1
```

## asset 更新方針
### 初期
- import ごとに rows を全置換する

### 理由
- シンプル
- 差分更新より壊れにくい
- データ量的に十分軽い

## GraphicTable との関係
### 方針
- CSV importer では GraphicTable を扱わない
- `PachimonGraphicTable.asset` などは別管理とする
- `id` 一致で参照解決する

## LogicRegistry との関係
### 方針
- importer は `logicId` の文字列を保持するだけでよい
- `logicId -> Logic` の解決は Registry 側の責務

## 初期実装のおすすめ順
1. `skill.csv` importer
2. `passive.csv` importer
3. `pachimon.csv` importer
4. `global_stat_gain.csv` importer
5. `mod.csv` importer
6. `trainer.csv` importer
7. `gym_leader.csv` importer

## TODO
- CSV parser を自作するか既存利用するか
- enum 列を文字列のまま持つか変換するか
- import ボタンを個別に分けるか一括にするか
