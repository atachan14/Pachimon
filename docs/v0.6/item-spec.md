# Item仕様

## Inventory

- 所持上限は9個
- 同種ItemはStackしない
- 同じItemを複数所持した場合も、1個ずつ別Slotへ格納する
- Itemは効果の適用に成功した時点で1個消費する
- 対象不正、効果適用不能、操作キャンセルでは消費しない
- 主な入手経路はCityとEvent

## Item Panel

- HeaderのItemボタンで開閉する
- 画面下部へ3Column x 3RowのGridを表示する
- MainPaneのLogWindowに相当する高さと位置を基準にする
- Mapと同様に、上側からスライドして表示する
- 各SlotにはItem Iconを表示する
- Item Iconを対象PachimonへDrag & Dropして使用する

## 詳細表示

- ExpandedではItem IconのClickでLeftPaneへ詳細を表示する
- CompactではItem IconのLong PressでLeftPaneへ詳細を表示する
- CompactでDragを始めてもLeftPaneを自動展開しない
- Compactでは使用前にLeftPaneの対象Pachimon Tabを選択しておく
- Compact向けの別操作は、必要になった段階で詳細画面からの使用などを検討する

## 使用可能タイミング

- 原則としてBattle中と非Battle中の両方で使用可能
- Battle中はPlayerのSkill入力待ち中のみ使用可能
- Item使用はTurnとTickを消費しない
- Battle Log表示中、演出中、敵行動中は使用できない

## Drop対象

### 非Battle中

- LeftPaneに表示中のPlayer Pachimon
- RightPaneに表示中のEnemy Pachimon
- RightPaneからの事前使用は、進行可否や情報公開状態に限定しない
- 生成済みの全Battle / Gym / Elite NodeのEnemyを対象にできる
- Start直後からEliteへItemを使用することも許可する

事前使用の効果はRun中の`PachimonInstance`へ反映し、Battle開始時の状態へ引き継ぐ。

### Battle中

- MainPaneのPachimon Graphic
- LeftPaneに表示中のPlayer Pachimon
- RightPaneに表示中のEnemy Pachimon

Battle中の効果は進行中の`BattleUnitState`へ反映し、Player側のHPなど永続対象はBattle終了時にRun側へ同期する。

## Skill習得Item

- 同じSkillを複数回習得できる
- Skill上限は合計9Slot
- 同一Skillも取得ごとに別Slotとして保持する
- 同一Skillの各SlotはBattle中に独立したCooldownを持つ
- Map生成時の初期Skill配布は従来どおり同一個体内で重複させない

## 初期実装Item

### きずぐすり

- 対象: 味方Pachimon
- 効果: CurrentHPを300回復
- 最大HPを超えて回復しない
- 戦闘不能からは復活させない

### 石ころ

- 対象: 敵Pachimon
- 効果: CurrentHPへ100の確定ダメージ
- 進行可否や情報公開状態にかかわらず、生成済みNodeの敵へ事前使用できる
- 戦闘不能の対象には使用できない
- 事前使用後のCurrentHPはBattle開始時へ引き継ぐ

## 将来Item

| Item | 対象 | 効果 |
| --- | --- | --- |
| いいきずぐすり | 味方Pachimon | HPを1000回復 |
| 赤巻紙 | 味方Pachimon | Fireを恒久的に50増加 |
| 赤ピーマン | 味方Pachimon | 統合後は赤巻紙と同じ効果になるため、別効果への変更または廃止を検討 |
| 技マシーン[xx] | 味方Pachimon | 対象Skillを1Slot習得 |
| 石ころ | Enemy Pachimon | 100の確定ダメージ |
| 着火剤 | 味方側 | 500tickの間、与えるFireダメージを50%増加 |
