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
            parameters[AdalIdParameter.Product] = PlatformPlugin.PlatformInformation.GetProductName();
            parameters[AdalIdParameter.Version] = AdalIdHelper.GetAdalVersion();

            var processorInofrmation = PlatformPlugin.PlatformInformation.GetProcessorArchitecture();
            if (processorInofrmation != null)
            {
                parameters[AdalIdParameter.CpuPlatform] = processorInofrmation;
            }

            var osInformation = PlatformPlugin.PlatformInformation.GetOperatingSystem();
            if (osInformation != null)
            {
                parameters[AdalIdParameter.OS] = osInformation;
            }

            var deviceInformation = PlatformPlugin.PlatformInformation.GetDeviceModel();
            if (deviceInformation != null)
            {
                parameters[AdalIdParameter.DeviceModel] = deviceInformation;
            }
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