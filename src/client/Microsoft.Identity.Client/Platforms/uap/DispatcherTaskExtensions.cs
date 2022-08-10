// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Windows.UI.Core;

namespace Microsoft.Identity.Client.Platforms.uap
{
    internal static class DispatcherTaskExtensions
    {
        public static async Task<T> RunTaskAsync<T>(this CoreDispatcher dispatcher,
            Func<Task<T>> func, CoreDispatcherPriority priority = CoreDispatcherPriority.High)
        {
            var taskCompletionSource = new TaskCompletionSource<T>();
#pragma warning disable VSTHRD101 // Avoid using async lambda for a void returning delegate type, because any exceptions not handled by the delegate will crash the process
            await dispatcher.RunAsync(priority, async () =>
            {
                try
                {
                    taskCompletionSource.SetResult(await func().ConfigureAwait(false));
                }
                catch (Exception ex)
                {
                    taskCompletionSource.SetException(ex);
                }
            });
#pragma warning restore VSTHRD101 // Avoid using async lambda for a void returning delegate type, because any exceptions not handled by the delegate will crash the process

            return await taskCompletionSource.Task.ConfigureAwait(false);
        }
    }
}
