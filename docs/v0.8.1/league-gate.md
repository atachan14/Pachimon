# League Gate

## Entry Condition

- The gate checks the total number of owned Badges.
- Duplicate Badges count separately.
- Eight or more Badges unlock the League Gate shops and the Elite challenge.

## Successful Entry

The Main Pane reuses the City shop UI with these options:

- Pharmacy
- Engraving Shop
- Challenge the Elite Four

### Pharmacy

Each recovery item has five copies. Its generated recovery percentage is between
70% and 100%.

| Item | Effect | Base Gold |
| --- | --- | ---: |
| すごいきずぐすり | Restores HP by the generated percentage of MaxHP | 500 |
| すごいMNポーション | Restores MN by the generated percentage of MaxMN | 500 |
| すごい回復薬 | Restores HP and MN by the generated percentage of each maximum | 1500 |
| げんきのかたまり | Revives a defeated target and restores HP and MN by the generated percentage | 2000 |

Recovery Items do not receive SUS/SUP amplification.

### Engraving Shop

- Generate three Engravings for each generated Stat.
- Generated Stats are the eight Attributes, MaxHP, and MaxMN: 30 entries total.
- Main effects, downsides, and price variation follow the City Engraving rules.

## Failed Entry

If the player owns fewer than eight Badges, shops are not shown. The professor
appears and the following Dialogue plays:

```text
集めたバッジの数は・・・
・・・X個じゃな。

もう、家に帰りなさい。

XXは目の前が真っ暗になった
```

`X` is the total Badge count and `XX` is the player name. After the final line,
the normal defeat fade returns to `TitleScene`.
