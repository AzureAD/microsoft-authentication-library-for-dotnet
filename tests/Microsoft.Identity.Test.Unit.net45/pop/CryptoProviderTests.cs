// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#if DESKTOP

using System.Security.Cryptography;
using System.Text;
using Microsoft.Identity.Client.Platforms.net45;
using Microsoft.Identity.Json.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit
{

    [TestClass]
    public class NetDesktopPoPCryptoProviderTests
    {

        [TestMethod]
        public void ValidateCannonicalJwk()
        {
            string jwk = NetDesktopPoPCryptoProvider.Instance.CannonicalPublicKeyJwk;
            dynamic jwkObj = JObject.Parse(jwk);

            Assert.IsNotNull(jwkObj.E);
            Assert.IsNotNull(jwkObj.N);
            Assert.AreEqual("RSA", jwkObj.kty.ToString());
        }

        [TestMethod]
        public void ValidateSignature()
        {
            byte[] payloadInClear = Encoding.UTF8.GetBytes("Hello World");
            var signature = NetDesktopPoPCryptoProvider.Instance.Sign(payloadInClear);

            Assert.IsNotNull(signature);

            // To verify the signature, use the same key container
            var reuseKeyParams = new CspParameters
            {
                Flags = CspProviderFlags.UseExistingKey,
                KeyContainerName = NetDesktopPoPCryptoProvider.ContainerName
            };

            var crypto = new RSACryptoServiceProvider(
                NetDesktopPoPCryptoProvider.RsaKeySize, 
                reuseKeyParams);

            Assert.IsTrue(crypto.VerifyData(payloadInClear, CryptoConfig.MapNameToOID("SHA256"), signature));
        }
    }

}
#endif
