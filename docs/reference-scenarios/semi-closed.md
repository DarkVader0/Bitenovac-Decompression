# Passive semi-closed reference scenarios

Each section is one test: full setup, the schedule this implementation produces, then an empty
block for the reference tool. Every setting is listed in full, so nothing has to be looked up
elsewhere. **Bold** marks a value that differs from the shared default.

**Level durations are time *at* the level.** Descent and inter-level travel are generated on top of
them. Subsurface's planner rows work the other way round — a segment's duration includes the
transit into it — so add the stated descent time to the first level, or switch the planner to
runtime entry. Getting this wrong shifts every stop.

**The loop gas is computed, not chosen.** The loop vents one part in ten of every breath, and the
oxygen the diver consumes is not replaced, so the loop runs leaner than its supply gas by a drop
in oxygen partial pressure that is constant in pressure rather than in fraction:

```
drop = metabolicOxygenConsumption × surfacePressure / (dumpRatio × respiratoryMinuteVolume)
```

Because the drop is constant in pressure, the loop runs proportionally leaner near the surface and
can become hypoxic on the shallow stops with a lean supply gas. **The drop is fixed for the whole
dive at the bottom breathing rate** — it does not fall when the diver switches to the deco SAC.
Configure the reference tool the same way, or it will model a richer loop on the stops than this
one does. A tool that scales the drop with the current breathing rate cannot reproduce these
scenarios exactly.

Water densities: fresh 1000, brackish 1015, EN 13319 1020, salt 1030 kg/m³.

---

## (60 m, 30 min) — Trimix 16/50 → Nitrox 50

`CreatePlan_ShouldMatchReferenceSchedule_WhenSemiClosedAt60MetersOnTrimix1650SwitchingToNitrox50`

Both trimix 16/50 and nitrox 50 are loop supplies. Oxygen is carried for bailout only and must not
be offered as a decompression gas.

### Profile

| Level | Depth | Time at level | Apparatus |
| --- | --- | --- | --- |
| 1 | 60 m | 30 min | pSCR 1:10 |

Descent: 3.33 min.

### Gases

| Gas | Size | Fill | Purpose |
| --- | --- | --- | --- |
| Trimix 16/50 | 24 L | 232 bar | Loop supply |
| Nitrox 50 | 11.1 L | 200 bar | Loop supply |
| Oxygen | 7 L | 200 bar | Bailout, never breathed |

### Settings

| Setting | Value | Setting | Value |
| --- | --- | --- | --- |
| Gradient factors | **35 / 75** | Reserve pressure | 40 bar |
| Dump ratio | **0.1 (1:10)** | Reserve stress factor | 2 |
| Oxygen drop | **555.5556 mbar** | Reserve team size | 2 |
| Surface pressure | 1000 mbar | Stop increment | 1 min |
| Salinity | Salt (1030) | Problem-solving | 1 min |
| Descent rate | 18 m/min | Gas switch | 2 min |
| Ascent below 75% | 9 m/min | O2 break interval | 20 min |
| Ascent 75-50% | 9 m/min | O2 break duration | 5 min |
| Ascent 50% to stops | 9 m/min | Safety stop | off |
| Ascent last 6 m | 1 m/min | Last stop | **3 m** |
| Bottom SAC | 18 L/min | Switch at required stop only | off |
| Deco SAC | **13 L/min** | Oxygen breaks | off |
| Bottom metabolic O2 | 1 L/min | Oxygen is narcotic | yes |
| Deco metabolic O2 | 0.6 L/min | MOD model | Realistic |
| Loop volume | 6 L | Bottom pO2 | 1.42 bar |
| Deco pO2 | 1.62 bar |  |  |

### Bitenovac plan

