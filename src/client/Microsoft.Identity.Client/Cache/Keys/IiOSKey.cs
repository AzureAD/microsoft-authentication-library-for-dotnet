// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Cache.Keys
{
    internal interface IiOSKey
    {
        string iOSAccount { get; }

        string iOSGeneric { get; }

        string iOSService { get; }

        int iOSType { get; }
    }
}
