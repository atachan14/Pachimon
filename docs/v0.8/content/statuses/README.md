# Status Contents

具体的な状態の仕様を系統別にまとめる。

共通の保持構造、Category、端数処理、時間進行は
[Status Effect Mechanics](../mechanics/status-effects.md)を参照する。

属性別ファイルには、SkillやPassiveが「どの状態を、どのValueで、誰へ付与するか」を記載する。
状態そのものの効果、再付与、終了条件、解除条件はこのフォルダを正本とする。

## 属性による付与Value軽減

以下の状態は、付与されるValueを対象の対応AttributeとDamage共通の
`ReductionMultiplier`で軽減する。

| 状態 | 軽減に使用するStat |
| --- | --- |
| 麻痺 | Electric |
| 冷気 | Ice |
| 火傷 | Fire |
| 毒素 | Poison |

```text
最終付与Value
= floor(付与Value × ReductionMultiplier(対象の対応Stat))
```

- 軽減は状態の付与時に一度だけ適用する
- 再付与時は、新しく追加するValueだけを軽減して既存Valueへ加算する
- 軽減後Valueが0の場合は付与せず、付与イベントも発行しない
- 毒素の付与履歴には軽減後Valueを記録する
- 状態から別状態への「変化」は付与と区別し、個別仕様で明記しない限り再軽減しない
- `ReductionMultiplier`の仕様は[Scaling](../mechanics/scaling.md#reductionmultiplier)を参照する

## Status Families

- [Slow](./slow.md)
- [Charge](./charge.md)
- [Add Chain](../mechanics/chain.md)
- 凍結
  - `Category`: `Stun`
  - 通常付与時は対象のIceでValueを軽減する
  - 冷気からの変化では再軽減しない
  - 炎DamageでValueが減少し、0で解除する

## 移行予定

- 漏電（雨からのValue加算を含む）
- 充電中、充電完了、蓄電
- Stun、ノックアウト
- 火傷
- フットワーク
- ドラゴンクランカー、ドラゴンディフェンス
