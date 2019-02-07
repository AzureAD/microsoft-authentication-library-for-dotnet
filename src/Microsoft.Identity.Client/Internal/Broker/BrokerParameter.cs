// ------------------------------------------------------------------------------
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
// ------------------------------------------------------------------------------

namespace Microsoft.Identity.Client.Internal.Broker
{
    internal class BrokerParameter
    {
        public const string Authority = "authority";
        public const string ClientId = "client_id";
        public const string RequestScopes = "request_scopes";
        public const string ExtraOidcScopes = "extra_oidc_scopes";
        public const string OidcScopesValue = "openid offline_access profile";
        public const string RedirectUri = "redirect_uri";
        public const string BrokerKey = "broker_key";
        public const string ClientVersion = "client_version";
        public const string MsgProtocolVersion = "msg_protocol_ver";

        // not required
        public const string CorrelationId = "correlation_id";
        public const string ExtraQp = "extra_qp";
        public const string HomeAccountId = "home_account_id";
        public const string Username = "username";
        public const string LoginHint = "login_hint";
        public const string IntuneEnrollmentIds = "intune_enrollment_ids";
        public const string IntuneMamResource = "intune_mam_resource";
        public const string ClientCapabilities = "client_capabilities";
        public const string ClientAppName = "client_app_name";
        public const string ClientAppVersion = "client_app_version";
        public const string Claims = "claims";
        public const string ExtraConsentScopes = "extra_consent_scopes";
        public const string Prompt = "prompt";

        public const string Force = "force";
        
        public const string SilentBrokerFlow = "silent_broker_flow";
        public const string BrokerInstallUrl = "broker_install_url";
    }
}