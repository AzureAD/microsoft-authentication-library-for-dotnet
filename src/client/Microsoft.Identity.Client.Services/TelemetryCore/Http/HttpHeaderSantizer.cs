// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text;

namespace Microsoft.Identity.Client.TelemetryCore.Http
{
    internal class HttpHeaderSanitizer
    {
          private static readonly string[] s_headerEncodingTable = new string[] {
            "%00", "%01", "%02", "%03", "%04", "%05", "%06", "%07",
            "%08", "%09", "%0a", "%0b", "%0c", "%0d", "%0e", "%0f",
            "%10", "%11", "%12", "%13", "%14", "%15", "%16", "%17",
            "%18", "%19", "%1a", "%1b", "%1c", "%1d", "%1e", "%1f"
        };

        // Based on https://referencesource.microsoft.com/#System.Web/Util/HttpEncoder.cs,e5d896b254faf84e
        public static string SanitizeHeader(string value)
        {
            string sanitizedHeader = value;
            if (HeaderValueNeedsEncoding(value))
            {
                StringBuilder sb = new StringBuilder();
                foreach (char c in value)
                {
                    if (c < 32 && c != 9)
                    {
                        sb.Append(s_headerEncodingTable[c]);
                    }
                    else if (c == 127)
                    {
                        sb.Append("%7f");
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                sanitizedHeader = sb.ToString();
            }

            return sanitizedHeader;
        }

        private static bool HeaderValueNeedsEncoding(string value)
        {
            foreach (char c in value)
            {
                if ((c < 32 && c != 9) || (c == 127))
                {
                    return true;
                }
            }
            return false;
        }

    }
}
