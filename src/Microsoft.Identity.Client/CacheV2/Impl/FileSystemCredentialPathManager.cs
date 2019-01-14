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

using System;
using System.IO;
using Microsoft.Identity.Client.CacheV2.Impl.Utils;
using Microsoft.Identity.Client.CacheV2.Schema;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.PlatformsCommon.Factories;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

namespace Microsoft.Identity.Client.CacheV2.Impl
{
    internal class FileSystemCredentialPathManager : ICredentialPathManager
    {
        private const string UserPrefix = "u_";
        private const string EnvironmentPrefix = "e_";
        private const string RealmPrefix = "r_";
        private const string ClientIdPrefix = "c_";
        private const string FamilyIdPrefix = "f_";
        private const string UserDataFolder = "UD";
        private const string AccountsFolder = "Accounts";
        private const string IdTokensFolder = "ID";
        private const string AccessTokensFolder = "AT";
        private const string RefreshTokensFolder = "RT";
        private const string FamilyRefreshTokensFolder = "FRT";
        private const string AppMetadataFolder = "AppMetadata";
        private const string FileExtension = ".bin";

        private readonly ICryptographyManager _cryptographyManager;

        public FileSystemCredentialPathManager(ICryptographyManager cryptographyManager)
        {
            _cryptographyManager = cryptographyManager;
        }

        public string GetCredentialPath(Credential credential)
        {
            return GetCredentialPath(
                credential.HomeAccountId,
                credential.Environment,
                credential.Realm,
                credential.ClientId,
                credential.FamilyId,
                credential.CredentialType);
        }

        public string ToSafeFilename(string data)
        {
            string normalizedData = NormalizeKey(data);
            byte[] hash = _cryptographyManager.CreateSha256HashBytes(normalizedData);
            var sizedHash = new byte[10];
            Array.Copy(hash, sizedHash, 10);
            return Base32Hex.ToBase32String(sizedHash);
        }

        public string GetCredentialPath(
            string homeAccountId,
            string environment,
            string realm,
            string clientId,
            string familyId,
            CredentialType credentialType)
        {
            string path = GetCommonPathPrefix(homeAccountId, environment);

            // Family refresh tokens
            if (credentialType == CredentialType.OAuth2RefreshToken && !string.IsNullOrEmpty(familyId))
            {
                path = Path.Combine(path, FamilyRefreshTokensFolder, FamilyIdPrefix + ToSafeFilename(familyId) + FileExtension);
                return PathUtils.Normalize(path);
            }

            switch (credentialType)
            {
            case CredentialType.OAuth2AccessToken:
                path = Path.Combine(path, AccessTokensFolder);
                break;
            case CredentialType.OAuth2RefreshToken:
                path = Path.Combine(path, RefreshTokensFolder);
                break;
            case CredentialType.OidcIdToken:
                path = Path.Combine(path, IdTokensFolder);
                break;
            default:
                throw new ArgumentException("bad credential type"); // todo: storage exception
            }

            if (credentialType == CredentialType.OAuth2AccessToken || credentialType == CredentialType.OidcIdToken)
            {
                path = Path.Combine(path, RealmPrefix + ToSafeFilename(realm));
            }

            path = Path.Combine(path, ClientIdPrefix + ToSafeFilename(clientId) + FileExtension);
            return PathUtils.Normalize(path);
        }

        public string GetAppMetadataPath(string environment, string clientId)
        {
            string path = Path.Combine(
                AppMetadataFolder,
                EnvironmentPrefix + ToSafeFilename(environment),
                ClientIdPrefix + ToSafeFilename(clientId) + FileExtension);
            return PathUtils.Normalize(path);
        }

        public string GetAccountPath(Microsoft.Identity.Client.CacheV2.Schema.Account account)
        {
            return GetAccountPath(account.HomeAccountId, account.Environment, account.Realm);
        }

        public string GetAccountPath(string homeAccountId, string environment, string realm)
        {
            string path = Path.Combine(
                GetCommonPathPrefix(homeAccountId, environment),
                AccountsFolder,
                RealmPrefix + ToSafeFilename(realm) + FileExtension);
            return PathUtils.Normalize(path);
        }

        public string GetAppMetadataPath(AppMetadata appMetadata)
        {
            return GetAppMetadataPath(appMetadata.Environment, appMetadata.ClientId);
        }

        public string GetAccountsPath(string homeAccountId, string environment)
        {
            string path = Path.Combine(UserDataFolder, UserPrefix + ToSafeFilename(homeAccountId));
            if (!string.IsNullOrWhiteSpace(environment))
            {
                path = Path.Combine(EnvironmentPrefix + ToSafeFilename(environment));
            }

            return PathUtils.Normalize(path);
        }

        private string NormalizeKey(string data)
        {
            return data.ToLowerInvariant().Trim();
        }

        private string GetCommonPathPrefix(string homeAccountId, string environment)
        {
            string path = Path.Combine(
                UserDataFolder,
                UserPrefix + ToSafeFilename(homeAccountId),
                EnvironmentPrefix + ToSafeFilename(environment));
            return PathUtils.Normalize(path);
        }
    }
}