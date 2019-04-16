// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.Identity.Client.CacheV2.Impl
{
    /// <summary>
    /// This interface represents raw byte i/o for cache data stored using relative paths (e.g. in memory, file system).
    /// </summary>
    internal interface ICachePathStorage
    {
        byte[] Read(string relativePath);
        void ReadModifyWrite(string relativePath, Func<byte[], byte[]> modify);
        void Write(string relativePath, byte[] data);
        void DeleteFile(string relativePath);
        void DeleteContent(string relativePath);
        IEnumerable<string> ListContent(string relativePath);
    }
}
