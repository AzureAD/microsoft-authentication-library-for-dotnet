// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.AuthScheme.Bearer;

namespace TenantIdRaceConditionTest
{
    /// <summary>
    /// Simple validation test for the TenantId race condition fix.
    /// This test validates that AuthenticationResult.TenantId is properly populated
    /// when there is no ID token (like in client credentials flow).
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Testing TenantId population in AuthenticationResult...");
            
            // Simulate a client credentials token response (no ID token)
            string expectedTenantId = "test-tenant-123";
            
            var tokenResponse = new MsalTokenResponse
            {
                AccessToken = "test-access-token",
                ExpiresIn = 3600,
                Scope = "https://graph.microsoft.com/.default",
                TokenType = "Bearer"
            };

            // Create an access token cache item (this would normally come from the token response)
            var accessTokenCacheItem = new MsalAccessTokenCacheItem(
                preferredCacheEnv: "login.microsoftonline.com",
                clientId: "test-client-id",
                response: tokenResponse,
                tenantId: expectedTenantId,  // This is what gets set from the request authority
                homeAccountId: "homeAccountId",
                keyId: null,
                oboCacheKey: null,
                persistedCacheParameters: null,
                cacheKeyComponents: null
            );

            // Create AuthenticationResult (simulating what happens internally)
            // In the bug scenario, msalIdTokenCacheItem would be null (no ID token in client credentials)
            var authResult = AuthenticationResult.CreateAsync(
                msalAccessTokenCacheItem: accessTokenCacheItem,
                msalIdTokenCacheItem: null,  // No ID token in client credentials flow
                authenticationScheme: new BearerAuthenticationScheme(),
                correlationId: Guid.NewGuid(),
                tokenSource: Microsoft.Identity.Client.Cache.TokenSource.IdentityProvider,
                apiEvent: new ApiEvent(Guid.NewGuid()),
                account: null,
                spaAuthCode: null,
                additionalResponseParameters: null
            ).Result;

            // Validate the fix: TenantId should now be populated from the access token
            if (string.IsNullOrEmpty(authResult.TenantId))
            {
                Console.WriteLine("❌ FAILED: TenantId is null or empty!");
                Console.WriteLine("   This indicates the bug is not fixed.");
                Environment.Exit(1);
            }

            if (authResult.TenantId != expectedTenantId)
            {
                Console.WriteLine($"❌ FAILED: TenantId mismatch!");
                Console.WriteLine($"   Expected: {expectedTenantId}");
                Console.WriteLine($"   Actual:   {authResult.TenantId}");
                Environment.Exit(1);
            }

            Console.WriteLine($"✅ SUCCESS: TenantId is correctly populated: {authResult.TenantId}");
            Console.WriteLine("   The fix prevents the race condition where TenantId was missing.");
            Environment.Exit(0);
        }
    }
}
