// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.AuthScheme.PoP;

namespace Microsoft.Identity.Client.AuthScheme.CDT
{
    /// <summary>
    /// An abstraction over an the asymmetric key operations needed by CDT, that encapsulates a pair of 
    /// public and private keys and some typical crypto operations.
    /// All symmetric operations are SHA256.
    /// </summary>
    /// <remarks>
    /// Important: The 2 methods on this interface will be called at different times but MUST return details of 
    /// the same private / public key pair, i.e. do not change to a different key pair mid way. Best to have this class immutable.
    /// 
    /// Ideally there should be a single public / private key pair associated with a machine, so implementers of this interface
    /// should consider exposing a singleton.
    /// </remarks>
    internal interface ICdtCryptoProvider : IPoPCryptoProvider
    {

    }
}
