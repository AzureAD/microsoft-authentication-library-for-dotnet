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

using System.Collections.Generic;
using System.Globalization;
using Microsoft.Identity.Core.Helpers;

namespace Test.Microsoft.Identity.Core.Unit
{
    class CoreTestConstants
    {
        public static readonly string ScopeStr = "scope1 scope2";
        public static readonly string ScopeForAnotherResourceStr = "scope3 scope4";
        public static readonly SortedSet<string> Scope = new SortedSet<string>(new[] {"r1/scope1", "r1/scope2"});
        public static readonly SortedSet<string> ScopeForAnotherResource = new SortedSet<string>(new[] { "r2/scope1", "r2/scope2" });
        public static readonly string ProductionPrefNetworkEnvironment = "login.microsoftonline.com";
        public static readonly string ProductionPrefCacheEnvironment = "login.windows.net";
        public static readonly string SovereignEnvironment = "login.microsoftonline.de";
        public static readonly string AuthorityHomeTenant = "https://" + ProductionPrefNetworkEnvironment + "/home/";
        public static readonly string AuthorityGuestTenant = "https://" + ProductionPrefNetworkEnvironment + "/guest/";
        public static readonly string AuthorityCommonTenant = "https://" + ProductionPrefNetworkEnvironment + "/common/";
        public static readonly string PrefCacheAuthorityCommonTenant = "https://" + ProductionPrefCacheEnvironment + "/common/";
        public static readonly string AuthorityOrganizationsTenant = "https://" + ProductionPrefNetworkEnvironment + "/organizations/";
        public const string B2CAuthority = "https://login.microsoftonline.in/tfp/tenant/policy/";
        public const string B2CLoginAuthority = "https://sometenantid.b2clogin.com/tfp/sometenantid/policy/";
        public const string B2CAuthorityNotTrustedHost = "https://sometenantid.b2blogin.com/tfp/sometenantid/policy/";
        public static readonly string ClientId = "client_id";
        public static readonly string UniqueId = "unique_id";
        public static readonly string IdentityProvider = "my-idp";
        public static readonly string Name = "First Last";
        public static readonly string DisplayableId = "displayable@id.com";
        public static readonly string RedirectUri = "urn:ietf:wg:oauth:2.0:oob";
        public static readonly string ClientSecret = "client_secret";
        public static readonly string Uid = "my-uid";
        public static readonly string Utid= "my-utid";
        public static readonly string DiscoveryEndPoint = "discovery/instance";

        public static readonly string UserIdentifier = CreateUserIdentifer();

        public static string CreateUserIdentifer()
        {
            return CreateUserIdentifer(Uid, Utid);
        }

        public static string GetDiscoveryEndpoint(string authority)
        {
            return authority + DiscoveryEndPoint;
        }

        public static string CreateUserIdentifer(string uid, string utid)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}.{1}",
                Base64UrlHelpers.Encode(uid),
                Base64UrlHelpers.Encode(utid));
        }

        public static readonly string OnPremiseAuthority = "https://fs.contoso.com/adfs/";
        public static readonly string OnPremiseClientId = "on_premise_client_id";
        public static readonly string OnPremiseUniqueId = "on_premise_unique_id";
        public static readonly string OnPremiseDisplayableId = "displayable@contoso.com";
        public static readonly string FabrikamDisplayableId = "displayable@fabrikam.com";
        public static readonly string OnPremiseHomeObjectId = OnPremiseUniqueId;
        public static readonly string OnPremisePolicy = "on_premise_policy";
        public static readonly string OnPremiseRedirectUri = "urn:ietf:wg:oauth:2.0:oob";
        public static readonly string OnPremiseClientSecret = "on_premise_client_secret";
        public static readonly string OnPremiseUid = "my-OnPremise-UID";
        public static readonly string OnPremiseUtid = "my-OnPremise-UTID";
    }
}
