// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Identity.Client.CacheV2.Impl.Utils;

namespace Microsoft.Identity.Client.CacheV2.Impl.InMemory
{
    /// <summary>
    /// This is the in-memory cache implementation.  This should be used when a developer wants
    /// to persist the cache data on their own (e.g. in a distributed cloud environment).
    /// </summary>
    internal class InMemoryCachePathStorage : ICachePathStorage
    {
        private readonly object _lock = new object();
        internal FsDirectory RootDirectory { get; } = new FsDirectory();

        public byte[] Read(string key)
        {
            lock (_lock)
            {
                return RootDirectory.FileExists(key) ? ReadLockedExisting(key) : new byte[0];
            }
        }

        public void ReadModifyWrite(string key, Func<byte[], byte[]> modify)
        {
            lock (_lock)
            {
                var data = new byte[0];

                if (RootDirectory.FileExists(key))
                {
                    data = ReadLockedExisting(key);
                }

                data = modify(data);

                WriteLocked(key, data);
            }
        }

        public void Write(string key, byte[] data)
        {
            lock (_lock)
            {
                WriteLocked(key, data);
            }
        }

        public void DeleteFile(string key)
        {
            lock (_lock)
            {
                RootDirectory.RemoveEntry(key);
            }
        }

        public void DeleteContent(string key)
        {
            lock (_lock)
            {
                RootDirectory.RemoveEntry(key);
            }
        }

        public IEnumerable<string> ListContent(string key)
        {
            lock (_lock)
            {
                var dir = RootDirectory;
                string keyToPrepend = string.Empty;

                if (!string.IsNullOrEmpty(key))
                {
                    dir = RootDirectory.GetDirectoryEntry(key);
                    if (dir == null)
                    {
                        return new List<string>();
                    }

                    keyToPrepend = key;
                    if (!keyToPrepend.EndsWith("/", StringComparison.Ordinal))
                    {
                        keyToPrepend += "/";
                    }
                }

                return dir.ListContents(false).Select(s => keyToPrepend + s).ToList();
            }
        }

        private byte[] ReadLockedExisting(string relativePath)
        {
            var fsFile = RootDirectory.GetFileEntry(relativePath);
            return fsFile == null ? new byte[0] : fsFile.Contents;
        }

        private void WriteLocked(string relativePath, byte[] data)
        {
            RootDirectory.CreateFile(relativePath, data);
        }

        internal abstract class FsEntry
        {
        }

        internal class FsFile : FsEntry
        {
            private byte[] _contents = new byte[0];

            public byte[] Contents
            {
                get => _contents;
                set
                {
                    _contents = new byte[value.Length];
                    value.CopyTo(_contents, 0);
                }
            }
        }

        internal class FsDirectory : FsEntry
        {
            private Dictionary<string, FsEntry> Entries { get; } =
                new Dictionary<string, FsEntry>(StringComparer.OrdinalIgnoreCase);

            private FsDirectory CreateDirectory(string relativePath)
            {
                string[] pathParts = SplitPath(relativePath);

                var currentDirectory = this;
                foreach (string dirName in pathParts)
                {
                    if (currentDirectory.Entries.TryGetValue(dirName, out var child))
                    {
                        if (child is FsFile file)
                        {
                            throw new InvalidOperationException("file exists when trying to create directory: " + relativePath);
                        }
                    }
                    else
                    {
                        child = new FsDirectory();
                        currentDirectory.Entries[dirName] = child;
                    }

                    currentDirectory = (FsDirectory)child;
                }

                return currentDirectory;
            }

            public void CreateFile(string relativePath, byte[] contents)
            {
                var fsDir = CreateDirectory(PathUtils.Normalize(Path.GetDirectoryName(relativePath)));
                string fileName = Path.GetFileName(relativePath);
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    throw new ArgumentException(nameof(relativePath));
                }

                var fsFile = new FsFile
                {
                    Contents = contents
                };

                fsDir.Entries[fileName] = fsFile;
            }

            private static string[] SplitPath(string relativePath)
            {
                return PathUtils.Normalize(relativePath).Split(
                    new[]
                    {
                        '/'
                    },
                    StringSplitOptions.RemoveEmptyEntries);
            }

            private FsEntry GetEntry(string relativePath)
            {
                string[] pathParts = SplitPath(relativePath);

                var currentDirectory = this;
                for (int i = 0; i < pathParts.Length; i++)
                {
                    string pathPart = pathParts[i];
                    if (currentDirectory.Entries.TryGetValue(pathPart, out var child))
                    {
                        if (child is FsFile file)
                        {
                            if (i == pathParts.Length - 1)
                            {
                                return file;
                            }
                            else
                            {
                                return null;
                            }
                        }
                        else
                        {
                            currentDirectory = (FsDirectory)child;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }

                return currentDirectory;
            }

            public void RemoveEntry(string relativePath)
            {
                string[] pathParts = SplitPath(relativePath);

                string curDir = pathParts[0];

                if (!Entries.TryGetValue(curDir, out var fsEntry))
                {
                    return;
                }

                if (pathParts.Length == 1)
                {
                    Entries.Remove(curDir);
                    return;
                }

                if (!(fsEntry is FsDirectory fsDir))
                {
                    throw new InvalidOperationException(
                        "Trying to delete directory but file exists in the middle of the path:" + relativePath);
                }

                // +1 to add the directory separator
                fsDir.RemoveEntry(relativePath.Substring(curDir.Length + 1));
            }

            public FsFile GetFileEntry(string relativePath)
            {
                return GetEntry(relativePath) as FsFile;
            }

            public FsDirectory GetDirectoryEntry(string relativePath)
            {
                return GetEntry(relativePath) as FsDirectory;
            }

            public bool FileExists(string relativePath)
            {
                return GetFileEntry(relativePath) != null;
            }

            public bool DirectoryExists(string relativePath)
            {
                return GetDirectoryEntry(relativePath) != null;
            }

            /// <summary>
            ///     Returns list of relative paths at this directory, and downwards if recurse is true.
            /// </summary>
            /// <param name="recurse"></param>
            /// <returns></returns>
            public IEnumerable<string> ListContents(bool recurse)
            {
                var contents = new List<string>();
                ListContentsRecursive(this, recurse, string.Empty, contents);
                return contents;
            }

            private static void ListContentsRecursive(
                FsDirectory directory,
                bool recurse,
                string parentPath,
                List<string> contents)
            {
                foreach (KeyValuePair<string, FsEntry> kvp in directory.Entries)
                {
                    switch (kvp.Value)
                    {
                    case FsFile file:
                        contents.Add($"{parentPath}{kvp.Key}");
                        break;
                    case FsDirectory dir:
                        string curPath = parentPath;
                        if (!string.IsNullOrEmpty(parentPath))
                        {
                            curPath += "/";
                        }

                        curPath += kvp.Key;
                        if (recurse)
                        {
                            ListContentsRecursive(dir, recurse, curPath, contents);
                        }
                        else
                        {
                            contents.Add(curPath);
                        }

                        break;
                    }
                }
            }
        }
    }
}
