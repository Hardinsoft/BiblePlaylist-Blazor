# Manual review: verses whose start/end could not be determined

These verses' timings were **estimated** (not aligned to the audio) because the fixed audio timing processor (#56) could not determine them. The processor's LCS alignment misaligns repeated phrases in genealogies and other repetitive passages, producing non-monotonic timings (a verse ending before it starts, or starting before the previous verse ends). Rather than ship broken timings, these verses were given estimated timings (previous verse end + word count x 0.4s) so playback stays monotonic — but the estimates are guesses and need a human to verify or correct them.

**How to review:** Open the chapter in the Parse page (`/parse`), find the verse, and use the Start/Stop capture buttons (or the Fine tune fields) to set the correct AudioStart/AudioEnd. Save, and the corrected timings will be picked up by the next timings PR for that book.

Total: **238 verses** across 24 chapters (Genesis 74 verses, Exodus 164 verses).

## Genesis

### Genesis 5 — 1 verse(s)

| Verse | Estimated timing | Reason | Original (broken) timing |
|---|---|---|---|
| 32 | 347.80 – 353.80 | end<start | 347.8 – 332.18 |

### Genesis 10 — 2 verse(s)

| Verse | Estimated timing | Reason | Original (broken) timing |
|---|---|---|---|
| 31 | 296.24 – 305.04 | end<start | 296.24 – 279.14 |
| 32 | 305.04 – 316.24 | start<prev_end | 279.14 – 295.08 |

### Genesis 16 — 4 verse(s)

| Verse | Estimated timing | Reason | Original (broken) timing |
|---|---|---|---|
| 13 | 199.20 – 210.00 | end<start | 199.2 – 173.64 |
| 14 | 210.00 – 216.80 | start<prev_end | 174.88 – 183.28 |
| 15 | 216.80 – 221.20 | start<prev_end | 184.92 – 191.08 |
| 16 | 221.20 – 226.00 | start<prev_end | 191.64 – 197.58 |

### Genesis 26 — 7 verse(s)

| Verse | Estimated timing | Reason | Original (broken) timing |
|---|---|---|---|
| 10 | 205.24 – 217.24 | end<start | 205.24 – 174.32 |
| 11 | 217.24 – 224.84 | start<prev_end | 174.32 – 189.4 |
| 12 | 224.84 – 234.44 | start<prev_end | 190.78 – 204.4 |
| 13 | 234.44 – 240.04 | start<prev_end | 205.54 – 215.1 |
| 14 | 240.04 – 248.44 | start<prev_end | 217.02 – 228.9 |
| 15 | 248.44 – 258.44 | start<prev_end | 230.06 – 243.1 |
| 16 | 258.44 – 258.56 | start<prev_end | 245.10000000000002 – 258.56 |

### Genesis 27 — 2 verse(s)

| Verse | Estimated timing | Reason | Original (broken) timing |
|---|---|---|---|
| 8 | 107.80 – 111.80 | end<start | 107.8 – 102.28 |
| 9 | 111.80 – 114.38 | start<prev_end | 102.28 – 114.38 |

### Genesis 28 — 5 verse(s)

| Verse | Estimated timing | Reason | Original (broken) timing |
|---|---|---|---|
| 14 | 262.60 – 278.20 | end<start | 262.6 – 240.3 |
| 15 | 278.20 – 291.00 | start<prev_end | 241.94 – 261.06 |
| 16 | 291.00 – 298.60 | start<prev_end | 262.86 – 274.7 |
| 17 | 298.60 – 309.00 | start<prev_end | 275.13999999999993 – 291.32 |
| 18 | 309.00 – 309.48 | start<prev_end | 294.2 – 309.48 |

### Genesis 32 — 13 verse(s)

| Verse | Estimated timing | Reason | Original (broken) timing |
|---|---|---|---|
| 19 | 271.94 – 285.54 | end<start | 271.94 – 245.68 |
| 20 | 285.54 – 300.34 | start<prev_end | 245.94 – 264.16 |
| 21 | 300.34 – 307.14 | start<prev_end | 266.52 – 272.28 |
| 22 | 307.14 – 316.74 | start<prev_end | 272.84 – 283.78 |
| 23 | 316.74 – 322.34 | start<prev_end | 285.54 – 291.72 |
| 24 | 322.34 – 327.54 | start<prev_end | 293.1 – 301.02 |
| 25 | 327.54 – 339.54 | start<prev_end | 301.98 – 315.74 |
| 26 | 339.54 – 349.14 | start<prev_end | 317.0 – 326.88 |
| 27 | 349.14 – 353.54 | start<prev_end | 327.3 – 332.56 |
| 28 | 353.54 – 363.54 | start<prev_end | 333.84 – 345.58 |
| 29 | 363.54 – 372.34 | start<prev_end | 346.6 – 358.08 |
| 30 | 372.34 – 379.54 | start<prev_end | 360.68 – 371.36 |
| 31 | 379.54 – 381.06 | start<prev_end | 373.22 – 381.06 |

