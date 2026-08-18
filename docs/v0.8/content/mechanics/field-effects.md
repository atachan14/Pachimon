# Field Effect Mechanics

環境パラメーター、Weather、陣営生成物に共通するField Effectの基本仕様をまとめる。

## 更新

- Field Effectは1tickごとに更新できる
- tick内の処理順は[Timing Mechanics](./timing.md#tick内の共通更新順)を参照する
- Value、残り時間、所有者、所属陣営を効果ごとに必要な範囲で保持する
- 同名再付与時のValue加算・置換・独立保持はField Effectごとに指定する

## DefinitionとRuntime

- 名前、説明、Icon、調整値はField Effect Definition SOへ保持する
- Skill SOは生成するField Effect Definitionを参照する
- 現在Value、HP、残り時間、所属陣営、生成者などの可変値はRuntime Instanceへ保持する
- StatusとWeatherも同様にDefinition SOとRuntime Instanceを分離する
- Field Effectから付与するStatusの生成量はField Effect Definition側へ保持する
- Status Definitionは、受け取ったValueの作用と終了条件を定義する

### 防御Snapshotを持つ生成物

- 生成時のStatを指定RatioでSnapshotし、生成後の生成者のStat変化から切り離す
- 再生成でValueを加算する場合でも、防御Snapshotは最新の生成者から取得した値で上書きできる
- 属性攻撃を肩代わりする場合は攻撃対象側の軽減を適用せず、軽減前Damageへ生成物自身のSnapshot防御を適用する
- 生成物のHPを超えた余剰Damageは、生成物による軽減後の値のまま元の対象へ引き継げる
- 引き継いだ余剰Damageへ元の対象の属性値とResistBonusによる軽減を適用する
- 元の対象が持つShieldなど、Damage適用段階の防御は余剰Damageにも適用する

### 軽量Field Entity

- Damageや状態を受け取る必要がある生成物でも、Turn・Skill・MNなどを必要としなければ完全な`BattleUnitState`にはしない
- Runtime InstanceへHP、防御Snapshot、許可された状態だけを保持する
- 生成物ごとに受け取れる状態を限定し、Slow・Stunなど行動用の状態は行動主体でない生成物へ適用しない
- 生成物へのDamageは`FieldEffectDamageAppliedEvent`として通知し、Passiveや別のField Effectが反応できる
- Field Effect由来Damageから同じ反応を再帰させないなど、Originによるループ防止を行う

## 表示領域

- MainPane中央をField表示領域とする
- `EnemyArea`と`AllyArea`の間を上下3段へ分割する
- 上段を敵陣Field（右寄せ）、中段を全体Field（中央寄せ）、下段を自陣Field（左寄せ）とする
- 3つのLaneはそれぞれ独立して横スクロールし、1つのLaneのカード数が他のLaneの配置へ影響しない
- カードが表示幅へ収まる間は、敵陣・全体・自陣それぞれの寄せ方向を維持する
- 環境パラメーターとWeatherは全体Fieldの中央寄せで表示する
- 異なるWeatherは同時に存在・表示できる
- 生成物、環境パラメーター、Weatherはカードで表示する
- 気温は`気温 +N / -N`と表示し、0のときはカードを表示しない
- カードをクリックすると、Skill・Passiveと共通の詳細Overlayを開く

## Battle Log

- SkillまたはPassiveが生成物を生成・再生成した場合、原則としてログを表示する
- 自陣生成物は`自陣に生成物名を生成した！`と表示する
- 敵陣生成物は`敵陣に生成物名を生成した！`と表示する
- 全体Fieldは`フィールドに生成物名を生成した！`と表示する
- Valueや効果時間は詳細表示へ任せ、基本ログには含めない

## 実装済み

### Weather基盤

- 陣営生成物の`BattleFieldRuntime`とは分離し、全体用の`BattleWeatherRuntime`で保持する
- Weather Definition SOは名前、説明、Icon、固有Ratioを保持する
- Weather Runtime InstanceはDefinition、最新の生成者、現在Value、小数減衰Workを保持する
- 同じWeatherの再生成ではValueを加算し、生成者を最新の使用者へ更新する
- 異なるWeatherは同時に保持・発動できる
- 毎tickの基本減衰量は`1`とし、Passiveによる減衰倍率を適用してから小数Workへ蓄積する
- Skillプレビュー用BattleへWeatherと小数減衰Workを複製する
- `Temperature`だけは符号付き・非減衰の環境パラメーターとして同Runtime内に保持する
- `Rain / Thunder / Wind`は互いに上書きせず、単独または同時に存在できる
- 発動中のWeather種類数は、Runtimeに存在するWeather IDごとに数える
- 気温は非0なら1種類、雨と雪は同じ`Rain`として1種類に数える

### 気温

- `WeatherId`: `Temperature`
- 初期値は`0`とし、Battle中は時間経過で減衰しない
- 正負の変更量を加算し、合計が`0`になった場合はRuntime Instanceを削除する
- 正の気温ではFire Attribute Ratioへ`AmplificationMultiplier(Temperature × 10%)`を乗算する
- 正の気温ではAquaとIce Attribute Ratioへ`ReductionMultiplier(Temperature × 20%)`を乗算する
- 負の気温ではFire Attribute Ratioへ`ReductionMultiplier(abs(Temperature) × 20%)`を乗算する
- 負の気温ではIce Attribute Ratioへ`AmplificationMultiplier(abs(Temperature) × 10%)`を乗算する
- 負の気温はAqua Ratioへ影響しない
- 補正後RatioはDamageとSkill/Passive固有効果のAttribute参照へ使用する
- 防御側の属性軽減には気温補正を使用しない
- Ratio Scaling PercentはSOで調整可能にする
- `温暖化`は気温を恒久的に増加させる
- `温暖化`自身のValueFireRatioにも現在の気温補正を適用し、自己増幅と寒冷による抑制を認める

### 雨と雪

- 雪は独立したWeather Instanceとして保持しない
- `Rain > 0 && Temperature >= 0`を雨とする
- `Rain > 0 && Temperature < 0`を雪とする
- 気温が0をまたいだ場合、Rain Valueを維持したまま雨と雪を即座に切り替える
- 雨ではValueに応じて通常の漏電Valueを加算する
- 雪では漏電を加算しない
- 雪の間、炎以外の非Status Damageを1以上受けるたびに冷気を付与する
- True Damageは炎属性ではないため、雪による冷気付与の対象に含める
- 毒素・漏電など`Origin = Status`のDamageは冷気付与の対象外とする
- 冷気Valueは`abs(Temperature)`依存で計算し、BaseValueとTemperature RatioをDefinition SOで調整可能にする
- Rain Definition SOは雨のAqua/Fire Ratio、漏電加算Ratio、雪の冷気BaseValue/Temperature Ratio、冷気Definitionを保持する
- Rain Runtime Instanceは気温の符号に応じて表示名と表示色も雨・雪へ切り替える
- 雨中は通常の漏電へ毎tick加算し、現在のRain Valueに応じて加算量を決める
- Rain Runtimeは漏電加算用の小数Workを保持し、整数化できた分だけ各生存Pachimonへ加算する
- 仮式は`RainValue × LeakValueRatioPerTick / 10000`、仮Ratioは`7`
- 雨Valueが`500`から`400`へ減衰する100tickで、漏電Valueは約`31`増える
- 漏電発動で消費された後も、雨が続いていれば次のtickから再蓄積する
- 雨の消滅や雪への切替時は小数Workだけを破棄し、既に加算された漏電Valueは残す

### 暴風

- `WeatherId`: `Wind`
- 同じ暴風を再生成した場合はValueを加算し、生成者を最新の使用者へ更新する
- 毎tickの基本減衰量は`1`
- 風Attribute Ratioへ`AmplificationMultiplier(暴風Value × WindRatioScalingPercent / 100)`を乗算する
- 全PachimonのSpeedへ`Wind × SpeedFromWindRatio / 100`を派生加算する
- 雨・雪の実効Valueを`Rain Value × AmplificationMultiplier(暴風Value × RainEffectRatioScalingPercent / 100)`とする
- 実効Rain Valueは、雨のAqua/Fire Ratio、雨による漏電加算、雪による冷気付与、雨男Passiveで共通使用する
- 仮値はWind Ratio `10%`、Speed参照 `20%`、雨・雪強化Ratio `10%`とする

### スモッグ

- `FieldEffectId`: `Smog`
- 名前、説明、毒素付与Ratio、Value減衰Ratio、付与する`ToxinStatusAsset`を`SmogFieldEffectAsset`へ保持する
- スモッグを生成するSkill SOは`SmogFieldEffectAsset`を参照する
- 敵陣単位で保持し、同じ敵陣への再生成ではValueを加算する
- 再生成時は同一Definitionの使用を必須とする
- 毎tickのStatus付与とValue減衰に、それぞれ小数Workを使用する
- Field Effectが付与したStatusは次のtickから更新する
