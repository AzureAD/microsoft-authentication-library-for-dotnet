// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.PlatformsCommon;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit
{
    [TestClass]
    public class DeviceAuthHelperTests
    {
        static HttpResponse s_httpResponse;

        [TestInitialize]
        public void Initialize()
        {
            //Arrange
            if (s_httpResponse == null)
            {
                s_httpResponse = new HttpResponse();
                s_httpResponse.Headers = MockHelpers.CreatePKeyAuthChallengeResponse().Headers;
            }
        }

        [TestMethod]
        public void ParsePKeyAuthChallengeData()
        {
            //Act
            var result = DeviceAuthHelper.ParseChallengeData(s_httpResponse.Headers);

            //Assert
            Assert.AreEqual("1.0", result["Version"]);
            Assert.AreEqual("thumbprint", result["CertThumbprint"]);
            Assert.AreEqual("context", result["Context"]);
            Assert.AreEqual("nonce", result["Nonce"]);
        }

        [TestMethod]
        public void CheckIfResponseIsDeviceAuthChallenge()
        {
            //Act
            bool successResponse = DeviceAuthHelper.IsDeviceAuthChallenge(s_httpResponse.Headers);
            bool failedResponse = DeviceAuthHelper.IsDeviceAuthChallenge((new HttpResponse()).Headers);

            //Assert
            Assert.IsTrue(successResponse);
            Assert.IsFalse(failedResponse);
        }

        [TestMethod]
        public void GetDeviceAuthBypassChallengeResponse()
        {
            //Arrange
            Dictionary<string, string> pKeyAuthHeaders = new Dictionary<string, string>();
            pKeyAuthHeaders.Add("Context", "context");
            pKeyAuthHeaders.Add("Version", "1.0");

            //Act
            var result1 = DeviceAuthHelper.GetBypassChallengeResponse(s_httpResponse.Headers);
            var result2 = DeviceAuthHelper.GetBypassChallengeResponse(pKeyAuthHeaders);

            //Assert
            Assert.AreEqual(TestConstants.PKeyAuthResponse, result1);
            Assert.AreEqual(TestConstants.PKeyAuthResponse, result2);
        }

        [TestMethod]
        public void CanOSPerformDeviceAuth()
        {
            Assert.IsFalse(DeviceAuthHelper.CanOSPerformPKeyAuth());
            //Check one additional time for cache
            Assert.IsFalse(DeviceAuthHelper.CanOSPerformPKeyAuth());
        }
    }
}
