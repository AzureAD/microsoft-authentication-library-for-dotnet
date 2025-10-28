// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommonCache.Test.Common;
using CommonCache.Test.Unit.Utils;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommonCache.Test.Unit
{
    [TestClass]
    public class CacheExecutionTests
    {
        private static readonly List<LabUserData> s_labUsers = new List<LabUserData>();

        private static async Task<LabUserData> GetPublicAadUserDataAsync()
        {
            var labUser = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
            return new LabUserData(
              labUser.User.Upn,
              labUser.User.GetOrFetchPassword(),
              labUser.App.AppId,
              labUser.User.TenantId);
        }

        [AssemblyInitialize]
        public static void AssemblyInit(TestContext testContext)
        {
            // TODO: add other users to the mix
            s_labUsers.Add(GetPublicAadUserDataAsync().GetAwaiter().GetResult());
        }

        [DataTestMethod]
        [DataRow(CacheProgramType.MsalV3, CacheProgramType.MsalV3, CacheStorageType.MsalV2, DisplayName = "MsalV3->MsalV3 msal v2 cache")]
        [DataRow(CacheProgramType.MsalV3, CacheProgramType.MsalV3, CacheStorageType.MsalV3, DisplayName = "MsalV3->MsalV3 msal v3 cache")]
        public async Task TestMsalV3CacheCompatibilityAsync(
            CacheProgramType interactiveType,
            CacheProgramType silentType,
            CacheStorageType cacheStorageType)
        {
            var executor = new CacheTestExecutor(s_labUsers, cacheStorageType);
            await executor.ExecuteAsync(interactiveType, silentType, CancellationToken.None).ConfigureAwait(false);
        }
      
        [DataTestMethod]
        [DataRow(CacheProgramType.MsalV3, CacheProgramType.MsalJava, CacheStorageType.MsalV3, DisplayName = "MsalV3->MsalJava msal v3 cache")]
        [DataRow(CacheProgramType.MsalJava, CacheProgramType.MsalV3, CacheStorageType.MsalV3, DisplayName = "MsalJava->MsalV3 msal v3 cache")]
        public async Task TestMsalJavaCacheCompatibilityAsync(
            CacheProgramType interactiveType,
            CacheProgramType silentType,
            CacheStorageType cacheStorageType)
        {
            var executor = new CacheTestExecutor(s_labUsers, cacheStorageType);
            await executor.ExecuteAsync(interactiveType, silentType, CancellationToken.None).ConfigureAwait(false);
        }
    }
}
