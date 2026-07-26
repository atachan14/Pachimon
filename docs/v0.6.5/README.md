# v0.6.5: Stat Refactor

状態: 完了（実装・通し確認済み）

v0.6.5の完成目標は、既存機能を維持したまま、PachimonのStat、Map Reward、Battle、UIを新しいStat構造へ統一すること。

v0.7 Cityへ進む前の横断的な基盤整理として扱う。

## 対象

- 属性Power / Resistを8つの統合属性へ変更
- TurnHasteをSpeedへ改名
- Hasteを個体Statとして採用。ただしHaste Modは生成しない
- UniversalPower / UniversalResistをDamageBonus / ResistBonusへ改名
- MaxMN / CurrentMNをRun永続Resourceとして追加
- Pachimon生成、Map Reward、Modifier、Badge、Battle Damage、RestSpot、UIを新Statへ対応
- 旧Enumを参照するScene / Assetを新構造へ移行

## 対象外

- MNを消費する本番Skillの追加
- StatとModの最終バランス調整
- Cityの実装
- Save / Load互換

## 完成条件

1. 旧Stat名への実行時参照が残っていない
2. 300個体を新Stat構造で再現可能に生成できる
3. First / Second Reward Deckが新しい69要素構成になる
4. Battle前後でCurrentHP / CurrentMNを正しく引き継げる
5. Speed、DamageBonus、属性防御、ResistBonusがBattleへ反映される
6. SidePane、Battle、Reward、RestSpotの表示と処理が新Statへ対応する
7. GameSceneとTitleSceneから開始し、Console Errorなく既存フローを確認できる

## 実装手順

詳細は[`stat-refactor-plan.md`](./stat-refactor-plan.md)を参照する。

## 仕様の正本

- Statと生成: [`../v0.1/pachimon-stats.md`](../v0.1/pachimon-stats.md)
- Map Rewardと個体配置: [`../v0.1/map-generation.md`](../v0.1/map-generation.md)
- Battle State: [`../v0.3/battle-state.md`](../v0.3/battle-state.md)
- Battle Flow: [`../v0.3/battle-flow.md`](../v0.3/battle-flow.md)
- Skill Damage: [`../v0.3/skill-runtime.md`](../v0.3/skill-runtime.md)
- Reward取得: [`../v0.4/reward-flow.md`](../v0.4/reward-flow.md)
- RestSpot: [`../v0.5/rest-spot-flow.md`](../v0.5/rest-spot-flow.md)
- Item: [`../v0.6/item-spec.md`](../v0.6/item-spec.md)
