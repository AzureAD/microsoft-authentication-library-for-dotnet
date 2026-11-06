// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.UI;
using Uri = System.Uri;

namespace Microsoft.Identity.Client.Platforms.Android.SystemWebview
{
    [Preserve(AllMembers = true)]
    internal class SystemWebUI : WebviewBase
    {
        private readonly Activity _parent;

        public SystemWebUI(Activity parent)
        {
            _parent = parent;
        }

        public override async Task<AuthorizationResult> AcquireAuthorizationAsync(
            Uri authorizationUri,
            Uri redirectUri,
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            var pendingRequest = new SystemWebViewRequest(authorizationUri, redirectUri, requestContext);
            AuthenticationActivity.PendingRequest = pendingRequest;

            using (cancellationToken.Register(() => pendingRequest.Completion.TrySetCanceled(cancellationToken)))
            {
                try
                {
                    try
                    {
                        _parent.RunOnUiThread(() =>
                        {
                            if (pendingRequest.Completion.Task.IsCompleted ||
                                !ReferenceEquals(AuthenticationActivity.PendingRequest, pendingRequest))
                            {
                                requestContext.Logger.Info("Skipping a cancelled or superseded system browser launch.");
                                pendingRequest.Completion.TrySetCanceled();
                                return;
                            }

                            try
                            {
                                var manager = (ActivityManager)_parent.GetSystemService(Context.ActivityService);
                                pendingRequest.CallerTask = manager.AppTasks.FirstOrDefault(task =>
                                {
#if NET8_0_OR_GREATER
                                    if (OperatingSystem.IsAndroidVersionAtLeast(29))
                                    {
                                        return task.TaskInfo.TaskId == _parent.TaskId;
                                    }
#endif
                                    return task.TaskInfo.PersistentId == _parent.TaskId;
                                });
                                if (pendingRequest.CallerTask is null)
                                {
                                    requestContext.Logger.Warning("The calling activity's task is unavailable.");
                                }

                                _parent.StartActivity(pendingRequest.CreateIntent(_parent));
                            }
                            catch (Exception ex)
                            {
                                FailStartup(pendingRequest, ex);
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        FailStartup(pendingRequest, ex);
                    }

                    return await pendingRequest.Completion.Task.ConfigureAwait(false);
                }
                finally
                {
                    if (ReferenceEquals(AuthenticationActivity.PendingRequest, pendingRequest))
                    {
                        AuthenticationActivity.PendingRequest = null;
                    }
                }
            }
        }

        private static void FailStartup(SystemWebViewRequest request, Exception exception)
        {
            request.RequestContext.Logger.ErrorPii(exception);
            request.Completion.TrySetException(new MsalClientException(
                MsalError.AuthenticationUiFailedError,
                "AuthenticationActivity failed to start",
                exception));
        }

        public override Uri UpdateRedirectUri(Uri redirectUri)
        {
            RedirectUriHelper.Validate(redirectUri, usesSystemBrowser: true);
            return redirectUri;
        }
    }
}
