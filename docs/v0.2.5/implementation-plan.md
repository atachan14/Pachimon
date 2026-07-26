# v0.2.5 Implementation Plan

## Phase 1: Drawer基盤

1. Body直下へOverlayLayerを生成する
2. Left / Right DrawerViewportを生成する
3. Compact時にLeft / Right PaneをDrawerへ移動する
4. Viewportの開口率をアニメーションする
5. Expandedへ戻した場合は既存ContentへPaneを戻す

## Phase 2: Header操作

1. Compact用`PARTY` / `INFO`ボタンを追加する
2. 同じボタンの再押下でDrawerを閉じる
3. Left / Rightを直接切り替えられるようにする
4. Expandedではボタンを非表示にする

## Phase 3: 自動開閉

1. RightPaneの表示開始をGameRootViewへ通知する
2. Compactの場合だけRight Drawerを自動展開する
3. RightPaneのClearでRight Drawerを閉じる
4. StartNodeのCandidate選択とキャンセルで確認する

## Phase 4: MapViewport

1. MapViewportをMainPaneからOverlayLayerへ移す
2. CompactではBody全体へStretchする
3. ExpandedではMainPaneの実Rectへ追従する
4. Map開閉前後でDrawer状態が維持されることを確認する
5. Open中のDrawer / Mapを最前面へ戻した場合も登場アニメーションを再生する

## 完了確認

1. 横長画面では従来の3Pane表示になる
2. 縦長画面ではMainPaneだけが基本表示になる
3. HeaderからLeft / Rightを開閉できる
4. Left / Rightを直接切り替えられる
5. Start候補クリックでRight Drawerが開く
6. キャンセルでMainPaneへ戻る
7. Right Drawer表示中にMapを開閉するとRight Drawerへ戻る
8. LayoutModeを往復してもPane内容とStart進行状態が失われない
