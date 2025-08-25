// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.ManagedIdentity.KeyProviders;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class InMemoryManagedIdentityKeyProviderTests
    {
        [TestMethod]
        public async Task ReturnsRsa2048_AndCaches_Success()
        {
            var p = new InMemoryManagedIdentityKeyProvider();

            ManagedIdentityKeyInfo k1 = await p.GetOrCreateKeyAsync(CancellationToken.None).ConfigureAwait(false);
            ManagedIdentityKeyInfo k2 = await p.GetOrCreateKeyAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.IsNotNull(k1);
            Assert.AreSame(k1, k2, "Provider should cache the same ManagedIdentityKeyInfo instance per process.");
            Assert.IsInstanceOfType(k1.Key, typeof(RSA));
            Assert.IsTrue(k1.Key.KeySize >= 2048);
            Assert.AreEqual(ManagedIdentityKeyType.InMemory, k1.Type);
        }

        [TestMethod]
        public async Task Concurrency_SingleCreation()
        {
            var p = new InMemoryManagedIdentityKeyProvider();

            var tasks = Enumerable.Range(0, 32)
                .Select(_ => p.GetOrCreateKeyAsync(CancellationToken.None))
                .ToArray();

            await Task.WhenAll(tasks).ConfigureAwait(false);

            var first = tasks[0].Result;
            foreach (var t in tasks)
            {
                Assert.AreSame(first, t.Result, "All concurrent calls should return the same cached ManagedIdentityKeyInfo.");
            }
        }

        [TestMethod]
        public async Task Rsa_SignsAndVerifies()
        {
            var p = new InMemoryManagedIdentityKeyProvider();
            var mi = await p.GetOrCreateKeyAsync(CancellationToken.None).ConfigureAwait(false);

            byte[] data = System.Text.Encoding.UTF8.GetBytes("ping");
            byte[] sig = mi.Key.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            bool ok = mi.Key.VerifyData(data, sig, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            Assert.IsTrue(ok);
        }

        [TestMethod]
        public async Task Cancellation_BeforeCreation_Throws_And_SubsequentCallSucceeds()
        {
            var p = new InMemoryManagedIdentityKeyProvider();

            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel(); // Cancel before calling to ensure WaitAsync observes cancellation.

                await Assert.ThrowsExceptionAsync<TaskCanceledException>(
                    () => p.GetOrCreateKeyAsync(cts.Token)).ConfigureAwait(false);
            }

            // Subsequent non-cancelled call should create and cache the key.
            var keyInfo = await p.GetOrCreateKeyAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.IsNotNull(keyInfo);
            Assert.IsNotNull(keyInfo.Key);
            Assert.AreEqual(ManagedIdentityKeyType.InMemory, keyInfo.Type);
        }

        [TestMethod]
        public async Task Cancellation_AfterCache_ReturnsCachedKey_IgnoringCancellation()
        {
            var p = new InMemoryManagedIdentityKeyProvider();
            var first = await p.GetOrCreateKeyAsync(CancellationToken.None).ConfigureAwait(false);

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Because the key is already cached, provider should return it without throwing.
            var second = await p.GetOrCreateKeyAsync(cts.Token).ConfigureAwait(false);

            Assert.AreSame(first, second);
            Assert.IsNotNull(second.Key);
        }
    }
}
