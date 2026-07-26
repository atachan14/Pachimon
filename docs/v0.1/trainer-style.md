# Trainer Style

## 目的

通常Trainer、GymLeader、Eliteで同じStyle構造と名前選択処理を使用する。性別は事前分配せず、選択されたStyleから決定する。

## データ

### TrainerStyleCatalog

1つのScriptableObjectに全`TrainerStyleDefinition`を保持する。

- Normal Style: 14 Theme x 2性別 x 2肩書の56件
- League Style: 8属性Themeに4件ずつ、合計32件
- Normal Styleは別Nodeで再使用可能
- League Styleは1Run内で重複使用しない

### TrainerStyleDefinition

- `styleId`
- `theme`
- `gender`
- `styleCategory`: Normal / League
- `normalTitle`: Normalのみ使用
- `battleGraphic`: Battle / RightPaneで共用する色変更なしの一枚絵

### TrainerNameCatalog

- `nameId`
- `gender`
- `displayName`

性別ごとにシャッフルしたNameDeckを作り、候補を一巡するまで同じ名前を再使用しない。

### TrainerProfile

- `role`: Normal / GymLeader / Elite
- `styleId`
- `nameId`

肩書はNormalならStyle、GymLeader / EliteならRoleから決定する。

## 生成

`TrainerProfileFactory`をRunごとに1つ生成する。

```text
TrainerThemeを渡す
  -> Roleに対応するStyleCategoryから候補を取得
  -> Styleをランダム選択
  -> Style.genderに対応するNameDeckから名前を取得
  -> TrainerProfileを返す
```

League StyleはFactory内で使用済みIDを記録する。

`MapGenerator`はNodeReward配置後にFactoryを呼び、以下を生成済みNodeContentへ保存する。

- Battle: FirstElementの属性または要素種別からNormal Profileを生成
- Gym: Badge属性からGymLeader Profileを生成
- Elite: 重複なしで選んだ4属性からElite Profileを生成

Catalog参照は`GameSceneInstaller -> RunBootstrap -> MapGenerator`の順に渡す。

## Trainer Graphic

BattleとRightPaneでは、TrainerStyleごとの色変更なしの`battleGraphic`一枚絵を共用する。

MapIconはTrainerStyleから切り離し、`TrainerMapIconCatalog.asset`からTrainerRole別のIconSetを取得する。

- Normalはキャップ型の`TrainerMapIconSet.asset`を使う
- GymLeaderは尖ったハット型の`GymLeaderMapIconSet.asset`を使う
- Elite用IconSetが未設定の間はNormalへフォールバックする

```text
Base
Primary   <- FirstElement色（帽子）
Secondary <- SecondElement色（服・靴）
Detail
```

- MapIconの全レイヤーは同じ画像サイズ、Pivot、Pixels Per Unitで制作する
- 制作用Masterと分割レイヤーは`112 x 112`、Runtime用レイヤーは`56 x 56`とする
- Runtime用レイヤーは112px版からNearest Neighborで縮小する
- MapIconのPrimary / Secondaryは白からグレーで制作し、ViewのTintで着色する
- MapIconのBase / Detailは原色のまま表示する
- `TrainerMapIconView`が共通4レイヤーへSpriteと色を設定する
- MapIconの色はStyleではなくRewardElementから作る`TrainerColorScheme`が持つ
- Battle / RightPane用GraphicにはMod色を適用しない

RewardElementの仮配色:

| 要素 | 色 |
| --- | --- |
| 属性 | 対象属性色 |
| Speed | `#8E63CE` |
| MaxHp | `#F4F4EF` |
| MaxMn | `#5EC4D6` |
| BonusGold | `#E59A23` |
| DamageBonus | `#252A30` |
| ResistBonus | `#A7B5C0` |

明るいMaxHp色が背景へ埋もれないよう、MapIconには共通の濃い輪郭を使用する。

## Placeholder

Unityで以下を実行する。

初期Catalog生成:

`Tools > Pachimon > Data > Create Trainer Placeholder Catalogs`

確定したMap Iconの適用:

`Tools > Pachimon > Data > Apply Trainer Map Icons`

生成物:

- `Assets/GameData/Trainer/TrainerStyleCatalog.asset`
- `Assets/GameData/Trainer/TrainerNameCatalog.asset`
- `Assets/GameData/Trainer/TrainerMapIconSet.asset`
- `Assets/GameData/Trainer/GymLeaderMapIconSet.asset`
- `Assets/GameData/Trainer/TrainerMapIconCatalog.asset`
- `Assets/Art/Trainers/MapIcon/Layers112`の制作用Sprite 4枚
- `Assets/Art/Trainers/MapIcon/Layers56`のRuntime用Sprite 4枚
- `Assets/Art/Trainers/GymLeaderMapIcon/Layers112`の制作用Sprite 4枚
- `Assets/Art/Trainers/GymLeaderMapIcon/Layers56`のRuntime用Sprite 4枚
- Battle / RightPane共用の一枚絵Placeholder
- Normal Style 14件
- League Style 32件
- 男女各64件のName

## v0.1 Content

- 属性Themeは各属性・性別ごとに2つの肩書候補を持つ
- Stat Mod / Gold Themeも各Theme・性別ごとに2つの肩書候補を持つ
- Style選択後、Styleの性別と一致するNameDeckから名前を取得する
- NameDeckは男女64件ずつを持ち、Deckを一巡するまで同じNameを再使用しない

既存Placeholder Catalogへ確定済み肩書とNameを投入する。

```text
Tools > Pachimon > Data > Apply Trainer Content Data
```

既存Catalogは上書きしない。作成メニューを再実行すると、既存Catalogを`GameSceneInstaller`へ自動設定する。内容確認は`Tools > Pachimon > Data > Validate Trainer Catalogs`を使用する。
