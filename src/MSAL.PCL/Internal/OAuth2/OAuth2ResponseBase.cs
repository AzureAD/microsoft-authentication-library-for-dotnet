using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Internal.OAuth2
{
    internal class OAuth2ResponseBaseClaim
    {
        public const string Error = "error";
        public const string ErrorDescription = "error_description";
        public const string ErrorCodes = "error_codes";
        public const string CorrelationId = "correlation_id";
    }

    [DataContract]
    internal class OAuth2ResponseBase
    {
        [DataMember(Name = OAuth2ResponseBaseClaim.Error, IsRequired = false)]
        public string Error { get; set; }

        [DataMember(Name = OAuth2ResponseBaseClaim.ErrorDescription, IsRequired = false)]
        public string ErrorDescription { get; set; }

        [DataMember(Name = OAuth2ResponseBaseClaim.ErrorCodes, IsRequired = false)]
        public string[] ErrorCodes { get; set; }

        [DataMember(Name = OAuth2ResponseBaseClaim.CorrelationId, IsRequired = false)]
        public string CorrelationId { get; set; }
    }
}