// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    /// <summary>
    /// Regression tests for https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/6053
    /// 
    /// The bug: non-async lambda with `using var` on a linked CancellationTokenSource disposes
    /// it before the async operation completes, severing cancellation propagation. Background
    /// refresh tasks cannot be cancelled, leading to unbounded semaphore convoy and thread starvation.
    /// 
    /// Pattern affected (4 files):
    ///   ClientCredentialRequest.cs, OboRequest.cs, SilentRequest.cs, ManagedIdentityAuthRequest.cs
    /// 
    /// Buggy code:
    ///   () => {
    ///       using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(ct);
    ///       return GetAccessTokenAsync(tokenSource.Token); // non-async: using disposes IMMEDIATELY
    ///   }
    ///
    /// Fixed code:
    ///   async () => {
    ///       using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(ct);
    ///       return await GetAccessTokenAsync(tokenSource.Token); // async: disposes AFTER await
    ///   }
    ///
    /// Why these are PATTERN tests rather than product-code tests:
    /// For the Client Credentials path specifically, OAuth2Client.ExecuteRequestAsync uses
    /// requestContext.UserCancellationToken (the original caller's token) for HTTP calls,
    /// bypassing the linked CTS entirely. The bug's real impact is in the Managed Identity
    /// path where s_semaphoreSlim.WaitAsync(cancellationToken) uses the passed linked token,
    /// making semaphore waits uncancellable after disposal.
    /// These pattern tests verify the fundamental language behavior that causes the bug.
    /// </summary>
    [TestClass]
    public class LinkedCancellationTokenTests
    {
        /// <summary>
        /// Demonstrates the BUG: a non-async lambda with `using var` on a linked CTS
        /// causes disposal before the async operation completes.
        /// After disposal, cancellation from the parent does NOT propagate to the linked token.
        /// 
        /// This is the pattern found in 4 MSAL files before the fix.
        /// The semaphore simulates ManagedIdentityAuthRequest.s_semaphoreSlim where this causes starvation.
        /// </summary>
        [TestMethod]
        [Description("Bug pattern: non-async lambda disposes linked CTS before async work completes")]
        public async Task LinkedCts_NonAsyncLambda_CancellationDoesNotPropagate_BugPattern()
        {
            // Arrange
            using var parentCts = new CancellationTokenSource();
            var semaphore = new SemaphoreSlim(0, 1); // starts locked — simulates contention

            // RunContinuationsAsynchronously prevents TrySetResult from inlining the test's
            // continuation (which would call Cancel() BEFORE the lambda's using-var disposes).
            var operationStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            // This lambda mimics the buggy pattern in ProcessFetchInBackground
            Func<Task> buggyLambda = () =>
            {
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(parentCts.Token);
                // Return a Task without awaiting — `using var` disposes linkedCts HERE (at return)
                return WaitOnSemaphoreAsync(linkedCts.Token, semaphore, operationStarted);
            };

            // Act
            Task backgroundTask = Task.Run(buggyLambda);

            // Wait for the operation to start (semaphore wait is active)
            await operationStarted.Task.ConfigureAwait(false);

            // Small delay to guarantee the non-async lambda has returned and disposed the linked CTS.
            // With RunContinuationsAsynchronously, we resume on a different thread, but the
            // TaskRun thread may not have finished the `return` + `using var` disposal yet.
            await Task.Delay(100).ConfigureAwait(false);

            // Cancel the parent — this SHOULD propagate to the linked token...
            parentCts.Cancel();

            // Assert — with the bug, the semaphore wait is NOT cancelled because
            // the linked CTS was disposed (unregistered from parent).
            // The task hangs indefinitely. We use a timeout to detect this.
            Task completed = await Task.WhenAny(backgroundTask, Task.Delay(TimeSpan.FromSeconds(2)))
                .ConfigureAwait(false);

            // BUG: backgroundTask did NOT complete — cancellation didn't propagate
            Assert.AreNotEqual(backgroundTask, completed,
                "Expected the background task to NOT be cancelled (demonstrating the bug). " +
                "If this fails, the bug pattern is no longer reproducible.");

            // Cleanup: release the semaphore so the task can finish
            semaphore.Release();
            await backgroundTask.ConfigureAwait(false);
        }

        /// <summary>
        /// Demonstrates the FIX: an async lambda with `using var` on a linked CTS
        /// keeps it alive for the duration of the await.
        /// After the parent fires, cancellation DOES propagate to the linked token.
        /// 
        /// This is the pattern from PR #6054 (the fix).
        /// </summary>
        [TestMethod]
        [Description("Fix pattern: async lambda keeps linked CTS alive, cancellation propagates")]
        public async Task LinkedCts_AsyncLambda_CancellationPropagates_FixPattern()
        {
            // Arrange
            using var parentCts = new CancellationTokenSource();
            var semaphore = new SemaphoreSlim(0, 1); // starts locked
            var operationStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            // This lambda mimics the FIXED pattern (async + await)
            Func<Task> fixedLambda = async () =>
            {
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(parentCts.Token);
                // Await keeps linkedCts alive until the operation completes
                await WaitOnSemaphoreAsync(linkedCts.Token, semaphore, operationStarted)
                    .ConfigureAwait(false);
            };

            // Act
            Task backgroundTask = Task.Run(fixedLambda);

            // Wait for the operation to start
            await operationStarted.Task.ConfigureAwait(false);

            // Cancel the parent
            parentCts.Cancel();

            // Assert — with the fix, cancellation propagates and the semaphore wait is cancelled
            Task completed = await Task.WhenAny(backgroundTask, Task.Delay(TimeSpan.FromSeconds(2)))
                .ConfigureAwait(false);

            Assert.AreEqual(backgroundTask, completed,
                "Expected the background task to be cancelled promptly after parent cancellation. " +
                "The async lambda keeps the linked CTS alive, so cancellation should propagate.");

            // The task should have thrown OperationCanceledException
            Assert.IsTrue(backgroundTask.IsFaulted || backgroundTask.IsCanceled,
                $"Expected task to be faulted/cancelled but was: {backgroundTask.Status}");
        }

        /// <summary>
        /// Helper that simulates a long-running operation blocked on a semaphore.
        /// This models ManagedIdentityAuthRequest.s_semaphoreSlim.WaitAsync(cancellationToken)
        /// which is where the bug causes real thread starvation.
        /// </summary>
        private static async Task WaitOnSemaphoreAsync(
            CancellationToken cancellationToken,
            SemaphoreSlim semaphore,
            TaskCompletionSource<bool> operationStarted)
        {
            operationStarted.TrySetResult(true);
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
