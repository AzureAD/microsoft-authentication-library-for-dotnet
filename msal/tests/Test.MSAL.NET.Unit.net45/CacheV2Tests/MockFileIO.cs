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
using System.Collections.Generic;
using Microsoft.Identity.Client.CacheV2.Impl;

namespace Test.MSAL.NET.Unit.net45.CacheV2Tests
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