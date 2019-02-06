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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.CacheV2.Impl.Utils;
using Microsoft.Identity.Client.CacheV2.Schema;
using Microsoft.Identity.Json;
using Microsoft.Identity.Json.Linq;

namespace Microsoft.Identity.Client.CacheV2.Impl
{
    internal class PathStorageWorker : IStorageWorker
    {
        private readonly ICredentialPathManager _credentialPathManager;
        private readonly ICachePathStorage _fileIo;

        public PathStorageWorker(ICachePathStorage fileIo, ICredentialPathManager credentialPathManager)
        {
            _fileIo = fileIo;
            _credentialPathManager = credentialPathManager;
        }

        public IEnumerable<Credential> ReadCredentials(
            string homeAccountId,
            string environment,
            string realm,
            string clientId,
            string familyId,
            string target,
            ISet<CredentialType> types)
        {
            var credentials = new List<Credential>();

            foreach (var credentialType in types)
            {
                switch (credentialType)
                {
                case CredentialType.OAuth2AccessToken:
                    ReadAccessTokens(
                        homeAccountId,
                        environment,
                        realm,
                        clientId,
                        target,
                        credentials);
                    break;
                case CredentialType.OAuth2RefreshToken:
                    ReadRefreshTokens(
                        homeAccountId,
                        environment,
                        clientId,
                        familyId,
                        credentials);
                    break;
                case CredentialType.OidcIdToken:
                    ReadIdTokens(
                        homeAccountId,
                        environment,
                        realm,
                        clientId,
                        credentials);
                    break;
                }
            }

            return credentials;
        }

        public void WriteCredentials(IEnumerable<Credential> credentials)
        {
            foreach (var credential in credentials)
            {
                var credentialJson = StorageJsonUtils.CredentialToJson(credential);
                string credentialPath = GetCredentialPath(credential);

                if (credential.CredentialType == CredentialType.OAuth2AccessToken)
                {
                    ReadModifyWrite(
                        credentialPath,
                        (fileContentJson) =>
                        {
                            AddAccessTokenWithScopes(fileContentJson, credentialJson);
                            return fileContentJson;
                        });
                }
                else
                {
                    ReadModifyWrite(
                        credentialPath,
                        (fileContentJson) =>
                        {
                            JsonMerge(credentialJson, fileContentJson);
                            return credentialJson;
                        });
                }
            }
        }

        public void DeleteCredentials(
            string homeAccountId,
            string environment,
            string realm,
            string clientId,
            string familyId,
            string target,
            ISet<CredentialType> types)
        {
            foreach (var type in types)
            {
                switch (type)
                {
                case CredentialType.OAuth2AccessToken:
                    DeleteAccessTokens(
                        homeAccountId,
                        environment,
                        realm,
                        clientId,
                        target);
                    break;
                case CredentialType.OAuth2RefreshToken:
                    DeleteRefreshTokens(homeAccountId, environment, clientId, familyId);
                    break;
                case CredentialType.OidcIdToken:
                    DeleteIdTokens(homeAccountId, environment, realm, clientId);
                    break;
                }
            }
        }

        public Microsoft.Identity.Client.CacheV2.Schema.Account ReadAccount(string homeAccountId, string environment, string realm)
        {
            var fileContentJson = Read(_credentialPathManager.GetAccountPath(homeAccountId, environment, realm));
            return fileContentJson.HasValues ? StorageJsonUtils.AccountFromJson(fileContentJson) : null;
        }

        public void WriteAccount(Microsoft.Identity.Client.CacheV2.Schema.Account account)
        {
            var accountJson = StorageJsonUtils.AccountToJson(account);
            string accountPath = _credentialPathManager.GetAccountPath(account);

            ReadModifyWrite(
                accountPath,
                fileContentJson =>
                {
                    JsonMerge(accountJson, fileContentJson);
                    return accountJson;
                });
        }

        public void DeleteAccount(string homeAccountId, string environment, string realm)
        {
            _fileIo.DeleteFile(_credentialPathManager.GetAccountPath(homeAccountId, environment, realm));
        }

        public void DeleteAccounts(string homeAccountId, string environment)
        {
            _fileIo.DeleteContent(_credentialPathManager.GetAccountsPath(homeAccountId, environment));
        }

        public AppMetadata ReadAppMetadata(string environment, string clientId)
        {
            string appMetadataPath = _credentialPathManager.GetAppMetadataPath(environment, clientId);
            var appMetadataJson = Read(appMetadataPath);
            return appMetadataJson.HasValues ? StorageJsonUtils.AppMetadataFromJson(appMetadataJson) : null;
        }

