// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;

namespace Microsoft.Identity.Client.Http
{
    internal sealed class HttpRequestOperationContext : IDisposable
    {
        private readonly CancellationToken _callerCancellationToken;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private bool _timeoutInitialized;

        public HttpRequestOperationContext(CancellationToken cancellationToken)
        {
            _callerCancellationToken = cancellationToken;
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        }

        public CancellationToken CancellationToken => _cancellationTokenSource.Token;

        public int RetryCount { get; set; }

        public bool IsTimedOut =>
            _cancellationTokenSource.IsCancellationRequested &&
            !_callerCancellationToken.IsCancellationRequested;

        public void InitializeTimeout(TimeSpan timeout)
        {
            if (_timeoutInitialized)
            {
                return;
            }

            _timeoutInitialized = true;

            if (timeout != Timeout.InfiniteTimeSpan)
            {
                _cancellationTokenSource.CancelAfter(timeout);
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource.Dispose();
        }
    }
}
