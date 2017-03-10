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
using System.Text;

using Windows.Foundation.Collections;
using Windows.Storage;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal static class LocalSettingsHelper
    {
        private const string CacheValue = "CacheValue";
        private const string CacheValueSegmentCount = "CacheValueSegmentCount";
        private const string CacheValueLength = "CacheValueLength";
        private const int MaxCompositeValueLength = 1024;

        internal static void SetCacheValue(IPropertySet containerValues, byte[] value)
        {
            byte[] encryptedValue = CryptographyHelper.Encrypt(value);
            containerValues[CacheValueLength] = encryptedValue.Length;
            if (encryptedValue == null)
            {
                containerValues[CacheValueSegmentCount] = 1;
                containerValues[CacheValue + 0] = null;
            }
            else
            {
                int segmentCount = (encryptedValue.Length / MaxCompositeValueLength) + ((encryptedValue.Length % MaxCompositeValueLength == 0) ? 0 : 1);
                byte[] subValue = new byte[MaxCompositeValueLength];
                for (int i = 0; i < segmentCount - 1; i++)
                {
                    Array.Copy(encryptedValue, i * MaxCompositeValueLength, subValue, 0, MaxCompositeValueLength);
                    containerValues[CacheValue + i] = subValue;
                }

                int copiedLength = (segmentCount - 1) * MaxCompositeValueLength;
                Array.Copy(encryptedValue, copiedLength, subValue, 0, encryptedValue.Length - copiedLength);
                containerValues[CacheValue + (segmentCount - 1)] = subValue;
                containerValues[CacheValueSegmentCount] = segmentCount;
            }
        }

        internal static byte[] GetCacheValue(IPropertySet containerValues)
        {
            if (!containerValues.ContainsKey(CacheValueLength))
            {
                return null;
            }

            int encyptedValueLength = (int)containerValues[CacheValueLength];
            int segmentCount = (int)containerValues[CacheValueSegmentCount];

            byte[] encryptedValue = new byte[encyptedValueLength];
            if (segmentCount == 1)
            {
                encryptedValue = (byte[])containerValues[CacheValue + 0];
            }
            else
            {
                for (int i = 0; i < segmentCount - 1; i++)
                {
                    Array.Copy((byte[])containerValues[CacheValue + i], 0, encryptedValue, i * MaxCompositeValueLength, MaxCompositeValueLength); 
                }
            }

            Array.Copy((byte[])containerValues[CacheValue + (segmentCount - 1)], 0, encryptedValue, (segmentCount - 1) * MaxCompositeValueLength, encyptedValueLength - (segmentCount - 1) * MaxCompositeValueLength);
            return CryptographyHelper.Decrypt(encryptedValue);
        }
    }
}
