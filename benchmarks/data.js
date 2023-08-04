window.BENCHMARK_DATA = {
  "lastUpdate": 1691190272593,
  "repoUrl": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet",
  "entries": {
    "AcquireTokenNoCache": [
      {
        "commit": {
          "author": {
            "email": "34331512+pmaytak@users.noreply.github.com",
            "name": "pmaytak",
            "username": "pmaytak"
          },
          "committer": {
            "email": "34331512+pmaytak@users.noreply.github.com",
            "name": "pmaytak",
            "username": "pmaytak"
          },
          "distinct": true,
          "id": "48ae66a35021e4108f2a69691b4df54c5ce66979",
          "message": "Fix test naming.",
          "timestamp": "2023-08-04T15:52:22-07:00",
          "tree_id": "a5b5a627d3b9fd3f6283433be8bfa95044a778c8",
          "url": "https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/commit/48ae66a35021e4108f2a69691b4df54c5ce66979"
        },
        "date": 1691190267376,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenForClient_TestAsync",
            "value": 468549.85,
            "unit": "ns",
            "range": "± 16587.439884612726"
          },
          {
            "name": "Microsoft.Identity.Test.Performance.AcquireTokenNoCacheTests.AcquireTokenOnBehalfOf_TestAsync",
            "value": 648263.6153846154,
            "unit": "ns",
            "range": "± 10366.559848365492"
          }
        ]
      }
    ]
  }
}