```
Valid: True, total runtime: 220.33333333

Descent       60 m     3 min     3 min  TX16/50 (PSCR 1:10)
Bottom        60 m    30 min    33 min  TX16/50 (PSCR 1:10)
Ascent        36 m     3 min    36 min  TX16/50 (PSCR 1:10)
Stop          36 m     3 min    39 min  TX16/50 (PSCR 1:10)
Ascent        33 m     0 min    39 min  TX16/50 (PSCR 1:10)
Stop          33 m     2 min    41 min  TX16/50 (PSCR 1:10)
Ascent        30 m     0 min    42 min  TX16/50 (PSCR 1:10)
Stop          30 m     4 min    46 min  TX16/50 (PSCR 1:10)
Ascent        27 m     0 min    46 min  TX16/50 (PSCR 1:10)
Stop          27 m     6 min    52 min  TX16/50 (PSCR 1:10)
Ascent        24 m     0 min    52 min  TX16/50 (PSCR 1:10)
Stop          24 m    10 min    62 min  TX16/50 (PSCR 1:10)
Ascent        21 m     0 min    63 min  TX16/50 (PSCR 1:10)
GasSwitch     21 m     2 min    65 min  NX50 (PSCR 1:10)
Stop          21 m     2 min    67 min  NX50 (PSCR 1:10)
Ascent        18 m     0 min    67 min  NX50 (PSCR 1:10)
Stop          18 m     5 min    72 min  NX50 (PSCR 1:10)
Ascent        15 m     0 min    72 min  NX50 (PSCR 1:10)
Stop          15 m     7 min    79 min  NX50 (PSCR 1:10)
Ascent        12 m     0 min    80 min  NX50 (PSCR 1:10)
Stop          12 m    10 min    90 min  NX50 (PSCR 1:10)
Ascent         9 m     0 min    90 min  NX50 (PSCR 1:10)
Stop           9 m    18 min   108 min  NX50 (PSCR 1:10)
Ascent         6 m     0 min   108 min  NX50 (PSCR 1:10)
Stop           6 m    32 min   140 min  NX50 (PSCR 1:10)
Ascent         3 m     3 min   143 min  NX50 (PSCR 1:10)
Stop           3 m    74 min   217 min  NX50 (PSCR 1:10)
Ascent         0 m     3 min   220 min  NX50 (PSCR 1:10)

CNS: 21.53 %
OTU: 49.58

Cylinder TX16/50: used 619.95 L, end pressure 206.17 bar
Cylinder NX50: used 345.22 L, end pressure 168.9 bar
Cylinder O2: used 0 L, end pressure 200 bar
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

The four dives below are also planned on open circuit and on the closed-circuit loop, so the only
difference between the three schedules is the breathing apparatus. Keep the settings identical
across all three documents.

Unlike the other two apparatus, the bottom mix **and the whole ladder down to nitrox 50** are loop
supplies, so the loop is fed from the richest gas the depth allows. Oxygen is carried for bailout
only and is never breathed.

The oxygen drop is **562.9167 mbar** here, not 555.5556 — it scales with surface pressure, which is
1013.25 mbar for these four.

---

## (45 m, 25 min) — Trimix 21/35 → Nitrox 50

`CreatePlan_ShouldMatchReferenceSchedule_WhenSemiClosedAt45MetersOnTrimix2135`

### Profile

| Level | Depth | Time at level | Apparatus |
| --- | --- | --- | --- |
| 1 | 45 m | 25 min | pSCR 1:10 |

Descent: 2.50 min.

### Gases

| Gas | Size | Fill | Purpose |
| --- | --- | --- | --- |
| Trimix 21/35 | 24 L | 232 bar | Loop supply |
| Nitrox 50 | 22.2 L | 207 bar | Loop supply |
| Oxygen | 22.2 L | 207 bar | Bailout, never breathed |

### Settings

| Setting | Value | Setting | Value |
| --- | --- | --- | --- |
| Gradient factors | **30 / 85** | Reserve pressure | 40 bar |
| Dump ratio | **0.1 (1:10)** | Reserve stress factor | 2 |
| Oxygen drop | **562.9167 mbar** | Reserve team size | 2 |
| Surface pressure | **1013.25 mbar** | Stop increment | 1 min |
| Salinity | Salt (1030) | Problem-solving | 1 min |
| Descent rate | 18 m/min | Gas switch | 2 min |
| Ascent below 75% | 9 m/min | O2 break interval | 20 min |
| Ascent 75-50% | 9 m/min | O2 break duration | 5 min |
| Ascent 50% to stops | 9 m/min | Safety stop | off |
| Ascent last 6 m | 1 m/min | Last stop | 6 m |
| Bottom SAC | 18 L/min | Switch at required stop only | off |
| Deco SAC | 14 L/min | Oxygen breaks | off |
| Bottom metabolic O2 | 1 L/min | Oxygen is narcotic | yes |
| Deco metabolic O2 | 0.6 L/min | MOD model | Realistic |
| Loop volume | 6 L | Bottom pO2 | 1.42 bar |
| Deco pO2 | 1.62 bar |  |  |

### Bitenovac plan

```
Valid: True, total runtime: 82.83333333

