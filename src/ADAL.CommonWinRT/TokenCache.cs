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
using Windows.Storage;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    public sealed partial class TokenCache
    {
        private static void DefaultTokenCache_BeforeAccess(TokenCacheNotificationArgs args)
        {
            try
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                localSettings.CreateContainer(LocalSettingsContainerName, ApplicationDataCreateDisposition.Always);
                byte[] state = LocalSettingsHelper.GetCacheValue(localSettings.Containers[LocalSettingsContainerName].Values);
                if (state != null)
                {
                    DefaultShared.Deserialize(state);
                }
            }
            catch (Exception ex)
            {
                Logger.Warning(null, "Failed to load cache: " + ex);
                // Ignore as the cache seems to be corrupt
            }
        }
        private static void DefaultTokenCache_AfterAccess(TokenCacheNotificationArgs args)
        {
            if (DefaultShared.HasStateChanged)
            {
                try
                {
                    var localSettings = ApplicationData.Current.LocalSettings;
                    localSettings.CreateContainer(LocalSettingsContainerName, ApplicationDataCreateDisposition.Always);
                    LocalSettingsHelper.SetCacheValue(localSettings.Containers[LocalSettingsContainerName].Values, DefaultShared.Serialize());
                    DefaultShared.HasStateChanged = false;
                }
                catch (Exception ex)
                {
                    Logger.Warning(null, "Failed to save cache: " + ex);
                }
            }
        }
    }
}
