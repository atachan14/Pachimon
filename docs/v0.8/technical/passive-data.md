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
| 9 | 闇の炎 | 追加Poison Damage | Fire軽減前Damage / Poison | 基礎20%・Poison 100%スケール | - |
| 12 | 水力発電 | Electric | Aqua | 30% | 0 |
| 13 | 毒素適応（仮） | Poison | 毒素付与回数 | 1回ごとに10%乗算 | - |
| 20 | 火力発電 | Electric | Fire | 30% | 0 |
| 21 | 科学工作 | Field Value | Poison | 30%増幅 | - |
| 28 | 風力発電 | Electric | Wind | 30% | 0 |
| 29 | 毒走り | Speed | Poison | 30% | 0 |
| 33 | ファイアアーチャー | 追加Fire Damage | 対象の減少HP / Fire | 減少HPの5%・Fire 100%スケール | - |
| 37 | 熱毒 | Poison | Fire | 100% | 0 |
| 41 | 燃える男 | Fire | Damageを受けた回数 | 1回ごとに+20 | - |
| 45 | 毒の騎士 | Shield / HP回復の共有 | Poison | 基礎30%・Poison 100%スケール | - |

これらの値はC#へ重複定義しない。`PassiveStatModifierRegistry`は`PassiveCatalog`を読み、実行用のStat Modifierへ変換する。

`毒素適応`は非Battle時の固定Stat補正ではなく、`ToxinAppliedEvent`を購読するBattle専用Passiveとして扱う。毒素の付与手段に依存せず、付与元Unitと実際の付与回数をEventから受け取り、Battle中の累積StatusをPoisonの乗算補正へ変換する。

`毒の騎士`は`ShieldAppliedEvent / HpRestoredEvent`を購読する。共有効果には`isSharedEffect`を付与し、複数の所持者がいる場合も共有から共有が連鎖しないようにする。Battle中のShield付与・HP回復は原則として`BattleSupportEffectRuntime`を経由させる。

`燃える男`は属性・True・Statusを統合した`DamageAppliedEvent`を購読する。実際のHP DamageとShield吸収Damageの合計が1以上で、かつ所持者が生存している場合にBattle中のFire加算Statusを1Stack追加する。

## 追加手順

1. `PassiveAsset`の適切な派生型を作成する。
2. `Assets/GameData/Passive/`へAssetを作成する。
3. ID、表示名、説明、調整値を設定する。
4. `PassiveCatalog.asset`へ追加する。
5. 固有処理が必要なら、IDに対応するC# LogicをRegistryへ登録する。
6. Stat表示、詳細表示、Battle本処理、Previewが同じ定義を参照することを確認する。

新しい調整項目が既存の派生Assetに合わない場合、無理に共通項目を増やさず、新しい`PassiveAsset`派生型と対応Logicを追加する。
