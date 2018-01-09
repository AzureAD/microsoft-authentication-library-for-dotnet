using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Flows
{
    internal static class BrokerParameter
    {
        public const string Authority = "authority";
        public const string Resource = "resource";
        public const string ClientId = "client_id";
        public const string CorrelationId = "correlation_id";
        public const string ClientVersion = "client_version";
        public const string Force = "force";
        public const string Username = "username";
        public const string UsernameType = "username_type";
        public const string RedirectUri = "redirect_uri";
        public const string ExtraQp = "extra_qp";
        public const string Claims = "claims";
        public const string SilentBrokerFlow = "silent_broker_flow";
        public const string BrokerInstallUrl = "broker_install_url";
    }
}
