# Battle Environment

## Damage-driven changes

Attribute damage changes the environment from its pre-defense damage. Evasion and missing targets do not change it. Shield and field-effect interception still count once from the original hit.

- Fire: Temperature `+0.5%`, Moisture `-0.5%`
- Aqua: Moisture `+0.5%`
- Leaf: Plasma `-0.5%`
- Electric: Plasma `+0.5%`
- Ice: Temperature `-0.5%`
- Wind: Windstorm `+10%`
- Poison / Dragon: no automatic change

Fractions are carried per axis. Damage-driven changes do not receive GenerationPower again.
Temperature, Moisture, and Plasma use diminishing growth only when a change extends
their current sign: `EffectiveChange = RawChange * 25 / (25 + abs(CurrentValue))`.
A change toward zero or across zero keeps its full RawChange. Direct changes from
Skills and environment effects do not use this diminishing formula.

## Signed axes

Temperature, Moisture, and Plasma persist for the whole Battle and do not decay naturally.

- Positive Temperature: Fire Ratio up, Ice Ratio down
- Negative Temperature: Ice Ratio up, Fire Ratio down
- Positive Moisture (`湿潤`): Aqua Ratio up, Fire Ratio down
- Negative Moisture (`乾燥`): Fire Ratio up, Aqua Ratio down
- Positive Plasma: Electric Ratio up, Leaf Ratio down
- Negative Plasma (`大自然`): Leaf Ratio up, Electric Ratio down

Amplification uses `AmplificationMultiplier`; reduction uses `ReductionMultiplier`.
Temperature uses its full absolute Value as the multiplier input, matching
Moisture and Plasma.

Environment detail overlays resolve the current Value through these formulas and
show the resulting amplification and reduction percentages rather than the raw
scaling inputs.

## Precipitation

Precipitation is one signed value that decays by 1 toward zero each tick.

- Positive with non-negative Temperature: `雨`
- Positive with negative Temperature: `雪`
- Negative: `晴天`
- Zero: not displayed

Every 10 ticks:

- Rain/Snow adds `EffectivePrecipitation * 1%` Moisture.
- Windstorm amplifies this Rain/Snow Moisture gain.
- Sunny adds `abs(Value) * 1%` Temperature and subtracts the same amount from Moisture.

Direct Ratio effects stack multiplicatively with Temperature and Moisture:

- Rain: Aqua up, Fire down
- Snow: Ice up, Fire down
- Sunny: Fire up, Aqua down

`あまごい` adds positive Precipitation. `にほんばれ` adds negative Precipitation.
