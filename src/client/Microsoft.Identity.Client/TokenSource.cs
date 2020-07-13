using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Specifies what is the source of token in the authentication result.
    /// </summary>
    public enum TokenSource
    {
        /// <summary>
        /// The source of token is cache.
        /// </summary>
        Cache,
        /// <summary>
        /// The source of token is Identity Provider like AAD, ADFS or B2C.
        /// </summary>
        IdentityProvider,
        /// <summary>
        /// The source of token is Broker.
        /// </summary>
        Broker
    }
}
