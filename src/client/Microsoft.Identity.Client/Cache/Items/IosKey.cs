// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Cache.Keys
{
    internal struct IosKey : IiOSKey
    {
        public string iOSAccount { get; }

        public string iOSGeneric { get; }

        public string iOSService { get; }

        public int iOSType { get; }

        internal IosKey(string iOSAccount, string iOSService, string iOSGeneric, int iOSType)
        {
            this.iOSAccount = iOSAccount;
            this.iOSGeneric = iOSGeneric;
            this.iOSService = iOSService;
            this.iOSType = iOSType;
        }
    }
}