### Genesis 36 — 12 verse(s)

| Verse | Estimated timing | Reason | Original (broken) timing |
|---|---|---|---|
| 32 | 515.72 – 521.72 | end<start | 515.72 – 424.06 |
| 33 | 521.72 – 527.32 | start<prev_end | 426.12 – 434.78 |
| 34 | 527.32 – 532.92 | start<prev_end | 437.02 – 444.0 |
| 35 | 532.92 – 544.12 | start<prev_end | 445.5 – 500.42 |
| 36 | 544.12 – 548.12 | start<prev_end | 500.42 – 504.42 |
| 37 | 548.12 – 553.32 | start<prev_end | 504.42 – 509.62 |
| 38 | 553.32 – 558.52 | start<prev_end | 509.62 – 514.82 |
| 39 | 558.52 – 571.72 | start<prev_end | 514.82 – 528.42 |
| 40 | 571.72 – 582.12 | start<prev_end | 528.42 – 529.58 |
| 41 | 582.12 – 584.52 | start<prev_end | 530.6 – 535.12 |
| 42 | 584.52 – 586.92 | start<prev_end | 536.26 – 540.66 |
| 43 | 586.92 – 597.72 | start<prev_end | 541.58 – 559.28 |

### Genesis 41 — 6 verse(s)

| Verse | Estimated timing | Reason | Original (broken) timing |
|---|---|---|---|
| 37 | 502.46 – 506.46 | end<start | 502.46 – 476.62 |
| 38 | 506.46 – 514.86 | start<prev_end | 477.68 – 489.2 |
| 39 | 514.86 – 524.86 | start<prev_end | 490.14 – 504.42 |
| 40 | 524.86 – 534.06 | start<prev_end | 505.12 – 518.22 |
| 41 | 534.06 – 540.86 | start<prev_end | 520.0 – 528.4 |
| 42 | 540.86 – 544.32 | start<prev_end | 529.7 – 544.32 |

### Genesis 44 — 3 verse(s)

| Verse | Estimated timing | Reason | Original (broken) timing |
|---|---|---|---|
| 32 | 457.98 – 471.18 | end<start | 457.98 – 413.56 |
| 33 | 471.18 – 481.58 | start<prev_end | 413.56 – 427.3 |
| 34 | 481.58 – 491.18 | start<prev_end | 427.3 – 439.58 |

### Genesis 45 — 9 verse(s)

| Verse | Estimated timing | Reason | Original (broken) timing |
|---|---|---|---|
| 19 | 319.46 – 331.46 | end<start | 319.46 – 291.32 |
| 20 | 331.46 – 338.26 | start<prev_end | 292.48 – 300.62 |
| 21 | 338.26 – 348.26 | start<prev_end | 301.87999999999994 – 314.58 |
| 22 | 348.26 – 357.86 | start<prev_end | 316.12 – 328.16 |
| 23 | 357.86 – 369.86 | start<prev_end | 329.4 – 346.26 |
| 24 | 369.86 – 379.06 | start<prev_end | 347.4 – 358.6 |
| 25 | 379.06 – 385.86 | start<prev_end | 360.68 – 367.78 |
| 26 | 385.86 – 396.26 | start<prev_end | 369.84 – 385.48 |
| 27 | 396.26 – 402.56 | start<prev_end | 386.28 – 402.56 |

### Genesis 47 — 10 verse(s)

