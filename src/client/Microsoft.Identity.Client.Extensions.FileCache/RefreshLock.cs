// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;

namespace Microsoft.Identity.Client.Extensions.FileCache
{
    internal interface IRefreshLock : IDisposable
    {
        bool TryEnter(TimeSpan timeout);
        void Exit();
    }

    internal interface IRefreshLockFactory
    {
        IRefreshLock ForKey(string scope, string bucket, string keyId);
    }

    internal sealed class NamedMutexRefreshLockFactory : IRefreshLockFactory
    {
        public IRefreshLock ForKey(string scope, string bucket, string keyId)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(scope + "::" + bucket + "::" + keyId));
                var suffix = ToHex16(bytes); // works on net8.0 + netstandard2.0
                var name = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? @"Global\MSAL_FileCache_" + suffix
                    : "MSAL_FileCache_" + suffix;
                return new NamedMutexRefreshLock(name);
            }
        }

        private static string ToHex16(byte[] bytes)
        {
            // first 16 bytes as hex
            int take = Math.Min(16, bytes.Length);
            var sb = new StringBuilder(take * 2);
            for (int i = 0; i < take; i++)
            {
                sb.Append(bytes[i].ToString("X2"));
            }
            return sb.ToString();
        }

        private sealed class NamedMutexRefreshLock : IRefreshLock
        {
            private readonly Mutex _mtx;
            internal NamedMutexRefreshLock(string name) { _mtx = new Mutex(false, name); }

            public bool TryEnter(TimeSpan timeout)
            {
                try
                { return _mtx.WaitOne(timeout); }
                catch (AbandonedMutexException) { return true; }
            }

            public void Exit()
            {
                try
                { _mtx.ReleaseMutex(); }
                catch { /* ignore */ }
            }

            public void Dispose() { _mtx.Dispose(); }
        }
    }
}
