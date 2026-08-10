# Recovery Mechanics

Battle中のHP回復に関する共通仕様をまとめる。

## 基本ルール

- 回復量はSkill / Passive / Status固有の式で算出する
- 現時点では、属性Stat以外の共通回復Bonus・回復Resistを設けない
- 計算結果が最大HPを超える場合、超過分を切り捨てる
- 戦闘不能Pachimonは回復対象にできない
- 全体回復でも戦闘不能Pachimonを対象へ含めない
- Battle中の通常回復では戦闘不能から復帰しない
- 将来、蘇生を行う場合は通常回復とは別のEffectとして実装する
