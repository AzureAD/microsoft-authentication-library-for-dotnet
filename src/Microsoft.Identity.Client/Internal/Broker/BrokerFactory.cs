// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if iOS
using Microsoft.Identity.Client.Platforms.iOS;
#endif
using Microsoft.Identity.Client.Core;
using System;

namespace Microsoft.Identity.Client.Internal.Broker
{
    internal class BrokerFactory
    {
        // thread safety ensured by implicit LazyThreadSafetyMode.ExecutionAndPublication
        public IBroker CreateBrokerFacade(IServiceBundle serviceBundle)
        {
#if iOS
            return new iOSBroker(serviceBundle);
#elif ANDROID
            return new NullBroker();
#else
            return new NullBroker();
#endif
        }
    }
}
