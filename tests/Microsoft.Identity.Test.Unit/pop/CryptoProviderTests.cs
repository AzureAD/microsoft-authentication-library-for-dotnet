// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
            var jwkObj = JsonNode.Parse(jwk).AsObject();

            Assert.IsNotNull(jwkObj["e"]);
            Assert.IsNotNull(jwkObj["n"]);
            Assert.AreEqual("RSA", jwkObj["kty"].ToString());
        }
    }
}
