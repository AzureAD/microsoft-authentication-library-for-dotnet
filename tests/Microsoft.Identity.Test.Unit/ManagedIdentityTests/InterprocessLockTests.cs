// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Identity.Client.ManagedIdentity.V2;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class InterprocessLockTests
    {
        private static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        [TestMethod]
        public void GetMutexName_Format_And_Canonicalization()
        {
            if (!IsWindows)
            { Assert.Inconclusive("Windows-only"); return; }

            var aliasRaw = "  my-alias  ";
            var g = InterprocessLock.GetMutexNameForAlias(aliasRaw, preferGlobal: true);
            var l = InterprocessLock.GetMutexNameForAlias(aliasRaw, preferGlobal: false);

            StringAssert.StartsWith(g, @"Global\MSAL_MI_P_");
            StringAssert.StartsWith(l, @"Local\MSAL_MI_P_");

            // Same alias after canonicalization should produce same suffix across scopes (ignoring prefix)
            var g2 = InterprocessLock.GetMutexNameForAlias("MY-ALIAS", true);
            Assert.AreEqual(g.Substring(@"Global\".Length), g2.Substring(@"Global\".Length));
        }

        [TestMethod]
        public void TryWithAliasLock_Executes_Action()
        {
            if (!IsWindows)
            { Assert.Inconclusive("Windows-only"); return; }

            var alias = "lock-test-" + Guid.NewGuid().ToString("N");
            var called = 0;

            var ok = InterprocessLock.TryWithAliasLock(
                alias,
                timeout: TimeSpan.FromMilliseconds(250),
                action: () => Interlocked.Increment(ref called));

            Assert.IsTrue(ok);
            Assert.AreEqual(1, called);
        }

        [TestMethod]
        public void TryWithAliasLock_Contention_Skips_IfBusy()
        {
            if (!IsWindows)
            { Assert.Inconclusive("Windows-only"); return; }

            var alias = "lock-busy-" + Guid.NewGuid().ToString("N");
            using var gate = new ManualResetEventSlim(false);

            // Thread A: hold the lock for ~500ms
            var t = new Thread(() =>
            {
                InterprocessLock.TryWithAliasLock(
                    alias,
                    timeout: TimeSpan.FromMilliseconds(250),
                    action: () =>
                    {
                        gate.Set();              // signal ready
                        Thread.Sleep(500);       // hold the lock
                    });
            });
            t.IsBackground = true;
            t.Start();

            // Wait until A holds the lock
            Assert.IsTrue(gate.Wait(2000));

            // Thread B: attempt with small timeout, expect "busy" (returns false)
            var got = InterprocessLock.TryWithAliasLock(
                alias,
                timeout: TimeSpan.FromMilliseconds(50),
                action: () => Assert.Fail("Should not enter under contention"));

            Assert.IsFalse(got);

            t.Join();
        }
    }
}
