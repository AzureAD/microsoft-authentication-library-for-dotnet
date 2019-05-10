// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
