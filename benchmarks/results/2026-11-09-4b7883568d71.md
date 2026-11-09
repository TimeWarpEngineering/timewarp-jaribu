# Benchmark results

Numbers from one serial run. Build time is not included in cold or warm.
The measured commit is the commit that was checked out while the processes ran.

- Measured at: `4b7883568d710dd82c102f1126a6dee387bc711c`
- Date (UTC): 2026-11-09 06:42:17Z
- Spec: 040-1
- SDK: 10.0.403
- Runtime: .NET 10.0.14
- Comparability: compare these numbers only with a run on this SDK band.
- OS: Ubuntu 26.04 LTS
- Processors: 32
- Warm launches: 3

## Versions

- BenchmarkDotNet 0.15.8
- Fixie 4.1.0
- Fixie.TestAdapter 4.1.0
- Jaribu 1.0.0-beta.16
- Microsoft.Testing.Platform 2.3.3
- TUnit 1.81.10
- TimeWarp.Fixie 3.1.0
- xunit.v3.mtp-v2 4.0.3

## Wall-clock

| Host | Workload | Cases | Build ms | Cold ms | Warm median ms | Warm min ms | Warm max ms | Per-test µs |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| jaribu-runfile | startup | 1 | 2300.0 | 1330.9 | 1326.8 | 1290.6 | 1338.7 | 1326768.20 |
| jaribu-runfile | volume-1k | 1000 | 2242.9 | 1711.8 | 1996.6 | 1726.7 | 2397.3 | 1996.55 |
| jaribu-runfile | volume-10k | 10000 | 18856.9 | 16258.1 | 17868.6 | 17216.9 | 18489.7 | 1786.86 |
| jaribu-runfile | parameterized | 5000 | 2566.9 | 1935.9 | 2035.1 | 2029.9 | 2158.3 | 407.02 |
| jaribu-runfile | async-1k | 1000 | 4074.9 | 2468.9 | 2431.1 | 2408.6 | 2476.3 | 2431.08 |
| jaribu-runfile | fixtures | 1000 | 2140.9 | 1505.3 | 1525.5 | 1494.6 | 1526.7 | 1525.49 |
| jaribu-mtp | startup | 1 | 2369.2 | 598.8 | 598.7 | 596.5 | 606.4 | 598743.40 |
| jaribu-mtp | volume-1k | 1000 | 2305.3 | 1059.0 | 1168.7 | 1076.1 | 1349.6 | 1168.75 |
| jaribu-mtp | volume-10k | 10000 | 6313.0 | 16311.6 | 16602.4 | 15938.3 | 19604.9 | 1660.24 |
| jaribu-mtp | parameterized | 5000 | 2618.0 | 1370.3 | 1337.8 | 1297.9 | 1363.6 | 267.56 |
| jaribu-mtp | async-1k | 1000 | 2498.1 | 1806.0 | 1787.8 | 1771.7 | 1855.0 | 1787.78 |
| jaribu-mtp | fixtures | 1000 | 2315.2 | 791.0 | 1275.0 | 919.3 | 1463.6 | 1275.04 |
| fixie | startup | 1 | 2148.8 | 946.8 | 591.6 | 543.5 | 634.9 | 591579.10 |
| fixie | volume-1k | 1000 | 1112.7 | 628.4 | 662.1 | 629.8 | 691.8 | 662.10 |
| fixie | volume-10k | 10000 | 1864.4 | 1266.7 | 1276.9 | 1220.2 | 1355.6 | 127.69 |
| fixie | parameterized | 5000 | 1128.0 | 673.0 | 777.8 | 682.6 | 792.0 | 155.56 |
| fixie | async-1k | 1000 | 1397.7 | 1387.9 | 1410.4 | 1320.1 | 1477.8 | 1410.37 |
| fixie | fixtures | 1000 | 1030.3 | 719.7 | 715.7 | 697.8 | 721.2 | 715.68 |
| tunit | startup | 1 | 1001.2 | 779.7 | 778.9 | 765.5 | 786.6 | 778901.30 |
| tunit | volume-1k | 1000 | 1346.5 | 1096.6 | 1141.6 | 1086.4 | 1377.0 | 1141.56 |
| tunit | volume-10k | 10000 | 22669.4 | 3997.9 | 3801.3 | 3778.4 | 4265.4 | 380.13 |
| tunit | parameterized | 5000 | 1605.0 | 2186.5 | 2041.5 | 2014.7 | 2173.5 | 408.29 |
| tunit | async-1k | 1000 | 1608.4 | 1259.2 | 1219.5 | 1197.9 | 1262.4 | 1219.49 |
| tunit | fixtures | 1000 | 1599.9 | 1194.0 | 1180.5 | 1177.9 | 1208.2 | 1180.50 |
| xunit | startup | 1 | 1083.6 | 884.0 | 850.2 | 839.9 | 883.5 | 850209.30 |
| xunit | volume-1k | 1000 | 1175.1 | 1236.2 | 1156.1 | 1140.5 | 1180.1 | 1156.09 |
| xunit | volume-10k | 10000 | 2431.5 | 6098.2 | 5377.0 | 5081.5 | 5815.0 | 537.70 |
| xunit | parameterized | 5000 | 1123.6 | 1720.5 | 1721.8 | 1713.4 | 1878.6 | 344.35 |
| xunit | async-1k | 1000 | 1456.8 | 1834.5 | 1824.8 | 1821.3 | 1850.9 | 1824.83 |
| xunit | fixtures | 1000 | 1107.1 | 1097.1 | 1102.9 | 1097.0 | 1115.5 | 1102.86 |

