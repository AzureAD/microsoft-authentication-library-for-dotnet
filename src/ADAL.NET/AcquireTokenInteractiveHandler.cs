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
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal partial class AcquireTokenInteractiveHandler
    {
        protected override async Task PreTokenRequest()
        {
            await base.PreTokenRequest();

            // We do not have async interactive API in .NET, so we call this synchronous method instead.
            await this.AcquireAuthorizationAsync();
            this.VerifyAuthorizationResult();
        }

        internal async Task AcquireAuthorizationAsync()
        {
            Uri authorizationUri = this.CreateAuthorizationUri(await IncludeFormsAuthParamsAsync());
            string resultUri = await this.webUi.AcquireAuthorizationAsync(authorizationUri, this.redirectUri, this.CallState);
            this.authorizationResult = OAuth2Response.ParseAuthorizeResponse(resultUri, this.CallState);
        }

        internal async Task<bool> IncludeFormsAuthParamsAsync()
        {
            return (await PlatformPlugin.PlatformInformation.IsUserLocalAsync(this.CallState)) && PlatformPlugin.PlatformInformation.IsDomainJoined();
        }

        internal async Task<Uri> CreateAuthorizationUriAsync(Guid correlationId)
        {
            this.CallState.CorrelationId = correlationId;
            await this.Authenticator.UpdateFromTemplateAsync(this.CallState);
            return this.CreateAuthorizationUri(false);
        }
    }
}
