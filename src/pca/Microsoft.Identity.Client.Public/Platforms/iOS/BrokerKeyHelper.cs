// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
