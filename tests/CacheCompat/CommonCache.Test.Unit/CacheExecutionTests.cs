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

        private static LabUserData GetLabUserData(string upn)
        {
            var labUser = new LabServiceApi().GetLabResponse(new UserQuery { Upn = upn }).User;
            return new LabUserData(labUser.Upn, labUser.GetOrFetchPassword());
        }

        [AssemblyInitialize]
        public static void AssemblyInit(TestContext testContext)
        {
            // TODO: these other users throw MsalUiRequiredException invalid_grant.
            // Are they MFA-required users?  What is different about them?
            // s_labUsers.Add(GetLabUserData("idlab@msidlab2.onmicrosoft.com"));
            // s_labUsers.Add(GetLabUserData("idlab@msidlab3.onmicrosoft.com"));
            s_labUsers.Add(GetLabUserData("idlab@msidlab4.onmicrosoft.com"));
        }

        [DataTestMethod]
        [DataRow(CacheProgramType.AdalV3, CacheProgramType.AdalV3, CacheStorageType.Adal, DisplayName = "AdalV3->AdalV3 adal v3 cache")]
        [DataRow(CacheProgramType.AdalV3, CacheProgramType.AdalV4, CacheStorageType.Adal, DisplayName = "AdalV3->AdalV4 adal v3 cache")]
        [DataRow(CacheProgramType.AdalV3, CacheProgramType.AdalV5, CacheStorageType.Adal, DisplayName = "AdalV3->AdalV5 adal v3 cache")]
        [DataRow(CacheProgramType.AdalV3, CacheProgramType.MsalV2, CacheStorageType.Adal, DisplayName = "AdalV3->MsalV2 adal v3 cache")]
        [DataRow(CacheProgramType.AdalV3, CacheProgramType.MsalV3, CacheStorageType.Adal, DisplayName = "AdalV3->MsalV3 adal v3 cache")]
        public async Task TestAdalV3CacheCompatibilityAsync(
            CacheProgramType interactiveType,
            CacheProgramType silentType,
            CacheStorageType cacheStorageType)
        {
            var executor = new CacheTestExecutor(
                s_labUsers,
                interactiveType,
                silentType,
                cacheStorageType);

            await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(CacheProgramType.AdalV4, CacheProgramType.AdalV3, CacheStorageType.Adal,   DisplayName = "AdalV4->AdalV3 adal v3 cache")]
        [DataRow(CacheProgramType.AdalV4, CacheProgramType.AdalV4, CacheStorageType.Adal,   DisplayName = "AdalV4->AdalV4 adal v3 cache")]
        [DataRow(CacheProgramType.AdalV4, CacheProgramType.AdalV5, CacheStorageType.Adal,   DisplayName = "AdalV4->AdalV5 adal v3 cache")]
        [DataRow(CacheProgramType.AdalV4, CacheProgramType.AdalV4, CacheStorageType.MsalV2, DisplayName = "AdalV4->AdalV4 msal v2 cache")]
        [DataRow(CacheProgramType.AdalV4, CacheProgramType.AdalV5, CacheStorageType.MsalV2, DisplayName = "AdalV4->AdalV5 msal v2 cache")]
        [DataRow(CacheProgramType.AdalV4, CacheProgramType.MsalV2, CacheStorageType.Adal,   DisplayName = "AdalV4->MsalV2 adal v3 cache")]
        [DataRow(CacheProgramType.AdalV4, CacheProgramType.MsalV2, CacheStorageType.MsalV2, DisplayName = "AdalV4->MsalV2 msal v2 cache")]
        [DataRow(CacheProgramType.AdalV4, CacheProgramType.MsalV3, CacheStorageType.Adal,   DisplayName = "AdalV4->MsalV3 adal v3 cache")]
        [DataRow(CacheProgramType.AdalV4, CacheProgramType.MsalV3, CacheStorageType.MsalV2, DisplayName = "AdalV4->MsalV3 msal v2 cache")]
        public async Task TestAdalV4CacheCompatibilityAsync(
            CacheProgramType interactiveType,
            CacheProgramType silentType,
            CacheStorageType cacheStorageType)
        {
            var executor = new CacheTestExecutor(
                s_labUsers,
                interactiveType,
                silentType,
                cacheStorageType);

            await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(CacheProgramType.AdalV5, CacheProgramType.AdalV3, CacheStorageType.Adal, DisplayName = "AdalV5->AdalV3 adal v3 cache")]
        [DataRow(CacheProgramType.AdalV5, CacheProgramType.AdalV4, CacheStorageType.Adal, DisplayName = "AdalV5->AdalV4 adal v3 cache")]
        [DataRow(CacheProgramType.AdalV5, CacheProgramType.AdalV5, CacheStorageType.Adal, DisplayName = "AdalV5->AdalV5 adal v3 cache")]
        [DataRow(CacheProgramType.AdalV5, CacheProgramType.AdalV4, CacheStorageType.MsalV2, DisplayName = "AdalV5->AdalV4 msal v2 cache")]
        [DataRow(CacheProgramType.AdalV5, CacheProgramType.AdalV5, CacheStorageType.MsalV2, DisplayName = "AdalV5->AdalV5 msal v2 cache")]
        [DataRow(CacheProgramType.AdalV5, CacheProgramType.AdalV5, CacheStorageType.MsalV3, DisplayName = "AdalV5->AdalV5 msal v3 cache")]
        [DataRow(CacheProgramType.AdalV5, CacheProgramType.MsalV2, CacheStorageType.Adal, DisplayName = "AdalV5->MsalV2 adal v3 cache")]
        [DataRow(CacheProgramType.AdalV5, CacheProgramType.MsalV2, CacheStorageType.MsalV2, DisplayName = "AdalV5->MsalV2 msal v2 cache")]
        [DataRow(CacheProgramType.AdalV5, CacheProgramType.MsalV3, CacheStorageType.Adal, DisplayName = "AdalV5->MsalV3 adal v3 cache")]
        [DataRow(CacheProgramType.AdalV5, CacheProgramType.MsalV3, CacheStorageType.MsalV2, DisplayName = "AdalV5->MsalV3 msal v2 cache")]
        [DataRow(CacheProgramType.AdalV5, CacheProgramType.MsalV3, CacheStorageType.MsalV3, DisplayName = "AdalV5->MsalV3 msal v3 cache")]
        public async Task TestAdalV5CacheCompatibilityAsync(
            CacheProgramType interactiveType,
            CacheProgramType silentType,
            CacheStorageType cacheStorageType)
        {
            var executor = new CacheTestExecutor(
                s_labUsers,
                interactiveType,
                silentType,
                cacheStorageType);

            await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(CacheProgramType.MsalV2, CacheProgramType.AdalV3, CacheStorageType.Adal,   DisplayName = "MsalV2->AdalV3 adal v3 cache")]
        [DataRow(CacheProgramType.MsalV2, CacheProgramType.AdalV4, CacheStorageType.Adal,   DisplayName = "MsalV2->AdalV4 adal v3 cache")]
        [DataRow(CacheProgramType.MsalV2, CacheProgramType.AdalV4, CacheStorageType.MsalV2, DisplayName = "MsalV2->AdalV4 msal v2 cache")]
        [DataRow(CacheProgramType.MsalV2, CacheProgramType.AdalV5, CacheStorageType.Adal,   DisplayName = "MsalV2->AdalV5 adal v3 cache")]
        [DataRow(CacheProgramType.MsalV2, CacheProgramType.AdalV5, CacheStorageType.MsalV2, DisplayName = "MsalV2->AdalV5 msal v2 cache")]
        [DataRow(CacheProgramType.MsalV2, CacheProgramType.MsalV2, CacheStorageType.Adal,   DisplayName = "MsalV2->MsalV2 adal v3 cache")]
        [DataRow(CacheProgramType.MsalV2, CacheProgramType.MsalV2, CacheStorageType.MsalV2, DisplayName = "MsalV2->MsalV2 msal v2 cache")]
        [DataRow(CacheProgramType.MsalV2, CacheProgramType.MsalV3, CacheStorageType.Adal,   DisplayName = "MsalV2->MsalV3 adal v3 cache")]
        [DataRow(CacheProgramType.MsalV2, CacheProgramType.MsalV3, CacheStorageType.MsalV2, DisplayName = "MsalV2->MsalV3 msal v2 cache")]
        public async Task TestMsalV2CacheCompatibilityAsync(
            CacheProgramType interactiveType,
            CacheProgramType silentType,
            CacheStorageType cacheStorageType)
        {
            var executor = new CacheTestExecutor(
                s_labUsers,
                interactiveType,
                silentType,
                cacheStorageType);

            await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(CacheProgramType.MsalV3, CacheProgramType.AdalV3, CacheStorageType.Adal,   DisplayName = "MsalV3->AdalV3 adal v3 cache")]
        [DataRow(CacheProgramType.MsalV3, CacheProgramType.AdalV4, CacheStorageType.Adal,   DisplayName = "MsalV3->AdalV4 adal v3 cache")]
        [DataRow(CacheProgramType.MsalV3, CacheProgramType.AdalV4, CacheStorageType.MsalV2, DisplayName = "MsalV3->AdalV4 msal v2 cache")]
        [DataRow(CacheProgramType.MsalV3, CacheProgramType.AdalV5, CacheStorageType.Adal,   DisplayName = "MsalV3->AdalV5 adal v3 cache")]
        [DataRow(CacheProgramType.MsalV3, CacheProgramType.AdalV5, CacheStorageType.MsalV2, DisplayName = "MsalV3->AdalV5 msal v2 cache")]
        [DataRow(CacheProgramType.MsalV3, CacheProgramType.AdalV5, CacheStorageType.MsalV3, DisplayName = "MsalV3->AdalV5 msal v3 cache")]
        [DataRow(CacheProgramType.MsalV3, CacheProgramType.MsalV3, CacheStorageType.Adal,   DisplayName = "MsalV3->MsalV3 adal v3 cache")]
        [DataRow(CacheProgramType.MsalV3, CacheProgramType.MsalV3, CacheStorageType.MsalV2, DisplayName = "MsalV3->MsalV3 msal v2 cache")]
        [DataRow(CacheProgramType.MsalV3, CacheProgramType.MsalV3, CacheStorageType.MsalV3, DisplayName = "MsalV3->MsalV3 msal v3 cache")]
        public async Task TestMsalV3CacheCompatibilityAsync(
            CacheProgramType interactiveType,
            CacheProgramType silentType,
            CacheStorageType cacheStorageType)
        {
            var executor = new CacheTestExecutor(
                s_labUsers,
                interactiveType,
                silentType,
                cacheStorageType);

            await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(CacheProgramType.MsalPython, CacheProgramType.MsalV3, CacheStorageType.MsalV3, DisplayName = "MsalPython->MsalV3 msal v3 cache")]
        [DataRow(CacheProgramType.MsalPython, CacheProgramType.AdalV5, CacheStorageType.MsalV3, DisplayName = "MsalPython->AdalV5 msal v3 cache")]
        [DataRow(CacheProgramType.MsalV3, CacheProgramType.MsalPython, CacheStorageType.MsalV3, DisplayName = "MsalV3->MsalPython msal v3 cache")] // this one will fail because we're missing authority aliasing in python
        [DataRow(CacheProgramType.AdalV5, CacheProgramType.MsalPython, CacheStorageType.MsalV3, DisplayName = "AdalV5->MsalPython msal v3 cache")] // this one will fail because we're missing authority aliasing in python
        public async Task TestMsalPythonCacheCompatibilityAsync(
            CacheProgramType interactiveType,
            CacheProgramType silentType,
            CacheStorageType cacheStorageType)
        {
            var executor = new CacheTestExecutor(
                s_labUsers,
                interactiveType,
                silentType,
                cacheStorageType);

            await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }
}
