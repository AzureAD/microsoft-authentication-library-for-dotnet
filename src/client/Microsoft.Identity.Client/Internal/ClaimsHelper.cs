using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Json;
using Microsoft.Identity.Json.Linq;

namespace Microsoft.Identity.Client.Internal
{
    internal class ClaimsHelper
    {
        private const string AccessTokenClaim = "access_token";
        private const string XmsClientCapability = "xms_cc";

        internal static string GetMergedClaimsAndClientCapabilities(
            string claims,
            IEnumerable<string> clientCapabilities)
        {
            if (clientCapabilities != null && clientCapabilities.Any())
            {
                JObject capabilitiesJson = CreateClientCapabilitiesRequestJson(clientCapabilities);
                JObject mergedClaimsAndCapabilities = MergeClaimsIntoCapabilityJson(claims, capabilitiesJson);

                return mergedClaimsAndCapabilities.ToString(Formatting.None);
            }

            return claims;
        }

        private static JObject MergeClaimsIntoCapabilityJson(string claims, JObject capabilitiesJson)
        {
            if (!string.IsNullOrEmpty(claims))
            {
                JObject claimsJson;
                try
                {
                    claimsJson = JObject.Parse(claims);
                }
                catch (JsonReaderException ex)
                {
                    throw new MsalClientException(
                        MsalError.InvalidJsonClaimsFormat,
                        MsalErrorMessage.InvalidJsonClaimsFormat(claims),
                        ex);
                }

                capabilitiesJson.Merge(claimsJson, new JsonMergeSettings
                {
                    // union array values together to avoid duplicates
                    MergeArrayHandling = MergeArrayHandling.Union
                });
            }

            return capabilitiesJson;
        }

        private static JObject CreateClientCapabilitiesRequestJson(IEnumerable<string> clientCapabilities)
        {
            // "access_token": {
            //     "xms_cc": { 
            //         values: ["cp1", "cp2"]
            //     }
            //  }
            return new JObject
            {
                new JProperty(AccessTokenClaim, new JObject(
                    new JObject(new JProperty(XmsClientCapability,
                            new JObject(new JProperty("values",
                                new JArray(clientCapabilities)))))))            };
        }
    }
}
