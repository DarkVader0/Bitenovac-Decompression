# Closed-circuit reference scenarios

Each section is one test: full setup, the schedule this implementation produces, then an empty
block for the reference tool. Every setting is listed in full, so nothing has to be looked up
elsewhere. **Bold** marks a value that differs from the shared default.

**Level durations are time *at* the level.** Descent and inter-level travel are generated on top of
them. Subsurface's planner rows work the other way round — a segment's duration includes the
transit into it — so add the stated descent time to the first level, or switch the planner to
runtime entry. Getting this wrong shifts every stop.

**Bailout gas is carried and costed but never breathed** unless a scenario says otherwise. It must
not be selectable as a decompression gas in the reference tool, or the dive silently becomes an
open-circuit one.

Water densities: fresh 1000, brackish 1015, EN 13319 1020, salt 1030 kg/m³.

---

## (80 m, 20 min) — Diluent Trimix 12/65 @ 1.3 bar

`CreatePlan_ShouldMatchReferenceSchedule_WhenClosedCircuitAt80MetersOnDiluent1265AtSetpoint13`

### Profile

| Level | Depth | Time at level | Apparatus |
| --- | --- | --- | --- |
| 1 | 80 m | 20 min | CCR @ 1.3 bar |

Descent: 4.44 min.

### Gases

| Gas | Size | Fill | Purpose |
| --- | --- | --- | --- |
| Trimix 12/65 | 24 L | 200 bar | Diluent |
| Oxygen | 3 L | 200 bar | Loop supply |
| Trimix 21/35 | 11.1 L | 207 bar | Bailout |
| Nitrox 50 | 11.1 L | 207 bar | Bailout |

### Settings

| Setting | Value | Setting | Value |
| --- | --- | --- | --- |
| Gradient factors | **30 / 80** | Reserve pressure | 40 bar |
| Setpoint | **1.3 bar, constant** | Reserve stress factor | 2 |
| Surface pressure | **1013.25 mbar** | Reserve team size | 2 |
| Salinity | Salt (1030) | Stop increment | 1 min |
| Descent rate | 18 m/min | Problem-solving | 1 min |
| Ascent below 75% | 9 m/min | Gas switch | 2 min |
| Ascent 75-50% | 9 m/min | O2 break interval | 20 min |
| Ascent 50% to stops | 9 m/min | O2 break duration | 5 min |
| Ascent last 6 m | 1 m/min | Safety stop | off |
| Bottom SAC | 18 L/min | Last stop | 6 m |
| Deco SAC | 14 L/min | Switch at required stop only | off |
| Bottom metabolic O2 | 1 L/min | Oxygen breaks | off |
| Deco metabolic O2 | 0.6 L/min | Oxygen is narcotic | yes |
| Loop volume | 6 L | MOD model | Realistic |
| Bottom pO2 | 1.42 bar | Deco pO2 | 1.62 bar |

### Bitenovac plan

