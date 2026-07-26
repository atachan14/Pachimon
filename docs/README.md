# Pachimon Documentation

[`v0.6.5 Stat Refactor`](./v0.6.5/README.md)は実装・通し確認まで完了した。次の実装対象は着手前に決定する。

## 読み始める場所

1. [`v0.6.5/README.md`](./v0.6.5/README.md): 完了したStat Refactor
2. [`v0.6.5/stat-refactor-plan.md`](./v0.6.5/stat-refactor-plan.md): Stat Refactorの実装手順
3. [`v0.1/pachimon-stats.md`](./v0.1/pachimon-stats.md): Statと生成仕様の正本
4. [`v0.1/map-generation.md`](./v0.1/map-generation.md): Map、Reward Deck、個体配置
5. [`v0.3/battle-state.md`](./v0.3/battle-state.md): Run個体とBattle Stateの境界
6. [`v0.3/battle-flow.md`](./v0.3/battle-flow.md): Tick、行動、勝敗の流れ
7. [`v0.3/skill-runtime.md`](./v0.3/skill-runtime.md): Skill、Damage、自動対象
8. [`v0.4/reward-flow.md`](./v0.4/reward-flow.md): Mod / Badge取得
9. [`v0.5/rest-spot-flow.md`](./v0.5/rest-spot-flow.md): HP / MN回復
10. [`v0.6/item-spec.md`](./v0.6/item-spec.md): Item仕様
11. [`backlog.md`](./backlog.md): バージョンをまたぐ改善候補と保留事項

## Archive

[`archive`](./archive/README.md) には、過去バージョンの資料をそのまま保存する。今回整理した資料は `archive/v0.0` にある。

- 過去の検討経緯を確認するための資料であり、現在の仕様ではない
- 原則として内容を更新しない
- 必要な仕様は確認後に `v0.1` へ書き直す
- `v0.1` と矛盾した場合は `v0.1` を優先する

## 運用ルール

- 1つの仕様について正本を1ファイルに絞る
- 確定事項と未決定事項を分けて書く
- 実装が仕様に追いついていない場合は「現状実装」と「目標」を併記する
- 一時的な議論は `decisions.md` に残し、確定後に該当仕様へ反映する
- Backlogはバージョン別に分けず、プロジェクト共通の`backlog.md`へ集約する
