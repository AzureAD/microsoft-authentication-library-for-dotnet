// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Unit.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class ImdsV2Tests : TestBase
    {
        private const string serverHeaderImdsVersion = "IMDS/150.870.65.1325";
        private readonly TestRetryPolicyFactory _testRetryPolicyFactory = new TestRetryPolicyFactory();

        [TestMethod]
        public async Task GetCsrMetadataAsyncSucceeds()
        {
            using (var httpManager = new MockHttpManager())
            {
                //httpManager.AddManagedIdentityMockHandler(
                //  ManagedIdentityTests.CsrMetadataEndpoint,
                //  ManagedIdentityTests.Resource,
                //  MockHelpers.GetCsrMetadataSuccessfulResponse(),
                //  ManagedIdentitySource.ImdsV2,
                //  serverHeaderImdsVersion: serverHeaderImdsVersion);
                //httpManager.AddSuccessTokenResponseMockHandlerForGet(null, null);
                var handler = httpManager.AddMockHandler(MockHelpers.CreateCsrResponse());
                    

                var managedIdentityApp = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)                    
                    .WithRetryPolicyFactory(_testRetryPolicyFactory)
                    .Build();

                
                var miSource = await (managedIdentityApp as ManagedIdentityApplication).GetManagedIdentitySourceAsync()
                    .ConfigureAwait(false);
                Assert.AreEqual(ManagedIdentitySource.ImdsV2, miSource);
                //RequestContext requestContext = new RequestContext(managedIdentityApp.ServiceBundle, Guid.NewGuid(), null);

                //bool isValid = await ImdsV2ManagedIdentitySource.GetCsrMetadataAsync(requestContext).ConfigureAwait(false);
                //Assert.IsTrue(isValid);
            }
        }

        //[TestMethod]
        //public async Task GetCsrMetadataAsyncSucceedsAfterRetry()
        //{
        //    using (var httpManager = new MockHttpManager())
        //    {
        //        var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
        //            .WithHttpManager(httpManager)
        //            .WithRetryPolicyFactory(_testRetryPolicyFactory);
        //        var managedIdentityApp = miBuilder.BuildConcrete();
        //        var miSource = await managedIdentityApp.GetManagedIdentitySourceAsync().ConfigureAwait(false);
        //        Assert.AreEqual(ManagedIdentitySource.ImdsV2, miSource);
        //        RequestContext requestContext = new RequestContext(managedIdentityApp.ServiceBundle, Guid.NewGuid(), null);

        //        // First attempt fails with NOT_FOUND (404)
        //        httpManager.AddManagedIdentityMockHandler(
        //            ManagedIdentityTests.CsrMetadataEndpoint,
        //            ManagedIdentityTests.Resource,
        //            MockHelpers.GetMsiImdsErrorResponse(),
        //            ManagedIdentitySource.ImdsV2,
        //            statusCode: HttpStatusCode.NotFound);

        //        // Second attempt succeeds
        //        httpManager.AddManagedIdentityMockHandler(
        //            ManagedIdentityTests.CsrMetadataEndpoint,
        //            ManagedIdentityTests.Resource,
        //            MockHelpers.GetCsrMetadataSuccessfulResponse(),
        //            ManagedIdentitySource.ImdsV2,
        //            serverHeaderImdsVersion: serverHeaderImdsVersion);

        //        var csr = await ImdsV2ManagedIdentitySource.GetCsrMetadataAsync(requestContext, true).ConfigureAwait(false);
        //        Assert.IsNotNull(csr);
        //    }
        //}

        //[TestMethod]
        //public async Task GetCsrMetadataAsyncFailsWithInvalidVersion()
        //{
        //    using (var httpManager = new MockHttpManager())
        //    {
        //        var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
        //            .WithHttpManager(httpManager)
        //            .WithRetryPolicyFactory(_testRetryPolicyFactory);
        //        var managedIdentityApp = miBuilder.BuildConcrete();
        //        var miSource = await managedIdentityApp.GetManagedIdentitySourceAsync().ConfigureAwait(false);
        //        Assert.AreEqual(ManagedIdentitySource.ImdsV2, miSource);
        //        RequestContext requestContext = new RequestContext(managedIdentityApp.ServiceBundle, Guid.NewGuid(), null);

        //        httpManager.AddManagedIdentityMockHandler(
        //            ManagedIdentityTests.CsrMetadataEndpoint,
        //            ManagedIdentityTests.Resource,
        //            MockHelpers.GetCsrMetadataSuccessfulResponse(),
        //            ManagedIdentitySource.ImdsV2,
        //            serverHeaderImdsVersion: "IMDS/150.870.65.1324");

        //        var csr = await ImdsV2ManagedIdentitySource.GetCsrMetadataAsync(requestContext, true).ConfigureAwait(false);
        //        Assert.IsNotNull(csr);
        //    }
        //}

        //[TestMethod]
        //public async Task GetCsrMetadataAsyncFailsWithMissingServerHeader()
        //{
        //    using (var httpManager = new MockHttpManager())
        //    {
        //        var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
        //            .WithHttpManager(httpManager)
        //            .WithRetryPolicyFactory(_testRetryPolicyFactory);
        //        var managedIdentityApp = miBuilder.BuildConcrete();
        //        var miSource = await managedIdentityApp.GetManagedIdentitySourceAsync().ConfigureAwait(false);
        //        Assert.AreEqual(ManagedIdentitySource.ImdsV2, miSource);
        //        RequestContext requestContext = new RequestContext(managedIdentityApp.ServiceBundle, Guid.NewGuid(), null);

        //        httpManager.AddManagedIdentityMockHandler(
        //            ManagedIdentityTests.CsrMetadataEndpoint,
        //            ManagedIdentityTests.Resource,
        //            MockHelpers.GetCsrMetadataSuccessfulResponse(),
        //            ManagedIdentitySource.ImdsV2,
        //            serverHeaderImdsVersion: null); // mock missing server header

        //        var csr = await ImdsV2ManagedIdentitySource.GetCsrMetadataAsync(requestContext, true).ConfigureAwait(false);
        //        Assert.IsNotNull(csr);
        //    }
        //}

        [TestMethod]
        public async Task GetCsrMetadataAsyncFailsAfterMaxRetries()
        {
            using (var httpManager = new MockHttpManager())
            {
                var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(_testRetryPolicyFactory);
                var managedIdentityApp = miBuilder.BuildConcrete();
                var miSource = await managedIdentityApp.GetManagedIdentitySourceAsync().ConfigureAwait(false);
                Assert.AreEqual(ManagedIdentitySource.ImdsV2, miSource);
                RequestContext requestContext = new RequestContext(managedIdentityApp.ServiceBundle, Guid.NewGuid(), null);

                const int Num404Errors = 1 + TestDefaultRetryPolicy.DefaultManagedIdentityMaxRetries;
                for (int i = 0; i < Num404Errors; i++)
                {
                    httpManager.AddManagedIdentityMockHandler(
                        ManagedIdentityTests.CsrMetadataEndpoint,
                        ManagedIdentityTests.Resource,
                        MockHelpers.GetMsiImdsErrorResponse(),
                        ManagedIdentitySource.ImdsV2,
                        statusCode: HttpStatusCode.NotFound);
                }

                var csr = await ImdsV2ManagedIdentitySource.GetCsrMetadataAsync(requestContext, true).ConfigureAwait(false);
                Assert.IsNotNull(csr);

                int requestsMade = Num404Errors - httpManager.QueueSize;
                Assert.AreEqual(Num404Errors, requestsMade);
            }
        }
    }
}