```
Valid: True, total runtime: 143.66666666166665

Descent       80 m     4 min     4 min  TX12/65 (CCR @ 1.3)
Bottom        80 m    20 min    24 min  TX12/65 (CCR @ 1.3)
Ascent        42 m     4 min    29 min  TX12/65 (CCR @ 1.3)
Stop          42 m     1 min    30 min  TX12/65 (CCR @ 1.3)
Ascent        39 m     0 min    30 min  TX12/65 (CCR @ 1.3)
Stop          39 m     1 min    31 min  TX12/65 (CCR @ 1.3)
Ascent        36 m     0 min    31 min  TX12/65 (CCR @ 1.3)
Stop          36 m     1 min    32 min  TX12/65 (CCR @ 1.3)
Ascent        33 m     0 min    33 min  TX12/65 (CCR @ 1.3)
Stop          33 m     2 min    35 min  TX12/65 (CCR @ 1.3)
Ascent        30 m     0 min    35 min  TX12/65 (CCR @ 1.3)
Stop          30 m     2 min    37 min  TX12/65 (CCR @ 1.3)
Ascent        27 m     0 min    37 min  TX12/65 (CCR @ 1.3)
Stop          27 m     2 min    39 min  TX12/65 (CCR @ 1.3)
Ascent        24 m     0 min    40 min  TX12/65 (CCR @ 1.3)
Stop          24 m     4 min    44 min  TX12/65 (CCR @ 1.3)
Ascent        21 m     0 min    44 min  TX12/65 (CCR @ 1.3)
Stop          21 m     3 min    47 min  TX12/65 (CCR @ 1.3)
Ascent        18 m     0 min    47 min  TX12/65 (CCR @ 1.3)
Stop          18 m     6 min    53 min  TX12/65 (CCR @ 1.3)
Ascent        15 m     0 min    54 min  TX12/65 (CCR @ 1.3)
Stop          15 m     6 min    60 min  TX12/65 (CCR @ 1.3)
Ascent        12 m     0 min    60 min  TX12/65 (CCR @ 1.3)
Stop          12 m     9 min    69 min  TX12/65 (CCR @ 1.3)
Ascent         9 m     0 min    69 min  TX12/65 (CCR @ 1.3)
Stop           9 m    12 min    81 min  TX12/65 (CCR @ 1.3)
Ascent         6 m     0 min    82 min  TX12/65 (CCR @ 1.3)
Stop           6 m    56 min   138 min  TX12/65 (CCR @ 1.3)
Ascent         0 m     6 min   144 min  TX12/65 (CCR @ 1.3)

CNS: 80.24 %
OTU: 209.89

Cylinder TX12/65: used 47.85 L, end pressure 197.98 bar
Cylinder O2: used 101.67 L, end pressure 165.66 bar
Cylinder TX21/35: used 0 L, end pressure 207 bar
Cylinder NX50: used 0 L, end pressure 207 bar
Reserve satisfied: True
```

### Reference

Tool / version:

| Depth | Stop time | Runtime | Gas |
| --- | --- | --- | --- |
|  |  |  |  |

Total runtime:
Gas used:
CNS / OTU:
Differences from the plan above:

---

## (75 m, 20 min + 75 m, 2 min bailout) — Diluent Trimix 12/60 @ 1.2 bar, Trimix 15/55, Nitrox 50

`CreatePlan_ShouldMatchReferenceSchedule_WhenBailingOutToOpenCircuitAt75MetersInEn13319Water`

The second segment is the loop failure itself, not travel: same depth, apparatus changed to open
circuit for the whole remaining ascent.

### Profile

| Level | Depth | Time at level | Apparatus |
| --- | --- | --- | --- |
| 1 | 75 m | 20 min | CCR @ 1.2 bar |
| 2 | 75 m | 2 min | Open circuit |

Descent: 4.17 min.

### Gases

| Gas | Size | Fill | Purpose |
| --- | --- | --- | --- |
| Trimix 12/60 | 24 L | 200 bar | Diluent |
| Oxygen | 3 L | 200 bar | Loop supply |
| Trimix 15/55 | 24 L | 232 bar | Bailout |
| Nitrox 50 | 22.2 L | 207 bar | Bailout |

### Settings

| Setting | Value | Setting | Value |
| --- | --- | --- | --- |
| Gradient factors | **20 / 80** | Reserve pressure | 40 bar |
| Setpoint | **1.2 bar, loop phase only** | Reserve stress factor | 2 |
| Surface pressure | 1000 mbar | Reserve team size | **1** |
| Salinity | **EN 13319 (1020)** | Stop increment | 1 min |
| Descent rate | 18 m/min | Problem-solving | 1 min |
| Ascent below 75% | 9 m/min | Gas switch | 2 min |
| Ascent 75-50% | 9 m/min | O2 break interval | 20 min |
| Ascent 50% to stops | 9 m/min | O2 break duration | 5 min |
| Ascent last 6 m | 1 m/min | Safety stop | off |
| Bottom SAC | 18 L/min | Last stop | **3 m** |
| Deco SAC | 14 L/min | Switch at required stop only | **on** |
| Bottom metabolic O2 | 1 L/min | Oxygen breaks | off |
| Deco metabolic O2 | 0.6 L/min | Oxygen is narcotic | yes |
| Loop volume | 6 L | MOD model | Realistic |
| Bottom pO2 | 1.42 bar | Deco pO2 | 1.62 bar |

### Bitenovac plan

