// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Microsoft.Identity.Test.Unit.Pop
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

            Assert.IsNotNull(jwkObj["e"]);
            Assert.IsNotNull(jwkObj["n"]);
            Assert.AreEqual("RSA", jwkObj["kty"].ToString());
        }
    }
}
