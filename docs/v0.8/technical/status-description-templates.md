# Status説明テンプレート

`BattleStatusAsset`の`Description`は、Skill / Passiveと同じテンプレート記法を使用する。
状態チップをクリックすると、実行中の`BattleStatusInstance`を基に値を展開して共通詳細Overlayへ表示する。

## 共通キー

| キー | 内容 |
| --- | --- |
| `{value:value}` | 現在Value |
| `{value:totalValue}` | `Value * StackCount` |
| `{value:stackCount}` | 現在Stack数 |
| `{value:remainingTicks}` | 残りtick。永続状態では`Battle中` |
| `{value:source}` | 付与者名 |

属性アイコン、色、用語リンクも既存と同様に使用できる。

- `{icon:Fire}`
- `{color:Fire}...{/color}`
- `{term:Toxin|毒素}`
- `{br}`

## 固有キー

| Status | キー |
| --- | --- |
| Slow / 冷気 / 風化 | `{value:decayPerTick}`。冷気は`{value:speedReduction}`も使用 |
| 麻痺 | `{value:remainingTicks}`（共通キー） |
| 毒素 | `{value:damagePerTick}`、`{value:decayPerTick}`、`{value:damagePerTickRatio}` |
| 充電中 | `{value:resistBonus}`、`{value:electricMultiplier}` |
| 充電完了 | `{value:speedBonus}`、`{value:electricMultiplier}`、`{value:durationRatio}` |
| 凍結 | `{value:fireDamagePerDecay}` |
| ノックアウト | `{value:damageDurationRatio}` |
| 飛行 | `{value:windSpeedRatio}` |
| 進水式 | `{value:aquaMultiplier}`、`{value:manaReductionRatio}` |
| フローズンブレイク | `{value:healingPerTick}`、`{value:totalDuration}` |
| 治癒の風 | `{value:windBonus}`、`{value:speedBonus}` |
| 龍の舞 | `{value:dragonBonus}`、`{value:speedBonus}` |

固有キーは対象Status以外では設定されない。新しいStatus固有値を説明へ出す場合は、`StatusDescriptionValueProviderRegistry`へキーを追加する。
