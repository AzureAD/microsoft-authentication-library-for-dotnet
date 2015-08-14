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
using System.IO;
using System.Security.Cryptography;
using Foundation;
using Security;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    static class BrokerKeyHelper
    {
        private const string LocalSettingsContainerName = "ActiveDirectoryAuthenticationLibrary";

        internal static String GetBase64UrlBrokerKey()
        {
            return Base64UrlEncoder.Encode(GetRawBrokerKey());
        }

        private static byte[] GetRawBrokerKey()
        {
            byte[] brokeyKey = null;
            SecRecord record = new SecRecord(SecKind.GenericPassword)
            {
                Generic = NSData.FromString(LocalSettingsContainerName),
                Service = "Service",
                Account = "brokerKey",
                Label = "BrokerKeyLabel",
                Comment = "Broker Comment",
                Description = "Storage for broker key"
            };

            NSData key = SecKeyChain.QueryAsData(record);
            if (key == null)
            {
                AesManaged algo = GetCryptoAlgorithm();
                algo.GenerateKey();
                byte[] rawBytes = algo.Key;
                NSData byteData = NSData.FromArray(rawBytes);
                record = new SecRecord(SecKind.GenericPassword)
                {
                    Generic = NSData.FromString(LocalSettingsContainerName),
                    Service = "Service",
                    Account = "brokerKey",
                    Label = "BrokerKeyLabel",
                    Comment = "Broker Comment",
                    Description = "Storage for broker key",
                    ValueData = byteData
                };

                SecStatusCode code = SecKeyChain.Add(record);
                brokeyKey = byteData.ToArray();
            }
            else
            {
                brokeyKey = key.ToArray();
            }

            return brokeyKey;
        }

        internal static String DecryptBrokerResponse(String encryptedBrokerResponse)
        {
            byte[] outputBytes = encryptedBrokerResponse.ToByteArray();
            string plaintext = string.Empty;
            using (MemoryStream memoryStream = new MemoryStream(outputBytes))
            {
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, GetCryptoAlgorithm().CreateDecryptor(GetRawBrokerKey(), GetRawBrokerKey()), CryptoStreamMode.Read))
                {
                    using (StreamReader srDecrypt = new StreamReader(cryptoStream))
                    {
                        plaintext = srDecrypt.ReadToEnd();
                    }
                }
            }

            return plaintext;
        }

        private static AesManaged GetCryptoAlgorithm()
        {
            AesManaged algorithm = new AesManaged();
            //set the mode, padding and block size
            algorithm.Padding = PaddingMode.PKCS7;
            algorithm.Mode = CipherMode.CBC;
            algorithm.KeySize = 256;
            algorithm.BlockSize = 128;
            return algorithm;
        }
    }
}