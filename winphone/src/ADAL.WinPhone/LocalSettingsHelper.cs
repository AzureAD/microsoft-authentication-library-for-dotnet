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
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal static class LocalSettingsHelper
    {
        private const int MaxCompositeValueLength = 1024;

        /// <summary>
        /// Sets cache value as a set of properties in local setting value.
        /// If the cache value is too large, it would split it into multiple properties of max 1KB.
        /// The number of segments of the value is stored in a separate property.
        /// </summary>
        /// <param name="compositeValue">Value in ApplicationDataCompositeValue format in application's local settings</param>
        /// <param name="value">string value to store in the cache</param>
        public static void SetCacheValue(ApplicationDataCompositeValue compositeValue, string value)
        {
            string encryptedValue = CryptographyHelper.Encrypt(value);
            if (String.IsNullOrEmpty(encryptedValue))
            {
                compositeValue[CompositeCacheElement.CacheValueSegmentCount] = 1;
                compositeValue[CompositeCacheElement.CacheValue + 0] = encryptedValue;
            }
            else
            {
                int segmentCount = (encryptedValue.Length / MaxCompositeValueLength) + ((encryptedValue.Length % MaxCompositeValueLength == 0) ? 0 : 1);
                for (int i = 0; i < segmentCount - 1; i++)
                {
                    compositeValue[CompositeCacheElement.CacheValue + i] = encryptedValue.Substring(i * MaxCompositeValueLength, MaxCompositeValueLength);
                }

                compositeValue[CompositeCacheElement.CacheValue + (segmentCount - 1)] = encryptedValue.Substring((segmentCount - 1) * MaxCompositeValueLength);
                compositeValue[CompositeCacheElement.CacheValueSegmentCount] = segmentCount;
            }
        }

        /// <summary>
        /// Reads cache value from local settings and merge multiple properties if it is too long.
        /// The number of segments of the value is stored in a separate property.
        /// </summary>
        /// <param name="compositeValue">Value in ApplicationDataCompositeValue format in application's local settings</param>
        /// <returns></returns>
        public static string GetCacheValue(ApplicationDataCompositeValue compositeValue)
        {
            int segmentCount = (int)compositeValue[CompositeCacheElement.CacheValueSegmentCount];

            string encryptedValue;
            if (segmentCount == 1)
            {
                encryptedValue = (string)compositeValue[CompositeCacheElement.CacheValue + 0];
            }
            else
            {
                StringBuilder builder = new StringBuilder(segmentCount * MaxCompositeValueLength);

                for (int i = 0; i < segmentCount; i++)
                {
                    builder.Append((string)compositeValue[CompositeCacheElement.CacheValue + i]);
                }

                encryptedValue = builder.ToString();
            }

            return CryptographyHelper.Decrypt(encryptedValue);
        }

        public static void RemoveCacheValue(ApplicationDataCompositeValue compositeValue)
        {
            if (compositeValue.ContainsKey(CompositeCacheElement.CacheValueSegmentCount))
            {
                int segmentCount = (int)compositeValue[CompositeCacheElement.CacheValueSegmentCount];

                for (int i = 0; i < segmentCount; i++)
                {
                    compositeValue.Remove(CompositeCacheElement.CacheValue + i);
                }

                compositeValue.Remove(CompositeCacheElement.CacheValueSegmentCount);
            }
        }
    }
}
