// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Identity.Client.Extensions.Msal
{
    internal class Constants
    {
        public const string MacKeyChainDeleteFailed = "SecKeychainItemDelete failed with error code: {0}";
        public const string MacKeyChainFindFailed = "SecKeychainFindGenericPassword failed with error code: {0}";
        public const string MacKeyChainInsertFailed = "SecKeychainAddGenericPassword failed with error code: {0}";
        public const string MacKeyChainUpdateFailed = "SecKeychainItemModifyAttributesAndData failed with error code: {0}";
    }
}
