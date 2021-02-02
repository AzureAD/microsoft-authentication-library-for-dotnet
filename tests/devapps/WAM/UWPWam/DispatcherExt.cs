using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;

namespace UWP_standalone
{
    public static class DispatcherExtensions
    {
        private static async Task<T> RunOnUiThreadAsync<T>(CoreDispatcher dispatcher,
           Func<Task<T>> func, CoreDispatcherPriority priority = CoreDispatcherPriority.Normal)
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

        public static async Task<T> RunOnUiThreadAsync<T>(Func<Task<T>> func)
        {
            return await RunOnUiThreadAsync<T>(
                Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher,
                func, CoreDispatcherPriority.Normal).ConfigureAwait(true);
        }

        
    }
}
