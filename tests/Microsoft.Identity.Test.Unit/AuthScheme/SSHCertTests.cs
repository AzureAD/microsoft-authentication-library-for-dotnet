// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AuthScheme;
using Microsoft.Identity.Client.AuthScheme.SSHCertificates;
using Microsoft.Identity.Client.OAuth2;
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
            AssertException.Throws<ArgumentNullException>(() => new SSHCertAuthenticationOperation(null, "jwk"));
            AssertException.Throws<ArgumentNullException>(() => new SSHCertAuthenticationOperation("", "jwk"));
            AssertException.Throws<ArgumentNullException>(() => new SSHCertAuthenticationOperation("kid", ""));
            AssertException.Throws<ArgumentNullException>(() => new SSHCertAuthenticationOperation("kid", null));
        }

        [TestMethod]
        public void NoAuthPrefix()
        {
            var scheme = new SSHCertAuthenticationOperation("kid", "jwk");
            MsalClientException ex = AssertException.Throws<MsalClientException>(() => scheme.AuthorizationHeaderPrefix);
            Assert.AreEqual(MsalError.SSHCertUsedAsHttpHeader, ex.ErrorCode);
        }

        [TestMethod]
        public void ParamsAndKeyId()
        {
            var scheme = new SSHCertAuthenticationOperation("kid", "jwk");
            Assert.AreEqual("kid", scheme.KeyId);
            Assert.AreEqual(SSHCertAuthenticationOperation.SSHCertTokenType,
                scheme.GetTokenRequestParams()[OAuth2Parameter.TokenType]);
            Assert.AreEqual("jwk",
                scheme.GetTokenRequestParams()[OAuth2Parameter.RequestConfirmation]);
            Assert.AreEqual(TokenType.SshCert, (TokenType)scheme.TelemetryTokenType);

        }
    }
}
