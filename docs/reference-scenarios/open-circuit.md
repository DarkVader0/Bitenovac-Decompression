# Open-circuit reference scenarios

Each section is one test: full setup, the schedule this implementation produces, then an empty
block for the reference tool. Every setting is listed in full, so nothing has to be looked up
elsewhere. **Bold** marks a value that differs from the shared default.

**Level durations are time *at* the level.** Descent and inter-level travel are generated on top of
them. Subsurface's planner rows work the other way round — a segment's duration includes the
transit into it — so add the stated descent time to the first level, or switch the planner to
runtime entry. Getting this wrong shifts every stop.

Water densities: fresh 1000, brackish 1015, EN 13319 1020, salt 1030 kg/m³.

---

## (42 m, 25 min + 27 m, 15 min) — Air, Nitrox 50

`CreatePlan_ShouldMatchReferenceSchedule_WhenAirWreckAt42MetersWithNitrox50Deco`

### Profile

| Level | Depth | Time at level |
| --- | --- | --- |
| 1 | 42 m | 25 min |
| 2 | 27 m | 15 min |

Descent to 42 m: 2.33 min. Travel 42 → 27 m generated.

### Gases

| Gas | Size | Fill | Purpose |
| --- | --- | --- | --- |
| Air (21/0) | 24 L | 232 bar | Bottom |
| Nitrox 50 | 11.1 L | 200 bar | Deco |

### Settings

| Setting | Value | Setting | Value |
| --- | --- | --- | --- |
| Gradient factors | **30 / 70** | Reserve pressure | 40 bar |
| Surface pressure | 1000 mbar | Reserve stress factor | 2 |
| Salinity | Salt (1030) | Reserve team size | 2 |
| Descent rate | 18 m/min | Stop increment | 1 min |
| Ascent below 75% | 9 m/min | Problem-solving | 1 min |
| Ascent 75-50% | 9 m/min | Gas switch | 2 min |
| Ascent 50% to stops | 9 m/min | O2 break interval | 20 min |
| Ascent last 6 m | 1 m/min | O2 break duration | 5 min |
| Bottom SAC | 18 L/min | Safety stop | off |
| Deco SAC | 14 L/min | Last stop | 6 m |
| Bottom pO2 | 1.42 bar | Switch at required stop only | off |
| Deco pO2 | 1.62 bar | Oxygen breaks | off |
| MOD model | Realistic | Oxygen is narcotic | yes |

### Bitenovac plan

```
Valid: True, total runtime: 101.33333333

Descent       42 m     2 min     2 min  Air
Bottom        42 m    25 min    27 min  Air
Ascent        27 m     2 min    29 min  Air
Bottom        27 m    15 min    44 min  Air
Ascent        21 m     1 min    45 min  Air
GasSwitch     21 m     2 min    47 min  NX50
Ascent        12 m     1 min    48 min  NX50
Stop          12 m     3 min    51 min  NX50
Ascent         9 m     0 min    51 min  NX50
Stop           9 m     7 min    58 min  NX50
Ascent         6 m     0 min    58 min  NX50
Stop           6 m    37 min    95 min  NX50
Ascent         0 m     6 min   101 min  NX50

CNS: 29.72 %
OTU: 83.76

Cylinder Air: used 3735.3 L, end pressure 76.36 bar
Cylinder NX50: used 1398.8 L, end pressure 73.98 bar
Reserve satisfied: False
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

## (55 m, 20 min) — Air, Nitrox 50, Oxygen

`CreatePlan_ShouldMatchReferenceSchedule_WhenDeepAirAt55MetersWithRawBuhlmannGradientFactors`

### Profile

| Level | Depth | Time at level |
| --- | --- | --- |
| 1 | 55 m | 20 min |

Descent: 3.06 min.

### Gases

| Gas | Size | Fill | Purpose |
| --- | --- | --- | --- |
| Air (21/0) | 24 L | 232 bar | Bottom |
| Nitrox 50 | 11.1 L | 200 bar | Deco |
| Oxygen | 7 L | 200 bar | Deco |

### Settings

| Setting | Value | Setting | Value |
| --- | --- | --- | --- |
| Gradient factors | **100 / 100** | Reserve pressure | 40 bar |
| Surface pressure | 1000 mbar | Reserve stress factor | 2 |
| Salinity | Salt (1030) | Reserve team size | 2 |
| Descent rate | 18 m/min | Stop increment | 1 min |
| Ascent below 75% | **10 m/min** | Problem-solving | 1 min |
| Ascent 75-50% | **10 m/min** | Gas switch | 2 min |
| Ascent 50% to stops | **10 m/min** | O2 break interval | 20 min |
| Ascent last 6 m | 1 m/min | O2 break duration | 5 min |
| Bottom SAC | 18 L/min | Safety stop | off |
| Deco SAC | 14 L/min | Last stop | 6 m |
| Bottom pO2 | 1.42 bar | Switch at required stop only | off |
| Deco pO2 | 1.62 bar | Oxygen breaks | **on** |
| MOD model | Realistic | Oxygen is narcotic | yes |

### Bitenovac plan

```
Valid: True, total runtime: 51.95555555333333

Descent       55 m     3 min     3 min  Air
Bottom        55 m    20 min    23 min  Air
Ascent        21 m     3 min    26 min  Air
GasSwitch     21 m     2 min    28 min  NX50
Ascent         9 m     1 min    30 min  NX50
Stop           9 m     3 min    33 min  NX50
Ascent         6 m     0 min    33 min  NX50
GasSwitch      6 m     2 min    35 min  O2
Stop           6 m    11 min    46 min  O2
Ascent         0 m     6 min    52 min  O2

CNS: 53.23 %
OTU: 79.75

Cylinder Air: used 2974.95 L, end pressure 108.04 bar
Cylinder NX50: used 250.63 L, end pressure 177.42 bar
Cylinder O2: used 413.15 L, end pressure 140.98 bar
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

## (33 m, 20 min + 21 m, 20 min) — Nitrox 32, Nitrox 50

`CreatePlan_ShouldMatchReferenceSchedule_WhenMultiLevelNitrox32ReefWithInstantGasSwitches`

### Profile

| Level | Depth | Time at level |
| --- | --- | --- |
| 1 | 33 m | 20 min |
| 2 | 21 m | 20 min |

Descent to 33 m: 2.75 min at the reduced rate. Travel 33 → 21 m generated.

### Gases

| Gas | Size | Fill | Purpose |
| --- | --- | --- | --- |
| Nitrox 32 | 15 L | 232 bar | Bottom |
| Nitrox 50 | 11.1 L | 200 bar | Deco |

### Settings

