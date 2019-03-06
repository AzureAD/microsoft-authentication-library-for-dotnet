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

namespace CommonCache.Test.Common
{
    public sealed class ProcessRunningInfo : IProcessRunningInfo
    {
        private readonly Process _process;
        private bool _isDisposed;

        public ProcessRunningInfo(Process process, bool shouldTerminateProcessOnDispose)
        {
            _process = process;
            _process.Exited += (_, args) => RaiseHasExited(args);
            _isDisposed = false;
            ShouldTerminateProcessOnDispose = shouldTerminateProcessOnDispose;
        }

        public bool ShouldTerminateProcessOnDispose { get; }
        public int ExitCode => _process.ExitCode;
        public bool HasExited => _process.HasExited;
        public int Id => _process.Id;
        public event EventHandler Exited;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ProcessRunningInfo()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing && ShouldTerminateProcessOnDispose)
            {
                _process.Dispose();
            }

            _isDisposed = true;
        }

        private void RaiseHasExited(EventArgs args)
        {
            Exited?.Invoke(this, args);
        }
    }
}
