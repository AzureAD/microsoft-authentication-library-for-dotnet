// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace Microsoft.Identity.Client.Extensions.Msal
{
    /// <summary>
    /// A cross-process lock that works on all platforms, implemented using files.
    /// Does not ensure thread safety, i.e. 2 threads from the same process will pass through this lock.
    /// </summary>
    /// <remarks>
    /// Thread locking should be done using <see cref="SemaphoreSlim"/> or another such primitive.
    /// </remarks>
    public sealed class CrossPlatLock : IDisposable
    {
        internal const int LockfileRetryDelayDefault = 100;
        internal const int LockfileRetryCountDefault = 60000 / LockfileRetryDelayDefault;
        private FileStream _lockFileStream;

        /// <summary>
        /// Creates a file lock and maintains it until the lock is disposed. Any other process trying to get the lock will wait (spin waiting) until the lock is released. 
        /// Works on Windows, Mac and Linux.
        /// </summary>
        /// <param name="lockfilePath">The path of the lock file, e.g. {MsalCacheHelper.UserRootDirectory}/MyAppsSecrets.lockfile </param>
        /// <param name="lockFileRetryDelay">Delay between each attempt to get the lock. Defaults to 100ms</param>
        /// <param name="lockFileRetryCount">How many times to try to get the lock before bailing. Defaults to 600 times.</param>
        /// <remarks>This class is experimental and may be removed from the public API.</remarks>
        public CrossPlatLock(string lockfilePath, int lockFileRetryDelay = LockfileRetryDelayDefault, int lockFileRetryCount = LockfileRetryCountDefault)
        {
            Exception exception = null;
            FileStream fileStream = null;

            // Create lock file dir if it doesn't already exist
            
            Directory.CreateDirectory(Path.GetDirectoryName(lockfilePath));
            string lockerProcessInfo = $"{SharedUtilities.GetCurrentProcessId()} {SharedUtilities.GetCurrentProcessName()}";

            for (int tryCount = 0; tryCount < lockFileRetryCount; tryCount++)
            {
                try
                {
                    // We are using the file locking to synchronize the store, do not allow multiple writers or readers for the file.
                    const int defaultBufferSize = 4096;
                    var fileShare = FileShare.None;
                    if (SharedUtilities.IsWindowsPlatform())
                    {
                        // This is so that Windows can offer read due to the granularity of the locking. Unix will not
                        // lock with FileShare.Read. Read access on Windows is only for debugging purposes and will not
                        // affect the functionality.
                        //
                        // See: https://github.com/dotnet/coreclr/blob/98472784f82cee7326a58e0c4acf77714cdafe03/src/System.Private.CoreLib/shared/System/IO/FileStream.Unix.cs#L74-L89
                        fileShare = FileShare.Read;
                    }

                    var fileOptions = FileOptions.DeleteOnClose;
                    if (SharedUtilities.IsMonoPlatform())
                    {
                        // Deleting on close/dispose would cause a file locked by another process to be deleted when
                        // running on Mono since locking is a two step process - it requires creating a FileStream and then
                        // calling FileStream.Lock, which then may fail.
                        fileOptions = FileOptions.None;
                    }

                    fileStream = new FileStream(lockfilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, fileShare, defaultBufferSize, fileOptions);

                    if (SharedUtilities.IsMonoPlatform())
                    {
                        // Mono requires FileStream.Lock to be called to lock the file. Using FileShare.None when creating the
                        // FileStream is not enough to lock the file on Mono.
                        fileStream.Lock(0, 0);
                    }

                    using (var writer = new StreamWriter(fileStream, Encoding.UTF8, defaultBufferSize, leaveOpen: true))
                    {
                        writer.WriteLine(lockerProcessInfo);
                    }
                        break;
                }
                catch (IOException ex)
                {
                    fileStream?.Dispose();
                    fileStream = null;
                    exception = ex;
                    Thread.Sleep(lockFileRetryDelay);
                }
                catch (UnauthorizedAccessException ex)
                {
                    fileStream?.Dispose();
                    fileStream = null;
                    exception = ex;
                    Thread.Sleep(lockFileRetryDelay);
                }
            }

            _lockFileStream = fileStream ?? throw new InvalidOperationException("Could not get access to the shared lock file.", exception);
        }

        /// <summary>
        /// Releases the lock
        /// </summary>
        public void Dispose()
        {
            _lockFileStream?.Dispose();
            _lockFileStream = null;
        }
    }
}
