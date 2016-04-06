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

using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.DataProtection;
using Windows.Storage.Streams;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal static class CryptographyHelper
    {
        // This descriptor does not require the enterprise authentication capability.
        private const string ProtectionDescriptor = "LOCAL=user";

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
