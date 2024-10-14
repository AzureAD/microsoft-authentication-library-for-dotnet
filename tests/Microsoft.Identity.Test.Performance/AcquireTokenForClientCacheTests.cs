// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Performance.Helpers;
using Microsoft.Identity.Test.Unit;
#if USE_IDENTITY_WEB
using Microsoft.Identity.Web;
#endif

namespace Microsoft.Identity.Test.Performance
{
    /// <summary>
    /// Used to test the performance of acquiring tokens using token cache with different number of items.
    /// </summary>
    /// <remarks>
    /// For app cache, the number of partitions is the number of tenants.
    /// </remarks>
    [MinColumn, MaxColumn]
    public class AcquireTokenForClientCacheTests
    {
        private readonly string _tenantPrefix = "l6a331n5-4fh7-7788-a78a-96f19f5d7a73";
        private readonly string _scopePrefix = "https://resource.com/.default";
        private ConfidentialClientApplication _cca;
        private InMemoryCache _serializationCache;
        private string _tenantId;
        private string _scope;
        private AuthenticationResult AuthResult;
        private static string constraintAsString = "[{\"Version\":\"1.0\",\"Type\":\"wk:user\",\"Action\":\"U\",\"Targets\":[{\"Value\":\"constraint1\",\"Policy\":\"pol1\",\"AdditionalProperties\":null},{\"Value\":\"constraint2\",\"Policy\":\"pol2\",\"AdditionalProperties\":null}],\"AdditionalProperties\":null}]";
        CdtAuthenticationOperation cdtAuthenticationOperation = new CdtAuthenticationOperation(constraintAsString);
        private static string ticket = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjNQYUs0RWZ5Qk5RdTNDdGpZc2EzWW1oUTVFMCIsImtpZCI6IjNQYUs0RWZ5Qk5RdTNDdGpZc2EzWW1oUTVFMCJ9.eyJhdWQiOiI4OGY5MWVhYy1jNjA2LTRjNjctYTBlMi1hNWU4YTE4Njg1NGYiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC9mNjQ1YWQ5Mi1lMzhkLTRkMWEtYjUxMC1kMWIwOWE3NGE4Y2EvIiwiaWF0IjoxNzI4NTE0Njg0LCJuYmYiOjE3Mjg1MTQ2ODQsImV4cCI6MTcyODUxODU4NCwiYWlvIjoiazJCZ1lHalpjMmxHNVFzcjg0My9EOGV4L3RndkJnQT0iLCJhcHBpZCI6Ijg4ZjkxZWFjLWM2MDYtNGM2Ny1hMGUyLWE1ZThhMTg2ODU0ZiIsImFwcGlkYWNyIjoiMSIsImlkcCI6Imh0dHBzOi8vc3RzLndpbmRvd3MubmV0L2Y2NDVhZDkyLWUzOGQtNGQxYS1iNTEwLWQxYjA5YTc0YThjYS8iLCJvaWQiOiI2NDg1MTFlZi01MmRhLTRiMmMtOTBhZC00ZDc1Yzk2YWExYzYiLCJyaCI6IjEuQVJ3QWtxMUY5bzNqR2syMUVOR3dtblNveXF3ZS1ZZ0d4bWRNb09LbDZLR0doVThjQUFBY0FBLiIsInN1YiI6IjY0ODUxMWVmLTUyZGEtNGIyYy05MGFkLTRkNzVjOTZhYTFjNiIsInRpZCI6ImY2NDVhZDkyLWUzOGQtNGQxYS1iNTEwLWQxYjA5YTc0YThjYSIsInV0aSI6IkxBdXVleGZMMDBtSmVJX0lncTBHQUEiLCJ2ZXIiOiIxLjAiLCJ4bXNfZHNfY25mIjp7Imp3ayI6eyJlIjoiQVFBQiIsImt0eSI6IlJTQSIsIm4iOiJvTEpwXzZHWHVIT2NCZkN4ZzczaUQ1M2RsM3kybnF0VS1QLUMyNkVLeUxUM0x2YV8tWDFOUGhiYU5xa3NlODE0S3RSVGJCcmhzdFFyV3p0RFJ3VC1nYXV3ZXVFVWJjdFVRMGtTeE9PcUF5enNybnZFWmxwaXJJOFNrSWZjUk9hc1pRVHI2WS11UGtUMUhoaTlSU1lsRF9vTEpNaFR6UnlzYXEzWXFfQkx4VzlrMlpJSm5sQkJTZmowMHdtcEpYN085VG40X0ZTYnNMc3VaclhPOGF2Wm01dDEwYzZYd0FqYm5WVnZQbmNqZWxwM05aTER1Z0VYdDVWYllla3ZlWHowZnZNS0ZDeG5lN0lWM0VEYmN1V3pBZ3NOMHFCMWtnVktlNmVnb3BDMkNMdjdmUmdoMVQ0b0hiV1pIVFVjRnlRVDdpYm44TnBtWUpVSHpIUWNkaXdlYVEifX0sInhtc19kc19ub25jZSI6Ilk2dk8xUVZlYUF6ampoVVFEQVNua1hudnkwRFZJVHFwLUNKd0tJQzVPRTAifQ.NM57MDkr7ovqg37fG0tSn_ODxFYXqBpR0tuShPJ84pgkRfAX5CvXFYLxva7PfgDxpqqVuIuOFFIawDiPRTaYwTxDJpzjbZer_v_TckqpVlNMLq4rV5CyPvcdRMdYfsOELQj3uNrdI2fJc66sEvhJFfGuAu3k4DSaresUydROKjbEgwqa83v-0BZM5e0fpQpQSvYsg67oasYvH0qJhMBN5sxk_pWbo9DCKFgfqBARB1wZfV7_atuorh3JhL1jjiv0_R4omvQX8OTv5heEUV9MoCJ5tJ8JKv2C_98NsMwbVxffjD4RqLV6spLG96Q8J1EfS2OTxy7mEt8d1509P84AKQ";
        // i.e. (partitions, tokens per partition)
        [ParamsSource(nameof(CacheSizeSource))]
        public (int TotalTenants, int TokensPerTenant) CacheSize { get; set; }