```
Valid: True, total runtime: 151.83333333

Descent       75 m     4 min     4 min  TX12/60 (CCR @ 1.2)
Bottom        75 m    20 min    24 min  TX12/60 (CCR @ 1.2)
Bottom        75 m     2 min    26 min  TX15/55
Ascent        42 m     4 min    30 min  TX15/55
Stop          42 m     1 min    31 min  TX15/55
Ascent        39 m     0 min    31 min  TX15/55
Stop          39 m     2 min    33 min  TX15/55
Ascent        36 m     0 min    33 min  TX15/55
Stop          36 m     1 min    34 min  TX15/55
Ascent        33 m     0 min    35 min  TX15/55
Stop          33 m     2 min    37 min  TX15/55
Ascent        30 m     0 min    37 min  TX15/55
Stop          30 m     3 min    40 min  TX15/55
Ascent        27 m     0 min    40 min  TX15/55
Stop          27 m     3 min    43 min  TX15/55
Ascent        24 m     0 min    44 min  TX15/55
Stop          24 m     6 min    50 min  TX15/55
Ascent        21 m     0 min    50 min  TX15/55
GasSwitch     21 m     2 min    52 min  NX50
Stop          21 m     1 min    53 min  NX50
Ascent        18 m     0 min    53 min  NX50
Stop          18 m     4 min    57 min  NX50
Ascent        15 m     0 min    58 min  NX50
Stop          15 m     5 min    63 min  NX50
Ascent        12 m     0 min    63 min  NX50
Stop          12 m     8 min    71 min  NX50
Ascent         9 m     0 min    71 min  NX50
Stop           9 m    12 min    83 min  NX50
Ascent         6 m     0 min    84 min  NX50
Stop           6 m    21 min   105 min  NX50
Ascent         3 m     3 min   108 min  NX50
Stop           3 m    41 min   149 min  NX50
Ascent         0 m     3 min   152 min  NX50

CNS: 45.06 %
OTU: 115.51

Cylinder TX12/60: used 45.01 L, end pressure 198.12 bar
Cylinder O2: used 24.17 L, end pressure 191.94 bar
Cylinder TX15/55: used 1874.05 L, end pressure 153.91 bar
Cylinder NX50: used 2459.48 L, end pressure 96.21 bar
Reserve satisfied: True
```

### Reference

Tool / version:

| Depth | Stop time | Runtime | Gas |
| --- | --- | --- | --- |
|  |  |  |  |

Total runtime:
Gas used:
CNS / OTU:
Differences from the plan above:

---

# Mode comparison

The four dives below are also planned on open circuit and on the passive semi-closed loop, so the
only difference between the three schedules is the breathing apparatus. Keep the settings identical
across all three documents.

The loop holds **1.2 bar on the bottom and 1.6 bar for the ascent**, unlike the constant setpoint
above. The bailout ladder is one gas shorter than the open-circuit ladder for the same dive: no
oxygen cylinder, because the loop supplies it.

---

## (45 m, 25 min) — Diluent Trimix 21/35 @ 1.2 / 1.6 bar

`CreatePlan_ShouldMatchReferenceSchedule_WhenClosedCircuitAt45MetersOnTrimix2135`

### Profile

| Level | Depth | Time at level | Apparatus |
| --- | --- | --- | --- |
| 1 | 45 m | 25 min | CCR @ 1.2 bar |

Descent: 2.50 min.

### Gases

| Gas | Size | Fill | Purpose |
| --- | --- | --- | --- |
| Trimix 21/35 | 24 L | 232 bar | Diluent |
| Oxygen | 3 L | 200 bar | Loop supply |
| Nitrox 50 | 22.2 L | 207 bar | Bailout |

### Settings

| Setting | Value | Setting | Value |
| --- | --- | --- | --- |
| Gradient factors | **30 / 85** | Reserve pressure | 40 bar |
| Setpoint | **1.2 bottom / 1.6 ascent** | Reserve stress factor | 2 |
| Surface pressure | **1013.25 mbar** | Reserve team size | 2 |
| Salinity | Salt (1030) | Stop increment | 1 min |
| Descent rate | 18 m/min | Problem-solving | 1 min |
| Ascent below 75% | 9 m/min | Gas switch | 2 min |
| Ascent 75-50% | 9 m/min | O2 break interval | 20 min |
| Ascent 50% to stops | 9 m/min | O2 break duration | 5 min |
| Ascent last 6 m | 1 m/min | Safety stop | off |
| Bottom SAC | 18 L/min | Last stop | 6 m |
| Deco SAC | 14 L/min | Switch at required stop only | off |
| Bottom metabolic O2 | 1 L/min | Oxygen breaks | off |
| Deco metabolic O2 | 0.6 L/min | Oxygen is narcotic | yes |
| Loop volume | 6 L | MOD model | Realistic |
| Bottom pO2 | 1.42 bar | Deco pO2 | 1.62 bar |