| Verse | Estimated timing | Reason | Original (broken) timing |
|---|---|---|---|
| 10 | 208.38 – 212.38 | end<start | 208.38 – 148.96 |
| 11 | 212.38 – 225.58 | start<prev_end | 151.4 – 168.4 |
| 12 | 225.58 – 234.38 | start<prev_end | 170.06 – 180.26 |
| 13 | 234.38 – 246.38 | start<prev_end | 181.28 – 196.94 |
| 14 | 246.38 – 260.78 | start<prev_end | 198.92 – 214.97 |
| 15 | 260.78 – 275.98 | start<prev_end | 216.91 – 236.31 |
| 16 | 275.98 – 284.78 | start<prev_end | 237.63 – 249.55 |
| 17 | 284.78 – 301.98 | start<prev_end | 250.45 – 272.01 |
| 18 | 301.98 – 321.58 | start<prev_end | 272.91 – 297.35 |
| 19 | 321.58 – 326.91 | start<prev_end | 298.89 – 326.91 |

## Exodus

### Exodus 4 — 24 verse(s)

| Verse | Estimated timing | Reason | Original (broken) timing |
|---|---|---|---|
| 8 | 114.70 – 122.70 | end<start | 114.7 – 82.3 |
| 9 | 122.70 – 140.30 | start<prev_end | 82.44 – 93.72 |
| 10 | 140.30 – 155.10 | start<prev_end | 94.06 – 104.5 |
| 11 | 155.10 – 166.70 | start<prev_end | 104.7 – 113.18 |
| 12 | 166.70 – 173.90 | start<prev_end | 113.18 – 117.88 |
| 13 | 173.90 – 179.90 | start<prev_end | 118.14 – 122.7 |
| 14 | 179.90 – 197.50 | start<prev_end | 123.02 – 136.34 |
| 15 | 197.50 – 212.70 | start<prev_end | 137.3 – 146.42 |
| 16 | 212.70 – 222.70 | start<prev_end | 146.74 – 152.74 |
| 17 | 222.70 – 229.10 | start<prev_end | 153.1 – 157.54 |
| 18 | 229.10 – 244.70 | start<prev_end | 158.12 – 170.06 |
| 19 | 244.70 – 253.50 | start<prev_end | 170.34 – 176.54 |
| 20 | 253.50 – 265.90 | start<prev_end | 176.86 – 184.98 |
| 21 | 265.90 – 281.90 | start<prev_end | 185.4 – 196.62 |
| 22 | 281.90 – 289.10 | start<prev_end | 196.94 – 202.72 |
| 23 | 289.10 – 301.50 | start<prev_end | 202.8 – 211.74 |
| 24 | 301.50 – 310.30 | start<prev_end | 212.24 – 217.84 |
| 25 | 310.30 – 322.30 | start<prev_end | 218.08 – 226.82 |
| 26 | 322.30 – 329.90 | start<prev_end | 226.82 – 233.2 |
| 27 | 329.90 – 341.50 | start<prev_end | 233.78 – 241.52 |
| 28 | 341.50 – 350.30 | start<prev_end | 242.1 – 247.96 |
| 29 | 350.30 – 355.10 | start<prev_end | 248.38 – 252.4 |
| 30 | 355.10 – 363.90 | start<prev_end | 252.72 – 258.58 |
| 31 | 363.90 – 375.50 | start<prev_end | 258.58 – 267.34 |

### Exodus 9 — 11 verse(s)

| Verse | Estimated timing | Reason | Original (broken) timing |
|---|---|---|---|
| 25 | 265.06 – 280.26 | end<start | 265.06 – 251.84 |
| 26 | 280.26 – 285.86 | start<prev_end | 252.74 – 258.06 |
| 27 | 285.86 – 297.06 | start<prev_end | 260.08 – 269.22 |
| 28 | 297.06 – 306.26 | start<prev_end | 269.84 – 277.6 |
| 29 | 306.26 – 322.26 | start<prev_end | 279.18 – 291.14 |
| 30 | 322.26 – 329.46 | start<prev_end | 291.62 – 297.9 |
| 31 | 329.46 – 338.26 | start<prev_end | 299.92 – 305.66 |
| 32 | 338.26 – 343.86 | start<prev_end | 305.98 – 309.92 |
| 33 | 343.86 – 356.66 | start<prev_end | 311.7 – 323.18 |
| 34 | 356.66 – 365.46 | start<prev_end | 324.12 – 334.08 |
| 35 | 365.46 – 373.06 | start<prev_end | 335.08 – 342.14 |

### Exodus 12 — 23 verse(s)

