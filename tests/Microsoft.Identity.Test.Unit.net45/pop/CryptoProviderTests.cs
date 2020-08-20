// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#if DESKTOP

using System.Security.Cryptography;
using System.Text;
using Microsoft.Identity.Client.Platforms.net45;
using Microsoft.Identity.Client.PlatformsCommon;
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
            var provider = new NetSharedPoPCryptoProvider();
            string jwk = provider.CannonicalPublicKeyJwk;
            dynamic jwkObj = JObject.Parse(jwk);

            Assert.IsNotNull(jwkObj.E);
            Assert.IsNotNull(jwkObj.N);
            Assert.AreEqual("RSA", jwkObj.kty.ToString());
        }
    }

}
#endif
