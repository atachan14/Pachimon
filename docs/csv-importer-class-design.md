# CSV Importer Class Design

このファイルは、`csv-importer-spec.md` を Unity 実装へ落とすためのクラス設計メモとする。

## 目的
- importer 実装時の責務分離を明確にする
- 定義ごとの importer をどう分けるか整理する
- 最小実装から段階的に増やせる構造にする

## 全体方針
- 共通処理と定義別処理を分ける
- CSV の読み取りと row 変換と asset 更新を分ける
- editor 用コードとして実装する

## 想定ディレクトリ
```text
Assets/
  Scripts/
    Editor/
      Definitions/
        Import/
          CsvTableReader.cs
          CsvRowReader.cs
          ImportReport.cs
          DefinitionImportUtility.cs
          BaseDefinitionImporter.cs
          PachimonDefinitionImporter.cs
          GlobalStatGainImporter.cs
          SkillDefinitionImporter.cs
          PassiveDefinitionImporter.cs
          ModDefinitionImporter.cs
          TrainerDefinitionImporter.cs
          GymLeaderDefinitionImporter.cs
          DefinitionImportMenu.cs
```

## 依存関係
### 大まかな流れ
1. `DefinitionImportMenu`
2. 各 `XxxDefinitionImporter`
3. `CsvTableReader`
4. `CsvRowReader`
5. row 変換
6. `DefinitionImportUtility`
7. `DefinitionTable.asset` 更新

## 共通クラス
### CsvTableReader
### 役割
- CSV 全体を読み込む
- header と data row を分ける

### 入力
- ファイルパス

### 出力
- `CsvTableData`

### 想定API
```csharp
public static CsvTableData Read(string assetPath)
```

### 持たせたい情報
- `Headers`
- `Rows`
- `SourcePath`

## CsvRowReader
### 役割
- 1行分の列アクセスを担当する
- 列名ベースで値を安全に取る

### 想定API
```csharp
public string GetRequired(string columnName)
public string GetOptional(string columnName, string defaultValue = "")
public int GetRequiredInt(string columnName)
public float GetRequiredFloat(string columnName)
public string[] GetSplitValues(string columnName, char separator = '|')
```

### 理由
- importer 側で column index を直接触らないようにする
- エラー位置を出しやすくする

## ImportReport
### 役割
- import 中の成功 / 警告 / エラーを集約する

### 想定API
```csharp
public void AddInfo(string message)
public void AddWarning(int rowIndex, string columnName, string message)
public void AddError(int rowIndex, string columnName, string message)
public bool HasError { get; }
public string BuildSummary()
```

### 持たせたい情報
- 成功件数
- 警告件数
- エラー件数
- メッセージ一覧

## DefinitionImportUtility
### 役割
- asset の生成 / 読み込み / 保存を共通化する

### 想定API
```csharp
public static T LoadOrCreateAsset<T>(string assetPath) where T : ScriptableObject
public static void SaveAsset(UnityEngine.Object asset)
public static void EnsureFolder(string folderPath)
```

## BaseDefinitionImporter
### 役割
- 定義別 importer の共通処理をまとめる

### 担当
- CSV 読み込み開始
- report 生成
- 例外ハンドリング
- asset 保存

### 想定API
```csharp
public abstract class BaseDefinitionImporter<TTable, TRow>
    where TTable : ScriptableObject
{
    public ImportReport Import(string csvPath, string assetPath);
    protected abstract List<TRow> ParseRows(CsvTableData table, ImportReport report);
    protected abstract void AssignRows(TTable asset, List<TRow> rows);
}
```

## 定義別 importer
### PachimonDefinitionImporter
### 役割
- `pachimon.csv` を `PachimonDefinitionTable.asset` に変換する

### 主な検証
- `id` 重複
- `initial_skill_ids` 参照確認
- `unique_passive_id` 参照確認
- `weight_* >= 0`

### 注意点
- `skill.csv` と `passive.csv` を参照するので、import 順か依存解決を意識する

## GlobalStatGainImporter
### 役割
- `global_stat_gain.csv` を `GlobalStatGainTable.asset` に変換する

### 主な検証
- `stat_id` 重複
- `gain_value >= 0`