| Verse | Estimated timing | Reason | Original (broken) timing |
|---|---|---|---|
| 27 | 312.40 – 328.00 | end<start | 312.4 – 307.78 |
| 28 | 328.00 – 334.40 | start<prev_end | 307.96 – 313.44 |
| 29 | 334.40 – 351.20 | start<prev_end | 315.4 – 329.86 |
| 30 | 351.20 – 364.80 | start<prev_end | 330.62 – 340.92 |
| 31 | 364.80 – 377.20 | start<prev_end | 342.56 – 353.34 |
| 32 | 377.20 – 384.40 | start<prev_end | 353.78 – 362.54 |
| 33 | 384.40 – 394.40 | start<prev_end | 364.68 – 371.5 |
| 34 | 394.40 – 403.60 | start<prev_end | 371.5 – 379.88 |
| 35 | 403.60 – 411.60 | start<prev_end | 380.6 – 387.0 |
| 36 | 411.60 – 421.20 | start<prev_end | 387.66 – 396.58 |
| 37 | 421.20 – 428.00 | start<prev_end | 398.24 – 406.4 |
| 38 | 428.00 – 434.80 | start<prev_end | 407.0 – 413.24 |
| 39 | 434.80 – 451.60 | start<prev_end | 413.98 – 426.72 |
| 40 | 451.60 – 456.80 | start<prev_end | 427.62 – 433.86 |
| 41 | 456.80 – 466.40 | start<prev_end | 434.76 – 447.34 |
| 42 | 466.40 – 481.20 | start<prev_end | 447.34 – 462.14 |
| 43 | 481.20 – 489.60 | start<prev_end | 462.14 – 464.26 |
| 44 | 489.60 – 496.00 | start<prev_end | 464.86 – 470.46 |
| 45 | 496.00 – 500.00 | start<prev_end | 471.22 – 474.5 |
| 46 | 500.00 – 503.70 | start<prev_end | 475.08 – 503.7 |
| 49 | 526.50 – 534.50 | end<start | 526.5 – 509.52 |
| 50 | 534.50 – 539.70 | start<prev_end | 511.26 – 515.54 |
| 51 | 539.70 – 547.30 | start<prev_end | 515.86 – 521.58 |

### Exodus 15 — 4 verse(s)

| Verse | Estimated timing | Reason | Original (broken) timing |
|---|---|---|---|
| 12 | 211.34 – 215.34 | end<start | 211.34 – 207.32 |
| 13 | 215.34 – 217.32 | start<prev_end | 207.32 – 217.32 |
| 26 | 317.74 – 338.94 | end<start | 317.74 – 276.08 |
| 27 | 338.94 – 348.14 | start<prev_end | 277.22 – 285.22 |

### Exodus 22 — 4 verse(s)

| Verse | Estimated timing | Reason | Original (broken) timing |
|---|---|---|---|
| 28 | 300.72 – 305.52 | end<start | 300.72 – 251.8 |
| 29 | 305.52 – 313.52 | start<prev_end | 252.8 – 259.16 |
| 30 | 313.52 – 325.12 | start<prev_end | 259.7 – 268.48 |
| 31 | 325.12 – 335.52 | start<prev_end | 269.6 – 278.52 |

### Exodus 25 — 23 verse(s)

| Verse | Estimated timing | Reason | Original (broken) timing |
|---|---|---|---|
| 18 | 147.92 – 157.52 | end<start | 147.92 – 144.44 |
| 19 | 157.52 – 168.32 | start<prev_end | 145.1 – 151.68 |
| 20 | 168.32 – 179.92 | start<prev_end | 152.9 – 164.24 |
| 21 | 179.92 – 190.32 | start<prev_end | 165.26 – 172.14 |
| 22 | 190.32 – 206.32 | start<prev_end | 173.02 – 186.02 |
| 23 | 206.32 – 216.32 | start<prev_end | 187.68 – 196.9 |
| 24 | 216.32 – 224.32 | start<prev_end | 197.28 – 203.78 |
| 25 | 224.32 – 234.72 | start<prev_end | 204.1 – 212.44 |
| 26 | 234.72 – 243.92 | start<prev_end | 213.08 – 220.52 |
| 27 | 243.92 – 251.52 | start<prev_end | 221.26 – 226.12 |
| 28 | 251.52 – 260.72 | start<prev_end | 226.78 – 233.64 |
| 29 | 260.72 – 271.92 | start<prev_end | 234.34 – 252.68 |
| 30 | 271.92 – 277.92 | start<prev_end | 252.68 – 258.68 |
| 31 | 277.92 – 292.72 | start<prev_end | 258.68 – 262.38 |
| 32 | 292.72 – 305.92 | start<prev_end | 263.18 – 272.72 |
| 33 | 305.92 – 323.92 | start<prev_end | 273.62 – 288.46 |
| 34 | 323.92 – 330.72 | start<prev_end | 290.08 – 296.14 |
| 35 | 330.72 – 346.72 | start<prev_end | 296.14 – 306.56 |
| 36 | 346.72 – 353.92 | start<prev_end | 307.74 – 313.76 |
| 37 | 353.92 – 365.12 | start<prev_end | 314.84 – 322.7 |
| 38 | 365.12 – 369.52 | start<prev_end | 323.18 – 326.36 |
| 39 | 369.52 – 376.32 | start<prev_end | 327.0 – 332.62 |
| 40 | 376.32 – 382.72 | start<prev_end | 333.06 – 337.22 |

