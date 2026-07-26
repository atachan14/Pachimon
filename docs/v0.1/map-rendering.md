# Map Rendering

## 方針

MapはRun開始時に一度だけViewを構築し、HeaderのMapボタンでは表示と非表示だけを切り替える。

```text
Run開始
  -> BuildMap(RunMap)
     -> NodeViewを生成
     -> EdgeViewを生成

Node状態変更
  -> RefreshState(RunState)
     -> 現在 / 解決済み / 選択可能だけ更新

HeaderのMapボタン
  -> Open / Close
     -> 再生成しない
```

数十から百数十程度のNodeで毎回再描画しても致命的な負荷にはなりにくいが、今回のMapはRun中にSceneをまたがず構造もほぼ変わらないため、初回構築と状態更新を分ける。

## 推奨Hierarchy

```text
MainPane
├─ GraphicWindow
├─ LogWindow
└─ MapViewport
   └─ MapOverlayView
      └─ MapScrollRect
         └─ ScrollViewport
            └─ MapContent
               ├─ EdgeLayer
               └─ NodeLayer
```

- `MapViewport`はStretch / StretchでMainPane全体を覆い、`Ignore Layout`と`RectMask2D`を持つ
- `MapOverlayView`もStretch / Stretchとし、固定Heightを持たせない
- `MapOverlayView`の`CanvasGroup`で、非表示中と開閉アニメーション中の入力を無効化する
- `MapScrollRect`は縦方向だけを操作できる`ScrollRect`を持つ
- `ScrollViewport`は`RectMask2D`を持ち、約6row分だけを表示する
- `MapContent`はMap全体の座標系とスクロール範囲を持つ
- `EdgeLayer`を先に描き、Nodeの背面へ置く
- `NodeLayer`にNodeViewを配置する
- MapはMainPane内だけに表示し、`RectMask2D`によってHeaderや左右Paneへのはみ出しを隠す

## 開閉

- 表示位置は`anchoredPosition = (0, 0)`
- 非表示位置は`anchoredPosition = (0, MapViewportの高さ)`とし、上側へ完全に隠す
- 親の高さから移動量を計算するため、画面サイズごとの固定Heightは不要
- 開閉中および非表示中は`CanvasGroup.interactable`と`blocksRaycasts`を`false`にする
- UIの前後関係にPos Zは使わず、Hierarchyの描画順と`RectMask2D`で制御する

Hierarchyは`Tools > Pachimon > UI > Setup Map Scroll View`から自動構築できる。既存オブジェクトがある場合は再利用するため、再実行しても同名オブジェクトを増やさない。

## Map内スクロール

- Mapは最大6columnを横幅全体に収め、横スクロールは使用しない
- `MapContent`は全row分の高さを持ち、スワイプまたはマウスホイールで縦に操作する
- Mapを開くたび、現在Nodeを表示域の下から38%付近へ合わせる
- Run開始時にNodeとEdgeを一度生成し、開閉時には再生成しない

## Node配置

`rowIndex / columnIndex`はMapの論理構造として保持し、表示座標だけに決定的な揺らぎを加える。

- 基準X座標は最大6columnの間隔から計算する
- Node数が6未満のrowは、そのrow全体を中央寄せする
- 横揺らぎはcolumn間隔の最大22%
- 縦揺らぎはrow間隔の最大18%
- Start Nodeは中央に固定し、縦揺らぎも加えない
- Elite Nodeは中央に固定し、row 37-40を揺らぎなしの等間隔で表示する
- Cityの2接続点は同じY座標にそろえ、通常のcolumn間隔の55%まで近づける
- `runSeed + nodeId`から揺らぎを生成し、Mapを開き直しても位置を変えない
- 揺らした後のNode座標をEdge描画にも使用する

横揺らぎをcolumn間隔の半分未満、縦揺らぎをrow間隔の半分未満に制限することで、Nodeの左右順やrow順を逆転させない。

## View

### MapOverlayView

- `BuildMap(RunMap)`を一度実行する
- `RefreshState(RunState)`で表示状態だけを更新する
- `Open / Close`は位置アニメーションとCanvasGroupの入力可否だけを扱う
- `nodeId -> MapNodeView`のDictionaryを保持する
- `groupId -> CityMapNodeView`のDictionaryを保持する

### MapNodeView

- Battle / Gym / Eliteでは文字ラベルの代わりに`TrainerMapIconView`を表示する
- Trainer Iconを表示するNodeは背景を透明にし、56 x 56のNode Rect全体へInsetなしでIconを表示する
- 透明なRoot Imageは56 x 56のクリック判定として残す
- Edgeは`EdgeLayer`へ描画されるため、透明余白では見え、Iconの不透明部分の背面へ隠れる
- Battleは`NodeReward.FirstElement`を上部色、`SecondElement`を下部色に使う
- Gymは`TrainerMapIconCatalog`からハット型IconSetを取得し、ハットと服のPrimaryへBadge属性色を使う
- Gym Iconは通常Trainerの1.05倍で表示する
- Gymには濃い輪郭付きの細い金色八角形`GymRoleFrame`を常時表示する
- `GymRoleFrame`は背景を塗らず、Roleを示す装飾としてIcon背面へ置く
- current / selectable / selected用の`TrainerSelectionFrame`はRole Frameより外側の状態表示として併用する
- Eliteは`TrainerStyle.Theme`に対応する属性色を上下へ使う
- Icon用Assetまたは配色元を取得できない場合は、従来の文字ラベルへフォールバックする
- Trainer Icon Nodeのcurrent / selectable / selectedはIcon外側の`TrainerSelectionFrame`で表現する
- EventはRoot背景を透明にし、白塗りの円へオレンジの円形リングと中央の`？`を表示する
- 円の外側だけを透過し、円の内側では背面のEdgeを隠す
- Eventの円形リングはドット絵にせず、高解像度画像をBilinearで縮小して滑らかに表示する
- RestSpotは白塗りの円へ緑の円形リングと中央の`＋`を表示する
- EventとRestSpotは同じ記号系Nodeレイヤーを共有し、Spriteと文字だけを切り替える
- Eventのcurrent / selectable / selectedは円形リングのOutlineで表現する
- Trainer Iconを使わないNodeは従来どおりRoot背景のOutlineで状態を表現する
- `nodeId`
- Node種別の見た目
- 現在状態
- 解決済み状態
- 選択可能状態
- 選択イベント