Descent       45 m     2 min     2 min  TX21/35 (PSCR 1:10)
Bottom        45 m    25 min    28 min  TX21/35 (PSCR 1:10)
Ascent        24 m     2 min    30 min  TX21/35 (PSCR 1:10)
Stop          24 m     1 min    31 min  TX21/35 (PSCR 1:10)
Ascent        21 m     0 min    31 min  TX21/35 (PSCR 1:10)
GasSwitch     21 m     2 min    33 min  NX50 (PSCR 1:10)
Ascent        15 m     1 min    34 min  NX50 (PSCR 1:10)
Stop          15 m     2 min    36 min  NX50 (PSCR 1:10)
Ascent        12 m     0 min    36 min  NX50 (PSCR 1:10)
Stop          12 m     3 min    39 min  NX50 (PSCR 1:10)
Ascent         9 m     0 min    39 min  NX50 (PSCR 1:10)
Stop           9 m     5 min    44 min  NX50 (PSCR 1:10)
Ascent         6 m     0 min    45 min  NX50 (PSCR 1:10)
Stop           6 m    32 min    77 min  NX50 (PSCR 1:10)
Ascent         0 m     6 min    83 min  NX50 (PSCR 1:10)

CNS: 12.81 %
OTU: 26.78

Cylinder TX21/35: used 321.75 L, end pressure 218.42 bar
Cylinder NX50: used 129.61 L, end pressure 201.08 bar
Cylinder O2: used 0 L, end pressure 207 bar
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

## (60 m, 20 min) — Trimix 18/45 → Trimix 35/25 → Nitrox 50

`CreatePlan_ShouldMatchReferenceSchedule_WhenSemiClosedAt60MetersOnTrimix1845`

### Profile

| Level | Depth | Time at level | Apparatus |
| --- | --- | --- | --- |
| 1 | 60 m | 20 min | pSCR 1:10 |

Descent: 3.33 min.

### Gases

| Gas | Size | Fill | Purpose |
| --- | --- | --- | --- |
| Trimix 18/45 | 24 L | 232 bar | Loop supply |
| Trimix 35/25 | 22.2 L | 207 bar | Loop supply |
| Nitrox 50 | 22.2 L | 207 bar | Loop supply |
| Oxygen | 22.2 L | 207 bar | Bailout, never breathed |

### Settings

| Setting | Value | Setting | Value |
| --- | --- | --- | --- |
| Gradient factors | **30 / 80** | Reserve pressure | 40 bar |
| Dump ratio | **0.1 (1:10)** | Reserve stress factor | 2 |
| Oxygen drop | **562.9167 mbar** | Reserve team size | 2 |
| Surface pressure | **1013.25 mbar** | Stop increment | 1 min |
| Salinity | Salt (1030) | Problem-solving | 1 min |
| Descent rate | 18 m/min | Gas switch | 2 min |
| Ascent below 75% | 9 m/min | O2 break interval | 20 min |
| Ascent 75-50% | 9 m/min | O2 break duration | 5 min |
| Ascent 50% to stops | 9 m/min | Safety stop | off |
| Ascent last 6 m | 1 m/min | Last stop | 6 m |
| Bottom SAC | 18 L/min | Switch at required stop only | off |
| Deco SAC | 14 L/min | Oxygen breaks | off |
| Bottom metabolic O2 | 1 L/min | Oxygen is narcotic | yes |
| Deco metabolic O2 | 0.6 L/min | MOD model | Realistic |
| Loop volume | 6 L | Bottom pO2 | 1.42 bar |
| Deco pO2 | 1.62 bar |  |  |

### Bitenovac plan

