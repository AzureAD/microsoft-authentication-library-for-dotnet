To run perf tests, install a tool like https://github.com/codesenberg/bombardier

bombardier -d 60s -l https://localhost:44355/FastCache
bombardier -d 60s -l https://localhost:44355/StaticDictionary
bombardier -d 60s -l https://localhost:44355/Singleton

Measurements taken on 4/27/2021

=========
SINGLETON
=========

bombardier -d 60s -l https://localhost:44355/Singleton

Bombarding https://localhost:44355/Singleton for 1m0s using 125 connection(s)
[==========================================================================================================================] 1m0s
Done!
Statistics        Avg      Stdev        Max
  Reqs/sec      5955.52     891.42    7150.21
  Latency       20.99ms     8.04ms   630.51ms
  Latency Distribution
     50%    17.00ms
     75%    18.00ms
     90%    20.00ms
     95%    69.00ms
     99%    83.00ms
  HTTP codes:
    1xx - 0, 2xx - 357399, 3xx - 0, 4xx - 0, 5xx - 0
    others - 0
  Throughput:     1.92MB/s



=================
Static Dictionary
=================

bombardier -d 60s -l https://localhost:44355/StaticDictionary

Bombarding https://localhost:44355/StaticDictionary for 1m0s using 125 connection(s)
[==========================================================================================================================] 1m0s
Done!
Statistics        Avg      Stdev        Max
  Reqs/sec     12157.45    4047.07   25269.74
  Latency       10.33ms     5.14ms   630.01ms
  Latency Distribution
     50%     9.00ms
     75%    10.52ms
     90%    14.00ms
     95%    17.00ms
     99%    68.00ms
  HTTP codes:
    1xx - 0, 2xx - 725479, 3xx - 0, 4xx - 0, 5xx - 0
    others - 0
  Throughput:     3.59MB/s


===============
static Flat cache 
===============

bombardier -d 60s -l https://localhost:44355/FlastCache

Bombarding https://localhost:44355/FlastCache for 1m0s using 125 connection(s)
[==========================================================================================================================] 1m0s

Statistics        Avg      Stdev        Max
  Reqs/sec       156.63     699.91   22015.41
  Latency         1.25s      1.10s     14.53s
  Latency Distribution
     50%      1.07s
     75%      1.55s
     90%      1.91s
     95%      2.52s
     99%      6.36s
  HTTP codes:
    1xx - 0, 2xx - 6068, 3xx - 0, 4xx - 0, 5xx - 0
    others - 0
  Throughput:    42.19KB/s

==================================
static Flat cache + fast AT save/search --> almost no diff
==================================


Done!
Statistics        Avg      Stdev        Max
  Reqs/sec       137.11     479.65   13331.11
  Latency         1.32s      1.86s     21.88s
  Latency Distribution
     50%      0.98s
     75%      1.38s
     90%      1.83s
     95%      2.35s
     99%     11.47s

