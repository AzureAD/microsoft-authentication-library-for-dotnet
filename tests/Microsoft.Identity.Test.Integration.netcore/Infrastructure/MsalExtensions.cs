// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
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

            return builder.WithSignedHttpRequestProofOfPossession(popAuthenticationConfiguration);
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

            RSAParameters parameters;

            if (popCredentials.Key is RsaSecurityKey rsaKey)
            {
                parameters = rsaKey.Rsa == null ? rsaKey.Parameters : rsaKey.Rsa.ExportParameters(false);
            }
            else if (popCredentials.Key is X509SecurityKey x509Key)
            {
                var rsa = x509Key.Certificate.GetRSAPublicKey();
                if (rsa == null)
                {
                    throw new NotSupportedException("Only certificates with RSA keys are supported");
                }
                parameters = rsa.ExportParameters(false);
            }
            else
            {
                throw new NotImplementedException("Only RSA and X509 POP keys are supported");
            }

            CannonicalPublicKeyJwk = CreateJwkClaim(parameters);
            CryptographicAlgorithm = popCredentials.Algorithm;
            _popCredentials = popCredentials;
            _assertNotSigned = assertNotSigned;
        }

        private string CreateJwkClaim(RSAParameters parameters)
        {
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