        // By default, benchmarks are run for all combinations of params.
        // This is a workaround to specify the exact param combinations to be used.
        public IEnumerable<(int, int)> CacheSizeSource => new[] {
            (1, 10),
            (10000, 10),
        };

        [ParamsAllValues]
        public bool EnableCacheSerialization { get; set; }

        //[Params(false)]
        public bool UseMicrosoftIdentityWebCache { get; set; }

        [GlobalSetup]
        public async Task GlobalSetupAsync()
        {
            _cca = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithRedirectUri(TestConstants.RedirectUri)
                .WithClientSecret(TestConstants.ClientSecret)
                .WithLegacyCacheCompatibility(false)
                .BuildConcrete();

            if (EnableCacheSerialization)
            {
                if (UseMicrosoftIdentityWebCache)
                {
#if USE_IDENTITY_WEB
                    (_cca as IConfidentialClientApplication).AddInMemoryTokenCache();
#endif
                }
                else
                {
                    _serializationCache = new InMemoryCache(_cca.AppTokenCache);
                }
            }

            await PopulateAppCacheAsync(_cca, CacheSize.TotalTenants, CacheSize.TokensPerTenant, EnableCacheSerialization).ConfigureAwait(false);

            _tenantId = $"{_tenantPrefix}0";
            _scope = $"{_scopePrefix}0";

            Dictionary<string, string> _additionalResponseParameters = new Dictionary<string, string>
            {
                { CdtAuthenticationOperation.CdtNonce, "Y6vO1QVeaAzjjhUQDASnkXnvy0DVITqp-CJwKIC5OE0" },
                { CdtAuthenticationOperation.CdtEncKey, "encKey" }
            };


            MsalAccessTokenCacheItem msalAccessTokenCacheItem = TokenCacheHelper.CreateAccessTokenItem();
            msalAccessTokenCacheItem.Secret = ticket;

            AuthResult = new AuthenticationResult(msalAccessTokenCacheItem, null, cdtAuthenticationOperation, Guid.NewGuid(), TokenSource.IdentityProvider, default, default, default, _additionalResponseParameters);


            //AuthResult = new AuthenticationResult(
            //                                     ticket,
            //                                     false,
            //                                     "uid",
            //                                     DateTime.UtcNow,
            //                                     DateTime.UtcNow,
            //                                     "tid",
            //                                     new Account("aid", "user", "env"),
            //                                     "idt",
            //                                     new[] { "scope" },
            //                                     Guid.NewGuid(),
            //                                     new AuthenticationResultMetadata(TokenSource.IdentityProvider));
        }

        [Benchmark(Description = "Cdt test---------------------------------------------------------------")]
        [BenchmarkCategory("With cache")]
        public void AcquireTokenForClient_TestAsync()
        {
            cdtAuthenticationOperation.FormatResult(AuthResult);
            AuthResult.AccessToken = ticket;
            //return await _cca.AcquireTokenForClient(new[] { _scope })
            //  .WithTenantId(_tenantId)
            //  .ExecuteAsync()
            //  .ConfigureAwait(false);
        }

        /// <summary>
        /// Create a fake token and save into the internal cache.
        /// If cache serialization is enabled, call an event handler to serialize current cache state into external cache,
        /// then clear the internal cache before new token is inserted.
        /// </summary>
        private async Task PopulateAppCacheAsync(ConfidentialClientApplication cca, int totalTenants, int tokensPerTenant, bool enableCacheSerialization)
        {
            for (int tenant = 0; tenant < totalTenants; tenant++)
            {
                string key = CacheKeyFactory.GetClientCredentialKey(_cca.AppConfig.ClientId, $"{_tenantPrefix}{tenant}", "");

                for (int token = 0; token < tokensPerTenant; token++)
                {
                    MsalAccessTokenCacheItem atItem = TokenCacheHelper.CreateAccessTokenItem(
                        scopes: $"{_scopePrefix}{token}",
                        tenant: $"{_tenantPrefix}{tenant}",
                        accessToken: TestConstants.AppAccessToken);

                    cca.AppTokenCacheInternal.Accessor.SaveAccessToken(atItem);
                }

                if (enableCacheSerialization)
                {
                    var args = new TokenCacheNotificationArgs(
                         cca.AppTokenCacheInternal,
                         cca.AppConfig.ClientId,
                         account: null,
                         hasStateChanged: true,
                         isApplicationCache: true,
                         suggestedCacheKey: key,
                         hasTokens: true,
                         suggestedCacheExpiry: null,
                         cancellationToken: CancellationToken.None);
                    await cca.AppTokenCacheInternal.OnAfterAccessAsync(args).ConfigureAwait(false);
                    cca.AppTokenCacheInternal.Accessor.Clear();
                }
            }
        }
    }
}
