// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Platforms.Android.SystemWebview
{
    internal sealed class SystemWebViewRequest
    {
        private readonly Uri _redirectUri;
        private readonly string _state;
        private readonly string _activityUri = "msal-request://" + Guid.NewGuid().ToString("N");

        internal SystemWebViewRequest(Uri authorizationUri, Uri redirectUri, RequestContext requestContext)
        {
            AuthorizationUri = authorizationUri ?? throw new ArgumentNullException(nameof(authorizationUri));
            _redirectUri = redirectUri ?? throw new ArgumentNullException(nameof(redirectUri));
            RequestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));

            var parameters = CoreHelpers.ParseKeyValueList(authorizationUri.Query.TrimStart('?'), '&', true, null);
            if (!parameters.TryGetValue(OAuth2Parameter.State, out _state) || string.IsNullOrEmpty(_state))
            {
                throw new ArgumentException("The authorization URI must contain state.", nameof(authorizationUri));
            }
        }

        internal Uri AuthorizationUri { get; }
        internal RequestContext RequestContext { get; }
        internal ActivityManager.AppTask CallerTask { get; set; }
        internal TaskCompletionSource<AuthorizationResult> Completion { get; } =
            new TaskCompletionSource<AuthorizationResult>(TaskCreationOptions.RunContinuationsAsynchronously);

        internal Intent CreateIntent(Context context)
        {
            var intent = new Intent(context, typeof(AuthenticationActivity));
            intent.SetData(global::Android.Net.Uri.Parse(_activityUri));
            // Document identity keeps cancelled and replacement requests in different tasks.
            intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.NewDocument | ActivityFlags.ClearTop | ActivityFlags.SingleTop);
            return intent;
        }

        internal bool MatchesIntent(Intent intent) =>
            string.Equals(intent?.DataString, _activityUri, StringComparison.Ordinal);

        internal void RestoreCallerTask()
        {
            try
            {
                CallerTask?.MoveToFront();
            }
            catch (Java.Lang.IllegalArgumentException)
            {
                RequestContext.Logger.Warning("The calling activity's task no longer exists.");
            }
        }

        internal bool IsValidRedirect(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri redirect) ||
                !string.Equals(redirect.Scheme, _redirectUri.Scheme, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(redirect.Host, _redirectUri.Host, StringComparison.OrdinalIgnoreCase) ||
                redirect.Port != _redirectUri.Port ||
                !string.Equals(redirect.UserInfo, _redirectUri.UserInfo, StringComparison.Ordinal) ||
                !string.Equals(redirect.AbsolutePath, _redirectUri.AbsolutePath, StringComparison.Ordinal) ||
                !string.IsNullOrEmpty(redirect.Fragment))
            {
                return false;
            }

            // Validate errors as well as successful responses before they can complete the pending operation.
            var parameters = CoreHelpers.ParseKeyValueList(redirect.Query.TrimStart('?'), '&', true, null);
            if (!parameters.TryGetValue(OAuth2Parameter.State, out string state) ||
                !string.Equals(state, _state, StringComparison.Ordinal))
            {
                return false;
            }

            var expectedParameters = CoreHelpers.ParseKeyValueList(_redirectUri.Query.TrimStart('?'), '&', true, null);
            foreach (var parameter in expectedParameters)
            {
                if (!parameters.TryGetValue(parameter.Key, out string value) ||
                    !string.Equals(value, parameter.Value, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