| Setting | Value | Setting | Value |
| --- | --- | --- | --- |
| Gradient factors | **45 / 95** | Reserve pressure | 40 bar |
| Surface pressure | 1000 mbar | Reserve stress factor | 2 |
| Salinity | Salt (1030) | Reserve team size | 2 |
| Descent rate | **12 m/min** | Stop increment | 1 min |
| Ascent below 75% | 9 m/min | Problem-solving | 1 min |
| Ascent 75-50% | 9 m/min | Gas switch | **0 min** |
| Ascent 50% to stops | 9 m/min | O2 break interval | 20 min |
| Ascent last 6 m | 1 m/min | O2 break duration | 5 min |
| Bottom SAC | 18 L/min | Safety stop | **on** |
| Deco SAC | 14 L/min | Last stop | **3 m** |
| Bottom pO2 | 1.42 bar | Switch at required stop only | off |
| Deco pO2 | 1.62 bar | Oxygen breaks | off |
| MOD model | Realistic | Oxygen is narcotic | yes |

### Bitenovac plan

```
Valid: True, total runtime: 51.749999996666666

Descent       33 m     3 min     3 min  NX32
Bottom        33 m    20 min    23 min  NX32
Ascent        21 m     1 min    24 min  NX32
Bottom        21 m    20 min    44 min  NX32
Ascent         0 m     8 min    52 min  NX50

CNS: 22.48 %
OTU: 59.01

Cylinder NX32: used 3034.19 L, end pressure 29.72 bar
Cylinder NX50: used 124.36 L, end pressure 188.8 bar
Reserve satisfied: False
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

## (45 m, 30 min) — Trimix 21/35, Nitrox 50 — fresh water

`CreatePlan_ShouldMatchReferenceSchedule_WhenNormoxicTrimix2135At45MetersInAFreshWaterLake`

### Profile

| Level | Depth | Time at level |
| --- | --- | --- |
| 1 | 45 m | 30 min |

Descent: 3.00 min.

### Gases

| Gas | Size | Fill | Purpose |
| --- | --- | --- | --- |
| Trimix 21/35 | 24 L | 232 bar | Bottom |
| Nitrox 50 | 11.1 L | 200 bar | Deco |

### Settings

| Setting | Value | Setting | Value |
| --- | --- | --- | --- |
| Gradient factors | **35 / 75** | Reserve pressure | 40 bar |
| Surface pressure | 1000 mbar | Reserve stress factor | **1.5** |
| Salinity | **Fresh (1000)** | Reserve team size | **1** |
| Descent rate | **15 m/min** | Stop increment | 1 min |
| Ascent below 75% | 9 m/min | Problem-solving | 1 min |
| Ascent 75-50% | 9 m/min | Gas switch | 2 min |
| Ascent 50% to stops | 9 m/min | O2 break interval | 20 min |
| Ascent last 6 m | 1 m/min | O2 break duration | 5 min |
| Bottom SAC | 18 L/min | Safety stop | off |
| Deco SAC | 14 L/min | Last stop | **3 m** |
| Bottom pO2 | 1.42 bar | Switch at required stop only | off |
| Deco pO2 | 1.62 bar | Oxygen breaks | off |
| MOD model | Realistic | Oxygen is narcotic | yes |

### Bitenovac plan

```
Valid: True, total runtime: 77.33333333

Descent       45 m     3 min     3 min  TX21/35
Bottom        45 m    30 min    33 min  TX21/35
Ascent        21 m     3 min    36 min  TX21/35
GasSwitch     21 m     2 min    38 min  NX50
Ascent        15 m     1 min    38 min  NX50
Stop          15 m     1 min    39 min  NX50
Ascent        12 m     0 min    40 min  NX50
Stop          12 m     3 min    43 min  NX50
Ascent         9 m     0 min    43 min  NX50
Stop           9 m     5 min    48 min  NX50
Ascent         6 m     0 min    48 min  NX50
Stop           6 m     9 min    57 min  NX50
Ascent         3 m     3 min    60 min  NX50
Stop           3 m    14 min    74 min  NX50
Ascent         0 m     3 min    77 min  NX50

CNS: 24.75 %
OTU: 68.22

Cylinder TX21/35: used 3395.36 L, end pressure 90.53 bar
Cylinder NX50: used 1010.93 L, end pressure 108.93 bar
Reserve satisfied: False
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

## (60 m, 25 min) — Trimix 18/45, Nitrox 50, Oxygen

`CreatePlan_ShouldMatchReferenceSchedule_WhenTrimix1845At60MetersWithNitrox50AndOxygen`

### Profile

| Level | Depth | Time at level |
| --- | --- | --- |
| 1 | 60 m | 25 min |

Descent: 3.33 min.

### Gases

| Gas | Size | Fill | Purpose |
| --- | --- | --- | --- |
| Trimix 18/45 | 24 L | 232 bar | Bottom |
| Nitrox 50 | 11.1 L | 200 bar | Deco |
| Oxygen | 7 L | 200 bar | Deco |

### Settings

| Setting | Value | Setting | Value |
| --- | --- | --- | --- |
| Gradient factors | **30 / 85** | Reserve pressure | 40 bar |
| Surface pressure | **1013.25 mbar** | Reserve stress factor | 2 |
| Salinity | Salt (1030) | Reserve team size | 2 |
| Descent rate | 18 m/min | Stop increment | 1 min |
| Ascent below 75% | 9 m/min | Problem-solving | 1 min |
| Ascent 75-50% | 9 m/min | Gas switch | **3 min** |
| Ascent 50% to stops | 9 m/min | O2 break interval | 20 min |
| Ascent last 6 m | 1 m/min | O2 break duration | 5 min |
| Bottom SAC | 18 L/min | Safety stop | off |
| Deco SAC | 14 L/min | Last stop | 6 m |
| Bottom pO2 | 1.42 bar | Switch at required stop only | off |
| Deco pO2 | 1.62 bar | Oxygen breaks | off |
| MOD model | Realistic | Oxygen is narcotic | yes |

### Bitenovac plan

