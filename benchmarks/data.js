window.BENCHMARK_DATA = {
  "lastUpdate": 1693420851895,
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
      },
      {
        "commit": {
          "author": {
            "name": "Neha Bhargava",
            "username": "neha-bhargava",
            "email": "61847233+neha-bhargava@users.noreply.github.com"
          },
          "committer": {
            "name": "GitHub",
            "username": "web-flow",
            "email": "noreply@github.com"
          },
          "id": "29de3eae8f07741bab1460afba13a4afdc8288c6",
          "message": "Merge branch 'main' into nebharg/openTelemetry",
          "timestamp": "2023-08-19T01:10:22Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/29de3eae8f07741bab1460afba13a4afdc8288c6"
        },
        "date": 1692408312118,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 416665.4646464646,
            "unit": "ns",
            "range": "± 30183.283152830492"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 658685.6,
            "unit": "ns",
            "range": "± 90486.9176668685"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "name": "Neha Bhargava",
            "username": "neha-bhargava",
            "email": "61847233+neha-bhargava@users.noreply.github.com"
          },
          "committer": {
            "name": "GitHub",
            "username": "web-flow",
            "email": "noreply@github.com"
          },
          "id": "29de3eae8f07741bab1460afba13a4afdc8288c6",
          "message": "Merge branch 'main' into nebharg/openTelemetry",
          "timestamp": "2023-08-19T01:10:22Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/29de3eae8f07741bab1460afba13a4afdc8288c6"
        },
        "date": 1692408312118,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 416665.4646464646,
            "unit": "ns",
            "range": "± 30183.283152830492"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 658685.6,
            "unit": "ns",
            "range": "± 90486.9176668685"
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
          "id": "80c848b5b7bfc033b11ee82107344bfc22efc0da",
          "message": "Add perf links to README.md (#4306)\n\nUpdate README.md",
          "timestamp": "2023-08-21T09:29:55Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/80c848b5b7bfc033b11ee82107344bfc22efc0da"
        },
        "date": 1692903119328,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 562021.425,
            "unit": "ns",
            "range": "± 83493.61240447522"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 934215.213483146,
            "unit": "ns",
            "range": "± 246422.65030290172"
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
          "id": "80c848b5b7bfc033b11ee82107344bfc22efc0da",
          "message": "Add perf links to README.md (#4306)\n\nUpdate README.md",
          "timestamp": "2023-08-21T09:29:55Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/80c848b5b7bfc033b11ee82107344bfc22efc0da"
        },
        "date": 1692903741768,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 401700.7301587302,
            "unit": "ns",
            "range": "± 18420.676293324046"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 544089.56,
            "unit": "ns",
            "range": "± 14043.553927810914"
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
          "id": "80c848b5b7bfc033b11ee82107344bfc22efc0da",
          "message": "Add perf links to README.md (#4306)\n\nUpdate README.md",
          "timestamp": "2023-08-21T09:29:55Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/80c848b5b7bfc033b11ee82107344bfc22efc0da"
        },
        "date": 1692903741768,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 401700.7301587302,
            "unit": "ns",
            "range": "± 18420.676293324046"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 544089.56,
            "unit": "ns",
            "range": "± 14043.553927810914"
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
          "id": "3d6b84e5db422a64dc67fb08ecade35daf5a42eb",
          "message": "Update ADO yaml",
          "timestamp": "2023-08-25T06:41:18Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/3d6b84e5db422a64dc67fb08ecade35daf5a42eb"
        },
        "date": 1692946148839,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 388799.91071428574,
            "unit": "ns",
            "range": "± 16556.392488348625"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 530896.0625,
            "unit": "ns",
            "range": "± 10413.76025886743"
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
          "id": "3d6b84e5db422a64dc67fb08ecade35daf5a42eb",
          "message": "Update ADO yaml",
          "timestamp": "2023-08-25T06:41:18Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/3d6b84e5db422a64dc67fb08ecade35daf5a42eb"
        },
        "date": 1692946148839,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 388799.91071428574,
            "unit": "ns",
            "range": "± 16556.392488348625"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 530896.0625,
            "unit": "ns",
            "range": "± 10413.76025886743"
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
          "id": "c6f4a21248aa02b829b0befdc73e9f48a03cc2d1",
          "message": "Update pipeline-perf-tests-automation.yaml",
          "timestamp": "2023-08-25T06:49:23Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/c6f4a21248aa02b829b0befdc73e9f48a03cc2d1"
        },
        "date": 1692947354533,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 400856.3469387755,
            "unit": "ns",
            "range": "± 15762.626361575214"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 526049.619047619,
            "unit": "ns",
            "range": "± 12247.310686335146"
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
          "id": "c6f4a21248aa02b829b0befdc73e9f48a03cc2d1",
          "message": "Update pipeline-perf-tests-automation.yaml",
          "timestamp": "2023-08-25T06:49:23Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/c6f4a21248aa02b829b0befdc73e9f48a03cc2d1"
        },
        "date": 1692947354533,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 400856.3469387755,
            "unit": "ns",
            "range": "± 15762.626361575214"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 526049.619047619,
            "unit": "ns",
            "range": "± 12247.310686335146"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "bogavril@microsoft.com",
            "name": "Bogdan Gavril",
            "username": "bgavrilMS"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "208aa5896d0c579b8a3dcb8aca7586f4784619a4",
          "message": "Move the MSAL desktop cache extension + tests (#4089)\n\n* Move the MSAL desktop cache extension + tests. No build.\r\n\r\n* Test fix\r\n\r\n* Move the MSAL desktop cache extension + tests. No build.\r\n\r\n* Test fix\r\n\r\n* Test fix\r\n\r\n* fix test\r\n\r\n* Build changes\r\n\r\n* Try net6\r\n\r\n* fix\r\n\r\n* fix\r\n\r\n* fix\r\n\r\n* fix\r\n\r\n* Address PR comments",
          "timestamp": "2023-08-28T11:03:22Z",
          "tree_id": "9687fc5c27879f21edb7c1e306eb952c455ff04a",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/208aa5896d0c579b8a3dcb8aca7586f4784619a4"
        },
        "date": 1693221144047,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 509074.6210526316,
            "unit": "ns",
            "range": "± 50297.46669293805"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 756752.6086956522,
            "unit": "ns",
            "range": "± 270247.2054130024"
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
          "id": "3e133830f074ae020d5ae9b2a7dd4022fdd85415",
          "message": "concurrent token refresh fix for managed identity and app token provider (cc) (#4309)\n\n* mi\r\n\r\n* app token\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* pr comments\r\n\r\n* pr comments\r\n\r\n* pr comments\r\n\r\n* verbose logging\r\n\r\n* pr comments\r\n\r\n* Update src/client/Microsoft.Identity.Client/Internal/Requests/ManagedIdentityAuthRequest.cs\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* mi fix\r\n\r\n* few more edits\r\n\r\n* app token provider\r\n\r\n* edits\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* pr comments\r\n\r\n* pr comments\r\n\r\n* ProactivelyRefreshed\r\n\r\n* move around\r\n\r\n---------\r\n\r\nCo-authored-by: Gladwin Johnson <gljohns@microsoft.com>\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>",
          "timestamp": "2023-08-29T15:57:07-07:00",
          "tree_id": "ae680d731353b083290e572c6f6edebe79abc00a",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/3e133830f074ae020d5ae9b2a7dd4022fdd85415"
        },
        "date": 1693350370744,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 475785.82608695654,
            "unit": "ns",
            "range": "± 18241.625981747653"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 645087.2173913043,
            "unit": "ns",
            "range": "± 15899.797196981424"
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
          "id": "3e133830f074ae020d5ae9b2a7dd4022fdd85415",
          "message": "concurrent token refresh fix for managed identity and app token provider (cc) (#4309)\n\n* mi\r\n\r\n* app token\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* pr comments\r\n\r\n* pr comments\r\n\r\n* pr comments\r\n\r\n* verbose logging\r\n\r\n* pr comments\r\n\r\n* Update src/client/Microsoft.Identity.Client/Internal/Requests/ManagedIdentityAuthRequest.cs\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* mi fix\r\n\r\n* few more edits\r\n\r\n* app token provider\r\n\r\n* edits\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* pr comments\r\n\r\n* pr comments\r\n\r\n* ProactivelyRefreshed\r\n\r\n* move around\r\n\r\n---------\r\n\r\nCo-authored-by: Gladwin Johnson <gljohns@microsoft.com>\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>",
          "timestamp": "2023-08-29T15:57:07-07:00",
          "tree_id": "ae680d731353b083290e572c6f6edebe79abc00a",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/3e133830f074ae020d5ae9b2a7dd4022fdd85415"
        },
        "date": 1693350370744,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 475785.82608695654,
            "unit": "ns",
            "range": "± 18241.625981747653"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 645087.2173913043,
            "unit": "ns",
            "range": "± 15899.797196981424"
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
          "id": "0ea350f977601b34c4d7c804db0c2d4de44d1404",
          "message": "Update logging for Runtime Broker (#4318)\n\nUpdate WamAdapters.cs",
          "timestamp": "2023-08-30T05:09:14-07:00",
          "tree_id": "235ada87308558bc551ac67445bf69859dd3787c",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/0ea350f977601b34c4d7c804db0c2d4de44d1404"
        },
        "date": 1693397868507,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 407319.52,
            "unit": "ns",
            "range": "± 16257.540404679788"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 546144.8,
            "unit": "ns",
            "range": "± 12311.541703450039"
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
          "id": "0ea350f977601b34c4d7c804db0c2d4de44d1404",
          "message": "Update logging for Runtime Broker (#4318)\n\nUpdate WamAdapters.cs",
          "timestamp": "2023-08-30T05:09:14-07:00",
          "tree_id": "235ada87308558bc551ac67445bf69859dd3787c",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/0ea350f977601b34c4d7c804db0c2d4de44d1404"
        },
        "date": 1693397868507,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 407319.52,
            "unit": "ns",
            "range": "± 16257.540404679788"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 546144.8,
            "unit": "ns",
            "range": "± 12311.541703450039"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "name": "Gladwin Johnson",
            "username": "gladjohn",
            "email": "90415114+gladjohn@users.noreply.github.com"
          },
          "committer": {
            "name": "GitHub",
            "username": "web-flow",
            "email": "noreply@github.com"
          },
          "id": "0ea350f977601b34c4d7c804db0c2d4de44d1404",
          "message": "Update logging for Runtime Broker (#4318)\n\nUpdate WamAdapters.cs",
          "timestamp": "2023-08-30T12:09:14Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/0ea350f977601b34c4d7c804db0c2d4de44d1404"
        },
        "date": 1693420179720,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 378830.6666666667,
            "unit": "ns",
            "range": "± 16942.760117827806"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 539826.7857142857,
            "unit": "ns",
            "range": "± 9171.770244183394"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "name": "Gladwin Johnson",
            "username": "gladjohn",
            "email": "90415114+gladjohn@users.noreply.github.com"
          },
          "committer": {
            "name": "GitHub",
            "username": "web-flow",
            "email": "noreply@github.com"
          },
          "id": "0ea350f977601b34c4d7c804db0c2d4de44d1404",
          "message": "Update logging for Runtime Broker (#4318)\n\nUpdate WamAdapters.cs",
          "timestamp": "2023-08-30T12:09:14Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/0ea350f977601b34c4d7c804db0c2d4de44d1404"
        },
        "date": 1693420179720,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 378830.6666666667,
            "unit": "ns",
            "range": "± 16942.760117827806"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 539826.7857142857,
            "unit": "ns",
            "range": "± 9171.770244183394"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "34331512+pmaytak@users.noreply.github.com",
            "name": "Peter",
            "username": "pmaytak"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "d25f45f7e356bd92986e412dd68b5d341343aa6a",
          "message": "Set CacheSynchronization to true by default (#4310)\n\n* Change EnableCacheSynchronization default.\r\n\r\n* Update tests.\r\n\r\n* Update method comment.\r\n\r\n* Update ConfidentialClientApplicationBuilder.cs\r\n\r\n* Update ConfidentialClientApplicationOptions.cs\r\n\r\n---------\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>",
          "timestamp": "2023-08-30T11:32:08-07:00",
          "tree_id": "7d6d146ebd3a97331a97839608624d076c5dcc99",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/d25f45f7e356bd92986e412dd68b5d341343aa6a"
        },
        "date": 1693420846891,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 465077.10447761195,
            "unit": "ns",
            "range": "± 21875.10509282761"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 641258.1612903225,
            "unit": "ns",
            "range": "± 19496.45872476465"
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
      },
      {
        "commit": {
          "author": {
            "name": "Neha Bhargava",
            "username": "neha-bhargava",
            "email": "61847233+neha-bhargava@users.noreply.github.com"
          },
          "committer": {
            "name": "GitHub",
            "username": "web-flow",
            "email": "noreply@github.com"
          },
          "id": "29de3eae8f07741bab1460afba13a4afdc8288c6",
          "message": "Merge branch 'main' into nebharg/openTelemetry",
          "timestamp": "2023-08-19T01:10:22Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/29de3eae8f07741bab1460afba13a4afdc8288c6"
        },
        "date": 1692408320003,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 24739.615376790363,
            "unit": "ns",
            "range": "± 358.5535128429255"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 298987.1404947917,
            "unit": "ns",
            "range": "± 5015.426302067345"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 55664.884690504805,
            "unit": "ns",
            "range": "± 470.2382481347005"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 306968.6367513021,
            "unit": "ns",
            "range": "± 4880.025071352569"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "name": "Neha Bhargava",
            "username": "neha-bhargava",
            "email": "61847233+neha-bhargava@users.noreply.github.com"
          },
          "committer": {
            "name": "GitHub",
            "username": "web-flow",
            "email": "noreply@github.com"
          },
          "id": "29de3eae8f07741bab1460afba13a4afdc8288c6",
          "message": "Merge branch 'main' into nebharg/openTelemetry",
          "timestamp": "2023-08-19T01:10:22Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/29de3eae8f07741bab1460afba13a4afdc8288c6"
        },
        "date": 1692408320003,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 24739.615376790363,
            "unit": "ns",
            "range": "± 358.5535128429255"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 298987.1404947917,
            "unit": "ns",
            "range": "± 5015.426302067345"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 55664.884690504805,
            "unit": "ns",
            "range": "± 470.2382481347005"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 306968.6367513021,
            "unit": "ns",
            "range": "± 4880.025071352569"
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
          "id": "80c848b5b7bfc033b11ee82107344bfc22efc0da",
          "message": "Add perf links to README.md (#4306)\n\nUpdate README.md",
          "timestamp": "2023-08-21T09:29:55Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/80c848b5b7bfc033b11ee82107344bfc22efc0da"
        },
        "date": 1692903748556,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 23896.345493570963,
            "unit": "ns",
            "range": "± 95.74316346901504"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 288669.3419921875,
            "unit": "ns",
            "range": "± 1548.2949115794593"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 49785.70501273019,
            "unit": "ns",
            "range": "± 27.435719689087254"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 290997.26861979166,
            "unit": "ns",
            "range": "± 1148.0687533732546"
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
          "id": "80c848b5b7bfc033b11ee82107344bfc22efc0da",
          "message": "Add perf links to README.md (#4306)\n\nUpdate README.md",
          "timestamp": "2023-08-21T09:29:55Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/80c848b5b7bfc033b11ee82107344bfc22efc0da"
        },
        "date": 1692903748556,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 23896.345493570963,
            "unit": "ns",
            "range": "± 95.74316346901504"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 288669.3419921875,
            "unit": "ns",
            "range": "± 1548.2949115794593"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 49785.70501273019,
            "unit": "ns",
            "range": "± 27.435719689087254"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 290997.26861979166,
            "unit": "ns",
            "range": "± 1148.0687533732546"
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
          "id": "3d6b84e5db422a64dc67fb08ecade35daf5a42eb",
          "message": "Update ADO yaml",
          "timestamp": "2023-08-25T06:41:18Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/3d6b84e5db422a64dc67fb08ecade35daf5a42eb"
        },
        "date": 1692946156839,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 22097.729736328125,
            "unit": "ns",
            "range": "± 61.35541956807626"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 245118.46520182292,
            "unit": "ns",
            "range": "± 1205.2554651824737"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 47990.21750313895,
            "unit": "ns",
            "range": "± 44.461694496818396"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 249110.39069475446,
            "unit": "ns",
            "range": "± 852.6031573386299"
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
          "id": "3d6b84e5db422a64dc67fb08ecade35daf5a42eb",
          "message": "Update ADO yaml",
          "timestamp": "2023-08-25T06:41:18Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/3d6b84e5db422a64dc67fb08ecade35daf5a42eb"
        },
        "date": 1692946156839,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 22097.729736328125,
            "unit": "ns",
            "range": "± 61.35541956807626"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 245118.46520182292,
            "unit": "ns",
            "range": "± 1205.2554651824737"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 47990.21750313895,
            "unit": "ns",
            "range": "± 44.461694496818396"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 249110.39069475446,
            "unit": "ns",
            "range": "± 852.6031573386299"
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
          "id": "c6f4a21248aa02b829b0befdc73e9f48a03cc2d1",
          "message": "Update pipeline-perf-tests-automation.yaml",
          "timestamp": "2023-08-25T06:49:23Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/c6f4a21248aa02b829b0befdc73e9f48a03cc2d1"
        },
        "date": 1692947362503,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 22265.298451741535,
            "unit": "ns",
            "range": "± 205.90216911144955"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 250844.24156901042,
            "unit": "ns",
            "range": "± 1050.6049401919247"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 47824.958350548375,
            "unit": "ns",
            "range": "± 64.8231753476551"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 250192.61783854166,
            "unit": "ns",
            "range": "± 2728.967950513722"
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
          "id": "c6f4a21248aa02b829b0befdc73e9f48a03cc2d1",
          "message": "Update pipeline-perf-tests-automation.yaml",
          "timestamp": "2023-08-25T06:49:23Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/c6f4a21248aa02b829b0befdc73e9f48a03cc2d1"
        },
        "date": 1692947362503,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 22265.298451741535,
            "unit": "ns",
            "range": "± 205.90216911144955"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 250844.24156901042,
            "unit": "ns",
            "range": "± 1050.6049401919247"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 47824.958350548375,
            "unit": "ns",
            "range": "± 64.8231753476551"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 250192.61783854166,
            "unit": "ns",
            "range": "± 2728.967950513722"
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
          "id": "3e133830f074ae020d5ae9b2a7dd4022fdd85415",
          "message": "concurrent token refresh fix for managed identity and app token provider (cc) (#4309)\n\n* mi\r\n\r\n* app token\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* pr comments\r\n\r\n* pr comments\r\n\r\n* pr comments\r\n\r\n* verbose logging\r\n\r\n* pr comments\r\n\r\n* Update src/client/Microsoft.Identity.Client/Internal/Requests/ManagedIdentityAuthRequest.cs\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* mi fix\r\n\r\n* few more edits\r\n\r\n* app token provider\r\n\r\n* edits\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* pr comments\r\n\r\n* pr comments\r\n\r\n* ProactivelyRefreshed\r\n\r\n* move around\r\n\r\n---------\r\n\r\nCo-authored-by: Gladwin Johnson <gljohns@microsoft.com>\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>",
          "timestamp": "2023-08-29T15:57:07-07:00",
          "tree_id": "ae680d731353b083290e572c6f6edebe79abc00a",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/3e133830f074ae020d5ae9b2a7dd4022fdd85415"
        },
        "date": 1693350379147,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 24901.748957707332,
            "unit": "ns",
            "range": "± 790.1977102248508"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 311954.92947387695,
            "unit": "ns",
            "range": "± 6116.615742864169"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 56632.10703531901,
            "unit": "ns",
            "range": "± 1271.4222662893515"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 315688.27163298236,
            "unit": "ns",
            "range": "± 7848.278108731289"
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
          "id": "3e133830f074ae020d5ae9b2a7dd4022fdd85415",
          "message": "concurrent token refresh fix for managed identity and app token provider (cc) (#4309)\n\n* mi\r\n\r\n* app token\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* pr comments\r\n\r\n* pr comments\r\n\r\n* pr comments\r\n\r\n* verbose logging\r\n\r\n* pr comments\r\n\r\n* Update src/client/Microsoft.Identity.Client/Internal/Requests/ManagedIdentityAuthRequest.cs\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* mi fix\r\n\r\n* few more edits\r\n\r\n* app token provider\r\n\r\n* edits\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* pr comments\r\n\r\n* pr comments\r\n\r\n* ProactivelyRefreshed\r\n\r\n* move around\r\n\r\n---------\r\n\r\nCo-authored-by: Gladwin Johnson <gljohns@microsoft.com>\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>",
          "timestamp": "2023-08-29T15:57:07-07:00",
          "tree_id": "ae680d731353b083290e572c6f6edebe79abc00a",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/3e133830f074ae020d5ae9b2a7dd4022fdd85415"
        },
        "date": 1693350379147,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 24901.748957707332,
            "unit": "ns",
            "range": "± 790.1977102248508"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 311954.92947387695,
            "unit": "ns",
            "range": "± 6116.615742864169"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 56632.10703531901,
            "unit": "ns",
            "range": "± 1271.4222662893515"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 315688.27163298236,
            "unit": "ns",
            "range": "± 7848.278108731289"
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
          "id": "0ea350f977601b34c4d7c804db0c2d4de44d1404",
          "message": "Update logging for Runtime Broker (#4318)\n\nUpdate WamAdapters.cs",
          "timestamp": "2023-08-30T05:09:14-07:00",
          "tree_id": "235ada87308558bc551ac67445bf69859dd3787c",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/0ea350f977601b34c4d7c804db0c2d4de44d1404"
        },
        "date": 1693397875145,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 23690.802906581335,
            "unit": "ns",
            "range": "± 97.97066922731474"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 286324.57809627755,
            "unit": "ns",
            "range": "± 5861.209062726872"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 50922.99212239583,
            "unit": "ns",
            "range": "± 65.59901507217937"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 290493.5352539063,
            "unit": "ns",
            "range": "± 1672.476860903505"
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
          "id": "0ea350f977601b34c4d7c804db0c2d4de44d1404",
          "message": "Update logging for Runtime Broker (#4318)\n\nUpdate WamAdapters.cs",
          "timestamp": "2023-08-30T05:09:14-07:00",
          "tree_id": "235ada87308558bc551ac67445bf69859dd3787c",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/0ea350f977601b34c4d7c804db0c2d4de44d1404"
        },
        "date": 1693397875145,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 23690.802906581335,
            "unit": "ns",
            "range": "± 97.97066922731474"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 286324.57809627755,
            "unit": "ns",
            "range": "± 5861.209062726872"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 50922.99212239583,
            "unit": "ns",
            "range": "± 65.59901507217937"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 290493.5352539063,
            "unit": "ns",
            "range": "± 1672.476860903505"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "name": "Gladwin Johnson",
            "username": "gladjohn",
            "email": "90415114+gladjohn@users.noreply.github.com"
          },
          "committer": {
            "name": "GitHub",
            "username": "web-flow",
            "email": "noreply@github.com"
          },
          "id": "0ea350f977601b34c4d7c804db0c2d4de44d1404",
          "message": "Update logging for Runtime Broker (#4318)\n\nUpdate WamAdapters.cs",
          "timestamp": "2023-08-30T12:09:14Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/0ea350f977601b34c4d7c804db0c2d4de44d1404"
        },
        "date": 1693420187233,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 22169.233968098957,
            "unit": "ns",
            "range": "± 123.32361226888884"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 247771.63365885417,
            "unit": "ns",
            "range": "± 2064.328907892224"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 48166.58170427595,
            "unit": "ns",
            "range": "± 154.08054179115632"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 245462.0402018229,
            "unit": "ns",
            "range": "± 5239.057053944816"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "name": "Gladwin Johnson",
            "username": "gladjohn",
            "email": "90415114+gladjohn@users.noreply.github.com"
          },
          "committer": {
            "name": "GitHub",
            "username": "web-flow",
            "email": "noreply@github.com"
          },
          "id": "0ea350f977601b34c4d7c804db0c2d4de44d1404",
          "message": "Update logging for Runtime Broker (#4318)\n\nUpdate WamAdapters.cs",
          "timestamp": "2023-08-30T12:09:14Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/0ea350f977601b34c4d7c804db0c2d4de44d1404"
        },
        "date": 1693420187233,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 22169.233968098957,
            "unit": "ns",
            "range": "± 123.32361226888884"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 247771.63365885417,
            "unit": "ns",
            "range": "± 2064.328907892224"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 48166.58170427595,
            "unit": "ns",
            "range": "± 154.08054179115632"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 245462.0402018229,
            "unit": "ns",
            "range": "± 5239.057053944816"
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
        "date": 1692220101869,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 47678.899914550784,
            "unit": "ns",
            "range": "± 675.8508804704387"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 382612.6973005022,
            "unit": "ns",
            "range": "± 4353.0946658173025"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 79183.53500976562,
            "unit": "ns",
            "range": "± 1101.547806986883"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 419764.54743303574,
            "unit": "ns",
            "range": "± 6082.424285081108"
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
        "date": 1692220101869,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 47678.899914550784,
            "unit": "ns",
            "range": "± 675.8508804704387"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 382612.6973005022,
            "unit": "ns",
            "range": "± 4353.0946658173025"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 79183.53500976562,
            "unit": "ns",
            "range": "± 1101.547806986883"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 419764.54743303574,
            "unit": "ns",
            "range": "± 6082.424285081108"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "name": "Neha Bhargava",
            "username": "neha-bhargava",
            "email": "61847233+neha-bhargava@users.noreply.github.com"
          },
          "committer": {
            "name": "GitHub",
            "username": "web-flow",
            "email": "noreply@github.com"
          },
          "id": "29de3eae8f07741bab1460afba13a4afdc8288c6",
          "message": "Merge branch 'main' into nebharg/openTelemetry",
          "timestamp": "2023-08-19T01:10:22Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/29de3eae8f07741bab1460afba13a4afdc8288c6"
        },
        "date": 1692408324362,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 43945.57993092256,
            "unit": "ns",
            "range": "± 902.1149412618001"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 410084.35009765625,
            "unit": "ns",
            "range": "± 6356.684944882213"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 74706.5951385498,
            "unit": "ns",
            "range": "± 1424.9611264915843"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 413129.06927083334,
            "unit": "ns",
            "range": "± 6911.760559770917"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "name": "Neha Bhargava",
            "username": "neha-bhargava",
            "email": "61847233+neha-bhargava@users.noreply.github.com"
          },
          "committer": {
            "name": "GitHub",
            "username": "web-flow",
            "email": "noreply@github.com"
          },
          "id": "29de3eae8f07741bab1460afba13a4afdc8288c6",
          "message": "Merge branch 'main' into nebharg/openTelemetry",
          "timestamp": "2023-08-19T01:10:22Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/29de3eae8f07741bab1460afba13a4afdc8288c6"
        },
        "date": 1692408324362,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 43945.57993092256,
            "unit": "ns",
            "range": "± 902.1149412618001"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 410084.35009765625,
            "unit": "ns",
            "range": "± 6356.684944882213"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 74706.5951385498,
            "unit": "ns",
            "range": "± 1424.9611264915843"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 413129.06927083334,
            "unit": "ns",
            "range": "± 6911.760559770917"
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
          "id": "80c848b5b7bfc033b11ee82107344bfc22efc0da",
          "message": "Add perf links to README.md (#4306)\n\nUpdate README.md",
          "timestamp": "2023-08-21T09:29:55Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/80c848b5b7bfc033b11ee82107344bfc22efc0da"
        },
        "date": 1692903751972,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 41508.58849283854,
            "unit": "ns",
            "range": "± 155.3597211340278"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 395997.07373046875,
            "unit": "ns",
            "range": "± 797.0558188442411"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 68823.73169817243,
            "unit": "ns",
            "range": "± 42.33204300533087"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 396170.30401141825,
            "unit": "ns",
            "range": "± 627.6359924117692"
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
          "id": "80c848b5b7bfc033b11ee82107344bfc22efc0da",
          "message": "Add perf links to README.md (#4306)\n\nUpdate README.md",
          "timestamp": "2023-08-21T09:29:55Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/80c848b5b7bfc033b11ee82107344bfc22efc0da"
        },
        "date": 1692903751972,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 41508.58849283854,
            "unit": "ns",
            "range": "± 155.3597211340278"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 395997.07373046875,
            "unit": "ns",
            "range": "± 797.0558188442411"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 68823.73169817243,
            "unit": "ns",
            "range": "± 42.33204300533087"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 396170.30401141825,
            "unit": "ns",
            "range": "± 627.6359924117692"
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
          "id": "3d6b84e5db422a64dc67fb08ecade35daf5a42eb",
          "message": "Update ADO yaml",
          "timestamp": "2023-08-25T06:41:18Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/3d6b84e5db422a64dc67fb08ecade35daf5a42eb"
        },
        "date": 1692946160912,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 39577.25639125279,
            "unit": "ns",
            "range": "± 68.16534733060786"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 344875.34629603795,
            "unit": "ns",
            "range": "± 1037.1919976026018"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 64825.47955791767,
            "unit": "ns",
            "range": "± 60.48420027057899"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 354135.4376953125,
            "unit": "ns",
            "range": "± 1406.2260674716806"
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
          "id": "3d6b84e5db422a64dc67fb08ecade35daf5a42eb",
          "message": "Update ADO yaml",
          "timestamp": "2023-08-25T06:41:18Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/3d6b84e5db422a64dc67fb08ecade35daf5a42eb"
        },
        "date": 1692946160912,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 39577.25639125279,
            "unit": "ns",
            "range": "± 68.16534733060786"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 344875.34629603795,
            "unit": "ns",
            "range": "± 1037.1919976026018"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 64825.47955791767,
            "unit": "ns",
            "range": "± 60.48420027057899"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 354135.4376953125,
            "unit": "ns",
            "range": "± 1406.2260674716806"
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
          "id": "c6f4a21248aa02b829b0befdc73e9f48a03cc2d1",
          "message": "Update pipeline-perf-tests-automation.yaml",
          "timestamp": "2023-08-25T06:49:23Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/c6f4a21248aa02b829b0befdc73e9f48a03cc2d1"
        },
        "date": 1692947366927,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 39707.87714436849,
            "unit": "ns",
            "range": "± 128.11866397099206"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 343212.0901066707,
            "unit": "ns",
            "range": "± 947.1074324867717"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 65418.25870455228,
            "unit": "ns",
            "range": "± 100.59127878527299"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 351709.34922572545,
            "unit": "ns",
            "range": "± 1100.1938212551056"
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
          "id": "c6f4a21248aa02b829b0befdc73e9f48a03cc2d1",
          "message": "Update pipeline-perf-tests-automation.yaml",
          "timestamp": "2023-08-25T06:49:23Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/c6f4a21248aa02b829b0befdc73e9f48a03cc2d1"
        },
        "date": 1692947366927,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 39707.87714436849,
            "unit": "ns",
            "range": "± 128.11866397099206"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 343212.0901066707,
            "unit": "ns",
            "range": "± 947.1074324867717"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 65418.25870455228,
            "unit": "ns",
            "range": "± 100.59127878527299"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 351709.34922572545,
            "unit": "ns",
            "range": "± 1100.1938212551056"
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
          "id": "3e133830f074ae020d5ae9b2a7dd4022fdd85415",
          "message": "concurrent token refresh fix for managed identity and app token provider (cc) (#4309)\n\n* mi\r\n\r\n* app token\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* pr comments\r\n\r\n* pr comments\r\n\r\n* pr comments\r\n\r\n* verbose logging\r\n\r\n* pr comments\r\n\r\n* Update src/client/Microsoft.Identity.Client/Internal/Requests/ManagedIdentityAuthRequest.cs\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* mi fix\r\n\r\n* few more edits\r\n\r\n* app token provider\r\n\r\n* edits\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* pr comments\r\n\r\n* pr comments\r\n\r\n* ProactivelyRefreshed\r\n\r\n* move around\r\n\r\n---------\r\n\r\nCo-authored-by: Gladwin Johnson <gljohns@microsoft.com>\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>",
          "timestamp": "2023-08-29T15:57:07-07:00",
          "tree_id": "ae680d731353b083290e572c6f6edebe79abc00a",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/3e133830f074ae020d5ae9b2a7dd4022fdd85415"
        },
        "date": 1693350383670,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 46936.49463704427,
            "unit": "ns",
            "range": "± 482.6689395398243"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 458432.8483072917,
            "unit": "ns",
            "range": "± 6189.919369331528"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 78163.78783365885,
            "unit": "ns",
            "range": "± 1121.204515599845"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 449146.3023763021,
            "unit": "ns",
            "range": "± 6005.149332528937"
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
          "id": "0ea350f977601b34c4d7c804db0c2d4de44d1404",
          "message": "Update logging for Runtime Broker (#4318)\n\nUpdate WamAdapters.cs",
          "timestamp": "2023-08-30T05:09:14-07:00",
          "tree_id": "235ada87308558bc551ac67445bf69859dd3787c",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/0ea350f977601b34c4d7c804db0c2d4de44d1404"
        },
        "date": 1693397878241,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 40635.34625651042,
            "unit": "ns",
            "range": "± 316.72344090023046"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 395834.6097981771,
            "unit": "ns",
            "range": "± 7303.725584508518"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 68019.11206926618,
            "unit": "ns",
            "range": "± 133.00461246954217"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 397771.51285807294,
            "unit": "ns",
            "range": "± 1828.8821735846993"
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
          "id": "0ea350f977601b34c4d7c804db0c2d4de44d1404",
          "message": "Update logging for Runtime Broker (#4318)\n\nUpdate WamAdapters.cs",
          "timestamp": "2023-08-30T05:09:14-07:00",
          "tree_id": "235ada87308558bc551ac67445bf69859dd3787c",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/0ea350f977601b34c4d7c804db0c2d4de44d1404"
        },
        "date": 1693397878241,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 40635.34625651042,
            "unit": "ns",
            "range": "± 316.72344090023046"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 395834.6097981771,
            "unit": "ns",
            "range": "± 7303.725584508518"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 68019.11206926618,
            "unit": "ns",
            "range": "± 133.00461246954217"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 397771.51285807294,
            "unit": "ns",
            "range": "± 1828.8821735846993"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "name": "Gladwin Johnson",
            "username": "gladjohn",
            "email": "90415114+gladjohn@users.noreply.github.com"
          },
          "committer": {
            "name": "GitHub",
            "username": "web-flow",
            "email": "noreply@github.com"
          },
          "id": "0ea350f977601b34c4d7c804db0c2d4de44d1404",
          "message": "Update logging for Runtime Broker (#4318)\n\nUpdate WamAdapters.cs",
          "timestamp": "2023-08-30T12:09:14Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/0ea350f977601b34c4d7c804db0c2d4de44d1404"
        },
        "date": 1693420191315,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 40057.57845633371,
            "unit": "ns",
            "range": "± 270.33081217432584"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 345415.2152669271,
            "unit": "ns",
            "range": "± 2685.9498407350197"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 65935.99844360352,
            "unit": "ns",
            "range": "± 709.9253902296622"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 357328.54996744794,
            "unit": "ns",
            "range": "± 928.6538365459887"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "name": "Gladwin Johnson",
            "username": "gladjohn",
            "email": "90415114+gladjohn@users.noreply.github.com"
          },
          "committer": {
            "name": "GitHub",
            "username": "web-flow",
            "email": "noreply@github.com"
          },
          "id": "0ea350f977601b34c4d7c804db0c2d4de44d1404",
          "message": "Update logging for Runtime Broker (#4318)\n\nUpdate WamAdapters.cs",
          "timestamp": "2023-08-30T12:09:14Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/0ea350f977601b34c4d7c804db0c2d4de44d1404"
        },
        "date": 1693420191315,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 40057.57845633371,
            "unit": "ns",
            "range": "± 270.33081217432584"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 345415.2152669271,
            "unit": "ns",
            "range": "± 2685.9498407350197"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 65935.99844360352,
            "unit": "ns",
            "range": "± 709.9253902296622"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 357328.54996744794,
            "unit": "ns",
            "range": "± 928.6538365459887"
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
        "date": 1692220105920,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 36697.539241536455,
            "unit": "ns",
            "range": "± 590.3090131109642"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 68037.42250462582,
            "unit": "ns",
            "range": "± 1508.5143044148886"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 18620.61758219401,
            "unit": "ns",
            "range": "± 181.54264003268335"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 49537.896580287386,
            "unit": "ns",
            "range": "± 473.4501097162311"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 59235.75,
            "unit": "ns",
            "range": "± 372.64853658397993"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 254822.16666666666,
            "unit": "ns",
            "range": "± 82577.93083176728"
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
        "date": 1692220105920,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 36697.539241536455,
            "unit": "ns",
            "range": "± 590.3090131109642"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 68037.42250462582,
            "unit": "ns",
            "range": "± 1508.5143044148886"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 18620.61758219401,
            "unit": "ns",
            "range": "± 181.54264003268335"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 49537.896580287386,
            "unit": "ns",
            "range": "± 473.4501097162311"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 59235.75,
            "unit": "ns",
            "range": "± 372.64853658397993"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 254822.16666666666,
            "unit": "ns",
            "range": "± 82577.93083176728"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "name": "Neha Bhargava",
            "username": "neha-bhargava",
            "email": "61847233+neha-bhargava@users.noreply.github.com"
          },
          "committer": {
            "name": "GitHub",
            "username": "web-flow",
            "email": "noreply@github.com"
          },
          "id": "29de3eae8f07741bab1460afba13a4afdc8288c6",
          "message": "Merge branch 'main' into nebharg/openTelemetry",
          "timestamp": "2023-08-19T01:10:22Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/29de3eae8f07741bab1460afba13a4afdc8288c6"
        },
        "date": 1692408328743,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 33578.81231689453,
            "unit": "ns",
            "range": "± 400.77833311329226"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 59667.560366821286,
            "unit": "ns",
            "range": "± 4278.021022022083"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 15185.670910151317,
            "unit": "ns",
            "range": "± 1274.5528939972285"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 44360.14719577366,
            "unit": "ns",
            "range": "± 2468.3835197825933"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 58455.63,
            "unit": "ns",
            "range": "± 6221.955742831796"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 185618.275,
            "unit": "ns",
            "range": "± 18234.70554542851"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "name": "Neha Bhargava",
            "username": "neha-bhargava",
            "email": "61847233+neha-bhargava@users.noreply.github.com"
          },
          "committer": {
            "name": "GitHub",
            "username": "web-flow",
            "email": "noreply@github.com"
          },
          "id": "29de3eae8f07741bab1460afba13a4afdc8288c6",
          "message": "Merge branch 'main' into nebharg/openTelemetry",
          "timestamp": "2023-08-19T01:10:22Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/29de3eae8f07741bab1460afba13a4afdc8288c6"
        },
        "date": 1692408328743,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 33578.81231689453,
            "unit": "ns",
            "range": "± 400.77833311329226"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 59667.560366821286,
            "unit": "ns",
            "range": "± 4278.021022022083"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 15185.670910151317,
            "unit": "ns",
            "range": "± 1274.5528939972285"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 44360.14719577366,
            "unit": "ns",
            "range": "± 2468.3835197825933"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 58455.63,
            "unit": "ns",
            "range": "± 6221.955742831796"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 185618.275,
            "unit": "ns",
            "range": "± 18234.70554542851"
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
          "id": "80c848b5b7bfc033b11ee82107344bfc22efc0da",
          "message": "Add perf links to README.md (#4306)\n\nUpdate README.md",
          "timestamp": "2023-08-21T09:29:55Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/80c848b5b7bfc033b11ee82107344bfc22efc0da"
        },
        "date": 1692903755294,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 32073.92776285807,
            "unit": "ns",
            "range": "± 45.178160114330396"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 59440.96331317608,
            "unit": "ns",
            "range": "± 42.47513818648071"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 16173.409865315754,
            "unit": "ns",
            "range": "± 30.948999566666068"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 43029.13295084635,
            "unit": "ns",
            "range": "± 29.189361275936182"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 61807.92307692308,
            "unit": "ns",
            "range": "± 322.79855987350726"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 218914.81720430107,
            "unit": "ns",
            "range": "± 65807.67746952943"
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
          "id": "80c848b5b7bfc033b11ee82107344bfc22efc0da",
          "message": "Add perf links to README.md (#4306)\n\nUpdate README.md",
          "timestamp": "2023-08-21T09:29:55Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/80c848b5b7bfc033b11ee82107344bfc22efc0da"
        },
        "date": 1692903755294,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 32073.92776285807,
            "unit": "ns",
            "range": "± 45.178160114330396"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 59440.96331317608,
            "unit": "ns",
            "range": "± 42.47513818648071"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 16173.409865315754,
            "unit": "ns",
            "range": "± 30.948999566666068"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 43029.13295084635,
            "unit": "ns",
            "range": "± 29.189361275936182"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 61807.92307692308,
            "unit": "ns",
            "range": "± 322.79855987350726"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 218914.81720430107,
            "unit": "ns",
            "range": "± 65807.67746952943"
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
          "id": "c6f4a21248aa02b829b0befdc73e9f48a03cc2d1",
          "message": "Update pipeline-perf-tests-automation.yaml",
          "timestamp": "2023-08-25T06:49:23Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/c6f4a21248aa02b829b0befdc73e9f48a03cc2d1"
        },
        "date": 1692946165099,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 29634.493483323316,
            "unit": "ns",
            "range": "± 42.738557732018705"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 55765.9325125558,
            "unit": "ns",
            "range": "± 47.67588701297102"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 15475.396580287388,
            "unit": "ns",
            "range": "± 23.587219256073006"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 41550.635306803386,
            "unit": "ns",
            "range": "± 35.1351431044544"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 53714.71428571428,
            "unit": "ns",
            "range": "± 1256.2973829017214"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 195331.60638297873,
            "unit": "ns",
            "range": "± 67277.90042611782"
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
          "id": "c6f4a21248aa02b829b0befdc73e9f48a03cc2d1",
          "message": "Update pipeline-perf-tests-automation.yaml",
          "timestamp": "2023-08-25T06:49:23Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/c6f4a21248aa02b829b0befdc73e9f48a03cc2d1"
        },
        "date": 1692946165099,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 29634.493483323316,
            "unit": "ns",
            "range": "± 42.738557732018705"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 55765.9325125558,
            "unit": "ns",
            "range": "± 47.67588701297102"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 15475.396580287388,
            "unit": "ns",
            "range": "± 23.587219256073006"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 41550.635306803386,
            "unit": "ns",
            "range": "± 35.1351431044544"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 53714.71428571428,
            "unit": "ns",
            "range": "± 1256.2973829017214"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 195331.60638297873,
            "unit": "ns",
            "range": "± 67277.90042611782"
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
          "id": "c6f4a21248aa02b829b0befdc73e9f48a03cc2d1",
          "message": "Update pipeline-perf-tests-automation.yaml",
          "timestamp": "2023-08-25T06:49:23Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/c6f4a21248aa02b829b0befdc73e9f48a03cc2d1"
        },
        "date": 1692947371170,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 30778.89018031529,
            "unit": "ns",
            "range": "± 109.4776345198452"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 56015.69307767428,
            "unit": "ns",
            "range": "± 107.08605473707722"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 15742.154408046177,
            "unit": "ns",
            "range": "± 131.80945241444863"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 40933.70902797154,
            "unit": "ns",
            "range": "± 34.51850181549713"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 57536.08,
            "unit": "ns",
            "range": "± 1393.9700176115698"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 188063.05208333334,
            "unit": "ns",
            "range": "± 77057.40313481545"
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
          "id": "c6f4a21248aa02b829b0befdc73e9f48a03cc2d1",
          "message": "Update pipeline-perf-tests-automation.yaml",
          "timestamp": "2023-08-25T06:49:23Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/c6f4a21248aa02b829b0befdc73e9f48a03cc2d1"
        },
        "date": 1692947371170,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 30778.89018031529,
            "unit": "ns",
            "range": "± 109.4776345198452"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 56015.69307767428,
            "unit": "ns",
            "range": "± 107.08605473707722"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 15742.154408046177,
            "unit": "ns",
            "range": "± 131.80945241444863"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 40933.70902797154,
            "unit": "ns",
            "range": "± 34.51850181549713"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 57536.08,
            "unit": "ns",
            "range": "± 1393.9700176115698"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 188063.05208333334,
            "unit": "ns",
            "range": "± 77057.40313481545"
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
          "id": "0ea350f977601b34c4d7c804db0c2d4de44d1404",
          "message": "Update logging for Runtime Broker (#4318)\n\nUpdate WamAdapters.cs",
          "timestamp": "2023-08-30T05:09:14-07:00",
          "tree_id": "235ada87308558bc551ac67445bf69859dd3787c",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/0ea350f977601b34c4d7c804db0c2d4de44d1404"
        },
        "date": 1693397881728,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 31781.753633626304,
            "unit": "ns",
            "range": "± 211.2605813631793"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 58960.76881190709,
            "unit": "ns",
            "range": "± 147.9967039441058"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 16672.98231288365,
            "unit": "ns",
            "range": "± 42.02521649022934"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 43001.27237810408,
            "unit": "ns",
            "range": "± 33.70545312192156"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 56408.230769230766,
            "unit": "ns",
            "range": "± 361.63129331916554"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 256787.73958333334,
            "unit": "ns",
            "range": "± 96080.76654513062"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "name": "Gladwin Johnson",
            "username": "gladjohn",
            "email": "90415114+gladjohn@users.noreply.github.com"
          },
          "committer": {
            "name": "GitHub",
            "username": "web-flow",
            "email": "noreply@github.com"
          },
          "id": "0ea350f977601b34c4d7c804db0c2d4de44d1404",
          "message": "Update logging for Runtime Broker (#4318)\n\nUpdate WamAdapters.cs",
          "timestamp": "2023-08-30T12:09:14Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/0ea350f977601b34c4d7c804db0c2d4de44d1404"
        },
        "date": 1693420195083,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 30663.240483210637,
            "unit": "ns",
            "range": "± 340.24727598492274"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 55999.61613972982,
            "unit": "ns",
            "range": "± 54.956969630429306"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 15920.939115397136,
            "unit": "ns",
            "range": "± 28.416639337916145"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 41356.198735163765,
            "unit": "ns",
            "range": "± 189.3076210143626"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 57064.41176470588,
            "unit": "ns",
            "range": "± 1012.3757367464617"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 201905.74468085106,
            "unit": "ns",
            "range": "± 113459.89185553456"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "name": "Gladwin Johnson",
            "username": "gladjohn",
            "email": "90415114+gladjohn@users.noreply.github.com"
          },
          "committer": {
            "name": "GitHub",
            "username": "web-flow",
            "email": "noreply@github.com"
          },
          "id": "0ea350f977601b34c4d7c804db0c2d4de44d1404",
          "message": "Update logging for Runtime Broker (#4318)\n\nUpdate WamAdapters.cs",
          "timestamp": "2023-08-30T12:09:14Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/0ea350f977601b34c4d7c804db0c2d4de44d1404"
        },
        "date": 1693420195083,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 30663.240483210637,
            "unit": "ns",
            "range": "± 340.24727598492274"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 55999.61613972982,
            "unit": "ns",
            "range": "± 54.956969630429306"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 15920.939115397136,
            "unit": "ns",
            "range": "± 28.416639337916145"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 41356.198735163765,
            "unit": "ns",
            "range": "± 189.3076210143626"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 57064.41176470588,
            "unit": "ns",
            "range": "± 1012.3757367464617"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 201905.74468085106,
            "unit": "ns",
            "range": "± 113459.89185553456"
          }
        ]
      }
    ]
  }
}