# Benchmark results

Numbers from one serial run. Build time is not included in cold or warm.
The measured commit is the commit that was checked out while the processes ran.

- Measured at: `6e3220471f71dbc261d0f02e73a65306a735dd28`
- Date (UTC): 2026-11-09 07:31:27Z
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
| jaribu-runfile | startup | 1 | 3865.9 | 1574.6 | 1487.9 | 1475.3 | 1497.6 | 1487923.00 |
| jaribu-runfile | volume-1k | 1000 | 2536.6 | 1926.3 | 2018.0 | 1963.7 | 2349.6 | 2017.96 |
| jaribu-runfile | volume-10k | 10000 | 22994.1 | 18578.5 | 18583.1 | 18516.8 | 20989.2 | 1858.31 |
| jaribu-runfile | parameterized | 5000 | 2707.1 | 3209.7 | 2128.7 | 2037.4 | 3348.3 | 425.73 |
| jaribu-runfile | async-1k | 1000 | 3600.9 | 2743.0 | 2756.5 | 2707.5 | 2925.1 | 2756.50 |
| jaribu-runfile | fixtures | 1000 | 2725.3 | 1785.1 | 1715.1 | 1694.7 | 1724.7 | 1715.05 |
| jaribu-mtp | startup | 1 | 3037.1 | 747.3 | 723.5 | 712.2 | 769.2 | 723499.60 |
| jaribu-mtp | volume-1k | 1000 | 2523.9 | 1221.3 | 1240.8 | 1234.9 | 1257.0 | 1240.83 |
| jaribu-mtp | volume-10k | 10000 | 3990.4 | 16524.5 | 16614.5 | 16341.8 | 19326.7 | 1661.45 |
| jaribu-mtp | parameterized | 5000 | 2373.7 | 1433.2 | 1467.1 | 1444.3 | 1481.9 | 293.41 |
| jaribu-mtp | async-1k | 1000 | 2783.5 | 1970.2 | 2014.6 | 1956.4 | 2040.8 | 2014.57 |
| jaribu-mtp | fixtures | 1000 | 2498.4 | 1009.1 | 968.1 | 926.6 | 980.5 | 968.05 |
| fixie | startup | 1 | 1436.0 | 663.8 | 659.3 | 651.3 | 705.1 | 659306.20 |
| fixie | volume-1k | 1000 | 1301.8 | 729.9 | 719.3 | 708.9 | 731.9 | 719.35 |
| fixie | volume-10k | 10000 | 2010.9 | 1396.7 | 1509.7 | 1331.1 | 1645.9 | 150.97 |
| fixie | parameterized | 5000 | 1575.4 | 887.2 | 832.2 | 781.8 | 925.1 | 166.44 |
| fixie | async-1k | 1000 | 1607.3 | 1669.7 | 1608.7 | 1605.6 | 1742.5 | 1608.66 |
| fixie | fixtures | 1000 | 1243.4 | 891.8 | 868.7 | 830.1 | 917.5 | 868.69 |
| tunit | startup | 1 | 1947.7 | 915.1 | 940.1 | 890.6 | 956.2 | 940061.30 |
| tunit | volume-1k | 1000 | 1727.2 | 1271.9 | 1278.6 | 1252.6 | 1338.2 | 1278.61 |
| tunit | volume-10k | 10000 | 18096.4 | 3602.2 | 3869.2 | 3667.4 | 3882.7 | 386.92 |
| tunit | parameterized | 5000 | 1408.1 | 2077.4 | 2119.3 | 2058.3 | 2123.7 | 423.85 |
| tunit | async-1k | 1000 | 1564.0 | 1304.7 | 1323.4 | 1304.7 | 1347.0 | 1323.36 |
| tunit | fixtures | 1000 | 1773.9 | 1290.4 | 1252.3 | 1228.9 | 1305.6 | 1252.30 |
| xunit | startup | 1 | 1600.6 | 875.6 | 887.0 | 878.8 | 906.9 | 887032.30 |
| xunit | volume-1k | 1000 | 1178.1 | 1203.4 | 1191.2 | 1131.5 | 1220.0 | 1191.16 |
| xunit | volume-10k | 10000 | 2356.1 | 5218.0 | 6755.2 | 5131.8 | 7126.7 | 675.52 |
| xunit | parameterized | 5000 | 1254.9 | 1966.2 | 2114.2 | 2052.0 | 2197.1 | 422.83 |
| xunit | async-1k | 1000 | 1648.0 | 2488.0 | 2234.1 | 2083.8 | 2558.0 | 2234.12 |
| xunit | fixtures | 1000 | 1383.4 | 1375.5 | 1299.7 | 1285.7 | 1356.2 | 1299.66 |

## Time-to-first-test, working set, allocations

Time-to-first-test is the earlier of the captured `BENCH first` line and the signal file's last-write time, measured from process start (capture end minus process runtime). A negative delta is 0. When only one signal exists, that one is used.
Peak bytes are the sampled sum of `VmRSS`. A launch shorter than the 15 ms sample can report 0.
Allocated bytes are the `BENCH allocated_bytes` probe delta.

