// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    [TestClass]
    public class InstanceDiscoveryIntegrationTests
    {
        private static readonly string[] s_scopes = { "User.Read" };

        public TestContext TestContext { get; set; }

#if NET_CORE

        [TestMethod]
        public async Task AuthorityMigrationAsync()
        {
            LabResponse labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
            LabUser user = labResponse.User;

            IPublicClientApplication pca = PublicClientApplicationBuilder
                .Create(labResponse.App.AppId)
                .WithAuthority("https://login.windows.net/" + labResponse.Lab.TenantId + "/")
                .WithTestLogging()
                .Build();

            Trace.WriteLine("Acquire a token using a not so common authority alias");

            AuthenticationResult authResult = await pca.AcquireTokenByUsernamePassword(
               s_scopes,
                user.Upn,
                user.GetOrFetchPassword())
                // BugBug https://identitydivision.visualstudio.com/Engineering/_workitems/edit/776308/
                // sts.windows.net fails when doing instance discovery, e.g.:
                // https://sts.windows.net/common/discovery/instance?api-version=1.1&authorization_endpoint=https%3A%2F%2Fsts.windows.net%2Ff645ad92-e38d-4d1a-b510-d1b09a74a8ca%2Foauth2%2Fv2.0%2Fauthorize
                .WithTenantId(labResponse.Lab.TenantId)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(authResult.AccessToken);

            Trace.WriteLine("Acquire a token silently using the common authority alias");

            authResult = await pca.AcquireTokenSilent(s_scopes, (await pca.GetAccountsAsync().ConfigureAwait(false)).First())
                .WithTenantId("common")
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
                .WithTestLogging()
                .Build();

            Trace.WriteLine("Acquire a token using a not so common authority alias");

            MsalServiceException exception = await AssertException.TaskThrowsAsync<MsalServiceException>(() =>
                 pca.AcquireTokenByUsernamePassword(
                    s_scopes,
                     user.Upn,
                     user.GetOrFetchPassword())
                     .ExecuteAsync())
                .ConfigureAwait(false);

            Assert.IsTrue(exception.Message.Contains("AADSTS50049"));
            Assert.AreEqual("invalid_instance", exception.ErrorCode);
        }

        [TestMethod]
        public async Task AuthorityValidationTestWithFalseValidateAuthorityAsync()
        {
            LabResponse labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
            LabUser user = labResponse.User;

            IPublicClientApplication pca = PublicClientApplicationBuilder
                .Create(labResponse.App.AppId)
                .WithAuthority("https://bogus.microsoft.com/common", false)
                .WithTestLogging()
                .Build();

            Trace.WriteLine("Acquire a token using a not so common authority alias");

            _ = await AssertException.TaskThrowsAsync<HttpRequestException>(() =>
                 pca.AcquireTokenByUsernamePassword(
                    s_scopes,
                     user.Upn,
                     user.GetOrFetchPassword())
                     .ExecuteAsync())
                .ConfigureAwait(false);
        }

        /// <summary>
        /// If this test fails, please update the <see cref="KnownMetadataProvider"/> to
        /// use whatever Evo uses (i.e. the aliases, preferred network / metadata from the url below).
        /// login.windows-ppe.net discovery metadata is not part of the standard login.microsoftonline.com discovery response and
        /// need to be appended after making a seperate discovery for login.windows-ppe.net
        /// </summary>
        [TestMethod]
        public async Task KnownInstanceMetadataIsUpToDateAsync()
        {
            const string validDiscoveryUri = @"https://login.microsoftonline.com/common/discovery/instance?api-version=1.1&authorization_endpoint=https%3A%2F%2Flogin.microsoftonline.com%2Fcommon%2Foauth2%2Fv2.0%2Fauthorize";
            const string validPpeDiscoveryUri = @"https://login.windows-ppe.net/common/discovery/instance?api-version=1.1&authorization_endpoint=https%3A%2F%2Flogin.microsoftonline.com%2Fcommon%2Foauth2%2Fv2.0%2Fauthorize";
            HttpClient httpClient = new HttpClient();
            HttpResponseMessage discoveryResponse = await httpClient.SendAsync(
                new HttpRequestMessage(
                    HttpMethod.Get,
                    validDiscoveryUri)).ConfigureAwait(false);
            string discoveryJson = await discoveryResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

            HttpResponseMessage ppeDiscoveryResponse = await httpClient.SendAsync(
                new HttpRequestMessage(
                    HttpMethod.Get,
                    validPpeDiscoveryUri)).ConfigureAwait(false);
            string ppeDiscoveryJson = await ppeDiscoveryResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

            InstanceDiscoveryMetadataEntry[] actualMetadata = JsonHelper.DeserializeFromJson<InstanceDiscoveryResponse>(discoveryJson).Metadata;
            InstanceDiscoveryMetadataEntry[] actualPpeMetadata = JsonHelper.DeserializeFromJson<InstanceDiscoveryResponse>(ppeDiscoveryJson).Metadata;

            var processedMetadata = new Dictionary<string, InstanceDiscoveryMetadataEntry>();
            foreach (InstanceDiscoveryMetadataEntry entry in actualMetadata.Concat(actualPpeMetadata))
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

        private class InstanceDiscoveryMetadataEntryComparer : IEqualityComparer<InstanceDiscoveryMetadataEntry>
        {
            public bool Equals(InstanceDiscoveryMetadataEntry x, InstanceDiscoveryMetadataEntry y)
            {
                return x != null &&
                       y != null &&
                       string.Equals(x.PreferredCache, y.PreferredCache, StringComparison.OrdinalIgnoreCase) &&
                       string.Equals(x.PreferredNetwork, y.PreferredNetwork, StringComparison.OrdinalIgnoreCase) &&
                       Enumerable.SequenceEqual(x.Aliases, y.Aliases, StringComparer.OrdinalIgnoreCase);
            }

            public int GetHashCode(InstanceDiscoveryMetadataEntry obj)
            {
                throw new NotImplementedException();
            }
        }

        #endif
    }
}