```
Valid: True, total runtime: 106.33333333

Descent       60 m     3 min     3 min  TX18/45 (PSCR 1:10)
Bottom        60 m    20 min    23 min  TX18/45 (PSCR 1:10)
Ascent        33 m     3 min    26 min  TX18/45 (PSCR 1:10)
GasSwitch     33 m     2 min    28 min  TX35/25 (PSCR 1:10)
Ascent        27 m     1 min    29 min  TX35/25 (PSCR 1:10)
Stop          27 m     1 min    30 min  TX35/25 (PSCR 1:10)
Ascent        24 m     0 min    30 min  TX35/25 (PSCR 1:10)
Stop          24 m     1 min    31 min  TX35/25 (PSCR 1:10)
Ascent        21 m     0 min    32 min  TX35/25 (PSCR 1:10)
GasSwitch     21 m     2 min    34 min  NX50 (PSCR 1:10)
Ascent        18 m     0 min    34 min  NX50 (PSCR 1:10)
Stop          18 m     2 min    36 min  NX50 (PSCR 1:10)
Ascent        15 m     0 min    36 min  NX50 (PSCR 1:10)
Stop          15 m     3 min    39 min  NX50 (PSCR 1:10)
Ascent        12 m     0 min    40 min  NX50 (PSCR 1:10)
Stop          12 m     4 min    44 min  NX50 (PSCR 1:10)
Ascent         9 m     0 min    44 min  NX50 (PSCR 1:10)
Stop           9 m     8 min    52 min  NX50 (PSCR 1:10)
Ascent         6 m     0 min    52 min  NX50 (PSCR 1:10)
Stop           6 m    48 min   100 min  NX50 (PSCR 1:10)
Ascent         0 m     6 min   106 min  NX50 (PSCR 1:10)

CNS: 18.49 %
OTU: 40.34

Cylinder TX18/45: used 355.8 L, end pressure 216.98 bar
Cylinder TX35/25: used 33.9 L, end pressure 205.45 bar
Cylinder NX50: used 187.77 L, end pressure 198.43 bar
Cylinder O2: used 0 L, end pressure 207 bar
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

## (75 m, 20 min) — Trimix 15/55 → Trimix 35/25 → Nitrox 50

`CreatePlan_ShouldMatchReferenceSchedule_WhenSemiClosedAt75MetersOnTrimix1555`

### Profile

| Level | Depth | Time at level | Apparatus |
| --- | --- | --- | --- |
| 1 | 75 m | 20 min | pSCR 1:10 |

Descent: 4.17 min.

### Gases

| Gas | Size | Fill | Purpose |
| --- | --- | --- | --- |
| Trimix 15/55 | 24 L | 232 bar | Loop supply |
| Trimix 35/25 | 22.2 L | 207 bar | Loop supply |
| Nitrox 50 | 22.2 L | 207 bar | Loop supply |
| Oxygen | 22.2 L | 207 bar | Bailout, never breathed |

### Settings

| Setting | Value | Setting | Value |
| --- | --- | --- | --- |
| Gradient factors | **25 / 80** | Reserve pressure | 40 bar |
| Dump ratio | **0.1 (1:10)** | Reserve stress factor | 2 |
| Oxygen drop | **562.9167 mbar** | Reserve team size | 2 |
| Surface pressure | **1013.25 mbar** | Stop increment | 1 min |
| Salinity | Salt (1030) | Problem-solving | 1 min |
| Descent rate | 18 m/min | Gas switch | 2 min |
| Ascent below 75% | 9 m/min | O2 break interval | 20 min |
| Ascent 75-50% | 9 m/min | O2 break duration | 5 min |
| Ascent 50% to stops | 9 m/min | Safety stop | off |
| Ascent last 6 m | 1 m/min | Last stop | 6 m |
| Bottom SAC | 18 L/min | Switch at required stop only | off |
| Deco SAC | 14 L/min | Oxygen breaks | off |
| Bottom metabolic O2 | 1 L/min | Oxygen is narcotic | yes |
| Deco metabolic O2 | 0.6 L/min | MOD model | Realistic |
| Loop volume | 6 L | Bottom pO2 | 1.42 bar |
| Deco pO2 | 1.62 bar |  |  |

### Bitenovac plan

```
Valid: True, total runtime: 188.83333333

