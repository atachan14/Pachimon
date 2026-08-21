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
各Speciesは`PachimonSpeciesAsset`に固有の初期Statを持ち、その価値を
先にBudgetから消費してから残りをRunごとにランダム振り分けする。

- 固有初期Statは全14StatをInspectorへ明示する
- `MaxHP / MaxMN`は表示値で設定し、内部では`5`で割った価値単位へ変換する
- `MaxHP / MaxMN`の固有値は`5`刻みとする
- 固有初期StatがグループのBudgetを超えるSpeciesは生成エラーとする
- 全Species共通の`MaxHP / MaxMN`最低値`500`は固有値とは別に加算する

### 属性Stat

- 対象は`Fire / Aqua / Leaf / Electric / Poison / Ice / Wind / Dragon`
- 暫定値として、全SpeciesはAllocation Typeと一致する属性を固有初期値`100`として持つ
- 属性Budget `800`からSpecies固有初期Statを引き、残りを8属性へランダムに全量振り分ける
- 固有初期Statを含む属性合計は常に`800`となる

### 共通Stat

- 対象は`MaxHP / MaxMN / Speed / Haste / DamageBonus / ResistBonus`
- 共通Budget `200`からSpecies固有初期Statの価値を引き、残りを6Statへランダムに全量振り分ける
- `MaxHP / MaxMN`は振分前に最低値`500`を持つ
- `MaxHP / MaxMN`は共通Budgetの振分値`1`につき`5`増加する
- その他の共通Statは振分値`1`につき`1`増加する
- 固有初期Statがすべて`0`の場合、各共通Statの振分値の平均は約`33`となる
- 同条件では`MaxHP / MaxMN`の平均は約`667`、その他4Statの平均は約`33`となる

各グループのBudget合計は個体間で一致させる。個別Statは大きく揺らぎ、`MaxHP / MaxMN`以外は`0`を許容する。

## Skill振り分け

- Startと通常Battleで追加する最初の2Skillは、SpeciesのAllocation Type一致を1つ、不一致を1つとする
- Rowによる3個目以降の追加Skillは全候補から使用回数の少ないものを優先する
- Gym / Eliteは専用の一致Skill振り分けを先に行い、通常追加枠は従来どおり全候補から振り分ける
- Mapへ追加振り分けするSkillは、Run参加Speciesの固定Skillから選ぶ
- Runへ参加しないPlaceholder Speciesの固定Skillは候補へ含めない
- 固定Skillは`isMapAssignable`でなければならない
- 各属性にType一致Skill候補を最低4つ要求する

## Speciesデータ

- 1Speciesにつき1つの`PachimonSpeciesAsset`を持つ
- `PachimonCatalog`は151個のSpecies Asset参照だけを保持する
- Species Assetは名前、画像、Allocation Type、Run有効フラグ、固定Skill参照、Passive参照、固有初期Statを保持する
- Run用の`PachimonInstance`生成時にSkill / Passive参照からIDを取り出し、Battle中は従来どおりIDで扱う

## 未決事項

- 151種完成時に18体となる属性
- コンテンツ項目の最終テンプレート
- 各属性内の制作順
