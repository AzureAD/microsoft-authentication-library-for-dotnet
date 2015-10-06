using System;
using System.Collections.Generic;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    class AuthenticationRequest
    {
        public int RequestId { get; set; }

        public string Authority { get; set; }

        public string RedirectUri { get; set; }

        public string Resource { get; set; }

        public string ClientId { get; set; }

        public string LoginHint { get; set; }

        public string UserId { get; set; }

        public string BrokerAccountName { get; set; }

        public Guid CorrelationId { get; set; }

        public string ExtraQueryParamsAuthentication { get; set; }
        
        public bool Silent { get; set; }

        public string Version { get; set; }

        public AuthenticationRequest(IDictionary<string, string> brokerPayload)
        {
            Authority = brokerPayload["authority"];
            Resource = brokerPayload["resource"];
            ClientId = brokerPayload["client_id"];
            if (brokerPayload.ContainsKey("redirect_uri"))
            {
                RedirectUri = brokerPayload["redirect_uri"];
            }

            LoginHint = brokerPayload["username"];
            BrokerAccountName = LoginHint;
            if (brokerPayload.ContainsKey("extra_qp"))
            {
                ExtraQueryParamsAuthentication = brokerPayload["extra_qp"];
            }
            CorrelationId = Guid.Parse(brokerPayload["correlation_id"]);
            Version = brokerPayload["client_version"];
        }
    }
}