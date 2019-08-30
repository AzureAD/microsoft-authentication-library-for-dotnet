// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Identity.Client.CacheV2.Impl.Utils;

namespace Microsoft.Identity.Client.Platforms.net45.CacheV2
{
    internal class FileMutex : IDisposable
    {
        public const string MutexNamePrefix = "Global\\3C98A163-8D7E-41B1-B10A-97B5BD62B79F";
        private const int MutexTimeoutSeconds = 16;
        private readonly string _mutexName;
        private Mutex _systemMutex;

        public FileMutex(string relativePath)
        {
            _mutexName = MakeMutexName(relativePath);
            if (!TryLock())
            {
                throw new InvalidOperationException("unable to lock mutex");
            }
        }

        public void Dispose()
        {
            Unlock();
        }

        public void Unlock()
        {
            if (_systemMutex != null)
            {
                Debug.WriteLine($"MUTEX RELEASED: ({Thread.CurrentThread.ManagedThreadId}) {_mutexName}");
                _systemMutex.ReleaseMutex();
                _systemMutex = null;
            }
        }

        public static string MakeMutexName(string relativePath)
        {
            return MutexNamePrefix + PathUtils.Normalize(relativePath);
        }

        public bool TryLock(int millisecondsTimeout = MutexTimeoutSeconds * 1000)
        {
            Unlock();

            _systemMutex = new Mutex(false, _mutexName);

            Debug.WriteLine($"Attempting to lock mutex: ({Thread.CurrentThread.ManagedThreadId}) {_mutexName}");
            if (_systemMutex.WaitOne(millisecondsTimeout))
            {
                Debug.WriteLine($"MUTEX ACQUIRED: ({Thread.CurrentThread.ManagedThreadId}) {_mutexName}");
                return true;
            }

            _systemMutex.Dispose();
            _systemMutex = null;

            return false;
        }
    }
}
