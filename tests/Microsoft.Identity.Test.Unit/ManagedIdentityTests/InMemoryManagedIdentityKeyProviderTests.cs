// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.ManagedIdentity.KeyProviders;
using Microsoft.Identity.Client.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute; // For Substitute.For<T>()

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class InMemoryManagedIdentityKeyProviderTests
    {
        private static (InMemoryManagedIdentityKeyProvider keyProvider, ILoggerAdapter logger) CreateKeyProviderAndLogger()
        {
            return (new InMemoryManagedIdentityKeyProvider(), Substitute.For<ILoggerAdapter>());
        }

        [TestMethod]
        public async Task ReturnsRsa2048_AndCaches_Success()
        {
            var (keyProvider, logger) = CreateKeyProviderAndLogger();

            ManagedIdentityKeyInfo k1 = await keyProvider.GetOrCreateKeyAsync(logger, CancellationToken.None).ConfigureAwait(false);
            ManagedIdentityKeyInfo k2 = await keyProvider.GetOrCreateKeyAsync(logger, CancellationToken.None).ConfigureAwait(false);

            Assert.IsNotNull(k1);
            Assert.AreSame(k1, k2, "Provider should cache the same ManagedIdentityKeyInfo instance per process.");
            Assert.IsInstanceOfType(k1.Key, typeof(RSA));
            Assert.IsGreaterThanOrEqualTo(Constants.RsaKeySize, k1.Key.KeySize);
            Assert.AreEqual(ManagedIdentityKeyType.InMemory, k1.Type);
        }

        [TestMethod]
        public async Task Concurrency_SingleCreation()
        {
            var (keyProvider, logger) = CreateKeyProviderAndLogger();

            var tasks = Enumerable.Range(0, 32)
                .Select(_ => keyProvider.GetOrCreateKeyAsync(logger, CancellationToken.None))
                .ToArray();

            await Task.WhenAll(tasks).ConfigureAwait(false);

            var first = tasks[0].Result;
            foreach (var task in tasks)
            {
                Assert.AreSame(first, task.Result, "All concurrent calls should return the same cached ManagedIdentityKeyInfo.");
            }
        }

        [TestMethod]
        public async Task Rsa_SignsAndVerifies()
        {
            var (keyProvider, logger) = CreateKeyProviderAndLogger();

            var managedIdentityApp = await keyProvider.GetOrCreateKeyAsync(logger, CancellationToken.None).ConfigureAwait(false);

            byte[] data = Encoding.UTF8.GetBytes("ping");
            byte[] signature = managedIdentityApp.Key.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            bool isSignatureValid = managedIdentityApp.Key.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            Assert.IsTrue(isSignatureValid);
        }

        [TestMethod]
        public async Task Cancellation_BeforeCreation_Throws_And_SubsequentCallSucceeds()
        {
            var (keyProvider, logger) = CreateKeyProviderAndLogger();

            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel(); // Pre-cancel so WaitAsync throws TaskCanceledException.

                await Assert.ThrowsAsync<TaskCanceledException>(
                    () => keyProvider.GetOrCreateKeyAsync(logger, cts.Token)).ConfigureAwait(false);
            }

            // Subsequent non-cancelled call should create and cache the key.
            var keyInfo = await keyProvider.GetOrCreateKeyAsync(logger, CancellationToken.None).ConfigureAwait(false);
            Assert.IsNotNull(keyInfo);
            Assert.IsNotNull(keyInfo.Key);
            Assert.AreEqual(ManagedIdentityKeyType.InMemory, keyInfo.Type);
        }

        [TestMethod]
        public async Task Cancellation_AfterCache_ReturnsCachedKey_IgnoringCancellation()
        {
            var (keyProvider, logger) = CreateKeyProviderAndLogger();

            ManagedIdentityKeyInfo k1 = await keyProvider.GetOrCreateKeyAsync(logger, CancellationToken.None).ConfigureAwait(false);

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Cached path should not throw.
            ManagedIdentityKeyInfo k2 = await keyProvider.GetOrCreateKeyAsync(logger, cts.Token).ConfigureAwait(false);

            Assert.AreSame(k1, k2);
            Assert.IsNotNull(k2.Key);
        }
    }
}
