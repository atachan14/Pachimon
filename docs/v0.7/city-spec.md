# City仕様

## City在庫

- 同じCity Groupの2Nodeは、1つの`CityNodeContent`と在庫を共有する
- ラインナップはMap生成時に確定し、Run中は再抽選しない
- 商品は1個ずつ独立した在庫Entryとして保持する
- 購入済みEntryは削除せず、`isPurchased`を保持して売り切れ表示にする

```text
CityStockEntry
- stockId
- itemId
- basePrice
- price
- isPurchased
```

## v0.7ラインナップ

| Category | Item | BasePrice | Cityごとの個数 |
|---|---|---:|---:|
| 薬局 | きずぐすり | 300 Gold | 10 |
| その他 | 石ころ | 200 Gold | 10 |

将来はCityのStageに応じた重み付き抽選へ拡張する。後半ほど上位Itemを増やす可能性がある。

## 価格

- 各商品の価格は`BasePrice`の70%から130%に収める
- 価格は個々の在庫Entryごとに異なる
- 価格差の合計はItem種類ごとではなく、City内の全在庫を対象に0とする
- City内の実売価格総額は、そのCityに並ぶ全商品のBasePrice総額と必ず一致する
- 高額Itemも同じ計算へ含める

これにより、きずぐすり全体が安く、石ころや技マシン全体が高いCityも生成できる。

## RightPane

- Cityへ移動する前から全ラインナップを閲覧できる
- 商品はCategoryごとのAccordionへまとめる
- 商品の名前・価格部分をClickすると、Skill / Passive詳細と共通のOverlayへItem詳細を表示する
- 購入操作は商品詳細とは分離し、商品行の右端に専用ボタンを表示する
- 移動前は閲覧のみとし、購入操作は表示しない
- City滞在中は商品ごとに購入操作を表示する
- 購入済み、Gold不足、Inventory満杯は理由を表示して購入不可にする
- Cityへ入った時点から進行可能で、何も買わずに終了できる

## 保留

- Cityの商品行へItem Iconを表示するか
- Item Icon自体を将来も維持するか

v0.7の商品行は文字中心で作り、後からIconを追加できる構造にする。
