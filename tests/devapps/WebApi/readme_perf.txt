Measurements taken on 4/30/2021, on my dev machine, assuming:

- cache starts off empty (cold), or full (warm)
- there are 500 tenants in total, each request goes to a random tenant
- a call to ESTS takes 100 ms
- cache hit ratio of 95% (to simulate token expiry)'

Conclusion: 

- make app cache static
- use Wilson's LRU cache with a 2GB limit 

No cache
--------

Initialize a CCA per request. Do not serialize the cache. A lot of apps do this because our samples indicate it (for simplicity).
           
     50%   119.99ms
     75%   128.00ms
     90%   138.59ms
     95%   147.00ms
     99%   208.00ms

Conclusion: not catastrophic for the app, but catastrophic for ESTS due to massive load. ESTS might throttle apps that do this.


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




To run the perf tests, start this webapi, install a tool like https://github.com/codesenberg/bombardier and hit it with:

bombardier -d 60s -l https://localhost:44355/FlatCache
bombardier -d 60s -l https://localhost:44355/StaticDictionary
bombardier -d 60s -l https://localhost:44355/Singleton
bombardier -d 60s -l https://localhost:44355/WilsonLruCache
