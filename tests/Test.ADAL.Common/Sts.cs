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
    public enum StsType
    {
        Unknown,
        ADFS,
        AAD,
        AADFederatedWithADFS3
    }

    public static class StsFactory
    {
        public static Sts CreateSts(StsType stsType)
        {
            Sts sts = null;
            if (stsType == StsType.ADFS)
            {
                sts = new AdfsSts();
            }
            else if (stsType == StsType.AADFederatedWithADFS3)
            {
                sts = new AadFederatedWithAdfs3Sts();
            }
            else if (stsType == StsType.AAD)
            {
                sts = new AadSts();
            }
            else
            {
                throw new ArgumentException(string.Format("Unsupported STS type '{0}'", stsType));
            }

            return sts;
        }
    }

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
            this.Type = StsType.Unknown;
            this.ValidDefaultRedirectUri = new Uri("https://non_existing_uri.com/");
            this.InvalidExistingRedirectUri = new Uri("https://skydrive.live.com/");
            this.InvalidNonExistingRedirectUri = new Uri("https://invalid_non_existing_uri.com/");
            this.ConfidentialClientCertificateName = "valid_cert.pfx";
            this.InvalidConfidentialClientCertificateName = "invalid_cert.pfx";
            this.ConfidentialClientCertificatePassword = "password";
            this.InvalidConfidentialClientCertificatePassword = "password";
        }

        public StsType Type { get; protected set; }

        public bool ValidateAuthority { get; protected set; }

        public string Authority { get; protected set; }

        public string TenantlessAuthority { get; protected set; }

        public string ValidResource { get; protected set; }

        public string ValidResource2 { get; protected set; }

        public string ValidResource3 { get; protected set; }

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

        public string ValidUserName { get; protected set; }

        public UserIdentifier ValidUserId
        {
            get
            {
                return new UserIdentifier(ValidUserName, UserIdentifierType.OptionalDisplayableId);
            }
        }

        public string ValidUserName2 { get; protected set; }

        public UserIdentifier ValidRequiredUserId2
        {
            get
            {
                return new UserIdentifier(ValidUserName2, UserIdentifierType.RequiredDisplayableId);
            }
        }

        public string ValidPassword { get; set; }

        public string ValidPassword2 { get; set; }

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

        public UserIdentifier InvalidRequiredUserId
        {
            get
            {
                return new UserIdentifier(InvalidUserName, UserIdentifierType.RequiredDisplayableId);
            }
        }

        public string ValidNonExistentRedirectUriClientId { get; set; }
    }

    class AadSts : Sts
    {
        public AadSts()
        {
            this.InvalidAuthority = "https://invalid_address.com/path";
            this.InvalidClientId = "87002806-c87a-41cd-896b-84ca5690d29e";
            this.InvalidResource = "00000003-0000-0ff1-ce00-000000000001";
            this.ValidateAuthority = true;
            this.ValidExistingRedirectUri = new Uri("https://login.live.com/");
            this.ValidExpiresIn = 28800;
            this.ValidNonExistingRedirectUri = new Uri("https://non_existing_uri.com/");
            this.ValidLoggedInFederatedUserName = "dummy\\dummy";
            string[] segments = this.ValidLoggedInFederatedUserName.Split(new char[] { '\\' });
            this.ValidLoggedInFederatedUserId = string.Format("{0}@microsoft.com", (segments.Length == 2) ? segments[1] : segments[0]);

            this.TenantName = "aaltests.onmicrosoft.com";
            this.Authority = string.Format("https://login.windows.net/{0}", this.TenantName);
            this.TenantlessAuthority = "https://login.windows.net/Common";
            this.Type = StsType.AAD;
            this.ValidClientId = "e70b115e-ac0a-4823-85da-8f4b7b4f00e6";    // Test Client App2
            this.ValidNonExistentRedirectUriClientId = this.ValidClientId;
            this.ValidClientIdWithExistingRedirectUri = "5c0986db-8d89-4442-b5f9-d281efae9bad";
            this.ValidConfidentialClientId = "9083ccb8-8a46-43e7-8439-1d696df984ae";
            this.ValidConfidentialClientSecret = "n+ZC/7zWCv7JDA+QsujTChJSC/ppt0iWXBFYSsaU+Ws=";
            this.ValidWinRTClientId = "786067bc-40cc-4171-be40-a73b2d05a461";
            this.ValidUserName = "admin@aaltests.onmicrosoft.com";
            this.ValidUserName2 = "user@aaltests.onmicrosoft.com";
            this.ValidDefaultRedirectUri = new Uri("https://non_existing_uri.com/");
            this.ValidExistingRedirectUri = new Uri("https://login.live.com/");
            this.ValidRedirectUriForConfidentialClient = new Uri("https://non_existing_uri_for_confidential_client.com/");
            this.ValidPassword = "<REPLACE>";
            this.ValidPassword2 = "<REPLACE>";
            this.ValidResource = "b7a671d8-a408-42ff-86e0-aaf447fd17c4";
            this.ValidResource2 = "4848e7b1-7a6e-450e-aedb-31fd3c196db4";
            this.ValidResource3 = "3e5e5728-f57e-4d7f-b4be-87d0bdc39900";

            this.MsaUserName = "aaltests@outlook.com";
            this.MsaPassword = "<REPLACE>";
        }

        public string TenantName { get; protected set; }
        public string MsaUserName { get; protected set; }        
        public string MsaPassword { get; protected set; }
    }

    class AdfsSts : Sts
    {
        public AdfsSts()
        {
            this.Authority = "https://fs.bahush.info/adfs";
            this.InvalidAuthority = "https://invalid_address.com/adfs";
            this.InvalidClientId = "DE25CE3A-B772-4E6A-B431-96DCB5E7E558";
            this.InvalidResource = "urn:msft:ad:test:oauth:teamdashboardx";
            this.ValidConfidentialClientSecret = "client_secret";
            this.Type = StsType.ADFS;
            this.ValidateAuthority = false;
            this.ValidClientId = "DE25CE3A-B772-4E6A-B431-96DCB5E7E559";
            this.ValidNonExistentRedirectUriClientId = "58703C56-D485-4FFD-8A9E-9917C18BC8C0";
            this.ValidClientIdWithExistingRedirectUri = "DE25CE3A-B772-4E6A-B431-96DCB5E7E559";
            this.ValidExistingRedirectUri = new Uri("https://login.live.com/");
            this.InvalidExistingRedirectUri = new Uri("https://skydrive.live.com/");
            this.ValidNonExistingRedirectUri = new Uri("https://non_existing_uri.com/");
            this.ValidDefaultRedirectUri = new Uri("https://login.live.com/");
            this.ValidExpiresIn = 3600;
            this.ValidUserName = @"bahush.info\test";
            this.ValidConfidentialClientId = this.ValidClientId;
            this.ValidRedirectUriForConfidentialClient = this.ValidExistingRedirectUri;
            this.ValidPassword = "<REPLACE>";
            this.ValidResource = "urn:msft:ad:test:oauth:test";
            this.ValidResource2 = "urn:msft:ad:test:oauth:Service2";
        }
    }

    class AadFederatedWithAdfs3Sts : AadSts
    {
        public AadFederatedWithAdfs3Sts()
        {
            this.TenantName = "aaltests.onmicrosoft.com";
            this.Authority = string.Format("https://login.windows.net/{0}", this.TenantName);
            this.Type = StsType.AADFederatedWithADFS3;
            this.ValidClientId = "e70b115e-ac0a-4823-85da-8f4b7b4f00e6";
            this.ValidNonExistentRedirectUriClientId = null;
            this.ValidClientIdWithExistingRedirectUri = this.ValidClientId;
            this.ValidExistingRedirectUri = new Uri("https://login.live.com/");
            this.ValidNonExistingRedirectUri = new Uri("https://DontApply.com");
            this.ValidDefaultRedirectUri = new Uri("https://login.live.com/");
            this.ValidExpiresIn = 25000;
            this.ValidUserName = @"test@bahush.info";
            this.ValidConfidentialClientId = this.ValidClientId;
            this.ValidRedirectUriForConfidentialClient = this.ValidExistingRedirectUri;
            this.ValidPassword = "<REPLACE>";
            this.ValidResource = "b7a671d8-a408-42ff-86e0-aaf447fd17c4";
        }
    }
}
