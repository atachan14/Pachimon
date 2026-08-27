# Pollen

`花粉`はHasteを低下させるValue減衰型の状態。

## Runtime

- 対象のHasteへ`-Value`の直接加算補正を適用する。
- 行動時計とCooldownを進めた後、Valueを毎tick `1`減少させる。
- Valueが`0`になった時点で終了する。
- 同じ対象へ再付与した場合は、既存のValueへ新しいValueを加算する。
- 戦闘不能時とBattle終了時には、ほかの状態と同様に破棄する。

## Sources

| 発生源 | 付与Value |
| --- | --- |
| はっぱスライサー | `50 * AmplificationMultiplier(Wind * 100%)` |
| ビートヴァインの攻撃 | `ビートヴァインValue * 50%` |
| ソーラービーム | `100 * AmplificationMultiplier(Wind * 100%)` |
| しびれ粉 | `50 * AmplificationMultiplier(Poison * 100%)` |

Skillによる花粉付与は、対応するSkill Hitが回避・遮断された場合には対象へ届かない。
ビートヴァインはフィールド攻撃の解決後、生存している攻撃対象へ花粉を付与する。

## Data

- 状態名・説明・tickごとの減衰量は`PollenStatusAsset`で管理する。
- 各SkillのBaseValueと、ビートヴァインの付与率は各SOから調整できる。
