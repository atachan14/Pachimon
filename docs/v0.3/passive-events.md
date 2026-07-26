# Passive Events

## 方針

- Passiveの発動条件と処理はPassiveごとに異なる
- 発動タイミングをCSVや共通Trigger Enumだけで表現しない
- Battle単位のEvent Dispatcherを介して、必要なEventだけ処理する
- Passive固有の可変値はBattleUnitState配下の専用Stateへ保持する
- ScriptableObjectなどの静的定義へBattle中Stateを保存しない

## Event候補

```text
BattleStarted
BeforeSkill
BeforeAttributeDamage
SkillResolved
UnitDefeated
BattleEnded
```

これはv0.3で実装済みのEventであり、今後のPassiveに必要なEventは適時追加する。Event一覧をTrigger Enumとして完全固定しない。

## Dispatcher

- Battle開始時に参加6体のPassive Logicを登録する
- 1体が複数Passiveを持つ場合はすべて登録する
- Battle終了時にBattle単位で破棄する
- Sceneや次Battleへ購読を残さない
- 同じEvent内の実行順は再現可能な規則を持つ
- Event処理中に発生した追加EventはQueueへ積み、無制限な再帰呼び出しを避ける

## 実行順

同じEventへ複数Passiveが反応する場合、まず以下を仮順序とする。

1. Eventの対象Unit
2. Eventの発生源Unit
3. その他のUnitをSide / Slot / Tie Priority順

実際のSample Passiveで不都合が出た場合は、優先度をPassive固有値として増やす前にEventの粒度を見直す。

## v0.3の完成範囲

- Battle Event Dispatcherの基盤
- Passive Logicの登録と破棄
- 少数のSample PassiveによるEvent発火確認
- Passive発動結果のBattleLog接続

## v0.3 Sample Passive

```text
対象属性値とResistBonusによる軽減後Damage
  -> floor(Damage * 130 / 100)
  -> CurrentHPへ適用
```

- 「与えるN属性ダメージが30%増加」を8属性分実装する
- True Damageは属性Damage Eventを通らないため対象外とする
- Passive発動時はBattle Logへ結果を追加する
- Passive ID 1～151は固有IDを維持し、ID順で8属性へ循環接続する
- 対応順は`Fire / Aqua / Leaf / Electric / Poison / Ice / Wind / Dragon`
- 後から`PassiveLogicRegistry.RegisterOrReplace()`でID単位の本番Logicへ差し替える

本番Passive 151種の制作はv0.8で扱う。
