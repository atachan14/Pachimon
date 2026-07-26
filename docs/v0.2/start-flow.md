# Start Flow

## 全体フロー

```text
TitleScene: player名を入力
  -> はじめから
  -> 空欄なら「ゲスト」を使用する
  -> GameSceneを読み込む
  -> New Game用RunとMapを生成する
  -> row:0のStartNodeを起動する
  -> 博士の導入会話
  -> 候補9体から3体を選択する
  -> 選択した3体の確認
  -> いいえ: 選択を全解除して選び直す
  -> はい: 3体をpartyへ登録する
           LeftPaneへpartyを表示する
           博士の最終メッセージを表示する
           StartNodeを完了する
  -> row:1の3Nodeを移動可能にする
  -> MapOverlayを自動で開く
```

## 進行状態

StartNodeの進行は、画面の表示状態ではなく専用の状態として管理する。

```text
IntroDialogue
  -> Selecting
  -> SelectionConfirmation
  -> FinalDialogue
  -> Completed
```

## Player名

- タイトル画面に名前入力欄を常時表示する
- 名前入力欄は`はじめから`ボタンの上へ配置する
- Placeholderは`名前を入力してください`とする
- タイトル画面でNew Game開始前に入力する
- 前後の空白を除去した結果が空文字なら`ゲスト`を使用する
- 確定した名前はNew Gameの入力値としてGameSceneへ渡し、Run中は`RunState`から参照できるようにする
- GameSceneをEditorから直接起動した場合も`ゲスト`を使用する
- 博士の台詞などでは確定済みの名前を参照する
- v0.2では名前以外のplayerプロフィールを編集しない
- トレーナーカードの作成と編集はv1.0以降で扱う

### IntroDialogue

- StartScreen開始時から博士を表示する
- 最初に`よく来たね、[名前]`と表示する
- `おう`を押すと`ここに9匹のパチモンがおるじゃろ\n3匹選びなさい`へ進む
- 2つ目のメッセージ表示と同時に画面を候補一覧へパンし、`Selecting`へ移る
- partyとStartNodeの解決状態は変更しない

### Selecting

- `StartNodeContent.candidatePachimonInstanceIds`の9体を表示する
- 選択済み個体IDを選択順つきで最大3件まで保持する
- CandidatePanelの候補を押すと、その候補の詳細をRightPaneへ表示する
- RightPaneの候補Tabから別の候補へ切り替えられる
- 未選択候補のFooterには`キャンセル`と`n匹目にする`を表示する
- `n`には現在の選択数 + 1を使用する
- 選択済み候補のFooterには`キャンセル`と`n匹目を取り消す`を表示する
- 途中の選択を取り消した場合、後続候補の選択順を前へ詰める
- CandidatePanelでは選択済み候補をグレー表示し、`n匹目`と表示する
- 3体目を選択すると候補の追加操作を終了し、確認演出後に`SelectionConfirmation`へ移る

### SelectionConfirmation

- 未選択の6体をフェードアウトする
- 選択した3体を少し拡大し、選択順に整列する
- LogWindowに`この3匹でよろしいか`と表示する
- `はい`と`いいえ`を表示する
- `いいえ`を選ぶと選択をすべて解除し、CandidatePanelを9体表示へ戻して`Selecting`へ移る
- `はい`を選ぶと3体を`RunState.party`へ登録する
- 同時にLeftPaneを確定済みparty表示へ更新する
- 同時にLogWindowへ`バッジを8つ以上集めて、パチモンマスターを目指すのじゃ！`と表示する
- `FinalDialogue`へ移り、LogWindowに`おう`を表示する

### FinalDialogue

- 最終メッセージの表示中はStartNodeを未完了のままにする
- `おう`を押すとStartNodeを完了し、`Completed`へ移る

### Completed

