using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Identity.Client.Internal.Broker
{
    internal class BrokerAcquireTokenInteractiveParameters
    {
        public Prompt Prompt { get; set; }
        public string LoginHint { get; set; }
    }
}
