# v0.7実装手順

## Phase 1: Item販売データ

1. [完了] Item定義へCategoryとBasePriceを追加する
2. [完了] `CityStockEntry`を追加する
3. [完了] City全体の価格合計を保証する生成処理を追加する

## Phase 2: Map生成

1. [完了] 各Cityへ在庫を生成する
2. [完了] 同じCity Groupの2Nodeで同じ在庫を共有する
3. [完了] 個数、価格範囲、価格総額を検証する

## Phase 3: RightPane

1. [完了] `CityNodeWindow`を追加する
2. [完了] Category Accordionを追加する
3. [完了] 移動前の読み取り専用表示を接続する

## Phase 4: 購入

1. [完了] City滞在中だけ購入を許可する
2. [完了] GoldとInventoryを検証する
3. [完了] 購入成功時にGold、在庫、Item Panelを更新する
4. [完了] 売り切れと購入失敗理由を表示する

## Phase 5: 確認

1. [自動確認済み] 同一Seedでラインナップと価格が再現される
2. [自動確認済み] 全価格がBasePriceの70%から130%に収まる
3. [自動確認済み] City全体の価格総額がBasePrice総額と一致する
4. [実機確認待ち] Cityの左右どちらから入っても在庫状態が共通になる
