// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Microsoft.Identity.Test.Unit.CacheExtension
{
    [TestClass]
    public class MsalCacheHelperTests
    {
        private const string ClientId = "1d18b3b0-251b-4714-a02a-9956cec86c2d";
        private string _cacheFilePath;
        private readonly TraceSource _logger = new TraceSource("TestSource");
        private StorageCreationPropertiesBuilder _storageCreationPropertiesBuilder;

        [TestInitialize]
        public void TestInitialize()
        {
            _cacheFilePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
            _storageCreationPropertiesBuilder = new StorageCreationPropertiesBuilder(
                Path.GetFileName(_cacheFilePath),
                Path.GetDirectoryName(_cacheFilePath));

            _storageCreationPropertiesBuilder = _storageCreationPropertiesBuilder.WithMacKeyChain(serviceName: "Microsoft.Developer.IdentityService", accountName: "MSALCache");
            _storageCreationPropertiesBuilder.WithLinuxUnprotectedFile();
        }

        [TestMethod]
        public void ImportExport()
        {
            // Arrange
            var cacheAccessor = NSubstitute.Substitute.For<ICacheAccessor>();
            var cache = new MockTokenCache();
            var storage = new Storage(
                _storageCreationPropertiesBuilder.Build(),
                cacheAccessor,
                new TraceSourceLogger(new TraceSource("ts")));
            var helper = new MsalCacheHelper(cache, storage, _logger);
            byte[] dataToSave = Encoding.UTF8.GetBytes("Hello World 2");

            cacheAccessor.Read().Returns(Encoding.UTF8.GetBytes("Hello World"));

            // Act
            byte[] actualData = helper.LoadUnencryptedTokenCache();
            helper.SaveUnencryptedTokenCache(dataToSave);

            // Assert
            Assert.AreEqual("Hello World", Encoding.UTF8.GetString(actualData));
            cacheAccessor.Received().Write(dataToSave);
        }

        [TestMethod]
        public void ImportExport_ThrowException()
        {
            // Arrange
            var cacheAccessor = NSubstitute.Substitute.For<ICacheAccessor>();
            var cache = new MockTokenCache();
            var storage = new Storage(
                _storageCreationPropertiesBuilder.Build(),
                cacheAccessor,
                new TraceSourceLogger(new TraceSource("ts")));
            var helper = new MsalCacheHelper(cache, storage, _logger);

            var ex = new InvalidCastException();

            cacheAccessor.Read().Throws(ex);

            // Act
            var actualEx = AssertException.Throws<InvalidCastException>(
                () => helper.LoadUnencryptedTokenCache());

            // Assert
            Assert.AreEqual(ex, actualEx);

            // Arrange
            cacheAccessor.WhenForAnyArgs(c => c.Write(default)).Throw(ex);

            // Act
            actualEx = AssertException.Throws<InvalidCastException>(
                () => helper.SaveUnencryptedTokenCache(new byte[0]));

            // Assert
            Assert.AreEqual(ex, actualEx);
        }

        [TestMethod]
        public void MultiAccessSerialization()
        {
            var cache1 = new MockTokenCache();
            var helper1 = new MsalCacheHelper(
                cache1,
                Storage.Create(_storageCreationPropertiesBuilder.Build(), _logger),
                _logger);

            var cache2 = new MockTokenCache();
            var helper2 = new MsalCacheHelper(
                cache2,
                Storage.Create(_storageCreationPropertiesBuilder.Build(), _logger),
                _logger);

            //Test signalling thread 1
            var resetEvent1 = new ManualResetEventSlim(initialState: false);

            //Test signalling thread 2
            var resetEvent2 = new ManualResetEventSlim(initialState: false);

            //Thread 1 signalling test
            var resetEvent3 = new ManualResetEventSlim(initialState: false);

            // Thread 2 signalling test
            var resetEvent4 = new ManualResetEventSlim(initialState: false);

            var thread1 = new Thread(() =>
            {
                var args = new TokenCacheNotificationArgs(cache1, string.Empty, null, false, false, null, true, null, CancellationToken.None);

                helper1.BeforeAccessNotification(args);
                resetEvent3.Set();
                resetEvent1.Wait();
                helper1.AfterAccessNotification(args);
            });

            var thread2 = new Thread(() =>
            {
                var args = new TokenCacheNotificationArgs(cache2, string.Empty, null, false, false, null, true, null, CancellationToken.None);
                helper2.BeforeAccessNotification(args);
                resetEvent4.Set();
                resetEvent2.Wait();
                helper2.AfterAccessNotification(args);
                resetEvent4.Set();
            });

            // Let thread 1 start and get the lock
            thread1.Start();
            resetEvent3.Wait();

            // Start thread 2 and give it enough time to get blocked on the lock
            thread2.Start();
            Thread.Sleep(5000);

            // Make sure helper1 has the lock still, and helper2 doesn't
            Assert.IsNotNull(helper1.CacheLock);
            Assert.IsNull(helper2.CacheLock);

            // Allow thread1 to give up the lock, and wait for helper2 to get it
            resetEvent1.Set();
            resetEvent4.Wait();
            resetEvent4.Reset();

            // Make sure helper1 gave it up properly, and helper2 now owns the lock
            Assert.IsNull(helper1.CacheLock);
            Assert.IsNotNull(helper2.CacheLock);

            // Allow thread2 to give up the lock, and wait for it to complete
            resetEvent2.Set();
            resetEvent4.Wait();

            // Make sure thread2 cleaned up after itself as well
            Assert.IsNull(helper2.CacheLock);
        }

        [TestMethod]
        public void LockTimeoutTest()
        {
            // Total of 1000ms delay
            _storageCreationPropertiesBuilder.CustomizeLockRetry(20, 100);
            var properties = _storageCreationPropertiesBuilder.Build();

            var cache1 = new MockTokenCache();
            var helper1 = new MsalCacheHelper(
                cache1,
                Storage.Create(properties, _logger),
                _logger);

            var cache2 = new MockTokenCache();
            var helper2 = new MsalCacheHelper(
                cache2,
                Storage.Create(properties, _logger),
                _logger);

            //Test signalling thread 1
            var resetEvent1 = new ManualResetEventSlim(initialState: false);

            //Test signalling thread 2
            var resetEvent2 = new ManualResetEventSlim(initialState: false);

            //Thread 1 signalling test
            var resetEvent3 = new ManualResetEventSlim(initialState: false);

            var thread1 = new Thread(() =>
            {
                var args = new TokenCacheNotificationArgs(cache1, string.Empty, null, false, false, null, true, null, CancellationToken.None);

                helper1.BeforeAccessNotification(args);
                // Indicate we are waiting
                resetEvent2.Set();
                resetEvent1.Wait();
                helper1.AfterAccessNotification(args);

                // Let thread 1 exit
                resetEvent3.Set();
            });

            Stopwatch getTime = new Stopwatch();

            var thread2 = new Thread(() =>
            {
                var args = new TokenCacheNotificationArgs(cache2, string.Empty, null, false, false, null, true, null, CancellationToken.None);
                getTime.Start();
                try
                {

                    helper2.BeforeAccessNotification(args);
                }
                catch (InvalidOperationException)
                {
                    // Invalid operation is the exception thrown if the lock cannot be acquired
                    getTime.Stop();
                }

                resetEvent1.Set();
            });

            // Let thread 1 start and get the lock
            thread1.Start();

            // Wait for thread one to get into the lock
            resetEvent2.Wait();

            // Start thread 2 and give it enough time to get blocked on the lock
            thread2.Start();

            // Wait for the seconf thread to finish
            resetEvent1.Wait();

            Assert.IsTrue(getTime.ElapsedMilliseconds > 2000);
        }

        [RunOnWindows]
        public async Task TwoRegisteredCachesRemainInSyncTestAsync()
        {
            var properties = _storageCreationPropertiesBuilder.Build();

            if (File.Exists(properties.CacheFilePath))
            {
                File.Delete(properties.CacheFilePath);
            }

            var helper = await MsalCacheHelper.CreateAsync(properties).ConfigureAwait(true);

            // Intentionally write the file after creating the MsalCacheHelper to avoid the initial inner PCA being created only to read garbage
            string startString = "Something to start with";
            var startBytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(startString), optionalEntropy: null, scope: DataProtectionScope.CurrentUser);
            File.WriteAllBytes(properties.CacheFilePath, startBytes);

            var cache1 = new MockTokenCache();
            var cache2 = new MockTokenCache();
            var cache3 = new MockTokenCache();

            helper.RegisterCache(cache1);
            helper.RegisterCache(cache2);
            helper.RegisterCache(cache3);

            // No calls at register
            Assert.AreEqual(0, cache1.DeserializeMsalV3_MergeCache);
            Assert.AreEqual(0, cache2.DeserializeMsalV3_MergeCache);
            Assert.AreEqual(0, cache3.DeserializeMsalV3_MergeCache);
            Assert.IsNull(cache1.LastDeserializedString);
            Assert.IsNull(cache2.LastDeserializedString);
            Assert.IsNull(cache3.LastDeserializedString);

            var args1 = new TokenCacheNotificationArgs(cache1, string.Empty, null, false, false, null, true, null, CancellationToken.None);
            var args2 = new TokenCacheNotificationArgs(cache2, string.Empty, null, false, false, null, true, null, CancellationToken.None);
            var args3 = new TokenCacheNotificationArgs(cache3, string.Empty, null, false, false, null, true, null, CancellationToken.None);

            var changedString = "Hey look, the file changed";

            helper.BeforeAccessNotification(args1);
            cache1.LastDeserializedString = changedString;
            args1.HasStateChanged = true;
            helper.AfterAccessNotification(args1);

            helper.BeforeAccessNotification(args2);
            helper.AfterAccessNotification(args2);

            helper.BeforeAccessNotification(args3);
            helper.AfterAccessNotification(args3);

            Assert.AreEqual(0, cache1.DeserializeMsalV3_MergeCache);
            Assert.AreEqual(0, cache2.DeserializeMsalV3_MergeCache);
            Assert.AreEqual(0, cache3.DeserializeMsalV3_MergeCache);

            // Cache 1 should deserialize, in spite of just writing, just in case another process wrote in the intervening time
            Assert.AreEqual(1, cache1.DeserializeMsalV3_ClearCache);

            // Caches 2 and 3 simply need to deserialize
            Assert.AreEqual(1, cache2.DeserializeMsalV3_ClearCache);
            Assert.AreEqual(1, cache3.DeserializeMsalV3_ClearCache);

            Assert.AreEqual(changedString, cache1.LastDeserializedString);
            Assert.AreEqual(changedString, cache2.LastDeserializedString);
            Assert.AreEqual(changedString, cache3.LastDeserializedString);

            File.Delete(properties.CacheFilePath);
            File.Delete(properties.CacheFilePath + ".version");
        }

        [TestMethod]
        [TestCategory(TestCategories.Regression)]
        [WorkItem(81)] // https://github.com/AzureAD/microsoft-authentication-extensions-for-dotnet/issues/81
        public async Task RegressionTest_CorruptedCacheIsDeleted_Async()
        {
            // Arrange
            var properties = _storageCreationPropertiesBuilder
                .WithCacheChangedEvent(ClientId, "https://login.microsoftonline.com/common")
                .Build();
            Storage storage = Storage.Create(properties, _logger);
            storage.WriteData(Encoding.UTF8.GetBytes("corrupted token cache"));

            // Act
            await MsalCacheHelper.CreateAsync(properties).ConfigureAwait(true);

            // Assert
            byte[] data = storage.ReadData();
            Assert.IsFalse(data.Any(), "Cache is corrupt, so it should have been deleted");
        }

        [TestMethod]
        [TestCategory(TestCategories.Regression)]
        [WorkItem(81)] // https://github.com/AzureAD/microsoft-authentication-extensions-for-dotnet/issues/81
        public async Task RegressionTest_CorruptedCacheIsDeleted_NoEvent_Async()
        {
            // Arrange
            var properties = _storageCreationPropertiesBuilder.Build();
            Storage storage = Storage.Create(properties, _logger);
            storage.WriteData(Encoding.UTF8.GetBytes("corrupted token cache"));
            var helper = await MsalCacheHelper.CreateAsync(properties).ConfigureAwait(true);
            var pca = PublicClientApplicationBuilder.Create(ClientId).Build();
            helper.RegisterCache(pca.UserTokenCache);

            // Act
            var ex = await AssertException.TaskThrowsAsync<MsalClientException>(
                () => pca.GetAccountsAsync())
                .ConfigureAwait(false);

            // Assert
            Assert.AreEqual(ex.ErrorCode, "json_parse_failed");
            byte[] data = storage.ReadData();
            Assert.IsFalse(data.Any(), "Cache is corrupt, so it should have been deleted");
        }

       
        [DoNotRunOnLinux] // The FileSystemWatcher on Linux doesn't always fire
        public async Task ClearCacheUsesTheLockAsync()
        {
            // Arrange
            var cacheAccessor = NSubstitute.Substitute.For<ICacheAccessor>();
            var cache = new MockTokenCache();
            var storage = new Storage(
                _storageCreationPropertiesBuilder.Build(),
                cacheAccessor,
                new TraceSourceLogger(new TraceSource("ts")));
            var helper = new MsalCacheHelper(cache, storage, _logger);

            FileSystemWatcher fileSystemWatcher = new FileSystemWatcher(
                Path.GetDirectoryName(_cacheFilePath),
                $"{Path.GetFileName(_cacheFilePath)}.lockfile");
            fileSystemWatcher.EnableRaisingEvents = true;

            // the events can be fired at a later time, so we need to wait for them
            // otherwise the test might finish first
            SemaphoreSlim semaphore1 = new SemaphoreSlim(0);
            SemaphoreSlim semaphore2 = new SemaphoreSlim(0);
            bool lockCreated = false, lockDeleted = false;

            fileSystemWatcher.Created += (_, _) =>
            {
                semaphore1.Release();
                lockCreated = true;
            };

            fileSystemWatcher.Deleted += (_, _) =>
            {
                semaphore2.Release();
                lockDeleted = true;
            };

            // Act
#pragma warning disable CS0618 // Type or member is obsolete
            helper.Clear();
#pragma warning restore CS0618 // Type or member is obsolete

            await semaphore1.WaitAsync(5000).ConfigureAwait(false);
            await semaphore2.WaitAsync(5000).ConfigureAwait(false);

            // Assert
            if (SharedUtilities.IsLinuxPlatform())
            {
                Assert.IsFalse(lockCreated, $"lockCreated {lockCreated} lockDeleted {lockDeleted}");
                Assert.IsFalse(lockDeleted, $"lockCreated {lockDeleted} lockDeleted {lockDeleted}");
            }
            else
            {
                Assert.IsTrue(lockCreated, $"lockCreated {lockCreated} lockDeleted {lockDeleted}");
                Assert.IsTrue(lockDeleted, $"lockCreated {lockDeleted} lockDeleted {lockDeleted}");
            }
        }

        [TestMethod]
        [DeploymentItem(@"Resources/token_cache_one_acc_seed.json")]

        public async Task EventFiresAsync()
        {
            string cacheWithOneUser = File.ReadAllText(
                ResourceHelper.GetTestResourceRelativePath("token_cache_one_acc_seed.json"));

            var properties = _storageCreationPropertiesBuilder
                .WithCacheChangedEvent(ClientId, "https://login.microsoftonline.com/common")
                .Build();

            if (File.Exists(properties.CacheFilePath))
            {
                File.Delete(properties.CacheFilePath);
            }

            var helper = await MsalCacheHelper.CreateAsync(properties).ConfigureAwait(true);
            var cache1 = new MockTokenCache();
            helper.RegisterCache(cache1);

            var semaphore = new SemaphoreSlim(0);
            int cacheChangedEventFired = 0;

            // event is fired asynchronously, test has to wait for it for a while
            helper.CacheChanged += (_, _) =>
            {
                semaphore.Release();
                cacheChangedEventFired++;
            };

            // Act - simulate that an external process writes to the token cache
            helper.CacheStore.WriteData(Encoding.UTF8.GetBytes(cacheWithOneUser));

            // Assert
            await semaphore.WaitAsync(5000).ConfigureAwait(false); // if event isn't fired in 5s, bail out
            Assert.AreEqual(1, cacheChangedEventFired);
        }

        [TestMethod]
        [DeploymentItem(@"Resources/token_cache_adfs.json")]
        [WorkItem(89)] // https://github.com/AzureAD/microsoft-authentication-extensions-for-dotnet/issues/89
        public async Task CacheWorksWithAdfsAsync()
        {
            // Arrange
            string cacheWithOneUser = File.ReadAllText(
                ResourceHelper.GetTestResourceRelativePath("token_cache_adfs.json"));

            var properties = _storageCreationPropertiesBuilder.Build();

            Storage storage = Storage.Create(properties, _logger);
            storage.WriteData(Encoding.UTF8.GetBytes(cacheWithOneUser));

            var helper = await MsalCacheHelper.CreateAsync(properties).ConfigureAwait(true);
            var pca = PublicClientApplicationBuilder
                .Create("PublicClientId")
                .WithAuthority("https://fs.msidlab8.com/adfs")
                .Build();
            helper.RegisterCache(pca.UserTokenCache);

            // Act
            var accounts = await pca.GetAccountsAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual(1, accounts.Count());
        }

        [TestMethod]
        [DeploymentItem(@"Resources/token_cache_one_acc_seed.json")]

        public async Task EventFires2Async()
        {
            string cacheWithOneUser = File.ReadAllText(
                ResourceHelper.GetTestResourceRelativePath("token_cache_one_acc_seed.json"));

            var properties = _storageCreationPropertiesBuilder
                .WithCacheChangedEvent(ClientId, "https://login.microsoftonline.com/common")
                .Build();

            if (File.Exists(properties.CacheFilePath))
            {
                File.Delete(properties.CacheFilePath);
            }

            var helper = await MsalCacheHelper.CreateAsync(properties).ConfigureAwait(true);
            var cache1 = new MockTokenCache();
            helper.RegisterCache(cache1);

            var semaphore = new SemaphoreSlim(0);
            int cacheChangedEventFired = 0;

            // event is fired asynchronously, test has to wait for it for a while
            helper.CacheChanged += (_, _) =>
            {
                semaphore.Release();
                cacheChangedEventFired++;
            };

            // Act - simulate that an external process writes to the token cache
            helper.CacheStore.WriteData(Encoding.UTF8.GetBytes(cacheWithOneUser));

            // Assert
            await semaphore.WaitAsync(5000).ConfigureAwait(false); // if event isn't fired in 5s, bail out
            Assert.AreEqual(1, cacheChangedEventFired);
        }

        [TestMethod]
        public async Task EventNeedsConfigurationAsync()
        {
            var properties = _storageCreationPropertiesBuilder.Build();

            if (File.Exists(properties.CacheFilePath))
            {
                File.Delete(properties.CacheFilePath);
            }

            var helper = await MsalCacheHelper.CreateAsync(properties).ConfigureAwait(true);

            // event is fired asynchronously, test has to wait for it for a while
            AssertException.Throws<InvalidOperationException>(
                () => helper.CacheChanged += (_, _) =>
            {
                Assert.Fail("Should not fire");
            });
        }
    }
}
