// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
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
        private readonly SemaphoreSlim _readerMutex;
        // A writer holds the turnstile until Release, preventing later readers from bypassing it.
        private readonly SemaphoreSlim _turnstile;
        private int _readers;

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

        public OptionalSemaphoreSlim(bool useRealSemaphore, bool allowSharedReaders = false)
        {
            _useRealSemaphore = useRealSemaphore;
            if (_useRealSemaphore)
            {
                _semaphoreSlim = new SemaphoreSlim(1, 1);
                if (allowSharedReaders)
                {
                    _readerMutex = new SemaphoreSlim(1, 1);
                    _turnstile = new SemaphoreSlim(1, 1);
                }
            }
            _noLockCurrentCount = 1;
        }

        public void Release()
        {
            if (_useRealSemaphore)
            {
                _semaphoreSlim.Release();
                _turnstile?.Release();
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
                if (_turnstile != null)
                {
                    return WaitForWriteAsync(cancellationToken);
                }
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
                _turnstile?.Wait();
                bool entered = false;
                try
                {
                    _semaphoreSlim.Wait();
                    entered = true;
                }
                finally
                {
                    if (!entered)
                    {
                        _turnstile?.Release();
                    }
                }
            }
            else
            {
                Interlocked.Decrement(ref _noLockCurrentCount);
            }
        }

        private async Task WaitForWriteAsync(CancellationToken cancellationToken)
        {
            await _turnstile.WaitAsync(cancellationToken).ConfigureAwait(false);
            bool entered = false;
            try
            {
                await _semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);
                entered = true;
            }
            finally
            {
                if (!entered)
                {
                    _turnstile.Release();
                }
            }
        }

        public async Task<IDisposable> AcquireReadAsync(CancellationToken cancellationToken)
        {
            if (_turnstile == null)
            {
                throw new NotSupportedException("Shared cache reads require synchronization enabled at cache construction.");
            }
            await _turnstile.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await _readerMutex.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    if (_readers == 0)
                    {
                        await _semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);
                    }
                    _readers++;
                }
                finally
                {
                    _readerMutex.Release();
                }
            }
            finally
            {
                _turnstile.Release();
            }
            return new ReadLease(this);
        }

        private void ReleaseRead()
        {
            _readerMutex.Wait();
            try
            {
                if (--_readers == 0)
                {
                    _semaphoreSlim.Release();
                }
            }
            finally
            {
                _readerMutex.Release();
            }
        }

        private sealed class ReadLease : IDisposable
        {
            private OptionalSemaphoreSlim _owner;
            internal ReadLease(OptionalSemaphoreSlim owner) => _owner = owner;
            public void Dispose() => Interlocked.Exchange(ref _owner, null)?.ReleaseRead();
        }
    }
}
