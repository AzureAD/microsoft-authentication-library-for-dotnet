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

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class RequestCreationHelper : IRequestCreationHelper
    {
        public bool RecordClientMetrics
        {
            get { return true; }            
        }

        public void AddAdalIdParameters(IDictionary<string, string> parameters)
        {
            parameters[AdalIdParameter.Product] = PlatformSpecificHelper.GetProductName();
            parameters[AdalIdParameter.Version] = AdalIdHelper.GetAdalVersion();

#if !ADAL_WINPHONE
            parameters[AdalIdParameter.CpuPlatform] = AdalIdHelper.GetProcessorArchitecture();
#endif

#if ADAL_NET
            parameters[AdalIdParameter.OS] = Environment.OSVersion.ToString();

            // Since ADAL .NET may be used on servers, for security reasons, we do not emit device type.
#elif SILVERLIGHT
            parameters[AdalIdParameter.OS] = Environment.OSVersion.ToString();
#else
            // In WinRT, there is no way to reliably get OS version. All can be done reliably is to check 
            // for existence of specific features which does not help in this case, so we do not emit OS in WinRT.

            var deviceInformation = new Windows.Security.ExchangeActiveSyncProvisioning.EasClientDeviceInformation();
            parameters[AdalIdParameter.DeviceModel] = deviceInformation.SystemProductName;
#endif
        }

        public DateTime GetJsonWebTokenValidFrom()
        {
            return DateTime.UtcNow;
        }

        public string GetJsonWebTokenId()
        {
            return Guid.NewGuid().ToString();            
        }
    }
}