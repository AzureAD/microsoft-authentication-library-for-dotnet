// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Platforms.Shared.Desktop.OsBrowser
{
    internal static class HttpResponseParser
    {
        private const string UrlQueryRegexp = @"GET \/\?(.*) HTTP";
        private const string UrlHostRegexp = @"Host: (.*)";

#pragma warning disable CS1570 // XML comment has badly formed XML

        /// <summary>
        /// Example TCP response:
        /// 
        /// {GET /?code=OAQABAAIAAAC5una0EUFgTIF8ElaxtWjTl5wse5YHycjcaO_qJukUUexKz660btJtJSiQKz1h4b5DalmXspKis-bS6Inu8lNs4CpoE4FITrLv00Mr3MEYEQzgrn6JiNoIwDFSl4HBzHG8Kjd4Ho65QGUMVNyTjhWyQDf_12E8Gw9sll_sbOU51FIreZlVuvsqIWBMIJ8mfmExZBSckofV6LbcKJTeEZKaqjC09x3k1dpsCNJAtYTQIus5g1DyhAW8viDpWDpQJlT55_0W4rrNKY3CSD5AhKd3Ng4_ePPd7iC6qObfmMBlCcldX688vR2IghV0GoA0qNalzwqP7lov-yf38uVZ3ir6VlDNpbzCoV-drw0zhlMKgSq6LXT7QQYmuA4RVy_7TE9gjQpW-P0_ZXUHirpgdsblaa3JUq4cXpbMU8YCLQm7I2L0oCkBTupYXKLoM2gHSYPJ5HChhj1x0pWXRzXdqbx_TPTujBLsAo4Skr_XiLQ4QPJZpkscmXezpPa5Z87gDenUBRBI9ppROhOksekMbvPataF0qBaM38QzcnzeOCFyih1OjIKsq3GeryChrEtfY9CL9lBZ6alIIQB4thD__Tc24OUmr04hX34PjMyt1Z9Qvr76Pw0r7A52JvqQLWupx8bqok6AyCwqUGfLCPjwylSLA7NYD7vScAbfkOOszfoCC3ff14Dqm3IAB1tUJfCZoab61c6Mozls74c2Ujr3roHw4NdPuo-re5fbpSw5RVu8MffWYwXrO3GdmgcvIMkli2uperucLldNVIp6Pc3MatMYSBeAikuhtaZiZAhhl3uQxzoMhU-MO9WXuG2oIkqSvKjghxi1NUhfTK4-du7I5h1r0lFh9b3h8kvE1WBhAIxLdSAA&state=b380f309-7d24-4793-b938-e4a512b2c7f6&session_state=a442c3cd-a25e-4b88-8b33-36d194ba11b2 HTTP/1.1
        /// Host: localhost:9001
        /// Accept-Language: en-GB,en;q=0.9,en-US;q=0.8,ro;q=0.7,fr;q=0.6
        /// Connection: keep-alive
        /// Upgrade-Insecure-Requests: 1
        /// User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.102 Safari/537.36
        /// Accept: text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8
        /// Accept-Encoding: gzip, deflate, br
        /// </summary>
        /// <returns>http://localhost:9001/?code=foo&session_state=bar</returns>
#pragma warning restore CS1570 // XML comment has badly formed XML
        public static Uri ExtractUriFromHttpRequest(string httpRequest, ICoreLogger logger)
        {
            string getQuery = ExtractQuery(httpRequest, logger);
            string host = ExtractHost(httpRequest, logger);
            var hostParts = host.Split(':');

            var uriBuilder = new UriBuilder();
            if (hostParts.Count() == 2)
            {
                uriBuilder.Host = hostParts[0];
                uriBuilder.Port = int.Parse(hostParts[1], CultureInfo.InvariantCulture);
            }
            else
            {
                uriBuilder.Host = host;
            }

            uriBuilder.Query = getQuery;


            return uriBuilder.Uri;
        }

        private static string ExtractQuery(string httpRequest, ICoreLogger logger)
        {
            var regex = new Regex(UrlQueryRegexp);
            Match match = regex.Match(httpRequest);

            if (!match.Success || match.Groups.Count != 2)
            {
                logger.ErrorPii(
                    "Could not extract the query from the authorization response: " + httpRequest,
                    "Could not extract the query from the authorization response. Enable Pii on to see the request");

                throw new MsalClientException(
                    MsalError.InvalidAuthorizationUri,
                    "Could not extract the query from the authorization response - check Pii enabled logs for details");
            }

            return match.Groups[1].Value;
        }

        private static string ExtractHost(string httpRequest, ICoreLogger logger)
        {
            var regex = new Regex(UrlHostRegexp);
            Match match = regex.Match(httpRequest);

            if (!match.Success || match.Groups.Count != 2)
            {
                logger.ErrorPii(
                    "Could not extract the host from the authorization response: " + httpRequest,
                    "Could not extract the host from the authorization response. Enable Pii on to see the request");

                throw new MsalClientException(
                    MsalError.InvalidAuthorizationUri,
                    "Could not extract the host from the authorization response - check Pii enabled logs for details");
            }

            return match.Groups[1].Value.Trim();
        }
    }
}