        public void WriteAppMetadata(AppMetadata appMetadata)
        {
            var appMetadataJson = StorageJsonUtils.AppMetadataToJson(appMetadata);
            string appMetadataPath = _credentialPathManager.GetAppMetadataPath(appMetadata);
            ReadModifyWrite(
                appMetadataPath,
                fileContent =>
                {
                    JsonMerge(appMetadataJson, fileContent);
                    return appMetadataJson;
                });
        }

        private void ReadRefreshTokens(
            string homeAccountId,
            string environment,
            string clientId,
            string familyId,
            List<Credential> credentials)
        {
            string credentialPath;

            // Read the RT
            if (!string.IsNullOrWhiteSpace(clientId))
            {
                credentialPath = _credentialPathManager.GetCredentialPath(
                    homeAccountId,
                    environment,
                    string.Empty,
                    clientId,
                    string.Empty,
                    CredentialType.OAuth2RefreshToken);
                ReadCredential(credentialPath, credentials);
            }

            // Read the FRT
            if (!string.IsNullOrWhiteSpace(familyId))
            {
                credentialPath = _credentialPathManager.GetCredentialPath(
                    homeAccountId,
                    environment,
                    string.Empty,
                    string.Empty,
                    familyId,
                    CredentialType.OAuth2RefreshToken);
                ReadCredential(credentialPath, credentials);
            }
        }

        private void ReadAccessTokens(
            string homeAccountId,
            string environment,
            string realm,
            string clientId,
            string target,
            List<Credential> credentials)
        {
            string credentialPath = _credentialPathManager.GetCredentialPath(
                homeAccountId,
                environment,
                realm,
                clientId,
                string.Empty,
                CredentialType.OAuth2AccessToken);
            var fileContentJson = Read(credentialPath);
            var accessToken = FindAccessTokenWithScopes(fileContentJson, target);
            if (accessToken != null)
            {
                credentials.Add(accessToken);
            }
        }

        private KeyValuePair<string, JToken>? FindAccessTokenIterWithScopes(JObject accessTokens, string target)
        {
            HashSet<string> requestedScopes = SplitAndNormalizeScopes(target);
            if (!requestedScopes.Any())
            {
                throw new InvalidOperationException("no access token scopes requested"); // todo: storage exception
            }

            foreach (KeyValuePair<string, JToken> token in accessTokens)
            {
                HashSet<string> currentTokenScopes = SplitAndNormalizeScopes(token.Key);
                if (requestedScopes.IsSubsetOf(currentTokenScopes))
                {
                    return token;
                }
            }

            return null;
        }

        public Credential FindAccessTokenWithScopes(JObject accessTokens, string target)
        {
            KeyValuePair<string, JToken>? kvp = FindAccessTokenIterWithScopes(accessTokens, target);
            if (kvp.HasValue)
            {
                return StorageJsonUtils.CredentialFromJson(JObject.Parse(kvp.Value.Value.ToString()));
            }

            return null;
        }

        internal HashSet<string> SplitAndNormalizeScopes(string target)
        {
            var scopes = ScopeUtils.SplitScopes(target);
            var normalizedScopes = new HashSet<string>();
            foreach (string scope in scopes)
            {
                normalizedScopes.Add(NormalizeKey(scope));
            }

            return normalizedScopes;
        }

        private void ReadIdTokens(
            string homeAccountId,
            string environment,
            string realm,
            string clientId,
            List<Credential> credentials)
        {
            string credentialPath = _credentialPathManager.GetCredentialPath(
                homeAccountId,
                environment,
                realm,
                clientId,
                string.Empty,
                CredentialType.OidcIdToken);
            ReadCredential(credentialPath, credentials);
        }

        private void ReadCredential(string relativePath, List<Credential> credentials)
        {
            var json = Read(relativePath);
            if (json != null && !json.IsEmpty())
            {
                credentials.Add(StorageJsonUtils.CredentialFromJson(json));
            }
        }

        public JObject Read(string relativePath)
        {
            byte[] fileContent = _fileIo.Read(relativePath);
            return fileContent.Length == 0 ? new JObject() : DecryptParse(fileContent, relativePath);
        }

        private JObject DecryptParse(byte[] fileContent, string relativePath)
        {
            if (fileContent == null || !fileContent.Any())
            {
                return new JObject();
            }

            try
            {
                byte[] decryptedFileContent = Decrypt(fileContent);
                return JObject.Parse(Encoding.UTF8.GetString(decryptedFileContent, 0, decryptedFileContent.Length));
            }
            catch (JsonReaderException)
            {
            }
            //catch (CryptoException)
            //{

            //}

            return new JObject();
        }

        private string NormalizeKey(string data)
        {
            return data.ToLowerInvariant().Trim();
        }

        internal string GetCredentialPath(Credential credential)
        {
            return _credentialPathManager.GetCredentialPath(
                credential.HomeAccountId,
                credential.Environment,
                credential.Realm,
                credential.ClientId,
                credential.FamilyId,
                credential.CredentialType);
        }

