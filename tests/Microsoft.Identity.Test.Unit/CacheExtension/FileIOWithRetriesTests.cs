// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Test.Common.Core.Helpers;

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

        [DoNotRunOnWindows]
        [TestMethod]
        public void CreateAndWriteToFile_SymlinkAtCachePath_ThrowsInvalidOperationException()
        {
            string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(dir);
            string realTarget = Path.Combine(dir, "real.bin");
            string symlinkPath = Path.Combine(dir, "cache.bin");
            File.WriteAllBytes(realTarget, Array.Empty<byte>());

#if NET6_0_OR_GREATER
            File.CreateSymbolicLink(symlinkPath, realTarget);
#else
            // net48 only runs on Windows where this test is already skipped by [DoNotRunOnWindows].
            using var proc = Process.Start(new ProcessStartInfo("ln", $"-s \"{realTarget}\" \"{symlinkPath}\""));
            proc?.WaitForExit();
#endif

            try
            {
                var ex = AssertException.Throws<InvalidOperationException>(() =>
                    FileIOWithRetries.CreateAndWriteToFile(
                        symlinkPath,
                        new byte[] { 1, 2, 3 },
                        setChmod600: true,
                        new TraceSourceLogger(_logger)));

                StringAssert.Contains(ex.Message, "symbolic link");
            }
            finally
            {
                Directory.Delete(dir, recursive: true);
            }
        }

        [DoNotRunOnWindows]
        [TestMethod]
        public void TouchFile_SymlinkAtCachePath_ThrowsInvalidOperationException()
        {
            string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(dir);
            string realTarget = Path.Combine(dir, "real.bin");
            string symlinkPath = Path.Combine(dir, "cache.bin");
            File.WriteAllBytes(realTarget, Array.Empty<byte>());

#if NET6_0_OR_GREATER
            File.CreateSymbolicLink(symlinkPath, realTarget);
#else
            using var proc = Process.Start(new ProcessStartInfo("ln", $"-s \"{realTarget}\" \"{symlinkPath}\""));
            proc?.WaitForExit();
#endif

            try
            {
                var ex = AssertException.Throws<InvalidOperationException>(() =>
                    FileIOWithRetries.TouchFile(
                        symlinkPath,
                        new TraceSourceLogger(_logger)));

                StringAssert.Contains(ex.Message, "symbolic link");
            }
            finally
            {
                Directory.Delete(dir, recursive: true);
            }
        }

        [TestMethod]
        public async Task Touch_FiresEvent_Async()
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
            watcher.Changed += (_, _) =>
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
                Assert.IsTrue(File.Exists(path));

                // LastWriteTime is not granular enough 
                await Task.Delay(50).ConfigureAwait(false);

                Assert.IsTrue(File.Exists(path));

                _logger.TraceInformation($"Semaphore at {semaphore.CurrentCount}");
                await semaphore.WaitAsync(5000).ConfigureAwait(false);

                Assert.AreEqual(1, cacheChangedEventFired);
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
