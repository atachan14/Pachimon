# Enemy Trainer Scaling

## Row補正

敵Trainerの全Pachimonへ、Nodeの`RowIndex`に応じたTrainerStatusを加算する。

| Stat | 加算値 |
| --- | ---: |
| 8属性 | `-40 + RowIndex * 8` |
| MaxHP / MaxMN | `(-40 + RowIndex * 8) * 8` |

SubStatへは直接Row補正を加えない。属性のRow補正は、各個体で対応するSubStatへ基礎対応率`100%`で反映される。

通常Battleでは、Nodeの報酬用TrainerStatusもRow補正へ加算する。

## GymLeader

- 通常のRow補正を適用する
- Badge属性を得意属性として`+50`する
- 得意属性以外の1属性を弱点として`-50`する
- Badge倍率は、上記の加算後に適用する
- 全24 Gymで各属性が弱点として3回ずつ登場する
- 得意属性と弱点属性は一致させない

## Elite

- 通常のRow補正を適用する
- 8属性をさらに`+50`する
- MaxHP / MaxMNをさらに`+400`する
- 得意属性をさらに`+150`する
- 弱点属性を`-150`する
- 4体の得意属性はRunごとに重複なしで選ぶ
- 選ばれなかった4属性を、弱点として1つずつ重複なしで割り当てる
- 全属性Badgeの倍率は、上記の加算後に適用する

## 調整値

仮値は`EnemyTrainerScalingSettings`へ集約する。
得意属性と弱点属性はMap生成時に`TrainerProfile`へ保存し、
RightPane表示とBattleで同じ補正結果を使用する。
