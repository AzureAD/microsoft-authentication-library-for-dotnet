// ------------------------------------------------------------------------------
// 
// Copyright (c) Microsoft Corporation.
// All rights reserved.
// 
// This code is licensed under the MIT License.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// ------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Identity.Client.CacheV2.Impl.Utils;

namespace Microsoft.Identity.Core.Platforms.net45.CacheV2
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