// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Json.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.PoP
{

    [TestClass]
    public class NetDesktopPoPCryptoProviderTests
    {

        [TestMethod]
        public void ValidateCannonicalJwk()
        {
            var provider = new InMemoryCryptoProvider();
            string jwk = provider.CannonicalPublicKeyJwk;
            JObject jwkObj = JObject.Parse(jwk);

            Assert.IsNotNull(jwkObj["E"]);
            Assert.IsNotNull(jwkObj["N"]);
            Assert.AreEqual("RSA", jwkObj["kty"].ToString());
        }
    }
}