```
Valid: True, total runtime: 89.33333333

Descent       60 m     3 min     3 min  TX18/45
Bottom        60 m    25 min    28 min  TX18/45
Ascent        30 m     3 min    32 min  TX18/45
Stop          30 m     1 min    33 min  TX18/45
Ascent        27 m     0 min    33 min  TX18/45
Stop          27 m     2 min    35 min  TX18/45
Ascent        24 m     0 min    35 min  TX18/45
Stop          24 m     2 min    37 min  TX18/45
Ascent        21 m     0 min    38 min  TX18/45
GasSwitch     21 m     3 min    41 min  NX50
Ascent        15 m     1 min    41 min  NX50
Stop          15 m     4 min    45 min  NX50
Ascent        12 m     0 min    46 min  NX50
Stop          12 m     4 min    50 min  NX50
Ascent         9 m     0 min    50 min  NX50
Stop           9 m     7 min    57 min  NX50
Ascent         6 m     0 min    57 min  NX50
GasSwitch      6 m     3 min    60 min  O2
Stop           6 m    23 min    83 min  O2
Ascent         0 m     6 min    89 min  O2

CNS: 98.74 %
OTU: 127.85

Cylinder TX18/45: used 4168.46 L, end pressure 56.01 bar
Cylinder NX50: used 681.59 L, end pressure 137.78 bar
Cylinder O2: used 708.9 L, end pressure 97.39 bar
Reserve satisfied: False
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

## (75 m, 20 min) — Trimix 15/55, Trimix 35/25, Nitrox 50, Oxygen

`CreatePlan_ShouldMatchReferenceSchedule_WhenTrimix1555At75MetersSwitchingOnlyAtRequiredStops`

### Profile

| Level | Depth | Time at level |
| --- | --- | --- |
| 1 | 75 m | 20 min |

Descent: 3.75 min.

### Gases

| Gas | Size | Fill | Purpose |
| --- | --- | --- | --- |
| Trimix 15/55 | 24 L | 232 bar | Bottom |
| Trimix 35/25 | 11.1 L | 200 bar | Deco |
| Nitrox 50 | 11.1 L | 200 bar | Deco |
| Oxygen | 7 L | 200 bar | Deco |

### Settings

| Setting | Value | Setting | Value |
| --- | --- | --- | --- |
| Gradient factors | **30 / 80** | Reserve pressure | 40 bar |
| Surface pressure | 1000 mbar | Reserve stress factor | 2 |
| Salinity | Salt (1030) | Reserve team size | 2 |
| Descent rate | **20 m/min** | Stop increment | **0.5 min** |
| Ascent below 75% | 9 m/min | Problem-solving | 1 min |
| Ascent 75-50% | 9 m/min | Gas switch | 2 min |
| Ascent 50% to stops | 9 m/min | O2 break interval | 20 min |
| Ascent last 6 m | 1 m/min | O2 break duration | 5 min |
| Bottom SAC | 18 L/min | Safety stop | off |
| Deco SAC | 14 L/min | Last stop | 6 m |
| Bottom pO2 | 1.42 bar | Switch at required stop only | **on** |
| Deco pO2 | 1.62 bar | Oxygen breaks | **on** |
| MOD model | Realistic | Oxygen is narcotic | **no** |

### Bitenovac plan

```
Valid: True, total runtime: 106.91666666333333

Descent       75 m     4 min     4 min  TX15/55
Bottom        75 m    20 min    24 min  TX15/55
Ascent        39 m     4 min    28 min  TX15/55
Stop          39 m     1 min    29 min  TX15/55
Ascent        36 m     0 min    29 min  TX15/55
Stop          36 m     2 min    31 min  TX15/55
Ascent        33 m     0 min    31 min  TX15/55
GasSwitch     33 m     2 min    33 min  TX35/25
Ascent        27 m     1 min    34 min  TX35/25
Stop          27 m     2 min    35 min  TX35/25
Ascent        24 m     0 min    35 min  TX35/25
Stop          24 m     2 min    37 min  TX35/25
Ascent        21 m     0 min    38 min  TX35/25
GasSwitch     21 m     2 min    40 min  NX50
Stop          21 m     0 min    40 min  NX50
Ascent        18 m     0 min    41 min  NX50
Stop          18 m     2 min    43 min  NX50
Ascent        15 m     0 min    43 min  NX50
Stop          15 m     4 min    48 min  NX50
Ascent        12 m     0 min    48 min  NX50
Stop          12 m     6 min    54 min  NX50
Ascent         9 m     0 min    55 min  NX50
Stop           9 m    10 min    64 min  NX50
Ascent         6 m     0 min    64 min  NX50
GasSwitch      6 m     2 min    66 min  O2
Stop           6 m    20 min    86 min  O2
Stop           6 m     5 min    91 min  NX50
Stop           6 m    10 min   101 min  O2
Ascent         0 m     6 min   107 min  O2

CNS: 108.41 %
OTU: 153.36

Cylinder TX15/55: used 4312.58 L, end pressure 52.31 bar
Cylinder TX35/25: used 415.96 L, end pressure 162.53 bar
Cylinder NX50: used 1009.81 L, end pressure 109.03 bar
Cylinder O2: used 829.12 L, end pressure 81.55 bar
Reserve satisfied: False
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

## (100 m, 15 min) — Trimix 10/70, Trimix 21/35, Nitrox 50, Oxygen

`CreatePlan_ShouldMatchReferenceSchedule_WhenHypoxicTrimix1070At100MetersWithThreeDecoGases`

### Profile

| Level | Depth | Time at level |
| --- | --- | --- |
| 1 | 100 m | 15 min |

Descent: 5.00 min.

### Gases

| Gas | Size | Fill | Purpose |
| --- | --- | --- | --- |
| Trimix 10/70 | 24 L | 232 bar | Bottom |
| Trimix 21/35 | 11.1 L | 207 bar | Deco |
| Nitrox 50 | 22.2 L | 207 bar | Deco |
| Oxygen | 11.1 L | 200 bar | Deco |

### Settings

| Setting | Value | Setting | Value |
| --- | --- | --- | --- |
| Gradient factors | **25 / 80** | Reserve pressure | 40 bar |
| Surface pressure | 1000 mbar | Reserve stress factor | **3** |
| Salinity | Salt (1030) | Reserve team size | 2 |
| Descent rate | **20 m/min** | Stop increment | 1 min |
| Ascent below 75% | 9 m/min | Problem-solving | 1 min |
| Ascent 75-50% | 9 m/min | Gas switch | 2 min |
| Ascent 50% to stops | 9 m/min | O2 break interval | **25 min** |
| Ascent last 6 m | 1 m/min | O2 break duration | 5 min |
| Bottom SAC | **20 L/min** | Safety stop | off |
| Deco SAC | **15 L/min** | Last stop | 6 m |
| Bottom pO2 | 1.42 bar | Switch at required stop only | off |
| Deco pO2 | 1.62 bar | Oxygen breaks | **on** |
| MOD model | Realistic | Oxygen is narcotic | **no** |

### Bitenovac plan

