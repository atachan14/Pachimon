# Pachimon Catalog

## 役割

`PachimonCatalog.asset`は、Runをまたいで共通する151種のPachimon情報を1枚のScriptableObjectで管理する。

個体ごとに変化するStatsや所持Skillは保持しない。

## 1種分のデータ

```text
speciesId
displayName
frontSprite
backSprite
allocationType
fixedSkillId
passiveId
```

- `speciesId`は1-151を重複なく使用する
- `allocationType`はPachimon配置用の内部Typeで、戦闘計算には影響しない
- `allocationType`は`Fire / Aqua / Leaf / Electric / Poison / Ice / Wind / Dragon`の8種とする
- `fixedSkillId`と`passiveId`は独立した参照IDとして明示的に保持する
- Placeholder生成時は両方を`speciesId`と同じ値で初期化する
- 将来、共有Skill、Passiveなし、派生種などの要件に応じて個別変更できる

## Run生成との関係

1. Catalogの151種から不参加1種を決める
2. 残り150種を各2個体生成する
3. 各個体へ個別のStatsを生成する
4. Catalogの`allocationType`を各`PachimonInstance`へ引き継ぐ
5. `PachimonInstance`は`speciesId`でCatalogの名前と画像を参照する

MapやNodeにはCatalog行そのものではなく、Run中の`instanceId`を保持する。

## 仮データ

Editorツールで以下を生成する。

- 151行
- 仮名`パチモン001`から`パチモン151`
- 全行共通のfront placeholder
- 全行共通のback placeholder

仮データ生成後は、Catalogの各行で名前と画像を順番に差し替える。

Species 1の`パチギダネ`はFront / Backを個別制作済みとする。Battleでは3体を横並びにするため、各画像は`512x768`の縦長キャンバスとし、可視シルエットを中央付近へ細く収める。

未制作SpeciesにはパチギダネのFront / Backを共通仮Graphicとして設定し、本番Graphicの完成後にSpecies単位で順次差し替える。

## Editor操作

```text
Tools > Pachimon > Data > Create Pachimon Placeholder Catalog
Tools > Pachimon > Data > Validate Pachimon Catalog
Tools > Pachimon > Data > Migrate Missing Pachimon Logic IDs
Tools > Pachimon > Data > Migrate Missing Pachimon Allocation Types
Tools > Pachimon > Data > Apply Pachigidane Placeholder Graphics
```

作成済みCatalogは生成ツールで上書きしない。既存行のLogic IDが0の場合だけSpecies IDと同じ値を補完し、Allocation Typeが未設定の場合だけSpecies ID順に8Typeを均等に仮設定する。GameSceneが開かれている場合は`GameSceneInstaller`へ自動設定し、SceneをDirtyにする。