### Bitenovac plan

```
Valid: True, total runtime: 55.83333333

Descent       45 m     2 min     2 min  TX21/35 (CCR @ 1.2)
Bottom        45 m    25 min    28 min  TX21/35 (CCR @ 1.2)
Ascent        15 m     3 min    31 min  TX21/35 (CCR @ 1.6)
Stop          15 m     1 min    32 min  TX21/35 (CCR @ 1.6)
Ascent        12 m     0 min    32 min  TX21/35 (CCR @ 1.6)
Stop          12 m     2 min    34 min  TX21/35 (CCR @ 1.6)
Ascent         9 m     0 min    34 min  TX21/35 (CCR @ 1.6)
Stop           9 m     2 min    36 min  TX21/35 (CCR @ 1.6)
Ascent         6 m     0 min    37 min  TX21/35 (CCR @ 1.6)
Stop           6 m    13 min    50 min  TX21/35 (CCR @ 1.6)
Ascent         0 m     6 min    56 min  TX21/35 (CCR @ 1.6)

CNS: 64.42 %
OTU: 87.85

Cylinder TX21/35: used 26.92 L, end pressure 230.86 bar
Cylinder O2: used 48.63 L, end pressure 183.57 bar
Cylinder NX50: used 0 L, end pressure 207 bar
Reserve satisfied: True
```

### Reference

Tool / version:

| Depth | Stop time | Runtime | Gas |
| --- | --- | --- | --- |
|  |  |  |  |

Total runtime:
Gas used:
CNS / OTU:
Differences from the plan above:

---

## (60 m, 20 min) — Diluent Trimix 18/45 @ 1.2 / 1.6 bar

`CreatePlan_ShouldMatchReferenceSchedule_WhenClosedCircuitAt60MetersOnTrimix1845`

### Profile

| Level | Depth | Time at level | Apparatus |
| --- | --- | --- | --- |
| 1 | 60 m | 20 min | CCR @ 1.2 bar |

Descent: 3.33 min.

### Gases

| Gas | Size | Fill | Purpose |
| --- | --- | --- | --- |
| Trimix 18/45 | 24 L | 232 bar | Diluent |
| Oxygen | 3 L | 200 bar | Loop supply |
| Trimix 35/25 | 22.2 L | 207 bar | Bailout |
| Nitrox 50 | 22.2 L | 207 bar | Bailout |

### Settings

| Setting | Value | Setting | Value |
| --- | --- | --- | --- |
| Gradient factors | **30 / 80** | Reserve pressure | 40 bar |
| Setpoint | **1.2 bottom / 1.6 ascent** | Reserve stress factor | 2 |
| Surface pressure | **1013.25 mbar** | Reserve team size | 2 |
| Salinity | Salt (1030) | Stop increment | 1 min |
| Descent rate | 18 m/min | Problem-solving | 1 min |
| Ascent below 75% | 9 m/min | Gas switch | 2 min |
| Ascent 75-50% | 9 m/min | O2 break interval | 20 min |
| Ascent 50% to stops | 9 m/min | O2 break duration | 5 min |
| Ascent last 6 m | 1 m/min | Safety stop | off |
| Bottom SAC | 18 L/min | Last stop | 6 m |
| Deco SAC | 14 L/min | Switch at required stop only | off |
| Bottom metabolic O2 | 1 L/min | Oxygen breaks | off |
| Deco metabolic O2 | 0.6 L/min | Oxygen is narcotic | yes |
| Loop volume | 6 L | MOD model | Realistic |
| Bottom pO2 | 1.42 bar | Deco pO2 | 1.62 bar |

### Bitenovac plan

