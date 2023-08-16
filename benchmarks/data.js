window.BENCHMARK_DATA = {
  "lastUpdate": 1692220100545,
  "repoUrl": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet",
  "entries": {
    "AcquireTokenNoCache": [
      {
        "commit": {
          "author": {
            "name": "Peter",
            "username": "pmaytak",
            "email": "34331512+pmaytak@users.noreply.github.com"
          },
          "committer": {
            "name": "GitHub",
            "username": "web-flow",
            "email": "noreply@github.com"
          },
          "id": "25ccce87cd6c3180c69b928319e19fbaac6bd8c0",
          "message": "Add Action to run performance benchmarks (#4285)\n\n* Pass console args to BDN.\r\n\r\n* Comment tests temporarily.\r\n\r\n* Add perf benchmark.\r\n\r\n* Update perf project to net6.0\r\n\r\n* Trigger action.\r\n\r\n* Fix path\r\n\r\n* Trigger.\r\n\r\n* Fix artifact path. Use .NET Core 3.1.\r\n\r\n* Comment out build path temporarily.\r\n\r\n* Install .NET.\r\n\r\n* Fix\r\n\r\n* Fix warnings.\r\n\r\n* Enable graphs.\r\n\r\n* Fix push gh pages\r\n\r\n* Fix graphs.\r\n\r\n* Added tests.\r\n\r\n* Add all tests to benchmark action. Remove (1, 1000) test case.\r\n\r\n* Fix test naming.\r\n\r\n* Update run command in ADO perf yml.\r\n\r\n* Change threshold.\r\n\r\n* Update path.\r\n\r\n* Temporary change.\r\n\r\n* Update yml, trigger on pull request, but don't publish the charts.\r\n\r\n* Test trigger.\r\n\r\n* Test.\r\n\r\n* Test3. Fix ADO perf yml.\r\n\r\n* Fix push GH pages.\r\n\r\n* Revert.\r\n\r\n* Revert ADO yml.\r\n\r\n* Update perf alert threshold.\r\n\r\n* Cleanup.\r\n\r\n* Try upload to GH pages.\r\n\r\n* Fix for testing.\r\n\r\n* Fix for test.\r\n\r\n* Fix for tests.\r\n\r\n* Revert.",
          "timestamp": "2023-08-10T06:33:34Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/25ccce87cd6c3180c69b928319e19fbaac6bd8c0"
        },
        "date": 1691650417193,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 522260.0833333333,
            "unit": "ns",
            "range": "± 66998.22841501767"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 716034.9195402298,
            "unit": "ns",
            "range": "± 72204.76327861866"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "name": "Peter",
            "username": "pmaytak",
            "email": "34331512+pmaytak@users.noreply.github.com"
          },
          "committer": {
            "name": "GitHub",
            "username": "web-flow",
            "email": "noreply@github.com"
          },
          "id": "25ccce87cd6c3180c69b928319e19fbaac6bd8c0",
          "message": "Add Action to run performance benchmarks (#4285)\n\n* Pass console args to BDN.\r\n\r\n* Comment tests temporarily.\r\n\r\n* Add perf benchmark.\r\n\r\n* Update perf project to net6.0\r\n\r\n* Trigger action.\r\n\r\n* Fix path\r\n\r\n* Trigger.\r\n\r\n* Fix artifact path. Use .NET Core 3.1.\r\n\r\n* Comment out build path temporarily.\r\n\r\n* Install .NET.\r\n\r\n* Fix\r\n\r\n* Fix warnings.\r\n\r\n* Enable graphs.\r\n\r\n* Fix push gh pages\r\n\r\n* Fix graphs.\r\n\r\n* Added tests.\r\n\r\n* Add all tests to benchmark action. Remove (1, 1000) test case.\r\n\r\n* Fix test naming.\r\n\r\n* Update run command in ADO perf yml.\r\n\r\n* Change threshold.\r\n\r\n* Update path.\r\n\r\n* Temporary change.\r\n\r\n* Update yml, trigger on pull request, but don't publish the charts.\r\n\r\n* Test trigger.\r\n\r\n* Test.\r\n\r\n* Test3. Fix ADO perf yml.\r\n\r\n* Fix push GH pages.\r\n\r\n* Revert.\r\n\r\n* Revert ADO yml.\r\n\r\n* Update perf alert threshold.\r\n\r\n* Cleanup.\r\n\r\n* Try upload to GH pages.\r\n\r\n* Fix for testing.\r\n\r\n* Fix for test.\r\n\r\n* Fix for tests.\r\n\r\n* Revert.",
          "timestamp": "2023-08-10T06:33:34Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/25ccce87cd6c3180c69b928319e19fbaac6bd8c0"
        },
        "date": 1691650417193,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 522260.0833333333,
            "unit": "ns",
            "range": "± 66998.22841501767"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 716034.9195402298,
            "unit": "ns",
            "range": "± 72204.76327861866"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "eltociear@gmail.com",
            "name": "Ikko Eltociear Ashimine",
            "username": "eltociear"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "fa7122b98df7cdeb78c55d1c3ef53065c35b980c",
          "message": "Fix typo in RSACng.cs (#4300)\n\nhte -> the",
          "timestamp": "2023-08-11T09:59:02-07:00",
          "tree_id": "abf88f9f30809e2d27cafd77b17e8cd1c23d23d6",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/fa7122b98df7cdeb78c55d1c3ef53065c35b980c"
        },
        "date": 1691773664891,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 398692.6041666667,
            "unit": "ns",
            "range": "± 15608.609608283885"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 542930.6,
            "unit": "ns",
            "range": "± 9409.630278147415"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "eltociear@gmail.com",
            "name": "Ikko Eltociear Ashimine",
            "username": "eltociear"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "fa7122b98df7cdeb78c55d1c3ef53065c35b980c",
          "message": "Fix typo in RSACng.cs (#4300)\n\nhte -> the",
          "timestamp": "2023-08-11T09:59:02-07:00",
          "tree_id": "abf88f9f30809e2d27cafd77b17e8cd1c23d23d6",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/fa7122b98df7cdeb78c55d1c3ef53065c35b980c"
        },
        "date": 1691773664891,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 398692.6041666667,
            "unit": "ns",
            "range": "± 15608.609608283885"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 542930.6,
            "unit": "ns",
            "range": "± 9409.630278147415"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "90415114+gladjohn@users.noreply.github.com",
            "name": "Gladwin Johnson",
            "username": "gladjohn"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "4e8dd12ead0138ff3826332bc40967d7966bae42",
          "message": "Fix Policheck issues (#4302)\n\nUpdate DefaultContractResolver.cs",
          "timestamp": "2023-08-16T13:59:03-07:00",
          "tree_id": "87e16a83853dd1200678c5b76a27e1c6fe342eb9",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/4e8dd12ead0138ff3826332bc40967d7966bae42"
        },
        "date": 1692220089864,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 494907.8658536585,
            "unit": "ns",
            "range": "± 67120.30283995642"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 631160.3369565217,
            "unit": "ns",
            "range": "± 51387.2319608626"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "90415114+gladjohn@users.noreply.github.com",
            "name": "Gladwin Johnson",
            "username": "gladjohn"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "4e8dd12ead0138ff3826332bc40967d7966bae42",
          "message": "Fix Policheck issues (#4302)\n\nUpdate DefaultContractResolver.cs",
          "timestamp": "2023-08-16T13:59:03-07:00",
          "tree_id": "87e16a83853dd1200678c5b76a27e1c6fe342eb9",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/4e8dd12ead0138ff3826332bc40967d7966bae42"
        },
        "date": 1692220089864,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 494907.8658536585,
            "unit": "ns",
            "range": "± 67120.30283995642"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 631160.3369565217,
            "unit": "ns",
            "range": "± 51387.2319608626"
          }
        ]
      }
    ],
    "AcquireTokenForClientWithCache": [
      {
        "commit": {
          "author": {
            "name": "Peter",
            "username": "pmaytak",
            "email": "34331512+pmaytak@users.noreply.github.com"
          },
          "committer": {
            "name": "GitHub",
            "username": "web-flow",
            "email": "noreply@github.com"
          },
          "id": "25ccce87cd6c3180c69b928319e19fbaac6bd8c0",
          "message": "Add Action to run performance benchmarks (#4285)\n\n* Pass console args to BDN.\r\n\r\n* Comment tests temporarily.\r\n\r\n* Add perf benchmark.\r\n\r\n* Update perf project to net6.0\r\n\r\n* Trigger action.\r\n\r\n* Fix path\r\n\r\n* Trigger.\r\n\r\n* Fix artifact path. Use .NET Core 3.1.\r\n\r\n* Comment out build path temporarily.\r\n\r\n* Install .NET.\r\n\r\n* Fix\r\n\r\n* Fix warnings.\r\n\r\n* Enable graphs.\r\n\r\n* Fix push gh pages\r\n\r\n* Fix graphs.\r\n\r\n* Added tests.\r\n\r\n* Add all tests to benchmark action. Remove (1, 1000) test case.\r\n\r\n* Fix test naming.\r\n\r\n* Update run command in ADO perf yml.\r\n\r\n* Change threshold.\r\n\r\n* Update path.\r\n\r\n* Temporary change.\r\n\r\n* Update yml, trigger on pull request, but don't publish the charts.\r\n\r\n* Test trigger.\r\n\r\n* Test.\r\n\r\n* Test3. Fix ADO perf yml.\r\n\r\n* Fix push GH pages.\r\n\r\n* Revert.\r\n\r\n* Revert ADO yml.\r\n\r\n* Update perf alert threshold.\r\n\r\n* Cleanup.\r\n\r\n* Try upload to GH pages.\r\n\r\n* Fix for testing.\r\n\r\n* Fix for test.\r\n\r\n* Fix for tests.\r\n\r\n* Revert.",
          "timestamp": "2023-08-10T06:33:34Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/25ccce87cd6c3180c69b928319e19fbaac6bd8c0"
        },
        "date": 1691650426602,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 29728.440393692406,
            "unit": "ns",
            "range": "± 1025.9075783982976"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 346861.1917169744,
            "unit": "ns",
            "range": "± 8358.673335373376"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 64349.311694335935,
            "unit": "ns",
            "range": "± 741.6143612712115"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 357303.32398365164,
            "unit": "ns",
            "range": "± 9102.881670346229"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "name": "Peter",
            "username": "pmaytak",
            "email": "34331512+pmaytak@users.noreply.github.com"
          },
          "committer": {
            "name": "GitHub",
            "username": "web-flow",
            "email": "noreply@github.com"
          },
          "id": "25ccce87cd6c3180c69b928319e19fbaac6bd8c0",
          "message": "Add Action to run performance benchmarks (#4285)\n\n* Pass console args to BDN.\r\n\r\n* Comment tests temporarily.\r\n\r\n* Add perf benchmark.\r\n\r\n* Update perf project to net6.0\r\n\r\n* Trigger action.\r\n\r\n* Fix path\r\n\r\n* Trigger.\r\n\r\n* Fix artifact path. Use .NET Core 3.1.\r\n\r\n* Comment out build path temporarily.\r\n\r\n* Install .NET.\r\n\r\n* Fix\r\n\r\n* Fix warnings.\r\n\r\n* Enable graphs.\r\n\r\n* Fix push gh pages\r\n\r\n* Fix graphs.\r\n\r\n* Added tests.\r\n\r\n* Add all tests to benchmark action. Remove (1, 1000) test case.\r\n\r\n* Fix test naming.\r\n\r\n* Update run command in ADO perf yml.\r\n\r\n* Change threshold.\r\n\r\n* Update path.\r\n\r\n* Temporary change.\r\n\r\n* Update yml, trigger on pull request, but don't publish the charts.\r\n\r\n* Test trigger.\r\n\r\n* Test.\r\n\r\n* Test3. Fix ADO perf yml.\r\n\r\n* Fix push GH pages.\r\n\r\n* Revert.\r\n\r\n* Revert ADO yml.\r\n\r\n* Update perf alert threshold.\r\n\r\n* Cleanup.\r\n\r\n* Try upload to GH pages.\r\n\r\n* Fix for testing.\r\n\r\n* Fix for test.\r\n\r\n* Fix for tests.\r\n\r\n* Revert.",
          "timestamp": "2023-08-10T06:33:34Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/25ccce87cd6c3180c69b928319e19fbaac6bd8c0"
        },
        "date": 1691650426602,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 29728.440393692406,
            "unit": "ns",
            "range": "± 1025.9075783982976"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 346861.1917169744,
            "unit": "ns",
            "range": "± 8358.673335373376"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 64349.311694335935,
            "unit": "ns",
            "range": "± 741.6143612712115"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 357303.32398365164,
            "unit": "ns",
            "range": "± 9102.881670346229"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "eltociear@gmail.com",
            "name": "Ikko Eltociear Ashimine",
            "username": "eltociear"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "fa7122b98df7cdeb78c55d1c3ef53065c35b980c",
          "message": "Fix typo in RSACng.cs (#4300)\n\nhte -> the",
          "timestamp": "2023-08-11T09:59:02-07:00",
          "tree_id": "abf88f9f30809e2d27cafd77b17e8cd1c23d23d6",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/fa7122b98df7cdeb78c55d1c3ef53065c35b980c"
        },
        "date": 1691773671378,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 22508.050279889787,
            "unit": "ns",
            "range": "± 132.8391205417984"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 265218.91585286456,
            "unit": "ns",
            "range": "± 1626.4754630091277"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 49249.52138671875,
            "unit": "ns",
            "range": "± 184.03301572960368"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 268278.702078683,
            "unit": "ns",
            "range": "± 2048.694192996991"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "eltociear@gmail.com",
            "name": "Ikko Eltociear Ashimine",
            "username": "eltociear"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "fa7122b98df7cdeb78c55d1c3ef53065c35b980c",
          "message": "Fix typo in RSACng.cs (#4300)\n\nhte -> the",
          "timestamp": "2023-08-11T09:59:02-07:00",
          "tree_id": "abf88f9f30809e2d27cafd77b17e8cd1c23d23d6",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/fa7122b98df7cdeb78c55d1c3ef53065c35b980c"
        },
        "date": 1691773671378,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 22508.050279889787,
            "unit": "ns",
            "range": "± 132.8391205417984"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 265218.91585286456,
            "unit": "ns",
            "range": "± 1626.4754630091277"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 49249.52138671875,
            "unit": "ns",
            "range": "± 184.03301572960368"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 268278.702078683,
            "unit": "ns",
            "range": "± 2048.694192996991"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "90415114+gladjohn@users.noreply.github.com",
            "name": "Gladwin Johnson",
            "username": "gladjohn"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "4e8dd12ead0138ff3826332bc40967d7966bae42",
          "message": "Fix Policheck issues (#4302)\n\nUpdate DefaultContractResolver.cs",
          "timestamp": "2023-08-16T13:59:03-07:00",
          "tree_id": "87e16a83853dd1200678c5b76a27e1c6fe342eb9",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/4e8dd12ead0138ff3826332bc40967d7966bae42"
        },
        "date": 1692220097759,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 27994.87970987956,
            "unit": "ns",
            "range": "± 303.33909108434017"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 277520.9061279297,
            "unit": "ns",
            "range": "± 5278.374201038014"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 56322.26717529297,
            "unit": "ns",
            "range": "± 734.4777838567768"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 280801.8371582031,
            "unit": "ns",
            "range": "± 5135.708326816203"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "90415114+gladjohn@users.noreply.github.com",
            "name": "Gladwin Johnson",
            "username": "gladjohn"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "4e8dd12ead0138ff3826332bc40967d7966bae42",
          "message": "Fix Policheck issues (#4302)\n\nUpdate DefaultContractResolver.cs",
          "timestamp": "2023-08-16T13:59:03-07:00",
          "tree_id": "87e16a83853dd1200678c5b76a27e1c6fe342eb9",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/4e8dd12ead0138ff3826332bc40967d7966bae42"
        },
        "date": 1692220097759,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 27994.87970987956,
            "unit": "ns",
            "range": "± 303.33909108434017"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 277520.9061279297,
            "unit": "ns",
            "range": "± 5278.374201038014"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 56322.26717529297,
            "unit": "ns",
            "range": "± 734.4777838567768"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 280801.8371582031,
            "unit": "ns",
            "range": "± 5135.708326816203"
          }
        ]
      }
    ],
    "AcquireTokenForOboWithCache": [
      {
        "commit": {
          "author": {
            "name": "Peter",
            "username": "pmaytak",
            "email": "34331512+pmaytak@users.noreply.github.com"
          },
          "committer": {
            "name": "GitHub",
            "username": "web-flow",
            "email": "noreply@github.com"
          },
          "id": "25ccce87cd6c3180c69b928319e19fbaac6bd8c0",
          "message": "Add Action to run performance benchmarks (#4285)\n\n* Pass console args to BDN.\r\n\r\n* Comment tests temporarily.\r\n\r\n* Add perf benchmark.\r\n\r\n* Update perf project to net6.0\r\n\r\n* Trigger action.\r\n\r\n* Fix path\r\n\r\n* Trigger.\r\n\r\n* Fix artifact path. Use .NET Core 3.1.\r\n\r\n* Comment out build path temporarily.\r\n\r\n* Install .NET.\r\n\r\n* Fix\r\n\r\n* Fix warnings.\r\n\r\n* Enable graphs.\r\n\r\n* Fix push gh pages\r\n\r\n* Fix graphs.\r\n\r\n* Added tests.\r\n\r\n* Add all tests to benchmark action. Remove (1, 1000) test case.\r\n\r\n* Fix test naming.\r\n\r\n* Update run command in ADO perf yml.\r\n\r\n* Change threshold.\r\n\r\n* Update path.\r\n\r\n* Temporary change.\r\n\r\n* Update yml, trigger on pull request, but don't publish the charts.\r\n\r\n* Test trigger.\r\n\r\n* Test.\r\n\r\n* Test3. Fix ADO perf yml.\r\n\r\n* Fix push GH pages.\r\n\r\n* Revert.\r\n\r\n* Revert ADO yml.\r\n\r\n* Update perf alert threshold.\r\n\r\n* Cleanup.\r\n\r\n* Try upload to GH pages.\r\n\r\n* Fix for testing.\r\n\r\n* Fix for test.\r\n\r\n* Fix for tests.\r\n\r\n* Revert.",
          "timestamp": "2023-08-10T06:33:34Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/25ccce87cd6c3180c69b928319e19fbaac6bd8c0"
        },
        "date": 1691650432218,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 58087.2845953864,
            "unit": "ns",
            "range": "± 1927.0434850089252"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 495827.5238606771,
            "unit": "ns",
            "range": "± 13506.978953199843"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 91389.71277436757,
            "unit": "ns",
            "range": "± 2152.6747913865656"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 497071.8078613281,
            "unit": "ns",
            "range": "± 8747.863709234984"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "name": "Peter",
            "username": "pmaytak",
            "email": "34331512+pmaytak@users.noreply.github.com"
          },
          "committer": {
            "name": "GitHub",
            "username": "web-flow",
            "email": "noreply@github.com"
          },
          "id": "25ccce87cd6c3180c69b928319e19fbaac6bd8c0",
          "message": "Add Action to run performance benchmarks (#4285)\n\n* Pass console args to BDN.\r\n\r\n* Comment tests temporarily.\r\n\r\n* Add perf benchmark.\r\n\r\n* Update perf project to net6.0\r\n\r\n* Trigger action.\r\n\r\n* Fix path\r\n\r\n* Trigger.\r\n\r\n* Fix artifact path. Use .NET Core 3.1.\r\n\r\n* Comment out build path temporarily.\r\n\r\n* Install .NET.\r\n\r\n* Fix\r\n\r\n* Fix warnings.\r\n\r\n* Enable graphs.\r\n\r\n* Fix push gh pages\r\n\r\n* Fix graphs.\r\n\r\n* Added tests.\r\n\r\n* Add all tests to benchmark action. Remove (1, 1000) test case.\r\n\r\n* Fix test naming.\r\n\r\n* Update run command in ADO perf yml.\r\n\r\n* Change threshold.\r\n\r\n* Update path.\r\n\r\n* Temporary change.\r\n\r\n* Update yml, trigger on pull request, but don't publish the charts.\r\n\r\n* Test trigger.\r\n\r\n* Test.\r\n\r\n* Test3. Fix ADO perf yml.\r\n\r\n* Fix push GH pages.\r\n\r\n* Revert.\r\n\r\n* Revert ADO yml.\r\n\r\n* Update perf alert threshold.\r\n\r\n* Cleanup.\r\n\r\n* Try upload to GH pages.\r\n\r\n* Fix for testing.\r\n\r\n* Fix for test.\r\n\r\n* Fix for tests.\r\n\r\n* Revert.",
          "timestamp": "2023-08-10T06:33:34Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/25ccce87cd6c3180c69b928319e19fbaac6bd8c0"
        },
        "date": 1691650432218,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 58087.2845953864,
            "unit": "ns",
            "range": "± 1927.0434850089252"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 495827.5238606771,
            "unit": "ns",
            "range": "± 13506.978953199843"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 91389.71277436757,
            "unit": "ns",
            "range": "± 2152.6747913865656"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 497071.8078613281,
            "unit": "ns",
            "range": "± 8747.863709234984"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "eltociear@gmail.com",
            "name": "Ikko Eltociear Ashimine",
            "username": "eltociear"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "fa7122b98df7cdeb78c55d1c3ef53065c35b980c",
          "message": "Fix typo in RSACng.cs (#4300)\n\nhte -> the",
          "timestamp": "2023-08-11T09:59:02-07:00",
          "tree_id": "abf88f9f30809e2d27cafd77b17e8cd1c23d23d6",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/fa7122b98df7cdeb78c55d1c3ef53065c35b980c"
        },
        "date": 1691773674445,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 41063.032885742185,
            "unit": "ns",
            "range": "± 427.9720066469242"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 377159.05810546875,
            "unit": "ns",
            "range": "± 4000.5040313464488"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 66783.44939716045,
            "unit": "ns",
            "range": "± 198.4996000988459"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 378692.4243815104,
            "unit": "ns",
            "range": "± 5019.930735235058"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "eltociear@gmail.com",
            "name": "Ikko Eltociear Ashimine",
            "username": "eltociear"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "fa7122b98df7cdeb78c55d1c3ef53065c35b980c",
          "message": "Fix typo in RSACng.cs (#4300)\n\nhte -> the",
          "timestamp": "2023-08-11T09:59:02-07:00",
          "tree_id": "abf88f9f30809e2d27cafd77b17e8cd1c23d23d6",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/fa7122b98df7cdeb78c55d1c3ef53065c35b980c"
        },
        "date": 1691773674445,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 41063.032885742185,
            "unit": "ns",
            "range": "± 427.9720066469242"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 377159.05810546875,
            "unit": "ns",
            "range": "± 4000.5040313464488"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 66783.44939716045,
            "unit": "ns",
            "range": "± 198.4996000988459"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 378692.4243815104,
            "unit": "ns",
            "range": "± 5019.930735235058"
          }
        ]
      }
    ],
    "TokenCacheTestsWithCache": [
      {
        "commit": {
          "author": {
            "name": "Peter",
            "username": "pmaytak",
            "email": "34331512+pmaytak@users.noreply.github.com"
          },
          "committer": {
            "name": "GitHub",
            "username": "web-flow",
            "email": "noreply@github.com"
          },
          "id": "25ccce87cd6c3180c69b928319e19fbaac6bd8c0",
          "message": "Add Action to run performance benchmarks (#4285)\n\n* Pass console args to BDN.\r\n\r\n* Comment tests temporarily.\r\n\r\n* Add perf benchmark.\r\n\r\n* Update perf project to net6.0\r\n\r\n* Trigger action.\r\n\r\n* Fix path\r\n\r\n* Trigger.\r\n\r\n* Fix artifact path. Use .NET Core 3.1.\r\n\r\n* Comment out build path temporarily.\r\n\r\n* Install .NET.\r\n\r\n* Fix\r\n\r\n* Fix warnings.\r\n\r\n* Enable graphs.\r\n\r\n* Fix push gh pages\r\n\r\n* Fix graphs.\r\n\r\n* Added tests.\r\n\r\n* Add all tests to benchmark action. Remove (1, 1000) test case.\r\n\r\n* Fix test naming.\r\n\r\n* Update run command in ADO perf yml.\r\n\r\n* Change threshold.\r\n\r\n* Update path.\r\n\r\n* Temporary change.\r\n\r\n* Update yml, trigger on pull request, but don't publish the charts.\r\n\r\n* Test trigger.\r\n\r\n* Test.\r\n\r\n* Test3. Fix ADO perf yml.\r\n\r\n* Fix push GH pages.\r\n\r\n* Revert.\r\n\r\n* Revert ADO yml.\r\n\r\n* Update perf alert threshold.\r\n\r\n* Cleanup.\r\n\r\n* Try upload to GH pages.\r\n\r\n* Fix for testing.\r\n\r\n* Fix for test.\r\n\r\n* Fix for tests.\r\n\r\n* Revert.",
          "timestamp": "2023-08-10T06:33:34Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/25ccce87cd6c3180c69b928319e19fbaac6bd8c0"
        },
        "date": 1691650437502,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 43008.634852359166,
            "unit": "ns",
            "range": "± 946.4199542815185"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 76727.95791625977,
            "unit": "ns",
            "range": "± 1728.1346239891113"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 21965.44908590878,
            "unit": "ns",
            "range": "± 432.99555078629544"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 56749.237837357956,
            "unit": "ns",
            "range": "± 1780.3280866451792"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 77688.48809523809,
            "unit": "ns",
            "range": "± 16808.45595461314"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 267422.56666666665,
            "unit": "ns",
            "range": "± 102130.44546004043"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "name": "Peter",
            "username": "pmaytak",
            "email": "34331512+pmaytak@users.noreply.github.com"
          },
          "committer": {
            "name": "GitHub",
            "username": "web-flow",
            "email": "noreply@github.com"
          },
          "id": "25ccce87cd6c3180c69b928319e19fbaac6bd8c0",
          "message": "Add Action to run performance benchmarks (#4285)\n\n* Pass console args to BDN.\r\n\r\n* Comment tests temporarily.\r\n\r\n* Add perf benchmark.\r\n\r\n* Update perf project to net6.0\r\n\r\n* Trigger action.\r\n\r\n* Fix path\r\n\r\n* Trigger.\r\n\r\n* Fix artifact path. Use .NET Core 3.1.\r\n\r\n* Comment out build path temporarily.\r\n\r\n* Install .NET.\r\n\r\n* Fix\r\n\r\n* Fix warnings.\r\n\r\n* Enable graphs.\r\n\r\n* Fix push gh pages\r\n\r\n* Fix graphs.\r\n\r\n* Added tests.\r\n\r\n* Add all tests to benchmark action. Remove (1, 1000) test case.\r\n\r\n* Fix test naming.\r\n\r\n* Update run command in ADO perf yml.\r\n\r\n* Change threshold.\r\n\r\n* Update path.\r\n\r\n* Temporary change.\r\n\r\n* Update yml, trigger on pull request, but don't publish the charts.\r\n\r\n* Test trigger.\r\n\r\n* Test.\r\n\r\n* Test3. Fix ADO perf yml.\r\n\r\n* Fix push GH pages.\r\n\r\n* Revert.\r\n\r\n* Revert ADO yml.\r\n\r\n* Update perf alert threshold.\r\n\r\n* Cleanup.\r\n\r\n* Try upload to GH pages.\r\n\r\n* Fix for testing.\r\n\r\n* Fix for test.\r\n\r\n* Fix for tests.\r\n\r\n* Revert.",
          "timestamp": "2023-08-10T06:33:34Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/25ccce87cd6c3180c69b928319e19fbaac6bd8c0"
        },
        "date": 1691650437502,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 43008.634852359166,
            "unit": "ns",
            "range": "± 946.4199542815185"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 76727.95791625977,
            "unit": "ns",
            "range": "± 1728.1346239891113"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 21965.44908590878,
            "unit": "ns",
            "range": "± 432.99555078629544"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 56749.237837357956,
            "unit": "ns",
            "range": "± 1780.3280866451792"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 77688.48809523809,
            "unit": "ns",
            "range": "± 16808.45595461314"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 267422.56666666665,
            "unit": "ns",
            "range": "± 102130.44546004043"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "eltociear@gmail.com",
            "name": "Ikko Eltociear Ashimine",
            "username": "eltociear"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "fa7122b98df7cdeb78c55d1c3ef53065c35b980c",
          "message": "Fix typo in RSACng.cs (#4300)\n\nhte -> the",
          "timestamp": "2023-08-11T09:59:02-07:00",
          "tree_id": "abf88f9f30809e2d27cafd77b17e8cd1c23d23d6",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/fa7122b98df7cdeb78c55d1c3ef53065c35b980c"
        },
        "date": 1691773677663,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 29938.15935872396,
            "unit": "ns",
            "range": "± 282.31440701943103"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 58485.23103114537,
            "unit": "ns",
            "range": "± 330.09283215204044"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 16204.13232014974,
            "unit": "ns",
            "range": "± 211.26830270048882"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 43885.758344862195,
            "unit": "ns",
            "range": "± 1458.0784230160048"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 57771.857142857145,
            "unit": "ns",
            "range": "± 551.2166762497523"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 244919.8085106383,
            "unit": "ns",
            "range": "± 78172.30201736496"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "eltociear@gmail.com",
            "name": "Ikko Eltociear Ashimine",
            "username": "eltociear"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "fa7122b98df7cdeb78c55d1c3ef53065c35b980c",
          "message": "Fix typo in RSACng.cs (#4300)\n\nhte -> the",
          "timestamp": "2023-08-11T09:59:02-07:00",
          "tree_id": "abf88f9f30809e2d27cafd77b17e8cd1c23d23d6",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/fa7122b98df7cdeb78c55d1c3ef53065c35b980c"
        },
        "date": 1691773677663,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 29938.15935872396,
            "unit": "ns",
            "range": "± 282.31440701943103"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 58485.23103114537,
            "unit": "ns",
            "range": "± 330.09283215204044"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 16204.13232014974,
            "unit": "ns",
            "range": "± 211.26830270048882"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 43885.758344862195,
            "unit": "ns",
            "range": "± 1458.0784230160048"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 57771.857142857145,
            "unit": "ns",
            "range": "± 551.2166762497523"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 244919.8085106383,
            "unit": "ns",
            "range": "± 78172.30201736496"
          }
        ]
      }
    ]
  }
}