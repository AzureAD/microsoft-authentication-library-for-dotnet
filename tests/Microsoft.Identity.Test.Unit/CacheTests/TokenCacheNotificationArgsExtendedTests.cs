// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Identity.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CacheTests
{
    [TestClass]
    public class TokenCacheNotificationArgsExtendedTests : TestBase
    {
        [TestMethod]
        public void Constructor_BasicOverload_SetsAllProperties()
        {
            var cts = new CancellationTokenSource();
            var expiry = DateTimeOffset.UtcNow.AddHours(1);

            var args = new TokenCacheNotificationArgs(
                tokenCache: null,
                clientId: "client-id",
                account: null,
                hasStateChanged: true,
                isApplicationCache: true,
                suggestedCacheKey: "cache-key",
                hasTokens: true,
                suggestedCacheExpiry: expiry,
                cancellationToken: cts.Token);

            Assert.AreEqual("client-id", args.ClientId);
            Assert.IsTrue(args.HasStateChanged);
            Assert.IsTrue(args.IsApplicationCache);
            Assert.AreEqual("cache-key", args.SuggestedCacheKey);
            Assert.IsTrue(args.HasTokens);
            Assert.AreEqual(expiry, args.SuggestedCacheExpiry);
            Assert.AreEqual(cts.Token, args.CancellationToken);
            Assert.IsNull(args.TokenCache);
            Assert.IsNull(args.Account);
        }

        [TestMethod]
        public void Constructor_WithCorrelationId_SetsCorrelationId()
        {
            var correlationId = Guid.NewGuid();
            var args = new TokenCacheNotificationArgs(
                tokenCache: null,
                clientId: "client-id",
                account: null,
                hasStateChanged: false,
                isApplicationCache: false,
                suggestedCacheKey: null,
                hasTokens: false,
                suggestedCacheExpiry: null,
                cancellationToken: CancellationToken.None,
                correlationId: correlationId);

            Assert.AreEqual(correlationId, args.CorrelationId);
        }

        [TestMethod]
        public void Constructor_WithScopesAndTenant_SetsFields()
        {
            var scopes = new[] { "User.Read", "Mail.Read" };
            var args = new TokenCacheNotificationArgs(
                tokenCache: null,
                clientId: "client-id",
                account: null,
                hasStateChanged: false,
                isApplicationCache: false,
                suggestedCacheKey: null,
                hasTokens: false,
                suggestedCacheExpiry: null,
                cancellationToken: CancellationToken.None,
                correlationId: Guid.Empty,
                requestScopes: scopes,
                requestTenantId: "tenant-id");

            CollectionAssert.AreEqual(scopes, new List<string>(args.RequestScopes));
            Assert.AreEqual("tenant-id", args.RequestTenantId);
        }

        [TestMethod]
        public void Constructor_FullOverload_SetsAllFields()
        {
            var correlationId = Guid.NewGuid();
            var scopes = new[] { "scope1" };
            var expiry = DateTimeOffset.UtcNow.AddMinutes(30);

            var args = new TokenCacheNotificationArgs(
                tokenCache: null,
                clientId: "cid",
                account: null,
                hasStateChanged: true,
                isApplicationCache: true,
                suggestedCacheKey: "key",
                hasTokens: true,
                suggestedCacheExpiry: expiry,
                cancellationToken: CancellationToken.None,
                correlationId: correlationId,
                requestScopes: scopes,
                requestTenantId: "tid",
                identityLogger: null,
                piiLoggingEnabled: true);

            Assert.AreEqual("cid", args.ClientId);
            Assert.IsTrue(args.HasStateChanged);
            Assert.IsTrue(args.IsApplicationCache);
            Assert.AreEqual("key", args.SuggestedCacheKey);
            Assert.IsTrue(args.HasTokens);
            Assert.AreEqual(expiry, args.SuggestedCacheExpiry);
            Assert.AreEqual(correlationId, args.CorrelationId);
            Assert.AreEqual("tid", args.RequestTenantId);
            Assert.IsTrue(args.PiiLoggingEnabled);
        }

        [TestMethod]
        public void Constructor_FullOverload_DefaultsTelemetryData_WhenNull()
        {
            var args = new TokenCacheNotificationArgs(
                tokenCache: null,
                clientId: "cid",
                account: null,
                hasStateChanged: false,
                isApplicationCache: false,
                suggestedCacheKey: null,
                hasTokens: false,
                suggestedCacheExpiry: null,
                cancellationToken: CancellationToken.None,
                correlationId: Guid.Empty,
                requestScopes: null,
                requestTenantId: null,
                identityLogger: null,
                piiLoggingEnabled: false,
                telemetryData: null);

            Assert.IsNotNull(args.TelemetryData, "TelemetryData should be defaulted when null");
        }

        [TestMethod]
        public void HasStateChanged_CanBeSetInternally()
        {
            var args = new TokenCacheNotificationArgs(
                tokenCache: null,
                clientId: "cid",
                account: null,
                hasStateChanged: false,
                isApplicationCache: false,
                suggestedCacheKey: null,
                hasTokens: false,
                suggestedCacheExpiry: null,
                cancellationToken: CancellationToken.None);

            Assert.IsFalse(args.HasStateChanged);
            args.HasStateChanged = true;
            Assert.IsTrue(args.HasStateChanged);
        }
    }
}
