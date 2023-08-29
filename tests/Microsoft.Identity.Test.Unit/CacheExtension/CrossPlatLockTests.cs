// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CacheExtension
{
    [TestClass]
    public class CrossPlatLockTests
    {
        const int NumTasks = 100;

        public TestContext TestContext { get; set; }

        [RunOnWindows]
        [TestCategory(TestCategories.Regression)]  // https://github.com/AzureAD/microsoft-authentication-extensions-for-dotnet/issues/187
        public void DirNotExists()
        {
            // Arrange

            string cacheFileDir;

            // ensure the cache directory does not exist
            do
            {
                string tempDirName = System.IO.Path.GetRandomFileName();
                cacheFileDir = Path.Combine(Path.GetTempPath(), tempDirName);

            } while (Directory.Exists(cacheFileDir));

            using (var crossPlatLock = new CrossPlatLock(
              Path.Combine(cacheFileDir, "file.lockfile"), // the directory is guaranteed to not exist
              100,
              1))
            {
                // no-op
            }

            // before fixing the bug, an exception would occur here: 
            // System.InvalidOperationException: Could not get access to the shared lock file.
            // ---> System.IO.DirectoryNotFoundException: Could not find a part of the path 'C:\Users\....
        }
    }
}
