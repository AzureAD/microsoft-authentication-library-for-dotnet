// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.Identity.Client.CacheV2.Impl;
using Microsoft.Identity.Client.CacheV2.Impl.Utils;

namespace Microsoft.Identity.Client.Platforms.net45.CacheV2
{
    internal class WindowsFileSystemCacheKeyStorage : ICachePathStorage
    {
        public WindowsFileSystemCacheKeyStorage(string basePath)
        {
            if (string.IsNullOrWhiteSpace(basePath) || !Path.IsPathRooted(basePath))
            {
                throw new ArgumentException(nameof(basePath));
            }

            BasePath = basePath;
        }

        public string BasePath { get; }

        public byte[] Read(string key)
        {
            using (LockFile(key))
            {
                return Exists(GetFullPath(key)) ? ReadLockedExisting(key) : new byte[0];
            }
        }

        public void ReadModifyWrite(string key, Func<byte[], byte[]> modify)
        {
            using (var fileMutex = LockFile(key))
            {
                var data = new byte[0];

                if (Exists(GetFullPath(key)))
                {
                    data = ReadLockedExisting(key);
                }

                data = modify(data);

                WriteLocked(key, data, fileMutex);
            }
        }

        public void Write(string key, byte[] data)
        {
            using (var fileMutex = LockFile(key))
            {
                WriteLocked(key, data, fileMutex);
            }
        }

        public void DeleteFile(string key)
        {
            // lock the parent
            using (LockFile(GetParentPath(key)))
            {
                using (LockFile(key))
                {
                    Remove(key);
                }
            }
        }

        public void DeleteContent(string key)
        {
            using (LockFile(GetParentPath(key)))
            {
                DeleteContentHelper(key);
            }
        }

        public IEnumerable<string> ListContent(string key)
        {
            using (LockFile(key))
            {
                string fullPath = GetFullPath(key);
                var result = new List<string>();

                if (Directory.Exists(fullPath))
                {
                    foreach (string item in Directory.EnumerateFileSystemEntries(fullPath))
                    {
                        string newItem = item.Substring(BasePath.Length);
                        newItem = PathUtils.Normalize(newItem);
                        while (newItem.StartsWith("/", StringComparison.OrdinalIgnoreCase))
                        {
                            newItem = newItem.Substring(1);
                        }

                        result.Add(newItem);
                    }
                }

                return result;
            }
        }

        public FileMutex LockFile(string relativePath)
        {
            return new FileMutex(relativePath);
        }

        private byte[] ReadLockedExisting(string relativePath)
        {
            return File.ReadAllBytes(GetFullPath(relativePath));
        }

        private void WriteLocked(string relativePath, byte[] data, FileMutex fileMutex)
        {
            if (Exists(GetFullPath(relativePath)))
            {
                File.WriteAllBytes(GetFullPath(relativePath), data);
            }
            else
            {
                fileMutex.Unlock();

                using (CreateDirectoriesLockParent(GetParentPath(relativePath)))
                {
                    if (!fileMutex.TryLock())
                    {
                        throw new InvalidOperationException("msal cannot lock file for write"); // todo: exception
                    }

                    File.WriteAllBytes(GetFullPath(relativePath), data);
                }
            }
        }

        public FileMutex CreateDirectoriesLockParent(string relativePath)
        {
            FileMutex parentFileMutex = null;
            if (!string.IsNullOrWhiteSpace(relativePath))
            {
                // Debug.WriteLine($"Getting parentFileMutex: ({Thread.CurrentThread.ManagedThreadId}) {key}");
                parentFileMutex = CreateDirectoriesLockParent(GetParentPath(relativePath));
            }

            try
            {
                var fileMutex = LockFile(relativePath);
                try
                {
                    string fullPath = GetFullPath(relativePath);
                    if (!Exists(fullPath))
                    {
                        Directory.CreateDirectory(fullPath);
                    }

                    return fileMutex;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Exception caught: ({Thread.CurrentThread.ManagedThreadId}) {relativePath} --> {ex}");
                    fileMutex.Dispose();
                    throw;
                }
            }
            finally
            {
                // Debug.WriteLine($"Releasing parentFileMutex: ({Thread.CurrentThread.ManagedThreadId}) {key}");
                if (parentFileMutex != null)
                {
                    parentFileMutex.Dispose();
                    parentFileMutex = null;
                }
            }
        }

        private bool Exists(string fullPath)
        {
            return File.Exists(fullPath);
        }

        public string GetFullPath(string relativePath)
        {
            if (Path.IsPathRooted(relativePath))
            {
                throw new ArgumentException("file cannot be absolute", nameof(relativePath));
            }

            return PathUtils.Normalize(Path.Combine(BasePath, relativePath));
        }

        private void Remove(string relativePath)
        {
            string fullPath = GetFullPath(relativePath);
            if (Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath);
            }
            else if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }

        public string GetParentPath(string startingPath)
        {
            if (string.IsNullOrWhiteSpace(startingPath))
            {
                return startingPath;
            }

            string fullPath = Path.IsPathRooted(startingPath) ? startingPath : GetFullPath(startingPath);
            if (IsDirectory(fullPath))
            {
                startingPath = startingPath.Trim();
                if (startingPath.EndsWith("/", StringComparison.Ordinal) || startingPath.EndsWith("\\", StringComparison.Ordinal))
                {
                    startingPath = startingPath.Substring(0, fullPath.Length - 1);
                }

                int lastIndex = startingPath.LastIndexOfAny(
                    new char[]
                    {
                        '/',
                        '\\'
                    });
                if (lastIndex >= 0)
                {
                    startingPath = startingPath.Substring(0, lastIndex);
                }
                else
                {
                    startingPath = string.Empty;
                }

                return startingPath;
            }
            else
            {
                return Path.GetDirectoryName(startingPath);
            }
        }

        private bool IsDirectory(string fullPath)
        {
            return Directory.Exists(fullPath);
        }

        private void DeleteContentHelper(string relativePath)
        {
            using (LockFile(relativePath))
            {
                string fullPath = GetFullPath(relativePath);
                if (IsDirectory(fullPath))
                {
                    Directory.Delete(fullPath, true);
                }

                Remove(relativePath);
            }
        }
    }
}