```
Valid: True, total runtime: 150.44444444166666

Descent      100 m     5 min     5 min  TX10/70
Bottom       100 m    15 min    20 min  TX10/70
Ascent        66 m     4 min    24 min  TX10/70
GasSwitch     66 m     2 min    26 min  TX21/35
Ascent        45 m     2 min    28 min  TX21/35
Stop          45 m     1 min    29 min  TX21/35
Ascent        42 m     0 min    29 min  TX21/35
Stop          42 m     1 min    30 min  TX21/35
Ascent        39 m     0 min    31 min  TX21/35
Stop          39 m     1 min    32 min  TX21/35
Ascent        36 m     0 min    32 min  TX21/35
Stop          36 m     2 min    34 min  TX21/35
Ascent        33 m     0 min    34 min  TX21/35
Stop          33 m     2 min    36 min  TX21/35
Ascent        30 m     0 min    37 min  TX21/35
Stop          30 m     3 min    40 min  TX21/35
Ascent        27 m     0 min    40 min  TX21/35
Stop          27 m     3 min    43 min  TX21/35
Ascent        24 m     0 min    43 min  TX21/35
Stop          24 m     4 min    47 min  TX21/35
Ascent        21 m     0 min    48 min  TX21/35
GasSwitch     21 m     2 min    50 min  NX50
Stop          21 m     2 min    52 min  NX50
Ascent        18 m     0 min    52 min  NX50
Stop          18 m     5 min    57 min  NX50
Ascent        15 m     0 min    57 min  NX50
Stop          15 m     7 min    64 min  NX50
Ascent        12 m     0 min    65 min  NX50
Stop          12 m     8 min    73 min  NX50
Ascent         9 m     0 min    73 min  NX50
Stop           9 m    15 min    88 min  NX50
Ascent         6 m     0 min    88 min  NX50
GasSwitch      6 m     2 min    90 min  O2
Stop           6 m    25 min   115 min  O2
Stop           6 m     5 min   120 min  NX50
Stop           6 m    24 min   144 min  O2
Ascent         0 m     6 min   150 min  O2

CNS: 161.4 %
OTU: 207.81

Cylinder TX10/70: used 5040.47 L, end pressure 21.98 bar
Cylinder TX21/35: used 1891.14 L, end pressure 36.63 bar
Cylinder NX50: used 1583.12 L, end pressure 135.69 bar
Cylinder O2: used 1364.69 L, end pressure 77.05 bar
Reserve satisfied: False
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

## (90 m, 15 min) — Trimix 12/60, Trimix 21/35, Nitrox 50, Nitrox 80 — brackish water

`CreatePlan_ShouldMatchReferenceSchedule_WhenTrimix1260At90MetersInBrackishWaterWithNitrox80`

### Profile

| Level | Depth | Time at level |
| --- | --- | --- |
| 1 | 90 m | 15 min |

Descent: 5.00 min.

### Gases

| Gas | Size | Fill | Purpose |
| --- | --- | --- | --- |
| Trimix 12/60 | 24 L | 232 bar | Bottom |
| Trimix 21/35 | 11.1 L | 207 bar | Deco |
| Nitrox 50 | 11.1 L | 207 bar | Deco |
| Nitrox 80 | 11.1 L | 200 bar | Deco |

### Settings

| Setting | Value | Setting | Value |
| --- | --- | --- | --- |
| Gradient factors | **20 / 85** | Reserve pressure | **50 bar** |
| Surface pressure | 1000 mbar | Reserve stress factor | 2 |
| Salinity | **Brackish (1015)** | Reserve team size | 2 |
| Descent rate | 18 m/min | Stop increment | 1 min |
| Ascent below 75% | 9 m/min | Problem-solving | 1 min |
| Ascent 75-50% | 9 m/min | Gas switch | 2 min |
| Ascent 50% to stops | 9 m/min | O2 break interval | 20 min |
| Ascent last 6 m | 1 m/min | O2 break duration | 5 min |
| Bottom SAC | **16 L/min** | Safety stop | off |
| Deco SAC | **12 L/min** | Last stop | **3 m** |
| Bottom pO2 | 1.42 bar | Switch at required stop only | off |
| Deco pO2 | **1.5 bar** | Oxygen breaks | off |
| MOD model | Realistic | Oxygen is narcotic | **no** |

### Bitenovac plan

```
Valid: True, total runtime: 122.33333333333333

Descent       90 m     5 min     5 min  TX12/60
Bottom        90 m    15 min    20 min  TX12/60
Ascent        60 m     3 min    23 min  TX12/60
GasSwitch     60 m     2 min    25 min  TX21/35
Ascent        39 m     2 min    28 min  TX21/35
Stop          39 m     1 min    29 min  TX21/35
Ascent        36 m     0 min    29 min  TX21/35
Stop          36 m     1 min    30 min  TX21/35
Ascent        33 m     0 min    30 min  TX21/35
Stop          33 m     1 min    31 min  TX21/35
Ascent        30 m     0 min    32 min  TX21/35
Stop          30 m     2 min    34 min  TX21/35
Ascent        27 m     0 min    34 min  TX21/35
Stop          27 m     3 min    37 min  TX21/35
Ascent        24 m     0 min    37 min  TX21/35
Stop          24 m     3 min    40 min  TX21/35
Ascent        21 m     0 min    41 min  TX21/35
Stop          21 m     5 min    46 min  TX21/35
Ascent        18 m     0 min    46 min  TX21/35
GasSwitch     18 m     2 min    48 min  NX50
Stop          18 m     1 min    49 min  NX50
Ascent        15 m     0 min    49 min  NX50
Stop          15 m     5 min    54 min  NX50
Ascent        12 m     0 min    55 min  NX50
Stop          12 m     8 min    63 min  NX50
Ascent         9 m     0 min    63 min  NX50
Stop           9 m    10 min    73 min  NX50
Ascent         6 m     0 min    73 min  NX50
GasSwitch      6 m     2 min    75 min  NX80
Stop           6 m    13 min    88 min  NX80
Ascent         3 m     3 min    91 min  NX80
Stop           3 m    28 min   119 min  NX80
Ascent         0 m     3 min   122 min  NX80

CNS: 46.5 %
OTU: 132.65

Cylinder TX12/60: used 3558.53 L, end pressure 83.73 bar
Cylinder TX21/35: used 1277.84 L, end pressure 91.88 bar
Cylinder NX50: used 754.21 L, end pressure 139.05 bar
Cylinder NX80: used 846.95 L, end pressure 123.7 bar
Reserve satisfied: False
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

## (52 m, 20 min) — Trimix 18/45, Nitrox 50, Oxygen — 2200 m altitude

`CreatePlan_ShouldMatchReferenceSchedule_WhenTrimix1845At52MetersInAMountainLakeAt2200Meters`

### Profile

