# v0.8.1 Playtest Build

Editorでの確認待ちは[`editor-test-checklist.md`](./editor-test-checklist.md)にまとめる。

## 目的

64Species構成でNormalAreaをExpanded表示から通して遊べる状態にし、
itch.ioで最初の限定Webプレイテストを行う。

`v0.8.1`はコンテンツ完成版ではない。進行、難易度曲線、Cityによる強化、
Web Buildの成立をまとめて確認するためのテスト公開版とする。

## 実装順

1. 全8属性を各8Species、合計64Speciesまで追加する
   - 攻撃Skillの割合を確認し、不足する属性では直接Damageを与えるSkillを優先する
2. Run参加フラグを切り替え、実装済み64SpeciesだけでMapを生成する
   - 既存の300個体配置枠は64Speciesの重複個体で満たす
   - Gym、Elite、属性TrainerのType一致条件を確認する
3. Item使用時のUI / UXを調整する
4. 状態詳細Overlayを実装する
5. row補正なしで1Run確認し、基準となる難易度推移を記録する
6. rowごとの敵強化を実装し、仮調整する
7. Cityへランダムな技マシーンとMNポーションを追加する
   - MNポーションの仮実装は完了（各City 10個、最大MNの50%回復）
   - きずぐすりも最大HPの50%回復へ変更済み
   - 各Cityへ無属性技マシン1個と属性技マシン1個を配置する
   - RightPaneでは大Categoryと小Categoryの二段Accordionで在庫を整理する
   - RightPaneは滞在状態を問わず全在庫を閲覧できる購入不可の一覧とする
   - MainPaneへ薬局 / 技インストラクター / 刻印屋 / 装備屋を実装する
   - MainPaneでの購入結果をRightPaneの売り切れ表示へ即時反映する
8. Expanded表示で通しプレイし、進行不能と難易度を調整する
9. Web Buildを作成する
10. itch.ioへ限定公開し、デスクトップブラウザーで確認する
11. 発見した問題を修正して`v0.8.1`を公開する

## Item使用UI

- 使用成功時と使用不可時の結果を専用Messageで表示する
  - Item Panelが既存LogWindowを覆うため、既存LogWindowは使用しない
- ドラッグ中はItem画像を移動せず、ドラッグ元から対象へ矢印を伸ばす
- ドラッグ元ItemをBorderで強調する
- 使用可能なドロップ先もBorderで強調する

## 状態詳細Overlay

- Skill / Passive / Item詳細と同じ共通Overlayを使用する
- 状態名、現在Value、残り時間、効果説明を表示する
- 実際の効果量と計算式へ値を反映できる構造にする
- Side PaneとField情報に表示された状態から開けるようにする

## 通し確認

- TitleからRunを開始できる
- StartNodeで3体を選択できる
- NormalAreaのNodeを進行できる
- Battle、Reward、RestSpot、Cityを経由できる
- 技マシーンとMNポーションを購入・使用できる
- LeagueGateへ到達できる
- 複数のRun Seedで進行不能や例外が発生しない
- Development用初期ItemがProduction Profileへ混入しない

## Web確認

- `index.html`を含むWeb Buildが起動する
- itch.ioの限定公開ページから起動する
- ChromeとFirefoxで基本フローを確認する
- 画面リサイズとフルスクリーンを確認する
- ブラウザーConsoleに進行を妨げる例外がない
- 読み込み時間、Build容量、長時間プレイ時のメモリを確認する

## 対象外

- Compact表示の最終調整
  - 進行不能になる問題だけは修正する
- モバイルブラウザー正式対応とitch.ioの`Mobile Friendly`設定
- EventNodeの詳細実装
  - `v0.8.2`以降で扱う
- Save / Load
- 最終バランス調整
