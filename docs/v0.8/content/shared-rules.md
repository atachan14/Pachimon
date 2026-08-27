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
- SubStatは`DamageBonus / ResistBonus / StatusMastery / StatusResistance / GenerationPower / SustainPower / Speed / Haste`と記載する
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

初期Statは、8属性と`MaxHP / MaxMN`の10Statを同じBudgetから生成する。
各Speciesは`PachimonSpeciesAsset`に固有の初期Statを持ち、その価値を
先に共通Budgetから消費してから、残りをRunごとに重みなしでランダム振り分けする。

- 共通Budgetは`500 Unit`とし、全個体で必ず全量を使用する
- 8属性は`1 Unit = +1`とする
- `MaxHP / MaxMN`は`1 Unit = +8`とする
- `MaxHP / MaxMN`はBudget外の基礎値`500`を持つ
- `MaxHP / MaxMN`のSpecies固有値は表示値で設定し、`8`刻みとする
- Species固有初期Statが共通Budgetを超える場合は生成エラーとする
- 暫定値として、全SpeciesはAllocation Typeと一致する属性を固有初期値`50`として持つ
- 固有値がない場合、10Statへ平均`50 Unit`ずつ振り分けられるため、属性平均は約`50`、`MaxHP / MaxMN`平均は約`900`となる
- 個別Statには大きな揺らぎを許容し、属性は`0`も許容する
- SubStatは初期生成の対象外とし、基本値を`0`とする

同じUnit換算を通常Battle報酬、刻印、Row補正にも使用する。
暫定値は、刻印が属性`+15`／Resource`+120`、Battle報酬1枠目が属性`+30`／Resource`+240`とする。
敵TrainerのRow補正は`-45 + Row × 15 Unit`とし、Resourceへは同じ値を8倍して適用する。

### SubStat

- 8属性と8 SubStatの対応は個体ごとに一対一で生成する。
- Speciesは必要な属性/SubStatペアだけを固定でき、残りはRun生成時に重複なしでランダム決定する。
- 属性値から対応SubStatへの基礎反映率は全種類共通で`100%`とする。
- 装備によるSubStat補正は、その個体の対応率へ適用する。
- 詳細は[Stat Calculation](./mechanics/stat-calculation.md#個体ごとの属性substat対応)を参照する。

- 対象は`DamageBonus / ResistBonus / StatusMastery / StatusResistance / GenerationPower / SustainPower / Speed / Haste`
- 初期生成、通常のTrainerStatus報酬、Row補正ではSubStatへ直接加算しない
- 8属性から派生加算し、対応関係と計算順序は[Stat Calculation](./mechanics/stat-calculation.md)に従う
- 靴や勾玉などの装備は対応率を増加させ、Slowや火傷などのBattle効果だけがSubStatを直接変更できる

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