| Host | Workload | TTFT cold ms | TTFT warm median ms | Cold peak | Warm peak | Cold allocated | Warm allocated median |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| jaribu-runfile | startup | 1515.8 | 1426.6 | 263,274,496 | 264,691,712 | 47,344 | 47,344 |
| jaribu-runfile | volume-1k | 1427.3 | 1472.4 | 319,029,248 | 319,070,208 | 73,840,296 | 73,840,296 |
| jaribu-runfile | volume-10k | 1823.7 | 1703.0 | 340,844,544 | 341,176,320 | 6,499,017,288 | 6,499,017,288 |
| jaribu-runfile | parameterized | 2384.9 | 1561.9 | 324,894,720 | 324,845,568 | 99,547,416 | 99,547,416 |
| jaribu-runfile | async-1k | 1414.2 | 1492.0 | 327,532,544 | 328,282,112 | 75,180,312 | 75,126,736 |
| jaribu-runfile | fixtures | 1510.5 | 1477.9 | 279,576,576 | 280,403,968 | 12,727,048 | 12,714,712 |
| jaribu-mtp | startup | 692.4 | 668.6 | 179,453,952 | 179,838,976 | 21,680 | 21,680 |
| jaribu-mtp | volume-1k | 657.8 | 682.0 | 237,608,960 | 237,932,544 | 76,560,112 | 76,555,608 |
| jaribu-mtp | volume-10k | 680.8 | 682.0 | 279,678,976 | 279,973,888 | 6,527,078,664 | 6,526,822,816 |
| jaribu-mtp | parameterized | 643.0 | 655.5 | 248,057,856 | 248,922,112 | 80,347,608 | 80,231,648 |
| jaribu-mtp | async-1k | 653.8 | 663.1 | 250,875,904 | 251,355,136 | 77,982,000 | 77,991,352 |
| jaribu-mtp | fixtures | 711.0 | 687.4 | 197,046,272 | 198,529,024 | 14,287,304 | 14,271,024 |
| fixie | startup | 631.0 | 630.1 | 177,950,720 | 178,028,544 | 4,960 | 4,960 |
| fixie | volume-1k | 624.4 | 611.0 | 180,654,080 | 181,465,088 | 1,197,800 | 1,210,040 |
| fixie | volume-10k | 664.4 | 718.8 | 198,664,192 | 203,096,064 | 11,785,320 | 11,782,888 |
| fixie | parameterized | 672.3 | 658.4 | 191,635,456 | 192,380,928 | 12,285,584 | 12,277,792 |
| fixie | async-1k | 665.4 | 707.1 | 191,508,480 | 191,512,576 | 1,653,952 | 1,653,976 |
| fixie | fixtures | 676.6 | 650.1 | 189,558,784 | 189,964,288 | 8,595,192 | 8,595,128 |
| tunit | startup | 784.7 | 814.0 | 193,634,304 | 197,410,816 | 2,558,680 | 2,558,920 |
| tunit | volume-1k | 895.5 | 918.4 | 238,718,976 | 239,886,336 | 19,247,912 | 19,292,936 |
| tunit | volume-10k | 1399.3 | 1515.3 | 456,560,640 | 508,686,336 | 180,985,792 | 179,311,168 |
| tunit | parameterized | 939.9 | 971.2 | 313,421,824 | 317,333,504 | 96,702,080 | 99,724,416 |
| tunit | async-1k | 872.7 | 905.4 | 242,954,240 | 244,695,040 | 19,947,736 | 19,991,240 |
| tunit | fixtures | 897.6 | 884.7 | 243,363,840 | 243,929,088 | 21,443,792 | 21,475,584 |
| xunit | startup | 793.8 | 801.4 | 195,346,432 | 195,428,352 | 61,448 | 62,328 |
| xunit | volume-1k | 840.1 | 825.6 | 250,023,936 | 252,440,576 | 59,006,328 | 59,006,672 |
| xunit | volume-10k | 1173.1 | 1389.1 | 327,692,288 | 328,531,968 | 1,309,179,256 | 1,309,288,120 |
| xunit | parameterized | 1030.0 | 1112.5 | 269,041,664 | 271,409,152 | 195,569,600 | 195,298,432 |
| xunit | async-1k | 954.3 | 1041.6 | 266,792,960 | 266,711,040 | 67,432,640 | 67,423,816 |
| xunit | fixtures | 1053.0 | 973.5 | 255,078,400 | 256,700,416 | 56,039,288 | 56,048,368 |

## Marginal per-test

`(warm volume-10k − warm volume-1k) / 9000` microseconds. Shared startup drops out when per-test cost is linear. A host whose cost grows with methods per class reports a class-size-dependent value, not a constant. A host needs both volume rows.

| Host | Volume-1k warm ms | Volume-10k warm ms | Marginal µs |
| --- | ---: | ---: | ---: |
| jaribu-runfile | 2018.0 | 18583.1 | 1840.57 |
| jaribu-mtp | 1240.8 | 16614.5 | 1708.19 |
| fixie | 719.3 | 1509.7 | 87.82 |
| tunit | 1278.6 | 3869.2 | 287.84 |
| xunit | 1191.2 | 6755.2 | 618.23 |

## Jaribu in-process

BenchmarkDotNet `ShortRunJob` and `MemoryDiagnoser` call `RunTestsAsync` with `NullSink`. Means are milliseconds.

| Benchmark | Mean ms | Allocated bytes |
| --- | ---: | ---: |
| JaribuRunnerBench.Startup: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3) | 0.005 | 4,688 |
| JaribuRunnerBench.Volume1K: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3) | 118.1 | 67,603,192 |
| JaribuRunnerBench.Volume10K: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3) | 5048.8 | 6,434,005,816 |
| JaribuRunnerBench.Parameterized: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3) | 89.2 | 62,563,332 |
| JaribuRunnerBench.Async1K: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3) | 113.8 | 68,832,172 |
| JaribuRunnerBench.Fixtures: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3) | 5.8 | 3,976,292 |

