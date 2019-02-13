//----------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using Foundation;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Utils;
using Security;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;

namespace Microsoft.Identity.Client.Platforms.iOS
{
    internal class BrokerKeyHelper
    {
        internal static byte[] GetRawBrokerKey(ICoreLogger logger)
        {
            byte[] brokerKey = null;
            SecRecord record = new SecRecord(SecKind.GenericPassword)
            {
                Generic = NSData.FromString(iOSBrokerConstants.LocalSettingsContainerName),
                Service = iOSBrokerConstants.BrokerKeyService,
                Account = iOSBrokerConstants.BrokerKeyAccount,
                Label = iOSBrokerConstants.BrokerKeyLabel,
                Comment = iOSBrokerConstants.BrokerKeyComment,
                Description = iOSBrokerConstants.BrokerKeyStorageDescription
            };

            NSData key = SecKeyChain.QueryAsData(record);
            if (key == null)
            {
                AesManaged algo = new AesManaged();
                algo.GenerateKey();
                byte[] rawBytes = algo.Key;
                NSData byteData = NSData.FromArray(rawBytes);
                record = new SecRecord(SecKind.GenericPassword)
                {
                    Generic = NSData.FromString(iOSBrokerConstants.LocalSettingsContainerName),
                    Service = iOSBrokerConstants.BrokerKeyService,
                    Account = iOSBrokerConstants.BrokerKeyAccount,
                    Label = iOSBrokerConstants.BrokerKeyLabel,
                    Comment = iOSBrokerConstants.BrokerKeyComment,
                    Description = iOSBrokerConstants.BrokerKeyStorageDescription,
                    ValueData = byteData
                };

                var result = SecKeyChain.Add(record);
                if (result != SecStatusCode.Success)
                {
                    logger.Info(iOSBrokerConstants.FailedToSaveBrokerKey + result);
                }

                brokerKey = byteData.ToArray();
            }
            else
            {
                brokerKey = key.ToArray();
            }

            return brokerKey;
        }

        internal static string DecryptBrokerResponse(string encryptedBrokerResponse, ICoreLogger logger)
        {
            byte[] outputBytes = Base64UrlHelpers.DecodeToBytes(encryptedBrokerResponse);
            string plaintext = string.Empty;

            using (MemoryStream memoryStream = new MemoryStream(outputBytes))
            {
                byte[] key = GetRawBrokerKey(logger);

                AesManaged algo = GetCryptoAlgorithm(key);
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, algo.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    using (StreamReader srDecrypt = new StreamReader(cryptoStream))
                    {
                        plaintext = srDecrypt.ReadToEnd();
                    }
                }
            }

            return plaintext;
        }

        private static AesManaged GetCryptoAlgorithm(byte[] key)
        {
            AesManaged algorithm = new AesManaged();

            //set the mode, padding and block size
            algorithm.Padding = PaddingMode.PKCS7;
            algorithm.Mode = CipherMode.CBC;
            algorithm.KeySize = 256;
            algorithm.BlockSize = 128;
            if (key != null)
            {
                algorithm.Key = key;
            }

            algorithm.IV = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return algorithm;
        }
    }
}