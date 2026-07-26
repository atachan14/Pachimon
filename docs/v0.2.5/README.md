# v0.2.5: Compact Pane Layout

v0.2.5では、スマホ向け縦長画面でLeftPaneとRightPaneをDrawer表示へ切り替える。
PC向け横長画面の3Pane同時表示は維持する。

## 完成目標

- `Expanded`ではLeft / Main / Rightを従来どおり3columnで表示する
- `Compact`ではMainPaneを常時100%表示する
- `Compact`のLeftPaneとRightPaneをMainPane上のDrawerとして開閉する
- HeaderからLeft / Right Drawerを手動開閉できる
- RightPaneへ情報を表示した場合、CompactではRight Drawerを自動展開する
- RightPaneのキャンセルでCompactのRight Drawerを閉じる
- Map表示中もDrawerの開閉状態を保持する
- Mapを閉じるとMapを開く前のPane表示へ戻る

GoldとBadgeをLeftPaneへ移す案は将来変更として扱い、v0.2.5では既存Header表示を維持する。

## Status

主要なDrawer、Map重ね合わせ、StartNode連携、Compact用表示の実装と確認を完了。細かな改善は共通[`backlog.md`](../backlog.md)で管理する。

## 文書

1. [`compact-pane-layout.md`](./compact-pane-layout.md)
2. [`implementation-plan.md`](./implementation-plan.md)
