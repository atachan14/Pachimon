# Passive Data

## 方針

Passiveは、調整データと実行ロジックを分離する。

- `PassiveAsset`
  - ID
  - 表示名
  - 説明
- 派生Asset
  - Passive種別ごとの調整値
- C# Logic
  - Battle Eventの購読
  - 対象選択
  - 状態変更
  - 固有処理
- `PassiveCatalog`
  - Runで利用するPassive Assetの一覧
  - ID検索と重複検証

ScriptableObjectは共有される不変の定義として扱う。所持者、スタック数、残りtickなどのRun中・Battle中の状態は保持しない。

## 派生Stat Passive

`DerivedAdditivePassiveAsset`は以下を保持する。

- 加算先Stat
- 参照Stat
- 参照値に掛ける割合
- 加算値の下限

現在は以下を`Assets/GameData/Passive/`で管理する。

| ID | Passive | 加算先 | 参照元 | 割合 | 下限 |
| ---: | --- | --- | --- | ---: | ---: |
| 12 | 水力発電 | Electric | Aqua | 30% | 0 |
| 20 | 火力発電 | Electric | Fire | 30% | 0 |
| 28 | 風力発電 | Electric | Wind | 30% | 0 |

これらの値はC#へ重複定義しない。`PassiveStatModifierRegistry`は`PassiveCatalog`を読み、実行用のStat Modifierへ変換する。

## 追加手順

1. `PassiveAsset`の適切な派生型を作成する。
2. `Assets/GameData/Passive/`へAssetを作成する。
3. ID、表示名、説明、調整値を設定する。
4. `PassiveCatalog.asset`へ追加する。
5. 固有処理が必要なら、IDに対応するC# LogicをRegistryへ登録する。
6. Stat表示、詳細表示、Battle本処理、Previewが同じ定義を参照することを確認する。

新しい調整項目が既存の派生Assetに合わない場合、無理に共通項目を増やさず、新しい`PassiveAsset`派生型と対応Logicを追加する。
