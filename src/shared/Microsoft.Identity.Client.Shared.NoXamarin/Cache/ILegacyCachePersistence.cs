// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Cache
{
    internal interface ILegacyCachePersistence
    {
        byte[] LoadCache();

        void WriteCache(byte[] serializedCache);
    }
}
