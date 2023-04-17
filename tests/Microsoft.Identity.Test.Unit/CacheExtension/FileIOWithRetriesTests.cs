// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CacheExtension
{
    [TestClass]
    public class FileIOWithRetriesTests
    {
        private TraceSource _logger;
        private TraceStringListener _testListener;

        [TestInitialize]
        public void TestInitialize()
        {
            _logger = new TraceSource("TestSource", SourceLevels.All);
            _testListener = new TraceStringListener();

            _logger.Listeners.Add(_testListener);
        }

        [TestMethod]
        public async Task Touch_FiresEvents_Async()
        {
            _logger.TraceInformation($"Starting test on " + TestHelper.GetOs());

            // a directory and a path that do not exist
            string dir = Path.Combine(Directory.GetCurrentDirectory(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(dir);
            string fileName = "testFile";
            string path = Path.Combine(dir, fileName);

            FileSystemWatcher watcher = new FileSystemWatcher(dir, fileName);
            watcher.EnableRaisingEvents = true;
            var semaphore = new SemaphoreSlim(0);
            int cacheChangedEventFired = 0;

            // expect this event to be fired twice
            watcher.Changed += (sender, args) =>
            {
                _logger.TraceInformation("Event fired!");
                cacheChangedEventFired++;
                semaphore.Release();
            };

            Assert.IsFalse(File.Exists(path));
            try
            {
                _logger.TraceInformation($"Touch once");

                FileIOWithRetries.TouchFile(path, new TraceSourceLogger(_logger));
                DateTime initialLastWriteTime = File.GetLastWriteTimeUtc(path);
                Assert.IsTrue(File.Exists(path));

                // LastWriteTime is not granular enough 
                await Task.Delay(50).ConfigureAwait(false);

                _logger.TraceInformation($"Touch twice");
                FileIOWithRetries.TouchFile(path, new TraceSourceLogger(_logger));
                Assert.IsTrue(File.Exists(path));

                DateTime subsequentLastWriteTime = File.GetLastWriteTimeUtc(path);
                Assert.IsTrue(subsequentLastWriteTime > initialLastWriteTime);

                _logger.TraceInformation($"Semaphore at {semaphore.CurrentCount}");
                await semaphore.WaitAsync(5000).ConfigureAwait(false);
                _logger.TraceInformation($"Semaphore at {semaphore.CurrentCount}");
                await semaphore.WaitAsync(10000).ConfigureAwait(false); 
                Assert.IsTrue(
                    cacheChangedEventFired==2 ||
                    cacheChangedEventFired == 3,
                    "Expecting the event to be fired 2 times as the file is touched 2 times. On Linux, " +
                    "the file watcher sometimes fires an additional time for the file creation");
            }
            finally
            {
                _logger.TraceInformation("Cleaning up");
                Trace.WriteLine(_testListener.CurrentLog);
                File.Delete(path);
            }
        }
    }
}