## Time-to-first-test, working set, allocations

Time-to-first-test is the earlier of the captured `BENCH first` line and the signal file's last-write time, measured from process start (capture end minus process runtime). A negative delta is 0. When only one signal exists, that one is used.
Peak bytes are the sampled sum of `VmRSS`. A launch shorter than the 15 ms sample can report 0.
Allocated bytes are the `BENCH allocated_bytes` probe delta.

| Host | Workload | TTFT cold ms | TTFT warm median ms | Cold peak | Warm peak | Cold allocated | Warm allocated median |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| jaribu-runfile | startup | 1270.3 | 1276.3 | 264,777,728 | 263,663,616 | 47,296 | 47,296 |
| jaribu-runfile | volume-1k | 1250.1 | 1267.5 | 318,779,392 | 319,168,512 | 73,734,160 | 73,727,968 |
| jaribu-runfile | volume-10k | 1342.5 | 1364.7 | 340,221,952 | 340,623,360 | 6,497,905,456 | 6,497,898,936 |
| jaribu-runfile | parameterized | 1332.5 | 1347.4 | 324,784,128 | 324,685,824 | 98,987,480 | 98,987,480 |
| jaribu-runfile | async-1k | 1285.6 | 1264.1 | 327,921,664 | 327,917,568 | 74,982,104 | 75,031,344 |
| jaribu-runfile | fixtures | 1274.5 | 1293.8 | 280,539,136 | 280,190,976 | 12,605,176 | 12,611,368 |
| jaribu-mtp | startup | 547.2 | 545.8 | 180,441,088 | 180,162,560 | 22,040 | 20,608 |
| jaribu-mtp | volume-1k | 539.7 | 557.4 | 237,547,520 | 237,916,160 | 76,335,592 | 76,387,912 |
| jaribu-mtp | volume-10k | 576.9 | 582.8 | 275,369,984 | 279,715,840 | 6,525,283,592 | 6,525,222,520 |
| jaribu-mtp | parameterized | 602.0 | 551.3 | 249,507,840 | 248,061,952 | 79,432,024 | 79,484,992 |
| jaribu-mtp | async-1k | 566.1 | 543.5 | 250,912,768 | 251,174,912 | 77,778,248 | 77,788,400 |
| jaribu-mtp | fixtures | 539.3 | 865.3 | 196,546,560 | 198,209,536 | 14,107,288 | 14,091,064 |
| fixie | startup | 904.3 | 559.5 | 178,282,496 | 178,139,136 | 17,296 | 4,832 |
| fixie | volume-1k | 520.7 | 559.6 | 181,088,256 | 181,506,048 | 1,196,776 | 1,197,800 |
| fixie | volume-10k | 555.0 | 544.6 | 198,148,096 | 202,813,440 | 11,794,200 | 11,793,560 |
| fixie | parameterized | 504.0 | 590.6 | 190,844,928 | 192,847,872 | 12,279,440 | 12,268,216 |
| fixie | async-1k | 579.1 | 609.4 | 190,861,312 | 191,438,848 | 1,654,976 | 1,653,912 |
| fixie | fixtures | 521.3 | 510.9 | 190,058,496 | 189,571,072 | 8,619,816 | 8,595,192 |
| tunit | startup | 661.0 | 659.0 | 193,474,560 | 195,989,504 | 2,556,792 | 2,558,992 |
| tunit | volume-1k | 753.0 | 769.7 | 237,441,024 | 240,648,192 | 19,206,544 | 19,258,440 |
| tunit | volume-10k | 1592.4 | 1415.1 | 470,302,720 | 513,687,552 | 189,523,336 | 179,619,288 |
| tunit | parameterized | 950.6 | 889.7 | 314,527,744 | 318,054,400 | 97,376,672 | 99,806,064 |
| tunit | async-1k | 844.1 | 806.0 | 243,367,936 | 244,453,376 | 19,988,472 | 20,011,800 |
| tunit | fixtures | 824.5 | 814.5 | 242,659,328 | 242,757,632 | 21,468,624 | 21,474,440 |
| xunit | startup | 802.8 | 767.2 | 195,526,656 | 196,096,000 | 60,024 | 61,840 |
| xunit | volume-1k | 881.2 | 783.9 | 252,002,304 | 252,203,008 | 59,013,584 | 59,017,592 |
| xunit | volume-10k | 1263.8 | 1132.8 | 328,130,560 | 327,909,376 | 1,309,643,656 | 1,309,531,648 |
| xunit | parameterized | 859.2 | 882.9 | 274,173,952 | 274,919,424 | 195,327,920 | 195,367,976 |
| xunit | async-1k | 747.6 | 753.6 | 266,108,928 | 266,510,336 | 67,401,312 | 67,403,496 |
| xunit | fixtures | 802.6 | 810.7 | 255,188,992 | 255,266,816 | 55,883,152 | 55,831,376 |