        public void AddAccessTokenWithScopes(JObject accessTokens, JObject tokenToAdd)
        {
            if (!tokenToAdd.ContainsKey(StorageJsonKeys.Target))
            {
                throw new ArgumentException("target field missing from access token", nameof(tokenToAdd));
            }

            string requestedTarget = tokenToAdd.GetValue(StorageJsonKeys.Target, StringComparison.Ordinal).ToObject<string>();
            HashSet<string> requestedScopes = SplitAndNormalizeScopes(requestedTarget);

            if (!requestedScopes.Any())
            {
                throw new InvalidOperationException("access token has no scopes");
            }

            var keysToRemove = new List<string>();

            foreach (KeyValuePair<string, JToken> token in accessTokens)
            {
                HashSet<string> currentTokenScopes = SplitAndNormalizeScopes(token.Key);
                if (HashSetsAreIntersecting(requestedScopes, currentTokenScopes))
                {
                    keysToRemove.Add(token.Key);
                }
            }

            foreach (string key in keysToRemove)
            {
                accessTokens.Remove(key);
            }

            accessTokens[requestedTarget] = tokenToAdd;
        }

        private bool HashSetsAreIntersecting(HashSet<string> lhs, HashSet<string> rhs)
        {
            HashSet<string> smallerSet = lhs.Count < rhs.Count ? lhs : rhs;
            HashSet<string> largerSet = lhs.Count < rhs.Count ? rhs : lhs;

            foreach (string item in smallerSet)
            {
                if (largerSet.Contains(item))
                {
                    return true;
                }
            }

            return false;
        }

        private void DeleteIdTokens(
            string homeAccountId,
            string environment,
            string realm,
            string clientId)
        {
            _fileIo.DeleteFile(
                _credentialPathManager.GetCredentialPath(
                    homeAccountId,
                    environment,
                    realm,
                    clientId,
                    string.Empty,
                    CredentialType.OidcIdToken));
        }

        private void DeleteRefreshTokens(
            string homeAccountId,
            string environment,
            string clientId,
            string familyId)
        {
            if (!string.IsNullOrWhiteSpace(familyId))
            {
                _fileIo.DeleteFile(
                    _credentialPathManager.GetCredentialPath(
                        homeAccountId,
                        environment,
                        string.Empty,
                        string.Empty,
                        familyId,
                        CredentialType.OAuth2RefreshToken));
            }

            if (!string.IsNullOrWhiteSpace(clientId))
            {
                _fileIo.DeleteFile(
                    _credentialPathManager.GetCredentialPath(
                        homeAccountId,
                        environment,
                        string.Empty,
                        clientId,
                        string.Empty,
                        CredentialType.OAuth2RefreshToken));
            }
        }

        private void DeleteAccessTokens(
            string homeAccountId,
            string environment,
            string realm,
            string clientId,
            string target)
        {
            string credentialPath = _credentialPathManager.GetCredentialPath(
                homeAccountId,
                environment,
                realm,
                clientId,
                string.Empty,
                CredentialType.OAuth2AccessToken);
            bool isFileEmpty = false;
            ReadModifyWrite(
                credentialPath,
                fileContentJson =>
                {
                    RemoveAccessTokenWithScopes(fileContentJson, target);
                    if (!fileContentJson.HasValues)
                    {
                        isFileEmpty = true;
                    }

                    return fileContentJson;
                });

            if (isFileEmpty)
            {
                _fileIo.DeleteFile(credentialPath);
            }
        }

        public void RemoveAccessTokenWithScopes(JObject accessTokens, string target)
        {
            KeyValuePair<string, JToken>? kvp = FindAccessTokenIterWithScopes(accessTokens, target);
            if (kvp.HasValue)
            {
                accessTokens.Remove(kvp.Value.Key);
            }
        }

        public void ReadModifyWrite(string relativePath, Func<JObject, JObject> modify)
        {
            _fileIo.ReadModifyWrite(
                relativePath,
                existingBytes =>
                {
                    var fileContentJson = DecryptParse(existingBytes, relativePath);
                    fileContentJson = modify(fileContentJson);
                    string serializedJson = fileContentJson.ToString();
                    return Encrypt(serializedJson);
                });
        }

        public byte[] Encrypt(string input)
        {
            // TODO: enable encryption/decryption and enable configuration to determine if we should.
            return Encoding.UTF8.GetBytes(input);
        }

        private byte[] Decrypt(byte[] input)
        {
            // TODO: enable encryption/decryption and enable configuration to determine if we should.
            return input;
        }

        private void JsonMerge(JObject source, JObject destination)
        {
            foreach (KeyValuePair<string, JToken> kvp in source)
            {
                destination[kvp.Key] = kvp.Value;
            }
        }
    }
}