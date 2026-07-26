# Pachimon Info Table

このファイルは、`PachimonInfoTable` の最小構造メモとする。
今の方針では、これが pachimon 固有情報の中心になる。

## 目的
- `row:0` の初期候補生成に使う
- battle node / elite node の敵配置に使う
- 見た目と初期挙動を決める最小情報だけを持つ

## 位置づけ
`PachimonInfoTable` は、重い DefinitionTable 群の代わりに使う最小の code-first データ置き場とする。

この table が持つのは、
- 見た目
- 表示名
- 初期 skill
- passive

まででよい。

ステータスの細かい個体差は持たない。
それは共通ルールでランダム生成する。

## 最小構造
### 1件が持つ項目
- `id`
- `name`
- `front`
- `back`
- `fixedSkillId`
- `passiveId`

## 各項目の意味
### id
- pachimon を識別する id
- map 生成や party 生成の基準になる

### name
- 表示名
- 現時点では 1 言語分でよい

### front / back
- 戦闘表示用グラフィック
- front は敵表示寄り
- back は味方表示寄り

### fixedSkillId
- その pachimon が最初から固定で持つ skill
- 初期 skill 3 つのうち 1 つをここで固定する

### passiveId
- その pachimon が最初から持つ passive
- 当面は `passiveId = pachimonId` 前提でもよい
- ただし構造上は明示的に持っておくほうが安全

## あえて持たないもの
- weight
- favoriteAttribute
- 固定ステータス
- mod
- 現在HP / 現在MN
- ランダム skill 2 つ

## ステータス生成との関係
- ステータスは `PachimonInfoTable` では持たない
- 全 pachimon 共通のルールでランダム生成する
- 上昇値は共通の code-first データで持つ

## 初期 skill との関係
初期 skill は 3 つ。

1. `fixedSkillId`
2. row:0 選択時に付与するランダム skill
3. row:0 選択時に付与するランダム skill

## row:0 での使い方
1. `MapGenerator` が `PachimonInfoTable` から候補 3 体を選ぶ
2. StartNodeContent に候補 id を入れる
3. player が 3 体を選ぶ
4. `RunState.party` に `PachimonInstance` を生成して入れる
5. そのとき fixed skill と passive を反映する
6. ランダム skill 2 つを追加する

## battle node での使い方
1. `MapGenerator` が node 用の敵 pachimon id を決める
2. `PachimonInfoTable` から name / graphics / fixedSkill / passive を引く
3. 共通ルールでステータスを生成する
4. node 内容に敵パーティ情報として保持する

## 実装方針
### 現時点のおすすめ
- まずは `PachimonInfoCatalog.cs` のような code-first で持つ
- 件数が増えてから ScriptableObject 化を再検討する

### 1件のイメージ
```csharp
public sealed class PachimonInfo
{
    public int Id;
    public string Name;
    public Sprite Front;
    public Sprite Back;
    public int FixedSkillId;
    public int PassiveId;
}
```

## 後で追加を検討する項目
- `description`
- `tags`
- `rarity`
- `favoriteAttribute`
- `selectableInStart`

## 完了条件
- `row:0` の候補生成に必要な情報が揃う
- battle node の敵配置に必要な情報が揃う
- それ以外の情報は持たない
