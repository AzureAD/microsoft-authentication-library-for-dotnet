// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Broker;

namespace Microsoft.Identity.Test.Unit
{
    internal class IosBrokerMock : NullBroker
    {
        public IosBrokerMock(ILoggerAdapter logger) : base(logger)
        {

        }
        public override bool IsBrokerInstalledAndInvokable(AuthorityType authorityType)
        {
            if (authorityType == AuthorityType.Adfs)
            {
                return false;
            }

            return true;
        }
    }

}
