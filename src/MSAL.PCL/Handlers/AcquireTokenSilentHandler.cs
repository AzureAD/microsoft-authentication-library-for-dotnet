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
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Handlers
{
    internal class AcquireTokenSilentHandler : AcquireTokenHandlerBase
    {
        private IPlatformParameters parameters;


        public AcquireTokenSilentHandler(HandlerData handlerData, string userIdentifer, IPlatformParameters parameters) 
            : this(handlerData, (User)null, parameters)
        {
            this.MapIdentifierToUser(userIdentifer);
            PlatformPlugin.BrokerHelper.PlatformParameters = parameters;
            this.SupportADFS = false;
        }

        public AcquireTokenSilentHandler(HandlerData handlerData, User user, IPlatformParameters parameters)
            : base(handlerData)
        {
            if (user != null)
            {
                this.User = user;
            }

            PlatformPlugin.BrokerHelper.PlatformParameters = parameters;    
            this.SupportADFS = false;
        }

        protected override Task<AuthenticationResultEx> SendTokenRequestAsync()
        {
            PlatformPlugin.Logger.Verbose(this.CallState, "No token matching arguments found in the cache");
            throw new MsalSilentTokenAcquisitionException();
        }

        protected override void AddAditionalRequestParameters(DictionaryRequestParameters requestParameters)
        {            
        }
    }
}
