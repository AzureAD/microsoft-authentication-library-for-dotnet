// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Test.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Test.Unit.RequestsTests
{
#if DESKTOP || NETSTANDARD1_3 || NET_CORE

    [TestClass]
    public class RequestValidationHelperTests
    {
        public const uint JwtToAadLifetimeInSeconds = 60 * 10; // Ten minutes

        private IServiceBundle _serviceBundle;
        readonly string _audience1 = "Audience1";
        readonly string _audience2 = "Audience2";

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
            _serviceBundle = TestCommon.CreateDefaultServiceBundle();
        }

        [TestMethod]
        [Description("Test for client assertion with mismatched parameters in Request Validator.")]
        public void ClientAssertionRequestValidatorMismatchParameterTest()
        {
            var credential = ClientCredentialWrapper.CreateWithSecret(TestConstants.ClientSecret);
            credential.Audience = _audience1;
            credential.ContainsX5C = false;
            credential.CachedAssertion = TestConstants.DefaultClientAssertion;
            credential.ValidTo = ConvertToTimeT(DateTime.UtcNow + TimeSpan.FromSeconds(JwtToAadLifetimeInSeconds));

            // Validate cached client assertion with parameters
            Assert.IsTrue(ClientCredentialHelper.ValidateClientAssertion(credential, _audience1, false));

            // Different audience
            credential.Audience = _audience2;

            // cached assertion should be invalid
            Assert.IsFalse(ClientCredentialHelper.ValidateClientAssertion(credential, _audience1, false));

            // Different x5c, same audience
            credential.Audience = _audience1;
            credential.ContainsX5C = true;

            // cached assertion should be invalid
            Assert.IsFalse(ClientCredentialHelper.ValidateClientAssertion(credential, _audience1, false));

            // Different audience and x5c
            credential.Audience = _audience2;

            // cached assertion should be invalid
            Assert.IsFalse(ClientCredentialHelper.ValidateClientAssertion(credential, _audience1, false));

            // No cached Assertion
            credential.CachedAssertion = "";

            // should return false
            Assert.IsFalse(ClientCredentialHelper.ValidateClientAssertion(credential, _audience1, false));
        }

        [TestMethod]
        [Description("Test for expired client assertion in Request Validator.")]
        public void ClientAssertionRequestValidatorExpirationTimeTest()
        {
            var credential = ClientCredentialWrapper.CreateWithSecret(TestConstants.ClientSecret);
            credential.Audience = _audience1;
            credential.ContainsX5C = false;
            credential.CachedAssertion = TestConstants.DefaultClientAssertion;
            credential.ValidTo = ConvertToTimeT(DateTime.UtcNow + TimeSpan.FromSeconds(JwtToAadLifetimeInSeconds));

            // Validate cached client assertion with expiration time
            // Cached assertion should be valid
            Assert.IsTrue(ClientCredentialHelper.ValidateClientAssertion(credential, _audience1, false));

            // Setting expiration time to now
            credential.ValidTo = ConvertToTimeT(DateTime.UtcNow);

            // cached assertion should have expired
            Assert.IsFalse(ClientCredentialHelper.ValidateClientAssertion(credential, _audience1, false));
        }

        internal static long ConvertToTimeT(DateTime time)
        {
            var startTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            TimeSpan diff = time - startTime;
            return (long)diff.TotalSeconds;
        }
    }
#endif
}