## Marginal per-test

`(warm volume-10k − warm volume-1k) / 9000` microseconds. Shared startup drops out when per-test cost is linear. A host whose cost grows with methods per class reports a class-size-dependent value, not a constant. A host needs both volume rows.

| Host | Volume-1k warm ms | Volume-10k warm ms | Marginal µs |
| --- | ---: | ---: | ---: |
| jaribu-runfile | 1996.6 | 17868.6 | 1763.56 |
| jaribu-mtp | 1168.7 | 16602.4 | 1714.85 |
| fixie | 662.1 | 1276.9 | 68.31 |
| tunit | 1141.6 | 3801.3 | 295.53 |
| xunit | 1156.1 | 5377.0 | 468.99 |

## Jaribu in-process

BenchmarkDotNet `ShortRunJob` and `MemoryDiagnoser` call `RunTestsAsync` with `NullSink`. Means are milliseconds.

| Benchmark | Mean ms | Allocated bytes |
| --- | ---: | ---: |
| JaribuRunnerBench.Startup: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3) | 0.004 | 4,576 |
| JaribuRunnerBench.Volume1K: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3) | 108.3 | 67,491,192 |
| JaribuRunnerBench.Volume10K: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3) | 5105.4 | 6,432,879,576 |
| JaribuRunnerBench.Parameterized: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3) | 185.7 | 63,444,584 |
| JaribuRunnerBench.Async1K: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3) | 98.3 | 68,595,848 |
| JaribuRunnerBench.Fixtures: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3) | 4.7 | 3,853,304 |

