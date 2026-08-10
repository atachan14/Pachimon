# v0.8実装手順

## 技術的な前提作業

[Stat Pipeline Refactor](./technical/stat-pipeline-refactor.md)を参照する。

- `Stage A`: 完了
- `Stage B`: 完了
- Phase 3では完成したStat Pipelineを使って派生Passiveを追加する

## Phase 1: 可変Species構成

1. Speciesへ`isRunEnabled`を追加する
2. 有効なSpeciesだけから300個体を均等に生成する
3. 同じNodeへの同種配置を禁止し、同じRowと隣接Nodeへの配置を可能な範囲で避ける
4. Gym、Elite、属性TrainerのType一致条件を維持する
5. 151種有効時は従来どおり1種を不参加とし、150種を各2個体生成する
6. 32種、40種、151種の構成でMap生成を検証する

## Phase 2: コンテンツ設計

1. 8属性のPachimon、Skill、Passive案を属性別ファイルへ記録する
2. 新しい効果に必要な共通イベントやBattle処理を洗い出す
3. 実装単位と優先順位を決める

## Phase 3: 段階的な実装

1. 8属性4体ずつの最小構成で通し確認する
2. 属性ごとにPachimon、Skill、Passiveを追加する
3. 8属性それぞれ約5体を目安に増やす
4. 追加途中でも定期的にMap生成とBattleを通し確認する
5. 新規Skillには検証用の技マシーンを用意し、`DevelopmentRunProfile.asset`から必要分だけ配布する

初期Itemは9Slot上限を共有するため、すべての実装済みSkillを同時配布しない。新しい仕組みを持つ代表Skillを優先し、確認済みSkillは開発Profileから外す。通常Buildは`ProductionRunProfile.asset`を強制使用する。

## Phase 4: Item

Item追加と、それに必要な仕組みはPachimon系コンテンツの進捗を見て別途整理する。
