# v0.2 Implementation Plan

StartNodeを最後まで通すための実装順。各Phaseを単独で動作確認し、演出は進行ロジックが完成してから追加する。

## Phase 1: New Game入力

対象:

- `TitleSceneInstaller`
- `SceneLoader`周辺のNew Game入力
- `RunBootstrap`
- `RunState`

実装:

1. TitleSceneの`はじめから`ボタンの上へ常設のplayer名入力欄を追加する
2. Placeholderを`名前を入力してください`にする
3. 入力値の前後の空白を除去する
4. 空欄なら`ゲスト`へ置き換える
5. Scene遷移用の一時的なNew Game入力としてGameSceneへ渡す
6. `RunBootstrap`がplayer名を受け取り、`RunState.PlayerName`へ設定する
7. GameScene直接起動時は`ゲスト`を使用する

一時入力はSaveデータにせず、次のGameScene起動時に一度だけ消費する。Save / Load用の永続構造はv1.1で分ける。

確認:

- 入力した名前がRunStateへ入る
- 空欄で`ゲスト`になる
- GameScene直接起動でも`ゲスト`になる
- GameSceneを再起動して古い入力が意図せず残らない
- 名前入力欄が常に表示され、入力の有無にかかわらず`はじめから`を押せる

## Phase 2: Party更新APIとLeftPane

対象:

- `RunState`
- `LeftPaneView`
- Trainer / Pachimon Tab表示

実装:

1. party 3体を選択順つきで登録するAPIを用意する
2. 4体以上、重複ID、二重確定を拒否する
3. LeftPaneをTrainerTab + PachimonTab 3個の構造へ対応させる
4. party未確定時は3つのPachimonTabへHidden Previewを渡す
5. party確定時は選択順に実Previewへ差し替える
6. TrainerTabには最低限player名を表示する

RightPane専用コードをLeftPaneから直接操作せず、TrainerとPachimonのTab表示部分だけを再利用できる形にする。FooterとNode選択処理はRightPane側に残す。

確認:

- Start開始時にTrainer + `？ / ？ / ？`の4Tabが見える
- 仮の3体を登録すると選択順に表示が更新される
- party登録を再度呼んでも内容が重複しない

## Phase 3: Start進行ロジック

対象:

- `StartNodeController`新設
- `MapRunController`
- `StartScreen`

実装:

1. `IntroDialogue / Selecting / SelectionConfirmation / FinalDialogue / Completed`を定義する
2. 会話ページと選択IDの順序を保持する
3. 3体目の選択で確認状態へ移る
4. `いいえ`で選択を全解除する
5. `はい`でparty登録、LeftPane更新、最終メッセージ表示を行う
6. 最終メッセージの`おう`でNode完了を行う
6. 完了通知を`MapRunController`へ渡す

このPhaseではスライドやフェードを行わず、LogWindowの文字と仮ボタンで状態遷移だけを通す。

確認:

- 3体選ぶ前に確認状態へ進まない
- 選択順が保持される
- 途中の選択解除で後続の順番が前へ詰まる
- `いいえ`で0体へ戻る
- `はい`で一度だけpartyとNodeが確定する

## Phase 4: CandidatePanel

対象:

- `StartScreen`
- Candidate Card
- Start候補9体のPreview生成

実装:

1. `StartNodeContent`から候補9体を取得する
2. Front Graphicと名前を持つCandidate Cardを9個表示する
3. CardクリックをRightPane詳細表示へ接続する
4. 選択済みCardをグレー表示する
5. Card下部へ`n匹目`を表示する
6. 選択状態の更新を9Cardすべてへ反映する

確認:

- Map生成済みの候補9体と表示内容が一致する
- Cardクリックだけではpartyへ登録されない
- 選択、解除、選び直しが表示へ即時反映される

## Phase 5: RightPane候補選択

対象:

- `RightPaneView`
- Trainer / Pachimon Window
- Tab列
- Footer

実装:

1. Start候補9体を公開済みPreviewとして渡す
2. 9個のPachimon Tabを動的に表示する
3. ExpandedではTabを非表示、Compactでは3列×3行のグリッドにする
4. Candidate Cardから対象Tabを選択する
5. 未選択時は`キャンセル / n匹目にする`を表示する
6. 選択済み時は`キャンセル / n匹目を取り消す`を表示する
7. `キャンセル`で詳細表示を閉じ、選択状態は変更しない

確認:

- 9Tabすべてへ切り替えられる
- Footerの文言が対象と選択順に追従する
- Start候補の詳細が`？`にならない
- Footer操作とCandidate Card表示が同期する

## Phase 6: 確認演出と完了接続

実装:

1. 3体目選択時にRightPaneの候補操作を閉じる
2. 未選択6体をフェードアウトする
3. 選択3体を少し拡大して選択順に整列する
4. LogWindowへ`この3匹でよろしいか`を表示する
5. `はい / いいえ`を接続する
6. `はい`で最終メッセージと`おう`を表示する
7. 最終メッセージの`おう`でMapOverlayを自動で開く

確認:

- `いいえ`から9体表示へ完全に戻れる
- `はい`でLeftPane更新と最終メッセージ表示が行われる
- 最終メッセージの`おう`でStartNode完了が行われる
- MapOverlayを閉じると最終メッセージが残っている
- row 1の3Nodeへ移動できる

## Phase 7: パンと表示演出

実装:

1. ProfessorLayerを右、CandidatePanelを左へ横並びにする
2. Start開始時はProfessorLayerを表示する
3. 2つ目のメッセージと同時にCandidatePanel側へパンする
4. スライド距離と時間をInspector設定にする
5. Map開閉で演出と進行状態がリセットされないようにする

確認:

- Editorの幅変更後も正しい距離だけパンする
- Compact / Expandedの両方で画面外の要素が見切れない
- Mapを途中で開閉しても導入演出を再生しない
- 演出中の操作で状態遷移が二重に実行されない

## Scene作業のタイミング

SceneのHierarchy変更は、対応するViewのSerializeFieldが決まったPhaseで行う。

- Phase 1: TitleSceneの名前入力欄
- Phase 2: LeftPaneのTab構造
- Phase 4: StartScreenのPanContentとCandidate Card Template
- Phase 5: RightPaneのCompact専用3列×3行TabとFooter Text参照

スクリプトを先に用意してからSceneへ差し込み、参照不足はInstallerまたはViewの検証ログで明示する。

## v0.2完了確認

1. TitleSceneで名前を入力して開始する
2. 博士が入力名を呼ぶ
3. 9体の詳細をRightPaneで確認する
4. 選択と解除を行う
5. 3体を選んで一度`いいえ`で戻る
6. 再度3体を選び`はい`で確定する
7. LeftPaneへpartyが表示される
8. 最終メッセージで`おう`を押すとMapが開く
9. row 1へ移動する
10. GameScene直接起動でも同じ流れを`ゲスト`で完走する