### Exodus 27 — 4 verse(s)

| Verse | Estimated timing | Reason | Original (broken) timing |
|---|---|---|---|
| 18 | 218.74 – 231.94 | end<start | 218.74 – 184.5 |
| 19 | 231.94 – 243.54 | start<prev_end | 185.64 – 193.9 |
| 20 | 243.54 – 253.94 | start<prev_end | 195.08 – 203.92 |
| 21 | 253.94 – 270.74 | start<prev_end | 204.72 – 219.0 |

### Exodus 28 — 16 verse(s)

| Verse | Estimated timing | Reason | Original (broken) timing |
|---|---|---|---|
| 28 | 275.12 – 291.52 | end<start | 275.12 – 244.04 |
| 29 | 291.52 – 304.32 | start<prev_end | 244.78 – 269.88 |
| 30 | 304.32 – 322.32 | start<prev_end | 269.88 – 287.88 |
| 31 | 287.88 – 292.28 | end<start | 287.88 – 274.06 |
| 32 | 292.28 – 307.48 | start<prev_end | 274.52 – 284.02 |
| 33 | 307.48 – 316.28 | start<prev_end | 284.76 – 293.38 |
| 34 | 316.28 – 325.88 | start<prev_end | 293.9 – 300.44 |
| 35 | 325.88 – 339.88 | start<prev_end | 301.04 – 310.86 |
| 36 | 339.88 – 349.08 | start<prev_end | 312.18 – 320.18 |
| 37 | 349.08 – 359.88 | start<prev_end | 320.82 – 326.36 |
| 38 | 359.88 – 375.88 | start<prev_end | 326.84 – 339.72 |
| 39 | 375.88 – 385.88 | start<prev_end | 340.3 – 348.34 |
| 40 | 385.88 – 392.28 | start<prev_end | 348.88 – 354.38 |
| 41 | 392.28 – 404.68 | start<prev_end | 354.78 – 364.9 |
| 42 | 404.68 – 412.28 | start<prev_end | 365.6 – 372.32 |
| 43 | 412.28 – 432.28 | start<prev_end | 372.92 – 388.4 |

### Exodus 29 — 24 verse(s)

| Verse | Estimated timing | Reason | Original (broken) timing |
|---|---|---|---|
| 23 | 275.70 – 287.30 | end<start | 275.7 – 224.9 |
| 24 | 287.30 – 298.10 | start<prev_end | 225.22 – 233.26 |
| 25 | 298.10 – 312.50 | start<prev_end | 233.96 – 244.58 |
| 26 | 312.50 – 324.90 | start<prev_end | 245.3 – 254.1 |
| 27 | 324.90 – 340.50 | start<prev_end | 254.96 – 267.14 |
| 28 | 340.50 – 356.50 | start<prev_end | 268.06 – 281.32 |
| 29 | 356.50 – 367.30 | start<prev_end | 282.64 – 290.78 |
| 30 | 367.30 – 379.30 | start<prev_end | 291.54 – 300.32 |
| 31 | 379.30 – 386.10 | start<prev_end | 301.48 – 306.86 |
| 32 | 386.10 – 397.30 | start<prev_end | 307.44 – 313.4 |
| 33 | 397.30 – 408.90 | start<prev_end | 313.76 – 322.6 |
| 34 | 408.90 – 424.10 | start<prev_end | 323.44 – 333.68 |
| 35 | 424.10 – 434.90 | start<prev_end | 334.54 – 342.14 |
| 36 | 434.90 – 449.70 | start<prev_end | 342.44 – 352.86 |
| 37 | 449.70 – 462.50 | start<prev_end | 353.46 – 363.92 |
| 38 | 462.50 – 470.10 | start<prev_end | 364.86 – 371.58 |
| 39 | 470.10 – 478.10 | start<prev_end | 372.02 – 377.44 |
| 40 | 478.10 – 492.90 | start<prev_end | 378.34 – 389.26 |
| 41 | 492.90 – 508.90 | start<prev_end | 389.88 – 402.12 |
| 42 | 508.90 – 521.70 | start<prev_end | 402.82 – 411.66 |
| 43 | 521.70 – 528.90 | start<prev_end | 412.44 – 418.26 |
| 44 | 528.90 – 542.10 | start<prev_end | 418.94 – 429.64 |
| 45 | 542.10 – 546.90 | start<prev_end | 430.14 – 434.66 |
| 46 | 546.90 – 560.10 | start<prev_end | 434.84 – 445.82 |

