// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Client.Extensions.Msal.UnitTests
{
    [TestClass]
    public class MsalCacheHelperTests
    {
        public static readonly string CacheFilePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
        private readonly TraceSource _logger = new TraceSource("TestSource");
        private StorageCreationPropertiesBuilder _storageCreationPropertiesBuilder;

        [TestInitialize]
        public void TestInitialize()
        {
            _storageCreationPropertiesBuilder = new StorageCreationPropertiesBuilder(Path.GetFileName(CacheFilePath), Path.GetDirectoryName(CacheFilePath), "ClientIDGoesHere");
            _storageCreationPropertiesBuilder = _storageCreationPropertiesBuilder.WithMacKeyChain(serviceName: "Microsoft.Developer.IdentityService", accountName: "MSALCache");
            _storageCreationPropertiesBuilder = _storageCreationPropertiesBuilder.WithLinuxKeyring(
                schemaName: "msal.cache",
                collection: "default",
                secretLabel: "MSALCache",
                attribute1: new KeyValuePair<string, string>("MsalClientID", "Microsoft.Developer.IdentityService"),
                attribute2: new KeyValuePair<string, string>("MsalClientVersion", "1.0.0.0"));
        }

        [TestMethod]
        public void MultiAccessSerializationAsync()
        {
            var cache1 = new MockTokenCache();
            var helper1 = new MsalCacheHelper(
                cache1,
                new MsalCacheStorage(_storageCreationPropertiesBuilder.Build(), _logger),
                _logger);

            var cache2 = new MockTokenCache();
            var helper2 = new MsalCacheHelper(
                cache2,
                new MsalCacheStorage(_storageCreationPropertiesBuilder.Build(), _logger),
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
                var args = new TokenCacheNotificationArgs(cache1, string.Empty, null, false);

                helper1.BeforeAccessNotification(args);
                resetEvent3.Set();
                resetEvent1.Wait();
                helper1.AfterAccessNotification(args);
            });

            var thread2 = new Thread(() =>
            {
                var args = new TokenCacheNotificationArgs(cache2, string.Empty, null, false);
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
                new MsalCacheStorage(properties, _logger),
                _logger);

            var cache2 = new MockTokenCache();
            var helper2 = new MsalCacheHelper(
                cache2,
                new MsalCacheStorage(properties, _logger),
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
                var args = new TokenCacheNotificationArgs(cache1, string.Empty, null, false);

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
                var args = new TokenCacheNotificationArgs(cache2, string.Empty, null, false);
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
            helper._cacheWatcher.EnableRaisingEvents = false;

            // Intentionally write the file after creating the MsalCacheHelper to avoid the initial inner PCA being created only to read garbage
            string startString = "Something to start with";
            var startBytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(startString), optionalEntropy: null, scope: DataProtectionScope.CurrentUser);
            await File.WriteAllBytesAsync(properties.CacheFilePath, startBytes).ConfigureAwait(true);

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

            var args1 = new TokenCacheNotificationArgs(cache1, string.Empty, null, false);
            var args2 = new TokenCacheNotificationArgs(cache2, string.Empty, null, false);
            var args3 = new TokenCacheNotificationArgs(cache3, string.Empty, null, false);

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
    }
}
