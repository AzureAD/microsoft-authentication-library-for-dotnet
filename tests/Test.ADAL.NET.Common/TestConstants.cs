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

namespace Test.ADAL.NET.Common
{
	public class TestConstants
    {
        public static readonly string DefaultResource = "resource1";
        public static readonly string AnotherResource = "resource2";
        public static readonly string DefaultAdfsAuthorityTenant = "https://login.contodo.com/adfs/";
        public static readonly string DefaultAuthorityHomeTenant = "https://login.microsoftonline.com/home/";
        public static readonly string SomeTenantId = "some-tenant-id";
        public static readonly string TenantSpecificAuthority = $"https://login.microsoftonline.com/{SomeTenantId}/";
        public static readonly string DefaultAuthorityGuestTenant = "https://login.microsoftonline.com/guest/";
        public static readonly string DefaultAuthorityCommonTenant = "https://login.microsoftonline.com/common/";
        public static readonly string DefaultClientId = "client_id";
        public static readonly string DefaultUniqueId = "unique_id";
        public static readonly string DefaultDisplayableId = "displayable@id.com";
        public static readonly Uri DefaultRedirectUri = new Uri("urn:ietf:wg:oauth:2.0:oob");
        public static readonly bool DefaultRestrictToSingleUser = false;
        public static readonly string DefaultClientSecret = "client_secret";
        public static readonly string DefaultPassword = "password";
        public static readonly bool DefaultExtendedLifeTimeEnabled = false;
        public static readonly bool PositiveExtendedLifeTimeEnabled = true;
        public static readonly string ErrorSubCode = "ErrorSubCode";
        public static readonly string CloudAudienceUrnMicrosoft = "urn:federation:MicrosoftOnline";
        public static readonly string CloudAudienceUrn = "urn:federation:Blackforest";
    }
	
    public static class StringValue
    {
        public const string NotProvided = "NotProvided";
        public const string NotReady = "Not Ready";
        public const string Null = "NULL";
    }

    public static class UserType
    {
        public const string NonFederated = "NonFederated";
        public const string Federated = "Federated";
    }

    public static class CacheType
    {
        public const string Default = "Default";
        public const string Null = "Null";
        public const string Constant = "Constant";
        public const string InMemory = "InMemory";
    }

    public enum ValidateAuthorityIndex
    {
        NotProvided = 0,
        Yes = 1,
        No = 2
    }

    public enum CacheTypeIndex
    {
        NotProvided = 0,
        Default = 1,
        Null = 2,
        InMemory = 3
    }
}
