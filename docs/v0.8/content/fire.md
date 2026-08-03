# Fire Content

## Pachimon

### [Pachimon名]2

- Status: `Implemented`
- Species ID: `9`
- モチーフ:
- 狙い:

#### Fixed Skill

- 名前: バックファイア
- Implementation: `Implemented`
- 硬直: `100`
- CD: `200`
- MN: `100`
- 対象: 最後尾の敵
- 効果:
  - 次のFireダメージを与える
  - Poisonに応じた貫通率を持つ

```text
ダメージ
= 100 × AmplificationMultiplier(Fire × 100%)

貫通率
= 10 × AmplificationMultiplier(Poison × 100%)
```

- `BasePower / FireScalingPercent / BasePenetrationPercent / PoisonScalingPercent`はSOで調整する

#### Passive

- 名前:闇の炎
- 効果:
炎ダメージを与えたとき、与えた軽減前ダメージの `(毒依存の式)` % の追加毒ダメージを同じ対象に与える。

### [Pachimon名]3

- Status: `Idea`
- Species ID:
- モチーフ:
- 狙い:
#### Fixed Skill

- 名前:チェインバーン
- 連鎖: 1回
- speed: 130
- CD: 250
- 効果:
[敵の先頭] に base:80 の炎ダメージを与える。
2回発動するごとに、自分に[状態異常:アドチェイン]を1スタック付与する

##### [連鎖]
与えた軽減前ダメージや状態異常を下記例のような割合で次の対象に与える（最後尾の場合は逆順になる）。

例：
連鎖: 5回 で、敵が 3体 の場合（小数誤差適当）
e1に100%のダメージを与え（本体分のダメージ）、
e2に83%のダメージを与え（1回目）、
e3に66%のダメージを与え、
e2に50%のダメージを与え、
e1に33%のダメージを与え、
e2に16%のダメージを与える。

連鎖: 3回 で、敵が 1体 の場合
e1に100%のダメージを与え、
e1に75%、
e1に50%、
e1に25%。

##### [アドチェイン]
連鎖の回数がスタック数分増える。（連鎖:0のスキルにも適応され、連鎖するようになる）
効果時間/消費タイミング：なし（Battle中、恒久的に増え続ける）

#### Passive

- 名前:コンボマスター
- 効果:
Battle中の最大連続 連鎖 回数に応じてDamegeBonusが上昇する。

### [Pachimon名]4

- Status: `Idea`
- Species ID:
- モチーフ:
- 狙い:

#### Fixed Skill

- 名前:炎の障壁
- 効果:
[自陣のフィールド] に`value=炎依存` の [生成物：炎の障壁]を生成する。

[生成物：炎の障壁]
value依存のHPとvalue依存の効果時間を保持し、味方への全ての攻撃を代わりに受ける。
攻撃を受けた際、攻撃者にvalue依存の[状態異常：火傷]を与える。
HPが消滅するか、もしくは効果時間経過後に消滅する。

##### [状態異常：火傷]
DamageBonusを減少する。自分のTurn終了時に全ての火傷を破棄する。(次のTurnに与えるダメージが減少する)

#### Passive

- 名前:追い打ち
- 効果:火傷している対象へのダメージが増加する。


### [Pachimon名]5

- Status: `Implemented`
- Species ID: `33`
- モチーフ:
- 狙い:

#### Fixed Skill

- 名前: ファイアアロー
- Implementation: `Implemented`
- 硬直: `100`
- CD: `250`
- MN: `100`
- 対象: 生存中でCurrentHPが最も低い敵
- 効果:
  - `100 × AmplificationMultiplier(Fire × 100%)`のFireダメージを与える
  - 対象を戦闘不能にした場合、MNを再度消費して再発動する
- 補足仕様:
  - CurrentHPが同値なら前方の敵を優先する
  - 再発動ごとに対象を再選択する
  - MN不足、対象なし、使用者の戦闘不能、または対象を戦闘不能にできなかった場合は終了する
  - CDと硬直は最初の使用時に一度だけ適用する
  - `BasePower / FireScalingPercent`はSOで調整する

#### Passive

- 名前:ファイアアーチャー
- 効果:ダメージを与える際、対象の減少体力に応じて追加の炎ダメージを与える。

### [Pachimon名]6

- Status: `Implemented`
- Species ID: `41`
- モチーフ:
- 狙い:

#### Fixed Skill

- 名前: 燃焼
- Implementation: `Implemented`
- 硬直: `100`
- CD: `300`
- MN: `100`
- 対象: 先頭の敵と自身
- 効果:
  - 先頭の敵と自身に、それぞれ`100 × AmplificationMultiplier(Fire × 100%)`のFireダメージを与える
  - 両者が生存し、MNを追加消費できる間は再発動する
- 補足仕様:
  - 1回の発動で敵へのDamage、自傷Damageの順に両方を解決する
  - 敵が戦闘不能になった発動でも自傷Damageは発生する
  - 自傷は自身を対象にした攻撃として扱う
  - 自傷にもDamageBonus、与ダメージPassive、被攻撃Passiveを適用する
  - 自傷は自身のFireとResistBonusによる軽減を受ける
  - 再発動してもCDと硬直は初回の一度だけ適用する
  - 敵と自身は共通の`BasePower / FireScalingPercent`をSOで調整する
  - Previewは現行どおり、全発動分をまとめた最終変化を表示する

#### Passive

- 名前:燃える男
- 効果:ダメージを受けるたびに、自身の炎を増加させる。

### [Pachimon名]7

- Status: `Idea`
- Species ID:
- モチーフ:
- 狙い:

#### Fixed Skill

- 名前:にほんばれ
- 効果:
フィールドにValue=炎依存の[天気:晴れ] を 与える。

##### [天気]
敵味方含め、全員に効果。
Valueを持ち、Valueはtick経過で減少し、Valueが0になると消滅する。
同じ天気が追加された場合、Valueを合算する。
違う天気が追加された場合、それぞれの天気は同時に効果を発動しつづける。

##### [天気：晴れ]
Valueに応じて、ダメージ軽減には適応されない炎パラメーターが増加し（炎ダメージや炎を参照した効果が増加するが、ダメージ軽減には反映されない）、ダメージ軽減には適応されない水パラメーターが減少する。

#### Passive

- 名前:晴れ男
- 効果:[天気：晴れ]の減少速度を低下させる。

## Ideas
