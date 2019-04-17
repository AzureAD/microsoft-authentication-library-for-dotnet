// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Security.Cryptography.DataProtection;
using Windows.Storage.Streams;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

namespace Microsoft.Identity.Client.Platforms.uap
{
    internal class UapCryptographyManager : ICryptographyManager
    {
        // This descriptor does not require the enterprise authentication capability.
        private const string ProtectionDescriptor = "LOCAL=user";

        public string CreateBase64UrlEncodedSha256Hash(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }

            IBuffer inputBuffer = CryptographicBuffer.ConvertStringToBinary(input, BinaryStringEncoding.Utf8);
            var hasher = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha256);

            IBuffer hashed = hasher.HashData(inputBuffer);
            string output = CryptographicBuffer.EncodeToBase64String(hashed);
            return Base64UrlHelpers.Encode(Convert.FromBase64String(output));
        }

        public string GenerateCodeVerifier()
        {
            byte[] buffer = new byte[Constants.CodeVerifierByteSize];
            var windowsBuffer = CryptographicBuffer.GenerateRandom((uint)buffer.Length);
            Array.Copy(windowsBuffer.ToArray(), buffer, buffer.Length);

            return Base64UrlHelpers.Encode(buffer);
        }

        public string CreateSha256Hash(string input)
        {
            var hashed = CreateSha256HashBuffer(input);
            return hashed == null ? null : CryptographicBuffer.EncodeToBase64String(hashed);
        }

        private IBuffer CreateSha256HashBuffer(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }

            IBuffer inputBuffer = CryptographicBuffer.ConvertStringToBinary(input, BinaryStringEncoding.Utf8);

            var hasher = HashAlgorithmProvider.OpenAlgorithm("SHA256");
            IBuffer hashed = hasher.HashData(inputBuffer);
            return hashed;
        }

        public byte[] CreateSha256HashBytes(string input)
        {
            var hashed = CreateSha256HashBuffer(input);
            return hashed?.ToArray();
        }

        public string Encrypt(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return message;
            }

            DataProtectionProvider dataProtectionProvider = new DataProtectionProvider(ProtectionDescriptor);
            IBuffer messageBuffer = CryptographicBuffer.ConvertStringToBinary(message, BinaryStringEncoding.Utf8);
            IBuffer protectedBuffer = dataProtectionProvider.ProtectAsync(messageBuffer).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
            return Convert.ToBase64String(protectedBuffer.ToArray(0, (int)protectedBuffer.Length));
        }

        public string Decrypt(string encryptedMessage)
        {
            if (string.IsNullOrEmpty(encryptedMessage))
            {
                return encryptedMessage;
            }

            DataProtectionProvider dataProtectionProvider = new DataProtectionProvider(ProtectionDescriptor);
            IBuffer messageBuffer = Convert.FromBase64String(encryptedMessage).AsBuffer();
            IBuffer unprotectedBuffer = dataProtectionProvider.UnprotectAsync(messageBuffer).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
            return CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, unprotectedBuffer);
        }

        public byte[] Encrypt(byte[] message)
        {
            if (message == null)
            {
                return new byte[]{};
            }

            DataProtectionProvider dataProtectionProvider = new DataProtectionProvider(ProtectionDescriptor);
            IBuffer protectedBuffer = dataProtectionProvider.ProtectAsync(message.AsBuffer()).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
            return protectedBuffer.ToArray(0, (int)protectedBuffer.Length);
        }

        public byte[] Decrypt(byte[] encryptedMessage)
        {
            if (encryptedMessage == null)
            {
                return null;
            }

            DataProtectionProvider dataProtectionProvider = new DataProtectionProvider(ProtectionDescriptor);
            IBuffer buffer = dataProtectionProvider.UnprotectAsync(encryptedMessage.AsBuffer()).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
            return buffer.ToArray(0, (int)buffer.Length);
        }

        /// <inheritdoc />
        public byte[] SignWithCertificate(string message, X509Certificate2 certificate)
        {
            // Used by Confidential Client, which is hidden on UWP
            throw new NotImplementedException();
        }

    }
}