| Level | Depth | Time at level |
| --- | --- | --- |
| 1 | 52 m | 20 min |

Descent: 3.47 min.

### Gases

| Gas | Size | Fill | Purpose |
| --- | --- | --- | --- |
| Trimix 18/45 | 24 L | 232 bar | Bottom |
| Nitrox 50 | 11.1 L | 200 bar | Deco |
| Oxygen | 7 L | 200 bar | Deco |

### Settings

| Setting | Value | Setting | Value |
| --- | --- | --- | --- |
| Gradient factors | **30 / 75** | Reserve pressure | 40 bar |
| Surface pressure | **775.4128 mbar** | Reserve stress factor | 2 |
| Salinity | **Fresh (1000)** | Reserve team size | 2 |
| Descent rate | **15 m/min** | Stop increment | 1 min |
| Ascent below 75% | 9 m/min | Problem-solving | 1 min |
| Ascent 75-50% | 9 m/min | Gas switch | 2 min |
| Ascent 50% to stops | 9 m/min | O2 break interval | 20 min |
| Ascent last 6 m | 1 m/min | O2 break duration | 5 min |
| Bottom SAC | 18 L/min | Safety stop | off |
| Deco SAC | 14 L/min | Last stop | **3 m** |
| Bottom pO2 | 1.42 bar | Switch at required stop only | off |
| Deco pO2 | 1.62 bar | Oxygen breaks | off |
| MOD model | Realistic | Oxygen is narcotic | yes |

### Bitenovac plan

```
Valid: True, total runtime: 64.577777775

Descent       52 m     3 min     3 min  TX18/45
Bottom        52 m    20 min    23 min  TX18/45
Ascent        24 m     3 min    27 min  TX18/45
GasSwitch     24 m     2 min    29 min  NX50
Ascent        18 m     1 min    29 min  NX50
Stop          18 m     1 min    30 min  NX50
Ascent        15 m     0 min    31 min  NX50
Stop          15 m     2 min    33 min  NX50
Ascent        12 m     0 min    33 min  NX50
Stop          12 m     2 min    35 min  NX50
Ascent         9 m     0 min    35 min  NX50
Stop           9 m     6 min    41 min  NX50
Ascent         6 m     0 min    42 min  NX50
GasSwitch      6 m     2 min    44 min  O2
Stop           6 m     5 min    49 min  O2
Ascent         3 m     3 min    52 min  O2
Stop           3 m    10 min    62 min  O2
Ascent         0 m     3 min    65 min  O2

CNS: 25.99 %
OTU: 70.12

Cylinder TX18/45: used 3486.57 L, end pressure 119.35 bar
Cylinder NX50: used 619.83 L, end pressure 156.7 bar
Cylinder O2: used 508.04 L, end pressure 143.72 bar
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

## (40 m, 20 min) — Trimix 21/35, Nitrox 50 — after a 120 min surface interval

`CreatePlan_ShouldMatchReferenceSchedule_WhenRepetitiveTrimixDiveFollowsATwoHourSurfaceInterval`

### Profile

**Prior dive:** 45 m for 25 min (descent 2.50 min), same gases and settings.
**Surface interval:** 120 min breathing **air**.

| Level | Depth | Time at level |
| --- | --- | --- |
| 1 | 40 m | 20 min |

Descent: 2.22 min.

### Gases

| Gas | Size | Fill | Purpose |
| --- | --- | --- | --- |
| Trimix 21/35 | 24 L | 232 bar | Bottom |
| Nitrox 50 | 11.1 L | 200 bar | Deco |

### Settings

Both dives.

| Setting | Value | Setting | Value |
| --- | --- | --- | --- |
| Gradient factors | **30 / 85** | Reserve pressure | 40 bar |
| Surface pressure | 1000 mbar | Reserve stress factor | 2 |
| Salinity | Salt (1030) | Reserve team size | 2 |
| Descent rate | 18 m/min | Stop increment | 1 min |
| Ascent below 75% | 9 m/min | Problem-solving | 1 min |
| Ascent 75-50% | 9 m/min | Gas switch | 2 min |
| Ascent 50% to stops | 9 m/min | O2 break interval | 20 min |
| Ascent last 6 m | 1 m/min | O2 break duration | 5 min |
| Bottom SAC | 18 L/min | Safety stop | off |
| Deco SAC | 14 L/min | Last stop | 6 m |
| Bottom pO2 | 1.42 bar | Switch at required stop only | off |
| Deco pO2 | 1.62 bar | Oxygen breaks | off |
| MOD model | Realistic | Oxygen is narcotic | yes |

### Bitenovac plan

```
Valid: True, total runtime: 49.999999996666666

Descent       40 m     2 min     2 min  TX21/35
Bottom        40 m    20 min    22 min  TX21/35
Ascent        21 m     2 min    24 min  TX21/35
GasSwitch     21 m     2 min    26 min  NX50
Ascent         9 m     1 min    28 min  NX50
Stop           9 m     1 min    29 min  NX50
Ascent         6 m     0 min    29 min  NX50
Stop           6 m    15 min    44 min  NX50
Ascent         0 m     6 min    50 min  NX50

CNS: 16.23 %
OTU: 43.23

Cylinder TX21/35: used 2152.16 L, end pressure 142.33 bar
Cylinder NX50: used 651.13 L, end pressure 141.34 bar
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

## (28 m, 30 min) — Air, Nitrox 50 — third dive of the day

`CreatePlan_ShouldMatchReferenceSchedule_WhenThirdAirDiveOfTheDayAfterOxygenDuringTheSecondInterval`

### Profile

**Dive 1:** 40 m for 20 min (descent 2.22 min), then 150 min on **air**.
**Dive 2:** 32 m for 25 min (descent 1.78 min), then 90 min on **oxygen**.

| Level | Depth | Time at level |
| --- | --- | --- |
| 1 | 28 m | 30 min |

Descent: 1.56 min.

### Gases

All three dives.

| Gas | Size | Fill | Purpose |
| --- | --- | --- | --- |
| Air (21/0) | 15 L | 232 bar | Bottom |
| Nitrox 50 | 11.1 L | 200 bar | Deco |

### Settings

All three dives.