Descent       75 m     4 min     4 min  TX15/55 (PSCR 1:10)
Bottom        75 m    20 min    24 min  TX15/55 (PSCR 1:10)
Ascent        48 m     3 min    27 min  TX15/55 (PSCR 1:10)
Stop          48 m     1 min    28 min  TX15/55 (PSCR 1:10)
Ascent        45 m     0 min    28 min  TX15/55 (PSCR 1:10)
Stop          45 m     1 min    29 min  TX15/55 (PSCR 1:10)
Ascent        42 m     0 min    30 min  TX15/55 (PSCR 1:10)
Stop          42 m     1 min    31 min  TX15/55 (PSCR 1:10)
Ascent        39 m     0 min    31 min  TX15/55 (PSCR 1:10)
Stop          39 m     3 min    34 min  TX15/55 (PSCR 1:10)
Ascent        36 m     0 min    34 min  TX15/55 (PSCR 1:10)
Stop          36 m     2 min    36 min  TX15/55 (PSCR 1:10)
Ascent        33 m     0 min    37 min  TX15/55 (PSCR 1:10)
GasSwitch     33 m     2 min    39 min  TX35/25 (PSCR 1:10)
Ascent        30 m     0 min    39 min  TX35/25 (PSCR 1:10)
Stop          30 m     2 min    41 min  TX35/25 (PSCR 1:10)
Ascent        27 m     0 min    41 min  TX35/25 (PSCR 1:10)
Stop          27 m     2 min    43 min  TX35/25 (PSCR 1:10)
Ascent        24 m     0 min    44 min  TX35/25 (PSCR 1:10)
Stop          24 m     4 min    48 min  TX35/25 (PSCR 1:10)
Ascent        21 m     0 min    48 min  TX35/25 (PSCR 1:10)
GasSwitch     21 m     2 min    50 min  NX50 (PSCR 1:10)
Stop          21 m     1 min    51 min  NX50 (PSCR 1:10)
Ascent        18 m     0 min    51 min  NX50 (PSCR 1:10)
Stop          18 m     4 min    55 min  NX50 (PSCR 1:10)
Ascent        15 m     0 min    56 min  NX50 (PSCR 1:10)
Stop          15 m     6 min    62 min  NX50 (PSCR 1:10)
Ascent        12 m     0 min    62 min  NX50 (PSCR 1:10)
Stop          12 m     9 min    71 min  NX50 (PSCR 1:10)
Ascent         9 m     0 min    71 min  NX50 (PSCR 1:10)
Stop           9 m    15 min    86 min  NX50 (PSCR 1:10)
Ascent         6 m     0 min    87 min  NX50 (PSCR 1:10)
Stop           6 m    96 min   183 min  NX50 (PSCR 1:10)
Ascent         0 m     6 min   189 min  NX50 (PSCR 1:10)

CNS: 31 %
OTU: 63.48

Cylinder TX15/55: used 517.58 L, end pressure 210.15 bar
Cylinder TX35/25: used 64.45 L, end pressure 204.06 bar
Cylinder NX50: used 351.77 L, end pressure 190.94 bar
Cylinder O2: used 0 L, end pressure 207 bar
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

## (100 m, 15 min) — Trimix 10/70 → Trimix 21/35 → Trimix 35/25 → Nitrox 50

`CreatePlan_ShouldMatchReferenceSchedule_WhenSemiClosedAt100MetersOnTrimix1070`

### Profile

| Level | Depth | Time at level | Apparatus |
| --- | --- | --- | --- |
| 1 | 100 m | 15 min | pSCR 1:10 |

Descent: 5.56 min.

### Gases

| Gas | Size | Fill | Purpose |
| --- | --- | --- | --- |
| Trimix 10/70 | 24 L | 232 bar | Loop supply |
| Trimix 21/35 | 22.2 L | 207 bar | Loop supply |
| Trimix 35/25 | 22.2 L | 207 bar | Loop supply |
| Nitrox 50 | 22.2 L | 207 bar | Loop supply |
| Oxygen | 22.2 L | 207 bar | Bailout, never breathed |

### Settings

| Setting | Value | Setting | Value |
| --- | --- | --- | --- |
| Gradient factors | **20 / 80** | Reserve pressure | 40 bar |
| Dump ratio | **0.1 (1:10)** | Reserve stress factor | 2 |
| Oxygen drop | **562.9167 mbar** | Reserve team size | 2 |
| Surface pressure | **1013.25 mbar** | Stop increment | 1 min |
| Salinity | Salt (1030) | Problem-solving | 1 min |
| Descent rate | 18 m/min | Gas switch | 2 min |
| Ascent below 75% | 9 m/min | O2 break interval | 20 min |
| Ascent 75-50% | 9 m/min | O2 break duration | 5 min |
| Ascent 50% to stops | 9 m/min | Safety stop | off |
| Ascent last 6 m | 1 m/min | Last stop | 6 m |
| Bottom SAC | 18 L/min | Switch at required stop only | off |
| Deco SAC | 14 L/min | Oxygen breaks | off |
| Bottom metabolic O2 | 1 L/min | Oxygen is narcotic | yes |
| Deco metabolic O2 | 0.6 L/min | MOD model | Realistic |
| Loop volume | 6 L | Bottom pO2 | 1.42 bar |
| Deco pO2 | 1.62 bar |  |  |

