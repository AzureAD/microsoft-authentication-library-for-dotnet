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

using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class PlatformInformation : PlatformInformationBase
    {
        public override string GetProductName()
        {
            return "PCL.Android";
        }

        public override string GetEnvironmentVariable(string variable)
        {
            // TODO: Update
            return null;
        }

        public override Task<string> GetUserPrincipalNameAsync()
        {
            // TODO: Update
            return null;
        }

        public override string GetProcessorArchitecture()
        {
            return null;
        }

        public override string GetOperatingSystem()
        {
            return null;
        }

        public override string GetDeviceModel()
        {
            return null;
        }
    }
}
