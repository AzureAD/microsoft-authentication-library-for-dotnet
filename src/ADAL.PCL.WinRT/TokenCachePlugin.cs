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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class TokenCachePlugin : ITokenCachePlugin
    {
        private const string LocalSettingsContainerName = "ActiveDirectoryAuthenticationLibrary";

        private const string CacheValue = "CacheValue";
        private const string CacheValueSegmentCount = "CacheValueSegmentCount";
        private const string CacheValueLength = "CacheValueLength";
        private const int MaxCompositeValueLength = 1024;

        public void BeforeAccess(TokenCacheNotificationArgs args)
        {
            if (args != null && args.TokenCache != null)
            {
                try
                {
                    var localSettings = ApplicationData.Current.LocalSettings;
                    localSettings.CreateContainer(LocalSettingsContainerName, ApplicationDataCreateDisposition.Always);
                    byte[] state = GetCacheValue(localSettings.Containers[LocalSettingsContainerName].Values);
                    if (state != null)
                    {
                        args.TokenCache.Deserialize(state);
                    }
                }
                catch (Exception ex)
                {
                    PlatformPlugin.Logger.Warning(null, "Failed to load cache: " + ex);
                    // Ignore as the cache seems to be corrupt
                }
            }
        }
        
        public void AfterAccess(TokenCacheNotificationArgs args)
        {
            if (args != null && args.TokenCache != null && args.TokenCache.HasStateChanged)
            {
                try
                {
                    var localSettings = ApplicationData.Current.LocalSettings;
                    localSettings.CreateContainer(LocalSettingsContainerName, ApplicationDataCreateDisposition.Always);
                    SetCacheValue(localSettings.Containers[LocalSettingsContainerName].Values, args.TokenCache.Serialize());
                    args.TokenCache.HasStateChanged = false;
                }
                catch (Exception ex)
                {
                    PlatformPlugin.Logger.Warning(null, "Failed to save cache: " + ex);
                }
            }
        }

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
