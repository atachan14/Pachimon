# v0.2: Start

v0.2の完成目標は、タイトル画面でplayer名を決め、`row:0`のStartNodeで博士の案内を受け、候補9体から3体を選択してpartyを確定し、最初の移動先を選べる状態にすること。

## Status

主要フローと動作確認は完了。スマホ向け縦長`Compact`とPC向け横長`Expanded`の基盤はv0.2.5で実装済み。

v0.1で完成したRun生成とMapを前提に、StartNodeの本処理だけを追加する。

## スコープ

- StartNode専用の進行状態とController
- タイトル画面でのplayer名入力
- 名前未入力時の`ゲスト`設定
- 博士による導入会話
- 事前配置済みの候補9体の表示
- 候補9体から重複なく3体を選択するUI
- 選択内容の確認とpartyへの登録
- LeftPaneでのplayer Trainerとparty 3体のTab表示
- 博士による最終メッセージ
- StartNodeの完了通知
- StartNode完了後のMapOverlay自動展開
- Start処理中にMapOverlayを開閉しても進行を維持すること
- GameSceneを直接起動した場合も同じStart処理を開始すること

## v0.2では完成させないもの

- Battleの本処理
- Skill / Passiveの詳細表示と操作
- Pachimonの編成変更
- 博士の最終台詞と文章演出の磨き込み
- 最終版のアニメーション時間、SE、BGM、画面効果
- Save / Load途中からのStart再開
- トレーナーカードの作成と編集

トレーナーカードはv1.0以降のアップデート要素として扱い、v0.2では名前入力だけを実装する。

Battleに必要なpartyはv0.2で確定するが、戦闘中のHPなどはv0.3で扱う。

## 完成条件

1. タイトル画面でplayer名を入力してNew Gameを開始できる
2. 名前が空の場合は`ゲスト`として開始する
3. New Gameで博士が表示されたStartNodeの導入会話から開始する
4. StartNodeに事前配置された9体が表示される
5. RightPaneで各候補の詳細を確認できる
6. 選択順を保持しながら0から3体まで選択と選択解除ができる
7. 3体目の選択後に3体だけを並べた確認画面へ移る
8. 確認で「いいえ」を選ぶと、選択をすべて解除して最初から選び直せる
9. 選択確定前のLeftPaneではparty 3枠を`？`で表示する
10. 確認で「はい」を選ぶと、3体が`RunState.party`へ一度だけ登録される
11. 同時にLeftPaneの3枠が選択したPachimon表示へ切り替わる
12. 同時に最終メッセージと`おう`ボタンを表示する
13. `おう`を押すとStartNodeが解決済みになる
14. `row:1`の3Nodeが移動可能になり、MapOverlayが自動で開く
15. Start処理中にMapを開閉しても、会話位置と選択内容が失われない
16. StartNode完了前は次Nodeへ移動できない

## 前提となるv0.1仕様

- [`../v0.1/run-flow.md`](../v0.1/run-flow.md)
- [`../v0.1/map-data-model.md`](../v0.1/map-data-model.md)
- [`../v0.1/pachimon-catalog.md`](../v0.1/pachimon-catalog.md)
- [`../v0.1/skill-spec.md`](../v0.1/skill-spec.md)

v0.1の文書はMap完成時点の記録として残し、v0.2で変更する内容はこのフォルダへ記録する。

## 読む順番

1. [`start-flow.md`](./start-flow.md)
2. [`start-presentation.md`](./start-presentation.md)
3. [`implementation-plan.md`](./implementation-plan.md)
