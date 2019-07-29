// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
using System.Net.Http.Headers;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Test.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests.HttpTests
{
    [TestClass]
    public class HttpClientFactoryTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        [TestMethod]
        public void GetHttpClient_MaxRespContentBuffSizeSetTo1Mb()
        {
            Assert.AreEqual(1024 * 1024, new HttpClientFactory().GetHttpClient().MaxResponseContentBufferSize);
        }

        [TestMethod]
        public void GetHttpClient_DefaultHeadersSetToJson()
        {
            var client = new HttpClientFactory().GetHttpClient();
            Assert.IsNotNull(client.DefaultRequestHeaders.Accept);
            Assert.IsTrue(
                client.DefaultRequestHeaders.Accept.Any<MediaTypeWithQualityHeaderValue>(x => x.MediaType == "application/json"));
        }
    }
}
