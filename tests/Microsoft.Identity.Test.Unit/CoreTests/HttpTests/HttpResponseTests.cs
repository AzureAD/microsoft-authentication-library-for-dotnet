// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http;
using Microsoft.Identity.Client.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests.HttpTests
{
    [TestClass]
    public class HttpResponseTests
    {
        [TestMethod]
        [DataRow("WWW-Authenticate")]
        [DataRow("www-authenticate")]
        [DataRow("WWW-AUTHENTICATE")]
        public void HeadersAsDictionary_IsCaseInsensitive(string lookupKey)
        {
            // HTTP header names are case-insensitive (RFC 9110, section 5.1)
            using var message = new HttpResponseMessage();
            message.Headers.TryAddWithoutValidation("www-authenticate", "Basic realm=https://example/some.key");

            var response = new HttpResponse { Headers = message.Headers };

            Assert.IsTrue(response.HeadersAsDictionary.TryGetValue(lookupKey, out string value));
            Assert.AreEqual("Basic realm=https://example/some.key", value);
        }

        [TestMethod]
        public void HeadersAsDictionary_NullHeaders_ReturnsEmpty()
        {
            var response = new HttpResponse();

            Assert.IsEmpty(response.HeadersAsDictionary);
        }
    }
}
