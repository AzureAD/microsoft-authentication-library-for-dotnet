// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
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
            var globalName = InterprocessLock.GetMutexNameForAlias(aliasRaw, preferGlobal: true);
            var localName = InterprocessLock.GetMutexNameForAlias(aliasRaw, preferGlobal: false);

            StringAssert.StartsWith(globalName, @"Global\MSAL_MI_P_");
            StringAssert.StartsWith(localName, @"Local\MSAL_MI_P_");

            // Same alias after canonicalization should produce same suffix across scopes (ignoring prefix)
            var globalName2 = InterprocessLock.GetMutexNameForAlias("MY-ALIAS", preferGlobal: true);
            Assert.AreEqual(
                globalName.Substring(@"Global\".Length),
                globalName2.Substring(@"Global\".Length),
                "Canonicalized alias should produce the same hashed suffix.");
        }

        [TestMethod]
        public void TryWithAliasLock_Executes_Action()
        {
            if (!IsWindows)
            { Assert.Inconclusive("Windows-only"); return; }

            var alias = "lock-test-" + Guid.NewGuid().ToString("N");
            var called = 0;

            // Best-effort: short, non-configurable timeout. We intentionally do not retry here:
            // if the lock is busy we skip persistence and fall back to in-memory cache only,
            // so token acquisition is never blocked on certificate store operations.
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

        [TestMethod]
        public void TryWithAliasLock_NullAndEmptyAlias_DoNotThrow()
        {
            // null alias
            int nullCalls = 0;
            bool nullResult = InterprocessLock.TryWithAliasLock(
                null,
                TimeSpan.FromSeconds(2),
                () => Interlocked.Increment(ref nullCalls));

            // empty/whitespace alias
            int emptyCalls = 0;
            bool emptyResult = InterprocessLock.TryWithAliasLock(
                "   ",
                TimeSpan.FromSeconds(2),
                () => Interlocked.Increment(ref emptyCalls));

            Assert.IsTrue(nullResult, "Null alias should still execute the action.");
            Assert.AreEqual(1, nullCalls);

            Assert.IsTrue(emptyResult, "Whitespace alias should still execute the action.");
            Assert.AreEqual(1, emptyCalls);
        }

        [TestMethod]
        public void TryWithAliasLock_VeryLongAlias_DoesNotThrow()
        {
            string veryLongAlias = new string('a', 10_000);
            int calls = 0;

            bool result = InterprocessLock.TryWithAliasLock(
                veryLongAlias,
                TimeSpan.FromSeconds(2),
                () => Interlocked.Increment(ref calls));

            Assert.IsTrue(result);
            Assert.AreEqual(1, calls);
        }

        [TestMethod]
        public void TryWithAliasLock_MultipleConcurrentAttempts_AreSerialized()
        {
            const string alias = "concurrent-alias";
            int inCritical = 0;
            int maxInCritical = 0;
            int executed = 0;

            var tasks = new List<Task>();

            for (int i = 0; i < 8; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    bool acquired = InterprocessLock.TryWithAliasLock(
                        alias,
                        TimeSpan.FromSeconds(5),
                        () =>
                        {
                            var current = Interlocked.Increment(ref inCritical);
                            maxInCritical = Math.Max(maxInCritical, current);

                            // simulate some work under the lock
                            Thread.Sleep(50);

                            Interlocked.Decrement(ref inCritical);
                            Interlocked.Increment(ref executed);
                        });

                    Assert.IsTrue(acquired, "Each caller should acquire the alias lock within timeout.");
                }));
            }

            Task.WaitAll(tasks.ToArray());

            Assert.AreEqual(8, executed, "All actions should have executed.");
            Assert.AreEqual(1, maxInCritical, "At most one action should be in the critical section at a time.");
        }

        [TestMethod]
        public void TryWithAliasLock_ActionThrows_ReturnsFalse_AndLockReleased()
        {
            if (!IsWindows)
            { Assert.Inconclusive("Windows-only"); return; }

            const string alias = "exception-alias";
            int attempts = 0;

            // First call: action throws; InterprocessLock should catch it and return false.
            bool firstResult = InterprocessLock.TryWithAliasLock(
                alias,
                TimeSpan.FromSeconds(2),
                () =>
                {
                    Interlocked.Increment(ref attempts);
                    throw new InvalidOperationException("boom");
                });

            Assert.IsFalse(firstResult, "TryWithAliasLock should return false when the action delegate throws.");
            Assert.AreEqual(1, attempts, "Action should have executed exactly once.");

            // Second call: lock must be usable again even after the exception.
            int secondAttempts = 0;
            bool secondResult = InterprocessLock.TryWithAliasLock(
                alias,
                TimeSpan.FromSeconds(2),
                () => Interlocked.Increment(ref secondAttempts));

            Assert.IsTrue(secondResult, "Lock should be usable again after an exception in the action.");
            Assert.AreEqual(1, secondAttempts, "Second call should execute exactly once.");
        }
    }
}
