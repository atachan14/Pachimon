# Content共通ルール

## 基本方針

- 1体のPachimon、固定Skill、Passiveを1セットとして考える
- 設計案はAllocation Typeごとのファイルへ記録する
- SkillとPassiveは他のPachimonへ取得・付与される可能性がある
- Markdownは効果の意図、計算式、発動条件、特殊ケースを記録する
- SkillとPassiveの調整値はScriptableObjectを正本とする
- SkillとPassiveの固有処理はC# Logicを正本とする
- 未確定事項は`要確認`へ残し、解消後にStatusを`Ready`へ変更する

## 表記

- Stat名はコードと同じ`Fire / Aqua / Leaf / Electric / Poison / Ice / Wind / Dragon`を使う
- 共通Statは`Speed / DamageBonus / ResistBonus`と記載する
- Skillの基本項目は`硬直 / CD / MN`と記載する
- 発生が0より大きいSkillだけ`発生`を記載する
- Statを増幅倍率として使う場合は、負数対応した`AmplificationMultiplier(Stat)`で記載する
- StatからDamageや状態Valueを生成する場合は、原則`BaseValue × AmplificationMultiplier(Stat)`で記載する
- 割合を表す値には`%`または`割合`を明記する
- SkillやPassive固有の未確定事項は各項目の`要確認`へ記載する

## Statから生成する効果量

Damageや状態Valueなど、Statを強さへ変換する効果は原則として次の共通式を使う。

```text
EffectValue
= BaseValue × AmplificationMultiplier(Stat)
```

- `Stat = 0`では`BaseValue`をそのまま得る
- 正のStatは線形に増加する
- 負のStatは0未満にならず漸減する
- 複数Statを別々の効果量へ変換する場合は、成分ごとにこの式を適用する
- 複数Statを同じ効果へ乗算する場合は、`BaseValue × AmplificationMultiplier(A) × AmplificationMultiplier(B)`とする
- Base値、参照Stat、追加倍率は各Skill / PassiveのScriptableObjectで調整可能にする

次の値には共通式を強制せず、個別仕様を優先する。

- 付与時のStatをそのまま保存するスナップショット
- 受けたDamageに対する割合
- スタック数
- 派生Stat加算
- 固定値または割合そのものを表す値

## 共通Mechanics

- [Scaling](./mechanics/scaling.md)
- [Damage](./mechanics/damage.md)
- [Stat Calculation](./mechanics/stat-calculation.md)
- [Timing](./mechanics/timing.md)
- [Status Effects](./mechanics/status-effects.md)

## Run参加

- Catalogには将来実装するSpeciesを残してよい
- Speciesは`isRunEnabled`でRunへの参加可否を保持する
- Run生成では、実装済みとして有効化されたSpeciesだけを使用する
- 有効Speciesが少ない間は、同じSpeciesの個体を複数生成して300配置枠を埋める
- 各Speciesの生成数は可能な限り均等にする
- 同一Nodeへ同じSpeciesを重複配置しない
- 同一Rowへの同じSpecies配置も可能な範囲で避ける
- 隣接Nodeへの同じSpecies配置も可能な範囲で避ける
- Eliteの一致Skill振り分けを成立させるため、各属性に最低4体の有効Speciesを要求する
- 151種すべてが有効な場合は1種をRunから外し、残り150種を各2個体生成する
- 150種以下の場合は有効SpeciesをすべてRunへ参加させる

## 初期Stat生成

初期Statは、属性Statと共通Statを別々のBudgetから生成する。

### 属性Stat

- 対象は`Fire / Aqua / Leaf / Electric / Poison / Ice / Wind / Dragon`
- 属性Budget `800`を8属性へランダムに全量振り分ける
- 各属性の平均は`100`となる

### 共通Stat

- 対象は`MaxHP / MaxMN / Speed / Haste / DamageBonus / ResistBonus`
- 共通Budget `600`を6Statへランダムに全量振り分ける
- `MaxHP / MaxMN`は振分前に最低値`500`を持つ
- `MaxHP / MaxMN`は共通Budgetの振分値`1`につき`5`増加する
- その他の共通Statは振分値`1`につき`1`増加する
- 各共通Statの振分値の平均は`100`となる
- したがって`MaxHP / MaxMN`の平均は約`1000`、その他4Statの平均は約`100`となる

各グループのBudget合計は個体間で一致させる。個別Statは大きく揺らぎ、`MaxHP / MaxMN`以外は`0`を許容する。

## Skill振り分け

- Mapへ追加振り分けするSkillは、Run参加Speciesの固定Skillから選ぶ
- Runへ参加しないPlaceholder Speciesの固定Skillは候補へ含めない
- 固定Skillは`isMapAssignable`でなければならない
- 各属性にType一致Skill候補を最低4つ要求する

## 未決事項

- 151種完成時に18体となる属性
- コンテンツ項目の最終テンプレート
- 各属性内の制作順
