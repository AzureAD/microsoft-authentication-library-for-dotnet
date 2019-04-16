// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.CacheV2.Impl;

namespace Microsoft.Identity.Test.Unit.CacheV2Tests
{
    internal class MockFileIO : ICachePathStorage
    {
        public readonly Dictionary<string, byte[]> _fileSystem = new Dictionary<string, byte[]>();

        public byte[] Read(string key)
        {
            if (_fileSystem.TryGetValue(key, out byte[] contents))
            {
                return contents;
            }
            else
            {
                return new byte[0];
            }
        }

        public void ReadModifyWrite(string key, Func<byte[], byte[]> modify)
        {
            _fileSystem.TryGetValue(key, out byte[] contents);
            if (contents == null)
            {
                contents = new byte[0];
            }

            _fileSystem[key] = modify(contents);
        }

        public void Write(string key, byte[] data)
        {
            _fileSystem[key] = data;
        }

        public void DeleteFile(string key)
        {
            _fileSystem.Remove(key);
        }

        public void DeleteContent(string key)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> ListContent(string key)
        {
            throw new NotImplementedException();
        }
    }
}
