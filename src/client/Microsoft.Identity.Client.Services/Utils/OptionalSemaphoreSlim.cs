// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Logger;

namespace Microsoft.Identity.Client.Utils
{
    /// <summary>
    /// An object that either wraps a SemaphoreSlim for synchronization or ignores synchronization completely and just keeps track of Wait / Release operations.
    /// </summary>
    internal class OptionalSemaphoreSlim
    {
        private readonly bool _useRealSemaphore;
        private int _noLockCurrentCount;
        private SemaphoreSlim _semaphoreSlim;

        public int CurrentCount
        {
            get
            {
                return _useRealSemaphore ? _semaphoreSlim.CurrentCount : _noLockCurrentCount;
            }
        }

        public string GetCurrentCountLogMessage()
        {
            return $"Real semaphore: {_useRealSemaphore}. Count: { CurrentCount}";
        }

        public OptionalSemaphoreSlim(bool useRealSemaphore)
        {
            _useRealSemaphore = useRealSemaphore;
            if (_useRealSemaphore)
            {
                _semaphoreSlim = new SemaphoreSlim(1, 1);
            }
            _noLockCurrentCount = 1;
        }

        public void Release()
        {
            if (_useRealSemaphore)
            {
                _semaphoreSlim.Release();
            }
            else
            {
                Interlocked.Increment(ref _noLockCurrentCount);
            }
        }

        public Task WaitAsync(CancellationToken cancellationToken)
        {
            if (_useRealSemaphore)
            {
                return _semaphoreSlim.WaitAsync(cancellationToken);
            }
            else
            {
                Interlocked.Decrement(ref _noLockCurrentCount);
                return Task.FromResult(true);
            }
        }
        
        public void Wait()
        {
            if (_useRealSemaphore)
            {
                _semaphoreSlim.Wait();
            }
            else
            {
                Interlocked.Decrement(ref _noLockCurrentCount);
            }
        }
    }
}
