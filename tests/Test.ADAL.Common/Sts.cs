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
using System.Globalization;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Test.ADAL.Common
{
    public enum StsType
    {
        Unknown,
        ADFS,
        AAD,
        AADFederatedWithADFS3,
        AdfsPasswordGrant,
        AadPasswordGrant
    }

    public static class StsFactory
    {
        public static Sts CreateSts(StsType stsType)
        {
            Sts sts;
            switch (stsType)
            {
                case StsType.ADFS:
                    sts = new AdfsSts();
                    break;
                case StsType.AADFederatedWithADFS3:
                    sts = new AadFederatedWithAdfs3Sts();
                    break;
                case StsType.AAD:
                    sts = new AadSts();
                    break;
                case StsType.AdfsPasswordGrant:
                    sts = new AdfsPasswordGrantSts();
                    break;
                case StsType.AadPasswordGrant:
                    sts = new AadPasswordGrantSts();
                    break;
                default:
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, " Unsupported STS type '{0}'", stsType));
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

        public string Authority { get; set; }

        public string TenantlessAuthority { get; protected set; }

        public string ValidResource { get; set; }

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

        public string ValidUserName { get; set; }

        public UserIdentifier ValidUserId
        {
            get
            {
                return new UserIdentifier(ValidUserName, UserIdentifierType.OptionalDisplayableId);
            }
        }

        public string ValidUserName2 { get; protected set; }

        public string ValidUserName3 { get; protected set; }

        public UserIdentifier ValidRequiredUserId2
        {
            get
            {
                return new UserIdentifier(ValidUserName2, UserIdentifierType.RequiredDisplayableId);
            }
        }

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

        public UserIdentifier InvalidRequiredUserId
        {
            get
            {
                return new UserIdentifier(InvalidUserName, UserIdentifierType.RequiredDisplayableId);
            }
        }

        public string ValidNonExistentRedirectUriClientId { get; set; }

        public string MsaUserName { get; protected set; }
        public string MsaPassword { get; protected set; }
    }
    
    class AadPasswordGrantSts : Sts
    {
        public AadPasswordGrantSts()
        {
            this.ValidateAuthority = true;
            
            this.TenantlessAuthority = "https://login.windows.net/common";
            this.Authority = this.TenantlessAuthority;
            this.Type = StsType.AAD;
            
            this.ValidClientId = "1950a258-227b-4e31-a9cf-717495945fc2";
            this.ValidUserName = "<REPLACE>";
            this.ValidPassword = "<REPLACE>";
            this.ValidResource = "https://graph.windows.net";
        }
    }
    
    class AdfsPasswordGrantSts : Sts
    {
        public AdfsPasswordGrantSts()
        {
            this.Authority = "https://identity.contoso.com/adfs/ls";
            this.Type = StsType.ADFS;
            this.ValidateAuthority = false;
            this.ValidClientId = "oic.resource.owner.flow";
            this.ValidDefaultRedirectUri = new Uri("oic://resource-owner/flow");
            this.ValidUserName = @"somedomain\username";
            this.ValidPassword = "Password123";
            this.ValidResource = "https://management.core.contoso.com/";
        }
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
            this.ValidNonExistingRedirectUri = new Uri("http://non-existant-uri.com");
            this.ValidLoggedInFederatedUserName = "dummy\\dummy";
            string[] segments = this.ValidLoggedInFederatedUserName.Split(new[] { '\\' });
            this.ValidLoggedInFederatedUserId = string.Format(CultureInfo.CurrentCulture, " {0}@microsoft.com", (segments.Length == 2) ? segments[1] : segments[0]);

            this.TenantName = "aadadfs.onmicrosoft.com";
            this.Authority = string.Format(CultureInfo.CurrentCulture, "https://login.windows.net/{0}/", this.TenantName);
            this.TenantlessAuthority = "https://login.windows.net/Common";
            this.Type = StsType.AAD;
            this.ValidClientId = "4b8d1b32-ee16-4b30-9b5d-e374c43deb31";
            this.ValidNonExistentRedirectUriClientId = this.ValidClientId;
            this.ValidClientIdWithExistingRedirectUri = this.ValidClientId;
            this.ValidConfidentialClientId = "91ce6b56-776c-4e07-83c3-ebbb11726999";
            this.ValidConfidentialClientSecret = "<REPLACE>";
            this.ValidWinRTClientId = "786067bc-40cc-4171-be40-a73b2d05a461";
            this.ValidUserName = @"adaltest@aadadfs.onmicrosoft.com";
            this.ValidUserName2 = "adaltest2@aadadfs.onmicrosoft.com";
            this.ValidUserName3 = "adaltest3@aadadfs.onmicrosoft.com";
            this.ValidDefaultRedirectUri = new Uri("https://login.live.com/");
            this.ValidExistingRedirectUri = new Uri("https://login.live.com/");
            this.ValidRedirectUriForConfidentialClient = new Uri("https://confidentialclient.com");
            this.ValidPassword = "<REPLACE>";
            this.ValidPassword2 = "<REPLACE>";
            this.ValidPassword3 = "<REPLACE>";
            this.ValidResource = "http://testwebapp.com";
            this.ValidResource2 = "http://testwebapp2.com";
            this.ValidResource3 = "http://testwebapp3.com";

            this.MsaUserName = "adaltest@outlook.com";
            this.MsaPassword = "<REPLACE>";
        }

        public string TenantName { get; protected set; }
    }

    class AadFederatedWithAdfs3Sts : AadSts
    {
        public AadFederatedWithAdfs3Sts()
        {
            this.Type = StsType.AADFederatedWithADFS3;
            this.ValidNonExistentRedirectUriClientId = null;
            this.ValidClientIdWithExistingRedirectUri = this.ValidClientId;
            this.ValidExpiresIn = 25000;
            this.ValidConfidentialClientId = this.ValidClientId;
            this.ValidRedirectUriForConfidentialClient = this.ValidExistingRedirectUri;

            this.ValidUserName = "testuser1@aadadfs.info";
            this.ValidPassword = "<REPLACE>";
        }

        public string ValidFederatedUserName { get; protected set; }
        public string ValidFederatedPassword { get; protected set; }
    }

    class AdfsSts : Sts
    {
        public AdfsSts()
        {
            this.Authority = "https://aadadfs.info/adfs";
            this.InvalidAuthority = "https://invalid_address.com/adfs";
            this.InvalidClientId = "DE25CE3A-B772-4E6A-B431-96DCB5E7E558";
            this.InvalidResource = "urn:msft:ad:test:oauth:teamdashboardx";
            this.ValidConfidentialClientSecret = "client_secret";
            this.Type = StsType.ADFS;
            this.ValidateAuthority = false;
            this.ValidClientId = "DE25CE3A-B772-4E6A-B431-96DCB5E7E549";
            this.ValidNonExistentRedirectUriClientId = "58703C56-D485-4FFD-8A9E-9917C18BC8C0";
            this.ValidClientIdWithExistingRedirectUri = this.ValidClientId;
            this.ValidExistingRedirectUri = new Uri("https://login.live.com/");
            this.InvalidExistingRedirectUri = new Uri("https://skydrive.live.com/");
            this.ValidNonExistingRedirectUri = new Uri("https://non_existing_uri.com/");
            this.ValidDefaultRedirectUri = new Uri("https://login.live.com/");
            this.ValidExpiresIn = 3600;
            this.ValidUserName = @"aadadfs.info\testuser1";
            this.ValidConfidentialClientId = this.ValidClientId;
            this.ValidRedirectUriForConfidentialClient = this.ValidExistingRedirectUri;
            this.ValidPassword = "<REPLACE>";
            this.ValidResource = "urn:msft:ad:test:oauth:test";
            this.ValidResource2 = "urn:msft:ad:test:oauth:test2";
        }
    }
}
