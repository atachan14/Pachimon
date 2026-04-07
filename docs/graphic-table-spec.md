# GraphicTable Spec

このファイルは、CSV とは別管理する `GraphicTable.asset` の仕様メモとする。

## 目的
- 画像参照を CSV から分離する
- Unity 上で安全に `id -> 画像参照` を解決できるようにする
- DefinitionTable と GraphicTable の責務を分ける

## 基本方針
- CSV 側では画像パスや画像IDを持たない
- Unity 側の `GraphicTable.asset` が定義IDと画像参照を紐づける
- `DefinitionTable.id` と `GraphicTable.id` は一致させる

## 対象
- Pachimon
- Trainer
- GymLeader
- Mod

## 非対象
- Skill
- Passive

### 理由
- Skill / Passive は演出参照の個数や意味が可変になりやすい
- 画像や演出参照は Logic 側に寄せたほうが扱いやすい

## 共通ルール
### id
- `DefinitionTable` 側の `id` と一致する
- importer は扱わず、別 asset として手動管理する

### 参照
- `Sprite`
- `Texture2D`
- `GameObject`
- `Prefab`

実際にどれを使うかは対象ごとに決める

## PachimonGraphicTable
### 役割
- パチモンの前向き / 後ろ向き表示用の画像参照を持つ

### 例
```text
id: pachimon_001
front: Sprite
back: Sprite
```

### Row案
- `id`
- `front`
- `back`

## TrainerGraphicTable
### 役割
- 通常敵トレーナーの画像参照を持つ

### Row案
- `id`
- `graphic`

## GymLeaderGraphicTable
### 役割
- ジムリーダーの画像参照を持つ

### Row案
- `id`
- `graphic`

## ModGraphicTable
### 役割
- mod のアイコン参照を持つ

### Row案
- `id`
- `icon`

## asset 形の方針
### 基本
- 1種類ごとに 1つの `.asset`
- 内部は `List<Row>` を持つ

### 例
```csharp
[CreateAssetMenu(...)]
public class PachimonGraphicTable : ScriptableObject
{
    public List<PachimonGraphicRow> rows;
}
```

## row class 案
### PachimonGraphicRow
```csharp
[System.Serializable]
public class PachimonGraphicRow
{
    public string id;
    public Sprite front;
    public Sprite back;
}
```

### TrainerGraphicRow
```csharp
[System.Serializable]
public class TrainerGraphicRow
{
    public string id;
    public Sprite graphic;
}
```

## 解決方法
### 基本
- runtime では `id` を使って GraphicTable を引く
- よく使うなら辞書キャッシュしてよい

### 例
```csharp
var graphic = pachimonGraphicTable.GetFront("pachimon_001");
```

## 検証ルール
### 必須
- `id` は空でない
- `id` は一意
- 対応する DefinitionTable 側の `id` が存在する

### 警告候補
- Definition はあるが画像参照が空
- GraphicTable にだけ存在する未使用 `id`

## importer との関係
### 方針
- CSV importer は GraphicTable を触らない
- GraphicTable は別の editor 管理対象とする

### 理由
- 画像参照は Unity 上で直接確認したい
- CSV と画像アセットの責務を混ぜないほうが安全

## 管理方法のおすすめ
### 初期
- 手動で `.asset` を作る
- inspector で row を編集する

### 将来
- 件数が増えてつらくなったら custom editor を作る
- `DefinitionTable` と突き合わせる検証ツールを作ってもよい

## 実装順のおすすめ
1. `PachimonGraphicTable`
2. `TrainerGraphicTable`
3. `GymLeaderGraphicTable`
4. `ModGraphicTable`

## TODO
- `Sprite` と `Prefab` のどちらを採用するか
- GraphicTable 用の簡易検証ツールを作るか
- custom editor を後で作るか
