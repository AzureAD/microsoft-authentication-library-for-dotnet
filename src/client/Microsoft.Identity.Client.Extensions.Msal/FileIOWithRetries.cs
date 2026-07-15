// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading;
using Microsoft.Identity.Client.Extensions.Msal.Accessors;

namespace Microsoft.Identity.Client.Extensions.Msal
{
    internal static class FileIOWithRetries
    {
        private const int FileLockRetryCount = 20;
        private const int FileLockRetryWaitInMs = 200;

        internal static void DeleteCacheFile(string filePath, TraceSourceLogger logger)
        {
            bool cacheFileExists = File.Exists(filePath);
            logger.LogInformation($"DeleteCacheFile Cache file exists '{cacheFileExists}'");

            TryProcessFile(() =>
            {
                logger.LogInformation("Before deleting the cache file");
                try
                {
                    File.Delete(filePath);
                }
                catch (Exception e)
                {
                    logger.LogError($"Problem deleting the cache file '{e}'");
                }

                logger.LogInformation($"After deleting the cache file.");
            }, logger);
        }

        internal static void CreateAndWriteToFile(string filePath, byte[] data, bool setChmod600, TraceSourceLogger logger)
        {
            EnsureParentDirectoryExists(filePath, logger);

            if (!SharedUtilities.IsWindowsPlatform())
            {
                ThrowIfCacheFileIsSymlink(filePath);
            }

            logger.LogInformation($"Writing cache file");

            TryProcessFile(() =>
            {
                if (setChmod600)
                {
                    logger.LogInformation($"Writing file with chmod 600");
                    FileWithPermissions.WriteToNewFileWithOwnerRWPermissions(filePath, data);
                }
                else
                {
                    logger.LogInformation($"Writing file without special permissions");
                    File.WriteAllBytes(filePath, data);
                }
            }, logger);
        }

        private static void EnsureParentDirectoryExists(string filePath, TraceSourceLogger logger)
        {
            string directoryForCacheFile = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directoryForCacheFile))
            {
                string directory = Path.GetDirectoryName(filePath);
                logger.LogInformation($"Creating directory '{directory}'");
                Directory.CreateDirectory(directory);
            }
        }

        /// <summary>
        /// Changes the LastWriteTime of the file, without actually writing anything to it.
        /// </summary>
        /// <remarks>
        /// Creates the file if it does not exist.
        /// This operation will enable a <see cref="FileSystemWatcher"/> to fire.
        /// </remarks>
        internal static void TouchFile(string filePath, TraceSourceLogger logger)
        {
            EnsureParentDirectoryExists(filePath, logger);

            if (!SharedUtilities.IsWindowsPlatform())
            {
                ThrowIfCacheFileIsSymlink(filePath);
            }

            logger.LogInformation($"Touching file...");

            TryProcessFile(() =>
            {
                if (!File.Exists(filePath))
                {
                    logger.LogInformation($"File {filePath} does not exist. Creating it..");

                    var fs = File.Create(filePath);
                    fs.Dispose();
                }

                File.SetLastWriteTimeUtc(filePath, DateTime.UtcNow);

            }, logger);
        }

        /// <summary>
        /// Throws <see cref="InvalidOperationException"/> if <paramref name="filePath"/> is a
        /// symbolic link. Called on non-Windows platforms before writing or touching the cache
        /// file to prevent a symlink from being used as a write-anywhere primitive.
        /// </summary>
        /// <remarks>
        /// Uses <see cref="File.GetAttributes(string)"/> which maps to <c>lstat(2)</c> on Unix and
        /// therefore returns <see cref="FileAttributes.ReparsePoint"/> for the symlink itself
        /// rather than following it to the target. If the path does not yet exist the method
        /// returns silently — the caller will create the file normally.
        /// </remarks>
        private static void ThrowIfCacheFileIsSymlink(string filePath)
        {
            try
            {
                if ((File.GetAttributes(filePath) & FileAttributes.ReparsePoint) != 0)
                {
                    throw new InvalidOperationException(
                        $"The cache file path '{filePath}' is a symbolic link. MSAL cache paths must not be symbolic links.");
                }
            }
            catch (FileNotFoundException)
            {
                // File does not exist yet — not a symlink, proceed normally.
            }
        }

        internal static void TryProcessFile(Action action, TraceSourceLogger logger)
        {
            for (int tryCount = 0; tryCount <= FileLockRetryCount; tryCount++)
            {
                try
                {
                    action.Invoke();
                    return;
                }
                catch (Exception e)
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(FileLockRetryWaitInMs));

                    

                    if (tryCount == FileLockRetryCount)
                    {
                        logger.LogError($"An exception was encountered while processing the cache file ex:'{e}'");
                    }
                    else
                    {
                        logger.LogWarning($"An exception was encountered while processing the cache file. Operation will be retried. Ex:'{e}'");
                    }
                }
            }
        }
    }
}