### Bitenovac plan

```
Valid: True, total runtime: 248.999999995

Descent      100 m     6 min     6 min  TX10/70 (PSCR 1:10)
Bottom       100 m    15 min    21 min  TX10/70 (PSCR 1:10)
Ascent        69 m     3 min    24 min  TX10/70 (PSCR 1:10)
Stop          69 m     1 min    25 min  TX10/70 (PSCR 1:10)
Ascent        66 m     0 min    25 min  TX10/70 (PSCR 1:10)
GasSwitch     66 m     2 min    27 min  TX21/35 (PSCR 1:10)
Ascent        51 m     2 min    29 min  TX21/35 (PSCR 1:10)
Stop          51 m     1 min    30 min  TX21/35 (PSCR 1:10)
Ascent        48 m     0 min    30 min  TX21/35 (PSCR 1:10)
Stop          48 m     1 min    31 min  TX21/35 (PSCR 1:10)
Ascent        45 m     0 min    32 min  TX21/35 (PSCR 1:10)
Stop          45 m     1 min    33 min  TX21/35 (PSCR 1:10)
Ascent        42 m     0 min    33 min  TX21/35 (PSCR 1:10)
Stop          42 m     1 min    34 min  TX21/35 (PSCR 1:10)
Ascent        39 m     0 min    34 min  TX21/35 (PSCR 1:10)
Stop          39 m     2 min    36 min  TX21/35 (PSCR 1:10)
Ascent        36 m     0 min    37 min  TX21/35 (PSCR 1:10)
Stop          36 m     2 min    39 min  TX21/35 (PSCR 1:10)
Ascent        33 m     0 min    39 min  TX21/35 (PSCR 1:10)
GasSwitch     33 m     2 min    41 min  TX35/25 (PSCR 1:10)
Stop          33 m     1 min    42 min  TX35/25 (PSCR 1:10)
Ascent        30 m     0 min    42 min  TX35/25 (PSCR 1:10)
Stop          30 m     3 min    45 min  TX35/25 (PSCR 1:10)
Ascent        27 m     0 min    46 min  TX35/25 (PSCR 1:10)
Stop          27 m     3 min    49 min  TX35/25 (PSCR 1:10)
Ascent        24 m     0 min    49 min  TX35/25 (PSCR 1:10)
Stop          24 m     6 min    55 min  TX35/25 (PSCR 1:10)
Ascent        21 m     0 min    55 min  TX35/25 (PSCR 1:10)
GasSwitch     21 m     2 min    57 min  NX50 (PSCR 1:10)
Stop          21 m     2 min    59 min  NX50 (PSCR 1:10)
Ascent        18 m     0 min    60 min  NX50 (PSCR 1:10)
Stop          18 m     6 min    66 min  NX50 (PSCR 1:10)
Ascent        15 m     0 min    66 min  NX50 (PSCR 1:10)
Stop          15 m     9 min    75 min  NX50 (PSCR 1:10)
Ascent        12 m     0 min    75 min  NX50 (PSCR 1:10)
Stop          12 m    12 min    87 min  NX50 (PSCR 1:10)
Ascent         9 m     0 min    88 min  NX50 (PSCR 1:10)
Stop           9 m    20 min   108 min  NX50 (PSCR 1:10)
Ascent         6 m     0 min   108 min  NX50 (PSCR 1:10)
Stop           6 m   135 min   243 min  NX50 (PSCR 1:10)
Ascent         0 m     6 min   249 min  NX50 (PSCR 1:10)

CNS: 41.88 %
OTU: 80.31

Cylinder TX10/70: used 533.25 L, end pressure 209.49 bar
Cylinder TX21/35: used 123.65 L, end pressure 201.36 bar
Cylinder TX35/25: used 90.71 L, end pressure 202.86 bar
Cylinder NX50: used 484.16 L, end pressure 184.9 bar
Cylinder O2: used 0 L, end pressure 207 bar
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
