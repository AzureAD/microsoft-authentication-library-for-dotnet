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
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Test.ADAL.Common
{

    public class Sts
    {
        public const string InvalidArgumentError = "invalid_argument";
        public const string InvalidRequest = "invalid_request";
        public const string InvalidResourceError = "invalid_resource";
        public const string InvalidClientError = "invalid_client";
        public const string AuthenticationFailedError = "authentication_failed";
        public const string AuthenticationUiFailedError = "authentication_ui_failed";
        public const string AuthenticationCanceledError = "authentication_canceled";
        public const string AuthorityNotInValidList = "authority_not_in_valid_list";
        public const string InvalidAuthorityType = "invalid_authority_type";
        public const string UnauthorizedClient = "unauthorized_client";
        public const string UserInteractionRequired = "user_interaction_required";

        public Sts()
        {
            this.ValidDefaultRedirectUri = new Uri("https://non_existing_uri.com/");
            this.InvalidExistingRedirectUri = new Uri("https://skydrive.live.com/");
            this.InvalidNonExistingRedirectUri = new Uri("https://invalid_non_existing_uri.com/");
            this.ConfidentialClientCertificateName = "valid_cert.pfx";
            this.InvalidConfidentialClientCertificateName = "invalid_cert.pfx";
            this.ConfidentialClientCertificatePassword = "password";
            this.InvalidConfidentialClientCertificatePassword = "password";
        }

        public bool ValidateAuthority { get; protected set; }

        public string Authority { get; set; }

        public string TenantlessAuthority { get; protected set; }

        public string[] ValidScope { get; set; }

        public string[] ValidScope2 { get; protected set; }

        public string ValidClientId { get; set; }

        public string ValidClientIdWithExistingRedirectUri { get; protected set; }

        public string ValidConfidentialClientId { get; set; }

        public string ValidConfidentialClientSecret { get; set; }

        public string ValidWinRTClientId { get; protected set; }

        public long ValidExpiresIn { get; protected set; }

        public Uri ValidExistingRedirectUri { get; set; }

        public string ValidLoggedInFederatedUserId { get; protected set; }

        public string ValidLoggedInFederatedUserName { get; protected set; }

        public Uri ValidNonExistingRedirectUri { get; set; }

        public Uri ValidDefaultRedirectUri { get; set; }

        public Uri ValidRedirectUriForConfidentialClient { get; set; }

        public string ValidUserName { get; set; }

        public string ValidUserName2 { get; protected set; }

        public string ValidUserName3 { get; protected set; }

        public string ValidPassword { get; set; }

        public string ValidPassword2 { get; set; }

        public string ValidPassword3 { get; set; }

        public string InvalidResource { get; protected set; }

        public string InvalidClientId { get; protected set; }

        public string InvalidAuthority { get; protected set; }

        public Uri InvalidExistingRedirectUri { get; set; }

        public Uri InvalidNonExistingRedirectUri { get; set; }

        public string ConfidentialClientCertificateName { get; set; }

        public string InvalidConfidentialClientCertificateName { get; set; }

        public string ConfidentialClientCertificatePassword { get; set; }

        public string InvalidConfidentialClientCertificatePassword { get; set; }

        public string InvalidUserName
        {
            get { return this.ValidUserName + "x"; }
        }

        public string ValidNonExistentRedirectUriClientId { get; set; }

        public string MsaUserName { get; protected set; }
        public string MsaPassword { get; protected set; }
    }
    
}