| Setting | Value | Setting | Value |
| --- | --- | --- | --- |
| Gradient factors | **40 / 85** | Reserve pressure | 40 bar |
| Surface pressure | 1000 mbar | Reserve stress factor | 2 |
| Salinity | Salt (1030) | Reserve team size | **3** |
| Descent rate | 18 m/min | Stop increment | 1 min |
| Ascent below 75% | 9 m/min | Problem-solving | 1 min |
| Ascent 75-50% | 9 m/min | Gas switch | 2 min |
| Ascent 50% to stops | 9 m/min | O2 break interval | 20 min |
| Ascent last 6 m | 1 m/min | O2 break duration | 5 min |
| Bottom SAC | 18 L/min | Safety stop | off |
| Deco SAC | 14 L/min | Last stop | **3 m** |
| Bottom pO2 | 1.42 bar | Switch at required stop only | off |
| Deco pO2 | 1.62 bar | Oxygen breaks | off |
| MOD model | Realistic | Oxygen is narcotic | yes |

### Bitenovac plan

```
Valid: True, total runtime: 41.999999996666666

Descent       28 m     2 min     2 min  Air
Bottom        28 m    30 min    32 min  Air
Ascent        21 m     1 min    32 min  Air
GasSwitch     21 m     2 min    34 min  NX50
Ascent         0 m     8 min    42 min  NX50

CNS: 11.53 %
OTU: 28.59

Cylinder Air: used 2218.14 L, end pressure 84.12 bar
Cylinder NX50: used 300.8 L, end pressure 172.9 bar
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

## (120 m, 12 min) — Trimix 8/80, Trimix 18/45, Trimix 35/25, Nitrox 50, Oxygen

`CreatePlan_ShouldMatchReferenceSchedule_WhenExpeditionTrimix880At120MetersWithFourDecoGases`

### Profile

| Level | Depth | Time at level |
| --- | --- | --- |
| 1 | 120 m | 12 min |

Descent: 6.00 min.

### Gases

| Gas | Size | Fill | Purpose |
| --- | --- | --- | --- |
| Trimix 8/80 | 32 L | 232 bar | Bottom |
| Trimix 18/45 | 11.1 L | 207 bar | Deco |
| Trimix 35/25 | 11.1 L | 207 bar | Deco |
| Nitrox 50 | 22.2 L | 207 bar | Deco |
| Oxygen | 22.2 L | 200 bar | Deco |

### Settings

| Setting | Value | Setting | Value |
| --- | --- | --- | --- |
| Gradient factors | **20 / 75** | Reserve pressure | 40 bar |
| Surface pressure | 1000 mbar | Reserve stress factor | 2 |
| Salinity | Salt (1030) | Reserve team size | 2 |
| Descent rate | **20 m/min** | Stop increment | 1 min |
| Ascent below 75% | 9 m/min | Problem-solving | **3 min** |
| Ascent 75-50% | **6 m/min** | Gas switch | 2 min |
| Ascent 50% to stops | 9 m/min | O2 break interval | **25 min** |
| Ascent last 6 m | 1 m/min | O2 break duration | **6 min** |
| Bottom SAC | 18 L/min | Safety stop | off |
| Deco SAC | **12 L/min** | Last stop | 6 m |
| Bottom pO2 | 1.42 bar | Switch at required stop only | off |
| Deco pO2 | 1.62 bar | Oxygen breaks | **on** |
| MOD model | Realistic | Oxygen is narcotic | **no** |

### Bitenovac plan

```
Valid: True, total runtime: 198.055555555

Descent      120 m     6 min     6 min  TX8/80
Bottom       120 m    12 min    18 min  TX8/80
Ascent        78 m     5 min    23 min  TX8/80
GasSwitch     78 m     2 min    25 min  TX18/45
Ascent        54 m     4 min    28 min  TX18/45
Stop          54 m     1 min    30 min  TX18/45
Ascent        51 m     0 min    30 min  TX18/45
Stop          51 m     1 min    31 min  TX18/45
Ascent        48 m     0 min    31 min  TX18/45
Stop          48 m     1 min    32 min  TX18/45
Ascent        45 m     0 min    33 min  TX18/45
Stop          45 m     2 min    35 min  TX18/45
Ascent        42 m     0 min    35 min  TX18/45
Stop          42 m     1 min    36 min  TX18/45
Ascent        39 m     0 min    36 min  TX18/45
Stop          39 m     3 min    39 min  TX18/45
Ascent        36 m     0 min    40 min  TX18/45
Stop          36 m     3 min    43 min  TX18/45
Ascent        33 m     0 min    43 min  TX18/45
GasSwitch     33 m     2 min    45 min  TX35/25
Ascent        30 m     0 min    45 min  TX35/25
Stop          30 m     3 min    48 min  TX35/25
Ascent        27 m     0 min    49 min  TX35/25
Stop          27 m     4 min    53 min  TX35/25
Ascent        24 m     0 min    53 min  TX35/25
Stop          24 m     5 min    58 min  TX35/25
Ascent        21 m     0 min    58 min  TX35/25
GasSwitch     21 m     2 min    60 min  NX50
Stop          21 m     3 min    63 min  NX50
Ascent        18 m     0 min    64 min  NX50
Stop          18 m     7 min    71 min  NX50
Ascent        15 m     0 min    71 min  NX50
Stop          15 m     9 min    80 min  NX50
Ascent        12 m     0 min    80 min  NX50
Stop          12 m    12 min    92 min  NX50
Ascent         9 m     0 min    93 min  NX50
Stop           9 m    21 min   114 min  NX50
Ascent         6 m     0 min   114 min  NX50
GasSwitch      6 m     2 min   116 min  O2
Stop           6 m    25 min   141 min  O2
Stop           6 m     6 min   147 min  NX50
Stop           6 m    25 min   172 min  O2
Stop           6 m     6 min   178 min  NX50
Stop           6 m    14 min   192 min  O2
Ascent         0 m     6 min   198 min  O2

CNS: 211.57 %
OTU: 278.67

Cylinder TX8/80: used 5062.47 L, end pressure 73.8 bar
Cylinder TX18/45: used 1842.25 L, end pressure 41.03 bar
Cylinder TX35/25: used 771.26 L, end pressure 137.52 bar
Cylinder NX50: used 1830.35 L, end pressure 124.55 bar
Cylinder O2: used 1399.26 L, end pressure 136.97 bar
Reserve satisfied: False
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

## (45 m, 25 min) — Trimix 21/35, Nitrox 50, Oxygen

`CreatePlan_ShouldMatchReferenceSchedule_WhenOpenCircuitAt45MetersOnTrimix2135`

### Profile

| Level | Depth | Time at level |
| --- | --- | --- |
| 1 | 45 m | 25 min |

Descent: 2.50 min.

### Gases

| Gas | Size | Fill | Purpose |
| --- | --- | --- | --- |
| Trimix 21/35 | 24 L | 232 bar | Bottom |
| Nitrox 50 | 22.2 L | 207 bar | Deco |
| Oxygen | 22.2 L | 207 bar | Deco |

### Settings

