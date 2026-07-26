# v0.6 Item

## 目的

Itemの所持、表示、ドラッグ&ドロップによる対象指定、使用と消費までを実装する。

## 完了条件

1. Itemを最大9個まで、1個につき1Slotで所持できる
2. Headerから3x3のItem Panelを開閉できる
3. `きずぐすり`を味方Pachimonへ使用し、HPを300回復できる
4. Battle中と非Battle中の両方でItemを使用できる
5. 無効な対象へのDropではItemを消費しない
6. ExpandedとCompactで定めた詳細表示操作が機能する
7. `石ころ`を敵へ使用し、100の確定ダメージを与えられる
8. 事前に与えたダメージがBattle開始時へ引き継がれる

## 関連文書

- [item-spec.md](item-spec.md)
- [implementation-plan.md](implementation-plan.md)
