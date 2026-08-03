# Backlog

実装中やテスト中に見つけた改善案を、忘れないために一時的に記録する。

- ここにある内容は未確定仕様を含む
- 方針が確定したら、該当する仕様書と`decisions.md`へ反映する
- 実装まで完了した項目は、このファイルから削除するか完了欄へ移す
- 新しい気づきは完成度を気にせず、まずこのファイルへ追記する

## Status

- `Next`: 次に着手する候補
- `Pending`: 方針決定または前提作業を待つ
- `Later`: 後続マイルストーンで扱う

## Inbox

- Map背景を単色ではなく、進行度や区間が視覚的にわかる画像・パターンにする

- 将来的に、報酬にitemも追加するかも。

- MapにSearch機能をつける
    - Map右上に配置予定
    - 文字列を入力して検索すると、文字列を含むSkillやPassiveやItemを含んだNodeがリストアップされる感じ。詳細は後で。
- battleのUX/UI
    - log送りの表示方法推敲
    - logに表示する内容の精査。

- 各詳細Overlay
    - 状態異常詳細
        - Skill/Passive/Item詳細と同様に表示。それぞれの状態異常が持つ値も表示し、実際の効果や計算式も説明文の中で表示。
    - PassiveやSkill
        - 実際の値を反映した具体的な軽減前ダメージや効果量や、それを算出する計算式を説明文の中で表示。

## Next

### B-007: Trainer以外のMap Node Iconを作る

目的:

- Map全体のIcon表現を比較できる状態にする
- GymをTrainer表示にするかBadge表示にするか判断する材料を揃える
ただし、対象範囲は制限したほうがよさそうです。四天王は最初から公開されているため、公開済みNodeすべてを対象にするとStart直後から石を投げられます。
進捗と仮の制作順:

- [x] City
- [x] RestSpot（外部表現はPachimonCenter施設）
- [ ] Event
- [ ] LeagueGate
- [ ] Start
- [ ] Elite

### B-006: 共通Pachimon仮Graphicを作る

案:

- 仮Pachimonを1種生成する
- 未制作の151Speciesへ同じGraphic参照を設定する
- Speciesごとの本番Graphicが完成したら順次差し替える

進捗:

- [x] Species 1 `パチギダネ`のFront / Back Graphicを制作
- [x] 3体横並び用の幅をPreviewで確認
- [x] 未制作151Speciesへパチギダネを共通仮Graphicとして設定

決定事項:

- FrontはBattleとRightPaneで共用する
- Species制作時はFront / Backを同時に用意する

未決事項:

- MapやStart選択で必要になる小型Iconも同時に用意するか

## Pending

### B-008: Gym Map Iconの最終表現を決める

現状:

- GymLeader用のハット型Iconと金色Role Frameは実装済み
- 現行表現は比較用の仮案として残す

比較候補:

- GymLeaderをIconとして表示する
- 獲得できるBadgeをIconとして表示する
- GymLeader IconへBadgeを補助表示する

他NodeのIconを揃えた後、Map全体の見分けやすさを見て決定する。

## Later

### B-009: Safe Areaへ対応する

発見: `v0.2.5`

- ノッチやホームインジケーターのある端末でHeader・Footer・Drawer操作を妨げないようにする
- 実機確認を行う段階で、Canvas全体ではなく操作UIの退避範囲を決める

### B-010: Compact Headerの仮ラベルを置き換える

発見: `v0.2.5`

- 現在の`PARTY` / `INFO`は機能確認用の仮ラベル
- Header全体の情報整理とGold / BadgeのLeftPane移動案を詰めた後に最終表現を決める

### B-011: Compactの見た目を最終調整する

発見: `v0.2.5`

- Pane内の余白、情報密度、文字と要素の比率を各Node実装後に調整する
- 進行不能や操作不能につながる問題はこの項目へ送らず、その場で修正する

### B-012: Responsive Typographyをイベント駆動へ最適化する

発見: `v0.2.5`

- 現在は実行時生成TMPを拾うため、GameRoot配下を低頻度で定期走査している
- UI要素が大幅に増えて負荷が問題になった場合、生成時登録または更新通知方式へ置き換える

### B-013: LayoutMode往復の回帰テストを行う

発見: `v0.2.5`

- Map / Left Drawer / Right Drawerを開いた各状態でExpandedとCompactを往復する
- Pane内容、最前面状態、StartNodeの進行状態、文字倍率が維持されることを確認する

## 推奨着手順

1. B-007: Trainer以外のMap Node Iconを作成
2. B-008: Gym Map Iconの最終表現を決定
3. B-006: 共通Pachimon仮Graphicを作成
