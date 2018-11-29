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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Microsoft.Identity.LabInfrastructure
{
    //TODO: add a layer of user and password caching to speed up the tests
    public static class LabUserHelper
    {
        static LabServiceApi _labService;
        static KeyVaultSecretsProvider _keyVaultSecretsProvider;
        static LabResponse _defaultLabResponse;


        static LabUserHelper()
        {
            _keyVaultSecretsProvider = new KeyVaultSecretsProvider();
            _labService = new LabServiceApi(_keyVaultSecretsProvider);
        }

        public static UserQueryParameters DefaultUserQuery => new UserQueryParameters
        {
            IsMamUser = false,
            IsMfaUser = false,
            IsFederatedUser = false
        };

        public static UserQueryParameters B2CLocalAccountUserQuery => new UserQueryParameters
        {
            UserType = UserType.B2C,
            B2CIdentityProvider = B2CIdentityProvider.Local
        };

        public static UserQueryParameters B2CFacebookUserQuery => new UserQueryParameters
        {
            UserType = UserType.B2C,
            B2CIdentityProvider = B2CIdentityProvider.Facebook
        };

        public static UserQueryParameters B2CGoogleUserQuery => new UserQueryParameters
        {
            UserType = UserType.B2C,
            B2CIdentityProvider = B2CIdentityProvider.Google
        };

        public static LabResponse GetLabUserData(UserQueryParameters query)
        {
            var user = _labService.GetLabResponse(query);
            if (user == null)
            {
                throw new LabUserNotFoundException(query, "Found no users for the given query.");
            }
            return user;
        }

        public static LabResponse GetDefaultUser()
        {
            if (_defaultLabResponse == null)
            {
                _defaultLabResponse = GetLabUserData(DefaultUserQuery);
            }

            return _defaultLabResponse;
        }

        public static LabResponse GetB2CLocalAccount()
        {
            var user = B2CLocalAccountUserQuery;
            return GetLabUserData(user);
        }

        public static LabResponse GetB2CFacebookAccount()
        {
            var user = B2CFacebookUserQuery;
            return GetLabUserData(user);
        }

        public static LabResponse GetB2CGoogleAccount()
        {
            var user = B2CGoogleUserQuery;
            return GetLabUserData(user);
        }

        public static LabResponse GetAdfsUser(FederationProvider federationProvider, bool federated = true)
        {
            var user = DefaultUserQuery;
            user.FederationProvider = federationProvider;
            user.IsFederatedUser = true;
            user.IsFederatedUser = federated;
            return GetLabUserData(user);
        }

        public static string GetUserPassword(LabUser user)
        {
            if (string.IsNullOrWhiteSpace(user.CredentialUrl))
            {
                throw new InvalidOperationException("Error: CredentialUrl is not set on user. Password retrieval failed.");
            }

            if (_keyVaultSecretsProvider == null)
            {
                throw new InvalidOperationException("Error: Keyvault secrets provider is not set");
            }

            try
            {
                var secret = _keyVaultSecretsProvider.GetSecret(user.CredentialUrl);
                return secret.Value;
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Test setup: cannot get the user password. See inner exception.", e);
            }
        }
    }
}
