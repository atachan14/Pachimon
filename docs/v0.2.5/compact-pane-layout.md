# Compact Pane Layout

## 構造

```text
Body
├─ Content
│  ├─ LeftPane        Expanded時
│  ├─ MainPane
│  └─ RightPane       Expanded時
└─ OverlayLayer
   ├─ LeftDrawerViewport
   │  └─ LeftPane     Compact時
   ├─ RightDrawerViewport
   │  └─ RightPane    Compact時
   └─ MapViewport
      └─ MapOverlayView
```

Paneは複製せず、LayoutMode切替時に同じLeftPane / RightPaneをContentとDrawerの間で移動する。

## Expanded

- 既存の`HorizontalLayoutGroup`を使う
- Left / Main / Rightを同時表示する
- MapViewportはBodyの子に置いたまま、MainPaneの実際のRectへ追従する
- Left / Rightの開閉要求は表示状態へ影響しない

## Compact

- 画面が縦長の場合はCanvasScaler後の論理幅にかかわらずCompactを使用する
- 横長でも論理幅がBreakpoint未満の場合はCompactを使用する
- MainPaneはContent内で常に画面幅100%を使う
- Left / Right DrawerはMainPaneより前面へ表示する
- DrawerViewportへ`RectMask2D`を設定する
- Pane本体は画面幅100%を維持し、Viewportの開口率だけを0%から100%へ変える
- Pane内部を開閉中に圧縮しない
- LeftからRight、RightからLeftへMainPaneを挟まず切り替える
- 同じDrawerのHeaderボタンを再度押すとMainPaneへ戻る

## 自動開閉

- RightPaneへNodeまたはPachimon情報を表示した場合、Right Drawerを自動展開する
- RightPaneをClearした場合、Right Drawerを閉じる
- StartNodeではCandidate選択で展開し、キャンセルまたは3体目確定で閉じる
- Left Drawerの自動展開タイミングは各機能の実装時に追加する
- 自動開閉要求はExpandedでは無視する

## Map

- MapViewportはDrawerより前面に置く
- CompactではBody全体を覆う
- ExpandedではMainPaneのRectだけを覆う
- Map開閉で現在のDrawer状態を変更しない
- Right Drawer表示中にMapを開き、閉じた場合はRight Drawerへ戻る

## Overlay Stack

- DrawerとMapはOpen状態と最前面状態を別々に保持する
- 新しく開いたOverlayを最前面へ移動する
- Open中だが背面にあるOverlayのボタンを押した場合、閉じずに最前面へ移す
- 背面から最前面へ戻す場合も登場アニメーションを再生する
- 最前面でOpen中のOverlayのボタンを押した場合だけ閉じる
- 閉じる際は逆方向のアニメーションを再生し、背面でOpen中のOverlayを表示する

```text
Main
-> Rightを右から展開
-> Mapを上から展開（Rightは背面でOpen）
-> Rightを右から再展開（Mapは背面でOpen）
-> Rightを右へ収納（Mapを再表示）
```

## Header

- Compact時だけLeft / Right Drawerの手動開閉ボタンを表示する
- 仮ラベルは`PARTY`と`INFO`を使用する
- ExpandedではDrawerボタンを非表示にする
- Gold / BadgeのLeftPane移動後にHeader内の最終配置を再調整する
