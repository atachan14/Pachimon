# RestSpot Flow

## 回復ルール

- Player Party3体すべてを対象とする
- 1体ごとにEffective MaxHPの50%をCurrentHPへ加算する
- 1体ごとにEffective MaxMNの50%をCurrentMNへ加算する
- Effective MaxHPはRun中に取得したModを含む
- Effective MaxMNもRun中に取得したModを含む
- Effective MaxHPを超えた分は切り捨てて全快で止める
- 割合計算の途中では端数を維持し、回復量の確定時に切り捨てる
- 正の割合回復は最低1回復を保証する
- CurrentHPが0のPachimonも同じ計算で復活する
- Battle中だけの状態異常、Cooldown、Stat変化は対象外とする

```text
healAmount = ceil(EffectiveMaxHP * 50 / 100)
newCurrentHP = min(EffectiveMaxHP, currentHP + healAmount)

mnHealAmount = ceil(EffectiveMaxMN * 50 / 100)
newCurrentMN = min(EffectiveMaxMN, currentMN + mnHealAmount)
```

例:

- `0 / 1001`から休むと`501 / 1001`
- `300 / 1001`から休むと`801 / 1001`
- `800 / 1001`から休むと`1001 / 1001`

## 進行

```text
RestSpotへ移動
  -> 「パチモンを休ませますか？」
  -> [ 休む ]
  -> Party全体へ回復を適用
  -> LeftPaneを更新
  -> 回復結果をLogへ表示
  -> [ おう ]
  -> Node Clear
  -> Mapを自動表示
```

- 回復前は次Nodeへ進めない
- Partyが全快でもRestSpotは通常どおり完了する
- 同じNodeで回復処理を複数回実行できない
