# v0.8 Content

Pachimon、Skill、Passive、Itemを追加し、Placeholder中心の状態からテストプレイ可能なコンテンツ構成へ移行する。

## 当面の目標

- 実装済みSpeciesだけを使って、既存の300配置枠を埋められるようにする
- 8属性それぞれ約5体、合計約40体の実装を最初の目安とする
- 実装済みSpeciesが少ない段階から、重複個体を使って通しテストできるようにする
- Pachimon、固定Skill、Passiveを属性単位でまとめて設計する

40体は固定の完了条件ではない。コンテンツのまとまりとテスト結果に応じて増減する。

現行のEliteは各PachimonへType一致Skillを3つ追加する。固定Skillとの重複を避けるため、通し生成には各属性最低4体、合計32体のRun参加Speciesを必要とする。

## Documents

- [implementation-plan.md](./implementation-plan.md)
- [technical/mechanics-inventory.md](./technical/mechanics-inventory.md)
- [technical/stat-pipeline-refactor.md](./technical/stat-pipeline-refactor.md)
- [technical/battle-preview-simulation.md](./technical/battle-preview-simulation.md)
- [technical/battle-presentation-timeline.md](./technical/battle-presentation-timeline.md)
- [technical/skill-hit-runtime.md](./technical/skill-hit-runtime.md)
- [technical/passive-data.md](./technical/passive-data.md)
- [technical/run-startup-profiles.md](./technical/run-startup-profiles.md)
- [content/README.md](./content/README.md)
- [content/shared-rules.md](./content/shared-rules.md)
- [content/implementation-questions.md](./content/implementation-questions.md)
- [content/statuses/README.md](./content/statuses/README.md)