## SkillDefinitionImporter
### 役割
- `skill.csv` を `SkillDefinitionTable.asset` に変換する

### 主な検証
- `logic_id` 必須
- `base_turn_cd >= 0`
- `base_skill_cd >= 0`
- `mana_cost` が数値または文字列

### 備考
- `mana_cost` は string で保持してもよい
- fixed 判定は runtime 側で `int.TryParse` してもよい

## PassiveDefinitionImporter
### 役割
- `passive.csv` を `PassiveDefinitionTable.asset` に変換する

### 主な検証
- `logic_id` 必須

## ModDefinitionImporter
### 役割
- `mod.csv` を `ModDefinitionTable.asset` に変換する

### 主な検証
- `stat_list` と `ratio_list` の数一致
- `ratio_list` が数値として読める

## TrainerDefinitionImporter
### 役割
- `trainer.csv` を `TrainerDefinitionTable.asset` に変換する

### 主な検証
- `gold_min <= gold_max`

## GymLeaderDefinitionImporter
### 役割
- `gym_leader.csv` を `GymLeaderDefinitionTable.asset` に変換する

### 主な検証
- `gold_min <= gold_max`

## メニュークラス
### DefinitionImportMenu
### 役割
- Unity Editor メニューから importer を呼ぶ

### 想定メニュー
- `Tools/Pachimon/Import/All`
- `Tools/Pachimon/Import/Pachimon`
- `Tools/Pachimon/Import/GlobalStatGain`
- `Tools/Pachimon/Import/Skill`
- `Tools/Pachimon/Import/Passive`
- `Tools/Pachimon/Import/Mod`
- `Tools/Pachimon/Import/Trainer`
- `Tools/Pachimon/Import/GymLeader`

## 補助データ型
### CsvTableData
```csharp
public class CsvTableData
{
    public string SourcePath;
    public List<string> Headers;
    public List<string[]> Rows;
}
```

### ImportMessage
```csharp
public class ImportMessage
{
    public ImportMessageType Type;
    public int RowIndex;
    public string ColumnName;
    public string Message;
}
```

## row 変換の方針
### 基本
- importer で CSV 文字列から型変換する
- asset には使いやすい型で保存する

### 例
- `gold_min` -> `int`
- `initial_skill_ids` -> `List<string>`
- `mana_cost` -> `string`

## 参照検証の方針
### 最初の実装
- CSV 同士の参照チェックで十分

### 方法
- `skill.csv` の `id` 一覧を set 化
- `passive.csv` の `id` 一覧を set 化
- `pachimon.csv` import 時に照合する

### 後回しでよいこと
- LogicRegistry との一致チェック
- GraphicTable との一致チェック

## エラー処理
### 基本方針
- 致命的エラーがあれば asset 更新しない
- report を Console とダイアログの両方で見られるようにしてよい

### 例
- `id` 重複
- 必須列欠落
- 数値変換失敗
- 参照ID不在

## import 順のおすすめ
1. `skill.csv`
2. `passive.csv`
3. `global_stat_gain.csv`
4. `pachimon.csv`
5. `mod.csv`
6. `trainer.csv`
7. `gym_leader.csv`

## 最小実装のおすすめ順
### Step 1
- `CsvTableReader`
- `ImportReport`
- `DefinitionImportUtility`

### Step 2
- `SkillDefinitionImporter`
- `PassiveDefinitionImporter`

### Step 3
- `GlobalStatGainImporter`
- `PachimonDefinitionImporter`

### Step 4
- `ModDefinitionImporter`
- `TrainerDefinitionImporter`
- `GymLeaderDefinitionImporter`

### Step 5
- `DefinitionImportMenu`

## 実装上の注意
- CSV parser を雑に `Split(',')` すると引用符対応で壊れやすい
- 最初はサンプル CSV が単純でも、将来的に説明文カラムで詰まりやすい
- parser は最初から少し安全寄りにしておくとよい

## 今のおすすめ
- まずは `SkillDefinitionImporter` と `PassiveDefinitionImporter` を作る
- 次に `GlobalStatGainImporter` と `PachimonDefinitionImporter`
- `Import All` は最後でよい

## TODO
- CSV parser 実装方式
- row class の配置場所
- Generated asset の CreateAssetMenu を使うかどうか
