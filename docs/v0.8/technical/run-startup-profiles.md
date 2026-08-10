# Run Startup Profiles

Run開始時のGoldとItemは`RunStartupProfile`で管理する。
`GameSceneInstaller`へ個別のDebug Item設定を持たせない。

## Assets

- `ProductionRunProfile.asset`: 通常Build用
- `DevelopmentRunProfile.asset`: EditorおよびDevelopment Build用
- `RunProfileSettings.asset`: 2つのProfileとEditor上の選択を管理

初期ItemはInventoryと同じ9Slot固定配列へ`ItemAsset`を直接設定する。
`null`は空Slotとして扱う。同種Itemを複数持たせる場合は、複数のSlotへ同じItemを設定する。

## Profile Selection

| Environment | Profile |
| --- | --- |
| Editor / Automatic | Development |
| Editor / Production | Production |
| Editor / Development | Development |
| Development Build | Development |
| 通常Build | Production |

通常BuildではEditorの選択を参照せず、必ずProductionを使用する。
これにより、EditorでDevelopmentを選んだままBuildしてもテストItemは混入しない。

TitleScene経由とGameScene直接起動は同じ選択規則を使用する。

## Scope

Run開始時の所持データはこのProfileへ追加できる。
固定Seed、UI初期化、演出SkipなどRun所持データではないDebug設定は、必要になった段階で別のDebug Profileへ切り出す。
