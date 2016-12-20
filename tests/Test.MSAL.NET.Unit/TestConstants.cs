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
using Microsoft.Identity.Client;

namespace Test.MSAL.NET.Unit
{
    class TestConstants
    {
        public static readonly SortedSet<string> DefaultScope = new SortedSet<string>(new[] {"r1/scope1", "r1/scope2"});
        public static readonly SortedSet<string> ScopeForAnotherResource = new SortedSet<string>(new[] { "r2/scope1", "r2/scope2" });
        public static readonly string DefaultAuthorityHomeTenant = "https://login.microsoftonline.com/home/";
        public static readonly string DefaultAuthorityGuestTenant = "https://login.microsoftonline.com/guest/";
        public static readonly string DefaultAuthorityCommonTenant = "https://login.microsoftonline.com/common/";
        public static readonly string DefaultClientId = "client_id";
        public static readonly string DefaultUniqueId = "unique_id";
        public static readonly string DefaultDisplayableId = "displayable@id.com";
        public static readonly string DefaultHomeObjectId = "home_oid";
        public static readonly string DefaultPolicy = "policy";
        public static readonly string DefaultRedirectUri = "urn:ietf:wg:oauth:2.0:oob";
        public static readonly bool DefaultRestrictToSingleUser = false;
        public static readonly string DefaultClientSecret = "client_secret";

        public static readonly User DefaultUser = new User
        {
            UniqueId = DefaultUniqueId,
            DisplayableId = DefaultDisplayableId,
            HomeObjectId = DefaultHomeObjectId
        };
    }
}