NodePointを土台にして別Prefabを重ねるより、1つの`MapNodeView`がNode種別に応じてアイコンや色を切り替える構成を基本とする。Gymなど構造が大きく異なる場合だけ派生Prefabを検討する。

Trainer Iconを使わないNodeは、Node種別を短い文字と色で表現する仮デザインを継続する。最終アイコンへ差し替える場合も、Node種別ごとにPrefabを分けず`MapNodeView`内の表示設定を変更する。

BattleNodeのIcon色は`TrainerColorSchemeResolver`を介して`NodeReward`から決定する。増額GoldもRewardElementの1種としてGold色を適用する。

### CityMapNodeView

- グラフ上の横並び2Nodeを、表示上は1個の横長Cityとして描画する
- 左右のMapNode座標はEdgeの接続点として残す
- City本体は2接続点の中央へ配置する
- current / resolved / selectableは`MapNodeGroup`内のNode状態をまとめて表示する
- 同じ外部Nodeと同じCityグループを結ぶ複数の内部Edgeは、表示上1本へまとめる
- Cityから同じ外部Nodeへ伸びる複数の内部Edgeも、表示上1本へまとめる
- 統合後もCityへのIncoming / CityからのOutgoingをそれぞれ最低2本表示する
- 表示候補が複数ある場合は、表示座標上の距離が最短になるCity接続ポートを採用する
- City本体には、2Node幅の近未来都市を描いた横長の専用画像を使用する
- 採用画像は`Assets/Art/Map/Nodes/City/city_map_icon_112.png`とし、編集用に`224x112`のマスターも保持する
- 論理サイズとEdge接続点は`112x56`のまま、City画像だけ通常時`1.1`倍で表示する

### MapEdgeView

- 接続元Nodeと接続先Nodeを結ぶ
- 選択可能経路や通過済み経路の表示差分を後から追加できるようにする
- 最初は単純な線でよい

## 座標

- `rowIndex`から進行方向の座標を決める
- `columnIndex`から横方向の座標を決める
- 行間隔と列間隔はMapOverlay側のレイアウト設定として持つ
- スマホでは縦方向に進むMapを基本とする

## 状態判定

### current

`RunState.currentNodeId`と一致するNode。

### resolved

`RunState.resolvedNodeIds`に含まれるNode。

### selectable

現在Nodeの処理が完了し、かつ対象Node IDが現在Nodeの`nextNodeIds`に含まれる場合。

進行可否はMapOverlayが独自判断せず、MapRunControllerから受け取る形にする。

選択可能でないNodeもクリックしてRightPane上の情報を閲覧できる。ただし進行用Footerは表示せず、現在Nodeは変更しない。

## Node選択

1. MapNodeViewが自身のNode IDを通知する
2. MapRunControllerが接続先かつ進行可能か検証する
3. 正しければ仮選択Nodeとして保持し、Map上で強調する
4. RightPaneにNode情報と「決定」「キャンセル」を表示する
5. 「決定」時に接続と進行可能状態を再検証する
6. 正しければRunStateの現在Nodeを更新する
7. Mapを閉じ、対象Node画面を起動する

「キャンセル」またはHeaderからMapを閉じた場合は、仮選択を破棄して現在Nodeを変更しない。

RightPaneには現時点で保持している範囲で、Node種別、Stage、Trainer、Gold、Mod、Badge、敵Pachimonなどを表示する。未実装Nodeの詳細は仮テキストでよい。

Node画面の「次へ進む」は直接次Nodeへ移動せず、現在Nodeを完了状態にしてMapを自動表示する。Map上で選択可能になった接続先を選ぶことで移動する。

## 現状実装

- `MapLayoutCalculator`が全Nodeの表示座標を決定する
- `MapOverlayView`がNode / Edge Viewを初回だけ生成する
- 画面サイズ変更時はPrefabを再生成せず、座標とContentサイズだけを更新する
- `MapRunController`が進行可能なNode IDを渡す
- Node選択時は`MapRunController`が接続と進行可能状態を再検証する
- 選択しただけでは移動せず、RightPaneの「決定」で移動する
- RightPaneの選択UIはEditorツールがHierarchyへ生成する
- 標準`MapNodeView.prefab / CityMapNodeView.prefab / MapEdgeView.prefab`はEditorツールが生成する

`MapOverlayView.Render()`のテキスト出力は、Prefabや参照が未設定の場合のデバッグ用フォールバックとして当面残す。
