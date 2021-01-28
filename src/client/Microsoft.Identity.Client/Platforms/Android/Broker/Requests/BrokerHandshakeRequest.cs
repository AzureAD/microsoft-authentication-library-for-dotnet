using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.OS;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Json;

namespace Microsoft.Identity.Client.Platforms.Android.Broker.Requests
{
    [JsonObject]
    [Preserve(AllMembers = true)]
    internal class BrokerHandshakeHelper
    {
        //private const string DefaultBrokerMinProtocalVersion = "2.0";
        //private const string DefaultBrokerMaxProtocalVersion = "3.0";
        //private const string DefaultBrokerAccountManagerOperationKey = "HELLO";

        //[JsonProperty(BrokerConstants.ClientAdvertisedMaximumBPVersionKey)]
        //public string MaxBpVersion { get { return DefaultBrokerMaxProtocalVersion; } }

        //[JsonProperty(BrokerConstants.ClientConfiguredMinimumBPVersionKey)]
        //public string MinBpVersion { get { return DefaultBrokerMinProtocalVersion; } }

        ////[JsonProperty(BrokerConstants.BrokerAccountManagerOperationKey)]
        ////public string PbOperationKey { get { return DefaultBrokerAccountManagerOperationKey; } }

        //public static string AsJsonString()
        //{
        //    Bundle helloRequestBundle = new Bundle();
        //    helloRequestBundle.PutString(BrokerConstants.ClientAdvertisedMaximumBPVersionKey, BrokerConstants.BrokerProtocalVersionCode);
        //    helloRequestBundle.PutString(BrokerConstants.ClientConfiguredMinimumBPVersionKey, "2.0");
        //    helloRequestBundle.PutString(BrokerConstants.BrokerAccountManagerOperationKey, "HELLO");

        //    return Base64UrlHelpers.Encode(marshall(helloRequestBundle));
        //}

        //public static byte[] marshall(Bundle parcelable)
        //{
        //    Parcel parcel = Parcel.Obtain();
        //    parcel.WriteBundle(parcelable);

        //    return parcel.Marshall();
        //}
    }
}
