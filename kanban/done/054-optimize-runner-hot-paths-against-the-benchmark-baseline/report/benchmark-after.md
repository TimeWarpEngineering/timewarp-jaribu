# Benchmark results

Numbers from one serial run. Build time is not included in cold or warm.
The measured commit is the commit that was checked out while the processes ran.

- Measured at: `6e3220471f71dbc261d0f02e73a65306a735dd28`
- Date (UTC): 2026-11-09 07:53:39Z
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
| jaribu-runfile | startup | 1 | 1561.1 | 1480.6 | 1484.8 | 1448.4 | 1502.4 | 1484807.50 |
| jaribu-runfile | volume-1k | 1000 | 2048.3 | 1959.3 | 1875.3 | 1870.9 | 2050.3 | 1875.31 |
| jaribu-runfile | volume-10k | 10000 | 18129.6 | 16799.5 | 17818.4 | 16750.5 | 18177.7 | 1781.84 |
| jaribu-runfile | parameterized | 5000 | 2301.3 | 2218.8 | 1956.4 | 1940.0 | 1984.4 | 391.28 |
| jaribu-runfile | async-1k | 1000 | 2982.7 | 2603.9 | 2580.2 | 2543.0 | 2648.1 | 2580.24 |
| jaribu-runfile | fixtures | 1000 | 2181.5 | 1606.7 | 1590.1 | 1555.7 | 1687.7 | 1590.11 |
| jaribu-mtp | startup | 1 | 2388.2 | 737.1 | 739.4 | 711.6 | 741.3 | 739431.50 |
| jaribu-mtp | volume-1k | 1000 | 2486.3 | 1134.5 | 1280.7 | 1226.4 | 1294.6 | 1280.69 |
| jaribu-mtp | volume-10k | 10000 | 3964.3 | 17532.2 | 17761.1 | 16644.7 | 22893.3 | 1776.11 |
| jaribu-mtp | parameterized | 5000 | 2748.4 | 1702.3 | 1565.1 | 1463.6 | 1572.5 | 313.02 |
| jaribu-mtp | async-1k | 1000 | 2815.8 | 1996.4 | 2056.0 | 2038.7 | 2082.5 | 2056.00 |
| jaribu-mtp | fixtures | 1000 | 2795.2 | 969.9 | 954.8 | 899.2 | 1005.2 | 954.79 |
| fixie | startup | 1 | 1168.2 | 705.4 | 690.8 | 690.3 | 693.2 | 690817.00 |
| fixie | volume-1k | 1000 | 1142.7 | 730.6 | 700.5 | 697.8 | 718.3 | 700.51 |
| fixie | volume-10k | 10000 | 1933.7 | 1325.3 | 1474.5 | 1276.6 | 1558.2 | 147.45 |
| fixie | parameterized | 5000 | 1445.0 | 997.4 | 920.8 | 901.9 | 963.9 | 184.16 |
| fixie | async-1k | 1000 | 1520.4 | 1504.0 | 1474.0 | 1456.9 | 1494.0 | 1473.99 |
| fixie | fixtures | 1000 | 1223.0 | 890.3 | 893.8 | 886.1 | 929.9 | 893.76 |
| tunit | startup | 1 | 1253.0 | 922.6 | 975.3 | 950.2 | 1043.3 | 975294.30 |
| tunit | volume-1k | 1000 | 1774.9 | 1297.7 | 1305.9 | 1285.4 | 1361.2 | 1305.89 |
| tunit | volume-10k | 10000 | 24361.2 | 4989.9 | 4337.7 | 4224.5 | 4699.5 | 433.77 |
| tunit | parameterized | 5000 | 1523.3 | 2217.7 | 2185.2 | 2166.3 | 2205.2 | 437.03 |
| tunit | async-1k | 1000 | 1585.9 | 1300.0 | 1294.6 | 1270.1 | 1303.3 | 1294.63 |
| tunit | fixtures | 1000 | 1625.8 | 1257.5 | 1223.6 | 1202.5 | 1247.8 | 1223.56 |
| xunit | startup | 1 | 1112.4 | 915.5 | 869.6 | 864.6 | 876.6 | 869556.80 |
| xunit | volume-1k | 1000 | 1213.9 | 1310.4 | 1207.4 | 1187.9 | 1282.7 | 1207.38 |
| xunit | volume-10k | 10000 | 2332.4 | 5025.1 | 5217.0 | 5180.7 | 5221.2 | 521.70 |
| xunit | parameterized | 5000 | 1278.5 | 1902.2 | 1859.0 | 1823.2 | 1926.6 | 371.80 |
| xunit | async-1k | 1000 | 1471.6 | 2042.3 | 2073.0 | 1992.2 | 2093.4 | 2072.98 |
| xunit | fixtures | 1000 | 1266.5 | 1170.9 | 1203.6 | 1199.5 | 1248.9 | 1203.59 |

## Time-to-first-test, working set, allocations

Time-to-first-test is the earlier of the captured `BENCH first` line and the signal file's last-write time, measured from process start (capture end minus process runtime). A negative delta is 0. When only one signal exists, that one is used.
Peak bytes are the sampled sum of `VmRSS`. A launch shorter than the 15 ms sample can report 0.
Allocated bytes are the `BENCH allocated_bytes` probe delta.

