//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    class DeviceAuthHelper : IDeviceAuthHelper
    {
        public bool CanHandleDeviceAuthChallenge { get { return true; } }

        public string CreateDeviceAuthChallengeResponse(IDictionary<string, string> challengeData)
        {
            string authHeaderTemplate = "PKeyAuth {0} Context=\"{1}\", Version=\"{2}\"";
            string expectedCertThumbprint = challengeData["CertThumbprint"];
          var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);

            try
            {
                store.Open(OpenFlags.ReadOnly);

                var certCollection = store.Certificates;
                var signingCert = certCollection.Find(X509FindType.FindByThumbprint, expectedCertThumbprint, false);
                if (signingCert.Count == 0)
                {
                    throw new FileNotFoundException(string.Format("Cert with thumbprint: '{0}' not found in local machine cert store.", expectedCertThumbprint));
                }

                X509Certificate2 certificate = signingCert[0];
                DeviceAuthJWTResponse response = new DeviceAuthJWTResponse(challengeData["SubmitUrl"], challengeData["nonce"], EncodingHelper.Base64Encode(certificate.GetRawCertDataString()));
                RSACryptoServiceProvider csp = new RSACryptoServiceProvider();

                byte[] sig = csp.SignData(new StringBuilder(response.GetResponseToSign()).ToByteArray(), CryptoConfig.MapNameToOID("SHA256"));
                string signedJwt = String.Format("{0}.{1}", response.GetResponseToSign(),
                    EncodingHelper.Base64Encode(Encoding.Default.GetString(sig)));

                return string.Format(authHeaderTemplate, signedJwt, challengeData["Context"], challengeData["Version"]);
            }
            finally
            {
                store.Close();
            }

            return null;
        }
    }
}
