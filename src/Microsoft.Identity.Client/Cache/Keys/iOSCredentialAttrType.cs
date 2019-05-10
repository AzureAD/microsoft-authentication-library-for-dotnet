// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Cache.Keys
{
    internal partial class MsalCacheKeys
    {
        #region iOS

        internal enum iOSCredentialAttrType
        {
            AccessToken = 2001,
            RefreshToken = 2002,
            IdToken = 2003,
            Password = 2004,
            AppMetadata = 3001
        }

        #endregion
    }
}
