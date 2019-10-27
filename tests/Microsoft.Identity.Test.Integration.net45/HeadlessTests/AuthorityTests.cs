// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    [TestClass]
    public class AuthorityMigrationTests
    {
        private static readonly string[] s_scopes = { "User.Read" };

        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task AuthorityMigrationAsync()
        {
            LabResponse labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
            LabUser user = labResponse.User;

            IPublicClientApplication pca = PublicClientApplicationBuilder
                .Create(labResponse.App.AppId)
                .WithLogging((lvl, msg, pii) => TestContext.WriteLine($"{lvl} - {msg}"), LogLevel.Verbose, true)
                .Build();

            Trace.WriteLine("Acquire a token using a not so common authority alias");

            AuthenticationResult authResult = await pca.AcquireTokenByUsernamePassword(
               s_scopes,
                user.Upn,
                new NetworkCredential("", user.GetOrFetchPassword()).SecurePassword)
                // BugBug https://identitydivision.visualstudio.com/Engineering/_workitems/edit/776308/
                // sts.windows.net fails when doing instance discovery, e.g.: 
                // https://sts.windows.net/common/discovery/instance?api-version=1.1&authorization_endpoint=https%3A%2F%2Fsts.windows.net%2Ff645ad92-e38d-4d1a-b510-d1b09a74a8ca%2Foauth2%2Fv2.0%2Fauthorize
                .WithAuthority("https://login.windows.net/" + user.TenantId + "/")
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(authResult.AccessToken);

            Trace.WriteLine("Acquire a token silently using the common authority alias");

            authResult = await pca.AcquireTokenSilent(s_scopes, (await pca.GetAccountsAsync().ConfigureAwait(false)).First())
                .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(authResult.AccessToken);
        }

        [TestMethod]
        public async Task FailedAuthorityValidationTestAsync()
        {
            LabResponse labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
            LabUser user = labResponse.User;

            IPublicClientApplication pca = PublicClientApplicationBuilder
                .Create(labResponse.App.AppId)
                .WithAuthority("https://bogus.microsoft.com/common")
                .WithLogging((lvl, msg, pii) => TestContext.WriteLine($"{lvl} - {msg}"), LogLevel.Verbose, true)
                .Build();

            Trace.WriteLine("Acquire a token using a not so common authority alias");

            MsalServiceException exception = await AssertException.TaskThrowsAsync<MsalServiceException>(() =>
                 pca.AcquireTokenByUsernamePassword(
                    s_scopes,
                     user.Upn,
                     new NetworkCredential("", user.GetOrFetchPassword()).SecurePassword)
                     .ExecuteAsync())
                .ConfigureAwait(false);

            Assert.IsTrue(exception.Message.Contains("AADSTS50049"));
            Assert.AreEqual("invalid_instance", exception.ErrorCode);
        }

        [TestMethod]
        public void FooTest()
        {
            var commonAuthority = AuthorityInfo.FromAuthorityUri(TestConstants.AuthorityCommonTenant, true);
            var utidAuthority = AuthorityInfo.FromAuthorityUri(TestConstants.AuthorityUtidTenant, true);
            var utid2Authority = AuthorityInfo.FromAuthorityUri(TestConstants.AuthorityUtid2Tenant, true);
            var utid = TestConstants.Utid;
            var utid2 = TestConstants.Utid2;

            VerifyAuthority(
                config: commonAuthority, 
                request:null, 
                accountTid: null, 
                resultTid: "common");

            VerifyAuthority(
               config: commonAuthority,
               request: commonAuthority,
               accountTid: null,
               resultTid: "common");

            VerifyAuthority(
              config: commonAuthority,
              request: commonAuthority,
              accountTid: utid,
              resultTid: utid);

            VerifyAuthority(
             config: commonAuthority,
             request: utidAuthority,
             accountTid: null,
             resultTid: utid);

            VerifyAuthority(
             config: commonAuthority,
             request: utid2Authority,
             accountTid: utid,
             resultTid: utid2);
        }

        private static void VerifyAuthority(
            AuthorityInfo config, 
            AuthorityInfo request, 
            string accountTid, 
            string resultTid)
        {
            var resultAuthority = Authority.CreateAuthorityForRequest(config, request, accountTid);
            Assert.AreEqual(resultTid, resultAuthority.GetTenantId());
        }

        /// <summary>
        /// If this test fails, please update the <see cref="KnownMetadataProvider"/> to 
        /// use whatever Evo uses (i.e. the aliases, preferred network / metadata from the url below).
        /// </summary>
        [TestMethod]
        public async Task KnownInstanceMetadataIsUpToDateAsync()
        {
            string validDiscoveryUri = @"https://login.microsoftonline.com/common/discovery/instance?api-version=1.1&authorization_endpoint=https%3A%2F%2Flogin.microsoftonline.com%2Fcommon%2Foauth2%2Fv2.0%2Fauthorize";
            HttpClient httpClient = new HttpClient();
            HttpResponseMessage discoveryResponse = await httpClient.SendAsync(
                new HttpRequestMessage(
                    HttpMethod.Get,
                    validDiscoveryUri)).ConfigureAwait(false);
            string discoveryJson = await discoveryResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

            InstanceDiscoveryMetadataEntry[] actualMetadata = JsonHelper.DeserializeFromJson<InstanceDiscoveryResponse>(discoveryJson).Metadata;
            var processedMetadata = new Dictionary<string, InstanceDiscoveryMetadataEntry>();
            foreach (InstanceDiscoveryMetadataEntry entry in actualMetadata)
            {
                foreach (var alias in entry.Aliases)
                {
                    processedMetadata[alias] = entry;
                }
            }

            IDictionary<string, InstanceDiscoveryMetadataEntry> expectedMetadata =
                KnownMetadataProvider.GetAllEntriesForTest();

            CoreAssert.AssertDictionariesAreEqual(
                expectedMetadata,
                processedMetadata,
                new InstanceDiscoveryMetadataEntryComparer());
        }
    }

    internal class InstanceDiscoveryMetadataEntryComparer : IEqualityComparer<InstanceDiscoveryMetadataEntry>
    {
        public bool Equals(InstanceDiscoveryMetadataEntry x, InstanceDiscoveryMetadataEntry y)
        {
            return
                 string.Equals(x.PreferredCache, y.PreferredCache, System.StringComparison.OrdinalIgnoreCase) &&
                 string.Equals(x.PreferredNetwork, y.PreferredNetwork, System.StringComparison.OrdinalIgnoreCase) &&
                 Enumerable.SequenceEqual(x.Aliases, y.Aliases);
        }

        public int GetHashCode(InstanceDiscoveryMetadataEntry obj)
        {
            throw new System.NotImplementedException();
        }
    }
}
