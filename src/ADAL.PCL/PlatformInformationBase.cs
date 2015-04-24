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
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal abstract class PlatformInformationBase
    {
        public abstract string GetProductName();

        public abstract string GetEnvironmentVariable(string variable);

        public abstract Task<string> GetUserPrincipalNameAsync();

        public abstract string GetProcessorArchitecture();

        public abstract string GetOperatingSystem();

        public abstract string GetDeviceModel();

        public virtual string GetAssemblyFileVersionAttribute()
        {
            return typeof(AdalIdHelper).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
        }

        public async virtual Task<bool> IsUserLocalAsync(CallState callState)
        {
            return false;
        }

        public virtual bool IsDomainJoined()
        {
            return false;
        }

        public virtual void AddPromptBehaviorQueryParameter(IPlatformParameters parameters, DictionaryRequestParameters authorizationRequestParameters)
        {
            authorizationRequestParameters[OAuthParameter.Prompt] = PromptValue.Login;
        }

        public virtual bool GetCacheLoadPolicy(IPlatformParameters parameters)
        {
            return true;
        }

        public virtual Uri ValidateRedirectUri(Uri redirectUri, CallState callState)
        {
            if (redirectUri == null)
            {
                throw new ArgumentNullException("redirectUri");
            }

            return redirectUri;
        }

        public virtual string GetRedirectUriAsString(Uri redirectUri, CallState callState)
        {
            return redirectUri.OriginalString;
        }
    }
}