```
Valid: True, total runtime: 68.33333333

Descent       60 m     3 min     3 min  TX18/45 (CCR @ 1.2)
Bottom        60 m    20 min    23 min  TX18/45 (CCR @ 1.2)
Ascent        24 m     4 min    27 min  TX18/45 (CCR @ 1.6)
Stop          24 m     1 min    28 min  TX18/45 (CCR @ 1.6)
Ascent        21 m     0 min    29 min  TX18/45 (CCR @ 1.6)
Stop          21 m     2 min    31 min  TX18/45 (CCR @ 1.6)
Ascent        18 m     0 min    31 min  TX18/45 (CCR @ 1.6)
Stop          18 m     1 min    32 min  TX18/45 (CCR @ 1.6)
Ascent        15 m     0 min    32 min  TX18/45 (CCR @ 1.6)
Stop          15 m     2 min    34 min  TX18/45 (CCR @ 1.6)
Ascent        12 m     0 min    35 min  TX18/45 (CCR @ 1.6)
Stop          12 m     4 min    39 min  TX18/45 (CCR @ 1.6)
Ascent         9 m     0 min    39 min  TX18/45 (CCR @ 1.6)
Stop           9 m     4 min    43 min  TX18/45 (CCR @ 1.6)
Ascent         6 m     0 min    43 min  TX18/45 (CCR @ 1.6)
Stop           6 m    19 min    62 min  TX18/45 (CCR @ 1.6)
Ascent         0 m     6 min    68 min  TX18/45 (CCR @ 1.6)

CNS: 99.5 %
OTU: 116.75

Cylinder TX18/45: used 35.89 L, end pressure 230.48 bar
Cylinder O2: used 55.13 L, end pressure 181.38 bar
Cylinder TX35/25: used 0 L, end pressure 207 bar
Cylinder NX50: used 0 L, end pressure 207 bar
Reserve satisfied: True
```

### Reference

Tool / version:

| Depth | Stop time | Runtime | Gas |
| --- | --- | --- | --- |
|  |  |  |  |

Total runtime:
Gas used:
CNS / OTU:
Differences from the plan above:

---

## (75 m, 20 min) — Diluent Trimix 15/55 @ 1.2 / 1.6 bar

`CreatePlan_ShouldMatchReferenceSchedule_WhenClosedCircuitAt75MetersOnTrimix1555`

### Profile

| Level | Depth | Time at level | Apparatus |
| --- | --- | --- | --- |
| 1 | 75 m | 20 min | CCR @ 1.2 bar |

Descent: 4.17 min.

### Gases

| Gas | Size | Fill | Purpose |
| --- | --- | --- | --- |
| Trimix 15/55 | 24 L | 232 bar | Diluent |
| Oxygen | 3 L | 200 bar | Loop supply |
| Trimix 35/25 | 22.2 L | 207 bar | Bailout |
| Nitrox 50 | 22.2 L | 207 bar | Bailout |

### Settings

| Setting | Value | Setting | Value |
| --- | --- | --- | --- |
| Gradient factors | **25 / 80** | Reserve pressure | 40 bar |
| Setpoint | **1.2 bottom / 1.6 ascent** | Reserve stress factor | 2 |
| Surface pressure | **1013.25 mbar** | Reserve team size | 2 |
| Salinity | Salt (1030) | Stop increment | 1 min |
| Descent rate | 18 m/min | Problem-solving | 1 min |
| Ascent below 75% | 9 m/min | Gas switch | 2 min |
| Ascent 75-50% | 9 m/min | O2 break interval | 20 min |
| Ascent 50% to stops | 9 m/min | O2 break duration | 5 min |
| Ascent last 6 m | 1 m/min | Safety stop | off |
| Bottom SAC | 18 L/min | Last stop | 6 m |
| Deco SAC | 14 L/min | Switch at required stop only | off |
| Bottom metabolic O2 | 1 L/min | Oxygen breaks | off |
| Deco metabolic O2 | 0.6 L/min | Oxygen is narcotic | yes |
| Loop volume | 6 L | MOD model | Realistic |
| Bottom pO2 | 1.42 bar | Deco pO2 | 1.62 bar |

### Bitenovac plan

