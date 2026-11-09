# Benchmark results

Numbers from one serial run. Build time is not included in cold or warm.
The measured commit is the commit that was checked out while the processes ran.

- Measured at: `5f2862e7e2d17ddc802b905333be1579c847cac5`
- Date (UTC): 2026-11-09 06:08:11Z
- Spec: 040-1
- SDK: 11.0.100-rc.3.26522.148
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
| jaribu-runfile | startup | 1 | 2127.7 | 1183.7 | 1170.2 | 1154.8 | 1178.6 | 1170158.30 |
| jaribu-runfile | volume-1k | 1000 | 1852.5 | 1711.1 | 1697.1 | 1494.8 | 1704.6 | 1697.07 |
| jaribu-runfile | volume-10k | 10000 | 16238.8 | 14686.4 | 14875.7 | 14672.6 | 15607.8 | 1487.57 |
| jaribu-runfile | parameterized | 5000 | 1862.2 | 1839.7 | 1780.3 | 1776.5 | 1837.6 | 356.05 |
| jaribu-runfile | async-1k | 1000 | 2535.1 | 2035.2 | 2118.2 | 2096.6 | 2154.4 | 2118.19 |
| jaribu-runfile | fixtures | 1000 | 1569.6 | 1470.9 | 1393.2 | 1378.7 | 1409.7 | 1393.25 |
| jaribu-mtp | startup | 1 | 2887.7 | 593.9 | 592.2 | 584.0 | 629.1 | 592228.80 |
| jaribu-mtp | volume-1k | 1000 | 1893.1 | 1058.1 | 1059.6 | 1047.6 | 1119.2 | 1059.65 |
| jaribu-mtp | volume-10k | 10000 | 2915.3 | 16248.1 | 16464.4 | 16051.1 | 17194.7 | 1646.44 |
| jaribu-mtp | parameterized | 5000 | 1955.7 | 1497.9 | 1535.8 | 1449.4 | 1656.1 | 307.16 |
| jaribu-mtp | async-1k | 1000 | 2126.7 | 1797.4 | 1848.7 | 1797.4 | 2047.5 | 1848.71 |
| jaribu-mtp | fixtures | 1000 | 2092.2 | 905.8 | 923.9 | 910.0 | 1006.5 | 923.86 |
| fixie | startup | 1 | 995.5 | 575.0 | 553.7 | 538.5 | 626.0 | 553663.50 |
| fixie | volume-1k | 1000 | 1058.7 | 609.7 | 595.3 | 594.9 | 621.4 | 595.27 |
| fixie | volume-10k | 10000 | 1877.3 | 1195.3 | 1202.6 | 1165.1 | 1252.8 | 120.26 |
| fixie | parameterized | 5000 | 981.9 | 692.6 | 680.2 | 673.9 | 685.9 | 136.03 |
| fixie | async-1k | 1000 | 1304.1 | 1451.9 | 1464.4 | 1445.2 | 1487.1 | 1464.40 |
| fixie | fixtures | 1000 | 1141.1 | 914.5 | 921.9 | 900.1 | 922.1 | 921.93 |
| tunit | startup | 1 | 1277.4 | 1136.9 | 1134.0 | 1104.2 | 1154.9 | 1133972.20 |
| tunit | volume-1k | 1000 | 2148.6 | 1548.9 | 1382.0 | 1368.4 | 1439.4 | 1382.03 |
| tunit | volume-10k | 10000 | 23061.3 | 4630.8 | 4543.8 | 4437.1 | 4858.4 | 454.38 |
| tunit | parameterized | 5000 | 1714.7 | 2433.2 | 2336.0 | 2261.9 | 2424.4 | 467.19 |
| tunit | async-1k | 1000 | 1480.4 | 1426.7 | 1353.7 | 1342.8 | 1411.0 | 1353.67 |
| tunit | fixtures | 1000 | 1751.9 | 1329.8 | 1267.5 | 1263.5 | 1270.6 | 1267.47 |
| xunit | startup | 1 | 1054.0 | 876.1 | 850.2 | 797.5 | 891.6 | 850169.50 |
| xunit | volume-1k | 1000 | 1158.7 | 1228.3 | 1205.3 | 1189.0 | 1205.3 | 1205.28 |
| xunit | volume-10k | 10000 | 2343.1 | 5669.4 | 5292.4 | 5236.7 | 5636.2 | 529.24 |
| xunit | parameterized | 5000 | 1078.6 | 1931.8 | 1891.0 | 1883.3 | 1934.6 | 378.20 |
| xunit | async-1k | 1000 | 1414.1 | 1910.0 | 1853.8 | 1844.1 | 1906.3 | 1853.83 |
| xunit | fixtures | 1000 | 1009.6 | 1124.4 | 1136.6 | 1110.1 | 1180.8 | 1136.65 |

## Time-to-first-test, working set, allocations

Time-to-first-test is the `BENCH first` timestamp minus (capture end − process runtime). A negative delta is 0.
Peak bytes are the sampled sum of `VmRSS`. A launch shorter than the 15 ms sample can report 0.
Allocated bytes are the `BENCH allocated_bytes` probe delta.

