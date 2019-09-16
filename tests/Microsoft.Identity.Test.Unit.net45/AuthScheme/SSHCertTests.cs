// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.SSHCertificates;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit
{
    [TestClass]
    public class SSHCertTests
    {
        [TestMethod]
        public void NullArgs()
        {
            AssertException.Throws<ArgumentNullException>(() => new SSHCertAuthenticationScheme(null, "jwk"));
            AssertException.Throws<ArgumentNullException>(() => new SSHCertAuthenticationScheme("", "jwk"));
            AssertException.Throws<ArgumentNullException>(() => new SSHCertAuthenticationScheme("kid", ""));
            AssertException.Throws<ArgumentNullException>(() => new SSHCertAuthenticationScheme("kid", null));
        }

        [TestMethod]
        public void NoAuthPrefix()
        {
            var scheme = new SSHCertAuthenticationScheme("kid", "jwk");
            MsalClientException ex = AssertException.Throws<MsalClientException>(() => scheme.AuthorizationHeaderPrefix);
            Assert.AreEqual(MsalError.SSHCertUsedAsHttpHeader, ex.ErrorCode);
        }

        [TestMethod]
        public void ParamsAndKeyId()
        {
            var scheme = new SSHCertAuthenticationScheme("kid", "jwk");
            Assert.AreEqual("kid", scheme.KeyId);
            Assert.AreEqual(SSHCertAuthenticationScheme.SSHCertTokenType,
                scheme.GetTokenRequestParams()[OAuth2Parameter.TokenType]);
            Assert.AreEqual("jwk",
                scheme.GetTokenRequestParams()[OAuth2Parameter.RequestConfirmation]);

        }
    }
}