### Exodus 36 — 14 verse(s)

| Verse | Estimated timing | Reason | Original (broken) timing |
|---|---|---|---|
| 7 | 73.20 – 78.80 | end<start | 73.2 – 67.32 |
| 8 | 78.80 – 83.32 | start<prev_end | 67.32 – 83.32 |
| 27 | 230.12 – 235.72 | end<start | 230.12 – 108.36 |
| 28 | 235.72 – 240.92 | start<prev_end | 108.36 – 113.56 |
| 29 | 240.92 – 250.92 | start<prev_end | 113.56 – 123.56 |
| 30 | 250.92 – 257.32 | start<prev_end | 123.56 – 129.96 |
| 31 | 257.32 – 263.72 | start<prev_end | 129.96 – 136.36 |
| 32 | 263.72 – 275.32 | start<prev_end | 136.36 – 147.96 |
| 33 | 275.32 – 282.12 | start<prev_end | 147.96 – 154.76 |
| 34 | 282.12 – 292.12 | start<prev_end | 154.76 – 164.76 |
| 35 | 164.76 – 175.16 | end<start | 164.76 – 148.98 |
| 36 | 175.16 – 185.16 | start<prev_end | 148.98 – 158.98 |
| 37 | 185.16 – 195.16 | start<prev_end | 158.98 – 168.98 |
| 38 | 195.16 – 203.96 | start<prev_end | 168.98 – 177.78 |

### Exodus 38 — 3 verse(s)

| Verse | Estimated timing | Reason | Original (broken) timing |
|---|---|---|---|
| 29 | 332.20 – 337.00 | end<start | 332.2 – 287.54 |
| 30 | 337.00 – 348.60 | start<prev_end | 287.74 – 296.62 |
| 31 | 348.60 – 361.40 | start<prev_end | 296.74 – 305.82 |

### Exodus 39 — 14 verse(s)

| Verse | Estimated timing | Reason | Original (broken) timing |
|---|---|---|---|
| 15 | 280.24 – 285.04 | end<start | 280.24 – 261.96 |
| 16 | 285.04 – 295.04 | start<prev_end | 261.96 – 271.96 |
| 17 | 295.04 – 298.02 | start<prev_end | 271.96 – 298.02 |
| 33 | 396.22 – 403.42 | end<start | 396.22 – 332.96 |
| 34 | 403.42 – 410.22 | start<prev_end | 332.96 – 339.76 |
| 35 | 339.76 – 344.56 | end<start | 339.76 – 338.98 |
| 36 | 344.56 – 348.96 | start<prev_end | 338.98 – 343.38 |
| 37 | 348.96 – 351.38 | start<prev_end | 343.38 – 351.38 |
| 41 | 383.38 – 393.38 | end<start | 383.38 – 351.26 |
| 42 | 393.38 – 399.38 | start<prev_end | 352.3 – 357.24 |
| 43 | 399.38 – 408.58 | start<prev_end | 358.04 – 367.2 |
| 38 | 427.42 – 435.82 | start<prev_end | 351.38 – 359.78 |
| 39 | 435.82 – 443.42 | start<prev_end | 359.78 – 367.38 |
| 40 | 443.42 – 459.42 | start<prev_end | 367.38 – 383.38 |

---
_Generated by the #57 audio-mapping batch, 2026-11-18. The estimated timings are placeholders — please verify each one against the audio before relying on it._