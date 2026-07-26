# Start Presentation

StartNodeの見せ方に関する初稿。進行ロジックと切り離し、実際に画面を見ながら変更できる仮仕様として扱う。

## 使用領域

既存のMainPane構造を利用する。

```text
MainPane
├─ GraphicWindow
│  └─ StartScreen
│     └─ PanContent
│        ├─ CandidatePanel  左側
│        └─ ProfessorLayer  右側
└─ LogWindow
   ├─ TextLog
   └─ SelectGrid
```

- `ProfessorLayer`と`CandidatePanel`を画面幅以上の横並びにする
- 初期表示域は右側の`ProfessorLayer`に合わせる
- 候補選択開始時に表示域を左へ移し、`CandidatePanel`を表示する
- 実装上扱いやすい場合は共通親のパンではなく、2要素を別々に同じ距離だけ動かしてよい
- 会話本文と会話送りは共通の`LogWindow`を使う
- 候補9体の主表示は`GraphicWindow`内の`CandidatePanel`を使う
- `LogWindow.SelectGrid`は決定操作など、少数の操作ボタンに利用する

## 画面遷移案

### 1. Start開始

1. `StartScreen`を表示する
2. 最初から博士を表示する
3. `LogWindow`へ`よく来たね、[名前]`と表示する

博士画像:

- [`../../Assets/Art/Characters/Professor/professor.png`](../../Assets/Art/Characters/Professor/professor.png)

### 2. 導入会話

- クリックまたは会話送りボタンで次の台詞へ進む
- タップすると`ここに9匹のパチモンがおるじゃろ\n3匹選びなさい`へ進む
- 2つ目のメッセージ表示と同時に候補側へパンする
- 表情差分はv0.2の必須要件にしない

### 3. 候補選択へ切り替え

1. `PanContent`を横へ移動する
2. 右側の博士をスライドアウトし、左側の`CandidatePanel`をスライドインする
3. 候補9体を3column x 3rowで表示する
4. `LogWindow`には2つ目のメッセージを表示したままにする

スマホ幅で3columnが窮屈な場合は、カードの情報量を減らして3columnを維持する。縦スクロール化は実画面を確認してから判断する。

### 4. 候補選択

候補カードの仮構造:

```text
CandidateCard
├─ FrontGraphic
├─ NameText
└─ SelectionOrderText
```

- CandidatePanelではPachimonのFront Graphicを使う
- 候補カードを押すとRightPaneへ詳細を表示する
- CandidatePanel上のクリックだけでは選択しない
- 選択済みカードをグレー表示し、下部に`n匹目`と表示する
- RightPane Footerの`n匹目にする`または`n匹目を取り消す`で状態を変更する
- RightPaneには9体分のTabを横並びで表示し、横スワイプに対応する
- RightPaneの詳細表示は公開済みBattleNodeのPachimon表示を再利用する

RightPane Footer:

```text
未選択: [ キャンセル ] [ n匹目にする ]
選択済: [ キャンセル ] [ n匹目を取り消す ]
```

### 5. 3体目の選択

1. CandidatePanelとRightPaneの候補入力を無効にする
2. 未選択の6体をフェードアウトする
3. 選択した3体を少し拡大する
4. 選択順に3体を整列する
5. LogWindowを`この3匹でよろしいか`へ更新する
6. `[ はい ] [ いいえ ]`を表示する

- `いいえ`: 9体表示へ戻し、選択状態をすべて解除する
- `はい`: 3体をpartyへ登録し、最終メッセージへ進む

### 6. Start完了

1. `はい`の後にLogWindowを`バッジを8つ以上集めて、パチモンマスターを目指すのじゃ！`へ更新する
2. 最終メッセージを読み終える操作でStartNodeを完了する
3. 既存のMapOverlay開閉演出でMapを自動表示する

StartScreenからMapへ独自の画面遷移を追加せず、既存のMapOverlayを利用する。

## アニメーション設定案

数値はInspectorから調整できる設定値にする。

| 設定 | 初期値案 |
| --- | ---: |
| スライド時間 | 0.35秒 |
| フェード時間 | 0.20秒 |
| 候補カードの時間差 | 0.03秒 |
| スライド距離 | GraphicWindowの表示幅 |

- パン移動には`RectTransform.anchoredPosition`を使う
- 候補のフェードには`CanvasGroup.alpha`を使う
- 解像度に依存する固定の画面外座標を直接指定しない
- Animation中は対象UIへの入力を無効にする
- HeaderのMapボタンは無効にしない
- アニメーション終了をゲーム進行条件にしない
- Mapを開いている間にアニメーションが完了しても、会話や選択は自動で進めない

外部Tweenライブラリは現時点では導入せず、必要性が見えてから再検討する。

## 初期化

StartScreen表示直後の1frameだけ博士や候補が見える状態を避ける。

- `Awake`または初期化処理でPanContentを博士側の初期位置へ設定する
- ProfessorLayerは最初から表示し、CandidatePanelは画面外左側へ置く
- `OnEnable`だけを起点に毎回演出を再生しない

MapOverlayを閉じてStartScreenへ戻った場合は、現在の進行状態をそのまま描画し、導入演出を再生しない。

## 演出スキップ

v0.2では次の軽量な挙動を仮採用する。

- スライド中に会話送り操作を受けた場合、現在のスライドを終了位置まで即時反映する
- 同じ操作で次の台詞までは進めない
- 専用のSkipボタンは作らない

誤操作が増える場合は、スライド中の会話送りを無視する方式へ変更する。

## 実装順

1. `StartNodeController`の状態遷移を実装する
2. 博士とLogWindowだけで導入から完了まで通す
3. `CandidatePanel`と候補カードを追加する
4. 3体選択とparty登録を接続する
5. Map開閉中の状態維持を確認する
6. スライドとフェードを追加する
7. 実画面を見て配置、速度、台詞を調整する

## 仮仕様として確認したい点

- 候補9体を3column x 3rowで常時表示できるか
- 会話送りを画面クリックにする
- 最終メッセージ表示後の終了操作をタップにする