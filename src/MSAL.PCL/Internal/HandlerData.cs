
using System.Threading;

namespace Microsoft.Identity.Client.Internal
{
    internal class HandlerData
    {
        public Authenticator Authenticator { get; set; }

        public TokenCache TokenCache { get; set; }

        public string[] Scope { get; set; }

        public ClientKey ClientKey { get; set; }

        public string Policy { get; set; }

        public bool RestrictToSingleUser { get; set; }
    }
}
