# Mechanics Inventory

v0.8のコンテンツ案から判明したMechanicsと、想定する実装境界をまとめる。

この文書は全Skill・Passiveの仕様を確定するものではない。共通基盤で扱う範囲を判断し、新しい固有Logicを追加しても既存基盤を壊しにくくするための設計入力とする。

## Stat

### 確認できた要求

- 直接加算補正
- 派生加算補正
- 直接乗算補正
- 派生乗算補正
- 補正値ごとの下限・上限
- Battle外の恒久補正と常時Passive
- Battle中の一時補正
- 付与時Statのスナップショット
- 攻撃計算だけに使い、防御計算には使わない属性補正
- 同じ段階のStatを相互参照する派生補正

### 実装方針

- 共通`StatCalculator`が段階処理と計算内訳を担当する
- 同じ段階の派生補正は、段階開始時のStat Snapshotから一斉に計算する
- 固有Passiveは補正の算出方法を提供し、全体の計算順序は変更しない
- 攻撃専用属性補正は基礎Statを書き換えず、Damage計算Contextの補正として扱う候補とする

## Damage

### 確認できた要求

- 単体、全体、多段、連鎖、再発動
- 先頭、最後尾、最低HP、状態量による対象選択
- 軽減前Damageと軽減後Damageの参照
- 属性を条件とする追加Damage
- 自傷Damage
- 継続Damage
- 貫通
- 超過Damageの引き継ぎ
- 減少HPやStat差によるDamage補正
- シールドへのDamage
- Damageの肩代わり
- 回避と対象指定不可

### 実装方針

- Damage Effectは属性、発生源、直接・追加・継続・自傷などの原因を保持する
- Damage生成側が`IsAttack`を明示し、属性・確定共通の`AttackReceivedEvent`をHP適用後に発行する
- Damage Eventは軽減前、軽減後、シールド吸収、HP減少量を区別する
- 追加Damageは元DamageのContextを複製せず、新しいDamage Effectとして生成する
- 再帰的な追加Damageには原因Chainを持たせ、無限再帰を検出できるようにする
- 対象選択はSkill固有Logicから分離し、共通Target Resolverで表現できる範囲を増やす

## Status Effect

### 確認できた要求

- 時限型、消費型、Stack型、遷移型
- Battle中永続
- tickごとのValue減少
- Turn終了時の解除
- Damage属性を条件とした発動
- 被Damage、与Damage、回避などのイベント反応
- 付与元ごとに独立した効果時間
- Slow、Stun、Leakなどとして扱う子分類
- 同時に複数の分類へ所属する状態

### 実装方針

- 固有挙動は個別Status Logicへ置く
- 共通分類はクラス継承だけで表現せず、`StatusCategory`で保持する
- 共通Systemは具体的なStatus IDではなくCategoryを条件に検索できるようにする
- Status Instanceは付与元、Value、Stack、残りtick、Snapshotを必要な範囲で保持する

### Category例

| Status | Categories |
| --- | --- |
| 通常漏電 | `Leak` |
| 麻痺 | `Slow`, `ElectricStatus` |
| 冷気 | `Slow`, `IceStatus` |
| 凍結 | `Stun`, `RemovedByFire` |
| ノックアウト | `Stun`, `ExtendedByDamage` |

## Timeline

### 確認できた要求

- 発生、硬直、CD
- SpeedとHasteによる符号付き補正
- Skill固有のTiming補正
- 発生待機中の対象指定不可
- Stun中の発生、硬直、CD停止
- 次Turnだけの補正
- Skillの自動再発動

### 実装方針

- Timelineは行動段階と残り作業量を明示的に保持する
- Speed/Hasteは現在値から1tickごとの進行量を決める
- Stunなどによる停止は残り作業量を書き換えず、進行可否を制御する
- Skill固有補正はPhase開始時の作業量へMultiplierを適用する

## Field

### 確認できた要求

- 両陣営へ作用する天気
- 自陣・敵陣へ所属する生成物
- 陣営全体へのシールド
- tickごとの回復、Damage、Value減少
- 天気同士の相互作用
- 生成物のValue補正

### 実装方針

- 天気は全体用`BattleWeatherRuntime`、陣営生成物は`BattleFieldRuntime`へ分離して保持する
- 効果範囲、所有者、所属陣営、Value、残りtickを分離して保持する
- 同名再付与時の合算・置換・独立保持はEffectごとのPolicyとする

## Resource and Recovery

### 確認できた要求

- 固定MN消費
- 現在MNの全消費
- 実消費量を参照する効果
- 次TurnのMN消費補正
- 最大MN参照のStat補正
- 単体・全体・継続回復
- 回復量への与回復・被回復補正

### 実装方針

- Skill選択時に消費予定量を計算し、確定した実消費量をSkill Contextへ保存する
- 回復はDamageと分離した共通Recovery Effectとして、PreviewとResolveで同じCalculatorを使う

## 実装優先順位

1. 共通Stat Calculatorと計算内訳
2. 派生Stat Passive
3. Damage Contextとイベント情報
4. Status InstanceとCategory
5. Target Resolver
6. Timelineの発生・停止・再発動
7. ShieldとDamage肩代わり
8. Field Effect
9. 複雑な連鎖・再帰的追加Damage

### 現在の進捗

- 1. 共通Stat Calculatorと計算内訳: 完了
- 2. 派生Stat Passive: 完了
- 3. Damage Contextとイベント情報: 属性ダメージの適用前・適用後イベント、貫通、超過ダメージの再軽減まで完了
- 4. Status InstanceとCategory: 通常漏電の保持・Value加算・消費、蓄電のスタック操作、Battle中UI表示まで実装
- 6. Timelineの発生・停止・再発動: 発生、Timed Status、Stun停止・再開、動的Speed/Haste、SlowのValue減衰と進行中Phaseへの反映まで実装済み
- 9. 複雑な追加Damage: 漏電からParty全体への追加Electricダメージを実装。Status Damageは別の漏電を発動しない

後続Mechanicsをすべて先に完成させる必要はない。各段階で代表Skill・Passiveを1つ実装し、共通基盤と固有Logicの境界を確認してから次へ進む。
