// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    [TestClass]
    public class GenericAuthorityTests 
    {
        private const string DemoDuendeSoftwareDotCom = "https://demo.duendesoftware.com";

        /// Based on the publicly available https://demo.duendesoftware.com/
        [RunOn(TargetFrameworks.NetCore)]
        public async Task ShouldSupportClientCredentialsWithDuendeDemoInstanceAsync()
        {
            var app = ConfidentialClientApplicationBuilder.Create("m2m")
                .WithExperimentalFeatures(true)
                .WithGenericAuthority(DemoDuendeSoftwareDotCom)                
                .WithClientSecret("secret")
                .Build();

            AuthenticationResult response;
            response = await app.AcquireTokenForClient(new[] { "api" }).ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual("api", response.Scopes.AsSingleString());
            Assert.AreEqual("Bearer", response.TokenType);
            Assert.AreEqual(TokenSource.IdentityProvider, response.AuthenticationResultMetadata.TokenSource);

            var response2 = await app.AcquireTokenForClient(new[] { "api" }).ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual("api", response.Scopes.AsSingleString());
            Assert.AreEqual("Bearer", response.TokenType);
            Assert.AreEqual(TokenSource.Cache, response2.AuthenticationResultMetadata.TokenSource);
        }

        /// Based on the publicly available https://demo.duendesoftware.com/
        [RunOn(TargetFrameworks.NetCore)]
        public async Task BadSecret_Duende_Async()
        {
            var app = ConfidentialClientApplicationBuilder.Create("m2m")
                .WithExperimentalFeatures(true)
                .WithGenericAuthority(DemoDuendeSoftwareDotCom)
                .WithClientSecret("bad_secret")
                .Build();

            var response = await app.AcquireTokenForClient(new[] { "api" }).ExecuteAsync().ConfigureAwait(false);

            var ex = await AssertException.TaskThrowsAsync<MsalServiceException>(() =>
                app.AcquireTokenForClient(new[] { "api" }).ExecuteAsync()).ConfigureAwait(false);

            Assert.AreEqual(ex.ErrorCode, "invalid_client");
            Assert.AreEqual(ex.StatusCode, HttpStatusCode.BadRequest);
        }

        /// Based on the publicly available https://demo.duendesoftware.com/
        [RunOn(TargetFrameworks.NetCore | TargetFrameworks.NetFx)]
        public async Task ShouldSupportClientCredentialsPrivateKeyJwtWithDuendeDemoInstanceAsync()
        {
            var applicationConfiguration = new ApplicationConfiguration(true);
            ConfidentialClientApplicationBuilder builder = new(applicationConfiguration);
            var app = builder
                .WithExperimentalFeatures(true)
                .WithGenericAuthority(DemoDuendeSoftwareDotCom)
                .WithClientId("m2m.jwt")
                .WithClientAssertion(options => GetPrivateKeyJwtClientAssertionAsync(options.ClientID, options.TokenEndpoint, options.CancellationToken))
                .Build();
            var response = await app.AcquireTokenForClient(new[] { "api" }).ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual("api", response.Scopes.AsSingleString());
            Assert.AreEqual("Bearer", response.TokenType);

            var response2 = await app.AcquireTokenForClient(new[] { "api" }).ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual("api", response.Scopes.AsSingleString());
            Assert.AreEqual("Bearer", response.TokenType);
            Assert.AreEqual(TokenSource.Cache, response2.AuthenticationResultMetadata.TokenSource);
        }

        /// Based on the publicly available https://demo.duendesoftware.com/
        private Task<string> GetPrivateKeyJwtClientAssertionAsync(string optionsClientId, string optionsTokenEndpoint, CancellationToken optionsCancellationToken)
        {
            JsonWebKey jsonWebKey = new ("{'d':'GmiaucNIzdvsEzGjZjd43SDToy1pz-Ph-shsOUXXh-dsYNGftITGerp8bO1iryXh_zUEo8oDK3r1y4klTonQ6bLsWw4ogjLPmL3yiqsoSjJa1G2Ymh_RY_sFZLLXAcrmpbzdWIAkgkHSZTaliL6g57vA7gxvd8L4s82wgGer_JmURI0ECbaCg98JVS0Srtf9GeTRHoX4foLWKc1Vq6NHthzqRMLZe-aRBNU9IMvXNd7kCcIbHCM3GTD_8cFj135nBPP2HOgC_ZXI1txsEf-djqJj8W5vaM7ViKU28IDv1gZGH3CatoysYx6jv1XJVvb2PH8RbFKbJmeyUm3Wvo-rgQ','dp':'YNjVBTCIwZD65WCht5ve06vnBLP_Po1NtL_4lkholmPzJ5jbLYBU8f5foNp8DVJBdFQW7wcLmx85-NC5Pl1ZeyA-Ecbw4fDraa5Z4wUKlF0LT6VV79rfOF19y8kwf6MigyrDqMLcH_CRnRGg5NfDsijlZXffINGuxg6wWzhiqqE','dq':'LfMDQbvTFNngkZjKkN2CBh5_MBG6Yrmfy4kWA8IC2HQqID5FtreiY2MTAwoDcoINfh3S5CItpuq94tlB2t-VUv8wunhbngHiB5xUprwGAAnwJ3DL39D2m43i_3YP-UO1TgZQUAOh7Jrd4foatpatTvBtY3F1DrCrUKE5Kkn770M','e':'AQAB','kid':'ZzAjSnraU3bkWGnnAqLapYGpTyNfLbjbzgAPbbW2GEA','kty':'RSA','n':'wWwQFtSzeRjjerpEM5Rmqz_DsNaZ9S1Bw6UbZkDLowuuTCjBWUax0vBMMxdy6XjEEK4Oq9lKMvx9JzjmeJf1knoqSNrox3Ka0rnxXpNAz6sATvme8p9mTXyp0cX4lF4U2J54xa2_S9NF5QWvpXvBeC4GAJx7QaSw4zrUkrc6XyaAiFnLhQEwKJCwUw4NOqIuYvYp_IXhw-5Ti_icDlZS-282PcccnBeOcX7vc21pozibIdmZJKqXNsL1Ibx5Nkx1F1jLnekJAmdaACDjYRLL_6n3W4wUp19UvzB1lGtXcJKLLkqB6YDiZNu16OSiSprfmrRXvYmvD8m6Fnl5aetgKw','p':'7enorp9Pm9XSHaCvQyENcvdU99WCPbnp8vc0KnY_0g9UdX4ZDH07JwKu6DQEwfmUA1qspC-e_KFWTl3x0-I2eJRnHjLOoLrTjrVSBRhBMGEH5PvtZTTThnIY2LReH-6EhceGvcsJ_MhNDUEZLykiH1OnKhmRuvSdhi8oiETqtPE','q':'0CBLGi_kRPLqI8yfVkpBbA9zkCAshgrWWn9hsq6a7Zl2LcLaLBRUxH0q1jWnXgeJh9o5v8sYGXwhbrmuypw7kJ0uA3OgEzSsNvX5Ay3R9sNel-3Mqm8Me5OfWWvmTEBOci8RwHstdR-7b9ZT13jk-dsZI7OlV_uBja1ny9Nz9ts','qi':'pG6J4dcUDrDndMxa-ee1yG4KjZqqyCQcmPAfqklI2LmnpRIjcK78scclvpboI3JQyg6RCEKVMwAhVtQM6cBcIO3JrHgqeYDblp5wXHjto70HVW6Z8kBruNx1AH9E8LzNvSRL-JVTFzBkJuNgzKQfD0G77tQRgJ-Ri7qu3_9o1M4'}"); 
            var tokenHandler = new JwtSecurityTokenHandler { TokenLifetimeInMinutes = 5 };
            var securityToken = tokenHandler.CreateJwtSecurityToken(
                                                                    // iss must be the client_id of our application
                                                                    issuer: optionsClientId,
                                                                    // aud must be the identity provider (token endpoint)
                                                                    audience: optionsTokenEndpoint,
                                                                    // sub must be the client_id of our application
                                                                    subject: new ClaimsIdentity(
                                                                                                new List<Claim>
                                                                                                {
                                                                                                    new ("sub", optionsClientId),
                                                                                                    new ("jti", Guid.NewGuid().ToString())
                                                                                                }
                                                                                               ),
                                                                    // sign with the private key
                                                                    signingCredentials: new SigningCredentials(jsonWebKey, SecurityAlgorithms.RsaSha256)
                                                                   );
            var assertion = tokenHandler.WriteToken(securityToken);
            return Task.FromResult(assertion);
        }
    }
}
