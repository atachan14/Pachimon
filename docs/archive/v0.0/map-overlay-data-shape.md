# Map Overlay Data Shape

このファイルは、`MapOverlay を開いたら node と edge が見える` 状態を作るための最小データ shape を整理する。

## 目的
- `MapGenerator` が何を返せば MapOverlay で描けるかを固定する
- UI 実装前に、必要な node / edge 情報を明確にする

## まず描ければよいもの
- node の位置
- node の種別
- edge の接続
- 現在 node
- 解決済み node
- 選択可能 node

## 最小 shape
### RunMap
- `rows`
- `nodes`
- `startNodeId`

### MapRow
- `rowIndex`
- `nodeIds`

### MapNode
- `nodeId`
- `rowIndex`
- `columnIndex`
- `nodeType`
- `nextNodeIds`
- `isResolved`

## MapOverlay で追加で見る runtime 情報
### RunState
- `currentNodeId`
- `resolvedNodeIds`

この 2 つがあれば、
- 今いる node
- すでに通過した node
- 次に進める node

を見分けられる。

## 描画時の扱い
### node の位置
- `rowIndex` で縦位置を決める
- `columnIndex` で横位置を決める

### edge の位置
- `MapNode.nextNodeIds` から接続先を引く
- 始点 node と終点 node の座標を結ぶ

### node の見た目
- `nodeType` に応じて色やアイコンを変える
- `isResolved` で見た目を変える
- `currentNodeId` と一致する node を強調する

## 最初の表示目標
1. `MapOverlay` を開く
2. node が row / column に従って並ぶ
3. edge が引かれる
4. 現在 node がわかる

ここまで通れば、次に
- node 選択
- 選択可能 node 強調
- node type ごとの見た目差分

へ進める。

## 今のおすすめ実装順
1. `MapGenerator` が `RunMap` を返す
2. `MapOverlayView` に仮描画用の node view を並べる
3. edge を線で描く
4. `currentNodeId` と `resolvedNodeIds` を反映する
5. その後に node 選択を入れる
