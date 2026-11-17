// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;

namespace Microsoft.Identity.Test.Performance
{
    public partial class Program
    {
        static partial void AddSnapshotBenchmarks(List<Type> benchmarks)
        {
            benchmarks.Add(typeof(TokenCacheSnapshotBenchmarks));
            benchmarks.Add(typeof(TokenCacheRetentionBenchmarks));
        }
    }

    [MemoryDiagnoser]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    public class TokenCacheSnapshotBenchmarks
    {
        private const string ClientId = "snapshot-benchmark";
        private static readonly string[] s_tenants =
        {
            "11111111-1111-1111-1111-111111111111",
            "22222222-2222-2222-2222-222222222222"
        };
        private static readonly string[] s_scopes = { "api://resource-0/.default" };
        private static readonly string s_accessToken = new string('x', 3000);

        private ITokenCacheSerializer _cache;
        private ITokenCacheSnapshotProvider _provider;
        private byte[][] _bytes;
        private TokenCacheSnapshot[] _snapshots;
        private IConfidentialClientApplication _jsonApp;
        private IConfidentialClientApplication _snapshotApp;
        private int _next;

        [Params(1, 10, 100)]
        public int TokensPerPartition { get; set; }

        [GlobalSetup(Targets = new[] { nameof(JsonPartitionRead), nameof(SnapshotPartitionRead), nameof(JsonPartitionWrite), nameof(SnapshotPartitionWrite) })]
        public void Setup()
        {
            var cache = ConfidentialClientApplicationBuilder.Create(ClientId).Build().AppTokenCache;
            _cache = (ITokenCacheSerializer)cache;
            _provider = (ITokenCacheSnapshotProvider)cache;
            _bytes = new[] { CreatePartition(s_tenants[0]), CreatePartition(s_tenants[1]) };
            _snapshots = new TokenCacheSnapshot[2];
            for (int i = 0; i < _bytes.Length; i++)
            {
                _cache.DeserializeMsalV3(_bytes[i], true);
                _snapshots[i] = _provider.CreateSnapshot();
                var expected = JsonNode.Parse(_cache.SerializeMsalV3());
                _provider.RestoreSnapshot(_snapshots[i]);
                if (!JsonNode.DeepEquals(expected, JsonNode.Parse(_cache.SerializeMsalV3())))
                {
                    throw new InvalidOperationException("Snapshot benchmark setup changed persisted cache state.");
                }
            }
        }

        [GlobalSetup(Targets = new[] { nameof(JsonAcquireTokenForClient), nameof(SnapshotAcquireTokenForClient) })]
        public async Task SetupAcquisitionAsync()
        {
            Setup();
            var jsonPartitions = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            var snapshotPartitions = new Dictionary<string, TokenCacheSnapshot>(StringComparer.Ordinal);
            for (int i = 0; i < s_tenants.Length; i++)
            {
                string key = CacheKeyFactory.GetAppTokenCacheItemKey(ClientId, s_tenants[i], "");
                jsonPartitions.Add(key, _bytes[i]);
                snapshotPartitions.Add(key, _snapshots[i]);
            }

            _jsonApp = CreateApplication();
            _snapshotApp = CreateApplication();
            _jsonApp.AppTokenCache.SetBeforeAccess(args =>
                args.TokenCache.DeserializeMsalV3(jsonPartitions[args.SuggestedCacheKey], shouldClearExistingCache: true));
            _snapshotApp.AppTokenCache.SetBeforeAccess(args =>
                ((ITokenCacheSnapshotProvider)args.TokenCache).RestoreSnapshot(snapshotPartitions[args.SuggestedCacheKey]));
            _jsonApp.AppTokenCache.SetAfterAccess(ValidateUnchangedCache);
            _snapshotApp.AppTokenCache.SetAfterAccess(ValidateUnchangedCache);

            for (int i = 0; i < s_tenants.Length * 2; i++)
            {
                int partition = i & 1;
                _next = partition;
                var jsonResult = await JsonAcquireTokenForClient().ConfigureAwait(false);
                _next = partition;
                var snapshotResult = await SnapshotAcquireTokenForClient().ConfigureAwait(false);
                ValidateCachedResult(jsonResult, s_tenants[partition]);
                ValidateCachedResult(snapshotResult, s_tenants[partition]);
            }
            _next = 0;
        }

        [Benchmark(Baseline = true)]
        [BenchmarkCategory("Read")]
        public void JsonPartitionRead()
        {
            _cache.DeserializeMsalV3(_bytes[_next++ & 1], true);
        }

        [Benchmark]
        [BenchmarkCategory("Read")]
        public void SnapshotPartitionRead()
        {
            _provider.RestoreSnapshot(_snapshots[_next++ & 1]);
        }

        [Benchmark(Baseline = true)]
        [BenchmarkCategory("Write")]
        public byte[] JsonPartitionWrite()
        {
            return _cache.SerializeMsalV3();
        }

        [Benchmark]
        [BenchmarkCategory("Write")]
        public TokenCacheSnapshot SnapshotPartitionWrite()
        {
            return _provider.CreateSnapshot();
        }

        [Benchmark(Baseline = true)]
        [BenchmarkCategory("AcquireTokenForClient")]
        public Task<AuthenticationResult> JsonAcquireTokenForClient()
        {
            return AcquireTokenForClientAsync(_jsonApp);
        }

        [Benchmark]
        [BenchmarkCategory("AcquireTokenForClient")]
        public Task<AuthenticationResult> SnapshotAcquireTokenForClient()
        {
            return AcquireTokenForClientAsync(_snapshotApp);
        }

        private Task<AuthenticationResult> AcquireTokenForClientAsync(IConfidentialClientApplication app)
        {
            return app.AcquireTokenForClient(s_scopes)
                .WithTenantId(s_tenants[_next++ & 1])
                .ExecuteAsync();
        }

        private static IConfidentialClientApplication CreateApplication()
        {
            return ConfidentialClientApplicationBuilder.Create(ClientId)
                .WithClientSecret("synthetic-benchmark-credential")
                .WithAuthority($"https://login.microsoftonline.com/{s_tenants[0]}")
                .WithInstanceDiscovery(false)
                .WithLegacyCacheCompatibility(false)
                .WithHttpClientFactory(new NoNetworkFactory())
                .Build();
        }

        private static void ValidateCachedResult(AuthenticationResult result, string tenant)
        {
            if (result.AuthenticationResultMetadata.TokenSource != TokenSource.Cache ||
                !string.Equals(result.AccessToken, s_accessToken, StringComparison.Ordinal) ||
                !string.Equals(result.TenantId, tenant, StringComparison.Ordinal) ||
                !result.Scopes.SequenceEqual(s_scopes))
            {
                throw new InvalidOperationException("Warm-cache benchmark did not return the expected cached token.");
            }
        }

        private static void ValidateUnchangedCache(TokenCacheNotificationArgs args)
        {
            if (args.HasStateChanged)
            {
                throw new InvalidOperationException("Warm-cache benchmark unexpectedly changed persisted cache state.");
            }
        }

        private sealed class NoNetworkFactory : IMsalHttpClientFactory
        {
            public HttpClient GetHttpClient()
            {
                throw new InvalidOperationException("Warm-cache benchmarks must not access the network.");
            }
        }

        internal byte[] CreatePartition(string tenant)
        {
            long cachedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var tokens = new JsonObject();
            for (int i = 0; i < TokensPerPartition; i++)
            {
                tokens[i.ToString(System.Globalization.CultureInfo.InvariantCulture)] = new JsonObject
                {
                    ["home_account_id"] = "",
                    ["environment"] = "login.microsoftonline.com",
                    ["credential_type"] = "AccessToken",
                    ["client_id"] = ClientId,
                    ["secret"] = s_accessToken,
                    ["realm"] = tenant,
                    ["target"] = $"api://resource-{i}/.default",
                    ["cached_at"] = cachedAt.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["expires_on"] = (cachedAt + 3600).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["extended_expires_on"] = (cachedAt + 7200).ToString(System.Globalization.CultureInfo.InvariantCulture)
                };
            }
            return Encoding.UTF8.GetBytes(new JsonObject { ["AccessToken"] = tokens }.ToJsonString());
        }
    }
}
