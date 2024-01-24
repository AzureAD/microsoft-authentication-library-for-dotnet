window.BENCHMARK_DATA = {
  "lastUpdate": 1706093593983,
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
          "id": "d25f45f7e356bd92986e412dd68b5d341343aa6a",
          "message": "Set CacheSynchronization to true by default (#4310)\n\n* Change EnableCacheSynchronization default.\r\n\r\n* Update tests.\r\n\r\n* Update method comment.\r\n\r\n* Update ConfidentialClientApplicationBuilder.cs\r\n\r\n* Update ConfidentialClientApplicationOptions.cs\r\n\r\n---------\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>",
          "timestamp": "2023-08-30T18:32:08Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/d25f45f7e356bd92986e412dd68b5d341343aa6a"
        },
        "date": 1693422352687,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 390153.0561797753,
            "unit": "ns",
            "range": "± 27642.90315851248"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 541523.3846153846,
            "unit": "ns",
            "range": "± 5219.649757382538"
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
          "id": "d25f45f7e356bd92986e412dd68b5d341343aa6a",
          "message": "Set CacheSynchronization to true by default (#4310)\n\n* Change EnableCacheSynchronization default.\r\n\r\n* Update tests.\r\n\r\n* Update method comment.\r\n\r\n* Update ConfidentialClientApplicationBuilder.cs\r\n\r\n* Update ConfidentialClientApplicationOptions.cs\r\n\r\n---------\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>",
          "timestamp": "2023-08-30T18:32:08Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/d25f45f7e356bd92986e412dd68b5d341343aa6a"
        },
        "date": 1693422352687,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 390153.0561797753,
            "unit": "ns",
            "range": "± 27642.90315851248"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 541523.3846153846,
            "unit": "ns",
            "range": "± 5219.649757382538"
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
          "id": "29cbbb0ebe4361c98bcfaf9f00092c1517939ea3",
          "message": "Expose WAM telemetry in AuthenticationResult and exceptions (#4307)\n\n* Add WAM telemetry to AuthResult\r\n\r\n* Add wam telemetry to exceptions\r\n\r\n* Address PR chages\r\n\r\n* Add WAM telemetry to AuthResult\r\n\r\n* Add wam telemetry to exceptions\r\n\r\n* Address PR chages\r\n\r\n* Fix naming\r\n\r\n* fix\r\n\r\n* Rename\r\n\r\n* Fix",
          "timestamp": "2023-09-05T12:44:21+01:00",
          "tree_id": "abfe00ce5ef36f4d45737e9a537369ba544de73a",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/29cbbb0ebe4361c98bcfaf9f00092c1517939ea3"
        },
        "date": 1693914746422,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 400122.88,
            "unit": "ns",
            "range": "± 15256.027132345338"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 539515.0833333334,
            "unit": "ns",
            "range": "± 5064.139691063632"
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
          "id": "29cbbb0ebe4361c98bcfaf9f00092c1517939ea3",
          "message": "Expose WAM telemetry in AuthenticationResult and exceptions (#4307)\n\n* Add WAM telemetry to AuthResult\r\n\r\n* Add wam telemetry to exceptions\r\n\r\n* Address PR chages\r\n\r\n* Add WAM telemetry to AuthResult\r\n\r\n* Add wam telemetry to exceptions\r\n\r\n* Address PR chages\r\n\r\n* Fix naming\r\n\r\n* fix\r\n\r\n* Rename\r\n\r\n* Fix",
          "timestamp": "2023-09-05T12:44:21+01:00",
          "tree_id": "abfe00ce5ef36f4d45737e9a537369ba544de73a",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/29cbbb0ebe4361c98bcfaf9f00092c1517939ea3"
        },
        "date": 1693914746422,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 400122.88,
            "unit": "ns",
            "range": "± 15256.027132345338"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 539515.0833333334,
            "unit": "ns",
            "range": "± 5064.139691063632"
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
          "id": "90ae6b9c6d33655a66cbc6413e8aadee487f052a",
          "message": "Check for null and empty scopes in managed identity (#4328)\n\n* managed identity fix\r\n\r\n* ArgumentNullException\r\n\r\n* fix\r\n\r\n* exception\r\n\r\n---------\r\n\r\nCo-authored-by: Gladwin Johnson <gljohns@microsoft.com>",
          "timestamp": "2023-09-06T17:15:40Z",
          "tree_id": "d1247ec22650b6f61de984ce7e3f7f5efbca8d89",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/90ae6b9c6d33655a66cbc6413e8aadee487f052a"
        },
        "date": 1694021120688,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 407943.51219512196,
            "unit": "ns",
            "range": "± 69195.47942688983"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 760415.0111111111,
            "unit": "ns",
            "range": "± 261512.2211211533"
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
          "id": "90ae6b9c6d33655a66cbc6413e8aadee487f052a",
          "message": "Check for null and empty scopes in managed identity (#4328)\n\n* managed identity fix\r\n\r\n* ArgumentNullException\r\n\r\n* fix\r\n\r\n* exception\r\n\r\n---------\r\n\r\nCo-authored-by: Gladwin Johnson <gljohns@microsoft.com>",
          "timestamp": "2023-09-06T17:15:40Z",
          "tree_id": "d1247ec22650b6f61de984ce7e3f7f5efbca8d89",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/90ae6b9c6d33655a66cbc6413e8aadee487f052a"
        },
        "date": 1694030716179,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 536800.5365853659,
            "unit": "ns",
            "range": "± 19308.948573262038"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 550572.4,
            "unit": "ns",
            "range": "± 6823.8953559218235"
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
          "id": "90ae6b9c6d33655a66cbc6413e8aadee487f052a",
          "message": "Check for null and empty scopes in managed identity (#4328)\n\n* managed identity fix\r\n\r\n* ArgumentNullException\r\n\r\n* fix\r\n\r\n* exception\r\n\r\n---------\r\n\r\nCo-authored-by: Gladwin Johnson <gljohns@microsoft.com>",
          "timestamp": "2023-09-06T17:15:40Z",
          "tree_id": "d1247ec22650b6f61de984ce7e3f7f5efbca8d89",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/90ae6b9c6d33655a66cbc6413e8aadee487f052a"
        },
        "date": 1694031581468,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 381899.3953488372,
            "unit": "ns",
            "range": "± 13920.842039973108"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 517141.9285714286,
            "unit": "ns",
            "range": "± 7235.4150427366185"
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
          "id": "90ae6b9c6d33655a66cbc6413e8aadee487f052a",
          "message": "Check for null and empty scopes in managed identity (#4328)\n\n* managed identity fix\r\n\r\n* ArgumentNullException\r\n\r\n* fix\r\n\r\n* exception\r\n\r\n---------\r\n\r\nCo-authored-by: Gladwin Johnson <gljohns@microsoft.com>",
          "timestamp": "2023-09-06T17:15:40Z",
          "tree_id": "d1247ec22650b6f61de984ce7e3f7f5efbca8d89",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/90ae6b9c6d33655a66cbc6413e8aadee487f052a"
        },
        "date": 1694031581468,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 381899.3953488372,
            "unit": "ns",
            "range": "± 13920.842039973108"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 517141.9285714286,
            "unit": "ns",
            "range": "± 7235.4150427366185"
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
          "id": "fe3c8eacfb013b73404b3295ec5a36b3a60b3ed8",
          "message": "Fix for WAM Runtime error logs duplicated info (#4354)\n\n* Update RuntimeBroker.cs\r\n\r\n* Update RuntimeBroker.cs",
          "timestamp": "2023-09-19T14:24:56-07:00",
          "tree_id": "e70a2e1aab89831139cede5cbd30c11f05df7ed4",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/fe3c8eacfb013b73404b3295ec5a36b3a60b3ed8"
        },
        "date": 1695159217113,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 378882.47826086957,
            "unit": "ns",
            "range": "± 14446.853793187607"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 539907.2619047619,
            "unit": "ns",
            "range": "± 36330.32476453858"
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
          "id": "fe3c8eacfb013b73404b3295ec5a36b3a60b3ed8",
          "message": "Fix for WAM Runtime error logs duplicated info (#4354)\n\n* Update RuntimeBroker.cs\r\n\r\n* Update RuntimeBroker.cs",
          "timestamp": "2023-09-19T14:24:56-07:00",
          "tree_id": "e70a2e1aab89831139cede5cbd30c11f05df7ed4",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/fe3c8eacfb013b73404b3295ec5a36b3a60b3ed8"
        },
        "date": 1695159217113,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 378882.47826086957,
            "unit": "ns",
            "range": "± 14446.853793187607"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 539907.2619047619,
            "unit": "ns",
            "range": "± 36330.32476453858"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "53200638+localden@users.noreply.github.com",
            "name": "Den Delimarsky",
            "username": "localden"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "b898ab8193bc751ae5aabe5ff7e5f079eccd3f85",
          "message": "Updates to documentation and minor code cleanup (#4339) (#4346)\n\n* Updates to documentation (#4339)\r\n\r\n* Update PublicClientApplication.cs\r\n\r\n* Update inheritdoc comments\r\n\r\n* Update documentation inheritance logic for PCA\r\n\r\n* Update documentation\r\n\r\n* Cleanup unused namespaces\r\n\r\n* Some extra cleanup\r\n\r\n* Update README.md\r\n\r\n* Documentation and minor cleanup\r\n\r\n* Update AuthenticationHeaderParser.cs\r\n\r\n* Update README.md\r\n\r\n* Update contributing documentation\r\n\r\nThis tackles the merge from https://github.com/MicrosoftDocs/microsoft-authentication-library-dotnet/pull/135\r\n\r\n* Roll back this change because we're building on .NET Fx, which doesn't support ranges\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update src/client/Microsoft.Identity.Client/AuthenticationHeaderParser.cs\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Update README.md\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Small documentation cleanup items\r\n\r\n* Update CONTRIBUTING.md\r\n\r\n* Cleanup\r\n\r\n* More cleanup - this time with some code\r\n\r\n* Update MsalServiceExceptionFactory.cs\r\n\r\n* Update MsalServiceExceptionFactory.cs\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Update src/client/Microsoft.Identity.Client/UserAssertion.cs\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Add missing Using statement\r\n\r\n---------\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\nCo-authored-by: Bogdan Gavril <bogavril@microsoft.com>",
          "timestamp": "2023-09-25T13:11:55Z",
          "tree_id": "b514ea57ab8de82c8840ca5149c4407d0b7a4c6a",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/b898ab8193bc751ae5aabe5ff7e5f079eccd3f85"
        },
        "date": 1695648069758,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 455679.02,
            "unit": "ns",
            "range": "± 29190.684877123993"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 843603.3369565217,
            "unit": "ns",
            "range": "± 336210.57111657504"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "53200638+localden@users.noreply.github.com",
            "name": "Den Delimarsky",
            "username": "localden"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "b898ab8193bc751ae5aabe5ff7e5f079eccd3f85",
          "message": "Updates to documentation and minor code cleanup (#4339) (#4346)\n\n* Updates to documentation (#4339)\r\n\r\n* Update PublicClientApplication.cs\r\n\r\n* Update inheritdoc comments\r\n\r\n* Update documentation inheritance logic for PCA\r\n\r\n* Update documentation\r\n\r\n* Cleanup unused namespaces\r\n\r\n* Some extra cleanup\r\n\r\n* Update README.md\r\n\r\n* Documentation and minor cleanup\r\n\r\n* Update AuthenticationHeaderParser.cs\r\n\r\n* Update README.md\r\n\r\n* Update contributing documentation\r\n\r\nThis tackles the merge from https://github.com/MicrosoftDocs/microsoft-authentication-library-dotnet/pull/135\r\n\r\n* Roll back this change because we're building on .NET Fx, which doesn't support ranges\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update src/client/Microsoft.Identity.Client/AuthenticationHeaderParser.cs\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Update README.md\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Small documentation cleanup items\r\n\r\n* Update CONTRIBUTING.md\r\n\r\n* Cleanup\r\n\r\n* More cleanup - this time with some code\r\n\r\n* Update MsalServiceExceptionFactory.cs\r\n\r\n* Update MsalServiceExceptionFactory.cs\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Update src/client/Microsoft.Identity.Client/UserAssertion.cs\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Add missing Using statement\r\n\r\n---------\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\nCo-authored-by: Bogdan Gavril <bogavril@microsoft.com>",
          "timestamp": "2023-09-25T13:11:55Z",
          "tree_id": "b514ea57ab8de82c8840ca5149c4407d0b7a4c6a",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/b898ab8193bc751ae5aabe5ff7e5f079eccd3f85"
        },
        "date": 1695658988075,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 392892.60869565216,
            "unit": "ns",
            "range": "± 27712.635125974906"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 509979.3076923077,
            "unit": "ns",
            "range": "± 7176.787551830036"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "53200638+localden@users.noreply.github.com",
            "name": "Den Delimarsky",
            "username": "localden"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "b898ab8193bc751ae5aabe5ff7e5f079eccd3f85",
          "message": "Updates to documentation and minor code cleanup (#4339) (#4346)\n\n* Updates to documentation (#4339)\r\n\r\n* Update PublicClientApplication.cs\r\n\r\n* Update inheritdoc comments\r\n\r\n* Update documentation inheritance logic for PCA\r\n\r\n* Update documentation\r\n\r\n* Cleanup unused namespaces\r\n\r\n* Some extra cleanup\r\n\r\n* Update README.md\r\n\r\n* Documentation and minor cleanup\r\n\r\n* Update AuthenticationHeaderParser.cs\r\n\r\n* Update README.md\r\n\r\n* Update contributing documentation\r\n\r\nThis tackles the merge from https://github.com/MicrosoftDocs/microsoft-authentication-library-dotnet/pull/135\r\n\r\n* Roll back this change because we're building on .NET Fx, which doesn't support ranges\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update src/client/Microsoft.Identity.Client/AuthenticationHeaderParser.cs\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Update README.md\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Small documentation cleanup items\r\n\r\n* Update CONTRIBUTING.md\r\n\r\n* Cleanup\r\n\r\n* More cleanup - this time with some code\r\n\r\n* Update MsalServiceExceptionFactory.cs\r\n\r\n* Update MsalServiceExceptionFactory.cs\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Update src/client/Microsoft.Identity.Client/UserAssertion.cs\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Add missing Using statement\r\n\r\n---------\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\nCo-authored-by: Bogdan Gavril <bogavril@microsoft.com>",
          "timestamp": "2023-09-25T13:11:55Z",
          "tree_id": "b514ea57ab8de82c8840ca5149c4407d0b7a4c6a",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/b898ab8193bc751ae5aabe5ff7e5f079eccd3f85"
        },
        "date": 1695658988075,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 392892.60869565216,
            "unit": "ns",
            "range": "± 27712.635125974906"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 509979.3076923077,
            "unit": "ns",
            "range": "± 7176.787551830036"
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
          "id": "80c3be514c4bd59412b20b7c1f792452be3943ea",
          "message": "Add CacheLevel to AuthResultMetadata (#4349)",
          "timestamp": "2023-09-26T06:26:38Z",
          "tree_id": "94eabae8ba8aa46440d553e91a36efc0b90018e4",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/80c3be514c4bd59412b20b7c1f792452be3943ea"
        },
        "date": 1695710057115,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 374213.1489361702,
            "unit": "ns",
            "range": "± 14329.645230565118"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 519182.75757575757,
            "unit": "ns",
            "range": "± 16150.007565846337"
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
          "id": "80c3be514c4bd59412b20b7c1f792452be3943ea",
          "message": "Add CacheLevel to AuthResultMetadata (#4349)",
          "timestamp": "2023-09-26T06:26:38Z",
          "tree_id": "94eabae8ba8aa46440d553e91a36efc0b90018e4",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/80c3be514c4bd59412b20b7c1f792452be3943ea"
        },
        "date": 1695710057115,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 374213.1489361702,
            "unit": "ns",
            "range": "± 14329.645230565118"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 519182.75757575757,
            "unit": "ns",
            "range": "± 16150.007565846337"
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
          "id": "118ab93b5afb78c8c4a594dc27a3288725f76213",
          "message": "Add one retry to assertion signing. (#4365)\n\n* Add one retry to assertion signing.\r\n\r\n* Fix.",
          "timestamp": "2023-10-04T18:13:42-07:00",
          "tree_id": "9e28b0749136868369fc637d0dfc253f9ffe26bd",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/118ab93b5afb78c8c4a594dc27a3288725f76213"
        },
        "date": 1696469281155,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 607331.8426966292,
            "unit": "ns",
            "range": "± 247990.09336233843"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 705936.375,
            "unit": "ns",
            "range": "± 73020.3934273087"
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
          "id": "118ab93b5afb78c8c4a594dc27a3288725f76213",
          "message": "Add one retry to assertion signing. (#4365)\n\n* Add one retry to assertion signing.\r\n\r\n* Fix.",
          "timestamp": "2023-10-05T01:13:42Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/118ab93b5afb78c8c4a594dc27a3288725f76213"
        },
        "date": 1696485244470,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 386462.39285714284,
            "unit": "ns",
            "range": "± 16573.275513942288"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 531205.7142857143,
            "unit": "ns",
            "range": "± 14741.275370366484"
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
          "id": "118ab93b5afb78c8c4a594dc27a3288725f76213",
          "message": "Add one retry to assertion signing. (#4365)\n\n* Add one retry to assertion signing.\r\n\r\n* Fix.",
          "timestamp": "2023-10-05T01:13:42Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/118ab93b5afb78c8c4a594dc27a3288725f76213"
        },
        "date": 1696485244470,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 386462.39285714284,
            "unit": "ns",
            "range": "± 16573.275513942288"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 531205.7142857143,
            "unit": "ns",
            "range": "± 14741.275370366484"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "travis.walker@microsoft.com",
            "name": "Travis Walker",
            "username": "trwalke"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "b259cf00936a11a9cff789bf094935d8d31aea7f",
          "message": "Disabling x-client-CPU (#4337)\n\nCo-authored-by: trwalke <trwalke@microsoft.com>",
          "timestamp": "2023-10-05T08:16:41Z",
          "tree_id": "41b84f6aa61c6b639b41d2fbf9b4f36b5cf13479",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/b259cf00936a11a9cff789bf094935d8d31aea7f"
        },
        "date": 1696494224103,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 381486.5319148936,
            "unit": "ns",
            "range": "± 14794.855089657327"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 520742.75,
            "unit": "ns",
            "range": "± 10230.99661160469"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "travis.walker@microsoft.com",
            "name": "Travis Walker",
            "username": "trwalke"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "b259cf00936a11a9cff789bf094935d8d31aea7f",
          "message": "Disabling x-client-CPU (#4337)\n\nCo-authored-by: trwalke <trwalke@microsoft.com>",
          "timestamp": "2023-10-05T08:16:41Z",
          "tree_id": "41b84f6aa61c6b639b41d2fbf9b4f36b5cf13479",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/b259cf00936a11a9cff789bf094935d8d31aea7f"
        },
        "date": 1696494224103,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 381486.5319148936,
            "unit": "ns",
            "range": "± 14794.855089657327"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 520742.75,
            "unit": "ns",
            "range": "± 10230.99661160469"
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
          "id": "474026f56b1c3dfe2628fea104b8c80c46ce7e57",
          "message": "Throw ArgumentException for malformed authorities (#4347)\n\n* Throw argument exceptions for consistency when authority or tenant are invalid. Update method comments.\r\n\r\n* Update and add tests.\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Den Delimarsky <53200638+localden@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Small test fixes.\r\n\r\n* Update comments.\r\n\r\n---------\r\n\r\nCo-authored-by: Den Delimarsky <53200638+localden@users.noreply.github.com>\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>",
          "timestamp": "2023-10-10T17:11:08-07:00",
          "tree_id": "5489e2487eb172ef71a482b92a3b09aa4b60fde3",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/474026f56b1c3dfe2628fea104b8c80c46ce7e57"
        },
        "date": 1696983665230,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 448461.26923076925,
            "unit": "ns",
            "range": "± 56807.01754703296"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 531543.25,
            "unit": "ns",
            "range": "± 11190.26339878604"
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
          "id": "474026f56b1c3dfe2628fea104b8c80c46ce7e57",
          "message": "Throw ArgumentException for malformed authorities (#4347)\n\n* Throw argument exceptions for consistency when authority or tenant are invalid. Update method comments.\r\n\r\n* Update and add tests.\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Den Delimarsky <53200638+localden@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Small test fixes.\r\n\r\n* Update comments.\r\n\r\n---------\r\n\r\nCo-authored-by: Den Delimarsky <53200638+localden@users.noreply.github.com>\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>",
          "timestamp": "2023-10-10T17:11:08-07:00",
          "tree_id": "5489e2487eb172ef71a482b92a3b09aa4b60fde3",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/474026f56b1c3dfe2628fea104b8c80c46ce7e57"
        },
        "date": 1696983665230,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 448461.26923076925,
            "unit": "ns",
            "range": "± 56807.01754703296"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 531543.25,
            "unit": "ns",
            "range": "± 11190.26339878604"
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
          "id": "474026f56b1c3dfe2628fea104b8c80c46ce7e57",
          "message": "Throw ArgumentException for malformed authorities (#4347)\n\n* Throw argument exceptions for consistency when authority or tenant are invalid. Update method comments.\r\n\r\n* Update and add tests.\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Den Delimarsky <53200638+localden@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Small test fixes.\r\n\r\n* Update comments.\r\n\r\n---------\r\n\r\nCo-authored-by: Den Delimarsky <53200638+localden@users.noreply.github.com>\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>",
          "timestamp": "2023-10-11T00:11:08Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/474026f56b1c3dfe2628fea104b8c80c46ce7e57"
        },
        "date": 1696995969944,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 728166.0721649484,
            "unit": "ns",
            "range": "± 479141.5139465203"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 721329.5348837209,
            "unit": "ns",
            "range": "± 217295.77053536402"
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
          "id": "474026f56b1c3dfe2628fea104b8c80c46ce7e57",
          "message": "Throw ArgumentException for malformed authorities (#4347)\n\n* Throw argument exceptions for consistency when authority or tenant are invalid. Update method comments.\r\n\r\n* Update and add tests.\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Den Delimarsky <53200638+localden@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Small test fixes.\r\n\r\n* Update comments.\r\n\r\n---------\r\n\r\nCo-authored-by: Den Delimarsky <53200638+localden@users.noreply.github.com>\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>",
          "timestamp": "2023-10-11T00:11:08Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/474026f56b1c3dfe2628fea104b8c80c46ce7e57"
        },
        "date": 1697001190271,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 432564.1282051282,
            "unit": "ns",
            "range": "± 27611.177775769876"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 859902.6170212766,
            "unit": "ns",
            "range": "± 421553.6423980162"
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
          "id": "474026f56b1c3dfe2628fea104b8c80c46ce7e57",
          "message": "Throw ArgumentException for malformed authorities (#4347)\n\n* Throw argument exceptions for consistency when authority or tenant are invalid. Update method comments.\r\n\r\n* Update and add tests.\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Den Delimarsky <53200638+localden@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Small test fixes.\r\n\r\n* Update comments.\r\n\r\n---------\r\n\r\nCo-authored-by: Den Delimarsky <53200638+localden@users.noreply.github.com>\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>",
          "timestamp": "2023-10-11T00:11:08Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/474026f56b1c3dfe2628fea104b8c80c46ce7e57"
        },
        "date": 1697004632853,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 380165.5714285714,
            "unit": "ns",
            "range": "± 16208.817065083767"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 513661.4347826087,
            "unit": "ns",
            "range": "± 12984.471245670511"
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
          "id": "474026f56b1c3dfe2628fea104b8c80c46ce7e57",
          "message": "Throw ArgumentException for malformed authorities (#4347)\n\n* Throw argument exceptions for consistency when authority or tenant are invalid. Update method comments.\r\n\r\n* Update and add tests.\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Den Delimarsky <53200638+localden@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Small test fixes.\r\n\r\n* Update comments.\r\n\r\n---------\r\n\r\nCo-authored-by: Den Delimarsky <53200638+localden@users.noreply.github.com>\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>",
          "timestamp": "2023-10-11T00:11:08Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/474026f56b1c3dfe2628fea104b8c80c46ce7e57"
        },
        "date": 1697004632853,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 380165.5714285714,
            "unit": "ns",
            "range": "± 16208.817065083767"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 513661.4347826087,
            "unit": "ns",
            "range": "± 12984.471245670511"
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
          "id": "8bdf6990fe9ad679adabcd1e7b008c3e38a75d3f",
          "message": "Fix comments warnings. (#4373)",
          "timestamp": "2023-10-12T21:14:28Z",
          "tree_id": "fc9a99ce441fb47274b6d000f90dfcea10f0698f",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/8bdf6990fe9ad679adabcd1e7b008c3e38a75d3f"
        },
        "date": 1697145728653,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 376136.0172413793,
            "unit": "ns",
            "range": "± 16248.089604834013"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 516880.9032258064,
            "unit": "ns",
            "range": "± 13563.385197790996"
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
          "id": "8bdf6990fe9ad679adabcd1e7b008c3e38a75d3f",
          "message": "Fix comments warnings. (#4373)",
          "timestamp": "2023-10-12T21:14:28Z",
          "tree_id": "fc9a99ce441fb47274b6d000f90dfcea10f0698f",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/8bdf6990fe9ad679adabcd1e7b008c3e38a75d3f"
        },
        "date": 1697145728653,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 376136.0172413793,
            "unit": "ns",
            "range": "± 16248.089604834013"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 516880.9032258064,
            "unit": "ns",
            "range": "± 13563.385197790996"
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
          "id": "a8b93d42c8083097bfa452d313504e92d1ebc414",
          "message": "Print refresh token cache keys to logs (#4375)\n\n* Print RTs to cache.\r\n\r\n* Add tests.",
          "timestamp": "2023-10-16T12:15:40-07:00",
          "tree_id": "0e6d34d0b69bca92a076c60a4f162dbf59a232b7",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/a8b93d42c8083097bfa452d313504e92d1ebc414"
        },
        "date": 1697484258559,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 409946.9456521739,
            "unit": "ns",
            "range": "± 26414.54876603985"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 601911.156626506,
            "unit": "ns",
            "range": "± 50751.154291926934"
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
          "id": "a8b93d42c8083097bfa452d313504e92d1ebc414",
          "message": "Print refresh token cache keys to logs (#4375)\n\n* Print RTs to cache.\r\n\r\n* Add tests.",
          "timestamp": "2023-10-16T12:15:40-07:00",
          "tree_id": "0e6d34d0b69bca92a076c60a4f162dbf59a232b7",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/a8b93d42c8083097bfa452d313504e92d1ebc414"
        },
        "date": 1697484258559,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 409946.9456521739,
            "unit": "ns",
            "range": "± 26414.54876603985"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 601911.156626506,
            "unit": "ns",
            "range": "± 50751.154291926934"
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
          "id": "f951d8418e0a9fcb011c1cd7cfbcfec5b8df0962",
          "message": "Add serialization of broker error data in exceptions (#4376)\n\n* Add serialization of broker error.\r\n\r\n* Refactor.\r\n\r\n* Fix.\r\n\r\n* PR feedback.\r\n\r\n* Naming change.",
          "timestamp": "2023-10-16T15:21:08-07:00",
          "tree_id": "85c04e69f87e6728882ceb75044aad7a206c196c",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/f951d8418e0a9fcb011c1cd7cfbcfec5b8df0962"
        },
        "date": 1697495304702,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 379151.3777777778,
            "unit": "ns",
            "range": "± 14171.34681526727"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 567250.2666666667,
            "unit": "ns",
            "range": "± 15248.966718942052"
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
          "id": "f951d8418e0a9fcb011c1cd7cfbcfec5b8df0962",
          "message": "Add serialization of broker error data in exceptions (#4376)\n\n* Add serialization of broker error.\r\n\r\n* Refactor.\r\n\r\n* Fix.\r\n\r\n* PR feedback.\r\n\r\n* Naming change.",
          "timestamp": "2023-10-16T15:21:08-07:00",
          "tree_id": "85c04e69f87e6728882ceb75044aad7a206c196c",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/f951d8418e0a9fcb011c1cd7cfbcfec5b8df0962"
        },
        "date": 1697495304702,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 379151.3777777778,
            "unit": "ns",
            "range": "± 14171.34681526727"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 567250.2666666667,
            "unit": "ns",
            "range": "± 15248.966718942052"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "61847233+neha-bhargava@users.noreply.github.com",
            "name": "Neha Bhargava",
            "username": "neha-bhargava"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "4b1c58a0ee196b6f12ea172f1df252d9bcc3b972",
          "message": "Bump Net461 to Net462 (#4367)\n\n* Bump Net461 to Net462\r\n\r\n* Update PublicClientApplicationBuilder.cs\r\n\r\n* Address comments\r\n\r\n* Bump broker and desktop projects to 462 as well",
          "timestamp": "2023-10-26T16:39:04-07:00",
          "tree_id": "f03444b94796040f06465ab6c78faac2ac23161d",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/4b1c58a0ee196b6f12ea172f1df252d9bcc3b972"
        },
        "date": 1698364066590,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 447460.29545454547,
            "unit": "ns",
            "range": "± 16592.443343883348"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 918668.8152173914,
            "unit": "ns",
            "range": "± 342267.0913205204"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "83938949+ashok672@users.noreply.github.com",
            "name": "Ashok Kumar Ramakrishnan",
            "username": "ashok672"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "4b0b6cdc73e079cc6a73c20f3ab01058da29e2b0",
          "message": "Fall back to local cache if broker fails to return result for silent token call  (#4395)\n\n* Update SilentRequest.cs\r\n\r\n* Fix failing unit tests",
          "timestamp": "2023-11-02T12:10:02Z",
          "tree_id": "7c1ab0ab79280c1535ba0365d55bca60c0e05432",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/4b0b6cdc73e079cc6a73c20f3ab01058da29e2b0"
        },
        "date": 1698927427078,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 375374.23333333334,
            "unit": "ns",
            "range": "± 10702.177202546809"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 555390.42,
            "unit": "ns",
            "range": "± 40388.19375049941"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "83938949+ashok672@users.noreply.github.com",
            "name": "Ashok Kumar Ramakrishnan",
            "username": "ashok672"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "4b0b6cdc73e079cc6a73c20f3ab01058da29e2b0",
          "message": "Fall back to local cache if broker fails to return result for silent token call  (#4395)\n\n* Update SilentRequest.cs\r\n\r\n* Fix failing unit tests",
          "timestamp": "2023-11-02T12:10:02Z",
          "tree_id": "7c1ab0ab79280c1535ba0365d55bca60c0e05432",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/4b0b6cdc73e079cc6a73c20f3ab01058da29e2b0"
        },
        "date": 1698927427078,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 375374.23333333334,
            "unit": "ns",
            "range": "± 10702.177202546809"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 555390.42,
            "unit": "ns",
            "range": "± 40388.19375049941"
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
          "id": "172a869d926f243a2c65cc6c63610a6de5f53de7",
          "message": "Deprecate request.WithAuthority (#4406)\n\n* Deprecate request.WithAuthority\r\n\r\n* Deprecate request.WithAuthority\r\n\r\n* fix tests\r\n\r\n* fix",
          "timestamp": "2023-11-06T14:35:37-08:00",
          "tree_id": "41c5124d5bc6117ba67d18a9615f8881cb5cef2f",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/172a869d926f243a2c65cc6c63610a6de5f53de7"
        },
        "date": 1699310648179,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 649967.6170212766,
            "unit": "ns",
            "range": "± 382738.15533900366"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 639153.3614457832,
            "unit": "ns",
            "range": "± 52263.311999298865"
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
          "id": "172a869d926f243a2c65cc6c63610a6de5f53de7",
          "message": "Deprecate request.WithAuthority (#4406)\n\n* Deprecate request.WithAuthority\r\n\r\n* Deprecate request.WithAuthority\r\n\r\n* fix tests\r\n\r\n* fix",
          "timestamp": "2023-11-06T14:35:37-08:00",
          "tree_id": "41c5124d5bc6117ba67d18a9615f8881cb5cef2f",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/172a869d926f243a2c65cc6c63610a6de5f53de7"
        },
        "date": 1699637962829,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 295056.5641025641,
            "unit": "ns",
            "range": "± 15051.560295862797"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 411215.5,
            "unit": "ns",
            "range": "± 6589.142453250709"
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
          "id": "172a869d926f243a2c65cc6c63610a6de5f53de7",
          "message": "Deprecate request.WithAuthority (#4406)\n\n* Deprecate request.WithAuthority\r\n\r\n* Deprecate request.WithAuthority\r\n\r\n* fix tests\r\n\r\n* fix",
          "timestamp": "2023-11-06T14:35:37-08:00",
          "tree_id": "41c5124d5bc6117ba67d18a9615f8881cb5cef2f",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/172a869d926f243a2c65cc6c63610a6de5f53de7"
        },
        "date": 1699637962829,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 295056.5641025641,
            "unit": "ns",
            "range": "± 15051.560295862797"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 411215.5,
            "unit": "ns",
            "range": "± 6589.142453250709"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "43250667+iulico-1@users.noreply.github.com",
            "name": "Iulian C",
            "username": "iulico-1"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "da67f067f14b26fdfb928178ad8b21bcdcde91a9",
          "message": "Add support for MSA passthrough for silent token acquisition calls through the boker (#4419)\n\nAdd support for MSA passthrough for silent token acquisition calls through the boker (#4419)",
          "timestamp": "2023-11-17T09:02:38-08:00",
          "tree_id": "15e73a063233ee0b0f3923f0dadd48a269fc59d3",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/da67f067f14b26fdfb928178ad8b21bcdcde91a9"
        },
        "date": 1700240974051,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 302004.6288659794,
            "unit": "ns",
            "range": "± 17105.940701941086"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 424623.8,
            "unit": "ns",
            "range": "± 7511.270723766671"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "43250667+iulico-1@users.noreply.github.com",
            "name": "Iulian C",
            "username": "iulico-1"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "da67f067f14b26fdfb928178ad8b21bcdcde91a9",
          "message": "Add support for MSA passthrough for silent token acquisition calls through the boker (#4419)\n\nAdd support for MSA passthrough for silent token acquisition calls through the boker (#4419)",
          "timestamp": "2023-11-17T09:02:38-08:00",
          "tree_id": "15e73a063233ee0b0f3923f0dadd48a269fc59d3",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/da67f067f14b26fdfb928178ad8b21bcdcde91a9"
        },
        "date": 1700240974051,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 302004.6288659794,
            "unit": "ns",
            "range": "± 17105.940701941086"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 424623.8,
            "unit": "ns",
            "range": "± 7511.270723766671"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "61847233+neha-bhargava@users.noreply.github.com",
            "name": "Neha Bhargava",
            "username": "neha-bhargava"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "9792dc6d7e7459a1d52ee0fda28e20dbd57111d8",
          "message": "Cache level should be L1 in case of default in memory cache. (#4423)\n\nCache level should be L1 in case of default in memory cache",
          "timestamp": "2023-11-17T11:30:35-08:00",
          "tree_id": "1d7c2f47b8b9893dd334d2726f49cd6580541a6a",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/9792dc6d7e7459a1d52ee0fda28e20dbd57111d8"
        },
        "date": 1700249869716,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 307761.9534883721,
            "unit": "ns",
            "range": "± 16134.886355827184"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 397435.35714285716,
            "unit": "ns",
            "range": "± 7024.557361988592"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "61847233+neha-bhargava@users.noreply.github.com",
            "name": "Neha Bhargava",
            "username": "neha-bhargava"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "9792dc6d7e7459a1d52ee0fda28e20dbd57111d8",
          "message": "Cache level should be L1 in case of default in memory cache. (#4423)\n\nCache level should be L1 in case of default in memory cache",
          "timestamp": "2023-11-17T11:30:35-08:00",
          "tree_id": "1d7c2f47b8b9893dd334d2726f49cd6580541a6a",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/9792dc6d7e7459a1d52ee0fda28e20dbd57111d8"
        },
        "date": 1700249869716,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 307761.9534883721,
            "unit": "ns",
            "range": "± 16134.886355827184"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 397435.35714285716,
            "unit": "ns",
            "range": "± 7024.557361988592"
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
          "id": "067159b7259f648798d0762a72b24ef4b3d5c8a3",
          "message": "Use net6.0",
          "timestamp": "2024-01-24T05:19:44Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/067159b7259f648798d0762a72b24ef4b3d5c8a3"
        },
        "date": 1706074014004,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 259642,
            "unit": "ns",
            "range": "± 7570.476698879395"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 333599.125,
            "unit": "ns",
            "range": "± 4624.94541769594"
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
          "id": "067159b7259f648798d0762a72b24ef4b3d5c8a3",
          "message": "Use net6.0",
          "timestamp": "2024-01-24T05:19:44Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/067159b7259f648798d0762a72b24ef4b3d5c8a3"
        },
        "date": 1706074014004,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 259642,
            "unit": "ns",
            "range": "± 7570.476698879395"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 333599.125,
            "unit": "ns",
            "range": "± 4624.94541769594"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "simon.cropp@gmail.com",
            "name": "Simon Cropp",
            "username": "SimonCropp"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "16827a2ccbfcaa607de7295520b5f097446c261a",
          "message": "reduce some string alloc on building (#4564)",
          "timestamp": "2024-01-24T08:51:29Z",
          "tree_id": "2559765585e66470a99924346ae757d15c86668f",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/16827a2ccbfcaa607de7295520b5f097446c261a"
        },
        "date": 1706086716211,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 244509.2142857143,
            "unit": "ns",
            "range": "± 3287.4001050297256"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 350950.64285714284,
            "unit": "ns",
            "range": "± 6015.10767033428"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "simon.cropp@gmail.com",
            "name": "Simon Cropp",
            "username": "SimonCropp"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "16827a2ccbfcaa607de7295520b5f097446c261a",
          "message": "reduce some string alloc on building (#4564)",
          "timestamp": "2024-01-24T08:51:29Z",
          "tree_id": "2559765585e66470a99924346ae757d15c86668f",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/16827a2ccbfcaa607de7295520b5f097446c261a"
        },
        "date": 1706086716211,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 244509.2142857143,
            "unit": "ns",
            "range": "± 3287.4001050297256"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 350950.64285714284,
            "unit": "ns",
            "range": "± 6015.10767033428"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "simon.cropp@gmail.com",
            "name": "Simon Cropp",
            "username": "SimonCropp"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "1f999fb8520ae1bc0062ac77fe686c241c1eb541",
          "message": "remove unreachable code in GetParsedAuthValueElements (#4541)",
          "timestamp": "2024-01-24T09:41:02Z",
          "tree_id": "89f97b9ffd703f2b73f57c1daee8a46414d1995c",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/1f999fb8520ae1bc0062ac77fe686c241c1eb541"
        },
        "date": 1706089701496,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 247765.3,
            "unit": "ns",
            "range": "± 3895.749610610444"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 341487.6111111111,
            "unit": "ns",
            "range": "± 7265.923261352908"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "simon.cropp@gmail.com",
            "name": "Simon Cropp",
            "username": "SimonCropp"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "1f999fb8520ae1bc0062ac77fe686c241c1eb541",
          "message": "remove unreachable code in GetParsedAuthValueElements (#4541)",
          "timestamp": "2024-01-24T09:41:02Z",
          "tree_id": "89f97b9ffd703f2b73f57c1daee8a46414d1995c",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/1f999fb8520ae1bc0062ac77fe686c241c1eb541"
        },
        "date": 1706089701496,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 247765.3,
            "unit": "ns",
            "range": "± 3895.749610610444"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 341487.6111111111,
            "unit": "ns",
            "range": "± 7265.923261352908"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "simon.cropp@gmail.com",
            "name": "Simon Cropp",
            "username": "SimonCropp"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "ddeb3151c037e3ca81a81c40868bbcd597a5c07e",
          "message": "remove StringReplace (#4569)",
          "timestamp": "2024-01-24T10:46:08Z",
          "tree_id": "a0e700837f33b0dd37e28df0ac904bd7b462bc8c",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/ddeb3151c037e3ca81a81c40868bbcd597a5c07e"
        },
        "date": 1706093590636,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 262185.9090909091,
            "unit": "ns",
            "range": "± 6122.093999909095"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 337042.1666666667,
            "unit": "ns",
            "range": "± 3469.6587422798207"
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
        "date": 1693420855705,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 27078.365657552084,
            "unit": "ns",
            "range": "± 336.9275642455167"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 320909.6796875,
            "unit": "ns",
            "range": "± 4424.117241846644"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 58446.714318847655,
            "unit": "ns",
            "range": "± 322.7737624965282"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 324368.80787760415,
            "unit": "ns",
            "range": "± 4445.082407271233"
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
          "id": "d25f45f7e356bd92986e412dd68b5d341343aa6a",
          "message": "Set CacheSynchronization to true by default (#4310)\n\n* Change EnableCacheSynchronization default.\r\n\r\n* Update tests.\r\n\r\n* Update method comment.\r\n\r\n* Update ConfidentialClientApplicationBuilder.cs\r\n\r\n* Update ConfidentialClientApplicationOptions.cs\r\n\r\n---------\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>",
          "timestamp": "2023-08-30T18:32:08Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/d25f45f7e356bd92986e412dd68b5d341343aa6a"
        },
        "date": 1693422359253,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 23065.543615722658,
            "unit": "ns",
            "range": "± 141.55432233644964"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 273922.5248372396,
            "unit": "ns",
            "range": "± 3899.879301755645"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 50009.10170084635,
            "unit": "ns",
            "range": "± 107.2744422117097"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 282490.02372472425,
            "unit": "ns",
            "range": "± 5757.603669377255"
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
          "id": "d25f45f7e356bd92986e412dd68b5d341343aa6a",
          "message": "Set CacheSynchronization to true by default (#4310)\n\n* Change EnableCacheSynchronization default.\r\n\r\n* Update tests.\r\n\r\n* Update method comment.\r\n\r\n* Update ConfidentialClientApplicationBuilder.cs\r\n\r\n* Update ConfidentialClientApplicationOptions.cs\r\n\r\n---------\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>",
          "timestamp": "2023-08-30T18:32:08Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/d25f45f7e356bd92986e412dd68b5d341343aa6a"
        },
        "date": 1693422359253,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 23065.543615722658,
            "unit": "ns",
            "range": "± 141.55432233644964"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 273922.5248372396,
            "unit": "ns",
            "range": "± 3899.879301755645"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 50009.10170084635,
            "unit": "ns",
            "range": "± 107.2744422117097"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 282490.02372472425,
            "unit": "ns",
            "range": "± 5757.603669377255"
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
          "id": "29cbbb0ebe4361c98bcfaf9f00092c1517939ea3",
          "message": "Expose WAM telemetry in AuthenticationResult and exceptions (#4307)\n\n* Add WAM telemetry to AuthResult\r\n\r\n* Add wam telemetry to exceptions\r\n\r\n* Address PR chages\r\n\r\n* Add WAM telemetry to AuthResult\r\n\r\n* Add wam telemetry to exceptions\r\n\r\n* Address PR chages\r\n\r\n* Fix naming\r\n\r\n* fix\r\n\r\n* Rename\r\n\r\n* Fix",
          "timestamp": "2023-09-05T12:44:21+01:00",
          "tree_id": "abfe00ce5ef36f4d45737e9a537369ba544de73a",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/29cbbb0ebe4361c98bcfaf9f00092c1517939ea3"
        },
        "date": 1693914753832,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 23182.085630289712,
            "unit": "ns",
            "range": "± 155.84122239151083"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 272924.29055989586,
            "unit": "ns",
            "range": "± 3093.6394821212293"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 50140.185384114586,
            "unit": "ns",
            "range": "± 62.49856989391205"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 276984.2393554688,
            "unit": "ns",
            "range": "± 3279.5734821544133"
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
          "id": "29cbbb0ebe4361c98bcfaf9f00092c1517939ea3",
          "message": "Expose WAM telemetry in AuthenticationResult and exceptions (#4307)\n\n* Add WAM telemetry to AuthResult\r\n\r\n* Add wam telemetry to exceptions\r\n\r\n* Address PR chages\r\n\r\n* Add WAM telemetry to AuthResult\r\n\r\n* Add wam telemetry to exceptions\r\n\r\n* Address PR chages\r\n\r\n* Fix naming\r\n\r\n* fix\r\n\r\n* Rename\r\n\r\n* Fix",
          "timestamp": "2023-09-05T12:44:21+01:00",
          "tree_id": "abfe00ce5ef36f4d45737e9a537369ba544de73a",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/29cbbb0ebe4361c98bcfaf9f00092c1517939ea3"
        },
        "date": 1693914753832,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 23182.085630289712,
            "unit": "ns",
            "range": "± 155.84122239151083"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 272924.29055989586,
            "unit": "ns",
            "range": "± 3093.6394821212293"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 50140.185384114586,
            "unit": "ns",
            "range": "± 62.49856989391205"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 276984.2393554688,
            "unit": "ns",
            "range": "± 3279.5734821544133"
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
          "id": "90ae6b9c6d33655a66cbc6413e8aadee487f052a",
          "message": "Check for null and empty scopes in managed identity (#4328)\n\n* managed identity fix\r\n\r\n* ArgumentNullException\r\n\r\n* fix\r\n\r\n* exception\r\n\r\n---------\r\n\r\nCo-authored-by: Gladwin Johnson <gljohns@microsoft.com>",
          "timestamp": "2023-09-06T17:15:40Z",
          "tree_id": "d1247ec22650b6f61de984ce7e3f7f5efbca8d89",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/90ae6b9c6d33655a66cbc6413e8aadee487f052a"
        },
        "date": 1694031589359,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 22295.442598615373,
            "unit": "ns",
            "range": "± 83.06417401473936"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 261486.94944661457,
            "unit": "ns",
            "range": "± 3714.958689715796"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 48951.477600097656,
            "unit": "ns",
            "range": "± 68.11166801767916"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 261411.85806274414,
            "unit": "ns",
            "range": "± 5062.130687208182"
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
          "id": "90ae6b9c6d33655a66cbc6413e8aadee487f052a",
          "message": "Check for null and empty scopes in managed identity (#4328)\n\n* managed identity fix\r\n\r\n* ArgumentNullException\r\n\r\n* fix\r\n\r\n* exception\r\n\r\n---------\r\n\r\nCo-authored-by: Gladwin Johnson <gljohns@microsoft.com>",
          "timestamp": "2023-09-06T17:15:40Z",
          "tree_id": "d1247ec22650b6f61de984ce7e3f7f5efbca8d89",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/90ae6b9c6d33655a66cbc6413e8aadee487f052a"
        },
        "date": 1694031589359,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 22295.442598615373,
            "unit": "ns",
            "range": "± 83.06417401473936"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 261486.94944661457,
            "unit": "ns",
            "range": "± 3714.958689715796"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 48951.477600097656,
            "unit": "ns",
            "range": "± 68.11166801767916"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 261411.85806274414,
            "unit": "ns",
            "range": "± 5062.130687208182"
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
          "id": "fe3c8eacfb013b73404b3295ec5a36b3a60b3ed8",
          "message": "Fix for WAM Runtime error logs duplicated info (#4354)\n\n* Update RuntimeBroker.cs\r\n\r\n* Update RuntimeBroker.cs",
          "timestamp": "2023-09-19T14:24:56-07:00",
          "tree_id": "e70a2e1aab89831139cede5cbd30c11f05df7ed4",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/fe3c8eacfb013b73404b3295ec5a36b3a60b3ed8"
        },
        "date": 1695159223881,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 23568.71450151716,
            "unit": "ns",
            "range": "± 75.28307200367637"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 290317.57330729166,
            "unit": "ns",
            "range": "± 1879.6766835058288"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 50120.99815586635,
            "unit": "ns",
            "range": "± 52.80473171651195"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 293644.50556640624,
            "unit": "ns",
            "range": "± 1911.3920088163766"
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
          "id": "fe3c8eacfb013b73404b3295ec5a36b3a60b3ed8",
          "message": "Fix for WAM Runtime error logs duplicated info (#4354)\n\n* Update RuntimeBroker.cs\r\n\r\n* Update RuntimeBroker.cs",
          "timestamp": "2023-09-19T14:24:56-07:00",
          "tree_id": "e70a2e1aab89831139cede5cbd30c11f05df7ed4",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/fe3c8eacfb013b73404b3295ec5a36b3a60b3ed8"
        },
        "date": 1695159223881,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 23568.71450151716,
            "unit": "ns",
            "range": "± 75.28307200367637"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 290317.57330729166,
            "unit": "ns",
            "range": "± 1879.6766835058288"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 50120.99815586635,
            "unit": "ns",
            "range": "± 52.80473171651195"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 293644.50556640624,
            "unit": "ns",
            "range": "± 1911.3920088163766"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "53200638+localden@users.noreply.github.com",
            "name": "Den Delimarsky",
            "username": "localden"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "b898ab8193bc751ae5aabe5ff7e5f079eccd3f85",
          "message": "Updates to documentation and minor code cleanup (#4339) (#4346)\n\n* Updates to documentation (#4339)\r\n\r\n* Update PublicClientApplication.cs\r\n\r\n* Update inheritdoc comments\r\n\r\n* Update documentation inheritance logic for PCA\r\n\r\n* Update documentation\r\n\r\n* Cleanup unused namespaces\r\n\r\n* Some extra cleanup\r\n\r\n* Update README.md\r\n\r\n* Documentation and minor cleanup\r\n\r\n* Update AuthenticationHeaderParser.cs\r\n\r\n* Update README.md\r\n\r\n* Update contributing documentation\r\n\r\nThis tackles the merge from https://github.com/MicrosoftDocs/microsoft-authentication-library-dotnet/pull/135\r\n\r\n* Roll back this change because we're building on .NET Fx, which doesn't support ranges\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update src/client/Microsoft.Identity.Client/AuthenticationHeaderParser.cs\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Update README.md\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Small documentation cleanup items\r\n\r\n* Update CONTRIBUTING.md\r\n\r\n* Cleanup\r\n\r\n* More cleanup - this time with some code\r\n\r\n* Update MsalServiceExceptionFactory.cs\r\n\r\n* Update MsalServiceExceptionFactory.cs\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Update src/client/Microsoft.Identity.Client/UserAssertion.cs\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Add missing Using statement\r\n\r\n---------\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\nCo-authored-by: Bogdan Gavril <bogavril@microsoft.com>",
          "timestamp": "2023-09-25T13:11:55Z",
          "tree_id": "b514ea57ab8de82c8840ca5149c4407d0b7a4c6a",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/b898ab8193bc751ae5aabe5ff7e5f079eccd3f85"
        },
        "date": 1695658994911,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 23243.49473063151,
            "unit": "ns",
            "range": "± 98.52740009707684"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 281186.20068359375,
            "unit": "ns",
            "range": "± 2684.3256677632266"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 50216.11032104492,
            "unit": "ns",
            "range": "± 33.20239515742203"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 285085.8111653646,
            "unit": "ns",
            "range": "± 2517.953390945688"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "53200638+localden@users.noreply.github.com",
            "name": "Den Delimarsky",
            "username": "localden"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "b898ab8193bc751ae5aabe5ff7e5f079eccd3f85",
          "message": "Updates to documentation and minor code cleanup (#4339) (#4346)\n\n* Updates to documentation (#4339)\r\n\r\n* Update PublicClientApplication.cs\r\n\r\n* Update inheritdoc comments\r\n\r\n* Update documentation inheritance logic for PCA\r\n\r\n* Update documentation\r\n\r\n* Cleanup unused namespaces\r\n\r\n* Some extra cleanup\r\n\r\n* Update README.md\r\n\r\n* Documentation and minor cleanup\r\n\r\n* Update AuthenticationHeaderParser.cs\r\n\r\n* Update README.md\r\n\r\n* Update contributing documentation\r\n\r\nThis tackles the merge from https://github.com/MicrosoftDocs/microsoft-authentication-library-dotnet/pull/135\r\n\r\n* Roll back this change because we're building on .NET Fx, which doesn't support ranges\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update src/client/Microsoft.Identity.Client/AuthenticationHeaderParser.cs\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Update README.md\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Small documentation cleanup items\r\n\r\n* Update CONTRIBUTING.md\r\n\r\n* Cleanup\r\n\r\n* More cleanup - this time with some code\r\n\r\n* Update MsalServiceExceptionFactory.cs\r\n\r\n* Update MsalServiceExceptionFactory.cs\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Update src/client/Microsoft.Identity.Client/UserAssertion.cs\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Add missing Using statement\r\n\r\n---------\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\nCo-authored-by: Bogdan Gavril <bogavril@microsoft.com>",
          "timestamp": "2023-09-25T13:11:55Z",
          "tree_id": "b514ea57ab8de82c8840ca5149c4407d0b7a4c6a",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/b898ab8193bc751ae5aabe5ff7e5f079eccd3f85"
        },
        "date": 1695658994911,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 23243.49473063151,
            "unit": "ns",
            "range": "± 98.52740009707684"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 281186.20068359375,
            "unit": "ns",
            "range": "± 2684.3256677632266"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 50216.11032104492,
            "unit": "ns",
            "range": "± 33.20239515742203"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 285085.8111653646,
            "unit": "ns",
            "range": "± 2517.953390945688"
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
          "id": "80c3be514c4bd59412b20b7c1f792452be3943ea",
          "message": "Add CacheLevel to AuthResultMetadata (#4349)",
          "timestamp": "2023-09-26T06:26:38Z",
          "tree_id": "94eabae8ba8aa46440d553e91a36efc0b90018e4",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/80c3be514c4bd59412b20b7c1f792452be3943ea"
        },
        "date": 1695710065702,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 22986.417218017577,
            "unit": "ns",
            "range": "± 106.34226044951744"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 246772.281703404,
            "unit": "ns",
            "range": "± 564.2455500402405"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 48213.281829833984,
            "unit": "ns",
            "range": "± 48.04113768845733"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 252837.14453125,
            "unit": "ns",
            "range": "± 526.1337509573245"
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
          "id": "80c3be514c4bd59412b20b7c1f792452be3943ea",
          "message": "Add CacheLevel to AuthResultMetadata (#4349)",
          "timestamp": "2023-09-26T06:26:38Z",
          "tree_id": "94eabae8ba8aa46440d553e91a36efc0b90018e4",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/80c3be514c4bd59412b20b7c1f792452be3943ea"
        },
        "date": 1695710065702,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 22986.417218017577,
            "unit": "ns",
            "range": "± 106.34226044951744"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 246772.281703404,
            "unit": "ns",
            "range": "± 564.2455500402405"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 48213.281829833984,
            "unit": "ns",
            "range": "± 48.04113768845733"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 252837.14453125,
            "unit": "ns",
            "range": "± 526.1337509573245"
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
          "id": "118ab93b5afb78c8c4a594dc27a3288725f76213",
          "message": "Add one retry to assertion signing. (#4365)\n\n* Add one retry to assertion signing.\r\n\r\n* Fix.",
          "timestamp": "2023-10-05T01:13:42Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/118ab93b5afb78c8c4a594dc27a3288725f76213"
        },
        "date": 1696485253553,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 22446.62495727539,
            "unit": "ns",
            "range": "± 80.30946472388882"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 248311.5810546875,
            "unit": "ns",
            "range": "± 1120.8925025518106"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 48431.1901163737,
            "unit": "ns",
            "range": "± 305.17856145176023"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 253745.94638671874,
            "unit": "ns",
            "range": "± 1184.6597124682835"
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
          "id": "118ab93b5afb78c8c4a594dc27a3288725f76213",
          "message": "Add one retry to assertion signing. (#4365)\n\n* Add one retry to assertion signing.\r\n\r\n* Fix.",
          "timestamp": "2023-10-05T01:13:42Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/118ab93b5afb78c8c4a594dc27a3288725f76213"
        },
        "date": 1696485253553,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 22446.62495727539,
            "unit": "ns",
            "range": "± 80.30946472388882"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 248311.5810546875,
            "unit": "ns",
            "range": "± 1120.8925025518106"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 48431.1901163737,
            "unit": "ns",
            "range": "± 305.17856145176023"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 253745.94638671874,
            "unit": "ns",
            "range": "± 1184.6597124682835"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "travis.walker@microsoft.com",
            "name": "Travis Walker",
            "username": "trwalke"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "b259cf00936a11a9cff789bf094935d8d31aea7f",
          "message": "Disabling x-client-CPU (#4337)\n\nCo-authored-by: trwalke <trwalke@microsoft.com>",
          "timestamp": "2023-10-05T08:16:41Z",
          "tree_id": "41b84f6aa61c6b639b41d2fbf9b4f36b5cf13479",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/b259cf00936a11a9cff789bf094935d8d31aea7f"
        },
        "date": 1696494232078,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 22249.377184186662,
            "unit": "ns",
            "range": "± 56.58902043925598"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 248560.94410807293,
            "unit": "ns",
            "range": "± 968.3273610798785"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 48027.537329101564,
            "unit": "ns",
            "range": "± 46.344073776775836"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 249265.12496744792,
            "unit": "ns",
            "range": "± 1368.055812174001"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "travis.walker@microsoft.com",
            "name": "Travis Walker",
            "username": "trwalke"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "b259cf00936a11a9cff789bf094935d8d31aea7f",
          "message": "Disabling x-client-CPU (#4337)\n\nCo-authored-by: trwalke <trwalke@microsoft.com>",
          "timestamp": "2023-10-05T08:16:41Z",
          "tree_id": "41b84f6aa61c6b639b41d2fbf9b4f36b5cf13479",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/b259cf00936a11a9cff789bf094935d8d31aea7f"
        },
        "date": 1696494232078,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 22249.377184186662,
            "unit": "ns",
            "range": "± 56.58902043925598"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 248560.94410807293,
            "unit": "ns",
            "range": "± 968.3273610798785"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 48027.537329101564,
            "unit": "ns",
            "range": "± 46.344073776775836"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 249265.12496744792,
            "unit": "ns",
            "range": "± 1368.055812174001"
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
          "id": "474026f56b1c3dfe2628fea104b8c80c46ce7e57",
          "message": "Throw ArgumentException for malformed authorities (#4347)\n\n* Throw argument exceptions for consistency when authority or tenant are invalid. Update method comments.\r\n\r\n* Update and add tests.\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Den Delimarsky <53200638+localden@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Small test fixes.\r\n\r\n* Update comments.\r\n\r\n---------\r\n\r\nCo-authored-by: Den Delimarsky <53200638+localden@users.noreply.github.com>\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>",
          "timestamp": "2023-10-10T17:11:08-07:00",
          "tree_id": "5489e2487eb172ef71a482b92a3b09aa4b60fde3",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/474026f56b1c3dfe2628fea104b8c80c46ce7e57"
        },
        "date": 1696983672655,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 25554.77113647461,
            "unit": "ns",
            "range": "± 99.88682557405329"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 319719.9439453125,
            "unit": "ns",
            "range": "± 4695.172016176"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 52526.35990397135,
            "unit": "ns",
            "range": "± 45.63704483074816"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 319454.60857282364,
            "unit": "ns",
            "range": "± 3088.7276798427933"
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
          "id": "474026f56b1c3dfe2628fea104b8c80c46ce7e57",
          "message": "Throw ArgumentException for malformed authorities (#4347)\n\n* Throw argument exceptions for consistency when authority or tenant are invalid. Update method comments.\r\n\r\n* Update and add tests.\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Den Delimarsky <53200638+localden@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Small test fixes.\r\n\r\n* Update comments.\r\n\r\n---------\r\n\r\nCo-authored-by: Den Delimarsky <53200638+localden@users.noreply.github.com>\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>",
          "timestamp": "2023-10-10T17:11:08-07:00",
          "tree_id": "5489e2487eb172ef71a482b92a3b09aa4b60fde3",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/474026f56b1c3dfe2628fea104b8c80c46ce7e57"
        },
        "date": 1696983672655,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 25554.77113647461,
            "unit": "ns",
            "range": "± 99.88682557405329"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 319719.9439453125,
            "unit": "ns",
            "range": "± 4695.172016176"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 52526.35990397135,
            "unit": "ns",
            "range": "± 45.63704483074816"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 319454.60857282364,
            "unit": "ns",
            "range": "± 3088.7276798427933"
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
          "id": "474026f56b1c3dfe2628fea104b8c80c46ce7e57",
          "message": "Throw ArgumentException for malformed authorities (#4347)\n\n* Throw argument exceptions for consistency when authority or tenant are invalid. Update method comments.\r\n\r\n* Update and add tests.\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Den Delimarsky <53200638+localden@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Small test fixes.\r\n\r\n* Update comments.\r\n\r\n---------\r\n\r\nCo-authored-by: Den Delimarsky <53200638+localden@users.noreply.github.com>\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>",
          "timestamp": "2023-10-11T00:11:08Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/474026f56b1c3dfe2628fea104b8c80c46ce7e57"
        },
        "date": 1697004639837,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 23558.57816205706,
            "unit": "ns",
            "range": "± 44.54434574700773"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 285848.8853190104,
            "unit": "ns",
            "range": "± 4180.34975791758"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 50243.17415945871,
            "unit": "ns",
            "range": "± 48.378099879252225"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 297243.56588541664,
            "unit": "ns",
            "range": "± 2348.733171054384"
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
          "id": "474026f56b1c3dfe2628fea104b8c80c46ce7e57",
          "message": "Throw ArgumentException for malformed authorities (#4347)\n\n* Throw argument exceptions for consistency when authority or tenant are invalid. Update method comments.\r\n\r\n* Update and add tests.\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Den Delimarsky <53200638+localden@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Small test fixes.\r\n\r\n* Update comments.\r\n\r\n---------\r\n\r\nCo-authored-by: Den Delimarsky <53200638+localden@users.noreply.github.com>\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>",
          "timestamp": "2023-10-11T00:11:08Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/474026f56b1c3dfe2628fea104b8c80c46ce7e57"
        },
        "date": 1697004639837,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 23558.57816205706,
            "unit": "ns",
            "range": "± 44.54434574700773"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 285848.8853190104,
            "unit": "ns",
            "range": "± 4180.34975791758"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 50243.17415945871,
            "unit": "ns",
            "range": "± 48.378099879252225"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 297243.56588541664,
            "unit": "ns",
            "range": "± 2348.733171054384"
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
          "id": "8bdf6990fe9ad679adabcd1e7b008c3e38a75d3f",
          "message": "Fix comments warnings. (#4373)",
          "timestamp": "2023-10-12T21:14:28Z",
          "tree_id": "fc9a99ce441fb47274b6d000f90dfcea10f0698f",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/8bdf6990fe9ad679adabcd1e7b008c3e38a75d3f"
        },
        "date": 1697145735222,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 22607.842427571613,
            "unit": "ns",
            "range": "± 112.63808508412279"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 277414.3603190104,
            "unit": "ns",
            "range": "± 3693.61991586272"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 50123.047045898435,
            "unit": "ns",
            "range": "± 154.61870064251085"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 276447.5130859375,
            "unit": "ns",
            "range": "± 3866.704201953475"
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
          "id": "8bdf6990fe9ad679adabcd1e7b008c3e38a75d3f",
          "message": "Fix comments warnings. (#4373)",
          "timestamp": "2023-10-12T21:14:28Z",
          "tree_id": "fc9a99ce441fb47274b6d000f90dfcea10f0698f",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/8bdf6990fe9ad679adabcd1e7b008c3e38a75d3f"
        },
        "date": 1697145735222,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 22607.842427571613,
            "unit": "ns",
            "range": "± 112.63808508412279"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 277414.3603190104,
            "unit": "ns",
            "range": "± 3693.61991586272"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 50123.047045898435,
            "unit": "ns",
            "range": "± 154.61870064251085"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 276447.5130859375,
            "unit": "ns",
            "range": "± 3866.704201953475"
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
          "id": "a8b93d42c8083097bfa452d313504e92d1ebc414",
          "message": "Print refresh token cache keys to logs (#4375)\n\n* Print RTs to cache.\r\n\r\n* Add tests.",
          "timestamp": "2023-10-16T12:15:40-07:00",
          "tree_id": "0e6d34d0b69bca92a076c60a4f162dbf59a232b7",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/a8b93d42c8083097bfa452d313504e92d1ebc414"
        },
        "date": 1697484266929,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 26886.9783299764,
            "unit": "ns",
            "range": "± 309.7224860958522"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 289543.4922736673,
            "unit": "ns",
            "range": "± 5864.555643334503"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 57011.13904244559,
            "unit": "ns",
            "range": "± 919.3998597859346"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 277259.33707682294,
            "unit": "ns",
            "range": "± 4661.52417665846"
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
          "id": "a8b93d42c8083097bfa452d313504e92d1ebc414",
          "message": "Print refresh token cache keys to logs (#4375)\n\n* Print RTs to cache.\r\n\r\n* Add tests.",
          "timestamp": "2023-10-16T12:15:40-07:00",
          "tree_id": "0e6d34d0b69bca92a076c60a4f162dbf59a232b7",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/a8b93d42c8083097bfa452d313504e92d1ebc414"
        },
        "date": 1697484266929,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 26886.9783299764,
            "unit": "ns",
            "range": "± 309.7224860958522"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 289543.4922736673,
            "unit": "ns",
            "range": "± 5864.555643334503"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 57011.13904244559,
            "unit": "ns",
            "range": "± 919.3998597859346"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 277259.33707682294,
            "unit": "ns",
            "range": "± 4661.52417665846"
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
          "id": "f951d8418e0a9fcb011c1cd7cfbcfec5b8df0962",
          "message": "Add serialization of broker error data in exceptions (#4376)\n\n* Add serialization of broker error.\r\n\r\n* Refactor.\r\n\r\n* Fix.\r\n\r\n* PR feedback.\r\n\r\n* Naming change.",
          "timestamp": "2023-10-16T15:21:08-07:00",
          "tree_id": "85c04e69f87e6728882ceb75044aad7a206c196c",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/f951d8418e0a9fcb011c1cd7cfbcfec5b8df0962"
        },
        "date": 1697495313468,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 22713.490240478517,
            "unit": "ns",
            "range": "± 112.80265928867681"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 253693.815952846,
            "unit": "ns",
            "range": "± 1628.7439047637656"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 47991.00689227764,
            "unit": "ns",
            "range": "± 58.36876992616688"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 256007.27227313703,
            "unit": "ns",
            "range": "± 855.1786744247387"
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
          "id": "f951d8418e0a9fcb011c1cd7cfbcfec5b8df0962",
          "message": "Add serialization of broker error data in exceptions (#4376)\n\n* Add serialization of broker error.\r\n\r\n* Refactor.\r\n\r\n* Fix.\r\n\r\n* PR feedback.\r\n\r\n* Naming change.",
          "timestamp": "2023-10-16T15:21:08-07:00",
          "tree_id": "85c04e69f87e6728882ceb75044aad7a206c196c",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/f951d8418e0a9fcb011c1cd7cfbcfec5b8df0962"
        },
        "date": 1697495313468,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 22713.490240478517,
            "unit": "ns",
            "range": "± 112.80265928867681"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 253693.815952846,
            "unit": "ns",
            "range": "± 1628.7439047637656"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 47991.00689227764,
            "unit": "ns",
            "range": "± 58.36876992616688"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 256007.27227313703,
            "unit": "ns",
            "range": "± 855.1786744247387"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "83938949+ashok672@users.noreply.github.com",
            "name": "Ashok Kumar Ramakrishnan",
            "username": "ashok672"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "4b0b6cdc73e079cc6a73c20f3ab01058da29e2b0",
          "message": "Fall back to local cache if broker fails to return result for silent token call  (#4395)\n\n* Update SilentRequest.cs\r\n\r\n* Fix failing unit tests",
          "timestamp": "2023-11-02T12:10:02Z",
          "tree_id": "7c1ab0ab79280c1535ba0365d55bca60c0e05432",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/4b0b6cdc73e079cc6a73c20f3ab01058da29e2b0"
        },
        "date": 1698927437067,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 22680.579357910156,
            "unit": "ns",
            "range": "± 110.9969598776772"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 253145.35152994792,
            "unit": "ns",
            "range": "± 1464.601053868588"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 48133.648864746094,
            "unit": "ns",
            "range": "± 155.56104424385447"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 257501.8277018229,
            "unit": "ns",
            "range": "± 1771.7335292990363"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "83938949+ashok672@users.noreply.github.com",
            "name": "Ashok Kumar Ramakrishnan",
            "username": "ashok672"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "4b0b6cdc73e079cc6a73c20f3ab01058da29e2b0",
          "message": "Fall back to local cache if broker fails to return result for silent token call  (#4395)\n\n* Update SilentRequest.cs\r\n\r\n* Fix failing unit tests",
          "timestamp": "2023-11-02T12:10:02Z",
          "tree_id": "7c1ab0ab79280c1535ba0365d55bca60c0e05432",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/4b0b6cdc73e079cc6a73c20f3ab01058da29e2b0"
        },
        "date": 1698927437067,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 22680.579357910156,
            "unit": "ns",
            "range": "± 110.9969598776772"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 253145.35152994792,
            "unit": "ns",
            "range": "± 1464.601053868588"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 48133.648864746094,
            "unit": "ns",
            "range": "± 155.56104424385447"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 257501.8277018229,
            "unit": "ns",
            "range": "± 1771.7335292990363"
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
          "id": "172a869d926f243a2c65cc6c63610a6de5f53de7",
          "message": "Deprecate request.WithAuthority (#4406)\n\n* Deprecate request.WithAuthority\r\n\r\n* Deprecate request.WithAuthority\r\n\r\n* fix tests\r\n\r\n* fix",
          "timestamp": "2023-11-06T14:35:37-08:00",
          "tree_id": "41c5124d5bc6117ba67d18a9615f8881cb5cef2f",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/172a869d926f243a2c65cc6c63610a6de5f53de7"
        },
        "date": 1699637968412,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 17387.954293387276,
            "unit": "ns",
            "range": "± 167.5728326224646"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 197451.0981968471,
            "unit": "ns",
            "range": "± 1388.785802103284"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 30126.29285975865,
            "unit": "ns",
            "range": "± 182.18030636593429"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 193361.97921316963,
            "unit": "ns",
            "range": "± 937.8553675537911"
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
          "id": "172a869d926f243a2c65cc6c63610a6de5f53de7",
          "message": "Deprecate request.WithAuthority (#4406)\n\n* Deprecate request.WithAuthority\r\n\r\n* Deprecate request.WithAuthority\r\n\r\n* fix tests\r\n\r\n* fix",
          "timestamp": "2023-11-06T14:35:37-08:00",
          "tree_id": "41c5124d5bc6117ba67d18a9615f8881cb5cef2f",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/172a869d926f243a2c65cc6c63610a6de5f53de7"
        },
        "date": 1699637968412,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 17387.954293387276,
            "unit": "ns",
            "range": "± 167.5728326224646"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 197451.0981968471,
            "unit": "ns",
            "range": "± 1388.785802103284"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 30126.29285975865,
            "unit": "ns",
            "range": "± 182.18030636593429"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 193361.97921316963,
            "unit": "ns",
            "range": "± 937.8553675537911"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "43250667+iulico-1@users.noreply.github.com",
            "name": "Iulian C",
            "username": "iulico-1"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "da67f067f14b26fdfb928178ad8b21bcdcde91a9",
          "message": "Add support for MSA passthrough for silent token acquisition calls through the boker (#4419)\n\nAdd support for MSA passthrough for silent token acquisition calls through the boker (#4419)",
          "timestamp": "2023-11-17T09:02:38-08:00",
          "tree_id": "15e73a063233ee0b0f3923f0dadd48a269fc59d3",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/da67f067f14b26fdfb928178ad8b21bcdcde91a9"
        },
        "date": 1700240979785,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 16619.242685171273,
            "unit": "ns",
            "range": "± 116.8348614929289"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 189823.1977887835,
            "unit": "ns",
            "range": "± 1281.7069959073797"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 30117.961606852212,
            "unit": "ns",
            "range": "± 157.29910350648206"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 199519.3994140625,
            "unit": "ns",
            "range": "± 1348.4879923132685"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "43250667+iulico-1@users.noreply.github.com",
            "name": "Iulian C",
            "username": "iulico-1"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "da67f067f14b26fdfb928178ad8b21bcdcde91a9",
          "message": "Add support for MSA passthrough for silent token acquisition calls through the boker (#4419)\n\nAdd support for MSA passthrough for silent token acquisition calls through the boker (#4419)",
          "timestamp": "2023-11-17T09:02:38-08:00",
          "tree_id": "15e73a063233ee0b0f3923f0dadd48a269fc59d3",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/da67f067f14b26fdfb928178ad8b21bcdcde91a9"
        },
        "date": 1700240979785,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 16619.242685171273,
            "unit": "ns",
            "range": "± 116.8348614929289"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 189823.1977887835,
            "unit": "ns",
            "range": "± 1281.7069959073797"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 30117.961606852212,
            "unit": "ns",
            "range": "± 157.29910350648206"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 199519.3994140625,
            "unit": "ns",
            "range": "± 1348.4879923132685"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "61847233+neha-bhargava@users.noreply.github.com",
            "name": "Neha Bhargava",
            "username": "neha-bhargava"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "9792dc6d7e7459a1d52ee0fda28e20dbd57111d8",
          "message": "Cache level should be L1 in case of default in memory cache. (#4423)\n\nCache level should be L1 in case of default in memory cache",
          "timestamp": "2023-11-17T11:30:35-08:00",
          "tree_id": "1d7c2f47b8b9893dd334d2726f49cd6580541a6a",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/9792dc6d7e7459a1d52ee0fda28e20dbd57111d8"
        },
        "date": 1700249877361,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 17310.47155526968,
            "unit": "ns",
            "range": "± 197.63654120614683"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 194859.9375174386,
            "unit": "ns",
            "range": "± 326.8540908555079"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 30465.417223249162,
            "unit": "ns",
            "range": "± 228.84251864668957"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 199857.91134207588,
            "unit": "ns",
            "range": "± 1613.0665288081707"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "61847233+neha-bhargava@users.noreply.github.com",
            "name": "Neha Bhargava",
            "username": "neha-bhargava"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "9792dc6d7e7459a1d52ee0fda28e20dbd57111d8",
          "message": "Cache level should be L1 in case of default in memory cache. (#4423)\n\nCache level should be L1 in case of default in memory cache",
          "timestamp": "2023-11-17T11:30:35-08:00",
          "tree_id": "1d7c2f47b8b9893dd334d2726f49cd6580541a6a",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/9792dc6d7e7459a1d52ee0fda28e20dbd57111d8"
        },
        "date": 1700249877361,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 17310.47155526968,
            "unit": "ns",
            "range": "± 197.63654120614683"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 194859.9375174386,
            "unit": "ns",
            "range": "± 326.8540908555079"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 30465.417223249162,
            "unit": "ns",
            "range": "± 228.84251864668957"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 199857.91134207588,
            "unit": "ns",
            "range": "± 1613.0665288081707"
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
          "id": "067159b7259f648798d0762a72b24ef4b3d5c8a3",
          "message": "Use net6.0",
          "timestamp": "2024-01-24T05:19:44Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/067159b7259f648798d0762a72b24ef4b3d5c8a3"
        },
        "date": 1706074020042,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 12553.941260782878,
            "unit": "ns",
            "range": "± 131.56053442812143"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 119379.61663411459,
            "unit": "ns",
            "range": "± 1258.212495870313"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 23221.126222737632,
            "unit": "ns",
            "range": "± 127.36991965959028"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 124186.66906738281,
            "unit": "ns",
            "range": "± 3145.3518079185665"
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
          "id": "067159b7259f648798d0762a72b24ef4b3d5c8a3",
          "message": "Use net6.0",
          "timestamp": "2024-01-24T05:19:44Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/067159b7259f648798d0762a72b24ef4b3d5c8a3"
        },
        "date": 1706074020042,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 12553.941260782878,
            "unit": "ns",
            "range": "± 131.56053442812143"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 119379.61663411459,
            "unit": "ns",
            "range": "± 1258.212495870313"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 23221.126222737632,
            "unit": "ns",
            "range": "± 127.36991965959028"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 124186.66906738281,
            "unit": "ns",
            "range": "± 3145.3518079185665"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "simon.cropp@gmail.com",
            "name": "Simon Cropp",
            "username": "SimonCropp"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "16827a2ccbfcaa607de7295520b5f097446c261a",
          "message": "reduce some string alloc on building (#4564)",
          "timestamp": "2024-01-24T08:51:29Z",
          "tree_id": "2559765585e66470a99924346ae757d15c86668f",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/16827a2ccbfcaa607de7295520b5f097446c261a"
        },
        "date": 1706086724358,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 12410.744950358074,
            "unit": "ns",
            "range": "± 79.09418421025937"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 120025.60974934897,
            "unit": "ns",
            "range": "± 1145.9657156987241"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 23279.261526254508,
            "unit": "ns",
            "range": "± 96.28979117119424"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 120056.35615030925,
            "unit": "ns",
            "range": "± 277.5509416895535"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "simon.cropp@gmail.com",
            "name": "Simon Cropp",
            "username": "SimonCropp"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "16827a2ccbfcaa607de7295520b5f097446c261a",
          "message": "reduce some string alloc on building (#4564)",
          "timestamp": "2024-01-24T08:51:29Z",
          "tree_id": "2559765585e66470a99924346ae757d15c86668f",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/16827a2ccbfcaa607de7295520b5f097446c261a"
        },
        "date": 1706086724358,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 12410.744950358074,
            "unit": "ns",
            "range": "± 79.09418421025937"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 120025.60974934897,
            "unit": "ns",
            "range": "± 1145.9657156987241"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 23279.261526254508,
            "unit": "ns",
            "range": "± 96.28979117119424"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 120056.35615030925,
            "unit": "ns",
            "range": "± 277.5509416895535"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "simon.cropp@gmail.com",
            "name": "Simon Cropp",
            "username": "SimonCropp"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "1f999fb8520ae1bc0062ac77fe686c241c1eb541",
          "message": "remove unreachable code in GetParsedAuthValueElements (#4541)",
          "timestamp": "2024-01-24T09:41:02Z",
          "tree_id": "89f97b9ffd703f2b73f57c1daee8a46414d1995c",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/1f999fb8520ae1bc0062ac77fe686c241c1eb541"
        },
        "date": 1706089707102,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 12207.340477576623,
            "unit": "ns",
            "range": "± 27.711351798295688"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 119745.36141531808,
            "unit": "ns",
            "range": "± 380.370046250605"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 22693.804003034318,
            "unit": "ns",
            "range": "± 158.82217066969875"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 124276.94586736505,
            "unit": "ns",
            "range": "± 2981.6852591050656"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "simon.cropp@gmail.com",
            "name": "Simon Cropp",
            "username": "SimonCropp"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "1f999fb8520ae1bc0062ac77fe686c241c1eb541",
          "message": "remove unreachable code in GetParsedAuthValueElements (#4541)",
          "timestamp": "2024-01-24T09:41:02Z",
          "tree_id": "89f97b9ffd703f2b73f57c1daee8a46414d1995c",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/1f999fb8520ae1bc0062ac77fe686c241c1eb541"
        },
        "date": 1706089707102,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 12207.340477576623,
            "unit": "ns",
            "range": "± 27.711351798295688"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 119745.36141531808,
            "unit": "ns",
            "range": "± 380.370046250605"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 22693.804003034318,
            "unit": "ns",
            "range": "± 158.82217066969875"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForClientCacheTests.AcquireTokenForClient_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 124276.94586736505,
            "unit": "ns",
            "range": "± 2981.6852591050656"
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
          "id": "d25f45f7e356bd92986e412dd68b5d341343aa6a",
          "message": "Set CacheSynchronization to true by default (#4310)\n\n* Change EnableCacheSynchronization default.\r\n\r\n* Update tests.\r\n\r\n* Update method comment.\r\n\r\n* Update ConfidentialClientApplicationBuilder.cs\r\n\r\n* Update ConfidentialClientApplicationOptions.cs\r\n\r\n---------\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>",
          "timestamp": "2023-08-30T18:32:08Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/d25f45f7e356bd92986e412dd68b5d341343aa6a"
        },
        "date": 1693422362762,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 40536.43046061198,
            "unit": "ns",
            "range": "± 243.7530595364035"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 377143.1552385603,
            "unit": "ns",
            "range": "± 2749.6969195917322"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 69171.73082624163,
            "unit": "ns",
            "range": "± 298.0690518493377"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 389395.3129507211,
            "unit": "ns",
            "range": "± 1718.3088732655776"
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
          "id": "d25f45f7e356bd92986e412dd68b5d341343aa6a",
          "message": "Set CacheSynchronization to true by default (#4310)\n\n* Change EnableCacheSynchronization default.\r\n\r\n* Update tests.\r\n\r\n* Update method comment.\r\n\r\n* Update ConfidentialClientApplicationBuilder.cs\r\n\r\n* Update ConfidentialClientApplicationOptions.cs\r\n\r\n---------\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>",
          "timestamp": "2023-08-30T18:32:08Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/d25f45f7e356bd92986e412dd68b5d341343aa6a"
        },
        "date": 1693422362762,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 40536.43046061198,
            "unit": "ns",
            "range": "± 243.7530595364035"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 377143.1552385603,
            "unit": "ns",
            "range": "± 2749.6969195917322"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 69171.73082624163,
            "unit": "ns",
            "range": "± 298.0690518493377"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 389395.3129507211,
            "unit": "ns",
            "range": "± 1718.3088732655776"
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
          "id": "29cbbb0ebe4361c98bcfaf9f00092c1517939ea3",
          "message": "Expose WAM telemetry in AuthenticationResult and exceptions (#4307)\n\n* Add WAM telemetry to AuthResult\r\n\r\n* Add wam telemetry to exceptions\r\n\r\n* Address PR chages\r\n\r\n* Add WAM telemetry to AuthResult\r\n\r\n* Add wam telemetry to exceptions\r\n\r\n* Address PR chages\r\n\r\n* Fix naming\r\n\r\n* fix\r\n\r\n* Rename\r\n\r\n* Fix",
          "timestamp": "2023-09-05T12:44:21+01:00",
          "tree_id": "abfe00ce5ef36f4d45737e9a537369ba544de73a",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/29cbbb0ebe4361c98bcfaf9f00092c1517939ea3"
        },
        "date": 1693914758031,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 40962.84500732422,
            "unit": "ns",
            "range": "± 257.50196647494766"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 379541.28834635415,
            "unit": "ns",
            "range": "± 2134.008904448864"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 67891.01722005209,
            "unit": "ns",
            "range": "± 223.95142426338404"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 385983.75081380206,
            "unit": "ns",
            "range": "± 3484.462640846443"
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
          "id": "29cbbb0ebe4361c98bcfaf9f00092c1517939ea3",
          "message": "Expose WAM telemetry in AuthenticationResult and exceptions (#4307)\n\n* Add WAM telemetry to AuthResult\r\n\r\n* Add wam telemetry to exceptions\r\n\r\n* Address PR chages\r\n\r\n* Add WAM telemetry to AuthResult\r\n\r\n* Add wam telemetry to exceptions\r\n\r\n* Address PR chages\r\n\r\n* Fix naming\r\n\r\n* fix\r\n\r\n* Rename\r\n\r\n* Fix",
          "timestamp": "2023-09-05T12:44:21+01:00",
          "tree_id": "abfe00ce5ef36f4d45737e9a537369ba544de73a",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/29cbbb0ebe4361c98bcfaf9f00092c1517939ea3"
        },
        "date": 1693914758031,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 40962.84500732422,
            "unit": "ns",
            "range": "± 257.50196647494766"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 379541.28834635415,
            "unit": "ns",
            "range": "± 2134.008904448864"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 67891.01722005209,
            "unit": "ns",
            "range": "± 223.95142426338404"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 385983.75081380206,
            "unit": "ns",
            "range": "± 3484.462640846443"
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
          "id": "90ae6b9c6d33655a66cbc6413e8aadee487f052a",
          "message": "Check for null and empty scopes in managed identity (#4328)\n\n* managed identity fix\r\n\r\n* ArgumentNullException\r\n\r\n* fix\r\n\r\n* exception\r\n\r\n---------\r\n\r\nCo-authored-by: Gladwin Johnson <gljohns@microsoft.com>",
          "timestamp": "2023-09-06T17:15:40Z",
          "tree_id": "d1247ec22650b6f61de984ce7e3f7f5efbca8d89",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/90ae6b9c6d33655a66cbc6413e8aadee487f052a"
        },
        "date": 1694031593301,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 41007.99698486328,
            "unit": "ns",
            "range": "± 158.84691903656372"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 350021.70455496653,
            "unit": "ns",
            "range": "± 1597.6852758537943"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 66835.70348307291,
            "unit": "ns",
            "range": "± 264.17405088374545"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 358212.3896484375,
            "unit": "ns",
            "range": "± 1701.5386687332066"
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
          "id": "90ae6b9c6d33655a66cbc6413e8aadee487f052a",
          "message": "Check for null and empty scopes in managed identity (#4328)\n\n* managed identity fix\r\n\r\n* ArgumentNullException\r\n\r\n* fix\r\n\r\n* exception\r\n\r\n---------\r\n\r\nCo-authored-by: Gladwin Johnson <gljohns@microsoft.com>",
          "timestamp": "2023-09-06T17:15:40Z",
          "tree_id": "d1247ec22650b6f61de984ce7e3f7f5efbca8d89",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/90ae6b9c6d33655a66cbc6413e8aadee487f052a"
        },
        "date": 1694031593301,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 41007.99698486328,
            "unit": "ns",
            "range": "± 158.84691903656372"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 350021.70455496653,
            "unit": "ns",
            "range": "± 1597.6852758537943"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 66835.70348307291,
            "unit": "ns",
            "range": "± 264.17405088374545"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 358212.3896484375,
            "unit": "ns",
            "range": "± 1701.5386687332066"
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
          "id": "fe3c8eacfb013b73404b3295ec5a36b3a60b3ed8",
          "message": "Fix for WAM Runtime error logs duplicated info (#4354)\n\n* Update RuntimeBroker.cs\r\n\r\n* Update RuntimeBroker.cs",
          "timestamp": "2023-09-19T14:24:56-07:00",
          "tree_id": "e70a2e1aab89831139cede5cbd30c11f05df7ed4",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/fe3c8eacfb013b73404b3295ec5a36b3a60b3ed8"
        },
        "date": 1695159227501,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 41439.67785135905,
            "unit": "ns",
            "range": "± 50.1551022877857"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 399135.9881766183,
            "unit": "ns",
            "range": "± 999.5882498840451"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 68196.63931710379,
            "unit": "ns",
            "range": "± 81.19725627806586"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 399999.0217285156,
            "unit": "ns",
            "range": "± 1253.2479021609277"
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
          "id": "fe3c8eacfb013b73404b3295ec5a36b3a60b3ed8",
          "message": "Fix for WAM Runtime error logs duplicated info (#4354)\n\n* Update RuntimeBroker.cs\r\n\r\n* Update RuntimeBroker.cs",
          "timestamp": "2023-09-19T14:24:56-07:00",
          "tree_id": "e70a2e1aab89831139cede5cbd30c11f05df7ed4",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/fe3c8eacfb013b73404b3295ec5a36b3a60b3ed8"
        },
        "date": 1695159227501,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 41439.67785135905,
            "unit": "ns",
            "range": "± 50.1551022877857"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 399135.9881766183,
            "unit": "ns",
            "range": "± 999.5882498840451"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 68196.63931710379,
            "unit": "ns",
            "range": "± 81.19725627806586"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 399999.0217285156,
            "unit": "ns",
            "range": "± 1253.2479021609277"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "53200638+localden@users.noreply.github.com",
            "name": "Den Delimarsky",
            "username": "localden"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "b898ab8193bc751ae5aabe5ff7e5f079eccd3f85",
          "message": "Updates to documentation and minor code cleanup (#4339) (#4346)\n\n* Updates to documentation (#4339)\r\n\r\n* Update PublicClientApplication.cs\r\n\r\n* Update inheritdoc comments\r\n\r\n* Update documentation inheritance logic for PCA\r\n\r\n* Update documentation\r\n\r\n* Cleanup unused namespaces\r\n\r\n* Some extra cleanup\r\n\r\n* Update README.md\r\n\r\n* Documentation and minor cleanup\r\n\r\n* Update AuthenticationHeaderParser.cs\r\n\r\n* Update README.md\r\n\r\n* Update contributing documentation\r\n\r\nThis tackles the merge from https://github.com/MicrosoftDocs/microsoft-authentication-library-dotnet/pull/135\r\n\r\n* Roll back this change because we're building on .NET Fx, which doesn't support ranges\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update src/client/Microsoft.Identity.Client/AuthenticationHeaderParser.cs\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Update README.md\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Small documentation cleanup items\r\n\r\n* Update CONTRIBUTING.md\r\n\r\n* Cleanup\r\n\r\n* More cleanup - this time with some code\r\n\r\n* Update MsalServiceExceptionFactory.cs\r\n\r\n* Update MsalServiceExceptionFactory.cs\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Update src/client/Microsoft.Identity.Client/UserAssertion.cs\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Add missing Using statement\r\n\r\n---------\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\nCo-authored-by: Bogdan Gavril <bogavril@microsoft.com>",
          "timestamp": "2023-09-25T13:11:55Z",
          "tree_id": "b514ea57ab8de82c8840ca5149c4407d0b7a4c6a",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/b898ab8193bc751ae5aabe5ff7e5f079eccd3f85"
        },
        "date": 1695658998442,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 39903.60693359375,
            "unit": "ns",
            "range": "± 171.5800245223969"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 385094.9791015625,
            "unit": "ns",
            "range": "± 2356.0495236287065"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 66762.61635335286,
            "unit": "ns",
            "range": "± 77.17680313151855"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 386334.454031808,
            "unit": "ns",
            "range": "± 3304.4058203060636"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "53200638+localden@users.noreply.github.com",
            "name": "Den Delimarsky",
            "username": "localden"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "b898ab8193bc751ae5aabe5ff7e5f079eccd3f85",
          "message": "Updates to documentation and minor code cleanup (#4339) (#4346)\n\n* Updates to documentation (#4339)\r\n\r\n* Update PublicClientApplication.cs\r\n\r\n* Update inheritdoc comments\r\n\r\n* Update documentation inheritance logic for PCA\r\n\r\n* Update documentation\r\n\r\n* Cleanup unused namespaces\r\n\r\n* Some extra cleanup\r\n\r\n* Update README.md\r\n\r\n* Documentation and minor cleanup\r\n\r\n* Update AuthenticationHeaderParser.cs\r\n\r\n* Update README.md\r\n\r\n* Update contributing documentation\r\n\r\nThis tackles the merge from https://github.com/MicrosoftDocs/microsoft-authentication-library-dotnet/pull/135\r\n\r\n* Roll back this change because we're building on .NET Fx, which doesn't support ranges\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update src/client/Microsoft.Identity.Client/AuthenticationHeaderParser.cs\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Update README.md\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Small documentation cleanup items\r\n\r\n* Update CONTRIBUTING.md\r\n\r\n* Cleanup\r\n\r\n* More cleanup - this time with some code\r\n\r\n* Update MsalServiceExceptionFactory.cs\r\n\r\n* Update MsalServiceExceptionFactory.cs\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Update src/client/Microsoft.Identity.Client/UserAssertion.cs\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Add missing Using statement\r\n\r\n---------\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\nCo-authored-by: Bogdan Gavril <bogavril@microsoft.com>",
          "timestamp": "2023-09-25T13:11:55Z",
          "tree_id": "b514ea57ab8de82c8840ca5149c4407d0b7a4c6a",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/b898ab8193bc751ae5aabe5ff7e5f079eccd3f85"
        },
        "date": 1695658998442,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 39903.60693359375,
            "unit": "ns",
            "range": "± 171.5800245223969"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 385094.9791015625,
            "unit": "ns",
            "range": "± 2356.0495236287065"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 66762.61635335286,
            "unit": "ns",
            "range": "± 77.17680313151855"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 386334.454031808,
            "unit": "ns",
            "range": "± 3304.4058203060636"
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
          "id": "80c3be514c4bd59412b20b7c1f792452be3943ea",
          "message": "Add CacheLevel to AuthResultMetadata (#4349)",
          "timestamp": "2023-09-26T06:26:38Z",
          "tree_id": "94eabae8ba8aa46440d553e91a36efc0b90018e4",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/80c3be514c4bd59412b20b7c1f792452be3943ea"
        },
        "date": 1695710070246,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 38646.32294640174,
            "unit": "ns",
            "range": "± 103.43918875236636"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 341820.3372395833,
            "unit": "ns",
            "range": "± 1596.8071150951541"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 64288.453552246094,
            "unit": "ns",
            "range": "± 154.94131276321392"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 351862.67333984375,
            "unit": "ns",
            "range": "± 1670.9451234712242"
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
          "id": "80c3be514c4bd59412b20b7c1f792452be3943ea",
          "message": "Add CacheLevel to AuthResultMetadata (#4349)",
          "timestamp": "2023-09-26T06:26:38Z",
          "tree_id": "94eabae8ba8aa46440d553e91a36efc0b90018e4",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/80c3be514c4bd59412b20b7c1f792452be3943ea"
        },
        "date": 1695710070246,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 38646.32294640174,
            "unit": "ns",
            "range": "± 103.43918875236636"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 341820.3372395833,
            "unit": "ns",
            "range": "± 1596.8071150951541"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 64288.453552246094,
            "unit": "ns",
            "range": "± 154.94131276321392"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 351862.67333984375,
            "unit": "ns",
            "range": "± 1670.9451234712242"
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
          "id": "118ab93b5afb78c8c4a594dc27a3288725f76213",
          "message": "Add one retry to assertion signing. (#4365)\n\n* Add one retry to assertion signing.\r\n\r\n* Fix.",
          "timestamp": "2023-10-05T01:13:42Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/118ab93b5afb78c8c4a594dc27a3288725f76213"
        },
        "date": 1696485258496,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 39493.666115897046,
            "unit": "ns",
            "range": "± 64.15755937342873"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 356342.6777018229,
            "unit": "ns",
            "range": "± 2557.3367253010924"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 65768.09816800631,
            "unit": "ns",
            "range": "± 1789.7333044880097"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 351026.3820452009,
            "unit": "ns",
            "range": "± 1428.8282090697046"
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
          "id": "118ab93b5afb78c8c4a594dc27a3288725f76213",
          "message": "Add one retry to assertion signing. (#4365)\n\n* Add one retry to assertion signing.\r\n\r\n* Fix.",
          "timestamp": "2023-10-05T01:13:42Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/118ab93b5afb78c8c4a594dc27a3288725f76213"
        },
        "date": 1696485258496,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 39493.666115897046,
            "unit": "ns",
            "range": "± 64.15755937342873"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 356342.6777018229,
            "unit": "ns",
            "range": "± 2557.3367253010924"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 65768.09816800631,
            "unit": "ns",
            "range": "± 1789.7333044880097"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 351026.3820452009,
            "unit": "ns",
            "range": "± 1428.8282090697046"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "travis.walker@microsoft.com",
            "name": "Travis Walker",
            "username": "trwalke"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "b259cf00936a11a9cff789bf094935d8d31aea7f",
          "message": "Disabling x-client-CPU (#4337)\n\nCo-authored-by: trwalke <trwalke@microsoft.com>",
          "timestamp": "2023-10-05T08:16:41Z",
          "tree_id": "41b84f6aa61c6b639b41d2fbf9b4f36b5cf13479",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/b259cf00936a11a9cff789bf094935d8d31aea7f"
        },
        "date": 1696494236351,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 38882.42126900809,
            "unit": "ns",
            "range": "± 129.37604895516208"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 346750.1705845424,
            "unit": "ns",
            "range": "± 676.3225846891324"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 65161.90284075056,
            "unit": "ns",
            "range": "± 62.57775916421441"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 355657.9146484375,
            "unit": "ns",
            "range": "± 1093.6919549978359"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "travis.walker@microsoft.com",
            "name": "Travis Walker",
            "username": "trwalke"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "b259cf00936a11a9cff789bf094935d8d31aea7f",
          "message": "Disabling x-client-CPU (#4337)\n\nCo-authored-by: trwalke <trwalke@microsoft.com>",
          "timestamp": "2023-10-05T08:16:41Z",
          "tree_id": "41b84f6aa61c6b639b41d2fbf9b4f36b5cf13479",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/b259cf00936a11a9cff789bf094935d8d31aea7f"
        },
        "date": 1696494236351,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 38882.42126900809,
            "unit": "ns",
            "range": "± 129.37604895516208"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 346750.1705845424,
            "unit": "ns",
            "range": "± 676.3225846891324"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 65161.90284075056,
            "unit": "ns",
            "range": "± 62.57775916421441"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 355657.9146484375,
            "unit": "ns",
            "range": "± 1093.6919549978359"
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
          "id": "474026f56b1c3dfe2628fea104b8c80c46ce7e57",
          "message": "Throw ArgumentException for malformed authorities (#4347)\n\n* Throw argument exceptions for consistency when authority or tenant are invalid. Update method comments.\r\n\r\n* Update and add tests.\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Den Delimarsky <53200638+localden@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Small test fixes.\r\n\r\n* Update comments.\r\n\r\n---------\r\n\r\nCo-authored-by: Den Delimarsky <53200638+localden@users.noreply.github.com>\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>",
          "timestamp": "2023-10-10T17:11:08-07:00",
          "tree_id": "5489e2487eb172ef71a482b92a3b09aa4b60fde3",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/474026f56b1c3dfe2628fea104b8c80c46ce7e57"
        },
        "date": 1696983676166,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 44257.0834917341,
            "unit": "ns",
            "range": "± 309.3992350392304"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 440307.2558268229,
            "unit": "ns",
            "range": "± 4748.035247271118"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 71598.68501164363,
            "unit": "ns",
            "range": "± 62.43065910505695"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 447297.14794921875,
            "unit": "ns",
            "range": "± 4608.781143281643"
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
          "id": "474026f56b1c3dfe2628fea104b8c80c46ce7e57",
          "message": "Throw ArgumentException for malformed authorities (#4347)\n\n* Throw argument exceptions for consistency when authority or tenant are invalid. Update method comments.\r\n\r\n* Update and add tests.\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Den Delimarsky <53200638+localden@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Small test fixes.\r\n\r\n* Update comments.\r\n\r\n---------\r\n\r\nCo-authored-by: Den Delimarsky <53200638+localden@users.noreply.github.com>\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>",
          "timestamp": "2023-10-10T17:11:08-07:00",
          "tree_id": "5489e2487eb172ef71a482b92a3b09aa4b60fde3",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/474026f56b1c3dfe2628fea104b8c80c46ce7e57"
        },
        "date": 1696983676166,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 44257.0834917341,
            "unit": "ns",
            "range": "± 309.3992350392304"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 440307.2558268229,
            "unit": "ns",
            "range": "± 4748.035247271118"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 71598.68501164363,
            "unit": "ns",
            "range": "± 62.43065910505695"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 447297.14794921875,
            "unit": "ns",
            "range": "± 4608.781143281643"
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
          "id": "474026f56b1c3dfe2628fea104b8c80c46ce7e57",
          "message": "Throw ArgumentException for malformed authorities (#4347)\n\n* Throw argument exceptions for consistency when authority or tenant are invalid. Update method comments.\r\n\r\n* Update and add tests.\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Den Delimarsky <53200638+localden@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Small test fixes.\r\n\r\n* Update comments.\r\n\r\n---------\r\n\r\nCo-authored-by: Den Delimarsky <53200638+localden@users.noreply.github.com>\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>",
          "timestamp": "2023-10-11T00:11:08Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/474026f56b1c3dfe2628fea104b8c80c46ce7e57"
        },
        "date": 1697004643355,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 40917.357857840405,
            "unit": "ns",
            "range": "± 57.951017779683845"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 399201.66653878347,
            "unit": "ns",
            "range": "± 1146.0534259845012"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 67940.73595319476,
            "unit": "ns",
            "range": "± 79.89405303188587"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 395768.505859375,
            "unit": "ns",
            "range": "± 2364.831621718528"
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
          "id": "474026f56b1c3dfe2628fea104b8c80c46ce7e57",
          "message": "Throw ArgumentException for malformed authorities (#4347)\n\n* Throw argument exceptions for consistency when authority or tenant are invalid. Update method comments.\r\n\r\n* Update and add tests.\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Den Delimarsky <53200638+localden@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Small test fixes.\r\n\r\n* Update comments.\r\n\r\n---------\r\n\r\nCo-authored-by: Den Delimarsky <53200638+localden@users.noreply.github.com>\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>",
          "timestamp": "2023-10-11T00:11:08Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/474026f56b1c3dfe2628fea104b8c80c46ce7e57"
        },
        "date": 1697004643355,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 40917.357857840405,
            "unit": "ns",
            "range": "± 57.951017779683845"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 399201.66653878347,
            "unit": "ns",
            "range": "± 1146.0534259845012"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 67940.73595319476,
            "unit": "ns",
            "range": "± 79.89405303188587"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 395768.505859375,
            "unit": "ns",
            "range": "± 2364.831621718528"
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
          "id": "8bdf6990fe9ad679adabcd1e7b008c3e38a75d3f",
          "message": "Fix comments warnings. (#4373)",
          "timestamp": "2023-10-12T21:14:28Z",
          "tree_id": "fc9a99ce441fb47274b6d000f90dfcea10f0698f",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/8bdf6990fe9ad679adabcd1e7b008c3e38a75d3f"
        },
        "date": 1697145738581,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 39689.52546386719,
            "unit": "ns",
            "range": "± 177.00035713713538"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 376088.46156529017,
            "unit": "ns",
            "range": "± 2917.7884627311223"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 67104.18908691406,
            "unit": "ns",
            "range": "± 255.32152229505147"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 391112.26263020834,
            "unit": "ns",
            "range": "± 4227.956949777226"
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
          "id": "8bdf6990fe9ad679adabcd1e7b008c3e38a75d3f",
          "message": "Fix comments warnings. (#4373)",
          "timestamp": "2023-10-12T21:14:28Z",
          "tree_id": "fc9a99ce441fb47274b6d000f90dfcea10f0698f",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/8bdf6990fe9ad679adabcd1e7b008c3e38a75d3f"
        },
        "date": 1697145738581,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 39689.52546386719,
            "unit": "ns",
            "range": "± 177.00035713713538"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 376088.46156529017,
            "unit": "ns",
            "range": "± 2917.7884627311223"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 67104.18908691406,
            "unit": "ns",
            "range": "± 255.32152229505147"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 391112.26263020834,
            "unit": "ns",
            "range": "± 4227.956949777226"
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
          "id": "a8b93d42c8083097bfa452d313504e92d1ebc414",
          "message": "Print refresh token cache keys to logs (#4375)\n\n* Print RTs to cache.\r\n\r\n* Add tests.",
          "timestamp": "2023-10-16T12:15:40-07:00",
          "tree_id": "0e6d34d0b69bca92a076c60a4f162dbf59a232b7",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/a8b93d42c8083097bfa452d313504e92d1ebc414"
        },
        "date": 1697484271372,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 47999.63027121804,
            "unit": "ns",
            "range": "± 1121.235652580753"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 399365.0455729167,
            "unit": "ns",
            "range": "± 6879.847686719751"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 77451.64072552849,
            "unit": "ns",
            "range": "± 1459.2900869304406"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 400135.4090820312,
            "unit": "ns",
            "range": "± 7411.790394130342"
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
          "id": "a8b93d42c8083097bfa452d313504e92d1ebc414",
          "message": "Print refresh token cache keys to logs (#4375)\n\n* Print RTs to cache.\r\n\r\n* Add tests.",
          "timestamp": "2023-10-16T12:15:40-07:00",
          "tree_id": "0e6d34d0b69bca92a076c60a4f162dbf59a232b7",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/a8b93d42c8083097bfa452d313504e92d1ebc414"
        },
        "date": 1697484271372,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 47999.63027121804,
            "unit": "ns",
            "range": "± 1121.235652580753"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 399365.0455729167,
            "unit": "ns",
            "range": "± 6879.847686719751"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 77451.64072552849,
            "unit": "ns",
            "range": "± 1459.2900869304406"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 400135.4090820312,
            "unit": "ns",
            "range": "± 7411.790394130342"
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
          "id": "f951d8418e0a9fcb011c1cd7cfbcfec5b8df0962",
          "message": "Add serialization of broker error data in exceptions (#4376)\n\n* Add serialization of broker error.\r\n\r\n* Refactor.\r\n\r\n* Fix.\r\n\r\n* PR feedback.\r\n\r\n* Naming change.",
          "timestamp": "2023-10-16T15:21:08-07:00",
          "tree_id": "85c04e69f87e6728882ceb75044aad7a206c196c",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/f951d8418e0a9fcb011c1cd7cfbcfec5b8df0962"
        },
        "date": 1697495318241,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 39411.9475789388,
            "unit": "ns",
            "range": "± 111.86072686414632"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 348605.3919921875,
            "unit": "ns",
            "range": "± 3010.1547699673697"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 64512.504865373885,
            "unit": "ns",
            "range": "± 84.98265568196693"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 361071.22398158483,
            "unit": "ns",
            "range": "± 1159.5060173049678"
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
          "id": "f951d8418e0a9fcb011c1cd7cfbcfec5b8df0962",
          "message": "Add serialization of broker error data in exceptions (#4376)\n\n* Add serialization of broker error.\r\n\r\n* Refactor.\r\n\r\n* Fix.\r\n\r\n* PR feedback.\r\n\r\n* Naming change.",
          "timestamp": "2023-10-16T15:21:08-07:00",
          "tree_id": "85c04e69f87e6728882ceb75044aad7a206c196c",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/f951d8418e0a9fcb011c1cd7cfbcfec5b8df0962"
        },
        "date": 1697495318241,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 39411.9475789388,
            "unit": "ns",
            "range": "± 111.86072686414632"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 348605.3919921875,
            "unit": "ns",
            "range": "± 3010.1547699673697"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 64512.504865373885,
            "unit": "ns",
            "range": "± 84.98265568196693"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 361071.22398158483,
            "unit": "ns",
            "range": "± 1159.5060173049678"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "83938949+ashok672@users.noreply.github.com",
            "name": "Ashok Kumar Ramakrishnan",
            "username": "ashok672"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "4b0b6cdc73e079cc6a73c20f3ab01058da29e2b0",
          "message": "Fall back to local cache if broker fails to return result for silent token call  (#4395)\n\n* Update SilentRequest.cs\r\n\r\n* Fix failing unit tests",
          "timestamp": "2023-11-02T12:10:02Z",
          "tree_id": "7c1ab0ab79280c1535ba0365d55bca60c0e05432",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/4b0b6cdc73e079cc6a73c20f3ab01058da29e2b0"
        },
        "date": 1698927442444,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 38897.65975516183,
            "unit": "ns",
            "range": "± 89.59935061185564"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 349342.5078125,
            "unit": "ns",
            "range": "± 1343.8809436489653"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 64898.42345174154,
            "unit": "ns",
            "range": "± 83.73341690721931"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 350841.5747445914,
            "unit": "ns",
            "range": "± 1412.398866282743"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "83938949+ashok672@users.noreply.github.com",
            "name": "Ashok Kumar Ramakrishnan",
            "username": "ashok672"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "4b0b6cdc73e079cc6a73c20f3ab01058da29e2b0",
          "message": "Fall back to local cache if broker fails to return result for silent token call  (#4395)\n\n* Update SilentRequest.cs\r\n\r\n* Fix failing unit tests",
          "timestamp": "2023-11-02T12:10:02Z",
          "tree_id": "7c1ab0ab79280c1535ba0365d55bca60c0e05432",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/4b0b6cdc73e079cc6a73c20f3ab01058da29e2b0"
        },
        "date": 1698927442444,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 38897.65975516183,
            "unit": "ns",
            "range": "± 89.59935061185564"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 349342.5078125,
            "unit": "ns",
            "range": "± 1343.8809436489653"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 64898.42345174154,
            "unit": "ns",
            "range": "± 83.73341690721931"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 350841.5747445914,
            "unit": "ns",
            "range": "± 1412.398866282743"
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
          "id": "172a869d926f243a2c65cc6c63610a6de5f53de7",
          "message": "Deprecate request.WithAuthority (#4406)\n\n* Deprecate request.WithAuthority\r\n\r\n* Deprecate request.WithAuthority\r\n\r\n* fix tests\r\n\r\n* fix",
          "timestamp": "2023-11-06T14:35:37-08:00",
          "tree_id": "41c5124d5bc6117ba67d18a9615f8881cb5cef2f",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/172a869d926f243a2c65cc6c63610a6de5f53de7"
        },
        "date": 1699637971250,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 31355.850110735213,
            "unit": "ns",
            "range": "± 183.37093355253256"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 269082.82732282364,
            "unit": "ns",
            "range": "± 1569.3604310417395"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 44655.1192580003,
            "unit": "ns",
            "range": "± 55.01033593713182"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 275450.6455829327,
            "unit": "ns",
            "range": "± 1543.985225303975"
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
          "id": "172a869d926f243a2c65cc6c63610a6de5f53de7",
          "message": "Deprecate request.WithAuthority (#4406)\n\n* Deprecate request.WithAuthority\r\n\r\n* Deprecate request.WithAuthority\r\n\r\n* fix tests\r\n\r\n* fix",
          "timestamp": "2023-11-06T14:35:37-08:00",
          "tree_id": "41c5124d5bc6117ba67d18a9615f8881cb5cef2f",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/172a869d926f243a2c65cc6c63610a6de5f53de7"
        },
        "date": 1699637971250,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 31355.850110735213,
            "unit": "ns",
            "range": "± 183.37093355253256"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 269082.82732282364,
            "unit": "ns",
            "range": "± 1569.3604310417395"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 44655.1192580003,
            "unit": "ns",
            "range": "± 55.01033593713182"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 275450.6455829327,
            "unit": "ns",
            "range": "± 1543.985225303975"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "43250667+iulico-1@users.noreply.github.com",
            "name": "Iulian C",
            "username": "iulico-1"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "da67f067f14b26fdfb928178ad8b21bcdcde91a9",
          "message": "Add support for MSA passthrough for silent token acquisition calls through the boker (#4419)\n\nAdd support for MSA passthrough for silent token acquisition calls through the boker (#4419)",
          "timestamp": "2023-11-17T09:02:38-08:00",
          "tree_id": "15e73a063233ee0b0f3923f0dadd48a269fc59d3",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/da67f067f14b26fdfb928178ad8b21bcdcde91a9"
        },
        "date": 1700240983192,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 30961.22031075614,
            "unit": "ns",
            "range": "± 369.42689299737134"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 264283.7095336914,
            "unit": "ns",
            "range": "± 4857.304175881879"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 44242.17529907227,
            "unit": "ns",
            "range": "± 304.5160943516941"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 273181.5087541853,
            "unit": "ns",
            "range": "± 1921.7158589759622"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "43250667+iulico-1@users.noreply.github.com",
            "name": "Iulian C",
            "username": "iulico-1"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "da67f067f14b26fdfb928178ad8b21bcdcde91a9",
          "message": "Add support for MSA passthrough for silent token acquisition calls through the boker (#4419)\n\nAdd support for MSA passthrough for silent token acquisition calls through the boker (#4419)",
          "timestamp": "2023-11-17T09:02:38-08:00",
          "tree_id": "15e73a063233ee0b0f3923f0dadd48a269fc59d3",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/da67f067f14b26fdfb928178ad8b21bcdcde91a9"
        },
        "date": 1700240983192,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 30961.22031075614,
            "unit": "ns",
            "range": "± 369.42689299737134"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 264283.7095336914,
            "unit": "ns",
            "range": "± 4857.304175881879"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 44242.17529907227,
            "unit": "ns",
            "range": "± 304.5160943516941"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 273181.5087541853,
            "unit": "ns",
            "range": "± 1921.7158589759622"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "61847233+neha-bhargava@users.noreply.github.com",
            "name": "Neha Bhargava",
            "username": "neha-bhargava"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "9792dc6d7e7459a1d52ee0fda28e20dbd57111d8",
          "message": "Cache level should be L1 in case of default in memory cache. (#4423)\n\nCache level should be L1 in case of default in memory cache",
          "timestamp": "2023-11-17T11:30:35-08:00",
          "tree_id": "1d7c2f47b8b9893dd334d2726f49cd6580541a6a",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/9792dc6d7e7459a1d52ee0fda28e20dbd57111d8"
        },
        "date": 1700249881920,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 29883.92694745745,
            "unit": "ns",
            "range": "± 121.31407961957198"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 268548.4717548077,
            "unit": "ns",
            "range": "± 1611.0716565881032"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 43700.23781644381,
            "unit": "ns",
            "range": "± 91.39671293588209"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 276771.2708217076,
            "unit": "ns",
            "range": "± 3251.4655225567135"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "61847233+neha-bhargava@users.noreply.github.com",
            "name": "Neha Bhargava",
            "username": "neha-bhargava"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "9792dc6d7e7459a1d52ee0fda28e20dbd57111d8",
          "message": "Cache level should be L1 in case of default in memory cache. (#4423)\n\nCache level should be L1 in case of default in memory cache",
          "timestamp": "2023-11-17T11:30:35-08:00",
          "tree_id": "1d7c2f47b8b9893dd334d2726f49cd6580541a6a",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/9792dc6d7e7459a1d52ee0fda28e20dbd57111d8"
        },
        "date": 1700249881920,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 29883.92694745745,
            "unit": "ns",
            "range": "± 121.31407961957198"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 268548.4717548077,
            "unit": "ns",
            "range": "± 1611.0716565881032"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 43700.23781644381,
            "unit": "ns",
            "range": "± 91.39671293588209"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 276771.2708217076,
            "unit": "ns",
            "range": "± 3251.4655225567135"
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
          "id": "067159b7259f648798d0762a72b24ef4b3d5c8a3",
          "message": "Use net6.0",
          "timestamp": "2024-01-24T05:19:44Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/067159b7259f648798d0762a72b24ef4b3d5c8a3"
        },
        "date": 1706074023752,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 22662.882271321614,
            "unit": "ns",
            "range": "± 56.72487117834205"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 168916.9863455636,
            "unit": "ns",
            "range": "± 1263.7145317353568"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 50323.30857966496,
            "unit": "ns",
            "range": "± 78.27505981306285"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 172506.38621303014,
            "unit": "ns",
            "range": "± 1628.115519417112"
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
          "id": "067159b7259f648798d0762a72b24ef4b3d5c8a3",
          "message": "Use net6.0",
          "timestamp": "2024-01-24T05:19:44Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/067159b7259f648798d0762a72b24ef4b3d5c8a3"
        },
        "date": 1706074023752,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 22662.882271321614,
            "unit": "ns",
            "range": "± 56.72487117834205"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 168916.9863455636,
            "unit": "ns",
            "range": "± 1263.7145317353568"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 50323.30857966496,
            "unit": "ns",
            "range": "± 78.27505981306285"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 172506.38621303014,
            "unit": "ns",
            "range": "± 1628.115519417112"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "simon.cropp@gmail.com",
            "name": "Simon Cropp",
            "username": "SimonCropp"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "16827a2ccbfcaa607de7295520b5f097446c261a",
          "message": "reduce some string alloc on building (#4564)",
          "timestamp": "2024-01-24T08:51:29Z",
          "tree_id": "2559765585e66470a99924346ae757d15c86668f",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/16827a2ccbfcaa607de7295520b5f097446c261a"
        },
        "date": 1706086728788,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 21390.059673602766,
            "unit": "ns",
            "range": "± 65.09743659670931"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 171229.74591936384,
            "unit": "ns",
            "range": "± 425.5720462116815"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 33471.82019478934,
            "unit": "ns",
            "range": "± 257.452115908924"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 168744.83869280134,
            "unit": "ns",
            "range": "± 1244.5666049925417"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "simon.cropp@gmail.com",
            "name": "Simon Cropp",
            "username": "SimonCropp"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "16827a2ccbfcaa607de7295520b5f097446c261a",
          "message": "reduce some string alloc on building (#4564)",
          "timestamp": "2024-01-24T08:51:29Z",
          "tree_id": "2559765585e66470a99924346ae757d15c86668f",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/16827a2ccbfcaa607de7295520b5f097446c261a"
        },
        "date": 1706086728788,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 21390.059673602766,
            "unit": "ns",
            "range": "± 65.09743659670931"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 171229.74591936384,
            "unit": "ns",
            "range": "± 425.5720462116815"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 33471.82019478934,
            "unit": "ns",
            "range": "± 257.452115908924"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 168744.83869280134,
            "unit": "ns",
            "range": "± 1244.5666049925417"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "simon.cropp@gmail.com",
            "name": "Simon Cropp",
            "username": "SimonCropp"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "1f999fb8520ae1bc0062ac77fe686c241c1eb541",
          "message": "remove unreachable code in GetParsedAuthValueElements (#4541)",
          "timestamp": "2024-01-24T09:41:02Z",
          "tree_id": "89f97b9ffd703f2b73f57c1daee8a46414d1995c",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/1f999fb8520ae1bc0062ac77fe686c241c1eb541"
        },
        "date": 1706089710217,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 22178.28476170131,
            "unit": "ns",
            "range": "± 59.199332319843286"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 167899.59632161458,
            "unit": "ns",
            "range": "± 996.6825429237725"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 32839.91231595553,
            "unit": "ns",
            "range": "± 54.875572984427215"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 174625.99577985491,
            "unit": "ns",
            "range": "± 1368.9943085680748"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "simon.cropp@gmail.com",
            "name": "Simon Cropp",
            "username": "SimonCropp"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "1f999fb8520ae1bc0062ac77fe686c241c1eb541",
          "message": "remove unreachable code in GetParsedAuthValueElements (#4541)",
          "timestamp": "2024-01-24T09:41:02Z",
          "tree_id": "89f97b9ffd703f2b73f57c1daee8a46414d1995c",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/1f999fb8520ae1bc0062ac77fe686c241c1eb541"
        },
        "date": 1706089710217,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: False)",
            "value": 22178.28476170131,
            "unit": "ns",
            "range": "± 59.199332319843286"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (1, 10), EnableCacheSerialization: True)",
            "value": 167899.59632161458,
            "unit": "ns",
            "range": "± 996.6825429237725"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: False)",
            "value": 32839.91231595553,
            "unit": "ns",
            "range": "± 54.875572984427215"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenForOboCacheTests.AcquireTokenOnBehalfOf_TestAsync(CacheSize: (10000, 10), EnableCacheSerialization: True)",
            "value": 174625.99577985491,
            "unit": "ns",
            "range": "± 1368.9943085680748"
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
          "id": "d25f45f7e356bd92986e412dd68b5d341343aa6a",
          "message": "Set CacheSynchronization to true by default (#4310)\n\n* Change EnableCacheSynchronization default.\r\n\r\n* Update tests.\r\n\r\n* Update method comment.\r\n\r\n* Update ConfidentialClientApplicationBuilder.cs\r\n\r\n* Update ConfidentialClientApplicationOptions.cs\r\n\r\n---------\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>",
          "timestamp": "2023-08-30T18:32:08Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/d25f45f7e356bd92986e412dd68b5d341343aa6a"
        },
        "date": 1693422366483,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 30577.304069010417,
            "unit": "ns",
            "range": "± 414.15598608791214"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 58528.02816336496,
            "unit": "ns",
            "range": "± 259.26612528421936"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 15970.782263183593,
            "unit": "ns",
            "range": "± 90.91011426093425"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 43005.75264078776,
            "unit": "ns",
            "range": "± 49.84686514203226"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 58401,
            "unit": "ns",
            "range": "± 408.248290463863"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 205260.55434782608,
            "unit": "ns",
            "range": "± 58530.41727271294"
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
          "id": "d25f45f7e356bd92986e412dd68b5d341343aa6a",
          "message": "Set CacheSynchronization to true by default (#4310)\n\n* Change EnableCacheSynchronization default.\r\n\r\n* Update tests.\r\n\r\n* Update method comment.\r\n\r\n* Update ConfidentialClientApplicationBuilder.cs\r\n\r\n* Update ConfidentialClientApplicationOptions.cs\r\n\r\n---------\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>",
          "timestamp": "2023-08-30T18:32:08Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/d25f45f7e356bd92986e412dd68b5d341343aa6a"
        },
        "date": 1693422366483,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 30577.304069010417,
            "unit": "ns",
            "range": "± 414.15598608791214"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 58528.02816336496,
            "unit": "ns",
            "range": "± 259.26612528421936"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 15970.782263183593,
            "unit": "ns",
            "range": "± 90.91011426093425"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 43005.75264078776,
            "unit": "ns",
            "range": "± 49.84686514203226"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 58401,
            "unit": "ns",
            "range": "± 408.248290463863"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 205260.55434782608,
            "unit": "ns",
            "range": "± 58530.41727271294"
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
          "id": "29cbbb0ebe4361c98bcfaf9f00092c1517939ea3",
          "message": "Expose WAM telemetry in AuthenticationResult and exceptions (#4307)\n\n* Add WAM telemetry to AuthResult\r\n\r\n* Add wam telemetry to exceptions\r\n\r\n* Address PR chages\r\n\r\n* Add WAM telemetry to AuthResult\r\n\r\n* Add wam telemetry to exceptions\r\n\r\n* Address PR chages\r\n\r\n* Fix naming\r\n\r\n* fix\r\n\r\n* Rename\r\n\r\n* Fix",
          "timestamp": "2023-09-05T12:44:21+01:00",
          "tree_id": "abfe00ce5ef36f4d45737e9a537369ba544de73a",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/29cbbb0ebe4361c98bcfaf9f00092c1517939ea3"
        },
        "date": 1693914761955,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 30378.883675711495,
            "unit": "ns",
            "range": "± 185.14867829212685"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 58494.67566353934,
            "unit": "ns",
            "range": "± 102.85590533808033"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 16390.328645833335,
            "unit": "ns",
            "range": "± 83.25430741037304"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 43011.609771728516,
            "unit": "ns",
            "range": "± 34.4792432695141"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 60032.25,
            "unit": "ns",
            "range": "± 1153.3827060723022"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 167806.175,
            "unit": "ns",
            "range": "± 43366.957593376275"
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
          "id": "29cbbb0ebe4361c98bcfaf9f00092c1517939ea3",
          "message": "Expose WAM telemetry in AuthenticationResult and exceptions (#4307)\n\n* Add WAM telemetry to AuthResult\r\n\r\n* Add wam telemetry to exceptions\r\n\r\n* Address PR chages\r\n\r\n* Add WAM telemetry to AuthResult\r\n\r\n* Add wam telemetry to exceptions\r\n\r\n* Address PR chages\r\n\r\n* Fix naming\r\n\r\n* fix\r\n\r\n* Rename\r\n\r\n* Fix",
          "timestamp": "2023-09-05T12:44:21+01:00",
          "tree_id": "abfe00ce5ef36f4d45737e9a537369ba544de73a",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/29cbbb0ebe4361c98bcfaf9f00092c1517939ea3"
        },
        "date": 1693914761955,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 30378.883675711495,
            "unit": "ns",
            "range": "± 185.14867829212685"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 58494.67566353934,
            "unit": "ns",
            "range": "± 102.85590533808033"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 16390.328645833335,
            "unit": "ns",
            "range": "± 83.25430741037304"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 43011.609771728516,
            "unit": "ns",
            "range": "± 34.4792432695141"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 60032.25,
            "unit": "ns",
            "range": "± 1153.3827060723022"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 167806.175,
            "unit": "ns",
            "range": "± 43366.957593376275"
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
          "id": "90ae6b9c6d33655a66cbc6413e8aadee487f052a",
          "message": "Check for null and empty scopes in managed identity (#4328)\n\n* managed identity fix\r\n\r\n* ArgumentNullException\r\n\r\n* fix\r\n\r\n* exception\r\n\r\n---------\r\n\r\nCo-authored-by: Gladwin Johnson <gljohns@microsoft.com>",
          "timestamp": "2023-09-06T17:15:40Z",
          "tree_id": "d1247ec22650b6f61de984ce7e3f7f5efbca8d89",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/90ae6b9c6d33655a66cbc6413e8aadee487f052a"
        },
        "date": 1694031597517,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 31116.001912434895,
            "unit": "ns",
            "range": "± 86.36272223902682"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 57164.452205113004,
            "unit": "ns",
            "range": "± 179.41368350392156"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 15810.811747959682,
            "unit": "ns",
            "range": "± 115.01802957228644"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 41834.71796090262,
            "unit": "ns",
            "range": "± 78.13141363785927"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 58666.52380952381,
            "unit": "ns",
            "range": "± 1334.388797129518"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 208356.40740740742,
            "unit": "ns",
            "range": "± 22194.816429054878"
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
          "id": "90ae6b9c6d33655a66cbc6413e8aadee487f052a",
          "message": "Check for null and empty scopes in managed identity (#4328)\n\n* managed identity fix\r\n\r\n* ArgumentNullException\r\n\r\n* fix\r\n\r\n* exception\r\n\r\n---------\r\n\r\nCo-authored-by: Gladwin Johnson <gljohns@microsoft.com>",
          "timestamp": "2023-09-06T17:15:40Z",
          "tree_id": "d1247ec22650b6f61de984ce7e3f7f5efbca8d89",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/90ae6b9c6d33655a66cbc6413e8aadee487f052a"
        },
        "date": 1694031597517,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 31116.001912434895,
            "unit": "ns",
            "range": "± 86.36272223902682"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 57164.452205113004,
            "unit": "ns",
            "range": "± 179.41368350392156"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 15810.811747959682,
            "unit": "ns",
            "range": "± 115.01802957228644"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 41834.71796090262,
            "unit": "ns",
            "range": "± 78.13141363785927"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 58666.52380952381,
            "unit": "ns",
            "range": "± 1334.388797129518"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 208356.40740740742,
            "unit": "ns",
            "range": "± 22194.816429054878"
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
          "id": "fe3c8eacfb013b73404b3295ec5a36b3a60b3ed8",
          "message": "Fix for WAM Runtime error logs duplicated info (#4354)\n\n* Update RuntimeBroker.cs\r\n\r\n* Update RuntimeBroker.cs",
          "timestamp": "2023-09-19T14:24:56-07:00",
          "tree_id": "e70a2e1aab89831139cede5cbd30c11f05df7ed4",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/fe3c8eacfb013b73404b3295ec5a36b3a60b3ed8"
        },
        "date": 1695159231160,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 31691.96006266276,
            "unit": "ns",
            "range": "± 267.234463477712"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 58011.17525373186,
            "unit": "ns",
            "range": "± 45.61176637520716"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 16713.04726155599,
            "unit": "ns",
            "range": "± 23.761163553790613"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 43073.30351969401,
            "unit": "ns",
            "range": "± 42.05711441952553"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 57054.61538461538,
            "unit": "ns",
            "range": "± 533.4348661366788"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 222631.53846153847,
            "unit": "ns",
            "range": "± 12446.57504102025"
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
          "id": "fe3c8eacfb013b73404b3295ec5a36b3a60b3ed8",
          "message": "Fix for WAM Runtime error logs duplicated info (#4354)\n\n* Update RuntimeBroker.cs\r\n\r\n* Update RuntimeBroker.cs",
          "timestamp": "2023-09-19T14:24:56-07:00",
          "tree_id": "e70a2e1aab89831139cede5cbd30c11f05df7ed4",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/fe3c8eacfb013b73404b3295ec5a36b3a60b3ed8"
        },
        "date": 1695159231160,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 31691.96006266276,
            "unit": "ns",
            "range": "± 267.234463477712"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 58011.17525373186,
            "unit": "ns",
            "range": "± 45.61176637520716"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 16713.04726155599,
            "unit": "ns",
            "range": "± 23.761163553790613"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 43073.30351969401,
            "unit": "ns",
            "range": "± 42.05711441952553"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 57054.61538461538,
            "unit": "ns",
            "range": "± 533.4348661366788"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 222631.53846153847,
            "unit": "ns",
            "range": "± 12446.57504102025"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "53200638+localden@users.noreply.github.com",
            "name": "Den Delimarsky",
            "username": "localden"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "b898ab8193bc751ae5aabe5ff7e5f079eccd3f85",
          "message": "Updates to documentation and minor code cleanup (#4339) (#4346)\n\n* Updates to documentation (#4339)\r\n\r\n* Update PublicClientApplication.cs\r\n\r\n* Update inheritdoc comments\r\n\r\n* Update documentation inheritance logic for PCA\r\n\r\n* Update documentation\r\n\r\n* Cleanup unused namespaces\r\n\r\n* Some extra cleanup\r\n\r\n* Update README.md\r\n\r\n* Documentation and minor cleanup\r\n\r\n* Update AuthenticationHeaderParser.cs\r\n\r\n* Update README.md\r\n\r\n* Update contributing documentation\r\n\r\nThis tackles the merge from https://github.com/MicrosoftDocs/microsoft-authentication-library-dotnet/pull/135\r\n\r\n* Roll back this change because we're building on .NET Fx, which doesn't support ranges\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update src/client/Microsoft.Identity.Client/AuthenticationHeaderParser.cs\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Update README.md\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Small documentation cleanup items\r\n\r\n* Update CONTRIBUTING.md\r\n\r\n* Cleanup\r\n\r\n* More cleanup - this time with some code\r\n\r\n* Update MsalServiceExceptionFactory.cs\r\n\r\n* Update MsalServiceExceptionFactory.cs\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Update src/client/Microsoft.Identity.Client/UserAssertion.cs\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Add missing Using statement\r\n\r\n---------\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\nCo-authored-by: Bogdan Gavril <bogavril@microsoft.com>",
          "timestamp": "2023-09-25T13:11:55Z",
          "tree_id": "b514ea57ab8de82c8840ca5149c4407d0b7a4c6a",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/b898ab8193bc751ae5aabe5ff7e5f079eccd3f85"
        },
        "date": 1695659002023,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 31346.069942220052,
            "unit": "ns",
            "range": "± 211.31020967035136"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 57741.1101902553,
            "unit": "ns",
            "range": "± 330.67985283622943"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 16264.572245279947,
            "unit": "ns",
            "range": "± 83.46904960091152"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 43103.34935506185,
            "unit": "ns",
            "range": "± 45.800428564143665"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 58662.769230769234,
            "unit": "ns",
            "range": "± 657.8755142940739"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 190656.5285714286,
            "unit": "ns",
            "range": "± 9217.400349844675"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "53200638+localden@users.noreply.github.com",
            "name": "Den Delimarsky",
            "username": "localden"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "b898ab8193bc751ae5aabe5ff7e5f079eccd3f85",
          "message": "Updates to documentation and minor code cleanup (#4339) (#4346)\n\n* Updates to documentation (#4339)\r\n\r\n* Update PublicClientApplication.cs\r\n\r\n* Update inheritdoc comments\r\n\r\n* Update documentation inheritance logic for PCA\r\n\r\n* Update documentation\r\n\r\n* Cleanup unused namespaces\r\n\r\n* Some extra cleanup\r\n\r\n* Update README.md\r\n\r\n* Documentation and minor cleanup\r\n\r\n* Update AuthenticationHeaderParser.cs\r\n\r\n* Update README.md\r\n\r\n* Update contributing documentation\r\n\r\nThis tackles the merge from https://github.com/MicrosoftDocs/microsoft-authentication-library-dotnet/pull/135\r\n\r\n* Roll back this change because we're building on .NET Fx, which doesn't support ranges\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update src/client/Microsoft.Identity.Client/AuthenticationHeaderParser.cs\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Update README.md\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Small documentation cleanup items\r\n\r\n* Update CONTRIBUTING.md\r\n\r\n* Cleanup\r\n\r\n* More cleanup - this time with some code\r\n\r\n* Update MsalServiceExceptionFactory.cs\r\n\r\n* Update MsalServiceExceptionFactory.cs\r\n\r\n* Update CONTRIBUTING.md\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Update src/client/Microsoft.Identity.Client/UserAssertion.cs\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Add missing Using statement\r\n\r\n---------\r\n\r\nCo-authored-by: Peter <34331512+pmaytak@users.noreply.github.com>\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\nCo-authored-by: Bogdan Gavril <bogavril@microsoft.com>",
          "timestamp": "2023-09-25T13:11:55Z",
          "tree_id": "b514ea57ab8de82c8840ca5149c4407d0b7a4c6a",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/b898ab8193bc751ae5aabe5ff7e5f079eccd3f85"
        },
        "date": 1695659002023,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 31346.069942220052,
            "unit": "ns",
            "range": "± 211.31020967035136"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 57741.1101902553,
            "unit": "ns",
            "range": "± 330.67985283622943"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 16264.572245279947,
            "unit": "ns",
            "range": "± 83.46904960091152"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 43103.34935506185,
            "unit": "ns",
            "range": "± 45.800428564143665"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 58662.769230769234,
            "unit": "ns",
            "range": "± 657.8755142940739"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 190656.5285714286,
            "unit": "ns",
            "range": "± 9217.400349844675"
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
          "id": "80c3be514c4bd59412b20b7c1f792452be3943ea",
          "message": "Add CacheLevel to AuthResultMetadata (#4349)",
          "timestamp": "2023-09-26T06:26:38Z",
          "tree_id": "94eabae8ba8aa46440d553e91a36efc0b90018e4",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/80c3be514c4bd59412b20b7c1f792452be3943ea"
        },
        "date": 1695710074887,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 30188.057969313402,
            "unit": "ns",
            "range": "± 55.35485266948147"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 56227.114237467445,
            "unit": "ns",
            "range": "± 272.3780492808784"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 16024.862605503627,
            "unit": "ns",
            "range": "± 40.46357717329084"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 41756.28579711914,
            "unit": "ns",
            "range": "± 28.489660816039372"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 53147.619047619046,
            "unit": "ns",
            "range": "± 1239.6043915778323"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 158317.36842105264,
            "unit": "ns",
            "range": "± 15376.574865114017"
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
          "id": "80c3be514c4bd59412b20b7c1f792452be3943ea",
          "message": "Add CacheLevel to AuthResultMetadata (#4349)",
          "timestamp": "2023-09-26T06:26:38Z",
          "tree_id": "94eabae8ba8aa46440d553e91a36efc0b90018e4",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/80c3be514c4bd59412b20b7c1f792452be3943ea"
        },
        "date": 1695710074887,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 30188.057969313402,
            "unit": "ns",
            "range": "± 55.35485266948147"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 56227.114237467445,
            "unit": "ns",
            "range": "± 272.3780492808784"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 16024.862605503627,
            "unit": "ns",
            "range": "± 40.46357717329084"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 41756.28579711914,
            "unit": "ns",
            "range": "± 28.489660816039372"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 53147.619047619046,
            "unit": "ns",
            "range": "± 1239.6043915778323"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 158317.36842105264,
            "unit": "ns",
            "range": "± 15376.574865114017"
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
          "id": "118ab93b5afb78c8c4a594dc27a3288725f76213",
          "message": "Add one retry to assertion signing. (#4365)\n\n* Add one retry to assertion signing.\r\n\r\n* Fix.",
          "timestamp": "2023-10-05T01:13:42Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/118ab93b5afb78c8c4a594dc27a3288725f76213"
        },
        "date": 1696485263369,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 29553.412389119465,
            "unit": "ns",
            "range": "± 49.02487632246484"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 57050.87018258231,
            "unit": "ns",
            "range": "± 55.90649537722677"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 15849.25878092448,
            "unit": "ns",
            "range": "± 42.89969494728351"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 41532.38331705729,
            "unit": "ns",
            "range": "± 48.48284963544506"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 54838.857142857145,
            "unit": "ns",
            "range": "± 1568.6174076009008"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 190117.04395604396,
            "unit": "ns",
            "range": "± 58689.578675701705"
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
          "id": "118ab93b5afb78c8c4a594dc27a3288725f76213",
          "message": "Add one retry to assertion signing. (#4365)\n\n* Add one retry to assertion signing.\r\n\r\n* Fix.",
          "timestamp": "2023-10-05T01:13:42Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/118ab93b5afb78c8c4a594dc27a3288725f76213"
        },
        "date": 1696485263369,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 29553.412389119465,
            "unit": "ns",
            "range": "± 49.02487632246484"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 57050.87018258231,
            "unit": "ns",
            "range": "± 55.90649537722677"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 15849.25878092448,
            "unit": "ns",
            "range": "± 42.89969494728351"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 41532.38331705729,
            "unit": "ns",
            "range": "± 48.48284963544506"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 54838.857142857145,
            "unit": "ns",
            "range": "± 1568.6174076009008"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 190117.04395604396,
            "unit": "ns",
            "range": "± 58689.578675701705"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "travis.walker@microsoft.com",
            "name": "Travis Walker",
            "username": "trwalke"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "b259cf00936a11a9cff789bf094935d8d31aea7f",
          "message": "Disabling x-client-CPU (#4337)\n\nCo-authored-by: trwalke <trwalke@microsoft.com>",
          "timestamp": "2023-10-05T08:16:41Z",
          "tree_id": "41b84f6aa61c6b639b41d2fbf9b4f36b5cf13479",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/b259cf00936a11a9cff789bf094935d8d31aea7f"
        },
        "date": 1696494240515,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 29893.572664701023,
            "unit": "ns",
            "range": "± 46.973751599134175"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 56273.8941791241,
            "unit": "ns",
            "range": "± 104.51004932062439"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 15629.145064290364,
            "unit": "ns",
            "range": "± 43.709298783914285"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 42007.64123065655,
            "unit": "ns",
            "range": "± 40.19306331989698"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 53320.708333333336,
            "unit": "ns",
            "range": "± 1336.2001763328294"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 164692.43636363637,
            "unit": "ns",
            "range": "± 4618.252574632275"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "travis.walker@microsoft.com",
            "name": "Travis Walker",
            "username": "trwalke"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "b259cf00936a11a9cff789bf094935d8d31aea7f",
          "message": "Disabling x-client-CPU (#4337)\n\nCo-authored-by: trwalke <trwalke@microsoft.com>",
          "timestamp": "2023-10-05T08:16:41Z",
          "tree_id": "41b84f6aa61c6b639b41d2fbf9b4f36b5cf13479",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/b259cf00936a11a9cff789bf094935d8d31aea7f"
        },
        "date": 1696494240515,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 29893.572664701023,
            "unit": "ns",
            "range": "± 46.973751599134175"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 56273.8941791241,
            "unit": "ns",
            "range": "± 104.51004932062439"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 15629.145064290364,
            "unit": "ns",
            "range": "± 43.709298783914285"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 42007.64123065655,
            "unit": "ns",
            "range": "± 40.19306331989698"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 53320.708333333336,
            "unit": "ns",
            "range": "± 1336.2001763328294"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 164692.43636363637,
            "unit": "ns",
            "range": "± 4618.252574632275"
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
          "id": "474026f56b1c3dfe2628fea104b8c80c46ce7e57",
          "message": "Throw ArgumentException for malformed authorities (#4347)\n\n* Throw argument exceptions for consistency when authority or tenant are invalid. Update method comments.\r\n\r\n* Update and add tests.\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Den Delimarsky <53200638+localden@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Small test fixes.\r\n\r\n* Update comments.\r\n\r\n---------\r\n\r\nCo-authored-by: Den Delimarsky <53200638+localden@users.noreply.github.com>\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>",
          "timestamp": "2023-10-10T17:11:08-07:00",
          "tree_id": "5489e2487eb172ef71a482b92a3b09aa4b60fde3",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/474026f56b1c3dfe2628fea104b8c80c46ce7e57"
        },
        "date": 1696983679662,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 34705.81794840495,
            "unit": "ns",
            "range": "± 215.6153601511452"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 61391.65170898438,
            "unit": "ns",
            "range": "± 230.24237055586798"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 17792.807666485125,
            "unit": "ns",
            "range": "± 65.28298096197861"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 44707.26985822405,
            "unit": "ns",
            "range": "± 63.214196867943116"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 60418.22222222222,
            "unit": "ns",
            "range": "± 1278.8936190664526"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 235532.35,
            "unit": "ns",
            "range": "± 24051.045424316406"
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
          "id": "474026f56b1c3dfe2628fea104b8c80c46ce7e57",
          "message": "Throw ArgumentException for malformed authorities (#4347)\n\n* Throw argument exceptions for consistency when authority or tenant are invalid. Update method comments.\r\n\r\n* Update and add tests.\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Den Delimarsky <53200638+localden@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Small test fixes.\r\n\r\n* Update comments.\r\n\r\n---------\r\n\r\nCo-authored-by: Den Delimarsky <53200638+localden@users.noreply.github.com>\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>",
          "timestamp": "2023-10-11T00:11:08Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/474026f56b1c3dfe2628fea104b8c80c46ce7e57"
        },
        "date": 1697004646793,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 31498.046842134914,
            "unit": "ns",
            "range": "± 129.95403498906128"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 57892.33239746094,
            "unit": "ns",
            "range": "± 29.3855720740819"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 16406.936901855468,
            "unit": "ns",
            "range": "± 30.15229014327754"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 43234.916368756974,
            "unit": "ns",
            "range": "± 95.80203634345574"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 58784.666666666664,
            "unit": "ns",
            "range": "± 808.8601782006505"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 194188.98780487804,
            "unit": "ns",
            "range": "± 10660.240481205545"
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
          "id": "474026f56b1c3dfe2628fea104b8c80c46ce7e57",
          "message": "Throw ArgumentException for malformed authorities (#4347)\n\n* Throw argument exceptions for consistency when authority or tenant are invalid. Update method comments.\r\n\r\n* Update and add tests.\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Den Delimarsky <53200638+localden@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Apply suggestions from code review\r\n\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>\r\n\r\n* Small test fixes.\r\n\r\n* Update comments.\r\n\r\n---------\r\n\r\nCo-authored-by: Den Delimarsky <53200638+localden@users.noreply.github.com>\r\nCo-authored-by: Gladwin Johnson <90415114+gladjohn@users.noreply.github.com>",
          "timestamp": "2023-10-11T00:11:08Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/474026f56b1c3dfe2628fea104b8c80c46ce7e57"
        },
        "date": 1697004646793,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 31498.046842134914,
            "unit": "ns",
            "range": "± 129.95403498906128"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 57892.33239746094,
            "unit": "ns",
            "range": "± 29.3855720740819"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 16406.936901855468,
            "unit": "ns",
            "range": "± 30.15229014327754"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 43234.916368756974,
            "unit": "ns",
            "range": "± 95.80203634345574"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 58784.666666666664,
            "unit": "ns",
            "range": "± 808.8601782006505"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 194188.98780487804,
            "unit": "ns",
            "range": "± 10660.240481205545"
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
          "id": "8bdf6990fe9ad679adabcd1e7b008c3e38a75d3f",
          "message": "Fix comments warnings. (#4373)",
          "timestamp": "2023-10-12T21:14:28Z",
          "tree_id": "fc9a99ce441fb47274b6d000f90dfcea10f0698f",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/8bdf6990fe9ad679adabcd1e7b008c3e38a75d3f"
        },
        "date": 1697145741979,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 30700.04934082031,
            "unit": "ns",
            "range": "± 218.7939923540508"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 57259.624834333146,
            "unit": "ns",
            "range": "± 167.8129305023756"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 15850.834698486327,
            "unit": "ns",
            "range": "± 103.99010324634784"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 43896.271425600404,
            "unit": "ns",
            "range": "± 1211.0862815331714"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 58654.53333333333,
            "unit": "ns",
            "range": "± 699.7252385928418"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 182811.9014084507,
            "unit": "ns",
            "range": "± 9165.282148497634"
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
          "id": "8bdf6990fe9ad679adabcd1e7b008c3e38a75d3f",
          "message": "Fix comments warnings. (#4373)",
          "timestamp": "2023-10-12T21:14:28Z",
          "tree_id": "fc9a99ce441fb47274b6d000f90dfcea10f0698f",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/8bdf6990fe9ad679adabcd1e7b008c3e38a75d3f"
        },
        "date": 1697145741979,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 30700.04934082031,
            "unit": "ns",
            "range": "± 218.7939923540508"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 57259.624834333146,
            "unit": "ns",
            "range": "± 167.8129305023756"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 15850.834698486327,
            "unit": "ns",
            "range": "± 103.99010324634784"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 43896.271425600404,
            "unit": "ns",
            "range": "± 1211.0862815331714"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 58654.53333333333,
            "unit": "ns",
            "range": "± 699.7252385928418"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 182811.9014084507,
            "unit": "ns",
            "range": "± 9165.282148497634"
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
          "id": "a8b93d42c8083097bfa452d313504e92d1ebc414",
          "message": "Print refresh token cache keys to logs (#4375)\n\n* Print RTs to cache.\r\n\r\n* Add tests.",
          "timestamp": "2023-10-16T12:15:40-07:00",
          "tree_id": "0e6d34d0b69bca92a076c60a4f162dbf59a232b7",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/a8b93d42c8083097bfa452d313504e92d1ebc414"
        },
        "date": 1697484275610,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 36757.454370117186,
            "unit": "ns",
            "range": "± 822.1156593213058"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 68755.32436697824,
            "unit": "ns",
            "range": "± 1032.2126059446778"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 18227.006260172526,
            "unit": "ns",
            "range": "± 301.9290310876613"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 50046.25175206801,
            "unit": "ns",
            "range": "± 995.9027902892769"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 62689.10227272727,
            "unit": "ns",
            "range": "± 4970.490146833096"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 236213.56842105262,
            "unit": "ns",
            "range": "± 75915.16985616565"
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
          "id": "a8b93d42c8083097bfa452d313504e92d1ebc414",
          "message": "Print refresh token cache keys to logs (#4375)\n\n* Print RTs to cache.\r\n\r\n* Add tests.",
          "timestamp": "2023-10-16T12:15:40-07:00",
          "tree_id": "0e6d34d0b69bca92a076c60a4f162dbf59a232b7",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/a8b93d42c8083097bfa452d313504e92d1ebc414"
        },
        "date": 1697484275610,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 36757.454370117186,
            "unit": "ns",
            "range": "± 822.1156593213058"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 68755.32436697824,
            "unit": "ns",
            "range": "± 1032.2126059446778"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 18227.006260172526,
            "unit": "ns",
            "range": "± 301.9290310876613"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 50046.25175206801,
            "unit": "ns",
            "range": "± 995.9027902892769"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 62689.10227272727,
            "unit": "ns",
            "range": "± 4970.490146833096"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 236213.56842105262,
            "unit": "ns",
            "range": "± 75915.16985616565"
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
          "id": "f951d8418e0a9fcb011c1cd7cfbcfec5b8df0962",
          "message": "Add serialization of broker error data in exceptions (#4376)\n\n* Add serialization of broker error.\r\n\r\n* Refactor.\r\n\r\n* Fix.\r\n\r\n* PR feedback.\r\n\r\n* Naming change.",
          "timestamp": "2023-10-16T15:21:08-07:00",
          "tree_id": "85c04e69f87e6728882ceb75044aad7a206c196c",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/f951d8418e0a9fcb011c1cd7cfbcfec5b8df0962"
        },
        "date": 1697495322989,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 30179.43326509916,
            "unit": "ns",
            "range": "± 52.39636788118778"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 55799.07071940104,
            "unit": "ns",
            "range": "± 61.9854637945513"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 16046.614687238422,
            "unit": "ns",
            "range": "± 32.726719689263724"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 41801.021960449216,
            "unit": "ns",
            "range": "± 36.10434013174779"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 57098.60655737705,
            "unit": "ns",
            "range": "± 2573.9240359076166"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 159900.6724137931,
            "unit": "ns",
            "range": "± 6300.77384940381"
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
          "id": "f951d8418e0a9fcb011c1cd7cfbcfec5b8df0962",
          "message": "Add serialization of broker error data in exceptions (#4376)\n\n* Add serialization of broker error.\r\n\r\n* Refactor.\r\n\r\n* Fix.\r\n\r\n* PR feedback.\r\n\r\n* Naming change.",
          "timestamp": "2023-10-16T15:21:08-07:00",
          "tree_id": "85c04e69f87e6728882ceb75044aad7a206c196c",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/f951d8418e0a9fcb011c1cd7cfbcfec5b8df0962"
        },
        "date": 1697495322989,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 30179.43326509916,
            "unit": "ns",
            "range": "± 52.39636788118778"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 55799.07071940104,
            "unit": "ns",
            "range": "± 61.9854637945513"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 16046.614687238422,
            "unit": "ns",
            "range": "± 32.726719689263724"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 41801.021960449216,
            "unit": "ns",
            "range": "± 36.10434013174779"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 57098.60655737705,
            "unit": "ns",
            "range": "± 2573.9240359076166"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 159900.6724137931,
            "unit": "ns",
            "range": "± 6300.77384940381"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "83938949+ashok672@users.noreply.github.com",
            "name": "Ashok Kumar Ramakrishnan",
            "username": "ashok672"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "4b0b6cdc73e079cc6a73c20f3ab01058da29e2b0",
          "message": "Fall back to local cache if broker fails to return result for silent token call  (#4395)\n\n* Update SilentRequest.cs\r\n\r\n* Fix failing unit tests",
          "timestamp": "2023-11-02T12:10:02Z",
          "tree_id": "7c1ab0ab79280c1535ba0365d55bca60c0e05432",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/4b0b6cdc73e079cc6a73c20f3ab01058da29e2b0"
        },
        "date": 1698927452496,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 29492.65011160714,
            "unit": "ns",
            "range": "± 71.51221516182632"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 57337.35847981771,
            "unit": "ns",
            "range": "± 794.2963905687948"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 16232.18872680664,
            "unit": "ns",
            "range": "± 74.38766642066508"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 41866.41778157552,
            "unit": "ns",
            "range": "± 131.37071640483964"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 54016.73333333333,
            "unit": "ns",
            "range": "± 1595.0829820232643"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 177999.2530120482,
            "unit": "ns",
            "range": "± 37760.03520172094"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "83938949+ashok672@users.noreply.github.com",
            "name": "Ashok Kumar Ramakrishnan",
            "username": "ashok672"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "4b0b6cdc73e079cc6a73c20f3ab01058da29e2b0",
          "message": "Fall back to local cache if broker fails to return result for silent token call  (#4395)\n\n* Update SilentRequest.cs\r\n\r\n* Fix failing unit tests",
          "timestamp": "2023-11-02T12:10:02Z",
          "tree_id": "7c1ab0ab79280c1535ba0365d55bca60c0e05432",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/4b0b6cdc73e079cc6a73c20f3ab01058da29e2b0"
        },
        "date": 1698927452496,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 29492.65011160714,
            "unit": "ns",
            "range": "± 71.51221516182632"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 57337.35847981771,
            "unit": "ns",
            "range": "± 794.2963905687948"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 16232.18872680664,
            "unit": "ns",
            "range": "± 74.38766642066508"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 41866.41778157552,
            "unit": "ns",
            "range": "± 131.37071640483964"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 54016.73333333333,
            "unit": "ns",
            "range": "± 1595.0829820232643"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 177999.2530120482,
            "unit": "ns",
            "range": "± 37760.03520172094"
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
          "id": "172a869d926f243a2c65cc6c63610a6de5f53de7",
          "message": "Deprecate request.WithAuthority (#4406)\n\n* Deprecate request.WithAuthority\r\n\r\n* Deprecate request.WithAuthority\r\n\r\n* fix tests\r\n\r\n* fix",
          "timestamp": "2023-11-06T14:35:37-08:00",
          "tree_id": "41c5124d5bc6117ba67d18a9615f8881cb5cef2f",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/172a869d926f243a2c65cc6c63610a6de5f53de7"
        },
        "date": 1699637974230,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 24216.407145182293,
            "unit": "ns",
            "range": "± 220.99517939396972"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 36990.94799368722,
            "unit": "ns",
            "range": "± 301.18302542859357"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 12837.67571105957,
            "unit": "ns",
            "range": "± 52.17058977531649"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 25410.137134258563,
            "unit": "ns",
            "range": "± 77.29675441158028"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 44743.03846153846,
            "unit": "ns",
            "range": "± 457.0420504695776"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 177726.88461538462,
            "unit": "ns",
            "range": "± 16679.747762139315"
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
          "id": "172a869d926f243a2c65cc6c63610a6de5f53de7",
          "message": "Deprecate request.WithAuthority (#4406)\n\n* Deprecate request.WithAuthority\r\n\r\n* Deprecate request.WithAuthority\r\n\r\n* fix tests\r\n\r\n* fix",
          "timestamp": "2023-11-06T14:35:37-08:00",
          "tree_id": "41c5124d5bc6117ba67d18a9615f8881cb5cef2f",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/172a869d926f243a2c65cc6c63610a6de5f53de7"
        },
        "date": 1699637974230,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 24216.407145182293,
            "unit": "ns",
            "range": "± 220.99517939396972"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 36990.94799368722,
            "unit": "ns",
            "range": "± 301.18302542859357"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 12837.67571105957,
            "unit": "ns",
            "range": "± 52.17058977531649"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 25410.137134258563,
            "unit": "ns",
            "range": "± 77.29675441158028"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 44743.03846153846,
            "unit": "ns",
            "range": "± 457.0420504695776"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 177726.88461538462,
            "unit": "ns",
            "range": "± 16679.747762139315"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "43250667+iulico-1@users.noreply.github.com",
            "name": "Iulian C",
            "username": "iulico-1"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "da67f067f14b26fdfb928178ad8b21bcdcde91a9",
          "message": "Add support for MSA passthrough for silent token acquisition calls through the boker (#4419)\n\nAdd support for MSA passthrough for silent token acquisition calls through the boker (#4419)",
          "timestamp": "2023-11-17T09:02:38-08:00",
          "tree_id": "15e73a063233ee0b0f3923f0dadd48a269fc59d3",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/da67f067f14b26fdfb928178ad8b21bcdcde91a9"
        },
        "date": 1700240986412,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 24349.586942400252,
            "unit": "ns",
            "range": "± 125.7184337593374"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 37438.65641784668,
            "unit": "ns",
            "range": "± 71.98083389003187"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 12715.824205525716,
            "unit": "ns",
            "range": "± 79.47333472997155"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 26196.27166442871,
            "unit": "ns",
            "range": "± 166.519500102826"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 45504.25,
            "unit": "ns",
            "range": "± 653.0987573423374"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 173037.6049382716,
            "unit": "ns",
            "range": "± 34868.80590502025"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "43250667+iulico-1@users.noreply.github.com",
            "name": "Iulian C",
            "username": "iulico-1"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "da67f067f14b26fdfb928178ad8b21bcdcde91a9",
          "message": "Add support for MSA passthrough for silent token acquisition calls through the boker (#4419)\n\nAdd support for MSA passthrough for silent token acquisition calls through the boker (#4419)",
          "timestamp": "2023-11-17T09:02:38-08:00",
          "tree_id": "15e73a063233ee0b0f3923f0dadd48a269fc59d3",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/da67f067f14b26fdfb928178ad8b21bcdcde91a9"
        },
        "date": 1700240986412,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 24349.586942400252,
            "unit": "ns",
            "range": "± 125.7184337593374"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 37438.65641784668,
            "unit": "ns",
            "range": "± 71.98083389003187"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 12715.824205525716,
            "unit": "ns",
            "range": "± 79.47333472997155"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 26196.27166442871,
            "unit": "ns",
            "range": "± 166.519500102826"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 45504.25,
            "unit": "ns",
            "range": "± 653.0987573423374"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 173037.6049382716,
            "unit": "ns",
            "range": "± 34868.80590502025"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "61847233+neha-bhargava@users.noreply.github.com",
            "name": "Neha Bhargava",
            "username": "neha-bhargava"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "9792dc6d7e7459a1d52ee0fda28e20dbd57111d8",
          "message": "Cache level should be L1 in case of default in memory cache. (#4423)\n\nCache level should be L1 in case of default in memory cache",
          "timestamp": "2023-11-17T11:30:35-08:00",
          "tree_id": "1d7c2f47b8b9893dd334d2726f49cd6580541a6a",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/9792dc6d7e7459a1d52ee0fda28e20dbd57111d8"
        },
        "date": 1700249886510,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 23925.904522705077,
            "unit": "ns",
            "range": "± 112.64353218509001"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 37231.91484187199,
            "unit": "ns",
            "range": "± 358.14622868085496"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 12508.651259358723,
            "unit": "ns",
            "range": "± 62.84085017201559"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 25392.409553019206,
            "unit": "ns",
            "range": "± 119.96229643918328"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 46248.46153846154,
            "unit": "ns",
            "range": "± 404.42234841491626"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 168831.1625,
            "unit": "ns",
            "range": "± 17581.441497361877"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "61847233+neha-bhargava@users.noreply.github.com",
            "name": "Neha Bhargava",
            "username": "neha-bhargava"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "9792dc6d7e7459a1d52ee0fda28e20dbd57111d8",
          "message": "Cache level should be L1 in case of default in memory cache. (#4423)\n\nCache level should be L1 in case of default in memory cache",
          "timestamp": "2023-11-17T11:30:35-08:00",
          "tree_id": "1d7c2f47b8b9893dd334d2726f49cd6580541a6a",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/9792dc6d7e7459a1d52ee0fda28e20dbd57111d8"
        },
        "date": 1700249886510,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 23925.904522705077,
            "unit": "ns",
            "range": "± 112.64353218509001"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 37231.91484187199,
            "unit": "ns",
            "range": "± 358.14622868085496"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 12508.651259358723,
            "unit": "ns",
            "range": "± 62.84085017201559"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 25392.409553019206,
            "unit": "ns",
            "range": "± 119.96229643918328"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 46248.46153846154,
            "unit": "ns",
            "range": "± 404.42234841491626"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 168831.1625,
            "unit": "ns",
            "range": "± 17581.441497361877"
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
          "id": "067159b7259f648798d0762a72b24ef4b3d5c8a3",
          "message": "Use net6.0",
          "timestamp": "2024-01-24T05:19:44Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/067159b7259f648798d0762a72b24ef4b3d5c8a3"
        },
        "date": 1706074027266,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 17883.24646935096,
            "unit": "ns",
            "range": "± 72.65848236947141"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 28279.839017427883,
            "unit": "ns",
            "range": "± 42.544096874464636"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 9269.53053894043,
            "unit": "ns",
            "range": "± 75.3422909751619"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 20168.53321940104,
            "unit": "ns",
            "range": "± 154.26821631788948"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 38942.357142857145,
            "unit": "ns",
            "range": "± 416.41877167893546"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 144184.75490196078,
            "unit": "ns",
            "range": "± 5885.548799706405"
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
          "id": "067159b7259f648798d0762a72b24ef4b3d5c8a3",
          "message": "Use net6.0",
          "timestamp": "2024-01-24T05:19:44Z",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/067159b7259f648798d0762a72b24ef4b3d5c8a3"
        },
        "date": 1706074027266,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 17883.24646935096,
            "unit": "ns",
            "range": "± 72.65848236947141"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 28279.839017427883,
            "unit": "ns",
            "range": "± 42.544096874464636"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 9269.53053894043,
            "unit": "ns",
            "range": "± 75.3422909751619"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 20168.53321940104,
            "unit": "ns",
            "range": "± 154.26821631788948"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 38942.357142857145,
            "unit": "ns",
            "range": "± 416.41877167893546"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 144184.75490196078,
            "unit": "ns",
            "range": "± 5885.548799706405"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "simon.cropp@gmail.com",
            "name": "Simon Cropp",
            "username": "SimonCropp"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "16827a2ccbfcaa607de7295520b5f097446c261a",
          "message": "reduce some string alloc on building (#4564)",
          "timestamp": "2024-01-24T08:51:29Z",
          "tree_id": "2559765585e66470a99924346ae757d15c86668f",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/16827a2ccbfcaa607de7295520b5f097446c261a"
        },
        "date": 1706086733130,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 17388.152539934432,
            "unit": "ns",
            "range": "± 39.85734136412449"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 28815.60500488281,
            "unit": "ns",
            "range": "± 179.92456816203457"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 9272.12839457194,
            "unit": "ns",
            "range": "± 65.49789168362328"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 20171.034465244837,
            "unit": "ns",
            "range": "± 179.60199241681337"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 39059.269230769234,
            "unit": "ns",
            "range": "± 480.22115631691923"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 143815.42,
            "unit": "ns",
            "range": "± 18774.74498619264"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "simon.cropp@gmail.com",
            "name": "Simon Cropp",
            "username": "SimonCropp"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "16827a2ccbfcaa607de7295520b5f097446c261a",
          "message": "reduce some string alloc on building (#4564)",
          "timestamp": "2024-01-24T08:51:29Z",
          "tree_id": "2559765585e66470a99924346ae757d15c86668f",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/16827a2ccbfcaa607de7295520b5f097446c261a"
        },
        "date": 1706086733130,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 17388.152539934432,
            "unit": "ns",
            "range": "± 39.85734136412449"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 28815.60500488281,
            "unit": "ns",
            "range": "± 179.92456816203457"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 9272.12839457194,
            "unit": "ns",
            "range": "± 65.49789168362328"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 20171.034465244837,
            "unit": "ns",
            "range": "± 179.60199241681337"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 39059.269230769234,
            "unit": "ns",
            "range": "± 480.22115631691923"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 143815.42,
            "unit": "ns",
            "range": "± 18774.74498619264"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "simon.cropp@gmail.com",
            "name": "Simon Cropp",
            "username": "SimonCropp"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "1f999fb8520ae1bc0062ac77fe686c241c1eb541",
          "message": "remove unreachable code in GetParsedAuthValueElements (#4541)",
          "timestamp": "2024-01-24T09:41:02Z",
          "tree_id": "89f97b9ffd703f2b73f57c1daee8a46414d1995c",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/1f999fb8520ae1bc0062ac77fe686c241c1eb541"
        },
        "date": 1706089713212,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 17291.83497619629,
            "unit": "ns",
            "range": "± 38.60922460924901"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 28842.86880493164,
            "unit": "ns",
            "range": "± 189.8864354636268"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 9369.697573148287,
            "unit": "ns",
            "range": "± 35.53898928202293"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 19801.6011329064,
            "unit": "ns",
            "range": "± 15.991831050067564"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 46983.10526315789,
            "unit": "ns",
            "range": "± 1027.975999219223"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 138978.20408163266,
            "unit": "ns",
            "range": "± 13981.08851414637"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "simon.cropp@gmail.com",
            "name": "Simon Cropp",
            "username": "SimonCropp"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "1f999fb8520ae1bc0062ac77fe686c241c1eb541",
          "message": "remove unreachable code in GetParsedAuthValueElements (#4541)",
          "timestamp": "2024-01-24T09:41:02Z",
          "tree_id": "89f97b9ffd703f2b73f57c1daee8a46414d1995c",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/1f999fb8520ae1bc0062ac77fe686c241c1eb541"
        },
        "date": 1706089713212,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (1, 10))",
            "value": 17291.83497619629,
            "unit": "ns",
            "range": "± 38.60922460924901"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.AcquireTokenSilent_TestAsync(CacheSize: (10000, 10))",
            "value": 28842.86880493164,
            "unit": "ns",
            "range": "± 189.8864354636268"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 9369.697573148287,
            "unit": "ns",
            "range": "± 35.53898928202293"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.GetAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 19801.6011329064,
            "unit": "ns",
            "range": "± 15.991831050067564"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (1, 10))",
            "value": 46983.10526315789,
            "unit": "ns",
            "range": "± 1027.975999219223"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.TokenCacheTests.RemoveAccountAsync_TestAsync(CacheSize: (10000, 10))",
            "value": 138978.20408163266,
            "unit": "ns",
            "range": "± 13981.08851414637"
          }
        ]
      }
    ]
  }
}