```
Valid: True, total runtime: 99.83333333

Descent       75 m     4 min     4 min  TX15/55 (CCR @ 1.2)
Bottom        75 m    20 min    24 min  TX15/55 (CCR @ 1.2)
Ascent        36 m     4 min    28 min  TX15/55 (CCR @ 1.6)
Stop          36 m     1 min    29 min  TX15/55 (CCR @ 1.6)
Ascent        33 m     0 min    30 min  TX15/55 (CCR @ 1.6)
Stop          33 m     1 min    31 min  TX15/55 (CCR @ 1.6)
Ascent        30 m     0 min    31 min  TX15/55 (CCR @ 1.6)
Stop          30 m     1 min    32 min  TX15/55 (CCR @ 1.6)
Ascent        27 m     0 min    32 min  TX15/55 (CCR @ 1.6)
Stop          27 m     2 min    34 min  TX15/55 (CCR @ 1.6)
Ascent        24 m     0 min    35 min  TX15/55 (CCR @ 1.6)
Stop          24 m     2 min    37 min  TX15/55 (CCR @ 1.6)
Ascent        21 m     0 min    37 min  TX15/55 (CCR @ 1.6)
Stop          21 m     2 min    39 min  TX15/55 (CCR @ 1.6)
Ascent        18 m     0 min    39 min  TX15/55 (CCR @ 1.6)
Stop          18 m     4 min    43 min  TX15/55 (CCR @ 1.6)
Ascent        15 m     0 min    44 min  TX15/55 (CCR @ 1.6)
Stop          15 m     4 min    48 min  TX15/55 (CCR @ 1.6)
Ascent        12 m     0 min    48 min  TX15/55 (CCR @ 1.6)
Stop          12 m     5 min    53 min  TX15/55 (CCR @ 1.6)
Ascent         9 m     0 min    53 min  TX15/55 (CCR @ 1.6)
Stop           9 m     8 min    61 min  TX15/55 (CCR @ 1.6)
Ascent         6 m     0 min    62 min  TX15/55 (CCR @ 1.6)
Stop           6 m    32 min    94 min  TX15/55 (CCR @ 1.6)
Ascent         0 m     6 min   100 min  TX15/55 (CCR @ 1.6)

CNS: 165.63 %
OTU: 177.29

Cylinder TX15/55: used 44.86 L, end pressure 230.11 bar
Cylinder O2: used 75.03 L, end pressure 174.66 bar
Cylinder TX35/25: used 0 L, end pressure 207 bar
Cylinder NX50: used 0 L, end pressure 207 bar
Reserve satisfied: True
```

### Reference

Tool / version:

| Depth | Stop time | Runtime | Gas |
| --- | --- | --- | --- |
|  |  |  |  |

Total runtime:
Gas used:
CNS / OTU:
Differences from the plan above:

---

## (100 m, 15 min) — Diluent Trimix 10/70 @ 1.2 / 1.6 bar

`CreatePlan_ShouldMatchReferenceSchedule_WhenClosedCircuitAt100MetersOnTrimix1070`

### Profile

| Level | Depth | Time at level | Apparatus |
| --- | --- | --- | --- |
| 1 | 100 m | 15 min | CCR @ 1.2 bar |

Descent: 5.56 min.

### Gases

| Gas | Size | Fill | Purpose |
| --- | --- | --- | --- |
| Trimix 10/70 | 24 L | 232 bar | Diluent |
| Oxygen | 3 L | 200 bar | Loop supply |
| Trimix 21/35 | 22.2 L | 207 bar | Bailout |
| Trimix 35/25 | 22.2 L | 207 bar | Bailout |
| Nitrox 50 | 22.2 L | 207 bar | Bailout |

### Settings

| Setting | Value | Setting | Value |
| --- | --- | --- | --- |
| Gradient factors | **20 / 80** | Reserve pressure | 40 bar |
| Setpoint | **1.2 bottom / 1.6 ascent** | Reserve stress factor | 2 |
| Surface pressure | **1013.25 mbar** | Reserve team size | 2 |
| Salinity | Salt (1030) | Stop increment | 1 min |
| Descent rate | 18 m/min | Problem-solving | 1 min |
| Ascent below 75% | 9 m/min | Gas switch | 2 min |
| Ascent 75-50% | 9 m/min | O2 break interval | 20 min |
| Ascent 50% to stops | 9 m/min | O2 break duration | 5 min |
| Ascent last 6 m | 1 m/min | Safety stop | off |
| Bottom SAC | 18 L/min | Last stop | 6 m |
| Deco SAC | 14 L/min | Switch at required stop only | off |
| Bottom metabolic O2 | 1 L/min | Oxygen breaks | off |
| Deco metabolic O2 | 0.6 L/min | Oxygen is narcotic | yes |
| Loop volume | 6 L | MOD model | Realistic |
| Bottom pO2 | 1.42 bar | Deco pO2 | 1.62 bar |

