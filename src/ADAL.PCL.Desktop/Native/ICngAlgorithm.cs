
using System.Security.Cryptography;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory.Native
{
    interface ICngAlgorithm
    {
        /// <summary>
        ///     Gets the algorithm or key storage provider being used for the implementation of the CNG
        ///     algorithm.
        /// </summary>
        CngProvider Provider { get; }
    }
}
