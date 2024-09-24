// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.NetCore.HeadlessTests
{
    [TestClass]
    public class CdtTests
    {
        private static readonly string[] s_scopes = new[] { "88f91eac-c606-4c67-a0e2-a5e8a186854f/.default" };

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        [TestMethod]
        [Ignore("Need to wait for ESTS to release feature from test slice.")]
        public async Task CDT_WithCertIntegrationTest_Async()
        {
            //Client.Constraint constraint = new Client.Constraint();
            //constraint.Type = "wk:user";
            //constraint.Action = "U";
            //constraint.Version = "1.0";
            //constraint.Targets = new List<ConstraintTarget>();

            //constraint.Targets.Add(new ConstraintTarget("constraint1", "pol1"));
            //constraint.Targets.Add(new ConstraintTarget("constraint2", "pol2"));

            //var constraintAsString = JsonHelper.SerializeToJson(new[] { constraint });

            //TODO: Resolve serialization failure in test. Seems to be related to some internal .net serialization issue
            //Using a hardcoded string for now
            var constraintAsString = "[{\"Version\":\"1.0\",\"Type\":\"wk:user\",\"Action\":\"U\",\"Targets\":[{\"Value\":\"constraint1\",\"Policy\":\"pol1\",\"AdditionalProperties\":null},{\"Value\":\"constraint2\",\"Policy\":\"pol2\",\"AdditionalProperties\":null}],\"AdditionalProperties\":null}]";

            var secret = GetSecretLazy(KeyVaultInstance.MSIDLab, TestConstants.MsalCCAKeyVaultSecretName).Value;
            var certificate = CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName);

            var confidentialApp = ConfidentialClientApplicationBuilder
                                    .Create("88f91eac-c606-4c67-a0e2-a5e8a186854f")
                                    .WithAuthority("https://login.microsoftonline.com/msidlab4.onmicrosoft.com")
                                    .WithClientSecret(secret)
                                    .WithExperimentalFeatures(true)
            .BuildConcrete();

            var provider = new CdtCryptoProvider(certificate);

            MsalAddIn cdtAddin = new MsalAddIn()
            {
                AuthenticationScheme = new CdtAuthenticationScheme(constraintAsString, certificate),
                AdditionalCacheParameters = new[] { CdtAuthenticationScheme.CdtNonce, CdtAuthenticationScheme.CdtEncKey }
            };

            var result = await confidentialApp.AcquireTokenForClient(s_scopes)
                                                .WithAddIn(cdtAddin)
                                                .WithExtraQueryParameters("dc=ESTS-PUB-JPELR1-AZ1-FD000-TEST1")
                                                .ExecuteAsync()
                                                .ConfigureAwait(false);

            // access token parsing can be done with MSAL's id token parsing logic
            var claims = IdToken.Parse(result.AccessToken).ClaimsPrincipal;

            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            AssertConstrainedDelegationClaims(provider, claims, constraintAsString);

            //Verify that the original AT token is cached and the CDT can be recreated
            result = await confidentialApp.AcquireTokenForClient(s_scopes)
                .WithAddIn(cdtAddin)
                .ExecuteAsync()
                .ConfigureAwait(false);

            // access token parsing can be done with MSAL's id token parsing logic
            claims = IdToken.Parse(result.AccessToken).ClaimsPrincipal;

            Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
            AssertConstrainedDelegationClaims(provider, claims, constraintAsString);
        }

        private static Lazy<string> GetSecretLazy(string keyVaultInstance, string secretName) => new Lazy<string>(() =>
        {
            var keyVault = new KeyVaultSecretsProvider(keyVaultInstance);
            var secret = keyVault.GetSecretByName(secretName).Value;
            return secret;
        });

        private static void AssertConstrainedDelegationClaims(CdtCryptoProvider cdtCryptoProvider, System.Security.Claims.ClaimsPrincipal claims, string constraint)
        {
            var ticket = claims.FindAll("t").Single().Value;
            var constraints = claims.FindAll("c").Single().Value;

            Assert.IsTrue(!string.IsNullOrEmpty(ticket));
            Assert.IsTrue(!string.IsNullOrEmpty(constraints));

            Assert.IsNotNull(ticket);

            var constraintsClaims = IdToken.Parse(constraints).ClaimsPrincipal;
            var constraintsClaim = constraintsClaims.FindAll("constraints").Single().Value;
            Assert.AreEqual(constraint, constraintsClaim);
        }
    }
}
