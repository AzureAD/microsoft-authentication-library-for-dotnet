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
            return await taskCompletionSource.Task.ConfigureAwait(false);
        }
    }
}
