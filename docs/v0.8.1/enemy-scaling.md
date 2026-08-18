# Enemy Trainer Scaling

## Row補正

敵Trainerの全Pachimonへ、Nodeの`RowIndex`に応じたTrainerStatusを加算する。

| Stat | 加算値 |
| --- | ---: |
| 8属性 / Speed / Haste / DamageBonus / ResistBonus | `RowIndex * 4` |
| MaxHP / MaxMN | `RowIndex * 20` |

通常Battleでは、Nodeの報酬用TrainerStatusもRow補正へ加算する。

## GymLeader

- 通常のRow補正を適用する
- Badge属性を得意属性として`+100`する
- 得意属性以外の1属性を弱点として`-100`する
- Badge倍率は、上記の加算後に適用する
- 全24 Gymで各属性が弱点として3回ずつ登場する
- 得意属性と弱点属性は一致させない

## Elite

- 通常のRow補正を適用する
- 全Statをさらに`+100`する
  - MaxHP / MaxMNは5倍の`+500`
- 得意属性をさらに`+300`する
- 弱点属性を`-300`する
- 4体の得意属性はRunごとに重複なしで選ぶ
- 選ばれなかった4属性を、弱点として1つずつ重複なしで割り当てる
- 全属性Badgeの倍率は、上記の加算後に適用する

## 調整値

仮値は`EnemyTrainerScalingSettings`へ集約する。
得意属性と弱点属性はMap生成時に`TrainerProfile`へ保存し、
RightPane表示とBattleで同じ補正結果を使用する。
