# Chain Mechanics

Skillが複数回Hitするときの対象遷移、減衰率、Skill別の追加連鎖数を定義する。

## 用語

- `BaseChainCount`: Skillが持つ追加連鎖回数。本体Hitは含めない
- `SkillChainCount`: Battle中、そのSkillだけへ加える追加連鎖回数。整数で保持する
- `EffectiveChainCount`: `BaseChainCount + SkillChainCount`
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

## Skill別の追加連鎖数

- `チェインバーン`、`連鎖する蔦`、`きりきり舞い`は、それぞれ独立したBattleStatusとして保持する
- Statusの`Value`は追加連鎖回数そのものを整数で保持し、固定小数点換算は行わない
- Skill使用後、そのSkillに対応するStatusへSOの`ChainGain`を加算する
- Battle終了まで持続し、Skill使用時に消費しない
- 他の連鎖Skillには影響しない
- SidePaneには`チェインバーン +1`のようにSkill名と現在値を表示する
- 追加連鎖数の増加はBattle Logへ表示しない
