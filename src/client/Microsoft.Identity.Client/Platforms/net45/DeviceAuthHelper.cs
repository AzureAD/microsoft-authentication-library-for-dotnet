using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client.Platforms.net45
{
    internal static class DeviceAuthHelper
    {
        public static bool CanHandleDeviceAuthChallenge
        {
            get { return true; }
        }

        public static async Task<string> CreateDeviceAuthChallengeResponseAsync(IDictionary<string, string> challengeData)
        {
            string authHeaderTemplate = "PKeyAuth {0}, Context=\"{1}\", Version=\"{2}\"";

            X509Certificate2 certificate = null;
            try
            {
                certificate = FindCertificate(challengeData);
            }
            catch (MsalException ex)
            {
                if (ex.ErrorCode == MsalError.DeviceCertificateNotFound)
                {
                    return await Task.FromResult(string.Format(CultureInfo.InvariantCulture, @"PKeyAuth Context=""{0}"",Version=""{1}""", challengeData["Context"], challengeData["Version"])).ConfigureAwait(false);
                }
            }

            DeviceAuthJWTResponse response = new DeviceAuthJWTResponse(challengeData["SubmitUrl"],
                challengeData["nonce"], Convert.ToBase64String(certificate.GetRawCertData()));
            CngKey key = SigningHelper.GetCngPrivateKey(certificate);
            byte[] sig = null;
            using (RSACng rsa = new RSACng(key))
            {
                rsa.SignatureHashAlgorithm = CngAlgorithm.Sha256;
                sig = rsa.SignData(response.GetResponseToSign().ToByteArray());
            }

            string signedJwt = string.Format(CultureInfo.InvariantCulture, "{0}.{1}", response.GetResponseToSign(),
                Base64UrlHelpers.Encode(sig));
            string authToken = string.Format(CultureInfo.InvariantCulture, " AuthToken=\"{0}\"", signedJwt);
            Task<string> resultTask =
                Task.Factory.StartNew(
                    () =>
                    {
                        return string.Format(CultureInfo.InvariantCulture, authHeaderTemplate, authToken,
                            challengeData["Context"],
                            challengeData["Version"]);
                    });

            return await resultTask.ConfigureAwait(false);
        }

        private static CngKey GetCngPrivateKey(X509Certificate2 certificate)
        {
            using (var certContext = GetCertificateContext(certificate))
            using (SafeNCryptKeyHandle privateKeyHandle = X509Native.AcquireCngPrivateKey(certContext))
            {
                // We need to assert for full trust when opening the CNG key because
                // CngKey.Open(SafeNCryptKeyHandle) does a full demand for full trust, and we want to allow
                // access to a certificate's private key by anyone who has access to the certificate itself.
                new PermissionSet(PermissionState.Unrestricted).Assert();
                return CngKey.Open(privateKeyHandle, CngKeyHandleOpenOptions.None);
            }
        }

        public static SafeCertContextHandle GetCertificateContext(X509Certificate certificate)
        {
            SafeCertContextHandle certContext = X509Native.DuplicateCertContext(certificate.Handle);

            // Make sure to keep the X509Certificate object alive until after its certificate context is
            // duplicated, otherwise it could end up being closed out from underneath us before we get a
            // chance to duplicate the handle.
            GC.KeepAlive(certificate);

            return certContext;
        }

        private static X509Certificate2 FindCertificate(IDictionary<string, string> challengeData)
        {
            var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            try
            {
                store.Open(OpenFlags.ReadOnly);
                var certCollection = store.Certificates;
                if (challengeData.ContainsKey("CertAuthorities"))
                {
                    return FindCertificateByCertAuthorities(challengeData, certCollection);
                }

                X509Certificate2Collection signingCert = null;
                signingCert = certCollection.Find(X509FindType.FindByThumbprint, challengeData["CertThumbprint"],
                    false);
                if (signingCert.Count == 0)
                {
                    throw new AdalException(AdalError.DeviceCertificateNotFound,
                        string.Format(CultureInfo.CurrentCulture, AdalErrorMessage.DeviceCertificateNotFoundTemplate,
                            "Cert thumbprint:" + challengeData["CertThumbprint"]));
                }

                return signingCert[0];
            }
            finally
            {
                store.Close();
            }
        }

        private static X509Certificate2 FindCertificateByCertAuthorities(IDictionary<string, string> challengeData, X509Certificate2Collection certCollection)
        {
            X509Certificate2Collection signingCert = null;
            string[] certAuthorities = challengeData["CertAuthorities"].Split(new[] { ";" },
                StringSplitOptions.None);
            foreach (var certAuthority in certAuthorities)
            {
                //reverse the tokenized string and replace "," with " + "
                string[] dNames = certAuthority.Split(new[] { "," }, StringSplitOptions.None);
                string distinguishedIssuerName = dNames[dNames.Length - 1];
                for (int i = dNames.Length - 2; i >= 0; i--)
                {
                    distinguishedIssuerName += " + " + dNames[i].Trim();
                }

                signingCert = certCollection.Find(X509FindType.FindByIssuerDistinguishedName,
                    distinguishedIssuerName, false);
                if (signingCert.Count > 0)
                {
                    break;
                }
            }

            if (signingCert == null || signingCert.Count == 0)
            {
                throw new AdalException(AdalError.DeviceCertificateNotFound,
                    string.Format(CultureInfo.CurrentCulture,
                        AdalErrorMessage.DeviceCertificateNotFoundTemplate,
                        "Cert Authorities:" + challengeData["CertAuthorities"]));
            }

            return signingCert[0];
        }
    }
}
