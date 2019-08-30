// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.Serialization;

namespace Microsoft.Identity.Client.OAuth2
{
    internal class OAuth2ResponseBaseClaim
    {
        public const string Claims = "claims";
        public const string Error = "error";
        public const string SubError = "suberror";
        public const string ErrorDescription = "error_description";
        public const string ErrorCodes = "error_codes";
        public const string CorrelationId = "correlation_id";
    }

    [DataContract]
    internal class OAuth2ResponseBase
    {
        [DataMember(Name = OAuth2ResponseBaseClaim.Error, IsRequired = false)]
        public string Error { get; set; }

        [DataMember(Name = OAuth2ResponseBaseClaim.SubError, IsRequired = false)]
        public string SubError { get; set; }

        [DataMember(Name = OAuth2ResponseBaseClaim.ErrorDescription, IsRequired = false)]
        public string ErrorDescription { get; set; }

        /// <summary>
        /// Do not expose these in the MsalException because Evo does not guarantee that the error
        /// codes remain the same.
        /// </summary>
        [DataMember(Name = OAuth2ResponseBaseClaim.ErrorCodes, IsRequired = false)]
        public string[] ErrorCodes { get; set; }

        [DataMember(Name = OAuth2ResponseBaseClaim.CorrelationId, IsRequired = false)]
        public string CorrelationId { get; set; }

        [DataMember(Name = OAuth2ResponseBaseClaim.Claims, IsRequired = false)]
        public string Claims { get; set; }
    }
}
