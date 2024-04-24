// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    /// <summary>
    /// These tests write data to disk / key chain / key ring etc. 
    /// </summary>
    [TestClass]
    public class MsalCacheStorageIntegrationTests
    {
        public static readonly string CacheFilePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
        private readonly TraceSource _logger = new TraceSource("TestSource", SourceLevels.All);
        private static StorageCreationProperties s_storageCreationProperties;

        public TestContext TestContext { get; set; }

        [ClassInitialize]
        public static void ClassInitialize(TestContext _)
        {
            var builder = new StorageCreationPropertiesBuilder(
                Path.GetFileName(CacheFilePath),
                Path.GetDirectoryName(CacheFilePath));
            builder = builder.WithMacKeyChain(serviceName: "Microsoft.Developer.IdentityService", accountName: "MSALCache");

            // Tests run on machines without Libsecret
            builder = builder.WithLinuxUnprotectedFile();
            s_storageCreationProperties = builder.Build();
        }

        [TestInitialize]
        public void TestiInitialize()
        {
            CleanTestData();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            CleanTestData();
        }

        [TestMethod]
        public void MsalTestUserDirectory()
        {
            Assert.AreEqual(MsalCacheHelper.UserRootDirectory,
                Environment.OSVersion.Platform == PlatformID.Win32NT
                    ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                    : Environment.GetEnvironmentVariable("HOME"));
        }

        [RunOnOSX]
        public void CacheStorageFactoryMac()
        {
            Storage store = Storage.Create(s_storageCreationProperties, logger: _logger);
            Assert.IsTrue(store.CacheAccessor is MacKeychainAccessor);
            store.VerifyPersistence();

            store = Storage.Create(s_storageCreationProperties, logger: _logger);
            Assert.IsTrue(store.CacheAccessor is MacKeychainAccessor);
        }

        [RunOnWindows]
        public void CacheStorageFactoryWindows()
        {
            Storage store = Storage.Create(s_storageCreationProperties, logger: _logger);
            Assert.IsTrue(store.CacheAccessor is DpApiEncryptedFileAccessor);
            store.VerifyPersistence();

            store = Storage.Create(s_storageCreationProperties, logger: _logger);
            Assert.IsTrue(store.CacheAccessor is DpApiEncryptedFileAccessor);
        }

        [TestMethod]
        public void CacheFallback()
        {
            const string data = "data";
            string cacheFilePathFallback = CacheFilePath + "fallback";
            var plaintextStorage = new StorageCreationPropertiesBuilder(
                    Path.GetFileName(cacheFilePathFallback),
                    Path.GetDirectoryName(CacheFilePath))
                .WithUnprotectedFile()
                .Build();

            Storage unprotectedStore = Storage.Create(plaintextStorage, _logger);
            Assert.IsTrue(unprotectedStore.CacheAccessor is FileAccessor);

            unprotectedStore.VerifyPersistence();
            unprotectedStore.WriteData(Encoding.UTF8.GetBytes(data));

            // Unprotected cache file should exist
            Assert.IsTrue(File.Exists(plaintextStorage.CacheFilePath));

            string dataReadFromPlaintext = File.ReadAllText(plaintextStorage.CacheFilePath);

            Assert.AreEqual(data, dataReadFromPlaintext);

            // Verify that file permissions are set to 600
            FileHelper.AssertChmod600(plaintextStorage.CacheFilePath);
        }

        [RunOnLinux]
        public void CacheStorageFactory_WithFallback_Linux()
        {
            var storageWithKeyRing = new StorageCreationPropertiesBuilder(
                    Path.GetFileName(CacheFilePath),
                    Path.GetDirectoryName(CacheFilePath))
                .WithMacKeyChain(serviceName: "Microsoft.Developer.IdentityService", accountName: "MSALCache")
                .WithLinuxKeyring(
                    schemaName: "msal.cache",
                    collection: "default",
                    secretLabel: "MSALCache",
                    attribute1: new KeyValuePair<string, string>("MsalClientID", "Microsoft.Developer.IdentityService"),
                    attribute2: new KeyValuePair<string, string>("MsalClientVersion", "1.0.0.0"))
                .Build();

            // Tests run on machines without Libsecret
            Storage store = Storage.Create(storageWithKeyRing, logger: _logger);
            Assert.IsTrue(store.CacheAccessor is LinuxKeyringAccessor);

            // ADO Linux test agents do not have libsecret installed by default
            // If you run this test on a Linux box with UI / LibSecret, then this test will fail
            // because the statement below will not throw.
            AssertException.Throws<MsalCachePersistenceException>(
                () => store.VerifyPersistence());

            Storage unprotectedStore = Storage.Create(s_storageCreationProperties, _logger);
            Assert.IsTrue(unprotectedStore.CacheAccessor is FileAccessor);

            unprotectedStore.VerifyPersistence();

            unprotectedStore.WriteData(new byte[] { 2, 3 });

            // Unproteced cache file should exist
            Assert.IsTrue(File.Exists(s_storageCreationProperties.CacheFilePath));

            // Mimic another sdk client to check libsecret availability by calling
            // MsalCacheStorage.VerifyPeristence() -> LinuxKeyringAccessor.CreateForPersistenceValidation()
            AssertException.Throws<MsalCachePersistenceException>(
                () => store.VerifyPersistence());

            // Verify above call doesn't delete existing cache file
            Assert.IsTrue(File.Exists(s_storageCreationProperties.CacheFilePath));

            // Verify that file permissions are set to 600
            FileHelper.AssertChmod600(s_storageCreationProperties.CacheFilePath);
        }

        [TestMethod]
        public void MsalNewStoreNoFile()
        {
            var store = Storage.Create(s_storageCreationProperties, logger: _logger);
            Assert.IsFalse(store.ReadData().Any());
        }

        [TestMethod]
        public void MsalWriteEmptyData()
        {
            var store = Storage.Create(s_storageCreationProperties, logger: _logger);
            Assert.ThrowsException<ArgumentNullException>(() => store.WriteData(null));

            store.WriteData(new byte[0]);

            Assert.IsFalse(store.ReadData().Any());
        }

        [TestMethod]
        public void MsalWriteGoodData()
        {
            var store = Storage.Create(s_storageCreationProperties, logger: _logger);
            Assert.ThrowsException<ArgumentNullException>(() => store.WriteData(null));

            byte[] data = { 2, 2, 3 };
            byte[] data2 = { 2, 2, 3, 4, 4 };
            store.WriteData(data);
            Assert.IsTrue(Enumerable.SequenceEqual(store.ReadData(), data));

            store.WriteData(data);
            store.WriteData(data2);
            store.WriteData(data);
            store.WriteData(data2);
            Assert.IsTrue(Enumerable.SequenceEqual(store.ReadData(), data2));
        }

        [TestMethod]
        public void MsalTestClear()
        {
            var store = Storage.Create(s_storageCreationProperties, logger: _logger);
            store.ReadData();

            var store2 = Storage.Create(s_storageCreationProperties, logger: _logger);
            AssertException.Throws<ArgumentNullException>(() => store.WriteData(null));

            byte[] data = { 2, 2, 3 };
            store.WriteData(data);
            store2.ReadData();

            Assert.IsTrue(Enumerable.SequenceEqual(store.ReadData(), data));
            Assert.IsTrue(File.Exists(CacheFilePath));

            store.Clear();

            Assert.IsFalse(store.ReadData().Any());
            Assert.IsFalse(store2.ReadData().Any());
            Assert.IsFalse(File.Exists(CacheFilePath));
        }

        private void CleanTestData()
        {
            var store = Storage.Create(s_storageCreationProperties, logger: _logger);
            store.Clear();
        }
    }

    public static class FileHelper
    {
        /// <summary>
        /// Checks that file permissions are set to 600.
        /// </summary>
        /// <param name="filePath"></param>
        public static void AssertChmod600(string filePath)
        {
            if (SharedUtilities.IsWindowsPlatform())
            {
                FileInfo fi = new FileInfo(filePath);
                var acl = fi.GetAccessControl();
                var accessRules = acl.GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier));

                Assert.AreEqual(1, accessRules.Count);

                var rule = accessRules.Cast<FileSystemAccessRule>().Single();

                Assert.AreEqual(FileSystemRights.Read | FileSystemRights.Write | FileSystemRights.Synchronize, rule.FileSystemRights);
                Assert.AreEqual(AccessControlType.Allow, rule.AccessControlType);
                Assert.AreEqual(System.Security.Principal.WindowsIdentity.GetCurrent().User, rule.IdentityReference);
                Assert.IsFalse(rule.IsInherited);
                Assert.AreEqual(InheritanceFlags.None, rule.InheritanceFlags);
            }
            else
            {
                // e.g. -rw------ 1 user1 user1 1280 Mar 23 08:39 /home/user1/g/Program.cs
                var output = ExecuteAndCaptureOutput($"ls -l {filePath}");
                Assert.IsTrue(output.StartsWith("-rw------")); // 600
            }
        }

        private static string ExecuteAndCaptureOutput(string cmd)
        {
            var escapedArgs = cmd.Replace("\"", "\\\"");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{escapedArgs}\""
                }
            };

            string output = string.Empty;

            process.OutputDataReceived += (sender, args) =>
            {
                output += args.Data;
            };

            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();

            return output;

        }
    }
}
