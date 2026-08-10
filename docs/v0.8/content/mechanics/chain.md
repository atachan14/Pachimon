# Chain Mechanics

Skillが複数回Hitするときの対象遷移と減衰率を定義する。

## 用語

- `BaseChainCount`: Skillが持つ追加連鎖回数。本体Hitは含めない
- `AddChain`: Battle中の全連鎖Skillへ加える追加連鎖回数。小数で蓄積する
- `EffectiveChainCount`: `BaseChainCount + floor(AddChain)`
- 総Hit数: `EffectiveChainCount + 1`

## Damage倍率

本体を含むHit番号を`i = 0..N`、追加連鎖回数を`N`とする。

```text
ChainRatio(i, N) = (N + 1 - i) / (N + 1)
```

倍率をSkillの軽減前効果量へ掛けた後、各Hitを独立して解決する。
Damageの端数処理、対象の軽減、DamageイベントはHitごとに適用する。

## 対象遷移

1. 生存中の先頭を最初の対象にする
2. Party順で後方へ進む
3. 生存中の最後尾へ到達したら前方へ折り返す
4. 対象が1体なら同じ対象を繰り返す
5. 各Hit直前に生存中の隊列を再取得する
6. 生存対象がいなくなった時点で連鎖を終了する

途中のHitで戦闘不能が発生しても、残りの連鎖は現在の生存隊列で継続する。

## Presentation

- 連鎖全体を1回のSkill発動として扱う
- Skill名は最初のBlockだけ表示する
- HitごとにDialogue Blockを分け、2Block目以降はDamage行から開始する
- 2Block目以降へ「再発動」の見出しを表示しない

## AddChain

- BattleStatusの`Value`へ固定小数点で保持する（`100 = 1.0`、`50 = 0.5`）
- Battle終了まで持続し、Skill使用時に消費しない
- 実際の追加連鎖回数には小数部分を切り捨てて反映する
- 連鎖0のSkillも、AddChainが1.0以上なら連鎖する
- SidePaneには`0.5`、`1.0`のように現在値を表示する
