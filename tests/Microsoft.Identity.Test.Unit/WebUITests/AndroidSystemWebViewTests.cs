// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Platforms.Android.SystemWebview;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Test.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.WebUITests
{
    [TestClass]
    [DoNotParallelize]
    public class AndroidSystemWebViewTests
    {
        private const string RedirectUri = "msaltest://auth/callback";
        private const string State = "pending-request-state";
        private const string AuthorizationUri = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize?state=" + State;
        private const string ValidResponse = RedirectUri + "?code=test-code&state=" + State;

        [TestCleanup]
        public void Cleanup()
        {
            AuthenticationActivity.PendingRequest?.Completion.TrySetCanceled();
            AuthenticationActivity.PendingRequest = null;
            Android.Util.Log.Warnings.Clear();
        }

        [TestMethod]
        public void OnlyBrowserCallbackIsExported()
        {
            // Arrange
            var internalActivity = typeof(AuthenticationActivity).GetCustomAttribute<ActivityAttribute>();
            var callbackActivity = typeof(BrowserTabActivity).GetCustomAttribute<ActivityAttribute>();

            // Act / Assert
            Assert.IsFalse(internalActivity.Exported);
            Assert.IsTrue(callbackActivity.Exported);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void LaunchWithoutPendingRequestFinishesWithoutOpeningBrowser(bool restored)
        {
            // Arrange
            var activity = CreateActivity();

            // Act
            activity.Create(restored ? new Bundle() : null);
            activity.Resume();

            // Assert
            Assert.IsTrue(activity.IsFinishing);
            Assert.IsEmpty(activity.StartedActivities);
            Assert.IsNull(activity.ResultCode);
            Assert.IsNotEmpty(Android.Util.Log.Warnings);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("https://unrelated.example/")]
        public void InvalidLaunchDoesNotOpenBrowser(string url)
        {
            // Arrange
            AuthenticationActivity.PendingRequest = CreateRequest();
            var activity = CreateActivity(url);

            // Act
            activity.Create();
            activity.Resume();

            // Assert
            Assert.IsTrue(activity.IsFinishing);
            Assert.IsEmpty(activity.StartedActivities);
        }

        [TestMethod]
        public void NullLaunchIntentFinishesWithoutOpeningBrowser()
        {
            // Arrange
            AuthenticationActivity.PendingRequest = CreateRequest();
            var activity = new TestAuthenticationActivity();

            // Act
            activity.Create();
            activity.Resume();

            // Assert
            Assert.IsTrue(activity.IsFinishing);
            Assert.IsEmpty(activity.StartedActivities);
            Assert.IsNotNull(AuthenticationActivity.PendingRequest);
            Assert.IsFalse(AuthenticationActivity.PendingRequest.Completion.Task.IsCompleted);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void InitializedActivityLaunchesExpectedBrowser(bool customTabs)
        {
            // Arrange
            AuthenticationActivity.PendingRequest = CreateRequest();
            var activity = CreateActivity();
            if (customTabs)
            {
                activity.PackageManager.Services.Add(new ResolveInfo
                {
                    ServiceInfo = new ServiceInfo { PackageName = "test.browser" }
                });
            }

            // Act
            activity.Create();
            activity.Resume();

            // Assert
            Assert.IsFalse(activity.IsFinishing);
            Assert.HasCount(1, activity.StartedActivities);
            Assert.AreEqual(AuthorizationUri, activity.StartedActivities[0].DataString);
            Assert.AreEqual(customTabs ? "test.browser" : null, activity.StartedActivities[0].Package);
        }

        [TestMethod]
        public void RequestClearedBeforeResumeDoesNotLaunchBrowser()
        {
            // Arrange
            AuthenticationActivity.PendingRequest = CreateRequest();
            var activity = CreateActivity();
            activity.Create();
            AuthenticationActivity.PendingRequest = null;

            // Act
            activity.Resume();

            // Assert
            Assert.IsTrue(activity.IsFinishing);
            Assert.IsEmpty(activity.StartedActivities);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void BrowserReturnOrRecreationCancelsPendingRequest(bool restored)
        {
            // Arrange
            var request = CreateRequest();
            AuthenticationActivity.PendingRequest = request;
            var activity = CreateActivity();
            activity.Create(restored ? new Bundle() : null);
            if (!restored)
            {
                activity.Resume();
            }

            // Act
            activity.Resume();

            // Assert
            Assert.IsTrue(activity.IsFinishing);
            Assert.AreEqual(AuthorizationStatus.UserCancel, request.Completion.Task.Result.Status);
            Assert.IsNull(activity.ResultCode);
            Assert.IsNull(AuthenticationActivity.PendingRequest);
        }

        [TestMethod]
        [DataRow(ValidResponse)]
        [DataRow(RedirectUri + "?error=access_denied&state=" + State)]
        public void MatchingResponseCompletesOnce(string url)
        {
            // Arrange
            var request = CreateRequest();
            AuthenticationActivity.PendingRequest = request;
            var activity = CreateActivity();
            activity.Create();
            activity.Resume();
            var response = request.CreateIntent(activity);
            response.PutExtra(AndroidConstants.CustomTabRedirect, url);

            // Act
            activity.Receive(response);
            activity.Resume();
            activity.Receive(response);

            // Assert
            Assert.IsTrue(activity.IsFinishing);
            Assert.AreEqual(AuthorizationResult.FromUri(url).Status, request.Completion.Task.Result.Status);
            Assert.AreEqual(AuthorizationResult.FromUri(url).Code, request.Completion.Task.Result.Code);
            Assert.IsNull(activity.ResultCode);
            Assert.IsNull(AuthenticationActivity.PendingRequest);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("not a URI")]
        [DataRow(RedirectUri + "?code=test-code")]
        [DataRow(RedirectUri + "?error=access_denied")]
        [DataRow(RedirectUri + "?code=test-code&state=other-request")]
        [DataRow(RedirectUri + "?error=access_denied&state=other-request")]
        [DataRow(RedirectUri + "?code=test-code&state=PENDING-REQUEST-STATE")]
        [DataRow("other://auth/callback?code=test-code&state=" + State)]
        [DataRow("msaltest://other/callback?code=test-code&state=" + State)]
        [DataRow("msaltest://auth:123/callback?code=test-code&state=" + State)]
        [DataRow("msaltest://auth/Callback?code=test-code&state=" + State)]
        [DataRow("msaltest://auth/callback/extra?code=test-code&state=" + State)]
        [DataRow("msaltest://user@auth/callback?code=test-code&state=" + State)]
        [DataRow(ValidResponse + "#unexpected")]
        public void UnboundCallbackDoesNotForwardOrCompletePendingRequest(string url)
        {
            // Arrange
            var request = CreateRequest();
            AuthenticationActivity.PendingRequest = request;
            var callback = new TestBrowserTabActivity { Intent = new Intent { DataString = url } };
            var activity = CreateActivity();
            activity.Create();
            activity.Resume();
            var response = request.CreateIntent(activity);
            response.PutExtra(AndroidConstants.CustomTabRedirect, url);

            // Act
            callback.Create();
            activity.Receive(response);

            // Assert
            Assert.IsTrue(callback.IsFinishing);
            Assert.IsEmpty(callback.StartedActivities);
            Assert.IsFalse(activity.IsFinishing);
            Assert.IsNull(activity.ResultCode);
            Assert.AreSame(request, AuthenticationActivity.PendingRequest);

            // A rejected callback must not consume the real response.
            response.PutExtra(AndroidConstants.CustomTabRedirect, ValidResponse);
            activity.Receive(response);
            Assert.AreEqual(AuthorizationStatus.Success, request.Completion.Task.Result.Status);
        }

        [TestMethod]
        public void CallbackWithoutPendingRequestDoesNotStartAuthenticationActivity()
        {
            // Arrange
            var callback = new TestBrowserTabActivity { Intent = new Intent { DataString = ValidResponse } };

            // Act
            callback.Create();

            // Assert
            Assert.IsTrue(callback.IsFinishing);
            Assert.IsEmpty(callback.StartedActivities);
        }

        [TestMethod]
        public void MatchingCallbackForwardsToInternalActivity()
        {
            // Arrange
            AuthenticationActivity.PendingRequest = CreateRequest();
            var callback = new TestBrowserTabActivity { Intent = new Intent { DataString = ValidResponse } };

            // Act
            callback.Create();

            // Assert
            Assert.IsTrue(callback.IsFinishing);
            Assert.HasCount(1, callback.StartedActivities);
            Intent forwarded = callback.StartedActivities[0];
            Assert.AreEqual(typeof(AuthenticationActivity), forwarded.ActivityType);
            Assert.AreEqual(ValidResponse, forwarded.GetStringExtra(AndroidConstants.CustomTabRedirect));
            Assert.IsTrue(AuthenticationActivity.PendingRequest.MatchesIntent(forwarded));
            Assert.AreEqual(AuthenticationActivity.PendingRequest.CreateIntent(callback).Flags, forwarded.Flags);
        }

        [TestMethod]
        public void OldActivityCannotCompleteReplacementRequest()
        {
            // Arrange
            AuthenticationActivity.PendingRequest = CreateRequest();
            var activity = CreateActivity();
            activity.Create();
            var replacement = CreateRequest();
            AuthenticationActivity.PendingRequest = replacement;
            var response = new Intent();
            response.PutExtra(AndroidConstants.CustomTabRedirect, ValidResponse);

            // Act
            activity.Receive(response);

            // Assert
            Assert.IsTrue(activity.IsFinishing);
            Assert.IsNull(activity.ResultCode);
            Assert.AreSame(replacement, AuthenticationActivity.PendingRequest);
        }

        [TestMethod]
        public void ConfiguredRedirectQueryAndEncodedStateArePreserved()
        {
            // Arrange
            var request = CreateRequest(
                "https://login.example/authorize?state=state%2Bvalue",
                "https://app.example/callback?tenant=example");

            // Act / Assert
            Assert.IsTrue(request.IsValidRedirect("https://APP.example/callback?code=test&state=state%2Bvalue&tenant=example"));
            Assert.IsFalse(request.IsValidRedirect("https://app.example/callback?code=test&state=state%2Bvalue"));
            Assert.IsFalse(request.IsValidRedirect("https://app.example/callback?code=test&state=state%2Bvalue&tenant=other"));
        }

        [TestMethod]
        [DataRow("https://login.example/authorize")]
        [DataRow("https://login.example/authorize?state=")]
        public void AuthorizationRequestWithoutStateIsRejected(string url)
        {
            // Arrange / Act / Assert
            Assert.ThrowsExactly<ArgumentException>(() => CreateRequest(url));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task RejectedCallbackDoesNotCancelRequestAndRealBrowserBackStillCancels(bool browserInSeparateTask)
        {
            // Arrange
            var parent = new Activity();
            Task<AuthorizationResult> result = AcquireAsync(parent);
            var activity = new TestAuthenticationActivity { Intent = parent.StartedActivities[0] };
            activity.Create();
            activity.Resume();

            // Model Android's NEW_DOCUMENT routing and task-affinity lookup. The callback
            // foregrounds the app task; a separate browser leaves the waiting activity exposed.
            bool isolated = activity.Intent.Flags.HasFlag(ActivityFlags.NewDocument) &&
                typeof(AuthenticationActivity).GetCustomAttribute<ActivityAttribute>().TaskAffinity == "";
            var callback = new TestBrowserTabActivity
            {
                Intent = new Intent { DataString = RedirectUri + "?error=access_denied&state=wrong" }
            };

            // Act
            callback.Create();
            if (!isolated && browserInSeparateTask)
            {
                activity.Resume();
            }

            // Assert
            Assert.IsFalse(result.IsCompleted, "Rejecting an unrelated callback must leave authentication pending.");
            Assert.IsEmpty(callback.StartedActivities);
            Assert.AreEqual(0, parent.ActivityManager.AppTasks[0].ForegroundCount);

            // A genuine browser Back must still cancel, even after the rejected callback.
            activity.Resume();
            Assert.AreEqual(AuthorizationStatus.UserCancel, (await result.ConfigureAwait(false)).Status);
            Assert.AreEqual(1, parent.ActivityManager.AppTasks[0].ForegroundCount);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task CancelledOrSupersededQueuedLaunchDoesNotStartOrCompleteReplacement(bool cancel)
        {
            // Arrange
            var parent = new Activity { QueueUiActions = true };
            using var cancellation = new CancellationTokenSource();
            Task<AuthorizationResult> first = AcquireAsync(parent, cancellation.Token);
            if (cancel)
            {
                cancellation.Cancel();
                await Assert.ThrowsExactlyAsync<TaskCanceledException>(() => first).ConfigureAwait(false);
            }

            Task<AuthorizationResult> second = AcquireAsync(parent);
            var replacement = AuthenticationActivity.PendingRequest;

            // Act
            while (parent.UiActions.Count > 0)
            {
                parent.UiActions.Dequeue()();
            }

            // Assert
            Assert.HasCount(1, parent.StartedActivities);
            Assert.IsTrue(replacement.MatchesIntent(parent.StartedActivities[0]));
            Assert.IsFalse(second.IsCompleted);
            await Assert.ThrowsExactlyAsync<TaskCanceledException>(() => first).ConfigureAwait(false);
            CompleteCallback(parent.StartedActivities[0]);
            Assert.AreEqual("test-code", (await second.ConfigureAwait(false)).Code);
        }

        [TestMethod]
        public async Task StaleLaunchAndDefaultActivityResultCannotCompleteReplacement()
        {
            // Arrange
            var parent = new Activity();
            using var cancellation = new CancellationTokenSource();
            Task<AuthorizationResult> first = AcquireAsync(parent, cancellation.Token);
            var stale = new TestAuthenticationActivity
            {
                Intent = parent.StartedActivities[0],
                ResultReceiver = (code, data) =>
                    AuthenticationContinuationHelper.SetAuthenticationContinuationEventArgs(0, code, data)
            };
            cancellation.Cancel();
            await Assert.ThrowsExactlyAsync<TaskCanceledException>(() => first).ConfigureAwait(false);
            Task<AuthorizationResult> second = AcquireAsync(parent);
            var replacement = AuthenticationActivity.PendingRequest;

            // Act: Finish delivers Android's implicit RESULT_CANCELED through the real helper.
            stale.Create();
            stale.Resume();

            // Assert
            Assert.IsTrue(stale.IsFinishing);
            Assert.IsFalse(second.IsCompleted);
            Assert.AreSame(replacement, AuthenticationActivity.PendingRequest);
            Assert.AreNotEqual(stale.Intent.DataString, parent.StartedActivities[1].DataString);
            CompleteCallback(parent.StartedActivities[1]);
            Assert.AreEqual("test-code", (await second.ConfigureAwait(false)).Code);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task MatchingCallbackCompletesRequestAndRestoresCallerTask(bool recreated)
        {
            // Arrange
            var parent = new Activity();
            Task<AuthorizationResult> result = AcquireAsync(parent);
            var activity = new TestAuthenticationActivity { Intent = parent.StartedActivities[0] };
            activity.Create();
            activity.Resume();
            var callback = new TestBrowserTabActivity { Intent = new Intent { DataString = ValidResponse } };

            // Act
            callback.Create();
            var forwarded = callback.StartedActivities[0];
            if (recreated)
            {
                activity = new TestAuthenticationActivity { Intent = forwarded };
                activity.Create();
            }
            else
            {
                activity.Receive(forwarded);
            }

            activity.Resume();

            // Assert
            Assert.AreEqual(parent.StartedActivities[0].DataString, forwarded.DataString);
            Assert.AreEqual("test-code", (await result.ConfigureAwait(false)).Code);
            Assert.AreEqual(1, parent.ActivityManager.AppTasks[0].ForegroundCount);
            Assert.IsNull(activity.ResultCode);
            Assert.IsTrue(activity.IsFinishing);
        }

        [TestMethod]
        public async Task RemovedCallerTaskDoesNotPreventCompletion()
        {
            // Arrange
            var parent = new Activity();
            Task<AuthorizationResult> result = AcquireAsync(parent);
            parent.ActivityManager.AppTasks[0].Removed = true;

            // Act
            CompleteCallback(parent.StartedActivities[0]);

            // Assert
            Assert.AreEqual(AuthorizationStatus.Success, (await result.ConfigureAwait(false)).Status);
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(true, false)]
        [DataRow(false, true)]
        public async Task ActivityStartupFailureFaultsPendingRequest(bool queued, bool dispatchFails)
        {
            // Arrange
            var parent = new Activity
            {
                QueueUiActions = queued,
                StartException = new ActivityNotFoundException(),
                DispatchException = dispatchFails ? new InvalidOperationException() : null
            };
            Task<AuthorizationResult> result = AcquireAsync(parent);

            // Act
            if (queued)
            {
                parent.UiActions.Dequeue()();
            }

            var exception = await Assert.ThrowsExactlyAsync<MsalClientException>(() => result).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(MsalError.AuthenticationUiFailedError, exception.ErrorCode);
            Assert.AreSame(dispatchFails ? parent.DispatchException : parent.StartException, exception.InnerException);
            Assert.IsNull(AuthenticationActivity.PendingRequest);
            Assert.IsEmpty(parent.StartedActivities);
        }

        private static Task<AuthorizationResult> AcquireAsync(Activity parent, CancellationToken cancellationToken = default)
        {
            var context = new RequestContext(TestCommon.CreateDefaultServiceBundle(), Guid.NewGuid(), null);
            return new SystemWebUI(parent).AcquireAuthorizationAsync(
                new Uri(AuthorizationUri), new Uri(RedirectUri), context, cancellationToken);
        }

        private static void CompleteCallback(Intent launch)
        {
            var activity = new TestAuthenticationActivity { Intent = launch };
            activity.Create();
            var callback = new TestBrowserTabActivity { Intent = new Intent { DataString = ValidResponse } };
            callback.Create();
            activity.Receive(callback.StartedActivities[0]);
        }

        private static SystemWebViewRequest CreateRequest(string authorizationUri = AuthorizationUri, string redirectUri = RedirectUri)
        {
            var context = new RequestContext(TestCommon.CreateDefaultServiceBundle(), Guid.NewGuid(), null);
            return new SystemWebViewRequest(new Uri(authorizationUri), new Uri(redirectUri), context);
        }

        private static TestAuthenticationActivity CreateActivity()
        {
            var intent = AuthenticationActivity.PendingRequest?.CreateIntent(new Context()) ?? new Intent();
            return new TestAuthenticationActivity { Intent = intent };
        }

        private static TestAuthenticationActivity CreateActivity(string activityUri)
        {
            var activity = CreateActivity();
            activity.Intent.DataString = activityUri;
            return activity;
        }

        private sealed class TestAuthenticationActivity : AuthenticationActivity
        {
            internal void Create(Bundle bundle = null) => OnCreate(bundle);
            internal void Resume() => OnResume();
            internal void Receive(Intent intent) => OnNewIntent(intent);
        }

        private sealed class TestBrowserTabActivity : BrowserTabActivity
        {
            internal void Create() => OnCreate(null);
        }
    }
}
