using System;
using System.Collections.Generic;
using System.Text;
using Foundation;

namespace Microsoft.Identity.Client
{
    internal class CustomHeaderHandler
    {
        public static IDictionary<string, string> AdditionalHeaders;

        public static void ApplyHeadersTo(NSMutableUrlRequest mutableRequest)
        {
            if (!mutableRequest.Headers.ContainsKey(new NSString(BrokerConstants.ChallengeHeaderKey)))
            {
                mutableRequest[BrokerConstants.ChallengeHeaderKey] = BrokerConstants.ChallengeHeaderValue;
            }

            if (AdditionalHeaders!=null)
            {
                foreach (var key in AdditionalHeaders.Keys)
                {
                    mutableRequest.Headers[new NSString(key)] = new NSString(AdditionalHeaders[key]);
                }

                AdditionalHeaders = null;
            }
        }

    }
}