| Host | Workload | TTFT cold ms | TTFT warm median ms | Cold peak | Warm peak | Cold allocated | Warm allocated median |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| jaribu-runfile | startup | 1415.8 | 1427.6 | 263,806,976 | 265,179,136 | 53,440 | 47,248 |
| jaribu-runfile | volume-1k | 1413.8 | 1384.7 | 319,565,824 | 319,815,680 | 73,590,608 | 73,587,824 |
| jaribu-runfile | volume-10k | 1456.6 | 1575.3 | 346,755,072 | 346,783,744 | 6,496,007,584 | 6,495,926,336 |
| jaribu-runfile | parameterized | 1644.2 | 1453.7 | 326,320,128 | 326,029,312 | 82,081,736 | 82,081,600 |
| jaribu-runfile | async-1k | 1404.4 | 1368.4 | 328,818,688 | 328,740,864 | 74,670,760 | 74,718,184 |
| jaribu-runfile | fixtures | 1369.7 | 1355.7 | 280,129,536 | 280,154,112 | 12,484,728 | 12,479,400 |
| jaribu-mtp | startup | 675.0 | 684.8 | 179,838,976 | 180,244,480 | 27,320 | 28,192 |
| jaribu-mtp | volume-1k | 622.7 | 690.8 | 236,904,448 | 237,977,600 | 76,328,392 | 76,330,000 |
| jaribu-mtp | volume-10k | 675.7 | 778.6 | 280,330,240 | 280,748,032 | 6,523,718,712 | 6,523,620,656 |
| jaribu-mtp | parameterized | 746.1 | 683.2 | 249,012,224 | 248,758,272 | 62,721,880 | 62,767,024 |
| jaribu-mtp | async-1k | 654.6 | 676.9 | 250,863,616 | 251,338,752 | 78,034,968 | 78,047,336 |
| jaribu-mtp | fixtures | 690.1 | 688.1 | 196,730,880 | 197,656,576 | 14,051,600 | 14,054,624 |
| fixie | startup | 667.9 | 657.4 | 178,491,392 | 178,450,432 | 17,296 | 5,128 |
| fixie | volume-1k | 622.9 | 593.6 | 180,424,704 | 181,104,640 | 1,198,768 | 1,198,856 |
| fixie | volume-10k | 621.3 | 640.2 | 199,696,384 | 206,651,392 | 11,794,200 | 11,795,224 |
| fixie | parameterized | 793.7 | 707.3 | 192,802,816 | 192,765,952 | 12,269,280 | 12,278,568 |
| fixie | async-1k | 693.9 | 644.0 | 190,918,656 | 191,131,648 | 1,653,912 | 1,653,912 |
| fixie | fixtures | 690.4 | 671.5 | 189,681,664 | 190,324,736 | 8,595,160 | 8,618,792 |
| tunit | startup | 798.0 | 838.3 | 197,447,680 | 197,615,616 | 2,560,008 | 2,559,104 |
| tunit | volume-1k | 902.6 | 915.6 | 237,481,984 | 238,759,936 | 19,152,536 | 19,232,016 |
| tunit | volume-10k | 1788.4 | 1796.3 | 506,916,864 | 575,447,040 | 183,732,824 | 185,901,096 |
| tunit | parameterized | 1003.1 | 1007.3 | 315,162,624 | 316,092,416 | 97,869,016 | 99,478,048 |
| tunit | async-1k | 859.6 | 847.4 | 244,649,984 | 244,695,040 | 19,966,744 | 20,003,320 |
| tunit | fixtures | 891.6 | 837.2 | 243,728,384 | 244,191,232 | 21,419,944 | 21,499,888 |
| xunit | startup | 832.5 | 785.6 | 195,330,048 | 195,338,240 | 59,456 | 61,736 |
| xunit | volume-1k | 888.0 | 841.6 | 251,990,016 | 252,350,464 | 58,999,312 | 59,005,320 |
| xunit | volume-10k | 1127.8 | 1162.4 | 326,885,376 | 327,770,112 | 1,309,172,240 | 1,309,288,800 |
| xunit | parameterized | 1010.1 | 986.6 | 271,187,968 | 270,794,752 | 195,302,032 | 195,303,136 |
| xunit | async-1k | 834.6 | 838.5 | 266,608,640 | 266,825,728 | 67,429,968 | 67,425,912 |
| xunit | fixtures | 878.5 | 889.0 | 255,393,792 | 255,401,984 | 55,993,400 | 55,913,768 |

## Marginal per-test

`(warm volume-10k − warm volume-1k) / 9000` microseconds. Shared startup drops out when per-test cost is linear. A host whose cost grows with methods per class reports a class-size-dependent value, not a constant. A host needs both volume rows.

| Host | Volume-1k warm ms | Volume-10k warm ms | Marginal µs |
| --- | ---: | ---: | ---: |
| jaribu-runfile | 1875.3 | 17818.4 | 1771.45 |
| jaribu-mtp | 1280.7 | 17761.1 | 1831.16 |
| fixie | 700.5 | 1474.5 | 85.99 |
| tunit | 1305.9 | 4337.7 | 336.86 |
| xunit | 1207.4 | 5217.0 | 445.51 |

## Jaribu in-process

BenchmarkDotNet `ShortRunJob` and `MemoryDiagnoser` call `RunTestsAsync` with `NullSink`. Means are milliseconds.

| Benchmark | Mean ms | Allocated bytes |
| --- | ---: | ---: |
| JaribuRunnerBench.Startup: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3) | 0.002 | 2,712 |
| JaribuRunnerBench.Volume1K: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3) | 58.1 | 66,001,800 |
| JaribuRunnerBench.Volume10K: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3) | 5133.0 | 6,420,263,656 |
| JaribuRunnerBench.Parameterized: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3) | 31.5 | 43,181,032 |
| JaribuRunnerBench.Async1K: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3) | 77.9 | 66,754,456 |
| JaribuRunnerBench.Fixtures: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3) | 1.6 | 2,950,776 |

