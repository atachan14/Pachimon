# Reward Flow

## 全体フロー

```text
Battle / Gym勝利
  -> Player CurrentHP / CurrentMNをRunへ反映
  -> Reward Windowを上から表示
  -> 「大盤振る舞いだ！」
  -> 4つのReward Buttonを順不同で取得
     -> Gold: 即時取得
     -> 通常Battleのステータス: NodeRewardの2要素を即時取得
     -> GymのBadge: 即時取得
     -> Passive: 選択Windowを表示して候補と取得先を選ぶ
     -> Skill: 選択Windowを表示して候補と取得先を選ぶ
  -> 全4枠取得
  -> Reward Windowを閉じる
  -> Node Clear
  -> Mapを自動表示
```

## Reward Window

- BattleScreen内部のOverlayとして表示する
- 開くときは画面上部からスライドインする
- Labelは`大盤振る舞いだ！`とする
- Reward Buttonは取得後に縮小して消す
- Reward Windowを手動で閉じるButtonは置かない
- 4枠すべて取得するまでNodeを完了しない

通常Battle:

```text
[ Gold ]
[ ステータス ]
[ パッシヴ ]
[ スキル ]
```

Gym:

```text
[ Gold ]
[ バッジ ]
[ パッシヴ ]
[ スキル ]
```

GymはModを持たず、第2枠をBadgeへ置き換える。

## Gold / Mod / Badge

- Goldは`NodeReward.Gold`をRunへ加算する
- Modは`FirstElement / SecondElement`をTrainer単位のModifierへ反映する
- Badgeは属性ごとの所持数と合計所持数をRunへ反映する
- `BonusGold`は基礎Goldとは別の追加Goldとして取得する
- Modの具体的な上昇量は`ModValueSettings`へ分離する
- FirstElementは基準上昇量、SecondElementはその50%とする

現在の仮値:

| Element | FirstElement | SecondElement |
| --- | ---: | ---: |
| 8属性 | +60 | +30 |
| MaxHP | +100 | +50 |
| MaxMN | +100 | +50 |
| Speed | +20 | +10 |
| DamageBonus | +20 | +10 |
| ResistBonus | +20 | +10 |
| BonusGold | +4,000 Gold | +2,000 Gold |

- Badge: 対応属性値を1個につき`+30%`
- Badge倍率は加算式とし、1個で`1.3倍`、2個で`1.6倍`、3個で`1.9倍`
- 実効値は`(基礎値 + Mod加算値) * Badge倍率`の順で計算し、小数点以下を切り捨てる
- MaxHP / MaxMNが増えた場合は、増加量だけ対象Party全員のCurrentHP / CurrentMNも増やす

## Skill / Passive選択

- 選択Windowは何もない状態からスケールインする
- 1つの縦Scroll内へ、Enemy候補を上、Player取得先を下に配置する
- Enemy3体は3Columnで表示する
- 各Enemyの下へ、その個体が戦闘開始時に保持していたSkillまたはPassiveを表示する
- 候補を選ぶとPlayer側3体まで自動スクロールする
- Player側も3Columnで表示する
- Playerは上へスクロールして候補を選び直せる
- Player側の1体を選ぶと、その個体へSkillまたはPassiveを追加する
- 取得成功後、選択Windowを回転させながら縮小して閉じる

### Skill制約

- 1体が保持できるSkillは最大9個
- すでに保持しているSkillも別Slotへ重複取得できる
- 最大数へ到達した個体は取得先に選べない

### Passive制約

- Passive所持数は可変とする
- すでに保持しているPassiveは重複取得できない

## Candidateの導出

- Skill / Passive候補を`NodeContent`へ複製保存しない
- 勝利したBattleのEnemy3体が戦闘開始時に保持していたLoadoutから導出する
- Battle中の一時的な追加・削除はReward候補へ反映しない
