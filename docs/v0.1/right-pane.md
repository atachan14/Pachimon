# RightPane

## 役割

`RightPaneView`は、状況に応じたWindowを表示するコンテナとする。Nodeの処理画面を切り替える`MainPaneView`とは分け、Map上の移動先確認、Battle中の詳細、Cityのラインナップなど補助情報を表示する。

## Node選択時の構造

```text
RightPane
└─ NodeSelectionWindow
   ├─ WindowHost
   │  ├─ BattleNodeWindow
   │  │  ├─ TabBar
   │  │  │  ├─ TrainerTabButton
   │  │  │  ├─ Pachimon1TabButton
   │  │  │  ├─ Pachimon2TabButton
   │  │  │  └─ Pachimon3TabButton
   │  │  └─ TabContent
   │  │     ├─ TrainerTab
   │  │     └─ PachimonTab x 3
   │  ├─ CityNodeWindow
   │  │  └─ Category Accordion
   │  └─ SimpleNodeWindow
   └─ SelectionFooter
      ├─ CancelButton
      └─ ConfirmButton
```

`CityNodeWindow`はMap上での事前閲覧とCity滞在中の購入に共用する。商品はCategoryごとのAccordionへまとめる。`EventNodeWindow`は表示内容が固まるまで`SimpleNodeWindow`で仮表示する。Cityの詳細は[`../v0.7/city-spec.md`](../v0.7/city-spec.md)を参照する。

## Footer

- Map上でNodeを仮選択している間だけ「決定」「キャンセル」を表示する
- Battle中の詳細表示ではFooterを非表示にする
- `WindowHost`を`Flexible Height = 1`、Footerを固定高とする
- Footerを非表示にした場合、VerticalLayoutGroupの再計算によって`WindowHost`が空いた領域を使用する
- Footer用の空白は残さない

## BattleNodeWindow

4Tabを切り替える。

- Trainer: 肩書、名前、RewardElement 2枠、獲得Gold、グラフィック
- Pachimon 1-3: グラフィック、CurrentHP / MaxHP、CurrentMN / MaxMN、8属性、Speed、DamageBonus、ResistBonus、Skill、Passive
- Pachimon用の3つのTab名には各Pachimonの名前を表示する
- Map上のBattle、Gym、Elite Nodeでは、到達状況にかかわらずTrainer/Pachimon情報を常に公開する
- 進行できないNodeでも情報は閲覧できるが、決定操作は表示しない
- 通過済みNodeは情報閲覧のみ可能とし、決定 / キャンセルFooterを表示しない

各Tabの本文は個別の`ScrollRect`内に置く。TabBarとFooterはScroll対象に含めない。

### TrainerTab

```text
TrainerTab (ScrollRect)
└ Viewport
  └ Content (VerticalLayoutGroup + ContentSizeFitter)
    ├ GraphicArea
    │ └ TrainerGraphic
    ├ TrainerName
    ├ RewardSection
    │ ├ Label
    │ └ RewardIcons
    │   └ RewardIcon x 0-2
    └ GoldSection
      ├ Label
      └ Value
```

- `TrainerGraphic`には`TrainerStyle.battleGraphic`を表示する
- 名前は`[肩書]の[名前]`形式とする。例: `燃える女のトキコ`
- BattleではFirstElementとSecondElementを常に2アイコンで並べる
- 属性、MaxHP / MaxMN、Speed、DamageBonus / ResistBonus、BonusGoldは共通の2枠表示を使用する
- GymではModの代わりにBadgeアイコンを表示する
- Eliteなど報酬がない場合は報酬とGoldを`---`で表示する
- BonusGold要素も専用色のRewardアイコンとして表示し、Gold欄へ実額を表示する
- 専用Spriteが未制作の間は、属性色と短縮ラベルを使った仮アイコンで表示する
- `TrainerRewardIconContent.Sprite`へSpriteを渡せば、仮ラベルから本番アイコンへ差し替えられる

### PachimonTab

```text
PachimonTab (ScrollRect)
└ Viewport
  └ Content (VerticalLayoutGroup + ContentSizeFitter)
    ├ GraphicArea
    │ └ FrontGraphic
    ├ Name
    ├ Hp
    ├ Mn
    ├ StatsGrid (2 columns)
    ├ StatusSection
    │ └ StatusGrid (auto wrap)
    ├ SkillSection
    │ └ SkillGrid (3 columns x 3 rows)
    └ PassiveSection
      └ PassiveGrid (3 columns, auto rows)
```

- Resourceは`CurrentHP / MaxHP`と`CurrentMN / MaxMN`を表示する
- StatsGridは8属性、Speed、DamageBonus、ResistBonusの合計11枠とする
- StatsGridはRightPaneの幅に追従し、中央で均等な2columnへ分割する
- Skillは最大9枠を常に表示し、3column x 3rowに固定する
- 未取得のSkill枠は`---`で表示する
- 状態異常は内容数と利用可能幅に応じて折り返す
- PassiveはSkillと同じ要素サイズ・3columnとし、所持数に応じて行を追加する
- 状態異常がない場合は`なし`を表示する
- SkillSectionとPassiveSectionは背景色とBorderで区切る
- BattleState接続前はRun中のCurrentHP / CurrentMNを表示する

Map上の全Nodeは情報を閲覧できる。ControllerはBattle、Gym、Elite Nodeの敵Pachimonについて、常に公開済みの`PachimonPreviewContent`をViewへ渡す。

進行可能でないNodeを閲覧している間はFooterを非表示にし、決定操作を受け付けない。Map上での仮選択強調は表示するが、`RunState.currentNodeId`は変更しない。

## Pane幅

RightPane内部のLayoutGroupやTMPのPreferred Widthが、親Content内のPane比率を変えないようにする。

- LeftPane / MainPane / RightPaneの`LayoutElement.Min Width = 0`
- LeftPane / MainPane / RightPaneの`LayoutElement.Preferred Width = 0`
- `Flexible Width`だけでPane比率を決める
- RightPane内部では横方向の`ContentSizeFitter`を使用しない
- 長いテキストは折り返し、縦方向のScrollで表示する

現在の比率はLeft/Main/Right = `1 / 1.5 / 1`。均等表示へ戻す場合はすべて`1`にする。

## Pane Palette

LeftPaneとRightPaneは同じ明度帯のライトテーマとし、緑と赤の色相で役割を区別する。

- LeftPane: `#C6FABE`（明るい緑）
- RightPane: `#F6C5C0`（淡いコーラル）
- Card: `#FFFFFFCC`
- Primary Text: `#263238`
- Secondary Text: `#667277`
- Border: `#BCC8CC`
- GraphicAreaは背景色を持たず、Pane背景をそのまま見せる
- Skill / Passive / 属性Iconなど、意味を持つ色は維持する
- 共通色は`GameUiPalette`へ集約する

## Editor setup

`Tools > Pachimon > UI > Setup Right Pane Windows`でHierarchyと参照を生成する。自動生成は行わず、既に設定済みの場合は再生成しない。

既存の改行テキスト版PachimonTabは、次のメニューで構造化レイアウトへ更新する。

```text
Tools > Pachimon > UI > Upgrade Pachimon Tab Layouts
```

TrainerTabの既存Hierarchyを専用レイアウトへ更新する場合は、次を実行する。

```text
Tools > Pachimon > UI > Upgrade Trainer Tab Layout
```

既存のLeftPane / RightPaneと各Tabへ共通Paletteを適用する場合は、次を実行する。

```text
Tools > Pachimon > UI > Apply Shared Pane Palette
```
