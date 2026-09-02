# v0.8.2 Compact First

## 目的

初見プレイヤーの情報量を抑えるため、Compact表示を標準UIとして完成させる。
Expanded表示は、複数Paneを同時に確認したいプレイヤー向けの任意モードとして維持する。

## 決定事項

- 新規環境では画面比率にかかわらずCompactを初期選択とする
- 設定OverlayからCompact / Expandedを切り替えられる
- 選択した表示モードは端末内へ保存する
- Expandedを選択中でも、画面幅が足りない場合はCompactへ一時的に切り替える
- 画面幅が戻った場合は、保存済みのExpanded選択へ戻す
- MainPane / LeftPane / RightPaneは複製せず、既存のDrawer構造を継続利用する

## 実装順

1. 表示モードの選択、保存、狭幅時フォールバックを実装する
2. HeaderのPane開閉とOverlayの重なり順をCompact基準で確認する
3. Start / Battle / Reward / RestSpot / City / LeagueGateをCompactで通す
4. 各画面の自動Pane展開と、戻る操作を統一する
5. 文字サイズ、スクロール誘導、選択状態、操作不能状態を調整する
6. デスクトップWeb BuildでCompact / Expanded双方を回帰確認する

## Party拡張

StartNodeで3体から1体を選び、その後の固定EncounterでPartyを
`1体 -> 2体 -> 3体`へ段階的に拡張する。

詳細は[party-progression.md](party-progression.md)を参照する。
影響範囲と実装順は[party-progression-implementation-plan.md](party-progression-implementation-plan.md)を参照する。

## 対象外

- EventNodeの本実装
- モバイルブラウザー正式対応
- Save / Load
