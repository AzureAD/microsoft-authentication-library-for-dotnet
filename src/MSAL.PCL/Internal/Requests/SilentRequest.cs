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

using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal.OAuth2;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal class SilentRequest : BaseRequest
    {
        public SilentRequest(AuthenticationRequestParameters authenticationRequestParameters, string userIdentifer,
            IPlatformParameters parameters, bool forceRefresh)
            : this(authenticationRequestParameters, (User) null, parameters, forceRefresh)
        {
            this.User = this.MapIdentifierToUser(userIdentifer);
            PlatformPlugin.BrokerHelper.PlatformParameters = parameters;
            this.SupportADFS = false;
        }

        public SilentRequest(AuthenticationRequestParameters authenticationRequestParameters, User user,
            IPlatformParameters parameters, bool forceRefresh)
            : base(authenticationRequestParameters)
        {
            if (user != null)
            {
                this.User = user;
            }

            PlatformPlugin.BrokerHelper.PlatformParameters = parameters;
            this.SupportADFS = false;
            this.ForceRefresh = forceRefresh;
        }

        protected override void SetAdditionalRequestParameters(OAuth2Client client)
        {
            throw new System.NotImplementedException();
        }

        protected override Task<AuthenticationResultEx> SendTokenRequestAsync()
        {
            if (ResultEx == null)
            {
                PlatformPlugin.Logger.Verbose(this.CallState, "No token matching arguments found in the cache");
                throw new MsalSilentTokenAcquisitionException();
            }

            throw new MsalSilentTokenAcquisitionException(ResultEx.Exception);
        }
    }
}