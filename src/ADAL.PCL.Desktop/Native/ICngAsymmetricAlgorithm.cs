using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory.Native
{
    /// <summary>
    ///     Interface for asymmetric algorithms implemented over the CNG layer of Windows to provide CNG
    ///     implementation details through.
    /// </summary>
    interface ICngAsymmetricAlgorithm : ICngAlgorithm
    {
        /// <summary>
        ///     Get the CNG key being used by the asymmetric algorithm.
        /// </summary>
        /// <permission cref="SecurityPermission">
        ///     This method requires that the immediate caller have SecurityPermission/UnmanagedCode
        /// </permission>
        CngKey Key
        {
            [SecurityCritical]
            [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
            get;
        }
    }
}
