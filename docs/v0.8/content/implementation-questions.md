# Content実装確認リスト

Skill / Passive実装を止める曖昧点だけを一時的に集める。
効果の正本は各属性・Mechanicsファイルとし、このファイルへ仕様を重複させない。

## 使い方

- Codexは実装前に、このリストから関係する確認点だけを提示する
- 回答済みの内容は正本mdへ反映し、このリストから削除する
- Base値やRatioなどSOで変更できる調整数値は、原則として確認待ちにしない
- 仮値で安全に実装できる項目は`仮実装可能`へ置く
- 共通基盤の選択に影響するものだけ`回答待ち`へ置く

## 回答待ち: 共通Mechanics

### Q-COMMON-03 同一タイミングのイベント優先順位

- Stat補正内の加算後・乗算後という計算順は確定済み
- ここでは、致死Damageに対する被攻撃Passive・Status反応・状態破棄の順序を決める
- 具体的に競合する効果を実装する段階まで保留する

- 関連: [Timing Mechanics](./mechanics/timing.md)

## 回答待ち: 個別Skill / Passive

### Fire

- `炎の障壁`: HP・効果時間・火傷Valueの式
- `気温`: 正負の値から攻撃・効果専用Attribute Ratio補正を作る式（決定済み）

### Leaf

- `日光浴`: 基本回復式と晴れ・雨による補正式
- `連鎖する蔦`: Damage・Slow・連鎖率の式
- `トリックルーム`: Value / 効果時間と、複数存在時の扱い
- `相互Stun Skill`: Stun時間と、使用者または対象が既にStun中の場合

### Poison

- 現時点の確認事項なし

### Ice

- `寒冷化`: 気温減少量と、雪による気温依存の冷気付与量
- `凍結`: Slow閾値、効果時間、再付与、Fire Damageを受けた際の解除タイミング

### Wind

- `低Wind DamageBonus Passive`: 非Battle用派生StatをBattle開始値へ焼き込んだ後、Battle中のWind変動を二重加算せず再計算する共通構造

### Dragon

- `ドラゴンフットワーク`: 回避対象となるDamage範囲
- `龍舞`: Dragon / Speed増加量
- `ノックアウト`: 基本時間、被Damageで延長する量、延長対象Damage

## 仮実装可能

次はSOへ仮のBase値・Ratioを置けば、追加確認なしで実装を開始できる。

- 既存の単体Attribute Damageと同型のSkill
- 単純な固定期間Stat加算Status
- 既存のSlow付与Skill
- 既存イベントへ反応する単純なDamage倍率Passive
- 既存Stat Pipelineへ登録する直接・派生Stat Passive

## 推奨実装順

1. `Recovery Effect`と回復Preview
2. `Poison`を代表とする定期Damage Status
3. `Chain`と共通Target遷移（代表実装完了）
4. `Shield`
5. `Field Effect / Weather`
6. `対象指定不可 / 回避 / Damage肩代わり`

各Mechanicsは代表SkillとPassiveを1セット実装して確認し、その後に同系統のコンテンツを追加する。
