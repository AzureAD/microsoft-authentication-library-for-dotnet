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
            _testRequestContext = new RequestContext(_harness.ServiceBundle, Guid.NewGuid(), _userCancellationTokenSource.Token);
            _apiEvent = new ApiEvent(
                _harness.ServiceBundle.DefaultLogger,
                _harness.ServiceBundle.PlatformProxy.CryptographyManager,
                Guid.NewGuid().AsMatsCorrelationId());
            _testRequestContext.ApiEvent = _apiEvent;
            _regionDiscoveryProvider = new RegionDiscoveryProvider(_httpManager);
        }

        [TestCleanup]
        public override void TestCleanup()
        {
            Environment.SetEnvironmentVariable(TestConstants.RegionName, "");
            _testRequestContext.ServiceBundle.Config.AuthorityInfo.RegionToUse = "";
            _harness?.Dispose();
            base.TestCleanup();
        }

        [TestMethod]
        public async Task SuccessfulResponseFromEnvironmentVariableAsync()
        {
            Environment.SetEnvironmentVariable(TestConstants.RegionName, TestConstants.Region);

            InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.TryGetMetadataAsync(new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

            validateInstanceMetadata(regionalMetadata);
        }

        [TestMethod]
        public async Task SuccessfulResponseFromLocalImdsAsync()
        {
            AddMockedResponse(MockHelpers.CreateSuccessResponseMessage(TestConstants.Region));

            InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.TryGetMetadataAsync(new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

            validateInstanceMetadata(regionalMetadata);
        }

        [TestMethod]
        public async Task FetchRegionFromLocalImdsThenGetMetadataFromCacheAsync()
        {
            AddMockedResponse(MockHelpers.CreateSuccessResponseMessage(TestConstants.Region));

            InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.TryGetMetadataAsync(new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

            validateInstanceMetadata(regionalMetadata);

            //get metadata from the instance metadata cache
            regionalMetadata = await _regionDiscoveryProvider.TryGetMetadataAsync(new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

            validateInstanceMetadata(regionalMetadata);
        }

        [TestMethod]
        public async Task SuccessfulResponseFromUserProvidedRegionAsync()
        {
            AddMockedResponse(MockHelpers.CreateNullMessage(System.Net.HttpStatusCode.NotFound));
            _testRequestContext.ServiceBundle.Config.AuthorityInfo.RegionToUse = TestConstants.Region;

            IRegionDiscoveryProvider regionDiscoveryProvider = new RegionDiscoveryProvider(_httpManager, new NetworkCacheMetadataProvider());
            InstanceDiscoveryMetadataEntry regionalMetadata = await regionDiscoveryProvider.TryGetMetadataAsync(new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

            Assert.IsNotNull(regionalMetadata);
            Assert.AreEqual("centralus.login.microsoft.com", regionalMetadata.PreferredNetwork);
            regionDiscoveryProvider.Clear();
        }

        [TestMethod]
        public async Task ResponseFromUserProvidedRegionSameAsRegionDetectedAsync()
        {
            Environment.SetEnvironmentVariable(TestConstants.RegionName, TestConstants.Region);
            _testRequestContext.ServiceBundle.Config.AuthorityInfo.RegionToUse = TestConstants.Region;

            IRegionDiscoveryProvider regionDiscoveryProvider = new RegionDiscoveryProvider(_httpManager, new NetworkCacheMetadataProvider());
            InstanceDiscoveryMetadataEntry regionalMetadata = await regionDiscoveryProvider.TryGetMetadataAsync(new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

            Assert.IsNotNull(regionalMetadata);
            Assert.AreEqual("centralus.login.microsoft.com", regionalMetadata.PreferredNetwork);
            regionDiscoveryProvider.Clear();
        }

        [TestMethod]
        public async Task ResponseFromUserProvidedRegionDifferentFromRegionDetectedAsync()
        {
            Environment.SetEnvironmentVariable(TestConstants.RegionName, "eastus");
            _testRequestContext.ServiceBundle.Config.AuthorityInfo.RegionToUse = TestConstants.Region;

            IRegionDiscoveryProvider regionDiscoveryProvider = new RegionDiscoveryProvider(_httpManager, new NetworkCacheMetadataProvider());
            InstanceDiscoveryMetadataEntry regionalMetadata = await regionDiscoveryProvider.TryGetMetadataAsync(new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

            Assert.IsNotNull(regionalMetadata);
            Assert.AreEqual("eastus.login.microsoft.com", regionalMetadata.PreferredNetwork);
            regionDiscoveryProvider.Clear();
        }

        private class HttpSnifferClientFactory : IMsalHttpClientFactory
        {
            readonly HttpClient _httpClient;

            public IList<(HttpRequestMessage, HttpResponseMessage)> RequestsAndResponses { get; }

            public HttpSnifferClientFactory()
            {
                RequestsAndResponses = new List<(HttpRequestMessage, HttpResponseMessage)>();

                var recordingHandler = new RecordingHandler2((req, res) =>
                {
                    RequestsAndResponses.Add((req, res));
                });
                recordingHandler.InnerHandler = new HttpClientHandler();
                _httpClient = new HttpClient(recordingHandler);
            }

            public HttpClient GetHttpClient()
            {
                return _httpClient;
            }

            private class RecordingHandler2 : DelegatingHandler
            {
                private readonly Action<HttpRequestMessage, HttpResponseMessage> _recordingAction;

                public RecordingHandler2(Action<HttpRequestMessage, HttpResponseMessage> recordingAction)
                {
                    _recordingAction = recordingAction;
                }

                protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                {
                    var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
                    _recordingAction.Invoke(request, response);
                    return response;
                }
            }
        }

        [TestMethod]
        public async Task NoImdsCancellation_UserCancelled_Async()
        {
            var httpManager = new HttpManager(new SimpleHttpClientFactory());

            IRegionDiscoveryProvider regionDiscoveryProvider = new RegionDiscoveryProvider(
                httpManager, 
                new NetworkCacheMetadataProvider());

            _userCancellationTokenSource.Cancel();

            var ex = await AssertException.TaskThrowsAsync<MsalServiceException>(() => regionDiscoveryProvider.TryGetMetadataAsync(
                new Uri("https://login.microsoftonline.com/common/"),
                _testRequestContext))
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.RegionDiscoveryFailed, ex.ErrorCode);
            regionDiscoveryProvider.Clear();
        }

        [TestMethod]
        public async Task ImdsTimeout_Async()
        {
            var httpManager = new HttpManager(new SimpleHttpClientFactory());

            IRegionDiscoveryProvider regionDiscoveryProvider = new RegionDiscoveryProvider(
                httpManager,
                new NetworkCacheMetadataProvider(),
                imdsCallTimeout: 0);

            var ex = await AssertException.TaskThrowsAsync<MsalServiceException>(() => regionDiscoveryProvider.TryGetMetadataAsync(
                new Uri("https://login.microsoftonline.com/common/"),
                _testRequestContext))
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.RegionDiscoveryFailed, ex.ErrorCode);
            regionDiscoveryProvider.Clear();
        }      

        [TestMethod]
        public async Task NonPublicCloudTestAsync()
        {
            Environment.SetEnvironmentVariable(TestConstants.RegionName, TestConstants.Region);

            InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.TryGetMetadataAsync(new Uri("https://login.someenv.com/common/"), _testRequestContext).ConfigureAwait(false);

            Assert.IsNotNull(regionalMetadata);
            Assert.AreEqual("centralus.login.someenv.com", regionalMetadata.PreferredNetwork);
        }

        [TestMethod]
        public async Task ResponseMissingRegionFromLocalImdsAsync()
        {
            AddMockedResponse(MockHelpers.CreateSuccessResponseMessage(string.Empty));

            try
            {
                InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.TryGetMetadataAsync(new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

                Assert.Fail("Exception should be thrown.");
            }
            catch (MsalServiceException e)
            {
                Assert.IsNotNull(e);
                Assert.AreEqual(MsalError.RegionDiscoveryFailed, e.ErrorCode);
                Assert.AreEqual(MsalErrorMessage.RegionDiscoveryFailed, e.Message);
            }
        }

        [TestMethod]
        public async Task ErrorResponseFromLocalImdsAsync()
        {
            AddMockedResponse(MockHelpers.CreateNullMessage(System.Net.HttpStatusCode.NotFound));

            try
            {
                InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.TryGetMetadataAsync(new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

                Assert.Fail("Exception should be thrown.");
            }
            catch (MsalServiceException e)
            {
                Assert.IsNotNull(e);
                Assert.AreEqual(MsalError.RegionDiscoveryFailed, e.ErrorCode);
                Assert.AreEqual(MsalErrorMessage.RegionDiscoveryFailed, e.Message);
            }
        }

        [TestMethod]
        public async Task UpdateApiversionWhenCurrentVersionExpiresForImdsAsync()
        {
            AddMockedResponse(MockHelpers.CreateNullMessage(System.Net.HttpStatusCode.BadRequest));
            AddMockedResponse(MockHelpers.CreateFailureMessage(System.Net.HttpStatusCode.BadRequest, File.ReadAllText(
                        ResourceHelper.GetTestResourceRelativePath("local-imds-error-response.json"))), expectedParams: false);
            AddMockedResponse(MockHelpers.CreateSuccessResponseMessage(TestConstants.Region), apiVersion: "2020-10-01");

            InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.TryGetMetadataAsync(new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

            validateInstanceMetadata(regionalMetadata);
        }

        [TestMethod]
        public async Task UpdateApiversionFailsWithEmptyResponseBodyAsync()
        {
            AddMockedResponse(MockHelpers.CreateNullMessage(System.Net.HttpStatusCode.BadRequest));
            AddMockedResponse(MockHelpers.CreateNullMessage(System.Net.HttpStatusCode.BadRequest), expectedParams: false);

            try
            {
                InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.TryGetMetadataAsync(new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

                Assert.Fail("The call should fail with MsalServiceException as the updated version for imds was not returned.");
            }
            catch (MsalServiceException e)
            {
                Assert.IsNotNull(e);
                Assert.AreEqual(MsalError.RegionDiscoveryFailed, e.ErrorCode);
                Assert.AreEqual(MsalErrorMessage.RegionDiscoveryFailed, e.Message);
            }
        }

        [TestMethod]
        public async Task UpdateApiversionFailsWithNoNewestVersionsAsync()
        {
            AddMockedResponse(MockHelpers.CreateNullMessage(System.Net.HttpStatusCode.BadRequest));
            AddMockedResponse(MockHelpers.CreateFailureMessage(System.Net.HttpStatusCode.BadRequest, File.ReadAllText(
                        ResourceHelper.GetTestResourceRelativePath("local-imds-error-response-versions-missing.json"))), expectedParams: false);

            try
            {
                InstanceDiscoveryMetadataEntry regionalMetadata = await _regionDiscoveryProvider.TryGetMetadataAsync(new Uri("https://login.microsoftonline.com/common/"), _testRequestContext).ConfigureAwait(false);

                Assert.Fail("The call should fail with MsalServiceException as the newest versions were missing in the response.");
            }
            catch (MsalServiceException e)
            {
                Assert.IsNotNull(e);
                Assert.AreEqual(MsalError.RegionDiscoveryFailed, e.ErrorCode);
                Assert.AreEqual(MsalErrorMessage.RegionDiscoveryFailed, e.Message);
            }
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

        private void validateInstanceMetadata(InstanceDiscoveryMetadataEntry entry)
        {
            InstanceDiscoveryMetadataEntry expectedEntry = new InstanceDiscoveryMetadataEntry()
            {
                Aliases = new[] { "centralus.login.microsoft.com" },
                PreferredCache = "login.microsoftonline.com",
                PreferredNetwork = "centralus.login.microsoft.com"
            };

            Assert.IsNotNull(entry, "The instance metadata should not be empty.");
            Assert.AreEqual(expectedEntry.Aliases.Single(), entry.Aliases.Single());
            Assert.AreEqual(expectedEntry.PreferredCache, entry.PreferredCache);
            Assert.AreEqual(expectedEntry.PreferredNetwork, entry.PreferredNetwork);
        }
    }
}
