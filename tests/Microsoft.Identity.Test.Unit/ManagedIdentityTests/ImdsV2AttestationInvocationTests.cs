// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

//using System;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.Identity.Client;
//using Microsoft.Identity.Client.ManagedIdentity;
//using Microsoft.Identity.Client.ManagedIdentity.V2;
//using Microsoft.Identity.Test.Common.Core.Helpers;
//using Microsoft.Identity.Test.Common.Core.Mocks;
//using Microsoft.Identity.Test.Unit.PublicApiTests;
//using Microsoft.VisualStudio.TestTools.UnitTesting;

//namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
//{
//    [TestClass]
//    public class ImdsV2AttestationInvocationTests : TestBase
//    {
//        private static readonly Func<AttestationTokenInput, CancellationToken, Task<AttestationTokenResponse>>
//            s_fakeProvider = (inp, ct) => Task.FromResult(new AttestationTokenResponse { AttestationToken = "header.payload.sig" });

//        [TestInitialize]
//        public void Init() => ImdsV2ManagedIdentitySource.ResetBindingCachesForTest();

//        [TestMethod]
//        public async Task Pop_Attestation_CalledOnce_OnMint_AndZero_OnCache()
//        {
//            using var env = new EnvVariableContext();
//            using var http = new MockHttpManager();
//            ManagedIdentityTestUtil.SetEnvironmentVariables(ManagedIdentitySource.ImdsV2, ManagedIdentityTestUtil.TestConstants.ImdsEndpoint);

//            var mi = await ManagedIdentityTestUtil.CreateManagedIdentityAsync(http, managedIdentityKeyType: ManagedIdentity.KeyProviders.ManagedIdentityKeyType.KeyGuard)
//                                                  .ConfigureAwait(false);

//            // CSR + issuecredential + token (mTLS PoP)
//            http.AddMockHandler(MockHelpers.MockCsrResponse());
//            http.AddMockHandler(MockHelpers.MockCertificateRequestResponse());
//            http.AddMockHandler(MockHelpers.MockImdsV2EntraTokenRequestResponse(new IdentityLoggerAdapter(new TestIdentityLogger(), Guid.Empty, "Test", "1.0.0"), mTLSPop: true));

//            int calls = 0;
//            var res1 = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
//                               .WithMtlsProofOfPossession()
//                               .WithAttestationProviderForTests((i, ct) => { calls++; return s_fakeProvider(i, ct); })
//                               .ExecuteAsync()
//                               .ConfigureAwait(false);

//            Assert.AreEqual("mtls_pop", res1.TokenType);
//            Assert.AreEqual(1, calls, "attestation called once on mint");

//            // Second call -> cache hit, so only token call; attestation NOT called
//            http.AddMockHandler(MockHelpers.MockImdsV2EntraTokenRequestResponse(new IdentityLoggerAdapter(new TestIdentityLogger(), Guid.Empty, "Test", "1.0.0"), mTLSPop: true));
//            var res2 = await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
//                               .WithMtlsProofOfPossession()
//                               .WithAttestationProviderForTests((i, ct) => { calls++; return s_fakeProvider(i, ct); })
//                               .ExecuteAsync()
//                               .ConfigureAwait(false);

//            Assert.AreEqual("mtls_pop", res2.TokenType);
//            Assert.AreEqual(1, calls, "attestation not called on cache/store hit");
//        }
//    }
//}