| Setting | Value | Setting | Value |
| --- | --- | --- | --- |
| Gradient factors | **30 / 85** | Reserve pressure | 40 bar |
| Surface pressure | **1013.25 mbar** | Reserve stress factor | 2 |
| Salinity | Salt (1030) | Reserve team size | 2 |
| Descent rate | 18 m/min | Stop increment | 1 min |
| Ascent below 75% | 9 m/min | Problem-solving | 1 min |
| Ascent 75-50% | 9 m/min | Gas switch | 2 min |
| Ascent 50% to stops | 9 m/min | O2 break interval | 20 min |
| Ascent last 6 m | 1 m/min | O2 break duration | 5 min |
| Bottom SAC | 18 L/min | Safety stop | off |
| Deco SAC | 14 L/min | Last stop | 6 m |
| Bottom pO2 | 1.42 bar | Switch at required stop only | off |
| Deco pO2 | 1.62 bar | Oxygen breaks | off |
| MOD model | Realistic | Oxygen is narcotic | yes |

### Bitenovac plan

```
Valid: True, total runtime: 58.83333333

Descent       45 m     2 min     2 min  TX21/35
Bottom        45 m    25 min    28 min  TX21/35
Ascent        21 m     3 min    30 min  TX21/35
GasSwitch     21 m     2 min    32 min  NX50
Ascent        12 m     1 min    33 min  NX50
Stop          12 m     2 min    35 min  NX50
Ascent         9 m     0 min    35 min  NX50
Stop           9 m     3 min    38 min  NX50
Ascent         6 m     0 min    39 min  NX50
GasSwitch      6 m     2 min    41 min  O2
Stop           6 m    12 min    53 min  O2
Ascent         0 m     6 min    59 min  O2

CNS: 58.17 %
OTU: 82.6

Cylinder TX21/35: used 2897.77 L, end pressure 109.66 bar
Cylinder NX50: used 318.43 L, end pressure 192.47 bar
Cylinder O2: used 434.02 L, end pressure 187.19 bar
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

## (60 m, 20 min) — Trimix 18/45, Trimix 35/25, Nitrox 50, Oxygen

`CreatePlan_ShouldMatchReferenceSchedule_WhenOpenCircuitAt60MetersOnTrimix1845`

### Profile

| Level | Depth | Time at level |
| --- | --- | --- |
| 1 | 60 m | 20 min |

Descent: 3.33 min.

### Gases

| Gas | Size | Fill | Purpose |
| --- | --- | --- | --- |
| Trimix 18/45 | 24 L | 232 bar | Bottom |
| Trimix 35/25 | 22.2 L | 207 bar | Deco |
| Nitrox 50 | 22.2 L | 207 bar | Deco |
| Oxygen | 22.2 L | 207 bar | Deco |

### Settings

| Setting | Value | Setting | Value |
| --- | --- | --- | --- |
| Gradient factors | **30 / 80** | Reserve pressure | 40 bar |
| Surface pressure | **1013.25 mbar** | Reserve stress factor | 2 |
| Salinity | Salt (1030) | Reserve team size | 2 |
| Descent rate | 18 m/min | Stop increment | 1 min |
| Ascent below 75% | 9 m/min | Problem-solving | 1 min |
| Ascent 75-50% | 9 m/min | Gas switch | 2 min |
| Ascent 50% to stops | 9 m/min | O2 break interval | 20 min |
| Ascent last 6 m | 1 m/min | O2 break duration | 5 min |
| Bottom SAC | 18 L/min | Safety stop | off |
| Deco SAC | 14 L/min | Last stop | 6 m |
| Bottom pO2 | 1.42 bar | Switch at required stop only | off |
| Deco pO2 | 1.62 bar | Oxygen breaks | off |
| MOD model | Realistic | Oxygen is narcotic | yes |

### Bitenovac plan

```
Valid: True, total runtime: 72.33333333

Descent       60 m     3 min     3 min  TX18/45
Bottom        60 m    20 min    23 min  TX18/45
Ascent        33 m     3 min    26 min  TX18/45
GasSwitch     33 m     2 min    28 min  TX35/25
Ascent        21 m     1 min    30 min  TX35/25
GasSwitch     21 m     2 min    32 min  NX50
Ascent        18 m     0 min    32 min  NX50
Stop          18 m     1 min    33 min  NX50
Ascent        15 m     0 min    33 min  NX50
Stop          15 m     2 min    35 min  NX50
Ascent        12 m     0 min    36 min  NX50
Stop          12 m     4 min    40 min  NX50
Ascent         9 m     0 min    40 min  NX50
Stop           9 m     5 min    45 min  NX50
Ascent         6 m     0 min    45 min  NX50
GasSwitch      6 m     2 min    47 min  O2
Stop           6 m    19 min    66 min  O2
Ascent         0 m     6 min    72 min  O2

CNS: 82.41 %
OTU: 109.06

Cylinder TX18/45: used 3195.68 L, end pressure 97.08 bar
Cylinder TX35/25: used 239.8 L, end pressure 196.05 bar
Cylinder NX50: used 542.03 L, end pressure 182.26 bar
Cylinder O2: used 590.63 L, end pressure 180.04 bar
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

## (75 m, 20 min) — Trimix 15/55, Trimix 35/25, Nitrox 50, Oxygen

`CreatePlan_ShouldMatchReferenceSchedule_WhenOpenCircuitAt75MetersOnTrimix1555`

### Profile

| Level | Depth | Time at level |
| --- | --- | --- |
| 1 | 75 m | 20 min |

Descent: 4.17 min.

### Gases

| Gas | Size | Fill | Purpose |
| --- | --- | --- | --- |
| Trimix 15/55 | 24 L | 232 bar | Bottom |
| Trimix 35/25 | 22.2 L | 207 bar | Deco |
| Nitrox 50 | 22.2 L | 207 bar | Deco |
| Oxygen | 22.2 L | 207 bar | Deco |

### Settings

| Setting | Value | Setting | Value |
| --- | --- | --- | --- |
| Gradient factors | **25 / 80** | Reserve pressure | 40 bar |
| Surface pressure | **1013.25 mbar** | Reserve stress factor | 2 |
| Salinity | Salt (1030) | Reserve team size | 2 |
| Descent rate | 18 m/min | Stop increment | 1 min |
| Ascent below 75% | 9 m/min | Problem-solving | 1 min |
| Ascent 75-50% | 9 m/min | Gas switch | 2 min |
| Ascent 50% to stops | 9 m/min | O2 break interval | 20 min |
| Ascent last 6 m | 1 m/min | O2 break duration | 5 min |
| Bottom SAC | 18 L/min | Safety stop | off |
| Deco SAC | 14 L/min | Last stop | 6 m |
| Bottom pO2 | 1.42 bar | Switch at required stop only | off |
| Deco pO2 | 1.62 bar | Oxygen breaks | off |
| MOD model | Realistic | Oxygen is narcotic | yes |