### Bitenovac plan

```
Valid: True, total runtime: 141.999999995

Descent      100 m     6 min     6 min  TX10/70 (CCR @ 1.2)
Bottom       100 m    15 min    21 min  TX10/70 (CCR @ 1.2)
Ascent        54 m     5 min    26 min  TX10/70 (CCR @ 1.6)
Stop          54 m     1 min    27 min  TX10/70 (CCR @ 1.6)
Ascent        51 m     0 min    27 min  TX10/70 (CCR @ 1.6)
Stop          51 m     1 min    28 min  TX10/70 (CCR @ 1.6)
Ascent        45 m     1 min    29 min  TX10/70 (CCR @ 1.6)
Stop          45 m     1 min    30 min  TX10/70 (CCR @ 1.6)
Ascent        42 m     0 min    30 min  TX10/70 (CCR @ 1.6)
Stop          42 m     1 min    31 min  TX10/70 (CCR @ 1.6)
Ascent        39 m     0 min    31 min  TX10/70 (CCR @ 1.6)
Stop          39 m     2 min    33 min  TX10/70 (CCR @ 1.6)
Ascent        36 m     0 min    34 min  TX10/70 (CCR @ 1.6)
Stop          36 m     1 min    35 min  TX10/70 (CCR @ 1.6)
Ascent        33 m     0 min    35 min  TX10/70 (CCR @ 1.6)
Stop          33 m     2 min    37 min  TX10/70 (CCR @ 1.6)
Ascent        30 m     0 min    37 min  TX10/70 (CCR @ 1.6)
Stop          30 m     2 min    39 min  TX10/70 (CCR @ 1.6)
Ascent        27 m     0 min    40 min  TX10/70 (CCR @ 1.6)
Stop          27 m     3 min    43 min  TX10/70 (CCR @ 1.6)
Ascent        24 m     0 min    43 min  TX10/70 (CCR @ 1.6)
Stop          24 m     4 min    47 min  TX10/70 (CCR @ 1.6)
Ascent        21 m     0 min    47 min  TX10/70 (CCR @ 1.6)
Stop          21 m     4 min    51 min  TX10/70 (CCR @ 1.6)
Ascent        18 m     0 min    52 min  TX10/70 (CCR @ 1.6)
Stop          18 m     5 min    57 min  TX10/70 (CCR @ 1.6)
Ascent        15 m     0 min    57 min  TX10/70 (CCR @ 1.6)
Stop          15 m     8 min    65 min  TX10/70 (CCR @ 1.6)
Ascent        12 m     0 min    65 min  TX10/70 (CCR @ 1.6)
Stop          12 m     8 min    73 min  TX10/70 (CCR @ 1.6)
Ascent         9 m     0 min    74 min  TX10/70 (CCR @ 1.6)
Stop           9 m    12 min    86 min  TX10/70 (CCR @ 1.6)
Ascent         6 m     0 min    86 min  TX10/70 (CCR @ 1.6)
Stop           6 m    50 min   136 min  TX10/70 (CCR @ 1.6)
Ascent         0 m     6 min   142 min  TX10/70 (CCR @ 1.6)

CNS: 259.63 %
OTU: 257.37

Cylinder TX10/70: used 59.81 L, end pressure 229.47 bar
Cylinder O2: used 100 L, end pressure 166.23 bar
Cylinder TX21/35: used 0 L, end pressure 207 bar
Cylinder TX35/25: used 0 L, end pressure 207 bar
Cylinder NX50: used 0 L, end pressure 207 bar
Reserve satisfied: True
```

### Reference

Tool / version:

| Depth | Stop time | Runtime | Gas |
| --- | --- | --- | --- |
|  |  |  |  |

Total runtime:
Gas used:
CNS / OTU:
Differences from the plan above:
