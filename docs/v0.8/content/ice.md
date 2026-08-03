# Ice Content

## Pachimon

### [Pachimon名]2

- Status: `Idea`
- Species ID:
- モチーフ:
- 狙い:

#### Fixed Skill

- 名前:氷の盾
- 効果:先頭の味方にシールドを付与

#### Passive

- 名前:
- 効果:受ける氷ダメージが減少

### [Pachimon名]3

- Status: `Idea`
- Species ID:
- モチーフ:
- 狙い:

#### Fixed Skill

- 名前:アイスシャード
- 効果:
先頭の敵にダメージと[冷気](./statuses/slow.md#冷気)を付与する。
先頭以外の敵にダメージと[冷気](./statuses/slow.md#冷気)を付与

#### Passive

- 名前:
- 効果:対象に付与されているSlowに応じて、自身の与えるダメージが増加。


### [Pachimon名]4

- Status: `Idea`
- Species ID:
- モチーフ:
- 狙い:

#### Fixed Skill

- 名前:豪雪
- 効果:

##### [天気：雪]
ダメージ軽減に使用されない雪が増加し、炎が低下。
水属性ダメージが、攻撃者の氷によって増加。
valueに応じた冷気を一定tick毎に全員に付与。

#### Passive

- 名前:
- 効果:全Pachimonを対象に（自身含む）、Slowが一定値を越えた対象に[状態異常：凍結]を付与する。

##### 凍結
Stunとしても扱う。Stunと同等の効果だが、炎属性ダメージで解消される。

### [Pachimon名]

- Status: `Idea`
- Species ID:
- モチーフ:
- 狙い:

#### Fixed Skill

- 名前:
- 効果:

#### Passive

- 名前:
- 効果:


## 既存共通Skill

### 冷たい手

- Implementation: `Implemented`
- 対象: 先頭の敵
- 既存のIceダメージに加えて、対象へ[冷気](./statuses/slow.md#冷気)を付与する

```text
冷気Value
= floor(75 × AmplificationMultiplier(Ice))
```

- 再付与と時間進行は[Slow共通仕様](./statuses/slow.md#共通仕様)に従う

## Ideas
