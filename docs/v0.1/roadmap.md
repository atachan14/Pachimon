# Roadmap

バージョン番号はリリース番号ではなく、開発上の大きな完成区分として扱う。

## Milestones

| Version | 完成対象 | 主な内容 |
| --- | --- | --- |
| v0.1 | Map | Run初期化、300個体生成、Map生成、Node / Edge表示、Node移動、Eventを含むNode画面スケルトン |
| v0.2 | Start | row:0で9体から3体を選択し、partyを生成 |
| v0.3 | Battle | 戦闘進行、Skill、Passive、勝敗、BattleLog |
| v0.4 | Reward | Battle後のGold / Mod / Skill / Passive / Badge取得 |
| v0.5 | RestSpot | 回復処理と進行再開 |
| v0.6 | Item | Itemの仕組みとサンプルItem |
| v0.6.5 | Stat Refactor | 統合属性、MN、Speed、DamageBonus / ResistBonusへの横断移行 |
| v0.7 | City | ShopなどCity内の処理 |
| v0.8 | Content | Skill / Passive / Item追加と、それに必要な仕組み |
| v0.9 | NormalArea | NormalAreaを通して遊べる状態とバランス調整 |
| v1.0 | GhostArea | GhostNode群の生成、進行、完走 |
| v1.1 | Save / Load | Runの保存と再開 |

## 境界

- Reward は Battle 勝利後に発生する独立した処理とする
- Reward の対象は `Gold / Mod / Skill / Passive / Badge`
- Item と City は Reward の完成条件に含めない
- Gym の Badge もReward処理を通して取得する
- Eventの詳細実装を行うマイルストーンは、内容を決めた段階で追加する

## 進め方

- 各マイルストーンで本実装へ接続可能な構造を作る
- 後続機能の詳細まで先行実装しない
- 前提となるデータ構造に問題が見つかった場合は、マイルストーンをまたいで先に修正してよい
