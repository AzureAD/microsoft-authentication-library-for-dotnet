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
