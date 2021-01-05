using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Json;

namespace Microsoft.Identity.Client.Platforms.Android.Broker.Requests
{
    [JsonObject]
    [Preserve(AllMembers = true)]
    internal class BrokerHandshakeRequest
    {
        private const string DefaultBrokerMinProtocalVersion = "2.0";
        private const string DefaultBrokerMaxProtocalVersion = "3.0";
        private const string DefaultBrokerAccountManagerOperationKey = "HELLO";

        [JsonProperty(BrokerConstants.ClientAdvertisedMaximumBPVersionKey)]
        public string MaxBpVersion { get { return DefaultBrokerMaxProtocalVersion; } }

        [JsonProperty(BrokerConstants.ClientConfiguredMinimumBPVersionKey)]
        public string MinBpVersion { get { return DefaultBrokerMinProtocalVersion; } }

        //[JsonProperty(BrokerConstants.BrokerAccountManagerOperationKey)]
        //public string PbOperationKey { get { return DefaultBrokerAccountManagerOperationKey; } }

        public static string AsJsonString()
        {
            return JsonHelper.SerializeToJson(new BrokerHandshakeRequest());
        }
    }
}
