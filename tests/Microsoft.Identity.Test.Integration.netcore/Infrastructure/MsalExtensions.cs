// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.Utils;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration
{
    internal static class MsalExtensions
    {
        public static AcquireTokenForClientParameterBuilder WithPoPSignedRequest(
            this AcquireTokenForClientParameterBuilder builder,
            SigningCredentials popCredentials) // TODO: this only supports RSA for now
        {

            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (popCredentials is null)
            {
                throw new ArgumentNullException(nameof(popCredentials));
            }

            var popAuthenticationConfiguration
                = new PoPAuthenticationConfiguration()
                {
                    SignHttpRequest = false,
                    PopCryptoProvider = new SigningCredentialsToPopCryptoProviderAdapter(popCredentials, assertNotSigned: true)
                };

            return builder.WithProofOfPossession(popAuthenticationConfiguration);
        }
    }

    internal class SigningCredentialsToPopCryptoProviderAdapter : IPoPCryptoProvider
    {
        private readonly SigningCredentials _popCredentials;
        private readonly bool _assertNotSigned;

        public SigningCredentialsToPopCryptoProviderAdapter(SigningCredentials popCredentials, bool assertNotSigned)
        {
            if (popCredentials is null)
            {
                throw new ArgumentNullException(nameof(popCredentials));
            }

            var rsaKey = popCredentials.Key as RsaSecurityKey;
            if (rsaKey == null)
            {
                throw new NotImplementedException("Only RSA POP keys are supported"); // TODO: support for other crypto?
            }

            CannonicalPublicKeyJwk = CreateJwkClaim(rsaKey, popCredentials.Algorithm);
            CryptographicAlgorithm = popCredentials.Algorithm;
            _popCredentials = popCredentials;
            _assertNotSigned = assertNotSigned;
        }

        private string CreateJwkClaim(RsaSecurityKey key, string algorithm)
        {
            var parameters = key.Rsa == null ? key.Parameters : key.Rsa.ExportParameters(false);
            //return "{\"kty\":\"RSA\",\"n\":\"" + Base64UrlEncoder.Encode(parameters.Modulus) + "\",\"e\":\"" + Base64UrlEncoder.Encode(parameters.Exponent) + "\",\"alg\":\"" + algorithm + "\"}";
            return $@"{{""e"":""{Base64UrlHelpers.Encode(parameters.Exponent)}"",""kty"":""RSA"",""n"":""{Base64UrlHelpers.Encode(parameters.Modulus)}""}}";

        }

        public string CannonicalPublicKeyJwk { get; }

        public string CryptographicAlgorithm { get; }

        // This will not be called if SAL constructs the SignedHttpRequest
        public byte[] Sign(byte[] data)
        {
            if (_assertNotSigned)
            {
                Assert.Fail("Sing call is not expected");
            }

            var cryptoFactory = _popCredentials.CryptoProviderFactory;
            var signatureProvider = cryptoFactory.CreateForSigning(_popCredentials.Key, _popCredentials.Algorithm);
            try
            {
                return signatureProvider.Sign(data);
            }
            finally
            {
                cryptoFactory.ReleaseSignatureProvider(signatureProvider);
            }
        }
    }
}
