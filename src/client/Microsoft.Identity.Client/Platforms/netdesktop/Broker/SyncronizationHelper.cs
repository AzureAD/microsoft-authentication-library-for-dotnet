using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.Identity.Client.Platforms.netdesktop.Broker
{
    /// <summary>
    /// Based on https://thomaslevesque.com/2015/11/11/explicitly-switch-to-the-ui-thread-in-an-async-method/
    /// Makes the syncronization context awaitable
    /// </summary>
    internal static class SynchronizationContextExtensions
    {
        public static SynchronizationContextAwaiter GetAwaiter(this SynchronizationContext context)
        {
            return new SynchronizationContextAwaiter(context);
        }
    }

    internal struct SynchronizationContextAwaiter : INotifyCompletion
    {
        private static readonly SendOrPostCallback _postCallback = state => ((Action)state)();

        private readonly SynchronizationContext _context;
        public SynchronizationContextAwaiter(SynchronizationContext context)
        {
            _context = context;
        }

        public bool IsCompleted => _context == SynchronizationContext.Current;

        public void OnCompleted(Action continuation) => _context.Post(_postCallback, continuation);

        public void GetResult() { }
    }
}
