// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.Serialization;

namespace Microsoft.Identity.Client.TelemetryCore.Internal
{
    [DataContract]
    internal class HttpTelemetryContent
    {
        [DataMember]
        public string LastErrorCode { get; set; }

        [DataMember]
        public int UnreportedErrorCount { get; set; }

        [DataMember]
        public string ApiId { get; set; }

        [DataMember]
        public string CorrelationId { get; set; }

        public void ResetLastErrorCode()
        {
            LastErrorCode = string.Empty;
        }
    }
}
