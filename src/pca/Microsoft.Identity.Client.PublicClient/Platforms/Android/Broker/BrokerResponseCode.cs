// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#if ANDROID

namespace Microsoft.Identity.Client.Platforms.Android.Broker
{
    internal enum BrokerResponseCode
    {
        UserCancelled = 2001,
        BrowserCodeError = 2002,
        ResponseReceived = 2004
    }
}
#endif
