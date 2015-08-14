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
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Certificates;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class DeviceAuthHelper : IDeviceAuthHelper
    {
        public bool CanHandleDeviceAuthChallenge
        {
            get { return true; }
        }

        public string CreateDeviceAuthChallengeResponse(IDictionary<string, string> challengeData)
        {
            string authHeaderTemplate = "PKeyAuth {0} Context=\"{1}\", Version=\"{2}\"";


            Certificate certificate = FindCertificate(challengeData);
            DeviceAuthJWTResponse response = new DeviceAuthJWTResponse(challengeData["SubmitUrl"],
                challengeData["nonce"], Convert.ToBase64String(certificate.GetCertificateBlob().ToArray()));
            IBuffer input = CryptographicBuffer.ConvertStringToBinary(response.GetResponseToSign(),
                BinaryStringEncoding.Utf16BE);
            CryptographicKey keyPair =
                RunAsyncTaskAndWait(
                    PersistedKeyProvider.OpenKeyPairFromCertificateAsync(certificate, HashAlgorithmNames.Sha256,
                        CryptographicPadding.RsaPkcs1V15).AsTask());

            IBuffer signed = CryptographicEngine.Sign(keyPair, input);

            return string.Format(authHeaderTemplate,
                string.Format("{0}.{1}", response.GetResponseToSign(), EncodingHelper.GetString(signed.ToArray())), challengeData["Context"], challengeData["Version"]);
        }

        private Certificate FindCertificate(IDictionary<string, string> challengeData)
        {
            CertificateQuery query = new CertificateQuery();
            if (challengeData.ContainsKey("CertAuthorities"))
            {
                query.IssuerName = challengeData["CertAuthorities"];
            }
            else
            {
                query.Thumbprint = challengeData["CertThumbprint"].ToByteArray();
            }
            IReadOnlyList<Certificate> certificates = RunAsyncTaskAndWait(CertificateStores.FindAllAsync(query).AsTask());

            if (certificates.Count == 0)
            {
                throw new AdalException();
            }

            return certificates[0];
        }

        private static T RunAsyncTaskAndWait<T>(Task<T> task)
        {
            try
            {
                Task.Run(async () => await task.ConfigureAwait(false)).Wait();
                return task.Result;
            }
            catch (AggregateException ae)
            {
                // Any exception thrown as a result of running task will cause AggregateException to be thrown with 
                // actual exception as inner.
                throw ae.InnerExceptions[0];
            }
        }
    }
}
