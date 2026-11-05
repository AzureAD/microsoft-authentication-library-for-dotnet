// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Reflection;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Platforms.Android.SystemWebview;
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
            Assert.IsNull(AuthenticationActivity.PendingRequest);
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
            AuthenticationActivity.PendingRequest = CreateRequest();
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
            Assert.AreEqual(AndroidConstants.Cancel, (int)activity.ResultCode.Value);
            Assert.IsNull(AuthenticationActivity.PendingRequest);
        }

        [TestMethod]
        [DataRow(ValidResponse)]
        [DataRow(RedirectUri + "?error=access_denied&state=" + State)]
        public void MatchingResponseCompletesOnce(string url)
        {
            // Arrange
            AuthenticationActivity.PendingRequest = CreateRequest();
            var activity = CreateActivity();
            activity.Create();
            activity.Resume();
            var response = new Intent();
            response.PutExtra(AndroidConstants.CustomTabRedirect, url);

            // Act
            activity.Receive(response);
            activity.Resume();
            activity.Receive(response);

            // Assert
            Assert.IsTrue(activity.IsFinishing);
            Assert.AreEqual(AndroidConstants.AuthCodeReceived, (int)activity.ResultCode.Value);
            Assert.AreEqual(url, activity.ResultData.GetStringExtra(AndroidConstants.AuthorizationFinalUrl));
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
            var response = new Intent();
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
            Assert.AreEqual(AndroidConstants.AuthCodeReceived, (int)activity.ResultCode.Value);
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
            Assert.AreEqual(ActivityFlags.ClearTop | ActivityFlags.SingleTop, forwarded.Flags);
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

        private static SystemWebViewRequest CreateRequest(string authorizationUri = AuthorizationUri, string redirectUri = RedirectUri)
        {
            var context = new RequestContext(TestCommon.CreateDefaultServiceBundle(), Guid.NewGuid(), null);
            return new SystemWebViewRequest(new Uri(authorizationUri), new Uri(redirectUri), context);
        }

        private static TestAuthenticationActivity CreateActivity(string url = AuthorizationUri)
        {
            var intent = new Intent();
            intent.PutExtra(AndroidConstants.RequestUrlKey, url);
            return new TestAuthenticationActivity { Intent = intent };
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
