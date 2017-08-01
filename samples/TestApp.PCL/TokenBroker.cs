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

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

using Test.ADAL.Common;

namespace TestApp.PCL
{
    public class MobileAppSts : Sts
    {
        public MobileAppSts()
        {
            this.InvalidAuthority = "https://invalid_address.com/path";
            this.InvalidClientId = "87002806-c87a-41cd-896b-84ca5690d29e";
            this.InvalidResource = "00000003-0000-0ff1-ce00-000000000001";
            this.ValidateAuthority = true;
            this.ValidExistingRedirectUri = new Uri("https://login.live.com/");
            this.ValidExpiresIn = 28800;
            this.ValidNonExistingRedirectUri = new Uri("urn:ietf:wg:oauth:2.0:oob");
            this.ValidLoggedInFederatedUserName = "dummy\\dummy";
            string[] segments = this.ValidLoggedInFederatedUserName.Split(new[] { '\\' });
            this.ValidLoggedInFederatedUserId = string.Format(CultureInfo.CurrentCulture, "{0}@microsoft.com", (segments.Length == 2) ? segments[1] : segments[0]);

            this.TenantName = "<REPLACE>";
            this.Authority = string.Format(CultureInfo.CurrentCulture, " https://login.windows.net/{0}", this.TenantName);
            this.TenantlessAuthority = "https://login.windows.net/Common";
            this.Type = StsType.AAD;
            this.ValidClientId = "dd9caee2-38bd-484e-998c-7529bdef220f";
            this.ValidNonExistentRedirectUriClientId = this.ValidClientId;
            this.ValidClientIdWithExistingRedirectUri = this.ValidClientId;
            this.ValidUserName = @"<REPLACE>";
            this.ValidDefaultRedirectUri = new Uri("https://login.live.com/");
            this.ValidExistingRedirectUri = new Uri("https://login.live.com/");
            this.ValidRedirectUriForConfidentialClient = new Uri("https://confidentialclient.com");
            this.ValidPassword = "<REPLACE>";
            this.ValidResource = "https://graph.windows.net";

            this.MsaUserName = "<REPLACE>";
            this.MsaPassword = "<REPLACE>";
        }

        public string TenantName { get; protected set; }
    }

    public class TokenBroker
    {

        private Sts sts = new AadSts();

        public Sts Sts
        {
            get { return this.sts; }
            set { this.sts = value; }
        }

        UserIdentifier GetUserIdentifier()
        {
            if (Sts.ValidUserName != null)
            {
                return new UserIdentifier(Sts.ValidUserName, UserIdentifierType.OptionalDisplayableId);
            }
            return UserIdentifier.AnyUser;
        }

        public async Task<string> GetTokenSilentAsync(IPlatformParameters parameters)
        {
            try
            {
                AuthenticationContext context = new AuthenticationContext(Sts.Authority, true);
                var result = await context.AcquireTokenSilentAsync(Sts.ValidResource, Sts.ValidClientId, GetUserIdentifier(), parameters);
                return result.AccessToken;
            }
            catch (Exception ex)
            {
                return GetErrorMessage(ex);
            }
        }

        public async Task<string> GetTokenInteractiveAsync(IPlatformParameters parameters)
        {
            try
            {
                AuthenticationContext context = new AuthenticationContext(Sts.Authority, true);
                var result = await context.AcquireTokenAsync(Sts.ValidResource, Sts.ValidClientId, Sts.ValidNonExistingRedirectUri, parameters, GetUserIdentifier());
                return result.AccessToken;
            }
            catch (Exception ex)
            {
                return GetErrorMessage(ex);
            }
        }

        public async Task<string> GetTokenInteractiveAsync(IPlatformParameters parameters, string extraQueryParameters, string claims)
        {
            try
            {
                AuthenticationContext context = new AuthenticationContext(Sts.Authority, true);
                var result = await context.AcquireTokenAsync(Sts.ValidResource, Sts.ValidClientId, Sts.ValidNonExistingRedirectUri, parameters,
           GetUserIdentifier(), extraQueryParameters, claims);
                return result.AccessToken;
            }
            catch (Exception ex)
            {
                return GetErrorMessage(ex);
            }
        }


        public async Task<string> GetTokenInteractiveWithMsAppAsync(IPlatformParameters parameters)
        {
            try
            {
                AuthenticationContext context = new AuthenticationContext(Sts.Authority, true);
                var result = await context.AcquireTokenAsync(Sts.ValidResource, Sts.ValidClientId, null, parameters, GetUserIdentifier());

                return result.AccessToken;
            }
            catch (Exception ex)
            {
                return GetErrorMessage(ex);
            }
        }

        public async Task<string> GetTokenWithClientCredentialAsync()
        {
            try
            {
                AuthenticationContext context = new AuthenticationContext(Sts.Authority, true);
                var result = await context.AcquireTokenAsync(Sts.ValidResource, new ClientCredential(Sts.ValidConfidentialClientId, Sts.ValidConfidentialClientSecret));

                return result.AccessToken;
            }
            catch (Exception ex)
            {
                return GetErrorMessage(ex);
            }
        }

        public void ClearTokenCache()
        {
            TokenCache.DefaultShared.Clear();
        }

        private static string GetErrorMessage(Exception ex)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(ex.Message);

            Exception current = ex;
            while (current.InnerException != null)
            {
                current = current.InnerException;
                sb.AppendLine();
                sb.AppendLine();
                sb.AppendFormat(CultureInfo.CurrentCulture, "Inner exception: {0}", current.Message);
            }

            sb.AppendLine();
            sb.AppendLine();
            sb.AppendFormat(CultureInfo.CurrentCulture, "Stack trace: {0}", current.StackTrace);

            return sb.ToString();
        }
    }
}