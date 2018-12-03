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

namespace Microsoft.Identity.Client.CacheV2.Schema
{
    internal static class StorageJsonKeys
    {
        public const string HomeAccountId = "home_account_id";
        public const string Environment = "environment";
        public const string Realm = "realm";
        public const string LocalAccountId = "local_account_id";
        public const string Username = "username";
        public const string AuthorityType = "authority_type";
        public const string AlternativeAccountId = "alternative_account_id";
        public const string GivenName = "given_name";
        public const string FamilyName = "family_name";
        public const string MiddleName = "middle_name";
        public const string Name = "name";
        public const string AvatarUrl = "avatar_url";
        public const string CredentialType = "credential_type";
        public const string ClientId = "client_id";
        public const string Secret = "secret";
        public const string Target = "target";
        public const string CachedAt = "cached_at";
        public const string ExpiresOn = "expires_on";
        public const string ExtendedExpiresOn = "extended_expires_on";
        public const string ClientInfo = "client_info";
        public const string FamilyId = "family_id";
    }
}