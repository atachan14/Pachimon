# v0.4 Reward

## 目標

Battle / Gym勝利後にBattle Rewardを表示し、すべての報酬を取得してからNodeを完了する。

## 対象

- Gold
- ModまたはBadge
- Passive
- Skill

ItemとCityはv0.4の対象外とする。Eliteは現行仕様どおりBattle Rewardを持たず、勝利後にNodeを完了する。

## 完成条件

1. Battle勝利後にReward Windowが上から表示される
2. 通常Battleでは`Gold / ステータス / パッシヴ / スキル`を表示する
3. Gymでは`Gold / バッジ / パッシヴ / スキル`を表示する
4. GoldとModまたはBadgeはButton選択で即時取得する
5. Skill / PassiveはEnemy候補とPlayer取得先を選んで取得する
6. 取得済みButtonは縮小して消える
7. 4枠すべて取得するとReward Windowを閉じ、Nodeを完了してMapを開く

