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
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Security.Cryptography.DataProtection;
using Windows.Storage.Streams;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class CryptographyHelper : ICryptographyHelper
    {
        // This descriptor does not require the enterprise authentication capability.
        private const string ProtectionDescriptor = "LOCAL=user";

        public string CreateSha256Hash(string input)
        {
            IBuffer inputBuffer = CryptographicBuffer.ConvertStringToBinary(input, BinaryStringEncoding.Utf8);

            var hasher = HashAlgorithmProvider.OpenAlgorithm("SHA256");
            IBuffer hashed = hasher.HashData(inputBuffer);

            return CryptographicBuffer.EncodeToBase64String(hashed);
        }

        public byte[] SignWithCertificate(string message, byte[] rawData, string password)
        {
            throw new NotImplementedException();
        }

        public string GetX509CertificateThumbprint(ClientAssertionCertificate credential)
        {
            throw new NotImplementedException();
        }

        public static string Encrypt(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return message;
            }

            DataProtectionProvider dataProtectionProvider = new DataProtectionProvider(ProtectionDescriptor);
            IBuffer messageBuffer = CryptographicBuffer.ConvertStringToBinary(message, BinaryStringEncoding.Utf8);
            IBuffer protectedBuffer = RunAsyncTaskAndWait(dataProtectionProvider.ProtectAsync(messageBuffer).AsTask());
            return Convert.ToBase64String(protectedBuffer.ToArray(0, (int)protectedBuffer.Length));
        }

        public static string Decrypt(string encryptedMessage)
        {
            if (string.IsNullOrEmpty(encryptedMessage))
            {
                return encryptedMessage;
            }

            DataProtectionProvider dataProtectionProvider = new DataProtectionProvider(ProtectionDescriptor);
            IBuffer messageBuffer = Convert.FromBase64String(encryptedMessage).AsBuffer();
            IBuffer unprotectedBuffer = RunAsyncTaskAndWait(dataProtectionProvider.UnprotectAsync(messageBuffer).AsTask());
            return CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, unprotectedBuffer);
        }

        public static byte[] Encrypt(byte[] message)
        {
            if (message == null)
            {
                return null;
            }

            DataProtectionProvider dataProtectionProvider = new DataProtectionProvider(ProtectionDescriptor);
            IBuffer protectedBuffer = RunAsyncTaskAndWait(dataProtectionProvider.ProtectAsync(message.AsBuffer()).AsTask());
            return protectedBuffer.ToArray(0, (int)protectedBuffer.Length);
        }

        public static byte[] Decrypt(byte[] encryptedMessage)
        {
            if (encryptedMessage == null)
            {
                return null;
            }

            DataProtectionProvider dataProtectionProvider = new DataProtectionProvider(ProtectionDescriptor);
            IBuffer buffer = RunAsyncTaskAndWait(dataProtectionProvider.UnprotectAsync(encryptedMessage.AsBuffer()).AsTask());
            return buffer.ToArray(0, (int)buffer.Length);
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
