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
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class AcquireTokenSilentHandler : AcquireTokenHandlerBase
    {
        public AcquireTokenSilentHandler(RequestData requestData, UserIdentifier userId, IPlatformParameters parameters)
            : base(requestData)
        {
            if (userId == null)
            {
                throw new ArgumentNullException("userId", AdalErrorMessage.SpecifyAnyUser);
            }
            requestData.SubjectType = requestData.ClientKey.HasCredential
                ? TokenSubjectType.UserPlusClient
                : TokenSubjectType.User;
            this.UniqueId = userId.UniqueId;
            this.DisplayableId = userId.DisplayableId;
            this.UserIdentifierType = userId.Type;
            PlatformPlugin.BrokerHelper.PlatformParameters = parameters;    
            this.SupportADFS = true;

            this.brokerParameters["username"] = userId.Id;
            this.brokerParameters["username_type"] = userId.Type.ToString();
            this.brokerParameters["silent_broker_flow"] = null; //add key
        }

        protected override Task<AuthenticationResultEx> SendTokenRequestAsync()
        {
            if (ResultEx == null)
                {
                PlatformPlugin.Logger.Verbose(this.CallState, "No token matching arguments found in the cache");
                throw new AdalSilentTokenAcquisitionException();
                }
            
            throw new AdalSilentTokenAcquisitionException(ResultEx.Exception);

        }

        protected override void AddAditionalRequestParameters(DictionaryRequestParameters requestParameters)
        {            
        }
    }
}
