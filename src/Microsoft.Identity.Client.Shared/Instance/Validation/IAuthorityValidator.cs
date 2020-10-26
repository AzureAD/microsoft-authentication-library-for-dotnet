// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Instance.Validation
{
    internal interface IAuthorityValidator
    {
        /// <summary>
        /// Validates the authority. This is specific to each authority type.
        /// </summary>
        Task ValidateAuthorityAsync(
            AuthorityInfo authorityInfo,
            RequestContext requestContext);
    }
}
