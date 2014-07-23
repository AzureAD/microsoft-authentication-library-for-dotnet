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
using System.Threading.Tasks;
using Windows.Networking;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal static partial class PlatformSpecificHelper
    {
        public static string GetProductName()
        {
            return "WinRT";
        }

        public async static Task<bool> IsUserLocalAsync()
        {
            if (!Windows.System.UserProfile.UserInformation.NameAccessAllowed)
            {
                throw new AdalException(AdalError.CannotAccessUserInformation);
            }

            try
            {
                return string.IsNullOrEmpty(await Windows.System.UserProfile.UserInformation.GetDomainNameAsync());
            }
            catch (UnauthorizedAccessException)
            {
                // This mostly means Enterprise capability is missing, so WIA cannot be used and
                // we return true to add form auth parameter in the caller.
                return true;
            }
        }

        public async static Task<string> GetUserPrincipalNameAsync()
        {
            if (!Windows.System.UserProfile.UserInformation.NameAccessAllowed)
            {
                throw new AdalException(AdalError.CannotAccessUserInformation);
            }

            try
            {
                return await Windows.System.UserProfile.UserInformation.GetPrincipalNameAsync();
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new AdalException(AdalError.UnauthorizedUserInformationAccess, ex);
            }
        }

        public static bool IsDomainJoined()
        {
            IReadOnlyList<HostName> hostNamesList = Windows.Networking.Connectivity.NetworkInformation
                .GetHostNames();

            foreach (var entry in hostNamesList)
            {
                if (entry.Type == Windows.Networking.HostNameType.DomainName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
