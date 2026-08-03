# Status Effect Mechanics

複数の属性ファイルから参照する状態異常の共通仕様をまとめる。

## 記載項目

状態異常を追加するときは、必要な範囲で次を記載する。

```text
Value
効果時間
付与タイミング
発動タイミング
消費タイミング
再付与時
重複時
戦闘不能時
Battle終了時
解除可否
```

## 分類案

- `時限型`: 効果時間が0になると消滅する
- `消費型`: 条件を満たしたときに効果を発生して消滅する
- `スタック型`: ValueまたはStack数を加算して保持する
- `遷移型`: 終了時に別の状態異常を付与する

分類は実装を共通化するための候補であり、全状態異常を同じLogicへまとめることは要求しない。

## Category

状態異常は、固有のStatus IDとは別に複数の`StatusCategory`を持つことができる。

- 固有の発動・更新処理は個別Status Logicへ実装する
- Slow、Stun、Leakなどの共通判定はCategoryを使用する
- CategoryはC#のクラス継承関係と一致する必要はない
- 共通SystemはCategoryに属する複数の状態異常をまとめて検索・集計・消費できる

例:

```text
雨漏電
- StatusId: RainLeak
- Categories: Leak, WeatherGranted

凍結
- StatusId: Freeze
- Categories: Stun, RemovedByFire
```

同じCategoryに属していても、Value、効果時間、再付与、解除条件はStatus IDごとに定義する。

## 端数処理

- 状態異常のValueを独立した整数値として保存するときに切り捨てる
- 効果時間の途中計算では端数を維持し、完了tickを確定するときに切り上げる
- 正の効果時間は最低1tickとする
- Valueの最低保証は状態異常ごとに指定する
- `BaseValue × AmplificationMultiplier(Stat)`で生成するValueは原則0を許容する
- 軽減後Valueが0の`Slow / Leak`は付与しない

## 共通の時間進行

- 行動時計とCooldownを進めた後に、状態異常の時間・Valueを減少させる
- Slowは効果時間ではなくValueを毎tick減少させる

## 共通の未決事項

- 戦闘不能中の効果時間
- Battle終了時に全状態異常を解除するか
- 同名状態異常の再付与ルールを状態ごとに持たせるか

## 実装済み

### 漏電

- `StatusId`: `Leak`
- `Category`: `Leak`
- 消費型
- 再付与時は暫定でValueと付与元を置換する

### 蓄電

- `StatusId`: `StoredCharge`
- `Category`: `Charge`
- スタック型
- Electricダメージ直前に全スタックを消費する
- 消費スタック数1つにつき、対象のElectricダメージを10%増加させる
- Electricダメージ確定後に1スタック獲得する
- 0ダメージや漏電による追加ダメージも獲得対象とする

### Slow

- 具体仕様は[Slow Statuses](../statuses/slow.md)を参照する
