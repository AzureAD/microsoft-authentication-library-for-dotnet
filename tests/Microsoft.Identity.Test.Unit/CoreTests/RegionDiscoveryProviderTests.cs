// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.Region;
using Microsoft.Identity.Client.TelemetryCore.Internal;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests
{
    [TestClass]
    [DeploymentItem("Resources\\local-imds-error-response.json")]
    [DeploymentItem("Resources\\local-imds-error-response-versions-missing.json")]
    public class RegionDiscoveryProviderTests : TestBase
    {
        private MockHttpAndServiceBundle _harness;
        private MockHttpManager _httpManager;
        private RequestContext _testRequestContext;
        private ApiEvent _apiEvent;
        private CancellationTokenSource _userCancellationTokenSource;
        private IRegionDiscoveryProvider _regionDiscoveryProvider;

        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();

            _harness = base.CreateTestHarness();
            _httpManager = _harness.HttpManager;
            _userCancellationTokenSource = new CancellationTokenSource();
            _testRequestContext = new RequestContext(
                _harness.ServiceBundle,
                Guid.NewGuid(),
                _userCancellationTokenSource.Token);
            _apiEvent = new ApiEvent(
                _harness.ServiceBundle.DefaultLogger,
                _harness.ServiceBundle.PlatformProxy.CryptographyManager,
                Guid.NewGuid().AsMatsCorrelationId());
            _testRequestContext.ApiEvent = _apiEvent;
            _regionDiscoveryProvider = new RegionDiscoveryProvider(_httpManager, true);
        }

        [TestCleanup]
        public override void TestCleanup()
        {
            Environment.SetEnvironmentVariable(TestConstants.RegionName, "");
            _harness?.Dispose();
            _regionDiscoveryProvider = new RegionDiscoveryProvider(_httpManager, true);
            base.TestCleanup();
        }

        [TestMethod]
        public async Task SuccessfulResponseFromEnvironmentVariableAsync()
        {
            Environment.SetEnvironmentVariable(TestConstants.RegionName, TestConstants.Region);

            _testRequestContext.ServiceBundle.Config.AzureRegion = null; // not configured

            InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.GetMetadataAsync(
                new Uri("https://login.microsoftonline.com/common/"), _testRequestContext)
                .ConfigureAwait(false);

            Assert.IsNull(regionalMetadata);
        }

        [TestMethod]
        public async Task SuccessfulResponseFromLocalImdsAsync()
        {
            AddMockedResponse(MockHelpers.CreateSuccessResponseMessage(TestConstants.Region));

            _testRequestContext.ServiceBundle.Config.AzureRegion =
                ConfidentialClientApplication.AttemptRegionDiscovery;

            InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.GetMetadataAsync(
                new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

            ValidateInstanceMetadata(regionalMetadata);
        }

        [TestMethod]
        public async Task FetchRegionFromLocalImdsThenGetMetadataFromCacheAsync()
        {
            AddMockedResponse(MockHelpers.CreateSuccessResponseMessage(TestConstants.Region));

            _testRequestContext.ServiceBundle.Config.AzureRegion =
               ConfidentialClientApplication.AttemptRegionDiscovery;

            InstanceDiscoveryMetadataEntry regionalMetadata =
                await _regionDiscoveryProvider.GetMetadataAsync(new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

            ValidateInstanceMetadata(regionalMetadata);

            //get metadata from the instance metadata cache
            regionalMetadata = await _regionDiscoveryProvider.GetMetadataAsync(new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

            ValidateInstanceMetadata(regionalMetadata);
        }

        [TestMethod]
        public async Task SuccessfulResponseFromUserProvidedRegionAsync()
        {
            AddMockedResponse(MockHelpers.CreateNullMessage(System.Net.HttpStatusCode.NotFound));

            _testRequestContext.ServiceBundle.Config.AzureRegion = TestConstants.Region;

            IRegionDiscoveryProvider regionDiscoveryProvider = new RegionDiscoveryProvider(_httpManager, true);
            InstanceDiscoveryMetadataEntry regionalMetadata = await regionDiscoveryProvider.GetMetadataAsync(
                new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

            Assert.IsNotNull(regionalMetadata);
            Assert.AreEqual("centralus.login.microsoft.com", regionalMetadata.PreferredNetwork);

            Assert.AreEqual(TestConstants.Region, _testRequestContext.ApiEvent.RegionUsed);
            Assert.AreEqual((int)RegionSource.UserProvided, _testRequestContext.ApiEvent.RegionSource);
            Assert.AreEqual(TestConstants.Region, _testRequestContext.ApiEvent.UserProvidedRegion);
            Assert.AreEqual(null, _testRequestContext.ApiEvent.IsValidUserProvidedRegion);
            Assert.AreEqual(false, _testRequestContext.ApiEvent.FallbackToGlobal);
        }

        [TestMethod]
        public async Task ResponseFromUserProvidedRegionSameAsRegionDetectedAsync()
        {
            Environment.SetEnvironmentVariable(TestConstants.RegionName, TestConstants.Region);
            _testRequestContext.ServiceBundle.Config.AzureRegion = TestConstants.Region;

            //            IRegionDiscoveryProvider regionDiscoveryProvider = new RegionDiscoveryProvider(_httpManager);
            InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.GetMetadataAsync(new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

            Assert.IsNotNull(regionalMetadata);
            Assert.AreEqual("centralus.login.microsoft.com", regionalMetadata.PreferredNetwork);
            Assert.AreEqual(TestConstants.Region, _testRequestContext.ApiEvent.RegionUsed);
            Assert.AreEqual((int)RegionSource.EnvVariable, _testRequestContext.ApiEvent.RegionSource);
            Assert.AreEqual(TestConstants.Region, _testRequestContext.ApiEvent.UserProvidedRegion);
            Assert.AreEqual(true, _testRequestContext.ApiEvent.IsValidUserProvidedRegion);
            Assert.AreEqual(false, _testRequestContext.ApiEvent.FallbackToGlobal);
        }

        [TestMethod]
        public async Task ResponseFromUserProvidedRegionDifferentFromRegionDetectedAsync()
        {
            Environment.SetEnvironmentVariable(TestConstants.RegionName, "detected_region");
            _testRequestContext.ServiceBundle.Config.AzureRegion = "user_region";

            //IRegionDiscoveryProvider regionDiscoveryProvider = new RegionDiscoveryProvider(_httpManager, new NetworkCacheMetadataProvider());
            InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.GetMetadataAsync(
                new Uri("https://login.microsoftonline.com/common/"), 
                _testRequestContext).ConfigureAwait(false);

            Assert.IsNotNull(regionalMetadata);
            Assert.AreEqual("user_region.login.microsoft.com", regionalMetadata.PreferredNetwork);
            Assert.AreEqual("user_region", _testRequestContext.ApiEvent.RegionUsed);
            Assert.AreEqual((int)RegionSource.EnvVariable, _testRequestContext.ApiEvent.RegionSource);
            Assert.AreEqual("user_region", _testRequestContext.ApiEvent.UserProvidedRegion);
            Assert.AreEqual(false, _testRequestContext.ApiEvent.IsValidUserProvidedRegion);
            Assert.AreEqual(false, _testRequestContext.ApiEvent.FallbackToGlobal);
        }

        [TestMethod]
        public async Task SuccessfulResponseFromRegionalizedAuthorityAsync()
        {
            var regionalizedAuthority = new Uri($"https://{TestConstants.Region}.login.microsoft.com/common/");
            _testRequestContext.ServiceBundle.Config.AzureRegion = ConfidentialClientApplication.AttemptRegionDiscovery;

            Environment.SetEnvironmentVariable(TestConstants.RegionName, TestConstants.Region);

            // In the instance discovery flow, GetMetadataAsync is always called with a known authority first, then with regionalized.
            await _regionDiscoveryProvider.GetMetadataAsync(new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);
            InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.GetMetadataAsync(regionalizedAuthority, _testRequestContext).ConfigureAwait(false);

            ValidateInstanceMetadata(regionalMetadata);
            Assert.AreEqual(TestConstants.Region, _testRequestContext.ApiEvent.RegionUsed);
            Assert.AreEqual((int)RegionSource.EnvVariable, _testRequestContext.ApiEvent.RegionSource);
            Assert.AreEqual(null, _testRequestContext.ApiEvent.UserProvidedRegion);
            Assert.AreEqual(false, _testRequestContext.ApiEvent.IsValidUserProvidedRegion);
            Assert.AreEqual(false, _testRequestContext.ApiEvent.FallbackToGlobal);
        }

        //[TestMethod]
        //public async Task NoImdsCancellation_UserCancelled_Async()
        //{
        //    var httpManager = new HttpManager(new SimpleHttpClientFactory());

        //    IRegionDiscoveryProvider regionDiscoveryProvider = new RegionDiscoveryProvider(
        //        httpManager, 
        //        new NetworkCacheMetadataProvider());

        //    _userCancellationTokenSource.Cancel();

        //    var ex = await AssertException.TaskThrowsAsync<MsalServiceException>(() => regionDiscoveryProvider.GetMetadataAsync(
        //        new Uri("https://login.microsoftonline.com/common/"),
        //        _testRequestContext))
        //        .ConfigureAwait(false);

        //    Assert.AreEqual(MsalError.RegionDiscoveryFailed, ex.ErrorCode);
        //    regionDiscoveryProvider.Clear();
        //}

        //[TestMethod]
        //public async Task ImdsTimeout_Async()
        //{
        //    var httpManager = new HttpManager(new SimpleHttpClientFactory());

        //    IRegionDiscoveryProvider regionDiscoveryProvider = new RegionDiscoveryProvider(
        //        httpManager,
        //        new NetworkCacheMetadataProvider(),
        //        imdsCallTimeout: 0);

        //    var ex = await AssertException.TaskThrowsAsync<MsalServiceException>(() => regionDiscoveryProvider.GetMetadataAsync(
        //        new Uri("https://login.microsoftonline.com/common/"),
        //        _testRequestContext))
        //        .ConfigureAwait(false);

        //    Assert.AreEqual(MsalError.RegionDiscoveryFailed, ex.ErrorCode);
        //  //  regionDiscoveryProvider.Clear();
        //}      

        [TestMethod]
        public async Task NonPublicCloudTestAsync()
        {
            // Arrange
            Environment.SetEnvironmentVariable(TestConstants.RegionName, TestConstants.Region);
            _testRequestContext.ServiceBundle.Config.AzureRegion = ConfidentialClientApplication.AttemptRegionDiscovery;

            // Act
            InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.GetMetadataAsync(new Uri("https://login.someenv.com/common/"), _testRequestContext).ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(regionalMetadata);
            Assert.AreEqual("centralus.login.someenv.com", regionalMetadata.PreferredNetwork);
        }

        [TestMethod]
        public async Task ResponseMissingRegionFromLocalImdsAsync()
        {
            // Arrange
            AddMockedResponse(MockHelpers.CreateSuccessResponseMessage(string.Empty));
            _testRequestContext.ServiceBundle.Config.AzureRegion = ConfidentialClientApplication.AttemptRegionDiscovery;
            
            // Act
            InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.GetMetadataAsync(new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

            // Assert
            Assert.IsNull(regionalMetadata, "Discovery requested, but it failed.");
            Assert.AreEqual(null, _testRequestContext.ApiEvent.RegionUsed);
            Assert.AreEqual((int)RegionSource.None, _testRequestContext.ApiEvent.RegionSource);
            Assert.AreEqual(null, _testRequestContext.ApiEvent.UserProvidedRegion);
            Assert.AreEqual(false, _testRequestContext.ApiEvent.IsValidUserProvidedRegion);
            Assert.AreEqual(true, _testRequestContext.ApiEvent.FallbackToGlobal);
        }

        [TestMethod]
        public async Task ErrorResponseFromLocalImdsAsync()
        {
            AddMockedResponse(MockHelpers.CreateNullMessage(System.Net.HttpStatusCode.NotFound));
            _testRequestContext.ServiceBundle.Config.AzureRegion = ConfidentialClientApplication.AttemptRegionDiscovery;

            InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.
                 GetMetadataAsync(new Uri("https://login.microsoftonline.com/common/"), _testRequestContext)
                 .ConfigureAwait(false);

            Assert.IsNull(regionalMetadata, "Discovery requested, but it failed.");

            Assert.AreEqual(null, _testRequestContext.ApiEvent.RegionUsed);
            Assert.AreEqual((int)RegionSource.None, _testRequestContext.ApiEvent.RegionSource);
            Assert.AreEqual(null, _testRequestContext.ApiEvent.UserProvidedRegion);
            Assert.AreEqual(false, _testRequestContext.ApiEvent.IsValidUserProvidedRegion);
            Assert.AreEqual(true, _testRequestContext.ApiEvent.FallbackToGlobal);
        }

        [TestMethod]
        public async Task UpdateImdsApiVersionWhenCurrentVersionExpiresForImdsAsync()
        {
            // Arrange
            AddMockedResponse(MockHelpers.CreateNullMessage(System.Net.HttpStatusCode.BadRequest));
            AddMockedResponse(MockHelpers.CreateFailureMessage(System.Net.HttpStatusCode.BadRequest, File.ReadAllText(
                        ResourceHelper.GetTestResourceRelativePath("local-imds-error-response.json"))), expectedParams: false);
            AddMockedResponse(MockHelpers.CreateSuccessResponseMessage(TestConstants.Region), apiVersion: "2020-10-01");
            _testRequestContext.ServiceBundle.Config.AzureRegion = ConfidentialClientApplication.AttemptRegionDiscovery;

            // Act
            InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.GetMetadataAsync(new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

            // Assert
            ValidateInstanceMetadata(regionalMetadata);
            Assert.AreEqual(TestConstants.Region, _testRequestContext.ApiEvent.RegionUsed);
            Assert.AreEqual((int)RegionSource.Imds, _testRequestContext.ApiEvent.RegionSource);
            Assert.AreEqual(null, _testRequestContext.ApiEvent.UserProvidedRegion);
            Assert.AreEqual(false, _testRequestContext.ApiEvent.IsValidUserProvidedRegion);
            Assert.AreEqual(false, _testRequestContext.ApiEvent.FallbackToGlobal);
        }

        [TestMethod]
        public async Task UpdateApiversionFailsWithEmptyResponseBodyAsync()
        {
            // Arrange
            AddMockedResponse(MockHelpers.CreateNullMessage(System.Net.HttpStatusCode.BadRequest));
            AddMockedResponse(MockHelpers.CreateNullMessage(System.Net.HttpStatusCode.BadRequest), expectedParams: false);
            _testRequestContext.ServiceBundle.Config.AzureRegion = ConfidentialClientApplication.AttemptRegionDiscovery;

            // Act
            InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.GetMetadataAsync(new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

            // Assert
            Assert.IsNull(regionalMetadata, "Discovery requested, but it failed.");
            Assert.AreEqual(null, _testRequestContext.ApiEvent.RegionUsed);
            Assert.AreEqual((int)RegionSource.None, _testRequestContext.ApiEvent.RegionSource);
            Assert.AreEqual(null, _testRequestContext.ApiEvent.UserProvidedRegion);
            Assert.AreEqual(false, _testRequestContext.ApiEvent.IsValidUserProvidedRegion);
            Assert.AreEqual(true, _testRequestContext.ApiEvent.FallbackToGlobal);
        }

        [TestMethod]
        public async Task UpdateApiversionFailsWithNoNewestVersionsAsync()
        {
            // Arrange
            AddMockedResponse(MockHelpers.CreateNullMessage(System.Net.HttpStatusCode.BadRequest));
            AddMockedResponse(MockHelpers.CreateFailureMessage(System.Net.HttpStatusCode.BadRequest, File.ReadAllText(
                        ResourceHelper.GetTestResourceRelativePath("local-imds-error-response-versions-missing.json"))), expectedParams: false);
            _testRequestContext.ServiceBundle.Config.AzureRegion = ConfidentialClientApplication.AttemptRegionDiscovery;

            // Act
            InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.GetMetadataAsync(new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

            // Assert
            Assert.IsNull(regionalMetadata, "Discovery requested, but it failed.");

            Assert.AreEqual(null, _testRequestContext.ApiEvent.RegionUsed);
            Assert.AreEqual((int)RegionSource.None, _testRequestContext.ApiEvent.RegionSource);
            Assert.AreEqual(null, _testRequestContext.ApiEvent.UserProvidedRegion);
            Assert.AreEqual(false, _testRequestContext.ApiEvent.IsValidUserProvidedRegion);
            Assert.AreEqual(true, _testRequestContext.ApiEvent.FallbackToGlobal);

        }

        private void AddMockedResponse(HttpResponseMessage responseMessage, string apiVersion = "2020-06-01", bool expectedParams = true)
        {
            var queryParams = new Dictionary<string, string>();

            if (expectedParams)
            {
                queryParams.Add("api-version", apiVersion);
                queryParams.Add("format", "text");

                _httpManager.AddMockHandler(
                   new MockHttpMessageHandler
                   {
                       ExpectedMethod = HttpMethod.Get,
                       ExpectedUrl = TestConstants.ImdsUrl,
                       ExpectedRequestHeaders = new Dictionary<string, string>
                        {
                            { "Metadata", "true" }
                        },
                       ExpectedQueryParams = queryParams,
                       ResponseMessage = responseMessage
                   });
            }
            else
            {
                _httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Get,
                        ExpectedUrl = TestConstants.ImdsUrl,
                        ExpectedRequestHeaders = new Dictionary<string, string>
                            {
                            { "Metadata", "true" }
                            },
                        ResponseMessage = responseMessage
                    });
            }
        }

        private void ValidateInstanceMetadata(InstanceDiscoveryMetadataEntry entry)
        {
            InstanceDiscoveryMetadataEntry expectedEntry = new InstanceDiscoveryMetadataEntry()
            {
                Aliases = new[] { "centralus.login.microsoft.com", "login.microsoftonline.com" },
                PreferredCache = "login.microsoftonline.com",
                PreferredNetwork = "centralus.login.microsoft.com"
            };

            CollectionAssert.AreEquivalent(expectedEntry.Aliases, entry.Aliases);
            Assert.AreEqual(expectedEntry.PreferredCache, entry.PreferredCache);
            Assert.AreEqual(expectedEntry.PreferredNetwork, entry.PreferredNetwork);
        }
    }
}