### Bitenovac plan

```
Valid: True, total runtime: 107.83333333

Descent       75 m     4 min     4 min  TX15/55
Bottom        75 m    20 min    24 min  TX15/55
Ascent        42 m     4 min    28 min  TX15/55
Stop          42 m     1 min    29 min  TX15/55
Ascent        39 m     0 min    29 min  TX15/55
Stop          39 m     1 min    30 min  TX15/55
Ascent        36 m     0 min    30 min  TX15/55
Stop          36 m     1 min    31 min  TX15/55
Ascent        33 m     0 min    32 min  TX15/55
GasSwitch     33 m     2 min    34 min  TX35/25
Ascent        27 m     1 min    34 min  TX35/25
Stop          27 m     2 min    36 min  TX35/25
Ascent        24 m     0 min    37 min  TX35/25
Stop          24 m     2 min    39 min  TX35/25
Ascent        21 m     0 min    39 min  TX35/25
GasSwitch     21 m     2 min    41 min  NX50
Stop          21 m     1 min    42 min  NX50
Ascent        18 m     0 min    42 min  NX50
Stop          18 m     2 min    44 min  NX50
Ascent        15 m     0 min    45 min  NX50
Stop          15 m     5 min    50 min  NX50
Ascent        12 m     0 min    50 min  NX50
Stop          12 m     6 min    56 min  NX50
Ascent         9 m     0 min    56 min  NX50
Stop           9 m    10 min    66 min  NX50
Ascent         6 m     0 min    67 min  NX50
GasSwitch      6 m     2 min    69 min  O2
Stop           6 m    33 min   102 min  O2
Ascent         0 m     6 min   108 min  O2

CNS: 128.12 %
OTU: 160.68

Cylinder TX15/55: used 4368.06 L, end pressure 47.59 bar
Cylinder TX35/25: used 437.79 L, end pressure 187.02 bar
Cylinder NX50: used 923.57 L, end pressure 164.85 bar
Cylinder O2: used 903.87 L, end pressure 165.75 bar
Reserve satisfied: False
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

## (100 m, 15 min) — Trimix 10/70, Trimix 21/35, Trimix 35/25, Nitrox 50, Oxygen

`CreatePlan_ShouldMatchReferenceSchedule_WhenOpenCircuitAt100MetersOnTrimix1070`

### Profile

| Level | Depth | Time at level |
| --- | --- | --- |
| 1 | 100 m | 15 min |

Descent: 5.56 min.

### Gases

| Gas | Size | Fill | Purpose |
| --- | --- | --- | --- |
| Trimix 10/70 | 24 L | 232 bar | Bottom |
| Trimix 21/35 | 22.2 L | 207 bar | Deco |
| Trimix 35/25 | 22.2 L | 207 bar | Deco |
| Nitrox 50 | 22.2 L | 207 bar | Deco |
| Oxygen | 22.2 L | 207 bar | Deco |

### Settings

| Setting | Value | Setting | Value |
| --- | --- | --- | --- |
| Gradient factors | **20 / 80** | Reserve pressure | 40 bar |
| Surface pressure | **1013.25 mbar** | Reserve stress factor | 2 |
| Salinity | Salt (1030) | Reserve team size | 2 |
| Descent rate | 18 m/min | Stop increment | 1 min |
| Ascent below 75% | 9 m/min | Problem-solving | 1 min |
| Ascent 75-50% | 9 m/min | Gas switch | 2 min |
| Ascent 50% to stops | 9 m/min | O2 break interval | 20 min |
| Ascent last 6 m | 1 m/min | O2 break duration | 5 min |
| Bottom SAC | 18 L/min | Safety stop | off |
| Deco SAC | 14 L/min | Last stop | 6 m |
| Bottom pO2 | 1.42 bar | Switch at required stop only | off |
| Deco pO2 | 1.62 bar | Oxygen breaks | off |
| MOD model | Realistic | Oxygen is narcotic | yes |

### Bitenovac plan

```
Valid: True, total runtime: 143.999999995

Descent      100 m     6 min     6 min  TX10/70
Bottom       100 m    15 min    21 min  TX10/70
Ascent        66 m     4 min    24 min  TX10/70
GasSwitch     66 m     2 min    26 min  TX21/35
Ascent        48 m     2 min    28 min  TX21/35
Stop          48 m     1 min    29 min  TX21/35
Ascent        42 m     1 min    30 min  TX21/35
Stop          42 m     1 min    31 min  TX21/35
Ascent        39 m     0 min    31 min  TX21/35
Stop          39 m     2 min    33 min  TX21/35
Ascent        36 m     0 min    34 min  TX21/35
Stop          36 m     1 min    35 min  TX21/35
Ascent        33 m     0 min    35 min  TX21/35
GasSwitch     33 m     2 min    37 min  TX35/25
Ascent        30 m     0 min    37 min  TX35/25
Stop          30 m     2 min    39 min  TX35/25
Ascent        27 m     0 min    40 min  TX35/25
Stop          27 m     3 min    43 min  TX35/25
Ascent        24 m     0 min    43 min  TX35/25
Stop          24 m     3 min    46 min  TX35/25
Ascent        21 m     0 min    46 min  TX35/25
GasSwitch     21 m     2 min    48 min  NX50
Stop          21 m     2 min    50 min  NX50
Ascent        18 m     0 min    51 min  NX50
Stop          18 m     4 min    55 min  NX50
Ascent        15 m     0 min    55 min  NX50
Stop          15 m     7 min    62 min  NX50
Ascent        12 m     0 min    62 min  NX50
Stop          12 m     9 min    71 min  NX50
Ascent         9 m     0 min    72 min  NX50
Stop           9 m    14 min    86 min  NX50
Ascent         6 m     0 min    86 min  NX50
GasSwitch      6 m     2 min    88 min  O2
Stop           6 m    50 min   138 min  O2
Ascent         0 m     6 min   144 min  O2

CNS: 183.59 %
OTU: 215.76

Cylinder TX10/70: used 4609.13 L, end pressure 37.41 bar
Cylinder TX21/35: used 1010.23 L, end pressure 160.89 bar
Cylinder TX35/25: used 648.71 L, end pressure 177.39 bar
Cylinder NX50: used 1313.47 L, end pressure 147.05 bar
Cylinder O2: used 1284.22 L, end pressure 148.39 bar
Reserve satisfied: False
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
