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
using Microsoft.Identity.Client.CacheV2.Schema;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.PlatformsCommon.Factories;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

namespace Microsoft.Identity.Client.CacheV2.Impl
{
    internal class StorageManager : IStorageManager
    {
        private readonly IStorageWorker _storageWorker;

        public StorageManager(IPlatformProxy platformProxy, IStorageWorker storageWorker, IAdalLegacyCacheManager adalLegacyCacheManager = null)
        {
            _storageWorker = storageWorker;
            AdalLegacyCacheManager = adalLegacyCacheManager ?? new AdalLegacyCacheManager(platformProxy.CreateLegacyCachePersistence());
        }

        public IAdalLegacyCacheManager AdalLegacyCacheManager { get; }
        //public event EventHandler<TokenCacheNotificationArgs> BeforeAccess;
        //public event EventHandler<TokenCacheNotificationArgs> AfterAccess;
        //public event EventHandler<TokenCacheNotificationArgs> BeforeWrite;

        /// <inheritdoc />
        public byte[] Serialize()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void Deserialize(byte[] serializedBytes)
        {
            throw new NotImplementedException();
        }

        public ReadCredentialsResponse ReadCredentials(
            string correlationId,
            string homeAccountId,
            string environment,
            string realm,
            string clientId,
            string familyId,
            string target,
            ISet<CredentialType> types)
        {
            try
            {
                //using (new StorageManagerDelegateManager(this, new TokenCacheNotificationArgs(clientId, null), false))
                {
                    IEnumerable<Credential> credentials = _storageWorker.ReadCredentials(
                        homeAccountId,
                        environment,
                        realm,
                        clientId,
                        familyId,
                        target,
                        types);

                    // if no refresh token in credentials...
                    // _adalLegacyCacheManager.GetAdalRefreshToken();

                    return new ReadCredentialsResponse(credentials, OperationStatus.CreateSuccess());
                }
            }
            catch (Exception ex)
            {
                return new ReadCredentialsResponse(null, HandleException(ex));
            }
        }

        public OperationStatus WriteCredentials(string correlationId, IEnumerable<Credential> credentials)
        {
            try
            {
                //using (new StorageManagerDelegateManager(this, new TokenCacheNotificationArgs(null, null), true))
                {
                    _storageWorker.WriteCredentials(credentials);

                    // walk credentials, and write any that are refresh tokens to adal cache
                    // _adalLegacyCacheManager.WriteAdalRefreshToken();

                    return OperationStatus.CreateSuccess();
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        public OperationStatus DeleteCredentials(
            string correlationId,
            string homeAccountId,
            string environment,
            string realm,
            string clientId,
            string familyId,
            string target,
            ISet<CredentialType> types)
        {
            try
            {
                //using (new StorageManagerDelegateManager(this, new TokenCacheNotificationArgs(null, null), true))
                {
                    _storageWorker.DeleteCredentials(
                        homeAccountId,
                        environment,
                        realm,
                        clientId,
                        familyId,
                        target,
                        types);
                    return OperationStatus.CreateSuccess();
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        public ReadAccountsResponse ReadAllAccounts(string correlationId)
        {
            // _adalLegacyCacheManager.GetAllAdalUsers();
            throw new NotImplementedException();
        }

        public ReadAccountResponse ReadAccount(
            string correlationId,
            string homeAccountId,
            string environment,
            string realm)
        {
            try
            {
                //using (new StorageManagerDelegateManager(this, new TokenCacheNotificationArgs(null, null), false))
                {
                    var account = _storageWorker.ReadAccount(homeAccountId, environment, realm);

                    // if not found...
                    // _adalLegacyCacheManager.GetAllAdalUsers().FirstOrDefault(x => x matches query);

                    return new ReadAccountResponse(account, OperationStatus.CreateSuccess());
                }
            }
            catch (Exception ex)
            {
                return new ReadAccountResponse(null, HandleException(ex));
            }
        }

        public OperationStatus WriteAccount(string correlationId, Microsoft.Identity.Client.CacheV2.Schema.Account account)
        {
            try
            {
                //using (new StorageManagerDelegateManager(this, new TokenCacheNotificationArgs(null, null), true))
                {
                    _storageWorker.WriteAccount(account);
                    return OperationStatus.CreateSuccess();
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        public OperationStatus DeleteAccount(
            string correlationId,
            string homeAccountId,
            string environment,
            string realm)
        {
            try
            {
                //using (new StorageManagerDelegateManager(this, new TokenCacheNotificationArgs(null, null), true))
                {
                    _storageWorker.DeleteAccount(homeAccountId, environment, realm);

                    // _adalLegacyCacheManager.RemoveAdalUser();

                    return OperationStatus.CreateSuccess();
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        public OperationStatus DeleteAccounts(string correlationId, string homeAccountId, string environment)
        {
            try
            {
                //using (new StorageManagerDelegateManager(this, new TokenCacheNotificationArgs(null, null), true))
                {
                    _storageWorker.DeleteAccounts(homeAccountId, environment);
                    return OperationStatus.CreateSuccess();
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        public AppMetadata ReadAppMetadata(string environment, string clientId)
        {
            try
            {
                //using (new StorageManagerDelegateManager(this, new TokenCacheNotificationArgs(null, null), false))
                {
                    return _storageWorker.ReadAppMetadata(environment, clientId);
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
                return null;
            }
        }

        public void WriteAppMetadata(AppMetadata appMetadata)
        {
            try
            {
                //using (new StorageManagerDelegateManager(this, new TokenCacheNotificationArgs(null, null), true))
                {
                    _storageWorker.WriteAppMetadata(appMetadata);
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        //internal void OnBeforeAccess(TokenCacheNotificationArgs args)
        //{
        //    BeforeAccess?.Invoke(this, args);
        //}

        //internal void OnBeforeWrite(TokenCacheNotificationArgs args)
        //{
        //    BeforeWrite?.Invoke(this, args);
        //}

        //internal void OnAfterAccess(TokenCacheNotificationArgs args)
        //{
        //    AfterAccess?.Invoke(this, args);
        //}

        private OperationStatus HandleException(Exception ex)
        {
            var status = new OperationStatus
            {
                Code = -1,
                PlatformCode = -1,
                PlatformDomain = string.Empty,
                StatusDescription = ex.Message,
                StatusType = OperationStatusType.Failure
            };

            if (ex is ArgumentException)
            {
            }

            return status;
        }
    }
}