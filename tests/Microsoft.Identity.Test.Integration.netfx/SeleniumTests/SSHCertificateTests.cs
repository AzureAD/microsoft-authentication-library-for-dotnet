// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.SSHCertificates;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.SeleniumTests
{
    public partial class InteractiveFlowTests
    {
        //This client id is for Azure CLI which is one of the only 2 clients that have PreAuth to use ssh cert feature
        string _SSH_ClientId = "04b07795-8ddb-461a-bbee-02f9e1bf7b46";
        //SSH User impersonation scope required for this test
        private string[] _SSH_scopes = new[] { "https://pas.windows.net/CheckMyAccess/Linux/user_impersonation" };

        [TestMethod]
        public async Task Interactive_SSHCert_Async()
        {
            LabResponse labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
            await CreateSSHCertTestAsync(labResponse).ConfigureAwait(false);
        }

        private async Task CreateSSHCertTestAsync(LabResponse labResponse)
        {
            IPublicClientApplication pca = PublicClientApplicationBuilder
            .Create(_SSH_ClientId)
            .WithRedirectUri(SeleniumWebUI.FindFreeLocalhostRedirectUri())
            .WithTestLogging()
            .Build();

            TokenCacheAccessRecorder userCacheAccess = pca.UserTokenCache.RecordAccess();

            Trace.WriteLine("Part 1 - Acquire an SSH cert interactively ");
            string jwk = CreateJwk();

            AuthenticationResult result = await pca
                .AcquireTokenInteractive(_SSH_scopes)
                .WithCustomWebUi(CreateSeleniumCustomWebUI(labResponse.User, Prompt.ForceLogin))
                .WithSSHCertificateAuthenticationScheme(jwk, "key1")
                .ExecuteAsync(new CancellationTokenSource(_interactiveAuthTimeout).Token)
                .ConfigureAwait(false);

            userCacheAccess.AssertAccessCounts(0, 1);
            Assert.AreEqual("ssh-cert", result.TokenType);
            IAccount account = await MsalAssert.AssertSingleAccountAsync(labResponse, pca, result).ConfigureAwait(false);
            userCacheAccess.AssertAccessCounts(1, 1); // the assert calls GetAccounts

            Trace.WriteLine("Part 2 - Acquire a token silent with the same keyID - should be served from the cache");
            result = await pca
                .AcquireTokenSilent(_SSH_scopes, account)
                .WithSSHCertificateAuthenticationScheme(jwk, "key1")
                .ExecuteAsync(new CancellationTokenSource(_interactiveAuthTimeout).Token)
                .ConfigureAwait(false);
            userCacheAccess.AssertAccessCounts(2, 1);

            account = await MsalAssert.AssertSingleAccountAsync(labResponse, pca, result).ConfigureAwait(false);
            userCacheAccess.AssertAccessCounts(3, 1);

            Trace.WriteLine("Part 3 - Acquire a token silent with a different keyID - should not sbe served from the cache");
            result = await pca
                .AcquireTokenSilent(_SSH_scopes, account)
                .WithSSHCertificateAuthenticationScheme(jwk, "key2")
                .ExecuteAsync(new CancellationTokenSource(_interactiveAuthTimeout).Token)
                .ConfigureAwait(false);

            Assert.AreEqual("ssh-cert", result.TokenType);
            userCacheAccess.AssertAccessCounts(4, 2);
            await MsalAssert.AssertSingleAccountAsync(labResponse, pca, result).ConfigureAwait(false);
        }

        private string CreateJwk()
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048);
            RSAParameters rsaKeyInfo = rsa.ExportParameters(false);

            string modulus = Base64UrlHelpers.Encode(rsaKeyInfo.Modulus);
            string exp = Base64UrlHelpers.Encode(rsaKeyInfo.Exponent);
            string jwk = $"{{\"kty\":\"RSA\", \"n\":\"{modulus}\", \"e\":\"{exp}\"}}";

            return jwk;
        }

        private Dictionary<string, string> GetTestSliceParams()
        {
            return new Dictionary<string, string>()
            {
                { "dc", "prod-wst-test1" },
                { "slice", "test" },
                { "sshcrt", "true" }
            };
        }
    }
}