- StartNodeを解決済みにする
- StartNodeのOutgoing Edgeに接続された`row:1`のNodeを公開する
- 次Nodeへの移動を許可する
- MapOverlayを自動で開く
- 完了処理は複数回呼ばれてもpartyの重複登録や二重完了を起こさない
- 最終メッセージはStartScreenのLogWindowに残し、MapOverlayを閉じた場合も確認できる

## LeftPane表示

LeftPaneは、RightPaneでBattleNodeを表示する場合と同じTab構成をplayer側の情報表示に利用する。

```text
LeftPane
├─ TrainerTab
├─ PachimonTab 1
├─ PachimonTab 2
└─ PachimonTab 3
```

- TrainerTabにはplayer Trainerの情報を表示する
- PachimonTab 1から3はpartyの選択順に対応する
- Start開始時点ではpartyが未確定のため、3つのPachimonTabを未公開BattleNodeと同じ`？`表示にする
- 確認で`はい`を選んだ時点で、3つのTabを選択したPachimonの名前と詳細へ切り替える
- party確定後はMapOverlayを開いている間も確定済み表示を維持する

## RightPane操作

- CandidatePanel上の候補クリックは選択確定ではなく、詳細表示だけを行う
- RightPane Footerの操作で選択または選択解除する
- `キャンセル`はRightPaneでの現在の候補確認を閉じる操作とし、すでに確定した選択順は変更しない
- ExpandedではStart候補Tabを表示しない
- CompactではRightPaneに候補9体分のTabを3列×3行で表示する
- CompactのStart候補Tabはスクロールさせない
- 詳細内容は公開済みBattleNodeのPachimon詳細と同じ表示規則を使う
- Start候補は公開済みとして扱い、`？`で隠さない

## Map開閉

- HeaderのMapボタンはStart処理中も使用できる
- MapOverlayを開いてもStartの進行状態を変更しない
- MapOverlayを閉じると、開く直前の会話位置または選択状態へ戻る
- StartNode完了前はMap上のNode情報を閲覧できるが、次Nodeへ移動できない
- 演出中にMapを開いた場合、演出だけは終了位置まで進んでよい
- 会話ページ送り、候補選択、決定などのゲーム進行はMap表示中に自動で進めない

## データ更新のタイミング

| タイミング | 更新内容 |
| --- | --- |
| Start開始 | Start進行状態を`IntroDialogue`にする |
| 候補選択 | Start処理中の選択IDだけを更新する |
| 確認で「いいえ」 | 選択IDをすべて消去して`Selecting`へ戻る |
| 確認で「はい」 | party登録、LeftPane更新、最終メッセージ表示、`FinalDialogue`への遷移を行う |
| 最終会話で「おう」 | StartNode完了を行う |

候補選択中と確認中は`RunState.party`を書き換えない。確認の`はい`をparty確定、最終会話の`おう`をStartNode完了のトリガーとして分ける。

## 責務案

### StartNodeController

- Start進行状態を保持する
- 会話ページを進める
- 候補の選択と解除を検証する
- 選択順を管理する
- 3体をpartyへ登録する
- LeftPaneへparty確定を通知する
- 終了時に`MapRunController`へ完了を通知する
- Viewのアニメーションそのものは制御せず、表示状態の切り替えを指示する

### StartScreen

- 博士と候補9体を表示する
- 現在の選択数と選択状態を反映する
- ProfessorLayerとCandidatePanelの表示位置を切り替える
- ユーザー操作を`StartNodeController`へ通知する
- partyやNode状態を直接変更しない

### MapRunController

- StartNodeへ対応する`StartNodeController`を起動する
- 完了通知を受けてStartNodeを解決済みにする
- 次Nodeへの移動を許可する
- MapOverlayを自動で開く

## 会話データ

v0.2では短い固定会話をC#またはScene参照で保持してよい。ただし、`StartNodeController`へ文章を直接埋め込まず、会話データの差し替え口を分ける。

多言語化や大量の会話データを扱う仕組みは、この段階では作らない。

## 仮仕様として確認したい点

- TrainerTabへplayer名以外に何を表示するか