| Host | Workload | TTFT cold ms | TTFT warm median ms | Cold peak | Warm peak | Cold allocated | Warm allocated median |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| jaribu-runfile | startup | 1128.4 | 1119.5 | 277,729,280 | 278,306,816 | 48,944 | 48,944 |
| jaribu-runfile | volume-1k | 1129.9 | 1120.5 | 332,472,320 | 332,431,360 | 74,919,040 | 74,912,848 |
| jaribu-runfile | volume-10k | 1124.6 | 1126.7 | 355,147,776 | 355,766,272 | 6,506,054,544 | 6,506,039,712 |
| jaribu-runfile | parameterized | 1154.9 | 1134.0 | 341,229,568 | 342,016,000 | 102,406,808 | 102,400,616 |
| jaribu-runfile | async-1k | 1133.8 | 1195.8 | 337,997,824 | 338,345,984 | 75,909,120 | 75,909,160 |
| jaribu-runfile | fixtures | 1207.8 | 1133.2 | 295,165,952 | 296,030,208 | 13,969,704 | 13,969,704 |
| jaribu-mtp | startup | 538.4 | 536.1 | 203,816,960 | 206,340,096 | 30,488 | 31,520 |
| jaribu-mtp | volume-1k | 539.2 | 549.9 | 263,933,952 | 264,241,152 | 76,410,416 | 76,417,016 |
| jaribu-mtp | volume-10k | 602.3 | 614.3 | 302,952,448 | 306,958,336 | 6,525,254,344 | 6,525,266,728 |
| jaribu-mtp | parameterized | 571.1 | 588.7 | 276,910,080 | 276,635,648 | 79,440,424 | 79,442,536 |
| jaribu-mtp | async-1k | 553.0 | 570.1 | 275,619,840 | 279,146,496 | 77,798,672 | 77,803,488 |
| jaribu-mtp | fixtures | 620.7 | 647.4 | 223,129,600 | 224,669,696 | 14,051,592 | 14,111,192 |
| fixie | startup | 510.2 | 515.5 | 200,404,992 | 199,884,800 | 5,808 | 5,976 |
| fixie | volume-1k | 510.5 | 488.4 | 202,633,216 | 202,600,448 | 1,198,648 | 1,198,648 |
| fixie | volume-10k | 511.5 | 503.5 | 225,050,624 | 228,913,152 | 11,796,072 | 11,783,608 |
| fixie | parameterized | 530.6 | 506.0 | 212,287,488 | 213,024,768 | 12,268,936 | 12,269,064 |
| fixie | async-1k | 551.5 | 578.0 | 213,131,264 | 212,738,048 | 1,604,384 | 1,654,760 |
| fixie | fixtures | 639.1 | 658.5 | 212,094,976 | 211,517,440 | 8,619,976 | 8,619,872 |
| tunit | startup | 967.5 | 968.5 | 223,449,088 | 225,116,160 | 2,612,848 | 2,611,952 |
| tunit | volume-1k | 1084.1 | 947.2 | 269,611,008 | 268,509,184 | 19,048,832 | 19,320,536 |
| tunit | volume-10k | 1811.4 | 1752.4 | 487,718,912 | 599,617,536 | 187,966,872 | 185,326,600 |
| tunit | parameterized | 994.3 | 963.9 | 345,128,960 | 342,568,960 | 104,809,768 | 98,142,840 |
| tunit | async-1k | 902.9 | 862.6 | 276,070,400 | 272,404,480 | 20,104,776 | 19,952,136 |
| tunit | fixtures | 894.7 | 842.6 | 271,974,400 | 274,833,408 | 21,501,520 | 21,421,456 |
| xunit | startup | 787.0 | 763.2 | 225,067,008 | 221,958,144 | 112,920 | 112,720 |
| xunit | volume-1k | 859.4 | 797.6 | 278,335,488 | 280,592,384 | 59,065,672 | 59,067,880 |
| xunit | volume-10k | 1234.3 | 1135.8 | 357,294,080 | 356,786,176 | 1,309,555,000 | 1,309,554,760 |
| xunit | parameterized | 960.1 | 926.7 | 295,718,912 | 303,747,072 | 195,421,456 | 195,686,760 |
| xunit | async-1k | 790.8 | 753.1 | 290,942,976 | 291,184,640 | 67,449,456 | 67,453,152 |
| xunit | fixtures | 794.3 | 813.1 | 280,846,336 | 281,620,480 | 55,905,040 | 55,948,216 |

## Marginal per-test

`(warm volume-10k − warm volume-1k) / 9000` microseconds. Shared startup drops out. A host needs both volume rows.

| Host | Volume-1k warm ms | Volume-10k warm ms | Marginal µs |
| --- | ---: | ---: | ---: |
| jaribu-runfile | 1697.1 | 14875.7 | 1464.29 |
| jaribu-mtp | 1059.6 | 16464.4 | 1711.64 |
| fixie | 595.3 | 1202.6 | 67.48 |
| tunit | 1382.0 | 4543.8 | 351.30 |
| xunit | 1205.3 | 5292.4 | 454.12 |

## Jaribu in-process

BenchmarkDotNet `ShortRunJob` and `MemoryDiagnoser` call `RunTestsAsync` with `NullSink`. Means are milliseconds.

| Benchmark | Mean ms | Allocated bytes |
| --- | ---: | ---: |
| JaribuRunnerBench.Startup: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3) | 0.005 | 4,576 |
| JaribuRunnerBench.Volume1K: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3) | 90.7 | 67,491,192 |
| JaribuRunnerBench.Volume10K: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3) | 5075.3 | 6,432,878,040 |
| JaribuRunnerBench.Parameterized: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3) | 90.9 | 62,005,332 |
| JaribuRunnerBench.Async1K: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3) | 129.6 | 68,595,832 |
| JaribuRunnerBench.Fixtures: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3) | 5.0 | 3,873,104 |

