To run the perf tests, start this webapi, install a tool like https://github.com/codesenberg/bombardier and hit it with:

bombardier -d 60s -l https://localhost:44355/FlatCache
bombardier -d 60s -l https://localhost:44355/StaticDictionary
bombardier -d 60s -l https://localhost:44355/Singleton
bombardier -d 60s -l https://localhost:44355/WilsonLruCache
bombardier -d 60s -l https://localhost:44355/Obo?refreshFlow=true
bombardier -d 60s -l https://localhost:44355/Obo?refreshFlow=false

Measurements taken on 4/30/2021, on my dev machine, assuming:

- cache starts off empty (cold), or full (warm)
- there are 500 tenants in total, each request goes to a random tenant
- a call to ESTS takes 100 ms
- cache hit ratio of 95% (to simulate token expiry)'

Conclusion: 

- make app cache static
- use Wilson's LRU cache with a 2GB limit 


Flat cache
----------

You'd get this perf profile if using a static / singleton Confidential Client App with MSAL < 4.30
https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/blob/master/tests/devapps/WebApi/Controllers/FlatCacheController.cs#L14

              Cold     Warm
     50%      0.83s    0.97s 
     75%      1.07s    1.21s
     90%      1.41s    1.93s
     95%      1.55s    2.62s
     99%      1.86s    3.90s

Conclusion: better to not have a cache at all, but ESTS will throttle you!


Singleton 
---------

https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/blob/master/tests/devapps/WebApi/Controllers/SingletonController.cs#L12

            Cold      Warm
     50%    16.00ms   26.00ms
     75%    20.00ms   29.00ms
     90%    27.00ms   38.00ms
     95%   119.00ms   136.00ms
     99%   147.00ms   203.00ms

Conclusion: This is the perf profile in MSAL 4.30+ if CCA is singleton. It's 2-3 times slower than using a dictionary, because of internal locking.


Static dictionary
-----------------

One CCA per requset, all using the same static app cache.

https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/blob/master/tests/devapps/WebApi/Controllers/StaticDictionaryController.cs#L14

            Cold      Warm
     50%    11.00ms   8.00ms
     75%    13.00ms   10.00ms
     90%    17.00ms   12.00ms
     95%    21.00ms   14.00ms
     99%   146.00ms   54.00ms

Conclusion: better to go back to ADAL strategy for app token cache, i.e. use a static cache!


Static Wilson LRU cache with agreesive size limitation (max size 250 tokens or ~500KB)
---------------------------------------------------------------------------------------

https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/blob/master/tests/devapps/WebApi/Controllers/WilsonLruCacheController.cs#L15

            Cold      Warm 
     50%    13.00ms   9.00ms 
     75%    18.00ms   10.01ms
     90%   110.00ms   13.00ms
     95%   123.01ms   16.00ms
     99%   167.00ms   106.00ms

Static Wilson LRU cache with normal size limitation (max size ~2GB or 700k tokens)
----------------------------------------------------------------------------------

            Cold      Warm
     50%     9.00ms   9.00ms
     75%    11.00ms   11.00ms
     90%    14.00ms   14.00ms
     95%    18.00ms   17.00ms
     99%   116.00ms   112.00ms

OBO performance tests for refresh flow. Added with 4.33 release.
----------------------------------------------------------------

https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/blob/master/tests/devapps/WebApi/Controllers/OboController.cs#L16
Measurements taken on 6/15/2021, on dev machine:

OBO tests with refreshFlow=false (Number of users: 300, Expiration time: 10 mins, Cache access penalty: 100ms)
------------------------------------------------------------------------------------
The strategy here is to set a high expiration time so the 1st 300 requests goes to Identity Provider and rest gets a response from the AT cache.

Statistics        Avg      Stdev        Max
  Reqs/sec       580.59    1253.94   32019.21
  Latency      229.72ms    28.89ms      1.12s
  Latency Distribution
     50%   224.10ms
     75%   234.11ms
     90%   242.66ms
     95%   247.53ms
     99%   260.50ms
  HTTP codes:
    1xx - 0, 2xx - 32706, 3xx - 0, 4xx - 0, 5xx - 0
    others - 0
  Throughput:   190.64KB/s

OBO tests with refreshFlow=true (Number of users: 50, Expiration time: 2 mins, Cache access penalty: 100ms)
---------------------------------------------------------------------------------
The strategy here is to set a low expiration time and low number of users. Since while we get the AT from the cache we ignore the one that have less than 5 mins of expiry all the
requests after the initial requests for 50 users will hit the refresh flow.

Statistics        Avg      Stdev        Max
  Reqs/sec       609.93    1769.33   54508.18
  Latency      227.40ms    20.26ms   766.14ms
  Latency Distribution
     50%   224.59ms
     75%   233.71ms
     90%   239.89ms
     95%   244.04ms
     99%   253.50ms
  HTTP codes:
    1xx - 0, 2xx - 33022, 3xx - 0, 4xx - 0, 5xx - 0
    others - 0
  Throughput:   192.70KB/s
