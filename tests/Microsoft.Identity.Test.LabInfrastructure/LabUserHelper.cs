//------------------------------------------------------------------------------
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
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Identity.Test.LabInfrastructure
{
    public static class LabUserHelper
    {
        private static readonly LabServiceApi s_labService;
        private static readonly KeyVaultSecretsProvider s_keyVaultSecretsProvider;
        private static readonly IDictionary<UserQuery, LabResponse> s_userCache =
            new Dictionary<UserQuery, LabResponse>();


        static LabUserHelper()
        {
            s_keyVaultSecretsProvider = new KeyVaultSecretsProvider();
            s_labService = new LabServiceApi(s_keyVaultSecretsProvider);
        }


        public static LabResponse GetLabUserData(UserQuery query)
        {
            if (s_userCache.ContainsKey(query))
            {
                Debug.WriteLine("User cache hit");
                return s_userCache[query];
            }

            var user = s_labService.GetLabResponse(query);
            if (user == null)
            {
                throw new LabUserNotFoundException(query, "Found no users for the given query.");
            }

            Debug.WriteLine("User cache miss");
            s_userCache.Add(query, user);

            return user;
        }

        public static LabResponse GetDefaultUser()
        {
            return GetLabUserData(UserQuery.DefaultUserQuery);
        }

        public static LabResponse GetB2CLocalAccount()
        {
            return GetLabUserData(UserQuery.B2CLocalAccountUserQuery);
        }

        public static LabResponse GetB2CFacebookAccount()
        {
            return GetLabUserData(UserQuery.B2CFacebookUserQuery);
        }

        public static LabResponse GetB2CGoogleAccount()
        {
            return GetLabUserData(UserQuery.B2CGoogleUserQuery);
        }

        public static LabResponse GetAdfsUser(FederationProvider federationProvider, bool federated = true)
        {
            var query = UserQuery.DefaultUserQuery;
            query.FederationProvider = federationProvider;
            query.IsFederatedUser = true;
            query.IsFederatedUser = federated;
            return GetLabUserData(query);
        }

        public static string GetUserPassword(LabUser user)
        {
            if (string.IsNullOrWhiteSpace(user.CredentialUrl))
            {
                throw new InvalidOperationException("Error: CredentialUrl is not set on user. Password retrieval failed.");
            }

            if (s_keyVaultSecretsProvider == null)
            {
                throw new InvalidOperationException("Error: Keyvault secrets provider is not set");
            }

            try
            {
                var secret = s_keyVaultSecretsProvider.GetSecret(user.CredentialUrl);
                return secret.Value;
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Test setup: cannot get the user password. See inner exception.", e);
            }
        }
    }
}
